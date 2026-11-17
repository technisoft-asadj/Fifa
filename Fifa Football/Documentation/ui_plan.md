# UI redesign plan (from the reference sheet, written 2026-11-17)

Goal: the look of the supplied reference sheet (dark navy and electric-blue glass panels, gold/yellow primary actions, purple, green and blue secondary buttons, a top bar with player level, coins and gems) on top of the working game. Nothing here may need a paid asset, plugin, API or service.

## What exists today
- All UI is built in code in `Scripts/UI/MatchHUD.cs` (+ `MatchHUD.Online.cs`): scoreboard, start screen, full-time, pause, Online screen. Legacy `UnityEngine.UI.Text` with the built-in font, flat dark panels, one flat button colour. Touch controls are in `Scripts/Input/TouchControls.cs`.
- One scene (`FootballPrototype`); menus are panels inside it and the match is paused (`timeScale 0`) behind them.
- Canvas Scaler already 1920x1080 reference, match 0.5. Orientation is landscape only (the reference sheet's menu screens are landscape too, so this stays).
- Packages: `com.unity.ugui 2.0.0` (contains TextMeshPro in Unity 6). TMP Essential Resources (default font) are not imported yet: that is a free, bundled import.

## What I can and cannot do without paid or external things
| Reference item | Plan |
|---|---|
| Glass panels, gradients, glows, rounded buttons, top bar, cards, tabs, toggles, sliders | Build procedurally (generated sprites and code); no art files needed. |
| Logo "FOOTBALL LEGENDS", crown, ball icon, coin and gem icons | Drawn procedurally / text with outline and gradient. The name in the sheet is a placeholder: title is your call. Avoid copying real clubs or the sheet's wording if it matches a real product. |
| Stadium photo backgrounds, player portraits, team crests | I cannot make photo-real art. Use the live 3D pitch as the background (blurred/dimmed) and procedural crests and kit icons. Real art needs your files or an asset you approve. |
| Google / Amazon / Facebook sign-in | Needs each provider's SDK, an app registration and (for stores) a backend. Not done. "Play as Guest" is built; the buttons are left out rather than faked. |
| Online "worldwide", leaderboards (Global / Friends), daily reward, coins and gems economy | Worldwide play and a global leaderboard need a server (a cost). Built instead: same-Wi-Fi online (done), a local leaderboard of your own results, a local daily reward and coins earned from matches. All saved on the device. |
| Ads | No ad SDK (that is a third-party service). A single optional, off-by-default banner slot is reserved in the layout so one can be added later. |
| Android ARMv7 + ARM64 | Needs the ARMv7 target ticked (project builds ARM64 only today). I will verify an ARMv7+ARM64 build. |
| iOS 64-bit | Cannot be built on this Windows PC (needs a Mac and Xcode). Code stays platform neutral. |
| Amazon Appstore | The same APK is accepted; store listing and account are yours. |

## Technical approach
1. Foundation: `UITheme` (palette, procedural sprites, fonts), reusable builders (`GlassPanel`, `MenuButton` in gold/purple/green/blue/red, `TopBar`, `Card`, `TabBar`, `Toggle`, `Slider`), a `ScreenRouter` (push/pop, Back key, one screen visible at a time, no per-frame Update).
2. Switch to TextMeshPro for new screens once TMP Essentials are imported; existing HUD text moves over screen by screen.
3. Screens built as code-made, reusable prefabs-by-code (this project builds UI in code and tests it that way) rather than hand-edited scenes, so they can be unit tested in PlayMode.
4. Order, each opened in Unity and checked in a screenshot before I call it done:
   1. Foundation + Main Menu (Play, Tournaments, Online, Teams, Settings) wired to the existing start/league/cup/season/online code.
   2. Mode Selection (Offline / Online), Team Selection (kits and fictional crests), Settings with Audio, Graphics and Camera Selection.
   3. Gameplay HUD restyle (score pill, minimap, joystick and buttons), Broadcast camera HUD and camera switch, Pause Menu, Match Result.
   4. Controls customisation (Joystick / Buttons / Hybrid, size, position), Leaderboard (local), Splash and Guest screens.
   5. Online 1v1 HUD (ping from the network layer); 11v11 online stays blocked on the networking work (client prediction, more than one guest).
5. Keep every existing test green; add PlayMode tests for navigation (each button leads where it says, Back returns).

## Not decided by me (need your answer eventually)
- The game's name and logo.
- Whether you will provide art (backgrounds, crests, portraits) or accept procedural placeholders.
- Whether ARMv7 support is worth the larger build.
