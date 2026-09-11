using System;
using System.Collections.Generic;
using UnityEngine;
using Mahjong.Procedural;

namespace Mahjong.Network
{
    /// <summary>
    /// GameNetworkManager: Manajer Jaringan Utama Unity (Singleton).
    /// Mengorkestrasi otentikasi akun, antrean Ranked Matchmaking, Custom Room by Code,
    /// sinkronisasi Friend List, serta alur permainan in-game (Deal, Turn, Discard, Reaction, Win).
    /// </summary>
    public class GameNetworkManager : MonoBehaviour
    {
        public static GameNetworkManager Instance { get; private set; }

        [Header("Konfigurasi Server")]
        [Tooltip("URL WebSocket Backend Go (Koyeb atau Localhost)")]
        public string serverWebSocketUrl = "ws://localhost:8080/ws";

        [Header("State Pemain Aktif")]
        public string currentUserId;
        public string currentUsername;
        public string sessionToken;
        public int currentTrophies;
        public string currentRankTier;
        public string activeRoomId;
        public int localSeatIndex = 0; // 0: South (Player Bawah)

        [Header("Referensi Objek Meja 3D")]
        public ProceduralTable gameTable;
        public TableCompass tableCompass;

        // Event C# untuk UI Listener
        public event Action<bool, string> OnAuthResult;
        public event Action<string> OnMatchFound;
        public event Action<string> OnCustomRoomCreated;
        public event Action<List<FriendItemData>> OnFriendListUpdated;
        public event Action<FriendInvitePayload> OnInviteReceived;
        public event Action<DealHandsPayload> OnHandsDealt;
        public event Action<TurnStartPayload> OnTurnStarted;
        public event Action<DiscardPayload> OnTileDiscarded;
        public event Action<ReactionPromptPayload> OnReactionPromptReceived;
        public event Action<RoundEndPayload> OnRoundEnded;

        private WebSocketClient wsClient;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeNetwork();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeNetwork()
        {
            wsClient = new WebSocketClient();
            wsClient.OnConnected += HandleConnected;
            wsClient.OnDisconnected += HandleDisconnected;
            wsClient.OnRawMessageReceived += HandleRawMessage;
            wsClient.OnError += HandleError;

            // Pastikan MainThreadDispatcher aktif
            _ = UnityMainThreadDispatcher.Instance;
        }

        private async void Start()
        {
            await wsClient.ConnectAsync(serverWebSocketUrl);
        }

        private void HandleConnected()
        {
            Debug.Log("[GameNetworkManager] Terhubung ke Server Mahjong!");
        }

        private void HandleDisconnected(string reason)
        {
            Debug.LogWarning($"[GameNetworkManager] Terputus dari server: {reason}");
        }

        private void HandleError(Exception ex)
        {
            Debug.LogError($"[GameNetworkManager] Jaringan Error: {ex.Message}");
        }

        // =========================================================================
        // PENGIRIMAN PESAN KE SERVER (OUTGOING CLIENT REQUESTS)
        // =========================================================================

        public async void Login(string username, string password)
        {
            var payload = new AuthRequestPayload { username = username, password = password };
            await SendEnvelope(NetworkAction.REQ_AUTH_LOGIN, JsonUtility.ToJson(payload));
        }

        public async void Register(string username, string password)
        {
            var payload = new AuthRequestPayload { username = username, password = password };
            await SendEnvelope(NetworkAction.REQ_AUTH_REGISTER, JsonUtility.ToJson(payload));
        }

        public async void JoinRankedQueue()
        {
            var payload = new MatchQueuePayload { game_mode = "ranked" };
            await SendEnvelope(NetworkAction.REQ_MATCH_QUEUE, JsonUtility.ToJson(payload));
        }

        public async void CreateCustomRoom()
        {
            await SendEnvelope(NetworkAction.REQ_CREATE_CUSTOM, "{}");
        }

        public async void JoinCustomRoom(string roomCode)
        {
            var payload = new CustomRoomPayload { room_code = roomCode };
            await SendEnvelope(NetworkAction.REQ_JOIN_CUSTOM, JsonUtility.ToJson(payload));
        }

        public async void RequestFriendList()
        {
            await SendEnvelope(NetworkAction.REQ_FRIEND_LIST, "{}");
        }

        public async void SendFriendInvite(string targetFriendId, string roomCode)
        {
            var payload = new FriendInvitePayload
            {
                sender_id = currentUserId,
                sender_username = currentUsername,
                target_friend_id = targetFriendId,
                room_code = roomCode
            };
            await SendEnvelope(NetworkAction.REQ_FRIEND_INVITE, JsonUtility.ToJson(payload));
        }

        public async void DiscardTile(int tileId)
        {
            var payload = new DiscardPayload { seat_index = localSeatIndex, tile = new TileData { id = tileId } };
            await SendEnvelope(NetworkAction.REQ_DISCARD_TILE, JsonUtility.ToJson(payload));
        }

        public async void SubmitReaction(string reactionType, List<int> tileIds = null)
        {
            var payload = new SubmitReactionPayload { reaction_type = reactionType, tile_ids = tileIds ?? new List<int>() };
            await SendEnvelope(NetworkAction.REQ_SUBMIT_REACTION, JsonUtility.ToJson(payload));
        }

        private async System.Threading.Tasks.Task SendEnvelope(string action, string jsonPayload)
        {
            NetworkEnvelope env = new NetworkEnvelope
            {
                action = action,
                session_token = sessionToken,
                room_id = activeRoomId,
                payload = jsonPayload,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            string serialized = JsonUtility.ToJson(env);
            await wsClient.SendAsync(serialized);
        }

        // =========================================================================
        // PENERIMAAN DAN DISPATCH PESAN DARI SERVER (INCOMING IN-GAME EVENTS)
        // =========================================================================

        private void HandleRawMessage(string rawJson)
        {
            try
            {
                NetworkEnvelope env = JsonUtility.FromJson<NetworkEnvelope>(rawJson);
                if (env == null) return;

                switch (env.action)
                {
                    case NetworkAction.RES_AUTH_LOGIN:
                    case NetworkAction.RES_AUTH_REGISTER:
                        var authRes = JsonUtility.FromJson<AuthResponsePayload>(env.payload);
                        if (authRes.success)
                        {
                            currentUserId = authRes.user_id;
                            currentUsername = authRes.username;
                            sessionToken = authRes.session_token;
                            currentTrophies = authRes.trophies;
                            currentRankTier = authRes.rank_tier;
                            OnAuthResult?.Invoke(true, $"Selamat datang, {currentUsername}!");
                        }
                        else
                        {
                            OnAuthResult?.Invoke(false, authRes.error_message);
                        }
                        break;

                    case NetworkAction.RES_MATCH_FOUND:
                        activeRoomId = env.room_id;
                        OnMatchFound?.Invoke(activeRoomId);
                        break;

                    case NetworkAction.RES_CREATE_CUSTOM:
                        var customData = JsonUtility.FromJson<CustomRoomPayload>(env.payload);
                        activeRoomId = customData.room_id;
                        OnCustomRoomCreated?.Invoke(customData.room_code);
                        break;

                    case NetworkAction.RES_FRIEND_LIST:
                        var friendListData = JsonUtility.FromJson<FriendListPayload>(env.payload);
                        OnFriendListUpdated?.Invoke(friendListData.friends);
                        break;

                    case NetworkAction.NOTIF_INVITE_RECV:
                        var inviteData = JsonUtility.FromJson<FriendInvitePayload>(env.payload);
                        OnInviteReceived?.Invoke(inviteData);
                        break;

                    case NetworkAction.NOTIF_DEAL_HANDS:
                        var dealData = JsonUtility.FromJson<DealHandsPayload>(env.payload);
                        localSeatIndex = dealData.seat_index;
                        OnHandsDealt?.Invoke(dealData);
                        break;

                    case NetworkAction.NOTIF_TURN_START:
                        var turnData = JsonUtility.FromJson<TurnStartPayload>(env.payload);
                        if (tableCompass != null)
                        {
                            tableCompass.StartTurnTimer(turnData.active_seat_index, turnData.turn_duration);
                        }
                        OnTurnStarted?.Invoke(turnData);
                        break;

                    case NetworkAction.NOTIF_TILE_DISCARDED:
                        var discardData = JsonUtility.FromJson<DiscardPayload>(env.payload);
                        OnTileDiscarded?.Invoke(discardData);
                        break;

                    case NetworkAction.NOTIF_REACTION_PROMPT:
                        var reactionPrompt = JsonUtility.FromJson<ReactionPromptPayload>(env.payload);
                        OnReactionPromptReceived?.Invoke(reactionPrompt);
                        break;

                    case NetworkAction.NOTIF_ROUND_END:
                        if (tableCompass != null) tableCompass.StopTimer();
                        var roundEndData = JsonUtility.FromJson<RoundEndPayload>(env.payload);
                        OnRoundEnded?.Invoke(roundEndData);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameNetworkManager] Gagal parsing pesan server: {ex.Message}\nRaw: {rawJson}");
            }
        }

        private async void OnApplicationQuit()
        {
            if (wsClient != null)
            {
                await wsClient.DisconnectAsync();
            }
        }
    }
}
