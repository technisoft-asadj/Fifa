using UnityEngine;

namespace FootballSim.Net
{
    /// <summary>
    /// Draws what a <see cref="NetClient"/> has received: 22 capsules and a ball, placed from the interpolation buffer a fixed number of
    /// ticks behind the newest snapshot. It reads no simulation objects, only snapshots, which is what a real remote player's screen would do.
    /// A spike: plain primitives, and the render point follows the newest arrival so it jitters when packets do.
    /// </summary>
    public class NetClientView : MonoBehaviour
    {
        public NetClient client;
        /// <summary>How many server ticks behind the newest snapshot to draw. 6 ticks is 100 ms at 60 ticks per second.</summary>
        public float delayTicks = 6f;

        Transform[] bodies;
        Transform ballBody;
        readonly WorldSnapshot view = new WorldSnapshot();

        public Transform Ball => ballBody;
        public Transform[] Bodies => bodies;
        public WorldSnapshot Latest => view;

        void Awake()
        {
            bodies = new Transform[WorldSnapshot.PlayerCount];
            for (int i = 0; i < bodies.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = "NetPlayer" + i;
                Destroy(go.GetComponent<Collider>()); // a display only: it must not touch the real match
                go.transform.SetParent(transform, false);
                var r = go.GetComponent<Renderer>();
                if (r != null) r.material.color = i < 11 ? new Color(0.85f, 0.15f, 0.15f) : new Color(0.15f, 0.3f, 0.85f);
                bodies[i] = go.transform;
            }
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "NetBall";
            Destroy(ball.GetComponent<Collider>());
            ball.transform.SetParent(transform, false);
            ball.transform.localScale = Vector3.one * 0.22f;
            ballBody = ball.transform;
        }

        void LateUpdate()
        {
            if (client == null || !client.Buffer.Sample(client.Buffer.NewestTick - delayTicks, view)) return;
            for (int i = 0; i < bodies.Length; i++)
            {
                var p = view.players[i];
                bodies[i].localPosition = new Vector3(p.x, 1f, p.z);
                bodies[i].localRotation = Quaternion.Euler(0f, p.yaw, 0f);
            }
            ballBody.localPosition = view.ballPos;
        }
    }
}
