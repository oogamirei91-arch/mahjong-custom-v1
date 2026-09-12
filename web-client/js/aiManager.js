/**
 * aiManager.js - Otak AI untuk 3 Pemain Bot (East "Kenji", North "Mei", West "Dragon")
 * Menghitung efisiensi ubin tangan, membuang ubin terisolasi lebih dulu, dan merespons aksi Chow/Pong/Kong/Win.
 */

import { SUITS, SEATS } from './config.js';

export class AIManager {
    constructor(gameLogic) {
        this.gameLogic = gameLogic;
        this.botProfiles = [
            { seat: SEATS.SOUTH, name: "Anda", isBot: false },
            { seat: SEATS.EAST,  name: "Kenji", isBot: true, style: "Aggressive" },
            { seat: SEATS.NORTH, name: "Mei", isBot: true, style: "Balanced" },
            { seat: SEATS.WEST,  name: "Dragon", isBot: true, style: "Master VIP" }
        ];
    }

    /**
     * Memilih ubin terbaik untuk dibuang oleh Bot
     */
    decideDiscard(seat) {
        const hand = this.gameLogic.hands[seat];
        if (!hand || hand.length === 0) return null;

        // Hitung skor "keburukan / isolasi" setiap ubin. Skor tertinggi = paling layak dibuang.
        let bestTile = hand[0];
        let highestDiscardScore = -999;

        for (const tile of hand) {
            const score = this.evaluateTileDiscardScore(hand, tile);
            if (score > highestDiscardScore) {
                highestDiscardScore = score;
                bestTile = tile;
            }
        }

        return bestTile;
    }

    evaluateTileDiscardScore(hand, tile) {
        let score = 0;
        const matchingCount = hand.filter(t => t.suit === tile.suit && t.value === tile.value).length;

        // Jika ubin sudah membentuk Triplet (3 biji) atau Quad (4 biji), jangan dibuang!
        if (matchingCount >= 3) return -500;
        // Jika ubin membentuk Pair (2 biji), simpan untuk mata (jantai) / Pong
        if (matchingCount === 2) return -150;

        // 1. Ubin Angin (East, South, West, North) atau Naga (Red, Green, White) tanpa pair
        if (tile.suit === SUITS.WIND || tile.suit === SUITS.DRAGON) {
            return 100; // Prioritas utama untuk dibuang
        }

        // 2. Ubin Angka (Wan, Bamboo, Dot)
        const v = tile.value;
        const s = tile.suit;

        const hasNeighbor1 = hand.some(t => t.suit === s && (t.value === v - 1 || t.value === v + 1));
        const hasNeighbor2 = hand.some(t => t.suit === s && (t.value === v - 2 || t.value === v + 2));

        // Jika ubin terhubung ke urutan (misal punya tetangga langsung seperti 4-5)
        if (hasNeighbor1) {
            score -= 100;
        } else if (hasNeighbor2) { // Hubungan kanchan (misal 3 dan 5)
            score -= 40;
        } else {
            // Ubin terisolasi tanpa teman
            score += 50;
            // Angka 1 atau 9 terisolasi lebih buruk daripada angka tengah (4, 5, 6)
            if (v === 1 || v === 9) {
                score += 30;
            } else if (v === 2 || v === 8) {
                score += 15;
            }
        }

        return score;
    }

    /**
     * Mengambil keputusan respon klaim (Win, Kong, Pong, Chow, atau Pass)
     */
    decideClaimAction(seat, prompt) {
        if (prompt.canWin) {
            return { action: "WIN" };
        }

        if (prompt.canKong && Math.random() < 0.85) {
            return { action: "KONG" };
        }

        if (prompt.canPong && Math.random() < 0.75) {
            return { action: "PONG" };
        }

        if (prompt.canChow && prompt.chowOptions && prompt.chowOptions.length > 0 && Math.random() < 0.50) {
            return { action: "CHOW", sequence: prompt.chowOptions[0] };
        }

        return { action: "PASS" };
    }
}
