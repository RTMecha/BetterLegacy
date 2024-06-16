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
    public class ObjectModifiersEditor : MonoBehaviour
    {
        public static ObjectModifiersEditor inst;

        public static bool installed = false;

        public Transform content;
        public Transform scrollView;
        public RectTransform scrollViewRT;

        public bool showModifiers;

        public GameObject modifierCardPrefab;
        public GameObject modifierAddPrefab;

        void Awake()
        {
            inst = this;

            CreateModifiersOnAwake();
            CreateDefaultModifiersList();
        }

        float time;
        float timeOffset;
        bool setTime;

        void Update()
        {
            if (!setTime)
            {
                timeOffset = Time.time;
                setTime = true;
            }

            time = timeOffset - Time.time;
            timeOffset = Time.time;

            try
            {
                if (RTEditor.ShowModdedUI && ObjectEditor.inst.SelectedObjectCount == 1 && ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                    intVariable.text = $"Integer Variable: [ {ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>().integerVariable} ]";
            }
            catch
            {

            }
        }

        public Text intVariable;

        public Toggle ignoreToggle;

        public bool renderingModifiers;

        public void CreateModifiersOnAwake()
        {
            var bmb = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View");

            // Integer variable
            {
                var label = ObjEditor.inst.ObjectView.transform.ChildList().First(x => x.name == "label").gameObject.Duplicate(ObjEditor.inst.ObjectView.transform, "int_variable");

                Destroy(label.transform.GetChild(1).gameObject);
                intVariable = label.transform.GetChild(0).GetComponent<Text>();
                intVariable.text = "Integer Variable: [ null ]";
                intVariable.fontSize = 18;
                EditorThemeManager.AddLightText(intVariable);
            }

            // Ignored Lifespan
            {
                var ignoreGameObject = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/grain/colored"));
                ignoreGameObject.transform.SetParent(bmb.transform.Find("Viewport/Content"));
                ignoreGameObject.transform.localScale = Vector3.one;
                ignoreGameObject.name = "ignore life";
                var ignoreLifeText = ignoreGameObject.transform.Find("Text").GetComponent<Text>();
                ignoreLifeText.text = "Ignore Lifespan";

                ignoreToggle = ignoreGameObject.GetComponent<Toggle>();

                EditorThemeManager.AddToggle(ignoreToggle, graphic: ignoreLifeText);
            }

            var act = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/grain/colored"));
            act.transform.SetParent(bmb.transform.Find("Viewport/Content"));
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
                if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                    RTEditor.inst.StartCoroutine(ObjectEditor.RefreshObjectGUI(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>()));
            });

            EditorThemeManager.AddToggle(toggle, graphic: activeText);

            var e = Instantiate(bmb);

            scrollView = e.transform;

            scrollView.SetParent(bmb.transform.Find("Viewport/Content"));
            scrollView.localScale = Vector3.one;
            scrollView.name = "Modifiers Scroll View";

            scrollViewRT = scrollView.GetComponent<RectTransform>();

            content = scrollView.Find("Viewport/Content");
            LSHelpers.DeleteChildren(content);

            scrollView.gameObject.SetActive(showModifiers);

            modifierCardPrefab = new GameObject("Modifier Prefab");
            modifierCardPrefab.transform.SetParent(transform);
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

            var mcpText = new GameObject("Text");
            mcpText.transform.SetParent(mcpLabelRT);
            mcpText.transform.localScale = Vector3.one;
            var mcpTextRT = mcpText.AddComponent<RectTransform>();
            UIManager.SetRectTransform(mcpTextRT, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, 32f));

            var mcpTextText = mcpText.AddComponent<Text>();
            mcpTextText.alignment = TextAnchor.MiddleLeft;
            mcpTextText.horizontalOverflow = HorizontalWrapMode.Overflow;
            mcpTextText.font = FontManager.inst.DefaultFont;
            mcpTextText.fontSize = 19;
            mcpTextText.color = new Color(0.9373f, 0.9216f, 0.9373f);

            var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(mcpLabelRT, "Delete");
            delete.transform.localScale = Vector3.one;
            var deleteLayoutElement = delete.GetComponent<LayoutElement>() ?? delete.AddComponent<LayoutElement>();
            deleteLayoutElement.minWidth = 32f;

            UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(150f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 32f));

            var duplicate = EditorPrefabHolder.Instance.DeleteButton.Duplicate(mcpLabelRT, "Copy");
            duplicate.transform.localScale = Vector3.one;
            var duplicateLayoutElement = duplicate.GetComponent<LayoutElement>() ?? duplicate.AddComponent<LayoutElement>();
            duplicateLayoutElement.minWidth = 32f;

            UIManager.SetRectTransform(duplicate.transform.AsRT(), new Vector2(116f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 32f));

            duplicate.GetComponent<DeleteButtonStorage>().image.sprite = SpriteManager.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}copy.png");

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

            modifierAddPrefab = EditorManager.inst.folderButtonPrefab.Duplicate(transform, "add modifier");

            var text = modifierAddPrefab.transform.GetChild(0).GetComponent<Text>();
            text.text = "+";
            text.alignment = TextAnchor.MiddleCenter;

            booleanBar = Boolean();

            numberInput = NumberInput();

            stringInput = StringInput();

            dropdownBar = Dropdown();
        }

        public static Modifier<BeatmapObject> copiedModifier;
        public IEnumerator RenderModifiers(BeatmapObject beatmapObject)
        {
            ignoreToggle.onValueChanged.ClearAll();
            ignoreToggle.isOn = beatmapObject.ignoreLifespan;
            ignoreToggle.onValueChanged.AddListener(delegate (bool _val)
            {
                beatmapObject.ignoreLifespan = _val;
            });

            if (!showModifiers)
                yield break;

            renderingModifiers = true;

            LSHelpers.DeleteChildren(content);

            ((RectTransform)content.parent.parent).sizeDelta = new Vector2(351f, 300f * Mathf.Clamp(beatmapObject.modifiers.Count, 1, 5));

            int num = 0;
            foreach (var modifier in beatmapObject.modifiers)
            {
                int index = num;
                var name = modifier.commands.Count > 0 ? modifier.commands[0] : "Invalid Modifier";
                var gameObject = modifierCardPrefab.Duplicate(content, name);
                EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.List_Button_1_Normal, gameObject, new List<Component>
                    {
                        gameObject.GetComponent<Image>(),
                    }, true, 1, SpriteManager.RoundedSide.W));
                gameObject.transform.localScale = Vector3.one;
                var modifierTitle = gameObject.transform.Find("Label/Text").GetComponent<Text>();
                modifierTitle.text = name;
                EditorThemeManager.ApplyLightText(modifierTitle);

                var delete = gameObject.transform.Find("Label/Delete").GetComponent<DeleteButtonStorage>();
                delete.button.onClick.ClearAll();
                delete.button.onClick.AddListener(delegate ()
                {
                    beatmapObject.modifiers.RemoveAt(index);
                    beatmapObject.reactivePositionOffset = Vector3.zero;
                    beatmapObject.reactiveScaleOffset = Vector3.zero;
                    beatmapObject.reactiveRotationOffset = 0f;
                    Updater.UpdateProcessor(beatmapObject);
                    StartCoroutine(RenderModifiers(beatmapObject));
                });

                EditorThemeManager.ApplyGraphic(delete.button.image, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(delete.image, ThemeGroup.Delete_Text);

                var copy = gameObject.transform.Find("Label/Copy").GetComponent<DeleteButtonStorage>();
                copy.button.onClick.ClearAll();
                copy.button.onClick.AddListener(delegate ()
                {
                    copiedModifier = Modifier<BeatmapObject>.DeepCopy(modifier, beatmapObject);
                    StartCoroutine(RenderModifiers(beatmapObject));
                    EditorManager.inst.DisplayNotification("Copied Modifier!", 1.5f, EditorManager.NotificationType.Success);
                });

                EditorThemeManager.ApplyGraphic(copy.button.image, ThemeGroup.Copy, true);
                EditorThemeManager.ApplyGraphic(copy.image, ThemeGroup.Copy_Text);

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

                    var labelText = startColorBase.transform.Find("Text").GetComponent<Text>();
                    labelText.text = label;

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

                    EditorThemeManager.ApplyLightText(labelText);
                    SetObjectColors(startColors.GetComponentsInChildren<Toggle>(), type, Parser.TryParse(type == 0 ? modifier.value : modifier.commands[type], 0), modifier);
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

                    d.value = Parser.TryParse(type == 0 ? modifier.value : modifier.commands[type], 0);

                    d.onValueChanged.AddListener(delegate (int _val)
                    {
                        if (type == 0)
                            modifier.value = _val.ToString();
                        else
                            modifier.commands[type] = _val.ToString();
                        modifier.active = false;
                    });

                    EditorThemeManager.ApplyLightText(labelText);
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

                    d.value = Parser.TryParse(type == 0 ? modifier.value : modifier.commands[type], 0);

                    d.onValueChanged.AddListener(delegate (int _val)
                    {
                        if (type == 0)
                            modifier.value = _val.ToString();
                        else
                            modifier.commands[type] = _val.ToString();
                        modifier.active = false;
                    });

                    EditorThemeManager.ApplyLightText(labelText);
                    EditorThemeManager.ApplyDropdown(d);
                };

                if (!modifier.verified)
                {
                    modifier.verified = true;
                    modifier.VerifyModifier(ModifiersManager.defaultBeatmapObjectModifiers);
                }

                if (!modifier.IsValid(ModifiersManager.defaultBeatmapObjectModifiers))
                {
                    EditorManager.inst.DisplayNotification("Modifier does not have a command name and is lacking values.", 2f, EditorManager.NotificationType.Error);
                    continue;
                }

                var cmd = modifier.commands[0];
                switch (cmd)
                {
                    #region Float
                    case "setPitch":
                    case "addPitch":
                    case "setMusicTime":
                    case "pitchEquals":
                    case "pitchLesserEquals":
                    case "pitchGreaterEquals":
                    case "pitchLesser":
                    case "pitchGreater":
                    case "playerDistanceLesser":
                    case "playerDistanceGreater":
                    case "setAlpha":
                    case "setAlphaOther":
                    case "blackHole":
                    case "musicTimeGreater":
                    case "musicTimeLesser":
                    case "playerSpeed":
                        {
                            singleGenerator("Value", 0, 1f);

                            if (cmd == "setAlphaOther")
                                stringGenerator("Object Group", 1);

                            if (cmd == "blackHole")
                            {
                                if (modifier.commands.Count < 2)
                                {
                                    modifier.commands.Add("False");
                                }

                                boolGenerator("Use Opacity", 1, false);
                            }

                            break;
                        }
                    #endregion
                    #region Sound
                    case "playSoundOnline":
                    case "playSound":
                        {
                            stringGenerator("Path", 0);
                            {
                                var search = layout.Find("Path/Input").gameObject.AddComponent<Clickable>();
                                search.onClick = delegate (PointerEventData pointerEventData)
                                {
                                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                                    {
                                        EditorManager.inst.ShowDialog("Browser Popup");

                                        var isGlobal = modifier.commands.Count > 1 && Parser.TryParse(modifier.commands[1], false);
                                        var directory = isGlobal && RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/soundlibrary") ?
                                                        RTFile.ApplicationDirectory + "beatmaps/soundlibrary" : System.IO.Path.GetDirectoryName(RTFile.BasePath);

                                        if (isGlobal && !RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/soundlibrary"))
                                        {
                                            EditorManager.inst.DisplayNotification("soundlibrary folder does not exist! If you want to have audio take from a global folder, make sure you create a soundlibrary folder inside your beatmaps folder and put your sounds in there.", 12f, EditorManager.NotificationType.Error);
                                        }

                                        RTFileBrowser.inst.UpdateBrowser(directory, new string[] { ".wav", ".ogg", ".mp3" }, onSelectFile: delegate (string _val)
                                        {
                                            var global = Parser.TryParse(modifier.commands[1], false);

                                            if (_val.Replace("\\", "/").Contains(global ? RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/soundlibrary/" : GameManager.inst.basePath.Replace("\\", "/")))
                                            {
                                                layout.Find("Path/Input").GetComponent<InputField>().text = _val.Replace("\\", "/").Replace(global ? RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/soundlibrary/" : GameManager.inst.basePath.Replace("\\", "/"), "");
                                                EditorManager.inst.HideDialog("Browser Popup");
                                            }
                                            else
                                            {
                                                EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                            }
                                        });
                                    }
                                };
                            }
                            boolGenerator("Global", 1, false);
                            singleGenerator("Pitch", 2, 1f);
                            singleGenerator("Volume", 3, 1f);
                            boolGenerator("Loop", 4, false);

                            break;
                        }
                    case "audioSource":
                        {
                            stringGenerator("Path", 0);
                            {
                                var search = layout.Find("Path/Input").gameObject.AddComponent<Clickable>();
                                search.onClick = delegate (PointerEventData pointerEventData)
                                {
                                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                                    {
                                        EditorManager.inst.ShowDialog("Browser Popup");

                                        var isGlobal = modifier.commands.Count > 1 && Parser.TryParse(modifier.commands[1], false);
                                        var directory = isGlobal && RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/soundlibrary") ?
                                                        RTFile.ApplicationDirectory + "beatmaps/soundlibrary" : System.IO.Path.GetDirectoryName(RTFile.BasePath);

                                        if (isGlobal && !RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/soundlibrary"))
                                        {
                                            EditorManager.inst.DisplayNotification("soundlibrary folder does not exist! If you want to have audio take from a global folder, make sure you create a soundlibrary folder inside your beatmaps folder and put your sounds in there.", 12f, EditorManager.NotificationType.Error);
                                        }

                                        RTFileBrowser.inst.UpdateBrowser(directory, new string[] { ".wav", ".ogg", ".mp3" }, onSelectFile: delegate (string _val)
                                        {
                                            var global = Parser.TryParse(modifier.commands[1], false);

                                            if (_val.Replace("\\", "/").Contains(global ? RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/soundlibrary/" : GameManager.inst.basePath.Replace("\\", "/")))
                                            {
                                                layout.Find("Path/Input").GetComponent<InputField>().text = _val.Replace("\\", "/").Replace(global ? RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/soundlibrary/" : GameManager.inst.basePath.Replace("\\", "/"), "");
                                                EditorManager.inst.HideDialog("Browser Popup");
                                            }
                                            else
                                            {
                                                EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                            }
                                        });
                                    }
                                };
                            }
                            boolGenerator("Global", 1, false);

                            break;
                        }
                    #endregion
                    #region String
                    case "updateObject":
                    case "copyColor":
                    case "copyColorOther":
                    case "loadLevel":
                    case "loadLevelInternal":
                    case "loadLevelID":
                    case "setText":
                    case "setTextOther":
                    case "addText":
                    case "addTextOther":
                    case "objectCollide":
                    case "setImage":
                    case "setImageOther":
                    case "code":
                    case "setWindowTitle":
                    case "realTimeDayWeekEquals":
                        {
                            if (cmd == "setTextOther" || cmd == "addTextOther" || cmd == "setImageOther")
                            {
                                stringGenerator("Object Group", 1);
                                stringGenerator(cmd == "setImageOther" ? "Path" : "Text", 0);
                            }

                            if (cmd == "updateObject" || cmd == "copyColor" || cmd == "copyColorOther" || cmd == "objectCollide")
                                stringGenerator("Object Group", 0);
                            else if (cmd != "setTextOther" && cmd != "addTextOther" && cmd != "setImageOther")
                                stringGenerator(
                                    cmd == "setText" || cmd == "addText" ? "Text" :
                                    cmd == "code" ? "Code" :
                                    cmd == "setWindowTitle" ? "Title" :
                                    cmd == "realTimeDayWeekEquals" ? "Day" :
                                    "Path", 0);

                            break;
                        }
                    case "textSequence":
                        {
                            singleGenerator("Time", 0, 1f);
                            boolGenerator("Display Glitch", 1, true);
                            boolGenerator("Play Sound", 2, true);
                            boolGenerator("Custom Sound", 3, false);
                            stringGenerator("Sound Path", 4);
                            boolGenerator("Global", 5, false);

                            singleGenerator("Pitch", 6, 1f);
                            singleGenerator("Volume", 7, 1f);

                            break;
                        }
                    #endregion
                    #region Component
                    case "blur":
                    case "blurOther":
                    case "blurVariable":
                    case "blurVariableOther":
                    case "blurColored":
                    case "blurColoredOther":
                        {
                            singleGenerator("Amount", 0, 0.5f);

                            if (cmd == "blur" || cmd == "blurColored")
                            {
                                boolGenerator("Use Opacity", 1, false);

                                if (modifier.commands.Count < 3)
                                {
                                    modifier.commands.Add("False");
                                }
                            }

                            if (cmd == "blurVariableOther" || cmd == "blurOther" || cmd == "blurColoredOther")
                                stringGenerator("Object Group", 1);

                            boolGenerator("Set Back to Normal", cmd != "blurVariable" ? 2 : 1, false);

                            break;
                        }
                    case "particleSystem":
                        {
                            singleGenerator("LifeTime", 0, 5f);

                            // Shape
                            {
                                var dd = dropdownBar.Duplicate(layout, "Shape");
                                dd.transform.localScale = Vector3.one;
                                var labelText = dd.transform.Find("Text").GetComponent<Text>();
                                labelText.text = "Shape";

                                Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
                                Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

                                var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                                d.onValueChanged.RemoveAllListeners();
                                d.options.Clear();
                                d.options = new List<Dropdown.OptionData>
                                {
                                    new Dropdown.OptionData("Square"),
                                    new Dropdown.OptionData("Circle"),
                                    new Dropdown.OptionData("Triangle"),
                                    new Dropdown.OptionData("Arrow"),
                                    new Dropdown.OptionData("Text"),
                                    new Dropdown.OptionData("Hexagon"),
                                    new Dropdown.OptionData("Image"),
                                    new Dropdown.OptionData("Pentagon"),
                                    new Dropdown.OptionData("Misc"),
                                };

                                d.value = Parser.TryParse(modifier.commands[1], 0);

                                d.onValueChanged.AddListener(delegate (int _val)
                                {
                                    if (_val == 4 || _val == 6)
                                    {
                                        EditorManager.inst.DisplayNotification("Shape type not available for particle system.", 1.5f, EditorManager.NotificationType.Warning);
                                        d.value = Parser.TryParse(modifier.commands[1], 0);
                                        return;
                                    }

                                    modifier.commands[1] = Mathf.Clamp(_val, 0, ShapeManager.inst.Shapes2D.Count - 1).ToString();
                                    modifier.active = false;
                                    StartCoroutine(RenderModifiers(beatmapObject));
                                    Updater.UpdateProcessor(beatmapObject);
                                });

                                EditorThemeManager.ApplyLightText(labelText);
                                EditorThemeManager.ApplyDropdown(d);

                                TriggerHelper.AddEventTriggerParams(d.gameObject, TriggerHelper.ScrollDelta(d));
                            }

                            // Shape Option
                            {
                                var dd = dropdownBar.Duplicate(layout, "Shape");
                                dd.transform.localScale = Vector3.one;
                                var labelText = dd.transform.Find("Text").GetComponent<Text>();
                                labelText.text = "Shape";

                                Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
                                Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

                                var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                                d.onValueChanged.RemoveAllListeners();
                                d.options.Clear();

                                var type = Parser.TryParse(modifier.commands[1], 0);
                                for (int i = 0; i < ShapeManager.inst.Shapes2D[type].Count; i++)
                                {
                                    var shape = ShapeManager.inst.Shapes2D[type][i].name.Replace("_", " ");
                                    d.options.Add(new Dropdown.OptionData(shape, ShapeManager.inst.Shapes2D[type][i].Icon));
                                }

                                d.value = Parser.TryParse(modifier.commands[2], 0);

                                d.onValueChanged.AddListener(delegate (int _val)
                                {
                                    modifier.commands[2] = Mathf.Clamp(_val, 0, ShapeManager.inst.Shapes2D[type].Count - 1).ToString();
                                    modifier.active = false;
                                    Updater.UpdateProcessor(beatmapObject);
                                });

                                EditorThemeManager.ApplyLightText(labelText);
                                EditorThemeManager.ApplyDropdown(d);

                                TriggerHelper.AddEventTriggerParams(d.gameObject, TriggerHelper.ScrollDelta(d));
                            }

                            colorGenerator("Color", 3);
                            singleGenerator("StartOpacity", 4, 1f);
                            singleGenerator("EndOpacity", 5, 0f);
                            singleGenerator("StartScale", 6, 1f);
                            singleGenerator("EndScale", 7, 0f);
                            singleGenerator("Rotation", 8, 0f);
                            singleGenerator("Speed", 9, 5f);
                            singleGenerator("Amount", 10, 1f);
                            singleGenerator("Duration", 11, 1f);
                            singleGenerator("Force X", 12, 0f);
                            singleGenerator("Force Y", 13, 0f);
                            boolGenerator("Trail Emit", 14, false);

                            break;
                        }
                    case "trailRenderer":
                        {
                            singleGenerator("Time", 0, 1f);
                            singleGenerator("StartWidth", 1, 1f);
                            singleGenerator("EndWidth", 2, 0f);
                            colorGenerator("StartColor", 3);
                            colorGenerator("EndColor", 5);
                            singleGenerator("StartOpacity", 4, 1f);
                            singleGenerator("EndOpacity", 6, 0f);

                            break;
                        }
                    case "rigidbody":
                    case "rigidbodyOther":
                        {
                            if (cmd == "rigidbodyOther")
                                stringGenerator("Object Group", 0);

                            singleGenerator("Gravity", 1, 0f);

                            dropdownGenerator("Collision Mode", 2, new List<string> { "Discrete", "Continuous" });

                            singleGenerator("Drag", 3, 0f);
                            singleGenerator("Velocity X", 4, 0f);
                            singleGenerator("Velocity Y", 5, 0f);

                            dropdownGenerator("Body Type", 6, new List<string> { "Dynamic", "Kinematic", "Static" });

                            break;
                        }
                    #endregion
                    #region Integer
                    case "playerHit":
                    case "playerHitAll":
                    case "playerHeal":
                    case "playerHealAll":
                    case "addVariable":
                    case "subVariable":
                    case "setVariable":
                    case "mouseButtonDown":
                    case "mouseButton":
                    case "mouseButtonUp":
                    case "playerHealthEquals":
                    case "playerHealthLesserEquals":
                    case "playerHealthGreaterEquals":
                    case "playerHealthLesser":
                    case "playerHealthGreater":
                    case "playerDeathsEquals":
                    case "playerDeathsLesserEquals":
                    case "playerDeathsGreaterEquals":
                    case "playerDeathsLesser":
                    case "playerDeathsGreater":
                    case "variableEquals":
                    case "variableLesserEquals":
                    case "variableGreaterEquals":
                    case "variableLesser":
                    case "variableGreater":
                    case "variableOtherEquals":
                    case "variableOtherLesserEquals":
                    case "variableOtherGreaterEquals":
                    case "variableOtherLesser":
                    case "variableOtherGreater":
                    case "removeText":
                    case "removeTextAt":
                    case "removeTextOther":
                    case "removeTextOtherAt":
                    case "playerBoostEquals":
                    case "playerBoostLesserEquals":
                    case "playerBoostGreaterEquals":
                    case "playerBoostLesser":
                    case "playerBoostGreater":
                    case "realTimeSecondEquals":
                    case "realTimeSecondLesserEquals":
                    case "realTimeSecondGreaterEquals":
                    case "realTimeSecondLesser":
                    case "realTimeSecondGreater":
                    case "realTimeMinuteEquals":
                    case "realTimeMinuteLesserEquals":
                    case "realTimeMinuteGreaterEquals":
                    case "realTimeMinuteLesser":
                    case "realTimeMinuteGreater":
                    case "realTime12HourEquals":
                    case "realTime12HourLesserEquals":
                    case "realTime12HourGreaterEquals":
                    case "realTime12HourLesser":
                    case "realTime12HourGreater":
                    case "realTime24HourEquals":
                    case "realTime24HourLesserEquals":
                    case "realTime24HourGreaterEquals":
                    case "realTime24HourLesser":
                    case "realTime24HourGreater":
                    case "realTimeDayEquals":
                    case "realTimeDayLesserEquals":
                    case "realTimeDayGreaterEquals":
                    case "realTimeDayLesser":
                    case "realTimeDayGreater":
                    case "realTimeMonthEquals":
                    case "realTimeMonthLesserEquals":
                    case "realTimeMonthGreaterEquals":
                    case "realTimeMonthLesser":
                    case "realTimeMonthGreater":
                    case "realTimeYearEquals":
                    case "realTimeYearLesserEquals":
                    case "realTimeYearGreaterEquals":
                    case "realTimeYearLesser":
                    case "realTimeYearGreater":
                        {
                            integerGenerator("Value", 0, 0);

                            if (cmd == "addVariable" || cmd == "subVariable" || cmd == "setVariable" || cmd.Contains("variableOther") || cmd == "setAlphaOther" || cmd == "removeTextOther" || cmd == "removeTextOtherAt")
                            {
                                stringGenerator("Object Group", 1);
                            }

                            break;
                        }
                    #endregion
                    #region Key
                    case "keyPressDown":
                    case "keyPress":
                    case "keyPressUp":
                        {
                            var dd = dropdownBar.Duplicate(layout, "Key");
                            var labelText = dd.transform.Find("Text").GetComponent<Text>();
                            labelText.text = "Value";

                            Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());

                            var hide = dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>();
                            hide.DisabledOptions.Clear();
                            var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                            d.onValueChanged.RemoveAllListeners();
                            d.options.Clear();

                            var keyCodes = Enum.GetValues(typeof(KeyCode));

                            for (int i = 0; i < keyCodes.Length; i++)
                            {
                                var str = Enum.GetName(typeof(KeyCode), i) ?? "Invalid Value";

                                hide.DisabledOptions.Add(string.IsNullOrEmpty(Enum.GetName(typeof(KeyCode), i)));

                                d.options.Add(new Dropdown.OptionData(str));
                            }

                            d.value = Parser.TryParse(modifier.value, 0);

                            d.onValueChanged.AddListener(delegate (int _val)
                            {
                                modifier.value = _val.ToString();
                            });

                            EditorThemeManager.ApplyLightText(labelText);
                            EditorThemeManager.ApplyDropdown(d);

                            break;
                        }
                    #endregion
                    #region Save / Load JSON
                    case "loadEquals":
                    case "loadLesserEquals":
                    case "loadGreaterEquals":
                    case "loadLesser":
                    case "loadGreater":
                    case "loadExists":
                    case "saveFloat":
                    case "saveString":
                    case "saveText":
                    case "saveVariable":
                        {
                            if (cmd == "loadEquals" && modifier.commands.Count < 5)
                                modifier.commands.Add("0");

                            if (cmd == "loadEquals" && Parser.TryParse(modifier.commands[4], 0) == 0 && !float.TryParse(modifier.value, out float abcdef))
                                modifier.value = "0";

                            stringGenerator("Path", 1);
                            stringGenerator("JSON 1", 2);
                            stringGenerator("JSON 2", 3);

                            if (cmd != "saveVariable" && cmd != "saveText" && cmd != "loadExists" && cmd != "saveString" && (cmd != "loadEquals" || Parser.TryParse(modifier.commands[4], 0) == 0))
                                singleGenerator("Value", 0, 0f);

                            if (cmd == "saveString" || cmd == "loadEquals" && Parser.TryParse(modifier.commands[4], 0) == 1)
                                stringGenerator("Value", 0);

                            if (cmd == "loadEquals")
                            {
                                var dd = dropdownBar.Duplicate(layout, "Type");
                                dd.transform.localScale = Vector3.one;
                                var labelText = dd.transform.Find("Text").GetComponent<Text>();
                                labelText.text = "Type";

                                Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
                                Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

                                var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                                d.onValueChanged.RemoveAllListeners();
                                d.options.Clear();

                                d.options = new List<Dropdown.OptionData>
                                {
                                    new Dropdown.OptionData("Number"),
                                    new Dropdown.OptionData("Text"),
                                };

                                d.value = Parser.TryParse(modifier.commands[4], 0);

                                d.onValueChanged.AddListener(delegate (int _val)
                                {
                                    modifier.commands[4] = _val.ToString();
                                    modifier.active = false;
                                    StartCoroutine(RenderModifiers(beatmapObject));
                                });

                                EditorThemeManager.ApplyLightText(labelText);
                                EditorThemeManager.ApplyDropdown(d);
                            }

                            break;
                        }
                    case "loadVariable":
                    case "loadVariableOther":
                        {
                            stringGenerator("Path", 1);
                            stringGenerator("JSON 1", 2);
                            stringGenerator("JSON 2", 3);

                            if (cmd.Contains("Other"))
                                stringGenerator("Object Group", 0);

                            break;
                        }
                    #endregion
                    #region Reactive
                    case "reactivePos":
                    case "reactiveSca":
                    case "reactiveRot":
                    case "reactiveCol":
                    case "reactiveColLerp":
                    case "reactivePosChain":
                    case "reactiveScaChain":
                    case "reactiveRotChain":
                        {
                            singleGenerator("Total Multiply", 0, 0f);

                            if (cmd == "reactivePos" || cmd == "reactiveSca" || cmd == "reactivePosChain" || cmd == "reactiveScaChain")
                            {
                                var samplesX = numberInput.Duplicate(layout, "Value");
                                var samplesXLabel = samplesX.transform.Find("Text").GetComponent<Text>();
                                samplesXLabel.text = "Sample X";

                                var samplesXIF = samplesX.transform.Find("Input").GetComponent<InputField>();
                                samplesXIF.onValueChanged.ClearAll();
                                samplesXIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                samplesXIF.text = Parser.TryParse(modifier.commands[1], 0).ToString();
                                samplesXIF.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (int.TryParse(_val, out int result))
                                    {
                                        modifier.commands[1] = result.ToString();
                                        modifier.active = false;
                                    }
                                });

                                EditorThemeManager.ApplyLightText(samplesXLabel);
                                EditorThemeManager.ApplyInputField(samplesXIF);
                                var samplesXLeftButton = samplesX.transform.Find("<").GetComponent<Button>();
                                var samplesXRightButton = samplesX.transform.Find(">").GetComponent<Button>();
                                samplesXLeftButton.transition = Selectable.Transition.ColorTint;
                                samplesXRightButton.transition = Selectable.Transition.ColorTint;
                                EditorThemeManager.ApplySelectable(samplesXLeftButton, ThemeGroup.Function_2, false);
                                EditorThemeManager.ApplySelectable(samplesXRightButton, ThemeGroup.Function_2, false);

                                var samplesY = numberInput.Duplicate(layout, "Value");
                                var samplesYLabel = samplesY.transform.Find("Text").GetComponent<Text>();
                                samplesYLabel.text = "Sample Y";

                                var samplesYIF = samplesY.transform.Find("Input").GetComponent<InputField>();
                                samplesYIF.onValueChanged.ClearAll();
                                samplesYIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                samplesYIF.text = Parser.TryParse(modifier.commands[2], 0).ToString();
                                samplesYIF.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (int.TryParse(_val, out int result))
                                    {
                                        modifier.commands[2] = result.ToString();
                                        modifier.active = false;
                                    }
                                });

                                EditorThemeManager.ApplyLightText(samplesYLabel);
                                EditorThemeManager.ApplyInputField(samplesYIF);
                                var samplesYLeftButton = samplesY.transform.Find("<").GetComponent<Button>();
                                var samplesYRightButton = samplesY.transform.Find(">").GetComponent<Button>();
                                samplesYLeftButton.transition = Selectable.Transition.ColorTint;
                                samplesYRightButton.transition = Selectable.Transition.ColorTint;
                                EditorThemeManager.ApplySelectable(samplesYLeftButton, ThemeGroup.Function_2, false);
                                EditorThemeManager.ApplySelectable(samplesYRightButton, ThemeGroup.Function_2, false);

                                TriggerHelper.IncreaseDecreaseButtonsInt(samplesXIF, t: samplesX.transform);
                                TriggerHelper.IncreaseDecreaseButtonsInt(samplesYIF, t: samplesY.transform);
                                TriggerHelper.AddEventTriggerParams(samplesXIF.gameObject,
                                    TriggerHelper.ScrollDeltaInt(samplesXIF, multi: true),
                                    TriggerHelper.ScrollDeltaVector2Int(samplesXIF, samplesYIF, 1, new List<int> { 0, 255 }));
                                TriggerHelper.AddEventTriggerParams(samplesYIF.gameObject,
                                    TriggerHelper.ScrollDeltaInt(samplesYIF, multi: true),
                                    TriggerHelper.ScrollDeltaVector2Int(samplesXIF, samplesYIF, 1, new List<int> { 0, 255 }));

                                var multiplyX = numberInput.Duplicate(layout, "Value");
                                var multiplyXLabel = multiplyX.transform.Find("Text").GetComponent<Text>();
                                multiplyXLabel.text = "Multiply X";

                                var multiplyXIF = multiplyX.transform.Find("Input").GetComponent<InputField>();
                                multiplyXIF.onValueChanged.ClearAll();
                                multiplyXIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                multiplyXIF.text = Parser.TryParse(modifier.commands[3], 0f).ToString();
                                multiplyXIF.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (float.TryParse(_val, out float result))
                                    {
                                        modifier.commands[3] = result.ToString();
                                        modifier.active = false;
                                    }
                                });

                                EditorThemeManager.ApplyLightText(multiplyXLabel);
                                EditorThemeManager.ApplyInputField(multiplyXIF);
                                var multiplyXLeftButton = multiplyX.transform.Find("<").GetComponent<Button>();
                                var multiplyXRightButton = multiplyX.transform.Find(">").GetComponent<Button>();
                                multiplyXLeftButton.transition = Selectable.Transition.ColorTint;
                                multiplyXRightButton.transition = Selectable.Transition.ColorTint;
                                EditorThemeManager.ApplySelectable(multiplyXLeftButton, ThemeGroup.Function_2, false);
                                EditorThemeManager.ApplySelectable(multiplyXRightButton, ThemeGroup.Function_2, false);

                                var multiplyY = numberInput.Duplicate(layout, "Value");
                                var multiplyYLabel = multiplyY.transform.Find("Text").GetComponent<Text>();
                                multiplyYLabel.text = "Multiply Y";

                                var multiplyYIF = multiplyY.transform.Find("Input").GetComponent<InputField>();
                                multiplyYIF.onValueChanged.ClearAll();
                                multiplyYIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                multiplyYIF.text = Parser.TryParse(modifier.commands[4], 0f).ToString();
                                multiplyYIF.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (float.TryParse(_val, out float result))
                                    {
                                        modifier.commands[4] = result.ToString();
                                        modifier.active = false;
                                    }
                                });

                                EditorThemeManager.ApplyLightText(multiplyYLabel);
                                EditorThemeManager.ApplyInputField(multiplyYIF);
                                var multiplyYLeftButton = multiplyY.transform.Find("<").GetComponent<Button>();
                                var multiplyYRightButton = multiplyY.transform.Find(">").GetComponent<Button>();
                                multiplyYLeftButton.transition = Selectable.Transition.ColorTint;
                                multiplyYRightButton.transition = Selectable.Transition.ColorTint;
                                EditorThemeManager.ApplySelectable(multiplyYLeftButton, ThemeGroup.Function_2, false);
                                EditorThemeManager.ApplySelectable(multiplyYRightButton, ThemeGroup.Function_2, false);

                                TriggerHelper.IncreaseDecreaseButtons(multiplyXIF, t: multiplyX.transform);
                                TriggerHelper.IncreaseDecreaseButtons(multiplyYIF, t: multiplyY.transform);
                                TriggerHelper.AddEventTriggerParams(multiplyXIF.gameObject,
                                    TriggerHelper.ScrollDelta(multiplyXIF, multi: true),
                                    TriggerHelper.ScrollDeltaVector2(multiplyXIF, multiplyYIF, 0.1f, 10f));
                                TriggerHelper.AddEventTriggerParams(multiplyYIF.gameObject,
                                    TriggerHelper.ScrollDelta(multiplyYIF, multi: true),
                                    TriggerHelper.ScrollDeltaVector2(multiplyXIF, multiplyYIF, 0.1f, 10f));
                            }
                            else
                            {
                                integerGenerator("Sample", 1, 0);

                                if (cmd == "reactiveCol" || cmd == "reactiveColLerp")
                                {
                                    colorGenerator("Color", 2);
                                }
                            }

                            break;
                        }
                    #endregion
                    #region Mod Compatibility
                    case "setPlayerModel":
                        {
                            var single = numberInput.Duplicate(layout, "Value");
                            var labelText = single.transform.Find("Text").GetComponent<Text>();
                            labelText.text = "Index";

                            var inputField = single.transform.Find("Input").GetComponent<InputField>();
                            inputField.onValueChanged.ClearAll();
                            inputField.textComponent.alignment = TextAnchor.MiddleCenter;
                            inputField.text = Parser.TryParse(modifier.commands[1], 0).ToString();
                            inputField.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (int.TryParse(_val, out int result))
                                {
                                    modifier.commands[1] = Mathf.Clamp(result, 0, 3).ToString();
                                    modifier.active = false;
                                }
                            });

                            EditorThemeManager.ApplyLightText(labelText);
                            EditorThemeManager.ApplyInputField(inputField);
                            var leftButton = single.transform.Find("<").GetComponent<Button>();
                            var rightButton = single.transform.Find(">").GetComponent<Button>();
                            leftButton.transition = Selectable.Transition.ColorTint;
                            rightButton.transition = Selectable.Transition.ColorTint;
                            EditorThemeManager.ApplySelectable(leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.ApplySelectable(rightButton, ThemeGroup.Function_2, false);

                            TriggerHelper.IncreaseDecreaseButtonsInt(inputField, 1, 0, 3, single.transform);
                            TriggerHelper.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(inputField, 1, 0, 3) });

                            stringGenerator("Model ID", 0);

                            break;
                        }
                    case "eventOffset":
                    case "eventOffsetVariable":
                    case "eventOffsetAnimate":
                        {
                            // Event Keyframe Type
                            dropdownGenerator("Event Type", 1, RTEventEditor.EventTypes.ToList());

                            var vindex = numberInput.Duplicate(layout, "Value");
                            var labelText = vindex.transform.Find("Text").GetComponent<Text>();
                            labelText.text = "Val Index";

                            var vindexIF = vindex.transform.Find("Input").GetComponent<InputField>();
                            vindexIF.onValueChanged.ClearAll();
                            vindexIF.textComponent.alignment = TextAnchor.MiddleCenter;
                            vindexIF.text = Parser.TryParse(modifier.commands[2], 0).ToString();
                            vindexIF.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (int.TryParse(_val, out int result))
                                {
                                    modifier.commands[2] = Mathf.Clamp(result, 0, GameData.DefaultKeyframes[Parser.TryParse(modifier.commands[1], 0)].eventValues.Length - 1).ToString();
                                    modifier.active = false;
                                }
                            });

                            EditorThemeManager.ApplyLightText(labelText);
                            EditorThemeManager.ApplyInputField(vindexIF);
                            var leftButton = vindex.transform.Find("<").GetComponent<Button>();
                            var rightButton = vindex.transform.Find(">").GetComponent<Button>();
                            leftButton.transition = Selectable.Transition.ColorTint;
                            rightButton.transition = Selectable.Transition.ColorTint;
                            EditorThemeManager.ApplySelectable(leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.ApplySelectable(rightButton, ThemeGroup.Function_2, false);

                            TriggerHelper.IncreaseDecreaseButtonsInt(vindexIF, 1, 0, GameData.DefaultKeyframes[Parser.TryParse(modifier.commands[1], 0)].eventValues.Length - 1, vindex.transform);
                            TriggerHelper.AddEventTriggerParams(vindexIF.gameObject, TriggerHelper.ScrollDeltaInt(vindexIF, 1, 0, GameData.DefaultKeyframes[Parser.TryParse(modifier.commands[1], 0)].eventValues.Length - 1));

                            singleGenerator(cmd == "eventOffsetVariable" ? "Multiply Var" : "Value", 0, 0f);

                            if (cmd == "eventOffsetAnimate")
                            {
                                if (modifier.commands.Count < 6)
                                    modifier.commands.Add("False");

                                singleGenerator("Time", 3, 1f);

                                dropdownGenerator2("Easing", 4, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                                boolGenerator("Relative", 5, false);
                            }

                            break;
                        }
                    #endregion
                    #region Color
                    case "addColor":
                    case "addColorOther":
                    case "lerpColor":
                    case "lerpColorOther":
                        {
                            if (cmd.Contains("Other"))
                            {
                                stringGenerator("Object Group", 1);
                            }

                            colorGenerator("Color", !cmd.Contains("Other") ? 1 : 2);

                            singleGenerator("Hue", !cmd.Contains("Other") ? 2 : 3, 0f);
                            singleGenerator("Saturation", !cmd.Contains("Other") ? 3 : 4, 0f);
                            singleGenerator("Value", !cmd.Contains("Other") ? 4 : 5, 0f);

                            singleGenerator("Multiply", 0, 1f);

                            break;
                        }
                    case "addColorPlayerDistance":
                    case "lerpColorPlayerDistance":
                        {
                            colorGenerator("Color", 1);
                            singleGenerator("Multiply", 0, 1f);
                            singleGenerator("Offset", 2, 10f);

                            if (cmd == "lerpColorPlayerDistance")
                            {
                                singleGenerator("Opacity", 3, 1f);
                                singleGenerator("Hue", 4, 0f);
                                singleGenerator("Saturation", 5, 0f);
                                singleGenerator("Value", 6, 0f);
                            }

                            break;
                        }
                    #endregion
                    #region Signal
                    case "signalModifier":
                    case "mouseOverSignalModifier":
                        {
                            stringGenerator("Object Group", 1);
                            singleGenerator("Delay", 0, 0f);

                            break;
                        }
                    #endregion
                    #region Random
                    case "randomGreater":
                    case "randomLesser":
                    case "randomEquals":
                        {
                            integerGenerator("Minimum", 1, 0);
                            integerGenerator("Maximum", 2, 0);
                            integerGenerator("Value", 0, 0);

                            break;
                        }
                    case "setVariableRandom":
                        {
                            stringGenerator("Object Group", 0);
                            integerGenerator("Minimum Range", 1, 0);
                            integerGenerator("Maximum Range", 2, 0);

                            break;
                        }
                    #endregion
                    #region Editor
                    case "editorNotify":
                        {
                            stringGenerator("Text", 0);
                            singleGenerator("Time", 1, 0.5f);
                            dropdownGenerator("Notify Type", 2, new List<string> { "Info", "Success", "Error", "Warning" });

                            break;
                        }
                    #endregion
                    #region Player Move
                    case "playerMove":
                    case "playerMoveAll":
                    case "playerMoveX":
                    case "playerMoveXAll":
                    case "playerMoveY":
                    case "playerMoveYAll":
                    case "playerRotate":
                    case "playerRotateAll":
                        {
                            string[] vector = new string[2];

                            bool isBothAxis = cmd == "playerMove" || cmd == "playerMoveAll";
                            if (isBothAxis)
                            {
                                vector = modifier.value.Split(new char[] { ',' });
                            }

                            var xPosition = numberInput.Duplicate(layout, "X");
                            var xPositionLabel = xPosition.transform.Find("Text").GetComponent<Text>();
                            xPositionLabel.text = cmd.Contains("X") || isBothAxis || cmd.Contains("Rotate") ? "X" : "Y";

                            var xPositionIF = xPosition.transform.Find("Input").GetComponent<InputField>();
                            xPositionIF.onValueChanged.ClearAll();
                            xPositionIF.textComponent.alignment = TextAnchor.MiddleCenter;
                            xPositionIF.text = Parser.TryParse(isBothAxis ? vector[0] : modifier.value, 0.5f).ToString();
                            xPositionIF.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (float.TryParse(_val, out float result))
                                {
                                    modifier.value = isBothAxis ? $"{result},{layout.transform.Find("Y/Input").GetComponent<InputField>().text}" : result.ToString();
                                    modifier.active = false;
                                }
                            });

                            EditorThemeManager.ApplyLightText(xPositionLabel);
                            EditorThemeManager.ApplyInputField(xPositionIF);
                            var xPositionLeftButton = xPosition.transform.Find("<").GetComponent<Button>();
                            var xPositionRightButton = xPosition.transform.Find(">").GetComponent<Button>();
                            xPositionLeftButton.transition = Selectable.Transition.ColorTint;
                            xPositionRightButton.transition = Selectable.Transition.ColorTint;
                            EditorThemeManager.ApplySelectable(xPositionLeftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.ApplySelectable(xPositionRightButton, ThemeGroup.Function_2, false);

                            if (isBothAxis)
                            {
                                var yPosition = numberInput.Duplicate(layout, "Y");
                                var yPositionLabel = yPosition.transform.Find("Text").GetComponent<Text>();
                                yPositionLabel.text = "Y";

                                var yPositionIF = yPosition.transform.Find("Input").GetComponent<InputField>();
                                yPositionIF.onValueChanged.ClearAll();
                                yPositionIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                yPositionIF.text = Parser.TryParse(isBothAxis ? vector[0] : modifier.value, 0.5f).ToString();
                                yPositionIF.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (float.TryParse(_val, out float result))
                                    {
                                        modifier.value = $"{layout.transform.Find("X/Input").GetComponent<InputField>().text},{result}";
                                        modifier.active = false;
                                    }
                                });

                                EditorThemeManager.ApplyLightText(yPositionLabel);
                                EditorThemeManager.ApplyInputField(yPositionIF);
                                var yPositionLeftButton = yPosition.transform.Find("<").GetComponent<Button>();
                                var yPositionRightButton = yPosition.transform.Find(">").GetComponent<Button>();
                                yPositionLeftButton.transition = Selectable.Transition.ColorTint;
                                yPositionRightButton.transition = Selectable.Transition.ColorTint;
                                EditorThemeManager.ApplySelectable(yPositionLeftButton, ThemeGroup.Function_2, false);
                                EditorThemeManager.ApplySelectable(yPositionRightButton, ThemeGroup.Function_2, false);

                                TriggerHelper.IncreaseDecreaseButtons(yPositionIF, t: yPosition.transform);
                                TriggerHelper.AddEventTriggerParams(yPositionIF.gameObject,
                                    TriggerHelper.ScrollDelta(yPositionIF),
                                    TriggerHelper.ScrollDeltaVector2(xPositionIF, yPositionIF, 0.1f, 10f));

                            }
                            else
                            {
                                TriggerHelper.IncreaseDecreaseButtons(xPositionIF, t: xPosition.transform);
                                TriggerHelper.AddEventTrigger(xPositionIF.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(xPositionIF) });
                            }

                            var single = numberInput.Duplicate(layout, "Duration");
                            var singleText = single.transform.Find("Text").GetComponent<Text>();
                            singleText.text = "Duration";

                            var inputField = single.transform.Find("Input").GetComponent<InputField>();
                            inputField.onValueChanged.ClearAll();
                            inputField.textComponent.alignment = TextAnchor.MiddleCenter;
                            inputField.text = Parser.TryParse(modifier.commands[1], 1f).ToString();
                            inputField.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (float.TryParse(_val, out float result))
                                {
                                    modifier.commands[1] = Mathf.Clamp(result, 0f, 9999f).ToString();
                                    modifier.active = false;
                                }
                            });

                            EditorThemeManager.ApplyLightText(singleText);
                            EditorThemeManager.ApplyInputField(inputField);
                            var inputFieldLeftButton = single.transform.Find("<").GetComponent<Button>();
                            var inputFieldRightButton = single.transform.Find(">").GetComponent<Button>();
                            inputFieldLeftButton.transition = Selectable.Transition.ColorTint;
                            inputFieldRightButton.transition = Selectable.Transition.ColorTint;
                            EditorThemeManager.ApplySelectable(inputFieldLeftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.ApplySelectable(inputFieldRightButton, ThemeGroup.Function_2, false);

                            TriggerHelper.IncreaseDecreaseButtons(inputField, t: single.transform);
                            TriggerHelper.AddEventTrigger(inputField.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(inputField) });

                            dropdownGenerator2("Easing", 2, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                            if (modifier.commands.Count < 4)
                                modifier.commands.Add("False");

                            var global = booleanBar.Duplicate(layout, "Relative");
                            var relativeLabel = global.transform.Find("Text").GetComponent<Text>();
                            relativeLabel.text = "Relative";

                            var globalToggle = global.transform.Find("Toggle").GetComponent<Toggle>();
                            globalToggle.onValueChanged.ClearAll();
                            globalToggle.isOn = Parser.TryParse(modifier.commands[3], false);
                            globalToggle.onValueChanged.AddListener(delegate (bool _val)
                            {
                                modifier.commands[3] = _val.ToString();
                                modifier.active = false;
                            });

                            EditorThemeManager.ApplyLightText(relativeLabel);
                            EditorThemeManager.ApplyToggle(globalToggle);

                            break;
                        }
                    #endregion
                    #region Prefab
                    case "spawnPrefab":
                        {
                            var prefabIndex = numberInput.Duplicate(layout, "Index");
                            var prefabIndexLabel = prefabIndex.transform.Find("Text").GetComponent<Text>();
                            prefabIndexLabel.text = "Prefab Index";

                            var prefabIndexIF = prefabIndex.transform.Find("Input").GetComponent<InputField>();
                            prefabIndexIF.onValueChanged.ClearAll();
                            prefabIndexIF.textComponent.alignment = TextAnchor.MiddleCenter;
                            prefabIndexIF.text = Parser.TryParse(modifier.value, 0).ToString();
                            prefabIndexIF.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (int.TryParse(_val, out int result))
                                {
                                    modifier.value = Mathf.Clamp(result, 0, DataManager.inst.gameData.prefabObjects.Count - 1).ToString();
                                    modifier.active = false;
                                }
                            });

                            EditorThemeManager.ApplyLightText(prefabIndexLabel);
                            EditorThemeManager.ApplyInputField(prefabIndexIF);
                            var prefabIndexLeftButton = prefabIndex.transform.Find("<").GetComponent<Button>();
                            var prefabIndexRightButton = prefabIndex.transform.Find(">").GetComponent<Button>();
                            prefabIndexLeftButton.transition = Selectable.Transition.ColorTint;
                            prefabIndexRightButton.transition = Selectable.Transition.ColorTint;
                            EditorThemeManager.ApplySelectable(prefabIndexLeftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.ApplySelectable(prefabIndexRightButton, ThemeGroup.Function_2, false);

                            TriggerHelper.IncreaseDecreaseButtonsInt(prefabIndexIF, 1, 0, DataManager.inst.gameData.prefabObjects.Count - 1, prefabIndex.transform);
                            TriggerHelper.AddEventTrigger(prefabIndexIF.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(prefabIndexIF, 1, 0, DataManager.inst.gameData.prefabObjects.Count - 1) });

                            singleGenerator("Position X", 1, 0f);
                            singleGenerator("Position Y", 2, 0f);
                            singleGenerator("Scale X", 3, 0f);
                            singleGenerator("Scale Y", 4, 0f);
                            singleGenerator("Rotation", 5, 0f);

                            if (modifier.commands.Count < 8)
                            {
                                modifier.commands.Add("0");
                                modifier.commands.Add("0");
                                modifier.commands.Add("1");
                            }

                            integerGenerator("Repeat Count", 6, 0);
                            singleGenerator("Repeat Offset Time", 7, 0);
                            singleGenerator("Speed", 8, 0);

                            break;
                        }
                    #endregion
                    #region Clamp Variable
                    case "clampVariable":
                    case "clampVariableOther":
                        {
                            if (cmd == "clampVariableOther")
                                stringGenerator("Object Group", 0);

                            integerGenerator("Minimum", 1, 0);
                            integerGenerator("Maximum", 2, 0);

                            break;
                        }
                    #endregion
                    #region Animate
                    case "animateObject":
                    case "animateObjectOther":
                    case "animateSignal":
                    case "animateSignalOther":
                        {
                            singleGenerator("Time", 0, 1f);
                            dropdownGenerator("Type", 1, new List<string> { "Position", "Scale", "Rotation" });
                            singleGenerator("X", 2, 0f);
                            singleGenerator("Y", 3, 0f);
                            singleGenerator("Z", 4, 0f);
                            boolGenerator("Relative", 5, true);

                            dropdownGenerator("Easing", 6, EditorManager.inst.CurveOptions.Select(x => x.name).ToList());

                            if (cmd.Contains("Other"))
                            {
                                stringGenerator("Object Group", 7);
                            }

                            if (cmd.Contains("Signal"))
                            {
                                int m = 0;
                                if (cmd.Contains("Other"))
                                    m = 1;
                                stringGenerator("Signal Group", 7 + m);
                                singleGenerator("Signal Delay", 8 + m, 0f);
                                boolGenerator("Signal Deactivate", 9 + m, true);
                            }

                            break;
                        }
                    case "animateVariableOther":
                        {
                            stringGenerator("Object Group", 0);

                            dropdownGenerator("From Type", 1, new List<string> { "Position", "Scale", "Rotation" });
                            dropdownGenerator("From Axis", 2, new List<string> { "X", "Y", "Z" });

                            singleGenerator("Delay", 3, 0f);

                            singleGenerator("Multiply", 4, 1f);
                            singleGenerator("Offset", 5, 0f);
                            singleGenerator("Min", 6, -99999f);
                            singleGenerator("Max", 7, 99999f);

                            singleGenerator("Loop", 8, 99999f);

                            break;
                        }
                    case "copyAxis":
                    case "copyPlayerAxis":
                        {
                            if (modifier.commands.Count < 6)
                                modifier.commands.Add("0");

                            if (modifier.commands.Count < 7)
                                modifier.commands.Add("1");

                            if (modifier.commands.Count < 8)
                                modifier.commands.Add("0");

                            if (modifier.commands.Count < 9)
                                modifier.commands.Add("-99999");

                            if (modifier.commands.Count < 10)
                                modifier.commands.Add("99999");

                            if (cmd == "copyAxis")
                            {
                                if (modifier.commands.Count < 11)
                                    modifier.commands.Add("9999");

                                if (modifier.commands.Count < 12)
                                    modifier.commands.Add("False");

                                stringGenerator("Object Group", 0);
                            }

                            dropdownGenerator("From Type", 1, new List<string> { "Position", "Scale", "Rotation", "Color" });
                            dropdownGenerator("From Axis", 2, new List<string> { "X", "Y", "Z" });

                            dropdownGenerator("To Type", 3, new List<string> { "Position", "Scale", "Rotation", "Color" });
                            dropdownGenerator("To Axis (3D)", 4, new List<string> { "X", "Y", "Z" });

                            if (cmd == "copyAxis")
                                singleGenerator("Delay", 5, 0f);

                            singleGenerator("Multiply", 6, 1f);
                            singleGenerator("Offset", 7, 0f);
                            singleGenerator("Min", 8, -99999f);
                            singleGenerator("Max", 9, 99999f);

                            if (cmd == "copyAxis")
                            {
                                singleGenerator("Loop", 10, 99999f);
                                boolGenerator("Use Visual", 11, false);
                            }

                            break;
                        }
                    case "eventOffsetCopyAxis":
                        {
                            dropdownGenerator("From Type", 1, new List<string> { "Position", "Scale", "Rotation", "Color" });
                            dropdownGenerator("From Axis", 2, new List<string> { "X", "Y", "Z" });

                            dropdownGenerator("To Type", 3, RTEventEditor.EventTypes.ToList());
                            integerGenerator("To Axis", 4, 0);

                            singleGenerator("Delay", 5, 0f);

                            singleGenerator("Multiply", 6, 1f);
                            singleGenerator("Offset", 7, 0f);
                            singleGenerator("Min", 8, -99999f);
                            singleGenerator("Max", 9, 99999f);

                            singleGenerator("Loop", 10, 99999f);
                            boolGenerator("Use Visual", 11, false);

                            break;
                        }
                    case "axisEquals":
                    case "axisLesserEquals":
                    case "axisGreaterEquals":
                    case "axisLesser":
                    case "axisGreater":
                        {
                            if (modifier.commands.Count < 11)
                            {
                                modifier.commands.Add("9999");
                            }

                            stringGenerator("Object Group", 0);

                            dropdownGenerator("Type", 1, new List<string> { "Position", "Scale", "Rotation" });
                            dropdownGenerator("Axis", 2, new List<string> { "X", "Y", "Z" });

                            singleGenerator("Delay", 3, 0f);
                            singleGenerator("Multiply", 4, 1f);
                            singleGenerator("Offset", 5, 0f);
                            singleGenerator("Min", 6, -99999f);
                            singleGenerator("Max", 7, 99999f);
                            singleGenerator("Equals", 8, 1f);
                            boolGenerator("Use Visual", 9, false);
                            singleGenerator("Loop", 10, 99999f);

                            break;
                        }
                    #endregion
                    #region Gravity
                    case "gravity":
                    case "gravityOther":
                        {
                            if (cmd == "gravityOther")
                                stringGenerator("Object Group", 0);

                            singleGenerator("X", 1, -1f);
                            singleGenerator("Y", 2, 0f);

                            break;
                        }
                    #endregion
                    #region Enable / Disable
                    case "enableObjectTree":
                    case "disableObjectTree":
                        {
                            if (modifier.value == "0")
                                modifier.value = "False";

                            boolGenerator("Use Self", 0, true);

                            break;
                        }
                    case "enableObjectTreeOther":
                    case "disableObjectTreeOther":
                        {
                            stringGenerator("Object Group", 1);
                            boolGenerator("Use Self", 0, true);

                            break;
                        }
                    case "enableObjectOther":
                    case "disableObjectOther":
                        {
                            stringGenerator("Object Group", 0);

                            break;
                        }
                    #endregion
                    #region Level Rank
                    case "levelRankEquals":
                    case "levelRankLesserEquals":
                    case "levelRankGreaterEquals":
                    case "levelRankLesser":
                    case "levelRankGreater":
                        {
                            dropdownGenerator("Rank", 0, DataManager.inst.levelRanks.Select(x => x.name).ToList());

                            break;
                        }
                    #endregion
                    #region Discord
                    case "setDiscordStatus":
                        {
                            stringGenerator("State", 0);
                            stringGenerator("Details", 1);
                            dropdownGenerator("Sub Icon", 2, new List<string> { "Arcade", "Editor", "Play", "Menu" });
                            dropdownGenerator("Icon", 3, new List<string> { "PA Logo White", "PA Logo Black" });

                            break;
                        }
                    #endregion
                    case "gameMode":
                        {
                            dropdownGenerator("Mode", 0, new List<string> { "Regular", "Platformer" });

                            break;
                        }
                    case "setCollision":
                    case "setCollisionOther":
                        {
                            boolGenerator("On", 0, false);

                            if (cmd == "setCollisionOther")
                                stringGenerator("Object Group", 1);

                            break;
                        }
                    case "playerVelocityAll":
                        {
                            singleGenerator("X", 1, 0f);
                            singleGenerator("Y", 2, 0f);

                            break;
                        }
                    case "playerVelocityXAll":
                    case "playerVelocityYAll":
                        {
                            singleGenerator(cmd == "playerVelocityXAll" ? "X" : "Y", 0, 0f);

                            break;
                        }
                    case "legacyTail":
                        {
                            singleGenerator("Total Time", 0, 200f);

                            var path = stringInput.Duplicate(layout, "usage");
                            path.transform.localScale = Vector3.one;
                            var labelText = path.transform.Find("Text").GetComponent<Text>();
                            labelText.text = "Update Object to Update Modifier";
                            path.transform.Find("Text").AsRT().sizeDelta = new Vector2(350f, 32f);
                            Destroy(path.transform.Find("Input").gameObject);

                            for (int i = 1; i < modifier.commands.Count; i += 3)
                            {
                                int groupIndex = i;
                                var label = stringInput.Duplicate(layout, "group label");
                                label.transform.localScale = Vector3.one;
                                var groupLabel = label.transform.Find("Text").GetComponent<Text>();
                                groupLabel.text = $" Tail Group {(i + 2) / 3}";
                                label.transform.Find("Text").AsRT().sizeDelta = new Vector2(268f, 32f);
                                Destroy(label.transform.Find("Input").gameObject);

                                var deleteGroup = gameObject.transform.Find("Label/Delete").gameObject.Duplicate(label.transform, "delete");
                                var deleteGroupButton = deleteGroup.GetComponent<DeleteButtonStorage>();
                                deleteGroupButton.button.onClick.ClearAll();
                                deleteGroupButton.button.onClick.AddListener(delegate ()
                                {
                                    for (int j = 0; j < 3; j++)
                                    {
                                        modifier.commands.RemoveAt(groupIndex);
                                    }

                                    Updater.UpdateProcessor(beatmapObject);
                                    StartCoroutine(RenderModifiers(beatmapObject));
                                });

                                EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete, deleteGroup, new List<Component>
                                    {
                                        deleteGroupButton.button.image,
                                    }, true, 1, SpriteManager.RoundedSide.W));

                                EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete_Text, deleteGroupButton.image.gameObject, new List<Component>
                                    {
                                        deleteGroupButton.image,
                                    }));

                                stringGenerator("Object Group", i);
                                singleGenerator("Distance", i + 1, 2f);
                                singleGenerator("Time", i + 2, 12f);
                            }

                            var baseAdd = new GameObject("add");
                            baseAdd.transform.SetParent(layout);
                            baseAdd.transform.localScale = Vector3.one;

                            var baseAddRT = baseAdd.AddComponent<RectTransform>();
                            baseAddRT.sizeDelta = new Vector2(0f, 32f);

                            var add = PrefabEditor.inst.CreatePrefab.Duplicate(baseAddRT, "add");
                            var addText = add.transform.GetChild(0).GetComponent<Text>();
                            addText.text = "Add Group";
                            add.transform.AsRT().anchoredPosition = new Vector2(-6f, 0f);
                            add.transform.AsRT().anchorMax = new Vector2(0.5f, 0.5f);
                            add.transform.AsRT().anchorMin = new Vector2(0.5f, 0.5f);
                            add.transform.AsRT().sizeDelta = new Vector2(300f, 32f);

                            var addButton = add.GetComponent<Button>();
                            addButton.onClick.ClearAll();
                            addButton.onClick.AddListener(delegate ()
                            {
                                var lastIndex = modifier.commands.Count - 1;
                                var length = "2";
                                var time = "12";
                                if (lastIndex - 1 > 2)
                                {
                                    length = modifier.commands[lastIndex - 1];
                                    time = modifier.commands[lastIndex];
                                }

                                modifier.commands.Add("Object Group");
                                modifier.commands.Add(length);
                                modifier.commands.Add(time);

                                Updater.UpdateProcessor(beatmapObject);
                                StartCoroutine(RenderModifiers(beatmapObject));
                            });

                            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Add, add, new List<Component>
                                {
                                    addButton.image,
                                }, true, 1, SpriteManager.RoundedSide.W));

                            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Add_Text, addText.gameObject, new List<Component>
                                {
                                    addText,
                                }));

                            break;
                        }
                    case "setMousePosition":
                        {
                            integerGenerator("Position X", 1, 0);
                            integerGenerator("Position Y", 1, 0);

                            break;
                        }
                }

                /* List of modifiers that have no values:
                 * - playerKill
                 * - playerKillAll
                 * - playerCollide
                 * - playerMoving
                 * - playerBoosting
                 * - playerAlive
                 * - playerBoost
                 * - playerBoostAll
                 * - playerDisableBoost
                 * - onPlayerHit
                 * - inZenMode
                 * - inNormal
                 * - in1Life
                 * - inNoHit
                 * - inEditor
                 * - showMouse
                 * - hideMouse
                 * - mouseOver
                 * - disableObject
                 * - disableObjectTree
                 * - bulletCollide
                 * - updateObjects
                 * - requireSignal
                 */

                num++;
            }

            // Add Modifier
            {
                var gameObject = modifierAddPrefab.Duplicate(content, "add modifier");

                var button = gameObject.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(delegate ()
                {
                    EditorManager.inst.ShowDialog("Default Modifiers Popup");
                    RefreshDefaultModifiersList(beatmapObject);
                });

                EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(gameObject.transform.GetChild(0).GetComponent<Text>());
            }

            // Paste Modifier
            if (copiedModifier != null)
            {
                var gameObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(content, "paste modifier");
                gameObject.transform.AsRT().sizeDelta = new Vector2(350f, 32f);
                var buttonStorage = gameObject.GetComponent<FunctionButtonStorage>();
                buttonStorage.text.text = "Paste";
                buttonStorage.button.onClick.ClearAll();
                buttonStorage.button.onClick.AddListener(delegate ()
                {
                    beatmapObject.modifiers.Add(Modifier<BeatmapObject>.DeepCopy(copiedModifier, beatmapObject));
                    StartCoroutine(RenderModifiers(beatmapObject));
                    EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
                });

                EditorThemeManager.ApplyGraphic(buttonStorage.button.image, ThemeGroup.Paste, true);
                EditorThemeManager.ApplyGraphic(buttonStorage.text, ThemeGroup.Paste_Text);
            }

            yield break;
        }

        public void SetObjectColors(Toggle[] toggles, int index, int i, Modifier<BeatmapObject> modifier)
        {
            if (index == 0)
                modifier.value = i.ToString();
            else
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

        #region Default Modifiers

        public void CreateDefaultModifiersList()
        {
            var defaultModifiersList = RTEditor.inst.GeneratePopup("Default Modifiers Popup", "Choose a modifer to add", Vector2.zero, new Vector2(600f, 400f), delegate (string _val)
            {
                searchTerm = _val;
                if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                    RefreshDefaultModifiersList(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
            }, placeholderText: "Search for default Modifier...");
        }

        public string searchTerm;
        public void RefreshDefaultModifiersList(BeatmapObject beatmapObject)
        {
            defaultModifiers = ModifiersManager.defaultBeatmapObjectModifiers;

            var dialog = EditorManager.inst.GetDialog("Default Modifiers Popup").Dialog.gameObject;

            var contentM = dialog.transform.Find("mask/content");
            LSHelpers.DeleteChildren(contentM);

            for (int i = 0; i < defaultModifiers.Count; i++)
            {
                if (string.IsNullOrEmpty(searchTerm) || defaultModifiers[i].commands[0].ToLower().Contains(searchTerm.ToLower()) ||
                    searchTerm.ToLower() == "action" && defaultModifiers[i].type == ModifierBase.Type.Action || searchTerm.ToLower() == "trigger" && defaultModifiers[i].type == ModifierBase.Type.Trigger)
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
                        if (cmd.Contains("Text") && !cmd.Contains("Other") && beatmapObject.shape != 4)
                        {
                            EditorManager.inst.DisplayNotification("Cannot add modifier to object because the object needs to be a Text Object.", 2f, EditorManager.NotificationType.Error);
                            return;
                        }

                        if (cmd.Contains("Image") && !cmd.Contains("Other") && beatmapObject.shape != 6)
                        {
                            EditorManager.inst.DisplayNotification("Cannot add modifier to object because the object needs to be an Image Object.", 2f, EditorManager.NotificationType.Error);
                            return;
                        }

                        var modifier = Modifier<BeatmapObject>.DeepCopy(defaultModifiers[tmpIndex], beatmapObject);
                        beatmapObject.modifiers.Add(modifier);
                        RTEditor.inst.StartCoroutine(ObjectEditor.RefreshObjectGUI(beatmapObject));
                        EditorManager.inst.HideDialog("Default Modifiers Popup");
                    });

                    EditorThemeManager.ApplyLightText(modifierName);
                    EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                }
            }
        }

        public List<Modifier<BeatmapObject>> defaultModifiers = new List<Modifier<BeatmapObject>>();

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
            textRT.sizeDelta = new Vector2(296f, 32f);

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
