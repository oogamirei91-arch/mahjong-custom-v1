/**
 * visualizer3d.js - Engine Visualisasi Meja Mahjong 3D (Three.js)
 * Menggunakan aset asli Gambar 2 (Meja Felt Berpola Emas) dan Gambar 3 (Atlas Ubin Mahjong HD).
 * Memperbaiki orientasi ubin (Right-side UP) dan punggung ubin giok lawan (Jade Back).
 */

import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.160.0/build/three.module.js';
import { GAME_CONSTANTS, SUITS, SEATS } from './config.js';
import { Sound } from './audio.js';

export class Mahjong3DVisualizer {
    constructor(canvasContainer, tileAtlasCanvas, jadeBackCanvas) {
        this.container = canvasContainer;
        this.tileAtlasCanvas = tileAtlasCanvas;
        this.jadeBackCanvas = jadeBackCanvas;

        this.scene = null;
        this.camera = null;
        this.renderer = null;
        this.raycaster = new THREE.Raycaster();
        this.mouse = new THREE.Vector2();

        // Textures & Materials
        this.atlasTexture = null;
        this.feltTexture = null;
        this.jadeTexture = null;
        this.ivoryMaterial = null;
        this.jadeMaterial = null;
        this.tableMaterial = null;

        // Visual Groups
        this.tableGroup = new THREE.Group();
        this.handsGroup = new THREE.Group();
        this.discardsGroup = new THREE.Group();
        this.meldsGroup = new THREE.Group();
        this.compassGroup = new THREE.Group();

        // Tile Meshes references
        this.handMeshes = [[], [], [], []];
        this.discardMeshes = [[], [], [], []];
        this.meldMeshes = [[], [], [], []];

        // Interaction State
        this.selectedTileIndex = -1;
        this.selectedMesh = null;
        this.hoveredMesh = null;
        this.onTileSelectedCallback = null;
        this.onTileDiscardCallback = null;

        // Compass & Timer state
        this.compassTextCanvas = null;
        this.compassTexture = null;
        this.activeTurnSeat = SEATS.SOUTH;
        this.remainingSeconds = 15;

        // Animation loop
        this.isAnimating = false;
        this.animationId = null;

        this.init();
    }

    init() {
        // 1. Scene Setup
        this.scene = new THREE.Scene();
        this.scene.background = new THREE.Color(0x06140e);
        this.scene.fog = new THREE.FogExp2(0x06140e, 0.5);

        // 2. Camera Setup (Perspektif 40 Derajat untuk Kedalaman 3D Optimal)
        const aspect = this.container.clientWidth / this.container.clientHeight;
        this.camera = new THREE.PerspectiveCamera(40, aspect, 0.05, 20);
        this.camera.position.set(0, 0.54, 0.56);
        this.camera.lookAt(0, -0.01, 0.06);

        // 3. Renderer Setup (WebGL with Soft Shadows & Antialiasing)
        this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true, powerPreference: "high-performance" });
        this.renderer.setSize(this.container.clientWidth, this.container.clientHeight);
        this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
        this.renderer.shadowMap.enabled = true;
        this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
        this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
        this.renderer.toneMappingExposure = 1.15;
        this.container.appendChild(this.renderer.domElement);

        // 4. Lighting Setup
        this.setupLights();

        // 5. Load Textures & Materials
        this.setupMaterials();

        // 6. Build 3D Table, Felt & Compass
        this.buildTable();
        this.buildCompass();

        // 7. Add Groups to Scene
        this.scene.add(this.tableGroup);
        this.scene.add(this.handsGroup);
        this.scene.add(this.discardsGroup);
        this.scene.add(this.meldsGroup);
        this.scene.add(this.compassGroup);

        // 8. Event Listeners
        this.setupEventListeners();

        // 9. Start Render Loop
        this.start();
    }

    setupLights() {
        const ambient = new THREE.AmbientLight(0xffffff, 0.88);
        this.scene.add(ambient);

        // Spotlight
        const spot = new THREE.SpotLight(0xfffaed, 2.0, 5, Math.PI / 3, 0.35, 1);
        spot.position.set(0, 1.2, 0.15);
        spot.castShadow = true;
        spot.shadow.mapSize.width = 2048;
        spot.shadow.mapSize.height = 2048;
        spot.shadow.bias = -0.0005;
        this.scene.add(spot);

        // Fill Light
        const fillLight = new THREE.DirectionalLight(0xffdf88, 0.5);
        fillLight.position.set(-0.6, 0.7, 0.5);
        this.scene.add(fillLight);

        // Rim Light
        const rimLight = new THREE.DirectionalLight(0x7ae5b8, 0.4);
        rimLight.position.set(0.6, 0.6, -0.6);
        this.scene.add(rimLight);
    }

    setupMaterials() {
        const textureLoader = new THREE.TextureLoader();

        // 1. Gambar 3: Mahjong Tile Spritesheet Atlas
        this.atlasTexture = textureLoader.load('assets/mahjong_atlas.png', (tex) => {
            tex.generateMipmaps = true;
            tex.minFilter = THREE.LinearMipmapLinearFilter;
            tex.magFilter = THREE.LinearFilter;
            tex.needsUpdate = true;
        }, undefined, () => {
            this.atlasTexture = new THREE.CanvasTexture(this.tileAtlasCanvas);
            this.atlasTexture.needsUpdate = true;
        });

        // 2. Gambar 2: Table Felt Texture
        this.feltTexture = textureLoader.load('assets/table_felt.png', (tex) => {
            tex.wrapS = THREE.ClampToEdgeWrapping;
            tex.wrapT = THREE.ClampToEdgeWrapping;
            tex.generateMipmaps = true;
            tex.minFilter = THREE.LinearMipmapLinearFilter;
        });

        // 3. Jade Back Texture
        this.jadeTexture = new THREE.CanvasTexture(this.jadeBackCanvas);
        this.jadeTexture.wrapS = THREE.RepeatWrapping;
        this.jadeTexture.wrapT = THREE.RepeatWrapping;

        // Front Face Material
        this.ivoryMaterial = new THREE.MeshStandardMaterial({
            color: 0xffffff,
            roughness: 0.22,
            metalness: 0.04,
            map: this.atlasTexture
        });

        // Back / Side Material (Jade Green Resin)
        this.jadeMaterial = new THREE.MeshStandardMaterial({
            color: 0x075e2b,
            roughness: 0.25,
            metalness: 0.12,
            map: this.jadeTexture
        });

        // Table Felt Material
        this.tableMaterial = new THREE.MeshStandardMaterial({
            color: 0xffffff,
            roughness: 0.85,
            metalness: 0.05,
            map: this.feltTexture
        });
    }

    buildTable() {
        const tableGeo = new THREE.BoxGeometry(0.96, 0.02, 0.96);
        const tableMesh = new THREE.Mesh(tableGeo, this.tableMaterial);
        tableMesh.position.y = -0.01;
        tableMesh.receiveShadow = true;
        this.tableGroup.add(tableMesh);

        // Mahogany Wood Outer Frame
        const woodMat = new THREE.MeshStandardMaterial({
            color: 0x1f110a,
            roughness: 0.35,
            metalness: 0.15
        });
        const frameWidth = 1.06;
        const frameHeight = 0.04;
        const frameThick = 0.05;

        const frameN = new THREE.Mesh(new THREE.BoxGeometry(frameWidth, frameHeight, frameThick), woodMat);
        frameN.position.set(0, 0, -0.505);
        this.tableGroup.add(frameN);

        const frameS = new THREE.Mesh(new THREE.BoxGeometry(frameWidth, frameHeight, frameThick), woodMat);
        frameS.position.set(0, 0, 0.505);
        this.tableGroup.add(frameS);

        const frameW = new THREE.Mesh(new THREE.BoxGeometry(frameThick, frameHeight, frameWidth), woodMat);
        frameW.position.set(-0.505, 0, 0);
        this.tableGroup.add(frameW);

        const frameE = new THREE.Mesh(new THREE.BoxGeometry(frameThick, frameHeight, frameWidth), woodMat);
        frameE.position.set(0.505, 0, 0);
        this.tableGroup.add(frameE);
    }

    buildCompass() {
        const compassGeo = new THREE.CylinderGeometry(0.088, 0.092, 0.012, 32);
        const compassBaseMat = new THREE.MeshStandardMaterial({
            color: 0x0f1613,
            metalness: 0.7,
            roughness: 0.25
        });
        const compassMesh = new THREE.Mesh(compassGeo, compassBaseMat);
        compassMesh.position.set(0, 0.006, 0);
        compassMesh.receiveShadow = true;
        this.compassGroup.add(compassMesh);

        // Gold Ring Rim
        const rimGeo = new THREE.TorusGeometry(0.089, 0.003, 16, 32);
        const goldMat = new THREE.MeshStandardMaterial({ color: 0xd4af37, metalness: 0.85, roughness: 0.15 });
        const rimMesh = new THREE.Mesh(rimGeo, goldMat);
        rimMesh.rotation.x = Math.PI / 2;
        rimMesh.position.set(0, 0.012, 0);
        this.compassGroup.add(rimMesh);

        // Canvas for Turn & Countdown LED
        this.compassTextCanvas = document.createElement("canvas");
        this.compassTextCanvas.width = 512;
        this.compassTextCanvas.height = 512;
        this.compassTexture = new THREE.CanvasTexture(this.compassTextCanvas);

        const screenGeo = new THREE.PlaneGeometry(0.155, 0.155);
        const screenMat = new THREE.MeshBasicMaterial({
            map: this.compassTexture,
            transparent: true
        });
        const screenMesh = new THREE.Mesh(screenGeo, screenMat);
        screenMesh.rotation.x = -Math.PI / 2;
        screenMesh.position.set(0, 0.013, 0);
        this.compassGroup.add(screenMesh);

        this.updateCompassDisplay();
    }

    updateCompassDisplay() {
        if (!this.compassTextCanvas) return;
        const ctx = this.compassTextCanvas.getContext("2d");
        ctx.clearRect(0, 0, 512, 512);

        ctx.fillStyle = "#0c1511";
        ctx.beginPath();
        ctx.arc(256, 256, 240, 0, Math.PI * 2);
        ctx.fill();

        const winds = [
            { text: "南 S", x: 256, y: 440, seat: SEATS.SOUTH },
            { text: "東 E", x: 440, y: 256, seat: SEATS.EAST },
            { text: "北 N", x: 256, y: 80, seat: SEATS.NORTH },
            { text: "西 W", x: 80, y: 256, seat: SEATS.WEST }
        ];

        winds.forEach(w => {
            const isActive = this.activeTurnSeat === w.seat;
            ctx.font = isActive ? "bold 44px 'Segoe UI', Arial" : "36px 'Segoe UI', Arial";
            ctx.fillStyle = isActive ? "#00ff88" : "rgba(255, 255, 255, 0.4)";
            ctx.textAlign = "center";
            ctx.textBaseline = "middle";

            if (isActive) {
                ctx.shadowColor = "#00ff88";
                ctx.shadowBlur = 20;
                ctx.beginPath();
                ctx.arc(w.x, w.y, 35, 0, Math.PI * 2);
                ctx.fillStyle = "rgba(0, 255, 136, 0.25)";
                ctx.fill();
                ctx.shadowBlur = 0;
                ctx.fillStyle = "#ffffff";
            }
            ctx.fillText(w.text, w.x, w.y);
        });

        // Center Digital Countdown
        ctx.textAlign = "center";
        ctx.textBaseline = "middle";
        ctx.font = "bold 110px 'Courier New', monospace";

        const isUrgent = this.remainingSeconds <= 5;
        ctx.fillStyle = isUrgent ? "#ff3344" : "#ffea00";
        ctx.shadowColor = isUrgent ? "#ff2233" : "#ffea00";
        ctx.shadowBlur = isUrgent ? 25 : 10;

        const secStr = this.remainingSeconds < 10 ? `0${this.remainingSeconds}` : `${this.remainingSeconds}`;
        ctx.fillText(secStr, 256, 256);
        ctx.shadowBlur = 0;

        this.compassTexture.needsUpdate = true;
    }

    setTurnState(seat, seconds) {
        this.activeTurnSeat = seat;
        this.remainingSeconds = seconds;
        this.updateCompassDisplay();
    }

    /**
     * Membuat Mesh Ubin 3D Dual-Layer (Gading Depan 35% + Giok Belakang 65%)
     */
    createDualLayerTileMesh(tileData) {
        const group = new THREE.Group();
        group.name = "MahjongTile";
        group.userData = { tileData, isSelected: false, basePosY: 0, index: -1 };

        const w = GAME_CONSTANTS.TILE_WIDTH;
        const h = GAME_CONSTANTS.TILE_HEIGHT;
        const totalT = GAME_CONSTANTS.TILE_THICKNESS;

        const frontT = totalT * 0.35; // 0.91 cm
        const backT = totalT * 0.65;  // 1.69 cm

        // 1. Hitung UV Koordinat Ubin pada Gambar 3 (9 Kolom x 5 Baris)
        const uvCoords = this.calculateTileUV(tileData.suit, tileData.value);

        // 2. Geometri Depan (Ivory Face)
        const frontGeo = new THREE.BoxGeometry(w * 0.98, h * 0.98, frontT);
        this.mapTileFaceUV(frontGeo, uvCoords);

        const plainIvorySideMat = new THREE.MeshStandardMaterial({
            color: 0xfcfaf6,
            roughness: 0.22,
            metalness: 0.04
        });
        const frontFaceMat = new THREE.MeshStandardMaterial({
            color: 0xffffff,
            roughness: 0.18,
            metalness: 0.02,
            map: this.atlasTexture
        });

        const frontMats = [
            plainIvorySideMat,
            plainIvorySideMat,
            plainIvorySideMat,
            plainIvorySideMat,
            frontFaceMat, // Muka depan bergambar Kanji / Simbol dari Gambar 3
            plainIvorySideMat
        ];

        const frontMesh = new THREE.Mesh(frontGeo, frontMats);
        frontMesh.position.z = (backT / 2) + (frontT / 2);
        frontMesh.castShadow = true;
        frontMesh.receiveShadow = true;
        group.add(frontMesh);

        // 3. Geometri Belakang (Giok Zamrud)
        const backGeo = new THREE.BoxGeometry(w, h, backT);
        const backMesh = new THREE.Mesh(backGeo, this.jadeMaterial);
        backMesh.position.z = 0;
        backMesh.castShadow = true;
        backMesh.receiveShadow = true;
        group.add(backMesh);

        return group;
    }

    calculateTileUV(suit, value) {
        const cols = 9;
        const rows = 5;
        let row = 0;
        let col = 0;

        switch (suit) {
            case SUITS.CHARACTER: // Wan (1-9) -> Row 0, Col 0..8
                row = 0;
                col = value - 1;
                break;
            case SUITS.BAMBOO:    // Sou (1-9) -> Row 1, Col 0..8
                row = 1;
                col = value - 1;
                break;
            case SUITS.DOT:       // Pin (1-9) -> Row 2, Col 0..8
                row = 2;
                col = value - 1;
                break;
            case SUITS.WIND:      // Winds (East, South, West, North) -> Row 3, Col 0..3
                row = 3;
                col = value - 1;
                break;
            case SUITS.DRAGON:    // Dragons (Red, Green, White) -> Row 3, Col 4..6
                row = 3;
                col = 4 + (value - 1);
                break;
            case SUITS.FLOWER:    // Flowers (Plum, Orchid, Bamboo, Chrysanthemum) -> Row 4, Col 0..3
                row = 4;
                col = value - 1;
                break;
            case SUITS.SEASON:    // Seasons (Spring, Summer, Autumn, Winter) -> Row 4, Col 4..7
                row = 4;
                col = 4 + (value - 1);
                break;
            default:
                row = 0;
                col = 0;
                break;
        }

        const cellW = 1.0 / cols;
        const cellH = 1.0 / rows;
        const padU = cellW * 0.02; // 2% inner inset to crop tile borders cleanly
        const padV = cellH * 0.02;

        const uMin = (col * cellW) + padU;
        const uMax = ((col + 1) * cellW) - padU;
        const vTop = 1.0 - (row * cellH) - padV;
        const vBottom = 1.0 - ((row + 1) * cellH) + padV;

        return { uMin, uMax, vTop, vBottom };
    }

    mapTileFaceUV(geometry, uv) {
        const uvAttr = geometry.attributes.uv;
        // In Three.js BoxGeometry (+Z face):
        // Vertex 16: Top-Left (+Y, -X)     -> (uMin, vTop)
        // Vertex 17: Top-Right (+Y, +X)    -> (uMax, vTop)
        // Vertex 18: Bottom-Left (-Y, -X)  -> (uMin, vBottom)
        // Vertex 19: Bottom-Right (-Y, +X) -> (uMax, vBottom)
        uvAttr.setXY(16, uv.uMin, uv.vTop);
        uvAttr.setXY(17, uv.uMax, uv.vTop);
        uvAttr.setXY(18, uv.uMin, uv.vBottom);
        uvAttr.setXY(19, uv.uMax, uv.vBottom);
        uvAttr.needsUpdate = true;
    }

    /**
     * Render Tangan Pemain Lokal (South / Bawah) - Muka Menghadap Kamera
     */
    renderLocalPlayerHand(handTiles) {
        this.handMeshes[SEATS.SOUTH].forEach(m => this.handsGroup.remove(m));
        this.handMeshes[SEATS.SOUTH] = [];
        this.selectedTileIndex = -1;
        this.selectedMesh = null;

        const count = handTiles.length;
        const spacing = GAME_CONSTANTS.TILE_SPACING_X;
        const totalW = (count - 1) * spacing;
        const startX = -totalW / 2;

        handTiles.forEach((tile, idx) => {
            const mesh = this.createDualLayerTileMesh(tile);
            mesh.userData.index = idx;
            mesh.userData.seat = SEATS.SOUTH;

            const posX = startX + (idx * spacing);
            const posY = GAME_CONSTANTS.HAND_POS_Y;
            const posZ = GAME_CONSTANTS.HAND_POS_Z;

            mesh.position.set(posX, posY, posZ);
            mesh.rotation.x = -THREE.MathUtils.degToRad(16); // Miring 16 derajat menghadap kamera
            mesh.userData.basePosY = posY;

            this.handsGroup.add(mesh);
            this.handMeshes[SEATS.SOUTH].push(mesh);
        });
    }

    /**
     * Render Tangan Bot Lawan (East, North, West) - Menampilkan Punggung Giok (Jade Back)
     */
    renderOpponentHands(opponentCounts = { [SEATS.EAST]: 13, [SEATS.NORTH]: 13, [SEATS.WEST]: 13 }) {
        [SEATS.EAST, SEATS.NORTH, SEATS.WEST].forEach(seat => {
            this.handMeshes[seat].forEach(m => this.handsGroup.remove(m));
            this.handMeshes[seat] = [];
        });

        const spacing = GAME_CONSTANTS.TILE_SPACING_X * 0.95;
        const dummyTile = { suit: SUITS.DRAGON, value: 3, isBonus: false };

        // 1. East (Kanan) -> Punggung Giok Menghadap Tengah Meja
        const eastCount = opponentCounts[SEATS.EAST] || 13;
        const eastStart = -(eastCount - 1) * spacing / 2;
        for (let i = 0; i < eastCount; i++) {
            const mesh = this.createDualLayerTileMesh(dummyTile);
            mesh.position.set(0.36, GAME_CONSTANTS.HAND_POS_Y, eastStart + (i * spacing));
            mesh.rotation.y = Math.PI / 2;
            mesh.rotation.z = -THREE.MathUtils.degToRad(16);
            this.handsGroup.add(mesh);
            this.handMeshes[SEATS.EAST].push(mesh);
        }

        // 2. North (Atas) -> Punggung Giok Menghadap Tengah Meja
        const northCount = opponentCounts[SEATS.NORTH] || 13;
        const northStart = (northCount - 1) * spacing / 2;
        for (let i = 0; i < northCount; i++) {
            const mesh = this.createDualLayerTileMesh(dummyTile);
            mesh.position.set(northStart - (i * spacing), GAME_CONSTANTS.HAND_POS_Y, -0.36);
            mesh.rotation.y = Math.PI;
            mesh.rotation.x = -THREE.MathUtils.degToRad(16);
            this.handsGroup.add(mesh);
            this.handMeshes[SEATS.NORTH].push(mesh);
        }

        // 3. West (Kiri) -> Punggung Giok Menghadap Tengah Meja
        const westCount = opponentCounts[SEATS.WEST] || 13;
        const westStart = (westCount - 1) * spacing / 2;
        for (let i = 0; i < westCount; i++) {
            const mesh = this.createDualLayerTileMesh(dummyTile);
            mesh.position.set(-0.36, GAME_CONSTANTS.HAND_POS_Y, westStart - (i * spacing));
            mesh.rotation.y = -Math.PI / 2;
            mesh.rotation.z = THREE.MathUtils.degToRad(16);
            this.handsGroup.add(mesh);
            this.handMeshes[SEATS.WEST].push(mesh);
        }
    }

    /**
     * Render Kolam Ubin Buangan (Discard River)
     */
    renderDiscardRiver(discardsBySeat) {
        for (let seat = 0; seat < 4; seat++) {
            this.discardMeshes[seat].forEach(m => this.discardsGroup.remove(m));
            this.discardMeshes[seat] = [];
        }

        const tileW = GAME_CONSTANTS.TILE_WIDTH * 0.88;
        const tileH = GAME_CONSTANTS.TILE_HEIGHT * 0.88;
        const rowSize = GAME_CONSTANTS.DISCARD_ROW_SIZE;

        for (let seat = 0; seat < 4; seat++) {
            const tiles = discardsBySeat[seat] || [];
            tiles.forEach((tile, idx) => {
                const mesh = this.createDualLayerTileMesh(tile);
                const col = idx % rowSize;
                const row = Math.floor(idx / rowSize);

                let x = 0, z = 0, rotY = 0;
                const offsetX = (col - (rowSize / 2) + 0.5) * (tileW + 0.003);
                const offsetZ = row * (tileH + 0.004);

                switch (seat) {
                    case SEATS.SOUTH:
                        x = offsetX;
                        z = 0.115 + offsetZ;
                        rotY = 0;
                        break;
                    case SEATS.EAST:
                        x = 0.115 + offsetZ;
                        z = -offsetX;
                        rotY = -Math.PI / 2;
                        break;
                    case SEATS.NORTH:
                        x = -offsetX;
                        z = -0.115 - offsetZ;
                        rotY = Math.PI;
                        break;
                    case SEATS.WEST:
                        x = -0.115 - offsetZ;
                        z = offsetX;
                        rotY = Math.PI / 2;
                        break;
                }

                mesh.position.set(x, 0.013, z);
                mesh.rotation.x = -Math.PI / 2;
                mesh.rotation.z = rotY;

                this.discardsGroup.add(mesh);
                this.discardMeshes[seat].push(mesh);
            });
        }
    }

    /**
     * Render Melds Terbuka (Pong, Chow, Kong) di Sudut Meja Setiap Pemain
     */
    renderMelds(meldsBySeat) {
        for (let seat = 0; seat < 4; seat++) {
            this.meldMeshes[seat].forEach(m => this.meldsGroup.remove(m));
            this.meldMeshes[seat] = [];
        }

        const tileW = GAME_CONSTANTS.TILE_WIDTH * 0.82;
        const spacing = tileW + 0.002;

        for (let seat = 0; seat < 4; seat++) {
            const melds = meldsBySeat[seat] || [];
            let totalMeldTileIdx = 0;

            melds.forEach(meld => {
                meld.tiles.forEach(tile => {
                    const mesh = this.createDualLayerTileMesh(tile);
                    let x = 0, z = 0, rotY = 0;

                    if (seat === SEATS.SOUTH) {
                        x = 0.32 + (totalMeldTileIdx * spacing);
                        z = GAME_CONSTANTS.HAND_POS_Z - 0.02;
                        rotY = 0;
                    } else if (seat === SEATS.EAST) {
                        x = 0.36;
                        z = 0.32 + (totalMeldTileIdx * spacing);
                        rotY = -Math.PI / 2;
                    } else if (seat === SEATS.NORTH) {
                        x = -0.32 - (totalMeldTileIdx * spacing);
                        z = -0.36;
                        rotY = Math.PI;
                    } else if (seat === SEATS.WEST) {
                        x = -0.36;
                        z = -0.32 - (totalMeldTileIdx * spacing);
                        rotY = Math.PI / 2;
                    }

                    mesh.position.set(x, 0.013, z);
                    mesh.rotation.x = -Math.PI / 2;
                    mesh.rotation.z = rotY;

                    this.meldsGroup.add(mesh);
                    this.meldMeshes[seat].push(mesh);
                    totalMeldTileIdx++;
                });
            });
        }
    }

    setupEventListeners() {
        window.addEventListener('resize', () => this.onWindowResize());

        const getPointerPos = (e) => {
            const rect = this.renderer.domElement.getBoundingClientRect();
            const clientX = e.clientX || (e.touches && e.touches[0] ? e.touches[0].clientX : 0);
            const clientY = e.clientY || (e.touches && e.touches[0] ? e.touches[0].clientY : 0);
            return {
                x: ((clientX - rect.left) / rect.width) * 2 - 1,
                y: -((clientY - rect.top) / rect.height) * 2 + 1
            };
        };

        const handlePointerDown = (e) => {
            const pos = getPointerPos(e);
            this.mouse.x = pos.x;
            this.mouse.y = pos.y;

            this.raycaster.setFromCamera(this.mouse, this.camera);
            const playerMeshes = this.handMeshes[SEATS.SOUTH];
            const intersects = this.raycaster.intersectObjects(playerMeshes, true);

            if (intersects.length > 0) {
                let target = intersects[0].object;
                while (target.parent && target.parent !== this.handsGroup) {
                    target = target.parent;
                }

                if (target && target.userData && target.userData.seat === SEATS.SOUTH) {
                    this.onTileClicked(target);
                }
            }
        };

        this.renderer.domElement.addEventListener('pointerdown', handlePointerDown);
    }

    onTileClicked(mesh) {
        const clickedIdx = mesh.userData.index;

        if (this.selectedTileIndex === clickedIdx) {
            Sound.playTileDiscard();
            if (this.onTileDiscardCallback) {
                this.onTileDiscardCallback(mesh.userData.tileData, clickedIdx);
            }
            this.deselectTile();
        } else {
            this.selectTile(mesh, clickedIdx);
            Sound.playTileClick();
            if (this.onTileSelectedCallback) {
                this.onTileSelectedCallback(mesh.userData.tileData, clickedIdx);
            }
        }
    }

    selectTile(mesh, index) {
        this.deselectTile();
        this.selectedTileIndex = index;
        this.selectedMesh = mesh;

        mesh.position.y = mesh.userData.basePosY + 0.024;
        mesh.userData.isSelected = true;
    }

    deselectTile() {
        if (this.selectedMesh) {
            this.selectedMesh.position.y = this.selectedMesh.userData.basePosY;
            this.selectedMesh.userData.isSelected = false;
            this.selectedMesh = null;
            this.selectedTileIndex = -1;
        }
    }

    onWindowResize() {
        if (!this.container || !this.renderer || !this.camera) return;
        const w = this.container.clientWidth;
        const h = this.container.clientHeight;
        this.camera.aspect = w / h;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(w, h);
    }

    start() {
        if (this.isAnimating) return;
        this.isAnimating = true;

        const animate = () => {
            if (!this.isAnimating) return;
            this.animationId = requestAnimationFrame(animate);
            this.renderer.render(this.scene, this.camera);
        };
        animate();
    }

    stop() {
        this.isAnimating = false;
        if (this.animationId) {
            cancelAnimationFrame(this.animationId);
            this.animationId = null;
        }
    }

    destroy() {
        this.stop();
        if (this.renderer && this.renderer.domElement) {
            this.container.removeChild(this.renderer.domElement);
            this.renderer.dispose();
        }
    }
}
