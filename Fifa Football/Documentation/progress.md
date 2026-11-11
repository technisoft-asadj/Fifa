# Progress

## Session 1 - Audit (VERIFIED facts)
- Unity 6000.3.5f1, URP 17.3.0, Input System 1.17.0 (activeInputHandler = new), Test Framework 1.6.0, AI Navigation 2.0.9.
- Template project: one scene `SampleScene`, no gameplay scripts.
- Scripting backend Android: IL2CPP. AndroidTargetArchitectures = ARM64 only. Min SDK 25.
- Android applicationIdentifier is still the template placeholder: must be changed before any release.
- Git repo exists at the parent folder; the Unity project folder was untracked.
- Quality assets: Mobile and PC URP renderer/pipeline assets exist.
- No netcode packages installed (decision deferred to networking evaluation).

## Session 1 - Foundation work
See "Foundation" entries below; status values: NOT STARTED / IN PROGRESS / IMPLEMENTED / TESTING / VERIFIED / BLOCKED.

| Item | Status |
|---|---|
| Documentation/plan.md | IMPLEMENTED |
| Folder structure (Scripts/Ball, Players, Input, Camera, Match, Editor) | IMPLEMENTED |
| Pitch, goals, goal triggers, boundary walls | IMPLEMENTED (scene `Assets/Scenes/FootballPrototype.unity`, built by menu FootballSim > Build Prototype Scene) |
| Ball (custom drag, Magnus, rolling resistance) | IMPLEMENTED, tuning untested |
| Player movement (accel, turn rate, sprint) | IMPLEMENTED, not yet played by a human |
| Camera (broadcast) | IMPLEMENTED, not visually reviewed |
| Input abstraction (keyboard + gamepad) | IMPLEMENTED, not yet tested with real input |
| Match manager (score, clock, kickoff reset) | IMPLEMENTED, goal path untested |

Verified so far: scripts compile with 0 errors; scene builds; entering Play mode produced 0 console errors/warnings.
NOT verified: controls feel, dribble/pass/shoot behaviour, goal detection, ball tuning.
Controls: WASD/arrows move, Shift sprint, Space (hold, release) shoot, K pass, J tackle (not wired), Tab switch (not wired).
| Android dev build | NOT STARTED |

## Known bugs
None recorded yet.

## Next recommended task
Playtest the foundation scene, tune ball/player parameters, then Phase 2 (tackling, player switching, match rules).

## Session 1 - Phase 2 (core gameplay) additions
| Item | Status |
|---|---|
| IPlayerInput shared by human + AI (world-space move, aim, action power) | IMPLEMENTED |
| Tackle (lunge + win chance by strength, cooldown) | IMPLEMENTED, untested by a human |
| Player switching (Tab / auto-switch when a teammate is >6 m closer) + yellow marker | IMPLEMENTED, untested by a human |
| Data-driven formation (FormationData, 4-4-2 asset) | IMPLEMENTED |
| 11v11 teams (TeamController, PlayerAI: chase, shoot, pass, dribble, tackle) | IMPLEMENTED, behaviour unevaluated |
| Goalkeeper AI (positioning, intercept line, clear) | IMPLEMENTED, unevaluated |
| Kick-off reset after goals | IMPLEMENTED, untested |

Verified: compiles with 0 errors; scene builds with 22 players; Play mode shows both teams in formation; 0 console errors/warnings.
NOT verified: full match flow, AI quality, goals scored, tackle feel, switching feel.

Known limitations / next:
- Ball bounces off boundary walls (no throw-ins, corners, goal kicks yet).
- No offside, fouls, cards or referee yet.
- All players share one PlayerData asset; no per-player stats yet.
- AI has no marking, passing lanes or attacking runs: Phase 4 work.
- No automated tests yet (EditMode tests for ball, match clock, formation are the next task).
