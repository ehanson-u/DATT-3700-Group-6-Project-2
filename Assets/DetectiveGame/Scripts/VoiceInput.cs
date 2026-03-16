using UnityEngine;
using UnityEngine.Windows.Speech;

namespace DetectiveGame
{
    public class VoiceInput : MonoBehaviour
    {
        [SerializeField] private InterrogationManager _interrogationManager;

        [Header("Settings")]
        [SerializeField] private KeyCode pushToTalkKey = KeyCode.Space;

        private DictationRecognizer _dictationRecognizer;
        private bool _isListening = false;
        private string _currentTranscript = "";
        private bool _keyHeld = false;

        public bool IsListening => _isListening;
        public string CurrentTranscript => _currentTranscript;

        private void Start()
        {
            _dictationRecognizer = new DictationRecognizer();

            _dictationRecognizer.DictationResult += OnDictationResult;
            _dictationRecognizer.DictationHypothesis += OnDictationHypothesis;
            _dictationRecognizer.DictationComplete += OnDictationComplete;
            _dictationRecognizer.DictationError += OnDictationError;
        }

        private void Update()
        {
            if (Input.GetKeyDown(pushToTalkKey) && !_keyHeld)
            {
                _keyHeld = true;
                StartListening();
            }

            if (Input.GetKeyUp(pushToTalkKey) && _keyHeld)
            {
                _keyHeld = false;
                StopListening();
            }
        }

        private void StartListening()
        {
            if (_isListening) return;
            if (_interrogationManager != null && _interrogationManager.IsWaitingForResponse) return;

            _currentTranscript = "";
            _isListening = true;

            if (_dictationRecognizer.Status == SpeechSystemStatus.Stopped)
            {
                _dictationRecognizer.Start();
                Debug.Log("[VoiceInput] Listening...");
            }
        }

        private void StopListening()
        {
            if (!_isListening) return;

            _isListening = false;

            if (_dictationRecognizer.Status == SpeechSystemStatus.Running)
            {
                _dictationRecognizer.Stop();
                Debug.Log("[VoiceInput] Stopped listening.");
            }
        }

        private void OnDictationResult(string text, ConfidenceLevel confidence)
        {
            Debug.Log("[VoiceInput] Final result: " + text + " (confidence: " + confidence + ")");
            _currentTranscript = text;

            if (_interrogationManager != null && !string.IsNullOrEmpty(text))
            {
                _interrogationManager.SubmitPlayerResponse(text);
            }

            _currentTranscript = "";
        }

        private void OnDictationHypothesis(string text)
        {
            _currentTranscript = text;
        }

        private void OnDictationComplete(DictationCompletionCause cause)
        {
            Debug.Log("[VoiceInput] Dictation completed: " + cause);
            _isListening = false;
        }

        private void OnDictationError(string error, int hresult)
        {
            Debug.LogError("[VoiceInput] Error: " + error);
            _isListening = false;
        }

        private void OnDestroy()
        {
            if (_dictationRecognizer != null)
            {
                if (_dictationRecognizer.Status == SpeechSystemStatus.Running)
                {
                    _dictationRecognizer.Stop();
                }
                _dictationRecognizer.DictationResult -= OnDictationResult;
                _dictationRecognizer.DictationHypothesis -= OnDictationHypothesis;
                _dictationRecognizer.DictationComplete -= OnDictationComplete;
                _dictationRecognizer.DictationError -= OnDictationError;
                _dictationRecognizer.Dispose();
            }
        }
    }
}