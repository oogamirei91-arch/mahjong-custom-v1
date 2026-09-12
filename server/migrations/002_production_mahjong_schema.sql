-- ====================================================================================
-- SKEMA DATABASE LENGKAP & ENTERPRISE: MAHJONG CUSTOM v1.0
-- DIRANCANG BERDASARKAN SPESIFIKASI MODUL 1 S/D MODUL 9
-- PostgreSQL 14+ / Supabase Compatible (Idempotent & Upgrade-Safe)
-- ====================================================================================

-- Aktifkan ekstensi UUID dan Kriptografi
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ====================================================================================
-- 1. TIPE ENUM & DOMAIN (MODUL 1 - 9)
-- ====================================================================================

DO $$ BEGIN
    CREATE TYPE auth_provider_type AS ENUM ('GUEST', 'EMAIL', 'GOOGLE', 'APPLE');
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE user_role_type AS ENUM ('PLAYER', 'VIP_PLAYER', 'MODERATOR', 'ADMIN');
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE rank_tier_type AS ENUM (
        'NOVICE',       -- 0 - 499 Trophy (Novice 🥉)
        'APPRENTICE',   -- 500 - 1,499 Trophy (Apprentice 🥈)
        'EXPERT',       -- 1,500 - 3,999 Trophy (Expert 🥇)
        'MASTER',       -- 4,000 - 9,999 Trophy (Master 💎)
        'VIP_LEGEND'    -- 10,000+ Trophy (VIP Legend 👑)
    );
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE game_mode_type AS ENUM ('SOLO_AI', 'RANKED_MATCH', 'CUSTOM_VIP', 'TOURNAMENT');
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE match_status_type AS ENUM ('WAITING', 'DEALING', 'IN_PROGRESS', 'FINISHED', 'ABORTED');
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE seat_position_type AS ENUM ('EAST', 'SOUTH', 'WEST', 'NORTH');
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE bot_difficulty_type AS ENUM ('HUMAN', 'NOVICE', 'EXPERT', 'MASTER_VIP');
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE round_win_type AS ENUM ('TSUMO', 'RON', 'DRAW', 'ABORTIVE_DRAW');
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE player_action_type AS ENUM (
        'DRAW', 'DISCARD', 'CHOW', 'PONG', 'KONG', 'WIN', 'PASS', 
        'FLOWER_REPLACE', 'AUTO_DISCARD'
    );
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE friendship_status_type AS ENUM ('PENDING', 'ACCEPTED', 'BLOCKED', 'REJECTED');
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE wallet_tx_type AS ENUM (
        'MATCH_WIN', 'MATCH_ENTRY_FEE', 'DAILY_BONUS', 'STORE_PURCHASE', 
        'SYSTEM_GRANT', 'TRANSFER'
    );
EXCEPTION WHEN duplicate_object THEN null; END $$;

-- ====================================================================================
-- 2. TABEL AKUN & OTENTIKASI (MODUL 4 & 5 - Multi-Auth: Google, Email, Guest)
-- ====================================================================================

CREATE TABLE IF NOT EXISTS public.users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(32) UNIQUE NOT NULL,
    password_hash TEXT,
    display_name VARCHAR(64) NOT NULL,
    avatar_id INT DEFAULT 1 NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW() NOT NULL,
    last_login TIMESTAMPTZ DEFAULT NOW() NOT NULL
);

-- Upgrade kolom jika tabel 'users' sudah ada sebelumnya
ALTER TABLE public.users ALTER COLUMN password_hash DROP NOT NULL;
ALTER TABLE public.users ADD COLUMN IF NOT EXISTS email VARCHAR(255);
ALTER TABLE public.users ADD COLUMN IF NOT EXISTS auth_provider VARCHAR(20) DEFAULT 'GUEST';
ALTER TABLE public.users ADD COLUMN IF NOT EXISTS oauth_provider_id VARCHAR(255);
ALTER TABLE public.users ADD COLUMN IF NOT EXISTS avatar_url TEXT;
ALTER TABLE public.users ADD COLUMN IF NOT EXISTS custom_frame_id INT DEFAULT 1;
ALTER TABLE public.users ADD COLUMN IF NOT EXISTS role VARCHAR(20) DEFAULT 'PLAYER';
ALTER TABLE public.users ADD COLUMN IF NOT EXISTS is_active BOOLEAN DEFAULT TRUE;
ALTER TABLE public.users ADD COLUMN IF NOT EXISTS is_banned BOOLEAN DEFAULT FALSE;
ALTER TABLE public.users ADD COLUMN IF NOT EXISTS ban_reason TEXT;
ALTER TABLE public.users ADD COLUMN IF NOT EXISTS created_ip VARCHAR(45);
ALTER TABLE public.users ADD COLUMN IF NOT EXISTS last_login_ip VARCHAR(45);
ALTER TABLE public.users ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ DEFAULT NOW();
ALTER TABLE public.users ADD COLUMN IF NOT EXISTS last_login_at TIMESTAMPTZ DEFAULT NOW();

CREATE INDEX IF NOT EXISTS idx_users_username ON public.users(username);
CREATE INDEX IF NOT EXISTS idx_users_email ON public.users(email);
CREATE INDEX IF NOT EXISTS idx_users_created_at ON public.users(created_at);

-- ====================================================================================
-- 3. TABEL DOMPET & TRANSAKSI CHIPS KASINO (VIP Emerald Economy)
-- ====================================================================================

CREATE TABLE IF NOT EXISTS public.user_wallets (
    user_id UUID PRIMARY KEY REFERENCES public.users(id) ON DELETE CASCADE,
    chips_balance BIGINT DEFAULT 10000 NOT NULL CHECK (chips_balance >= 0),
    diamonds_balance INT DEFAULT 50 NOT NULL CHECK (diamonds_balance >= 0),
    total_chips_earned BIGINT DEFAULT 10000 NOT NULL,
    updated_at TIMESTAMPTZ DEFAULT NOW() NOT NULL
);

-- Inisialisasi wallet untuk user yang sudah ada
INSERT INTO public.user_wallets (user_id, chips_balance, diamonds_balance, total_chips_earned)
SELECT id, 10000, 50, 10000 FROM public.users
ON CONFLICT (user_id) DO NOTHING;

CREATE TABLE IF NOT EXISTS public.wallet_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    tx_type VARCHAR(30) NOT NULL,
    amount BIGINT NOT NULL,
    balance_after BIGINT NOT NULL,
    reference_id VARCHAR(64),
    description TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW() NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_wallet_tx_user ON public.wallet_transactions(user_id, created_at DESC);

-- ====================================================================================
-- 4. TABEL STATISTIK & PROGRESI RANK TIER (MODUL 5 & 8)
-- ====================================================================================

CREATE TABLE IF NOT EXISTS public.user_stats (
    user_id UUID PRIMARY KEY REFERENCES public.users(id) ON DELETE CASCADE,
    total_matches INT DEFAULT 0 NOT NULL,
    total_wins INT DEFAULT 0 NOT NULL,
    trophy_points BIGINT DEFAULT 0 NOT NULL,
    rank_tier VARCHAR(30) DEFAULT 'Novice 🥉' NOT NULL,
    highest_score INT DEFAULT 0 NOT NULL,
    updated_at TIMESTAMPTZ DEFAULT NOW() NOT NULL
);

ALTER TABLE public.user_stats ADD COLUMN IF NOT EXISTS total_draws INT DEFAULT 0;
ALTER TABLE public.user_stats ADD COLUMN IF NOT EXISTS total_losses INT DEFAULT 0;
ALTER TABLE public.user_stats ADD COLUMN IF NOT EXISTS win_rate NUMERIC(5, 2) DEFAULT 0.00;
ALTER TABLE public.user_stats ADD COLUMN IF NOT EXISTS highest_match_score INT DEFAULT 0;
ALTER TABLE public.user_stats ADD COLUMN IF NOT EXISTS highest_round_score INT DEFAULT 0;
ALTER TABLE public.user_stats ADD COLUMN IF NOT EXISTS current_win_streak INT DEFAULT 0;
ALTER TABLE public.user_stats ADD COLUMN IF NOT EXISTS highest_win_streak INT DEFAULT 0;
ALTER TABLE public.user_stats ADD COLUMN IF NOT EXISTS tsumo_wins INT DEFAULT 0;
ALTER TABLE public.user_stats ADD COLUMN IF NOT EXISTS ron_wins INT DEFAULT 0;
ALTER TABLE public.user_stats ADD COLUMN IF NOT EXISTS special_hands_record JSONB DEFAULT '{
    "thirteen_orphans": 0, "nine_gates": 0, "all_green": 0, "big_four_winds": 0,
    "all_honors": 0, "little_four_winds": 0, "big_three_dragons": 0, "all_terminals": 0,
    "all_kongs": 0, "four_concealed_pongs": 0, "little_three_dragons": 0, "pure_flush": 0,
    "seven_pairs": 0, "mixed_flush": 0, "all_pongs": 0
}'::jsonb;

CREATE INDEX IF NOT EXISTS idx_user_stats_trophy ON public.user_stats(trophy_points DESC, total_wins DESC);

-- ====================================================================================
-- 5. TABEL PERTEMANAN & UNDANGAN MABAR (MODUL 4, 5, 7, 8)
-- ====================================================================================

CREATE TABLE IF NOT EXISTS public.friendships (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    friend_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    status VARCHAR(20) DEFAULT 'PENDING' NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW() NOT NULL,
    updated_at TIMESTAMPTZ DEFAULT NOW() NOT NULL,
    CONSTRAINT uq_friend_pair UNIQUE(user_id, friend_id)
);

ALTER TABLE public.friendships ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ DEFAULT NOW();

CREATE INDEX IF NOT EXISTS idx_friendships_user ON public.friendships(user_id, status);
CREATE INDEX IF NOT EXISTS idx_friendships_friend ON public.friendships(friend_id, status);

-- ====================================================================================
-- 6. TABEL CUSTOM ROOM & LOBBY VIP (MODUL 3, 4, 8)
-- ====================================================================================

CREATE TABLE IF NOT EXISTS public.custom_rooms (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    room_code VARCHAR(16) UNIQUE NOT NULL,
    host_user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    game_mode VARCHAR(30) DEFAULT 'CUSTOM_VIP' NOT NULL,
    min_entry_chips BIGINT DEFAULT 1000 NOT NULL,
    max_players INT DEFAULT 4 NOT NULL,
    auto_fill_bots BOOLEAN DEFAULT TRUE NOT NULL,
    bot_difficulty VARCHAR(30) DEFAULT 'EXPERT' NOT NULL,
    turn_timer_seconds INT DEFAULT 15 NOT NULL,
    reaction_timer_seconds INT DEFAULT 5 NOT NULL,
    is_private BOOLEAN DEFAULT TRUE NOT NULL,
    room_passcode VARCHAR(16),
    status VARCHAR(20) DEFAULT 'WAITING' NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW() NOT NULL,
    closed_at TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_custom_rooms_code ON public.custom_rooms(room_code) WHERE status = 'WAITING';

-- ====================================================================================
-- 7. TABEL PERTANDINGAN (MATCHES) & KURSI PEMAIN (MODUL 3, 4, 5, 9)
-- ====================================================================================

CREATE TABLE IF NOT EXISTS public.matches (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    room_id UUID REFERENCES public.custom_rooms(id) ON DELETE SET NULL,
    room_code VARCHAR(16) NOT NULL,
    game_mode VARCHAR(30) DEFAULT 'RANKED_MATCH' NOT NULL,
    status VARCHAR(20) DEFAULT 'IN_PROGRESS' NOT NULL,
    total_rounds INT DEFAULT 4 NOT NULL,
    current_round_num INT DEFAULT 1 NOT NULL,
    initial_dealer_seat VARCHAR(10) DEFAULT 'EAST' NOT NULL,
    winner_user_id UUID REFERENCES public.users(id) ON DELETE SET NULL,
    winning_score INT DEFAULT 0,
    started_at TIMESTAMPTZ DEFAULT NOW() NOT NULL,
    finished_at TIMESTAMPTZ,
    server_node_id VARCHAR(64)
);

CREATE INDEX IF NOT EXISTS idx_matches_winner ON public.matches(winner_user_id);
CREATE INDEX IF NOT EXISTS idx_matches_started_at ON public.matches(started_at DESC);
CREATE INDEX IF NOT EXISTS idx_matches_room_code ON public.matches(room_code);

-- Tabel Detail 4 Kursi Pemain / Bot dalam 1 Match
CREATE TABLE IF NOT EXISTS public.match_players (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    match_id UUID NOT NULL REFERENCES public.matches(id) ON DELETE CASCADE,
    user_id UUID REFERENCES public.users(id) ON DELETE SET NULL,
    seat_position VARCHAR(10) NOT NULL,
    seat_index INT NOT NULL CHECK (seat_index BETWEEN 0 AND 3),
    is_bot BOOLEAN DEFAULT FALSE NOT NULL,
    bot_name VARCHAR(50),
    bot_difficulty VARCHAR(30) DEFAULT 'HUMAN' NOT NULL,
    initial_chips BIGINT DEFAULT 10000 NOT NULL,
    final_score INT DEFAULT 0 NOT NULL,
    rank_position INT CHECK (rank_position BETWEEN 1 AND 4),
    trophy_delta INT DEFAULT 0 NOT NULL,
    chips_delta BIGINT DEFAULT 0 NOT NULL,
    is_disconnected BOOLEAN DEFAULT FALSE NOT NULL,
    CONSTRAINT uq_match_seat UNIQUE (match_id, seat_position),
    CONSTRAINT uq_match_seat_idx UNIQUE (match_id, seat_index)
);

CREATE INDEX IF NOT EXISTS idx_match_players_match ON public.match_players(match_id);
CREATE INDEX IF NOT EXISTS idx_match_players_user ON public.match_players(user_id);

-- ====================================================================================
-- 8. TABEL DETAIL RONDE (MATCH ROUNDS) & SKORING YAKU (MODUL 1, 2, 3)
-- ====================================================================================

CREATE TABLE IF NOT EXISTS public.match_rounds (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    match_id UUID NOT NULL REFERENCES public.matches(id) ON DELETE CASCADE,
    round_number INT NOT NULL CHECK (round_number BETWEEN 1 AND 4),
    dealer_seat VARCHAR(10) NOT NULL,
    wall_seed VARCHAR(64) NOT NULL,
    winner_seat VARCHAR(10),
    winner_match_player_id UUID REFERENCES public.match_players(id) ON DELETE SET NULL,
    loser_seat VARCHAR(10),
    win_type VARCHAR(20) DEFAULT 'DRAW' NOT NULL,
    base_points INT DEFAULT 0 NOT NULL,
    self_draw_bonus INT DEFAULT 0 NOT NULL,
    discard_win_bonus INT DEFAULT 0 NOT NULL,
    melds_points INT DEFAULT 0 NOT NULL,
    flower_season_points INT DEFAULT 0 NOT NULL,
    special_hand_points INT DEFAULT 0 NOT NULL,
    total_round_score INT DEFAULT 0 NOT NULL,
    winning_hand_json JSONB,
    remaining_wall_tiles INT DEFAULT 0 NOT NULL,
    started_at TIMESTAMPTZ DEFAULT NOW() NOT NULL,
    finished_at TIMESTAMPTZ,
    CONSTRAINT uq_match_round_number UNIQUE (match_id, round_number)
);

CREATE INDEX IF NOT EXISTS idx_match_rounds_match ON public.match_rounds(match_id);

-- Rincian Breakdown Skoring Yaku / Kombinasi per Ronde
CREATE TABLE IF NOT EXISTS public.round_score_breakdowns (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    round_id UUID NOT NULL REFERENCES public.match_rounds(id) ON DELETE CASCADE,
    category_code VARCHAR(50) NOT NULL,
    category_name VARCHAR(100) NOT NULL,
    points_awarded INT NOT NULL,
    description TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW() NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_round_score_breakdown ON public.round_score_breakdowns(round_id);

-- ====================================================================================
-- 9. TABEL LOG AKSI GAMEPLAY & REPLAY SYSTEM (MODUL 1, 3, 4, 7, 8)
-- ====================================================================================

CREATE TABLE IF NOT EXISTS public.round_actions_log (
    id BIGSERIAL PRIMARY KEY,
    round_id UUID NOT NULL REFERENCES public.match_rounds(id) ON DELETE CASCADE,
    step_number INT NOT NULL,
    seat_position VARCHAR(10) NOT NULL,
    action_type VARCHAR(20) NOT NULL,
    tile_id INT,
    tile_code VARCHAR(16),
    is_auto_discard BOOLEAN DEFAULT FALSE NOT NULL,
    response_time_ms INT DEFAULT 0 NOT NULL,
    payload_json JSONB,
    created_at TIMESTAMPTZ DEFAULT NOW() NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_round_actions_replay ON public.round_actions_log(round_id, step_number);

-- ====================================================================================
-- 10. TABEL AUDIT & ANTI-CHEAT SYSTEM (MODUL 4 & 5)
-- ====================================================================================

CREATE TABLE IF NOT EXISTS public.audit_anti_cheat_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES public.users(id) ON DELETE SET NULL,
    match_id UUID REFERENCES public.matches(id) ON DELETE SET NULL,
    round_id UUID REFERENCES public.match_rounds(id) ON DELETE SET NULL,
    event_type VARCHAR(64) NOT NULL,
    severity VARCHAR(16) DEFAULT 'WARNING' NOT NULL,
    details JSONB NOT NULL,
    ip_address VARCHAR(45),
    client_version VARCHAR(32),
    created_at TIMESTAMPTZ DEFAULT NOW() NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_audit_user ON public.audit_anti_cheat_logs(user_id, created_at DESC);

-- ====================================================================================
-- 11. DATABASE VIEWS (LEADERBOARD & CAREER PROFILES)
-- ====================================================================================

-- View Leaderboard Global
CREATE OR REPLACE VIEW public.v_leaderboard_global AS
SELECT 
    ROW_NUMBER() OVER(ORDER BY s.trophy_points DESC, s.total_wins DESC, s.win_rate DESC) AS rank,
    u.id AS user_id,
    u.username,
    u.display_name,
    u.avatar_id,
    COALESCE(u.custom_frame_id, 1) AS custom_frame_id,
    COALESCE(w.chips_balance, 0) AS chips_balance,
    s.trophy_points,
    s.rank_tier,
    s.total_matches,
    s.total_wins,
    s.total_draws,
    s.total_losses,
    s.win_rate,
    s.highest_score AS highest_match_score,
    s.highest_win_streak
FROM public.users u
JOIN public.user_stats s ON u.id = s.user_id
LEFT JOIN public.user_wallets w ON u.id = w.user_id
WHERE u.is_banned = FALSE AND u.is_active = TRUE
ORDER BY s.trophy_points DESC, s.total_wins DESC;

-- View Ringkasan Riwayat Match Pemain
CREATE OR REPLACE VIEW public.v_player_match_history AS
SELECT 
    m.id AS match_id,
    m.room_code,
    m.game_mode,
    m.started_at,
    m.finished_at,
    mp.user_id,
    mp.seat_position,
    mp.rank_position,
    mp.final_score,
    mp.trophy_delta,
    mp.chips_delta,
    (m.winner_user_id = mp.user_id) AS is_winner,
    m.winning_score
FROM public.matches m
JOIN public.match_players mp ON m.id = mp.match_id
ORDER BY m.started_at DESC;

-- ====================================================================================
-- 12. STORED PROCEDURES & TRIGGERS
-- ====================================================================================

CREATE OR REPLACE FUNCTION public.fn_update_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_users_timestamp ON public.users;
CREATE TRIGGER trg_users_timestamp BEFORE UPDATE ON public.users
FOR EACH ROW EXECUTE FUNCTION public.fn_update_timestamp();

DROP TRIGGER IF EXISTS trg_user_stats_timestamp ON public.user_stats;
CREATE TRIGGER trg_user_stats_timestamp BEFORE UPDATE ON public.user_stats
FOR EACH ROW EXECUTE FUNCTION public.fn_update_timestamp();

DROP TRIGGER IF EXISTS trg_user_wallets_timestamp ON public.user_wallets;
CREATE TRIGGER trg_user_wallets_timestamp BEFORE UPDATE ON public.user_wallets
FOR EACH ROW EXECUTE FUNCTION public.fn_update_timestamp();

-- Fungsi Perhitungan Otomatis Rank Tier Berdasarkan Trophy Points
CREATE OR REPLACE FUNCTION public.fn_calculate_rank_tier_str(trophies BIGINT)
RETURNS VARCHAR(30) AS $$
BEGIN
    IF trophies >= 10000 THEN
        RETURN 'VIP Legend 👑';
    ELSIF trophies >= 4000 THEN
        RETURN 'Master 💎';
    ELSIF trophies >= 1500 THEN
        RETURN 'Expert 🥇';
    ELSIF trophies >= 500 THEN
        RETURN 'Apprentice 🥈';
    ELSE
        RETURN 'Novice 🥉';
    END IF;
END;
$$ LANGUAGE plpgsql IMMUTABLE;

-- Trigger Update Otomatis Rank Tier dan Win Rate saat UserStats di-update
CREATE OR REPLACE FUNCTION public.fn_recalc_user_stats()
RETURNS TRIGGER AS $$
BEGIN
    NEW.rank_tier = public.fn_calculate_rank_tier_str(NEW.trophy_points);
    IF NEW.total_matches > 0 THEN
        NEW.win_rate = ROUND((NEW.total_wins::NUMERIC / NEW.total_matches::NUMERIC) * 100.0, 2);
    ELSE
        NEW.win_rate = 0.00;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_recalc_user_stats ON public.user_stats;
CREATE TRIGGER trg_recalc_user_stats BEFORE INSERT OR UPDATE OF trophy_points, total_matches, total_wins ON public.user_stats
FOR EACH ROW EXECUTE FUNCTION public.fn_recalc_user_stats();
