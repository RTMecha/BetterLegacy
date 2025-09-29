﻿using UnityEngine;
using UnityEngine.EventSystems;

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

        bool dragging;

        void Awake()
        {
            if (!CoreHelper.InEditor)
                Destroy(this);
        }

        void OnMouseDown()
        {
            if (!CoreHelper.IsEditing || EventSystem.current.IsPointerOverGameObject() || dragging)
                return;

            //    EditorContextMenu.inst.ShowContextMenu(
            //        new ButtonFunction("Respawn", () =>
            //        {
            //            if (player)
            //                PlayerManager.RespawnPlayer(player.playerIndex);
            //        }),
            //        new ButtonFunction("Hide", () =>
            //        {
            //            EventsConfig.Instance.ShowGUI.Value = false;
            //            EditorManager.inst.DisplayNotification($"Hidden players and GUI. If you want to un-hide, press {EventsConfig.Instance.ShowGUIToggle.Value}.", 4f, EditorManager.NotificationType.Success);
            //        }));
            //    return;

            PlayerEditor.inst.Dialog.Open();
            StartCoroutine(PlayerEditor.inst.RefreshEditor());
            AchievementManager.inst.UnlockAchievement("select_player");
        }

        void OnMouseDrag()
        {
            if (!player || !Input.GetMouseButton((int)PointerEventData.InputButton.Left))
                return;

            dragging = true;
            player.rb.position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        void OnMouseUp() => dragging = false;

        void OnMouseEnter()
        {
            if (!player)
                return;

            EditorManager.inst.SetTooltip(null, $"<#{LSColors.ColorToHex(CoreHelper.CurrentBeatmapTheme.GetPlayerColor(player.playerIndex))}>Player {player.playerIndex + 1}</color>",
                $"<b>Health</b>: <#4B86B4>{player.Core?.Health ?? 3}</color>\n" +
                $"<b>Damage Colliding</b>: <#4B86B4>{player.triggerColliding}</color>\n" +
                $"<b>Solid Colliding</b>: <#4B86B4>{player.colliding}</color>\n" +
                $"<b>Position</b>: <#4B86B4>{player.rb.position}</color>\n" +
                $"<b>Velocity</b>: <#4B86B4>{player.rb.velocity}</color>\n");
        }
    }
}
