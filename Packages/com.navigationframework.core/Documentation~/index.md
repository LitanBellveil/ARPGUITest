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
- **Testing note: "Reload Scene" in Enter Play Mode Settings breaks these references.** A graph
  asset's `RectTransform`/`Selectable` fields point at a specific in-memory instance of a scene
  object. If `Edit > Project Settings > Editor > Enter Play Mode Settings` has "Reload Scene"
  enabled (Unity's default), entering Play Mode reloads the scene from disk and replaces every
  scene object with a fresh instance — the graph's field still shows the old object's name (stale
  label) but no longer resolves to anything, and re-resolves fine again once you're back in Edit
  Mode with the original (never-actually-replaced) instances. This is not fixed by saving the
  scene; the fix is disabling "Reload Scene" for that project. Worth revisiting when Phase 5
  (Generate From Scene) lands, since it will hit the exact same caveat.
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

## Phase 3 — Editor Graph Window

A GraphView (UI Toolkit) based window for authoring `NavigationGraph` assets, all in the
`NavigationFramework.Editor` assembly (`Editor/`), which references Runtime only.

| Type | Role |
|---|---|
| `NavigationGraphEditorWindow` | The `EditorWindow`. Opens via an "Open Graph Window" button on the asset's Inspector or by double-clicking the asset (`[OnOpenAsset]`). Hosts a `NavigationGraphView` and a `NavigationGraphInspectorPanel` side by side in a `TwoPaneSplitView`. A single window instance is reused across graphs. |
| `NavigationGraphView` | The `GraphView` canvas. Builds one `NavigationNodeView` per `NavigationNode` and one `Edge` per `NavigationConnection`; keeps the graph asset in sync as nodes are dragged, created (right-click → "Create Node"), or deleted, and as edges are drawn or removed. |
| `NavigationNodeView` | A `Node` with one generic "In" port (`Port.Capacity.Multi`) and four directional output ports — Up/Down/Left/Right (also `Multi`, since a node can have several same-direction connections; see `NavigationConnection.Priority`). Dragging an edge from a directional output port to another node's input port is what creates a `NavigationConnection` for that direction. Overrides `SetPosition` to keep `NavigationNode.EditorPosition` live while dragging. |
| `NavigationGraphInspectorPanel` | Side panel showing fields for whatever is selected: a node's display name/group/page/default/enabled/scene references, or a connection's priority/enabled. Empty or multi-element selections show a hint instead — bulk editing isn't part of Phase 3. |
| `NavigationGraphEditor` | `[CustomEditor(NavigationGraph)]`. Adds the "Open Graph Window" button plus add/rename/remove for Groups and Pages — graph-wide metadata that doesn't map naturally onto a GraphView node or edge. |

### Design notes and deviations

- **Groups and Pages are edited in the plain Inspector, not inside the GraphView.** They're
  graph-wide lists (not per-node), so there's no natural node/edge to represent them on the canvas.
  Node-level assignment (which group/page a node belongs to) is still done inside the GraphView's
  side panel, via a dropdown populated from the Inspector-managed lists.
- **Connection `Priority`/`Enabled` are edited via the side panel, not on the edge itself.**
  GraphView's default `Edge` has no inline label/field UI, and building a custom edge visual with an
  embedded control was judged more effort than Phase 3 needs — select the edge, then edit it in the
  panel. `Direction` is shown read-only there since it's fixed by which output port the edge came
  from.
- **Three Phase 1 data classes gained mutators they didn't have.** Authoring priority, renaming
  nodes/groups/pages, and editing a page's default node/entry mode all needed a setter that Phase 1
  never added: `NavigationConnection.SetPriority`, `NavigationNode.SetDisplayName`,
  `NavigationGroup.SetDisplayName`/`SetEnabledByDefault`, `NavigationPage.SetDisplayName`/
  `SetDefaultNode`/`SetEntryMode`. These follow the exact pattern already established by Phase 1's
  own `SetGroup`/`SetPage`/`SetDefault`/`SetEnabled`/`SetSceneReferences` — "intended for use by the
  graph editor" was always the plan, Phase 1 just hadn't needed every setter yet.
- **Setting a node "Is Default" clears the flag on every other node in the graph.**
  `NavigationNode.IsDefault` has no built-in uniqueness constraint (Phase 1 didn't enforce one), but
  `NavigationManager.SelectDefault()` only ever honors the *first* default node it finds — so the
  editor enforces "at most one default per graph" itself when the toggle is checked, rather than
  leaving multiple defaults silently possible with confusing runtime behavior.
- **Persistence is just `EditorUtility.SetDirty` + Unity's normal save cycle.** Every mutation calls
  `Undo.RecordObject` (so Ctrl+Z works) and marks the graph dirty, but there is no explicit "Save"
  button or auto-save timer — that's Phase 4's job (Graph Save/Load + Auto Save). For now, saving a
  graph means saving the project the normal way.
- **`TwoPaneSplitView`/`TwoPaneSplitViewOrientation` live in `UnityEngine.UIElements`, not
  `UnityEditor.UIElements`.** Worth calling out since older GraphView tutorials (and this package's
  first draft) reference the pre-2021 namespace; Unity 6 has these types in the runtime UI Toolkit
  module since the control is no longer editor-exclusive.

## Phase 4 — Graph Save/Load + Auto Save

`NavigationGraphAutoSaver` (`Editor/NavigationGraphAutoSaver.cs`) replaces Phase 3's bare
`EditorUtility.SetDirty` calls with a debounced save: `Touch(graph)` marks the graph dirty and
(re)schedules a save 2 seconds out, so a burst of edits collapses into one disk write instead of
one per keystroke/drag. All mutation sites in `NavigationGraphView`, `NavigationGraphInspectorPanel`,
and `NavigationGraphEditor` now call `Touch` instead of `SetDirty` directly — including node drag
(`GraphViewChange.movedElements`), which Phase 3 tracked in `NavigationNode.EditorPosition` but
never actually marked dirty, so dragging a node to reposition it was silently never saved.

| Member | Role |
|---|---|
| `NavigationGraphAutoSaver.Touch(graph)` | Marks dirty; if auto-save is on, debounce-schedules `AssetDatabase.SaveAssetIfDirty(graph)`. |
| `NavigationGraphAutoSaver.SaveNow(graph)` | Immediate save, bypassing the debounce delay — wired to the window's "Save Now" toolbar button. |
| `NavigationGraphAutoSaver.AutoSaveEnabled` | Per-user `EditorPrefs`-backed toggle, defaults to on, exposed as the "Auto Save" toolbar checkbox. |
| `NavigationGraphEditorWindow` toolbar | "Save Now" button + "Auto Save" toggle, added above the split view. The window title also grows a trailing `*` while the graph is dirty (checked every `Update()` tick), mirroring Unity's own convention for unsaved scenes/assets. |

### Design notes and deviations

- **Auto-save is debounced, not instant.** Saving on every single edit (e.g. every frame while
  dragging a node) would mean constant disk I/O and constant `AssetDatabase` refresh churn. A
  2-second idle window after the *last* edit was chosen instead — arbitrary but conservative; there
  is no config for this yet since no concrete need for a different value has come up.
- **All pending auto-saves are force-flushed right before entering Play Mode**
  (`EditorApplication.playModeStateChanged`, `PlayModeStateChange.ExitingEditMode`), regardless of
  the debounce timer. This exists specifically because of a real bug hit during manual testing: a
  graph asset holds direct references to scene objects, and if the project's
  `Enter Play Mode Settings` has "Reload Scene" enabled (Unity's default), entering Play reloads the
  scene and replaces every scene object with a fresh instance. That reload risk is orthogonal to
  whether the *graph* itself was saved — flushing the graph doesn't fix a stale scene reference by
  itself — but there is no reason to *also* risk losing an unsaved graph edit at the same moment, so
  Phase 4 removes that variable.
- **No "Load" step, because there isn't a custom serialization format to load from.** The graph is
  a plain Unity `ScriptableObject` asset — Unity's own `AssetDatabase`/import pipeline already *is*
  the load path (opening the asset, or Unity loading it as a dependency, deserializes it the normal
  way). "Graph Save/Load" in the phase name turned out to mean "make sure edits reliably reach
  disk," not "build a bespoke load routine" — there was nothing else Phase 1's `ScriptableObject`
  choice left to build here.
- **The dirty asterisk polls `EditorUtility.IsDirty` once per `Update()` tick rather than reacting
  to a change event.** GraphView/IMGUI mutations don't raise a single common "graph changed" event
  to hook into (they go through several different code paths — GraphView callbacks, IMGUI change
  checks), so polling a cheap boolean each tick was simpler than wiring a bespoke event through every
  mutation site just to avoid a poll.
