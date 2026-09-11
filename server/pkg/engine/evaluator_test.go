package engine

import (
	"testing"
)

// Helper untuk membuat slice Tile dengan cepat
func makeHand(specs ...struct {
	s Suit
	v int
}) []Tile {
	var hand []Tile
	id := 0
	for _, spec := range specs {
		hand = append(hand, NewTile(id, spec.s, spec.v))
		id++
	}
	return hand
}

func TestStandardWin(t *testing.T) {
	// Hand 14 Tile: 123 Bamboo, 456 Bamboo, 789 Dot, 111 Dragon(Red), Pair 55 Character
	hand := makeHand(
		struct{ s Suit; v int }{SuitBamboo, 1},
		struct{ s Suit; v int }{SuitBamboo, 2},
		struct{ s Suit; v int }{SuitBamboo, 3},

		struct{ s Suit; v int }{SuitBamboo, 4},
		struct{ s Suit; v int }{SuitBamboo, 5},
		struct{ s Suit; v int }{SuitBamboo, 6},

		struct{ s Suit; v int }{SuitDot, 7},
		struct{ s Suit; v int }{SuitDot, 8},
		struct{ s Suit; v int }{SuitDot, 9},

		struct{ s Suit; v int }{SuitDragon, DragonRed},
		struct{ s Suit; v int }{SuitDragon, DragonRed},
		struct{ s Suit; v int }{SuitDragon, DragonRed},

		struct{ s Suit; v int }{SuitCharacter, 5},
		struct{ s Suit; v int }{SuitCharacter, 5},
	)

	res := CheckWin(hand, nil)
	if !res.IsWin {
		t.Fatalf("Ekspektasi Standard Win sah, namun dinyatakan kalah")
	}
	if res.WinningPairTile.Suit != SuitCharacter || res.WinningPairTile.Value != 5 {
		t.Errorf("Pair terdeteksi salah: %v", res.WinningPairTile)
	}
}

func TestSevenPairs(t *testing.T) {
	// 7 Pasangan Unik
	hand := makeHand(
		struct{ s Suit; v int }{SuitBamboo, 1}, struct{ s Suit; v int }{SuitBamboo, 1},
		struct{ s Suit; v int }{SuitBamboo, 9}, struct{ s Suit; v int }{SuitBamboo, 9},
		struct{ s Suit; v int }{SuitDot, 3}, struct{ s Suit; v int }{SuitDot, 3},
		struct{ s Suit; v int }{SuitCharacter, 5}, struct{ s Suit; v int }{SuitCharacter, 5},
		struct{ s Suit; v int }{SuitWind, WindEast}, struct{ s Suit; v int }{SuitWind, WindEast},
		struct{ s Suit; v int }{SuitDragon, DragonRed}, struct{ s Suit; v int }{SuitDragon, DragonRed},
		struct{ s Suit; v int }{SuitDragon, DragonWhite}, struct{ s Suit; v int }{SuitDragon, DragonWhite},
	)

	res := CheckWin(hand, nil)
	if !res.IsWin || !res.IsSevenPairs {
		t.Fatalf("Ekspektasi Seven Pairs sah, didapat: %+v", res)
	}
	if res.SpecialHand != SpecialHandSevenPairs {
		t.Errorf("Special hand harusnya SEVEN_PAIRS, didapat: %s", res.SpecialHand)
	}
}

func TestPureSuit(t *testing.T) {
	// 14 Tile semuanya Bamboo: 123, 456, 789, 222, Pair 88
	hand := makeHand(
		struct{ s Suit; v int }{SuitBamboo, 1}, struct{ s Suit; v int }{SuitBamboo, 2}, struct{ s Suit; v int }{SuitBamboo, 3},
		struct{ s Suit; v int }{SuitBamboo, 4}, struct{ s Suit; v int }{SuitBamboo, 5}, struct{ s Suit; v int }{SuitBamboo, 6},
		struct{ s Suit; v int }{SuitBamboo, 7}, struct{ s Suit; v int }{SuitBamboo, 8}, struct{ s Suit; v int }{SuitBamboo, 9},
		struct{ s Suit; v int }{SuitBamboo, 2}, struct{ s Suit; v int }{SuitBamboo, 2}, struct{ s Suit; v int }{SuitBamboo, 2},
		struct{ s Suit; v int }{SuitBamboo, 8}, struct{ s Suit; v int }{SuitBamboo, 8},
	)

	res := CheckWin(hand, nil)
	if !res.IsWin || !res.IsPureSuit {
		t.Fatalf("Ekspektasi Pure Suit sah, didapat: %+v", res)
	}
}

func TestAllTriplets(t *testing.T) {
	// 4 Pong + 1 Pair: 111 Bamboo, 333 Dot, 555 Character, Red Dragon x3, Pair East-East
	hand := makeHand(
		struct{ s Suit; v int }{SuitBamboo, 1}, struct{ s Suit; v int }{SuitBamboo, 1}, struct{ s Suit; v int }{SuitBamboo, 1},
		struct{ s Suit; v int }{SuitDot, 3}, struct{ s Suit; v int }{SuitDot, 3}, struct{ s Suit; v int }{SuitDot, 3},
		struct{ s Suit; v int }{SuitCharacter, 5}, struct{ s Suit; v int }{SuitCharacter, 5}, struct{ s Suit; v int }{SuitCharacter, 5},
		struct{ s Suit; v int }{SuitDragon, DragonRed}, struct{ s Suit; v int }{SuitDragon, DragonRed}, struct{ s Suit; v int }{SuitDragon, DragonRed},
		struct{ s Suit; v int }{SuitWind, WindEast}, struct{ s Suit; v int }{SuitWind, WindEast},
	)

	res := CheckWin(hand, nil)
	if !res.IsWin || !res.IsAllTriplets {
		t.Fatalf("Ekspektasi All Triplets sah, didapat: %+v", res)
	}
}

func TestCanChowAndPong(t *testing.T) {
	hand := makeHand(
		struct{ s Suit; v int }{SuitBamboo, 2},
		struct{ s Suit; v int }{SuitBamboo, 3},
		struct{ s Suit; v int }{SuitDot, 5},
		struct{ s Suit; v int }{SuitDot, 5},
	)

	// Uji Chow: Player East membuang 4 Bamboo -> South (next seat) bisa Chow (2-3-4)
	targetChow := NewTile(99, SuitBamboo, 4)
	_, canChow := CanChow(hand, targetChow, SeatEast, SeatSouth)
	if !canChow {
		t.Errorf("South seharusnya bisa Chow 4-Bamboo dari East")
	}

	// Chow dari West (bukan prev seat) harus gagal
	_, canChowWrong := CanChow(hand, targetChow, SeatWest, SeatSouth)
	if canChowWrong {
		t.Errorf("South TIDAK boleh Chow dari West")
	}

	// Uji Pong: Buangan 5 Dot -> Bisa Pong karena ada sepasang 5 Dot di tangan
	targetPong := NewTile(98, SuitDot, 5)
	if !CanPong(hand, targetPong) {
		t.Errorf("Seharusnya bisa Pong 5-Dot")
	}
}

func TestCalculateScore(t *testing.T) {
	winResult := WinResult{
		IsWin:       true,
		IsPureSuit:  true,
		SpecialHand: SpecialHandPureSuit,
	}

	// Pure Suit (+100) + Self Draw (+30) + 2 Flower (+20) + Base Win (+100) = 250 Poin
	score := CalculateScore(WinSourceSelfDraw, winResult, 2, false, false)

	if score.TotalScore != 250 {
		t.Errorf("Ekspektasi total skor 250, didapat %d", score.TotalScore)
	}
	if score.BaseWinScore != 100 || score.WinSourceScore != 30 || score.SpecialHandScore != 100 || score.FlowerBonusScore != 20 {
		t.Errorf("Rincian skor tidak sesuai: %+v", score)
	}
}
