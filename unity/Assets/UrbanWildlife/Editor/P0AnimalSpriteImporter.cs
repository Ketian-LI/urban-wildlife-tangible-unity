using UnityEditor;

namespace UrbanWildlife.EditorTools
{
    public sealed class P0AnimalSpriteImporter : AssetPostprocessor
    {
        private const string VisualAssetPrefix = "Assets/Resources/UrbanWildlife/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(VisualAssetPrefix) || !assetPath.EndsWith(".png"))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.maxTextureSize = assetPath.Contains("/Environment/") ? 2048 : 1024;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }
}
