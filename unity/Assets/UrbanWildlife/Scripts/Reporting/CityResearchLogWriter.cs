using System;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UrbanWildlife.Logging;

namespace UrbanWildlife.Reporting
{
    public sealed class CityResearchLogWriter
    {
        public const string CsvHeader =
            "timestamp_utc,session_id,event_type,subject_id,cause_id,cycle_index,phase,time_block," +
            "development_points,city_balance_total,development,accessibility,waste_management," +
            "habitat_connectivity,wildlife_safety,waste_demand,waste_capacity,overflow_active," +
            "active_wildlife,feeding_events,migration_events,roadkill_events," +
            "human_trace_points,animal_trace_points,note";

        public CityResearchLogWriter(string outputRoot, string sessionId)
        {
            if (string.IsNullOrWhiteSpace(outputRoot))
            {
                throw new ArgumentException("A city log output directory is required.", nameof(outputRoot));
            }
            string safeSession = ResearchLogFormatter.SanitizeFileComponent(sessionId);
            SessionDirectory = Path.Combine(Path.GetFullPath(outputRoot), safeSession);
            JsonlPath = Path.Combine(SessionDirectory, "city-events.jsonl");
            CsvPath = Path.Combine(SessionDirectory, "city-events.csv");
        }

        public string SessionDirectory { get; }
        public string JsonlPath { get; }
        public string CsvPath { get; }

        public void Append(CityResearchLogRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }
            Directory.CreateDirectory(SessionDirectory);
            File.AppendAllText(
                JsonlPath,
                JsonConvert.SerializeObject(record, Formatting.None) + Environment.NewLine,
                Encoding.UTF8);
            if (!File.Exists(CsvPath))
            {
                File.WriteAllText(CsvPath, CsvHeader + Environment.NewLine, Encoding.UTF8);
            }
            object[] values =
            {
                record.timestamp_utc, record.session_id, record.event_type, record.subject_id,
                record.cause_id, record.cycle_index, record.phase, record.time_block,
                record.development_points, record.city_balance_total, record.development,
                record.accessibility, record.waste_management, record.habitat_connectivity,
                record.wildlife_safety, record.waste_demand, record.waste_capacity,
                record.overflow_active, record.active_wildlife, record.feeding_events,
                record.migration_events, record.roadkill_events, record.human_trace_points,
                record.animal_trace_points, record.note,
            };
            File.AppendAllText(CsvPath, string.Join(",", Array.ConvertAll(values, Csv)) +
                Environment.NewLine, Encoding.UTF8);
        }

        private static string Csv(object value)
        {
            if (value == null) return string.Empty;
            string text = value is bool flag
                ? (flag ? "true" : "false")
                : value is IFormattable formattable
                    ? formattable.ToString(null, CultureInfo.InvariantCulture)
                    : value.ToString();
            return text.Contains(",") || text.Contains("\"") || text.Contains("\n")
                ? $"\"{text.Replace("\"", "\"\"")}\""
                : text;
        }
    }
}
