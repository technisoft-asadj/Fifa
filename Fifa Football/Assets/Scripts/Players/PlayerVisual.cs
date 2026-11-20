using UnityEngine;

namespace FootballSim.Players
{
    /// <summary>
    /// Procedural humanoid built from primitives with code-driven run, kick and tackle animation.
    /// Replaces the placeholder capsule's mesh (shirt colour is taken from its material). Gameplay never
    /// depends on this; a rigged model with an Animator can replace it later by implementing the same
    /// three calls (speed from PlayerController.Velocity, PlayKick, PlayTackle).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerVisual : MonoBehaviour
    {
        static readonly Color[] SkinTones =
        {
            new Color(0.96f, 0.80f, 0.69f), new Color(0.87f, 0.68f, 0.52f), new Color(0.72f, 0.53f, 0.38f),
            new Color(0.55f, 0.38f, 0.26f), new Color(0.36f, 0.24f, 0.17f)
        };
        static readonly Color[] HairColors =
        {
            new Color(0.08f, 0.06f, 0.05f), new Color(0.25f, 0.16f, 0.09f), new Color(0.55f, 0.38f, 0.16f), new Color(0.75f, 0.62f, 0.3f)
        };

        /// <summary>Which players wear the rigged, animated body instead of the primitive one. Saved; Off by default.</summary>
        public enum ModelMode { Off = 0, One = 1, All = 2 }
        const string ModelModeKey = "player_models";
        static ModelMode cachedMode = (ModelMode)(-1);

        public static ModelMode Mode
        {
            get
            {
                if ((int)cachedMode < 0)
                {
                    try { cachedMode = (ModelMode)Mathf.Clamp(PlayerPrefs.GetInt(ModelModeKey, 0), 0, 2); } catch { cachedMode = ModelMode.Off; }
                }
                return cachedMode;
            }
            set
            {
                cachedMode = value;
                try { PlayerPrefs.SetInt(ModelModeKey, (int)value); PlayerPrefs.Save(); } catch { }
            }
        }

        /// <summary>Sets the mode and applies it to every player in the scene.</summary>
        public static void SetMode(ModelMode mode)
        {
            Mode = mode;
            foreach (var v in FindObjectsByType<PlayerVisual>(FindObjectsSortMode.None)) v.ApplyMode();
        }

        PlayerController pc;
        Transform visualRoot;
        AnimatedPlayerVisual animated;
        bool controlled, animatedActive, animatedFailed;
        Color shirtColor = Color.red;
        Transform body, hipL, hipR, kneeL, kneeR, shoulderL, shoulderR;
        float phase, amp, kickT = -1f, tackleT = -1f;
        bool kickRight = true;

        Material shirtMaterial, shortsMaterial, sockMaterial, secondaryMaterial, gloveMaterial, skinMaterial, hairMaterial;
        readonly System.Collections.Generic.List<GameObject> patternParts = new System.Collections.Generic.List<GameObject>();
        readonly System.Collections.Generic.List<Renderer> sleeves = new System.Collections.Generic.List<Renderer>();
        TMPro.TextMeshPro numberText;
        string shirtNumber;

        /// <summary>Change the team kit (shirt, shorts and socks) after the player has been built.</summary>
        public void SetKit(Color shirt) { ApplyKit(Core.ClubKit.Plain(shirt), false, null); }

        /// <summary>Puts a full strip on the player: shirt with its pattern, shorts, socks, the number on the back and (for a keeper) gloves.</summary>
        public void ApplyKit(Core.ClubKit kit, bool keeper, string number)
        {
            shirtColor = kit.shirt;
            Tint(shirtMaterial, kit.shirt);
            Tint(shortsMaterial, kit.shorts);
            Tint(sockMaterial, kit.socks);
            Tint(secondaryMaterial, kit.secondary);
            Tint(gloveMaterial, keeper ? Color.Lerp(kit.shirt, Color.white, 0.55f) : skinColor);
            shirtNumber = number;
            if (body != null) BuildPattern(kit);
            if (animated != null) animated.SetKit(kit.shirt);
        }

        Color skinColor = Color.white;

        void BuildPattern(Core.ClubKit kit)
        {
            foreach (var g in patternParts) if (g != null) Destroy(g);
            patternParts.Clear();
            var torso = body.Find("Torso");
            if (torso == null || secondaryMaterial == null) return;
            // the sleeves wear the second colour on a "sleeves" strip
            foreach (var r in sleeves) if (r != null) r.sharedMaterial = kit.pattern == Core.KitPattern.Sleeves ? secondaryMaterial : shirtMaterial;
            if (kit.pattern == Core.KitPattern.Stripes)
                for (int i = -1; i <= 1; i++) patternParts.Add(Part(PrimitiveType.Cube, "Stripe", body, new Vector3(i * 0.135f, 0.3f, 0f), new Vector3(0.07f, 0.5f, 0.236f), secondaryMaterial));
            else if (kit.pattern == Core.KitPattern.Hoops)
                for (int i = 0; i < 3; i++) patternParts.Add(Part(PrimitiveType.Cube, "Hoop", body, new Vector3(0f, 0.13f + i * 0.17f, 0f), new Vector3(0.404f, 0.075f, 0.236f), secondaryMaterial));
            // a collar in the second colour
            patternParts.Add(Part(PrimitiveType.Cube, "Collar", body, new Vector3(0f, 0.55f, 0f), new Vector3(0.17f, 0.035f, 0.14f), secondaryMaterial));
            // the number on the back
            try
            {
                if (numberText == null)
                {
                    var go = new GameObject("Number");
                    go.transform.SetParent(body, false);
                    go.transform.localPosition = new Vector3(0f, 0.3f, -0.122f);
                    go.transform.localRotation = Quaternion.identity; // the player faces +Z, so text that reads from -Z is on his back
                    numberText = go.AddComponent<TMPro.TextMeshPro>();
                    numberText.alignment = TMPro.TextAlignmentOptions.Center;
                    numberText.fontStyle = TMPro.FontStyles.Bold;
                    numberText.enableAutoSizing = true; numberText.fontSizeMin = 0.5f; numberText.fontSizeMax = 3.2f;
                    numberText.rectTransform.sizeDelta = new Vector2(0.3f, 0.22f);
                    numberText.raycastTarget = false;
                    numberText.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                }
                numberText.text = shirtNumber ?? "";
                numberText.color = Core.TeamCatalog.Distance(kit.secondary, kit.shirt) > 0.7f ? kit.secondary : ((kit.shirt.r + kit.shirt.g + kit.shirt.b) / 3f > 0.55f ? new Color(0.08f, 0.08f, 0.1f) : Color.white);
                numberText.gameObject.SetActive(!string.IsNullOrEmpty(shirtNumber));
            }
            catch { /* no text font in this build: the shirt simply has no number */ }
        }

        /// <summary>Tells the visual whether the human controls this player (in <see cref="ModelMode.One"/> only that player is animated).</summary>
        public void SetControlled(bool humanControlled) { controlled = humanControlled; ApplyMode(); }

        public bool AnimatedActive => animatedActive;
        public AnimatedPlayerVisual Animated => animated;

        /// <summary>Shows the animated body or the primitive one according to <see cref="Mode"/>; builds the animated body the first time it is needed.</summary>
        public void ApplyMode()
        {
            bool want = Mode == ModelMode.All || (Mode == ModelMode.One && controlled);
            if (want && animated == null && !animatedFailed)
            {
                var a = gameObject.AddComponent<AnimatedPlayerVisual>();
                if (a.Build(pc != null ? pc : GetComponent<PlayerController>(), shirtColor)) animated = a;
                else { Destroy(a); animatedFailed = true; } // no prefab (files not downloaded): the primitive body stays
            }
            animatedActive = want && animated != null && animated.IsBuilt;
            if (animated != null && animated.Model != null) animated.Model.SetActive(animatedActive);
            if (visualRoot != null) visualRoot.gameObject.SetActive(!animatedActive);
        }

        static void Tint(Material m, Color c)
        {
            if (m == null) return;
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        }

        public Transform Body => body;
        public bool IsBuilt => body != null;

        void Awake()
        {
            pc = GetComponent<PlayerController>();
            Build();
            ApplyMode();
        }

        void Build()
        {
            var shirt = Color.red;
            var srcRenderer = GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (srcRenderer != null)
            {
                if (srcRenderer.sharedMaterial != null)
                {
                    shirt = srcRenderer.sharedMaterial.HasProperty("_BaseColor") ? srcRenderer.sharedMaterial.GetColor("_BaseColor") : srcRenderer.sharedMaterial.color;
                    if (srcRenderer.sharedMaterial.shader != null) shader = srcRenderer.sharedMaterial.shader;
                }
                srcRenderer.enabled = false;
            }
            var mf = GetComponent<MeshFilter>();
            if (mf != null) { /* mesh stays but is not rendered */ }

            // Deterministic variety per player from the object's name hash
            int h = Mathf.Abs(name.GetHashCode());
            var skin = SkinTones[h % SkinTones.Length];
            var hair = HairColors[(h / 7) % HairColors.Length];
            var shorts = Color.Lerp(shirt, Color.white, 0.75f);
            var boot = new Color(0.08f, 0.08f, 0.08f);

            Material M(Color c)
            {
                var m = new Material(shader);
                m.color = c;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                return m;
            }
            var mShirt = M(shirt); var mShorts = M(shorts); var mSkin = M(skin); var mHair = M(hair); var mBoot = M(boot);
            var mSock = M(Color.Lerp(shirt, Color.black, 0.15f));
            shirtMaterial = mShirt; shortsMaterial = mShorts; sockMaterial = mSock;
            secondaryMaterial = M(Color.white); gloveMaterial = M(skin); skinMaterial = mSkin; hairMaterial = mHair; skinColor = skin;

            shirtColor = shirt;
            var visual = new GameObject("Visual").transform;
            visualRoot = visual;
            visual.SetParent(transform, false);
            visual.localPosition = new Vector3(0f, 0.09f, 0f); // put boots on the ground (capsule feet are at -0.9)
            body = new GameObject("Body").transform;
            body.SetParent(visual, false);

            Part(PrimitiveType.Cube, "Torso", body, new Vector3(0f, 0.3f, 0f), new Vector3(0.4f, 0.5f, 0.23f), mShirt);
            Part(PrimitiveType.Cylinder, "Neck", body, new Vector3(0f, 0.6f, 0f), new Vector3(0.09f, 0.04f, 0.09f), mSkin);
            Part(PrimitiveType.Sphere, "Head", body, new Vector3(0f, 0.74f, 0f), new Vector3(0.21f, 0.24f, 0.22f), mSkin);
            // face: eyes and ears, and a hair style that varies from player to player
            Part(PrimitiveType.Sphere, "EyeL", body, new Vector3(-0.04f, 0.76f, 0.098f), new Vector3(0.028f, 0.03f, 0.02f), MatOf(shader, new Color(0.05f, 0.05f, 0.06f)));
            Part(PrimitiveType.Sphere, "EyeR", body, new Vector3(0.04f, 0.76f, 0.098f), new Vector3(0.028f, 0.03f, 0.02f), MatOf(shader, new Color(0.05f, 0.05f, 0.06f)));
            Part(PrimitiveType.Sphere, "Nose", body, new Vector3(0f, 0.72f, 0.108f), new Vector3(0.035f, 0.04f, 0.035f), mSkin);
            Part(PrimitiveType.Sphere, "EarL", body, new Vector3(-0.108f, 0.74f, 0f), new Vector3(0.03f, 0.06f, 0.045f), mSkin);
            Part(PrimitiveType.Sphere, "EarR", body, new Vector3(0.108f, 0.74f, 0f), new Vector3(0.03f, 0.06f, 0.045f), mSkin);
            int style = (h / 3) % 5;
            if (style == 0) Part(PrimitiveType.Sphere, "Hair", body, new Vector3(0f, 0.8f, -0.012f), new Vector3(0.235f, 0.13f, 0.235f), mHair);
            else if (style == 1) Part(PrimitiveType.Sphere, "Hair", body, new Vector3(0f, 0.815f, -0.005f), new Vector3(0.226f, 0.09f, 0.228f), mHair); // cropped
            else if (style == 2)
            {
                Part(PrimitiveType.Sphere, "Hair", body, new Vector3(0f, 0.8f, -0.012f), new Vector3(0.24f, 0.14f, 0.24f), mHair);
                Part(PrimitiveType.Cube, "HairBack", body, new Vector3(0f, 0.72f, -0.1f), new Vector3(0.2f, 0.16f, 0.07f), mHair); // longer at the back
            }
            else if (style == 3) Part(PrimitiveType.Sphere, "Hair", body, new Vector3(0f, 0.83f, -0.01f), new Vector3(0.27f, 0.2f, 0.27f), mHair); // big curly hair
            else { Part(PrimitiveType.Sphere, "Hair", body, new Vector3(0f, 0.805f, -0.01f), new Vector3(0.232f, 0.11f, 0.232f), mHair); Part(PrimitiveType.Sphere, "Bun", body, new Vector3(0f, 0.9f, -0.07f), Vector3.one * 0.08f, mHair); }
            Part(PrimitiveType.Cube, "Shorts", body, new Vector3(0f, -0.07f, 0f), new Vector3(0.38f, 0.24f, 0.22f), mShorts);

            hipL = Leg("L", -0.12f, body, mSkin, mSock, mBoot, out kneeL);
            hipR = Leg("R", 0.12f, body, mSkin, mSock, mBoot, out kneeR);
            shoulderL = Arm("L", -0.27f, body, mSkin, mShirt, gloveMaterial, sleeves);
            shoulderR = Arm("R", 0.27f, body, mSkin, mShirt, gloveMaterial, sleeves);
            BuildPattern(Core.ClubKit.Plain(shirt));
        }

        static Material MatOf(Shader shader, Color c)
        {
            var m = new Material(shader) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        static GameObject Part(PrimitiveType type, string n, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = n;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static Transform Leg(string side, float x, Transform parent, Material skin, Material sock, Material boot, out Transform knee)
        {
            var hip = new GameObject("Hip" + side).transform;
            hip.SetParent(parent, false); hip.localPosition = new Vector3(x, -0.12f, 0f);
            Part(PrimitiveType.Capsule, "Thigh", hip, new Vector3(0f, -0.2f, 0f), new Vector3(0.17f, 0.21f, 0.17f), skin);
            knee = new GameObject("Knee" + side).transform;
            knee.SetParent(hip, false); knee.localPosition = new Vector3(0f, -0.41f, 0f);
            Part(PrimitiveType.Capsule, "Shin", knee, new Vector3(0f, -0.2f, 0f), new Vector3(0.14f, 0.2f, 0.14f), sock);
            Part(PrimitiveType.Cube, "Boot", knee, new Vector3(0f, -0.42f, 0.06f), new Vector3(0.14f, 0.09f, 0.27f), boot);
            return hip;
        }

        static Transform Arm(string side, float x, Transform parent, Material skin, Material sleeve, Material hand, System.Collections.Generic.List<Renderer> sleeveList)
        {
            var sh = new GameObject("Shoulder" + side).transform;
            sh.SetParent(parent, false); sh.localPosition = new Vector3(x, 0.5f, 0f);
            Part(PrimitiveType.Capsule, "Arm", sh, new Vector3(0f, -0.22f, 0f), new Vector3(0.095f, 0.22f, 0.095f), skin);
            var s = Part(PrimitiveType.Capsule, "Sleeve", sh, new Vector3(0f, -0.08f, 0f), new Vector3(0.12f, 0.1f, 0.12f), sleeve);
            sleeveList.Add(s.GetComponent<Renderer>());
            Part(PrimitiveType.Sphere, "Hand", sh, new Vector3(0f, -0.45f, 0f), Vector3.one * 0.1f, hand); // a keeper's gloves are this material in his colour
            return sh;
        }

        // ---- what a remote screen is told: the last action started, whichever body is showing (see Net.NetAction) ----
        int netAction, netSerial; bool netRight = true;
        /// <summary>The last body action, packed for the snapshot (0 = none yet).</summary>
        public byte NetAct => Net.NetAction.Pack(netAction, netRight, netSerial);
        void Tell(AnimatedPlayerVisual.Action a, bool right)
        {
            netAction = (int)a + 1; netRight = right; netSerial = (netSerial + 1) & 7;
        }

        public void PlayKick(bool rightFoot = true, bool shot = false)
        {
            Tell(shot ? AnimatedPlayerVisual.Action.Shot : AnimatedPlayerVisual.Action.Pass, rightFoot);
            kickT = 0f; kickRight = rightFoot;
            if (animatedActive && animated != null) animated.PlayAction(shot ? AnimatedPlayerVisual.Action.Shot : AnimatedPlayerVisual.Action.Pass, rightFoot);
        }

        /// <summary>A fast ball arriving at a player who is about to take it down: the animated body plays its first-touch clip.</summary>
        public void PlayFirstTouch(bool rightFoot = true)
        {
            Tell(AnimatedPlayerVisual.Action.Receive, rightFoot);
            if (animatedActive && animated != null) animated.PlayAction(AnimatedPlayerVisual.Action.Receive, rightFoot);
        }
        public void PlayTackle()
        {
            Tell(AnimatedPlayerVisual.Action.Tackle, true);
            tackleT = 0f; kickRight = true;
            if (animatedActive && animated != null) animated.PlayAction(AnimatedPlayerVisual.Action.Tackle, true);
        }

        /// <summary>A keeper dives: sideways to the right or left of his own body.</summary>
        public void PlayDive(bool right)
        {
            Tell(AnimatedPlayerVisual.Action.Dive, right);
            if (animatedActive && animated != null) animated.PlayAction(AnimatedPlayerVisual.Action.Dive, right);
        }

        /// <summary>A keeper stops a shot: the primitive body swings a leg, the animated one plays a catch (high balls jump) when he held it.</summary>
        public void PlaySave(bool held, bool high)
        {
            if (held) Tell(high ? AnimatedPlayerVisual.Action.JumpCatch : AnimatedPlayerVisual.Action.Catch, true);
            if (animatedActive && animated != null)
            {
                if (held) animated.PlayAction(high ? AnimatedPlayerVisual.Action.JumpCatch : AnimatedPlayerVisual.Action.Catch, true);
            }
            else PlayKick(true);
        }

        /// <summary>
        /// Plays a goal reaction (the animated body only) and returns how many seconds the clip lasts, or 0 when this player has no animated
        /// body: the caller holds the player still for that long so the body is not carried away from its pose.
        /// </summary>
        public float PlayReaction(AnimatedPlayerVisual.Action reaction)
        {
            if (!animatedActive || animated == null) return 0f;
            Tell(reaction, true); // only a reaction the host really plays is shown to guests, since the host holds the player still for it
            return animated.PlayAction(reaction, true) ? animated.LastActionLength : 0f;
        }

        public void EndReaction() { if (animatedActive && animated != null) animated.EndAction(); }

        /// <summary>The victim of a foul goes down and gets back up (animated body only).</summary>
        public void PlayFall()
        {
            Tell(AnimatedPlayerVisual.Action.Fall, true);
            if (animatedActive && animated != null) animated.PlayAction(AnimatedPlayerVisual.Action.Fall, true);
        }

        void Update()
        {
            if (body == null || pc == null || animatedActive) return;
            float speed = pc.Velocity.magnitude;
            float targetAmp = speed > 0.25f ? Mathf.Clamp01(speed / 8f) * 50f + 8f : 0f;
            amp = Mathf.Lerp(amp, targetAmp, 1f - Mathf.Exp(-10f * Time.deltaTime));
            phase += speed * Time.deltaTime * 1.5f;

            float swing = Mathf.Sin(phase) * amp;
            float bendL = Mathf.Max(0f, Mathf.Sin(phase - 1.1f)) * amp * 1.3f;
            float bendR = Mathf.Max(0f, Mathf.Sin(phase + Mathf.PI - 1.1f)) * amp * 1.3f;

            float lean = Mathf.Clamp01(speed / 9f) * 12f;
            float dip = 0f;
            float kickHip = 0f, kickKnee = 0f;
            bool kicking = false, tackling = false;

            if (kickT >= 0f)
            {
                kickT += Time.deltaTime / 0.3f;
                if (kickT >= 1f) kickT = -1f;
                else
                {
                    kicking = true;
                    kickHip = kickT < 0.35f ? Mathf.Lerp(0f, 35f, kickT / 0.35f) : Mathf.Lerp(35f, -80f, (kickT - 0.35f) / 0.65f);
                    kickKnee = kickT < 0.35f ? 50f : Mathf.Lerp(50f, 8f, (kickT - 0.35f) / 0.65f);
                }
            }
            if (tackleT >= 0f)
            {
                tackleT += Time.deltaTime / 0.45f;
                if (tackleT >= 1f) tackleT = -1f;
                else
                {
                    tackling = true;
                    float s = Mathf.Sin(tackleT * Mathf.PI);
                    lean += 35f * s; dip = -0.28f * s;
                    kickHip = -85f * s; kickKnee = 5f;
                }
            }

            body.localRotation = Quaternion.Euler(lean, 0f, 0f);
            body.localPosition = new Vector3(0f, dip, 0f);

            ApplyLeg(hipL, kneeL, -swing, bendL, (kicking || tackling) && !kickRight, kickHip, kickKnee);
            ApplyLeg(hipR, kneeR, swing, bendR, (kicking || tackling) && kickRight, kickHip, kickKnee);
            shoulderL.localRotation = Quaternion.Euler(swing * 0.8f, 0f, 6f);
            shoulderR.localRotation = Quaternion.Euler(-swing * 0.8f, 0f, -6f);
        }

        static void ApplyLeg(Transform hip, Transform knee, float swing, float bend, bool override_, float kHip, float kKnee)
        {
            hip.localRotation = Quaternion.Euler(override_ ? kHip : swing, 0f, 0f);
            knee.localRotation = Quaternion.Euler(override_ ? kKnee : bend, 0f, 0f);
        }
    }
}
