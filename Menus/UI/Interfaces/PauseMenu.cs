using BetterLegacy.Components;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using TMPro;
using BetterLegacy.Core.Data;
using LSFunctions;
using System.IO;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Configs;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Layouts;

namespace BetterLegacy.Menus.UI.Interfaces
{
    public class PauseMenu : MenuBase
    {
        public static PauseMenu Current { get; set; }

        public PauseMenu() : base(false)
        {
            if (!CoreHelper.InGame || CoreHelper.InEditor)
            {
                CoreHelper.LogError($"Cannot pause outside of the game!");
                return;
            }

            elements.Add(new MenuText
            {
                text = "Paused",
            });

            CoreHelper.StartCoroutine(GenerateUI());
        }

        public static void Pause()
        {
            if (!CoreHelper.Playing)
                return;

            LSHelpers.ShowCursor();
            AudioManager.inst.CurrentAudioSource.Pause();
            InputDataManager.inst.SetAllControllerRumble(0f);
            GameManager.inst.gameState = GameManager.State.Paused;
            ArcadeHelper.endedLevel = false;
            Current = new PauseMenu();
        }

        public static void UnPause()
        {
            if (!CoreHelper.Paused)
                return;

            Current?.Clear();
            Current = null;
            LSHelpers.HideCursor();
            AudioManager.inst.CurrentAudioSource.UnPause();
            GameManager.inst.gameState = GameManager.State.Playing;
        }
    }
}
