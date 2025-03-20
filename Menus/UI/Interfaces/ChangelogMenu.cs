using LSFunctions;

using BetterLegacy.Core.Data;
using BetterLegacy.Menus.UI.Elements;

namespace BetterLegacy.Menus.UI.Interfaces
{

    public class ChangeLogMenu : MenuBase
    {
        public ChangeLogMenu() : base()
        {
            musicName = InterfaceManager.RANDOM_MUSIC_NAME;
            exitFunc = () => InterfaceManager.inst.SetCurrentInterface(InterfaceManager.MAIN_MENU_ID);
        }

        public static bool Seen { get; set; }

        public void AddUpdateNote(string note)
        {
            elements.Add(new MenuText
            {
                id = LSText.randomNumString(16),
                name = "Update Note",
                text = note,
                parentLayout = "updates",
                rect = RectValues.Default.SizeDelta(0f, 36f),
                hideBG = true,
                textColor = 6
            });
        }
    }
}
