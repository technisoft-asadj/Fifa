using TMPro;
using UnityEngine;
using FootballSim.Core;

namespace FootballSim.UI
{
    /// <summary>
    /// Optional frame-rate readout (Settings > Show FPS, off by default). While on it also writes a one-line summary to the log every
    /// five seconds ("[perf] ..."), so numbers from a phone can be read over adb without screenshots.
    /// </summary>
    public partial class MatchHUD
    {
        const string ShowFpsKey = "show_fps";
        readonly FrameStats frameStats = new FrameStats();
        TextMeshProUGUI fpsText, settingsShowFps;
        bool showFps;
        float nextFpsRefresh, nextFpsLog;

        public bool FpsReadoutShown => showFps && fpsText != null && fpsText.gameObject.activeSelf;
        public string FpsReadoutText => fpsText != null ? fpsText.text : "";

        void BuildFpsReadout(RectTransform root)
        {
            fpsText = UIKit.TextAt(root, "FpsReadout", "", 24f, new Color(1f, 1f, 1f, 0.85f), UIKit.TopRight, new Vector2(-265f, -150f), new Vector2(480f, 36f), TextAlignmentOptions.Right, true); // under the pause / settings corner
            fpsText.raycastTarget = false;
            try { showFps = PlayerPrefs.GetInt(ShowFpsKey, 0) == 1; } catch { showFps = false; }
            fpsText.gameObject.SetActive(showFps);
        }

        public void ToggleShowFps()
        {
            showFps = !showFps;
            try { PlayerPrefs.SetInt(ShowFpsKey, showFps ? 1 : 0); PlayerPrefs.Save(); } catch { }
            if (fpsText != null) fpsText.gameObject.SetActive(showFps);
            frameStats.Reset();
            nextFpsRefresh = 0f; nextFpsLog = 0f;
            RefreshSettings();
        }

        void UpdatePerf()
        {
            if (!showFps || fpsText == null) return;
            frameStats.Add(Time.unscaledDeltaTime);
            float now = Time.unscaledTime;
            if (now >= nextFpsRefresh)
            {
                nextFpsRefresh = now + 0.5f;
                fpsText.text = $"{frameStats.AverageFps:0} fps   low {frameStats.LowFps:0}   worst {frameStats.WorstMs:0} ms";
            }
            if (now >= nextFpsLog)
            {
                nextFpsLog = now + 5f;
                int players = FindObjectsByType<FootballSim.Players.PlayerController>(FindObjectsSortMode.None).Length;
                Debug.Log($"[perf] avg {frameStats.AverageFps:0.0} fps, 1% low {frameStats.LowFps:0.0} fps, worst {frameStats.WorstMs:0.0} ms, frames {frameStats.Count}, players {players}, target {Application.targetFrameRate}, quality {QualitySettings.names[Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, QualitySettings.names.Length - 1)]}, {(MenuOpen ? "menu" : "match")}");
            }
        }
    }
}
