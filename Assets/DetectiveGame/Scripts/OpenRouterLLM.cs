using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace DetectiveGame
{
    public class OpenRouterLLM : MonoBehaviour
    {
        [Header("API Settings")]
        [SerializeField] private string _apiKey = "YOUR_OPENROUTER_API_KEY_HERE";
        [SerializeField] private string _model = "google/gemma-2-9b-it:free";
        [SerializeField] private string _apiUrl = "https://openrouter.ai/api/v1/chat/completions";

        [Header("Model Settings")]
        [SerializeField] private int _maxTokens = 300;
        [SerializeField] private float _temperature = 0.8f;

        [Header("Detective Settings")]
        [TextArea(5, 15)]
        [SerializeField]
        private string _systemPrompt =
            "You are a detective trying to get to the bottom of a likely attempted murder case. " +
            "You are to be proactive in questioning the user.\n\n" +
            "Setting: High-end restaurant.\n\n" +
            "Prologue: The attempted murder target is a regular visitor to the restaurant. " +
            "Keep your responses concise - 2 to 3 sentences max. Be intimidating but professional.";

        private List<MessageData> _conversationHistory = new List<MessageData>();
        private bool _isProcessing = false;

        public bool IsProcessing => _isProcessing;
        public bool IsReady { get; private set; } = false;

        [Serializable]
        private class MessageData
        {
            public string role;
            public string content;
        }

        [Serializable]
        private class RequestBody
        {
            public string model;
            public List<MessageData> messages;
            public int max_tokens;
            public float temperature;
        }

        [Serializable]
        private class ResponseBody
        {
            public Choice[] choices;
        }

        [Serializable]
        private class Choice
        {
            public MessageData message;
        }

        private void Start()
        {
            _conversationHistory.Add(new MessageData
            {
                role = "system",
                content = _systemPrompt
            });

            IsReady = true;
            Debug.Log("[OpenRouterLLM] Ready. Model: " + _model);
        }

        public void SetSystemPromptSuffix(string suffix)
        {
            if (_conversationHistory.Count > 0 && _conversationHistory[0].role == "system")
            {
                _conversationHistory[0].content += suffix;
            }
        }

        public void SendMessage(string userMessage, Action<string> onResponse, Action<string> onStreamToken = null)
        {
            if (_isProcessing) return;

            _conversationHistory.Add(new MessageData
            {
                role = "user",
                content = userMessage
            });

            StartCoroutine(SendRequest(onResponse));
        }

        private IEnumerator SendRequest(Action<string> onResponse)
        {
            _isProcessing = true;

            var requestBody = new RequestBody
            {
                model = _model,
                messages = _conversationHistory,
                max_tokens = _maxTokens,
                temperature = _temperature
            };

            string jsonBody = JsonUtility.ToJson(requestBody);

            // JsonUtility doesn't serialize List<T> inside objects well,
            // so we build the JSON manually
            jsonBody = BuildRequestJson();

            using (UnityWebRequest request = new UnityWebRequest(_apiUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + _apiKey);
                request.SetRequestHeader("HTTP-Referer", "unity-detective-game");

                Debug.Log("[OpenRouterLLM] Sending request...");
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    Debug.Log("[OpenRouterLLM] Response received.");

                    string assistantMessage = ParseResponse(responseText);

                    if (!string.IsNullOrEmpty(assistantMessage))
                    {
                        _conversationHistory.Add(new MessageData
                        {
                            role = "assistant",
                            content = assistantMessage
                        });

                        onResponse?.Invoke(assistantMessage);
                    }
                    else
                    {
                        Debug.LogError("[OpenRouterLLM] Failed to parse response: " + responseText);
                        onResponse?.Invoke("*The detective stares at you silently.*");
                    }
                }
                else
                {
                    Debug.LogError("[OpenRouterLLM] Request failed: " + request.error);
                    Debug.LogError("[OpenRouterLLM] Response: " + request.downloadHandler.text);
                    onResponse?.Invoke("*The detective is momentarily distracted.*");
                }
            }

            _isProcessing = false;
        }

        private string BuildRequestJson()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("{");
            sb.Append("\"model\":\"" + EscapeJson(_model) + "\",");
            sb.Append("\"max_tokens\":" + _maxTokens + ",");
            sb.Append("\"temperature\":" + _temperature.ToString("F1") + ",");
            sb.Append("\"messages\":[");

            for (int i = 0; i < _conversationHistory.Count; i++)
            {
                var msg = _conversationHistory[i];
                sb.Append("{");
                sb.Append("\"role\":\"" + EscapeJson(msg.role) + "\",");
                sb.Append("\"content\":\"" + EscapeJson(msg.content) + "\"");
                sb.Append("}");
                if (i < _conversationHistory.Count - 1) sb.Append(",");
            }

            sb.Append("]}");
            return sb.ToString();
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
                // Simple manual parse since Unity's JsonUtility struggles with nested arrays
                int contentStart = json.IndexOf("\"content\":");
                if (contentStart == -1) return null;

                // Find the first "content" inside "choices" -> "message"
                int choicesStart = json.IndexOf("\"choices\"");
                if (choicesStart == -1) return null;

                contentStart = json.IndexOf("\"content\":", choicesStart);
                if (contentStart == -1) return null;

                contentStart = json.IndexOf("\"", contentStart + 10) + 1;
                int contentEnd = -1;

                // Find the closing quote, handling escaped quotes
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
                // Unescape
                content = content.Replace("\\n", "\n")
                                 .Replace("\\\"", "\"")
                                 .Replace("\\\\", "\\");
                return content;
            }
            catch (Exception e)
            {
                Debug.LogError("[OpenRouterLLM] Parse error: " + e.Message);
                return null;
            }
        }

        public void ClearHistory()
        {
            string systemPrompt = _conversationHistory.Count > 0 ? _conversationHistory[0].content : _systemPrompt;
            _conversationHistory.Clear();
            _conversationHistory.Add(new MessageData { role = "system", content = systemPrompt });
        }
    }
}