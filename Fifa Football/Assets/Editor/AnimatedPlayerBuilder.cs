using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace FootballSim.EditorTools
{
    /// <summary>
    /// Builds the animated player from the Mixamo files in Assets/Art/Mixamo: measures how fast each locomotion clip really travels (from its
    /// planted foot), builds the speed-matched locomotion blend tree, and saves the player prefab under Assets/Resources/Animated (git-ignored with
    /// the raw files). The game loads it by name and falls back to the primitive body when it is missing, so a checkout without the files still runs.
    /// </summary>
    public static class AnimatedPlayerBuilder
    {
        const string Src = "Assets/Art/Mixamo/";
        const string Out = "Assets/Resources/Animated/";
        public static readonly string[] CycleClips = { "Idle", "Walk", "Jog", "Run", "Sprint" };
        /// <summary>Distribution of the planted-foot speeds behind the last <see cref="MeasureNaturalSpeed"/> result (for judging how steady the clip is).</summary>
        public static string LastDetail = "";

        static AnimationClip LoadClip(string file)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Src + file + ".fbx"))
            {
                var c = o as AnimationClip;
                if (c != null && !c.name.StartsWith("__preview")) return c;
            }
            return null;
        }

        /// <summary>
        /// The ground speed (m/s) a looping in-place locomotion clip corresponds to: the median backward speed of a foot while it is planted,
        /// in the model's own space. Zero for a clip without a planted foot moving back (idle).
        /// </summary>
        public static float MeasureNaturalSpeed(GameObject model, AnimationClip clip)
        {
            var animator = model.GetComponent<Animator>();
            // the toe bone is the planted contact point (the ankle rolls over the foot during stance and would under-read ground speed)
            var footL = animator.GetBoneTransform(HumanBodyBones.LeftToes) ?? animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var footR = animator.GetBoneTransform(HumanBodyBones.RightToes) ?? animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (footL == null || footR == null || clip.length < 0.05f) return 0f;
            int n = Mathf.Max(8, Mathf.RoundToInt(clip.length * 60f));
            float dt = clip.length / n;
            var zs = new float[2][]; var ys = new float[2][];
            for (int f = 0; f < 2; f++) { zs[f] = new float[n]; ys[f] = new float[n]; }
            AnimationMode.StartAnimationMode();
            try
            {
                for (int i = 0; i < n; i++)
                {
                    AnimationMode.SampleAnimationClip(model, clip, i * dt);
                    var pl = model.transform.InverseTransformPoint(footL.position); var pr = model.transform.InverseTransformPoint(footR.position);
                    zs[0][i] = pl.z; ys[0][i] = pl.y; zs[1][i] = pr.z; ys[1][i] = pr.y;
                }
            }
            finally { AnimationMode.StopAnimationMode(); }

            var speeds = new List<float>();
            for (int f = 0; f < 2; f++)
            {
                float minY = float.MaxValue; for (int i = 0; i < n; i++) minY = Mathf.Min(minY, ys[f][i]);
                for (int i = 0; i < n; i++)
                {
                    if (ys[f][i] > minY + 0.025f) continue;                      // only while the toe is on the ground
                    float dz = zs[f][(i + 1) % n] - zs[f][(i + n - 1) % n];       // cyclic central difference
                    float v = -dz / (2f * dt);
                    if (v > 0.3f) speeds.Add(v);                                  // moving backward = planted; a toe sliding forward near the ground is the swing foot
                }
            }
            if (speeds.Count == 0) { LastDetail = "no planted samples"; return 0f; }
            speeds.Sort();
            LastDetail = $"{speeds.Count}/{2 * n} planted samples, speed p10 {speeds[speeds.Count / 10]:0.00} p25 {speeds[speeds.Count / 4]:0.00} p50 {speeds[speeds.Count / 2]:0.00} p75 {speeds[speeds.Count * 3 / 4]:0.00} p90 {speeds[speeds.Count * 9 / 10]:0.00}";
            return Mathf.Max(0f, speeds[speeds.Count / 2]);
        }

        /// <summary>One-shot action clips: file name, which action it serves.</summary>
        public static readonly string[] ActionClips = { "SoccerPass", "Shot", "StrikeJog", "Receive", "SlideTackle", "FallOver", "GetUp", "KeeperDive", "KeeperCatch", "KeeperJumpCatch", "CelebrateA", "CelebrateB", "Defeated" };

        /// <summary>
        /// When the ball is struck in a kick clip: the moment a toe moves fastest (the foot is at full swing as it meets the ball) and which
        /// foot it is. Sampled at 120 Hz in the model's own space, so body travel in the clip does not matter.
        /// </summary>
        public static void MeasureContact(GameObject model, AnimationClip clip, out float time, out bool rightFoot, out float peakSpeed)
        {
            time = 0f; rightFoot = true; peakSpeed = 0f;
            var animator = model.GetComponent<Animator>();
            var toes = new[] { animator.GetBoneTransform(HumanBodyBones.LeftToes), animator.GetBoneTransform(HumanBodyBones.RightToes) };
            if (toes[0] == null || toes[1] == null) return;
            int n = Mathf.Max(8, Mathf.RoundToInt(clip.length * 120f));
            float dt = clip.length / n;
            var pos = new Vector3[2][]; pos[0] = new Vector3[n + 1]; pos[1] = new Vector3[n + 1];
            AnimationMode.StartAnimationMode();
            try
            {
                for (int i = 0; i <= n; i++)
                {
                    AnimationMode.SampleAnimationClip(model, clip, i * dt);
                    for (int f = 0; f < 2; f++) pos[f][i] = model.transform.InverseTransformPoint(toes[f].position);
                }
            }
            finally { AnimationMode.StopAnimationMode(); }
            for (int f = 0; f < 2; f++)
                for (int i = 1; i < n; i++)
                {
                    float v = (pos[f][i + 1] - pos[f][i - 1]).magnitude / (2f * dt);
                    if (v > 40f) continue; // a sampling glitch, not a foot (no foot moves at 144 km/h)
                    if (v > peakSpeed) { peakSpeed = v; time = i * dt; rightFoot = f == 1; }
                }
        }

        static Material MakeMaterial(string name, Color c)
        {
            string path = Out + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.color = c; if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(m);
            return m;
        }

        [MenuItem("Football/Animation/Build Animated Player")]
        public static void BuildMenu() { Debug.Log(Build()); }

        /// <summary>Builds (or rebuilds) the controller and prefab and returns a report of what was measured.</summary>
        public static string Build()
        {
            var report = new StringBuilder();
            var characterAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Src + "Character.fbx");
            if (characterAsset == null) return "Assets/Art/Mixamo/Character.fbx is missing; nothing built.";
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Animated")) AssetDatabase.CreateFolder("Assets/Resources", "Animated");

            var temp = (GameObject)Object.Instantiate(characterAsset);
            temp.hideFlags = HideFlags.HideAndDontSave;
            var tempAnimator = temp.GetComponent<Animator>() ?? temp.AddComponent<Animator>();
            tempAnimator.avatar = AssetDatabase.LoadAssetAtPath<Avatar>(Src + "Character.fbx") ?? FindAvatar(Src + "Character.fbx");

            var thresholds = new float[CycleClips.Length];
            for (int i = 0; i < CycleClips.Length; i++)
            {
                var clip = LoadClip(CycleClips[i]);
                if (clip == null) { report.AppendLine(CycleClips[i] + ": clip missing"); continue; }
                thresholds[i] = i == 0 ? 0f : MeasureNaturalSpeed(temp, clip);
                report.AppendLine($"{CycleClips[i]}: length {clip.length:0.00} s, natural ground speed {thresholds[i]:0.00} m/s ({LastDetail})");
            }
            var info = new FootballSim.Players.AnimatedClipInfo();
            foreach (var an in ActionClips)
            {
                var clip = LoadClip(an);
                if (clip == null) { report.AppendLine(an + ": clip missing"); continue; }
                MeasureContact(temp, clip, out float ct, out bool right, out float peak);
                if (an == "Receive") { ct = clip.length * 0.3f; right = true; } // a chest take-down has no kicking foot: the ball arrives about a third of the way in (judged by eye, not measured)
                info.clips.Add(new FootballSim.Players.AnimatedClipInfo.Entry { name = an, length = clip.length, contact = ct, rightFoot = right });
                report.AppendLine($"{an}: length {clip.length:0.00} s, contact at {ct:0.00} s with the {(right ? "right" : "left")} foot (toe peak {peak:0.0} m/s)");
            }
            File.WriteAllText(Out + "ClipInfo.json", JsonUtility.ToJson(info, true));
            Object.DestroyImmediate(temp);

            // controller with one 1D blend tree on "Speed"; the thresholds are the measured speeds, so each clip plays at its own pace
            string ctrlPath = Out + "PlayerLocomotion.controller";
            AssetDatabase.DeleteAsset(ctrlPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            BlendTree tree;
            ctrl.CreateBlendTreeInController("Locomotion", out tree);
            tree.blendParameter = "Speed"; tree.useAutomaticThresholds = false;
            // Run and Sprint also appear sped up or slowed down so the game's whole speed range (jog 4-5.6, sprint 7.5-11.9 m/s after PlayerData.PaceScale) is covered by
            // gaits whose planted feet match the ground; a clip played at scale k travels k times faster, so its threshold is its speed times k
            var entries = new List<(string clip, float scale)>
            { ("Idle", 1f), ("Walk", 1f), ("Jog", 1f), ("Run", 1f), ("Run", 1.45f), ("Sprint", 0.8f), ("Sprint", 1f), ("Sprint", 1.3f), ("Sprint", 1.6f) };
            var children = new List<ChildMotion>();
            foreach (var (name, scale) in entries)
            {
                int idx = System.Array.IndexOf(CycleClips, name);
                var clip = LoadClip(name);
                if (clip == null) continue;
                children.Add(new ChildMotion { motion = clip, threshold = thresholds[idx] * scale, timeScale = scale, directBlendParameter = "Speed" });
                report.AppendLine($"  blend child {name} x{scale:0.00} at {thresholds[idx] * scale:0.00} m/s");
            }
            children.Sort((a, b) => a.threshold.CompareTo(b.threshold));
            tree.children = children.ToArray();
            // action states (kicks, first touch): entered by code with a cross-fade at the moment the simulation acts, mirrored for the other
            // foot, and left by exit time back into locomotion
            ctrl.AddParameter("Mirror", AnimatorControllerParameterType.Bool);
            var sm = ctrl.layers[0].stateMachine;
            AnimatorState loco = null;
            foreach (var cs in sm.states) if (cs.state.name == "Locomotion") loco = cs.state;
            sm.defaultState = loco;
            var actionStates = new Dictionary<string, AnimatorState>();
            foreach (var an in ActionClips)
            {
                var clip = LoadClip(an);
                if (clip == null) continue;
                var st = sm.AddState(an);
                st.motion = clip; st.mirrorParameterActive = true; st.mirrorParameter = "Mirror"; st.writeDefaultValues = true;
                // the simulation does not hold a tackler or a fouled player down, so these play faster than the raw clip to be over about when
                // the player is free to move again (the sim's tackle cool-down is 0.9 s)
                st.speed = an == "KeeperDive" ? 2f : an == "SlideTackle" ? 1.9f : an == "FallOver" || an == "GetUp" ? 1.25f : 1f;
                actionStates[an] = st;
            }
            foreach (var kv in actionStates)
            {
                var st = kv.Value; var clip = (AnimationClip)st.motion;
                // a fall flows into the get-up, everything else back into locomotion
                var next = kv.Key == "FallOver" && actionStates.ContainsKey("GetUp") ? actionStates["GetUp"] : loco;
                var back = st.AddTransition(next);
                back.hasExitTime = true; back.exitTime = Mathf.Clamp01(1f - 0.18f / clip.length); back.hasFixedDuration = true; back.duration = 0.18f;
            }
            EditorUtility.SetDirty(ctrl);

            // prefab: the character, an Animator on the controller (no root motion; the game moves the player), URP materials
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(characterAsset);
            inst.name = "AnimatedPlayer";
            var animator = inst.GetComponent<Animator>() ?? inst.AddComponent<Animator>();
            animator.runtimeAnimatorController = ctrl;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            var body = MakeMaterial("AnimatedPlayer_Body", new Color(0.85f, 0.15f, 0.15f));
            var joints = MakeMaterial("AnimatedPlayer_Joints", new Color(0.12f, 0.12f, 0.14f));
            foreach (var r in inst.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int k = 0; k < mats.Length; k++) mats[k] = r.sharedMaterials[k] != null && r.sharedMaterials[k].name.ToLowerInvariant().Contains("joint") ? joints : body;
                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.updateWhenOffscreen = false;
            }
            string prefabPath = Out + "AnimatedPlayer.prefab";
            PrefabUtility.SaveAsPrefabAsset(inst, prefabPath);
            Object.DestroyImmediate(inst);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            report.AppendLine("Saved " + ctrlPath + " and " + prefabPath);
            return report.ToString();
        }

        static Avatar FindAvatar(string path)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path)) { var a = o as Avatar; if (a != null) return a; }
            return null;
        }
    }
}
