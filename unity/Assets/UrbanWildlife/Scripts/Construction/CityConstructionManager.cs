using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlife.City;
using UrbanWildlife.Input;

namespace UrbanWildlife.Construction
{
    public sealed class CityConstructionManager
    {
        public const string SourceContract = "city-token-construction-0.1";

        private CityTokenScanPacket pendingScan;
        private CityTokenState[] confirmedTokens;
        private CityPlanningGrid pendingPlanningGrid;
        private CityBuilding[] pendingBuildings = Array.Empty<CityBuilding>();
        private CityGreenPatch[] pendingPatches = Array.Empty<CityGreenPatch>();
        private string[] pendingRemovedPatchIds = Array.Empty<string>();

        public CityConstructionManager(
            CityState initialState,
            IEnumerable<CityTokenState> initialConfirmedTokens = null)
        {
            CityStateValidationResult validation = CityStateValidator.Validate(initialState);
            if (!validation.IsValid)
            {
                throw new ArgumentException($"Initial city state is invalid: {validation.Summary}");
            }

            CurrentState = initialState;
            confirmedTokens = SnapTokensToGrid(
                CloneTokens(initialConfirmedTokens),
                initialState.planning_grid,
                initialState.bounds);
            CurrentPhase = CityConstructionFlowPhase.Planning;
            LastMessage = "Ready to Scan City.";
        }

        public CityState CurrentState { get; private set; }
        public CityConstructionFlowPhase CurrentPhase { get; private set; }
        public CityConstructionPreview PendingPreview { get; private set; }
        public string LastMessage { get; private set; }
        public CityTokenState[] ConfirmedTokens => CloneTokens(confirmedTokens);

        public CityConstructionPreview ScanCity(CityTokenScanPacket scan)
        {
            ValidateScanObject(scan);
            CityTokenScanPacket snappedScan = CloneScan(scan);
            snappedScan.token_states = SnapTokensToGrid(
                snappedScan.token_states,
                CurrentState.planning_grid,
                CurrentState.bounds);
            CityTokenChange[] changes = CityTokenDiffer.Compare(
                confirmedTokens,
                snappedScan.token_states,
                CityTokenDiffer.DefaultPositionToleranceNorm,
                CityTokenDiffer.DefaultRotationToleranceDeg);
            List<CityBuilding> proposedBuildings = new List<CityBuilding>();
            List<CityGreenPatch> proposedPatches = new List<CityGreenPatch>();
            HashSet<string> removedPatchIds = new HashSet<string>(StringComparer.Ordinal);
            CityPlanningGrid previewGrid = CityGridResolver.DeepClone(CurrentState.planning_grid);
            bool desktopPointerPlacement = string.Equals(
                snappedScan.capture?.mode,
                "desktop_pointer",
                StringComparison.Ordinal);
            int activePlayerBuildings = (CurrentState.buildings ?? Array.Empty<CityBuilding>())
                .Count(IsPlayerBuilding);
            foreach (CityTokenChange change in changes.Where(item =>
                         item.change_kind == CityTokenChangeKind.New))
            {
                if (change.token_type == CityPhysicalTokenType.GreenIntervention)
                {
                    CityGreenPatch patch = CityConstructionFactory.CreateGreenIntervention(
                        change.scanned_state,
                        CurrentState.bounds,
                        CityConstructionState.Proposed);
                    string issue = string.Empty;
                    CityPlanningGrid candidateGrid = CityGridResolver.DeepClone(previewGrid);
                    if (candidateGrid != null && !CityGridResolver.TryOccupyWithGreenPatch(
                            candidateGrid,
                            CurrentState.bounds,
                            patch,
                            CurrentState.revision + 1,
                            out issue))
                    {
                        // Resolver supplies the placement issue.
                    }
                    if (string.IsNullOrEmpty(issue))
                    {
                        issue = CityConstructionPlacementRules.ValidateGreenIntervention(patch);
                    }
                    ApplyPlacementIssue(change, issue);
                    if (string.IsNullOrEmpty(issue))
                    {
                        previewGrid = candidateGrid;
                        proposedPatches.Add(patch);
                    }
                }
                else
                {
                    CityBuilding building = CityConstructionFactory.CreateBuilding(
                        change.scanned_state,
                        CityConstructionState.Proposed);
                    string issue = string.Empty;
                    CityPlanningGrid candidateGrid = CityGridResolver.DeepClone(previewGrid);
                    string[] overwrittenPatchIds = Array.Empty<string>();
                    if (candidateGrid != null)
                    {
                        if (activePlayerBuildings + proposedBuildings.Count >=
                            candidateGrid.max_active_player_buildings)
                        {
                            issue =
                                $"The city allows at most {candidateGrid.max_active_player_buildings} " +
                                "active player buildings.";
                        }
                        else
                        {
                            CityGridCell[] footprintCells =
                                CityGridResolver.GetBuildingFootprintCellsAtPosition(
                                    candidateGrid,
                                    CurrentState.bounds,
                                    building,
                                    out issue);
                            if (string.IsNullOrEmpty(issue))
                            {
                                overwrittenPatchIds = footprintCells
                                    .Select(cell => cell.habitat_patch_id)
                                    .Where(id => !string.IsNullOrWhiteSpace(id))
                                    .Distinct(StringComparer.Ordinal)
                                    .ToArray();
                                if (!CityGridResolver.TryOccupyWithBuildingAtPosition(
                                        candidateGrid,
                                        CurrentState.bounds,
                                        building,
                                        CurrentState.revision + 1,
                                        desktopPointerPlacement,
                                        out issue))
                                {
                                    // Resolver supplies the placement issue.
                                }
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(issue))
                    {
                        issue = CityConstructionPlacementRules.ValidateBuilding(
                            building,
                            CurrentState.bounds,
                            CurrentState.buildings,
                            proposedBuildings,
                            CurrentState.vehicle_roads);
                    }
                    ApplyPlacementIssue(change, issue);
                    if (string.IsNullOrEmpty(issue))
                    {
                        previewGrid = candidateGrid;
                        proposedBuildings.Add(building);
                        foreach (string overwrittenPatchId in overwrittenPatchIds)
                        {
                            removedPatchIds.Add(overwrittenPatchId);
                        }
                    }
                }
            }

            pendingScan = snappedScan;
            pendingPlanningGrid = previewGrid;
            pendingBuildings = proposedBuildings.ToArray();
            pendingPatches = proposedPatches.ToArray();
            pendingRemovedPatchIds = removedPatchIds.ToArray();
            PendingPreview = new CityConstructionPreview
            {
                scan_id = snappedScan.scan_id,
                scan_timestamp_ms = snappedScan.timestamp_ms,
                changes = changes,
            };
            CurrentPhase = CityConstructionFlowPhase.Preview;
            LastMessage = PreviewMessage(PendingPreview);
            return PendingPreview;
        }

        public bool ConfirmConstruction(out string error)
        {
            error = string.Empty;
            if (PendingPreview == null || pendingScan == null ||
                CurrentPhase != CityConstructionFlowPhase.Preview)
            {
                error = "Scan City before confirming construction.";
                LastMessage = error;
                return false;
            }
            if (!PendingPreview.CanConfirmConstruction)
            {
                error = PendingPreview.HasBlockingChanges
                    ? "Resolve Moved, Missing or invalid placements before confirmation."
                    : "The scan contains no new construction to confirm.";
                LastMessage = error;
                return false;
            }

            int confirmedCount = PendingPreview.NewCount;
            if (pendingBuildings.Length + pendingPatches.Length != confirmedCount)
            {
                error = "The grid preview is incomplete. Scan City again before confirming.";
                LastMessage = error;
                return false;
            }
            CityBuilding[] newBuildings = pendingBuildings;
            CityGreenPatch[] newPatches = pendingPatches;
            CityWasteNode[] newWasteNodes = newBuildings
                .Select(CityConstructionFactory.CreateBuildingWasteNode)
                .ToArray();

            CityState next = AppendConstruction(
                CurrentState,
                newBuildings,
                newPatches,
                newWasteNodes,
                pendingPlanningGrid,
                pendingRemovedPatchIds);
            CityStateValidationResult validation = CityStateValidator.Validate(next);
            if (!validation.IsValid)
            {
                error = $"Confirmed construction would create an invalid city: {validation.Summary}";
                LastMessage = error;
                return false;
            }

            CurrentState = next;
            confirmedTokens = CloneTokens(pendingScan.token_states);
            ClearPendingPreview();
            CurrentPhase = CityConstructionFlowPhase.Planning;
            LastMessage =
                $"Confirmed {confirmedCount} proposed construction item(s). Road selection is the next step.";
            return true;
        }

        public void CancelPreview()
        {
            ClearPendingPreview();
            CurrentPhase = CityConstructionFlowPhase.Planning;
            LastMessage = "Preview cancelled; the confirmed city was not changed.";
        }

        private static CityState AppendConstruction(
            CityState current,
            CityBuilding[] newBuildings,
            CityGreenPatch[] newPatches,
            CityWasteNode[] newWasteNodes,
            CityPlanningGrid plannedGrid,
            IEnumerable<string> removedPatchIds)
        {
            CityWasteSystem currentWaste = current.waste ?? new CityWasteSystem();
            HashSet<string> removed = new HashSet<string>(
                removedPatchIds ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            CityGreenPatch[] retainedPatches = (current.green_patches ??
                                                Array.Empty<CityGreenPatch>())
                .Where(patch => patch != null && !removed.Contains(patch.id))
                .Select(CloneGreenPatch)
                .ToArray();
            CityGreenPatch[] combinedPatches = retainedPatches
                .Concat(newPatches ?? Array.Empty<CityGreenPatch>())
                .ToArray();
            foreach (CityGreenPatch patch in combinedPatches)
            {
                patch.connected_patch_ids = (patch.connected_patch_ids ?? Array.Empty<string>())
                    .Where(id => !removed.Contains(id))
                    .ToArray();
            }
            return new CityState
            {
                schema_version = current.schema_version,
                city_id = current.city_id,
                revision = current.revision + 1,
                source_contract = SourceContract,
                bounds = current.bounds,
                planning_grid = CityGridResolver.DeepClone(plannedGrid),
                buildings = (current.buildings ?? Array.Empty<CityBuilding>())
                    .Concat(newBuildings)
                    .ToArray(),
                green_patches = combinedPatches,
                vehicle_roads = current.vehicle_roads ?? Array.Empty<CityVehicleRoad>(),
                pedestrian_links = current.pedestrian_links ?? Array.Empty<CityPedestrianLink>(),
                amenities = current.amenities ?? Array.Empty<CityAmenity>(),
                waste = new CityWasteSystem
                {
                    total_demand = currentWaste.total_demand +
                                   newWasteNodes.Sum(node => node.generation_rate),
                    total_capacity = currentWaste.total_capacity,
                    overflow_elapsed_seconds = currentWaste.overflow_elapsed_seconds,
                    overflow_active = currentWaste.overflow_active,
                    nodes = (currentWaste.nodes ?? Array.Empty<CityWasteNode>())
                        .Concat(newWasteNodes)
                        .ToArray(),
                    litter_hotspots = currentWaste.litter_hotspots ??
                                      Array.Empty<CityLitterHotspot>(),
                },
            };
        }

        private static void ApplyPlacementIssue(CityTokenChange change, string issue)
        {
            if (string.IsNullOrEmpty(issue))
            {
                return;
            }
            change.blocks_confirmation = true;
            change.message = issue;
        }

        private void ClearPendingPreview()
        {
            pendingScan = null;
            pendingPlanningGrid = null;
            pendingBuildings = Array.Empty<CityBuilding>();
            pendingPatches = Array.Empty<CityGreenPatch>();
            pendingRemovedPatchIds = Array.Empty<string>();
            PendingPreview = null;
        }

        private static bool IsPlayerBuilding(CityBuilding building)
        {
            return building != null && building.source_token_id >= 0;
        }

        private static CityTokenState[] SnapTokensToGrid(
            IEnumerable<CityTokenState> tokens,
            CityPlanningGrid grid,
            CityBounds bounds)
        {
            CityTokenState[] snapped = CloneTokens(tokens);
            if (grid == null)
            {
                return snapped;
            }

            foreach (CityTokenState token in snapped)
            {
                if (token.type != CityPhysicalTokenType.GreenIntervention)
                {
                    continue;
                }
                float[] position = { token.x_norm, token.y_norm };
                if (!CityGridResolver.TryResolveNearestCell(
                        grid,
                        bounds,
                        position,
                        out CityGridCell cell))
                {
                    continue;
                }
                token.x_norm = cell.center_norm[0];
                token.y_norm = cell.center_norm[1];
            }
            return snapped;
        }

        private static CityGreenPatch CloneGreenPatch(CityGreenPatch patch)
        {
            return new CityGreenPatch
            {
                id = patch.id,
                source_token_id = patch.source_token_id,
                planning_cell_id = patch.planning_cell_id,
                type = patch.type,
               construction_state = patch.construction_state,
               public_park = patch.public_park,
               polygon_norm = (patch.polygon_norm ?? Array.Empty<float[]>())
                    .Select(point => point == null ? null : (float[])point.Clone())
                    .ToArray(),
                shelter_value = patch.shelter_value,
               natural_food_value = patch.natural_food_value,
                human_disturbance = patch.human_disturbance,
                patch_size_units = patch.patch_size_units,
                connected_patch_ids = (patch.connected_patch_ids ?? Array.Empty<string>())
                    .ToArray(),
            };
        }

        private static string PreviewMessage(CityConstructionPreview preview)
        {
            if (preview.HasBlockingChanges)
            {
                return
                    $"Preview blocked — New {preview.NewCount}, Moved {preview.MovedCount}, " +
                    $"Missing {preview.MissingCount}, Unchanged {preview.UnchangedCount}.";
            }
            if (preview.NewCount == 0)
            {
                return $"Preview contains {preview.UnchangedCount} unchanged Token(s) and no construction.";
            }
            return
                $"Preview ready — {preview.NewCount} proposed construction item(s); confirm to commit.";
        }

        private static void ValidateScanObject(CityTokenScanPacket scan)
        {
            if (scan == null || string.IsNullOrWhiteSpace(scan.scan_id) ||
                scan.token_states == null)
            {
                throw new ArgumentException("A validated City Scan packet is required.");
            }
            CityTokenState[] tokens = scan.token_states;
            if (tokens.Any(token => token == null) ||
                tokens.GroupBy(token => token.id).Any(group => group.Count() > 1))
            {
                throw new ArgumentException("City Scan Token states must be non-null and unique.");
            }
            foreach (CityTokenState token in tokens)
            {
                if (!CityTokenInventory.MatchesInventory(token, out string error))
                {
                    throw new ArgumentException(error);
                }
            }
        }

        private static CityTokenScanPacket CloneScan(CityTokenScanPacket scan)
        {
            return new CityTokenScanPacket
            {
                schema_version = scan.schema_version,
                packet_type = scan.packet_type,
                timestamp_ms = scan.timestamp_ms,
                timestamp_utc = scan.timestamp_utc,
                scan_id = scan.scan_id,
                session_id = scan.session_id,
                development_phase = scan.development_phase,
                calibration_id = scan.calibration_id,
                coordinate_system = scan.coordinate_system,
                capture = scan.capture,
                recognition = scan.recognition,
                token_states = CloneTokens(scan.token_states),
            };
        }

        private static CityTokenState[] CloneTokens(IEnumerable<CityTokenState> tokens)
        {
            return (tokens ?? Array.Empty<CityTokenState>())
                .Select(token => new CityTokenState
                {
                    id = token.id,
                    type = token.type,
                    x_norm = token.x_norm,
                    y_norm = token.y_norm,
                    rotation_deg = token.rotation_deg,
                    confidence = token.confidence,
                })
                .ToArray();
        }
    }
}
