package main

import (
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"os"

	"mahjong-server/pkg/engine"
	"mahjong-server/pkg/network"
)

func main() {
	// Baca port dinamis dari environment variable (Koyeb menyuntikkan $PORT secara otomatis)
	port := os.Getenv("PORT")
	if port == "" {
		port = "8080" // Port default untuk pengujian lokal
	}

	// Inisialisasi central WebSocket Hub
	hub := network.NewHub()

	// Endpoint Health Check untuk monitoring Koyeb
	http.HandleFunc("/healthz", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{
			"status":  "healthy",
			"game":    "Mahjong Custom v1.0",
			"version": "1.0.0",
		})
	})

	// Endpoint Root
	http.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path != "/" {
			http.NotFound(w, r)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]string{
			"message": "Mahjong Custom Game Server is Running!",
			"ws_url":  "/ws",
		})
	})

	// Endpoint WebSocket Game
	http.HandleFunc("/ws", func(w http.ResponseWriter, r *http.Request) {
		// Ambil parameter username & user_id dari URL query string (misal: /ws?username=Budi&user_id=u1)
		username := r.URL.Query().Get("username")
		if username == "" {
			username = fmt.Sprintf("Guest_%d", os.Getpid())
		}
		userID := r.URL.Query().Get("user_id")
		if userID == "" {
			userID = fmt.Sprintf("uid_%s", username)
		}

		// Lakukan upgrade HTTP ke WebSocket RFC 6455
		wsConn, err := network.UpgradeToWebSocket(w, r)
		if err != nil {
			log.Printf("[Main] Gagal upgrade WebSocket: %v", err)
			return
		}

		// Buat objek Player dan Client baru
		player := engine.NewPlayer(userID, username, engine.SeatEast)
		client := network.NewClient(hub, wsConn, player, "")

		// Daftarkan client ke Hub dan masukkan ke Room
		hub.RegisterClient(client)
		_, err = hub.JoinOrCreateRoom(client)
		if err != nil {
			log.Printf("[Main] Gagal memasukkan pemain ke room: %v", err)
			client.Conn.Close()
			return
		}

		// Jalankan goroutine WritePump dan ReadPump secara konkuren
		go client.WritePump()
		go client.ReadPump()
	})

	log.Printf("==================================================")
	log.Printf(" 🀄 MAHJONG CUSTOM v1.0 GAME SERVER BERJALAN")
	log.Printf(" Port: %s | Mode: Server Authoritative", port)
	log.Printf(" WebSocket URL: ws://localhost:%s/ws", port)
	log.Printf("==================================================")

	if err := http.ListenAndServe(":"+port, nil); err != nil {
		log.Fatalf("[Main] Gagal menjalankan HTTP Server: %v", err)
	}
}
