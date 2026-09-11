package database

import (
	"crypto/sha256"
	"encoding/hex"
	"fmt"
	"time"
)

// User merepresentasikan model data akun pemain di tabel 'users'.
type User struct {
	ID           string    `json:"id"`
	Username     string    `json:"username"`
	PasswordHash string    `json:"-"` // Disembunyikan dari JSON response
	DisplayName  string    `json:"display_name"`
	AvatarID     int       `json:"avatar_id"`
	CreatedAt    time.Time `json:"created_at"`
	LastLogin    time.Time `json:"last_login"`
}

// UserStats merepresentasikan statistik pemain dan trophy points di tabel 'user_stats'.
type UserStats struct {
	UserID       string    `json:"user_id"`
	TotalMatches int       `json:"total_matches"`
	TotalWins    int       `json:"total_wins"`
	TrophyPoints int64     `json:"trophy_points"`
	RankTier     string    `json:"rank_tier"`
	HighestScore int       `json:"highest_score"`
	WinRate      float64   `json:"win_rate"`
	UpdatedAt    time.Time `json:"updated_at"`
}

// Friendship merepresentasikan relasi pertemanan antar dua user di tabel 'friendships'.
type Friendship struct {
	ID        string    `json:"id"`
	UserID    string    `json:"user_id"`
	FriendID  string    `json:"friend_id"`
	Status    string    `json:"status"` // "PENDING", "ACCEPTED", "BLOCKED"
	CreatedAt time.Time `json:"created_at"`
}

// FriendProfile merepresentasikan tampilan profil teman di panel Friend List.
type FriendProfile struct {
	UserID       string `json:"user_id"`
	Username     string `json:"username"`
	DisplayName  string `json:"display_name"`
	AvatarID     int    `json:"avatar_id"`
	RankTier     string `json:"rank_tier"`
	TrophyPoints int64  `json:"trophy_points"`
	Status       string `json:"status"` // "ACCEPTED", "PENDING"
	IsOnline     bool   `json:"is_online"`
}

// MatchRecord merepresentasikan ringkasan riwayat pertandingan di tabel 'match_records'.
type MatchRecord struct {
	ID           string    `json:"id"`
	RoomCode     string    `json:"room_code"`
	GameMode     string    `json:"game_mode"` // "RANKED" atau "CUSTOM"
	WinnerID     string    `json:"winner_id"`
	WinningType  string    `json:"winning_type"`
	TotalRounds  int       `json:"total_rounds"`
	PlayerScores string    `json:"player_scores"` // JSON String
	PlayedAt     time.Time `json:"played_at"`
}

// LeaderboardEntry merepresentasikan satu baris data peringkat juara di view 'v_leaderboard'.
type LeaderboardEntry struct {
	Rank         int     `json:"rank"`
	Username     string  `json:"username"`
	DisplayName  string  `json:"display_name"`
	AvatarID     int     `json:"avatar_id"`
	TrophyPoints int64   `json:"trophy_points"`
	RankTier     string  `json:"rank_tier"`
	TotalWins    int     `json:"total_wins"`
	TotalMatches int     `json:"total_matches"`
	WinRate      float64 `json:"win_rate"`
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
		return "VIP Legend 👑"
	case trophyPoints >= 4000:
		return "Master 💎"
	case trophyPoints >= 1500:
		return "Expert 🥇"
	case trophyPoints >= 500:
		return "Apprentice 🥈"
	default:
		return "Novice 🥉"
	}
}

// DBConfig menyimpan parameter koneksi database.
type DBConfig struct {
	DatabaseURL string
}

func (c DBConfig) String() string {
	if c.DatabaseURL == "" {
		return "Mode: In-Memory Database (Development Local)"
	}
	return fmt.Sprintf("Mode: Cloud PostgreSQL (Supabase Connected)")
}
