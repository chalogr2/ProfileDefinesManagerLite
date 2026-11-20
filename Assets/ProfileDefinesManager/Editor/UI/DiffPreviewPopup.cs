using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_6000_0_OR_NEWER
using UnityEditor.Build.Profile;
#endif

namespace ProfileDefinesManager
{
    /// <summary>
    /// Modal popup window that shows a per-profile diff preview before a
    /// <see cref="DefineSet"/> is applied to one or more Build Profiles.
    /// Open via <see cref="Show"/>; diffs are precomputed at open time and
    /// no changes are written until the user confirms with the Apply button.
    /// </summary>
    public sealed class DiffPreviewPopup : EditorWindow
    {
#if UNITY_6000_0_OR_NEWER

        // ─── Internal data ────────────────────────────────────────────────────────

        private struct ProfileDiffEntry
        {
            public BuildProfile Profile;
            public string       Name;
            public DiffResult   Diff;
        }

        private DefineSet              _set;
        private Action                 _onApplied;
        private List<ProfileDiffEntry> _entries = new List<ProfileDiffEntry>();
        private bool[]                 _foldouts = Array.Empty<bool>();
        private Vector2                _scrollPos;
        private bool                   _shouldClose;

        // Precomputed totals shown in the header summary line
        private int _totalAdds;
        private int _totalRemoves;
        private int _totalUnchanged;

        // ─── Static factory ───────────────────────────────────────────────────────

        /// <summary>
        /// Creates and shows the diff preview as a modal utility window.
        /// All diffs are computed immediately; the window is read-only until
        /// the user clicks <b>Apply</b>.
        /// </summary>
        /// <param name="set">The <see cref="DefineSet"/> being applied.</param>
        /// <param name="profiles">
        /// The Build Profiles that will receive the defines.
        /// Null entries in the list are silently skipped.
        /// </param>
        /// <param name="onApplied">
        /// Optional callback invoked on the main thread immediately after
        /// the user confirms Apply (and before the window closes).
        /// Use this to trigger a data refresh in the parent window.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="set"/> or <paramref name="profiles"/> is null.
        /// </exception>
        public static void Show(
            DefineSet          set,
            List<BuildProfile> profiles,
            Action             onApplied = null)
        {
            if (set      == null) throw new ArgumentNullException(nameof(set));
            if (profiles == null) throw new ArgumentNullException(nameof(profiles));

            var popup = CreateInstance<DiffPreviewPopup>();
            popup.titleContent = new GUIContent("Profile Defines Manager Lite");
            popup._set       = set;
            popup._onApplied = onApplied;
            popup.BuildEntries(profiles);

            const float w = 420f;
            const float h = 520f;

            var main = EditorGUIUtility.GetMainWindowPosition();
            popup.position = new Rect(
                Mathf.Round(main.x + (main.width  - w) * 0.5f),
                Mathf.Round(main.y + (main.height - h) * 0.5f),
                w, h);

            popup.minSize = new Vector2(320f, 240f);
            popup.ShowAuxWindow();
        }

        // ─── Initialization ───────────────────────────────────────────────────────

        private void BuildEntries(List<BuildProfile> profiles)
        {
            _entries = new List<ProfileDiffEntry>(profiles.Count);
            _totalAdds = _totalRemoves = _totalUnchanged = 0;

            foreach (var profile in profiles)
            {
                if (profile == null) continue;

                var current  = ProfileDiscovery.GetDefines(profile);
                var proposed = DefineOperations.ComputeResult(current, _set.Defines);
                var diff     = DefineOperations.ComputeDiff(current, proposed);

                _entries.Add(new ProfileDiffEntry
                {
                    Profile = profile,
                    Name    = ProfileDiscovery.GetProfileName(profile),
                    Diff    = diff,
                });

                _totalAdds      += diff.ToAdd.Count;
                _totalRemoves   += diff.ToRemove.Count;
                _totalUnchanged += diff.Unchanged.Count;
            }

            // All profiles start expanded
            _foldouts = new bool[_entries.Count];
            for (var i = 0; i < _foldouts.Length; i++)
                _foldouts[i] = true;
        }

        // ─── OnGUI ────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            // Defer Close() to the start of the next OnGUI call, before any layout
            // group is opened, to avoid the "EndLayoutGroup: BeginLayoutGroup must
            // be called first" IMGUI error that occurs when closing mid-layout-pass.
            if (_shouldClose)
            {
                Close();
                return;
            }

            DrawHeader();
            Styles.DrawSeparator();
            DrawScrollBody();
            Styles.DrawSeparator();
            DrawFooter();
        }

        // ─── Header ───────────────────────────────────────────────────────────────

        private void DrawHeader()
        {
            EditorGUILayout.Space(6f);

            EditorGUILayout.LabelField(
                $"Apply \"{_set.SetName}\" to {_entries.Count} profile(s)",
                Styles.PanelHeader);

            EditorGUILayout.LabelField(BuildSummaryLine(), EditorStyles.miniLabel);

            EditorGUILayout.Space(4f);
        }

        private string BuildSummaryLine()
        {
            if (_totalAdds == 0 && _totalRemoves == 0 && _totalUnchanged == 0)
                return "Nothing to show.";

            var parts = new List<string>(3);

            if (_totalAdds      > 0) parts.Add($"+{_totalAdds} add{(_totalAdds == 1 ? "" : "s")}");
            if (_totalRemoves   > 0) parts.Add($"-{_totalRemoves} removal{(_totalRemoves == 1 ? "" : "s")}");
            if (_totalUnchanged > 0) parts.Add($"{_totalUnchanged} unchanged");

            return string.Join("  ·  ", parts);
        }

        // ─── Scroll body ──────────────────────────────────────────────────────────

        private void DrawScrollBody()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));

            if (_entries.Count == 0)
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.HelpBox("No Build Profiles were provided.", MessageType.Info);
            }
            else
            {
                for (var i = 0; i < _entries.Count; i++)
                    DrawProfileEntry(i);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawProfileEntry(int index)
        {
            var entry = _entries[index];

            _foldouts[index] = EditorGUILayout.BeginFoldoutHeaderGroup(_foldouts[index], entry.Name);

            if (_foldouts[index])
            {
                EditorGUI.indentLevel++;

                if (entry.Diff.IsEmpty)
                {
                    EditorGUILayout.LabelField("No changes for this profile.", Styles.DiffUnchanged);
                }
                else
                {
                    // Additions
                    foreach (var sym in entry.Diff.ToAdd)
                        EditorGUILayout.LabelField($"+ {sym}", Styles.DiffAdd);

                    // Removals
                    foreach (var sym in entry.Diff.ToRemove)
                        EditorGUILayout.LabelField($"- {sym}", Styles.DiffRemove);

                    // Symbols that survive unchanged
                    foreach (var sym in entry.Diff.Unchanged)
                        EditorGUILayout.LabelField($"= {sym}", Styles.DiffUnchanged);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        // ─── Footer ───────────────────────────────────────────────────────────────

        private void DrawFooter()
        {
            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Cancel", GUILayout.Width(84f)))
                    _shouldClose = true;

                GUILayout.Space(6f);

                using (new EditorGUI.DisabledScope(_entries.Count == 0))
                {
                    if (GUILayout.Button("Apply", GUILayout.Width(84f)))
                        ExecuteApply();
                }

                GUILayout.Space(6f);
            }

            EditorGUILayout.Space(6f);
        }

        private void ExecuteApply()
        {
            var profiles = new List<BuildProfile>(_entries.Count);
            foreach (var e in _entries)
                profiles.Add(e.Profile);

            // ApplyToMany handles Undo grouping, SetDirty, and SaveAssets internally
            DefineOperations.ApplyToMany(profiles, _set, ApplyMode.Merge);

            _onApplied?.Invoke();
            _shouldClose = true;
        }

#else
        // ── Stub: allows compilation on Unity versions older than 6000.0 ──────────

        /// <summary>
        /// Not supported on this Unity version. Displays an error dialog.
        /// Build Profiles require Unity 6000.0 or newer.
        /// </summary>
        public static void Show(
            DefineSet                                           set,
            System.Collections.Generic.List<UnityEngine.Object> profiles,
            System.Action                                       onApplied = null)
        {
            EditorUtility.DisplayDialog(
                "Not Supported",
                "Build Profiles require Unity 6000.0 or newer.",
                "OK");
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Build Profiles require Unity 6000.0 or newer.",
                MessageType.Error);
        }
#endif
    }
}
