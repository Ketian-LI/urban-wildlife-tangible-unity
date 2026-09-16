using System;
using System.Collections.Generic;
using System.Linq;

namespace UrbanWildlife.Input
{
    public static class CityTokenInventory
    {
        private const int DesktopApartmentStart = 1000;
        private const int DesktopDetachedStart = 1200;
        private const int DesktopCommercialStart = 1400;
        private const int DesktopCommunityStart = 1600;
        private const int DesktopRangeSize = 200;

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
            return TypeById.TryGetValue(id, out type) ||
                   TryGetDesktopVirtualType(id, out type);
        }

        public static bool IsPhysicalTokenId(int id)
        {
            return TypeById.ContainsKey(id);
        }

        public static bool IsDesktopVirtualTokenId(int id)
        {
            return TryGetDesktopVirtualType(id, out _);
        }

        public static int NextDesktopVirtualId(
            CityPhysicalTokenType type,
            IEnumerable<int> usedIds)
        {
            int start;
            switch (type)
            {
                case CityPhysicalTokenType.Apartment:
                    start = DesktopApartmentStart;
                    break;
                case CityPhysicalTokenType.DetachedHouse:
                    start = DesktopDetachedStart;
                    break;
                case CityPhysicalTokenType.Commercial:
                    start = DesktopCommercialStart;
                    break;
                case CityPhysicalTokenType.CommunityFacility:
                    start = DesktopCommunityStart;
                    break;
                default:
                    return 0;
            }
            HashSet<int> used = new HashSet<int>(usedIds ?? Enumerable.Empty<int>());
            for (int id = start; id < start + DesktopRangeSize; id += 1)
            {
                if (!used.Contains(id))
                {
                    return id;
                }
            }
            return 0;
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

        private static bool TryGetDesktopVirtualType(
            int id,
            out CityPhysicalTokenType type)
        {
            if (id >= DesktopApartmentStart &&
                id < DesktopApartmentStart + DesktopRangeSize)
            {
                type = CityPhysicalTokenType.Apartment;
                return true;
            }
            if (id >= DesktopDetachedStart &&
                id < DesktopDetachedStart + DesktopRangeSize)
            {
                type = CityPhysicalTokenType.DetachedHouse;
                return true;
            }
            if (id >= DesktopCommercialStart &&
                id < DesktopCommercialStart + DesktopRangeSize)
            {
                type = CityPhysicalTokenType.Commercial;
                return true;
            }
            if (id >= DesktopCommunityStart &&
                id < DesktopCommunityStart + DesktopRangeSize)
            {
                type = CityPhysicalTokenType.CommunityFacility;
                return true;
            }
            type = default;
            return false;
        }
    }
}
