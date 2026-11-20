# Soccer Fight UI redesign: audit and plan

## Audit (what exists)
- UI is built in code (no prefabs): `UIKit` (widgets), `UITheme` (palette, procedural sprites and icons), `UIKit.Widgets` (Screen, Selector, SliderRow, Switch, Crest...), `UIFx` (button press motion, screen fade, spinner).
- `MatchHUD` (partial class across Menus, Teams, Online, FullTime, Splash, Radar, Perf, Extra) owns navigation. Canvas scales to 1920x1080; panels fit to the screen (`FitToCanvas`).

## Screens (all done)
Home (3D trophy + ball stage), Settings (5 tabs), Teams, Online, Match Setup, Full Time, Player Profile, Pause menu, Match Statistics, Tournament Bracket, Lineup and Formation, Matchmaking / Room Lobby (with Cancel), Loading screen (real async progress), shared Confirm dialog.
Not restyled: the Controls panel (still the older layout; it works) and the in-match HUD.

## Behaviour
- Back / Esc closes the innermost screen first (`ExtraBack`, then `NavigateBack`).
- Confirmation before: restart match, leave match, abandon cup. No purchases exist; rewards shown are the real ones paid by `PlayerProfile`.
- Every button dips when pressed; screens fade in over 0.2 s (real time, works while paused).

## Tests at the last run
EditMode 217/217. PlayMode 113/113 passed (the 5 balance probes are inconclusive by design without `run_balance.flag`).

## Open items
- Controls panel restyle; phone-ratio (19.5:9, 4:3) visual check of the new screens (they use `FitToCanvas`).
- Lineup screen shows the current formation and the club's players; it does not yet reassign players to slots.
- The new reference image from the earlier message was never attached; only the six-screen mock was available.
- Real club and player names in `TeamCatalog` need licences before a store release (see `ASSETS.md`).
