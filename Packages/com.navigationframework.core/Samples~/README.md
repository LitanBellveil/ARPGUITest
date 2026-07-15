# Samples

Each sample is a self-contained scene demonstrating the framework against one use case: Character,
Weapon, Inventory, Setting, Popup, Dialog, Carousel, ScrollView, Skill Tree. Samples are registered
in `package.json`'s `samples` array and imported on demand through the Unity Package Manager
window — they are never compiled as part of the package itself.

**Import `Common` first** (Package Manager > Navigation Framework > Samples > Common > Import) —
every other sample's build steps assume `KeyboardInputSource` and `NavigationFocusVisual` already
exist in your project. It's listed as its own importable sample specifically so Package Manager's
Samples UI can put it in your project at all; it isn't meant to be used standalone.

`Common/KeyboardInputSource.cs` is the reference `INavigationInputSource` implementation (arrow
keys + Enter/Escape via the new Input System) that every sample's `NavigationInputRouter` uses. It
supersedes `Assets/_NavigationFrameworkTest/NavigationTestDriver.cs`, which only existed for manual
smoke-testing before these samples existed and should be deleted once a sample is imported and
confirmed working. Swapping to gamepad/touch input means writing another `INavigationInputSource`
implementation next to it and assigning that instead — no framework code changes.

Each sample folder below has its own `README.md` with exact Editor build steps (scene hierarchy,
which components to add, what to wire in the Inspector) since the visual layout itself isn't
something checked into source as hand-written YAML.
