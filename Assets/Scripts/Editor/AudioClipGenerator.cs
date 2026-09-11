using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Match2.EditorTools
{
    /// <summary>
    /// Generates simple procedural SFX (short sine-wave tones with an
    /// exponential decay envelope) to use until real audio is sourced — the
    /// project has no audio assets at all otherwise. Mirrors
    /// <see cref="PlaceholderSpriteGenerator"/>'s role for art: good enough
    /// to be legible and easy to swap out later, not meant as final audio.
    /// </summary>
    public static class AudioClipGenerator
    {
        private const string OutputFolder = "Assets/Audio/Generated";
        private const int SampleRate = 44100;

        [MenuItem("Match2/Generate Blast Sfx")]
        public static void GenerateBlastSfx()
        {
            float[] samples = GenerateTone(frequency: 600f, duration: 0.12f, decayRate: 18f);
            WriteClip("Blast.wav", samples);
        }

        [MenuItem("Match2/Generate Power-Up Sfx")]
        public static void GeneratePowerUpSfx()
        {
            float[] samples = GenerateSweep(startFrequency: 300f, endFrequency: 900f, duration: 0.25f);
            WriteClip("PowerUp.wav", samples);
        }

        [MenuItem("Match2/Generate Level Won Sfx")]
        public static void GenerateLevelWonSfx()
        {
            float[] samples = ConcatSamples(
                GenerateTone(523.25f, 0.15f, 6f), // C5
                GenerateTone(659.25f, 0.15f, 6f), // E5
                GenerateTone(783.99f, 0.35f, 4f)); // G5
            WriteClip("LevelWon.wav", samples);
        }

        [MenuItem("Match2/Generate Level Lost Sfx")]
        public static void GenerateLevelLostSfx()
        {
            float[] samples = ConcatSamples(
                GenerateTone(392.00f, 0.2f, 5f), // G4
                GenerateTone(261.63f, 0.5f, 3f)); // C4
            WriteClip("LevelLost.wav", samples);
        }

        private static float[] GenerateTone(float frequency, float duration, float decayRate)
        {
            int sampleCount = (int)(duration * SampleRate);
            var samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Exp(-decayRate * t);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.5f;
            }

            return samples;
        }

        private static float[] GenerateSweep(float startFrequency, float endFrequency, float duration)
        {
            int sampleCount = (int)(duration * SampleRate);
            var samples = new float[sampleCount];
            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float frequency = Mathf.Lerp(startFrequency, endFrequency, t / duration);
                phase += 2f * Mathf.PI * frequency / SampleRate;
                float envelope = Mathf.Exp(-3f * t);
                samples[i] = Mathf.Sin(phase) * envelope * 0.5f;
            }

            return samples;
        }

        private static float[] ConcatSamples(params float[][] parts)
        {
            int total = 0;
            foreach (float[] part in parts)
                total += part.Length;

            var result = new float[total];
            int offset = 0;
            foreach (float[] part in parts)
            {
                part.CopyTo(result, offset);
                offset += part.Length;
            }

            return result;
        }

        private static void WriteClip(string fileName, float[] samples)
        {
            Directory.CreateDirectory(OutputFolder);
            string path = $"{OutputFolder}/{fileName}";
            WriteWav(path, samples);
            AssetDatabase.ImportAsset(path);
            Debug.Log($"AudioClipGenerator: generated {path}");
        }

        /// <summary>Writes 16-bit PCM mono samples as a standard WAV file — no external library needed for such a simple format.</summary>
        private static void WriteWav(string path, float[] samples)
        {
            using var stream = new FileStream(path, FileMode.Create);
            using var writer = new BinaryWriter(stream);

            const int bitsPerSample = 16;
            const int channels = 1;
            int byteRate = SampleRate * channels * bitsPerSample / 8;
            int dataSize = samples.Length * channels * bitsPerSample / 8;

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1); // PCM
            writer.Write((short)channels);
            writer.Write(SampleRate);
            writer.Write(byteRate);
            writer.Write((short)(channels * bitsPerSample / 8));
            writer.Write((short)bitsPerSample);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            foreach (float sample in samples)
                writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
        }
    }
}
