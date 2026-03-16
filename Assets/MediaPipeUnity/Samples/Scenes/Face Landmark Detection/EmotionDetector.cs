using UnityEngine;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using Mediapipe.Tasks.Components.Containers;
using System.Collections.Generic;

namespace Mediapipe.Unity.Sample.FaceLandmarkDetection
{
    public class EmotionDetector : MonoBehaviour
    {
        public string CurrentEmotion { get; private set; } = "neutral";
        public float CurrentConfidence { get; private set; } = 0f;

        // Expose all scores so you can see them in the debug UI
        public float HappyScore { get; private set; }
        public float SurprisedScore { get; private set; }
        public float AngryScore { get; private set; }
        public float SadScore { get; private set; }
        public float NervousScore { get; private set; }

        public void ProcessResult(FaceLandmarkerResult result)
        {
            if (result.faceBlendshapes == null || result.faceBlendshapes.Count == 0)
            {
                CurrentEmotion = "no_face";
                CurrentConfidence = 0f;
                return;
            }

            var blendshapes = result.faceBlendshapes[0];

            var bs = new Dictionary<string, float>();
            foreach (var category in blendshapes.categories)
            {
                bs[category.categoryName] = category.score;
            }

            ClassifyEmotion(bs);
        }

        private void ClassifyEmotion(Dictionary<string, float> bs)
        {
            float Get(string name) => bs.ContainsKey(name) ? bs[name] : 0f;

            // HAPPY — smiling, cheeks raised
            HappyScore = (Get("mouthSmileLeft") + Get("mouthSmileRight")) / 2f
                       + (Get("cheekSquintLeft") + Get("cheekSquintRight")) / 4f;

            // SURPRISED — brows up, eyes wide, jaw open
            SurprisedScore = (Get("browInnerUp") + Get("browOuterUpLeft") + Get("browOuterUpRight")) / 3f
                           + (Get("jawOpen") * 0.5f)
                           + (Get("eyeWideLeft") + Get("eyeWideRight")) / 4f;

            // ANGRY — brows down, nose scrunch, tight mouth, jaw clench
            // Boosted: noseSneer is a very strong anger signal, and we weight browDown more heavily
            AngryScore = (Get("browDownLeft") + Get("browDownRight")) * 0.8f
                       + (Get("noseSneerLeft") + Get("noseSneerRight")) * 0.6f
                       + (Get("mouthFrownLeft") + Get("mouthFrownRight")) / 4f
                       + Get("jawForward") * 0.3f
                       + (Get("eyeSquintLeft") + Get("eyeSquintRight")) * 0.2f
                       + Get("mouthShrugLower") * 0.2f;

            // SAD — frown, inner brow raise, mouth droop
            SadScore = (Get("mouthFrownLeft") + Get("mouthFrownRight")) / 2f
                     + Get("browInnerUp") * 0.4f
                     + Get("mouthPucker") * 0.2f
                     + (Get("mouthLowerDownLeft") + Get("mouthLowerDownRight")) * 0.15f;

            // NERVOUS — lip press/tension, slight squint, mouth stretch, lip movement
            // Nervous is about facial tension rather than big movements
            NervousScore = (Get("mouthPressLeft") + Get("mouthPressRight")) * 0.6f
                         + (Get("eyeSquintLeft") + Get("eyeSquintRight")) * 0.3f
                         + (Get("mouthStretchLeft") + Get("mouthStretchRight")) * 0.3f
                         + (Get("mouthRollLower") + Get("mouthRollUpper")) * 0.4f
                         + Get("browInnerUp") * 0.25f
                         + (Get("lipsSuctionLower") + Get("lipsSuctionUpper")) * 0.3f
                         + Get("mouthShrugUpper") * 0.2f;

            // Lower neutral threshold so emotions trigger more easily
            float neutral = 0.12f;

            string bestEmotion = "neutral";
            float bestScore = neutral;

            if (HappyScore > bestScore) { bestEmotion = "happy"; bestScore = HappyScore; }
            if (SurprisedScore > bestScore) { bestEmotion = "surprised"; bestScore = SurprisedScore; }
            if (AngryScore > bestScore) { bestEmotion = "angry"; bestScore = AngryScore; }
            if (SadScore > bestScore) { bestEmotion = "sad"; bestScore = SadScore; }
            if (NervousScore > bestScore) { bestEmotion = "nervous"; bestScore = NervousScore; }

            CurrentEmotion = bestEmotion;
            CurrentConfidence = bestScore;
        }
    }
}