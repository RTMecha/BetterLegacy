using BetterLegacy.Editor.Components;
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
using BetterLegacy.Core.Components;
using BetterLegacy.Editor.Data;

namespace BetterLegacy.Editor.Managers
{
    public class ObjectModifiersEditor : MonoBehaviour
    {
        public static ObjectModifiersEditor inst;

        public Transform content;
        public Transform scrollView;

        public bool showModifiers;

        public GameObject modifierCardPrefab;
        public GameObject modifierAddPrefab;

        public static void Init() => Creator.NewGameObject(nameof(ObjectModifiersEditor), EditorManager.inst.transform.parent).AddComponent<ObjectModifiersEditor>();

        void Awake()
        {
            inst = this;

            CreateModifiersOnAwake();
            RTEditor.inst.GeneratePopup("Default Modifiers Popup", "Choose a modifer to add", Vector2.zero, new Vector2(600f, 400f), _val =>
            {
                searchTerm = _val;
                if (ObjectEditor.inst.CurrentSelection.isBeatmapObject)
                    RefreshDefaultModifiersList(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>(), addIndex);
            }, placeholderText: "Search for default Modifier...");
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
                if (RTEditor.ShowModdedUI && ObjectEditor.inst.SelectedObjectCount == 1 && ObjectEditor.inst.CurrentSelection.isBeatmapObject)
                    intVariable.text = $"Integer Variable: [ {ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>().integerVariable} ]";
            }
            catch
            {

            }
        }

        public Text modifiersLabel;

        public Text intVariable;

        public Toggle activeToggle;

        public Toggle ignoreToggle;

        public Toggle orderToggle;

        public bool renderingModifiers;

        public void CreateModifiersOnAwake()
        {
            // Label
            {
                var label = EditorPrefabHolder.Instance.Labels.Duplicate(ObjEditor.inst.ObjectView.transform, "label");

                modifiersLabel = label.transform.GetChild(0).GetComponent<Text>();
                modifiersLabel.text = "Modifiers";
                EditorThemeManager.AddLightText(modifiersLabel);
            }
            
            // Integer variable
            {
                var label = EditorPrefabHolder.Instance.Labels.Duplicate(ObjEditor.inst.ObjectView.transform, "int_variable");

                intVariable = label.transform.GetChild(0).GetComponent<Text>();
                intVariable.text = "Integer Variable: [ null ]";
                intVariable.fontSize = 18;
                EditorThemeManager.AddLightText(intVariable);
                label.AddComponent<Image>().color = LSColors.transparent;
                TooltipHelper.AssignTooltip(label, "Modifiers Integer Variable");
            }

            // Ignored Lifespan
            {
                var ignoreLifespan = EditorPrefabHolder.Instance.ToggleButton.Duplicate(ObjEditor.inst.ObjectView.transform, "ignore life");
                var ignoreLifespanToggleButton = ignoreLifespan.GetComponent<ToggleButtonStorage>();
                ignoreLifespanToggleButton.label.text = "Ignore Lifespan";

                ignoreToggle = ignoreLifespanToggleButton.toggle;

                EditorThemeManager.AddToggle(ignoreToggle, graphic: ignoreLifespanToggleButton.label);
                TooltipHelper.AssignTooltip(ignoreLifespan, "Modifiers Ignore Lifespan");
            }
            
            // Order Modifiers
            {
                var orderMatters = EditorPrefabHolder.Instance.ToggleButton.Duplicate(ObjEditor.inst.ObjectView.transform, "order modifiers");
                var orderMattersToggleButton = orderMatters.GetComponent<ToggleButtonStorage>();
                orderMattersToggleButton.label.text = "Order Matters";

                orderToggle = orderMattersToggleButton.toggle;

                EditorThemeManager.AddToggle(orderToggle, graphic: orderMattersToggleButton.label);
                TooltipHelper.AssignTooltip(orderMatters, "Modifiers Order Matters");
            }

            // Active
            {
                var showModifiers = EditorPrefabHolder.Instance.ToggleButton.Duplicate(ObjEditor.inst.ObjectView.transform, "active");
                var showModifiersToggleButton = showModifiers.GetComponent<ToggleButtonStorage>();
                showModifiersToggleButton.label.text = "Show Modifiers";

                activeToggle = showModifiersToggleButton.toggle;
                activeToggle.onValueChanged.ClearAll();
                activeToggle.isOn = this.showModifiers;
                activeToggle.onValueChanged.AddListener(_val =>
                {
                    this.showModifiers = _val;
                    scrollView.gameObject.SetActive(this.showModifiers);
                    if (ObjectEditor.inst.CurrentSelection.isBeatmapObject)
                        RTEditor.inst.StartCoroutine(ObjectEditor.inst.RefreshObjectGUI(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>()));
                });

                EditorThemeManager.AddToggle(activeToggle, graphic: showModifiersToggleButton.label);
                TooltipHelper.AssignTooltip(showModifiers, "Show Modifiers");
            }

            var scrollObj = EditorPrefabHolder.Instance.ScrollView.Duplicate(ObjEditor.inst.ObjectView.transform, "Modifiers Scroll View");

            scrollView = scrollObj.transform;

            scrollView.localScale = Vector3.one;

            content = scrollView.Find("Viewport/Content");

            scrollView.gameObject.SetActive(showModifiers);

            #region Prefabs

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
            mcpTextText.horizontalOverflow = HorizontalWrapMode.Overflow;
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
            var deleteLayoutElement = delete.GetComponent<LayoutElement>() ?? delete.AddComponent<LayoutElement>();
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

            UIManager.SetRectTransform(notifierImage.rectTransform, new Vector2(44f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(8f, 32f));

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

            #endregion
        }

        public static Modifier<BeatmapObject> copiedModifier;
        public IEnumerator RenderModifiers(BeatmapObject beatmapObject)
        {
            ignoreToggle.onValueChanged.ClearAll();
            ignoreToggle.isOn = beatmapObject.ignoreLifespan;
            ignoreToggle.onValueChanged.AddListener(_val => beatmapObject.ignoreLifespan = _val);

            orderToggle.onValueChanged.ClearAll();
            orderToggle.isOn = beatmapObject.orderModifiers;
            orderToggle.onValueChanged.AddListener(_val => beatmapObject.orderModifiers = _val);

            if (!showModifiers)
                yield break;

            renderingModifiers = true;

            LSHelpers.DeleteChildren(content);

            //content.parent.parent.AsRT().sizeDelta = new Vector2(351f, 300f * Mathf.Clamp(beatmapObject.modifiers.Count, 1, 5));
            content.parent.parent.AsRT().sizeDelta = new Vector2(351f, 500f);

            int num = 0;
            foreach (var modifier in beatmapObject.modifiers)
            {
                int index = num;
                var name = modifier.Name;

                var gameObject = modifierCardPrefab.Duplicate(content, name);

                TooltipHelper.AssignTooltip(gameObject, $"Object Modifier - {(name + " (" + modifier.type.ToString() + ")")}");

                EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.List_Button_1_Normal, true);

                gameObject.transform.localScale = Vector3.one;
                var modifierTitle = gameObject.transform.Find("Label/Text").GetComponent<Text>();
                modifierTitle.text = name;
                EditorThemeManager.ApplyLightText(modifierTitle);

                var collapse = gameObject.transform.Find("Label/Collapse").GetComponent<Toggle>();
                collapse.onValueChanged.ClearAll();
                collapse.isOn = modifier.collapse;
                collapse.onValueChanged.AddListener(_val =>
                {
                    modifier.collapse = _val;
                    StartCoroutine(RenderModifiers(beatmapObject));
                });
                TooltipHelper.AssignTooltip(collapse.gameObject, "Collapse Modifier");

                EditorThemeManager.ApplyToggle(collapse, ThemeGroup.List_Button_1_Normal);

                for (int i = 0; i < collapse.transform.Find("dots").childCount; i++)
                    EditorThemeManager.ApplyGraphic(collapse.transform.Find("dots").GetChild(i).GetComponent<Image>(), ThemeGroup.Dark_Text);

                var delete = gameObject.transform.Find("Label/Delete").GetComponent<DeleteButtonStorage>();
                delete.button.onClick.ClearAll();
                delete.button.onClick.AddListener(() =>
                {
                    beatmapObject.modifiers.RemoveAt(index);
                    beatmapObject.reactivePositionOffset = Vector3.zero;
                    beatmapObject.reactiveScaleOffset = Vector3.zero;
                    beatmapObject.reactiveRotationOffset = 0f;
                    Updater.UpdateObject(beatmapObject);
                    StartCoroutine(RenderModifiers(beatmapObject));
                });

                EditorThemeManager.ApplyGraphic(delete.button.image, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(delete.image, ThemeGroup.Delete_Text);

                var copy = gameObject.transform.Find("Label/Copy").GetComponent<DeleteButtonStorage>();
                copy.button.onClick.ClearAll();
                copy.button.onClick.AddListener(() =>
                {
                    copiedModifier = Modifier<BeatmapObject>.DeepCopy(modifier, beatmapObject);
                    PasteGenerator(beatmapObject);
                    EditorManager.inst.DisplayNotification("Copied Modifier!", 1.5f, EditorManager.NotificationType.Success);
                });

                EditorThemeManager.ApplyGraphic(copy.button.image, ThemeGroup.Copy, true);
                EditorThemeManager.ApplyGraphic(copy.image, ThemeGroup.Copy_Text);

                var notifier = gameObject.AddComponent<ModifierActiveNotifier>();
                notifier.modifierBase = modifier;
                notifier.notifier = gameObject.transform.Find("Label/Notifier").gameObject;
                EditorThemeManager.ApplyGraphic(notifier.notifier.GetComponent<Image>(), ThemeGroup.Warning_Confirm, true);

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
                            RefreshDefaultModifiersList(beatmapObject);
                        }),
                        new ButtonFunction("Add Above", () =>
                        {
                            EditorManager.inst.ShowDialog("Default Modifiers Popup");
                            RefreshDefaultModifiersList(beatmapObject, index);
                        }),
                        new ButtonFunction("Add Below", () =>
                        {
                            EditorManager.inst.ShowDialog("Default Modifiers Popup");
                            RefreshDefaultModifiersList(beatmapObject, index + 1);
                        }),
                        new ButtonFunction("Delete", () =>
                        {
                            beatmapObject.modifiers.RemoveAt(index);
                            beatmapObject.reactivePositionOffset = Vector3.zero;
                            beatmapObject.reactiveScaleOffset = Vector3.zero;
                            beatmapObject.reactiveRotationOffset = 0f;
                            Updater.UpdateObject(beatmapObject);
                            StartCoroutine(RenderModifiers(beatmapObject));
                        }),
                        new ButtonFunction(true),
                        new ButtonFunction("Copy", () =>
                        {
                            copiedModifier = Modifier<BeatmapObject>.DeepCopy(modifier, beatmapObject);
                            PasteGenerator(beatmapObject);
                            EditorManager.inst.DisplayNotification("Copied Modifier!", 1.5f, EditorManager.NotificationType.Success);
                        }),
                        new ButtonFunction("Paste", () =>
                        {
                            if (copiedModifier == null)
                                return;

                            beatmapObject.modifiers.Add(Modifier<BeatmapObject>.DeepCopy(copiedModifier, beatmapObject));
                            StartCoroutine(RenderModifiers(beatmapObject));
                            EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
                        }),
                        new ButtonFunction("Paste Above", () =>
                        {
                            if (copiedModifier == null)
                                return;

                            beatmapObject.modifiers.Insert(index, Modifier<BeatmapObject>.DeepCopy(copiedModifier, beatmapObject));
                            StartCoroutine(RenderModifiers(beatmapObject));
                            EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
                        }),
                        new ButtonFunction("Paste Below", () =>
                        {
                            if (copiedModifier == null)
                                return;

                            beatmapObject.modifiers.Insert(index + 1, Modifier<BeatmapObject>.DeepCopy(copiedModifier, beatmapObject));
                            StartCoroutine(RenderModifiers(beatmapObject));
                            EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
                        }),
                        new ButtonFunction(true),
                        new ButtonFunction("Sort Modifiers", () =>
                        {
                            beatmapObject.modifiers = beatmapObject.modifiers.OrderBy(x => x.type == ModifierBase.Type.Action).ToList();
                            StartCoroutine(RenderModifiers(beatmapObject));
                        }),
                        new ButtonFunction("Move Up", () =>
                        {
                            if (index <= 0)
                            {
                                EditorManager.inst.DisplayNotification("Could not move modifier up since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                                return;
                            }

                            beatmapObject.modifiers.Move(index, index - 1);
                            StartCoroutine(RenderModifiers(beatmapObject));
                        }),
                        new ButtonFunction("Move Down", () =>
                        {
                            if (index >= beatmapObject.modifiers.Count - 1)
                            {
                                EditorManager.inst.DisplayNotification("Could not move modifier up since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                                return;
                            }

                            beatmapObject.modifiers.Move(index, index + 1);
                            StartCoroutine(RenderModifiers(beatmapObject));
                        }),
                        new ButtonFunction("Move to Start", () =>
                        {
                            beatmapObject.modifiers.Move(index, 0);
                            StartCoroutine(RenderModifiers(beatmapObject));
                        }),
                        new ButtonFunction("Move to End", () =>
                        {
                            beatmapObject.modifiers.Move(index, beatmapObject.modifiers.Count - 1);
                            StartCoroutine(RenderModifiers(beatmapObject));
                        }),
                        new ButtonFunction(true),
                        new ButtonFunction("Update Modifier", () =>
                        {
                            modifier.active = false;
                            modifier.Inactive?.Invoke(modifier);
                        }),
                        new ButtonFunction(true),
                        new ButtonFunction("Collapse", () =>
                        {
                            modifier.collapse = true;
                            StartCoroutine(RenderModifiers(beatmapObject));
                        }),
                        new ButtonFunction("Unollapse", () =>
                        {
                            modifier.collapse = false;
                            StartCoroutine(RenderModifiers(beatmapObject));
                        }),
                        new ButtonFunction("Collapse All", () =>
                        {
                            foreach (var mod in beatmapObject.modifiers)
                                mod.collapse = true;
                            StartCoroutine(RenderModifiers(beatmapObject));
                        }),
                        new ButtonFunction("Uncollapse All", () =>
                        {
                            foreach (var mod in beatmapObject.modifiers)
                                mod.collapse = false;
                            StartCoroutine(RenderModifiers(beatmapObject));
                        })
                    };
                    if (ModCompatibility.UnityExplorerInstalled)
                        buttonFunctions.Add(new ButtonFunction("Inspect", () => ModCompatibility.Inspect(modifier)));

                    RTEditor.inst.ShowContextMenu(buttonFunctions);
                };

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
                EditorThemeManager.ApplyLightText(constantText);

                var toggle = constant.transform.Find("Toggle").GetComponent<Toggle>();
                toggle.onValueChanged.ClearAll();
                toggle.isOn = modifier.constant;
                toggle.onValueChanged.AddListener(_val =>
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
                    notToggle.onValueChanged.AddListener(_val =>
                    {
                        modifier.not = _val;
                        modifier.active = false;
                    });

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

                    EditorThemeManager.ApplyLightText(elseIfText);
                    EditorThemeManager.ApplyToggle(elseIfToggle);
                }

                if (!modifier.verified)
                {
                    modifier.verified = true;
                    if (!name.Contains("DEVONLY"))
                        modifier.VerifyModifier(ModifiersManager.defaultBeatmapObjectModifiers);
                }

                if (!name.Contains("DEVONLY") && !modifier.IsValid(ModifiersManager.defaultBeatmapObjectModifiers))
                {
                    EditorManager.inst.DisplayNotification("Modifier does not have a command name and is lacking values.", 2f, EditorManager.NotificationType.Error);
                    num++;
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
                    case "playerSpeed":
                    case "setAudioTransition":
                        {
                            if (cmd.Contains("Other"))
                                PrefabGroupOnly(modifier, layout);

                            SingleGenerator(modifier, layout, "Value", 0, 1f);

                            if (cmd == "setAlphaOther")
                            {
                                var str = StringGenerator(modifier, layout, "Object Group", 1);
                                EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            }

                            if (cmd == "blackHole")
                                BoolGenerator(modifier, layout, "Use Opacity", 1, false);

                            break;
                        }

                    case "musicTimeGreater":
                    case "musicTimeLesser":
                        {
                            SingleGenerator(modifier, layout, "Time", 0, 0f);
                            BoolGenerator(modifier, layout, "Offset From Start Time", 1, false);

                            break;
                        }
                    #endregion

                    #region Sound

                    case "playSound":
                        {
                            var str = StringGenerator(modifier, layout, "Path", 0);
                            var search = str.transform.Find("Input").gameObject.AddComponent<ContextClickable>();
                            search.onClick = pointerEventData =>
                            {
                                if (pointerEventData.button != PointerEventData.InputButton.Right)
                                    return;

                                RTEditor.inst.ShowContextMenu(
                                    new ButtonFunction("Use Local Browser", () =>
                                    {
                                        var isGlobal = modifier.commands.Count > 1 && Parser.TryParse(modifier.commands[1], false);
                                        var directory = isGlobal && RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH) ?
                                                        RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH : RTFile.RemoveEndSlash(RTFile.BasePath);

                                        if (isGlobal && !RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH))
                                        {
                                            EditorManager.inst.DisplayNotification("soundlibrary folder does not exist! If you want to have audio take from a global folder, make sure you create a soundlibrary folder inside your beatmaps folder and put your sounds in there.", 12f, EditorManager.NotificationType.Error);
                                            return;
                                        }

                                        var result = Crosstales.FB.FileBrowser.OpenSingleFile("Select a sound to use!", directory, FileFormat.OGG.ToName(), FileFormat.WAV.ToName(), FileFormat.MP3.ToName());
                                        if (string.IsNullOrEmpty(result))
                                            return;

                                        var global = Parser.TryParse(modifier.commands[1], false);

                                        result = RTFile.ReplaceSlash(result);
                                        if (result.Contains(global ? RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH + "/" : RTFile.ReplaceSlash(RTFile.AppendEndSlash(RTFile.BasePath))))
                                        {
                                            str.transform.Find("Input").GetComponent<InputField>().text =
                                                result.Replace(global ? RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH + "/" : RTFile.ReplaceSlash(RTFile.AppendEndSlash(RTFile.BasePath)), "");
                                            EditorManager.inst.HideDialog("Browser Popup");
                                            return;
                                        }

                                        EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                    }),
                                    new ButtonFunction("Use In-game Browser", () =>
                                    {
                                        EditorManager.inst.ShowDialog("Browser Popup");

                                        var isGlobal = modifier.commands.Count > 1 && Parser.TryParse(modifier.commands[1], false);
                                        var directory = isGlobal && RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH) ?
                                                        RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH : RTFile.RemoveEndSlash(RTFile.BasePath);

                                        if (isGlobal && !RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH))
                                        {
                                            EditorManager.inst.DisplayNotification("soundlibrary folder does not exist! If you want to have audio take from a global folder, make sure you create a soundlibrary folder inside your beatmaps folder and put your sounds in there.", 12f, EditorManager.NotificationType.Error);
                                            return;
                                        }

                                        RTFileBrowser.inst.UpdateBrowserFile(directory, RTFile.AudioDotFormats, onSelectFile: _val =>
                                        {
                                            var global = Parser.TryParse(modifier.commands[1], false);
                                            _val = RTFile.ReplaceSlash(_val);
                                            if (_val.Contains(global ? RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH : RTFile.ReplaceSlash(RTFile.AppendEndSlash(RTFile.BasePath))))
                                            {
                                                str.transform.Find("Input").GetComponent<InputField>().text = _val.Replace(global ? RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH + "/" : RTFile.ReplaceSlash(RTFile.AppendEndSlash(RTFile.BasePath)), "");
                                                EditorManager.inst.HideDialog("Browser Popup");
                                                return;
                                            }

                                            EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                        });
                                    })
                                    );
                            };
                            BoolGenerator(modifier, layout, "Global", 1, false);
                            SingleGenerator(modifier, layout, "Pitch", 2, 1f);
                            SingleGenerator(modifier, layout, "Volume", 3, 1f);
                            BoolGenerator(modifier, layout, "Loop", 4, false);

                            break;
                        }
                    case "playSoundOnline":
                        {
                            StringGenerator(modifier, layout, "URL", 0);
                            SingleGenerator(modifier, layout, "Pitch", 1, 1f);
                            SingleGenerator(modifier, layout, "Volume", 2, 1f);
                            BoolGenerator(modifier, layout, "Loop", 3, false);

                            break;
                        }
                    case "playDefaultSound":
                        {
                            var dd = dropdownBar.Duplicate(layout, "Sound");
                            dd.transform.localScale = Vector3.one;
                            var labelText = dd.transform.Find("Text").GetComponent<Text>();
                            labelText.text = "Sound";

                            Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
                            Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

                            var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                            d.onValueChanged.ClearAll();
                            d.options.Clear();
                            var sounds = Enum.GetNames(typeof(DefaultSounds));
                            d.options = CoreHelper.StringToOptionData(sounds);

                            int soundIndex = -1;
                            for (int i = 0; i < sounds.Length; i++)
                            {
                                if (sounds[i] == modifier.value)
                                {
                                    soundIndex = i;
                                    break;
                                }
                            }

                            if (soundIndex >= 0)
                                d.value = soundIndex;

                            d.onValueChanged.AddListener(_val =>
                            {
                                modifier.value = sounds[_val];
                                modifier.active = false;
                            });

                            EditorThemeManager.ApplyLightText(labelText);
                            EditorThemeManager.ApplyDropdown(d);

                            SingleGenerator(modifier, layout, "Pitch", 1, 1f);
                            SingleGenerator(modifier, layout, "Volume", 2, 1f);
                            BoolGenerator(modifier, layout, "Loop", 3, false);

                            break;
                        }
                    case "audioSource":
                        {
                            var str = StringGenerator(modifier, layout, "Path", 0);
                            var search = str.transform.Find("Input").gameObject.AddComponent<Clickable>();
                            search.onClick = pointerEventData =>
                            {
                                if (pointerEventData.button != PointerEventData.InputButton.Right)
                                    return;
                                RTEditor.inst.ShowContextMenu(
                                    new ButtonFunction("Use Local Browser", () =>
                                    {
                                        var isGlobal = modifier.commands.Count > 1 && Parser.TryParse(modifier.commands[1], false);
                                        var directory = isGlobal && RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH) ?
                                                        RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH : RTFile.RemoveEndSlash(RTFile.BasePath);

                                        if (isGlobal && !RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH))
                                        {
                                            EditorManager.inst.DisplayNotification("soundlibrary folder does not exist! If you want to have audio take from a global folder, make sure you create a soundlibrary folder inside your beatmaps folder and put your sounds in there.", 12f, EditorManager.NotificationType.Error);
                                            return;
                                        }

                                        var result = Crosstales.FB.FileBrowser.OpenSingleFile("Select a sound to use!", directory, FileFormat.OGG.ToName(), FileFormat.WAV.ToName(), FileFormat.MP3.ToName());
                                        if (string.IsNullOrEmpty(result))
                                            return;

                                        var global = Parser.TryParse(modifier.commands[1], false);
                                        result = RTFile.ReplaceSlash(result);
                                        if (result.Contains(global ? RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH + "/" : RTFile.ReplaceSlash(RTFile.AppendEndSlash(RTFile.BasePath))))
                                        {
                                            str.transform.Find("Input").GetComponent<InputField>().text = result.Replace(global ? RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH + "/" : RTFile.ReplaceSlash(RTFile.AppendEndSlash(RTFile.BasePath)), "");
                                            EditorManager.inst.HideDialog("Browser Popup");
                                            return;
                                        }

                                        EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                    }),
                                    new ButtonFunction("Use In-game Browser", () =>
                                    {
                                        EditorManager.inst.ShowDialog("Browser Popup");

                                        var isGlobal = modifier.commands.Count > 1 && Parser.TryParse(modifier.commands[1], false);
                                        var directory = isGlobal && RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH) ?
                                                        RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH : RTFile.RemoveEndSlash(RTFile.BasePath);

                                        if (isGlobal && !RTFile.DirectoryExists(RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH))
                                        {
                                            EditorManager.inst.DisplayNotification("soundlibrary folder does not exist! If you want to have audio take from a global folder, make sure you create a soundlibrary folder inside your beatmaps folder and put your sounds in there.", 12f, EditorManager.NotificationType.Error);
                                            return;
                                        }

                                        RTFileBrowser.inst.UpdateBrowserFile(directory, RTFile.AudioDotFormats, onSelectFile: _val =>
                                        {
                                            var global = Parser.TryParse(modifier.commands[1], false);
                                            _val = RTFile.ReplaceSlash(_val);
                                            if (_val.Contains(global ? RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH + "/" : RTFile.ReplaceSlash(RTFile.AppendEndSlash(RTFile.BasePath))))
                                            {
                                                str.transform.Find("Input").GetComponent<InputField>().text = _val.Replace(global ? RTFile.ApplicationDirectory + ModifiersManager.SOUNDLIBRARY_PATH + "/" : RTFile.ReplaceSlash(RTFile.AppendEndSlash(RTFile.BasePath)), "");
                                                EditorManager.inst.HideDialog("Browser Popup");
                                                return;
                                            }

                                            EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                        });
                                    })
                                    );
                            };
                            BoolGenerator(modifier, layout, "Global", 1, false);

                            break;
                        }

                    #endregion

                    #region String

                    case "usernameEquals":
                        {
                            StringGenerator(modifier, layout, "Username", 0);
                            break;
                        }
                    case "updateObject":
                    case "objectCollide":
                        {
                            PrefabGroupOnly(modifier, layout);
                            var str = StringGenerator(modifier, layout, "Object Group", 0);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                            break;
                        }
                    case "setTextOther":
                    case "addTextOther":
                    case "setImageOther":
                        {
                            PrefabGroupOnly(modifier, layout);
                            var str = StringGenerator(modifier, layout, "Object Group", 1);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            StringGenerator(modifier, layout, cmd == "setImageOther" ? "Path" : "Text", 0);

                            break;
                        }
                    case "loadLevel":
                    case "loadLevelInternal":
                    case "levelPathExists":
                    case "setText":
                    case "addText":
                    case "setImage":
                    case "setWindowTitle":
                    case "realTimeDayWeekEquals":
                    case "loadInterface":
                        {
                            StringGenerator(modifier, layout, cmd == "setText" || cmd == "addText" ? "Text" :
                                cmd == "setWindowTitle" ? "Title" :
                                cmd == "realTimeDayWeekEquals" ? "Day" :
                                "Path", 0);

                            break;
                        }
                    case "textSequence":
                        {
                            SingleGenerator(modifier, layout, "Length", 0, 1f);
                            BoolGenerator(modifier, layout, "Display Glitch", 1, true);
                            BoolGenerator(modifier, layout, "Play Sound", 2, true);
                            BoolGenerator(modifier, layout, "Custom Sound", 3, false);
                            StringGenerator(modifier, layout, "Sound Path", 4);
                            BoolGenerator(modifier, layout, "Global", 5, false);

                            SingleGenerator(modifier, layout, "Pitch", 6, 1f);
                            SingleGenerator(modifier, layout, "Volume", 7, 1f);
                            SingleGenerator(modifier, layout, "Pitch Vary", 8, 0f);
                            var str = StringGenerator(modifier, layout, "Custom Text", 9);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            SingleGenerator(modifier, layout, "Time Offset", 10, 0f);
                            BoolGenerator(modifier, layout, "Time Relative", 11, false);

                            break;
                        }
                    case "levelUnlocked":
                    case "loadLevelID":
                    case "loadLevelInCollection":
                    case "levelCompletedOther":
                    case "levelExists":
                        {
                            StringGenerator(modifier, layout, "ID", 0);

                            break;
                        }

                    case "downloadLevel":
                        {
                            StringGenerator(modifier, layout, "Arcade ID", 0);
                            StringGenerator(modifier, layout, "Server ID", 1);
                            StringGenerator(modifier, layout, "Workshop ID", 2);
                            StringGenerator(modifier, layout, "Song Title", 3);
                            StringGenerator(modifier, layout, "Level Name", 4);
                            BoolGenerator(modifier, layout, "Play Level", 5, true);

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
                            if (cmd.Contains("Other"))
                                PrefabGroupOnly(modifier, layout);
                            SingleGenerator(modifier, layout, "Amount", 0, 0.5f);

                            if (cmd == "blur" || cmd == "blurColored")
                                BoolGenerator(modifier, layout, "Use Opacity", 1, false);

                            if (cmd.Contains("Other"))
                            {
                                var str = StringGenerator(modifier, layout, "Object Group", 1);
                                EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            }

                            BoolGenerator(modifier, layout, "Set Back to Normal", cmd != "blurVariable" ? 2 : 1, false);

                            break;
                        }
                    case "particleSystem":
                        {
                            SingleGenerator(modifier, layout, "Life Time", 0, 5f);

                            // Shape
                            {
                                var dd = dropdownBar.Duplicate(layout, "Shape");
                                dd.transform.localScale = Vector3.one;
                                var labelText = dd.transform.Find("Text").GetComponent<Text>();
                                labelText.text = "Shape";

                                Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
                                Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

                                var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
                                d.onValueChanged.ClearAll();
                                d.options = CoreHelper.StringToOptionData("Square", "Circle", "Triangle", "Arrow", "Text", "Hexagon", "Image", "Pentagon", "Misc");

                                d.value = Parser.TryParse(modifier.commands[1], 0);

                                d.onValueChanged.AddListener(_val =>
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
                                    Updater.UpdateObject(beatmapObject);
                                });

                                EditorThemeManager.ApplyLightText(labelText);
                                EditorThemeManager.ApplyDropdown(d);

                                TriggerHelper.AddEventTriggers(d.gameObject, TriggerHelper.ScrollDelta(d));
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
                                d.onValueChanged.ClearAll();
                                d.options.Clear();

                                var type = Parser.TryParse(modifier.commands[1], 0);
                                for (int i = 0; i < ShapeManager.inst.Shapes2D[type].Count; i++)
                                {
                                    var shape = ShapeManager.inst.Shapes2D[type][i].name.Replace("_", " ");
                                    d.options.Add(new Dropdown.OptionData(shape, ShapeManager.inst.Shapes2D[type][i].icon));
                                }

                                d.value = Parser.TryParse(modifier.commands[2], 0);

                                d.onValueChanged.AddListener(_val =>
                                {
                                    modifier.commands[2] = Mathf.Clamp(_val, 0, ShapeManager.inst.Shapes2D[type].Count - 1).ToString();
                                    modifier.active = false;
                                    Updater.UpdateObject(beatmapObject);
                                });

                                EditorThemeManager.ApplyLightText(labelText);
                                EditorThemeManager.ApplyDropdown(d);

                                TriggerHelper.AddEventTriggers(d.gameObject, TriggerHelper.ScrollDelta(d));
                            }

                            ColorGenerator(modifier, layout, "Color", 3);
                            SingleGenerator(modifier, layout, "Start Opacity", 4, 1f);
                            SingleGenerator(modifier, layout, "End Opacity", 5, 0f);
                            SingleGenerator(modifier, layout, "Start Scale", 6, 1f);
                            SingleGenerator(modifier, layout, "End Scale", 7, 0f);
                            SingleGenerator(modifier, layout, "Rotation", 8, 0f);
                            SingleGenerator(modifier, layout, "Speed", 9, 5f);
                            SingleGenerator(modifier, layout, "Amount", 10, 1f);
                            SingleGenerator(modifier, layout, "Duration", 11, 1f);
                            SingleGenerator(modifier, layout, "Force X", 12, 0f);
                            SingleGenerator(modifier, layout, "Force Y", 13, 0f);
                            BoolGenerator(modifier, layout, "Emit Trail", 14, false);
                            SingleGenerator(modifier, layout, "Angle", 15, 0f);

                            break;
                        }
                    case "trailRenderer":
                        {
                            SingleGenerator(modifier, layout, "Time", 0, 1f);
                            SingleGenerator(modifier, layout, "Start Width", 1, 1f);
                            SingleGenerator(modifier, layout, "End Width", 2, 0f);
                            ColorGenerator(modifier, layout, "Start Color", 3);
                            SingleGenerator(modifier, layout, "Start Opacity", 4, 1f);
                            ColorGenerator(modifier, layout, "End Color", 5);
                            SingleGenerator(modifier, layout, "End Opacity", 6, 0f);

                            break;
                        }
                    case "rigidbody":
                    case "rigidbodyOther":
                        {
                            if (cmd == "rigidbodyOther")
                            {
                                PrefabGroupOnly(modifier, layout);
                                var str = StringGenerator(modifier, layout, "Object Group", 0);
                                EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            }

                            SingleGenerator(modifier, layout, "Gravity", 1, 0f);

                            DropdownGenerator(modifier, layout, "Collision Mode", 2, CoreHelper.StringToOptionData("Discrete", "Continuous"));

                            SingleGenerator(modifier, layout, "Drag", 3, 0f);
                            SingleGenerator(modifier, layout, "Velocity X", 4, 0f);
                            SingleGenerator(modifier, layout, "Velocity Y", 5, 0f);

                            DropdownGenerator(modifier, layout, "Body Type", 6, CoreHelper.StringToOptionData("Dynamic", "Kinematic", "Static"));

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
                    case "playerCountEquals":
                    case "playerCountLesserEquals":
                    case "playerCountGreaterEquals":
                    case "playerCountLesser":
                    case "playerCountGreater":
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
                            if (cmd == "addVariable" || cmd == "subVariable" || cmd == "setVariable" || cmd.Contains("variableOther") ||
                                cmd == "setAlphaOther" || cmd == "removeTextOther" || cmd == "removeTextOtherAt")
                                PrefabGroupOnly(modifier, layout);
                            IntegerGenerator(modifier, layout, "Value", 0, 0);

                            if (cmd == "addVariable" || cmd == "subVariable" || cmd == "setVariable" || cmd.Contains("variableOther") ||
                                cmd == "setAlphaOther" || cmd == "removeTextOther" || cmd == "removeTextOtherAt")
                            {
                                var str = StringGenerator(modifier, layout, "Object Group", 1);
                                EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            }

                            break;
                        }
                    #endregion

                    #region Key

                    case "keyPressDown":
                    case "keyPress":
                    case "keyPressUp":
                        {
                            var dropdownData = CoreHelper.ToDropdownData<KeyCode>();
                            DropdownGenerator(modifier, layout, "Key", 0, dropdownData.Key, dropdownData.Value);

                            break;
                        }

                    case "controlPressDown":
                    case "controlPress":
                    case "controlPressUp":
                        {
                            var dropdownData = CoreHelper.ToDropdownData<PlayerInputControlType>();
                            DropdownGenerator(modifier, layout, "Button", 0, dropdownData.Key, dropdownData.Value);

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

                            StringGenerator(modifier, layout, "Path", 1);
                            StringGenerator(modifier, layout, "JSON 1", 2);
                            StringGenerator(modifier, layout, "JSON 2", 3);

                            if (cmd != "saveVariable" && cmd != "saveText" && cmd != "loadExists" && cmd != "saveString" && (cmd != "loadEquals" || Parser.TryParse(modifier.commands[4], 0) == 0))
                                SingleGenerator(modifier, layout, "Value", 0, 0f);

                            if (cmd == "saveString" || cmd == "loadEquals" && Parser.TryParse(modifier.commands[4], 0) == 1)
                                StringGenerator(modifier, layout, "Value", 0);

                            if (cmd == "loadEquals")
                                DropdownGenerator(modifier, layout, "Type", 4, CoreHelper.StringToOptionData("Number", "Text"));

                            break;
                        }
                    case "loadVariable":
                    case "loadVariableOther":
                        {
                            if (cmd.Contains("Other"))
                            {
                                PrefabGroupOnly(modifier, layout);
                                var str = StringGenerator(modifier, layout, "Object Group", 0);
                                EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            }

                            StringGenerator(modifier, layout, "Path", 1);
                            StringGenerator(modifier, layout, "JSON 1", 2);
                            StringGenerator(modifier, layout, "JSON 2", 3);

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
                            SingleGenerator(modifier, layout, "Total Multiply", 0, 1f);

                            if (cmd == "reactivePos" || cmd == "reactiveSca" || cmd == "reactivePosChain" || cmd == "reactiveScaChain")
                            {
                                var samplesX = numberInput.Duplicate(layout, "Value");
                                var samplesXLabel = samplesX.transform.Find("Text").GetComponent<Text>();
                                samplesXLabel.text = "Sample X";

                                var samplesXIF = samplesX.transform.Find("Input").GetComponent<InputField>();
                                samplesXIF.onValueChanged.ClearAll();
                                samplesXIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                samplesXIF.text = Parser.TryParse(modifier.commands[1], 0).ToString();
                                samplesXIF.onValueChanged.AddListener(_val =>
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
                                samplesYIF.onValueChanged.AddListener(_val =>
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
                                TriggerHelper.AddEventTriggers(samplesXIF.gameObject,
                                    TriggerHelper.ScrollDeltaInt(samplesXIF, multi: true),
                                    TriggerHelper.ScrollDeltaVector2Int(samplesXIF, samplesYIF, 1, new List<int> { 0, 255 }));
                                TriggerHelper.AddEventTriggers(samplesYIF.gameObject,
                                    TriggerHelper.ScrollDeltaInt(samplesYIF, multi: true),
                                    TriggerHelper.ScrollDeltaVector2Int(samplesXIF, samplesYIF, 1, new List<int> { 0, 255 }));

                                var multiplyX = numberInput.Duplicate(layout, "Value");
                                var multiplyXLabel = multiplyX.transform.Find("Text").GetComponent<Text>();
                                multiplyXLabel.text = "Multiply X";

                                var multiplyXIF = multiplyX.transform.Find("Input").GetComponent<InputField>();
                                multiplyXIF.onValueChanged.ClearAll();
                                multiplyXIF.textComponent.alignment = TextAnchor.MiddleCenter;
                                multiplyXIF.text = Parser.TryParse(modifier.commands[3], 0f).ToString();
                                multiplyXIF.onValueChanged.AddListener(_val =>
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
                                multiplyYIF.onValueChanged.AddListener(_val =>
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
                                TriggerHelper.AddEventTriggers(multiplyXIF.gameObject,
                                    TriggerHelper.ScrollDelta(multiplyXIF, multi: true),
                                    TriggerHelper.ScrollDeltaVector2(multiplyXIF, multiplyYIF, 0.1f, 10f));
                                TriggerHelper.AddEventTriggers(multiplyYIF.gameObject,
                                    TriggerHelper.ScrollDelta(multiplyYIF, multi: true),
                                    TriggerHelper.ScrollDeltaVector2(multiplyXIF, multiplyYIF, 0.1f, 10f));
                            }
                            else
                            {
                                IntegerGenerator(modifier, layout, "Sample", 1, 0);

                                if (cmd == "reactiveCol" || cmd == "reactiveColLerp")
                                    ColorGenerator(modifier, layout, "Color", 2);
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
                            inputField.onValueChanged.AddListener(_val =>
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
                            TriggerHelper.AddEventTriggers(inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputField, 1, 0, 3));

                            StringGenerator(modifier, layout, "Model ID", 0);

                            break;
                        }
                    case "eventOffset":
                    case "eventOffsetVariable":
                    case "eventOffsetAnimate":
                    case "eventOffsetMath":
                        {
                            // Event Keyframe Type
                            DropdownGenerator(modifier, layout, "Event Type", 1, CoreHelper.StringToOptionData(RTEventEditor.EventTypes));

                            var vindex = numberInput.Duplicate(layout, "Value");
                            var labelText = vindex.transform.Find("Text").GetComponent<Text>();
                            labelText.text = "Val Index";

                            var vindexIF = vindex.transform.Find("Input").GetComponent<InputField>();
                            vindexIF.onValueChanged.ClearAll();
                            vindexIF.textComponent.alignment = TextAnchor.MiddleCenter;
                            vindexIF.text = Parser.TryParse(modifier.commands[2], 0).ToString();
                            vindexIF.onValueChanged.AddListener(_val =>
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
                            TriggerHelper.AddEventTriggers(vindexIF.gameObject, TriggerHelper.ScrollDeltaInt(vindexIF, 1, 0, GameData.DefaultKeyframes[Parser.TryParse(modifier.commands[1], 0)].eventValues.Length - 1));

                            if (cmd == "eventOffsetMath")
                                StringGenerator(modifier, layout, "Value", 0);
                            else
                                SingleGenerator(modifier, layout, cmd == "eventOffsetVariable" ? "Multiply Var" : "Value", 0, 0f);

                            if (cmd == "eventOffsetAnimate")
                            {
                                SingleGenerator(modifier, layout, "Time", 3, 1f);
                                DropdownGenerator(modifier, layout, "Easing", 4, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());
                                BoolGenerator(modifier, layout, "Relative", 5, false);
                            }

                            break;
                        }

                    #endregion

                    #region Color

                    case "copyColor":
                    case "copyColorOther":
                        {
                            PrefabGroupOnly(modifier, layout);
                            var str = StringGenerator(modifier, layout, "Object Group", 0);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            BoolGenerator(modifier, layout, "Apply Color 1", 1, true);
                            BoolGenerator(modifier, layout, "Apply Color 2", 2, true);

                            break;
                        }
                    case "addColor":
                    case "addColorOther":
                    case "lerpColor":
                    case "lerpColorOther":
                        {
                            if (cmd.Contains("Other"))
                            {
                                PrefabGroupOnly(modifier, layout);
                                var str = StringGenerator(modifier, layout, "Object Group", 1);
                                EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            }

                            ColorGenerator(modifier, layout, "Color", !cmd.Contains("Other") ? 1 : 2);

                            SingleGenerator(modifier, layout, "Hue", !cmd.Contains("Other") ? 2 : 3, 0f);
                            SingleGenerator(modifier, layout, "Saturation", !cmd.Contains("Other") ? 3 : 4, 0f);
                            SingleGenerator(modifier, layout, "Value", !cmd.Contains("Other") ? 4 : 5, 0f);

                            SingleGenerator(modifier, layout, "Multiply", 0, 1f);

                            break;
                        }
                    case "addColorPlayerDistance":
                    case "lerpColorPlayerDistance":
                        {
                            ColorGenerator(modifier, layout, "Color", 1);
                            SingleGenerator(modifier, layout, "Multiply", 0, 1f);
                            SingleGenerator(modifier, layout, "Offset", 2, 10f);

                            if (cmd == "lerpColorPlayerDistance")
                            {
                                SingleGenerator(modifier, layout, "Opacity", 3, 1f);
                                SingleGenerator(modifier, layout, "Hue", 4, 0f);
                                SingleGenerator(modifier, layout, "Saturation", 5, 0f);
                                SingleGenerator(modifier, layout, "Value", 6, 0f);
                            }

                            break;
                        }
                    case "applyColorGroup":
                        {
                            PrefabGroupOnly(modifier, layout);
                            var str = StringGenerator(modifier, layout, "Object Group", 0);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            DropdownGenerator(modifier, layout, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                            DropdownGenerator(modifier, layout, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                            break;
                        }
                    case "setColorHex":
                    case "setColorHexOther":
                        {
                            if (cmd.Contains("Other"))
                            {
                                PrefabGroupOnly(modifier, layout);
                                var str = StringGenerator(modifier, layout, "Object Group", 1);
                                EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            }

                            StringGenerator(modifier, layout, "Hex Code", 0);
                            StringGenerator(modifier, layout, "Hex Gradient Color", cmd.Contains("Other") ? 2 : 1);
                            break;
                        }

                    #endregion

                    #region Signal
                    case "signalModifier":
                    case "mouseOverSignalModifier":
                        {
                            PrefabGroupOnly(modifier, layout);
                            var str = StringGenerator(modifier, layout, "Object Group", 1);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            SingleGenerator(modifier, layout, "Delay", 0, 0f);

                            break;
                        }
                    case "activateModifier":
                        {
                            PrefabGroupOnly(modifier, layout);
                            var str = StringGenerator(modifier, layout, "Object Group", 0);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            BoolGenerator(modifier, layout, "Do Multiple", 1, true);
                            IntegerGenerator(modifier, layout, "Singlular Index", 2, 0);

                            for (int i = 3; i < modifier.commands.Count; i++)
                            {
                                int groupIndex = i;
                                var label = stringInput.Duplicate(layout, "group label");
                                label.transform.localScale = Vector3.one;
                                var groupLabel = label.transform.Find("Text").GetComponent<Text>();
                                groupLabel.text = $" Name {i + 1}";
                                label.transform.Find("Text").AsRT().sizeDelta = new Vector2(268f, 32f);
                                Destroy(label.transform.Find("Input").gameObject);

                                StringGenerator(modifier, layout, "Modifier Name", groupIndex);

                                var deleteGroup = gameObject.transform.Find("Label/Delete").gameObject.Duplicate(label.transform, "delete");
                                var deleteGroupButton = deleteGroup.GetComponent<DeleteButtonStorage>();
                                deleteGroup.GetComponent<LayoutElement>().ignoreLayout = false;
                                deleteGroupButton.button.onClick.ClearAll();
                                deleteGroupButton.button.onClick.AddListener(() =>
                                {
                                    modifier.commands.RemoveAt(groupIndex);

                                    Updater.UpdateObject(beatmapObject);
                                    StartCoroutine(RenderModifiers(beatmapObject));
                                });

                                EditorThemeManager.ApplyGraphic(deleteGroupButton.button.image, ThemeGroup.Delete, true);
                                EditorThemeManager.ApplyGraphic(deleteGroupButton.image, ThemeGroup.Delete_Text);
                            }

                            AddGenerator(modifier, layout, "Add Group", () =>
                            {
                                modifier.commands.Add("modifierName");

                                Updater.UpdateObject(beatmapObject);
                                StartCoroutine(RenderModifiers(beatmapObject));
                            });

                            break;
                        }
                    #endregion

                    #region Random

                    case "randomGreater":
                    case "randomLesser":
                    case "randomEquals":
                        {
                            IntegerGenerator(modifier, layout, "Minimum", 1, 0);
                            IntegerGenerator(modifier, layout, "Maximum", 2, 0);
                            IntegerGenerator(modifier, layout, "Compare To", 0, 0);

                            break;
                        }
                    case "setVariableRandom":
                        {
                            PrefabGroupOnly(modifier, layout);
                            var str = StringGenerator(modifier, layout, "Object Group", 0);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            IntegerGenerator(modifier, layout, "Minimum", 1, 0);
                            IntegerGenerator(modifier, layout, "Maximum", 2, 0);

                            break;
                        }

                    #endregion

                    #region Editor

                    case "editorNotify":
                        {
                            StringGenerator(modifier, layout, "Text", 0);
                            SingleGenerator(modifier, layout, "Time", 1, 0.5f);
                            DropdownGenerator(modifier, layout, "Notify Type", 2, CoreHelper.StringToOptionData("Info", "Success", "Error", "Warning"));

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
                            xPositionIF.onValueChanged.AddListener(_val =>
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
                                yPositionIF.onValueChanged.AddListener(_val =>
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
                                TriggerHelper.AddEventTriggers(yPositionIF.gameObject,
                                    TriggerHelper.ScrollDelta(yPositionIF),
                                    TriggerHelper.ScrollDeltaVector2(xPositionIF, yPositionIF, 0.1f, 10f));

                            }
                            else
                            {
                                TriggerHelper.IncreaseDecreaseButtons(xPositionIF, t: xPosition.transform);
                                TriggerHelper.AddEventTriggers(xPositionIF.gameObject, TriggerHelper.ScrollDelta(xPositionIF));
                            }

                            var single = numberInput.Duplicate(layout, "Duration");
                            var singleText = single.transform.Find("Text").GetComponent<Text>();
                            singleText.text = "Duration";

                            var inputField = single.transform.Find("Input").GetComponent<InputField>();
                            inputField.onValueChanged.ClearAll();
                            inputField.textComponent.alignment = TextAnchor.MiddleCenter;
                            inputField.text = Parser.TryParse(modifier.commands[1], 1f).ToString();
                            inputField.onValueChanged.AddListener(_val =>
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
                            TriggerHelper.AddEventTriggers(inputField.gameObject, TriggerHelper.ScrollDelta(inputField));

                            DropdownGenerator(modifier, layout, "Easing", 2, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                            BoolGenerator(modifier, layout, "Relative", 3, false);

                            break;
                        }

                    #endregion

                    #region Prefab

                    case "spawnPrefab":
                    case "spawnPrefabOffset":
                    case "spawnPrefabOffsetOther":
                    case "spawnMultiPrefab":
                    case "spawnMultiPrefabOffset":
                    case "spawnMultiPrefabOffsetOther":
                        {
                            var isMulti = cmd.Contains("Multi");
                            var isOther = cmd.Contains("Other");
                            if (isOther)
                            {
                                PrefabGroupOnly(modifier, layout);
                                var str = StringGenerator(modifier, layout, "Object Group", isMulti ? 9 : 10);
                                EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            }

                            var prefabIndex = numberInput.Duplicate(layout, "Index");
                            var prefabIndexLabel = prefabIndex.transform.Find("Text").GetComponent<Text>();
                            prefabIndexLabel.text = "Prefab Index";

                            var prefabIndexIF = prefabIndex.transform.Find("Input").GetComponent<InputField>();
                            prefabIndexIF.onValueChanged.ClearAll();
                            prefabIndexIF.textComponent.alignment = TextAnchor.MiddleCenter;
                            prefabIndexIF.text = Parser.TryParse(modifier.value, 0).ToString();
                            prefabIndexIF.onValueChanged.AddListener(_val =>
                            {
                                if (int.TryParse(_val, out int result))
                                {
                                    try
                                    {
                                        modifier.Inactive?.Invoke(modifier);
                                    }
                                    catch (Exception ex)
                                    {
                                        CoreHelper.LogException(ex);
                                    }
                                    modifier.value = Mathf.Clamp(result, 0, GameData.Current.prefabs.Count - 1).ToString();
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

                            TriggerHelper.IncreaseDecreaseButtonsInt(prefabIndexIF, 1, 0, GameData.Current.prefabs.Count - 1, prefabIndex.transform);
                            TriggerHelper.AddEventTriggers(prefabIndexIF.gameObject, TriggerHelper.ScrollDeltaInt(prefabIndexIF, 1, 0, GameData.Current.prefabs.Count - 1));

                            SingleGenerator(modifier, layout, "Position X", 1, 0f);
                            SingleGenerator(modifier, layout, "Position Y", 2, 0f);
                            SingleGenerator(modifier, layout, "Scale X", 3, 0f);
                            SingleGenerator(modifier, layout, "Scale Y", 4, 0f);
                            SingleGenerator(modifier, layout, "Rotation", 5, 0f, 15f, 3f);

                            IntegerGenerator(modifier, layout, "Repeat Count", 6, 0);
                            SingleGenerator(modifier, layout, "Repeat Offset Time", 7, 0f);
                            SingleGenerator(modifier, layout, "Speed", 8, 1f);

                            if (!isMulti)
                                BoolGenerator(modifier, layout, "Permanent", 9, false);

                            int valueIndex = 10;
                            if (isOther)
                                valueIndex++;
                            if (isMulti)
                                valueIndex--;

                            SingleGenerator(modifier, layout, "Time Offset", valueIndex, 0f);
                            BoolGenerator(modifier, layout, "Time Relative", valueIndex + 1, true);

                            break;
                        }

                    case "clearSpawnedPrefabs":
                        {
                            PrefabGroupOnly(modifier, layout);
                            var str = StringGenerator(modifier, layout, "Object Group", 0);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                            break;
                        }

                    #endregion

                    #region Clamp Variable

                    case "clampVariable":
                    case "clampVariableOther":
                        {
                            if (cmd == "clampVariableOther")
                            {
                                PrefabGroupOnly(modifier, layout);
                                var str = StringGenerator(modifier, layout, "Object Group", 0);
                                EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            }

                            IntegerGenerator(modifier, layout, "Minimum", 1, 0);
                            IntegerGenerator(modifier, layout, "Maximum", 2, 0);

                            break;
                        }

                    #endregion

                    #region Animate

                    case "animateObject":
                    case "animateObjectOther":
                    case "animateSignal":
                    case "animateSignalOther":
                    case "animateObjectMath":
                    case "animateObjectMathOther":
                    case "animateSignalMath":
                    case "animateSignalMathOther":
                        {
                            if (cmd.Contains("Signal") || cmd.Contains("Other"))
                                PrefabGroupOnly(modifier, layout);

                            if (cmd.Contains("Math"))
                                StringGenerator(modifier, layout, "Time", 0);
                            else
                                SingleGenerator(modifier, layout, "Time", 0, 1f);

                            DropdownGenerator(modifier, layout, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

                            if (cmd.Contains("Math"))
                            {
                                StringGenerator(modifier, layout, "X", 2);
                                StringGenerator(modifier, layout, "Y", 3);
                                StringGenerator(modifier, layout, "Z", 4);
                            }
                            else
                            {
                                SingleGenerator(modifier, layout, "X", 2, 0f);
                                SingleGenerator(modifier, layout, "Y", 3, 0f);
                                SingleGenerator(modifier, layout, "Z", 4, 0f);
                            }

                            BoolGenerator(modifier, layout, "Relative", 5, true);

                            DropdownGenerator(modifier, layout, "Easing", 6, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                            if (cmd.Contains("Other"))
                            {
                                var str = StringGenerator(modifier, layout, "Object Group", 7);
                                EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            }

                            if (cmd.Contains("Signal"))
                            {
                                int m = 0;
                                if (cmd.Contains("Other"))
                                    m = 1;

                                StringGenerator(modifier, layout, "Signal Group", 7 + m);
                                if (cmd.Contains("Math"))
                                    StringGenerator(modifier, layout, "Signal Delay", 8 + m);
                                else
                                    SingleGenerator(modifier, layout, "Signal Delay", 8 + m, 0f);
                                BoolGenerator(modifier, layout, "Signal Deactivate", 9 + m, true);
                            }

                            break;
                        }
                    case "animateVariableOther":
                        {
                            PrefabGroupOnly(modifier, layout);
                            var str = StringGenerator(modifier, layout, "Object Group", 0);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                            DropdownGenerator(modifier, layout, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                            DropdownGenerator(modifier, layout, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                            SingleGenerator(modifier, layout, "Delay", 3, 0f);

                            SingleGenerator(modifier, layout, "Multiply", 4, 1f);
                            SingleGenerator(modifier, layout, "Offset", 5, 0f);
                            SingleGenerator(modifier, layout, "Min", 6, -99999f);
                            SingleGenerator(modifier, layout, "Max", 7, 99999f);
                            SingleGenerator(modifier, layout, "Loop", 8, 99999f);

                            break;
                        }
                    case "copyAxis":
                    case "copyPlayerAxis":
                        {
                            if (cmd == "copyAxis")
                            {
                                PrefabGroupOnly(modifier, layout);
                                var str = StringGenerator(modifier, layout, "Object Group", 0);
                                EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            }

                            DropdownGenerator(modifier, layout, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
                            DropdownGenerator(modifier, layout, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                            DropdownGenerator(modifier, layout, "To Type", 3, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
                            DropdownGenerator(modifier, layout, "To Axis (3D)", 4, CoreHelper.StringToOptionData("X", "Y", "Z"));

                            if (cmd == "copyAxis")
                                SingleGenerator(modifier, layout, "Delay", 5, 0f);

                            SingleGenerator(modifier, layout, "Multiply", 6, 1f);
                            SingleGenerator(modifier, layout, "Offset", 7, 0f);
                            SingleGenerator(modifier, layout, "Min", 8, -99999f);
                            SingleGenerator(modifier, layout, "Max", 9, 99999f);

                            if (cmd == "copyAxis")
                            {
                                SingleGenerator(modifier, layout, "Loop", 10, 99999f);
                                BoolGenerator(modifier, layout, "Use Visual", 11, false);
                            }

                            break;
                        }
                    case "copyAxisMath":
                        {
                            PrefabGroupOnly(modifier, layout);
                            var str = StringGenerator(modifier, layout, "Object Group", 0);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                            DropdownGenerator(modifier, layout, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
                            DropdownGenerator(modifier, layout, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                            DropdownGenerator(modifier, layout, "To Type", 3, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
                            DropdownGenerator(modifier, layout, "To Axis (3D)", 4, CoreHelper.StringToOptionData("X", "Y", "Z"));

                            SingleGenerator(modifier, layout, "Delay", 5, 0f);

                            SingleGenerator(modifier, layout, "Min", 6, -99999f);
                            SingleGenerator(modifier, layout, "Max", 7, 99999f);
                            BoolGenerator(modifier, layout, "Use Visual", 9, false);
                            StringGenerator(modifier, layout, "Expression", 8);

                            break;
                        }
                    case "copyAxisGroup":
                        {
                            PrefabGroupOnly(modifier, layout);
                            StringGenerator(modifier, layout, "Expression", 0);

                            DropdownGenerator(modifier, layout, "To Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                            DropdownGenerator(modifier, layout, "To Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                            int a = 0;
                            for (int i = 3; i < modifier.commands.Count; i += 8)
                            {
                                int groupIndex = i;
                                var label = stringInput.Duplicate(layout, "group label");
                                label.transform.localScale = Vector3.one;
                                var groupLabel = label.transform.Find("Text").GetComponent<Text>();
                                groupLabel.text = $"Group {a + 1}";
                                label.transform.Find("Text").AsRT().sizeDelta = new Vector2(268f, 32f);
                                Destroy(label.transform.Find("Input").gameObject);

                                var deleteGroup = gameObject.transform.Find("Label/Delete").gameObject.Duplicate(label.transform, "delete");
                                deleteGroup.GetComponent<LayoutElement>().ignoreLayout = false;
                                var deleteGroupButton = deleteGroup.GetComponent<DeleteButtonStorage>();
                                deleteGroupButton.button.onClick.ClearAll();
                                deleteGroupButton.button.onClick.AddListener(() =>
                                {
                                    for (int j = 0; j < 8; j++)
                                        modifier.commands.RemoveAt(groupIndex);

                                    Updater.UpdateObject(beatmapObject);
                                    StartCoroutine(RenderModifiers(beatmapObject));
                                });

                                EditorThemeManager.ApplyGraphic(deleteGroupButton.button.image, ThemeGroup.Delete, true);
                                EditorThemeManager.ApplyGraphic(deleteGroupButton.image, ThemeGroup.Delete_Text);

                                var groupName = StringGenerator(modifier, layout, "Name", i);
                                EditorHelper.AddInputFieldContextMenu(groupName.transform.Find("Input").GetComponent<InputField>());
                                var str = StringGenerator(modifier, layout, "Object Group", i + 1);
                                EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                                DropdownGenerator(modifier, layout, "From Type", i + 2, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color", "Variable"));
                                DropdownGenerator(modifier, layout, "From Axis", i + 3, CoreHelper.StringToOptionData("X", "Y", "Z"));
                                SingleGenerator(modifier, layout, "Delay", i + 4, 0f);
                                SingleGenerator(modifier, layout, "Min", i + 5, -9999f);
                                SingleGenerator(modifier, layout, "Max", i + 6, 9999f);
                                BoolGenerator(modifier, layout, "Use Visual", 7, false);

                                a++;
                            }

                            AddGenerator(modifier, layout, "Add Group", () =>
                            {
                                var lastIndex = modifier.commands.Count - 1;

                                modifier.commands.Add($"var_{a}");
                                modifier.commands.Add("Object Group");
                                modifier.commands.Add("0");
                                modifier.commands.Add("0");
                                modifier.commands.Add("0");
                                modifier.commands.Add("-9999");
                                modifier.commands.Add("9999");
                                modifier.commands.Add("False");

                                Updater.UpdateObject(beatmapObject);
                                StartCoroutine(RenderModifiers(beatmapObject));
                            });

                            break;
                        }
                    case "eventOffsetCopyAxis":
                        {
                            DropdownGenerator(modifier, layout, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
                            DropdownGenerator(modifier, layout, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                            DropdownGenerator(modifier, layout, "To Type", 3, CoreHelper.StringToOptionData(RTEventEditor.EventTypes));
                            IntegerGenerator(modifier, layout, "To Axis", 4, 0);

                            SingleGenerator(modifier, layout, "Delay", 5, 0f);

                            SingleGenerator(modifier, layout, "Multiply", 6, 1f);
                            SingleGenerator(modifier, layout, "Offset", 7, 0f);
                            SingleGenerator(modifier, layout, "Min", 8, -99999f);
                            SingleGenerator(modifier, layout, "Max", 9, 99999f);

                            SingleGenerator(modifier, layout, "Loop", 10, 99999f);
                            BoolGenerator(modifier, layout, "Use Visual", 11, false);

                            break;
                        }
                    case "axisEquals":
                    case "axisLesserEquals":
                    case "axisGreaterEquals":
                    case "axisLesser":
                    case "axisGreater":
                        {
                            PrefabGroupOnly(modifier, layout);
                            var str = StringGenerator(modifier, layout, "Object Group", 0);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                            DropdownGenerator(modifier, layout, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                            DropdownGenerator(modifier, layout, "Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                            SingleGenerator(modifier, layout, "Delay", 3, 0f);

                            SingleGenerator(modifier, layout, "Multiply", 4, 1f);
                            SingleGenerator(modifier, layout, "Offset", 5, 0f);
                            SingleGenerator(modifier, layout, "Min", 6, -99999f);
                            SingleGenerator(modifier, layout, "Max", 7, 99999f);
                            SingleGenerator(modifier, layout, "Loop", 10, 99999f);
                            BoolGenerator(modifier, layout, "Use Visual", 9, false);

                            SingleGenerator(modifier, layout, "Equals", 8, 1f);

                            break;
                        }
                    case "applyAnimationFrom":
                    case "applyAnimationTo":
                    case "applyAnimation":
                        {
                            PrefabGroupOnly(modifier, layout);
                            if (cmd != "applyAnimation")
                            {
                                var str = StringGenerator(modifier, layout, "Object Group", 0);
                                EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            }
                            else
                            {
                                var from = StringGenerator(modifier, layout, "From Group", 0);
                                EditorHelper.AddInputFieldContextMenu(from.transform.Find("Input").GetComponent<InputField>());
                                var to = StringGenerator(modifier, layout, "To Group", 10);
                                EditorHelper.AddInputFieldContextMenu(to.transform.Find("Input").GetComponent<InputField>());
                            }

                            BoolGenerator(modifier, layout, "Animate Position", 1, true);
                            BoolGenerator(modifier, layout, "Animate Scale", 2, true);
                            BoolGenerator(modifier, layout, "Animate Rotation", 3, true);
                            SingleGenerator(modifier, layout, "Delay Position", 4, 0f);
                            SingleGenerator(modifier, layout, "Delay Scale", 5, 0f);
                            SingleGenerator(modifier, layout, "Delay Rotation", 6, 0f);
                            BoolGenerator(modifier, layout, "Use Visual", 7, false);
                            SingleGenerator(modifier, layout, "Length", 8, 1f);
                            SingleGenerator(modifier, layout, "Speed", 9, 1f);

                            break;
                        }
                    case "applyAnimationFromMath":
                    case "applyAnimationToMath":
                    case "applyAnimationMath":
                        {
                            PrefabGroupOnly(modifier, layout);
                            if (cmd != "applyAnimationMath")
                            {
                                var str = StringGenerator(modifier, layout, "Object Group", 0);
                                EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            }
                            else
                            {
                                var from = StringGenerator(modifier, layout, "From Group", 0);
                                EditorHelper.AddInputFieldContextMenu(from.transform.Find("Input").GetComponent<InputField>());
                                var to = StringGenerator(modifier, layout, "To Group", 10);
                                EditorHelper.AddInputFieldContextMenu(to.transform.Find("Input").GetComponent<InputField>());
                            }

                            BoolGenerator(modifier, layout, "Animate Position", 1, true);
                            BoolGenerator(modifier, layout, "Animate Scale", 2, true);
                            BoolGenerator(modifier, layout, "Animate Rotation", 3, true);
                            StringGenerator(modifier, layout, "Delay Position", 4);
                            StringGenerator(modifier, layout, "Delay Scale", 5);
                            StringGenerator(modifier, layout, "Delay Rotation", 6);
                            BoolGenerator(modifier, layout, "Use Visual", 7, false);
                            StringGenerator(modifier, layout, "Length", 8);
                            StringGenerator(modifier, layout, "Speed", 9);
                            StringGenerator(modifier, layout, "Time", cmd != "applyAnimationMath" ? 10 : 11);

                            break;
                        }

                    #endregion

                    #region Gravity

                    case "gravity":
                    case "gravityOther":
                        {
                            if (cmd == "gravityOther")
                            {
                                PrefabGroupOnly(modifier, layout);
                                var str = StringGenerator(modifier, layout, "Object Group", 0);
                                EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            }

                            SingleGenerator(modifier, layout, "X", 1, -1f);
                            SingleGenerator(modifier, layout, "Y", 2, 0f);
                            SingleGenerator(modifier, layout, "Time Multiply", 3, 1f);
                            IntegerGenerator(modifier, layout, "Curve", 4, 2);

                            break;
                        }

                    #endregion

                    #region Enable / Disable

                    case "enableObject":
                    case "disableObject":
                        {
                            BoolGenerator(modifier, layout, "Reset", 1, true);
                            break;
                        }

                    case "enableObjectTree":
                    case "disableObjectTree":
                        {
                            if (modifier.value == "0")
                                modifier.value = "False";

                            BoolGenerator(modifier, layout, "Use Self", 0, true);
                            BoolGenerator(modifier, layout, "Reset", 1, true);

                            break;
                        }
                    case "enableObjectTreeOther":
                    case "disableObjectTreeOther":
                        {
                            PrefabGroupOnly(modifier, layout);
                            var str = StringGenerator(modifier, layout, "Object Group", 1);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            BoolGenerator(modifier, layout, "Use Self", 0, true);
                            BoolGenerator(modifier, layout, "Reset", 2, true);

                            break;
                        }
                    case "enableObjectOther":
                    case "disableObjectOther":
                        {
                            PrefabGroupOnly(modifier, layout);
                            var str = StringGenerator(modifier, layout, "Object Group", 0);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            BoolGenerator(modifier, layout, "Reset", 1, true);

                            break;
                        }

                    #endregion

                    #region Level Rank

                    case "levelRankEquals":
                    case "levelRankLesserEquals":
                    case "levelRankGreaterEquals":
                    case "levelRankLesser":
                    case "levelRankGreater":
                    case "levelRankOtherEquals":
                    case "levelRankOtherLesserEquals":
                    case "levelRankOtherGreaterEquals":
                    case "levelRankOtherLesser":
                    case "levelRankOtherGreater":
                    case "levelRankCurrentEquals":
                    case "levelRankCurrentLesserEquals":
                    case "levelRankCurrentGreaterEquals":
                    case "levelRankCurrentLesser":
                    case "levelRankCurrentGreater":
                        {
                            if (cmd.Contains("Other"))
                                StringGenerator(modifier, layout, "ID", 1);

                            DropdownGenerator(modifier, layout, "Rank", 0, DataManager.inst.levelRanks.Select(x => x.name).ToList());

                            break;
                        }

                    case "addHit":
                    case "addDeath":
                        {
                            BoolGenerator(modifier, layout, "Use Self Position", 0, true);
                            StringGenerator(modifier, layout, "Time", 1);

                            break;
                        }

                    #endregion

                    #region Discord

                    case "setDiscordStatus":
                        {
                            StringGenerator(modifier, layout, "State", 0);
                            StringGenerator(modifier, layout, "Details", 1);
                            DropdownGenerator(modifier, layout, "Sub Icon", 2, CoreHelper.StringToOptionData("Arcade", "Editor", "Play", "Menu"));
                            DropdownGenerator(modifier, layout, "Icon", 3, CoreHelper.StringToOptionData("PA Logo White", "PA Logo Black"));

                            break;
                        }

                    #endregion

                    #region BG

                    case "setBGActive":
                        {
                            BoolGenerator(modifier, layout, "Active", 0, false);
                            var str = StringGenerator(modifier, layout, "BG Group", 1);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                            break;
                        }

                    #endregion

                    #region Math

                    case "mathEquals":
                    case "mathLesserEquals":
                    case "mathGreaterEquals":
                    case "mathLesser":
                    case "mathGreater":
                        {
                            StringGenerator(modifier, layout, "First", 0);
                            StringGenerator(modifier, layout, "Second", 1);

                            break;
                        }

                    case "setPitchMath":
                    case "addPitchMath":
                        {
                            StringGenerator(modifier, layout, "Pitch", 0);
                            break;
                        }

                    case "setMusicTimeMath":
                        {
                            StringGenerator(modifier, layout, "Time", 0);
                            break;
                        }

                    #endregion

                    #region Misc

                    case "objectAlive":
                    case "objectSpawned":
                        {
                            PrefabGroupOnly(modifier, layout);
                            var str = StringGenerator(modifier, layout, "Object Group", 0);
                            EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());

                            break;
                        }

                    case "gameMode":
                        {
                            DropdownGenerator(modifier, layout, "Mode", 0, CoreHelper.StringToOptionData("Regular", "Platformer"));

                            break;
                        }
                    case "setCollision":
                    case "setCollisionOther":
                        {
                            BoolGenerator(modifier, layout, "On", 0, false);

                            if (cmd == "setCollisionOther")
                            {
                                PrefabGroupOnly(modifier, layout);
                                var str = StringGenerator(modifier, layout, "Object Group", 1);
                                EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                            }

                            break;
                        }
                    case "playerVelocityAll":
                        {
                            SingleGenerator(modifier, layout, "X", 1, 0f);
                            SingleGenerator(modifier, layout, "Y", 2, 0f);

                            break;
                        }
                    case "playerVelocityXAll":
                    case "playerVelocityYAll":
                        {
                            SingleGenerator(modifier, layout, cmd == "playerVelocityXAll" ? "X" : "Y", 0, 0f);

                            break;
                        }
                    case "legacyTail":
                        {
                            SingleGenerator(modifier, layout, "Total Time", 0, 200f);

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
                                deleteGroup.GetComponent<LayoutElement>().ignoreLayout = false;
                                deleteGroupButton.button.onClick.ClearAll();
                                deleteGroupButton.button.onClick.AddListener(() =>
                                {
                                    for (int j = 0; j < 3; j++)
                                        modifier.commands.RemoveAt(groupIndex);

                                    Updater.UpdateObject(beatmapObject);
                                    StartCoroutine(RenderModifiers(beatmapObject));
                                });

                                EditorThemeManager.ApplyGraphic(deleteGroupButton.button.image, ThemeGroup.Delete, true);
                                EditorThemeManager.ApplyGraphic(deleteGroupButton.image, ThemeGroup.Delete_Text);

                                var str = StringGenerator(modifier, layout, "Object Group", i);
                                EditorHelper.AddInputFieldContextMenu(str.transform.Find("Input").GetComponent<InputField>());
                                SingleGenerator(modifier, layout, "Distance", i + 1, 2f);
                                SingleGenerator(modifier, layout, "Time", i + 2, 12f);
                            }

                            AddGenerator(modifier, layout, "Add Group", () =>
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

                                Updater.UpdateObject(beatmapObject);
                                StartCoroutine(RenderModifiers(beatmapObject));
                            });

                            break;
                        }
                    case "setMousePosition":
                        {
                            IntegerGenerator(modifier, layout, "Position X", 1, 0);
                            IntegerGenerator(modifier, layout, "Position Y", 1, 0);

                            break;
                        }
                    case "followMousePosition":
                        {
                            SingleGenerator(modifier, layout, "Position Focus", 0, 1f);
                            SingleGenerator(modifier, layout, "Rotation Delay", 1, 1f);
                            break;
                        }
                    case "translateShape":
                        {
                            SingleGenerator(modifier, layout, "Pos X", 1, 0f);
                            SingleGenerator(modifier, layout, "Pos Y", 2, 0f);
                            SingleGenerator(modifier, layout, "Sca X", 3, 0f);
                            SingleGenerator(modifier, layout, "Sca Y", 4, 0f);
                            SingleGenerator(modifier, layout, "Rot", 5, 0f, 15f, 3f);

                            break;
                        }
                    case "actorFrameTexture":
                        {
                            DropdownGenerator(modifier, layout, "Camera", 0, CoreHelper.StringToOptionData("Foreground", "Background"));
                            IntegerGenerator(modifier, layout, "Width", 1, 512);
                            IntegerGenerator(modifier, layout, "Height", 2, 512);
                            SingleGenerator(modifier, layout, "Pos X", 3, 0f);
                            SingleGenerator(modifier, layout, "Pos Y", 4, 0f);

                            break;
                        }
                    case "videoPlayer":
                        {
                            StringGenerator(modifier, layout, "Path", 0);
                            SingleGenerator(modifier, layout, "Time Offset", 1, 0f);
                            DropdownGenerator(modifier, layout, "Audio Type", 2, CoreHelper.StringToOptionData("None", "AudioSource", "Direct"));

                            break;
                        }
                    case "languageEquals":
                        {
                            var options = new List<Dropdown.OptionData>();

                            var languages = Enum.GetValues(typeof(Language));

                            for (int i = 0; i < languages.Length; i++)
                                options.Add(new Dropdown.OptionData(Enum.GetName(typeof(Language), i) ?? "Invalid Value"));

                            DropdownGenerator(modifier, layout, "Language", 0, options);

                            break;
                        }
                    case "setIntroFade":
                        {
                            BoolGenerator(modifier, layout, "Should Fade", 0, true);
                            break;
                        }
                    case "endLevel":
                    case "setLevelEndFunc":
                        {
                            var options = CoreHelper.ToOptionData<EndLevelFunction>();
                            options.Insert(0, new Dropdown.OptionData("Default"));
                            DropdownGenerator(modifier, layout, "End Level Function", 0, options);
                            StringGenerator(modifier, layout, "End Level Data", 1);
                            BoolGenerator(modifier, layout, "Save Player Data", 2, true);

                            break;
                        }
                    case "setMusicPlaying":
                        {
                            BoolGenerator(modifier, layout, "Playing", 0, false);
                            break;
                        }

                    #endregion

                    #region Dev Only

                    case "loadSceneDEVONLY":
                        {
                            StringGenerator(modifier, layout, "Scene", 0);
                            if (modifier.commands.Count > 1)
                                BoolGenerator(modifier, layout, "Show Loading", 1, true);

                            break;
                        }
                    case "loadStoryLevelDEVONLY":
                        {
                            IntegerGenerator(modifier, layout, "Chapter", 1, 0);
                            IntegerGenerator(modifier, layout, "Level", 2, 0);
                            BoolGenerator(modifier, layout, "Bonus", 0, false);
                            BoolGenerator(modifier, layout, "Skip Cutscene", 3, false);

                            break;
                        }
                    case "storySaveIntVariableDEVONLY":
                    case "storySaveIntDEVONLY":
                        {
                            StringGenerator(modifier, layout, "Save", 0);
                            if (cmd == "storySaveIntDEVONLY")
                                IntegerGenerator(modifier, layout, "Value", 1, 0);
                            break;
                        }
                    case "storySaveBoolDEVONLY":
                        {
                            StringGenerator(modifier, layout, "Save", 0);
                            BoolGenerator(modifier, layout, "Value", 1, false);
                            break;
                        }
                    case "storyLoadIntEqualsDEVONLY":
                    case "storyLoadIntLesserEqualsDEVONLY":
                    case "storyLoadIntGreaterEqualsDEVONLY":
                    case "storyLoadIntLesserDEVONLY":
                    case "storyLoadIntGreaterDEVONLY":
                        {
                            StringGenerator(modifier, layout, "Load", 0);
                            IntegerGenerator(modifier, layout, "Default", 1, 0);
                            IntegerGenerator(modifier, layout, "Equals", 2, 0);

                            break;
                        }
                    case "storyLoadBoolDEVONLY":
                        {
                            StringGenerator(modifier, layout, "Load", 0);
                            BoolGenerator(modifier, layout, "Default", 1, false);

                            break;
                        }
                    case "enableExampleDEVONLY":
                        {
                            BoolGenerator(modifier, layout, "Active", 0, false);
                            break;
                        }

                        #endregion
                }

                num++;
            }

            // Add Modifier
            {
                var gameObject = modifierAddPrefab.Duplicate(content, "add modifier");
                TooltipHelper.AssignTooltip(gameObject, "Add Modifier");

                var button = gameObject.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(() =>
                {
                    EditorManager.inst.ShowDialog("Default Modifiers Popup");
                    RefreshDefaultModifiersList(beatmapObject);
                });

                EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(gameObject.transform.GetChild(0).GetComponent<Text>());
            }

            // Paste Modifier
            PasteGenerator(beatmapObject);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content.AsRT());

            yield break;
        }

        public void SetObjectColors<T>(Toggle[] toggles, int index, int currentValue, Modifier<T> modifier)
        {
            modifier.SetValue(index, currentValue.ToString());

            try
            {
                modifier.Inactive?.Invoke(modifier);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
            modifier.active = false;

            int num = 0;
            foreach (var toggle in toggles)
            {
                int toggleIndex = num;
                toggle.onValueChanged.ClearAll();
                toggle.isOn = num == currentValue;
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

        #region Generators

        void PrefabGroupOnly(Modifier<BeatmapObject> modifier, Transform layout)
        {
            var prefabInstance = booleanBar.Duplicate(layout, "Prefab");
            prefabInstance.transform.localScale = Vector3.one;
            var prefabInstanceText = prefabInstance.transform.Find("Text").GetComponent<Text>();
            prefabInstanceText.text = "Prefab Group Only";

            var prefabInstanceToggle = prefabInstance.transform.Find("Toggle").GetComponent<Toggle>();
            prefabInstanceToggle.onValueChanged.ClearAll();
            prefabInstanceToggle.isOn = modifier.prefabInstanceOnly;
            prefabInstanceToggle.onValueChanged.AddListener(_val =>
            {
                modifier.prefabInstanceOnly = _val;
                modifier.active = false;
            });

            EditorThemeManager.ApplyLightText(prefabInstanceText);
            EditorThemeManager.ApplyToggle(prefabInstanceToggle);
        }

        GameObject NumberGenerator(Transform layout, string label, string text, Action<string> action, out InputField result)
        {
            var single = numberInput.Duplicate(layout, label);
            single.transform.localScale = Vector3.one;
            var labelText = single.transform.Find("Text").GetComponent<Text>();
            labelText.text = label;

            var inputField = single.transform.Find("Input").GetComponent<InputField>();
            inputField.onValueChanged.ClearAll();
            inputField.textComponent.alignment = TextAnchor.MiddleCenter;
            inputField.text = text;
            inputField.onValueChanged.AddListener(_val => action?.Invoke(_val));

            EditorThemeManager.ApplyLightText(labelText);
            EditorThemeManager.ApplyInputField(inputField);
            var leftButton = single.transform.Find("<").GetComponent<Button>();
            var rightButton = single.transform.Find(">").GetComponent<Button>();
            leftButton.transition = Selectable.Transition.ColorTint;
            rightButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.ApplySelectable(leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.ApplySelectable(rightButton, ThemeGroup.Function_2, false);

            var inputFieldSwapper = inputField.gameObject.AddComponent<InputFieldSwapper>();
            inputFieldSwapper.Init(inputField, InputFieldSwapper.Type.Num);
            result = inputField;
            return single;
        }

        public GameObject SingleGenerator<T>(Modifier<T> modifier, Transform layout, string label, int type, float defaultValue, float amount = 0.1f, float multiply = 10f)
        {
            var single = NumberGenerator(layout, label, modifier.GetFloat(type, defaultValue).ToString(), _val =>
            {
                if (float.TryParse(_val, out float num))
                    modifier.SetValue(type, num.ToString());

                try
                {
                    modifier.Inactive?.Invoke(modifier);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
                modifier.active = false;
            }, out InputField inputField);

            TriggerHelper.IncreaseDecreaseButtons(inputField, amount, multiply, t: single.transform);
            TriggerHelper.AddEventTriggers(inputField.gameObject, TriggerHelper.ScrollDelta(inputField, amount, multiply));

            return single;
        }

        public GameObject IntegerGenerator<T>(Modifier<T> modifier, Transform layout, string label, int type, int defaultValue)
        {
            var single = NumberGenerator(layout, label, modifier.GetInt(type, defaultValue).ToString(), _val =>
            {
                if (int.TryParse(_val, out int num))
                    modifier.SetValue(type, num.ToString());

                try
                {
                    modifier.Inactive?.Invoke(modifier);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
                modifier.active = false;
            }, out InputField inputField);

            TriggerHelper.IncreaseDecreaseButtonsInt(inputField, t: single.transform);
            TriggerHelper.AddEventTriggers(inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputField));

            return single;
        }

        public GameObject BoolGenerator<T>(Modifier<T> modifier, Transform layout, string label, int type, bool defaultValue)
        {
            var global = booleanBar.Duplicate(layout, label);
            global.transform.localScale = Vector3.one;
            var labelText = global.transform.Find("Text").GetComponent<Text>();
            labelText.text = label;

            var globalToggle = global.transform.Find("Toggle").GetComponent<Toggle>();
            globalToggle.onValueChanged.ClearAll();
            globalToggle.isOn = modifier.GetBool(type, defaultValue);
            globalToggle.onValueChanged.AddListener(_val =>
            {
                modifier.SetValue(type, _val.ToString());

                try
                {
                    modifier.Inactive?.Invoke(modifier);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
                modifier.active = false;
            });

            EditorThemeManager.ApplyLightText(labelText);
            EditorThemeManager.ApplyToggle(globalToggle);
            return global;
        }

        public GameObject StringGenerator<T>(Modifier<T> modifier, Transform layout, string label, int type, params ButtonFunction[] buttonFunctions)
        {
            var path = stringInput.Duplicate(layout, label);
            path.transform.localScale = Vector3.one;
            var labelText = path.transform.Find("Text").GetComponent<Text>();
            labelText.text = label;

            var pathInputField = path.transform.Find("Input").GetComponent<InputField>();
            pathInputField.onValueChanged.ClearAll();
            pathInputField.textComponent.alignment = TextAnchor.MiddleLeft;
            pathInputField.text = modifier.GetValue(type);
            pathInputField.onValueChanged.AddListener(_val =>
            {
                modifier.SetValue(type, _val);

                try
                {
                    modifier.Inactive?.Invoke(modifier);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
                modifier.active = false;
            });

            EditorThemeManager.ApplyLightText(labelText);
            EditorThemeManager.ApplyInputField(pathInputField);

            var button = EditorPrefabHolder.Instance.DeleteButton.Duplicate(path.transform, "edit");
            var buttonStorage = button.GetComponent<DeleteButtonStorage>();
            buttonStorage.image.sprite = EditorSprites.EditSprite;
            EditorThemeManager.ApplySelectable(buttonStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(buttonStorage.image, ThemeGroup.Function_2_Text);
            buttonStorage.button.onClick.ClearAll();
            buttonStorage.button.onClick.AddListener(() => TextEditor.inst.SetInputField(pathInputField));
            UIManager.SetRectTransform(buttonStorage.baseImage.rectTransform, new Vector2(120, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 32f));

            return path;
        }

        public GameObject ColorGenerator<T>(Modifier<T> modifier, Transform layout, string label, int type)
        {
            var startColorBase = numberInput.Duplicate(layout, label);
            startColorBase.transform.localScale = Vector3.one;

            var labelText = startColorBase.transform.Find("Text").GetComponent<Text>();
            labelText.text = label;

            Destroy(startColorBase.transform.Find("Input").gameObject);
            Destroy(startColorBase.transform.Find(">").gameObject);
            Destroy(startColorBase.transform.Find("<").gameObject);

            var startColors = ObjEditor.inst.KeyframeDialogs[3].transform.Find("color").gameObject.Duplicate(startColorBase.transform, "color");

            if (startColors.TryGetComponent(out GridLayoutGroup scglg))
            {
                scglg.cellSize = new Vector2(16f, 16f);
                scglg.spacing = new Vector2(4.66f, 2.5f);
            }

            startColors.transform.AsRT().sizeDelta = new Vector2(183f, 32f);

            var toggles = startColors.GetComponentsInChildren<Toggle>();

            foreach (var toggle in toggles)
            {
                EditorThemeManager.ApplyGraphic(toggle.image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(toggle.graphic, ThemeGroup.List_Button_1_Normal);
            }

            EditorThemeManager.ApplyLightText(labelText);
            SetObjectColors(toggles, type, modifier.GetInt(type, 0), modifier);
            return startColorBase;
        }

        public GameObject DropdownGenerator<T>(Modifier<T> modifier, Transform layout, string label, int type, List<string> options) => DropdownGenerator(modifier, layout, label, type, options.Select(x => new Dropdown.OptionData(x)).ToList());

        public GameObject DropdownGenerator<T>(Modifier<T> modifier, Transform layout, string label, int type, List<Dropdown.OptionData> options, List<bool> disabledOptions = null)
        {
            var dd = dropdownBar.Duplicate(layout, label);
            dd.transform.localScale = Vector3.one;
            var labelText = dd.transform.Find("Text").GetComponent<Text>();
            labelText.text = label;

            Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());

            var hideOptions = dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>();
            if (disabledOptions == null)
                Destroy(hideOptions);
            else
            {
                if (!hideOptions)
                    hideOptions = dd.transform.Find("Dropdown").gameObject.AddComponent<HideDropdownOptions>();

                hideOptions.DisabledOptions = disabledOptions;
                hideOptions.remove = true;
            }

            var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
            d.onValueChanged.ClearAll();
            d.options.Clear();

            d.options = options;

            d.value = modifier.GetInt(type, 0);

            d.onValueChanged.AddListener(_val =>
            {
                modifier.SetValue(type, _val.ToString());

                try
                {
                    modifier.Inactive?.Invoke(modifier);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
                modifier.active = false;
            });

            if (d.template)
                d.template.sizeDelta = new Vector2(80f, 192f);

            EditorThemeManager.ApplyLightText(labelText);
            EditorThemeManager.ApplyDropdown(d);
            return dd;
        }

        public GameObject AddGenerator<T>(Modifier<T> modifier, Transform layout, string text, Action onAdd)
        {
            var baseAdd = Creator.NewUIObject("add", layout);
            baseAdd.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(baseAdd.transform, "add");
            var addText = add.transform.GetChild(0).GetComponent<Text>();
            addText.text = text;
            RectValues.Default.AnchoredPosition(-6f, 0f).SizeDelta(300f, 32f).AssignToRectTransform(add.transform.AsRT());

            var addButton = add.GetComponent<Button>();
            addButton.onClick.ClearAll();
            addButton.onClick.AddListener(() => onAdd?.Invoke());

            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text);
            return baseAdd;
        }

        GameObject pasteModifier;
        public void PasteGenerator(BeatmapObject beatmapObject)
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
                beatmapObject.modifiers.Add(Modifier<BeatmapObject>.DeepCopy(copiedModifier, beatmapObject));
                StartCoroutine(RenderModifiers(beatmapObject));
                EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
            });

            EditorThemeManager.ApplyGraphic(buttonStorage.button.image, ThemeGroup.Paste, true);
            EditorThemeManager.ApplyGraphic(buttonStorage.text, ThemeGroup.Paste_Text);
        }

        #endregion

        #region Default Modifiers

        public string searchTerm;
        public int addIndex = -1;
        public void RefreshDefaultModifiersList(BeatmapObject beatmapObject, int addIndex = -1)
        {
            this.addIndex = addIndex;
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

                    TooltipHelper.AssignTooltip(gameObject, $"Object Modifier - {name}", 4f);

                    var modifierName = gameObject.transform.GetChild(0).GetComponent<Text>();
                    modifierName.text = name;

                    var button = gameObject.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(() =>
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
                        if (addIndex == -1)
                            beatmapObject.modifiers.Add(modifier);
                        else
                            beatmapObject.modifiers.Insert(Mathf.Clamp(addIndex, 0, beatmapObject.modifiers.Count), modifier);
                        RTEditor.inst.StartCoroutine(ObjectEditor.inst.RefreshObjectGUI(beatmapObject));
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
            input.transform.AsRT().anchoredPosition = new Vector2(210f, -16f);
            input.transform.AsRT().sizeDelta = new Vector2(120, 32f);
            input.transform.Find("Text").AsRT().sizeDelta = Vector2.zero;

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
