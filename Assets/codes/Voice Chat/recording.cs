using UnityEngine;
using System.Collections;

public class VoiceRecorder : MonoBehaviour
{
    private AudioClip micClip;
    private string selectedDevice;
    private bool isRecording = false;

    // ¢w¢w Tune these values ¢w¢w
    public const int SAMPLE_RATE = 16000;   // 11025, 22050, 44100 also possible; lower = smaller packets
    private const int RECORD_LENGTH = 1;       // seconds ¡X how long one clip segment is
    private const int PACKET_FREQUENCY_MS = 100; // how often we grab & send data (every 100 ms = 10 packets/sec)

    private int lastMicPosition = 0;

    void Start()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone detected!");
            return;
        }

        selectedDevice = Microphone.devices[0]; // or let player choose later
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

        StartCoroutine(RecordAndSendRoutine());
    }

    public void StopVoice()
    {
        if (!isRecording) return;

        StopAllCoroutines();
        Microphone.End(selectedDevice);
        isRecording = false;
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

        // Convert float[-1..1] ¡÷ 16-bit PCM signed bytes (most common format for VoIP)
        byte[] pcmBytes = new byte[length * 2]; // 2 bytes per sample

        for (int i = 0; i < samples.Length; i++)
        {
            // Scale float to int16 range
            short pcmValue = (short)(samples[i] * 32767f);

            // Little-endian
            pcmBytes[i * 2] = (byte)(pcmValue & 0xFF);
            pcmBytes[i * 2 + 1] = (byte)((pcmValue >> 8) & 0xFF);
        }

        if (NetworkSystem.Instance.IsServer)
        {
            ServerSend.DistributeVoicePacket(NetworkSystem.Instance.PlayerId,pcmBytes);
        }
        else
        {
            ClientSend.VoicePacket(pcmBytes);
        }
            lastMicPosition = currentPos;
    }
}