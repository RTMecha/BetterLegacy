using BetterLegacy.Core.Data;
using BetterLegacy.Editor.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Components
{
    /// <summary>
    /// Updates Multi Object editor info.
    /// </summary>
    public class UpdateMultiObjectInfo : MonoBehaviour
    {
        /// <summary>
        /// Text to update.
        /// </summary>
        public Text Text { get; set; }

        /// <summary>
        /// String to format from.
        /// </summary>
        public const string DEFAULT_TEXT = "You are currently editing multiple objects.\n\nObject Count: {0}/{3}\nPrefab Object Count: {1}/{4}\nTotal: {2}";

        void Update()
        {
            if (!Text || !Text.isActiveAndEnabled)
                return;

            Text.text = string.Format(DEFAULT_TEXT,
                ObjectEditor.inst.SelectedObjects.Count,
                ObjectEditor.inst.SelectedPrefabObjects.Count,
                ObjectEditor.inst.SelectedObjects.Count + ObjectEditor.inst.SelectedPrefabObjects.Count,
                GameData.Current.beatmapObjects.Count,
                GameData.Current.prefabObjects.Count);
        }
    }
}