using System;
using System.Linq;
using Newtonsoft.Json;

namespace UrbanWildlife.Input
{
    public static class CityTokenScanReader
    {
        public static bool TryParseAndValidate(
            string json,
            long lastAcceptedTimestampMs,
            out CityTokenScanPacket packet,
            out string error)
        {
            packet = null;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "City scan JSON is empty.";
                return false;
            }

            try
            {
                packet = JsonConvert.DeserializeObject<CityTokenScanPacket>(json);
            }
            catch (JsonException exception)
            {
                error = $"City scan JSON is invalid: {exception.Message}";
                return false;
            }

            if (packet == null)
            {
                error = "City scan JSON produced no object.";
                return false;
            }
            if (packet.schema_version != CityTokenScanContract.SchemaVersion ||
                packet.packet_type != CityTokenScanContract.PacketType)
            {
                error = $"Unsupported city scan contract: {packet.schema_version}/{packet.packet_type}.";
                return false;
            }
            if (packet.timestamp_ms <= lastAcceptedTimestampMs ||
                string.IsNullOrWhiteSpace(packet.timestamp_utc))
            {
                error = "City scan timestamp is missing or is not newer than the accepted scan.";
                return false;
            }
            if (!DateTimeOffset.TryParse(packet.timestamp_utc, out _))
            {
                error = "City scan UTC timestamp is not a valid date-time.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(packet.scan_id) ||
                string.IsNullOrWhiteSpace(packet.session_id) || packet.development_phase < 1)
            {
                error = "City scan identity or development phase is invalid.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(packet.calibration_id) ||
                packet.coordinate_system == null || packet.coordinate_system.origin != "top_left" ||
                packet.coordinate_system.x_axis != "right" ||
                packet.coordinate_system.y_axis != "down" ||
                string.IsNullOrWhiteSpace(packet.coordinate_system.unity_plane_mapping) ||
                packet.coordinate_system.range == null || packet.coordinate_system.range.Length != 2 ||
                !Approximately(packet.coordinate_system.range[0], 0f) ||
                !Approximately(packet.coordinate_system.range[1], 1f))
            {
                error = "City scan must use a calibrated top-left normalized coordinate system.";
                return false;
            }
            if (packet.capture == null || !packet.capture.stable)
            {
                error = "City scan frame is not stable and cannot enter Preview.";
                return false;
            }
            bool cameraRecognition = packet.recognition != null &&
                                     (packet.recognition.marker_backend == "aruco" ||
                                      packet.recognition.marker_backend == "apriltag");
            bool desktopRecognition = packet.recognition != null &&
                                      packet.recognition.marker_backend == "desktop_pointer" &&
                                      packet.capture.mode == "desktop_pointer";
            if ((!cameraRecognition && !desktopRecognition) ||
                string.IsNullOrWhiteSpace(packet.recognition.marker_family) ||
                string.IsNullOrWhiteSpace(packet.recognition.token_config_version))
            {
                error = "City token recognition metadata is missing or unsupported.";
                return false;
            }

            CityTokenState[] tokens = packet.token_states ?? Array.Empty<CityTokenState>();
            if (tokens.Any(token => token == null))
            {
                error = "City scan contains a null Token state.";
                return false;
            }
            int[] duplicateIds = tokens.GroupBy(token => token.id)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .OrderBy(id => id)
                .ToArray();
            if (duplicateIds.Length > 0)
            {
                error = $"City scan contains duplicate Token IDs [{string.Join(",", duplicateIds)}].";
                return false;
            }

            foreach (CityTokenState token in tokens)
            {
                if (!CityTokenInventory.MatchesInventory(token, out error))
                {
                    return false;
                }
                if (!UnitValue(token.x_norm) || !UnitValue(token.y_norm) ||
                    !Finite(token.rotation_deg) || token.rotation_deg < -180f ||
                    token.rotation_deg > 180f || !UnitValue(token.confidence))
                {
                    error = $"City Token {token.id} contains invalid position, rotation or confidence.";
                    return false;
                }
            }

            foreach (CityPhysicalTokenType type in Enum.GetValues(typeof(CityPhysicalTokenType)))
            {
                int detected = tokens.Count(token => token.type == type);
                if (detected > CityTokenInventory.MaximumFor(type))
                {
                    error = $"City scan exceeds the physical inventory for {type}.";
                    return false;
                }
            }
            return true;
        }

        private static bool Approximately(float first, float second)
        {
            return Math.Abs(first - second) <= 0.0001f;
        }

        private static bool UnitValue(float value)
        {
            return value >= 0f && value <= 1f && Finite(value);
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
