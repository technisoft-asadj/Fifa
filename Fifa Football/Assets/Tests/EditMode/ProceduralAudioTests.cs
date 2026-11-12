using NUnit.Framework;
using FootballSim.Audio;

namespace FootballSim.Tests
{
    public class ProceduralAudioTests
    {
        static void AssertUsable(float[] d, float minSeconds, float maxSeconds)
        {
            Assert.GreaterOrEqual(d.Length, (int)(minSeconds * ProceduralAudio.SampleRate));
            Assert.LessOrEqual(d.Length, (int)(maxSeconds * ProceduralAudio.SampleRate));
            float peak = 0f;
            foreach (var s in d)
            {
                Assert.IsFalse(float.IsNaN(s) || float.IsInfinity(s));
                Assert.LessOrEqual(System.Math.Abs(s), 1f, "must not clip");
                if (System.Math.Abs(s) > peak) peak = System.Math.Abs(s);
            }
            Assert.Greater(peak, 0.1f, "must be audible");
            Assert.Less(System.Math.Abs(d[d.Length - 1]), 0.05f, "must end near silence (no click)");
        }

        [Test] public void KickIsAudibleShortAndClean() { AssertUsable(ProceduralAudio.Kick(), 0.1f, 0.4f); }
        [Test] public void ThudIsAudibleShortAndClean() { AssertUsable(ProceduralAudio.Thud(), 0.1f, 0.5f); }
        [Test] public void WhistleIsAudibleAndClean() { AssertUsable(ProceduralAudio.Whistle(), 0.3f, 1.2f); }
        [Test] public void CheerIsAudibleAndClean() { AssertUsable(ProceduralAudio.Cheer(), 1f, 4f); }

        [Test]
        public void GenerationIsDeterministic()
        {
            Assert.AreEqual(ProceduralAudio.Cheer(), ProceduralAudio.Cheer());
        }
    }
}
