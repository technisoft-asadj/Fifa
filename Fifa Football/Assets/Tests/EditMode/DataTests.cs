using NUnit.Framework;
using UnityEngine;
using FootballSim.Core;
using FootballSim.FootballAI;
using FootballSim.Inputs;
using FootballSim.Players;

namespace FootballSim.Tests
{
    public class DataTests
    {
        PlayerData Make(int v)
        {
            var d = ScriptableObject.CreateInstance<PlayerData>();
            d.pace = d.acceleration = d.sprintSpeed = d.shooting = d.passing = d.agility = v;
            return d;
        }

        [Test]
        public void HigherAttributesGiveFasterPlayers()
        {
            var low = Make(20); var high = Make(95);
            Assert.Greater(high.SprintSpeedMs, low.SprintSpeedMs);
            Assert.Greater(high.JogSpeed, low.JogSpeed);
            Assert.Greater(high.AccelerationMs2, low.AccelerationMs2);
            Assert.Greater(high.TurnRateDegPerSec, low.TurnRateDegPerSec);
        }

        [Test]
        public void HigherSkillMeansStrongerAndMoreAccurateStrikes()
        {
            var low = Make(20); var high = Make(95);
            Assert.Greater(high.MaxShotSpeed, low.MaxShotSpeed);
            Assert.Greater(high.MaxPassSpeed, low.MaxPassSpeed);
            Assert.Less(high.ShotInaccuracyDeg, low.ShotInaccuracyDeg);
            Assert.Less(high.PassInaccuracyDeg, low.PassInaccuracyDeg);
        }

        [Test]
        public void SprintIsFasterThanJog()
        {
            var d = Make(70);
            Assert.Greater(d.SprintSpeedMs, d.JogSpeed);
        }

        [Test]
        public void Default442HasElevenSlotsWithGoalkeeperFirst()
        {
            var slots = FormationData.Default442();
            Assert.AreEqual(11, slots.Length);
            Assert.Less(slots[0].y, 0.1f, "slot 0 must be the goalkeeper near own goal line");
            foreach (var s in slots)
            {
                Assert.GreaterOrEqual(s.x, -1f); Assert.LessOrEqual(s.x, 1f);
                Assert.GreaterOrEqual(s.y, 0f); Assert.LessOrEqual(s.y, 1f);
            }
        }

        [Test]
        public void Default442HasFourDefendersFourMidfieldersTwoForwards()
        {
            var slots = FormationData.Default442();
            int def = 0, mid = 0, fwd = 0;
            for (int i = 1; i < slots.Length; i++)
            {
                if (slots[i].y < 0.4f) def++; else if (slots[i].y < 0.7f) mid++; else fwd++;
            }
            Assert.AreEqual(4, def); Assert.AreEqual(4, mid); Assert.AreEqual(2, fwd);
        }

        [Test]
        public void PitchDimensionsAreRealistic()
        {
            Assert.That(Pitch.HalfLength * 2, Is.InRange(90f, 120f));
            Assert.That(Pitch.HalfWidth * 2, Is.InRange(45f, 90f));
            Assert.AreEqual(7.32f, Pitch.GoalHalfWidth * 2, 0.01f);
        }

        [Test]
        public void AIInputOneShotActionsAreConsumedOnce()
        {
            var ai = new AIInputSource();
            ai.QueueShoot(Vector3.forward, 0.8f);
            Assert.IsTrue(ai.ShootReleased);
            Assert.IsFalse(ai.ShootReleased, "shoot must fire once");
            ai.QueueTackle();
            Assert.IsTrue(ai.TacklePressed);
            Assert.IsFalse(ai.TacklePressed);
            ai.QueuePass(Vector3.right, 0.5f);
            Assert.AreEqual(0.5f, ai.ActionPower);
            Assert.IsTrue(ai.PassPressed);
            Assert.IsFalse(ai.PassPressed);
        }

        [Test]
        public void AIInputClearResetsEverything()
        {
            var ai = new AIInputSource { MoveVector = Vector2.one, SprintFlag = true };
            ai.QueuePass(Vector3.forward, 1f);
            ai.Clear();
            Assert.AreEqual(Vector2.zero, ai.Move);
            Assert.IsFalse(ai.Sprint);
            Assert.IsFalse(ai.PassPressed);
            Assert.AreEqual(0f, ai.ActionPower);
        }

        [Test]
        public void AllFormationPresetsAreValid()
        {
            Assert.AreEqual(5, FormationData.PresetNames.Length);
            for (int p = 0; p < FormationData.PresetNames.Length; p++)
            {
                var slots = FormationData.Slots(p);
                Assert.AreEqual(11, slots.Length, FormationData.PresetNames[p]);
                Assert.Less(slots[0].y, 0.1f, FormationData.PresetNames[p] + " slot 0 must be GK");
                int def = 0, mid = 0, fwd = 0;
                for (int i = 1; i < slots.Length; i++)
                {
                    Assert.That(slots[i].x, Is.InRange(-1f, 1f)); Assert.That(slots[i].y, Is.InRange(0f, 1f));
                    if (slots[i].y < 0.4f) def++; else if (slots[i].y < 0.7f) mid++; else fwd++;
                }
                Assert.AreEqual(10, def + mid + fwd);
                var parts = FormationData.PresetNames[p].Split('-');
                Assert.AreEqual(int.Parse(parts[0]), def, FormationData.PresetNames[p] + " defenders");
            }
        }

        [Test]
        public void HarderDifficultiesReactFasterAndAimBetter()
        {
            var prev = AiTuning.For(Difficulty.Beginner);
            foreach (Difficulty d in new[] { Difficulty.Amateur, Difficulty.Professional, Difficulty.WorldClass, Difficulty.Legendary })
            {
                var t = AiTuning.For(d);
                Assert.Less(t.reactionDelay, prev.reactionDelay, d + " reaction");
                Assert.Less(t.aimErrorDeg, prev.aimErrorDeg, d + " aim error");
                Assert.Greater(t.shootRange, prev.shootRange, d + " shoot range");
                Assert.Less(t.tackleCooldown, prev.tackleCooldown, d + " tackle cooldown");
                prev = t;
            }
        }
    }
}
