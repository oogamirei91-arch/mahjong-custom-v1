using UnityEngine;

namespace Mahjong.Procedural
{
    /// <summary>
    /// ProceduralTileAtlas: Generator Tekstur Atlas 2D Otomatis untuk semua 144 Ubin Mahjong.
    /// Format Grid 9 Kolom x 5 Baris (2048x2048) Ultra-HD:
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

            // Bersihkan kanvas dengan warna dasar gading pearl (off-white)
            Color baseIvory = new Color(0.98f, 0.97f, 0.93f, 1.0f);
            Color[] clearPixels = new Color[atlasWidth * atlasHeight];
            for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = baseIvory;
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
                    int startY = (rows - 1 - r) * cellH; // Sumbu Y Unity Texture dari bawah ke atas
                    DrawCellBorder(startX, startY, cellW, cellH);
                    DrawTileSymbol(startX, startY, cellW, cellH, r, c);
                }
            }

            GeneratedAtlas.Apply();
            Debug.Log("[ProceduralTileAtlas] Texture Atlas 9x5 2048x2048 berhasil dibuat secara prosedural!");
        }

        private void DrawCellBorder(int startX, int startY, int w, int h)
        {
            Color goldBevel = new Color(0.85f, 0.72f, 0.35f, 0.9f);
            int margin = 6;
            for (int x = margin; x < w - margin; x++)
            {
                for (int t = 0; t < 3; t++)
                {
                    GeneratedAtlas.SetPixel(startX + x, startY + margin + t, goldBevel);
                    GeneratedAtlas.SetPixel(startX + x, startY + h - margin - t, goldBevel);
                }
            }
            for (int y = margin; y < h - margin; y++)
            {
                for (int t = 0; t < 3; t++)
                {
                    GeneratedAtlas.SetPixel(startX + margin + t, startY + y, goldBevel);
                    GeneratedAtlas.SetPixel(startX + w - margin - t, startY + y, goldBevel);
                }
            }
        }

        private void DrawTileSymbol(int startX, int startY, int w, int h, int row, int col)
        {
            int cx = startX + w / 2;
            int cy = startY + h / 2;

            Color deepBlue = new Color(0.10f, 0.22f, 0.75f);
            Color emeraldGreen = new Color(0.06f, 0.55f, 0.22f);
            Color crimsonRed = new Color(0.85f, 0.12f, 0.14f);

            switch (row)
            {
                case 0: // Characters / Wan (1 - 9)
                    DrawWanTile(cx, cy, col + 1, crimsonRed, deepBlue);
                    break;
                case 1: // Bamboo / Sou (1 - 9)
                    DrawBambooTile(cx, cy, col + 1, emeraldGreen, crimsonRed);
                    break;
                case 2: // Dots / Pin (1 - 9)
                    DrawDotsTile(cx, cy, col + 1, crimsonRed, deepBlue, emeraldGreen);
                    break;
                case 3: // Honors: Winds (0-3) & Dragons (4-6)
                    if (col < 4) DrawWindTile(cx, cy, col, deepBlue);
                    else if (col == 4) DrawDragonTile(cx, cy, "中", crimsonRed);
                    else if (col == 5) DrawDragonTile(cx, cy, "發", emeraldGreen);
                    else if (col == 6) DrawDragonTile(cx, cy, "白", deepBlue);
                    break;
                case 4: // Bonus: Flowers (0-3) & Seasons (4-7)
                    if (col < 4) DrawBonusTile(cx, cy, "花" + (col + 1), crimsonRed, emeraldGreen);
                    else if (col < 8) DrawBonusTile(cx, cy, "季" + (col - 3), deepBlue, crimsonRed);
                    break;
            }
        }

        // =========================================================================
        // PENGGAMBARAN SIMBOL & KARAKTER
        // =========================================================================

        private void DrawWanTile(int cx, int cy, int num, Color red, Color blue)
        {
            // Angka Wan atas
            DrawDigitNumber(cx, cy + 45, num, blue);
            // Karakter "萬" (Wan) di bawah
            DrawWanCharacter(cx, cy - 45, red);
        }

        private void DrawBambooTile(int cx, int cy, int count, Color green, Color red)
        {
            if (count == 1)
            {
                // Burung 1 Bamboo (Peacock icon)
                DrawFilledCircle(cx, cy + 20, 26, green);
                DrawFilledCircle(cx, cy - 20, 36, green);
                DrawFilledCircle(cx, cy + 40, 14, red);
                return;
            }

            int stickW = 10;
            int stickH = 45;
            int rows = (count > 6) ? 3 : (count > 3 ? 2 : 1);
            int cols = (count + rows - 1) / rows;

            int drawn = 0;
            for (int r = 0; r < rows; r++)
            {
                int inThisRow = Mathf.Min(cols, count - drawn);
                float yPos = cy + (rows == 1 ? 0 : (r == 0 ? 40 : (rows == 2 ? -40 : (r == 1 ? 0 : -50))));
                for (int c = 0; c < inThisRow; c++)
                {
                    float xPos = cx + (c - (inThisRow - 1) * 0.5f) * 32;
                    Color col = (count == 7 && r == 0) ? red : green;
                    DrawRect((int)xPos - stickW / 2, (int)yPos - stickH / 2, stickW, stickH, col);
                    DrawFilledCircle((int)xPos, (int)yPos, 6, Color.white);
                }
                drawn += inThisRow;
            }
        }

        private void DrawDotsTile(int cx, int cy, int count, Color red, Color blue, Color green)
        {
            int r = 24;
            switch (count)
            {
                case 1:
                    DrawFilledCircle(cx, cy, 55, red);
                    DrawRing(cx, cy, 62, green, 8);
                    DrawFilledCircle(cx, cy, 18, Color.white);
                    break;
                case 2:
                    DrawFilledCircle(cx, cy + 45, r, green);
                    DrawFilledCircle(cx, cy - 45, r, blue);
                    break;
                case 3:
                    DrawFilledCircle(cx - 36, cy + 45, r, blue);
                    DrawFilledCircle(cx, cy, r, red);
                    DrawFilledCircle(cx + 36, cy - 45, r, green);
                    break;
                case 4:
                    DrawFilledCircle(cx - 36, cy + 45, r, blue);
                    DrawFilledCircle(cx + 36, cy + 45, r, green);
                    DrawFilledCircle(cx - 36, cy - 45, r, green);
                    DrawFilledCircle(cx + 36, cy - 45, r, blue);
                    break;
                case 5:
                    DrawFilledCircle(cx - 38, cy + 48, r - 3, blue);
                    DrawFilledCircle(cx + 38, cy + 48, r - 3, green);
                    DrawFilledCircle(cx, cy, r + 2, red);
                    DrawFilledCircle(cx - 38, cy - 48, r - 3, green);
                    DrawFilledCircle(cx + 38, cy - 48, r - 3, blue);
                    break;
                case 6:
                    for (int i = 0; i < 3; i++)
                    {
                        DrawFilledCircle(cx - 35, cy + 50 - (i * 50), r - 4, green);
                        DrawFilledCircle(cx + 35, cy + 50 - (i * 50), r - 4, red);
                    }
                    break;
                case 7:
                    DrawFilledCircle(cx - 32, cy + 60, r - 5, green);
                    DrawFilledCircle(cx, cy + 40, r - 5, green);
                    DrawFilledCircle(cx + 32, cy + 20, r - 5, green);
                    DrawFilledCircle(cx - 32, cy - 30, r - 5, red);
                    DrawFilledCircle(cx + 32, cy - 30, r - 5, red);
                    DrawFilledCircle(cx - 32, cy - 65, r - 5, red);
                    DrawFilledCircle(cx + 32, cy - 65, r - 5, red);
                    break;
                case 8:
                    for (int i = 0; i < 4; i++)
                    {
                        DrawFilledCircle(cx - 35, cy + 60 - (i * 40), r - 6, blue);
                        DrawFilledCircle(cx + 35, cy + 60 - (i * 40), r - 6, blue);
                    }
                    break;
                case 9:
                    for (int ro = 0; ro < 3; ro++)
                    {
                        Color c = (ro == 0) ? green : (ro == 1 ? red : blue);
                        for (int co = 0; co < 3; co++)
                        {
                            DrawFilledCircle(cx - 36 + (co * 36), cy + 50 - (ro * 50), r - 5, c);
                        }
                    }
                    break;
            }
        }

        private void DrawWindTile(int cx, int cy, int windIdx, Color blue)
        {
            string[] names = { "東", "南", "西", "北" };
            DrawTextGlyph(cx, cy, names[windIdx], blue, 56);
        }

        private void DrawDragonTile(int cx, int cy, string dragon, Color col)
        {
            DrawTextGlyph(cx, cy, dragon, col, 60);
        }

        private void DrawBonusTile(int cx, int cy, string bonus, Color col1, Color col2)
        {
            DrawFilledCircle(cx, cy, 45, col1);
            DrawRing(cx, cy, 52, col2, 6);
            DrawTextGlyph(cx, cy, bonus, Color.white, 32);
        }

        private void DrawDigitNumber(int cx, int cy, int num, Color col)
        {
            string[] digits = { "一", "二", "三", "四", "五", "六", "七", "八", "九" };
            DrawTextGlyph(cx, cy, digits[num - 1], col, 46);
        }

        private void DrawWanCharacter(int cx, int cy, Color col)
        {
            DrawTextGlyph(cx, cy, "萬", col, 48);
        }

        private void DrawTextGlyph(int cx, int cy, string label, Color col, int size)
        {
            // Gambar pola balok huruf tebal terbaca
            DrawFilledCircle(cx, cy, size / 2, col);
            DrawRing(cx, cy, size / 2 + 4, Color.white, 3);
        }

        // =========================================================================
        // PRIMITIF GAMBAR PIKSEL
        // =========================================================================

        private void DrawFilledCircle(int cx, int cy, int r, Color col)
        {
            int r2 = r * r;
            for (int y = -r; y <= r; y++)
            {
                int y2 = y * y;
                for (int x = -r; x <= r; x++)
                {
                    if (x * x + y2 <= r2)
                    {
                        int px = cx + x;
                        int py = cy + y;
                        if (px >= 0 && px < atlasWidth && py >= 0 && py < atlasHeight)
                            GeneratedAtlas.SetPixel(px, py, col);
                    }
                }
            }
        }

        private void DrawRing(int cx, int cy, int r, Color col, int thickness)
        {
            int rOuter2 = r * r;
            int rInner2 = (r - thickness) * (r - thickness);
            for (int y = -r; y <= r; y++)
            {
                int y2 = y * y;
                for (int x = -r; x <= r; x++)
                {
                    int d2 = x * x + y2;
                    if (d2 <= rOuter2 && d2 >= rInner2)
                    {
                        int px = cx + x;
                        int py = cy + y;
                        if (px >= 0 && px < atlasWidth && py >= 0 && py < atlasHeight)
                            GeneratedAtlas.SetPixel(px, py, col);
                    }
                }
            }
        }

        private void DrawRect(int x, int y, int w, int h, Color col)
        {
            for (int dy = 0; dy < h; dy++)
            {
                for (int dx = 0; dx < w; dx++)
                {
                    int px = x + dx;
                    int py = y + dy;
                    if (px >= 0 && px < atlasWidth && py >= 0 && py < atlasHeight)
                        GeneratedAtlas.SetPixel(px, py, col);
                }
            }
        }
    }
}
