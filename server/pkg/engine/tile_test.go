package engine

import (
	"testing"
)

// TestCreateFullDeckMemeriksa apakah jumlah 144 tile dan kombinasinya tepat sesuai Rule Book v1.0.
func TestCreateFullDeck(t *testing.T) {
	deck := CreateFullDeck()

	if len(deck) != TotalTilesCount {
		t.Fatalf("Ekspektasi %d tile, namun didapat %d", TotalTilesCount, len(deck))
	}

	suitCounts := make(map[Suit]int)
	idSet := make(map[int]bool)

	for _, tile := range deck {
		suitCounts[tile.Suit]++

		if idSet[tile.ID] {
			t.Errorf("Ditemukan duplikasi ID tile: %d", tile.ID)
		}
		idSet[tile.ID] = true
	}

	// Verifikasi komposisi
	if suitCounts[SuitBamboo] != 36 {
		t.Errorf("Ekspektasi 36 Bamboo, didapat %d", suitCounts[SuitBamboo])
	}
	if suitCounts[SuitDot] != 36 {
		t.Errorf("Ekspektasi 36 Dot, didapat %d", suitCounts[SuitDot])
	}
	if suitCounts[SuitCharacter] != 36 {
		t.Errorf("Ekspektasi 36 Character, didapat %d", suitCounts[SuitCharacter])
	}
	if suitCounts[SuitWind] != 16 {
		t.Errorf("Ekspektasi 16 Wind, didapat %d", suitCounts[SuitWind])
	}
	if suitCounts[SuitDragon] != 12 {
		t.Errorf("Ekspektasi 12 Dragon, didapat %d", suitCounts[SuitDragon])
	}
	if suitCounts[SuitFlower] != 4 {
		t.Errorf("Ekspektasi 4 Flower, didapat %d", suitCounts[SuitFlower])
	}
	if suitCounts[SuitSeason] != 4 {
		t.Errorf("Ekspektasi 4 Season, didapat %d", suitCounts[SuitSeason])
	}
}

// TestSecureShuffleMemeriksa apakah pengacakan tidak menghilangkan atau menduplikasi tile.
func TestSecureShuffle(t *testing.T) {
	deck := CreateFullDeck()
	shuffled := SecureShuffle(deck)

	if len(shuffled) != len(deck) {
		t.Fatalf("Panjang deck berubah setelah shuffle")
	}

	idMap := make(map[int]bool)
	for _, tile := range shuffled {
		idMap[tile.ID] = true
	}

	if len(idMap) != TotalTilesCount {
		t.Errorf("Shuffle menghilangkan beberapa ID tile yang unik")
	}
}

// TestDealInitialHandsMemeriksa pembagian awal ke 4 pemain dan alokasi Dead Wall.
func TestDealInitialHands(t *testing.T) {
	wall := NewWall()
	deal, err := wall.DealInitialHands()
	if err != nil {
		t.Fatalf("Gagal membagikan kartu: %v", err)
	}

	// East (Dealer) harus mendapat 14 tile
	if len(deal.Hands[SeatEast]) != 14 {
		t.Errorf("East seharusnya mendapat 14 tile, namun didapat %d", len(deal.Hands[SeatEast]))
	}

	// South, West, North masing-masing harus mendapat 13 tile
	for _, seat := range []PlayerSeat{SeatSouth, SeatWest, SeatNorth} {
		if len(deal.Hands[seat]) != 13 {
			t.Errorf("%s seharusnya mendapat 13 tile, namun didapat %d", seat, len(deal.Hands[seat]))
		}
	}

	// Total dibagikan = 14 + 13 + 13 + 13 = 53 tile.
	// Total awal = 144. Sisa total = 91 tile.
	if wall.TotalRemainingTiles() != 91 {
		t.Errorf("Ekspektasi total tile tersisa 91, namun didapat %d", wall.TotalRemainingTiles())
	}

	// Tile aktif yang bisa di-draw (di luar 14 dead wall) = 91 - 14 = 77 tile.
	if wall.RemainingLiveTiles() != 77 {
		t.Errorf("Ekspektasi tile aktif live tersisa 77, namun didapat %d", wall.RemainingLiveTiles())
	}
}
