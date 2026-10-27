// (c) Ahmed Alamrani. All rights reserved.
using System;

namespace Aron.Logic
{
    public enum TutorialStep { Steer, Jump, Brake, Attack, Sprint, Pause, Done }

    /// <summary>
    /// First-play walk through the touch gestures. Each card stays up until the player has actually done
    /// the gesture once; the last card (pause) is information only and times out. The step index is what
    /// gets saved, so a player who quits halfway picks up where they left off.
    /// </summary>
    public class TouchTutorial
    {
        public float SteerSeconds = 0.6f;
        public float PauseCardSeconds = 3.5f;
        public float SteerThreshold = 0.3f;

        private float progress;

        public TutorialStep Step { get; private set; }
        public bool IsDone => Step == TutorialStep.Done;

        public TouchTutorial(int savedStep = 0)
        {
            Step = (TutorialStep)Math.Max(0, Math.Min(savedStep, (int)TutorialStep.Done));
        }

        /// <returns>True when this frame finished the current step.</returns>
        public bool Update(GestureFrame g, float dt)
        {
            bool done;
            switch (Step)
            {
                case TutorialStep.Steer:
                    if (g.steering && Math.Abs(g.moveX) > SteerThreshold) progress += dt;
                    done = progress >= SteerSeconds;
                    break;
                case TutorialStep.Jump: done = g.jump; break;
                case TutorialStep.Brake: done = g.steering && g.braking; break;
                case TutorialStep.Attack: done = g.attack; break;
                case TutorialStep.Sprint: done = g.sprint; break;
                case TutorialStep.Pause:
                    progress += dt;
                    done = g.pause || progress >= PauseCardSeconds;
                    break;
                default: return false;
            }
            if (!done) return false;
            Step++;
            progress = 0f;
            return true;
        }
    }
}
