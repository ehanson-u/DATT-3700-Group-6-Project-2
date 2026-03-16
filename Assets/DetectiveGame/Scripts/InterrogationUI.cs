using UnityEngine;
using System.Collections.Generic;
using Mediapipe.Unity.Sample.FaceLandmarkDetection;

namespace DetectiveGame
{
    public class InterrogationUI : MonoBehaviour
    {
        [SerializeField] private InterrogationManager _interrogationManager;
        [SerializeField] private EmotionDetector _emotionDetector;

        private string _playerInput = "";
        private Vector2 _scrollPosition;
        private GUIStyle _detectiveStyle;
        private GUIStyle _playerStyle;
        private GUIStyle _systemStyle;
        private GUIStyle _inputStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _emotionBarSmall;
        private GUIStyle _streamingStyle;
        private bool _stylesInitialized = false;

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _detectiveStyle = new GUIStyle(GUI.skin.label);
            _detectiveStyle.fontSize = 16;
            _detectiveStyle.wordWrap = true;
            _detectiveStyle.normal.textColor = new UnityEngine.Color(1f, 0.85f, 0.4f);
            _detectiveStyle.padding = new RectOffset(10, 10, 5, 5);

            _playerStyle = new GUIStyle(GUI.skin.label);
            _playerStyle.fontSize = 16;
            _playerStyle.wordWrap = true;
            _playerStyle.normal.textColor = new UnityEngine.Color(0.7f, 0.9f, 1f);
            _playerStyle.padding = new RectOffset(10, 10, 5, 5);

            _systemStyle = new GUIStyle(GUI.skin.label);
            _systemStyle.fontSize = 12;
            _systemStyle.wordWrap = true;
            _systemStyle.normal.textColor = new UnityEngine.Color(0.5f, 0.5f, 0.5f);
            _systemStyle.fontStyle = FontStyle.Italic;
            _systemStyle.padding = new RectOffset(10, 10, 2, 2);

            _inputStyle = new GUIStyle(GUI.skin.textField);
            _inputStyle.fontSize = 16;
            _inputStyle.padding = new RectOffset(10, 10, 8, 8);

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fontSize = 16;
            _buttonStyle.fontStyle = FontStyle.Bold;

            _headerStyle = new GUIStyle(GUI.skin.label);
            _headerStyle.fontSize = 20;
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.normal.textColor = UnityEngine.Color.white;
            _headerStyle.alignment = TextAnchor.MiddleCenter;

            _emotionBarSmall = new GUIStyle(GUI.skin.label);
            _emotionBarSmall.fontSize = 14;
            _emotionBarSmall.normal.textColor = UnityEngine.Color.white;

            _streamingStyle = new GUIStyle(GUI.skin.label);
            _streamingStyle.fontSize = 16;
            _streamingStyle.wordWrap = true;
            _streamingStyle.normal.textColor = new UnityEngine.Color(1f, 0.85f, 0.4f, 0.7f);
            _streamingStyle.padding = new RectOffset(10, 10, 5, 5);

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (_interrogationManager == null) return;

            InitStyles();

            float screenW = Screen.width;
            float screenH = Screen.height;

            // Right panel - interrogation dialogue
            float panelWidth = 520;
            float panelX = screenW - panelWidth - 10;
            float panelY = 10;
            float panelHeight = screenH - 20;

            GUI.Box(new UnityEngine.Rect(panelX, panelY, panelWidth, panelHeight), "");

            // Header
            string headerText = _interrogationManager.IsReady ? "INTERROGATION ROOM" : "LOADING MODEL...";
            GUI.Label(new UnityEngine.Rect(panelX, panelY + 5, panelWidth, 30), headerText, _headerStyle);

            // Conversation scroll area
            float convoTop = panelY + 40;
            float convoHeight = panelHeight - 130;
            List<string> history = _interrogationManager.ConversationLog;

            // Calculate total content height
            float contentHeight = 0;
            foreach (string line in history)
            {
                GUIStyle lineStyle = GetStyleForLine(line);
                float lineHeight = lineStyle.CalcHeight(new GUIContent(line), panelWidth - 40);
                contentHeight += lineHeight + 5;
            }

            // Add streaming text height if currently generating
            if (_interrogationManager.IsWaitingForResponse && !string.IsNullOrEmpty(_interrogationManager.StreamingText))
            {
                string streamLabel = "[Detective]: " + _interrogationManager.StreamingText;
                float streamHeight = _streamingStyle.CalcHeight(new GUIContent(streamLabel), panelWidth - 40);
                contentHeight += streamHeight + 5;
            }

            _scrollPosition = GUI.BeginScrollView(
                new UnityEngine.Rect(panelX + 5, convoTop, panelWidth - 10, convoHeight),
                _scrollPosition,
                new UnityEngine.Rect(0, 0, panelWidth - 40, contentHeight + 20)
            );

            float yPos = 5;
            foreach (string line in history)
            {
                GUIStyle lineStyle = GetStyleForLine(line);
                float lineHeight = lineStyle.CalcHeight(new GUIContent(line), panelWidth - 40);
                GUI.Label(new UnityEngine.Rect(5, yPos, panelWidth - 40, lineHeight), line, lineStyle);
                yPos += lineHeight + 5;
            }

            // Show streaming response as it comes in
            if (_interrogationManager.IsWaitingForResponse && !string.IsNullOrEmpty(_interrogationManager.StreamingText))
            {
                string streamLabel = "[Detective]: " + _interrogationManager.StreamingText + " _";
                float streamHeight = _streamingStyle.CalcHeight(new GUIContent(streamLabel), panelWidth - 40);
                GUI.Label(new UnityEngine.Rect(5, yPos, panelWidth - 40, streamHeight), streamLabel, _streamingStyle);
                yPos += streamHeight + 5;
            }

            GUI.EndScrollView();

            // Auto-scroll to bottom
            if (contentHeight > convoHeight)
            {
                _scrollPosition.y = contentHeight - convoHeight + 50;
            }

            // Status text
            if (_interrogationManager.IsWaitingForResponse)
            {
                GUI.Label(
                    new UnityEngine.Rect(panelX + 10, panelY + panelHeight - 85, panelWidth - 20, 25),
                    "Detective is speaking...",
                    _systemStyle
                );
            }
            else if (!_interrogationManager.IsReady)
            {
                GUI.Label(
                    new UnityEngine.Rect(panelX + 10, panelY + panelHeight - 85, panelWidth - 20, 25),
                    "Loading model, please wait...",
                    _systemStyle
                );
            }

            // Input field and send button (text input for now, voice later)
            float inputY = panelY + panelHeight - 55;
            bool canType = !_interrogationManager.IsWaitingForResponse && _interrogationManager.IsReady;

            GUI.enabled = canType;
            _playerInput = GUI.TextField(
                new UnityEngine.Rect(panelX + 10, inputY, panelWidth - 100, 40),
                _playerInput,
                _inputStyle
            );

            if (GUI.Button(new UnityEngine.Rect(panelX + panelWidth - 80, inputY, 70, 40), "Send", _buttonStyle))
            {
                if (!string.IsNullOrEmpty(_playerInput))
                {
                    _interrogationManager.SubmitPlayerResponse(_playerInput);
                    _playerInput = "";
                    GUI.FocusControl(null);
                }
            }

            if (canType && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                if (!string.IsNullOrEmpty(_playerInput))
                {
                    _interrogationManager.SubmitPlayerResponse(_playerInput);
                    _playerInput = "";
                    GUI.FocusControl(null);
                    Event.current.Use();
                }
            }
            GUI.enabled = true;

            // Left side - small emotion readout
            if (_emotionDetector != null)
            {
                GUI.Box(new UnityEngine.Rect(10, 10, 200, 35), "");
                string emo = _emotionDetector.CurrentEmotion.ToUpper();
                float conf = _emotionDetector.CurrentConfidence;
                _emotionBarSmall.normal.textColor = GetEmotionColor(emo.ToLower());
                GUI.Label(new UnityEngine.Rect(15, 14, 190, 25), emo + " (" + conf.ToString("F2") + ")", _emotionBarSmall);
            }
        }

        private GUIStyle GetStyleForLine(string line)
        {
            if (line.StartsWith("[Detective]")) return _detectiveStyle;
            if (line.StartsWith("[Player]")) return _playerStyle;
            return _systemStyle;
        }

        private UnityEngine.Color GetEmotionColor(string emotion)
        {
            switch (emotion)
            {
                case "happy": return UnityEngine.Color.green;
                case "surprised": return UnityEngine.Color.yellow;
                case "angry": return UnityEngine.Color.red;
                case "sad": return UnityEngine.Color.cyan;
                case "nervous": return new UnityEngine.Color(1f, 0.5f, 0f);
                default: return UnityEngine.Color.white;
            }
        }
    }
}