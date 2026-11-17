using System.Collections.Generic;
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
            Object.DestroyImmediate(temp);

            // controller with one 1D blend tree on "Speed"; the thresholds are the measured speeds, so each clip plays at its own pace
            string ctrlPath = Out + "PlayerLocomotion.controller";
            AssetDatabase.DeleteAsset(ctrlPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            BlendTree tree;
            ctrl.CreateBlendTreeInController("Locomotion", out tree);
            tree.blendParameter = "Speed"; tree.useAutomaticThresholds = false;
            // Run and Sprint also appear sped up or slowed down so the game's whole speed range (jog 3.2-4.5, sprint 6-9.5 m/s) is covered by
            // gaits whose planted feet match the ground; a clip played at scale k travels k times faster, so its threshold is its speed times k
            var entries = new List<(string clip, float scale)>
            { ("Idle", 1f), ("Walk", 1f), ("Jog", 1f), ("Run", 1f), ("Run", 1.45f), ("Sprint", 0.8f), ("Sprint", 1f), ("Sprint", 1.3f) };
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
