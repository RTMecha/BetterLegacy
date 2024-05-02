using BetterLegacy.Editor.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Components.Editor
{
    public class UpdateMultiObjectInfo : MonoBehaviour
    {
        public Text Text { get; set; }

        public string DefaultText => "You are currently editing multiple objects.\n\nObject Count: {0}/{3}\nPrefab Object Count: {1}/{4}\nTotal: {2}";

        void Update()
        {
            if (!Text || !Text.isActiveAndEnabled)
                return;

            Text.text = string.Format(DefaultText,
                ObjectEditor.inst.SelectedObjects.Count,
                ObjectEditor.inst.SelectedPrefabObjects.Count,
                ObjectEditor.inst.SelectedObjects.Count + ObjectEditor.inst.SelectedPrefabObjects.Count,
                DataManager.inst.gameData.beatmapObjects.Count,
                DataManager.inst.gameData.prefabObjects.Count);
        }
    }
}