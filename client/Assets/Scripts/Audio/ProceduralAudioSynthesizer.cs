using System;
using UnityEngine;

namespace Mahjong.Audio
{
    /// <summary>
    /// ProceduralAudioSynthesizer: Generator Suara Prosedural Mandiri (Zero-Asset DSP).
    /// Menghasilkan efek audio realistis (ketukan ubin gading/giok, gesekan meja beludru,
    /// nada tombol Action Bar, dan musik kemenangan) murni melalui sintesis matematika C#
    /// tanpa memerlukan file audio eksternal (.mp3/.wav).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ProceduralAudioSynthesizer : MonoBehaviour
    {
        public static ProceduralAudioSynthesizer Instance { get; private set; }

        private AudioSource audioSource;
        private AudioClip tileClickClip;
        private AudioClip tileDiscardClip;
        private AudioClip buttonPopClip;
        private AudioClip victoryFanfareClip;
        private AudioClip timerBeepClip;

        private const int SAMPLE_RATE = 44100;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                audioSource = GetComponent<AudioSource>();
                GenerateAllSoundEffects();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void GenerateAllSoundEffects()
        {
            tileClickClip      = SynthesizeTileClick();
            tileDiscardClip    = SynthesizeTileDiscard();
            buttonPopClip      = SynthesizeButtonPop();
            victoryFanfareClip = SynthesizeVictoryFanfare();
            timerBeepClip      = SynthesizeTimerBeep();
        }

        // =========================================================================
        // METODE PEMUTARAN AUDIO (PLAY METHODS)
        // =========================================================================

        public void PlayTileClick()      => PlayOneShot(tileClickClip, 0.8f);
        public void PlayTileDiscard()    => PlayOneShot(tileDiscardClip, 0.9f);
        public void PlayButtonPop()      => PlayOneShot(buttonPopClip, 0.7f);
        public void PlayVictoryFanfare() => PlayOneShot(victoryFanfareClip, 1.0f);
        public void PlayTimerBeep()      => PlayOneShot(timerBeepClip, 0.6f);

        private void PlayOneShot(AudioClip clip, float volume = 1.0f)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }

        // =========================================================================
        // SINTESIS DSP AUDIO PROCEDURAL (SINE & NOISE MATHEMATICS)
        // =========================================================================

        /// <summary>
        /// Suara ketukan padat ubin gading/giok (Solid Ivory Impact).
        /// </summary>
        private AudioClip SynthesizeTileClick()
        {
            float duration = 0.06f; // 60 ms
            int totalSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float decay = Mathf.Exp(-t * 90f); // Peluruhan cepat
                // Kombinasi frekuensi resonansi batu padat (1400 Hz & 2800 Hz)
                float wave = Mathf.Sin(2f * Mathf.PI * 1400f * t) * 0.6f +
                             Mathf.Sin(2f * Mathf.PI * 2800f * t) * 0.4f;
                samples[i] = wave * decay;
            }

            AudioClip clip = AudioClip.Create("SFX_TileClick", totalSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Suara ubin mendarat di atas meja beludru felt (Velvet Felt Drop).
        /// </summary>
        private AudioClip SynthesizeTileDiscard()
        {
            float duration = 0.12f;
            int totalSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float decay = Mathf.Exp(-t * 40f);
                // Resonansi frekuensi rendah (350 Hz) bercampur gesekan lembut (noise)
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.15f;
                float thud = Mathf.Sin(2f * Mathf.PI * 350f * t) * 0.85f;
                samples[i] = (thud + noise) * decay;
            }

            AudioClip clip = AudioClip.Create("SFX_TileDiscard", totalSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Suara nada klik cerah tombol Action Bar (Chow/Pong/Kong/Win).
        /// </summary>
        private AudioClip SynthesizeButtonPop()
        {
            float duration = 0.10f;
            int totalSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float decay = Mathf.Exp(-t * 30f);
                float wave = Mathf.Sin(2f * Mathf.PI * 880f * t); // Nada A5 (880 Hz)
                samples[i] = wave * decay;
            }

            AudioClip clip = AudioClip.Create("SFX_ButtonPop", totalSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Fanfare akord kemenangan mayor kasino (Triumph Chord Arpeggio).
        /// </summary>
        private AudioClip SynthesizeVictoryFanfare()
        {
            float duration = 1.2f;
            int totalSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            // Akord C Mayor (C5 = 523Hz, E5 = 659Hz, G5 = 784Hz, C6 = 1046Hz)
            float[] freqs = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f };

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float sum = 0;
                for (int f = 0; f < freqs.Length; f++)
                {
                    float noteStartTime = f * 0.15f;
                    if (t >= noteStartTime)
                    {
                        float noteT = t - noteStartTime;
                        float noteDecay = Mathf.Exp(-noteT * 3.5f);
                        sum += Mathf.Sin(2f * Mathf.PI * freqs[f] * noteT) * noteDecay * 0.25f;
                    }
                }
                samples[i] = Mathf.Clamp(sum, -1.0f, 1.0f);
            }

            AudioClip clip = AudioClip.Create("SFX_VictoryFanfare", totalSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Nada peringatan waktu giliran sisa sedikit (< 4 detik).
        /// </summary>
        private AudioClip SynthesizeTimerBeep()
        {
            float duration = 0.08f;
            int totalSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float decay = Mathf.Exp(-t * 25f);
                float wave = Mathf.Sin(2f * Mathf.PI * 1200f * t);
                samples[i] = wave * decay;
            }

            AudioClip clip = AudioClip.Create("SFX_TimerBeep", totalSamples, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
