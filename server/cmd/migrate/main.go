package main

import (
	"bufio"
	"database/sql"
	"fmt"
	"log"
	"os"
	"path/filepath"
	"strings"
	"time"

	_ "github.com/lib/pq"
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

func main() {
	fmt.Println("==================================================================")
	fmt.Println(" 🀄 MAHJONG CUSTOM v1.0 - DATABASE UPLOADER & MIGRATION TOOL")
	fmt.Println(" Target: Supabase Cloud PostgreSQL")
	fmt.Println("==================================================================")

	// Baca file .env jika ada
	env := loadEnv(".env")
	if len(env) == 0 {
		env = loadEnv("server/.env")
	}

	dbURL := os.Getenv("DATABASE_URL")
	if dbURL == "" {
		dbURL = env["DATABASE_URL"]
	}

	if dbURL == "" {
		host := env["DB_HOST"]
		port := env["DB_PORT"]
		user := env["DB_USER"]
		password := env["DB_PASSWORD"]
		dbname := env["DB_NAME"]
		sslmode := env["DB_SSLMODE"]
		if sslmode == "" {
			sslmode = "require"
		}
		if host != "" && user != "" {
			dbURL = fmt.Sprintf("host=%s port=%s user=%s password=%s dbname=%s sslmode=%s",
				host, port, user, password, dbname, sslmode)
		}
	}

	if dbURL == "" {
		log.Fatalf("❌ ERROR: DATABASE_URL tidak ditemukan di .env atau environment variable!")
	}

	fmt.Println("📡 Menghubungkan ke Supabase PostgreSQL...")
	db, err := sql.Open("postgres", dbURL)
	if err != nil {
		log.Fatalf("❌ Gagal membuka driver database: %v", err)
	}
	defer db.Close()

	db.SetConnMaxLifetime(time.Minute * 3)
	db.SetMaxOpenConns(5)
	db.SetMaxIdleConns(2)

	// Test koneksi
	if err := db.Ping(); err != nil {
		log.Fatalf("❌ Gagal terhubung ke Supabase PostgreSQL (Ping failed): %v\nPeriksa koneksi internet atau password database Anda.", err)
	}
	fmt.Println("✅ Koneksi ke Supabase PostgreSQL BERHASIL!")

	// Cari file SQL migrasi
	schemaFiles := []string{
		"migrations/002_production_mahjong_schema.sql",
		"server/migrations/002_production_mahjong_schema.sql",
		"../migrations/002_production_mahjong_schema.sql",
	}

	var schemaPath string
	for _, f := range schemaFiles {
		if _, err := os.Stat(f); err == nil {
			schemaPath = f
			break
		}
	}

	if schemaPath == "" {
		// Cek direktori absolut
		cwd, _ := os.Getwd()
		alt := filepath.Join(cwd, "migrations", "002_production_mahjong_schema.sql")
		if _, err := os.Stat(alt); err == nil {
			schemaPath = alt
		} else {
			log.Fatalf("❌ File migrasi '002_production_mahjong_schema.sql' tidak ditemukan!")
		}
	}

	fmt.Printf("📄 Membaca file skema: %s\n", schemaPath)
	sqlBytes, err := os.ReadFile(schemaPath)
	if err != nil {
		log.Fatalf("❌ Gagal membaca file SQL: %v", err)
	}

	sqlContent := string(sqlBytes)
	fmt.Printf("🚀 Menjalankan migrasi SQL (%d bytes)...\n", len(sqlContent))

	start := time.Now()
	_, err = db.Exec(sqlContent)
	if err != nil {
		log.Fatalf("❌ Gagal mengeksekusi SQL Migrasi: %v", err)
	}
	elapsed := time.Since(start)

	fmt.Printf("🎉 Migrasi Berhasil Dijalankan dalam %v!\n\n", elapsed)

	// Verifikasi Tabel yang Terbuat
	fmt.Println("📊 DAFTAR TABEL YANG TERBUAT DI SUPABASE:")
	fmt.Println("------------------------------------------------------------------")
	rows, err := db.Query(`
		SELECT table_name 
		FROM information_schema.tables 
		WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
		ORDER BY table_name;
	`)
	if err != nil {
		log.Printf("⚠️ Gagal mengambil daftar tabel: %v", err)
	} else {
		defer rows.Close()
		count := 0
		for rows.Next() {
			var tableName string
			if err := rows.Scan(&tableName); err == nil {
				count++
				fmt.Printf("  [%d] 📁 Tabel: %s\n", count, tableName)
			}
		}
	}

	// Verifikasi Views
	fmt.Println("\n👁️  DAFTAR VIEWS YANG TERBUAT:")
	fmt.Println("------------------------------------------------------------------")
	viewRows, err := db.Query(`
		SELECT table_name 
		FROM information_schema.views 
		WHERE table_schema = 'public'
		ORDER BY table_name;
	`)
	if err != nil {
		log.Printf("⚠️ Gagal mengambil daftar view: %v", err)
	} else {
		defer viewRows.Close()
		vCount := 0
		for viewRows.Next() {
			var viewName string
			if err := viewRows.Scan(&viewName); err == nil {
				vCount++
				fmt.Printf("  [%d] 🔍 View: %s\n", vCount, viewName)
			}
		}
	}

	fmt.Println("==================================================================")
	fmt.Println(" 🏆 STATUS: DATABASE MAHJONG CUSTOM v1.0 SIAP DIGUNAKAN (100% READY)!")
	fmt.Println("==================================================================")
}
