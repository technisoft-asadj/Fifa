using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using FootballSim.Ball;
using FootballSim.Core;
using FootballSim.Match;

namespace FootballSim.Venue
{
    /// <summary>
    /// Builds the ground around the pitch the way a real one looks: tiered stands full of fans (with a roof on the long sides), advertising boards,
    /// two dugouts with substitutes and staff, a coach in each technical area, substitutes warming up, linesmen, corner flags and floodlight towers.
    /// Everything is made from code (no downloaded assets) and merged into a handful of meshes so a phone can draw it. The crowd bobs, jumps when
    /// a goal goes in, and the benches drink water, talk, stretch, cheer or sit with their heads in their hands.
    /// </summary>
    public sealed class Stadium : MonoBehaviour
    {
        public const string PrefKey = "stadium_on";
        static bool? enabledCache;

        /// <summary>The surroundings can be switched off (for a slow phone); saved. On by default.</summary>
        public static bool Enabled
        {
            get
            {
                if (enabledCache == null) { try { enabledCache = PlayerPrefs.GetInt(PrefKey, 1) == 1; } catch { enabledCache = true; } }
                return enabledCache.Value;
            }
            set { enabledCache = value; try { PlayerPrefs.SetInt(PrefKey, value ? 1 : 0); PlayerPrefs.Save(); } catch { } }
        }

        public static Stadium Current { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook() { SceneManager.sceneLoaded += (s, m) => Spawn(); }

        static void Spawn()
        {
            if (!Enabled || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            if (FindFirstObjectByType<Stadium>() != null || FindFirstObjectByType<MatchManager>() == null) return;
            new GameObject("Stadium").AddComponent<Stadium>();
        }

        // ---- layout, metres. The pitch grass is 80 x 115 (lines at 34 and 52.5); the ball walls stand at 37 and 58.5 ----
        const float SideFront = 43f, EndFront = 63f;
        const int SideRows = 10, EndRows = 7;
        const float RowDepth = 0.85f, RowRise = 0.8f, SpectatorSpacing = 0.95f;
        const float BenchX = 40.6f;

        StadiumPalette palette;
        readonly List<GameObject> crowdGroups = new List<GameObject>();
        readonly List<float> crowdPhase = new List<float>();
        readonly List<Material> ownMaterials = new List<Material>();
        Material homeJacket, awayJacket;
        BenchCrew homeBench, awayBench;
        SidelineWalker homeCoach, awayCoach;
        float cheerT, cheerLen;
        Transform ballT;
        System.Random rng;
        MatchManager match;

        public bool CrowdCheering => cheerT > 0f;
        public int CrowdGroupCount => crowdGroups.Count;
        public int PeopleCount { get; private set; }
        public int BenchPeopleCount => (homeBench != null ? homeBench.people.Length : 0) + (awayBench != null ? awayBench.people.Length : 0);

        void Awake()
        {
            Current = this;
            rng = new System.Random(20260);
            palette = new StadiumPalette();
            var ballC = FindFirstObjectByType<BallController>();
            ballT = ballC != null ? ballC.transform : null;
            Build();
            match = FindFirstObjectByType<MatchManager>();
            if (match != null) match.GoalScored += OnGoal;
            RefreshKits();
        }

        void OnDestroy()
        {
            if (match != null) match.GoalScored -= OnGoal;
            if (Current == this) Current = null;
            StadiumPerson.ClearMaterials();
            foreach (var m in ownMaterials) if (m != null) Destroy(m);
        }

        /// <summary>The jackets of the benches follow the kits chosen on the start screen.</summary>
        public static void RefreshKits() { if (Current != null) Current.ApplyKits(); }

        void ApplyKits()
        {
            FootballSim.Players.TeamKits.Resolve(KitPalette.Load(), out var home, out var away, out _, out _);
            Tint(homeJacket, Color.Lerp(home.shirt, Color.black, 0.15f));
            Tint(awayJacket, Color.Lerp(away.shirt, Color.black, 0.15f));
        }

        static void Tint(Material m, Color c) { if (m == null) return; m.color = c; if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); }

        void OnGoal(int scoringTeam)
        {
            cheerLen = 4.5f; cheerT = cheerLen;
            var scoring = scoringTeam == 0 ? homeBench : awayBench; var conceding = scoringTeam == 0 ? awayBench : homeBench;
            if (scoring != null) scoring.SetMood(CrewMood.Cheering, 3.2f);
            if (conceding != null) conceding.SetMood(CrewMood.Dejected, 3.2f);
            var sc = scoringTeam == 0 ? homeCoach : awayCoach; var cc = scoringTeam == 0 ? awayCoach : homeCoach;
            if (sc != null) sc.SetMood(CrewMood.Cheering, 3.2f);
            if (cc != null) cc.SetMood(CrewMood.Dejected, 3.2f);
        }

        void Update()
        {
            float t = Time.time;
            if (cheerT > 0f) cheerT -= Time.deltaTime;
            float env = cheerT > 0f ? Mathf.Clamp01(cheerT / 1.2f) : 0f;
            for (int i = 0; i < crowdGroups.Count; i++)
            {
                float ph = crowdPhase[i];
                float y = Mathf.Sin(t * 1.6f + ph) * 0.012f;
                if (env > 0f) y += Mathf.Abs(Mathf.Sin(t * 6.5f + ph * 2.3f)) * 0.3f * env;
                var tr = crowdGroups[i].transform;
                var pos = tr.position; pos.y = y; tr.position = pos;
            }
        }

        // ---------------------------------------------------------------- building ----
        void Build()
        {
            var shared = new GameObject("Structure").transform; shared.SetParent(transform, false);
            var kit = new StadiumMeshKit(palette);
            var lamps = new StadiumMeshKit(palette);
            int concrete = palette.Get(new Color(0.55f, 0.56f, 0.6f)), concreteDark = palette.Get(new Color(0.38f, 0.39f, 0.43f));

            // ground around the pitch
            kit.Box(new Vector3(0f, -0.06f, 0f), new Vector3(190f, 0.08f, 220f), palette.Get(new Color(0.1f, 0.22f, 0.12f)), StadiumMeshKit.PY);

            // advertising boards just outside the lines: 0.9 m high, a run of coloured panels
            AdBoards(kit);

            // stands: the long sides get a roof
            Stand(kit, shared, new Vector3(SideFront, 0f, 0f), Vector3.forward, Vector3.right, 96f, SideRows, true, concrete, concreteDark, 0);
            Stand(kit, shared, new Vector3(-SideFront, 0f, 0f), Vector3.forward, Vector3.left, 96f, SideRows, true, concrete, concreteDark, 1);
            Stand(kit, shared, new Vector3(0f, 0f, EndFront), Vector3.right, Vector3.forward, 78f, EndRows, false, concrete, concreteDark, 2);
            Stand(kit, shared, new Vector3(0f, 0f, -EndFront), Vector3.right, Vector3.back, 78f, EndRows, false, concrete, concreteDark, 3);

            // corner flags and floodlight towers
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    CornerFlag(kit, new Vector3(sx * Pitch.HalfWidth, 0f, sz * Pitch.HalfLength));
                    Tower(kit, lamps, new Vector3(sx * 62f, 0f, sz * 74f), concreteDark);
                }

            Dugout(kit, new Vector3(BenchX, 0f, -9.5f), concreteDark);
            Dugout(kit, new Vector3(BenchX, 0f, 9.5f), concreteDark);

            var mat = NewMaterial(palette.Bake(), false);
            var go = new GameObject("StadiumShell"); go.transform.SetParent(shared, false);
            Attach(go, kit.Build("StadiumShell"), mat);
            var lampMat = NewMaterial(mat.GetTexture("_BaseMap"), true);
            var lampGo = new GameObject("Floodlights"); lampGo.transform.SetParent(shared, false);
            Attach(lampGo, lamps.Build("Floodlights"), lampMat);

            crowdMaterial = mat;
            BuildCrowd();
            BuildPeople();
        }

        Material crowdMaterial;

        Material NewMaterial(Texture tex, bool emissive)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(sh);
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            m.mainTexture = tex;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", new Color(2.2f, 2.1f, 1.8f));
            }
            ownMaterials.Add(m);
            return m;
        }

        static void Attach(GameObject go, Mesh mesh, Material mat)
        {
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        }

        /// <summary>Axis-aligned size for a box whose long side runs along <paramref name="along"/> and whose depth runs along <paramref name="outward"/>.</summary>
        static Vector3 SizeOf(Vector3 along, Vector3 outward, float a, float o, float h)
        {
            return new Vector3(Mathf.Abs(along.x) * a + Mathf.Abs(outward.x) * o, h, Mathf.Abs(along.z) * a + Mathf.Abs(outward.z) * o);
        }

        void AdBoards(StadiumMeshKit kit)
        {
            Color[] cols =
            {
                new Color(0.9f, 0.15f, 0.15f), new Color(0.1f, 0.35f, 0.85f), new Color(0.95f, 0.8f, 0.1f), new Color(0.12f, 0.12f, 0.14f),
                new Color(0.1f, 0.65f, 0.35f), new Color(0.95f, 0.95f, 0.95f), new Color(0.85f, 0.4f, 0.05f)
            };
            float bx = 38.4f, bz = 59.4f; int n = 0;
            for (float z = -56f; z < 56f; z += 4f, n++)
            {
                int c = palette.Get(cols[n % cols.Length]);
                kit.Box(new Vector3(bx, 0.45f, z + 2f), new Vector3(0.12f, 0.9f, 3.85f), c, StadiumMeshKit.All & ~StadiumMeshKit.PX);
                kit.Box(new Vector3(-bx, 0.45f, z + 2f), new Vector3(0.12f, 0.9f, 3.85f), palette.Get(cols[(n + 3) % cols.Length]), StadiumMeshKit.All & ~StadiumMeshKit.NX);
            }
            n = 0;
            for (float x = -34f; x < 34f; x += 4f, n++)
            {
                kit.Box(new Vector3(x + 2f, 0.45f, bz), new Vector3(3.85f, 0.9f, 0.12f), palette.Get(cols[(n + 1) % cols.Length]), StadiumMeshKit.All & ~StadiumMeshKit.PZ);
                kit.Box(new Vector3(x + 2f, 0.45f, -bz), new Vector3(3.85f, 0.9f, 0.12f), palette.Get(cols[(n + 4) % cols.Length]), StadiumMeshKit.All & ~StadiumMeshKit.NZ);
            }
        }

        void Stand(StadiumMeshKit kit, Transform parent, Vector3 front, Vector3 along, Vector3 outward, float length, int rows, bool roof, int concrete, int concreteDark, int index)
        {
            Color[] seatCols = { new Color(0.12f, 0.3f, 0.75f), new Color(0.75f, 0.12f, 0.12f), new Color(0.9f, 0.9f, 0.92f), new Color(0.95f, 0.78f, 0.1f) };
            for (int r = 0; r < rows; r++)
            {
                float top = (r + 1) * RowRise;
                // the concrete step
                kit.Box(front + outward * (r * RowDepth + RowDepth * 0.5f) + Vector3.up * (top * 0.5f), SizeOf(along, outward, length, RowDepth, top), r % 2 == 0 ? concrete : concreteDark);
                // seats: coloured sections of ten metres
                int sections = Mathf.CeilToInt(length / 12f);
                for (int s = 0; s < sections; s++)
                {
                    float a0 = -length * 0.5f + s * 12f, a1 = Mathf.Min(a0 + 11f, length * 0.5f);
                    if (a1 <= a0) continue;
                    int col = palette.Get(seatCols[(s + index + (r / 5)) % seatCols.Length]);
                    Vector3 c = front + along * ((a0 + a1) * 0.5f) + outward * (r * RowDepth + RowDepth * 0.62f) + Vector3.up * (top + 0.04f);
                    kit.Box(c, SizeOf(along, outward, a1 - a0, 0.42f, 0.08f), col);
                }
            }
            // back wall
            float wallH = rows * RowRise + 2.2f;
            kit.Box(front + outward * (rows * RowDepth + 0.3f) + Vector3.up * (wallH * 0.5f), SizeOf(along, outward, length + 1f, 0.6f, wallH), concreteDark);
            // side walls
            for (int e = -1; e <= 1; e += 2)
                kit.Box(front + along * (e * (length * 0.5f + 0.25f)) + outward * (rows * RowDepth * 0.5f) + Vector3.up * (wallH * 0.5f), SizeOf(along, outward, 0.5f, rows * RowDepth, wallH * 0.85f), concreteDark);
            if (roof)
            {
                float y = rows * RowRise + 6.5f;
                int roofCol = palette.Get(new Color(0.8f, 0.82f, 0.86f)), pillar = palette.Get(new Color(0.3f, 0.31f, 0.35f));
                // a canopy over the back half of the seats only, so a camera looking down still sees the front rows full of people
                kit.Box(front + outward * (rows * RowDepth * 0.8f) + Vector3.up * y, SizeOf(along, outward, length + 2f, rows * RowDepth * 0.5f, 0.3f), roofCol);
                for (float a = -length * 0.5f; a <= length * 0.5f + 0.1f; a += length / 6f)
                    kit.Box(front + along * a + outward * (rows * RowDepth + 0.2f) + Vector3.up * (y * 0.5f), SizeOf(along, outward, 0.5f, 0.5f, y), pillar);
            }
            // remember the layout so the crowd can be filled
            stands.Add(new StandInfo { front = front, along = along, outward = outward, length = length, rows = rows });
        }

        struct StandInfo { public Vector3 front, along, outward; public float length; public int rows; }
        readonly List<StandInfo> stands = new List<StandInfo>();

        void CornerFlag(StadiumMeshKit kit, Vector3 p)
        {
            kit.Box(p + Vector3.up * 0.8f, new Vector3(0.05f, 1.6f, 0.05f), palette.Get(Color.white));
            kit.Box(p + new Vector3(-Mathf.Sign(p.x) * 0.2f, 1.4f, 0f), new Vector3(0.4f, 0.28f, 0.02f), palette.Get(new Color(1f, 0.85f, 0.1f)));
        }

        void Tower(StadiumMeshKit kit, StadiumMeshKit lamps, Vector3 p, int concreteDark)
        {
            float h = 30f;
            kit.Box(p + Vector3.up * (h * 0.5f), new Vector3(1.1f, h, 1.1f), concreteDark);
            Vector3 head = p + Vector3.up * (h + 1.4f);
            kit.Box(head, new Vector3(9f, 3.4f, 0.5f), palette.Get(new Color(0.2f, 0.2f, 0.23f)));
            // a grid of lamps facing the pitch (the head is a flat panel turned toward the centre)
            Vector3 toCentre = new Vector3(-p.x, 0f, -p.z).normalized;
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 2; j++)
                    lamps.Box(head + new Vector3((i - 1.5f) * 2f, (j - 0.5f) * 1.5f, 0f) + toCentre * 0.35f, new Vector3(1.5f, 1.1f, 0.3f), palette.Get(new Color(1f, 0.98f, 0.9f)));
        }

        void Dugout(StadiumMeshKit kit, Vector3 c, int concreteDark)
        {
            int frame = palette.Get(new Color(0.25f, 0.27f, 0.32f)), bench = palette.Get(new Color(0.15f, 0.16f, 0.2f)), seat = palette.Get(new Color(0.9f, 0.55f, 0.1f));
            // floor slab, back wall, side panels, a bench and a thin roof frame (open at the front so the people stay visible)
            kit.Box(c + new Vector3(0.3f, 0.03f, 0f), new Vector3(3.4f, 0.06f, 9.6f), palette.Get(new Color(0.3f, 0.3f, 0.34f)), StadiumMeshKit.All);
            kit.Box(c + new Vector3(1.5f, 1.1f, 0f), new Vector3(0.12f, 2.2f, 9.6f), frame);
            for (int e = -1; e <= 1; e += 2)
            {
                kit.Box(c + new Vector3(0.3f, 1.1f, e * 4.8f), new Vector3(3.4f, 2.2f, 0.1f), frame);
                kit.Box(c + new Vector3(-1.35f, 1.1f, e * 4.8f), new Vector3(0.12f, 2.2f, 0.14f), frame);
            }
            kit.Box(c + new Vector3(0.3f, 2.25f, 0f), new Vector3(3.5f, 0.08f, 9.8f), frame, StadiumMeshKit.All & ~StadiumMeshKit.PY);
            kit.Box(c + new Vector3(0.05f, 0.4f, 0f), new Vector3(0.5f, 0.06f, 8.2f), seat);
            kit.Box(c + new Vector3(0.62f, 0.7f, 0f), new Vector3(0.1f, 0.55f, 8.2f), seat);
            kit.Box(c + new Vector3(0.05f, 0.2f, 0f), new Vector3(0.18f, 0.4f, 8.2f), bench);
        }

        // ---------------------------------------------------------------- crowd ----
        void BuildCrowd()
        {
            var root = new GameObject("Crowd").transform; root.SetParent(transform, false);
            Color[] shirts =
            {
                new Color(0.8f, 0.1f, 0.1f), new Color(0.8f, 0.1f, 0.1f), new Color(0.1f, 0.3f, 0.8f), new Color(0.1f, 0.3f, 0.8f), Color.white, new Color(0.95f, 0.8f, 0.1f),
                new Color(0.1f, 0.1f, 0.12f), new Color(0.1f, 0.5f, 0.3f), new Color(0.75f, 0.4f, 0.12f), new Color(0.3f, 0.32f, 0.38f), new Color(0.5f, 0.52f, 0.56f), new Color(0.2f, 0.45f, 0.55f)
            };
            Color[] skins = { new Color(0.96f, 0.80f, 0.69f), new Color(0.87f, 0.68f, 0.52f), new Color(0.72f, 0.53f, 0.38f), new Color(0.55f, 0.38f, 0.26f), new Color(0.36f, 0.24f, 0.17f) };
            const int groupsPerStand = 5;
            for (int si = 0; si < stands.Count; si++)
            {
                var st = stands[si];
                var kits = new StadiumMeshKit[groupsPerStand];
                for (int g = 0; g < groupsPerStand; g++) kits[g] = new StadiumMeshKit(palette);
                int cols = Mathf.FloorToInt(st.length / SpectatorSpacing);
                // the back of a spectator faces away from the pitch and is never seen; nor is the underside
                int hide = StadiumMeshKit.NY | (st.outward.x > 0.5f ? StadiumMeshKit.PX : st.outward.x < -0.5f ? StadiumMeshKit.NX : st.outward.z > 0.5f ? StadiumMeshKit.PZ : StadiumMeshKit.NZ);
                int mask = StadiumMeshKit.All & ~hide;
                for (int r = 0; r < st.rows; r++)
                {
                    float top = (r + 1) * RowRise;
                    for (int c = 0; c < cols; c++)
                    {
                        if (rng.NextDouble() < 0.13) continue; // an empty seat
                        float a = -st.length * 0.5f + (c + 0.5f) * SpectatorSpacing + (float)(rng.NextDouble() - 0.5) * 0.15f;
                        Vector3 baseP = st.front + st.along * a + st.outward * (r * RowDepth + RowDepth * 0.4f) + Vector3.up * top;
                        int g = (c + r * 2) % groupsPerStand;
                        var shirt = shirts[rng.Next(shirts.Length)];
                        var skin = skins[rng.Next(skins.Length)];
                        kits[g].Box(baseP + Vector3.up * 0.36f, SizeOf(st.along, st.outward, 0.62f, 0.4f, 0.62f), palette.Get(shirt), mask);
                        kits[g].Box(baseP + Vector3.up * 0.88f, SizeOf(st.along, st.outward, 0.3f, 0.3f, 0.3f), palette.Get(rng.NextDouble() < 0.12 ? shirts[rng.Next(shirts.Length)] : skin), mask);
                        PeopleCount++;
                    }
                }
                for (int g = 0; g < groupsPerStand; g++)
                {
                    var go = new GameObject($"Crowd{si}_{g}"); go.transform.SetParent(root, false);
                    Attach(go, kits[g].Build(go.name), crowdMaterial);
                    crowdGroups.Add(go); crowdPhase.Add(g * 1.3f + si * 0.7f);
                }
            }
        }

        // ---------------------------------------------------------------- people at the pitch side ----
        void BuildPeople()
        {
            var root = new GameObject("PitchSide").transform; root.SetParent(transform, false);
            homeJacket = StadiumPerson.NewMaterial(new Color(0.7f, 0.1f, 0.1f)); awayJacket = StadiumPerson.NewMaterial(new Color(0.1f, 0.3f, 0.8f));
            ownMaterials.Add(homeJacket); ownMaterials.Add(awayJacket);
            var dark = StadiumPerson.MaterialFor(new Color(0.12f, 0.13f, 0.17f));
            var tracksuit = StadiumPerson.MaterialFor(new Color(0.18f, 0.19f, 0.24f));
            var white = StadiumPerson.MaterialFor(new Color(0.95f, 0.95f, 0.95f));

            homeBench = Bench(root, "HomeBench", new Vector3(BenchX, 0f, -9.5f), homeJacket, tracksuit, 11);
            awayBench = Bench(root, "AwayBench", new Vector3(BenchX, 0f, 9.5f), awayJacket, tracksuit, 37);

            homeCoach = Walker(root, "HomeCoach", SidelineWalker.Role.Coach, StadiumPerson.Create(root, "HomeCoach", dark, dark, null, 2, false), 36.2f, -14f, -5f, -90f);
            awayCoach = Walker(root, "AwayCoach", SidelineWalker.Role.Coach, StadiumPerson.Create(root, "AwayCoach", dark, dark, null, 3, false), 36.2f, 5f, 14f, -90f);
            // two substitutes per team warming up on the line, away from the dugouts
            for (int i = 0; i < 2; i++)
            {
                var hp = StadiumPerson.Create(root, "HomeWarmUp" + i, homeJacket, StadiumPerson.MaterialFor(new Color(0.92f, 0.92f, 0.95f)), StadiumPerson.MaterialFor(new Color(0.7f, 0.1f, 0.1f)), 5 + i * 3);
                Walker(root, "HomeWarmUp" + i, SidelineWalker.Role.WarmUp, hp, 37.3f - i * 0.0f, -34f + i * 2f, -20f, -90f);
                var ap = StadiumPerson.Create(root, "AwayWarmUp" + i, awayJacket, StadiumPerson.MaterialFor(new Color(0.92f, 0.92f, 0.95f)), StadiumPerson.MaterialFor(new Color(0.1f, 0.3f, 0.8f)), 8 + i * 3);
                Walker(root, "AwayWarmUp" + i, SidelineWalker.Role.WarmUp, ap, 37.3f, 20f + i * 2f, 34f, -90f);
            }
            // linesmen along both touchlines
            var lineKit = StadiumPerson.MaterialFor(new Color(0.95f, 0.8f, 0.1f));
            Walker(root, "Linesman1", SidelineWalker.Role.Linesman, StadiumPerson.Create(root, "Linesman1", lineKit, dark, white, 7), 35.0f, -48f, 48f, -90f);
            Walker(root, "Linesman2", SidelineWalker.Role.Linesman, StadiumPerson.Create(root, "Linesman2", lineKit, dark, white, 12), -35.0f, -48f, 48f, 90f);
        }

        SidelineWalker Walker(Transform root, string n, SidelineWalker.Role role, StadiumPerson p, float x, float z0, float z1, float yaw)
        {
            var w = p.gameObject.AddComponent<SidelineWalker>();
            w.role = role; w.person = p; w.ball = ballT; w.x = x; w.zMin = z0; w.zMax = z1; w.faceYaw = yaw;
            p.Facing = yaw;
            return w;
        }

        BenchCrew Bench(Transform root, string n, Vector3 c, Material jacket, Material tracksuit, int seed)
        {
            var holder = new GameObject(n); holder.transform.SetParent(root, false);
            var people = new StadiumPerson[7];
            float[] zs = { -3.3f, -2.2f, -1.1f, 0f, 1.1f, 2.2f, 3.3f };
            for (int i = 0; i < 7; i++)
            {
                bool staff = i == 0 || i == 6;
                var p = StadiumPerson.Create(holder.transform, n + i, staff ? tracksuit : jacket, staff ? tracksuit : StadiumPerson.MaterialFor(new Color(0.15f, 0.15f, 0.2f)), null, seed + i * 3, false);
                p.transform.position = new Vector3(c.x + 0.05f, 0f, c.z + zs[i]);
                p.Facing = -90f;
                p.Relax(false);
                people[i] = p;
            }
            var crew = holder.AddComponent<BenchCrew>();
            crew.people = people; crew.ball = ballT;
            return crew;
        }
    }
}
