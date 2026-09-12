package database

import (
	"crypto/sha256"
	"encoding/hex"
	"fmt"
	"time"
)

// AuthProvider mendefinisikan jenis penyedia otentikasi login.
type AuthProvider string

const (
	AuthProviderGuest  AuthProvider = "GUEST"
	AuthProviderEmail  AuthProvider = "EMAIL"
	AuthProviderGoogle AuthProvider = "GOOGLE"
	AuthProviderApple  AuthProvider = "APPLE"
)

// RankTier mendefinisikan gelar kasino pemain.
type RankTier string

const (
	RankTierNovice     RankTier = "Novice 🥉"
	RankTierApprentice RankTier = "Apprentice 🥈"
	RankTierExpert     RankTier = "Expert 🥇"
	RankTierMaster     RankTier = "Master 💎"
	RankTierVIPLegend  RankTier = "VIP Legend 👑"
)

// User merepresentasikan model data akun pemain di tabel 'users'.
type User struct {
	ID              string       `json:"id"`
	Username        string       `json:"username"`
	Email           string       `json:"email,omitempty"`
	PasswordHash    string       `json:"-"` // Disembunyikan dari JSON response
	AuthProvider    AuthProvider `json:"auth_provider"`
	OAuthProviderID string       `json:"oauth_provider_id,omitempty"`
	DisplayName     string       `json:"display_name"`
	AvatarID        int          `json:"avatar_id"`
	AvatarURL       string       `json:"avatar_url,omitempty"`
	CustomFrameID   int          `json:"custom_frame_id"`
	Role            string       `json:"role"`
	IsActive        bool         `json:"is_active"`
	IsBanned        bool         `json:"is_banned"`
	CreatedAt       time.Time    `json:"created_at"`
	UpdatedAt       time.Time    `json:"updated_at"`
	LastLogin       time.Time    `json:"last_login"`
}

// UserWallet merepresentasikan dompet chips & diamonds di tabel 'user_wallets'.
type UserWallet struct {
	UserID           string    `json:"user_id"`
	ChipsBalance     int64     `json:"chips_balance"`
	DiamondsBalance  int       `json:"diamonds_balance"`
	TotalChipsEarned int64     `json:"total_chips_earned"`
	UpdatedAt        time.Time `json:"updated_at"`
}

// SpecialHandsRecord mencatat jumlah yaku/special hands yang pernah dicapai pemain.
type SpecialHandsRecord struct {
	ThirteenOrphans    int `json:"thirteen_orphans"`
	NineGates          int `json:"nine_gates"`
	AllGreen           int `json:"all_green"`
	BigFourWinds       int `json:"big_four_winds"`
	AllHonors          int `json:"all_honors"`
	LittleFourWinds    int `json:"little_four_winds"`
	BigThreeDragons    int `json:"big_three_dragons"`
	AllTerminals       int `json:"all_terminals"`
	AllKongs           int `json:"all_kongs"`
	FourConcealedPongs int `json:"four_concealed_pongs"`
	LittleThreeDragons int `json:"little_three_dragons"`
	PureFlush          int `json:"pure_flush"`
	SevenPairs         int `json:"seven_pairs"`
	MixedFlush         int `json:"mixed_flush"`
	AllPongs           int `json:"all_pongs"`
}

// UserStats merepresentasikan statistik pemain dan trophy points di tabel 'user_stats'.
type UserStats struct {
	UserID             string             `json:"user_id"`
	TrophyPoints       int64              `json:"trophy_points"`
	RankTier           string             `json:"rank_tier"`
	TotalMatches       int                `json:"total_matches"`
	TotalWins          int                `json:"total_wins"`
	TotalDraws         int                `json:"total_draws"`
	TotalLosses        int                `json:"total_losses"`
	WinRate            float64            `json:"win_rate"`
	HighestMatchScore  int                `json:"highest_match_score"`
	HighestRoundScore  int                `json:"highest_round_score"`
	CurrentWinStreak   int                `json:"current_win_streak"`
	HighestWinStreak   int                `json:"highest_win_streak"`
	TsumoWins          int                `json:"tsumo_wins"`
	RonWins            int                `json:"ron_wins"`
	SpecialHandsRecord SpecialHandsRecord `json:"special_hands_record"`
	UpdatedAt          time.Time          `json:"updated_at"`
}

// Friendship merepresentasikan relasi pertemanan antar dua user di tabel 'friendships'.
type Friendship struct {
	ID        string    `json:"id"`
	UserID    string    `json:"user_id"`
	FriendID  string    `json:"friend_id"`
	Status    string    `json:"status"` // "PENDING", "ACCEPTED", "BLOCKED", "REJECTED"
	CreatedAt time.Time `json:"created_at"`
	UpdatedAt time.Time `json:"updated_at"`
}

// FriendProfile merepresentasikan tampilan profil teman di panel Friend List.
type FriendProfile struct {
	UserID       string `json:"user_id"`
	Username     string `json:"username"`
	DisplayName  string `json:"display_name"`
	AvatarID     int    `json:"avatar_id"`
	RankTier     string `json:"rank_tier"`
	TrophyPoints int64  `json:"trophy_points"`
	ChipsBalance int64  `json:"chips_balance"`
	Status       string `json:"status"` // "ACCEPTED", "PENDING"
	IsOnline     bool   `json:"is_online"`
}

// CustomRoom merepresentasikan meja mabar ber-kode (e.g. VIP-8821).
type CustomRoom struct {
	ID                   string    `json:"id"`
	RoomCode             string    `json:"room_code"`
	HostUserID           string    `json:"host_user_id"`
	GameMode             string    `json:"game_mode"`
	MinEntryChips        int64     `json:"min_entry_chips"`
	MaxPlayers           int       `json:"max_players"`
	AutoFillBots         bool      `json:"auto_fill_bots"`
	BotDifficulty        string    `json:"bot_difficulty"`
	TurnTimerSeconds     int       `json:"turn_timer_seconds"`
	ReactionTimerSeconds int       `json:"reaction_timer_seconds"`
	IsPrivate            bool      `json:"is_private"`
	Status               string    `json:"status"` // "WAITING", "IN_PROGRESS", "CLOSED"
	CreatedAt            time.Time `json:"created_at"`
}

// MatchPlayerRecord merepresentasikan 1 kursi pemain / bot dalam 1 match.
type MatchPlayerRecord struct {
	ID             string `json:"id"`
	MatchID        string `json:"match_id"`
	UserID         string `json:"user_id,omitempty"` // Kosong jika Bot
	SeatPosition   string `json:"seat_position"`     // "EAST", "SOUTH", "WEST", "NORTH"
	SeatIndex      int    `json:"seat_index"`        // 0..3
	IsBot          bool   `json:"is_bot"`
	BotName        string `json:"bot_name,omitempty"`
	BotDifficulty  string `json:"bot_difficulty"`
	InitialChips   int64  `json:"initial_chips"`
	FinalScore     int    `json:"final_score"`
	RankPosition   int    `json:"rank_position"`
	TrophyDelta    int    `json:"trophy_delta"`
	ChipsDelta     int64  `json:"chips_delta"`
	IsDisconnected bool   `json:"is_disconnected"`
}

// MatchRoundRecord merepresentasikan detail 1 dari 4 ronde dalam match.
type MatchRoundRecord struct {
	ID                 string   `json:"id"`
	MatchID            string   `json:"match_id"`
	RoundNumber        int      `json:"round_number"` // 1..4
	DealerSeat         string   `json:"dealer_seat"`
	WallSeed           string   `json:"wall_seed"`
	WinnerSeat         string   `json:"winner_seat,omitempty"`
	WinType            string   `json:"win_type"` // "TSUMO", "RON", "DRAW"
	BasePoints         int      `json:"base_points"`
	SelfDrawBonus      int      `json:"self_draw_bonus"`
	DiscardWinBonus    int      `json:"discard_win_bonus"`
	MeldsPoints        int      `json:"melds_points"`
	FlowerSeasonPoints int      `json:"flower_season_points"`
	SpecialHandPoints  int      `json:"special_hand_points"`
	TotalRoundScore    int      `json:"total_round_score"`
	WinningHandJSON    string   `json:"winning_hand_json,omitempty"`
	ScoreBreakdowns    []string `json:"score_breakdowns,omitempty"`
}

// MatchRecord merepresentasikan ringkasan riwayat pertandingan di tabel 'matches'.
type MatchRecord struct {
	ID           string               `json:"id"`
	RoomCode     string               `json:"room_code"`
	GameMode     string               `json:"game_mode"` // "RANKED_MATCH", "CUSTOM_VIP", "SOLO_AI"
	Status       string               `json:"status"`
	TotalRounds  int                  `json:"total_rounds"`
	WinnerID     string               `json:"winner_id,omitempty"`
	WinningScore int                  `json:"winning_score"`
	Players      []MatchPlayerRecord  `json:"players"`
	Rounds       []MatchRoundRecord   `json:"rounds,omitempty"`
	PlayerScores string               `json:"player_scores"` // JSON String backward compatibility
	StartedAt    time.Time            `json:"started_at"`
	FinishedAt   time.Time            `json:"finished_at"`
}

// LeaderboardEntry merepresentasikan satu baris data peringkat juara di view 'v_leaderboard_global'.
type LeaderboardEntry struct {
	Rank              int     `json:"rank"`
	UserID            string  `json:"user_id"`
	Username          string  `json:"username"`
	DisplayName       string  `json:"display_name"`
	AvatarID          int     `json:"avatar_id"`
	CustomFrameID     int     `json:"custom_frame_id"`
	ChipsBalance      int64   `json:"chips_balance"`
	TrophyPoints      int64   `json:"trophy_points"`
	RankTier          string  `json:"rank_tier"`
	TotalWins         int     `json:"total_wins"`
	TotalMatches      int     `json:"total_matches"`
	WinRate           float64 `json:"win_rate"`
	HighestMatchScore int     `json:"highest_match_score"`
}

// PlayerMatchHistoryEntry merepresentasikan riwayat pertandingan satu pemain dari view 'v_player_match_history'.
type PlayerMatchHistoryEntry struct {
	UserID       string    `json:"user_id"`
	MatchID      string    `json:"match_id"`
	RoomCode     string    `json:"room_code"`
	GameMode     string    `json:"game_mode"`
	StartedAt    time.Time `json:"started_at"`
	FinishedAt   time.Time `json:"finished_at"`
	SeatPosition string    `json:"seat_position"`
	FinalScore   int       `json:"final_score"`
	RankPosition int       `json:"rank_position"`
	TrophyDelta  int       `json:"trophy_delta"`
	ChipsDelta   int64     `json:"chips_delta"`
	IsWinner     bool      `json:"is_winner"`
	WinningScore int       `json:"winning_score"`
}

// HashPassword membuat hash SHA-256 ber-salt untuk keamanan password akun.
func HashPassword(password, salt string) string {
	hasher := sha256.New()
	hasher.Write([]byte(password + ":" + salt))
	return hex.EncodeToString(hasher.Sum(nil))
}

// CalculateRankTier menentukan gelar kemahiran kasino berdasarkan Trophy Points.
func CalculateRankTier(trophyPoints int64) string {
	switch {
	case trophyPoints >= 10000:
		return string(RankTierVIPLegend)
	case trophyPoints >= 4000:
		return string(RankTierMaster)
	case trophyPoints >= 1500:
		return string(RankTierExpert)
	case trophyPoints >= 500:
		return string(RankTierApprentice)
	default:
		return string(RankTierNovice)
	}
}

// DBConfig menyimpan parameter koneksi database.
type DBConfig struct {
	DatabaseURL string
	RedisURL    string
}

func (c DBConfig) String() string {
	if c.DatabaseURL == "" {
		return "Mode: In-Memory Database (Development Local)"
	}
	return fmt.Sprintf("Mode: Cloud PostgreSQL (Supabase Connected)")
}
