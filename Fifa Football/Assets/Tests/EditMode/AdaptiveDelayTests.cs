using NUnit.Framework;
using FootballSim.Net;

namespace FootballSim.Tests
{
    public class AdaptiveDelayTests
    {
        [Test]
        public void BeforeAnySamplesTheDelayIsTheMinimum()
        {
            Assert.AreEqual(6f, new AdaptiveDelay().Ticks, 1e-4f);
        }

        [Test]
        public void ACleanLinkStaysAtTheMinimum()
        {
            var d = new AdaptiveDelay();
            double t = 0; uint tick = 0;
            for (int i = 0; i < 200; i++) { d.OnArrival(t, tick); t += 3.0 / 60.0; tick += 3; }
            Assert.AreEqual(6f, d.Ticks, 0.01f);
            Assert.Less(d.MeanErrorTicks, 0.01f);
        }

        [Test]
        public void AJitteryLinkRaisesTheDelayAndItStaysWithinTheCap()
        {
            var d = new AdaptiveDelay();
            double t = 0; uint tick = 0;
            for (int i = 0; i < 400; i++)
            {
                double jitter = (i % 2 == 0 ? 0.030 : -0.030); // +-30 ms
                d.OnArrival(t + jitter, tick); t += 3.0 / 60.0; tick += 3;
            }
            Assert.Greater(d.Ticks, 12f, "30 ms of jitter should need a deeper buffer");
            Assert.LessOrEqual(d.Ticks, 30f);
        }

        [Test]
        public void DelayComesBackDownWhenTheLinkSettles()
        {
            var d = new AdaptiveDelay();
            double t = 0; uint tick = 0;
            for (int i = 0; i < 200; i++) { d.OnArrival(t + (i % 2 == 0 ? 0.03 : -0.03), tick); t += 0.05; tick += 3; }
            float high = d.Ticks;
            for (int i = 0; i < 400; i++) { d.OnArrival(t, tick); t += 0.05; tick += 3; }
            Assert.Less(d.Ticks, high);
            Assert.AreEqual(6f, d.Ticks, 0.5f);
        }

        [Test]
        public void LateOrRepeatedTicksAreIgnored()
        {
            var d = new AdaptiveDelay();
            d.OnArrival(0, 10); d.OnArrival(0.05, 13);
            int before = d.Samples;
            d.OnArrival(0.06, 13); d.OnArrival(0.07, 9);
            Assert.AreEqual(before, d.Samples);
        }
    }
}
