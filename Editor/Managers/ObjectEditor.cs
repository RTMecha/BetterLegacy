using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Core.Optimization.Objects;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;
using BetterLegacy.Example;
using Crosstales.FB;
using HarmonyLib;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AutoKillType = DataManager.GameData.BeatmapObject.AutoKillType;
using BaseEventKeyframe = DataManager.GameData.EventKeyframe;
using ObjectType = BetterLegacy.Core.Data.BeatmapObject.ObjectType;
using BetterLegacy.Core.Components;
using TMPro;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;

namespace BetterLegacy.Editor.Managers
{
    public class ObjectEditor : MonoBehaviour
    {
        #region Init

        public static ObjectEditor inst;

        public static void Init() => ObjEditor.inst.gameObject.AddComponent<ObjectEditor>();

        void Awake()
        {
            inst = this;

            var objectView = ObjEditor.inst.ObjectView.transform;
            var dialog = ObjEditor.inst.ObjectView.transform.parent.parent.parent.parent.parent; // lol wtf
            var right = dialog.Find("data/right");

            right.gameObject.AddComponent<Mask>();

            var todDropdown = objectView.Find("autokill/tod-dropdown");
            var hide = todDropdown.GetComponent<HideDropdownOptions>();
            hide.DisabledOptions[0] = false;
            hide.remove = true;
            var template = todDropdown.transform.Find("Template/Viewport/Content").gameObject;
            var vlg = template.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;

            var csf = template.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.MinSize;

            objectView.Find("name/name").GetComponent<InputField>().characterLimit = 0;

            // Labels
            for (int j = 0; j < objectView.childCount; j++)
            {
                var label = objectView.GetChild(j);
                if (label.name == "label" || label.name == "collapselabel")
                {
                    for (int k = 0; k < label.childCount; k++)
                    {
                        var labelText = label.GetChild(k).GetComponent<Text>();
                        EditorThemeManager.AddLightText(labelText);
                    }
                }
            }

            for (int i = 0; i < ObjEditor.inst.KeyframeDialogs.Count; i++)
            {
                var kfdialog = ObjEditor.inst.KeyframeDialogs[i].transform;

                for (int j = 0; j < kfdialog.childCount; j++)
                {
                    var label = kfdialog.GetChild(j);
                    if (label.name == "label")
                    {
                        for (int k = 0; k < label.childCount; k++)
                        {
                            var labelText = label.GetChild(k).GetComponent<Text>();
                            EditorThemeManager.AddLightText(labelText);
                        }
                    }
                }
            }

            var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");

            // Depth
            {
                var spacer = Creator.NewUIObject("depth input", objectView, 15);

                var spHLG = spacer.AddComponent<HorizontalLayoutGroup>();

                spacer.transform.AsRT().sizeDelta = new Vector2(30f, 30f);
                spHLG.spacing = 8;

                var depth = singleInput.Duplicate(spacer.transform, "depth");
                depth.transform.localScale = Vector3.one;
                depth.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);

                Destroy(depth.GetComponent<EventInfo>());

                var depthif = depth.GetComponent<InputField>();
                depthif.onValueChanged.RemoveAllListeners();

                var sliderObject = objectView.Find("depth/depth").gameObject;

                var depthLeft = objectView.Find("depth/<").gameObject;
                var depthRight = objectView.Find("depth/>").gameObject;
                EditorHelper.SetComplexity(depthLeft, Complexity.Simple);
                EditorHelper.SetComplexity(depthRight, Complexity.Simple);
                EditorThemeManager.AddSelectable(depthLeft.GetComponent<Button>(), ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(depthRight.GetComponent<Button>(), ThemeGroup.Function_2, false);

                sliderObject.transform.AsRT().sizeDelta = new Vector2(RTEditor.NotSimple ? 352f : 292f, 32f);
                objectView.Find("depth").AsRT().sizeDelta = new Vector2(261f, 32f);

                EditorThemeManager.AddInputField(depthif);
                var leftButton = depth.transform.Find(">").GetComponent<Button>();
                var rightButton = depth.transform.Find("<").GetComponent<Button>();
                Destroy(leftButton.GetComponent<Animator>());
                Destroy(rightButton.GetComponent<Animator>());
                leftButton.transition = Selectable.Transition.ColorTint;
                rightButton.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddSelectable(leftButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(rightButton, ThemeGroup.Function_2, false);

                var depthSlider = sliderObject.GetComponent<Slider>();
                var depthSliderImage = sliderObject.transform.Find("Image").GetComponent<Image>();
                depthSlider.colors = UIManager.SetColorBlock(depthSlider.colors, Color.white, new Color(0.9f, 0.9f, 0.9f), Color.white, Color.white, Color.white);

                EditorThemeManager.AddGraphic(depthSliderImage, ThemeGroup.Slider_2, true);
                EditorThemeManager.AddGraphic(depthSlider.image, ThemeGroup.Slider_2_Handle, true);
            }

            // Lock
            {
                var timeParent = objectView.Find("time");

                var locker = EditorPrefabHolder.Instance.Toggle.Duplicate(timeParent.transform, "lock", 0);
                locker.transform.localScale = Vector3.one;

                var timeLayout = timeParent.GetComponent<HorizontalLayoutGroup>();
                timeLayout.childControlWidth = false;
                timeLayout.childForceExpandWidth = false;

                locker.transform.AsRT().sizeDelta = new Vector2(32f, 32f);

                var time = timeParent.Find("time");
                time.AsRT().sizeDelta = new Vector2(151, 32f);
                var lockToggle = locker.GetComponent<Toggle>();

                ((Image)lockToggle.graphic).sprite = ObjEditor.inst.timelineObjectPrefabLock.transform.Find("lock (1)").GetComponent<Image>().sprite;

                EditorThemeManager.AddToggle(lockToggle);

                timeParent.Find("<<").AsRT().sizeDelta = new Vector2(32f, 32f);
                timeParent.Find("<").AsRT().sizeDelta = new Vector2(16f, 32f);
                timeParent.Find("|").AsRT().sizeDelta = new Vector2(16f, 32f);
                timeParent.Find(">").AsRT().sizeDelta = new Vector2(16f, 32f);
                timeParent.Find(">>").AsRT().sizeDelta = new Vector2(32f, 32f);

                DestroyImmediate(timeParent.Find("<<").GetComponent<Animator>());
                var leftGreaterButton = timeParent.Find("<<").GetComponent<Button>();
                leftGreaterButton.transition = Selectable.Transition.ColorTint;
                DestroyImmediate(timeParent.Find("<").GetComponent<Animator>());
                var leftButton = timeParent.Find("<").GetComponent<Button>();
                leftButton.transition = Selectable.Transition.ColorTint;
                DestroyImmediate(timeParent.Find("|").GetComponent<Animator>());
                var middleButton = timeParent.Find("|").GetComponent<Button>();
                middleButton.transition = Selectable.Transition.ColorTint;
                DestroyImmediate(timeParent.Find(">").GetComponent<Animator>());
                var rightButton = timeParent.Find(">").GetComponent<Button>();
                rightButton.transition = Selectable.Transition.ColorTint;
                DestroyImmediate(timeParent.Find(">>").GetComponent<Animator>());
                var rightGreaterButton = timeParent.Find(">>").GetComponent<Button>();
                rightGreaterButton.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddSelectable(leftGreaterButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(leftButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(middleButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(rightButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(rightGreaterButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddInputField(timeParent.Find("time").GetComponent<InputField>());
            }

            // Colors
            {
                var colorParent = ObjEditor.inst.KeyframeDialogs[3].transform.Find("color");
                colorParent.GetComponent<GridLayoutGroup>().spacing = new Vector2(9.32f, 9.32f);

                ObjEditor.inst.KeyframeDialogs[3].transform.GetChild(colorParent.GetSiblingIndex() - 1).gameObject.name = "color_label";

                for (int i = 1; i < 19; i++)
                {
                    if (i >= 10)
                        colorParent.Find("9").gameObject.Duplicate(colorParent, i.ToString());

                    var toggle = colorParent.Find(i.ToString()).GetComponent<Toggle>();

                    EditorThemeManager.AddGraphic(toggle.image, ThemeGroup.Null, true);
                    EditorThemeManager.AddGraphic(toggle.graphic, ThemeGroup.Background_3);
                }
            }

            // Origin X / Y
            {
                var contentOriginTF = objectView.transform.Find("origin").transform;

                EditorHelper.SetComplexity(contentOriginTF.Find("origin-x").gameObject, Complexity.Simple);
                EditorHelper.SetComplexity(contentOriginTF.Find("origin-y").gameObject, Complexity.Simple);

                try
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        var origin = contentOriginTF.Find("origin-x/" + i);
                        EditorThemeManager.AddToggle(origin.GetComponent<Toggle>(), ThemeGroup.Background_1);
                        EditorThemeManager.AddGraphic(origin.Find("Image").GetComponent<Image>(), ThemeGroup.Toggle_1_Check);
                    }
                    for (int i = 1; i <= 3; i++)
                    {
                        var origin = contentOriginTF.Find("origin-y/" + i);
                        EditorThemeManager.AddToggle(origin.GetComponent<Toggle>(), ThemeGroup.Background_1);
                        EditorThemeManager.AddGraphic(origin.Find("Image").GetComponent<Image>(), ThemeGroup.Toggle_1_Check);
                    }
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }

                var xo = singleInput.Duplicate(contentOriginTF.transform, "x");
                xo.transform.localScale = Vector3.one;
                xo.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);

                Destroy(xo.GetComponent<EventInfo>());

                var xoif = xo.GetComponent<InputField>();
                xoif.onValueChanged.RemoveAllListeners();

                var yo = singleInput.Duplicate(contentOriginTF, "y");
                yo.transform.localScale = Vector3.one;
                yo.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);

                Destroy(yo.GetComponent<EventInfo>());

                var yoif = yo.GetComponent<InputField>();
                yoif.onValueChanged.RemoveAllListeners();

                EditorThemeManager.AddInputField(xoif);
                var xLeftButton = xo.transform.Find(">").GetComponent<Button>();
                var xRightButton = xo.transform.Find("<").GetComponent<Button>();
                Destroy(xLeftButton.GetComponent<Animator>());
                Destroy(xRightButton.GetComponent<Animator>());
                xLeftButton.transition = Selectable.Transition.ColorTint;
                xRightButton.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddSelectable(xLeftButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(xRightButton, ThemeGroup.Function_2, false);

                EditorThemeManager.AddInputField(yoif);
                var yLeftButton = yo.transform.Find(">").GetComponent<Button>();
                var yRightButton = yo.transform.Find("<").GetComponent<Button>();
                Destroy(yLeftButton.GetComponent<Animator>());
                Destroy(yRightButton.GetComponent<Animator>());
                yLeftButton.transition = Selectable.Transition.ColorTint;
                yRightButton.transition = Selectable.Transition.ColorTint;

                EditorThemeManager.AddSelectable(yLeftButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(yRightButton, ThemeGroup.Function_2, false);

                EditorHelper.SetComplexity(xo, Complexity.Normal);
                EditorHelper.SetComplexity(yo, Complexity.Normal);
            }

            // Opacity
            {
                var opacityLabel = EditorPrefabHolder.Instance.Labels.Duplicate(ObjEditor.inst.KeyframeDialogs[3].transform, "opacity_label");
                opacityLabel.transform.localScale = Vector3.one;
                var opacityLabelText = opacityLabel.transform.GetChild(0).GetComponent<Text>();
                opacityLabelText.text = "Opacity";

                EditorThemeManager.AddLightText(opacityLabelText);

                var opacity = Instantiate(ObjEditor.inst.KeyframeDialogs[2].transform.Find("rotation").gameObject);
                opacity.transform.SetParent(ObjEditor.inst.KeyframeDialogs[3].transform);
                opacity.transform.localScale = Vector3.one;
                opacity.name = "opacity";

                var collisionToggle = EditorPrefabHolder.Instance.ToggleButton.Duplicate(opacity.transform, "collision");

                var collisionToggleText = collisionToggle.transform.Find("Text").GetComponent<Text>();
                collisionToggleText.text = "Collide";
                opacity.transform.Find("x/input").AsRT().sizeDelta = new Vector2(136f, 32f);

                EditorThemeManager.AddInputFields(opacity, true, "");
                EditorThemeManager.AddToggle(collisionToggle.GetComponent<Toggle>(), graphic: collisionToggleText);
            }

            // Hue / Sat / Val
            {
                var hsvLabels = EditorPrefabHolder.Instance.Labels.Duplicate(ObjEditor.inst.KeyframeDialogs[3].transform, "huesatval_label");
                hsvLabels.transform.GetChild(0).AsRT().sizeDelta = new Vector2(120f, 20f);
                hsvLabels.transform.GetChild(0).GetComponent<Text>().text = "Hue";

                var saturationLabel = hsvLabels.transform.GetChild(0).gameObject.Duplicate(hsvLabels.transform);
                saturationLabel.transform.AsRT().sizeDelta = new Vector2(120f, 20f);
                saturationLabel.GetComponent<Text>().text = "Saturation";

                var valueLabel = hsvLabels.transform.GetChild(0).gameObject.Duplicate(hsvLabels.transform);
                valueLabel.GetComponent<Text>().text = "Value";

                var opacity = ObjEditor.inst.KeyframeDialogs[1].transform.Find("scale").gameObject.Duplicate(ObjEditor.inst.KeyframeDialogs[3].transform);
                opacity.name = "huesatval";

                opacity.transform.GetChild(1).gameObject.Duplicate(opacity.transform, "z");

                for (int i = 0; i < opacity.transform.childCount; i++)
                {
                    if (!opacity.transform.GetChild(i).GetComponent<InputFieldSwapper>())
                    {
                        var inputField = opacity.transform.GetChild(i).GetComponent<InputField>();
                        var swapper = opacity.transform.GetChild(i).gameObject.AddComponent<InputFieldSwapper>();
                        swapper.inputField = inputField;

                        inputField.characterValidation = InputField.CharacterValidation.None;
                        inputField.contentType = InputField.ContentType.Standard;
                        inputField.keyboardType = TouchScreenKeyboardType.Default;
                    }

                    var horizontal = opacity.transform.GetChild(i).GetComponent<HorizontalLayoutGroup>();
                    var input = opacity.transform.GetChild(i).Find("input").AsRT();

                    horizontal.childControlWidth = false;

                    input.sizeDelta = new Vector2(60f, 32f);

                    var layout = opacity.transform.GetChild(i).GetComponent<LayoutElement>();
                    layout.minWidth = 109f;
                }

                EditorThemeManager.AddInputFields(opacity, true, "");
            }

            // Gradient
            {
                try
                {
                    var index = objectView.Find("shape").GetSiblingIndex();
                    objectView.GetChild(index - 1).GetComponentInChildren<Text>().text = "Gradient / Shape";
                    var gradient = objectView.Find("shape").gameObject.Duplicate(objectView, "gradienttype", index);

                    var listToDestroy = new List<GameObject>();
                    for (int i = 1; i < gradient.transform.childCount; i++)
                        listToDestroy.Add(gradient.transform.GetChild(i).gameObject);
                    for (int i = 0; i < listToDestroy.Count; i++)
                        Destroy(listToDestroy[i]);

                    Destroy(gradient.GetComponent<ToggleGroup>());

                    // Normal
                    {
                        var normalToggle = gradient.transform.GetChild(0);
                        var normalToggleImage = normalToggle.Find("Image").GetComponent<Image>();
                        normalToggleImage.sprite = SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_close.png");

                        EditorThemeManager.AddGraphic(normalToggleImage, ThemeGroup.Toggle_1_Check);
                        var tog = normalToggle.GetComponent<Toggle>();
                        tog.group = null;
                        EditorThemeManager.AddToggle(tog, ThemeGroup.Background_1);
                    }

                    // Right
                    {
                        var normalToggle = gradient.transform.GetChild(0).gameObject.Duplicate(gradient.transform, "2");
                        var normalToggleImage = normalToggle.transform.Find("Image").GetComponent<Image>();
                        normalToggleImage.sprite = SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_linear_gradient_right.png");

                        EditorThemeManager.AddGraphic(normalToggleImage, ThemeGroup.Toggle_1_Check);
                        var tog = normalToggle.GetComponent<Toggle>();
                        tog.group = null;
                        EditorThemeManager.AddToggle(tog, ThemeGroup.Background_1);
                    }

                    // Left
                    {
                        var normalToggle = gradient.transform.GetChild(0).gameObject.Duplicate(gradient.transform, "3");
                        var normalToggleImage = normalToggle.transform.Find("Image").GetComponent<Image>();
                        normalToggleImage.sprite = SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_linear_gradient_left.png");

                        EditorThemeManager.AddGraphic(normalToggleImage, ThemeGroup.Toggle_1_Check);
                        var tog = normalToggle.GetComponent<Toggle>();
                        tog.group = null;
                        EditorThemeManager.AddToggle(tog, ThemeGroup.Background_1);
                    }

                    // In
                    {
                        var normalToggle = gradient.transform.GetChild(0).gameObject.Duplicate(gradient.transform, "4");
                        var normalToggleImage = normalToggle.transform.Find("Image").GetComponent<Image>();
                        normalToggleImage.sprite = SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_radial_gradient_in.png");

                        EditorThemeManager.AddGraphic(normalToggleImage, ThemeGroup.Toggle_1_Check);
                        var tog = normalToggle.GetComponent<Toggle>();
                        tog.group = null;
                        EditorThemeManager.AddToggle(tog, ThemeGroup.Background_1);
                    }

                    // Out
                    {
                        var normalToggle = gradient.transform.GetChild(0).gameObject.Duplicate(gradient.transform, "5");
                        var normalToggleImage = normalToggle.transform.Find("Image").GetComponent<Image>();
                        normalToggleImage.sprite = SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_radial_gradient_out.png");

                        EditorThemeManager.AddGraphic(normalToggleImage, ThemeGroup.Toggle_1_Check);
                        var tog = normalToggle.GetComponent<Toggle>();
                        tog.group = null;
                        EditorThemeManager.AddToggle(tog, ThemeGroup.Background_1);
                    }

                    var colorDialog = ObjEditor.inst.KeyframeDialogs[3].transform;

                    var shift = EditorPrefabHolder.Instance.ToggleButton.Duplicate(colorDialog, "shift", 16);
                    var shiftToggleButton = shift.GetComponent<ToggleButtonStorage>();
                    shiftToggleButton.label.text = "Shift Dialog Down";
                    shiftToggleButton.toggle.onValueChanged.ClearAll();
                    shiftToggleButton.toggle.isOn = false;
                    shiftToggleButton.toggle.onValueChanged.AddListener(_val =>
                    {
                        colorShifted = _val;
                        shiftToggleButton.label.text = _val ? "Shift Dialog Up" : "Shift Dialog Down";
                        var animation = new RTAnimation("shift color UI");
                        animation.animationHandlers = new List<AnimationHandlerBase>
                            {
                                new AnimationHandler<float>(new List<IKeyframe<float>>
                                {
                                    new FloatKeyframe(0f, _val ? 0f : 195f, Ease.Linear),
                                    new FloatKeyframe(0.3f, _val ? 195f : 0f, Ease.CircOut),
                                    new FloatKeyframe(0.32f, _val ? 195f : 0f, Ease.Linear),
                                }, x => { if (ObjEditor.inst) ObjEditor.inst.KeyframeDialogs[3].transform.AsRT().anchoredPosition = new Vector2(0f, x); }),
                            };

                        animation.onComplete = () =>
                        {
                            if (ObjEditor.inst)
                                ObjEditor.inst.KeyframeDialogs[3].transform.AsRT().anchoredPosition = new Vector2(0f, _val ? 195f : 0f);
                            AnimationManager.inst.Remove(animation.id);
                        };

                        AnimationManager.inst.Play(animation);
                    });

                    EditorThemeManager.AddSelectable(shiftToggleButton.toggle, ThemeGroup.Function_2);
                    EditorThemeManager.AddGraphic(shiftToggleButton.label, ThemeGroup.Function_2_Text);

                    var endColorLabel = colorDialog.Find("color_label").gameObject.Duplicate(colorDialog, "gradient_color_label");
                    endColorLabel.GetComponentInChildren<Text>().text = "End Color";
                    var endColor = colorDialog.Find("color").gameObject.Duplicate(colorDialog, "gradient_color");

                    var endOpacityLabel = colorDialog.Find("opacity_label").gameObject.Duplicate(colorDialog, "gradient_opacity_label");
                    endOpacityLabel.GetComponentInChildren<Text>().text = "End Opacity";
                    var endOpacity = colorDialog.Find("opacity").gameObject.Duplicate(colorDialog, "gradient_opacity");
                    if (endOpacity.transform.Find("collision"))
                        Destroy(endOpacity.transform.Find("collision").gameObject);

                    var endHSVLabel = colorDialog.Find("huesatval_label").gameObject.Duplicate(colorDialog, "gradient_huesatval_label");
                    endHSVLabel.transform.GetChild(0).GetComponent<Text>().text = "End Hue";
                    endHSVLabel.transform.GetChild(1).GetComponent<Text>().text = "End Sat";
                    endHSVLabel.transform.GetChild(2).GetComponent<Text>().text = "End Val";
                    var endHSV = colorDialog.Find("huesatval").gameObject.Duplicate(colorDialog, "gradient_huesatval");

                    ObjEditor.inst.colorButtons.Clear();
                    for (int i = 1; i <= 18; i++)
                        ObjEditor.inst.colorButtons.Add(colorDialog.Find("color/" + i).GetComponent<Toggle>());

                    gradientColorButtons.Clear();
                    for (int i = 0; i < endColor.transform.childCount; i++)
                        gradientColorButtons.Add(endColor.transform.GetChild(i).GetComponent<Toggle>());
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
            }

            // Position Z
            {
                var positionBase = ObjEditor.inst.KeyframeDialogs[0].transform.Find("position");

                var posZ = positionBase.Find("x").gameObject.Duplicate(positionBase, "z");

                DestroyImmediate(positionBase.GetComponent<HorizontalLayoutGroup>());
                var grp = positionBase.gameObject.AddComponent<GridLayoutGroup>();

                DestroyImmediate(positionBase.Find("x/input").GetComponent<LayoutElement>());
                DestroyImmediate(positionBase.Find("y/input").GetComponent<LayoutElement>());
                DestroyImmediate(positionBase.Find("z/input").GetComponent<LayoutElement>());

                var xLayout = positionBase.Find("x/input").GetComponent<LayoutElement>();
                var yLayout = positionBase.Find("y/input").GetComponent<LayoutElement>();
                var zLayout = positionBase.Find("z/input").GetComponent<LayoutElement>();

                xLayout.preferredWidth = -1;
                yLayout.preferredWidth = -1;
                zLayout.preferredWidth = -1;

                var labels = ObjEditor.inst.KeyframeDialogs[0].transform.GetChild(8);
                var posZLabel = labels.GetChild(1).gameObject.Duplicate(labels, "text");
                posZLabel.GetComponent<Text>().text = "Position Z";

                EditorConfig.AdjustPositionInputsChanged = () =>
                {
                    if (!inst)
                        return;

                    bool adjusted = EditorConfig.Instance.AdjustPositionInputs.Value && RTEditor.ShowModdedUI;
                    positionBase.AsRT().sizeDelta = new Vector2(553f, adjusted ? 32f : 64f);
                    grp.cellSize = new Vector2(adjusted ? 122f : 183f, 40f);

                    var minWidth = adjusted ? 65f : 125.3943f;
                    xLayout.minWidth = minWidth;
                    yLayout.minWidth = minWidth;
                    zLayout.minWidth = minWidth;
                    posZLabel.gameObject.SetActive(adjusted);
                    positionBase.gameObject.SetActive(false);
                    positionBase.gameObject.SetActive(true);

                    posZ.gameObject.SetActive(RTEditor.ShowModdedUI);
                };

                bool adjusted = EditorConfig.Instance.AdjustPositionInputs.Value && RTEditor.ShowModdedUI;
                positionBase.AsRT().sizeDelta = new Vector2(553f, adjusted ? 32f : 64f);
                grp.cellSize = new Vector2(adjusted ? 122f : 183f, 40f);

                var minWidth = adjusted ? 65f : 125.3943f;
                xLayout.minWidth = minWidth;
                yLayout.minWidth = minWidth;
                zLayout.minWidth = minWidth;
                posZLabel.gameObject.SetActive(adjusted);

                posZ.gameObject.SetActive(RTEditor.ShowModdedUI);
            }

            // Layers
            {
                var editor = objectView.Find("editor");
                objectView.GetChild(objectView.Find("spacer") ? 18 : 17).GetChild(1).gameObject.SetActive(true);

                Destroy(editor.Find("layer").gameObject);

                var layers = objectView.Find("time/time").gameObject.Duplicate(editor, "layers", 0);

                var layersIF = layers.GetComponent<InputField>();

                layersIF.characterValidation = InputField.CharacterValidation.Integer;

                var edhlg = objectView.transform.Find("editor").GetComponent<HorizontalLayoutGroup>();
                edhlg.childControlWidth = false;
                edhlg.childForceExpandWidth = false;

                layers.transform.AsRT().sizeDelta = new Vector2(100f, 32f);
                objectView.Find("editor/bin").AsRT().sizeDelta = new Vector2(237f, 32f);

                layers.AddComponent<ContrastColors>().Init(layersIF.textComponent, layersIF.image);

                EditorThemeManager.AddGraphic(layersIF.image, ThemeGroup.Null, true);

                var binSlider = objectView.Find("editor/bin").GetComponent<Slider>();
                var binSliderImage = binSlider.transform.Find("Image").GetComponent<Image>();
                binSlider.colors = UIManager.SetColorBlock(binSlider.colors, Color.white, new Color(0.9f, 0.9f, 0.9f), Color.white, Color.white, Color.white);
                EditorThemeManager.AddGraphic(binSliderImage, ThemeGroup.Slider_2, true);
                EditorThemeManager.AddGraphic(binSlider.image, ThemeGroup.Slider_2_Handle, true);
            }

            // Clear Parent
            {
                var parent = objectView.Find("parent");
                var hlg = parent.GetComponent<HorizontalLayoutGroup>();
                hlg.childControlWidth = false;
                hlg.spacing = 4f;

                parent.transform.Find("text").AsRT().sizeDelta = new Vector2(201f, 32f);

                var resetParent = EditorPrefabHolder.Instance.CloseButton.Duplicate(parent.transform, "clear parent", 1);

                var resetParentButton = resetParent.GetComponent<Button>();

                var parentPicker = EditorPrefabHolder.Instance.CloseButton.Duplicate(parent.transform, "parent picker", 2);

                var parentPickerButton = parentPicker.GetComponent<Button>();

                parentPickerButton.onClick.ClearAll();
                parentPickerButton.onClick.AddListener(() => RTEditor.inst.parentPickerEnabled = true);

                var parentPickerIcon = parentPicker.transform.GetChild(0).GetComponent<Image>();
                parentPickerIcon.sprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_dropper{FileFormat.PNG.Dot()}"));

                var searchParent = parent.transform.Find("parent").GetComponent<Image>();
                EditorThemeManager.AddGraphic(searchParent, ThemeGroup.Function_3, true);
                EditorThemeManager.AddGraphic(searchParent.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Function_3_Text);

                Destroy(resetParent.GetComponent<Animator>());
                resetParentButton.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddSelectable(resetParentButton, ThemeGroup.Close);
                EditorThemeManager.AddGraphic(resetParent.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

                Destroy(parentPicker.GetComponent<Animator>());
                parentPickerButton.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddSelectable(parentPickerButton, ThemeGroup.Picker);
                EditorThemeManager.AddGraphic(parentPickerIcon, ThemeGroup.Picker_Icon);

                parent.transform.Find("parent").AsRT().sizeDelta = new Vector2(32f, 32f);
                parent.transform.Find("more").AsRT().sizeDelta = new Vector2(32f, 32f);
            }

            // ID & LDM
            {
                var id = EditorPrefabHolder.Instance.Labels.Duplicate(objectView, "id", 0);
                EditorHelper.SetComplexity(id, Complexity.Normal);

                id.transform.AsRT().sizeDelta = new Vector2(515, 32f);
                id.transform.GetChild(0).AsRT().sizeDelta = new Vector2(226f, 32f);

                var text = id.transform.GetChild(0).GetComponent<Text>();
                text.fontSize = 18;
                text.text = "ID:";
                text.alignment = TextAnchor.MiddleLeft;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;

                var image = id.AddComponent<Image>();
                EditorThemeManager.AddGraphic(image, ThemeGroup.Background_2, true);

                ldmLabel = text.gameObject.Duplicate(id.transform, "title").GetComponent<Text>();
                ldmLabel.rectTransform.sizeDelta = new Vector2(44f, 32f);
                ldmLabel.text = "LDM";
                ldmLabel.fontStyle = FontStyle.Bold;
                ldmLabel.fontSize = 20;

                var ldm = EditorPrefabHolder.Instance.Toggle.Duplicate(id.transform, "ldm");
                ldmToggle = ldm.GetComponent<Toggle>();

                EditorThemeManager.AddLightText(text);
                EditorThemeManager.AddLightText(ldmLabel);
                EditorThemeManager.AddToggle(ldmToggle);
            }

            // Relative / Copy / Paste
            {
                for (int i = 0; i < 4; i++)
                {
                    var parent = ObjEditor.inst.KeyframeDialogs[i].transform;
                    if (i != 3)
                    {
                        var toggleLabel = EditorPrefabHolder.Instance.Labels.gameObject.Duplicate(parent, "relative-label");
                        var toggleLabelText = toggleLabel.transform.GetChild(0).GetComponent<Text>();
                        toggleLabelText.text = "Value Additive";
                        var toggle = EditorPrefabHolder.Instance.ToggleButton.Duplicate(parent, "relative");
                        var toggleText = toggle.transform.GetChild(1).GetComponent<Text>();
                        toggleText.text = "Relative";

                        EditorThemeManager.AddLightText(toggleLabelText);
                        EditorThemeManager.AddToggle(toggle.GetComponent<Toggle>(), graphic: toggleText);

                        var flipX = EditorPrefabHolder.Instance.Function1Button.Duplicate(parent, "flipx");
                        var flipXText = flipX.transform.GetChild(0).GetComponent<Text>();
                        flipXText.text = "Flip X";
                        ((RectTransform)flipX.transform).sizeDelta = new Vector2(366f, 32f);
                        var flipXButton = flipX.GetComponent<Button>();

                        flipXButton.onClick.ClearAll();
                        flipXButton.onClick.AddListener(() =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.CurrentSelection.InternalTimelineObjects.Where(x => x.Selected))
                            {
                                var eventKeyframe = timelineObject.GetData<EventKeyframe>();
                                eventKeyframe.eventValues[0] = -eventKeyframe.eventValues[0];
                            }

                            var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                            Updater.UpdateObject(beatmapObject, "Keyframes");
                            RenderObjectKeyframesDialog(beatmapObject);
                        });

                        EditorThemeManager.AddGraphic(flipXButton.image, ThemeGroup.Function_1, true);
                        EditorThemeManager.AddGraphic(flipXText, ThemeGroup.Function_1_Text);

                        EditorHelper.SetComplexity(flipX, Complexity.Normal);

                        if (i != 2)
                        {
                            var flipY = EditorPrefabHolder.Instance.Function1Button.Duplicate(parent, "flipy");
                            var flipYText = flipY.transform.GetChild(0).GetComponent<Text>();
                            flipYText.text = "Flip Y";
                            ((RectTransform)flipY.transform).sizeDelta = new Vector2(366f, 32f);
                            var flipYButton = flipY.GetComponent<Button>();

                            flipYButton.onClick.ClearAll();
                            flipYButton.onClick.AddListener(() =>
                            {
                                foreach (var timelineObject in EditorTimeline.inst.CurrentSelection.InternalTimelineObjects.Where(x => x.Selected))
                                {
                                    var eventKeyframe = timelineObject.GetData<EventKeyframe>();
                                    eventKeyframe.eventValues[1] = -eventKeyframe.eventValues[1];
                                }

                                var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                                Updater.UpdateObject(beatmapObject, "Keyframes");
                                RenderObjectKeyframesDialog(beatmapObject);
                            });

                            EditorThemeManager.AddGraphic(flipYButton.image, ThemeGroup.Function_1, true);
                            EditorThemeManager.AddGraphic(flipYText, ThemeGroup.Function_1_Text);

                            EditorHelper.SetComplexity(flipY, Complexity.Normal);
                        }
                    }

                    var edit = parent.Find("edit");
                    EditorHelper.SetComplexity(edit.Find("spacer").gameObject, Complexity.Simple);

                    var copy = EditorPrefabHolder.Instance.Function1Button.Duplicate(edit, "copy", 5);
                    var copyText = copy.transform.GetChild(0).GetComponent<Text>();
                    copyText.text = "Copy";
                    copy.transform.AsRT().sizeDelta = new Vector2(70f, 32f);

                    var paste = EditorPrefabHolder.Instance.Function1Button.Duplicate(edit, "paste", 6);
                    var pasteText = paste.transform.GetChild(0).GetComponent<Text>();
                    pasteText.text = "Paste";
                    paste.transform.AsRT().sizeDelta = new Vector2(70f, 32f);

                    EditorThemeManager.AddGraphic(copy.GetComponent<Image>(), ThemeGroup.Copy, true);
                    EditorThemeManager.AddGraphic(copyText, ThemeGroup.Copy_Text);

                    EditorThemeManager.AddGraphic(paste.GetComponent<Image>(), ThemeGroup.Paste, true);
                    EditorThemeManager.AddGraphic(pasteText, ThemeGroup.Paste_Text);

                    EditorHelper.SetComplexity(copy, Complexity.Normal);
                    EditorHelper.SetComplexity(paste, Complexity.Normal);
                }
            }

            // Homing Buttons
            {
                var position = ObjEditor.inst.KeyframeDialogs[0].transform;
                var randomPosition = position.transform.Find("random");
                randomPosition.Find("interval-input/x").gameObject.SetActive(false);
                var homingStaticPosition = randomPosition.Find("none").gameObject.Duplicate(randomPosition, "homing-static", 4);

                if (RTFile.FileExists(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "editor_gui__s_homing.png"))
                    homingStaticPosition.transform.Find("Image").GetComponent<Image>().sprite = SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "editor_gui__s_homing.png");

                var homingDynamicPosition = randomPosition.Find("none").gameObject.Duplicate(randomPosition, "homing-dynamic", 5);

                if (RTFile.FileExists(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "editor_gui_d_homing.png"))
                    homingDynamicPosition.transform.Find("Image").GetComponent<Image>().sprite = SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "editor_gui_d_homing.png");

                var rotation = ObjEditor.inst.KeyframeDialogs[2].transform;
                var randomRotation = rotation.Find("random");
                randomRotation.Find("interval-input/x").gameObject.SetActive(false);
                var homingStaticRotation = randomRotation.Find("none").gameObject.Duplicate(randomRotation, "homing-static", 3);

                if (RTFile.FileExists(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "editor_gui__s_homing.png"))
                    homingStaticRotation.transform.Find("Image").GetComponent<Image>().sprite = SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "editor_gui__s_homing.png");

                var homingDynamicRotation = randomRotation.Find("none").gameObject.Duplicate(randomRotation, "homing-dynamic", 4);

                if (RTFile.FileExists(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "editor_gui_d_homing.png"))
                    homingDynamicRotation.transform.Find("Image").GetComponent<Image>().sprite = SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "editor_gui_d_homing.png");

                var rRotation = rotation.Find("r_rotation");
                var rRotationX = rRotation.Find("x");

                var rRotationY = rRotationX.gameObject.Duplicate(rRotation, "y");

                var rRotationLabel = rotation.Find("r_rotation_label");
                var l = rRotationLabel.GetChild(0);
                var max = l.gameObject.Duplicate(rRotationLabel, "text");

                Destroy(rRotation.GetComponent<EventTrigger>());

                var rAxis = EditorPrefabHolder.Instance.Dropdown.Duplicate(position, "r_axis", 14);
                var rAxisDD = rAxis.GetComponent<Dropdown>();
                rAxisDD.options = CoreHelper.StringToOptionData("Both", "X Only", "Y Only");

                EditorThemeManager.AddDropdown(rAxisDD);
            }

            // Object Tags
            {
                var label = EditorPrefabHolder.Instance.Labels.Duplicate(objectView, "tags_label");
                var index = objectView.Find("name").GetSiblingIndex() + 1;
                label.transform.SetSiblingIndex(index);

                label.transform.GetChild(0).GetComponent<Text>().text = "Tags";

                // Tags Scroll View/Viewport/Content
                var tagScrollView = Creator.NewUIObject("Tags Scroll View", objectView, index + 1);

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

            // Render Type
            {
                var label = EditorPrefabHolder.Instance.Labels.Duplicate(objectView, "rendertype_label");
                var index = objectView.Find("depth").GetSiblingIndex() + 1;
                label.transform.SetSiblingIndex(index);

                var labelText = label.transform.GetChild(0).GetComponent<Text>();
                labelText.text = "Render Type";
                EditorThemeManager.AddLightText(labelText);

                var renderType = EditorPrefabHolder.Instance.Dropdown.Duplicate(objectView, "rendertype", index + 1);
                var renderTypeDD = renderType.GetComponent<Dropdown>();
                renderTypeDD.options = CoreHelper.StringToOptionData("Foreground", "Background");

                EditorThemeManager.AddDropdown(renderTypeDD);
            }

            DestroyImmediate(ObjEditor.inst.KeyframeDialogs[2].transform.GetChild(1).gameObject);
            DestroyImmediate(ObjEditor.inst.KeyframeDialogs[3].transform.GetChild(1).gameObject);

            var multiKF = ObjEditor.inst.KeyframeDialogs[4];
            multiKF.transform.AsRT().anchorMax = new Vector2(0f, 1f);
            multiKF.transform.AsRT().anchorMin = new Vector2(0f, 1f);

            // Shift Dialogs
            {
                try
                {
                    ObjEditor.inst.KeyframeDialogs[0].transform.GetChild(2).gameObject.SetActive(false);
                    ObjEditor.inst.KeyframeDialogs[0].transform.GetChild(7).gameObject.SetActive(false);
                    ObjEditor.inst.KeyframeDialogs[1].transform.GetChild(2).gameObject.SetActive(false);
                    ObjEditor.inst.KeyframeDialogs[1].transform.GetChild(7).gameObject.SetActive(false);
                    ObjEditor.inst.KeyframeDialogs[2].transform.GetChild(2).gameObject.SetActive(false);
                    ObjEditor.inst.KeyframeDialogs[2].transform.GetChild(7).gameObject.SetActive(false);
                    ObjEditor.inst.KeyframeDialogs[3].transform.GetChild(2).gameObject.SetActive(false);
                    ObjEditor.inst.KeyframeDialogs[3].transform.GetChild(7).gameObject.SetActive(false);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }

                DestroyImmediate(right.GetComponent<VerticalLayoutGroup>());
            }

            // Parent Settings
            {
                var array = new string[] { "pos", "sca", "rot" };
                for (int i = 0; i < 3; i++)
                {
                    var parent = objectView.Find("parent_more").GetChild(i + 1);

                    if (parent.Find("<<"))
                        DestroyImmediate(parent.Find("<<").gameObject);

                    if (parent.Find("<"))
                        DestroyImmediate(parent.Find("<").gameObject);

                    if (parent.Find(">"))
                        DestroyImmediate(parent.Find(">").gameObject);

                    if (parent.Find(">>"))
                        DestroyImmediate(parent.Find(">>").gameObject);

                    var additive = parent.GetChild(2).gameObject.Duplicate(parent, $"{array[i]}_add");
                    var parallax = parent.GetChild(3).gameObject.Duplicate(parent, $"{array[i]}_parallax");

                    if (parent.Find("text"))
                    {
                        var text = parent.Find("text").GetComponent<Text>();
                        text.fontSize = 19;
                        EditorThemeManager.AddLightText(text);
                    }

                    var type = parent.GetChild(2).gameObject;
                    var offset = parent.GetChild(3).gameObject;

                    EditorThemeManager.AddToggle(type.GetComponent<Toggle>(), ThemeGroup.Background_1);
                    EditorThemeManager.AddGraphic(type.transform.Find("Image").GetComponent<Image>(), ThemeGroup.Toggle_1_Check);
                    EditorThemeManager.AddInputField(offset.GetComponent<InputField>());
                    EditorThemeManager.AddToggle(additive.GetComponent<Toggle>(), ThemeGroup.Background_1);
                    var additiveImage = additive.transform.Find("Image").GetComponent<Image>();
                    EditorThemeManager.AddGraphic(additiveImage, ThemeGroup.Toggle_1_Check);
                    EditorThemeManager.AddInputField(parallax.GetComponent<InputField>());

                    var path = $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_{array[i]}_addtoggle.png";
                    if (RTFile.FileExists(path))
                        additiveImage.sprite = SpriteHelper.LoadSprite(path);
                }
            }

            // Make Shape list scrollable, for any more shapes I decide to add.
            {
                var shape = objectView.Find("shape");
                var scroll = shape.gameObject.AddComponent<ScrollRect>();
                shape.gameObject.AddComponent<Mask>();
                var image = shape.gameObject.AddComponent<Image>();

                scroll.horizontal = true;
                scroll.vertical = false;
                scroll.content = shape.AsRT();
                scroll.viewport = shape.AsRT();
                image.color = new Color(1f, 1f, 1f, 0.01f);
            }

            // Timeline Object adjustments
            {
                var gameObject = ObjEditor.inst.timelineObjectPrefab.Duplicate(transform, ObjEditor.inst.timelineObjectPrefab.name);
                var icons = gameObject.transform.Find("icons");

                if (!icons.gameObject.GetComponent<HorizontalLayoutGroup>())
                {
                    var timelineObjectStorage = gameObject.AddComponent<TimelineObjectStorage>();

                    var @lock = ObjEditor.inst.timelineObjectPrefabLock.Duplicate(icons);
                    @lock.name = "lock";
                    @lock.transform.AsRT().anchoredPosition = Vector3.zero;

                    var dots = ObjEditor.inst.timelineObjectPrefabDots.Duplicate(icons);
                    dots.name = "dots";
                    dots.transform.AsRT().anchoredPosition = Vector3.zero;

                    var hlg = icons.gameObject.AddComponent<HorizontalLayoutGroup>();
                    hlg.childControlWidth = false;
                    hlg.childForceExpandWidth = false;
                    hlg.spacing = -4f;
                    hlg.childAlignment = TextAnchor.UpperRight;

                    @lock.transform.AsRT().sizeDelta = new Vector2(20f, 20f);
                    dots.transform.AsRT().sizeDelta = new Vector2(32f, 20f);

                    var b = Creator.NewUIObject("type", icons);
                    b.transform.AsRT().sizeDelta = new Vector2(20f, 20f);

                    var bImage = b.AddComponent<Image>();
                    bImage.color = new Color(0f, 0f, 0f, 0.45f);

                    var icon = Creator.NewUIObject("type", b.transform);
                    icon.transform.AsRT().anchoredPosition = Vector2.zero;
                    icon.transform.AsRT().sizeDelta = new Vector2(20f, 20f);

                    icon.AddComponent<Image>();

                    var hoverUI = gameObject.AddComponent<HoverUI>();
                    hoverUI.animatePos = false;
                    hoverUI.animateSca = true;

                    timelineObjectStorage.hoverUI = hoverUI;
                    timelineObjectStorage.image = gameObject.GetComponent<Image>();
                    timelineObjectStorage.eventTrigger = gameObject.GetComponent<EventTrigger>() ?? gameObject.AddComponent<EventTrigger>();
                    timelineObjectStorage.text = gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                }

                ObjEditor.inst.timelineObjectPrefab = gameObject;

                var gameObject2 = ObjEditor.inst.objTimelinePrefab.Duplicate(ObjEditor.inst.transform, ObjEditor.inst.objTimelinePrefab.name);

                var lockedKeyframe = ObjEditor.inst.timelineObjectPrefabLock.Duplicate(gameObject2.transform, "lock");
                lockedKeyframe.transform.AsRT().anchoredPosition = new Vector2(6f, 0f);
                lockedKeyframe.transform.AsRT().sizeDelta = new Vector2(15f, 15f);

                ObjEditor.inst.objTimelinePrefab = gameObject2;
            }

            // Store Image Shape
            {
                if (objectView.Find("shapesettings/7"))
                {
                    var select = objectView.Find("shapesettings/7/select").GetComponent<Button>();
                    Destroy(select.GetComponent<Animator>());
                    select.transition = Selectable.Transition.ColorTint;
                    EditorThemeManager.AddSelectable(select, ThemeGroup.Function_2, false);

                    EditorThemeManager.AddLightText(objectView.Find("shapesettings/7/text").GetComponent<Text>());

                    var setData = EditorPrefabHolder.Instance.Function1Button.Duplicate(objectView.Find("shapesettings/7"), "set", 5);
                    var setDataText = setData.transform.GetChild(0).GetComponent<Text>();
                    setDataText.text = "Set Data";
                    ((RectTransform)setData.transform).sizeDelta = new Vector2(70f, 32f);

                    setData.GetComponent<LayoutElement>().minWidth = 130f;

                    EditorThemeManager.AddGraphic(setData.GetComponent<Image>(), ThemeGroup.Function_1, true);
                    EditorThemeManager.AddGraphic(setDataText, ThemeGroup.Function_1_Text);
                }
            }

            // Parent Desync
            {
                var parentMore = objectView.Find("parent_more");
                var parentDesync = EditorPrefabHolder.Instance.ToggleButton.Duplicate(parentMore, "spawn_once", 1);
                var parentDesyncButtonToggle = parentDesync.GetComponent<ToggleButtonStorage>();
                parentDesyncButtonToggle.label.text = "Parent Desync";

                EditorThemeManager.AddToggle(parentDesyncButtonToggle.toggle, graphic: parentDesyncButtonToggle.label);
                parentMore.AsRT().sizeDelta = new Vector2(351f, 152f);
            }

            // Assign Prefab
            {
                var collapseLabel = ObjEditor.inst.ObjectView.transform.Find("collapselabel");
                var applyPrefab = ObjEditor.inst.ObjectView.transform.Find("applyprefab");
                var siblingIndex = applyPrefab.GetSiblingIndex();
                var applyPrefabText = applyPrefab.transform.GetChild(0).GetComponent<Text>();

                var applyPrefabButton = applyPrefab.GetComponent<Button>();
                Destroy(applyPrefab.GetComponent<Animator>());
                applyPrefabButton.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddSelectable(applyPrefabButton, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(applyPrefabText, ThemeGroup.Function_2_Text);

                var assignPrefabLabel = collapseLabel.gameObject.Duplicate(ObjEditor.inst.ObjectView.transform, "assignlabel", siblingIndex + 1);
                var assignPrefabLabelText = assignPrefabLabel.transform.GetChild(0).GetComponent<Text>();
                assignPrefabLabelText.text = "Assign Object to a Prefab";
                EditorThemeManager.AddLightText(assignPrefabLabelText);
                var assignPrefab = applyPrefab.gameObject.Duplicate(ObjEditor.inst.ObjectView.transform, "assign prefab", siblingIndex + 2);
                var assignPrefabText = assignPrefab.transform.GetChild(0).GetComponent<Text>();
                assignPrefabText.text = "Assign";
                var assignPrefabButton = assignPrefab.GetComponent<Button>();
                Destroy(assignPrefab.GetComponent<Animator>());
                assignPrefabButton.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddSelectable(assignPrefabButton, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(assignPrefabText, ThemeGroup.Function_2_Text);

                assignPrefabButton.onClick.ClearAll();
                assignPrefabButton.onClick.AddListener(() =>
                {
                    RTEditor.inst.selectingMultiple = false;
                    RTEditor.inst.prefabPickerEnabled = true;
                });

                var removePrefab = applyPrefab.gameObject.Duplicate(ObjEditor.inst.ObjectView.transform, "remove prefab", siblingIndex + 3);
                var removePrefabText = removePrefab.transform.GetChild(0).GetComponent<Text>();
                removePrefabText.text = "Remove";
                var removePrefabButton = removePrefab.GetComponent<Button>();
                Destroy(removePrefab.GetComponent<Animator>());
                removePrefabButton.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddSelectable(removePrefabButton, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(removePrefabText, ThemeGroup.Function_2_Text);

                removePrefabButton.onClick.ClearAll();
                removePrefabButton.onClick.AddListener(() =>
                {
                    var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                    beatmapObject.RemovePrefabReference();
                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.CurrentSelection);
                    OpenDialog(beatmapObject);
                });

                EditorHelper.SetComplexity(assignPrefabLabel, Complexity.Normal);
                EditorHelper.SetComplexity(assignPrefab, Complexity.Normal);
                EditorHelper.SetComplexity(removePrefab, Complexity.Normal);
            }

            // Markers
            {
                var markers = Creator.NewUIObject("Markers", ObjEditor.inst.objTimelineSlider.transform);
                RectValues.FullAnchored.AssignToRectTransform(markers.transform.AsRT());
            }

            // Editor Themes
            {
                EditorThemeManager.AddGraphic(dialog.GetComponent<Image>(), ThemeGroup.Background_1);
                EditorThemeManager.AddGraphic(right.GetComponent<Image>(), ThemeGroup.Background_3);
                EditorThemeManager.AddInputField(objectView.Find("name/name").GetComponent<InputField>());
                EditorThemeManager.AddDropdown(objectView.Find("name/object-type").GetComponent<Dropdown>());
                EditorThemeManager.AddDropdown(todDropdown.GetComponent<Dropdown>());

                var autokill = objectView.Find("autokill");
                EditorThemeManager.AddInputField(autokill.Find("tod-value").GetComponent<InputField>());

                var setAutokillButton = autokill.Find("|").GetComponent<Button>();
                Destroy(setAutokillButton.GetComponent<Animator>());
                setAutokillButton.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddSelectable(setAutokillButton, ThemeGroup.Function_2, false);

                var collapse = autokill.Find("collapse").GetComponent<Toggle>();

                EditorThemeManager.AddToggle(collapse, ThemeGroup.Background_1);

                for (int i = 0; i < collapse.transform.Find("dots").childCount; i++)
                    EditorThemeManager.AddGraphic(collapse.transform.Find("dots").GetChild(i).GetComponent<Image>(), ThemeGroup.Dark_Text);

                var parentButton = objectView.Find("parent/text").GetComponent<Button>();
                EditorThemeManager.AddSelectable(parentButton, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(parentButton.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Function_2_Text);

                var moreButton = objectView.Find("parent/more").GetComponent<Button>();
                Destroy(moreButton.GetComponent<Animator>());
                moreButton.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddSelectable(moreButton, ThemeGroup.Function_2, false);

                EditorThemeManager.AddInputField(objectView.transform.Find("shapesettings/5").GetComponent<InputField>());

                try
                {
                    for (int i = 0; i < ObjEditor.inst.KeyframeDialogs.Count - 1; i++)
                    {
                        var kfdialog = ObjEditor.inst.KeyframeDialogs[i].transform;

                        var topPanel = kfdialog.GetChild(0);
                        var bg = topPanel.GetChild(0).GetComponent<Image>();
                        var title = topPanel.GetChild(1).GetComponent<Text>();
                        bg.gameObject.AddComponent<ContrastColors>().Init(title, bg);

                        EditorThemeManager.AddGraphic(bg, EditorThemeManager.EditorTheme.GetGroup($"Object Keyframe Color {i + 1}"));

                        var edit = kfdialog.Find("edit");
                        for (int j = 0; j < edit.childCount; j++)
                        {
                            var button = edit.GetChild(j);
                            if (button.name == "copy" || button.name == "paste")
                                continue;

                            var buttonComponent = button.GetComponent<Button>();

                            if (!buttonComponent)
                                continue;

                            if (button.name == "del")
                            {
                                EditorThemeManager.AddGraphic(button.GetChild(0).GetComponent<Image>(), ThemeGroup.Delete_Keyframe_BG);
                                EditorThemeManager.AddSelectable(buttonComponent, ThemeGroup.Delete_Keyframe_Button, false);

                                continue;
                            }

                            Destroy(button.GetComponent<Animator>());
                            buttonComponent.transition = Selectable.Transition.ColorTint;

                            EditorThemeManager.AddSelectable(buttonComponent, ThemeGroup.Function_2, false);
                        }

                        // Labels
                        for (int j = 0; j < kfdialog.childCount; j++)
                        {
                            var label = kfdialog.GetChild(j);
                            if (label.name == "label" || label.name == "curves_label" || label.name == "r_label" ||
                                label.name == "r_position_label" || label.name == "r_scale_label" || label.name == "r_rotation_label")
                            {
                                for (int k = 0; k < label.childCount; k++)
                                    EditorThemeManager.AddLightText(label.GetChild(k).GetComponent<Text>());
                            }
                        }

                        var timeBase = kfdialog.Find("time");
                        var timeInput = timeBase.Find("time").GetComponent<InputField>();

                        EditorThemeManager.AddInputField(timeInput, ThemeGroup.Input_Field);

                        for (int j = 1; j < timeBase.childCount; j++)
                        {
                            var button = timeBase.GetChild(j);
                            var buttonComponent = button.GetComponent<Button>();

                            if (!buttonComponent)
                                continue;

                            Destroy(button.GetComponent<Animator>());
                            buttonComponent.transition = Selectable.Transition.ColorTint;

                            EditorThemeManager.AddSelectable(buttonComponent, ThemeGroup.Function_2, false);
                        }

                        EditorThemeManager.AddDropdown(kfdialog.Find("curves").GetComponent<Dropdown>());

                        if (i < 3)
                        {
                            var find = i switch
                            {
                                0 => "position",
                                1 => "scale",
                                _ => "rotation",
                            };
                            EditorThemeManager.AddInputFields(kfdialog.Find(find).gameObject, true, "");
                            EditorThemeManager.AddInputFields(kfdialog.Find($"r_{find}").gameObject, true, "");
                        }

                        if (kfdialog.Find("random"))
                        {
                            for (int j = 0; j < kfdialog.Find("random").childCount; j++)
                            {
                                var toggle = kfdialog.Find("random").GetChild(j).GetComponent<Toggle>();
                                if (!toggle)
                                    continue;

                                EditorThemeManager.AddToggle(toggle, ThemeGroup.Background_3);
                                EditorThemeManager.AddGraphic(toggle.transform.Find("Image").GetComponent<Image>(), ThemeGroup.Toggle_1_Check);
                            }

                            EditorThemeManager.AddInputField(kfdialog.Find("random/interval-input").GetComponent<InputField>());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"\nException: {ex}");
                }

                var timeline = ObjEditor.inst.objTimelineContent.parent.parent;

                EditorThemeManager.AddScrollbar(timeline.Find("Scrollbar Horizontal").GetComponent<Scrollbar>(),
                    scrollbarGroup: ThemeGroup.Timeline_Scrollbar_Base, handleGroup: ThemeGroup.Timeline_Scrollbar, canSetScrollbarRounded: false);
                EditorThemeManager.AddGraphic(ObjEditor.inst.objTimelineSlider.transform.Find("Background").GetComponent<Image>(), ThemeGroup.Timeline_Time_Scrollbar);
                EditorThemeManager.AddGraphic(timeline.GetComponent<Image>(), ThemeGroup.Background_1);
                EditorThemeManager.AddScrollbar(dialog.Find("data/left/Scroll View/Scrollbar Vertical").GetComponent<Scrollbar>());

                var zoomSliderBase = ObjEditor.inst.zoomSlider.transform.parent;

                var gameObject = Creator.NewUIObject("zoom back", zoomSliderBase.parent, 1);

                var image = gameObject.AddComponent<Image>();
                RectValues.BottomLeftAnchored.SizeDelta(128f, 25f).AssignToRectTransform(image.rectTransform);
                EditorThemeManager.AddGraphic(image, ThemeGroup.Timeline_Scrollbar_Base);
                EditorThemeManager.AddGraphic(zoomSliderBase.GetComponent<Image>(), ThemeGroup.Background_1, true);
                EditorThemeManager.AddGraphic(zoomSliderBase.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Slider_2);
                EditorThemeManager.AddGraphic(zoomSliderBase.transform.GetChild(2).GetComponent<Image>(), ThemeGroup.Slider_2);
                EditorThemeManager.AddGraphic(ObjEditor.inst.zoomSlider.transform.Find("Background").GetComponent<Image>(), ThemeGroup.Slider_2, true);
                EditorThemeManager.AddGraphic(ObjEditor.inst.zoomSlider.transform.Find("Fill Area/Fill").GetComponent<Image>(), ThemeGroup.Slider_2, true);
                EditorThemeManager.AddGraphic(ObjEditor.inst.zoomSlider.image, ThemeGroup.Slider_2_Handle, true);
            }

            ObjEditor.inst.SelectedColor = EditorConfig.Instance.ObjectSelectionColor.Value;
            ObjEditor.inst.ObjectLengthOffset = EditorConfig.Instance.KeyframeEndLengthOffset.Value;

            // Multi Keyframe Editor
            {
                var multiKeyframeEditor = multiKF.transform;

                multiKeyframeEditor.GetChild(1).gameObject.SetActive(false);

                RTEditor.GenerateLabels("time_label", multiKeyframeEditor, new LabelSettings("Time"));

                var timeBase = Creator.NewUIObject("time", multiKeyframeEditor);
                timeBase.transform.AsRT().sizeDelta = new Vector2(765f, 38f);

                var time = EditorPrefabHolder.Instance.NumberInputField.Duplicate(timeBase.transform, "time");
                new RectValues(Vector2.zero, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(300f, 32f)).AssignToRectTransform(time.transform.AsRT());
                time.GetComponent<HorizontalLayoutGroup>().spacing = 5f;

                var timeStorage = time.GetComponent<InputFieldStorage>();
                timeStorage.inputField.gameObject.name = "time";

                EditorThemeManager.AddInputField(timeStorage.inputField);
                EditorThemeManager.AddSelectable(timeStorage.leftGreaterButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(timeStorage.leftButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(timeStorage.middleButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(timeStorage.rightButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(timeStorage.rightGreaterButton, ThemeGroup.Function_2, false);

                RTEditor.GenerateLabels("curve_label", multiKeyframeEditor, new LabelSettings("Ease Type"));

                var curveBase = Creator.NewUIObject("curves", multiKeyframeEditor);
                curveBase.transform.AsRT().sizeDelta = new Vector2(765f, 38f);
                var curveBaseLayout = curveBase.AddComponent<HorizontalLayoutGroup>();
                curveBaseLayout.childControlWidth = false;
                curveBaseLayout.childForceExpandWidth = false;
                curveBaseLayout.spacing = 4f;

                var curves = EditorPrefabHolder.Instance.CurvesDropdown.Duplicate(curveBase.transform, "curves");
                curves.transform.AsRT().sizeDelta = new Vector2(230f, 38f);
                var curvesDropdown = curves.GetComponent<Dropdown>();
                curvesDropdown.options = EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList();
                EditorThemeManager.AddDropdown(curvesDropdown);

                var curvesApply = EditorPrefabHolder.Instance.Function1Button.Duplicate(curveBase.transform, "apply");
                curvesApply.transform.AsRT().sizeDelta = new Vector2(132f, 38f);
                var curvesApplyFunctionButton = curvesApply.GetComponent<FunctionButtonStorage>();
                curvesApplyFunctionButton.text.text = "Apply Curves";
                EditorThemeManager.AddGraphic(curvesApplyFunctionButton.button.image, ThemeGroup.Function_1, true);
                EditorThemeManager.AddGraphic(curvesApplyFunctionButton.text, ThemeGroup.Function_1_Text);

                RTEditor.GenerateLabels("value index_label", multiKeyframeEditor, new LabelSettings("Value Index / Value"));

                var valueBase = Creator.NewUIObject("value base", multiKeyframeEditor);
                valueBase.transform.AsRT().sizeDelta = new Vector2(364f, 32f);

                var valueBaseHLG = valueBase.AddComponent<HorizontalLayoutGroup>();
                valueBaseHLG.childControlHeight = false;
                valueBaseHLG.childControlWidth = false;
                valueBaseHLG.childForceExpandHeight = false;
                valueBaseHLG.childForceExpandWidth = false;

                var valueIndex = EditorPrefabHolder.Instance.NumberInputField.Duplicate(valueBase.transform, "value index");
                valueIndex.transform.Find("input").AsRT().sizeDelta = new Vector2(60f, 32f);
                valueIndex.transform.AsRT().sizeDelta = new Vector2(130f, 32f);

                var valueIndexStorage = valueIndex.GetComponent<InputFieldStorage>();
                Destroy(valueIndexStorage.leftGreaterButton.gameObject);
                Destroy(valueIndexStorage.middleButton.gameObject);
                Destroy(valueIndexStorage.rightGreaterButton.gameObject);
                EditorThemeManager.AddInputField(valueIndexStorage.inputField);
                EditorThemeManager.AddSelectable(valueIndexStorage.leftButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(valueIndexStorage.rightButton, ThemeGroup.Function_2, false);

                var value = EditorPrefabHolder.Instance.NumberInputField.Duplicate(valueBase.transform, "value");
                value.transform.Find("input").AsRT().sizeDelta = new Vector2(128f, 32f);
                value.transform.AsRT().sizeDelta = new Vector2(200f, 32f);

                var valueStorage = value.GetComponent<InputFieldStorage>();
                Destroy(valueStorage.leftGreaterButton.gameObject);
                Destroy(valueStorage.rightGreaterButton.gameObject);
                EditorThemeManager.AddInputField(valueStorage.inputField);
                EditorThemeManager.AddSelectable(valueStorage.leftButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(valueStorage.middleButton, ThemeGroup.Function_2, false);
                EditorThemeManager.AddSelectable(valueStorage.rightButton, ThemeGroup.Function_2, false);

                RTEditor.GenerateLabels("snap_label", multiKeyframeEditor, new LabelSettings("Force Snap Time to BPM"));

                var snapToBPMObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(multiKeyframeEditor, "snap bpm");
                snapToBPMObject.transform.localScale = Vector3.one;

                ((RectTransform)snapToBPMObject.transform).sizeDelta = new Vector2(368f, 32f);

                var snapToBPMText = snapToBPMObject.transform.GetChild(0).GetComponent<Text>();
                snapToBPMText.text = "Snap";

                var snapToBPM = snapToBPMObject.GetComponent<Button>();
                snapToBPM.onClick.ClearAll();
                snapToBPM.onClick.AddListener(() =>
                {
                    var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                    foreach (var timelineObject in EditorTimeline.inst.CurrentSelection.InternalTimelineObjects.Where(x => x.Selected))
                    {
                        if (timelineObject.Index != 0)
                            timelineObject.Time = RTEditor.SnapToBPM(timelineObject.Time);

                        float st = beatmapObject.StartTime;

                        st = -(st - RTEditor.SnapToBPM(st + timelineObject.Time));

                        float timePosition = ObjectEditor.TimeTimelineCalc(st);

                        ((RectTransform)timelineObject.GameObject.transform).anchoredPosition = new Vector2(timePosition, 0f);

                        Updater.UpdateObject(beatmapObject, "Keyframes");

                        RenderKeyframe(beatmapObject, timelineObject);
                    }
                });

                EditorThemeManager.AddGraphic(snapToBPM.image, ThemeGroup.Function_1, true);
                EditorThemeManager.AddGraphic(snapToBPMText, ThemeGroup.Function_1_Text);

                RTEditor.GenerateLabels("paste_label", multiKeyframeEditor, new LabelSettings("All Types"));

                var pasteAllObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(multiKeyframeEditor, "paste");
                pasteAllObject.transform.localScale = Vector3.one;

                ((RectTransform)pasteAllObject.transform).sizeDelta = new Vector2(368f, 32f);

                var pasteAllText = pasteAllObject.transform.GetChild(0).GetComponent<Text>();
                pasteAllText.text = "Paste";

                var pasteAll = pasteAllObject.GetComponent<Button>();
                pasteAll.onClick.ClearAll();
                pasteAll.onClick.AddListener(() =>
                {
                    var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                    var list = EditorTimeline.inst.CurrentSelection.InternalTimelineObjects.Where(x => x.Selected);

                    foreach (var timelineObject in list)
                    {
                        var kf = timelineObject.GetData<EventKeyframe>();
                        switch (timelineObject.Type)
                        {
                            case 0:
                                if (CopiedPositionData != null)
                                {
                                    kf.curveType = CopiedPositionData.curveType;
                                    kf.eventValues = CopiedPositionData.eventValues.Copy();
                                    kf.eventRandomValues = CopiedPositionData.eventRandomValues.Copy();
                                    kf.random = CopiedPositionData.random;
                                    kf.relative = CopiedPositionData.relative;
                                }
                                break;
                            case 1:
                                if (CopiedScaleData != null)
                                {
                                    kf.curveType = CopiedScaleData.curveType;
                                    kf.eventValues = CopiedScaleData.eventValues.Copy();
                                    kf.eventRandomValues = CopiedScaleData.eventRandomValues.Copy();
                                    kf.random = CopiedScaleData.random;
                                    kf.relative = CopiedScaleData.relative;
                                }
                                break;
                            case 2:
                                if (CopiedRotationData != null)
                                {
                                    kf.curveType = CopiedRotationData.curveType;
                                    kf.eventValues = CopiedRotationData.eventValues.Copy();
                                    kf.eventRandomValues = CopiedRotationData.eventRandomValues.Copy();
                                    kf.random = CopiedRotationData.random;
                                    kf.relative = CopiedRotationData.relative;
                                }
                                break;
                            case 3:
                                if (CopiedColorData != null)
                                {
                                    kf.curveType = CopiedColorData.curveType;
                                    kf.eventValues = CopiedColorData.eventValues.Copy();
                                    kf.eventRandomValues = CopiedColorData.eventRandomValues.Copy();
                                    kf.random = CopiedColorData.random;
                                    kf.relative = CopiedColorData.relative;
                                }
                                break;
                        }
                    }

                    RenderKeyframes(beatmapObject);
                    RenderObjectKeyframesDialog(beatmapObject);
                    Updater.UpdateObject(beatmapObject, "Keyframes");
                    EditorManager.inst.DisplayNotification("Pasted keyframe data to selected keyframes!", 2f, EditorManager.NotificationType.Success);
                });

                EditorThemeManager.AddGraphic(pasteAll.image, ThemeGroup.Paste, true);
                EditorThemeManager.AddGraphic(pasteAllText, ThemeGroup.Paste_Text);

                RTEditor.GenerateLabels("paste_label", multiKeyframeEditor, new LabelSettings("Position / Scale"));

                var pastePosScaObject = new GameObject("paste pos sca base");
                pastePosScaObject.transform.SetParent(multiKeyframeEditor);
                pastePosScaObject.transform.localScale = Vector3.one;

                var pastePosScaRT = pastePosScaObject.AddComponent<RectTransform>();
                pastePosScaRT.sizeDelta = new Vector2(364f, 32f);

                var pastePosScaHLG = pastePosScaObject.AddComponent<HorizontalLayoutGroup>();
                pastePosScaHLG.childControlHeight = false;
                pastePosScaHLG.childControlWidth = false;
                pastePosScaHLG.childForceExpandHeight = false;
                pastePosScaHLG.childForceExpandWidth = false;
                pastePosScaHLG.spacing = 8f;

                var pastePosObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(pastePosScaRT, "paste");
                pastePosObject.transform.localScale = Vector3.one;

                ((RectTransform)pastePosObject.transform).sizeDelta = new Vector2(180f, 32f);

                var pastePosText = pastePosObject.transform.GetChild(0).GetComponent<Text>();
                pastePosText.text = "Paste Pos";

                var pastePos = pastePosObject.GetComponent<Button>();
                pastePos.onClick.ClearAll();
                pastePos.onClick.AddListener(() =>
                {
                    var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                    var list = EditorTimeline.inst.CurrentSelection.InternalTimelineObjects.Where(x => x.Selected);

                    foreach (var timelineObject in list)
                    {
                        if (timelineObject.Type != 0)
                            continue;

                        var kf = timelineObject.GetData<EventKeyframe>();

                        if (CopiedPositionData != null)
                        {
                            kf.curveType = CopiedPositionData.curveType;
                            kf.eventValues = CopiedPositionData.eventValues.Copy();
                            kf.eventRandomValues = CopiedPositionData.eventRandomValues.Copy();
                            kf.random = CopiedPositionData.random;
                            kf.relative = CopiedPositionData.relative;
                        }
                    }

                    RenderKeyframes(beatmapObject);
                    RenderObjectKeyframesDialog(beatmapObject);
                    Updater.UpdateObject(beatmapObject, "Keyframes");
                    EditorManager.inst.DisplayNotification("Pasted position keyframe data to selected position keyframes!", 3f, EditorManager.NotificationType.Success);
                });

                EditorThemeManager.AddGraphic(pastePos.image, ThemeGroup.Paste, true);
                EditorThemeManager.AddGraphic(pastePosText, ThemeGroup.Paste_Text);

                var pasteScaObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(pastePosScaRT, "paste");
                pasteScaObject.transform.localScale = Vector3.one;

                ((RectTransform)pasteScaObject.transform).sizeDelta = new Vector2(180f, 32f);

                var pasteScaText = pasteScaObject.transform.GetChild(0).GetComponent<Text>();
                pasteScaText.text = "Paste Scale";

                var pasteSca = pasteScaObject.GetComponent<Button>();
                pasteSca.onClick.ClearAll();
                pasteSca.onClick.AddListener(() =>
                {
                    var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                    var list = EditorTimeline.inst.CurrentSelection.InternalTimelineObjects.Where(x => x.Selected);

                    foreach (var timelineObject in list)
                    {
                        if (timelineObject.Type != 1)
                            continue;

                        var kf = timelineObject.GetData<EventKeyframe>();

                        if (CopiedScaleData != null)
                        {
                            kf.curveType = CopiedScaleData.curveType;
                            kf.eventValues = CopiedScaleData.eventValues.Copy();
                            kf.eventRandomValues = CopiedScaleData.eventRandomValues.Copy();
                            kf.random = CopiedScaleData.random;
                            kf.relative = CopiedScaleData.relative;
                        }
                    }

                    RenderKeyframes(beatmapObject);
                    RenderObjectKeyframesDialog(beatmapObject);
                    Updater.UpdateObject(beatmapObject, "Keyframes");
                    EditorManager.inst.DisplayNotification("Pasted scale keyframe data to selected scale keyframes!", 3f, EditorManager.NotificationType.Success);
                });

                EditorThemeManager.AddGraphic(pasteSca.image, ThemeGroup.Paste, true);
                EditorThemeManager.AddGraphic(pasteScaText, ThemeGroup.Paste_Text);

                RTEditor.GenerateLabels("paste_label", multiKeyframeEditor, new LabelSettings("Rotation / Color"));

                var pasteRotColObject = new GameObject("paste rot col base");
                pasteRotColObject.transform.SetParent(multiKeyframeEditor);
                pasteRotColObject.transform.localScale = Vector3.one;

                var pasteRotColObjectRT = pasteRotColObject.AddComponent<RectTransform>();
                pasteRotColObjectRT.sizeDelta = new Vector2(364f, 32f);

                var pasteRotColObjectHLG = pasteRotColObject.AddComponent<HorizontalLayoutGroup>();
                pasteRotColObjectHLG.childControlHeight = false;
                pasteRotColObjectHLG.childControlWidth = false;
                pasteRotColObjectHLG.childForceExpandHeight = false;
                pasteRotColObjectHLG.childForceExpandWidth = false;
                pasteRotColObjectHLG.spacing = 8f;

                var pasteRotObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(pasteRotColObjectRT, "paste");
                pasteRotObject.transform.localScale = Vector3.one;

                ((RectTransform)pasteRotObject.transform).sizeDelta = new Vector2(180f, 32f);

                var pasteRotText = pasteRotObject.transform.GetChild(0).GetComponent<Text>();
                pasteRotText.text = "Paste Rot";

                var pasteRot = pasteRotObject.GetComponent<Button>();
                pasteRot.onClick.ClearAll();
                pasteRot.onClick.AddListener(() =>
                {
                    var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                    var list = EditorTimeline.inst.CurrentSelection.InternalTimelineObjects.Where(x => x.Selected);

                    foreach (var timelineObject in list)
                    {
                        if (timelineObject.Type != 3)
                            continue;

                        var kf = timelineObject.GetData<EventKeyframe>();

                        if (CopiedRotationData != null)
                        {
                            kf.curveType = CopiedRotationData.curveType;
                            kf.eventValues = CopiedRotationData.eventValues.Copy();
                            kf.eventRandomValues = CopiedRotationData.eventRandomValues.Copy();
                            kf.random = CopiedRotationData.random;
                            kf.relative = CopiedRotationData.relative;
                        }
                    }

                    RenderKeyframes(beatmapObject);
                    RenderObjectKeyframesDialog(beatmapObject);
                    Updater.UpdateObject(beatmapObject, "Keyframes");
                    EditorManager.inst.DisplayNotification("Pasted rotation keyframe data to selected rotation keyframes!", 3f, EditorManager.NotificationType.Success);
                });

                EditorThemeManager.AddGraphic(pasteRot.image, ThemeGroup.Paste, true);
                EditorThemeManager.AddGraphic(pasteRotText, ThemeGroup.Paste_Text);

                var pasteColObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(pasteRotColObjectRT, "paste");
                pasteColObject.transform.localScale = Vector3.one;

                ((RectTransform)pasteColObject.transform).sizeDelta = new Vector2(180f, 32f);

                var pasteColText = pasteColObject.transform.GetChild(0).GetComponent<Text>();
                pasteColText.text = "Paste Col";

                var pasteCol = pasteColObject.GetComponent<Button>();
                pasteCol.onClick.ClearAll();
                pasteCol.onClick.AddListener(() =>
                {
                    var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                    var list = EditorTimeline.inst.CurrentSelection.InternalTimelineObjects.Where(x => x.Selected);

                    foreach (var timelineObject in list)
                    {
                        if (timelineObject.Type != 4)
                            continue;

                        var kf = timelineObject.GetData<EventKeyframe>();

                        if (CopiedColorData != null)
                        {
                            kf.curveType = CopiedColorData.curveType;
                            kf.eventValues = CopiedColorData.eventValues.Copy();
                            kf.eventRandomValues = CopiedColorData.eventRandomValues.Copy();
                            kf.random = CopiedColorData.random;
                            kf.relative = CopiedColorData.relative;
                        }
                    }

                    RenderKeyframes(beatmapObject);
                    RenderObjectKeyframesDialog(beatmapObject);
                    Updater.UpdateObject(beatmapObject, "Keyframes");
                    EditorManager.inst.DisplayNotification("Pasted color keyframe data to selected color keyframes!", 3f, EditorManager.NotificationType.Success);
                });

                EditorThemeManager.AddGraphic(pasteCol.image, ThemeGroup.Paste, true);
                EditorThemeManager.AddGraphic(pasteColText, ThemeGroup.Paste_Text);
            }

            timelinePosScrollbar = ObjEditor.inst.objTimelineContent.parent.parent.GetComponent<ScrollRect>().horizontalScrollbar;
            timelinePosScrollbar.onValueChanged.AddListener(_val =>
            {
                if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                    EditorTimeline.inst.CurrentSelection.TimelinePosition = _val;
            });

            var idRight = ObjEditor.inst.objTimelineContent.parent.Find("id/right");
            for (int i = 0; i < ObjEditor.inst.TimelineParents.Count; i++)
            {
                int tmpIndex = i;
                var entry = TriggerHelper.CreateEntry(EventTriggerType.PointerUp, eventData =>
                {
                    if (((PointerEventData)eventData).button != PointerEventData.InputButton.Right)
                        return;

                    float timeTmp = MouseTimelineCalc();

                    var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();

                    int index = beatmapObject.events[tmpIndex].FindLastIndex(x => x.eventTime <= timeTmp);
                    var eventKeyfame = AddEvent(beatmapObject, timeTmp, tmpIndex, (EventKeyframe)beatmapObject.events[tmpIndex][index], false);
                    UpdateKeyframeOrder(beatmapObject);

                    RenderKeyframes(beatmapObject);

                    int keyframe = beatmapObject.events[tmpIndex].FindLastIndex(x => x.eventTime == eventKeyfame.eventTime);
                    if (keyframe < 0)
                        keyframe = 0;

                    SetCurrentKeyframe(beatmapObject, tmpIndex, keyframe, false, InputDataManager.inst.editorActions.MultiSelect.IsPressed);
                    ResizeKeyframeTimeline(beatmapObject);

                    RenderObjectKeyframesDialog(beatmapObject);

                    // Keyframes affect both physical object and timeline object.
                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Keyframes");
                });

                var comp = ObjEditor.inst.TimelineParents[tmpIndex].GetComponent<EventTrigger>();
                comp.triggers.RemoveAll(x => x.eventID == EventTriggerType.PointerUp);
                comp.triggers.Add(entry);

                EditorThemeManager.AddGraphic(idRight.GetChild(i).GetComponent<Image>(), EditorThemeManager.EditorTheme.GetGroup($"Object Keyframe Color {i + 1}"));
            }

            ObjEditor.inst.objTimelineSlider.onValueChanged.RemoveAllListeners();
            ObjEditor.inst.objTimelineSlider.onValueChanged.AddListener(_val =>
            {
                if (!ObjEditor.inst.changingTime)
                    return;
                ObjEditor.inst.newTime = _val;
                AudioManager.inst.SetMusicTime(Mathf.Clamp(_val, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
            });

            var objectKeyframeTimelineEventTrigger = ObjEditor.inst.objTimelineContent.parent.parent.parent.GetComponent<EventTrigger>();
            ObjEditor.inst.objTimelineContent.GetComponent<EventTrigger>().triggers.AddRange(objectKeyframeTimelineEventTrigger.triggers);
            objectKeyframeTimelineEventTrigger.triggers.Clear();

            TriggerHelper.AddEventTriggers(timelinePosScrollbar.gameObject, TriggerHelper.CreateEntry(EventTriggerType.Scroll, baseEventData =>
            {
                var pointerEventData = (PointerEventData)baseEventData;

                var scrollBar = timelinePosScrollbar;
                float multiply = Input.GetKey(KeyCode.LeftAlt) ? 0.1f : Input.GetKey(KeyCode.LeftControl) ? 10f : 1f;

                scrollBar.value = pointerEventData.scrollDelta.y > 0f ? scrollBar.value + (0.005f * multiply) : pointerEventData.scrollDelta.y < 0f ? scrollBar.value - (0.005f * multiply) : 0f;
            }));

            try
            {
                InitShapes();
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to init shapes due to the exception: {ex}");
            } // init shapes

            AnimationEditor.Init();

            try
            {
                var prefabFilePath = RTFile.CombinePaths(Application.persistentDataPath, $"copied_objects{FileFormat.LSP.Dot()}");
                if (!RTFile.FileExists(prefabFilePath))
                    return;

                var jn = JSON.Parse(RTFile.ReadFromFile(prefabFilePath));
                ObjEditor.inst.beatmapObjCopy = Prefab.Parse(jn);
                ObjEditor.inst.hasCopiedObject = true;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Could not load global copied objects.\n{ex}");
            } // load global copy

            try
            {
                Dialog = new ObjectEditorDialog();
                Dialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        void InitShapes()
        {
            CoreHelper.Log($"Caching values...");
            var shape = ObjEditor.inst.ObjectView.transform.Find("shape");
            var shapeSettings = ObjEditor.inst.ObjectView.transform.Find("shapesettings");

            shapeButtonPrefab = shape.Find("1").gameObject.Duplicate(transform);

            var shapeGLG = shape.gameObject.GetOrAddComponent<GridLayoutGroup>();
            shapeGLG.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            shapeGLG.constraintCount = 1;
            shapeGLG.spacing = new Vector2(7.6f, 0f);

            if (!updatedShapes)
            {
                CoreHelper.Log($"Removing...");
                DestroyImmediate(shape.GetComponent<ToggleGroup>());

                var toDestroy = new List<GameObject>();

                for (int i = 0; i < shape.childCount; i++)
                    toDestroy.Add(shape.GetChild(i).gameObject);

                for (int i = 0; i < shapeSettings.childCount; i++)
                {
                    if (i != 4 && i != 6)
                        for (int j = 0; j < shapeSettings.GetChild(i).childCount; j++)
                            toDestroy.Add(shapeSettings.GetChild(i).GetChild(j).gameObject);
                }

                foreach (var obj in toDestroy)
                    DestroyImmediate(obj);

                toDestroy = null;

                CoreHelper.Log($"Adding shapes...");
                for (int i = 0; i < ShapeManager.inst.Shapes2D.Count; i++)
                {
                    var obj = shapeButtonPrefab.Duplicate(shape, (i + 1).ToString(), i);
                    if (obj.transform.Find("Image") && obj.transform.Find("Image").gameObject.TryGetComponent(out Image image))
                    {
                        image.sprite = ShapeManager.inst.Shapes2D[i][0].icon;
                        EditorThemeManager.ApplyGraphic(image, ThemeGroup.Toggle_1_Check);
                    }

                    if (!obj.GetComponent<HoverUI>())
                    {
                        var hoverUI = obj.AddComponent<HoverUI>();
                        hoverUI.animatePos = false;
                        hoverUI.animateSca = true;
                        hoverUI.size = 1.1f;
                    }

                    var shapeToggle = obj.GetComponent<Toggle>();
                    EditorThemeManager.ApplyToggle(shapeToggle, ThemeGroup.Background_1);

                    shapeToggles.Add(shapeToggle);

                    shapeOptionToggles.Add(new List<Toggle>());

                    if (i != 4 && i != 6)
                    {
                        var so = shapeSettings.Find((i + 1).ToString());
                        if (!so)
                        {
                            so = shapeSettings.Find("6").gameObject.Duplicate(shapeSettings, (i + 1).ToString()).transform;
                            CoreHelper.DestroyChildren(so);
                        }

                        var rect = so.AsRT();
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

                        for (int j = 0; j < ShapeManager.inst.Shapes2D[i].Count; j++)
                        {
                            var opt = shapeButtonPrefab.Duplicate(shapeSettings.GetChild(i), (j + 1).ToString(), j);
                            if (opt.transform.Find("Image") && opt.transform.Find("Image").gameObject.TryGetComponent(out Image image1))
                            {
                                image1.sprite = ShapeManager.inst.Shapes2D[i][j].icon;
                                EditorThemeManager.ApplyGraphic(image1, ThemeGroup.Toggle_1_Check);
                            }

                            if (!opt.GetComponent<HoverUI>())
                            {
                                var hoverUI = opt.AddComponent<HoverUI>();
                                hoverUI.animatePos = false;
                                hoverUI.animateSca = true;
                                hoverUI.size = 1.1f;
                            }

                            var shapeOptionToggle = opt.GetComponent<Toggle>();
                            EditorThemeManager.ApplyToggle(shapeOptionToggle, ThemeGroup.Background_1);

                            shapeOptionToggles[i].Add(shapeOptionToggle);

                            var layoutElement = opt.AddComponent<LayoutElement>();
                            layoutElement.layoutPriority = 1;
                            layoutElement.minWidth = 32f;

                            ((RectTransform)opt.transform).sizeDelta = new Vector2(32f, 32f);

                            if (!opt.GetComponent<HoverUI>())
                            {
                                var he = opt.AddComponent<HoverUI>();
                                he.animatePos = false;
                                he.animateSca = true;
                                he.size = 1.1f;
                            }
                        }

                        LastGameObject(shapeSettings.GetChild(i));
                    }
                }

                CoreHelper.Log($"Checking player shapes...");
                if (ObjectManager.inst.objectPrefabs.Count > 9)
                {
                    var playerSprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_player{FileFormat.PNG.Dot()}"));
                    int i = shape.childCount;
                    var obj = shapeButtonPrefab.Duplicate(shape, (i + 1).ToString());
                    if (obj.transform.Find("Image") && obj.transform.Find("Image").gameObject.TryGetComponent(out Image image))
                    {
                        image.sprite = playerSprite;
                        EditorThemeManager.ApplyGraphic(image, ThemeGroup.Toggle_1_Check);
                    }

                    var so = shapeSettings.Find((i + 1).ToString());

                    if (!so)
                    {
                        so = shapeSettings.Find("6").gameObject.Duplicate(shapeSettings, (i + 1).ToString()).transform;
                        CoreHelper.DestroyChildren(so);
                    }

                    var rect = so.AsRT();
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

                    var shapeToggle = obj.GetComponent<Toggle>();
                    shapeToggles.Add(shapeToggle);
                    EditorThemeManager.ApplyToggle(shapeToggle, ThemeGroup.Background_1);

                    shapeOptionToggles.Add(new List<Toggle>());

                    for (int j = 0; j < ObjectManager.inst.objectPrefabs[9].options.Count; j++)
                    {
                        var opt = shapeButtonPrefab.Duplicate(shapeSettings.GetChild(i), (j + 1).ToString(), j);
                        if (opt.transform.Find("Image") && opt.transform.Find("Image").gameObject.TryGetComponent(out Image image1))
                        {
                            image1.sprite = playerSprite;
                            EditorThemeManager.ApplyGraphic(image1, ThemeGroup.Toggle_1_Check);
                        }

                        var shapeOptionToggle = opt.GetComponent<Toggle>();
                        EditorThemeManager.ApplyToggle(shapeOptionToggle, ThemeGroup.Background_1);

                        shapeOptionToggles[i].Add(shapeOptionToggle);

                        var layoutElement = opt.AddComponent<LayoutElement>();
                        layoutElement.layoutPriority = 1;
                        layoutElement.minWidth = 32f;

                        ((RectTransform)opt.transform).sizeDelta = new Vector2(32f, 32f);

                        if (!opt.GetComponent<HoverUI>())
                        {
                            var he = opt.AddComponent<HoverUI>();
                            he.animatePos = false;
                            he.animateSca = true;
                            he.size = 1.1f;
                        }
                    }

                    LastGameObject(shapeSettings.GetChild(i));
                }

                var textIF = shapeSettings.Find("5").GetComponent<InputField>();
                if (!textIF.transform.Find("edit"))
                {
                    var button = EditorPrefabHolder.Instance.DeleteButton.Duplicate(textIF.transform, "edit");
                    var buttonStorage = button.GetComponent<DeleteButtonStorage>();
                    buttonStorage.image.sprite = EditorSprites.EditSprite;
                    EditorThemeManager.ApplySelectable(buttonStorage.button, ThemeGroup.Function_2);
                    EditorThemeManager.ApplyGraphic(buttonStorage.image, ThemeGroup.Function_2_Text);
                    buttonStorage.button.onClick.ClearAll();
                    buttonStorage.button.onClick.AddListener(() => TextEditor.inst.SetInputField(textIF));
                    UIManager.SetRectTransform(buttonStorage.baseImage.rectTransform, new Vector2(160f, 24f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(22f, 22f));
                    EditorHelper.SetComplexity(button, Complexity.Advanced);
                }

                updatedShapes = true;
            }
        }

        #endregion

        #region Variables

        public ObjectEditorDialog Dialog { get; set; }

        public Text ldmLabel;
        public Toggle ldmToggle;

        public Scrollbar timelinePosScrollbar;
        public GameObject shapeButtonPrefab;


        public List<TimelineObject> copiedObjectKeyframes = new List<TimelineObject>();

        public EventKeyframe CopiedPositionData { get; set; }
        public EventKeyframe CopiedScaleData { get; set; }
        public EventKeyframe CopiedRotationData { get; set; }
        public EventKeyframe CopiedColorData { get; set; }

        public List<Toggle> gradientColorButtons = new List<Toggle>();

        public bool colorShifted;

        public static bool RenderPrefabTypeIcon { get; set; }

        public static float TimelineObjectHoverSize { get; set; }

        public static float TimelineCollapseLength { get; set; }

        #endregion

        /// <summary>
        /// Sets the Object Keyframe timeline zoom and position.
        /// </summary>
        /// <param name="zoom">The amount to zoom in.</param>
        /// <param name="position">The position to set the timeline scroll. If the value is less that 0, it will automatically calculate the position to match the audio time.</param>
        /// <param name="render">If the timeline should render.</param>
        public void SetTimeline(float zoom, float position = -1f, bool render = true, bool log = true)
        {
            float prevZoom = ObjEditor.inst.zoomFloat;
            ObjEditor.inst.zoomFloat = Mathf.Clamp01(zoom);
            ObjEditor.inst.zoomVal =
                LSMath.InterpolateOverCurve(ObjEditor.inst.ZoomCurve, ObjEditor.inst.zoomBounds.x, ObjEditor.inst.zoomBounds.y, ObjEditor.inst.zoomFloat);

            var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
            EditorTimeline.inst.CurrentSelection.Zoom = ObjEditor.inst.zoomFloat;

            if (render)
            {
                ResizeKeyframeTimeline(beatmapObject);
                RenderKeyframes(beatmapObject);
            }

            CoreHelper.StartCoroutine(SetTimelinePosition(beatmapObject, position));

            ObjEditor.inst.zoomSlider.onValueChanged.ClearAll();
            ObjEditor.inst.zoomSlider.value = ObjEditor.inst.zoomFloat;
            ObjEditor.inst.zoomSlider.onValueChanged.AddListener(_val =>
            {
                ObjEditor.inst.Zoom = _val;
                EditorTimeline.inst.CurrentSelection.Zoom = Mathf.Clamp01(_val);
            });

            if (log)
                CoreHelper.Log($"SET OBJECT ZOOM\n" +
                    $"ZoomFloat: {ObjEditor.inst.zoomFloat}\n" +
                    $"ZoomVal: {ObjEditor.inst.zoomVal}\n" +
                    $"ZoomBounds: {ObjEditor.inst.zoomBounds}\n" +
                    $"Timeline Position: {timelinePosScrollbar.value}");
        }

        IEnumerator SetTimelinePosition(BeatmapObject beatmapObject, float position = 0f)
        {
            yield return new WaitForFixedUpdate();
            float timelineCalc = ObjEditor.inst.objTimelineSlider.value;
            if (AudioManager.inst.CurrentAudioSource.clip != null)
            {
                float time = -beatmapObject.StartTime + AudioManager.inst.CurrentAudioSource.time;
                float objectLifeLength = beatmapObject.GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset);

                timelineCalc = time / objectLifeLength;
            }

            timelinePosScrollbar.value =
                position >= 0f ? position : timelineCalc;
        }

        public static float TimeTimelineCalc(float _time) => _time * 14f * ObjEditor.inst.zoomVal + 5f;

        public static float MouseTimelineCalc()
        {
            float num = Screen.width * ((1155f - Mathf.Abs(ObjEditor.inst.timelineScroll.transform.AsRT().anchoredPosition.x) + 7f) / 1920f);
            float screenScale = 1f / (Screen.width / 1920f);
            float mouseX = Input.mousePosition.x < num ? num : Input.mousePosition.x;

            return (mouseX - num) / ObjEditor.inst.Zoom / 14f * screenScale;
        }

        #region Dragging

        void Update()
        {
            if (!ObjEditor.inst.changingTime && EditorTimeline.inst.CurrentSelection && EditorTimeline.inst.CurrentSelection.isBeatmapObject)
            {
                // Sets new audio time using the Object Keyframe timeline cursor.
                ObjEditor.inst.newTime = Mathf.Clamp(EditorManager.inst.CurrentAudioPos,
                    EditorTimeline.inst.CurrentSelection.Time,
                    EditorTimeline.inst.CurrentSelection.Time + EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>().GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset));
                ObjEditor.inst.objTimelineSlider.value = ObjEditor.inst.newTime;
            }

            if (Input.GetMouseButtonUp(0))
            {
                ObjEditor.inst.beatmapObjectsDrag = false;
                ObjEditor.inst.timelineKeyframesDrag = false;
                RTEditor.inst.dragOffset = -1f;
                RTEditor.inst.dragBinOffset = -100;
            }

            HandleObjectsDrag();
            HandleKeyframesDrag();
        }

        void HandleObjectsDrag()
        {
            if (!ObjEditor.inst.beatmapObjectsDrag)
                return;

            if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
            {
                int binOffset = 14 - Mathf.RoundToInt((float)((Input.mousePosition.y - 25) * EditorManager.inst.ScreenScaleInverse / 20)) + ObjEditor.inst.mouseOffsetYForDrag;

                bool hasChanged = false;

                foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                {
                    if (timelineObject.Locked)
                        continue;

                    int binCalc = EditorConfig.Instance.TimelineObjectRetainsBinOnDrag.Value ? binOffset + timelineObject.binOffset : Mathf.Clamp(binOffset + timelineObject.binOffset, 0, EditorTimeline.inst.BinCount);

                    if (timelineObject.Bin != binCalc)
                        hasChanged = true;

                    timelineObject.Bin = binCalc;
                    timelineObject.RenderPosLength();
                    if (timelineObject.isBeatmapObject && EditorTimeline.inst.SelectedObjects.Count == 1)
                        RenderBin(timelineObject.GetData<BeatmapObject>());
                }

                if (RTEditor.inst.dragBinOffset != binOffset && !EditorTimeline.inst.SelectedObjects.All(x => x.Locked))
                {
                    if (hasChanged && RTEditor.DraggingPlaysSound)
                        SoundManager.inst.PlaySound(DefaultSounds.UpDown, 0.4f, 0.6f);

                    RTEditor.inst.dragBinOffset = binOffset;
                }

                return;
            }

            float timeOffset = Mathf.Round(Mathf.Clamp(EditorTimeline.inst.GetTimelineTime() + ObjEditor.inst.mouseOffsetXForDrag,
                0f, AudioManager.inst.CurrentAudioSource.clip.length) * 1000f) / 1000f;

            if (RTEditor.inst.dragOffset != timeOffset && !EditorTimeline.inst.SelectedObjects.All(x => x.Locked))
            {
                if (RTEditor.DraggingPlaysSound && (SettingEditor.inst.SnapActive || !RTEditor.DraggingPlaysSoundBPM))
                    SoundManager.inst.PlaySound(DefaultSounds.LeftRight, SettingEditor.inst.SnapActive ? 0.6f : 0.1f, 0.7f);

                RTEditor.inst.dragOffset = timeOffset;
            }

            if (!Updater.levelProcessor || !Updater.levelProcessor.engine || Updater.levelProcessor.engine.objectSpawner == null)
                return;

            var spawner = Updater.levelProcessor.engine.objectSpawner;

            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                if (timelineObject.Locked)
                    continue;

                timelineObject.Time = Mathf.Clamp(timeOffset + timelineObject.timeOffset, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                timelineObject.RenderPosLength();

                if (timelineObject.isPrefabObject)
                {
                    var prefabObject = timelineObject.GetData<PrefabObject>();
                    RTPrefabEditor.inst.RenderPrefabObjectDialog(prefabObject);
                    Updater.UpdatePrefab(prefabObject, "Drag");
                    continue;
                }

                var beatmapObject = timelineObject.GetData<BeatmapObject>();

                if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject))
                {
                    levelObject.StartTime = beatmapObject.StartTime;
                    levelObject.KillTime = beatmapObject.StartTime + beatmapObject.SpawnDuration;

                    levelObject.SetActive(beatmapObject.Alive);

                    for (int i = 0; i < levelObject.parentObjects.Count; i++)
                    {
                        var levelParent = levelObject.parentObjects[i];
                        var parent = levelParent.BeatmapObject;

                        levelParent.timeOffset = parent.StartTime;
                    }
                }

                if (EditorTimeline.inst.SelectedObjectCount == 1)
                {
                    RenderStartTime(beatmapObject);
                    ResizeKeyframeTimeline(beatmapObject);
                }
            }

            Updater.Sort();

            if (EditorConfig.Instance.UpdateHomingKeyframesDrag.Value)
                System.Threading.Tasks.Task.Run(Updater.UpdateHomingKeyframes);
        }

        void HandleKeyframesDrag()
        {
            if (!ObjEditor.inst.timelineKeyframesDrag || !EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                return;

            var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();

            var snap = EditorConfig.Instance.BPMSnapsKeyframes.Value;
            var timelineCalc = MouseTimelineCalc();
            var selected = EditorTimeline.inst.CurrentSelection.InternalTimelineObjects.Where(x => x.Selected);
            var startTime = beatmapObject.StartTime;

            foreach (var timelineObject in selected)
            {
                if (timelineObject.Index == 0 || timelineObject.Locked)
                    continue;

                float calc = Mathf.Clamp(
                    Mathf.Round(Mathf.Clamp(timelineCalc + timelineObject.timeOffset + ObjEditor.inst.mouseOffsetXForKeyframeDrag, 0f, AudioManager.inst.CurrentAudioSource.clip.length) * 1000f) / 1000f,
                    0f, beatmapObject.GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset));

                float st = beatmapObject.StartTime;

                st = SettingEditor.inst.SnapActive && snap && !Input.GetKey(KeyCode.LeftAlt) ? -(st - RTEditor.SnapToBPM(st + calc)) : calc;

                beatmapObject.events[timelineObject.Type][timelineObject.Index].eventTime = st;

                ((RectTransform)timelineObject.GameObject.transform).anchoredPosition = new Vector2(TimeTimelineCalc(st), 0f);

                RenderKeyframe(beatmapObject, timelineObject);
            }

            Updater.UpdateObject(beatmapObject, "Keyframes");
            Updater.UpdateObject(beatmapObject, "Autokill");
            RenderObjectKeyframesDialog(beatmapObject);
            ResizeKeyframeTimeline(beatmapObject);

            foreach (var timelineObject in EditorTimeline.inst.SelectedBeatmapObjects)
                EditorTimeline.inst.RenderTimelineObject(timelineObject);

            if (!selected.All(x => x.Locked) && RTEditor.inst.dragOffset != timelineCalc + ObjEditor.inst.mouseOffsetXForDrag)
            {
                if (RTEditor.DraggingPlaysSound && (SettingEditor.inst.SnapActive && snap || !RTEditor.DraggingPlaysSoundBPM))
                    SoundManager.inst.PlaySound(DefaultSounds.LeftRight, SettingEditor.inst.SnapActive && snap ? 0.6f : 0.1f, 0.8f);

                RTEditor.inst.dragOffset = timelineCalc + ObjEditor.inst.mouseOffsetXForDrag;
            }
        }

        #endregion

        #region Deleting

        public IEnumerator DeleteObjects(bool _set = true)
        {
            var list = EditorTimeline.inst.SelectedObjects;
            int count = EditorTimeline.inst.SelectedObjectCount;

            var gameData = GameData.Current;
            if (count == gameData.beatmapObjects.FindAll(x => !x.fromPrefab).Count + gameData.prefabObjects.Count)
                yield break;

            int min = list.Min(x => x.Index) - 1;

            var beatmapObjects = list.FindAll(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()).ToList();
            var beatmapObjectIDs = new List<string>();
            var prefabObjectIDs = new List<string>();

            beatmapObjectIDs.AddRange(list.FindAll(x => x.isBeatmapObject).Select(x => x.ID));
            prefabObjectIDs.AddRange(list.FindAll(x => x.isPrefabObject).Select(x => x.ID));

            if (beatmapObjectIDs.Count == gameData.beatmapObjects.FindAll(x => !x.fromPrefab).Count)
                yield break;

            if (prefabObjectIDs.Count > 0)
                list.FindAll(x => x.isPrefabObject)
                    .Select(x => x.GetData<PrefabObject>()).ToList()
                    .ForEach(x => beatmapObjectIDs
                        .AddRange(GameData.Current.beatmapObjects
                            .Where(c => c.prefabInstanceID == x.ID)
                        .Select(c => c.id)));

            gameData.beatmapObjects.FindAll(x => beatmapObjectIDs.Contains(x.id)).ForEach(x =>
                {
                    for (int i = 0; i < x.modifiers.Count; i++)
                    {
                        var modifier = x.modifiers[i];
                        try
                        {
                            modifier.Inactive?.Invoke(modifier); // for cases where we want to clear data.
                        }
                        catch (Exception ex)
                        {
                            CoreHelper.LogException(ex);
                        } // allow further objects to be deleted if a modifiers' inactive state throws an error
                    }

                    Updater.UpdateObject(x, reinsert: false, recalculate: false);
                });
            gameData.beatmapObjects.FindAll(x => prefabObjectIDs.Contains(x.prefabInstanceID)).ForEach(x => Updater.UpdateObject(x, reinsert: false, recalculate: false));

            gameData.beatmapObjects.RemoveAll(x => beatmapObjectIDs.Contains(x.id));
            gameData.beatmapObjects.RemoveAll(x => prefabObjectIDs.Contains(x.prefabInstanceID));
            gameData.prefabObjects.RemoveAll(x => prefabObjectIDs.Contains(x.ID));

            Updater.RecalculateObjectStates();

            EditorTimeline.inst.timelineObjects.FindAll(x => beatmapObjectIDs.Contains(x.ID) || prefabObjectIDs.Contains(x.ID)).ForEach(x => Destroy(x.GameObject));
            EditorTimeline.inst.timelineObjects.RemoveAll(x => beatmapObjectIDs.Contains(x.ID) || prefabObjectIDs.Contains(x.ID));

            EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.timelineObjects[Mathf.Clamp(min, 0, EditorTimeline.inst.timelineObjects.Count - 1)]);

            EditorManager.inst.DisplayNotification($"Deleted Beatmap Objects [ {count} ]", 1f, EditorManager.NotificationType.Success);
            yield break;
        }

        public IEnumerator DeleteObject(TimelineObject timelineObject, bool _set = true)
        {
            int index = timelineObject.Index;

            EditorTimeline.inst.RemoveTimelineObject(timelineObject);

            if (timelineObject.isBeatmapObject)
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();

                if (GameData.Current.beatmapObjects.Count > 1)
                {
                    Updater.UpdateObject(beatmapObject, reinsert: false, recalculate: false);
                    string id = beatmapObject.id;

                    index = GameData.Current.beatmapObjects.FindIndex(x => x.id == id);

                    GameData.Current.beatmapObjects.RemoveAt(index);

                    foreach (var bm in GameData.Current.beatmapObjects)
                    {
                        if (bm.parent == id)
                        {
                            bm.parent = "";

                            Updater.UpdateObject(bm, recalculate: false);
                        }
                    }

                    Updater.RecalculateObjectStates();
                }
                else
                    EditorManager.inst.DisplayNotification("Can't delete only object", 2f, EditorManager.NotificationType.Error);
            }
            else if (timelineObject.isPrefabObject)
            {
                var prefabObject = timelineObject.GetData<PrefabObject>();

                Updater.UpdatePrefab(prefabObject, false);

                string id = prefabObject.ID;

                index = GameData.Current.prefabObjects.FindIndex(x => x.ID == id);
                GameData.Current.prefabObjects.RemoveAt(index);
            }

            if (_set && EditorTimeline.inst.timelineObjects.Count > 0)
                EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.timelineObjects[Mathf.Clamp(index - 1, 0, EditorTimeline.inst.timelineObjects.Count - 1)]);

            yield break;
        }

        public IEnumerator DeleteKeyframes()
        {
            if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                yield return DeleteKeyframes(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
            yield break;
        }

        public IEnumerator DeleteKeyframes(BeatmapObject beatmapObject)
        {
            var bmTimelineObject = EditorTimeline.inst.GetTimelineObject(beatmapObject);

            var list = bmTimelineObject.InternalTimelineObjects.Where(x => x.Selected).ToList();
            int count = list.Where(x => x.Index != 0).Count();

            if (count < 1)
            {
                EditorManager.inst.DisplayNotification($"No Object keyframes to delete.", 2f, EditorManager.NotificationType.Warning);
                yield break;
            }

            int index = list.Min(x => x.Index);
            int type = list.Min(x => x.Type);
            bool allOfTheSameType = list.All(x => x.Type == list.Min(y => y.Type));

            EditorManager.inst.DisplayNotification($"Deleting Object Keyframes [ {count} ]", 0.2f, EditorManager.NotificationType.Success);

            UpdateKeyframeOrder(beatmapObject);

            var strs = new List<string>();
            foreach (var timelineObject in list)
            {
                if (timelineObject.Index != 0)
                    strs.Add(timelineObject.GetData<EventKeyframe>().id);
            }

            for (int i = 0; i < beatmapObject.events.Count; i++)
            {
                beatmapObject.events[i].RemoveAll(x => strs.Contains(((EventKeyframe)x).id));
            }

            bmTimelineObject.InternalTimelineObjects.Where(x => x.Selected).ToList().ForEach(x => Destroy(x.GameObject));
            bmTimelineObject.InternalTimelineObjects.RemoveAll(x => x.Selected);

            EditorTimeline.inst.RenderTimelineObject(bmTimelineObject);
            Updater.UpdateObject(beatmapObject, "Keyframes");

            if (beatmapObject.autoKillType == AutoKillType.LastKeyframe || beatmapObject.autoKillType == AutoKillType.LastKeyframeOffset)
                Updater.UpdateObject(beatmapObject, "Autokill");

            RenderKeyframes(beatmapObject);

            if (count == 1 || allOfTheSameType)
                SetCurrentKeyframe(beatmapObject, type, Mathf.Clamp(index - 1, 0, beatmapObject.events[type].Count - 1));
            else
                SetCurrentKeyframe(beatmapObject, type, 0);

            ResizeKeyframeTimeline(beatmapObject);

            EditorManager.inst.DisplayNotification("Deleted Object Keyframes [ " + count + " ]", 2f, EditorManager.NotificationType.Success);

            yield break;
        }

        public void DeleteKeyframe(BeatmapObject beatmapObject, TimelineObject timelineObject)
        {
            if (timelineObject.Index != 0)
            {
                Debug.Log($"{ObjEditor.inst.className}Deleting keyframe: ({timelineObject.Type}, {timelineObject.Index})");
                beatmapObject.events[timelineObject.Type].RemoveAt(timelineObject.Index);

                Destroy(timelineObject.GameObject);

                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject, "Keyframes");
                return;
            }
            EditorManager.inst.DisplayNotification("Can't delete first Keyframe", 2f, EditorManager.NotificationType.Error, false);
        }

        #endregion

        #region Copy / Paste

        public void CopyAllSelectedEvents(BeatmapObject beatmapObject)
        {
            copiedObjectKeyframes.Clear();
            UpdateKeyframeOrder(beatmapObject);

            var bmTimelineObject = EditorTimeline.inst.GetTimelineObject(beatmapObject);

            float num = bmTimelineObject.InternalTimelineObjects.Where(x => x.Selected).Min(x => x.Time);

            foreach (var timelineObject in bmTimelineObject.InternalTimelineObjects.Where(x => x.Selected))
            {
                int type = timelineObject.Type;
                int index = timelineObject.Index;
                var eventKeyframe = EventKeyframe.DeepCopy((EventKeyframe)beatmapObject.events[type][index]);
                eventKeyframe.eventTime -= num;

                copiedObjectKeyframes.Add(new TimelineObject(eventKeyframe) { Type = type, Index = index, isObjectKeyframe = true });
            }
        }

        public void PasteKeyframes(BeatmapObject beatmapObject, bool setTime = true) => PasteKeyframes(beatmapObject, copiedObjectKeyframes, setTime);

        public void PasteKeyframes(BeatmapObject beatmapObject, List<TimelineObject> kfs, bool setTime = true)
        {
            if (kfs.Count <= 0)
            {
                Debug.LogError($"{ObjEditor.inst.className}No copied event yet!");
                return;
            }

            var ids = new List<string>();
            for (int i = 0; i < beatmapObject.events.Count; i++)
                beatmapObject.events[i].AddRange(kfs.Where(x => x.Type == i).Select(x =>
                {
                    var kf = PasteKF(beatmapObject, x, setTime);
                    ids.Add(kf.id);
                    return kf;
                }));

            ResizeKeyframeTimeline(beatmapObject);
            UpdateKeyframeOrder(beatmapObject);
            RenderKeyframes(beatmapObject);

            if (EditorConfig.Instance.SelectPasted.Value)
            {
                var timelineObject = EditorTimeline.inst.GetTimelineObject(beatmapObject);
                foreach (var kf in timelineObject.InternalTimelineObjects)
                    kf.Selected = ids.Contains(kf.ID);
            }

            RenderObjectKeyframesDialog(beatmapObject);
            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));

            if (UpdateObjects)
            {
                Updater.UpdateObject(beatmapObject, "Keyframes");
                Updater.UpdateObject(beatmapObject, "Autokill");
            }
        }

        public EventKeyframe PasteKF(BeatmapObject beatmapObject, TimelineObject timelineObject, bool setTime = true)
        {
            var eventKeyframe = EventKeyframe.DeepCopy(timelineObject.GetData<EventKeyframe>());

            var time = EditorManager.inst.CurrentAudioPos;
            if (SettingEditor.inst.SnapActive)
                time = RTEditor.SnapToBPM(time);

            if (!setTime)
                return eventKeyframe;

            eventKeyframe.eventTime = time - beatmapObject.StartTime + eventKeyframe.eventTime;
            if (eventKeyframe.eventTime <= 0f)
                eventKeyframe.eventTime = 0.001f;

            return eventKeyframe;
        }

        public void PasteObject(float _offsetTime = 0f, bool _regen = true)
        {
            if (!ObjEditor.inst.hasCopiedObject || ObjEditor.inst.beatmapObjCopy == null || (ObjEditor.inst.beatmapObjCopy.prefabObjects.Count <= 0 && ObjEditor.inst.beatmapObjCopy.objects.Count <= 0))
            {
                EditorManager.inst.DisplayNotification("No copied object yet!", 1f, EditorManager.NotificationType.Error, false);
                return;
            }

            EditorTimeline.inst.DeselectAllObjects();
            EditorManager.inst.DisplayNotification("Pasting objects, please wait.", 1f, EditorManager.NotificationType.Success);

            StartCoroutine(AddPrefabExpandedToLevel((Prefab)ObjEditor.inst.beatmapObjCopy, true, _offsetTime, false, _regen));
        }

        public EventKeyframe GetCopiedData(int type) => type switch
        {
            0 => CopiedPositionData,
            1 => CopiedScaleData,
            2 => CopiedRotationData,
            3 => CopiedColorData,
            _ => null,
        };

        #endregion

        #region Prefabs

        /// <summary>
        /// Expands a prefab into the level.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="select"></param>
        /// <param name="offset"></param>
        /// <param name="undone"></param>
        /// <param name="regen"></param>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public IEnumerator AddPrefabExpandedToLevel(Prefab prefab, bool select = false, float offset = 0f, bool undone = false, bool regen = false, bool retainID = false)
        {
            RTEditor.inst.ienumRunning = true;
            float delay = 0f;
            float audioTime = EditorManager.inst.CurrentAudioPos;
            CoreHelper.Log($"Placing prefab with {prefab.objects.Count} objects and {prefab.prefabObjects.Count} prefabs");

            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
                EditorTimeline.inst.SetLayer(EditorTimeline.LayerType.Objects);

            if (EditorTimeline.inst.CurrentSelection.isBeatmapObject && prefab.objects.Count > 0)
                ClearKeyframes(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());

            if (prefab.objects.Count > 1 || prefab.prefabObjects.Count > 1)
                EditorManager.inst.ClearDialogs();

            var sw = CoreHelper.StartNewStopwatch();

            var pasteObjectsYieldType = EditorConfig.Instance.PasteObjectsYieldMode.Value;
            var updatePastedObjectsYieldType = EditorConfig.Instance.UpdatePastedObjectsYieldMode.Value;

            //Objects
            {
                var objectIDs = new List<IDPair>();
                for (int j = 0; j < prefab.objects.Count; j++)
                    objectIDs.Add(new IDPair(prefab.objects[j].id));

                var pastedObjects = new List<BeatmapObject>();
                var unparentedPastedObjects = new List<BeatmapObject>();
                for (int i = 0; i < prefab.objects.Count; i++)
                {
                    var beatmapObject = prefab.objects[i];
                    if (i > 0 && pasteObjectsYieldType != YieldType.None)
                        yield return CoreHelper.GetYieldInstruction(pasteObjectsYieldType, ref delay);

                    var beatmapObjectCopy = BeatmapObject.DeepCopy((BeatmapObject)beatmapObject, false);

                    if (!retainID)
                        beatmapObjectCopy.id = objectIDs[i].newID;

                    if (!retainID && !string.IsNullOrEmpty(beatmapObject.parent) && objectIDs.TryFind(x => x.oldID == beatmapObject.parent, out IDPair idPair))
                        beatmapObjectCopy.parent = idPair.newID;
                    else if (!retainID && !string.IsNullOrEmpty(beatmapObject.parent) && GameData.Current.beatmapObjects.FindIndex(x => x.id == beatmapObject.parent) == -1 && beatmapObjectCopy.parent != BeatmapObject.CAMERA_PARENT)
                        beatmapObjectCopy.parent = "";

                    if (regen)
                        beatmapObjectCopy.RemovePrefabReference();
                    else
                    {
                        beatmapObjectCopy.prefabID = beatmapObject.prefabID;
                        beatmapObjectCopy.prefabInstanceID = beatmapObject.prefabInstanceID;
                    }

                    beatmapObjectCopy.fromPrefab = false;

                    beatmapObjectCopy.StartTime += offset == 0.0 ? undone ? prefab.Offset : audioTime + prefab.Offset : offset;
                    if (offset != 0.0)
                        ++beatmapObjectCopy.editorData.Bin;

                    if (beatmapObjectCopy.shape == 6 && !string.IsNullOrEmpty(beatmapObjectCopy.text) && prefab.SpriteAssets.TryGetValue(beatmapObjectCopy.text, out Sprite sprite))
                        AssetManager.SpriteAssets[beatmapObjectCopy.text] = sprite;

                    beatmapObjectCopy.editorData.layer = EditorTimeline.inst.Layer;
                    GameData.Current.beatmapObjects.Add(beatmapObjectCopy);
                    if (Updater.levelProcessor && Updater.levelProcessor.converter != null)
                        Updater.levelProcessor.converter.beatmapObjects[beatmapObjectCopy.id] = beatmapObjectCopy;

                    if (string.IsNullOrEmpty(beatmapObject.parent) || beatmapObjectCopy.parent == BeatmapObject.CAMERA_PARENT || GameData.Current.beatmapObjects.FindIndex(x => x.id == beatmapObject.parent) != -1) // prevent updating of parented objects since updating is recursive.
                        unparentedPastedObjects.Add(beatmapObjectCopy);
                    pastedObjects.Add(beatmapObjectCopy);

                    var timelineObject = new TimelineObject(beatmapObjectCopy);

                    timelineObject.Selected = true;
                    EditorTimeline.inst.CurrentSelection = timelineObject;

                    EditorTimeline.inst.RenderTimelineObject(timelineObject);
                }

                var list = unparentedPastedObjects.Count > 0 ? unparentedPastedObjects : pastedObjects;
                delay = 0f;
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0 && updatePastedObjectsYieldType != YieldType.None)
                        yield return CoreHelper.GetYieldInstruction(updatePastedObjectsYieldType, ref delay);
                    Updater.UpdateObject(list[i], recalculate: false);
                }

                unparentedPastedObjects.Clear();
                unparentedPastedObjects = null;
                pastedObjects.Clear();
                pastedObjects = null;
            }

            //Prefabs
            {
                var ids = new List<string>();
                for (int i = 0; i < prefab.prefabObjects.Count; i++)
                    ids.Add(LSText.randomString(16));

                delay = 0f;
                for (int i = 0; i < prefab.prefabObjects.Count; i++)
                {
                    var prefabObject = prefab.prefabObjects[i];
                    if (i > 0 && pasteObjectsYieldType != YieldType.None)
                        yield return CoreHelper.GetYieldInstruction(pasteObjectsYieldType, ref delay);

                    var prefabObjectCopy = PrefabObject.DeepCopy((PrefabObject)prefabObject, false);
                    prefabObjectCopy.ID = ids[i];
                    prefabObjectCopy.prefabID = prefabObject.prefabID;

                    prefabObjectCopy.StartTime += offset == 0.0 ? undone ? prefab.Offset : audioTime + prefab.Offset : offset;
                    if (offset != 0.0)
                        ++prefabObjectCopy.editorData.Bin;

                    prefabObjectCopy.editorData.layer = EditorTimeline.inst.Layer;

                    GameData.Current.prefabObjects.Add(prefabObjectCopy);

                    var timelineObject = new TimelineObject(prefabObjectCopy);

                    timelineObject.Selected = true;
                    EditorTimeline.inst.CurrentSelection = timelineObject;

                    EditorTimeline.inst.RenderTimelineObject(timelineObject);

                    Updater.AddPrefabToLevel(prefabObjectCopy, recalculate: false);
                }
            }

            CoreHelper.StopAndLogStopwatch(sw);

            Updater.RecalculateObjectStates();

            string stri = "object";
            if (prefab.objects.Count == 1)
                stri = prefab.objects[0].name;
            if (prefab.objects.Count > 1)
                stri = prefab.Name;

            EditorManager.inst.DisplayNotification(
                $"Pasted Beatmap Object{(prefab.objects.Count == 1 ? "" : "s")} [ {stri} ] {(regen ? "" : $"and kept Prefab Instance ID")} in {sw.Elapsed}!",
                5f, EditorManager.NotificationType.Success);

            if (select)
            {
                if (prefab.objects.Count > 1 || prefab.prefabObjects.Count > 1)
                    EditorManager.inst.ShowDialog("Multi Object Editor", false);
                else if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                    OpenDialog(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
                else if (EditorTimeline.inst.CurrentSelection.isPrefabObject)
                    PrefabEditor.inst.OpenPrefabDialog();
            }

            RTEditor.inst.ienumRunning = false;
            yield break;
        }

        #endregion

        #region Create New Objects

        public static bool SetToCenterCam => EditorConfig.Instance.CreateObjectsatCameraCenter.Value;

        public void CreateNewObject(Action<TimelineObject> action = null, bool select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(select);

            var bm = timelineObject.GetData<BeatmapObject>();
            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            action?.Invoke(timelineObject);
            Updater.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            OpenDialog(bm);

            if (setHistory)
                EditorManager.inst.history.Add(new History.Command("Create New Object", () => CreateNewObject(action, select, false), DeleteObject(timelineObject).Start));
        }

        public void CreateNewNormalObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.GetData<BeatmapObject>();
            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            OpenDialog(bm);

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Normal Object", () =>
            {
                CreateNewNormalObject(_select, false);
            }, () =>
            {
                inst.StartCoroutine(DeleteObject(timelineObject));
            }));
        }

        public void CreateNewCircleObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.shape = 1;
            bm.shapeOption = 0;
            bm.name = CoreHelper.AprilFools ? "<font=Arrhythmia>bro" : "circle";

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            OpenDialog(bm);

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Normal Circle Object", () =>
            {
                CreateNewCircleObject(_select, false);
            }, () =>
            {
                inst.StartCoroutine(DeleteObject(timelineObject));
            }));
        }

        public void CreateNewTriangleObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.shape = 2;
            bm.shapeOption = 0;
            bm.name = CoreHelper.AprilFools ? "baracuda <i>beat plays</i>" : "triangle";

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            OpenDialog(bm);

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Normal Triangle Object", () =>
            {
                CreateNewTriangleObject(_select, false);
            }, () =>
            {
                inst.StartCoroutine(DeleteObject(timelineObject));
            }));
        }

        public void CreateNewTextObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.shape = 4;
            bm.shapeOption = 0;
            bm.text = CoreHelper.AprilFools ? "Never gonna give you up<br>" +
                                            "Never gonna let you down<br>" +
                                            "Never gonna run around and desert you<br>" +
                                            "Never gonna make you cry<br>" +
                                            "Never gonna say goodbye<br>" +
                                            "Never gonna tell a lie and hurt you" : "text";
            bm.name = CoreHelper.AprilFools ? "Don't look at my text" : "text";
            bm.objectType = ObjectType.Decoration;
            if (CoreHelper.AprilFools)
                bm.StartTime += 1f;

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);

            if (!CoreHelper.AprilFools)
                OpenDialog(bm);

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Normal Text Object", () =>
            {
                CreateNewTextObject(_select, false);
            }, () =>
            {
                inst.StartCoroutine(DeleteObject(timelineObject));
            }));
        }

        public void CreateNewHexagonObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.shape = 5;
            bm.shapeOption = 0;
            bm.name = CoreHelper.AprilFools ? "super" : "hexagon";

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            OpenDialog(bm);

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Normal Hexagon Object", () =>
            {
                CreateNewHexagonObject(_select, false);
            }, () =>
            {
                inst.StartCoroutine(DeleteObject(timelineObject));
            }));
        }

        public void CreateNewHelperObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.name = CoreHelper.AprilFools ? "totally not deprecated object" : "helper";
            bm.objectType = CoreHelper.AprilFools ? ObjectType.Decoration : ObjectType.Helper;
            if (CoreHelper.AprilFools)
                bm.events[3][0].eventValues[1] = 0.65f;

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            OpenDialog(bm);

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Helper Object", () =>
            {
                CreateNewHelperObject(_select, false);
            }, () =>
            {
                inst.StartCoroutine(DeleteObject(timelineObject));
            }));
        }

        public void CreateNewDecorationObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.name = "decoration";
            if (!CoreHelper.AprilFools)
                bm.objectType = ObjectType.Decoration;

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            OpenDialog(bm);

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Decoration Object", () =>
            {
                CreateNewDecorationObject(_select, false);
            }, () =>
            {
                inst.StartCoroutine(DeleteObject(timelineObject));
            }));
        }

        public void CreateNewEmptyObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.name = "empty";
            if (!CoreHelper.AprilFools)
                bm.objectType = ObjectType.Empty;

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y + (CoreHelper.AprilFools ? 999f : 0f);
            }

            Updater.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            OpenDialog(bm);

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Empty Object", () =>
            {
                CreateNewEmptyObject(_select, false);
            }, () =>
            {
                inst.StartCoroutine(DeleteObject(timelineObject));
            }));
        }

        public void CreateNewNoAutokillObject(bool _select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(_select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.name = CoreHelper.AprilFools ? "dead" : "no autokill";
            bm.autoKillType = AutoKillType.OldStyleNoAutokill;

            if (SetToCenterCam)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].eventValues[0] = pos.x;
                bm.events[0][0].eventValues[1] = pos.y;
            }

            Updater.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            OpenDialog(bm);

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New No Autokill Object", () =>
            {
                CreateNewNoAutokillObject(_select, false);
            }, () =>
            {
                inst.StartCoroutine(DeleteObject(timelineObject));
            }));
        }

        public TimelineObject CreateNewDefaultObject(bool _select = true)
        {
            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Can't add objects to level until a level has been loaded!", 2f, EditorManager.NotificationType.Error);
                return null;
            }

            var list = new List<List<BaseEventKeyframe>>
            {
                new List<BaseEventKeyframe>(),
                new List<BaseEventKeyframe>(),
                new List<BaseEventKeyframe>(),
                new List<BaseEventKeyframe>()
            };

            // Position
            list[0].Add(new EventKeyframe(0f, new float[3], new float[4], 0));
            // Scale
            list[1].Add(new EventKeyframe(0f, new float[]  { 1f, 1f }, new float[3], 0));
            // Rotation
            list[2].Add(new EventKeyframe(0f, new float[1], new float[3], 0) { relative = true });
            // Color
            list[3].Add(new EventKeyframe(0f, new float[10]
            {
                0f, // start color slot
                0f, // start opacity
                0f, // start hue
                0f, // start saturation
                0f, // start value
                1f, // end color slot
                0f, // end opacity
                0f, // end hue
                0f, // end saturation
                0f, // end value
            }, new float[4], 0));

            var beatmapObject = new BeatmapObject(true, AudioManager.inst.CurrentAudioSource.time, "", 0, "", list);
            beatmapObject.id = LSText.randomString(16);
            beatmapObject.autoKillType = AutoKillType.LastKeyframeOffset;
            beatmapObject.autoKillOffset = 5f;
            beatmapObject.orderModifiers = EditorConfig.Instance.CreateObjectModifierOrderDefault.Value;

            if (!CoreHelper.AprilFools)
                beatmapObject.editorData.layer = EditorTimeline.inst.Layer;
            beatmapObject.parentType = EditorConfig.Instance.CreateObjectsScaleParentDefault.Value ? "111" : "101";

            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
                EditorTimeline.inst.SetLayer(EditorTimeline.LayerType.Objects);

            int num = GameData.Current.beatmapObjects.FindIndex(x => x.fromPrefab);
            if (num == -1)
                GameData.Current.beatmapObjects.Add(beatmapObject);
            else
                GameData.Current.beatmapObjects.Insert(num, beatmapObject);

            var timelineObject = new TimelineObject(beatmapObject);

            AudioManager.inst.SetMusicTime(AllowTimeExactlyAtStart ? AudioManager.inst.CurrentAudioSource.time : AudioManager.inst.CurrentAudioSource.time + 0.001f);

            if (_select)
                EditorTimeline.inst.SetCurrentObject(timelineObject);

            if (ExampleManager.inst && ExampleManager.inst.Visible && RandomHelper.PercentChance(20))
                ExampleManager.inst.SayDialogue("CreateObject");

            return timelineObject;
        }

        public static BeatmapObject CreateNewBeatmapObject(float _time, bool _add = true)
        {
            var beatmapObject = new BeatmapObject(_time);

            if (!CoreHelper.AprilFools)
                beatmapObject.editorData.layer = EditorTimeline.inst.Layer;

            var positionKeyframe = new EventKeyframe(0f);
            positionKeyframe.SetEventValues(new float[3]);
            positionKeyframe.SetEventRandomValues(new float[4]);

            var scaleKeyframe = new EventKeyframe(0f);
            scaleKeyframe.SetEventValues(1f, 1f);

            var rotationKeyframe = new EventKeyframe(0f) { relative = true };
            rotationKeyframe.SetEventValues(new float[1]);

            var colorKeyframe = new EventKeyframe(0f, new float[10]
            {
                0f, // start color slot
                0f, // start opacity
                0f, // start hue
                0f, // start saturation
                0f, // start value
                1f, // end color slot
                0f, // end opacity
                0f, // end hue
                0f, // end saturation
                0f, // end value
            }, new float[4], 0);

            beatmapObject.events[0].Add(positionKeyframe);
            beatmapObject.events[1].Add(scaleKeyframe);
            beatmapObject.events[2].Add(rotationKeyframe);
            beatmapObject.events[3].Add(colorKeyframe);

            if (_add)
            {
                GameData.Current.beatmapObjects.Add(beatmapObject);

                if (inst)
                {
                    var timelineObject = new TimelineObject(beatmapObject);

                    EditorTimeline.inst.RenderTimelineObject(timelineObject);
                    Updater.UpdateObject(beatmapObject);
                    EditorTimeline.inst.SetCurrentObject(timelineObject);
                }
            }
            return beatmapObject;
        }

        #endregion

        #region Selection

        public IEnumerator GroupSelectKeyframes(bool _add = true)
        {
            if (!EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                yield break;

            var list = EditorTimeline.inst.CurrentSelection.InternalTimelineObjects;

            if (!_add)
                list.ForEach(x => x.Selected = false);

            list.Where(x => RTMath.RectTransformToScreenSpace(ObjEditor.inst.SelectionBoxImage.rectTransform)
            .Overlaps(RTMath.RectTransformToScreenSpace(x.Image.rectTransform))).ToList().ForEach(timelineObject =>
            {
                timelineObject.Selected = true;
                timelineObject.timeOffset = 0f;
                ObjEditor.inst.currentKeyframeKind = timelineObject.Type;
                ObjEditor.inst.currentKeyframe = timelineObject.Index;
            });

            var bm = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
            RenderObjectKeyframesDialog(bm);
            RenderKeyframes(bm);

            yield break;
        }

        public void SetCurrentKeyframe(BeatmapObject beatmapObject, int _keyframe, bool _bringTo = false) => SetCurrentKeyframe(beatmapObject, ObjEditor.inst.currentKeyframeKind, _keyframe, _bringTo, false);

        public void AddCurrentKeyframe(BeatmapObject beatmapObject, int _add, bool _bringTo = false)
        {
            SetCurrentKeyframe(beatmapObject,
                ObjEditor.inst.currentKeyframeKind,
                Mathf.Clamp(ObjEditor.inst.currentKeyframe + _add == int.MaxValue ? 1000000 : _add, 0, beatmapObject.events[ObjEditor.inst.currentKeyframeKind].Count - 1),
                _bringTo);
        }

        public void SetCurrentKeyframe(BeatmapObject beatmapObject, int type, int index, bool _bringTo = false, bool _shift = false)
        {
            var bmTimelineObject = EditorTimeline.inst.GetTimelineObject(beatmapObject);

            if (!ObjEditor.inst.timelineKeyframesDrag)
            {
                Debug.Log($"{ObjEditor.inst.className}Setting Current Keyframe: {type}, {index}");
                if (!_shift && bmTimelineObject.InternalTimelineObjects.Count > 0)
                    bmTimelineObject.InternalTimelineObjects.ForEach(timelineObject => { timelineObject.Selected = false; });

                var kf = GetKeyframe(beatmapObject, type, index);

                kf.Selected = !_shift || !kf.Selected;
            }

            DataManager.inst.UpdateSettingInt("EditorObjKeyframeKind", type);
            DataManager.inst.UpdateSettingInt("EditorObjKeyframe", index);
            ObjEditor.inst.currentKeyframeKind = type;
            ObjEditor.inst.currentKeyframe = index;

            if (_bringTo)
            {
                float value = beatmapObject.events[ObjEditor.inst.currentKeyframeKind][ObjEditor.inst.currentKeyframe].eventTime + beatmapObject.StartTime;

                value = Mathf.Clamp(value, AllowTimeExactlyAtStart ? beatmapObject.StartTime + 0.001f : beatmapObject.StartTime, beatmapObject.StartTime + beatmapObject.GetObjectLifeLength());

                AudioManager.inst.SetMusicTime(Mathf.Clamp(value, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
                AudioManager.inst.CurrentAudioSource.Pause();
                EditorManager.inst.UpdatePlayButton();
            }

            RenderObjectKeyframesDialog(beatmapObject);
        }

        public EventKeyframe AddEvent(BeatmapObject beatmapObject, float time, int type, EventKeyframe _keyframe, bool openDialog)
        {
            var eventKeyframe = EventKeyframe.DeepCopy(_keyframe);
            var t = SettingEditor.inst.SnapActive && EditorConfig.Instance.BPMSnapsKeyframes.Value ? -(beatmapObject.StartTime - RTEditor.SnapToBPM(beatmapObject.StartTime + time)) : time;
            eventKeyframe.eventTime = t;

            if (eventKeyframe.relative)
                for (int i = 0; i < eventKeyframe.eventValues.Length; i++)
                    eventKeyframe.eventValues[i] = 0f;

            eventKeyframe.locked = false;

            beatmapObject.events[type].Add(eventKeyframe);

            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
            Updater.UpdateObject(beatmapObject, "Autokill");
            if (openDialog)
            {
                ResizeKeyframeTimeline(beatmapObject);
                RenderObjectKeyframesDialog(beatmapObject);
            }
            return eventKeyframe;
        }

        #endregion

        #region RefreshObjectGUI

        public static bool UpdateObjects => true;

        public static bool HideVisualElementsWhenObjectIsEmpty { get; set; }

        /// <summary>
        /// Opens the Object Editor dialog.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to edit.</param>
        public void OpenDialog(BeatmapObject beatmapObject)
        {
            if (!EditorManager.inst.hasLoadedLevel || string.IsNullOrEmpty(beatmapObject.id))
                return;

            if (!EditorTimeline.inst.CurrentSelection.isBeatmapObject)
            {
                EditorManager.inst.DisplayNotification("Cannot edit non-object!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (EditorManager.inst.ActiveDialogs.Count > 2 || !EditorManager.inst.ActiveDialogs.Has(x => x.Name == "Object Editor")) // Only need to clear the dialogs if object editor isn't the only active dialog.
            {
                EditorManager.inst.ClearDialogs();
                Dialog.Open();
            }

            if (EditorTimeline.inst.CurrentSelection.ID != beatmapObject.id)
                for (int i = 0; i < ObjEditor.inst.TimelineParents.Count; i++)
                    LSHelpers.DeleteChildren(ObjEditor.inst.TimelineParents[i], true);

            StartCoroutine(RefreshObjectGUI(beatmapObject));
        }

        /// <summary>
        /// Refreshes the Object Editor to the specified BeatmapObject, allowing for any object to be edited from anywhere.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        /// <returns></returns>
        public IEnumerator RefreshObjectGUI(BeatmapObject beatmapObject)
        {
            if (!EditorManager.inst.hasLoadedLevel || string.IsNullOrEmpty(beatmapObject.id))
                yield break;

            EditorTimeline.inst.CurrentSelection = EditorTimeline.inst.GetTimelineObject(beatmapObject);
            EditorTimeline.inst.CurrentSelection.Selected = true;

            RenderID(beatmapObject);
            RenderLDM(beatmapObject);
            RenderName(beatmapObject);
            RenderTags(beatmapObject);
            RenderObjectType(beatmapObject);

            RenderStartTime(beatmapObject);
            RenderAutokill(beatmapObject);

            RenderParent(beatmapObject);

            RenderEmpty(beatmapObject);

            if (!HideVisualElementsWhenObjectIsEmpty || beatmapObject.objectType != ObjectType.Empty)
            {
                RenderOrigin(beatmapObject);
                RenderGradient(beatmapObject);
                RenderShape(beatmapObject);
                RenderDepth(beatmapObject);
            }

            RenderLayers(beatmapObject);
            RenderBin(beatmapObject);

            RenderGameObjectInspector(beatmapObject);

            bool fromPrefab = !string.IsNullOrEmpty(beatmapObject.prefabID);
            Dialog.CollapsePrefabLabel.SetActive(fromPrefab);
            Dialog.CollapsePrefabButton.gameObject.SetActive(fromPrefab);

            SetTimeline(EditorTimeline.inst.CurrentSelection.Zoom, EditorTimeline.inst.CurrentSelection.TimelinePosition);

            RenderObjectKeyframesDialog(beatmapObject);

            try
            {
                if (EditorConfig.Instance.ShowMarkersInObjectEditor.Value)
                    RenderMarkers(beatmapObject);
                else
                    LSHelpers.DeleteChildren(ObjEditor.inst.objTimelineSlider.transform.Find("Markers"));
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Error {ex}");
            }

            if (ObjectModifiersEditor.inst)
                StartCoroutine(ObjectModifiersEditor.inst.RenderModifiers(beatmapObject));

            yield break;
        }

        /// <summary>
        /// Sets specific GUI elements active / inactive depending on settings.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderEmpty(BeatmapObject beatmapObject)
        {
            var active = !HideVisualElementsWhenObjectIsEmpty || beatmapObject.objectType != ObjectType.Empty;
            var shapeTF = Dialog.ShapeTypesParent;
            var shapesLabel = shapeTF.parent.GetChild(shapeTF.GetSiblingIndex() - 2);
            var shapeTFPActive = shapesLabel.gameObject.activeSelf;
            shapeTF.parent.GetChild(shapeTF.GetSiblingIndex() - 2).gameObject.SetActive(active);
            shapeTF.gameObject.SetActive(active);

            try
            {
                shapesLabel.GetChild(0).GetComponent<Text>().text = RTEditor.NotSimple ? "Gradient / Shape" : "Shape";
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
            Dialog.GradientParent.gameObject.SetActive(active && RTEditor.NotSimple);

            Dialog.ShapeOptionsParent.gameObject.SetActive(active);
            Dialog.DepthParent.gameObject.SetActive(active);
            var depthTf = Dialog.DepthField.transform.parent;
            depthTf.parent.GetChild(depthTf.GetSiblingIndex() - 1).gameObject.SetActive(active);
            depthTf.gameObject.SetActive(RTEditor.NotSimple && active);
            Dialog.DepthSlider.transform.AsRT().sizeDelta = new Vector2(RTEditor.NotSimple ? 352f : 292f, 32f);

            var renderTypeTF = Dialog.RenderTypeDropdown.transform;
            renderTypeTF.parent.GetChild(renderTypeTF.GetSiblingIndex() - 1).gameObject.SetActive(active && RTEditor.ShowModdedUI);
            renderTypeTF.gameObject.SetActive(active && RTEditor.ShowModdedUI);

            var originTF = Dialog.OriginParent;
            originTF.parent.GetChild(originTF.GetSiblingIndex() - 1).gameObject.SetActive(active);
            originTF.gameObject.SetActive(active);

            var tagsParent = ObjEditor.inst.ObjectView.transform.Find("Tags Scroll View");
            tagsParent.parent.GetChild(tagsParent.GetSiblingIndex() - 1).gameObject.SetActive(RTEditor.ShowModdedUI);
            bool tagsActive = tagsParent.gameObject.activeSelf;
            tagsParent.gameObject.SetActive(RTEditor.ShowModdedUI);

            ldmLabel.gameObject.SetActive(RTEditor.ShowModdedUI);
            ldmToggle.gameObject.SetActive(RTEditor.ShowModdedUI);

            ObjectModifiersEditor.inst.modifiersLabel.gameObject.SetActive(RTEditor.ShowModdedUI);
            ObjectModifiersEditor.inst.intVariable.gameObject.SetActive(RTEditor.ShowModdedUI);
            ObjectModifiersEditor.inst.ignoreToggle.gameObject.SetActive(RTEditor.ShowModdedUI);
            ObjectModifiersEditor.inst.orderToggle.gameObject.SetActive(RTEditor.ShowModdedUI);

            var activeModifiers = ObjectModifiersEditor.inst.activeToggle;

            if (!RTEditor.ShowModdedUI)
                activeModifiers.isOn = false;

            activeModifiers.gameObject.SetActive(RTEditor.ShowModdedUI);

            if (active && !shapeTFPActive)
            {
                RenderOrigin(beatmapObject);
                RenderShape(beatmapObject);
                RenderDepth(beatmapObject);
            }

            if (RTEditor.ShowModdedUI && !tagsActive)
            {
                RenderLDM(beatmapObject);
                RenderTags(beatmapObject);
            }
        }

        /// <summary>
        /// Renders the ID Text.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderID(BeatmapObject beatmapObject)
        {
            Dialog.IDText.text = $"ID: {beatmapObject.id}";

            var clickable = Dialog.IDBase.gameObject.GetOrAddComponent<Clickable>();

            clickable.onClick = pointerEventData =>
            {
                EditorManager.inst.DisplayNotification($"Copied ID from {beatmapObject.name}!", 2f, EditorManager.NotificationType.Success);
                LSText.CopyToClipboard(beatmapObject.id);
            };
        }

        /// <summary>
        /// Renders the LDM Toggle.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderLDM(BeatmapObject beatmapObject)
        {
            ldmToggle.onValueChanged.ClearAll();
            ldmToggle.isOn = beatmapObject.LDM;
            ldmToggle.onValueChanged.AddListener(_val =>
            {
                beatmapObject.LDM = _val;
                Updater.UpdateObject(beatmapObject);
            });
        }

        /// <summary>
        /// Renders the Name InputField.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderName(BeatmapObject beatmapObject)
        {
            var name = Dialog.NameField;

            // Allows for left / right flipping.
            if (!name.GetComponent<InputFieldSwapper>() && name.gameObject)
            {
                var t = name.gameObject.AddComponent<InputFieldSwapper>();
                t.Init(name, InputFieldSwapper.Type.String);
            }

            EditorHelper.AddInputFieldContextMenu(name);

            name.onValueChanged.ClearAll();
            name.text = beatmapObject.name;
            name.onValueChanged.AddListener(_val =>
            {
                beatmapObject.name = _val;

                // Since name has no effect on the physical object, we will only need to update the timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
            });
        }

        /// <summary>
        /// Renders the Tags list.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderTags(BeatmapObject beatmapObject)
        {
            var tagsParent = Dialog.TagsContent;

            LSHelpers.DeleteChildren(tagsParent);

            if (!RTEditor.ShowModdedUI)
                return;

            int num = 0;
            foreach (var tag in beatmapObject.tags)
            {
                int index = num;
                var gameObject = EditorPrefabHolder.Instance.Tag.Duplicate(tagsParent, index.ToString());
                gameObject.transform.localScale = Vector3.one;
                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.onValueChanged.ClearAll();
                input.text = tag;
                input.onValueChanged.AddListener(_val => beatmapObject.tags[index] = _val);

                var inputFieldSwapper = gameObject.AddComponent<InputFieldSwapper>();
                inputFieldSwapper.Init(input, InputFieldSwapper.Type.String);

                var deleteStorage = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.ClearAll();
                deleteStorage.button.onClick.AddListener(() =>
                {
                    beatmapObject.tags.RemoveAt(index);
                    RenderTags(beatmapObject);
                });

                EditorHelper.AddInputFieldContextMenu(input);

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
                beatmapObject.tags.Add("New Tag");
                RenderTags(beatmapObject);
            });

            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text, true);
        }

        /// <summary>
        /// Renders the ObjectType Dropdown.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderObjectType(BeatmapObject beatmapObject)
        {
            Dialog.ObjectTypeDropdown.options =
                EditorConfig.Instance.EditorComplexity.Value == Complexity.Advanced ?
                    CoreHelper.StringToOptionData("Normal", "Helper", "Decoration", "Empty", "Solid") :
                    CoreHelper.StringToOptionData("Normal", "Helper", "Decoration", "Empty"); // don't show solid object type 
            Dialog.ObjectTypeDropdown.onValueChanged.ClearAll();
            Dialog.ObjectTypeDropdown.value = Mathf.Clamp((int)beatmapObject.objectType, 0, Dialog.ObjectTypeDropdown.options.Count - 1);
            Dialog.ObjectTypeDropdown.onValueChanged.AddListener(_val =>
            {
                beatmapObject.objectType = (ObjectType)_val;
                RenderGameObjectInspector(beatmapObject);
                // ObjectType affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject);

                RenderEmpty(beatmapObject);
            });
        }

        /// <summary>
        /// Renders all StartTime UI.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderStartTime(BeatmapObject beatmapObject)
        {
            var startTimeField = Dialog.StartTimeField;

            startTimeField.lockToggle.onValueChanged.ClearAll();
            startTimeField.lockToggle.isOn = beatmapObject.editorData.locked;
            startTimeField.lockToggle.onValueChanged.AddListener(_val =>
            {
                beatmapObject.editorData.locked = _val;

                // Since locking has no effect on the physical object, we will only need to update the timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
            });

            startTimeField.inputField.onValueChanged.ClearAll();
            startTimeField.inputField.text = beatmapObject.StartTime.ToString();
            startTimeField.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    beatmapObject.StartTime = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                    ResizeKeyframeTimeline(beatmapObject);

                    // StartTime affects both physical object and timeline object.
                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "StartTime");
                }
            });

            TriggerHelper.AddEventTriggers(Dialog.StartTimeField.gameObject, TriggerHelper.ScrollDelta(startTimeField.inputField, max: AudioManager.inst.CurrentAudioSource.clip.length));

            startTimeField.leftGreaterButton.onClick.ClearAll();
            startTimeField.leftGreaterButton.interactable = (beatmapObject.StartTime > 0f);
            startTimeField.leftGreaterButton.onClick.AddListener(() =>
            {
                float moveTime = beatmapObject.StartTime - 1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                startTimeField.inputField.text = moveTime.ToString();

                ResizeKeyframeTimeline(beatmapObject);

                // StartTime affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject, "StartTime");

                ResizeKeyframeTimeline(beatmapObject);
            });

            startTimeField.leftButton.onClick.ClearAll();
            startTimeField.leftButton.interactable = (beatmapObject.StartTime > 0f);
            startTimeField.leftButton.onClick.AddListener(() =>
            {
                float moveTime = beatmapObject.StartTime - 0.1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                startTimeField.inputField.text = moveTime.ToString();

                ResizeKeyframeTimeline(beatmapObject);

                // StartTime affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject, "StartTime");

                ResizeKeyframeTimeline(beatmapObject);
            });

            startTimeField.middleButton.onClick.ClearAll();
            startTimeField.middleButton.onClick.AddListener(() =>
            {
                startTimeField.inputField.text = EditorManager.inst.CurrentAudioPos.ToString();

                ResizeKeyframeTimeline(beatmapObject);

                // StartTime affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject, "StartTime");

                ResizeKeyframeTimeline(beatmapObject);
            });

            startTimeField.rightButton.onClick.ClearAll();
            startTimeField.rightButton.onClick.AddListener(() =>
            {
                float moveTime = beatmapObject.StartTime + 0.1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                startTimeField.inputField.text = moveTime.ToString();

                ResizeKeyframeTimeline(beatmapObject);

                // StartTime affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject, "StartTime");

                ResizeKeyframeTimeline(beatmapObject);
            });

            startTimeField.rightGreaterButton.onClick.ClearAll();
            startTimeField.rightGreaterButton.onClick.AddListener(() =>
            {
                float moveTime = beatmapObject.StartTime + 1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                startTimeField.inputField.text = moveTime.ToString();

                ResizeKeyframeTimeline(beatmapObject);

                // StartTime affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject, "StartTime");

                ResizeKeyframeTimeline(beatmapObject);
            });
        }

        /// <summary>
        /// Renders all Autokill UI.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderAutokill(BeatmapObject beatmapObject)
        {
            Dialog.AutokillDropdown.onValueChanged.ClearAll();
            Dialog.AutokillDropdown.value = (int)beatmapObject.autoKillType;
            Dialog.AutokillDropdown.onValueChanged.AddListener(_val =>
            {
                beatmapObject.autoKillType = (AutoKillType)_val;
                // AutoKillType affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject, "Autokill");
                ResizeKeyframeTimeline(beatmapObject);
                RenderAutokill(beatmapObject);
            });

            if (beatmapObject.autoKillType == AutoKillType.FixedTime ||
                beatmapObject.autoKillType == AutoKillType.SongTime ||
                beatmapObject.autoKillType == AutoKillType.LastKeyframeOffset)
            {
                Dialog.AutokillField.gameObject.SetActive(true);

                Dialog.AutokillField.onValueChanged.ClearAll();
                Dialog.AutokillField.text = beatmapObject.autoKillOffset.ToString();
                Dialog.AutokillField.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        if (beatmapObject.autoKillType == AutoKillType.SongTime)
                        {
                            float startTime = beatmapObject.StartTime;
                            if (num < startTime)
                                num = startTime + 0.1f;
                        }

                        if (num < 0f)
                            num = 0f;

                        beatmapObject.autoKillOffset = num;

                        // AutoKillType affects both physical object and timeline object.
                        EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                        if (UpdateObjects)
                            Updater.UpdateObject(beatmapObject, "Autokill");
                        ResizeKeyframeTimeline(beatmapObject);
                    }
                });

                Dialog.AutokillSetButton.gameObject.SetActive(true);
                Dialog.AutokillSetButton.onClick.ClearAll();
                Dialog.AutokillSetButton.onClick.AddListener(() =>
                {
                    float num = 0f;

                    if (beatmapObject.autoKillType == AutoKillType.SongTime)
                        num = AudioManager.inst.CurrentAudioSource.time;
                    else num = AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime;

                    if (num < 0f)
                        num = 0f;

                    Dialog.AutokillField.text = num.ToString();
                });

                // Add Scrolling for easy changing of values.
                TriggerHelper.AddEventTriggers(Dialog.AutokillField.gameObject, TriggerHelper.ScrollDelta(Dialog.AutokillField, 0.1f, 10f, 0f, float.PositiveInfinity));
            }
            else
            {
                Dialog.AutokillField.gameObject.SetActive(false);
                Dialog.AutokillField.onValueChanged.ClearAll();
                Dialog.AutokillSetButton.gameObject.SetActive(false);
                Dialog.AutokillSetButton.onClick.ClearAll();
            }

            Dialog.CollapseToggle.onValueChanged.ClearAll();
            Dialog.CollapseToggle.isOn = beatmapObject.editorData.collapse;
            Dialog.CollapseToggle.onValueChanged.AddListener(_val =>
            {
                beatmapObject.editorData.collapse = _val;

                // Since autokill collapse has no affect on the physical object, we will only need to update the timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
            });
        }

        /// <summary>
        /// Renders all Parent UI.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderParent(BeatmapObject beatmapObject)
        {
            string parent = beatmapObject.parent;
            
            Dialog.ParentButton.transform.AsRT().sizeDelta = new Vector2(!string.IsNullOrEmpty(parent) ? 201f : 241f, 32f);

            Dialog.ParentSearchButton.onClick.ClearAll();
            Dialog.ParentClearButton.onClick.ClearAll();
            Dialog.ParentPickerButton.onClick.ClearAll();

            Dialog.ParentSearchButton.onClick.AddListener(EditorManager.inst.OpenParentPopup);
            var parentSearchContextMenu = Dialog.ParentSearchButton.gameObject.GetOrAddComponent<ContextClickable>();
            parentSearchContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                RTEditor.inst.ShowContextMenu(
                    new ButtonFunction("Open Parent Popup", EditorManager.inst.OpenParentPopup),
                    new ButtonFunction("Parent to Camera", () =>
                    {
                        beatmapObject.parent = BeatmapObject.CAMERA_PARENT;
                        Updater.UpdateObject(beatmapObject);
                        RenderParent(beatmapObject);
                    })
                    );
            };

            Dialog.ParentPickerButton.onClick.AddListener(() => RTEditor.inst.parentPickerEnabled = true);

            Dialog.ParentClearButton.gameObject.SetActive(!string.IsNullOrEmpty(parent));

            Dialog.ParentSettingsParent.transform.AsRT().sizeDelta = new Vector2(351f, RTEditor.ShowModdedUI ? 152f : 112f);

            var parentContextMenu = Dialog.ParentButton.gameObject.GetOrAddComponent<ContextClickable>();
            parentContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                var list = new List<ButtonFunction>();

                if (!string.IsNullOrEmpty(beatmapObject.parent))
                {
                    var parentChain = beatmapObject.GetParentChain();
                    if (parentChain.Count > 0)
                        list.Add(new ButtonFunction("View Parent Chain", () =>
                        {
                            RTEditor.inst.ShowObjectSearch(x => EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.GetTimelineObject(x), Input.GetKey(KeyCode.LeftControl)), beatmapObjects: parentChain);
                        }));
                }

                if (GameData.Current.beatmapObjects.TryFindAll(x => x.parent == beatmapObject.id, out List<BeatmapObject> findAll))
                {
                    var childTree = beatmapObject.GetChildTree();
                    if (childTree.Count > 0)
                        list.Add(new ButtonFunction("View Child Tree", () =>
                        {
                            RTEditor.inst.ShowObjectSearch(x => EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.GetTimelineObject(x), Input.GetKey(KeyCode.LeftControl)), beatmapObjects: childTree);
                        }));
                }

                RTEditor.inst.ShowContextMenu(list);
            };

            if (string.IsNullOrEmpty(parent))
            {
                Dialog.ParentButton.button.interactable = false;
                Dialog.ParentMoreButton.interactable = false;
                Dialog.ParentSettingsParent.gameObject.SetActive(false);
                Dialog.ParentButton.text.text = "No Parent Object";

                Dialog.ParentInfo.tooltipLangauges[0].hint = string.IsNullOrEmpty(parent) ? "Object not parented." : "No parent found.";
                Dialog.ParentButton.button.onClick.ClearAll();
                Dialog.ParentMoreButton.onClick.ClearAll();

                return;
            }

            string p = null;

            if (GameData.Current.beatmapObjects.TryFindIndex(x => x.id == parent, out int pa))
            {
                p = GameData.Current.beatmapObjects[pa].name;
                Dialog.ParentInfo.tooltipLangauges[0].hint = string.Format("Parent chain count: [{0}]\n(Inclusive)", beatmapObject.GetParentChain().Count);
            }
            else if (parent == BeatmapObject.CAMERA_PARENT)
            {
                p = "[CAMERA]";
                Dialog.ParentInfo.tooltipLangauges[0].hint = "Object parented to the camera.";
            }

            Dialog.ParentButton.button.interactable = p != null;
            Dialog.ParentMoreButton.interactable = p != null;

            Dialog.ParentSettingsParent.gameObject.SetActive(p != null && ObjEditor.inst.advancedParent);

            Dialog.ParentClearButton.onClick.AddListener(() =>
            {
                beatmapObject.parent = "";

                // Since parent has no affect on the timeline object, we will only need to update the physical object.
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject, "Parent");

                RenderParent(beatmapObject);
            });

            if (p == null)
            {
                Dialog.ParentButton.text.text = "No Parent Object";
                Dialog.ParentInfo.tooltipLangauges[0].hint = string.IsNullOrEmpty(parent) ? "Object not parented." : "No parent found.";
                Dialog.ParentButton.button.onClick.ClearAll();
                Dialog.ParentMoreButton.onClick.ClearAll();

                return;
            }

            Dialog.ParentButton.text.text = p;

            Dialog.ParentButton.button.onClick.ClearAll();
            Dialog.ParentButton.button.onClick.AddListener(() =>
            {
                if (GameData.Current.beatmapObjects.Find(x => x.id == parent) != null &&
                    parent != BeatmapObject.CAMERA_PARENT &&
                    EditorTimeline.inst.timelineObjects.TryFind(x => x.ID == parent, out TimelineObject timelineObject))

                    EditorTimeline.inst.SetCurrentObject(timelineObject);
                else if (parent == BeatmapObject.CAMERA_PARENT)
                {
                    EditorTimeline.inst.SetLayer(EditorTimeline.LayerType.Events);
                    EventEditor.inst.SetCurrentEvent(0, GameData.Current.ClosestEventKeyframe(0));
                }
            });

            Dialog.ParentMoreButton.onClick.ClearAll();
            Dialog.ParentMoreButton.onClick.AddListener(() =>
            {
                ObjEditor.inst.advancedParent = !ObjEditor.inst.advancedParent;
                Dialog.ParentSettingsParent.gameObject.SetActive(ObjEditor.inst.advancedParent);
            });
            Dialog.ParentSettingsParent.gameObject.SetActive(ObjEditor.inst.advancedParent);

            Dialog.ParentDesyncToggle.onValueChanged.ClearAll();
            Dialog.ParentDesyncToggle.gameObject.SetActive(RTEditor.ShowModdedUI && EditorConfig.Instance.ShowExperimental.Value);
            if (RTEditor.ShowModdedUI && EditorConfig.Instance.ShowExperimental.Value)
            {
                Dialog.ParentDesyncToggle.isOn = beatmapObject.desync;
                Dialog.ParentDesyncToggle.onValueChanged.AddListener(_val =>
                {
                    beatmapObject.desync = _val;
                    Updater.UpdateObject(beatmapObject);
                });
            }

            for (int i = 0; i < Dialog.ParentSettings.Count; i++)
            {
                var parentSetting = Dialog.ParentSettings[i];

                var index = i;

                // Parent Type
                parentSetting.activeToggle.onValueChanged.ClearAll();
                parentSetting.activeToggle.isOn = beatmapObject.GetParentType(i);
                parentSetting.activeToggle.onValueChanged.AddListener(_val =>
                {
                    beatmapObject.SetParentType(index, _val);

                    // Since updating parent type has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects && !string.IsNullOrEmpty(beatmapObject.parent) && beatmapObject.parent != BeatmapObject.CAMERA_PARENT)
                        Updater.UpdateObject(beatmapObject.Parent);
                    else if (UpdateObjects && beatmapObject.parent == BeatmapObject.CAMERA_PARENT)
                        Updater.UpdateObject(beatmapObject);
                });

                // Parent Offset
                var lel = parentSetting.offsetField.GetComponent<LayoutElement>();
                lel.minWidth = RTEditor.ShowModdedUI ? 64f : 128f;
                lel.preferredWidth = RTEditor.ShowModdedUI ? 64f : 128f;
                parentSetting.offsetField.onValueChanged.ClearAll();
                parentSetting.offsetField.text = beatmapObject.getParentOffset(i).ToString();
                parentSetting.offsetField.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        beatmapObject.SetParentOffset(index, num);

                        // Since updating parent type has no affect on the timeline object, we will only need to update the physical object.
                        if (UpdateObjects && !string.IsNullOrEmpty(beatmapObject.parent) && beatmapObject.parent != BeatmapObject.CAMERA_PARENT)
                            Updater.UpdateObject(beatmapObject.Parent);
                        else if (UpdateObjects && beatmapObject.parent == BeatmapObject.CAMERA_PARENT)
                            Updater.UpdateObject(beatmapObject);
                    }
                });

                TriggerHelper.AddEventTriggers(parentSetting.offsetField.gameObject, TriggerHelper.ScrollDelta(parentSetting.offsetField));

                parentSetting.additiveToggle.onValueChanged.ClearAll();
                parentSetting.parallaxField.onValueChanged.ClearAll();
                parentSetting.additiveToggle.gameObject.SetActive(RTEditor.ShowModdedUI);
                parentSetting.parallaxField.gameObject.SetActive(RTEditor.ShowModdedUI);

                if (!RTEditor.ShowModdedUI)
                    continue;

                parentSetting.additiveToggle.isOn = beatmapObject.parentAdditive[i] == '1';
                parentSetting.additiveToggle.onValueChanged.AddListener(_val =>
                {
                    beatmapObject.SetParentAdditive(index, _val);
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject);
                });
                parentSetting.parallaxField.text = beatmapObject.parallaxSettings[index].ToString();
                parentSetting.parallaxField.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        beatmapObject.parallaxSettings[index] = num;

                        // Since updating parent type has no affect on the timeline object, we will only need to update the physical object.
                        if (UpdateObjects)
                            Updater.UpdateObject(beatmapObject);
                    }
                });

                TriggerHelper.AddEventTriggers(parentSetting.parallaxField.gameObject, TriggerHelper.ScrollDelta(parentSetting.parallaxField));
            }
        }

        /// <summary>
        /// Renders the Origin InputFields.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderOrigin(BeatmapObject beatmapObject)
        {
            // Reimplemented origin toggles for Simple Editor Complexity.
            float[] originDefaultPositions = new float[] { 0f, -0.5f, 0f, 0.5f };
            for (int i = 1; i <= 3; i++)
            {
                int index = i;
                var toggle = Dialog.OriginXToggles[i - 1];
                toggle.onValueChanged.ClearAll();
                toggle.isOn = beatmapObject.origin.x == originDefaultPositions[i];
                toggle.onValueChanged.AddListener(_val =>
                {
                    if (!_val)
                        return;

                    switch (index)
                    {
                        case 1:
                            beatmapObject.origin.x = -0.5f;

                            // Since origin has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Origin");
                            return;
                        case 2:
                            beatmapObject.origin.x = 0f;

                            // Since origin has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Origin");
                            return;
                        case 3:
                            beatmapObject.origin.x = 0.5f;

                            // Since origin has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Origin");
                            break;
                        default:
                            return;
                    }
                });

                var originContextMenu = toggle.gameObject.GetOrAddComponent<ContextClickable>();

                originContextMenu.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    OriginContextMenu(beatmapObject);
                };
            }
            for (int i = 1; i <= 3; i++)
            {
                int index = i;
                var toggle = Dialog.OriginYToggles[i - 1];
                toggle.onValueChanged.ClearAll();
                toggle.isOn = beatmapObject.origin.y == originDefaultPositions[i];
                toggle.onValueChanged.AddListener(_val =>
                {
                    if (!_val)
                        return;

                    switch (index)
                    {
                        case 1:
                            beatmapObject.origin.y = -0.5f;

                            // Since origin has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Origin");
                            return;
                        case 2:
                            beatmapObject.origin.y = 0f;

                            // Since origin has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Origin");
                            return;
                        case 3:
                            beatmapObject.origin.y = 0.5f;

                            // Since origin has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Origin");
                            break;
                        default:
                            return;
                    }
                });

                var originContextMenu = toggle.gameObject.GetOrAddComponent<ContextClickable>();

                originContextMenu.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    OriginContextMenu(beatmapObject);
                };
            }

            if (!Dialog.OriginXField.inputField.gameObject.GetComponent<InputFieldSwapper>())
            {
                var ifh = Dialog.OriginXField.inputField.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(Dialog.OriginXField.inputField, InputFieldSwapper.Type.Num);
            }

            Dialog.OriginXField.inputField.onValueChanged.ClearAll();
            Dialog.OriginXField.inputField.text = beatmapObject.origin.x.ToString();
            Dialog.OriginXField.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    beatmapObject.origin.x = num;

                    // Since origin has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Origin");
                }
            });

            if (!Dialog.OriginYField.inputField.gameObject.GetComponent<InputFieldSwapper>())
            {
                var ifh = Dialog.OriginYField.inputField.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(Dialog.OriginYField.inputField, InputFieldSwapper.Type.Num);
            }

            Dialog.OriginYField.inputField.onValueChanged.ClearAll();
            Dialog.OriginYField.inputField.text = beatmapObject.origin.y.ToString();
            Dialog.OriginYField.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    beatmapObject.origin.y = num;

                    // Since origin has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Origin");
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.OriginXField);
            TriggerHelper.IncreaseDecreaseButtons(Dialog.OriginYField);

            TriggerHelper.AddEventTriggers(Dialog.OriginXField.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.OriginXField.inputField, multi: true), TriggerHelper.ScrollDeltaVector2(Dialog.OriginXField.inputField, Dialog.OriginYField.inputField, 0.1f, 10f));
            TriggerHelper.AddEventTriggers(Dialog.OriginYField.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.OriginYField.inputField, multi: true), TriggerHelper.ScrollDeltaVector2(Dialog.OriginXField.inputField, Dialog.OriginYField.inputField, 0.1f, 10f));

            var originXContextMenu = Dialog.OriginXField.inputField.gameObject.GetOrAddComponent<ContextClickable>();

            originXContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                OriginContextMenu(beatmapObject);
            };

            var originYContextMenu = Dialog.OriginYField.inputField.gameObject.GetOrAddComponent<ContextClickable>();

            originYContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                OriginContextMenu(beatmapObject);
            };
        }

        void OriginContextMenu(BeatmapObject beatmapObject)
        {
            RTEditor.inst.ShowContextMenu(
                new ButtonFunction("Center", () =>
                {
                    beatmapObject.origin = Vector2.zero;
                    // Since origin has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Origin");
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Top", () =>
                {
                    beatmapObject.origin.y = -0.5f;
                    // Since origin has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Origin");
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Bottom", () =>
                {
                    beatmapObject.origin.y = 0.5f;
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Origin");
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Left", () =>
                {
                    beatmapObject.origin.x = -0.5f;
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Origin");
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Right", () =>
                {
                    beatmapObject.origin.x = 0.5f;
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Origin");
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Top (Triangle)", () =>
                {
                    beatmapObject.origin.y = -0.575f;
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Origin");
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Bottom (Triangle)", () =>
                {
                    beatmapObject.origin.y = 0.2875f;
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Origin");
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Left (Triangle)", () =>
                {
                    beatmapObject.origin.x = -0.497964993f;
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Origin");
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Right (Triangle)", () =>
                {
                    beatmapObject.origin.x = 0.497964993f;
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Origin");
                    RenderOrigin(beatmapObject);
                })
                );
        }

        public void RenderGradient(BeatmapObject beatmapObject)
        {
            for (int i = 0; i < Dialog.GradientToggles.Count; i++)
            {
                var index = i;
                var toggle = Dialog.GradientToggles[i];
                toggle.onValueChanged.ClearAll();
                toggle.isOn = index == (int)beatmapObject.gradientType;
                toggle.onValueChanged.AddListener(_val =>
                {
                    beatmapObject.gradientType = (BeatmapObject.GradientType)index;

                    if (beatmapObject.gradientType != BeatmapObject.GradientType.Normal && (beatmapObject.shape == 4 || beatmapObject.shape == 6 || beatmapObject.shape == 10))
                    {
                        beatmapObject.shape = 0;
                        beatmapObject.shapeOption = 0;
                        RenderShape(beatmapObject);
                    }

                    if (!RTEditor.ShowModdedUI)
                    {
                        for (int i = 0; i < beatmapObject.events[3].Count; i++)
                            beatmapObject.events[3][i].eventValues[6] = 10f;
                    }

                    // Since shape has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject);

                    RenderGradient(beatmapObject);
                    inst.RenderObjectKeyframesDialog(beatmapObject);
                });
            }
        }

        /// <summary>
        /// Ensures a toggle list ends with a non-toggle game object.
        /// </summary>
        /// <param name="parent">The parent for the end non-toggle.</param>
        public void LastGameObject(Transform parent)
        {
            var gameObject = new GameObject("GameObject");
            gameObject.transform.SetParent(parent);
            gameObject.transform.localScale = Vector3.one;

            var rectTransform = gameObject.AddComponent<RectTransform>();

            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(0f, 32f);

            var layoutElement = gameObject.AddComponent<LayoutElement>();
            layoutElement.layoutPriority = 1;
            layoutElement.preferredWidth = 1000f;
        }

        bool updatedShapes = false;
        public List<Toggle> shapeToggles = new List<Toggle>();
        public List<List<Toggle>> shapeOptionToggles = new List<List<Toggle>>();

        /// <summary>
        /// Renders the Shape ToggleGroup.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderShape(BeatmapObject beatmapObject)
        {
            var shape = Dialog.ShapeTypesParent;
            var shapeSettings = Dialog.ShapeOptionsParent;

            LSHelpers.SetActiveChildren(shapeSettings, false);

            if (beatmapObject.shape >= shapeSettings.childCount)
            {
                Debug.Log($"{ObjEditor.inst.className}Somehow, the object ended up being at a higher shape than normal.");
                beatmapObject.shape = shapeSettings.childCount - 1;
                // Since shape has no affect on the timeline object, we will only need to update the physical object.
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject, "Shape");

                RenderShape(beatmapObject);
            }

            shapeSettings.AsRT().sizeDelta = new Vector2(351f, beatmapObject.shape == 4 ? 74f : 32f);
            shapeSettings.GetChild(4).AsRT().sizeDelta = new Vector2(351f, beatmapObject.shape == 4 ? 74f : 32f);

            shapeSettings.GetChild(beatmapObject.shape).gameObject.SetActive(true);

            int num = 0;
            foreach (var toggle in shapeToggles)
            {
                int index = num;
                toggle.onValueChanged.ClearAll();
                toggle.isOn = beatmapObject.shape == index;
                toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes.Length);

                if (RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes.Length)
                    toggle.onValueChanged.AddListener(_val =>
                    {
                        beatmapObject.shape = index;
                        beatmapObject.shapeOption = 0;

                        if (beatmapObject.gradientType != BeatmapObject.GradientType.Normal && (index == 4 || index == 6 || index == 10))
                        {
                            beatmapObject.shape = 0;
                        }

                        // Since shape has no affect on the timeline object, we will only need to update the physical object.
                        if (UpdateObjects)
                            Updater.UpdateObject(beatmapObject, "Shape");

                        RenderShape(beatmapObject);
                    });

                num++;
            }

            switch (beatmapObject.shape)
            {
                case 4:
                    {
                        var textIF = shapeSettings.Find("5").GetComponent<InputField>();
                        textIF.textComponent.alignment = TextAnchor.UpperLeft;
                        textIF.GetPlaceholderText().alignment = TextAnchor.UpperLeft;
                        textIF.GetPlaceholderText().text = "Enter text...";
                        textIF.lineType = InputField.LineType.MultiLineNewline;

                        textIF.onValueChanged.ClearAll();
                        textIF.text = beatmapObject.text;
                        textIF.onValueChanged.AddListener(_val =>
                        {
                            beatmapObject.text = _val;

                            // Since text has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Shape");
                        });

                        break;
                    }
                case 6:
                    {
                        var select = shapeSettings.Find("7/select").GetComponent<Button>();
                        select.onClick.ClearAll();
                        var selectContextClickable = select.gameObject.GetOrAddComponent<ContextClickable>();
                        selectContextClickable.onClick = eventData =>
                        {
                            if (eventData.button == PointerEventData.InputButton.Right)
                            {
                                RTEditor.inst.ShowContextMenu(
                                    new ButtonFunction($"Use {RTEditor.SYSTEM_BROWSER}", () => OpenImageSelector(beatmapObject)),
                                    new ButtonFunction($"Use {RTEditor.EDITOR_BROWSER}", () =>
                                    {
                                        var editorPath = RTFile.RemoveEndSlash(RTEditor.inst.CurrentLevel.path);
                                        EditorManager.inst.ShowDialog("Browser Popup");
                                        RTFileBrowser.inst.UpdateBrowserFile(new string[] { FileFormat.PNG.Dot(), FileFormat.JPG.Dot() }, file =>
                                        {
                                            SelectImage(file, beatmapObject);
                                            EditorManager.inst.HideDialog("Browser Popup");
                                        });
                                    }),
                                    new ButtonFunction(true),
                                    new ButtonFunction("Remove Image", () =>
                                    {
                                        beatmapObject.text = string.Empty;

                                // Since setting image has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                            Updater.UpdateObject(beatmapObject, "Shape");

                                        RenderShape(beatmapObject);
                                    }),
                                    new ButtonFunction("Delete Image", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete the image and remove it from the image object?", () =>
                                    {
                                        RTFile.DeleteFile(RTFile.CombinePaths(RTEditor.inst.CurrentLevel.path, beatmapObject.text));

                                        beatmapObject.text = string.Empty;

                                // Since setting image has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                            Updater.UpdateObject(beatmapObject, "Shape");

                                        RenderShape(beatmapObject);
                                    }, RTEditor.inst.HideWarningPopup))
                                    );
                                return;
                            }
                            OpenImageSelector(beatmapObject);
                        };
                        shapeSettings.Find("7/text").GetComponent<Text>().text = string.IsNullOrEmpty(beatmapObject.text) ? "No image selected" : beatmapObject.text;

                        // Sets Image Data for transfering of Image Objects between levels.
                        var dataText = shapeSettings.Find("7/set/Text").GetComponent<Text>();
                        dataText.text = !AssetManager.SpriteAssets.ContainsKey(beatmapObject.text) ? "Set Data" : "Clear Data";
                        var set = shapeSettings.Find("7/set").GetComponent<Button>();
                        set.onClick.ClearAll();
                        set.onClick.AddListener(() =>
                        {
                            var assetExists = AssetManager.SpriteAssets.ContainsKey(beatmapObject.text);
                            if (!assetExists)
                            {
                                var regex = new Regex(@"img\((.*?)\)");
                                var match = regex.Match(beatmapObject.text);

                                var path = match.Success ? RTFile.CombinePaths(RTFile.BasePath, match.Groups[1].ToString()) : RTFile.CombinePaths(RTFile.BasePath, beatmapObject.text);

                                if (RTFile.FileExists(path))
                                {
                                    var imageData = File.ReadAllBytes(path);

                                    var texture2d = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                                    texture2d.LoadImage(imageData);

                                    texture2d.wrapMode = TextureWrapMode.Clamp;
                                    texture2d.filterMode = FilterMode.Point;
                                    texture2d.Apply();

                                    AssetManager.SpriteAssets[beatmapObject.text] = SpriteHelper.CreateSprite(texture2d);
                                }
                                else
                                {
                                    var imageData = ArcadeManager.inst.defaultImage.texture.EncodeToPNG();

                                    var texture2d = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                                    texture2d.LoadImage(imageData);

                                    texture2d.wrapMode = TextureWrapMode.Clamp;
                                    texture2d.filterMode = FilterMode.Point;
                                    texture2d.Apply();

                                    AssetManager.SpriteAssets[beatmapObject.text] = SpriteHelper.CreateSprite(texture2d);
                                }

                                Updater.UpdateObject(beatmapObject);
                            }
                            else
                            {
                                AssetManager.SpriteAssets.Remove(beatmapObject.text);

                                Updater.UpdateObject(beatmapObject);
                            }

                            dataText.text = !assetExists ? "Set Data" : "Clear Data";
                        });

                        break;
                    }
                default:
                    {
                        num = 0;
                        foreach (var toggle in shapeOptionToggles[beatmapObject.shape])
                        {
                            int index = num;
                            toggle.onValueChanged.ClearAll();
                            toggle.isOn = beatmapObject.shapeOption == index;
                            toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes[beatmapObject.shape]);

                            if (RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes[beatmapObject.shape])
                                toggle.onValueChanged.AddListener(_val =>
                                {
                                    beatmapObject.shapeOption = index;

                                    // Since shape has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        Updater.UpdateObject(beatmapObject, "Shape");

                                    RenderShape(beatmapObject);
                                });

                            num++;
                        }

                        break;
                    }
            }
        }

        public void SetDepthSlider(BeatmapObject beatmapObject, int value, InputField inputField, Slider slider)
        {
            if (!RTEditor.ShowModdedUI)
                value = Mathf.Clamp(value, EditorConfig.Instance.RenderDepthRange.Value.y, EditorConfig.Instance.RenderDepthRange.Value.x);

            beatmapObject.Depth = value;

            slider.onValueChanged.ClearAll();
            slider.value = value;
            slider.onValueChanged.AddListener(_val => SetDepthInputField(beatmapObject, ((int)_val).ToString(), inputField, slider));

            // Since depth has no affect on the timeline object, we will only need to update the physical object.
            if (UpdateObjects)
                Updater.UpdateObject(beatmapObject, "Depth");
        }

        public void SetDepthInputField(BeatmapObject beatmapObject, string value, InputField inputField, Slider slider)
        {
            if (!int.TryParse(value, out int num))
                return;

            if (!RTEditor.ShowModdedUI)
                num = Mathf.Clamp(num, EditorConfig.Instance.RenderDepthRange.Value.y, EditorConfig.Instance.RenderDepthRange.Value.x);

            beatmapObject.Depth = num;

            inputField.onValueChanged.ClearAll();
            inputField.text = num.ToString();
            inputField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int numb))
                    SetDepthSlider(beatmapObject, numb, inputField, slider);
            });

            // Since depth has no affect on the timeline object, we will only need to update the physical object.
            if (UpdateObjects)
                Updater.UpdateObject(beatmapObject, "Depth");
        }

        /// <summary>
        /// Renders the Depth InputField and Slider.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderDepth(BeatmapObject beatmapObject)
        {
            var depthSlider = Dialog.DepthSlider;
            var depthText = Dialog.DepthField.inputField;

            if (!Dialog.DepthField.inputField.GetComponent<InputFieldSwapper>())
            {
                var ifh = Dialog.DepthField.inputField.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(Dialog.DepthField.inputField, InputFieldSwapper.Type.Num);
            }

            Dialog.DepthField.inputField.onValueChanged.ClearAll();
            Dialog.DepthField.inputField.text = beatmapObject.Depth.ToString();
            Dialog.DepthField.inputField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                    SetDepthSlider(beatmapObject, num, Dialog.DepthField.inputField, Dialog.DepthSlider);
            });

            var max = EditorConfig.Instance.EditorComplexity.Value == Complexity.Simple ? 30 : EditorConfig.Instance.RenderDepthRange.Value.x;
            var min = EditorConfig.Instance.EditorComplexity.Value == Complexity.Simple ? 0 : EditorConfig.Instance.RenderDepthRange.Value.y;

            Dialog.DepthSlider.maxValue = max;
            Dialog.DepthSlider.minValue = min;

            Dialog.DepthSlider.onValueChanged.ClearAll();
            Dialog.DepthSlider.value = beatmapObject.Depth;
            Dialog.DepthSlider.onValueChanged.AddListener(_val => SetDepthInputField(beatmapObject, _val.ToString(), Dialog.DepthField.inputField, Dialog.DepthSlider));

            if (RTEditor.ShowModdedUI)
            {
                max = 0;
                min = 0;
            }

            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.DepthField.inputField, -1, min, max);
            TriggerHelper.AddEventTriggers(Dialog.DepthField.inputField.gameObject, TriggerHelper.ScrollDeltaInt(Dialog.DepthField.inputField, 1, min, max));
            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.DepthField.inputField, -1, min, max, Dialog.DepthParent);

            Dialog.RenderTypeDropdown.onValueChanged.ClearAll();
            Dialog.RenderTypeDropdown.value = beatmapObject.background ? 1 : 0;
            Dialog.RenderTypeDropdown.onValueChanged.AddListener(_val =>
            {
                beatmapObject.background = _val == 1;
                if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                    levelObject.visualObject.GameObject.layer = beatmapObject.background ? 9 : 8;
            });
        }

        /// <summary>
        /// Creates and Renders the UnityExplorer GameObject Inspector.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to get.</param>
        public void RenderGameObjectInspector(BeatmapObject beatmapObject)
        {
            if (!ModCompatibility.UnityExplorerInstalled)
                return;

            var tfv = ObjEditor.inst.ObjectView.transform;

            var inspector = AccessTools.TypeByName("UnityExplorer.InspectorManager");
            var uiManager = AccessTools.TypeByName("UnityExplorer.UI.UIManager");

            if (inspector != null && !tfv.Find("inspect"))
            {
                var label = tfv.ChildList().First(x => x.name == "label").gameObject.Duplicate(tfv, "unity explorer label");
                var index = tfv.Find("editor").GetSiblingIndex() + 1;
                label.transform.SetSiblingIndex(index);

                Destroy(label.transform.GetChild(1).gameObject);
                var labelText = label.transform.GetChild(0).GetComponent<Text>();
                labelText.text = "Unity Explorer";
                EditorThemeManager.AddLightText(labelText);

                var inspect = EditorPrefabHolder.Instance.Function2Button.Duplicate(tfv, "inspectbeatmapobject", index + 1);
                inspect.SetActive(true);

                var inspectText = inspect.transform.GetChild(0).GetComponent<Text>();
                inspectText.text = "Inspect BeatmapObject";

                var inspectGameObject = EditorPrefabHolder.Instance.Function2Button.Duplicate(tfv, "inspect", index + 2);
                inspectGameObject.SetActive(true);

                var inspectGameObjectText = inspectGameObject.transform.GetChild(0).GetComponent<Text>();
                inspectGameObjectText.text = "Inspect LevelObject";
                
                var inspectTimelineObject = EditorPrefabHolder.Instance.Function2Button.Duplicate(tfv, "inspecttimelineobject", index + 3);
                inspectTimelineObject.SetActive(true);

                var inspectTimelineObjectText = inspectTimelineObject.transform.GetChild(0).GetComponent<Text>();
                inspectTimelineObjectText.text = "Inspect TimelineObject";

                var inspectButton = inspect.GetComponent<Button>();
                var inspectGameObjectButton = inspectGameObject.GetComponent<Button>();
                var inspectTimelineObjectButton = inspectTimelineObject.GetComponent<Button>();

                Destroy(inspect.GetComponent<Animator>());
                inspectButton.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddSelectable(inspectButton, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(inspectText, ThemeGroup.Function_2_Text);

                Destroy(inspectGameObject.GetComponent<Animator>());
                inspectGameObjectButton.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddSelectable(inspectGameObjectButton, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(inspectGameObjectText, ThemeGroup.Function_2_Text);

                Destroy(inspectTimelineObject.GetComponent<Animator>());
                inspectTimelineObjectButton.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddSelectable(inspectTimelineObjectButton, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(inspectTimelineObjectText, ThemeGroup.Function_2_Text);
            }

            if (tfv.TryFind("unity explorer label", out Transform unityExplorerLabel))
                unityExplorerLabel.gameObject.SetActive(RTEditor.ShowModdedUI);

            if (tfv.Find("inspect"))
            {
                bool active = Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && RTEditor.ShowModdedUI;
                tfv.Find("inspect").gameObject.SetActive(active);
                var inspectButton = tfv.Find("inspect").GetComponent<Button>();
                inspectButton.onClick.ClearAll();
                if (active)
                    inspectButton.onClick.AddListener(() => ModCompatibility.Inspect(levelObject));
            }

            if (tfv.Find("inspectbeatmapobject"))
            {
                var inspectButton = tfv.Find("inspectbeatmapobject").GetComponent<Button>();
                inspectButton.gameObject.SetActive(RTEditor.ShowModdedUI);
                inspectButton.onClick.ClearAll();
                if (RTEditor.ShowModdedUI)
                    inspectButton.onClick.AddListener(() => ModCompatibility.Inspect(beatmapObject));
            }

            if (tfv.Find("inspecttimelineobject"))
            {
                var inspectButton = tfv.Find("inspecttimelineobject").GetComponent<Button>();
                inspectButton.gameObject.SetActive(RTEditor.ShowModdedUI);
                inspectButton.onClick.ClearAll();
                if (RTEditor.ShowModdedUI)
                    inspectButton.onClick.AddListener(() => ModCompatibility.Inspect(EditorTimeline.inst.GetTimelineObject(beatmapObject)));
            }
        }

        /// <summary>
        /// Renders the Layers InputField.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderLayers(BeatmapObject beatmapObject)
        {
            Dialog.EditorLayerField.onValueChanged.ClearAll();
            Dialog.EditorLayerField.text = (beatmapObject.editorData.layer + 1).ToString();
            Dialog.EditorLayerField.image.color = EditorTimeline.GetLayerColor(beatmapObject.editorData.layer);
            Dialog.EditorLayerField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                {
                    num = Mathf.Clamp(num - 1, 0, int.MaxValue);
                    beatmapObject.editorData.layer = num;

                    // Since layers have no effect on the physical object, we will only need to update the timeline object.
                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));

                    //editorLayersImage.color = RTEditor.GetLayerColor(beatmapObject.editorData.Layer);
                    RenderLayers(beatmapObject);
                }
            });

            if (Dialog.EditorLayerField.gameObject)
                TriggerHelper.AddEventTriggers(Dialog.EditorLayerField.gameObject, TriggerHelper.ScrollDeltaInt(Dialog.EditorLayerField, 1, 1, int.MaxValue));
        }

        /// <summary>
        /// Renders the Bin Slider.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderBin(BeatmapObject beatmapObject)
        {
            Dialog.BinSlider.onValueChanged.ClearAll();
            Dialog.BinSlider.maxValue = EditorTimeline.inst.BinCount;
            Dialog.BinSlider.value = beatmapObject.editorData.Bin;
            Dialog.BinSlider.onValueChanged.AddListener(_val =>
            {
                beatmapObject.editorData.Bin = Mathf.Clamp((int)_val, 0, EditorTimeline.inst.BinCount);

                // Since bin has no effect on the physical object, we will only need to update the timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
            });
        }

        void KeyframeHandler(Transform kfdialog, int type, IEnumerable<TimelineObject> selected, TimelineObject firstKF, BeatmapObject beatmapObject, string typeName, int i, string valueType)
        {
            var valueBase = kfdialog.Find(typeName);
            var value = valueBase.Find(valueType);

            if (!value)
            {
                CoreHelper.LogError($"Value {valueType} is null.");
                return;
            }

            var valueEventTrigger = typeName != "rotation" ? value.GetComponent<EventTrigger>() : kfdialog.GetChild(9).GetComponent<EventTrigger>();

            var valueInputField = value.GetComponent<InputField>();
            var valueButtonLeft = value.Find("<").GetComponent<Button>();
            var valueButtonRight = value.Find(">").GetComponent<Button>();

            if (!value.GetComponent<InputFieldSwapper>())
            {
                var ifh = value.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(valueInputField, InputFieldSwapper.Type.Num);
            }

            valueEventTrigger.triggers.Clear();

            switch (type)
            {
                case 0:
                    {
                        valueEventTrigger.triggers.Add(TriggerHelper.ScrollDelta(valueInputField, EditorConfig.Instance.ObjectPositionScroll.Value, EditorConfig.Instance.ObjectPositionScrollMultiply.Value, multi: true));
                        valueEventTrigger.triggers.Add(TriggerHelper.ScrollDeltaVector2(kfdialog.GetChild(9).GetChild(0).GetComponent<InputField>(), kfdialog.GetChild(9).GetChild(1).GetComponent<InputField>(), EditorConfig.Instance.ObjectPositionScroll.Value, EditorConfig.Instance.ObjectPositionScrollMultiply.Value));
                        break;
                    }
                case 1:
                    {
                        valueEventTrigger.triggers.Add(TriggerHelper.ScrollDelta(valueInputField, EditorConfig.Instance.ObjectScaleScroll.Value, EditorConfig.Instance.ObjectScaleScrollMultiply.Value, multi: true));
                        valueEventTrigger.triggers.Add(TriggerHelper.ScrollDeltaVector2(kfdialog.GetChild(9).GetChild(0).GetComponent<InputField>(), kfdialog.GetChild(9).GetChild(1).GetComponent<InputField>(), EditorConfig.Instance.ObjectScaleScroll.Value, EditorConfig.Instance.ObjectScaleScrollMultiply.Value));
                        break;
                    }
                case 2:
                    {
                        valueEventTrigger.triggers.Add(TriggerHelper.ScrollDelta(valueInputField, EditorConfig.Instance.ObjectRotationScroll.Value, EditorConfig.Instance.ObjectRotationScrollMultiply.Value));
                        break;
                    }
            }

            int current = i;

            valueInputField.characterValidation = InputField.CharacterValidation.None;
            valueInputField.contentType = InputField.ContentType.Standard;
            valueInputField.keyboardType = TouchScreenKeyboardType.Default;

            valueInputField.onEndEdit.ClearAll();
            valueInputField.onValueChanged.ClearAll();
            valueInputField.text = selected.Count() == 1 ? firstKF.GetData<EventKeyframe>().eventValues[i].ToString() : typeName == "rotation" ? "15" : "1";
            valueInputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num) && selected.Count() == 1)
                {

                    firstKF.GetData<EventKeyframe>().eventValues[current] = num;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Keyframes");
                }
            });
            valueInputField.onEndEdit.AddListener(_val =>
            {
                var variables = new Dictionary<string, float>
                {
                    { "eventTime", firstKF.GetData<EventKeyframe>().eventTime },
                    { "currentValue", firstKF.GetData<EventKeyframe>().eventValues[current] }
                };

                if (!float.TryParse(_val, out float n) && RTMath.TryParse(_val, firstKF.GetData<EventKeyframe>().eventValues[current], variables, out float calc))
                    valueInputField.text = calc.ToString();
            });

            valueButtonLeft.onClick.ClearAll();
            valueButtonLeft.onClick.AddListener(() =>
            {
                if (float.TryParse(valueInputField.text, out float x))
                {
                    if (selected.Count() == 1)
                    {
                        valueInputField.text = (x - (typeName == "rotation" ? 5f : 1f)).ToString();
                        return;
                    }

                    foreach (var keyframe in selected)
                        keyframe.GetData<EventKeyframe>().eventValues[current] -= x;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Keyframes");
                }
            });

            valueButtonRight.onClick.ClearAll();
            valueButtonRight.onClick.AddListener(() =>
            {
                if (float.TryParse(valueInputField.text, out float x))
                {
                    if (selected.Count() == 1)
                    {
                        valueInputField.text = (x + (typeName == "rotation" ? 5f : 1f)).ToString();
                        return;
                    }

                    foreach (var keyframe in selected)
                        keyframe.GetData<EventKeyframe>().eventValues[current] += x;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Keyframes");
                }
            });
        }

        void UpdateKeyframeRandomDialog(Transform kfdialog, Transform randomValueLabel, Transform randomValue, int type, int randomType)
        {
            if (kfdialog.Find("r_axis"))
                kfdialog.Find("r_axis").gameObject.SetActive(RTEditor.ShowModdedUI && (randomType == 5 || randomType == 6));

            randomValueLabel.gameObject.SetActive(randomType != 0 && randomType != 5);
            randomValue.gameObject.SetActive(randomType != 0 && randomType != 5);
            randomValueLabel.GetChild(0).GetComponent<Text>().text = (randomType == 4) ? "Random Scale Min" : randomType == 6 ? "Minimum Range" : "Random X";
            randomValueLabel.GetChild(1).gameObject.SetActive(type != 2 || randomType == 6);
            randomValueLabel.GetChild(1).GetComponent<Text>().text = (randomType == 4) ? "Random Scale Max" : randomType == 6 ? "Maximum Range" : "Random Y";
            kfdialog.Find("random/interval-input").gameObject.SetActive(randomType != 0 && randomType != 3 && randomType != 5);
            kfdialog.Find("r_label/interval").gameObject.SetActive(randomType != 0 && randomType != 3 && randomType != 5);

            if (kfdialog.Find("relative-label"))
            {
                kfdialog.Find("relative-label").gameObject.SetActive(RTEditor.ShowModdedUI);
                if (RTEditor.ShowModdedUI)
                {
                    kfdialog.Find("relative-label").GetChild(0).GetComponent<Text>().text =
                        randomType == 6 && type != 2 ? "Object Flees from Player" : randomType == 6 ? "Object Turns Away from Player" : "Value Additive";
                    kfdialog.Find("relative").GetChild(1).GetComponent<Text>().text =
                        randomType == 6 && type != 2 ? "Flee" : randomType == 6 ? "Turn Away" : "Relative";
                }
            }

            randomValue.GetChild(1).gameObject.SetActive(type != 2 || randomType == 6);

            randomValue.GetChild(0).GetChild(0).AsRT().sizeDelta = new Vector2(type != 2 || randomType == 6 ? 117 : 317f, 32f);
            randomValue.GetChild(1).GetChild(0).AsRT().sizeDelta = new Vector2(type != 2 || randomType == 6 ? 117 : 317f, 32f);

            if (randomType != 0 && randomType != 3 && randomType != 5)
                kfdialog.Find("r_label/interval").GetComponent<Text>().text = randomType == 6 ? "Speed" : "Random Interval";
        }

        void KeyframeRandomHandler(Transform kfdialog, int type, IEnumerable<TimelineObject> selected, TimelineObject firstKF, BeatmapObject beatmapObject, string typeName)
        {
            var randomValueLabel = kfdialog.Find($"r_{typeName}_label");
            var randomValue = kfdialog.Find($"r_{typeName}");

            int random = firstKF.GetData<EventKeyframe>().random;

            if (kfdialog.Find("r_axis") && kfdialog.Find("r_axis").gameObject.TryGetComponent(out Dropdown rAxis))
            {
                var active = (random == 5 || random == 6) && RTEditor.ShowModdedUI && EditorConfig.Instance.ShowExperimental.Value;
                rAxis.gameObject.SetActive(active);
                rAxis.onValueChanged.ClearAll();
                if (active)
                {
                    rAxis.value = Mathf.Clamp((int)firstKF.GetData<EventKeyframe>().eventRandomValues[3], 0, 3);
                    rAxis.onValueChanged.AddListener(_val =>
                    {
                        foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                            keyframe.eventRandomValues[3] = _val;
                        Updater.UpdateObject(beatmapObject, "Keyframes");
                    });
                }
            }

            for (int n = 0; n <= (type == 0 ? 5 : type == 2 ? 4 : 3); n++)
            {
                // We skip the 2nd random type for compatibility with old PA levels (for some reason).
                int buttonTmp = (n >= 2 && (type != 2 || n < 3)) ? (n + 1) : (n > 2 && type == 2) ? n + 2 : n;

                var randomToggles = kfdialog.Find("random");
                var active = buttonTmp != 5 && buttonTmp != 6 || RTEditor.ShowModdedUI && EditorConfig.Instance.ShowExperimental.Value;
                randomToggles.GetChild(n).gameObject.SetActive(active);

                if (!active)
                    continue;

                var toggle = randomToggles.GetChild(n).GetComponent<Toggle>();
                toggle.onValueChanged.ClearAll();
                toggle.isOn = random == buttonTmp;
                toggle.onValueChanged.AddListener(_val =>
                {
                    if (_val)
                    {
                        foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                            keyframe.random = buttonTmp;

                            // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                            Updater.UpdateObject(beatmapObject, "Keyframes");
                    }

                    UpdateKeyframeRandomDialog(kfdialog, randomValueLabel, randomValue, type, buttonTmp);
                });
                if (!toggle.GetComponent<HoverUI>())
                {
                    var hoverUI = toggle.gameObject.AddComponent<HoverUI>();
                    hoverUI.animatePos = false;
                    hoverUI.animateSca = true;
                    hoverUI.size = 1.1f;
                }
            }

            UpdateKeyframeRandomDialog(kfdialog, randomValueLabel, randomValue, type, random);

            float num = 0f;
            if (firstKF.GetData<EventKeyframe>().eventRandomValues.Length > 2)
                num = firstKF.GetData<EventKeyframe>().eventRandomValues[2];

            var randomInterval = kfdialog.Find("random/interval-input");
            var randomIntervalIF = randomInterval.GetComponent<InputField>();
            randomIntervalIF.NewValueChangedListener(num.ToString(), _val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                        keyframe.eventRandomValues[2] = num;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Keyframes");
                }
            });

            TriggerHelper.AddEventTriggers(randomIntervalIF.gameObject,
                TriggerHelper.ScrollDelta(randomIntervalIF, 0.01f));

            if (!randomInterval.GetComponent<InputFieldSwapper>())
            {
                var ifh = randomInterval.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(randomIntervalIF, InputFieldSwapper.Type.Num);
            }

            TriggerHelper.AddEventTriggers(randomInterval.gameObject, TriggerHelper.ScrollDelta(randomIntervalIF, max: random == 6 ? 1f : 0f));
        }

        void KeyframeRandomValueHandler(Transform kfdialog, int type, IEnumerable<TimelineObject> selected, TimelineObject firstKF, BeatmapObject beatmapObject, string typeName, int i, string valueType)
        {
            var randomValueLabel = kfdialog.Find($"r_{typeName}_label");
            var randomValueBase = kfdialog.Find($"r_{typeName}");

            if (!randomValueBase)
            {
                CoreHelper.LogError($"Value {valueType} (Base) is null.");
                return;
            }

            var randomValue = randomValueBase.Find(valueType);

            if (!randomValue)
            {
                CoreHelper.LogError($"Value {valueType} is null.");
                return;
            }

            var random = firstKF.GetData<EventKeyframe>().random;

            var valueButtonLeft = randomValue.Find("<").GetComponent<Button>();
            var valueButtonRight = randomValue.Find(">").GetComponent<Button>();

            var randomValueInputField = randomValue.GetComponent<InputField>();

            randomValueInputField.characterValidation = InputField.CharacterValidation.None;
            randomValueInputField.contentType = InputField.ContentType.Standard;
            randomValueInputField.keyboardType = TouchScreenKeyboardType.Default;
            randomValueInputField.onValueChanged.ClearAll();
            randomValueInputField.text = selected.Count() == 1 ? firstKF.GetData<EventKeyframe>().eventRandomValues[i].ToString() : typeName == "rotation" ? "15" : "1";
            randomValueInputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num) && selected.Count() == 1)
                {
                    firstKF.GetData<EventKeyframe>().eventRandomValues[i] = num;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Keyframes");
                }
            });

            valueButtonLeft.onClick.ClearAll();
            valueButtonLeft.onClick.AddListener(() =>
            {
                if (float.TryParse(randomValueInputField.text, out float x))
                {
                    if (selected.Count() == 1)
                    {
                        randomValueInputField.text = (x - (typeName == "rotation" ? 15f : 1f)).ToString();
                        return;
                    }

                    foreach (var keyframe in selected)
                        keyframe.GetData<EventKeyframe>().eventRandomValues[i] -= x;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Keyframes");
                }
            });

            valueButtonRight.onClick.ClearAll();
            valueButtonRight.onClick.AddListener(() =>
            {
                if (float.TryParse(randomValueInputField.text, out float x))
                {
                    if (selected.Count() == 1)
                    {
                        randomValueInputField.text = (x + (typeName == "rotation" ? 15f : 1f)).ToString();
                        return;
                    }

                    foreach (var keyframe in selected)
                        keyframe.GetData<EventKeyframe>().eventRandomValues[i] += x;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Keyframes");
                }
            });

            TriggerHelper.AddEventTriggers(randomValue.gameObject,
                TriggerHelper.ScrollDelta(randomValueInputField, type == 2 && random != 6 ? 15f : 0.1f, type == 2 && random != 6 ? 3f : 10f, multi: true),
                TriggerHelper.ScrollDeltaVector2(randomValueInputField, randomValueBase.GetChild(1).GetComponent<InputField>(), type == 2 && random != 6 ? 15f : 0.1f, type == 2 && random != 6 ? 3f : 10f));

            if (!randomValue.GetComponent<InputFieldSwapper>())
            {
                var ifh = randomValue.gameObject.AddComponent<InputFieldSwapper>();
                ifh.Init(randomValueInputField, InputFieldSwapper.Type.Num);
            }
        }

        public void PasteKeyframeData(EventKeyframe copiedData, IEnumerable<TimelineObject> selected, BeatmapObject beatmapObject, string name)
        {
            if (copiedData == null)
            {
                EditorManager.inst.DisplayNotification($"{name} keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            foreach (var timelineObject in selected)
            {
                var kf = timelineObject.GetData<EventKeyframe>();
                kf.curveType = copiedData.curveType;
                kf.eventValues = copiedData.eventValues.Copy();
                kf.eventRandomValues = copiedData.eventRandomValues.Copy();
                kf.random = copiedData.random;
                kf.relative = copiedData.relative;
            }

            RenderKeyframes(beatmapObject);
            RenderObjectKeyframesDialog(beatmapObject);
            Updater.UpdateObject(beatmapObject, "Keyframes");
            EditorManager.inst.DisplayNotification($"Pasted {name.ToLower()} keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
        }

        public void RenderObjectKeyframesDialog(BeatmapObject beatmapObject)
        {
            var selected = beatmapObject.timelineObject.InternalTimelineObjects.Where(x => x.Selected);

            for (int i = 0; i < ObjEditor.inst.KeyframeDialogs.Count; i++)
                ObjEditor.inst.KeyframeDialogs[i].SetActive(false);

            if (selected.Count() < 1)
                return;

            if (!(selected.Count() == 1 || selected.All(x => x.Type == selected.Min(y => y.Type))))
            {
                ObjEditor.inst.KeyframeDialogs[4].SetActive(true);

                try
                {
                    var dialog = ObjEditor.inst.KeyframeDialogs[4].transform;
                    var time = dialog.Find("time/time/time").GetComponent<InputField>();
                    time.onValueChanged.ClearAll();
                    if (time.text == "100.000")
                        time.text = "10";

                    var setTime = dialog.Find("time/time").GetChild(3).GetComponent<Button>();
                    setTime.onClick.ClearAll();
                    setTime.onClick.AddListener(() =>
                    {
                        if (float.TryParse(time.text, out float num))
                        {
                            if (num < 0f)
                                num = 0f;

                            if (EditorConfig.Instance.RoundToNearest.Value)
                                num = RTMath.RoundToNearestDecimal(num, 3);

                            foreach (var kf in selected.Where(x => x.Index != 0))
                                kf.Time = num;

                            ResizeKeyframeTimeline(beatmapObject);

                            RenderKeyframes(beatmapObject);

                            // Keyframe Time affects both physical object and timeline object.
                            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Keyframes");
                        }
                    });

                    var decreaseTimeGreat = dialog.Find("time/time/<<").GetComponent<Button>();
                    var decreaseTime = dialog.Find("time/time/<").GetComponent<Button>();
                    var increaseTimeGreat = dialog.Find("time/time/>>").GetComponent<Button>();
                    var increaseTime = dialog.Find("time/time/>").GetComponent<Button>();

                    decreaseTime.onClick.ClearAll();
                    decreaseTime.onClick.AddListener(() =>
                    {
                        if (float.TryParse(time.text, out float num))
                        {
                            if (num < 0f)
                                num = 0f;

                            if (EditorConfig.Instance.RoundToNearest.Value)
                                num = RTMath.RoundToNearestDecimal(num, 3);

                            foreach (var kf in selected.Where(x => x.Index != 0))
                                kf.Time = Mathf.Clamp(kf.Time - num, 0f, float.MaxValue);

                            ResizeKeyframeTimeline(beatmapObject);

                            RenderKeyframes(beatmapObject);

                            // Keyframe Time affects both physical object and timeline object.
                            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Keyframes");
                        }
                    });

                    increaseTime.onClick.ClearAll();
                    increaseTime.onClick.AddListener(() =>
                    {
                        if (float.TryParse(time.text, out float num))
                        {
                            if (num < 0f)
                                num = 0f;

                            if (EditorConfig.Instance.RoundToNearest.Value)
                                num = RTMath.RoundToNearestDecimal(num, 3);

                            foreach (var kf in selected.Where(x => x.Index != 0))
                                kf.Time = Mathf.Clamp(kf.Time + num, 0f, float.MaxValue);

                            ResizeKeyframeTimeline(beatmapObject);

                            RenderKeyframes(beatmapObject);

                            // Keyframe Time affects both physical object and timeline object.
                            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Keyframes");
                        }
                    });

                    decreaseTimeGreat.onClick.ClearAll();
                    decreaseTimeGreat.onClick.AddListener(() =>
                    {
                        if (float.TryParse(time.text, out float num))
                        {
                            if (num < 0f)
                                num = 0f;

                            if (EditorConfig.Instance.RoundToNearest.Value)
                                num = RTMath.RoundToNearestDecimal(num, 3);

                            foreach (var kf in selected.Where(x => x.Index != 0))
                                kf.Time = Mathf.Clamp(kf.Time - (num * 10f), 0f, float.MaxValue);

                            ResizeKeyframeTimeline(beatmapObject);

                            RenderKeyframes(beatmapObject);

                            // Keyframe Time affects both physical object and timeline object.
                            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Keyframes");
                        }
                    });

                    increaseTimeGreat.onClick.ClearAll();
                    increaseTimeGreat.onClick.AddListener(() =>
                    {
                        if (float.TryParse(time.text, out float num))
                        {
                            if (num < 0f)
                                num = 0f;

                            if (EditorConfig.Instance.RoundToNearest.Value)
                                num = RTMath.RoundToNearestDecimal(num, 3);

                            foreach (var kf in selected.Where(x => x.Index != 0))
                                kf.Time = Mathf.Clamp(kf.Time + (num * 10f), 0f, float.MaxValue);

                            ResizeKeyframeTimeline(beatmapObject);

                            RenderKeyframes(beatmapObject);

                            // Keyframe Time affects both physical object and timeline object.
                            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Keyframes");
                        }
                    });

                    TriggerHelper.AddEventTriggers(time.gameObject, TriggerHelper.ScrollDelta(time));

                    var curvesMulti = dialog.Find("curves/curves").GetComponent<Dropdown>();
                    var curvesMultiApplyButton = dialog.Find("curves/apply").GetComponent<Button>();
                    curvesMulti.onValueChanged.ClearAll();
                    curvesMultiApplyButton.onClick.AddListener(() =>
                    {
                        if (!DataManager.inst.AnimationListDictionary.TryGetValue(curvesMulti.value, out DataManager.LSAnimation anim))
                            return;

                        foreach (var keyframe in selected.Where(x => x.Index != 0).Select(x => x.GetData<EventKeyframe>()))
                            keyframe.curveType = anim;

                        ResizeKeyframeTimeline(beatmapObject);

                        RenderKeyframes(beatmapObject);

                        // Keyframe Time affects both physical object and timeline object.
                        EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                        if (UpdateObjects)
                            Updater.UpdateObject(beatmapObject, "Keyframes");
                    });

                    var valueIndex = dialog.Find("value base/value index/input").GetComponent<InputField>();
                    valueIndex.onValueChanged.ClearAll();
                    if (!int.TryParse(valueIndex.text, out int a))
                        valueIndex.text = "0";
                    valueIndex.onValueChanged.AddListener(_val =>
                    {
                        if (!int.TryParse(_val, out int n))
                            valueIndex.text = "0";
                    });

                    TriggerHelper.IncreaseDecreaseButtonsInt(valueIndex, t: valueIndex.transform.parent);
                    TriggerHelper.AddEventTriggers(valueIndex.gameObject, TriggerHelper.ScrollDeltaInt(valueIndex));

                    var value = dialog.Find("value base/value/input").GetComponent<InputField>();
                    value.onValueChanged.ClearAll();
                    value.onValueChanged.AddListener(_val =>
                    {
                        if (!float.TryParse(_val, out float n))
                            value.text = "0";
                    });

                    var setValue = value.transform.parent.GetChild(2).GetComponent<Button>();
                    setValue.onClick.ClearAll();
                    setValue.onClick.AddListener(() =>
                    {
                        if (float.TryParse(value.text, out float num))
                        {
                            foreach (var kf in selected)
                            {
                                var keyframe = kf.GetData<EventKeyframe>();

                                var index = Parser.TryParse(valueIndex.text, 0);

                                index = Mathf.Clamp(index, 0, keyframe.eventValues.Length - 1);
                                if (index >= 0 && index < keyframe.eventValues.Length)
                                    keyframe.eventValues[index] = kf.Type == 3 ? Mathf.Clamp((int)num, 0, CoreHelper.CurrentBeatmapTheme.objectColors.Count - 1) : num;
                            }

                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject, "Keyframes");
                        }
                    });

                    TriggerHelper.IncreaseDecreaseButtons(value, t: value.transform.parent);
                    TriggerHelper.AddEventTriggers(value.gameObject, TriggerHelper.ScrollDelta(value));

                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                return;
            }

            var firstKF = selected.ElementAt(0);
            var type = firstKF.Type;

            CoreHelper.Log($"Selected Keyframe:\nID - {firstKF.ID}\nType: {firstKF.Type}\nIndex {firstKF.Index}");

            ObjEditor.inst.KeyframeDialogs[type].SetActive(true);

            ObjEditor.inst.currentKeyframeKind = type;
            ObjEditor.inst.currentKeyframe = firstKF.Index;

            var kfdialog = ObjEditor.inst.KeyframeDialogs[type].transform;

            var timeDecreaseGreat = kfdialog.Find("time/<<").GetComponent<Button>();
            var timeDecrease = kfdialog.Find("time/<").GetComponent<Button>();
            var timeIncrease = kfdialog.Find("time/>").GetComponent<Button>();
            var timeIncreaseGreat = kfdialog.Find("time/>>").GetComponent<Button>();
            var timeSet = kfdialog.Find("time/time").GetComponent<InputField>();

            timeDecreaseGreat.interactable = firstKF.Index != 0;
            timeDecrease.interactable = firstKF.Index != 0;
            timeIncrease.interactable = firstKF.Index != 0;
            timeIncreaseGreat.interactable = firstKF.Index != 0;
            timeSet.interactable = firstKF.Index != 0;

            var superLeft = kfdialog.Find("edit/<<").GetComponent<Button>();

            superLeft.onClick.ClearAll();
            superLeft.interactable = firstKF.Index != 0;
            superLeft.onClick.AddListener(() => { SetCurrentKeyframe(beatmapObject, 0, true); });

            var left = kfdialog.Find("edit/<").GetComponent<Button>();

            left.onClick.ClearAll();
            left.interactable = selected.Count() == 1 && firstKF.Index != 0;
            left.onClick.AddListener(() => SetCurrentKeyframe(beatmapObject, firstKF.Index - 1, true));

            kfdialog.Find("edit/|").GetComponentInChildren<Text>().text = firstKF.Index == 0 ? "S" : firstKF.Index == beatmapObject.events[firstKF.Type].Count - 1 ? "E" : firstKF.Index.ToString();

            var right = kfdialog.Find("edit/>").GetComponent<Button>();

            right.onClick.ClearAll();
            right.interactable = selected.Count() == 1 && firstKF.Index < beatmapObject.events[type].Count - 1;
            right.onClick.AddListener(() => SetCurrentKeyframe(beatmapObject, firstKF.Index + 1, true));

            var superRight = kfdialog.Find("edit/>>").GetComponent<Button>();

            superRight.onClick.ClearAll();
            superRight.interactable = selected.Count() == 1 && firstKF.Index < beatmapObject.events[type].Count - 1;
            superRight.onClick.AddListener(() => SetCurrentKeyframe(beatmapObject, beatmapObject.events[type].Count - 1, true));

            var copy = kfdialog.Find("edit/copy").GetComponent<Button>();
            copy.onClick.ClearAll();
            copy.onClick.AddListener(() =>
            {
                switch (type)
                {
                    case 0:
                        CopiedPositionData = EventKeyframe.DeepCopy(firstKF.GetData<EventKeyframe>());
                        break;
                    case 1:
                        CopiedScaleData = EventKeyframe.DeepCopy(firstKF.GetData<EventKeyframe>());
                        break;
                    case 2:
                        CopiedRotationData = EventKeyframe.DeepCopy(firstKF.GetData<EventKeyframe>());
                        break;
                    case 3:
                        CopiedColorData = EventKeyframe.DeepCopy(firstKF.GetData<EventKeyframe>());
                        break;
                }
                EditorManager.inst.DisplayNotification("Copied keyframe data!", 2f, EditorManager.NotificationType.Success);
            });

            var paste = kfdialog.Find("edit/paste").GetComponent<Button>();
            paste.onClick.ClearAll();
            paste.onClick.AddListener(() =>
            {
                switch (type)
                {
                    case 0:
                        PasteKeyframeData(CopiedPositionData, selected, beatmapObject, "Position");
                        break;
                    case 1:
                        PasteKeyframeData(CopiedScaleData, selected, beatmapObject, "Scale");
                        break;
                    case 2:
                        PasteKeyframeData(CopiedRotationData, selected, beatmapObject, "Rotation");
                        break;
                    case 3:
                        PasteKeyframeData(CopiedColorData, selected, beatmapObject, "Color");
                        break;
                }
            });

            var deleteKey = kfdialog.Find("edit/del").GetComponent<Button>();

            deleteKey.onClick.ClearAll();
            deleteKey.onClick.AddListener(DeleteKeyframes(beatmapObject).Start);

            var tet = kfdialog.Find("time").GetComponent<EventTrigger>();
            var tif = kfdialog.Find("time/time").GetComponent<InputField>();

            tet.triggers.Clear();
            if (selected.Count() == 1 && firstKF.Index != 0 || selected.Count() > 1)
                tet.triggers.Add(TriggerHelper.ScrollDelta(tif));

            tif.onValueChanged.ClearAll();
            tif.text = selected.Count() == 1 ? firstKF.Time.ToString() : "1";
            tif.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num) && !ObjEditor.inst.timelineKeyframesDrag && selected.Count() == 1)
                {
                    if (num < 0f)
                        num = 0f;

                    if (EditorConfig.Instance.RoundToNearest.Value)
                        num = RTMath.RoundToNearestDecimal(num, 3);

                    firstKF.Time = num;

                    ResizeKeyframeTimeline(beatmapObject);

                    RenderKeyframes(beatmapObject);

                    // Keyframe Time affects both physical object and timeline object.
                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Keyframes");
                }
            });

            if (selected.Count() == 1)
                TriggerHelper.IncreaseDecreaseButtons(tif, t: kfdialog.Find("time"));
            else
            {
                var btR = kfdialog.Find("time/<").GetComponent<Button>();
                var btL = kfdialog.Find("time/>").GetComponent<Button>();
                var btGR = kfdialog.Find("time/<<").GetComponent<Button>();
                var btGL = kfdialog.Find("time/>>").GetComponent<Button>();

                btR.onClick.ClearAll();
                btR.onClick.AddListener(() =>
                {
                    if (float.TryParse(tif.text, out float result))
                    {
                        var num = Input.GetKey(KeyCode.LeftAlt) ? 0.1f / 10f : Input.GetKey(KeyCode.LeftControl) ? 0.1f * 10f : 0.1f;
                        result -= num;

                        if (selected.Count() == 1)
                        {
                            tif.text = result.ToString();
                            return;
                        }

                        foreach (var keyframe in selected)
                            keyframe.Time = Mathf.Clamp(keyframe.Time - num, 0.001f, float.PositiveInfinity);
                    }
                });

                btL.onClick.ClearAll();
                btL.onClick.AddListener(() =>
                {
                    if (float.TryParse(tif.text, out float result))
                    {
                        var num = Input.GetKey(KeyCode.LeftAlt) ? 0.1f / 10f : Input.GetKey(KeyCode.LeftControl) ? 0.1f * 10f : 0.1f;
                        result += num;

                        if (selected.Count() == 1)
                        {
                            tif.text = result.ToString();
                            return;
                        }

                        foreach (var keyframe in selected)
                            keyframe.Time = Mathf.Clamp(keyframe.Time + num, 0.001f, float.PositiveInfinity);
                    }
                });

                btGR.onClick.ClearAll();
                btGR.onClick.AddListener(() =>
                {
                    if (float.TryParse(tif.text, out float result))
                    {
                        var num = (Input.GetKey(KeyCode.LeftAlt) ? 0.1f / 10f : Input.GetKey(KeyCode.LeftControl) ? 0.1f * 10f : 0.1f) * 10f;
                        result -= num;

                        if (selected.Count() == 1)
                        {
                            tif.text = result.ToString();
                            return;
                        }

                        foreach (var keyframe in selected)
                            keyframe.Time = Mathf.Clamp(keyframe.Time - num, 0.001f, float.PositiveInfinity);
                    }
                });

                btGL.onClick.ClearAll();
                btGL.onClick.AddListener(() =>
                {
                    if (float.TryParse(tif.text, out float result))
                    {
                        var num = (Input.GetKey(KeyCode.LeftAlt) ? 0.1f / 10f : Input.GetKey(KeyCode.LeftControl) ? 0.1f * 10f : 0.1f) * 10f;
                        result += num;

                        if (selected.Count() == 1)
                        {
                            tif.text = result.ToString();
                            return;
                        }

                        foreach (var keyframe in selected)
                            keyframe.Time = Mathf.Clamp(keyframe.Time + num, 0.001f, float.PositiveInfinity);
                    }
                });
            }

            kfdialog.Find("curves_label").gameObject.SetActive(selected.Count() == 1 && firstKF.Index != 0 || selected.Count() > 1);
            kfdialog.Find("curves").gameObject.SetActive(selected.Count() == 1 && firstKF.Index != 0 || selected.Count() > 1);
            var curves = kfdialog.Find("curves").GetComponent<Dropdown>();
            curves.onValueChanged.ClearAll();

            if (DataManager.inst.AnimationListDictionaryBack.TryGetValue(firstKF.GetData<EventKeyframe>().curveType, out int animIndex))
                curves.value = animIndex;

            curves.onValueChanged.AddListener(_val =>
            {
                if (!DataManager.inst.AnimationListDictionary.TryGetValue(_val, out DataManager.LSAnimation anim))
                    return;

                foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                    keyframe.curveType = anim;

                // Since keyframe curve has no affect on the timeline object, we will only need to update the physical object.
                if (UpdateObjects)
                    Updater.UpdateObject(beatmapObject, "Keyframes");
                RenderKeyframes(beatmapObject);
            });

            switch (type)
            {
                case 0:
                    {
                        KeyframeHandler(kfdialog, type, selected, firstKF, beatmapObject, "position", 0, "x");
                        KeyframeHandler(kfdialog, type, selected, firstKF, beatmapObject, "position", 1, "y");
                        KeyframeHandler(kfdialog, type, selected, firstKF, beatmapObject, "position", 2, "z");

                        KeyframeRandomHandler(kfdialog, type, selected, firstKF, beatmapObject, "position");
                        KeyframeRandomValueHandler(kfdialog, type, selected, firstKF, beatmapObject, "position", 0, "x");
                        KeyframeRandomValueHandler(kfdialog, type, selected, firstKF, beatmapObject, "position", 1, "y");

                        break;
                    }
                case 1:
                    {
                        KeyframeHandler(kfdialog, type, selected, firstKF, beatmapObject, "scale", 0, "x");
                        KeyframeHandler(kfdialog, type, selected, firstKF, beatmapObject, "scale", 1, "y");

                        KeyframeRandomHandler(kfdialog, type, selected, firstKF, beatmapObject, "scale");
                        KeyframeRandomValueHandler(kfdialog, type, selected, firstKF, beatmapObject, "scale", 0, "x");
                        KeyframeRandomValueHandler(kfdialog, type, selected, firstKF, beatmapObject, "scale", 1, "y");

                        break;
                    }
                case 2:
                    {
                        KeyframeHandler(kfdialog, type, selected, firstKF, beatmapObject, "rotation", 0, "x");

                        KeyframeRandomHandler(kfdialog, type, selected, firstKF, beatmapObject, "rotation");
                        KeyframeRandomValueHandler(kfdialog, type, selected, firstKF, beatmapObject, "rotation", 0, "x");
                        KeyframeRandomValueHandler(kfdialog, type, selected, firstKF, beatmapObject, "rotation", 1, "y");

                        break;
                    }
                case 3:
                    {
                        bool showModifiedColors = EditorConfig.Instance.ShowModifiedColors.Value;
                        var eventTime = firstKF.GetData<EventKeyframe>().eventTime;
                        int index = 0;
                        foreach (var toggle in ObjEditor.inst.colorButtons)
                        {
                            int tmpIndex = index;

                            toggle.gameObject.SetActive(RTEditor.ShowModdedUI || tmpIndex < 9);

                            toggle.onValueChanged.ClearAll();
                            if (RTEditor.ShowModdedUI || tmpIndex < 9)
                            {
                                toggle.isOn = index == firstKF.GetData<EventKeyframe>().eventValues[0];
                                toggle.onValueChanged.AddListener(_val => SetKeyframeColor(beatmapObject, 0, tmpIndex, ObjEditor.inst.colorButtons, selected));
                            }

                            if (showModifiedColors)
                            {
                                var color = CoreHelper.CurrentBeatmapTheme.GetObjColor(tmpIndex);

                                float hueNum = beatmapObject.Interpolate(type, 2, eventTime);
                                float satNum = beatmapObject.Interpolate(type, 3, eventTime);
                                float valNum = beatmapObject.Interpolate(type, 4, eventTime);

                                toggle.image.color = CoreHelper.ChangeColorHSV(color, hueNum, satNum, valNum);
                            }
                            else
                                toggle.image.color = CoreHelper.CurrentBeatmapTheme.GetObjColor(tmpIndex);

                            if (!toggle.GetComponent<HoverUI>())
                            {
                                var hoverUI = toggle.gameObject.AddComponent<HoverUI>();
                                hoverUI.animatePos = false;
                                hoverUI.animateSca = true;
                                hoverUI.size = 1.1f;
                            }
                            index++;
                        }

                        var random = firstKF.GetData<EventKeyframe>().random;

                        kfdialog.Find("opacity_label").gameObject.SetActive(RTEditor.NotSimple);
                        kfdialog.Find("opacity").gameObject.SetActive(RTEditor.NotSimple);
                        kfdialog.Find("opacity/collision").gameObject.SetActive(RTEditor.ShowModdedUI);

                        kfdialog.Find("huesatval_label").gameObject.SetActive(RTEditor.ShowModdedUI);
                        kfdialog.Find("huesatval").gameObject.SetActive(RTEditor.ShowModdedUI);

                        var showGradient = RTEditor.NotSimple && beatmapObject.gradientType != BeatmapObject.GradientType.Normal;

                        kfdialog.Find("color_label").GetChild(0).GetComponent<Text>().text = showGradient ? "Start Color" : "Color";
                        kfdialog.Find("opacity_label").GetChild(0).GetComponent<Text>().text = showGradient ? "Start Opacity" : "Opacity";
                        kfdialog.Find("huesatval_label").GetChild(0).GetComponent<Text>().text = showGradient ? "Start Hue" : "Hue";
                        kfdialog.Find("huesatval_label").GetChild(1).GetComponent<Text>().text = showGradient ? "Start Sat" : "Saturation";
                        kfdialog.Find("huesatval_label").GetChild(2).GetComponent<Text>().text = showGradient ? "Start Val" : "Value";

                        kfdialog.Find("gradient_color_label").gameObject.SetActive(showGradient);
                        kfdialog.Find("gradient_color").gameObject.SetActive(showGradient);
                        kfdialog.Find("gradient_opacity_label").gameObject.SetActive(showGradient && RTEditor.ShowModdedUI);
                        kfdialog.Find("gradient_opacity").gameObject.SetActive(showGradient && RTEditor.ShowModdedUI);
                        kfdialog.Find("gradient_huesatval_label").gameObject.SetActive(showGradient && RTEditor.ShowModdedUI);
                        kfdialog.Find("gradient_huesatval").gameObject.SetActive(showGradient && RTEditor.ShowModdedUI);

                        kfdialog.Find("color").AsRT().sizeDelta = new Vector2(366f, RTEditor.ShowModdedUI ? 78f : 32f);
                        kfdialog.Find("gradient_color").AsRT().sizeDelta = new Vector2(366f, RTEditor.ShowModdedUI ? 78f : 32f);

                        if (!RTEditor.NotSimple)
                            break;

                        var opacity = kfdialog.Find("opacity/x").GetComponent<InputField>();

                        opacity.onValueChanged.RemoveAllListeners();
                        opacity.text = (-firstKF.GetData<EventKeyframe>().eventValues[1] + 1).ToString();
                        opacity.onValueChanged.AddListener(_val =>
                        {
                            if (float.TryParse(_val, out float n))
                            {
                                var value = Mathf.Clamp(-n + 1, 0f, 1f);
                                foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                                {
                                    keyframe.eventValues[1] = value;
                                    if (!RTEditor.ShowModdedUI)
                                        keyframe.eventValues[6] = 10f;
                                }

                                // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                    Updater.UpdateObject(beatmapObject, "Keyframes");
                            }
                        });

                        TriggerHelper.AddEventTriggers(kfdialog.Find("opacity").gameObject, TriggerHelper.ScrollDelta(opacity, 0.1f, 10f, 0f, 1f));

                        TriggerHelper.IncreaseDecreaseButtons(opacity);

                        index = 0;
                        foreach (var toggle in gradientColorButtons)
                        {
                            int tmpIndex = index;

                            toggle.gameObject.SetActive(RTEditor.ShowModdedUI || tmpIndex < 9);

                            toggle.onValueChanged.ClearAll();
                            if (RTEditor.ShowModdedUI || tmpIndex < 9)
                            {
                                toggle.isOn = index == firstKF.GetData<EventKeyframe>().eventValues[5];
                                toggle.onValueChanged.AddListener(_val => { SetKeyframeColor(beatmapObject, 5, tmpIndex, gradientColorButtons, selected); });
                            }

                            if (showModifiedColors)
                            {
                                var color = CoreHelper.CurrentBeatmapTheme.GetObjColor(tmpIndex);

                                float hueNum = beatmapObject.Interpolate(type, 7, eventTime);
                                float satNum = beatmapObject.Interpolate(type, 8, eventTime);
                                float valNum = beatmapObject.Interpolate(type, 9, eventTime);

                                toggle.image.color = CoreHelper.ChangeColorHSV(color, hueNum, satNum, valNum);
                            }
                            else
                                toggle.image.color = CoreHelper.CurrentBeatmapTheme.GetObjColor(tmpIndex);

                            if (!toggle.GetComponent<HoverUI>())
                            {
                                var hoverUI = toggle.gameObject.AddComponent<HoverUI>();
                                hoverUI.animatePos = false;
                                hoverUI.animateSca = true;
                                hoverUI.size = 1.1f;
                            }
                            index++;
                        }

                        if (!RTEditor.ShowModdedUI)
                            break;

                        var collision = kfdialog.Find("opacity/collision").GetComponent<Toggle>();
                        collision.onValueChanged.ClearAll();
                        collision.isOn = beatmapObject.opacityCollision;
                        collision.onValueChanged.AddListener(_val =>
                        {
                            beatmapObject.opacityCollision = _val;
                            // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                Updater.UpdateObject(beatmapObject);
                        });

                        var gradientOpacity = kfdialog.Find("gradient_opacity/x").GetComponent<InputField>();

                        gradientOpacity.onValueChanged.RemoveAllListeners();
                        gradientOpacity.text = (-firstKF.GetData<EventKeyframe>().eventValues[6] + 1).ToString();
                        gradientOpacity.onValueChanged.AddListener(_val =>
                        {
                            if (float.TryParse(_val, out float n))
                            {
                                foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                                    keyframe.eventValues[6] = Mathf.Clamp(-n + 1, 0f, 1f);

                                // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                    Updater.UpdateObject(beatmapObject, "Keyframes");
                            }
                        });

                        TriggerHelper.AddEventTriggers(kfdialog.Find("gradient_opacity").gameObject, TriggerHelper.ScrollDelta(gradientOpacity, 0.1f, 10f, 0f, 1f));

                        TriggerHelper.IncreaseDecreaseButtons(gradientOpacity);

                        // Start
                        {
                            var hue = kfdialog.Find("huesatval/x").GetComponent<InputField>();

                            hue.onValueChanged.RemoveAllListeners();
                            hue.text = firstKF.GetData<EventKeyframe>().eventValues[2].ToString();
                            hue.onValueChanged.AddListener(_val =>
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    firstKF.GetData<EventKeyframe>().eventValues[2] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        Updater.UpdateObject(beatmapObject, "Keyframes");
                                }
                            });

                            Destroy(kfdialog.transform.Find("huesatval").GetComponent<EventTrigger>());

                            TriggerHelper.AddEventTriggers(hue.gameObject, TriggerHelper.ScrollDelta(hue));
                            TriggerHelper.IncreaseDecreaseButtons(hue);

                            var sat = kfdialog.Find("huesatval/y").GetComponent<InputField>();

                            sat.onValueChanged.RemoveAllListeners();
                            sat.text = firstKF.GetData<EventKeyframe>().eventValues[3].ToString();
                            sat.onValueChanged.AddListener(_val =>
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    firstKF.GetData<EventKeyframe>().eventValues[3] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        Updater.UpdateObject(beatmapObject, "Keyframes");
                                }
                            });

                            TriggerHelper.AddEventTriggers(sat.gameObject, TriggerHelper.ScrollDelta(sat));
                            TriggerHelper.IncreaseDecreaseButtons(sat);

                            var val = kfdialog.Find("huesatval/z").GetComponent<InputField>();

                            val.onValueChanged.RemoveAllListeners();
                            val.text = firstKF.GetData<EventKeyframe>().eventValues[4].ToString();
                            val.onValueChanged.AddListener(_val =>
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    firstKF.GetData<EventKeyframe>().eventValues[4] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        Updater.UpdateObject(beatmapObject, "Keyframes");
                                }
                            });

                            TriggerHelper.AddEventTriggers(val.gameObject, TriggerHelper.ScrollDelta(val));
                            TriggerHelper.IncreaseDecreaseButtons(val);
                        }
                        
                        // End
                        {
                            var hue = kfdialog.Find("gradient_huesatval/x").GetComponent<InputField>();

                            hue.onValueChanged.RemoveAllListeners();
                            hue.text = firstKF.GetData<EventKeyframe>().eventValues[7].ToString();
                            hue.onValueChanged.AddListener(_val =>
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    firstKF.GetData<EventKeyframe>().eventValues[7] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        Updater.UpdateObject(beatmapObject, "Keyframes");
                                }
                            });

                            Destroy(kfdialog.transform.Find("gradient_huesatval").GetComponent<EventTrigger>());

                            TriggerHelper.AddEventTriggers(hue.gameObject, TriggerHelper.ScrollDelta(hue));
                            TriggerHelper.IncreaseDecreaseButtons(hue);

                            var sat = kfdialog.Find("gradient_huesatval/y").GetComponent<InputField>();

                            sat.onValueChanged.RemoveAllListeners();
                            sat.text = firstKF.GetData<EventKeyframe>().eventValues[8].ToString();
                            sat.onValueChanged.AddListener(_val =>
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    firstKF.GetData<EventKeyframe>().eventValues[8] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        Updater.UpdateObject(beatmapObject, "Keyframes");
                                }
                            });

                            TriggerHelper.AddEventTriggers(sat.gameObject, TriggerHelper.ScrollDelta(sat));
                            TriggerHelper.IncreaseDecreaseButtons(sat);

                            var val = kfdialog.Find("gradient_huesatval/z").GetComponent<InputField>();

                            val.onValueChanged.RemoveAllListeners();
                            val.text = firstKF.GetData<EventKeyframe>().eventValues[9].ToString();
                            val.onValueChanged.AddListener(_val =>
                            {
                                if (float.TryParse(_val, out float n))
                                {
                                    firstKF.GetData<EventKeyframe>().eventValues[9] = n;

                                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        Updater.UpdateObject(beatmapObject, "Keyframes");
                                }
                            });

                            TriggerHelper.AddEventTriggers(val.gameObject, TriggerHelper.ScrollDelta(val));
                            TriggerHelper.IncreaseDecreaseButtons(val);
                        }

                        break;
                    }
            }

            var relativeBase = kfdialog.Find("relative");

            if (!relativeBase)
                return;

            RTEditor.SetActive(relativeBase.gameObject, RTEditor.ShowModdedUI);
            if (RTEditor.ShowModdedUI)
            {
                var relative = relativeBase.GetComponent<Toggle>();
                relative.onValueChanged.ClearAll();
                relative.isOn = firstKF.GetData<EventKeyframe>().relative;
                relative.onValueChanged.AddListener(_val =>
                {
                    foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
                        keyframe.relative = _val;

                    // Since keyframe value has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        Updater.UpdateObject(beatmapObject, "Keyframes");
                });
            }
        }

        public void RenderMarkers(BeatmapObject beatmapObject)
        {
            var parent = ObjEditor.inst.objTimelineSlider.transform.Find("Markers");

            var dottedLine = ObjEditor.inst.KeyframeEndPrefab.GetComponent<Image>().sprite;
            LSHelpers.DeleteChildren(parent);

            for (int i = 0; i < GameData.Current.beatmapData.markers.Count; i++)
            {
                var marker = GameData.Current.beatmapData.markers[i];
                var length = beatmapObject.GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset);
                if (marker.time < beatmapObject.StartTime || marker.time > beatmapObject.StartTime + length)
                    continue;
                int index = i;

                var gameObject = MarkerEditor.inst.markerPrefab.Duplicate(parent, $"Marker {index}");
                var pos = (marker.time - beatmapObject.StartTime) / length;
                UIManager.SetRectTransform(gameObject.transform.AsRT(), new Vector2(0f, -12f), new Vector2(pos, 1f), new Vector2(pos, 1f), new Vector2(0.5f, 1f), new Vector2(12f, 12f));

                gameObject.GetComponent<Image>().color = MarkerEditor.inst.markerColors[Mathf.Clamp(marker.color, 0, MarkerEditor.inst.markerColors.Count - 1)];
                gameObject.GetComponentInChildren<Text>().text = marker.name;
                var line = gameObject.transform.Find("line").GetComponent<Image>();
                line.rectTransform.sizeDelta = new Vector2(5f, 301f);
                line.sprite = dottedLine;
                line.type = Image.Type.Tiled;

                TriggerHelper.AddEventTriggers(gameObject, TriggerHelper.CreateEntry(EventTriggerType.PointerClick, eventData =>
                {
                    var pointerEventData = (PointerEventData)eventData;

                    if (!marker.timelineMarker)
                        return;

                    switch (pointerEventData.button)
                    {
                        case PointerEventData.InputButton.Left:
                            {
                                //RTMarkerEditor.inst.SetCurrentMarker(marker.timelineMarker);
                                AudioManager.inst.SetMusicTimeWithDelay(Mathf.Clamp(marker.time, 0f, AudioManager.inst.CurrentAudioSource.clip.length), 0.05f);
                                break;
                            }
                        case PointerEventData.InputButton.Right:
                            {
                                RTMarkerEditor.inst.ShowMarkerContextMenu(marker.timelineMarker);
                                break;
                            }
                        case PointerEventData.InputButton.Middle:
                            {
                                if (EditorConfig.Instance.MarkerDragButton.Value == PointerEventData.InputButton.Middle)
                                    return;

                                AudioManager.inst.SetMusicTime(Mathf.Clamp(marker.time, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
                                break;
                            }
                    }
                }));
            }
        }

        public void OpenImageSelector(BeatmapObject beatmapObject)
        {
            var editorPath = RTFile.RemoveEndSlash(RTEditor.inst.CurrentLevel.path);
            string jpgFile = FileBrowser.OpenSingleFile("Select an image!", editorPath, new string[] { "png", "jpg" });
            SelectImage(jpgFile, beatmapObject);
        }

        void SelectImage(string file, BeatmapObject beatmapObject)
        {
            var editorPath = RTFile.RemoveEndSlash(RTEditor.inst.CurrentLevel.path);
            file = RTFile.ReplaceSlash(file);
            CoreHelper.Log($"Selected file: {file}");
            if (string.IsNullOrEmpty(file))
                return;

            string jpgFileLocation = RTFile.CombinePaths(editorPath, Path.GetFileName(file));

            var levelPath = file.Remove(editorPath + "/");

            if (!RTFile.FileExists(jpgFileLocation) && !file.Contains(editorPath))
                RTFile.CopyFile(file, jpgFileLocation);
            else
                jpgFileLocation = editorPath + "/" + levelPath;

            beatmapObject.text = jpgFileLocation.Remove(jpgFileLocation.Substring(0, jpgFileLocation.LastIndexOf('/') + 1));

            // Since setting image has no affect on the timeline object, we will only need to update the physical object.
            if (UpdateObjects)
                Updater.UpdateObject(beatmapObject, "Shape");

            RenderShape(beatmapObject);
        }

        #endregion

        #region Keyframe Handlers

        public GameObject keyframeEnd;

        public static bool AllowTimeExactlyAtStart => false;
        public void ResizeKeyframeTimeline(BeatmapObject beatmapObject)
        {
            // ObjEditor.inst.ObjectLengthOffset is the offset from the last keyframe. Could allow for more timeline space.
            float objectLifeLength = beatmapObject.GetObjectLifeLength();
            float x = TimeTimelineCalc(objectLifeLength + ObjEditor.inst.ObjectLengthOffset);

            ObjEditor.inst.objTimelineContent.AsRT().sizeDelta = new Vector2(x, 0f);
            ObjEditor.inst.objTimelineGrid.AsRT().sizeDelta = new Vector2(x, 122f);

            // Whether the value should clamp at 0.001 over StartTime or not.
            ObjEditor.inst.objTimelineSlider.minValue = AllowTimeExactlyAtStart ? beatmapObject.StartTime : beatmapObject.StartTime + 0.001f;
            ObjEditor.inst.objTimelineSlider.maxValue = beatmapObject.StartTime + objectLifeLength + ObjEditor.inst.ObjectLengthOffset;

            if (!keyframeEnd)
            {
                ObjEditor.inst.objTimelineGrid.DeleteChildren();
                keyframeEnd = ObjEditor.inst.KeyframeEndPrefab.Duplicate(ObjEditor.inst.objTimelineGrid, "end keyframe");
            }

            var rectTransform = keyframeEnd.transform.AsRT();
            rectTransform.sizeDelta = new Vector2(4f, 122f);
            rectTransform.anchoredPosition = new Vector2(objectLifeLength * ObjEditor.inst.Zoom * 14f, 0f);
        }

        public void ClearKeyframes(BeatmapObject beatmapObject)
        {
            var timelineObject = EditorTimeline.inst.GetTimelineObject(beatmapObject);

            foreach (var kf in timelineObject.InternalTimelineObjects)
                Destroy(kf.GameObject);
        }

        public TimelineObject GetKeyframe(BeatmapObject beatmapObject, int type, int index)
        {
            var bmTimelineObject = EditorTimeline.inst.GetTimelineObject(beatmapObject);

            var kf = bmTimelineObject.InternalTimelineObjects.Find(x => x.Type == type && x.Index == index);

            if (!kf)
                kf = bmTimelineObject.InternalTimelineObjects.Find(x => x.ID == (beatmapObject.events[type][index] as EventKeyframe).id);

            if (!kf)
            {
                kf = CreateKeyframe(beatmapObject, type, index);
                bmTimelineObject.InternalTimelineObjects.Add(kf);
            }

            if (!kf.GameObject)
            {
                kf.GameObject = KeyframeObject(beatmapObject, kf);
                kf.Image = kf.GameObject.transform.GetChild(0).GetComponent<Image>();
                kf.RenderVisibleState();
            }

            return kf;
        }

        public void CreateKeyframes(BeatmapObject beatmapObject)
        {
            ClearKeyframes(beatmapObject);

            if (!beatmapObject.timelineObject)
                return;

            for (int i = 0; i < beatmapObject.events.Count; i++)
            {
                if (beatmapObject.events[i].Count <= 0)
                    return;

                for (int j = 0; j < beatmapObject.events[i].Count; j++)
                {
                    var keyframe = (EventKeyframe)beatmapObject.events[i][j];
                    var kf = beatmapObject.timelineObject.InternalTimelineObjects.Find(x => x.ID == keyframe.id);
                    if (!kf)
                    {
                        kf = CreateKeyframe(beatmapObject, i, j);
                        beatmapObject.timelineObject.InternalTimelineObjects.Add(kf);
                    }

                    if (!kf.GameObject)
                    {
                        kf.GameObject = KeyframeObject(beatmapObject, kf);
                        kf.Image = kf.GameObject.transform.GetChild(0).GetComponent<Image>();
                        kf.RenderVisibleState();
                    }

                    RenderKeyframe(beatmapObject, kf);
                }
            }
        }

        public TimelineObject CreateKeyframe(BeatmapObject beatmapObject, int type, int index)
        {
            var eventKeyframe = beatmapObject.events[type][index];

            var kf = new TimelineObject(eventKeyframe)
            {
                Type = type,
                Index = index,
                isObjectKeyframe = true
            };

            if (eventKeyframe is EventKeyframe eventKF)
                eventKF.timelineObject = kf;

            kf.GameObject = KeyframeObject(beatmapObject, kf);
            kf.Image = kf.GameObject.transform.GetChild(0).GetComponent<Image>();
            kf.RenderVisibleState();

            return kf;
        }

        public GameObject KeyframeObject(BeatmapObject beatmapObject, TimelineObject kf)
        {
            var gameObject = ObjEditor.inst.objTimelinePrefab.Duplicate(ObjEditor.inst.TimelineParents[kf.Type], $"{IntToType(kf.Type)}_{kf.Index}");

            var button = gameObject.GetComponent<Button>();
            button.onClick.ClearAll();
            button.onClick.AddListener(() =>
            {
                if (!Input.GetMouseButtonDown(2))
                    SetCurrentKeyframe(beatmapObject, kf.Type, kf.Index, false, InputDataManager.inst.editorActions.MultiSelect.IsPressed);
            });

            TriggerHelper.AddEventTriggers(gameObject,
                TriggerHelper.CreateKeyframeStartDragTrigger(beatmapObject, kf),
                TriggerHelper.CreateKeyframeEndDragTrigger(beatmapObject, kf),
                TriggerHelper.CreateKeyframeSelectTrigger(beatmapObject, kf));

            return gameObject;
        }

        public void RenderKeyframes(BeatmapObject beatmapObject)
        {
            for (int i = 0; i < beatmapObject.events.Count; i++)
            {
                for (int j = 0; j < beatmapObject.events[i].Count; j++)
                {
                    var kf = GetKeyframe(beatmapObject, i, j);

                    RenderKeyframe(beatmapObject, kf);
                }
            }

            var timelineObject = EditorTimeline.inst.GetTimelineObject(beatmapObject);
            if (timelineObject.InternalTimelineObjects.Count > 0 && timelineObject.InternalTimelineObjects.Where(x => x.Selected).Count() == 0)
            {
                if (EditorConfig.Instance.RememberLastKeyframeType.Value && timelineObject.InternalTimelineObjects.TryFind(x => x.Type == ObjEditor.inst.currentKeyframeKind, out TimelineObject kf))
                    kf.Selected = true;
                else
                    timelineObject.InternalTimelineObjects[0].Selected = true;
            }

            if (timelineObject.InternalTimelineObjects.Count >= 1000)
                AchievementManager.inst.UnlockAchievement("holy_keyframes");
        }

        public void RenderKeyframe(BeatmapObject beatmapObject, TimelineObject timelineObject)
        {
            if (beatmapObject.events[timelineObject.Type].TryFindIndex(x => (x as EventKeyframe).id == timelineObject.ID, out int kfIndex))
                timelineObject.Index = kfIndex;

            var eventKeyframe = timelineObject.GetData<EventKeyframe>();
            timelineObject.RenderSprite(beatmapObject.events[timelineObject.Type]);
            timelineObject.RenderPosLength(ObjEditor.inst.zoomVal, 0f, eventKeyframe.eventTime);
            timelineObject.RenderIcons();
        }

        public void UpdateKeyframeOrder(BeatmapObject beatmapObject)
        {
            for (int i = 0; i < beatmapObject.events.Count; i++)
            {
                beatmapObject.events[i] = (from x in beatmapObject.events[i]
                                           orderby x.eventTime
                                           select x).ToList();
            }

            RenderKeyframes(beatmapObject);
        }

        public static string IntToAxis(int num) => num switch
        {
            0 => "x",
            1 => "y",
            2 => "z",
            _ => string.Empty,
        };

        public static string IntToType(int num) => num switch
        {
            0 => "pos",
            1 => "sca",
            2 => "rot",
            3 => "col",
            _ => string.Empty,
        };

        #endregion

        #region Set Values

        public void SetKeyframeColor(BeatmapObject beatmapObject, int index, int value, List<Toggle> colorButtons, IEnumerable<TimelineObject> selected)
        {
            foreach (var keyframe in selected.Select(x => x.GetData<EventKeyframe>()))
            {
                keyframe.eventValues[index] = value;
                if (!RTEditor.ShowModdedUI)
                    keyframe.eventValues[6] = 10f; // set behaviour to alpha's default if editor complexity is not set to advanced.
            }

            // Since keyframe color has no affect on the timeline object, we will only need to update the physical object.
            if (UpdateObjects)
                Updater.UpdateObject(beatmapObject, "Keyframes");

            int num = 0;
            foreach (var toggle in colorButtons)
            {
                int tmpIndex = num;
                toggle.onValueChanged.ClearAll();
                toggle.isOn = num == value;
                toggle.onValueChanged.AddListener(_val => { SetKeyframeColor(beatmapObject, index, tmpIndex, colorButtons, selected); });
                num++;
            }
        }

        public void SetKeyframeRandomColorTarget(BeatmapObject beatmapObject, int index, int value, Toggle[] toggles)
        {
            beatmapObject.events[3][ObjEditor.inst.currentKeyframe].eventRandomValues[index] = (float)value;

            // Since keyframe color has no affect on the timeline object, we will only need to update the physical object.
            Updater.UpdateObject(beatmapObject, "Keyframes");

            int num = 0;
            foreach (var toggle in toggles)
            {
                int tmpIndex = num;
                toggle.onValueChanged.ClearAll();
                toggle.isOn = num == value;
                toggle.onValueChanged.AddListener(_val => { SetKeyframeRandomColorTarget(beatmapObject, index, tmpIndex, toggles); });
                num++;
            }
        }

        #endregion
    }

    public class ObjectEditorDialog : EditorDialog
    {
        #region Object Properties

        public RectTransform Content { get; set; }

        public override void Init()
        {
            if (init)
                return;

            Name = "Object Editor";
            GameObject = EditorManager.inst.GetDialog(Name).Dialog.gameObject;
            Content = ObjEditor.inst.ObjectView.transform.AsRT();

            #region Top Properties

            IDBase = Content.Find("id").AsRT();
            IDText = IDBase.Find("text").GetComponent<Text>();
            LDMToggle = IDBase.Find("ldm").GetComponent<Toggle>();

            #endregion

            #region Name Area

            NameField = Content.Find("name/name").GetComponent<InputField>();
            ObjectTypeDropdown = Content.Find("name/object-type").GetComponent<Dropdown>();
            TagsContent = Content.Find("Tags Scroll View/Viewport/Content").AsRT();

            #endregion

            #region Start Time

            StartTimeField = Content.Find("time").gameObject.AddComponent<InputFieldStorage>();
            StartTimeField.Assign(StartTimeField.gameObject);

            #endregion

            #region Autokill

            AutokillDropdown = Content.Find("autokill/tod-dropdown").GetComponent<Dropdown>();
            AutokillField = Content.Find("autokill/tod-value").GetComponent<InputField>();
            AutokillSetButton = Content.Find("autokill/|").GetComponent<Button>();
            CollapseToggle = Content.Find("autokill/collapse").GetComponent<Toggle>();

            #endregion

            #region Parent

            ParentButton = Content.Find("parent/text").gameObject.AddComponent<FunctionButtonStorage>();
            ParentButton.button = ParentButton.GetComponent<Button>();
            ParentButton.text = ParentButton.transform.Find("text").GetComponent<Text>();
            ParentInfo = ParentButton.GetComponent<HoverTooltip>();
            ParentMoreButton = Content.Find("parent/more").GetComponent<Button>();
            ParentSettingsParent = Content.Find("parent_more").gameObject;
            ParentDesyncToggle = ParentSettingsParent.transform.Find("spawn_once").GetComponent<Toggle>();
            ParentSearchButton = Content.Find("parent/parent").GetComponent<Button>();
            ParentClearButton = Content.Find("parent/clear parent").GetComponent<Button>();
            ParentPickerButton = Content.Find("parent/parent picker").GetComponent<Button>();

            for (int i = 0; i < 3; i++)
            {
                var name = i switch
                {
                    0 => "pos",
                    1 => "sca",
                    _ => "rot"
                };

                var row = ParentSettingsParent.transform.Find($"{name}_row");
                var parentSetting = new ParentSetting();
                parentSetting.row = row;
                parentSetting.label = row.Find("text").GetComponent<Text>();
                parentSetting.activeToggle = row.Find(name).GetComponent<Toggle>();
                parentSetting.offsetField = row.Find($"{name}_offset").GetComponent<InputField>();
                parentSetting.additiveToggle = row.Find($"{name}_add").GetComponent<Toggle>();
                parentSetting.parallaxField = row.Find($"{name}_parallax").GetComponent<InputField>();
                ParentSettings.Add(parentSetting);
            }

            #endregion

            #region Origin

            OriginParent = Content.Find("origin").AsRT();
            OriginXField = OriginParent.Find("x").gameObject.GetOrAddComponent<InputFieldStorage>();
            OriginXField.Assign(OriginXField.gameObject);
            OriginYField = OriginParent.Find("y").gameObject.GetOrAddComponent<InputFieldStorage>();
            OriginYField.Assign(OriginYField.gameObject);

            for (int i = 0; i < 3; i++)
            {
                OriginXToggles.Add(OriginParent.Find("origin-x").GetChild(i).GetComponent<Toggle>());
                OriginYToggles.Add(OriginParent.Find("origin-y").GetChild(i).GetComponent<Toggle>());
            }

            #endregion

            #region Gradient / Shape

            GradientParent = Content.Find("gradienttype").AsRT();
            for (int i = 0; i < GradientParent.childCount; i++)
                GradientToggles.Add(GradientParent.GetChild(i).GetComponent<Toggle>());
            ShapeTypesParent = Content.Find("shape").AsRT();
            ShapeOptionsParent = Content.Find("shapesettings").AsRT();

            #endregion

            #region Render Depth / Type

            DepthParent = Content.Find("depth").AsRT();
            DepthField = Content.Find("depth input/depth").gameObject.GetOrAddComponent<InputFieldStorage>();
            DepthField.Assign(DepthField.gameObject);
            DepthSlider = Content.Find("depth/depth").GetComponent<Slider>();
            DepthSliderLeftButton = DepthParent.Find("<").GetComponent<Button>();
            DepthSliderRightButton = DepthParent.Find(">").GetComponent<Button>();
            RenderTypeDropdown = Content.Find("rendertype").GetComponent<Dropdown>();

            #endregion

            #region Editor Settings

            EditorSettingsParent = Content.Find("editor").AsRT();
            EditorLayerField = EditorSettingsParent.Find("layers")?.GetComponent<InputField>();
            EditorLayerField.image = EditorLayerField.GetComponent<Image>();
            BinSlider = EditorSettingsParent.Find("bin")?.GetComponent<Slider>();

            #endregion

            #region Prefab

            CollapsePrefabLabel = Content.Find("collapselabel").gameObject;
            CollapsePrefabButton = Content.Find("applyprefab").gameObject.GetOrAddComponent<FunctionButtonStorage>();
            CollapsePrefabButton.text = CollapsePrefabButton.transform.Find("Text").GetComponent<Text>();
            CollapsePrefabButton.button = CollapsePrefabButton.GetComponent<Button>();

            AssignPrefabLabel = Content.Find("assignlabel").gameObject;
            AssignPrefabButton = Content.Find("assign prefab").gameObject.GetOrAddComponent<FunctionButtonStorage>();
            AssignPrefabButton.text = AssignPrefabButton.transform.Find("Text").GetComponent<Text>();
            AssignPrefabButton.button = AssignPrefabButton.GetComponent<Button>();
            RemovePrefabButton = Content.Find("remove prefab").gameObject.GetOrAddComponent<FunctionButtonStorage>();
            RemovePrefabButton.text = RemovePrefabButton.transform.Find("Text").GetComponent<Text>();
            RemovePrefabButton.button = RemovePrefabButton.GetComponent<Button>();

            #endregion

            init = true;
        }

        #region Top Properties

        public RectTransform IDBase { get; set; }
        public Text IDText { get; set; }
        public Toggle LDMToggle { get; set; }

        #endregion

        #region Name Area

        public InputField NameField { get; set; }
        public Dropdown ObjectTypeDropdown { get; set; }
        public RectTransform TagsContent { get; set; }

        #endregion

        #region Start Time / Autokill

        public InputFieldStorage StartTimeField { get; set; }

        public Dropdown AutokillDropdown { get; set; }
        public InputField AutokillField { get; set; }
        public Button AutokillSetButton { get; set; }
        public Toggle CollapseToggle { get; set; }

        #endregion

        #region Parent

        public FunctionButtonStorage ParentButton { get; set; }
        public HoverTooltip ParentInfo { get; set; }
        public Button ParentMoreButton { get; set; }
        public GameObject ParentSettingsParent { get; set; }
        public Toggle ParentDesyncToggle { get; set; }
        public Button ParentSearchButton { get; set; }
        public Button ParentClearButton { get; set; }
        public Button ParentPickerButton { get; set; }

        public List<ParentSetting> ParentSettings { get; set; } = new List<ParentSetting>();

        #endregion

        #region Origin

        public RectTransform OriginParent { get; set; }
        public InputFieldStorage OriginXField { get; set; }
        public InputFieldStorage OriginYField { get; set; }

        public List<Toggle> OriginXToggles { get; set; } = new List<Toggle>();
        public List<Toggle> OriginYToggles { get; set; } = new List<Toggle>();

        #endregion

        #region Gradient / Shape

        public RectTransform GradientParent { get; set; }
        public List<Toggle> GradientToggles { get; set; } = new List<Toggle>();
        public RectTransform ShapeTypesParent { get; set; }
        public RectTransform ShapeOptionsParent { get; set; }

        #endregion

        #region Render Depth / Type

        public RectTransform DepthParent { get; set; }
        public InputFieldStorage DepthField { get; set; }
        public Slider DepthSlider { get; set; }
        public Button DepthSliderLeftButton { get; set; }
        public Button DepthSliderRightButton { get; set; }
        public Dropdown RenderTypeDropdown { get; set; }

        #endregion

        #region Editor Settings

        public RectTransform EditorSettingsParent { get; set; }
        public Slider BinSlider { get; set; }
        public InputField EditorLayerField { get; set; }

        #endregion

        #region Prefab

        public GameObject CollapsePrefabLabel { get; set; }
        public FunctionButtonStorage CollapsePrefabButton { get; set; }
        public GameObject AssignPrefabLabel { get; set; }
        public FunctionButtonStorage AssignPrefabButton { get; set; }
        public FunctionButtonStorage RemovePrefabButton { get; set; }

        #endregion

        #endregion

        public List<KeyframeDialog> keyframeDialogs = new List<KeyframeDialog>();
    }

    public class KeyframeDialog
    {
        public GameObject GameObject { get; set; }
        public Dropdown CurvesDropdown { get; set; }
        public InputFieldStorage EventTimeField { get; set; }

        public FunctionButtonStorage CopyButton { get; set; }
        public FunctionButtonStorage PasteButton { get; set; }
        public DeleteButtonStorage DeleteButton { get; set; }

        public List<InputFieldStorage> EventValueFields { get; set; } = new List<InputFieldStorage>();
    }

    public class ParentSetting
    {
        public Transform row;
        public Text label;
        public Toggle activeToggle;
        public InputField offsetField;
        public Toggle additiveToggle;
        public InputField parallaxField;
    }
}
