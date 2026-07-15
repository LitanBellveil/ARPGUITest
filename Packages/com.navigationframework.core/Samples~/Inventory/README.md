# Inventory sample

A grid of slots that don't exist until Play Mode — demonstrates `RegisterDynamicNode` /
`UnregisterDynamicNode` instead of authoring nodes ahead of time in the Graph Window.

## Build steps

1. **Create the graph asset**: `Create > NavigationFramework > Navigation Graph`, name it
   `InventoryGraph`. Leave it empty — no nodes need to be authored, since `InventoryController`
   spawns and registers all of them at runtime.
2. **Make a slot prefab**: a `Button` with `NavigationSelectable` + `NavigationFocusVisual` on it
   (same as the other samples), saved as a Prefab (e.g. `InventorySlot.prefab`). Give it a visible
   size (e.g. 96x96) since the grid layout in step 4 positions instances by index.
3. **Build the scene hierarchy.**
   - `Canvas`
     - `InventoryPanel` (empty RectTransform — `InventoryController` parents spawned slots here)
   - Empty GameObject `NavigationBootstrap` with `KeyboardInputSource` + `NavigationInputRouter`
     (`Graph` = `InventoryGraph`, `Input Source` = the `KeyboardInputSource`), same as every other
     sample.
4. **Add `InventoryController`** (anywhere, e.g. on `InventoryPanel`). Fill in the Inspector:
   - `Router` -> the `NavigationInputRouter` from step 3.
   - `Slot Parent` -> `InventoryPanel`'s `RectTransform`.
   - `Slot Prefab` -> `InventorySlot.prefab`'s `NavigationSelectable`.
   - `Item Count` / `Columns` -> however many slots and how many per row (defaults: 12 / 4). Note
     this sample doesn't apply a `GridLayoutGroup` for you — add one to `InventoryPanel` (or let
     `InventoryController`'s spawn order line up with whatever layout group you add) so the visual
     grid matches the Left/Right/Up/Down adjacency `InventoryController` computes from `columns`.
5. **Play test**: arrow keys move through the grid, Enter on a slot discards it (destroys the
   GameObject and unregisters its node) — try Enter-ing a slot, then moving toward where it used to
   be; the framework silently skips the now-missing target rather than erroring.
