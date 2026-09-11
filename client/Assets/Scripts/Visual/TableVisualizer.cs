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
    /// menata ulang ubin secara mulus saat dibuang, menempatkan ubin tertutup lawan (East/North/West),
    /// serta menampilkan ubin buangan di 4 kuadran kolam meja.
    /// </summary>
    public class TableVisualizer : MonoBehaviour
    {
        public static TableVisualizer Instance { get; private set; }

        [Header("Pengaturan Posisi & Jarak Ubin HD")]
        public float tileSpacingX = 0.048f; // Jarak horizontal antar ubin (4.8 cm)
        public float handCenterZ = -0.27f;  // Jarak tangan dari tengah meja (dekat dan jelas di layar bawah)
        public float handHeightY = 0.034f;  // Tinggi ubin dari permukaan felt

        // Kontainer Objek Ubin 3D
        private Transform tilesContainer;
        private List<ProceduralTile> playerTileObjects = new List<ProceduralTile>();
        private List<GameObject> opponentTileObjects = new List<GameObject>();
        private List<ProceduralTile> discardPondObjects = new List<ProceduralTile>();

        private int[] seatDiscardCounts = new int[4];

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
            for (int i = 0; i < 4; i++) seatDiscardCounts[i] = 0;
        }

        /// <summary>
        /// Menghitung bobot urutan ubin Mahjong standar:
        /// Characters (Wan 1-9) -> Bamboo (Sou 1-9) -> Dots (Pin 1-9) -> Winds (E/S/W/N) -> Dragons (C/F/P) -> Flowers/Seasons.
        /// </summary>
        public static int GetTileSortWeight(int suit, int value)
        {
            int suitWeight = 0;
            switch (suit)
            {
                case 2: suitWeight = 100; break; // Characters / Wan (1-9) -> 101 .. 109
                case 0: suitWeight = 200; break; // Bamboo / Sou (1-9)     -> 201 .. 209
                case 1: suitWeight = 300; break; // Dots / Pin (1-9)       -> 301 .. 309
                case 3: suitWeight = 400; break; // Winds (1-4: E,S,W,N)   -> 401 .. 404
                case 4: suitWeight = 500; break; // Dragons (1-3: C,F,P)   -> 501 .. 503
                case 5: suitWeight = 600; break; // Flowers (1-4)          -> 601 .. 604
                case 6: suitWeight = 700; break; // Seasons (1-4)          -> 701 .. 704
                default: suitWeight = 900; break;
            }
            return suitWeight + value;
        }

        public static void SortHandData(List<TileData> hand)
        {
            if (hand == null) return;
            hand.Sort((a, b) => GetTileSortWeight(a.suit, a.value).CompareTo(GetTileSortWeight(b.suit, b.value)));
        }

        /// <summary>
        /// Membagikan dan menampilkan 13 ubin di depan layar pemain lokal (South).
        /// Ubin berdiri tegak dan sedikit miring (pitch 28°) menghadap langsung ke kamera pemain.
        /// </summary>
        public void SpawnPlayerHand(List<TileData> hand)
        {
            foreach (var t in playerTileObjects) if (t != null) Destroy(t.gameObject);
            playerTileObjects.Clear();

            // Urutkan data ubin tangan secara otomatis
            SortHandData(hand);

            int count = hand.Count;
            float startX = -((count - 1) * tileSpacingX) * 0.5f;

            for (int i = 0; i < count; i++)
            {
                TileData data = hand[i];
                Vector3 pos = new Vector3(startX + (i * tileSpacingX), handHeightY, handCenterZ);
                Quaternion rot = Quaternion.Euler(28f, 0f, 0f);

                ProceduralTile tileObj = CreateTileGameObject(data, pos, rot, true);
                tileObj.SaveDefaultPosition(pos);
                playerTileObjects.Add(tileObj);
            }

            ProceduralAudioSynthesizer.Instance?.PlayTileClick();
            Debug.Log($"[TableVisualizer] Berhasil menampilkan & mengurutkan {count} ubin 3D HD pemain!");
        }

        /// <summary>
        /// Menampilkan ubin tertutup milik 3 lawan (East, North, West).
        /// </summary>
        public void SpawnOpponentHands(int countEast = 13, int countNorth = 13, int countWest = 13)
        {
            foreach (var o in opponentTileObjects) if (o != null) Destroy(o);
            opponentTileObjects.Clear();

            SpawnOpponentRow(1, countEast, new Vector3(0.36f, handHeightY, 0), Quaternion.Euler(0, -90, 0));   // East (Kanan - Punggung Giok ke Tengah)
            SpawnOpponentRow(2, countNorth, new Vector3(0, handHeightY, 0.36f), Quaternion.Euler(0, 180, 0));  // North (Atas - Punggung Giok ke Kamera)
            SpawnOpponentRow(3, countWest, new Vector3(-0.36f, handHeightY, 0), Quaternion.Euler(0, 90, 0));    // West (Kiri - Punggung Giok ke Tengah)
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
            Vector3 pos = new Vector3(startX + (count * tileSpacingX) + 0.025f, handHeightY, handCenterZ);
            Quaternion rot = Quaternion.Euler(28f, 0f, 0f);

            ProceduralTile tileObj = CreateTileGameObject(data, pos, rot, true);
            tileObj.SaveDefaultPosition(pos);
            playerTileObjects.Add(tileObj);

            ProceduralAudioSynthesizer.Instance?.PlayTileClick();
        }

        /// <summary>
        /// Merapikan dan mengurutkan kembali posisi sisa ubin pemain agar selalu terorganisir rapi di tangan.
        /// </summary>
        private void RealignPlayerHand()
        {
            // Urutkan objek ubin berdasarkan bobot suit & nilai
            playerTileObjects.Sort((a, b) => GetTileSortWeight(a.suit, a.value).CompareTo(GetTileSortWeight(b.suit, b.value)));

            int count = playerTileObjects.Count;
            if (count == 0) return;

            float startX = -((count - 1) * tileSpacingX) * 0.5f;
            for (int i = 0; i < count; i++)
            {
                Vector3 newPos = new Vector3(startX + (i * tileSpacingX), handHeightY, handCenterZ);
                playerTileObjects[i].MoveToPositionSmooth(newPos, 0.20f);
            }
        }

        /// <summary>
        /// Menghapus ubin yang dibuang dari tangan pemain dan menempatkannya di kolam meja sesuai kuadran kursi.
        /// </summary>
        public void VisualDiscardTile(int seatIndex, TileData data, List<TileData> updatedSortedHand = null)
        {
            if (seatIndex == 0)
            {
                ProceduralTile found = playerTileObjects.Find(t => t.tileId == data.id);
                if (found != null)
                {
                    playerTileObjects.Remove(found);
                    Destroy(found.gameObject);
                }
                RealignPlayerHand();
            }

            int count = seatDiscardCounts[Mathf.Clamp(seatIndex, 0, 3)]++;
            int col = count % 6;
            int row = count / 6;

            Vector3 pondPos = Vector3.zero;
            Quaternion pondRot = Quaternion.identity;

            float spacingX = 0.048f;
            float spacingZ = 0.066f;

            // Atur posisi 4 kuadran buangan di sekitar kompas tengah meja (Wajah Ubin Menghadap Ke Atas!)
            switch (seatIndex)
            {
                case 0: // South (Bawah): berbaris di bawah kompas menghadap ke atas
                    pondPos = new Vector3(-0.12f + (col * spacingX), 0.014f, -0.11f - (row * spacingZ));
                    pondRot = Quaternion.Euler(90f, 0f, 0f);
                    break;
                case 1: // East (Kanan): berbaris di kanan kompas
                    pondPos = new Vector3(0.11f + (row * spacingZ), 0.014f, -0.12f + (col * spacingX));
                    pondRot = Quaternion.Euler(90f, -90f, 0f);
                    break;
                case 2: // North (Atas): berbaris di atas kompas
                    pondPos = new Vector3(0.12f - (col * spacingX), 0.014f, 0.11f + (row * spacingZ));
                    pondRot = Quaternion.Euler(90f, 180f, 0f);
                    break;
                case 3: // West (Kiri): berbaris di kiri kompas
                    pondPos = new Vector3(-0.11f - (row * spacingZ), 0.014f, 0.12f - (col * spacingX));
                    pondRot = Quaternion.Euler(90f, 90f, 0f);
                    break;
            }

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
