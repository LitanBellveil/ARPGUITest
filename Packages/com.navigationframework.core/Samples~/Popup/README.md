# Popup sample

A popup panel that's inactive by default and takes over navigation focus while open, restoring
focus to whatever was selected before it opened.

## Build steps

1. **Create the graph asset**: `Create > NavigationFramework > Navigation Graph`, name it
   `PopupGraph`.
2. **Build the scene hierarchy.**
   - `Canvas`
     - `BaseScreen` — a couple of `Button`s (`NavigationSelectable` + `NavigationFocusVisual`),
       including one named e.g. `OpenPopupButton`.
     - `PopupPanel` — **inactive by default** (uncheck the GameObject's active checkbox). Inside:
       a couple of controls plus a `CloseButton`, all `NavigationSelectable` + `NavigationFocusVisual`.
   - `NavigationBootstrap` with `KeyboardInputSource` + `NavigationInputRouter` (`Graph` =
     `PopupGraph`). Mark one `BaseScreen` node "Is Default" in the Graph Window.
3. **Wire the graph.**
   - "Scan Root" -> the Canvas (a common parent of both `BaseScreen` and `PopupPanel`) -> "Generate
     From Scene". Inactive children are included, so `PopupPanel`'s controls get nodes too even
     though the panel itself is off.
   - In "Groups", add two groups: `Base` and `Popup`. In the Graph Window, assign every
     `BaseScreen` node to `Base` and every `PopupPanel` node to `Popup`.
   - Click "Auto Connect" — since Group is a hard boundary, this only wires `BaseScreen`'s nodes to
     each other and `PopupPanel`'s nodes to each other, never across the two (correct — they should
     never be reachable from one another via arrow keys, only via `PopupController`).
4. **Add `PopupController`** to `PopupPanel`'s parent (or anywhere). In the Inspector:
   - `Router` -> the `NavigationInputRouter`.
   - `Input Source` -> the same `KeyboardInputSource` on `NavigationBootstrap`.
   - `Open Button` -> `OpenPopupButton`'s `NavigationSelectable`.
   - `Close Button` -> `PopupPanel`'s `CloseButton`'s `NavigationSelectable`.
   - `Popup Panel` -> the `PopupPanel` GameObject.
   - `Popup Default Selectable` -> whichever popup control should get focus first when it opens.
   - `Base Group Name` / `Popup Group Name` -> `Base` / `Popup` (must match the group display names
     from step 3 exactly — same GUID-lookup-by-name caveat as the Setting sample).
5. **Play test**: Enter on `OpenPopupButton` opens the popup and focuses its first control; arrow
   keys can no longer reach `BaseScreen` while it's open. Enter on `CloseButton`, or pressing
   Escape anywhere inside the popup, closes it and returns focus to `OpenPopupButton`.
