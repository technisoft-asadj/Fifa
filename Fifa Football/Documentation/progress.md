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

### Pass receiver becomes chaser (measured)
- 3 x 5-min AI-vs-AI, Professional (`balance_report_tuning8_receiver.txt`): counted passes 55 / 64 / 57 per match (support runs: 69 / 56 / 50), queued 60 / 72 / 64, offsides 0-0 in all three, fouls 6 / 6 / 2, one yellow in two matches, no reds, goals 1 / 1 / 0.
- No measurable gain over support runs: pass volume looks capped by the hold timer (0.7 s) and pass cooldown, and offsides never occur because the AI never passes to an offside team-mate and support runs stay level with the offside line. KB-018 stays open.
- `BallController.FixedUpdate` now returns early while the ball is kinematic (restart hold); it was spamming "Setting linear velocity of a kinematic body" warnings. Covered by the full test run below.

### Shorter build-up hold (0.7 s -> 0.4 s): tried and reverted
- 3 x 5-min AI-vs-AI (`balance_report_tuning9_hold04_reverted.txt`): counted passes 48 / 69 / 68 per match (receiver run: 55 / 64 / 57), queued 54 / 77 / 73, offsides 0-0 in all three, goals 2 / 0 / 2. No change beyond noise, and "no pass target" rose (9 / 10 / 1), so the value was put back to 0.7 s.
- Conclusion for KB-018: pass volume is not capped by the hold timer, the cooldown or who chases the ball. Four AI passes in a row (hold time, strike range, support runs, receiver chaser) have each moved the numbers by less than the match-to-match spread. Further blind tuning is not worth it; the next useful input is a human playtest of how matches actually feel, and a larger sample (10+ matches) if tuning continues.

### Full-time panel layout
- The old single stats label (28 pt, 220 px box, vertical overflow) held up to 5 goal lines plus five stat lines (about 12 lines), so with several goals it ran over the result line and the REMATCH button. Now: result at y=190, a separate goals label (22 pt, 130 px, max 4 goals then "+N more") and a stats label (26 pt, 190 px), both truncating instead of overflowing.
- New PlayMode test `FullTimePanelTextFitsItsBoxesWithManyGoals` adds 7 goals, opens the full-time panel and checks both labels' preferred height fits their boxes. Full PlayMode suite 42/42 passes (EditMode untouched, 28).
- Not seen on a screen or a phone: the test checks text height only, not that the panel looks right, and it was not run against the old layout to confirm it would have failed.

### Kick-off: centre circle enforced for the human too
- Before: the team not kicking off was moved out of the centre circle once, at the whistle, so a human player could walk back in before the ball was played. Now `MatchManager` keeps every player of the non-kicking team out of the circle every frame while the kick-off is pending (`KeepOutOfCentreCircle`, shared with `PrepareKickoff`); the player is placed on the circle edge in his own half.
- New PlayMode test `NonKickingTeamCannotEnterCentreCircleBeforeKickoff` teleports a player into the circle and checks he ends outside. Full PlayMode suite 43/43 passes.
- Not played by hand: how it feels when a human runs at the circle (he is pushed back instantly, no animation or warning) is untested. Players of the kicking team are not restricted to their own half.

### Offside: stale positions no longer count
- A pending offside (runners flagged when the pass is played) is now cancelled when an onside team-mate other than the passer plays the ball, or when 6 s pass without a touch. Before, a runner flagged for one pass could be penalised for a touch much later or after another team-mate had played it. The passer's own touch does not cancel it.
- New PlayMode test `OnsideTeammateTouchingFirstCancelsPendingOffside`; full PlayMode suite 44/44 passes. Not watched in a live match, and the 6 s limit is a guess.

### Goal assists
- `BallController` now remembers the toucher before the current one (`PreviousToucher`, `PreviousTouchTime`). A goal records an assist when a different team-mate touched the ball within 6 s before the scorer; never on own goals or when the previous touch was an opponent's. The goal line reads e.g. `34'  HOME  Home_10 (assist Home_8)`; it appears on the full-time screen and in balance reports.
- Simplifications: an assist is simply the previous touch (a dribble that merely carried the ball is not a touch here, only kicks count), no assist table or player stats, not seen in a live match.
- New PlayMode tests `GoalCreditsTheTeammateWhoTouchedItBeforeTheScorer` and `NoAssistWhenScorerIsAloneOrPreviousTouchWasAnOpponent`.
- `LiveMatchAccumulatesStatsAndSummaryText` failed once (expected 2 passes, got 3): the AI chaser can legitimately pass in the same two frames as the forced pass. The assertion now requires at least one counted pass. This is a flaky-test fix, not a game change.

### Goalkeeper dives and saves
- New `GoalkeeperModel` (pure rules, 7 EditMode tests): reach 1.3 m standing / 2.3 m diving, balls above 2.4 m go over, save chance 0.15-0.92 falling with shot speed and distance from the body, shots up to 13 m/s are held and faster ones parried.
- `PlayerController`: for a goalkeeper the tackle button dives (0.45 s lunge, 1.5 s cooldown); `TrySave` rolls once per shot when an opponent's ball arrives within reach and moves towards him; a keeper kicking away an opponent's moving ball also counts as a save (`Saved` event). `MatchStats.saves[2]` is shown next to passes on the full-time screen and in balance reports.
- AI keeper (`PlayerAI.Goalkeeper`) dives when a shot will cross the line 0.9-4.5 m from where he stands within 0.6 s.
- Found by testing: the first version measured reach from 0.9 m above the capsule pivot (already about 1 m up), so ground shots never counted as reachable: 12 shots, 0 saves. After the fix and counting keeper kicks, `GoalkeeperSavesSomeShotsAndCountsThem` (12 shots at 18 m/s from 8.5 m, three targets) passes with at least 3 saves.
- Not verified: how it feels, whether dives look right (no dive animation; only the tackle animation plays), balance of save chances over a full match (not measured with the 3-match report), and the human cannot control the keeper's dive direction on touch controls beyond the stick. Penalty taker aiming and a replacement keeper after a red card are still not done.

### Balance with goalkeeper saves and assists (measured)
- 3 x 5-min AI-vs-AI, Professional (`balance_report_tuning10_keepers.txt`): score 3-0, 1-0, 0-1 (5 goals, 1.7 per match), shots on target 6 / 2 / 4 versus saves 3 / 1 / 3 and goals 3 / 1 / 1: every shot on target is a goal or a save, so the counts are consistent. Counted passes 79 / 64 / 49, offsides 0, fouls 4-5 per match, one yellow, no reds. Three of the five goals had an assist.
- Reading: keepers are not unbeatable (saved about 50% of shots on target) and goals per match are in a believable range. Three matches is a small sample; offsides and pass volume are still the open KB-018 items.

### Penalties against the new keepers
- Reviewing KB-016: a keeper never tackles (his tackle button is the dive), so he can never be sent off and a replacement keeper is not needed; a human already aims a penalty with the stick like any shot, so no separate aiming mechanic was added either. The AI keeper dives on penalties through the normal dive logic.
- New PlayMode test `PenaltiesAreMostlyScoredAndTheKeeperStillGetsSomeHands` (10 shots at 22 m/s from the spot, five targets) passes when run alone: at least 3 goals and at least 6 of 10 ending as a goal or a save. The goal/save split was not read out, and the whole suite was not re-run after adding it.

### Kit choice on the start screen
- New row "Your kit" (6 colours: red, blue, yellow, green, white, black) on the start panel; the choice is saved (`KitPalette`, PlayerPrefs `set_kit`). The away team gets blue unless the home kit is blue (red) or black (yellow), so the kits never clash. The subtitle names both colours; players recolour at once through `PlayerVisual.SetKit` (shirt, shorts and socks).
- Tests: 4 EditMode (`KitPaletteTests`) and PlayMode `ChoosingAKitRecoloursBothTeams`. EditMode 39/39, PlayMode 49/49 (one skipped balance test counted).
- Not seen on screen: the start panel got a new row and the PLAY button moved down to y=-305 in a 900x800 panel; nobody has looked at the layout or tried it on a phone. Goalkeepers wear the same kit as their team (no separate keeper kit), and the away colour cannot be chosen.

### Mini-league mode
- Start panel gets a "Mode" row (single match / league; the start panel is now 900 high and PLAY sits at y=-385). The league is three matches against Amateur, Professional and World Class opponents (3 points for a win, 1 for a draw); the difficulty row shows "Opponent: ..." and is fixed while it runs. At full time the result is recorded (human = home), the panel shows "League: N/3 played, P points" and the button reads NEXT MATCH, or NEW LEAGUE after the third match with a verdict (8+ points champions, 5+ runners-up).
- `MiniLeague` is static so it survives the scene reload between matches; it is not saved to disk, so quitting the game abandons the league, and leaving the league restores the saved single-match difficulty.
- Tests: EditMode `MiniLeagueTests` (6) and PlayMode `MiniLeagueFixesTheOpponentAndScoresTheMatchAtFullTime`. EditMode 45/45, PlayMode 50/50.
- Not seen on screen: the new Mode row and the league line on the full-time panel were never viewed; a league across three real matches was not played through (only one result is driven in the test). Draws score a point because there is no extra time or penalty shoot-out.

### Documentation refresh (2026-11-13)
- `architecture.md` rewritten to match the code as it is now (module table, test layout, honest list of architectural debt incl. "not netcode-ready" and the oversized `MatchHUD`); `plan.md` phase list now states where each phase actually stands. No code changed.
- A new Android APK ("Fifa Football.apk", about 34 MB, in the git-ignored `Builds/Android/`) was built at 19:47 with everything since the pause-menu build; it was not installed (no phone connected) and never run.

### Online groundwork: seeded random and a determinism probe
- New `SimRandom` (seedable xorshift) replaces `UnityEngine.Random` in all gameplay decisions (tackles, shot error, keeper saves, fouls, AI aim, away formation); only the cosmetic kick-animation choice still uses Unity's. 7 EditMode tests (`SimRandomTests`).
- New `DeterminismProbeTests` (gated by `run_balance.flag`): plays AI vs AI from the same seed twice at a fixed 60 fps frame time and compares the end state. Result (`determinism_report.txt`, `determinism_report_first_run.txt`): the runs are NOT identical, even after 2 frames; the ball (a PhysX Rigidbody) differs first, players follow from about frame 60, and after 30 s of play scores differ. So the simulation cannot run in lockstep on two devices; the plan is server-authoritative play and a custom fixed-step ball integrator. Details, options and the ordered groundwork list are in the new `networking.md`. No networking package was added or tried.
- Test status: EditMode 52/52. PlayMode: the full run was 49 of 50 (the balance and determinism probes counted as skipped): `KickAnimationMovesTheKickingLeg` failed once (leg swing 16.8 degrees against 30 needed) and passed on every re-run alone; it sampled 20 frames of a time-based animation, so I changed it to sample half a second of real time and re-ran it alone (passed). The full PlayMode suite was not re-run after that test edit.

## Determinism probe, second round (2026-11-13)
`DeterminismProbeTests` now compares the default physics step against `fixedDeltaTime = 1/60`. With one physics step per frame the two runs were identical at 60 frames and 0.02 m apart at 300 (default step: 6 m apart); they still differ by 900 frames. Conclusion: step timing is a major cause, not the only one. Details in `networking.md`; raw output in `determinism_report.txt` and `determinism_report_default_timestep.txt`. Single samples, indicative only.

## Determinism: causes found and fixed (2026-11-13)
Traced the first differing frame between two seeded runs. The ball (PhysX) was reproducible; two bugs of ours were not: gameplay timers read the absolute float `Time.time`, and gameplay read the interpolated `ball.transform.position`. Added `SimClock` (match clock in double precision, reset per scene) and `BallController.Position` (`rb.position`). With a 1/60 physics step, runs were identical for 400 frames (three runs at very different start times) and 1200 frames (two runs). Not tested: the default 0.02 step (the game still ships with it), different devices, or matches longer than 20 s. Raw output: `determinism_after_fix_*.txt`. See `networking.md`.

## Shipped physics step is now 1/60 (2026-11-13)
`ProjectSettings/TimeManager.asset` Fixed Timestep changed from 0.02 to 0.016666668 so the game runs the step the determinism tests proved reproducible. The ball's drag and spin code scale with `fixedDeltaTime`. EditMode 52/52 and PlayMode 50/50 pass. Not measured: the extra physics CPU cost on a phone (60 Hz instead of 50 Hz), and whether ball feel changed by hand (the balance report was not re-run).

## Balance check at the 1/60 step (2026-11-13)
Two Professional-vs-Professional runs of three matches each (`balance_report_step60.txt`, `balance_report_step60_run2.txt`), compared with `balance_report_tuning10_keepers.txt` (0.02 step). Run 1: 6 goals, 33 shots, 10 saves, 0 offsides, no assists credited. Run 2: 4 goals, 33 shots, 8 saves; assists were credited on 2 of 4 goals, so the assist rule works after the `SimClock` change and run 1 was chance. Offsides stay at 0 (KB-018). No sign that ball feel changed; 6 matches is a small sample, and nothing was tried by hand or on a phone.

## Input recording and replay (2026-11-13)
Added `InputFrame`, `RecordingInput` and `ReplayInput` (Scripts/Input/InputRecording.cs) and a gated probe that records an AI-vs-AI match and replays it from the recorded inputs alone with the AI off: identical in all 900 frames. The AI's random choices now use their own stream (`SimRandom.Ai`, `RandomStream` class) so the simulation's numbers do not depend on whether the AI runs. EditMode 54 (2 new). Not covered: human/touch input, player switching, two devices, real network. Report: `determinism_replay.txt`.

## Wire format sketch (2026-11-13)
`NetCodec` encodes a match snapshot in 174 bytes (22 players, ball, score, clock) and an input frame in 5 bytes; `SnapshotCapture` fills a snapshot from the live match. Estimate: 4.0 KB/s at 20 Hz, 6.1 KB/s at 30 Hz including UDP/IP headers, against the 20 KB/s target. 4 new EditMode tests (round trip within quantisation, size under the target, input round trip) and 1 PlayMode test (live match positions survive encoding). Nothing is sent over a network yet. Details in `networking.md`.

## Client interpolation (2026-11-13)
`SnapshotInterpolator` plus a loopback probe: with 20 Hz snapshots and a 100 ms render delay the client picture stayed within 0.11 m (players) and 0.22 m (ball) of the server's truth in a 15 s AI match, mean errors a few millimetres. No loss or jitter simulated; local-player prediction not built. 7 EditMode tests. See `networking.md`.

## UDP loopback (2026-11-13)
`UdpEndpoint` plus 4 EditMode tests: snapshot and input frame cross a real local socket intact. EditMode 69/69 (PlayMode not re-run: no gameplay code touched). Loopback only; not tried on a device or a network. See `networking.md`.

## In-process UDP server and client (2026-11-13)
`NetServer` and `NetClient` stream a live match's snapshots over a real loopback UDP socket into the interpolation buffer. One PlayMode test (client receives at least 90% of snapshots sent; newest view near the server's player). Loopback only, nothing drawn, no client input, no loss simulation. A phone or second-process test is the next real step and needs hands-on work. See `networking.md`.

## Bad-network simulation (2026-11-13)
`NetworkConditioner` (loss, latency, jitter, reordering, seeded) plus 7 EditMode tests; EditMode 76/76. With the synthetic scenario, a 100 ms render delay is accurate on a clean link, about 200 ms is needed with 5% loss and 40 ms jitter, and a very short delay starves the buffer. These are asserted thresholds, not measured figures. Adaptive render delay and local prediction are not built. See `networking.md`.

## Client input over the socket (2026-11-13)
`RemoteInput`, `NetClient.SendInput` and server-side `RegisterRemote`: a client's input packets drive one handed-over player; others are rejected and counted. 1 PlayMode test. No authentication, no input tick numbers, no prediction; loopback only. See `networking.md`.

## Input packet hardening (2026-11-13)
Input packets now carry a per-player random session token and a 16-bit sequence number (13 bytes). Wrong token or unregistered player: dropped and counted; repeated or older sequence: ignored and counted. EditMode 80/80 (4 new). Token is clear text and brute-forceable, with no handshake to hand it to a client: this is a first layer, not security. See `networking.md`.

## Join handshake and client view (2026-11-13)
Clients now join, get a player index and token from the server (distinct per client; the third is refused as Full), and can draw the match from snapshots alone (`NetClientView`). 2 PlayMode tests; EditMode 80/80. No timeout for vanished clients, no join approval, loopback only. See `networking.md`.

## Client timeout and keep-alive (2026-11-16)
A client silent for more than 5 s (configurable) is dropped: its player goes back to the AI, the slot is re-offered with a fresh token. Clients send a hello every second as keep-alive. 1 PlayMode test. No reconnect that keeps identity, no leave message, loopback only. See `networking.md`.

## Smooth render clock (2026-11-16)
`RenderClock` gives the client view a steady playback time that eases toward newest-minus-delay and snaps on stalls. EditMode 85/85 (5 new). Constants untuned; no adaptive delay or prediction. See `networking.md`.

## Leave message (2026-11-16)
Clients say goodbye (`Leave()`, also on destroy) and the server frees the slot at once; a stranger's Leave is ignored; the timeout remains the fallback. 1 PlayMode test; EditMode unchanged 85/85. Address not authenticated. See `networking.md`.

## Adaptive render delay (2026-11-16)
`AdaptiveDelay` measures arrival jitter and sets the client view's delay between 6 and 30 ticks. EditMode 90/90 (5 new). Constants are guesses; synthetic tests only. See `networking.md`.

## Client over a simulated bad network (2026-11-16)
`NetClient.Inbound` test hook plus a PlayMode test: with loss, latency and reordering the adaptive delay rises and the view still tracks the match. PlayMode 59 tests (58 passed before the new one, new one passes). Server-to-client direction only. See `networking.md`.

## Slot bound to the joining address (2026-11-16)
Input for a joined slot is accepted only from the address that joined; a copied token from elsewhere is refused and counted. 1 PlayMode test; the 3 existing input/join tests still pass. Address spoofing and clear-text tokens remain. See `networking.md`.

## Input sender (2026-11-16)
`NetInputSender` turns a local input source into network input frames for a joined client. 1 PlayMode test. Not tried with real keyboard or touch input; no host/join screen; no prediction. See `networking.md`.

## Session helpers (2026-11-16)
`NetSession.Host` / `NetSession.Join` wrap the server, client, view and input sender. 1 PlayMode test. Loopback only; no menu screen, no discovery. See `networking.md`.

## Listening beyond loopback (2026-11-16)
Server can opt in to all interfaces and a fixed port; clients pick the socket to match the server address. Default unchanged (loopback). EditMode 93/93 (2 new). Never crossed a real LAN; no rate limit or join approval, so not safe to expose. See `networking.md`.

## League saved between sessions (2026-11-16)
`MiniLeague` now saves to PlayerPrefs after every change (start, result, abandon) and restores on first use, so closing the game mid-league no longer abandons it. Saved data that cannot be right (counts or points that do not add up) is discarded with a warning and cleared. EditMode 96/96 (3 new: restart mid-league, finished/abandoned restore, nonsense data). Not tried on a phone; PlayerPrefs on Android is the normal shared-preferences store, but I have not confirmed it survives an app update or a 'clear cache'.

## Training mode (2026-11-16)
A third value on the start screen's mode toggle (Single match -> League -> Training -> Single). In training the away outfield players stand where they are (`PlayerAI.passive`), their goalkeeper still plays, and the match is 120 minutes long, so full time is not reached in a session. `TrainingMode.Active` is static (survives the scene reload, not saved to disk: each launch starts as a single match). Goals still count and restart at the kick-off; fouls, offsides and the rest of the rules still apply. PlayMode 2 new tests: dummies stay within 1 m over 480 frames at 60 fps and the match is not finished and is over 100 minutes long; the toggle cycles through all three states and back. Not seen on screen: the mode label text and layout were never viewed, nothing was tried on a phone, and a person has not played a training session. There is no drill content (cones, targets, a ball feeder), no reset-ball button and no stats readout; it is free play against stationary defenders only.

## Tournament (2026-11-16)
A three-round knockout (quarter-final vs Amateur, semi-final vs Professional, final vs WorldClass) as a fourth value on the mode toggle (Single -> League -> Tournament -> Training -> Single). A win advances, a loss eliminates, and a draw replays the match because there is no extra time or penalty shoot-out. `Tournament` is static, saved to PlayerPrefs after every change and restored on first use; saved data that does not add up (out and also won, too few result lines, round out of range) is discarded with a warning. The HUD records the result at full time, shows the summary line, and the rematch button reads NEXT MATCH or NEW TOURNAMENT; `BeginMatch` now also re-applies a running league or cup's fixed opponent. EditMode 103/103 (7 new). PlayMode: toggle cycle updated, plus one test that drives a short match to full time and checks one result is recorded. The label texts and layout were never looked at on screen, nothing was tried on a phone, and a whole cup was not played through by a person. Draw replays could in theory repeat forever; the bracket is a ladder against the AI, not a draw of 8 teams.

## Penalty shoot-out for tournament draws (2026-11-16)
`ShootOut` is the shoot-out rules, simulated rather than played: five kicks each, home first, stopping as soon as the other side cannot catch up, then sudden death in pairs (capped at 50 pairs, then a coin so a cup can always continue). Each kick scores with a fixed chance: the human side 0.75; the AI 0.65 / 0.75 / 0.80 for Amateur / Professional / WorldClass. A drawn tournament match is now settled by it instead of being replayed: `Tournament.RecordResult(h, a, shootOut)` advances or eliminates, the result line carries the penalty score, and the full-time panel shows who won on penalties. A match that was not a draw ignores any shoot-out passed in; with none supplied a draw still replays. EditMode 111/111 (8 new). The player does not take or save these kicks, there is no screen for the sequence, and the chances are my guesses, not measured from the in-game penalty system.

## Season (career-lite) (2026-11-16)
A fifth mode (Single -> League -> Tournament -> Season -> Training -> Single): a one-round-robin season for six teams, You plus Rovers, Athletic, United, City and Wanderers (weakest to strongest; difficulties Amateur, Amateur, Professional, Professional, WorldClass). The human plays five matches, one per opponent, weakest first. In every round the other four teams play two matches that are simulated (Poisson goals around a per-team average) from a saved seed, so the table is recomputed from the seed and the human's own results and only those are saved to PlayerPrefs. The circle method schedules the pairings, and a test checks that every pair of the six teams meets exactly once. The table sorts by points, goal difference, goals scored. The full-time panel shows the summary line (place, points, next opponent) and the rematch button reads NEXT MATCH or NEW SEASON. Draws count as one point; there is no extra time or shoot-out in a season. Saved data that does not parse is discarded with a warning. EditMode 119/119 (8 new); PlayMode: the toggle test now walks all five presses, plus a test that a season match records exactly one result and the table matches the score.
Limits: this is a season ladder, not a career: no transfers, squads, player growth, money, calendar, cups alongside the league or multi-season history. The full table is built (`Season.TableText()`) but no screen shows it, only the one-line summary. The simulated teams' strengths and goal averages are guesses with no tuning; the label text and layout were never looked at on screen, nothing was tried on a phone, and a whole season was not played by a person.

## Reset saved progress (2026-11-16)
`SaveData.ResetProgress()` ends any league, tournament, season and training mode (in memory and on disk) and leaves settings alone; `SaveData.ResetAll()` also clears the start-screen choices, shirt colour, touch-control size and opacity, and the sound switch, deleting only this game's own keys (never `DeleteAll`). The start screen has a small "Reset progress" button bottom-left, next to PLAY: the first tap arms it ("Tap again to erase", for 4 seconds), the second tap erases and returns to a single match with the saved difficulty. The pref-key constants in `TouchControls` and `MatchAudio` became public so the reset can name them. EditMode 122/122 (3 new); PlayMode: one test for the two-tap behaviour. Nothing yet calls `ResetAll` from the UI: there is no settings screen, only this button and the existing sound and touch-size controls in the pause menu. The button's position and size were never looked at on screen and may overlap something on a small phone. The reset tests clear the same editor PlayerPrefs keys the game uses, so running them in the editor resets the developer's own saved settings.

## LAN address helper (groundwork for a host/join screen)
- Android IL2CPP build of everything up to the reset button: succeeded (0 errors, 15 warnings, ~19 min). Compiles for Android; nothing about phone behaviour is verified.
- `Net/LanAddress.cs`: `TryParse` ("a.b.c.d" or "a.b.c.d:port", IPv4 only, no hostnames, default port 7777), `LocalAddress()` (this device's private IPv4 via an unsent UDP connect, null when offline), `IsPrivate`. 19 EditMode tests, all pass.
- NOT done: the host/join screen itself. A guest needs the local simulation switched off and the snapshot view drawn with a camera; that needs a second device to verify, so it is deferred rather than guessed.

## Career record
- `Core/CareerRecord.cs`: lifetime matches / wins / draws / losses / goals, saved to PlayerPrefs (`career_record`) after every match; saved data that does not add up is discarded with a warning. Recorded in `MatchHUD.OnFullTime` for every real match (training excluded). `SaveData.ResetProgress` keeps it; `SaveData.ResetAll` clears it.
- Start screen shows it as a small label (W/D/L, GF/GA) beside the PLAY button, opposite "Reset progress". The label has never been viewed on screen.
- Tests: EditMode `CareerRecordTests` (8), PlayMode `FullTimeAddsOneMatchToTheCareerRecordButTrainingDoesNot`. Totals: EditMode 149/149, PlayMode 68/68 (5 gated probes Inconclusive, expected).

## Squad growth
- `Players/TeamGrowth.cs`: a squad level from the saved career record (2 points per win, 1 per draw, a level per 4 points, capped at 6). Each level adds +1 to every outfield attribute of the home team, capped at 99. No save data of its own: it follows `CareerRecord` and goes back to 0 with a full reset.
- `PlayerController.SetGrowth(levels)` applies it to a private copy of the player's data (the shared asset is never changed, repeated calls do not stack, 0 restores the original). `MatchHUD.BeginMatch` applies it to the home outfield only; the keeper and the away side are unchanged.
- Start screen: the record label gets a "Squad lv N" line once the level is above 0.
- Effect size: +6 at the cap moves speed by about 0.1 m/s and shot power by about 1 m/s; deliberately small. Whether it feels like growth, or upsets the difficulty balance, has not been judged by playing.
- Tests: EditMode `TeamGrowthTests` (3), PlayMode `SquadGrowthRaisesOnlyTheHomeOutfieldOnACopyAndDoesNotStack`. Totals: EditMode 153/153, PlayMode 69/69 (5 gated probes Inconclusive, expected).

## Start screen seen for the first time, two fixes
- Captured the start screen in play mode (editor, 2:1 game view). Found and fixed: the "Reset progress" label was wider than its button (font 22 now, button and record label moved 25 px inward so they sit inside the panel), and PlayMode tests that play a match to full time were leaking results into the saved career record (the screen showed W1 GF1 with no match played). `PrototypeSceneTests` now clears `CareerRecord` in SetUp and TearDown.
- Still not seen: a phone-shaped screen (portrait or notch), the other modes' labels, the full-time and pause panels. Remaining known leak: some tests save a match difficulty into the editor's saved settings (harmless to the build, visible only in the editor).
- Totals: EditMode 153/153, PlayMode 71/71 (5 gated probes Inconclusive).

## Screens checked in the editor, two more fixes
- Captured in play mode (editor wide view): the Season mode label, the pause panel and the full-time panel. Fixed: "1 points" in the season and league summaries (`Season.PointsText`, test `OnePointIsNotWrittenAsOnePoints`) and the pause panel's RESUME and SOUND buttons touching the panel edges (370 wide, 20 margin).
- Still not seen: a true landscape phone shape (the editor's Game view pane overrode the size I set), touch controls (none appear without a touchscreen), League, Tournament and Training labels, the shoot-out text on the full-time panel.
- Totals: EditMode 154/154; PlayMode last full run 71/71, plus the pause and season tests re-run after the last change (3/3).

## Shoot-out text was invisible, fixed
- Looking at a drawn tournament match on the full-time panel showed "DRAW 0 - 0" and nothing else: the "WIN x - y ON PENALTIES" line had been added as a second line to a one-line-high text box and was cut off. It now sits on the same line in a smaller size: "DRAW  0 - 0  (HOME WIN 4 - 1 ON PENALTIES)". New PlayMode test `ADrawnTournamentMatchShowsThePenaltyResultOnTheResultLine`.
- Seen in the editor (wide view): League, Tournament and Season mode labels, the pause and full-time panels. Not seen: Training label, a true landscape phone shape, touch controls.
