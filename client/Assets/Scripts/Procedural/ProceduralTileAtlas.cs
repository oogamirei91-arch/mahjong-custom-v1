using System.Collections.Generic;
using UnityEngine;

namespace Mahjong.Procedural
{
    /// <summary>
    /// ProceduralTileAtlas: Generator Tekstur Atlas 2D Otomatis Ultra-HD (2048x2048)
    /// untuk seluruh 144 Ubin Mahjong standar dengan simbol kaligrafi Kanji proporsional,
    /// pin lingkaran sempurna (aspect-ratio corrected), batang bambu bertekstur, serta corner badge tajam.
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

        [Header("Kustom Sprite / Texture Atlas (Opsional)")]
        [Tooltip("Jika diisi dengan Texture2D / Sprite Sheet buatan sendiri (Grid 9x5), game akan memakai gambar ini secara langsung!")]
        public Texture2D customTileAtlasTexture;

        [Header("Pengaturan Resolusi Tekstur")]
        public int atlasWidth = 2048;
        public int atlasHeight = 2048;

        public Texture2D GeneratedAtlas { get; private set; }

        // Rasio koreksi aspek: perbandingan cell atlas (227x409) terhadap mesh ubin 3D (0.044 x 0.062)
        // Ratio = (409.6 / 227.55) * (0.044 / 0.062) = 1.800 * 0.7097 = ~1.277
        private const float AspectCorrectionY = 1.28f;

        // Palette Warna VIP Casino Mahjong
        private readonly Color colIvoryBase  = new Color(0.985f, 0.980f, 0.955f, 1.0f);
        private readonly Color colBevelGold  = new Color(0.86f, 0.72f, 0.38f, 0.95f);
        private readonly Color colDeepNavy   = new Color(0.06f, 0.18f, 0.58f, 1.0f);
        private readonly Color colRubyRed    = new Color(0.86f, 0.10f, 0.12f, 1.0f);
        private readonly Color colJadeGreen  = new Color(0.04f, 0.54f, 0.24f, 1.0f);
        private readonly Color colCharcoal   = new Color(0.12f, 0.12f, 0.14f, 1.0f);

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            GenerateFullAtlas();
        }

        [ContextMenu("Regenerate Full Atlas")]
        public void GenerateFullAtlas()
        {
            // 1. Cek apakah ada Custom Texture yang di-assign via Inspector atau ditaruh di Resources
            if (customTileAtlasTexture == null)
            {
                customTileAtlasTexture = Resources.Load<Texture2D>("CustomMahjongAtlas");
            }

            if (customTileAtlasTexture != null)
            {
                GeneratedAtlas = customTileAtlasTexture;
                Debug.Log("[ProceduralTileAtlas] Menggunakan Custom Sprite / Texture Atlas Impor: " + customTileAtlasTexture.name);
                return;
            }

            GeneratedAtlas = new Texture2D(atlasWidth, atlasHeight, TextureFormat.RGBA32, true);
            GeneratedAtlas.name = "Tex_Mahjong_Procedural_Atlas";
            GeneratedAtlas.filterMode = FilterMode.Bilinear;
            GeneratedAtlas.wrapMode = TextureWrapMode.Clamp;

            // 2. Bersihkan bidang dengan warna dasar Pearl Ivory
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
            int margin = 6;
            int borderThick = 5;

            // Highlight atas dan kiri (Ivory Bright Light)
            Color colHighlight = new Color(1.0f, 1.0f, 0.98f, 1.0f);
            // Shadow bawah dan kanan (Bevel Drop Shadow)
            Color colShadow = new Color(0.84f, 0.78f, 0.70f, 1.0f);

            for (int x = margin; x < w - margin; x++)
            {
                for (int t = 0; t < borderThick; t++)
                {
                    SetPixelSafe(startX + x, startY + margin + t, colShadow); // Bawah
                    SetPixelSafe(startX + x, startY + h - margin - 1 - t, colHighlight); // Atas
                }
            }
            for (int y = margin; y < h - margin; y++)
            {
                for (int t = 0; t < borderThick; t++)
                {
                    SetPixelSafe(startX + margin + t, startY + y, colHighlight); // Kiri
                    SetPixelSafe(startX + w - margin - 1 - t, startY + y, colShadow); // Kanan
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
            // Badge Sudut Kiri Atas
            DrawCornerBadge(startX + 14, startY + h - 38, $"{num}W", colRubyRed);

            // Bagian Atas: Angka Kanji (1..9)
            int numCenterY = cy + 70;
            DrawKanjiDigit(cx, numCenterY, num, colDeepNavy);

            // Bagian Bawah: Karakter Tradisional "萬" (Wan)
            int wanCenterY = cy - 65;
            DrawKanjiWan(cx, wanCenterY, colRubyRed);
        }

        private void DrawKanjiDigit(int cx, int cy, int num, Color col)
        {
            int t = 10;
            switch (num)
            {
                case 1: // 一
                    DrawHLine(cx - 52, cx + 52, cy, t, col);
                    break;
                case 2: // 二
                    DrawHLine(cx - 38, cx + 38, cy + (int)(22 * AspectCorrectionY), t, col);
                    DrawHLine(cx - 56, cx + 56, cy - (int)(22 * AspectCorrectionY), t, col);
                    break;
                case 3: // 三
                    DrawHLine(cx - 40, cx + 40, cy + (int)(28 * AspectCorrectionY), t - 1, col);
                    DrawHLine(cx - 30, cx + 30, cy, t - 1, col);
                    DrawHLine(cx - 56, cx + 56, cy - (int)(28 * AspectCorrectionY), t, col);
                    break;
                case 4: // 四
                    DrawRectOutline(cx - 46, cy - (int)(32 * AspectCorrectionY), 92, (int)(64 * AspectCorrectionY), t, col);
                    DrawVLine(cx - 16, cy - (int)(20 * AspectCorrectionY), cy + (int)(20 * AspectCorrectionY), t - 2, col);
                    DrawVLine(cx + 16, cy - (int)(20 * AspectCorrectionY), cy + (int)(20 * AspectCorrectionY), t - 2, col);
                    DrawHLine(cx - 16, cx + 16, cy - (int)(12 * AspectCorrectionY), t - 2, col);
                    break;
                case 5: // 五
                    DrawHLine(cx - 48, cx + 48, cy + (int)(30 * AspectCorrectionY), t, col);
                    DrawVLine(cx - 22, cy - (int)(26 * AspectCorrectionY), cy + (int)(30 * AspectCorrectionY), t, col);
                    DrawHLine(cx - 22, cx + 28, cy + (int)(4 * AspectCorrectionY), t, col);
                    DrawVLine(cx + 28, cy - (int)(30 * AspectCorrectionY), cy + (int)(4 * AspectCorrectionY), t, col);
                    DrawHLine(cx - 54, cx + 54, cy - (int)(30 * AspectCorrectionY), t, col);
                    break;
                case 6: // 六
                    DrawVLine(cx, cy + (int)(18 * AspectCorrectionY), cy + (int)(36 * AspectCorrectionY), t + 2, col);
                    DrawHLine(cx - 52, cx + 52, cy + (int)(14 * AspectCorrectionY), t, col);
                    DrawLine(cx - 14, cy + (int)(8 * AspectCorrectionY), cx - 44, cy - (int)(32 * AspectCorrectionY), t, col);
                    DrawLine(cx + 14, cy + (int)(8 * AspectCorrectionY), cx + 44, cy - (int)(32 * AspectCorrectionY), t, col);
                    break;
                case 7: // 七
                    DrawHLine(cx - 50, cx + 50, cy + (int)(6 * AspectCorrectionY), t, col);
                    DrawVLine(cx - 6, cy - (int)(32 * AspectCorrectionY), cy + (int)(32 * AspectCorrectionY), t, col);
                    DrawHLine(cx - 6, cx + 38, cy - (int)(32 * AspectCorrectionY), t, col);
                    DrawVLine(cx + 38, cy - (int)(32 * AspectCorrectionY), cy - (int)(10 * AspectCorrectionY), t, col);
                    break;
                case 8: // 八
                    DrawLine(cx - 14, cy + (int)(30 * AspectCorrectionY), cx - 46, cy - (int)(32 * AspectCorrectionY), t + 2, col);
                    DrawLine(cx + 14, cy + (int)(30 * AspectCorrectionY), cx + 48, cy - (int)(32 * AspectCorrectionY), t + 2, col);
                    break;
                case 9: // 九
                    DrawLine(cx - 20, cy + (int)(32 * AspectCorrectionY), cx - 44, cy - (int)(32 * AspectCorrectionY), t + 2, col);
                    DrawHLine(cx - 36, cx + 30, cy + (int)(14 * AspectCorrectionY), t, col);
                    DrawVLine(cx + 30, cy - (int)(30 * AspectCorrectionY), cy + (int)(14 * AspectCorrectionY), t, col);
                    DrawHLine(cx + 30, cx + 50, cy - (int)(30 * AspectCorrectionY), t, col);
                    break;
            }
        }

        private void DrawKanjiWan(int cx, int cy, Color col)
        {
            int t = 9;
            DrawHLine(cx - 50, cx + 50, cy + (int)(34 * AspectCorrectionY), t, col);
            DrawLine(cx - 22, cy + (int)(34 * AspectCorrectionY), cx - 42, cy + (int)(8 * AspectCorrectionY), t, col);
            DrawHLine(cx - 38, cx + 38, cy + (int)(8 * AspectCorrectionY), t, col);
            DrawRectOutline(cx - 34, cy - (int)(32 * AspectCorrectionY), 68, (int)(40 * AspectCorrectionY), t - 1, col);
            DrawVLine(cx, cy - (int)(32 * AspectCorrectionY), cy + (int)(8 * AspectCorrectionY), t - 1, col);
            DrawHLine(cx - 34, cx + 34, cy - (int)(12 * AspectCorrectionY), t - 1, col);
            DrawLine(cx - 22, cy - (int)(32 * AspectCorrectionY), cx - 42, cy - (int)(48 * AspectCorrectionY), t, col);
            DrawLine(cx + 22, cy - (int)(32 * AspectCorrectionY), cx + 42, cy - (int)(48 * AspectCorrectionY), t, col);
        }

        // =========================================================================
        // 2. BAMBOO / SOU TILES (1 - 9)
        // =========================================================================

        private void DrawBambooTile(int startX, int startY, int cx, int cy, int w, int h, int count)
        {
            DrawCornerBadge(startX + 14, startY + h - 38, $"{count}B", colJadeGreen);

            if (count == 1)
            {
                DrawPeacockBird1Sou(cx, cy);
                return;
            }

            int stickW = 14;
            int stickH = (int)(55 * AspectCorrectionY);

            switch (count)
            {
                case 2:
                    DrawBambooStick(cx, cy + (int)(48 * AspectCorrectionY), stickW, stickH, colJadeGreen);
                    DrawBambooStick(cx, cy - (int)(48 * AspectCorrectionY), stickW, stickH, colJadeGreen);
                    break;
                case 3:
                    DrawBambooStick(cx, cy + (int)(56 * AspectCorrectionY), stickW, stickH - 8, colDeepNavy);
                    DrawBambooStick(cx - 36, cy - (int)(40 * AspectCorrectionY), stickW, stickH, colJadeGreen);
                    DrawBambooStick(cx + 36, cy - (int)(40 * AspectCorrectionY), stickW, stickH, colJadeGreen);
                    break;
                case 4:
                    DrawBambooStick(cx - 36, cy + (int)(48 * AspectCorrectionY), stickW, stickH, colJadeGreen);
                    DrawBambooStick(cx + 36, cy + (int)(48 * AspectCorrectionY), stickW, stickH, colDeepNavy);
                    DrawBambooStick(cx - 36, cy - (int)(48 * AspectCorrectionY), stickW, stickH, colDeepNavy);
                    DrawBambooStick(cx + 36, cy - (int)(48 * AspectCorrectionY), stickW, stickH, colJadeGreen);
                    break;
                case 5:
                    DrawBambooStick(cx - 40, cy + (int)(52 * AspectCorrectionY), stickW, stickH - 6, colJadeGreen);
                    DrawBambooStick(cx + 40, cy + (int)(52 * AspectCorrectionY), stickW, stickH - 6, colDeepNavy);
                    DrawBambooStick(cx, cy, stickW + 2, stickH - 4, colRubyRed);
                    DrawBambooStick(cx - 40, cy - (int)(52 * AspectCorrectionY), stickW, stickH - 6, colDeepNavy);
                    DrawBambooStick(cx + 40, cy - (int)(52 * AspectCorrectionY), stickW, stickH - 6, colJadeGreen);
                    break;
                case 6:
                    for (int c = 0; c < 3; c++)
                    {
                        int x = cx - 42 + (c * 42);
                        DrawBambooStick(x, cy + (int)(48 * AspectCorrectionY), stickW, stickH - 8, colJadeGreen);
                        DrawBambooStick(x, cy - (int)(48 * AspectCorrectionY), stickW, stickH - 8, colJadeGreen);
                    }
                    break;
                case 7:
                    DrawBambooStick(cx, cy + (int)(68 * AspectCorrectionY), stickW, (int)(40 * AspectCorrectionY), colRubyRed);
                    DrawBambooStick(cx - 42, cy + (int)(16 * AspectCorrectionY), stickW, (int)(42 * AspectCorrectionY), colJadeGreen);
                    DrawBambooStick(cx + 42, cy + (int)(16 * AspectCorrectionY), stickW, (int)(42 * AspectCorrectionY), colJadeGreen);
                    for (int c = 0; c < 4; c++)
                    {
                        int x = cx - 48 + (c * 32);
                        DrawBambooStick(x, cy - (int)(54 * AspectCorrectionY), stickW - 2, (int)(50 * AspectCorrectionY), colJadeGreen);
                    }
                    break;
                case 8:
                    DrawBambooStick(cx - 46, cy + (int)(62 * AspectCorrectionY), stickW - 2, (int)(38 * AspectCorrectionY), colJadeGreen);
                    DrawBambooStick(cx - 15, cy + (int)(44 * AspectCorrectionY), stickW - 2, (int)(38 * AspectCorrectionY), colDeepNavy);
                    DrawBambooStick(cx + 15, cy + (int)(44 * AspectCorrectionY), stickW - 2, (int)(38 * AspectCorrectionY), colDeepNavy);
                    DrawBambooStick(cx + 46, cy + (int)(62 * AspectCorrectionY), stickW - 2, (int)(38 * AspectCorrectionY), colJadeGreen);

                    DrawBambooStick(cx - 46, cy - (int)(44 * AspectCorrectionY), stickW - 2, (int)(38 * AspectCorrectionY), colJadeGreen);
                    DrawBambooStick(cx - 15, cy - (int)(62 * AspectCorrectionY), stickW - 2, (int)(38 * AspectCorrectionY), colDeepNavy);
                    DrawBambooStick(cx + 15, cy - (int)(62 * AspectCorrectionY), stickW - 2, (int)(38 * AspectCorrectionY), colDeepNavy);
                    DrawBambooStick(cx + 46, cy - (int)(44 * AspectCorrectionY), stickW - 2, (int)(38 * AspectCorrectionY), colJadeGreen);
                    break;
                case 9:
                    for (int r = 0; r < 3; r++)
                    {
                        int y = cy + (int)((58 - (r * 58)) * AspectCorrectionY);
                        Color rowCol = (r == 0) ? colDeepNavy : (r == 1 ? colRubyRed : colJadeGreen);
                        for (int c = 0; c < 3; c++)
                        {
                            int x = cx - 42 + (c * 42);
                            DrawBambooStick(x, y, stickW - 2, (int)(44 * AspectCorrectionY), rowCol);
                        }
                    }
                    break;
            }
        }

        private void DrawBambooStick(int cx, int cy, int w, int h, Color col)
        {
            DrawFilledRect(cx - w / 2, cy - h / 2, w, h, col);
            DrawProportionalCircle(cx, cy, w / 2 + 2, col);
            DrawProportionalCircle(cx, cy + h / 2 - 4, w / 2 + 1, col);
            DrawProportionalCircle(cx, cy - h / 2 + 4, w / 2 + 1, col);
            DrawFilledRect(cx - 2, cy - h / 2 + 8, 4, h - 16, Color.white);
            DrawProportionalCircle(cx, cy, 3, Color.white);
        }

        private void DrawPeacockBird1Sou(int cx, int cy)
        {
            DrawProportionalCircle(cx, cy - (int)(18 * AspectCorrectionY), 32, colJadeGreen);
            DrawProportionalCircle(cx, cy + (int)(26 * AspectCorrectionY), 20, colJadeGreen);
            DrawProportionalCircle(cx + 12, cy + (int)(36 * AspectCorrectionY), 9, colRubyRed);
            DrawProportionalCircle(cx + 7, cy + (int)(30 * AspectCorrectionY), 4, Color.white);
            DrawProportionalCircle(cx - 26, cy - (int)(8 * AspectCorrectionY), 15, colRubyRed);
            DrawProportionalCircle(cx + 26, cy - (int)(8 * AspectCorrectionY), 15, colDeepNavy);
            DrawProportionalCircle(cx - 32, cy + (int)(16 * AspectCorrectionY), 13, colJadeGreen);
            DrawProportionalCircle(cx + 32, cy + (int)(16 * AspectCorrectionY), 13, colJadeGreen);
            DrawProportionalRing(cx, cy - (int)(18 * AspectCorrectionY), 38, colBevelGold, 4);
        }

        // =========================================================================
        // 3. DOTS / PIN TILES (1 - 9)
        // =========================================================================

        private void DrawDotsTile(int startX, int startY, int cx, int cy, int w, int h, int count)
        {
            DrawCornerBadge(startX + 14, startY + h - 38, $"{count}D", colDeepNavy);

            int r = 24;
            switch (count)
            {
                case 1:
                    DrawProportionalCircle(cx, cy, 60, colRubyRed);
                    DrawProportionalRing(cx, cy, 68, colJadeGreen, 9);
                    DrawProportionalRing(cx, cy, 75, colBevelGold, 4);
                    DrawProportionalCircle(cx, cy, 22, Color.white);
                    DrawProportionalCircle(cx, cy, 12, colRubyRed);
                    break;
                case 2:
                    DrawOrnatePin(cx, cy + (int)(52 * AspectCorrectionY), r + 4, colJadeGreen);
                    DrawOrnatePin(cx, cy - (int)(52 * AspectCorrectionY), r + 4, colDeepNavy);
                    break;
                case 3:
                    DrawOrnatePin(cx - 40, cy + (int)(56 * AspectCorrectionY), r + 2, colDeepNavy);
                    DrawOrnatePin(cx, cy, r + 2, colRubyRed);
                    DrawOrnatePin(cx + 40, cy - (int)(56 * AspectCorrectionY), r + 2, colJadeGreen);
                    break;
                case 4:
                    DrawOrnatePin(cx - 38, cy + (int)(50 * AspectCorrectionY), r + 2, colDeepNavy);
                    DrawOrnatePin(cx + 38, cy + (int)(50 * AspectCorrectionY), r + 2, colJadeGreen);
                    DrawOrnatePin(cx - 38, cy - (int)(50 * AspectCorrectionY), r + 2, colJadeGreen);
                    DrawOrnatePin(cx + 38, cy - (int)(50 * AspectCorrectionY), r + 2, colDeepNavy);
                    break;
                case 5:
                    DrawOrnatePin(cx - 42, cy + (int)(56 * AspectCorrectionY), r, colDeepNavy);
                    DrawOrnatePin(cx + 42, cy + (int)(56 * AspectCorrectionY), r, colJadeGreen);
                    DrawOrnatePin(cx, cy, r + 4, colRubyRed);
                    DrawOrnatePin(cx - 42, cy - (int)(56 * AspectCorrectionY), r, colJadeGreen);
                    DrawOrnatePin(cx + 42, cy - (int)(56 * AspectCorrectionY), r, colDeepNavy);
                    break;
                case 6:
                    for (int row = 0; row < 3; row++)
                    {
                        int y = cy + (int)((54 - (row * 54)) * AspectCorrectionY);
                        DrawOrnatePin(cx - 38, y, r, colJadeGreen);
                        DrawOrnatePin(cx + 38, y, r, colRubyRed);
                    }
                    break;
                case 7:
                    DrawOrnatePin(cx - 38, cy + (int)(66 * AspectCorrectionY), r - 3, colJadeGreen);
                    DrawOrnatePin(cx, cy + (int)(44 * AspectCorrectionY), r - 3, colJadeGreen);
                    DrawOrnatePin(cx + 38, cy + (int)(20 * AspectCorrectionY), r - 3, colJadeGreen);
                    DrawOrnatePin(cx - 36, cy - (int)(26 * AspectCorrectionY), r - 2, colRubyRed);
                    DrawOrnatePin(cx + 36, cy - (int)(26 * AspectCorrectionY), r - 2, colRubyRed);
                    DrawOrnatePin(cx - 36, cy - (int)(68 * AspectCorrectionY), r - 2, colRubyRed);
                    DrawOrnatePin(cx + 36, cy - (int)(68 * AspectCorrectionY), r - 2, colRubyRed);
                    break;
                case 8:
                    for (int row = 0; row < 4; row++)
                    {
                        int y = cy + (int)((64 - (row * 42)) * AspectCorrectionY);
                        DrawOrnatePin(cx - 38, y, r - 4, colDeepNavy);
                        DrawOrnatePin(cx + 38, y, r - 4, colDeepNavy);
                    }
                    break;
                case 9:
                    for (int row = 0; row < 3; row++)
                    {
                        int y = cy + (int)((56 - (row * 56)) * AspectCorrectionY);
                        Color rowCol = (row == 0) ? colDeepNavy : (row == 1 ? colRubyRed : colJadeGreen);
                        for (int col = 0; col < 3; col++)
                        {
                            int x = cx - 40 + (col * 40);
                            DrawOrnatePin(x, y, r - 3, rowCol);
                        }
                    }
                    break;
            }
        }

        private void DrawOrnatePin(int cx, int cy, int r, Color col)
        {
            DrawProportionalCircle(cx, cy, r, col);
            DrawProportionalRing(cx, cy, r + 3, colBevelGold, 2);
            DrawProportionalCircle(cx, cy, r / 3 + 1, Color.white);
            DrawProportionalCircle(cx, cy, r / 5, col);
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
                    DrawCornerBadge(startX + 14, startY + h - 38, "E", colDeepNavy);
                    DrawKanjiEast(cx, cy, colDeepNavy);
                    break;
                case 1: // South (南)
                    DrawCornerBadge(startX + 14, startY + h - 38, "S", colDeepNavy);
                    DrawKanjiSouth(cx, cy, colDeepNavy);
                    break;
                case 2: // West (西)
                    DrawCornerBadge(startX + 14, startY + h - 38, "W", colDeepNavy);
                    DrawKanjiWest(cx, cy, colDeepNavy);
                    break;
                case 3: // North (北)
                    DrawCornerBadge(startX + 14, startY + h - 38, "N", colDeepNavy);
                    DrawKanjiNorth(cx, cy, colDeepNavy);
                    break;
                case 4: // Red Dragon (中 - Chun)
                    DrawCornerBadge(startX + 14, startY + h - 38, "C", colRubyRed);
                    DrawRectOutline(cx - 46, cy - (int)(32 * AspectCorrectionY), 92, (int)(64 * AspectCorrectionY), t + 2, colRubyRed);
                    DrawVLine(cx, cy - (int)(72 * AspectCorrectionY), cy + (int)(72 * AspectCorrectionY), t + 4, colRubyRed);
                    break;
                case 5: // Green Dragon (發 - Fa)
                    DrawCornerBadge(startX + 14, startY + h - 38, "F", colJadeGreen);
                    DrawKanjiFa(cx, cy, colJadeGreen);
                    break;
                case 6: // White Dragon (白 - Bai / Blank Frame)
                    DrawCornerBadge(startX + 14, startY + h - 38, "P", colDeepNavy);
                    DrawRectOutline(cx - 52, cy - (int)(70 * AspectCorrectionY), 104, (int)(140 * AspectCorrectionY), t + 2, colDeepNavy);
                    DrawRectOutline(cx - 42, cy - (int)(60 * AspectCorrectionY), 84, (int)(120 * AspectCorrectionY), 4, colBevelGold);
                    break;
            }
        }

        private void DrawKanjiEast(int cx, int cy, Color col)
        {
            int t = 10;
            DrawHLine(cx - 52, cx + 52, cy + (int)(48 * AspectCorrectionY), t, col);
            DrawRectOutline(cx - 40, cy - (int)(12 * AspectCorrectionY), 80, (int)(48 * AspectCorrectionY), t, col);
            DrawHLine(cx - 40, cx + 40, cy + (int)(12 * AspectCorrectionY), t, col);
            DrawVLine(cx, cy - (int)(68 * AspectCorrectionY), cy + (int)(68 * AspectCorrectionY), t + 2, col);
            DrawLine(cx - 16, cy - (int)(18 * AspectCorrectionY), cx - 46, cy - (int)(60 * AspectCorrectionY), t, col);
            DrawLine(cx + 16, cy - (int)(18 * AspectCorrectionY), cx + 46, cy - (int)(60 * AspectCorrectionY), t, col);
        }

        private void DrawKanjiSouth(int cx, int cy, Color col)
        {
            int t = 9;
            DrawHLine(cx - 42, cx + 42, cy + (int)(56 * AspectCorrectionY), t, col);
            DrawVLine(cx, cy + (int)(40 * AspectCorrectionY), cy + (int)(68 * AspectCorrectionY), t, col);
            DrawRectOutline(cx - 48, cy - (int)(58 * AspectCorrectionY), 96, (int)(94 * AspectCorrectionY), t, col);
            DrawVLine(cx - 16, cy - (int)(36 * AspectCorrectionY), cy + (int)(18 * AspectCorrectionY), t, col);
            DrawVLine(cx + 16, cy - (int)(36 * AspectCorrectionY), cy + (int)(18 * AspectCorrectionY), t, col);
            DrawHLine(cx - 32, cx + 32, cy - (int)(10 * AspectCorrectionY), t, col);
        }

        private void DrawKanjiWest(int cx, int cy, Color col)
        {
            int t = 10;
            DrawHLine(cx - 50, cx + 50, cy + (int)(52 * AspectCorrectionY), t, col);
            DrawRectOutline(cx - 44, cy - (int)(54 * AspectCorrectionY), 88, (int)(95 * AspectCorrectionY), t, col);
            DrawVLine(cx - 16, cy - (int)(36 * AspectCorrectionY), cy + (int)(36 * AspectCorrectionY), t, col);
            DrawVLine(cx + 16, cy - (int)(36 * AspectCorrectionY), cy + (int)(36 * AspectCorrectionY), t, col);
            DrawHLine(cx - 28, cx + 28, cy - (int)(18 * AspectCorrectionY), t, col);
        }

        private void DrawKanjiNorth(int cx, int cy, Color col)
        {
            int t = 10;
            DrawVLine(cx - 22, cy - (int)(54 * AspectCorrectionY), cy + (int)(54 * AspectCorrectionY), t, col);
            DrawHLine(cx - 46, cx - 22, cy + (int)(8 * AspectCorrectionY), t, col);
            DrawLine(cx - 46, cy - (int)(42 * AspectCorrectionY), cx - 22, cy - (int)(8 * AspectCorrectionY), t, col);
            DrawLine(cx + 14, cy + (int)(50 * AspectCorrectionY), cx + 14, cy - (int)(32 * AspectCorrectionY), t, col);
            DrawHLine(cx + 14, cx + 46, cy - (int)(32 * AspectCorrectionY), t, col);
            DrawVLine(cx + 46, cy - (int)(32 * AspectCorrectionY), cy + (int)(14 * AspectCorrectionY), t, col);
        }

        private void DrawKanjiFa(int cx, int cy, Color col)
        {
            int t = 9;
            DrawLine(cx - 40, cy + (int)(58 * AspectCorrectionY), cx, cy + (int)(36 * AspectCorrectionY), t, col);
            DrawLine(cx + 40, cy + (int)(58 * AspectCorrectionY), cx, cy + (int)(36 * AspectCorrectionY), t, col);
            DrawHLine(cx - 42, cx + 42, cy + (int)(26 * AspectCorrectionY), t, col);
            DrawVLine(cx - 22, cy - (int)(58 * AspectCorrectionY), cy + (int)(18 * AspectCorrectionY), t, col);
            DrawVLine(cx + 22, cy - (int)(58 * AspectCorrectionY), cy + (int)(18 * AspectCorrectionY), t, col);
            DrawHLine(cx - 38, cx + 38, cy - (int)(10 * AspectCorrectionY), t, col);
            DrawLine(cx - 14, cy - (int)(10 * AspectCorrectionY), cx - 42, cy - (int)(52 * AspectCorrectionY), t, col);
            DrawLine(cx + 14, cy - (int)(10 * AspectCorrectionY), cx + 42, cy - (int)(52 * AspectCorrectionY), t, col);
        }

        // =========================================================================
        // 5. BONUS: FLOWERS (0..3) & SEASONS (4..7)
        // =========================================================================

        private void DrawBonusTile(int startX, int startY, int cx, int cy, int w, int h, int colIdx)
        {
            if (colIdx < 4)
            {
                DrawCornerBadge(startX + 14, startY + h - 38, $"F{colIdx + 1}", colRubyRed);
                DrawProportionalCircle(cx, cy, 42, colRubyRed);
                DrawProportionalRing(cx, cy, 50, colBevelGold, 4);
                DrawFlowerPetals(cx, cy, colJadeGreen);
            }
            else if (colIdx < 8)
            {
                DrawCornerBadge(startX + 14, startY + h - 38, $"S{colIdx - 3}", colDeepNavy);
                DrawProportionalCircle(cx, cy, 42, colDeepNavy);
                DrawProportionalRing(cx, cy, 50, colBevelGold, 4);
                DrawFlowerPetals(cx, cy, colRubyRed);
            }
        }

        private void DrawFlowerPetals(int cx, int cy, Color petalCol)
        {
            DrawProportionalCircle(cx, cy + (int)(28 * AspectCorrectionY), 14, petalCol);
            DrawProportionalCircle(cx, cy - (int)(28 * AspectCorrectionY), 14, petalCol);
            DrawProportionalCircle(cx + 28, cy, 14, petalCol);
            DrawProportionalCircle(cx - 28, cy, 14, petalCol);
            DrawProportionalCircle(cx, cy, 12, Color.white);
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
                cursorX += 17;
            }
        }

        private void DrawBitmapChar5x7(int startX, int startY, char c, Color col, int scale)
        {
            byte[] rows = GetGlyphRows(c);
            if (rows == null) return;

            int scaleY = Mathf.Max(1, Mathf.RoundToInt(scale * AspectCorrectionY));

            for (int r = 0; r < 7; r++)
            {
                byte rowByte = rows[r];
                for (int colBit = 0; colBit < 5; colBit++)
                {
                    if ((rowByte & (1 << (4 - colBit))) != 0)
                    {
                        DrawFilledRect(startX + (colBit * scale), startY - (r * scaleY), scale, scaleY, col);
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
        // PRIMITIF GRAFIK ASPECT-CORRECTED (LINGKARAN & GARIS)
        // =========================================================================

        private void DrawProportionalCircle(int cx, int cy, int r, Color col)
        {
            int rx = r;
            int ry = Mathf.RoundToInt(r * AspectCorrectionY);
            int rx2 = rx * rx;
            int ry2 = ry * ry;

            for (int dy = -ry; dy <= ry; dy++)
            {
                int dy2 = dy * dy;
                for (int dx = -rx; dx <= rx; dx++)
                {
                    if ((float)(dx * dx) / rx2 + (float)(dy2) / ry2 <= 1.0f)
                    {
                        SetPixelSafe(cx + dx, cy + dy, col);
                    }
                }
            }
        }

        private void DrawProportionalRing(int cx, int cy, int r, Color col, int thickness)
        {
            int rxOuter = r;
            int ryOuter = Mathf.RoundToInt(r * AspectCorrectionY);
            int rxInner = Mathf.Max(1, r - thickness);
            int ryInner = Mathf.Max(1, Mathf.RoundToInt((r - thickness) * AspectCorrectionY));

            float rxO2 = rxOuter * rxOuter;
            float ryO2 = ryOuter * ryOuter;
            float rxI2 = rxInner * rxInner;
            float ryI2 = ryInner * ryInner;

            for (int dy = -ryOuter; dy <= ryOuter; dy++)
            {
                int dy2 = dy * dy;
                for (int dx = -rxOuter; dx <= rxOuter; dx++)
                {
                    float dOuter = (float)(dx * dx) / rxO2 + (float)(dy2) / ryO2;
                    float dInner = (float)(dx * dx) / rxI2 + (float)(dy2) / ryI2;
                    if (dOuter <= 1.0f && dInner >= 1.0f)
                    {
                        SetPixelSafe(cx + dx, cy + dy, col);
                    }
                }
            }
        }

        private void DrawHLine(int x0, int x1, int y, int thickness, Color col)
        {
            int minX = Mathf.Min(x0, x1);
            int maxX = Mathf.Max(x0, x1);
            int ht = Mathf.Max(1, Mathf.RoundToInt((thickness / 2f) * AspectCorrectionY));
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

            int htX = thickness / 2;
            int htY = Mathf.Max(1, Mathf.RoundToInt((thickness / 2f) * AspectCorrectionY));

            while (true)
            {
                for (int ox = -htX; ox <= htX; ox++)
                {
                    for (int oy = -htY; oy <= htY; oy++)
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
            int thickY = Mathf.Max(1, Mathf.RoundToInt(thickness * AspectCorrectionY));
            DrawFilledRect(x, y, w, thickY, col);                         // Bawah
            DrawFilledRect(x, y + h - thickY, w, thickY, col);             // Atas
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

        private void SetPixelSafe(int x, int y, Color col)
        {
            if (x >= 0 && x < atlasWidth && y >= 0 && y < atlasHeight)
            {
                GeneratedAtlas.SetPixel(x, y, col);
            }
        }
    }
}
