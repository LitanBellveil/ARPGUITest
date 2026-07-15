# Dialog sample

A "Delete Item?" Yes/No confirm dialog, demonstrating `Submitted`/`Cancelled` used directly on each
button rather than the Popup sample's global-input-listener workaround (practical here because
there are only two focusable widgets to subscribe to).

## Build steps

1. **Create the graph asset**: `Create > NavigationFramework > Navigation Graph`, name it
   `DialogGraph`.
2. **Build the scene hierarchy.**
   - `Canvas`
     - `BaseScreen` — a `DeleteButton` (`NavigationSelectable` + `NavigationFocusVisual`).
     - `DialogPanel` — **inactive by default**. Inside: `YesButton` and `NoButton`, both
       `NavigationSelectable` + `NavigationFocusVisual`.
   - `NavigationBootstrap` with `KeyboardInputSource` + `NavigationInputRouter` (`Graph` =
     `DialogGraph`). Mark `DeleteButton` "Is Default".
3. **Wire the graph**: same pattern as Popup — Generate From Scene from a common parent (inactive
   children included), two Groups (`Base`, `Dialog`) assigned per-panel, Auto Connect (never
   crosses the Group boundary).
4. **Add `DialogController`** to `DialogPanel`'s parent. In the Inspector:
   - `Router`, `Open Button` (-> `DeleteButton`), `Yes Button`, `No Button`, `Dialog Panel` (->
     `DialogPanel`), `Base Group Name` / `Dialog Group Name` -> `Base` / `Dialog`.
5. **(Optional) Consume the result**: from your own script, subscribe to
   `dialogController.Resolved += confirmed => { /* delete or not */ }`.
6. **Play test**: Enter on `DeleteButton` opens the dialog focused on `No` (the safer default).
   Enter on `Yes`/`No` resolves and closes it; Escape on either button also resolves as "No" and
   closes, returning focus to `DeleteButton`.
