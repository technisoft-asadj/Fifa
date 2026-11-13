using NUnit.Framework;
using FootballSim.Core;

namespace FootballSim.Tests
{
    public class SimRandomTests
    {
        [Test]
        public void SameSeedGivesTheSameSequence()
        {
            SimRandom.Seed(1234);
            var a = new float[20];
            for (int i = 0; i < a.Length; i++) a[i] = SimRandom.Value;
            SimRandom.Seed(1234);
            for (int i = 0; i < a.Length; i++) Assert.AreEqual(a[i], SimRandom.Value, "draw " + i);
        }

        [Test]
        public void DifferentSeedsGiveDifferentSequences()
        {
            SimRandom.Seed(1); float x = SimRandom.Value;
            SimRandom.Seed(2); float y = SimRandom.Value;
            Assert.AreNotEqual(x, y);
        }

        [Test]
        public void ZeroSeedStillWorks()
        {
            SimRandom.Seed(0);
            float sum = 0f;
            for (int i = 0; i < 100; i++) sum += SimRandom.Value;
            Assert.Greater(sum, 10f);
        }

        [Test]
        public void ValueStaysInZeroToOne()
        {
            SimRandom.Seed(99);
            for (int i = 0; i < 20000; i++)
            {
                float v = SimRandom.Value;
                Assert.GreaterOrEqual(v, 0f); Assert.Less(v, 1f);
            }
        }

        [Test]
        public void RangesStayInBoundsAndCoverThem()
        {
            SimRandom.Seed(7);
            bool sawLow = false, sawHigh = false;
            for (int i = 0; i < 5000; i++)
            {
                float f = SimRandom.Range(-3f, 3f);
                Assert.GreaterOrEqual(f, -3f); Assert.Less(f, 3f);
                int n = SimRandom.Range(0, 6);
                Assert.GreaterOrEqual(n, 0); Assert.Less(n, 6);
                if (n == 0) sawLow = true;
                if (n == 5) sawHigh = true;
            }
            Assert.IsTrue(sawLow && sawHigh, "every integer in [0,6) must be reachable");
        }

        [Test]
        public void MeanIsCloseToOneHalf()
        {
            SimRandom.Seed(2024);
            double sum = 0;
            const int n = 50000;
            for (int i = 0; i < n; i++) sum += SimRandom.Value;
            Assert.AreEqual(0.5, sum / n, 0.01);
        }

        [Test]
        public void EmptyIntRangeReturnsTheMinimum()
        {
            Assert.AreEqual(4, SimRandom.Range(4, 4));
            Assert.AreEqual(4, SimRandom.Range(4, 2));
        }

        [Test]
        public void DrawingFromTheAiStreamDoesNotChangeTheSimulationStream()
        {
            SimRandom.Seed(77);
            var plain = new float[10];
            for (int i = 0; i < plain.Length; i++) plain[i] = SimRandom.Value;

            SimRandom.Seed(77);
            var mixed = new float[10];
            for (int i = 0; i < mixed.Length; i++) { SimRandom.Ai.Range(-1f, 1f); mixed[i] = SimRandom.Value; SimRandom.Ai.Value.ToString(); }

            CollectionAssert.AreEqual(plain, mixed);
        }

        [Test]
        public void TheAiStreamIsSeededAndDiffersFromTheSimulationStream()
        {
            SimRandom.Seed(5); float a1 = SimRandom.Ai.Value, s1 = SimRandom.Value;
            SimRandom.Seed(5); float a2 = SimRandom.Ai.Value;
            Assert.AreEqual(a1, a2);
            Assert.AreNotEqual(a1, s1);
        }
    }
}
