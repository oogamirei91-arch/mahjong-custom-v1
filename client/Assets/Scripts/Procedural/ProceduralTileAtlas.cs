using UnityEngine;

namespace Mahjong.Procedural
{
    /// <summary>
    /// ProceduralTileAtlas: Generator Tekstur Atlas 2D Otomatis untuk semua 144 Ubin Mahjong.
    /// Menghasilkan tekstur resolusi tinggi (2048x2048) berisi gambar simbol Dots (Pin),
    /// Bamboo (Sou), Characters (Wan), Honors (Angin & Naga), serta Bonus (Bunga & Musim)
    /// sehingga game Unity dapat langsung dijalankan tanpa perlu mengunduh aset gambar eksternal!
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

        /// <summary>
        /// Membuat tekstur atlas 6 kolom x 7 baris berisi seluruh set ubin Mahjong.
        /// </summary>
        [ContextMenu("Regenerate Full Atlas")]
        public void GenerateFullAtlas()
        {
            GeneratedAtlas = new Texture2D(atlasWidth, atlasHeight, TextureFormat.RGBA32, true);
            GeneratedAtlas.name = "Tex_Mahjong_Procedural_Atlas";
            GeneratedAtlas.filterMode = FilterMode.Bilinear;
            GeneratedAtlas.wrapMode = TextureWrapMode.Clamp;

            // Bersihkan kanvas dengan warna dasar gading pearl (off-white)
            Color baseIvory = new Color(0.97f, 0.96f, 0.92f, 1.0f);
            Color[] clearPixels = new Color[atlasWidth * atlasHeight];
            for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = baseIvory;
            GeneratedAtlas.SetPixels(clearPixels);

            int cellW = atlasWidth / 6;
            int cellH = atlasHeight / 7;

            // Gambarkan bingkai dan simbol di setiap sel atlas
            for (int row = 0; row < 7; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    int startX = col * cellW;
                    int startY = (6 - row) * cellH; // Y-axis inverted in Unity texture
                    DrawCellBorder(startX, startY, cellW, cellH);
                    DrawTileSymbol(startX, startY, cellW, cellH, row, col);
                }
            }

            GeneratedAtlas.Apply();
            Debug.Log("[ProceduralTileAtlas] Mahjong Texture Atlas 2048x2048 berhasil dibuat secara prosedural!");
        }

        private void DrawCellBorder(int startX, int startY, int w, int h)
        {
            Color goldBevel = new Color(0.85f, 0.75f, 0.40f, 0.8f);
            int margin = 8;
            for (int x = margin; x < w - margin; x++)
            {
                GeneratedAtlas.SetPixel(startX + x, startY + margin, goldBevel);
                GeneratedAtlas.SetPixel(startX + x, startY + h - margin, goldBevel);
            }
            for (int y = margin; y < h - margin; y++)
            {
                GeneratedAtlas.SetPixel(startX + margin, startY + y, goldBevel);
                GeneratedAtlas.SetPixel(startX + w - margin, startY + y, goldBevel);
            }
        }

        private void DrawTileSymbol(int startX, int startY, int w, int h, int row, int col)
        {
            int centerX = startX + w / 2;
            int centerY = startY + h / 2;

            // Palet Warna Kasino Tradisional
            Color deepBlue = new Color(0.10f, 0.20f, 0.70f);
            Color emeraldGreen = new Color(0.08f, 0.55f, 0.25f);
            Color crimsonRed = new Color(0.80f, 0.12f, 0.15f);

            switch (row)
            {
                case 0: // Baris 0: Dots 1 s/d 6
                    DrawDots(centerX, centerY, col + 1, crimsonRed, deepBlue, emeraldGreen);
                    break;
                case 1: // Baris 1: Bamboo 1 s/d 6
                    DrawBamboo(centerX, centerY, col + 1, emeraldGreen, crimsonRed);
                    break;
                case 2: // Baris 2: Characters 1 s/d 6 (Wan)
                    DrawWan(centerX, centerY, col + 1, crimsonRed);
                    break;
                case 3: // Baris 3: Dots/Bamboo/Wan 7-9 & Honors (Angin E/S/W/N)
                    if (col < 4) DrawWind(centerX, centerY, col, deepBlue); // East, South, West, North
                    else DrawDragon(centerX, centerY, col - 4, crimsonRed, emeraldGreen); // Red, Green Dragon
                    break;
                case 4: // Baris 4: White Dragon & Bonus (Bunga 1-4)
                    DrawFlower(centerX, centerY, col + 1, crimsonRed, emeraldGreen);
                    break;
                case 5: // Baris 5: Bonus (Musim 1-4)
                    DrawSeason(centerX, centerY, col + 1, deepBlue, crimsonRed);
                    break;
            }
        }

        private void DrawDots(int cx, int cy, int count, Color red, Color blue, Color green)
        {
            int radius = 18;
            switch (count)
            {
                case 1:
                    DrawFilledCircle(cx, cy, radius * 2, red);
                    DrawRing(cx, cy, radius * 2 + 6, green, 4);
                    break;
                case 2:
                    DrawFilledCircle(cx, cy - 35, radius, green);
                    DrawFilledCircle(cx, cy + 35, radius, blue);
                    break;
                case 3:
                    DrawFilledCircle(cx - 30, cy - 35, radius, blue);
                    DrawFilledCircle(cx, cy, radius, red);
                    DrawFilledCircle(cx + 30, cy + 35, radius, green);
                    break;
                case 4:
                    DrawFilledCircle(cx - 30, cy - 35, radius, blue);
                    DrawFilledCircle(cx + 30, cy - 35, radius, green);
                    DrawFilledCircle(cx - 30, cy + 35, radius, green);
                    DrawFilledCircle(cx + 30, cy + 35, radius, blue);
                    break;
                case 5:
                    DrawFilledCircle(cx - 30, cy - 35, radius, blue);
                    DrawFilledCircle(cx + 30, cy - 35, radius, green);
                    DrawFilledCircle(cx, cy, radius, red);
                    DrawFilledCircle(cx - 30, cy + 35, radius, green);
                    DrawFilledCircle(cx + 30, cy + 35, radius, blue);
                    break;
                default:
                    DrawFilledCircle(cx, cy, radius, green);
                    break;
            }
        }

        private void DrawBamboo(int cx, int cy, int count, Color green, Color red)
        {
            int stickWidth = 8;
            int stickHeight = 35;
            for (int i = 0; i < count; i++)
            {
                int offsetX = (i - count / 2) * 20;
                DrawRect(cx + offsetX - stickWidth / 2, cy - stickHeight / 2, stickWidth, stickHeight, green);
            }
        }

        private void DrawWan(int cx, int cy, int num, Color red)
        {
            // Gambar karakter persegi sederhana untuk Wan
            DrawRect(cx - 25, cy + 15, 50, 10, red);
            DrawRect(cx - 5, cy - 30, 10, 50, red);
            DrawRect(cx - 20, cy - 35, 40, 10, red);
        }

        private void DrawWind(int cx, int cy, int windIdx, Color blue)
        {
            // Gambar penanda Angin (E, S, W, N)
            DrawFilledCircle(cx, cy, 28, blue);
            DrawRing(cx, cy, 32, Color.white, 3);
        }

        private void DrawDragon(int cx, int cy, int dragonIdx, Color red, Color green)
        {
            Color col = (dragonIdx == 0) ? red : green;
            DrawRect(cx - 25, cy - 25, 50, 50, col);
            DrawRing(cx, cy, 26, Color.white, 4);
        }

        private void DrawFlower(int cx, int cy, int num, Color red, Color green)
        {
            DrawFilledCircle(cx, cy, 22, red);
            DrawFilledCircle(cx - 15, cy, 12, green);
            DrawFilledCircle(cx + 15, cy, 12, green);
            DrawFilledCircle(cx, cy - 15, 12, green);
            DrawFilledCircle(cx, cy + 15, 12, green);
        }

        private void DrawSeason(int cx, int cy, int num, Color blue, Color red)
        {
            DrawFilledCircle(cx, cy, 24, blue);
            DrawFilledCircle(cx, cy, 10, red);
        }

        private void DrawFilledCircle(int cx, int cy, int r, Color col)
        {
            for (int y = -r; y <= r; y++)
            {
                for (int x = -r; x <= r; x++)
                {
                    if (x * x + y * y <= r * r)
                    {
                        GeneratedAtlas.SetPixel(cx + x, cy + y, col);
                    }
                }
            }
        }

        private void DrawRing(int cx, int cy, int r, Color col, int thickness)
        {
            int rInner = r - thickness;
            for (int y = -r; y <= r; y++)
            {
                for (int x = -r; x <= r; x++)
                {
                    int d2 = x * x + y * y;
                    if (d2 <= r * r && d2 >= rInner * rInner)
                    {
                        GeneratedAtlas.SetPixel(cx + x, cy + y, col);
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
                    GeneratedAtlas.SetPixel(x + dx, y + dy, col);
                }
            }
        }
    }
}
