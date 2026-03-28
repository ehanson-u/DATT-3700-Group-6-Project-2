using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace DetectiveGame
{
    public class VisionAnalyzer : MonoBehaviour
    {
        [Header("API Settings")]
        [SerializeField] private string _apiKey = "YOUR_OPENROUTER_API_KEY_HERE";
        [SerializeField] private string _model = "anthropic/claude-3-haiku";
        [SerializeField] private string _apiUrl = "https://openrouter.ai/api/v1/chat/completions";

        [Header("Webcam")]
        [Tooltip("Leave empty to auto-find the active webcam texture")]
        [SerializeField] private WebCamTexture _webCamTexture;

        [Header("Settings")]
        [SerializeField] private int _captureWidth = 512;
        [SerializeField] private int _captureHeight = 384;

        [TextArea(3, 6)]
        [SerializeField]
        private string _visionPrompt =
            "You are assisting a detective in an interrogation. Briefly describe what you see about this person in 1-2 sentences. " +
            "Focus on: their appearance (hair, clothing, accessories, facial hair, glasses), " +
            "anything they are holding, and notable objects visible behind them or near them. " +
            "Be factual and concise. Do not speculate about emotions. Example: 'Male with short dark hair, wearing a grey t-shirt. A keyboard and two monitors visible behind them.'";

        public string LastDescription { get; private set; } = "";
        public bool IsAnalyzing { get; private set; } = false;

        public void AnalyzeCurrentFrame(Action<string> onResult)
        {
            if (IsAnalyzing)
            {
                onResult?.Invoke(LastDescription);
                return;
            }

            // Try to find webcam texture if not assigned
            WebCamTexture cam = GetWebcamTexture();
            if (cam == null || !cam.isPlaying)
            {
                Debug.LogWarning("[VisionAnalyzer] No active webcam found.");
                onResult?.Invoke("Could not capture image.");
                return;
            }

            StartCoroutine(CaptureAndAnalyze(cam, onResult));
        }

        private WebCamTexture GetWebcamTexture()
        {
            if (_webCamTexture != null) return _webCamTexture;

            // Try to find any active WebCamTexture
            WebCamTexture[] allCams = FindObjectsByType<WebCamTexture>(FindObjectsSortMode.None);
            if (allCams != null && allCams.Length > 0) return allCams[0];

            return null;
        }

        private IEnumerator CaptureAndAnalyze(WebCamTexture cam, Action<string> onResult)
        {
            IsAnalyzing = true;

            // Capture frame to Texture2D
            Texture2D frame = new Texture2D(cam.width, cam.height, TextureFormat.RGB24, false);
            frame.SetPixels(cam.GetPixels());
            frame.Apply();

            // Resize for smaller upload
            Texture2D resized = ResizeTexture(frame, _captureWidth, _captureHeight);
            Destroy(frame);

            // Convert to base64 JPEG
            byte[] jpgBytes = resized.EncodeToJPG(75);
            Destroy(resized);

            string base64Image = Convert.ToBase64String(jpgBytes);
            Debug.Log("[VisionAnalyzer] Captured frame. Size: " + jpgBytes.Length + " bytes.");

            // Send to API
            yield return SendToVisionAPI(base64Image, onResult);

            IsAnalyzing = false;
        }

        private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
            Graphics.Blit(source, rt);

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
            result.ReadPixels(new UnityEngine.Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            return result;
        }

        private IEnumerator SendToVisionAPI(string base64Image, Action<string> onResult)
        {
            string json = BuildVisionRequestJson(base64Image);

            using (UnityWebRequest request = new UnityWebRequest(_apiUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + _apiKey);
                request.SetRequestHeader("HTTP-Referer", "unity-detective-game");

                Debug.Log("[VisionAnalyzer] Sending image to vision API...");
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    string description = ParseResponse(responseText);

                    if (!string.IsNullOrEmpty(description))
                    {
                        LastDescription = description;
                        Debug.Log("[VisionAnalyzer] Description: " + description);
                    }
                    else
                    {
                        Debug.LogError("[VisionAnalyzer] Failed to parse: " + responseText);
                        LastDescription = "Unable to analyze image.";
                    }
                }
                else
                {
                    Debug.LogError("[VisionAnalyzer] Request failed: " + request.error);
                    Debug.LogError("[VisionAnalyzer] Response: " + request.downloadHandler.text);
                    LastDescription = "Vision analysis unavailable.";
                }
            }

            onResult?.Invoke(LastDescription);
        }

        private string BuildVisionRequestJson(string base64Image)
        {
            // OpenRouter/OpenAI vision format with image_url using base64
            string escaped_prompt = EscapeJson(_visionPrompt);

            return "{" +
                "\"model\":\"" + EscapeJson(_model) + "\"," +
                "\"max_tokens\":150," +
                "\"messages\":[{" +
                    "\"role\":\"user\"," +
                    "\"content\":[" +
                        "{\"type\":\"text\",\"text\":\"" + escaped_prompt + "\"}," +
                        "{\"type\":\"image_url\",\"image_url\":{" +
                            "\"url\":\"data:image/jpeg;base64," + base64Image + "\"" +
                        "}}" +
                    "]" +
                "}]" +
            "}";
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

        private string ParseResponse(string json)
        {
            try
            {
                int choicesStart = json.IndexOf("\"choices\"");
                if (choicesStart == -1) return null;

                int contentStart = json.IndexOf("\"content\":", choicesStart);
                if (contentStart == -1) return null;

                contentStart = json.IndexOf("\"", contentStart + 10) + 1;
                int contentEnd = -1;

                for (int i = contentStart; i < json.Length; i++)
                {
                    if (json[i] == '"' && json[i - 1] != '\\')
                    {
                        contentEnd = i;
                        break;
                    }
                }

                if (contentEnd == -1) return null;

                string content = json.Substring(contentStart, contentEnd - contentStart);
                content = content.Replace("\\n", "\n")
                                 .Replace("\\\"", "\"")
                                 .Replace("\\\\", "\\");
                return content;
            }
            catch (Exception e)
            {
                Debug.LogError("[VisionAnalyzer] Parse error: " + e.Message);
                return null;
            }
        }
    }
}