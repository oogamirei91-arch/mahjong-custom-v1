package engine

import (
	"testing"
)

func TestAIBotDiscardIsolatedHonor(t *testing.T) {
	bot := NewAIBot("bot_1", "Master_Bot", SeatEast, AIDifficultyMaster)

	// Tangan bot: 1-9 Dots berurutan + 1 ubin Angin Timur (terisolasi)
	bot.Player.Hand = []Tile{
		NewTile(0, SuitDot, 1),
		NewTile(1, SuitDot, 2),
		NewTile(2, SuitDot, 3),
		NewTile(3, SuitDot, 4),
		NewTile(4, SuitDot, 5),
		NewTile(5, SuitDot, 6),
		NewTile(6, SuitDot, 7),
		NewTile(7, SuitDot, 8),
		NewTile(8, SuitDot, 9),
		NewTile(9, SuitWind, 1), // Harus dibuang lebih dulu
	}

	discard := bot.DecideDiscard()
	if discard.Suit != SuitWind || discard.Value != 1 {
		t.Fatalf("Bot gagal memilih ubin honor terisolasi untuk dibuang! Terpilih: %+v", discard)
	}
}

func TestAIBotReactionWinPriority(t *testing.T) {
	bot := NewAIBot("bot_2", "Expert_Bot", SeatSouth, AIDifficultyExpert)
	discardedTile := NewTile(99, SuitBamboo, 5)

	// Jika canWin = true, bot WAJIB memilih reaksi "win"
	reaction, _ := bot.DecideReaction(discardedTile, true, true, false, true)
	if reaction != AIReactionWin {
		t.Fatalf("Bot tidak memprioritaskan kemenangan (Win)! Terpilih: %s", reaction)
	}
}
