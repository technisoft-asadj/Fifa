# Asset licences

Rule (locked, animation_spec.md L3/L5): nothing paid; no EA/FIFA/commercial-game animations or extracted assets; check the licence, commercial-use permission, redistribution limits and Humanoid compatibility BEFORE importing; record the source and the terms here with the date. "Checked" below means the primary licence text was read; nothing is checked yet.

## Status of assets in the project today
| Asset | Source | Licence | Status |
|---|---|---|---|
| All code, procedural player bodies, procedural sounds, UI | Written in this project | Project's own | In use |
| TextMesh Pro essential resources (Assets/TextMesh Pro: font LiberationSans SDF, shaders) | Bundled with Unity's `com.unity.ugui` package; imported 2026-11-17 | Unity package licence; Liberation fonts are SIL OFL 1.1 (per the package) | In the working folder, not committed; **primary licence text not yet read** |
| No character models, no animation clips | - | - | Nothing imported yet |

## Candidate sources (named by the owner). NOTHING DOWNLOADED OR IMPORTED
Notes gathered on 2026-11-17 from web search results (secondary sources: forum posts and press articles). They must be re-checked against the primary licence text when the files are in hand.

### Adobe Mixamo (https://www.mixamo.com/)
- Reported: Adobe's FAQ lists video games among the allowed royalty-free uses of Mixamo characters and animations, personal and commercial, embedded in a game. Redistribution of the raw character/animation files (asset packs, templates, standalone downloads, public repositories) is reported as NOT permitted. A Unity Asset Store submission was reported rejected over Mixamo content.
- Needs an Adobe account (free); the owner must log in and download.
- Consequences for this project: keep raw FBX out of any public repository; ask the owner whether the repository is private; originality: many games use the same free Mixamo characters.
- Sources: [Adobe community FAQ post](https://community.adobe.com/t5/mixamo-discussions/mixamo-faq-licensing-royalties-ownership-eula-and-tos/td-p/13234775), [itch.io discussion](https://itch.io/post/7747839). Official FAQ to read: helpx.adobe.com/creative-cloud/faq/mixamo-faq.html (not retrieved).
- Humanoid compatibility: Mixamo rigs are commonly used as Unity Humanoid (unverified until imported). Quality and football coverage: unverified.

### Free Soccer Motion Capture Pack, Anderson Rohr (https://andersonrohr.gumroad.com/l/mocap_pack5)
- Reported by press: his free packs are licensed for commercial projects without attribution; a soccer pack with dribbles, juggling and more exists. The soccer pack's own licence text was not seen.
- Sources: [CG Channel](https://www.cgchannel.com/2026/09/download-130-free-fbx-mocap-moves-from-anderson-rohr/), [80.lv](https://80.lv/articles/get-this-free-soccer-mocap-animation-pack-with-dribbles-juggling-more/).
- Humanoid compatibility, retarget quality and which actions are covered: **unverified** (files not seen). Redistribution terms: unverified.

### Human Basic Motions FREE, Kevin Iglesias (Unity Asset Store listing 154271; also Gumroad/itch.io free versions)
- Reported: the free version links to a licence at keviniglesias.com; the paid pack uses the standard Unity Asset Store EULA; the free pack lists about 118 animation files (idles, turns, 8-direction walks, talking). Licence text not seen; whether the free Unity listing has the same terms is unverified.
- Sources: [itch.io free page](https://kevdev.itch.io/basic-motions-free), [Gumroad free page](https://keviniglesias.gumroad.com/l/human-basic-motions-free).
- Not football-specific (no kicks, tackles or keeper actions expected); likely useful only for idles and turns. Unverified.

### Blender (https://www.blender.org/)
- GPL-licensed tool; models and animations you make with it are yours. Needed only for rigging/cleanup. Not installed or used yet.

## Missing / not available (cannot be claimed)
Everything in animation_spec.md section 2 is currently missing as real animation: no mocap clips exist in the project. Which of them the three sources cover will be recorded here after the owner downloads them and I inspect them on the model.

## Log
| Date | Event |
|---|---|
| 2026-11-17 | File created. Candidate sources researched via web search only. No downloads. TMP essentials imported from the built-in Unity package. |

- Decision 2026-11-17: the owner chose a free Mixamo character as the base. Raw Mixamo files are kept out of git (`/Assets/Art/Mixamo/` in `.gitignore`) until the owner confirms the repository is private. Download steps: `mixamo_download_guide.md`. The licence text is still to be read at the primary source (helpx.adobe.com Mixamo FAQ) once the files are imported.
