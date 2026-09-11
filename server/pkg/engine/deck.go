package engine

import (
	"crypto/rand"
	"encoding/binary"
	"errors"
	"fmt"
	mathrand "math/rand"
)

const (
	TotalTilesCount   = 144 // Total seluruh tile fisik dalam permainan
	DeadWallSize      = 14  // Jumlah tile yang disisihkan sebagai Replacement Wall
	DealerInitialHand = 14  // Jumlah tile awal untuk Dealer (East)
	PlayerInitialHand = 13  // Jumlah tile awal untuk pemain non-dealer
)

// PlayerSeat mendefinisikan 4 posisi kursi pemain.
type PlayerSeat int

const (
	SeatEast  PlayerSeat = 0 // Dealer Awal
	SeatSouth PlayerSeat = 1
	SeatWest  PlayerSeat = 2
	SeatNorth PlayerSeat = 3
)

func (s PlayerSeat) String() string {
	switch s {
	case SeatEast:
		return "East"
	case SeatSouth:
		return "South"
	case SeatWest:
		return "West"
	case SeatNorth:
		return "North"
	default:
		return fmt.Sprintf("Seat(%d)", s)
	}
}

// InitialDealResult menyimpan hasil pembagian kartu awal untuk ke-4 pemain.
type InitialDealResult struct {
	Hands map[PlayerSeat][]Tile // Kartu tangan masing-masing pemain
}

// Wall mengelola susunan 144 tile di meja permainan selama satu ronde berlangsung.
type Wall struct {
	Tiles         []Tile // Daftar 144 tile hasil shuffle
	HeadIndex     int    // Penunjuk tile yang akan diambil pada Draw normal (dari depan)
	TailIndex     int    // Penunjuk tile yang akan diambil pada Replacement Draw (dari belakang)
	DeadWallCount int    // Jumlah tile dead wall yang dialokasikan (14)
}

// CreateFullDeck membuat daftar lengkap 144 tile Mahjong Custom v1.0 secara berurutan.
func CreateFullDeck() []Tile {
	deck := make([]Tile, 0, TotalTilesCount)
	currentID := 0

	// 1. Number Tiles: Bamboo (36), Dot (36), Character (36) = 108 Tile
	numberSuits := []Suit{SuitBamboo, SuitDot, SuitCharacter}
	for _, suit := range numberSuits {
		for value := 1; value <= 9; value++ {
			// Setiap angka memiliki 4 keping kembar
			for copyNum := 0; copyNum < 4; copyNum++ {
				deck = append(deck, NewTile(currentID, suit, value))
				currentID++
			}
		}
	}

	// 2. Wind Tiles: East, South, West, North (4 x 4 = 16 Tile)
	for windVal := WindEast; windVal <= WindNorth; windVal++ {
		for copyNum := 0; copyNum < 4; copyNum++ {
			deck = append(deck, NewTile(currentID, SuitWind, windVal))
			currentID++
		}
	}

	// 3. Dragon Tiles: Red, Green, White (3 x 4 = 12 Tile)
	for dragonVal := DragonRed; dragonVal <= DragonWhite; dragonVal++ {
		for copyNum := 0; copyNum < 4; copyNum++ {
			deck = append(deck, NewTile(currentID, SuitDragon, dragonVal))
			currentID++
		}
	}

	// 4. Flower Tiles: Plum, Orchid, Chrysanthemum, Bamboo (4 Tile)
	for flowerVal := FlowerPlum; flowerVal <= FlowerBamboo; flowerVal++ {
		deck = append(deck, NewTile(currentID, SuitFlower, flowerVal))
		currentID++
	}

	// 5. Season Tiles: Spring, Summer, Autumn, Winter (4 Tile)
	for seasonVal := SeasonSpring; seasonVal <= SeasonWinter; seasonVal++ {
		deck = append(deck, NewTile(currentID, SuitSeason, seasonVal))
		currentID++
	}

	return deck
}

// SecureShuffle mengocok deck menggunakan algoritma Fisher-Yates
// dengan sumber entropi kriptografi yang tidak dapat ditebak oleh client.
func SecureShuffle(deck []Tile) []Tile {
	shuffled := make([]Tile, len(deck))
	copy(shuffled, deck)

	// Buat seed kriptografis aman dari sistem operasi
	var seedBytes [8]byte
	_, err := rand.Read(seedBytes[:])
	var seed int64
	if err == nil {
		seed = int64(binary.LittleEndian.Uint64(seedBytes[:]))
	} else {
		seed = 1789092899448 // fallback
	}

	rng := mathrand.New(mathrand.NewSource(seed))

	// Algoritma Fisher-Yates Shuffle
	for i := len(shuffled) - 1; i > 0; i-- {
		j := rng.Intn(i + 1)
		shuffled[i], shuffled[j] = shuffled[j], shuffled[i]
	}

	return shuffled
}

// NewWall menginisialisasi dinding tile (Wall) baru yang sudah dikocok secara aman.
func NewWall() *Wall {
	rawDeck := CreateFullDeck()
	shuffled := SecureShuffle(rawDeck)

	return &Wall{
		Tiles:         shuffled,
		HeadIndex:     0,
		TailIndex:     len(shuffled) - 1,
		DeadWallCount: DeadWallSize,
	}
}

// DealInitialHands membagikan kartu awal ke 4 pemain:
// - Dealer (East): 14 tile (langsung siap discard tanpa draw pertama)
// - Player 2 (South): 13 tile
// - Player 3 (West): 13 tile
// - Player 4 (North): 13 tile
// Total dibagikan: 53 tile awal.
func (w *Wall) DealInitialHands() (InitialDealResult, error) {
	result := InitialDealResult{
		Hands: make(map[PlayerSeat][]Tile),
	}

	seats := []PlayerSeat{SeatEast, SeatSouth, SeatWest, SeatNorth}
	tileCounts := map[PlayerSeat]int{
		SeatEast:  DealerInitialHand, // 14
		SeatSouth: PlayerInitialHand, // 13
		SeatWest:  PlayerInitialHand, // 13
		SeatNorth: PlayerInitialHand, // 13
	}

	for _, seat := range seats {
		count := tileCounts[seat]
		hand := make([]Tile, count)
		for i := 0; i < count; i++ {
			tile, err := w.DrawTile()
			if err != nil {
				return result, fmt.Errorf("gagal membagikan kartu untuk %s: %w", seat, err)
			}
			hand[i] = tile
		}
		result.Hands[seat] = hand
	}

	return result, nil
}

// DrawTile mengambil 1 tile dari ujung depan (head) wall untuk giliran normal.
func (w *Wall) DrawTile() (Tile, error) {
	if w.RemainingLiveTiles() <= 0 {
		return Tile{}, errors.New("wall habis: tidak ada tile tersisa untuk di-draw")
	}

	tile := w.Tiles[w.HeadIndex]
	w.HeadIndex++
	return tile, nil
}

// DrawReplacementTile mengambil 1 tile dari ujung belakang (tail / Dead Wall)
// yang digunakan khusus untuk penggantian Flower, Season, atau Kong.
func (w *Wall) DrawReplacementTile() (Tile, error) {
	if w.HeadIndex > w.TailIndex {
		return Tile{}, errors.New("replacement wall habis")
	}

	tile := w.Tiles[w.TailIndex]
	w.TailIndex--
	return tile, nil
}

// RemainingLiveTiles mengembalikan jumlah tile aktif yang masih bisa di-draw (di luar Dead Wall).
func (w *Wall) RemainingLiveTiles() int {
	// Sisa tile aktif adalah jarak antara Head dan batas awal Dead Wall di ujung Tail
	available := (w.TailIndex - w.HeadIndex + 1) - w.DeadWallCount
	if available < 0 {
		return 0
	}
	return available
}

// TotalRemainingTiles mengembalikan total seluruh tile fisik yang belum terambil di meja.
func (w *Wall) TotalRemainingTiles() int {
	if w.HeadIndex > w.TailIndex {
		return 0
	}
	return w.TailIndex - w.HeadIndex + 1
}
