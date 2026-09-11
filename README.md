# Mahjong Custom v1.0 — 4-Player 3D Multiplayer Game

Game Mahjong 4-Pemain Multiplayer Online bertema **Modern Luxury VIP Emerald Casino** dengan arsitektur **Authoritative Go Backend Server** dan **Unity 3D Client (Android .APK & iOS Ready)**.

---

## Arsitektur Modular Proyek (Modul 1 s/d 8)

### Backend Server (Golang & PostgreSQL / Supabase)
- **[Modul 1: Tile & Deck Engine](file:///d:/Project%20SS/Mahjong/MODUL_1_PENJELASAN_DAN_FLOWCHART.pdf)** — 144 ubin, `crypto/rand` Fisher-Yates shuffle, pembagian 13 ubin privat.
- **[Modul 2: Hand Evaluator & Scoring](file:///d:/Project%20SS/Mahjong/MODUL_2_PENJELASAN_DAN_FLOWCHART.pdf)** — Backtracking evaluator $O(1)$, validasi 4 Melds + 1 Pair, Seven Pairs, Pure Suit, All Triplets, kalkulasi skor.
- **[Modul 3: Room & Game State Manager](file:///d:/Project%20SS/Mahjong/MODUL_3_PENJELASAN_DAN_FLOWCHART.pdf)** — State Machine (Dealing, TurnActive 15s Timer, AwaitingReactions 5s Window, RoundEnd, MatchEnd).
- **[Modul 4: WebSocket Network Layer](file:///d:/Project%20SS/Mahjong/MODUL_4_PENJELASAN_DAN_FLOWCHART.pdf)** — Zero-dependency RFC 6455 WebSockets, matchmaking queue, custom room map (`VIP-XXXX`), direct friend invites.
- **[Modul 5: Database & Persistence Layer](file:///d:/Project%20SS/Mahjong/MODUL_5_PENJELASAN_DAN_FLOWCHART.pdf)** — PostgreSQL schema di Supabase (users, user_stats, friendships, match_records, leaderboard), trophy progression.

### Client Game Engine (Unity 3D C#)
- **[Modul 6: Unity 3D Scene & Procedural Assets](file:///d:/Project%20SS/Mahjong/MODUL_6_PENJELASAN_DAN_FLOWCHART.pdf)** — Meja beludru zamrud, ubin dual-layer (pearl/jade), kompas LED gyro, atlas generator 2048x2048, kamera isometrik mobile.
- **[Modul 7: Unity Network Client & WebSocket Controller](file:///d:/Project%20SS/Mahjong/MODUL_7_PENJELASAN_DAN_FLOWCHART.pdf)** — `ClientWebSocket`, thread-safe dispatcher, sinkronisasi event in-game realtime.
- **[Modul 8: Unity UI Touch Controller, Audio & Android APK](file:///d:/Project%20SS/Mahjong/MODUL_8_PENJELASAN_DAN_FLOWCHART.pdf)** — Kontrol sentuh (tap/drag), Action Bar Chow/Pong/Kong/Win, procedural DSP audio synthesizer, panduan export APK Android.

---

## Panduan Menjalankan Game

### 1. Menjalankan Server Backend (Go)
```bash
cd server
go run cmd/game-server/main.go
```
*Server aktif pada port 8080 (atau variabel lingkungan `$PORT` di Koyeb).*

### 2. Menjalankan / Build Client (Unity)
1. Buka folder `client/` di Unity Editor (versi 2021 LTS, 2022 LTS, atau Unity 6).
2. Tekan **Play** untuk mencoba langsung di Editor.
3. Untuk membuat APK Android: **File ➔ Build Settings ➔ Platform Android ➔ Build (MahjongVIP.apk)**.
