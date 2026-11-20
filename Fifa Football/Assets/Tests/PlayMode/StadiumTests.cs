using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using FootballSim.Venue;

namespace FootballSim.Tests
{
    public class StadiumTests
    {
        IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("FootballPrototype", LoadSceneMode.Single);
            yield return null; yield return null;
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator TheGroundIsBuiltAroundThePitchWithFansAndBenches()
        {
            yield return Load();
            var s = Stadium.Current;
            Assert.IsNotNull(s, "a stadium surrounds the pitch");
            Assert.Greater(s.PeopleCount, 1000, "the stands are full of fans");
            Assert.AreEqual(14, s.BenchPeopleCount, "seven people on each bench");
            Assert.Greater(s.CrowdGroupCount, 4);
        }

        [UnityTest]
        public IEnumerator AGoalMakesTheCrowdCheerAndTheBenchesReact()
        {
            yield return Load();
            var match = Object.FindFirstObjectByType<FootballSim.Match.MatchManager>();
            Assert.IsFalse(Stadium.Current.CrowdCheering);
            match.OnGoal(0);
            yield return null;
            Assert.IsTrue(Stadium.Current.CrowdCheering, "the fans leap up when a goal goes in");
        }

        [UnityTest]
        public IEnumerator TheBenchPeopleSitAndTheCoachStandsBesideThePitch()
        {
            yield return Load();
            for (int i = 0; i < 30; i++) yield return null;
            var bench = GameObject.Find("HomeBench").GetComponent<BenchCrew>();
            foreach (var p in bench.people)
                Assert.Less(p.transform.Find("Pelvis").localPosition.y, 0.7f, "seated, not standing");
            var coach = GameObject.Find("HomeCoach");
            Assert.IsNotNull(coach);
            Assert.Greater(coach.transform.position.x, 34f, "outside the touchline");
        }
    }
}
