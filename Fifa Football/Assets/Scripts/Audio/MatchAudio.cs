using UnityEngine;
using FootballSim.Match;
using FootballSim.Players;

namespace FootballSim.Audio
{
    /// <summary>Plays the synthesised sound effects for match events. One 2D AudioSource, no assets.</summary>
    public class MatchAudio : MonoBehaviour
    {
        const string PrefKey = "sound_on";

        MatchManager match;
        AudioSource source;
        AudioClip kick, thud, whistle, cheer;

        /// <summary>Master sound switch, saved between sessions.</summary>
        public static bool SoundOn
        {
            get { try { return PlayerPrefs.GetInt(PrefKey, 1) == 1; } catch { return true; } }
            set
            {
                try { PlayerPrefs.SetInt(PrefKey, value ? 1 : 0); PlayerPrefs.Save(); } catch { }
                AudioListener.volume = value ? 1f : 0f;
            }
        }

        public void Init(MatchManager m) { match = m; Subscribe(); }

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            kick = ProceduralAudio.ToClip("kick", ProceduralAudio.Kick());
            thud = ProceduralAudio.ToClip("thud", ProceduralAudio.Thud());
            whistle = ProceduralAudio.ToClip("whistle", ProceduralAudio.Whistle());
            cheer = ProceduralAudio.ToClip("cheer", ProceduralAudio.Cheer());
            AudioListener.volume = SoundOn ? 1f : 0f;
        }

        void OnEnable() { Subscribe(); PlayerController.Struck += OnStruck; PlayerController.Fouled += OnFouled; }

        void OnDisable()
        {
            PlayerController.Struck -= OnStruck; PlayerController.Fouled -= OnFouled;
            Unsubscribe();
        }

        bool subscribed;
        void Subscribe()
        {
            if (subscribed || match == null || !isActiveAndEnabled) return;
            subscribed = true;
            match.GoalScored += OnGoal;
            match.RestartAwarded += OnRestart;
            match.YellowCard += OnCard;
            match.RedCard += OnCard;
            match.HalfTime += OnWhistle;
            match.FullTime += OnWhistle;
        }

        void Unsubscribe()
        {
            if (!subscribed || match == null) return;
            subscribed = false;
            match.GoalScored -= OnGoal;
            match.RestartAwarded -= OnRestart;
            match.YellowCard -= OnCard;
            match.RedCard -= OnCard;
            match.HalfTime -= OnWhistle;
            match.FullTime -= OnWhistle;
        }

        void Play(AudioClip c, float volume) { if (c != null && source != null) source.PlayOneShot(c, volume); }

        void OnStruck(PlayerController p, bool isShot, Vector3 dir) { Play(kick, isShot ? 0.9f : 0.5f); }
        void OnFouled(PlayerController a, PlayerController b) { Play(thud, 0.8f); }
        void OnGoal(int team) { Play(cheer, 0.8f); Play(whistle, 0.5f); }
        void OnRestart(RestartType t, int team) { if (t == RestartType.FreeKick || t == RestartType.Penalty || t == RestartType.Offside) Play(whistle, 0.6f); }
        void OnCard(int team, int total) { Play(whistle, 0.7f); }
        void OnWhistle() { Play(whistle, 0.8f); }
    }
}
