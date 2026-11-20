using UnityEngine;

namespace FootballSim.Venue
{
    /// <summary>What the people at the pitch side are doing after a goal.</summary>
    public enum CrewMood { Normal, Cheering, Dejected }

    /// <summary>
    /// The dugout: substitutes and staff on a bench. They watch the ball, now and then one takes a drink, two turn to talk, one stretches or claps;
    /// when their side scores they leap up and cheer, when it concedes they sit with their heads in their hands.
    /// </summary>
    public sealed class BenchCrew : MonoBehaviour
    {
        enum Act { None, Drink, Talk, Stretch, Clap, Shout }

        public StadiumPerson[] people;
        public Transform ball;
        CrewMood mood; float moodT;
        Act act; int who, partner; float actT, actLen, nextAct = 2f;
        float clock;

        public void SetMood(CrewMood m, float seconds) { mood = m; moodT = seconds; }

        void Update()
        {
            if (people == null) return;
            float dt = Time.deltaTime; clock += dt;
            if (moodT > 0f) { moodT -= dt; if (moodT <= 0f) mood = CrewMood.Normal; }
            if (mood == CrewMood.Normal) PickActivity(dt);
            else act = Act.None;

            for (int i = 0; i < people.Length; i++)
            {
                var p = people[i];
                if (p == null) continue;
                p.Relax(false);
                p.Rate = 9f;
                float ph = clock * 1.3f + i * 1.7f;
                if (mood == CrewMood.Cheering) Cheer(p, i);
                else if (mood == CrewMood.Dejected) Dejected(p, i);
                else
                {
                    if (ball != null) p.Watch(ball.position, 0.9f);
                    p.Target[StadiumPerson.Spine] = 4f + Mathf.Sin(ph) * 2f + (i % 3 == 0 ? 6f : 0f); // leaning forward a little, breathing
                    Overlay(p, i);
                }
                p.BottleVisible = mood == CrewMood.Normal && act == Act.Drink && i == who && actT > 0.3f && actT < actLen - 0.3f;
            }
        }

        void PickActivity(float dt)
        {
            if (act != Act.None)
            {
                actT += dt;
                if (actT >= actLen) { act = Act.None; nextAct = Random.Range(3f, 8f); }
                return;
            }
            nextAct -= dt;
            if (nextAct > 0f) return;
            who = Random.Range(0, people.Length);
            float r = Random.value;
            act = r < 0.34f ? Act.Drink : r < 0.66f ? Act.Talk : r < 0.80f ? Act.Stretch : r < 0.92f ? Act.Clap : Act.Shout;
            partner = who + (who + 1 < people.Length && (who == 0 || Random.value < 0.5f) ? 1 : -1);
            partner = Mathf.Clamp(partner, 0, people.Length - 1);
            actT = 0f;
            actLen = act == Act.Drink ? 3.2f : act == Act.Talk ? Random.Range(3.5f, 6f) : act == Act.Stretch ? 3f : act == Act.Clap ? 2.5f : 2.6f;
        }

        void Overlay(StadiumPerson p, int i)
        {
            if (act == Act.None) return;
            float t = actT, len = actLen;
            float env = Mathf.Clamp01(Mathf.Min(t, len - t) / 0.5f); // ease in and out of the action
            if (i == who)
            {
                switch (act)
                {
                    case Act.Drink:
                        // bring the bottle up, lean the head back, drink, bring it down
                        p.Target[StadiumPerson.ShR] = Mathf.Lerp(12f, -95f, env);
                        p.Target[StadiumPerson.ShRz] = 12f;
                        p.Target[StadiumPerson.ElR] = Mathf.Lerp(-25f, -125f, env);
                        p.Target[StadiumPerson.HeadPitch] = Mathf.Lerp(0f, -22f, env);
                        p.Target[StadiumPerson.HeadYaw] = 0f;
                        p.Target[StadiumPerson.Spine] = Mathf.Lerp(p.Target[StadiumPerson.Spine], -4f, env);
                        break;
                    case Act.Talk:
                        p.Target[StadiumPerson.HeadYaw] = Side(i, partner) * 40f * env;
                        p.Target[StadiumPerson.ShR] = -55f * env;
                        p.Target[StadiumPerson.ElR] = (-70f + Mathf.Sin(t * 6f) * 25f) * env;
                        p.Target[StadiumPerson.HeadPitch] = Mathf.Sin(t * 5f) * 4f * env;
                        break;
                    case Act.Stretch:
                        p.Target[StadiumPerson.ShR] = p.Target[StadiumPerson.ShL] = -170f * env;
                        p.Target[StadiumPerson.ShRz] = p.Target[StadiumPerson.ShLz] = 18f * env;
                        p.Target[StadiumPerson.ElR] = p.Target[StadiumPerson.ElL] = 0f;
                        p.Target[StadiumPerson.Spine] = -8f * env;
                        p.Target[StadiumPerson.HeadPitch] = -15f * env;
                        break;
                    case Act.Clap:
                        p.Target[StadiumPerson.ShR] = p.Target[StadiumPerson.ShL] = -60f * env;
                        p.Target[StadiumPerson.ElR] = p.Target[StadiumPerson.ElL] = (-95f + Mathf.Sin(t * 16f) * 18f) * env;
                        p.Target[StadiumPerson.ShRz] = p.Target[StadiumPerson.ShLz] = 0f;
                        break;
                    case Act.Shout:
                        p.Target[StadiumPerson.ShR] = -90f * env; p.Target[StadiumPerson.ElR] = (-20f + Mathf.Sin(t * 8f) * 15f) * env;
                        p.Target[StadiumPerson.HeadPitch] = -8f * env; p.Target[StadiumPerson.Spine] = 10f * env;
                        break;
                }
            }
            else if (i == partner && act == Act.Talk)
            {
                p.Target[StadiumPerson.HeadYaw] = Side(i, who) * 40f * env;
                p.Target[StadiumPerson.HeadPitch] = (Mathf.Max(0f, Mathf.Sin(t * 3.2f)) * 9f) * env; // listening, nodding
            }
        }

        static float Side(int from, int to) { return to > from ? 1f : -1f; }

        void Cheer(StadiumPerson p, int i)
        {
            p.Relax(true);
            p.Rate = 14f;
            float t = clock * 8f + i * 1.9f;
            p.Target[StadiumPerson.Stand] = 1f;
            p.Target[StadiumPerson.ShR] = -165f + Mathf.Sin(t) * 14f; p.Target[StadiumPerson.ShL] = -165f + Mathf.Sin(t + 1.5f) * 14f;
            p.Target[StadiumPerson.ShRz] = p.Target[StadiumPerson.ShLz] = 14f;
            p.Target[StadiumPerson.ElR] = p.Target[StadiumPerson.ElL] = -10f;
            p.Target[StadiumPerson.HeadPitch] = -12f;
            p.Target[StadiumPerson.Bob] = Mathf.Abs(Mathf.Sin(t * 0.5f)) * 0.09f;
        }

        void Dejected(StadiumPerson p, int i)
        {
            p.Relax(false);
            p.Target[StadiumPerson.Spine] = 38f;
            p.Target[StadiumPerson.HeadPitch] = 20f;
            p.Target[StadiumPerson.ShR] = p.Target[StadiumPerson.ShL] = -105f;
            p.Target[StadiumPerson.ElR] = p.Target[StadiumPerson.ElL] = -130f; // hands over the face
            p.Target[StadiumPerson.ShRz] = p.Target[StadiumPerson.ShLz] = -4f;
        }
    }

    /// <summary>
    /// A person on his feet beside the pitch. The coach paces his technical area, points, shouts, folds his arms and clutches his head;
    /// a substitute jogs up and down the line and stops to stretch; a linesman runs along the line level with the ball.
    /// </summary>
    public sealed class SidelineWalker : MonoBehaviour
    {
        public enum Role { Coach, WarmUp, Linesman }

        public Role role;
        public StadiumPerson person;
        public Transform ball;
        public float x, zMin, zMax;
        public float faceYaw = -90f; // toward the pitch

        float z, targetZ, wait = 1f, gait, speedNow, actT, actLen;
        int gesture; // 0 watching, 1 pointing, 2 arms folded, 3 hand on head, 4 stretch, 5 lunge
        bool started;
        CrewMood mood; float moodT;
        float yaw;

        public void SetMood(CrewMood m, float seconds) { mood = m; moodT = seconds; }

        void Start() { z = Mathf.Lerp(zMin, zMax, Random.value); targetZ = z; yaw = faceYaw; started = true; }

        void Update()
        {
            if (person == null || !started) return;
            float dt = Time.deltaTime;
            if (moodT > 0f) { moodT -= dt; if (moodT <= 0f) mood = CrewMood.Normal; }

            float want = z; float maxSpeed = 1f;
            switch (role)
            {
                case Role.Coach: maxSpeed = 1.0f; break;
                case Role.WarmUp: maxSpeed = 3.2f; break;
                case Role.Linesman: maxSpeed = 6.5f; break;
            }
            bool moving = false;
            if (role == Role.Linesman)
            {
                want = ball != null ? Mathf.Clamp(ball.position.z * 0.95f, zMin, zMax) : z;
                float d = want - z;
                float sp = Mathf.Min(maxSpeed, Mathf.Abs(d) * 2.5f);
                if (sp > 0.35f) { z += Mathf.Sign(d) * sp * dt; speedNow = sp; moving = true; } else speedNow = 0f;
            }
            else if (mood == CrewMood.Normal)
            {
                if (wait > 0f)
                {
                    wait -= dt; speedNow = 0f;
                    if (wait <= 0f)
                    {
                        // pick the next target: the coach somewhere in his area, the jogger the far end
                        targetZ = role == Role.WarmUp ? (Mathf.Abs(z - zMin) < Mathf.Abs(z - zMax) ? zMax : zMin) : Random.Range(zMin, zMax);
                        if (Mathf.Abs(targetZ - z) < 0.5f) { wait = 1f; }
                    }
                }
                if (wait <= 0f)
                {
                    float d = targetZ - z;
                    if (Mathf.Abs(d) < 0.15f) { wait = role == Role.WarmUp ? Random.Range(2f, 4f) : Random.Range(2f, 6f); PickGesture(); }
                    else { z += Mathf.Sign(d) * maxSpeed * dt; speedNow = maxSpeed; moving = true; }
                }
            }
            else speedNow = 0f;

            transform.position = new Vector3(x, 0f, z);
            float wantYaw = moving ? (targetDir() > 0f ? 0f : 180f) : faceYaw;
            if (role == Role.Linesman && moving) wantYaw = want > z ? 0f : 180f;
            yaw = Mathf.MoveTowardsAngle(yaw, wantYaw, 360f * dt);
            person.Facing = yaw;

            Pose(dt, moving);
        }

        float targetDir() { return targetZ - z; }

        void PickGesture()
        {
            actT = 0f;
            if (role == Role.WarmUp) { gesture = Random.value < 0.5f ? 4 : 5; actLen = 3.5f; }
            else if (role == Role.Coach) { float r = Random.value; gesture = r < 0.35f ? 1 : r < 0.65f ? 2 : r < 0.8f ? 3 : 0; actLen = 3f; }
            else gesture = 0;
        }

        void Pose(float dt, bool moving)
        {
            var p = person;
            p.Relax(true);
            p.Rate = moving ? 16f : 9f;
            actT += dt;
            if (ball != null) p.Watch(ball.position, 0.7f);
            if (role == Role.Linesman) { p.FlagVisible = true; p.Target[StadiumPerson.ShR] = -30f; p.Target[StadiumPerson.ElR] = -60f; }

            if (mood == CrewMood.Cheering)
            {
                float t = Time.time * 9f;
                p.Rate = 14f;
                p.Target[StadiumPerson.ShR] = -165f + Mathf.Sin(t) * 14f; p.Target[StadiumPerson.ShL] = -165f + Mathf.Sin(t + 1.5f) * 14f;
                p.Target[StadiumPerson.ShRz] = p.Target[StadiumPerson.ShLz] = 14f;
                p.Target[StadiumPerson.ElR] = p.Target[StadiumPerson.ElL] = -10f;
                p.Target[StadiumPerson.HeadPitch] = -12f;
                p.Target[StadiumPerson.Bob] = Mathf.Abs(Mathf.Sin(t * 0.5f)) * 0.12f;
                return;
            }
            if (mood == CrewMood.Dejected)
            {
                p.Target[StadiumPerson.HeadPitch] = 20f; p.Target[StadiumPerson.Spine] = 10f;
                p.Target[StadiumPerson.ShR] = p.Target[StadiumPerson.ShL] = -110f; p.Target[StadiumPerson.ElR] = p.Target[StadiumPerson.ElL] = -125f;
                return;
            }

            if (moving)
            {
                gait += dt * speedNow * (role == Role.Coach ? 3.2f : 2.2f);
                float amp = Mathf.Clamp(speedNow * 14f, 0f, 55f);
                float s = Mathf.Sin(gait);
                p.Target[StadiumPerson.ThighL] = -s * amp; p.Target[StadiumPerson.ThighR] = s * amp;
                p.Target[StadiumPerson.KneeL] = Mathf.Max(0f, Mathf.Sin(gait - 1.1f)) * amp * 1.2f;
                p.Target[StadiumPerson.KneeR] = Mathf.Max(0f, Mathf.Sin(gait + Mathf.PI - 1.1f)) * amp * 1.2f;
                float armAmp = Mathf.Clamp(speedNow * 9f, 0f, 45f);
                if (role != Role.Linesman) p.Target[StadiumPerson.ShR] = s * armAmp; // the linesman keeps his flag arm steady
                p.Target[StadiumPerson.ShL] = -s * armAmp;
                p.Target[StadiumPerson.ElL] = -(role == Role.WarmUp ? 80f : 15f);
                if (role == Role.WarmUp) p.Target[StadiumPerson.ElR] = -80f;
                p.Target[StadiumPerson.Spine] = Mathf.Clamp(speedNow * 2f, 0f, 10f);
                p.Target[StadiumPerson.Bob] = Mathf.Abs(Mathf.Sin(gait)) * Mathf.Clamp(speedNow * 0.012f, 0f, 0.05f);
                return;
            }

            float env = Mathf.Clamp01(Mathf.Min(actT, actLen - actT) / 0.5f);
            switch (gesture)
            {
                case 1: // pointing and shouting instructions
                    p.Target[StadiumPerson.ShR] = -95f * env; p.Target[StadiumPerson.ElR] = (-12f + Mathf.Sin(actT * 7f) * 14f) * env;
                    p.Target[StadiumPerson.ShRz] = 12f * env; p.Target[StadiumPerson.Spine] = 8f * env; p.Target[StadiumPerson.HeadPitch] = -6f * env;
                    break;
                case 2: // arms folded, watching
                    p.Target[StadiumPerson.ShR] = p.Target[StadiumPerson.ShL] = -55f * env;
                    p.Target[StadiumPerson.ElR] = p.Target[StadiumPerson.ElL] = -135f * env;
                    p.Target[StadiumPerson.ShRz] = p.Target[StadiumPerson.ShLz] = -8f * env;
                    break;
                case 3: // hand on head
                    p.Target[StadiumPerson.ShR] = -150f * env; p.Target[StadiumPerson.ElR] = -130f * env; p.Target[StadiumPerson.ShRz] = 20f * env;
                    p.Target[StadiumPerson.HeadPitch] = 8f * env;
                    break;
                case 4: // arms overhead, leaning side to side
                    p.Target[StadiumPerson.ShR] = p.Target[StadiumPerson.ShL] = -170f * env;
                    p.Target[StadiumPerson.ShRz] = p.Target[StadiumPerson.ShLz] = (20f + Mathf.Sin(actT * 2.5f) * 10f) * env;
                    p.Target[StadiumPerson.ElR] = p.Target[StadiumPerson.ElL] = 0f;
                    break;
                case 5: // forward lunge, one knee bent
                    p.Target[StadiumPerson.ThighR] = -50f * env; p.Target[StadiumPerson.KneeR] = 70f * env;
                    p.Target[StadiumPerson.ThighL] = 25f * env; p.Target[StadiumPerson.Bob] = -0.18f * env;
                    p.Target[StadiumPerson.ShR] = p.Target[StadiumPerson.ShL] = -40f * env; p.Target[StadiumPerson.ElR] = p.Target[StadiumPerson.ElL] = -50f * env;
                    break;
            }
            // hands on hips for a jogger catching his breath
            if (gesture == 0 && role == Role.WarmUp)
            {
                p.Target[StadiumPerson.ShR] = p.Target[StadiumPerson.ShL] = 10f; p.Target[StadiumPerson.ShRz] = p.Target[StadiumPerson.ShLz] = 30f;
                p.Target[StadiumPerson.ElR] = p.Target[StadiumPerson.ElL] = -90f;
            }
        }
    }
}
