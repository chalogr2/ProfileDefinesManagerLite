using System.Collections.Generic;
using System.IO;
using UnityEditor;

#if UNITY_6000_0_OR_NEWER
using UnityEditor.Build.Profile;
#endif

namespace ProfileDefinesManager
{
    /// <summary>
    /// Discovers Unity 6 Build Profile assets in the project and reads their scripting define symbols.
    /// All methods that reference <c>BuildProfile</c> are compiled only on Unity 6000.0 or newer.
    /// </summary>
    public static class ProfileDiscovery
    {
#if UNITY_6000_0_OR_NEWER

        /// <summary>
        /// Finds and loads all Build Profile assets in the current project.
        /// </summary>
        /// <returns>
        /// A list of every <see cref="BuildProfile"/> asset found in the project,
        /// sorted by asset file name.
        /// </returns>
        public static List<BuildProfile> FindAllProfiles()
        {
            var results = new List<BuildProfile>();
            var guids = AssetDatabase.FindAssets("t:BuildProfile");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(path);
                if (profile != null)
                    results.Add(profile);
            }

            results.Sort((a, b) =>
                string.Compare(
                    GetProfileName(a),
                    GetProfileName(b),
                    System.StringComparison.OrdinalIgnoreCase));

            return results;
        }

        /// <summary>
        /// Gets the scripting define symbols currently set on the given Build Profile.
        /// </summary>
        /// <param name="profile">The Build Profile to query.</param>
        /// <returns>
        /// An array of define symbol strings. Returns an empty array when
        /// <paramref name="profile"/> is null or has no defines set.
        /// </returns>
        public static string[] GetDefines(BuildProfile profile)
        {
            if (profile == null)
                return System.Array.Empty<string>();

            return profile.scriptingDefines ?? System.Array.Empty<string>();
        }

        /// <summary>
        /// Gets the display name of a Build Profile derived from its asset file name.
        /// </summary>
        /// <param name="profile">The Build Profile to query.</param>
        /// <returns>The asset file name without extension, or an empty string if the profile is null.</returns>
        public static string GetProfileName(BuildProfile profile)
        {
            if (profile == null)
                return string.Empty;

            var path = AssetDatabase.GetAssetPath(profile);
            return string.IsNullOrEmpty(path) ? string.Empty : Path.GetFileNameWithoutExtension(path);
        }

        /// <summary>
        /// Gets the asset path of a Build Profile.
        /// </summary>
        /// <param name="profile">The Build Profile to query.</param>
        /// <returns>The project-relative asset path, or an empty string if the profile is null.</returns>
        public static string GetProfilePath(BuildProfile profile)
        {
            if (profile == null)
                return string.Empty;

            return AssetDatabase.GetAssetPath(profile) ?? string.Empty;
        }

        /// <summary>
        /// Returns the currently active Build Profile, if any.
        /// </summary>
        /// <returns>
        /// The active <see cref="BuildProfile"/>, or <c>null</c> if no custom profile is active
        /// (platform profiles always return null from <c>BuildProfile.GetActiveBuildProfile()</c>).
        /// </returns>
        public static BuildProfile GetActiveProfile()
        {
            return BuildProfile.GetActiveBuildProfile();
        }

#else
        // ── Stub shown when compiling on Unity versions older than 6000.0 ──────────

        /// <summary>
        /// Not supported on this Unity version. Logs an error and returns an empty list.
        /// Build Profiles require Unity 6000.0 or newer.
        /// </summary>
        public static System.Collections.Generic.List<UnityEngine.Object> FindAllProfiles()
        {
            UnityEngine.Debug.LogError(
                "[Profile Defines Manager] Build Profiles are only available in Unity 6000.0 or newer. " +
                "Please upgrade your Unity installation.");
            return new System.Collections.Generic.List<UnityEngine.Object>();
        }
#endif
    }
}
