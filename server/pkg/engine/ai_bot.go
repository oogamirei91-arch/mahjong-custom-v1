package engine

import (
	"math/rand"
	"time"
)

// AIDifficulty mendefinisikan tingkat kepintaran bot AI Mahjong.
type AIDifficulty int

const (
	AIDifficultyNovice AIDifficulty = 0 // Pemula: Buang ubin terisolasi acak
	AIDifficultyExpert AIDifficulty = 1 // Mahir: Kalkulasi efisiensi Shanten standar
	AIDifficultyMaster AIDifficulty = 2 // Master VIP: Optimal Shanten & defensif
)

// AIBot merepresentasikan agen kecerdasan buatan pemain Mahjong.
type AIBot struct {
	Player     *Player
	Difficulty AIDifficulty
}

// NewAIBot membuat instansiasi bot AI baru.
func NewAIBot(userID, username string, seat PlayerSeat, difficulty AIDifficulty) *AIBot {
	p := NewPlayer(userID, username, seat)
	p.IsBot = true
	return &AIBot{
		Player:     p,
		Difficulty: difficulty,
	}
}

// DecideDiscard memilih ubin terbaik dari tangan untuk dibuang berdasarkan heuristik efisiensi ubin.
func (bot *AIBot) DecideDiscard() Tile {
	hand := bot.Player.Hand
	if len(hand) == 0 {
		return Tile{}
	}

	counts := BuildCountsMatrix(hand)

	// 1. Prioritas Buangan Tertinggi: Ubin Angin / Naga (Honors) yang sendirian (isolasian, count == 1)
	for i, t := range hand {
		if (t.Suit == SuitWind || t.Suit == SuitDragon) && counts[t.Suit][t.Value] == 1 {
			return hand[i]
		}
	}

	// 2. Prioritas Kedua: Ubin Terminal (Nilai 1 atau 9) yang terisolasi tanpa tetangga (jarak > 2)
	for i, t := range hand {
		if t.Suit <= SuitCharacter && (t.Value == 1 || t.Value == 9) && counts[t.Suit][t.Value] == 1 {
			hasNeighbor := false
			if t.Value == 1 && (counts[t.Suit][2] > 0 || counts[t.Suit][3] > 0) {
				hasNeighbor = true
			}
			if t.Value == 9 && (counts[t.Suit][8] > 0 || counts[t.Suit][7] > 0) {
				hasNeighbor = true
			}
			if !hasNeighbor {
				return hand[i]
			}
		}
	}

	// 3. Prioritas Ketiga: Ubin biasa yang tidak memiliki pasangan dan tidak memiliki koneksi urutan
	var isolatedCandidates []Tile
	for _, t := range hand {
		if counts[t.Suit][t.Value] == 1 {
			hasSequenceConnect := false
			v := t.Value
			s := t.Suit
			if s <= SuitCharacter {
				if (v > 1 && counts[s][v-1] > 0) || (v < 9 && counts[s][v+1] > 0) ||
					(v > 2 && counts[s][v-2] > 0) || (v < 8 && counts[s][v+2] > 0) {
					hasSequenceConnect = true
				}
			}
			if !hasSequenceConnect {
				isolatedCandidates = append(isolatedCandidates, t)
			}
		}
	}

	if len(isolatedCandidates) > 0 {
		return isolatedCandidates[rand.Intn(len(isolatedCandidates))]
	}

	// 4. Fallback: Pilih ubin terakhir yang baru saja di-draw
	return hand[len(hand)-1]
}

// AIReactionType mendefinisikan jenis aksi respon bot terhadap buangan lawan.
type AIReactionType string

const (
	AIReactionPass AIReactionType = "pass"
	AIReactionChow AIReactionType = "chow"
	AIReactionPong AIReactionType = "pong"
	AIReactionKong AIReactionType = "kong"
	AIReactionWin  AIReactionType = "win"
)

// DecideReaction memutuskan respon bot terhadap ubin yang dibuang lawan.
func (bot *AIBot) DecideReaction(discardedTile Tile, canChow, canPong, canKong, canWin bool) (AIReactionType, []int) {
	// 1. Jika bisa menang (Hu / Ron) -> 100% Ambil kemenangan!
	if canWin {
		return AIReactionWin, nil
	}

	// 2. Jika bisa Kong (4 ubin identik) -> Ambil Kong dengan probabilitas tinggi
	if canKong {
		var matchedIDs []int
		for _, t := range bot.Player.Hand {
			if t.Suit == discardedTile.Suit && t.Value == discardedTile.Value {
				matchedIDs = append(matchedIDs, t.ID)
			}
		}
		if len(matchedIDs) == 3 {
			return AIReactionKong, matchedIDs
		}
	}

	// 3. Jika bisa Pong (3 ubin identik) -> Ambil jika ubin bernilai tinggi (Honors / Dragons) atau acak cerdas
	if canPong {
		shouldPong := (discardedTile.Suit == SuitWind || discardedTile.Suit == SuitDragon) || (rand.Float32() < 0.65)
		if shouldPong {
			var matchedIDs []int
			for _, t := range bot.Player.Hand {
				if t.Suit == discardedTile.Suit && t.Value == discardedTile.Value {
					matchedIDs = append(matchedIDs, t.ID)
					if len(matchedIDs) == 2 {
						break
					}
				}
			}
			if len(matchedIDs) == 2 {
				return AIReactionPong, matchedIDs
			}
		}
	}

	// 4. Jika bisa Chow (Urutan berurutan dari pemain sebelah kiri)
	if canChow && rand.Float32() < 0.50 {
		return AIReactionChow, nil
	}

	// Default: Lewatkan (Pass)
	return AIReactionPass, nil
}

// GetSimulatedThinkingDuration memberikan jeda berpikir manusiawi (1.0 - 2.5 detik).
func (bot *AIBot) GetSimulatedThinkingDuration() time.Duration {
	baseMs := 1200
	varianceMs := rand.Intn(1000)
	return time.Duration(baseMs+varianceMs) * time.Millisecond
}
