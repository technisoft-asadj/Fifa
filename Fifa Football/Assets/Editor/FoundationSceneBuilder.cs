#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using FootballSim.Ball;
using FootballSim.Players;
using FootballSim.Inputs;
using FootballSim.Match;
using FootballSim.FootballAI;
using FootballSim.CameraSystem;

/// <summary>Builds the Phase 1 prototype scene (pitch, goals, ball, player, camera). Re-runnable.</summary>
public static class FoundationSceneBuilder
{
    [MenuItem("FootballSim/Build Prototype Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        System.IO.Directory.CreateDirectory("Assets/Materials");
        System.IO.Directory.CreateDirectory("Assets/ScriptableObjects");

        Material Mat(string n, Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.color = c; m.SetColor("_BaseColor", c);
            AssetDatabase.CreateAsset(m, "Assets/Materials/" + n + ".mat");
            return m;
        }
        var grass = Mat("Grass", new Color(0.18f, 0.55f, 0.2f));
        var white = Mat("White", Color.white);
        var ballMat = Mat("Ball", Color.white);
        var playerMat = Mat("PlayerHome", new Color(0.8f, 0.1f, 0.1f));

        var root = new GameObject("Match");
        var pitch = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pitch.name = "Pitch"; pitch.transform.SetParent(root.transform);
        pitch.transform.position = new Vector3(0, -0.5f, 0);
        pitch.transform.localScale = new Vector3(80, 1, 115);
        pitch.GetComponent<Renderer>().sharedMaterial = grass;

        void Line(string n, Vector3 p, Vector3 s)
        {
            var l = GameObject.CreatePrimitive(PrimitiveType.Cube);
            l.name = n; l.transform.SetParent(root.transform);
            l.transform.position = p; l.transform.localScale = s;
            l.GetComponent<Renderer>().sharedMaterial = white;
            Object.DestroyImmediate(l.GetComponent<Collider>());
        }
        float hw = 34f, hl = 52.5f;
        Line("Line_Left", new Vector3(-hw, 0.01f, 0), new Vector3(0.15f, 0.02f, hl * 2));
        Line("Line_Right", new Vector3(hw, 0.01f, 0), new Vector3(0.15f, 0.02f, hl * 2));
        Line("Line_Top", new Vector3(0, 0.01f, hl), new Vector3(hw * 2, 0.02f, 0.15f));
        Line("Line_Bottom", new Vector3(0, 0.01f, -hl), new Vector3(hw * 2, 0.02f, 0.15f));
        Line("Line_Half", new Vector3(0, 0.01f, 0), new Vector3(hw * 2, 0.02f, 0.15f));

        var mm = new GameObject("MatchManager").AddComponent<MatchManager>();
        mm.transform.SetParent(root.transform);

        void Goal(string n, float z, int defending)
        {
            var g = new GameObject(n); g.transform.SetParent(root.transform);
            g.transform.position = new Vector3(0, 0, z);
            float dir = Mathf.Sign(z); float gw = 7.32f, gh = 2.44f, d = 2f;
            void Post(string pn, Vector3 lp, Vector3 s)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                p.name = pn; p.transform.SetParent(g.transform, false);
                p.transform.localPosition = lp; p.transform.localScale = s;
                p.GetComponent<Renderer>().sharedMaterial = white;
            }
            Post("PostL", new Vector3(-gw / 2, gh / 2, 0), new Vector3(0.12f, gh, 0.12f));
            Post("PostR", new Vector3(gw / 2, gh / 2, 0), new Vector3(0.12f, gh, 0.12f));
            Post("Crossbar", new Vector3(0, gh, 0), new Vector3(gw, 0.12f, 0.12f));
            var trig = new GameObject("Trigger"); trig.transform.SetParent(g.transform, false);
            trig.transform.localPosition = new Vector3(0, gh / 2, dir * (d / 2f + 0.2f));
            var bc = trig.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(gw - 0.3f, gh - 0.1f, d);
            var gt = trig.AddComponent<GoalTrigger>(); gt.defendingTeam = defending; gt.match = mm;
            void Wall(string wn, Vector3 lp, Vector3 s)
            {
                var w = new GameObject(wn); w.transform.SetParent(g.transform, false);
                w.transform.localPosition = lp; w.AddComponent<BoxCollider>().size = s;
            }
            Wall("NetBack", new Vector3(0, gh / 2, dir * (d + 0.2f)), new Vector3(gw, gh, 0.2f));
            Wall("NetL", new Vector3(-gw / 2 - 0.1f, gh / 2, dir * d / 2f), new Vector3(0.2f, gh, d));
            Wall("NetR", new Vector3(gw / 2 + 0.1f, gh / 2, dir * d / 2f), new Vector3(0.2f, gh, d));
            Wall("NetTop", new Vector3(0, gh + 0.1f, dir * d / 2f), new Vector3(gw, 0.2f, d));
        }
        Goal("Goal_Away", hl, 1);
        Goal("Goal_Home", -hl, 0);

        void Boundary(string n, Vector3 p, Vector3 s)
        {
            var w = new GameObject(n); w.transform.SetParent(root.transform);
            w.transform.position = p; w.AddComponent<BoxCollider>().size = s;
        }
        Boundary("Wall_L", new Vector3(-hw - 3, 2, 0), new Vector3(1, 6, hl * 2 + 14));
        Boundary("Wall_R", new Vector3(hw + 3, 2, 0), new Vector3(1, 6, hl * 2 + 14));
        Boundary("Wall_T", new Vector3(0, 2, hl + 6), new Vector3(hw * 2 + 8, 6, 1));
        Boundary("Wall_B", new Vector3(0, 2, -hl - 6), new Vector3(hw * 2 + 8, 6, 1));

        var ballGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballGo.name = "Ball"; ballGo.transform.position = new Vector3(0, 0.12f, 0);
        ballGo.transform.localScale = Vector3.one * 0.22f;
        ballGo.GetComponent<Renderer>().sharedMaterial = ballMat;
        ballGo.AddComponent<Rigidbody>();
        var bs = ScriptableObject.CreateInstance<BallSettings>();
        AssetDatabase.CreateAsset(bs, "Assets/ScriptableObjects/BallSettings.asset");
        var ball = ballGo.AddComponent<BallController>();
        var pm = new PhysicsMaterial("Ball") { bounciness = 0.65f, dynamicFriction = 0.5f, staticFriction = 0.6f, bounceCombine = PhysicsMaterialCombine.Maximum };
        AssetDatabase.CreateAsset(pm, "Assets/ScriptableObjects/BallPhysics.asset");
        ballGo.GetComponent<SphereCollider>().sharedMaterial = pm;
        var so = new SerializedObject(ball);
        so.FindProperty("settings").objectReferenceValue = bs; so.ApplyModifiedProperties();

        var pd = ScriptableObject.CreateInstance<PlayerData>(); pd.displayName = "Test Player";
        AssetDatabase.CreateAsset(pd, "Assets/ScriptableObjects/TestPlayer.asset");
        var formation = ScriptableObject.CreateInstance<FormationData>();
        AssetDatabase.CreateAsset(formation, "Assets/ScriptableObjects/Formation_442.asset");
        var markerMat = Mat("ControlMarker", Color.yellow);

        var cam = Camera.main;
        cam.transform.position = new Vector3(0, 18, -28);
        cam.transform.rotation = Quaternion.Euler(35, 0, 0); cam.fieldOfView = 45;
        var bcam = cam.gameObject.AddComponent<BroadcastCamera>();

        var humanInput = new GameObject("HumanInput").AddComponent<PlayerInputSource>();
        humanInput.transform.SetParent(root.transform);

        TeamController MakeTeam(string teamName, int id, float attackDir, bool human, Color kit, Color gkKit)
        {
            var tgo = new GameObject(teamName); tgo.transform.SetParent(root.transform);
            var team = tgo.AddComponent<TeamController>();
            team.teamId = id; team.attackDir = attackDir; team.humanControlled = human;
            team.ball = ball; team.match = mm; team.formation = formation; team.humanInput = humanInput;
            var kitMat = Mat(teamName + "_Kit", kit); var gkMat = Mat(teamName + "_GK", gkKit);
            for (int i = 0; i < formation.slots.Length; i++)
            {
                bool gk = i == 0;
                var pl = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                pl.name = teamName + "_" + (i + 1); pl.transform.SetParent(tgo.transform);
                pl.transform.position = new Vector3(0, 1, 0);
                Object.DestroyImmediate(pl.GetComponent<CapsuleCollider>());
                pl.GetComponent<Renderer>().sharedMaterial = gk ? gkMat : kitMat;
                var cc = pl.AddComponent<CharacterController>();
                cc.height = 1.8f; cc.radius = 0.35f; cc.center = Vector3.zero; cc.skinWidth = 0.03f;
                var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
                nose.name = "FacingMarker"; nose.transform.SetParent(pl.transform, false);
                nose.transform.localPosition = new Vector3(0, 0.5f, 0.4f); nose.transform.localScale = new Vector3(0.25f, 0.2f, 0.3f);
                Object.DestroyImmediate(nose.GetComponent<Collider>());
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "ControlMarker"; marker.transform.SetParent(pl.transform, false);
                marker.transform.localPosition = new Vector3(0, 1.6f, 0); marker.transform.localScale = Vector3.one * 0.5f;
                marker.GetComponent<Renderer>().sharedMaterial = markerMat;
                Object.DestroyImmediate(marker.GetComponent<Collider>());

                var pc = pl.AddComponent<PlayerController>();
                var pso = new SerializedObject(pc);
                pso.FindProperty("data").objectReferenceValue = pd;
                pso.FindProperty("ball").objectReferenceValue = ball;
                pso.FindProperty("cameraReference").objectReferenceValue = cam.transform;
                pso.FindProperty("controlMarker").objectReferenceValue = marker;
                if (gk) pso.FindProperty("controlRadius").floatValue = 1.8f;
                pso.FindProperty("isGoalkeeper").boolValue = gk;
                pso.ApplyModifiedProperties();
                var ai = pl.AddComponent<PlayerAI>();
                ai.team = team; ai.slot = i;
                team.players.Add(pc); team.brains.Add(ai);
            }
            return team;
        }
        var home = MakeTeam("Home", 0, 1f, true, new Color(0.85f, 0.1f, 0.1f), new Color(0.9f, 0.8f, 0.1f));
        var away = MakeTeam("Away", 1, -1f, false, new Color(0.1f, 0.25f, 0.85f), new Color(0.1f, 0.8f, 0.3f));
        home.cameraRig = bcam;
        foreach (var b in home.brains) b.opponents = away;
        foreach (var b in away.brains) b.opponents = home;

        var cso = new SerializedObject(bcam);
        cso.FindProperty("ball").objectReferenceValue = ballGo.transform;
        cso.FindProperty("player").objectReferenceValue = home.players[9].transform; cso.ApplyModifiedProperties();
        var mso = new SerializedObject(mm);
        mso.FindProperty("ball").objectReferenceValue = ball;
        var teamsProp = mso.FindProperty("teams");
        teamsProp.arraySize = 2;
        teamsProp.GetArrayElementAtIndex(0).objectReferenceValue = home;
        teamsProp.GetArrayElementAtIndex(1).objectReferenceValue = away;
        mso.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/FootballPrototype.unity");
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        list.RemoveAll(s => s.path == "Assets/Scenes/FootballPrototype.unity");
        list.Insert(0, new EditorBuildSettingsScene("Assets/Scenes/FootballPrototype.unity", true));
        EditorBuildSettings.scenes = list.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("FootballSim: prototype scene built.");
    }
}
#endif
