package database

import (
	"testing"
)

func TestRegisterAndAuthenticate(t *testing.T) {
	repo := NewRepository()

	// 1. Uji Registrasi
	user, err := repo.RegisterUser("budi_pro", "password123", "Budi The Master", 1)
	if err != nil {
		t.Fatalf("Gagal registrasi user: %v", err)
	}
	if user.Username != "budi_pro" || user.DisplayName != "Budi The Master" {
		t.Errorf("Data registrasi tidak sesuai: %+v", user)
	}

	// 2. Uji Username Duplikat
	_, errDup := repo.RegisterUser("budi_pro", "otherpass", "Budi Clone", 2)
	if errDup == nil {
		t.Errorf("Seharusnya gagal jika username duplikat")
	}

	// 3. Uji Login Berhasil
	authUser, authErr := repo.AuthenticateUser("budi_pro", "password123")
	if authErr != nil {
		t.Fatalf("Login seharusnya berhasil: %v", authErr)
	}
	if authUser.ID != user.ID {
		t.Errorf("ID user login tidak cocok")
	}

	// 4. Uji Login Password Salah
	_, errWrong := repo.AuthenticateUser("budi_pro", "wrongpass")
	if errWrong == nil {
		t.Errorf("Seharusnya gagal saat password salah")
	}
}

func TestFriendListSystem(t *testing.T) {
	repo := NewRepository()

	u1, _ := repo.RegisterUser("budi", "pass1", "Budi", 1)
	u2, _ := repo.RegisterUser("andi", "pass2", "Andi", 2)

	// 1. Budi kirim Friend Request ke Andi
	req, err := repo.SendFriendRequest(u1.ID, "andi")
	if err != nil {
		t.Fatalf("Gagal kirim friend request: %v", err)
	}
	if req.Status != "PENDING" {
		t.Errorf("Status request harusnya PENDING")
	}

	// 2. Andi menerima Friend Request dari Budi
	errAccept := repo.AcceptFriendRequest(u2.ID, u1.ID)
	if errAccept != nil {
		t.Fatalf("Gagal accept friend request: %v", errAccept)
	}

	// 3. Cek Friend List Budi
	budiFriends := repo.GetFriendList(u1.ID)
	if len(budiFriends) != 1 || budiFriends[0].Username != "andi" {
		t.Fatalf("Friend list Budi salah: %+v", budiFriends)
	}

	// 4. Cek Friend List Andi (Harus dua arah)
	andiFriends := repo.GetFriendList(u2.ID)
	if len(andiFriends) != 1 || andiFriends[0].Username != "budi" {
		t.Fatalf("Friend list Andi salah: %+v", andiFriends)
	}
}

func TestRecordMatchAndTrophyProgression(t *testing.T) {
	repo := NewRepository()

	u1, _ := repo.RegisterUser("player1", "pass1", "Pemain Satu", 1)
	u2, _ := repo.RegisterUser("player2", "pass2", "Pemain Dua", 2)

	scores := map[string]int{
		u1.ID: 550, // Melewati 500 trophy -> Rank naik jadi Apprentice
		u2.ID: 120,
	}

	// Uji Mode RANKED
	err := repo.RecordMatchOutcome("ROOM-101", "RANKED", u1.ID, "PURE_SUIT", scores, 250)
	if err != nil {
		t.Fatalf("Gagal mencatat match: %v", err)
	}

	_, stat1, _ := repo.GetUserProfile(u1.ID)
	if stat1.TotalMatches != 1 || stat1.TotalWins != 1 {
		t.Errorf("Statistik match U1 salah: %+v", stat1)
	}
	if stat1.TrophyPoints != 550 {
		t.Errorf("Trophy U1 harusnya 550, didapat %d", stat1.TrophyPoints)
	}
	if stat1.RankTier != "Apprentice 🥈" {
		t.Errorf("Rank tier U1 harusnya Apprentice 🥈, didapat %s", stat1.RankTier)
	}

	// Verifikasi Leaderboard
	leaderboard := repo.GetLeaderboard(10)
	if len(leaderboard) != 2 || leaderboard[0].Username != "player1" {
		t.Errorf("Juara 1 leaderboard harusnya player1")
	}
}
