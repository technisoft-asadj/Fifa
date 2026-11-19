using UnityEngine;
using FootballSim.Players;

namespace FootballSim.Net
{
    /// <summary>
    /// Draws what a <see cref="NetClient"/> has received: 22 capsules and a ball, placed from the interpolation buffer a fixed number of
    /// ticks behind the newest snapshot. It reads no simulation objects, only snapshots, which is what a real remote player's screen would do.
    /// Bodies are plain capsules unless the player models setting asks for the animated body (and its files exist): then the same
    /// speed-driven locomotion plays from the snapshot's speed, and the host's kicks, tackles, dives, falls and celebrations play when the
    /// snapshot's action counter changes. The render point follows the newest arrival so it jitters when packets do.
    /// </summary>
    public class NetClientView : MonoBehaviour
    {
        public NetClient client;
        /// <summary>How many server ticks behind the newest snapshot to draw. 6 ticks is 100 ms at 60 ticks per second.</summary>
        public float delayTicks = 6f;
        /// <summary>When true the delay comes from the client's measured arrival jitter (never below 6 ticks) and <see cref="delayTicks"/> is ignored.</summary>
        public bool adaptive = true;

        Transform[] bodies;
        Transform[] capsules;
        AnimatedPlayerVisual[] animated;
        bool[] animatedFailed;
        int[] lastSerial;
        Transform ballBody;
        /// <summary>-1 follows the saved player models setting; 0, 1 or 2 (Off, One, All) overrides it (tests).</summary>
        public int modeOverride = -1;
        readonly WorldSnapshot view = new WorldSnapshot();
        readonly RenderClock clock = new RenderClock();

        public Transform Ball => ballBody;
        public Transform[] Bodies => bodies;
        public WorldSnapshot Latest => view;
        /// <summary>How many body actions this view has started on animated bodies.</summary>
        public int ActionsPlayed { get; private set; }

        /// <summary>The animated body of a slot, or null while that slot shows its capsule.</summary>
        public AnimatedPlayerVisual AnimatedBody(int i)
        {
            return animated != null && i >= 0 && i < animated.Length && animated[i] != null && animated[i].IsBuilt && animated[i].Model.activeSelf ? animated[i] : null;
        }

        void Awake()
        {
            bodies = new Transform[WorldSnapshot.PlayerCount];
            capsules = new Transform[bodies.Length];
            animated = new AnimatedPlayerVisual[bodies.Length];
            animatedFailed = new bool[bodies.Length];
            lastSerial = new int[bodies.Length];
            for (int i = 0; i < bodies.Length; i++)
            {
                lastSerial[i] = -1;
                var root = new GameObject("NetPlayer" + i);
                root.transform.SetParent(transform, false);
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = "Capsule";
                Destroy(go.GetComponent<Collider>()); // a display only: it must not touch the real match
                go.transform.SetParent(root.transform, false);
                var r = go.GetComponent<Renderer>();
                if (r != null) r.material.color = KitOf(i);
                bodies[i] = root.transform;
                capsules[i] = go.transform;
            }
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "NetBall";
            Destroy(ball.GetComponent<Collider>());
            ball.transform.SetParent(transform, false);
            ball.transform.localScale = Vector3.one * 0.22f;
            ballBody = ball.transform;
        }

        static Color KitOf(int i) { return i < 11 ? new Color(0.85f, 0.15f, 0.15f) : new Color(0.15f, 0.3f, 0.85f); }

        /// <summary>Shows the animated body where the models setting asks for it (the whole team set, or just the player the server gave this client).</summary>
        void ApplyModels()
        {
            var mode = modeOverride >= 0 ? (PlayerVisual.ModelMode)modeOverride : PlayerVisual.Mode;
            int mine = client != null && client.Joined ? client.PlayerIndex : -1;
            for (int i = 0; i < bodies.Length; i++)
            {
                bool want = mode == PlayerVisual.ModelMode.All || (mode == PlayerVisual.ModelMode.One && i == mine);
                if (want && animated[i] == null && !animatedFailed[i])
                {
                    var a = bodies[i].gameObject.AddComponent<AnimatedPlayerVisual>();
                    if (a.Build(null, KitOf(i)))
                    {
                        a.Model.transform.localPosition = new Vector3(0f, -1f, 0f); // the body root is the capsule centre, 1 m above the grass
                        animated[i] = a;
                    }
                    else { Destroy(a); animatedFailed[i] = true; } // no prefab: the capsule stays
                }
                bool on = want && animated[i] != null && animated[i].IsBuilt;
                if (animated[i] != null && animated[i].Model.activeSelf != on) animated[i].Model.SetActive(on);
                capsules[i].gameObject.SetActive(!on);
            }
        }

        void LateUpdate()
        {
            if (client == null || client.Buffer.Count == 0) return;
            float delay = adaptive ? client.Delay.Ticks : delayTicks;
            float t = clock.Step(Time.unscaledDeltaTime, client.Buffer.NewestTick - delay);
            if (!client.Buffer.Sample(t, view)) return;
            ApplyModels();
            for (int i = 0; i < bodies.Length; i++)
            {
                var p = view.players[i];
                bodies[i].localPosition = new Vector3(p.x, 1f, p.z);
                bodies[i].localRotation = Quaternion.Euler(0f, p.yaw, 0f);
                int serial = NetAction.Serial(p.act);
                bool fresh = lastSerial[i] >= 0 && serial != lastSerial[i]; // the first snapshot only sets the baseline, so joining mid-match plays nothing stale
                lastSerial[i] = serial;
                var a = AnimatedBody(i);
                if (a == null) continue;
                a.SpeedOverride = p.speed;
                int code = NetAction.Action(p.act) - 1;
                if (fresh && code >= 0 && code <= (int)AnimatedPlayerVisual.Action.Defeated)
                    if (a.PlayAction((AnimatedPlayerVisual.Action)code, NetAction.Right(p.act))) ActionsPlayed++;
            }
            ballBody.localPosition = view.ballPos;
        }
    }
}
