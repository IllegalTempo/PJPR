using System.Collections.Generic;
using UnityEngine;

namespace Assets.codes.voicechat
{
    public interface IVoiceChunkFilter
    {
        void Reset();
        void Process(byte[] pcmBytes, float rms, float chunkDuration, List<byte> output);
        void Flush(List<byte> output);
    }

    public interface IVoiceLineFilter
    {
        byte[] Process(byte[] pcmBytes);
    }

    public class ShortLoudVoiceFilter : IVoiceChunkFilter
    {
        private readonly List<byte> pendingBytes = new List<byte>();
        private readonly bool enabled;
        private readonly float loudRmsThreshold;
        private readonly float maxShortLoudSeconds;
        private float pendingSeconds;

        public ShortLoudVoiceFilter(bool enabled, float loudRmsThreshold, float maxShortLoudSeconds)
        {
            this.enabled = enabled;
            this.loudRmsThreshold = loudRmsThreshold;
            this.maxShortLoudSeconds = maxShortLoudSeconds;
        }

        public void Reset()
        {
            pendingBytes.Clear();
            pendingSeconds = 0f;
        }

        public void Process(byte[] pcmBytes, float rms, float chunkDuration, List<byte> output)
        {
            if (!enabled || pcmBytes == null || pcmBytes.Length == 0)
            {
                AddBytes(output, pcmBytes);
                return;
            }

            if (rms >= loudRmsThreshold)
            {
                pendingBytes.AddRange(pcmBytes);
                pendingSeconds += chunkDuration;

                if (pendingSeconds > maxShortLoudSeconds)
                {
                    output.AddRange(pendingBytes);
                    Reset();
                }

                return;
            }

            if (pendingBytes.Count > 0)
            {
                Debug.Log($"Filtered short loud mic burst: {pendingSeconds:F2}s");
                Reset();
            }

            output.AddRange(pcmBytes);
        }

        public void Flush(List<byte> output)
        {
            if (pendingBytes.Count == 0)
            {
                return;
            }

            if (pendingSeconds > maxShortLoudSeconds)
            {
                output.AddRange(pendingBytes);
            }
            else
            {
                Debug.Log($"Filtered short loud mic burst: {pendingSeconds:F2}s");
            }

            Reset();
        }

        private static void AddBytes(List<byte> output, byte[] bytes)
        {
            if (bytes != null && bytes.Length > 0)
            {
                output.AddRange(bytes);
            }
        }
    }

    public class TrimSilenceVoiceLineFilter : IVoiceLineFilter
    {
        private readonly bool enabled;
        private readonly float silenceRmsThreshold;
        private readonly float keepPaddingSeconds;
        private readonly int sampleRate;

        public TrimSilenceVoiceLineFilter(bool enabled, float silenceRmsThreshold, float keepPaddingSeconds, int sampleRate)
        {
            this.enabled = enabled;
            this.silenceRmsThreshold = silenceRmsThreshold;
            this.keepPaddingSeconds = keepPaddingSeconds;
            this.sampleRate = sampleRate;
        }

        public byte[] Process(byte[] pcmBytes)
        {
            if (!enabled || pcmBytes == null || pcmBytes.Length < 2)
            {
                return pcmBytes;
            }

            int totalSamples = pcmBytes.Length / 2;
            int firstSoundSample = FindFirstSoundSample(pcmBytes, totalSamples);
            if (firstSoundSample < 0)
            {
                Debug.Log("Filtered soundless voice line.");
                return new byte[0];
            }

            int lastSoundSample = FindLastSoundSample(pcmBytes, totalSamples);
            int paddingSamples = Mathf.Max(0, Mathf.RoundToInt(keepPaddingSeconds * sampleRate));
            int startSample = Mathf.Max(0, firstSoundSample - paddingSamples);
            int endSample = Mathf.Min(totalSamples - 1, lastSoundSample + paddingSamples);
            int outputSampleCount = endSample - startSample + 1;
            byte[] output = new byte[outputSampleCount * 2];
            System.Buffer.BlockCopy(pcmBytes, startSample * 2, output, 0, output.Length);

            int removedBytes = pcmBytes.Length - output.Length;
            if (removedBytes > 0)
            {
                Debug.Log($"Trimmed soundless voice: removed {removedBytes / 2f / sampleRate:F2}s");
            }

            return output;
        }

        private int FindFirstSoundSample(byte[] pcmBytes, int totalSamples)
        {
            for (int i = 0; i < totalSamples; i++)
            {
                if (Mathf.Abs(ReadPcmSample(pcmBytes, i) / 32767f) >= silenceRmsThreshold)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindLastSoundSample(byte[] pcmBytes, int totalSamples)
        {
            for (int i = totalSamples - 1; i >= 0; i--)
            {
                if (Mathf.Abs(ReadPcmSample(pcmBytes, i) / 32767f) >= silenceRmsThreshold)
                {
                    return i;
                }
            }

            return -1;
        }

        private short ReadPcmSample(byte[] bytes, int sampleIndex)
        {
            int byteIndex = sampleIndex * 2;
            if (byteIndex < 0 || byteIndex + 1 >= bytes.Length) return 0;

            return (short)((bytes[byteIndex + 1] << 8) | (bytes[byteIndex] & 0xFF));
        }
    }
}
