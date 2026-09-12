package database

import (
	"database/sql"
	"encoding/json"
	"errors"
	"fmt"
	"log"
	"time"

	_ "github.com/lib/pq"
)

// PostgresRepository mengelola persistensi data langsung ke Cloud Supabase PostgreSQL.
type PostgresRepository struct {
	db *sql.DB
}

// NewPostgresRepository membuat koneksi aktif ke Supabase PostgreSQL.
func NewPostgresRepository(databaseURL string) (*PostgresRepository, error) {
	db, err := sql.Open("postgres", databaseURL)
	if err != nil {
		return nil, fmt.Errorf("gagal membuka koneksi postgres: %w", err)
	}

	db.SetConnMaxLifetime(time.Minute * 3)
	db.SetMaxOpenConns(10)
	db.SetMaxIdleConns(5)

	if err := db.Ping(); err != nil {
		return nil, fmt.Errorf("ping ke Supabase PostgreSQL gagal: %w", err)
	}

	log.Println("✅ [Database] Berhasil terhubung ke Supabase Cloud PostgreSQL!")
	return &PostgresRepository{db: db}, nil
}

// Close menutup pool koneksi database.
func (r *PostgresRepository) Close() error {
	if r.db != nil {
		return r.db.Close()
	}
	return nil
}

// --- AKUN & OTENTIKASI (MULTI-AUTH) ---

// RegisterUser mendaftarkan user baru dengan email & password di Supabase.
func (r *PostgresRepository) RegisterUser(username, email, password, displayName string, avatarID int) (*User, error) {
	if username == "" || password == "" {
		return nil, errors.New("username dan password tidak boleh kosong")
	}
	if displayName == "" {
		displayName = username
	}
	if avatarID <= 0 {
		avatarID = 1
	}

	salt := fmt.Sprintf("salt_%s", username)
	passHash := HashPassword(password, salt)

	var user User
	err := r.db.QueryRow(`
		INSERT INTO public.users (username, email, password_hash, display_name, avatar_id, auth_provider, role)
		VALUES ($1, $2, $3, $4, $5, 'EMAIL', 'PLAYER')
		RETURNING id, username, COALESCE(email, ''), display_name, avatar_id, COALESCE(custom_frame_id, 1), role, is_active, is_banned, created_at, updated_at, last_login_at;
	`, username, email, passHash, displayName, avatarID).Scan(
		&user.ID, &user.Username, &user.Email, &user.DisplayName, &user.AvatarID,
		&user.CustomFrameID, &user.Role, &user.IsActive, &user.IsBanned,
		&user.CreatedAt, &user.UpdatedAt, &user.LastLogin,
	)
	if err != nil {
		return nil, fmt.Errorf("gagal mendaftarkan user ke Supabase: %w", err)
	}
	user.AuthProvider = AuthProviderEmail

	// Buat wallet awal 25,000 chips
	_, _ = r.db.Exec(`
		INSERT INTO public.user_wallets (user_id, chips_balance, diamonds_balance, total_chips_earned)
		VALUES ($1, 25000, 50, 25000)
		ON CONFLICT (user_id) DO NOTHING;
	`, user.ID)

	// Inisialisasi statistik awal
	_, _ = r.db.Exec(`
		INSERT INTO public.user_stats (user_id, trophy_points, rank_tier)
		VALUES ($1, 0, 'Novice 🥉')
		ON CONFLICT (user_id) DO NOTHING;
	`, user.ID)

	return &user, nil
}

// AuthenticateUser memverifikasi login username/email dan password di Supabase.
func (r *PostgresRepository) AuthenticateUser(identifier, password string) (*User, error) {
	var user User
	var passHash string
	var authProv string

	err := r.db.QueryRow(`
		SELECT id, username, COALESCE(email, ''), password_hash, display_name, avatar_id, COALESCE(custom_frame_id, 1), auth_provider, role, is_active, is_banned, created_at, updated_at, last_login_at
		FROM public.users
		WHERE (username = $1 OR email = $1) AND is_banned = FALSE
		LIMIT 1;
	`, identifier).Scan(
		&user.ID, &user.Username, &user.Email, &passHash, &user.DisplayName, &user.AvatarID,
		&user.CustomFrameID, &authProv, &user.Role, &user.IsActive, &user.IsBanned,
		&user.CreatedAt, &user.UpdatedAt, &user.LastLogin,
	)
	if err != nil {
		if err == sql.ErrNoRows {
			return nil, errors.New("username atau password salah")
		}
		return nil, fmt.Errorf("gagal query akun: %w", err)
	}

	user.AuthProvider = AuthProvider(authProv)

	// Verifikasi salt hash
	salt := fmt.Sprintf("salt_%s", user.Username)
	inputHash := HashPassword(password, salt)
	if passHash != inputHash {
		return nil, errors.New("username atau password salah")
	}

	// Update last login
	_, _ = r.db.Exec(`UPDATE public.users SET last_login_at = NOW() WHERE id = $1;`, user.ID)
	return &user, nil
}

// CheckUserExists mengecek apakah akun dengan username atau email sudah ada di Supabase.
func (r *PostgresRepository) CheckUserExists(identifier string) (bool, error) {
	var count int
	err := r.db.QueryRow(`
		SELECT COUNT(*) FROM public.users WHERE username = $1 OR email = $1;
	`, identifier).Scan(&count)
	if err != nil {
		return false, err
	}
	return count > 0, nil
}

// CheckGuestExists mengecek apakah perangkat tamu sudah terdaftar di Supabase.
func (r *PostgresRepository) CheckGuestExists(guestDeviceID string) (*User, error) {
	var user User
	var authProv string
	err := r.db.QueryRow(`
		SELECT id, username, COALESCE(email, ''), display_name, avatar_id, COALESCE(custom_frame_id, 1), auth_provider, role, is_active, is_banned, created_at, updated_at, last_login_at
		FROM public.users WHERE auth_provider = 'GUEST' AND oauth_provider_id = $1 LIMIT 1;
	`, guestDeviceID).Scan(
		&user.ID, &user.Username, &user.Email, &user.DisplayName, &user.AvatarID,
		&user.CustomFrameID, &authProv, &user.Role, &user.IsActive, &user.IsBanned,
		&user.CreatedAt, &user.UpdatedAt, &user.LastLogin,
	)
	if err != nil {
		if err == sql.ErrNoRows {
			return nil, nil // Tidak ditemukan
		}
		return nil, err
	}
	user.AuthProvider = AuthProvider(authProv)
	_, _ = r.db.Exec(`UPDATE public.users SET last_login_at = NOW() WHERE id = $1;`, user.ID)
	return &user, nil
}

// RegisterGuest mendaftarkan perangkat tamu baru ke database Supabase.
func (r *PostgresRepository) RegisterGuest(guestDeviceID, displayName string) (*User, error) {
	if guestDeviceID == "" {
		guestDeviceID = fmt.Sprintf("dev_%d", time.Now().UnixNano())
	}
	if displayName == "" {
		displayName = "VIP Player"
	}
	username := fmt.Sprintf("Guest_%s", guestDeviceID[max(0, len(guestDeviceID)-6):])

	var user User
	err := r.db.QueryRow(`
		INSERT INTO public.users (username, display_name, auth_provider, oauth_provider_id, role)
		VALUES ($1, $2, 'GUEST', $3, 'PLAYER')
		RETURNING id, username, COALESCE(email, ''), display_name, avatar_id, COALESCE(custom_frame_id, 1), role, is_active, is_banned, created_at, updated_at, last_login_at;
	`, username, displayName, guestDeviceID).Scan(
		&user.ID, &user.Username, &user.Email, &user.DisplayName, &user.AvatarID,
		&user.CustomFrameID, &user.Role, &user.IsActive, &user.IsBanned,
		&user.CreatedAt, &user.UpdatedAt, &user.LastLogin,
	)
	if err != nil {
		return nil, fmt.Errorf("gagal registrasi akun tamu ke Supabase: %w", err)
	}
	user.AuthProvider = AuthProviderGuest

	// Inisialisasi Wallet 10,000 Chips
	_, _ = r.db.Exec(`
		INSERT INTO public.user_wallets (user_id, chips_balance, diamonds_balance, total_chips_earned)
		VALUES ($1, 10000, 50, 10000)
		ON CONFLICT (user_id) DO NOTHING;
	`, user.ID)

	// Inisialisasi Stats Novice Tier
	_, _ = r.db.Exec(`
		INSERT INTO public.user_stats (user_id, trophy_points, rank_tier)
		VALUES ($1, 0, 'Novice 🥉')
		ON CONFLICT (user_id) DO NOTHING;
	`, user.ID)

	return &user, nil
}

// RegisterOrLoginGuest membuat / mengautentikasi akun mode tamu di Supabase.
func (r *PostgresRepository) RegisterOrLoginGuest(guestDeviceID, displayName string) (*User, error) {
	existing, err := r.CheckGuestExists(guestDeviceID)
	if err == nil && existing != nil {
		return existing, nil
	}
	return r.RegisterGuest(guestDeviceID, displayName)
}

// CheckOAuthExists mengecek apakah akun OAuth (Google/Apple) sudah terdaftar di Supabase.
func (r *PostgresRepository) CheckOAuthExists(provider AuthProvider, oauthID string) (*User, error) {
	var user User
	var authProv string
	err := r.db.QueryRow(`
		SELECT id, username, COALESCE(email, ''), display_name, avatar_id, COALESCE(custom_frame_id, 1), auth_provider, role, is_active, is_banned, created_at, updated_at, last_login_at
		FROM public.users WHERE auth_provider = $1 AND oauth_provider_id = $2 LIMIT 1;
	`, string(provider), oauthID).Scan(
		&user.ID, &user.Username, &user.Email, &user.DisplayName, &user.AvatarID,
		&user.CustomFrameID, &authProv, &user.Role, &user.IsActive, &user.IsBanned,
		&user.CreatedAt, &user.UpdatedAt, &user.LastLogin,
	)
	if err != nil {
		if err == sql.ErrNoRows {
			return nil, nil
		}
		return nil, err
	}
	user.AuthProvider = AuthProvider(authProv)
	_, _ = r.db.Exec(`UPDATE public.users SET last_login_at = NOW() WHERE id = $1;`, user.ID)
	return &user, nil
}

// RegisterOAuthUser mendaftarkan akun OAuth baru (Google/Apple) ke Supabase.
func (r *PostgresRepository) RegisterOAuthUser(provider AuthProvider, oauthID, email, displayName, avatarURL string) (*User, error) {
	if displayName == "" {
		displayName = "VIP Master"
	}
	username := fmt.Sprintf("%s_%s", string(provider), oauthID[:min(6, len(oauthID))])

	var user User
	err := r.db.QueryRow(`
		INSERT INTO public.users (username, email, display_name, auth_provider, oauth_provider_id, avatar_url, role)
		VALUES ($1, $2, $3, $4, $5, $6, 'VIP_PLAYER')
		RETURNING id, username, COALESCE(email, ''), display_name, avatar_id, COALESCE(custom_frame_id, 1), role, is_active, is_banned, created_at, updated_at, last_login_at;
	`, username, email, displayName, string(provider), oauthID, avatarURL).Scan(
		&user.ID, &user.Username, &user.Email, &user.DisplayName, &user.AvatarID,
		&user.CustomFrameID, &user.Role, &user.IsActive, &user.IsBanned,
		&user.CreatedAt, &user.UpdatedAt, &user.LastLogin,
	)
	if err != nil {
		return nil, fmt.Errorf("gagal registrasi user OAuth ke Supabase: %w", err)
	}
	user.AuthProvider = provider

	// Wallet VIP: 50,000 chips
	_, _ = r.db.Exec(`
		INSERT INTO public.user_wallets (user_id, chips_balance, diamonds_balance, total_chips_earned)
		VALUES ($1, 50000, 100, 50000)
		ON CONFLICT (user_id) DO NOTHING;
	`, user.ID)

	// Stats Awal: 500 Trophy (Apprentice)
	_, _ = r.db.Exec(`
		INSERT INTO public.user_stats (user_id, trophy_points, rank_tier)
		VALUES ($1, 500, 'Apprentice 🥈')
		ON CONFLICT (user_id) DO NOTHING;
	`, user.ID)

	return &user, nil
}

// GetUserProfile mengambil profil, saldo wallet, dan statistik trofi dari Supabase.
func (r *PostgresRepository) GetUserProfile(userID string) (*User, *UserWallet, *UserStats, error) {
	var user User
	var authProv string
	err := r.db.QueryRow(`
		SELECT id, username, COALESCE(email, ''), display_name, avatar_id, COALESCE(custom_frame_id, 1), auth_provider, role, is_active, is_banned, created_at, updated_at, last_login_at
		FROM public.users WHERE id = $1;
	`, userID).Scan(
		&user.ID, &user.Username, &user.Email, &user.DisplayName, &user.AvatarID,
		&user.CustomFrameID, &authProv, &user.Role, &user.IsActive, &user.IsBanned,
		&user.CreatedAt, &user.UpdatedAt, &user.LastLogin,
	)
	if err != nil {
		return nil, nil, nil, fmt.Errorf("user tidak ditemukan: %w", err)
	}
	user.AuthProvider = AuthProvider(authProv)

	var wallet UserWallet
	wallet.UserID = userID
	_ = r.db.QueryRow(`
		SELECT chips_balance, diamonds_balance, total_chips_earned, updated_at
		FROM public.user_wallets WHERE user_id = $1;
	`, userID).Scan(&wallet.ChipsBalance, &wallet.DiamondsBalance, &wallet.TotalChipsEarned, &wallet.UpdatedAt)

	var stats UserStats
	stats.UserID = userID
	var specHandsJSON []byte
	_ = r.db.QueryRow(`
		SELECT trophy_points, rank_tier, total_matches, total_wins, total_draws, total_losses, win_rate, highest_score, special_hands_record, updated_at
		FROM public.user_stats WHERE user_id = $1;
	`, userID).Scan(
		&stats.TrophyPoints, &stats.RankTier, &stats.TotalMatches, &stats.TotalWins,
		&stats.TotalDraws, &stats.TotalLosses, &stats.WinRate, &stats.HighestMatchScore,
		&specHandsJSON, &stats.UpdatedAt,
	)
	if len(specHandsJSON) > 0 {
		_ = json.Unmarshal(specHandsJSON, &stats.SpecialHandsRecord)
	}

	return &user, &wallet, &stats, nil
}

// GetLeaderboard mengambil Top N peringkat pemain dari View Supabase.
func (r *PostgresRepository) GetLeaderboard(limit int) []LeaderboardEntry {
	if limit <= 0 {
		limit = 50
	}

	rows, err := r.db.Query(`
		SELECT rank, user_id, username, display_name, avatar_id, custom_frame_id, chips_balance, trophy_points, rank_tier, total_wins, total_matches, win_rate, highest_match_score
		FROM public.v_leaderboard_global
		LIMIT $1;
	`, limit)
	if err != nil {
		log.Printf("⚠️ [Database] Gagal query leaderboard Supabase: %v", err)
		return nil
	}
	defer rows.Close()

	var entries []LeaderboardEntry
	for rows.Next() {
		var e LeaderboardEntry
		if err := rows.Scan(
			&e.Rank, &e.UserID, &e.Username, &e.DisplayName, &e.AvatarID,
			&e.CustomFrameID, &e.ChipsBalance, &e.TrophyPoints, &e.RankTier,
			&e.TotalWins, &e.TotalMatches, &e.WinRate, &e.HighestMatchScore,
		); err == nil {
			entries = append(entries, e)
		}
	}
	return entries
}

// RecordMatchOutcome menyimpan hasil match 4 ronde ke Supabase dan memperbarui trofi & chips.
func (r *PostgresRepository) RecordMatchOutcome(roomCode, gameMode, winnerID, winningType string, playerScores map[string]int, highestRoundScore int) error {
	var matchID string
	err := r.db.QueryRow(`
		INSERT INTO public.matches (room_code, game_mode, status, total_rounds, winner_user_id, winning_score, finished_at)
		VALUES ($1, $2, 'FINISHED', 4, $3, $4, NOW())
		RETURNING id;
	`, roomCode, gameMode, winnerID, highestRoundScore).Scan(&matchID)
	if err != nil {
		return fmt.Errorf("gagal insert matches Supabase: %w", err)
	}

	idx := 0
	for uid, score := range playerScores {
		isWinner := (uid == winnerID)
		trophyDelta := -25
		chipsDelta := int64(score * 10)
		rankPos := 2
		if isWinner {
			trophyDelta = 40
			rankPos = 1
		}

		seatPos := []string{"EAST", "SOUTH", "WEST", "NORTH"}[idx%4]
		_, _ = r.db.Exec(`
			INSERT INTO public.match_players (match_id, user_id, seat_position, seat_index, is_bot, final_score, rank_position, trophy_delta, chips_delta)
			VALUES ($1, $2, $3, $4, FALSE, $5, $6, $7, $8);
		`, matchID, uid, seatPos, idx%4, score, rankPos, trophyDelta, chipsDelta)

		// Update Stats & Trofi
		_, _ = r.db.Exec(`
			UPDATE public.user_stats
			SET total_matches = total_matches + 1,
				total_wins = total_wins + (CASE WHEN $2 = 1 THEN 1 ELSE 0 END),
				trophy_points = GREATEST(0, trophy_points + $3),
				highest_score = GREATEST(highest_score, $4),
				updated_at = NOW()
			WHERE user_id = $1;
		`, uid, rankPos, trophyDelta, score)

		// Update Wallet
		_, _ = r.db.Exec(`
			UPDATE public.user_wallets
			SET chips_balance = GREATEST(0, chips_balance + $2),
				total_chips_earned = total_chips_earned + GREATEST(0, $2),
				updated_at = NOW()
			WHERE user_id = $1;
		`, uid, chipsDelta)

		idx++
	}

	return nil
}

func max(a, b int) int {
	if a > b {
		return a
	}
	return b
}
