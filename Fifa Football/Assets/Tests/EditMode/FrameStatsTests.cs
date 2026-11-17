using NUnit.Framework;
using FootballSim.Core;

namespace FootballSim.Tests
{
    public class FrameStatsTests
    {
        [Test]
        public void EmptyStatsReadZero()
        {
            var s = new FrameStats();
            Assert.AreEqual(0, s.Count);
            Assert.AreEqual(0f, s.AverageFps); Assert.AreEqual(0f, s.LowFps); Assert.AreEqual(0f, s.WorstMs);
        }

        [Test]
        public void SteadyFramesGiveTheirRateAndNoSpike()
        {
            var s = new FrameStats();
            for (int i = 0; i < 120; i++) s.Add(1f / 60f);
            Assert.AreEqual(60f, s.AverageFps, 0.01f);
            Assert.AreEqual(60f, s.LowFps, 0.01f);
            Assert.AreEqual(16.67f, s.WorstMs, 0.01f);
        }

        [Test]
        public void OneSlowFrameShowsInTheWorstAndLowButBarelyInTheAverage()
        {
            var s = new FrameStats();
            for (int i = 0; i < 199; i++) s.Add(1f / 60f);
            s.Add(0.1f);                          // one 100 ms hitch in 200 frames
            Assert.AreEqual(100f, s.WorstMs, 0.01f);
            Assert.AreEqual(2f / (0.1f + 1f / 60f), s.LowFps, 0.01f, "the slowest 1% of 200 frames is two frames: the hitch and one normal frame");
            Assert.Greater(s.AverageFps, 50f);
        }

        [Test]
        public void OnlyTheLastWindowCountsAndBadValuesAreIgnored()
        {
            var s = new FrameStats();
            for (int i = 0; i < FrameStats.Capacity; i++) s.Add(0.5f);   // old, slow
            for (int i = 0; i < FrameStats.Capacity; i++) s.Add(1f / 30f); // newer, replaces them all
            Assert.AreEqual(FrameStats.Capacity, s.Count);
            Assert.AreEqual(30f, s.AverageFps, 0.01f);
            s.Add(0f); s.Add(-1f); s.Add(float.NaN); s.Add(float.PositiveInfinity);
            Assert.AreEqual(FrameStats.Capacity, s.Count, "zero, negative, NaN and infinite times are not frames");
            s.Reset();
            Assert.AreEqual(0, s.Count);
        }
    }
}
