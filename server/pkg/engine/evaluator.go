package engine

// WinSourceType mendefinisikan sumber tile kemenangan (Self Draw atau Discard).
type WinSourceType int

const (
	WinSourceSelfDraw WinSourceType = 0 // Menang dari draw sendiri (+30 poin)
	WinSourceDiscard  WinSourceType = 1 // Menang dari buangan pemain lain (+20 poin)
)

func (w WinSourceType) String() string {
	if w == WinSourceSelfDraw {
		return "Self-Draw"
	}
	return "Discard-Win"
}

// SpecialHandType mencatat jenis pola kemenangan khusus v1.0.
type SpecialHandType string

const (
	SpecialHandNone        SpecialHandType = "NONE"
	SpecialHandSevenPairs  SpecialHandType = "SEVEN_PAIRS"   // 7 Pasangan (+100 poin)
	SpecialHandPureSuit    SpecialHandType = "PURE_SUIT"     // Semua 1 suit tanpa honor (+100 poin)
	SpecialHandAllTriplets SpecialHandType = "ALL_TRIPLETS"  // 4 Pong/Kong + 1 Pair (+50 poin)
)

// WinResult menyimpan informasi lengkap hasil evaluasi kemenangan sebuah hand.
type WinResult struct {
	IsWin           bool            `json:"is_win"`
	SpecialHand     SpecialHandType `json:"special_hand"`
	IsPureSuit      bool            `json:"is_pure_suit"`
	IsAllTriplets   bool            `json:"is_all_triplets"`
	IsSevenPairs    bool            `json:"is_seven_pairs"`
	WinningPairTile Tile            `json:"winning_pair_tile"`
}

// ScoreBreakdown mencatat rincian perolehan poin kemenangan.
type ScoreBreakdown struct {
	BaseWinScore       int             `json:"base_win_score"`       // +100
	WinSourceScore     int             `json:"win_source_score"`     // +30 (Self Draw) atau +20 (Discard)
	SpecialHandScore   int             `json:"special_hand_score"`   // Seven Pairs (+100), Pure Suit (+100), All Triplets (+50)
	FlowerBonusScore   int             `json:"flower_bonus_score"`   // +10 per bonus tile
	FlowerSetBonus     int             `json:"flower_set_bonus"`     // +50 (Full Flower), +50 (Full Season), +150 (Full 8)
	TotalScore         int             `json:"total_score"`          // Total keseluruhan
	SpecialHandApplied SpecialHandType `json:"special_hand_applied"`
}

// BuildCountsMatrix mengubah daftar tile aktif menjadi tabel frekuensi [5][10]int.
// Index 0: Bamboo, 1: Dot, 2: Character, 3: Wind, 4: Dragon.
func BuildCountsMatrix(tiles []Tile) [5][10]int {
	var counts [5][10]int
	for _, t := range tiles {
		if t.Suit >= 0 && t.Suit <= 4 && t.Value >= 1 && t.Value <= 9 {
			counts[t.Suit][t.Value]++
		}
	}
	return counts
}

// CanChow memeriksa apakah pemain berhak melakukan Chow terhadap tile buangan.
// Syarat:
// 1. Tile berasal dari pemain tepat sebelum giliran pemain ini (previous seat).
// 2. Tile adalah Number Tile (Bamboo, Dot, Character).
// 3. Pemain memiliki 2 tile di tangan yang bisa membentuk urutan 3 tile.
func CanChow(hand []Tile, target Tile, fromSeat, mySeat PlayerSeat) ([][2]Tile, bool) {
	// Chow hanya berlaku dari pemain sebelah kiri (previous seat)
	expectedPrevSeat := (mySeat + 3) % 4
	if fromSeat != expectedPrevSeat {
		return nil, false
	}

	if !target.IsNumberTile() {
		return nil, false
	}

	counts := BuildCountsMatrix(hand)
	suit := target.Suit
	val := target.Value
	var validOptions [][2]Tile

	// Opsi 1: target adalah kartu ketiga (val-2, val-1, val)
	if val >= 3 && counts[suit][val-2] > 0 && counts[suit][val-1] > 0 {
		t1 := findTileInHand(hand, suit, val-2)
		t2 := findTileInHand(hand, suit, val-1)
		validOptions = append(validOptions, [2]Tile{t1, t2})
	}

	// Opsi 2: target adalah kartu tengah (val-1, val, val+1)
	if val >= 2 && val <= 8 && counts[suit][val-1] > 0 && counts[suit][val+1] > 0 {
		t1 := findTileInHand(hand, suit, val-1)
		t2 := findTileInHand(hand, suit, val+1)
		validOptions = append(validOptions, [2]Tile{t1, t2})
	}

	// Opsi 3: target adalah kartu pertama (val, val+1, val+2)
	if val <= 7 && counts[suit][val+1] > 0 && counts[suit][val+2] > 0 {
		t1 := findTileInHand(hand, suit, val+1)
		t2 := findTileInHand(hand, suit, val+2)
		validOptions = append(validOptions, [2]Tile{t1, t2})
	}

	return validOptions, len(validOptions) > 0
}

// CanPong memeriksa apakah pemain memiliki minimal 2 tile kembar untuk mengambil tile buangan.
func CanPong(hand []Tile, target Tile) bool {
	if target.IsBonus {
		return false
	}
	counts := BuildCountsMatrix(hand)
	return counts[target.Suit][target.Value] >= 2
}

// CanOpenKong memeriksa apakah pemain memiliki 3 tile kembar di tangan untuk membentuk Open Kong.
func CanOpenKong(hand []Tile, target Tile) bool {
	if target.IsBonus {
		return false
	}
	counts := BuildCountsMatrix(hand)
	return counts[target.Suit][target.Value] == 3
}

// CanConcealedKong mencari tile di tangan yang berjumlah 4 keping untuk membentuk Concealed Kong.
func CanConcealedKong(hand []Tile) ([]Tile, bool) {
	counts := BuildCountsMatrix(hand)
	var kongCandidates []Tile

	for _, t := range hand {
		if !t.IsBonus && counts[t.Suit][t.Value] == 4 {
			// Cegah duplikasi dalam daftar kandidat
			alreadyAdded := false
			for _, c := range kongCandidates {
				if c.IsEqual(t) {
					alreadyAdded = true
					break
				}
			}
			if !alreadyAdded {
				kongCandidates = append(kongCandidates, t)
			}
		}
	}

	return kongCandidates, len(kongCandidates) > 0
}

// CheckWin mengevaluasi apakah kombinasi tangan saat ini memenuhi syarat kemenangan.
// Total tile aktif (tangan tertutup + meld terbuka) harus setara dengan 14 tile.
func CheckWin(hand []Tile, openMelds []Meld) WinResult {
	result := WinResult{IsWin: false, SpecialHand: SpecialHandNone}

	// 1. Cek Special Hand: Seven Pairs (Hanya jika 0 meld terbuka dan tepat 14 kartu di tangan)
	if len(openMelds) == 0 && len(hand) == 14 {
		if isSevenPairs(hand) {
			result.IsWin = true
			result.IsSevenPairs = true
			result.SpecialHand = SpecialHandSevenPairs
			result.IsPureSuit = isPureSuit(hand, openMelds)
			return result
		}
	}

	// 2. Cek Standard Win: (4 - len(openMelds)) Meld tertutup + 1 Pair
	counts := BuildCountsMatrix(hand)
	neededMelds := 4 - len(openMelds)

	// Cari calon Pair (Mata)
	for suit := 0; suit <= 4; suit++ {
		maxVal := 9
		if suit == int(SuitWind) {
			maxVal = 4
		} else if suit == int(SuitDragon) {
			maxVal = 3
		}

		for val := 1; val <= maxVal; val++ {
			if counts[suit][val] >= 2 {
				// Coba ambil sebagai Pair
				counts[suit][val] -= 2

				// Rekursif: Pecah sisa kartu menjadi Triplet (Pong) atau Sequence (Chow)
				if canDecomposeMelds(&counts, neededMelds) {
					result.IsWin = true
					result.WinningPairTile = Tile{Suit: Suit(suit), Value: val}

					// Cek apakah Pure Suit
					result.IsPureSuit = isPureSuit(hand, openMelds)

					// Cek apakah All Triplets
					result.IsAllTriplets = isAllTriplets(&counts, openMelds)

					if result.IsPureSuit {
						result.SpecialHand = SpecialHandPureSuit
					} else if result.IsAllTriplets {
						result.SpecialHand = SpecialHandAllTriplets
					}

					return result
				}

				// Backtrack: Kembalikan pair jika tidak menghasilkan kemenangan
				counts[suit][val] += 2
			}
		}
	}

	return result
}

// canDecomposeMelds menguji secara rekursif apakah sisa kartu dapat habis dibagi menjadi meld yang sah.
func canDecomposeMelds(counts *[5][10]int, remainingMelds int) bool {
	if remainingMelds == 0 {
		// Pastikan semua kartu habis terbagi
		for s := 0; s <= 4; s++ {
			for v := 1; v <= 9; v++ {
				if counts[s][v] > 0 {
					return false
				}
			}
		}
		return true
	}

	// Temukan tile pertama yang masih tersisa
	for suit := 0; suit <= 4; suit++ {
		maxVal := 9
		if suit == int(SuitWind) {
			maxVal = 4
		} else if suit == int(SuitDragon) {
			maxVal = 3
		}

		for val := 1; val <= maxVal; val++ {
			if counts[suit][val] > 0 {
				// Cabang 1: Coba jadikan Triplet (Pong: 3 tile sama)
				if counts[suit][val] >= 3 {
					counts[suit][val] -= 3
					if canDecomposeMelds(counts, remainingMelds-1) {
						counts[suit][val] += 3 // backtrack restore
						return true
					}
					counts[suit][val] += 3 // backtrack
				}

				// Cabang 2: Coba jadikan Sequence (Chow: val, val+1, val+2) - Hanya untuk Number Tiles
				if suit <= 2 && val <= 7 {
					if counts[suit][val+1] > 0 && counts[suit][val+2] > 0 {
						counts[suit][val]--
						counts[suit][val+1]--
						counts[suit][val+2]--

						if canDecomposeMelds(counts, remainingMelds-1) {
							counts[suit][val]++
							counts[suit][val+1]++
							counts[suit][val+2]++
							return true
						}

						counts[suit][val]++
						counts[suit][val+1]++
						counts[suit][val+2]++
					}
				}

				// Jika tile ini tidak bisa membentuk Triplet maupun Sequence, maka kombinasi gagal
				return false
			}
		}
	}

	return false
}

// isSevenPairs memeriksa apakah 14 kartu tertutup terdiri dari tepat 7 pasangan unik.
func isSevenPairs(hand []Tile) bool {
	if len(hand) != 14 {
		return false
	}
	counts := BuildCountsMatrix(hand)
	pairCount := 0

	for s := 0; s <= 4; s++ {
		for v := 1; v <= 9; v++ {
			if counts[s][v] == 2 {
				pairCount++
			} else if counts[s][v] != 0 {
				return false
			}
		}
	}

	return pairCount == 7
}

// isPureSuit memeriksa apakah seluruh tile (tangan + open melds) hanya berasal dari 1 suit angka tanpa Honor.
func isPureSuit(hand []Tile, openMelds []Meld) bool {
	var targetSuit Suit = -1

	checkTile := func(t Tile) bool {
		if !t.IsNumberTile() {
			return false // Honor tiles (Wind/Dragon) dilarang dalam Pure Suit
		}
		if targetSuit == -1 {
			targetSuit = t.Suit
		} else if t.Suit != targetSuit {
			return false
		}
		return true
	}

	for _, t := range hand {
		if !checkTile(t) {
			return false
		}
	}

	for _, m := range openMelds {
		for _, t := range m.Tiles {
			if !checkTile(t) {
				return false
			}
		}
	}

	return targetSuit != -1
}

// isAllTriplets memeriksa apakah semua meld yang dibentuk adalah Triplet/Pong/Kong (tanpa Chow).
func isAllTriplets(counts *[5][10]int, openMelds []Meld) bool {
	for _, m := range openMelds {
		if m.Type == MeldTypeChow {
			return false
		}
	}
	return true
}

// CalculateScore menghitung total skor kemenangan sesuai Rule Book v1.0.
func CalculateScore(winType WinSourceType, winResult WinResult, flowerCount int, fullFlowerSet, fullSeasonSet bool) ScoreBreakdown {
	breakdown := ScoreBreakdown{
		BaseWinScore:       100,
		SpecialHandApplied: winResult.SpecialHand,
	}

	// 1. Bonus Win Source
	if winType == WinSourceSelfDraw {
		breakdown.WinSourceScore = 30
	} else {
		breakdown.WinSourceScore = 20
	}

	// 2. Bonus Special Hand
	if winResult.IsSevenPairs {
		breakdown.SpecialHandScore += 100
	}
	if winResult.IsPureSuit {
		breakdown.SpecialHandScore += 100
	}
	if winResult.IsAllTriplets {
		breakdown.SpecialHandScore += 50
	}

	// 3. Bonus Flower & Season
	breakdown.FlowerBonusScore = flowerCount * 10

	// 4. Bonus Set Bunga
	if fullFlowerSet && fullSeasonSet {
		breakdown.FlowerSetBonus = 150 // Full 8 Bonus
	} else {
		if fullFlowerSet {
			breakdown.FlowerSetBonus += 50
		}
		if fullSeasonSet {
			breakdown.FlowerSetBonus += 50
		}
	}

	// Total Keseluruhan
	breakdown.TotalScore = breakdown.BaseWinScore +
		breakdown.WinSourceScore +
		breakdown.SpecialHandScore +
		breakdown.FlowerBonusScore +
		breakdown.FlowerSetBonus

	return breakdown
}

// Helper untuk menemukan Tile di hand berdasarkan Suit dan Value
func findTileInHand(hand []Tile, suit Suit, value int) Tile {
	for _, t := range hand {
		if t.Suit == suit && t.Value == value {
			return t
		}
	}
	return Tile{}
}
