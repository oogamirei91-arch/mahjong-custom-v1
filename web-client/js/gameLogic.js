/**
 * gameLogic.js - Engine Aturan & Logika Mahjong Standar 144 Ubin
 * Mengatur deck shuffling, pembagian ubin, pengecekan Chow (Chi), Pong (Peng), Kong (Gang), dan Hu (Menang).
 */

import { generateFullDeck, sortTiles, SUITS, SEATS } from './config.js';

export class MahjongGameLogic {
    constructor() {
        this.deck = [];
        this.hands = [[], [], [], []];       // South, East, North, West
        this.discards = [[], [], [], []];    // Ubin di kolam buangan per kursi
        this.melds = [[], [], [], []];       // Ubin meld (Chow, Pong, Kong) terbuka
        this.bonusTiles = [[], [], [], []];  // Bunga & Musim

        this.currentTurnSeat = SEATS.SOUTH;
        this.dealerSeat = SEATS.SOUTH;
        this.lastDiscardedTile = null;
        this.lastDiscarderSeat = null;

        this.isRoundActive = false;
        this.onStateChangeCallback = null;
        this.onTurnChangeCallback = null;
        this.onActionPromptCallback = null;
        this.onRoundEndCallback = null;
    }

    startNewGame(dealerSeat = SEATS.SOUTH) {
        this.dealerSeat = dealerSeat;
        this.currentTurnSeat = dealerSeat;
        this.deck = this.shuffleDeck(generateFullDeck());
        this.hands = [[], [], [], []];
        this.discards = [[], [], [], []];
        this.melds = [[], [], [], []];
        this.bonusTiles = [[], [], [], []];
        this.lastDiscardedTile = null;
        this.lastDiscarderSeat = null;
        this.isRoundActive = true;

        // Deal 13 tiles to all players
        for (let seat = 0; seat < 4; seat++) {
            for (let i = 0; i < 13; i++) {
                this.dealTileTo(seat);
            }
            this.hands[seat] = sortTiles(this.hands[seat]);
        }

        // Deal the 14th tile to the dealer
        this.dealTileTo(this.dealerSeat);
        this.hands[this.dealerSeat] = sortTiles(this.hands[this.dealerSeat]);

        if (this.onStateChangeCallback) {
            this.onStateChangeCallback({
                hands: this.hands,
                discards: this.discards,
                melds: this.melds,
                remainingWall: this.deck.length,
                currentTurn: this.currentTurnSeat
            });
        }
    }

    shuffleDeck(deck) {
        const shuffled = [...deck];
        for (let i = shuffled.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [shuffled[i], shuffled[j]] = [shuffled[j], shuffled[i]];
        }
        return shuffled;
    }

    dealTileTo(seat) {
        if (this.deck.length === 0) return null;
        let tile = this.deck.pop();

        // Check if Flower/Season (Bonus)
        while (tile && tile.isBonus) {
            this.bonusTiles[seat].push(tile);
            if (this.deck.length === 0) break;
            tile = this.deck.pop(); // Replacement draw
        }

        if (tile && !tile.isBonus) {
            this.hands[seat].push(tile);
        }
        return tile;
    }

    playerDraw(seat) {
        if (this.deck.length === 0) {
            this.endRoundDraw("Tembok Ubin Habis (Draw / Ryuukyoku)");
            return null;
        }

        const drawnTile = this.dealTileTo(seat);
        if (drawnTile) {
            this.hands[seat] = sortTiles(this.hands[seat]);
        }
        return drawnTile;
    }

    playerDiscard(seat, tileId) {
        const hand = this.hands[seat];
        const tileIdx = hand.findIndex(t => t.id === tileId);
        if (tileIdx === -1) return null;

        const discarded = hand.splice(tileIdx, 1)[0];
        this.discards[seat].push(discarded);
        this.lastDiscardedTile = discarded;
        this.lastDiscarderSeat = seat;

        return discarded;
    }

    /**
     * Mengecek apakah pemain lain berhak melakukan Pong, Kong, Chow, atau Hu dari ubin yang baru dibuang
     */
    checkActionPrompts(discardedTile, discarderSeat) {
        const prompts = {};

        for (let seat = 0; seat < 4; seat++) {
            if (seat === discarderSeat) continue;

            const hand = this.hands[seat];
            const canWin = this.checkWinningHand(hand, discardedTile);
            const canPong = this.checkPong(hand, discardedTile);
            const canKong = this.checkKong(hand, discardedTile);
            
            // Chow hanya boleh dilakukan oleh pemain persis di sebelah kanan discarder (Next Seat)
            const isNextSeat = (discarderSeat + 1) % 4 === seat;
            const chowOptions = isNextSeat ? this.checkChow(hand, discardedTile) : [];
            const canChow = chowOptions.length > 0;

            if (canWin || canKong || canPong || canChow) {
                prompts[seat] = { canWin, canKong, canPong, canChow, chowOptions };
            }
        }

        return prompts;
    }

    // 1. PONG (3 Ubin Sama)
    checkPong(hand, tile) {
        if (!tile) return false;
        const matching = hand.filter(t => t.suit === tile.suit && t.value === tile.value);
        return matching.length >= 2;
    }

    // 2. KONG (4 Ubin Sama)
    checkKong(hand, tile) {
        if (!tile) return false;
        const matching = hand.filter(t => t.suit === tile.suit && t.value === tile.value);
        return matching.length >= 3;
    }

    // 3. CHOW (Urutan 3 Angka berurutan)
    checkChow(hand, tile) {
        if (!tile) return [];
        if (tile.suit === SUITS.WIND || tile.suit === SUITS.DRAGON || tile.isBonus) {
            return [];
        }

        const v = tile.value;
        const s = tile.suit;
        const options = [];

        const hasVal = (val) => hand.some(t => t.suit === s && t.value === val);

        if (v >= 3 && hasVal(v - 2) && hasVal(v - 1)) {
            options.push([v - 2, v - 1, v]);
        }
        if (v >= 2 && v <= 8 && hasVal(v - 1) && hasVal(v + 1)) {
            options.push([v - 1, v, v + 1]);
        }
        if (v <= 7 && hasVal(v + 1) && hasVal(v + 2)) {
            options.push([v, v + 1, v + 2]);
        }

        return options;
    }

    /**
     * 4. HU / MENANG (14 Ubin: 4 Melds + 1 Pair, atau 7 Pairs)
     */
    checkWinningHand(hand, targetTile = null) {
        const fullHand = targetTile ? [...hand, targetTile] : [...hand];
        if (fullHand.length % 3 !== 2) return false;

        const sorted = sortTiles(fullHand);

        // A. Cek Seven Pairs
        if (sorted.length === 14 && this.isSevenPairs(sorted)) {
            return true;
        }

        // B. Cek Standar 4 Melds + 1 Pair
        return this.canFormMeldsAndPair(sorted);
    }

    isSevenPairs(tiles) {
        for (let i = 0; i < 14; i += 2) {
            if (tiles[i].suit !== tiles[i + 1].suit || tiles[i].value !== tiles[i + 1].value) {
                return false;
            }
        }
        return true;
    }

    canFormMeldsAndPair(tiles) {
        const uniqueTiles = [];
        for (let i = 0; i < tiles.length; i++) {
            if (!uniqueTiles.some(t => t.suit === tiles[i].suit && t.value === tiles[i].value)) {
                uniqueTiles.push(tiles[i]);
            }
        }

        for (const candidate of uniqueTiles) {
            const pairMatches = tiles.filter(t => t.suit === candidate.suit && t.value === candidate.value);
            if (pairMatches.length >= 2) {
                const remaining = [...tiles];
                const idx1 = remaining.findIndex(t => t.suit === candidate.suit && t.value === candidate.value);
                remaining.splice(idx1, 1);
                const idx2 = remaining.findIndex(t => t.suit === candidate.suit && t.value === candidate.value);
                remaining.splice(idx2, 1);

                if (this.canDecomposeIntoMelds(remaining)) {
                    return true;
                }
            }
        }

        return false;
    }

    canDecomposeIntoMelds(tiles) {
        if (tiles.length === 0) return true;

        const first = tiles[0];

        // 1. Coba Triplet
        const tripletMatches = tiles.filter(t => t.suit === first.suit && t.value === first.value);
        if (tripletMatches.length >= 3) {
            const nextTiles = [...tiles];
            for (let i = 0; i < 3; i++) {
                const idx = nextTiles.findIndex(t => t.suit === first.suit && t.value === first.value);
                nextTiles.splice(idx, 1);
            }
            if (this.canDecomposeIntoMelds(nextTiles)) return true;
        }

        // 2. Coba Sequence
        if (first.suit === SUITS.CHARACTER || first.suit === SUITS.BAMBOO || first.suit === SUITS.DOT) {
            const v = first.value;
            const idxV1 = tiles.findIndex(t => t.suit === first.suit && t.value === v + 1);
            const idxV2 = tiles.findIndex(t => t.suit === first.suit && t.value === v + 2);

            if (idxV1 !== -1 && idxV2 !== -1) {
                const nextTiles = [...tiles];
                nextTiles.splice(0, 1);
                const n1 = nextTiles.findIndex(t => t.suit === first.suit && t.value === v + 1);
                nextTiles.splice(n1, 1);
                const n2 = nextTiles.findIndex(t => t.suit === first.suit && t.value === v + 2);
                nextTiles.splice(n2, 1);

                if (this.canDecomposeIntoMelds(nextTiles)) return true;
            }
        }

        return false;
    }

    executePong(seat, targetTile) {
        const hand = this.hands[seat];
        const removed = [];
        for (let i = 0; i < 2; i++) {
            const idx = hand.findIndex(t => t.suit === targetTile.suit && t.value === targetTile.value);
            if (idx !== -1) removed.push(hand.splice(idx, 1)[0]);
        }
        const meld = { type: "PONG", tiles: [...removed, targetTile] };
        this.melds[seat].push(meld);
        this.currentTurnSeat = seat;

        // Hapus ubin yang diklaim dari kolam buangan (river)
        if (this.lastDiscarderSeat !== null) {
            const river = this.discards[this.lastDiscarderSeat];
            const dIdx = river.findIndex(t => t.id === targetTile.id);
            if (dIdx !== -1) {
                river.splice(dIdx, 1);
            }
        }

        return meld;
    }

    executeChow(seat, targetTile, sequence) {
        const hand = this.hands[seat];
        const removed = [];
        const requiredValues = sequence.filter(v => v !== targetTile.value);

        requiredValues.forEach(val => {
            const idx = hand.findIndex(t => t.suit === targetTile.suit && t.value === val);
            if (idx !== -1) removed.push(hand.splice(idx, 1)[0]);
        });

        const meld = { type: "CHOW", tiles: [...removed, targetTile].sort((a, b) => a.value - b.value) };
        this.melds[seat].push(meld);
        this.currentTurnSeat = seat;

        // Hapus ubin yang diklaim dari river
        if (this.lastDiscarderSeat !== null) {
            const river = this.discards[this.lastDiscarderSeat];
            const dIdx = river.findIndex(t => t.id === targetTile.id);
            if (dIdx !== -1) {
                river.splice(dIdx, 1);
            }
        }

        return meld;
    }

    executeKong(seat, targetTile) {
        const hand = this.hands[seat];
        const removed = [];
        for (let i = 0; i < 3; i++) {
            const idx = hand.findIndex(t => t.suit === targetTile.suit && t.value === targetTile.value);
            if (idx !== -1) removed.push(hand.splice(idx, 1)[0]);
        }
        const meld = { type: "KONG", tiles: [...removed, targetTile] };
        this.melds[seat].push(meld);
        this.currentTurnSeat = seat;

        if (this.lastDiscarderSeat !== null) {
            const river = this.discards[this.lastDiscarderSeat];
            const dIdx = river.findIndex(t => t.id === targetTile.id);
            if (dIdx !== -1) {
                river.splice(dIdx, 1);
            }
        }

        // Kong memberikan 1 draw bonus
        this.playerDraw(seat);
        return meld;
    }

    nextTurn() {
        this.currentTurnSeat = (this.currentTurnSeat + 1) % 4;
        return this.currentTurnSeat;
    }

    /**
     * Menghitung Skor Berdasarkan MAHJONG CUSTOM Rule Book v1.0 (Section 7, 20-24)
     */
    calculateScore(winnerSeat, isSelfDraw, winTile = null) {
        const hand = this.hands[winnerSeat] || [];
        const melds = this.melds[winnerSeat] || [];
        const bonuses = this.bonusTiles[winnerSeat] || [];

        let totalScore = 100; // Base Win (+100)
        const breakdown = [{ name: "Base Win (Menang Dasar)", points: 100 }];

        // 1. Self Draw (+30) vs Discard Win (+20)
        if (isSelfDraw) {
            totalScore += 30;
            breakdown.push({ name: "Self Draw Win (Tsumo)", points: 30 });
        } else {
            totalScore += 20;
            breakdown.push({ name: "Discard Win (Ron)", points: 20 });
        }

        const fullTiles = winTile && !isSelfDraw ? [...hand, winTile] : [...hand];

        // 2. Seven Pairs (+100)
        if (fullTiles.length === 14 && this.isSevenPairs(sortTiles(fullTiles))) {
            totalScore += 100;
            breakdown.push({ name: "Seven Pairs (7 Pasang)", points: 100 });
        }

        // 3. All Triplets (+50)
        if (this.isAllTriplets(fullTiles, melds)) {
            totalScore += 50;
            breakdown.push({ name: "All Triplets (Semua Triplet/Pong)", points: 50 });
        }

        // 4. Pure Suit (+100)
        if (this.isPureSuit(fullTiles, melds)) {
            totalScore += 100;
            breakdown.push({ name: "Pure Suit (Satu Warna)", points: 100 });
        }

        // 5. Flower & Season Bonuses (+10 per ubin, +50 set 4, +150 set 8)
        const flowerCount = bonuses.filter(b => b.suit === SUITS.FLOWER).length;
        const seasonCount = bonuses.filter(b => b.suit === SUITS.SEASON).length;
        const totalBonus = bonuses.length;

        if (totalBonus > 0) {
            const indPoints = totalBonus * 10;
            totalScore += indPoints;
            breakdown.push({ name: `Bonus ${totalBonus} Ubin Bunga & Musim`, points: indPoints });
        }

        if (flowerCount === 4) {
            totalScore += 50;
            breakdown.push({ name: "Full Flower Set (4 Bunga Lengkap)", points: 50 });
        }

        if (seasonCount === 4) {
            totalScore += 50;
            breakdown.push({ name: "Full Season Set (4 Musim Lengkap)", points: 50 });
        }

        if (totalBonus === 8) {
            totalScore += 150;
            breakdown.push({ name: "Full 8 Bonus (8 Bunga & Musim Sempurna)", points: 150 });
        }

        return { totalScore, breakdown };
    }

    isAllTriplets(hand, melds) {
        // Melds harus semuanya Pong atau Kong (tidak ada Chow)
        if (melds.some(m => m.type === "CHOW")) return false;

        // Di tangan tertutup harus ada 1 pair dan sisa triplets
        const unique = [];
        for (const t of hand) {
            if (!unique.some(u => u.suit === t.suit && u.value === t.value)) {
                unique.push(t);
            }
        }

        let pairFound = false;
        for (const u of unique) {
            const count = hand.filter(t => t.suit === u.suit && t.value === u.value).length;
            if (count === 2 && !pairFound) {
                pairFound = true;
            } else if (count !== 3 && count !== 4 && count !== 0) {
                return false;
            }
        }

        return pairFound;
    }

    isPureSuit(hand, melds) {
        const allTiles = [...hand];
        melds.forEach(m => allTiles.push(...m.tiles));

        if (allTiles.length === 0) return false;
        const firstSuit = allTiles[0].suit;

        // Harus berupa Character, Bamboo, atau Dot (tanpa Wind, Dragon, Bonus)
        if (firstSuit !== SUITS.CHARACTER && firstSuit !== SUITS.BAMBOO && firstSuit !== SUITS.DOT) {
            return false;
        }

        return allTiles.every(t => t.suit === firstSuit);
    }

    endRoundWin(winnerSeat, winTile = null, isSelfDraw = false) {
        this.isRoundActive = false;
        const scoreData = this.calculateScore(winnerSeat, isSelfDraw, winTile);

        if (this.onRoundEndCallback) {
            this.onRoundEndCallback({
                result: "WIN",
                winnerSeat,
                winTile,
                isSelfDraw,
                scoreData,
                hands: this.hands
            });
        }
    }

    endRoundDraw(reason) {
        this.isRoundActive = false;
        if (this.onRoundEndCallback) {
            this.onRoundEndCallback({
                result: "DRAW",
                reason,
                hands: this.hands
            });
        }
    }
}
