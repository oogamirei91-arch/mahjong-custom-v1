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
        this.selectedAvatarId = 1;
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

    getAvatarForAuth(avatarId = null) {
        const id = avatarId || this.avatarIdx || 1;
        const avatars = {
            1: "👑",
            2: "🀄",
            3: "💎",
            4: "🐉",
            5: "🌸",
            6: "⚡"
        };
        if (avatars[id]) return avatars[id];
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
        // 1. Tab Switching pada Layar Landing
        const tabs = [
            { btn: "tab-btn-account", content: "tab-content-account" },
            { btn: "tab-btn-guest", content: "tab-content-guest" },
            { btn: "tab-btn-google", content: "tab-content-google" }
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

        // 1b. Sub-tab Switching: Masuk (Login) vs Daftar Baru (Register)
        const subtabLogin = document.getElementById("subtab-btn-login");
        const subtabRegister = document.getElementById("subtab-btn-register");
        const formLogin = document.getElementById("form-account-login");
        const formRegister = document.getElementById("form-account-register");

        subtabLogin?.addEventListener("click", () => {
            Sound.playButtonPop();
            hideAuthStatus();
            subtabLogin.classList.add("active");
            subtabRegister?.classList.remove("active");
            formLogin?.classList.add("active");
            formRegister?.classList.remove("active");
        });

        subtabRegister?.addEventListener("click", () => {
            Sound.playButtonPop();
            hideAuthStatus();
            subtabRegister.classList.add("active");
            subtabLogin?.classList.remove("active");
            formRegister?.classList.add("active");
            formLogin?.classList.remove("active");
        });

        // 1c. Avatar Selection in Registration Form
        document.querySelectorAll(".avatar-choice-btn").forEach(btn => {
            btn.addEventListener("click", () => {
                Sound.playButtonPop();
                document.querySelectorAll(".avatar-choice-btn").forEach(b => b.classList.remove("active"));
                btn.classList.add("active");
                this.selectedAvatarId = parseInt(btn.dataset.avatar) || 1;
            });
        });

        // Landscape Force Button Handler
        document.getElementById("btn-force-landscape")?.addEventListener("click", () => {
            this.requestLandscapeLock();
            try {
                const docEl = document.documentElement;
                if (docEl.requestFullscreen) {
                    docEl.requestFullscreen().catch(() => {});
                } else if (docEl.webkitRequestFullscreen) {
                    docEl.webkitRequestFullscreen();
                }
            } catch (e) {}
            const overlay = document.getElementById("rotate-prompt-overlay");
            if (overlay) overlay.style.display = "none";
        });

        // 2. Form Method: MASUK (Login Akun Pemain)
        document.getElementById("btn-login-account")?.addEventListener("click", async () => {
            Sound.playButtonPop();
            hideAuthStatus();
            const identInput = document.getElementById("input-login-identifier");
            const passInput = document.getElementById("input-login-password");
            const identVal = identInput ? identInput.value.trim() : "";
            const passVal = passInput ? passInput.value.trim() : "";

            if (!identVal) {
                showAuthStatus("⚠️ Harap masukkan username atau email!", true);
                this.showStatusToast("⚠️ Harap masukkan username atau email!");
                return;
            }
            if (!passVal) {
                showAuthStatus("⚠️ Harap masukkan kata sandi!", true);
                this.showStatusToast("⚠️ Harap masukkan kata sandi!");
                return;
            }

            this.showStatusToast("⏳ Memeriksa akun di database Supabase...");
            try {
                const res = await SupabaseDB.loginWithEmail(identVal, passVal);
                this.username = res.user.username;
                this.displayName = res.user.display_name || res.user.username;
                this.authType = "email";
                this.userEmail = res.user.email || "";
                this.avatarIdx = res.user.avatar_id || 1;
                this.coins = res.wallet ? res.wallet.chips_balance : 25000;
                this.trophies = res.stats ? res.stats.trophy_points : 0;
                this.rankTier = res.stats ? res.stats.rank_tier : "Novice 🥉";

                this.updateProfileUI();
                this.showStatusToast(`✅ Berhasil login: ${this.displayName}!`);
                this.switchScreen("screen-lobby");
            } catch (err) {
                console.warn("Account Login Check:", err.message);
                const msg = err.needRegister 
                    ? "⚠️ Akun belum terdaftar di database Supabase! Silakan klik tab <b>DAFTAR BARU</b> di atas." 
                    : `⚠️ ${err.message}`;
                showAuthStatus(msg, true);
                this.showStatusToast(err.needRegister ? "⚠️ Akun belum terdaftar! Silakan klik DAFTAR BARU." : err.message);
            }
        });

        // 2b. Form Method: DAFTAR AKUN BARU (Registrasi Akun Pemain)
        document.getElementById("btn-register-account")?.addEventListener("click", async () => {
            Sound.playButtonPop();
            hideAuthStatus();
            const unameInput = document.getElementById("input-reg-username");
            const dnameInput = document.getElementById("input-reg-displayname");
            const emailInput = document.getElementById("input-reg-email");
            const passInput = document.getElementById("input-reg-password");

            const unameVal = unameInput ? unameInput.value.trim() : "";
            const dnameVal = dnameInput ? dnameInput.value.trim() : "";
            const emailVal = emailInput ? emailInput.value.trim() : "";
            const passVal = passInput ? passInput.value.trim() : "";
            const avatarId = this.selectedAvatarId || 1;

            if (!unameVal || unameVal.length < 3) {
                showAuthStatus("⚠️ Username minimal 3 karakter!", true);
                this.showStatusToast("⚠️ Username minimal 3 karakter!");
                return;
            }
            if (!passVal || passVal.length < 6) {
                showAuthStatus("⚠️ Kata sandi minimal 6 karakter!", true);
                this.showStatusToast("⚠️ Kata sandi minimal 6 karakter!");
                return;
            }

            this.showStatusToast("⏳ Mendaftarkan akun baru ke Supabase...");
            try {
                const res = await SupabaseDB.registerWithEmail(unameVal, emailVal, passVal, dnameVal || unameVal, avatarId);
                this.username = res.user.username;
                this.displayName = res.user.display_name || res.user.username;
                this.authType = "email";
                this.userEmail = res.user.email || emailVal;
                this.avatarIdx = avatarId;
                this.coins = res.wallet ? res.wallet.chips_balance : 25000;
                this.trophies = res.stats ? res.stats.trophy_points : 0;
                this.rankTier = res.stats ? res.stats.rank_tier : "Novice 🥉";

                this.updateProfileUI();
                this.showStatusToast(`🎉 Akun berhasil didaftarkan! Bonus 25,000 Chips VIP`);
                this.switchScreen("screen-lobby");
            } catch (err) {
                console.error("Account Register Error:", err);
                showAuthStatus(`⚠️ Gagal mendaftar: ${err.message}`, true);
                this.showStatusToast(`⚠️ Gagal mendaftar: ${err.message}`);
            }
        });

        // 3. Login Method: Guest / Tamu Cepat (Check User di Supabase)
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
                this.avatarIdx = res.user.avatar_id || 1;
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

        // 3b. Register Method: Guest / Tamu
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
                this.avatarIdx = res.user.avatar_id || 1;
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

        // 4. Login Method: Google Sign-In (Supabase OAuth & VIP Fast-Auth)
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
                this.avatarIdx = res.user.avatar_id || 2;
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

        // 5. Lobby Profile Badge & History Buttons (Open Personal Profile & Match History)
        document.getElementById("lobby-profile-badge")?.addEventListener("click", () => {
            Sound.playButtonPop();
            this.openUserProfileHistoryModal();
        });

        document.getElementById("btn-lobby-history")?.addEventListener("click", () => {
            Sound.playButtonPop();
            this.openUserProfileHistoryModal();
        });

        // 5b. Profile & History Modal Tabs
        const profTabs = [
            { btn: "tab-btn-prof-stats", panel: "prof-panel-stats" },
            { btn: "tab-btn-prof-history", panel: "prof-panel-history" },
            { btn: "tab-btn-prof-yaku", panel: "prof-panel-yaku" }
        ];

        profTabs.forEach(t => {
            document.getElementById(t.btn)?.addEventListener("click", () => {
                Sound.playButtonPop();
                profTabs.forEach(item => {
                    document.getElementById(item.btn)?.classList.remove("active");
                    document.getElementById(item.panel)?.classList.remove("active");
                });
                document.getElementById(t.btn)?.classList.add("active");
                document.getElementById(t.panel)?.classList.add("active");
            });
        });

        // 6. Leaderboard Button
        document.getElementById("btn-lobby-leaderboard")?.addEventListener("click", () => {
            Sound.playButtonPop();
            this.openModal("modal-leaderboard");
            this.loadLeaderboardFromSupabase();
        });

        // 7. Logout / Ganti Akun
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

    requestLandscapeLock() {
        try {
            if (screen.orientation && typeof screen.orientation.lock === 'function') {
                screen.orientation.lock('landscape').catch(() => {});
            } else if (screen.lockOrientation) {
                screen.lockOrientation('landscape');
            }
        } catch (e) {}
    }

    switchScreen(screenId) {
        this.requestLandscapeLock();
        document.querySelectorAll(".screen-view").forEach(s => s.classList.remove("active"));
        const target = document.getElementById(screenId);
        if (target) target.classList.add("active");

        if (screenId === "screen-game") {
            setTimeout(() => {
                if (this.visualizer && typeof this.visualizer.onWindowResize === 'function') {
                    this.visualizer.onWindowResize();
                }
            }, 100);
        }
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
        this.updateHUDWallCount();

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
        const remaining = (this.gameLogic && this.gameLogic.deck) ? this.gameLogic.deck.length : 0;
        const wallEl = document.getElementById("hud-wall-count");
        if (wallEl) {
            wallEl.textContent = `${remaining}`;
        }
        if (this.visualizer && typeof this.visualizer.setWallCount === 'function') {
            this.visualizer.setWallCount(remaining);
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
            const isTsumo = result.isSelfDraw || (result.scoreData && result.scoreData.isSelfDraw) || false;

            // Kumpulkan Yaku / Special Hands yang didapat
            const specialHandsEarned = [];
            const yakuKeyMap = {
                "Thirteen Orphans": "thirteen_orphans",
                "Nine Gates": "nine_gates",
                "All Green": "all_green",
                "Pure Suit": "pure_flush",
                "Seven Pairs": "seven_pairs",
                "All Triplets": "all_pongs",
                "All Honors": "all_honors",
                "Full 8 Bonus": "all_honors"
            };
            if (breakdown) {
                breakdown.forEach(item => {
                    for (const [nameMatch, key] of Object.entries(yakuKeyMap)) {
                        if (item.name && item.name.includes(nameMatch)) {
                            specialHandsEarned.push(key);
                        }
                    }
                });
            }

            // Sinkronkan hasil pertandingan ke Supabase Cloud & Local Storage
            const cloudSync = await SupabaseDB.recordMatchEnd({
                roomCode: this.currentRoomCode || "VIP-SOLO",
                gameMode: this.currentGameMode,
                winnerSeat: result.winnerSeat,
                winnerName: winnerName,
                isWinnerLocal: isMe,
                finalScores: { [this.localSeat]: isMe ? totalScore : 0 },
                highestScore: totalScore,
                isTsumo: isTsumo,
                specialHandsEarned: specialHandsEarned
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

    // --- PROFIL & RIWAYAT PERTANDINGAN PEMAIN ---

    async openUserProfileHistoryModal() {
        const uid = localStorage.getItem("mahjong_user_id") || "usr_guest";

        // Update Hero Card
        const avatarEl = document.getElementById("prof-modal-avatar");
        if (avatarEl) avatarEl.textContent = this.getAvatarForAuth();

        const nameEl = document.getElementById("prof-modal-name");
        if (nameEl) nameEl.textContent = this.displayName || this.username;

        const unameEl = document.getElementById("prof-modal-username");
        if (unameEl) unameEl.textContent = `@${this.username}`;

        const uidEl = document.getElementById("prof-modal-uid");
        if (uidEl) uidEl.textContent = uid.length > 16 ? uid.substring(0, 16) + "..." : uid;

        const authEl = document.getElementById("prof-modal-auth");
        if (authEl) {
            authEl.className = `auth-tag tag-${this.authType.toLowerCase()}`;
            authEl.textContent = this.authType === "google" ? "🔴 Google VIP" : (this.authType === "email" ? "💎 Terdaftar" : "👤 Tamu");
        }

        const rankEl = document.getElementById("prof-modal-rank");
        if (rankEl) rankEl.textContent = this.rankTier;

        const chipsEl = document.getElementById("prof-modal-chips");
        if (chipsEl) chipsEl.textContent = this.coins.toLocaleString();

        const trophiesEl = document.getElementById("prof-modal-trophies");
        if (trophiesEl) trophiesEl.textContent = `${this.trophies.toLocaleString()} Pts`;

        this.openModal("modal-profile-history");

        // Load data tabs
        await this.loadUserCareerStats(uid);
        await this.loadUserMatchHistory(uid);
    }

    async loadUserCareerStats(userId) {
        try {
            const stats = await SupabaseDB.fetchUserStatsDetail(userId);
            if (!stats) return;

            const setVal = (id, val) => {
                const el = document.getElementById(id);
                if (el) el.textContent = val;
            };

            setVal("stat-total-matches", (stats.total_matches || 0).toLocaleString());
            setVal("stat-total-wins", (stats.total_wins || 0).toLocaleString());
            setVal("stat-total-losses", (stats.total_losses || 0).toLocaleString());
            setVal("stat-win-rate", `${(stats.win_rate || 0).toFixed(1)}%`);
            setVal("stat-win-streak", `${stats.current_win_streak || 0} 🔥`);
            setVal("stat-best-streak", stats.highest_win_streak || 0);
            setVal("stat-tsumo-wins", (stats.tsumo_wins || 0).toLocaleString());
            setVal("stat-ron-wins", (stats.ron_wins || 0).toLocaleString());
            setVal("stat-highest-score", (stats.highest_match_score || 0).toLocaleString());
            setVal("stat-total-earned", (stats.total_chips_earned || this.coins || 25000).toLocaleString());

            this.renderYakuGallery(stats.special_hands_record || {});
        } catch (e) {
            console.warn("Gagal render career stats:", e);
        }
    }

    async loadUserMatchHistory(userId) {
        const container = document.getElementById("prof-match-history-list");
        if (!container) return;

        container.innerHTML = `
            <div style="text-align: center; padding: 25px; color: var(--gold-bright);">
                ⏳ Mengambil riwayat pertandingan dari Cloud Supabase...
            </div>
        `;

        try {
            const history = await SupabaseDB.fetchUserMatchHistory(userId);
            if (!history || history.length === 0) {
                container.innerHTML = `
                    <div style="text-align: center; padding: 30px 20px; background: rgba(0,0,0,0.3); border-radius: 12px; border: 1px dashed rgba(255,255,255,0.15);">
                        <div style="font-size: 2.2rem; margin-bottom: 8px;">🀄</div>
                        <div style="font-weight: 700; color: #ffffff; margin-bottom: 4px;">Belum Ada Riwayat Pertandingan</div>
                        <div style="font-size: 0.82rem; color: rgba(255,255,255,0.6);">Mainkan ronde pertama Anda sekarang untuk mencatat data karir!</div>
                    </div>
                `;
                return;
            }

            container.innerHTML = history.map((item, idx) => {
                const isWin = item.is_winner || item.rank_position === 1;
                const isDraw = item.win_type === "DRAW" || item.game_mode === "DRAW";
                const modeName = item.game_mode === "SOLO_AI" ? "🤖 Latihan AI" : (item.game_mode === "TOURNAMENT" ? "🏆 Turnamen" : "🗝️ Ruangan VIP");
                
                let dateStr = "Baru Saja";
                if (item.started_at) {
                    try {
                        const d = new Date(item.started_at);
                        dateStr = d.toLocaleDateString("id-ID", { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' });
                    } catch (e) {}
                }

                let badgeClass = isWin ? "win" : (isDraw ? "draw" : "loss");
                let badgeText = isWin ? "🥇 MENANG (HU)" : (isDraw ? "⚖️ SERI" : "KALAH");
                let itemClass = isWin ? "is-victory" : (isDraw ? "is-draw" : "is-defeat");

                const trophyText = item.trophy_delta > 0 ? `+${item.trophy_delta}` : `${item.trophy_delta}`;
                const chipsText = item.chips_delta > 0 ? `+${item.chips_delta.toLocaleString()}` : `${item.chips_delta.toLocaleString()}`;

                return `
                    <div class="match-history-item ${itemClass}">
                        <div style="display: flex; align-items: center; gap: 12px;">
                            <span class="match-rank-badge ${badgeClass}">${badgeText}</span>
                            <div>
                                <div style="font-weight: 700; color: #ffffff; font-size: 0.9rem;">${modeName} <span style="font-weight: 400; font-size: 0.75rem; color: rgba(255,255,255,0.5);">(${item.room_code || 'VIP'})</span></div>
                                <div style="font-size: 0.75rem; color: rgba(255,255,255,0.5);">${dateStr}</div>
                            </div>
                        </div>

                        <div style="display: flex; align-items: center; gap: 16px;">
                            <div style="text-align: right;">
                                <div style="font-size: 0.85rem; font-weight: 800; color: var(--gold-bright);">${item.final_score ? `+${item.final_score} Pts` : ''}</div>
                                <div style="font-size: 0.75rem; color: ${item.trophy_delta >= 0 ? 'var(--jade-accent)' : '#ff8080'}; font-weight: 700;">
                                    🏆 ${trophyText} Pts
                                </div>
                            </div>

                            <div style="background: rgba(0,0,0,0.4); padding: 4px 10px; border-radius: 8px; border: 1px solid rgba(255,255,255,0.1); font-family: monospace; font-size: 0.85rem; color: ${item.chips_delta >= 0 ? 'var(--jade-accent)' : '#ff8080'};">
                                🪙 ${chipsText}
                            </div>
                        </div>
                    </div>
                `;
            }).join('');
        } catch (err) {
            container.innerHTML = `
                <div style="text-align: center; padding: 20px; color: #ff6666;">
                    ⚠️ Gagal memuat riwayat pertandingan: ${err.message}
                </div>
            `;
        }
    }

    renderYakuGallery(yakuRecord = {}) {
        const container = document.getElementById("prof-yaku-grid");
        if (!container) return;

        const yakus = this.getYakuDefinitions();
        container.innerHTML = yakus.map(y => {
            const count = yakuRecord[y.key] || 0;
            const isUnlocked = count > 0;

            return `
                <div class="yaku-card ${isUnlocked ? 'unlocked' : ''}">
                    <div>
                        <div class="yaku-card-header">
                            <span class="yaku-card-title">${y.name}</span>
                            <span class="yaku-card-pts">${y.pts}</span>
                        </div>
                        <p class="yaku-card-desc">${y.desc}</p>
                    </div>
                    <div>
                        <span class="yaku-card-status ${isUnlocked ? 'achieved' : 'locked'}">
                            ${isUnlocked ? `✨ Tercapai: ${count}x` : '🔒 Belum Tercapai'}
                        </span>
                    </div>
                </div>
            `;
        }).join('');
    }

    getYakuDefinitions() {
        return [
            { key: "thirteen_orphans", name: "Thirteen Orphans (Kokushi 國士無雙)", pts: "+150 Pts", desc: "13 jenis ubin Terminal (1 & 9) & Honor lengkap + 1 pasang kembar." },
            { key: "nine_gates", name: "Nine Gates (Chuuren 九蓮寶燈)", pts: "+150 Pts", desc: "Pola 1112345678999 pada satu jenis suit angka." },
            { key: "all_green", name: "All Green (Ryuuiisou 綠一色)", pts: "+150 Pts", desc: "Hanya terdiri dari Bamboo (2, 3, 4, 6, 8) & Green Dragon (Fa)." },
            { key: "big_four_winds", name: "Big Four Winds (Daisuushii 大四喜)", pts: "+150 Pts", desc: "4 set Pong/Kong dari Angin Timur, Selatan, Barat, dan Utara." },
            { key: "all_honors", name: "All Honors (Tsuuiisou 字一色)", pts: "+150 Pts", desc: "Seluruh ubin hanya terdiri dari ubin Angin dan Naga." },
            { key: "four_concealed_pongs", name: "Four Concealed Pongs (Suuankou 四暗刻)", pts: "+150 Pts", desc: "4 set Pong/Kong tertutup tanpa mencuri buangan lawan." },
            { key: "all_kongs", name: "All Kongs (Suukantsu 四槓子)", pts: "+150 Pts", desc: "4 set Kong (16 ubin lengkap + 1 pair)." },
            { key: "all_terminals", name: "All Terminals (Chinroutou 清老頭)", pts: "+150 Pts", desc: "Seluruh set hanya terdiri dari ubin angka 1 dan 9." },
            { key: "little_four_winds", name: "Little Four Winds (Shousuushii 小四喜)", pts: "+120 Pts", desc: "3 set Pong/Kong angin + 1 pasang angin ke-4." },
            { key: "big_three_dragons", name: "Big Three Dragons (Daisangen 大三元)", pts: "+120 Pts", desc: "3 set Pong/Kong Naga Merah, Hijau, dan Putih." },
            { key: "pure_flush", name: "Pure Flush (Chinitsu 清一色)", pts: "+100 Pts", desc: "Semua ubin dari satu suit angka (tanpa angin/naga)." },
            { key: "seven_pairs", name: "Seven Pairs (Chiitoitsu 七對子)", pts: "+100 Pts", desc: "7 pasang ubin kembar berbeda yang terpisah." },
            { key: "little_three_dragons", name: "Little Three Dragons (Shousangen 小三元)", pts: "+80 Pts", desc: "2 set Pong/Kong naga + 1 pasang naga ke-3." },
            { key: "mixed_flush", name: "Mixed Flush (Honitsu 混一色)", pts: "+60 Pts", desc: "Satu suit angka dikombinasikan dengan ubin Angin/Naga." },
            { key: "all_pongs", name: "All Triplets (Toitoi 對對和)", pts: "+50 Pts", desc: "4 set Pong/Kong ubin kembar + 1 pair." }
        ];
    }

    saveAuthState() {
        localStorage.setItem("mahjong_user", this.username);
        localStorage.setItem("mahjong_display_name", this.displayName || this.username);
        localStorage.setItem("mahjong_auth_type", this.authType);
        localStorage.setItem("mahjong_email", this.userEmail || "");
        localStorage.setItem("mahjong_avatar", (this.avatarIdx || 1).toString());
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
                const avatarIcon = this.getAvatarForAuth(entry.avatar_id);

                return `
                    <tr class="${topClass}" style="${meHighlight}">
                        <td>${rankBadgeHtml}</td>
                        <td>
                            <div style="display: flex; align-items: center; gap: 8px;">
                                <span style="font-size: 1.2rem;">${avatarIcon}</span>
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
