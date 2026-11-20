using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

#if UNITY_6000_0_OR_NEWER
using UnityEditor.Build.Profile;
using UnityEditor.Compilation;
#endif

namespace ProfileDefinesManager
{
    /// <summary>
    /// Main editor window for Profile Defines Manager Lite.
    /// Manage Define Sets and apply them to Unity 6 Build Profiles.
    /// Open via <b>Tools → Profile Defines Manager Lite</b>.
    /// </summary>
    public sealed class DefinesManagerWindow : EditorWindow
    {
        // ─── Menu ─────────────────────────────────────────────────────────────────

        /// <summary>Opens (or focuses) the Profile Defines Manager Lite window.</summary>
        [MenuItem("Tools/Profile Defines Manager Lite")]
        public static void ShowWindow()
        {
            var window = GetWindow<DefinesManagerWindow>();
            window.titleContent = new GUIContent("Profile Defines Manager Lite");
            window.minSize = new Vector2(640f, 420f);
            window.Show();
        }

        // ─── Define-set state ─────────────────────────────────────────────────────

        private List<DefineSet> _sets             = new List<DefineSet>();
        private int             _selectedSetIndex = -1;
        private int             _renamingSetIndex = -1;
        private string          _renameBuffer     = string.Empty;
        private string          _newDefineBuffer  = string.Empty;
        private Vector2         _leftScrollPos;
        private bool            _isCreatingNew    = false;
        private string          _newSetName       = string.Empty;

        // ─── Profile state (Unity 6 only) ─────────────────────────────────────────

#if UNITY_6000_0_OR_NEWER
        // Selection is keyed by profile reference, not list index: RefreshProfiles()
        // re-sorts the list, so indices shift when profiles are added or removed.
        private List<BuildProfile>             _profiles         = new List<BuildProfile>();
        private readonly HashSet<BuildProfile> _checkedProfiles  = new HashSet<BuildProfile>();
        private readonly HashSet<BuildProfile> _expandedProfiles = new HashSet<BuildProfile>();
        private BuildProfile                   _focusedProfile;
        private Vector2                        _rightScrollPos;
#endif

        // ─── Shared state ─────────────────────────────────────────────────────────

        private string _statusMessage = string.Empty;

        // ─── Unity messages ───────────────────────────────────────────────────────

        private void OnEnable()
        {
            RefreshSets();
#if UNITY_6000_0_OR_NEWER
            RefreshProfiles();
#endif
        }

        private void OnFocus()
        {
            // Re-scan whenever the window regains focus in case assets changed externally.
            RefreshSets();
#if UNITY_6000_0_OR_NEWER
            RefreshProfiles();
#endif
        }

        private void OnGUI()
        {
            // Two-panel body (grows to fill all space above the bottom bar)
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));

            // ── Left ────────────────────────────────────────────────────────────
            EditorGUILayout.BeginVertical(GUILayout.Width(Styles.LeftPanelWidth));
            DrawLeftPanel();
            EditorGUILayout.EndVertical();

            // ── Vertical divider ─────────────────────────────────────────────────
            var divRect = EditorGUILayout.GetControlRect(false, GUILayout.Width(1f), GUILayout.ExpandHeight(true));
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(divRect, Styles.SeparatorColor);

            // ── Right ────────────────────────────────────────────────────────────
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            DrawRightPanel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            // ── Bottom bar ───────────────────────────────────────────────────────
            Styles.DrawSeparator();
            DrawBottomBar();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // LEFT PANEL — Define Sets
        // ─────────────────────────────────────────────────────────────────────────

        private void DrawLeftPanel()
        {
            // Header row (always visible, outside the scroll view)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Define Sets", Styles.PanelHeader);
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(_isCreatingNew))
            {
                if (GUILayout.Button("+ New", Styles.ToolbarButton, GUILayout.Width(54f)))
                    CreateNewSet();
            }
            EditorGUILayout.EndHorizontal();

            Styles.DrawSeparator();

            // Scrollable body: set list + define editor for the selected set
            _leftScrollPos = EditorGUILayout.BeginScrollView(_leftScrollPos);
            DrawSetList();

            if (_selectedSetIndex >= 0 && _selectedSetIndex < _sets.Count)
            {
                GUILayout.Space(Styles.SectionSpacing);
                Styles.DrawSeparator();
                DrawSetEditor(_sets[_selectedSetIndex]);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSetList()
        {
            if (_sets.Count == 0 && !_isCreatingNew)
            {
                GUILayout.Space(8f);
                EditorGUILayout.LabelField("No define sets found.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            for (var i = 0; i < _sets.Count; i++)
                DrawSetRow(_sets[i], i);

            if (_isCreatingNew)
                DrawNewSetRow();
        }

        private void DrawNewSetRow()
        {
            EditorGUILayout.BeginHorizontal(GUIStyle.none, GUILayout.Height(Styles.RowHeight));

            GUI.SetNextControlName("NewSetNameField");
            _newSetName = EditorGUILayout.TextField(_newSetName, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.KeyDown
                && GUI.GetNameOfFocusedControl() == "NewSetNameField")
            {
                if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                { CommitNewSet(); Event.current.Use(); }
                else if (Event.current.keyCode == KeyCode.Escape)
                { CancelNewSet(); Event.current.Use(); }
            }

            if (GUILayout.Button("✓", Styles.ToolbarButton, GUILayout.Width(Styles.InlineButtonWidth)))
                CommitNewSet();
            if (GUILayout.Button("✗", Styles.ToolbarButton, GUILayout.Width(Styles.InlineButtonWidth)))
                CancelNewSet();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSetRow(DefineSet set, int index)
        {
            var isSelected = index == _selectedSetIndex;
            var isRenaming = index == _renamingSetIndex;

            var rowRect = EditorGUILayout.BeginHorizontal(
                isSelected ? Styles.SelectedRow : GUIStyle.none,
                GUILayout.Height(Styles.RowHeight));

            if (isRenaming)
            {
                GUI.SetNextControlName("RenameField_" + index);
                _renameBuffer = EditorGUILayout.TextField(_renameBuffer, GUILayout.ExpandWidth(true));

                // Keyboard shortcuts
                if (Event.current.type == EventType.KeyDown)
                {
                    if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                    { CommitRename(); Event.current.Use(); }
                    else if (Event.current.keyCode == KeyCode.Escape)
                    { CancelRename(); Event.current.Use(); }
                }

                if (GUILayout.Button("✓", Styles.ToolbarButton, GUILayout.Width(Styles.InlineButtonWidth)))
                    CommitRename();
                if (GUILayout.Button("✗", Styles.ToolbarButton, GUILayout.Width(Styles.InlineButtonWidth)))
                    CancelRename();
            }
            else
            {
                // Name label
                EditorGUILayout.LabelField(set.SetName, GUILayout.ExpandWidth(true));

                if (GUILayout.Button("✎", Styles.ToolbarButton, GUILayout.Width(Styles.InlineButtonWidth)))
                    BeginRename(index);
                if (GUILayout.Button("✕", Styles.ToolbarButton, GUILayout.Width(Styles.InlineButtonWidth)))
                    DeleteSet(index);

                // Click anywhere on the row to select it
                if (Event.current.type == EventType.MouseDown
                    && Event.current.button == 0
                    && rowRect.Contains(Event.current.mousePosition))
                {
                    _selectedSetIndex = index;
                    GUI.FocusControl(null);
                    Event.current.Use();
                    Repaint();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSetEditor(DefineSet set)
        {
            EditorGUILayout.LabelField($"Defines in \"{set.SetName}\"", Styles.SectionHeader);
            GUILayout.Space(2f);

            if (set.Defines.Count == 0)
                EditorGUILayout.LabelField("(empty — add a define below)", EditorStyles.centeredGreyMiniLabel);

            int removeAt = -1;

            for (var i = 0; i < set.Defines.Count; i++)
            {
                var issues = DefineValidator.ValidateSymbol(set.Defines[i]);

                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();
                var newValue = EditorGUILayout.TextField(
                    set.Defines[i], Styles.DefineTextField, GUILayout.ExpandWidth(true));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(set, "Edit Define Symbol");
                    set.Defines[i] = newValue;
                    DefineSetManager.Save(set);
                }

                if (GUILayout.Button("✕", Styles.ToolbarButton, GUILayout.Width(Styles.InlineButtonWidth)))
                    removeAt = i;

                EditorGUILayout.EndHorizontal();

                // Show the first validation issue inline
                if (issues.Count > 0)
                    EditorGUILayout.LabelField($"⚠  {issues[0].Description}", Styles.WarningLabel);
            }

            if (removeAt >= 0)
                RemoveDefine(set, removeAt);

            // ── Add define row ───────────────────────────────────────────────────
            GUILayout.Space(2f);
            EditorGUILayout.BeginHorizontal();

            GUI.SetNextControlName("NewDefineField");
            _newDefineBuffer = EditorGUILayout.TextField(
                _newDefineBuffer, Styles.DefineTextField, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("+ Add", Styles.ToolbarButton, GUILayout.Width(52f))
                && !string.IsNullOrWhiteSpace(_newDefineBuffer))
            {
                AddDefine(set, _newDefineBuffer);
            }

            EditorGUILayout.EndHorizontal();

            // Enter key in the "new define" field
            if (Event.current.type == EventType.KeyDown
                && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                && GUI.GetNameOfFocusedControl() == "NewDefineField"
                && !string.IsNullOrWhiteSpace(_newDefineBuffer))
            {
                AddDefine(set, _newDefineBuffer);
                Event.current.Use();
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // RIGHT PANEL — Build Profiles
        // ─────────────────────────────────────────────────────────────────────────

        private void DrawRightPanel()
        {
#if UNITY_6000_0_OR_NEWER
            // Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Build Profiles", Styles.PanelHeader);
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(!CanSwitchToFocusedProfile()))
            {
                if (GUILayout.Button(SwitchProfileContent, Styles.ToolbarButton))
                    SwitchToFocusedProfile();
            }
            if (GUILayout.Button("Refresh", Styles.ToolbarButton))
            {
                RefreshProfiles();
                SetStatus("Profile list refreshed.");
            }
            EditorGUILayout.EndHorizontal();

            Styles.DrawSeparator();

            // Profile list (scrollable, expands to fill)
            _rightScrollPos = EditorGUILayout.BeginScrollView(
                _rightScrollPos, GUILayout.ExpandHeight(true));

            if (_profiles.Count == 0)
            {
                GUILayout.Space(8f);
                EditorGUILayout.HelpBox(
                    "No Build Profiles found in the project.\n" +
                    "Create them via File → Build Profiles.",
                    MessageType.Info);
            }
            else
            {
                foreach (var profile in _profiles)
                    DrawProfileRow(profile);
            }

            EditorGUILayout.EndScrollView();

            // Focused-profile detail at the bottom (fixed, not scrolled)
            Styles.DrawSeparator();
            DrawEffectiveDefines();
#else
            GUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "Build Profiles require Unity 6000.0 or newer.",
                MessageType.Warning);
#endif
        }

#if UNITY_6000_0_OR_NEWER

        private void DrawProfileRow(BuildProfile profile)
        {
            var isChecked  = _checkedProfiles.Contains(profile);
            var isExpanded = _expandedProfiles.Contains(profile);
            var isFocused  = profile == _focusedProfile;
            var defines    = ProfileDiscovery.GetDefines(profile);
            var name       = ProfileDiscovery.GetProfileName(profile);

            var rowRect = EditorGUILayout.BeginHorizontal(
                isFocused ? Styles.SelectedRow : GUIStyle.none,
                GUILayout.Height(Styles.RowHeight));

            // Checkbox — marks this profile for the Apply action
            var nowChecked = EditorGUILayout.Toggle(isChecked, GUILayout.Width(18f));
            if (nowChecked != isChecked)
            {
                if (nowChecked) _checkedProfiles.Add(profile);
                else            _checkedProfiles.Remove(profile);
            }

            // Profile name
            EditorGUILayout.LabelField(name, GUILayout.ExpandWidth(true));

            if (profile == ProfileDiscovery.GetActiveProfile())
                EditorGUILayout.LabelField("Active", EditorStyles.miniBoldLabel, GUILayout.Width(40f));

            // Define count badge
            EditorGUILayout.LabelField(
                $"({defines.Length})",
                EditorStyles.centeredGreyMiniLabel,
                GUILayout.Width(36f));

            // Expand / collapse button — also focuses the profile
            if (GUILayout.Button(
                    isExpanded ? "▼" : "▶",
                    Styles.ToolbarButton,
                    GUILayout.Width(Styles.InlineButtonWidth)))
            {
                if (isExpanded) _expandedProfiles.Remove(profile);
                else            _expandedProfiles.Add(profile);
                _focusedProfile = profile;
                Repaint();
            }

            EditorGUILayout.EndHorizontal();

            // Clicking anywhere on the row focuses the profile for the Effective Defines section
            if (Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && rowRect.Contains(Event.current.mousePosition))
            {
                _focusedProfile = profile;
                GUI.FocusControl(null);
                Event.current.Use();
                Repaint();
            }

            // Inline read-only define list (shown when expanded)
            if (isExpanded)
            {
                EditorGUI.indentLevel++;

                if (defines.Length == 0)
                {
                    EditorGUILayout.LabelField("(no defines set on this profile)", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        foreach (var sym in defines)
                            EditorGUILayout.LabelField(sym, EditorStyles.miniLabel);
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        private void DrawEffectiveDefines()
        {
            if (_focusedProfile == null)
            {
                EditorGUILayout.LabelField(
                    "Click a profile row to inspect its defines.",
                    EditorStyles.centeredGreyMiniLabel);
                GUILayout.Space(Styles.SectionSpacing);
                return;
            }

            var profile = _focusedProfile;
            var name    = ProfileDiscovery.GetProfileName(profile);
            var defines = ProfileDiscovery.GetDefines(profile);

            EditorGUILayout.LabelField($"Profile Defines — {name}", Styles.SectionHeader);

            if (defines.Length == 0)
            {
                EditorGUILayout.LabelField("(none)", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    foreach (var sym in defines)
                        EditorGUILayout.LabelField(sym, EditorStyles.miniLabel);
                }
            }

            GUILayout.Space(2f);
            EditorGUILayout.LabelField(
                "Global PlayerSettings defines are applied additively by Unity at build time.",
                EditorStyles.miniLabel);
            GUILayout.Space(Styles.SectionSpacing);
        }

#endif

        // ─────────────────────────────────────────────────────────────────────────
        // BOTTOM BAR
        // ─────────────────────────────────────────────────────────────────────────

        private void DrawBottomBar()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(Styles.BottomBarHeight - 6f));

#if UNITY_6000_0_OR_NEWER
            var hasSet     = _selectedSetIndex >= 0 && _selectedSetIndex < _sets.Count;
            var hasTargets = _checkedProfiles.Count > 0;

            using (new EditorGUI.DisabledScope(!hasSet || !hasTargets))
            {
                if (GUILayout.Button(
                        $"Apply to Selected ({_checkedProfiles.Count})",
                        GUILayout.Width(170f)))
                    OpenDiffPreview();
            }

            GUILayout.Space(4f);

            using (new EditorGUI.DisabledScope(!CanApplyActiveProfileChanges()))
            {
                if (GUILayout.Button(ApplyChangesContent, GUILayout.Width(100f)))
                    ApplyActiveProfileChanges();
            }

            GUILayout.Space(4f);
#endif

            GUILayout.FlexibleSpace();

            // Status label (rightmost)
            EditorGUILayout.LabelField(_statusMessage, Styles.StatusLabel, GUILayout.ExpandWidth(true));

            EditorGUILayout.EndHorizontal();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // DATA REFRESH
        // ─────────────────────────────────────────────────────────────────────────

        private void RefreshSets()
        {
            // GetAll() re-sorts by name, so re-resolve the selected and renaming sets
            // by reference; otherwise a set added elsewhere shifts the selection.
            var selected = _selectedSetIndex >= 0 && _selectedSetIndex < _sets.Count ? _sets[_selectedSetIndex] : null;
            var renaming = _renamingSetIndex >= 0 && _renamingSetIndex < _sets.Count ? _sets[_renamingSetIndex] : null;

            _sets = DefineSetManager.GetAll();
            _selectedSetIndex = selected != null ? _sets.IndexOf(selected) : -1;
            _renamingSetIndex = renaming != null ? _sets.IndexOf(renaming) : -1;
        }

#if UNITY_6000_0_OR_NEWER
        private void RefreshProfiles()
        {
            _profiles = ProfileDiscovery.FindAllProfiles();

            // Drop selections for profiles that were deleted or are no longer found.
            _checkedProfiles.RemoveWhere(p => p == null || !_profiles.Contains(p));
            _expandedProfiles.RemoveWhere(p => p == null || !_profiles.Contains(p));
            if (_focusedProfile != null && !_profiles.Contains(_focusedProfile))
                _focusedProfile = null;
        }
#endif

        // ─────────────────────────────────────────────────────────────────────────
        // DEFINE SET ACTIONS
        // ─────────────────────────────────────────────────────────────────────────

        private void CreateNewSet()
        {
            _isCreatingNew = true;
            _newSetName    = string.Empty;
            GUI.FocusControl("NewSetNameField");
            Repaint();
        }

        private void CommitNewSet()
        {
            var trimmed = _newSetName.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                CancelNewSet();
                return;
            }

            if (DefineSetManager.FindByName(trimmed) != null)
            {
                EditorUtility.DisplayDialog(
                    "Name Already Exists",
                    $"A define set named \"{trimmed}\" already exists.\nPlease choose a different name.",
                    "OK");
                return;
            }

            var newSet = DefineSetManager.Create(trimmed);
            _isCreatingNew = false;
            _newSetName    = string.Empty;
            RefreshSets();
            _selectedSetIndex = _sets.FindIndex(s => s == newSet);
            SetStatus($"Created \"{trimmed}\".");
            Repaint();
        }

        private void CancelNewSet()
        {
            _isCreatingNew = false;
            _newSetName    = string.Empty;
            Repaint();
        }

        private void DeleteSet(int index)
        {
            if (index < 0 || index >= _sets.Count) return;
            var set = _sets[index];

            if (!EditorUtility.DisplayDialog(
                    "Delete Define Set",
                    $"Delete \"{set.SetName}\"?\nThis cannot be undone.",
                    "Delete", "Cancel"))
                return;

            var deletedName = set.SetName;
            DefineSetManager.Delete(set);
            SetStatus($"Deleted \"{deletedName}\".");
            RefreshSets();
            if (_selectedSetIndex >= _sets.Count)
                _selectedSetIndex = _sets.Count - 1;
            Repaint();
        }

        private void BeginRename(int index)
        {
            _renamingSetIndex = index;
            _renameBuffer     = _sets[index].SetName;
            GUI.FocusControl("RenameField_" + index);
        }

        private void CommitRename()
        {
            if (_renamingSetIndex < 0 || _renamingSetIndex >= _sets.Count)
            { CancelRename(); return; }

            var trimmed = _renameBuffer.Trim();
            var set     = _sets[_renamingSetIndex];
            if (!string.IsNullOrEmpty(trimmed) && trimmed != set.SetName)
            {
                var error = DefineSetManager.Rename(set, trimmed);
                if (error != null)
                {
                    EditorUtility.DisplayDialog("Rename Failed", error, "OK");
                    // Keep rename mode open so the user can correct the name.
                    return;
                }

                SetStatus($"Renamed to \"{trimmed}\".");
            }

            _renamingSetIndex = -1;
            RefreshSets();
            Repaint();
        }

        private void CancelRename()
        {
            _renamingSetIndex = -1;
            Repaint();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // DEFINE SYMBOL ACTIONS
        // ─────────────────────────────────────────────────────────────────────────

        private void AddDefine(DefineSet set, string symbol)
        {
            var trimmed = symbol.Trim();
            if (string.IsNullOrEmpty(trimmed)) return;

            Undo.RecordObject(set, "Add Define Symbol");
            set.Defines.Add(trimmed);
            _newDefineBuffer = string.Empty;
            DefineSetManager.Save(set);
            SetStatus($"Added \"{trimmed}\".");
            Repaint();
        }

        private void RemoveDefine(DefineSet set, int defineIndex)
        {
            if (defineIndex < 0 || defineIndex >= set.Defines.Count) return;

            var sym = set.Defines[defineIndex];
            Undo.RecordObject(set, "Remove Define Symbol");
            set.Defines.RemoveAt(defineIndex);
            DefineSetManager.Save(set);
            SetStatus($"Removed \"{sym}\".");
            Repaint();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // PROFILE ACTIONS (Unity 6 only)
        // ─────────────────────────────────────────────────────────────────────────

#if UNITY_6000_0_OR_NEWER

        private void OpenDiffPreview()
        {
            if (_selectedSetIndex < 0 || _selectedSetIndex >= _sets.Count) return;

            var set     = _sets[_selectedSetIndex];
            // Keep list order so the diff popup matches the profile panel.
            var targets = _profiles.Where(_checkedProfiles.Contains).ToList();

            if (targets.Count == 0) return;

            DiffPreviewPopup.Show(set, targets, () =>
            {
                RefreshProfiles();
                SetStatus($"Applied \"{set.SetName}\" to {targets.Count} profile(s).");
                Repaint();
            });
        }

        // ─── Active profile ───────────────────────────────────────────────────────

        private static readonly GUIContent SwitchProfileContent = new GUIContent(
            "Switch Profile",
            "Make the highlighted profile the active Build Profile " +
            "(same as Switch Profile in File > Build Profiles).");

        private static readonly GUIContent ApplyChangesContent = new GUIContent(
            "Apply Changes",
            "Recompile scripts so the active profile's current defines take effect " +
            "(same as Apply in File > Build Profiles). Highlight the active profile to enable.");

        // Switching or recompiling during compilation or Play mode is not allowed by Unity.
        private static bool EditorIsBusy =>
            EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode;

        private bool CanSwitchToFocusedProfile() =>
            _focusedProfile != null
            && _focusedProfile != ProfileDiscovery.GetActiveProfile()
            && !EditorIsBusy;

        private bool CanApplyActiveProfileChanges() =>
            _focusedProfile != null
            && _focusedProfile == ProfileDiscovery.GetActiveProfile()
            && !EditorIsBusy;

        private void SwitchToFocusedProfile()
        {
            var profile = _focusedProfile;
            if (profile == null) return;

            var name = ProfileDiscovery.GetProfileName(profile);

            // Switching can change the platform and trigger a reimport and recompile,
            // so run it outside OnGUI.
            EditorApplication.delayCall += () =>
            {
                try
                {
                    BuildProfile.SetActiveBuildProfile(profile);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                var switched = ProfileDiscovery.GetActiveProfile() == profile;
                if (!switched)
                {
                    EditorUtility.DisplayDialog(
                        "Switch Profile",
                        $"Could not make \"{name}\" the active Build Profile. Check that its platform " +
                        "module is installed, and see the Console for details.",
                        "OK");
                }

                // The window may have been closed before the delayed call ran.
                if (this == null) return;
                SetStatus(switched ? $"Switched to \"{name}\"." : $"Could not switch to \"{name}\".");
                Repaint();
            };
        }

        private void ApplyActiveProfileChanges()
        {
            var profile = _focusedProfile;
            if (profile == null || profile != ProfileDiscovery.GetActiveProfile()) return;

            // Some Unity 6 versions do not recompile when the active profile's defines are
            // changed from script, so request it explicitly. Scripts compile with the
            // active profile's current defines.
            CompilationPipeline.RequestScriptCompilation();
            SetStatus($"Recompiling with the defines of \"{ProfileDiscovery.GetProfileName(profile)}\"...");
        }

#endif

        // ─────────────────────────────────────────────────────────────────────────
        // UTILITIES
        // ─────────────────────────────────────────────────────────────────────────

        private void SetStatus(string message) => _statusMessage = message;
    }
}
