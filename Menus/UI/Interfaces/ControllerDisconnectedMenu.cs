using System.Linq;

using UnityEngine;

using LSFunctions;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Menus.UI.Elements;

namespace BetterLegacy.Menus.UI.Interfaces
{
    public class ControllerDisconnectedMenu : MenuBase
    {
        public static ControllerDisconnectedMenu Current { get; set; }

        public static void Init(int index)
        {
            AudioManager.inst.CurrentAudioSource.Pause();
            InputDataManager.inst.SetAllControllerRumble(0f);
            GameManager.inst.gameState = GameManager.State.Paused;
            ArcadeHelper.endedLevel = false;
            Current = new ControllerDisconnectedMenu(index);
        }

        public ControllerDisconnectedMenu(int index)
        {
            if (!CoreHelper.InGame || CoreHelper.InEditor)
            {
                CoreHelper.LogError($"Cannot pause outside of the game!");
                return;
            }

            elements.Add(new MenuImage
            {
                id = "35255236785",
                name = "Background",
                siblingIndex = 0,
                rect = RectValues.FullAnchored,
                color = 0,
                val = -999f,
                opacity = 0.7f,
                length = 0f,
            });

            elements.Add(new MenuText
            {
                id = "0",
                name = "message",
                text = $"<align=center>Controller {index + 1} Disconnected. Reconnect your controller.",
                rect = RectValues.Default,
                hideBG = true,
                textVal = 40f,
            });

            elements.Add(new MenuButton
            {
                id = LSText.randomNumString(16),
                name = "Continue Button",
                text = "<align=center><b>[ CONTINUE ]",
                selectionPosition = new Vector2Int(0, 0),
                rect = RectValues.Default.AnchoredPosition(-140f, -200f).SizeDelta(220f, 64f),
                opacity = 0.1f,
                val = -40f,
                textVal = 40f,
                selectedOpacity = 1f,
                selectedVal = 40f,
                selectedTextVal = -40f,
                length = 1f,
                playBlipSound = true,
                func = Continue,
            });
            
            elements.Add(new MenuButton
            {
                id = LSText.randomNumString(16),
                name = "Quit Button",
                text = "<align=center><b>[ QUIT ]",
                selectionPosition = new Vector2Int(1, 0),
                rect = RectValues.Default.AnchoredPosition(140f, -200f).SizeDelta(220f, 64f),
                opacity = 0.1f,
                val = -40f,
                textVal = 40f,
                selectedOpacity = 1f,
                selectedVal = 40f,
                selectedTextVal = -40f,
                length = 1f,
                playBlipSound = true,
                func = ArcadeHelper.QuitToMainMenu,
            });

            InterfaceManager.inst.SetCurrentInterface(this);
        }

        public override void UpdateTheme()
        {
            Theme = CoreHelper.CurrentBeatmapTheme;

            base.UpdateTheme();
        }

        public void Continue()
        {
            if (InputDataManager.inst.players.Count == 0 || !InputDataManager.inst.players.Any(x => x.controllerConnected || x.deviceName == "keyboard"))
            {
                CoreHelper.Notify("There are no players, so the level cannot continue.", Color.red);
                return;
            }

            Reconnected();
        }

        public void Reconnected()
        {
            InterfaceManager.inst.CloseMenus();
            CursorManager.inst.HideCursor();
            AudioManager.inst.CurrentAudioSource.UnPause();
            GameManager.inst.gameState = GameManager.State.Playing;
        }
    }
}
