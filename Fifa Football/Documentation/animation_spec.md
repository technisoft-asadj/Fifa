# Animation, movement and controls: LOCKED SPECIFICATION (my working checklist)

Source: two documents from the owner, received 2026-11-17: "MASTER DEVELOPMENT TASK: realistic football player movement, animation and mobile controls" and "OFFICIAL MASTER SPECIFICATION AMENDMENT: elite realism and individual player animation identity" (STATUS: FINALIZED AND LOCKED). This file restates them as numbered, testable requirements. IDs are what progress.md and testing.md refer to. A requirement is DONE only when it has been seen working on the real player model in Unity (and, where it says so, on a phone). Importing an FBX is never "done".

Status values: `todo` (nothing), `part` (something exists, not meeting the requirement), `done` (verified, with the test that shows it), `blocked` (needs an asset, decision or device from the owner).

## 0. Locked product constraints (never change)
| ID | Requirement |
|---|---|
| L1 | Unity 6; offline and online (1v1 and 11v11); broadcast and gameplay cameras; customizable mobile controls; original fictional teams and players |
| L2 | Android ARMv7 and ARM64, iOS 64-bit, Amazon Appstore |
| L3 | Free assets and tools wherever possible; nothing paid; no mandatory paid API, plugin or subscription |
| L4 | No artificial deadline: quality first |
| L5 | No EA / FIFA / commercial-game animations or extracted assets; licences checked and recorded in `asset_licenses.md` before any import |
| L6 | Server stays authoritative for scoring, ball ownership and results; clients never decide them |
| L7 | Do not mark anything implemented or tested before real Unity verification; no fabricated results; missing animations are listed as missing |

## 1. Quality bar (amendment section 1) - "ELITE"
Natural body mechanics; realistic run and sprint; smooth acceleration and deceleration; accurate foot placement; natural body rotation; weight transfer; believable jump and landing; convincing ball contact; natural tackle and fall; realistic goalkeeper movement; smooth transitions; authentic celebrations. No robotic, repetitive or arcade-like movement.

## 2. Animation categories (master task section 2)
Status is `todo` for every row today (the current players are procedural primitives with a sine-wave leg swing, a kick and a tackle pose; see the audit).

| ID | Category | Must include |
|---|---|---|
| A | Idle | natural stand, breathing, look around, weight shift, waiting for a pass, defensive ready, relaxed and tired idle |
| B | Locomotion | walk, jog, run, sprint, backward run, left/right strafe, diagonals, 90 and 180 turns, acceleration, deceleration, quick stop, direction change, turning with the ball. Blend Trees 1D and 2D. **No foot sliding.** |
| C | Ball control | first touch, close dribble, fast dribble, left and right foot dribble, trap, shield, receive, control after a long pass, turn with ball, skill moves (stepover, feint, drag back, flick), juggling. Ball stays believably close to the feet |
| D | Passing | short, long, through, lofted, cross, one-touch, weak-foot. Kicking foot, body rotation, contact and ball velocity coordinated |
| E | Shooting | normal, power, finesse, curved, volley, half-volley, header, chip, penalty, free kick. Power and accuracy depend on attributes, body position, foot, stamina, timing |
| F | Jumping | standing, running, header, high-ball jump, anticipation, airborne, landing, landing recovery. No teleport or snap to the ball |
| G | Defending | standing tackle, slide, interception, shot block, jockey, shoulder challenge, shield, fall after collision, get-up. Fouls depend on collision circumstances, direction, timing |
| H | Goalkeeper | ready stance, side/high/low dive, catch, parry, punch, jump, recovery, one-handed save, throw, goal kick, distribution |
| I | Reactions | goal scored, missed shot, successful and failed tackle, injury, frustration, disappointment, excitement, teammate and referee interaction |
| J | Celebrations | knee slide, fist pump, arms raised, running, point to the sky, team, high-five, group, trophy. Several variations, not one |

## 3. Architecture (section 4)
| ID | Requirement |
|---|---|
| AR1 | Unity Humanoid rig, Animator Controller, parameters, 1D/2D Blend Trees, layers, Avatar Masks, animation events, IK / Animation Rigging where it helps, state machines, foot IK, ball-interaction IK |
| AR2 | Reusable components with clear responsibilities (suggested: PlayerController, PlayerMovement, PlayerAnimationController, PlayerBallController, PlayerShooting, PlayerPassing, PlayerDefending, PlayerSkillMoves, PlayerAttributes, PlayerStamina, PlayerIKController, PlayerNetworkState). Suggestions, not a licence to create needless scripts; reuse what exists |
| AR3 | One system controls player movement. No second system fighting `PlayerController` |
| AR4 | Shared animation assets, not a private copy of every clip per player |

## 4. Physics realism (section 5)
Natural accel and decel; realistic turning; ground detection; foot-ground contact; no visible foot slide; realistic jump and landing; ball collision, bounce and roll; player-player collision; controlled physical reactions; smooth transitions. **The ball is never moved to the foot each frame**: physics plus controlled attach/interaction states. No snapping, teleporting or unnatural body rotation.

## 5. Mobile controls (section 6)
| ID | Control |
|---|---|
| M1 | Left virtual joystick: 360 degrees, walk / jog / run, direction changes |
| M2 | SPRINT: faster, uses stamina, hold or toggle preference |
| M3 | Attacking: PASS (tap short, hold power), THROUGH (ground; hold = lofted), SHOOT (hold to charge), FINESSE (modifier for curve), LOB/CROSS, SKILL (stepover, feint, drag back, unlocked moves), SWITCH |
| M4 | Defending: TACKLE, SLIDE, CONTAIN, PRESS (send an AI teammate), JOCKEY, SWITCH (pick a suitable defender), GK RUSH |
| M5 | Goalkeeper: DIVE LEFT, DIVE RIGHT, JUMP, CATCH, PUNCH, THROW, KICK, RUSH |
| M6 | **Context-sensitive**: buttons change meaning/set when possession changes. Never show everything at once |
| M7 | Also: pause, camera switch, radar, replay, settings |

## 6. Customisation screen (section 7)
Drag to reposition; button size; opacity; joystick size; left-handed and right-handed layouts; Classic and Advanced layouts; Custom layouts; tap/hold sensitivity; vibration on/off; restore defaults; saved persistently; adaptive to resolution and aspect ratio.

## 7. Online compatibility (section 8, amendment 7)
| ID | Requirement |
|---|---|
| N1 | Do not sync bones. Sync validated gameplay state: movement state, direction, velocity, current action, animation id, action timing, ball-interaction state |
| N2 | Server authoritative for anything gameplay-critical |
| N3 | Animations stay smooth under interpolation; no duplicate actions; no desync |
| N4 | Designed for 22 players even if built first for 1v1 |
| N5 | Animation profiles are deterministic and identical on every client; only profile ids and state are networked |

## 8. Performance (section 9, amendment 7)
Minimise Animator cost; animation culling; fewer physics calls; light models; limit IK; LOD; profile many characters at once; ARMv7 + ARM64; check iOS and Amazon compatibility; **measure on representative devices**; do not trade gameplay quality for an arbitrary fps.

## 9. Individual animation identity (amendment sections 2-6)
| ID | Requirement |
|---|---|
| P1 | Every player has an animation profile: running style, sprinting style, acceleration character, turning style, dribbling style, preferred foot, shooting technique, passing technique, jumping technique, celebration preferences, recovery behaviour, defensive movement |
| P2 | Archetypes (not rigid): striker (explosive, agile, quick finishing), winger (fast sprint, quick direction change, close control, skill moves), midfielder (balanced, smooth turns, natural passing, controlled reception), defender (strong stance, controlled jockey, physical challenge, recovery), goalkeeper (own stance, individual dive, catch and parry techniques, recovery) |
| P3 | Preferred foot: right, left, both. It affects passing, shooting, first touch, dribbling, ball position, body orientation. Weak-foot quality from attributes. **Do not simply mirror every animation** without checking mechanics and foot contact |
| P4 | Individual celebrations: preferred styles, signature, teammate and group interaction; not the same one every time |
| P5 | Profiles work offline, online 1v1 and online 11v11, deterministic across clients |
| P6 | Techniques: mocap + Humanoid retargeting + Blend Trees + layers + masks + IK + procedural adjustment + state machines + controlled variation; shared assets; variations must keep natural mechanics |

## 10. Implementation order (amendment 10)
1. One exceptionally polished player prototype. 2. Then different animation profiles. 3. Then the full 22-player squad. Never lower quality to rush more players.

## 11. Acceptance criteria (master 12 + amendment 8)
Master: (1) running and turning natural; (2) accel and stopping realistic; (3) ball control with both feet; (4) pass and shot foot contact proper; (5) jump and landing convincing; (6) tackle and fall believable; (7) keeper saves work; (8) celebrations play; (9) controls responsive and customisable; (10) online animation sync correct; (11) many players animate without unacceptable cost; (12) platform builds verified.

Amendment: (1) at least **five** visually different profiles tested; (2) left vs right foot compared; (3) run and sprint differences verified; (4) individual turn and dribble verified; (5) different shooting techniques; (6) different celebration preferences; (7) animation survives collisions; (8) ball contact and foot placement; (9) all 22 players at once; (10) actual mobile performance profiled; (11) animation consistency online.

## 12. Documents to keep current
`plan.md`, `architecture.md`, `progress.md`, `known_bugs.md`, `testing.md`, `asset_licenses.md`. After each phase report: changed files, Unity Editor steps, actual tests, remaining problems. Begin with Phase 1 only and show the audit and plan before major architectural changes (done in `animation_audit_and_plan.md`; waiting for the owner's go-ahead on the decisions in its section 9).
