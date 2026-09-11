using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mahjong.Network
{
    /// <summary>
    /// NetworkAction: Action Code yang dipetakan 1:1 dengan Go Backend Server (protocol.go).
    /// </summary>
    public static class NetworkAction
    {
        // Otentikasi & Akun
        public const string REQ_AUTH_LOGIN       = "REQ_AUTH_LOGIN";
        public const string RES_AUTH_LOGIN       = "RES_AUTH_LOGIN";
        public const string REQ_AUTH_REGISTER    = "REQ_AUTH_REGISTER";
        public const string RES_AUTH_REGISTER    = "RES_AUTH_REGISTER";

        // Matchmaking & Custom Room
        public const string REQ_MATCH_QUEUE      = "REQ_MATCH_QUEUE";
        public const string RES_MATCH_FOUND      = "RES_MATCH_FOUND";
        public const string REQ_CREATE_CUSTOM    = "REQ_CREATE_CUSTOM";
        public const string RES_CREATE_CUSTOM    = "RES_CREATE_CUSTOM";
        public const string REQ_JOIN_CUSTOM      = "REQ_JOIN_CUSTOM";
        public const string RES_JOIN_CUSTOM      = "RES_JOIN_CUSTOM";

        // Friend List & Invites
        public const string REQ_FRIEND_LIST      = "REQ_FRIEND_LIST";
        public const string RES_FRIEND_LIST      = "RES_FRIEND_LIST";
        public const string REQ_FRIEND_INVITE    = "REQ_FRIEND_INVITE";
        public const string RES_FRIEND_INVITE    = "RES_FRIEND_INVITE";
        public const string NOTIF_INVITE_RECV    = "NOTIF_INVITE_RECV";

        // In-Game Events (Authoritative Server)
        public const string NOTIF_GAME_START     = "NOTIF_GAME_START";
        public const string NOTIF_DEAL_HANDS     = "NOTIF_DEAL_HANDS";
        public const string NOTIF_TURN_START     = "NOTIF_TURN_START";
        public const string REQ_DISCARD_TILE     = "REQ_DISCARD_TILE";
        public const string NOTIF_TILE_DISCARDED = "NOTIF_TILE_DISCARDED";
        public const string NOTIF_REACTION_PROMPT= "NOTIF_REACTION_PROMPT";
        public const string REQ_SUBMIT_REACTION  = "REQ_SUBMIT_REACTION";
        public const string NOTIF_ROUND_END      = "NOTIF_ROUND_END";
        public const string NOTIF_MATCH_END      = "NOTIF_MATCH_END";
        public const string REQ_PING             = "REQ_PING";
        public const string RES_PONG             = "RES_PONG";
    }

    /// <summary>
    /// Envelope Pesan JSON Standar Komunikasi WebSocket.
    /// </summary>
    [Serializable]
    public class NetworkEnvelope
    {
        public string action;
        public string session_token;
        public string room_id;
        public string payload; // Serialized JSON string atau JSON object
        public long timestamp;
    }

    // --- STRUKTUR DATA PAYLOAD SERIALIZATION ---

    [Serializable]
    public class TileData
    {
        public int id;
        public int suit;
        public int value;
        public bool is_bonus;
        public string name;
    }

    [Serializable]
    public class MeldData
    {
        public int type; // 0: Chow, 1: Pong, 2: Kong
        public List<TileData> tiles;
        public int source_player;
    }

    [Serializable]
    public class AuthRequestPayload
    {
        public string username;
        public string password;
    }

    [Serializable]
    public class AuthResponsePayload
    {
        public bool success;
        public string user_id;
        public string username;
        public string session_token;
        public int trophies;
        public string rank_tier;
        public string error_message;
    }

    [Serializable]
    public class MatchQueuePayload
    {
        public string game_mode; // "ranked" atau "casual"
    }

    [Serializable]
    public class CustomRoomPayload
    {
        public string room_code;
        public string room_id;
        public bool is_host;
    }

    [Serializable]
    public class FriendItemData
    {
        public string friend_id;
        public string friend_username;
        public string status; // "online_lobby", "in_game", "offline"
        public int trophies;
        public string rank_tier;
    }

    [Serializable]
    public class FriendListPayload
    {
        public List<FriendItemData> friends;
    }

    [Serializable]
    public class FriendInvitePayload
    {
        public string sender_id;
        public string sender_username;
        public string target_friend_id;
        public string room_code;
    }

    [Serializable]
    public class DealHandsPayload
    {
        public int seat_index;
        public List<TileData> hand_tiles;
        public List<TileData> bonus_tiles;
        public int remaining_wall_count;
    }

    [Serializable]
    public class TurnStartPayload
    {
        public int active_seat_index;
        public TileData drawn_tile; // null jika giliran lawan
        public int remaining_wall_count;
        public float turn_duration; // 15 detik
    }

    [Serializable]
    public class DiscardPayload
    {
        public int seat_index;
        public TileData tile;
    }

    [Serializable]
    public class ReactionPromptPayload
    {
        public int discarded_by_seat;
        public TileData tile;
        public bool can_chow;
        public bool can_pong;
        public bool can_kong;
        public bool can_win;
        public float window_duration; // 5 detik
    }

    [Serializable]
    public class SubmitReactionPayload
    {
        public string reaction_type; // "pass", "chow", "pong", "kong", "win"
        public List<int> tile_ids;
    }

    [Serializable]
    public class RoundEndPlayerScore
    {
        public int seat_index;
        public string username;
        public int round_score;
        public int total_score;
        public int bonus_count;
    }

    [Serializable]
    public class RoundEndPayload
    {
        public int winner_seat;
        public string win_type; // "Self-Draw", "Discard-Win", "Exhaustive-Draw"
        public int total_pts;
        public List<string> special_hands;
        public List<RoundEndPlayerScore> player_scores;
    }
}
