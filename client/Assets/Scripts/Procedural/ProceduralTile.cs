using UnityEngine;
using System.Collections;

namespace Mahjong.Procedural
{
    /// <summary>
    /// ProceduralTile: Mengontrol visual dan perilaku 3D satu Ubin Mahjong.
    /// Dilengkapi mesh dual-layer (Wajah Gading Pearl di depan dengan simbol Atlas, Giok Hijau di belakang),
    /// animasi hover saat disentuh di layar HP, dan efek lemparan ubin (discard drop).
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider))]
    public class ProceduralTile : MonoBehaviour
    {
        [Header("Identitas Tile")]
        public int tileId;          // 0 - 143
        public int suit;            // 0: Bamboo, 1: Dot, 2: Character, 3: Wind, 4: Dragon, 5: Flower, 6: Season
        public int value;           // 1 - 9
        public bool isBonus;
        public string tileName;

        [Header("Dimensi Ubin 3D (Rasio Standar Mahjong)")]
        public float tileWidth = 0.032f;    // Lebar X (3.2 cm)
        public float tileHeight = 0.042f;   // Panjang Y (4.2 cm)
        public float tileThickness = 0.022f;// Tebal Z (2.2 cm)

        [Header("State Interaksi")]
        public bool isSelected = false;
        public bool isInteractive = true;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private BoxCollider boxCollider;

        private Vector3 defaultLocalPos;
        private Coroutine activeAnimCoroutine;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            boxCollider = GetComponent<BoxCollider>();
            GenerateTileMesh();
        }

        public void Initialize(int id, int suitVal, int numVal, bool bonus, string name)
        {
            this.tileId = id;
            this.suit = suitVal;
            this.value = numVal;
            this.isBonus = bonus;
            this.tileName = name;
            gameObject.name = $"Tile_{id}_{name}";
            SetupMaterials();
            ApplyTileUV();
        }

        /// <summary>
        /// Membuat mesh 3D kotak ubin dengan 2 material (0: Depan Wajah Ivory bergambar, 1: Belakang Giok).
        /// </summary>
        [ContextMenu("Regenerate Tile Mesh")]
        public void GenerateTileMesh()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (boxCollider == null) boxCollider = GetComponent<BoxCollider>();

            float hx = tileWidth * 0.5f;
            float hy = tileHeight * 0.5f;
            float hz = tileThickness * 0.5f;

            Mesh mesh = new Mesh { name = "Procedural_Tile_Mesh" };

            // 24 Vertices untuk kubus dengan mapping terpisah tiap sisi
            Vector3[] vertices = new Vector3[]
            {
                // Front Face (Z +) -> Wajah Ubin Bergambar (Menghadap Pemain)
                new Vector3(-hx, -hy,  hz), new Vector3( hx, -hy,  hz),
                new Vector3( hx,  hy,  hz), new Vector3(-hx,  hy,  hz),

                // Back Face (Z -) -> Punggung Giok Zamrud
                new Vector3( hx, -hy, -hz), new Vector3(-hx, -hy, -hz),
                new Vector3(-hx,  hy, -hz), new Vector3( hx,  hy, -hz),

                // Top Face (Y +)
                new Vector3(-hx,  hy,  hz), new Vector3( hx,  hy,  hz),
                new Vector3( hx,  hy, -hz), new Vector3(-hx,  hy, -hz),

                // Bottom Face (Y -)
                new Vector3(-hx, -hy, -hz), new Vector3( hx, -hy, -hz),
                new Vector3( hx, -hy,  hz), new Vector3(-hx, -hy,  hz),

                // Left Face (X -)
                new Vector3(-hx, -hy, -hz), new Vector3(-hx, -hy,  hz),
                new Vector3(-hx,  hy,  hz), new Vector3(-hx,  hy, -hz),

                // Right Face (X +)
                new Vector3( hx, -hy,  hz), new Vector3( hx, -hy, -hz),
                new Vector3( hx,  hy, -hz), new Vector3( hx,  hy,  hz)
            };

            Vector2[] uvs = new Vector2[24];
            // Front Face UV (0-3)
            uvs[0] = new Vector2(0, 0); uvs[1] = new Vector2(1, 0);
            uvs[2] = new Vector2(1, 1); uvs[3] = new Vector2(0, 1);

            for (int i = 4; i < 24; i += 4)
            {
                uvs[i]   = new Vector2(0, 0);
                uvs[i+1] = new Vector2(1, 0);
                uvs[i+2] = new Vector2(1, 1);
                uvs[i+3] = new Vector2(0, 1);
            }

            // Submesh 0: Front Face (Ivory Pearl bertekstur atlas)
            int[] frontTriangles = new int[] { 0, 2, 1, 0, 3, 2 };

            // Submesh 1: Sisi Lainnya (Jade Green & Body)
            int[] jadeTriangles = new int[]
            {
                // Back
                4, 6, 5, 4, 7, 6,
                // Top
                8, 10, 9, 8, 11, 10,
                // Bottom
                12, 14, 13, 12, 15, 14,
                // Left
                16, 18, 17, 16, 19, 18,
                // Right
                20, 22, 21, 20, 23, 22
            };

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.subMeshCount = 2;
            mesh.SetTriangles(frontTriangles, 0);
            mesh.SetTriangles(jadeTriangles, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshFilter.sharedMesh = mesh;
            boxCollider.size = new Vector3(tileWidth * 1.2f, tileHeight * 1.2f, tileThickness * 2.0f);
            boxCollider.center = Vector3.zero;

            SetupMaterials();
        }

        public void SetupMaterials()
        {
            if (meshRenderer == null) return;

            Texture2D atlas = ProceduralTileAtlas.Instance != null ? ProceduralTileAtlas.Instance.GeneratedAtlas : null;
            if (atlas == null && ProceduralTileAtlas.Instance != null)
            {
                ProceduralTileAtlas.Instance.GenerateFullAtlas();
                atlas = ProceduralTileAtlas.Instance.GeneratedAtlas;
            }

            // Material Wajah: Pearl Ivory Putih dengan Tekstur Atlas
            Material frontMat = new Material(Shader.Find("Standard"))
            {
                name = "Mat_Tile_Front_Ivory",
                color = Color.white,
                mainTexture = atlas
            };
            frontMat.SetFloat("_Glossiness", 0.4f);

            // Material Punggung: Jade Green (Giok Hijau Zamrud Mewah)
            Material jadeMat = new Material(Shader.Find("Standard"))
            {
                name = "Mat_Tile_Back_Jade",
                color = new Color(0.04f, 0.42f, 0.24f)
            };
            jadeMat.SetFloat("_Glossiness", 0.75f);
            jadeMat.SetFloat("_Metallic", 0.15f);

            meshRenderer.sharedMaterials = new Material[] { frontMat, jadeMat };
        }

        /// <summary>
        /// Mengatur UV offset untuk menampilkan ikon ubin spesifik dari Texture Atlas 9x5.
        /// </summary>
        public void ApplyTileUV()
        {
            if (meshFilter == null || meshFilter.sharedMesh == null) return;

            int col = 0;
            int row = 0;

            // Suit: 0: Bamboo (Row 1), 1: Dot (Row 2), 2: Character/Wan (Row 0), 3: Wind (Row 3), 4: Dragon (Row 3), 5: Flower (Row 4), 6: Season (Row 4)
            switch (suit)
            {
                case 0: // Bamboo / Sou (1 - 9)
                    row = 1;
                    col = Mathf.Clamp(value - 1, 0, 8);
                    break;
                case 1: // Dots / Pin (1 - 9)
                    row = 2;
                    col = Mathf.Clamp(value - 1, 0, 8);
                    break;
                case 2: // Characters / Wan (1 - 9)
                    row = 0;
                    col = Mathf.Clamp(value - 1, 0, 8);
                    break;
                case 3: // Winds (East, South, West, North -> Col 0..3)
                    row = 3;
                    col = Mathf.Clamp(value - 1, 0, 3);
                    break;
                case 4: // Dragons (Red, Green, White -> Col 4..6)
                    row = 3;
                    col = Mathf.Clamp(3 + value, 4, 6);
                    break;
                case 5: // Flowers (1..4 -> Col 0..3)
                    row = 4;
                    col = Mathf.Clamp(value - 1, 0, 3);
                    break;
                case 6: // Seasons (1..4 -> Col 4..7)
                    row = 4;
                    col = Mathf.Clamp(3 + value, 4, 7);
                    break;
            }

            float cols = 9.0f;
            float rows = 5.0f;
            float uWidth = 1.0f / cols;
            float vHeight = 1.0f / rows;

            float uMin = col * uWidth;
            float uMax = uMin + uWidth;
            float vMax = 1.0f - (row * vHeight);
            float vMin = vMax - vHeight;

            // Buat copy mesh instance agar UV tiap ubin unik
            Mesh m = Instantiate(meshFilter.sharedMesh);
            Vector2[] uvs = m.uv;
            uvs[0] = new Vector2(uMin, vMin);
            uvs[1] = new Vector2(uMax, vMin);
            uvs[2] = new Vector2(uMax, vMax);
            uvs[3] = new Vector2(uMin, vMax);
            m.uv = uvs;
            meshFilter.sharedMesh = m;
        }

        /// <summary>
        /// Animasi ketika pemain memilih (tap) ubin: Ubin terangkat naik 2.2 cm.
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (isSelected == selected) return;
            isSelected = selected;

            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
            Vector3 targetPos = isSelected ? defaultLocalPos + new Vector3(0, 0.022f, 0) : defaultLocalPos;
            activeAnimCoroutine = StartCoroutine(AnimateLocalPosition(targetPos, 0.12f));
        }

        public void SaveDefaultPosition(Vector3 pos)
        {
            defaultLocalPos = pos;
            transform.localPosition = pos;
        }

        private IEnumerator AnimateLocalPosition(Vector3 target, float duration)
        {
            Vector3 start = transform.localPosition;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                transform.localPosition = Vector3.Lerp(start, target, t);
                yield return null;
            }
            transform.localPosition = target;
        }
    }
}
