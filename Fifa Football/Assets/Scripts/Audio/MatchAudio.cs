using UnityEngine;
using FootballSim.Match;
using FootballSim.Players;

namespace FootballSim.Audio
{
    /// <summary>Plays the synthesised sound effects for match events. One 2D AudioSource, no assets.</summary>
    public class MatchAudio : MonoBehaviour
    {
        public const string PrefKey = "sound_on";

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

        public const string MusicKey = "vol_music", SfxKey = "vol_sfx", CrowdKey = "crowd_on", CommentaryKey = "commentary_on";

        static float GetVolume(string key, float fallback) { try { return Mathf.Clamp01(PlayerPrefs.GetFloat(key, fallback)); } catch { return fallback; } }
        static void SetPref(string key, float v) { try { PlayerPrefs.SetFloat(key, v); PlayerPrefs.Save(); } catch { } }
        static bool GetFlag(string key) { try { return PlayerPrefs.GetInt(key, 1) == 1; } catch { return true; } }
        static void SetFlag(string key, bool on) { try { PlayerPrefs.SetInt(key, on ? 1 : 0); PlayerPrefs.Save(); } catch { } }

        /// <summary>Background music loudness, 0..1 (saved).</summary>
        public static float MusicVolume { get { return GetVolume(MusicKey, 0.8f); } set { SetPref(MusicKey, Mathf.Clamp01(value)); if (live != null) live.ApplyLevels(); } }
        /// <summary>Loudness of the kicks, whistles and the crowd, 0..1 (saved).</summary>
        public static float SfxVolume { get { return GetVolume(SfxKey, 0.8f); } set { SetPref(SfxKey, Mathf.Clamp01(value)); if (live != null) live.ApplyLevels(); } }
        /// <summary>The crowd's murmur and cheers (saved).</summary>
        public static bool CrowdOn { get { return GetFlag(CrowdKey); } set { SetFlag(CrowdKey, value); if (live != null) live.ApplyLevels(); } }
        /// <summary>The on-screen commentary: throw-ins, corners, fouls and cards are announced as text; with it off only goals are (saved).</summary>
        public static bool CommentaryOn { get { return GetFlag(CommentaryKey); } set { SetFlag(CommentaryKey, value); } }

        static MatchAudio live;
        AudioSource musicSource, crowdSource;

        void ApplyLevels()
        {
            if (musicSource != null) musicSource.volume = MusicVolume * 0.35f;
            if (crowdSource != null) crowdSource.volume = CrowdOn ? SfxVolume * 0.3f : 0f;
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
            live = this;
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = ProceduralAudio.ToClip("music", ProceduralAudio.Music()); musicSource.loop = true; musicSource.spatialBlend = 0f; musicSource.playOnAwake = false;
            crowdSource = gameObject.AddComponent<AudioSource>();
            crowdSource.clip = ProceduralAudio.ToClip("crowd", ProceduralAudio.Ambience()); crowdSource.loop = true; crowdSource.spatialBlend = 0f; crowdSource.playOnAwake = false;
            ApplyLevels();
            musicSource.ignoreListenerPause = true; crowdSource.ignoreListenerPause = true;
            musicSource.Play(); crowdSource.Play();
        }

        void OnDestroy() { if (live == this) live = null; }

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

        void Play(AudioClip c, float volume) { if (c != null && source != null) source.PlayOneShot(c, volume * SfxVolume * 1.25f); }

        void OnStruck(PlayerController p, bool isShot, Vector3 dir) { Play(kick, isShot ? 0.9f : 0.5f); }
        void OnFouled(PlayerController a, PlayerController b) { Play(thud, 0.8f); }
        void OnGoal(int team) { if (CrowdOn) Play(cheer, 0.8f); Play(whistle, 0.5f); }
        void OnRestart(RestartType t, int team) { if (t == RestartType.FreeKick || t == RestartType.Penalty || t == RestartType.Offside) Play(whistle, 0.6f); }
        void OnCard(int team, int total) { Play(whistle, 0.7f); }
        void OnWhistle() { Play(whistle, 0.8f); }
    }
}
