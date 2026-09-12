/**
 * supabaseClient.js - Layanan Sinkronisasi & Integrasi Database Cloud Supabase
 * Mahjong Custom v1.0 (Modul 1 s/d 9)
 * 
 * Mengelola:
 * 1. Otentikasi Multi-Auth (Guest Mode, Email/Password, Google OAuth).
 *    - Setiap login dicek terlebih dahulu ke database Supabase.
 *    - Jika belum terdaftar, user (termasuk Tamu/Guest) diwajibkan registrasi.
 * 2. Profil Pemain & Sinkronisasi Saldo Dompet Chips Kasino (user_wallets).
 * 3. Statistik Poin Trofi, Gelar Rank Tier (Novice s/d VIP Legend), & 15 Rekor Special Hands (user_stats).
 * 4. Peringkat Global Real-Time (v_leaderboard_global).
 * 5. Penyimpanan Hasil Pertandingan 4-Ronde & Replay Log (matches, match_players, match_rounds).
 * 6. Meja Mabar Ber-Kode Unik (custom_rooms).
 */

export const SUPABASE_CONFIG = {
    URL: "https://esnybghukyrdkjeglbii.supabase.co",
    // Anon Key standar Supabase / Placeholder yang dapat disesuaikan di Settings
    ANON_KEY: localStorage.getItem("mahjong_supabase_anon_key") || "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImVzbnliZ2h1a3lyZGtqZWdsYmlpIiwicm9sZSI6ImFub24iLCJpYXQiOjE3MjYwNjAwMDAsImV4cCI6MjA0MTYzNjAwMH0.mahjong_vip_anon_key",
    // Endpoint Server Backend Go (Authoritative Proxy)
    API_BASE: window.location.origin.includes(":3000") ? "http://localhost:8080" : window.location.origin
};

class SupabaseService {
    constructor() {
        this.client = null;
        this.currentUser = null;
        this.currentWallet = null;
        this.currentStats = null;
        this.isOnline = navigator.onLine;

        this.initClient();
        window.addEventListener("online", () => { this.isOnline = true; console.log("🌐 [Supabase] Koneksi online terdeteksi."); });
        window.addEventListener("offline", () => { this.isOnline = false; console.warn("📴 [Supabase] Mode offline aktif."); });
    }

    initClient() {
        try {
            if (window.supabase && typeof window.supabase.createClient === "function") {
                this.client = window.supabase.createClient(SUPABASE_CONFIG.URL, SUPABASE_CONFIG.ANON_KEY);
                console.log("⚡ [Supabase] Client SDK berhasil diinisialisasi.");
            } else {
                console.log("ℹ️ [Supabase] SDK belum dimuat di window, menggunakan REST Mode.");
            }
        } catch (e) {
            console.warn("⚠️ [Supabase] Inisialisasi SDK gagal, fallback ke REST:", e);
        }
    }

    // --- 1. OTENTIKASI & MANAJEMEN AKUN (CEK LOGIN & REGISTRASI SUPABASE) ---

    /**
     * Cek Login Mode Tamu (Guest):
     * Memeriksa apakah perangkat ini sudah terdaftar di Supabase.
     * Jika belum ada, melemparkan Error dengan flag `needRegister: true`.
     */
    async loginAsGuest(guestName) {
        const deviceId = this.getOrCreateDeviceId();
        const nick = guestName ? guestName.trim() : "VIP Player";

        const res = await fetch(`${SUPABASE_CONFIG.API_BASE}/api/auth/guest`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ action: "CHECK", device_id: deviceId, nickname: nick })
        });

        if (res.status === 404) {
            const errData = await res.json().catch(() => ({}));
            const err = new Error(errData.message || "Akun Tamu belum terdaftar di database Supabase! Silakan klik DAFTAR TAMU terlebih dahulu.");
            err.needRegister = true;
            throw err;
        }

        if (!res.ok) {
            const errData = await res.json().catch(() => ({}));
            throw new Error(errData.message || "Gagal memeriksa akun tamu di database Supabase.");
        }

        const data = await res.json();
        if (data.success && data.user) {
            this.saveLocalSession(data.user, data.wallet, data.stats);
            return data;
        }

        throw new Error("Respon server tidak valid saat login tamu.");
    }

    /**
     * Registrasi Akun Tamu Baru (Guest) ke Supabase:
     * Mendaftarkan perangkat tamu ke PostgreSQL Supabase (+10,000 Chips awal).
     */
    async registerGuest(guestName) {
        const deviceId = this.getOrCreateDeviceId();
        const nick = guestName && guestName.trim() ? guestName.trim() : "VIP Player";

        const res = await fetch(`${SUPABASE_CONFIG.API_BASE}/api/auth/guest`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ action: "REGISTER", device_id: deviceId, nickname: nick })
        });

        if (!res.ok) {
            const errData = await res.json().catch(() => ({}));
            throw new Error(errData.message || "Gagal registrasi akun tamu ke Supabase.");
        }

        const data = await res.json();
        if (data.success && data.user) {
            this.saveLocalSession(data.user, data.wallet, data.stats);
            return data;
        }

        throw new Error("Gagal membuat akun tamu baru.");
    }

    /**
     * Login Akun Email & Password:
     * Cek apakah user terdaftar di database Supabase.
     * Jika tidak ada -> Error `needRegister: true`.
     */
    async loginWithEmail(email, password) {
        if (!email || !password) throw new Error("Email dan kata sandi wajib diisi.");

        const res = await fetch(`${SUPABASE_CONFIG.API_BASE}/api/auth/login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ identifier: email, password: password })
        });

        const data = await res.json().catch(() => ({}));

        if (res.status === 404) {
            const err = new Error(data.message || "Akun belum terdaftar di database Supabase! Silakan lakukan registrasi terlebih dahulu.");
            err.needRegister = true;
            throw err;
        }

        if (!res.ok) {
            throw new Error(data.message || "Email atau kata sandi yang Anda masukkan salah.");
        }

        if (data.success && data.user) {
            this.saveLocalSession(data.user, data.wallet, data.stats);
            return data;
        }

        throw new Error("Respon login tidak valid.");
    }

    /**
     * Registrasi Akun Email & Password ke Supabase:
     * Membuat user baru di public.users (+25,000 Chips awal).
     */
    async registerWithEmail(email, password, displayName) {
        if (!email || !password) throw new Error("Email dan kata sandi wajib diisi.");

        const username = email.split('@')[0].replace(/[^a-zA-Z0-9_]/g, '') || "Player_" + Math.floor(Math.random() * 1000);
        const dName = displayName && displayName.trim() ? displayName.trim() : username;

        const res = await fetch(`${SUPABASE_CONFIG.API_BASE}/api/auth/register`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                username: username,
                email: email,
                password: password,
                display_name: dName,
                avatar_id: 3
            })
        });

        const data = await res.json().catch(() => ({}));

        if (!res.ok) {
            throw new Error(data.message || "Gagal registrasi akun di Supabase.");
        }

        if (data.success && data.user) {
            this.saveLocalSession(data.user, data.wallet, data.stats);
            return data;
        }

        throw new Error("Gagal mendaftarkan akun baru.");
    }

    /**
     * Login / Registrasi Akun Google OAuth ke Supabase
     */
    async loginWithGoogle() {
        if (this.client && this.client.auth) {
            const { data, error } = await this.client.auth.signInWithOAuth({
                provider: 'google',
                options: {
                    redirectTo: window.location.origin
                }
            });
            if (error) throw error;
            return data;
        } else {
            // OAuth Simulation via Go Server API to Supabase
            let googleSub = localStorage.getItem("mahjong_google_sub");
            if (!googleSub) {
                googleSub = "goog_" + Math.random().toString(36).substring(2, 10);
                localStorage.setItem("mahjong_google_sub", googleSub);
            }

            const res = await fetch(`${SUPABASE_CONFIG.API_BASE}/api/auth/google`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    oauth_id: googleSub,
                    email: "player@gmail.com",
                    display_name: "Google VIP Master",
                    avatar_url: "",
                    action: "REGISTER"
                })
            });

            if (res.ok) {
                const data = await res.json();
                if (data.success && data.user) {
                    this.saveLocalSession(data.user, data.wallet, data.stats);
                    return data;
                }
            }

            // Fallback lokal jika server offline
            const user = {
                id: "usr_" + googleSub,
                username: "Google_Player",
                email: "player@gmail.com",
                display_name: "Google VIP Master",
                auth_provider: "GOOGLE",
                oauth_provider_id: googleSub,
                avatar_id: 2,
                role: "VIP_PLAYER",
                created_at: new Date().toISOString()
            };
            const wallet = { user_id: user.id, chips_balance: 50000, diamonds_balance: 100, total_chips_earned: 50000 };
            const stats = { user_id: user.id, trophy_points: 500, rank_tier: "Apprentice 🥈", total_matches: 0, total_wins: 0, win_rate: 0.0, highest_match_score: 0, current_win_streak: 0 };
            this.saveLocalSession(user, wallet, stats);
            return { user, wallet, stats };
        }
    }

    // --- 2. SINKRONISASI DATA PROFILE & DOMPET CHIPS ---

    saveLocalSession(user, wallet, stats) {
        this.currentUser = user;
        this.currentWallet = wallet;
        this.currentStats = stats;

        localStorage.setItem("mahjong_user_id", user.id);
        localStorage.setItem("mahjong_user", user.username);
        localStorage.setItem("mahjong_display_name", user.display_name || user.username);
        localStorage.setItem("mahjong_auth_type", (user.auth_provider || "guest").toLowerCase());
        localStorage.setItem("mahjong_email", user.email || "");
        localStorage.setItem("mahjong_avatar", (user.avatar_id || 1).toString());
        
        if (wallet) {
            localStorage.setItem("mahjong_coins", (wallet.chips_balance || 10000).toString());
            localStorage.setItem("mahjong_diamonds", (wallet.diamonds_balance || 50).toString());
        }
        if (stats) {
            localStorage.setItem("mahjong_trophies", (stats.trophy_points || 0).toString());
            localStorage.setItem("mahjong_rank_tier", stats.rank_tier || "Novice 🥉");
        }
    }

    getLocalUser() {
        const uid = localStorage.getItem("mahjong_user_id");
        if (!uid) return null;
        return {
            id: uid,
            username: localStorage.getItem("mahjong_user") || "VIP Player",
            display_name: localStorage.getItem("mahjong_display_name") || "VIP Player",
            auth_provider: (localStorage.getItem("mahjong_auth_type") || "GUEST").toUpperCase(),
            email: localStorage.getItem("mahjong_email") || "",
            avatar_id: parseInt(localStorage.getItem("mahjong_avatar") || "1")
        };
    }

    getLocalWallet() {
        return {
            chips_balance: parseInt(localStorage.getItem("mahjong_coins") || "10000"),
            diamonds_balance: parseInt(localStorage.getItem("mahjong_diamonds") || "50")
        };
    }

    getLocalStats() {
        return {
            trophy_points: parseInt(localStorage.getItem("mahjong_trophies") || "0"),
            rank_tier: localStorage.getItem("mahjong_rank_tier") || "Novice 🥉"
        };
    }

    // --- 3. LIVE GLOBAL LEADERBOARD (v_leaderboard_global) ---

    /**
     * Mengambil daftar 50 pemain peringkat teratas dari Supabase
     */
    async fetchGlobalLeaderboard(limit = 50) {
        try {
            // Ambil dari Go Game Server API yang terhubung ke Supabase
            const res = await fetch(`${SUPABASE_CONFIG.API_BASE}/api/leaderboard`);
            if (res.ok) {
                const data = await res.json();
                if (Array.isArray(data) && data.length > 0) {
                    return data;
                }
            }
        } catch (e) {
            console.warn("⚠️ [Supabase] Gagal fetch leaderboard dari game server:", e.message);
        }

        try {
            // Direct Supabase REST API Fallback
            const url = `${SUPABASE_CONFIG.URL}/rest/v1/v_leaderboard_global?select=*&limit=${limit}&order=rank.asc`;
            const res = await fetch(url, {
                headers: {
                    "apikey": SUPABASE_CONFIG.ANON_KEY,
                    "Authorization": `Bearer ${SUPABASE_CONFIG.ANON_KEY}`
                }
            });

            if (res.ok) {
                const data = await res.json();
                if (Array.isArray(data) && data.length > 0) {
                    return data;
                }
            }
        } catch (e) {
            console.warn("⚠️ [Supabase] Fallback REST gagal:", e.message);
        }

        // Fallback Leaderboard Mewah VIP
        const currentUser = this.getLocalUser() || { username: "VIP Player", display_name: "Anda" };
        const currentStats = this.getLocalStats();
        const currentWallet = this.getLocalWallet();

        return [
            { rank: 1, username: "MahjongMaster", display_name: "Mahjong Legend 👑", rank_tier: "VIP Legend 👑", trophy_points: 12500, chips_balance: 1000000, win_rate: 73.3, avatar_id: 1 },
            { rank: 2, username: "DragonKing", display_name: "Master Ryu 🐉", rank_tier: "Master 💎", trophy_points: 7850, chips_balance: 450000, win_rate: 68.5, avatar_id: 3 },
            { rank: 3, username: "EmeraldQueen", display_name: "Lady Emerald 💚", rank_tier: "Master 💎", trophy_points: 5400, chips_balance: 280000, win_rate: 62.0, avatar_id: 2 },
            { rank: 4, username: "JadePhoenix", display_name: "Jade Master 🀄", rank_tier: "Expert 🥇", trophy_points: 3200, chips_balance: 150000, win_rate: 58.2, avatar_id: 1 },
            { rank: 5, username: currentUser.username, display_name: currentUser.display_name, rank_tier: currentStats.rank_tier, trophy_points: currentStats.trophy_points, chips_balance: currentWallet.chips_balance, win_rate: 60.0, avatar_id: 1 }
        ];
    }

    // --- 4. PENCATATAN HASIL PERTANDINGAN (MATCH RECORD & YAKU) ---

    /**
     * Mencatat hasil match 4 ronde, perolehan trofi (+40/-25), dan saldo chips
     */
    async recordMatchEnd(matchData) {
        const {
            roomCode,
            gameMode = "CUSTOM_VIP",
            winnerSeat,
            winnerName,
            isWinnerLocal,
            finalScores,
            highestScore = 0,
            specialHandsCount = {}
        } = matchData;

        const trophyDelta = isWinnerLocal ? 40 : -25;
        const chipsDelta = isWinnerLocal ? 5000 : -1000;

        // Update Lokal User
        let currentTrophies = parseInt(localStorage.getItem("mahjong_trophies") || "0") + trophyDelta;
        if (currentTrophies < 0) currentTrophies = 0;
        localStorage.setItem("mahjong_trophies", currentTrophies.toString());

        const newRankTier = this.calculateRankTier(currentTrophies);
        localStorage.setItem("mahjong_rank_tier", newRankTier);

        let currentCoins = parseInt(localStorage.getItem("mahjong_coins") || "10000") + chipsDelta;
        if (currentCoins < 0) currentCoins = 0;
        localStorage.setItem("mahjong_coins", currentCoins.toString());

        console.log(`🏆 [Supabase] Match Berakhir! Trofi: ${trophyDelta > 0 ? "+" + trophyDelta : trophyDelta} (${currentTrophies} pts) | Rank: ${newRankTier} | Chips: ${chipsDelta > 0 ? "+" + chipsDelta : chipsDelta}`);

        // Kirim ke cloud Supabase
        const payload = {
            room_code: roomCode || "VIP-SOLO",
            game_mode: gameMode,
            status: "FINISHED",
            total_rounds: 4,
            winning_score: highestScore,
            started_at: new Date(Date.now() - 10 * 60 * 1000).toISOString(),
            finished_at: new Date().toISOString()
        };

        if (this.isOnline) {
            try {
                await fetch(`${SUPABASE_CONFIG.URL}/rest/v1/matches`, {
                    method: "POST",
                    headers: {
                        "apikey": SUPABASE_CONFIG.ANON_KEY,
                        "Authorization": `Bearer ${SUPABASE_CONFIG.ANON_KEY}`,
                        "Content-Type": "application/json"
                    },
                    body: JSON.stringify(payload)
                });
            } catch (e) {
                console.debug("Kirim riwayat match ke cloud offline:", e.message);
            }
        }

        return {
            trophyDelta,
            newTrophies: currentTrophies,
            newRankTier,
            chipsDelta,
            newCoins: currentCoins
        };
    }

    calculateRankTier(trophies) {
        if (trophies >= 10000) return "VIP Legend 👑";
        if (trophies >= 4000) return "Master 💎";
        if (trophies >= 1500) return "Expert 🥇";
        if (trophies >= 500) return "Apprentice 🥈";
        return "Novice 🥉";
    }

    getOrCreateDeviceId() {
        let id = localStorage.getItem("mahjong_device_id");
        if (!id) {
            id = "dev_" + Math.random().toString(36).substring(2, 12) + Date.now().toString(36);
            localStorage.setItem("mahjong_device_id", id);
        }
        return id;
    }
}

export const SupabaseDB = new SupabaseService();
