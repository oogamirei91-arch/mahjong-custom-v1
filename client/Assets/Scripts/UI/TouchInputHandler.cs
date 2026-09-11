using UnityEngine;
using Mahjong.Procedural;
using Mahjong.Network;

namespace Mahjong.UI
{
    /// <summary>
    /// TouchInputHandler: Pengendali Interaksi Layar Sentuh Mobile (Android / iOS), Simulator, & Mouse.
    /// Mendukung tap untuk memilih ubin (terangkat 2.2 cm), gesture drag-ke-atas untuk membuang ubin (Discard),
    /// dan tombol aksi cepat di layar.
    /// </summary>
    public class TouchInputHandler : MonoBehaviour
    {
        public static TouchInputHandler Instance { get; private set; }

        [Header("Pengaturan Input & Toleransi")]
        public LayerMask tileLayerMask = ~0; // Layer ubin 3D
        public float dragDiscardThreshold = 35.0f; // Jarak piksel geser ke atas untuk memicu Discard

        public ProceduralTile SelectedTile { get; private set; } = null;

        private Camera mainCam;
        private Vector2 touchStartScreenPos;
        private bool isDragging = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            mainCam = GetComponent<Camera>();
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) mainCam = Object.FindFirstObjectByType<Camera>();
        }

        private void Update()
        {
            HandleMobileOrMouseInput();
        }

        private void HandleMobileOrMouseInput()
        {
            if (mainCam == null)
            {
                mainCam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
                if (mainCam == null) return;
            }

            // Input Mouse (PC / Unity Editor / Simulator) atau Touch (Android / iOS)
            bool inputDown = Input.GetMouseButtonDown(0);
            bool inputUp   = Input.GetMouseButtonUp(0);
            Vector2 currentScreenPos = Input.mousePosition;

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began) inputDown = true;
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) inputUp = true;
                currentScreenPos = touch.position;
            }

            // 1. Sentuhan Awal (Tap Down)
            if (inputDown)
            {
                touchStartScreenPos = currentScreenPos;
                isDragging = false;

                Ray ray = mainCam.ScreenPointToRay(currentScreenPos);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, tileLayerMask))
                {
                    ProceduralTile tile = hit.collider.GetComponent<ProceduralTile>();
                    if (tile != null && tile.isInteractive)
                    {
                        // Jika ubin yang sama di-tap untuk kedua kalinya -> Lakukan Discard langsung
                        if (SelectedTile == tile)
                        {
                            ExecuteDiscard(tile);
                            return;
                        }

                        // Batalkan seleksi ubin sebelumnya
                        if (SelectedTile != null) SelectedTile.SetSelected(false);

                        // Pilih ubin baru
                        SelectedTile = tile;
                        SelectedTile.SetSelected(true);
                        Audio.ProceduralAudioSynthesizer.Instance?.PlayTileClick();
                        ProceduralLandingAndHUD.Instance?.OnTileSelectedHUD(tile);
                    }
                }
            }

            // 2. Sentuhan Dilepas (Release / Drag-Up Check)
            if (inputUp && SelectedTile != null)
            {
                float deltaY = currentScreenPos.y - touchStartScreenPos.y;
                if (deltaY > dragDiscardThreshold)
                {
                    // Pemain menggeser ubin ke atas layar (Gesture Discard ke tengah meja)
                    ExecuteDiscard(SelectedTile);
                }
            }
        }

        public void ExecuteDiscardSelectedTile()
        {
            if (SelectedTile != null)
            {
                ExecuteDiscard(SelectedTile);
            }
        }

        private void ExecuteDiscard(ProceduralTile tile)
        {
            if (tile == null) return;

            Debug.Log($"[TouchInputHandler] Membuang Ubin: {tile.tileName} (ID: {tile.tileId})");

            ProceduralLandingAndHUD.Instance?.OnTileDiscardedHUD();

            // Rute aksi ke SinglePlayer AI jika mode Solo aktif, atau ke Network Manager jika Online
            if (AI.SinglePlayerAIManager.Instance != null && AI.SinglePlayerAIManager.Instance.isGameActive)
            {
                AI.SinglePlayerAIManager.Instance.OnPlayerDiscardTile(tile.tileId);
            }
            else
            {
                GameNetworkManager.Instance?.DiscardTile(tile.tileId);
            }

            tile.SetSelected(false);
            tile.isInteractive = false;
            SelectedTile = null;
        }

        public void DeselectCurrentTile()
        {
            if (SelectedTile != null)
            {
                SelectedTile.SetSelected(false);
                SelectedTile = null;
                ProceduralLandingAndHUD.Instance?.OnTileDiscardedHUD();
            }
        }
    }
}
