# Football Simulator

Original 3D mobile football simulation built with Unity 6 (6000.3.5f1) and URP.
Vision and full requirements: `My Plan/FOOTBALL SIMULATOR.docx`. Working plan: `Documentation/plan.md`.

## Open and run
1. Open the `Fifa Football` folder in Unity 6000.3.5f1.
2. Open `Assets/Scenes/FootballPrototype.unity` and press Play.
   (Rebuild it any time from the menu **FootballSim > Build Prototype Scene**.)

## Controls (keyboard / gamepad)
| Action | Keyboard | Gamepad |
|---|---|---|
| Move | WASD / arrows | Left stick |
| Sprint | Shift | Right trigger |
| Pass | K | South button |
| Shoot (hold to charge, release) | Space | East button |
| Tackle | J | West button |
| Switch player | Tab | Left shoulder |

## Tests
Window > General > Test Runner (EditMode and PlayMode), or the MCP `run_tests` tool.
Current: 8 EditMode + 9 PlayMode, all passing.

## Documentation
See `Documentation/`: `plan.md`, `architecture.md`, `progress.md`, `known_bugs.md`.

## Status
Pre-production prototype: playable 11v11 vs AI with placeholder capsule players.
Not yet implemented: animations, UI, offside/fouls, offline modes, multiplayer, stadium art.
