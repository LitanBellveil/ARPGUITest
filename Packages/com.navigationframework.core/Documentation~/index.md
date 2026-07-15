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

## Phase 5 — Generate From Scene

`NavigationSceneGenerator` (`Editor/NavigationSceneGenerator.cs`), wired into `NavigationGraphEditor`
alongside a new "Scan Root" field on `NavigationGraph`. Scans a chosen `Transform` (inactive children
included) for `NavigationSelectable` components and creates/updates matching `NavigationNode`
entries.

| Member | Role |
|---|---|
| `NavigationGraph.GenerateFromSceneRoot` / `SetGenerateFromSceneRoot` | The scene root remembered per-graph so the Inspector doesn't need it re-dragged every run. Editor-only, no runtime effect — same rationale as `NavigationNode.EditorPosition`. |
| `NavigationSceneGenerator.GenerateFromScene(graph, scanRoot)` | `scanRoot.GetComponentsInChildren<NavigationSelectable>(true)`, matched against existing nodes by their `Selectable` reference. New selectables get a new `NavigationNode` (display name = GameObject name, `EditorPosition` derived from `anchoredPosition`); already-matched nodes only get their scene references refreshed. |
| `NavigationGraphEditor` "Generate From Scene" section | The "Scan Root" object field + "Generate From Scene" button (disabled with no root set), placed above Groups/Pages in the Inspector. |

### Design notes and deviations

- **Additive and non-destructive by design: matching is by `Selectable` reference, and nothing is
  ever deleted.** Re-running this after the UI has grown only adds nodes for newly-added
  `NavigationSelectable`s and refreshes scene references on ones already tracked — it never
  overwrites a node's display name, group/page assignment, connections, or `EditorPosition`, and
  never removes a node whose selectable can no longer be found. The alternative (diffing and
  pruning orphaned nodes automatically) risked silently deleting hand-wired connections the moment
  a button was temporarily disabled or renamed during iteration; deleting a node is left to the
  Graph Window, where it's a deliberate, visible action.
- **Inactive GameObjects are included in the scan** (`GetComponentsInChildren<NavigationSelectable>(true)`).
  Popups/Dialogs are routinely authored inactive-by-default and only activated at runtime, so
  scanning only active objects would silently skip exactly the screens most likely to need this.
- **The scan root is a single `Transform`, not "the whole scene."** Sweeping the entire open scene
  with `FindObjectsByType<NavigationSelectable>` would mix unrelated panels' selectables into
  whichever graph happened to be selected — most projects will have one `NavigationGraph` per screen
  (Character/Weapon/Inventory/etc., per the framework's stated goal), so scanning is scoped to
  whatever root the user points at (typically that screen's root Canvas or panel).
- **`anchoredPosition` is Y-flipped when converted to `EditorPosition`.** uGUI's `anchoredPosition`
  is Y-up; the Graph Window's canvas (like most UI Toolkit/screen-space layouts) is Y-down. Copying
  the value directly would make the generated graph look vertically mirrored from the actual UI, so
  the Y sign is flipped to keep "up" in the Graph Window match "up" on screen.
- **No cleanup pass for orphaned nodes.** Deliberately left out of this phase — see the
  "non-destructive by design" note above. If a concrete need for an assisted cleanup step comes up
  later, it should be a separate, explicit action in the Graph Window (so removals stay visible and
  reviewable) rather than an implicit side effect of generation.

## Phase 6 — Auto Connect

`NavigationAutoConnector` (`Editor/NavigationAutoConnector.cs`), wired into `NavigationGraphEditor`
below "Generate From Scene". Generates one `NavigationConnection` per direction per node from real
screen geometry (`NavigationNode.RectTransform`), instead of every edge needing to be dragged by
hand in the Graph Window.

| Member | Role |
|---|---|
| `NavigationConnection.IsAutoGenerated` / `SetAutoGenerated` | New field marking whether a connection came from Auto Connect rather than being hand-drawn. Lets a re-run regenerate only its own previous output. |
| `NavigationAutoConnector.AutoConnect(graph)` | Removes every `IsAutoGenerated` connection in the graph, then for each node with a `RectTransform`, finds the nearest same-Group-and-Page node in each of the 4 directions and adds one connection to it, marked `IsAutoGenerated`. Nodes without a `RectTransform` are skipped entirely (neither as a source nor a candidate). |
| `NavigationGraphEditor` "Auto Connect" section | A help box explaining the behavior + the "Auto Connect" button. |
| `NavigationGraphEditorWindow.RefreshIfOpen(graph)` | New: if a Graph Window is already open on this graph, rebuilds its view so newly generated nodes/edges are visible immediately, without opening a new window or stealing focus. Called after both Generate From Scene and Auto Connect. |
| `NavigationGraphInspectorPanel` connection view | Now shows a read-only "Auto-Generated: Yes/No" line, so it's obvious at a glance whether a selected edge came from Auto Connect or was hand-drawn. |

### Design notes and deviations

- **Direction is classified by whichever axis has the larger displacement, not a diagonal/angle
  threshold.** For two node centers, `|dx| > |dy|` means horizontal (Left/Right), otherwise vertical
  (Up/Down) — the simplest classification that gives predictable, easy-to-reason-about results
  (equivalent to a 45°-diagonal split of the plane into 4 quadrants), matching how Unity's own uGUI
  "Automatic" navigation mode approaches the same problem.
- **Exactly one connection per direction per node — the single nearest candidate, not multiple
  prioritized ones.** `NavigationConnection.Priority` exists specifically to support multiple
  same-direction edges (Phase 1), but guessing a *set* of weighted candidates automatically was
  judged more likely to produce confusing, hard-to-predict graphs than useful ones. Auto Connect
  produces the obvious single edge; a human adds a second prioritized edge by hand in the Graph
  Window for the cases that actually need one.
- **Page is treated as a hard boundary alongside Group, even though Phase 1 only explicitly called
  out Group.** Without this, two nodes on different Pages (e.g. different tabs, only one of which is
  ever visible at once) that happen to sit at similar screen positions could get auto-connected to
  each other, which never makes sense — a Page is exactly the same kind of "these don't coexist"
  boundary Group already is. For graphs that don't use Pages at all, every node shares the same
  (empty) `PageId`, so this restriction is a no-op in the common case.
- **Auto-generated connections are marked and regenerated, not merged additively.** Unlike Generate
  From Scene (where identity-matching naturally prevents duplicates), re-running Auto Connect after
  moving a widget would otherwise leave a stale edge from the old position alongside a new one from
  the new position, accumulating garbage on every re-run. Tracking `IsAutoGenerated` and wiping only
  those connections before regenerating keeps Auto Connect safely repeatable while never touching
  connections drawn by hand.
- **World-space geometry, not `EditorPosition`.** The Graph Window's canvas position is a visual
  authoring aid the user can rearrange freely and is not guaranteed to reflect actual on-screen
  layout. Direction is computed from each `RectTransform`'s world-space rect center
  (`TransformPoint(rect.center)`, X/Y only — Z depth is ignored, a reasonable simplification for
  standard Screen Space canvases, less accurate for an unusually rotated World Space canvas) so
  "Down" in the generated graph matches what actually renders below on screen, regardless of how the
  nodes happen to be arranged in the Graph Window.
- **`RefreshIfOpen` fully rebuilds the Graph Window's view, so it drops the current selection and
  any pan/zoom state.** Preserving viewport/selection across a refresh would need extra state
  capture-and-restore machinery; given Generate From Scene and Auto Connect are typically run before
  fine-tuning (not mid-fine-tuning), losing the transient view state on refresh was judged an
  acceptable trade-off rather than something worth the extra complexity right now.

## Phase 7 foundation — Input driver abstraction

Before writing the Samples themselves, Phase 7 adds a small `Runtime/Input/` module so every sample
doesn't reinvent `NavigationTestDriver`'s mix of graph-lifecycle boilerplate and keyboard polling in
one file.

| Member | Role |
|---|---|
| `INavigationInputSource` | Runtime-only interface: `DirectionPressed` (`Action<Direction>`), `SubmitPressed`, `CancelPressed`. No reference to Unity's Input System or any project's PlayerControls asset — implementations own that. |
| `NavigationInputRouter` (`MonoBehaviour`) | Owns a `NavigationManager` (exposed via `Manager`, for UI code to subscribe to `NodeChanged`). On `OnEnable`, calls `SetGraph` then `SelectDefault` (or `SwitchToPage(initialPageId)` if set), and subscribes to the assigned `inputSource`'s three events, forwarding them to `Manager.Move`/`Submit`/`Cancel`. Unsubscribes on `OnDisable`. |

### Design notes and deviations

- **`inputSource` is serialized as a plain `MonoBehaviour` field, not the interface itself.** Unity's
  Inspector cannot serialize a reference typed as a C# interface directly; the field is validated
  with an `as INavigationInputSource` cast in `OnEnable` (logging an error if it fails) instead of a
  custom property drawer, since that was judged unnecessary complexity for a field with a clear
  failure mode.
- **Concrete input sources (keyboard, gamepad, touch) are deliberately left out of Runtime.** This is
  the direct continuation of the project's original goal — "no coupling to PlayerControls/
  InputActions" — extended to mean the framework never references a specific input backend at all,
  not just a specific project's action asset. `Samples~` is where sample-specific (but still
  reusable across samples) input source implementations belong, since they are never compiled as
  part of the package itself and are the one place that legitimately needs to depend on Unity's
  Input System.
- **`NavigationTestDriver.cs` is superseded, not replaced in place.** It stays as-is until a
  `Samples~` keyboard input source exists to replace it, per its own "delete this once samples land"
  note from Phase 1-3 manual testing.

## Phase 7 — Samples

Nine samples under `Samples~`, each with its own `README.md` covering exact Editor build steps
(scene hierarchy, Graph Window setup, Inspector wiring) — the actual UI layout for each isn't
checked in as hand-authored `.unity`/`.prefab` YAML, only the C# and instructions are.

| Sample | Demonstrates | Controller |
|---|---|---|
| Character | Framework wiring only — the floor for how little code a static menu needs. | none |
| Weapon | A second, persistent piece of state ("equipped") distinct from transient focus. | `WeaponListController` |
| Inventory | `RegisterDynamicNode`/`UnregisterDynamicNode` for runtime-spawned content; manual grid adjacency since Auto Connect can't see nodes that don't exist yet at edit time. | `InventoryController` |
| Setting | `NavigationPage`/`SwitchToPage` tabs, each backed by its own content Group so a hidden tab is never reachable by arrow keys. | `PageTabController` |
| Popup | Group-toggling focus takeover with return-to-caller on close. | `PopupController` |
| Dialog | The same takeover pattern, contrasted with per-widget `Cancelled` subscriptions (practical here since there are only two buttons). | `DialogController` |
| Carousel | Wraparound layered on top of `Move()` via one extra connection pair — the graph can't tell a wrapped carousel from an ordinary row that loops. | `CarouselController` |
| ScrollView | `NodeChanged` used to keep focus visible inside a `ScrollRect` — a UI concern the framework doesn't own. | `ScrollFocusIntoView` |
| Skill Tree | A generated, non-grid layout; `NavigationConnection.priority` ranks multiple children sharing one direction. | `SkillTreeController` |

`Samples~/Common/` holds two scripts shared by all nine: `KeyboardInputSource` (the reference
`INavigationInputSource`) and `NavigationFocusVisual` (tints a `Graphic` based on
`NavigationSelectable.Selected`/`Deselected`).

### Design notes and deviations

- **`NavigationPage`/`NavigationGroup` are looked up by `DisplayName`, not `Id`, throughout the
  samples that need one (Setting, Popup, Dialog).** Both types only expose their GUID `Id` at
  runtime, and the current Graph Window UI (Phase 3's `NavigationGraphEditor`) has no field that
  displays a page or group's `Id` for copying into a component's Inspector array. Matching by
  display name sidesteps that gap without touching Editor code that's out of Phase 7's scope; a
  real project revisiting this should probably add an Id label to the Groups/Pages Inspector
  section instead.
- **Popup vs. Dialog use two different patterns for "the popup/dialog should close now" because
  their widget counts are different.** `NavigationSelectable.Cancelled` only fires on whichever
  node happens to be focused when Cancel is invoked — for Dialog's two buttons, subscribing to both
  individually is simple and exhaustive. Popup can have arbitrarily many controls, so subscribing to
  every one's `Cancelled` doesn't scale; instead `PopupController` listens directly to the same
  `INavigationInputSource.CancelPressed` event a `NavigationInputRouter` already forwards to
  `Manager.Cancel()`. Both fire from the same key press, which is harmless for a sample but worth
  knowing if a real project wants Popup's Cancel-anywhere behavior without also invoking whatever
  the focused widget's own `Cancelled` handler does.
- **Inventory and Carousel wire their dynamic nodes' connections by hand (row/grid math), not Auto
  Connect.** `NavigationAutoConnector.AutoConnect` (Phase 6) only reads `NavigationGraph.Nodes` —
  nodes registered via `RegisterDynamicNode` are tracked solely in the manager's runtime dictionary
  and never added to the graph asset (by design, so the graph stays read-only in Play Mode), so Auto
  Connect has nothing to see for them regardless of when it runs.

### Post-Phase-7 fixes (found building the Character sample)

- **`NavigationSelectable` now forces the same GameObject's Unity `Selectable.navigation` to
  `None`.** Left at Unity's default (Automatic), a Button's own EventSystem-driven navigation reacts
  to the same arrow keys as this framework and drives the same `Target Graphic`'s color via its own
  Selected/Highlighted state — two independent systems fighting over one widget. This is exactly
  the coupling `NavigationSelectable`'s class doc already disclaimed ("never relies on Unity's
  EventSystem selection state"), but nothing enforced it structurally until now. Enforced in both
  `Reset` (new widgets) and `OnValidate` (existing ones, so it self-corrects on next Inspector touch
  or recompile without manual fixing per widget).
- **The Graph Window highlights the live `NavigationManager.CurrentNode` during Play Mode**
  (`NavigationGraphEditorWindow.UpdateLiveFocusHighlight`, `NavigationGraphView.SetLiveFocusedNode`,
  `NavigationNodeView.SetLiveFocused`) — a green border on whichever node a running
  `NavigationInputRouter`'s manager currently has focused, found by matching `Manager.Graph` against
  the open graph asset via `FindObjectsByType<NavigationInputRouter>`. No registry/event was added
  to `NavigationManager` itself for this — it's Editor-only polling on top of already-public runtime
  state, so it adds zero runtime cost or coupling outside the Editor assembly.
- **`NavigationGeometryConnector` (`Runtime/Navigation/`) extracts Auto Connect's nearest-neighbor
  algorithm out of the Editor assembly so runtime code can call it too.** Came up retrofitting the
  framework onto an existing inventory `ScrollView` whose item count grows at runtime — hardcoding
  row/column adjacency (the original Inventory sample's approach) doesn't adapt if the `LayoutGroup`
  arrangement changes, and can't be authored in the Graph Window anyway since the nodes don't exist
  until Play. The extracted `Connect(nodes, markAutoGenerated)` takes a plain node list instead of a
  `NavigationGraph`, so it has no Group/Page concept of its own — `NavigationAutoConnector` recovers
  that boundary by calling it once per (Group, Page) bucket, since `Connect` only ever connects
  nodes within whatever list it's given to each other. `DisconnectAll` exists because, unlike the
  Editor tool's `IsAutoGenerated`-flag-based wipe, runtime callers connecting a list that keeps
  growing need to clear old edges between still-alive nodes before recomputing, or connections
  accumulate duplicates every time the list changes.
- **A geometry-connected dynamic list still needs one connection hand-wired at runtime if something
  else (e.g. a filter/tab bar) sits above it.** `NavigationGeometryConnector.Connect` only connects
  nodes within the list passed to it — an authored "entry" node from the graph asset isn't part of
  that list, so linking the entry node to the list's first item is left to the caller. Superseded by
  `NavigationScrollViewAnchor`/`NavigationDynamicListConnector` below for the general case of *any*
  number of neighbors in *any* direction, not just one entry point from one side.
- **`NavigationScrollViewAnchor` (`Runtime/Components/`) + `NavigationDynamicListConnector.AttachDynamicList`
  (`Runtime/Navigation/`) let a page mix regular authored buttons with one dynamic list in a single
  Generate From Scene + Auto Connect pass.** Came from retrofitting the framework onto a page that
  has a ScrollView (dynamic, item count grows at runtime) alongside ordinary static buttons — the
  dynamic list's nodes don't exist at edit time, so there was nothing for the surrounding buttons'
  connections to target when the whole page gets Auto-Connected together. `NavigationScrollViewAnchor`
  is a plain `NavigationSelectable` subclass with no added behavior — its only purpose is being a
  distinct, recognizable type so Generate From Scene picks it up as a normal node (wired to its
  neighbors exactly like a button would be) while still being identifiable at runtime as "this one's
  a stand-in, not a real widget." `AttachDynamicList` then transplants every one of the anchor's
  connections onto whichever real boundary node (first or last, chosen by direction — Up/Left→first,
  Down/Right→last) is now adjacent to that neighbor, redirects the neighbors' own connections the
  same way, and disables the anchor.
  - **Assumes a single row or column, not a grid** — a grid's side/interior edges wouldn't get
    outside connections from this, only the very first and last cell in spawn order would. Extending
    it to grids would need knowing which cells sit on which edge, which isn't information this
    algorithm has (it only knows first/last in a flat list) — left for a future need to drive the
    design rather than guessed at now.
  - **Intended to run exactly once**, right after the list's first population. It only adds
    connections rather than replacing anything, so a second call duplicates whatever the first call
    added — most visibly on the first node, which usually doesn't change across list growth (unlike
    the last node, which does). A list whose trailing boundary needs to keep tracking "whatever's
    currently last" after growth has to redirect that one connection by hand instead of re-running
    the whole transplant.
