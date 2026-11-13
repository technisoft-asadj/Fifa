using UnityEngine;
using FootballSim.Core;

namespace FootballSim.Match
{
    /// <summary>Per-team match statistics. Index 0 = home, 1 = away. Plain data, no Unity lifecycle.</summary>
    public class MatchStats
    {
        public readonly float[] possessionSeconds = new float[2];
        public readonly int[] shots = new int[2];
        public readonly int[] shotsOnTarget = new int[2];
        public readonly int[] passes = new int[2];
        public readonly int[] corners = new int[2];
        /// <summary>Shots stopped by each team's goalkeeper (held or parried).</summary>
        public readonly int[] saves = new int[2];
        public readonly int[] offsides = new int[2];

        /// <summary>Share of possession in whole percent; 50 when nobody has had the ball yet.</summary>
        public int PossessionPercent(int team)
        {
            float total = possessionSeconds[0] + possessionSeconds[1];
            if (total <= 0f) return 50;
            int home = Mathf.RoundToInt(possessionSeconds[0] / total * 100f);
            return team == 0 ? home : 100 - home;
        }

        public void AddPossession(int team, float seconds)
        {
            if (team == 0 || team == 1) possessionSeconds[team] += seconds;
        }

        public void AddStrike(int team, bool isShot, Vector3 from, Vector3 dir)
        {
            AddStrike(team, isShot, from, dir, team == 0 ? 1f : -1f);
        }

        /// <summary>attackSign = +1 when the team currently attacks +Z, -1 when it attacks -Z (sides swap at half-time).</summary>
        public void AddStrike(int team, bool isShot, Vector3 from, Vector3 dir, float attackSign)
        {
            if (team != 0 && team != 1) return;
            if (!isShot) { passes[team]++; return; }
            shots[team]++;
            if (IsOnTarget(from, dir, attackSign)) shotsOnTarget[team]++;
        }

        public static bool IsOnTarget(Vector3 from, Vector3 dir, float attackSign)
        {
            float goalZ = attackSign * Pitch.HalfLength;
            float dz = goalZ - from.z;
            if (Mathf.Abs(dir.z) < 1e-4f || Mathf.Sign(dir.z) != Mathf.Sign(dz)) return false;
            float x = from.x + dir.x * (dz / dir.z);
            return Mathf.Abs(x) <= Pitch.GoalHalfWidth;
        }

        /// <summary>
        /// True when a strike from 'from' along 'dir' would cross the opponent goal line between the posts.
        /// Flat check: height, keeper and curve are ignored. Home (0) attacks +Z.
        /// </summary>
        public static bool IsOnTarget(Vector3 from, Vector3 dir, int team)
        {
            float goalZ = team == 0 ? Pitch.HalfLength : -Pitch.HalfLength;
            float dz = goalZ - from.z;
            if (Mathf.Abs(dir.z) < 1e-4f || Mathf.Sign(dir.z) != Mathf.Sign(dz)) return false;
            float x = from.x + dir.x * (dz / dir.z);
            return Mathf.Abs(x) <= Pitch.GoalHalfWidth;
        }
    }
}
