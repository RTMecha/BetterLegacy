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
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
                if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                    RefreshDefaultModifiersList(ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>());
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
            toggle.onValueChanged.ClearAll();
            toggle.isOn = showModifiers;
            toggle.onValueChanged.AddListener(_val =>
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

            duplicate.GetComponent<DeleteButtonStorage>().image.sprite = SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}copy.png");

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
            ignoreToggle.onValueChanged.AddListener(_val => { beatmapObject.ignoreLifespan = _val; });

            if (!showModifiers)
                yield break;

            renderingModifiers = true;

            LSHelpers.DeleteChildren(content);

            content.parent.parent.AsRT().sizeDelta = new Vector2(351f, 300f * Mathf.Clamp(beatmapObject.modifiers.Count, 1, 5));

            int num = 0;
            foreach (var modifier in beatmapObject.modifiers)
            {
                int index = num;
                var name = modifier.commands.Count > 0 ? modifier.commands[0] : "Invalid Modifier";

                var gameObject = modifierCardPrefab.Duplicate(content, name);

                TooltipHelper.AssignTooltip(gameObject, $"Object Modifier - {(modifier.commands[0] + " (" + modifier.type.ToString() + ")")}", 1.5f);

                EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.List_Button_1_Normal, true);

                gameObject.transform.localScale = Vector3.one;
                var modifierTitle = gameObject.transform.Find("Label/Text").GetComponent<Text>();
                modifierTitle.text = name;
                EditorThemeManager.ApplyLightText(modifierTitle);

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
                    continue;
                }

                gameObject.AddComponent<Button>();
                var modifierContextMenu = gameObject.AddComponent<ContextClickable>();
                modifierContextMenu.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    var buttonFunctions = new List<RTEditor.ButtonFunction>()
                    {
                        new RTEditor.ButtonFunction("Add", () =>
                        {
                            EditorManager.inst.ShowDialog("Default Modifiers Popup");
                            RefreshDefaultModifiersList(beatmapObject);
                        }),
                        new RTEditor.ButtonFunction("Copy", () =>
                        {
                            copiedModifier = Modifier<BeatmapObject>.DeepCopy(modifier, beatmapObject);
                            StartCoroutine(RenderModifiers(beatmapObject));
                            EditorManager.inst.DisplayNotification("Copied Modifier!", 1.5f, EditorManager.NotificationType.Success);
                        }),
                        new RTEditor.ButtonFunction("Paste", () =>
                        {
                            if (copiedModifier == null)
                                return;

                            beatmapObject.modifiers.Add(Modifier<BeatmapObject>.DeepCopy(copiedModifier, beatmapObject));
                            StartCoroutine(RenderModifiers(beatmapObject));
                            EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
                        }),
                        new RTEditor.ButtonFunction("Delete", () =>
                        {
                            beatmapObject.modifiers.RemoveAt(index);
                            beatmapObject.reactivePositionOffset = Vector3.zero;
                            beatmapObject.reactiveScaleOffset = Vector3.zero;
                            beatmapObject.reactiveRotationOffset = 0f;
                            Updater.UpdateObject(beatmapObject);
                            StartCoroutine(RenderModifiers(beatmapObject));
                        }),
                        new RTEditor.ButtonFunction(true),
                        new RTEditor.ButtonFunction("Sort Modifiers", () =>
                        {
                            beatmapObject.modifiers = beatmapObject.modifiers.OrderBy(x => x.type == ModifierBase.Type.Action).ToList();
                            StartCoroutine(RenderModifiers(beatmapObject));
                        }),
                        new RTEditor.ButtonFunction("Move Up", () =>
                        {
                            if (index <= 0)
                            {
                                EditorManager.inst.DisplayNotification("Could not move modifier up since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                                return;
                            }

                            beatmapObject.modifiers.Move(index, index - 1);
                            StartCoroutine(RenderModifiers(beatmapObject));
                        }),
                        new RTEditor.ButtonFunction("Move Down", () =>
                        {
                            if (index >= beatmapObject.modifiers.Count - 1)
                            {
                                EditorManager.inst.DisplayNotification("Could not move modifier up since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                                return;
                            }

                            beatmapObject.modifiers.Move(index, index + 1);
                            StartCoroutine(RenderModifiers(beatmapObject));
                        }),
                        new RTEditor.ButtonFunction("Move to Start", () =>
                        {
                            beatmapObject.modifiers.Move(index, 0);
                            StartCoroutine(RenderModifiers(beatmapObject));
                        }),
                        new RTEditor.ButtonFunction("Move to End", () =>
                        {
                            beatmapObject.modifiers.Move(index, beatmapObject.modifiers.Count - 1);
                            StartCoroutine(RenderModifiers(beatmapObject));
                        }),
                        new RTEditor.ButtonFunction(true),
                        new RTEditor.ButtonFunction("Update Modifier", () =>
                        {
                            modifier.active = false;
                            modifier.Inactive?.Invoke(modifier);
                        })
                    };
                    if (ModCompatibility.UnityExplorerInstalled)
                        buttonFunctions.Add(new RTEditor.ButtonFunction("Inspect", () => { ModCompatibility.Inspect(modifier); }));

                    RTEditor.inst.ShowContextMenu(RTEditor.DEFAULT_CONTEXT_MENU_WIDTH, buttonFunctions);
                };

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

                                RTEditor.inst.ShowContextMenu(300f,
                                    new RTEditor.ButtonFunction("Use Local Browser", () =>
                                    {
                                        var isGlobal = modifier.commands.Count > 1 && Parser.TryParse(modifier.commands[1], false);
                                        var directory = isGlobal && RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/soundlibrary") ?
                                                        RTFile.ApplicationDirectory + "beatmaps/soundlibrary" : System.IO.Path.GetDirectoryName(RTFile.BasePath);

                                        if (isGlobal && !RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/soundlibrary"))
                                        {
                                            EditorManager.inst.DisplayNotification("soundlibrary folder does not exist! If you want to have audio take from a global folder, make sure you create a soundlibrary folder inside your beatmaps folder and put your sounds in there.", 12f, EditorManager.NotificationType.Error);
                                            return;
                                        }

                                        var result = Crosstales.FB.FileBrowser.OpenSingleFile("Select a sound to use!", directory, "wav", "ogg", "mp3");
                                        if (string.IsNullOrEmpty(result))
                                            return;

                                        var global = Parser.TryParse(modifier.commands[1], false);

                                        if (result.Replace("\\", "/").Contains(global ? RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/soundlibrary/" : GameManager.inst.basePath.Replace("\\", "/")))
                                        {
                                            str.transform.Find("Input").GetComponent<InputField>().text = result.Replace("\\", "/").Replace(global ? RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/soundlibrary/" : GameManager.inst.basePath.Replace("\\", "/"), "");
                                            EditorManager.inst.HideDialog("Browser Popup");
                                            return;
                                        }

                                        EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                    }),
                                    new RTEditor.ButtonFunction("Use In-game Browser", () =>
                                    {
                                        EditorManager.inst.ShowDialog("Browser Popup");

                                        var isGlobal = modifier.commands.Count > 1 && Parser.TryParse(modifier.commands[1], false);
                                        var directory = isGlobal && RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/soundlibrary") ?
                                                        RTFile.ApplicationDirectory + "beatmaps/soundlibrary" : System.IO.Path.GetDirectoryName(RTFile.BasePath);

                                        if (isGlobal && !RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/soundlibrary"))
                                        {
                                            EditorManager.inst.DisplayNotification("soundlibrary folder does not exist! If you want to have audio take from a global folder, make sure you create a soundlibrary folder inside your beatmaps folder and put your sounds in there.", 12f, EditorManager.NotificationType.Error);
                                            return;
                                        }

                                        RTFileBrowser.inst.UpdateBrowser(directory, new string[] { ".wav", ".ogg", ".mp3" }, onSelectFile: _val =>
                                        {
                                            var global = Parser.TryParse(modifier.commands[1], false);

                                            if (_val.Replace("\\", "/").Contains(global ? RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/soundlibrary/" : GameManager.inst.basePath.Replace("\\", "/")))
                                            {
                                                str.transform.Find("Input").GetComponent<InputField>().text = _val.Replace("\\", "/").Replace(global ? RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/soundlibrary/" : GameManager.inst.basePath.Replace("\\", "/"), "");
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

                            d.options = AudioManager.inst.library.soundGroups.Select(x => new Dropdown.OptionData(x.soundID)).ToList();

                            var soundIndex = AudioManager.inst.library.soundGroups.ToList().FindIndex(x => x.soundID == modifier.value);
                            if (soundIndex >= 0)
                                d.value = soundIndex;

                            d.onValueChanged.AddListener(_val =>
                            {
                                modifier.value = AudioManager.inst.library.soundGroups[_val].soundID;
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
                                RTEditor.inst.ShowContextMenu(300f,
                                    new RTEditor.ButtonFunction("Use Local Browser", () =>
                                    {
                                        var isGlobal = modifier.commands.Count > 1 && Parser.TryParse(modifier.commands[1], false);
                                        var directory = isGlobal && RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/soundlibrary") ?
                                                        RTFile.ApplicationDirectory + "beatmaps/soundlibrary" : System.IO.Path.GetDirectoryName(RTFile.BasePath);

                                        if (isGlobal && !RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/soundlibrary"))
                                        {
                                            EditorManager.inst.DisplayNotification("soundlibrary folder does not exist! If you want to have audio take from a global folder, make sure you create a soundlibrary folder inside your beatmaps folder and put your sounds in there.", 12f, EditorManager.NotificationType.Error);
                                            return;
                                        }

                                        var result = Crosstales.FB.FileBrowser.OpenSingleFile("Select a sound to use!", directory, "wav", "ogg", "mp3");
                                        if (string.IsNullOrEmpty(result))
                                            return;

                                        var global = Parser.TryParse(modifier.commands[1], false);

                                        if (result.Replace("\\", "/").Contains(global ? RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/soundlibrary/" : GameManager.inst.basePath.Replace("\\", "/")))
                                        {
                                            str.transform.Find("Input").GetComponent<InputField>().text = result.Replace("\\", "/").Replace(global ? RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/soundlibrary/" : GameManager.inst.basePath.Replace("\\", "/"), "");
                                            EditorManager.inst.HideDialog("Browser Popup");
                                            return;
                                        }

                                        EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                    }),
                                    new RTEditor.ButtonFunction("Use In-game Browser", () =>
                                    {
                                        EditorManager.inst.ShowDialog("Browser Popup");

                                        var isGlobal = modifier.commands.Count > 1 && Parser.TryParse(modifier.commands[1], false);
                                        var directory = isGlobal && RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/soundlibrary") ?
                                                        RTFile.ApplicationDirectory + "beatmaps/soundlibrary" : System.IO.Path.GetDirectoryName(RTFile.BasePath);

                                        if (isGlobal && !RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/soundlibrary"))
                                        {
                                            EditorManager.inst.DisplayNotification("soundlibrary folder does not exist! If you want to have audio take from a global folder, make sure you create a soundlibrary folder inside your beatmaps folder and put your sounds in there.", 12f, EditorManager.NotificationType.Error);
                                            return;
                                        }

                                        RTFileBrowser.inst.UpdateBrowser(directory, new string[] { ".wav", ".ogg", ".mp3" }, onSelectFile: _val =>
                                        {
                                            var global = Parser.TryParse(modifier.commands[1], false);

                                            if (_val.Replace("\\", "/").Contains(global ? RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/soundlibrary/" : GameManager.inst.basePath.Replace("\\", "/")))
                                            {
                                                str.transform.Find("Input").GetComponent<InputField>().text = _val.Replace("\\", "/").Replace(global ? RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/soundlibrary/" : GameManager.inst.basePath.Replace("\\", "/"), "");
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

                            break;
                        }
                    case "levelUnlocked":
                    case "loadLevelID":
                    case "levelCompletedOther":
                        {
                            StringGenerator(modifier, layout, "ID", 0);

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
                                    d.options.Add(new Dropdown.OptionData(shape, ShapeManager.inst.Shapes2D[type][i].Icon));
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

                            d.onValueChanged.AddListener(_val => { modifier.value = _val.ToString(); });

                            EditorThemeManager.ApplyLightText(labelText);
                            EditorThemeManager.ApplyDropdown(d);

                            break;
                        }

                    case "controlPressDown":
                    case "controlPress":
                    case "controlPressUp":
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

                            var keyCodes = Enum.GetValues(typeof(PlayerInputControlType));

                            for (int i = 0; i < keyCodes.Length; i++)
                            {
                                var str = Enum.GetName(typeof(PlayerInputControlType), i) ?? "Invalid Value";

                                hide.DisabledOptions.Add(string.IsNullOrEmpty(Enum.GetName(typeof(PlayerInputControlType), i)));

                                d.options.Add(new Dropdown.OptionData(str));
                            }

                            d.value = Parser.TryParse(modifier.value, 0);

                            d.onValueChanged.AddListener(_val => { modifier.value = _val.ToString(); });

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
                            if (cmd.Contains("Other"))
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
                                    {
                                        modifier.commands.RemoveAt(groupIndex);
                                    }

                                    Updater.UpdateObject(beatmapObject);
                                    StartCoroutine(RenderModifiers(beatmapObject));
                                });

                                EditorThemeManager.ApplyGraphic(deleteGroupButton.button.image, ThemeGroup.Delete, true);
                                EditorThemeManager.ApplyGraphic(deleteGroupButton.image, ThemeGroup.Delete_Text);

                                StringGenerator(modifier, layout, "Name", i);
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
                    case "languageEquals":
                        {
                            var options = new List<Dropdown.OptionData>();

                            var languages = Enum.GetValues(typeof(Language));

                            for (int i = 0; i < languages.Length; i++)
                                options.Add(new Dropdown.OptionData(Enum.GetName(typeof(Language), i) ?? "Invalid Value"));

                            DropdownGenerator(modifier, layout, "Language", 0, options);

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
            if (copiedModifier != null)
            {
                var gameObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(content, "paste modifier");
                gameObject.transform.AsRT().sizeDelta = new Vector2(350f, 32f);
                var buttonStorage = gameObject.GetComponent<FunctionButtonStorage>();
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

            yield break;
        }

        public void SetObjectColors<T>(Toggle[] toggles, int index, int currentValue, Modifier<T> modifier)
        {
            if (index == 0)
                modifier.value = currentValue.ToString();
            else
                modifier.commands[index] = currentValue.ToString();

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
                toggle.onValueChanged.AddListener(_val => { SetObjectColors(toggles, index, toggleIndex, modifier); });

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

        public GameObject SingleGenerator<T>(Modifier<T> modifier, Transform layout, string label, int type, float defaultValue, float amount = 0.1f, float multiply = 10f)
        {
            var single = numberInput.Duplicate(layout, label);
            single.transform.localScale = Vector3.one;
            var labelText = single.transform.Find("Text").GetComponent<Text>();
            labelText.text = label;

            var inputField = single.transform.Find("Input").GetComponent<InputField>();
            inputField.onValueChanged.ClearAll();
            inputField.textComponent.alignment = TextAnchor.MiddleCenter;
            inputField.text = Parser.TryParse(type == 0 ? modifier.value : modifier.commands[type], defaultValue).ToString();
            inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    if (type == 0)
                        modifier.value = num.ToString();
                    else
                        modifier.commands[type] = num.ToString();
                }

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
            EditorThemeManager.ApplyInputField(inputField);
            var leftButton = single.transform.Find("<").GetComponent<Button>();
            var rightButton = single.transform.Find(">").GetComponent<Button>();
            leftButton.transition = Selectable.Transition.ColorTint;
            rightButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.ApplySelectable(leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.ApplySelectable(rightButton, ThemeGroup.Function_2, false);

            TriggerHelper.IncreaseDecreaseButtons(inputField, amount, multiply, t: single.transform);
            TriggerHelper.AddEventTriggers(inputField.gameObject, TriggerHelper.ScrollDelta(inputField, amount, multiply));

            var inputFieldSwapper = inputField.gameObject.AddComponent<InputFieldSwapper>();
            inputFieldSwapper.Init(inputField, InputFieldSwapper.Type.Num);
            return single;
        }

        public GameObject IntegerGenerator<T>(Modifier<T> modifier, Transform layout, string label, int type, int defaultValue)
        {
            var single = numberInput.Duplicate(layout, label);
            single.transform.localScale = Vector3.one;
            var labelText = single.transform.Find("Text").GetComponent<Text>();
            labelText.text = label;

            var inputField = single.transform.Find("Input").GetComponent<InputField>();
            inputField.onValueChanged.ClearAll();
            inputField.textComponent.alignment = TextAnchor.MiddleCenter;
            inputField.text = Parser.TryParse(type == 0 ? modifier.value : modifier.commands[type], defaultValue).ToString();
            inputField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                {
                    if (type == 0)
                        modifier.value = num.ToString();
                    else
                        modifier.commands[type] = num.ToString();
                }

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
            EditorThemeManager.ApplyInputField(inputField);
            var leftButton = single.transform.Find("<").GetComponent<Button>();
            var rightButton = single.transform.Find(">").GetComponent<Button>();
            leftButton.transition = Selectable.Transition.ColorTint;
            rightButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.ApplySelectable(leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.ApplySelectable(rightButton, ThemeGroup.Function_2, false);

            TriggerHelper.IncreaseDecreaseButtonsInt(inputField, t: single.transform);
            TriggerHelper.AddEventTriggers(inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputField));

            var inputFieldSwapper = inputField.gameObject.AddComponent<InputFieldSwapper>();
            inputFieldSwapper.Init(inputField, InputFieldSwapper.Type.Num);
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
            globalToggle.isOn = Parser.TryParse(type == 0 ? modifier.value : modifier.commands[type], defaultValue);
            globalToggle.onValueChanged.AddListener(_val =>
            {
                if (type == 0)
                    modifier.value = _val.ToString();
                else
                    modifier.commands[type] = _val.ToString();

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

        public GameObject StringGenerator<T>(Modifier<T> modifier, Transform layout, string label, int type, params RTEditor.ButtonFunction[] buttonFunctions)
        {
            var path = stringInput.Duplicate(layout, label);
            path.transform.localScale = Vector3.one;
            var labelText = path.transform.Find("Text").GetComponent<Text>();
            labelText.text = label;

            var pathInputField = path.transform.Find("Input").GetComponent<InputField>();
            pathInputField.onValueChanged.ClearAll();
            pathInputField.textComponent.alignment = TextAnchor.MiddleLeft;
            pathInputField.text = type == 0 ? modifier.value : modifier.commands[type];
            pathInputField.onValueChanged.AddListener(_val =>
            {
                if (type == 0)
                    modifier.value = _val;
                else
                    modifier.commands[type] = _val;

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
            buttonStorage.image.sprite = KeybindManager.inst.editSprite;
            EditorThemeManager.ApplySelectable(buttonStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(buttonStorage.image, ThemeGroup.Function_2_Text);
            buttonStorage.button.onClick.ClearAll();
            buttonStorage.button.onClick.AddListener(() => { TextEditor.inst.SetInputField(pathInputField); });
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
                EditorThemeManager.ApplyGraphic(toggle.image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(toggle.graphic, ThemeGroup.List_Button_1_Normal);
            }

            EditorThemeManager.ApplyLightText(labelText);
            SetObjectColors(startColors.GetComponentsInChildren<Toggle>(), type, Parser.TryParse(type == 0 ? modifier.value : modifier.commands[type], 0), modifier);
            return startColorBase;
        }

        public GameObject DropdownGenerator<T>(Modifier<T> modifier, Transform layout, string label, int type, List<string> options)
        {
            var dd = dropdownBar.Duplicate(layout, label);
            dd.transform.localScale = Vector3.one;
            var labelText = dd.transform.Find("Text").GetComponent<Text>();
            labelText.text = label;

            Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
            Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

            var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
            d.onValueChanged.ClearAll();
            d.options.Clear();

            d.options = options.Select(x => new Dropdown.OptionData(x)).ToList();

            d.value = Parser.TryParse(type == 0 ? modifier.value : modifier.commands[type], 0);

            d.onValueChanged.AddListener(_val =>
            {
                if (type == 0)
                    modifier.value = _val.ToString();
                else
                    modifier.commands[type] = _val.ToString();

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
            EditorThemeManager.ApplyDropdown(d);
            return dd;
        }

        public GameObject DropdownGenerator<T>(Modifier<T> modifier, Transform layout, string label, int type, List<Dropdown.OptionData> options)
        {
            var dd = dropdownBar.Duplicate(layout, label);
            dd.transform.localScale = Vector3.one;
            var labelText = dd.transform.Find("Text").GetComponent<Text>();
            labelText.text = label;

            Destroy(dd.transform.Find("Dropdown").GetComponent<HoverTooltip>());
            Destroy(dd.transform.Find("Dropdown").GetComponent<HideDropdownOptions>());

            var d = dd.transform.Find("Dropdown").GetComponent<Dropdown>();
            d.onValueChanged.ClearAll();
            d.options.Clear();

            d.options = options;

            d.value = Parser.TryParse(type == 0 ? modifier.value : modifier.commands[type], 0);

            d.onValueChanged.AddListener(_val =>
            {
                if (type == 0)
                    modifier.value = _val.ToString();
                else
                    modifier.commands[type] = _val.ToString();

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
            EditorThemeManager.ApplyDropdown(d);
            return dd;
        }

        public GameObject AddGenerator<T>(Modifier<T> modifier, Transform layout, string text, Action onAdd)
        {
            var baseAdd = new GameObject("add");
            baseAdd.transform.SetParent(layout);
            baseAdd.transform.localScale = Vector3.one;

            var baseAddRT = baseAdd.AddComponent<RectTransform>();
            baseAddRT.sizeDelta = new Vector2(0f, 32f);

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(baseAddRT, "add");
            var addText = add.transform.GetChild(0).GetComponent<Text>();
            addText.text = text;
            add.transform.AsRT().anchoredPosition = new Vector2(-6f, 0f);
            add.transform.AsRT().anchorMax = new Vector2(0.5f, 0.5f);
            add.transform.AsRT().anchorMin = new Vector2(0.5f, 0.5f);
            add.transform.AsRT().sizeDelta = new Vector2(300f, 32f);

            var addButton = add.GetComponent<Button>();
            addButton.onClick.ClearAll();
            addButton.onClick.AddListener(() => { onAdd?.Invoke(); });

            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text);
            return baseAdd;
        }

        #endregion

        #region Default Modifiers

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
                    if (name.Contains("DEVONLY"))
                    {
                        continue;
                    }

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

            var input = RTEditor.inst.defaultIF.Duplicate(rectTransform, "Input");
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

            var input = RTEditor.inst.defaultIF.Duplicate(rectTransform, "Input");
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
