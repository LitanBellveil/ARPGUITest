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
