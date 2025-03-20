using UnityEngine;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Components.Player
{
    /// <summary>
    /// Component for selecting a player in the editor.
    /// </summary>
    public class PlayerSelector : MonoBehaviour
    {
        void Awake()
        {
            if (!CoreHelper.InEditor)
                Destroy(this);
        }

        void OnMouseDown()
        {
            PlayerEditor.inst.Dialog.Open();
            StartCoroutine(PlayerEditor.inst.RefreshEditor());
            AchievementManager.inst.UnlockAchievement("select_player");
        }
    }
}
