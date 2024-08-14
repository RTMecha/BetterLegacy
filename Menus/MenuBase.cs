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

namespace BetterLegacy.Menus
{
    public abstract class MenuBase
    {
        public MenuBase()
        {
            CoreHelper.StartCoroutine(GenerateUI());
        }

        public bool isOpen;

        public Vector2Int selected;

        public List<MenuText> elements = new List<MenuText>();

        public abstract IEnumerator GenerateUI();

        /// <summary>
        /// Control system.
        /// </summary>
        public void UpdateControls()
        {
            var actions = InputDataManager.inst.menuActions;

            if (!isOpen)
            {
                for (int i = 0; i < elements.Count; i++)
                {
                    var text = elements[i];
                    text.textInterpolation?.animationHandlers[0]?.SetKeyframeTime(1, actions.Submit.IsPressed ? 0.3f : 1f);
                }
                return;
            }

            if (CoreHelper.IsUsingInputField)
                return;

            if (actions.Left.WasPressed)
            {
                if (selected.x > 0)
                {
                    AudioManager.inst.PlaySound("LeftRight");
                    selected.x--;
                }
                else
                    AudioManager.inst.PlaySound("Block");
            }

            if (actions.Right.WasPressed)
            {
                if (selected.x < elements.FindAll(x => x is MenuButton menuButton && menuButton.position.y == selected.y).Select(x => x as MenuButton).Max(x => x.position.x))
                {
                    AudioManager.inst.PlaySound("LeftRight");
                    selected.x++;
                }
                else
                    AudioManager.inst.PlaySound("Block");
            }

            if (actions.Up.WasPressed)
            {
                if (selected.y > 0)
                {
                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y--;
                    selected.x = Mathf.Clamp(selected.x, 0, elements.FindAll(x => x is MenuButton menuButton && menuButton.position.y == selected.y).Select(x => x as MenuButton).Max(x => x.position.x));
                }
                else
                    AudioManager.inst.PlaySound("Block");
            }

            if (actions.Down.WasPressed)
            {
                if (selected.y < elements.FindAll(x => x is MenuButton).Select(x => x as MenuButton).Max(x => x.position.y))
                {
                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y++;
                    selected.x = Mathf.Clamp(selected.x, 0, elements.FindAll(x => x is MenuButton menuButton && menuButton.position.y == selected.y).Select(x => x as MenuButton).Max(x => x.position.x));
                }
                else
                    AudioManager.inst.PlaySound("Block");
            }

            for (int i = 0; i < elements.Count; i++)
            {
                var text = elements[i];
                if (text is not MenuButton button)
                    continue;

                if (button.position == selected)
                {
                    if (actions.Submit.WasPressed)
                        button.clickable?.onClick?.Invoke(null);

                    button.image.color = Theme.guiAccentColor;
                    if (!button.isHovered)
                    {
                        button.isHovered = true;
                        button.OnEnter();
                    }
                }
                else
                {
                    button.image.color = Theme.GetObjColor(button.color);
                    if (button.isHovered)
                    {
                        button.isHovered = false;
                        button.OnExit();
                    }
                }
            }
        }

        /// <summary>
        /// Updates the colors.
        /// </summary>
        public void UpdateTheme()
        {
            if (!isOpen)
                return;

            //var currentTheme = DataManager.inst.interfaceSettings["UITheme"][SaveManager.inst.settings.Video.UITheme];

            //Camera.main.backgroundColor = default;
        }

        public abstract BeatmapTheme Theme { get; set; }

        public MenuText CreateText(string name, Transform parent, string text, Vector2 pos = default, Vector2 size = default)
        {
            var gameObject = Creator.NewUIObject(name, parent);
            var menuText = gameObject.AddComponent<MenuText>();
            menuText.image = gameObject.AddComponent<Image>();
            menuText.image.rectTransform.anchoredPosition = pos;
            menuText.image.rectTransform.sizeDelta = size;

            menuText.clickable = gameObject.AddComponent<Clickable>();

            var t = UIManager.GenerateUITextMeshPro("Text", gameObject.transform);
            ((RectTransform)t["RectTransform"]).anchoredPosition = Vector2.zero;
            menuText.textUI = (TextMeshProUGUI)t["Text"];
            menuText.textUI.text = "";
            menuText.text = text;

            menuText.Spawn();

            return menuText;
        }

        public MenuButton CreateButton(string name, Transform parent, Vector2Int position, string text, Vector2 pos = default, Vector2 size = default)
        {
            var gameObject = Creator.NewUIObject(name, parent);
            var menuButton = gameObject.AddComponent<MenuButton>();
            menuButton.position = position;
            menuButton.image = gameObject.AddComponent<Image>();
            menuButton.image.rectTransform.anchoredPosition = pos;
            menuButton.image.rectTransform.sizeDelta = size;

            menuButton.clickable = gameObject.AddComponent<Clickable>();
            menuButton.clickable.onEnter = pointerEventdata =>
            {
                if (!isOpen)
                    return;

                AudioManager.inst.PlaySound("LeftRight");
                selected = position;
            };

            var t = UIManager.GenerateUITextMeshPro("Text", gameObject.transform);
            ((RectTransform)t["RectTransform"]).anchoredPosition = Vector2.zero;
            menuButton.textUI = (TextMeshProUGUI)t["Text"];
            menuButton.textUI.text = "";
            menuButton.text = text;

            menuButton.Spawn();

            return menuButton;
        }
    }
}
