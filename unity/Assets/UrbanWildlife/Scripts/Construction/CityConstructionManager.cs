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
            confirmedTokens = CloneTokens(initialConfirmedTokens);
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
            CityTokenChange[] changes = CityTokenDiffer.Compare(
                confirmedTokens,
                scan.token_states,
                CityTokenDiffer.DefaultPositionToleranceNorm,
                CityTokenDiffer.DefaultRotationToleranceDeg);
            List<CityBuilding> proposedBuildings = new List<CityBuilding>();
            foreach (CityTokenChange change in changes.Where(item =>
                         item.change_kind == CityTokenChangeKind.New))
            {
                if (change.token_type == CityPhysicalTokenType.GreenIntervention)
                {
                    CityGreenPatch patch = CityConstructionFactory.CreateGreenIntervention(
                        change.scanned_state,
                        CurrentState.bounds,
                        CityConstructionState.Proposed);
                    string issue = CityConstructionPlacementRules.ValidateGreenIntervention(patch);
                    ApplyPlacementIssue(change, issue);
                }
                else
                {
                    CityBuilding building = CityConstructionFactory.CreateBuilding(
                        change.scanned_state,
                        CityConstructionState.Proposed);
                    string issue = CityConstructionPlacementRules.ValidateBuilding(
                        building,
                        CurrentState.bounds,
                        CurrentState.buildings,
                        proposedBuildings);
                    ApplyPlacementIssue(change, issue);
                    if (string.IsNullOrEmpty(issue))
                    {
                        proposedBuildings.Add(building);
                    }
                }
            }

            pendingScan = CloneScan(scan);
            PendingPreview = new CityConstructionPreview
            {
                scan_id = scan.scan_id,
                scan_timestamp_ms = scan.timestamp_ms,
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

            CityTokenChange[] additions = PendingPreview.changes
                .Where(change => change.change_kind == CityTokenChangeKind.New)
                .ToArray();
            CityBuilding[] newBuildings = additions
                .Where(change => change.token_type != CityPhysicalTokenType.GreenIntervention)
                .Select(change => CityConstructionFactory.CreateBuilding(
                    change.scanned_state,
                    CityConstructionState.Proposed))
                .ToArray();
            CityGreenPatch[] newPatches = additions
                .Where(change => change.token_type == CityPhysicalTokenType.GreenIntervention)
                .Select(change => CityConstructionFactory.CreateGreenIntervention(
                    change.scanned_state,
                    CurrentState.bounds,
                    CityConstructionState.Proposed))
                .ToArray();
            CityWasteNode[] newWasteNodes = newBuildings
                .Select(CityConstructionFactory.CreateBuildingWasteNode)
                .ToArray();

            CityState next = AppendConstruction(CurrentState, newBuildings, newPatches, newWasteNodes);
            CityStateValidationResult validation = CityStateValidator.Validate(next);
            if (!validation.IsValid)
            {
                error = $"Confirmed construction would create an invalid city: {validation.Summary}";
                LastMessage = error;
                return false;
            }

            CurrentState = next;
            confirmedTokens = CloneTokens(pendingScan.token_states);
            int confirmedCount = additions.Length;
            pendingScan = null;
            PendingPreview = null;
            CurrentPhase = CityConstructionFlowPhase.Planning;
            LastMessage =
                $"Confirmed {confirmedCount} proposed construction item(s). Road selection is the next step.";
            return true;
        }

        public void CancelPreview()
        {
            pendingScan = null;
            PendingPreview = null;
            CurrentPhase = CityConstructionFlowPhase.Planning;
            LastMessage = "Preview cancelled; the confirmed city was not changed.";
        }

        private static CityState AppendConstruction(
            CityState current,
            CityBuilding[] newBuildings,
            CityGreenPatch[] newPatches,
            CityWasteNode[] newWasteNodes)
        {
            CityWasteSystem currentWaste = current.waste ?? new CityWasteSystem();
            return new CityState
            {
                schema_version = current.schema_version,
                city_id = current.city_id,
                revision = current.revision + 1,
                source_contract = SourceContract,
                bounds = current.bounds,
                buildings = (current.buildings ?? Array.Empty<CityBuilding>())
                    .Concat(newBuildings)
                    .ToArray(),
                green_patches = (current.green_patches ?? Array.Empty<CityGreenPatch>())
                    .Concat(newPatches)
                    .ToArray(),
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
