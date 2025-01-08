using BetterLegacy.Editor.Components;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Core.Prefabs;
using LSFunctions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using BaseBackgroundObject = DataManager.GameData.BackgroundObject;
using BetterLegacy.Core.Components;
using BetterLegacy.Editor.Data;

namespace BetterLegacy.Editor.Managers
{
    public class RTBackgroundEditor : MonoBehaviour
    {
        public static RTBackgroundEditor inst;

        public static BackgroundObject CurrentSelectedBG => BackgroundEditor.inst == null || BackgroundEditor.inst.currentObj < 0 || BackgroundEditor.inst.currentObj >= GameData.Current.backgroundObjects.Count ? null : GameData.Current.backgroundObjects[BackgroundEditor.inst.currentObj];

        public List<BackgroundObject> copiedBackgroundObjects = new List<BackgroundObject>();

        public static void Init() => BackgroundEditor.inst.gameObject.AddComponent<RTBackgroundEditor>();

        public GameObject shapeButtonCopy;

        void Awake()
        {
            inst = this;
            StartCoroutine(SetupUI());
        }

        IEnumerator SetupUI()
        {
            var dialog = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/BackgroundDialog").transform;
            var bgRight = dialog.Find("data/right").gameObject;
            var bgLeft = dialog.Find("data/left").gameObject;

            #region Right

            bgRight.transform.Find("create").GetComponent<HoverTooltip>().tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = "Create New Background Object",
                hint = "Press this to create a new background object."
            });

            var destroyAll = bgRight.transform.Find("create").gameObject.Duplicate(bgRight.transform, "destroy", 2);
            destroyAll.transform.localScale = Vector3.one;

            var destroyAllText = destroyAll.transform.GetChild(0).GetComponent<Text>();
            destroyAllText.text = "Delete All Backgrounds";
            destroyAll.transform.GetChild(0).localScale = Vector3.one;

            var destroyAllButtons = destroyAll.GetComponent<Button>();
            destroyAllButtons.onClick.ClearAll();
            destroyAllButtons.onClick.AddListener(() =>
            {
                if (GameData.Current.backgroundObjects.Count <= 1)
                {
                    EditorManager.inst.DisplayNotification("Cannot delete only background object.", 2f, EditorManager.NotificationType.Warning);
                    return;
                }

                RTEditor.inst.ShowWarningPopup("Are you sure you want to delete all backgrounds?", () =>
                {
                    DeleteAllBackgrounds();
                    RTEditor.inst.HideWarningPopup();
                }, RTEditor.inst.HideWarningPopup);
            });

            var destroyAllTip = destroyAll.GetComponent<HoverTooltip>();

            destroyAllTip.tooltipLangauges.Clear();
            destroyAllTip.tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = "Destroy All Objects",
                hint = "Press this to destroy all background objects, EXCEPT the first one."
            });

            var copy = bgRight.transform.Find("create").gameObject.Duplicate(bgRight.transform, "copy", 3);
            copy.transform.localScale = Vector3.one;

            var copyText = copy.transform.GetChild(0).GetComponent<Text>();
            copyText.text = "Copy Backgrounds";
            copy.transform.GetChild(0).localScale = Vector3.one;

            var copyButtons = copy.GetComponent<Button>();
            copyButtons.onClick.ClearAll();
            copyButtons.onClick.AddListener(() =>
            {
                copiedBackgroundObjects.Clear();
                copiedBackgroundObjects.AddRange(GameData.Current.backgroundObjects.Select(x => BackgroundObject.DeepCopy(x)));
                EditorManager.inst.DisplayNotification("Copied all Background Objects.", 2f, EditorManager.NotificationType.Success);
            });

            var copyTip = copy.GetComponent<HoverTooltip>();

            copyTip.tooltipLangauges.Clear();
            copyTip.tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = "Copy all backgrounds",
                hint = "Copies all backgrounds."
            });

            var paste = bgRight.transform.Find("create").gameObject.Duplicate(bgRight.transform, "paste", 4);
            paste.transform.localScale = Vector3.one;

            var pasteText = paste.transform.GetChild(0).GetComponent<Text>();
            pasteText.text = "Paste Backgrounds";
            paste.transform.GetChild(0).localScale = Vector3.one;

            var pasteButtons = paste.GetComponent<Button>();
            pasteButtons.onClick.ClearAll();
            pasteButtons.onClick.AddListener(() =>
            {
                if (copiedBackgroundObjects == null || copiedBackgroundObjects.Count < 1)
                    return;

                var overwrite = EditorConfig.Instance.PasteBackgroundObjectsOverwrites.Value;
                if (overwrite)
                {
                    for (int i = GameData.Current.backgroundObjects.Count - 1; i >= 0; i--)
                    {
                        var backgroundObject = GameData.Current.backgroundObjects[i];
                        Updater.DestroyBackgroundObject(backgroundObject);
                        GameData.Current.backgroundObjects.RemoveAt(i);
                    }
                }

                for (int i = 0; i < copiedBackgroundObjects.Count; i++)
                    GameData.Current.backgroundObjects.Add(BackgroundObject.DeepCopy(copiedBackgroundObjects[i]));

                BackgroundManager.inst.UpdateBackgrounds();
                BackgroundEditor.inst.UpdateBackgroundList();
                EditorManager.inst.DisplayNotification($"Pasted all copied Background Objects into level{(overwrite ? " and cleared the original list." : "")}.", 2f, EditorManager.NotificationType.Success);
            });

            var pasteTip = paste.GetComponent<HoverTooltip>();

            pasteTip.tooltipLangauges.Clear();
            pasteTip.tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = "Paste backgrounds",
                hint = "Pastes all backgrounds from copied Backgrounds list."
            });

            var createBGs = bgLeft.transform.Find("name").gameObject.Duplicate(bgRight.transform, "create bgs", 2);

            var name = createBGs.transform.Find("name").GetComponent<InputField>();

            name.onValueChanged.ClearAll();

            Destroy(createBGs.transform.Find("active").gameObject);
            name.transform.localScale = Vector3.one;
            name.text = "12";
            name.characterValidation = InputField.CharacterValidation.Integer;
            name.transform.AsRT().sizeDelta = new Vector2(80f, 34f);

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
                    CreateBackgrounds(result);
            });

            bgRight.transform.Find("backgrounds").AsRT().sizeDelta = new Vector2(366f, 440f);

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
            EditorThemeManager.AddGraphic(copyButtons.image, ThemeGroup.Copy, true);
            EditorThemeManager.AddGraphic(copyText, ThemeGroup.Copy_Text);
            EditorThemeManager.AddGraphic(pasteButtons.image, ThemeGroup.Paste, true);
            EditorThemeManager.AddGraphic(pasteText, ThemeGroup.Paste_Text);

            #endregion

            #endregion

            #region Left

            //Set UI Parents
            {
                var listtoadd = new List<Transform>();
                for (int i = 0; i < bgLeft.transform.childCount; i++)
                    listtoadd.Add(bgLeft.transform.GetChild(i));

                var e = EditorPrefabHolder.Instance.ScrollView.Duplicate(bgLeft.transform, "Object Scroll View");

                var scrollView2 = e.transform;

                var content = scrollView2.Find("Viewport/Content");

                var scrollViewRT = scrollView2.AsRT();
                scrollViewRT.anchoredPosition = new Vector2(188f, -353f);
                scrollViewRT.sizeDelta = new Vector2(370f, 690f);

                foreach (var l in listtoadd)
                {
                    l.SetParent(content);
                    l.transform.localScale = Vector3.one;
                }

                BackgroundEditor.inst.left = content;
            }

            BackgroundEditor.inst.right = BackgroundEditor.inst.dialog.Find("data/right");

            // Adjustments
            {
                var position = BackgroundEditor.inst.left.Find("position");
                var scale = BackgroundEditor.inst.left.Find("scale");

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

                BackgroundEditor.inst.left.Find("color").GetComponent<GridLayoutGroup>().spacing = new Vector2(7.7f, 0f);

                var rotSlider = BackgroundEditor.inst.left.Find("rotation/slider").GetComponent<Slider>();
                rotSlider.maxValue = 360f;
                rotSlider.minValue = -360f;
            }

            var label = BackgroundEditor.inst.left.GetChild(10).gameObject;

            var shape = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/shape");
            var shapeOption = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/shapesettings");

            var labelShape = Instantiate(label);
            labelShape.transform.SetParent(BackgroundEditor.inst.left);
            labelShape.transform.localScale = Vector3.one;
            labelShape.transform.SetSiblingIndex(12);
            labelShape.name = "label";
            labelShape.transform.GetChild(0).GetComponent<Text>().text = "Shape";

            var shapeBG = Instantiate(shape);
            shapeBG.transform.SetParent(BackgroundEditor.inst.left);
            shapeBG.transform.localScale = Vector3.one;
            shapeBG.transform.SetSiblingIndex(13);
            shapeBG.name = "shape";

            var shapeOptionBG = Instantiate(shapeOption);
            shapeOptionBG.transform.SetParent(BackgroundEditor.inst.left);
            shapeOptionBG.transform.localScale = Vector3.one;
            shapeOptionBG.transform.SetSiblingIndex(14);
            shapeOptionBG.name = "shapesettings";
            var shapeSettings = shapeOptionBG.transform;

            // Depth
            {
                DestroyImmediate(BackgroundEditor.inst.left.Find("depth").gameObject);

                var iterations = Instantiate(BackgroundEditor.inst.left.Find("position").gameObject);
                iterations.transform.SetParent(BackgroundEditor.inst.left);
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
                iLabel.transform.SetParent(BackgroundEditor.inst.left);
                iLabel.transform.localScale = Vector3.one;
                iLabel.name = "label";
                iLabel.transform.GetChild(0).GetComponent<Text>().text = "Iterations";
                iLabel.transform.SetSiblingIndex(4);

                var iterations = Instantiate(BackgroundEditor.inst.left.Find("position").gameObject);
                iterations.transform.SetParent(BackgroundEditor.inst.left);
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
                TriggerHelper.AddEventTriggers(x.gameObject, TriggerHelper.ScrollDeltaInt(xif));
            }

            // ZPosition
            {
                var iLabel = Instantiate(label);
                iLabel.transform.SetParent(BackgroundEditor.inst.left);
                iLabel.transform.localScale = Vector3.one;
                iLabel.name = "label";
                iLabel.transform.GetChild(0).GetComponent<Text>().text = "Z Position";
                iLabel.transform.SetSiblingIndex(6);

                var iterations = Instantiate(BackgroundEditor.inst.left.Find("position").gameObject);
                iterations.transform.SetParent(BackgroundEditor.inst.left);
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
                TriggerHelper.AddEventTriggers(x.gameObject, TriggerHelper.ScrollDelta(xif));
            }

            // ZScale
            {
                var iLabel = Instantiate(label);
                iLabel.transform.SetParent(BackgroundEditor.inst.left);
                iLabel.transform.localScale = Vector3.one;
                iLabel.name = "label";
                iLabel.transform.GetChild(0).GetComponent<Text>().text = "Z Scale";
                iLabel.transform.SetSiblingIndex(8);

                var iterations = Instantiate(BackgroundEditor.inst.left.Find("position").gameObject);
                iterations.transform.SetParent(BackgroundEditor.inst.left);
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
                TriggerHelper.AddEventTriggers(x.gameObject, TriggerHelper.ScrollDelta(xif));
            }

            // Reactive
            {
                var reactiveRanges = BackgroundEditor.inst.left.Find("reactive-ranges");

                reactiveRanges.GetComponent<GridLayoutGroup>().cellSize = new Vector2(62f, 32f);

                var custom = Instantiate(reactiveRanges.GetChild(3).gameObject);
                custom.transform.SetParent(reactiveRanges);
                custom.transform.localScale = Vector3.one;
                custom.name = "custom";
                custom.transform.GetChild(1).GetComponent<Text>().text = "Custom";

                var toggle = custom.GetComponent<Toggle>();
                toggle.onValueChanged.ClearAll();
                toggle.onValueChanged.AddListener(_val =>
                {
                    if (_val && CurrentSelectedBG != null)
                    {
                        CurrentSelectedBG.reactiveType = (BaseBackgroundObject.ReactiveType)3;
                        CurrentSelectedBG.reactive = true;
                    }
                });

                var reactive = BackgroundEditor.inst.left.Find("reactive");
                var slider = reactive.Find("slider").GetComponent<RectTransform>();
                slider.sizeDelta = new Vector2(205f, 32f);

                // Reactive Position
                {
                    // Samples
                    {
                        var iLabel = Instantiate(label);
                        iLabel.transform.SetParent(BackgroundEditor.inst.left);
                        iLabel.transform.localScale = Vector3.one;
                        iLabel.name = "label";
                        iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Position Samples";
                        iLabel.transform.SetSiblingIndex(24);

                        var position = Instantiate(BackgroundEditor.inst.left.Find("position").gameObject);
                        position.transform.SetParent(BackgroundEditor.inst.left);
                        position.transform.localScale = Vector3.one;
                        position.name = "reactive-position-samples";
                        position.transform.SetSiblingIndex(25);

                        var xif = position.transform.Find("x").GetComponent<InputField>();
                        var yif = position.transform.Find("y").GetComponent<InputField>();

                        xif.onValueChanged.ClearAll();
                        xif.onValueChanged.AddListener(_val =>
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                CurrentSelectedBG.reactivePosSamples.x = num;
                                BackgroundManager.inst.UpdateBackgrounds();
                            }
                        });

                        yif.onValueChanged.ClearAll();
                        yif.onValueChanged.AddListener(_val =>
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
                        iLabel.transform.SetParent(BackgroundEditor.inst.left);
                        iLabel.transform.localScale = Vector3.one;
                        iLabel.name = "label";
                        iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Position Intensity";
                        iLabel.transform.SetSiblingIndex(26);

                        var position = Instantiate(BackgroundEditor.inst.left.Find("position").gameObject);
                        position.transform.SetParent(BackgroundEditor.inst.left);
                        position.transform.localScale = Vector3.one;
                        position.name = "reactive-position-intensity";
                        position.transform.SetSiblingIndex(27);

                        var xif = position.transform.Find("x").GetComponent<InputField>();
                        var yif = position.transform.Find("y").GetComponent<InputField>();

                        xif.onValueChanged.ClearAll();
                        xif.onValueChanged.AddListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                CurrentSelectedBG.reactivePosIntensity.x = num;
                                BackgroundManager.inst.UpdateBackgrounds();
                            }
                        });

                        yif.onValueChanged.ClearAll();
                        yif.onValueChanged.AddListener(_val =>
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
                        iLabel.transform.SetParent(BackgroundEditor.inst.left);
                        iLabel.transform.localScale = Vector3.one;
                        iLabel.name = "label";
                        iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Scale Samples";
                        iLabel.transform.SetSiblingIndex(28);

                        var position = Instantiate(BackgroundEditor.inst.left.Find("position").gameObject);
                        position.transform.SetParent(BackgroundEditor.inst.left);
                        position.transform.localScale = Vector3.one;
                        position.name = "reactive-scale-samples";
                        position.transform.SetSiblingIndex(29);

                        var xif = position.transform.Find("x").GetComponent<InputField>();
                        var yif = position.transform.Find("y").GetComponent<InputField>();

                        xif.onValueChanged.ClearAll();
                        xif.onValueChanged.AddListener(_val =>
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                CurrentSelectedBG.reactiveScaSamples.x = num;
                                BackgroundManager.inst.UpdateBackgrounds();
                            }
                        });

                        yif.onValueChanged.ClearAll();
                        yif.onValueChanged.AddListener(_val =>
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
                        iLabel.transform.SetParent(BackgroundEditor.inst.left);
                        iLabel.transform.localScale = Vector3.one;
                        iLabel.name = "label";
                        iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Scale Intensity";
                        iLabel.transform.SetSiblingIndex(30);

                        var position = Instantiate(BackgroundEditor.inst.left.Find("position").gameObject);
                        position.transform.SetParent(BackgroundEditor.inst.left);
                        position.transform.localScale = Vector3.one;
                        position.name = "reactive-scale-intensity";
                        position.transform.SetSiblingIndex(31);

                        var xif = position.transform.Find("x").GetComponent<InputField>();
                        var yif = position.transform.Find("y").GetComponent<InputField>();

                        xif.onValueChanged.ClearAll();
                        xif.onValueChanged.AddListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                CurrentSelectedBG.reactiveScaIntensity.x = num;
                                BackgroundManager.inst.UpdateBackgrounds();
                            }
                        });

                        yif.onValueChanged.ClearAll();
                        yif.onValueChanged.AddListener(_val =>
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
                        iLabel.transform.SetParent(BackgroundEditor.inst.left);
                        iLabel.transform.localScale = Vector3.one;
                        iLabel.name = "label";
                        iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Rotation Sample";
                        iLabel.transform.SetSiblingIndex(32);

                        var position = Instantiate(BackgroundEditor.inst.left.Find("position").gameObject);
                        position.transform.SetParent(BackgroundEditor.inst.left);
                        position.transform.localScale = Vector3.one;
                        position.name = "reactive-rotation-sample";
                        position.transform.SetSiblingIndex(33);

                        DestroyImmediate(position.transform.Find("y").gameObject);

                        var xif = position.transform.Find("x").GetComponent<InputField>();

                        xif.onValueChanged.ClearAll();
                        xif.onValueChanged.AddListener(_val =>
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
                        iLabel.transform.SetParent(BackgroundEditor.inst.left);
                        iLabel.transform.localScale = Vector3.one;
                        iLabel.name = "label";
                        iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Rotation Intensity";
                        iLabel.transform.SetSiblingIndex(34);

                        var position = Instantiate(BackgroundEditor.inst.left.Find("position").gameObject);
                        position.transform.SetParent(BackgroundEditor.inst.left);
                        position.transform.localScale = Vector3.one;
                        position.name = "reactive-rotation-intensity";
                        position.transform.SetSiblingIndex(35);

                        DestroyImmediate(position.transform.Find("y").gameObject);

                        var xif = position.transform.Find("x").GetComponent<InputField>();

                        xif.onValueChanged.ClearAll();
                        xif.onValueChanged.AddListener(_val =>
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
                        iLabel.transform.SetParent(BackgroundEditor.inst.left);
                        iLabel.transform.localScale = Vector3.one;
                        iLabel.name = "label";
                        iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Color Sample";
                        iLabel.transform.SetSiblingIndex(36);

                        var position = Instantiate(BackgroundEditor.inst.left.Find("position").gameObject);
                        position.transform.SetParent(BackgroundEditor.inst.left);
                        position.transform.localScale = Vector3.one;
                        position.name = "reactive-color-sample";
                        position.transform.SetSiblingIndex(37);

                        DestroyImmediate(position.transform.Find("y").gameObject);

                        var xif = position.transform.Find("x").GetComponent<InputField>();

                        xif.onValueChanged.ClearAll();
                        xif.onValueChanged.AddListener(_val =>
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
                        iLabel.transform.SetParent(BackgroundEditor.inst.left);
                        iLabel.transform.localScale = Vector3.one;
                        iLabel.name = "label";
                        iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Color Intensity";
                        iLabel.transform.SetSiblingIndex(38);

                        var position = Instantiate(BackgroundEditor.inst.left.Find("position").gameObject);
                        position.transform.SetParent(BackgroundEditor.inst.left);
                        position.transform.localScale = Vector3.one;
                        position.name = "reactive-color-intensity";
                        position.transform.SetSiblingIndex(39);

                        DestroyImmediate(position.transform.Find("y").gameObject);

                        var xif = position.transform.Find("x").GetComponent<InputField>();

                        xif.onValueChanged.ClearAll();
                        xif.onValueChanged.AddListener(_val =>
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
                        colorLabel.transform.SetParent(BackgroundEditor.inst.left);
                        colorLabel.transform.localScale = Vector3.one;
                        colorLabel.name = "label";
                        colorLabel.transform.SetSiblingIndex(40);
                        colorLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Color";

                        var color = BackgroundEditor.inst.left.Find("color");
                        var fadeColor = Instantiate(color.gameObject);
                        fadeColor.transform.SetParent(BackgroundEditor.inst.left);
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
                        iLabel.transform.SetParent(BackgroundEditor.inst.left);
                        iLabel.transform.localScale = Vector3.one;
                        iLabel.name = "label";
                        iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Z Sample";
                        iLabel.transform.SetSiblingIndex(42);

                        var position = Instantiate(BackgroundEditor.inst.left.Find("position").gameObject);
                        position.transform.SetParent(BackgroundEditor.inst.left);
                        position.transform.localScale = Vector3.one;
                        position.name = "reactive-z-sample";
                        position.transform.SetSiblingIndex(43);

                        DestroyImmediate(position.transform.Find("y").gameObject);

                        var xif = position.transform.Find("x").GetComponent<InputField>();

                        xif.onValueChanged.ClearAll();
                        xif.onValueChanged.AddListener(_val =>
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
                        iLabel.transform.SetParent(BackgroundEditor.inst.left);
                        iLabel.transform.localScale = Vector3.one;
                        iLabel.name = "label";
                        iLabel.transform.GetChild(0).GetComponent<Text>().text = "Reactive Z Intensity";
                        iLabel.transform.SetSiblingIndex(44);

                        var position = Instantiate(BackgroundEditor.inst.left.Find("position").gameObject);
                        position.transform.SetParent(BackgroundEditor.inst.left);
                        position.transform.localScale = Vector3.one;
                        position.name = "reactive-z-intensity";
                        position.transform.SetSiblingIndex(45);

                        DestroyImmediate(position.transform.Find("y").gameObject);

                        var xif = position.transform.Find("x").GetComponent<InputField>();

                        xif.onValueChanged.ClearAll();
                        xif.onValueChanged.AddListener(_val =>
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
                colorLabel.transform.SetParent(BackgroundEditor.inst.left);
                colorLabel.transform.localScale = Vector3.one;
                colorLabel.name = "label";
                colorLabel.transform.SetSiblingIndex(16);
                colorLabel.transform.GetChild(0).GetComponent<Text>().text = "Fade Color";

                var color = BackgroundEditor.inst.left.Find("color");
                var fadeColor = Instantiate(color.gameObject);
                fadeColor.transform.SetParent(BackgroundEditor.inst.left);
                fadeColor.transform.localScale = Vector3.one;
                fadeColor.name = "fade-color";
                fadeColor.transform.SetSiblingIndex(17);
            }

            // Rotation
            {
                var index = BackgroundEditor.inst.left.Find("rotation").GetSiblingIndex();

                var iLabel = Instantiate(label);
                iLabel.transform.SetParent(BackgroundEditor.inst.left);
                iLabel.transform.localScale = Vector3.one;
                iLabel.name = "label";
                iLabel.transform.GetChild(0).GetComponent<Text>().text = "3D Rotation";
                iLabel.transform.SetSiblingIndex(index - 1);

                var iterations = Instantiate(BackgroundEditor.inst.left.Find("position").gameObject);
                iterations.transform.SetParent(BackgroundEditor.inst.left);
                iterations.transform.localScale = Vector3.one;
                iterations.name = "depth-rotation";
                iterations.transform.SetSiblingIndex(index);

                var xif = iterations.transform.Find("x").GetComponent<InputField>();

                xif.onValueChanged.ClearAll();
                xif.onValueChanged.AddListener(_val =>
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
                yif.onValueChanged.AddListener(_val =>
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
                colorLabel.transform.SetParent(BackgroundEditor.inst.left);
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

                var iterations = Instantiate(BackgroundEditor.inst.left.Find("position").gameObject);
                iterations.transform.SetParent(BackgroundEditor.inst.left);
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
                    xif.onValueChanged.AddListener(_val =>
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
                    xif.onValueChanged.AddListener(_val =>
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
                    xif.onValueChanged.AddListener(_val =>
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
                colorLabel.transform.SetParent(BackgroundEditor.inst.left);
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

                var iterations = Instantiate(BackgroundEditor.inst.left.Find("position").gameObject);
                iterations.transform.SetParent(BackgroundEditor.inst.left);
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
                    xif.onValueChanged.AddListener(_val =>
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
                    xif.onValueChanged.AddListener(_val =>
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
                    xif.onValueChanged.AddListener(_val =>
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            CurrentSelectedBG.value = num;
                        }
                    });

                    TriggerHelper.IncreaseDecreaseButtons(xif);
                }
            }

            // Tags
            {
                var iLabel = label.Duplicate(BackgroundEditor.inst.left, "label", 2);
                iLabel.transform.localScale = Vector3.one;
                iLabel.transform.GetChild(0).GetComponent<Text>().text = "Tags";

                // Tags Scroll View/Viewport/Content
                var tagScrollView = Creator.NewUIObject("Tags Scroll View", BackgroundEditor.inst.left, 3);

                tagScrollView.transform.AsRT().sizeDelta = new Vector2(522f, 40f);
                var scroll = tagScrollView.AddComponent<ScrollRect>();

                scroll.horizontal = true;
                scroll.vertical = false;

                var image = tagScrollView.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.01f);

                var mask = tagScrollView.AddComponent<Mask>();

                var tagViewport = Creator.NewUIObject("Viewport", tagScrollView.transform);
                RectValues.FullAnchored.AssignToRectTransform(tagViewport.transform.AsRT());

                var tagContent = Creator.NewUIObject("Content", tagViewport.transform);

                var tagContentGLG = tagContent.AddComponent<GridLayoutGroup>();
                tagContentGLG.cellSize = new Vector2(168f, 32f);
                tagContentGLG.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                tagContentGLG.constraintCount = 1;
                tagContentGLG.childAlignment = TextAnchor.MiddleLeft;
                tagContentGLG.spacing = new Vector2(8f, 0f);

                var tagContentCSF = tagContent.AddComponent<ContentSizeFitter>();
                tagContentCSF.horizontalFit = ContentSizeFitter.FitMode.MinSize;
                tagContentCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

                scroll.viewport = tagViewport.transform.AsRT();
                scroll.content = tagContent.transform.AsRT();
            }

            // Modifiers
            {
                var iLabel = Instantiate(label);
                iLabel.transform.SetParent(BackgroundEditor.inst.left);
                iLabel.transform.localScale = Vector3.one;
                iLabel.name = "label";
                iLabel.transform.GetChild(0).GetComponent<Text>().text = "Modifier Blocks";

                var iterations = Instantiate(BackgroundEditor.inst.left.Find("position").gameObject);
                iterations.transform.SetParent(BackgroundEditor.inst.left);
                iterations.transform.localScale = Vector3.one;
                iterations.name = "block";
                DestroyImmediate(iterations.transform.GetChild(1).gameObject);

                var addBlock = EditorPrefabHolder.Instance.Function1Button.Duplicate(iterations.transform.Find("x"), "add");
                addBlock.transform.localScale = Vector3.one;
                addBlock.transform.AsRT().sizeDelta = new Vector2(80f, 32f);

                var addBlockText = addBlock.transform.GetChild(0).GetComponent<Text>();
                addBlockText.text = "Add";

                var removeBlock = EditorPrefabHolder.Instance.Function1Button.Duplicate(iterations.transform.Find("x"), "del");
                removeBlock.transform.localScale = Vector3.one;
                removeBlock.transform.AsRT().sizeDelta = new Vector2(80f, 32f);

                var removeBlockText = removeBlock.transform.GetChild(0).GetComponent<Text>();
                removeBlockText.text = "Del";

                EditorThemeManager.AddGraphic(addBlock.GetComponent<Image>(), ThemeGroup.Add, true);
                EditorThemeManager.AddGraphic(addBlockText, ThemeGroup.Add_Text);
                EditorThemeManager.AddGraphic(removeBlock.GetComponent<Image>(), ThemeGroup.Delete, true);
                EditorThemeManager.AddGraphic(removeBlockText, ThemeGroup.Delete_Text);

                CreateModifiersOnAwake();
                RTEditor.inst.GeneratePopup("Default Background Modifiers Popup", "Choose a modifer to add", Vector2.zero, new Vector2(600f, 400f), _val =>
                {
                    searchTerm = _val;
                    if (CurrentSelectedBG)
                        RefreshDefaultModifiersList(CurrentSelectedBG, addIndex);
                }, placeholderText: "Search for default Modifier...");

                EditorThemeManager.AddInputFields(BackgroundEditor.inst.left.Find("block").gameObject, true, "Background Editor Reactive");
            }

            var active = BackgroundEditor.inst.left.Find("name/active").GetComponent<Toggle>();
            Destroy(active.GetComponent<Animator>());
            active.transition = Selectable.Transition.ColorTint;
            active.colors = UIManager.SetColorBlock(active.colors, Color.white, new Color(0.7f, 0.7f, 0.7f), new Color(0.7f, 0.7f, 0.7f), new Color(0.7f, 0.7f, 0.7f), new Color(0.7f, 0.7f, 0.7f));
            EditorThemeManager.AddToggle(active);
            BackgroundEditor.inst.left.Find("name/name").AsRT().sizeDelta = new Vector2(300f, 32f);
            EditorThemeManager.AddInputField(BackgroundEditor.inst.left.Find("name/name").GetComponent<InputField>());
            EditorThemeManager.AddInputFields(BackgroundEditor.inst.left.Find("depth").gameObject, true, "Background Editor Depth");
            EditorThemeManager.AddInputFields(BackgroundEditor.inst.left.Find("iterations").gameObject, true, "Background Editor Iterations");
            EditorThemeManager.AddInputFields(BackgroundEditor.inst.left.Find("zposition").gameObject, true, "");
            EditorThemeManager.AddInputFields(BackgroundEditor.inst.left.Find("zscale").gameObject, true, "Background Editor Z Scale");
            EditorThemeManager.AddInputFields(BackgroundEditor.inst.left.Find("position").gameObject, true, "Background Editor Position");
            EditorThemeManager.AddInputFields(BackgroundEditor.inst.left.Find("scale").gameObject, true, "Background Editor Scale");
            EditorThemeManager.AddInputFields(BackgroundEditor.inst.left.Find("depth-rotation").gameObject, true, "Background Editor 3D Rotation");
            EditorThemeManager.AddInputField(BackgroundEditor.inst.left.Find("rotation/x").GetComponent<InputField>());

            EditorThemeManager.AddInputFields(BackgroundEditor.inst.left.Find("fadehuesatval").gameObject, true, "");
            EditorThemeManager.AddInputFields(BackgroundEditor.inst.left.Find("huesatval").gameObject, true, "");

            var rotationSliderImage = BackgroundEditor.inst.left.Find("rotation/slider/Image").GetComponent<Image>();
            var rotationSlider = BackgroundEditor.inst.left.Find("rotation/slider").GetComponent<Slider>();
            rotationSlider.colors = UIManager.SetColorBlock(rotationSlider.colors, Color.white, new Color(0.9f, 0.9f, 0.9f), Color.white, Color.white, Color.white);
            rotationSlider.transform.AsRT().sizeDelta = new Vector2(207f, 32f);

            EditorThemeManager.AddSlider(rotationSlider, rotationSliderImage);

            for (int i = 0; i < BackgroundEditor.inst.left.Find("reactive-ranges").childCount; i++)
            {
                var child = BackgroundEditor.inst.left.Find("reactive-ranges").GetChild(i);
                var toggle = child.GetComponent<Toggle>();
                var background = toggle.image;
                var checkmark = toggle.graphic;

                EditorThemeManager.AddGraphic(background, ThemeGroup.Function_2_Normal, true);
                EditorThemeManager.AddGraphic(checkmark, ThemeGroup.Function_2_Highlighted);
                EditorThemeManager.AddGraphic(child.Find("Label").GetComponent<Text>(), ThemeGroup.Function_2_Text);
            }

            EditorThemeManager.AddInputField(BackgroundEditor.inst.left.Find("reactive/x").GetComponent<InputField>());

            var reactiveSliderImage = BackgroundEditor.inst.left.Find("reactive/slider/Image").GetComponent<Image>();
            var reactiveSlider = BackgroundEditor.inst.left.Find("reactive/slider").GetComponent<Slider>();
            reactiveSlider.colors = UIManager.SetColorBlock(reactiveSlider.colors, Color.white, new Color(0.9f, 0.9f, 0.9f), Color.white, Color.white, Color.white);
            reactiveSlider.transform.AsRT().sizeDelta = new Vector2(207f, 32f);

            EditorThemeManager.AddSlider(reactiveSlider, reactiveSliderImage);

            EditorThemeManager.AddInputFields(BackgroundEditor.inst.left.Find("reactive-position-samples").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(BackgroundEditor.inst.left.Find("reactive-position-intensity").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(BackgroundEditor.inst.left.Find("reactive-scale-samples").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(BackgroundEditor.inst.left.Find("reactive-scale-intensity").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(BackgroundEditor.inst.left.Find("reactive-rotation-sample").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(BackgroundEditor.inst.left.Find("reactive-rotation-intensity").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(BackgroundEditor.inst.left.Find("reactive-color-sample").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(BackgroundEditor.inst.left.Find("reactive-color-intensity").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(BackgroundEditor.inst.left.Find("reactive-z-sample").gameObject, true, "Background Editor Reactive");
            EditorThemeManager.AddInputFields(BackgroundEditor.inst.left.Find("reactive-z-intensity").gameObject, true, "Background Editor Reactive");

            var fade = BackgroundEditor.inst.left.Find("fade");
            var fadeToggle = fade.GetComponent<Toggle>();
            var fadeBackground = fadeToggle.image;
            var fadeCheckmark = fadeToggle.graphic;

            EditorThemeManager.AddGraphic(fadeBackground, ThemeGroup.Function_2_Normal, true);
            EditorThemeManager.AddGraphic(fadeCheckmark, ThemeGroup.Function_2_Highlighted, true);
            EditorThemeManager.AddGraphic(fade.Find("Label").GetComponent<Text>(), ThemeGroup.Function_2_Text, true);

            // Labels
            for (int i = 0; i < BackgroundEditor.inst.left.childCount; i++)
            {
                var child = BackgroundEditor.inst.left.GetChild(i);
                if (child.name != "label")
                    continue;

                for (int j = 0; j < child.childCount; j++)
                    EditorThemeManager.AddLightText(child.GetChild(j).GetComponent<Text>());
            }

            #endregion

            yield break;
        }

        void RenderTags(BackgroundObject backgroundObject)
        {
            var tagsParent = BackgroundEditor.inst.left.Find("Tags Scroll View/Viewport/Content");

            LSHelpers.DeleteChildren(tagsParent);

            int num = 0;
            foreach (var tag in backgroundObject.tags)
            {
                int index = num;
                var gameObject = EditorPrefabHolder.Instance.Tag.Duplicate(tagsParent, index.ToString());
                gameObject.transform.localScale = Vector3.one;
                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.onValueChanged.ClearAll();
                input.text = tag;
                input.onValueChanged.AddListener(_val => { backgroundObject.tags[index] = _val; });

                var inputFieldSwapper = gameObject.AddComponent<InputFieldSwapper>();
                inputFieldSwapper.Init(input, InputFieldSwapper.Type.String);

                var deleteStorage = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.ClearAll();
                deleteStorage.button.onClick.AddListener(() =>
                {
                    backgroundObject.tags.RemoveAt(index);
                    RenderTags(backgroundObject);
                });

                EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.Input_Field, true);

                EditorThemeManager.ApplyInputField(input);

                EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);

                num++;
            }

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(tagsParent, "Add");
            add.transform.localScale = Vector3.one;
            var addText = add.transform.Find("Text").GetComponent<Text>();
            addText.text = "Add Tag";
            var addButton = add.GetComponent<Button>();
            addButton.onClick.ClearAll();
            addButton.onClick.AddListener(() =>
            {
                backgroundObject.tags.Add("New Tag");
                RenderTags(backgroundObject);
            });

            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text, true);
        }

        public void OpenDialog(int index)
        {
            var __instance = BackgroundEditor.inst;

            EditorManager.inst.ClearDialogs();
            EditorManager.inst.ShowDialog("Background Editor");

            var backgroundObject = GameData.Current.backgroundObjects[index];

            __instance.left.Find("name/active").GetComponent<Toggle>().isOn = backgroundObject.active;
            __instance.left.Find("name/name").GetComponent<InputField>().text = backgroundObject.name;

            RenderTags(backgroundObject);

            SetSingleInputFieldInt(__instance.left, "iterations/x", backgroundObject.depth);

            SetSingleInputFieldInt(__instance.left, "depth/x", backgroundObject.layer);

            SetSingleInputField(__instance.left, "zposition/x", backgroundObject.zposition);
            SetSingleInputField(__instance.left, "zscale/x", backgroundObject.zscale);

            var fade = __instance.left.Find("fade").GetComponent<Toggle>();

            fade.interactable = false;
            fade.isOn = backgroundObject.drawFade;
            fade.interactable = true;

            SetVector2InputField(__instance.left, "position", backgroundObject.pos);

            SetVector2InputField(__instance.left, "scale", backgroundObject.scale);

            SetSingleInputField(__instance.left, "rotation/x", backgroundObject.rot, 15f, 3f);

            var rotSlider = __instance.left.Find("rotation/slider").GetComponent<Slider>();
            rotSlider.maxValue = 360f;
            rotSlider.minValue = -360f;
            rotSlider.value = backgroundObject.rot;

            // 3D Rotation
            SetVector2InputField(__instance.left, "depth-rotation", backgroundObject.rotation, 15f, 3f);

            try
            {
                __instance.left.Find("reactive-ranges").GetChild(backgroundObject.reactive ? (int)(backgroundObject.reactiveType + 1) : 0).GetComponent<Toggle>().isOn = true;
            }
            catch
            {
                __instance.left.Find("reactive-ranges").GetChild(0).GetComponent<Toggle>().isOn = true;
                CoreHelper.LogError($"Custom Reactive not implemented.");
            }

            __instance.left.Find("reactive/x").GetComponent<InputField>().text = backgroundObject.reactiveScale.ToString("f2");
            __instance.left.Find("reactive/slider").GetComponent<Slider>().value = backgroundObject.reactiveScale;

            SetSingleInputField(__instance.left, "fadehuesatval/x", backgroundObject.fadeHue);
            SetSingleInputField(__instance.left, "fadehuesatval/y", backgroundObject.fadeSaturation);
            SetSingleInputField(__instance.left, "fadehuesatval/z", backgroundObject.fadeValue);
            
            SetSingleInputField(__instance.left, "huesatval/x", backgroundObject.hue);
            SetSingleInputField(__instance.left, "huesatval/y", backgroundObject.saturation);
            SetSingleInputField(__instance.left, "huesatval/z", backgroundObject.value);

            LSHelpers.DeleteChildren(__instance.left.Find("color"));
            LSHelpers.DeleteChildren(__instance.left.Find("fade-color"));
            LSHelpers.DeleteChildren(__instance.left.Find("reactive-color"));

            int num = 0;
            foreach (var col in GameManager.inst.LiveTheme.backgroundColors)
            {
                int colTmp = num;
                SetColorToggle(col, backgroundObject.color, colTmp, __instance.left.Find("color"), __instance.SetColor);
                SetColorToggle(col, backgroundObject.FadeColor, colTmp, __instance.left.Find("fade-color"), SetFadeColor);
                SetColorToggle(col, backgroundObject.reactiveCol, colTmp, __instance.left.Find("reactive-color"), SetReactiveColor);

                num++;
            }

            SetShape(backgroundObject, index);

            // Reactive Position Samples
            SetVector2InputFieldInt(__instance.left, "reactive-position-samples", backgroundObject.reactivePosSamples);

            // Reactive Position Intensity
            SetVector2InputField(__instance.left, "reactive-position-intensity", backgroundObject.reactivePosIntensity);

            // Reactive Scale Samples
            SetVector2InputFieldInt(__instance.left, "reactive-scale-samples", backgroundObject.reactiveScaSamples);

            // Reactive Scale Intensity
            SetVector2InputField(__instance.left, "reactive-scale-intensity", backgroundObject.reactiveScaIntensity);

            // Reactive Rotation Samples
            SetSingleInputFieldInt(__instance.left, "reactive-rotation-sample/x", backgroundObject.reactiveRotSample);

            // Reactive Rotation Intensity
            SetSingleInputField(__instance.left, "reactive-rotation-intensity/x", backgroundObject.reactiveRotIntensity);

            // Reactive Color Samples
            SetSingleInputFieldInt(__instance.left, "reactive-color-sample/x", backgroundObject.reactiveColSample);

            // Reactive Color Intensity
            SetSingleInputField(__instance.left, "reactive-color-intensity/x", backgroundObject.reactiveColIntensity);

            // Reactive Z Samples
            SetSingleInputFieldInt(__instance.left, "reactive-z-sample/x", backgroundObject.reactiveZSample);

            // Reactive Z Intensity
            SetSingleInputField(__instance.left, "reactive-z-intensity/x", backgroundObject.reactiveZIntensity);

            __instance.UpdateBackgroundList();

            StartCoroutine(RenderModifiers(backgroundObject));

            __instance.dialog.gameObject.SetActive(true);
        }

        public void SetColorToggle(Color color, int currentColor, int colTmp, Transform parent, Action<int> onSetColor)
        {
            var gameObject = EditorManager.inst.colorGUI.Duplicate(parent, "color gui");
            gameObject.transform.localScale = Vector3.one;
            var button = gameObject.GetComponent<Button>();
            button.image.color = LSColors.fadeColor(color, 1f);
            gameObject.transform.Find("Image").gameObject.SetActive(currentColor == colTmp);

            button.onClick.AddListener(() => { onSetColor.Invoke(colTmp); });

            EditorThemeManager.ApplyGraphic(button.image, ThemeGroup.Null, true);
            EditorThemeManager.ApplyGraphic(gameObject.transform.Find("Image").GetComponent<Image>(), ThemeGroup.Background_1);
        }

        public void SetShape(BackgroundObject backgroundObject, int index)
        {
            var shape = BackgroundEditor.inst.left.Find("shape");
            var shapeSettings = BackgroundEditor.inst.left.Find("shapesettings");

            shape.GetComponent<GridLayoutGroup>().spacing = new Vector2(7.6f, 0f);

            DestroyImmediate(shape.GetComponent<ToggleGroup>());

            var toDestroy = new List<GameObject>();

            for (int i = 0; i < shape.childCount; i++)
            {
                toDestroy.Add(shape.GetChild(i).gameObject);
            }

            for (int i = 0; i < shapeSettings.childCount; i++)
            {
                if (i != 4 && i != 6)
                    for (int j = 0; j < shapeSettings.GetChild(i).childCount; j++)
                    {
                        toDestroy.Add(shapeSettings.GetChild(i).GetChild(j).gameObject);
                    }
            }

            foreach (var obj in toDestroy)
                DestroyImmediate(obj);

            toDestroy = null;

            // Re-add everything
            for (int i = 0; i < ShapeManager.inst.Shapes3D.Count; i++)
            {
                var obj = ObjectEditor.inst.shapeButtonPrefab.Duplicate(shape, (i + 1).ToString(), i);
                if (obj.transform.Find("Image") && obj.transform.Find("Image").gameObject.TryGetComponent(out Image image))
                {
                    image.sprite = ShapeManager.inst.Shapes3D[i][0].icon;
                    EditorThemeManager.ApplyGraphic(image, ThemeGroup.Toggle_1_Check);
                }

                var shapeToggle = obj.GetComponent<Toggle>();
                EditorThemeManager.ApplyToggle(shapeToggle, ThemeGroup.Background_1);

                shapeToggle.group = null;

                if (i != 4 && i != 6)
                {
                    if (!shapeSettings.Find((i + 1).ToString()))
                    {
                        shapeSettings.Find("6").gameObject.Duplicate(shapeSettings, (i + 1).ToString());
                    }

                    var so = shapeSettings.Find((i + 1).ToString());

                    var rect = (RectTransform)so;
                    if (!so.GetComponent<ScrollRect>())
                    {
                        var scroll = so.gameObject.AddComponent<ScrollRect>();
                        so.gameObject.AddComponent<Mask>();
                        var ad = so.gameObject.AddComponent<Image>();

                        scroll.horizontal = true;
                        scroll.vertical = false;
                        scroll.content = rect;
                        scroll.viewport = rect;
                        ad.color = new Color(1f, 1f, 1f, 0.01f);
                    }

                    for (int j = 0; j < ShapeManager.inst.Shapes3D[i].Count; j++)
                    {
                        var opt = ObjectEditor.inst.shapeButtonPrefab.Duplicate(shapeSettings.GetChild(i), (j + 1).ToString(), j);
                        if (opt.transform.Find("Image") && opt.transform.Find("Image").gameObject.TryGetComponent(out Image image1))
                        {
                            image1.sprite = ShapeManager.inst.Shapes3D[i][j].icon;
                            EditorThemeManager.ApplyGraphic(image1, ThemeGroup.Toggle_1_Check);
                        }

                        var layoutElement = opt.AddComponent<LayoutElement>();
                        layoutElement.layoutPriority = 1;
                        layoutElement.minWidth = 32f;

                        ((RectTransform)opt.transform).sizeDelta = new Vector2(32f, 32f);

                        var shapeOptionToggle = opt.GetComponent<Toggle>();
                        shapeOptionToggle.group = null;
                        EditorThemeManager.ApplyToggle(shapeOptionToggle, checkGroup: ThemeGroup.Background_1);

                        if (!opt.GetComponent<HoverUI>())
                        {
                            var he = opt.AddComponent<HoverUI>();
                            he.animatePos = false;
                            he.animateSca = true;
                            he.size = 1.1f;
                        }
                    }

                    ObjectEditor.inst.LastGameObject(shapeSettings.GetChild(i));
                }
            }

            LSHelpers.SetActiveChildren(shapeSettings, false);

            if (backgroundObject.shape.type >= shapeSettings.childCount)
            {
                Debug.Log($"{BackgroundEditor.inst.className}Somehow, the object ended up being at a higher shape than normal.");
                backgroundObject.SetShape(shapeSettings.childCount - 1 - 1, 0);

                BackgroundEditor.inst.OpenDialog(index);
                return;
            }

            var shapeType = backgroundObject.shape.type;

            if (shapeType == 4)
            {
                // Make the text larger for better readability.
                shapeSettings.transform.AsRT().sizeDelta = new Vector2(351f, 74f);
                var child = shapeSettings.GetChild(4);
                child.AsRT().sizeDelta = new Vector2(351f, 74f);
                child.Find("Text").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
                child.Find("Placeholder").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
                child.GetComponent<InputField>().lineType = InputField.LineType.MultiLineNewline;
            }
            else
            {
                shapeSettings.AsRT().sizeDelta = new Vector2(351f, 32f);
                shapeSettings.GetChild(4).AsRT().sizeDelta = new Vector2(351f, 32f);
            }

            shapeSettings.GetChild(backgroundObject.shape.type).gameObject.SetActive(true);
            for (int i = 1; i <= ShapeManager.inst.Shapes3D.Count; i++)
            {
                int buttonTmp = i - 1;

                if (shape.Find(i.ToString()))
                {
                    var shoggle = shape.Find(i.ToString()).GetComponent<Toggle>();
                    shoggle.onValueChanged.ClearAll();
                    shoggle.isOn = backgroundObject.shape.type == buttonTmp;
                    shoggle.onValueChanged.AddListener(_val =>
                    {
                        if (!_val)
                            return;

                        backgroundObject.SetShape(buttonTmp, 0);

                        BackgroundEditor.inst.OpenDialog(index);
                    });

                    if (!shape.Find(i.ToString()).GetComponent<HoverUI>())
                    {
                        var hoverUI = shape.Find(i.ToString()).gameObject.AddComponent<HoverUI>();
                        hoverUI.animatePos = false;
                        hoverUI.animateSca = true;
                        hoverUI.size = 1.1f;
                    }
                }
            }

            if (shapeType == 4 || shapeType == 6)
            {
                EditorManager.inst.DisplayNotification($"{(shapeType == 4 ? "Text" : "Image")} background not supported.", 2f, EditorManager.NotificationType.Error);
                backgroundObject.SetShape(0, 0);
                return;
            }

            for (int i = 0; i < shapeSettings.GetChild(backgroundObject.shape.type).childCount - 1; i++)
            {
                int buttonTmp = i;
                var shoggle = shapeSettings.GetChild(backgroundObject.shape.type).GetChild(i).GetComponent<Toggle>();

                shoggle.onValueChanged.RemoveAllListeners();
                shoggle.isOn = backgroundObject.shape.option == i;
                shoggle.onValueChanged.AddListener(_val =>
                {
                    if (!_val)
                        return;

                    backgroundObject.SetShape(backgroundObject.shape.type, buttonTmp);

                    BackgroundEditor.inst.OpenDialog(index);
                });
            }
        }

        void SetSingleInputField(Transform dialogTmp, string name, float value, float amount = 0.1f, float multiply = 10f)
        {
            var reactiveX = dialogTmp.Find(name).GetComponent<InputField>();
            reactiveX.text = value.ToString();

            if (!reactiveX.GetComponent<EventTrigger>())
            {
                var etX = reactiveX.gameObject.AddComponent<EventTrigger>();

                etX.triggers.Add(TriggerHelper.ScrollDelta(reactiveX, amount, multiply));
            }

            if (!reactiveX.GetComponent<InputFieldSwapper>())
            {
                var reactiveXSwapper = reactiveX.gameObject.AddComponent<InputFieldSwapper>();
                reactiveXSwapper.Init(reactiveX);
            }
        }

        void SetSingleInputFieldInt(Transform dialogTmp, string name, int value)
        {
            var reactiveX = dialogTmp.Find(name).GetComponent<InputField>();
            reactiveX.text = value.ToString();

            if (!reactiveX.GetComponent<EventTrigger>())
            {
                var etX = reactiveX.gameObject.AddComponent<EventTrigger>();

                etX.triggers.Add(TriggerHelper.ScrollDeltaInt(reactiveX, 1));
            }

            if (!reactiveX.GetComponent<InputFieldSwapper>())
            {
                var reactiveXSwapper = reactiveX.gameObject.AddComponent<InputFieldSwapper>();
                reactiveXSwapper.Init(reactiveX);
            }
        }

        void SetVector2InputField(Transform dialogTmp, string name, Vector2 value, float amount = 0.1f, float multiply = 10f)
        {
            var reactiveX = dialogTmp.Find($"{name}/x").GetComponent<InputField>();
            reactiveX.text = value.x.ToString();

            var reactiveY = dialogTmp.Find($"{name}/y").GetComponent<InputField>();
            reactiveY.text = value.y.ToString();

            if (!reactiveX.GetComponent<EventTrigger>())
            {
                var etX = reactiveX.gameObject.AddComponent<EventTrigger>();

                etX.triggers.Add(TriggerHelper.ScrollDelta(reactiveX, amount, multiply, multi: true));
                etX.triggers.Add(TriggerHelper.ScrollDeltaVector2(reactiveX, reactiveY, amount, multiply));
            }

            if (!reactiveY.GetComponent<EventTrigger>())
            {
                var etY = reactiveY.gameObject.AddComponent<EventTrigger>();

                etY.triggers.Add(TriggerHelper.ScrollDelta(reactiveY, amount, multiply, multi: true));
                etY.triggers.Add(TriggerHelper.ScrollDeltaVector2(reactiveX, reactiveY, amount, multiply));
            }

            if (!reactiveX.GetComponent<InputFieldSwapper>())
            {
                var reactiveXSwapper = reactiveX.gameObject.AddComponent<InputFieldSwapper>();
                reactiveXSwapper.Init(reactiveX);
            }

            if (!reactiveY.GetComponent<InputFieldSwapper>())
            {
                var reactiveYSwapper = reactiveY.gameObject.AddComponent<InputFieldSwapper>();
                reactiveYSwapper.Init(reactiveY);
            }
        }

        void SetVector2InputFieldInt(Transform dialogTmp, string name, Vector2 value)
        {
            var reactiveX = dialogTmp.Find($"{name}/x").GetComponent<InputField>();
            reactiveX.text = value.x.ToString();

            if (!reactiveX.GetComponent<EventTrigger>())
            {
                var etX = reactiveX.gameObject.AddComponent<EventTrigger>();

                etX.triggers.Add(TriggerHelper.ScrollDeltaInt(reactiveX, 1));
            }

            if (!reactiveX.GetComponent<InputFieldSwapper>())
            {
                var reactiveXSwapper = reactiveX.gameObject.AddComponent<InputFieldSwapper>();
                reactiveXSwapper.Init(reactiveX);
            }

            var reactiveY = dialogTmp.Find($"{name}/y").GetComponent<InputField>();
            reactiveY.text = value.y.ToString();

            if (!reactiveY.GetComponent<EventTrigger>())
            {
                var etX = reactiveY.gameObject.AddComponent<EventTrigger>();

                etX.triggers.Add(TriggerHelper.ScrollDeltaInt(reactiveY, 1));
            }

            if (!reactiveY.GetComponent<InputFieldSwapper>())
            {
                var reactiveYSwapper = reactiveY.gameObject.AddComponent<InputFieldSwapper>();
                reactiveYSwapper.Init(reactiveY);
            }
        }

        public void SetFadeColor(int _col)
        {
            CurrentSelectedBG.FadeColor = _col;
            BackgroundEditor.inst.UpdateBackground(BackgroundEditor.inst.currentObj);
            UpdateColorList("fade-color");
        }

        public void SetReactiveColor(int _col)
        {
            CurrentSelectedBG.reactiveCol = _col;
            BackgroundEditor.inst.UpdateBackground(BackgroundEditor.inst.currentObj);
            UpdateColorList("reactive-color");
        }

        void UpdateColorList(string name)
        {
            var bg = CurrentSelectedBG;
            var colorList = BackgroundEditor.inst.left.Find(name);

            for (int i = 0; i < GameManager.inst.LiveTheme.backgroundColors.Count; i++)
                if (colorList.childCount > i)
                    colorList.GetChild(i).Find("Image").gameObject.SetActive(name == "fade-color" ? bg.FadeColor == i : bg.reactiveCol == i);
        }

        public void CreateBackgrounds(int _amount)
        {
            int number = Mathf.Clamp(_amount, 0, 100);

            for (int i = 0; i < number; i++)
            {
                var backgroundObject = new BackgroundObject();
                backgroundObject.name = "bg - " + i;

                float num = UnityEngine.Random.Range(2, 6);
                backgroundObject.scale = UnityEngine.Random.value > 0.5f ? new Vector2((float)UnityEngine.Random.Range(2, 8), (float)UnityEngine.Random.Range(2, 8)) : new Vector2(num, num);

                backgroundObject.pos = new Vector2((float)UnityEngine.Random.Range(-48, 48), (float)UnityEngine.Random.Range(-32, 32));
                backgroundObject.color = UnityEngine.Random.Range(1, 6);
                backgroundObject.layer = UnityEngine.Random.Range(0, 6);
                backgroundObject.reactive = (UnityEngine.Random.value > 0.5f);

                if (backgroundObject.reactive)
                {
                    backgroundObject.reactiveType = (BaseBackgroundObject.ReactiveType)UnityEngine.Random.Range(0, 4);

                    backgroundObject.reactiveScale = UnityEngine.Random.Range(0.01f, 0.04f);
                }

                backgroundObject.reactivePosIntensity = new Vector2(UnityEngine.Random.Range(0, 100) > 65 ? UnityEngine.Random.Range(0f, 1f) : 0f, UnityEngine.Random.Range(0, 100) > 65 ? UnityEngine.Random.Range(0f, 1f) : 0f);
                backgroundObject.reactiveScaIntensity = new Vector2(UnityEngine.Random.Range(0, 100) > 45 ? UnityEngine.Random.Range(0f, 1f) : 0f, UnityEngine.Random.Range(0, 100) > 45 ? UnityEngine.Random.Range(0f, 1f) : 0f);
                backgroundObject.reactiveRotIntensity = UnityEngine.Random.Range(0, 100) > 45 ? UnityEngine.Random.Range(0f, 1f) : 0f;
                backgroundObject.reactiveCol = UnityEngine.Random.Range(1, 6);

                var randomShape = UnityEngine.Random.Range(0, ShapeManager.inst.Shapes3D.Count);
                var randomShapeOption = UnityEngine.Random.Range(0, ShapeManager.inst.Shapes3D[randomShape].Count);

                backgroundObject.shape = ShapeManager.inst.Shapes3D[randomShape][randomShapeOption];

                GameData.Current.backgroundObjects.Add(backgroundObject);
            }

            BackgroundManager.inst.UpdateBackgrounds();
            BackgroundEditor.inst.UpdateBackgroundList();
        }

        public void DeleteAllBackgrounds()
        {
            int num = GameData.Current.backgroundObjects.Count;
            for (int i = GameData.Current.backgroundObjects.Count - 1; i > 0; i--)
            {
                var backgroundObject = GameData.Current.backgroundObjects[i];
                Updater.DestroyBackgroundObject(backgroundObject);
                GameData.Current.backgroundObjects.RemoveAt(i);
            }
            BackgroundEditor.inst.SetCurrentBackground(0);
            BackgroundEditor.inst.UpdateBackgroundList();

            EditorManager.inst.DisplayNotification("Deleted " + (num - 1).ToString() + " backgrounds!", 2f, EditorManager.NotificationType.Success);
        }

        #region Modifiers

        public Transform content;
        public Transform scrollView;

        public bool showModifiers;

        public GameObject modifierCardPrefab;
        public GameObject modifierAddPrefab;

        public Toggle modifiersOrderToggle;
        public Toggle modifiersActiveToggle;

        public void CreateModifiersOnAwake()
        {
            var orderMatters = EditorPrefabHolder.Instance.ToggleButton.Duplicate(BackgroundEditor.inst.left, "order");
            var orderMattersToggleButton = orderMatters.GetComponent<ToggleButtonStorage>();
            orderMattersToggleButton.label.text = "Order Matters";

            modifiersOrderToggle = orderMattersToggleButton.toggle;

            EditorThemeManager.AddToggle(modifiersOrderToggle, graphic: orderMattersToggleButton.label);
            
            var showModifiers = EditorPrefabHolder.Instance.ToggleButton.Duplicate(BackgroundEditor.inst.left, "active");
            var showModifiersToggleButton = showModifiers.GetComponent<ToggleButtonStorage>();
            showModifiersToggleButton.label.text = "Show Modifiers";

            modifiersActiveToggle = showModifiersToggleButton.toggle;
            modifiersActiveToggle.onValueChanged.ClearAll();
            modifiersActiveToggle.isOn = this.showModifiers;
            modifiersActiveToggle.onValueChanged.AddListener(_val =>
            {
                this.showModifiers = _val;
                scrollView.gameObject.SetActive(this.showModifiers);
                if (CurrentSelectedBG)
                    StartCoroutine(RenderModifiers(CurrentSelectedBG));
            });

            EditorThemeManager.AddToggle(modifiersActiveToggle, graphic: showModifiersToggleButton.label);

            scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(BackgroundEditor.inst.left, "Modifiers Scroll View").transform;

            content = scrollView.Find("Viewport/Content");

            scrollView.gameObject.SetActive(this.showModifiers);

            modifierCardPrefab = Creator.NewUIObject("Modifier Prefab", transform);
            modifierCardPrefab.transform.AsRT().sizeDelta = new Vector2(336f, 128f);

            var mcpImage = modifierCardPrefab.AddComponent<Image>();
            mcpImage.color = new Color(1f, 1f, 1f, 0.03f);

            var mcpVLG = modifierCardPrefab.AddComponent<VerticalLayoutGroup>();
            mcpVLG.childControlHeight = false;
            mcpVLG.childForceExpandHeight = false;

            var mcpCSF = modifierCardPrefab.AddComponent<ContentSizeFitter>();
            mcpCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var mcpSpacerTop = Creator.NewUIObject("Spacer Top", modifierCardPrefab.transform);
            mcpSpacerTop.transform.AsRT().sizeDelta = new Vector2(350f, 8f);

            var mcpLabel = Creator.NewUIObject("Label", modifierCardPrefab.transform);
            UIManager.SetRectTransform(mcpLabel.transform.AsRT(), new Vector2(0f, -8f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(352f, 32f));

            var mcpText = Creator.NewUIObject("Text", mcpLabel.transform);
            UIManager.SetRectTransform(mcpText.transform.AsRT(), Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, 32f));

            var mcpTextText = mcpText.AddComponent<Text>();
            mcpTextText.alignment = TextAnchor.MiddleLeft;
            mcpTextText.font = FontManager.inst.DefaultFont;
            mcpTextText.fontSize = 19;
            mcpTextText.color = new Color(0.9373f, 0.9216f, 0.9373f);

            var collapse = EditorPrefabHolder.Instance.CollapseToggle.Duplicate(mcpLabel.transform, "Collapse");
            collapse.transform.localScale = Vector3.one;
            var collapseLayoutElement = collapse.GetComponent<LayoutElement>() ?? collapse.AddComponent<LayoutElement>();
            collapseLayoutElement.minWidth = 32f;
            UIManager.SetRectTransform(collapse.transform.AsRT(), new Vector2(70f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 32f));

            var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(mcpLabel.transform, "Delete");
            delete.transform.localScale = Vector3.one;
            var deleteLayoutElement = delete.GetComponent<LayoutElement>() ?? delete.GetComponent<LayoutElement>();
            deleteLayoutElement.minWidth = 32f;
            UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(140f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 32f));

            var duplicate = EditorPrefabHolder.Instance.DeleteButton.Duplicate(mcpLabel.transform, "Copy");
            duplicate.transform.localScale = Vector3.one;
            var duplicateLayoutElement = duplicate.GetComponent<LayoutElement>() ?? duplicate.AddComponent<LayoutElement>();
            duplicateLayoutElement.minWidth = 32f;

            UIManager.SetRectTransform(duplicate.transform.AsRT(), new Vector2(106f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 32f));

            duplicate.GetComponent<DeleteButtonStorage>().image.sprite = SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}copy.png");

            var notifier = Creator.NewUIObject("Notifier", mcpLabel.transform);
            var notifierImage = notifier.AddComponent<Image>();

            UIManager.SetRectTransform(notifierImage.rectTransform, new Vector2(84f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(8f, 32f));

            var mcpSpacerMid = Creator.NewUIObject("Spacer Middle", modifierCardPrefab.transform);
            mcpSpacerMid.transform.AsRT().sizeDelta = new Vector2(350f, 8f);

            var layout = Creator.NewUIObject("Layout", modifierCardPrefab.transform);

            var layoutVLG = layout.AddComponent<VerticalLayoutGroup>();
            layoutVLG.childControlHeight = false;
            layoutVLG.childForceExpandHeight = false;
            layoutVLG.spacing = 4f;

            var layoutCSF = layout.AddComponent<ContentSizeFitter>();
            layoutCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var mcpSpacerBot = Creator.NewUIObject("Spacer Bottom", modifierCardPrefab.transform);
            mcpSpacerBot.transform.AsRT().sizeDelta = new Vector2(350f, 8f);

            modifierAddPrefab = EditorManager.inst.folderButtonPrefab.Duplicate(transform, "add modifier");

            var text = modifierAddPrefab.transform.GetChild(0).GetComponent<Text>();
            text.text = "+";
            text.alignment = TextAnchor.MiddleCenter;

            booleanBar = Boolean();

            numberInput = NumberInput();

            stringInput = StringInput();

            dropdownBar = Dropdown();
        }

        public static Modifier<BackgroundObject> copiedModifier;
        public IEnumerator RenderModifiers(BackgroundObject backgroundObject)
        {
            LSHelpers.DeleteChildren(content);

            modifiersOrderToggle.onValueChanged.ClearAll();
            modifiersOrderToggle.isOn = CurrentSelectedBG.orderModifiers;
            modifiersOrderToggle.onValueChanged.AddListener(_val => CurrentSelectedBG.orderModifiers = _val);

            var x = BackgroundEditor.inst.left.Find("block/x");
            var xif = x.GetComponent<InputField>();
            var left = x.Find("<").GetComponent<Button>();
            var right = x.Find(">").GetComponent<Button>();

            xif.onValueChanged.ClearAll();
            xif.text = currentPage.ToString();
            xif.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int page))
                {
                    currentPage = Mathf.Clamp(page, 0, backgroundObject.modifiers.Count - 1);
                    StartCoroutine(RenderModifiers(backgroundObject));
                }
            });

            left.onClick.ClearAll();
            left.onClick.AddListener(() =>
            {
                if (int.TryParse(xif.text, out int page))
                    xif.text = Mathf.Clamp(page - 1, 0, backgroundObject.modifiers.Count - 1).ToString();
            });

            right.onClick.ClearAll();
            right.onClick.AddListener(() =>
            {
                if (int.TryParse(xif.text, out int page))
                    xif.text = Mathf.Clamp(page + 1, 0, backgroundObject.modifiers.Count - 1).ToString();
            });

            TriggerHelper.AddEventTriggers(xif.gameObject, TriggerHelper.ScrollDeltaInt(xif, max: backgroundObject.modifiers.Count - 1));

            var addBlockButton = x.Find("add").GetComponent<Button>();
            addBlockButton.onClick.ClearAll();
            addBlockButton.onClick.AddListener(() =>
            {
                if (backgroundObject.modifiers.Count > 0 && backgroundObject.modifiers[backgroundObject.modifiers.Count - 1].Count < 1)
                {
                    EditorManager.inst.DisplayNotification($"Modifier Block {currentPage} requires modifiers before adding a new block!", 2f, EditorManager.NotificationType.Warning);
                    return;
                }

                AddBlock(backgroundObject);
            });

            var removeBlockButton = x.Find("del").GetComponent<Button>();
            removeBlockButton.onClick.ClearAll();
            removeBlockButton.onClick.AddListener(() =>
            {
                if (backgroundObject.modifiers.Count < 1)
                    return;

                RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this modifier block?", () =>
                {
                    DelBlock(backgroundObject);
                    RTEditor.inst.HideWarningPopup();
                }, RTEditor.inst.HideWarningPopup);
            });

            if (!showModifiers || backgroundObject.modifiers.Count <= currentPage)
                yield break;

            content.parent.parent.AsRT().sizeDelta = new Vector2(351f, 500f);

            int num = 0;
            foreach (var modifier in backgroundObject.modifiers[currentPage])
            {
                int index = num;
                var gameObject = modifierCardPrefab.Duplicate(content, modifier.commands[0]);
                EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.List_Button_1_Normal, true);
                gameObject.transform.localScale = Vector3.one;
                var modifierTitle = gameObject.transform.Find("Label/Text").GetComponent<Text>();
                modifierTitle.text = modifier.commands[0];
                EditorThemeManager.ApplyLightText(modifierTitle);

                var collapse = gameObject.transform.Find("Label/Collapse").GetComponent<Toggle>();
                collapse.onValueChanged.ClearAll();
                collapse.isOn = modifier.collapse;
                collapse.onValueChanged.AddListener(_val =>
                {
                    modifier.collapse = _val;
                    StartCoroutine(RenderModifiers(backgroundObject));
                });

                TooltipHelper.AssignTooltip(collapse.gameObject, "Collapse Modifier");
                EditorThemeManager.ApplyToggle(collapse, ThemeGroup.List_Button_1_Normal);

                for (int i = 0; i < collapse.transform.Find("dots").childCount; i++)
                    EditorThemeManager.ApplyGraphic(collapse.transform.Find("dots").GetChild(i).GetComponent<Image>(), ThemeGroup.Dark_Text);

                var delete = gameObject.transform.Find("Label/Delete").GetComponent<DeleteButtonStorage>();
                delete.button.onClick.ClearAll();
                delete.button.onClick.AddListener(() =>
                {
                    backgroundObject.modifiers[currentPage].RemoveAt(index);
                    backgroundObject.positionOffset = Vector3.zero;
                    backgroundObject.scaleOffset = Vector3.zero;
                    backgroundObject.rotationOffset = Vector3.zero;

                    Destroy(backgroundObject.BaseObject);
                    Updater.CreateBackgroundObject(backgroundObject);

                    StartCoroutine(RenderModifiers(backgroundObject));
                });

                TooltipHelper.AssignTooltip(delete.gameObject, "Delete Modifier");
                EditorThemeManager.ApplyGraphic(delete.button.image, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(delete.image, ThemeGroup.Delete_Text);

                var copy = gameObject.transform.Find("Label/Copy").GetComponent<DeleteButtonStorage>();
                copy.button.onClick.ClearAll();
                copy.button.onClick.AddListener(() =>
                {
                    copiedModifier = Modifier<BackgroundObject>.DeepCopy(modifier, backgroundObject);
                    StartCoroutine(RenderModifiers(backgroundObject));
                    EditorManager.inst.DisplayNotification("Copied Modifier!", 1.5f, EditorManager.NotificationType.Success);
                });

                TooltipHelper.AssignTooltip(copy.gameObject, "Copy Modifier");
                EditorThemeManager.ApplyGraphic(copy.button.image, ThemeGroup.Copy, true);
                EditorThemeManager.ApplyGraphic(copy.image, ThemeGroup.Copy_Text);

                var notifier = gameObject.AddComponent<ModifierActiveNotifier>();
                notifier.modifierBase = modifier;
                notifier.notifier = gameObject.transform.Find("Label/Notifier").gameObject.GetComponent<Image>();
                TooltipHelper.AssignTooltip(notifier.notifier.gameObject, "Notifier Modifier");
                EditorThemeManager.ApplyGraphic(notifier.notifier, ThemeGroup.Warning_Confirm, true);

                if (modifier.collapse)
                {
                    num++;
                    continue;
                }

                var layout = gameObject.transform.Find("Layout");

                var constant = booleanBar.Duplicate(layout, "Constant");
                constant.transform.localScale = Vector3.one;

                var constantText = constant.transform.Find("Text").GetComponent<Text>();
                constantText.text = "Constant";

                var constantToggle = constant.transform.Find("Toggle").GetComponent<Toggle>();
                constantToggle.onValueChanged.ClearAll();
                constantToggle.isOn = modifier.constant;
                constantToggle.onValueChanged.AddListener(_val =>
                {
                    modifier.constant = _val;
                    modifier.active = false;
                });

                TooltipHelper.AssignTooltip(constantToggle.gameObject, "Constant Modifier");
                EditorThemeManager.ApplyLightText(constantText);
                EditorThemeManager.ApplyToggle(constantToggle);

                if (modifier.type == ModifierBase.Type.Trigger)
                {
                    var not = booleanBar.Duplicate(layout, "Not");
                    not.transform.localScale = Vector3.one;
                    var notText = not.transform.Find("Text").GetComponent<Text>();
                    notText.text = "Not";

                    var notToggle = not.transform.Find("Toggle").GetComponent<Toggle>();
                    notToggle.onValueChanged.ClearAll();
                    notToggle.isOn = modifier.not;
                    notToggle.onValueChanged.AddListener(_val =>
                    {
                        modifier.not = _val;
                        modifier.active = false;
                    });

                    TooltipHelper.AssignTooltip(notToggle.gameObject, "Trigger Not Modifier");
                    EditorThemeManager.ApplyLightText(notText);
                    EditorThemeManager.ApplyToggle(notToggle);

                    var elseIf = booleanBar.Duplicate(layout, "Not");
                    elseIf.transform.localScale = Vector3.one;
                    var elseIfText = elseIf.transform.Find("Text").GetComponent<Text>();
                    elseIfText.text = "Else If";

                    var elseIfToggle = elseIf.transform.Find("Toggle").GetComponent<Toggle>();
                    elseIfToggle.onValueChanged.ClearAll();
                    elseIfToggle.isOn = modifier.elseIf;
                    elseIfToggle.onValueChanged.AddListener(_val =>
                    {
                        modifier.elseIf = _val;
                        modifier.active = false;
                    });

                    TooltipHelper.AssignTooltip(elseIfToggle.gameObject, "Trigger Else If Modifier");
                    EditorThemeManager.ApplyLightText(elseIfText);
                    EditorThemeManager.ApplyToggle(elseIfToggle);
                }

                if (!modifier.verified)
                {
                    modifier.verified = true;
                    modifier.VerifyModifier(ModifiersManager.defaultBackgroundObjectModifiers);
                }

                if (!modifier.IsValid(ModifiersManager.defaultBackgroundObjectModifiers))
                {
                    EditorManager.inst.DisplayNotification("Modifier does not have a command name and is lacking values.", 2f, EditorManager.NotificationType.Error);
                    continue;
                }

                gameObject.AddComponent<Button>();
                var modifierContextMenu = gameObject.AddComponent<ContextClickable>();
                modifierContextMenu.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    var buttonFunctions = new List<ButtonFunction>()
                    {
                        new ButtonFunction("Add", () =>
                        {
                            EditorManager.inst.ShowDialog("Default Modifiers Popup");
                            RefreshDefaultModifiersList(backgroundObject);
                        }),
                        new ButtonFunction("Add Above", () =>
                        {
                            EditorManager.inst.ShowDialog("Default Modifiers Popup");
                            RefreshDefaultModifiersList(backgroundObject, index);
                        }),
                        new ButtonFunction("Add Below", () =>
                        {
                            EditorManager.inst.ShowDialog("Default Modifiers Popup");
                            RefreshDefaultModifiersList(backgroundObject, index + 1);
                        }),
                        new ButtonFunction("Delete", () =>
                        {
                            backgroundObject.modifiers[currentPage].RemoveAt(index);
                            backgroundObject.positionOffset = Vector3.zero;
                            backgroundObject.scaleOffset = Vector3.zero;
                            backgroundObject.rotationOffset = Vector3.zero;

                            Destroy(backgroundObject.BaseObject);
                            Updater.CreateBackgroundObject(backgroundObject);

                            StartCoroutine(RenderModifiers(backgroundObject));
                        }),
                        new ButtonFunction(true),
                        new ButtonFunction("Copy", () =>
                        {
                            copiedModifier = Modifier<BackgroundObject>.DeepCopy(modifier, backgroundObject);
                            StartCoroutine(RenderModifiers(backgroundObject));
                            EditorManager.inst.DisplayNotification("Copied Modifier!", 1.5f, EditorManager.NotificationType.Success);
                        }),
                        new ButtonFunction("Paste", () =>
                        {
                            if (copiedModifier == null)
                                return;

                            backgroundObject.modifiers[currentPage].Add(Modifier<BackgroundObject>.DeepCopy(copiedModifier, backgroundObject));
                            StartCoroutine(RenderModifiers(backgroundObject));
                            EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
                        }),
                        new ButtonFunction("Paste Above", () =>
                        {
                            if (copiedModifier == null)
                                return;

                            backgroundObject.modifiers[currentPage].Insert(index, Modifier<BackgroundObject>.DeepCopy(copiedModifier, backgroundObject));
                            StartCoroutine(RenderModifiers(backgroundObject));
                            EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
                        }),
                        new ButtonFunction("Paste Below", () =>
                        {
                            if (copiedModifier == null)
                                return;

                            backgroundObject.modifiers[currentPage].Insert(index + 1, Modifier<BackgroundObject>.DeepCopy(copiedModifier, backgroundObject));
                            StartCoroutine(RenderModifiers(backgroundObject));
                            EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
                        }),
                        new ButtonFunction(true),
                        new ButtonFunction("Sort Modifiers", () =>
                        {
                            backgroundObject.modifiers[currentPage] = backgroundObject.modifiers[currentPage].OrderBy(x => x.type == ModifierBase.Type.Action).ToList();
                            StartCoroutine(RenderModifiers(backgroundObject));
                        }),
                        new ButtonFunction("Move Up", () =>
                        {
                            if (index <= 0)
                            {
                                EditorManager.inst.DisplayNotification("Could not move modifier up since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                                return;
                            }

                            backgroundObject.modifiers[currentPage].Move(index, index - 1);
                            StartCoroutine(RenderModifiers(backgroundObject));
                        }),
                        new ButtonFunction("Move Down", () =>
                        {
                            if (index >= backgroundObject.modifiers[currentPage].Count - 1)
                            {
                                EditorManager.inst.DisplayNotification("Could not move modifier up since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                                return;
                            }

                            backgroundObject.modifiers[currentPage].Move(index, index + 1);
                            StartCoroutine(RenderModifiers(backgroundObject));
                        }),
                        new ButtonFunction("Move to Start", () =>
                        {
                            backgroundObject.modifiers[currentPage].Move(index, 0);
                            StartCoroutine(RenderModifiers(backgroundObject));
                        }),
                        new ButtonFunction("Move to End", () =>
                        {
                            backgroundObject.modifiers[currentPage].Move(index, backgroundObject.modifiers[currentPage].Count - 1);
                            StartCoroutine(RenderModifiers(backgroundObject));
                        }),
                        new ButtonFunction(true),
                        new ButtonFunction("Update Modifier", () =>
                        {
                            modifier.active = false;
                            modifier.Inactive?.Invoke(modifier);
                        })
                    };
                    if (ModCompatibility.UnityExplorerInstalled)
                        buttonFunctions.Add(new ButtonFunction("Inspect", () => ModCompatibility.Inspect(modifier)));

                    RTEditor.inst.ShowContextMenu(buttonFunctions);
                };

                var cmd = modifier.commands[0];
                switch (cmd)
                {
                    case "setActive":
                        {
                            ObjectModifiersEditor.inst.BoolGenerator(modifier, layout, "Active", 0, false);

                            break;
                        }
                    case "setActiveOther":
                        {
                            ObjectModifiersEditor.inst.BoolGenerator(modifier, layout, "Active", 0, false);
                            ObjectModifiersEditor.inst.StringGenerator(modifier, layout, "BG Group", 1);

                            break;
                        }
                    case "timeLesserEquals":
                    case "timeGreaterEquals":
                    case "timeLesser":
                    case "timeGreater":
                        {
                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "Time", 0, 0f);

                            break;
                        }
                    case "animateObject":
                    case "animateObjectOther":
                        {
                            if (cmd.Contains("Other"))
                                ObjectModifiersEditor.inst.StringGenerator(modifier, layout, "BG Group", 7);

                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "Time", 0, 1f);
                            ObjectModifiersEditor.inst.DropdownGenerator(modifier, layout, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "X", 2, 0f);
                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "Y", 3, 0f);
                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "Z", 4, 0f);
                            ObjectModifiersEditor.inst.BoolGenerator(modifier, layout, "Relative", 5, true);
                            ObjectModifiersEditor.inst.DropdownGenerator(modifier, layout, "Easing", 6, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                            break;
                        }
                    case "copyAxis":
                        {
                            ObjectModifiersEditor.inst.StringGenerator(modifier, layout, "Object Group", 0);
                            ObjectModifiersEditor.inst.DropdownGenerator(modifier, layout, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                            ObjectModifiersEditor.inst.DropdownGenerator(modifier, layout, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));
                            ObjectModifiersEditor.inst.DropdownGenerator(modifier, layout, "To Type", 3, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                            ObjectModifiersEditor.inst.DropdownGenerator(modifier, layout, "To Axis (3D)", 4, CoreHelper.StringToOptionData("X", "Y", "Z"));

                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "Delay", 5, 0f);
                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "Multiply", 6, 1f);
                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "Offset", 7, 0f);
                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "Min", 8, -99999f);
                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "Max", 9, 99999f);
                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "Loop", 10, 99999f);

                            break;
                        }
                }

                num++;
            }

            //Add Modifier
            {
                var gameObject = modifierAddPrefab.Duplicate(content, "add modifier");
                TooltipHelper.AssignTooltip(gameObject, "Add Modifier");

                var button = gameObject.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(() =>
                {
                    EditorManager.inst.ShowDialog("Default Background Modifiers Popup");
                    RefreshDefaultModifiersList(backgroundObject);
                });

                EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(gameObject.transform.GetChild(0).GetComponent<Text>());
            }

            // Paste Modifier
            PasteGenerator(backgroundObject);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content.AsRT());

            yield break;
        }

        public void AddBlock(BackgroundObject backgroundObject)
        {
            backgroundObject.modifiers.Add(new List<Modifier<BackgroundObject>>());
            currentPage = backgroundObject.modifiers.Count - 1;
            StartCoroutine(RenderModifiers(backgroundObject));
        }

        public void DelBlock(BackgroundObject backgroundObject)
        {
            backgroundObject.modifiers.RemoveAt(currentPage);
            currentPage = Mathf.Clamp(currentPage - 1, 0, backgroundObject.modifiers.Count - 1);
            StartCoroutine(RenderModifiers(backgroundObject));
        }

        public void SetObjectColors(Toggle[] toggles, int index, int i, Modifier<BackgroundObject> modifier)
        {
            modifier.commands[index] = i.ToString();

            int num = 0;
            foreach (var toggle in toggles)
            {
                int toggleIndex = num;
                toggle.onValueChanged.ClearAll();
                toggle.isOn = num == i;
                toggle.onValueChanged.AddListener(_val => SetObjectColors(toggles, index, toggleIndex, modifier));

                toggle.GetComponent<Image>().color = GameManager.inst.LiveTheme.GetObjColor(toggleIndex);

                if (!toggle.GetComponent<HoverUI>())
                {
                    var hoverUI = toggle.gameObject.AddComponent<HoverUI>();
                    hoverUI.animatePos = false;
                    hoverUI.animateSca = true;
                    hoverUI.size = 1.1f;
                }
                num++;
            }
        }

        GameObject pasteModifier;
        public void PasteGenerator(BackgroundObject backgroundObject)
        {
            if (copiedModifier == null)
                return;

            if (pasteModifier)
                CoreHelper.Destroy(pasteModifier);

            pasteModifier = EditorPrefabHolder.Instance.Function1Button.Duplicate(content, "paste modifier");
            pasteModifier.transform.AsRT().sizeDelta = new Vector2(350f, 32f);
            var buttonStorage = pasteModifier.GetComponent<FunctionButtonStorage>();
            buttonStorage.text.text = "Paste";
            buttonStorage.button.onClick.ClearAll();
            buttonStorage.button.onClick.AddListener(() =>
            {
                backgroundObject.modifiers[currentPage].Add(Modifier<BackgroundObject>.DeepCopy(copiedModifier, backgroundObject));
                StartCoroutine(RenderModifiers(backgroundObject));
                EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
            });

            TooltipHelper.AssignTooltip(pasteModifier, "Paste Modifier");
            EditorThemeManager.ApplyGraphic(buttonStorage.button.image, ThemeGroup.Paste, true);
            EditorThemeManager.ApplyGraphic(buttonStorage.text, ThemeGroup.Paste_Text);
        }

        #endregion

        #region Default Modifiers

        public int currentPage;

        public string searchTerm;
        public int addIndex = -1;
        public void RefreshDefaultModifiersList(BackgroundObject backgroundObject, int addIndex = -1)
        {
            this.addIndex = addIndex;
            defaultModifiers = ModifiersManager.defaultBackgroundObjectModifiers;

            var dialog = EditorManager.inst.GetDialog("Default Background Modifiers Popup").Dialog.gameObject;

            var contentM = dialog.transform.Find("mask/content");
            LSHelpers.DeleteChildren(contentM);

            for (int i = 0; i < defaultModifiers.Count; i++)
            {
                if (string.IsNullOrEmpty(searchTerm) || defaultModifiers[i].commands[0].ToLower().Contains(searchTerm.ToLower()))
                {
                    int tmpIndex = i;

                    var name = defaultModifiers[i].commands[0] + " (" + defaultModifiers[i].type.ToString() + ")";

                    var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(contentM, name);

                    var modifierName = gameObject.transform.GetChild(0).GetComponent<Text>();
                    modifierName.text = name;

                    var button = gameObject.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(() =>
                    {
                        var cmd = defaultModifiers[tmpIndex].commands[0];

                        var modifier = Modifier<BackgroundObject>.DeepCopy(defaultModifiers[tmpIndex], backgroundObject);
                        if (addIndex == -1)
                            backgroundObject.modifiers[currentPage].Add(modifier);
                        else
                            backgroundObject.modifiers[currentPage].Insert(Mathf.Clamp(addIndex, 0, backgroundObject.modifiers[currentPage].Count), modifier);
                        StartCoroutine(RenderModifiers(backgroundObject));
                        EditorManager.inst.HideDialog("Default Background Modifiers Popup");
                    });

                    EditorThemeManager.ApplyLightText(modifierName);
                    EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                }
            }
        }

        public List<Modifier<BackgroundObject>> defaultModifiers = new List<Modifier<BackgroundObject>>();

        #endregion

        #region UI Part Handlers

        GameObject booleanBar;

        GameObject numberInput;

        GameObject stringInput;

        GameObject dropdownBar;

        GameObject Base(string name)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(transform);
            gameObject.transform.localScale = Vector3.one;

            var rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0f, 32f);

            var horizontalLayoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.childControlWidth = false;
            horizontalLayoutGroup.childForceExpandWidth = false;
            horizontalLayoutGroup.spacing = 8f;

            var text = new GameObject("Text");
            text.transform.SetParent(rectTransform);
            text.transform.localScale = Vector3.one;
            var textRT = text.AddComponent<RectTransform>();
            textRT.anchoredPosition = new Vector2(10f, -5f);
            textRT.anchorMax = Vector2.one;
            textRT.anchorMin = Vector2.zero;
            textRT.pivot = new Vector2(0f, 1f);
            textRT.sizeDelta = new Vector2(247f, 32f);

            var textText = text.AddComponent<Text>();
            textText.alignment = TextAnchor.MiddleLeft;
            textText.font = FontManager.inst.DefaultFont;
            textText.fontSize = 19;
            textText.color = new Color(0.9373f, 0.9216f, 0.9373f);

            return gameObject;
        }

        GameObject Boolean()
        {
            var gameObject = Base("Bool");
            var rectTransform = (RectTransform)gameObject.transform;

            ((RectTransform)rectTransform.Find("Text")).sizeDelta = new Vector2(266f, 32f);

            var toggleBase = new GameObject("Toggle");
            toggleBase.transform.SetParent(rectTransform);
            toggleBase.transform.localScale = Vector3.one;

            var toggleBaseRT = toggleBase.AddComponent<RectTransform>();

            toggleBaseRT.anchorMax = Vector2.one;
            toggleBaseRT.anchorMin = Vector2.zero;
            toggleBaseRT.sizeDelta = new Vector2(32f, 32f);

            var toggle = toggleBase.AddComponent<Toggle>();

            var background = new GameObject("Background");
            background.transform.SetParent(toggleBaseRT);
            background.transform.localScale = Vector3.one;

            var backgroundRT = background.AddComponent<RectTransform>();
            backgroundRT.anchoredPosition = Vector3.zero;
            backgroundRT.anchorMax = new Vector2(0f, 1f);
            backgroundRT.anchorMin = new Vector2(0f, 1f);
            backgroundRT.pivot = new Vector2(0f, 1f);
            backgroundRT.sizeDelta = new Vector2(32f, 32f);
            var backgroundImage = background.AddComponent<Image>();

            var checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(backgroundRT);
            checkmark.transform.localScale = Vector3.one;

            var checkmarkRT = checkmark.AddComponent<RectTransform>();
            checkmarkRT.anchoredPosition = Vector3.zero;
            checkmarkRT.anchorMax = new Vector2(0.5f, 0.5f);
            checkmarkRT.anchorMin = new Vector2(0.5f, 0.5f);
            checkmarkRT.pivot = new Vector2(0.5f, 0.5f);
            checkmarkRT.sizeDelta = new Vector2(20f, 20f);
            var checkmarkImage = checkmark.AddComponent<Image>();
            checkmarkImage.sprite = SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_checkmark.png");
            checkmarkImage.color = new Color(0.1294f, 0.1294f, 0.1294f);

            toggle.image = backgroundImage;
            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkmarkImage;

            return gameObject;
        }

        GameObject NumberInput()
        {
            var gameObject = Base("Number");
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.localScale = Vector2.one;

            ((RectTransform)rectTransform.Find("Text")).sizeDelta = new Vector2(146f, 32f);

            var input = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(rectTransform, "Input");
            input.transform.localScale = Vector2.one;
            ((RectTransform)input.transform.Find("Text")).sizeDelta = Vector2.zero;

            var buttonL = Button("<", SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_left_small.png"));
            buttonL.transform.SetParent(rectTransform);
            buttonL.transform.localScale = Vector3.one;

            ((RectTransform)buttonL.transform).sizeDelta = new Vector2(16f, 32f);

            var buttonR = Button(">", SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_right_small.png"));
            buttonR.transform.SetParent(rectTransform);
            buttonR.transform.localScale = Vector3.one;

            ((RectTransform)buttonR.transform).sizeDelta = new Vector2(16f, 32f);

            return gameObject;
        }

        GameObject StringInput()
        {
            var gameObject = Base("String");
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.localScale = Vector2.one;

            ((RectTransform)rectTransform.Find("Text")).sizeDelta = new Vector2(146f, 32f);

            var input = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(rectTransform, "Input");
            input.transform.localScale = Vector2.one;
            ((RectTransform)input.transform).sizeDelta = new Vector2(152f, 32f);
            ((RectTransform)input.transform.Find("Text")).sizeDelta = Vector2.zero;

            return gameObject;
        }

        GameObject Dropdown()
        {
            var gameObject = Base("Dropdown");
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.localScale = Vector2.one;

            ((RectTransform)rectTransform.Find("Text")).sizeDelta = new Vector2(146f, 32f);

            var dropdownInput = EditorPrefabHolder.Instance.CurvesDropdown.Duplicate(rectTransform, "Dropdown");
            dropdownInput.transform.localScale = Vector2.one;

            return gameObject;
        }

        GameObject Button(string name, Sprite sprite)
        {
            var gameObject = new GameObject(name);
            var rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.localScale = Vector2.one;

            var image = gameObject.AddComponent<Image>();
            image.color = new Color(0.8784f, 0.8784f, 0.8784f);
            image.sprite = sprite;

            var button = gameObject.AddComponent<Button>();
            button.colors = UIManager.SetColorBlock(button.colors, Color.white, new Color(0.898f, 0.451f, 0.451f, 1f), Color.white, Color.white, Color.red);

            return gameObject;
        }

        #endregion
    }
}
