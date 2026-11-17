using UnityEditor;
using UnityEngine;

namespace FootballSim.EditorTools
{
    /// <summary>
    /// Import rules for files the owner drops into Assets/Art/Mixamo: every FBX there becomes a Humanoid with its own avatar (Humanoid clips
    /// retarget between avatars), and clips that are cycles (idle, walk, jog, run, sprint, strafe) are set to loop. Nothing else under Assets
    /// is touched. Re-import a file (right-click > Reimport) to apply the rules to one that was already there.
    /// </summary>
    public class MixamoImportSettings : AssetPostprocessor
    {
        const string Folder = "Assets/Art/Mixamo/";
        static readonly string[] CycleWords = { "idle", "walk", "jog", "run", "sprint", "strafe", "dribble", "backward" };

        static readonly string[] OneShotWords = { "strike", "shot", "pass", "receive", "turn", "tackle", "header", "celebrate", "defeated", "keeper" };

        /// <summary>True when a clip name describes a repeating motion (for example "Jog Forward" or "soccer_idle").</summary>
        public static bool IsCycle(string clipName)
        {
            if (string.IsNullOrEmpty(clipName)) return false;
            string n = clipName.ToLowerInvariant();
            foreach (var w in OneShotWords) if (n.Contains(w)) return false; // "Strike Jog" is a kick, not a jog cycle
            foreach (var w in CycleWords) if (n.Contains(w)) return true;
            return false;
        }

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Folder, System.StringComparison.OrdinalIgnoreCase)) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false; // kept off so bone transforms stay available while the rig is being tuned
            importer.importCameras = false;
            importer.importLights = false;
        }

        void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(Folder, System.StringComparison.OrdinalIgnoreCase)) return;
            var importer = (ModelImporter)assetImporter;
            var clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0) return;
            string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            foreach (var clip in clips)
            {
                bool loop = IsCycle(file) || IsCycle(clip.name);
                clip.loopTime = loop;
                clip.loopPose = loop;
                // keep the motion in place: the game moves the player, the animation only plays on top of it (no root motion)
                clip.lockRootRotation = true; clip.lockRootHeightY = true;
                // cycles are in place already (baked into the pose); a one-shot such as a slide travels metres, and the game owns where the player
                // is, so its horizontal travel goes to the (unused) root motion instead of moving the body away from the player
                clip.lockRootPositionXZ = loop;
                clip.keepOriginalOrientation = true; clip.keepOriginalPositionY = true; clip.keepOriginalPositionXZ = true;
            }
            importer.clipAnimations = clips;
        }
    }
}
