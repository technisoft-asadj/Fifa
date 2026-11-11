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

## Session 2 - Fouls, formations, difficulty
| Item | Status |
|---|---|
| Fouls: failed tackle that connects with an opponent (85% from behind, 45% otherwise) -> free kick for fouled team | IMPLEMENTED; logic unit-tested via AwardFoul; no foul seen in a live AI match yet |
| Yellow card on every 2nd foul by the same player (counted per team, shown in banner and full-time stats) | IMPLEMENTED, unit-tested; no red cards or sending off |
| 5 formations (4-4-2, 4-3-3, 4-2-3-1, 3-5-2, 5-3-2), player picks theirs, AI picks random | IMPLEMENTED, data validated by tests |
| 5 difficulty levels (reaction delay, aim error, shoot range, tackle cooldown) | IMPLEMENTED, tuning values unit-tested for ordering only; not balanced by play |
| Start-screen selectors for formation and difficulty | IMPLEMENTED, rendered in Play mode |

Live check (1-minute Legendary AI match): ran without errors, throw-ins were awarded in real play. Fouls/cards not observed in that window.
Tests: 10/10 EditMode, 12/12 PlayMode pass.
Not done: penalties, offside, red cards, balanced difficulty (needs playtesting), away-team formation is random.

## Session 2 - Player visuals and procedural animation
| Item | Status |
|---|---|
| PlayerVisual: articulated humanoid from primitives (head, hair, torso, shorts, thighs, shins, boots, arms), kit colour taken from the team material, skin/hair variety per player | IMPLEMENTED, visually checked in Play mode |
| Procedural run cycle (leg/arm swing and knee bend scale with speed, body lean), idle settle | IMPLEMENTED, unit-tested |
| Kick animation (on pass/shot) and slide-tackle pose (on tackle) | IMPLEMENTED, kick unit-tested; tackle pose not seen live |
| No new colliders; gameplay unchanged (CharacterController capsule still drives physics) | VERIFIED by test |

Tests: 10/10 EditMode, 15/15 PlayMode pass.
Limits: still primitives, no real rig, no goalkeeper dive/catch animation, no celebrations, ~15 renderers per player (22 players) is fine in the editor but needs GPU instancing / merged meshes before mobile device builds.
Replacement path: a rigged model with an Animator only needs speed (PlayerController.Velocity), PlayKick and PlayTackle.

Dev note: entering Play immediately after the "Build Prototype Scene" menu can race and play a stale scene; wait for the build to finish first.

## Session 2 - Mobile touch controls
| Item | Status |
|---|---|
| TouchControls: floating joystick (left 45% x lower 65% of screen) + SHOOT (hold/release), PASS, TACKLE, SWITCH, SPRINT buttons; implements IPlayerInput | IMPLEMENTED |
| Merged with keyboard/gamepad in PlayerInputSource (larger stick wins; buttons OR-ed; one-shots latched and consumed) | IMPLEMENTED, unit-tested |
| Visible only on touch devices (or forceShow) and only while the game is unpaused | IMPLEMENTED, unit-tested |
| Size/opacity settings persisted in PlayerPrefs (SetAppearance) | IMPLEMENTED, no settings UI yet |
| Layout rendered in Play mode with forceShow (buttons on right, scaled via 1920x1080 reference) | CHECKED visually at editor game-view size |

Tests: 10/10 EditMode, 19/19 PlayMode pass.
NOT verified: real finger input on a phone/tablet, multi-touch (stick + button together), notch/safe-area handling, button placement ergonomics, joystick feel. The joystick drag path was only driven through test hooks, not real pointer events.
Not done: draggable/resizable button layout, left-handed mode, safe-area insets, settings screen.
