package engine

import (
	"testing"
)

func setupTestRoom() *Room {
	room := NewRoom("ROOM-TEST-1")
	p1 := NewPlayer("u1", "Budi", SeatEast)
	p2 := NewPlayer("u2", "Andi", SeatSouth)
	p3 := NewPlayer("u3", "Citra", SeatWest)
	p4 := NewPlayer("u4", "Dewi", SeatNorth)

	room.AddPlayer(p1, SeatEast)
	room.AddPlayer(p2, SeatSouth)
	room.AddPlayer(p3, SeatWest)
	room.AddPlayer(p4, SeatNorth)

	return room
}

func TestRoomInitializationAndDeal(t *testing.T) {
	room := setupTestRoom()
	err := room.StartMatch()
	if err != nil {
		t.Fatalf("Gagal memulai match: %v", err)
	}

	if room.Phase != PhaseTurnActive {
		t.Errorf("Phase seharusnya PhaseTurnActive, didapat %s", room.Phase)
	}

	if room.ActiveSeat != SeatEast {
		t.Errorf("Active seat awal seharusnya East, didapat %s", room.ActiveSeat)
	}

	// Dealer (East) harus memiliki 14 kartu aktif
	if len(room.Players[SeatEast].Hand) != 14 {
		t.Errorf("East seharusnya punya 14 tile aktif, didapat %d", len(room.Players[SeatEast].Hand))
	}

	// South, West, North masing-masing harus memiliki 13 kartu aktif
	for _, seat := range []PlayerSeat{SeatSouth, SeatWest, SeatNorth} {
		if len(room.Players[seat].Hand) != 13 {
			t.Errorf("%s seharusnya punya 13 tile aktif, didapat %d", seat, len(room.Players[seat].Hand))
		}
	}
}

func TestExecuteDiscardAndDraw(t *testing.T) {
	room := setupTestRoom()
	_ = room.StartMatch()

	// East melakukan Discard tile pertama di tangannya
	eastHand := room.Players[SeatEast].Hand
	discardTileID := eastHand[0].ID

	discarded, _, err := room.ExecuteDiscard(SeatEast, discardTileID)
	if err != nil {
		t.Fatalf("Gagal melakukan discard: %v", err)
	}
	if discarded.ID != discardTileID {
		t.Errorf("Tile yang dibuang salah")
	}

	// Jumlah tile East sekarang harus 13
	if len(room.Players[SeatEast].Hand) != 13 {
		t.Errorf("East seharusnya bersisa 13 tile, didapat %d", len(room.Players[SeatEast].Hand))
	}

	// Jika tidak ada reaksi, giliran harus berpindah ke South
	if room.Phase == PhaseTurnActive {
		if room.ActiveSeat != SeatSouth {
			t.Errorf("Giliran seharusnya berpindah ke South, didapat %s", room.ActiveSeat)
		}

		// South melakukan Draw
		drawn, _, drawErr := room.ExecuteDraw(SeatSouth)
		if drawErr != nil {
			t.Fatalf("South gagal melakukan draw: %v", drawErr)
		}
		if drawn.IsBonus {
			t.Errorf("Draw tidak boleh menghasilkan tile bonus di tangan aktif")
		}

		// Tangan South sekarang harus 14 tile
		if len(room.Players[SeatSouth].Hand) != 14 {
			t.Errorf("South seharusnya memiliki 14 tile setelah draw, didapat %d", len(room.Players[SeatSouth].Hand))
		}
	}
}

func TestAutoDiscardSelection(t *testing.T) {
	hand := []Tile{
		NewTile(1, SuitBamboo, 1),
		NewTile(2, SuitBamboo, 2),
		NewTile(3, SuitDot, 9),
	}

	autoTile, ok := SelectAutoDiscardTile(hand)
	if !ok || autoTile.ID != 3 {
		t.Errorf("Auto discard seharusnya memilih tile paling kanan (ID 3)")
	}
}
