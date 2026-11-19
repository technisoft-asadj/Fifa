using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using FootballSim.Players;
using FootballSim.Inputs;

namespace FootballSim.Tests
{
    /// <summary>A runner with the ball and nobody else on the pitch: the ball must stay close to his feet at jog and at sprint.</summary>
    public class DribbleTests
    {
        const string SceneName = "FootballPrototype";

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            var keep = SceneManager.CreateScene("DribbleTestsEmpty");
            SceneManager.SetActiveScene(keep);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var sc = SceneManager.GetSceneAt(i);
                if (sc != keep && sc.isLoaded && sc.name == SceneName) yield return SceneManager.UnloadSceneAsync(sc);
            }
            yield return SceneManager.UnloadSceneAsync(keep);
        }

        IEnumerator Run(bool sprint, float push, float seconds, System.Action<float, float, float> report)
        {
            FootballSim.UI.MatchHUD.SplashShown = true;
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            yield return null;
            Time.timeScale = 1f;
            var hud = Object.FindFirstObjectByType<FootballSim.UI.MatchHUD>();
            hud.BeginMatch();
            for (int i = 0; i < 30; i++) yield return null;
            var ball = Object.FindFirstObjectByType<FootballSim.Ball.BallController>();
            var human = System.Array.Find(Object.FindObjectsByType<FootballSim.FootballAI.TeamController>(FindObjectsSortMode.None), t => t.humanControlled);
            var pc = human.Controlled;
            foreach (var other in Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None)) if (other != pc) other.enabled = false;
            foreach (var t in Object.FindObjectsByType<FootballSim.FootballAI.TeamController>(FindObjectsSortMode.None)) t.enabled = false;
            var ai = new AIInputSource();
            pc.SetInput(ai, true);
            pc.TeleportTo(new Vector3(0f, 1f, -40f), Quaternion.LookRotation(Vector3.forward));
            ball.Freeze(false); // the match holds the ball still for the kick-off
            ball.ResetTo(pc.transform.position + new Vector3(0f, -0.77f, 0.8f));
            yield return null;
            ai.MoveVector = new Vector2(0f, push); ai.SprintFlag = sprint;
            float end = Time.time + seconds, sum = 0f, max = 0f; int frames = 0, near = 0;
            while (Time.time < end)
            {
                yield return null;
                if (ball.Body.isKinematic) ball.Freeze(false);
                Vector3 d = ball.Position - pc.transform.position; d.y = 0f;
                float dist = d.magnitude;
                sum += dist; frames++; if (dist > max) max = dist; if (dist < 1.6f) near++;
                if (frames % 30 == 1) Debug.Log($"[dribble-trace] t={frames / 60f:0.0}s player speed {pc.Velocity.magnitude:0.0} ball speed {ball.Velocity.magnitude:0.0} gap {dist:0.00} playerZ {pc.transform.position.z:0.0} ballZ {ball.Position.z:0.0} lastToucher={(ball.LastToucher == pc)}");
            }
            report(sum / frames, max, (float)near / frames);
        }

        [UnityTest]
        public IEnumerator TheBallStaysAtTheFeetOfASprintingRunner()
        {
            float avg = 0f, max = 0f, near = 0f;
            yield return Run(true, 1f, 6f, (a, m, n) => { avg = a; max = m; near = n; });
            Debug.Log($"[dribble] sprint: average gap {avg:0.00} m, largest {max:0.00} m, within 1.6 m {near * 100f:0}% of the time");
            Assert.Greater(near, 0.85f, "the ball stays within reach while sprinting");
            Assert.Less(max, 3.5f);
        }

        [UnityTest]
        public IEnumerator TheBallStaysAtTheFeetOfAJoggingRunner()
        {
            float avg = 0f, max = 0f, near = 0f;
            yield return Run(false, 0.6f, 6f, (a, m, n) => { avg = a; max = m; near = n; });
            Debug.Log($"[dribble] jog: average gap {avg:0.00} m, largest {max:0.00} m, within 1.6 m {near * 100f:0}% of the time");
            Assert.Greater(near, 0.85f, "the ball stays within reach while jogging");
            Assert.Less(max, 3.5f);
        }
    }
}
