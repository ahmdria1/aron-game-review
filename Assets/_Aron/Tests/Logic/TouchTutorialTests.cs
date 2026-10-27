// (c) Ahmed Alamrani. All rights reserved.
using Aron.Logic;
using NUnit.Framework;

namespace Aron.Tests.Logic
{
    public class TouchTutorialTests
    {
        private static GestureFrame Steering(float moveX) => new GestureFrame { steering = true, moveX = moveX, moveY = 0.8f };

        [Test]
        public void WalksThroughEveryGestureInOrder()
        {
            var tut = new TouchTutorial();
            Assert.AreEqual(TutorialStep.Steer, tut.Step);

            for (int i = 0; i < 10; i++) tut.Update(Steering(0.6f), 0.1f);
            Assert.AreEqual(TutorialStep.Jump, tut.Step);

            Assert.IsTrue(tut.Update(new GestureFrame { jump = true }, 0.02f));
            Assert.AreEqual(TutorialStep.Brake, tut.Step);

            Assert.IsTrue(tut.Update(new GestureFrame { steering = true, braking = true }, 0.02f));
            Assert.IsTrue(tut.Update(new GestureFrame { attack = true }, 0.02f));
            Assert.IsTrue(tut.Update(new GestureFrame { sprint = true }, 0.02f));
            Assert.AreEqual(TutorialStep.Pause, tut.Step);
            Assert.IsTrue(tut.Update(new GestureFrame { pause = true }, 0.02f));
            Assert.IsTrue(tut.IsDone);
            Assert.IsFalse(tut.Update(new GestureFrame { jump = true }, 0.02f));
        }

        [Test]
        public void OtherGesturesDoNotSkipTheCurrentCard()
        {
            var tut = new TouchTutorial((int)TutorialStep.Jump);
            Assert.IsFalse(tut.Update(new GestureFrame { attack = true, sprint = true }, 0.1f));
            Assert.AreEqual(TutorialStep.Jump, tut.Step);
        }

        [Test]
        public void SteerNeedsARealSidewaysMove()
        {
            var tut = new TouchTutorial();
            for (int i = 0; i < 30; i++) tut.Update(Steering(0.1f), 0.1f);
            Assert.AreEqual(TutorialStep.Steer, tut.Step, "running straight is not steering yet");
            for (int i = 0; i < 30; i++) tut.Update(new GestureFrame { moveX = 1f }, 0.1f);
            Assert.AreEqual(TutorialStep.Steer, tut.Step, "keyboard movement does not count");
        }

        [Test]
        public void BrakeOnlyCountsWhileHoldingHim()
        {
            var tut = new TouchTutorial((int)TutorialStep.Brake);
            Assert.IsFalse(tut.Update(new GestureFrame { braking = true }, 0.1f));
        }

        [Test]
        public void PauseCardTimesOut()
        {
            var tut = new TouchTutorial((int)TutorialStep.Pause);
            tut.Update(default, 2f);
            Assert.IsFalse(tut.IsDone);
            tut.Update(default, 2f);
            Assert.IsTrue(tut.IsDone);
        }

        [TestCase(-3, TutorialStep.Steer)]
        [TestCase(2, TutorialStep.Brake)]
        [TestCase(99, TutorialStep.Done)]
        public void ResumesFromSavedStep(int saved, TutorialStep expected)
        {
            Assert.AreEqual(expected, new TouchTutorial(saved).Step);
        }
    }
}
