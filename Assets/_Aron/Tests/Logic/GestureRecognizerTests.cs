// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic;
using NUnit.Framework;

namespace Aron.Tests.Logic
{
    public class GestureRecognizerTests
    {
        private const float Dpi = 100f; // 1 inch = 100 px keeps the numbers readable
        private const float CharX = 500f, CharY = 300f, CharRadius = 80f;
        private GestureRecognizer recognizer;
        private float charX;

        [SetUp]
        public void SetUp()
        {
            recognizer = new GestureRecognizer();
            charX = CharX;
        }

        private GestureFrame Step(float time, params (int id, TouchState state, float x, float y)[] touches)
        {
            recognizer.BeginFrame();
            foreach (var t in touches) recognizer.Feed(t.id, t.state, t.x, t.y, time, Dpi);
            return recognizer.EndFrame(time);
        }

        /// <summary>Same as <see cref="Step"/> but with the character on screen.</summary>
        private GestureFrame CharStep(float time, params (int id, TouchState state, float x, float y)[] touches)
        {
            recognizer.SetCharacter(charX, CharY, CharRadius);
            return Step(time, touches);
        }

        /// <summary>Holds finger 1 at (x, y) from <paramref name="from"/> to <paramref name="to"/> at 20 fps.</summary>
        private GestureFrame Hold(float from, float to, float x, float y)
        {
            var f = default(GestureFrame);
            for (float t = from; t <= to + 1e-4f; t += 0.05f) f = CharStep(t, (1, TouchState.Moved, x, y));
            return f;
        }

        /// <summary>Puts a finger on him and drags it 0.3 inch right; he has already moved over under it.</summary>
        private const float RunX = CharX + 30f;

        private GestureFrame StartRunning(float until)
        {
            CharStep(0f, (1, TouchState.Began, CharX, CharY));
            charX = RunX;
            return Hold(0.05f, until, RunX, CharY);
        }

        // ---- Touching the character

        [Test]
        public void HoldingStillOnTheCharacterKeepsHimStanding()
        {
            CharStep(0f, (1, TouchState.Began, CharX, CharY));
            var f = Hold(0.05f, 1f, CharX, CharY);
            Assert.IsTrue(f.steering, "the finger has hold of him");
            Assert.AreEqual(0f, f.moveX);
            Assert.AreEqual(0f, f.moveY, "no running just for holding");
            Assert.IsFalse(f.braking);
        }

        [Test]
        public void DraggingLeftMovesHimLeft()
        {
            CharStep(0f, (1, TouchState.Began, CharX, CharY));
            var f = Hold(0.05f, 1f, CharX - 60f, CharY);
            Assert.Less(f.moveX, -0.5f);
            Assert.Greater(f.moveY, 0.5f, "a drag gets him going");
        }

        [Test]
        public void TinySideWobbleDoesNotSetHimOff()
        {
            CharStep(0f, (1, TouchState.Began, CharX, CharY));
            var f = Hold(0.05f, 1f, CharX + 8f, CharY);
            Assert.AreEqual(0f, f.moveY);
            Assert.AreEqual(0f, f.moveX);
        }

        [Test]
        public void DraggingRightRunsAndFollowsTheFinger()
        {
            CharStep(0f, (1, TouchState.Began, CharX, CharY));
            var f = Hold(0.05f, 1f, CharX + 60f, CharY);
            Assert.IsTrue(f.steering);
            Assert.Greater(f.moveX, 0.5f, "finger is right of him, so he heads right");
            Assert.Greater(f.moveY, 0.5f, "and keeps running forward");
            Assert.LessOrEqual(f.moveX * f.moveX + f.moveY * f.moveY, 1.0001f);
            Assert.IsFalse(f.jump);
        }

        [Test]
        public void StopsSteeringSidewaysOnceUnderTheFinger()
        {
            CharStep(0f, (1, TouchState.Began, CharX, CharY));
            Hold(0.05f, 0.5f, CharX + 60f, CharY);
            charX = CharX + 60f; // he caught up with the finger
            var f = Hold(0.55f, 1.2f, CharX + 60f, CharY);
            Assert.AreEqual(0f, f.moveX, 0.05f);
            Assert.Greater(f.moveY, 0.9f);
        }

        [Test]
        public void GrabOffsetIsKeptSoHeDoesNotJumpToTheFinger()
        {
            // Pressing near his edge must not make him lurch towards the fingertip.
            CharStep(0f, (1, TouchState.Began, CharX + 50f, CharY));
            var f = Hold(0.05f, 0.8f, CharX + 50f, CharY);
            Assert.AreEqual(0f, f.moveX, 0.05f);
        }

        [Test]
        public void PullingDownSlowsHimToAStop()
        {
            var f = StartRunning(0.8f);
            Assert.Greater(f.moveY, 0.9f);

            float y = CharY, t = 0.85f;
            for (; y > CharY - 100f; t += 0.05f) { y -= 10f; f = CharStep(t, (1, TouchState.Moved, RunX, y)); }
            Assert.IsFalse(f.slide, "a slow pull is braking, not a slide");
            f = Hold(t, t + 0.6f, RunX, y);
            Assert.IsTrue(f.braking);
            Assert.Less(f.moveY, 0.05f);
        }

        [Test]
        public void SmallPullOnlySlowsHimDown()
        {
            StartRunning(0.4f);
            for (int i = 1; i <= 5; i++) CharStep(0.4f + i * 0.05f, (1, TouchState.Moved, RunX, CharY - i * 10f));
            var f = Hold(0.7f, 1.4f, RunX, CharY - 50f);
            Assert.IsFalse(f.slide);
            Assert.Greater(f.moveY, 0.3f);
            Assert.Less(f.moveY, 0.8f);
        }

        [Test]
        public void ReleasingLetsHimCoastToAStop()
        {
            Assert.Greater(StartRunning(0.5f).moveY, 0.5f);
            var f = CharStep(0.55f, (1, TouchState.Ended, RunX, CharY));
            Assert.IsFalse(f.attack);
            Assert.IsFalse(f.steering);
            for (float t = 0.6f; t < 1.2f; t += 0.05f) f = CharStep(t);
            Assert.AreEqual(0f, f.moveY);
            Assert.AreEqual(0f, f.moveX);
        }

        [Test]
        public void FlickUpFromTheCharacterJumps()
        {
            CharStep(0f, (1, TouchState.Began, CharX, CharY));
            var f = CharStep(0.05f, (1, TouchState.Moved, CharX, CharY + 60f));
            Assert.IsTrue(f.jump);
            Assert.IsTrue(f.jumpHeld);
            f = CharStep(0.1f, (1, TouchState.Moved, CharX, CharY + 60f));
            Assert.IsFalse(f.jump, "one jump per flick");
        }

        [Test]
        public void SlowDragUpJumps()
        {
            CharStep(0f, (1, TouchState.Began, CharX, CharY));
            var f = default(GestureFrame);
            bool jumped = false;
            for (int i = 1; i <= 12; i++)
            {
                f = CharStep(i * 0.1f, (1, TouchState.Moved, CharX, CharY + i * 5f));
                jumped |= f.jump;
            }
            Assert.IsTrue(jumped, "0.6 inch up over a second still jumps");
        }

        [Test]
        public void ComingBackUpAfterABrakeDoesNotJump()
        {
            StartRunning(0.5f);
            float t = 0.55f;
            for (float y = CharY; y > CharY - 80f; t += 0.1f) { y -= 10f; CharStep(t, (1, TouchState.Moved, RunX, y)); }
            bool jumped = false;
            var f = default(GestureFrame);
            for (float y = CharY - 80f; y < CharY; t += 0.1f) { y += 10f; f = CharStep(t, (1, TouchState.Moved, RunX, y)); jumped |= f.jump; }
            Assert.IsFalse(jumped);
            f = Hold(t, t + 0.5f, RunX, CharY);
            Assert.Greater(f.moveY, 0.9f, "back to full speed");
        }

        [Test]
        public void HarderFlickJumpsHigher()
        {
            // Lazy flick: 0.4 inch over 0.15 s.
            CharStep(0f, (1, TouchState.Began, CharX, CharY));
            CharStep(0.05f, (1, TouchState.Moved, CharX, CharY + 15f));
            CharStep(0.1f, (1, TouchState.Moved, CharX, CharY + 30f));
            var weak = CharStep(0.15f, (1, TouchState.Moved, CharX, CharY + 40f));
            Assert.IsTrue(weak.jump);
            bool weakHeld = CharStep(0.3f, (1, TouchState.Moved, CharX, CharY + 40f)).jumpHeld;

            SetUp();
            CharStep(0f, (1, TouchState.Began, CharX, CharY));
            var strong = CharStep(0.05f, (1, TouchState.Moved, CharX, CharY + 80f));
            Assert.IsTrue(strong.jump);
            bool strongHeld = CharStep(0.2f, (1, TouchState.Moved, CharX, CharY + 80f)).jumpHeld;

            Assert.Greater(strong.jumpStrength, weak.jumpStrength);
            Assert.IsFalse(weakHeld, "a lazy flick lets go of the jump early (short hop)");
            Assert.IsTrue(strongHeld, "a hard flick keeps the jump held (full height)");
        }

        [Test]
        public void SecondFlickWhileSteeringDoubleJumps()
        {
            StartRunning(0.5f);
            Assert.IsTrue(CharStep(0.55f, (1, TouchState.Moved, RunX, CharY + 60f)).jump);
            Hold(0.6f, 0.9f, RunX, CharY + 60f);
            Assert.IsTrue(CharStep(0.95f, (1, TouchState.Moved, RunX, CharY + 120f)).jump);
        }

        [Test]
        public void HardFlickDownFromTheCharacterSlides()
        {
            CharStep(0f, (1, TouchState.Began, CharX, CharY));
            var f = CharStep(0.05f, (1, TouchState.Moved, CharX, CharY - 70f));
            Assert.IsTrue(f.slide);
            Assert.IsFalse(f.jump);
        }

        [Test]
        public void TapAwayFromTheCharacterAttacksWhereItLanded()
        {
            CharStep(0f, (1, TouchState.Began, 900f, 600f));
            var f = CharStep(0.1f, (1, TouchState.Ended, 901f, 600f));
            Assert.IsTrue(f.attack);
            Assert.AreEqual(901f, f.tapX);
            Assert.AreEqual(600f, f.tapY);
        }

        [Test]
        public void TapOnTheCharacterDoesNotAttack()
        {
            CharStep(0f, (1, TouchState.Began, CharX, CharY));
            var f = CharStep(0.1f, (1, TouchState.Ended, CharX, CharY));
            Assert.IsFalse(f.attack);
            for (float t = 0.15f; t < 1f; t += 0.05f) Assert.IsFalse(CharStep(t).attack);
        }

        [Test]
        public void DragAwayFromTheCharacterDoesNotMoveHim()
        {
            CharStep(0f, (1, TouchState.Began, 900f, 600f));
            CharStep(0.3f, (1, TouchState.Moved, 910f, 600f));
            var f = CharStep(0.4f, (1, TouchState.Moved, 960f, 600f));
            Assert.AreEqual(0f, f.moveX);
            Assert.AreEqual(0f, f.moveY);
        }

        [Test]
        public void DoubleTapOnTheCharacterSprintsUntilLetGo()
        {
            CharStep(0f, (1, TouchState.Began, CharX, CharY));
            CharStep(0.1f, (1, TouchState.Ended, CharX, CharY));
            var f = CharStep(0.25f, (2, TouchState.Began, CharX, CharY));
            Assert.IsTrue(f.sprint);
            for (float t = 0.3f; t < 1f; t += 0.05f) f = CharStep(t, (2, TouchState.Moved, CharX, CharY));
            Assert.IsTrue(f.sprint);
            Assert.IsTrue(f.steering);
            f = CharStep(1f, (2, TouchState.Ended, CharX, CharY));
            Assert.IsFalse(f.sprint);
        }

        [Test]
        public void QuickDoubleTapKeepsSprintForTheNextRun()
        {
            CharStep(0f, (1, TouchState.Began, CharX, CharY));
            CharStep(0.1f, (1, TouchState.Ended, CharX, CharY));
            CharStep(0.2f, (2, TouchState.Began, CharX, CharY));
            Assert.IsTrue(CharStep(0.28f, (2, TouchState.Ended, CharX, CharY)).sprint);
            CharStep(0.6f, (3, TouchState.Began, CharX, CharY));
            var f = Hold3(0.65f, 1.2f);
            Assert.IsTrue(f.sprint);
        }

        private GestureFrame Hold3(float from, float to)
        {
            var f = default(GestureFrame);
            for (float t = from; t <= to + 1e-4f; t += 0.05f) f = CharStep(t, (3, TouchState.Moved, CharX, CharY));
            return f;
        }

        [Test]
        public void SlowDoubleTapIsNotASprint()
        {
            CharStep(0f, (1, TouchState.Began, CharX, CharY));
            CharStep(0.1f, (1, TouchState.Ended, CharX, CharY));
            Assert.IsFalse(CharStep(0.7f, (2, TouchState.Began, CharX, CharY)).sprint);
        }

        [Test]
        public void BrakingHardDropsTheSprint()
        {
            CharStep(0f, (1, TouchState.Began, CharX, CharY));
            CharStep(0.1f, (1, TouchState.Ended, CharX, CharY));
            CharStep(0.2f, (2, TouchState.Began, CharX, CharY));
            var f = default(GestureFrame);
            for (float t = 0.25f; t < 0.6f; t += 0.05f) f = CharStep(t, (2, TouchState.Moved, CharX, CharY));
            Assert.IsTrue(f.sprint);
            float y = CharY;
            for (float t = 0.6f; y > CharY - 80f; t += 0.05f) { y -= 10f; f = CharStep(t, (2, TouchState.Moved, CharX, y)); }
            Assert.IsFalse(f.sprint);
        }

        [Test]
        public void TouchAreaIsAtLeastAFingertipWide()
        {
            recognizer.SetCharacter(CharX, CharY, 5f); // tiny on screen, far from the camera
            Assert.IsTrue(recognizer.IsOnCharacter(CharX + 50f, CharY, Dpi));
            Assert.IsFalse(recognizer.IsOnCharacter(CharX + 70f, CharY, Dpi));
        }

        [Test]
        public void TwoFingerTapPausesEvenWhenOneLandsOnTheCharacter()
        {
            CharStep(0f, (1, TouchState.Began, CharX, CharY), (2, TouchState.Began, 900f, 300f));
            var f = CharStep(0.1f, (1, TouchState.Ended, CharX, CharY), (2, TouchState.Moved, 900f, 300f));
            Assert.IsFalse(f.attack);
            f = CharStep(0.15f, (2, TouchState.Ended, 900f, 300f));
            Assert.IsTrue(f.pause);
            Assert.IsFalse(f.attack);
        }

        [Test]
        public void FlickWithASecondFingerJumpsWhileSteering()
        {
            CharStep(0f, (1, TouchState.Began, CharX, CharY));
            Hold(0.05f, 0.5f, CharX, CharY);
            CharStep(0.55f, (1, TouchState.Moved, CharX, CharY), (2, TouchState.Began, 1200f, 300f));
            var f = CharStep(0.6f, (1, TouchState.Moved, CharX, CharY), (2, TouchState.Moved, 1200f, 370f));
            Assert.IsTrue(f.jump);
            Assert.IsTrue(f.steering, "the steering finger is not disturbed");
        }

        // ---- No character on screen: the same drags, measured from where the finger went down

        [Test]
        public void WithoutTheCharacterHoldingStillDoesNotMove()
        {
            Step(0f, (1, TouchState.Began, 500, 500));
            var f = default(GestureFrame);
            for (float t = 0.05f; t < 1f; t += 0.05f) f = Step(t, (1, TouchState.Moved, 500, 500));
            Assert.AreEqual(0f, f.moveX);
            Assert.AreEqual(0f, f.moveY);
        }

        [Test]
        public void WithoutTheCharacterDragDownBrakes()
        {
            Step(0f, (1, TouchState.Began, 500, 500));
            var f = default(GestureFrame);
            for (float t = 0.05f; t < 0.6f; t += 0.05f) f = Step(t, (1, TouchState.Moved, 530, 500));
            Assert.Greater(f.moveY, 0.8f, "dragged right, so he is off and running");
            float y = 500f, tt = 0.6f;
            for (; y > 400f; tt += 0.05f) { y -= 10f; f = Step(tt, (1, TouchState.Moved, 530, y)); }
            for (float t = tt; t < tt + 0.6f; t += 0.05f) f = Step(t, (1, TouchState.Moved, 530, y));
            Assert.IsTrue(f.braking);
            Assert.Less(f.moveY, 0.05f);
        }

        [Test]
        public void QuickTapAttacks()
        {
            Step(0f, (1, TouchState.Began, 500, 500));
            var f = Step(0.1f, (1, TouchState.Ended, 502, 501));
            Assert.IsTrue(f.attack);
            Assert.IsFalse(f.jump);
        }

        [Test]
        public void SlowPressIsNotATap()
        {
            Step(0f, (1, TouchState.Began, 500, 500));
            var f = Step(0.6f, (1, TouchState.Ended, 500, 500));
            Assert.IsFalse(f.attack);
        }

        [Test]
        public void FastFlickDownSlides()
        {
            Step(0f, (1, TouchState.Began, 500, 500));
            var f = Step(0.1f, (1, TouchState.Moved, 498, 440));
            Assert.IsTrue(f.slide);
            Assert.IsFalse(f.jump);
        }

        [Test]
        public void SlowDragMovesWithoutJumping()
        {
            Step(0f, (1, TouchState.Began, 500, 500));
            Step(0.3f, (1, TouchState.Moved, 500, 510));
            var f = Step(0.4f, (1, TouchState.Moved, 530, 500));
            Assert.IsFalse(f.jump);
            for (float t = 0.45f; t < 1f; t += 0.05f) f = Step(t, (1, TouchState.Moved, 530, 500));
            Assert.Greater(f.moveX, 0.4f);
            Assert.LessOrEqual(f.moveX * f.moveX + f.moveY * f.moveY, 1.0001f);
        }

        [Test]
        public void DragIsClampedToUnitLength()
        {
            Step(0f, (1, TouchState.Began, 500, 500));
            Step(0.3f, (1, TouchState.Moved, 510, 500));
            var f = default(GestureFrame);
            for (float t = 0.4f; t < 1.2f; t += 0.05f) f = Step(t, (1, TouchState.Moved, 900, 500));
            Assert.Greater(f.moveX, 0.6f);
            Assert.LessOrEqual(f.moveX * f.moveX + f.moveY * f.moveY, 1.0001f);
        }

        [Test]
        public void HorizontalFlickIsNotAJump()
        {
            Step(0f, (1, TouchState.Began, 500, 500));
            var f = Step(0.1f, (1, TouchState.Moved, 580, 520));
            Assert.IsFalse(f.jump);
            Assert.IsFalse(f.slide);
        }

        [Test]
        public void TapWhileDraggingStillAttacks()
        {
            Step(0f, (1, TouchState.Began, 300, 300));
            Step(0.3f, (1, TouchState.Moved, 340, 300));
            Step(0.35f, (1, TouchState.Moved, 340, 300), (2, TouchState.Began, 900, 500));
            var f = Step(0.45f, (1, TouchState.Moved, 340, 300), (2, TouchState.Ended, 900, 500));
            Assert.IsTrue(f.attack);
            Assert.IsFalse(f.pause);
        }

        [Test]
        public void TwoFingerTapPausesInsteadOfAttacking()
        {
            Step(0f, (1, TouchState.Began, 300, 300), (2, TouchState.Began, 900, 300));
            var f = Step(0.1f, (1, TouchState.Ended, 300, 300), (2, TouchState.Moved, 900, 300));
            Assert.IsFalse(f.attack);
            f = Step(0.15f, (2, TouchState.Ended, 900, 300));
            Assert.IsTrue(f.pause);
            Assert.IsFalse(f.attack);
            f = Step(1f);
            Assert.IsFalse(f.attack, "the swallowed tap must not fire later");
        }

        [Test]
        public void LostTouchesAreDropped()
        {
            Step(0f, (1, TouchState.Began, 500, 500));
            Step(0.1f);
            Assert.AreEqual(0, recognizer.ActiveTouches);
        }

        [Test]
        public void ZeroDpiFallsBackToDefault()
        {
            recognizer.BeginFrame();
            recognizer.Feed(1, TouchState.Began, 0, 0, 0f, 0f);
            recognizer.EndFrame(0f);
            recognizer.BeginFrame();
            recognizer.Feed(1, TouchState.Ended, 1, 1, 0.1f, 0f);
            Assert.IsTrue(recognizer.EndFrame(0.1f).attack);
        }

        // ---- Taps claimed by the world (shop doors)

        [Test]
        public void ClaimedTapDoesNotAttack()
        {
            float cx = 0f, cy = 0f;
            recognizer.ClaimTap = (x, y) => { cx = x; cy = y; return true; };
            CharStep(0f, (1, TouchState.Began, 900f, 600f));
            var f = CharStep(0.1f, (1, TouchState.Ended, 900f, 600f));
            Assert.IsFalse(f.attack);
            Assert.IsTrue(f.interact);
            Assert.AreEqual(900f, cx);
            Assert.AreEqual(600f, cy);
        }

        [Test]
        public void TapNobodyClaimsStillAttacks()
        {
            recognizer.ClaimTap = (x, y) => false;
            CharStep(0f, (1, TouchState.Began, 900f, 600f));
            var f = CharStep(0.1f, (1, TouchState.Ended, 900f, 600f));
            Assert.IsTrue(f.attack);
            Assert.IsFalse(f.interact);
        }

        [Test]
        public void TapOnTheCharacterIsNeverOfferedForClaiming()
        {
            bool asked = false;
            recognizer.ClaimTap = (x, y) => asked = true;
            CharStep(0f, (1, TouchState.Began, CharX, CharY));
            CharStep(0.1f, (1, TouchState.Ended, CharX, CharY));
            Assert.IsFalse(asked);
        }

        [Test]
        public void FlickIsNeverOfferedForClaiming()
        {
            bool asked = false;
            recognizer.ClaimTap = (x, y) => asked = true;
            CharStep(0f, (1, TouchState.Began, 900f, 300f));
            CharStep(0.05f, (1, TouchState.Moved, 900f, 360f));
            CharStep(0.1f, (1, TouchState.Ended, 900f, 420f));
            Assert.IsFalse(asked);
        }

        [Test]
        public void TwoFingerPauseIsNotClaimed()
        {
            recognizer.ClaimTap = (x, y) => true;
            CharStep(0f, (1, TouchState.Began, 900f, 600f), (2, TouchState.Began, 1000f, 600f));
            CharStep(0.08f, (1, TouchState.Ended, 900f, 600f), (2, TouchState.Moved, 1000f, 600f));
            var f = CharStep(0.1f, (2, TouchState.Ended, 1000f, 600f));
            Assert.IsTrue(f.pause);
            Assert.IsFalse(f.interact);
        }
    }
}
