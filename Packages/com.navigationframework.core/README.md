# Navigation Framework

An input-agnostic UI navigation framework for UGUI on Unity 6.

The framework knows nothing about `InputAction`, `PlayerInput`, or any specific device. It exposes
exactly three verbs at runtime — `Move(Direction)`, `Submit()`, `Cancel()` — and your own input
code (New Input System, legacy Input, or anything else) decides when to call them.

Navigation layouts are authored as a `NavigationGraph` asset (nodes + connections + groups + pages)
using a GraphView-based editor window, then driven at runtime by `NavigationManager`.

See `Documentation~/index.md` for the full architecture write-up and `Samples~/` for end-to-end
examples (Character, Weapon, Inventory, Setting, Popup, Dialog, Carousel, ScrollView, Skill Tree).

## Status

This package is being built in reviewed phases. Current state: **Phase 4 — Graph Save/Load + Auto Save**.
See `CHANGELOG.md` for what has landed so far.

## Install

This package is embedded directly in this project's `Packages/` folder — no separate installation
step is required. To reuse it in another project, copy the `com.navigationframework.core` folder
into that project's `Packages/` directory (or host it in its own git repository and add it via
Package Manager's "Install package from git URL").
