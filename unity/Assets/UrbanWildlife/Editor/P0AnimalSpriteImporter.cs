using UnityEditor;

namespace UrbanWildlife.EditorTools
{
    public sealed class P0AnimalSpriteImporter : AssetPostprocessor
    {
        private const string AnimalAssetPrefix = "Assets/Resources/UrbanWildlife/Animals/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(AnimalAssetPrefix))
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
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }
}
