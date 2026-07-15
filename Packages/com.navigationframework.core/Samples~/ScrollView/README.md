# ScrollView sample

A tall list inside a `ScrollRect`, longer than the viewport, demonstrating that the framework
doesn't scroll anything for you — moving focus to an off-screen node is valid on its own, so this
sample adds `ScrollFocusIntoView` to bring the focused node into the viewport whenever it changes.

## Build steps

1. **Create the graph asset**: `Create > NavigationFramework > Navigation Graph`, name it
   `ScrollViewGraph`.
2. **Build the scene hierarchy** (standard Unity ScrollRect setup):
   - `Canvas`
     - `ScrollView` (`ScrollRect` component, vertical only, with a `Viewport` child using a
       `RectMask2D`/`Mask`)
       - `Viewport`
         - `Content` (`VerticalLayoutGroup` + `ContentSizeFitter`, taller than the viewport) —
           15-20 `Button` rows, each `NavigationSelectable` + `NavigationFocusVisual`.
   - `NavigationBootstrap` with `KeyboardInputSource` + `NavigationInputRouter` (`Graph` =
     `ScrollViewGraph`).
3. **Wire the graph**: Scan Root -> `Content` -> Generate From Scene -> Auto Connect (rows are
   already vertically stacked, so Up/Down connections come out correctly) -> mark the first row "Is
   Default".
4. **Add `ScrollFocusIntoView`** anywhere (e.g. on `ScrollView`). In the Inspector: `Router` -> the
   `NavigationInputRouter`, `Scroll Rect` -> the `ScrollView`'s `ScrollRect`.
5. **Play test**: Down arrow past the last visible row scrolls the list to keep the focused row
   visible; same going back Up. Focus never gets stuck on an invisible row.
