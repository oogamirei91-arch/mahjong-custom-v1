package engine

import (
	"errors"
	"fmt"
)

// Room merepresentasikan satu meja permainan Mahjong Custom v1.0 yang diisi 4 pemain.
type Room struct {
	ID                  string                    `json:"id"`
	Players             [4]*Player                `json:"players"`               // 4 Kursi: East (0), South (1), West (2), North (3)
	Wall                *Wall                     `json:"wall"`                  // Dinding kartu aktif
	CurrentRound        int                       `json:"current_round"`         // Ronde 1 sampai 4
	DealerSeat          PlayerSeat                `json:"dealer_seat"`           // Posisi dealer ronde ini
	ActiveSeat          PlayerSeat                `json:"active_seat"`           // Pemain yang gilirannya sedang aktif
	Phase               GamePhase                 `json:"phase"`                 // Status tahapan permainan saat ini
	LastDiscardedTile   Tile                      `json:"last_discarded_tile"`   // Tile terakhir yang dibuang ke meja
	LastDiscardSeat     PlayerSeat                `json:"last_discard_seat"`     // Kursi pemain pembuang tile terakhir
	PendingClaims       map[PlayerSeat]ActionClaim `json:"pending_claims"`        // Klaim aksi dari 3 pemain lain saat ada discard
	WinnerSeat          PlayerSeat                `json:"winner_seat"`           // Pemenang ronde (-1 jika draw)
	RoundScoreBreakdown ScoreBreakdown            `json:"round_score_breakdown"` // Rincian skor ronde ini
}

// NewRoom membuat room baru dengan 4 kursi kosong.
func NewRoom(id string) *Room {
	return &Room{
		ID:            id,
		CurrentRound:  1,
		DealerSeat:    SeatEast,
		ActiveSeat:    SeatEast,
		Phase:         PhaseLobby,
		PendingClaims: make(map[PlayerSeat]ActionClaim),
		WinnerSeat:    -1,
	}
}

// AddPlayer mendudukkan pemain pada kursi tertentu di meja.
func (r *Room) AddPlayer(player *Player, seat PlayerSeat) bool {
	if seat < 0 || seat > 3 {
		return false
	}
	if r.Players[seat] != nil {
		return false // Kursi sudah terisi
	}
	player.Seat = seat
	r.Players[seat] = player
	return true
}

// IsFull memeriksa apakah ke-4 kursi sudah terisi penuh.
func (r *Room) IsFull() bool {
	for i := 0; i < 4; i++ {
		if r.Players[i] == nil {
			return false
		}
	}
	return true
}

// StartMatch memulai pertandingan 4 ronde.
func (r *Room) StartMatch() error {
	if !r.IsFull() {
		return errors.New("meja belum penuh: butuh 4 pemain untuk memulai match")
	}
	r.CurrentRound = 1
	r.DealerSeat = SeatEast
	return r.StartNewRound()
}

// StartNewRound memulai ronde baru: reset tangan, kocok wall, deal 53 tile, dan tukar bunga awal.
func (r *Room) StartNewRound() error {
	r.Phase = PhaseDealing
	r.WinnerSeat = -1
	r.PendingClaims = make(map[PlayerSeat]ActionClaim)

	// 1. Reset kartu masing-masing pemain
	for _, p := range r.Players {
		p.ResetForNewRound()
	}

	// 2. Buat wall baru yang sudah dikocok secara aman
	r.Wall = NewWall()

	// 3. Deal kartu awal: Dealer (14 tile), P2-P4 (13 tile)
	dealResult, err := r.Wall.DealInitialHands()
	if err != nil {
		return fmt.Errorf("gagal membagikan kartu awal: %w", err)
	}

	for seat, tiles := range dealResult.Hands {
		r.Players[seat].Hand = tiles
	}

	// 4. Rekursi Pembersihan Flower & Season awal
	// Jika kartu awal mengandung Bunga, otomatis pindahkan ke bonus dan ambil pengganti dari tail
	for _, p := range r.Players {
		cleanHand := make([]Tile, 0, len(p.Hand))
		for _, t := range p.Hand {
			if t.IsBonus {
				p.AddBonusTile(t)
				// Ambil replacement tile sampai dapat kartu aktif
				for {
					repTile, err := r.Wall.DrawReplacementTile()
					if err != nil {
						break
					}
					if repTile.IsBonus {
						p.AddBonusTile(repTile)
					} else {
						cleanHand = append(cleanHand, repTile)
						break
					}
				}
			} else {
				cleanHand = append(cleanHand, t)
			}
		}
		p.Hand = cleanHand
	}

	// Dealer (East) mulai giliran pertama
	r.ActiveSeat = r.DealerSeat
	r.Phase = PhaseTurnActive
	return nil
}

// ExecuteDraw mengambil 1 tile dari wall untuk pemain aktif.
// Jika tile yang diambil adalah Flower/Season, otomatis masuk ke bonus dan mengambil replacement tile.
func (r *Room) ExecuteDraw(seat PlayerSeat) (Tile, []Tile, error) {
	if r.Phase != PhaseTurnActive || r.ActiveSeat != seat {
		return Tile{}, nil, errors.New("bukan giliran pemain ini untuk draw")
	}

	player := r.Players[seat]
	var collectedBonus []Tile

	for {
		drawnTile, err := r.Wall.DrawTile()
		if err != nil {
			// Wall habis -> Ronde berakhir Draw
			r.EndRoundDraw()
			return Tile{}, nil, err
		}

		if drawnTile.IsBonus {
			player.AddBonusTile(drawnTile)
			collectedBonus = append(collectedBonus, drawnTile)

			// Ambil pengganti dari Dead Wall (Tail)
			for {
				repTile, repErr := r.Wall.DrawReplacementTile()
				if repErr != nil {
					break
				}
				if repTile.IsBonus {
					player.AddBonusTile(repTile)
					collectedBonus = append(collectedBonus, repTile)
				} else {
					player.AddTile(repTile)
					return repTile, collectedBonus, nil
				}
			}
		} else {
			player.AddTile(drawnTile)
			return drawnTile, collectedBonus, nil
		}
	}
}

// ExecuteDiscard memproses aksi pembuangan tile oleh pemain aktif.
// Mengembalikan opsi aksi reaktif untuk 3 pemain lainnya.
func (r *Room) ExecuteDiscard(seat PlayerSeat, tileID int) (Tile, map[PlayerSeat][]string, error) {
	if r.Phase != PhaseTurnActive || r.ActiveSeat != seat {
		return Tile{}, nil, errors.New("bukan giliran pemain ini untuk discard")
	}

	player := r.Players[seat]
	discardedTile, ok := player.RemoveTileByID(tileID)
	if !ok {
		return Tile{}, nil, errors.New("tile tidak ditemukan di tangan pemain")
	}

	player.AddDiscard(discardedTile)
	r.LastDiscardedTile = discardedTile
	r.LastDiscardSeat = seat
	r.PendingClaims = make(map[PlayerSeat]ActionClaim)

	// Evaluasi opsi aksi untuk 3 pemain lainnya
	availableActions := make(map[PlayerSeat][]string)
	hasAnyAction := false

	for otherSeat := SeatEast; otherSeat <= SeatNorth; otherSeat++ {
		if otherSeat == seat {
			continue
		}
		otherPlayer := r.Players[otherSeat]
		var actions []string

		// 1. Cek Discard WIN
		tempHand := append([]Tile{}, otherPlayer.Hand...)
		tempHand = append(tempHand, discardedTile)
		winCheck := CheckWin(tempHand, otherPlayer.OpenMelds)
		if winCheck.IsWin {
			actions = append(actions, "WIN")
		}

		// 2. Cek KONG (Open Kong)
		if CanOpenKong(otherPlayer.Hand, discardedTile) {
			actions = append(actions, "KONG")
		}

		// 3. Cek PONG
		if CanPong(otherPlayer.Hand, discardedTile) {
			actions = append(actions, "PONG")
		}

		// 4. Cek CHOW (hanya untuk next seat)
		if _, canChow := CanChow(otherPlayer.Hand, discardedTile, seat, otherSeat); canChow {
			actions = append(actions, "CHOW")
		}

		if len(actions) > 0 {
			actions = append(actions, "PASS")
			availableActions[otherSeat] = actions
			hasAnyAction = true
		}
	}

	if hasAnyAction {
		r.Phase = PhaseAwaitingReactions
	} else {
		// Jika tidak ada pemain yang bisa bereaksi, langsung lanjut ke giliran berikutnya
		r.NextTurn()
	}

	return discardedTile, availableActions, nil
}

// SubmitActionClaim mencatat respon aksi pemain selama fase reaksi.
func (r *Room) SubmitActionClaim(seat PlayerSeat, claim ActionClaim) {
	claim.Seat = seat
	r.PendingClaims[seat] = claim
}

// ResolveActionClaims mengevaluasi klaim yang masuk dan mengeksekusi aksi berprioritas tertinggi:
// WIN > KONG > PONG > CHOW > PASS.
func (r *Room) ResolveActionClaims() (ActionClaim, bool) {
	if r.Phase != PhaseAwaitingReactions {
		return ActionClaim{}, false
	}

	priorityOrder := []string{"WIN", "KONG", "PONG", "CHOW"}

	for _, targetAction := range priorityOrder {
		for seat, claim := range r.PendingClaims {
			if claim.Action == targetAction {
				// Eksekusi aksi terpilih
				r.executeApprovedAction(seat, claim)
				return claim, true
			}
		}
	}

	// Jika semua pemain PASS
	r.NextTurn()
	return ActionClaim{Action: "PASS"}, true
}

func (r *Room) executeApprovedAction(seat PlayerSeat, claim ActionClaim) {
	player := r.Players[seat]
	targetTile := r.LastDiscardedTile

	switch claim.Action {
	case "WIN":
		// Discard WIN
		tempHand := append([]Tile{}, player.Hand...)
		tempHand = append(tempHand, targetTile)
		player.Hand = tempHand
		r.EndRoundWin(seat, WinSourceDiscard)

	case "PONG":
		// Ambil 2 tile kembar dari tangan, satukan dengan targetTile menjadi Pong terbuka
		var usedTiles []Tile
		for i := 0; i < 2; i++ {
			for _, t := range player.Hand {
				if t.Suit == targetTile.Suit && t.Value == targetTile.Value {
					removed, _ := player.RemoveTileByID(t.ID)
					usedTiles = append(usedTiles, removed)
					break
				}
			}
		}
		usedTiles = append(usedTiles, targetTile)
		meld := NewMeld(MeldTypePong, usedTiles, false, r.LastDiscardSeat, targetTile)
		player.AddOpenMeld(meld)

		// Giliran berpindah ke pemain yang melakukan Pong dan siap untuk discard
		r.ActiveSeat = seat
		r.Phase = PhaseTurnActive

	case "KONG":
		// Open Kong: Ambil 3 tile kembar dari tangan
		var usedTiles []Tile
		for i := 0; i < 3; i++ {
			for _, t := range player.Hand {
				if t.Suit == targetTile.Suit && t.Value == targetTile.Value {
					removed, _ := player.RemoveTileByID(t.ID)
					usedTiles = append(usedTiles, removed)
					break
				}
			}
		}
		usedTiles = append(usedTiles, targetTile)
		meld := NewMeld(MeldTypeKongOpen, usedTiles, false, r.LastDiscardSeat, targetTile)
		player.AddOpenMeld(meld)

		// Setelah Kong, pemain mengambil 1 replacement tile dari Dead Wall
		repTile, err := r.Wall.DrawReplacementTile()
		if err == nil {
			if repTile.IsBonus {
				player.AddBonusTile(repTile)
			} else {
				player.AddTile(repTile)
			}
		}

		r.ActiveSeat = seat
		r.Phase = PhaseTurnActive

	case "CHOW":
		// Buat Chow terbuka menggunakan 2 tile yang ditentukan
		if len(claim.MeldTiles) == 2 {
			for _, t := range claim.MeldTiles {
				player.RemoveTileByID(t.ID)
			}
			meldTiles := []Tile{claim.MeldTiles[0], claim.MeldTiles[1], targetTile}
			meld := NewMeld(MeldTypeChow, meldTiles, false, r.LastDiscardSeat, targetTile)
			player.AddOpenMeld(meld)
		}
		r.ActiveSeat = seat
		r.Phase = PhaseTurnActive
	}
}

// NextTurn menggeser giliran searah jarum jam (East -> South -> West -> North).
func (r *Room) NextTurn() {
	if r.Wall.RemainingLiveTiles() <= 0 {
		r.EndRoundDraw()
		return
	}

	r.ActiveSeat = (r.ActiveSeat + 1) % 4
	r.Phase = PhaseTurnActive
}

// EndRoundWin menyelesaikan ronde dengan adanya pemain yang menang.
func (r *Room) EndRoundWin(winnerSeat PlayerSeat, winType WinSourceType) {
	r.WinnerSeat = winnerSeat
	winner := r.Players[winnerSeat]

	winResult := CheckWin(winner.Hand, winner.OpenMelds)
	breakdown := CalculateScore(winType, winResult, len(winner.BonusTiles), winner.HasFullFlowerSet(), winner.HasFullSeasonSet())

	r.RoundScoreBreakdown = breakdown
	winner.MatchScore += breakdown.TotalScore

	r.finishRound()
}

// EndRoundDraw menyelesaikan ronde dengan hasil Draw karena wall habis.
func (r *Room) EndRoundDraw() {
	r.WinnerSeat = -1
	r.RoundScoreBreakdown = ScoreBreakdown{
		BaseWinScore: 0,
		TotalScore:   0,
	}
	r.finishRound()
}

func (r *Room) finishRound() {
	r.Phase = PhaseRoundEnded

	// Rotasi Dealer untuk ronde berikutnya
	r.DealerSeat = (r.DealerSeat + 1) % 4

	if r.CurrentRound >= TotalRoundsPerMatch {
		r.Phase = PhaseMatchEnded
	} else {
		r.CurrentRound++
	}
}
