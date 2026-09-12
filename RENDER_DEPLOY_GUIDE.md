# 🚀 Panduan Lengkap Deploy Mahjong VIP 3D ke Render.com (100% GRATIS)

Panduan ini memandu Anda langkah demi langkah untuk meng-online-kan **Game Mahjong VIP 3D (Go Server + Web Client + Supabase Database)** ke **Render.com**.

---

## 🌟 Keuntungan Deploy di Render
1. **100% Gratis (Free Web Service)** tanpa perlu kartu kredit.
2. **1 URL untuk Semuanya**: Frontend Web Client 3D, WebSocket Multiplayer, dan REST API berjalan dalam 1 domain HTTPS gratis (`https://nama-game-anda.onrender.com`).
3. **Terhubung Langsung ke Supabase PostgreSQL**.

---

## 📋 Langkah Demi Langkah

### LANGKAH 1: Unggah (Push) Kode ke GitHub
1. Pastikan seluruh folder project (`server`, `web-client`, `Dockerfile`, `render.yaml`) sudah di-commit dan di-push ke repositori GitHub Anda (bisa berupa Public atau Private repository).

---

### LANGKAH 2: Buat Akun & Web Service di Render.com
1. Kunjungi **[https://render.com/](https://render.com/)** dan login menggunakan akun **GitHub** Anda.
2. Di Dashboard Render, klik tombol **New +** $\rightarrow$ Pilih **Web Service**.
3. Pilih opsi **"Build and deploy from a Git repository"** $\rightarrow$ Hubungkan ke repositori GitHub Mahjong Anda.

---

### LANGKAH 3: Atur Konfigurasi Web Service
Isi formulir dengan pengaturan berikut:

* **Name**: `mahjong-vip-3d` *(atau nama pilihan Anda)*
* **Region**: `Singapore` *(paling dekat dan cepat dari Indonesia)*
* **Branch**: `main` *(atau `master`)*
* **Runtime**: **`Docker`** *(Render akan otomatis mendeteksi `Dockerfile` yang telah kita buat)*
* **Instance Type**: **`Free` ($0/month)**

---

### LANGKAH 4: Masukkan Environment Variable (Database Supabase)
Gulir ke bawah ke bagian **Environment Variables** dan tambahkan variabel berikut:

| Key | Value |
| :--- | :--- |
| `PORT` | `8080` |
| `DATABASE_URL` | `postgresql://postgres.esnybghukyrdkjeglbii:121191GabeTheo*@aws-0-ap-south-1.pooler.supabase.com:5432/postgres?sslmode=require` |

---

### LANGKAH 5: Klik Deploy!
1. Klik tombol **Create Web Service**.
2. Render akan otomatis mendownload source code, mengompilasi binary Go, dan menjalankan web client 3D.
3. Tunggu sekitar 1-2 menit hingga status berubah menjadi **`Live ✅`**.
4. Anda akan mendapatkan URL publik HTTPS gratis, contoh:
   ```text
   https://mahjong-vip-3d.onrender.com
   ```

---

## 🎮 Cara Bermain & Berbagi Link
1. Buka URL tersebut di browser Laptop / HP mana saja.
2. Game langsung tampil dalam tampilan kasino 3D mewah.
3. Login Tamu / Email terhubung langsung ke database Supabase.
4. Buat Meja di **Ruangan VIP** dan bagikan link `https://mahjong-vip-3d.onrender.com/?room=KODEXX` ke teman Anda untuk main bersama secara *real-time*!
