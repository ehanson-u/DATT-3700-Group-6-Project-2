using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace DetectiveGame
{
    public class RecordAudio : MonoBehaviour
    {
        [SerializeField] private string serverUrl = "http://localhost:8000/predict";

        private AudioClip _recordedClip;
        private float _startTime;
        private string _directoryPath = "Recordings";
        private bool _isRecording = false;

        public string LastVoiceEmotion { get; private set; } = "neutral";
        public string LastVoiceConfidence { get; private set; } = "0.00";
        public bool IsAnalyzing { get; private set; } = false;
        public bool HasResult { get; private set; } = false;

        public event Action<string, string> OnVoiceEmotionResult;

        private void Awake()
        {
            if (!Directory.Exists(_directoryPath))
            {
                Directory.CreateDirectory(_directoryPath);
            }
        }

        public void StartRecording()
        {
            if (_isRecording) return;
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("[RecordAudio] No microphone found!");
                return;
            }

            string device = Microphone.devices[0];
            _recordedClip = Microphone.Start(device, false, 3599, 44100);
            _startTime = Time.realtimeSinceStartup;
            _isRecording = true;
            HasResult = false;
            Debug.Log("[RecordAudio] Recording started.");
        }

        public void StopRecording()
        {
            if (!_isRecording) return;

            Microphone.End(null);
            _isRecording = false;

            float length = Time.realtimeSinceStartup - _startTime;
            _recordedClip = TrimClip(_recordedClip, length);

            string filePath = Path.Combine(_directoryPath, "latest_recording.wav");
            WavUtility.Save(filePath, _recordedClip);
            Debug.Log("[RecordAudio] Recording saved: " + filePath);

            StartCoroutine(UploadAndAnalyze(filePath));
        }

        private AudioClip TrimClip(AudioClip clip, float length)
        {
            int samples = Mathf.Min((int)(clip.frequency * length), clip.samples);
            float[] data = new float[samples];
            clip.GetData(data, 0);

            AudioClip trimmedClip = AudioClip.Create(clip.name, samples,
                clip.channels, clip.frequency, false);
            trimmedClip.SetData(data, 0);
            return trimmedClip;
        }

        private IEnumerator UploadAndAnalyze(string filePath)
        {
            IsAnalyzing = true;

            byte[] fileBytes = File.ReadAllBytes(filePath);
            WWWForm form = new WWWForm();
            form.AddBinaryData("file", fileBytes, "recording.wav", "audio/wav");

            using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    string response = www.downloadHandler.text;
                    Debug.Log("[RecordAudio] Server response: " + response);
                    ParseResponse(response);
                }
                else
                {
                    Debug.LogError("[RecordAudio] Upload failed: " + www.error);
                    LastVoiceEmotion = "unknown";
                    LastVoiceConfidence = "0.00";
                }
            }

            HasResult = true;
            IsAnalyzing = false;
            OnVoiceEmotionResult?.Invoke(LastVoiceEmotion, LastVoiceConfidence);
        }

        private void ParseResponse(string json)
        {
            try
            {
                // Parse {"emotion": "happy", "confidence": "0.85"}
                var result = JsonUtility.FromJson<VoiceEmotionResponse>(json);
                LastVoiceEmotion = result.emotion;
                LastVoiceConfidence = result.confidence;
                Debug.Log("[RecordAudio] Voice emotion: " + LastVoiceEmotion + " (" + LastVoiceConfidence + ")");
            }
            catch (Exception e)
            {
                Debug.LogError("[RecordAudio] Parse error: " + e.Message);
                LastVoiceEmotion = "unknown";
                LastVoiceConfidence = "0.00";
            }
        }

        [Serializable]
        private class VoiceEmotionResponse
        {
            public string emotion;
            public string confidence;
        }
    }
}