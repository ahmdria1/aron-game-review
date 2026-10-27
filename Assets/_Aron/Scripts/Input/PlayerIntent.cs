// (c) Ahmed Alamrani. All rights reserved.
using UnityEngine;

namespace Aron.Input
{
    /// <summary>One frame of high-level player intent, device-agnostic.</summary>
    public struct PlayerIntent
    {
        public Vector2 move;       // x = right, y = forward (camera relative), magnitude 0..1
        public bool jumpPressed;
        public bool jumpHeld;
        public bool fingerDown;    // a finger is on the screen, or jump is held on a keyboard
        public bool attackPressed;
        public bool slidePressed;
        public bool sprintHeld;
        public bool pausePressed;
        public bool hasAim;        // attack was aimed at something (a tapped enemy or crate)
        public Vector3 aimPoint;

        public void Merge(PlayerIntent o)
        {
            if (o.move.sqrMagnitude > move.sqrMagnitude) move = o.move;
            jumpPressed |= o.jumpPressed; jumpHeld |= o.jumpHeld; fingerDown |= o.fingerDown;
            attackPressed |= o.attackPressed; slidePressed |= o.slidePressed; pausePressed |= o.pausePressed;
            sprintHeld |= o.sprintHeld;
            if (o.hasAim && !hasAim) { hasAim = true; aimPoint = o.aimPoint; }
        }
    }

    public interface IIntentSource
    {
        /// <summary>Called once per frame by <see cref="InputRouter"/>.</summary>
        PlayerIntent Poll();
    }
}
