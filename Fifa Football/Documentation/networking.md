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

### Follow-up: does physics step timing explain it? (`determinism_report.txt`, default step in `determinism_report_default_timestep.txt`)
Second probe: same seed, run twice, with the default `fixedDeltaTime` (0.02) against `fixedDeltaTime = 1/60` (exactly one physics step per rendered frame).

| Frames | Default step | 1/60 step |
|---|---|---|
| 2 | differs (ball height) | differs (run A's players differed too: first run after changing the step, probably a start-up artefact; not isolated) |
| 60 | differs (ball 0.04 m apart) | **identical** |
| 300 | differs (ball 6 m apart) | differs, but only 0.02 m (vs 6 m) |
| 900 | differs | differs (ball 24 m apart) |

Reading: aligning physics to frames removes most of the early divergence, so step timing is a major cause. It is not the only one: something still diverges between 60 and 300 frames (candidates: PhysX contact solving order, non-seeded `Random` or time-dependent code left in a script, `Update` ordering). Not isolated yet. One sample per case; treat as indicative, not proven.

### Follow-up 2: the remaining causes found and fixed (`determinism_after_fix_400_frames.txt`, `determinism_after_fix_1200_frames.txt`)
A per-frame trace (physics step count, ball position and velocity, player positions) located the first differing frame. In the 1/60 setup the ball and step count were identical in both runs and the **players** drifted first (about 0.0005 units at frame 171, sooner when the two runs started at very different `Time.time`). Two causes, both in our code and not in PhysX:
1. **Gameplay timers used `Time.time`.** It is an absolute float, so `nextThink`, the pass-receiver window, the offside snapshot and the assist window rounded differently depending on how long the editor had been running, shifting AI decisions by a frame. Fix: `SimClock` (double, starts at 0 on every scene load, advanced by `Time.deltaTime` first in the frame).
2. **Gameplay read `ball.transform.position`**, which the Rigidbody interpolates for display using the render-time fraction. Fix: `BallController.Position` returns `rb.position`, and the AI, rules and player code use it.
Result (fixedDeltaTime 1/60, captureFramerate 60, seed 42): three runs started at very different absolute times were identical frame by frame for 400 frames; for 1200 frames (20 s of play) two runs were identical in ball, ball velocity, players and step counts. The extra warm-up run in the 1200 test differed from frame 0 at a level below the 9 decimals printed (not isolated; it did not happen in the 400-frame test). So PhysX itself was reproducible on one machine; the earlier "PhysX is not deterministic" reading was wrong for same-machine runs. It says nothing about two different devices, which is still untested and still a risk.

What this does and does not show: it shows that two runs of the same build on the same machine diverge. It does not isolate which part of PhysX or of frame timing causes it, and it does not test two different devices (which would be worse: PhysX is not deterministic across platforms).

## Consequences for the design
- **Lockstep or rollback is no longer ruled out on one machine**, but it needs bit-identical results on two different phones, which is untested and unlikely with PhysX across CPUs. Server-authoritative stays the plan.
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
2. Fixed-step simulation loop: player movement, AI and match rules still run in `Update` with a variable `Time.deltaTime`. The probe shows aligning to a fixed step helps a lot (see follow-up above).
3. Replace the Rigidbody ball with a custom fixed-step integrator (drag, Magnus spin, bounce, ground friction), then re-run the probe; success = identical runs.
4. Command-based input per tick: **recording and replay done** (`InputFrame`, `RecordingInput`, `ReplayInput`; see below). Still missing: a tick number instead of a frame number, and serialisation to bytes.
5. Separate simulation state from presentation (the HUD already listens through events; `PlayerVisual` and audio must stay out of the simulation).
6. Only then: the two-client spike, a snapshot format, interpolation, and a measured bandwidth figure.

## Input replay (step 4, first half)
`DeterminismProbeTests.ReplayingRecordedInputsReproducesTheMatch` (gated by `run_balance.flag`) plays 900 frames of AI vs AI at the 1/60 step while `RecordingInput` writes down every read each player's controller makes (move, sprint, pass, shot, tackle, aim, power), then plays the same seed again with the AI switched off and `ReplayInput` feeding the recorded frames. Result (`determinism_replay.txt`): **identical in every frame** (ball, ball velocity, all 22 players, physics step count). The recording held 9 one-shot actions (pass, shot, tackle), so it is a small sample of the action code paths.
What this establishes: everything the players do reaches the simulation through `IPlayerInput`; the AI has no hidden side channel into movement or kicks. To make it hold, the AI's own random choices (aim error, clearances) moved to a separate stream, `SimRandom.Ai`, so switching the AI off does not shift the numbers the simulation draws (2 EditMode tests).
What it does not establish: a human player (touch input, camera-relative movement, player switching, which are not in AI-vs-AI), two machines, or a server and client agreeing over a real connection.

## Wire format and bandwidth estimate (step 4/6 groundwork)
`Scripts/Net/NetCodec.cs`: `WorldSnapshot` (tick, ball position and velocity, 22 players, score, clock) and an `InputFrame` encoder. Quantised: positions and ball velocity to 1 cm, yaw to 1.4 degrees, player speed to 0.1 m/s. Sizes are constants checked by EditMode tests:
| Message | Bytes | At 20 Hz (+28 B IPv4/UDP header per packet) | At 30 Hz (same) |
|---|---|---|---|
| Snapshot (server to client) | 174 | 4.0 KB/s | 6.1 KB/s |
| Input frame (client to server) | 5 (+ tick and player id, not written yet) | 0.7 KB/s | 1.0 KB/s |
Against the plan.md target of under 20 KB/s per client, the snapshot leaves a large margin before any delta compression. This is arithmetic on an encoder that round-trips in tests; it has not been sent over a real connection, so real overhead (retransmits, relay, mobile networks) is not included. `SnapshotCapture` reads the live match into a snapshot (PlayMode test: positions survive encode and decode within 1 cm).
Not done: header and versioning, delta compression, a tick-numbered input stream, client interpolation, any socket.

## Client interpolation, loopback measurement
`SnapshotInterpolator` (Scripts/Net) keeps the last 32 snapshots by tick, returns the world at any fractional tick (positions and velocities blended, yaw by the short way round, score and flags switching at the later snapshot), ignores late and duplicate snapshots, and holds the newest one instead of extrapolating. 7 EditMode tests.
Loopback probe (`InterpolatedClientViewStaysCloseToTheServer`, gated by `run_balance.flag`, output `interpolation_error.txt`): an AI-vs-AI match for 900 frames at 60 fps; every 3rd frame (20 Hz) a snapshot goes through encode and decode into the interpolator; the client view is sampled 6 frames (100 ms) in the past and compared with what the server actually had at that moment.
| | mean | max |
|---|---|---|
| Player position error | 0.003 m | 0.11 m |
| Ball position error | 0.006 m | 0.22 m |
Payload was 3480 B/s (300 snapshots), matching the 174-byte estimate before headers.
Caveats: no packet loss, jitter or reordering was simulated, and the 100 ms delay is added on top of any real network latency, so it counts against the plan's 100 ms input-to-action target; the match was only 15 s of play with few tackles and shots, so the maxima above are the worst case seen in that one run, not a guarantee. Nothing here predicts the local player's own movement, which the client will need so that its own input does not feel 100 ms late.

## Real sockets (loopback only)
`UdpEndpoint` (Scripts/Net) wraps a non-blocking .NET UDP socket (no package needed), bound to 127.0.0.1. 4 EditMode tests send a real snapshot and an input frame between two sockets in one process and decode them intact, and show a server replying to whoever sent to it. This proves the encoder output survives a real datagram, nothing more: loopback never loses, delays or reorders packets, nothing was tried on a phone or across a network, and Android socket permissions and mobile NAT are untested. Transport decision (own UDP vs Unity Transport/NGO vs relay) is still open; this path costs no dependency but leaves connection handling, reliability for events (goals, cards) and NAT traversal to be written.

## Open questions
- Is 20-30 Hz enough for tackles and shots to feel right on a phone connection? Unknown until measured.
- Hosting: dedicated server builds, relay, or player-hosted? Cost and ops not assessed.
- Anti-cheat and accounts: out of scope until phase 10.
