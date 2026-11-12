using NUnit.Framework;
using UnityEngine;
using FootballSim.Core;
using FootballSim.Inputs;

namespace FootballSim.Tests
{
    public class SafeAreaTests
    {
        [Test]
        public void SafeAreaInsetsBecomeAnchors()
        {
            var go = new GameObject("rt", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            // 2436x1080 landscape phone with a 108 px cutout on the left
            TouchControls.ApplySafeArea(rt, new Rect(108, 0, 2328, 1080), 2436, 1080);
            Assert.AreEqual(108f / 2436f, rt.anchorMin.x, 1e-4f);
            Assert.AreEqual(0f, rt.anchorMin.y, 1e-4f);
            Assert.AreEqual(1f, rt.anchorMax.x, 1e-4f);
            Assert.AreEqual(1f, rt.anchorMax.y, 1e-4f);
            Object.Destroy(go);
        }

        [Test]
        public void InvalidSafeAreaFallsBackToFullScreen()
        {
            var go = new GameObject("rt", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            TouchControls.ApplySafeArea(rt, new Rect(0, 0, 0, 0), 0, 0);
            Assert.AreEqual(Vector2.zero, rt.anchorMin);
            Assert.AreEqual(Vector2.one, rt.anchorMax);
            Object.Destroy(go);
        }

        [Test]
        public void FrameRateTargetIsSixty()
        {
            Assert.AreEqual(AppBootstrap.TargetFps, Application.targetFrameRate, "mobile default is 30 unless set");
        }
    }
}
