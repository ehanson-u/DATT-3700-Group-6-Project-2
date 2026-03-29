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

        [Header("Webcam Source")]
        [Tooltip("Drag any RawImage or Renderer showing the webcam feed here. Leave empty to use screen capture.")]
        [SerializeField] private Texture _webcamSourceTexture;

        [Header("Settings")]
        [SerializeField] private int _captureWidth = 512;
        [SerializeField] private int _captureHeight = 384;

        [TextArea(3, 6)]
        [SerializeField]
        private string _visionPrompt =
            "You are assisting a detective in an interrogation. Briefly describe what you see about this person in 1-2 sentences. " +
            "Focus on: their appearance (hair, clothing, accessories, facial hair, glasses), " +
            "anything they are holding, and notable objects visible behind them or near them. " +
            "Be factual and concise. Example: 'Male with short dark hair, wearing a grey t-shirt. A keyboard and two monitors visible behind them.'";

        public string LastDescription { get; private set; } = "";
        public bool IsAnalyzing { get; private set; } = false;
        public bool IsReady { get; private set; } = true;

        public void AnalyzeCurrentFrame(Action<string> onResult)
        {
            if (IsAnalyzing)
            {
                onResult?.Invoke(LastDescription);
                return;
            }

            StartCoroutine(CaptureAndAnalyze(onResult));
        }

        private IEnumerator CaptureAndAnalyze(Action<string> onResult)
        {
            IsAnalyzing = true;

            yield return new WaitForEndOfFrame();

            Texture2D frame = null;

            // Try to find the webcam texture MediaPipe is using
            Texture sourceTexture = _webcamSourceTexture;

            if (sourceTexture == null)
            {
                // Search for any active WebCamTexture in the scene
                WebCamTexture[] allCams = FindObjectsByType<WebCamTexture>(FindObjectsSortMode.None);
                if (allCams != null && allCams.Length > 0)
                {
                    sourceTexture = allCams[0];
                }
            }

            if (sourceTexture != null)
            {
                // Copy from the existing webcam texture
                RenderTexture rt = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height);
                Graphics.Blit(sourceTexture, rt);

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = rt;

                frame = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGB24, false);
                frame.ReadPixels(new UnityEngine.Rect(0, 0, sourceTexture.width, sourceTexture.height), 0, 0);
                frame.Apply();

                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }
            else
            {
                // Fallback: screen capture
                Debug.LogWarning("[VisionAnalyzer] No webcam texture found, using screen capture.");
                frame = ScreenCapture.CaptureScreenshotAsTexture();
            }

            // Resize for smaller upload
            Texture2D resized = ResizeTexture(frame, _captureWidth, _captureHeight);
            Destroy(frame);

            byte[] jpgBytes = resized.EncodeToJPG(75);
            Destroy(resized);

            string base64Image = Convert.ToBase64String(jpgBytes);
            Debug.Log("[VisionAnalyzer] Captured frame. Size: " + jpgBytes.Length + " bytes.");

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