using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UrbanWildlife.Input;

namespace UrbanWildlife.Construction
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityTokenChangeKind
    {
        New,
        Moved,
        Missing,
        Unchanged,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityConstructionFlowPhase
    {
        Planning,
        Preview,
    }

    [Serializable]
    public sealed class CityTokenChange
    {
        public int token_id;
        public CityPhysicalTokenType token_type;
        public CityTokenChangeKind change_kind;
        public CityTokenState previous_state;
        public CityTokenState scanned_state;
        public string proposed_city_object_id;
        public bool blocks_confirmation;
        public bool requires_demolition_confirmation;
        public string message;
    }

    [Serializable]
    public sealed class CityConstructionPreview
    {
        public string scan_id;
        public long scan_timestamp_ms;
        public CityTokenChange[] changes = Array.Empty<CityTokenChange>();

        public int NewCount => Count(CityTokenChangeKind.New);
        public int MovedCount => Count(CityTokenChangeKind.Moved);
        public int MissingCount => Count(CityTokenChangeKind.Missing);
        public int UnchangedCount => Count(CityTokenChangeKind.Unchanged);
        public bool HasBlockingChanges => changes.Any(change => change.blocks_confirmation);
        public bool CanConfirmConstruction => NewCount > 0 && !HasBlockingChanges;

        private int Count(CityTokenChangeKind kind)
        {
            return changes.Count(change => change.change_kind == kind);
        }
    }
}
