using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlife.Input;

namespace UrbanWildlife.Construction
{
    public static class CityTokenDiffer
    {
        public const float DefaultPositionToleranceNorm = 0.01f;
        public const float DefaultRotationToleranceDeg = 5f;

        public static CityTokenChange[] Compare(
            IEnumerable<CityTokenState> previous,
            IEnumerable<CityTokenState> scanned,
            float positionToleranceNorm = DefaultPositionToleranceNorm,
            float rotationToleranceDeg = DefaultRotationToleranceDeg)
        {
            if (positionToleranceNorm < 0f || rotationToleranceDeg < 0f)
            {
                throw new ArgumentOutOfRangeException("Token tolerances cannot be negative.");
            }

            CityTokenState[] previousStates = Prepare(previous, "previous");
            CityTokenState[] scannedStates = Prepare(scanned, "scanned");
            Dictionary<int, CityTokenState> previousById = previousStates
                .ToDictionary(token => token.id);
            Dictionary<int, CityTokenState> scannedById = scannedStates
                .ToDictionary(token => token.id);
            List<CityTokenChange> changes = new List<CityTokenChange>();

            foreach (CityTokenState current in scannedStates.OrderBy(token => token.id))
            {
                if (!previousById.TryGetValue(current.id, out CityTokenState before))
                {
                    changes.Add(Create(
                        current,
                        CityTokenChangeKind.New,
                        null,
                        current,
                        false,
                        false,
                        "New Token enters Proposed Construction."));
                    continue;
                }

                bool moved = Distance(before, current) > positionToleranceNorm ||
                             RotationDelta(before.rotation_deg, current.rotation_deg) > rotationToleranceDeg;
                changes.Add(Create(
                    current,
                    moved ? CityTokenChangeKind.Moved : CityTokenChangeKind.Unchanged,
                    before,
                    current,
                    moved,
                    false,
                    moved
                        ? "Built objects cannot move directly. Return the Token or request demolition first."
                        : "Token matches the last confirmed city state."));
            }

            foreach (CityTokenState before in previousStates
                         .Where(token => !scannedById.ContainsKey(token.id))
                         .OrderBy(token => token.id))
            {
                changes.Add(Create(
                    before,
                    CityTokenChangeKind.Missing,
                    before,
                    null,
                    true,
                    true,
                    "Token is missing. Ask Demolish?; never remove the city object automatically."));
            }

            return changes.ToArray();
        }

        private static CityTokenState[] Prepare(IEnumerable<CityTokenState> states, string label)
        {
            CityTokenState[] values = (states ?? Array.Empty<CityTokenState>()).ToArray();
            if (values.Any(token => token == null))
            {
                throw new ArgumentException($"The {label} Token collection contains a null state.");
            }
            int[] duplicates = values.GroupBy(token => token.id)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();
            if (duplicates.Length > 0)
            {
                throw new ArgumentException(
                    $"The {label} Token collection repeats ID {duplicates[0]}.");
            }
            foreach (CityTokenState token in values)
            {
                if (!CityTokenInventory.MatchesInventory(token, out string error))
                {
                    throw new ArgumentException(error);
                }
            }
            return values;
        }

        private static CityTokenChange Create(
            CityTokenState identity,
            CityTokenChangeKind kind,
            CityTokenState previous,
            CityTokenState scanned,
            bool blocks,
            bool requiresDemolition,
            string message)
        {
            return new CityTokenChange
            {
                token_id = identity.id,
                token_type = identity.type,
                change_kind = kind,
                previous_state = Clone(previous),
                scanned_state = Clone(scanned),
                proposed_city_object_id = CityConstructionFactory.ObjectId(identity),
                blocks_confirmation = blocks,
                requires_demolition_confirmation = requiresDemolition,
                message = message,
            };
        }

        private static CityTokenState Clone(CityTokenState token)
        {
            if (token == null)
            {
                return null;
            }
            return new CityTokenState
            {
                id = token.id,
                type = token.type,
                x_norm = token.x_norm,
                y_norm = token.y_norm,
                rotation_deg = token.rotation_deg,
                confidence = token.confidence,
            };
        }

        private static float Distance(CityTokenState first, CityTokenState second)
        {
            float x = first.x_norm - second.x_norm;
            float y = first.y_norm - second.y_norm;
            return (float)Math.Sqrt(x * x + y * y);
        }

        private static float RotationDelta(float first, float second)
        {
            float delta = Math.Abs(first - second) % 360f;
            return Math.Min(delta, 360f - delta);
        }
    }
}
