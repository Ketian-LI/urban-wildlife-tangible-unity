using UnityEngine;

namespace UrbanWildlife.Presentation
{
    public static class WoodenTokenPresentation
    {
        public const string ResourcePath = "UrbanWildlife/Shaders/wooden-token-silhouette";
        public const string ShaderName = "UrbanWildlife/WoodenTokenSilhouette";
        public const float RimScale = 1.105f;
        public const float ShadowScale = 1.145f;
        public const float ShadowOffsetX = 0.035f;
        public const float ShadowOffsetY = -0.032f;

        private static readonly Color WoodLight = new Color(0.78f, 0.52f, 0.25f, 1f);
        private static readonly Color WoodDark = new Color(0.25f, 0.105f, 0.035f, 1f);
        private static readonly Color ShadowLight = new Color(0.07f, 0.055f, 0.038f, 1f);
        private static readonly Color ShadowDark = new Color(0.018f, 0.014f, 0.01f, 1f);

        public static Material CreateRimMaterial()
        {
            return CreateMaterial("Runtime wooden token rim", WoodLight, WoodDark, 1f, 48f);
        }

        public static Material CreateShadowMaterial()
        {
            return CreateMaterial("Runtime wooden token shadow", ShadowLight, ShadowDark, 0.5f, 36f);
        }

        public static void Attach(
            Transform artwork,
            SpriteRenderer source,
            Material rimMaterial,
            Material shadowMaterial,
            out SpriteRenderer rim,
            out SpriteRenderer shadow)
        {
            rim = null;
            shadow = null;
            if (artwork == null || source == null || rimMaterial == null || shadowMaterial == null)
            {
                return;
            }

            GameObject shadowObject = new GameObject("Wooden token shadow");
            shadowObject.transform.SetParent(artwork, false);
            shadowObject.transform.localPosition = new Vector3(ShadowOffsetX, ShadowOffsetY, 0f);
            shadowObject.transform.localScale = Vector3.one * ShadowScale;
            shadow = shadowObject.AddComponent<SpriteRenderer>();
            shadow.sprite = source.sprite;
            shadow.sortingOrder = source.sortingOrder - 2;
            shadow.sharedMaterial = shadowMaterial;

            GameObject rimObject = new GameObject("Wooden token rim");
            rimObject.transform.SetParent(artwork, false);
            rimObject.transform.localScale = Vector3.one * RimScale;
            rim = rimObject.AddComponent<SpriteRenderer>();
            rim.sprite = source.sprite;
            rim.sortingOrder = source.sortingOrder - 1;
            rim.sharedMaterial = rimMaterial;
        }

        public static void Sync(
            SpriteRenderer source,
            SpriteRenderer rim,
            SpriteRenderer shadow)
        {
            if (source == null)
            {
                return;
            }

            if (rim != null)
            {
                rim.sprite = source.sprite;
                rim.flipX = source.flipX;
                rim.enabled = source.enabled;
            }
            if (shadow != null)
            {
                shadow.sprite = source.sprite;
                shadow.flipX = source.flipX;
                shadow.enabled = source.enabled;
            }
        }

        private static Material CreateMaterial(
            string materialName,
            Color light,
            Color dark,
            float opacity,
            float grainScale)
        {
            Shader shader = Resources.Load<Shader>(ResourcePath) ?? Shader.Find(ShaderName);
            if (shader == null)
            {
                return null;
            }

            Material material = new Material(shader)
            {
                name = materialName,
                hideFlags = HideFlags.DontSave,
            };
            material.SetColor("_WoodLight", light);
            material.SetColor("_WoodDark", dark);
            material.SetFloat("_Opacity", Mathf.Clamp01(opacity));
            material.SetFloat("_GrainScale", Mathf.Max(1f, grainScale));
            return material;
        }
    }
}
