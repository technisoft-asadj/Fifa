using UnityEngine;

namespace FootballSim.Core
{
    /// <summary>
    /// Match time for gameplay timers (AI think delay, pass-receiver window, offside snapshot, assist window). UnityEngine.Time.time
    /// is an absolute float that depends on how long the editor or app has been running, so the same timer rounds differently
    /// in two otherwise identical runs and shifts decisions by a frame. This clock starts at zero with every match scene and
    /// only advances by the frame time, in double precision.
    /// </summary>
    public static class SimClock
    {
        public static double Now { get; private set; }
        public static void Reset() { Now = 0.0; }
        public static void Advance(float dt) { Now += dt; }
    }

    /// <summary>Advances <see cref="SimClock"/> before any other script runs its Update.</summary>
    [DefaultExecutionOrder(-10000)]
    public class SimClockDriver : MonoBehaviour
    {
        static SimClockDriver instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (instance != null) return;
            var go = new GameObject("SimClock") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SimClockDriver>();
        }

        void OnEnable() { UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded; }
        void OnDisable() { UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded; }
        static void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m) { SimClock.Reset(); }
        void Update() { SimClock.Advance(Time.deltaTime); }
    }
}
