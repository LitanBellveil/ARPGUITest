# Weapon sample

A selection list/grid over a fixed set of authored items, demonstrating a second piece of state
("equipped") that persists independently of transient focus.

## Build steps

1. **Create the graph asset**: `Create > NavigationFramework > Navigation Graph`, name it
   `WeaponGraph`.
2. **Build the scene hierarchy.**
   - `Canvas`
     - `WeaponPanel` (`GridLayoutGroup` or a manual row/column layout)
       - One `Button` per weapon (4-6 is plenty). On each: `NavigationSelectable` +
         `NavigationFocusVisual` (same as the Character sample), plus a small child GameObject
         (e.g. a checkmark `Image`, disabled by default) to serve as the "equipped" indicator.
3. **Wire the graph**: same as Character — Scan Root -> Generate From Scene -> Auto Connect -> mark
   one node Is Default.
4. **Add `WeaponListController`** to `WeaponPanel` (or any parent). In the Inspector, fill
   `Weapon Slots` with each Button's `NavigationSelectable` and `Equipped Indicators` with each
   Button's checkmark child, **in the same order**.
5. **Add the input rig**: same `KeyboardInputSource` + `NavigationInputRouter` pair as Character,
   pointed at `WeaponGraph`.
6. **Play test**: arrow keys move focus (yellow tint), Enter on a slot equips it (checkmark
   appears and stays even after focus moves elsewhere).
