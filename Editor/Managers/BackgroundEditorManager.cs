using BetterLegacy.Components;
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

namespace BetterLegacy.Editor.Managers
{
    public class BackgroundEditorManager : MonoBehaviour
    {
        public static BackgroundEditorManager inst;

        public static BackgroundObject CurrentSelectedBG => BackgroundEditor.inst == null ? null : (BackgroundObject)DataManager.inst.gameData.backgroundObjects[BackgroundEditor.inst.currentObj];

        public static void Init(BackgroundEditor backgroundEditor) => backgroundEditor.gameObject.AddComponent<BackgroundEditorManager>();

        public GameObject shapeButtonCopy;

        void Awake()
        {
            inst = this;
        }

        public void OpenDialog(int index)
        {
            var __instance = BackgroundEditor.inst;

            EditorManager.inst.ClearDialogs();
            EditorManager.inst.ShowDialog("Background Editor");

            var backgroundObject = (BackgroundObject)DataManager.inst.gameData.backgroundObjects[index];

            __instance.left.Find("name/active").GetComponent<Toggle>().isOn = backgroundObject.active;
            __instance.left.Find("name/name").GetComponent<InputField>().text = backgroundObject.name;

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

            button.onClick.AddListener(delegate ()
            {
                onSetColor.Invoke(colTmp);
            });

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

            //Debug.Log($"{ObjEditor.inst.className}Removing all...");
            foreach (var obj in toDestroy)
                DestroyImmediate(obj);

            toDestroy = null;

            // Re-add everything
            for (int i = 0; i < ShapeManager.inst.Shapes3D.Count; i++)
            {
                var obj = ObjectEditor.inst.shapeButtonPrefab.Duplicate(shape, (i + 1).ToString(), i);
                if (obj.transform.Find("Image") && obj.transform.Find("Image").gameObject.TryGetComponent(out Image image))
                {
                    image.sprite = ShapeManager.inst.Shapes3D[i][0].Icon;
                    EditorThemeManager.ApplyGraphic(image, ThemeGroup.Toggle_1_Check);
                }

                var shapeToggle = obj.GetComponent<Toggle>();
                EditorThemeManager.ApplyToggle(shapeToggle, ThemeGroup.Background_1);

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
                            image1.sprite = ShapeManager.inst.Shapes3D[i][j].Icon;
                            EditorThemeManager.ApplyGraphic(image1, ThemeGroup.Toggle_1_Check);
                        }

                        var layoutElement = opt.AddComponent<LayoutElement>();
                        layoutElement.layoutPriority = 1;
                        layoutElement.minWidth = 32f;

                        ((RectTransform)opt.transform).sizeDelta = new Vector2(32f, 32f);

                        var shapeOptionToggle = opt.GetComponent<Toggle>();
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
                    shoggle.onValueChanged.AddListener(delegate (bool _value)
                    {
                        if (_value)
                        {
                            backgroundObject.SetShape(buttonTmp, 0);

                            BackgroundEditor.inst.OpenDialog(index);
                        }
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
                shoggle.onValueChanged.AddListener(delegate (bool _value)
                {
                    if (!_value)
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
                    backgroundObject.reactiveType = (DataManager.GameData.BackgroundObject.ReactiveType)UnityEngine.Random.Range(0, 4);

                    backgroundObject.reactiveScale = UnityEngine.Random.Range(0.01f, 0.04f);
                }

                backgroundObject.reactivePosIntensity = new Vector2(UnityEngine.Random.Range(0, 100) > 65 ? UnityEngine.Random.Range(0f, 1f) : 0f, UnityEngine.Random.Range(0, 100) > 65 ? UnityEngine.Random.Range(0f, 1f) : 0f);
                backgroundObject.reactiveScaIntensity = new Vector2(UnityEngine.Random.Range(0, 100) > 45 ? UnityEngine.Random.Range(0f, 1f) : 0f, UnityEngine.Random.Range(0, 100) > 45 ? UnityEngine.Random.Range(0f, 1f) : 0f);
                backgroundObject.reactiveRotIntensity = UnityEngine.Random.Range(0, 100) > 45 ? UnityEngine.Random.Range(0f, 1f) : 0f;
                backgroundObject.reactiveCol = UnityEngine.Random.Range(1, 6);

                var randomShape = UnityEngine.Random.Range(0, ShapeManager.inst.Shapes3D.Count);
                var randomShapeOption = UnityEngine.Random.Range(0, ShapeManager.inst.Shapes3D[randomShape].Count);

                backgroundObject.shape = ShapeManager.inst.Shapes3D[randomShape][randomShapeOption];

                DataManager.inst.gameData.backgroundObjects.Add(backgroundObject);
            }

            BackgroundManager.inst.UpdateBackgrounds();
            BackgroundEditor.inst.UpdateBackgroundList();
        }

        public void DeleteAllBackgrounds()
        {
            int num = DataManager.inst.gameData.backgroundObjects.Count;

            for (int i = 1; i < num; i++)
            {
                int nooo = Mathf.Clamp(i, 1, DataManager.inst.gameData.backgroundObjects.Count - 1);
                DataManager.inst.gameData.backgroundObjects.RemoveAt(nooo);
            }

            BackgroundEditor.inst.SetCurrentBackground(0);
            BackgroundManager.inst.UpdateBackgrounds();
            BackgroundEditor.inst.UpdateBackgroundList();

            EditorManager.inst.DisplayNotification("Deleted " + (num - 1).ToString() + " backgrounds!", 2f, EditorManager.NotificationType.Success);
        }

        #region Modifiers

        public static bool installed = false;

        public Transform content;
        public Transform scrollView;
        public RectTransform scrollViewRT;

        public bool showModifiers;

        public GameObject modifierCardPrefab;
        public GameObject modifierAddPrefab;

        public void CreateModifiersOnAwake()
        {
            var bmb = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View");

            var act = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/grain/colored"));
            act.transform.SetParent(BackgroundEditor.inst.left);
            act.transform.localScale = Vector3.one;
            act.name = "active";
            var activeText = act.transform.Find("Text").GetComponent<Text>();
            activeText.text = "Show Modifiers";

            var toggle = act.GetComponent<Toggle>();
            toggle.onValueChanged.RemoveAllListeners();
            toggle.isOn = showModifiers;
            toggle.onValueChanged.AddListener(delegate (bool _val)
            {
                showModifiers = _val;
                scrollView.gameObject.SetActive(showModifiers);
                if (CurrentSelectedBG)
                    StartCoroutine(RenderModifiers(CurrentSelectedBG));
            });

            EditorThemeManager.AddToggle(toggle, graphic: activeText);

            var e = Instantiate(bmb);

            scrollView = e.transform;

            scrollView.SetParent(BackgroundEditor.inst.left);
            scrollView.localScale = Vector3.one;
            scrollView.name = "Modifiers Scroll View";

            scrollViewRT = scrollView.GetComponent<RectTransform>();

            content = scrollView.Find("Viewport/Content");
            LSHelpers.DeleteChildren(content);

            scrollView.gameObject.SetActive(showModifiers);

            modifierCardPrefab = new GameObject("Modifier Prefab");
            var mcpRT = modifierCardPrefab.AddComponent<RectTransform>();
            mcpRT.sizeDelta = new Vector2(336f, 128f);

            var mcpImage = modifierCardPrefab.AddComponent<Image>();
            mcpImage.color = new Color(1f, 1f, 1f, 0.03f);

            var mcpVLG = modifierCardPrefab.AddComponent<VerticalLayoutGroup>();
            mcpVLG.childControlHeight = false;
            mcpVLG.childForceExpandHeight = false;

            var mcpCSF = modifierCardPrefab.AddComponent<ContentSizeFitter>();
            mcpCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var mcpSpacerTop = new GameObject("Spacer Top");
            mcpSpacerTop.transform.SetParent(mcpRT);
            mcpSpacerTop.transform.localScale = Vector3.one;
            var mcpSpacerTopRT = mcpSpacerTop.AddComponent<RectTransform>();
            mcpSpacerTopRT.sizeDelta = new Vector2(350f, 8f);

            var mcpLabel = new GameObject("Label");
            mcpLabel.transform.SetParent(mcpRT);
            mcpLabel.transform.localScale = Vector3.one;

            var mcpLabelRT = mcpLabel.AddComponent<RectTransform>();
            mcpLabelRT.anchorMax = new Vector2(0f, 1f);
            mcpLabelRT.anchorMin = new Vector2(0f, 1f);
            mcpLabelRT.pivot = new Vector2(0f, 1f);
            mcpLabelRT.sizeDelta = new Vector2(187f, 32f);

            var mcpLabelHLG = mcpLabel.AddComponent<HorizontalLayoutGroup>();
            mcpLabelHLG.childControlWidth = false;
            mcpLabelHLG.childForceExpandWidth = false;

            var mcpText = new GameObject("Text");
            mcpText.transform.SetParent(mcpLabelRT);
            mcpText.transform.localScale = Vector3.one;
            var mcpTextRT = mcpText.AddComponent<RectTransform>();
            mcpTextRT.anchoredPosition = new Vector2(10f, -5f);
            mcpTextRT.anchorMax = Vector2.one;
            mcpTextRT.anchorMin = Vector2.zero;
            mcpTextRT.pivot = new Vector2(0f, 1f);
            mcpTextRT.sizeDelta = new Vector2(300f, 32f);

            var mcpTextText = mcpText.AddComponent<Text>();
            mcpTextText.alignment = TextAnchor.MiddleLeft;
            mcpTextText.font = FontManager.inst.DefaultFont;
            mcpTextText.fontSize = 19;
            mcpTextText.color = new Color(0.9373f, 0.9216f, 0.9373f);

            var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(mcpLabelRT, "Delete");
            delete.transform.localScale = Vector3.one;
            var deleteLayoutElement = delete.GetComponent<LayoutElement>() ?? delete.GetComponent<LayoutElement>();
            deleteLayoutElement.minWidth = 32f;

            UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(150f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 32f));

            var mcpSpacerMid = new GameObject("Spacer Middle");
            mcpSpacerMid.transform.SetParent(mcpRT);
            mcpSpacerMid.transform.localScale = Vector3.one;
            var mcpSpacerMidRT = mcpSpacerMid.AddComponent<RectTransform>();
            mcpSpacerMidRT.sizeDelta = new Vector2(350f, 8f);

            var layout = new GameObject("Layout");
            layout.transform.SetParent(mcpRT);
            layout.transform.localScale = Vector3.one;

            var layoutRT = layout.AddComponent<RectTransform>();

            var layoutVLG = layout.AddComponent<VerticalLayoutGroup>();
            layoutVLG.childControlHeight = false;
            layoutVLG.childForceExpandHeight = false;
            layoutVLG.spacing = 4f;

            var layoutCSF = layout.AddComponent<ContentSizeFitter>();
            layoutCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var mcpSpacerBot = new GameObject("Spacer Botom");
            mcpSpacerBot.transform.SetParent(mcpRT);
            mcpSpacerBot.transform.localScale = Vector3.one;
            var mcpSpacerBotRT = mcpSpacerBot.AddComponent<RectTransform>();
            mcpSpacerBotRT.sizeDelta = new Vector2(350f, 8f);

            modifierAddPrefab = EditorManager.inst.folderButtonPrefab.Duplicate(null, "add modifier");

            var text = modifierAddPrefab.transform.GetChild(0).GetComponent<Text>();
            text.text = "+";
            text.alignment = TextAnchor.MiddleCenter;

            booleanBar = Boolean();

            numberInput = NumberInput();

            stringInput = StringInput();

            dropdownBar = Dropdown();
        }

        public IEnumerator RenderModifiers(BackgroundObject backgroundObject)
        {
            LSHelpers.DeleteChildren(content);

            var x = BackgroundEditor.inst.left.Find("block/x");
            var xif = x.GetComponent<InputField>();
            var left = x.Find("<").GetComponent<Button>();
            var right = x.Find(">").GetComponent<Button>();

            xif.onValueChanged.ClearAll();
            xif.text = currentPage.ToString();
            xif.onValueChanged.AddListener(delegate (string _val)
            {
                if (int.TryParse(_val, out int page))
                {
                    currentPage = Mathf.Clamp(page, 0, backgroundObject.modifiers.Count - 1);
                    StartCoroutine(RenderModifiers(backgroundObject));
                }
            });

            left.onClick.ClearAll();
            left.onClick.AddListener(delegate ()
            {
                if (int.TryParse(xif.text, out int page))
                {
                    xif.text = Mathf.Clamp(page - 1, 0, backgroundObject.modifiers.Count - 1).ToString();
                }
            });

            right.onClick.ClearAll();
            right.onClick.AddListener(delegate ()
            {
                if (int.TryParse(xif.text, out int page))
                {
                    xif.text = Mathf.Clamp(page + 1, 0, backgroundObject.modifiers.Count - 1).ToString();
                }
            });

            TriggerHelper.AddEventTriggerParams(xif.gameObject, TriggerHelper.ScrollDeltaInt(xif, max: backgroundObject.modifiers.Count - 1));

            var addBlockButton = x.Find("add").GetComponent<Button>();
            addBlockButton.onClick.ClearAll();
            addBlockButton.onClick.AddListener(delegate ()
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
            removeBlockButton.onClick.AddListener(delegate ()
            {
                if (backgroundObject.modifiers.Count < 1)
                    return;

                RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this modifier block?", delegate ()
                {
                    DelBlock(backgroundObject);
                    EditorManager.inst.HideDialog("Warning Popup");
                }, delegate ()
                {
                    EditorManager.inst.HideDialog("Warning Popup");
                });
            });

            if (showModifiers && backgroundObject.modifiers.Count > currentPage)
            {
                ((RectTransform)content.parent.parent).sizeDelta = new Vector2(351f, 300f * Mathf.Clamp(backgroundObject.modifiers[currentPage].Count, 1, 5));

                int num = 0;
                foreach (var modifier in backgroundObject.modifiers[currentPage])
                {
                    int index = num;
                    var gameObject = modifierCardPrefab.Duplicate(content, modifier.commands[0]);
                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.List_Button_1_Normal, gameObject, new List<Component>
                    {
                        gameObject.GetComponent<Image>(),
                    }, true, 1, SpriteManager.RoundedSide.W));
                    gameObject.transform.localScale = Vector3.one;
                    var modifierTitle = gameObject.transform.Find("Label/Text").GetComponent<Text>();
                    modifierTitle.text = modifier.commands[0];
                    EditorThemeManager.ApplyLightText(modifierTitle);

                    var delete = gameObject.transform.Find("Label/Delete").GetComponent<DeleteButtonStorage>();
                    delete.button.onClick.ClearAll();
                    delete.button.onClick.AddListener(delegate ()
                    {
                        backgroundObject.modifiers[currentPage].RemoveAt(index);
                        backgroundObject.positionOffset = Vector3.zero;
                        backgroundObject.scaleOffset = Vector3.zero;
                        backgroundObject.rotationOffset = Vector3.zero;

                        Destroy(backgroundObject.BaseObject);
                        Updater.CreateBackgroundObject(backgroundObject);

                        StartCoroutine(RenderModifiers(backgroundObject));
                    });

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete, delete.gameObject, new List<Component>
                    {
                        delete.button.image,
                    }, true, 1, SpriteManager.RoundedSide.W));

                    EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete_Text, delete.image.gameObject, new List<Component>
                    {
                        delete.image,
                    }));

                    var layout = gameObject.transform.Find("Layout");

                    var constant = booleanBar.Duplicate(layout, "Constant");
                    constant.transform.localScale = Vector3.one;

                    var constantText = constant.transform.Find("Text").GetComponent<Text>();
                    constantText.text = "Constant";
                    EditorThemeManager.ApplyLightText(constantText);

                    var toggle = constant.transform.Find("Toggle").GetComponent<Toggle>();
                    toggle.onValueChanged.ClearAll();
                    toggle.isOn = modifier.constant;
                    toggle.onValueChanged.AddListener(delegate (bool _val)
                    {
                        modifier.constant = _val;
                        modifier.active = false;
                    });
                    EditorThemeManager.ApplyToggle(toggle);

                    if (modifier.type == ModifierBase.Type.Trigger)
                    {
                        var not = booleanBar.Duplicate(layout, "Not");
                        not.transform.localScale = Vector3.one;
                        var notText = not.transform.Find("Text").GetComponent<Text>();
                        notText.text = "Not";

                        var notToggle = not.transform.Find("Toggle").GetComponent<Toggle>();
                        notToggle.onValueChanged.ClearAll();
                        notToggle.isOn = modifier.not;
                        notToggle.onValueChanged.AddListener(delegate (bool _val)
                        {
                            modifier.not = _val;
                            modifier.active = false;
                        });

                        EditorThemeManager.ApplyLightText(notText);
                        EditorThemeManager.ApplyToggle(notToggle);
                    }

                    Action<string, int, float> singleGenerator = delegate (string label, int type, float defaultValue)
                    {
                        var single = numberInput.Duplicate(layout, label);
                        single.transform.localScale = Vector3.one;
                        var labelText = single.transform.Find("Text").GetComponent<Text>();
                        labelText.text = label;

                        var inputField = single.transform.Find("Input").GetComponent<InputField>();
                        inputField.onValueChanged.ClearAll();
                        inputField.textComponent.alignment = TextAnchor.MiddleCenter;
                        inputField.text = Parser.TryParse(type == 0 ? modifier.value : modifier.commands[type], defaultValue).ToString();
                        inputField.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                if (type == 0)
                                    modifier.value = num.ToString();
                                else
                                    modifier.commands[type] = num.ToString();
                            }

                            modifier.active = false;
                        });

                        EditorThemeManager.ApplyLightText(labelText);
                        EditorThemeManager.ApplyInputField(inputField);
                        var leftButton = single.transform.Find("<").GetComponent<Button>();
                        var rightButton = single.transform.Find(">").GetComponent<Button>();
                        leftButton.transition = Selectable.Transition.ColorTint;
                        rightButton.transition = Selectable.Transition.ColorTint;
                        EditorThemeManager.ApplySelectable(leftButton, ThemeGroup.Function_2, false);
                        EditorThemeManager.ApplySelectable(rightButton, ThemeGroup.Function_2, false);

                        TriggerHelper.IncreaseDecreaseButtons(inputField, t: single.transform);
                        TriggerHelper.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(inputField) });

                        var inputFieldSwapper = inputField.gameObject.AddComponent<InputFieldSwapper>();
                        inputFieldSwapper.Init(inputField, InputFieldSwapper.Type.Num);
                    };

                    Action<string, int, int> integerGenerator = delegate (string label, int type, int defaultValue)
                    {
                        var single = numberInput.Duplicate(layout, label);
                        single.transform.localScale = Vector3.one;
                        var labelText = single.transform.Find("Text").GetComponent<Text>();
                        labelText.text = label;

                        var inputField = single.transform.Find("Input").GetComponent<InputField>();
                        inputField.onValueChanged.ClearAll();
                        inputField.textComponent.alignment = TextAnchor.MiddleCenter;
                        inputField.text = Parser.TryParse(type == 0 ? modifier.value : modifier.commands[type], defaultValue).ToString();
                        inputField.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                if (type == 0)
                                    modifier.value = num.ToString();
                                else
                                    modifier.commands[type] = num.ToString();
                            }

                            modifier.active = false;
                        });

                        EditorThemeManager.ApplyLightText(labelText);
                        EditorThemeManager.ApplyInputField(inputField);
                        var leftButton = single.transform.Find("<").GetComponent<Button>();
                        var rightButton = single.transform.Find(">").GetComponent<Button>();
                        leftButton.transition = Selectable.Transition.ColorTint;
                        rightButton.transition = Selectable.Transition.ColorTint;
                        EditorThemeManager.ApplySelectable(leftButton, ThemeGroup.Function_2, false);
                        EditorThemeManager.ApplySelectable(rightButton, ThemeGroup.Function_2, false);

                        TriggerHelper.IncreaseDecreaseButtonsInt(inputField, t: single.transform);
                        TriggerHelper.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(inputField) });

                        var inputFieldSwapper = inputField.gameObject.AddComponent<InputFieldSwapper>();
                        inputFieldSwapper.Init(inputField, InputFieldSwapper.Type.Num);
                    };

                    Action<string, int, bool> boolGenerator = delegate (string label, int type, bool defaultValue)
                    {
                        var global = booleanBar.Duplicate(layout, label);
                        global.transform.localScale = Vector3.one;
                        var labelText = global.transform.Find("Text").GetComponent<Text>();
                        labelText.text = label;

                        var globalToggle = global.transform.Find("Toggle").GetComponent<Toggle>();
                        globalToggle.onValueChanged.ClearAll();
                        globalToggle.isOn = Parser.TryParse(type == 0 ? modifier.value : modifier.commands[type], defaultValue);
                        globalToggle.onValueChanged.AddListener(delegate (bool _val)
                        {
                            if (type == 0)
                                modifier.value = _val.ToString();
                            else
                                modifier.commands[type] = _val.ToString();
                            modifier.active = false;
                        });

                        EditorThemeManager.ApplyLightText(labelText);
                        EditorThemeManager.ApplyToggle(globalToggle);
                    };

                    Action<string, int> stringGenerator = delegate (string label, int type)
                    {
                        var path = stringInput.Duplicate(layout, label);
                        path.transform.localScale = Vector3.one;
                        var labelText = path.transform.Find("Text").GetComponent<Text>();
                        labelText.text = label;

                        var pathInputField = path.transform.Find("Input").GetComponent<InputField>();
                        pathInputField.onValueChanged.ClearAll();
                        pathInputField.textComponent.alignment = TextAnchor.MiddleLeft;
                        pathInputField.text = type == 0 ? modifier.value : modifier.commands[type];
                        pathInputField.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (type == 0)
                                modifier.value = _val;
                            else
                                modifier.commands[type] = _val;
                            modifier.active = false;
                        });

                        EditorThemeManager.ApplyLightText(labelText);
                        EditorThemeManager.ApplyInputField(pathInputField);
                    };

                    Action<string, int> colorGenerator = delegate (string label, int type)
                    {
                        var startColorBase = numberInput.Duplicate(layout, label);
                        startColorBase.transform.localScale = Vector3.one;

                        startColorBase.transform.Find("Text").GetComponent<Text>().text = label;

                        Destroy(startColorBase.transform.Find("Input").gameObject);
                        Destroy(startColorBase.transform.Find(">").gameObject);
                        Destroy(startColorBase.transform.Find("<").gameObject);

                        var startColors = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/color/color"));
                        startColors.transform.SetParent(startColorBase.transform);
                        startColors.transform.localScale = Vector3.one;
                        startColors.name = "color";

                        if (startColors.TryGetComponent(out GridLayoutGroup scglg))
                        {
                            scglg.cellSize = new Vector2(16f, 16f);
                            scglg.spacing = new Vector2(4.66f, 2.5f);
                        }

                        startColors.transform.AsRT().sizeDelta = new Vector2(183f, 32f);

                        var toggles = startColors.GetComponentsInChildren<Toggle>();

                        foreach (var toggle in toggles)
                        {
                            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Null, toggle.gameObject, new List<Component>
                            {
                                toggle.image,
                            }, true, 1, SpriteManager.RoundedSide.W));

                            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.List_Button_1_Normal, toggle.graphic.gameObject, new List<Component>
                            {
                                toggle.graphic,
                            }));
                        }

                        SetObjectColors(startColors.GetComponentsInChildren<Toggle>(), type, Parser.TryParse(modifier.commands[type], 0), modifier);
                    };

                    Action<string, int, List<string>> dropdownGenerator = delegate (string label, int type, List<string> options)
                    {
                        var dd = dropdownBar.Duplicate(layout, label);
                        dd.transform.localScale = Vector3.one;
                        var labelText = dd.transform.Find("Text").GetComponent<Text>();
                        labelText.text = label;

                        Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
                        Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

                        var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                        d.onValueChanged.RemoveAllListeners();
                        d.options.Clear();

                        d.options = options.Select(x => new Dropdown.OptionData(x)).ToList();

                        d.value = Parser.TryParse(modifier.commands[type], 0);

                        d.onValueChanged.AddListener(delegate (int _val)
                        {
                            modifier.commands[type] = _val.ToString();
                            modifier.active = false;
                        });

                        EditorThemeManager.ApplyDropdown(d);
                    };

                    Action<string, int, List<Dropdown.OptionData>> dropdownGenerator2 = delegate (string label, int type, List<Dropdown.OptionData> options)
                    {
                        var dd = dropdownBar.Duplicate(layout, label);
                        dd.transform.localScale = Vector3.one;
                        var labelText = dd.transform.Find("Text").GetComponent<Text>();
                        labelText.text = label;

                        Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
                        Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

                        var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                        d.onValueChanged.RemoveAllListeners();
                        d.options.Clear();

                        d.options = options;

                        d.value = Parser.TryParse(modifier.commands[type], 0);

                        d.onValueChanged.AddListener(delegate (int _val)
                        {
                            modifier.commands[type] = _val.ToString();
                            modifier.active = false;
                        });

                        EditorThemeManager.ApplyDropdown(d);
                    };

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

                    var cmd = modifier.commands[0];
                    switch (cmd)
                    {
                        case "setActive":
                            {
                                boolGenerator("Active", 0, false);

                                break;
                            }
                        case "timeLesserEquals":
                        case "timeGreaterEquals":
                        case "timeLesser":
                        case "timeGreater":
                            {
                                singleGenerator("Time", 0, 0f);

                                break;
                            }
                        case "animateObject":
                            {
                                singleGenerator("Time", 0, 1f);
                                dropdownGenerator("Type", 1, new List<string> { "Position", "Scale", "Rotation" });
                                singleGenerator("X", 2, 0f);
                                singleGenerator("Y", 3, 0f);
                                singleGenerator("Z", 4, 0f);
                                boolGenerator("Relative", 5, true);

                                dropdownGenerator2("Easing", 6, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                                break;
                            }
                        case "copyAxis":
                            {
                                if (cmd == "copyAxis")
                                    stringGenerator("Object Group", 0);

                                dropdownGenerator("From Type", 1, new List<string> { "Position", "Scale", "Rotation" });
                                dropdownGenerator("From Axis", 2, new List<string> { "X", "Y", "Z" });

                                dropdownGenerator("To Type", 3, new List<string> { "Position", "Scale", "Rotation" });
                                dropdownGenerator("To Axis (3D)", 4, new List<string> { "X", "Y", "Z" });

                                if (cmd == "copyAxis")
                                    singleGenerator("Delay", 5, 0f);

                                singleGenerator("Multiply", 6, 1f);
                                singleGenerator("Offset", 7, 0f);
                                singleGenerator("Min", 8, -99999f);
                                singleGenerator("Max", 9, 99999f);

                                if (cmd == "copyAxis")
                                    singleGenerator("Loop", 10, 99999f);

                                break;
                            }
                    }

                    num++;
                }

                //Add Modifier
                {
                    var button = modifierAddPrefab.Duplicate(content, "add modifier");

                    var butt = button.GetComponent<Button>();
                    butt.onClick.RemoveAllListeners();
                    butt.onClick.AddListener(delegate ()
                    {
                        EditorManager.inst.ShowDialog("Default Background Modifiers Popup");
                        RefreshDefaultModifiersList(backgroundObject);
                    });

                    EditorThemeManager.ApplySelectable(butt, ThemeGroup.List_Button_1);
                    EditorThemeManager.ApplyLightText(button.transform.GetChild(0).GetComponent<Text>());
                }
            }

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
                toggle.onValueChanged.RemoveAllListeners();
                int tmpIndex = num;

                toggle.isOn = num == i;

                toggle.onValueChanged.AddListener(delegate (bool _value)
                {
                    SetObjectColors(toggles, index, tmpIndex, modifier);
                });

                toggle.GetComponent<Image>().color = GameManager.inst.LiveTheme.GetObjColor(tmpIndex);

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

        #endregion

        #region Default Modifiers

        public void CreateDefaultModifiersList()
        {
            var defaultModifiersList = RTEditor.inst.GeneratePopup("Default Background Modifiers Popup", "Choose a modifer to add", Vector2.zero, new Vector2(600f, 400f), delegate (string _val)
            {
                searchTerm = _val;
                if (CurrentSelectedBG)
                    RefreshDefaultModifiersList(CurrentSelectedBG);
            }, placeholderText: "Search for default Modifier...");
        }

        public int currentPage;

        public string searchTerm;
        public void RefreshDefaultModifiersList(BackgroundObject backgroundObject)
        {
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
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(delegate ()
                    {
                        var cmd = defaultModifiers[tmpIndex].commands[0];

                        var modifier = Modifier<BackgroundObject>.DeepCopy(defaultModifiers[tmpIndex], backgroundObject);
                        backgroundObject.modifiers[currentPage].Add(modifier);
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
            checkmarkImage.sprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_checkmark.png");
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

            var input = RTEditor.inst.defaultIF.Duplicate(rectTransform, "Input");
            input.transform.localScale = Vector2.one;
            ((RectTransform)input.transform.Find("Text")).sizeDelta = Vector2.zero;

            var buttonL = Button("<", SpriteManager.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_left_small.png"));
            buttonL.transform.SetParent(rectTransform);
            buttonL.transform.localScale = Vector3.one;

            ((RectTransform)buttonL.transform).sizeDelta = new Vector2(16f, 32f);

            var buttonR = Button(">", SpriteManager.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_right_small.png"));
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

            var input = RTEditor.inst.defaultIF.Duplicate(rectTransform, "Input");
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

            var dropdownInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown")
                .Duplicate(rectTransform, "Dropdown");
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
