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

## Session 2 - Android build setup (build NOT yet verified)
- Android build support is installed in this editor (verified: IsBuildTargetSupported = true). Scripting backend IL2CPP, ARM64 only, min SDK 25.
- Set landscape-only orientation (left/right auto-rotate), version 0.2.0, and the build list now contains only `FootballPrototype` (template SampleScene removed).
- A development build to `Builds/Android/FootballSim-dev.apk` was started through the MCP build tool. The platform switch/reimport was still running when this was committed, so **no APK result is recorded yet**. Do not treat Phase 0 as exited until an APK builds and installs on a device.
- Open items: ARMv7 and 16 KB page-size checks, release signing (keystore must never be committed; `*.keystore` is git-ignored), device install test.

## Session 2 - First Android development build and a goal-wiring bug
- **Android dev build: SUCCEEDED** (first build). `Builds/Android/FootballSim-dev.apk`, about 34 MB, IL2CPP, ARM64, min SDK 25, development build. JDK/SDK/NDK were detected automatically. Not yet installed or run on any device; ARMv7 and 16 KB page-size checks still open.
- The build's scene validation reported: "Script attached to 'Trigger' in scene FootballPrototype is missing". Root cause: `GoalTrigger` lived in `MatchManager.cs` (Unity needs one MonoBehaviour per file, same name). Goal detection was therefore not wired in the saved scene. Fixed by moving it to `GoalTrigger.cs`; scene rebuilt and now references the new script GUID.
- New `PrototypeSceneTests` load the real saved scene and check: no missing scripts, two teams of 11 with one keeper each, two goal triggers, and a ball placed in each goal scores for the correct team. These would have caught the bug.
- Tests: 10/10 EditMode, 23/23 PlayMode pass. The APK with the fix was rebuilt successfully (34.1 MB).

## Session 3 - First run on a real phone
Device: Infinix X6881, Android 14 (API 34), arm64, 1080x2436, 8 GB RAM, 4 KB page size, punch-hole cutout.
VERIFIED on device (driven over adb: install, taps, swipes, screenshots, frame timestamps from SurfaceFlinger):
- APK installs and launches; no exceptions or missing-script messages in the Unity log.
- Start screen renders correctly in landscape; tapping PLAY starts the match.
- Touch buttons appear; the floating joystick appears under a drag and the knob follows it.
- Frame rate was locked at 30 fps (33.5 ms every frame) - Unity's mobile default cap, not a performance limit.
  Added `Core/AppBootstrap.cs` (target 60 fps, vSync off, screen never sleeps). Re-measured: 16.77 ms average,
  p95 16.79 ms, max 17.06 ms over 126 frames = steady 60 fps with 22 players.
- Memory: about 400 MB PSS (development build). Not yet compared with a release build.
- Keyboard hint no longer shows on a touch device (KB-013 fixed).
- Touch canvas now sits inside `Screen.safeArea` (KB-012 partly addressed).
- Tests: 10 EditMode, 26/26 PlayMode pass (3 new: safe-area anchors x2, frame-rate target).

NOT verified:
- Human play feel: nobody has played a match by hand on the phone yet. Multi-touch (joystick + button together),
  button reach and size were not judged; adb can only inject one pointer.
- Only one device and only the first seconds of a match were measured; no thermal or battery run, no low-end phone.
- 32-bit (ARMv7) devices and 16 KB page-size devices: this phone is 64-bit with 4 KB pages, so neither is covered.
- The safe-area change was not checked visually with the cutout on the button side.

## Session 3 - Pause menu and touch-control settings
- Pause button (top-right), Esc key and Android back button pause the match; ignored on the start and full-time screens.
- Pause panel: RESUME, RESTART, button size (50-160 %) and button opacity (20-100 %), saved in PlayerPrefs.
- Touch controls stay visible (but do not take touches) while paused, so changes are previewed live.
- Tests: 28/28 PlayMode pass (2 new: pause/resume time scale; preview while paused + setting clamps).
- VERIFIED on the phone afterwards (adb taps + screenshots): pause button pauses, panel layout is correct,
  size +/- and opacity +/- change the buttons live (100 -> 120 %, 60 -> 50 %), RESUME continues the match.
- FAILED on the phone: the Android back key (adb `keyevent 4`) did not resume/pause. See KB-015.

## Session 3 - Red cards and penalties
- Cards: a player's 2nd foul is a yellow, the 4th is a second yellow = red. The player is removed from the pitch
  (GameObject disabled, formation slot kept); human control moves to the nearest team-mate; AI ignores sent-off players.
  Goalkeepers are not sent off yet (no replacement-keeper logic).
- Penalties: a foul inside the fouling team's own penalty area (40.32 m x 16.5 m) gives a penalty. Ball on the spot
  (11 m), an outfield taker behind it, everyone else moved out of the area, defending keeper on the goal line.
  After the 1.2 s hold normal play resumes: there is no dedicated penalty aiming / keeper-dive mini-game yet.
- HUD: PENALTY and RED CARD banners; full-time stats show red cards.
- Tests: 32/32 PlayMode pass (4 new: penalty vs free kick by foul position, red on 4th foul, send-off in the real
  scene with control hand-over, penalty box clearing in the real scene). EditMode tests not rerun this session.
- NOT verified: no red card or penalty has been seen in live play (they depend on random fouls); penalty balance
  (how often the AI scores) is unknown; not yet in the APK on the phone.
- Lesson: the earlier in-editor checks passed because the editor resolved the script by type; only the build's validation exposed it. Run scene-level tests after every scene rebuild.
