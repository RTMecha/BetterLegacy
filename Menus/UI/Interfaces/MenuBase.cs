﻿using BetterLegacy.Components;
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
using SimpleJSON;
using BetterLegacy.Core.Animation;

namespace BetterLegacy.Menus.UI.Interfaces
{
    /// <summary>
    /// Base menu class to be used for interfaces. Includes a custom selection system and UI system.
    /// </summary>
    public abstract class MenuBase
    {
        public UICanvas canvas;

        public MenuBase(bool setupUI = true)
        {
            if (setupUI)
                CoreHelper.StartCoroutine(GenerateUI());
        }

        /// <summary>
        /// For cases where the UI only needs to be set active / inactive instead of destroyed.
        /// </summary>
        /// <param name="active"></param>
        public void SetActive(bool active)
        {
            canvas?.GameObject.SetActive(active);
            isOpen = active;
        }

        /// <summary>
        /// The music to play when the user enters the interface.
        /// </summary>
        public AudioClip music;

        /// <summary>
        /// The name to be used if <see cref="music"/> is not loaded.
        /// </summary>
        public string musicName;

        /// <summary>
        /// Name of the menu.
        /// </summary>
        public string name;

        /// <summary>
        /// Identification of the menu.
        /// </summary>
        public string id;

        /// <summary>
        /// True if the menu is open, otherwise false. Prevents main functions like the control system if false.
        /// </summary>
        public bool isOpen;

        /// <summary>
        /// Where the user is selecting. To be compared with <see cref="MenuButton.selectionPosition"/>.
        /// </summary>
        public Vector2Int selected;

        /// <summary>
        /// All the loaded elements in the menu.
        /// </summary>
        public List<MenuImage> elements = new List<MenuImage>();

        /// <summary>
        /// All the layouts to be used for the elements.
        /// </summary>
        public Dictionary<string, MenuLayoutBase> layouts = new Dictionary<string, MenuLayoutBase>();

        /// <summary>
        /// The file location of the menu. This isn't necessary for cases where the menu does not have a file origin.
        /// </summary>
        public string filePath;

        /// <summary>
        /// Looping animation to be used for all events.
        /// </summary>
        public RTAnimation loopingEvents;

        /// <summary>
        /// Animation to be used for all events when the menu spawns.
        /// </summary>
        public RTAnimation spawnEvents;

        /// <summary>
        /// Plays the menus' default music.
        /// </summary>
        public void PlayDefaultMusic()
        {
            if (music)
            {
                NewMenuManager.inst.PlayMusic(music);
                return;
            }

            if (string.IsNullOrEmpty(musicName))
                return;
            
            if (AudioManager.inst.library.musicClips.ContainsKey(musicName))
            {
                var group = AudioManager.inst.library.musicClips[musicName];

                int index = UnityEngine.Random.Range(0, group.Length);
                if (AudioManager.inst.library.musicClipsRandomIndex.ContainsKey(musicName) && AudioManager.inst.library.musicClipsRandomIndex[musicName] < group.Length)
                    index = AudioManager.inst.library.musicClipsRandomIndex[musicName];

                NewMenuManager.inst.PlayMusic(group[index]);
            }
            else if (RTFile.FileExists($"{Path.GetDirectoryName(filePath)}/{musicName}.ogg"))
            {
                CoreHelper.StartCoroutine(AlephNetworkManager.DownloadAudioClip($"file://{Path.GetDirectoryName(filePath)}/{musicName}.ogg", AudioType.OGGVORBIS, audioClip =>
                {
                    CoreHelper.Log($"Attempting to play music: {musicName}");
                    music = audioClip;
                    NewMenuManager.inst.PlayMusic(audioClip);
                }));
            }
        }

        /// <summary>
        /// IEnumerator Coroutine used to Generate all the menus' elements. Can be overridden if needed.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerator GenerateUI()
        {
            selected = Vector2Int.zero;

            var canvas = UIManager.GenerateUICanvas(nameof(CustomMenu), null, sortingOrder: 900);
            this.canvas = canvas;
            canvas.Canvas.scaleFactor = 1f;
            canvas.CanvasScaler.referenceResolution = new Vector2(1920f, 1080f);

            if (!CoreHelper.InGame)
            {
                canvas.Canvas.worldCamera = Camera.main;
                canvas.Canvas.renderMode = RenderMode.ScreenSpaceCamera;
                yield return null;
                canvas.Canvas.renderMode = RenderMode.WorldSpace;
            }

            if (loopingEvents != null)
            {
                loopingEvents.loop = true;
                AnimationManager.inst.Play(loopingEvents);
            }
            
            if (spawnEvents != null)
            {
                spawnEvents.loop = false;
                AnimationManager.inst.Play(spawnEvents);
            }

            var gameObject = Creator.NewUIObject("Base Layout", canvas.Canvas.transform);
            UIManager.SetRectTransform(gameObject.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);

            for (int i = 0; i < layouts.Count; i++)
            {
                var layout = layouts.ElementAt(i).Value;
                if (layout is MenuGridLayout gridLayout)
                    SetupGridLayout(gridLayout, gameObject.transform);
                if (layout is MenuHorizontalLayout horizontalLayout)
                    SetupHorizontalLayout(horizontalLayout, gameObject.transform);
                if (layout is MenuVerticalLayout verticalLayout)
                    SetupVerticalLayout(verticalLayout, gameObject.transform);
            }

            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];

                if (element is MenuEvent menuEvent)
                {
                    menuEvent.TriggerEvent();
                    while (menuEvent.isSpawning)
                        yield return null;

                    continue;
                }

                var parent = GetElementParent(element, gameObject);

                if (element is MenuButton menuButton)
                {
                    SetupButton(menuButton, parent);
                    if (menuButton.siblingIndex >= 0 && menuButton.siblingIndex < menuButton.gameObject.transform.parent.childCount)
                        menuButton.gameObject.transform.SetSiblingIndex(menuButton.siblingIndex);

                    while (element.isSpawning)
                        yield return null;

                    menuButton.clickable.onClick = p =>
                    {
                        if (menuButton.playBlipSound)
                            AudioManager.inst.PlaySound("blip");
                        menuButton.ParseFunction(menuButton.funcJSON);
                        menuButton.func?.Invoke();
                    };

                    continue;
                }

                if (element is MenuText menuText)
                {
                    SetupText(menuText, parent);
                    if (menuText.siblingIndex >= 0 && menuText.siblingIndex < menuText.gameObject.transform.parent.childCount)
                        menuText.gameObject.transform.SetSiblingIndex(menuText.siblingIndex);
                    while (menuText.isSpawning)
                        yield return null;
                }
                else
                {
                    SetupImage(element, parent);
                    if (element.siblingIndex >= 0 && element.siblingIndex < element.gameObject.transform.parent.childCount)
                        element.gameObject.transform.SetSiblingIndex(element.siblingIndex);
                    while (element.isSpawning)
                        yield return null;
                }

                element.clickable.onClick = p =>
                {
                    if (element.playBlipSound)
                        AudioManager.inst.PlaySound("blip");
                    element.ParseFunction(element.funcJSON);
                    element.func?.Invoke();
                };
            }

            if (elements.TryFind(x => x is MenuButton, out MenuImage menuImage) && menuImage is MenuButton button)
            {
                button.OnEnter();
            }

            isOpen = true;

            yield break;
        }

        /// <summary>
        /// Clears the menu.
        /// </summary>
        public void Clear()
        {
            isOpen = false;
            selected = Vector2Int.zero;

            for (int i = 0; i < elements.Count; i++)
                elements[i]?.Clear();

            UnityEngine.Object.Destroy(canvas?.GameObject);
            if (loopingEvents != null)
                AnimationManager.inst.RemoveID(loopingEvents.id);
            if (spawnEvents != null)
                AnimationManager.inst.RemoveID(spawnEvents.id);
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
                    var element = elements[i];

                    if (element.isSpawning)
                        element.UpdateSpawnCondition();

                    if (element is MenuButton menuButton)
                        menuButton.isHovered = false;
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

            if (canvas != null)
                canvas.Canvas.scaleFactor = CoreHelper.ScreenScale;

            if (elements == null)
                return;

            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (!element.image)
                    continue;

                if (element is MenuButton button)
                {
                    var isSelected = button.selectionPosition == selected;
                    button.image.color = isSelected ? LSColors.fadeColor(Theme.GetObjColor(button.selectedColor), button.selectedOpacity) : LSColors.fadeColor(Theme.GetObjColor(button.color), button.opacity);
                    button.textUI.color = isSelected ? Theme.GetObjColor(button.selectedTextColor) : Theme.GetObjColor(button.textColor);
                    continue;
                }

                if (element is MenuText text)
                {
                    text.textUI.color = Theme.GetObjColor(text.textColor);
                    text.UpdateText();
                }

                element.image.color = LSColors.fadeColor(Theme.GetObjColor(element.color), element.opacity);
            }
        }

        /// <summary>
        /// The current theme to use for the menu colors.
        /// </summary>
        public BeatmapTheme Theme { get; set; }

        /// <summary>
        /// Gets an elements' parent, whether it be a layout or another element.
        /// </summary>
        /// <param name="element">The element to parent.</param>
        /// <param name="defaultLayout">The default GameObject to parent to if no parent is found.</param>
        /// <returns></returns>
        public Transform GetElementParent(MenuImage element, GameObject defaultLayout) => !string.IsNullOrEmpty(element.parentLayout) && layouts.ContainsKey(element.parentLayout) ? layouts[element.parentLayout].gameObject.transform : !string.IsNullOrEmpty(element.parent) && elements.TryFind(x => x.id == element.parent, out MenuImage menuParent) && menuParent.gameObject ? menuParent.gameObject.transform : defaultLayout.transform;

        /// <summary>
        /// Initializes a <see cref="MenuGridLayout"/>s' UI.
        /// </summary>
        /// <param name="layout">The layout to generate UI for.</param>
        /// <param name="parent">The parent to set the layout to.</param>
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

        /// <summary>
        /// Initializes a <see cref="MenuHorizontalLayout"/>s' UI.
        /// </summary>
        /// <param name="layout">The layout to generate UI for.</param>
        /// <param name="parent">The parent to set the layout to.</param>
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

        /// <summary>
        /// Initializes a <see cref="MenuVerticalLayout"/>s' UI.
        /// </summary>
        /// <param name="layout">The layout to generate UI for.</param>
        /// <param name="parent">The parent to set the layout to.</param>
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

        /// <summary>
        /// Initializes a <see cref="MenuImage"/>s' UI.
        /// </summary>
        /// <param name="menuImage">The element to generate UI for.</param>
        /// <param name="parent">The parent to set the element to.</param>
        public void SetupImage(MenuImage menuImage, Transform parent)
        {
            if (menuImage.gameObject)
                UnityEngine.Object.Destroy(menuImage.gameObject);

            menuImage.gameObject = Creator.NewUIObject(menuImage.name, parent);
            menuImage.image = menuImage.gameObject.AddComponent<Image>();

            if (menuImage.rectJSON != null)
                Parser.ParseRectTransform(menuImage.image.rectTransform, menuImage.rectJSON);

            menuImage.clickable = menuImage.gameObject.AddComponent<Clickable>();

            if (menuImage.icon)
                menuImage.image.sprite = menuImage.icon;
            else if (MenuConfig.Instance.RoundedUI.Value)
                SpriteManager.SetRoundedSprite(menuImage.image, menuImage.rounded, menuImage.roundedSide);

            if (menuImage.reactiveSetting.init)
            {
                var reactiveAudio = menuImage.gameObject.AddComponent<MenuReactiveAudio>();
                reactiveAudio.reactiveSetting = menuImage.reactiveSetting;
                reactiveAudio.ogPosition = menuImage.rectJSON == null || menuImage.rectJSON["anc_pos"] == null ? Vector2.zero : menuImage.rectJSON["anc_pos"].AsVector2();
            }

            if (menuImage.spawnFuncJSON != null)
                menuImage.ParseFunction(menuImage.spawnFuncJSON);
            menuImage.spawnFunc?.Invoke();

            menuImage.Spawn();
        }

        /// <summary>
        /// Initializes a <see cref="MenuText"/>s' UI.
        /// </summary>
        /// <param name="menuText">The element to generate UI for.</param>
        /// <param name="parent">The parent to set the element to.</param>
        public void SetupText(MenuText menuText, Transform parent)
        {
            if (menuText.gameObject)
                UnityEngine.Object.Destroy(menuText.gameObject);

            menuText.gameObject = Creator.NewUIObject(menuText.name, parent);
            menuText.image = menuText.gameObject.AddComponent<Image>();

            if (menuText.rectJSON != null)
                Parser.ParseRectTransform(menuText.image.rectTransform, menuText.rectJSON);

            menuText.image.enabled = !menuText.hideBG;
            if (!menuText.hideBG && MenuConfig.Instance.RoundedUI.Value)
                SpriteManager.SetRoundedSprite(menuText.image, menuText.rounded, menuText.roundedSide);

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

            if (menuText.icon)
            {
                var icon = Creator.NewUIObject("Icon", menuText.gameObject.transform);
                menuText.iconUI = icon.AddComponent<Image>();
                menuText.iconUI.sprite = menuText.icon;
                if (menuText.iconRectJSON != null)
                    Parser.ParseRectTransform(menuText.iconUI.rectTransform, menuText.iconRectJSON);
            }

            if (menuText.reactiveSetting.init)
            {
                var reactiveAudio = menuText.gameObject.AddComponent<MenuReactiveAudio>();
                reactiveAudio.reactiveSetting = menuText.reactiveSetting;
                reactiveAudio.ogPosition = menuText.rectJSON == null || menuText.rectJSON["anc_pos"] == null ? Vector2.zero : menuText.rectJSON["anc_pos"].AsVector2();
            }

            if (menuText.spawnFuncJSON != null)
                menuText.ParseFunction(menuText.spawnFuncJSON);
            menuText.spawnFunc?.Invoke();

            menuText.Spawn();
        }

        /// <summary>
        /// Initializes a <see cref="MenuButton"/>s' UI.
        /// </summary>
        /// <param name="menuButton">The element to generate UI for.</param>
        /// <param name="parent">The parent to set the element to.</param>
        public void SetupButton(MenuButton menuButton, Transform parent)
        {
            if (menuButton.gameObject)
                UnityEngine.Object.Destroy(menuButton.gameObject);

            menuButton.gameObject = Creator.NewUIObject(menuButton.name, parent);
            menuButton.image = menuButton.gameObject.AddComponent<Image>();

            if (menuButton.rectJSON != null)
                Parser.ParseRectTransform(menuButton.image.rectTransform, menuButton.rectJSON);

            menuButton.image.enabled = !menuButton.hideBG;
            if (!menuButton.hideBG && MenuConfig.Instance.RoundedUI.Value)
                SpriteManager.SetRoundedSprite(menuButton.image, menuButton.rounded, menuButton.roundedSide);

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

            if (menuButton.icon)
            {
                var icon = Creator.NewUIObject("Icon", menuButton.gameObject.transform);
                menuButton.iconUI = icon.AddComponent<Image>();
                menuButton.iconUI.sprite = menuButton.icon;
                if (menuButton.iconRectJSON != null)
                    Parser.ParseRectTransform(menuButton.iconUI.rectTransform, menuButton.iconRectJSON);
            }

            if (menuButton.reactiveSetting.init)
            {
                var reactiveAudio = menuButton.gameObject.AddComponent<MenuReactiveAudio>();
                reactiveAudio.reactiveSetting = menuButton.reactiveSetting;
                reactiveAudio.ogPosition = menuButton.rectJSON == null || menuButton.rectJSON["anc_pos"] == null ? Vector2.zero : menuButton.rectJSON["anc_pos"].AsVector2();
            }

            if (menuButton.spawnFuncJSON != null)
                menuButton.ParseFunction(menuButton.spawnFuncJSON);
            menuButton.spawnFunc?.Invoke();

            menuButton.Spawn();
        }
    }
}
