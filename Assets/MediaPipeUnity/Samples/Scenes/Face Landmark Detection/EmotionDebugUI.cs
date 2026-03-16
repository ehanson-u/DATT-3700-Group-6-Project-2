using UnityEngine;

namespace Mediapipe.Unity.Sample.FaceLandmarkDetection
{
    public class EmotionDebugUI : MonoBehaviour
    {
        [SerializeField] private EmotionDetector _emotionDetector;

        private GUIStyle _style;
        private GUIStyle _smallStyle;
        private GUIStyle _barStyle;

        private void OnGUI()
        {
            if (_emotionDetector == null) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label);
                _style.fontSize = 28;
                _style.fontStyle = FontStyle.Bold;
                _style.normal.textColor = UnityEngine.Color.white;

                _smallStyle = new GUIStyle(GUI.skin.label);
                _smallStyle.fontSize = 18;
                _smallStyle.normal.textColor = UnityEngine.Color.white;

                _barStyle = new GUIStyle(GUI.skin.box);
            }

            string emotion = _emotionDetector.CurrentEmotion;
            float confidence = _emotionDetector.CurrentConfidence;

            switch (emotion)
            {
                case "happy": _style.normal.textColor = UnityEngine.Color.green; break;
                case "surprised": _style.normal.textColor = UnityEngine.Color.yellow; break;
                case "angry": _style.normal.textColor = UnityEngine.Color.red; break;
                case "sad": _style.normal.textColor = UnityEngine.Color.cyan; break;
                case "nervous": _style.normal.textColor = new UnityEngine.Color(1f, 0.5f, 0f); break;
                default: _style.normal.textColor = UnityEngine.Color.white; break;
            }

            // Background
            GUI.Box(new UnityEngine.Rect(10, 10, 320, 230), "");

            // Main emotion display
            GUI.Label(new UnityEngine.Rect(20, 15, 300, 35), "Emotion: " + emotion.ToUpper(), _style);

            // All scores breakdown
            float y = 55;
            DrawScoreLine("Happy", _emotionDetector.HappyScore, UnityEngine.Color.green, ref y);
            DrawScoreLine("Surprised", _emotionDetector.SurprisedScore, UnityEngine.Color.yellow, ref y);
            DrawScoreLine("Angry", _emotionDetector.AngryScore, UnityEngine.Color.red, ref y);
            DrawScoreLine("Sad", _emotionDetector.SadScore, UnityEngine.Color.cyan, ref y);
            DrawScoreLine("Nervous", _emotionDetector.NervousScore, new UnityEngine.Color(1f, 0.5f, 0f), ref y);
        }

        private void DrawScoreLine(string label, float score, UnityEngine.Color color, ref float y)
        {
            _smallStyle.normal.textColor = color;
            GUI.Label(new UnityEngine.Rect(20, y, 100, 25), label, _smallStyle);

            // Draw a bar representing the score
            float barWidth = score * 150f;
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            GUI.DrawTexture(new UnityEngine.Rect(120, y + 4, barWidth, 14), tex);

            _smallStyle.normal.textColor = UnityEngine.Color.white;
            GUI.Label(new UnityEngine.Rect(280, y, 50, 25), score.ToString("F2"), _smallStyle);

            y += 28;
        }
    }
}