using System;
using System.Collections.Generic;
using System.Linq;

namespace UrbanWildlife.Input
{
    public static class CityTokenInventory
    {
        private static readonly Dictionary<int, CityPhysicalTokenType> TypeById =
            new Dictionary<int, CityPhysicalTokenType>
            {
                { 100, CityPhysicalTokenType.Apartment },
                { 101, CityPhysicalTokenType.Apartment },
                { 102, CityPhysicalTokenType.Apartment },
                { 110, CityPhysicalTokenType.DetachedHouse },
                { 111, CityPhysicalTokenType.DetachedHouse },
                { 112, CityPhysicalTokenType.DetachedHouse },
                { 113, CityPhysicalTokenType.DetachedHouse },
                { 120, CityPhysicalTokenType.Commercial },
                { 121, CityPhysicalTokenType.Commercial },
                { 130, CityPhysicalTokenType.CommunityFacility },
                { 131, CityPhysicalTokenType.CommunityFacility },
                { 140, CityPhysicalTokenType.GreenIntervention },
                { 141, CityPhysicalTokenType.GreenIntervention },
                { 142, CityPhysicalTokenType.GreenIntervention },
            };

        public static int TotalAvailable => TypeById.Count;

        public static int MaximumFor(CityPhysicalTokenType type)
        {
            return TypeById.Values.Count(value => value == type);
        }

        public static bool TryGetExpectedType(int id, out CityPhysicalTokenType type)
        {
            return TypeById.TryGetValue(id, out type);
        }

        public static int[] IdsFor(CityPhysicalTokenType type)
        {
            return TypeById.Where(pair => pair.Value == type)
                .Select(pair => pair.Key)
                .OrderBy(id => id)
                .ToArray();
        }

        public static bool MatchesInventory(CityTokenState token, out string error)
        {
            error = string.Empty;
            if (token == null)
            {
                error = "Token state is missing.";
                return false;
            }
            if (!TryGetExpectedType(token.id, out CityPhysicalTokenType expected))
            {
                error = $"Token ID {token.id} is not part of the 14-piece city inventory.";
                return false;
            }
            if (token.type != expected)
            {
                error = $"Token ID {token.id} must be {expected}, not {token.type}.";
                return false;
            }
            return true;
        }
    }
}
