using UnityEngine;

namespace FootballSim.FootballAI
{
    /// <summary>
    /// Data-driven formation. Slot 0 is the goalkeeper. Each slot: x = -1..1 across the pitch (left to right
    /// from the team's own perspective), y = 0 (own goal line) to 1 (halfway/attacking end).
    /// </summary>
    [CreateAssetMenu(menuName = "FootballSim/Formation")]
    public class FormationData : ScriptableObject
    {
        public string formationName = "4-4-2";
        public Vector2[] slots = Default442();

        public static readonly string[] PresetNames = { "4-4-2", "4-3-3", "4-2-3-1", "3-5-2", "5-3-2" };

        /// <summary>Create a runtime formation from a preset index (see PresetNames).</summary>
        public static FormationData Create(int preset)
        {
            var f = CreateInstance<FormationData>();
            preset = Mathf.Clamp(preset, 0, PresetNames.Length - 1);
            f.formationName = PresetNames[preset];
            f.slots = Slots(preset);
            return f;
        }

        public static Vector2[] Slots(int preset)
        {
            var gk = new Vector2(0f, 0.02f);
            switch (preset)
            {
                case 1: // 4-3-3
                    return new[] { gk,
                        new Vector2(-0.75f, 0.28f), new Vector2(-0.25f, 0.24f), new Vector2(0.25f, 0.24f), new Vector2(0.75f, 0.28f),
                        new Vector2(-0.5f, 0.5f), new Vector2(0f, 0.45f), new Vector2(0.5f, 0.5f),
                        new Vector2(-0.7f, 0.82f), new Vector2(0f, 0.88f), new Vector2(0.7f, 0.82f) };
                case 2: // 4-2-3-1
                    return new[] { gk,
                        new Vector2(-0.75f, 0.28f), new Vector2(-0.25f, 0.24f), new Vector2(0.25f, 0.24f), new Vector2(0.75f, 0.28f),
                        new Vector2(-0.3f, 0.42f), new Vector2(0.3f, 0.42f),
                        new Vector2(-0.7f, 0.64f), new Vector2(0f, 0.68f), new Vector2(0.7f, 0.64f),
                        new Vector2(0f, 0.88f) };
                case 3: // 3-5-2
                    return new[] { gk,
                        new Vector2(-0.5f, 0.25f), new Vector2(0f, 0.22f), new Vector2(0.5f, 0.25f),
                        new Vector2(-0.9f, 0.5f), new Vector2(-0.45f, 0.45f), new Vector2(0f, 0.5f), new Vector2(0.45f, 0.45f), new Vector2(0.9f, 0.5f),
                        new Vector2(-0.25f, 0.85f), new Vector2(0.25f, 0.85f) };
                case 4: // 5-3-2
                    return new[] { gk,
                        new Vector2(-0.9f, 0.32f), new Vector2(-0.45f, 0.25f), new Vector2(0f, 0.22f), new Vector2(0.45f, 0.25f), new Vector2(0.9f, 0.32f),
                        new Vector2(-0.5f, 0.5f), new Vector2(0f, 0.46f), new Vector2(0.5f, 0.5f),
                        new Vector2(-0.25f, 0.85f), new Vector2(0.25f, 0.85f) };
                default:
                    return Default442();
            }
        }

        public static Vector2[] Default442() => new[]
        {
            new Vector2(0f, 0.02f),                                                           // GK
            new Vector2(-0.75f, 0.28f), new Vector2(-0.25f, 0.24f), new Vector2(0.25f, 0.24f), new Vector2(0.75f, 0.28f), // DEF
            new Vector2(-0.8f, 0.55f), new Vector2(-0.28f, 0.5f), new Vector2(0.28f, 0.5f), new Vector2(0.8f, 0.55f),     // MID
            new Vector2(-0.25f, 0.85f), new Vector2(0.25f, 0.85f)                                                         // FWD
        };
    }
}
