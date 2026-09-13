using System;
using System.IO;
using UnityEngine;

namespace UrbanWildlife.Input
{
    public static class CityTokenScanFileSource
    {
        public const string DefaultProjectRelativePath =
            "../../data/raw/city-token-scans/latest_city_scan.json";

        public static bool TryRead(
            string configuredPath,
            out string json,
            out string resolvedPath,
            out string error)
        {
            json = string.Empty;
            error = string.Empty;
            resolvedPath = Resolve(configuredPath);
            if (!File.Exists(resolvedPath))
            {
                error = $"No camera scan is ready yet: {resolvedPath}";
                return false;
            }
            try
            {
                json = File.ReadAllText(resolvedPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    error = "The latest camera scan file is empty.";
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                error = $"Could not read the latest camera scan: {exception.Message}";
                return false;
            }
        }

        public static string Resolve(string configuredPath)
        {
            string path = string.IsNullOrWhiteSpace(configuredPath)
                ? DefaultProjectRelativePath
                : configuredPath;
            return Path.IsPathRooted(path)
                ? Path.GetFullPath(path)
                : Path.GetFullPath(Path.Combine(Application.dataPath, path));
        }
    }
}
