using UnityEngine;
using System.Collections;

namespace Mahjong.Procedural
{
    /// <summary>
    /// ProceduralTile: Mengontrol visual dan perilaku 3D satu Ubin Mahjong.
    /// Dilengkapi mesh dual-layer (Wajah Gading Pearl di depan, Giok Hijau di belakang),
    /// animasi hover saat disentuh di layar HP, dan efek lemparan ubin (discard drop).
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider))]
    public class ProceduralTile : MonoBehaviour
    {
        [Header("Identitas Tile")]
        public int tileId;          // 0 - 143
        public int suit;            // 0: Dot, 1: Bamboo, 2: Character, 3: Honor, 4: Bonus
        public int value;           // 1 - 9
        public bool isBonus;
        public string tileName;

        [Header("Dimensi Ubin 3D (Rasio Standar Mahjong)")]
        public float tileWidth = 0.030f;    // Lebar X (3.0 cm)
        public float tileHeight = 0.040f;   // Panjang Y (4.0 cm)
        public float tileThickness = 0.020f;// Tebal Z (2.0 cm)

        [Header("State Interaksi")]
        public bool isSelected = false;
        public bool isConcealed = true; // Tertutup (hanya pemain sendiri yang melihat wajahnya)
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

        /// <summary>
        /// Menginisialisasi data ubin dari engine server.
        /// </summary>
        public void Initialize(int id, int suitVal, int numVal, bool bonus, string name)
        {
            this.tileId = id;
            this.suit = suitVal;
            this.value = numVal;
            this.isBonus = bonus;
            this.tileName = name;
            gameObject.name = $"Tile_{id}_{name}";
            ApplyTileUV();
        }

        /// <summary>
        /// Membuat mesh 3D kotak ubin beveled dengan 2 material (0: Depan Gading, 1: Belakang Giok).
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
                // Front Face (Z +) -> Wajah Ubin
                new Vector3(-hx, -hy,  hz), new Vector3( hx, -hy,  hz),
                new Vector3( hx,  hy,  hz), new Vector3(-hx,  hy,  hz),

                // Back Face (Z -) -> Punggung Giok
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

            // Sisanya default mapping
            for (int i = 4; i < 24; i += 4)
            {
                uvs[i]   = new Vector2(0, 0);
                uvs[i+1] = new Vector2(1, 0);
                uvs[i+2] = new Vector2(1, 1);
                uvs[i+3] = new Vector2(0, 1);
            }

            // Submesh 0: Front Face (Ivory Pearl)
            int[] frontTriangles = new int[] { 0, 2, 1, 0, 3, 2 };

            // Submesh 1: Sisi Lainnya (Jade Green & Sides)
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
            boxCollider.size = new Vector3(tileWidth, tileHeight, tileThickness);

            SetupMaterials();
        }

        private void SetupMaterials()
        {
            if (meshRenderer == null) return;

            // Material Wajah: Pearl Ivory Putih
            Material frontMat = new Material(Shader.Find("Standard"))
            {
                name = "Mat_Tile_Front_Ivory",
                color = new Color(0.96f, 0.95f, 0.90f) // Gading Elegan
            };
            frontMat.SetFloat("_Glossiness", 0.5f);

            // Material Punggung: Jade Green (Giok Hijau Mewah)
            Material jadeMat = new Material(Shader.Find("Standard"))
            {
                name = "Mat_Tile_Back_Jade",
                color = new Color(0.06f, 0.45f, 0.28f) // Giok Zamrud Tua
            };
            jadeMat.SetFloat("_Glossiness", 0.75f);
            jadeMat.SetFloat("_Metallic", 0.1f);

            meshRenderer.sharedMaterials = new Material[] { frontMat, jadeMat };
        }

        /// <summary>
        /// Mengatur UV offset untuk menampilkan ikon ubin spesifik dari Texture Atlas 6x7.
        /// </summary>
        public void ApplyTileUV()
        {
            if (meshFilter == null || meshFilter.sharedMesh == null) return;

            // Atlas Matrix 6 Kolom x 7 Baris
            int col = (value - 1) % 6;
            int row = suit;
            if (suit == 3) row = 3; // Honors
            if (suit == 4) row = 4; // Bonus

            float uWidth = 1.0f / 6.0f;
            float vHeight = 1.0f / 7.0f;

            float uMin = col * uWidth;
            float uMax = uMin + uWidth;
            float vMax = 1.0f - (row * vHeight);
            float vMin = vMax - vHeight;

            Vector2[] uvs = meshFilter.sharedMesh.uv;
            uvs[0] = new Vector2(uMin, vMin);
            uvs[1] = new Vector2(uMax, vMin);
            uvs[2] = new Vector2(uMax, vMax);
            uvs[3] = new Vector2(uMin, vMax);

            meshFilter.sharedMesh.uv = uvs;
        }

        /// <summary>
        /// Animasi ketika pemain memilih (tap) ubin: Ubin terangkat naik 1.5 cm.
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (isSelected == selected) return;
            isSelected = selected;

            if (activeAnimCoroutine != null) StopCoroutine(activeAnimCoroutine);
            Vector3 targetPos = isSelected ? defaultLocalPos + new Vector3(0, 0.018f, 0) : defaultLocalPos;
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
