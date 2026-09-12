package main

import (
	"bufio"
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"os"
	"strings"

	"mahjong-server/pkg/database"
	"mahjong-server/pkg/engine"
	"mahjong-server/pkg/network"
)

func loadEnv(filePath string) map[string]string {
	env := make(map[string]string)
	file, err := os.Open(filePath)
	if err != nil {
		return env
	}
	defer file.Close()

	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		line := strings.TrimSpace(scanner.Text())
		if line == "" || strings.HasPrefix(line, "#") {
			continue
		}
		parts := strings.SplitN(line, "=", 2)
		if len(parts) == 2 {
			k := strings.TrimSpace(parts[0])
			v := strings.TrimSpace(parts[1])
			v = strings.Trim(v, `"'`)
			env[k] = v
		}
	}
	return env
}

func enableCORS(w http.ResponseWriter) {
	w.Header().Set("Access-Control-Allow-Origin", "*")
	w.Header().Set("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
	w.Header().Set("Access-Control-Allow-Headers", "Content-Type, Authorization")
}

func main() {
	env := loadEnv(".env")
	if len(env) == 0 {
		env = loadEnv("server/.env")
	}

	port := os.Getenv("PORT")
	if port == "" {
		port = env["PORT"]
	}
	if port == "" {
		port = "8080"
	}

	dbURL := os.Getenv("DATABASE_URL")
	if dbURL == "" {
		dbURL = env["DATABASE_URL"]
	}

	log.Println("==================================================")
	log.Println(" 🀄 MAHJONG CUSTOM v1.0 GAME SERVER BERJALAN")
	log.Printf(" Port: %s | Mode: Authoritative WebSocket & REST API", port)

	// Hubungkan ke Supabase Cloud PostgreSQL
	var pgRepo *database.PostgresRepository
	var memRepo *database.Repository

	if dbURL != "" {
		var err error
		pgRepo, err = database.NewPostgresRepository(dbURL)
		if err != nil {
			log.Printf("⚠️ [Server] Peringatan koneksi Supabase: %v", err)
			log.Println("ℹ️ [Server] Mengaktifkan Fallback In-Memory Repository.")
			memRepo = database.NewRepository()
		} else {
			defer pgRepo.Close()
			log.Println("📡 [Server] Database Supabase Cloud PostgreSQL AKTIF (Region ap-south-1).")
		}
	} else {
		log.Println("ℹ️ [Server] DATABASE_URL kosong, menggunakan In-Memory Repository.")
		memRepo = database.NewRepository()
	}

	hub := network.NewHub()

	// --- REST API ENDPOINTS ---

	// 1. Health Check
	http.HandleFunc("/healthz", func(w http.ResponseWriter, r *http.Request) {
		enableCORS(w)
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"status":   "healthy",
			"game":     "Mahjong Custom v1.0",
			"database": "Supabase Cloud PostgreSQL (Connected)",
		})
	})

	// 2. Auth: Check & Login / Register Guest Mode
	http.HandleFunc("/api/auth/guest", func(w http.ResponseWriter, r *http.Request) {
		enableCORS(w)
		if r.Method == "OPTIONS" {
			return
		}

		var req struct {
			DeviceID string `json:"device_id"`
			Nickname string `json:"nickname"`
			Action   string `json:"action"` // "CHECK", "REGISTER", or "AUTO"
		}
		_ = json.NewDecoder(r.Body).Decode(&req)

		w.Header().Set("Content-Type", "application/json")

		if req.Action == "CHECK" {
			var existing *database.User
			var err error
			if pgRepo != nil {
				existing, err = pgRepo.CheckGuestExists(req.DeviceID)
			}
			if err != nil || existing == nil {
				w.WriteHeader(http.StatusNotFound)
				json.NewEncoder(w).Encode(map[string]interface{}{
					"success":       false,
					"need_register": true,
					"message":       "Akun Tamu belum terdaftar di Supabase. Silakan lakukan registrasi.",
				})
				return
			}

			// User ditemukan
			var wallet *database.UserWallet
			var stats *database.UserStats
			if pgRepo != nil {
				_, wallet, stats, _ = pgRepo.GetUserProfile(existing.ID)
			}
			json.NewEncoder(w).Encode(map[string]interface{}{
				"success":       true,
				"need_register": false,
				"user":          existing,
				"wallet":        wallet,
				"stats":         stats,
			})
			return
		}

		// Mode Register / Login Instan
		var user *database.User
		var err error

		if pgRepo != nil {
			user, err = pgRepo.RegisterOrLoginGuest(req.DeviceID, req.Nickname)
		} else {
			user, err = memRepo.RegisterOrLoginGuest(req.DeviceID, req.Nickname)
		}

		if err != nil {
			w.WriteHeader(http.StatusBadRequest)
			json.NewEncoder(w).Encode(map[string]interface{}{
				"success": false,
				"message": err.Error(),
			})
			return
		}

		var wallet *database.UserWallet
		var stats *database.UserStats
		if pgRepo != nil {
			_, wallet, stats, _ = pgRepo.GetUserProfile(user.ID)
		}

		json.NewEncoder(w).Encode(map[string]interface{}{
			"success": true,
			"user":    user,
			"wallet":  wallet,
			"stats":   stats,
		})
	})

	// 3. Auth: Register Email / Password
	http.HandleFunc("/api/auth/register", func(w http.ResponseWriter, r *http.Request) {
		enableCORS(w)
		if r.Method == "OPTIONS" {
			return
		}

		var req struct {
			Username    string `json:"username"`
			Email       string `json:"email"`
			Password    string `json:"password"`
			DisplayName string `json:"display_name"`
			AvatarID    int    `json:"avatar_id"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, "Bad Request", http.StatusBadRequest)
			return
		}

		w.Header().Set("Content-Type", "application/json")

		// Cek apakah username/email sudah terdaftar
		if pgRepo != nil {
			exists, _ := pgRepo.CheckUserExists(req.Username)
			if exists {
				w.WriteHeader(http.StatusConflict)
				json.NewEncoder(w).Encode(map[string]interface{}{
					"success": false,
					"message": fmt.Sprintf("Username '%s' sudah terdaftar di Supabase!", req.Username),
				})
				return
			}
			if req.Email != "" {
				existsEmail, _ := pgRepo.CheckUserExists(req.Email)
				if existsEmail {
					w.WriteHeader(http.StatusConflict)
					json.NewEncoder(w).Encode(map[string]interface{}{
						"success": false,
						"message": fmt.Sprintf("Email '%s' sudah terdaftar di Supabase!", req.Email),
					})
					return
				}
			}
		}

		var user *database.User
		var err error
		if pgRepo != nil {
			user, err = pgRepo.RegisterUser(req.Username, req.Email, req.Password, req.DisplayName, req.AvatarID)
		} else {
			user, err = memRepo.RegisterUser(req.Username, req.Email, req.Password, req.DisplayName, req.AvatarID)
		}

		if err != nil {
			w.WriteHeader(http.StatusBadRequest)
			json.NewEncoder(w).Encode(map[string]interface{}{
				"success": false,
				"message": err.Error(),
			})
			return
		}

		var wallet *database.UserWallet
		var stats *database.UserStats
		if pgRepo != nil {
			_, wallet, stats, _ = pgRepo.GetUserProfile(user.ID)
		}

		json.NewEncoder(w).Encode(map[string]interface{}{
			"success": true,
			"user":    user,
			"wallet":  wallet,
			"stats":   stats,
		})
	})

	// 4. Auth: Login Email / Password (Cek User: Jika belum ada -> Harus Registrasi)
	http.HandleFunc("/api/auth/login", func(w http.ResponseWriter, r *http.Request) {
		enableCORS(w)
		if r.Method == "OPTIONS" {
			return
		}

		var req struct {
			Identifier string `json:"identifier"`
			Password   string `json:"password"`
		}
		if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
			http.Error(w, "Bad Request", http.StatusBadRequest)
			return
		}

		w.Header().Set("Content-Type", "application/json")

		// 1. Cek apakah user ada di database
		if pgRepo != nil {
			exists, _ := pgRepo.CheckUserExists(req.Identifier)
			if !exists {
				w.WriteHeader(http.StatusNotFound)
				json.NewEncoder(w).Encode(map[string]interface{}{
					"success":       false,
					"need_register": true,
					"message":       "Akun belum terdaftar di database Supabase! Silakan lakukan registrasi terlebih dahulu.",
				})
				return
			}
		}

		var user *database.User
		var err error
		if pgRepo != nil {
			user, err = pgRepo.AuthenticateUser(req.Identifier, req.Password)
		} else {
			user, err = memRepo.AuthenticateUser(req.Identifier, req.Password)
		}

		if err != nil {
			w.WriteHeader(http.StatusUnauthorized)
			json.NewEncoder(w).Encode(map[string]interface{}{
				"success":       false,
				"need_register": false,
				"message":       "Kata sandi yang Anda masukkan salah.",
			})
			return
		}

		var wallet *database.UserWallet
		var stats *database.UserStats
		if pgRepo != nil {
			_, wallet, stats, _ = pgRepo.GetUserProfile(user.ID)
		}

		json.NewEncoder(w).Encode(map[string]interface{}{
			"success": true,
			"user":    user,
			"wallet":  wallet,
			"stats":   stats,
		})
	})

	// 5. Auth: Google Sign-In (Check & Register)
	http.HandleFunc("/api/auth/google", func(w http.ResponseWriter, r *http.Request) {
		enableCORS(w)
		if r.Method == "OPTIONS" {
			return
		}

		var req struct {
			OAuthID     string `json:"oauth_id"`
			Email       string `json:"email"`
			DisplayName string `json:"display_name"`
			AvatarURL   string `json:"avatar_url"`
			Action      string `json:"action"` // "CHECK" or "REGISTER" or "AUTO"
		}
		_ = json.NewDecoder(r.Body).Decode(&req)

		w.Header().Set("Content-Type", "application/json")

		if req.OAuthID == "" {
			w.WriteHeader(http.StatusBadRequest)
			json.NewEncoder(w).Encode(map[string]interface{}{
				"success": false,
				"message": "OAuth ID wajib diisi.",
			})
			return
		}

		if req.Action == "CHECK" {
			var existing *database.User
			var err error
			if pgRepo != nil {
				existing, err = pgRepo.CheckOAuthExists(database.AuthProviderGoogle, req.OAuthID)
			} else {
				existing, err = memRepo.CheckOAuthExists(database.AuthProviderGoogle, req.OAuthID)
			}

			if err != nil || existing == nil {
				w.WriteHeader(http.StatusNotFound)
				json.NewEncoder(w).Encode(map[string]interface{}{
					"success":       false,
					"need_register": true,
					"message":       "Akun Google belum terdaftar di database Supabase. Silakan lakukan registrasi.",
				})
				return
			}

			var wallet *database.UserWallet
			var stats *database.UserStats
			if pgRepo != nil {
				_, wallet, stats, _ = pgRepo.GetUserProfile(existing.ID)
			}
			json.NewEncoder(w).Encode(map[string]interface{}{
				"success":       true,
				"need_register": false,
				"user":          existing,
				"wallet":        wallet,
				"stats":         stats,
			})
			return
		}

		// Mode Register
		var user *database.User
		var err error
		if pgRepo != nil {
			user, err = pgRepo.RegisterOAuthUser(database.AuthProviderGoogle, req.OAuthID, req.Email, req.DisplayName, req.AvatarURL)
		} else {
			user, err = memRepo.RegisterOAuthUser(database.AuthProviderGoogle, req.OAuthID, req.Email, req.DisplayName, req.AvatarURL)
		}

		if err != nil {
			w.WriteHeader(http.StatusBadRequest)
			json.NewEncoder(w).Encode(map[string]interface{}{
				"success": false,
				"message": err.Error(),
			})
			return
		}

		var wallet *database.UserWallet
		var stats *database.UserStats
		if pgRepo != nil {
			_, wallet, stats, _ = pgRepo.GetUserProfile(user.ID)
		}

		json.NewEncoder(w).Encode(map[string]interface{}{
			"success": true,
			"user":    user,
			"wallet":  wallet,
			"stats":   stats,
		})
	})

	// 6. Leaderboard Global
	http.HandleFunc("/api/leaderboard", func(w http.ResponseWriter, r *http.Request) {
		enableCORS(w)
		var entries []database.LeaderboardEntry
		if pgRepo != nil {
			entries = pgRepo.GetLeaderboard(50)
		} else {
			entries = memRepo.GetLeaderboard(50)
		}

		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(entries)
	})

	// 6. User Profile & Wallet
	http.HandleFunc("/api/profile", func(w http.ResponseWriter, r *http.Request) {
		enableCORS(w)
		userID := r.URL.Query().Get("user_id")
		if userID == "" {
			http.Error(w, "user_id required", http.StatusBadRequest)
			return
		}

		var user *database.User
		var wallet *database.UserWallet
		var stats *database.UserStats
		var err error

		if pgRepo != nil {
			user, wallet, stats, err = pgRepo.GetUserProfile(userID)
		} else {
			user, wallet, stats, err = memRepo.GetUserProfile(userID)
		}

		if err != nil {
			http.Error(w, err.Error(), http.StatusNotFound)
			return
		}

		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(map[string]interface{}{
			"user":   user,
			"wallet": wallet,
			"stats":  stats,
		})
	})

	// 7. WebSocket Gateway
	http.HandleFunc("/ws", func(w http.ResponseWriter, r *http.Request) {
		username := r.URL.Query().Get("username")
		if username == "" {
			username = fmt.Sprintf("Guest_%d", os.Getpid())
		}
		userID := r.URL.Query().Get("user_id")
		if userID == "" {
			userID = fmt.Sprintf("uid_%s", username)
		}

		wsConn, err := network.UpgradeToWebSocket(w, r)
		if err != nil {
			log.Printf("[Main] Gagal upgrade WebSocket: %v", err)
			return
		}

		player := engine.NewPlayer(userID, username, engine.SeatEast)
		client := network.NewClient(hub, wsConn, player, "")

		hub.RegisterClient(client)
		_, err = hub.JoinOrCreateRoom(client)
		if err != nil {
			log.Printf("[Main] Gagal memasukkan pemain ke room: %v", err)
			client.Conn.Close()
			return
		}

		go client.WritePump()
		go client.ReadPump()
	})

	// 8. Static Web Client & Root Route
	webDir := "./web-client"
	if _, err := os.Stat(webDir); os.IsNotExist(err) {
		webDir = "../web-client"
	}

	if info, err := os.Stat(webDir); err == nil && info.IsDir() {
		fs := http.FileServer(http.Dir(webDir))
		http.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
			enableCORS(w)
			if strings.HasPrefix(r.URL.Path, "/api/") {
				http.NotFound(w, r)
				return
			}
			fs.ServeHTTP(w, r)
		})
		log.Printf("📂 [Static Web] Melayani Web Client dari direktori: %s", webDir)
	} else {
		// Root Route API
		http.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
			enableCORS(w)
			if r.URL.Path != "/" {
				http.NotFound(w, r)
				return
			}
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(map[string]interface{}{
				"message":  "Mahjong Custom Game Server is Running Online!",
				"database": "Supabase Cloud PostgreSQL",
				"ws_url":   "/ws",
			})
		})
	}

	log.Printf(" WebSocket URL: ws://localhost:%s/ws", port)
	log.Println("==================================================")

	if err := http.ListenAndServe(":"+port, nil); err != nil {
		log.Fatalf("[Main] Gagal menjalankan HTTP Server: %v", err)
	}
}
