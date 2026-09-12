/**
 * config.js - Konfigurasi Utama & Definisi 144 Ubin Mahjong VIP
 */

export const SUITS = {
    CHARACTER: 2, // 萬 (Wan / Man) -> 1 - 9
    BAMBOO: 0,    // 索 (Sou / Bamboo) -> 1 - 9
    DOT: 1,       // 筒 (Pin / Dots) -> 1 - 9
    WIND: 3,      // 風 (East, South, West, North) -> 1 - 4
    DRAGON: 4,    // 箭 (Red, Green, White) -> 1 - 3
    FLOWER: 5,    // 花 (Plum, Orchid, Chrysanthemum, Bamboo) -> 1 - 4
    SEASON: 6     // 季 (Spring, Summer, Autumn, Winter) -> 1 - 4
};

export const SEATS = {
    SOUTH: 0, // Pemain Lokal
    EAST: 1,  // Bot Kanan
    NORTH: 2, // Bot Atas
    WEST: 3   // Bot Kiri
};

export const SEAT_NAMES = ["South (Anda)", "East (Kenji)", "North (Mei)", "West (Dragon)"];

export const GAME_CONSTANTS = {
    TURN_DURATION: 15,          // Detik per giliran
    HAND_SIZE: 13,              // Jumlah ubin di tangan awal
    DISCARD_ROW_SIZE: 6,        // Jumlah ubin per baris di kolam buangan (Kawa)
    TILE_WIDTH: 0.040,          // 4.0 cm
    TILE_HEIGHT: 0.056,         // 5.6 cm
    TILE_THICKNESS: 0.024,      // 2.4 cm
    TILE_SPACING_X: 0.041,      // 4.1 cm (celah rapi 1mm)
    HAND_POS_Z: 0.272,          // Posisi tangan aktif pemain mundur memberi ruang melds di depan
    HAND_POS_Y: 0.030           // Tinggi dari felt meja
};

/**
 * Menghitung bobot urutan ubin Mahjong standar:
 * Characters (Wan 1-9) -> Bamboo (Sou 1-9) -> Dots (Pin 1-9) -> Winds (E/S/W/N) -> Dragons (C/F/P) -> Flowers/Seasons.
 */
export function getTileSortWeight(suit, value) {
    let suitWeight = 0;
    switch (suit) {
        case SUITS.CHARACTER: suitWeight = 100; break;
        case SUITS.BAMBOO:    suitWeight = 200; break;
        case SUITS.DOT:       suitWeight = 300; break;
        case SUITS.WIND:      suitWeight = 400; break;
        case SUITS.DRAGON:    suitWeight = 500; break;
        case SUITS.FLOWER:    suitWeight = 600; break;
        case SUITS.SEASON:    suitWeight = 700; break;
        default: suitWeight = 900; break;
    }
    return suitWeight + value;
}

export function sortTiles(tiles) {
    if (!tiles || !Array.isArray(tiles)) return [];
    return tiles.sort((a, b) => getTileSortWeight(a.suit, a.value) - getTileSortWeight(b.suit, b.value));
}

/**
 * Membuat deck lengkap 144 Ubin Mahjong standar
 */
export function generateFullDeck() {
    const deck = [];
    let id = 0;

    // 1. Characters / Wan (1 - 9) x 4
    for (let val = 1; val <= 9; val++) {
        for (let copy = 0; copy < 4; copy++) {
            deck.push({ id: id++, suit: SUITS.CHARACTER, value: val, isBonus: false, name: `${val} Wan (萬)` });
        }
    }

    // 2. Bamboo / Sou (1 - 9) x 4
    for (let val = 1; val <= 9; val++) {
        for (let copy = 0; copy < 4; copy++) {
            deck.push({ id: id++, suit: SUITS.BAMBOO, value: val, isBonus: false, name: `${val} Bamboo (索)` });
        }
    }

    // 3. Dots / Pin (1 - 9) x 4
    for (let val = 1; val <= 9; val++) {
        for (let copy = 0; copy < 4; copy++) {
            deck.push({ id: id++, suit: SUITS.DOT, value: val, isBonus: false, name: `${val} Dot (筒)` });
        }
    }

    // 4. Winds (East, South, West, North) x 4
    const windNames = ["East (東)", "South (南)", "West (西)", "North (北)"];
    for (let val = 1; val <= 4; val++) {
        for (let copy = 0; copy < 4; copy++) {
            deck.push({ id: id++, suit: SUITS.WIND, value: val, isBonus: false, name: windNames[val - 1] });
        }
    }

    // 5. Dragons (Red Chun, Green Fa, White Bai) x 4
    const dragonNames = ["Red Dragon (中)", "Green Dragon (發)", "White Dragon (白)"];
    for (let val = 1; val <= 3; val++) {
        for (let copy = 0; copy < 4; copy++) {
            deck.push({ id: id++, suit: SUITS.DRAGON, value: val, isBonus: false, name: dragonNames[val - 1] });
        }
    }

    // 6. Bonus: Flowers (1 - 4) & Seasons (1 - 4) x 1
    const flowerNames = ["Plum (梅)", "Orchid (蘭)", "Chrysanthemum (菊)", "Bamboo (竹)"];
    for (let val = 1; val <= 4; val++) {
        deck.push({ id: id++, suit: SUITS.FLOWER, value: val, isBonus: true, name: `Flower ${flowerNames[val - 1]}` });
    }
    const seasonNames = ["Spring (春)", "Summer (夏)", "Autumn (秋)", "Winter (冬)"];
    for (let val = 1; val <= 4; val++) {
        deck.push({ id: id++, suit: SUITS.SEASON, value: val, isBonus: true, name: `Season ${seasonNames[val - 1]}` });
    }

    return deck;
}
