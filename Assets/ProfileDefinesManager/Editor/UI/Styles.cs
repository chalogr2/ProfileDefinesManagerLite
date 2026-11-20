using UnityEditor;
using UnityEngine;

namespace ProfileDefinesManager
{
    /// <summary>
    /// Shared GUI styles, colors, and layout constants for Build Profile Defines Pro windows.
    /// All styles are lazily initialized on first access.
    /// </summary>
    public static class Styles
    {
        // ─── Colors ───────────────────────────────────────────────────────────────

        /// <summary>Color used for symbols that will be added (diff "add" entries).</summary>
        public static readonly Color AddColor = new Color(0.27f, 0.75f, 0.35f); // green

        /// <summary>Color used for symbols that will be removed (diff "remove" entries).</summary>
        public static readonly Color RemoveColor = new Color(0.90f, 0.33f, 0.33f); // red

        /// <summary>Color used for symbols that remain unchanged in a diff.</summary>
        public static readonly Color UnchangedColor = new Color(0.55f, 0.55f, 0.55f); // grey

        /// <summary>Color used for validation warning labels.</summary>
        public static readonly Color WarningColor = new Color(0.95f, 0.75f, 0.20f); // amber

        /// <summary>Background tint for the selected item in a list.</summary>
        public static readonly Color SelectionColor = new Color(0.24f, 0.49f, 0.91f, 0.30f);

        /// <summary>Separator line color.</summary>
        public static readonly Color SeparatorColor = new Color(0.15f, 0.15f, 0.15f, 1f);

        // ─── Layout constants ─────────────────────────────────────────────────────

        /// <summary>Width of the left "Define Sets" panel in pixels.</summary>
        public const float LeftPanelWidth = 260f;

        /// <summary>Height of the bottom action bar in pixels.</summary>
        public const float BottomBarHeight = 52f;

        /// <summary>Height of a single list row in pixels.</summary>
        public const float RowHeight = 22f;

        /// <summary>Width of the small inline action buttons ("X", "✎") inside list rows.</summary>
        public const float InlineButtonWidth = 22f;

        /// <summary>Standard spacing between major sections.</summary>
        public const float SectionSpacing = 6f;

        // ─── Lazy style backing fields ────────────────────────────────────────────

        private static GUIStyle _panelHeader;
        private static GUIStyle _sectionHeader;
        private static GUIStyle _diffAdd;
        private static GUIStyle _diffRemove;
        private static GUIStyle _diffUnchanged;
        private static GUIStyle _warningLabel;
        private static GUIStyle _selectedRow;
        private static GUIStyle _toolbarButton;
        private static GUIStyle _statusLabel;
        private static GUIStyle _defineTextField;

        // ─── Styles ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Bold label style used for panel top-level headers (e.g. "Define Sets", "Build Profiles").
        /// </summary>
        public static GUIStyle PanelHeader => _panelHeader ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            margin = new RectOffset(4, 4, 6, 4),
        };

        /// <summary>
        /// Smaller bold label for sub-sections within a panel.
        /// </summary>
        public static GUIStyle SectionHeader => _sectionHeader ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 11,
            margin = new RectOffset(4, 4, 4, 2),
        };

        /// <summary>
        /// Colored label for diff entries that will be <b>added</b> (green prefix "+ ").
        /// </summary>
        public static GUIStyle DiffAdd => _diffAdd ??= BuildColoredLabel(AddColor);

        /// <summary>
        /// Colored label for diff entries that will be <b>removed</b> (red prefix "- ").
        /// </summary>
        public static GUIStyle DiffRemove => _diffRemove ??= BuildColoredLabel(RemoveColor);

        /// <summary>
        /// Colored label for diff entries that are <b>unchanged</b> (grey prefix "= ").
        /// </summary>
        public static GUIStyle DiffUnchanged => _diffUnchanged ??= BuildColoredLabel(UnchangedColor);

        /// <summary>
        /// Label style for inline validation warnings (amber text).
        /// </summary>
        public static GUIStyle WarningLabel => _warningLabel ??= BuildColoredLabel(WarningColor);

        /// <summary>
        /// Background style applied to the selected/highlighted list row.
        /// </summary>
        public static GUIStyle SelectedRow
        {
            get
            {
                if (_selectedRow != null) return _selectedRow;

                var tex = MakeSolidTexture(SelectionColor);
                _selectedRow = new GUIStyle(GUIStyle.none)
                {
                    normal = { background = tex },
                    hover  = { background = tex },
                };
                return _selectedRow;
            }
        }

        /// <summary>
        /// Compact toolbar-style button (no extra margin).
        /// </summary>
        public static GUIStyle ToolbarButton => _toolbarButton ??= new GUIStyle(EditorStyles.miniButton)
        {
            margin  = new RectOffset(2, 2, 2, 2),
            padding = new RectOffset(4, 4, 2, 2),
        };

        /// <summary>
        /// Small italic label used for the status bar at the bottom of the main window.
        /// </summary>
        public static GUIStyle StatusLabel => _statusLabel ??= new GUIStyle(EditorStyles.miniLabel)
        {
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleLeft,
            margin    = new RectOffset(6, 6, 4, 4),
        };

        /// <summary>
        /// Text field style for individual define symbol rows (compact height).
        /// </summary>
        public static GUIStyle DefineTextField => _defineTextField ??= new GUIStyle(EditorStyles.textField)
        {
            margin  = new RectOffset(2, 2, 1, 1),
            padding = new RectOffset(4, 4, 2, 2),
        };

        // ─── Utility drawing helpers ──────────────────────────────────────────────

        /// <summary>
        /// Draws a 1-pixel horizontal separator line using <see cref="SeparatorColor"/>.
        /// </summary>
        public static void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, SeparatorColor);
            GUILayout.Space(2f);
        }

        /// <summary>
        /// Draws a filled background rectangle for the given <paramref name="rect"/> using
        /// the editor's default dark/light skin alternating row color.
        /// </summary>
        /// <param name="rect">The screen rect to fill.</param>
        /// <param name="index">Row index — even/odd determines the shade.</param>
        public static void DrawAlternatingRow(Rect rect, int index)
        {
            if (Event.current.type != EventType.Repaint) return;
            var color = index % 2 == 0
                ? new Color(0f, 0f, 0f, 0.05f)
                : new Color(0f, 0f, 0f, 0.00f);
            EditorGUI.DrawRect(rect, color);
        }

        // ─── Private helpers ──────────────────────────────────────────────────────

        private static GUIStyle BuildColoredLabel(Color color)
        {
            var style = new GUIStyle(EditorStyles.label)
            {
                richText  = false,
                wordWrap  = false,
                margin    = new RectOffset(2, 2, 1, 1),
                padding   = new RectOffset(4, 4, 1, 1),
            };
            style.normal.textColor = color;
            style.hover.textColor  = color;
            return style;
        }

        private static Texture2D MakeSolidTexture(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.DontSave,
            };
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}
