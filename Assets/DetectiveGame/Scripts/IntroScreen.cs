using UnityEngine;

namespace DetectiveGame
{
    public class IntroScreen : MonoBehaviour
    {
        private bool _showIntro = true;
        private int _currentPage = 0;

        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _techStyle;
        private GUIStyle _tipStyle;
        private GUIStyle _storyStyle;
        private bool _stylesInit = false;

        public bool IsShowingIntro()
        {
            return _showIntro;
        }

        private void InitStyles()
        {
            if (_stylesInit) return;

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.fontSize = 36;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.normal.textColor = new UnityEngine.Color(1f, 0.85f, 0.4f);
            _titleStyle.alignment = TextAnchor.MiddleCenter;
            _titleStyle.wordWrap = true;

            _subtitleStyle = new GUIStyle(GUI.skin.label);
            _subtitleStyle.fontSize = 18;
            _subtitleStyle.fontStyle = FontStyle.Italic;
            _subtitleStyle.normal.textColor = new UnityEngine.Color(0.8f, 0.8f, 0.8f);
            _subtitleStyle.alignment = TextAnchor.MiddleCenter;
            _subtitleStyle.wordWrap = true;

            _bodyStyle = new GUIStyle(GUI.skin.label);
            _bodyStyle.fontSize = 16;
            _bodyStyle.wordWrap = true;
            _bodyStyle.normal.textColor = UnityEngine.Color.white;
            _bodyStyle.alignment = TextAnchor.UpperCenter;
            _bodyStyle.padding = new RectOffset(30, 30, 5, 5);

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fontSize = 20;
            _buttonStyle.fontStyle = FontStyle.Bold;
            _buttonStyle.padding = new RectOffset(20, 20, 10, 10);

            _techStyle = new GUIStyle(GUI.skin.label);
            _techStyle.fontSize = 14;
            _techStyle.wordWrap = true;
            _techStyle.normal.textColor = new UnityEngine.Color(0.6f, 0.8f, 1f);
            _techStyle.padding = new RectOffset(30, 30, 3, 3);

            _tipStyle = new GUIStyle(GUI.skin.label);
            _tipStyle.fontSize = 15;
            _tipStyle.wordWrap = true;
            _tipStyle.normal.textColor = new UnityEngine.Color(0.5f, 1f, 0.5f);
            _tipStyle.padding = new RectOffset(30, 30, 3, 3);

            _storyStyle = new GUIStyle(GUI.skin.label);
            _storyStyle.fontSize = 15;
            _storyStyle.wordWrap = true;
            _storyStyle.normal.textColor = new UnityEngine.Color(0.9f, 0.9f, 0.85f);
            _storyStyle.alignment = TextAnchor.UpperLeft;
            _storyStyle.padding = new RectOffset(35, 35, 3, 3);

            _stylesInit = true;
        }

        private void OnGUI()
        {
            if (!_showIntro) return;

            InitStyles();

            float sw = Screen.width;
            float sh = Screen.height;

            Texture2D black = new Texture2D(1, 1);
            black.SetPixel(0, 0, new UnityEngine.Color(0, 0, 0, 0.95f));
            black.Apply();
            GUI.DrawTexture(new UnityEngine.Rect(0, 0, sw, sh), black);

            float pw = Mathf.Min(sw * 0.7f, 800);
            float ph = Mathf.Min(sh * 0.85f, 680);
            float px = (sw - pw) / 2;
            float py = (sh - ph) / 2;

            GUI.Box(new UnityEngine.Rect(px, py, pw, ph), "");

            switch (_currentPage)
            {
                case 0: DrawPage0(px, py, pw, ph); break;
                case 1: DrawPage1(px, py, pw, ph); break;
                case 2: DrawPage2(px, py, pw, ph); break;
                case 3: DrawPage3(px, py, pw, ph); break;
                case 4: DrawPage4(px, py, pw, ph); break;
            }
        }

        // PAGE 0: Title
        private void DrawPage0(float x, float y, float w, float h)
        {
            float cy = y + 40;

            GUI.Label(new UnityEngine.Rect(x, cy, w, 45), "THE MERIDIAN CASE", _titleStyle);
            cy += 50;

            GUI.Label(new UnityEngine.Rect(x, cy, w, 30), "An AI-Powered Detective Interrogation", _subtitleStyle);
            cy += 60;

            GUI.Label(new UnityEngine.Rect(x, cy, w, 0),
                "You are a suspect in an attempted poisoning at a high-end restaurant.", _bodyStyle);
            cy += 40;

            GUI.Label(new UnityEngine.Rect(x, cy, w, 0),
                "Detective Rafael Moreno will question you. He can see your face,\nhear your voice, and read your body language in real time.", _bodyStyle);
            cy += 55;

            GUI.Label(new UnityEngine.Rect(x, cy, w, 0),
                "How you look, how you sound, and what you say all matter.", _bodyStyle);
            cy += 40;

            _bodyStyle.normal.textColor = new UnityEngine.Color(1f, 0.7f, 0.3f);
            GUI.Label(new UnityEngine.Rect(x, cy, w, 0),
                "Can you convince the detective you're innocent?", _bodyStyle);
            _bodyStyle.normal.textColor = UnityEngine.Color.white;

            if (GUI.Button(new UnityEngine.Rect(x + w / 2 - 70, y + h - 65, 140, 45), "Next", _buttonStyle))
            {
                _currentPage = 1;
            }
        }

        // PAGE 1: The Story
        private void DrawPage1(float x, float y, float w, float h)
        {
            float cy = y + 20;

            GUI.Label(new UnityEngine.Rect(x, cy, w, 40), "THE CASE", _titleStyle);
            cy += 50;

            // Crime details
            _storyStyle.fontStyle = FontStyle.Bold;
            _storyStyle.normal.textColor = new UnityEngine.Color(1f, 0.85f, 0.4f);
            GUI.Label(new UnityEngine.Rect(x, cy, w, 22), "INCIDENT REPORT", _storyStyle);
            cy += 25;

            _storyStyle.fontStyle = FontStyle.Normal;
            _storyStyle.normal.textColor = new UnityEngine.Color(0.9f, 0.9f, 0.85f);
            GUI.Label(new UnityEngine.Rect(x, cy, w, 60),
                "Last Tuesday evening, Victor Ashford — a wealthy businessman and regular patron of The Meridian, a high-end restaurant — was poisoned during dinner. He was rushed to the hospital in critical condition. Toxicology confirmed a fast-acting poison was placed in his main course.", _storyStyle);
            cy += 70;

            // Timeline
            _storyStyle.fontStyle = FontStyle.Bold;
            _storyStyle.normal.textColor = new UnityEngine.Color(1f, 0.85f, 0.4f);
            GUI.Label(new UnityEngine.Rect(x, cy, w, 22), "TIMELINE", _storyStyle);
            cy += 25;

            _storyStyle.fontStyle = FontStyle.Normal;
            _storyStyle.normal.textColor = new UnityEngine.Color(0.75f, 0.75f, 0.75f);
            GUI.Label(new UnityEngine.Rect(x, cy, w, 20), "6:00 PM  —  Victor Ashford and guests arrive", _storyStyle);
            cy += 20;
            GUI.Label(new UnityEngine.Rect(x, cy, w, 20), "6:15 PM  —  Party is seated at their reserved table", _storyStyle);
            cy += 20;
            GUI.Label(new UnityEngine.Rect(x, cy, w, 20), "6:20 PM  —  Server takes their order", _storyStyle);
            cy += 20;
            GUI.Label(new UnityEngine.Rect(x, cy, w, 20), "7:05 PM  —  Food is served", _storyStyle);
            cy += 20;
            GUI.Label(new UnityEngine.Rect(x, cy, w, 20), "7:25 PM  —  Victor shows signs of distress", _storyStyle);
            cy += 20;
            GUI.Label(new UnityEngine.Rect(x, cy, w, 20), "7:30 PM  —  Emergency services called", _storyStyle);
            cy += 20;
            GUI.Label(new UnityEngine.Rect(x, cy, w, 20), "7:45 PM  —  Restaurant sealed, all present held for questioning", _storyStyle);
            cy += 30;

            // Suspects
            _storyStyle.fontStyle = FontStyle.Bold;
            _storyStyle.normal.textColor = new UnityEngine.Color(1f, 0.85f, 0.4f);
            GUI.Label(new UnityEngine.Rect(x, cy, w, 22), "PERSONS OF INTEREST", _storyStyle);
            cy += 25;

            _storyStyle.fontStyle = FontStyle.Normal;
            _storyStyle.normal.textColor = new UnityEngine.Color(0.9f, 0.9f, 0.85f);
            GUI.Label(new UnityEngine.Rect(x, cy, w, 20), "The Server  —  Last person to handle the food", _storyStyle);
            cy += 20;
            GUI.Label(new UnityEngine.Rect(x, cy, w, 20), "The Chef  —  Prepared the meal, has a violent past", _storyStyle);
            cy += 20;
            GUI.Label(new UnityEngine.Rect(x, cy, w, 20), "The Sibling  —  Stands to inherit Victor's fortune", _storyStyle);
            cy += 20;
            GUI.Label(new UnityEngine.Rect(x, cy, w, 20), "The Date  —  Recently discovered Victor was cheating", _storyStyle);
            cy += 20;
            GUI.Label(new UnityEngine.Rect(x, cy, w, 20), "The Coworker  —  Was alone at the table, seen reaching toward the plate", _storyStyle);
            cy += 30;

            _storyStyle.normal.textColor = new UnityEngine.Color(1f, 0.5f, 0.5f);
            _storyStyle.fontStyle = FontStyle.Italic;
            GUI.Label(new UnityEngine.Rect(x, cy, w, 22), "You are one of these suspects. The detective will decide which.", _storyStyle);
            _storyStyle.fontStyle = FontStyle.Normal;

            DrawNavButtons(x, y, w, h, 0, 2);
        }

        // PAGE 2: How it works
        private void DrawPage2(float x, float y, float w, float h)
        {
            float cy = y + 25;

            GUI.Label(new UnityEngine.Rect(x, cy, w, 40), "HOW IT WORKS", _titleStyle);
            cy += 55;

            DrawBullet(x, ref cy, w, "WEBCAM",
                "The system tracks your facial expressions in real time using MediaPipe face landmarks and AI vision analysis.");
            cy += 10;

            DrawBullet(x, ref cy, w, "MICROPHONE",
                "Your voice tone is analyzed for emotion. The detective knows if you sound nervous, angry, calm, or evasive.");
            cy += 10;

            DrawBullet(x, ref cy, w, "APPEARANCE",
                "The AI can see what you look like, what you're wearing, and what's around you. It may comment on details.");
            cy += 10;

            DrawBullet(x, ref cy, w, "AI DETECTIVE",
                "All of this data feeds into a large language model that roleplays as the detective. Every response is unique.");

            DrawNavButtons(x, y, w, h, 1, 3);
        }

        // PAGE 3: How to play
        private void DrawPage3(float x, float y, float w, float h)
        {
            float cy = y + 25;

            GUI.Label(new UnityEngine.Rect(x, cy, w, 40), "HOW TO PLAY", _titleStyle);
            cy += 60;

            GUI.Label(new UnityEngine.Rect(x, cy, w, 25),
                "SPEAK  —  Hold SPACEBAR and talk into your mic. Release to send.", _tipStyle);
            cy += 35;

            GUI.Label(new UnityEngine.Rect(x, cy, w, 25),
                "TYPE  —  You can also type your response and click Send or press Enter.", _tipStyle);
            cy += 35;

            GUI.Label(new UnityEngine.Rect(x, cy, w, 25),
                "LOOK AT THE CAMERA  —  The detective is watching. Expressions matter.", _tipStyle);
            cy += 35;

            GUI.Label(new UnityEngine.Rect(x, cy, w, 25),
                "BE CREATIVE  —  Lie, tell the truth, get angry, or stay silent.", _tipStyle);
            cy += 35;

            GUI.Label(new UnityEngine.Rect(x, cy, w, 25),
                "STAY IN CHARACTER  —  You are a suspect. Play however you want.", _tipStyle);
            cy += 50;

            _bodyStyle.normal.textColor = new UnityEngine.Color(1f, 0.7f, 0.3f);
            _bodyStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new UnityEngine.Rect(x + 20, cy, w - 40, 50),
                "TIP: Try different emotions! Smile when you shouldn't, look away\nwhen answering, or act angry. See how the detective reacts.", _bodyStyle);
            _bodyStyle.normal.textColor = UnityEngine.Color.white;
            _bodyStyle.alignment = TextAnchor.UpperCenter;

            DrawNavButtons(x, y, w, h, 2, 4);
        }

        // PAGE 4: Tech stack + start
        private void DrawPage4(float x, float y, float w, float h)
        {
            float cy = y + 25;

            GUI.Label(new UnityEngine.Rect(x, cy, w, 40), "TECHNOLOGY", _titleStyle);
            cy += 55;

            GUI.Label(new UnityEngine.Rect(x, cy, w, 22), "Face Tracking  —  MediaPipe FaceLandmarker (52 ARKit blendshapes)", _techStyle);
            cy += 25;
            GUI.Label(new UnityEngine.Rect(x, cy, w, 22), "Emotion Detection  —  Real-time blendshape classification", _techStyle);
            cy += 25;
            GUI.Label(new UnityEngine.Rect(x, cy, w, 22), "Vision Analysis  —  Multimodal LLM via OpenRouter", _techStyle);
            cy += 25;
            GUI.Label(new UnityEngine.Rect(x, cy, w, 22), "Voice Emotion  —  Hume AI prosody analysis", _techStyle);
            cy += 25;
            GUI.Label(new UnityEngine.Rect(x, cy, w, 22), "Speech-to-Text  —  Windows Dictation Recognizer", _techStyle);
            cy += 25;
            GUI.Label(new UnityEngine.Rect(x, cy, w, 22), "Text-to-Speech  —  ElevenLabs neural voice synthesis", _techStyle);
            cy += 25;
            GUI.Label(new UnityEngine.Rect(x, cy, w, 22), "LLM  —  OpenRouter API / Local Gemma 2 9B via LLMUnity", _techStyle);
            cy += 25;
            GUI.Label(new UnityEngine.Rect(x, cy, w, 22), "Game Engine  —  Unity 6", _techStyle);
            cy += 40;

            _subtitleStyle.fontSize = 15;
            _subtitleStyle.normal.textColor = new UnityEngine.Color(0.6f, 0.6f, 0.6f);
            GUI.Label(new UnityEngine.Rect(x, cy, w, 22), "DATT-3700  —  Group 6  —  York University", _subtitleStyle);
            _subtitleStyle.fontSize = 18;
            _subtitleStyle.normal.textColor = new UnityEngine.Color(0.8f, 0.8f, 0.8f);

            if (GUI.Button(new UnityEngine.Rect(x + 25, y + h - 65, 110, 45), "Back", _buttonStyle))
            {
                _currentPage = 3;
            }

            _buttonStyle.normal.textColor = new UnityEngine.Color(0.3f, 1f, 0.3f);
            if (GUI.Button(new UnityEngine.Rect(x + w / 2 - 90, y + h - 65, 180, 45), "BEGIN", _buttonStyle))
            {
                _showIntro = false;
            }
            _buttonStyle.normal.textColor = UnityEngine.Color.white;
        }

        private void DrawBullet(float x, ref float cy, float w, string title, string body)
        {
            _bodyStyle.fontStyle = FontStyle.Bold;
            _bodyStyle.normal.textColor = new UnityEngine.Color(1f, 0.85f, 0.4f);
            _bodyStyle.alignment = TextAnchor.UpperLeft;
            GUI.Label(new UnityEngine.Rect(x, cy, w, 22), title, _bodyStyle);
            cy += 22;

            _bodyStyle.fontStyle = FontStyle.Normal;
            _bodyStyle.normal.textColor = UnityEngine.Color.white;
            float bodyH = _bodyStyle.CalcHeight(new GUIContent(body), w - 60);
            GUI.Label(new UnityEngine.Rect(x, cy, w, bodyH), body, _bodyStyle);
            cy += bodyH + 5;

            _bodyStyle.alignment = TextAnchor.UpperCenter;
        }

        private void DrawNavButtons(float x, float y, float w, float h, int backPage, int nextPage)
        {
            if (GUI.Button(new UnityEngine.Rect(x + 25, y + h - 65, 110, 45), "Back", _buttonStyle))
            {
                _currentPage = backPage;
            }
            if (GUI.Button(new UnityEngine.Rect(x + w - 135, y + h - 65, 110, 45), "Next", _buttonStyle))
            {
                _currentPage = nextPage;
            }
        }
    }
}