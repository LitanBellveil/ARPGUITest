# Character sample

The simplest possible use case: a static equip-slot menu. No custom script — this sample exists to
prove the framework needs zero extra code for a plain, non-dynamic screen. It's a template for the
other samples' Editor setup steps too.

## Build steps

1. **Create the graph asset.** In the Project window: `Create > NavigationFramework > Navigation
   Graph`, name it `CharacterGraph`.
2. **Build the scene hierarchy.**
   - `Canvas` (Screen Space - Overlay)
     - `CharacterPanel` (empty RectTransform, arrange children in a cross or grid — e.g. Head/Chest/
       Legs/Weapon/Accessory slots)
       - One `Button` per equip slot. On each: add `NavigationSelectable` (auto-fills its
         `RectTransform` via `Reset`) and `NavigationFocusVisual` (drag the Button's own `Image`
         into `Target Graphic`).
3. **Wire the graph.** Select `CharacterGraph` in the Inspector:
   - "Scan Root" -> drag `CharacterPanel` -> click "Generate From Scene" (creates one
     `NavigationNode` per slot).
   - Click "Auto Connect" (wires Up/Down/Left/Right between slots from their actual screen
     positions).
   - Open the Graph Window, select one node (e.g. Head), check "Is Default".
4. **Add the input rig.** Create an empty GameObject (e.g. `NavigationBootstrap`):
   - Add `KeyboardInputSource` (from `Common/`).
   - Add `NavigationInputRouter`. Assign `Graph` = `CharacterGraph`, `Input Source` = the
     `KeyboardInputSource` you just added (same GameObject — `Reset` will auto-fill this if the
     component is added after `KeyboardInputSource`).
5. **Play test.** Enter Play Mode, use arrow keys — focus should move between slots and
   `NavigationFocusVisual` should tint the focused slot.

If you want Submit to do something (e.g. open a tooltip), subscribe to that slot's
`NavigationSelectable.Submitted` event from your own small script — that's the one piece this
sample deliberately leaves out, since "what Submit does" is entirely use-case-specific.
