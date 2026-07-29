using System;
using System.Collections;
using UnityEngine;

namespace Assets.codes.voicechat
{
    public struct VoiceBubble
    {
        public const int SampleCount = 4;
        public const int Resolution = 3;

        public Vector3 sendPosition;
        public Vector3 sendDirection;
        public PlayerMain player;

        public VoiceBubble(Vector3 sendPosition, Vector3 sendDirection, PlayerMain player)
        {
            this.sendPosition = sendPosition;
            this.sendDirection = sendDirection.sqrMagnitude > 0f ? sendDirection.normalized : Vector3.forward;
            this.player = player;
        }

        
    }

    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(Rigidbody))]
    public class voicebubble : MonoBehaviour
    {
        [SerializeField] private float startSpeed = 4f;
        [SerializeField] private float playbackVolume = 1f;
        [SerializeField] private float sampleGain = 2.5f;
        [SerializeField] private float loudnessLossPerBounce = 0.25f;
        [SerializeField] private float scaleLossPerBounce = 0.18f;
        [SerializeField] private int maxBounces = 5;

        public VoiceBubble data;
        public byte[] voiceBytes;

        private AudioSource audioSource;
        private Rigidbody rb;
        private int bounceCount;
        private Vector3 startScale;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            rb = GetComponent<Rigidbody>();
            startScale = transform.localScale;

            audioSource.loop = false;
            audioSource.playOnAwake = false;

            rb.useGravity = false;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public void Init(VoiceBubble bubbleData, byte[] voiceLineBytes)
        {
            data = bubbleData;
            voiceBytes = voiceLineBytes;
            transform.position = data.sendPosition;

            if (data.sendDirection.sqrMagnitude > 0f)
            {
                transform.forward = data.sendDirection;
            }

            audioSource.clip = CreateClipFromPcm(voiceBytes);
            audioSource.volume = playbackVolume;
            StopAllCoroutines();
            StartCoroutine(PlayWholeClipThenLoop());

            rb.linearVelocity = data.sendDirection.normalized * startSpeed;
        }

        private IEnumerator PlayWholeClipThenLoop()
        {
            if (audioSource.clip == null)
            {
                yield break;
            }

            audioSource.loop = false;
            audioSource.time = 0f;
            audioSource.Play();

            yield return new WaitForSeconds(audioSource.clip.length);

            if (audioSource == null || audioSource.clip == null)
            {
                yield break;
            }

            audioSource.loop = true;
            audioSource.time = 0f;
            audioSource.Play();
        }

        private void OnCollisionEnter(Collision collision)
        {
            bounceCount++;

            float strength = Mathf.Clamp01(1f - loudnessLossPerBounce * bounceCount);
            audioSource.volume = playbackVolume * strength;
            transform.localScale = startScale * Mathf.Clamp01(1f - scaleLossPerBounce * bounceCount);

            if (bounceCount >= maxBounces)
            {
                Destroy(gameObject);
            }
        }

        private AudioClip CreateClipFromPcm(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            float[] samples = new float[bytes.Length / 2];
            for (int i = 0; i < samples.Length; i++)
            {
                short pcmValue = (short)((bytes[i * 2 + 1] << 8) | (bytes[i * 2] & 0xFF));
                samples[i] = Mathf.Clamp(pcmValue / 32767f * sampleGain, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("voiceBubbleLoop", samples.Length, 1, recording.SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
