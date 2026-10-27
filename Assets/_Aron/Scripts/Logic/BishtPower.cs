// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic
{
    public enum BishtPickup { Layer, Invincible, Topped }

    /// <summary>
    /// The golden bisht. Each one picked up guards against one hit, up to three. The third makes the hero
    /// untouchable for a while; when that runs out he keeps two.
    /// </summary>
    public class BishtPower
    {
        public const int MaxLayers = 3;
        public const float InvincibleSeconds = 10f;

        public int Layers { get; private set; }
        public float InvincibleLeft { get; private set; }
        public bool Invincible => InvincibleLeft > 0f;

        public BishtPickup Collect()
        {
            if (Invincible) { InvincibleLeft = InvincibleSeconds; return BishtPickup.Topped; }
            Layers = Math.Min(MaxLayers, Layers + 1);
            if (Layers < MaxLayers) return BishtPickup.Layer;
            InvincibleLeft = InvincibleSeconds;
            return BishtPickup.Invincible;
        }

        /// <summary>A hit lands. True when the bisht took it instead of the hero.</summary>
        public bool Absorb()
        {
            if (Invincible) return true;
            if (Layers <= 0) return false;
            Layers--;
            return true;
        }

        /// <returns>True on the frame the invincibility wears off.</returns>
        public bool Tick(float dt)
        {
            if (!Invincible) return false;
            InvincibleLeft = Math.Max(0f, InvincibleLeft - dt);
            if (Invincible) return false;
            Layers = MaxLayers - 1;
            return true;
        }

        /// <summary>Losing a life takes the bisht with it.</summary>
        public void Clear() { Layers = 0; InvincibleLeft = 0f; }
    }
}
