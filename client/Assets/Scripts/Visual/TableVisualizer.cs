using System.Collections.Generic;
using UnityEngine;
using Mahjong.Procedural;
using Mahjong.Network;
using Mahjong.Audio;

namespace Mahjong.Visual
{
    /// <summary>
    /// TableVisualizer: Mengatur visualisasi peletakan ubin 3D di atas meja kasino.
    /// Membagikan 13 ubin ke tangan pemain (South) menghadap kamera secara tegak & jelas,
    /// menempatkan ubin tertutup lawan (East/North/West), serta menampilkan ubin buangan di tengah meja.
    /// </summary>
    public class TableVisualizer : MonoBehaviour
    {
        public static TableVisualizer Instance { get; private set; }

        [Header("Pengaturan Posisi & Jarak Ubin")]
        public float tileSpacingX = 0.034f; // Jarak horizontal antar ubin (3.4 cm)
        public float handCenterZ = -0.36f;  // Jarak tangan dari tengah meja
        public float handHeightY = 0.024f;  // Tinggi ubin dari permukaan felt

        // Kontainer Objek Ubin 3D
        private Transform tilesContainer;
        private List<ProceduralTile> playerTileObjects = new List<ProceduralTile>();
        private List<GameObject> opponentTileObjects = new List<GameObject>();
        private List<ProceduralTile> discardPondObjects = new List<ProceduralTile>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            GameObject container = new GameObject("Tiles_Container");
            container.transform.SetParent(transform);
            tilesContainer = container.transform;
        }

        public void ClearAllTiles()
        {
            foreach (var t in playerTileObjects) if (t != null) Destroy(t.gameObject);
            foreach (var o in opponentTileObjects) if (o != null) Destroy(o);
            foreach (var d in discardPondObjects) if (d != null) Destroy(d.gameObject);

            playerTileObjects.Clear();
            opponentTileObjects.Clear();
            discardPondObjects.Clear();
        }

        /// <summary>
        /// Membagikan dan menampilkan 13 ubin di depan layar pemain lokal (South).
        /// Ubin berdiri tegak dan sedikit miring (pitch 22°) menghadap langsung ke kamera pemain.
        /// </summary>
        public void SpawnPlayerHand(List<TileData> hand)
        {
            foreach (var t in playerTileObjects) if (t != null) Destroy(t.gameObject);
            playerTileObjects.Clear();

            int count = hand.Count;
            float startX = -((count - 1) * tileSpacingX) * 0.5f;

            for (int i = 0; i < count; i++)
            {
                TileData data = hand[i];
                Vector3 pos = new Vector3(startX + (i * tileSpacingX), handHeightY, handCenterZ);
                // Miring 22 derajat ke belakang agar wajah ubin menghadap tegak lurus ke sudut pandang kamera 48 derajat
                Quaternion rot = Quaternion.Euler(22f, 0f, 0f);

                ProceduralTile tileObj = CreateTileGameObject(data, pos, rot, true);
                tileObj.SaveDefaultPosition(pos);
                playerTileObjects.Add(tileObj);
            }

            ProceduralAudioSynthesizer.Instance?.PlayTileClick();
            Debug.Log($"[TableVisualizer] Berhasil menampilkan {count} ubin 3D menghadap kamera pemain!");
        }

        /// <summary>
        /// Menampilkan ubin tertutup milik 3 lawan (East, North, West).
        /// </summary>
        public void SpawnOpponentHands(int countEast = 13, int countNorth = 13, int countWest = 13)
        {
            foreach (var o in opponentTileObjects) if (o != null) Destroy(o);
            opponentTileObjects.Clear();

            // Lawan berdiri membelakangi tengah meja (menampilkan punggung giok hijau ke pemain)
            SpawnOpponentRow(1, countEast, new Vector3(0.36f, handHeightY, 0), Quaternion.Euler(0, -90, 0));   // East (Kanan)
            SpawnOpponentRow(2, countNorth, new Vector3(0, handHeightY, 0.36f), Quaternion.Euler(0, 180, 0));  // North (Atas)
            SpawnOpponentRow(3, countWest, new Vector3(-0.36f, handHeightY, 0), Quaternion.Euler(0, 90, 0));    // West (Kiri)
        }

        private void SpawnOpponentRow(int seatIndex, int count, Vector3 centerPos, Quaternion rot)
        {
            float startOffset = -((count - 1) * tileSpacingX) * 0.5f;
            for (int i = 0; i < count; i++)
            {
                Vector3 localOffset = (seatIndex == 2) 
                    ? new Vector3(startOffset + (i * tileSpacingX), 0, 0)
                    : new Vector3(0, 0, startOffset + (i * tileSpacingX));

                Vector3 finalPos = centerPos + localOffset;
                TileData dummyData = new TileData { id = -1, suit = 0, value = 1, is_bonus = false, name = "Concealed" };
                ProceduralTile t = CreateTileGameObject(dummyData, finalPos, rot, false);
                opponentTileObjects.Add(t.gameObject);
            }
        }

        /// <summary>
        /// Menambahkan 1 ubin baru yang baru saja di-draw (diberi jeda spasi di sebelah kanan).
        /// </summary>
        public void AddDrawnTile(TileData data)
        {
            int count = playerTileObjects.Count;
            float startX = -((count - 1) * tileSpacingX) * 0.5f;
            Vector3 pos = new Vector3(startX + (count * tileSpacingX) + 0.018f, handHeightY, handCenterZ);
            Quaternion rot = Quaternion.Euler(22f, 0f, 0f);

            ProceduralTile tileObj = CreateTileGameObject(data, pos, rot, true);
            tileObj.SaveDefaultPosition(pos);
            playerTileObjects.Add(tileObj);

            ProceduralAudioSynthesizer.Instance?.PlayTileClick();
        }

        /// <summary>
        /// Menghapus ubin yang dibuang dari tangan pemain dan menempatkannya di tengah meja (Discard Pond).
        /// </summary>
        public void VisualDiscardTile(int seatIndex, TileData data)
        {
            // Jika pemain lokal yang membuang, hapus dari list tangan
            if (seatIndex == 0)
            {
                ProceduralTile found = playerTileObjects.Find(t => t.tileId == data.id);
                if (found != null)
                {
                    playerTileObjects.Remove(found);
                    Destroy(found.gameObject);
                }
            }

            // Tempatkan ubin terbuka di area buangan tengah meja (Discard Pond 6 kolom)
            int discardCount = discardPondObjects.Count;
            int col = discardCount % 6;
            int row = discardCount / 6;

            float pondStartX = -0.10f;
            float pondStartZ = 0.16f;
            Vector3 pondPos = new Vector3(pondStartX + (col * 0.035f), 0.012f, pondStartZ - (row * 0.046f));
            // Ubin berbaring rata di atas meja dengan wajah menghadap ke atas (Rotasi X -90 derajat)
            Quaternion pondRot = Quaternion.Euler(-90f, 0, 0);

            ProceduralTile pondTile = CreateTileGameObject(data, pondPos, pondRot, false);
            discardPondObjects.Add(pondTile);

            ProceduralAudioSynthesizer.Instance?.PlayTileDiscard();
        }

        private ProceduralTile CreateTileGameObject(TileData data, Vector3 pos, Quaternion rot, bool isPlayerTile)
        {
            GameObject obj = new GameObject($"Tile_{data.id}_{data.name}");
            obj.transform.SetParent(tilesContainer);
            obj.transform.position = pos;
            obj.transform.rotation = rot;

            ProceduralTile tileComponent = obj.AddComponent<ProceduralTile>();
            tileComponent.isInteractive = isPlayerTile;
            tileComponent.Initialize(data.id, data.suit, data.value, data.is_bonus, data.name);
            return tileComponent;
        }
    }
}
