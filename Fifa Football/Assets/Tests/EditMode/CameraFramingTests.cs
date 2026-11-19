using System.Collections.Generic;
using FootballSim.Core;
using FootballSim.Players;
using FootballSim.UI;
using NUnit.Framework;
using UnityEngine;

namespace FootballSim.Tests
{
    public class CameraFramingTests
    {
        [Test]
        public void TightPlayZoomsInAndSpreadPlayZoomsOutWithinLimits()
        {
            var tight = new List<Vector2> { new Vector2(2, 1), new Vector2(-3, 2), new Vector2(1, -4) };
            Assert.AreEqual(CameraFraming.MinAuto, CameraFraming.AutoZoom(Vector2.zero, tight), 1e-4f, "everyone close: as close as the camera goes");
            var medium = new List<Vector2> { new Vector2(15, 0) };
            Assert.AreEqual(1f, CameraFraming.AutoZoom(Vector2.zero, medium), 1e-4f, "the reference radius is the normal view");
            var spread = new List<Vector2> { new Vector2(0, 21) };
            Assert.AreEqual(1.4f, CameraFraming.AutoZoom(Vector2.zero, spread), 1e-3f);
            var far = new List<Vector2> { new Vector2(60, 60) };
            Assert.AreEqual(CameraFraming.MaxAuto, CameraFraming.AutoZoom(Vector2.zero, far), 1e-4f, "never wider than the limit");
        }

        [Test]
        public void TheFarthestPointDecidesTheZoom()
        {
            var pts = new List<Vector2> { new Vector2(3, 0), new Vector2(0, 18), new Vector2(-6, 6) };
            Assert.AreEqual(18f / CameraFraming.ReferenceRadius, CameraFraming.AutoZoom(Vector2.zero, pts), 1e-4f);
        }

        [Test]
        public void ZoomButtonsStepByTenthsAndStopAtTheLimits()
        {
            Assert.AreEqual(0.9f, CameraFraming.StepUser(1f, -1), 1e-4f);
            Assert.AreEqual(1.1f, CameraFraming.StepUser(1f, 1), 1e-4f);
            float z = 1f;
            for (int i = 0; i < 20; i++) z = CameraFraming.StepUser(z, -1);
            Assert.AreEqual(CameraFraming.MinUser, z, 1e-4f);
            for (int i = 0; i < 30; i++) z = CameraFraming.StepUser(z, 1);
            Assert.AreEqual(CameraFraming.MaxUser, z, 1e-4f);
        }

        [Test]
        public void TheRadarPutsEachSideOfThePitchOnTheMatchingSideAndTurnsWithTheCamera()
        {
            var size = new Vector2(135f, 200f);
            Assert.AreEqual(Vector2.zero, MiniRadar.Map(Vector3.zero, size, 1f));
            var corner = MiniRadar.Map(new Vector3(30f, 0f, 45f), size, 1f);
            Assert.AreEqual(size.x * 0.5f, corner.x, 1e-3f); Assert.AreEqual(size.y * 0.5f, corner.y, 1e-3f);
            var turned = MiniRadar.Map(new Vector3(30f, 0f, 45f), size, -1f);
            Assert.AreEqual(-corner.x, turned.x, 1e-3f); Assert.AreEqual(-corner.y, turned.y, 1e-3f);
        }

        [Test]
        public void PlayersRunFasterThanTheRealFiguresOnPurposeAndStayInTheAnimationRange()
        {
            var d = ScriptableObject.CreateInstance<PlayerData>();
            try
            {
                d.pace = 99; d.sprintSpeed = 99; d.acceleration = 99;
                Assert.AreEqual(4.5f * PlayerData.PaceScale, d.JogSpeed, 1e-3f);
                Assert.AreEqual(9.5f * PlayerData.PaceScale, d.SprintSpeedMs, 1e-3f);
                Assert.Greater(d.SprintSpeedMs, 11f, "a fast sprinter now covers 11+ m/s");
                Assert.Less(d.SprintSpeedMs, 12.5f, "inside the speeds the run animations cover");
                d.pace = 1; d.sprintSpeed = 1;
                Assert.Greater(d.JogSpeed, 3.9f); Assert.Greater(d.SprintSpeedMs, 7f);
            }
            finally { Object.DestroyImmediate(d); }
        }
    }
}
