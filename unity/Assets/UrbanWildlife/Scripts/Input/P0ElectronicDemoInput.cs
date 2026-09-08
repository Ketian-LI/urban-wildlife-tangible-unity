using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UrbanWildlife.Cycle;

namespace UrbanWildlife.Input
{
    [RequireComponent(typeof(LayoutPacketReader), typeof(P0CycleController))]
    public sealed class P0ElectronicDemoInput : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Privacy-safe fixture used before the physical board is available.")]
        private string fixturePath = "../../data/examples/s001_valid_human_demo.json";

        private LayoutPacketReader reader;
        private P0CycleController cycle;
        private string demoSessionId;

        public string LastMessage { get; private set; } = string.Empty;

        private void Awake()
        {
            reader = GetComponent<LayoutPacketReader>();
            cycle = GetComponent<P0CycleController>();
            demoSessionId = $"electronic-demo-{DateTimeOffset.Now:yyyyMMdd-HHmmss}";
        }

        public bool TryPrepareCurrentCycle()
        {
            if (cycle.Phase != P0Phase.Plan)
            {
                LastMessage = "Electronic layout can only be prepared during Plan.";
                return false;
            }

            bool prepared = TryCreatePacket(
                ResolvePath(fixturePath),
                reader.ResolvedPacketPath,
                demoSessionId,
                cycle.CycleIndex,
                reader.LastAcceptedTimestampMs,
                out LayoutPacket packet,
                out string error);
            LastMessage = prepared
                ? $"Electronic layout ready for cycle {packet.cycle_index + 1}. Confirm when ready."
                : $"Electronic layout failed: {error}";
            if (prepared)
            {
                Debug.Log(LastMessage, this);
            }
            else
            {
                Debug.LogWarning(LastMessage, this);
            }
            return prepared;
        }

        public static bool TryCreatePacket(
            string sourceFixturePath,
            string destinationPath,
            string sessionId,
            int cycleIndex,
            long minimumTimestampMs,
            out LayoutPacket packet,
            out string error)
        {
            packet = null;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                error = "A non-empty demo session ID is required.";
                return false;
            }
            if (cycleIndex < 0)
            {
                error = "Cycle index cannot be negative.";
                return false;
            }

            try
            {
                if (!File.Exists(sourceFixturePath))
                {
                    error = $"Demo fixture does not exist: {sourceFixturePath}";
                    return false;
                }

                LayoutPacket source = JsonConvert.DeserializeObject<LayoutPacket>(
                    File.ReadAllText(sourceFixturePath));
                if (source == null)
                {
                    error = "Demo fixture contains no layout packet.";
                    return false;
                }

                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                source.timestamp_ms = Math.Max(now, minimumTimestampMs + 1);
                source.timestamp_utc = DateTimeOffset
                    .FromUnixTimeMilliseconds(source.timestamp_ms)
                    .ToString("O");
                source.session_id = sessionId;
                source.cycle_index = cycleIndex;
                source.calibration_id = "electronic-demo-no-physical-calibration";
                if (source.capture != null)
                {
                    source.capture.mode = "image_input";
                    source.capture.stable = true;
                }

                string json = JsonConvert.SerializeObject(source, Formatting.Indented);
                if (!LayoutPacketReader.TryParseAndValidate(
                        json,
                        minimumTimestampMs,
                        out packet,
                        out error))
                {
                    return false;
                }

                string fullDestination = Path.GetFullPath(destinationPath);
                string directory = Path.GetDirectoryName(fullDestination);
                if (string.IsNullOrWhiteSpace(directory))
                {
                    error = "Demo output directory could not be resolved.";
                    packet = null;
                    return false;
                }

                Directory.CreateDirectory(directory);
                string temporaryPath = fullDestination + ".tmp";
                File.WriteAllText(temporaryPath, json);
                if (File.Exists(fullDestination))
                {
                    File.Replace(temporaryPath, fullDestination, null);
                }
                else
                {
                    File.Move(temporaryPath, fullDestination);
                }
                return true;
            }
            catch (JsonException exception)
            {
                error = $"Demo fixture JSON is invalid: {exception.Message}";
            }
            catch (IOException exception)
            {
                error = $"Could not prepare demo packet: {exception.Message}";
            }
            catch (UnauthorizedAccessException exception)
            {
                error = $"Could not prepare demo packet: {exception.Message}";
            }

            packet = null;
            return false;
        }

        private static string ResolvePath(string configuredPath)
        {
            return Path.IsPathRooted(configuredPath)
                ? Path.GetFullPath(configuredPath)
                : Path.GetFullPath(Path.Combine(Application.dataPath, configuredPath));
        }
    }
}
