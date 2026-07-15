# Changelog

All notable changes to this package are documented in this file.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- Phase 2: runtime navigation — `NavigationManager` (`Runtime/Navigation/`), a plain C# class
  driving `Move`/`Submit`/`Cancel` over a loaded `NavigationGraph`, plus `SelectDefault`,
  `SwitchToPage` (page-aware focus per `PageEntryMode`), `SelectNode`, `SetGroupEnabled`, and
  `RegisterDynamicNode`/`UnregisterDynamicNode` for runtime-spawned content. See Phase 6 for
  automated connection generation.
- Phase 3: editor graph window — `NavigationGraphEditorWindow`, a GraphView (UI Toolkit) window for
  authoring `NavigationGraph` assets (`Editor/`), with `NavigationNodeView` (one input port, four
  directional output ports), a side `NavigationGraphInspectorPanel` for node/connection fields, and
  a `NavigationGraphEditor` custom Inspector for Group/Page management. Adds mutators that Phase 1
  was missing (`NavigationConnection.SetPriority`, `NavigationNode.SetDisplayName`,
  `NavigationGroup.SetDisplayName`/`SetEnabledByDefault`, `NavigationPage.SetDisplayName`/
  `SetDefaultNode`/`SetEntryMode`). Persistence is Unity's normal dirty/save cycle for now — an
  explicit save/auto-save flow is Phase 4.
- Phase 4: graph save/load + auto save — `NavigationGraphAutoSaver` debounce-saves a
  `NavigationGraph` 2 seconds after its last edit (`AssetDatabase.SaveAssetIfDirty`), replacing
  Phase 3's bare `SetDirty` calls everywhere (including node drag, which Phase 3 tracked in
  `EditorPosition` but never actually marked dirty). Forces an immediate flush of any pending saves
  right before entering Play Mode. Adds a "Save Now" button and "Auto Save" toggle to the graph
  window's toolbar, plus a dirty `*` in the window title.
- A testing note on `Enter Play Mode Settings` → "Reload Scene" breaking a graph's scene references
  under Play, found during manual smoke-testing of Phases 1-3; documented in
  `Documentation~/index.md` since Phase 5 will hit the same caveat.

## [0.1.0] - 2026-07-10

### Added

- Phase 1: runtime data structures — `Direction`, `NavigationConnection`, `NavigationNode`,
  `NavigationGroup`, `NavigationPage`, `NavigationGraph` (ScriptableObject) and the
  `NavigationSelectable` component. No navigation logic yet (see Phase 2).
