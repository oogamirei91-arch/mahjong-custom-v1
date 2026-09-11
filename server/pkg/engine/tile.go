package engine

import "fmt"

// Suit mendefinisikan jenis / kelompok tile pada Mahjong Custom v1.0.
type Suit int

const (
	SuitBamboo    Suit = 0 // Bambu (1-9)
	SuitDot       Suit = 1 // Lingkaran / Circle (1-9)
	SuitCharacter Suit = 2 // Karakter / Wan (1-9)
	SuitWind      Suit = 3 // Angin (East, South, West, North)
	SuitDragon    Suit = 4 // Naga (Red, Green, White)
	SuitFlower    Suit = 5 // Bunga Bonus (Plum, Orchid, Chrysanthemum, Bamboo)
	SuitSeason    Suit = 6 // Musim Bonus (Spring, Summer, Autumn, Winter)
)

// Konstanta nilai untuk Wind (Angin)
const (
	WindEast  = 1 // Timur (Dealer Awal)
	WindSouth = 2 // Selatan
	WindWest  = 3 // Barat
	WindNorth = 4 // Utara
)

// Konstanta nilai untuk Dragon (Naga)
const (
	DragonRed   = 1 // Naga Merah (Zhong / 中)
	DragonGreen = 2 // Naga Hijau (Fa / 發)
	DragonWhite = 3 // Naga Putih (Bai / 白)
)

// Konstanta nilai untuk Flower (Bunga)
const (
	FlowerPlum          = 1 // Bunga Prem (Mei / 梅)
	FlowerOrchid        = 2 // Bunga Anggrek (Lan / 蘭)
	FlowerChrysanthemum = 3 // Bunga Krisan (Ju / 菊)
	FlowerBamboo        = 4 // Bunga Bambu (Zhu / 竹)
)

// Konstanta nilai untuk Season (Musim)
const (
	SeasonSpring = 1 // Musim Semi (Chun / 春)
	SeasonSummer = 2 // Musim Panas (Xia / 夏)
	SeasonAutumn = 3 // Musim Gugur (Qiu / 秋)
	SeasonWinter = 4 // Musim Dingin (Dong / 冬)
)

// Tile merepresentasikan satu keping batu Mahjong fisik di dalam permainan.
// Setiap keping memiliki ID unik dari 0 hingga 143 untuk pelacakan individual.
type Tile struct {
	ID      int  `json:"id"`       // ID unik (0 - 143)
	Suit    Suit `json:"suit"`     // Kelompok Suit tile
	Value   int  `json:"value"`    // Nilai angka (1-9 untuk number, 1-4 untuk wind/flower/season, 1-3 untuk dragon)
	IsBonus bool `json:"is_bonus"` // True jika merupakan Flower atau Season
}

// NewTile membuat instansiasi Tile baru.
func NewTile(id int, suit Suit, value int) Tile {
	isBonus := (suit == SuitFlower || suit == SuitSeason)
	return Tile{
		ID:      id,
		Suit:    suit,
		Value:   value,
		IsBonus: isBonus,
	}
}

// IsEqual memeriksa apakah dua tile memiliki jenis dan nilai yang sama (mengabaikan ID unik).
func (t Tile) IsEqual(other Tile) bool {
	return t.Suit == other.Suit && t.Value == other.Value
}

// IsHonor memeriksa apakah tile merupakan kategori Honor (Wind atau Dragon).
func (t Tile) IsHonor() bool {
	return t.Suit == SuitWind || t.Suit == SuitDragon
}

// IsNumberTile memeriksa apakah tile merupakan tile angka (Bamboo, Dot, atau Character).
func (t Tile) IsNumberTile() bool {
	return t.Suit == SuitBamboo || t.Suit == SuitDot || t.Suit == SuitCharacter
}

// String mengembalikan representasi teks yang mudah dibaca dari sebuah tile.
func (t Tile) String() string {
	switch t.Suit {
	case SuitBamboo:
		return fmt.Sprintf("%d-Bamboo", t.Value)
	case SuitDot:
		return fmt.Sprintf("%d-Dot", t.Value)
	case SuitCharacter:
		return fmt.Sprintf("%d-Character", t.Value)
	case SuitWind:
		winds := map[int]string{WindEast: "East-Wind", WindSouth: "South-Wind", WindWest: "West-Wind", WindNorth: "North-Wind"}
		return winds[t.Value]
	case SuitDragon:
		dragons := map[int]string{DragonRed: "Red-Dragon", DragonGreen: "Green-Dragon", DragonWhite: "White-Dragon"}
		return dragons[t.Value]
	case SuitFlower:
		flowers := map[int]string{FlowerPlum: "Flower-Plum", FlowerOrchid: "Flower-Orchid", FlowerChrysanthemum: "Flower-Chrysanthemum", FlowerBamboo: "Flower-Bamboo"}
		return flowers[t.Value]
	case SuitSeason:
		seasons := map[int]string{SeasonSpring: "Season-Spring", SeasonSummer: "Season-Summer", SeasonAutumn: "Season-Autumn", SeasonWinter: "Season-Winter"}
		return seasons[t.Value]
	default:
		return fmt.Sprintf("Unknown-Tile(%d)", t.ID)
	}
}
