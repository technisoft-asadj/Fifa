using System.Collections.Generic;
using FootballSim.Core;
using NUnit.Framework;
using UnityEngine;

namespace FootballSim.Tests
{
    public class TouchLayoutTests
    {
        static readonly Vector2 Wide = new Vector2(2400f, 1080f), Squarish = new Vector2(1440f, 1080f);

        [Test]
        public void ADraggedButtonStaysFullyOnTheScreenAtAnyAspect()
        {
            foreach (var area in new[] { Wide, Squarish })
                foreach (TouchButtonId id in System.Enum.GetValues(typeof(TouchButtonId)))
                {
                    float size = TouchLayout.DefaultSize(id), half = size * 0.5f;
                    foreach (var wanted in new[] { new Vector2(500f, -500f), new Vector2(-9999f, 9999f), new Vector2(-1f, 1f), new Vector2(-300f, 300f) })
                    {
                        var p = TouchLayout.Clamp(wanted, size, area);
                        Assert.GreaterOrEqual(p.x, -(area.x - half) - 1e-3f, $"{id} left edge");
                        Assert.LessOrEqual(p.x, -half + 1e-3f, $"{id} right edge");
                        Assert.GreaterOrEqual(p.y, half - 1e-3f, $"{id} bottom edge");
                        Assert.LessOrEqual(p.y, area.y - half + 1e-3f, $"{id} top edge");
                    }
                }
        }

        [Test]
        public void TheDefaultLayoutFitsBothScreensAndNoButtonsOverlap()
        {
            var ids = (TouchButtonId[])System.Enum.GetValues(typeof(TouchButtonId));
            foreach (var area in new[] { Wide, Squarish })
                foreach (var id in ids)
                {
                    var d = TouchLayout.DefaultPosition(id);
                    Assert.AreEqual(d, TouchLayout.Clamp(d, TouchLayout.DefaultSize(id), area), $"{id} default already inside {area}");
                }
            for (int i = 0; i < ids.Length; i++)
                for (int j = i + 1; j < ids.Length; j++)
                    Assert.IsFalse(TouchLayout.Overlaps(TouchLayout.DefaultPosition(ids[i]), TouchLayout.DefaultSize(ids[i]), TouchLayout.DefaultPosition(ids[j]), TouchLayout.DefaultSize(ids[j])),
                        $"{ids[i]} and {ids[j]} overlap by default");
        }

        [Test]
        public void OverlapNeedsCentresCloserThanTheTwoRadii()
        {
            Assert.IsTrue(TouchLayout.Overlaps(new Vector2(0, 0), 100f, new Vector2(90, 0), 100f));
            Assert.IsFalse(TouchLayout.Overlaps(new Vector2(0, 0), 100f, new Vector2(100, 0), 100f));
        }

        [Test]
        public void ASavedLayoutComesBackExactly()
        {
            var moved = new Dictionary<TouchButtonId, Vector2>
            {
                { TouchButtonId.Shoot, new Vector2(-250.5f, 300f) },
                { TouchButtonId.Sprint, new Vector2(-800f, 120.5f) },
            };
            var back = TouchLayout.Parse(TouchLayout.Serialize(moved));
            Assert.AreEqual(2, back.Count);
            Assert.AreEqual(moved[TouchButtonId.Shoot], back[TouchButtonId.Shoot]);
            Assert.AreEqual(moved[TouchButtonId.Sprint], back[TouchButtonId.Sprint]);
        }

        [Test]
        public void ADamagedSavedLayoutIsIgnoredPieceByPiece()
        {
            Assert.AreEqual(0, TouchLayout.Parse(null).Count);
            Assert.AreEqual(0, TouchLayout.Parse("").Count);
            Assert.AreEqual(0, TouchLayout.Parse("garbage").Count);
            var p = TouchLayout.Parse("Shoot:-200,200;Nope:1,2;Pass:abc,3;Tackle:5;Switch:NaN,4;Sprint:-600,90");
            Assert.AreEqual(2, p.Count, "only the two readable entries survive");
            Assert.AreEqual(new Vector2(-200f, 200f), p[TouchButtonId.Shoot]);
            Assert.AreEqual(new Vector2(-600f, 90f), p[TouchButtonId.Sprint]);
        }

        [Test]
        public void SensitivityScalesTheStickAndNeverExceedsFull()
        {
            Assert.AreEqual(0.5f, TouchTuning.Shape(new Vector2(0.5f, 0f), 1f).magnitude, 1e-4f);
            Assert.AreEqual(0.75f, TouchTuning.Shape(new Vector2(0.5f, 0f), 1.5f).magnitude, 1e-4f);
            Assert.AreEqual(1f, TouchTuning.Shape(new Vector2(0.9f, 0.4f), 1.6f).magnitude, 1e-4f, "capped at full");
            Assert.Less(TouchTuning.Shape(new Vector2(0.5f, 0f), 0.6f).magnitude, 0.5f);
            Assert.AreEqual(TouchTuning.MaxSensitivity, TouchTuning.ClampSensitivity(9f));
            Assert.AreEqual(TouchTuning.MinSensitivity, TouchTuning.ClampSensitivity(0f));
        }

        [Test]
        public void ANearlyFullPushSprintsAndEasingOffWellBelowItStops()
        {
            Assert.IsFalse(TouchTuning.AutoSprint(false, 0.5f), "a light push jogs");
            Assert.IsFalse(TouchTuning.AutoSprint(false, 0.8f), "not yet");
            Assert.IsTrue(TouchTuning.AutoSprint(false, 0.9f), "a nearly full push sprints");
            Assert.IsTrue(TouchTuning.AutoSprint(true, 0.7f), "once sprinting, a thumb that sags a little keeps sprinting");
            Assert.IsFalse(TouchTuning.AutoSprint(true, 0.4f), "easing right off goes back to a jog");
            Assert.IsFalse(TouchTuning.AutoSprint(true, 0f));
        }

        [Test]
        public void SensitivityKeepsTheDirectionOfTheThumb()
        {
            var v = TouchTuning.Shape(new Vector2(0.3f, 0.3f), 1.4f);
            Assert.AreEqual(v.x, v.y, 1e-5f);
            Assert.Greater(v.x, 0.3f);
        }
    }
}
