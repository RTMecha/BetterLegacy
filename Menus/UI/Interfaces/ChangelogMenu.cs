
using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Layouts;

namespace BetterLegacy.Menus.UI.Interfaces
{

    public class ChangeLogMenu : MenuBase
    {
        public ChangeLogMenu(string[] notes) : base()
        {
            name = "Changelog";
            musicName = InterfaceManager.RANDOM_MUSIC_NAME;

            layouts.Add("updates", new MenuVerticalLayout
            {
                name = "updates",
                childControlWidth = true,
                childForceExpandWidth = true,
                spacing = 4f,
                rect = RectValues.FullAnchored.AnchoredPosition(0f, -32f).SizeDelta(-64f, -320f),
                mask = true,
                scrollable = true,
                minScroll = 0f,
                maxScroll = 10000f,
            });

            var onScrollUpFunc = Parser.NewJSONObject();
            onScrollUpFunc["name"] = "ScrollLayout";
            onScrollUpFunc["params"][0] = "updates";
            onScrollUpFunc["params"][1] = "-36";
            onScrollUpFunc["params"][2] = true;

            var onScrollDownFunc = Parser.NewJSONObject();
            onScrollDownFunc["name"] = "ScrollLayout";
            onScrollDownFunc["params"][0] = "updates";
            onScrollDownFunc["params"][1] = "36";
            onScrollDownFunc["params"][2] = true;

            elements.Add(new MenuImage
            {
                id = "0",
                name = "Background",
                opacity = 0f,
                rect = RectValues.FullAnchored,
                onScrollUpFuncJSON = onScrollUpFunc,
                onScrollDownFuncJSON = onScrollDownFunc,
            });

            elements.Add(new MenuText
            {
                id = "1",
                name = "Title",
                rect = RectValues.Default.AnchoredPosition(-640f, 440f).SizeDelta(400f, 64f),
                text = "<size=60><b>BetterLegacy Changelog",
                icon = LegacyPlugin.PALogoSprite,
                iconRect = RectValues.Default.AnchoredPosition(-256f, 0f).SizeDelta(64f, 64f),
                hideBG = true,
                textColor = 6,
                onScrollUpFuncJSON = onScrollUpFunc,
                onScrollDownFuncJSON = onScrollDownFunc,
            });

            notes.ForLoop(note =>
            {
                float size = (note.Length / 92) + 1;
                var isDotPoint = note.StartsWith("- ");
                if (isDotPoint)
                {
                    note = RTString.SectionString(note, 2, note.Length - 1);
                    note = "● " + note;
                }
                var isSubDotPoint = note.StartsWith("  - ");
                if (isSubDotPoint)
                {
                    note = RTString.SectionString(note, 4, note.Length - 1);
                    note = "° " + note;
                }

                if (note.StartsWith("### "))
                {
                    note = RTString.SectionString(note, 4, note.Length - 1);
                    note = "<size=34><b>" + note;
                }
                
                if (note.StartsWith("## "))
                {
                    note = RTString.SectionString(note, 3, note.Length - 1);
                    note = "<size=40><b>" + note;
                }
                
                if (note.StartsWith("# "))
                {
                    note = RTString.SectionString(note, 2, note.Length - 1);
                    note = "<size=52><b>" + note;
                }

                elements.Add(new MenuText
                {
                    id = LSText.randomNumString(16),
                    name = "Update Note",
                    text = note,
                    textRect = isSubDotPoint ? RectValues.FullAnchored.AnchoredPosition(16f, 0f).SizeDelta(-32f, 0f) : RectValues.FullAnchored,
                    enableWordWrapping = true,
                    parentLayout = "updates",
                    rect = RectValues.Default.SizeDelta(0f, 36f * size),
                    hideBG = true,
                    textColor = 6,
                    length = 0.05f,
                    onScrollUpFuncJSON = onScrollUpFunc,
                    onScrollDownFuncJSON = onScrollDownFunc,
                });
            });

            elements.Add(new MenuButton
            {
                id = "0",
                name = "Next Menu Button",
                text = "<b><align=center>[ NEXT ]",
                rect = RectValues.Default.AnchoredPosition(0f, -470f).SizeDelta(300f, 64f),
                func = () => InterfaceManager.inst.SetCurrentInterface(InterfaceManager.MAIN_MENU_ID),
                opacity = 0.1f,
                selectedOpacity = 1f,
                color = 6,
                selectedColor = 6,
                textColor = 6,
                selectedTextColor = 7,
                length = 1f,
                onScrollUpFuncJSON = onScrollUpFunc,
                onScrollDownFuncJSON = onScrollDownFunc,
            });

            InterfaceManager.inst.SetCurrentInterface(this);
            InterfaceManager.inst.PlayMusic();
            exitFunc = () => InterfaceManager.inst.SetCurrentInterface(InterfaceManager.MAIN_MENU_ID);
            Seen = true;
        }

        public static bool Seen { get; set; }
    }
}
