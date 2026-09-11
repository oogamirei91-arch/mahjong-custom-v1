using System;
using System.Collections.Generic;
using UnityEngine;
using Mahjong.Network;

namespace Mahjong.AI
{
    public enum AIDifficultyLevel
    {
        Novice = 0,  // Pemula
        Expert = 1,  // Mahir
        Master = 2   // Master VIP
    }

    /// <summary>
    /// MahjongAIController: Otak Bot AI Mahjong di sisi klien (Unity C#).
    /// Mengkalkulasi efisiensi ubin (Tile Efficiency & Shanten Heuristic) untuk memilih
    /// ubin yang paling tidak berguna untuk dibuang serta merespons deklarasi Chow/Pong/Kong/Win.
    /// </summary>
    public class MahjongAIController
    {
        public string BotId { get; private set; }
        public string BotName { get; private set; }
        public int SeatIndex { get; private set; } // 1: East, 2: North, 3: West
        public AIDifficultyLevel Difficulty { get; set; }

        public List<TileData> Hand { get; private set; } = new List<TileData>();
        public List<TileData> BonusTiles { get; private set; } = new List<TileData>();
        public List<MeldData> Melds { get; private set; } = new List<MeldData>();
        public List<TileData> Discards { get; private set; } = new List<TileData>();

        public MahjongAIController(string id, string name, int seat, AIDifficultyLevel diff = AIDifficultyLevel.Expert)
        {
            BotId = id;
            BotName = name;
            SeatIndex = seat;
            Difficulty = diff;
        }

        public void ResetForNewRound()
        {
            Hand.Clear();
            BonusTiles.Clear();
            Melds.Clear();
            Discards.Clear();
        }

        public void AddTile(TileData tile)
        {
            if (tile.is_bonus) BonusTiles.Add(tile);
            else Hand.Add(tile);
        }

        /// <summary>
        /// Memilih ubin dari tangan untuk dibuang menggunakan heuristik prioritas efisiensi.
        /// </summary>
        public TileData DecideDiscard()
        {
            if (Hand.Count == 0) return null;

            // Matriks frekuensi [Suit][Value]
            int[,] counts = new int[7, 10];
            foreach (var t in Hand)
            {
                if (t.suit >= 0 && t.suit < 7 && t.value >= 1 && t.value <= 9)
                {
                    counts[t.suit, t.value]++;
                }
            }

            // 1. Prioritas 1: Ubin Angin / Naga (Honors: Suit 3 & 4) yang terisolasi (hanya punya 1 keping)
            for (int i = 0; i < Hand.Count; i++)
            {
                var t = Hand[i];
                if ((t.suit == 3 || t.suit == 4) && counts[t.suit, t.value] == 1)
                {
                    return t;
                }
            }

            // 2. Prioritas 2: Ubin Terminal (Nilai 1 atau 9) yang terisolasi tanpa tetangga (2 atau 8)
            for (int i = 0; i < Hand.Count; i++)
            {
                var t = Hand[i];
                if (t.suit <= 2 && (t.value == 1 || t.value == 9) && counts[t.suit, t.value] == 1)
                {
                    bool hasNeighbor = false;
                    if (t.value == 1 && (counts[t.suit, 2] > 0 || counts[t.suit, 3] > 0)) hasNeighbor = true;
                    if (t.value == 9 && (counts[t.suit, 8] > 0 || counts[t.suit, 7] > 0)) hasNeighbor = true;
                    if (!hasNeighbor) return t;
                }
            }

            // 3. Prioritas 3: Ubin biasa (2-8) yang tidak punya kawan urutan (+-1 atau +-2)
            List<TileData> isolatedCandidates = new List<TileData>();
            foreach (var t in Hand)
            {
                if (counts[t.suit, t.value] == 1)
                {
                    bool hasSeq = false;
                    int v = t.value;
                    int s = t.suit;
                    if (s <= 2)
                    {
                        if ((v > 1 && counts[s, v - 1] > 0) || (v < 9 && counts[s, v + 1] > 0) ||
                            (v > 2 && counts[s, v - 2] > 0) || (v < 8 && counts[s, v + 2] > 0))
                        {
                            hasSeq = true;
                        }
                    }
                    if (!hasSeq) isolatedCandidates.Add(t);
                }
            }

            if (isolatedCandidates.Count > 0)
            {
                return isolatedCandidates[UnityEngine.Random.Range(0, isolatedCandidates.Count)];
            }

            // Fallback: Buang ubin paling kanan di tangan
            return Hand[Hand.Count - 1];
        }

        /// <summary>
        /// Menentukan reaksi bot (Chow, Pong, Kong, Win, Pass) terhadap buangan lawan.
        /// </summary>
        public string DecideReaction(TileData discardedTile, bool canWin, bool canPong, bool canKong, bool canChow)
        {
            if (canWin) return "win";
            if (canKong) return "kong";
            if (canPong && UnityEngine.Random.value < 0.70f) return "pong";
            if (canChow && UnityEngine.Random.value < 0.50f) return "chow";
            return "pass";
        }

        /// <summary>
        /// Jeda simulasi berpikir bot (1.0 - 2.0 detik) agar terasa natural seperti manusia.
        /// </summary>
        public float GetSimulatedThinkingTime()
        {
            return UnityEngine.Random.Range(1.1f, 1.9f);
        }
    }
}
