# Status and handoff

Written 2026-11-17. This is the short version; `progress.md` has the detail for every feature, `known_bugs.md` the open problems, `plan.md` the roadmap, `networking.md` the network design.

## What exists (all tested in the editor)
- A playable 3D football match: touch and keyboard controls, AI teams, goalkeepers, fouls and cards, offside (approximate), penalties, corners, throw-ins, goal kicks, stamina, formations, three difficulties, kit colours, sound.
- Five modes on the start screen toggle: single match, league (3 matches), tournament (3-round knockout with simulated shoot-outs), season (6-team round robin with a simulated table), training (stationary opponents, endless clock). League, tournament and season are saved between sessions.
- Lifetime career record (matches, W/D/L, goals) shown beside PLAY, and a squad level earned from results (+1 to every home outfield attribute per level, max level 6).
- Two-tap "Reset progress" on the start screen (keeps settings). `SaveData.ResetAll` also clears settings and the career record.
- Networking (server-authoritative over UDP): snapshots, join handshake with a per-player token bound to the joining address, input packets with sequence numbers, keep-alive, timeout, leave message, adaptive render delay, smooth render clock, input acknowledgement (type 7), a bad-network simulator for tests, `NetSession.Host/Join` helpers, `LanAddress` parsing.

## Test state
EditMode 154/154. PlayMode 76/76, plus 5 gated probe tests that show as Inconclusive unless a file named `run_balance.flag` exists in the project root (expected). Android IL2CPP build succeeds (0 errors).

## Never checked by a person or on a device
- Anything on a phone: touch feel, frame rate with the newer code, the real Android Back gesture (adb Back works, KB-015).
- Seen in the editor's wide view and a tall narrow view (not a true landscape phone shape): the start screen, all five mode labels, the pause and full-time panels. Not seen: touch controls (they need a touchscreen), the "Squad lv" line, the in-match HUD on a phone.
- Whether the five modes and the squad growth feel right when played.
- Real networking: nothing has run between two machines. Tokens travel unencrypted and there is no join approval or rate limit, so a server open to the internet is not safe.
- Constants chosen by guess: shoot-out chances, season goal averages, clock snap and adaptive-delay margins, squad growth size.

## Not built
- Host/join: built (start screen "Online" button, same Wi-Fi, free). Verified in the editor and by tests only; never run between two devices. The guest sees the host's match but its scoreboard shows "ONLINE GUEST" (not fed by the server).
- Client-side prediction (the ack it needs is in; the replay logic is not).
- A full career (squads, transfers, player development beyond the squad level).
- AI-versus-AI realism (KB-018): too few passes and no offsides; needs support runs and passing lanes.
- Art, audio assets, accounts, store listing.

## How to run and test
- Open `Fifa Football` in Unity 6000.3.5f1. Tests: Window > General > Test Runner (EditMode and PlayMode).
- Android: the latest build is `Builds/Android/Fifa Football.apk` (git-ignored). Install with `adb install -r "Builds/Android/Fifa Football.apk"` with USB debugging on.
- Balance and determinism probes: create `run_balance.flag` in the project root, run PlayMode, delete the file afterwards (never commit it).

## Pushing
Roughly 22 commits are local only; `origin/main` is still at `d2365c5`. Sign in to GitHub on this machine, then run `git push origin main` from `D:\Trisoftic Unity Games\Football\Fifa`. These files are deliberately never committed: `Assets/Settings/Mobile_RPAsset.asset`, `PC_RPAsset.asset`, `UniversalRenderPipelineGlobalSettings.asset`, `ProjectSettings/EditorSettings.asset`, `ProjectSettings.asset`, `UnityConnectSettings.asset`, `Assets/InitTestScene*`, `Assets/_Recovery*`, `run_balance.flag`.
