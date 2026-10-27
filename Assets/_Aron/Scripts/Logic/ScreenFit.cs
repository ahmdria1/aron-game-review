// (c) Ahmed Alamrani. All rights reserved.
namespace Aron.Logic
{
    /// <summary>Normalised anchors for the safe area, and how a canvas should scale for a given aspect.</summary>
    public static class ScreenFit
    {
        public const float ReferenceAspect = 16f / 9f;

        public struct Anchors { public float minX, minY, maxX, maxY; }

        public static Anchors SafeAnchors(float x, float y, float w, float h, float screenW, float screenH)
        {
            if (screenW <= 0f || screenH <= 0f || w <= 0f || h <= 0f)
                return new Anchors { maxX = 1f, maxY = 1f };
            return new Anchors
            {
                minX = Clamp01(x / screenW),
                minY = Clamp01(y / screenH),
                maxX = Clamp01((x + w) / screenW),
                maxY = Clamp01((y + h) / screenH),
            };
        }

        /// <summary>
        /// 0 = match width, 1 = match height. Wide phones (19.5:9, 21:9) keep the full UI height and gain
        /// room at the sides; squarer screens (iPad 4:3, foldables) keep the full width and gain room
        /// above and below, so nothing designed for 1920x1080 is ever cut off.
        /// </summary>
        public static float CanvasMatch(float screenW, float screenH)
        {
            if (screenW <= 0f || screenH <= 0f) return 0.5f;
            return screenW / screenH >= ReferenceAspect - 0.001f ? 1f : 0f;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
