using System.Collections.Generic;
using UnityEngine;

namespace FootballSim.Players
{
    /// <summary>
    /// After a goal the scorer and the two nearest team-mates celebrate and the conceding keeper and two nearest defenders show frustration.
    /// Only players wearing the animated body take part, and only they are held still for the clip (so the body is not carried off its pose);
    /// with the simple bodies nothing changes. The goal pause is 3 s and the match resets players afterwards.
    /// </summary>
    public static class GoalReactions
    {
        public const float MaxHold = 2.8f;
        /// <summary>Names of the players who reacted to the last goal (for tests and diagnostics).</summary>
        public static readonly List<string> LastReactors = new List<string>();

        public static void Run(PlayerController scorer, int scoringTeam)
        {
            LastReactors.Clear();
            var all = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            Vector3 focus = scorer != null && scorer.TeamId == scoringTeam ? scorer.transform.position : Vector3.zero;
            bool haveFocus = scorer != null && scorer.TeamId == scoringTeam;
            if (haveFocus) React(scorer, AnimatedPlayerVisual.Action.CelebrateA);
            else focus = all.Length > 0 ? AverageTeamPosition(all, scoringTeam) : Vector3.zero;
            int cel = 0, sad = 0;
            foreach (var p in Nearest(all, focus, scoringTeam, true, scorer))
                if (cel++ < 2) React(p, AnimatedPlayerVisual.Action.CelebrateB);
            foreach (var p in Nearest(all, focus, 1 - scoringTeam, false, null))
                if (sad++ < 3) React(p, AnimatedPlayerVisual.Action.Defeated);
        }

        static Vector3 AverageTeamPosition(PlayerController[] all, int team)
        {
            Vector3 sum = Vector3.zero; int n = 0;
            foreach (var p in all) if (p.TeamId == team) { sum += p.transform.position; n++; }
            return n > 0 ? sum / n : Vector3.zero;
        }

        /// <summary>The team's players ordered by distance from the point (the keepers go last for the scoring side and first for the conceding side).</summary>
        static List<PlayerController> Nearest(PlayerController[] all, Vector3 focus, int team, bool scoringSide, PlayerController skip)
        {
            var list = new List<PlayerController>();
            foreach (var p in all) if (p != null && p.TeamId == team && p != skip) list.Add(p);
            list.Sort((a, b) =>
            {
                float da = (a.transform.position - focus).sqrMagnitude + (a.isGoalkeeper == scoringSide ? 1e6f : 0f);
                float db = (b.transform.position - focus).sqrMagnitude + (b.isGoalkeeper == scoringSide ? 1e6f : 0f);
                return da.CompareTo(db);
            });
            return list;
        }

        static void React(PlayerController p, AnimatedPlayerVisual.Action reaction)
        {
            var v = p.GetComponent<PlayerVisual>();
            if (v == null) return;
            float len = v.PlayReaction(reaction);
            if (len <= 0f) return;
            p.HoldStill = Mathf.Min(len, MaxHold);
            LastReactors.Add(p.name + ":" + reaction);
        }
    }
}
