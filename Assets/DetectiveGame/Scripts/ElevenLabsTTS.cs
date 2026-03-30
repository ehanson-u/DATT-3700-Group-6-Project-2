using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace DetectiveGame
{
    [RequireComponent(typeof(AudioSource))]
    public class ElevenLabsTTS : MonoBehaviour
    {
        [Header("API Settings")]
        [SerializeField] private string _apiKey = "YOUR_ELEVENLABS_API_KEY";
        [SerializeField] private string _voiceId = "pNInz6obpgDQGcFmaJgB";
        [SerializeField] private string _modelId = "eleven_monolingual_v1";

        [Header("Voice Settings")]
        [Range(0f, 1f)][SerializeField] private float _stability = 0.5f;
        [Range(0f, 1f)][SerializeField] private float _similarityBoost = 0.75f;

        private AudioSource _audioSource;
        private bool _isSpeaking = false;

        public bool IsSpeaking => _isSpeaking;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        public void Speak(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            StartCoroutine(SynthesizeAndPlay(text));
        }

        private IEnumerator SynthesizeAndPlay(string text)
        {
            _isSpeaking = true;

            // Use output_format=pcm_22050 for raw PCM audio
            string url = "https://api.elevenlabs.io/v1/text-to-speech/" + _voiceId + "?output_format=pcm_22050";

            string json = "{" +
                "\"text\":\"" + EscapeJson(text) + "\"," +
                "\"model_id\":\"" + _modelId + "\"," +
                "\"voice_settings\":{" +
                    "\"stability\":" + _stability.ToString("F2") + "," +
                    "\"similarity_boost\":" + _similarityBoost.ToString("F2") +
                "}" +
            "}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("xi-api-key", _apiKey);

                Debug.Log("[ElevenLabsTTS] Generating speech...");
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    byte[] pcmData = request.downloadHandler.data;
                    Debug.Log("[ElevenLabsTTS] Received PCM audio: " + pcmData.Length + " bytes.");

                    // Convert raw PCM bytes (16-bit signed, mono, 22050Hz) to float array
                    float[] floatData = new float[pcmData.Length / 2];
                    for (int i = 0; i < floatData.Length; i++)
                    {
                        short sample = BitConverter.ToInt16(pcmData, i * 2);
                        floatData[i] = sample / 32768f;
                    }

                    AudioClip clip = AudioClip.Create("ElevenLabsTTS", floatData.Length, 1, 22050, false);
                    clip.SetData(floatData, 0);

                    _audioSource.clip = clip;
                    _audioSource.Play();

                    Debug.Log("[ElevenLabsTTS] Playing audio: " + clip.length.ToString("F1") + "s");
                    yield return new WaitWhile(() => _audioSource.isPlaying);
                }
                else
                {
                    Debug.LogError("[ElevenLabsTTS] Error: " + request.error);
                    Debug.LogError("[ElevenLabsTTS] Response: " + request.downloadHandler.text);
                }
            }

            _isSpeaking = false;
        }

        private string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }
    }
}