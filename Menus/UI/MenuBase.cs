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

namespace BetterLegacy.Menus.UI
{
    public abstract class MenuBase
    {
        public GameObject canvas;

        public MenuBase(bool setupUI = true)
        {
            if (setupUI)
                CoreHelper.StartCoroutine(GenerateUI());
        }

        public bool isOpen;

        public Vector2Int selected;

        public List<MenuText> elements = new List<MenuText>();
        public Dictionary<string, MenuLayoutBase> layouts = new Dictionary<string, MenuLayoutBase>();

        public abstract IEnumerator GenerateUI();

        public void Clear()
        {
            isOpen = false;
            UnityEngine.Object.Destroy(canvas);
            elements.Clear();
            layouts.Clear();
        }

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
                    text.textInterpolation?.animationHandlers[0]?.SetKeyframeTime(1, actions.Submit.IsPressed ? text.length * 0.3f : text.length);
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
                if (selected.x < elements.FindAll(x => x is MenuButton menuButton && menuButton.selectionPosition.y == selected.y).Select(x => x as MenuButton).Max(x => x.selectionPosition.x))
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
                    selected.x = Mathf.Clamp(selected.x, 0, elements.FindAll(x => x is MenuButton menuButton && menuButton.selectionPosition.y == selected.y).Select(x => x as MenuButton).Max(x => x.selectionPosition.x));
                }
                else
                    AudioManager.inst.PlaySound("Block");
            }

            if (actions.Down.WasPressed)
            {
                if (selected.y < elements.FindAll(x => x is MenuButton).Select(x => x as MenuButton).Max(x => x.selectionPosition.y))
                {
                    AudioManager.inst.PlaySound("LeftRight");
                    selected.y++;
                    selected.x = Mathf.Clamp(selected.x, 0, elements.FindAll(x => x is MenuButton menuButton && menuButton.selectionPosition.y == selected.y).Select(x => x as MenuButton).Max(x => x.selectionPosition.x));
                }
                else
                    AudioManager.inst.PlaySound("Block");
            }

            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element is not MenuButton button)
                    continue;

                if (button.selectionPosition == selected)
                {
                    if (actions.Submit.WasPressed)
                        button.clickable?.onClick?.Invoke(null);

                    if (!button.isHovered)
                    {
                        button.isHovered = true;
                        button.OnEnter();
                    }
                }
                else
                {
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
        public virtual void UpdateTheme()
        {
            if (!CoreHelper.InGame)
                Camera.main.backgroundColor = Theme.backgroundColor;

            if (elements == null)
                return;

            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element is not MenuButton button || element == null || !button.image || !button.textUI)
                    continue;

                var isSelected = button.selectionPosition == selected;
                button.image.color = isSelected ? Theme.GetObjColor(button.selectedColor) : Theme.GetObjColor(button.color);
                button.textUI.color = isSelected ? Theme.GetObjColor(button.selectedTextColor) : Theme.GetObjColor(button.textColor);
            }
        }

        public abstract BeatmapTheme Theme { get; set; }

        public void SetupGridLayout(MenuGridLayout layout, Transform parent)
        {
            if (layout.gameObject)
                UnityEngine.Object.Destroy(layout.gameObject);

            layout.gameObject = Creator.NewUIObject(layout.name, parent);
            layout.gridLayout = layout.gameObject.AddComponent<GridLayoutGroup>();
            layout.gridLayout.cellSize = layout.cellSize;
            layout.gridLayout.spacing = layout.spacing;
            layout.gridLayout.constraintCount = layout.constraintCount;
            layout.gridLayout.constraint = layout.constraint;
            layout.gridLayout.childAlignment = layout.childAlignment;
            layout.gridLayout.startAxis = layout.startAxis;
            layout.gridLayout.startCorner = layout.startCorner;

            if (layout.rectJSON != null)
                Parser.ParseRectTransform(layout.gameObject.transform.AsRT(), layout.rectJSON);
        }

        public void SetupHorizontalLayout(MenuHorizontalLayout layout, Transform parent)
        {
            if (layout.gameObject)
                UnityEngine.Object.Destroy(layout.gameObject);

            layout.gameObject = Creator.NewUIObject(layout.name, parent);
            layout.horizontalLayout = layout.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.horizontalLayout.childControlHeight = layout.childControlHeight;
            layout.horizontalLayout.childControlWidth = layout.childControlWidth;
            layout.horizontalLayout.childForceExpandHeight = layout.childForceExpandHeight;
            layout.horizontalLayout.childForceExpandWidth = layout.childForceExpandWidth;
            layout.horizontalLayout.childScaleHeight = layout.childScaleHeight;
            layout.horizontalLayout.childScaleWidth = layout.childScaleWidth;
            layout.horizontalLayout.spacing = layout.spacing;
            layout.horizontalLayout.childAlignment = layout.childAlignment;

            if (layout.rectJSON != null)
                Parser.ParseRectTransform(layout.gameObject.transform.AsRT(), layout.rectJSON);
        }

        public void SetupVerticalLayout(MenuVerticalLayout layout, Transform parent)
        {
            if (layout.gameObject)
                UnityEngine.Object.Destroy(layout.gameObject);

            layout.gameObject = Creator.NewUIObject(layout.name, parent);
            layout.verticalLayout = layout.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.verticalLayout.childControlHeight = layout.childControlHeight;
            layout.verticalLayout.childControlWidth = layout.childControlWidth;
            layout.verticalLayout.childForceExpandHeight = layout.childForceExpandHeight;
            layout.verticalLayout.childForceExpandWidth = layout.childForceExpandWidth;
            layout.verticalLayout.childScaleHeight = layout.childScaleHeight;
            layout.verticalLayout.childScaleWidth = layout.childScaleWidth;
            layout.verticalLayout.spacing = layout.spacing;
            layout.verticalLayout.childAlignment = layout.childAlignment;

            if (layout.rectJSON != null)
                Parser.ParseRectTransform(layout.gameObject.transform.AsRT(), layout.rectJSON);
        }

        public void SetupText(MenuText menuText, Transform parent)
        {
            if (menuText.gameObject)
                UnityEngine.Object.Destroy(menuText.gameObject);

            menuText.gameObject = Creator.NewUIObject(menuText.name, parent);
            menuText.image = menuText.gameObject.AddComponent<Image>();
            menuText.image.rectTransform.anchoredPosition = menuText.pos;
            menuText.image.rectTransform.sizeDelta = menuText.size;

            if (menuText.rectJSON != null)
                Parser.ParseRectTransform(menuText.image.rectTransform, menuText.rectJSON);

            menuText.clickable = menuText.gameObject.AddComponent<Clickable>();

            var t = UIManager.GenerateUITextMeshPro("Text", menuText.gameObject.transform);
            ((RectTransform)t["RectTransform"]).anchoredPosition = Vector2.zero;
            menuText.textUI = (TextMeshProUGUI)t["Text"];
            menuText.textUI.text = menuText.text;
            menuText.textUI.maxVisibleCharacters = 0;

            if (menuText.textRectJSON != null)
                Parser.ParseRectTransform(menuText.textUI.rectTransform, menuText.textRectJSON);
            else
                UIManager.SetRectTransform(menuText.textUI.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);

            menuText.Spawn();
        }

        public MenuText CreateText(string name, Transform parent, string text, Vector2 pos = default, Vector2 size = default)
        {
            var menuText = new MenuText
            {
                name = name,
                text = text,
                pos = pos,
                size = size,
            };
            SetupText(menuText, parent);

            return menuText;
        }

        public void SetupButton(MenuButton menuButton, Transform parent)
        {
            if (menuButton.gameObject)
                UnityEngine.Object.Destroy(menuButton.gameObject);

            menuButton.gameObject = Creator.NewUIObject(menuButton.name, parent);
            menuButton.image = menuButton.gameObject.AddComponent<Image>();
            menuButton.image.rectTransform.anchoredPosition = menuButton.pos;
            menuButton.image.rectTransform.sizeDelta = menuButton.size;

            if (menuButton.rectJSON != null)
                Parser.ParseRectTransform(menuButton.image.rectTransform, menuButton.rectJSON);

            menuButton.clickable = menuButton.gameObject.AddComponent<Clickable>();
            menuButton.clickable.onEnter = pointerEventdata =>
            {
                if (!isOpen)
                    return;

                AudioManager.inst.PlaySound("LeftRight");
                selected = menuButton.selectionPosition;
            };

            var t = UIManager.GenerateUITextMeshPro("Text", menuButton.gameObject.transform);
            ((RectTransform)t["RectTransform"]).anchoredPosition = Vector2.zero;
            menuButton.textUI = (TextMeshProUGUI)t["Text"];
            menuButton.textUI.text = menuButton.text;
            menuButton.textUI.maxVisibleCharacters = 0;

            if (menuButton.textRectJSON != null)
                Parser.ParseRectTransform(menuButton.textUI.rectTransform, menuButton.textRectJSON);
            else
                UIManager.SetRectTransform(menuButton.textUI.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);

            menuButton.Spawn();
        }

        public MenuButton CreateButton(string name, Transform parent, Vector2Int position, string text, Vector2 pos = default, Vector2 size = default)
        {
            var menuButton = new MenuButton
            {
                name = name,
                text = text,
                pos = pos,
                size = size,
                selectionPosition = position,
            };
            SetupButton(menuButton, parent);

            return menuButton;
        }
    }
}
