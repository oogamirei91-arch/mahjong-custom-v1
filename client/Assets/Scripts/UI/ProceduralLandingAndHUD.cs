using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mahjong.Network;
using Mahjong.Audio;

namespace Mahjong.UI
{
    /// <summary>
    /// ProceduralLandingAndHUD: Generator Antarmuka Otomatis (Landing Page, Login/Register Modal, 
    /// Main Menu Lobby 4-Card Grid, Custom Room Modal, Friend List HUD, dan In-Game HUD).
    /// Didesain khusus untuk Orientasi LANDSCAPE (16:9 / 1920x1080) bergaya Modern VIP Casino.
    /// </summary>
    public class ProceduralLandingAndHUD : MonoBehaviour
    {
        public static ProceduralLandingAndHUD Instance { get; private set; }

        private Canvas canvas;
        private CanvasScaler canvasScaler;
        private GraphicRaycaster graphicRaycaster;

        // Panel Kontainer Utama
        private GameObject panelLandingLogin;
        private GameObject panelMainMenuLobby;
        private GameObject panelCustomRoomModal;
        private GameObject panelFriendListModal;
        private GameObject panelActionBarHUD;
        private GameObject panelVictoryModal;
        private GameObject panelInGameHUD;

        // Komponen Input & Text
        private InputField inputUsername;
        private InputField inputPassword;
        private Text txtAuthStatus;
        private Text txtTopBarProfile;
        private Text txtCustomRoomDisplay;
        private InputField inputJoinCode;
        private Text txtVictoryDetails;

        // Komponen In-Game HUD
        private Text txtInGameTurnStatus;
        private Text txtWallTilesCounter;
        private Button btnDiscardSelected;
        private Text txtDiscardButtonLabel;

        /// <summary>
        /// Otomatis dijalankan saat scene dimuat agar UI selalu muncul walaupun belum ditaruh manual di Hierarchy!
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeAllManagers()
        {
            // Set orientasi layar ke Landscape
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;

            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                if (cam.GetComponent<Visual.CameraController>() == null)
                    cam.gameObject.AddComponent<Visual.CameraController>();
                if (cam.GetComponent<TouchInputHandler>() == null)
                    cam.gameObject.AddComponent<TouchInputHandler>();
            }

            if (Object.FindFirstObjectByType<Procedural.ProceduralTable>() == null)
            {
                GameObject tableObj = new GameObject("[ProceduralTable]");
                tableObj.AddComponent<Procedural.ProceduralTable>();
                DontDestroyOnLoad(tableObj);
            }
            if (Object.FindFirstObjectByType<Procedural.TableCompass>() == null)
            {
                GameObject compassObj = new GameObject("[TableCompass]");
                compassObj.AddComponent<Procedural.TableCompass>();
                DontDestroyOnLoad(compassObj);
            }
            if (Object.FindFirstObjectByType<Procedural.ProceduralTileAtlas>() == null)
            {
                GameObject atlasObj = new GameObject("[ProceduralTileAtlas]");
                atlasObj.AddComponent<Procedural.ProceduralTileAtlas>();
                DontDestroyOnLoad(atlasObj);
            }
            if (Object.FindFirstObjectByType<GameNetworkManager>() == null)
            {
                GameObject netObj = new GameObject("[GameNetworkManager]");
                netObj.AddComponent<GameNetworkManager>();
                DontDestroyOnLoad(netObj);
            }
            if (Object.FindFirstObjectByType<Visual.TableVisualizer>() == null)
            {
                GameObject visObj = new GameObject("[TableVisualizer]");
                visObj.AddComponent<Visual.TableVisualizer>();
                DontDestroyOnLoad(visObj);
            }
            if (Object.FindFirstObjectByType<AI.SinglePlayerAIManager>() == null)
            {
                GameObject aiObj = new GameObject("[SinglePlayerAIManager]");
                aiObj.AddComponent<AI.SinglePlayerAIManager>();
                DontDestroyOnLoad(aiObj);
            }
            if (Object.FindFirstObjectByType<Audio.ProceduralAudioSynthesizer>() == null)
            {
                GameObject audioObj = new GameObject("[AudioManager]");
                audioObj.AddComponent<Audio.ProceduralAudioSynthesizer>();
                DontDestroyOnLoad(audioObj);
            }
            if (Object.FindFirstObjectByType<ProceduralLandingAndHUD>() == null)
            {
                GameObject canvasObj = new GameObject("[Canvas_UI]", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ProceduralLandingAndHUD));
                DontDestroyOnLoad(canvasObj);
            }
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            SetupCanvas();
            BuildLandingLoginUI();
            BuildMainMenuLobbyUI();
            BuildCustomRoomModalUI();
            BuildFriendListModalUI();
            BuildInGameHUD();
            BuildActionBarHUD();
            BuildVictoryModalUI();
        }

        private void Start()
        {
            if (GameNetworkManager.Instance != null)
            {
                GameNetworkManager.Instance.OnAuthResult += OnAuthResultReceived;
                GameNetworkManager.Instance.OnMatchFound += OnMatchFoundReceived;
                GameNetworkManager.Instance.OnCustomRoomCreated += OnCustomRoomCreatedReceived;
                GameNetworkManager.Instance.OnReactionPromptReceived += OnReactionPromptReceived;
                GameNetworkManager.Instance.OnRoundEnded += OnRoundEndedReceived;
            }

            ShowLandingScreen();
        }

        private void SetupCanvas()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
                DontDestroyOnLoad(eventSystemObj);
            }

            canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            canvasScaler = gameObject.GetComponent<CanvasScaler>();
            if (canvasScaler == null) canvasScaler = gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080); // Orientasi Landscape 16:9
            canvasScaler.matchWidthOrHeight = 0.5f;

            graphicRaycaster = gameObject.GetComponent<GraphicRaycaster>();
            if (graphicRaycaster == null) graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
        }

        // =========================================================================
        // 1. LANDING PAGE & LOGIN / REGISTER SCREEN (LANDSCAPE)
        // =========================================================================

        private void BuildLandingLoginUI()
        {
            panelLandingLogin = CreatePanel("Panel_LandingLogin", new Color(0.04f, 0.12f, 0.08f, 0.96f));

            CreateText(panelLandingLogin.transform, "MAHJONG VIP 🀄", 56, FontStyle.Bold, new Color(1.0f, 0.84f, 0.0f), new Vector2(0, 390), new Vector2(1200, 90));
            CreateText(panelLandingLogin.transform, "Casual Multiplayer • 4-Player 3D Board Game", 24, FontStyle.Italic, new Color(0.8f, 0.95f, 0.85f), new Vector2(0, 325), new Vector2(1200, 50));

            GameObject card = CreateCard(panelLandingLogin.transform, new Vector2(850, 540), new Vector2(0, -60));

            CreateText(card.transform, "MASUK / DAFTAR AKUN", 28, FontStyle.Bold, Color.white, new Vector2(0, 200), new Vector2(700, 50));

            inputUsername = CreateInputField(card.transform, "Username...", new Vector2(0, 115), new Vector2(700, 70));
            inputPassword = CreateInputField(card.transform, "Password...", new Vector2(0, 30), new Vector2(700, 70), true);

            txtAuthStatus = CreateText(card.transform, "Silakan login atau mainkan secara instan sebagai Tamu (Guest)", 20, FontStyle.Normal, new Color(1.0f, 0.85f, 0.4f), new Vector2(0, -45), new Vector2(700, 45));

            CreateButton(card.transform, "LOGIN", new Color(0.1f, 0.55f, 0.25f), new Vector2(-180, -125), new Vector2(320, 68), OnClickLogin);
            CreateButton(card.transform, "REGISTER", new Color(0.2f, 0.4f, 0.7f), new Vector2(180, -125), new Vector2(320, 68), OnClickRegister);
            CreateButton(card.transform, "⚡ MAIN SEBAGAI GUEST", new Color(0.85f, 0.65f, 0.15f), new Vector2(0, -205), new Vector2(680, 65), OnClickGuestPlay);
        }

        // =========================================================================
        // 2. MAIN MENU LOBBY (LANDSCAPE 4-CARD GRID)
        // =========================================================================

        private void BuildMainMenuLobbyUI()
        {
            panelMainMenuLobby = CreatePanel("Panel_MainMenuLobby", Color.clear);

            // Top Bar Profile HUD (Landscape Span)
            GameObject topBar = CreateCard(panelMainMenuLobby.transform, new Vector2(1820, 85), new Vector2(0, 475));
            topBar.GetComponent<Image>().color = new Color(0.04f, 0.14f, 0.09f, 0.98f);
            txtTopBarProfile = CreateText(topBar.transform, "👑 Player_VIP | 🏆 1,000 Trofi | 🥇 Gold Master", 24, FontStyle.Bold, new Color(1.0f, 0.85f, 0.2f), new Vector2(-300, 0), new Vector2(1100, 70));
            CreateButton(topBar.transform, "🚪 Logout", new Color(0.6f, 0.2f, 0.2f), new Vector2(780, 0), new Vector2(180, 58), ShowLandingScreen);

            // 4 Mode Kartu Berjejer di Tengah Meja (Landscape 4-Card Hub)
            CreateMenuModeCard(panelMainMenuLobby.transform, "🏆 RANKED MATCH", "Multiplayer Online 4 Pemain\nPerebutan Trofi & Peringkat", new Color(0.08f, 0.55f, 0.28f), new Vector2(-540, -35), OnClickRankedMatch);
            CreateMenuModeCard(panelMainMenuLobby.transform, "🗝️ CUSTOM ROOM", "Mabar Bersama Teman\nDengan 4-Digit Kode VIP", new Color(0.15f, 0.45f, 0.75f), new Vector2(-180, -35), OnClickOpenCustomRoomModal);
            CreateMenuModeCard(panelMainMenuLobby.transform, "👥 DAFTAR TEMAN", "Lihat Teman Online\nKirim Undangan Mabar", new Color(0.55f, 0.35f, 0.75f), new Vector2(180, -35), OnClickOpenFriendListModal);
            CreateMenuModeCard(panelMainMenuLobby.transform, "🤖 SOLO VS 3 BOT", "Latihan Cepat Offline\nMelawan 3 Bot AI Cerdas", new Color(0.85f, 0.45f, 0.15f), new Vector2(540, -35), OnClickSoloAIMatch);
        }

        private void CreateMenuModeCard(Transform parent, string title, string subtitle, Color btnColor, Vector2 pos, UnityEngine.Events.UnityAction action)
        {
            GameObject card = CreateCard(parent, new Vector2(335, 460), pos);
            card.GetComponent<Image>().color = new Color(0.05f, 0.16f, 0.10f, 0.94f);

            CreateText(card.transform, title, 26, FontStyle.Bold, new Color(1f, 0.88f, 0.2f), new Vector2(0, 160), new Vector2(300, 60));
            CreateText(card.transform, subtitle, 19, FontStyle.Normal, new Color(0.85f, 0.95f, 0.9f), new Vector2(0, 30), new Vector2(290, 160));
            CreateButton(card.transform, "MAIN SEKARANG", btnColor, new Vector2(0, -155), new Vector2(280, 65), action);
        }

        // =========================================================================
        // 3. IN-GAME TURN HUD & FLOATING ACTION BUTTON (LANDSCAPE)
        // =========================================================================

        private void BuildInGameHUD()
        {
            panelInGameHUD = CreatePanel("Panel_InGameHUD", Color.clear);

            // Tombol Menu (Kiri Atas)
            CreateButton(panelInGameHUD.transform, "⚙️ Menu", new Color(0.2f, 0.25f, 0.3f, 0.95f), new Vector2(-840, 480), new Vector2(140, 60), ShowMainMenuLobby);

            // Banner Status Giliran (Tengah Atas)
            GameObject banner = CreateCard(panelInGameHUD.transform, new Vector2(720, 60), new Vector2(0, 480));
            banner.GetComponent<Image>().color = new Color(0.03f, 0.15f, 0.08f, 0.95f);
            txtInGameTurnStatus = CreateText(banner.transform, "🟢 GILIRAN ANDA! (Pilih ubin lalu buang)", 22, FontStyle.Bold, new Color(1f, 0.92f, 0.4f), Vector2.zero, new Vector2(700, 55));

            // Sisa Ubin Wall (Kanan Atas)
            GameObject wallBox = CreateCard(panelInGameHUD.transform, new Vector2(180, 60), new Vector2(830, 480));
            wallBox.GetComponent<Image>().color = new Color(0.03f, 0.15f, 0.08f, 0.95f);
            txtWallTilesCounter = CreateText(wallBox.transform, "🀄 Wall: 72", 20, FontStyle.Bold, Color.white, Vector2.zero, new Vector2(170, 55));

            // Tombol Buang Ubin Terpilih (Kanan Bawah - Mudah Ditekan Jempol Kanan di Layar HP!)
            btnDiscardSelected = CreateButton(panelInGameHUD.transform, "🔥 BUANG UBIN", new Color(0.85f, 0.25f, 0.15f), new Vector2(720, -380), new Vector2(340, 85), () =>
            {
                TouchInputHandler.Instance?.ExecuteDiscardSelectedTile();
            });
            txtDiscardButtonLabel = btnDiscardSelected.GetComponentInChildren<Text>();

            panelInGameHUD.SetActive(false);
        }

        public void UpdateTurnStatusHUD(string status, bool isPlayerTurn)
        {
            if (txtInGameTurnStatus != null)
            {
                txtInGameTurnStatus.text = status;
                txtInGameTurnStatus.color = isPlayerTurn ? new Color(0.3f, 1.0f, 0.5f) : new Color(1.0f, 0.85f, 0.3f);
            }
        }

        public void UpdateWallCounterHUD(int remaining)
        {
            if (txtWallTilesCounter != null)
            {
                txtWallTilesCounter.text = $"🀄 Wall: {remaining}";
            }
        }

        public void OnTileSelectedHUD(Procedural.ProceduralTile tile)
        {
            if (btnDiscardSelected != null && tile != null)
            {
                btnDiscardSelected.gameObject.SetActive(true);
                if (txtDiscardButtonLabel != null)
                {
                    txtDiscardButtonLabel.text = $"🔥 BUANG: {tile.tileName}";
                }
            }
        }

        public void OnTileDiscardedHUD()
        {
            if (btnDiscardSelected != null)
            {
                btnDiscardSelected.gameObject.SetActive(false);
            }
        }

        // =========================================================================
        // 4. CUSTOM ROOM MODAL (VIP CODE)
        // =========================================================================

        private void BuildCustomRoomModalUI()
        {
            panelCustomRoomModal = CreatePanel("Panel_CustomRoomModal", new Color(0, 0, 0, 0.85f));

            GameObject card = CreateCard(panelCustomRoomModal.transform, new Vector2(850, 560), Vector2.zero);
            CreateText(card.transform, "CUSTOM ROOM (VIP MABAR)", 30, FontStyle.Bold, new Color(1f, 0.84f, 0f), new Vector2(0, 215), new Vector2(750, 55));

            CreateButton(card.transform, "✨ BUAT ROOM BARU", new Color(0.1f, 0.6f, 0.3f), new Vector2(0, 120), new Vector2(600, 75), OnClickCreateVIPRoom);
            txtCustomRoomDisplay = CreateText(card.transform, "Kode Anda: Belum dibuat", 22, FontStyle.Bold, new Color(0.8f, 1f, 0.8f), new Vector2(0, 50), new Vector2(600, 45));

            CreateText(card.transform, "— ATAU GABUNG ROOM TEMAN —", 20, FontStyle.Italic, Color.gray, new Vector2(0, -5), new Vector2(600, 35));

            inputJoinCode = CreateInputField(card.transform, "Masukkan Kode (misal: VIP-8821)...", new Vector2(0, -70), new Vector2(600, 70));
            CreateButton(card.transform, "➡️ GABUNG ROOM", new Color(0.2f, 0.45f, 0.8f), new Vector2(0, -155), new Vector2(600, 75), OnClickJoinVIPRoom);

            CreateButton(card.transform, "❌ TUTUP", new Color(0.5f, 0.5f, 0.5f), new Vector2(0, -230), new Vector2(260, 55), () => panelCustomRoomModal.SetActive(false));
        }

        // =========================================================================
        // 5. FRIEND LIST MODAL
        // =========================================================================

        private void BuildFriendListModalUI()
        {
            panelFriendListModal = CreatePanel("Panel_FriendListModal", new Color(0, 0, 0, 0.85f));

            GameObject card = CreateCard(panelFriendListModal.transform, new Vector2(850, 580), Vector2.zero);
            CreateText(card.transform, "DAFTAR TEMAN (FRIEND LIST)", 30, FontStyle.Bold, new Color(1f, 0.84f, 0f), new Vector2(0, 230), new Vector2(750, 55));

            CreateText(card.transform, "Teman Online:", 22, FontStyle.Bold, Color.white, new Vector2(-260, 165), new Vector2(250, 45));

            CreateText(card.transform, "🟢 Alex_VIP (In Lobby) — 🏆 1,450 [UNDANG MABAR]", 20, FontStyle.Normal, new Color(0.7f, 1f, 0.7f), new Vector2(0, 95), new Vector2(750, 45));
            CreateText(card.transform, "🟢 Dewi_Mahjong (In Game) — 🏆 2,100", 20, FontStyle.Normal, new Color(0.9f, 0.9f, 0.5f), new Vector2(0, 40), new Vector2(750, 45));
            CreateText(card.transform, "⚪ Budi_Dragon (Offline)", 20, FontStyle.Normal, Color.gray, new Vector2(0, -15), new Vector2(750, 45));

            CreateButton(card.transform, "➕ TAMBAH TEMAN", new Color(0.15f, 0.5f, 0.75f), new Vector2(0, -120), new Vector2(500, 68), () => Debug.Log("Tambah Teman"));
            CreateButton(card.transform, "❌ TUTUP", new Color(0.5f, 0.5f, 0.5f), new Vector2(0, -210), new Vector2(260, 55), () => panelFriendListModal.SetActive(false));
        }

        // =========================================================================
        // 6. IN-GAME ACTION BAR HUD (CHOW, PONG, KONG, WIN, PASS)
        // =========================================================================

        private void BuildActionBarHUD()
        {
            panelActionBarHUD = CreatePanel("Panel_ActionBarHUD", Color.clear);
            GameObject barCard = CreateCard(panelActionBarHUD.transform, new Vector2(980, 100), new Vector2(0, -260));
            barCard.GetComponent<Image>().color = new Color(0.04f, 0.14f, 0.08f, 0.96f);

            CreateButton(barCard.transform, "CHOW (吃)", new Color(0.2f, 0.6f, 0.8f), new Vector2(-360, 0), new Vector2(170, 70), () => SubmitReaction("chow"));
            CreateButton(barCard.transform, "PONG (碰)", new Color(0.9f, 0.6f, 0.1f), new Vector2(-180, 0), new Vector2(170, 70), () => SubmitReaction("pong"));
            CreateButton(barCard.transform, "KONG (槓)", new Color(0.7f, 0.2f, 0.8f), new Vector2(0, 0), new Vector2(170, 70), () => SubmitReaction("kong"));
            CreateButton(barCard.transform, "WIN / HU (胡)", new Color(0.9f, 0.15f, 0.2f), new Vector2(180, 0), new Vector2(170, 70), () => SubmitReaction("win"));
            CreateButton(barCard.transform, "PASS (過)", new Color(0.4f, 0.4f, 0.4f), new Vector2(360, 0), new Vector2(150, 70), () => SubmitReaction("pass"));

            panelActionBarHUD.SetActive(false);
        }

        // =========================================================================
        // 7. VICTORY / ROUND END MODAL
        // =========================================================================

        private void BuildVictoryModalUI()
        {
            panelVictoryModal = CreatePanel("Panel_VictoryModal", new Color(0, 0, 0, 0.88f));

            GameObject card = CreateCard(panelVictoryModal.transform, new Vector2(850, 560), Vector2.zero);
            CreateText(card.transform, "🏆 RONDE SELESAI 🏆", 34, FontStyle.Bold, new Color(1f, 0.85f, 0f), new Vector2(0, 215), new Vector2(750, 60));

            txtVictoryDetails = CreateText(card.transform, "PEMENANG: SEAT SOUTH (ANDA)\n\n• Base Win: +100 Poin\n• Self-Draw (Zimo): +30 Poin\n• Special Hand (All Triplets): +50 Poin\n\nTotal: +180 Poin | Perolehan: +40 Trofi 🏆", 22, FontStyle.Normal, Color.white, new Vector2(0, 15), new Vector2(750, 260));

            CreateButton(card.transform, "LANJUT KE LOBBY", new Color(0.1f, 0.65f, 0.3f), new Vector2(0, -190), new Vector2(450, 75), ShowMainMenuLobby);

            panelVictoryModal.SetActive(false);
        }

        // =========================================================================
        // LOGIKA TOMBOL & NAVIGASI
        // =========================================================================

        public void ShowLandingScreen()
        {
            if (panelLandingLogin) panelLandingLogin.SetActive(true);
            if (panelMainMenuLobby) panelMainMenuLobby.SetActive(false);
            if (panelCustomRoomModal) panelCustomRoomModal.SetActive(false);
            if (panelFriendListModal) panelFriendListModal.SetActive(false);
            if (panelInGameHUD) panelInGameHUD.SetActive(false);
            if (panelActionBarHUD) panelActionBarHUD.SetActive(false);
            if (panelVictoryModal) panelVictoryModal.SetActive(false);
        }

        public void ShowMainMenuLobby()
        {
            if (panelLandingLogin) panelLandingLogin.SetActive(false);
            if (panelMainMenuLobby) panelMainMenuLobby.SetActive(true);
            if (panelCustomRoomModal) panelCustomRoomModal.SetActive(false);
            if (panelFriendListModal) panelFriendListModal.SetActive(false);
            if (panelInGameHUD) panelInGameHUD.SetActive(false);
            if (panelActionBarHUD) panelActionBarHUD.SetActive(false);
            if (panelVictoryModal) panelVictoryModal.SetActive(false);
        }

        public void ShowInGameHUD()
        {
            if (panelLandingLogin) panelLandingLogin.SetActive(false);
            if (panelMainMenuLobby) panelMainMenuLobby.SetActive(false);
            if (panelCustomRoomModal) panelCustomRoomModal.SetActive(false);
            if (panelFriendListModal) panelFriendListModal.SetActive(false);
            if (panelInGameHUD) panelInGameHUD.SetActive(true);
            if (btnDiscardSelected) btnDiscardSelected.gameObject.SetActive(false);
        }

        private void OnClickLogin()
        {
            string user = inputUsername.text.Trim();
            string pass = inputPassword.text.Trim();
            if (string.IsNullOrEmpty(user)) { txtAuthStatus.text = "Username tidak boleh kosong!"; return; }
            txtAuthStatus.text = "Menghubungi server...";
            ProceduralAudioSynthesizer.Instance?.PlayButtonPop();
            GameNetworkManager.Instance?.Login(user, pass);
        }

        private void OnClickRegister()
        {
            string user = inputUsername.text.Trim();
            string pass = inputPassword.text.Trim();
            if (string.IsNullOrEmpty(user)) { txtAuthStatus.text = "Username tidak boleh kosong!"; return; }
            txtAuthStatus.text = "Mendaftarkan akun...";
            ProceduralAudioSynthesizer.Instance?.PlayButtonPop();
            GameNetworkManager.Instance?.Register(user, pass);
        }

        private void OnClickGuestPlay()
        {
            string guestName = "Guest_" + Random.Range(1000, 9999);
            txtTopBarProfile.text = $"👑 {guestName} | 🏆 500 Trofi | 🥈 Silver Pro";
            ProceduralAudioSynthesizer.Instance?.PlayButtonPop();
            ShowMainMenuLobby();
        }

        private void OnClickRankedMatch()
        {
            ProceduralAudioSynthesizer.Instance?.PlayButtonPop();
            panelMainMenuLobby.SetActive(false);
            ShowInGameHUD();
            Debug.Log("[Lobby] Masuk ke antrean Ranked Matchmaking...");
            GameNetworkManager.Instance?.JoinRankedQueue();
        }

        private void OnClickSoloAIMatch()
        {
            ProceduralAudioSynthesizer.Instance?.PlayButtonPop();
            ShowInGameHUD();
            if (Mahjong.AI.SinglePlayerAIManager.Instance != null)
            {
                Mahjong.AI.SinglePlayerAIManager.Instance.StartSoloGame(Mahjong.AI.AIDifficultyLevel.Expert);
            }
            Debug.Log("[Lobby] Memulai Mode Solo Latihan vs 3 Bot AI...");
        }

        private void OnClickOpenCustomRoomModal()
        {
            ProceduralAudioSynthesizer.Instance?.PlayButtonPop();
            panelCustomRoomModal.SetActive(true);
        }

        private void OnClickOpenFriendListModal()
        {
            ProceduralAudioSynthesizer.Instance?.PlayButtonPop();
            panelFriendListModal.SetActive(true);
            GameNetworkManager.Instance?.RequestFriendList();
        }

        private void OnClickCreateVIPRoom()
        {
            ProceduralAudioSynthesizer.Instance?.PlayButtonPop();
            GameNetworkManager.Instance?.CreateCustomRoom();
            string mockCode = "VIP-" + Random.Range(1000, 9999);
            txtCustomRoomDisplay.text = $"KODE ROOM: {mockCode}";
        }

        private void OnClickJoinVIPRoom()
        {
            string code = inputJoinCode.text.Trim().ToUpper();
            if (!string.IsNullOrEmpty(code))
            {
                ProceduralAudioSynthesizer.Instance?.PlayButtonPop();
                GameNetworkManager.Instance?.JoinCustomRoom(code);
                panelCustomRoomModal.SetActive(false);
                ShowInGameHUD();
            }
        }

        private void SubmitReaction(string reactionType)
        {
            ProceduralAudioSynthesizer.Instance?.PlayButtonPop();
            panelActionBarHUD.SetActive(false);
            GameNetworkManager.Instance?.SubmitReaction(reactionType);
        }

        // =========================================================================
        // HANDLER NETWORK EVENT
        // =========================================================================

        private void OnAuthResultReceived(bool success, string msg)
        {
            if (success)
            {
                txtTopBarProfile.text = $"👑 {GameNetworkManager.Instance.currentUsername} | 🏆 {GameNetworkManager.Instance.currentTrophies} Trofi | 🥇 {GameNetworkManager.Instance.currentRankTier}";
                ShowMainMenuLobby();
            }
            else
            {
                txtAuthStatus.text = msg;
            }
        }

        private void OnMatchFoundReceived(string roomId)
        {
            ShowInGameHUD();
        }

        private void OnCustomRoomCreatedReceived(string roomCode)
        {
            txtCustomRoomDisplay.text = $"KODE ROOM ANDA: {roomCode}";
        }

        private void OnReactionPromptReceived(ReactionPromptPayload prompt)
        {
            panelActionBarHUD.SetActive(true);
            ProceduralAudioSynthesizer.Instance?.PlayButtonPop();
        }

        private void OnRoundEndedReceived(RoundEndPayload roundEnd)
        {
            panelActionBarHUD.SetActive(false);
            panelVictoryModal.SetActive(true);
            ProceduralAudioSynthesizer.Instance?.PlayVictoryFanfare();
        }

        // =========================================================================
        // HELPER PEMBUATAN UI PROCEDURAL & FONT
        // =========================================================================

        private static Font GetSafeFont()
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (f == null)
            {
                try { f = Font.CreateDynamicFontFromOSFont("Arial", 24); } catch { }
            }
            if (f == null)
            {
                try { f = Font.CreateDynamicFontFromOSFont("Segoe UI", 24); } catch { }
            }
            if (f == null)
            {
                try { f = Font.CreateDynamicFontFromOSFont("Sans-Serif", 24); } catch { }
            }
            if (f == null)
            {
                Font[] allFonts = Resources.FindObjectsOfTypeAll<Font>();
                if (allFonts != null && allFonts.Length > 0) f = allFonts[0];
            }
            return f;
        }

        private GameObject CreatePanel(string name, Color color)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(transform, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = obj.GetComponent<Image>();
            img.color = color;
            return obj;
        }

        private GameObject CreateCard(Transform parent, Vector2 size, Vector2 anchoredPos)
        {
            GameObject card = new GameObject("Card_Glass", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(parent, false);
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            Image img = card.GetComponent<Image>();
            img.color = new Color(0.06f, 0.18f, 0.12f, 0.94f); // Emerald Dark Glass
            return card;
        }

        private Text CreateText(Transform parent, string content, int fontSize, FontStyle style, Color color, Vector2 anchoredPos, Vector2 size)
        {
            string safeName = "Text_" + (content.Length > 15 ? content.Substring(0, 15) : content);
            GameObject textObj = new GameObject(safeName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObj.transform.SetParent(parent, false);
            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Text txt = textObj.GetComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.font = GetSafeFont();
            return txt;
        }

        private InputField CreateInputField(Transform parent, string placeholder, Vector2 anchoredPos, Vector2 size, bool isPassword = false)
        {
            GameObject inputObj = new GameObject("Input_" + placeholder, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
            inputObj.transform.SetParent(parent, false);
            RectTransform rect = inputObj.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Image bg = inputObj.GetComponent<Image>();
            bg.color = new Color(0.03f, 0.08f, 0.05f, 0.9f);

            InputField input = inputObj.GetComponent<InputField>();

            GameObject phObj = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            phObj.transform.SetParent(inputObj.transform, false);
            RectTransform phRect = phObj.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero; phRect.anchorMax = Vector2.one;
            phRect.offsetMin = new Vector2(20, 0); phRect.offsetMax = new Vector2(-20, 0);
            Text phText = phObj.GetComponent<Text>();
            phText.text = placeholder;
            phText.fontSize = 22;
            phText.fontStyle = FontStyle.Italic;
            phText.color = new Color(0.6f, 0.7f, 0.6f, 0.6f);
            phText.alignment = TextAnchor.MiddleLeft;
            phText.horizontalOverflow = HorizontalWrapMode.Overflow;
            phText.verticalOverflow = VerticalWrapMode.Overflow;
            phText.font = GetSafeFont();

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            txtObj.transform.SetParent(inputObj.transform, false);
            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(20, 0); txtRect.offsetMax = new Vector2(-20, 0);
            Text text = txtObj.GetComponent<Text>();
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.font = GetSafeFont();

            input.textComponent = text;
            input.placeholder = phText;
            input.targetGraphic = bg;
            if (isPassword) input.contentType = InputField.ContentType.Password;

            return input;
        }

        private Button CreateButton(Transform parent, string label, Color btnColor, Vector2 anchoredPos, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject("Btn_" + label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Image img = btnObj.GetComponent<Image>();
            img.color = btnColor;

            Button btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.highlightedColor = btnColor * 1.2f;
            cb.pressedColor = btnColor * 0.8f;
            btn.colors = cb;
            if (onClick != null) btn.onClick.AddListener(onClick);

            GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero; txtRect.offsetMax = Vector2.zero;

            Text txt = txtObj.GetComponent<Text>();
            txt.text = label;
            txt.fontSize = 24;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.font = GetSafeFont();

            return btn;
        }
    }
}
