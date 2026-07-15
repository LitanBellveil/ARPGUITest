# Skill Tree sample

A small branching skill tree, generated and connected entirely at runtime — the one sample where
Up/Down means parent/child rather than "whatever's geometrically nearest."

## Build steps

1. **Create the graph asset**: `Create > NavigationFramework > Navigation Graph`, name it
   `SkillTreeGraph`. Leave it empty — every node is spawned by `SkillTreeController`.
2. **Make a node prefab**: a `Button`/`Image` with `NavigationSelectable` + `NavigationFocusVisual`,
   saved as a Prefab (e.g. `SkillNode.prefab`).
3. **Build the scene hierarchy.**
   - `Canvas`
     - `TreeParent` — empty `RectTransform`, centered; `SkillTreeController` positions every
       spawned node relative to this.
   - `NavigationBootstrap` with `KeyboardInputSource` + `NavigationInputRouter` (`Graph` =
     `SkillTreeGraph`).
4. **Add `SkillTreeController`** (e.g. on `TreeParent`). In the Inspector:
   - `Router`, `Tree Parent` (-> `TreeParent`), `Node Prefab` (-> `SkillNode.prefab`'s
     `NavigationSelectable`).
   - `Skills` — a list of `{ id, parentId, displayName }` entries. `id`s must be unique;
     `parentId` empty means "this is the root." Example, a root with two children, one of which has
     two children of its own:
     ```
     { id: "root",   parentId: "",     displayName: "Root" }
     { id: "left",   parentId: "root", displayName: "Left Branch" }
     { id: "right",  parentId: "root", displayName: "Right Branch" }
     { id: "left_a", parentId: "left", displayName: "Left A" }
     { id: "left_b", parentId: "left", displayName: "Left B" }
     ```
   - `Row Height` / `Column Width` control the generated layout spacing (defaults are usually fine
     for a small tree).
5. **Play test**: Down from `Root` always lands on `Left Branch` (the first-declared child, via
   `NavigationConnection.priority`); Right from `Left Branch` reaches `Right Branch`; Down from
   `Left Branch` reaches `Left A`, Right reaches `Left B`; Up from any child returns to its parent.
