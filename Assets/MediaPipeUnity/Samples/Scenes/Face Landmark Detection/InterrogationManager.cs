using System.Collections.Generic;
using UnityEngine;
using LLMUnity;

namespace Mediapipe.Unity.Sample.FaceLandmarkDetection
{
    public class InterrogationManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EmotionDetector _emotionDetector;

        [Tooltip("Drag the LLM Detective GameObject here (the one with LLMAgent)")]
        [SerializeField] private LLMAgent _detectiveAgent;

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
        private string _lastPlayerMessage = "";
        private bool _isWaitingForResponse = false;
        private bool _isReady = false;
        private string _streamingText = "";

        // Public getters for the UI
        public string CurrentDetectiveText => _currentDetectiveText;
        public bool IsWaitingForResponse => _isWaitingForResponse;
        public bool IsReady => _isReady;
        public List<string> ConversationLog => _conversationLog;
        public string StreamingText => _streamingText;

        private async void Start()
        {
            if (_detectiveAgent == null)
            {
                Debug.LogError("InterrogationManager: No LLMAgent assigned! Drag the LLM Detective GameObject here.");
                return;
            }

            // Append emotion awareness to the system prompt
            if (_injectEmotionIntoPrompt)
            {
                _detectiveAgent.systemPrompt += _emotionSystemSuffix;
            }

            // Wait for LLM to be ready, then warm up
            await _detectiveAgent.Warmup();
            _isReady = true;

            // Get the detective's opening line
            await SendToDetective("[The suspect has just sat down in the interrogation room. Begin your questioning.]");
        }

        public async void SubmitPlayerResponse(string playerMessage)
        {
            if (_isWaitingForResponse || !_isReady || string.IsNullOrEmpty(playerMessage)) return;

            _isWaitingForResponse = true;
            _lastPlayerMessage = playerMessage;

            // Build the message with emotion context
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

            await SendToDetective(messageWithEmotion);
        }

        private async System.Threading.Tasks.Task SendToDetective(string message)
        {
            _isWaitingForResponse = true;
            _streamingText = "";

            string response = await _detectiveAgent.Chat(
                message,
                OnStreamingToken,
                OnResponseComplete,
                true
            );

            if (response != null)
            {
                _currentDetectiveText = response;
                _conversationLog.Add("[Detective]: " + response);
            }

            _isWaitingForResponse = false;
        }

        private void OnStreamingToken(string partialResponse)
        {
            _streamingText = partialResponse;
        }

        private void OnResponseComplete()
        {
            // Called when the full response is done
        }

        private string GetEmotionContext()
        {
            if (_emotionDetector == null) return "neutral (0.00)";
            return _emotionDetector.CurrentEmotion + " (" + _emotionDetector.CurrentConfidence.ToString("F2") + ")";
        }

        public void CancelResponse()
        {
            if (_isWaitingForResponse)
            {
                _detectiveAgent.CancelRequests();
                _isWaitingForResponse = false;
            }
        }
    }
}