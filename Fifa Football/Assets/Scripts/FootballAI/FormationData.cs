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

        public static Vector2[] Default442() => new[]
        {
            new Vector2(0f, 0.02f),                                                           // GK
            new Vector2(-0.75f, 0.28f), new Vector2(-0.25f, 0.24f), new Vector2(0.25f, 0.24f), new Vector2(0.75f, 0.28f), // DEF
            new Vector2(-0.8f, 0.55f), new Vector2(-0.28f, 0.5f), new Vector2(0.28f, 0.5f), new Vector2(0.8f, 0.55f),     // MID
            new Vector2(-0.25f, 0.85f), new Vector2(0.25f, 0.85f)                                                         // FWD
        };
    }
}
