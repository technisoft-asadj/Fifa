using UnityEngine;

namespace FootballSim.Players
{
    /// <summary>
    /// The rigged, Animator-driven body of a player (the Mixamo Y Bot prefab under Resources/Animated, built by the editor tool). Animation
    /// follows the simulation: the Animator only reads the player's speed, never moves the player (no root motion). Phase 2 covers
    /// locomotion only; kicks and tackles are not animated on this body yet. When the prefab is missing, <see cref="Build"/> returns false
    /// and the primitive body stays.
    /// </summary>
    public class AnimatedPlayerVisual : MonoBehaviour
    {
        public const string ResourcePath = "Animated/AnimatedPlayer";
        /// <summary>Local height of the feet: the character controller's capsule bottom.</summary>
        public const float FeetY = -0.9f;
        static readonly int SpeedId = Animator.StringToHash("Speed");

        PlayerController pc;
        Animator animator;
        Material bodyMaterial;
        GameObject model;

        public bool IsBuilt => model != null;
        public Animator Animator => animator;
        public GameObject Model => model;

        public static bool PrefabAvailable => Resources.Load<GameObject>(ResourcePath) != null;

        public bool Build(PlayerController owner, Color shirt)
        {
            pc = owner;
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null) return false;
            model = Instantiate(prefab, transform);
            model.name = "Animated";
            model.transform.localPosition = new Vector3(0f, FeetY, 0f);
            model.transform.localRotation = Quaternion.identity;
            animator = model.GetComponent<Animator>();
            // one material instance per player so each can wear its own kit colour (the joint material stays dark)
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                var mats = r.materials;
                for (int i = 0; i < mats.Length; i++)
                    if (!mats[i].name.ToLowerInvariant().Contains("joints")) bodyMaterial = mats[i];
                r.materials = mats;
            }
            SetKit(shirt);
            return true;
        }

        public void SetKit(Color shirt)
        {
            if (bodyMaterial == null) return;
            bodyMaterial.color = shirt;
            if (bodyMaterial.HasProperty("_BaseColor")) bodyMaterial.SetColor("_BaseColor", shirt);
        }

        /// <summary>The speed fed to the Animator, in metres per second (test hook; normally the player's own speed).</summary>
        public float SpeedOverride = -1f;

        void Update()
        {
            if (animator == null) return;
            float speed = SpeedOverride >= 0f ? SpeedOverride : (pc != null ? pc.Velocity.magnitude : 0f);
            animator.SetFloat(SpeedId, speed, 0.08f, Time.deltaTime);
        }
    }
}
