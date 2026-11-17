# Testing record (animation, movement and controls programme)

Rules: a result is recorded only if the test was actually run, with the date and the evidence. "Not run" is a valid entry. Nothing here is estimated or assumed. Requirement IDs refer to animation_spec.md.

## Existing automated suites (the regression net that every phase must keep green)
| Suite | Last run | Result |
|---|---|---|
| EditMode | 2026-11-17 | 154 / 154 passed |
| PlayMode | 2026-11-17 | 76 / 76 passed; 5 balance/determinism probes show Inconclusive unless `run_balance.flag` exists (expected) |
| Android IL2CPP build (ARM64) | 2026-11-17 19:54 | built; installed and run on Infinix X6881; 0 Unity errors in logcat; Back key pauses (the host/join screen came after this build and is not on the phone) |

## Animation programme tests (none run: the programme has not started coding)
| ID | Check | Method | Status | Evidence |
|---|---|---|---|---|
| B-slide | No foot slide at walk/jog/run/sprint | PlayMode: compare measured foot speed to body speed at 3 speeds + video/frame check | not run | - |
| B-turn | 90 and 180 degree turns play, no pops | screenshot sequence + transition log | not run | - |
| C/D/E-contact | Ball contacts the kicking foot at the contact frame; left and right | frame-stepped capture; PlayMode test on contact tick | not run | - |
| F | Jump, header, landing without teleport | PlayMode + capture | not run | - |
| G | Tackle, slide, fall, get-up; foul by circumstance | PlayMode + capture | not run | - |
| H | Keeper dives and saves | PlayMode + capture | not run | - |
| J | Celebration variety, group and team | capture | not run | - |
| M | Context-sensitive controls | PlayMode + phone | not run | - |
| Customisation | Layouts persist; two aspect ratios | PlayMode + phone | not run | - |
| N | Online animation sync, no duplicate action under loss/reorder | network-conditioner PlayMode tests; two instances | not run | - |
| P | At least 5 distinct profiles; left vs right; run/sprint, turn, dribble, shot, celebration differences | captures side by side | not run | - |
| Perf | 22 animated players on the reference phone | Profiler / fps counter on Infinix X6881 | not run (no baseline either) | - |
| Platform | ARMv7+ARM64; iOS; Amazon | build attempts | not run | - |

## Baselines to record at the start of Phase 2
- Frame rate on the Infinix X6881 with the current 22 primitive players (never measured).
