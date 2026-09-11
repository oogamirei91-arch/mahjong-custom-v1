using UnityEngine;
using Mahjong.Procedural;
using Mahjong.Network;

namespace Mahjong.UI
{
    /// <summary>
    /// TouchInputHandler: Pengendali Interaksi Layar Sentuh Mobile (Android / iOS) & Mouse.
    /// Mendukung tap untuk memilih ubin (terangkat 1.8 cm) dan gesture drag-ke-atas untuk membuang ubin (Discard).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class TouchInputHandler : MonoBehaviour
    {
        [Header("Pengaturan Input & Toleransi")]
        public LayerMask tileLayerMask = ~0; // Layer ubin 3D
        public float dragDiscardThreshold = 40.0f; // Jarak piksel geser ke atas untuk memicu Discard

        private Camera mainCam;
        private ProceduralTile selectedTile = null;
        private Vector2 touchStartScreenPos;
        private bool isDragging = false;

        private void Awake()
        {
            mainCam = GetComponent<Camera>();
        }

        private void Update()
        {
            HandleMobileOrMouseInput();
        }

        private void HandleMobileOrMouseInput()
        {
            // Input Mouse (PC / Unity Editor) atau Touch (Android / iOS)
            bool inputDown = Input.GetMouseButtonDown(0);
            bool inputUp   = Input.GetMouseButtonUp(0);
            Vector2 currentScreenPos = Input.mousePosition;

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                inputDown = (touch.phase == TouchPhase.Began);
                inputUp   = (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled);
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
                        if (selectedTile == tile)
                        {
                            ExecuteDiscard(tile);
                            return;
                        }

                        // Batalkan seleksi ubin sebelumnya
                        if (selectedTile != null) selectedTile.SetSelected(false);

                        // Pilih ubin baru
                        selectedTile = tile;
                        selectedTile.SetSelected(true);
                        Audio.ProceduralAudioSynthesizer.Instance?.PlayTileClick();
                    }
                }
            }

            // 2. Sentuhan Dilepas (Release / Drag-Up Check)
            if (inputUp && selectedTile != null)
            {
                float deltaY = currentScreenPos.y - touchStartScreenPos.y;
                if (deltaY > dragDiscardThreshold)
                {
                    // Pemain menggeser ubin ke atas layar (Gesture Discard ke tengah meja)
                    ExecuteDiscard(selectedTile);
                }
            }
        }

        private void ExecuteDiscard(ProceduralTile tile)
        {
            if (tile == null) return;

            Debug.Log($"[TouchInputHandler] Membuang Ubin: {tile.tileName} (ID: {tile.tileId})");

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
            selectedTile = null;
        }

        public void DeselectCurrentTile()
        {
            if (selectedTile != null)
            {
                selectedTile.SetSelected(false);
                selectedTile = null;
            }
        }
    }
}
