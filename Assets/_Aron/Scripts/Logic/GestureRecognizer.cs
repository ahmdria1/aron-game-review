// (c) Ahmed Alamrani. All rights reserved.
using System;
using System.Collections.Generic;

namespace Aron.Logic
{
    public enum TouchState { Began, Moved, Ended }

    /// <summary>Result of one frame of touch input.</summary>
    public struct GestureFrame
    {
        public float moveX, moveY;
        public bool jump, jumpHeld, attack, slide, pause, sprint;
        /// <summary>Any finger is on the screen (Noura glides while one is).</summary>
        public bool holding;
        /// <summary>0 for a lazy flick, 1 for a hard one.</summary>
        public float jumpStrength;
        /// <summary>Screen position of the tap that caused <see cref="attack"/>.</summary>
        public float tapX, tapY;
        /// <summary>A finger is holding the character and steering him.</summary>
        public bool steering;
        /// <summary>The steering finger is pulled down, so he is braking.</summary>
        public bool braking;
        /// <summary>A tap was taken by something in the world (a shop door) instead of becoming an attack.</summary>
        public bool interact;
    }

    /// <summary>
    /// Engine-independent touch controls built around touching the character himself. Distances are in
    /// inches so it feels the same on every screen density.
    ///  - press on him          : nothing yet, he stays put while the finger is still
    ///  - drag left / right     : he sets off and heads that way until he is back under the finger
    ///  - drag down             : he slows down, all the way to a stop
    ///  - drag up               : jump, a faster swipe jumps higher (a second one in the air double jumps)
    ///  - hard flick down       : slide
    ///  - double tap on him     : sprint until he stops or the finger lets go
    ///  - quick tap elsewhere   : attack (tapX/tapY tell where, so the caller can aim at an enemy)
    ///  - two-finger tap        : pause
    ///  - tap on a shop         : <see cref="ClaimTap"/> takes it, no attack
    /// Call <see cref="SetCharacter"/> each frame before feeding touches. Without a character position the
    /// same drags work from wherever the finger goes down.
    /// </summary>
    public class GestureRecognizer
    {
        public float FlickMinInches = 0.35f;
        public float FlickWindow = 0.18f;
        public float FlickCooldown = 0.25f;
        public float WeakFlickSpeed = 2f;
        public float StrongFlickSpeed = 8f;
        public float MinJumpHold = 0.06f;
        public float MaxJumpHold = 0.3f;
        public float TapMaxTime = 0.25f;
        public float TapMaxInches = 0.12f;
        public float DoubleTapWindow = 0.35f;
        public float TwoFingerWindow = 0.3f;
        public float GrabRadiusInches = 0.55f;
        public float FollowRangeInches = 0.45f;
        public float FollowDeadzoneInches = 0.06f;
        public float BrakeDeadzoneInches = 0.12f;
        public float BrakeRangeInches = 0.7f;
        public float SteerResponse = 14f;
        public float RunDragInches = 0.15f;
        public float JumpDragInches = 0.45f;

        private struct Sample { public float t, x, y; }

        private sealed class Track
        {
            public float startX, startY, startTime, maxTravel;
            public float originX, originY, topY, grabOffsetX, lastFlick = -10f;
            public bool onCharacter, steering, running, flicked, secondTap;
            public readonly List<Sample> samples = new List<Sample>(16);
        }

        private readonly Dictionary<int, Track> tracks = new Dictionary<int, Track>();
        private readonly HashSet<int> seenThisFrame = new HashSet<int>();
        private readonly List<int> stale = new List<int>();
        private GestureFrame frame;
        private int steerFinger = -1;
        private float charX, charY, charRadius;
        private bool hasCharacter;
        private float targetX, targetY, smoothX, smoothY, lastTime = -1f;
        private bool steeringThisFrame;
        private float jumpHoldUntil = -10f;
        private float lastCharacterTap = -10f;
        private bool sprintLatched;
        private bool pendingTap, pendingOnCharacter;
        private float pendingTime, pendingX, pendingY;

        /// <summary>
        /// Asked about every tap that would attack, with its screen position. Return true to keep it from
        /// attacking (it was on a shop, or a menu is open); the frame then reports <see cref="GestureFrame.interact"/>.
        /// </summary>
        public Func<float, float, bool> ClaimTap;

        public int ActiveTouches => tracks.Count;
        public bool HasCharacter => hasCharacter;

        /// <summary>Where the character is on screen this frame and how big his touch area is (pixels).</summary>
        public void SetCharacter(float x, float y, float radiusPixels)
        {
            charX = x; charY = y; charRadius = radiusPixels; hasCharacter = true;
        }

        public void ClearCharacter() => hasCharacter = false;

        public bool IsOnCharacter(float x, float y, float dpi)
        {
            if (!hasCharacter) return false;
            float r = Math.Max(charRadius, GrabRadiusInches * Dpi(dpi));
            float dx = x - charX, dy = y - charY;
            return dx * dx + dy * dy <= r * r;
        }

        public void BeginFrame()
        {
            frame = new GestureFrame();
            seenThisFrame.Clear();
            targetX = targetY = 0f;
            steeringThisFrame = false;
        }

        /// <param name="dpi">Screen dots per inch (0 falls back to a sensible default).</param>
        public void Feed(int id, TouchState state, float x, float y, float time, float dpi)
        {
            dpi = Dpi(dpi);
            seenThisFrame.Add(id);

            if (state == TouchState.Began || !tracks.TryGetValue(id, out var tr))
            {
                tr = new Track { startX = x, startY = y, originX = x, originY = y, topY = y, startTime = time };
                tr.onCharacter = IsOnCharacter(x, y, dpi);
                if (tr.onCharacter)
                {
                    tr.grabOffsetX = x - charX;
                    if (time - lastCharacterTap <= DoubleTapWindow)
                    {
                        tr.secondTap = true;
                        sprintLatched = true;
                        lastCharacterTap = -10f;
                    }
                }
                tracks[id] = tr;
                if (steerFinger < 0 && (tr.onCharacter || !hasCharacter)) steerFinger = id;
            }

            float dx = x - tr.startX, dy = y - tr.startY;
            tr.maxTravel = Math.Max(tr.maxTravel, (float)Math.Sqrt(dx * dx + dy * dy) / dpi);

            tr.samples.Add(new Sample { t = time, x = x, y = y });
            while (tr.samples.Count > 1 && time - tr.samples[0].t > FlickWindow) tr.samples.RemoveAt(0);
            DetectFlick(tr, x, y, time, dpi);

            bool ended = state == TouchState.Ended;
            if (id == steerFinger && !ended)
            {
                DetectDragJump(tr, x, y, time, dpi);
                Steer(tr, x, y, time, dpi, tr.onCharacter ? charX + tr.grabOffsetX : tr.startX);
            }

            if (ended)
            {
                bool isTap = !tr.flicked && !tr.steering
                             && time - tr.startTime <= TapMaxTime && tr.maxTravel <= TapMaxInches;
                tracks.Remove(id);
                if (id == steerFinger)
                {
                    steerFinger = -1;
                    if (!isTap) sprintLatched = false;
                }
                if (isTap) RegisterTap(tr, time, x, y);
            }
        }

        private void DetectFlick(Track tr, float x, float y, float time, float dpi)
        {
            if (time - tr.lastFlick < FlickCooldown || tr.samples.Count < 2) return;
            var s = tr.samples[0];
            float dx = x - s.x, dy = y - s.y;
            float min = FlickMinInches * dpi;
            // Pulling down is also how you brake, so a slide needs a clearly harder flick.
            if (dy < 0f && tr.steering) min *= 1.4f;
            if (Math.Abs(dy) < min || Math.Abs(dy) <= Math.Abs(dx) * 1.3f) return;

            tr.flicked = true;
            tr.lastFlick = time;
            tr.samples.Clear();
            tr.samples.Add(new Sample { t = time, x = x, y = y });
            tr.originX = x; tr.originY = y; tr.topY = y;

            if (dy > 0f)
            {
                float speed = dy / dpi / Math.Max(time - s.t, 1f / 120f);
                Jump(Clamp01((speed - WeakFlickSpeed) / (StrongFlickSpeed - WeakFlickSpeed)), time);
            }
            else frame.slide = true;
        }

        /// <summary>
        /// A slow drag upward jumps too, once the finger is well above where it started (or above where it
        /// was at the last jump). Coming back up after a brake only returns to that line, so it never jumps.
        /// </summary>
        private void DetectDragJump(Track tr, float x, float y, float time, float dpi)
        {
            if (time - tr.lastFlick < FlickCooldown) return;
            float up = y - tr.originY, side = Math.Abs(x - tr.originX);
            if (up < JumpDragInches * dpi || up <= side) return;
            tr.flicked = true;
            tr.lastFlick = time;
            tr.samples.Clear();
            tr.samples.Add(new Sample { t = time, x = x, y = y });
            tr.originX = x; tr.originY = y; tr.topY = y;
            Jump(0f, time);
        }

        private void Jump(float strength, float time)
        {
            frame.jump = true;
            frame.jumpStrength = Math.Max(frame.jumpStrength, strength);
            jumpHoldUntil = Math.Max(jumpHoldUntil, time + MinJumpHold + (MaxJumpHold - MinJumpHold) * strength);
        }

        /// <param name="anchorX">Where the finger would be if he were exactly under it: his grab point, or
        /// the touch-down point when he is not on screen.</param>
        private void Steer(Track tr, float x, float y, float time, float dpi, float anchorX)
        {
            if (!tr.steering && (time - tr.startTime > TapMaxTime || tr.maxTravel > TapMaxInches)) tr.steering = true;
            if (!tr.steering) return;

            // He only sets off once the finger has been dragged sideways; holding still keeps him standing.
            if (!tr.running && Math.Abs(x - tr.startX) / dpi >= RunDragInches) tr.running = true;

            tr.topY = Math.Max(tr.topY, y);
            float side = (x - anchorX) / dpi;
            side = Math.Sign(side) * Math.Max(0f, Math.Abs(side) - FollowDeadzoneInches);
            float down = (tr.topY - y) / dpi;
            float brake = Clamp01((down - BrakeDeadzoneInches) / BrakeRangeInches);
            float forward = tr.running ? 1f - brake : 0f;

            targetX = tr.running ? Math.Max(-1f, Math.Min(1f, side / FollowRangeInches)) : 0f;
            targetY = forward;
            steeringThisFrame = true;
            frame.braking = tr.running && brake > 0.05f;
            if (brake > 0.4f) sprintLatched = false;
        }

        private void RegisterTap(Track tr, float time, float x, float y)
        {
            if (pendingTap && time - pendingTime <= TwoFingerWindow)
            {
                pendingTap = false;
                frame.pause = true;
                return;
            }
            foreach (var kv in tracks)
            {
                var t = kv.Value;
                if (!t.steering && !t.flicked && time - t.startTime <= TwoFingerWindow)
                {
                    pendingTap = true;
                    pendingOnCharacter = tr.onCharacter;
                    pendingTime = time; pendingX = x; pendingY = y;
                    return;
                }
            }
            Tap(tr.onCharacter, tr.secondTap, time, x, y);
        }

        private void Tap(bool onCharacter, bool secondTap, float time, float x, float y)
        {
            if (onCharacter)
            {
                if (!secondTap) lastCharacterTap = time;
                return;
            }
            frame.tapX = x;
            frame.tapY = y;
            if (ClaimTap != null && ClaimTap(x, y))
            {
                frame.interact = true;
                return;
            }
            frame.attack = true;
        }

        /// <summary>Drops touches that vanished without an end event, resolves delayed taps and smooths steering.</summary>
        public GestureFrame EndFrame(float time)
        {
            stale.Clear();
            foreach (var id in tracks.Keys) if (!seenThisFrame.Contains(id)) stale.Add(id);
            foreach (var id in stale)
            {
                tracks.Remove(id);
                if (id == steerFinger) { steerFinger = -1; sprintLatched = false; }
            }

            if (pendingTap && time - pendingTime > TwoFingerWindow)
            {
                pendingTap = false;
                Tap(pendingOnCharacter, false, pendingTime, pendingX, pendingY);
            }

            float dt = lastTime < 0f ? 0f : Math.Max(0f, time - lastTime);
            lastTime = time;
            float k = 1f - (float)Math.Exp(-SteerResponse * dt);
            smoothX += (targetX - smoothX) * k;
            smoothY += (targetY - smoothY) * k;
            if (!steeringThisFrame && Math.Abs(smoothX) < 0.02f && Math.Abs(smoothY) < 0.02f) smoothX = smoothY = 0f;

            if (steeringThisFrame || smoothX != 0f || smoothY != 0f)
            {
                float len = (float)Math.Sqrt(smoothX * smoothX + smoothY * smoothY);
                float scale = len > 1f ? 1f / len : 1f;
                frame.moveX = smoothX * scale;
                frame.moveY = smoothY * scale;
            }
            frame.steering = steeringThisFrame;
            frame.jumpHeld = time < jumpHoldUntil;
            frame.holding = tracks.Count > 0;
            frame.sprint = sprintLatched;
            return frame;
        }

        private static float Dpi(float dpi) => dpi <= 1f ? 160f : dpi;
        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
