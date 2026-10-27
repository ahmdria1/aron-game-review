// (c) Ahmed Alamrani. All rights reserved.
namespace Aron.Logic
{
    public enum Emotion
    {
        Neutral,
        Joy,
        Sadness,
        Anger,
        Fear,
        Admiration,
        Worry,
        Surprise,
        Embarrassment,
        Relief,
        Affection,
        Hunger,
    }

    /// <summary>
    /// Target shape of a cartoon face for one emotion. Distances are in "eye units" (half the gap between the eyes),
    /// angles in degrees. Positive brow tilt raises the inner ends (sad/worried), negative pulls them down (angry).
    /// </summary>
    public struct FacePose
    {
        public float eyeOpen;
        public float eyeSize;
        public float browRaise;
        public float browTilt;
        public float smile;
        public float mouthOpen;
        public float blush;
        public float tears;
        public float sweat;

        public static FacePose For(Emotion e)
        {
            switch (e)
            {
                case Emotion.Joy:
                    return new FacePose { eyeOpen = 0.35f, eyeSize = 1f, browRaise = 0.15f, browTilt = 0f, smile = 1f, mouthOpen = 0.55f, blush = 1f };
                case Emotion.Sadness:
                    return new FacePose { eyeOpen = 0.65f, eyeSize = 0.95f, browRaise = 0.05f, browTilt = 24f, smile = -0.9f, tears = 1f };
                case Emotion.Anger:
                    return new FacePose { eyeOpen = 0.7f, eyeSize = 0.95f, browRaise = -0.18f, browTilt = -30f, smile = -0.6f, mouthOpen = 0.2f };
                case Emotion.Fear:
                    return new FacePose { eyeOpen = 1.3f, eyeSize = 1.15f, browRaise = 0.3f, browTilt = 20f, smile = -0.35f, mouthOpen = 0.75f, sweat = 1f };
                case Emotion.Admiration:
                    return new FacePose { eyeOpen = 1.25f, eyeSize = 1.2f, browRaise = 0.32f, browTilt = 6f, smile = 0.35f, mouthOpen = 0.65f, blush = 0.6f };
                case Emotion.Worry:
                    return new FacePose { eyeOpen = 0.95f, eyeSize = 1f, browRaise = 0.12f, browTilt = 18f, smile = -0.45f, sweat = 1f };
                case Emotion.Surprise:
                    return new FacePose { eyeOpen = 1.35f, eyeSize = 1.2f, browRaise = 0.35f, mouthOpen = 0.8f };
                case Emotion.Embarrassment:
                    return new FacePose { eyeOpen = 0.6f, eyeSize = 0.95f, browRaise = 0.1f, browTilt = 12f, smile = 0.15f, blush = 1f };
                case Emotion.Relief:
                    return new FacePose { eyeOpen = 0.5f, eyeSize = 1f, browRaise = 0.05f, browTilt = 10f, smile = 0.5f, sweat = 1f };
                case Emotion.Affection:
                    return new FacePose { eyeOpen = 0.3f, eyeSize = 1f, browRaise = 0.1f, smile = 0.9f, blush = 0.8f };
                case Emotion.Hunger:
                    return new FacePose { eyeOpen = 1.15f, eyeSize = 1.15f, browRaise = 0.2f, smile = 0.6f, mouthOpen = 0.4f };
                default:
                    return new FacePose { eyeOpen = 1f, eyeSize = 1f, smile = 0.2f };
            }
        }

        public static FacePose Lerp(FacePose a, FacePose b, float t)
        {
            if (t < 0f) t = 0f; else if (t > 1f) t = 1f;
            return new FacePose
            {
                eyeOpen = a.eyeOpen + (b.eyeOpen - a.eyeOpen) * t,
                eyeSize = a.eyeSize + (b.eyeSize - a.eyeSize) * t,
                browRaise = a.browRaise + (b.browRaise - a.browRaise) * t,
                browTilt = a.browTilt + (b.browTilt - a.browTilt) * t,
                smile = a.smile + (b.smile - a.smile) * t,
                mouthOpen = a.mouthOpen + (b.mouthOpen - a.mouthOpen) * t,
                blush = a.blush + (b.blush - a.blush) * t,
                tears = a.tears + (b.tears - a.tears) * t,
                sweat = a.sweat + (b.sweat - a.sweat) * t,
            };
        }
    }

    public static class EmotionRules
    {
        /// <summary>A new emotion only interrupts an active one of equal or lower weight.</summary>
        public static int Weight(Emotion e)
        {
            switch (e)
            {
                case Emotion.Sadness: return 5;
                case Emotion.Joy: return 4;
                case Emotion.Anger: return 3;
                case Emotion.Fear: return 3;
                case Emotion.Worry: return 2;
                case Emotion.Admiration: return 2;
                case Emotion.Surprise: return 3;
                case Emotion.Embarrassment: return 2;
                case Emotion.Relief: return 2;
                case Emotion.Affection: return 1;
                case Emotion.Hunger: return 1;
                default: return 0;
            }
        }

        public static bool CanInterrupt(Emotion active, Emotion incoming) => Weight(incoming) >= Weight(active);
    }
}
