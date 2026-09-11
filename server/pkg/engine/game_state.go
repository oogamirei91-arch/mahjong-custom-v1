package engine

import "fmt"

// GamePhase mendefinisikan tahapan status permainan di dalam sebuah Room.
type GamePhase int

const (
	PhaseLobby             GamePhase = 0 // Menunggu pemain bergabung
	PhaseDealing           GamePhase = 1 // Membagikan kartu dan menukar Flower/Season
	PhaseTurnActive        GamePhase = 2 // Giliran aktif pemain (Draw -> Pikirkan Discard, timer 15s)
	PhaseAwaitingReactions GamePhase = 3 // Menunggu respon aksi pemain lain saat ada discard (timer 5s)
	PhaseRoundEnded        GamePhase = 4 // Ronde selesai (Ada WIN atau Wall habis/Draw)
	PhaseMatchEnded        GamePhase = 5 // Pertandingan 4 ronde selesai, penentuan Match Winner
)

func (p GamePhase) String() string {
	switch p {
	case PhaseLobby:
		return "Lobby"
	case PhaseDealing:
		return "Dealing"
	case PhaseTurnActive:
		return "TurnActive"
	case PhaseAwaitingReactions:
		return "AwaitingReactions"
	case PhaseRoundEnded:
		return "RoundEnded"
	case PhaseMatchEnded:
		return "MatchEnded"
	default:
		return fmt.Sprintf("Phase(%d)", p)
	}
}

const (
	TurnTimeoutSeconds      = 15 // Waktu berpikir giliran normal (15 detik)
	ReactionTimeoutSeconds  = 5  // Jendela waktu respon aksi Chow/Pong/Kong/Win (5 detik)
	ReconnectTimeoutSeconds = 30 // Batas waktu reconnect pemain (30 detik)
	TotalRoundsPerMatch     = 4  // Jumlah ronde per 1 match (4 ronde)
)

// ActionClaim mencatat klaim aksi yang diajukan pemain.
type ActionClaim struct {
	Seat      PlayerSeat `json:"seat"`
	Action    string     `json:"action"`     // "WIN", "KONG", "PONG", "CHOW", "PASS"
	MeldTiles []Tile     `json:"meld_tiles"` // Tile tangan yang dipakai untuk meld
}

// SelectAutoDiscardTile memilih tile yang akan dibuang secara otomatis jika pemain kehabisan waktu 15 detik.
// Prioritas: buang tile terakhir yang baru saja di-draw (ujung kanan hand).
func SelectAutoDiscardTile(hand []Tile) (Tile, bool) {
	if len(hand) == 0 {
		return Tile{}, false
	}
	// Buang tile terakhir di tangan
	return hand[len(hand)-1], true
}
