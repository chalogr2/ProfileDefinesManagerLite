using System.Collections.Generic;
using UnityEngine;

namespace ProfileDefinesManager
{
    /// <summary>
    /// A named collection of scripting define symbols stored as a ScriptableObject asset.
    /// Create instances via <see cref="DefineSetManager.Create"/>.
    /// </summary>
    public class DefineSet : ScriptableObject
    {
        [SerializeField] private string _setName = string.Empty;
        [SerializeField] private List<string> _defines = new List<string>();

        /// <summary>
        /// Gets or sets the display name of this define set.
        /// </summary>
        public string SetName
        {
            get => _setName;
            set => _setName = value;
        }

        /// <summary>
        /// Gets the list of scripting define symbols in this set.
        /// Modify this list directly, then call <see cref="DefineSetManager.Save"/> to persist changes.
        /// </summary>
        public List<string> Defines => _defines;
    }
}
