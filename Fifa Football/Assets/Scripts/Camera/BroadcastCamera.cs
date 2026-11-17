using UnityEngine;

namespace FootballSim.CameraSystem
{
    /// <summary>Smooth broadcast camera that tracks a blend of the ball and the controlled player.</summary>
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

        public const string PrefKey = "camera_preset";
        /// <summary>Camera views the player can pick: (height, distance). The first is the default broadcast view.</summary>
        public static readonly string[] PresetNames = { "Broadcast", "Close", "Wide", "High" };
        static readonly Vector2[] PresetValues = { new Vector2(18f, 22f), new Vector2(11f, 14f), new Vector2(26f, 32f), new Vector2(34f, 12f) };
        public int Preset { get; private set; }
        public float Height => height;
        public float Distance => distance;

        void Awake()
        {
            try { SetPreset(PlayerPrefs.GetInt(PrefKey, 0), false); } catch { }
        }

        /// <summary>Choose a camera view (wraps around). The camera glides to it; the choice is saved.</summary>
        public void SetPreset(int index, bool save = true)
        {
            Preset = ((index % PresetValues.Length) + PresetValues.Length) % PresetValues.Length;
            height = PresetValues[Preset].x; distance = PresetValues[Preset].y;
            if (save) { try { PlayerPrefs.SetInt(PrefKey, Preset); PlayerPrefs.Save(); } catch { } }
        }

        /// <summary>+1 = looking toward +Z (default); -1 = looking toward -Z after the teams change ends. Snaps so players see the new view at once.</summary>
        public void SetViewSign(float sign)
        {
            viewSign = sign;
            vel = Vector3.zero;
            if (ball == null) return;
            Vector3 focus = ball.position; focus.y = 0f;
            transform.position = focus + new Vector3(0f, height, -distance * viewSign);
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

        void LateUpdate()
        {
            if (ball == null) return;
            Vector3 focus = ball.position;
            if (player) focus = Vector3.Lerp(player.position, ball.position, ballWeight);
            focus.x = Mathf.Clamp(focus.x, -pitchHalfExtents.x, pitchHalfExtents.x);
            focus.z = Mathf.Clamp(focus.z, -pitchHalfExtents.y, pitchHalfExtents.y);
            focus.y = 0f;

            Vector3 desired = focus + new Vector3(0f, height, -distance * viewSign);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref vel, smoothTime);
            Quaternion look = Quaternion.LookRotation(focus - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 1f - Mathf.Exp(-6f * Time.deltaTime));
        }
    }
}
