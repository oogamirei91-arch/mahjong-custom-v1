/**
 * main.js - Master Controller Aplikasi Mahjong 3D VIP
 * Mengatur Ruang Tunggu VIP, Alokasi Dinamis (1P+3B, 2P+2B, 3P+1B, 4P Real-Time),
 * Sinkronisasi Dual-Layer (BroadcastChannel & WebSocket), dan 3D Visualizer.
 */

import { TileAtlasGenerator } from './tileAtlas.js';
import { Mahjong3DVisualizer } from './visualizer3d.js';
import { MahjongGameLogic } from './gameLogic.js';
import { AIManager } from './aiManager.js';
import { NetworkManager } from './networkManager.js';
import { Sound } from './audio.js';
import { SEATS, SEAT_NAMES, sortTiles, generateFullDeck } from './config.js';
import { SupabaseDB } from './supabaseClient.js';

class MahjongApp {
    constructor() {
        this.visualizer = null;
        this.gameLogic = null;
        this.aiManager = null;
        this.networkManager = null;

        // Player & Auth State (Synchronized with Supabase)
        this.authType = localStorage.getItem("mahjong_auth_type") || "guest";
        this.username = localStorage.getItem("mahjong_user") || "VIP Player";
        this.displayName = localStorage.getItem("mahjong_display_name") || this.username;
        this.userEmail = localStorage.getItem("mahjong_email") || "";
        this.avatarIdx = parseInt(localStorage.getItem("mahjong_avatar") || "1");
        this.coins = parseInt(localStorage.getItem("mahjong_coins") || "10000");
        this.trophies = parseInt(localStorage.getItem("mahjong_trophies") || "0");
        this.rankTier = localStorage.getItem("mahjong_rank_tier") || "Novice 🥉";
        this.currentGameMode = "AI";

        // VIP Room & Seat State
        this.currentRoomCode = "";
        this.isHost = true;
        this.localSeat = SEATS.SOUTH; // 0=South, 1=East, 2=North, 3=West
        this.roomSlots = [
            { seat: SEATS.SOUTH, name: this.username, isHuman: true, isHost: true, avatar: this.getAvatarForAuth(), userId: null },
            { seat: SEATS.EAST,  name: "Bot AI: Kenji", isHuman: false, isHost: false, avatar: "🤖", userId: null },
            { seat: SEATS.NORTH, name: "Bot AI: Mei", isHuman: false, isHost: false, avatar: "🤖", userId: null },
            { seat: SEATS.WEST,  name: "Bot AI: Dragon", isHuman: false, isHost: false, avatar: "🤖", userId: null }
        ];

        // Turn & Timer State
        this.turnTimerInterval = null;
        this.promptTimerTimeout = null;
        this.remainingSeconds = 15;
        this.isLocalPlayerTurn = false;
        this.activeClaimPrompt = null;
        this.botNames = ["Kenji", "Mei", "Dragon"];

        this.init();
    }

    getAvatarForAuth() {
        if (this.authType === "google") return "🔴";
        if (this.authType === "email") return "💎";
        return "👑";
    }

    init() {
        // Inisialisasi Atlas & Visualizer
        const atlasGen = new TileAtlasGenerator(2048, 2048);
        const atlasCanvas = atlasGen.generateAtlasCanvas();
        const jadeCanvas = atlasGen.generateJadeBackCanvas();

        const container = document.getElementById("canvas-container");
        this.visualizer = new Mahjong3DVisualizer(container, atlasCanvas, jadeCanvas);

        this.gameLogic = new MahjongGameLogic();
        this.aiManager = new AIManager(this.gameLogic);
        this.networkManager = new NetworkManager();

        this.visualizer.onTileSelectedCallback = (tile, idx) => this.handleTileSelected(tile, idx);
        this.visualizer.onTileDiscardCallback = (tile, idx) => this.handleTileDiscard(tile, idx);

        this.gameLogic.onStateChangeCallback = (state) => this.onGameStateChanged(state);
        this.gameLogic.onRoundEndCallback = (result) => this.onRoundEnded(result);

        this.bindUIEvents();
        this.bindNetworkEvents();
        this.updateProfileUI();

        // Cek jika ada session OAuth Google setelah redirect Supabase
        SupabaseDB.checkOAuthRedirectSession().then(res => {
            if (res && res.user) {
                this.username = res.user.username;
                this.displayName = res.user.display_name;
                this.authType = "google";
                this.userEmail = res.user.email;
                this.coins = res.wallet.chips_balance;
                this.trophies = res.stats.trophy_points;
                this.rankTier = res.stats.rank_tier;
                this.updateProfileUI();
                this.showStatusToast(`✅ Masuk sebagai ${this.displayName} (Google VIP)`);
                this.switchScreen("screen-lobby");
            }
        });

        // Cek jika ada URL parameter ?room=XXXXXX untuk auto join
        this.checkUrlRoomParam();

        console.log("🀄 [Mahjong 3D VIP] Master App siap!");
    }

    checkUrlRoomParam() {
        try {
            const urlParams = new URLSearchParams(window.location.search);
            const roomCode = urlParams.get('room');
            if (roomCode && roomCode.trim().length >= 4) {
                const guestName = "Guest " + Math.floor(100 + Math.random() * 900);
                this.username = guestName;
                this.authType = "guest";
                setTimeout(() => {
                    this.joinCustomVIPRoom(roomCode.trim().toUpperCase());
                }, 300);
            }
        } catch (e) {
            console.warn("Gagal membaca room param:", e);
        }
    }

    bindUIEvents() {
        // 1. Tab Switching pada Layar Login
        const tabs = [
            { btn: "tab-btn-guest", content: "tab-content-guest" },
            { btn: "tab-btn-google", content: "tab-content-google" },
            { btn: "tab-btn-email", content: "tab-content-email" }
        ];

        const showAuthStatus = (msg, isError = true) => {
            const el = document.getElementById("login-status-msg");
            if (!el) return;
            el.style.display = "block";
            el.style.background = isError ? "rgba(255, 77, 77, 0.18)" : "rgba(0, 255, 136, 0.18)";
            el.style.border = isError ? "1px solid #ff4d4d" : "1px solid var(--jade-accent)";
            el.style.color = isError ? "#ff8080" : "#a3ffcc";
            el.innerHTML = msg;
        };

        const hideAuthStatus = () => {
            const el = document.getElementById("login-status-msg");
            if (el) el.style.display = "none";
        };

        tabs.forEach(t => {
            document.getElementById(t.btn)?.addEventListener("click", () => {
                Sound.playButtonPop();
                hideAuthStatus();
                tabs.forEach(item => {
                    document.getElementById(item.btn)?.classList.remove("active");
                    document.getElementById(item.content)?.classList.remove("active");
                });
                document.getElementById(t.btn)?.classList.add("active");
                document.getElementById(t.content)?.classList.add("active");
            });
        });

        // 2. Login Method: Guest / Tamu (Check User di Supabase)
        document.getElementById("btn-login-guest")?.addEventListener("click", async () => {
            Sound.playButtonPop();
            hideAuthStatus();
            const nameInput = document.getElementById("input-guest-name");
            const nick = (nameInput && nameInput.value.trim()) ? nameInput.value.trim() : "VIP Player";
            
            this.showStatusToast("⏳ Memeriksa akun tamu di database Supabase...");
            try {
                const res = await SupabaseDB.loginAsGuest(nick);
                this.username = res.user.username;
                this.displayName = res.user.display_name;
                this.authType = "guest";
                this.userEmail = "";
                this.coins = res.wallet.chips_balance;
                this.trophies = res.stats.trophy_points;
                this.rankTier = res.stats.rank_tier;

                this.updateProfileUI();
                this.showStatusToast(`✅ Selamat datang kembali, ${this.displayName}! (Tamu Terdaftar)`);
                this.switchScreen("screen-lobby");
            } catch (err) {
                console.warn("Guest Login Check:", err.message);
                const msg = err.needRegister 
                    ? "⚠️ Akun Tamu belum terdaftar di Supabase! Silakan klik tombol <b>DAFTAR TAMU</b> terlebih dahulu." 
                    : `⚠️ Gagal login: ${err.message}`;
                showAuthStatus(msg, true);
                this.showStatusToast(err.needRegister ? "⚠️ Akun Tamu belum terdaftar! Silakan klik DAFTAR TAMU." : err.message);
            }
        });

        // 2b. Register Method: Guest / Tamu (Registrasi Akun Tamu Baru ke Supabase)
        document.getElementById("btn-register-guest")?.addEventListener("click", async () => {
            Sound.playButtonPop();
            hideAuthStatus();
            const nameInput = document.getElementById("input-guest-name");
            const nick = (nameInput && nameInput.value.trim()) ? nameInput.value.trim() : "VIP Player";

            this.showStatusToast("⏳ Mendaftarkan akun tamu baru ke Supabase...");
            try {
                const res = await SupabaseDB.registerGuest(nick);
                this.username = res.user.username;
                this.displayName = res.user.display_name;
                this.authType = "guest";
                this.userEmail = "";
                this.coins = res.wallet.chips_balance;
                this.trophies = res.stats.trophy_points;
                this.rankTier = res.stats.rank_tier;

                this.updateProfileUI();
                this.showStatusToast(`🎉 Akun Tamu terdaftar di Supabase! Saldo: 10,000 Chips`);
                this.switchScreen("screen-lobby");
            } catch (err) {
                console.error("Guest Register Error:", err);
                showAuthStatus(`⚠️ Gagal registrasi tamu: ${err.message}`, true);
                this.showStatusToast(`⚠️ Gagal mendaftar: ${err.message}`);
            }
        });

        // 3. Login Method: Google Sign-In (Supabase OAuth & VIP Fast-Auth)
        document.getElementById("btn-login-google")?.addEventListener("click", async () => {
            Sound.playButtonPop();
            hideAuthStatus();
            const emailInput = document.getElementById("input-google-email");
            const customEmail = (emailInput && emailInput.value.trim()) ? emailInput.value.trim() : "";

            this.showStatusToast("⏳ Mengautentikasi dengan Google...");
            try {
                const res = await SupabaseDB.loginWithGoogle(customEmail);
                this.username = res.user.username;
                this.displayName = res.user.display_name;
                this.authType = "google";
                this.userEmail = res.user.email;
                this.coins = res.wallet.chips_balance;
                this.trophies = res.stats.trophy_points;
                this.rankTier = res.stats.rank_tier;

                this.updateProfileUI();
                this.showStatusToast(`✅ Masuk sebagai ${this.displayName} (Google VIP)`);
                this.switchScreen("screen-lobby");
            } catch (err) {
                showAuthStatus(`⚠️ Gagal login Google: ${err.message}`, true);
                this.showStatusToast(`⚠️ Gagal login Google: ${err.message}`);
            }
        });

        // 4. Login Method: Email & Password (Cek User Terdaftar)
        document.getElementById("btn-login-email")?.addEventListener("click", async () => {
            Sound.playButtonPop();
            hideAuthStatus();
            const emailInput = document.getElementById("input-email-address");
            const passInput = document.getElementById("input-email-password");
            const emailVal = emailInput ? emailInput.value.trim() : "";
            const passVal = passInput ? passInput.value.trim() : "";

            if (!emailVal || !emailVal.includes("@")) {
                showAuthStatus("⚠️ Harap masukkan alamat email yang valid!", true);
                this.showStatusToast("⚠️ Harap masukkan alamat email yang valid!");
                return;
            }
            if (!passVal) {
                showAuthStatus("⚠️ Harap masukkan kata sandi!", true);
                this.showStatusToast("⚠️ Harap masukkan kata sandi!");
                return;
            }

            this.showStatusToast("⏳ Memeriksa akun di database Supabase...");
            try {
                const res = await SupabaseDB.loginWithEmail(emailVal, passVal);
                this.username = res.user.username;
                this.displayName = res.user.display_name;
                this.authType = "email";
                this.userEmail = emailVal;
                this.coins = res.wallet.chips_balance;
                this.trophies = res.stats.trophy_points;
                this.rankTier = res.stats.rank_tier;

                this.updateProfileUI();
                this.showStatusToast(`✅ Berhasil login: ${emailVal}`);
                this.switchScreen("screen-lobby");
            } catch (err) {
                console.warn("Email Login Check:", err.message);
                const msg = err.needRegister 
                    ? "⚠️ Akun belum terdaftar di database Supabase! Silakan klik tombol <b>DAFTAR</b> terlebih dahulu." 
                    : `⚠️ ${err.message}`;
                showAuthStatus(msg, true);
                this.showStatusToast(err.needRegister ? "⚠️ Akun belum terdaftar! Silakan klik DAFTAR." : err.message);
            }
        });

        // 4b. Register Method: Email & Password (Registrasi ke Supabase)
        document.getElementById("btn-register-email")?.addEventListener("click", async () => {
            Sound.playButtonPop();
            hideAuthStatus();
            const emailInput = document.getElementById("input-email-address");
            const passInput = document.getElementById("input-email-password");
            const emailVal = emailInput ? emailInput.value.trim() : "";
            const passVal = passInput ? passInput.value.trim() : "";

            if (!emailVal || !emailVal.includes("@")) {
                showAuthStatus("⚠️ Masukkan email yang valid untuk mendaftar!", true);
                this.showStatusToast("⚠️ Masukkan email yang valid untuk mendaftar!");
                return;
            }
            if (!passVal || passVal.length < 6) {
                showAuthStatus("⚠️ Kata sandi minimal 6 karakter!", true);
                this.showStatusToast("⚠️ Kata sandi minimal 6 karakter!");
                return;
            }

            this.showStatusToast("⏳ Mendaftarkan akun ke Supabase...");
            try {
                const res = await SupabaseDB.registerWithEmail(emailVal, passVal);
                this.username = res.user.username;
                this.displayName = res.user.display_name;
                this.authType = "email";
                this.userEmail = emailVal;
                this.coins = res.wallet.chips_balance;
                this.trophies = res.stats.trophy_points;
                this.rankTier = res.stats.rank_tier;

                this.updateProfileUI();
                this.showStatusToast(`🎉 Akun terdaftar di Supabase! Saldo: 25,000 Chips`);
                this.switchScreen("screen-lobby");
            } catch (err) {
                console.error("Email Register Error:", err);
                showAuthStatus(`⚠️ Gagal mendaftar: ${err.message}`, true);
                this.showStatusToast(`⚠️ Gagal mendaftar: ${err.message}`);
            }
        });

        // 5. Leaderboard Button
        document.getElementById("btn-lobby-leaderboard")?.addEventListener("click", () => {
            Sound.playButtonPop();
            this.openModal("modal-leaderboard");
            this.loadLeaderboardFromSupabase();
        });

        // 6. Logout / Ganti Akun
        document.getElementById("btn-settings-logout")?.addEventListener("click", () => {
            Sound.playButtonPop();
            this.closeModal("modal-settings");
            this.showStatusToast("Telah keluar dari akun. Silakan pilih login kembali.");
            this.switchScreen("screen-landing");
        });

        // Mode Cards
        document.getElementById("card-mode-ai")?.addEventListener("click", () => {
            Sound.playButtonPop();
            this.startSinglePlayerGame("AI");
        });

        document.getElementById("card-mode-tournament")?.addEventListener("click", () => {
            Sound.playButtonPop();
            this.startSinglePlayerGame("TOURNAMENT");
        });

        document.getElementById("card-mode-quick")?.addEventListener("click", () => {
            Sound.playButtonPop();
            this.startOnlineMatchmaking();
        });

        document.getElementById("card-mode-custom")?.addEventListener("click", () => {
            Sound.playButtonPop();
            this.openModal("modal-custom-room");
        });

        // Custom Room Modal Buttons
        document.getElementById("btn-create-room")?.addEventListener("click", () => {
            Sound.playButtonPop();
            this.createCustomVIPRoom();
        });

        document.getElementById("btn-join-room")?.addEventListener("click", () => {
            Sound.playButtonPop();
            const codeInput = document.getElementById("input-room-code");
            if (codeInput && codeInput.value.trim()) {
                this.joinCustomVIPRoom(codeInput.value.trim().toUpperCase());
            }
        });

        // VIP Waiting Room Controls
        document.getElementById("btn-copy-room-code")?.addEventListener("click", () => {
            Sound.playButtonPop();
            this.copyRoomCodeToClipboard();
        });

        document.getElementById("btn-copy-room-link")?.addEventListener("click", () => {
            Sound.playButtonPop();
            this.copyRoomLinkToClipboard();
        });

        document.getElementById("btn-vip-invite")?.addEventListener("click", () => {
            Sound.playButtonPop();
            this.copyRoomLinkToClipboard();
        });

        document.getElementById("btn-leave-vip-room")?.addEventListener("click", () => {
            Sound.playButtonPop();
            this.leaveVIPRoom();
        });

        document.getElementById("btn-start-vip-game")?.addEventListener("click", () => {
            if (!this.isHost) return;
            Sound.playButtonPop();
            this.startVIPGame();
        });

        // Slot Toggle Buttons (1, 2, 3)
        [1, 2, 3].forEach(seatIdx => {
            const btnToggle = document.getElementById(`btn-slot-${seatIdx}-toggle`);
            if (btnToggle) {
                btnToggle.addEventListener("click", () => {
                    Sound.playButtonPop();
                    this.toggleSlotHumanBot(seatIdx);
                });
            }
        });

        // In-Game Action Bar
        document.getElementById("btn-action-chow")?.addEventListener("click", () => this.handlePlayerClaim("CHOW"));
        document.getElementById("btn-action-pong")?.addEventListener("click", () => this.handlePlayerClaim("PONG"));
        document.getElementById("btn-action-kong")?.addEventListener("click", () => this.handlePlayerClaim("KONG"));
        document.getElementById("btn-action-win")?.addEventListener("click", () => this.handlePlayerClaim("WIN"));
        document.getElementById("btn-action-pass")?.addEventListener("click", () => this.handlePlayerClaim("PASS"));

        // Sort Hand Floating Button
        document.getElementById("btn-hud-sort")?.addEventListener("click", () => {
            Sound.playButtonPop();
            this.gameLogic.hands[this.localSeat] = sortTiles(this.gameLogic.hands[this.localSeat]);
            this.visualizer.renderLocalPlayerHand(this.gameLogic.hands[this.localSeat]);
        });

        // Modals & Navigation
        document.getElementById("btn-hud-rules")?.addEventListener("click", () => {
            Sound.playButtonPop();
            this.openModal("modal-rules");
        });
        document.getElementById("btn-hud-settings")?.addEventListener("click", () => {
            Sound.playButtonPop();
            this.openModal("modal-settings");
        });
        document.getElementById("btn-hud-quit")?.addEventListener("click", () => {
            Sound.playButtonPop();
            this.quitToLobby();
        });
        document.getElementById("btn-play-again")?.addEventListener("click", () => {
            Sound.playButtonPop();
            this.closeModal("modal-winner");
            if (this.currentGameMode === "CUSTOM") {
                this.switchScreen("screen-vip-room");
            } else {
                this.startVIPGame();
            }
        });

        document.querySelectorAll(".btn-modal-close").forEach(btn => {
            btn.addEventListener("click", (e) => {
                Sound.playButtonPop();
                const modal = e.target.closest(".modal-overlay");
                if (modal) modal.classList.remove("active");
            });
        });

        document.getElementById("toggle-sound")?.addEventListener("change", (e) => {
            Sound.isMuted = !e.target.checked;
        });
    }

    bindNetworkEvents() {
        // Player bergabung ke room
        this.networkManager.onPlayerJoin = (payload) => {
            console.log("Pemain lain terhubung:", payload);
            if (this.isHost) {
                // Cari slot bot pertama yang kosong untuk pemain baru
                let assignedSeat = payload.seat;
                if (!assignedSeat || this.roomSlots[assignedSeat]?.isHuman) {
                    assignedSeat = this.roomSlots.findIndex(s => !s.isHuman);
                }

                if (assignedSeat !== -1) {
                    this.roomSlots[assignedSeat] = {
                        seat: assignedSeat,
                        name: payload.username || `Pemain ${assignedSeat + 1}`,
                        isHuman: true,
                        isHost: false,
                        avatar: "👤",
                        userId: payload.userId
                    };

                    // Broadcast state terbaru ke semua pemain
                    this.networkManager.broadcast("ROOM_STATE_SYNC", {
                        roomSlots: this.roomSlots,
                        roomCode: this.currentRoomCode
                    });

                    this.updateVIPRoomUI();
                    this.showStatusToast(`🟢 ${payload.username} bergabung ke Kursi ${assignedSeat + 1}!`);
                }
            }
        };

        // Sinkronisasi status ruangan dari Host
        this.networkManager.onRoomStateSync = (payload) => {
            if (payload.roomSlots) {
                this.roomSlots = payload.roomSlots;
                // Cek kursi lokal kita
                const mySlot = this.roomSlots.find(s => s.userId === this.networkManager.userId);
                if (mySlot) {
                    this.localSeat = mySlot.seat;
                }
                this.updateVIPRoomUI();
            }
        };

        // Pemain keluar
        this.networkManager.onPlayerLeave = (payload) => {
            const leftSlot = this.roomSlots.find(s => s.userId === payload.userId);
            if (leftSlot) {
                const defaultNames = ["Kenji", "Mei", "Dragon"];
                leftSlot.isHuman = false;
                leftSlot.name = `Bot AI: ${defaultNames[leftSlot.seat - 1] || 'Bot'}`;
                leftSlot.avatar = "🤖";
                leftSlot.userId = null;
                this.updateVIPRoomUI();
                this.showStatusToast(`🔴 ${payload.username || 'Pemain'} telah keluar`);

                if (this.isHost) {
                    this.networkManager.broadcast("ROOM_STATE_SYNC", { roomSlots: this.roomSlots });
                }
            }
        };

        // Host memulai permainan
        this.networkManager.onGameStart = (payload) => {
            this.switchScreen("screen-game");
            this.visualizer.onWindowResize();

            // Set deck dan kursi yang disinkronkan dari Host
            this.gameLogic.dealerSeat = payload.dealerSeat || SEATS.SOUTH;
            this.gameLogic.currentTurnSeat = this.gameLogic.dealerSeat;
            this.gameLogic.deck = payload.deck || generateFullDeck();
            this.gameLogic.hands = payload.hands || [[], [], [], []];
            this.gameLogic.discards = [[], [], [], []];
            this.gameLogic.melds = [[], [], [], []];
            this.gameLogic.bonusTiles = [[], [], [], []];
            this.gameLogic.isRoundActive = true;

            this.updateHUDWallCount();
            this.visualizer.renderLocalPlayerHand(this.gameLogic.hands[this.localSeat]);
            this.visualizer.renderOpponentHands(this.getOpponentHandCounts());
            this.visualizer.renderDiscardRiver(this.gameLogic.discards);
            this.visualizer.renderMelds(this.gameLogic.melds);

            this.showStatusToast("🀄 Permainan Dimulai!");
            this.startTurnSequence(this.gameLogic.currentTurnSeat);
        };

        // Discard sinkronisasi
        this.networkManager.onTileDiscarded = (payload) => {
            if (payload.seat !== this.localSeat) {
                const discarded = this.gameLogic.playerDiscard(payload.seat, payload.tileId);
                Sound.playTileDiscard();
                this.visualizer.renderDiscardRiver(this.gameLogic.discards);
                this.visualizer.renderOpponentHands(this.getOpponentHandCounts());
                this.visualizer.renderMelds(this.gameLogic.melds);

                if (discarded) {
                    this.processDiscardPrompts(discarded, payload.seat);
                }
            }
        };

        // Claim action sinkronisasi (Pong/Chow/Kong/Win)
        this.networkManager.onActionClaimed = (payload) => {
            if (payload.seat !== this.localSeat) {
                this.executeClaimAction(payload.seat, payload.action, payload.targetTile, payload.sequence, false);
            }
        };
    }

    getOpponentHandCounts() {
        const counts = {};
        for (let s = 0; s < 4; s++) {
            if (s !== this.localSeat) {
                counts[s] = this.gameLogic.hands[s]?.length || 13;
            }
        }
        return counts;
    }

    saveAuthState() {
        localStorage.setItem("mahjong_user", this.username);
        localStorage.setItem("mahjong_auth_type", this.authType);
        localStorage.setItem("mahjong_email", this.userEmail);
        localStorage.setItem("mahjong_coins", this.coins.toString());
        this.roomSlots[0].name = this.username;
        this.roomSlots[0].avatar = this.getAvatarForAuth();
        this.updateProfileUI();
    }

    updateProfileUI() {
        document.querySelectorAll(".profile-name").forEach(el => el.textContent = this.username);
        document.querySelectorAll(".profile-coins").forEach(el => el.textContent = this.coins.toLocaleString());

        // Header Avatar
        const headerAvatar = document.getElementById("header-avatar");
        if (headerAvatar) {
            headerAvatar.textContent = this.getAvatarForAuth();
        }

        // Header Auth Badge
        const authBadge = document.getElementById("header-auth-badge");
        if (authBadge) {
            if (this.authType === "google") {
                authBadge.className = "auth-tag tag-google";
                authBadge.textContent = "🔴 Google";
            } else if (this.authType === "email") {
                authBadge.className = "auth-tag tag-email";
                authBadge.textContent = "📧 Email";
            } else {
                authBadge.className = "auth-tag tag-guest";
                authBadge.textContent = "👤 Tamu";
            }
        }

        // Settings Modal Info
        const settingsAuthType = document.getElementById("settings-auth-type");
        const settingsUserDetail = document.getElementById("settings-user-detail");
        if (settingsAuthType) {
            if (this.authType === "google") {
                settingsAuthType.textContent = "Akun Google";
                settingsAuthType.style.color = "#8ab4f8";
                settingsAuthType.style.borderColor = "#4285F4";
            } else if (this.authType === "email") {
                settingsAuthType.textContent = "Akun Email";
                settingsAuthType.style.color = "var(--jade-accent)";
                settingsAuthType.style.borderColor = "var(--jade-accent)";
            } else {
                settingsAuthType.textContent = "Tamu (Guest)";
                settingsAuthType.style.color = "#ffffff";
                settingsAuthType.style.borderColor = "rgba(255,255,255,0.4)";
            }
        }
        if (settingsUserDetail) {
            if (this.authType === "email" && this.userEmail) {
                settingsUserDetail.textContent = `Email: ${this.userEmail} | Nama: ${this.username}`;
            } else if (this.authType === "google") {
                settingsUserDetail.textContent = `Google ID: ${this.username} (${this.userEmail || 'Terhubung'})`;
            } else {
                settingsUserDetail.textContent = `Nickname: ${this.username} (Akun Tamu Lokal)`;
            }
        }
    }

    showStatusToast(message, durationMs = 2500) {
        let toast = document.getElementById("hud-status-toast");
        if (!toast) {
            toast = document.createElement("div");
            toast.id = "hud-status-toast";
            toast.style.position = "absolute";
            toast.style.top = "70px";
            toast.style.left = "50%";
            toast.style.transform = "translateX(-50%)";
            toast.style.background = "rgba(10, 36, 26, 0.94)";
            toast.style.border = "1px solid var(--gold-bright)";
            toast.style.color = "var(--gold-bright)";
            toast.style.padding = "10px 24px";
            toast.style.borderRadius = "30px";
            toast.style.fontWeight = "700";
            toast.style.fontSize = "1rem";
            toast.style.zIndex = "40";
            toast.style.boxShadow = "0 8px 25px rgba(0,0,0,0.8), 0 0 15px rgba(212,175,55,0.4)";
            toast.style.backdropFilter = "blur(16px)";
            toast.style.transition = "opacity 0.3s ease";
            document.getElementById("app-root")?.appendChild(toast);
        }
        toast.textContent = message;
        toast.style.opacity = "1";
        toast.style.display = "block";

        if (this._toastTimer) clearTimeout(this._toastTimer);
        this._toastTimer = setTimeout(() => {
            toast.style.opacity = "0";
            setTimeout(() => { toast.style.display = "none"; }, 300);
        }, durationMs);
    }

    switchScreen(screenId) {
        document.querySelectorAll(".screen-view").forEach(s => s.classList.remove("active"));
        const target = document.getElementById(screenId);
        if (target) target.classList.add("active");
    }

    openModal(modalId) {
        document.getElementById(modalId)?.classList.add("active");
    }

    closeModal(modalId) {
        document.getElementById(modalId)?.classList.remove("active");
    }

    // =========================================================
    // VIP WAITING ROOM LOGIC (1P+3B, 2P+2B, 3P+1B, 4P)
    // =========================================================
    createCustomVIPRoom() {
        this.closeModal("modal-custom-room");
        this.isHost = true;
        this.localSeat = SEATS.SOUTH;
        this.currentRoomCode = Math.random().toString(36).substring(2, 8).toUpperCase();
        this.currentGameMode = "CUSTOM";

        // Setup 4 Slots Awal (Host + 3 Bots)
        this.roomSlots = [
            { seat: SEATS.SOUTH, name: this.username, isHuman: true, isHost: true, avatar: "👑", userId: this.networkManager.userId },
            { seat: SEATS.EAST,  name: "Bot AI: Kenji", isHuman: false, isHost: false, avatar: "🤖", userId: null },
            { seat: SEATS.NORTH, name: "Bot AI: Mei", isHuman: false, isHost: false, avatar: "🤖", userId: null },
            { seat: SEATS.WEST,  name: "Bot AI: Dragon", isHuman: false, isHost: false, avatar: "🤖", userId: null }
        ];

        // Inisialisasi network channel
        this.networkManager.initRoomChannel(this.currentRoomCode, this.username);
        this.networkManager.connect("ws://localhost:8080/ws", this.username);

        this.updateVIPRoomUI();
        this.switchScreen("screen-vip-room");
        this.showStatusToast(`Ruang VIP Dibuat! Kode: #${this.currentRoomCode}`);
    }

    joinCustomVIPRoom(roomCode) {
        this.closeModal("modal-custom-room");
        this.isHost = false;
        this.currentRoomCode = roomCode;
        this.currentGameMode = "CUSTOM";
        this.localSeat = SEATS.EAST; // Default kursi tamu pertama

        // Inisialisasi network channel
        this.networkManager.initRoomChannel(this.currentRoomCode, this.username);
        this.networkManager.connect("ws://localhost:8080/ws", this.username);

        // Siarkan event join ke Host
        setTimeout(() => {
            this.networkManager.broadcast("PLAYER_JOIN", {
                username: this.username,
                userId: this.networkManager.userId,
                seat: this.localSeat
            });
        }, 200);

        this.updateVIPRoomUI();
        this.switchScreen("screen-vip-room");
        this.showStatusToast(`Bergabung ke Ruang VIP #${this.currentRoomCode}`);
    }

    toggleSlotHumanBot(slotIdx) {
        if (!this.isHost) {
            this.showStatusToast("Hanya Host yang dapat mengubah konfigurasi kursi");
            return;
        }

        const slot = this.roomSlots[slotIdx];
        if (!slot) return;

        const botNames = ["Kenji", "Mei", "Dragon"];
        const botName = botNames[slotIdx - 1] || "Bot";

        if (slot.isHuman) {
            // Ubah dari Human -> Bot
            slot.isHuman = false;
            slot.name = `Bot AI: ${botName}`;
            slot.avatar = "🤖";
            slot.userId = null;
        } else {
            // Ubah dari Bot -> Human Simulasi
            slot.isHuman = true;
            slot.name = `Teman VIP ${slotIdx + 1}`;
            slot.avatar = "👤";
            slot.userId = `sim_user_${slotIdx}`;
        }

        this.updateVIPRoomUI();

        // Broadcast perubahan ke semua tab
        this.networkManager.broadcast("ROOM_STATE_SYNC", {
            roomSlots: this.roomSlots,
            roomCode: this.currentRoomCode
        });
    }

    updateVIPRoomUI() {
        const codeEl = document.getElementById("vip-display-room-code");
        if (codeEl) codeEl.textContent = `#${this.currentRoomCode}`;

        // Hitung jumlah pemain manusia dan bot
        let humanCount = 0;
        let botCount = 0;

        this.roomSlots.forEach((slot, idx) => {
            if (slot.isHuman) humanCount++;
            else botCount++;

            const nameEl = document.getElementById(`vip-slot-${idx}-name`);
            const avatarEl = document.getElementById(`vip-slot-${idx}-avatar`);
            const statusEl = document.getElementById(`vip-slot-${idx}-status`);

            if (nameEl) nameEl.textContent = slot.name;
            if (avatarEl) avatarEl.textContent = slot.avatar;
            if (statusEl) {
                if (slot.isHost) {
                    statusEl.className = "slot-status badge-host";
                    statusEl.textContent = "🟢 HOST (SIAP)";
                } else if (slot.isHuman) {
                    statusEl.className = "slot-status badge-player";
                    statusEl.textContent = "🟢 PEMAIN (SIAP)";
                } else {
                    statusEl.className = "slot-status badge-bot";
                    statusEl.textContent = "🤖 BOT OTOMATIS";
                }
            }
        });

        // Update Summary Banner
        const summaryText = document.getElementById("vip-player-count-text");
        const ruleNote = document.getElementById("vip-rule-note");
        if (summaryText) {
            if (humanCount === 1) {
                summaryText.textContent = "1 Pemain Asli + 3 Bot AI";
                if (ruleNote) ruleNote.textContent = "(3 Kursi diisi Bot Kenji, Mei, Dragon)";
            } else if (humanCount === 2) {
                summaryText.textContent = "2 Pemain Real-Time + 2 Bot AI";
                if (ruleNote) ruleNote.textContent = "(2 Kursi diisi Bot Mei, Dragon)";
            } else if (humanCount === 3) {
                summaryText.textContent = "3 Pemain Real-Time + 1 Bot AI";
                if (ruleNote) ruleNote.textContent = "(1 Kursi diisi Bot Dragon)";
            } else {
                summaryText.textContent = "4 Pemain Penuh Real-Time!";
                if (ruleNote) ruleNote.textContent = "(100% Real-Time Multiplayer Turn-Based)";
            }
        }

        // Update Start Button status
        const startBtn = document.getElementById("btn-start-vip-game");
        const startText = document.getElementById("btn-start-text");
        if (startBtn) {
            if (this.isHost) {
                startBtn.classList.remove("waiting-host");
                if (startText) startText.textContent = "MULAI PERMAINAN";
            } else {
                startBtn.classList.add("waiting-host");
                if (startText) startText.textContent = "Menunggu Host...";
            }
        }
    }

    copyRoomCodeToClipboard() {
        if (!this.currentRoomCode) return;
        navigator.clipboard.writeText(this.currentRoomCode).then(() => {
            this.showStatusToast(`📋 Kode #${this.currentRoomCode} disalin!`);
        }).catch(() => {
            this.showStatusToast(`Kode Ruangan: #${this.currentRoomCode}`);
        });
    }

    copyRoomLinkToClipboard() {
        if (!this.currentRoomCode) return;
        const url = `${window.location.origin}${window.location.pathname}?room=${this.currentRoomCode}`;
        navigator.clipboard.writeText(url).then(() => {
            this.showStatusToast("🔗 Link Ruang VIP disalin ke clipboard!");
        }).catch(() => {
            this.showStatusToast(`Link: ${url}`);
        });
    }

    leaveVIPRoom() {
        this.networkManager.disconnect();
        this.switchScreen("screen-lobby");
    }

    startVIPGame() {
        this.switchScreen("screen-game");
        this.visualizer.onWindowResize();

        // Host inisialisasi deck acak 144 ubin
        this.gameLogic.startNewGame(SEATS.SOUTH);

        // Broadcast payload inisialisasi ke pemain lain
        this.networkManager.broadcast("GAME_START", {
            dealerSeat: SEATS.SOUTH,
            deck: this.gameLogic.deck,
            hands: this.gameLogic.hands,
            roomSlots: this.roomSlots
        });

        this.updateHUDWallCount();
        this.visualizer.renderLocalPlayerHand(this.gameLogic.hands[this.localSeat]);
        this.visualizer.renderOpponentHands(this.getOpponentHandCounts());
        this.visualizer.renderDiscardRiver(this.gameLogic.discards);
        this.visualizer.renderMelds(this.gameLogic.melds);

        this.startTurnSequence(this.gameLogic.currentTurnSeat);
    }

    startSinglePlayerGame(mode = "AI") {
        this.currentGameMode = mode;
        this.isHost = true;
        this.localSeat = SEATS.SOUTH;
        this.roomSlots = [
            { seat: SEATS.SOUTH, name: this.username, isHuman: true, isHost: true, avatar: "👑" },
            { seat: SEATS.EAST,  name: "Bot AI: Kenji", isHuman: false, isHost: false, avatar: "🤖" },
            { seat: SEATS.NORTH, name: "Bot AI: Mei", isHuman: false, isHost: false, avatar: "🤖" },
            { seat: SEATS.WEST,  name: "Bot AI: Dragon", isHuman: false, isHost: false, avatar: "🤖" }
        ];

        this.switchScreen("screen-game");
        this.visualizer.onWindowResize();
        this.gameLogic.startNewGame(SEATS.SOUTH);
        this.updateHUDWallCount();
        this.visualizer.renderLocalPlayerHand(this.gameLogic.hands[SEATS.SOUTH]);
        this.visualizer.renderOpponentHands(this.getOpponentHandCounts());
        this.visualizer.renderDiscardRiver(this.gameLogic.discards);
        this.visualizer.renderMelds(this.gameLogic.melds);

        this.startTurnSequence(this.gameLogic.currentTurnSeat);
    }

    startTurnSequence(seat) {
        this.clearIntervalTimer();
        this.clearPromptTimeout();
        this.remainingSeconds = 15;
        this.visualizer.setTurnState(seat, this.remainingSeconds);

        const slot = this.roomSlots[seat] || { isHuman: false, name: `Kursi ${seat + 1}` };

        if (seat === this.localSeat) {
            // Giliran Pemain Lokal
            this.isLocalPlayerTurn = true;
            this.hideClaimActionBar();
            this.showStatusToast(`Giliran Anda (${slot.name}): Pilih ubin untuk dibuang`);
            this.startTurnTimer(() => {
                this.autoDiscardLocalPlayer();
            });
        } else if (slot.isHuman) {
            // Giliran Pemain Manusia Lain di jaringan
            this.isLocalPlayerTurn = false;
            this.hideClaimActionBar();
            this.showStatusToast(`Giliran: ${slot.name} (15 Detik)`);
            this.startTurnTimer();
        } else {
            // Giliran Bot AI
            this.isLocalPlayerTurn = false;
            this.hideClaimActionBar();

            if (this.isHost) {
                // Host yang mengeksekusi AI
                this.startTurnTimer(() => {
                    this.executeBotTurn(seat);
                });

                const thinkDelay = 1000 + Math.random() * 600;
                setTimeout(() => {
                    if (!this.gameLogic.isRoundActive || this.gameLogic.currentTurnSeat !== seat) return;
                    this.executeBotTurn(seat);
                }, thinkDelay);
            } else {
                // Non-host hanya menyalakan timer animasi
                this.startTurnTimer();
            }
        }
    }

    startTurnTimer(onExpired = null) {
        this.clearIntervalTimer();
        this.turnTimerInterval = setInterval(() => {
            this.remainingSeconds--;
            this.visualizer.setTurnState(this.gameLogic.currentTurnSeat, this.remainingSeconds);

            const timerBar = document.getElementById("hud-timer-bar");
            if (timerBar) {
                const percent = (this.remainingSeconds / 15) * 100;
                timerBar.style.width = `${percent}%`;
                if (this.remainingSeconds <= 5) {
                    timerBar.classList.add("urgent");
                } else {
                    timerBar.classList.remove("urgent");
                }
            }

            if (this.remainingSeconds <= 0) {
                this.clearIntervalTimer();
                if (onExpired) onExpired();
            }
        }, 1000);
    }

    clearIntervalTimer() {
        if (this.turnTimerInterval) {
            clearInterval(this.turnTimerInterval);
            this.turnTimerInterval = null;
        }
    }

    clearPromptTimeout() {
        if (this.promptTimerTimeout) {
            clearTimeout(this.promptTimerTimeout);
            this.promptTimerTimeout = null;
        }
    }

    executeBotTurn(seat) {
        if (!this.gameLogic.isRoundActive) return;

        const bestTile = this.aiManager.decideDiscard(seat);
        if (!bestTile) {
            this.advanceToNextTurn();
            return;
        }

        const discarded = this.gameLogic.playerDiscard(seat, bestTile.id);
        if (!discarded) {
            this.advanceToNextTurn();
            return;
        }

        Sound.playTileDiscard();

        // Broadcast aksi discard bot ke seluruh tab
        this.networkManager.broadcast("TILE_DISCARDED", {
            seat,
            tileId: bestTile.id
        });

        this.visualizer.renderDiscardRiver(this.gameLogic.discards);
        this.visualizer.renderOpponentHands(this.getOpponentHandCounts());
        this.visualizer.renderMelds(this.gameLogic.melds);

        this.processDiscardPrompts(discarded, seat);
    }

    handleTileSelected(tile, idx) {
        // Interaksi pemilihan ubin
    }

    handleTileDiscard(tile, idx) {
        if (!this.isLocalPlayerTurn) return;

        this.isLocalPlayerTurn = false;
        this.clearIntervalTimer();

        const discarded = this.gameLogic.playerDiscard(this.localSeat, tile.id);
        if (!discarded) return;

        Sound.playTileDiscard();

        // Broadcast discard ke pemain lain
        this.networkManager.broadcast("TILE_DISCARDED", {
            seat: this.localSeat,
            tileId: tile.id
        });

        this.visualizer.renderLocalPlayerHand(this.gameLogic.hands[this.localSeat]);
        this.visualizer.renderDiscardRiver(this.gameLogic.discards);
        this.visualizer.renderMelds(this.gameLogic.melds);

        this.processDiscardPrompts(discarded, this.localSeat);
    }

    autoDiscardLocalPlayer() {
        if (!this.isLocalPlayerTurn) return;
        const hand = this.gameLogic.hands[this.localSeat];
        if (!hand || hand.length === 0) return;

        const lastTile = hand[hand.length - 1];
        this.handleTileDiscard(lastTile, hand.length - 1);
    }

    processDiscardPrompts(discardedTile, discarderSeat) {
        const prompts = this.gameLogic.checkActionPrompts(discardedTile, discarderSeat);

        // Cek apakah pemain lokal mendapat opsi Pong/Chow/Kong/Win
        if (prompts[this.localSeat]) {
            this.showClaimActionBar(prompts[this.localSeat], discardedTile);

            this.clearPromptTimeout();
            this.promptTimerTimeout = setTimeout(() => {
                this.handlePlayerClaim("PASS");
            }, 10000);
            return;
        }

        // Cek jika Bot AI yang mendapat kesempatan klaim (hanya Host yang mengeksekusi)
        if (this.isHost) {
            for (let s = 0; s < 4; s++) {
                if (s === discarderSeat || s === this.localSeat) continue;
                const slot = this.roomSlots[s];
                if (!slot?.isHuman && prompts[s]) {
                    const decision = this.aiManager.decideClaimAction(s, prompts[s]);
                    if (decision.action !== "PASS") {
                        this.executeClaimAction(s, decision.action, discardedTile, decision.sequence, true);
                        return;
                    }
                }
            }
        }

        this.advanceToNextTurn();
    }

    advanceToNextTurn() {
        const nextSeat = this.gameLogic.nextTurn();
        const drawn = this.gameLogic.playerDraw(nextSeat);
        if (!drawn) return;

        this.updateHUDWallCount();

        if (nextSeat === this.localSeat) {
            this.visualizer.renderLocalPlayerHand(this.gameLogic.hands[this.localSeat]);
            Sound.playTileClick();

            if (this.gameLogic.checkWinningHand(this.gameLogic.hands[this.localSeat])) {
                this.showTsumoWinButton();
            }
        } else {
            this.visualizer.renderOpponentHands(this.getOpponentHandCounts());
        }

        this.visualizer.renderMelds(this.gameLogic.melds);
        this.startTurnSequence(nextSeat);
    }

    showClaimActionBar(prompt, targetTile) {
        this.activeClaimPrompt = { prompt, targetTile };
        const bar = document.getElementById("hud-action-bar");
        if (!bar) return;

        bar.classList.add("active");

        const btnChow = document.getElementById("btn-action-chow");
        const btnPong = document.getElementById("btn-action-pong");
        const btnKong = document.getElementById("btn-action-kong");
        const btnWin = document.getElementById("btn-action-win");

        if (btnChow) btnChow.style.display = prompt.canChow ? "inline-flex" : "none";
        if (btnPong) btnPong.style.display = prompt.canPong ? "inline-flex" : "none";
        if (btnKong) btnKong.style.display = prompt.canKong ? "inline-flex" : "none";
        if (btnWin) btnWin.style.display = prompt.canWin ? "inline-flex" : "none";
    }

    showTsumoWinButton() {
        const bar = document.getElementById("hud-action-bar");
        if (!bar) return;
        bar.classList.add("active");
        const btnWin = document.getElementById("btn-action-win");
        if (btnWin) {
            btnWin.style.display = "inline-flex";
            btnWin.textContent = "TSUMO WIN! (自摸)";
        }
    }

    hideClaimActionBar() {
        const bar = document.getElementById("hud-action-bar");
        if (bar) bar.classList.remove("active");
        this.activeClaimPrompt = null;
        this.clearPromptTimeout();
    }

    handlePlayerClaim(action) {
        Sound.playButtonPop();
        const claimData = this.activeClaimPrompt;
        this.hideClaimActionBar();

        if (action === "PASS" || !claimData) {
            this.advanceToNextTurn();
            return;
        }

        const { targetTile, prompt } = claimData;

        let sequence = null;
        if (action === "CHOW" && prompt.chowOptions && prompt.chowOptions.length > 0) {
            sequence = prompt.chowOptions[0];
        }

        this.executeClaimAction(this.localSeat, action, targetTile, sequence, true);
    }

    executeClaimAction(seat, action, targetTile, sequence = null, broadcast = true) {
        if (broadcast) {
            this.networkManager.broadcast("ACTION_CLAIMED", {
                seat,
                action,
                targetTile,
                sequence
            });
        }

        if (action === "WIN") {
            Sound.playVictoryFanfare();
            const isSelfDraw = !targetTile;
            this.gameLogic.endRoundWin(seat, targetTile, isSelfDraw);
            return;
        }

        let meldName = "PONG";
        if (action === "PONG") {
            this.gameLogic.executePong(seat, targetTile);
            meldName = "PONG (碰)";
        } else if (action === "CHOW") {
            this.gameLogic.executeChow(seat, targetTile, sequence);
            meldName = "CHOW (吃)";
        } else if (action === "KONG") {
            this.gameLogic.executeKong(seat, targetTile);
            meldName = "KONG (槓)";
        }

        this.visualizer.renderLocalPlayerHand(this.gameLogic.hands[this.localSeat]);
        this.visualizer.renderOpponentHands(this.getOpponentHandCounts());
        this.visualizer.renderDiscardRiver(this.gameLogic.discards);
        this.visualizer.renderMelds(this.gameLogic.melds);

        const claimantName = this.roomSlots[seat]?.name || `Kursi ${seat + 1}`;
        if (seat === this.localSeat) {
            this.showStatusToast(`${meldName} BERHASIL! Silakan buang 1 ubin.`);
        } else {
            this.showStatusToast(`${claimantName} melakukan ${meldName}!`);
        }

        this.startTurnSequence(seat);
    }

    onGameStateChanged(state) {
        this.visualizer.renderLocalPlayerHand(state.hands[this.localSeat]);
        this.visualizer.renderOpponentHands(this.getOpponentHandCounts());
        this.visualizer.renderDiscardRiver(state.discards);
        this.visualizer.renderMelds(state.melds);
        this.updateHUDWallCount();
    }

    updateHUDWallCount() {
        const wallEl = document.getElementById("hud-wall-count");
        if (wallEl) {
            wallEl.textContent = `${this.gameLogic.deck.length}`;
        }
    }

    async onRoundEnded(result) {
        this.clearIntervalTimer();
        this.clearPromptTimeout();

        const modal = document.getElementById("modal-winner");
        const titleEl = document.getElementById("winner-title");
        const nameEl = document.getElementById("winner-name");
        const scoreEl = document.getElementById("winner-score");
        const breakdownEl = document.getElementById("winner-breakdown");

        if (result.result === "WIN") {
            const isMe = result.winnerSeat === this.localSeat;
            const winnerName = this.roomSlots[result.winnerSeat]?.name || `Pemain ${result.winnerSeat + 1}`;
            const totalScore = result.scoreData ? result.scoreData.totalScore : 100;
            const breakdown = result.scoreData ? result.scoreData.breakdown : [];

            // Sinkronkan hasil pertandingan ke Supabase Cloud
            const cloudSync = await SupabaseDB.recordMatchEnd({
                roomCode: this.currentRoomCode || "VIP-SOLO",
                gameMode: this.currentGameMode,
                winnerSeat: result.winnerSeat,
                winnerName: winnerName,
                isWinnerLocal: isMe,
                finalScores: { [this.localSeat]: isMe ? totalScore : 0 },
                highestScore: totalScore
            });

            this.trophies = cloudSync.newTrophies;
            this.rankTier = cloudSync.newRankTier;
            this.coins = cloudSync.newCoins;

            if (isMe) {
                Sound.playVictoryFanfare();
                if (titleEl) titleEl.textContent = "🏆 VICTORY! HU (和牌)";
                if (nameEl) nameEl.textContent = `Selamat, ${this.displayName || this.username}! Anda Memenangkan Ronde Ini!`;
                if (scoreEl) scoreEl.textContent = `+${totalScore} Poin • Trofi +40 (${this.trophies} Pts • ${this.rankTier})`;
            } else {
                if (titleEl) titleEl.textContent = "RONDE SELESAI";
                if (nameEl) nameEl.textContent = `${winnerName} Memenangkan Ronde Ini!`;
                if (scoreEl) scoreEl.textContent = `+${totalScore} Poin untuk ${winnerName} • Trofi -25 (${this.trophies} Pts)`;
            }

            if (breakdownEl) {
                breakdownEl.innerHTML = `
                    <div style="background: rgba(0,0,0,0.4); border: 1px solid rgba(212,175,55,0.35); border-radius: 12px; padding: 12px 16px; margin: 14px 0; text-align: left;">
                        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px;">
                            <div style="font-weight: 700; color: var(--gold-bright); font-size: 0.9rem; letter-spacing: 0.5px;">Rincian Skor (Rule Book v1.0):</div>
                            <span class="sync-status-indicator" style="font-size: 0.7rem;"><span class="sync-dot"></span> Supabase Saved</span>
                        </div>
                        ${breakdown.map(item => `
                            <div style="display: flex; justify-content: space-between; font-size: 0.85rem; color: rgba(255,255,255,0.9); padding: 3px 0; border-bottom: 1px dashed rgba(255,255,255,0.08);">
                                <span>&bull; ${item.name}</span>
                                <strong style="color: var(--jade-accent); font-family: monospace; font-size: 0.95rem;">+${item.points}</strong>
                            </div>
                        `).join('')}
                    </div>
                `;
            }
        } else {
            if (titleEl) titleEl.textContent = "DRAW / RYUUKYOKU (流局)";
            if (nameEl) nameEl.textContent = result.reason || "Tembok Ubin Habis (Wall Exhaustion)";
            if (scoreEl) scoreEl.textContent = "+0 Poin";
            if (breakdownEl) breakdownEl.innerHTML = "";
        }

        this.updateProfileUI();
        this.openModal("modal-winner");
    }

    updateProfileUI() {
        const nameEl = document.querySelector(".profile-name");
        if (nameEl) nameEl.textContent = this.displayName || this.username;

        const avatarEl = document.getElementById("header-avatar");
        if (avatarEl) avatarEl.textContent = this.getAvatarForAuth();

        const authBadge = document.getElementById("header-auth-badge");
        if (authBadge) {
            authBadge.className = `auth-tag tag-${this.authType.toLowerCase()}`;
            authBadge.textContent = this.authType === "google" ? "🔴 Google" : (this.authType === "email" ? "💎 Email" : "👤 Tamu");
        }

        const rankBadge = document.getElementById("header-rank-tier");
        if (rankBadge) rankBadge.textContent = this.rankTier;

        const trophyBadge = document.getElementById("header-trophies");
        if (trophyBadge) trophyBadge.textContent = `${this.trophies.toLocaleString()} Pts`;

        const coinsEls = document.querySelectorAll(".profile-coins");
        coinsEls.forEach(el => el.textContent = this.coins.toLocaleString());

        // Update settings modal
        const settingType = document.getElementById("settings-auth-type");
        if (settingType) settingType.textContent = this.authType.toUpperCase();

        const settingDetail = document.getElementById("settings-user-detail");
        if (settingDetail) {
            settingDetail.textContent = `Pemain: ${this.displayName || this.username} | Trofi: ${this.trophies} Pts | Gelar: ${this.rankTier}`;
        }
    }

    saveAuthState() {
        localStorage.setItem("mahjong_user", this.username);
        localStorage.setItem("mahjong_display_name", this.displayName || this.username);
        localStorage.setItem("mahjong_auth_type", this.authType);
        localStorage.setItem("mahjong_email", this.userEmail || "");
        localStorage.setItem("mahjong_coins", this.coins.toString());
        localStorage.setItem("mahjong_trophies", this.trophies.toString());
        localStorage.setItem("mahjong_rank_tier", this.rankTier);
        this.updateProfileUI();
    }

    async loadLeaderboardFromSupabase() {
        const rowsContainer = document.getElementById("leaderboard-rows");
        if (!rowsContainer) return;

        rowsContainer.innerHTML = `
            <tr>
                <td colspan="6" style="text-align: center; padding: 25px; color: var(--gold-bright);">
                    ⏳ Memuat data realtime dari Supabase Cloud...
                </td>
            </tr>
        `;

        try {
            const data = await SupabaseDB.fetchGlobalLeaderboard(50);
            if (!data || data.length === 0) {
                rowsContainer.innerHTML = `
                    <tr><td colspan="6" style="text-align: center; padding: 20px; color: rgba(255,255,255,0.5);">Belum ada data pertandingan.</td></tr>
                `;
                return;
            }

            rowsContainer.innerHTML = data.map((entry, idx) => {
                const rankNum = entry.rank || (idx + 1);
                let rankBadgeHtml = `<span style="font-weight: 700; color: rgba(255,255,255,0.7);">${rankNum}</span>`;
                let topClass = "";

                if (rankNum === 1) {
                    rankBadgeHtml = `<span class="rank-pill gold">🥇 1</span>`;
                    topClass = "rank-top-1";
                } else if (rankNum === 2) {
                    rankBadgeHtml = `<span class="rank-pill silver">🥈 2</span>`;
                } else if (rankNum === 3) {
                    rankBadgeHtml = `<span class="rank-pill bronze">🥉 3</span>`;
                }

                const isMe = (entry.username === this.username || entry.display_name === this.displayName);
                const meHighlight = isMe ? "background: rgba(0, 255, 136, 0.15); font-weight: bold;" : "";

                return `
                    <tr class="${topClass}" style="${meHighlight}">
                        <td>${rankBadgeHtml}</td>
                        <td>
                            <div style="display: flex; align-items: center; gap: 8px;">
                                <span style="font-size: 1.2rem;">${entry.avatar_id === 2 ? '🔴' : (entry.avatar_id === 3 ? '💎' : '👑')}</span>
                                <div>
                                    <div style="font-weight: 700; color: #fff;">${entry.display_name || entry.username}</div>
                                    <div style="font-size: 0.75rem; color: rgba(255,255,255,0.5);">@${entry.username}</div>
                                </div>
                            </div>
                        </td>
                        <td><span class="rank-tier-tag">${entry.rank_tier || 'Novice 🥉'}</span></td>
                        <td style="text-align: right; color: var(--gold-bright); font-weight: 800; font-family: monospace;">${(entry.trophy_points || 0).toLocaleString()}</td>
                        <td style="text-align: right; color: var(--jade-accent); font-family: monospace;">${(entry.chips_balance || 0).toLocaleString()}</td>
                        <td style="text-align: right; color: rgba(255,255,255,0.8);">${(entry.win_rate || 0).toFixed(1)}%</td>
                    </tr>
                `;
            }).join('');
        } catch (err) {
            rowsContainer.innerHTML = `
                <tr><td colspan="6" style="text-align: center; padding: 20px; color: #ff6666;">⚠️ Gagal memuat leaderboard: ${err.message}</td></tr>
            `;
        }
    }

    startOnlineMatchmaking() {
        this.showStatusToast("Menghubungi Server WebSocket Online...");
        this.networkManager.connect("ws://localhost:8080/ws", this.username);
        this.networkManager.onError = () => {
            this.showStatusToast("Server offline, beralih ke Mode AI.");
            this.startSinglePlayerGame("AI");
        };
        this.networkManager.onConnected = () => {
            this.startSinglePlayerGame("QUICK");
        };
    }

    quitToLobby() {
        this.clearIntervalTimer();
        this.clearPromptTimeout();
        this.gameLogic.isRoundActive = false;
        this.networkManager.disconnect();
        this.switchScreen("screen-lobby");
    }
}

window.addEventListener("DOMContentLoaded", () => {
    window.mahjongApp = new MahjongApp();
});
