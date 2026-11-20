using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ProfileDefinesManager
{
    /// <summary>
    /// Handles creation, retrieval, renaming, deletion, and persistence of <see cref="DefineSet"/> assets.
    /// </summary>
    public static class DefineSetManager
    {
        /// <summary>
        /// The default project folder where new <see cref="DefineSet"/> assets are saved.
        /// Kept outside the product folder so removing or upgrading the package never
        /// deletes the user's sets. Existing sets are found anywhere under Assets.
        /// </summary>
        public const string DefaultFolder = "Assets/Settings/DefineSets";

        // ─── Retrieval ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns all <see cref="DefineSet"/> assets found in the project, sorted by name.
        /// </summary>
        public static List<DefineSet> GetAll()
        {
            var results = new List<DefineSet>();
            var guids = AssetDatabase.FindAssets("t:DefineSet");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<DefineSet>(path);
                if (asset != null)
                    results.Add(asset);
            }

            results.Sort((a, b) => string.Compare(a.SetName, b.SetName, System.StringComparison.OrdinalIgnoreCase));
            return results;
        }

        /// <summary>
        /// Finds a <see cref="DefineSet"/> by its display name (case-insensitive).
        /// </summary>
        /// <param name="name">The name to search for.</param>
        /// <returns>The first matching <see cref="DefineSet"/>, or null if none found.</returns>
        public static DefineSet FindByName(string name)
        {
            foreach (var set in GetAll())
            {
                if (string.Equals(set.SetName, name, System.StringComparison.OrdinalIgnoreCase))
                    return set;
            }

            return null;
        }

        // ─── Create / Delete ─────────────────────────────────────────────────────

        /// <summary>
        /// Creates and saves a new <see cref="DefineSet"/> asset with the given name.
        /// The asset is placed in <see cref="DefaultFolder"/>.
        /// </summary>
        /// <param name="name">The display name for the new define set.</param>
        /// <returns>The newly created <see cref="DefineSet"/>.</returns>
        public static DefineSet Create(string name)
        {
            EnsureFolderExists(DefaultFolder);

            var fileName = SanitizeFileName(name);
            var path = AssetDatabase.GenerateUniqueAssetPath($"{DefaultFolder}/{fileName}.asset");

            var asset = ScriptableObject.CreateInstance<DefineSet>();
            asset.SetName = name;

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            return asset;
        }

        /// <summary>
        /// Creates a deep copy of an existing <see cref="DefineSet"/> with a new name.
        /// </summary>
        /// <param name="source">The define set to duplicate.</param>
        /// <param name="newName">The display name for the copy.</param>
        /// <returns>The newly created duplicate <see cref="DefineSet"/>.</returns>
        public static DefineSet Duplicate(DefineSet source, string newName)
        {
            var copy = Create(newName);
            copy.Defines.AddRange(source.Defines);
            Save(copy);
            return copy;
        }

        /// <summary>
        /// Deletes the given <see cref="DefineSet"/> asset from disk.
        /// This action cannot be undone.
        /// </summary>
        /// <param name="set">The define set to delete.</param>
        public static void Delete(DefineSet set)
        {
            if (set == null)
                return;

            var path = AssetDatabase.GetAssetPath(set);
            if (!string.IsNullOrEmpty(path))
                AssetDatabase.DeleteAsset(path);
        }

        // ─── Save / Rename ───────────────────────────────────────────────────────

        /// <summary>
        /// Marks the given <see cref="DefineSet"/> as dirty and saves all modified assets to disk.
        /// </summary>
        /// <param name="set">The define set to persist.</param>
        public static void Save(DefineSet set)
        {
            if (set == null)
                return;

            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Renames a <see cref="DefineSet"/>, updating both its display name and its asset file name
        /// (sanitized via <see cref="SanitizeFileName"/>). Records an Undo entry for the property change.
        /// Nothing is changed if the rename fails.
        /// </summary>
        /// <param name="set">The define set to rename.</param>
        /// <param name="newName">The new display name. Leading and trailing whitespace is trimmed.</param>
        /// <returns><c>null</c> on success, otherwise an error message describing why the rename failed.</returns>
        public static string Rename(DefineSet set, string newName)
        {
            if (set == null)
                return "No define set to rename.";
            if (string.IsNullOrWhiteSpace(newName))
                return "The name cannot be empty.";

            newName = newName.Trim();

            var existing = FindByName(newName);
            if (existing != null && existing != set)
                return $"A define set named \"{newName}\" already exists.";

            var path       = AssetDatabase.GetAssetPath(set);
            var fileName   = SanitizeFileName(newName);
            var renameFile = !string.IsNullOrEmpty(path)
                             && Path.GetFileNameWithoutExtension(path) != fileName;

            if (renameFile)
            {
                var newPath = $"{Path.GetDirectoryName(path).Replace('\\', '/')}/{fileName}{Path.GetExtension(path)}";
                var error   = AssetDatabase.ValidateMoveAsset(path, newPath);
                if (!string.IsNullOrEmpty(error))
                    return error;
            }

            // Flush the new name to disk BEFORE renaming the file. If RenameAsset triggers
            // a reimport, Unity then finds the new name on disk instead of reverting it.
            var oldName = set.SetName;
            Undo.RecordObject(set, "Rename Define Set");
            set.SetName = newName;
            Save(set);

            if (renameFile)
            {
                var error = AssetDatabase.RenameAsset(path, fileName);
                if (!string.IsNullOrEmpty(error))
                {
                    // Keep the display name and the file name in sync.
                    set.SetName = oldName;
                    Save(set);
                    return error;
                }
            }

            return null;
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Ensures that every segment of the given asset-relative folder path exists,
        /// creating any missing folders via <see cref="AssetDatabase.CreateFolder"/>.
        /// </summary>
        /// <param name="folderPath">An asset-relative path such as <c>"Assets/Foo/Bar"</c>.</param>
        public static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            var parts = folderPath.Split('/');
            var parent = parts[0]; // "Assets"

            for (var i = 1; i < parts.Length; i++)
            {
                var fullPath = $"{parent}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(fullPath))
                    AssetDatabase.CreateFolder(parent, parts[i]);

                parent = fullPath;
            }
        }

        /// <summary>
        /// Replaces characters that are not valid in file names with underscores.
        /// Returns <c>"DefineSet"</c> for an empty name.
        /// </summary>
        /// <param name="name">The display name to convert.</param>
        public static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);

            foreach (var c in name)
                sb.Append(System.Array.IndexOf(invalid, c) < 0 ? c : '_');

            return sb.Length > 0 ? sb.ToString() : "DefineSet";
        }
    }
}
