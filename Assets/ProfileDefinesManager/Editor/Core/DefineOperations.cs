using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

#if UNITY_6000_0_OR_NEWER
using UnityEditor.Build.Profile;
#endif

namespace ProfileDefinesManager
{
    /// <summary>
    /// Determines how a <see cref="DefineSet"/> is applied to a Build Profile.
    /// </summary>
    public enum ApplyMode
    {
        /// <summary>
        /// Union the profile's existing defines with those in the set.
        /// Existing defines are preserved; only new symbols are added.
        /// </summary>
        Merge,
    }

    /// <summary>
    /// Describes the difference between a Build Profile's current defines
    /// and a proposed new set of defines.
    /// </summary>
    public readonly struct DiffResult
    {
        /// <summary>Gets the symbols that will be <b>added</b> to the profile.</summary>
        public IReadOnlyList<string> ToAdd { get; }

        /// <summary>Gets the symbols that will be <b>removed</b> from the profile.</summary>
        public IReadOnlyList<string> ToRemove { get; }

        /// <summary>Gets the symbols that will remain <b>unchanged</b>.</summary>
        public IReadOnlyList<string> Unchanged { get; }

        /// <summary>Gets <c>true</c> when there are no additions or removals.</summary>
        public bool IsEmpty => ToAdd.Count == 0 && ToRemove.Count == 0;

        /// <summary>Initializes a new <see cref="DiffResult"/>.</summary>
        /// <param name="toAdd">Symbols to be added.</param>
        /// <param name="toRemove">Symbols to be removed.</param>
        /// <param name="unchanged">Symbols that stay the same.</param>
        public DiffResult(
            IReadOnlyList<string> toAdd,
            IReadOnlyList<string> toRemove,
            IReadOnlyList<string> unchanged)
        {
            ToAdd = toAdd;
            ToRemove = toRemove;
            Unchanged = unchanged;
        }
    }

    /// <summary>
    /// Provides diff, merge, and apply operations for scripting define symbols
    /// on Unity 6 Build Profiles.
    /// </summary>
    public static class DefineOperations
    {
        // ─── Pure string operations (always available) ────────────────────────────

        /// <summary>
        /// Computes the difference between a profile's current defines and a proposed set of defines.
        /// </summary>
        /// <param name="current">The profile's current define symbols.</param>
        /// <param name="proposed">The proposed define symbols to apply.</param>
        /// <returns>
        /// A <see cref="DiffResult"/> describing which symbols will be added, removed, or unchanged.
        /// </returns>
        public static DiffResult ComputeDiff(IEnumerable<string> current, IEnumerable<string> proposed)
        {
            var currentSet = new HashSet<string>(
                current ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);

            var proposedSet = new HashSet<string>(
                proposed ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);

            var toAdd = proposedSet
                .Except(currentSet)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var toRemove = currentSet
                .Except(proposedSet)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var unchanged = currentSet
                .Intersect(proposedSet)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new DiffResult(toAdd, toRemove, unchanged);
        }

        /// <summary>
        /// Computes the effective result of merging <paramref name="setDefines"/>
        /// into <paramref name="current"/>.
        /// Existing defines are preserved; new symbols are added.
        /// The result is normalized (trimmed, deduplicated, sorted).
        /// </summary>
        /// <param name="current">The profile's current define symbols.</param>
        /// <param name="setDefines">The define symbols from the <see cref="DefineSet"/> being applied.</param>
        /// <returns>The resulting normalized list of define symbols.</returns>
        public static List<string> ComputeResult(
            IEnumerable<string> current,
            IEnumerable<string> setDefines)
        {
            var currentList = current ?? Enumerable.Empty<string>();
            var setList     = setDefines ?? Enumerable.Empty<string>();

            return DefineValidator.Normalize(currentList.Concat(setList));
        }

#if UNITY_6000_0_OR_NEWER

        // ─── Single-profile apply ─────────────────────────────────────────────────

        /// <summary>
        /// Applies a <see cref="DefineSet"/> to a single Build Profile using Merge mode.
        /// Records an Undo entry and marks the asset dirty.
        /// </summary>
        /// <param name="profile">The target Build Profile.</param>
        /// <param name="set">The define set whose symbols will be applied.</param>
        /// <param name="mode">Apply mode (only <see cref="ApplyMode.Merge"/> is supported).</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="profile"/> or <paramref name="set"/> is null.
        /// </exception>
        public static void Apply(BuildProfile profile, DefineSet set, ApplyMode mode)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (set == null)     throw new ArgumentNullException(nameof(set));

            var newDefines = ComputeResult(profile.scriptingDefines, set.Defines);

            Undo.RecordObject(profile, $"Apply Define Set \"{set.SetName}\"");
            profile.scriptingDefines = newDefines.ToArray();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        // ─── Multi-profile apply ──────────────────────────────────────────────────

        /// <summary>
        /// Applies a <see cref="DefineSet"/> to multiple Build Profiles using Merge mode.
        /// All changes are grouped into a single Undo operation.
        /// </summary>
        /// <param name="profiles">The target Build Profiles.</param>
        /// <param name="set">The define set whose symbols will be applied.</param>
        /// <param name="mode">Apply mode (only <see cref="ApplyMode.Merge"/> is supported).</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="profiles"/> or <paramref name="set"/> is null.
        /// </exception>
        public static void ApplyToMany(IEnumerable<BuildProfile> profiles, DefineSet set, ApplyMode mode)
        {
            if (profiles == null) throw new ArgumentNullException(nameof(profiles));
            if (set == null)      throw new ArgumentNullException(nameof(set));

            var profileList = profiles.Where(p => p != null).ToList();
            if (profileList.Count == 0)
                return;

            var undoLabel = $"Apply Define Set \"{set.SetName}\" to {profileList.Count} profile(s)";
            Undo.SetCurrentGroupName(undoLabel);
            var groupIndex = Undo.GetCurrentGroup();

            foreach (var profile in profileList)
            {
                var newDefines = ComputeResult(profile.scriptingDefines, set.Defines);
                Undo.RecordObject(profile, undoLabel);
                profile.scriptingDefines = newDefines.ToArray();
                EditorUtility.SetDirty(profile);
            }

            Undo.CollapseUndoOperations(groupIndex);
            AssetDatabase.SaveAssets();
        }

        // ─── Quick actions ────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a new <see cref="DefineSet"/> pre-populated with the defines
        /// from the currently active Build Profile.
        /// </summary>
        /// <param name="name">The display name for the new define set.</param>
        /// <returns>
        /// The created <see cref="DefineSet"/>, or <c>null</c> if no active Build Profile is found.
        /// </returns>
        public static DefineSet CreateFromActiveProfile(string name)
        {
            var profile = BuildProfile.GetActiveBuildProfile();
            if (profile == null)
                return null;

            var set = DefineSetManager.Create(name);
            set.Defines.AddRange(profile.scriptingDefines ?? Array.Empty<string>());
            DefineSetManager.Save(set);
            return set;
        }

        /// <summary>
        /// Creates a new <see cref="DefineSet"/> pre-populated with the defines
        /// from a specific Build Profile.
        /// </summary>
        /// <param name="profile">The source Build Profile.</param>
        /// <param name="name">The display name for the new define set.</param>
        /// <returns>The created <see cref="DefineSet"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="profile"/> is null.</exception>
        public static DefineSet CreateFromProfile(BuildProfile profile, string name)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var set = DefineSetManager.Create(name);
            set.Defines.AddRange(profile.scriptingDefines ?? Array.Empty<string>());
            DefineSetManager.Save(set);
            return set;
        }

#endif
    }
}
