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

        [Header("Dimensi Ubin 3D (Rasio Standar Mahjong HD)")]
        public float tileWidth = 0.040f;    // Lebar X (4.0 cm)
        public float tileHeight = 0.056f;   // Panjang Y (5.6 cm)
        public float tileThickness = 0.026f;// Tebal Z (2.6 cm)

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

            if (meshRenderer != null)
            {
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                meshRenderer.receiveShadows = true;
            }

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
        /// Membuat mesh 3D ubin dual-layer autentik:
        /// - Lapisan Depan (Submesh 0): Gading Pearl Ivory (Wajah bertanda + 4 tepi sisi depan).
        /// - Lapisan Belakang (Submesh 1): Giok Hijau Zamrud (Punggung giok + 4 tepi sisi belakang).
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
            float splitZ = hz * 0.15f; // Garis batas dua lapis (35% Gading Depan, 65% Giok Belakang)

            Mesh mesh = new Mesh { name = "Procedural_DualLayer_Tile_Mesh" };

            // 40 Vertices untuk membagi 2 layer secara sempurna
            Vector3[] vertices = new Vector3[40];

            // --- LAYER 1: GADING / IVORY DEPAN (Submesh 0) ---
            // Front Face (Z = hz)
            vertices[0] = new Vector3(-hx, -hy,  hz);
            vertices[1] = new Vector3( hx, -hy,  hz);
            vertices[2] = new Vector3( hx,  hy,  hz);
            vertices[3] = new Vector3(-hx,  hy,  hz);

            // Front Top (Y = hy)
            vertices[4] = new Vector3(-hx,  hy,  hz);
            vertices[5] = new Vector3( hx,  hy,  hz);
            vertices[6] = new Vector3( hx,  hy, splitZ);
            vertices[7] = new Vector3(-hx,  hy, splitZ);

            // Front Bottom (Y = -hy)
            vertices[8]  = new Vector3(-hx, -hy, splitZ);
            vertices[9]  = new Vector3( hx, -hy, splitZ);
            vertices[10] = new Vector3( hx, -hy,  hz);
            vertices[11] = new Vector3(-hx, -hy,  hz);

            // Front Left (X = -hx)
            vertices[12] = new Vector3(-hx, -hy, splitZ);
            vertices[13] = new Vector3(-hx, -hy,  hz);
            vertices[14] = new Vector3(-hx,  hy,  hz);
            vertices[15] = new Vector3(-hx,  hy, splitZ);

            // Front Right (X = hx)
            vertices[16] = new Vector3( hx, -hy,  hz);
            vertices[17] = new Vector3( hx, -hy, splitZ);
            vertices[18] = new Vector3( hx,  hy, splitZ);
            vertices[19] = new Vector3( hx,  hy,  hz);

            // --- LAYER 2: GIOK / JADE BELAKANG (Submesh 1) ---
            // Back Face (Z = -hz)
            vertices[20] = new Vector3( hx, -hy, -hz);
            vertices[21] = new Vector3(-hx, -hy, -hz);
            vertices[22] = new Vector3(-hx,  hy, -hz);
            vertices[23] = new Vector3( hx,  hy, -hz);

            // Back Top (Y = hy)
            vertices[24] = new Vector3(-hx,  hy, splitZ);
            vertices[25] = new Vector3( hx,  hy, splitZ);
            vertices[26] = new Vector3( hx,  hy, -hz);
            vertices[27] = new Vector3(-hx,  hy, -hz);

            // Back Bottom (Y = -hy)
            vertices[28] = new Vector3(-hx, -hy, -hz);
            vertices[29] = new Vector3( hx, -hy, -hz);
            vertices[30] = new Vector3( hx, -hy, splitZ);
            vertices[31] = new Vector3(-hx, -hy, splitZ);

            // Back Left (X = -hx)
            vertices[32] = new Vector3(-hx, -hy, -hz);
            vertices[33] = new Vector3(-hx, -hy, splitZ);
            vertices[34] = new Vector3(-hx,  hy, splitZ);
            vertices[35] = new Vector3(-hx,  hy, -hz);

            // Back Right (X = hx)
            vertices[36] = new Vector3( hx, -hy, splitZ);
            vertices[37] = new Vector3( hx, -hy, -hz);
            vertices[38] = new Vector3( hx,  hy, -hz);
            vertices[39] = new Vector3( hx,  hy, splitZ);

            Vector2[] uvs = new Vector2[40];
            for (int i = 0; i < 40; i += 4)
            {
                uvs[i]   = new Vector2(0, 0);
                uvs[i+1] = new Vector2(1, 0);
                uvs[i+2] = new Vector2(1, 1);
                uvs[i+3] = new Vector2(0, 1);
            }

            // Triangles Submesh 0 (Lapisan Gading Ivory)
            int[] frontTriangles = new int[]
            {
                // Front
                0, 2, 1, 0, 3, 2,
                // Front Top
                4, 6, 5, 4, 7, 6,
                // Front Bottom
                8, 10, 9, 8, 11, 10,
                // Front Left
                12, 14, 13, 12, 15, 14,
                // Front Right
                16, 18, 17, 16, 19, 18
            };

            // Triangles Submesh 1 (Lapisan Giok Jade)
            int[] jadeTriangles = new int[]
            {
                // Back
                20, 22, 21, 20, 23, 22,
                // Back Top
                24, 26, 25, 24, 27, 26,
                // Back Bottom
                28, 30, 29, 28, 31, 30,
                // Back Left
                32, 34, 33, 32, 35, 34,
                // Back Right
                36, 38, 37, 36, 39, 38
            };

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.subMeshCount = 2;
            mesh.SetTriangles(frontTriangles, 0);
            mesh.SetTriangles(jadeTriangles, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshFilter.sharedMesh = mesh;
            boxCollider.size = new Vector3(tileWidth * 1.15f, tileHeight * 1.15f, tileThickness * 2.0f);
            boxCollider.center = Vector3.zero;
        }

        private static Material s_frontMat;
        private static Material s_jadeMat;
        private static bool s_materialsInitialized = false;

        public static void ResetSharedMaterials()
        {
            s_frontMat = null;
            s_jadeMat = null;
            s_materialsInitialized = false;
        }

        public static void EnsureMaterials()
        {
            if (s_materialsInitialized && s_frontMat != null && s_jadeMat != null) return;

            Texture2D atlas = ProceduralTileAtlas.Instance != null ? ProceduralTileAtlas.Instance.GeneratedAtlas : null;
            if (atlas == null && ProceduralTileAtlas.Instance != null)
            {
                ProceduralTileAtlas.Instance.GenerateFullAtlas();
                atlas = ProceduralTileAtlas.Instance.GeneratedAtlas;
            }
            if (atlas == null)
            {
                atlas = Resources.Load<Texture2D>("CustomMahjongAtlas");
            }

            Shader standardShader = Shader.Find("Standard");
            if (standardShader == null) standardShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Mobile/Diffuse") ?? Shader.Find("Diffuse");

            // Material Wajah: Pearl Ivory Putih dengan Tekstur Atlas
            s_frontMat = new Material(standardShader)
            {
                name = "Mat_Tile_Front_Ivory_Shared",
                color = Color.white,
                mainTexture = atlas
            };
            s_frontMat.SetFloat("_Glossiness", 0.35f);

            // Material Punggung: Jade Green (Giok Hijau Zamrud Mewah)
            Texture2D jadeTex = Resources.Load<Texture2D>("CustomJadeBack");
            s_jadeMat = new Material(standardShader)
            {
                name = "Mat_Tile_Back_Jade_Shared",
                color = jadeTex != null ? Color.white : new Color(0.04f, 0.42f, 0.24f),
                mainTexture = jadeTex
            };
            s_jadeMat.SetFloat("_Glossiness", 0.75f);
            s_jadeMat.SetFloat("_Metallic", 0.15f);

            s_materialsInitialized = true;
        }

        public void SetupMaterials()
        {
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null) return;

            EnsureMaterials();
            meshRenderer.sharedMaterials = new Material[] { s_frontMat, s_jadeMat };
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
            // Front face (0..3) -> Ikon ubin
            uvs[0] = new Vector2(uMin, vMin);
            uvs[1] = new Vector2(uMax, vMin);
            uvs[2] = new Vector2(uMax, vMax);
            uvs[3] = new Vector2(uMin, vMax);

            // Sisi samping gading depan (4..19) -> Warna gading bersih
            float ivoryU = uMin + uWidth * 0.03f;
            float ivoryV = vMin + vHeight * 0.03f;
            for (int i = 4; i < 20; i++)
            {
                uvs[i] = new Vector2(ivoryU, ivoryV);
            }

            m.uv = uvs;
            meshFilter.sharedMesh = m;
        }

        /// <summary>
        /// Animasi ketika pemain memilih (tap) ubin: Ubin terangkat naik 2.0 cm.
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (isSelected == selected) return;
            isSelected = selected;

            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
            Vector3 targetPos = isSelected ? defaultLocalPos + new Vector3(0, 0.020f, 0) : defaultLocalPos;
            activeAnimCoroutine = StartCoroutine(AnimateLocalPosition(targetPos, 0.12f));
        }

        public void SaveDefaultPosition(Vector3 pos)
        {
            defaultLocalPos = pos;
            transform.localPosition = pos;
        }

        public void MoveToPositionSmooth(Vector3 newPos, float duration = 0.18f)
        {
            defaultLocalPos = newPos;
            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
            activeAnimCoroutine = StartCoroutine(AnimateLocalPosition(newPos, duration));
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
