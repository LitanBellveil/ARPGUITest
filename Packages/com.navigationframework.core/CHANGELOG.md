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

## [0.1.0] - 2026-07-10

### Added

- Phase 1: runtime data structures — `Direction`, `NavigationConnection`, `NavigationNode`,
  `NavigationGroup`, `NavigationPage`, `NavigationGraph` (ScriptableObject) and the
  `NavigationSelectable` component. No navigation logic yet (see Phase 2).
