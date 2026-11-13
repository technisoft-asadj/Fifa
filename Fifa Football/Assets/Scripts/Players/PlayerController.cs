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
            if (ball.Body.isKinematic) return; // ball is held for a restart: nobody can touch it yet (setting velocity on it only spams warnings)
            // A keeper kicking away an opponent's moving ball (a shot he got to with his feet) is a save as well
            bool stoppedShot = isGoalkeeper && ball.LastTouchTeam != TeamId && ball.LastTouchTeam != -1 && ball.Velocity.magnitude > GoalkeeperModel.MinShotSpeed;
            ball.LastTouchTeam = TeamId;
            ball.LastToucher = this;
            ball.Kick(dir, speed, spin);
            if (stoppedShot) Saved?.Invoke(this, false);
        }

        CharacterController cc;
        PlayerVisual visual;
        Vector3 planarVelocity;
        float shootCharge, touchCooldown, tackleCooldown, lungeTimer;
        Vector3 lungeDir;
        IPlayerInput input;

        /// <summary>0..1; sprinting drains it. When it hits 0 the player jogs until it recovers to 25%.</summary>
        public float Stamina { get; set; } = 1f;
        public bool Exhausted { get; private set; }

        public Vector3 Velocity => planarVelocity;
        public PlayerData Data => data;
        public IPlayerInput Input => input;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            visual = GetComponent<PlayerVisual>();
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
            if (isGoalkeeper) TrySave();
        }

        // ---- goalkeeper: dive and save ----
        float saveCooldown;
        /// <summary>Raised when a keeper stops a shot: keeper, true if he held it, false if he pushed it away.</summary>
        public static event System.Action<PlayerController, bool> Saved;
        public bool IsDiving => isGoalkeeper && lungeTimer > 0f;

        void StartDive()
        {
            if (tackleCooldown > 0f) return;
            tackleCooldown = 1.5f;
            Vector2 m = input.Move;
            Vector3 dir = input.MoveIsWorldSpace ? new Vector3(m.x, 0f, m.y) : transform.right * m.x;
            if (dir.sqrMagnitude < 0.04f) dir = transform.right; // no direction given: dive to the keeper's right
            lungeDir = dir.normalized;
            lungeTimer = 0.45f;
            if (visual != null) visual.PlayTackle();
        }

        /// <summary>A fast ball arriving within the keeper's reach is held or parried (see GoalkeeperModel); one attempt per shot.</summary>
        void TrySave()
        {
            saveCooldown -= Time.deltaTime;
            if (ball == null || saveCooldown > 0f || ball.Body.isKinematic) return;
            if (ball.LastTouchTeam == TeamId) return; // only opponents' shots
            // The player's pivot is the middle of the capsule (about 1 m up), so measure from it; height counts half, as a
            // keeper reaches down to the ground and up above his head more easily than he moves sideways
            Vector3 toBall = ball.transform.position - transform.position;
            float dist = new Vector2(toBall.x, toBall.z).magnitude;
            dist = Mathf.Sqrt(dist * dist + (toBall.y - 0.2f) * (toBall.y - 0.2f) * 0.25f);
            float reach = IsDiving ? GoalkeeperModel.DiveReach : GoalkeeperModel.StandReach;
            if (dist > reach || ball.transform.position.y > GoalkeeperModel.MaxHeight) return;
            Vector3 vel = ball.Velocity;
            if (Vector3.Dot(vel, -toBall) <= 0f) return; // not coming towards him
            float speed = vel.magnitude;
            if (speed < GoalkeeperModel.MinShotSpeed) return;

            saveCooldown = 0.6f;
            var outcome = GoalkeeperModel.Decide(speed, dist, reach, Random.value);
            if (outcome == SaveOutcome.None) return;
            ball.LastTouchTeam = TeamId;
            ball.LastToucher = this;
            if (outcome == SaveOutcome.Catch)
            {
                ball.ResetTo(transform.position + transform.forward * 0.5f + Vector3.up * 0.12f);
                Saved?.Invoke(this, true);
            }
            else
            {
                // Push it back out and to the side the ball came from
                Vector3 flat = new Vector3(toBall.x, 0f, toBall.z).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, flat) * (toBall.x >= 0f ? 1f : -1f);
                Vector3 dir = (flat * 0.6f + side * 0.6f + Vector3.up * 0.3f).normalized;
                ball.Kick(dir, Mathf.Clamp(speed * 0.35f, 4f, 9f), Vector3.zero);
                Saved?.Invoke(this, false);
            }
            if (visual != null) visual.PlayKick(true);
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
                bool sprinting = input.Sprint && dir.sqrMagnitude > 0.01f && !Exhausted;
                Stamina = StaminaModel.Step(Stamina, sprinting, Time.deltaTime);
                Exhausted = StaminaModel.UpdateExhausted(Stamina, Exhausted);
                float topSpeed = sprinting ? data.SprintSpeedMs : data.JogSpeed;
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
                if (inRange && !ball.Body.isKinematic) { Strike(p, data.MaxShotSpeed, data.ShotInaccuracyDeg, 0.18f); Struck?.Invoke(this, true, lastStrikeDir); }
                shootCharge = 0f;
            }
            else if (input.PassPressed && inRange && !ball.Body.isKinematic)
            {
                float p = input.ActionPower > 0f ? input.ActionPower : 0.55f;
                Strike(p, data.MaxPassSpeed, data.PassInaccuracyDeg, 0.02f);
                Struck?.Invoke(this, false, lastStrikeDir);
            }
        }

        /// <summary>Raised after a pass or shot; arguments = player, true for a shot, strike direction.</summary>
        public static event System.Action<PlayerController, bool, Vector3> Struck;
        Vector3 lastStrikeDir;

        void Strike(float power, float maxSpeed, float inaccuracyDeg, float loft)
        {
            Vector3 aim = input.AimWorld;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.01f) aim = planarVelocity.sqrMagnitude > 0.5f ? planarVelocity : transform.forward;
            aim.Normalize();
            aim = Quaternion.Euler(0f, Random.Range(-inaccuracyDeg, inaccuracyDeg) * power, 0f) * aim;
            Vector3 dir = (aim + Vector3.up * loft * power).normalized;
            Vector3 spin = Vector3.Cross(Vector3.up, aim) * Random.Range(-4f, 4f) * power;
            lastStrikeDir = dir;
            KickBall(dir, maxSpeed * Mathf.Clamp01(power), spin);
            if (visual != null) visual.PlayKick(Random.value > 0.25f);
            touchCooldown = 0.4f;
        }

        void TryTackle()
        {
            if (isGoalkeeper) { StartDive(); return; } // keepers do not tackle: the same button dives
            if (tackleCooldown > 0f) return;
            tackleCooldown = 0.9f;
            lungeDir = transform.forward;
            lungeTimer = 0.22f;
            if (visual != null) visual.PlayTackle();

            Vector3 toBall = ball.transform.position - transform.position;
            toBall.y = 0f;
            bool inReach = toBall.magnitude < 1.7f && Vector3.Dot(transform.forward, toBall.normalized) > 0.3f;
            float winChance = 0.55f + data.strength / 400f;
            bool won = inReach && Random.value < winChance;
            if (won)
            {
                // Poke the ball loose to the side/forward of the tackler
                Vector3 loose = (transform.forward + Quaternion.Euler(0, Random.Range(-50f, 50f), 0) * transform.forward).normalized;
                KickBall(loose, 6.5f, Vector3.zero);
                touchCooldown = 0.5f;
            }
            else CheckFoul();
        }

        /// <summary>Raised when a tackle takes a player instead of the ball. MatchManager listens.</summary>
        public static event System.Action<PlayerController, PlayerController> Fouled;

        /// <summary>A failed tackle that connects with an opponent is a foul; from behind it is almost certain.</summary>
        void CheckFoul()
        {
            var hits = Physics.OverlapSphere(transform.position + transform.forward * 0.9f, 0.9f);
            foreach (var h in hits)
            {
                var other = h.GetComponentInParent<PlayerController>();
                if (other == null || other == this || other.TeamId == TeamId || other.isGoalkeeper) continue;
                bool fromBehind = Vector3.Dot(other.transform.forward, transform.forward) > 0.5f;
                float chance = fromBehind ? 0.85f : 0.45f;
                if (Random.value < chance) Fouled?.Invoke(this, other);
                return;
            }
        }
    }
}
