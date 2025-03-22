using UnityEngine;

using LSFunctions;

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
        public RTPlayer player;

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

        void OnMouseEnter()
        {
            if (!player)
                return;

            EditorManager.inst.SetTooltip(null, $"<#{LSColors.ColorToHex(CoreHelper.CurrentBeatmapTheme.GetPlayerColor(player.playerIndex))}>Player {player.playerIndex + 1}</color>",
                $"<b>Health</b>: <#4B86B4>{player.CustomPlayer?.Health ?? 3}</color>\n" +
                $"<b>Damage Colliding</b>: <#4B86B4>{player.triggerColliding}</color>\n" +
                $"<b>Solid Colliding</b>: <#4B86B4>{player.colliding}</color>\n" +
                $"<b>Position</b>: <#4B86B4>{player.rb.position}</color>\n" +
                $"<b>Velocity</b>: <#4B86B4>{player.rb.velocity}</color>\n");
        }
    }
}
