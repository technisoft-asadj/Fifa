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
            // a player the camera cannot see keeps his state (so an action still ends on time) but his bones are not recalculated
            if (animator != null) animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
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

        public enum Action { Pass, Shot, Receive, Tackle, Fall, Dive, Catch, JumpCatch, CelebrateA, CelebrateB, Defeated }

        /// <summary>Seconds the clip is started before its contact moment, so the foot meets the ball right after the simulation kicked it.</summary>
        public const float ContactLead = 0.04f;
        static AnimatedClipInfo clipInfo;
        /// <summary>The state last entered by <see cref="PlayAction"/> and whether it was mirrored (for tests and diagnostics).</summary>
        public string LastActionState { get; private set; }
        public bool LastActionMirrored { get; private set; }
        public float LastActionContactTime { get; private set; }
        /// <summary>Length in seconds of the clip last started by <see cref="PlayAction"/> (at its own speed 1).</summary>
        public float LastActionLength { get; private set; }

        /// <summary>
        /// Plays a kick or a first touch. The simulation has already acted (the ball is struck this frame), so the clip starts just before its
        /// own contact moment: the wind-up is skipped and the foot arrives within <see cref="ContactLead"/> seconds. A slow player gets the
        /// standing clip, a jogging one the strike-while-jogging clip; the left foot is the same clip mirrored.
        /// </summary>
        public bool PlayAction(Action action, bool rightFoot)
        {
            if (animator == null || !animator.isActiveAndEnabled) return false;
            if (clipInfo == null) clipInfo = AnimatedClipInfo.Load();
            if (clipInfo == null) return false;
            float speedNow = SpeedOverride >= 0f ? SpeedOverride : (pc != null ? pc.Velocity.magnitude : 0f);
            bool moving = speedNow > 2.5f;
            string state = action == Action.Receive ? "Receive" : action == Action.Tackle ? "SlideTackle" : action == Action.Fall ? "FallOver"
                : action == Action.CelebrateA ? "CelebrateA" : action == Action.CelebrateB ? "CelebrateB" : action == Action.Defeated ? "Defeated"
                : action == Action.Dive ? "KeeperDive" : action == Action.Catch ? "KeeperCatch" : action == Action.JumpCatch ? "KeeperJumpCatch"
                : moving ? "StrikeJog" : action == Action.Shot ? "Shot" : "SoccerPass";
            var e = clipInfo.Find(state);
            if (e == null) return false;
            if (action == Action.Dive)
            {
                // the clip dives to the keeper's right; the left is its mirror image. The simulation carries the capsule, the body stays on it.
                animator.SetBool(MirrorId, !rightFoot);
                animator.CrossFadeInFixedTime(state, 0.06f, 0, 0f);
                LastActionState = state; LastActionMirrored = !rightFoot; LastActionContactTime = e.contact;
                return true;
            }
            if (action == Action.Tackle || action == Action.Fall || action == Action.Catch || action == Action.JumpCatch
                || action == Action.CelebrateA || action == Action.CelebrateB || action == Action.Defeated)
            {
                // whole-body moves play from their start: the slide is the player's own lunge, the fall follows a foul
                animator.SetBool(MirrorId, false);
                animator.CrossFadeInFixedTime(state, 0.08f, 0, 0f);
                LastActionState = state; LastActionMirrored = false; LastActionContactTime = e.contact; LastActionLength = e.length;
                return true;
            }
            animator.SetBool(MirrorId, rightFoot != e.rightFoot); // the clip's own foot, or its mirror image
            float offset = Mathf.Max(0f, e.contact - ContactLead);
            animator.CrossFadeInFixedTime(state, ContactLead, 0, offset);
            LastActionState = state; LastActionMirrored = rightFoot != e.rightFoot; LastActionContactTime = e.contact;
            return true;
        }

        /// <summary>Leaves a long clip early (a goal reaction cut at the end of the goal pause) with a short blend back into locomotion.</summary>
        public void EndAction()
        {
            if (animator == null || !animator.isActiveAndEnabled) return;
            var st = animator.GetCurrentAnimatorStateInfo(0);
            if (!st.IsName("Locomotion")) animator.CrossFadeInFixedTime("Locomotion", 0.25f, 0, 0f);
        }

        static readonly int MirrorId = Animator.StringToHash("Mirror");

        void Update()
        {
            if (animator == null) return;
            float speed = SpeedOverride >= 0f ? SpeedOverride : (pc != null ? pc.Velocity.magnitude : 0f);
            animator.SetFloat(SpeedId, speed, 0.08f, Time.deltaTime);
        }
    }
}
