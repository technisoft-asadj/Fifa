using UnityEngine;

namespace FootballSim.Core
{
    /// <summary>
    /// App-wide startup settings. Unity caps mobile players at 30 fps unless told otherwise
    /// (measured on device: a locked 33.5 ms frame), so the target is set explicitly here.
    /// </summary>
    public static class AppBootstrap
    {
        public const int TargetFps = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFps;
            Screen.sleepTimeout = SleepTimeout.NeverSleep; // a match must not dim the screen
        }
    }
}
