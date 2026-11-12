using UnityEngine;
using FootballSim.Core;
using FootballSim.Inputs;
using FootballSim.Players;

namespace FootballSim.FootballAI
{
    /// <summary>
    /// Per-player decision making. Produces input for PlayerController (movement execution is shared with humans).
    /// Tactical positioning comes from TeamController; this class only decides what to do right now.
    /// </summary>
    public class PlayerAI : MonoBehaviour
    {
        public TeamController team;
        public TeamController opponents;
        public int slot;
        public AIInputSource Input { get; } = new AIInputSource();

        PlayerController pc;
        float actionCooldown;
        float holdTimer;
        // Tuned from the AI-vs-AI balance measurement (see progress.md, KB-018)
        const float BuildUpPassAfterSeconds = 1.6f;
        const float ShootRangeScale = 1.25f;
        float nextThink;
        Vector3 chaseTarget;
        AiTuning Tune => AiTuning.Current;

        Vector3 Jitter(Vector3 dir)
        {
            float e = Tune.aimErrorDeg;
            return Quaternion.Euler(0f, Random.Range(-e, e), 0f) * dir;
        }

        void Awake() { pc = GetComponent<PlayerController>(); }

        void Update()
        {
            if (team == null || team.ball == null) return;
            actionCooldown -= Time.deltaTime;
            if (pc.isGoalkeeper) Goalkeeper(); else Outfield();
        }

        Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        void MoveToward(Vector3 target, float sprintDistance)
        {
            Vector3 to = Flat(target - transform.position);
            float d = to.magnitude;
            Vector3 dir = d > 0.35f ? to / Mathf.Max(d, 1f) * Mathf.Clamp01(d) : Vector3.zero;
            Input.MoveVector = new Vector2(dir.x, dir.z);
            Input.SprintFlag = d > sprintDistance;
        }

        void Outfield()
        {
            Vector3 ballPos = Flat(team.ball.transform.position);
            Vector3 pos = transform.position;
            float ballDist = (ballPos - Flat(pos)).magnitude;
            bool isChaser = team.Chaser == pc;
            // The ball runs ahead of a dribbler between touches, so allow it a few metres before counting possession as lost
            if (!isChaser || ballDist > 3.5f) holdTimer = 0f;

            // Kick-off: the team that is not kicking off waits in its own half until the ball is played
            if (team.match != null && team.match.KickoffPending && team.match.KickoffTeam != team.teamId)
            {
                MoveToward(team.HomePosition(slot, true), 10f);
                return;
            }

            if (!isChaser)
            {
                MoveToward(team.HomePosition(slot), 10f);
                return;
            }

            if (ballDist > 1.4f)
            {
                // Run to where the ball will be shortly; slower difficulties re-evaluate less often
                if (Time.time >= nextThink)
                {
                    chaseTarget = ballPos + Flat(team.ball.Velocity) * 0.25f;
                    nextThink = Time.time + Tune.reactionDelay;
                }
                MoveToward(chaseTarget, 3f);
                TryTackleNearBall(ballDist);
                return;
            }

            // Has the ball: decide shoot / pass / dribble
            holdTimer += Time.deltaTime;
            Vector3 goal = team.OpponentGoalPos;
            float goalDist = (goal - Flat(pos)).magnitude;
            Vector3 toGoal = Flat(goal - pos).normalized;

            if (actionCooldown <= 0f && goalDist < Tune.shootRange * ShootRangeScale && Vector3.Dot(transform.forward, toGoal) > 0.35f)
            {
                Vector3 target = goal + new Vector3(Random.Range(-2.8f, 2.8f), 0f, 0f);
                Input.QueueShoot(Jitter((target - Flat(pos)).normalized), Mathf.Clamp(0.55f + goalDist / 50f, 0.5f, 1f));
                actionCooldown = 1.2f;
                return;
            }

            // Pass when pressed, or after holding the ball for a while (build-up play) so the team moves the ball around
            if (actionCooldown <= 0f && (OpponentNear(pos, 3f) || holdTimer > BuildUpPassAfterSeconds))
            {
                var mate = BestPassTarget(pos);
                if (mate != null)
                {
                    holdTimer = 0f;
                    Vector3 mp = Flat(mate.transform.position + mate.Velocity * 0.5f);
                    float dist = (mp - Flat(pos)).magnitude;
                    float speed = Mathf.Clamp(6f + dist * 0.45f, 8f, pc.Data.MaxPassSpeed);
                    Input.QueuePass(Jitter((mp - Flat(pos)).normalized), speed / pc.Data.MaxPassSpeed);
                    actionCooldown = 1.0f;
                    return;
                }
            }

            // Dribble toward goal, drifting around the nearest opponent
            Vector3 dribble = toGoal;
            var opp = NearestOpponent(pos, out float od);
            if (opp != null && od < 4f)
            {
                Vector3 away = Flat(pos - opp.transform.position).normalized;
                dribble = (toGoal + away * 0.8f).normalized;
            }
            Input.MoveVector = new Vector2(dribble.x, dribble.z);
            Input.SprintFlag = true;
        }

        void TryTackleNearBall(float ballDist)
        {
            if (ballDist > 2.4f || actionCooldown > 0f) return;
            // Only lunge when the ball is actually in reach and in front: a tackle that misses but connects with a man is a foul
            Vector3 toBall = Flat(team.ball.transform.position - transform.position);
            if (ballDist > 1.7f || Vector3.Dot(transform.forward, toBall.normalized) < 0.4f) return;
            // Tackle only if an opponent is controlling the ball (near it)
            foreach (var o in opponents.players)
            {
                if (!o.gameObject.activeSelf) continue; // sent off
                if ((Flat(o.transform.position) - Flat(team.ball.transform.position)).magnitude < 1.5f)
                {
                    Input.QueueTackle();
                    actionCooldown = Tune.tackleCooldown;
                    return;
                }
            }
        }

        bool OpponentNear(Vector3 pos, float r) { NearestOpponent(pos, out float d); return d < r; }

        PlayerController NearestOpponent(Vector3 pos, out float dist)
        {
            PlayerController best = null; dist = float.MaxValue;
            foreach (var o in opponents.players)
            {
                if (!o.gameObject.activeSelf) continue; // sent off
                float d = (Flat(o.transform.position) - Flat(pos)).magnitude;
                if (d < dist) { dist = d; best = o; }
            }
            return best;
        }

        PlayerController BestPassTarget(Vector3 pos)
        {
            PlayerController best = null; float bestScore = float.MinValue;
            // AI does not pass to a team-mate who is offside right now
            var defZ = new System.Collections.Generic.List<float>();
            foreach (var o in opponents.players) if (o.gameObject.activeSelf) defZ.Add(o.transform.position.z);
            float line = FootballSim.Match.OffsideRule.SecondLastDefenderZ(team.attackDir, defZ);
            foreach (var m in team.players)
            {
                if (m == pc || m.isGoalkeeper || !m.gameObject.activeSelf) continue;
                if (FootballSim.Match.OffsideRule.IsOffside(team.attackDir, m.transform.position.z, team.ball.transform.position.z, line)) continue;
                Vector3 d = Flat(m.transform.position - pos);
                float dist = d.magnitude;
                if (dist < 6f || dist > 35f) continue;
                float progress = d.z * team.attackDir;
                NearestOpponent(m.transform.position, out float freeDist);
                float score = progress * 0.5f - dist * 0.15f + Mathf.Min(freeDist, 8f) * 1.5f;
                if (score > bestScore) { bestScore = score; best = m; }
            }
            return best;
        }

        void Goalkeeper()
        {
            Vector3 ballPos = Flat(team.ball.transform.position);
            Vector3 ballVel = Flat(team.ball.Velocity);
            float lineZ = team.OwnGoalZ + team.attackDir * 1.5f;
            float dir = team.attackDir;

            float x = Mathf.Clamp(ballPos.x * 0.35f, -3f, 3f);
            float z = lineZ + dir * Mathf.Clamp(Mathf.Abs(ballPos.z - team.OwnGoalZ) * 0.06f, 0f, 3.5f);
            bool shotIncoming = ballVel.z * dir < -5f && Mathf.Abs(ballPos.z - team.OwnGoalZ) < 30f;
            if (shotIncoming)
            {
                float t = (lineZ - ballPos.z) / ballVel.z;
                x = Mathf.Clamp(ballPos.x + ballVel.x * t, -Pitch.GoalHalfWidth - 1f, Pitch.GoalHalfWidth + 1f);
                z = lineZ;
            }

            float ballDist = (ballPos - Flat(transform.position)).magnitude;
            if (ballDist < 1.7f && actionCooldown <= 0f)
            {
                // Save / collect and clear to the side-front
                Vector3 clear = new Vector3(Random.Range(-0.8f, 0.8f), 0f, dir).normalized;
                Input.QueueShoot(clear, 0.6f);
                actionCooldown = 0.8f;
            }

            MoveToward(new Vector3(x, 0f, z), shotIncoming ? 0.1f : 4f);
            if (shotIncoming) Input.SprintFlag = true;
        }
    }
}
