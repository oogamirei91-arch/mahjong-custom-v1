/**
 * sw.js - Service Worker untuk Offline Caching Mahjong VIP 3D PWA
 */

const CACHE_NAME = 'mahjong-vip-3d-v1.0';
const ASSETS_TO_CACHE = [
    './',
    './index.html',
    './manifest.json',
    './css/style.css',
    './js/config.js',
    './js/audio.js',
    './js/tileAtlas.js',
    './js/visualizer3d.js',
    './js/gameLogic.js',
    './js/aiManager.js',
    './js/networkManager.js',
    './js/main.js'
];

self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME).then((cache) => {
            console.log('[Service Worker] Caching app shell & assets...');
            return cache.addAll(ASSETS_TO_CACHE);
        })
    );
    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    event.waitUntil(
        caches.keys().then((keyList) => {
            return Promise.all(
                keyList.map((key) => {
                    if (key !== CACHE_NAME) {
                        console.log('[Service Worker] Menghapus cache lama:', key);
                        return caches.delete(key);
                    }
                })
            );
        })
    );
    self.clients.claim();
});

self.addEventListener('fetch', (event) => {
    event.respondWith(
        caches.match(event.request).then((response) => {
            // Return cached asset if available, else fetch network
            return response || fetch(event.request);
        })
    );
});
