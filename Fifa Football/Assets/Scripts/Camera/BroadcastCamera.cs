using System.Collections.Generic;
using UnityEngine;
using FootballSim.Core;
using FootballSim.Players;

namespace FootballSim.CameraSystem
{
    /// <summary>
    /// Smooth broadcast camera that tracks a blend of the ball and the controlled player. It also zooms like an operator following play:
    /// closer when the ball and the players around it are tight, wider when they spread out so the nearby players on every side stay in
    /// view. The player can nudge the zoom, and can ask it to look at one player for a few seconds (tap on the radar).
    /// </summary>
    public class BroadcastCamera : MonoBehaviour
    {
        [SerializeField] Transform ball;
        [SerializeField] Transform player;
        [SerializeField] float height = 18f;
        [SerializeField] float distance = 22f;
        [SerializeField] float smoothTime = 0.35f;
        [SerializeField, Range(0f, 1f)] float ballWeight = 0.7f;
        [SerializeField] Vector2 pitchHalfExtents = new Vector2(30f, 45f);

        Vector3 vel;
        float viewSign = 1f;

        public const string PrefKey = "camera_preset", PrefZoom = "camera_zoom";
        /// <summary>Camera views the player can pick: (height, distance). The first is the default broadcast view.</summary>
        public static readonly string[] PresetNames = { "Broadcast", "Close", "Wide", "High" };
        static readonly Vector2[] PresetValues = { new Vector2(18f, 22f), new Vector2(11f, 14f), new Vector2(26f, 32f), new Vector2(34f, 12f) };
        public int Preset { get; private set; }
        public float Height => height;
        public float Distance => distance;

        // ---- zoom ----
        /// <summary>The player's own zoom preference (below 1 closer, above 1 wider), saved.</summary>
        public float UserZoom { get; private set; } = 1f;
        /// <summary>Whether the camera pulls in and out by itself to keep the nearby players in view.</summary>
        public bool autoFrame = true;
        float autoZoom = 1f, autoZoomVel;
        /// <summary>The zoom in use now: automatic framing times the player's preference.</summary>
        public float Zoom => Mathf.Clamp(autoZoom * UserZoom, 0.4f, 2.2f);

        Transform focusTarget;
        float focusUntil;
        PlayerController[] all = new PlayerController[0];
        float nextRefresh;
        Transform cachedFor; PlayerController cachedPc;
        readonly List<Vector2> points = new List<Vector2>(8);
        readonly float[] oppD = new float[3]; readonly Vector2[] oppP = new Vector2[3];
        readonly float[] mateD = new float[2]; readonly Vector2[] mateP = new Vector2[2];
        const float NearRange = 22f;

        void Awake()
        {
            try { SetPreset(PlayerPrefs.GetInt(PrefKey, 0), false); UserZoom = CameraFraming.ClampUser(PlayerPrefs.GetFloat(PrefZoom, 1f)); } catch { }
        }

        /// <summary>Choose a camera view (wraps around). The camera glides to it; the choice is saved.</summary>
        public void SetPreset(int index, bool save = true)
        {
            Preset = ((index % PresetValues.Length) + PresetValues.Length) % PresetValues.Length;
            height = PresetValues[Preset].x; distance = PresetValues[Preset].y;
            if (save) { try { PlayerPrefs.SetInt(PrefKey, Preset); PlayerPrefs.Save(); } catch { } }
        }

        /// <summary>Closer (-1) or wider (+1) by a tenth; saved.</summary>
        public void StepZoom(int direction) { SetUserZoom(CameraFraming.StepUser(UserZoom, direction)); }

        public void SetUserZoom(float z, bool save = true)
        {
            UserZoom = CameraFraming.ClampUser(z);
            if (save) { try { PlayerPrefs.SetFloat(PrefZoom, UserZoom); PlayerPrefs.Save(); } catch { } }
        }

        /// <summary>Look at one player (or anything) for a few seconds, close up, then go back to following play.</summary>
        public void FocusOn(Transform target, float seconds)
        {
            focusTarget = target; focusUntil = Time.unscaledTime + seconds;
        }

        public void ClearFocus() { focusTarget = null; }
        public bool Focusing => focusTarget != null && Time.unscaledTime < focusUntil;
        public Transform FocusTarget => Focusing ? focusTarget : null;

        /// <summary>+1 = looking toward +Z (default); -1 = looking toward -Z after the teams change ends. Snaps so players see the new view at once.</summary>
        public void SetViewSign(float sign)
        {
            viewSign = sign;
            vel = Vector3.zero;
            if (ball == null) return;
            Vector3 focus = ball.position; focus.y = 0f;
            transform.position = focus + new Vector3(0f, height * Zoom, -distance * Zoom * viewSign);
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }
        public float ViewSign => viewSign;

        /// <summary>Set the player the camera blends toward (called when control switches).</summary>
        public void SetPlayer(Transform p) { player = p; }

        public Transform Ball => ball;
        public Transform Player => player;

        /// <summary>Points the camera at a different ball and player (a guest watching a network view). Null keeps the current one.</summary>
        public void SetTargets(Transform newBall, Transform newPlayer)
        {
            if (newBall != null) ball = newBall;
            player = newPlayer;
        }

        // keep the nearest few players on each side in view: 3 opponents and 2 team-mates within NearRange of the controlled player
        float AutoZoomFor(Vector3 focus)
        {
            if (player == null) return 1f;
            if (cachedFor != player) { cachedFor = player; cachedPc = player.GetComponent<PlayerController>(); }
            if (cachedPc == null) return 1f; // a guest's plain capsules: no local players to frame
            if (Time.unscaledTime >= nextRefresh) { all = FindObjectsByType<PlayerController>(FindObjectsSortMode.None); nextRefresh = Time.unscaledTime + 1f; }
            for (int i = 0; i < oppD.Length; i++) oppD[i] = float.MaxValue;
            for (int i = 0; i < mateD.Length; i++) mateD[i] = float.MaxValue;
            Vector2 me = new Vector2(player.position.x, player.position.z);
            foreach (var p in all)
            {
                if (p == null || p == cachedPc || !p.isActiveAndEnabled) continue;
                Vector2 pos = new Vector2(p.transform.position.x, p.transform.position.z);
                float d = Vector2.Distance(me, pos);
                if (d > NearRange) continue;
                if (p.TeamId == cachedPc.TeamId) Insert(mateD, mateP, d, pos); else Insert(oppD, oppP, d, pos);
            }
            points.Clear();
            points.Add(new Vector2(ball.position.x, ball.position.z));
            points.Add(me);
            for (int i = 0; i < oppD.Length; i++) if (oppD[i] < float.MaxValue) points.Add(oppP[i]);
            for (int i = 0; i < mateD.Length; i++) if (mateD[i] < float.MaxValue) points.Add(mateP[i]);
            return CameraFraming.AutoZoom(new Vector2(focus.x, focus.z), points);
        }

        static void Insert(float[] ds, Vector2[] ps, float d, Vector2 pos)
        {
            for (int i = 0; i < ds.Length; i++)
            {
                if (d >= ds[i]) continue;
                for (int j = ds.Length - 1; j > i; j--) { ds[j] = ds[j - 1]; ps[j] = ps[j - 1]; }
                ds[i] = d; ps[i] = pos;
                return;
            }
        }

        void Update()
        {
            // mouse wheel zoom on a computer; phones use the zoom buttons next to the radar
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
            {
                float wheel = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > 0.1f) StepZoom(wheel > 0f ? -1 : 1);
            }
        }

        void LateUpdate()
        {
            if (ball == null) return;
            Vector3 focus = ball.position;
            if (player) focus = Vector3.Lerp(player.position, ball.position, ballWeight);
            float targetZoom = autoFrame ? AutoZoomFor(focus) : 1f;
            if (focusTarget != null)
            {
                if (Time.unscaledTime < focusUntil) { focus = focusTarget.position; targetZoom = CameraFraming.FocusZoom; }
                else focusTarget = null;
            }
            autoZoom = Mathf.SmoothDamp(autoZoom, targetZoom, ref autoZoomVel, 0.6f, float.MaxValue, Time.unscaledDeltaTime);
            focus.x = Mathf.Clamp(focus.x, -pitchHalfExtents.x, pitchHalfExtents.x);
            focus.z = Mathf.Clamp(focus.z, -pitchHalfExtents.y, pitchHalfExtents.y);
            focus.y = 0f;

            float z = Zoom;
            Vector3 desired = focus + new Vector3(0f, height * z, -distance * z * viewSign);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref vel, smoothTime);
            Quaternion look = Quaternion.LookRotation(focus - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 1f - Mathf.Exp(-6f * Time.deltaTime));
        }
    }
}
