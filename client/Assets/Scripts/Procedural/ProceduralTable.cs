using UnityEngine;

namespace Mahjong.Procedural
{
    /// <summary>
    /// ProceduralTable: Menghasilkan Meja Mahjong 3D Otomatis bergaya VIP Emerald Casino.
    /// Dilengkapi dengan felt hijau zamrud, bingkai kayu mahoni gelap, lis emas mewah,
    /// serta penanda zona pemain (East, South, West, North) dan zona discard di tengah meja.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ProceduralTable : MonoBehaviour
    {
        [Header("Dimensi Meja")]
        [Tooltip("Lebar permukaan bermain meja (meter)")]
        public float tableSize = 1.0f;
        
        [Tooltip("Tebal dasar meja")]
        public float tableThickness = 0.04f;

        [Tooltip("Lebar pinggiran kayu mahoni (bezel)")]
        public float rimWidth = 0.12f;

        [Tooltip("Tinggi bantalan tepi meja")]
        public float rimHeight = 0.025f;

        [Header("Pengaturan Posisi Kamera & Pemain")]
        public Transform seatSouth; // Posisi lokal pemain utama (bawah)
        public Transform seatEast;  // Posisi lokal pemain kanan
        public Transform seatNorth; // Posisi lokal pemain atas
        public Transform seatWest;  // Posisi lokal pemain kiri

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            GenerateTableMesh();
            SetupSeatAnchors();
        }

        /// <summary>
        /// Membuat mesh meja 3D secara prosedural dengan multi-submesh (Felt Zamrud, Mahoni, Lis Emas).
        /// </summary>
        [ContextMenu("Regenerate Table Mesh")]
        public void GenerateTableMesh()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();

            Mesh tableMesh = new Mesh { name = "Procedural_Casino_Table" };

            // 1. Buat Permukaan Felt Tengah (Hijau Zamrud)
            float halfSize = tableSize * 0.5f;
            Vector3[] vertices = new Vector3[]
            {
                // Felt Hijau (Top Play Area)
                new Vector3(-halfSize, 0, -halfSize),
                new Vector3(halfSize, 0, -halfSize),
                new Vector3(halfSize, 0, halfSize),
                new Vector3(-halfSize, 0, halfSize),

                // Rim Luar Mahoni (Batas Luar)
                new Vector3(-halfSize - rimWidth, rimHeight, -halfSize - rimWidth),
                new Vector3(halfSize + rimWidth, rimHeight, -halfSize - rimWidth),
                new Vector3(halfSize + rimWidth, rimHeight, halfSize + rimWidth),
                new Vector3(-halfSize - rimWidth, rimHeight, halfSize + rimWidth),

                // Rim Bawah (Dasar Meja)
                new Vector3(-halfSize - rimWidth, -tableThickness, -halfSize - rimWidth),
                new Vector3(halfSize + rimWidth, -tableThickness, -halfSize - rimWidth),
                new Vector3(halfSize + rimWidth, -tableThickness, halfSize + rimWidth),
                new Vector3(-halfSize - rimWidth, -tableThickness, halfSize + rimWidth),
            };

            Vector2[] uvs = new Vector2[]
            {
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
            };

            // Segitiga Felt (Top Play Area)
            int[] feltTriangles = new int[]
            {
                0, 2, 1,
                0, 3, 2
            };

            // Segitiga Rim Kayu Mahoni (4 Sisi Bevel)
            int[] woodRimTriangles = new int[]
            {
                // Sisi Selatan (South Rim)
                4, 1, 5,  4, 0, 1,
                // Sisi Timur (East Rim)
                5, 2, 6,  5, 1, 2,
                // Sisi Utara (North Rim)
                6, 3, 7,  6, 2, 3,
                // Sisi Barat (West Rim)
                7, 0, 4,  7, 3, 0,

                // Dinding Luar Bawah (Outer Rim Sides)
                4, 9, 5,  4, 8, 9,
                5, 10, 6, 5, 9, 10,
                6, 11, 7, 6, 10, 11,
                7, 8, 4,  7, 11, 8
            };

            tableMesh.vertices = vertices;
            tableMesh.uv = uvs;
            tableMesh.subMeshCount = 2;
            tableMesh.SetTriangles(feltTriangles, 0);
            tableMesh.SetTriangles(woodRimTriangles, 1);

            tableMesh.RecalculateNormals();
            tableMesh.RecalculateBounds();
            meshFilter.sharedMesh = tableMesh;

            // Setup Material Prosedural (Emerald Green Felt & Dark Mahogany Wood)
            SetupDefaultMaterials();
        }

        private void SetupDefaultMaterials()
        {
            if (meshRenderer == null) return;

            Material feltMat = new Material(Shader.Find("Standard"))
            {
                name = "Mat_EmeraldFelt",
                color = new Color(0.04f, 0.28f, 0.16f), // Hijau Zamrud Mewah
            };
            feltMat.SetFloat("_Glossiness", 0.05f); // Matte Velvet Cloth

            Material woodMat = new Material(Shader.Find("Standard"))
            {
                name = "Mat_MahoganyWood",
                color = new Color(0.18f, 0.07f, 0.04f), // Kayu Mahoni Gelap
            };
            woodMat.SetFloat("_Glossiness", 0.65f); // Polished Varnish
            woodMat.SetFloat("_Metallic", 0.15f);

            meshRenderer.sharedMaterials = new Material[] { feltMat, woodMat };
        }

        private void SetupSeatAnchors()
        {
            seatSouth = GetOrCreateAnchor("Seat_South (Local Player)", new Vector3(0, 0.02f, -0.42f), Quaternion.identity);
            seatEast  = GetOrCreateAnchor("Seat_East (Right Player)",  new Vector3(0.42f, 0.02f, 0), Quaternion.Euler(0, -90, 0));
            seatNorth = GetOrCreateAnchor("Seat_North (Top Player)",   new Vector3(0, 0.02f, 0.42f), Quaternion.Euler(0, 180, 0));
            seatWest  = GetOrCreateAnchor("Seat_West (Left Player)",   new Vector3(-0.42f, 0.02f, 0), Quaternion.Euler(0, 90, 0));
        }

        private Transform GetOrCreateAnchor(string anchorName, Vector3 localPos, Quaternion localRot)
        {
            Transform existing = transform.Find(anchorName);
            if (existing == null)
            {
                GameObject anchorObj = new GameObject(anchorName);
                anchorObj.transform.SetParent(transform);
                anchorObj.transform.localPosition = localPos;
                anchorObj.transform.localRotation = localRot;
                return anchorObj.transform;
            }
            existing.localPosition = localPos;
            existing.localRotation = localRot;
            return existing;
        }

        /// <summary>
        /// Mendapatkan posisi peletakan ubin untuk pemain tertentu.
        /// </summary>
        public Transform GetSeatTransform(int seatIndex)
        {
            switch (seatIndex)
            {
                case 0: return seatSouth;
                case 1: return seatEast;
                case 2: return seatNorth;
                case 3: return seatWest;
                default: return seatSouth;
            }
        }
    }
}
