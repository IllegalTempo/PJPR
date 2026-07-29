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
}
