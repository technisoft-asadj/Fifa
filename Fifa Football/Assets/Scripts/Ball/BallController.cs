using UnityEngine;

namespace FootballSim.Ball
{
    /// <summary>
    /// Independent gameplay ball. Uses Rigidbody for collisions, with custom drag,
    /// rolling resistance and Magnus (curve) forces applied in FixedUpdate.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class BallController : MonoBehaviour
    {
        [SerializeField] BallSettings settings;
        Rigidbody rb;
        bool grounded;

        public Rigidbody Body => rb;
        public Vector3 Velocity => rb.linearVelocity;
        public bool IsGrounded => grounded;
        /// <summary>Team that last touched the ball (-1 = nobody yet). Used for restarts.</summary>
        public int LastTouchTeam { get; set; } = -1;
        /// <summary>Player who last kicked the ball (null after a restart). Used for offside.</summary>
        public Component LastToucher { get; set; }

        /// <summary>Hold the ball still (set pieces). Unfreezing leaves it at rest.</summary>
        public void Freeze(bool frozen)
        {
            if (frozen) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            rb.isKinematic = frozen;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (settings == null) settings = ScriptableObject.CreateInstance<BallSettings>();
            rb.mass = settings.mass;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.maxAngularVelocity = 60f;
            var col = GetComponent<SphereCollider>();
            col.radius = settings.radius / Mathf.Max(0.0001f, transform.localScale.x);
        }

        void FixedUpdate()
        {
            Vector3 v = rb.linearVelocity;
            float speed = v.magnitude;

            grounded = Physics.Raycast(transform.position, Vector3.down, settings.radius + 0.03f);

            if (speed > 0.001f)
            {
                // Quadratic air drag
                rb.AddForce(-v.normalized * (settings.airDrag * speed * speed), ForceMode.Force);
                // Magnus effect: F = k * (omega x v)
                rb.AddForce(settings.magnusStrength * Vector3.Cross(rb.angularVelocity, v), ForceMode.Force);
            }

            if (grounded)
            {
                Vector3 flat = new Vector3(v.x, 0f, v.z);
                float fs = flat.magnitude;
                if (fs > 0.01f)
                {
                    float dec = Mathf.Min(fs, settings.rollingResistance * Time.fixedDeltaTime);
                    rb.AddForce(-flat.normalized * (dec / Time.fixedDeltaTime) * rb.mass, ForceMode.Force);
                }
                else rb.linearVelocity = new Vector3(0f, v.y, 0f); // settle to rest
            }

            rb.angularVelocity *= 1f - settings.spinDecay * Time.fixedDeltaTime;

            if (speed > settings.maxSpeed) rb.linearVelocity = v.normalized * settings.maxSpeed;
        }

        /// <summary>Apply a kick: world direction (normalised), speed in m/s, and spin (rad/s, world axis).</summary>
        public void Kick(Vector3 direction, float speed, Vector3 spin)
        {
            rb.linearVelocity = direction.normalized * Mathf.Min(speed, settings.maxSpeed);
            rb.angularVelocity = spin;
        }

        public void ResetTo(Vector3 position)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = position;
            transform.position = position;
        }
    }
}
