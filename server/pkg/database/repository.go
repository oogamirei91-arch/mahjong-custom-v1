package database

import (
	"encoding/json"
	"errors"
	"fmt"
	"sort"
	"sync"
	"time"
)

// Repository mengelola operasi penyimpanan data akun, statistik, friend list, dan riwayat pertandingan.
type Repository struct {
	users        map[string]*User        // Key: UserID
	usersByUname map[string]*User        // Key: Username
	stats        map[string]*UserStats   // Key: UserID
	friendships  map[string]*Friendship  // Key: "UserID:FriendID"
	matches      []*MatchRecord
	mu           sync.RWMutex
}

// NewRepository membuat instansiasi Repository baru.
func NewRepository() *Repository {
	return &Repository{
		users:        make(map[string]*User),
		usersByUname: make(map[string]*User),
		stats:        make(map[string]*UserStats),
		friendships:  make(map[string]*Friendship),
		matches:      make([]*MatchRecord, 0),
	}
}

// RegisterUser mendaftarkan akun pemain baru dan menginisialisasi statistik awal.
func (r *Repository) RegisterUser(username, password, displayName string, avatarID int) (*User, error) {
	r.mu.Lock()
	defer r.mu.Unlock()

	if username == "" || password == "" {
		return nil, errors.New("username dan password tidak boleh kosong")
	}

	if _, exists := r.usersByUname[username]; exists {
		return nil, fmt.Errorf("username '%s' sudah terdaftar", username)
	}

	if displayName == "" {
		displayName = username
	}
	if avatarID <= 0 {
		avatarID = 1
	}

	userID := fmt.Sprintf("usr_%d", len(r.users)+1)
	salt := fmt.Sprintf("salt_%s", userID)
	passHash := HashPassword(password, salt)
	now := time.Now()

	user := &User{
		ID:           userID,
		Username:     username,
		PasswordHash: passHash,
		DisplayName:  displayName,
		AvatarID:     avatarID,
		CreatedAt:    now,
		LastLogin:    now,
	}

	userStats := &UserStats{
		UserID:       userID,
		TotalMatches: 0,
		TotalWins:    0,
		TrophyPoints: 0,
		RankTier:     CalculateRankTier(0),
		HighestScore: 0,
		WinRate:      0.0,
		UpdatedAt:    now,
	}

	r.users[userID] = user
	r.usersByUname[username] = user
	r.stats[userID] = userStats

	return user, nil
}

// AuthenticateUser memverifikasi login username dan password.
func (r *Repository) AuthenticateUser(username, password string) (*User, error) {
	r.mu.Lock()
	defer r.mu.Unlock()

	user, exists := r.usersByUname[username]
	if !exists {
		return nil, errors.New("username atau password salah")
	}

	salt := fmt.Sprintf("salt_%s", user.ID)
	inputHash := HashPassword(password, salt)

	if user.PasswordHash != inputHash {
		return nil, errors.New("username atau password salah")
	}

	user.LastLogin = time.Now()
	return user, nil
}

// GetUserProfile mengambil profil akun dan statistik pemain.
func (r *Repository) GetUserProfile(userID string) (*User, *UserStats, error) {
	r.mu.RLock()
	defer r.mu.RUnlock()

	user, exists := r.users[userID]
	if !exists {
		return nil, nil, errors.New("pemain tidak ditemukan")
	}

	stats, statsExists := r.stats[userID]
	if !statsExists {
		return user, nil, nil
	}

	return user, stats, nil
}

// --- SISTEM PERTEMANAN (FRIEND LIST) ---

// SendFriendRequest mengirimkan permintaan pertemanan ke username target.
func (r *Repository) SendFriendRequest(fromUserID, targetUsername string) (*Friendship, error) {
	r.mu.Lock()
	defer r.mu.Unlock()

	targetUser, exists := r.usersByUname[targetUsername]
	if !exists {
		return nil, fmt.Errorf("user '%s' tidak ditemukan", targetUsername)
	}

	if fromUserID == targetUser.ID {
		return nil, errors.New("tidak bisa menambahkan diri sendiri sebagai teman")
	}

	pairKey := fmt.Sprintf("%s:%s", fromUserID, targetUser.ID)
	if _, alreadyReq := r.friendships[pairKey]; alreadyReq {
		return nil, errors.New("permintaan pertemanan sudah pernah dikirim")
	}

	friendship := &Friendship{
		ID:        fmt.Sprintf("frnd_%d", len(r.friendships)+1),
		UserID:    fromUserID,
		FriendID:  targetUser.ID,
		Status:    "PENDING",
		CreatedAt: time.Now(),
	}

	r.friendships[pairKey] = friendship
	return friendship, nil
}

// AcceptFriendRequest menyetujui permintaan pertemanan yang masuk.
func (r *Repository) AcceptFriendRequest(currentUserID, requesterUserID string) error {
	r.mu.Lock()
	defer r.mu.Unlock()

	pairKey := fmt.Sprintf("%s:%s", requesterUserID, currentUserID)
	friendship, exists := r.friendships[pairKey]
	if !exists {
		return errors.New("permintaan pertemanan tidak ditemukan")
	}

	friendship.Status = "ACCEPTED"

	// Buat relasi timbal balik (Accepted dua arah)
	reverseKey := fmt.Sprintf("%s:%s", currentUserID, requesterUserID)
	r.friendships[reverseKey] = &Friendship{
		ID:        fmt.Sprintf("frnd_%d", len(r.friendships)+1),
		UserID:    currentUserID,
		FriendID:  requesterUserID,
		Status:    "ACCEPTED",
		CreatedAt: time.Now(),
	}

	return nil
}

// GetFriendList mengambil daftar seluruh teman dari pemain.
func (r *Repository) GetFriendList(userID string) []FriendProfile {
	r.mu.RLock()
	defer r.mu.RUnlock()

	var friends []FriendProfile

	for _, f := range r.friendships {
		if f.UserID == userID && f.Status == "ACCEPTED" {
			friendUser := r.users[f.FriendID]
			friendStat := r.stats[f.FriendID]

			if friendUser != nil {
				rankTier := "Novice 🥉"
				var trophy int64 = 0
				if friendStat != nil {
					rankTier = friendStat.RankTier
					trophy = friendStat.TrophyPoints
				}

				friends = append(friends, FriendProfile{
					UserID:       friendUser.ID,
					Username:     friendUser.Username,
					DisplayName:  friendUser.DisplayName,
					AvatarID:     friendUser.AvatarID,
					RankTier:     rankTier,
					TrophyPoints: trophy,
					Status:       f.Status,
					IsOnline:     true, // Disinkronkan dengan status koneksi Hub
				})
			}
		}
	}

	return friends
}

// --- PENCATATAN MATCH & TROPHY PROGRESSION ---

// RecordMatchOutcome mencatat hasil pertandingan (Ranked / Custom) dan memperbarui Trophy Points.
func (r *Repository) RecordMatchOutcome(roomCode, gameMode, winnerID, winningType string, playerScores map[string]int, highestRoundScore int) error {
	r.mu.Lock()
	defer r.mu.Unlock()

	scoresJSON, err := json.Marshal(playerScores)
	if err != nil {
		return fmt.Errorf("gagal encode player scores: %w", err)
	}

	recordID := fmt.Sprintf("rec_%d", len(r.matches)+1)
	matchRecord := &MatchRecord{
		ID:           recordID,
		RoomCode:     roomCode,
		GameMode:     gameMode,
		WinnerID:     winnerID,
		WinningType:  winningType,
		TotalRounds:  4,
		PlayerScores: string(scoresJSON),
		PlayedAt:     time.Now(),
	}
	r.matches = append(r.matches, matchRecord)

	// Perbarui statistik pemain
	for uid, score := range playerScores {
		if stat, exists := r.stats[uid]; exists {
			stat.TotalMatches++

			// Mode Ranked menambah Trophy Points untuk ranking
			if gameMode == "RANKED" {
				stat.TrophyPoints += int64(score)
			}

			stat.UpdatedAt = time.Now()

			if uid == winnerID {
				stat.TotalWins++
			}
			if score > stat.HighestScore {
				stat.HighestScore = score
			}

			// Hitung ulang Win Rate dan Rank Tier
			if stat.TotalMatches > 0 {
				stat.WinRate = float64(stat.TotalWins) / float64(stat.TotalMatches) * 100.0
			}
			stat.RankTier = CalculateRankTier(stat.TrophyPoints)
		}
	}

	return nil
}

// GetLeaderboard mengembalikan daftar pemain teratas berdasarkan Trophy Points.
func (r *Repository) GetLeaderboard(limit int) []LeaderboardEntry {
	r.mu.RLock()
	defer r.mu.RUnlock()

	var entries []LeaderboardEntry

	for uid, stat := range r.stats {
		user := r.users[uid]
		if user == nil {
			continue
		}

		entries = append(entries, LeaderboardEntry{
			Username:     user.Username,
			DisplayName:  user.DisplayName,
			AvatarID:     user.AvatarID,
			TrophyPoints: stat.TrophyPoints,
			RankTier:     stat.RankTier,
			TotalWins:    stat.TotalWins,
			TotalMatches: stat.TotalMatches,
			WinRate:      stat.WinRate,
		})
	}

	// Urutkan berdasarkan TrophyPoints descending
	sort.Slice(entries, func(i, j int) bool {
		if entries[i].TrophyPoints == entries[j].TrophyPoints {
			return entries[i].TotalWins > entries[j].TotalWins
		}
		return entries[i].TrophyPoints > entries[j].TrophyPoints
	})

	for i := range entries {
		entries[i].Rank = i + 1
	}

	if limit > 0 && len(entries) > limit {
		entries = entries[:limit]
	}

	return entries
}
