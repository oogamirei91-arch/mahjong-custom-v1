package network

import (
	"encoding/json"
	"log"
	"mahjong-server/pkg/engine"
)

// Client merepresentasikan koneksi aktif satu pemain di server.
type Client struct {
	Hub      *Hub           // Referensi ke central Hub
	Conn     *WSConn        // Koneksi WebSocket fisik
	Player   *engine.Player // Data pemain di game engine
	SendChan chan []byte    // Channel antrian pesan keluar
	RoomID   string         // ID Room tempat pemain berada
}

// NewClient membuat instansiasi Client baru.
func NewClient(hub *Hub, conn *WSConn, player *engine.Player, roomID string) *Client {
	return &Client{
		Hub:      hub,
		Conn:     conn,
		Player:   player,
		SendChan: make(chan []byte, 256),
		RoomID:   roomID,
	}
}

// ReadPump adalah goroutine yang bertugas membaca pesan masuk dari HP pemain.
func (c *Client) ReadPump() {
	defer func() {
		c.Hub.UnregisterClient(c)
		c.Conn.Close()
	}()

	for {
		rawMsg, err := c.Conn.ReadTextMessage()
		if err != nil {
			log.Printf("[WS] Pemain %s terputus: %v", c.Player.Username, err)
			break
		}

		var envelope Message
		if err := json.Unmarshal(rawMsg, &envelope); err != nil {
			log.Printf("[WS] Format pesan tidak valid dari %s: %v", c.Player.Username, err)
			continue
		}

		// Teruskan pesan ke Hub untuk diproses oleh Room Game Engine
		c.Hub.HandleIncomingMessage(c, envelope)
	}
}

// WritePump adalah goroutine yang bertugas mengirimkan pesan keluar ke HP pemain.
func (c *Client) WritePump() {
	defer func() {
		c.Conn.Close()
	}()

	for msg := range c.SendChan {
		if err := c.Conn.WriteTextMessage(msg); err != nil {
			log.Printf("[WS] Gagal mengirim pesan ke %s: %v", c.Player.Username, err)
			break
		}
	}
}

// SendMessage mengirim pesan ke channel client secara non-blocking.
func (c *Client) SendMessage(data []byte) {
	select {
	case c.SendChan <- data:
	default:
		log.Printf("[WS] SendChan buffer penuh untuk pemain %s", c.Player.Username)
	}
}
