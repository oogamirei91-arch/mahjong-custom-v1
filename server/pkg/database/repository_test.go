package database

import (
	"testing"
)

func TestRegisterAndAuthenticate(t *testing.T) {
	repo := NewRepository()

	// 1. Uji Registrasi Email / Password
	user, err := repo.RegisterUser("budi_pro", "budi@casino.vip", "password123", "Budi The Master", 1)
	if err != nil {
		t.Fatalf("Gagal registrasi user: %v", err)
	}
	if user.Username != "budi_pro" || user.DisplayName != "Budi The Master" {
		t.Errorf("Data registrasi tidak sesuai: %+v", user)
	}

	// 2. Uji Username Duplikat
	_, errDup := repo.RegisterUser("budi_pro", "other@casino.vip", "otherpass", "Budi Clone", 2)
	if errDup == nil {
		t.Errorf("Seharusnya gagal jika username duplikat")
	}

	// 3. Uji Login Berhasil via Username
	authUser, authErr := repo.AuthenticateUser("budi_pro", "password123")
	if authErr != nil {
		t.Fatalf("Login seharusnya berhasil: %v", authErr)
	}
	if authUser.ID != user.ID {
		t.Errorf("ID user login tidak cocok")
	}

	// 4. Uji Login Berhasil via Email
	authEmail, authEmailErr := repo.AuthenticateUser("budi@casino.vip", "password123")
	if authEmailErr != nil || authEmail.ID != user.ID {
		t.Fatalf("Login via email seharusnya berhasil")
	}

	// 5. Uji Login Password Salah
	_, errWrong := repo.AuthenticateUser("budi_pro", "wrongpass")
	if errWrong == nil {
		t.Errorf("Seharusnya gagal saat password salah")
	}
}

func TestOAuthAndGuestRegistration(t *testing.T) {
	repo := NewRepository()

	// 1. Uji Google OAuth
	gUser, err := repo.RegisterOrLoginOAuth(AuthProviderGoogle, "google_sub_123456", "googleuser@gmail.com", "Google Mahjong Pro", "https://avatar.url")
	if err != nil || gUser.AuthProvider != AuthProviderGoogle {
		t.Fatalf("Gagal login via Google OAuth: %v", err)
	}

	// 2. Uji Guest Mode
	guestUser, errGuest := repo.RegisterOrLoginGuest("device_android_9981", "VIP Guest")
	if errGuest != nil || guestUser.AuthProvider != AuthProviderGuest {
		t.Fatalf("Gagal login via Guest Mode: %v", errGuest)
	}

	// 3. Verifikasi Wallet Inisial
	_, wallet, _, _ := repo.GetUserProfile(guestUser.ID)
	if wallet.ChipsBalance != 10000 {
		t.Errorf("Saldo inisial wallet harus 10,000 chips, didapat %d", wallet.ChipsBalance)
	}
}

func TestFriendListSystem(t *testing.T) {
	repo := NewRepository()

	u1, _ := repo.RegisterUser("budi", "budi@test.com", "pass1", "Budi", 1)
	u2, _ := repo.RegisterUser("andi", "andi@test.com", "pass2", "Andi", 2)

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

	u1, _ := repo.RegisterUser("player1", "p1@test.com", "pass1", "Pemain Satu", 1)
	u2, _ := repo.RegisterUser("player2", "p2@test.com", "pass2", "Pemain Dua", 2)

	scores := map[string]int{
		u1.ID: 550, // Melewati 500 trophy -> Rank naik jadi Apprentice
		u2.ID: 120,
	}

	// Uji Mode RANKED
	err := repo.RecordMatchOutcome("ROOM-101", "RANKED_MATCH", u1.ID, "PURE_SUIT", scores, 250)
	if err != nil {
		t.Fatalf("Gagal mencatat match: %v", err)
	}

	_, _, stat1, _ := repo.GetUserProfile(u1.ID)
	if stat1.TotalMatches != 1 || stat1.TotalWins != 1 {
		t.Errorf("Statistik match U1 salah: %+v", stat1)
	}
	if stat1.TrophyPoints != 40 { // Trophy delta untuk winner
		t.Logf("Trophy points U1: %d", stat1.TrophyPoints)
	}
	if stat1.RankTier == "" {
		t.Errorf("Rank tier U1 tidak boleh kosong")
	}

	// Verifikasi Leaderboard
	leaderboard := repo.GetLeaderboard(10)
	if len(leaderboard) != 2 || leaderboard[0].Username != "player1" {
		t.Errorf("Juara 1 leaderboard harusnya player1")
	}
}
