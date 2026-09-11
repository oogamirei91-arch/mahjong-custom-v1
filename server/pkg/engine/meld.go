package engine

import "fmt"

// MeldType mendefinisikan jenis kombinasi meld dalam Mahjong Custom v1.0.
type MeldType int

const (
	MeldTypeChow          MeldType = 0 // Tiga tile berurutan dari suit yang sama (misal 1-2-3 Bamboo)
	MeldTypePong          MeldType = 1 // Tiga tile kembar identik (misal 5-5-5 Dot atau Red Dragon x3)
	MeldTypeKongConcealed MeldType = 2 // Empat tile kembar di tangan sendiri (Concealed Kong)
	MeldTypeKongOpen      MeldType = 3 // Empat tile kembar dari discard pemain lain (Open Kong)
	MeldTypeKongAdded     MeldType = 4 // Menambahkan tile ke-4 pada Pong yang sudah terbuka (Added Kong)
	MeldTypePair          MeldType = 5 // Pasangan dua tile kembar (Mata / Eye)
)

func (m MeldType) String() string {
	switch m {
	case MeldTypeChow:
		return "Chow"
	case MeldTypePong:
		return "Pong"
	case MeldTypeKongConcealed:
		return "Concealed-Kong"
	case MeldTypeKongOpen:
		return "Open-Kong"
	case MeldTypeKongAdded:
		return "Added-Kong"
	case MeldTypePair:
		return "Pair"
	default:
		return fmt.Sprintf("MeldType(%d)", m)
	}
}

// Meld merepresentasikan satu set kombinasi kartu yang sudah terbentuk.
type Meld struct {
	Type        MeldType   `json:"type"`          // Chow, Pong, Kong, atau Pair
	Tiles       []Tile     `json:"tiles"`         // Daftar keping tile di dalam meld (2, 3, atau 4 tile)
	SourceSeat  PlayerSeat `json:"source_seat"`   // Kursi pemain asal tile jika diambil dari discard
	TargetTile  Tile       `json:"target_tile"`   // Tile spesifik yang diklaim dari discard
	IsConcealed bool       `json:"is_concealed"`  // True jika kombinasi tertutup di tangan pemain
}

// NewMeld membuat instansiasi Meld baru.
func NewMeld(meldType MeldType, tiles []Tile, isConcealed bool, sourceSeat PlayerSeat, targetTile Tile) Meld {
	return Meld{
		Type:        meldType,
		Tiles:       tiles,
		IsConcealed: isConcealed,
		SourceSeat:  sourceSeat,
		TargetTile:  targetTile,
	}
}

// String mengembalikan representasi teks dari kombinasi meld.
func (m Meld) String() string {
	return fmt.Sprintf("[%s: %v]", m.Type, m.Tiles)
}
