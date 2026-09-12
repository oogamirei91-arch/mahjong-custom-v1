/**
 * tileAtlas.js - Generator Tekstur Atlas Ubin Mahjong Prosedural Ultra-HD
 * Menghasilkan Texture Canvas 2048x2048 untuk seluruh 144 ubin dengan kaligrafi Kanji, Pin, Bambu, dan Giok Zamrud.
 */

export class TileAtlasGenerator {
    constructor(width = 2048, height = 2048) {
        this.width = width;
        this.height = height;
        this.cols = 9;
        this.rows = 5;
        this.cellW = width / this.cols;
        this.cellH = height / this.rows;

        // Color Palette
        this.colIvory = "#fdfbf7";
        this.colBevelGold = "#cda44b";
        this.colNavy = "#0d2b80";
        this.colRuby = "#c81418";
        this.colJade = "#0b7c3b";
        this.colCharcoal = "#1a1a1c";
    }

    generateAtlasCanvas() {
        const canvas = document.createElement("canvas");
        canvas.width = this.width;
        canvas.height = this.height;
        const ctx = canvas.getContext("2d");

        // 1. Bersihkan bidang dengan warna dasar Pearl Ivory
        ctx.fillStyle = this.colIvory;
        ctx.fillRect(0, 0, this.width, this.height);

        // 2. Gambar setiap sel ubin (9 Kolom x 5 Baris)
        for (let r = 0; r < this.rows; r++) {
            for (let c = 0; c < this.cols; c++) {
                const x = c * this.cellW;
                const y = r * this.cellH;
                this.drawTileCell(ctx, x, y, this.cellW, this.cellH, r, c);
            }
        }

        return canvas;
    }

    // Generator Tekstur Punggung Giok Zamrud (Jade Green Back)
    generateJadeBackCanvas() {
        const canvas = document.createElement("canvas");
        canvas.width = 512;
        canvas.height = 512;
        const ctx = canvas.getContext("2d");

        // Emerald gradient base
        const grad = ctx.createRadialGradient(256, 256, 40, 256, 256, 320);
        grad.addColorStop(0, "#0e9146");
        grad.addColorStop(0.6, "#086630");
        grad.addColorStop(1, "#033b1a");
        ctx.fillStyle = grad;
        ctx.fillRect(0, 0, 512, 512);

        // Subtle gold luxury inner border
        ctx.strokeStyle = "rgba(218, 178, 80, 0.4)";
        ctx.lineWidth = 12;
        ctx.strokeRect(18, 18, 476, 476);

        // Jade marble vein effect
        ctx.strokeStyle = "rgba(140, 245, 180, 0.12)";
        ctx.lineWidth = 4;
        for (let i = 0; i < 8; i++) {
            ctx.beginPath();
            ctx.moveTo(Math.random() * 512, Math.random() * 512);
            ctx.bezierCurveTo(Math.random() * 512, Math.random() * 512, Math.random() * 512, Math.random() * 512, Math.random() * 512, Math.random() * 512);
            ctx.stroke();
        }

        return canvas;
    }

    drawTileCell(ctx, x, y, w, h, row, col) {
        // Outer Bevel Frame & Shadow
        ctx.save();
        ctx.strokeStyle = "rgba(200, 180, 150, 0.4)";
        ctx.lineWidth = 6;
        ctx.strokeRect(x + 8, y + 8, w - 16, h - 16);

        ctx.strokeStyle = "rgba(255, 255, 255, 0.8)";
        ctx.lineWidth = 3;
        ctx.strokeRect(x + 12, y + 12, w - 24, h - 24);

        const cx = x + w / 2;
        const cy = y + h / 2;

        switch (row) {
            case 0: // Characters / Wan (1 - 9)
                this.drawWanTile(ctx, x, y, cx, cy, w, h, col + 1);
                break;
            case 1: // Bamboo / Sou (1 - 9)
                this.drawBambooTile(ctx, x, y, cx, cy, w, h, col + 1);
                break;
            case 2: // Dots / Pin (1 - 9)
                this.drawDotsTile(ctx, x, y, cx, cy, w, h, col + 1);
                break;
            case 3: // Honors: Winds (0..3) & Dragons (4..6)
                this.drawHonorTile(ctx, x, y, cx, cy, w, h, col);
                break;
            case 4: // Bonus: Flowers (0..3) & Seasons (4..7)
                this.drawBonusTile(ctx, x, y, cx, cy, w, h, col);
                break;
        }

        ctx.restore();
    }

    // 1. WAN TILES (1 - 9)
    drawWanTile(ctx, x, y, cx, cy, w, h, num) {
        this.drawCornerBadge(ctx, x + 20, y + 42, `${num}W`, this.colRuby);

        const kanjiDigits = ["一", "二", "三", "四", "五", "六", "七", "八", "九"];
        // Top Kanji Digit
        ctx.fillStyle = this.colNavy;
        ctx.font = "bold 96px 'Microsoft YaHei', 'PingFang SC', sans-serif";
        ctx.textAlign = "center";
        ctx.textBaseline = "middle";
        ctx.fillText(kanjiDigits[num - 1], cx, cy - 65);

        // Bottom Wan Symbol (萬)
        ctx.fillStyle = this.colRuby;
        ctx.font = "bold 90px 'Microsoft YaHei', 'PingFang SC', sans-serif";
        ctx.fillText("萬", cx, cy + 70);
    }

    // 2. BAMBOO / SOU TILES (1 - 9)
    drawBambooTile(ctx, x, y, cx, cy, w, h, count) {
        this.drawCornerBadge(ctx, x + 20, y + 42, `${count}B`, this.colJade);

        if (count === 1) {
            // Peacock Bird for 1 Sou
            ctx.fillStyle = this.colJade;
            ctx.beginPath();
            ctx.arc(cx, cy + 20, 42, 0, Math.PI * 2);
            ctx.fill();
            ctx.beginPath();
            ctx.arc(cx, cy - 35, 26, 0, Math.PI * 2);
            ctx.fill();

            // Head & Eye
            ctx.fillStyle = this.colRuby;
            ctx.beginPath();
            ctx.arc(cx + 16, cy - 48, 12, 0, Math.PI * 2);
            ctx.fill();
            ctx.fillStyle = "#ffffff";
            ctx.beginPath();
            ctx.arc(cx + 10, cy - 40, 5, 0, Math.PI * 2);
            ctx.fill();

            // Tail feathers
            ctx.fillStyle = this.colRuby;
            ctx.beginPath();
            ctx.arc(cx - 36, cy + 10, 18, 0, Math.PI * 2);
            ctx.fill();
            ctx.fillStyle = this.colNavy;
            ctx.beginPath();
            ctx.arc(cx + 36, cy + 10, 18, 0, Math.PI * 2);
            ctx.fill();
            return;
        }

        const stickW = 16;
        const stickH = 65;

        const drawStick = (sx, sy, color) => {
            ctx.fillStyle = color;
            ctx.fillRect(sx - stickW / 2, sy - stickH / 2, stickW, stickH);
            // Bamboo nodes
            ctx.beginPath();
            ctx.arc(sx, sy, stickW / 2 + 2, 0, Math.PI * 2);
            ctx.arc(sx, sy - stickH / 2 + 6, stickW / 2 + 2, 0, Math.PI * 2);
            ctx.arc(sx, sy + stickH / 2 - 6, stickW / 2 + 2, 0, Math.PI * 2);
            ctx.fill();
            // Center notch
            ctx.fillStyle = "#ffffff";
            ctx.fillRect(sx - 2, sy - stickH / 2 + 8, 4, stickH - 16);
        };

        switch (count) {
            case 2:
                drawStick(cx, cy - 60, this.colJade);
                drawStick(cx, cy + 60, this.colJade);
                break;
            case 3:
                drawStick(cx, cy - 70, this.colNavy);
                drawStick(cx - 45, cy + 50, this.colJade);
                drawStick(cx + 45, cy + 50, this.colJade);
                break;
            case 4:
                drawStick(cx - 45, cy - 60, this.colJade);
                drawStick(cx + 45, cy - 60, this.colNavy);
                drawStick(cx - 45, cy + 60, this.colNavy);
                drawStick(cx + 45, cy + 60, this.colJade);
                break;
            case 5:
                drawStick(cx - 50, cy - 65, this.colJade);
                drawStick(cx + 50, cy - 65, this.colNavy);
                drawStick(cx, cy, this.colRuby);
                drawStick(cx - 50, cy + 65, this.colNavy);
                drawStick(cx + 50, cy + 65, this.colJade);
                break;
            case 6:
                for (let i = 0; i < 3; i++) {
                    const sx = cx - 50 + (i * 50);
                    drawStick(sx, cy - 60, this.colJade);
                    drawStick(sx, cy + 60, this.colJade);
                }
                break;
            case 7:
                drawStick(cx, cy - 85, this.colRuby);
                drawStick(cx - 50, cy - 20, this.colJade);
                drawStick(cx + 50, cy - 20, this.colJade);
                for (let i = 0; i < 4; i++) {
                    const sx = cx - 60 + (i * 40);
                    drawStick(sx, cy + 65, this.colJade);
                }
                break;
            case 8:
                for (let i = 0; i < 4; i++) {
                    const sx = cx - 60 + (i * 40);
                    const sy1 = (i === 1 || i === 2) ? cy - 45 : cy - 70;
                    const sy2 = (i === 1 || i === 2) ? cy + 70 : cy + 45;
                    const col1 = (i === 1 || i === 2) ? this.colNavy : this.colJade;
                    drawStick(sx, sy1, col1);
                    drawStick(sx, sy2, col1);
                }
                break;
            case 9:
                for (let r = 0; r < 3; r++) {
                    const sy = cy - 70 + (r * 70);
                    const rCol = r === 0 ? this.colNavy : (r === 1 ? this.colRuby : this.colJade);
                    for (let c = 0; c < 3; c++) {
                        const sx = cx - 50 + (c * 50);
                        drawStick(sx, sy, rCol);
                    }
                }
                break;
        }
    }

    // 3. DOTS / PIN TILES (1 - 9)
    drawDotsTile(ctx, x, y, cx, cy, w, h, count) {
        this.drawCornerBadge(ctx, x + 20, y + 42, `${count}D`, this.colNavy);

        const drawPin = (px, py, r, color) => {
            ctx.fillStyle = color;
            ctx.beginPath();
            ctx.arc(px, py, r, 0, Math.PI * 2);
            ctx.fill();

            // Gold Ring
            ctx.strokeStyle = this.colBevelGold;
            ctx.lineWidth = 3;
            ctx.beginPath();
            ctx.arc(px, py, r + 2, 0, Math.PI * 2);
            ctx.stroke();

            // Inner white dot
            ctx.fillStyle = "#ffffff";
            ctx.beginPath();
            ctx.arc(px, py, r * 0.35, 0, Math.PI * 2);
            ctx.fill();

            // Core center dot
            ctx.fillStyle = color;
            ctx.beginPath();
            ctx.arc(px, py, r * 0.18, 0, Math.PI * 2);
            ctx.fill();
        };

        const r = 26;
        switch (count) {
            case 1:
                drawPin(cx, cy, 65, this.colRuby);
                break;
            case 2:
                drawPin(cx, cy - 65, r + 4, this.colJade);
                drawPin(cx, cy + 65, r + 4, this.colNavy);
                break;
            case 3:
                drawPin(cx - 50, cy - 70, r + 2, this.colNavy);
                drawPin(cx, cy, r + 2, this.colRuby);
                drawPin(cx + 50, cy + 70, r + 2, this.colJade);
                break;
            case 4:
                drawPin(cx - 48, cy - 65, r + 2, this.colNavy);
                drawPin(cx + 48, cy - 65, r + 2, this.colJade);
                drawPin(cx - 48, cy + 65, r + 2, this.colJade);
                drawPin(cx + 48, cy + 65, r + 2, this.colNavy);
                break;
            case 5:
                drawPin(cx - 52, cy - 70, r, this.colNavy);
                drawPin(cx + 52, cy - 70, r, this.colJade);
                drawPin(cx, cy, r + 4, this.colRuby);
                drawPin(cx - 52, cy + 70, r, this.colJade);
                drawPin(cx + 52, cy + 70, r, this.colNavy);
                break;
            case 6:
                for (let i = 0; i < 3; i++) {
                    const py = cy - 65 + (i * 65);
                    drawPin(cx - 48, py, r, this.colJade);
                    drawPin(cx + 48, py, r, this.colRuby);
                }
                break;
            case 7:
                drawPin(cx - 50, cy - 80, r - 3, this.colJade);
                drawPin(cx, cy - 50, r - 3, this.colJade);
                drawPin(cx + 50, cy - 20, r - 3, this.colJade);
                drawPin(cx - 48, cy + 35, r - 2, this.colRuby);
                drawPin(cx + 48, cy + 35, r - 2, this.colRuby);
                drawPin(cx - 48, cy + 85, r - 2, this.colRuby);
                drawPin(cx + 48, cy + 85, r - 2, this.colRuby);
                break;
            case 8:
                for (let i = 0; i < 4; i++) {
                    const py = cy - 80 + (i * 54);
                    drawPin(cx - 48, py, r - 4, this.colNavy);
                    drawPin(cx + 48, py, r - 4, this.colNavy);
                }
                break;
            case 9:
                for (let row = 0; row < 3; row++) {
                    const py = cy - 70 + (row * 70);
                    const colCol = row === 0 ? this.colNavy : (row === 1 ? this.colRuby : this.colJade);
                    for (let col = 0; col < 3; col++) {
                        const px = cx - 50 + (col * 50);
                        drawPin(px, py, r - 3, colCol);
                    }
                }
                break;
        }
    }

    // 4. HONORS (WINDS & DRAGONS)
    drawHonorTile(ctx, x, y, cx, cy, w, h, colIdx) {
        ctx.font = "bold 130px 'Microsoft YaHei', 'PingFang SC', sans-serif";
        ctx.textAlign = "center";
        ctx.textBaseline = "middle";

        switch (colIdx) {
            case 0: // East (東)
                this.drawCornerBadge(ctx, x + 20, y + 42, "E", this.colNavy);
                ctx.fillStyle = this.colNavy;
                ctx.fillText("東", cx, cy);
                break;
            case 1: // South (南)
                this.drawCornerBadge(ctx, x + 20, y + 42, "S", this.colNavy);
                ctx.fillStyle = this.colNavy;
                ctx.fillText("南", cx, cy);
                break;
            case 2: // West (西)
                this.drawCornerBadge(ctx, x + 20, y + 42, "W", this.colNavy);
                ctx.fillStyle = this.colNavy;
                ctx.fillText("西", cx, cy);
                break;
            case 3: // North (北)
                this.drawCornerBadge(ctx, x + 20, y + 42, "N", this.colNavy);
                ctx.fillStyle = this.colNavy;
                ctx.fillText("北", cx, cy);
                break;
            case 4: // Red Dragon (中)
                this.drawCornerBadge(ctx, x + 20, y + 42, "C", this.colRuby);
                ctx.fillStyle = this.colRuby;
                ctx.fillText("中", cx, cy);
                break;
            case 5: // Green Dragon (發)
                this.drawCornerBadge(ctx, x + 20, y + 42, "F", this.colJade);
                ctx.fillStyle = this.colJade;
                ctx.fillText("發", cx, cy);
                break;
            case 6: // White Dragon (白 - Blank Frame)
                this.drawCornerBadge(ctx, x + 20, y + 42, "P", this.colNavy);
                ctx.strokeStyle = this.colNavy;
                ctx.lineWidth = 14;
                ctx.strokeRect(cx - 55, cy - 75, 110, 150);
                ctx.strokeStyle = this.colBevelGold;
                ctx.lineWidth = 4;
                ctx.strokeRect(cx - 45, cy - 65, 90, 130);
                break;
        }
    }

    // 5. BONUS (FLOWERS & SEASONS)
    drawBonusTile(ctx, x, y, cx, cy, w, h, colIdx) {
        ctx.font = "bold 110px 'Microsoft YaHei', 'PingFang SC', sans-serif";
        ctx.textAlign = "center";
        ctx.textBaseline = "middle";

        const flowerChars = ["梅", "蘭", "菊", "竹"];
        const seasonChars = ["春", "夏", "秋", "冬"];

        if (colIdx < 4) {
            this.drawCornerBadge(ctx, x + 20, y + 42, `F${colIdx + 1}`, this.colRuby);
            ctx.fillStyle = this.colRuby;
            ctx.fillText(flowerChars[colIdx], cx, cy);
        } else if (colIdx < 8) {
            this.drawCornerBadge(ctx, x + 20, y + 42, `S${colIdx - 3}`, this.colNavy);
            ctx.fillStyle = this.colNavy;
            ctx.fillText(seasonChars[colIdx - 4], cx, cy);
        }
    }

    drawCornerBadge(ctx, x, y, text, color) {
        ctx.save();
        ctx.font = "bold 26px 'Segoe UI', Arial, sans-serif";
        ctx.fillStyle = color;
        ctx.textAlign = "left";
        ctx.textBaseline = "middle";
        ctx.fillText(text, x, y);
        ctx.restore();
    }
}
