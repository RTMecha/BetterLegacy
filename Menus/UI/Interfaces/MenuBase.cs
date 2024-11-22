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
using SimpleJSON;
using BetterLegacy.Core.Animation;
using BetterLegacy.Story;
using System.Text.RegularExpressions;

namespace BetterLegacy.Menus.UI.Interfaces
{
    /// <summary>
    /// Base menu class to be used for interfaces. Includes a custom selection system and UI system.
    /// </summary>
    public abstract class MenuBase
    {
        public MenuBase() { }

        #region Variables

        public Dictionary<string, Sprite> spriteAssets = new Dictionary<string, Sprite>();

        /// <summary>
        /// Base canvas of the interface.
        /// </summary>
        public UICanvas canvas;

        /// <summary>
        /// The music to play when the user enters the interface.
        /// </summary>
        public AudioClip music;

        /// <summary>
        /// The name to be used if <see cref="music"/> is not loaded.
        /// </summary>
        public string musicName;

        /// <summary>
        /// If true, the user is allowed to have their own music play. Otherwise, prevent custom music.
        /// </summary>
        public bool allowCustomMusic = true;

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
        /// If the menu should regenerate when <see cref="GenerateUI"/> is run.
        /// </summary>
        public bool regenerate = true;

        #region Elements

        /// <summary>
        /// Where the user is selecting. To be compared with <see cref="MenuButton.selectionPosition"/>.
        /// </summary>
        public Vector2Int selected;

        /// <summary>
        /// The first item that is selected when the interface is generated.
        /// </summary>
        public Vector2Int defaultSelection;

        /// <summary>
        /// All the loaded elements in the menu.
        /// </summary>
        public List<MenuImage> elements = new List<MenuImage>();

        /// <summary>
        /// All the layouts to be used for the elements.
        /// </summary>
        public Dictionary<string, MenuLayoutBase> layouts = new Dictionary<string, MenuLayoutBase>();

        /// <summary>
        /// All the prefabs to be applied to the interface when it is generated.
        /// </summary>
        public List<MenuPrefab> prefabs = new List<MenuPrefab>();

        #endregion

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
        /// Function to run when the user wants to exit the interface.
        /// </summary>
        public Action exitFunc;

        /// <summary>
        /// Function to run when the user wants to exit the interface (JSON).
        /// </summary>
        public JSONNode exitFuncJSON;

        /// <summary>
        /// Function to run when <see cref="GenerateUI"/> is complete.
        /// </summary>
        public Action onGenerateUIFinish;

        /// <summary>
        /// If <see cref="MenuEffectsManager"/> should be added to the interface.
        /// </summary>
        public bool allowEffects = true;

        /// <summary>
        /// The canvas layer of the interface.
        /// </summary>
        public int layer = 900;

        public bool pauseGame = false;

        #endregion

        #region Methods

        /// <summary>
        /// For cases where the UI only needs to be set active / inactive instead of destroyed.
        /// </summary>
        /// <param name="active"></param>
        public void SetActive(bool active)
        {
            canvas?.GameObject.SetActive(active);
            isOpen = active;
        }

        #region Autos / Prefabs

        /// <summary>
        /// Iterates through a <see cref="MenuPrefab"/>s' elements and returns elements to be applied to the interface.
        /// </summary>
        /// <param name="prefab">Prefab to apply.</param>
        /// <returns>Returns iterated copies of the prefabs' elements.</returns>
        public IEnumerable<MenuImage> ApplyPrefabElements(MenuPrefabObject prefabObject)
        {
            for (int i = 0; i < prefabObject.prefab.elements.Count; i++)
            {
                var element = prefabObject.prefab.elements[i];
                prefabObject.ApplyElementSetting(element);

                if (element is MenuEvent menuEvent)
                {
                    yield return MenuEvent.DeepCopy(menuEvent, false);
                    continue;
                }
                if (element is MenuText menuText)
                {
                    yield return MenuText.DeepCopy(menuText, false);
                    continue;
                }
                if (element is MenuButton menuButton)
                {
                    yield return MenuButton.DeepCopy(menuButton, false);
                    continue;
                }
                yield return MenuImage.DeepCopy(element, false);
            }
        }

        public static MenuPrefab GenerateTopBarPrefab(string title)
        {
            var menuPrefab = new MenuPrefab();

            menuPrefab.elements.AddRange(GenerateTopBar(title));

            return menuPrefab;
        }

        public static MenuPrefab GenerateBottomBarPrefab()
        {
            var menuPrefab = new MenuPrefab();

            menuPrefab.elements.AddRange(GenerateBottomBar());

            return menuPrefab;
        }

        /// <summary>
        /// Automatically generates the top section of an interface (the interface title and the "----------" line)
        /// </summary>
        /// <param name="title">The name of the interface to be shown.</param>
        /// <param name="textColor"></param>
        /// <param name="textVal"></param>
        /// <returns>Returns a generated top bar.</returns>
        public static IEnumerable<MenuImage> GenerateTopBar(string title, int textColor = 0, float textVal = 40f, bool regenerate = true)
        {
            yield return new MenuText
            {
                id = "264726346",
                name = "Top Title",
                text = $"{title} | BetterLegacy {LegacyPlugin.ModVersion}",
                rect = RectValues.HorizontalAnchored.AnchoredPosition(0f, 460f).SizeDelta(100f, 100f),
                textRect = RectValues.FullAnchored.AnchoredPosition(100f, 0f),
                hideBG = true,
                textColor = textColor,
                textVal = textVal,
                length = 0.6f,
                regenerate = regenerate
            };

            yield return new MenuText
            {
                id = "800",
                name = "Top Bar",
                text = "<size=56>----------------------------------------------------------------",
                rect = RectValues.HorizontalAnchored.AnchoredPosition(0f, 400f).SizeDelta(100f, 100f),
                textRect = RectValues.FullAnchored.AnchoredPosition(80f, 0f),
                hideBG = true,
                textColor = textColor,
                textVal = textVal,
                length = 0.6f,
                regenerate = regenerate
            };
        }

        /// <summary>
        /// Automatically generates the bottom section of an interface (the "----------" line and the PA version)
        /// </summary>
        /// <param name="textColor"></param>
        /// <param name="textVal"></param>
        /// <returns>Returns a generated bottom bar.</returns>
        public static IEnumerable<MenuImage> GenerateBottomBar(int textColor = 0, float textVal = 40f, bool regenerate = true)
        {
            yield return new MenuText
            {
                id = "801",
                name = "Bottom Bar",
                text = "<size=56>----------------------------------------------------------------",
                rect = RectValues.HorizontalAnchored.AnchoredPosition(0f, -400f).SizeDelta(100f, 100f),
                textRect = RectValues.FullAnchored.AnchoredPosition(80f, 0f),
                hideBG = true,
                textColor = textColor,
                textVal = textVal,
                length = 0.6f,
                regenerate = regenerate
            };

            yield return new MenuText
            {
                id = "264726346",
                name = "Bottom Title",
                text = $"<align=right><#F05355><b>Project Arrhythmia</b></color> Unified Operating System | Version {ProjectArrhythmia.GameVersion}",
                rect = RectValues.HorizontalAnchored.AnchoredPosition(0f, -460f).SizeDelta(100f, 100f),
                textRect = RectValues.FullAnchored.AnchoredPosition(-100f, 0f),
                hideBG = true,
                textColor = textColor,
                textVal = textVal,
                length = 0.6f,
                regenerate = regenerate
            };
        }

        #endregion

        /// <summary>
        /// Plays the menus' default music.
        /// </summary>
        public void PlayDefaultMusic()
        {
            if (music)
            {
                CoreHelper.Log($"Playing AudioClip");
                InterfaceManager.inst.PlayMusic(music);
                return;
            }

            if (string.IsNullOrEmpty(musicName))
            {
                CoreHelper.LogWarning($"Music name is null or empty.\nMusic Name: {musicName}\nMusic is null: {musicName == null}\nMusic is empty: {musicName == ""}");
                return;
            }

            if (SoundManager.inst.TryGetMusic(musicName, out AudioClip audioClip))
            {
                CoreHelper.Log($"Playing from music clip groups {musicName}");
                InterfaceManager.inst.PlayMusic(audioClip);
            }
            else if (RTFile.FileExists($"{Path.GetDirectoryName(filePath)}/{musicName}.ogg"))
            {
                CoreHelper.Log($"Playing from music ogg file");
                CoreHelper.StartCoroutine(AlephNetworkManager.DownloadAudioClip($"file://{Path.GetDirectoryName(filePath)}/{musicName}.ogg", AudioType.OGGVORBIS, audioClip =>
                {
                    CoreHelper.Log($"Attempting to play music: {musicName}");
                    music = audioClip;
                    InterfaceManager.inst.PlayMusic(audioClip);
                }));
            }
            else
                CoreHelper.LogError($"No audio found with name {musicName}");
        }

        #region Generate UI

        public bool generating = false;

        /// <summary>
        /// IEnumerator Coroutine used to Generate all the menus' elements. Can be overridden if needed.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerator GenerateUI()
        {
            generating = true;

            if (regenerate || this.canvas == null || !this.canvas.GameObject)
            {
                if (this.canvas != null && this.canvas.GameObject)
                {
                    CoreHelper.Destroy(this.canvas.GameObject);
                    this.canvas = null;
                }

                selected = defaultSelection;

                InterfaceManager.inst.LoadThemes();

                var canvas = UIManager.GenerateUICanvas(nameof(CustomMenu), null, sortingOrder: layer);
                this.canvas = canvas;
                canvas.Canvas.scaleFactor = 1f;
                canvas.CanvasScaler.referenceResolution = new Vector2(1920f, 1080f);

                canvas.GameObject.AddComponent<CursorManager>();

                if (!CoreHelper.InGame && allowEffects)
                {
                    canvas.GameObject.layer = 5;
                    canvas.GameObject.AddComponent<MenuEffectsManager>();
                    canvas.Canvas.worldCamera = Camera.main;
                    canvas.Canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    yield return null;
                    canvas.Canvas.renderMode = RenderMode.WorldSpace;

                    MenuEffectsManager.inst.ResetEffects();
                    MenuEffectsManager.inst.MoveCamera(Vector2.zero);
                    MenuEffectsManager.inst.ZoomCamera(5f);
                    MenuEffectsManager.inst.RotateCamera(0f);
                }

                if (CoreHelper.InGame && pauseGame)
                    AudioManager.inst.CurrentAudioSource.Pause();
            }

            if (!CoreHelper.InGame)
                CoreHelper.UpdateDiscordStatus($"Navigating {name}", "In Menu", "menu");

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

            CoreHelper.Log("Creating layouts...");

            var gameObject = canvas.Canvas.transform.Find("Base Layout") ? canvas.Canvas.transform.Find("Base Layout").gameObject : Creator.NewUIObject("Base Layout", canvas.Canvas.transform);
            UIManager.SetRectTransform(gameObject.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);

            for (int i = 0; i < prefabs.Count; i++)
            {
                for (int j = 0; j < prefabs[i].layouts.Count; j++)
                {
                    var layout = prefabs[i].layouts.ElementAt(i);
                    if (!layouts.ContainsKey(layout.Key))
                        layouts.Add(layout.Key, layout.Value);
                }
            }

            for (int i = 0; i < layouts.Count; i++)
            {
                var layout = layouts.ElementAt(i).Value;

                if (!regenerate && !layout.regenerate && layout.gameObject)
                    continue;

                if (layout is MenuGridLayout gridLayout)
                    SetupGridLayout(gridLayout, gameObject.transform);
                if (layout is MenuHorizontalLayout horizontalLayout)
                    SetupHorizontalLayout(horizontalLayout, gameObject.transform);
                if (layout is MenuVerticalLayout verticalLayout)
                    SetupVerticalLayout(verticalLayout, gameObject.transform);
            }

            CoreHelper.Log("Creating elements...");

            int num = 0;
            int count = elements.Count;
            for (int i = 0; i < count; i++)
            {
                var element = elements[num];

                if (element is MenuPrefabObject prefabObject)
                {
                    if (prefabObject.prefab == null)
                    {
                        num++;
                        continue;
                    }

                    var list = ApplyPrefabElements(prefabObject).ToList();

                    for (int j = 0; j < list.Count; j++)
                    {
                        elements.Insert(num, list[j]);
                        count = elements.Count;
                        num++;
                    }

                    prefabObject.Spawn();
                    if (prefabObject.wait)
                        while (prefabObject.isSpawning)
                            yield return null;
                    num++;
                }
            }

            num = 0;
            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];

                //CoreHelper.Log($"Creating {element}...");

                if (element is MenuPrefabObject prefabObject)
                    continue;

                if (element is MenuEvent menuEvent)
                {
                    menuEvent.TriggerEvent();
                    if (menuEvent.wait)
                        while (menuEvent.isSpawning)
                            yield return null;

                    if (menuEvent.onWaitEndFuncJSON != null)
                        menuEvent.ParseFunction(menuEvent.onWaitEndFuncJSON);
                    menuEvent.onWaitEndFunc?.Invoke();

                    continue;
                }

                if (!regenerate && !element.regenerate && element.gameObject)
                    continue;

                var parent = GetElementParent(element, gameObject);

                if (element is MenuButton menuButton)
                {
                    SetupButton(menuButton, parent);
                    if (menuButton.siblingIndex >= 0 && menuButton.siblingIndex < menuButton.gameObject.transform.parent.childCount)
                        menuButton.gameObject.transform.SetSiblingIndex(menuButton.siblingIndex);

                    if (menuButton.autoAlignSelectionPosition && !string.IsNullOrEmpty(element.parentLayout) && layouts.TryGetValue(element.parentLayout, out MenuLayoutBase menuLayoutParent))
                    {
                        if (menuLayoutParent is MenuVerticalLayout)
                            menuButton.selectionPosition = new Vector2Int(0, num);
                        if (menuLayoutParent is MenuHorizontalLayout)
                            menuButton.selectionPosition = new Vector2Int(num, 0);

                        // idk how to handle MenuGridLayout
                    }

                    if (menuButton.wait)
                        while (menuButton.isSpawning)
                            yield return null;

                    if (menuButton.onWaitEndFuncJSON != null)
                        menuButton.ParseFunction(menuButton.onWaitEndFuncJSON);
                    menuButton.onWaitEndFunc?.Invoke();

                    if (menuButton.clickable)
                        menuButton.clickable.onClick = p =>
                        {
                            if (menuButton.playBlipSound)
                                AudioManager.inst.PlaySound("blip");

                            if (menuButton.funcJSON != null)
                                menuButton.ParseFunction(menuButton.funcJSON);
                            menuButton.func?.Invoke();
                        };

                    if (menuButton.onScrollUpFuncJSON != null || menuButton.onScrollDownFuncJSON != null || menuButton.onScrollUpFunc != null || menuButton.onScrollDownFunc != null)
                    {
                        var eventTrigger = menuButton.gameObject.AddComponent<EventTrigger>();
                        eventTrigger.triggers.Add(TriggerHelper.CreateEntry(EventTriggerType.Scroll, eventData =>
                        {
                            var pointerData = (PointerEventData)eventData;
                            if (pointerData.scrollDelta.y > 0f)
                            {
                                if (menuButton.onScrollUpFuncJSON != null)
                                    menuButton.ParseFunction(menuButton.onScrollUpFuncJSON);
                                menuButton.onScrollUpFunc?.Invoke();
                            }
                            if (pointerData.scrollDelta.y < 0f)
                            {
                                if (menuButton.onScrollDownFuncJSON != null)
                                    menuButton.ParseFunction(menuButton.onScrollDownFuncJSON);
                                menuButton.onScrollDownFunc?.Invoke();
                            }
                        }));
                    }

                    num++;
                    continue;
                }

                if (element is MenuInputField menuInputField)
                {
                    SetupInputField(menuInputField, parent);
                    if (menuInputField.siblingIndex >= 0 && menuInputField.siblingIndex < menuInputField.gameObject.transform.parent.childCount)
                        menuInputField.gameObject.transform.SetSiblingIndex(menuInputField.siblingIndex);
                    if (menuInputField.wait)
                        while (menuInputField.isSpawning)
                            yield return null;

                    if (menuInputField.onWaitEndFuncJSON != null)
                        menuInputField.ParseFunction(menuInputField.onWaitEndFuncJSON);
                    menuInputField.onWaitEndFunc?.Invoke();

                    continue;
                }

                if (element is MenuText menuText)
                {
                    SetupText(menuText, parent);
                    if (menuText.siblingIndex >= 0 && menuText.siblingIndex < menuText.gameObject.transform.parent.childCount)
                        menuText.gameObject.transform.SetSiblingIndex(menuText.siblingIndex);
                    if (menuText.wait)
                        while (menuText.isSpawning)
                            yield return null;

                    if (menuText.onWaitEndFuncJSON != null)
                        menuText.ParseFunction(menuText.onWaitEndFuncJSON);
                    menuText.onWaitEndFunc?.Invoke();
                }
                else
                {
                    SetupImage(element, parent);
                    if (element.siblingIndex >= 0 && element.siblingIndex < element.gameObject.transform.parent.childCount)
                        element.gameObject.transform.SetSiblingIndex(element.siblingIndex);
                    if (element.wait)
                        while (element.isSpawning)
                            yield return null;

                    if (element.onWaitEndFuncJSON != null)
                        element.ParseFunction(element.onWaitEndFuncJSON);
                    element.onWaitEndFunc?.Invoke();
                }

                if (element.clickable != null)
                    element.clickable.onClick = p =>
                    {
                        if (element.playBlipSound)
                            AudioManager.inst.PlaySound("blip");
                        if (element.funcJSON != null)
                            element.ParseFunction(element.funcJSON);
                        element.func?.Invoke();
                    };

                if (element.onScrollUpFuncJSON != null || element.onScrollDownFuncJSON != null || element.onScrollUpFunc != null || element.onScrollDownFunc != null)
                {
                    var eventTrigger = element.gameObject.AddComponent<EventTrigger>();
                    eventTrigger.triggers.Add(TriggerHelper.CreateEntry(EventTriggerType.Scroll, eventData =>
                    {
                        var pointerData = (PointerEventData)eventData;
                        if (pointerData.scrollDelta.y > 0f)
                        {
                            if (element.onScrollUpFuncJSON != null)
                                element.ParseFunction(element.onScrollUpFuncJSON);
                            element.onScrollUpFunc?.Invoke();
                        }
                        if (pointerData.scrollDelta.y < 0f)
                        {
                            if (element.onScrollDownFuncJSON != null)
                                element.ParseFunction(element.onScrollDownFuncJSON);
                            element.onScrollDownFunc?.Invoke();
                        }
                    }));
                }
            }

            onGenerateUIFinish?.Invoke();

            isOpen = true;

            generating = false;

            yield break;
        }

        /// <summary>
        /// Gets an elements' parent, whether it be a layout or another element.
        /// </summary>
        /// <param name="element">The element to parent.</param>
        /// <param name="defaultLayout">The default GameObject to parent to if no parent is found.</param>
        /// <returns></returns>
        public Transform GetElementParent(MenuImage element, GameObject defaultLayout) => !string.IsNullOrEmpty(element.parentLayout) && layouts.TryGetValue(element.parentLayout, out MenuLayoutBase menuLayoutParent) ? menuLayoutParent.content : !string.IsNullOrEmpty(element.parent) && elements.TryFind(x => x.id == element.parent, out MenuImage menuParent) && menuParent.gameObject ? menuParent.gameObject.transform : defaultLayout.transform;

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

            layout.rect.AssignToRectTransform(layout.gameObject.transform.AsRT());

            if (layout.scrollable)
            {
                var content = Creator.NewUIObject("Content", layout.gameObject.transform);
                layout.content = content.transform.AsRT();
                layout.contentRect.AssignToRectTransform(layout.content);

                layout.gridLayout = content.AddComponent<GridLayoutGroup>();
            }
            else
            {
                layout.content = layout.gameObject.transform.AsRT();
                layout.gridLayout = layout.gameObject.AddComponent<GridLayoutGroup>();
            }

            layout.gridLayout.cellSize = layout.cellSize;
            layout.gridLayout.spacing = layout.spacing;
            layout.gridLayout.constraintCount = layout.constraintCount;
            layout.gridLayout.constraint = layout.constraint;
            layout.gridLayout.childAlignment = layout.childAlignment;
            layout.gridLayout.startAxis = layout.startAxis;
            layout.gridLayout.startCorner = layout.startCorner;

            if (layout.mask)
            {
                layout.gameObject.AddComponent<Image>();
                layout.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            }

            SetupLayoutTriggers(layout);
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

            layout.rect.AssignToRectTransform(layout.gameObject.transform.AsRT());

            if (layout.scrollable)
            {
                var content = Creator.NewUIObject("Content", layout.gameObject.transform);
                layout.content = content.transform.AsRT();
                layout.contentRect.AssignToRectTransform(layout.content);

                layout.horizontalLayout = content.AddComponent<HorizontalLayoutGroup>();
            }
            else
            {
                layout.content = layout.gameObject.transform.AsRT();
                layout.horizontalLayout = layout.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            layout.horizontalLayout.childControlHeight = layout.childControlHeight;
            layout.horizontalLayout.childControlWidth = layout.childControlWidth;
            layout.horizontalLayout.childForceExpandHeight = layout.childForceExpandHeight;
            layout.horizontalLayout.childForceExpandWidth = layout.childForceExpandWidth;
            layout.horizontalLayout.childScaleHeight = layout.childScaleHeight;
            layout.horizontalLayout.childScaleWidth = layout.childScaleWidth;
            layout.horizontalLayout.spacing = layout.spacing;
            layout.horizontalLayout.childAlignment = layout.childAlignment;

            if (layout.mask)
            {
                layout.gameObject.AddComponent<Image>();
                layout.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            }

            SetupLayoutTriggers(layout);
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

            layout.rect.AssignToRectTransform(layout.gameObject.transform.AsRT());

            if (layout.scrollable)
            {
                var content = Creator.NewUIObject("Content", layout.gameObject.transform);
                layout.content = content.transform.AsRT();
                layout.contentRect.AssignToRectTransform(layout.content);

                layout.verticalLayout = content.AddComponent<VerticalLayoutGroup>();
            }
            else
            {
                layout.content = layout.gameObject.transform.AsRT();
                layout.verticalLayout = layout.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.verticalLayout.childControlHeight = layout.childControlHeight;
            layout.verticalLayout.childControlWidth = layout.childControlWidth;
            layout.verticalLayout.childForceExpandHeight = layout.childForceExpandHeight;
            layout.verticalLayout.childForceExpandWidth = layout.childForceExpandWidth;
            layout.verticalLayout.childScaleHeight = layout.childScaleHeight;
            layout.verticalLayout.childScaleWidth = layout.childScaleWidth;
            layout.verticalLayout.spacing = layout.spacing;
            layout.verticalLayout.childAlignment = layout.childAlignment;

            if (layout.mask)
            {
                layout.gameObject.AddComponent<Image>();
                layout.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            }

            SetupLayoutTriggers(layout);
        }

        void SetupLayoutTriggers(MenuLayoutBase layout)
        {
            if (layout.onScrollUpFunc != null || layout.onScrollDownFunc != null || layout.onScrollUpFuncJSON != null || layout.onScrollDownFuncJSON != null)
            {
                var eventTrigger = layout.gameObject.AddComponent<EventTrigger>();
                eventTrigger.triggers.Add(TriggerHelper.CreateEntry(EventTriggerType.Scroll, eventData =>
                {
                    var pointerData = (PointerEventData)eventData;
                    if (pointerData.scrollDelta.y > 0f)
                    {
                        if (layout.onScrollUpFuncJSON != null)
                            new MenuImage().ParseFunction(layout.onScrollUpFuncJSON);
                        layout.onScrollUpFunc?.Invoke();
                    }
                    if (pointerData.scrollDelta.y < 0f)
                    {
                        if (layout.onScrollDownFuncJSON != null)
                            new MenuImage().ParseFunction(layout.onScrollDownFuncJSON);
                        layout.onScrollDownFunc?.Invoke();
                    }
                }));
            }
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
            menuImage.gameObject.layer = 5;
            menuImage.image = menuImage.gameObject.AddComponent<Image>();

            menuImage.rect.AssignToRectTransform(menuImage.image.rectTransform);

            if (menuImage.funcJSON != null || menuImage.func != null)
                menuImage.clickable = menuImage.gameObject.AddComponent<Clickable>();

            if (menuImage.icon)
                menuImage.image.sprite = menuImage.icon;
            else if (RTFile.FileExists(menuImage.iconPath))
                menuImage.image.sprite = SpriteHelper.LoadSprite(menuImage.iconPath);
            else if (MenuConfig.Instance.RoundedUI.Value)
                SpriteHelper.SetRoundedSprite(menuImage.image, menuImage.rounded, menuImage.roundedSide);

            if (menuImage.reactiveSetting.init)
            {
                var reactiveAudio = menuImage.gameObject.AddComponent<MenuReactiveAudio>();
                reactiveAudio.reactiveSetting = menuImage.reactiveSetting;
                reactiveAudio.ogPosition = menuImage.rect.anchoredPosition;
            }

            if (menuImage.mask)
                menuImage.gameObject.AddComponent<Mask>();

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
            menuText.gameObject.layer = 5;
            menuText.image = menuText.gameObject.AddComponent<Image>();

            menuText.rect.AssignToRectTransform(menuText.image.rectTransform);

            menuText.image.enabled = !menuText.hideBG;
            if (!menuText.hideBG && MenuConfig.Instance.RoundedUI.Value)
                SpriteHelper.SetRoundedSprite(menuText.image, menuText.rounded, menuText.roundedSide);

            if (menuText.funcJSON != null || menuText.func != null)
                menuText.clickable = menuText.gameObject.AddComponent<Clickable>();

            var t = UIManager.GenerateUITextMeshPro("Text", menuText.gameObject.transform);
            ((RectTransform)t["RectTransform"]).anchoredPosition = Vector2.zero;
            ((RectTransform)t["RectTransform"]).localRotation = Quaternion.identity;
            menuText.textUI = (TextMeshProUGUI)t["Text"];
            menuText.textUI.gameObject.layer = 5;
            menuText.text = ParseText(menuText.text);
            menuText.textUI.text = menuText.text;
            menuText.textUI.maxVisibleCharacters = 0;
            menuText.textUI.enableWordWrapping = menuText.enableWordWrapping;
            menuText.textUI.alignment = menuText.alignment;
            menuText.textUI.overflowMode = menuText.overflowMode;

            menuText.textRect.AssignToRectTransform(menuText.textUI.rectTransform);

            if (menuText.icon)
            {
                var icon = Creator.NewUIObject("Icon", menuText.gameObject.transform);
                icon.layer = 5;
                menuText.iconUI = icon.AddComponent<Image>();
                menuText.iconUI.sprite = menuText.icon;
                menuText.iconRect.AssignToRectTransform(menuText.iconUI.rectTransform);
            }
            else if (RTFile.FileExists(menuText.iconPath))
            {
                var icon = Creator.NewUIObject("Icon", menuText.gameObject.transform);
                icon.layer = 5;
                menuText.iconUI = icon.AddComponent<Image>();
                menuText.iconUI.sprite = SpriteHelper.LoadSprite(menuText.iconPath);
                menuText.iconRect.AssignToRectTransform(menuText.iconUI.rectTransform);
            }

            if (menuText.reactiveSetting.init)
            {
                var reactiveAudio = menuText.gameObject.AddComponent<MenuReactiveAudio>();
                reactiveAudio.reactiveSetting = menuText.reactiveSetting;
                reactiveAudio.ogPosition = menuText.rect.anchoredPosition;
            }

            if (menuText.mask)
                menuText.gameObject.AddComponent<Mask>();

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
            menuButton.gameObject.layer = 5;
            menuButton.image = menuButton.gameObject.AddComponent<Image>();

            menuButton.rect.AssignToRectTransform(menuButton.image.rectTransform);

            menuButton.image.enabled = !menuButton.hideBG;
            if (!menuButton.hideBG && MenuConfig.Instance.RoundedUI.Value)
                SpriteHelper.SetRoundedSprite(menuButton.image, menuButton.rounded, menuButton.roundedSide);

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
            ((RectTransform)t["RectTransform"]).localRotation = Quaternion.identity;
            menuButton.textUI = (TextMeshProUGUI)t["Text"];
            menuButton.textUI.gameObject.layer = 5;
            menuButton.text = ParseText(menuButton.text);
            menuButton.textUI.text = menuButton.text;
            menuButton.textUI.maxVisibleCharacters = 0;
            menuButton.textUI.enableWordWrapping = menuButton.enableWordWrapping;
            menuButton.textUI.alignment = menuButton.alignment;
            menuButton.textUI.overflowMode = menuButton.overflowMode;

            menuButton.textRect.AssignToRectTransform(menuButton.textUI.rectTransform);

            if (menuButton.icon)
            {
                var icon = Creator.NewUIObject("Icon", menuButton.gameObject.transform);
                icon.layer = 5;
                menuButton.iconUI = icon.AddComponent<Image>();
                menuButton.iconUI.sprite = menuButton.icon;
                menuButton.iconRect.AssignToRectTransform(menuButton.iconUI.rectTransform);
            }
            else if (RTFile.FileExists(menuButton.iconPath))
            {
                var icon = Creator.NewUIObject("Icon", menuButton.gameObject.transform);
                icon.layer = 5;
                menuButton.iconUI = icon.AddComponent<Image>();
                menuButton.iconUI.sprite = SpriteHelper.LoadSprite(menuButton.iconPath);
                menuButton.iconRect.AssignToRectTransform(menuButton.iconUI.rectTransform);
            }

            if (menuButton.reactiveSetting.init)
            {
                var reactiveAudio = menuButton.gameObject.AddComponent<MenuReactiveAudio>();
                reactiveAudio.reactiveSetting = menuButton.reactiveSetting;
                reactiveAudio.ogPosition = menuButton.rect.anchoredPosition;
            }

            if (menuButton.mask)
                menuButton.gameObject.AddComponent<Mask>();

            if (menuButton.spawnFuncJSON != null)
                menuButton.ParseFunction(menuButton.spawnFuncJSON);
            menuButton.spawnFunc?.Invoke();

            menuButton.Spawn();
        }

        /// <summary>
        /// Initializes a <see cref="MenuInputField"/>'s UI.
        /// </summary>
        /// <param name="menuInputField">The element to generate UI for.</param>
        /// <param name="parent">The parent to set the element to.</param>
        public void SetupInputField(MenuInputField menuInputField, Transform parent)
        {
            if (menuInputField.gameObject)
                UnityEngine.Object.Destroy(menuInputField.gameObject);

            menuInputField.inputField = UIManager.GenerateInputField(menuInputField.name, parent);

            menuInputField.gameObject = menuInputField.inputField.gameObject;
            menuInputField.gameObject.layer = 5;
            menuInputField.image = menuInputField.inputField.image;

            menuInputField.rect.AssignToRectTransform(menuInputField.image.rectTransform);

            if (menuInputField.icon)
                menuInputField.image.sprite = menuInputField.icon;
            else if (RTFile.FileExists(menuInputField.iconPath))
                menuInputField.image.sprite = SpriteHelper.LoadSprite(menuInputField.iconPath);
            else if (MenuConfig.Instance.RoundedUI.Value)
                SpriteHelper.SetRoundedSprite(menuInputField.image, menuInputField.rounded, menuInputField.roundedSide);

            if (menuInputField.reactiveSetting.init)
            {
                var reactiveAudio = menuInputField.gameObject.AddComponent<MenuReactiveAudio>();
                reactiveAudio.reactiveSetting = menuInputField.reactiveSetting;
                reactiveAudio.ogPosition = menuInputField.rect.anchoredPosition;
            }

            menuInputField.inputField.onValueChanged.ClearAll();
            menuInputField.inputField.onEndEdit.ClearAll();
            menuInputField.inputField.GetPlaceholderText().text = menuInputField.placeholder;
            menuInputField.inputField.text = menuInputField.text;
            menuInputField.inputField.textComponent.alignment = menuInputField.textAnchor;
            menuInputField.inputField.GetPlaceholderText().alignment = menuInputField.placeholderAnchor;
            menuInputField.inputField.textComponent.fontSize = menuInputField.textFontSize;
            menuInputField.inputField.GetPlaceholderText().fontSize = menuInputField.placeholderFontSize;
            menuInputField.inputField.onValueChanged.AddListener(menuInputField.Write);
            menuInputField.inputField.onEndEdit.AddListener(menuInputField.Finish);

            if (menuInputField.triggers != null)
                TriggerHelper.AddEventTriggers(menuInputField.gameObject, menuInputField.triggers);

            if (menuInputField.mask)
                menuInputField.gameObject.AddComponent<Mask>();

            if (menuInputField.spawnFuncJSON != null)
                menuInputField.ParseFunction(menuInputField.spawnFuncJSON);
            menuInputField.spawnFunc?.Invoke();

            menuInputField.Spawn();
        }

        public static string ParseText(string input)
        {
            RTString.RegexMatches(input, new Regex(@"{{Date=(.*?)}}"), match =>
            {
                input = input.Replace(match.Groups[0].ToString(), DateTime.Now.ToString(match.Groups[1].ToString()));
            });

            RTString.RegexMatches(input, new Regex(@"{{LevelRank=([0-9]+)}}"), match =>
            {
                DataManager.LevelRank levelRank =
                    LevelManager.Levels.TryFind(x => x.id == match.Groups[1].ToString(), out Level level) ? LevelManager.GetLevelRank(level) :
                    CoreHelper.InEditor ?
                        LevelManager.EditorRank :
                        DataManager.inst.levelRanks[0];

                input = input.Replace(match.Groups[0].ToString(), RTString.FormatLevelRank(levelRank));
            });

            RTString.RegexMatches(input, new Regex(@"{{StoryLevelRank=([0-9]+)}}"), match =>
            {
                DataManager.LevelRank levelRank =
                    StoryManager.inst.Saves.TryFind(x => x.ID == match.Groups[1].ToString(), out LevelManager.PlayerData playerData) ? LevelManager.GetLevelRank(playerData) :
                    CoreHelper.InEditor ?
                        LevelManager.EditorRank :
                        DataManager.inst.levelRanks[0];

                input = input.Replace(match.Groups[0].ToString(), RTString.FormatLevelRank(levelRank));
            });

            RTString.RegexMatches(input, new Regex(@"{{LoadStoryString=(.*?),(.*?)}}"), match =>
            {
                input = input.Replace(match.Groups[0].ToString(), StoryManager.inst.LoadString(match.Groups[1].ToString(), match.Groups[2].ToString()));
            });

            RTString.RegexMatches(input, new Regex(@"{{RandomNumber=([0-9]+)}}"), match =>
            {
                input = input.Replace(match.Groups[0].ToString(), LSText.randomNumString(Parser.TryParse(match.Groups[1].ToString(), 0)));
            });

            RTString.RegexMatches(input, new Regex(@"{{RandomText=([0-9]+)}}"), match =>
            {
                input = input.Replace(match.Groups[0].ToString(), LSText.randomString(Parser.TryParse(match.Groups[1].ToString(), 0)));
            });

            return input
                .Replace("{{CurrentPlayingChapterNumber}}", (StoryManager.inst.currentPlayingChapterIndex + 1).ToString("00"))
                .Replace("{{CurrentPlayingLevelNumber}}", (StoryManager.inst.currentPlayingLevelSequenceIndex + 1).ToString("00"))
                .Replace("{{SaveSlotNumber}}", (StoryManager.inst.SaveSlot + 1).ToString("00"))
                ;
        }

        #endregion

        /// <summary>
        /// Clears the menu.
        /// </summary>
        public virtual void Clear()
        {
            isOpen = false;
            selected = Vector2Int.zero;

            for (int i = 0; i < elements.Count; i++)
                elements[i]?.Clear();

            UnityEngine.Object.Destroy(canvas?.GameObject);
            if (loopingEvents != null)
                AnimationManager.inst.Remove(loopingEvents.id);
            if (spawnEvents != null)
                AnimationManager.inst.Remove(spawnEvents.id);
        }

        /// <summary>
        /// Control system.
        /// </summary>
        public virtual void UpdateControls()
        {
            var actions = InputDataManager.inst.menuActions;

            if (!isOpen || generating)
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
                if (selected.x > elements.Where(x => x is MenuButton menuButton && menuButton.selectionPosition.y == selected.y).Select(x => x as MenuButton).Min(x => x.selectionPosition.x))
                {
                    AudioManager.inst.PlaySound("LeftRight");

                    int num = 1;
                    while (!elements.Has(x => x is MenuButton menuButton && menuButton.selectionPosition.y == selected.y && menuButton.selectionPosition.x == selected.x - num))
                    {
                        num++;
                    }
                    selected.x -= num;
                }
                else
                    AudioManager.inst.PlaySound("Block");
            }

            if (actions.Right.WasPressed)
            {
                if (selected.x < elements.FindAll(x => x is MenuButton menuButton && menuButton.selectionPosition.y == selected.y).Select(x => x as MenuButton).Max(x => x.selectionPosition.x))
                {
                    AudioManager.inst.PlaySound("LeftRight");

                    int num = 1;
                    while (!elements.Has(x => x is MenuButton menuButton && menuButton.selectionPosition.y == selected.y && menuButton.selectionPosition.x == selected.x + num))
                    {
                        num++;
                    }
                    selected.x += num;
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
                    if (button.gameObject.activeInHierarchy && actions.Submit.WasPressed)
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

            if (actions.Cancel.WasPressed)
            {
                exitFunc?.Invoke();

                if (exitFuncJSON != null)
                {
                    new MenuEvent().ParseFunction(exitFuncJSON);
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
                    button.image.color = isSelected ?
                        LSColors.fadeColor(
                            CoreHelper.ChangeColorHSV(
                                button.useOverrideSelectedColor ? button.overrideSelectedColor : Theme.GetObjColor(button.selectedColor),
                                button.selectedHue,
                                button.selectedSat,
                                button.selectedVal),
                            button.selectedOpacity) :
                        LSColors.fadeColor(
                            CoreHelper.ChangeColorHSV(
                                button.useOverrideColor ? button.overrideColor : Theme.GetObjColor(button.color),
                                button.hue,
                                button.sat,
                                button.val),
                            button.opacity); ;
                    button.textUI.color = isSelected ?
                        CoreHelper.ChangeColorHSV(
                            button.useOverrideSelectedTextColor ? button.overrideSelectedTextColor : Theme.GetObjColor(button.selectedTextColor),
                            button.selectedTextHue,
                            button.selectedTextSat,
                            button.selectedTextVal) :
                        CoreHelper.ChangeColorHSV(
                            button.useOverrideTextColor ? button.overrideTextColor : Theme.GetObjColor(button.textColor),
                            button.textHue,
                            button.textSat,
                            button.textVal);
                    continue;
                }

                if (element is MenuText text)
                {
                    text.textUI.color =
                        CoreHelper.ChangeColorHSV(
                            text.useOverrideTextColor ? text.overrideTextColor : Theme.GetObjColor(text.textColor),
                            text.textHue,
                            text.textSat,
                            text.textVal);
                    text.UpdateText();
                }

                if (element is MenuInputField inputField)
                {
                    inputField.inputField.textComponent.color =
                        CoreHelper.ChangeColorHSV(
                            inputField.useOverrideTextColor ? inputField.overrideTextColor : Theme.GetObjColor(inputField.textColor),
                            inputField.textHue,
                            inputField.textSat,
                            inputField.textVal);
                    inputField.inputField.GetPlaceholderText().color =
                        LSColors.fadeColor(CoreHelper.ChangeColorHSV(
                            inputField.useOverridePlaceholderColor ? inputField.overridePlaceholderColor : Theme.GetObjColor(inputField.placeholderColor),
                            inputField.placeholderHue,
                            inputField.placeholderSat,
                            inputField.placeholderVal), 0.3f);
                }

                element.image.color =
                    LSColors.fadeColor(
                        CoreHelper.ChangeColorHSV(
                            element.useOverrideColor ? element.overrideColor : Theme.GetObjColor(element.color),
                            element.hue,
                            element.sat,
                            element.val),
                        element.opacity);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// The current theme to use for the menu colors.
        /// </summary>
        public BeatmapTheme Theme { get; set; }

        #endregion
    }
}
