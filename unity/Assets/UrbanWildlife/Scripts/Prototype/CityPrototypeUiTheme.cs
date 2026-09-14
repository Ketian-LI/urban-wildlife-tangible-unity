using System;
using System.Linq;
using UnityEngine;

namespace UrbanWildlife.Prototype
{
    /// <summary>
    /// Typography, colour and spacing tokens for the City Prototype IMGUI layer.
    /// Uses the bundled OFL-licensed Nunito asset, with Unity's runtime font as a
    /// defensive fallback if the resource is ever missing.
    /// </summary>
    internal static class CityPrototypeUiTheme
    {
        public const float ReferenceHeight = 1080f;

        public const int DisplayFontSize = 30;
        public const int SectionFontSize = 15;
        public const int EyebrowFontSize = 11;
        public const int MetricFontSize = 16;
        public const int BodyFontSize = 14;
        public const int CaptionFontSize = 12;
        public const int ButtonFontSize = 14;

        public const int SpaceXs = 4;
        public const int SpaceSm = 8;
        public const int SpaceMd = 12;
        public const int SpaceLg = 20;
        public const int SpaceXl = 28;

        public static readonly Color Panel = Hex(0xF8, 0xF6, 0xEF);
        public static readonly Color Ink = Hex(0x20, 0x34, 0x3F);
        public static readonly Color InkSecondary = Hex(0x50, 0x60, 0x66);
        public static readonly Color InkMuted = Hex(0x5D, 0x6A, 0x6D);
        public static readonly Color Accent = Hex(0x22, 0x69, 0x6D);
        public static readonly Color Button = Hex(0xDC, 0xEC, 0xEE);
        public static readonly Color ButtonHover = Hex(0xC7, 0xE1, 0xE4);
        public static readonly Color ButtonPressed = Hex(0xAF, 0xD3, 0xD7);
        public static readonly Color ButtonText = Hex(0x17, 0x39, 0x42);
        private static Font runtimeFontRegular;
        private static Font runtimeFontBold;
        private static Font runtimeFontCjkRegular;
        private static Font runtimeFontCjkBold;

        public static Font LoadRuntimeFont(bool bold = false, bool preferCjk = false)
        {
            Font runtimeFont = preferCjk
                ? (bold ? runtimeFontCjkBold : runtimeFontCjkRegular)
                : (bold ? runtimeFontBold : runtimeFontRegular);
            if (runtimeFont == null)
            {
                if (preferCjk)
                {
                    runtimeFont = CreateCjkFont(bold);
                }
                if (runtimeFont == null)
                {
                    string resource = bold
                        ? "UrbanWildlife/Fonts/Nunito-Bold"
                        : "UrbanWildlife/Fonts/Nunito-Regular";
                    runtimeFont = Resources.Load<Font>(resource) ??
                                  Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                if (preferCjk)
                {
                    if (bold) runtimeFontCjkBold = runtimeFont;
                    else runtimeFontCjkRegular = runtimeFont;
                }
                else
                {
                    if (bold) runtimeFontBold = runtimeFont;
                    else runtimeFontRegular = runtimeFont;
                }
            }
            return runtimeFont;
        }

        private static Font CreateCjkFont(bool bold)
        {
            string[] preferred = bold
                ? new[]
                {
                    "Microsoft YaHei UI Bold",
                    "Microsoft YaHei Bold",
                    "PingFang SC Semibold",
                    "Noto Sans CJK SC Bold",
                    "Noto Sans SC Bold",
                }
                : new[]
                {
                    "Microsoft YaHei UI",
                    "Microsoft YaHei",
                    "PingFang SC",
                    "Noto Sans CJK SC",
                    "Noto Sans SC",
                    "Arial Unicode MS",
                };
            string[] installed = Font.GetOSInstalledFontNames();
            string selected = preferred.FirstOrDefault(candidate =>
                installed.Any(font => string.Equals(font, candidate, StringComparison.OrdinalIgnoreCase)));
            return string.IsNullOrEmpty(selected)
                ? null
                : Font.CreateDynamicFontFromOSFont(selected, 18);
        }

        public static float ScaleForScreen(int screenHeight)
        {
            return Mathf.Clamp(screenHeight / ReferenceHeight, 0.85f, 1.25f);
        }

        public static int ScaledFontSize(int referenceSize, float scale)
        {
            return Mathf.Max(10, Mathf.RoundToInt(referenceSize * scale));
        }

        public static int ScaledPixel(int referencePixels, float scale)
        {
            return Mathf.Max(1, Mathf.RoundToInt(referencePixels * scale));
        }

        private static Color Hex(byte red, byte green, byte blue)
        {
            return new Color32(red, green, blue, 255);
        }
    }
}
