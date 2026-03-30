using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

namespace DetectiveGame
{
    public class HumeVoiceAnalyzer : MonoBehaviour
    {
        [Header("API Settings")]
        [SerializeField] private string _apiKey = "YOUR_HUME_API_KEY";

        private AudioClip _recordedClip;
        private float _startTime;
        private bool _isRecording = false;

        public string LastVoiceEmotion { get; private set; } = "neutral";
        public string LastVoiceConfidence { get; private set; } = "0.00";
        public bool IsAnalyzing { get; private set; } = false;
        public bool HasResult { get; private set; } = false;

        public void StartRecording()
        {
            if (_isRecording) return;
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("[HumeVoice] No microphone found!");
                return;
            }

            string device = Microphone.devices[0];
            _recordedClip = Microphone.Start(device, false, 30, 16000);
            _startTime = Time.realtimeSinceStartup;
            _isRecording = true;
            HasResult = false;
            Debug.Log("[HumeVoice] Recording started.");
        }

        public void StopRecording()
        {
            if (!_isRecording) return;

            Microphone.End(null);
            _isRecording = false;

            float length = Time.realtimeSinceStartup - _startTime;
            if (length < 0.5f)
            {
                Debug.LogWarning("[HumeVoice] Recording too short, skipping analysis.");
                HasResult = true;
                return;
            }

            _recordedClip = TrimClip(_recordedClip, length);
            Debug.Log("[HumeVoice] Recording stopped. Length: " + length.ToString("F1") + "s");
            StartCoroutine(AnalyzeAudio());
        }

        private AudioClip TrimClip(AudioClip clip, float length)
        {
            int samples = Mathf.Min((int)(clip.frequency * length), clip.samples);
            if (samples <= 0) return clip;

            float[] data = new float[samples];
            clip.GetData(data, 0);

            AudioClip trimmed = AudioClip.Create("recording", samples, clip.channels, clip.frequency, false);
            trimmed.SetData(data, 0);
            return trimmed;
        }

        private IEnumerator AnalyzeAudio()
        {
            IsAnalyzing = true;

            byte[] wavBytes = AudioClipToWav(_recordedClip);
            Debug.Log("[HumeVoice] WAV size: " + wavBytes.Length + " bytes.");

            // Create multipart form with JSON config and audio file
            string url = "https://api.hume.ai/v0/batch/jobs";

            // Build the form manually with the models config
            WWWForm form = new WWWForm();
            form.AddBinaryData("file", wavBytes, "recording.wav", "audio/wav");
            form.AddField("json", "{\"models\":{\"prosody\":{}}}");

            using (UnityWebRequest request = UnityWebRequest.Post(url, form))
            {
                request.SetRequestHeader("X-Hume-Api-Key", _apiKey);

                Debug.Log("[HumeVoice] Uploading to Hume AI...");
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    Debug.Log("[HumeVoice] Job response: " + response);

                    string jobId = ExtractJsonValue(response, "job_id");
                    if (!string.IsNullOrEmpty(jobId))
                    {
                        Debug.Log("[HumeVoice] Job ID: " + jobId);
                        yield return StartCoroutine(PollForResult(jobId));
                    }
                    else
                    {
                        Debug.LogError("[HumeVoice] No job_id in response: " + response);
                        SetFallback();
                    }
                }
                else
                {
                    Debug.LogError("[HumeVoice] Upload failed: " + request.error);
                    Debug.LogError("[HumeVoice] Response: " + request.downloadHandler.text);
                    SetFallback();
                }
            }

            IsAnalyzing = false;
            HasResult = true;
        }

        private IEnumerator PollForResult(string jobId)
        {
            // First wait a bit for processing
            yield return new WaitForSeconds(3f);

            string url = "https://api.hume.ai/v0/batch/jobs/" + jobId + "/predictions";
            int maxAttempts = 10;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.SetRequestHeader("X-Hume-Api-Key", _apiKey);

                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string response = request.downloadHandler.text;
                        Debug.Log("[HumeVoice] Poll attempt " + (attempt + 1) + " response length: " + response.Length);

                        // Check if job is still processing
                        if (response.Contains("\"state\":\"QUEUED\"") || response.Contains("\"state\":\"IN_PROGRESS\""))
                        {
                            Debug.Log("[HumeVoice] Still processing...");
                            yield return new WaitForSeconds(2f);
                            continue;
                        }

                        // Try to parse results
                        if (response.Contains("\"name\"") && response.Contains("\"score\""))
                        {
                            ParseHumeResult(response);
                            Debug.Log("[HumeVoice] Result: " + LastVoiceEmotion + " (" + LastVoiceConfidence + ")");
                            yield break;
                        }
                        else
                        {
                            Debug.LogWarning("[HumeVoice] Unexpected response format: " + response.Substring(0, Mathf.Min(200, response.Length)));
                            yield return new WaitForSeconds(2f);
                            continue;
                        }
                    }
                    else if (request.responseCode == 400 || request.responseCode == 404)
                    {
                        Debug.Log("[HumeVoice] Job not ready (attempt " + (attempt + 1) + ")");
                        yield return new WaitForSeconds(2f);
                        continue;
                    }
                    else
                    {
                        Debug.LogError("[HumeVoice] Poll error: " + request.error + " Code: " + request.responseCode);
                        Debug.LogError("[HumeVoice] Body: " + request.downloadHandler.text);
                        yield return new WaitForSeconds(2f);
                        continue;
                    }
                }
            }

            Debug.LogWarning("[HumeVoice] Timed out after " + maxAttempts + " attempts.");
            SetFallback();
        }

        private void ParseHumeResult(string json)
        {
            try
            {
                string bestEmotion = "neutral";
                float bestScore = 0f;

                // Find all "name":"xxx","score":yyy patterns
                int searchFrom = 0;
                while (true)
                {
                    int nameKeyIdx = json.IndexOf("\"name\"", searchFrom);
                    if (nameKeyIdx == -1) break;

                    // Extract name value
                    int nameStart = json.IndexOf("\"", nameKeyIdx + 6) + 1;
                    if (nameStart == 0) break;
                    int nameEnd = json.IndexOf("\"", nameStart);
                    if (nameEnd == -1) break;
                    string emotionName = json.Substring(nameStart, nameEnd - nameStart);

                    // Extract score value
                    int scoreKeyIdx = json.IndexOf("\"score\"", nameEnd);
                    if (scoreKeyIdx == -1 || scoreKeyIdx > nameEnd + 50)
                    {
                        searchFrom = nameEnd + 1;
                        continue;
                    }

                    int scoreStart = json.IndexOf(":", scoreKeyIdx) + 1;
                    if (scoreStart == 0) break;

                    // Skip whitespace
                    while (scoreStart < json.Length && json[scoreStart] == ' ') scoreStart++;

                    int scoreEnd = scoreStart;
                    while (scoreEnd < json.Length && (char.IsDigit(json[scoreEnd]) || json[scoreEnd] == '.' || json[scoreEnd] == '-' || json[scoreEnd] == 'e' || json[scoreEnd] == 'E' || json[scoreEnd] == '+'))
                        scoreEnd++;

                    string scoreStr = json.Substring(scoreStart, scoreEnd - scoreStart);

                    if (float.TryParse(scoreStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float score))
                    {
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestEmotion = emotionName;
                        }
                    }

                    searchFrom = scoreEnd;
                }

                LastVoiceEmotion = MapToSimple(bestEmotion);
                LastVoiceConfidence = bestScore.ToString("F2");
                Debug.Log("[HumeVoice] Top raw emotion: " + bestEmotion + " → mapped: " + LastVoiceEmotion + " (" + LastVoiceConfidence + ")");
            }
            catch (Exception e)
            {
                Debug.LogError("[HumeVoice] Parse error: " + e.Message);
                SetFallback();
            }
        }

        private string MapToSimple(string humeEmotion)
        {
            switch (humeEmotion.ToLower())
            {
                case "anger": case "contempt": return "angry";
                case "anxiety": case "fear": case "horror": case "doubt": return "nervous";
                case "joy": case "ecstasy": case "excitement": case "enthusiasm": case "triumph": case "amusement": return "happy";
                case "sadness": case "disappointment": case "empathic pain": case "guilt": case "shame": return "sad";
                case "surprise (positive)": case "surprise (negative)": case "realization": return "surprised";
                case "disgust": return "disgusted";
                case "calmness": case "contentment": case "relief": return "calm";
                case "confusion": case "awkwardness": return "confused";
                case "boredom": case "tiredness": return "disengaged";
                case "determination": case "concentration": case "interest": return "focused";
                default: return humeEmotion.ToLower();
            }
        }

        private void SetFallback()
        {
            LastVoiceEmotion = "unknown";
            LastVoiceConfidence = "0.00";
        }

        private string ExtractJsonValue(string json, string key)
        {
            string search = "\"" + key + "\":\"";
            int start = json.IndexOf(search);
            if (start == -1) return null;
            start += search.Length;
            int end = json.IndexOf("\"", start);
            if (end == -1) return null;
            return json.Substring(start, end - start);
        }

        private byte[] AudioClipToWav(AudioClip clip)
        {
            float[] data = new float[clip.samples * clip.channels];
            clip.GetData(data, 0);

            int sampleCount = data.Length;
            int fileSize = 44 + sampleCount * 2;
            byte[] wav = new byte[fileSize];

            System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(wav, 0);
            BitConverter.GetBytes(fileSize - 8).CopyTo(wav, 4);
            System.Text.Encoding.ASCII.GetBytes("WAVE").CopyTo(wav, 8);
            System.Text.Encoding.ASCII.GetBytes("fmt ").CopyTo(wav, 12);
            BitConverter.GetBytes(16).CopyTo(wav, 16);
            BitConverter.GetBytes((short)1).CopyTo(wav, 20);
            BitConverter.GetBytes((short)clip.channels).CopyTo(wav, 22);
            BitConverter.GetBytes(clip.frequency).CopyTo(wav, 24);
            BitConverter.GetBytes(clip.frequency * clip.channels * 2).CopyTo(wav, 28);
            BitConverter.GetBytes((short)(clip.channels * 2)).CopyTo(wav, 32);
            BitConverter.GetBytes((short)16).CopyTo(wav, 34);
            System.Text.Encoding.ASCII.GetBytes("data").CopyTo(wav, 36);
            BitConverter.GetBytes(sampleCount * 2).CopyTo(wav, 40);

            int offset = 44;
            for (int i = 0; i < sampleCount; i++)
            {
                short sample = (short)(Mathf.Clamp(data[i], -1f, 1f) * 32767);
                BitConverter.GetBytes(sample).CopyTo(wav, offset);
                offset += 2;
            }

            return wav;
        }
    }
}