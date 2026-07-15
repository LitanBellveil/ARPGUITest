# Carousel sample

A horizontal row of runtime-spawned items that wraps around (Right off the last item reaches the
first, and vice versa) — layered entirely on top of `Move()` by adding one extra connection pair,
no framework changes.

## Build steps

1. **Create the graph asset**: `Create > NavigationFramework > Navigation Graph`, name it
   `CarouselGraph`. Leave it empty, same reasoning as Inventory — all nodes are spawned at runtime.
2. **Make an item prefab**: a `Button`/`Image` with `NavigationSelectable` + `NavigationFocusVisual`,
   saved as a Prefab (e.g. `CarouselItem.prefab`).
3. **Build the scene hierarchy.**
   - `Canvas`
     - `CarouselRow` — a `RectTransform` with a `HorizontalLayoutGroup` (spawned items will lay out
       left-to-right automatically).
   - `NavigationBootstrap` with `KeyboardInputSource` + `NavigationInputRouter` (`Graph` =
     `CarouselGraph`).
4. **Add `CarouselController`** (e.g. on `CarouselRow`). In the Inspector:
   - `Router` -> the `NavigationInputRouter`.
   - `Item Parent` -> `CarouselRow`'s `RectTransform`.
   - `Item Prefab` -> `CarouselItem.prefab`'s `NavigationSelectable`.
   - `Item Count` -> however many items (default 6).
5. **Play test**: Left/Right arrows move along the row; from the last item, Right wraps to the
   first, and from the first, Left wraps to the last. Up/Down do nothing here since this sample is
   a single row — wire them up the same way if you extend it to a grid.
