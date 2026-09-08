using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace UrbanWildlife.Input
{
    public sealed class LayoutPacketReader : MonoBehaviour
    {
        private static readonly int[] RequiredP0TokenIds = { 10, 11, 12, 20, 21 };

        [SerializeField]
        [Tooltip("Path relative to the Unity Assets folder, or an absolute path.")]
        private string packetPath = "../../data/raw/layout-packets/latest_layout.json";

        [SerializeField]
        private bool loadOnStart;

        public event Action<LayoutPacket> LayoutAccepted;

        public LayoutPacket LatestPacket { get; private set; }
        public long LastAcceptedTimestampMs { get; private set; } = -1;
        public string LastError { get; private set; } = string.Empty;
        public string ResolvedPacketPath => ResolvePacketPath(packetPath);

        private void Start()
        {
            if (loadOnStart)
            {
                ConfirmLatestLayout();
            }
        }

        [ContextMenu("Confirm Latest Layout")]
        public void ConfirmLatestLayout()
        {
            if (!TryLoadLatest(out LayoutPacket packet, out string error))
            {
                Debug.LogWarning($"Layout packet was not accepted: {error}", this);
                return;
            }

            Debug.Log(
                $"Accepted layout {packet.session_id} cycle {packet.cycle_index} " +
                $"with {packet.tokens.Length} tokens and {packet.path.point_count} path points.",
                this);
        }

        public bool TryLoadLatest(out LayoutPacket packet, out string error)
        {
            packet = null;
            error = string.Empty;
            string path = ResolvedPacketPath;
            if (!File.Exists(path))
            {
                return Fail($"Packet file does not exist: {path}", out error);
            }

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception exception)
            {
                return Fail($"Could not read packet: {exception.Message}", out error);
            }

            if (!TryParseAndValidate(json, LastAcceptedTimestampMs, out packet, out error))
            {
                LastError = error;
                return false;
            }

            LatestPacket = packet;
            LastAcceptedTimestampMs = packet.timestamp_ms;
            LastError = string.Empty;
            LayoutAccepted?.Invoke(packet);
            return true;
        }

        public static bool TryParseAndValidate(
            string json,
            long lastAcceptedTimestampMs,
            out LayoutPacket packet,
            out string error)
        {
            packet = null;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Packet JSON is empty.";
                return false;
            }

            try
            {
                packet = JsonConvert.DeserializeObject<LayoutPacket>(json);
            }
            catch (JsonException exception)
            {
                error = $"Packet JSON is invalid: {exception.Message}";
                return false;
            }

            if (packet == null)
            {
                error = "Packet JSON produced no object.";
                return false;
            }

            if (packet.schema_version != "0.1" || packet.packet_type != "confirmed_layout")
            {
                error = $"Unsupported packet contract: {packet.schema_version}/{packet.packet_type}.";
                return false;
            }

            if (packet.timestamp_ms <= lastAcceptedTimestampMs)
            {
                error = $"Packet timestamp {packet.timestamp_ms} is not newer than {lastAcceptedTimestampMs}.";
                return false;
            }

            if (packet.recognition == null ||
                (packet.recognition.token_backend != "contour" && packet.recognition.token_backend != "aruco"))
            {
                error = "Packet recognition metadata is missing or unsupported.";
                return false;
            }

            if (!ValidateTokens(packet.tokens, out error) || !ValidatePath(packet.path, out error))
            {
                return false;
            }

            if (packet.validation == null ||
                !packet.validation.all_tokens_in_bounds ||
                !packet.validation.path_detected ||
                !packet.validation.path_continuous ||
                !packet.validation.all_required_tokens_detected)
            {
                error = "Python validation flags do not describe a complete valid P0 layout.";
                return false;
            }

            return true;
        }

        private static bool ValidateTokens(LayoutToken[] tokens, out string error)
        {
            error = string.Empty;
            if (tokens == null)
            {
                error = "Token array is missing.";
                return false;
            }

            Dictionary<int, int> counts = tokens
                .GroupBy(token => token.id)
                .ToDictionary(group => group.Key, group => group.Count());
            int[] missing = RequiredP0TokenIds.Where(id => !counts.ContainsKey(id)).ToArray();
            int[] duplicates = counts.Where(pair => pair.Value > 1).Select(pair => pair.Key).OrderBy(id => id).ToArray();
            if (missing.Length > 0 || duplicates.Length > 0)
            {
                error = $"Token set mismatch. Missing [{string.Join(",", missing)}], duplicate [{string.Join(",", duplicates)}].";
                return false;
            }

            foreach (LayoutToken token in tokens)
            {
                bool finite = IsFinite(token.x_norm) && IsFinite(token.y_norm) &&
                              IsFinite(token.angle_deg) && IsFinite(token.confidence);
                bool inRange = token.x_norm >= 0f && token.x_norm <= 1f &&
                               token.y_norm >= 0f && token.y_norm <= 1f &&
                               token.angle_deg >= -180f && token.angle_deg <= 180f &&
                               token.confidence >= 0f && token.confidence <= 1f;
                if (!finite || !inRange || !token.in_bounds)
                {
                    error = $"Token {token.id} contains invalid coordinates, angle, or confidence.";
                    return false;
                }
            }

            return true;
        }

        private static bool ValidatePath(LayoutPath path, out string error)
        {
            error = string.Empty;
            if (path == null || path.format != "polyline" || path.points_norm == null)
            {
                error = "Path payload is missing or unsupported.";
                return false;
            }

            if (!path.continuous || path.point_count != path.points_norm.Length || path.point_count < 2)
            {
                error = "Path must be continuous and contain at least two declared points.";
                return false;
            }

            for (int index = 0; index < path.points_norm.Length; index += 1)
            {
                float[] point = path.points_norm[index];
                if (point == null || point.Length != 2 || !IsFinite(point[0]) || !IsFinite(point[1]) ||
                    point[0] < 0f || point[0] > 1f || point[1] < 0f || point[1] > 1f)
                {
                    error = $"Path point {index} is invalid.";
                    return false;
                }
            }

            return true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private bool Fail(string message, out string error)
        {
            LastError = message;
            error = message;
            return false;
        }

        private static string ResolvePacketPath(string configuredPath)
        {
            if (Path.IsPathRooted(configuredPath))
            {
                return Path.GetFullPath(configuredPath);
            }

            return Path.GetFullPath(Path.Combine(Application.dataPath, configuredPath));
        }
    }
}
