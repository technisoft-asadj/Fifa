# Mixamo downloads for animation Phase 2 (what you do by hand)

Decision (2026-11-17): the base character is a free Mixamo character. Mixamo needs an Adobe login that only you can use, so these steps are yours. I will not download or sign in for you.

## Where files go
Everything goes in **`Assets/Art/Mixamo/`** (already created). That folder is git-ignored, because Mixamo's terms allow using the files inside a game but not redistributing the raw files; a fresh checkout will therefore not contain the character until the files are downloaded again. If the repository is private and you are happy to commit them, tell me and I will lift the ignore line in `.gitignore`.

When a file lands there, Unity imports it as a Humanoid automatically (`Assets/Editor/MixamoImportSettings.cs`): its own avatar, no root motion, and idle/walk/jog/run/sprint/strafe/dribble/backward clips set to loop.

## 1. The character (one file)
1. Sign in at mixamo.com (free Adobe account), open **Characters**.
2. Pick **Y Bot** (a neutral mannequin) as the first prototype character. Why: team colours are applied by tinting the material, and a mannequin reads as a plain "team colour" player. A human-looking Mixamo character has one fused mesh (skin, hair, clothes), so a shirt colour cannot be tinted on its own. Any other free character is fine if you prefer it; tell me which.
3. **Download**: Format **FBX for Unity (.fbx)**, Pose **T-pose**. Save as `Assets/Art/Mixamo/Character.fbx`.

## 2. The animations (with the character selected, "Skin: Without Skin")
For each clip: search the name, adjust nothing, then Download with **Format FBX for Unity**, **Skin: Without Skin**, **Frames per second 30**, **Keyframe Reduction: none**. Tick **In Place** for the locomotion ones marked (ip). Save into `Assets/Art/Mixamo/` with the file name shown.

Needed for Phase 2 (movement):
| Search for | Save as | Notes |
|---|---|---|
| Idle (any calm one, e.g. "Idle") | `Idle.fbx` | |
| Walking | `Walk.fbx` | (ip) |
| Jog Forward | `Jog.fbx` | (ip) |
| Run Forward | `Run.fbx` | (ip) |
| Sprint | `Sprint.fbx` | (ip), if there is no "Sprint" use the fastest "Run" |
| Left Strafe / Right Strafe (walk and run if offered) | `StrafeLeft.fbx`, `StrafeRight.fbx` | (ip) |
| Jog Backward | `JogBackward.fbx` | (ip) |
| Left Turn 90 / Right Turn 90 | `Turn90Left.fbx`, `Turn90Right.fbx` | **not** in place |
| Turn 180 | `Turn180.fbx` | **not** in place |

For later phases (not needed yet, download when convenient): Mixamo's soccer set ("Soccer Pass", "Soccer Kick", "Soccer Header", "Goalkeeper ..." and similar, if present), "Tackle"/"Slide", "Falling", "Getting Up", and celebrations. I will tell you which are missing when we get there rather than fake them.

## 3. After downloading
Tell me when the files are in. I will then: open them in Unity, record the licence terms I can read at the source in `asset_licenses.md`, build the speed-matched locomotion Animator on one player behind a switch, and measure foot sliding and frame rate against the baseline in `progress.md` (59.6 fps average with 22 primitive players on the Infinix).

## Defaults I am using for the other open decisions (say so if you disagree)
- Architecture as written in `animation_audit_and_plan.md`: animation follows the simulation, no root motion, the primitive body stays as the fallback.
- `com.unity.animation.rigging` is **not** added yet; foot/hand IK is only needed in Phase 3, and I will ask again then.
- UI redesign: finished, so no interleaving needed.
