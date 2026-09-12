#if false
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mahjong.Network;
using Mahjong.Audio;

namespace Mahjong.UI
{
    /// <summary>
    /// GameUIManager: Manajer Antarmuka Pengguna (Canvas UI Manager).
    /// Mengontrol Top Bar HUD, Action Bar (Chow/Pong/Kong/Win), Panel Friend List,
    /// Modal Custom Room by Code, dan Layar Pengumuman Kemenangan (Victory Modal).
    /// </summary>
    public class GameUIManager : MonoBehaviour
    {
        public static GameUIManager Instance { get; private set; }

        [Header("Top Bar HUD")]
        public Text txtUsername;
        public Text txtTrophies;
        public Text txtRankTier;
        public Text txtRoundInfo; // "EAST 1"
        public Text txtWallTiles; // "Tiles Left: 72"

        [Header("Action Bar Panel (5-Second Reaction Window)")]
        public GameObject panelActionBar;
        public Button btnChow;
        public Button btnPong;
        public Button btnKong;
        public Button btnWin;
        public Button btnPass;

        [Header("Friend List & Custom Room Modals")]
        public GameObject panelFriendList;
        public Transform friendListContainer;
        public GameObject panelCustomRoom;
        public Text txtRoomCodeDisplay;
        public InputField inputJoinRoomCode;

        [Header("Victory / Round End Modal")]
        public GameObject panelVictory;
        public Text txtWinnerName;
        public Text txtWinType;
        public Text txtTotalScore;
        public Text txtSpecialHands;
        public Text txtTrophyDelta; // "+40 Trophies"

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            SetupButtonListeners();
        }

        private void Start()
        {
            // Subscribe ke event dari GameNetworkManager
            if (GameNetworkManager.Instance != null)
            {
                GameNetworkManager.Instance.OnAuthResult += HandleAuthResult;
                GameNetworkManager.Instance.OnReactionPromptReceived += HandleReactionPrompt;
                GameNetworkManager.Instance.OnRoundEnded += HandleRoundEnd;
                GameNetworkManager.Instance.OnCustomRoomCreated += HandleCustomRoomCreated;
                GameNetworkManager.Instance.OnFriendListUpdated += HandleFriendListUpdated;
            }

            HideAllPopups();
        }

        private void SetupButtonListeners()
        {
            if (btnChow) btnChow.onClick.AddListener(() => OnActionButtonClicked("chow"));
            if (btnPong) btnPong.onClick.AddListener(() => OnActionButtonClicked("pong"));
            if (btnKong) btnKong.onClick.AddListener(() => OnActionButtonClicked("kong"));
            if (btnWin)  btnWin.onClick.AddListener(() => OnActionButtonClicked("win"));
            if (btnPass) btnPass.onClick.AddListener(() => OnActionButtonClicked("pass"));
        }

        private void HideAllPopups()
        {
            if (panelActionBar) panelActionBar.SetActive(false);
            if (panelFriendList) panelFriendList.SetActive(false);
            if (panelCustomRoom) panelCustomRoom.SetActive(false);
            if (panelVictory) panelVictory.SetActive(false);
        }

        // =========================================================================
        // HANDLER EVENT DARI JARINGAN
        // =========================================================================

        private void HandleAuthResult(bool success, string message)
        {
            if (success)
            {
                if (txtUsername) txtUsername.text = GameNetworkManager.Instance.currentUsername;
                if (txtTrophies) txtTrophies.text = $"{GameNetworkManager.Instance.currentTrophies} 🏆";
                if (txtRankTier) txtRankTier.text = GameNetworkManager.Instance.currentRankTier;
            }
        }

        private void HandleReactionPrompt(ReactionPromptPayload prompt)
        {
            if (panelActionBar == null) return;

            // Aktifkan hanya tombol reaksi yang valid sesuai aturan permainan
            if (btnChow) btnChow.gameObject.SetActive(prompt.can_chow);
            if (btnPong) btnPong.gameObject.SetActive(prompt.can_pong);
            if (btnKong) btnKong.gameObject.SetActive(prompt.can_kong);
            if (btnWin)  btnWin.gameObject.SetActive(prompt.can_win);
            if (btnPass) btnPass.gameObject.SetActive(true);

            panelActionBar.SetActive(true);
            ProceduralAudioSynthesizer.Instance?.PlayButtonPop();
        }

        private void OnActionButtonClicked(string actionType)
        {
            if (panelActionBar) panelActionBar.SetActive(false);
            ProceduralAudioSynthesizer.Instance?.PlayButtonPop();
            GameNetworkManager.Instance?.SubmitReaction(actionType);
        }

        private void HandleCustomRoomCreated(string roomCode)
        {
            if (panelCustomRoom)
            {
                panelCustomRoom.SetActive(true);
                if (txtRoomCodeDisplay) txtRoomCodeDisplay.text = $"KODE ROOM: {roomCode}";
            }
        }

        private void HandleFriendListUpdated(List<FriendItemData> friends)
        {
            if (panelFriendList) panelFriendList.SetActive(true);
            Debug.Log($"[GameUIManager] Daftar Teman Diperbarui: {friends.Count} teman ditemukan.");
        }

        private void HandleRoundEnd(RoundEndPayload roundEnd)
        {
            if (panelActionBar) panelActionBar.SetActive(false);
            if (panelVictory == null) return;

            panelVictory.SetActive(true);
            ProceduralAudioSynthesizer.Instance?.PlayVictoryFanfare();

            if (txtWinnerName) txtWinnerName.text = $"PEMENANG: SEAT {roundEnd.winner_seat}";
            if (txtWinType) txtWinType.text = $"Tipe: {roundEnd.win_type}";
            if (txtTotalScore) txtTotalScore.text = $"Total Poin: +{roundEnd.total_pts}";

            if (txtSpecialHands && roundEnd.special_hands != null)
            {
                txtSpecialHands.text = string.Join(", ", roundEnd.special_hands);
            }

            if (txtTrophyDelta)
            {
                bool isLocalWinner = (roundEnd.winner_seat == GameNetworkManager.Instance.localSeatIndex);
                txtTrophyDelta.text = isLocalWinner ? "+40 Trofi 🏆" : "-10 Trofi 🔻";
                txtTrophyDelta.color = isLocalWinner ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
            }
        }

        // =========================================================================
        // METODE INTERAKSI MODAL (TOMBOL UI)
        // =========================================================================

        public void OpenCustomRoomModal()
        {
            if (panelCustomRoom) panelCustomRoom.SetActive(true);
            ProceduralAudioSynthesizer.Instance?.PlayButtonPop();
        }

        public void OpenFriendListModal()
        {
            GameNetworkManager.Instance?.RequestFriendList();
            ProceduralAudioSynthesizer.Instance?.PlayButtonPop();
        }

        public void CloseAllModals()
        {
            HideAllPopups();
            ProceduralAudioSynthesizer.Instance?.PlayButtonPop();
        }

        public void OnClickCreateVIPRoom()
        {
            GameNetworkManager.Instance?.CreateCustomRoom();
            ProceduralAudioSynthesizer.Instance?.PlayButtonPop();
        }

        public void OnClickJoinRoomByCode()
        {
            if (inputJoinRoomCode != null && !string.IsNullOrEmpty(inputJoinRoomCode.text))
            {
                GameNetworkManager.Instance?.JoinCustomRoom(inputJoinRoomCode.text.Trim().ToUpper());
                ProceduralAudioSynthesizer.Instance?.PlayButtonPop();
            }
        }
    }
}
#endif
