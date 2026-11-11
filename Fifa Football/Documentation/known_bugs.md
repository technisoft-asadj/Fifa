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
| KB-011 | L | "The referenced script (Unknown) on this Behaviour is missing" warning appeared twice right after a recompile + scene rebuild. A scan of the saved scene found 0 missing scripts. | Not reproducible; watch |
