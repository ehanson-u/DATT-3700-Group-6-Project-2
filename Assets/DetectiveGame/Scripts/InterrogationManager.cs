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

        [Tooltip("For Local mode: drag the LLM Detective GameObject here")]
        [SerializeField] private MonoBehaviour _detectiveAgentLocal;

        [Tooltip("For OpenRouter mode: drag the OpenRouterLLM GameObject here")]
        [SerializeField] private OpenRouterLLM _openRouterLLM;

        [Tooltip("Drag the PiperManager GameObject here")]
        [SerializeField] private PiperManager _piperTTS;

        [Header("Settings")]
        [SerializeField] private bool _injectEmotionIntoPrompt = true;

        [TextArea(3, 6)]
        [SerializeField]
        private string _emotionSystemSuffix =
            "\n\nIMPORTANT: Each message from the player includes a note about their current facial expression. " +
            "Use this to inform your questioning. If they look nervous, press harder. If they look happy during serious questions, be suspicious. " +
            "If they look angry, note it. React naturally as a detective would to body language. " +
            "Do NOT explicitly say 'I can see you look nervous' - instead react naturally, like a real detective reading body language.";

        private List<string> _conversationLog = new List<string>();
        private string _currentDetectiveText = "";
        private bool _isWaitingForResponse = false;
        private bool _isReady = false;
        private string _streamingText = "";
        private bool _isSpeaking = false;

        public string CurrentDetectiveText => _currentDetectiveText;
        public bool IsWaitingForResponse => _isWaitingForResponse;
        public bool IsReady => _isReady;
        public List<string> ConversationLog => _conversationLog;
        public string StreamingText => _streamingText;
        public bool IsSpeaking => _isSpeaking;

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

            string emotionContext = GetEmotionContext();
            string messageWithEmotion;

            if (_injectEmotionIntoPrompt)
            {
                messageWithEmotion = playerMessage + "\n[Player's facial expression: " + emotionContext + "]";
            }
            else
            {
                messageWithEmotion = playerMessage;
            }

            _conversationLog.Add("[Player]: " + playerMessage);
            _conversationLog.Add("[Emotion: " + emotionContext + "]");

            if (_llmMode == LLMMode.Local)
            {
                SendToLocalLLM(messageWithEmotion);
            }
            else
            {
                _openRouterLLM.SendMessage(messageWithEmotion, OnOpenRouterResponse);
            }
        }

        // --- Local LLM ---
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
#endif
        }

        // --- OpenRouter ---
        private void OnOpenRouterResponse(string response)
        {
            _currentDetectiveText = response;
            _conversationLog.Add("[Detective]: " + response);
            _isWaitingForResponse = false;
            SpeakText(response);
        }

        // --- TTS ---
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

        private string GetEmotionContext()
        {
            if (_emotionDetector == null) return "neutral (0.00)";
            return _emotionDetector.CurrentEmotion + " (" + _emotionDetector.CurrentConfidence.ToString("F2") + ")";
        }

        public void CancelResponse()
        {
            _isWaitingForResponse = false;
        }
    }
}