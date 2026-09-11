package network

import (
	"encoding/json"
	"mahjong-server/pkg/engine"
)

// GameMode mendefinisikan mode permainan (Ranked vs Custom Room).
type GameMode string

const (
	GameModeRanked GameMode = "RANKED" // Auto-matchmaking, mempengaruhi Trophy Points & Ranking
	GameModeCustom GameMode = "CUSTOM" // Main bareng teman menggunakan kode meja / invite friend list
)

// Message adalah struktur dasar (Envelope) untuk seluruh komunikasi WebSocket.
type Message struct {
	Type    string          `json:"type"`              // Nama event / aksi
	Payload json.RawMessage `json:"payload,omitempty"` // Isi data dalam format JSON
}

// EncodeMessage membungkus tipe event dan payload menjadi slice byte JSON yang siap dikirim.
func EncodeMessage(msgType string, payload any) ([]byte, error) {
	payloadBytes, err := json.Marshal(payload)
	if err != nil {
		return nil, err
	}
	msg := Message{
		Type:    msgType,
		Payload: payloadBytes,
	}
	return json.Marshal(msg)
}

// PlayerInfo mencatat profil ringkas pemain di dalam room untuk dikirim ke client.
type PlayerInfo struct {
	Seat      engine.PlayerSeat `json:"seat"`
	Username  string            `json:"username"`
	Score     int               `json:"score"`
	TileCount int               `json:"tile_count"`
	RankTier  string            `json:"rank_tier,omitempty"`
}

// --- PAYLOAD EVENT MATCHMAKING & CUSTOM ROOM ---

// CreateCustomRoomPayload dikirim dari client untuk membuat meja mabar baru ber-kode.
type CreateCustomRoomPayload struct {
	RoomCode string `json:"room_code,omitempty"` // Opsional, bisa auto-generate di server
}

// JoinCustomRoomPayload dikirim dari client untuk bergabung ke meja mabar dengan memasukkan kode.
type JoinCustomRoomPayload struct {
	RoomCode string `json:"room_code"`
}

// CustomRoomCreatedPayload dikirim server ke Host saat meja custom berhasil dibuat.
type CustomRoomCreatedPayload struct {
	RoomCode string `json:"room_code"`
	RoomID   string `json:"room_id"`
}

// InviteFriendPayload dikirim Host untuk mengundang teman tertentu ke room meja.
type InviteFriendPayload struct {
	FriendUserID string `json:"friend_user_id"`
	RoomCode     string `json:"room_code"`
}

// FriendInviteReceivedPayload diterima oleh teman saat diundang mabar oleh Host.
type FriendInviteReceivedPayload struct {
	FromUsername string `json:"from_username"`
	RoomCode     string `json:"room_code"`
}

// --- PAYLOAD EVENT GAMEPLAY ---

// GameStartPayload dikirim ke client saat permainan 4 pemain dimulai.
type GameStartPayload struct {
	RoomID             string            `json:"room_id"`
	RoomCode           string            `json:"room_code,omitempty"`
	GameMode           GameMode          `json:"game_mode"`
	RoundNumber        int               `json:"round_number"`
	MySeat             engine.PlayerSeat `json:"my_seat"`
	DealerSeat         engine.PlayerSeat `json:"dealer_seat"`
	RemainingWallCount int               `json:"remaining_wall_count"`
	MyHand             []engine.Tile     `json:"my_hand"`
	Players            []PlayerInfo      `json:"players"`
}

// MyDrawPayload dikirim khusus ke pemain yang sedang aktif mengambil kartu dari wall.
type MyDrawPayload struct {
	Tile               engine.Tile   `json:"tile"`
	BonusCollected     []engine.Tile `json:"bonus_collected,omitempty"`
	RemainingWallCount int           `json:"remaining_wall_count"`
	TurnTimer          int           `json:"turn_timer"`
}

// PlayerDrawPayload di-broadcast ke 3 pemain lain saat ada pemain yang draw (tanpa membocorkan ID tile).
type PlayerDrawPayload struct {
	Seat               engine.PlayerSeat `json:"seat"`
	RemainingWallCount int               `json:"remaining_wall_count"`
	TurnTimer          int               `json:"turn_timer"`
}

// ActionDiscardPayload dikirim dari HP pemain ke server saat membuang kartu.
type ActionDiscardPayload struct {
	TileID int `json:"tile_id"`
}

// TileDiscardedPayload di-broadcast ke seluruh pemain saat sebuah kartu dibuang ke meja.
type TileDiscardedPayload struct {
	Seat engine.PlayerSeat `json:"seat"`
	Tile engine.Tile       `json:"tile"`
}

// ActionPromptPayload dikirim ke pemain yang berhak melakukan respon aksi saat lawan discard.
type ActionPromptPayload struct {
	TargetTile       engine.Tile       `json:"target_tile"`
	FromSeat         engine.PlayerSeat `json:"from_seat"`
	AvailableActions []string          `json:"available_actions"` // ["WIN", "KONG", "PONG", "CHOW", "PASS"]
	TimeoutSeconds   int               `json:"timeout_seconds"`   // 5 detik
}

// ClaimActionPayload dikirim dari pemain ke server untuk memilih aksi (Chow, Pong, Kong, Win, Pass).
type ClaimActionPayload struct {
	Action     string        `json:"action"`                // "WIN", "KONG", "PONG", "CHOW", "PASS"
	TargetTile engine.Tile   `json:"target_tile"`           // Tile yang diklaim
	MeldTiles  []engine.Tile `json:"meld_tiles,omitempty"`  // Tile tangan yang digunakan untuk meld
}

// ActionResolvedPayload di-broadcast ke seluruh meja saat aksi berhasil disetujui server.
type ActionResolvedPayload struct {
	Seat     engine.PlayerSeat `json:"seat"`
	Action   string            `json:"action"`
	Meld     engine.Meld       `json:"meld,omitempty"`
	FromSeat engine.PlayerSeat `json:"from_seat"`
}

// RoundEndPayload di-broadcast saat ronde berakhir (ada WIN atau Wall habis).
type RoundEndPayload struct {
	Result             string                 `json:"result"` // "WIN" atau "DRAW"
	WinnerSeat         engine.PlayerSeat      `json:"winner_seat"`
	WinnerUsername     string                 `json:"winner_username,omitempty"`
	WinType            string                 `json:"win_type,omitempty"`
	WinningHand        []engine.Tile          `json:"winning_hand,omitempty"`
	ScoreDetails       engine.ScoreBreakdown  `json:"score_details"`
	UpdatedMatchScores map[engine.PlayerSeat]int `json:"updated_match_scores"`
	NextRound          int                    `json:"next_round"`
	IsMatchOver        bool                   `json:"is_match_over"`
	TrophyEarned       int                    `json:"trophy_earned,omitempty"`
}

// ErrorPayload dikirim jika ada aksi ilegal atau error jaringan.
type ErrorPayload struct {
	Message string `json:"message"`
}
