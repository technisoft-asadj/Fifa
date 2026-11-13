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

## Session 3 - Match statistics
- `Match/MatchStats.cs`: possession (time since each team last touched the ball, live play only), shots,
  shots on target, passes, corners. Fouls and cards were already counted.
- Shots/passes come from a new `PlayerController.Struck` event. "On target" is a flat geometric check at the moment
  of the strike (would the line cross the goal line between the posts); it ignores height, curve, blocks and saves.
- Full-time screen now shows a five-line statistics summary.
- Tests: 13/13 EditMode (3 new for MatchStats). 33/33 PlayMode
  (1 new: a short AI match in the real scene accumulates possession and at least one pass or shot).
- NOT verified: the full-time screen layout has not been looked at (in the editor or on the phone); the numbers have
  not been compared against a watched match.

## Session 3 - Offside
- `Match/OffsideRule.cs` (pure geometry): a receiver is offside if, when the ball is played, he is in the opponent half
  and ahead of both the ball and the second-last opponent (keeper counts), with 0.3 m tolerance.
- `MatchManager` snapshots the offside team-mates on every pass/shot (`SnapshotOffside`); if one of them is the next
  player to touch the ball, play stops and the opponents get an indirect-style free kick (`RestartType.Offside`,
  "OFFSIDE - FREE KICK" banner, counted in `Stats.offsides`, shown on the full-time screen). An opponent touch,
  or any restart/goal, cancels the pending offside. Throw-ins, goal kicks, corners and kick-offs are exempt for the first strike.
- `BallController.LastToucher` added. AI no longer passes to a team-mate who is offside at that moment.
- Tests: 16/16 EditMode (3 new OffsideRule), 35/35 PlayMode (2 new: offside gives a free kick; opponent touch cancels it).
  The existing stats test was flaky because AI only passes under pressure; its window was raised from 24 to 60 game seconds.
- NOT verified: never seen in a watched live match; the tests place players and set the last toucher by hand.
  Simplifications: no "interfering with play" judgement (any touch counts), tolerance is a flat 0.3 m,
  no assistant-referee flag/animation. Not in the phone APK yet. Logged as KB-017.

## Session 3 - Stamina
- `Players/StaminaModel.cs` (pure): sprint drains a full bar in 20 s, recovery takes 14 s when not sprinting; if it runs
  dry, sprint is locked (jog only) until 25% is back. `PlayerController.Stamina` / `Exhausted` apply it to every player
  (human and AI), so AI players that sprint-dribble tire too.
- HUD: stamina bar at bottom centre for the player you control (green to amber, red while exhausted).
- Tests: 19/19 EditMode (3 new), 35/35 PlayMode (the live-match test also checks that AI stamina was used).
- NOT verified: how it feels (20 s / 14 s / 25% are guesses, not tuned); bar position vs. touch buttons on the phone;
  not in the phone APK; stamina does not reset at half/kick-off and has no per-player attribute yet.

## Session 3 - Half-time break
- At half of the match length (once no goal pause or restart is in progress) `MatchManager` raises `HalfTime`, holds the
  ball for 4 s (`HalfTimeSeconds`, goals are blocked) and then restarts from the centre spot via the existing kick-off
  path. HUD banner "HALF TIME h - a". The match clock keeps running during the break.
- Tests: 36/36 PlayMode (1 new: break fires, ball held, then centre restart). 19/19 EditMode unchanged.
- NOT verified: not seen on screen or on the phone. Simplifications (known): the second half
  is not kicked off by the other team, the clock is not paused, no half-time stats screen.

## Session 3 - Teams change ends at half-time
- `MatchManager.SidesSwapped` / `AttackSign(team)` replace every "home attacks +Z" assumption in match rules
  (free-kick/penalty direction and spot, corner vs goal kick decision, offside, shot-on-target, offside free kick).
  At the end of the break: both `TeamController.attackDir` flip, both `GoalTrigger.defendingTeam` flip, and
  `BroadcastCamera.SetViewSign(-1)` turns the view round (snapped) so the human still attacks "up the screen".
  `OffsideRule` and `MatchStats.IsOnTarget/AddStrike` gained attack-sign overloads (old signatures kept).
- Tests: 20/20 EditMode (1 new), 36/36 PlayMode (half-time test now also checks attackDir, camera sign and that a ball in
  the +Z goal scores for AWAY after the swap).
- NOT verified: the swapped second half has not been watched (AI behaviour, touch controls feel, camera snap), and not on
  the phone. Pre-existing tests that assume home attacks +Z only cover the first half.

## Session 3 - Procedural sound effects
- No audio assets are downloaded or licensed: `Audio/ProceduralAudio.cs` synthesises ball kick, tackle thud, referee
  whistle and crowd cheer from maths (deterministic, 22.05 kHz mono). `Audio/MatchAudio.cs` (added to the HUD object at
  runtime, no scene rebuild needed) plays them on kicks/passes, fouls, goals, free kicks/penalties/offsides, cards,
  half-time and full-time.
- Pause menu has a SOUND: ON/OFF button (saved in PlayerPrefs, applied through `AudioListener.volume`).
- Tests: 25/25 EditMode (5 new: each sound is audible, unclipped, ends silent; deterministic), 37/37 PlayMode (audio
  player exists, mute switch works).
- NOT verified: I have not LISTENED to any of it, so how it sounds (levels, harshness, whether the whistle is annoying) is
  unknown; the editor/tests have no meaningful audio output. Not tested on the phone. No music, no ambient crowd loop,
  no per-event volume tuning, and the pause panel layout with the new button has not been looked at.

## Session 3 - Proper kick-offs
- `MatchManager.KickoffTeam`: home starts, the conceding team restarts after a goal, away starts the second half.
  `PrepareKickoff()` (also called when the match begins) puts the kicking team's nearest outfielder beside the ball and
  moves any player of the other team out of the 9.15 m centre circle into its own half.
- `KickoffPending` holds the non-kicking AI team in its formation until the ball is first played (or 8 s pass, so a taker
  who never kicks cannot freeze the game). The old behaviour had both teams sprinting at the ball from the whistle.
- Tests: 38/38 PlayMode (1 new: first kick-off, then kick-off after a goal goes to the other team, circle clear, taker at the ball);
  25/25 EditMode unchanged.
- NOT verified: not watched live or on the phone. A HUMAN-controlled player on the non-kicking team is not stopped from
  entering the circle (only AI is held). No dedicated kick-off pass animation or "must play forward" rule.

## Session 3 - Remembered start-screen choices
- `Core/MatchSettingsStore.cs`: match length, your formation and difficulty are saved in PlayerPrefs whenever you change
  them and restored when the start screen opens. Missing, unknown (e.g. 7 min) or out-of-range values fall back to
  the defaults. The AI team's random formation is not saved.
- Tests: 28/28 EditMode (3 new, they back up and restore the real prefs), 38/38 PlayMode.
- Test fix: `LiveMatchAccumulatesStatsAndSummaryText` failed intermittently (and once more after the kick-off change)
  because the AI only passes under pressure and the human-controlled home team idles. It now checks possession from the
  AI play, and forces one queued AI pass to prove passes are counted. The earlier "AI passes within 60 s" assertion was luck.
- NOT verified: the saved values were checked only through the store, not by restarting the real app; not on the phone.

## Session 3 - Goal log and own goals
- `MatchManager.Goals` records each goal: team it counts for, game minute, last toucher's name and whether he was an
  opponent (own goal). The goal banner adds "(OWN GOAL)" and the full-time screen lists up to 5 goals above the stats.
- Tests: 40/40 PlayMode (2 new: scorer + minute recorded; defender's touch gives an own goal). 28/28 EditMode unchanged.
- NOT verified: the full-time screen with goal lines has not been looked at (the panel is not resized for them);
  "scorer" is the GameObject name of the last toucher (e.g. a generated name, not a squad number or real name), and a
  deflection off the keeper counts as the keeper's touch; no assists; not on the phone.

## Session 3 - Full-match soak test
- New PlayMode test `FullMatchPlaysThroughHalfTimeToFullTimeWithoutErrors`: a 90-game-second match at 4x speed in the real
  scene (human team idle, AI plays). Every frame it checks: clock never goes backwards, ball and player positions are
  finite and on/near the pitch, the ball does not stand still in live play for 12 s (catches a deadlocked kick-off), and
  Unity logs no errors. At the end: full time reached, half-time happened, sides swapped, goal log matches the score,
  possession adds to 100.
- Result: passed on two consecutive runs (~24 s each). It found no bugs, so nothing was changed in the game code.
- NOT covered: it plays 90 game seconds, not a 5-10 minute match; the human team never acts (so human-controlled
  paths, touch input and the pause menu are not exercised in it); it runs at 4x with the editor's frame rate, not at
  phone speed; a different random seed could still hit a rare state.

## Session 3 - AI-vs-AI balance measurement (first numbers)
- `TeamController.ReleaseHumanControl()` lets the AI play both sides. `BalanceReportTests` (runs only when a file named
  `run_balance.flag` exists in the project root) plays full 5-minute matches at 4x and writes `Documentation/balance_report.txt`.
- Mistake worth recording: I pushed two commits (`148d3d9`, `f137f6c`) whose PlayMode test assembly did not compile (missing
  `using FootballSim.Core`); every test run then failed to start until `781455a` fixed it. I also first blamed `[Explicit]`.
- Results, Professional difficulty, 2 matches (90 simulated minutes each): scores 0-1 and 0-0; shots 4-1 and 1-0 (on target
  0-1 and 0-0); passes about 26-45 per team; corners 0-3, 0-4; offsides 0-0 in both; fouls 6-11 and 8-4; yellows 1-3, 2-2;
  reds 1-1 and 1-0.
- Reading: the AI-only game is far too quiet and too hard on cards. A real match has roughly 20 shots, 300+ passes
  per team, 1-3 offsides, and a red card in far fewer than half of matches. Here shots and passes are an order of
  magnitude low and reds are almost every match. Only 2 matches, so these are indications, not statistics.
- Likely causes (NOT yet confirmed or fixed): the AI passes only when an opponent is within 3 m and otherwise dribbles; it shoots
  only inside a short range and when facing goal; the same chasing defender keeps fouling, so the 4th foul by one
  player (= red card) arrives quickly.

## Session 3 - AI tuning pass 1 and a frozen-ball bug (UNVERIFIED)
- Commit `860dcbe` tuned the AI (build-up pass after 1.6 s of possession, 1.25x shooting range, facing > 0.35, AI tackle
  cooldowns ~1.6x longer). Its effect is NOT known: the 3-match balance run hit NUnit's 180 s default timeout and wrote no report.
- That run's log exposed a real bug: during a restart hold the ball is kinematic, but players kept "touching" it every
  frame (hundreds of KB of "Setting linear velocity of a kinematic body" warnings; also changed `LastTouchTeam` during the hold).
  Fix: `PlayerController` ignores kicks, passes and shots while the ball is kinematic.
- `BalanceReportTests` now has `[Timeout(900000)]`.
- Unity was found closed mid-session and was reopened. After the fix: 28/28 EditMode and 41/41 PlayMode pass
  (the balance test is skipped unless `run_balance.flag` exists). Whether the AI tuning improved the numbers is still
  being measured; no tuned balance report exists yet.

## Session 3 - AI tuning results (3 AI-vs-AI matches per row, Professional, raw files in Documentation/balance_report_*.txt)
| | before (2 matches) | pass 1 (3) | pass 2 (3) |
|---|---|---|---|
| shots per team per match | 0-4 | 1-14 | 6-16 |
| passes per team | 26-45 | 17-25 | 21-32 |
| fouls per match (both teams) | 12-19 | 19-31 | 1-6 |
| red cards per match | 1-2 | 1-4 | 0 in all 3 |
| offsides | 0 | 0 | 0 |
| goals per match (total) | 0-1 | 0-3 | 0-1 |
- Pass 1 (commit `860dcbe`) did NOT help: the build-up-pass timer was reset whenever the ball was over 1.4 m away, which a
  dribbler's ball often is between touches, so it rarely reached 1.6 s; and AI tackles that missed the ball but touched a
  man were fouls, which went up.
- Pass 2 (this change, in `PlayerAI`): the possession timer only resets beyond 3.5 m; the AI only tackles when the ball is
  within 1.7 m and in front (dot > 0.4). Result: red cards disappeared, shots are now in a realistic range, tests 28 EditMode / 41 PlayMode pass.
- Still wrong (KB-018 stays open): passes are still about a tenth of real football, so offsides never happen; fouls (1-6)
  are now below realistic (~20) - the pendulum went too far; goals are low (conversion of on-target shots is poor).
  Sample is only 3 matches per row and varies a lot (e.g. shots 1 vs 16), so treat these as indications.
- Next idea (not done): let non-ball-carriers make forward runs and let the nearest teammate offer a pass, so the ball is passed
  more than dribbled; and let defenders tackle somewhat more often to bring fouls to a realistic count.

### Pass 3 (hold-time 0.7 s, pass cooldown 0.6 s) - small effect
- 3 matches: passes per team 23-36 (pass 2: 21-32), shots 3-16, fouls 0-4, reds 0, offsides 0. So hold time is NOT the bottleneck.
- Added static diagnostics in `PlayerAI` (frames with ball, passes queued, times no pass target was found), printed by the
  balance report, to find the real limit instead of guessing. Result of that diagnostic run: not yet read.
- The full test suite has not been re-run on pass 3.
- Diagnostic finding (`balance_report_tuning3b_diag.txt`, counters are cumulative over the 3 matches): about 100 passes were
  queued per match but only about 55-60 were counted, and "no pass target" was rare (3-15). So roughly half of the AI's
  passes were wasted: the AI decides while the ball is up to 1.4 m away but `PlayerController` only strikes within 1.1 m,
  and the queued request is consumed either way.
- Pass 4 (`StrikeRange` 0.95 m in `PlayerAI`, commit `b6237e3`) made things WORSE: counted passes fell to 15-23 per team
  (about 35-40 per match, from about 55), queued passes to 50-70 per match, because the ball is rarely within 0.95 m at the
  moment of decision (raw: `balance_report_tuning4_strike095.txt`). Goals rose (2-3, 0-1, 2-2) with 4 own goals in 3 matches.
- Pass 5: `StrikeRange` raised to 1.08 m (just inside the 1.1 m control radius), which should keep the "no wasted request"
  benefit without starving the window. Result (`balance_report_tuning5_strike108.txt`, 3 matches): passes counted 11-34 per
  team (about 29-55 per match) against about 83-93 queued per match, so range was NOT the main loss. Goals 2, 0, 1.
- Pass 6 (uncommitted-at-time-of-writing hypothesis fix): while the ball is held for a restart (kinematic) the AI player at
  the ball now waits instead of queuing passes/shots that `PlayerController` discards. Hypothesis: restart holds (about
  1.2 s each, plus goal pauses and half-time) account for the missing half of queued passes. Effect NOT yet known
  (measurement running when this was committed); the full test suite has not been re-run since the AI changes started.

### Pass 6 result and where tuning stops (`balance_report_tuning6_wait_on_hold.txt`)
- With the AI waiting while the ball is held for a restart: queued passes per match 48 / 64 / 48 and counted passes 40 / 57 / 47,
  so the "wasted request" loss is gone (hypothesis confirmed). Fouls 0-8, one red in 3 matches, shots 4-8 per team, goals 1-1-1.
- But the absolute number of real passes did not rise (~40-57 per match, both teams): the AI simply decides to pass that
  rarely, because only one player per team (the chaser) ever acts and the ball carrier is usually alone with the ball.
  Parameter tweaks will not fix that. It needs structural AI work: supporting players making runs and offering passing
  lanes, receivers moving to the ball and controlling it, one-two passes. That is a bigger piece of work, not tuning.
- Tests after all AI changes: 28/28 EditMode (unchanged) and 41/41 PlayMode pass.
- Lessons from this series: I committed three AI changes on the user's say-so before their effect was measured, and
  one (strike range 0.95 m) made things worse; measuring first (the counters) found the real cause in one run.

### Support runs (first structural AI change; UNMEASURED at commit time)
- `PlayerAI.SupportOrHomePosition`: when the team has the ball (last touch) and it is not a kick-off, midfielders and
  forwards (formation depth >= 0.25) move up to 12 m ahead of the ball in their own lane, never beyond the second-last
  defender (offside line minus 1.5 m), instead of sitting on their formation spot. Goal: give the ball carrier forward
  passing options so passes (and offsides) become more frequent.
- The balance measurement was still running and the full test suite had not been run on this change when it was committed.
- Measured afterwards (3 x 5-min AI-vs-AI, Professional): counted passes 69 / 56 / 50 per match (before: 40 / 57 / 47), shots 7-3, 16-10, 30-... mixed, offsides 0-0 in all three, fouls 2 / 4 / 4, no cards, goals 2 / 0 / 1 (one own goal). Passes are only marginally higher (noise-level on 3 matches), offsides still never occur. Support runs alone did not fix KB-018; receivers still do not move onto passes or control them deliberately. The full test suite has still not been re-run on this change.

## Still unverified from red cards / penalties
- No red card or penalty has been seen in live play (they depend on random fouls); penalty balance
  (how often the AI scores) is unknown; not yet in the APK on the phone.
- Lesson: the earlier in-editor checks passed because the editor resolved the script by type; only the build's validation exposed it. Run scene-level tests after every scene rebuild.
