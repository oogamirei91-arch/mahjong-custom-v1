using System.Collections.Generic;
using UnityEngine;

namespace Mahjong.Procedural
{
    /// <summary>
    /// ProceduralTileAtlas: Generator Tekstur Atlas 2D Otomatis Ultra-HD (2048x2048)
    /// untuk seluruh 144 Ubin Mahjong standar dengan simbol tebal, jernih, kontras tinggi,
    /// kanji tradisional, bambu berpola, roda pin lingkaran, serta badge indikator di sudut ubin.
    /// 
    /// Format Grid 9 Kolom x 5 Baris:
    /// - Baris 0: Characters / Wan (1 - 9)
    /// - Baris 1: Bamboo / Sou (1 - 9)
    /// - Baris 2: Dots / Pin (1 - 9)
    /// - Baris 3: Honors (East, South, West, North, Red Dragon, Green Dragon, White Dragon)
    /// - Baris 4: Bonus (Flowers 1-4 & Seasons 1-4)
    /// </summary>
    public class ProceduralTileAtlas : MonoBehaviour
    {
        public static ProceduralTileAtlas Instance { get; private set; }

        [Header("Pengaturan Resolusi Tekstur")]
        public int atlasWidth = 2048;
        public int atlasHeight = 2048;

        public Texture2D GeneratedAtlas { get; private set; }

        // Palette Warna VIP Casino Mahjong
        private readonly Color colIvoryBase = new Color(0.985f, 0.980f, 0.955f, 1.0f);
        private readonly Color colBevelGold = new Color(0.86f, 0.72f, 0.38f, 0.95f);
        private readonly Color colDeepNavy  = new Color(0.06f, 0.18f, 0.58f, 1.0f);
        private readonly Color colRubyRed   = new Color(0.86f, 0.10f, 0.12f, 1.0f);
        private readonly Color colJadeGreen = new Color(0.04f, 0.54f, 0.24f, 1.0f);
        private readonly Color colCharcoal  = new Color(0.12f, 0.12f, 0.14f, 1.0f);

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            GenerateFullAtlas();
        }

        [ContextMenu("Regenerate Full Atlas")]
        public void GenerateFullAtlas()
        {
            GeneratedAtlas = new Texture2D(atlasWidth, atlasHeight, TextureFormat.RGBA32, true);
            GeneratedAtlas.name = "Tex_Mahjong_Procedural_Atlas";
            GeneratedAtlas.filterMode = FilterMode.Bilinear;
            GeneratedAtlas.wrapMode = TextureWrapMode.Clamp;

            // 1. Bersihkan seluruh bidang dengan warna dasar Ivory Pearl
            Color[] clearPixels = new Color[atlasWidth * atlasHeight];
            for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = colIvoryBase;
            GeneratedAtlas.SetPixels(clearPixels);

            int cols = 9;
            int rows = 5;
            int cellW = atlasWidth / cols;
            int cellH = atlasHeight / rows;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int startX = c * cellW;
                    int startY = (rows - 1 - r) * cellH; // Y Texture2D dari bawah ke atas
                    
                    // Gambar bingkai gading & bevel emas
                    DrawTileBevelBorder(startX, startY, cellW, cellH);
                    
                    // Gambar simbol utama ubin
                    DrawTileCell(startX, startY, cellW, cellH, r, c);
                }
            }

            GeneratedAtlas.Apply();
            Debug.Log("[ProceduralTileAtlas] Texture Atlas Mahjong Ultra-HD 2048x2048 berhasil dibuat!");
        }

        private void DrawTileBevelBorder(int startX, int startY, int w, int h)
        {
            int margin = 5;
            int borderThick = 4;

            // Garis bingkai luar
            for (int x = margin; x < w - margin; x++)
            {
                for (int t = 0; t < borderThick; t++)
                {
                    SetPixelSafe(startX + x, startY + margin + t, colBevelGold);
                    SetPixelSafe(startX + x, startY + h - margin - 1 - t, colBevelGold);
                }
            }
            for (int y = margin; y < h - margin; y++)
            {
                for (int t = 0; t < borderThick; t++)
                {
                    SetPixelSafe(startX + margin + t, startY + y, colBevelGold);
                    SetPixelSafe(startX + w - margin - 1 - t, startY + y, colBevelGold);
                }
            }
        }

        private void DrawTileCell(int startX, int startY, int w, int h, int row, int col)
        {
            int cx = startX + w / 2;
            int cy = startY + h / 2;

            switch (row)
            {
                case 0: // Characters / Wan (1 - 9)
                    DrawWanTile(startX, startY, cx, cy, w, h, col + 1);
                    break;
                case 1: // Bamboo / Sou (1 - 9)
                    DrawBambooTile(startX, startY, cx, cy, w, h, col + 1);
                    break;
                case 2: // Dots / Pin (1 - 9)
                    DrawDotsTile(startX, startY, cx, cy, w, h, col + 1);
                    break;
                case 3: // Honors: Winds (0..3) & Dragons (4..6)
                    DrawHonorTile(startX, startY, cx, cy, w, h, col);
                    break;
                case 4: // Bonus: Flowers (0..3) & Seasons (4..7)
                    DrawBonusTile(startX, startY, cx, cy, w, h, col);
                    break;
            }
        }

        // =========================================================================
        // 1. CHARACTERS / WAN TILES (1 - 9)
        // =========================================================================

        private void DrawWanTile(int startX, int startY, int cx, int cy, int w, int h, int num)
        {
            // Gambar Badge Angka di Sudut Kiri Atas (e.g. "1W", "2W"...)
            DrawCornerBadge(startX + 14, startY + h - 42, $"{num}W", colRubyRed);

            // Bagian Atas: Angka Kanji (1..9)
            int numCenterY = cy + 62;
            DrawKanjiDigit(cx, numCenterY, num, colDeepNavy);

            // Bagian Bawah: Karakter Tradisional "萬" (Wan)
            int wanCenterY = cy - 65;
            DrawKanjiWan(cx, wanCenterY, colRubyRed);
        }

        private void DrawKanjiDigit(int cx, int cy, int num, Color col)
        {
            int t = 10; // Tebal garis
            switch (num)
            {
                case 1: // 一
                    DrawHLine(cx - 55, cx + 55, cy, t, col);
                    break;
                case 2: // 二
                    DrawHLine(cx - 40, cx + 40, cy + 24, t, col);
                    DrawHLine(cx - 60, cx + 60, cy - 24, t, col);
                    break;
                case 3: // 三
                    DrawHLine(cx - 42, cx + 42, cy + 32, t - 2, col);
                    DrawHLine(cx - 30, cx + 30, cy, t - 2, col);
                    DrawHLine(cx - 60, cx + 60, cy - 32, t - 1, col);
                    break;
                case 4: // 四
                    DrawRectOutline(cx - 48, cy - 38, 96, 76, t, col);
                    DrawVLine(cx - 16, cy - 24, cy + 24, t - 2, col);
                    DrawVLine(cx + 16, cy - 24, cy + 24, t - 2, col);
                    DrawHLine(cx - 16, cx + 16, cy - 16, t - 2, col);
                    break;
                case 5: // 五
                    DrawHLine(cx - 50, cx + 50, cy + 35, t, col);
                    DrawVLine(cx - 24, cy - 30, cy + 35, t, col);
                    DrawHLine(cx - 24, cx + 30, cy + 4, t, col);
                    DrawVLine(cx + 30, cy - 35, cy + 4, t, col);
                    DrawHLine(cx - 58, cx + 58, cy - 35, t, col);
                    break;
                case 6: // 六
                    DrawVLine(cx, cy + 22, cy + 42, t + 2, col);
                    DrawHLine(cx - 55, cx + 55, cy + 18, t, col);
                    DrawLine(cx - 16, cy + 12, cx - 48, cy - 38, t, col);
                    DrawLine(cx + 16, cy + 12, cx + 48, cy - 38, t, col);
                    break;
                case 7: // 七
                    DrawHLine(cx - 54, cx + 54, cy + 8, t, col);
                    DrawVLine(cx - 6, cy - 36, cy + 36, t, col);
                    DrawHLine(cx - 6, cx + 42, cy - 36, t, col);
                    DrawVLine(cx + 42, cy - 36, cy - 12, t, col);
                    break;
                case 8: // 八
                    DrawLine(cx - 14, cy + 36, cx - 48, cy - 38, t + 2, col);
                    DrawLine(cx + 14, cy + 36, cx + 52, cy - 38, t + 2, col);
                    break;
                case 9: // 九
                    DrawLine(cx - 22, cy + 38, cx - 48, cy - 38, t + 2, col);
                    DrawHLine(cx - 40, cx + 32, cy + 18, t, col);
                    DrawVLine(cx + 32, cy - 36, cy + 18, t, col);
                    DrawHLine(cx + 32, cx + 55, cy - 36, t, col);
                    break;
            }
        }

        private void DrawKanjiWan(int cx, int cy, Color col)
        {
            int t = 9;
            // Garis horizontal atas
            DrawHLine(cx - 52, cx + 52, cy + 38, t, col);
            // Kaki kiri atas melengkung
            DrawLine(cx - 24, cy + 38, cx - 46, cy + 8, t, col);
            // Garis horizontal tengah
            DrawHLine(cx - 42, cx + 42, cy + 8, t, col);
            // Kotak tengah & silang
            DrawRectOutline(cx - 36, cy - 36, 72, 44, t - 1, col);
            DrawVLine(cx, cy - 36, cy + 8, t - 1, col);
            DrawHLine(cx - 36, cx + 36, cy - 14, t - 1, col);
            // Kaki bawah
            DrawLine(cx - 24, cy - 36, cx - 44, cy - 54, t, col);
            DrawLine(cx + 24, cy - 36, cx + 44, cy - 54, t, col);
        }

        // =========================================================================
        // 2. BAMBOO / SOU TILES (1 - 9)
        // =========================================================================

        private void DrawBambooTile(int startX, int startY, int cx, int cy, int w, int h, int count)
        {
            DrawCornerBadge(startX + 14, startY + h - 42, $"{count}B", colJadeGreen);

            if (count == 1)
            {
                // 1 Sou: Burung Merak / Pipit Mahjong Ikonik
                DrawPeacockBird1Sou(cx, cy);
                return;
            }

            int stickW = 14;
            int stickH = 68;

            switch (count)
            {
                case 2:
                    DrawBambooStick(cx, cy + 55, stickW, stickH, colJadeGreen);
                    DrawBambooStick(cx, cy - 55, stickW, stickH, colJadeGreen);
                    break;
                case 3:
                    DrawBambooStick(cx, cy + 65, stickW, stickH - 10, colDeepNavy);
                    DrawBambooStick(cx - 36, cy - 45, stickW, stickH, colJadeGreen);
                    DrawBambooStick(cx + 36, cy - 45, stickW, stickH, colJadeGreen);
                    break;
                case 4:
                    DrawBambooStick(cx - 36, cy + 55, stickW, stickH, colJadeGreen);
                    DrawBambooStick(cx + 36, cy + 55, stickW, stickH, colDeepNavy);
                    DrawBambooStick(cx - 36, cy - 55, stickW, stickH, colDeepNavy);
                    DrawBambooStick(cx + 36, cy - 55, stickW, stickH, colJadeGreen);
                    break;
                case 5:
                    DrawBambooStick(cx - 42, cy + 60, stickW, stickH - 8, colJadeGreen);
                    DrawBambooStick(cx + 42, cy + 60, stickW, stickH - 8, colDeepNavy);
                    DrawBambooStick(cx, cy, stickW + 2, stickH - 4, colRubyRed);
                    DrawBambooStick(cx - 42, cy - 60, stickW, stickH - 8, colDeepNavy);
                    DrawBambooStick(cx + 42, cy - 60, stickW, stickH - 8, colJadeGreen);
                    break;
                case 6:
                    for (int c = 0; c < 3; c++)
                    {
                        int x = cx - 44 + (c * 44);
                        DrawBambooStick(x, cy + 55, stickW, stickH - 8, colJadeGreen);
                        DrawBambooStick(x, cy - 55, stickW, stickH - 8, colJadeGreen);
                    }
                    break;
                case 7:
                    DrawBambooStick(cx, cy + 78, stickW, 46, colRubyRed);
                    DrawBambooStick(cx - 44, cy + 18, stickW, 48, colJadeGreen);
                    DrawBambooStick(cx + 44, cy + 18, stickW, 48, colJadeGreen);
                    for (int c = 0; c < 4; c++)
                    {
                        int x = cx - 48 + (c * 32);
                        DrawBambooStick(x, cy - 62, stickW - 2, 58, colJadeGreen);
                    }
                    break;
                case 8:
                    DrawBambooStick(cx - 48, cy + 72, stickW - 2, 44, colJadeGreen);
                    DrawBambooStick(cx - 16, cy + 50, stickW - 2, 44, colDeepNavy);
                    DrawBambooStick(cx + 16, cy + 50, stickW - 2, 44, colDeepNavy);
                    DrawBambooStick(cx + 48, cy + 72, stickW - 2, 44, colJadeGreen);

                    DrawBambooStick(cx - 48, cy - 50, stickW - 2, 44, colJadeGreen);
                    DrawBambooStick(cx - 16, cy - 72, stickW - 2, 44, colDeepNavy);
                    DrawBambooStick(cx + 16, cy - 72, stickW - 2, 44, colDeepNavy);
                    DrawBambooStick(cx + 48, cy - 50, stickW - 2, 44, colJadeGreen);
                    break;
                case 9:
                    for (int r = 0; r < 3; r++)
                    {
                        int y = cy + 68 - (r * 68);
                        Color rowCol = (r == 0) ? colDeepNavy : (r == 1 ? colRubyRed : colJadeGreen);
                        for (int c = 0; c < 3; c++)
                        {
                            int x = cx - 44 + (c * 44);
                            DrawBambooStick(x, y, stickW - 2, 50, rowCol);
                        }
                    }
                    break;
            }
        }

        private void DrawBambooStick(int cx, int cy, int w, int h, Color col)
        {
            // Badan batang bambu
            DrawFilledRect(cx - w / 2, cy - h / 2, w, h, col);
            // Sambungan ruas bambu (knots)
            DrawFilledCircle(cx, cy, w / 2 + 2, col);
            DrawFilledCircle(cx, cy + h / 2 - 4, w / 2 + 1, col);
            DrawFilledCircle(cx, cy - h / 2 + 4, w / 2 + 1, col);
            // Garis tengah ruas putih
            DrawFilledRect(cx - 2, cy - h / 2 + 8, 4, h - 16, Color.white);
            DrawFilledCircle(cx, cy, 3, Color.white);
        }

        private void DrawPeacockBird1Sou(int cx, int cy)
        {
            // Badan Merak
            DrawFilledCircle(cx, cy - 20, 36, colJadeGreen);
            DrawFilledCircle(cx, cy + 30, 22, colJadeGreen);
            // Mahkota & Paruh
            DrawFilledCircle(cx + 14, cy + 42, 10, colRubyRed);
            DrawFilledCircle(cx + 8, cy + 34, 4, Color.white);
            // Ekor Kipas
            DrawFilledCircle(cx - 28, cy - 10, 16, colRubyRed);
            DrawFilledCircle(cx + 28, cy - 10, 16, colDeepNavy);
            DrawFilledCircle(cx - 36, cy + 18, 14, colJadeGreen);
            DrawFilledCircle(cx + 36, cy + 18, 14, colJadeGreen);
            DrawRing(cx, cy - 20, 42, colBevelGold, 4);
        }

        // =========================================================================
        // 3. DOTS / PIN TILES (1 - 9)
        // =========================================================================

        private void DrawDotsTile(int startX, int startY, int cx, int cy, int w, int h, int count)
        {
            DrawCornerBadge(startX + 14, startY + h - 42, $"{count}D", colDeepNavy);

            int r = 26; // Radius pin standar
            switch (count)
            {
                case 1:
                    // 1 Pin: Roda Matahari Besar Mahjong
                    DrawFilledCircle(cx, cy, 68, colRubyRed);
                    DrawRing(cx, cy, 76, colJadeGreen, 10);
                    DrawRing(cx, cy, 84, colBevelGold, 4);
                    DrawFilledCircle(cx, cy, 24, Color.white);
                    DrawFilledCircle(cx, cy, 14, colRubyRed);
                    break;
                case 2:
                    DrawOrnatePin(cx, cy + 58, r + 4, colJadeGreen);
                    DrawOrnatePin(cx, cy - 58, r + 4, colDeepNavy);
                    break;
                case 3:
                    DrawOrnatePin(cx - 44, cy + 62, r + 2, colDeepNavy);
                    DrawOrnatePin(cx, cy, r + 2, colRubyRed);
                    DrawOrnatePin(cx + 44, cy - 62, r + 2, colJadeGreen);
                    break;
                case 4:
                    DrawOrnatePin(cx - 42, cy + 56, r + 2, colDeepNavy);
                    DrawOrnatePin(cx + 42, cy + 56, r + 2, colJadeGreen);
                    DrawOrnatePin(cx - 42, cy - 56, r + 2, colJadeGreen);
                    DrawOrnatePin(cx + 42, cy - 56, r + 2, colDeepNavy);
                    break;
                case 5:
                    DrawOrnatePin(cx - 46, cy + 62, r, colDeepNavy);
                    DrawOrnatePin(cx + 46, cy + 62, r, colJadeGreen);
                    DrawOrnatePin(cx, cy, r + 4, colRubyRed);
                    DrawOrnatePin(cx - 46, cy - 62, r, colJadeGreen);
                    DrawOrnatePin(cx + 46, cy - 62, r, colDeepNavy);
                    break;
                case 6:
                    for (int row = 0; row < 3; row++)
                    {
                        int y = cy + 62 - (row * 62);
                        DrawOrnatePin(cx - 42, y, r, colJadeGreen);
                        DrawOrnatePin(cx + 42, y, r, colRubyRed);
                    }
                    break;
                case 7:
                    DrawOrnatePin(cx - 42, cy + 74, r - 3, colJadeGreen);
                    DrawOrnatePin(cx, cy + 48, r - 3, colJadeGreen);
                    DrawOrnatePin(cx + 42, cy + 22, r - 3, colJadeGreen);
                    DrawOrnatePin(cx - 40, cy - 30, r - 2, colRubyRed);
                    DrawOrnatePin(cx + 40, cy - 30, r - 2, colRubyRed);
                    DrawOrnatePin(cx - 40, cy - 76, r - 2, colRubyRed);
                    DrawOrnatePin(cx + 40, cy - 76, r - 2, colRubyRed);
                    break;
                case 8:
                    for (int row = 0; row < 4; row++)
                    {
                        int y = cy + 72 - (row * 48);
                        DrawOrnatePin(cx - 42, y, r - 4, colDeepNavy);
                        DrawOrnatePin(cx + 42, y, r - 4, colDeepNavy);
                    }
                    break;
                case 9:
                    for (int row = 0; row < 3; row++)
                    {
                        int y = cy + 64 - (row * 64);
                        Color rowCol = (row == 0) ? colDeepNavy : (row == 1 ? colRubyRed : colJadeGreen);
                        for (int col = 0; col < 3; col++)
                        {
                            int x = cx - 44 + (col * 44);
                            DrawOrnatePin(x, y, r - 3, rowCol);
                        }
                    }
                    break;
            }
        }

        private void DrawOrnatePin(int cx, int cy, int r, Color col)
        {
            DrawFilledCircle(cx, cy, r, col);
            DrawRing(cx, cy, r + 4, colBevelGold, 2);
            DrawFilledCircle(cx, cy, r / 3 + 1, Color.white);
            DrawFilledCircle(cx, cy, r / 5, col);
        }

        // =========================================================================
        // 4. HONORS: WINDS (0..3) & DRAGONS (4..6)
        // =========================================================================

        private void DrawHonorTile(int startX, int startY, int cx, int cy, int w, int h, int colIdx)
        {
            int t = 11;
            switch (colIdx)
            {
                case 0: // East (東)
                    DrawCornerBadge(startX + 14, startY + h - 42, "E", colDeepNavy);
                    DrawKanjiEast(cx, cy, colDeepNavy);
                    break;
                case 1: // South (南)
                    DrawCornerBadge(startX + 14, startY + h - 42, "S", colDeepNavy);
                    DrawKanjiSouth(cx, cy, colDeepNavy);
                    break;
                case 2: // West (西)
                    DrawCornerBadge(startX + 14, startY + h - 42, "W", colDeepNavy);
                    DrawKanjiWest(cx, cy, colDeepNavy);
                    break;
                case 3: // North (北)
                    DrawCornerBadge(startX + 14, startY + h - 42, "N", colDeepNavy);
                    DrawKanjiNorth(cx, cy, colDeepNavy);
                    break;
                case 4: // Red Dragon (中 - Chun)
                    DrawCornerBadge(startX + 14, startY + h - 42, "C", colRubyRed);
                    DrawRectOutline(cx - 50, cy - 36, 100, 72, t + 2, colRubyRed);
                    DrawVLine(cx, cy - 80, cy + 80, t + 4, colRubyRed);
                    break;
                case 5: // Green Dragon (發 - Fa)
                    DrawCornerBadge(startX + 14, startY + h - 42, "F", colJadeGreen);
                    DrawKanjiFa(cx, cy, colJadeGreen);
                    break;
                case 6: // White Dragon (白 - Bai / Blank Frame)
                    DrawCornerBadge(startX + 14, startY + h - 42, "P", colDeepNavy);
                    DrawRectOutline(cx - 58, cy - 78, 116, 156, t + 2, colDeepNavy);
                    DrawRectOutline(cx - 46, cy - 66, 92, 132, 4, colBevelGold);
                    break;
            }
        }

        private void DrawKanjiEast(int cx, int cy, Color col)
        {
            int t = 10;
            DrawHLine(cx - 58, cx + 58, cy + 54, t, col);
            DrawRectOutline(cx - 44, cy - 14, 88, 54, t, col);
            DrawHLine(cx - 44, cx + 44, cy + 13, t, col);
            DrawVLine(cx, cy - 75, cy + 75, t + 2, col);
            DrawLine(cx - 18, cy - 20, cx - 52, cy - 68, t, col);
            DrawLine(cx + 18, cy - 20, cx + 52, cy - 68, t, col);
        }

        private void DrawKanjiSouth(int cx, int cy, Color col)
        {
            int t = 9;
            DrawHLine(cx - 46, cx + 46, cy + 62, t, col);
            DrawVLine(cx, cy + 44, cy + 76, t, col);
            DrawRectOutline(cx - 52, cy - 65, 104, 105, t, col);
            DrawVLine(cx - 18, cy - 40, cy + 20, t, col);
            DrawVLine(cx + 18, cy - 40, cy + 20, t, col);
            DrawHLine(cx - 36, cx + 36, cy - 10, t, col);
        }

        private void DrawKanjiWest(int cx, int cy, Color col)
        {
            int t = 10;
            DrawHLine(cx - 56, cx + 56, cy + 58, t, col);
            DrawRectOutline(cx - 48, cy - 60, 96, 106, t, col);
            DrawVLine(cx - 18, cy - 40, cy + 40, t, col);
            DrawVLine(cx + 18, cy - 40, cy + 40, t, col);
            DrawHLine(cx - 32, cx + 32, cy - 20, t, col);
        }

        private void DrawKanjiNorth(int cx, int cy, Color col)
        {
            int t = 10;
            // Kiri
            DrawVLine(cx - 24, cy - 60, cy + 60, t, col);
            DrawHLine(cx - 52, cx - 24, cy + 8, t, col);
            DrawLine(cx - 52, cy - 48, cx - 24, cy - 8, t, col);
            // Kanan
            DrawLine(cx + 16, cy + 56, cx + 16, cy - 35, t, col);
            DrawHLine(cx + 16, cx + 52, cy - 35, t, col);
            DrawVLine(cx + 52, cy - 35, cy + 15, t, col);
        }

        private void DrawKanjiFa(int cx, int cy, Color col)
        {
            int t = 9;
            // Atas
            DrawLine(cx - 45, cy + 65, cx, cy + 40, t, col);
            DrawLine(cx + 45, cy + 65, cx, cy + 40, t, col);
            DrawHLine(cx - 48, cx + 48, cy + 30, t, col);
            // Tengah & Bawah
            DrawVLine(cx - 24, cy - 65, cy + 20, t, col);
            DrawVLine(cx + 24, cy - 65, cy + 20, t, col);
            DrawHLine(cx - 44, cx + 44, cy - 10, t, col);
            DrawLine(cx - 16, cy - 10, cx - 48, cy - 58, t, col);
            DrawLine(cx + 16, cy - 10, cx + 48, cy - 58, t, col);
        }

        // =========================================================================
        // 5. BONUS: FLOWERS (0..3) & SEASONS (4..7)
        // =========================================================================

        private void DrawBonusTile(int startX, int startY, int cx, int cy, int w, int h, int colIdx)
        {
            if (colIdx < 4)
            {
                // Bunga 1..4 (Plum, Orchid, Chrysanthemum, Bamboo)
                DrawCornerBadge(startX + 14, startY + h - 42, $"F{colIdx + 1}", colRubyRed);
                DrawFilledCircle(cx, cy, 48, colRubyRed);
                DrawRing(cx, cy, 58, colBevelGold, 5);
                DrawFlowerPetals(cx, cy, colJadeGreen);
            }
            else if (colIdx < 8)
            {
                // Musim 1..4 (Spring, Summer, Autumn, Winter)
                DrawCornerBadge(startX + 14, startY + h - 42, $"S{colIdx - 3}", colDeepNavy);
                DrawFilledCircle(cx, cy, 48, colDeepNavy);
                DrawRing(cx, cy, 58, colBevelGold, 5);
                DrawFlowerPetals(cx, cy, colRubyRed);
            }
        }

        private void DrawFlowerPetals(int cx, int cy, Color petalCol)
        {
            DrawFilledCircle(cx, cy + 32, 16, petalCol);
            DrawFilledCircle(cx, cy - 32, 16, petalCol);
            DrawFilledCircle(cx + 32, cy, 16, petalCol);
            DrawFilledCircle(cx - 32, cy, 16, petalCol);
            DrawFilledCircle(cx, cy, 14, Color.white);
        }

        // =========================================================================
        // CORNER BADGE & BITMAP FONT GLYPHS (5x7)
        // =========================================================================

        private void DrawCornerBadge(int x, int y, string text, Color col)
        {
            int cursorX = x;
            for (int i = 0; i < text.Length; i++)
            {
                DrawBitmapChar5x7(cursorX, y, text[i], col, 3);
                cursorX += 18; // Spasi antar huruf
            }
        }

        private void DrawBitmapChar5x7(int startX, int startY, char c, Color col, int scale)
        {
            byte[] rows = GetGlyphRows(c);
            if (rows == null) return;

            for (int r = 0; r < 7; r++)
            {
                byte rowByte = rows[r];
                for (int colBit = 0; colBit < 5; colBit++)
                {
                    if ((rowByte & (1 << (4 - colBit))) != 0)
                    {
                        DrawFilledRect(startX + (colBit * scale), startY - (r * scale), scale, scale, col);
                    }
                }
            }
        }

        private byte[] GetGlyphRows(char c)
        {
            switch (c)
            {
                case '1': return new byte[] { 0x04, 0x0C, 0x04, 0x04, 0x04, 0x04, 0x0E };
                case '2': return new byte[] { 0x0E, 0x11, 0x01, 0x06, 0x08, 0x10, 0x1F };
                case '3': return new byte[] { 0x1F, 0x02, 0x04, 0x06, 0x01, 0x11, 0x0E };
                case '4': return new byte[] { 0x02, 0x06, 0x0A, 0x12, 0x1F, 0x02, 0x02 };
                case '5': return new byte[] { 0x1F, 0x10, 0x1E, 0x01, 0x01, 0x11, 0x0E };
                case '6': return new byte[] { 0x06, 0x08, 0x10, 0x1E, 0x11, 0x11, 0x0E };
                case '7': return new byte[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x08, 0x08 };
                case '8': return new byte[] { 0x0E, 0x11, 0x11, 0x0E, 0x11, 0x11, 0x0E };
                case '9': return new byte[] { 0x0E, 0x11, 0x11, 0x0F, 0x01, 0x02, 0x0C };
                case 'W': return new byte[] { 0x11, 0x11, 0x11, 0x15, 0x15, 0x1B, 0x11 };
                case 'B': return new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x11, 0x11, 0x1E };
                case 'D': return new byte[] { 0x1C, 0x12, 0x11, 0x11, 0x11, 0x12, 0x1C };
                case 'E': return new byte[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x1F };
                case 'S': return new byte[] { 0x0F, 0x10, 0x10, 0x0E, 0x01, 0x01, 0x1E };
                case 'N': return new byte[] { 0x11, 0x19, 0x15, 0x13, 0x11, 0x11, 0x11 };
                case 'C': return new byte[] { 0x0E, 0x11, 0x10, 0x10, 0x10, 0x11, 0x0E };
                case 'F': return new byte[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x10 };
                case 'P': return new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x10, 0x10, 0x10 };
                default: return null;
            }
        }

        // =========================================================================
        // PRIMITIF GRAFIK & RASTERIZER GARIS/KOTAK/LINGKARAN
        // =========================================================================

        private void DrawHLine(int x0, int x1, int y, int thickness, Color col)
        {
            int minX = Mathf.Min(x0, x1);
            int maxX = Mathf.Max(x0, x1);
            int ht = thickness / 2;
            for (int dy = -ht; dy <= ht; dy++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    SetPixelSafe(x, y + dy, col);
                }
            }
        }

        private void DrawVLine(int x, int y0, int y1, int thickness, Color col)
        {
            int minY = Mathf.Min(y0, y1);
            int maxY = Mathf.Max(y0, y1);
            int ht = thickness / 2;
            for (int dx = -ht; dx <= ht; dx++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    SetPixelSafe(x + dx, y, col);
                }
            }
        }

        private void DrawLine(int x0, int y0, int x1, int y1, int thickness, Color col)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int ht = thickness / 2;
            while (true)
            {
                for (int ox = -ht; ox <= ht; ox++)
                {
                    for (int oy = -ht; oy <= ht; oy++)
                    {
                        SetPixelSafe(x0 + ox, y0 + oy, col);
                    }
                }

                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        private void DrawRectOutline(int x, int y, int w, int h, int thickness, Color col)
        {
            DrawFilledRect(x, y, w, thickness, col);                     // Bawah
            DrawFilledRect(x, y + h - thickness, w, thickness, col);     // Atas
            DrawFilledRect(x, y, thickness, h, col);                     // Kiri
            DrawFilledRect(x + w - thickness, y, thickness, h, col);     // Kanan
        }

        private void DrawFilledRect(int x, int y, int w, int h, Color col)
        {
            for (int dy = 0; dy < h; dy++)
            {
                for (int dx = 0; dx < w; dx++)
                {
                    SetPixelSafe(x + dx, y + dy, col);
                }
            }
        }

        private void DrawFilledCircle(int cx, int cy, int r, Color col)
        {
            int r2 = r * r;
            for (int dy = -r; dy <= r; dy++)
            {
                int dy2 = dy * dy;
                for (int dx = -r; dx <= r; dx++)
                {
                    if (dx * dx + dy2 <= r2)
                    {
                        SetPixelSafe(cx + dx, cy + dy, col);
                    }
                }
            }
        }

        private void DrawRing(int cx, int cy, int r, Color col, int thickness)
        {
            int rOuter2 = r * r;
            int rInner2 = (r - thickness) * (r - thickness);
            for (int dy = -r; dy <= r; dy++)
            {
                int dy2 = dy * dy;
                for (int dx = -r; dx <= r; dx++)
                {
                    int d2 = dx * dx + dy2;
                    if (d2 <= rOuter2 && d2 >= rInner2)
                    {
                        SetPixelSafe(cx + dx, cy + dy, col);
                    }
                }
            }
        }

        private void SetPixelSafe(int x, int y, Color col)
        {
            if (x >= 0 && x < atlasWidth && y >= 0 && y < atlasHeight)
            {
                GeneratedAtlas.SetPixel(x, y, col);
            }
        }
    }
}
