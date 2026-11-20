# Profile Defines Manager Lite - Documentation

## Table of Contents

1. Overview
2. Getting Started
3. The Main Window
4. Define Sets
5. Build Profiles Panel
6. Applying Defines
7. Diff Preview
8. How Build Profile Defines Work in Unity
9. Troubleshooting
10. FAQ

---

## 1. Overview

Profile Defines Manager Lite is an editor tool for Unity 6 (6000.0.68f1 or newer) that gives you control over scripting define symbols across Build Profiles.

Unity 6 replaced the old Build Settings with Build Profiles, which let you have multiple build configurations side by side. Each profile can have its own set of scripting defines. This tool makes it easy to manage those defines without clicking through each profile manually.

Instead of editing defines one profile at a time, you create reusable define sets and apply them to whichever profiles you want. The tool shows you a diff before making changes and supports undo.

---

## 2. Getting Started

### Installation

Import the .unitypackage into any Unity 6 project. The tool is editor-only and will not add anything to your game builds.

### Opening the window

Go to Tools > Profile Defines Manager Lite in the Unity menu bar.

### Quick start

1. Click "+ New" in the top-left to create a define set.
2. Type a name like "DEV" and press Enter or click the checkmark.
3. Type a define symbol in the text field at the bottom of the left panel (for example, DEV_BUILD) and click "+ Add".
4. Repeat to add more defines.
5. Make sure the created set is selected in the left panel. In the right panel, check one or more Build Profiles to apply your created set to.
6. Click "Apply to Selected".
7. Review the diff preview and click "Apply".

Done. The selected profiles now have your defines.

---

## 3. The Main Window

The window is split into three areas:

**Left panel** - Shows your define sets and lets you create, rename, delete, and edit them. When a set is selected, its defines are listed below.

**Right panel** - Shows all Build Profiles found in your project. Each one has a checkbox for targeting and an arrow button to inspect its current defines. The header has the "Switch Profile" and "Refresh" buttons.

**Bottom bar** - Contains the "Apply to Selected" and "Apply Changes" buttons.

---

## 4. Define Sets

A define set is a named list of scripting define symbols. Each set is stored as a ScriptableObject asset in your project. New sets are saved in Assets/Settings/DefineSets.

### Creating a set

Click "+ New" in the Define Sets header. A text field appears where you can type the name. Press Enter or click the checkmark to confirm. Click the X to cancel.

### Renaming a set

Click the rename icon (pencil) next to the set name. Edit the name in the text field that appears and press Enter to confirm.

### Deleting a set

Click the X button next to the set name. A confirmation dialog will appear.

### Adding defines to a set

Select a set in the left panel. At the bottom of the defines list, type a symbol name in the text field and click "+ Add". The define is added to the set immediately.

Define names must follow C# identifier rules. The tool will warn you if a name is invalid, contains spaces, or is a duplicate.

### Removing defines from a set

Click the X button next to any define in the list to remove it.

---

## 5. Build Profiles Panel

The right panel lists every Build Profile found in your project. This includes profiles stored in the default Settings/Build Profiles folder and any profiles elsewhere in your Assets folder.

Each profile row shows:

- A checkbox for selecting it as a target for apply operations
- The profile name
- A count in parentheses showing how many defines the profile currently has
- An "Active" label on the profile that is currently active
- An arrow button that expands a panel showing the profile's current defines

Click "Refresh" at the top of the panel if you have added or removed profiles outside the tool.

### Switching the active profile

Click a profile row to highlight it, then click "Switch Profile" at the top of the panel. The highlighted profile becomes the active Build Profile, exactly as if you had clicked "Switch Profile" in File > Build Profiles. If the profile targets another platform, Unity switches platform, which can take a while in large projects.

"Switch Profile" is grayed out when no profile is highlighted, when the highlighted profile is already active, and while Unity is compiling or in Play mode.

The bottom of the right panel shows the "Profile Defines" detail view for whichever profile is currently highlighted (clicked on, not just checked).

The note at the bottom reminds you that global PlayerSettings defines are applied additively by Unity at build time. This means a profile's effective defines are its own defines plus whatever is set globally.

---

## 6. Applying Defines

To apply a define set to one or more profiles:

1. Select a define set in the left panel.
2. Check one or more profiles in the right panel.
3. Click "Apply to Selected (N)" where N is the number of checked profiles.
4. Review the diff preview popup.
5. Click "Apply" to confirm, or "Cancel" to back out.

### Merge mode

The Lite version uses Merge mode, which adds the defines from your set to the profile. Any defines the profile already has are left alone.

Example: Profile has [A, B]. Set has [B, C]. After merge, profile has [A, B, C].

### Applying changes to the active profile

Only the active profile's defines are used when Unity compiles your scripts in the editor. Some Unity 6 versions do not recompile on their own when the active profile's defines are changed by a tool, so your code keeps the old defines until the next recompile.

To make the changes take effect, highlight the active profile in the right panel and click "Apply Changes" in the bottom bar. This recompiles your scripts with the active profile's current defines, the same as clicking "Apply" in File > Build Profiles. On Unity versions that already recompiled, it does no harm.

"Apply Changes" is grayed out unless the highlighted profile is the active one, and while Unity is compiling or in Play mode.

---

## 7. Diff Preview

Before any changes are written to disk, a popup window appears showing a per-profile breakdown of what will happen.

For each selected profile, you will see:

- Green lines (+ symbol) for defines that will be added
- Gray lines (= symbol) for defines that will stay unchanged

The header shows a summary with total additions and unchanged counts.

If a profile will not be changed at all (for example, it already has exactly the defines in your set), it will say "No changes for this profile."

Click "Apply" to go ahead, or "Cancel" to close without making changes.

---

## 8. How Build Profile Defines Work in Unity

Unity 6 introduced Build Profiles as a replacement for the old platform switcher in Build Settings. Each Build Profile can have its own scripting define symbols.

There are two levels of defines:

**Global defines** are set in Edit > Project Settings > Player > Other Settings > Scripting Define Symbols. These apply to all builds regardless of which profile is active.

**Profile defines** are set on individual Build Profiles. These are specific to that profile.

When Unity compiles your scripts, the effective set of defines is the combination of both. Global defines and profile defines are merged together. There is no way for a profile to "remove" a global define.

Only the active profile's defines are used for compiling in the editor (and for builds made with that profile). Defines on other profiles take effect when you switch to them.

This tool manages profile-level defines only. It does not touch global PlayerSettings defines.

---

## 9. Troubleshooting

### The right panel is empty / no profiles are showing

Click "Refresh". If profiles still do not appear, make sure you have created at least one Build Profile in File > Build Profiles. The default platform profile may not appear if it has not been explicitly created as a custom profile.

### "Apply to Selected" is grayed out

Make sure you have both a define set selected (highlighted in the left panel) and at least one profile checked (checkbox in the right panel).

### My code does not see the defines I just applied

Profile defines only affect compilation when the profile is active. Highlight the profile and click "Switch Profile", or, if it is already active, click "Apply Changes" to recompile.

### "Switch Profile" or "Apply Changes" is grayed out

Click a profile row to highlight it first. "Switch Profile" needs a profile that is not active; "Apply Changes" needs the active one. Both are disabled while Unity is compiling or in Play mode.

### Undo is not working

Undo should work for all apply operations. Press Ctrl+Z (Cmd+Z on Mac) immediately after applying. If you have performed other actions in Unity since the apply, you may need to undo multiple times to get back to the pre-apply state.

### Compile errors after import

This tool requires Unity 6000.0.68f1 or newer. If you see errors about BuildProfile or missing types, check that your Unity version is 6000.0.68f1 or later. Earlier versions of Unity do not have the Build Profiles API.

---

## 10. FAQ

**Does this tool affect my game builds?**

No. Everything is in an Editor assembly. None of this code is included in your player builds. It only runs inside the Unity Editor.

**Can I use this with Unity 2022 or earlier?**

No. Build Profiles are a Unity 6 feature, so the tool requires Unity 6 (6000.0.68f1 or newer) and is not supported on earlier versions.

**What happens if I delete a define set that I already applied?**

Nothing changes on the profiles. The defines were written to the profiles when you clicked Apply. Deleting the set just removes the saved template. The profiles keep their defines.

**Can two people on a team use different define sets?**

Yes. Define sets are local to each person's project. If you want to share them, commit the ScriptableObject assets to version control.

**What does the Pro version add?**

Profile Defines Manager Pro adds Replace mode (overwrite defines entirely), Comparison Matrix (see which defines are on which profiles in a table), Define Discovery (scan your codebase to find every define in use), Audit All (check every profile for duplicates, casing mismatches, invalid characters, and stray whitespace), Normalize All (trim, dedupe, and sort every profile in one undoable step), and JSON export/import for team sharing. Upgrading from Lite to Pro is seamless since both use the same namespace and folder structure: import Pro on top of Lite, and your define sets (stored outside the package folder) carry over.

**Where are define set assets stored?**

They are stored as ScriptableObject .asset files in your project. By default they go into an Assets/Settings/DefineSets folder, outside the tool's own folder, so removing or upgrading the tool never deletes them. You can move them anywhere under Assets; the tool finds them wherever they are.
