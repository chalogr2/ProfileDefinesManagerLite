# Profile Defines Manager Lite

A Unity 6 editor tool for managing scripting define symbols across Build Profiles.

Unity 6 replaced Build Settings with Build Profiles, but it has no way to manage scripting defines across several profiles at once: you open each profile and edit its define list by hand. This tool lets you keep reusable sets of defines (for example `DEV_BUILD`, `ENABLE_CHEATS`, `USE_LOGGING`) and apply them to any combination of profiles in one click, with a preview of every change before anything is written and a single Ctrl+Z to undo it.

This is the free edition of a tool I built for the Unity Asset Store. A paid Pro edition adds more features on top of the same code.

## Features

- **Define sets:** named, reusable collections of defines, saved as ScriptableObject assets in `Assets/Settings/DefineSets` so they live in version control and survive upgrades.
- **Merge apply:** adds a set's defines to each selected profile and leaves the profile's existing defines alone.
- **Diff preview:** a popup shows, per profile, which defines will be added and which stay the same, before anything is written.
- **Undo:** applying to several profiles at once is one undo step.
- **Active profile:** switch the active Build Profile from the tool, and recompile with its defines on Unity versions that don't do it on their own.
- **Editor-only:** everything sits under an `Editor` folder with its own assembly definition, so none of it ends up in game builds.

Full usage instructions are in [`Assets/ProfileDefinesManager/README.md`](Assets/ProfileDefinesManager/README.md) and [`Documentation/ProfileDefinesManager.md`](Assets/ProfileDefinesManager/Documentation/ProfileDefinesManager.md).

## Trying it

This repository is a complete Unity project. Open it in **Unity 6000.0.68f1** or newer, then open **Tools > Profile Defines Manager Lite**. Three example Build Profiles (`Windows-DEV`, `Windows-QA`, `Windows-RELEASE`) are included under `Assets/Settings/Build Profiles` so there is something to apply defines to.

The product itself is only `Assets/ProfileDefinesManager/`. Everything else (scene, render settings, `TutorialInfo`) is Unity's URP project template.

## Code tour

| File | Responsibility |
|---|---|
| `Editor/Core/DefineSet.cs` | The define set asset |
| `Editor/Core/DefineSetManager.cs` | Creating, finding, renaming and deleting set assets |
| `Editor/Core/DefineOperations.cs` | Computing and applying the merged define lists, with undo |
| `Editor/Core/DefineValidator.cs` | Validating and normalizing define symbols |
| `Editor/Core/ProfileDiscovery.cs` | Finding Build Profiles and reading/writing their defines |
| `Editor/UI/DefinesManagerWindow.cs` | The main editor window |
| `Editor/UI/DiffPreviewPopup.cs` | The preview shown before an apply |

## Testing

Each release is checked with an automated smoke test that builds a throwaway Unity project, runs in batch mode on three Unity 6 editors (6000.0.48f1, 6000.0.68f1 and 6000.3.22f1), and reports every failed check, compiler error and compiler warning. Version 1.0.0 passed all 35 checks on all three editors with no compiler warnings. It covers profile discovery, creating, duplicating, renaming and deleting sets, merge apply to one and several profiles, persistence to disk, single-step undo, validation and diffing, opening the window and the preview popup, and the active-profile buttons. The test harness is shared with the paid edition, so it isn't part of this repository.

## About this repository

Development happens in a private repository that the free and paid editions share: Lite is the upstream, and fixes flow from Lite into Pro. The free edition started as a copy of the Pro code with the paid features removed, so its early history contains code from the paid edition. Publishing that history would publish the paid features.

So this public repository holds snapshots instead: each release of the free edition is added here as a single commit containing exactly the released code. The first commit is version 1.0.0.

## License

Copyright (c) 2026 Fernando González Ruiz. All rights reserved. See [LICENSE](LICENSE).
