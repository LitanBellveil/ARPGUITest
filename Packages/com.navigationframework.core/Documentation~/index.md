# Navigation Framework — Architecture

## Goal

A UI navigation framework for UGUI that is completely independent of any input source. The
framework never references `InputAction`, `PlayerInput`, or any Input System asset. It exposes
three verbs at runtime:

- `Move(Direction)`
- `Submit()`
- `Cancel()`

Calling code (New Input System, legacy Input, AI, replay tooling, whatever) decides when to call
these. This is what lets the same graph and manager serve a Character panel, a Weapon panel, an
Inventory, Settings, a Popup/Dialog, a Carousel, a ScrollView, or a Skill Tree without the
framework knowing anything about how any of them are actually controlled.

## Layered structure

- **Runtime** — pure C# + UGUI types. No `UnityEditor` reference, ever. Assembly: `NavigationFramework.Runtime`.
- **Editor** — GraphView/UI Toolkit tooling that authors `NavigationGraph` assets. Assembly: `NavigationFramework.Editor`, references Runtime only. Runtime never references Editor.
- **Samples~** — one focused sample scene per use case (Character, Weapon, Inventory, Setting, Popup, Dialog, Carousel, ScrollView, Skill Tree), imported on demand via Package Manager, never compiled as part of the package itself.

## Phase 1 — Runtime Data Structure

The data model is deliberately split into small, single-purpose types instead of one large graph
class, so each piece can be understood, tested, and extended independently (SOLID's single
responsibility + open/closed):

| Type | Role |
|---|---|
| `Direction` | The 4 cardinal directions a `Move` can request. Submit/Cancel are intentionally excluded — they are separate actions, not directions. |
| `NavigationConnection` | One directed edge: target node id, direction, priority, enabled. Multiple connections may share a direction on one node; `Priority` picks the winner — this is the seam for "multiple Down edges", weighting, and conditional navigation called out in the spec. |
| `NavigationNode` | One focusable position: id, display name, optional scene `RectTransform`/`NavigationSelectable`, owning group/page, default/enabled flags, outgoing connections, editor-only canvas position. Holds no runtime cache. |
| `NavigationGroup` | A named, independently enable/disable-able bundle of nodes. Also a hard boundary for Auto Connect (Phase 6). |
| `NavigationPage` | A logical screen/tab (e.g. "Character", "Weapon") with a default node and an entry policy (`SelectDefaultNode` vs. `RestoreLastSelected`), so tabbed UIs get page-aware focus handling from day one instead of it being retrofitted later. |
| `NavigationGraph` | `ScriptableObject` holding the lists of nodes/groups/pages for one navigable UI. Read-only at runtime; all mutators are for editor tooling. |
| `NavigationSelectable` | `MonoBehaviour` placed on any focusable widget (Button, Toggle, Slider, TMP_InputField, or custom). Exposes Select/Deselect/Submit/Cancel/Hover/Highlight as explicit methods + events that `NavigationManager` (Phase 2) drives directly — it does not rely on Unity's `EventSystem` selection state, so gamepad/keyboard/virtual-cursor input all funnel through one explicit API. |

### Design notes and deviations from the literal spec

- **Direction vs. Submit/Cancel.** The original class list mentioned `Direction` twice — once as
  its own type, and once with `Submit`/`Cancel` appended to its value list. Since
  `NavigationManager` already exposes `Submit()`/`Cancel()` as separate methods from `Move(Direction)`,
  folding them into the `Direction` enum as well would let a `NavigationConnection` "point in the
  Submit direction", which is meaningless. `Direction` was kept to the 4 cardinal values only;
  Submit/Cancel remain distinct actions. Flagging this now in case the intent was actually different.
- **Scene references live on the data, not just at runtime.** `NavigationNode.RectTransform` /
  `NavigationNode.Selectable` are direct Unity object references stored in the graph asset itself
  (not resolved via `FindObjectOfType` or a runtime scan). This works because Unity fully supports a
  `ScriptableObject` asset referencing a scene object, as long as the graph is authored against a
  saved scene (exactly what Generate From Scene does in Phase 5). It also means the "Dictionary Cache"
  requirement is satisfied for free for statically authored nodes: `NavigationManager.SetGraph()`
  (Phase 2) reads these already-resolved references directly into its cache — no scene search
  happens, ever, on `Move()`.
- **Dynamic content (ScrollView/Carousel/Skill Tree) needs a second path.** Nodes for content that
  doesn't exist until runtime (spawned inventory slots, a procedurally generated skill tree) can't
  have a baked scene reference. The reserved fix for this — explicit runtime registration through
  `NavigationSelectable` rather than any scene search — is described in the Phase 2 write-up so it
  doesn't need to be bolted on later.
- **No Unity `ISelectHandler`/`ISubmitHandler`/`ICancelHandler`.** `NavigationSelectable` only
  implements `IPointerEnterHandler`/`IPointerExitHandler` (for `Hover`), which are `EventSystem`
  pointer events, not Input System actions, and are needed regardless of what drives navigation.
  Select/Deselect/Submit/Cancel are plain public methods invoked by `NavigationManager`, not
  `EventSystem` messages — this keeps the framework the sole owner of "what is currently focused",
  rather than sharing that state with Unity's built-in (and mouse/EventSystem-centric) selection system.

## Phase 2 — Runtime Navigation

`NavigationManager` (`Runtime/Navigation/NavigationManager.cs`) is the runtime driver: it loads a
`NavigationGraph`, tracks the currently focused node, and exposes exactly the three input-agnostic
verbs described in the Goal section.

| Member | Role |
|---|---|
| `SetGraph(graph)` | Loads a graph and rebuilds the node/group lookup caches from its already-resolved scene references. No scene search, ever — see the Phase 1 note on scene references. Does not select anything by itself. |
| `SelectDefault()` | Selects the graph's node flagged `IsDefault`. |
| `SwitchToPage(pageId)` | Activates a page and applies its `PageEntryMode` — `SelectDefaultNode` always focuses the page's default node; `RestoreLastSelected` focuses whichever node was last selected while that page was active, per-page, falling back to the default node the first time (or if that node is no longer selectable). |
| `SelectNode(nodeId)` | Direct selection, bypassing directional movement. Returns `false` if the node is missing, disabled, or in a disabled group. |
| `Move(Direction)` | Among the current node's connections facing that direction, picks the highest-`Priority` one that is enabled and resolves to a currently selectable node; ties keep declaration order. No-op if there is no current node or no valid candidate — Phase 2 does not wrap around. |
| `Submit()` / `Cancel()` | Invoke the corresponding method on the current node's `NavigationSelectable`, if any. |
| `SetGroupEnabled(groupId, enabled)` | Batch-toggles every node in a group without touching each node's own `IsEnabled` flag. If the group containing the current node is disabled, focus is cleared and the caller is expected to reselect. |
| `RegisterDynamicNode(...)` / `UnregisterDynamicNode(id)` | The runtime registration path for spawned content (inventory slots, carousel items, skill tree nodes) promised in Phase 1. |

### Design notes and deviations

- **`NavigationManager` is a plain C# class, not a MonoBehaviour.** It has no `Update()` of its
  own — it does nothing until a caller invokes `Move`/`Submit`/`Cancel`. This keeps "when
  navigation happens" entirely outside the framework (a `MonoBehaviour` with its own update loop
  would tempt polling Input System state internally, which is exactly the coupling the framework
  is meant to avoid) and makes the manager trivially constructible in edit-mode tests without a
  scene.
- **Dynamic nodes never touch the `NavigationGraph` asset.** `RegisterDynamicNode` creates a
  `NavigationNode` with a fresh GUID and tracks it only in the manager's own lookup dictionary —
  it is never passed to `graph.AddNode`. This honors the Phase 1 promise that the graph "is
  read-only at runtime; all mutators are for editor tooling." Wiring a dynamic node into the rest
  of the graph (or between two dynamic nodes) is done with `NavigationNode.AddConnection` directly
  on the node instances. Auto Connect (Phase 6) is expected to be the thing that normally calls
  this for geometry-based edges — Phase 2 only provides the registration/connection primitives it
  will need.
- **Group enable/disable is manager-owned state, not part of `NavigationGroup`.** A node is
  selectable only if both its own `IsEnabled` flag and its group's *runtime* enabled state (tracked
  in `NavigationManager`, seeded from `NavigationGroup.EnabledByDefault`) are true. Keeping this off
  the data class matches the existing split between authored data (`NavigationGraph`/`NavigationGroup`)
  and runtime state (`NavigationManager`) — the same reasoning already used for why `NavigationNode`
  itself caches no runtime state. The group's runtime-enabled state and `NavigationNode.IsEnabled`
  are two independent switches: the group one is for batch/gameplay toggles (a whole Locked Skill
  group), the node one is for one-off cases (a single locked item), and either being off makes a
  node unselectable.
- **Page switching is a separate call from node selection.** `SelectNode` and `Move` do not
  implicitly change `CurrentPage` — only `SwitchToPage` does, and only it consults per-page
  "last selected" memory. Plain node selection (e.g. `SelectDefault`) is a lower-level operation
  than page/tab management and intentionally does not carry page bookkeeping.
- **No wraparound and no auto-recovery on `Move`.** If there is no connection in the requested
  direction (or the only candidates are currently unselectable), `Move` simply does nothing. Adding
  wraparound or "nearest selectable" fallback was judged premature without a concrete sample asking
  for it — flagging here in case a specific screen (e.g. a Carousel) needs it, which would be
  handled in that sample rather than in the core manager.
