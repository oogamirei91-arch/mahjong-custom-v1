using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mahjong.Procedural;
using Mahjong.Network;
using Mahjong.Audio;
using Mahjong.UI;

namespace Mahjong.AI
{
    /// <summary>
    /// SinglePlayerAIManager: Manajer Mode Single Player Offline / Latihan Melawan 3 Bot AI.
    /// Memungkinkan pemain bermain secara instan kapan saja tanpa koneksi internet atau menunggu antrean.
    /// Mengontrol pembagian 144 ubin, pergiliran 15 detik pada kompas meja, logika berpikir bot, dan evaluasi kemenangan.
    /// </summary>
    public class SinglePlayerAIManager : MonoBehaviour
    {
        public static SinglePlayerAIManager Instance { get; private set; }

        [Header("State Permainan Solo vs AI")]
        public bool isGameActive = false;
        public int currentTurnSeat = 0; // 0: Player (South), 1: East Bot, 2: North Bot, 3: West Bot
        public int remainingWallCount = 0;
        public AIDifficultyLevel currentDifficulty = AIDifficultyLevel.Expert;

        // Referensi 3 Bot AI
        private MahjongAIController botEast;
        private MahjongAIController botNorth;
        private MahjongAIController botWest;

        // Data Tangan Pemain Lokal (South)
        public List<TileData> playerHand = new List<TileData>();
        public List<TileData> playerBonusTiles = new List<TileData>();
        public List<TileData> centerDiscards = new List<TileData>();

        // Deck Dinding Ubin
        private List<TileData> wallDeck = new List<TileData>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            InitializeBots();
        }

        private void InitializeBots()
        {
            botEast  = new MahjongAIController("bot_e", "Bot_Kenji (East)", 1, currentDifficulty);
            botNorth = new MahjongAIController("bot_n", "Bot_Mei (North)", 2, currentDifficulty);
            botWest  = new MahjongAIController("bot_w", "Bot_Dragon (West)", 3, currentDifficulty);
        }

        /// <summary>
        /// Memulai permainan baru melawan 3 Bot AI.
        /// </summary>
        public void StartSoloGame(AIDifficultyLevel diff = AIDifficultyLevel.Expert)
        {
            this.currentDifficulty = diff;
            botEast.Difficulty = diff;
            botNorth.Difficulty = diff;
            botWest.Difficulty = diff;

            GenerateFullDeckAndShuffle();
            DealHands();

            isGameActive = true;
            currentTurnSeat = 0; // Pemain lokal (South) mulai pertama
            StartPlayerTurn();
        }

        private void GenerateFullDeckAndShuffle()
        {
            wallDeck.Clear();
            int id = 0;

            // 1. Bamboo (0), Dot (1), Character (2): Nilai 1-9 (masing-masing 4 keping)
            for (int suit = 0; suit <= 2; suit++)
            {
                for (int val = 1; val <= 9; val++)
                {
                    for (int copy = 0; copy < 4; copy++)
                    {
                        wallDeck.Add(new TileData { id = id++, suit = suit, value = val, is_bonus = false, name = $"{val} Suit {suit}" });
                    }
                }
            }

            // 2. Winds (3): East, South, West, North (masing-masing 4 keping)
            for (int val = 1; val <= 4; val++)
            {
                for (int copy = 0; copy < 4; copy++)
                {
                    wallDeck.Add(new TileData { id = id++, suit = 3, value = val, is_bonus = false, name = $"Wind {val}" });
                }
            }

            // 3. Dragons (4): Red, Green, White (masing-masing 4 keping)
            for (int val = 1; val <= 3; val++)
            {
                for (int copy = 0; copy < 4; copy++)
                {
                    wallDeck.Add(new TileData { id = id++, suit = 4, value = val, is_bonus = false, name = $"Dragon {val}" });
                }
            }

            // 4. Bonus (5 & 6): Flowers (1-4) & Seasons (1-4) (masing-masing 1 keping)
            for (int val = 1; val <= 4; val++)
            {
                wallDeck.Add(new TileData { id = id++, suit = 5, value = val, is_bonus = true, name = $"Flower {val}" });
                wallDeck.Add(new TileData { id = id++, suit = 6, value = val, is_bonus = true, name = $"Season {val}" });
            }

            // Fisher-Yates Shuffle
            for (int i = wallDeck.Count - 1; i > 0; i--)
            {
                int r = UnityEngine.Random.Range(0, i + 1);
                var temp = wallDeck[i];
                wallDeck[i] = wallDeck[r];
                wallDeck[r] = temp;
            }

            remainingWallCount = wallDeck.Count;
        }

        private void DealHands()
        {
            playerHand.Clear();
            playerBonusTiles.Clear();
            centerDiscards.Clear();
            botEast.ResetForNewRound();
            botNorth.ResetForNewRound();
            botWest.ResetForNewRound();

            // Bagikan 13 ubin untuk masing-masing 4 pemain
            for (int i = 0; i < 13; i++)
            {
                DrawTileForPlayer();
                DrawTileForBot(botEast);
                DrawTileForBot(botNorth);
                DrawTileForBot(botWest);
            }
        }

        private TileData DrawFromWall()
        {
            if (wallDeck.Count == 0) return null;
            TileData t = wallDeck[0];
            wallDeck.RemoveAt(0);
            remainingWallCount = wallDeck.Count;
            return t;
        }

        private void DrawTileForPlayer()
        {
            TileData t = DrawFromWall();
            if (t == null) return;

            if (t.is_bonus)
            {
                playerBonusTiles.Add(t);
                DrawTileForPlayer(); // Ambil ubin pengganti (flower replacement)
            }
            else
            {
                playerHand.Add(t);
            }
        }

        private void DrawTileForBot(MahjongAIController bot)
        {
            TileData t = DrawFromWall();
            if (t == null) return;

            if (t.is_bonus)
            {
                bot.AddTile(t);
                DrawTileForBot(bot);
            }
            else
            {
                bot.AddTile(t);
            }
        }

        // =========================================================================
        // SIKLUS GILIRAN & LOGIKA AI BERPIKIR
        // =========================================================================

        public void StartPlayerTurn()
        {
            if (!isGameActive) return;
            currentTurnSeat = 0;

            DrawTileForPlayer();
            FindObjectOfType<TableCompass>()?.StartTurnTimer(0, 15f);
            Debug.Log($"[Solo AI Mode] Giliran Pemain (South)! Total Ubin: {playerHand.Count}");
        }

        public void OnPlayerDiscardTile(int tileId)
        {
            if (!isGameActive || currentTurnSeat != 0) return;

            TileData discarded = playerHand.Find(t => t.id == tileId);
            if (discarded != null)
            {
                playerHand.Remove(discarded);
                centerDiscards.Add(discarded);
                ProceduralAudioSynthesizer.Instance?.PlayTileDiscard();
                Debug.Log($"[Solo AI Mode] Pemain membuang: {discarded.name}");

                // Periksa apakah ada Bot yang bisa menang / Pong
                StartCoroutine(ProcessAITurns(1)); // Lanjut ke Bot East
            }
        }

        private IEnumerator ProcessAITurns(int nextSeat)
        {
            currentTurnSeat = nextSeat;

            while (isGameActive && currentTurnSeat > 0)
            {
                MahjongAIController activeBot = GetBotBySeat(currentTurnSeat);
                if (activeBot == null) break;

                // 1. Bot mengambil ubin dari wall
                DrawTileForBot(activeBot);
                FindObjectOfType<TableCompass>()?.StartTurnTimer(currentTurnSeat, 15f);

                // 2. Simulasi bot berpikir (1.2s - 1.8s)
                float thinkTime = activeBot.GetSimulatedThinkingTime();
                yield return new WaitForSeconds(thinkTime);

                // 3. Bot memilih ubin buangan
                TileData botDiscard = activeBot.DecideDiscard();
                if (botDiscard != null)
                {
                    activeBot.Hand.Remove(botDiscard);
                    centerDiscards.Add(botDiscard);
                    ProceduralAudioSynthesizer.Instance?.PlayTileDiscard();
                    Debug.Log($"[Solo AI Mode] {activeBot.BotName} membuang: {botDiscard.name}");
                }

                // Periksa jika sisa ubin habis (Exhaustive Draw)
                if (remainingWallCount <= 0)
                {
                    isGameActive = false;
                    Debug.Log("[Solo AI Mode] Ubin Wall Habis! Ronde Berakhir Seri (Draw).");
                    yield break;
                }

                // Lanjut ke giliran berikutnya (Seat 1 ➔ 2 ➔ 3 ➔ 0)
                currentTurnSeat = (currentTurnSeat + 1) % 4;
                if (currentTurnSeat == 0)
                {
                    StartPlayerTurn();
                    yield break;
                }
            }
        }

        private MahjongAIController GetBotBySeat(int seat)
        {
            switch (seat)
            {
                case 1: return botEast;
                case 2: return botNorth;
                case 3: return botWest;
                default: return null;
            }
        }
    }
}
