# Inventory sample

A list/grid of slots that don't exist until Play Mode, and can keep growing afterward (e.g. new
items as the player collects them) — demonstrates `RegisterDynamicNode`/`UnregisterDynamicNode`
plus `NavigationGeometryConnector` (geometry-based connections driven by whatever `LayoutGroup`
arranges the slots, the same algorithm the Editor's Auto Connect uses) instead of authoring nodes
ahead of time in the Graph Window or hardcoding row/column math.

## Build steps

1. **Create the graph asset**: `Create > NavigationFramework > Navigation Graph`, name it
   `InventoryGraph`. Leave it empty — no nodes need to be authored, since `InventoryController`
   spawns and registers all of them at runtime.
2. **Make a slot prefab**: a `Button` with `NavigationSelectable` + `NavigationFocusVisual` on it
   (same as the other samples), saved as a Prefab (e.g. `InventorySlot.prefab`). Give it a visible
   size (e.g. 96x96).
3. **Build the scene hierarchy.**
   - `Canvas`
     - `InventoryPanel` — a `RectTransform` with a `GridLayoutGroup` (or `VerticalLayoutGroup`/
       `HorizontalLayoutGroup` — any of them works, since connections are computed from wherever it
       actually places each slot, not from a hardcoded column count). `InventoryController` parents
       spawned slots here.
   - Empty GameObject `NavigationBootstrap` with `KeyboardInputSource` + `NavigationInputRouter`
     (`Graph` = `InventoryGraph`, `Input Source` = the `KeyboardInputSource`), same as every other
     sample.
4. **Add `InventoryController`** (anywhere, e.g. on `InventoryPanel`). Fill in the Inspector:
   - `Router` -> the `NavigationInputRouter` from step 3.
   - `Slot Parent` -> `InventoryPanel`'s `RectTransform`.
   - `Slot Prefab` -> `InventorySlot.prefab`'s `NavigationSelectable`.
   - `Initial Item Count` -> however many slots to spawn at Start (default 12).
5. **Play test**: arrow keys move through the list/grid (matching whatever your `LayoutGroup`
   actually laid out), Enter on a slot discards it (destroys the GameObject and unregisters its
   node) — try Enter-ing a slot, then moving toward where it used to be; the framework silently
   skips the now-missing target rather than erroring.
6. **(Optional) Test growth**: call `inventoryController.AddItem("New Item")` from anywhere (e.g. a
   test button's `OnClick`, or the Inspector's Debug view at runtime) — a new slot appears and the
   whole list's connections are recomputed around it.

## If this list sits below something else on screen (e.g. a filter/tab bar)

`NavigationGeometryConnector.Connect` only connects the nodes you pass it to each other — it won't
wire a static "entry point" node above the list into the list's first slot, because that entry node
lives in the authored graph and isn't part of `spawnedNodes`. Connect it once yourself after the
first spawn, e.g. in `InventoryController` (or a script it calls):

```csharp
NavigationNode entryNode = router.Manager.Graph.FindNode(entryNodeId); // whatever's above the list
entryNode.AddConnection(new NavigationConnection(spawnedNodes[0].Id, Direction.Down));
spawnedNodes[0].AddConnection(new NavigationConnection(entryNode.Id, Direction.Up));
```
