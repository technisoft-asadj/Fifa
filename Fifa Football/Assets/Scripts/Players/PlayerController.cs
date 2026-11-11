using UnityEngine;
using FootballSim.Ball;
using FootballSim.Inputs;

namespace FootballSim.Players
{
    /// <summary>Locomotion, dribble touches, pass, shoot and tackle. Driven by any IPlayerInput (human or AI).</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] PlayerData data;
        [SerializeField] BallController ball;
        [SerializeField] Transform cameraReference;
        [SerializeField] float controlRadius = 1.1f;
        [SerializeField] float chargeTime = 0.8f;
        [SerializeField] GameObject controlMarker;
        public bool isGoalkeeper;
        /// <summary>0 = home, 1 = away. Set by TeamController.</summary>
        public int TeamId { get; set; }

        void KickBall(Vector3 dir, float speed, Vector3 spin)
        {
            ball.LastTouchTeam = TeamId;
            ball.Kick(dir, speed, spin);
        }

        CharacterController cc;
        Vector3 planarVelocity;
        float shootCharge, touchCooldown, tackleCooldown, lungeTimer;
        Vector3 lungeDir;
        IPlayerInput input;

        public Vector3 Velocity => planarVelocity;
        public PlayerData Data => data;
        public IPlayerInput Input => input;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (data == null) data = ScriptableObject.CreateInstance<PlayerData>();
            if (controlMarker) controlMarker.SetActive(false);
        }

        public void SetInput(IPlayerInput newInput, bool humanControlled)
        {
            input = newInput;
            shootCharge = 0f;
            if (controlMarker) controlMarker.SetActive(humanControlled);
        }

        /// <summary>Teleport (kick-off reset). CharacterController must be disabled while moving it.</summary>
        public void TeleportTo(Vector3 pos, Quaternion rot)
        {
            cc.enabled = false;
            transform.SetPositionAndRotation(pos, rot);
            cc.enabled = true;
            planarVelocity = Vector3.zero;
            lungeTimer = 0f;
        }

        void Update()
        {
            if (input == null) return;
            tackleCooldown -= Time.deltaTime;
            Locomotion();
            BallInteraction();
            if (input.TacklePressed) TryTackle();
        }

        void Locomotion()
        {
            Vector2 m = input.Move;
            Vector3 dir;
            if (input.MoveIsWorldSpace) dir = new Vector3(m.x, 0f, m.y);
            else
            {
                Vector3 fwd = cameraReference ? Vector3.ProjectOnPlane(cameraReference.forward, Vector3.up).normalized : Vector3.forward;
                dir = fwd * m.y + Vector3.Cross(Vector3.up, fwd) * m.x;
            }
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            if (lungeTimer > 0f)
            {
                lungeTimer -= Time.deltaTime;
                planarVelocity = lungeDir * data.SprintSpeedMs * 1.15f;
            }
            else
            {
                float topSpeed = input.Sprint ? data.SprintSpeedMs : data.JogSpeed;
                float accel = data.AccelerationMs2 * (dir.sqrMagnitude > 0.01f ? 1f : 1.6f);
                planarVelocity = Vector3.MoveTowards(planarVelocity, dir * topSpeed, accel * Time.deltaTime);
            }

            if (planarVelocity.sqrMagnitude > 0.05f)
            {
                Quaternion want = Quaternion.LookRotation(planarVelocity.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, data.TurnRateDegPerSec * Time.deltaTime);
            }

            Vector3 motion = planarVelocity;
            motion.y = -2f;
            cc.Move(motion * Time.deltaTime);
        }

        bool BallInRange(out Vector3 toBall)
        {
            toBall = ball.transform.position - transform.position;
            toBall.y = 0f;
            return toBall.magnitude < controlRadius;
        }

        void BallInteraction()
        {
            if (ball == null) return;
            touchCooldown -= Time.deltaTime;
            bool inRange = BallInRange(out Vector3 toBall);
            bool inFront = Vector3.Dot(transform.forward, toBall.normalized) > 0.2f;

            // Dribble touch (not for goalkeepers: they catch/clear instead)
            if (!isGoalkeeper && inRange && inFront && planarVelocity.magnitude > 0.5f && touchCooldown <= 0f && ball.IsGrounded)
            {
                float push = planarVelocity.magnitude * 1.15f + 0.8f;
                Vector3 dirTo = Vector3.Lerp(transform.forward, planarVelocity.normalized, 0.7f).normalized;
                KickBall(dirTo, push, Vector3.zero);
                touchCooldown = Mathf.Lerp(0.35f, 0.18f, data.ballControl / 99f);
            }

            if (input.ShootHeld) shootCharge = Mathf.Min(1f, shootCharge + Time.deltaTime / chargeTime);

            if (input.ShootReleased)
            {
                float p = input.ActionPower > 0f ? input.ActionPower : Mathf.Max(0.25f, shootCharge);
                if (inRange) Strike(p, data.MaxShotSpeed, data.ShotInaccuracyDeg, 0.18f);
                shootCharge = 0f;
            }
            else if (input.PassPressed && inRange)
            {
                float p = input.ActionPower > 0f ? input.ActionPower : 0.55f;
                Strike(p, data.MaxPassSpeed, data.PassInaccuracyDeg, 0.02f);
            }
        }

        void Strike(float power, float maxSpeed, float inaccuracyDeg, float loft)
        {
            Vector3 aim = input.AimWorld;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.01f) aim = planarVelocity.sqrMagnitude > 0.5f ? planarVelocity : transform.forward;
            aim.Normalize();
            aim = Quaternion.Euler(0f, Random.Range(-inaccuracyDeg, inaccuracyDeg) * power, 0f) * aim;
            Vector3 dir = (aim + Vector3.up * loft * power).normalized;
            Vector3 spin = Vector3.Cross(Vector3.up, aim) * Random.Range(-4f, 4f) * power;
            KickBall(dir, maxSpeed * Mathf.Clamp01(power), spin);
            touchCooldown = 0.4f;
        }

        void TryTackle()
        {
            if (tackleCooldown > 0f || isGoalkeeper) return;
            tackleCooldown = 0.9f;
            lungeDir = transform.forward;
            lungeTimer = 0.22f;

            Vector3 toBall = ball.transform.position - transform.position;
            toBall.y = 0f;
            bool inReach = toBall.magnitude < 1.7f && Vector3.Dot(transform.forward, toBall.normalized) > 0.3f;
            float winChance = 0.55f + data.strength / 400f;
            if (inReach && Random.value < winChance)
            {
                // Poke the ball loose to the side/forward of the tackler
                Vector3 loose = (transform.forward + Quaternion.Euler(0, Random.Range(-50f, 50f), 0) * transform.forward).normalized;
                KickBall(loose, 6.5f, Vector3.zero);
                touchCooldown = 0.5f;
            }
        }
    }
}
