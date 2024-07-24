using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Editor.Managers;
using HarmonyLib;
using LSFunctions;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BaseBackgroundObject = DataManager.GameData.BackgroundObject;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(BackgroundEditor))]
    public class BackgroundEditorPatch : MonoBehaviour
    {
        public static BackgroundEditor Instance { get => BackgroundEditor.inst; set => BackgroundEditor.inst = value; }

        public static BackgroundObject CurrentSelectedBG => BackgroundEditor.inst == null ? null : (BackgroundObject)DataManager.inst.gameData.backgroundObjects[BackgroundEditor.inst.currentObj];

        // need to somehow clean up this messy code
        [HarmonyPatch(nameof(BackgroundEditor.Awake))]
        [HarmonyPrefix]
        static bool AwakePrefix(BackgroundEditor __instance)
        {
            if (Instance == null)
                BackgroundEditor.inst = __instance;
            else if (Instance != __instance)
            {
                Destroy(__instance.gameObject);
                return false;
            }

            CoreHelper.LogInit(__instance.className);

            BackgroundEditorManager.Init(__instance);

            var dialog = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/BackgroundDialog").transform;
            var bgRight = dialog.Find("data/right").gameObject;
            var bgLeft = dialog.Find("data/left").gameObject;

            #region Right

            var createTip = bgRight.transform.Find("create").GetComponent<HoverTooltip>();
            var createTooltip = new HoverTooltip.Tooltip();
            createTooltip.desc = "Create New Background Object";
            createTooltip.hint = "Press this to create a new background object.";
            createTip.tooltipLangauges.Add(createTooltip);

            var destroyAll = bgRight.transform.Find("create").gameObject.Duplicate(bgRight.transform, "destroy", 2);
            destroyAll.transform.localScale = Vector3.one;

            var destroyAllText = destroyAll.transform.GetChild(0).GetComponent<Text>();
            destroyAllText.text = "Delete All Backgrounds";
            destroyAll.transform.GetChild(0).localScale = Vector3.one;

            var destroyAllButtons = destroyAll.GetComponent<Button>();
            destroyAllButtons.onClick.ClearAll();
            destroyAllButtons.onClick.AddListener(() =>
            {
                if (DataManager.inst.gameData.backgroundObjects.Count <= 1)
                {
                    EditorManager.inst.DisplayNotification("Cannot delete only background object.", 2f, EditorManager.NotificationType.Warning);
                    return;
                }

                RTEditor.inst.ShowWarningPopup("Are you sure you want to delete all backgrounds?", () =>
                {
                    BackgroundEditorManager.inst.DeleteAllBackgrounds();
                    EditorManager.inst.HideDialog("Warning Popup");
                }, () => { EditorManager.inst.HideDialog("Warning Popup"); });
            });

            var destroyAllTip = destroyAll.GetComponent<HoverTooltip>();
            var destroyAllTooltip = new HoverTooltip.Tooltip();
            destroyAllTooltip.desc = "Destroy All Objects";
            destroyAllTooltip.hint = "Press this to destroy all background objects, EXCEPT the first one.";
            destroyAllTip.tooltipLangauges.Clear();
            destroyAllTip.tooltipLangauges.Add(destroyAllTooltip);

            var createBGs = Instantiate(bgLeft.transform.Find("name").gameObject);
            createBGs.transform.SetParent(bgRight.transform);
            createBGs.transform.localScale = Vector3.one;
            createBGs.transform.SetSiblingIndex(2);
            createBGs.name = "create bgs";

            var name = createBGs.transform.Find("name").GetComponent<InputField>();
            var nameRT = name.GetComponent<RectTransform>();

            name.onValueChanged.ClearAll();

            Destroy(createBGs.transform.Find("active").gameObject);
            nameRT.localScale = Vector3.one;
            name.text = "12";
            name.characterValidation = InputField.CharacterValidation.Integer;
            nameRT.sizeDelta = new Vector2(80f, 34f);

            var createAll = bgRight.transform.Find("create").gameObject.Duplicate(createBGs.transform, "create");
            createAll.transform.localScale = Vector3.one;

            createAll.transform.AsRT().sizeDelta = new Vector2(278f, 34f);
            var createAllText = createAll.transform.GetChild(0).GetComponent<Text>();
            createAllText.text = "Create Backgrounds";
            createAll.transform.GetChild(0).localScale = Vector3.one;

            var buttonCreate = createAll.GetComponent<Button>();
            buttonCreate.onClick.ClearAll();
            buttonCreate.onClick.AddListener(() =>
            {
                if (int.TryParse(name.text, out int result) && result >= 0)
                    BackgroundEditorManager.inst.CreateBackgrounds(result);
            });

            bgRight.transform.Find("backgrounds").AsRT().sizeDelta = new Vector2(366f, 524f);

            #region Editor Themes

            EditorThemeManager.AddGraphic(dialog.GetComponent<Image>(), ThemeGroup.Background_1);
            EditorThemeManager.AddGraphic(bgRight.GetComponent<Image>(), ThemeGroup.Background_3);
            EditorThemeManager.AddInputField(bgRight.transform.Find("search").GetComponent<InputField>(), ThemeGroup.Search_Field_2);
            EditorThemeManager.AddScrollbar(bgRight.transform.Find("backgrounds/Scrollbar Vertical").GetComponent<Scrollbar>(), scrollbarGroup: ThemeGroup.Scrollbar_2, handleGroup: ThemeGroup.Scrollbar_2_Handle);
            EditorThemeManager.AddGraphic(bgRight.transform.Find("create").GetComponent<Image>(), ThemeGroup.Add, true);
            EditorThemeManager.AddGraphic(bgRight.transform.Find("create").GetChild(0).GetComponent<Text>(), ThemeGroup.Add_Text);
            EditorThemeManager.AddInputField(name);
            EditorThemeManager.AddGraphic(buttonCreate.image, ThemeGroup.Add, true);
            EditorThemeManager.AddGraphic(createAllText, ThemeGroup.Add_Text);
            EditorThemeManager.AddGraphic(destroyAllButtons.image, ThemeGroup.Delete, true);
            EditorThemeManager.AddGraphic(destroyAllText, ThemeGroup.Delete_Text);

            #endregion

            #endregion

            #region Left

            //Set UI Parents
            {
                var listtoadd = new List<Transform>();
                for (int i = 0; i < bgLeft.transform.childCount; i++)
                    listtoadd.Add(bgLeft.transform.GetChild(i));

                var bmb = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View");

                var e = Instantiate(bmb);

                var scrollView2 = e.transform;

                scrollView2.SetParent(bgLeft.transform);
                scrollView2.localScale = Vector3.one;
                scrollView2.name = "Object Scroll View";

                var content = scrollView2.Find("Viewport/Content");
                var contentChildren = new List<Transform>();
                for (int i = 0; i < content.childCount; i++)
                    contentChildren.Add(content.GetChild(i));

                foreach (var child in contentChildren)
                {
                    DestroyImmediate(child.gameObject);
                }

                int num = 0;
                while (num < 20)
                    num++;

                var scrollViewRT = scrollView2.GetComponent<RectTransform>();
                scrollViewRT.anchoredPosition = new Vector2(188f, -353f);
                scrollViewRT.sizeDelta = new Vector2(370f, 690f);

                foreach (var l in listtoadd)
                {
                    l.SetParent(content);
                    l.transform.localScale = Vector3.one;
                }

                __instance.left = content;
            }

            __instance.right = __instance.dialog.Find("data/right");

            // Adjustments
            {
                var position = __instance.left.Find("position");
                var scale = __instance.left.Find("scale");

                DestroyImmediate(position.GetComponent<HorizontalLayoutGroup>());
                DestroyImmediate(scale.GetComponent<HorizontalLayoutGroup>());

                position.Find("x").GetComponent<HorizontalLayoutGroup>().spacing = 4f;
                position.Find("y").GetComponent<HorizontalLayoutGroup>().spacing = 4f;
                position.Find("x/text-field").AsRT().sizeDelta = new Vector2(125f, 32f);
                position.Find("y/text-field").AsRT().sizeDelta = new Vector2(125f, 32f);

                scale.Find("x").GetComponent<HorizontalLayoutGroup>().spacing = 4f;
                scale.Find("y").GetComponent<HorizontalLayoutGroup>().spacing = 4f;
                scale.Find("x/text-field").AsRT().sizeDelta = new Vector2(125f, 32f);
                scale.Find("y/text-field").AsRT().sizeDelta = new Vector2(125f, 32f);

                __instance.left.Find("color").GetComponent<GridLayoutGroup>().spacing = new Vector2(7.7f, 0f);

                var rotSlider = __instance.left.Find("rotation/slider").GetComponent<Slider>();
                rotSlider.maxValue = 360f;
                rotSlider.minValue = -360f;
            }

            var label = __instance.left.GetChild(10).gameObject;

            var shape = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/shape");
            var shapeOption = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/shapesettings");

            var labelShape = Instantiate(label);
            labelShape.transform.SetParent(__instance.left);
            labelShape.transform.localScale = Vector3.one;
            labelShape.transform.SetSiblingIndex(12);
            labelShape.name = "label";
            labelShape.transform.GetChild(0).GetComponent<Text>().text = "Shape";

            var shapeBG = Instantiate(shape);
            shapeBG.transform.SetParent(__instance.left);
            shapeBG.transform.localScale = Vector3.one;
            shapeBG.transform.SetSiblingIndex(13);
            shapeBG.name = "shape";

            var shapeOptionBG = Instantiate(shapeOption);
            shapeOptionBG.transform.SetParent(__instance.left);
            shapeOptionBG.transform.localScale = Vector3.one;
            shapeOptionBG.transform.SetSiblingIndex(14);
            shapeOptionBG.name = "shapesettings";
            var shapeSettings = shapeOptionBG.transform;

            // Depth
            {
                DestroyImmediate(__instance.left.Find("depth").gameObject);

                var iterations = Instantiate(__instance.left.Find("position").gameObject);
                iterations.transform.SetParent(__instance.left);
                iterations.transform.localScale = Vector3.one;
                iterations.name = "depth";
                DestroyImmediate(iterations.transform.GetChild(1).gameObject);
                iterations.transform.SetSiblingIndex(3);

                var xif = iterations.transform.Find("x").GetComponent<InputField>();

                xif.onValueChanged.ClearAll();
                xif.onValueChanged.AddListener(_val =>
                {
                    if (int.TryParse(_val, out int num))
                    {
                        CurrentSelectedBG.layer = num;
                        BackgroundManager.inst.UpdateBackgrounds();
                    }
                });

                TriggerHelper.IncreaseDecreaseButtons(xif);
            }

            // Iterations
            {
                var iLabel = Instantiate(label);
                iLabel.transform.SetParent(__instance.left);
                iLabel.transform.localScale = Vector3.one;
                iLabel.name = "label";
                iLabel.transform.GetChild(0).GetComponent<Text>().text = "Iterations";
                iLabel.transform.SetSiblingIndex(4);

                var iterations = Instantiate(__instance.left.Find("position").gameObject);
                iterations.transform.SetParent(__instance.left);
                iterations.transform.localScale = Vector3.one;
                iterations.name = "iterations";
                DestroyImmediate(iterations.transform.GetChild(1).gameObject);
                iterations.transform.SetSiblingIndex(5);

                var x = iterations.transform.Find("x");
                var xif = x.GetComponent<InputField>();

                xif.onValueChanged.ClearAll();
                xif.onValueChanged.AddListener(_val =>
                {
                    if (int.TryParse(_val, out int num))
                    {
                        CurrentSelectedBG.depth = num;
                        BackgroundManager.inst.UpdateBackgrounds();
                    }
                });

                TriggerHelper.IncreaseDecreaseButtonsInt(xif);
                TriggerHelper.AddEventTriggerParams(x.gameObject, TriggerHelper.ScrollDeltaInt(xif));
            }

            // ZPosition
            {
                var iLabel = Instantiate(label);
                iLabel.transform.SetParent(__instance.left);
                iLabel.transform.localScale = Vector3.one;
                iLabel.name = "label";
                iLabel.transform.GetChild(0).GetComponent<Text>().text = "Z Position";
                iLabel.transform.SetSiblingIndex(6);

                var iterations = Instantiate(__instance.left.Find("position").gameObject);
                iterations.transform.SetParent(__instance.left);
                iterations.transform.localScale = Vector3.one;
                iterations.name = "zposition";
                DestroyImmediate(iterations.transform.GetChild(1).gameObject);
                iterations.transform.SetSiblingIndex(7);

                var x = iterations.transform.Find("x");
                var xif = x.GetComponent<InputField>();
                var left = x.Find("<").GetComponent<Button>();
                var right = x.Find(">").GetComponent<Button>();

                xif.onValueChanged.ClearAll();
                xif.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        CurrentSelectedBG.zposition = num;
                        BackgroundManager.inst.UpdateBackgrounds();
                    }
                });

                TriggerHelper.IncreaseDecreaseButtons(xif);
                TriggerHelper.AddEventTriggerParams(x.gameObject, TriggerHelper.ScrollDelta(xif));
            }
            
            // ZScale
            {
                var iLabel = Instantiate(label);
                iLabel.transform.SetParent(__instance.left);
                iLabel.transform.localScale = Vector3.one;
                iLabel.name = "label";
                iLabel.transform.GetChild(0).GetComponent<Text>().text = "Z Scale";
                iLabel.transform.SetSiblingIndex(8);

                var iterations = Instantiate(__instance.left.Find("position").gameObject);
                iterations.transform.SetParent(__instance.left);
                iterations.transform.localScale = Vector3.one;
                iterations.name = "zscale";
                DestroyImmediate(iterations.transform.GetChild(1).gameObject);
                iterations.transform.SetSiblingIndex(9);

                var x = iterations.transform.Find("x");
                var xif = x.GetComponent<InputField>();
                var left = x.Find("<").GetComponent<Button>();
                var right = x.Find(">").GetComponent<Button>();

                xif.onValueChanged.ClearAll();
                xif.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        CurrentSelectedBG.zscale = float.Parse(_val);
                        BackgroundManager.inst.UpdateBackgrounds();
                    }
                });

                TriggerHelper.IncreaseDecreaseButtons(xif);
                TriggerHelper.AddEventTriggerParams(x.gameObject, TriggerHelper.ScrollDelta(xif));
            }

            // Reactive
            {
                var reactiveRanges = __instance.left.Find("reactive-ranges");

                reactiveRanges.GetComponent<GridLayoutGroup>().cellSize = new Vector2(62f, 32f);

                var custom = Instantiate(reactiveRanges.GetChild(3).gameObject);
                custom.transform.SetParent(reactiveRanges);
                custom.transform.localScale = Vector3.one;
                custom.name = "custom";
                custom.transform.GetChild(1).GetComponent<Text>().text = "Custom";

                var toggle = custom.GetComponent<Toggle>();
                toggle.onValueChanged.ClearAll();
                toggle.onValueChanged.AddListener(delegate (bool _val)
                {
                    if (_val && CurrentSelectedBG != null)
                    {
                        CurrentSelectedBG.reactiveType = (BaseBackgroundObject.ReactiveType)3;
                        CurrentSelectedBG.reactive = true;
                    }
                });

                var reactive = __instance.left.Find("reactive");
                var slider = reactive.Find("slider").GetComponent<RectTransform>();
                slider.sizeDelta = new Vector2(205f, 32f);

                // Reactive Position
                {
                    // Samples
                    {
                        var iLabel = Instantiate(label);
                        iLabel.transform.SetParent(__instance.left);
                        iLabel.transform.localScale = Vector3.one;
                        iLabel.name = "label";
                        iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Position Samples";
                        iLabel.transform.SetSiblingIndex(24);

                        var position = Instantiate(__instance.left.Find("position").gameObject);
                        position.transform.SetParent(__instance.left);
                        position.transform.localScale = Vector3.one;
                        position.name = "reactive-position-samples";
                        position.transform.SetSiblingIndex(25);

                        var xif = position.transform.Find("x").GetComponent<InputField>();
                        var yif = position.transform.Find("y").GetComponent<InputField>();

                        xif.onValueChanged.ClearAll();
                        xif.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                CurrentSelectedBG.reactivePosSamples.x = num;
                                BackgroundManager.inst.UpdateBackgrounds();
                            }
                        });

                        yif.onValueChanged.ClearAll();
                        yif.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                CurrentSelectedBG.reactivePosSamples.y = num;
                                BackgroundManager.inst.UpdateBackgrounds();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtonsInt(xif, max: 255);
                        TriggerHelper.IncreaseDecreaseButtonsInt(yif, max: 255);
                    }

                    // Intensity
                    {
                        var iLabel = Instantiate(label);
                        iLabel.transform.SetParent(__instance.left);
                        iLabel.transform.localScale = Vector3.one;
                        iLabel.name = "label";
                        iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Position Intensity";
                        iLabel.transform.SetSiblingIndex(26);

                        var position = Instantiate(__instance.left.Find("position").gameObject);
                        position.transform.SetParent(__instance.left);
                        position.transform.localScale = Vector3.one;
                        position.name = "reactive-position-intensity";
                        position.transform.SetSiblingIndex(27);

                        var xif = position.transform.Find("x").GetComponent<InputField>();
                        var yif = position.transform.Find("y").GetComponent<InputField>();

                        xif.onValueChanged.ClearAll();
                        xif.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                CurrentSelectedBG.reactivePosIntensity.x = num;
                                BackgroundManager.inst.UpdateBackgrounds();
                            }
                        });

                        yif.onValueChanged.ClearAll();
                        yif.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                CurrentSelectedBG.reactivePosIntensity.y = num;
                                BackgroundManager.inst.UpdateBackgrounds();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(xif, max: 255);
                        TriggerHelper.IncreaseDecreaseButtons(yif, max: 255);
                    }
                }

                // Reactive Scale
                {
                    // Samples
                    {
                        var iLabel = Instantiate(label);
                        iLabel.transform.SetParent(__instance.left);
                        iLabel.transform.localScale = Vector3.one;
                        iLabel.name = "label";
                        iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Scale Samples";
                        iLabel.transform.SetSiblingIndex(28);

                        var position = Instantiate(__instance.left.Find("position").gameObject);
                        position.transform.SetParent(__instance.left);
                        position.transform.localScale = Vector3.one;
                        position.name = "reactive-scale-samples";
                        position.transform.SetSiblingIndex(29);

                        var xif = position.transform.Find("x").GetComponent<InputField>();
                        var yif = position.transform.Find("y").GetComponent<InputField>();

                        xif.onValueChanged.ClearAll();
                        xif.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                CurrentSelectedBG.reactiveScaSamples.x = num;
                                BackgroundManager.inst.UpdateBackgrounds();
                            }
                        });

                        yif.onValueChanged.ClearAll();
                        yif.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                CurrentSelectedBG.reactiveScaSamples.y = num;
                                BackgroundManager.inst.UpdateBackgrounds();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtonsInt(xif, max: 255);
                        TriggerHelper.IncreaseDecreaseButtonsInt(yif, max: 255);
                    }

                    // Intensity
                    {
                        var iLabel = Instantiate(label);
                        iLabel.transform.SetParent(__instance.left);
                        iLabel.transform.localScale = Vector3.one;
                        iLabel.name = "label";
                        iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Scale Intensity";
                        iLabel.transform.SetSiblingIndex(30);

                        var position = Instantiate(__instance.left.Find("position").gameObject);
                        position.transform.SetParent(__instance.left);
                        position.transform.localScale = Vector3.one;
                        position.name = "reactive-scale-intensity";
                        position.transform.SetSiblingIndex(31);

                        var xif = position.transform.Find("x").GetComponent<InputField>();
                        var yif = position.transform.Find("y").GetComponent<InputField>();

                        xif.onValueChanged.ClearAll();
                        xif.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                CurrentSelectedBG.reactiveScaIntensity.x = num;
                                BackgroundManager.inst.UpdateBackgrounds();
                            }
                        });

                        yif.onValueChanged.ClearAll();
                        yif.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                CurrentSelectedBG.reactiveScaIntensity.y = num;
                                BackgroundManager.inst.UpdateBackgrounds();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(xif, max: 255);
                        TriggerHelper.IncreaseDecreaseButtons(yif, max: 255);
                    }
                }

                // Reactive Rotation
                {
                    // Samples
                    {
                        var iLabel = Instantiate(label);
                        iLabel.transform.SetParent(__instance.left);
                        iLabel.transform.localScale = Vector3.one;
                        iLabel.name = "label";
                        iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Rotation Sample";
                        iLabel.transform.SetSiblingIndex(32);

                        var position = Instantiate(__instance.left.Find("position").gameObject);
                        position.transform.SetParent(__instance.left);
                        position.transform.localScale = Vector3.one;
                        position.name = "reactive-rotation-sample";
                        position.transform.SetSiblingIndex(33);

                        DestroyImmediate(position.transform.Find("y").gameObject);

                        var xif = position.transform.Find("x").GetComponent<InputField>();

                        xif.onValueChanged.ClearAll();
                        xif.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                CurrentSelectedBG.reactiveRotSample = num;
                                BackgroundManager.inst.UpdateBackgrounds();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtonsInt(xif, max: 255);
                    }

                    // Intensity
                    {
                        var iLabel = Instantiate(label);
                        iLabel.transform.SetParent(__instance.left);
                        iLabel.transform.localScale = Vector3.one;
                        iLabel.name = "label";
                        iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Rotation Intensity";
                        iLabel.transform.SetSiblingIndex(34);

                        var position = Instantiate(__instance.left.Find("position").gameObject);
                        position.transform.SetParent(__instance.left);
                        position.transform.localScale = Vector3.one;
                        position.name = "reactive-rotation-intensity";
                        position.transform.SetSiblingIndex(35);

                        DestroyImmediate(position.transform.Find("y").gameObject);

                        var xif = position.transform.Find("x").GetComponent<InputField>();

                        xif.onValueChanged.ClearAll();
                        xif.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                CurrentSelectedBG.reactiveRotIntensity = num;
                                BackgroundManager.inst.UpdateBackgrounds();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(xif, max: 255);
                    }
                }

                // Reactive Color
                {
                    // Samples
                    {
                        var iLabel = Instantiate(label);
                        iLabel.transform.SetParent(__instance.left);
                        iLabel.transform.localScale = Vector3.one;
                        iLabel.name = "label";
                        iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Color Sample";
                        iLabel.transform.SetSiblingIndex(36);

                        var position = Instantiate(__instance.left.Find("position").gameObject);
                        position.transform.SetParent(__instance.left);
                        position.transform.localScale = Vector3.one;
                        position.name = "reactive-color-sample";
                        position.transform.SetSiblingIndex(37);

                        DestroyImmediate(position.transform.Find("y").gameObject);

                        var xif = position.transform.Find("x").GetComponent<InputField>();

                        xif.onValueChanged.ClearAll();
                        xif.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                CurrentSelectedBG.reactiveColSample = num;
                                BackgroundManager.inst.UpdateBackgrounds();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtonsInt(xif, max: 255);
                    }

                    // Intensity
                    {
                        var iLabel = Instantiate(label);
                        iLabel.transform.SetParent(__instance.left);
                        iLabel.transform.localScale = Vector3.one;
                        iLabel.name = "label";
                        iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Color Intensity";
                        iLabel.transform.SetSiblingIndex(38);

                        var position = Instantiate(__instance.left.Find("position").gameObject);
                        position.transform.SetParent(__instance.left);
                        position.transform.localScale = Vector3.one;
                        position.name = "reactive-color-intensity";
                        position.transform.SetSiblingIndex(39);

                        DestroyImmediate(position.transform.Find("y").gameObject);

                        var xif = position.transform.Find("x").GetComponent<InputField>();

                        xif.onValueChanged.ClearAll();
                        xif.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                CurrentSelectedBG.reactiveColIntensity = num;
                                BackgroundManager.inst.UpdateBackgrounds();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(xif, max: 255);
                    }

                    // Reactive Color
                    {
                        var colorLabel = Instantiate(label);
                        colorLabel.transform.SetParent(__instance.left);
                        colorLabel.transform.localScale = Vector3.one;
                        colorLabel.name = "label";
                        colorLabel.transform.SetSiblingIndex(40);
                        colorLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Color";

                        var color = __instance.left.Find("color");
                        var fadeColor = Instantiate(color.gameObject);
                        fadeColor.transform.SetParent(__instance.left);
                        fadeColor.transform.localScale = Vector3.one;
                        fadeColor.name = "reactive-color";
                        fadeColor.transform.SetSiblingIndex(41);
                    }
                }

                // Reactive Z
                {
                    // Samples
                    {
                        var iLabel = Instantiate(label);
                        iLabel.transform.SetParent(__instance.left);
                        iLabel.transform.localScale = Vector3.one;
                        iLabel.name = "label";
                        iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Z Sample";
                        iLabel.transform.SetSiblingIndex(42);

                        var position = Instantiate(__instance.left.Find("position").gameObject);
                        position.transform.SetParent(__instance.left);
                        position.transform.localScale = Vector3.one;
                        position.name = "reactive-z-sample";
                        position.transform.SetSiblingIndex(43);

                        DestroyImmediate(position.transform.Find("y").gameObject);

                        var xif = position.transform.Find("x").GetComponent<InputField>();

                        xif.onValueChanged.ClearAll();
                        xif.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                CurrentSelectedBG.reactiveColSample = num;
                                BackgroundManager.inst.UpdateBackgrounds();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtonsInt(xif, max: 255);
                    }

                    // Intensity
                    {
                        var iLabel = Instantiate(label);
                        iLabel.transform.SetParent(__instance.left);
                        iLabel.transform.localScale = Vector3.one;
                        iLabel.name = "label";
                        iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Z Intensity";
                        iLabel.transform.SetSiblingIndex(44);

                        var position = Instantiate(__instance.left.Find("position").gameObject);
                        position.transform.SetParent(__instance.left);
                        position.transform.localScale = Vector3.one;
                        position.name = "reactive-z-intensity";
                        position.transform.SetSiblingIndex(45);

                        DestroyImmediate(position.transform.Find("y").gameObject);

                        var xif = position.transform.Find("x").GetComponent<InputField>();

                        xif.onValueChanged.ClearAll();
                        xif.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                CurrentSelectedBG.reactiveZIntensity = num;
                                BackgroundManager.inst.UpdateBackgrounds();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(xif, max: 255);
                    }
                }
            }

            // Fade Color
            {
                var colorLabel = Instantiate(label);
                colorLabel.transform.SetParent(__instance.left);
                colorLabel.transform.localScale = Vector3.one;
                colorLabel.name = "label";
                colorLabel.transform.SetSiblingIndex(16);
                colorLabel.transform.GetChild(0).GetComponent<Text>().text = "Fade Color";

                var color = __instance.left.Find("color");
                var fadeColor = Instantiate(color.gameObject);
                fadeColor.transform.SetParent(__instance.left);
                fadeColor.transform.localScale = Vector3.one;
                fadeColor.name = "fade-color";
                fadeColor.transform.SetSiblingIndex(17);
            }

            // Rotation
            {
                var index = __instance.left.Find("rotation").GetSiblingIndex();

                var iLabel = Instantiate(label);
                iLabel.transform.SetParent(__instance.left);
                iLabel.transform.localScale = Vector3.one;
                iLabel.name = "label";
                iLabel.transform.GetChild(0).GetComponent<Text>().text = "3D Rotation";
                iLabel.transform.SetSiblingIndex(index - 1);

                var iterations = Instantiate(__instance.left.Find("position").gameObject);
                iterations.transform.SetParent(__instance.left);
                iterations.transform.localScale = Vector3.one;
                iterations.name = "depth-rotation";
                iterations.transform.SetSiblingIndex(index);

                var xif = iterations.transform.Find("x").GetComponent<InputField>();

                xif.onValueChanged.ClearAll();
                xif.onValueChanged.AddListener(delegate (string _val)
                {
                    if (float.TryParse(_val, out float num))
                    {
                        CurrentSelectedBG.rotation.x = num;
                        BackgroundManager.inst.UpdateBackgrounds();
                    }
                });

                TriggerHelper.IncreaseDecreaseButtons(xif, 15f, 3f);

                var yif = iterations.transform.Find("y").GetComponent<InputField>();

                yif.onValueChanged.ClearAll();
                yif.onValueChanged.AddListener(delegate (string _val)
                {
                    if (float.TryParse(_val, out float num))
                    {
                        CurrentSelectedBG.rotation.y = num;
                        BackgroundManager.inst.UpdateBackgrounds();
                    }
                });

                TriggerHelper.IncreaseDecreaseButtons(yif, 15f, 3f);
            }

            // Hue / Sat / Val (Fade)
            {
                var colorLabel = Instantiate(label);
                colorLabel.transform.SetParent(__instance.left);
                colorLabel.transform.localScale = Vector3.one;
                colorLabel.name = "label";
                colorLabel.transform.SetSiblingIndex(20);
                colorLabel.AddComponent<HorizontalLayoutGroup>();

                var label1 = colorLabel.transform.GetChild(0);
                label1.GetComponent<Text>().text = "Hue";
                var label2 = label1.gameObject.Duplicate(colorLabel.transform, "text");
                label2.GetComponent<Text>().text = "Saturation";
                var label3 = label1.gameObject.Duplicate(colorLabel.transform, "text");
                label3.GetComponent<Text>().text = "Value";

                var iterations = Instantiate(__instance.left.Find("position").gameObject);
                iterations.transform.SetParent(__instance.left);
                iterations.transform.localScale = Vector3.one;
                iterations.name = "fadehuesatval";
                iterations.transform.SetSiblingIndex(21);

                // Hue
                {
                    var x = iterations.transform.Find("x");
                    var xif = x.GetComponent<InputField>();
                    x.transform.GetChild(0).AsRT().sizeDelta = new Vector2(70f, 32f);
                    xif.image = x.transform.GetChild(0).GetComponent<Image>();

                    xif.onValueChanged.ClearAll();
                    xif.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            CurrentSelectedBG.fadeHue = num;
                        }
                    });

                    TriggerHelper.IncreaseDecreaseButtons(xif);
                }

                // Saturation
                {
                    var x = iterations.transform.Find("y");
                    var xif = x.GetComponent<InputField>();
                    x.transform.AsRT().anchoredPosition = new Vector2(120f, 0f);
                    x.transform.GetChild(0).AsRT().sizeDelta = new Vector2(70f, 32f);
                    xif.image = x.transform.GetChild(0).GetComponent<Image>();

                    xif.onValueChanged.ClearAll();
                    xif.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            CurrentSelectedBG.fadeSaturation = num;
                        }
                    });

                    TriggerHelper.IncreaseDecreaseButtons(xif);
                }

                // Value
                {
                    var x = iterations.transform.Find("x").gameObject.Duplicate(iterations.transform, "z");
                    var xif = x.GetComponent<InputField>();
                    x.transform.AsRT().anchoredPosition = new Vector2(240f, 0f);
                    x.transform.GetChild(0).AsRT().sizeDelta = new Vector2(70f, 32f);
                    xif.image = x.transform.GetChild(0).GetComponent<Image>();

                    xif.onValueChanged.ClearAll();
                    xif.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            CurrentSelectedBG.fadeValue = num;
                        }
                    });

                    TriggerHelper.IncreaseDecreaseButtons(xif);
                }
            }
            
            // Hue / Sat / Val (Color)
            {
                var colorLabel = Instantiate(label);
                colorLabel.transform.SetParent(__instance.left);
                colorLabel.transform.localScale = Vector3.one;
                colorLabel.name = "label";
                colorLabel.transform.SetSiblingIndex(24);
                colorLabel.AddComponent<HorizontalLayoutGroup>();

                var label1 = colorLabel.transform.GetChild(0);
                label1.GetComponent<Text>().text = "Hue";
                var label2 = label1.gameObject.Duplicate(colorLabel.transform, "text");
                label2.GetComponent<Text>().text = "Saturation";
                var label3 = label1.gameObject.Duplicate(colorLabel.transform, "text");
                label3.GetComponent<Text>().text = "Value";

                var iterations = Instantiate(__instance.left.Find("position").gameObject);
                iterations.transform.SetParent(__instance.left);
                iterations.transform.localScale = Vector3.one;
                iterations.name = "huesatval";
                iterations.transform.SetSiblingIndex(25);

                // Hue
                {
                    var x = iterations.transform.Find("x");
                    var xif = x.GetComponent<InputField>();
                    x.transform.GetChild(0).AsRT().sizeDelta = new Vector2(70f, 32f);
                    xif.image = x.transform.GetChild(0).GetComponent<Image>();

                    xif.onValueChanged.ClearAll();
                    xif.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            CurrentSelectedBG.hue = num;
                        }
                    });

                    TriggerHelper.IncreaseDecreaseButtons(xif);
                }

                // Saturation
                {
                    var x = iterations.transform.Find("y");
                    var xif = x.GetComponent<InputField>();
                    x.transform.AsRT().anchoredPosition = new Vector2(120f, 0f);
                    x.transform.GetChild(0).AsRT().sizeDelta = new Vector2(70f, 32f);
                    xif.image = x.transform.GetChild(0).GetComponent<Image>();

                    xif.onValueChanged.ClearAll();
                    xif.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            CurrentSelectedBG.saturation = num;
                        }
                    });

                    TriggerHelper.IncreaseDecreaseButtons(xif);
                }

                // Value
                {
                    var x = iterations.transform.Find("x").gameObject.Duplicate(iterations.transform, "z");
                    var xif = x.GetComponent<InputField>();
                    x.transform.AsRT().anchoredPosition = new Vector2(240f, 0f);
                    x.transform.GetChild(0).AsRT().sizeDelta = new Vector2(70f, 32f);
                    xif.image = x.transform.GetChild(0).GetComponent<Image>();

                    xif.onValueChanged.ClearAll();
                    xif.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            CurrentSelectedBG.value = num;
                        }
                    });

                    TriggerHelper.IncreaseDecreaseButtons(xif);
                }
            }

            // Modifiers
            {
                var eventButton = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/event");

                var iLabel = Instantiate(label);
                iLabel.transform.SetParent(__instance.left);
                iLabel.transform.localScale = Vector3.one;
                iLabel.name = "label";
                iLabel.transform.GetChild(0).GetComponent<Text>().text = "Modifier Blocks";

                var iterations = Instantiate(__instance.left.Find("position").gameObject);
                iterations.transform.SetParent(__instance.left);
                iterations.transform.localScale = Vector3.one;
                iterations.name = "block";
                DestroyImmediate(iterations.transform.GetChild(1).gameObject);

                var addBlock = eventButton.Duplicate(iterations.transform.Find("x"), "add");
                addBlock.transform.localScale = Vector3.one;
                addBlock.transform.AsRT().sizeDelta = new Vector2(80f, 32f);

                var addBlockText = addBlock.transform.GetChild(0).GetComponent<Text>();
                addBlockText.text = "Add";

                var removeBlock = eventButton.Duplicate(iterations.transform.Find("x"), "del");
                removeBlock.transform.localScale = Vector3.one;
                removeBlock.transform.AsRT().sizeDelta = new Vector2(80f, 32f);

                var removeBlockText = removeBlock.transform.GetChild(0).GetComponent<Text>();
                removeBlockText.text = "Del";

                EditorThemeManager.AddGraphic(addBlock.GetComponent<Image>(), ThemeGroup.Add, true);
                EditorThemeManager.AddGraphic(addBlockText, ThemeGroup.Add_Text);
                EditorThemeManager.AddGraphic(removeBlock.GetComponent<Image>(), ThemeGroup.Delete, true);
                EditorThemeManager.AddGraphic(removeBlockText, ThemeGroup.Delete_Text);

                BackgroundEditorManager.inst.CreateModifiersOnAwake();
                BackgroundEditorManager.inst.CreateDefaultModifiersList();

                EditorThemeManager.AddInputFields(__instance.left.Find("block").gameObject, true, "Background Editor Reactive");
            }

            var active = __instance.left.Find("name/active").GetComponent<Toggle>();
            Destroy(active.GetComponent<Animator>());
            active.transition = Selectable.Transition.ColorTint;
            active.colors = UIManager.SetColorBlock(active.colors, Color.white, new Color(0.7f, 0.7f, 0.7f), new Color(0.7f, 0.7f, 0.7f), new Color(0.7f, 0.7f, 0.7f), new Color(0.7f, 0.7f, 0.7f));
            EditorThemeManager.AddToggle(active);
            __instance.left.Find("name/name").AsRT().sizeDelta = new Vector2(300f, 32f);
            EditorThemeManager.AddInputField(__instance.left.Find("name/name").GetComponent<InputField>());
            EditorThemeManager.AddInputFields(__instance.left.Find("depth").gameObject, true, "Background Editor Depth");
            EditorThemeManager.AddInputFields(__instance.left.Find("iterations").gameObject, true, "Background Editor Iterations");
            EditorThemeManager.AddInputFields(__instance.left.Find("zposition").gameObject, true, "");
            EditorThemeManager.AddInputFields(__instance.left.Find("zscale").gameObject, true, "Background Editor Z Scale");
            EditorThemeManager.AddInputFields(__instance.left.Find("position").gameObject, true, "Background Editor Position");
            EditorThemeManager.AddInputFields(__instance.left.Find("scale").gameObject, true, "Background Editor Scale");
            EditorThemeManager.AddInputFields(__instance.left.Find("depth-rotation").gameObject, true, "Background Editor 3D Rotation");
            EditorThemeManager.AddInputField(__instance.left.Find("rotation/x").GetComponent<InputField>());

            EditorThemeManager.AddInputFields(__instance.left.Find("fadehuesatval").gameObject, true, "");
            EditorThemeManager.AddInputFields(__instance.left.Find("huesatval").gameObject, true, "");

            var rotationSliderImage = __instance.left.Find("rotation/slider/Image").GetComponent<Image>();
            var rotationSlider = __instance.left.Find("rotation/slider").GetComponent<Slider>();
            rotationSlider.colors = UIManager.SetColorBlock(rotationSlider.colors, Color.white, new Color(0.9f, 0.9f, 0.9f), Color.white, Color.white, Color.white);
            rotationSlider.transform.AsRT().sizeDelta = new Vector2(207f, 32f);

            EditorThemeManager.AddSlider(rotationSlider, rotationSliderImage);

            for (int i = 0; i < __instance.left.Find("reactive-ranges").childCount; i++)
            {
                var child = __instance.left.Find("reactive-ranges").GetChild(i);
                var toggle = child.GetComponent<Toggle>();
                var background = toggle.image;
                var checkmark = toggle.graphic;

                EditorThemeManager.AddGraphic(background, ThemeGroup.Function_2_Normal, true);
                EditorThemeManager.AddGraphic(checkmark, ThemeGroup.Function_2_Highlighted);
                EditorThemeManager.AddGraphic(child.Find("Label").GetComponent<Text>(), ThemeGroup.Function_2_Text);
            }

            EditorThemeManager.AddInputField(__instance.left.Find("reactive/x").GetComponent<InputField>());

            var reactiveSliderImage = __instance.left.Find("reactive/slider/Image").GetComponent<Image>();
            var reactiveSlider = __instance.left.Find("reactive/slider").GetComponent<Slider>();
            reactiveSlider.colors = UIManager.SetColorBlock(reactiveSlider.colors, Color.white, new Color(0.9f, 0.9f, 0.9f), Color.white, Color.white, Color.white);
            reactiveSlider.transform.AsRT().sizeDelta = new Vector2(207f, 32f);

            EditorThemeManager.AddSlider(reactiveSlider, reactiveSliderImage);

            EditorThemeManager.AddInputFields(__instance.left.Find("reactive-position-samples").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(__instance.left.Find("reactive-position-intensity").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(__instance.left.Find("reactive-scale-samples").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(__instance.left.Find("reactive-scale-intensity").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(__instance.left.Find("reactive-rotation-sample").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(__instance.left.Find("reactive-rotation-intensity").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(__instance.left.Find("reactive-color-sample").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(__instance.left.Find("reactive-color-intensity").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(__instance.left.Find("reactive-z-sample").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(__instance.left.Find("reactive-z-intensity").gameObject, true, "Background Editor Reactive");

            var fade = __instance.left.Find("fade");
            var fadeToggle = fade.GetComponent<Toggle>();
            var fadeBackground = fadeToggle.image;
            var fadeCheckmark = fadeToggle.graphic;

            EditorThemeManager.AddGraphic(fadeBackground, ThemeGroup.Function_2_Normal, true);
            EditorThemeManager.AddGraphic(fadeCheckmark, ThemeGroup.Function_2_Highlighted, true);
            EditorThemeManager.AddGraphic(fade.Find("Label").GetComponent<Text>(), ThemeGroup.Function_2_Text, true);

            // Labels
            for (int i = 0; i < __instance.left.childCount; i++)
            {
                var child = __instance.left.GetChild(i);
                if (child.name != "label")
                    continue;

                for (int j = 0; j < child.childCount; j++)
                    EditorThemeManager.AddLightText(child.GetChild(j).GetComponent<Text>());
            }

            #endregion

            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.OpenDialog))]
        [HarmonyPrefix]
        static bool OpenDialogPrefix(int __0)
        {
            BackgroundEditorManager.inst.OpenDialog(__0);
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.CreateNewBackground))]
        [HarmonyPrefix]
        static bool CreateNewBackgroundPrefix()
        {
            var backgroundObject = new BackgroundObject
            {
                name = "Background",
                pos = Vector2.zero,
                scale = new Vector2(2f, 2f),
            };

            DataManager.inst.gameData.backgroundObjects.Add(backgroundObject);

            BackgroundManager.inst.CreateBackgroundObject(backgroundObject);
            Instance.SetCurrentBackground(DataManager.inst.gameData.backgroundObjects.Count - 1);
            Instance.OpenDialog(DataManager.inst.gameData.backgroundObjects.Count - 1);

            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.UpdateBackgroundList))]
        [HarmonyPrefix]
        static bool UpdateBackgroundListPrefix()
        {
            var parent = Instance.right.Find("backgrounds/viewport/content");
            LSHelpers.DeleteChildren(parent);
            int num = 0;
            foreach (var backgroundObject in DataManager.inst.gameData.backgroundObjects)
            {
                if (!CoreHelper.SearchString(Instance.sortedName, backgroundObject.name))
                {
                    num++;
                    continue;
                }

                int index = num;
                var gameObject = Instance.backgroundButtonPrefab.Duplicate(parent, $"BG {index}");
                gameObject.transform.localScale = Vector3.one;

                var name = gameObject.transform.Find("name").GetComponent<Text>();
                var text = gameObject.transform.Find("pos").GetComponent<Text>();
                var image = gameObject.transform.Find("color").GetComponent<Image>();

                name.text = backgroundObject.name;
                text.text = $"({backgroundObject.pos.x}, {backgroundObject.pos.y})";

                image.color = GameManager.inst.LiveTheme.GetBGColor(backgroundObject.color);

                var button = gameObject.GetComponent<Button>();
                button.onClick.AddListener(() => { Instance.SetCurrentBackground(index); });

                EditorThemeManager.ApplyGraphic(button.image, ThemeGroup.List_Button_2_Normal, true);
                EditorThemeManager.ApplyGraphic(image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(name, ThemeGroup.List_Button_2_Text);
                EditorThemeManager.ApplyGraphic(text, ThemeGroup.List_Button_2_Text);

                num++;
            }

            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.UpdateBackground))]
        [HarmonyPrefix]
        static bool UpdateBackgroundPrefix(int __0)
        {
            var backgroundObject = (BackgroundObject)DataManager.inst.gameData.backgroundObjects[__0];

            if (backgroundObject.BaseObject)
                Destroy(backgroundObject.BaseObject);

            Updater.CreateBackgroundObject(backgroundObject);

            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.CopyBackground))]
        [HarmonyPrefix]
        static bool CopyBackgroundPrefix()
        {
            CoreHelper.Log($"Copied Background Object");
            Instance.backgroundObjCopy = BackgroundObject.DeepCopy((BackgroundObject)DataManager.inst.gameData.backgroundObjects[Instance.currentObj]);
            Instance.hasCopiedObject = true;

            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.DeleteBackground))]
        [HarmonyPrefix]
        static bool DeleteBackgroundPrefix(ref string __result, int __0)
        {
            if (DataManager.inst.gameData.backgroundObjects.Count <= 1)
            {
                EditorManager.inst.DisplayNotification("Unable to delete last background element! Consider moving it off screen or turning it into your first background element for the level.", 2f, EditorManager.NotificationType.Error, false);
                __result = null;
                return false;
            }

            string name = DataManager.inst.gameData.backgroundObjects[__0].name;
            DataManager.inst.gameData.backgroundObjects.RemoveAt(__0);

            if (DataManager.inst.gameData.backgroundObjects.Count > 0)
                Instance.SetCurrentBackground(Mathf.Clamp(Instance.currentObj - 1, 0, DataManager.inst.gameData.backgroundObjects.Count - 1));

            BackgroundManager.inst.UpdateBackgrounds();

            __result = name;
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.PasteBackground))]
        [HarmonyPrefix]
        static bool PasteBackgroundPrefix(ref string __result)
        {
            if (!Instance.hasCopiedObject || Instance.backgroundObjCopy == null)
            {
                EditorManager.inst.DisplayNotification("No copied background yet!", 2f, EditorManager.NotificationType.Error);
                __result = "";
                return false;
            }

            var backgroundObject = BackgroundObject.DeepCopy((BackgroundObject)Instance.backgroundObjCopy);
            int currentBackground = DataManager.inst.gameData.backgroundObjects.Count;
            DataManager.inst.gameData.backgroundObjects.Add(backgroundObject);

            BackgroundManager.inst.CreateBackgroundObject(backgroundObject);
            Instance.SetCurrentBackground(currentBackground);
            __result = backgroundObject.name.ToString();

            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetRot), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool SetRotPrefix(string __0)
        {
            if (float.TryParse(__0, out float rot))
            {
                DataManager.inst.gameData.backgroundObjects[Instance.currentObj].rot = rot;
                Instance.left.Find("rotation/slider").GetComponent<Slider>().value = rot;
                Instance.UpdateBackground(Instance.currentObj);
            }

            return false;
        }
    }
}
