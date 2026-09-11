package engine

import "fmt"

// Player merepresentasikan seorang pemain yang duduk di meja Mahjong.
type Player struct {
	ID          string     `json:"id"`           // ID unik pemain (UUID / string)
	Username    string     `json:"username"`     // Nama tampilan pemain
	Seat        PlayerSeat `json:"seat"`         // Posisi kursi: East, South, West, North
	Hand        []Tile     `json:"hand"`         // Kartu aktif di tangan (tertutup)
	BonusTiles  []Tile     `json:"bonus_tiles"`  // Kartu Flower & Season yang terkumpul
	OpenMelds   []Meld     `json:"open_melds"`   // Kombinasi terbuka (Chow/Pong/Kong)
	Discards    []Tile     `json:"discards"`     // Kartu yang sudah dibuang ke discard pond
	MatchScore  int        `json:"match_score"`  // Akumulasi skor pertandingan saat ini
	IsBot       bool       `json:"is_bot"`       // True jika digantikan bot saat disconnect
	IsConnected bool       `json:"is_connected"` // Status koneksi jaringan
}

// NewPlayer membuat instansiasi Player baru.
func NewPlayer(id, username string, seat PlayerSeat) *Player {
	return &Player{
		ID:          id,
		Username:    username,
		Seat:        seat,
		Hand:        make([]Tile, 0, 14),
		BonusTiles:  make([]Tile, 0, 8),
		OpenMelds:   make([]Meld, 0, 4),
		Discards:    make([]Tile, 0, 30),
		MatchScore:  0,
		IsBot:       false,
		IsConnected: true,
	}
}

// ResetForNewRound mereset kartu tangan, bonus, meld, dan discard pemain untuk ronde baru.
func (p *Player) ResetForNewRound() {
	p.Hand = make([]Tile, 0, 14)
	p.BonusTiles = make([]Tile, 0, 8)
	p.OpenMelds = make([]Meld, 0, 4)
	p.Discards = make([]Tile, 0, 30)
}

// AddTile menambahkan satu tile ke tangan pemain.
func (p *Player) AddTile(t Tile) {
	p.Hand = append(p.Hand, t)
}

// RemoveTileByID menghapus satu tile dari tangan berdasarkan ID fisiknya (saat discard).
func (p *Player) RemoveTileByID(tileID int) (Tile, bool) {
	for i, t := range p.Hand {
		if t.ID == tileID {
			removed := p.Hand[i]
			p.Hand = append(p.Hand[:i], p.Hand[i+1:]...)
			return removed, true
		}
	}
	return Tile{}, false
}

// AddBonusTile menambahkan Flower/Season ke area bonus.
func (p *Player) AddBonusTile(t Tile) {
	p.BonusTiles = append(p.BonusTiles, t)
}

// AddDiscard mencatat tile yang dibuang pemain ke discard pond.
func (p *Player) AddDiscard(t Tile) {
	p.Discards = append(p.Discards, t)
}

// AddOpenMeld menambahkan kombinasi meld terbuka (Chow/Pong/Kong).
func (p *Player) AddOpenMeld(m Meld) {
	p.OpenMelds = append(p.OpenMelds, m)
}

// HasFullFlowerSet memeriksa apakah pemain mengumpulkan seluruh 4 jenis Bunga.
func (p *Player) HasFullFlowerSet() bool {
	flowers := make(map[int]bool)
	for _, b := range p.BonusTiles {
		if b.Suit == SuitFlower {
			flowers[b.Value] = true
		}
	}
	return len(flowers) == 4
}

// HasFullSeasonSet memeriksa apakah pemain mengumpulkan seluruh 4 jenis Musim.
func (p *Player) HasFullSeasonSet() bool {
	seasons := make(map[int]bool)
	for _, b := range p.BonusTiles {
		if b.Suit == SuitSeason {
			seasons[b.Value] = true
		}
	}
	return len(seasons) == 4
}

// ActiveTileCount mengembalikan total tile aktif (tangan + tile di open melds).
func (p *Player) ActiveTileCount() int {
	count := len(p.Hand)
	for _, m := range p.OpenMelds {
		if m.Type == MeldTypeKongConcealed || m.Type == MeldTypeKongOpen || m.Type == MeldTypeKongAdded {
			count += 3 // Kong dihitung setara 3 tile aktif normal
		} else {
			count += len(m.Tiles)
		}
	}
	return count
}

func (p *Player) String() string {
	return fmt.Sprintf("[%s: %s (Hand: %d, Bonus: %d, Score: %d)]", p.Seat, p.Username, len(p.Hand), len(p.BonusTiles), p.MatchScore)
}
