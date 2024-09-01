using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Managers;
using UnityEngine;

namespace BetterLegacy.Components.Player
{
    /// <summary>
    /// Component for selecting a player in the editor.
    /// </summary>
    public class PlayerSelector : MonoBehaviour
    {
        public int id;

        void Awake()
        {
            if (!CoreHelper.InEditor)
                Destroy(this);
        }

        void OnMouseDown()
        {
            EditorManager.inst.ShowDialog("Player Editor");
            StartCoroutine(PlayerEditor.inst.RefreshEditor());
        }
    }
}
