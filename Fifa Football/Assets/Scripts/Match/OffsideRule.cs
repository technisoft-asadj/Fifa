using System.Collections.Generic;

namespace FootballSim.Match
{
    /// <summary>
    /// Pure offside geometry. Home (team 0) attacks +Z, away attacks -Z.
    /// A receiver is offside when, at the moment the ball is played, he is in the opponent half and
    /// nearer the goal line than both the ball and the second-last opponent (the keeper counts as one).
    /// </summary>
    public static class OffsideRule
    {
        /// <summary>Metres of benefit of the doubt for the attacker.</summary>
        public const float Tolerance = 0.3f;

        /// <summary>World Z of the second-last defender (second nearest to his own goal line); the only one if there is one; 0 if none.</summary>
        public static float SecondLastDefenderZ(int attackTeam, IEnumerable<float> defenderZs)
        {
            return SecondLastDefenderZ(attackTeam == 0 ? 1f : -1f, defenderZs);
        }

        /// <summary>Same, for a team attacking along attackSign (+1 = +Z, -1 = -Z).</summary>
        public static float SecondLastDefenderZ(float attackSign, IEnumerable<float> defenderZs)
        {
            float s = attackSign;
            float first = float.MinValue, second = float.MinValue;
            foreach (var z in defenderZs)
            {
                float v = z * s;
                if (v > first) { second = first; first = v; }
                else if (v > second) second = v;
            }
            if (first == float.MinValue) return 0f;
            return (second == float.MinValue ? first : second) * s;
        }

        public static bool IsOffside(int attackTeam, float receiverZ, float ballZ, float secondLastDefenderZ)
        {
            return IsOffside(attackTeam == 0 ? 1f : -1f, receiverZ, ballZ, secondLastDefenderZ);
        }

        public static bool IsOffside(float attackSign, float receiverZ, float ballZ, float secondLastDefenderZ)
        {
            float s = attackSign;
            float r = receiverZ * s;
            return r > 0f && r > ballZ * s + Tolerance && r > secondLastDefenderZ * s + Tolerance;
        }
    }
}
