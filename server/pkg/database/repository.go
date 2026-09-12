package database

import (
	"encoding/json"
	"errors"
	"fmt"
	"sort"
	"sync"
	"time"
)

// Repository mengelola operasi penyimpanan data akun, statistik, wallet, friend list, dan riwayat pertandingan.
type Repository struct {
	users        map[string]*User        // Key: UserID
	usersByUname map[string]*User        // Key: Username
	usersByOAuth map[string]*User        // Key: Provider:OAuthID
	usersByEmail map[string]*User        // Key: Email
	wallets      map[string]*UserWallet  // Key: UserID
	stats        map[string]*UserStats   // Key: UserID
	friendships  map[string]*Friendship  // Key: "UserID:FriendID"
	customRooms  map[string]*CustomRoom  // Key: RoomCode
	matches      []*MatchRecord
	mu           sync.RWMutex
}

// NewRepository membuat instansiasi Repository baru (In-Memory + Thread-Safe).
func NewRepository() *Repository {
	return &Repository{
		users:        make(map[string]*User),
		usersByUname: make(map[string]*User),
		usersByOAuth: make(map[string]*User),
		usersByEmail: make(map[string]*User),
		wallets:      make(map[string]*UserWallet),
		stats:        make(map[string]*UserStats),
		friendships:  make(map[string]*Friendship),
		customRooms:  make(map[string]*CustomRoom),
		matches:      make([]*MatchRecord, 0),
	}
}

// --- AKUN & OTENTIKASI (MULTI-AUTH: GUEST, EMAIL, GOOGLE) ---

// RegisterUser mendaftarkan akun baru berbasis Email / Username & Password.
func (r *Repository) RegisterUser(username, email, password, displayName string, avatarID int) (*User, error) {
	r.mu.Lock()
	defer r.mu.Unlock()

	if username == "" || password == "" {
		return nil, errors.New("username dan password tidak boleh kosong")
	}

	if _, exists := r.usersByUname[username]; exists {
		return nil, fmt.Errorf("username '%s' sudah terdaftar", username)
	}

	if email != "" {
		if _, exists := r.usersByEmail[email]; exists {
			return nil, fmt.Errorf("email '%s' sudah terdaftar", email)
		}
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
		ID:            userID,
		Username:      username,
		Email:         email,
		PasswordHash:  passHash,
		AuthProvider:  AuthProviderEmail,
		DisplayName:   displayName,
		AvatarID:      avatarID,
		CustomFrameID: 1,
		Role:          "PLAYER",
		IsActive:      true,
		IsBanned:      false,
		CreatedAt:     now,
		UpdatedAt:     now,
		LastLogin:     now,
	}

	r.initUserEntities(user, now)
	return user, nil
}

// RegisterOrLoginOAuth menangani autentikasi instan Google / Apple OAuth.
func (r *Repository) RegisterOrLoginOAuth(provider AuthProvider, oauthID, email, displayName, avatarURL string) (*User, error) {
	r.mu.Lock()
	defer r.mu.Unlock()

	key := fmt.Sprintf("%s:%s", provider, oauthID)
	if user, exists := r.usersByOAuth[key]; exists {
		user.LastLogin = time.Now()
		if displayName != "" {
			user.DisplayName = displayName
		}
		if avatarURL != "" {
			user.AvatarURL = avatarURL
		}
		return user, nil
	}

	// Buat user baru dari OAuth
	userID := fmt.Sprintf("usr_%d", len(r.users)+1)
	uname := fmt.Sprintf("%s_%s", string(provider), oauthID[:min(6, len(oauthID))])
	now := time.Now()

	user := &User{
		ID:              userID,
		Username:        uname,
		Email:           email,
		AuthProvider:    provider,
		OAuthProviderID: oauthID,
		DisplayName:     displayName,
		AvatarID:        1,
		AvatarURL:       avatarURL,
		CustomFrameID:   1,
		Role:            "PLAYER",
		IsActive:        true,
		IsBanned:        false,
		CreatedAt:       now,
		UpdatedAt:       now,
		LastLogin:       now,
	}

	r.initUserEntities(user, now)
	r.usersByOAuth[key] = user
	return user, nil
}

// RegisterOrLoginGuest mendaftarkan / mengautentikasi akun Tamu (Guest Mode).
func (r *Repository) RegisterOrLoginGuest(guestDeviceID, displayName string) (*User, error) {
	r.mu.Lock()
	defer r.mu.Unlock()

	key := fmt.Sprintf("GUEST:%s", guestDeviceID)
	if user, exists := r.usersByOAuth[key]; exists {
		user.LastLogin = time.Now()
		return user, nil
	}

	userID := fmt.Sprintf("usr_guest_%d", len(r.users)+1)
	uname := fmt.Sprintf("Guest_%d", 1000+len(r.users))
	if displayName == "" {
		displayName = uname
	}
	now := time.Now()

	user := &User{
		ID:              userID,
		Username:        uname,
		AuthProvider:    AuthProviderGuest,
		OAuthProviderID: guestDeviceID,
		DisplayName:     displayName,
		AvatarID:        1,
		CustomFrameID:   1,
		Role:            "PLAYER",
		IsActive:        true,
		IsBanned:        false,
		CreatedAt:       now,
		UpdatedAt:       now,
		LastLogin:       now,
	}

	r.initUserEntities(user, now)
	r.usersByOAuth[key] = user
	return user, nil
}

// CheckUserExists mengecek apakah akun dengan username atau email sudah ada.
func (r *Repository) CheckUserExists(identifier string) (bool, error) {
	r.mu.RLock()
	defer r.mu.RUnlock()

	if _, exists := r.usersByUname[identifier]; exists {
		return true, nil
	}
	if _, exists := r.usersByEmail[identifier]; exists {
		return true, nil
	}
	return false, nil
}

// CheckGuestExists mengecek apakah perangkat tamu sudah terdaftar.
func (r *Repository) CheckGuestExists(guestDeviceID string) (*User, error) {
	r.mu.RLock()
	defer r.mu.RUnlock()

	key := fmt.Sprintf("GUEST:%s", guestDeviceID)
	if user, exists := r.usersByOAuth[key]; exists {
		return user, nil
	}
	return nil, nil
}

// RegisterGuest mendaftarkan perangkat tamu baru.
func (r *Repository) RegisterGuest(guestDeviceID, displayName string) (*User, error) {
	return r.RegisterOrLoginGuest(guestDeviceID, displayName)
}

// CheckOAuthExists mengecek apakah akun OAuth sudah terdaftar.
func (r *Repository) CheckOAuthExists(provider AuthProvider, oauthID string) (*User, error) {
	r.mu.RLock()
	defer r.mu.RUnlock()

	key := fmt.Sprintf("%s:%s", provider, oauthID)
	if user, exists := r.usersByOAuth[key]; exists {
		return user, nil
	}
	return nil, nil
}

// RegisterOAuthUser mendaftarkan akun OAuth baru.
func (r *Repository) RegisterOAuthUser(provider AuthProvider, oauthID, email, displayName, avatarURL string) (*User, error) {
	return r.RegisterOrLoginOAuth(provider, oauthID, email, displayName, avatarURL)
}

// AuthenticateUser memverifikasi login username/email dan password.
func (r *Repository) AuthenticateUser(identifier, password string) (*User, error) {
	r.mu.Lock()
	defer r.mu.Unlock()

	var user *User
	var exists bool

	// Cek berdasarkan Username
	user, exists = r.usersByUname[identifier]
	if !exists {
		// Cek berdasarkan Email
		user, exists = r.usersByEmail[identifier]
	}

	if !exists || user == nil {
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

func (r *Repository) initUserEntities(user *User, now time.Time) {
	r.users[user.ID] = user
	r.usersByUname[user.Username] = user
	if user.Email != "" {
		r.usersByEmail[user.Email] = user
	}

	r.wallets[user.ID] = &UserWallet{
		UserID:           user.ID,
		ChipsBalance:     10000, // Saldo awal 10,000 Chips
		DiamondsBalance:  50,
		TotalChipsEarned: 10000,
		UpdatedAt:        now,
	}

	r.stats[user.ID] = &UserStats{
		UserID:            user.ID,
		TrophyPoints:      0,
		RankTier:          CalculateRankTier(0),
		TotalMatches:      0,
		TotalWins:         0,
		TotalDraws:        0,
		TotalLosses:       0,
		WinRate:           0.0,
		HighestMatchScore: 0,
		HighestRoundScore: 0,
		CurrentWinStreak:  0,
		HighestWinStreak:  0,
		TsumoWins:         0,
		RonWins:           0,
		SpecialHandsRecord: SpecialHandsRecord{},
		UpdatedAt:         now,
	}
}

// GetUserProfile mengambil profil akun, wallet, dan statistik pemain.
func (r *Repository) GetUserProfile(userID string) (*User, *UserWallet, *UserStats, error) {
	r.mu.RLock()
	defer r.mu.RUnlock()

	user, exists := r.users[userID]
	if !exists {
		return nil, nil, nil, errors.New("pemain tidak ditemukan")
	}

	wallet := r.wallets[userID]
	stats := r.stats[userID]

	return user, wallet, stats, nil
}

// UpdateChips mengubah saldo chips pemain (misal: menang match atau bayar room entry).
func (r *Repository) UpdateChips(userID string, amountDelta int64) (int64, error) {
	r.mu.Lock()
	defer r.mu.Unlock()

	wallet, exists := r.wallets[userID]
	if !exists {
		return 0, errors.New("wallet tidak ditemukan")
	}

	if wallet.ChipsBalance+amountDelta < 0 {
		return wallet.ChipsBalance, errors.New("saldo chips tidak mencukupi")
	}

	wallet.ChipsBalance += amountDelta
	if amountDelta > 0 {
		wallet.TotalChipsEarned += amountDelta
	}
	wallet.UpdatedAt = time.Now()
	return wallet.ChipsBalance, nil
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
		UpdatedAt: time.Now(),
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
	friendship.UpdatedAt = time.Now()

	// Buat relasi timbal balik (Accepted dua arah)
	reverseKey := fmt.Sprintf("%s:%s", currentUserID, requesterUserID)
	r.friendships[reverseKey] = &Friendship{
		ID:        fmt.Sprintf("frnd_%d", len(r.friendships)+1),
		UserID:    currentUserID,
		FriendID:  requesterUserID,
		Status:    "ACCEPTED",
		CreatedAt: time.Now(),
		UpdatedAt: time.Now(),
	}

	return nil
}

// GetFriendList mengambil daftar seluruh teman dari pemain beserta status saldo & rank.
func (r *Repository) GetFriendList(userID string) []FriendProfile {
	r.mu.RLock()
	defer r.mu.RUnlock()

	var friends []FriendProfile

	for _, f := range r.friendships {
		if f.UserID == userID && f.Status == "ACCEPTED" {
			friendUser := r.users[f.FriendID]
			friendStat := r.stats[f.FriendID]
			friendWallet := r.wallets[f.FriendID]

			if friendUser != nil {
				rankTier := string(RankTierNovice)
				var trophy int64 = 0
				var chips int64 = 0
				if friendStat != nil {
					rankTier = friendStat.RankTier
					trophy = friendStat.TrophyPoints
				}
				if friendWallet != nil {
					chips = friendWallet.ChipsBalance
				}

				friends = append(friends, FriendProfile{
					UserID:       friendUser.ID,
					Username:     friendUser.Username,
					DisplayName:  friendUser.DisplayName,
					AvatarID:     friendUser.AvatarID,
					RankTier:     rankTier,
					TrophyPoints: trophy,
					ChipsBalance: chips,
					Status:       f.Status,
					IsOnline:     true,
				})
			}
		}
	}

	return friends
}

// --- CUSTOM ROOM & VIP LOBBY ---

// CreateCustomRoom membuat ruangan mabar ber-kode unik (e.g. VIP-8821).
func (r *Repository) CreateCustomRoom(hostUserID, roomCode, gameMode string, minEntryChips int64, autoFillBots bool, botDiff string) (*CustomRoom, error) {
	r.mu.Lock()
	defer r.mu.Unlock()

	if _, exists := r.customRooms[roomCode]; exists {
		return nil, fmt.Errorf("kode room '%s' sudah aktif", roomCode)
	}

	room := &CustomRoom{
		ID:                   fmt.Sprintf("room_%d", len(r.customRooms)+1),
		RoomCode:             roomCode,
		HostUserID:           hostUserID,
		GameMode:             gameMode,
		MinEntryChips:        minEntryChips,
		MaxPlayers:           4,
		AutoFillBots:         autoFillBots,
		BotDifficulty:        botDiff,
		TurnTimerSeconds:     15,
		ReactionTimerSeconds: 5,
		IsPrivate:            true,
		Status:               "WAITING",
		CreatedAt:            time.Now(),
	}

	r.customRooms[roomCode] = room
	return room, nil
}

// GetCustomRoom mengambil detail custom room via kode.
func (r *Repository) GetCustomRoom(roomCode string) (*CustomRoom, error) {
	r.mu.RLock()
	defer r.mu.RUnlock()

	room, exists := r.customRooms[roomCode]
	if !exists {
		return nil, errors.New("ruangan tidak ditemukan atau sudah ditutup")
	}
	return room, nil
}

// --- PENCATATAN PERTANDINGAN & STATISTIK (MODUL 1 - 9) ---

// RecordFullMatch mencatat hasil akhir match 4 ronde beserta rincian kursi pemain & bot.
func (r *Repository) RecordFullMatch(record *MatchRecord) error {
	r.mu.Lock()
	defer r.mu.Unlock()

	if record.ID == "" {
		record.ID = fmt.Sprintf("match_%d", len(r.matches)+1)
	}
	if record.FinishedAt.IsZero() {
		record.FinishedAt = time.Now()
	}

	// Update backwards-compatible player scores string
	scoresMap := make(map[string]int)
	for _, p := range record.Players {
		if p.UserID != "" {
			scoresMap[p.UserID] = p.FinalScore
		}
	}
	scoresJSON, _ := json.Marshal(scoresMap)
	record.PlayerScores = string(scoresJSON)

	r.matches = append(r.matches, record)

	// Update stats & wallets untuk seluruh pemain manusia
	for _, p := range record.Players {
		if p.IsBot || p.UserID == "" {
			continue
		}

		if stat, exists := r.stats[p.UserID]; exists {
			stat.TotalMatches++
			if p.RankPosition == 1 {
				stat.TotalWins++
				stat.CurrentWinStreak++
				if stat.CurrentWinStreak > stat.HighestWinStreak {
					stat.HighestWinStreak = stat.CurrentWinStreak
				}
			} else {
				stat.TotalLosses++
				stat.CurrentWinStreak = 0
			}

			// Mode Ranked menambah trophy
			if record.GameMode == "RANKED_MATCH" {
				stat.TrophyPoints += int64(p.TrophyDelta)
				if stat.TrophyPoints < 0 {
					stat.TrophyPoints = 0
				}
			}

			if p.FinalScore > stat.HighestMatchScore {
				stat.HighestMatchScore = p.FinalScore
			}

			// Recalculate Win Rate & Rank Tier
			if stat.TotalMatches > 0 {
				stat.WinRate = float64(stat.TotalWins) / float64(stat.TotalMatches) * 100.0
			}
			stat.RankTier = CalculateRankTier(stat.TrophyPoints)
			stat.UpdatedAt = time.Now()
		}

		// Update Wallet Chips
		if wallet, exists := r.wallets[p.UserID]; exists {
			wallet.ChipsBalance += p.ChipsDelta
			if wallet.ChipsBalance < 0 {
				wallet.ChipsBalance = 0
			}
			if p.ChipsDelta > 0 {
				wallet.TotalChipsEarned += p.ChipsDelta
			}
			wallet.UpdatedAt = time.Now()
		}
	}

	return nil
}

// RecordMatchOutcome menyediakan kompatibilitas lama untuk mencatat match ringkas.
func (r *Repository) RecordMatchOutcome(roomCode, gameMode, winnerID, winningType string, playerScores map[string]int, highestRoundScore int) error {
	players := make([]MatchPlayerRecord, 0, len(playerScores))
	idx := 0
	for uid, score := range playerScores {
		isWin := (uid == winnerID)
		trophy := -25
		rankPos := 2
		if isWin {
			trophy = 40
			rankPos = 1
		}

		players = append(players, MatchPlayerRecord{
			ID:           fmt.Sprintf("mp_%d_%d", len(r.matches)+1, idx),
			UserID:       uid,
			SeatPosition: []string{"EAST", "SOUTH", "WEST", "NORTH"}[idx%4],
			SeatIndex:    idx % 4,
			IsBot:        false,
			FinalScore:   score,
			RankPosition: rankPos,
			TrophyDelta:  trophy,
			ChipsDelta:   int64(score * 10),
		})
		idx++
	}

	record := &MatchRecord{
		ID:           fmt.Sprintf("match_%d", len(r.matches)+1),
		RoomCode:     roomCode,
		GameMode:     gameMode,
		Status:       "FINISHED",
		TotalRounds:  4,
		WinnerID:     winnerID,
		WinningScore: highestRoundScore,
		Players:      players,
		StartedAt:    time.Now().Add(-10 * time.Minute),
		FinishedAt:   time.Now(),
	}

	return r.RecordFullMatch(record)
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
		wallet := r.wallets[uid]
		var chips int64 = 0
		if wallet != nil {
			chips = wallet.ChipsBalance
		}

		entries = append(entries, LeaderboardEntry{
			UserID:            user.ID,
			Username:          user.Username,
			DisplayName:       user.DisplayName,
			AvatarID:          user.AvatarID,
			CustomFrameID:     user.CustomFrameID,
			ChipsBalance:      chips,
			TrophyPoints:      stat.TrophyPoints,
			RankTier:          stat.RankTier,
			TotalWins:         stat.TotalWins,
			TotalMatches:      stat.TotalMatches,
			WinRate:           stat.WinRate,
			HighestMatchScore: stat.HighestMatchScore,
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

// GetPlayerMatchHistory mengambil riwayat pertandingan in-memory pemain.
func (r *Repository) GetPlayerMatchHistory(userID string, limit int) []PlayerMatchHistoryEntry {
	r.mu.RLock()
	defer r.mu.RUnlock()

	var list []PlayerMatchHistoryEntry
	for i := len(r.matches) - 1; i >= 0; i-- {
		m := r.matches[i]
		for _, p := range m.Players {
			if p.UserID == userID {
				list = append(list, PlayerMatchHistoryEntry{
					UserID:       userID,
					MatchID:      m.ID,
					RoomCode:     m.RoomCode,
					GameMode:     m.GameMode,
					StartedAt:    m.StartedAt,
					FinishedAt:   m.FinishedAt,
					SeatPosition: p.SeatPosition,
					FinalScore:   p.FinalScore,
					RankPosition: p.RankPosition,
					TrophyDelta:  p.TrophyDelta,
					ChipsDelta:   p.ChipsDelta,
					IsWinner:     m.WinnerID == userID,
					WinningScore: m.WinningScore,
				})
				break
			}
		}
		if limit > 0 && len(list) >= limit {
			break
		}
	}
	return list
}

func min(a, b int) int {
	if a < b {
		return a
	}
	return b
}
