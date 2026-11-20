# Profile Defines Manager Lite

A Unity 6 editor tool for managing scripting define symbols across Build Profiles.

Unity 6 introduced Build Profiles to replace the old Build Settings workflow, but there is no built-in way to manage scripting defines across multiple profiles at once. This tool fills that gap.

## What it does

Profile Defines Manager Lite lets you create reusable sets of scripting defines (like DEV_BUILD, ENABLE_CHEATS, USE_LOGGING) and apply them to any combination of Build Profiles in one click. You can preview exactly what will change before anything is written, and undo any apply with Ctrl+Z.

## Requirements

- Unity 6000.0.68f1 or newer (Unity 6; the latest Unity 6 LTS is recommended)

## Installation

1. Import the .unitypackage into your Unity project
2. Open the tool from the menu: Tools > Profile Defines Manager Lite

That's it. No setup, no configuration, no dependencies.

## Features

### Define Sets

Create named collections of scripting defines that you can reuse across profiles. For example, you might have a "DEV" set with DEV_BUILD, ENABLE_CHEATS, and USE_LOGGING, and a "RELEASE" set with RELEASE_BUILD and STRIP_DEBUG.

Each define set is saved as a ScriptableObject asset in your project (by default in Assets/Settings/DefineSets, next to Unity's Build Profiles), so it persists between sessions and can be version controlled. You can move set assets anywhere under Assets; the tool finds them wherever they are.

### Merge Mode

Adds your defines to whatever the profile already has. Existing defines are left untouched.

Example: Profile has [A, B]. Your set has [B, C]. After merge, profile has [A, B, C].

### Diff Preview

Before any changes are written, a popup shows you exactly what will happen to each selected profile. Green entries are defines that will be added. Gray entries are defines that will stay the same.

### Active Profile

The profile list marks the active Build Profile. Highlight another profile and click "Switch Profile" to make it active, without opening File > Build Profiles. Highlight the active profile and click "Apply Changes" to recompile your scripts with its current defines (some Unity 6 versions don't recompile on their own when a tool changes them).

### Undo Support

Applying a define set and editing the defines in a set both support Ctrl+Z. Apply defines to a profile and change your mind? Just undo. (Deleting a define set cannot be undone; you are asked to confirm first.)

## How to use it

1. Open Tools > Profile Defines Manager Lite
2. Click "+ New" to create a define set. Give it a name like "DEV" or "RELEASE".
3. Add your defines in the left panel using the text field and "+ Add" button.
4. In the right panel, check the Build Profiles you want to target.
5. Click "Apply to Selected" and review the diff preview.
6. Click "Apply" to confirm.
7. If you changed the active profile, highlight it and click "Apply Changes" so your scripts recompile with the new defines. To try another profile, highlight it and click "Switch Profile".

You can also click the arrow button next to any profile to see what defines it currently has.

## Folder structure

ProfileDefinesManager/
  Editor/
    Core/          Data model and operations
    UI/            Editor window and popups
  Documentation/
  README.md

Everything lives under an Editor folder with an assembly definition, so none of this code is included in your game builds.

## Upgrade to Pro

Profile Defines Manager Pro adds the following features:

- Replace mode (overwrite a profile's defines entirely)
- Comparison Matrix (see which defines are on which profiles in a table view)
- Define Discovery (scan your codebase to find every define symbol in use)
- Audit All (check every profile for duplicates, casing mismatches, invalid characters, and stray whitespace)
- Normalize All (trim, dedupe, and sort the defines on every profile in one undoable step)
- JSON export and import (share define sets with your team)

The Pro version uses the same namespace and folder structure, so upgrading is seamless. Import the Pro package on top of Lite (you don't need to remove Lite first; its files are updated in place). Your define sets live outside the package folder, in Assets/Settings/DefineSets, so they carry over automatically.

## Support

If you run into any issues or have feature requests, please reach out through the Asset Store page or leave a review. Bug reports with steps to reproduce are always appreciated.

## Version history

### 1.0.0

- Initial release
- Define set creation, editing, and deletion
- Merge apply mode
- Diff preview popup
- Switch the active profile and apply its changes from the tool
- Undo support for applies and define set edits
