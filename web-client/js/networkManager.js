/**
 * networkManager.js - Klien Jaringan Dual-Layer (BroadcastChannel & WebSocket)
 * Memungkinkan sinkronisasi instan antar-tab / antar-perangkat secara real-time.
 */

export class NetworkManager {
    constructor() {
        this.ws = null;
        this.isConnected = false;
        this.serverUrl = "ws://localhost:8080/ws";
        this.username = "Player";
        this.userId = "uid_" + Math.floor(1000 + Math.random() * 9000);
        this.roomCode = "";
        this.channel = null;

        // Callbacks
        this.onConnected = null;
        this.onDisconnected = null;
        this.onCustomRoomCreated = null;
        this.onPlayerJoin = null;
        this.onPlayerLeave = null;
        this.onRoomStateSync = null;
        this.onGameStart = null;
        this.onTileDiscarded = null;
        this.onPlayerDraw = null;
        this.onActionPrompt = null;
        this.onActionClaimed = null;
        this.onRoundEnd = null;
        this.onError = null;
    }

    initRoomChannel(roomCode, username) {
        this.roomCode = roomCode;
        this.username = username;

        if (this.channel) {
            this.channel.close();
            this.channel = null;
        }

        if (typeof BroadcastChannel !== "undefined") {
            try {
                this.channel = new BroadcastChannel(`mahjong_vip_${roomCode}`);
                this.channel.onmessage = (event) => {
                    const { senderId, type, payload } = event.data;
                    if (senderId === this.userId) return; // Ignore own messages
                    this.routeMessage(type, payload);
                };
                console.log(`[Network] BroadcastChannel aktif untuk room: ${roomCode}`);
            } catch (e) {
                console.warn("[Network] BroadcastChannel tidak didukung:", e);
            }
        }
    }

    getDefaultWsUrl() {
        if (typeof window !== "undefined") {
            if (window.location.origin.includes(":3000")) {
                return "ws://localhost:8080/ws";
            }
            const proto = window.location.protocol === "https:" ? "wss:" : "ws:";
            return `${proto}//${window.location.host}/ws`;
        }
        return "ws://localhost:8080/ws";
    }

    connect(serverUrl = null, username = "Player") {
        this.serverUrl = serverUrl || this.getDefaultWsUrl();
        this.username = username;

        const fullUrl = `${this.serverUrl}?username=${encodeURIComponent(this.username)}&user_id=${this.userId}`;

        try {
            this.ws = new WebSocket(fullUrl);

            this.ws.onopen = () => {
                this.isConnected = true;
                console.log("[Network] Terhubung ke WebSocket Server:", fullUrl);
                if (this.onConnected) this.onConnected();
            };

            this.ws.onmessage = (event) => {
                try {
                    const envelope = JSON.parse(event.data);
                    const msgType = envelope.type;
                    let payload = {};
                    if (envelope.payload) {
                        payload = typeof envelope.payload === 'string' ? JSON.parse(envelope.payload) : envelope.payload;
                    }
                    this.routeMessage(msgType, payload);
                } catch (e) {
                    console.error("[Network] Gagal parse WS message:", e);
                }
            };

            this.ws.onclose = () => {
                this.isConnected = false;
                console.log("[Network] Terputus dari WebSocket Server");
                if (this.onDisconnected) this.onDisconnected();
            };

            this.ws.onerror = (err) => {
                if (this.onError) this.onError(err);
            };
        } catch (e) {
            if (this.onError) this.onError(e);
        }
    }

    broadcast(type, payload = {}) {
        // 1. Kirim via BroadcastChannel
        if (this.channel) {
            this.channel.postMessage({
                senderId: this.userId,
                type,
                payload
            });
        }

        // 2. Kirim via WebSocket jika terhubung
        if (this.isConnected && this.ws) {
            const envelope = {
                type,
                payload: JSON.stringify(payload)
            };
            this.ws.send(JSON.stringify(envelope));
        }
    }

    routeMessage(type, payload) {
        console.log(`[Network] Terima event: ${type}`, payload);

        switch (type) {
            case "PLAYER_JOIN":
                if (this.onPlayerJoin) this.onPlayerJoin(payload);
                break;
            case "PLAYER_LEAVE":
                if (this.onPlayerLeave) this.onPlayerLeave(payload);
                break;
            case "ROOM_STATE_SYNC":
                if (this.onRoomStateSync) this.onRoomStateSync(payload);
                break;
            case "GAME_START":
                if (this.onGameStart) this.onGameStart(payload);
                break;
            case "TILE_DISCARDED":
                if (this.onTileDiscarded) this.onTileDiscarded(payload);
                break;
            case "PLAYER_DRAW":
                if (this.onPlayerDraw) this.onPlayerDraw(payload);
                break;
            case "ACTION_CLAIMED":
                if (this.onActionClaimed) this.onActionClaimed(payload);
                break;
            case "ACTION_PROMPT":
                if (this.onActionPrompt) this.onActionPrompt(payload);
                break;
            case "ROUND_END":
                if (this.onRoundEnd) this.onRoundEnd(payload);
                break;
            case "CUSTOM_ROOM_CREATED":
                if (this.onCustomRoomCreated) this.onCustomRoomCreated(payload);
                break;
            case "ERROR":
                if (this.onError) this.onError(payload.message || "Error server");
                break;
        }
    }

    disconnect() {
        if (this.channel) {
            this.broadcast("PLAYER_LEAVE", { userId: this.userId, username: this.username });
            this.channel.close();
            this.channel = null;
        }
        if (this.ws) {
            this.ws.close();
            this.ws = null;
            this.isConnected = false;
        }
    }
}
