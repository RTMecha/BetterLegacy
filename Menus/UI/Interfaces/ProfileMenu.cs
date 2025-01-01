using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Layouts;
using LSFunctions;
using UnityEngine;

namespace BetterLegacy.Menus.UI.Interfaces
{
    public class ProfileMenu : MenuBase
    {
        // todo: make this display both achievements and level data properly.
        public ProfileMenu() : base()
        {
            id = InterfaceManager.PROFILE_MENU_ID;

            musicName = InterfaceManager.RANDOM_MUSIC_NAME;
            name = "Profile";

            elements.Add(new MenuEvent
            {
                id = "09",
                name = "Effects",
                func = MenuEffectsManager.inst.SetDefaultEffects,
                length = 0f,
            });

            layouts.Add("buttons", new MenuVerticalLayout
            {
                name = "buttons",
                spacing = 4f,
                childControlWidth = true,
                childForceExpandWidth = true,
                rect = RectValues.Default.SizeDelta(800f, 100f),
            });

            for (int i = 0; i < LevelManager.Saves.Count; i++)
            {
                var save = LevelManager.Saves[i];
                int index = i;
                var id = LSText.randomNumString(16);

                var levelRank = LevelManager.GetLevelRank(save.Hits);

                var elementBase = new MenuButton
                {
                    id = id,
                    name = "Element Base",
                    parentLayout = "buttons",
                    rect = RectValues.Default.SizeDelta(400f, 64f),
                    text = $" <b><#{LSColors.ColorToHex(levelRank.color)}>{levelRank.name}</color></b>  {save.LevelName} - ID: {save.ID}",
                    selectionPosition = new Vector2Int(0, index + 1),
                    opacity = 0.1f,
                    selectedOpacity = 1f,
                    color = 6,
                    selectedColor = 6,
                    textColor = 6,
                    allowOriginalHoverMethods = true,
                };
                elementBase.enterFunc = () => { MenuEffectsManager.inst.MoveCameraY(elementBase.gameObject.transform.position.y); };

                var delete = new MenuButton
                {
                    id = "0",
                    name = "Delete",
                    parent = id,
                    rect = RectValues.Default.AnchoredPosition(450f, 0f).SizeDelta(126f, 64f),
                    text = "[ DELETE ]",
                    selectionPosition = new Vector2Int(1, index + 1),
                    opacity = 1f,
                    selectedOpacity = 1f,
                    color = 0,
                    selectedColor = 6,
                    textColor = 6,
                    selectedTextColor = 0,
                    func = () =>
                    {
                        LevelManager.Saves.RemoveAt(index);
                        elements.RemoveAll(x => x.id == id);
                        CoreHelper.Destroy(elementBase.gameObject);
                        elementBase = null;
                    },
                    allowOriginalHoverMethods = true,
                };
                delete.enterFunc = () => { MenuEffectsManager.inst.MoveCameraY(elementBase.gameObject.transform.position.y); };

                elements.Add(elementBase);
                elements.Add(delete);
            }

            exitFunc = () => InterfaceManager.inst.SetCurrentInterface(InterfaceManager.MAIN_MENU_ID);
            StartGeneration();
        }
    }
}
