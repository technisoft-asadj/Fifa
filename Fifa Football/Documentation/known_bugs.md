# Known bugs and limitations

Severity: H = blocks gameplay, M = noticeable, L = cosmetic. Nothing below was reproduced by a human playtest unless stated; items marked "untested" are risks, not confirmed bugs.

| ID | Sev | Description | Status |
|---|---|---|---|
| KB-001 | M | Controls/tackle/switching feel has never been playtested by a person. | Untested |
| KB-002 | M | Camera did not follow the newly controlled player after switching. | Fixed (BroadcastCamera.SetPlayer called by TeamController); not yet seen with a human switch |
| KB-003 | M | No offside, fouls, cards, penalties or referee. | Not started |
| KB-004 | M | AI has no marking, passing lanes or attacking runs; one chaser per team. | Open |
| KB-005 | L | All 22 players share one `PlayerData` asset. | Open |
| KB-006 | L | Boundary walls remain behind the lines as a safety net; ball can bounce off them before the restart triggers. | Untested |
| KB-007 | L | Set-piece taker is teleported next to the ball (no walking, no animation). | By design for now |
| KB-008 | L | Stale material asset `PlayerHome.mat` is left over from the first prototype. | Open |
| KB-009 | M | Android: ARM64 only (ARMv7 not enabled); 16 KB page size and device builds not verified. No Android build made yet. | Open |
| KB-010 | L | Company name `Trisoftic` and package ID `com.trisoftic.footballsim` were chosen from the folder name; confirm before release. | Needs confirmation |
| KB-011 | H | "Script attached to 'Trigger' ... is missing" (seen as the Unknown-script warning). Cause: `GoalTrigger` was declared inside `MatchManager.cs`, so the scene could not serialise it and goals were not wired in the saved scene / Android build. | Fixed: GoalTrigger moved to its own file; scene rebuilt; scene-level tests added. APK rebuilt and run on a phone without the error |
| KB-012 | M | Touch controls need a hands-on test: multi-touch (stick + button together), reach and size. | Partly verified on an Infinix X6881: buttons show, joystick follows a drag, canvas uses the safe area. Hands-on play still needed |
| KB-013 | L | Keyboard controls hint is shown at the bottom even on touch devices. | Fixed, confirmed on device |
| KB-015 | M | Android back key does not pause/resume on device (tested with adb `keyevent 4` on Infinix X6881). Code checks `Keyboard.current.escapeKey`; the key may not reach the Input System under GameActivity, or adb injection may differ from the real button. | Open - needs a check with the real back gesture |
| KB-016 | L | Goalkeepers are never sent off (no replacement-keeper logic). Penalties resume as normal play; no aiming/dive mechanic. | Open (by design for now) |
| KB-017 | L | Offside is approximate: any touch by an offside-positioned player is penalised (no "interfering" check), and the exempt-first-strike flag for throw-ins/corners/goal kicks persists if the taker never strikes. Not watched in a live match. | Open |
| KB-018 | M | AI-vs-AI matches are unrealistically quiet (about 0-5 shots and 26-45 passes per team per match; offsides never) and red cards occur in most matches (measured on 2 matches, see progress.md). | Partly fixed: red cards and shot counts now realistic; passes (~20-25 per team, ~40-57 per match) and offsides still far too low because only one AI player per team acts (needs support runs / passing lanes, a structural change); fouls now low (0-8) (see progress.md) |
| KB-014 | M | Game ran at a locked 30 fps on Android (Unity's default mobile cap). | Fixed: `AppBootstrap` sets 60 fps; measured steady 60 on device |
