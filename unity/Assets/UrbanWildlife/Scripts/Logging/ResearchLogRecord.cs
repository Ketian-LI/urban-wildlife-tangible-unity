using System;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace UrbanWildlife.Logging
{
    [Serializable]
    public sealed class ResearchLogRecord
    {
        public string timestamp_utc;
        public string session_id;
        public int cycle_index;
        public string cycle_scenario_id;
        public string cycle_title;
        public string event_type;
        public string phase;
        public string predicted_top_feeder;
        public string predicted_conflict_area;
        public int? memory_source_cycle_index;
        public int? remembered_human_trips;
        public int? remembered_avoidance_events;
        public int? remembered_pigeon_food_token_id;
        public int? remembered_squirrel_food_token_id;
        public int? remembered_fox_food_token_id;
        public float? carried_squirrel_familiarity;
        public float? memory_caution_multiplier;
        public string score_formula_version;
        public int? score_total;
        public int? score_human_access;
        public int? score_pigeon_feeding;
        public int? score_squirrel_feeding;
        public int? score_fox_feeding;
        public long? layout_timestamp_ms;
        public bool? human_connected;
        public bool? animal_reachable;
        public bool? food_hotspot_valid;
        public int? changes_used;
        public int? changes_allowed;
        public int human_trips;
        public int pigeon_count;
        public int squirrel_count;
        public int fox_count;
        public int pigeon_feed_events;
        public int squirrel_feed_events;
        public int fox_feed_events;
        public int animal_avoidance_events;
        public string note;
    }

    public static class ResearchLogFormatter
    {
        public const string CsvHeader =
            "timestamp_utc,session_id,cycle_index,event_type,phase,layout_timestamp_ms," +
            "cycle_scenario_id,cycle_title,predicted_top_feeder,predicted_conflict_area," +
            "memory_source_cycle_index,remembered_human_trips,remembered_avoidance_events," +
            "remembered_pigeon_food_token_id,remembered_squirrel_food_token_id," +
            "remembered_fox_food_token_id,carried_squirrel_familiarity,memory_caution_multiplier," +
            "score_formula_version,score_total,score_human_access,score_pigeon_feeding," +
            "score_squirrel_feeding,score_fox_feeding," +
            "human_connected,animal_reachable,food_hotspot_valid,changes_used,changes_allowed," +
            "human_trips,pigeon_count,squirrel_count,fox_count," +
            "pigeon_feed_events,squirrel_feed_events,fox_feed_events," +
            "animal_avoidance_events,note";

        public static string ToCsvRow(ResearchLogRecord record)
        {
            object[] values =
            {
                record.timestamp_utc,
                record.session_id,
                record.cycle_index,
                record.event_type,
                record.phase,
                record.layout_timestamp_ms,
                record.cycle_scenario_id,
                record.cycle_title,
                record.predicted_top_feeder,
                record.predicted_conflict_area,
                record.memory_source_cycle_index,
                record.remembered_human_trips,
                record.remembered_avoidance_events,
                record.remembered_pigeon_food_token_id,
                record.remembered_squirrel_food_token_id,
                record.remembered_fox_food_token_id,
                record.carried_squirrel_familiarity,
                record.memory_caution_multiplier,
                record.score_formula_version,
                record.score_total,
                record.score_human_access,
                record.score_pigeon_feeding,
                record.score_squirrel_feeding,
                record.score_fox_feeding,
                record.human_connected,
                record.animal_reachable,
                record.food_hotspot_valid,
                record.changes_used,
                record.changes_allowed,
                record.human_trips,
                record.pigeon_count,
                record.squirrel_count,
                record.fox_count,
                record.pigeon_feed_events,
                record.squirrel_feed_events,
                record.fox_feed_events,
                record.animal_avoidance_events,
                record.note,
            };
            return string.Join(",", Array.ConvertAll(values, FormatCsvValue));
        }

        public static string SanitizeFileComponent(string value)
        {
            string source = string.IsNullOrWhiteSpace(value) ? "unnamed-session" : value.Trim();
            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder result = new StringBuilder(source.Length);
            foreach (char character in source)
            {
                result.Append(Array.IndexOf(invalid, character) >= 0 ? '_' : character);
            }
            return result.ToString();
        }

        private static string FormatCsvValue(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            string text;
            if (value is bool boolean)
            {
                text = boolean ? "true" : "false";
            }
            else if (value is IFormattable formattable)
            {
                text = formattable.ToString(null, CultureInfo.InvariantCulture);
            }
            else
            {
                text = value.ToString();
            }

            if (text.Contains(",") || text.Contains("\"") || text.Contains("\r") || text.Contains("\n"))
            {
                return $"\"{text.Replace("\"", "\"\"")}\"";
            }
            return text;
        }
    }

    public sealed class ResearchLogWriter
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
        };

        public ResearchLogWriter(string outputRoot, string sessionId)
        {
            if (string.IsNullOrWhiteSpace(outputRoot))
            {
                throw new ArgumentException("A research log output directory is required.", nameof(outputRoot));
            }

            string safeSession = ResearchLogFormatter.SanitizeFileComponent(sessionId);
            SessionDirectory = Path.Combine(Path.GetFullPath(outputRoot), safeSession);
            JsonlPath = Path.Combine(SessionDirectory, "events.jsonl");
            CsvPath = Path.Combine(SessionDirectory, "events.csv");
        }

        public string SessionDirectory { get; }
        public string JsonlPath { get; }
        public string CsvPath { get; }

        public void Append(ResearchLogRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            Directory.CreateDirectory(SessionDirectory);
            string json = JsonConvert.SerializeObject(record, Formatting.None, JsonSettings);
            File.AppendAllText(JsonlPath, json + Environment.NewLine, Encoding.UTF8);
            if (!File.Exists(CsvPath))
            {
                File.WriteAllText(CsvPath, ResearchLogFormatter.CsvHeader + Environment.NewLine, Encoding.UTF8);
            }
            File.AppendAllText(CsvPath, ResearchLogFormatter.ToCsvRow(record) + Environment.NewLine, Encoding.UTF8);
        }
    }
}
