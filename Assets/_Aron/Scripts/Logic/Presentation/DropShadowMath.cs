// (c) Ahmed Alamrani. All rights reserved.
using System;
using Aron.Logic.GameFeel;

namespace Aron.Logic.Presentation
{
    /// <summary>What the contact shadow under one character looks like this frame.</summary>
    public struct ShadowLook
    {
        public bool Visible;
        /// <summary>Multiplier on the character's own shadow radius (1 = full size).</summary>
        public float Scale;
        /// <summary>0 to 1.</summary>
        public float Alpha;

        public static readonly ShadowLook Hidden = new ShadowLook { Visible = false, Scale = 0f, Alpha = 0f };
    }

    /// <summary>
    /// The size and opacity of the contact shadow against the height above the ground. Pure maths, tested outside Unity; the component
    /// (<c>Aron.Fx.DropShadow</c>) only does the one raycast down and applies the result. All numbers are in <see cref="FeelValues"/> (group Shadow).
    /// </summary>
    public static class DropShadowMath
    {
        /// <summary>At most this many characters get a shadow in one frame (the player and the boss first).</summary>
        public const int MaxCasters = 24;
        /// <summary>The ray starts this far above the feet so it clears the step under them.</summary>
        public const float RayLift = 0.15f;

        /// <summary>The height of the feet above the ground from the raycast distance (never below zero).</summary>
        public static float HeightFromHit(float hitDistance, float rayLift = RayLift) => Math.Max(0f, hitDistance - rayLift);

        /// <summary>
        /// The shadow for a character whose feet are <paramref name="height"/> metres above the ground.
        /// <paramref name="hasGround"/> is false when the ray found nothing within the maximum distance (over a pit), and then the shadow is hidden.
        /// <paramref name="realShadows"/> is true when the quality tier draws real shadows: standing on the ground the disc is weaker so the two never add up to too dark,
        /// and the disc comes back to full strength as the character rises (the real shadow then lies away from the feet, the disc stays under them).
        /// <paramref name="assist"/> makes the landing shadow bigger and stronger.
        /// </summary>
        public static ShadowLook Evaluate(FeelValues v, bool hasGround, float height, bool realShadows, bool assist)
        {
            if (!hasGround || float.IsNaN(height) || height > v.shadowMaxDistance || v.shadowOpacity <= 0f) return ShadowLook.Hidden;
            float h = Math.Max(0f, height);
            float t = Math.Min(1f, h / v.shadowMaxDistance);
            float alpha = v.shadowOpacity * (1f - (float)Math.Pow(t, v.shadowFadeCurve));
            float scale = v.shadowSize * (1f + (v.shadowMinScale - 1f) * t);
            if (realShadows)
            {
                float k = Math.Min(1f, h / Math.Max(0.01f, v.shadowRealBlendHeight));
                alpha *= v.shadowRealBlend + (1f - v.shadowRealBlend) * k;
            }
            if (assist) { alpha = Math.Min(0.9f, alpha * v.shadowAssistOpacityMult); scale *= v.shadowAssistSizeMult; }
            if (alpha <= 0.004f) return ShadowLook.Hidden;
            return new ShadowLook { Visible = true, Scale = scale, Alpha = alpha };
        }
    }
}
