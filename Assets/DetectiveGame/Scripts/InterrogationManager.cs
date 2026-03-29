using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mediapipe.Unity.Sample.FaceLandmarkDetection;

namespace DetectiveGame
{
    public enum LLMMode
    {
        Local,
        OpenRouter
    }

    public class InterrogationManager : MonoBehaviour
    {
        [Header("LLM Mode")]
        [SerializeField] private LLMMode _llmMode = LLMMode.Local;

        [Header("References")]
        [SerializeField] private EmotionDetector _emotionDetector;
        [SerializeField] private RecordAudio _voiceAnalyzer;
        [SerializeField] private VisionAnalyzer _visionAnalyzer;

        [Tooltip("For Local mode: drag the LLM Detective GameObject here")]
        [SerializeField] private MonoBehaviour _detectiveAgentLocal;

        [Tooltip("For OpenRouter mode: drag the OpenRouterLLM GameObject here")]
        [SerializeField] private OpenRouterLLM _openRouterLLM;

        [Tooltip("Drag the PiperManager GameObject here")]
        [SerializeField] private PiperManager _piperTTS;

        [Header("Settings")]
        [SerializeField] private bool _injectEmotionIntoPrompt = true;
        [SerializeField] private bool _useVisionAnalysis = true;

        [Tooltip("Analyze vision every N messages (1 = every message, 3 = every 3rd)")]
        [SerializeField] private int _visionAnalysisFrequency = 2;

        [Tooltip("Max seconds to wait for voice/vision analysis before sending anyway")]
        [SerializeField] private float _analysisTimeout = 8f;

        [TextArea(3, 6)]
        [SerializeField]
        private string _emotionSystemSuffix =
            "\n\nIMPORTANT: Each message from the player includes notes about their current facial expression, voice emotion, and sometimes a visual description of their appearance and surroundings. " +
            "Use ALL of this to inform your questioning. If their face looks calm but their voice is nervous, they are trying to hide something. " +
            "If you see something interesting in their appearance or surroundings, you can reference it naturally. " +
            "React naturally as a detective reading body language, tone, and visual cues. " +
            "Do NOT explicitly say 'I can see you look nervous' or reference the emotion data directly - react naturally like a real detective.";

        private List<string> _conversationLog = new List<string>();
        private string _currentDetectiveText = "";
        private bool _isWaitingForResponse = false;
        private bool _isReady = false;
        private string _streamingText = "";
        private bool _isSpeaking = false;
        private int _messageCount = 0;
        private string _lastVisionDescription = "";
        private string _statusText = "";

        public string CurrentDetectiveText => _currentDetectiveText;
        public bool IsWaitingForResponse => _isWaitingForResponse;
        public bool IsReady => _isReady;
        public List<string> ConversationLog => _conversationLog;
        public string StreamingText => _streamingText;
        public bool IsSpeaking => _isSpeaking;
        public string StatusText => _statusText;

        private async void Start()
        {
            if (_llmMode == LLMMode.Local)
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                var agent = _detectiveAgentLocal as LLMUnity.LLMAgent;
                if (agent == null)
                {
                    Debug.LogError("InterrogationManager: No LLMAgent assigned for Local mode!");
                    return;
                }

                if (_injectEmotionIntoPrompt)
                {
                    agent.systemPrompt += _emotionSystemSuffix;
                }

                await agent.Warmup();
                _isReady = true;

                string response = await agent.Chat(
                    "[The suspect has just sat down in the interrogation room. Begin your questioning.]",
                    null, null, true);

                if (response != null)
                {
                    _currentDetectiveText = response;
                    _conversationLog.Add("[Detective]: " + response);
                    SpeakText(response);
                }
#endif
            }
            else if (_llmMode == LLMMode.OpenRouter)
            {
                if (_openRouterLLM == null)
                {
                    Debug.LogError("InterrogationManager: No OpenRouterLLM assigned!");
                    return;
                }

                if (_injectEmotionIntoPrompt)
                {
                    _openRouterLLM.SetSystemPromptSuffix(_emotionSystemSuffix);
                }

                _isReady = true;

                _openRouterLLM.SendMessage(
                    "[The suspect has just sat down in the interrogation room. Begin your questioning.]",
                    OnOpenRouterResponse);
            }
        }

        public void SubmitPlayerResponse(string playerMessage)
        {
            if (_isWaitingForResponse || !_isReady || string.IsNullOrEmpty(playerMessage)) return;

            _isWaitingForResponse = true;
            _messageCount++;

            // Start the coroutine that waits for all analysis to finish
            StartCoroutine(GatherAnalysisAndSend(playerMessage));
        }

        private IEnumerator GatherAnalysisAndSend(string playerMessage)
        {
            _statusText = "Analyzing...";

            bool doVision = _useVisionAnalysis && _visionAnalyzer != null &&
                           (_messageCount % _visionAnalysisFrequency == 0);

            bool visionDone = !doVision;
            bool voiceDone = (_voiceAnalyzer == null);
            string visionResult = _lastVisionDescription;

            // Kick off vision analysis if needed
            if (doVision)
            {
                _statusText = "Analyzing appearance...";
                _visionAnalyzer.AnalyzeCurrentFrame(desc =>
                {
                    visionResult = desc;
                    _lastVisionDescription = desc;
                    visionDone = true;
                });
            }

            // Wait for voice analyzer if it's currently processing
            if (_voiceAnalyzer != null && _voiceAnalyzer.IsAnalyzing)
            {
                _statusText = "Analyzing voice...";
            }

            // Wait for everything with a timeout
            float timer = 0f;
            while ((!visionDone || (_voiceAnalyzer != null && _voiceAnalyzer.IsAnalyzing)) && timer < _analysisTimeout)
            {
                timer += Time.deltaTime;

                if (!visionDone && !voiceDone)
                    _statusText = "Analyzing voice & appearance...";
                else if (!visionDone)
                    _statusText = "Analyzing appearance...";
                else if (_voiceAnalyzer != null && _voiceAnalyzer.IsAnalyzing)
                    _statusText = "Analyzing voice...";

                yield return null;
            }

            if (timer >= _analysisTimeout)
            {
                Debug.LogWarning("[InterrogationManager] Analysis timed out, sending with available data.");
            }

            _statusText = "Detective is thinking...";

            // Now build the full message with all context
            string faceEmotion = GetFaceEmotionContext();
            string voiceEmotion = GetVoiceEmotionContext();

            string messageWithContext = playerMessage;

            if (_injectEmotionIntoPrompt)
            {
                messageWithContext = playerMessage +
                    "\n[Player's facial expression: " + faceEmotion + "]" +
                    "\n[Player's voice tone: " + voiceEmotion + "]";

                if (!string.IsNullOrEmpty(_lastVisionDescription))
                {
                    messageWithContext += "\n[Visual observation: " + _lastVisionDescription + "]";
                }
            }

            _conversationLog.Add("[Player]: " + playerMessage);
            _conversationLog.Add("[Face: " + faceEmotion + " | Voice: " + voiceEmotion + "]");

            if (doVision)
            {
                _conversationLog.Add("[Vision: " + _lastVisionDescription + "]");
            }

            // Send to LLM
            if (_llmMode == LLMMode.Local)
            {
                SendToLocalLLM(messageWithContext);
            }
            else
            {
                _openRouterLLM.SendMessage(messageWithContext, OnOpenRouterResponse);
            }
        }

        private async void SendToLocalLLM(string message)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            var agent = _detectiveAgentLocal as LLMUnity.LLMAgent;
            _streamingText = "";

            string response = await agent.Chat(message, OnStreamingToken, OnResponseComplete, true);

            if (response != null)
            {
                _currentDetectiveText = response;
                _conversationLog.Add("[Detective]: " + response);
                SpeakText(response);
            }

            _isWaitingForResponse = false;
            _statusText = "";
#endif
        }

        private void OnOpenRouterResponse(string response)
        {
            _currentDetectiveText = response;
            _conversationLog.Add("[Detective]: " + response);
            _isWaitingForResponse = false;
            _statusText = "";
            SpeakText(response);
        }

        private void SpeakText(string text)
        {
            if (_piperTTS == null)
            {
                Debug.LogWarning("No PiperManager assigned, skipping TTS");
                return;
            }

            _isSpeaking = true;
            _piperTTS.SynthesizeAndPlay(text);
        }

        private void Update()
        {
            if (_isSpeaking && _piperTTS != null)
            {
                AudioSource audioSource = _piperTTS.GetComponent<AudioSource>();
                if (audioSource != null && !audioSource.isPlaying)
                {
                    _isSpeaking = false;
                }
            }
        }

        private void OnStreamingToken(string partialResponse)
        {
            _streamingText = partialResponse;
        }

        private void OnResponseComplete()
        {
        }

        private string GetFaceEmotionContext()
        {
            if (_emotionDetector == null) return "neutral (0.00)";
            return _emotionDetector.CurrentEmotion + " (" + _emotionDetector.CurrentConfidence.ToString("F2") + ")";
        }

        private string GetVoiceEmotionContext()
        {
            if (_voiceAnalyzer == null) return "not analyzed";
            if (_voiceAnalyzer.IsAnalyzing) return "analyzing...";
            return _voiceAnalyzer.LastVoiceEmotion + " (" + _voiceAnalyzer.LastVoiceConfidence + ")";
        }

        public void CancelResponse()
        {
            _isWaitingForResponse = false;
            _statusText = "";
        }
    }
}