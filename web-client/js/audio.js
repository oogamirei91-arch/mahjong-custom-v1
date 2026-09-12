/**
 * audio.js - Synthesizer Efek Suara Prosedural (Web Audio API)
 * Menghasilkan suara ubin keramik, tap, discard felt drop, dan victory fanfare tanpa perlu load file mp3 eksternal!
 */

class SoundSynthesizer {
    constructor() {
        this.ctx = null;
        this.isMuted = false;
    }

    init() {
        if (!this.ctx) {
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            if (AudioContext) {
                this.ctx = new AudioContext();
            }
        }
        if (this.ctx && this.ctx.state === 'suspended') {
            this.ctx.resume();
        }
    }

    // Suara ketukan ubin (Tile Click / Select)
    playTileClick() {
        if (this.isMuted) return;
        this.init();
        if (!this.ctx) return;

        const now = this.ctx.currentTime;
        const osc = this.ctx.createOscillator();
        const gain = this.ctx.createGain();

        osc.type = 'triangle';
        osc.frequency.setValueAtTime(1400, now);
        osc.frequency.exponentialRampToValueAtTime(320, now + 0.06);

        gain.gain.setValueAtTime(0.35, now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.07);

        osc.connect(gain);
        gain.connect(this.ctx.destination);

        osc.start(now);
        osc.stop(now + 0.08);
    }

    // Suara ubin dijatuhkan ke meja felt (Tile Discard Drop)
    playTileDiscard() {
        if (this.isMuted) return;
        this.init();
        if (!this.ctx) return;

        const now = this.ctx.currentTime;
        
        // Low thump
        const osc = this.ctx.createOscillator();
        const gain = this.ctx.createGain();
        osc.type = 'sine';
        osc.frequency.setValueAtTime(240, now);
        osc.frequency.exponentialRampToValueAtTime(60, now + 0.12);

        gain.gain.setValueAtTime(0.5, now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.13);

        osc.connect(gain);
        gain.connect(this.ctx.destination);

        osc.start(now);
        osc.stop(now + 0.14);

        // High ceramic click
        const clickOsc = this.ctx.createOscillator();
        const clickGain = this.ctx.createGain();
        clickOsc.type = 'triangle';
        clickOsc.frequency.setValueAtTime(1800, now);
        clickOsc.frequency.exponentialRampToValueAtTime(800, now + 0.04);
        clickGain.gain.setValueAtTime(0.2, now);
        clickGain.gain.exponentialRampToValueAtTime(0.001, now + 0.05);

        clickOsc.connect(clickGain);
        clickGain.connect(this.ctx.destination);

        clickOsc.start(now);
        clickOsc.stop(now + 0.06);
    }

    // Suara tombol UI (Button Pop)
    playButtonPop() {
        if (this.isMuted) return;
        this.init();
        if (!this.ctx) return;

        const now = this.ctx.currentTime;
        const osc = this.ctx.createOscillator();
        const gain = this.ctx.createGain();

        osc.type = 'sine';
        osc.frequency.setValueAtTime(520, now);
        osc.frequency.exponentialRampToValueAtTime(880, now + 0.08);

        gain.gain.setValueAtTime(0.25, now);
        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.09);

        osc.connect(gain);
        gain.connect(this.ctx.destination);

        osc.start(now);
        osc.stop(now + 0.10);
    }

    // Suara Kemenangan (Victory Fanfare)
    playVictoryFanfare() {
        if (this.isMuted) return;
        this.init();
        if (!this.ctx) return;

        const notes = [523.25, 659.25, 783.99, 1046.50]; // C5, E5, G5, C6
        const now = this.ctx.currentTime;

        notes.forEach((freq, idx) => {
            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();
            const startT = now + (idx * 0.14);

            osc.type = 'triangle';
            osc.frequency.setValueAtTime(freq, startT);

            gain.gain.setValueAtTime(0.3, startT);
            gain.gain.exponentialRampToValueAtTime(0.001, startT + 0.45);

            osc.connect(gain);
            gain.connect(this.ctx.destination);

            osc.start(startT);
            osc.stop(startT + 0.5);
        });
    }
}

export const Sound = new SoundSynthesizer();
