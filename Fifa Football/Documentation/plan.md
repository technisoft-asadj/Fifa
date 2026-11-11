# Football Simulator - Revised Plan (v1.1)

Source: `My Plan/FOOTBALL SIMULATOR.docx` (v1.0). This file refines it; the docx stays the master vision.

## Release tiers (scope control)
- **MVP (ship first):** offline Kick-Off, Training, Tournament, League; 1 stadium; 5 formations; AI; local saves; optional ads.
- **v1.1:** 1v1 online (server-authoritative), matchmaking, private rooms, rankings.
- **v2 (stretch):** 11v11 online (22 humans), Career mode, extra stadiums.
- Cut-line if time runs out: 11v11 -> Career -> weather -> extra camera modes.

## Key technical decisions
1. **Deterministic simulation core.** Gameplay runs on a fixed timestep (50 Hz) driven by input commands, kept separate from presentation, so offline and online share one rule set and prediction/rollback stays possible.
2. **Custom ball integrator** (drag, Magnus spin, ground friction, bounce). PhysX is used only for triggers/collision queries. PhysX is not deterministic across platforms.
3. **Data-driven** players, formations, tactics and ball parameters (ScriptableObjects).
4. **Stylised-realistic art** target; character budget ~5-8k tris with LODs.
5. **Unified input abstraction** (`IPlayerInput`) shared by keyboard, gamepad, touch and network.

## Measurable targets (to be confirmed on real devices)
| Metric | Target |
|---|---|
| Input-to-action latency | < 100 ms |
| Frame rate | 60 FPS capable devices, stable 30 FPS floor on reference low-end |
| Network (1v1) | < 20 KB/s per client, 20-30 Hz server tick |
| Crash-free sessions | > 99.5% |
| Reference devices | to be named (low 3 GB, mid, flagship) |

## Phases
0. Audit & foundation -> **in progress** (see progress.md)
1. Football prototype (pitch, player, ball, pass, shoot) -> **in progress**
2. Core gameplay (dribble, tackle, switching, timer, score, offside/fouls)
3. **Vertical slice:** 1 stadium, 2 teams, full match vs AI
4. Characters & animation
5. Football AI, formations, difficulty
6. Offline modes & save system
7. Networking evaluation + dedicated server (decide NGO vs custom; record in networking.md)
8. 1v1 online
9. Stadium/graphics/audio, replays, commentary
10. UI, accounts, backend
11. Ads, optimisation, device QA
12. Platform QA & publishing
(11v11 = post-launch stretch.)

## Risks
- ARMv7: project is currently ARM64 only. Verify Unity 6 ARMv7 viability before promising it; likely drop (Play Store requires 64-bit).
- 16 KB page size: verify when first Android build is made.
- Thermal throttling and memory on low-end phones.
- Character/animation quality on zero budget.
- Network feel for 22 players.
