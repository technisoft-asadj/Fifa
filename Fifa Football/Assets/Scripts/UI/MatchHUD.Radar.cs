using TMPro;
using UnityEngine;

namespace FootballSim.UI
{
    /// <summary>The pitch map (top left) and the camera zoom buttons under it.</summary>
    public partial class MatchHUD
    {
        MiniRadar radar;
        GameObject radarRoot;
        TextMeshProUGUI zoomLabel;
        float shownZoom = -1f;

        public MiniRadar Radar => radar;
        public GameObject RadarRoot => radarRoot;

        void BuildRadar(RectTransform root)
        {
            var holder = new GameObject("RadarGroup", typeof(RectTransform));
            holder.transform.SetParent(root, false);
            var hr = (RectTransform)holder.transform;
            hr.anchorMin = Vector2.zero; hr.anchorMax = Vector2.one; hr.offsetMin = hr.offsetMax = Vector2.zero;
            radarRoot = holder;
            radar = MiniRadar.Create(hr, new Vector2(135f, 200f), new Vector2(20f, -75f));
            // zoom buttons: closer (-) and wider (+), with the current level between them
            UIKit.SmallButton(hr, "-", UITheme.BlueDark, UIKit.TopLeft, new Vector2(20f, -285f), new Vector2(52f, 52f), () => ZoomCamera(-1), 36f);
            zoomLabel = UIKit.TextAt(hr, "ZoomLabel", "", 24f, Color.white, UIKit.TopLeft, new Vector2(76f, -291f), new Vector2(75f, 40f), TextAlignmentOptions.Center, true);
            UIKit.SmallButton(hr, "+", UITheme.BlueDark, UIKit.TopLeft, new Vector2(103f, -285f), new Vector2(52f, 52f), () => ZoomCamera(1), 36f);
            RefreshZoomLabel();
        }

        /// <summary>Camera closer (-1) or wider (+1).</summary>
        public void ZoomCamera(int direction)
        {
            var c = Cam(); if (c == null) return;
            c.StepZoom(direction);
            RefreshZoomLabel();
        }

        void RefreshZoomLabel()
        {
            if (zoomLabel == null) return;
            var c = Cam();
            float z = c == null ? 0f : c.UserZoom;
            if (z == shownZoom) return; // the wheel can change it too, so this is checked every frame but only rewritten when it moved
            shownZoom = z;
            // shown as magnification: closer means a bigger number
            zoomLabel.text = c == null ? "" : $"{100f / z:0}%";
        }
    }
}
