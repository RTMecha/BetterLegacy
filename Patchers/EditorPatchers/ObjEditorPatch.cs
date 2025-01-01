using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Managers;
using HarmonyLib;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BaseEventKeyframe = DataManager.GameData.EventKeyframe;
using BasePrefab = DataManager.GameData.Prefab;
using BasePrefabObject = DataManager.GameData.PrefabObject;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(ObjEditor))]
    public class ObjEditorPatch : MonoBehaviour
    {
        static ObjEditor Instance { get => ObjEditor.inst; set => ObjEditor.inst = value; }

        [HarmonyPatch(nameof(ObjEditor.Awake))]
        [HarmonyPrefix]
        static bool AwakePrefix(ObjEditor __instance)
        {
            // og code
            {
                if (!Instance)
                    Instance = __instance;
                else if (Instance != __instance)
                {
                    Destroy(__instance.gameObject);
                    return false;
                }

                CoreHelper.LogInit(__instance.className);

                var beginDragTrigger = TriggerHelper.CreateEntry(EventTriggerType.BeginDrag, eventData =>
                {
                    var pointerEventData = (PointerEventData)eventData;
                    __instance.SelectionBoxImage.gameObject.SetActive(true);
                    __instance.DragStartPos = pointerEventData.position * EditorManager.inst.ScreenScaleInverse;
                    __instance.SelectionRect = default;
                });

                var dragTrigger = TriggerHelper.CreateEntry(EventTriggerType.Drag, eventData =>
                {
                    var vector = ((PointerEventData)eventData).position * EditorManager.inst.ScreenScaleInverse;

                    __instance.SelectionRect.xMin = vector.x < __instance.DragStartPos.x ? vector.x : __instance.DragStartPos.x;
                    __instance.SelectionRect.xMax = vector.x < __instance.DragStartPos.x ? __instance.DragStartPos.x : vector.x;
                    __instance.SelectionRect.yMin = vector.y < __instance.DragStartPos.y ? vector.y : __instance.DragStartPos.y;
                    __instance.SelectionRect.yMax = vector.y < __instance.DragStartPos.y ? __instance.DragStartPos.y : vector.y;

                    __instance.SelectionBoxImage.rectTransform.offsetMin = __instance.SelectionRect.min;
                    __instance.SelectionBoxImage.rectTransform.offsetMax = __instance.SelectionRect.max;
                });

                var endDragTrigger = TriggerHelper.CreateEntry(EventTriggerType.EndDrag, eventData =>
                {
                    var pointerEventData = (PointerEventData)eventData;
                    __instance.DragEndPos = pointerEventData.position;
                    __instance.SelectionBoxImage.gameObject.SetActive(false);

                    CoreHelper.StartCoroutine(ObjectEditor.inst.GroupSelectKeyframes(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)));
                });

                foreach (var gameObject in __instance.SelectionArea)
                    TriggerHelper.AddEventTriggers(gameObject, beginDragTrigger, dragTrigger, endDragTrigger);
            }

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

            for (int i = 0; i < __instance.KeyframeDialogs.Count; i++)
            {
                var kfdialog = __instance.KeyframeDialogs[i].transform;

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

            var labelToCopy = objectView.ChildList().First(x => x.name == "label").gameObject;

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

                var locker = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle").Duplicate(timeParent.transform, "lock", 0);
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
                var colorParent = __instance.KeyframeDialogs[3].transform.Find("color");
                colorParent.GetComponent<GridLayoutGroup>().spacing = new Vector2(9.32f, 9.32f);

                __instance.KeyframeDialogs[3].transform.GetChild(colorParent.GetSiblingIndex() - 1).gameObject.name = "color_label";

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
                var opacityLabel = __instance.KeyframeDialogs[3].transform.Find("label").gameObject.Duplicate(__instance.KeyframeDialogs[3].transform, "opacity_label");
                opacityLabel.transform.localScale = Vector3.one;
                var opacityLabelText = opacityLabel.transform.GetChild(0).GetComponent<Text>();
                opacityLabelText.text = "Opacity";

                EditorThemeManager.AddLightText(opacityLabelText);

                var opacity = Instantiate(__instance.KeyframeDialogs[2].transform.Find("rotation").gameObject);
                opacity.transform.SetParent(__instance.KeyframeDialogs[3].transform);
                opacity.transform.localScale = Vector3.one;
                opacity.name = "opacity";

                var collisionToggle = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/grain/colored").Duplicate(opacity.transform, "collision");

                var collisionToggleText = collisionToggle.transform.Find("Text").GetComponent<Text>();
                collisionToggleText.text = "Collide";
                opacity.transform.Find("x/input").AsRT().sizeDelta = new Vector2(136f, 32f);

                EditorThemeManager.AddInputFields(opacity, true, "");
                EditorThemeManager.AddToggle(collisionToggle.GetComponent<Toggle>(), graphic: collisionToggleText);
            }

            // Hue / Sat / Val
            {
                var hsvLabels = __instance.KeyframeDialogs[2].transform.Find("label").gameObject.Duplicate(__instance.KeyframeDialogs[3].transform, "huesatval_label");
                hsvLabels.transform.GetChild(0).GetComponent<Text>().text = "Hue";

                hsvLabels.AddComponent<HorizontalLayoutGroup>();

                var saturationLabel = hsvLabels.transform.GetChild(0).gameObject.Duplicate(hsvLabels.transform);
                saturationLabel.GetComponent<Text>().text = "Saturation";

                var valueLabel = hsvLabels.transform.GetChild(0).gameObject.Duplicate(hsvLabels.transform);
                valueLabel.GetComponent<Text>().text = "Value";

                var opacity = __instance.KeyframeDialogs[1].transform.Find("scale").gameObject.Duplicate(__instance.KeyframeDialogs[3].transform);
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
                    
                    var colorDialog = __instance.KeyframeDialogs[3].transform;

                    var di = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/grain").transform;
                    var shift = di.GetChild(13).gameObject.Duplicate(colorDialog, "shift", 16);
                    var text = shift.transform.GetChild(1).GetComponent<Text>();
                    text.text = "Shift Dialog Down";
                    var shiftToggle = shift.GetComponent<Toggle>();
                    shiftToggle.onValueChanged.ClearAll();
                    shiftToggle.isOn = false;
                    shiftToggle.onValueChanged.AddListener(_val =>
                    {
                        ObjectEditor.inst.colorShifted = _val;
                        text.text = _val ? "Shift Dialog Up" : "Shift Dialog Down";
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

                    EditorThemeManager.AddSelectable(shiftToggle, ThemeGroup.Function_2);
                    EditorThemeManager.AddGraphic(text, ThemeGroup.Function_2_Text);

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
                    endHSVLabel.transform.GetChild(1).GetComponent<Text>().text = "End Saturation";
                    endHSVLabel.transform.GetChild(2).GetComponent<Text>().text = "End Value";
                    var endHSV = colorDialog.Find("huesatval").gameObject.Duplicate(colorDialog, "gradient_huesatval");
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

                DestroyImmediate(ObjEditor.inst.KeyframeDialogs[0].transform.Find("position/x/input").GetComponent<LayoutElement>());
                DestroyImmediate(ObjEditor.inst.KeyframeDialogs[0].transform.Find("position/y/input").GetComponent<LayoutElement>());
                DestroyImmediate(ObjEditor.inst.KeyframeDialogs[0].transform.Find("position/z/input").GetComponent<LayoutElement>());

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
                    if (!ObjectEditor.inst)
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
                objectView.GetChild(objectView.Find("spacer") ? 18 : 17).GetChild(1).gameObject.SetActive(true);

                Destroy(objectView.Find("editor/layer").gameObject);

                var layers = Instantiate(objectView.Find("time/time").gameObject);

                layers.transform.SetParent(objectView.transform.Find("editor"));
                layers.name = "layers";
                layers.transform.SetSiblingIndex(0);
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
                var close = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/Open File Popup/Panel/x");

                var parent = objectView.Find("parent");
                var hlg = parent.GetComponent<HorizontalLayoutGroup>();
                hlg.childControlWidth = false;
                hlg.spacing = 4f;

                parent.transform.Find("text").AsRT().sizeDelta = new Vector2(201f, 32f);

                var resetParent = close.Duplicate(parent.transform, "clear parent", 1);

                var resetParentButton = resetParent.GetComponent<Button>();

                var parentPicker = close.Duplicate(parent.transform, "parent picker", 2);

                var parentPickerButton = parentPicker.GetComponent<Button>();

                parentPickerButton.onClick.ClearAll();
                parentPickerButton.onClick.AddListener(() => { RTEditor.inst.parentPickerEnabled = true; });

                var parentPickerIcon = parentPicker.transform.GetChild(0).GetComponent<Image>();

                if (parentPicker.transform.childCount >= 0
                    && CoreHelper.TryFind("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/left/theme/theme/viewport/content/player0/preview/dropper",
                    out GameObject dropper)
                    && dropper.TryGetComponent(out Image dropperImage))
                    parentPickerIcon.sprite = dropperImage.sprite;

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
                var id = objectView.GetChild(0).gameObject.Duplicate(objectView, "id", 0);
                Destroy(id.transform.GetChild(1).gameObject);
                EditorHelper.SetComplexity(id, Complexity.Normal);

                id.transform.AsRT().sizeDelta = new Vector2(515, 32f);
                id.transform.GetChild(0).AsRT().sizeDelta = new Vector2(226f, 32f);

                var text = id.transform.GetChild(0).GetComponent<Text>();
                text.fontSize = 18;
                text.text = "ID:";
                text.alignment = TextAnchor.MiddleLeft;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;

                if (!id.GetComponent<Image>())
                {
                    var image = id.AddComponent<Image>();
                    image.color = new Color(1f, 1f, 1f, 0.07f);
                }

                var ldm = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle").Duplicate(id.transform, "ldm");

                ldm.transform.Find("title").AsRT().sizeDelta = new Vector2(44f, 32f);
                var ldmText = ldm.transform.Find("title").GetComponent<Text>();
                ldmText.text = "LDM";

                EditorThemeManager.AddLightText(text);
                EditorThemeManager.AddLightText(ldmText);
                EditorThemeManager.AddToggle(ldm.transform.Find("toggle").GetComponent<Toggle>());
            }

            // Relative / Copy / Paste
            {
                var button = GameObject.Find("TimelineBar/GameObject/event");
                for (int i = 0; i < 4; i++)
                {
                    var parent = ObjEditor.inst.KeyframeDialogs[i].transform;
                    if (i != 3)
                    {
                        var di = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/grain").transform;
                        var toggleLabel = di.GetChild(12).gameObject.Duplicate(parent, "relative-label");
                        var toggleLabelText = toggleLabel.transform.GetChild(0).GetComponent<Text>();
                        toggleLabelText.text = "Value Additive";
                        var toggle = di.GetChild(13).gameObject.Duplicate(parent, "relative");
                        var toggleText = toggle.transform.GetChild(1).GetComponent<Text>();
                        toggleText.text = "Relative";

                        EditorThemeManager.AddLightText(toggleLabelText);
                        EditorThemeManager.AddToggle(toggle.GetComponent<Toggle>(), graphic: toggleText);

                        var flipX = button.Duplicate(parent, "flipx");
                        var flipXText = flipX.transform.GetChild(0).GetComponent<Text>();
                        flipXText.text = "Flip X";
                        ((RectTransform)flipX.transform).sizeDelta = new Vector2(366f, 32f);
                        var flipXButton = flipX.GetComponent<Button>();

                        flipXButton.onClick.ClearAll();
                        flipXButton.onClick.AddListener(() =>
                        {
                            foreach (var timelineObject in ObjectEditor.inst.CurrentSelection.InternalTimelineObjects.Where(x => x.Selected))
                            {
                                var eventKeyframe = timelineObject.GetData<EventKeyframe>();
                                eventKeyframe.eventValues[0] = -eventKeyframe.eventValues[0];
                            }

                            var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
                            Updater.UpdateObject(beatmapObject, "Keyframes");
                            ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
                        });

                        EditorThemeManager.AddGraphic(flipXButton.image, ThemeGroup.Function_1, true);
                        EditorThemeManager.AddGraphic(flipXText, ThemeGroup.Function_1_Text);

                        EditorHelper.SetComplexity(flipX, Complexity.Normal);

                        if (i != 2)
                        {
                            var flipY = button.Duplicate(parent, "flipy");
                            var flipYText = flipY.transform.GetChild(0).GetComponent<Text>();
                            flipYText.text = "Flip Y";
                            ((RectTransform)flipY.transform).sizeDelta = new Vector2(366f, 32f);
                            var flipYButton = flipY.GetComponent<Button>();

                            flipYButton.onClick.ClearAll();
                            flipYButton.onClick.AddListener(() =>
                            {
                                foreach (var timelineObject in ObjectEditor.inst.CurrentSelection.InternalTimelineObjects.Where(x => x.Selected))
                                {
                                    var eventKeyframe = timelineObject.GetData<EventKeyframe>();
                                    eventKeyframe.eventValues[1] = -eventKeyframe.eventValues[1];
                                }

                                var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
                                Updater.UpdateObject(beatmapObject, "Keyframes");
                                ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
                            });

                            EditorThemeManager.AddGraphic(flipYButton.image, ThemeGroup.Function_1, true);
                            EditorThemeManager.AddGraphic(flipYText, ThemeGroup.Function_1_Text);

                            EditorHelper.SetComplexity(flipY, Complexity.Normal);
                        }
                    }

                    var edit = parent.Find("edit");
                    EditorHelper.SetComplexity(edit.Find("spacer").gameObject, Complexity.Simple);

                    var copy = button.Duplicate(edit, "copy", 5);
                    var copyText = copy.transform.GetChild(0).GetComponent<Text>();
                    copyText.text = "Copy";
                    copy.transform.AsRT().sizeDelta = new Vector2(70f, 32f);

                    var paste = button.Duplicate(edit, "paste", 6);
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

                var rAxis = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown")
                    .Duplicate(position, "r_axis", 14);
                var rAxisDD = rAxis.GetComponent<Dropdown>();
                rAxisDD.options = CoreHelper.StringToOptionData("Both", "X Only", "Y Only");

                EditorThemeManager.AddDropdown(rAxisDD);
            }

            // Object Tags
            {
                var label = objectView.ChildList().First(x => x.name == "label").gameObject.Duplicate(objectView, "tags_label");
                var index = objectView.Find("name").GetSiblingIndex() + 1;
                label.transform.SetSiblingIndex(index);

                Destroy(label.transform.GetChild(1).gameObject);
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
                var label = objectView.ChildList().First(x => x.name == "label").gameObject.Duplicate(objectView, "rendertype_label");
                var index = objectView.Find("depth").GetSiblingIndex() + 1;
                label.transform.SetSiblingIndex(index);

                Destroy(label.transform.GetChild(1).gameObject);
                var labelText = label.transform.GetChild(0).GetComponent<Text>();
                labelText.text = "Render Type";
                EditorThemeManager.AddLightText(labelText);

                var renderType = objectView.Find("autokill/tod-dropdown").gameObject
                    .Duplicate(objectView, "rendertype", index + 1);
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

                DestroyImmediate(GameObject.Find("Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right").GetComponent<VerticalLayoutGroup>());
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
                var rect = (RectTransform)shape;
                var scroll = shape.gameObject.AddComponent<ScrollRect>();
                shape.gameObject.AddComponent<Mask>();
                var image = shape.gameObject.AddComponent<Image>();

                scroll.horizontal = true;
                scroll.vertical = false;
                scroll.content = rect;
                scroll.viewport = rect;
                image.color = new Color(1f, 1f, 1f, 0.01f);
            }

            // Timeline Object adjustments
            {
                var gameObject = ObjEditor.inst.timelineObjectPrefab.Duplicate(null, ObjEditor.inst.timelineObjectPrefab.name);
                var icons = gameObject.transform.Find("icons");

                if (!icons.gameObject.GetComponent<HorizontalLayoutGroup>())
                {
                    var timelineObjectStorage = gameObject.AddComponent<TimelineObjectStorage>();

                    var @lock = ObjEditor.inst.timelineObjectPrefabLock.Duplicate(icons);
                    @lock.name = "lock";
                    ((RectTransform)@lock.transform).anchoredPosition = Vector3.zero;

                    var dots = ObjEditor.inst.timelineObjectPrefabDots.Duplicate(icons);
                    dots.name = "dots";
                    ((RectTransform)dots.transform).anchoredPosition = Vector3.zero;

                    var hlg = icons.gameObject.AddComponent<HorizontalLayoutGroup>();
                    hlg.childControlWidth = false;
                    hlg.childForceExpandWidth = false;
                    hlg.spacing = -4f;
                    hlg.childAlignment = TextAnchor.UpperRight;

                    ((RectTransform)@lock.transform).sizeDelta = new Vector2(20f, 20f);

                    ((RectTransform)dots.transform).sizeDelta = new Vector2(32f, 20f);

                    var b = new GameObject("type");
                    b.transform.SetParent(icons);
                    b.transform.localScale = Vector3.one;

                    var bRT = b.AddComponent<RectTransform>();
                    bRT.sizeDelta = new Vector2(20f, 20f);

                    var bImage = b.AddComponent<Image>();
                    bImage.color = new Color(0f, 0f, 0f, 0.45f);

                    var icon = new GameObject("type");
                    icon.transform.SetParent(b.transform);
                    icon.transform.localScale = Vector3.one;

                    var iconRT = icon.AddComponent<RectTransform>();
                    iconRT.anchoredPosition = Vector2.zero;
                    iconRT.sizeDelta = new Vector2(20f, 20f);

                    var iconImage = icon.AddComponent<Image>();

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
                    var button = GameObject.Find("TimelineBar/GameObject/event");

                    var select = objectView.Find("shapesettings/7/select").GetComponent<Button>();
                    Destroy(select.GetComponent<Animator>());
                    select.transition = Selectable.Transition.ColorTint;
                    EditorThemeManager.AddSelectable(select, ThemeGroup.Function_2, false);

                    EditorThemeManager.AddLightText(objectView.Find("shapesettings/7/text").GetComponent<Text>());

                    var setData = button.Duplicate(objectView.Find("shapesettings/7"), "set", 5);
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
                var ignoreGameObject = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/grain/colored").Duplicate(parentMore, "spawn_once", 1);
                ignoreGameObject.transform.localScale = Vector3.one;
                var spawnOnceText = ignoreGameObject.transform.Find("Text").GetComponent<Text>();
                spawnOnceText.text = "Parent Desync";

                EditorThemeManager.AddToggle(ignoreGameObject.GetComponent<Toggle>(), graphic: spawnOnceText);
                parentMore.AsRT().sizeDelta = new Vector2(351f, 152f);
            }

            // Assign Prefab
            {
                var collapseLabel = __instance.ObjectView.transform.Find("collapselabel");
                var applyPrefab = __instance.ObjectView.transform.Find("applyprefab");
                var siblingIndex = applyPrefab.GetSiblingIndex();
                var applyPrefabText = applyPrefab.transform.GetChild(0).GetComponent<Text>();

                var applyPrefabButton = applyPrefab.GetComponent<Button>();
                Destroy(applyPrefab.GetComponent<Animator>());
                applyPrefabButton.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddSelectable(applyPrefabButton, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(applyPrefabText, ThemeGroup.Function_2_Text);

                var assignPrefabLabel = collapseLabel.gameObject.Duplicate(__instance.ObjectView.transform, "assignlabel", siblingIndex + 1);
                var assignPrefabLabelText = assignPrefabLabel.transform.GetChild(0).GetComponent<Text>();
                assignPrefabLabelText.text = "Assign Object to a Prefab";
                EditorThemeManager.AddLightText(assignPrefabLabelText);
                var assignPrefab = applyPrefab.gameObject.Duplicate(__instance.ObjectView.transform, "assign prefab", siblingIndex + 2);
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

                var removePrefab = applyPrefab.gameObject.Duplicate(__instance.ObjectView.transform, "remove prefab", siblingIndex + 3);
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
                    var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
                    beatmapObject.RemovePrefabReference();
                    ObjectEditor.inst.RenderTimelineObject(ObjectEditor.inst.CurrentSelection);
                    ObjectEditor.inst.OpenDialog(beatmapObject);
                });

                EditorHelper.SetComplexity(assignPrefabLabel, Complexity.Normal);
                EditorHelper.SetComplexity(assignPrefab, Complexity.Normal);
                EditorHelper.SetComplexity(removePrefab, Complexity.Normal);
            }

            // Markers
            {
                var markers = Creator.NewUIObject("Markers", ObjEditor.inst.objTimelineSlider.transform);
                UIManager.SetRectTransform(markers.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);
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
                {
                    var dot = collapse.transform.Find("dots").GetChild(i).GetComponent<Image>();
                    EditorThemeManager.AddGraphic(collapse.transform.Find("dots").GetChild(i).GetComponent<Image>(), ThemeGroup.Dark_Text);
                }

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
                    for (int i = 0; i < __instance.KeyframeDialogs.Count - 1; i++)
                    {
                        var kfdialog = __instance.KeyframeDialogs[i].transform;

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

                        switch (i)
                        {
                            case 0:
                                {
                                    EditorThemeManager.AddInputFields(kfdialog.Find("position").gameObject, true, "");
                                    EditorThemeManager.AddInputFields(kfdialog.Find("r_position").gameObject, true, "");

                                    break;
                                }
                            case 1:
                                {
                                    EditorThemeManager.AddInputFields(kfdialog.Find("scale").gameObject, true, "");
                                    EditorThemeManager.AddInputFields(kfdialog.Find("r_scale").gameObject, true, "");

                                    break;
                                }
                            case 2:
                                {
                                    EditorThemeManager.AddInputFields(kfdialog.Find("rotation").gameObject, true, "");
                                    EditorThemeManager.AddInputFields(kfdialog.Find("r_rotation").gameObject, true, "");

                                    break;
                                }
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

                var gameObject = new GameObject("zoom back");
                gameObject.transform.SetParent(zoomSliderBase.parent);
                gameObject.transform.SetSiblingIndex(1);

                var rectTransform = gameObject.AddComponent<RectTransform>();
                var image = gameObject.AddComponent<Image>();
                UIManager.SetRectTransform(rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(128f, 25f));
                EditorThemeManager.AddGraphic(image, ThemeGroup.Timeline_Scrollbar_Base);
                EditorThemeManager.AddGraphic(zoomSliderBase.GetComponent<Image>(), ThemeGroup.Background_1, true);
                EditorThemeManager.AddGraphic(zoomSliderBase.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Slider_2);
                EditorThemeManager.AddGraphic(zoomSliderBase.transform.GetChild(2).GetComponent<Image>(), ThemeGroup.Slider_2);
                EditorThemeManager.AddGraphic(ObjEditor.inst.zoomSlider.transform.Find("Background").GetComponent<Image>(), ThemeGroup.Slider_2, true);
                EditorThemeManager.AddGraphic(ObjEditor.inst.zoomSlider.transform.Find("Fill Area/Fill").GetComponent<Image>(), ThemeGroup.Slider_2, true);
                EditorThemeManager.AddGraphic(ObjEditor.inst.zoomSlider.image, ThemeGroup.Slider_2_Handle, true);
            }

            __instance.SelectedColor = EditorConfig.Instance.ObjectSelectionColor.Value;
            __instance.ObjectLengthOffset = EditorConfig.Instance.KeyframeEndLengthOffset.Value;

            Instance.StartCoroutine(Wait(multiKF, labelToCopy, singleInput));

            return false;
        }

        public static IEnumerator Wait(GameObject multiKF, GameObject labelToCopy, GameObject singleInput)
        {
            var move = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move").transform;
            var multiKeyframeEditor = multiKF.transform;

            multiKeyframeEditor.GetChild(1).gameObject.SetActive(false);

            // Label
            {
                var label = labelToCopy.Duplicate(multiKeyframeEditor, "time_label");

                Destroy(label.transform.GetChild(1).gameObject);
                var labelText = label.transform.GetChild(0).GetComponent<Text>();
                labelText.text = "Time";

                EditorThemeManager.AddLightText(labelText);
            }

            var timeBase = new GameObject("time");
            timeBase.transform.SetParent(multiKeyframeEditor);
            timeBase.transform.localScale = Vector3.one;
            var timeBaseRT = timeBase.AddComponent<RectTransform>();
            timeBaseRT.sizeDelta = new Vector2(765f, 38f);

            while (!EditorPrefabHolder.Instance.NumberInputField)
            {
                yield return null;
            }

            var time = EditorPrefabHolder.Instance.NumberInputField.Duplicate(timeBaseRT, "time");
            time.transform.AsRT().anchoredPosition = Vector2.zero;
            time.transform.AsRT().anchorMax = new Vector2(0f, 0.5f);
            time.GetComponent<HorizontalLayoutGroup>().spacing = 5f;

            var timeStorage = time.GetComponent<InputFieldStorage>();
            timeStorage.inputField.gameObject.name = "time";

            EditorThemeManager.AddInputField(timeStorage.inputField);
            EditorThemeManager.AddSelectable(timeStorage.leftGreaterButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(timeStorage.leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(timeStorage.middleButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(timeStorage.rightButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(timeStorage.rightGreaterButton, ThemeGroup.Function_2, false);

            // Label
            {
                var label = labelToCopy.Duplicate(multiKeyframeEditor, "curve_label");

                Destroy(label.transform.GetChild(1).gameObject);
                var labelText = label.transform.GetChild(0).GetComponent<Text>();
                labelText.text = "Ease Type";

                EditorThemeManager.AddLightText(labelText);
            }

            var curveBase = new GameObject("curves");
            curveBase.transform.SetParent(multiKeyframeEditor);
            curveBase.transform.localScale = Vector3.one;
            var curveBaseRT = curveBase.AddComponent<RectTransform>();
            curveBaseRT.sizeDelta = new Vector2(765f, 38f);

            var curves = move.Find("curves").gameObject.Duplicate(curveBaseRT, "curves");
            curves.transform.AsRT().anchoredPosition = new Vector2(182f, -19f);
            EditorThemeManager.AddDropdown(curves.GetComponent<Dropdown>());

            // Label
            {
                var label = labelToCopy.Duplicate(multiKeyframeEditor, "value index_label");

                Destroy(label.transform.GetChild(1).gameObject);
                var labelText = label.transform.GetChild(0).GetComponent<Text>();
                labelText.text = "Value Index / Value";

                EditorThemeManager.AddLightText(labelText);
            }

            var valueBase = new GameObject("value base");
            valueBase.transform.SetParent(multiKeyframeEditor);
            valueBase.transform.localScale = Vector3.one;

            var valueBaseRT = valueBase.AddComponent<RectTransform>();
            valueBaseRT.sizeDelta = new Vector2(364f, 32f);

            var valueBaseHLG = valueBase.AddComponent<HorizontalLayoutGroup>();
            valueBaseHLG.childControlHeight = false;
            valueBaseHLG.childControlWidth = false;
            valueBaseHLG.childForceExpandHeight = false;
            valueBaseHLG.childForceExpandWidth = false;

            var valueIndex = singleInput.Duplicate(valueBaseRT, "value index");
            valueIndex.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);
            EditorThemeManager.AddInputField(valueIndex.GetComponent<InputField>());
            EditorThemeManager.AddSelectable(valueIndex.transform.Find("<").GetComponent<Button>(), ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(valueIndex.transform.Find(">").GetComponent<Button>(), ThemeGroup.Function_2, false);

            Destroy(valueIndex.GetComponent<EventInfo>());

            var value = EditorPrefabHolder.Instance.NumberInputField.Duplicate(valueBaseRT, "value");
            value.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);
            var valueStorage = value.GetComponent<InputFieldStorage>();
            Destroy(valueStorage.leftGreaterButton.gameObject);
            Destroy(valueStorage.rightGreaterButton.gameObject);
            EditorThemeManager.AddInputField(valueStorage.inputField);
            EditorThemeManager.AddSelectable(valueStorage.leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(valueStorage.middleButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(valueStorage.rightButton, ThemeGroup.Function_2, false);

            // Label
            {
                var label = labelToCopy.Duplicate(multiKeyframeEditor, "snap_label");

                Destroy(label.transform.GetChild(1).gameObject);
                var labelText = label.transform.GetChild(0).GetComponent<Text>();
                labelText.text = "Force Snap Time to BPM";

                EditorThemeManager.AddLightText(labelText);
            }

            var eventButton = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TimelineBar/GameObject/event");

            var snapToBPMObject = eventButton.Duplicate(multiKeyframeEditor, "snap bpm");
            snapToBPMObject.transform.localScale = Vector3.one;

            ((RectTransform)snapToBPMObject.transform).sizeDelta = new Vector2(368f, 32f);

            var snapToBPMText = snapToBPMObject.transform.GetChild(0).GetComponent<Text>();
            snapToBPMText.text = "Snap";

            var snapToBPM = snapToBPMObject.GetComponent<Button>();
            snapToBPM.onClick.ClearAll();
            snapToBPM.onClick.AddListener(() =>
            {
                var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
                foreach (var timelineObject in ObjectEditor.inst.CurrentSelection.InternalTimelineObjects.Where(x => x.Selected))
                {
                    if (timelineObject.Index != 0)
                        timelineObject.Time = RTEditor.SnapToBPM(timelineObject.Time);

                    float st = beatmapObject.StartTime;

                    st = -(st - RTEditor.SnapToBPM(st + timelineObject.Time));

                    float timePosition = ObjectEditor.TimeTimelineCalc(st);

                    ((RectTransform)timelineObject.GameObject.transform).anchoredPosition = new Vector2(timePosition, 0f);

                    Updater.UpdateObject(beatmapObject, "Keyframes");

                    ObjectEditor.inst.RenderKeyframe(beatmapObject, timelineObject);
                }
            });

            EditorThemeManager.AddGraphic(snapToBPM.image, ThemeGroup.Function_1, true);
            EditorThemeManager.AddGraphic(snapToBPMText, ThemeGroup.Function_1_Text);

            // Label
            {
                var label = labelToCopy.Duplicate(multiKeyframeEditor, "paste_label");

                Destroy(label.transform.GetChild(1).gameObject);
                var labelText = label.transform.GetChild(0).GetComponent<Text>();
                labelText.text = "All Types";

                EditorThemeManager.AddLightText(labelText);
            }

            var pasteAllObject = eventButton.Duplicate(multiKeyframeEditor, "paste");
            pasteAllObject.transform.localScale = Vector3.one;

            ((RectTransform)pasteAllObject.transform).sizeDelta = new Vector2(368f, 32f);

            var pasteAllText = pasteAllObject.transform.GetChild(0).GetComponent<Text>();
            pasteAllText.text = "Paste";

            var pasteAll = pasteAllObject.GetComponent<Button>();
            pasteAll.onClick.ClearAll();
            pasteAll.onClick.AddListener(() =>
            {
                var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
                var list = ObjectEditor.inst.CurrentSelection.InternalTimelineObjects.Where(x => x.Selected);

                foreach (var timelineObject in list)
                {
                    var kf = timelineObject.GetData<EventKeyframe>();
                    switch (timelineObject.Type)
                    {
                        case 0:
                            if (ObjectEditor.inst.CopiedPositionData != null)
                            {
                                kf.curveType = ObjectEditor.inst.CopiedPositionData.curveType;
                                kf.eventValues = ObjectEditor.inst.CopiedPositionData.eventValues.Copy();
                                kf.eventRandomValues = ObjectEditor.inst.CopiedPositionData.eventRandomValues.Copy();
                                kf.random = ObjectEditor.inst.CopiedPositionData.random;
                                kf.relative = ObjectEditor.inst.CopiedPositionData.relative;
                            }
                            break;
                        case 1:
                            if (ObjectEditor.inst.CopiedScaleData != null)
                            {
                                kf.curveType = ObjectEditor.inst.CopiedScaleData.curveType;
                                kf.eventValues = ObjectEditor.inst.CopiedScaleData.eventValues.Copy();
                                kf.eventRandomValues = ObjectEditor.inst.CopiedScaleData.eventRandomValues.Copy();
                                kf.random = ObjectEditor.inst.CopiedScaleData.random;
                                kf.relative = ObjectEditor.inst.CopiedScaleData.relative;
                            }
                            break;
                        case 2:
                            if (ObjectEditor.inst.CopiedRotationData != null)
                            {
                                kf.curveType = ObjectEditor.inst.CopiedRotationData.curveType;
                                kf.eventValues = ObjectEditor.inst.CopiedRotationData.eventValues.Copy();
                                kf.eventRandomValues = ObjectEditor.inst.CopiedRotationData.eventRandomValues.Copy();
                                kf.random = ObjectEditor.inst.CopiedRotationData.random;
                                kf.relative = ObjectEditor.inst.CopiedRotationData.relative;
                            }
                            break;
                        case 3:
                            if (ObjectEditor.inst.CopiedColorData != null)
                            {
                                kf.curveType = ObjectEditor.inst.CopiedColorData.curveType;
                                kf.eventValues = ObjectEditor.inst.CopiedColorData.eventValues.Copy();
                                kf.eventRandomValues = ObjectEditor.inst.CopiedColorData.eventRandomValues.Copy();
                                kf.random = ObjectEditor.inst.CopiedColorData.random;
                                kf.relative = ObjectEditor.inst.CopiedColorData.relative;
                            }
                            break;
                    }
                }

                ObjectEditor.inst.RenderKeyframes(beatmapObject);
                ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
                Updater.UpdateObject(beatmapObject, "Keyframes");
                EditorManager.inst.DisplayNotification("Pasted keyframe data to selected keyframes!", 2f, EditorManager.NotificationType.Success);
            });

            EditorThemeManager.AddGraphic(pasteAll.image, ThemeGroup.Paste, true);
            EditorThemeManager.AddGraphic(pasteAllText, ThemeGroup.Paste_Text);

            // Label
            {
                var label = labelToCopy.Duplicate(multiKeyframeEditor, "paste_label");

                Destroy(label.transform.GetChild(1).gameObject);
                var labelText = label.transform.GetChild(0).GetComponent<Text>();
                labelText.text = "Position / Scale";

                EditorThemeManager.AddLightText(labelText);
            }

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

            var pastePosObject = eventButton.Duplicate(pastePosScaRT, "paste");
            pastePosObject.transform.localScale = Vector3.one;

            ((RectTransform)pastePosObject.transform).sizeDelta = new Vector2(180f, 32f);

            var pastePosText = pastePosObject.transform.GetChild(0).GetComponent<Text>();
            pastePosText.text = "Paste Pos";

            var pastePos = pastePosObject.GetComponent<Button>();
            pastePos.onClick.ClearAll();
            pastePos.onClick.AddListener(() =>
            {
                var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
                var list = ObjectEditor.inst.CurrentSelection.InternalTimelineObjects.Where(x => x.Selected);

                foreach (var timelineObject in list)
                {
                    if (timelineObject.Type != 0)
                        continue;

                    var kf = timelineObject.GetData<EventKeyframe>();

                    if (ObjectEditor.inst.CopiedPositionData != null)
                    {
                        kf.curveType = ObjectEditor.inst.CopiedPositionData.curveType;
                        kf.eventValues = ObjectEditor.inst.CopiedPositionData.eventValues.Copy();
                        kf.eventRandomValues = ObjectEditor.inst.CopiedPositionData.eventRandomValues.Copy();
                        kf.random = ObjectEditor.inst.CopiedPositionData.random;
                        kf.relative = ObjectEditor.inst.CopiedPositionData.relative;
                    }
                }

                ObjectEditor.inst.RenderKeyframes(beatmapObject);
                ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
                Updater.UpdateObject(beatmapObject, "Keyframes");
                EditorManager.inst.DisplayNotification("Pasted position keyframe data to selected position keyframes!", 3f, EditorManager.NotificationType.Success);
            });

            EditorThemeManager.AddGraphic(pastePos.image, ThemeGroup.Paste, true);
            EditorThemeManager.AddGraphic(pastePosText, ThemeGroup.Paste_Text);

            var pasteScaObject = eventButton.Duplicate(pastePosScaRT, "paste");
            pasteScaObject.transform.localScale = Vector3.one;

            ((RectTransform)pasteScaObject.transform).sizeDelta = new Vector2(180f, 32f);

            var pasteScaText = pasteScaObject.transform.GetChild(0).GetComponent<Text>();
            pasteScaText.text = "Paste Scale";

            var pasteSca = pasteScaObject.GetComponent<Button>();
            pasteSca.onClick.ClearAll();
            pasteSca.onClick.AddListener(() =>
            {
                var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
                var list = ObjectEditor.inst.CurrentSelection.InternalTimelineObjects.Where(x => x.Selected);

                foreach (var timelineObject in list)
                {
                    if (timelineObject.Type != 1)
                        continue;

                    var kf = timelineObject.GetData<EventKeyframe>();

                    if (ObjectEditor.inst.CopiedScaleData != null)
                    {
                        kf.curveType = ObjectEditor.inst.CopiedScaleData.curveType;
                        kf.eventValues = ObjectEditor.inst.CopiedScaleData.eventValues.Copy();
                        kf.eventRandomValues = ObjectEditor.inst.CopiedScaleData.eventRandomValues.Copy();
                        kf.random = ObjectEditor.inst.CopiedScaleData.random;
                        kf.relative = ObjectEditor.inst.CopiedScaleData.relative;
                    }
                }

                ObjectEditor.inst.RenderKeyframes(beatmapObject);
                ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
                Updater.UpdateObject(beatmapObject, "Keyframes");
                EditorManager.inst.DisplayNotification("Pasted scale keyframe data to selected scale keyframes!", 3f, EditorManager.NotificationType.Success);
            });

            EditorThemeManager.AddGraphic(pasteSca.image, ThemeGroup.Paste, true);
            EditorThemeManager.AddGraphic(pasteScaText, ThemeGroup.Paste_Text);

            // Label
            {
                var label = labelToCopy.Duplicate(multiKeyframeEditor, "paste_label");

                Destroy(label.transform.GetChild(1).gameObject);
                var labelText = label.transform.GetChild(0).GetComponent<Text>();
                labelText.text = "Rotation / Color";

                EditorThemeManager.AddLightText(labelText);
            }

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

            var pasteRotObject = eventButton.Duplicate(pasteRotColObjectRT, "paste");
            pasteRotObject.transform.localScale = Vector3.one;

            ((RectTransform)pasteRotObject.transform).sizeDelta = new Vector2(180f, 32f);

            var pasteRotText = pasteRotObject.transform.GetChild(0).GetComponent<Text>();
            pasteRotText.text = "Paste Rot";

            var pasteRot = pasteRotObject.GetComponent<Button>();
            pasteRot.onClick.ClearAll();
            pasteRot.onClick.AddListener(() =>
            {
                var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
                var list = ObjectEditor.inst.CurrentSelection.InternalTimelineObjects.Where(x => x.Selected);

                foreach (var timelineObject in list)
                {
                    if (timelineObject.Type != 3)
                        continue;

                    var kf = timelineObject.GetData<EventKeyframe>();

                    if (ObjectEditor.inst.CopiedRotationData != null)
                    {
                        kf.curveType = ObjectEditor.inst.CopiedRotationData.curveType;
                        kf.eventValues = ObjectEditor.inst.CopiedRotationData.eventValues.Copy();
                        kf.eventRandomValues = ObjectEditor.inst.CopiedRotationData.eventRandomValues.Copy();
                        kf.random = ObjectEditor.inst.CopiedRotationData.random;
                        kf.relative = ObjectEditor.inst.CopiedRotationData.relative;
                    }
                }

                ObjectEditor.inst.RenderKeyframes(beatmapObject);
                ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
                Updater.UpdateObject(beatmapObject, "Keyframes");
                EditorManager.inst.DisplayNotification("Pasted rotation keyframe data to selected rotation keyframes!", 3f, EditorManager.NotificationType.Success);
            });

            EditorThemeManager.AddGraphic(pasteRot.image, ThemeGroup.Paste, true);
            EditorThemeManager.AddGraphic(pasteRotText, ThemeGroup.Paste_Text);

            var pasteColObject = eventButton.Duplicate(pasteRotColObjectRT, "paste");
            pasteColObject.transform.localScale = Vector3.one;

            ((RectTransform)pasteColObject.transform).sizeDelta = new Vector2(180f, 32f);

            var pasteColText = pasteColObject.transform.GetChild(0).GetComponent<Text>();
            pasteColText.text = "Paste Col";

            var pasteCol = pasteColObject.GetComponent<Button>();
            pasteCol.onClick.ClearAll();
            pasteCol.onClick.AddListener(() =>
            {
                var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
                var list = ObjectEditor.inst.CurrentSelection.InternalTimelineObjects.Where(x => x.Selected);

                foreach (var timelineObject in list)
                {
                    if (timelineObject.Type != 4)
                        continue;

                    var kf = timelineObject.GetData<EventKeyframe>();

                    if (ObjectEditor.inst.CopiedColorData != null)
                    {
                        kf.curveType = ObjectEditor.inst.CopiedColorData.curveType;
                        kf.eventValues = ObjectEditor.inst.CopiedColorData.eventValues.Copy();
                        kf.eventRandomValues = ObjectEditor.inst.CopiedColorData.eventRandomValues.Copy();
                        kf.random = ObjectEditor.inst.CopiedColorData.random;
                        kf.relative = ObjectEditor.inst.CopiedColorData.relative;
                    }
                }

                ObjectEditor.inst.RenderKeyframes(beatmapObject);
                ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
                Updater.UpdateObject(beatmapObject, "Keyframes");
                EditorManager.inst.DisplayNotification("Pasted color keyframe data to selected color keyframes!", 3f, EditorManager.NotificationType.Success);
            });

            EditorThemeManager.AddGraphic(pasteCol.image, ThemeGroup.Paste, true);
            EditorThemeManager.AddGraphic(pasteColText, ThemeGroup.Paste_Text);

            AnimationEditor.Init();

            yield break;
        }

        [HarmonyPatch(nameof(ObjEditor.Start))]
        [HarmonyPrefix]
        static bool StartPrefix()
        {
            Instance.colorButtons.Clear();
            for (int i = 1; i <= 18; i++)
            {
                Instance.colorButtons.Add(Instance.KeyframeDialogs[3].transform.Find("color/" + i).GetComponent<Toggle>());
            }

            if (RTFile.FileExists(Application.persistentDataPath + "/copied_objects.lsp"))
            {
                var jn = JSON.Parse(FileManager.inst.LoadJSONFileRaw(Application.persistentDataPath + "/copied_objects.lsp"));

                var objects = new List<BaseBeatmapObject>();
                for (int i = 0; i < jn["objects"].Count; ++i)
                    objects.Add(BeatmapObject.Parse(jn["objects"][i]));

                var prefabObjects = new List<BasePrefabObject>();
                for (int i = 0; i < jn["prefab_objects"].Count; ++i)
                    prefabObjects.Add(PrefabObject.Parse(jn["prefab_objects"][i]));

                Instance.beatmapObjCopy = new BasePrefab(jn["name"], jn["type"].AsInt, jn["offset"].AsFloat, objects, prefabObjects);
                Instance.hasCopiedObject = true;
            }

            Instance.zoomBounds = EditorConfig.Instance.KeyframeZoomBounds.Value;

            ObjectEditor.Init(Instance);

            ObjectEditor.inst.shapeButtonPrefab = Instance.ObjectView.transform.Find("shape/1").gameObject.Duplicate(Instance.transform);

            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.SetMainTimelineZoom))]
        [HarmonyPrefix]
        static bool SetMainTimelineZoomPrefix(float __0, bool __1 = true)
        {
            var beatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();
            if (__1)
            {
                ObjectEditor.inst.ResizeKeyframeTimeline(beatmapObject);
                ObjectEditor.inst.RenderKeyframes(beatmapObject);
            }
            float f = ObjEditor.inst.objTimelineSlider.value;
            if (AudioManager.inst.CurrentAudioSource.clip != null)
            {
                float time = -beatmapObject.StartTime + AudioManager.inst.CurrentAudioSource.time;
                float objectLifeLength = beatmapObject.GetObjectLifeLength(ObjEditor.inst.ObjectLengthOffset);

                f = time / objectLifeLength;
            }

            Instance.StartCoroutine(UpdateTimelineScrollRect(0f, f));

            return false;
        }

        public static IEnumerator UpdateTimelineScrollRect(float _delay, float _val)
        {
            yield return new WaitForSeconds(_delay);
            if (ObjectEditor.inst.timelinePosScrollbar)
                ObjectEditor.inst.timelinePosScrollbar.value = _val;

            yield break;
        }

        [HarmonyPatch(nameof(ObjEditor.SetCurrentObj))]
        [HarmonyPrefix]
        static bool SetCurrentObjPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.UpdateHighlightedKeyframe))]
        [HarmonyPrefix]
        static bool UpdateHighlightedKeyframePrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.DeRenderObject))]
        [HarmonyPrefix]
        static bool DeRenderObjectPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.RenderTimelineObject))]
        [HarmonyPrefix]
        static bool RenderTimelineObjectPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.RenderTimelineObjects))]
        [HarmonyPrefix]
        static bool RenderTimelineObjectsPrefix()
        {
            ObjectEditor.inst.RenderTimelineObjects();
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.DeleteObject))]
        [HarmonyPrefix]
        static bool DeleteObjectPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.DeleteObjects))]
        [HarmonyPrefix]
        static bool DeleteObjectsPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.AddPrefabExpandedToLevel))]
        [HarmonyPrefix]
        static bool AddPrefabExpandedToLevelPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.AddSelectedObject))]
        [HarmonyPrefix]
        static bool AddSelectedObjectPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.AddSelectedObjectOnly))]
        [HarmonyPrefix]
        static bool AddSelectedObjectOnlyPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.ContainedInSelectedObjects))]
        [HarmonyPrefix]
        static bool ContainedInSelectedObjectsPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.RefreshParentGUI))]
        [HarmonyPrefix]
        static bool RefreshParentGUIPrefix() => false;

        [HarmonyPatch(nameof(ObjEditor.CopyAllSelectedEvents))]
        [HarmonyPrefix]
        static bool CopyAllSelectedEventsPrefix()
        {
            if (ObjectEditor.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.CopyAllSelectedEvents(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.PasteKeyframes))]
        [HarmonyPrefix]
        static bool PasteKeyframesPrefix()
        {
            if (ObjectEditor.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.PasteKeyframes(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.OpenDialog))]
        [HarmonyPrefix]
        static bool OpenDialogPrefix()
        {
            ObjectEditor.inst.OpenDialog(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.SetCurrentKeyframe), new Type[] { typeof(int), typeof(bool) })]
        [HarmonyPrefix]
        static bool SetCurrentKeyframePrefix(int __0, bool __1 = false)
        {
            if (ObjectEditor.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.SetCurrentKeyframe(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>(), __0, __1);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.SetCurrentKeyframe), new Type[] { typeof(int), typeof(int), typeof(bool), typeof(bool) })]
        [HarmonyPrefix]
        static bool SetCurrentKeyframePrefix(int __0, int __1, bool __2 = false, bool __3 = false)
        {
            if (ObjectEditor.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.SetCurrentKeyframe(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>(), __0, __1);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.AddCurrentKeyframe))]
        [HarmonyPrefix]
        static bool AddCurrentKeyframePrefix(int __0, bool __1 = false)
        {
            if (ObjectEditor.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.AddCurrentKeyframe(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>(), __0, __1);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.ResizeKeyframeTimeline))]
        [HarmonyPrefix]
        static bool ResizeKeyframeTimelinePrefix()
        {
            if (ObjectEditor.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.ResizeKeyframeTimeline(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.SetAudioTime))]
        [HarmonyPrefix]
        static bool SetAudioTimePrefix(float __0)
        {
            if (Instance.changingTime)
            {
                Instance.newTime = __0;
                AudioManager.inst.SetMusicTime(Mathf.Clamp(__0, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
            }
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.GetKeyframeIcon))]
        [HarmonyPrefix]
        static bool GetKeyframeIconPrefix(ref Sprite __result, DataManager.LSAnimation __0, DataManager.LSAnimation __1)
        {
            __result = RTEditor.GetKeyframeIcon(__0, __1);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateKeyframes))]
        [HarmonyPrefix]
        static bool CreateKeyframesPrefix()
        {
            if (ObjectEditor.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.CreateKeyframes(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateKeyframeStartDragTrigger))]
        [HarmonyPrefix]
        static bool CreateKeyframeStartDragTriggerPrefix(ref EventTrigger.Entry __result, EventTriggerType __0, int __1, int __2)
        {
            __result = TriggerHelper.CreateKeyframeStartDragTrigger(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>(), RTEditor.inst.timelineKeyframes.Find(x => x.Type == __1 && x.Index == __2));
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateKeyframeEndDragTrigger))]
        [HarmonyPrefix]
        static bool CreateKeyframeEndDragTriggerPrefix(ref EventTrigger.Entry __result, EventTriggerType __0, int __1, int __2)
        {
            __result = TriggerHelper.CreateKeyframeEndDragTrigger(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>(), RTEditor.inst.timelineKeyframes.Find(x => x.Type == __1 && x.Index == __2));
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.DeRenderSelectedObjects))]
        [HarmonyPrefix]
        static bool DeRenderSelectedObjectsPrefix()
        {
            ObjectEditor.inst.DeselectAllObjects();
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CopyObject))]
        [HarmonyPrefix]
        static bool CopyObjectPrefix()
        {
            var a = new List<TimelineObject>(ObjectEditor.inst.SelectedObjects);

            a = (from x in a
                 orderby x.Time
                 select x).ToList();

            float start = 0f;
            if (EditorConfig.Instance.PasteOffset.Value)
                start = -AudioManager.inst.CurrentAudioSource.time + a[0].Time;

            var copy = new Prefab("copied prefab", 0, start,
                a.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()).ToList(),
                a.Where(x => x.isPrefabObject).Select(x => x.GetData<PrefabObject>()).ToList());

            copy.description = "Take me wherever you go!";
            Instance.beatmapObjCopy = copy;
            Instance.hasCopiedObject = true;

            if (EditorConfig.Instance.CopyPasteGlobal.Value && RTFile.DirectoryExists(Application.persistentDataPath))
                RTFile.WriteToFile(RTFile.CombinePaths(Application.persistentDataPath, $"copied_objects{FileFormat.LSP.Dot()}"), copy.ToJSON().ToString());
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.PasteObject))]
        [HarmonyPrefix]
        static bool PasteObjectPrefix(float __0)
        {
            ObjectEditor.inst.PasteObject(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.AddEvent))]
        [HarmonyPrefix]
        static bool AddEventPrefix(ref int __result, float __0, int __1, BaseEventKeyframe __2) => false;

        [HarmonyPatch(nameof(ObjEditor.ToggleLockCurrentSelection))]
        [HarmonyPrefix]
        static bool ToggleLockCurrentSelectionPrefix()
        {
            foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
            {
                if (timelineObject.isBeatmapObject)
                    timelineObject.GetData<BeatmapObject>().editorData.locked = !timelineObject.GetData<BeatmapObject>().editorData.locked;
                if (timelineObject.isPrefabObject)
                    timelineObject.GetData<PrefabObject>().editorData.locked = !timelineObject.GetData<PrefabObject>().editorData.locked;

                ObjectEditor.inst.RenderTimelineObject(timelineObject);
            }

            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.UpdateKeyframeOrder))]
        [HarmonyPrefix]
        static bool UpdateKeyframeOrderPrefix(bool _setCurrent = true)
        {
            if (ObjectEditor.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.UpdateKeyframeOrder(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.SnapToBPM))]
        [HarmonyPrefix]
        static bool SnapToBPMPrefix(ref float __result, float __0)
        {
            __result = RTEditor.SnapToBPM(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.posCalc))]
        [HarmonyPrefix]
        static bool posCalcPrefix(ref float __result, float __0)
        {
            __result = ObjectEditor.TimeTimelineCalc(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.timeCalc))]
        [HarmonyPrefix]
        static bool timeCalcPrefix(ref float __result)
        {
            __result = ObjectEditor.MouseTimelineCalc();
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.RefreshKeyframeGUI))]
        [HarmonyPrefix]
        static bool RefreshKeyframeGUIPrefix()
        {
            if (ObjectEditor.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.StartCoroutine(ObjectEditor.RefreshObjectGUI(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>()));
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateNewNormalObject))]
        [HarmonyPrefix]
        static bool CreateNewNormalObjectPrefix(bool __0)
        {
            ObjectEditor.inst.CreateNewNormalObject(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateNewCircleObject))]
        [HarmonyPrefix]
        static bool CreateNewCircleObjectPrefix(bool __0)
        {
            ObjectEditor.inst.CreateNewCircleObject(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateNewTriangleObject))]
        [HarmonyPrefix]
        static bool CreateNewTriangleObjectPrefix(bool __0)
        {
            ObjectEditor.inst.CreateNewTriangleObject(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateNewTextObject))]
        [HarmonyPrefix]
        static bool CreateNewTextObjectPrefix(bool __0)
        {
            ObjectEditor.inst.CreateNewTextObject(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateNewHexagonObject))]
        [HarmonyPrefix]
        static bool CreateNewHexagonObjectPrefix(bool __0)
        {
            ObjectEditor.inst.CreateNewHexagonObject(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateNewHelperObject))]
        [HarmonyPrefix]
        static bool CreateNewHelperObjectPrefix(bool __0)
        {
            ObjectEditor.inst.CreateNewHelperObject(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateNewDecorationObject))]
        [HarmonyPrefix]
        static bool CreateNewDecorationObjectPrefix(bool __0)
        {
            ObjectEditor.inst.CreateNewDecorationObject(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateNewEmptyObject))]
        [HarmonyPrefix]
        static bool CreateNewEmptyObjectPrefix(bool __0)
        {
            ObjectEditor.inst.CreateNewEmptyObject(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.CreateNewPersistentObject))]
        [HarmonyPrefix]
        static bool CreateNewPersistentObjectPrefix(bool __0)
        {
            ObjectEditor.inst.CreateNewNoAutokillObject(__0);
            return false;
        }

        [HarmonyPatch(nameof(ObjEditor.Zoom), MethodType.Setter)]
        [HarmonyPrefix]
        static bool ZoomSetterPrefix(ref float value)
        {
            ObjectEditor.inst.SetTimeline(value);
            return false;
        }
    }
}
