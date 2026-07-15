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

## If this list shares a page with other regular buttons (e.g. a filter/tab bar, a footer)

`NavigationGeometryConnector.Connect` only connects the nodes you pass it to each other — it can't
wire the page's other buttons to the list's first/last slot, because those buttons live in the
authored graph and the list's slots don't exist until Play. Rather than hand-writing connection code
for every neighbor, use `NavigationScrollViewAnchor` as a stand-in:

1. Put an empty `RectTransform` where the list goes (same size/position the list will occupy), add
   `NavigationScrollViewAnchor` to it (instead of `NavigationSelectable` — it's a subclass, so
   Generate From Scene picks it up the same way).
2. Generate From Scene + Auto Connect the **whole page** in one pass, same as any other sample — the
   anchor gets wired to its neighbors (tab bar above, footer below, whatever) exactly like a normal
   button would.
3. In `InventoryController`'s Inspector, assign that anchor to **Scroll View Anchor**.

At `Start()`, once the real slots are spawned, `InventoryController` calls
`NavigationDynamicListConnector.AttachDynamicList` once: every connection the anchor had gets
transplanted onto the list's first slot (Up/Left-facing ones) or last slot (Down/Right-facing
ones), every neighbor's connection pointing at the anchor gets redirected to point at the list
instead, and the anchor itself is disabled — it's never reachable during actual navigation, it only
existed so the Graph Window tooling had something to connect to.

This assumes a single row or column list; a grid's side edges won't get outside connections from
this beyond the very first/last cell. **Only call `AttachDynamicList` once** — a second call reapplies
the anchor's original connections on top of whatever's already there, duplicating them on
`firstNode` (which usually doesn't change across growth). If items appended via `AddItem` extend
past the original `lastNode` and something beyond the list needs to keep reaching the current last
item, redirect just that one connection by hand instead of calling `AttachDynamicList` again — find
the neighbor's connection that targets the old last node and repoint it at the new one.
