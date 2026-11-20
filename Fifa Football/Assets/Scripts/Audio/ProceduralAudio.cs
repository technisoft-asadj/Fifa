using UnityEngine;

namespace FootballSim.Audio
{
    /// <summary>
    /// Original sound effects synthesised from maths, so the game has sound without any downloaded or licensed assets.
    /// Everything returns mono float samples in -1..1 (deterministic: noise uses a fixed seed).
    /// </summary>
    public static class ProceduralAudio
    {
        public const int SampleRate = 22050;

        static float[] Make(float seconds) { return new float[Mathf.CeilToInt(seconds * SampleRate)]; }

        /// <summary>Ball being struck: a short low thump plus a click of noise.</summary>
        public static float[] Kick(float seconds = 0.18f)
        {
            var d = Make(seconds); var rng = new System.Random(1); float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)SampleRate;
                float f = Mathf.Lerp(150f, 55f, Mathf.Clamp01(t / 0.1f));
                phase += 2f * Mathf.PI * f / SampleRate;
                float thump = Mathf.Sin(phase) * Mathf.Exp(-t * 22f);
                float click = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 70f) * 0.45f;
                d[i] = Mathf.Clamp((thump + click) * 0.9f, -1f, 1f);
            }
            return d;
        }

        /// <summary>Tackle / body contact: a duller, longer thud.</summary>
        public static float[] Thud(float seconds = 0.22f)
        {
            var d = Make(seconds); var rng = new System.Random(2); float phase = 0f, lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)SampleRate;
                phase += 2f * Mathf.PI * 75f / SampleRate;
                lp += 0.08f * (((float)rng.NextDouble() * 2f - 1f) - lp);
                d[i] = Mathf.Clamp((Mathf.Sin(phase) * 0.8f + lp * 1.2f) * Mathf.Exp(-t * 16f), -1f, 1f);
            }
            return d;
        }

        /// <summary>Referee whistle: ~2.9 kHz tone with a fast trill.</summary>
        public static float[] Whistle(float seconds = 0.55f)
        {
            var d = Make(seconds); float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)SampleRate;
                float f = 2900f + 110f * Mathf.Sin(2f * Mathf.PI * 28f * t);
                phase += 2f * Mathf.PI * f / SampleRate;
                float env = Mathf.Clamp01(t / 0.02f) * Mathf.Clamp01((seconds - t) / 0.06f);
                float trill = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 28f * t);
                d[i] = Mathf.Sin(phase) * env * trill * 0.5f;
            }
            return d;
        }

        /// <summary>Crowd roar: low-passed noise that swells and fades.</summary>
        public static float[] Cheer(float seconds = 2.2f)
        {
            var d = Make(seconds); var rng = new System.Random(3); float lp = 0f, lp2 = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)SampleRate;
                float n = (float)rng.NextDouble() * 2f - 1f;
                lp += 0.22f * (n - lp);
                lp2 += 0.05f * (n - lp2);
                float env = Mathf.Clamp01(t / 0.35f) * Mathf.Clamp01((seconds - t) / 0.9f);
                float swell = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 3.1f * t);
                d[i] = Mathf.Clamp((lp * 0.9f + lp2 * 1.6f) * env * swell, -1f, 1f);
            }
            return d;
        }

        /// <summary>A calm looping background tune: four soft chords, each fading in and out so the loop has no click.</summary>
        public static float[] Music()
        {
            const float chordSeconds = 3f;
            float[][] chords =
            {
                new[] { 220.00f, 261.63f, 329.63f },  // A minor
                new[] { 174.61f, 220.00f, 261.63f },  // F major
                new[] { 261.63f, 329.63f, 392.00f },  // C major
                new[] { 196.00f, 246.94f, 293.66f },  // G major
            };
            var d = Make(chordSeconds * chords.Length);
            for (int c = 0; c < chords.Length; c++)
            {
                int start = Mathf.RoundToInt(c * chordSeconds * SampleRate), len = Mathf.RoundToInt(chordSeconds * SampleRate);
                for (int i = 0; i < len && start + i < d.Length; i++)
                {
                    float t = i / (float)SampleRate;
                    float env = Mathf.Clamp01(t / 0.6f) * Mathf.Clamp01((chordSeconds - t) / 0.9f);
                    float v = 0f;
                    foreach (float f in chords[c]) v += Mathf.Sin(2f * Mathf.PI * f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.12f;
                    v += Mathf.Sin(2f * Mathf.PI * chords[c][0] * 0.5f * t) * 0.6f; // the bass
                    float beat = 0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * 2f * t);
                    d[start + i] += v * 0.16f * env * beat;
                }
            }
            return d;
        }

        /// <summary>A stadium murmur that loops without a seam: low-passed noise with slow swells, its end cross-faded into its start.</summary>
        public static float[] Ambience(float seconds = 6f)
        {
            int n = Mathf.CeilToInt(seconds * SampleRate), x = SampleRate / 2;
            var raw = new float[n + x]; var rng = new System.Random(11); float lp = 0f, lp2 = 0f;
            for (int i = 0; i < raw.Length; i++)
            {
                float t = i / (float)SampleRate;
                float w = (float)rng.NextDouble() * 2f - 1f;
                lp += 0.06f * (w - lp); lp2 += 0.015f * (w - lp2);
                raw[i] = (lp * 0.8f + lp2 * 2.2f) * (0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * 0.35f * t));
            }
            var d = new float[n];
            for (int i = 0; i < n; i++) d[i] = i < x ? Mathf.Lerp(raw[n + i], raw[i], i / (float)x) : raw[i];
            return d;
        }

        public static AudioClip ToClip(string name, float[] samples)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
