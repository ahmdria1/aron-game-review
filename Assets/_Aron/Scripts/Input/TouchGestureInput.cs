// (c) Ahmed Alamrani. All rights reserved.
using Aron.Config;
using Aron.Core;
using Aron.Save;
using Aron.Logic;
using Aron.Logic.GameFeel;
using Aron.Player;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using ETouchPhase = UnityEngine.InputSystem.TouchPhase;
#endif

namespace Aron.Input
{
    /// <summary>
    /// Button-free touch controls: the player grabs Aron himself. Each frame his screen position is handed
    /// to the <see cref="GestureRecognizer"/>, and attack taps are turned into an aim point on whatever was tapped.
    /// Taps on a shop front open the shop instead of attacking.
    /// </summary>
    public class TouchGestureInput : IIntentSource
    {
        public const string SimulatePref = "Aron.SimulateTouch";

        private const float AimRange = 4f;

        private readonly GestureRecognizer recognizer = new GestureRecognizer();
        private readonly RaycastHit[] hits = new RaycastHit[8];

        public GestureRecognizer Recognizer => recognizer;

        /// <summary>The last recognised frame, for the tutorial overlay.</summary>
        public static GestureFrame Last { get; private set; }

        /// <summary>True once any finger has touched the screen this session.</summary>
        public static bool TouchSeen { get; private set; }

        public TouchGestureInput()
        {
            recognizer.ClaimTap = (x, y) => Aron.Heroes.HeroTeam.ClaimTap(x, y) || Aron.Store.ShopFront.ClaimTap(x, y);
#if ENABLE_INPUT_SYSTEM
            if (!EnhancedTouchSupport.enabled) EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
            if (UnityEditor.EditorPrefs.GetBool(SimulatePref, false)) TouchSimulation.Enable();
            else TouchSimulation.Disable();
#endif
#endif
        }

        public PlayerIntent Poll()
        {
            float now = Time.unscaledTime;
            float dpi = Screen.dpi;
            var cam = Camera.main;
            TrackCharacter(cam, dpi);

            var fv = FeelSettings.V;
            recognizer.SteerResponse = fv.touchSteerResponse;
            recognizer.FollowDeadzoneInches = fv.touchFollowDeadzoneInches;
            recognizer.RunDragInches = fv.touchRunDragInches;
            recognizer.BeginFrame();
#if ENABLE_INPUT_SYSTEM
            foreach (var t in ETouch.activeTouches)
            {
                TouchSeen = true;
                if (Aron.Diagnostics.FeelTuningPanel.BlocksTouch(t.screenPosition)) continue;   // a finger on the hidden tuning panel is not steering
                recognizer.Feed(t.touchId, Map(t.phase), t.screenPosition.x, t.screenPosition.y, now, dpi);
            }
#else
            for (int i = 0; i < UnityEngine.Input.touchCount; i++)
            {
                TouchSeen = true;
                var t = UnityEngine.Input.GetTouch(i);
                if (Aron.Diagnostics.FeelTuningPanel.BlocksTouch(t.position)) continue;
                var state = t.phase == TouchPhase.Began ? TouchState.Began
                          : t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled ? TouchState.Ended
                          : TouchState.Moved;
                recognizer.Feed(t.fingerId, state, t.position.x, t.position.y, now, dpi);
            }
#endif
            var g = recognizer.EndFrame(now);
            Last = g;

            // Dead zone and response curve on the analog touch steering (keyboard is digital and stays as it is).
            var acc = SaveSystem.Data != null ? SaveSystem.Data.access : null;
            InputShaping.Apply(fv, g.moveX, g.moveY, acc != null && acc.oneHand, out float sx, out float sy);
            var intent = new PlayerIntent
            {
                move = new Vector2(sx, sy),
                jumpPressed = g.jump,
                jumpHeld = g.jumpHeld,
                fingerDown = g.holding,
                attackPressed = g.attack,
                slidePressed = g.slide,
                pausePressed = g.pause,
                sprintHeld = g.sprint,
            };
            if (g.attack && cam != null)
            {
                var tap = new Vector2(g.tapX, g.tapY);
                // A tap right on something wins; a near miss gets the light assist; a swing with no target turns toward the nearest enemy.
                if (TryAim(cam, tap, out var aim) || Aron.Combat.AimAssistRuntime.TrySling(cam, tap, out aim) || Aron.Combat.AimAssistRuntime.TryMelee(cam, out aim))
                {
                    intent.hasAim = true;
                    intent.aimPoint = aim;
                }
            }
            return intent;
        }

        private void TrackCharacter(Camera cam, float dpi)
        {
            var player = PlayerController.Active;
            if (cam == null || player == null) { recognizer.ClearCharacter(); return; }

            Vector3 feet = cam.WorldToScreenPoint(player.transform.position);
            Vector3 head = cam.WorldToScreenPoint(player.transform.position + Vector3.up * player.BodyHeight);
            if (feet.z <= 0f || head.z <= 0f) { recognizer.ClearCharacter(); return; }

            // Aim for the middle of the body, and be generous: most of a thumb covers him anyway.
            Vector2 mid = (feet + head) * 0.5f;
            float radius = Mathf.Abs(head.y - feet.y) * 0.8f;
            recognizer.SetCharacter(mid.x, mid.y, radius);
        }

        private bool TryAim(Camera cam, Vector2 screen, out Vector3 point)
        {
            point = default;
            var player = PlayerController.Active;
            if (player == null) return false;
            var ray = cam.ScreenPointToRay(screen);
            int n = Physics.SphereCastNonAlloc(ray, 0.4f, hits, 80f, ~0, QueryTriggerInteraction.Collide);
            float best = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var col = hits[i].collider;
                if (col.transform.IsChildOf(player.transform)) continue;
                var target = col.GetComponentInParent<IDamageable>();
                if (target == null || !target.CanBeDamaged) continue;
                var c = ((Component)target).transform.position;
                float d = (c - player.transform.position).sqrMagnitude;
                if (d < AimRange * AimRange && d < best) { best = d; point = c; }
            }
            return best < float.MaxValue;
        }

#if ENABLE_INPUT_SYSTEM
        private static TouchState Map(ETouchPhase p)
        {
            switch (p)
            {
                case ETouchPhase.Began: return TouchState.Began;
                case ETouchPhase.Ended:
                case ETouchPhase.Canceled: return TouchState.Ended;
                default: return TouchState.Moved;
            }
        }
#endif
    }
}
