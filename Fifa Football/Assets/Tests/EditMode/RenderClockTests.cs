using NUnit.Framework;
using FootballSim.Net;

namespace FootballSim.Tests
{
    public class RenderClockTests
    {
        [Test]
        public void FirstStepStartsAtTheTarget()
        {
            var c = new RenderClock();
            Assert.AreEqual(100f, c.Step(0.016f, 100f), 1e-4f);
            Assert.IsTrue(c.Started);
        }

        [Test]
        public void WithTheTargetMovingAtRealTimeItRunsAtRealTime()
        {
            var c = new RenderClock();
            float target = 100f;
            c.Step(0.016f, target);
            for (int i = 0; i < 60; i++) { target += 1f; c.Step(1f / 60f, target); }
            Assert.AreEqual(target, c.Time, 0.01f);
        }

        [Test]
        public void ArrivalJitterMovesTheClockFarLessThanItMovesTheTarget()
        {
            var c = new RenderClock();
            float t = 100f; c.Step(1f / 60f, t);
            float maxStepClock = 0f, maxStepTarget = 0f, prevClock = c.Time, prevTarget = t;
            for (int i = 0; i < 300; i++)
            {
                t += 1f;
                float jittery = t + (i % 7 == 0 ? 4f : 0f) - (i % 5 == 0 ? 2f : 0f); // target follows arrival, which is uneven
                c.Step(1f / 60f, jittery);
                maxStepClock = System.Math.Max(maxStepClock, System.Math.Abs(c.Time - prevClock - 1f));
                maxStepTarget = System.Math.Max(maxStepTarget, System.Math.Abs(jittery - prevTarget - 1f));
                prevClock = c.Time; prevTarget = jittery;
            }
            Assert.Less(maxStepClock, maxStepTarget * 0.25f, "clock deviates from steady 1 tick per frame by far less than the raw target");
        }

        [Test]
        public void ABigJumpSnapsInsteadOfEasing()
        {
            var c = new RenderClock();
            c.Step(0.016f, 100f);
            Assert.AreEqual(500f, c.Step(0.016f, 500f), 1e-4f);
        }

        [Test]
        public void ResetStartsOver()
        {
            var c = new RenderClock();
            c.Step(0.016f, 100f); c.Reset();
            Assert.IsFalse(c.Started);
            Assert.AreEqual(7f, c.Step(0.016f, 7f), 1e-4f);
        }
    }
}
