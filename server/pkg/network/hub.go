package network

import (
	"encoding/json"
	"fmt"
	"log"
	"mahjong-server/pkg/engine"
	"math/rand"
	"sync"
)

// Hub mengelola seluruh koneksi WebSocket aktif, routing pesan, Ranked Queue, Custom Rooms, dan Invite Friends.
type Hub struct {
	clients       map[*Client]bool
	clientsByUser map[string]*Client      // Key: UserID (untuk kirim notifikasi & invite teman)
	rooms         map[string]*engine.Room // Key: RoomID
	customRooms   map[string]*engine.Room // Key: RoomCode (misal: "VIP-8821")
	roomModes     map[string]GameMode     // Key: RoomID -> RANKED / CUSTOM
	roomClients   map[string][]*Client    // Key: RoomID
	mu            sync.RWMutex
}

// NewHub membuat instansiasi Hub baru.
func NewHub() *Hub {
	return &Hub{
		clients:       make(map[*Client]bool),
		clientsByUser: make(map[string]*Client),
		rooms:         make(map[string]*engine.Room),
		customRooms:   make(map[string]*engine.Room),
		roomModes:     make(map[string]GameMode),
		roomClients:   make(map[string][]*Client),
	}
}

// RegisterClient mendaftarkan koneksi baru ke dalam Hub.
func (h *Hub) RegisterClient(c *Client) {
	h.mu.Lock()
	defer h.mu.Unlock()
	h.clients[c] = true
	h.clientsByUser[c.Player.ID] = c
	log.Printf("[Hub] Client terhubung: %s (ID: %s, Total Client: %d)", c.Player.Username, c.Player.ID, len(h.clients))
}

// UnregisterClient menghapus client yang terputus dari Hub.
func (h *Hub) UnregisterClient(c *Client) {
	h.mu.Lock()
	defer h.mu.Unlock()

	if _, ok := h.clients[c]; ok {
		delete(h.clients, c)
		delete(h.clientsByUser, c.Player.ID)
		close(c.SendChan)
		log.Printf("[Hub] Client terputus: %s (Sisa Client: %d)", c.Player.Username, len(h.clients))
	}
}

// BroadcastToRoom mengirim pesan ke seluruh pemain yang berada di meja yang sama.
func (h *Hub) BroadcastToRoom(roomID string, msg []byte) {
	h.mu.RLock()
	clients := h.roomClients[roomID]
	h.mu.RUnlock()

	for _, c := range clients {
		c.SendMessage(msg)
	}
}

// JoinRankedQueue memasukkan pemain ke dalam antrian Ranked Matchmaking otomatis.
func (h *Hub) JoinRankedQueue(c *Client) (*engine.Room, error) {
	h.mu.Lock()
	defer h.mu.Unlock()

	// Cari room Ranked publik yang belum penuh
	var targetRoom *engine.Room
	for roomID, r := range h.rooms {
		if h.roomModes[roomID] == GameModeRanked && !r.IsFull() && r.Phase == engine.PhaseLobby {
			targetRoom = r
			break
		}
	}

	// Jika tidak ada room terbuka, buat room Ranked baru
	if targetRoom == nil {
		roomID := fmt.Sprintf("RANKED-%d", len(h.rooms)+1)
		targetRoom = engine.NewRoom(roomID)
		h.rooms[roomID] = targetRoom
		h.roomModes[roomID] = GameModeRanked
		h.roomClients[roomID] = make([]*Client, 0, 4)
	}

	return h.assignSeatAndJoin(c, targetRoom)
}

// JoinOrCreateRoom adalah alias untuk JoinRankedQueue demi kompatibilitas endpoint HTTP/WS.
func (h *Hub) JoinOrCreateRoom(c *Client) (*engine.Room, error) {
	return h.JoinRankedQueue(c)
}

// CreateCustomRoom membuat meja khusus baru dengan kode unik (misal: "VIP-8821") untuk mabar bersama teman.
func (h *Hub) CreateCustomRoom(c *Client, requestedCode string) (*engine.Room, string, error) {
	h.mu.Lock()
	defer h.mu.Unlock()

	roomCode := requestedCode
	if roomCode == "" {
		roomCode = fmt.Sprintf("VIP-%04d", rand.Intn(10000))
	}

	roomID := fmt.Sprintf("CUSTOM-%s", roomCode)
	targetRoom := engine.NewRoom(roomID)

	h.rooms[roomID] = targetRoom
	h.customRooms[roomCode] = targetRoom
	h.roomModes[roomID] = GameModeCustom
	h.roomClients[roomID] = make([]*Client, 0, 4)

	room, err := h.assignSeatAndJoin(c, targetRoom)
	if err != nil {
		return nil, "", err
	}

	log.Printf("[Hub] Custom Room dibuat: Kode %s (RoomID: %s) oleh %s", roomCode, roomID, c.Player.Username)
	return room, roomCode, nil
}

// JoinCustomRoom memasukkan pemain ke dalam meja custom berdasarkan kode room yang dimasukkan.
func (h *Hub) JoinCustomRoom(c *Client, roomCode string) (*engine.Room, error) {
	h.mu.Lock()
	defer h.mu.Unlock()

	targetRoom, exists := h.customRooms[roomCode]
	if !exists {
		return nil, fmt.Errorf("kode meja '%s' tidak ditemukan", roomCode)
	}

	if targetRoom.IsFull() {
		return nil, fmt.Errorf("meja '%s' sudah penuh (4 pemain)", roomCode)
	}

	if targetRoom.Phase != engine.PhaseLobby {
		return nil, fmt.Errorf("permainan di meja '%s' sudah dimulai", roomCode)
	}

	return h.assignSeatAndJoin(c, targetRoom)
}

// SendFriendInvite mengirimkan undangan mabar dari Host ke HP teman yang sedang online.
func (h *Hub) SendFriendInvite(fromUser *Client, friendUserID, roomCode string) error {
	h.mu.RLock()
	friendClient, exists := h.clientsByUser[friendUserID]
	h.mu.RUnlock()

	if !exists {
		return fmt.Errorf("teman sedang offline atau tidak terhubung")
	}

	invitePayload := FriendInviteReceivedPayload{
		FromUsername: fromUser.Player.Username,
		RoomCode:     roomCode,
	}

	msgBytes, err := EncodeMessage("friend_invite_received", invitePayload)
	if err != nil {
		return err
	}

	friendClient.SendMessage(msgBytes)
	log.Printf("[Hub] Undangan mabar terkirim dari %s ke UserID %s (Kode: %s)", fromUser.Player.Username, friendUserID, roomCode)
	return nil
}

func (h *Hub) assignSeatAndJoin(c *Client, targetRoom *engine.Room) (*engine.Room, error) {
	var assignedSeat engine.PlayerSeat = -1
	for seat := engine.SeatEast; seat <= engine.SeatNorth; seat++ {
		if targetRoom.Players[seat] == nil {
			assignedSeat = seat
			break
		}
	}

	if assignedSeat == -1 {
		return nil, fmt.Errorf("kursi di room %s sudah penuh", targetRoom.ID)
	}

	targetRoom.AddPlayer(c.Player, assignedSeat)
	c.RoomID = targetRoom.ID
	h.roomClients[targetRoom.ID] = append(h.roomClients[targetRoom.ID], c)

	log.Printf("[Hub] Pemain %s (%s) duduk di %s pada %s", c.Player.Username, c.Player.ID, assignedSeat, targetRoom.ID)

	// Jika meja sudah penuh 4 pemain, mulai match!
	if targetRoom.IsFull() {
		mode := h.roomModes[targetRoom.ID]
		log.Printf("[Hub] Meja %s PENUH! Memulai Match 4 Ronde (Mode: %s)...", targetRoom.ID, mode)
		go h.StartRoomMatch(targetRoom, mode)
	}

	return targetRoom, nil
}

// StartRoomMatch memulai pertandingan dan broadcast state awal ke 4 pemain.
func (h *Hub) StartRoomMatch(room *engine.Room, mode GameMode) {
	if err := room.StartMatch(); err != nil {
		log.Printf("[Hub] Gagal memulai match: %v", err)
		return
	}

	h.mu.RLock()
	clients := h.roomClients[room.ID]
	h.mu.RUnlock()

	var playersInfo []PlayerInfo
	for _, p := range room.Players {
		playersInfo = append(playersInfo, PlayerInfo{
			Seat:      p.Seat,
			Username:  p.Username,
			Score:     p.MatchScore,
			TileCount: len(p.Hand),
		})
	}

	for _, c := range clients {
		payload := GameStartPayload{
			RoomID:             room.ID,
			GameMode:           mode,
			RoundNumber:        room.CurrentRound,
			MySeat:             c.Player.Seat,
			DealerSeat:         room.DealerSeat,
			RemainingWallCount: room.Wall.TotalRemainingTiles(),
			MyHand:             c.Player.Hand,
			Players:            playersInfo,
		}

		msgBytes, _ := EncodeMessage("game_start", payload)
		c.SendMessage(msgBytes)
	}
}

// HandleIncomingMessage memproses aksi pesan masuk dari client.
func (h *Hub) HandleIncomingMessage(c *Client, msg Message) {
	switch msg.Type {
	case "create_custom_room":
		var payload CreateCustomRoomPayload
		_ = json.Unmarshal(msg.Payload, &payload)
		room, code, err := h.CreateCustomRoom(c, payload.RoomCode)
		if err != nil {
			errMsg, _ := EncodeMessage("error", ErrorPayload{Message: err.Error()})
			c.SendMessage(errMsg)
			return
		}
		respBytes, _ := EncodeMessage("custom_room_created", CustomRoomCreatedPayload{
			RoomCode: code,
			RoomID:   room.ID,
		})
		c.SendMessage(respBytes)

	case "join_custom_room":
		var payload JoinCustomRoomPayload
		if err := json.Unmarshal(msg.Payload, &payload); err != nil {
			return
		}
		_, err := h.JoinCustomRoom(c, payload.RoomCode)
		if err != nil {
			errMsg, _ := EncodeMessage("error", ErrorPayload{Message: err.Error()})
			c.SendMessage(errMsg)
			return
		}

	case "invite_friend":
		var payload InviteFriendPayload
		if err := json.Unmarshal(msg.Payload, &payload); err != nil {
			return
		}
		if err := h.SendFriendInvite(c, payload.FriendUserID, payload.RoomCode); err != nil {
			errMsg, _ := EncodeMessage("error", ErrorPayload{Message: err.Error()})
			c.SendMessage(errMsg)
		}

	case "action_discard":
		h.mu.RLock()
		room := h.rooms[c.RoomID]
		h.mu.RUnlock()
		if room == nil {
			return
		}

		var payload ActionDiscardPayload
		if err := json.Unmarshal(msg.Payload, &payload); err != nil {
			return
		}

		discarded, availableActions, err := room.ExecuteDiscard(c.Player.Seat, payload.TileID)
		if err != nil {
			errMsg, _ := EncodeMessage("error", ErrorPayload{Message: err.Error()})
			c.SendMessage(errMsg)
			return
		}

		tileDiscardedMsg, _ := EncodeMessage("tile_discarded", TileDiscardedPayload{
			Seat: c.Player.Seat,
			Tile: discarded,
		})
		h.BroadcastToRoom(room.ID, tileDiscardedMsg)

		if len(availableActions) > 0 {
			h.mu.RLock()
			clients := h.roomClients[room.ID]
			h.mu.RUnlock()

			for _, client := range clients {
				if actions, ok := availableActions[client.Player.Seat]; ok {
					promptMsg, _ := EncodeMessage("action_prompt", ActionPromptPayload{
						TargetTile:       discarded,
						FromSeat:         c.Player.Seat,
						AvailableActions: actions,
						TimeoutSeconds:   engine.ReactionTimeoutSeconds,
					})
					client.SendMessage(promptMsg)
				}
			}
		}

	case "claim_action":
		h.mu.RLock()
		room := h.rooms[c.RoomID]
		h.mu.RUnlock()
		if room == nil {
			return
		}

		var payload ClaimActionPayload
		if err := json.Unmarshal(msg.Payload, &payload); err != nil {
			return
		}

		room.SubmitActionClaim(c.Player.Seat, engine.ActionClaim{
			Seat:      c.Player.Seat,
			Action:    payload.Action,
			MeldTiles: payload.MeldTiles,
		})

		approvedClaim, ok := room.ResolveActionClaims()
		if ok {
			resolvedMsg, _ := EncodeMessage("action_resolved", ActionResolvedPayload{
				Seat:     approvedClaim.Seat,
				Action:   approvedClaim.Action,
				FromSeat: room.LastDiscardSeat,
			})
			h.BroadcastToRoom(room.ID, resolvedMsg)
		}
	}
}
