using UnityEngine;

namespace UrbanWildlife.Presentation
{
    public static class SpritePaletteMaterial
    {
        public const string ResourcePath = "UrbanWildlife/Shaders/palette-harmonized-sprite";
        public const string ShaderName = "UrbanWildlife/PaletteHarmonizedSprite";

        private static readonly Color ParkAmbientTint = new Color(0.9f, 0.95f, 0.82f, 1f);

        public static Material Create(float saturation, float brightness, float ambientBlend)
        {
            Shader shader = Resources.Load<Shader>(ResourcePath) ?? Shader.Find(ShaderName);
            if (shader == null)
            {
                return null;
            }

            Material material = new Material(shader)
            {
                name = "Runtime Park Palette Material",
                hideFlags = HideFlags.DontSave,
            };
            material.SetFloat("_Saturation", Mathf.Clamp01(saturation));
            material.SetFloat("_Brightness", Mathf.Clamp(brightness, 0.5f, 1.25f));
            material.SetFloat("_AmbientBlend", Mathf.Clamp01(ambientBlend));
            material.SetColor("_AmbientTint", ParkAmbientTint);
            return material;
        }
    }
}
