using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Assets.codes.Network.Messages;
using Assets.codes.voicechat;


public class recording : MonoBehaviour
{
    private AudioClip micClip;
    private string selectedDevice;
    private bool isRecording = false;
    private readonly List<byte> currentLineBytes = new List<byte>();
    private readonly List<IVoiceChunkFilter> voiceFilters = new List<IVoiceChunkFilter>();
    private readonly List<IVoiceLineFilter> voiceLineFilters = new List<IVoiceLineFilter>();
    private float currentLineSeconds = 0f;

    // ¢w¢w Tune these values ¢w¢w
    public const int SAMPLE_RATE = 16000;   // 11025, 22050, 44100 also possible; lower = smaller packets
    public const int RECORD_LENGTH = 1;       // seconds ¡X how long one clip segment is
    public const int PACKET_FREQUENCY_MS = 100; // how often we grab & send data (every 100 ms = 10 packets/sec)
    [SerializeField] private bool filterShortLoudSounds = true;
    [SerializeField] private float shortLoudRmsThreshold = 0.18f;
    [SerializeField] private float maxShortLoudSeconds = 0.25f;
    [SerializeField] private bool trimSoundlessVoice = true;
    [SerializeField] private float silenceTrimThreshold = 0.015f;
    [SerializeField] private float silenceTrimPaddingSeconds = 0.05f;
    [SerializeField] private float volumeDisplayRmsForFull = 0.08f;
    public GameObject VCBubblePrefab;

    private int lastMicPosition = 0;

    void Start()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone detected!");
            return;
        }

        selectedDevice = Microphone.devices[0]; // or let player choose later
        RebuildVoiceFilters();
    }

    public void StartVoice()
    {
        if (isRecording) return;

        micClip = Microphone.Start(
            selectedDevice,
            loop: true,             // important ¡X keeps the ring buffer going
            lengthSec: RECORD_LENGTH,
            frequency: SAMPLE_RATE
        );

        // Wait until recording actually starts (hardware delay)
        while (!(Microphone.GetPosition(selectedDevice) > 0)) { }

        isRecording = true;
        lastMicPosition = 0;
        ResetBufferedLine();
        ResetVoiceFilters();
        ShowVCVolumeDisplay();

        StartCoroutine(RecordAndSendRoutine());
    }

    public void StopVoice()
    {
        if (!isRecording) return;

        StopAllCoroutines();
        SendLatestAudioChunk();
        SendBufferedLine();
        Microphone.End(selectedDevice);
        isRecording = false;
        HideVCVolumeDisplay();
    }

    private IEnumerator RecordAndSendRoutine()
    {
        while (isRecording)
        {
            yield return new WaitForSeconds(PACKET_FREQUENCY_MS / 1000f);

            if (!isRecording) yield break;

            SendLatestAudioChunk();
        }


    }
    private void SendLatestAudioChunk()
    {
        int currentPos = Microphone.GetPosition(selectedDevice);
        if (currentPos == lastMicPosition) return; // no new data

        int length = currentPos - lastMicPosition;
        if (length < 0) // ring buffer wrap-around
        {
            length += micClip.samples;
        }

        if (length == 0) return;

        // Get raw float samples from the position we last read
        float[] samples = new float[length];
        micClip.GetData(samples, lastMicPosition);

        // Convert float[-1..1] -> 16-bit PCM signed bytes (most common format for VoIP)
        byte[] pcmBytes = new byte[length * 2]; // 2 bytes per sample
        float sumSquares = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            sumSquares += samples[i] * samples[i];

            // Scale float to int16 range
            short pcmValue = (short)(samples[i] * 32767f);

            // Little-endian
            pcmBytes[i * 2] = (byte)(pcmValue & 0xFF);
            pcmBytes[i * 2 + 1] = (byte)((pcmValue >> 8) & 0xFF);
        }

        float rms = Mathf.Sqrt(sumSquares / samples.Length);
        UpdateVCVolumeDisplay(rms);
        float chunkDuration = (float)length / SAMPLE_RATE;

        int bytesBeforeFilter = currentLineBytes.Count;
        ApplyVoiceFilters(pcmBytes, rms, chunkDuration, currentLineBytes);
        currentLineSeconds += (currentLineBytes.Count - bytesBeforeFilter) / 2f / SAMPLE_RATE;


        lastMicPosition = currentPos;
    }
    
    private void ShowVCVolumeDisplay()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowVCVolumeDisplay();
        }
    }

    private void HideVCVolumeDisplay()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideVCVolumeDisplay();
        }
    }
    private void RebuildVoiceFilters()
    {
        voiceFilters.Clear();
        voiceFilters.Add(new ShortLoudVoiceFilter(filterShortLoudSounds, shortLoudRmsThreshold, maxShortLoudSeconds));

        voiceLineFilters.Clear();
        voiceLineFilters.Add(new TrimSilenceVoiceLineFilter(trimSoundlessVoice, silenceTrimThreshold, silenceTrimPaddingSeconds, SAMPLE_RATE));
    }

    private void ResetVoiceFilters()
    {
        foreach (IVoiceChunkFilter filter in voiceFilters)
        {
            filter.Reset();
        }
    }

    private void ApplyVoiceFilters(byte[] pcmBytes, float rms, float chunkDuration, List<byte> output)
    {
        if (voiceFilters.Count == 0)
        {
            output.AddRange(pcmBytes);
            return;
        }

        List<byte> input = new List<byte>(pcmBytes);
        List<byte> filtered = new List<byte>();
        for (int i = 0; i < voiceFilters.Count; i++)
        {
            filtered.Clear();
            voiceFilters[i].Process(input.ToArray(), rms, chunkDuration, filtered);
            input.Clear();
            input.AddRange(filtered);
        }

        output.AddRange(input);
    }

    private void FlushVoiceFilters()
    {
        foreach (IVoiceChunkFilter filter in voiceFilters)
        {
            int bytesBeforeFlush = currentLineBytes.Count;
            filter.Flush(currentLineBytes);
            currentLineSeconds += (currentLineBytes.Count - bytesBeforeFlush) / 2f / SAMPLE_RATE;
        }
    }
    private byte[] ApplyVoiceLineFilters(byte[] pcmBytes)
    {
        byte[] filteredBytes = pcmBytes;
        foreach (IVoiceLineFilter filter in voiceLineFilters)
        {
            filteredBytes = filter.Process(filteredBytes);
            if (filteredBytes == null || filteredBytes.Length == 0)
            {
                return new byte[0];
            }
        }

        return filteredBytes;
    }
    private void UpdateVCVolumeDisplay(float rms)
    {
        if (UIManager.Instance == null)
        {
            return;
        }

        float normalizedVolume = volumeDisplayRmsForFull > 0f ? rms / volumeDisplayRmsForFull : 0f;
        UIManager.Instance.UpdateVCVolumeDisplay(normalizedVolume);
    }
    private void ResetBufferedLine()
    {
        currentLineBytes.Clear();
        currentLineSeconds = 0f;
    }
    private void SendBufferedLine()
    {
        FlushVoiceFilters();
        if (currentLineBytes.Count == 0) return;

        byte[] lineBytes = ApplyVoiceLineFilters(currentLineBytes.ToArray());
        if (lineBytes.Length == 0)
        {
            ResetBufferedLine();
            return;
        }

        currentLineSeconds = lineBytes.Length / 2f / SAMPLE_RATE;
        Vector3 spawnpos = GameCore.Instance != null && GameCore.Instance.Local_Player != null ? GameCore.Instance.Local_Player.cam.transform.position : Vector3.zero;
        Vector3 spawndir = GameCore.Instance != null && GameCore.Instance.Local_Player != null ? GameCore.Instance.Local_Player.cam.transform.forward : Vector3.forward;
        //debug voice length
        Debug.Log($"Sending voice line: {currentLineSeconds:F2}s, {lineBytes.Length} bytes, from {spawnpos}, dir {spawndir}");
        NMS_Both_VoicePacket msg = new NMS_Both_VoicePacket(lineBytes, NetworkSystem.Instance.SteamID, spawnpos, spawndir);
        msg.SendMessageAsServerOrClient();

        ResetBufferedLine();
    }
    public voicebubble SpawnVCBubbleForLocal(VoiceBubble data, byte[] voiceLineBytes)
    {
        if (VCBubblePrefab == null)
        {
            Debug.LogError("VCBubblePrefab is not assigned!");
            return null;
        }

        Quaternion rotation = data.sendDirection.sqrMagnitude > 0f
            ? Quaternion.LookRotation(data.sendDirection)
            : Quaternion.identity;

        GameObject bubble = Instantiate(VCBubblePrefab, data.sendPosition, rotation);
        voicebubble voiceBubble = bubble.GetComponent<voicebubble>();
        if (voiceBubble == null)
        {
            Debug.LogError("VCBubblePrefab does not have a voicebubble component!");
            Destroy(bubble);
            return null;
        }

        voiceBubble.Init(data, voiceLineBytes);
        return voiceBubble;
    }
}