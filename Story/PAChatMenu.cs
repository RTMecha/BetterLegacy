using BetterLegacy.Menus.UI.Interfaces;
using BetterLegacy.Menus.UI.Layouts;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus;
using BetterLegacy.Core.Data;
using BetterLegacy.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BetterLegacy.Configs;
using BetterLegacy.Core.Helpers;
using LSFunctions;

namespace BetterLegacy.Story
{
    /// <summary>
    /// PA Chat menu for story mode requires specific functionality that it needs to be built-in.
    /// </summary>
    public class PAChatMenu : MenuBase
    {
        public static PAChatMenu Current { get; set; }

        public PAChatMenu() : base()
        {
            InterfaceManager.inst.CurrentMenu = this;

            id = "24";

            regenerate = false;

            elements.Add(new MenuEvent
            {
                id = "09",
                name = "Effects",
                func = () => MenuEffectsManager.inst.UpdateChroma(0.1f),
                length = 0f,
                regenerate = false,
            });

            layouts.Add("chat", new MenuVerticalLayout
            {
                name = "chat",
                childControlWidth = true,
                childForceExpandWidth = true,
                spacing = 4f,
                rect = RectValues.Default.AnchoredPosition(0f, 140f).SizeDelta(1700f, 200f),
                regenerate = false,
            });

            layouts.Add("buttons", new MenuVerticalLayout
            {
                name = "buttons",
                childControlWidth = true,
                childForceExpandWidth = true,
                spacing = 4f,
                rect = RectValues.Default.AnchoredPosition(-500f, 220f).SizeDelta(800f, 200f),
                regenerate = false,
            });
            
            elements.Add(new MenuImage
            {
                id = "0",
                name = "bg",
                parentLayout = "chat",
                rect = RectValues.FullAnchored,
                color = 6,
                opacity = 0.02f,
                wait = false,
                regenerate = false,
            });

            elements.AddRange(GenerateTopBar($"PAChat, stable {ProjectArrhythmia.GameVersion}", 6, 0f, false));

            elements.Add(new MenuButton
            {
                id = "4918487",
                name = name,
                text = $"<b> [ RETURN ]",
                parentLayout = "buttons",
                selectionPosition = new Vector2Int(0, 0),
                rect = RectValues.Default.SizeDelta(200f, 64f),
                color = 6,
                opacity = 0.1f,
                textColor = 6,
                selectedColor = 6,
                selectedTextColor = 7,
                selectedOpacity = 1f,
                length = 1f,
                playBlipSound = true,
                func = InterfaceManager.inst.StartupStoryInterface,
                regenerate = false,
            });

            elements.AddRange(GenerateBottomBar(6, 0f, false));

            var chats = StoryManager.inst.ReadChats();
            for (int i = 0; i < chats.Count; i++)
            {
                var time = StoryManager.inst.ReadChatTime(i);
                var character = StoryManager.inst.ReadChatCharacter(i);

                var isHal = character.Equals("hal", StringComparison.OrdinalIgnoreCase);

                string final = time;
                for (int j = 0; j < 14 - character.Length; j++)
                {
                    if (!isHal)
                        final += " ";
                    else
                        final = " " + final;
                }

                if (!isHal)
                    final += $"<b>{character}</b>";
                else
                    final = $"<b>{character}</b>" + final;

                elements.Add(new MenuText
                {
                    id = "2566",
                    name = "chat",
                    parentLayout = "chat",
                    text = isHal ? $"{chats[i]} | {final} | " : $" | {final} | {chats[i]}",
                    alignment = isHal ? TMPro.TextAlignmentOptions.Right : TMPro.TextAlignmentOptions.Left,
                    length = 0.1f,
                    color = 6,
                    opacity = 0.05f,
                    regenerate = false,
                    rect = RectValues.Default.SizeDelta(1700f, 64f),
                    textRect = RectValues.FullAnchored.SizeDelta(-32f, 0f),
                });
            }

            exitFunc = InterfaceManager.inst.StartupStoryInterface;

            //allowEffects = false;
            layer = 10000;
            //defaultSelection = new Vector2Int(0, 4);
            InterfaceManager.inst.CurrentGenerateUICoroutine = CoreHelper.StartCoroutine(GenerateUI());
        }

        public void AddChat(string character, string chat, Action onSeen = null)
        {
            var isHal = character.Equals("hal", StringComparison.OrdinalIgnoreCase);

            var time = DateTime.Now.ToString("HH:mm:ss:tt");
            string final = time;
            for (int i = 0; i < 14 - character.Length; i++)
            {
                if (!isHal)
                    final += " ";
                else
                    final = " " + final;
            }

            if (!isHal)
                final += $"<b>{character}</b>";
            else
                final = $"<b>{character}</b>" + final;

            elements.Add(new MenuText
            {
                id = "2566",
                name = "chat",
                parentLayout = "chat",
                text = isHal ? $"{chat} | {final} |" : $" |{final} | {chat}",
                alignment = isHal ? TMPro.TextAlignmentOptions.Right : TMPro.TextAlignmentOptions.Left,
                length = 2f,
                color = 6,
                opacity = 0.05f,
                regenerate = false,
                rect = RectValues.Default.SizeDelta(1700f, 64f),
                textRect = RectValues.FullAnchored.SizeDelta(-32f, 0f),
                onWaitEndFunc = onSeen,
            });
            StoryManager.inst.AddChat(character, chat, time);
            InterfaceManager.inst.CurrentGenerateUICoroutine = CoreHelper.StartCoroutine(GenerateUI());
        }

        public void RemoveNextButton()
        {
            for (int i = 0; i < elements.Count; i++)
                if (elements[i].id == "148581" && elements[i].gameObject)
                    CoreHelper.Destroy(elements[i].gameObject);
            elements.RemoveAll(x => x.id == "148581");
        }

        public void AddNextButton(Action onClick)
        {
            var element = new MenuButton
            {
                id = "148581",
                name = "Next",
                text = "[ NEXT ]",
                selectionPosition = new Vector2Int(0, 1),
                rect = RectValues.Default.AnchoredPosition(0f, -340f).SizeDelta(300f, 64f),
                alignment = TMPro.TextAlignmentOptions.Center,
                func = onClick,
                color = 6,
                opacity = 0.1f,
                textColor = 6,
                selectedColor = 6,
                selectedTextColor = 7,
                selectedOpacity = 1f,
                length = 1f,
                playBlipSound = true,
                regenerate = false,
            };
            elements.Add(element);
            InterfaceManager.inst.CurrentGenerateUICoroutine = CoreHelper.StartCoroutine(GenerateUI());
        }

        public override void UpdateTheme()
        {
            if (Parser.TryParse(MenuConfig.Instance.InterfaceThemeID.Value, -1) >= 0 && InterfaceManager.inst.themes.TryFind(x => x.id == MenuConfig.Instance.InterfaceThemeID.Value, out BeatmapTheme interfaceTheme))
                Theme = interfaceTheme;
            else
                Theme = InterfaceManager.inst.themes[0];

            base.UpdateTheme();
        }

        public static void Init()
        {
            InterfaceManager.inst.CloseMenus();
            Current = new PAChatMenu();
        }
    }
}
