using UnityEngine;
using System.Collections;

namespace Mahjong.Procedural
{
    /// <summary>
    /// TableCompass: Kompas Meja 3D Digital di tengah meja Mahjong.
    /// Berfungsi sebagai pengukur waktu giliran (15s Turn Timer), penunjuk arah angin aktif (E/S/W/N),
    /// dan indikator sisa ubin pada Wall serta ronde pertandingan (misal: "EAST 1").
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TableCompass : MonoBehaviour
    {
        [Header("Pengaturan Tampilan Visual")]
        public float compassRadius = 0.048f; // Radius 4.8 cm di tengah meja
        public float compassHeight = 0.007f; // Tebal piringan 0.7 cm

        [Header("LED Penunjuk Angin (East, South, West, North)")]
        public MeshRenderer ledEast;
        public MeshRenderer ledSouth;
        public MeshRenderer ledWest;
        public MeshRenderer ledNorth;

        [Header("Status Timer Giliran")]
        public float maxTurnDuration = 15.0f;
        public float remainingTime = 15.0f;
        public bool isTimerRunning = false;
        public int activeSeatIndex = 0; // 0: South (Player), 1: East, 2: North, 3: West

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Material goldBrassMat;
        private Material activeLedMat;
        private Material inactiveLedMat;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            GenerateCompassMesh();
            CreateMaterials();
        }

        private void Update()
        {
            if (isTimerRunning && remainingTime > 0)
            {
                remainingTime -= Time.deltaTime;
                if (remainingTime < 0)
                {
                    remainingTime = 0;
                    isTimerRunning = false;
                    OnTimerExpired();
                }
                UpdateTimerVisual();
            }
        }

        /// <summary>
        /// Membuat mesh silinder heksagonal/lingkaran dengan lis emas berkilau.
        /// </summary>
        [ContextMenu("Regenerate Compass Mesh")]
        public void GenerateCompassMesh()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();

            int segments = 16;
            Mesh mesh = new Mesh { name = "Procedural_Compass_Disc" };

            Vector3[] vertices = new Vector3[segments + 1 + segments * 2];
            int[] triangles = new int[segments * 3 + segments * 6];

            vertices[0] = new Vector3(0, compassHeight, 0); // Titik pusat atas

            float angleStep = 360f / segments;
            for (int i = 0; i < segments; i++)
            {
                float rad = Mathf.Deg2Rad * (i * angleStep);
                float x = Mathf.Cos(rad) * compassRadius;
                float z = Mathf.Sin(rad) * compassRadius;

                // Titik lingkar atas
                vertices[i + 1] = new Vector3(x, compassHeight, z);
                // Titik lingkar samping atas & bawah
                vertices[segments + 1 + i * 2] = new Vector3(x, compassHeight, z);
                vertices[segments + 1 + i * 2 + 1] = new Vector3(x, 0, z);
            }

            // Segitiga piringan atas
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = next + 1;
                triangles[i * 3 + 2] = i + 1;
            }

            // Segitiga dinding samping silinder
            int sideOffset = segments * 3;
            for (int i = 0; i < segments; i++)
            {
                int currTop = segments + 1 + i * 2;
                int currBot = currTop + 1;
                int nextTop = segments + 1 + ((i + 1) % segments) * 2;
                int nextBot = nextTop + 1;

                int tIdx = sideOffset + i * 6;
                triangles[tIdx] = currTop;
                triangles[tIdx + 1] = nextTop;
                triangles[tIdx + 2] = currBot;

                triangles[tIdx + 3] = nextTop;
                triangles[tIdx + 4] = nextBot;
                triangles[tIdx + 5] = currBot;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            meshFilter.sharedMesh = mesh;
        }

        private void CreateMaterials()
        {
            goldBrassMat = new Material(Shader.Find("Standard"))
            {
                name = "Mat_Compass_Gold",
                color = new Color(0.85f, 0.65f, 0.15f) // Emas Berkilau
            };
            goldBrassMat.SetFloat("_Metallic", 0.85f);
            goldBrassMat.SetFloat("_Glossiness", 0.75f);

            meshRenderer.sharedMaterial = goldBrassMat;

            // Material LED Neon
            activeLedMat = new Material(Shader.Find("Standard"))
            {
                name = "Mat_LED_Active",
                color = new Color(0.1f, 0.9f, 0.3f) // Hijau Neon Menyala
            };
            activeLedMat.EnableKeyword("_EMISSION");
            activeLedMat.SetColor("_EmissionColor", new Color(0.2f, 1.0f, 0.4f) * 2.0f);

            inactiveLedMat = new Material(Shader.Find("Standard"))
            {
                name = "Mat_LED_Inactive",
                color = new Color(0.2f, 0.2f, 0.2f) // Abu-abu Redup
            };
        }

        /// <summary>
        /// Memulai countdown giliran untuk pemain tertentu.
        /// </summary>
        public void StartTurnTimer(int seatIndex, float duration = 15.0f)
        {
            this.activeSeatIndex = seatIndex;
            this.maxTurnDuration = duration;
            this.remainingTime = duration;
            this.isTimerRunning = true;
            HighlightActiveWind(seatIndex);
        }

        /// <summary>
        /// Menghentikan timer (misal: pemain telah membuang ubin atau round selesai).
        /// </summary>
        public void StopTimer()
        {
            this.isTimerRunning = false;
        }

        private void HighlightActiveWind(int seatIndex)
        {
            // Reset semua LED arah angin ke padam
            if (ledSouth) ledSouth.sharedMaterial = (seatIndex == 0) ? activeLedMat : inactiveLedMat;
            if (ledEast)  ledEast.sharedMaterial  = (seatIndex == 1) ? activeLedMat : inactiveLedMat;
            if (ledNorth) ledNorth.sharedMaterial = (seatIndex == 2) ? activeLedMat : inactiveLedMat;
            if (ledWest)  ledWest.sharedMaterial  = (seatIndex == 3) ? activeLedMat : inactiveLedMat;
        }

        private void UpdateTimerVisual()
        {
            // Update HUD teks giliran dengan hitung mundur detik
            if (activeSeatIndex == 0)
            {
                int sec = Mathf.CeilToInt(remainingTime);
                if (remainingTime <= 4.0f)
                {
                    UI.ProceduralLandingAndHUD.Instance?.UpdateTurnStatusHUD($"🔴 SISA WAKTU ({sec}s)! Memilih otomatis...", true);
                }
                else
                {
                    UI.ProceduralLandingAndHUD.Instance?.UpdateTurnStatusHUD($"🟢 GILIRAN ANDA! ({sec}s) - Pilih ubin lalu buang", true);
                }
            }

            // Efek perubahan warna LED saat waktu menipis (< 5 detik berubah kuning/merah)
            if (activeLedMat != null)
            {
                if (remainingTime <= 4.0f)
                {
                    // Berkedip Merah Darurat
                    float pulse = Mathf.PingPong(Time.time * 8f, 1.0f);
                    Color alertColor = Color.Lerp(Color.red, Color.yellow, pulse);
                    activeLedMat.SetColor("_EmissionColor", alertColor * 3.0f);
                }
                else if (remainingTime <= 7.0f)
                {
                    activeLedMat.SetColor("_EmissionColor", Color.yellow * 2.0f);
                }
                else
                {
                    activeLedMat.SetColor("_EmissionColor", new Color(0.2f, 1.0f, 0.4f) * 2.0f);
                }
            }
        }

        private void OnTimerExpired()
        {
            Debug.Log($"[TableCompass] Waktu giliran 15 detik habis untuk Seat {activeSeatIndex}! Membuang ubin secara otomatis...");
            if (activeSeatIndex == 0)
            {
                if (AI.SinglePlayerAIManager.Instance != null && AI.SinglePlayerAIManager.Instance.isGameActive)
                {
                    AI.SinglePlayerAIManager.Instance.AutoDiscardForPlayer();
                }
            }
        }
    }
}
