# 📱 Panduan Ekspor APK & Google Play Store (Tanpa Android Studio)

Web App **Mahjong VIP 3D** ini telah dikonfigurasi penuh dengan standar **Progressive Web App (PWA)**, lengkap dengan `manifest.json`, `sw.js` (Service Worker Offline), dan icon HD (192x192 & 512x512).

---

## 🚀 Langkah 1: Hosting Game ke Web (Gratis / Cepat)
Pilih salah satu layanan hosting web statis gratis (Vercel, Netlify, Cloudflare Pages, atau GitHub Pages):

### Opsi A: Menggunakan Vercel / Netlify (Paling Mudah)
1. Unggah folder `d:\Project SS\Mahjong\web-client\` ke akun GitHub Anda atau langsung drag-and-drop folder ke [Netlify Drop](https://app.netlify.com/drop).
2. Dapatkan URL website publik HTTPS Anda (misal: `https://mahjong-vip-3d.vercel.app`).

---

## 📦 Langkah 2: Generate APK / AAB dengan PWABuilder
1. Buka browser dan kunjungi: **[https://www.pwabuilder.com/](https://www.pwabuilder.com/)**
2. Masukkan URL game Anda (misal: `https://mahjong-vip-3d.vercel.app`) lalu klik **Start**.
3. PWABuilder akan memverifikasi manifest, icon, dan service worker (semua akan bernilai hijau/valid).
4. Klik tombol **Package for Stores** -> Pilih **Android (Google Play)**.
5. Isi informasi aplikasi:
   - **Package ID**: `com.yourname.mahjongvip3d`
   - **App Name**: `Mahjong VIP 3D`
   - **Signing Key**: Pilih *Generate New Key* (atau gunakan keystore Anda sendiri).
6. Klik **Download Package**.
7. Anda akan langsung mendapatkan file **`.apk`** (untuk diuji langsung di HP Android) dan **`.aab`** (Android App Bundle untuk diunggah ke Google Play Console).

---

## 🎮 Cara Menjalankan Game Secara Lokal di Komputer
Cukup jalankan file:
`d:\Project SS\Mahjong\web-client\start-game.bat` atau buka `http://localhost:3000/`.
