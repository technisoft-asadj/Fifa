# Networking: evaluation and groundwork (phase 7, started 2026-11-13)

Status: **nothing online exists yet.** This document records what was measured, what is still unknown, and the order of work. No networking package is installed (the project only has `com.unity.multiplayer.center`, an informational window).

## Targets (from plan.md, unconfirmed on devices)
1v1 first, server-authoritative; 20-30 Hz server tick; under 20 KB/s per client; input-to-action latency under 100 ms. 11v11 with 22 humans is a post-launch stretch.

## What was measured: the simulation is not deterministic
`DeterminismProbeTests` (gated by `run_balance.flag`) plays AI vs AI from the same seed twice with `Time.captureFramerate = 60` and compares the end state. Raw output: `determinism_report.txt` (short runs) and `determinism_report_first_run.txt` (300 and 1800 frames).

| Frames | What differs between two identical runs |
|---|---|
| 2 | ball height only (0.1129 vs 0.1112 m); all 22 players identical |
| 10 | identical to 4 decimals (ball z 0.0872) but flagged different on a smaller difference |
| 30 | ball z 0.7145 vs 0.6992; players still identical |
| 60 | players start to differ too (sum 59.168 vs 59.066) |
| 300 | ball 0.04 m apart, players 4 units apart in the position sum |
| 1800 (30 s) | different scores (0-2 against 0-0) and different pass counts |

Reading: the first divergence is the **ball**, within two frames of kick-off. The ball is a PhysX Rigidbody, and its settling and first steps are not reproducible from run to run in this setup. The AI reacts to the ball position, so the small difference grows until the matches are unrelated. Gameplay randomness is not the cause: all gameplay dice now come from the seeded `SimRandom`, and the runs still differ.

What this does and does not show: it shows that two runs of the same build on the same machine diverge. It does not isolate which part of PhysX or of frame timing causes it, and it does not test two different devices (which would be worse: PhysX is not deterministic across platforms).

## Consequences for the design
- **Lockstep or rollback netcode on the current simulation is not feasible.** It would need bit-identical results on both phones.
- The practical model is **server-authoritative with client interpolation (and later prediction)**: one machine runs the real simulation, clients send inputs and render snapshots. For 1v1 a snapshot is 22 players + ball, which fits the bandwidth target in principle (not measured).
- Determinism is still worth pursuing, because it makes replays, tests and prediction easier. The plan's decision 2 (a custom ball integrator, PhysX only for queries) is now supported by evidence.

## Options for the transport layer (none tried yet; versions and fit not verified)
| Option | For | Against / unknown |
|---|---|---|
| Netcode for GameObjects (NGO) with Unity Transport | Standard Unity route; object and RPC layer included | Not installed; fit for a 22-player snapshot and a dedicated server build is untested |
| Unity Transport directly, own protocol | Full control over snapshot size and tick | More code to write and test |
| Relay/lobby services | Easier private rooms and NAT traversal | Account and cost questions not looked at |
Decision pending: build a two-client spike (one editor as server, one build as client) before choosing. Record the result here.

## Ordered groundwork (what to do before any network code)
1. Seeded gameplay random source: **done** (`SimRandom`, 7 EditMode tests; tackles, shots, keeper saves, fouls, AI aim).
2. Fixed-step simulation loop: player movement, AI and match rules still run in `Update` with a variable `Time.deltaTime`.
3. Replace the Rigidbody ball with a custom fixed-step integrator (drag, Magnus spin, bounce, ground friction), then re-run the probe; success = identical runs.
4. Command-based input per tick (the `IPlayerInput` seam exists; it needs a tick number and serialisation).
5. Separate simulation state from presentation (the HUD already listens through events; `PlayerVisual` and audio must stay out of the simulation).
6. Only then: the two-client spike, a snapshot format, interpolation, and a measured bandwidth figure.

## Open questions
- Is 20-30 Hz enough for tackles and shots to feel right on a phone connection? Unknown until measured.
- Hosting: dedicated server builds, relay, or player-hosted? Cost and ops not assessed.
- Anti-cheat and accounts: out of scope until phase 10.
