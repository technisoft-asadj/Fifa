using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FootballSim.UI
{
    /// <summary>Small motion for every button: it dips a little while pressed and springs back, so a tap always shows that it was received. Uses real time (menus run with the game paused).</summary>
    public sealed class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        const float Pressed = 0.94f;
        Vector3 rest; bool haveRest, down;

        void Capture() { if (!haveRest) { rest = transform.localScale; haveRest = true; } }
        public void OnPointerDown(PointerEventData e) { Capture(); down = true; enabled = true; }
        public void OnPointerUp(PointerEventData e) { down = false; }
        public void OnPointerExit(PointerEventData e) { down = false; }
        void OnEnable() { haveRest = false; }
        void OnDisable() { if (haveRest) transform.localScale = rest; down = false; }

        void Update()
        {
            if (!haveRest) return;
            var target = down ? rest * Pressed : rest;
            transform.localScale = Vector3.Lerp(transform.localScale, target, 1f - Mathf.Exp(-24f * Time.unscaledDeltaTime));
        }
    }

    /// <summary>Fades a screen in over a fifth of a second whenever it is switched on. Real time, so it also works while the match is paused.</summary>
    public sealed class PanelFade : MonoBehaviour
    {
        CanvasGroup group; float t; bool done;
        const float Seconds = 0.2f;

        void Awake() { group = GetComponent<CanvasGroup>(); if (group == null) group = gameObject.AddComponent<CanvasGroup>(); }
        void OnEnable() { if (group == null) Awake(); t = 0f; done = false; group.alpha = 0f; }
        void Update()
        {
            if (done) return;
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Seconds);
            group.alpha = 1f - (1f - k) * (1f - k);
            if (k >= 1f) { group.alpha = 1f; done = true; }
        }
        void OnDisable() { if (group != null) group.alpha = 1f; }
    }

    /// <summary>A circular arrow that turns while it is shown: the "working on it" mark for loading and matchmaking.</summary>
    public sealed class Spinner : MonoBehaviour
    {
        public float degreesPerSecond = -300f;
        void Update() { transform.Rotate(0f, 0f, degreesPerSecond * Time.unscaledDeltaTime); }
    }

    public static class UIFx
    {
        /// <summary>Gives every button under <paramref name="root"/> the press motion (once).</summary>
        public static void WireButtons(Transform root)
        {
            foreach (var b in root.GetComponentsInChildren<Button>(true))
                if (b.GetComponent<PressFeedback>() == null) b.gameObject.AddComponent<PressFeedback>();
        }

        /// <summary>Makes a screen fade in each time it is shown.</summary>
        public static void FadeIn(GameObject screen)
        {
            if (screen != null && screen.GetComponent<PanelFade>() == null) screen.AddComponent<PanelFade>();
        }
    }
}
