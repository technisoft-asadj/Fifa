using System;
using System.Collections.Generic;
using UnityEngine;

namespace FootballSim.Players
{
    /// <summary>Measured facts about the one-shot action clips (written by the editor builder next to the prefab): clip length, the moment the
    /// ball is struck, and which foot the clip kicks with unmirrored.</summary>
    [Serializable]
    public class AnimatedClipInfo
    {
        [Serializable]
        public class Entry { public string name; public float length; public float contact; public bool rightFoot; }

        public List<Entry> clips = new List<Entry>();

        public Entry Find(string name)
        {
            foreach (var e in clips) if (e.name == name) return e;
            return null;
        }

        public static AnimatedClipInfo Load()
        {
            var t = Resources.Load<TextAsset>("Animated/ClipInfo");
            if (t == null) return null;
            try { return JsonUtility.FromJson<AnimatedClipInfo>(t.text); } catch { return null; }
        }
    }
}
