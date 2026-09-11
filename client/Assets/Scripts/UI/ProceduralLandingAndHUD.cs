using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mahjong.Network;
using Mahjong.Audio;

namespace Mahjong.UI
{
    /// <summary>
    /// ProceduralLandingAndHUD: Generator Antarmuka Otomatis (Landing Page, Login/Register Modal, 
    /// Main Menu Lobby, Custom Room Code Input, dan Friend List HUD).
    /// Menggunakan tema "Modern Luxury VIP Casino" dengan sentuhan emas dan emerald glassmorphism.
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

        // Komponen Input & Text
        private InputField inputUsername;
        private InputField inputPassword;
        private Text txtAuthStatus;
        private Text txtTopBarProfile;
        private Text txtCustomRoomDisplay;
        private InputField inputJoinCode;
        private Text txtVictoryDetails;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            SetupCanvas();
            BuildLandingLoginUI();
            BuildMainMenuLobbyUI();
            BuildCustomRoomModalUI();
            BuildFriendListModalUI();
            BuildActionBarHUD();
            BuildVictoryModalUI();
        }

        private void Start()
        {
            // Hubungkan dengan GameNetworkManager
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
            // Pastikan EventSystem aktif agar input keyboard & klik mouse/touch bekerja
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvasScaler = gameObject.GetComponent<CanvasScaler>();
            if (canvasScaler == null) canvasScaler = gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1080, 1920); // Portrait / Mobile Responsive
            canvasScaler.matchWidthOrHeight = 0.5f;

            graphicRaycaster = gameObject.GetComponent<GraphicRaycaster>();
            if (graphicRaycaster == null) graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
        }

        // =========================================================================
        // 1. LANDING PAGE & LOGIN / REGISTER SCREEN
        // =========================================================================

        private void BuildLandingLoginUI()
        {
            panelLandingLogin = CreatePanel("Panel_LandingLogin", new Color(0.04f, 0.12f, 0.08f, 0.92f));

            // Logo & Judul Game
            CreateText(panelLandingLogin.transform, "MAHJONG VIP 🀄", 48, FontStyle.Bold, new Color(1.0f, 0.84f, 0.0f), new Vector2(0, 380), new Vector2(800, 100));
            CreateText(panelLandingLogin.transform, "Casual Multiplayer • 4-Player 3D Board Game", 22, FontStyle.Italic, new Color(0.8f, 0.9f, 0.8f), new Vector2(0, 320), new Vector2(800, 60));

            // Box Card Form Login
            GameObject card = CreateCard(panelLandingLogin.transform, new Vector2(760, 580), new Vector2(0, -20));

            CreateText(card.transform, "MASUK / DAFTAR AKUN", 28, FontStyle.Bold, Color.white, new Vector2(0, 220), new Vector2(600, 60));

            // Input Fields
            inputUsername = CreateInputField(card.transform, "Username...", new Vector2(0, 120), new Vector2(620, 75));
            inputPassword = CreateInputField(card.transform, "Password...", new Vector2(0, 25), new Vector2(620, 75), true);

            // Status Error / Info Text
            txtAuthStatus = CreateText(card.transform, "Silakan login atau mainkan sebagai Tamu (Guest)", 18, FontStyle.Normal, new Color(0.9f, 0.8f, 0.4f), new Vector2(0, -50), new Vector2(620, 50));

            // Buttons
            CreateButton(card.transform, "LOGIN", new Color(0.1f, 0.55f, 0.25f), new Vector2(-160, -135), new Vector2(280, 75), OnClickLogin);
            CreateButton(card.transform, "REGISTER", new Color(0.2f, 0.4f, 0.7f), new Vector2(160, -135), new Vector2(280, 75), OnClickRegister);
            CreateButton(card.transform, "⚡ MAIN SEBAGAI GUEST", new Color(0.85f, 0.65f, 0.15f), new Vector2(0, -225), new Vector2(600, 70), OnClickGuestPlay);
        }

        // =========================================================================
        // 2. MAIN MENU LOBBY (AFTER LOGIN)
        // =========================================================================

        private void BuildMainMenuLobbyUI()
        {
            panelMainMenuLobby = CreatePanel("Panel_MainMenuLobby", Color.clear);

            // Top Bar Profile HUD
            GameObject topBar = CreateCard(panelMainMenuLobby.transform, new Vector2(1000, 110), new Vector2(0, 850));
            topBar.GetComponent<Image>().color = new Color(0.05f, 0.15f, 0.1f, 0.95f);
            txtTopBarProfile = CreateText(topBar.transform, "👑 Player_VIP | 🏆 1,000 Trofi | 🥇 Gold Master", 24, FontStyle.Bold, new Color(1.0f, 0.85f, 0.2f), new Vector2(-120, 0), new Vector2(700, 80));
            CreateButton(topBar.transform, "🚪 Logout", new Color(0.6f, 0.2f, 0.2f), new Vector2(380, 0), new Vector2(180, 65), ShowLandingScreen);

            // Main Menu Buttons Card (Tengah Bawah)
            GameObject menuCard = CreateCard(panelMainMenuLobby.transform, new Vector2(850, 580), new Vector2(0, -520));

            CreateText(menuCard.transform, "PILIH MODE PERMAINAN", 28, FontStyle.Bold, new Color(1f, 0.84f, 0f), new Vector2(0, 230), new Vector2(700, 60));

            CreateButton(menuCard.transform, "🏆 RANKED MATCHMAKING (ONLINE)", new Color(0.08f, 0.55f, 0.28f), new Vector2(0, 140), new Vector2(750, 80), OnClickRankedMatch);
            CreateButton(menuCard.transform, "🗝️ CUSTOM ROOM (KODE VIP)", new Color(0.15f, 0.45f, 0.75f), new Vector2(0, 45), new Vector2(750, 80), OnClickOpenCustomRoomModal);
            CreateButton(menuCard.transform, "👥 DAFTAR TEMAN (FRIEND LIST)", new Color(0.55f, 0.35f, 0.75f), new Vector2(0, -50), new Vector2(750, 80), OnClickOpenFriendListModal);
            CreateButton(menuCard.transform, "🤖 LATIHAN SOLO VS 3 BOT AI (OFFLINE)", new Color(0.85f, 0.45f, 0.15f), new Vector2(0, -145), new Vector2(750, 80), OnClickSoloAIMatch);
        }

        // =========================================================================
        // 3. CUSTOM ROOM MODAL (VIP CODE)
        // =========================================================================

        private void BuildCustomRoomModalUI()
        {
            panelCustomRoomModal = CreatePanel("Panel_CustomRoomModal", new Color(0, 0, 0, 0.8f));

            GameObject card = CreateCard(panelCustomRoomModal.transform, new Vector2(750, 650), Vector2.zero);
            CreateText(card.transform, "CUSTOM ROOM (VIP MABAR)", 30, FontStyle.Bold, new Color(1f, 0.84f, 0f), new Vector2(0, 250), new Vector2(650, 60));

            CreateButton(card.transform, "✨ BUAT ROOM BARU", new Color(0.1f, 0.6f, 0.3f), new Vector2(0, 140), new Vector2(580, 85), OnClickCreateVIPRoom);
            txtCustomRoomDisplay = CreateText(card.transform, "Kode Anda: Belum dibuat", 22, FontStyle.Bold, new Color(0.8f, 1f, 0.8f), new Vector2(0, 60), new Vector2(580, 50));

            CreateText(card.transform, "— ATAU GABUNG ROOM TEMAN —", 20, FontStyle.Italic, Color.gray, new Vector2(0, 0), new Vector2(580, 40));

            inputJoinCode = CreateInputField(card.transform, "Masukkan Kode (misal: VIP-8821)...", new Vector2(0, -70), new Vector2(580, 75));
            CreateButton(card.transform, "➡️ GABUNG ROOM", new Color(0.2f, 0.45f, 0.8f), new Vector2(0, -170), new Vector2(580, 80), OnClickJoinVIPRoom);

            CreateButton(card.transform, "❌ TUTUP", new Color(0.5f, 0.5f, 0.5f), new Vector2(0, -260), new Vector2(300, 60), () => panelCustomRoomModal.SetActive(false));
        }

        // =========================================================================
        // 4. FRIEND LIST MODAL
        // =========================================================================

        private void BuildFriendListModalUI()
        {
            panelFriendListModal = CreatePanel("Panel_FriendListModal", new Color(0, 0, 0, 0.8f));

            GameObject card = CreateCard(panelFriendListModal.transform, new Vector2(750, 750), Vector2.zero);
            CreateText(card.transform, "DAFTAR TEMAN (FRIEND LIST)", 30, FontStyle.Bold, new Color(1f, 0.84f, 0f), new Vector2(0, 310), new Vector2(650, 60));

            CreateText(card.transform, "Teman Online:", 22, FontStyle.Bold, Color.white, new Vector2(-220, 240), new Vector2(250, 50));

            // Contoh Daftar Teman Mockup
            CreateText(card.transform, "🟢 Alex_VIP (In Lobby) — 🏆 1,450 [UNDANG MABAR]", 20, FontStyle.Normal, new Color(0.7f, 1f, 0.7f), new Vector2(0, 160), new Vector2(650, 50));
            CreateText(card.transform, "🟢 Dewi_Mahjong (In Game) — 🏆 2,100", 20, FontStyle.Normal, new Color(0.9f, 0.9f, 0.5f), new Vector2(0, 100), new Vector2(650, 50));
            CreateText(card.transform, "⚪ Budi_Dragon (Offline)", 20, FontStyle.Normal, Color.gray, new Vector2(0, 40), new Vector2(650, 50));

            CreateButton(card.transform, "➕ TAMBAH TEMAN", new Color(0.15f, 0.5f, 0.75f), new Vector2(0, -180), new Vector2(550, 75), () => Debug.Log("Tambah Teman"));
            CreateButton(card.transform, "❌ TUTUP", new Color(0.5f, 0.5f, 0.5f), new Vector2(0, -280), new Vector2(300, 60), () => panelFriendListModal.SetActive(false));
        }

        // =========================================================================
        // 5. IN-GAME ACTION BAR HUD (CHOW, PONG, KONG, WIN, PASS)
        // =========================================================================

        private void BuildActionBarHUD()
        {
            panelActionBarHUD = CreatePanel("Panel_ActionBarHUD", Color.clear);
            GameObject barCard = CreateCard(panelActionBarHUD.transform, new Vector2(980, 110), new Vector2(0, -320));
            barCard.GetComponent<Image>().color = new Color(0.05f, 0.15f, 0.08f, 0.95f);

            CreateButton(barCard.transform, "CHOW (吃)", new Color(0.2f, 0.6f, 0.8f), new Vector2(-360, 0), new Vector2(170, 75), () => SubmitReaction("chow"));
            CreateButton(barCard.transform, "PONG (碰)", new Color(0.9f, 0.6f, 0.1f), new Vector2(-180, 0), new Vector2(170, 75), () => SubmitReaction("pong"));
            CreateButton(barCard.transform, "KONG (槓)", new Color(0.7f, 0.2f, 0.8f), new Vector2(0, 0), new Vector2(170, 75), () => SubmitReaction("kong"));
            CreateButton(barCard.transform, "WIN / HU (胡)", new Color(0.9f, 0.15f, 0.2f), new Vector2(180, 0), new Vector2(170, 75), () => SubmitReaction("win"));
            CreateButton(barCard.transform, "PASS (過)", new Color(0.4f, 0.4f, 0.4f), new Vector2(360, 0), new Vector2(150, 75), () => SubmitReaction("pass"));

            panelActionBarHUD.SetActive(false);
        }

        // =========================================================================
        // 6. VICTORY / ROUND END MODAL
        // =========================================================================

        private void BuildVictoryModalUI()
        {
            panelVictoryModal = CreatePanel("Panel_VictoryModal", new Color(0, 0, 0, 0.85f));

            GameObject card = CreateCard(panelVictoryModal.transform, new Vector2(800, 680), Vector2.zero);
            CreateText(card.transform, "🏆 RONDE SELESAI 🏆", 34, FontStyle.Bold, new Color(1f, 0.85f, 0f), new Vector2(0, 260), new Vector2(700, 70));

            txtVictoryDetails = CreateText(card.transform, "PEMENANG: SEAT SOUTH (ANDA)\n\n• Base Win: +100 Poin\n• Self-Draw (Zimo): +30 Poin\n• Special Hand (All Triplets): +50 Poin\n\nTotal: +180 Poin | Perolehan: +40 Trofi 🏆", 22, FontStyle.Normal, Color.white, new Vector2(0, 30), new Vector2(700, 320));

            CreateButton(card.transform, "LANJUT KE LOBBY", new Color(0.1f, 0.65f, 0.3f), new Vector2(0, -240), new Vector2(500, 85), ShowMainMenuLobby);

            panelVictoryModal.SetActive(false);
        }

        // =========================================================================
        // LOGIKA TOMBOL & NAVIGASI
        // =========================================================================

        public void ShowLandingScreen()
        {
            panelLandingLogin.SetActive(true);
            panelMainMenuLobby.SetActive(false);
            panelCustomRoomModal.SetActive(false);
            panelFriendListModal.SetActive(false);
            panelActionBarHUD.SetActive(false);
            panelVictoryModal.SetActive(false);
        }

        public void ShowMainMenuLobby()
        {
            panelLandingLogin.SetActive(false);
            panelMainMenuLobby.SetActive(true);
            panelCustomRoomModal.SetActive(false);
            panelFriendListModal.SetActive(false);
            panelActionBarHUD.SetActive(false);
            panelVictoryModal.SetActive(false);
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
            panelActionBarHUD.SetActive(false);
            Debug.Log("[Lobby] Masuk ke antrean Ranked Matchmaking...");
            GameNetworkManager.Instance?.JoinRankedQueue();
        }

        private void OnClickSoloAIMatch()
        {
            ProceduralAudioSynthesizer.Instance?.PlayButtonPop();
            panelMainMenuLobby.SetActive(false);
            panelLandingLogin.SetActive(false);
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
                panelMainMenuLobby.SetActive(false);
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
            panelMainMenuLobby.SetActive(false);
            panelLandingLogin.SetActive(false);
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
        // HELPER PEMBUATAN UI PROCEDURAL
        // =========================================================================

        private GameObject CreatePanel(string name, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }

        private GameObject CreateCard(Transform parent, Vector2 size, Vector2 anchoredPos)
        {
            GameObject card = new GameObject("Card_Glass");
            card.transform.SetParent(parent, false);
            RectTransform rect = card.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            Image img = card.AddComponent<Image>();
            img.color = new Color(0.06f, 0.18f, 0.12f, 0.94f); // Emerald Dark Glass
            return card;
        }

        private Text CreateText(Transform parent, string content, int fontSize, FontStyle style, Color color, Vector2 anchoredPos, Vector2 size)
        {
            GameObject textObj = new GameObject("Text_" + content);
            textObj.transform.SetParent(parent, false);
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Text txt = textObj.AddComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return txt;
        }

        private InputField CreateInputField(Transform parent, string placeholder, Vector2 anchoredPos, Vector2 size, bool isPassword = false)
        {
            GameObject inputObj = new GameObject("Input_" + placeholder);
            inputObj.transform.SetParent(parent, false);
            RectTransform rect = inputObj.AddComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Image bg = inputObj.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.08f, 0.05f, 0.9f);

            InputField input = inputObj.AddComponent<InputField>();

            // Text placeholder
            GameObject phObj = new GameObject("Placeholder");
            phObj.transform.SetParent(inputObj.transform, false);
            RectTransform phRect = phObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero; phRect.anchorMax = Vector2.one;
            phRect.offsetMin = new Vector2(20, 0); phRect.offsetMax = new Vector2(-20, 0);
            Text phText = phObj.AddComponent<Text>();
            phText.text = placeholder;
            phText.fontSize = 22;
            phText.fontStyle = FontStyle.Italic;
            phText.color = new Color(0.6f, 0.7f, 0.6f, 0.6f);
            phText.alignment = TextAnchor.MiddleLeft;
            phText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (phText.font == null) phText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Text input
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(inputObj.transform, false);
            RectTransform txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(20, 0); txtRect.offsetMax = new Vector2(-20, 0);
            Text text = txtObj.AddComponent<Text>();
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            input.textComponent = text;
            input.placeholder = phText;
            if (isPassword) input.contentType = InputField.ContentType.Password;

            return input;
        }

        private Button CreateButton(Transform parent, string label, Color btnColor, Vector2 anchoredPos, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject("Btn_" + label);
            btnObj.transform.SetParent(parent, false);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.color = btnColor;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = btnColor * 1.2f;
            cb.pressedColor = btnColor * 0.8f;
            btn.colors = cb;
            if (onClick != null) btn.onClick.AddListener(onClick);

            // Label Text
            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero; txtRect.offsetMax = Vector2.zero;

            Text txt = txtObj.AddComponent<Text>();
            txt.text = label;
            txt.fontSize = 24;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            return btn;
        }
    }
}
