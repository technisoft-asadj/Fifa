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

## Session 2 - Out-of-bounds restarts and automated tests
| Item | Status |
|---|---|
| Last-touch tracking (BallController.LastTouchTeam) | IMPLEMENTED |
| Throw-in, corner, goal kick (MatchManager, taker teleported to ball, 1.2 s hold) | IMPLEMENTED, unit-tested; not yet seen live in a full match |
| Assembly definitions (FootballSim.Runtime + EditMode/PlayMode test assemblies) | IMPLEMENTED |
| EditMode tests (8): attribute maths, formation data, pitch size, AI input one-shots | VERIFIED (8/8 passed) |
| PlayMode tests (9): ball rest, kick, speed clamp, spin curve, freeze, goal scoring + duplicate guard, throw-in, corner, goal kick | VERIFIED (9/9 passed) |

Smoke test: scene rebuilt, Play mode ran about a minute with AI vs AI, clock advanced, 0 errors. No goal or restart happened during that window, so live restarts remain unobserved.

Still missing: offside, fouls/cards/referee, penalties, per-player stats, marking/passing-lane AI, animations, UI beyond the debug score label.

Run tests: Window > General > Test Runner, or via the MCP run_tests tool.

## Session 2 - Match flow and HUD
| Item | Status |
|---|---|
| MatchManager events (GoalScored, FullTime, RestartAwarded), SetDurationMinutes | IMPLEMENTED |
| MatchHUD (code-built uGUI): scoreboard, clock, goal/restart banners, controls hint | IMPLEMENTED, visually checked in Play mode |
| Start screen (3/5/10 min, PLAY) pauses the sim until pressed | VERIFIED via script-invoked click: timeScale 0 -> 1, panel hidden |
| Full-time screen with result and REMATCH | VERIFIED: 30 s match ended, showed "DRAW 0 - 0"; rematch reloaded scene back to paused start screen |
| Debug OnGUI score label removed | IMPLEMENTED |

Not verified: mouse/touch clicks on the real buttons (clicks were invoked from script), goal banner during a real goal, UI on phone aspect ratios.
Tests after this change: 8/8 EditMode, 9/9 PlayMode pass.
Next: fouls/offside, difficulty levels, more formations, then player models and animation.
