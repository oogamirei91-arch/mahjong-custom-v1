#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using Mahjong.Procedural;

namespace Mahjong.Editor
{
    /// <summary>
    /// TileAtlasExporter: Alat Editor untuk merender dan menyimpan Texture Atlas / Sprite Sheet Mahjong 3D
    /// ke dalam folder Assets (Assets/Resources/CustomMahjongAtlas.png dan Assets/Textures/Mahjong_Tiles_3D_Atlas.png).
    /// </summary>
    [InitializeOnLoad]
    public static class TileAtlasExporter
    {
        static TileAtlasExporter()
        {
            EditorApplication.delayCall += CheckAndBakeAtlasAsset;
        }

        private static void CheckAndBakeAtlasAsset()
        {
            string resPath = Path.Combine(Application.dataPath, "Resources", "CustomMahjongAtlas.png");
            if (!File.Exists(resPath))
            {
                BakeAndSaveMahjongAtlasPNG();
            }
        }

        [MenuItem("Mahjong VIP/🖼️ Export & Simpan Sprite Sheet Mahjong PNG ke Assets", false, 2)]
        public static void BakeAndSaveMahjongAtlasPNG()
        {
            // Buat instance generator atlas prosedural
            GameObject tempObj = new GameObject("Temp_Atlas_Baker");
            ProceduralTileAtlas atlasBaker = tempObj.AddComponent<ProceduralTileAtlas>();
            atlasBaker.GenerateFullAtlas();

            Texture2D tex = atlasBaker.GeneratedAtlas;
            if (tex != null)
            {
                byte[] pngBytes = tex.EncodeToPNG();

                // 1. Simpan ke Assets/Resources/CustomMahjongAtlas.png (untuk auto-load saat runtime)
                string resourcesDir = Path.Combine(Application.dataPath, "Resources");
                if (!Directory.Exists(resourcesDir)) Directory.CreateDirectory(resourcesDir);
                string resPath = Path.Combine(resourcesDir, "CustomMahjongAtlas.png");
                File.WriteAllBytes(resPath, pngBytes);

                // 2. Simpan ke Assets/Textures/Mahjong_Tiles_3D_Atlas.png (untuk asset library & kustomisasi)
                string texturesDir = Path.Combine(Application.dataPath, "Textures");
                if (!Directory.Exists(texturesDir)) Directory.CreateDirectory(texturesDir);
                string texPath = Path.Combine(texturesDir, "Mahjong_Tiles_3D_Atlas.png");
                File.WriteAllBytes(texPath, pngBytes);

                Object.DestroyImmediate(tempObj);

                AssetDatabase.Refresh();

                // Konfigurasi Texture Importer agar kualitas Ultra-HD tajam tanpa kompresi buram
                ConfigureTextureImport("Assets/Resources/CustomMahjongAtlas.png");
                ConfigureTextureImport("Assets/Textures/Mahjong_Tiles_3D_Atlas.png");

                Debug.Log($"[TileAtlasExporter] Berhasil mengekspor & menyimpan Sprite Sheet Mahjong Ultra-HD (2048x2048) ke:\n1. Assets/Resources/CustomMahjongAtlas.png\n2. Assets/Textures/Mahjong_Tiles_3D_Atlas.png");
            }
            else
            {
                Object.DestroyImmediate(tempObj);
                Debug.LogError("[TileAtlasExporter] Gagal merender atlas texture!");
            }
        }

        private static void ConfigureTextureImport(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
