# Setting sample

A tabbed settings screen (Gameplay / Audio / Video), demonstrating `NavigationPage` +
`SwitchToPage` together with per-tab content Groups so hidden tabs are never reachable by arrow
keys.

## Build steps

1. **Create the graph asset**: `Create > NavigationFramework > Navigation Graph`, name it
   `SettingGraph`.
2. **Build the scene hierarchy.**
   - `Canvas`
     - `SettingPanel`
       - `TabsBar` — 3 `Button`s ("Gameplay", "Audio", "Video"), each with `NavigationSelectable` +
         `NavigationFocusVisual`, plus a small child (e.g. an underline `Image`, disabled by
         default) as its active-tab indicator.
       - `ContentGameplay` — a few controls (sliders/toggles as `NavigationSelectable`s), **active**
         by default.
       - `ContentAudio` — a few controls, **inactive** by default.
       - `ContentVideo` — a few controls, **inactive** by default.
   - `NavigationBootstrap` with `KeyboardInputSource` + `NavigationInputRouter` (`Graph` =
     `SettingGraph`). Leave every node's "Is Default" unchecked in the Graph Window —
     `PageTabController` establishes the real initial selection in `Start()`, and if a default node
     also exists, the router's own `SelectDefault()` would flash focus onto it one frame earlier.
3. **Wire the graph.**
   - `CharacterGraph`-style Generate From Scene + Auto Connect works here too, but note: Auto
     Connect's boundary is same-Group-**and**-Page, so it will only wire tabs to each other and each
     content panel's own controls to each other — never across panels.
   - In `SettingGraph`'s Inspector, use "Pages" to add 3 pages named exactly `Gameplay`, `Audio`,
     `Video` (set each page's Default Node to that panel's first control).
   - Use "Groups" to add 3 groups named exactly `Gameplay`, `Audio`, `Video`; assign each content
     panel's nodes to the matching group in the Graph Window's node inspector panel. Leave the tab
     buttons out of all three groups (or put them in their own always-enabled group).
   - Assign each content node's Page field to match its group's name the same way.
   - Hand-draw one connection in the Graph Window from each tab button Down to that tab's content
     Default Node, and from that content panel's top row back Up to the tab button — Auto Connect
     cannot generate these (different Page), so they stay hand-drawn permanently.
4. **Add `PageTabController`** to `SettingPanel`. In the Inspector:
   - `Router` -> the `NavigationInputRouter`.
   - `Tab Selectables` -> the 3 tab buttons' `NavigationSelectable`s, in order.
   - `Page Names` / `Group Names` -> `["Gameplay", "Audio", "Video"]` each, same order. These are
     matched by the page/group's **display name** you typed in step 3, not a raw GUID — the Graph
     Window doesn't currently expose a page/group's Id for copying.
   - `Content Panels` -> `ContentGameplay` / `ContentAudio` / `ContentVideo`, same order.
   - `Tab Active Indicators` -> each tab's underline child, same order.
5. **Play test**: Enter on a tab switches panels (old panel's controls become unreachable, new
   panel's default control gets focus); arrow keys navigate within the active panel and back up to
   the tabs bar via the hand-drawn connections.
