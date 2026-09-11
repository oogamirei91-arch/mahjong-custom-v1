-- =====================================================================
-- SKEMA DATABASE POSTGRESQL (SUPABASE)
-- MAHJONG CUSTOM v1.0 (LENGKAP: USER, RANKING, MATCH, FRIEND LIST)
-- =====================================================================

-- 1. TABEL PENGGUNA / AKUN PEMAIN
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(30) UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    display_name VARCHAR(50) NOT NULL,
    avatar_id INT DEFAULT 1,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    last_login TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);

-- 2. TABEL STATISTIK, TROPHY & PERINGKAT (1:1 DENGAN USERS)
CREATE TABLE IF NOT EXISTS user_stats (
    user_id UUID PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    total_matches INT DEFAULT 0,
    total_wins INT DEFAULT 0,
    trophy_points BIGINT DEFAULT 0,
    rank_tier VARCHAR(20) DEFAULT 'Novice',
    highest_score INT DEFAULT 0,
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_user_stats_trophy ON user_stats(trophy_points DESC);

-- 3. TABEL SISTEM PERTEMANAN (FRIEND LIST)
CREATE TABLE IF NOT EXISTS friendships (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    friend_id UUID REFERENCES users(id) ON DELETE CASCADE,
    status VARCHAR(20) DEFAULT 'PENDING', -- 'PENDING', 'ACCEPTED', 'BLOCKED'
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(user_id, friend_id)
);

CREATE INDEX IF NOT EXISTS idx_friendships_user ON friendships(user_id, status);
CREATE INDEX IF NOT EXISTS idx_friendships_friend ON friendships(friend_id, status);

-- 4. TABEL RIWAYAT PERTANDINGAN (MATCH RECORDS - RANKED & CUSTOM)
CREATE TABLE IF NOT EXISTS match_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    room_code VARCHAR(15) NOT NULL,
    game_mode VARCHAR(20) DEFAULT 'RANKED', -- 'RANKED' atau 'CUSTOM'
    winner_id UUID REFERENCES users(id) ON DELETE SET NULL,
    winning_type VARCHAR(30) DEFAULT 'NORMAL_WIN',
    total_rounds INT DEFAULT 4,
    player_scores JSONB NOT NULL,
    played_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_match_records_winner ON match_records(winner_id);
CREATE INDEX IF NOT EXISTS idx_match_records_played_at ON match_records(played_at DESC);

-- 5. VIEW LEADERBOARD GLOBAL (TOP PLAYERS)
CREATE OR REPLACE VIEW v_leaderboard AS
SELECT 
    u.id,
    u.username,
    u.display_name,
    u.avatar_id,
    s.total_matches,
    s.total_wins,
    s.trophy_points,
    s.rank_tier,
    s.highest_score,
    CASE 
        WHEN s.total_matches > 0 THEN ROUND((s.total_wins::NUMERIC / s.total_matches::NUMERIC) * 100, 1) 
        ELSE 0 
    END AS win_rate_percentage
FROM users u
JOIN user_stats s ON u.id = s.user_id
ORDER BY s.trophy_points DESC, s.total_wins DESC;
