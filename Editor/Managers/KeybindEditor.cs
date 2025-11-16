using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Popups;
using BetterLegacy.Editor.Data.Timeline;

using SelectionType = ObjEditor.ObjectSelection.SelectionType;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Editor class that manages custom keybinds.
    /// </summary>
    public class KeybindEditor : BaseManager<KeybindEditor, EditorManagerSettings>
    {
        #region Init

        public override void OnInit()
        {
            try
            {
                keybindFunctions = new List<KeybindFunction>()
                {
                    #region Main

                    new KeybindFunction(nameof(TogglePlayingSong), TogglePlayingSong),
                    new KeybindFunction(nameof(TogglePreview), TogglePreview),
                    new KeybindFunction(nameof(Undo), Undo),
                    new KeybindFunction(nameof(Redo), Redo),
                    new KeybindFunction(nameof(Cut), Cut),
                    new KeybindFunction(nameof(Copy), Copy),
                    new KeybindFunction(nameof(Paste), Paste, new Keybind.Setting("Remove Prefab Instance ID", "False")),
                    new KeybindFunction(nameof(Duplicate), Duplicate, new Keybind.Setting("Remove Prefab Instance ID", "False")),
                    new KeybindFunction(nameof(Delete), Delete),

                    #endregion

                    #region Timeline
                    
                    new KeybindFunction(nameof(SetLayer), SetLayer, new Keybind.Setting("Layer", "0")),
                    new KeybindFunction(nameof(AddLayer), AddLayer, new Keybind.Setting("Layer", "1")),
                    new KeybindFunction(nameof(ToggleEventLayer), ToggleEventLayer),

                    new KeybindFunction(nameof(GoToCurrentTime), GoToCurrentTime),
                    new KeybindFunction(nameof(GoToStart), GoToStart),
                    new KeybindFunction(nameof(GoToEnd), GoToEnd),

                    new KeybindFunction(nameof(ToggleBPMSnap), ToggleBPMSnap),
                    new KeybindFunction(nameof(ForceSnapBPM), ForceSnapBPM),

                    new KeybindFunction(nameof(AddTimelineBin), AddTimelineBin),
                    new KeybindFunction(nameof(RemoveTimelineBin), RemoveTimelineBin),
                    new KeybindFunction(nameof(SetTimelineBin), SetTimelineBin, new Keybind.Setting("Count", "60")),

                    #endregion

                    #region Object

                    new KeybindFunction(nameof(UpdateEverything), UpdateEverything),
                    new KeybindFunction(nameof(UpdateObject), UpdateObject),

                    new KeybindFunction(nameof(SetSongTimeAutokill), SetSongTimeAutokill),

                    new KeybindFunction(nameof(SwapLockSelection), SwapLockSelection),
                    new KeybindFunction(nameof(ToggleLockSelection), ToggleLockSelection),

                    new KeybindFunction(nameof(SwapCollapseSelection), SwapCollapseSelection),
                    new KeybindFunction(nameof(ToggleCollapseSelection), ToggleCollapseSelection),

                    new KeybindFunction(nameof(SetObjectLayer), SetObjectLayer, new Keybind.Setting("Amount", "0")),
                    new KeybindFunction(nameof(AddObjectLayer), AddObjectLayer, new Keybind.Setting("Amount", "1")),

                    new KeybindFunction(nameof(CycleObjectTypeUp), CycleObjectTypeUp),
                    new KeybindFunction(nameof(CycleObjectTypeDown), CycleObjectTypeDown),

                    new KeybindFunction(nameof(SelectNextObject), SelectNextObject),
                    new KeybindFunction(nameof(SelectPreviousObject), SelectPreviousObject),

                    new KeybindFunction(nameof(HideSelection), HideSelection),
                    new KeybindFunction(nameof(UnhideHiddenObjects), UnhideHiddenObjects),
                    new KeybindFunction(nameof(ToggleHideSelection), ToggleHideSelection),

                    new KeybindFunction(nameof(ToggleObjectDragging), ToggleObjectDragging),
                    new KeybindFunction(nameof(ToggleObjectDragHelper), ToggleObjectDragHelper),
                    new KeybindFunction(nameof(SetObjectDragHelperAxisX), SetObjectDragHelperAxisX),
                    new KeybindFunction(nameof(SetObjectDragHelperAxisY), SetObjectDragHelperAxisY),

                    new KeybindFunction(nameof(TransformPosition), TransformPosition, new Keybind.Setting("Create Keyframe", "True"), new Keybind.Setting("Use Nearest", "True"), new Keybind.Setting("Use Previous", "False")),
                    new KeybindFunction(nameof(TransformScale), TransformScale, new Keybind.Setting("Create Keyframe", "True"), new Keybind.Setting("Use Nearest", "True"), new Keybind.Setting("Use Previous", "False")),
                    new KeybindFunction(nameof(TransformRotation), TransformRotation, new Keybind.Setting("Create Keyframe", "True"), new Keybind.Setting("Use Nearest", "True"), new Keybind.Setting("Use Previous", "False")),
                    new KeybindFunction(nameof(FinishTransform), FinishTransform, new Keybind.Setting("Cancel", "False")),

                    new KeybindFunction(nameof(ParentPicker), ParentPicker),

                    new KeybindFunction(nameof(ResetIntegerVariables), ResetIntegerVariables),

                    #endregion

                    #region Prefab

                    new KeybindFunction(nameof(OpenPrefabCreator), OpenPrefabCreator, new Keybind.Setting("External", "True")),
                    new KeybindFunction(nameof(CollapsePrefab), CollapsePrefab),
                    new KeybindFunction(nameof(ExpandPrefab), ExpandPrefab),
                    new KeybindFunction(nameof(SpawnPrefab), SpawnPrefab, new Keybind.Setting("Search Prefab Using", "0"), new Keybind.Setting("Prefab Reference", string.Empty)),
                    new KeybindFunction(nameof(SpawnSelectedQuickPrefab), SpawnSelectedQuickPrefab),

                    #endregion

                    #region Marker
                    
                    new KeybindFunction(nameof(CreateNewMarker), CreateNewMarker),
                    new KeybindFunction(nameof(JumpToNextMarker), JumpToNextMarker),
                    new KeybindFunction(nameof(JumpToPreviousMarker), JumpToPreviousMarker),

                    #endregion

                    #region Save / Load
                    
                    new KeybindFunction(nameof(SaveLevel), SaveLevel),
                    new KeybindFunction(nameof(OpenLevelPopup), OpenLevelPopup),
                    new KeybindFunction(nameof(SaveLevelCopy), SaveLevelCopy),
                    new KeybindFunction(nameof(CreateNewLevel), CreateNewLevel),

                    #endregion

                    #region Keyframe
                    
                    new KeybindFunction(nameof(SetFirstKeyframeInType), SetFirstKeyframeInType),
                    new KeybindFunction(nameof(SetLastKeyframeInType), SetLastKeyframeInType),
                    new KeybindFunction(nameof(SetNextKeyframeInType), SetNextKeyframeInType),
                    new KeybindFunction(nameof(SetPreviousKeyframeInType), SetPreviousKeyframeInType),
                    new KeybindFunction(nameof(IncreaseKeyframeValue), IncreaseKeyframeValue, new Keybind.Setting("Type", "0"), new Keybind.Setting("Index", "0"), new Keybind.Setting("Value Index", "0"), new Keybind.Setting("Amount", "0")),
                    new KeybindFunction(nameof(DecreaseKeyframeValue), DecreaseKeyframeValue, new Keybind.Setting("Type", "0"), new Keybind.Setting("Index", "0"), new Keybind.Setting("Value Index", "0"), new Keybind.Setting("Amount", "0")),
                    new KeybindFunction(nameof(SetKeyframeValue), SetKeyframeValue, new Keybind.Setting("Type", "0"), new Keybind.Setting("Index", "0"), new Keybind.Setting("Value Index", "0"), new Keybind.Setting("Amount", "0")),

                    #endregion

                    #region Game
                    
                    new KeybindFunction(nameof(ToggleZenMode), ToggleZenMode),
                    new KeybindFunction(nameof(CycleGameMode), CycleGameMode),
                    new KeybindFunction(nameof(AddPitch), AddPitch, new Keybind.Setting("Pitch", "0.1")),
                    new KeybindFunction(nameof(SetPitch), SetPitch, new Keybind.Setting("Pitch", "1")),
                    new KeybindFunction(nameof(UpdateSeed), UpdateSeed),

                    #endregion

                    #region Info

                    new KeybindFunction(nameof(ToggleShowHelp), ToggleShowHelp),
                    new KeybindFunction(nameof(ToggleMouseTooltip), ToggleMouseTooltip),

                    #endregion

                    new KeybindFunction(nameof(ToggleProjectPlanner), ToggleProjectPlanner),
                    new KeybindFunction(nameof(StopProjectPlannerOST), StopProjectPlannerOST),
                    new KeybindFunction(nameof(NextProjectPlannerOST), NextProjectPlannerOST),
                    new KeybindFunction(nameof(TogglePlayingProjectPlannerOST), TogglePlayingProjectPlannerOST),
                    new KeybindFunction(nameof(ShuffleProjectPlannerOST), ShuffleProjectPlannerOST),
                    new KeybindFunction(nameof(StartProjectPlannerOST), StartProjectPlannerOST),
                    new KeybindFunction(nameof(SwitchKeybindProfile), SwitchKeybindProfile, new Keybind.Setting("Profile ID", string.Empty)),
                };

                Load();

                Dialog = new KeybindEditorDialog();
                Dialog.Init();

                // clear cached values when the editor closes.
                Dialog.GameObject.AddComponent<ActiveState>().onStateChanged = enabled =>
                {
                    if (enabled)
                        return;

                    RTEditor.inst.selectingKey = false;
                    RTEditor.inst.setKey = null;

                    if (FunctionPopup.IsOpen)
                        FunctionPopup.Close();
                };

                Popup = RTEditor.inst.GeneratePopup(EditorPopup.KEYBIND_LIST_POPUP, "Edit a Keybind", Vector2.zero, new Vector2(600f, 400f),
                    refreshSearch: _val => RenderPopup(), placeholderText: "Search for keybind...");

                FunctionPopup = RTEditor.inst.GeneratePopup(EditorPopup.KEYBIND_LIST_POPUP, "Select a Function", Vector2.zero, new Vector2(300f, 400f),
                    refreshSearch: _val => RenderFunctionPopup(), placeholderText: "Search for function...");

                var reload = EditorPrefabHolder.Instance.SpriteButton.Duplicate(Popup.TopPanel, "Reload");
                RectValues.TopRightAnchored.AnchoredPosition(-64f, 0f).SizeDelta(32f, 32f).AssignToRectTransform(reload.transform.AsRT());
                var reloadButton = reload.GetComponent<Button>();
                reloadButton.onClick.NewListener(Load);

                reloadButton.image.sprite = EditorSprites.ReloadSprite;
                EditorThemeManager.AddSelectable(reloadButton, ThemeGroup.Function_2, false);

                // Key Prefab
                {
                    keyPrefab = Creator.NewUIObject("Key", transform);
                    keyPrefab.transform.AsRT().sizeDelta = new Vector2(400f, 32f);
                    var image = keyPrefab.AddComponent<Image>();
                    image.color = new Color(0.2f, 0.2f, 0.2f);

                    var horizontalLayoutGroup = keyPrefab.AddComponent<HorizontalLayoutGroup>();
                    horizontalLayoutGroup.childControlWidth = false;
                    horizontalLayoutGroup.childForceExpandWidth = false;
                    horizontalLayoutGroup.spacing = 4;

                    var keyTypeDropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(keyPrefab.transform, "Key Type");

                    keyTypeDropdown.transform.AsRT().sizeDelta = new Vector2(220f, 32f);

                    var keyTypeDropdownDD = keyTypeDropdown.GetComponent<Dropdown>();
                    keyTypeDropdownDD.onValueChanged.ClearAll();
                    keyTypeDropdownDD.options = Enum.GetNames(typeof(Keybind.Key.Type)).Select(x => new Dropdown.OptionData(x)).ToList();
                    keyTypeDropdownDD.value = 0;

                    var watchKey = EditorPrefabHolder.Instance.Function1Button.Duplicate(keyPrefab.transform, "Key Watcher");
                    var text = watchKey.transform.GetChild(0).GetComponent<Text>();
                    text.text = "Set Key";
                    watchKey.transform.AsRT().sizeDelta = new Vector2(140f, 32f);

                    var keyCodeDropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(keyPrefab.transform, "Key Code");

                    keyCodeDropdown.transform.AsRT().sizeDelta = new Vector2(360f, 32f);

                    var keyCodeDropdownDD = keyCodeDropdown.GetComponent<Dropdown>();
                    keyCodeDropdownDD.onValueChanged.ClearAll();
                    keyCodeDropdownDD.value = 0;
                    keyCodeDropdownDD.options.Clear();

                    var hide = keyCodeDropdown.GetComponent<HideDropdownOptions>();
                    hide.DisabledOptions.Clear();
                    hide.remove = true;
                    var keyCodes = Enum.GetValues(typeof(KeyCode));
                    for (int i = 0; i < keyCodes.Length; i++)
                    {
                        var str = Enum.GetName(typeof(KeyCode), i) ?? "Invalid Value";

                        hide.DisabledOptions.Add(string.IsNullOrEmpty(Enum.GetName(typeof(KeyCode), i)));

                        keyCodeDropdownDD.options.Add(new Dropdown.OptionData(str));
                    }

                    var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(keyPrefab.transform, "Delete");
                    delete.transform.AsRT().anchoredPosition = new Vector2(744f, -16f);
                }

                EditorHelper.AddEditorDropdown("Edit Keybinds", string.Empty, "Edit", SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/keybind{FileFormat.PNG.Dot()}")), OpenPopup);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        public override void OnTick()
        {
            if (!CurrentProfile)
                return;

            if (!CoreHelper.IsUsingInputField && EditorManager.inst.isEditing && Application.isFocused && (EditorConfig.Instance.AllowEditorKeybindsWithEditorCam.Value || !RTEditor.inst.Freecam))
            {
                var keybinds = CurrentProfile.keybinds;
                foreach (var keybind in keybinds)
                {
                    if (EditorManager.inst.editorState != EditorManager.EditorState.Main && keybind.Name != nameof(ToggleProjectPlanner) && keybind.Name != nameof(SaveLevel) ||
                        !EditorManager.inst.hasLoadedLevel && keybind.Name != nameof(ToggleShowHelp) && keybind.Name != nameof(TogglePlayingSong) &&
                        keybind.Name != nameof(OpenLevelPopup) && keybind.Name != nameof(SaveLevel) && keybind.Name != nameof(ToggleProjectPlanner))
                        continue;

                    if (dragging && keybind.Name != nameof(FinishTransform))
                        continue;

                    var active = keybind.Check() && !isPressingKey;
                    isPressingKey = active;
                    if (!active)
                        continue;

                    keybind.Activate();
                    lastKeybind = keybind;
                }
            }

            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                EndDrag(Input.GetMouseButtonDown(1));

            UpdateValues();

            if (!dragging)
                return;

            if (selectionType == SelectionType.Object)
                ObjectEditor.inst.Dialog.Timeline.RenderDialog(beatmapObject);
            if (selectionType == SelectionType.Prefab)
                RTPrefabEditor.inst.RenderPrefabObjectTransforms(prefabObject);
        }

        #endregion

        #region Values

        /// <summary>
        /// Dialog of the editor.
        /// </summary>
        public KeybindEditorDialog Dialog { get; set; }

        /// <summary>
        /// Popup list of the editor.
        /// </summary>
        public ContentPopup Popup { get; set; }

        /// <summary>
        /// Keybind Function popup list of the editor.
        /// </summary>
        public ContentPopup FunctionPopup { get; set; }

        GameObject keyPrefab;

        /// <summary>
        /// Currently selected keybind profile.
        /// </summary>
        public KeybindProfile CurrentProfile { get; set; }

        /// <summary>
        /// List of loaded keybind profiles.
        /// </summary>
        public List<KeybindProfile> profiles = new List<KeybindProfile>();

        /// <summary>
        /// Currently selected keybind.
        /// </summary>
        public Keybind CurrentKeybind { get; set; }

        /// <summary>
        /// Folder where keybinds are stored.
        /// </summary>
        public string KeybindsFolder => RTFile.CombinePaths(RTFile.ApplicationDirectory, "settings/keybinds");

        /// <summary>
        /// File where main keybind settings are stored.
        /// </summary>
        public string KeybindEditorSettings => RTFile.CombinePaths(RTFile.ApplicationDirectory, "settings/keybind_settings" + FileFormat.LSS.Dot());

        /// <summary>
        /// ID of the currently selected profile.
        /// </summary>
        public string currentProfileID;

        /// <summary>
        /// If a keybind is active.
        /// </summary>
        public bool isPressingKey;

        /// <summary>
        /// The last activated keybind.
        /// </summary>
        public Keybind lastKeybind;

        #region Transform

        public bool createKeyframe = true;
        public bool useNearest = true;
        public bool usePrevious = false;

        public int currentType;

        public bool dragging;

        public BeatmapObject beatmapObject;
        public PrefabObject prefabObject;

        public bool setKeyframeValues;
        public Vector2 dragKeyframeValues;
        public EventKeyframe selectedKeyframe;
        public float[] originalValues;
        public Vector2 dragOffset;
        float dragOffsetFloat;
        float dragKeyframeValuesFloat;
        public SelectObject.Axis firstDirection = SelectObject.Axis.Static;

        public SelectionType selectionType;

        #endregion

        #endregion

        #region Functions

        /// <summary>
        /// Creates the default profile.
        /// </summary>
        public void FirstInit()
        {
            profiles.Clear();
            RTFile.CreateDirectory(KeybindsFolder);
            var profile = KeybindProfile.DefaultProfile;
            profiles.Add(profile);
            CurrentProfile = profile;
            currentProfileID = profile.id;
            Save();
        }

        /// <summary>
        /// Saves all loaded keybind profiles to the keybinds folder.
        /// </summary>
        public void Save()
        {
            for (int i = 0; i < profiles.Count; i++)
            {
                var profile = profiles[i];
                profile.WriteToFile(RTFile.CombinePaths(KeybindsFolder, profile.GetFileName()));
            }

            var jn = Parser.NewJSONObject();
            jn["current_profile_id"] = currentProfileID ?? string.Empty;
            RTFile.WriteToFile(KeybindEditorSettings, jn.ToString(3));
        }

        /// <summary>
        /// Loads the keybind profiles from the keybinds folder.
        /// </summary>
        public void Load()
        {
            if (Dialog && Dialog.IsCurrent)
                Dialog.Close();
            if (FunctionPopup && FunctionPopup.IsOpen)
                FunctionPopup.Close();

            profiles.Clear();

            if (!RTFile.DirectoryExists(KeybindsFolder))
            {
                FirstInit();
                return;
            }

            var files = Directory.GetFiles(KeybindsFolder);
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var format = RTFile.GetFileFormat(file);
                if (format != FileFormat.LSS)
                    continue;

                try
                {
                    profiles.Add(RTFile.CreateFromFile<KeybindProfile>(file));
                }
                catch (Exception ex)
                {
                    CoreHelper.LogError($"Failed to load keybind profile due to the exception: {ex}");
                }
            }

            if (RTFile.FileExists(KeybindEditorSettings))
            {
                var file = RTFile.ReadFromFile(KeybindEditorSettings);
                var jn = JSON.Parse(file);
                currentProfileID = jn["current_profile_id"];
            }

            if (!string.IsNullOrEmpty(currentProfileID))
                CurrentProfile = profiles.TryFind(x => x.id == currentProfileID, out KeybindProfile currentProfile) ? currentProfile : KeybindProfile.DefaultProfile;
            else
                CurrentProfile = profiles.TryGetAt(0, out KeybindProfile currentProfile) ? currentProfile : KeybindProfile.DefaultProfile;
        }

        /// <summary>
        /// Displays the no profile selected error notification.
        /// </summary>
        public void NoProfileSelectedError() => EditorManager.inst.DisplayNotification($"No keybind profile selected!", 2f, EditorManager.NotificationType.Error);

        #region Editor

        /// <summary>
        /// Opens the editor dialog and sets the keybind to edit.
        /// </summary>
        /// <param name="keybind">Keybind to edit.</param>
        public void OpenDialog(Keybind keybind)
        {
            Dialog.Open();
            RenderDialog(keybind);
        }

        /// <summary>
        /// Renders the editor dialog and sets the keybind to edit.
        /// </summary>
        /// <param name="keybind">Keybind to edit.</param>
        public void RenderDialog(Keybind keybind)
        {
            CurrentKeybind = keybind;

            Dialog.SelectActionButton.Text = keybind.Name + " (Click to select a function)";
            Dialog.SelectActionButton.OnClick.NewListener(OpenFunctionPopup);

            Dialog.ClearKeys();
            
            var add = EditorPrefabHolder.Instance.CreateAddButton(Dialog.KeysContent, "Add Key");
            add.Text = "Add new Key";
            add.OnClick.NewListener(() =>
            {
                var key = new Keybind.Key(Keybind.Key.Type.Down, KeyCode.None);
                keybind.keys.Add(key);
                RenderPopupIfOpen();
                RenderDialog(keybind);
                Save();
            });

            int num = 0;
            foreach (var key in keybind.keys)
            {
                int index = num;
                var gameObject = keyPrefab.Duplicate(Dialog.KeysContent, "Key");
                var type = gameObject.transform.Find("Key Type").GetComponent<Dropdown>();
                var watch = gameObject.transform.Find("Key Watcher").GetComponent<FunctionButtonStorage>();
                var code = gameObject.transform.Find("Key Code").GetComponent<Dropdown>();

                EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.List_Button_1_Normal, true);

                var text = gameObject.transform.Find("Key Watcher").GetChild(0).GetComponent<Text>();
                text.text = "Set Key";

                type.SetValueWithoutNotify((int)key.InteractType);
                type.onValueChanged.NewListener(_val =>
                {
                    key.InteractType = (Keybind.Key.Type)_val;
                    RenderPopupIfOpen();
                    Save();
                    text.text = "Set Key";
                });

                EditorThemeManager.ApplyDropdown(type);

                watch.OnClick.NewListener(() =>
                {
                    RTEditor.inst.selectingKey = true;
                    RTEditor.inst.setKey = keyCode =>
                    {
                        key.KeyCode = keyCode;
                        RenderPopupIfOpen();
                        Save();
                        text.text = "Set Key";

                        code.SetValueWithoutNotify((int)key.KeyCode);
                        code.onValueChanged.NewListener(_val =>
                        {
                            key.KeyCode = (KeyCode)_val;
                            RenderPopupIfOpen();
                            Save();
                            text.text = "Set Key";
                        });
                    };

                    text.text = "Watching Key";
                });

                EditorThemeManager.ApplyGraphic(watch.button.image, ThemeGroup.Function_1, true);
                EditorThemeManager.ApplyGraphic(watch.label, ThemeGroup.Function_1_Text);

                code.SetValueWithoutNotify((int)key.KeyCode);
                code.onValueChanged.NewListener(_val =>
                {
                    key.KeyCode = (KeyCode)_val;
                    RenderPopupIfOpen();
                    Save();
                    text.text = "Set Key";
                });

                EditorThemeManager.ApplyDropdown(code);

                var delete = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                delete.OnClick.NewListener(() =>
                {
                    keybind.keys.RemoveAt(index);
                    RenderPopupIfOpen();
                    RenderDialog(keybind);
                    Save();
                });

                EditorThemeManager.ApplyDeleteButton(delete);

                num++;
            }

            Dialog.ClearSettings();

            num = 0;
            foreach (var setting in keybind.settings)
            {
                int index = num;

                var key = setting.key;
                switch (key.ToLower())
                {
                    case "cancel":
                    case "external":
                    case "useid":
                    case "remove prefab instance id":
                    case "create keyframe":
                    case "use nearest":
                    case "use previous": {
                            var bar = Creator.NewUIObject("input", Dialog.SettingsContent);
                            bar.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

                            var layout = bar.AddComponent<HorizontalLayoutGroup>();
                            layout.childControlHeight = false;
                            layout.childControlWidth = false;
                            layout.childForceExpandHeight = true;
                            layout.childForceExpandWidth = false;
                            layout.spacing = 8f;

                            bar.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

                            TooltipHelper.AddHoverTooltip(bar, setting.key, string.Empty);

                            var labels = EditorPrefabHolder.Instance.Labels.Duplicate(bar.transform, "label", 0);
                            var labelText = labels.transform.GetChild(0).GetComponent<Text>();
                            labelText.text = setting.key;
                            labels.transform.AsRT().sizeDelta = new Vector2(688f, 32f);

                            var toggle = EditorPrefabHolder.Instance.Toggle.Duplicate(bar.transform).GetComponent<Toggle>();
                            toggle.SetIsOnWithoutNotify(Parser.TryParse(setting.value, false));
                            toggle.onValueChanged.NewListener(_val =>
                            {
                                setting.value = _val.ToString();
                                Save();
                            });

                            EditorThemeManager.ApplyToggle(toggle);

                            break;
                        }

                    case "dialog":
                    case "profile id":
                    case "id": {
                            var bar = Creator.NewUIObject("input", Dialog.SettingsContent);
                            bar.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

                            var layout = bar.AddComponent<HorizontalLayoutGroup>();
                            layout.childControlHeight = false;
                            layout.childControlWidth = false;
                            layout.childForceExpandHeight = true;
                            layout.childForceExpandWidth = false;
                            layout.spacing = 8f;

                            bar.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

                            TooltipHelper.AddHoverTooltip(bar, setting.key, string.Empty);

                            var labels = EditorPrefabHolder.Instance.Labels.Duplicate(bar.transform, "label", 0);
                            var labelText = labels.transform.GetChild(0).GetComponent<Text>();
                            labelText.text = setting.key;
                            labels.transform.AsRT().sizeDelta = new Vector2(354f, 20f);

                            var inputField = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(bar.transform).GetComponent<InputField>();
                            Destroy(inputField.GetComponent<HoverTooltip>());
                            inputField.transform.AsRT().sizeDelta = new Vector2(366f, 32f);
                            inputField.characterValidation = InputField.CharacterValidation.None;
                            inputField.characterLimit = 0;
                            inputField.textComponent.fontSize = 18;
                            inputField.SetTextWithoutNotify(setting.value);
                            inputField.onValueChanged.NewListener(_val => setting.value = _val);
                            inputField.onEndEdit.NewListener(_val => Save());

                            EditorThemeManager.ApplyInputField(inputField, ThemeGroup.Input_Field);

                            break;
                        }

                    case "type":
                    case "index":
                    case "value":
                    case "value index":
                    case "layer":
                    case "amount":
                    case "count": {
                            var gameObject = EditorPrefabHolder.Instance.NumberInputField.Duplicate(Dialog.SettingsContent, "input");
                            var inputFieldStorage = gameObject.GetComponent<InputFieldStorage>();

                            var labels = EditorPrefabHolder.Instance.Labels.Duplicate(gameObject.transform, "label", 0);
                            var labelText = labels.transform.GetChild(0).GetComponent<Text>();
                            labelText.text = setting.key;
                            labels.transform.AsRT().sizeDelta = new Vector2(541f, 32f);

                            gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

                            TooltipHelper.AddHoverTooltip(gameObject, setting.key, string.Empty);

                            inputFieldStorage.inputField.characterValidation = InputField.CharacterValidation.None;
                            inputFieldStorage.inputField.SetTextWithoutNotify(Parser.TryParse(setting.value, 0f).ToString());
                            inputFieldStorage.inputField.onValueChanged.NewListener(_val =>
                            {
                                switch (keybind.Name)
                                {
                                    case nameof(SetPitch):
                                    case nameof(AddPitch):
                                    case nameof(IncreaseKeyframeValue):
                                    case nameof(DecreaseKeyframeValue):
                                    case nameof(SetKeyframeValue): {
                                            if (float.TryParse(_val, out float result))
                                                setting.value = result.ToString();
                                            break;
                                        }
                                    case nameof(SetLayer):
                                    case nameof(AddLayer):
                                    case nameof(SetTimelineBin):
                                    case nameof(AddObjectLayer):
                                    case nameof(SetObjectLayer): {
                                            if (int.TryParse(_val, out int result))
                                                setting.value = result.ToString();
                                            break;
                                        }
                                }
                            });
                            inputFieldStorage.inputField.onEndEdit.NewListener(_val => Save());

                            Destroy(inputFieldStorage.rightGreaterButton.gameObject);
                            Destroy(inputFieldStorage.middleButton.gameObject);
                            Destroy(inputFieldStorage.leftGreaterButton.gameObject);

                            TriggerHelper.IncreaseDecreaseButtonsInt(inputFieldStorage);
                            TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputFieldStorage.inputField));

                            EditorThemeManager.ApplyInputField(inputFieldStorage);

                            break;
                        }
                    case "search prefab using": {
                            var bar = Creator.NewUIObject("input", Dialog.SettingsContent);
                            bar.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

                            var layout = bar.AddComponent<HorizontalLayoutGroup>();
                            layout.childControlHeight = false;
                            layout.childControlWidth = false;
                            layout.childForceExpandHeight = true;
                            layout.childForceExpandWidth = false;
                            layout.spacing = 8f;

                            bar.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);

                            TooltipHelper.AddHoverTooltip(bar, setting.key, string.Empty);

                            var labels = EditorPrefabHolder.Instance.Labels.Duplicate(bar.transform, "label", 0);
                            var labelText = labels.transform.GetChild(0).GetComponent<Text>();
                            labelText.text = setting.key;
                            labels.transform.AsRT().sizeDelta = new Vector2(354f, 20f);

                            var dropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(bar.transform).GetComponent<Dropdown>();
                            dropdown.options = CoreHelper.StringToOptionData("Index", "Name", "ID");
                            dropdown.SetValueWithoutNotify(Parser.TryParse(setting.value, 0));
                            dropdown.onValueChanged.NewListener(_val => setting.value = _val.ToString());

                            EditorThemeManager.ApplyDropdown(dropdown);

                            break;
                        }
                }

                num++;
            }
        }

        #endregion

        #region List

        /// <summary>
        /// Opens the keybind list popup.
        /// </summary>
        public void OpenPopup()
        {
            Popup.Open();
            RenderPopup();
        }

        /// <summary>
        /// Renders the keybind list popup if it's currently open.
        /// </summary>
        public void RenderPopupIfOpen()
        {
            if (Popup.IsOpen)
                RenderPopup();
        }

        /// <summary>
        /// Renders the keybind list popup.
        /// </summary>
        public void RenderPopup()
        {
            if (!CurrentProfile)
            {
                RenderProfilePopup();
                return;
            }

            Popup.ClearContent();

            var select = EditorManager.inst.folderButtonPrefab.Duplicate(Popup.Content, "select").GetComponent<FunctionButtonStorage>();
            select.Text = "Select Profile";
            select.OnClick.NewListener(() =>
            {
                CurrentProfile = null;
                currentProfileID = null;
                RenderPopup();
                Save();
            });
            EditorThemeManager.ApplySelectable(select.button, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(select.label);

            var add = EditorPrefabHolder.Instance.CreateAddButton(Popup.Content);
            add.Text = "Add new Keybind";
            add.OnClick.NewListener(() =>
            {
                if (!CurrentProfile)
                {
                    NoProfileSelectedError();
                    return;
                }

                var keybind = new Keybind(nameof(TogglePlayingSong), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Alpha0));
                CurrentProfile.keybinds.Add(keybind);
                RenderPopup();
                OpenDialog(keybind);
            });

            int num = 0;
            foreach (var keybind in CurrentProfile.keybinds)
            {
                int index = num;

                var name = keybind.Name;

                if (!RTString.SearchString(Popup.SearchTerm, name))
                {
                    num++;
                    continue;
                }

                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(Popup.Content, name);
                var storage = gameObject.GetComponent<FunctionButtonStorage>();
                if (keybind.keys != null && keybind.keys.Count > 0)
                {
                    name += " [";
                    for (int i = 0; i < keybind.keys.Count; i++)
                    {
                        name += $"{keybind.keys[i].InteractType}: {keybind.keys[i].KeyCode}";
                        if (i != keybind.keys.Count - 1)
                            name += ", ";
                    }
                    name += "]";
                }
                if (keybind.settings != null && keybind.settings.Count > 0)
                {
                    name += " (";
                    for (int i = 0; i < keybind.settings.Count; i++)
                    {
                        var setting = keybind.settings[i];
                        name += $"{setting.key}: {setting.value}";
                        if (i != keybind.settings.Count - 1)
                            name += ", ";
                    }
                    name += ")";
                }
                storage.Text = name;
                storage.OnClick.ClearAll();

                var contextClickable = gameObject.GetOrAddComponent<ContextClickable>();
                contextClickable.onClick = pointerEventData =>
                {
                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                    {
                        var buttonFunctions = new List<ButtonFunction>()
                        {
                            new ButtonFunction("Edit", () => OpenDialog(keybind)),
                            new ButtonFunction("Delete", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this keybind? You cannot undo this.", () =>
                            {
                                if (!CurrentProfile)
                                {
                                    NoProfileSelectedError();
                                    return;
                                }

                                CurrentProfile.keybinds.RemoveAt(index);
                                RenderPopup();
                                Save();
                                RTEditor.inst.HideWarningPopup();
                            }, RTEditor.inst.HideWarningPopup)),
                            new ButtonFunction(true),
                        };
                        buttonFunctions.AddRange(EditorContextMenu.GetMoveIndexFunctions(CurrentProfile.keybinds, index, () =>
                        {
                            RenderPopup();
                            Save();
                        }));
                        EditorContextMenu.inst.ShowContextMenu(buttonFunctions);
                        return;
                    }
                    OpenDialog(keybind);
                };

                var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(gameObject.transform, "Delete").GetComponent<DeleteButtonStorage>();
                RectValues.LeftAnchored.AnchoredPosition(580f, 0f).Pivot(1f, 1f).SizeDelta(32f, 32f).AssignToRectTransform(delete.transform.AsRT());
                delete.OnClick.NewListener(() => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this keybind? You cannot undo this.", () =>
                {
                    if (!CurrentProfile)
                    {
                        NoProfileSelectedError();
                        return;
                    }

                    CurrentProfile.keybinds.RemoveAt(index);
                    Dialog.Close();
                    RenderPopup();
                    Save();
                    RTEditor.inst.HideWarningPopup();
                }, RTEditor.inst.HideWarningPopup));

                EditorThemeManager.ApplySelectable(storage.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(storage.label);
                EditorThemeManager.ApplyDeleteButton(delete);

                num++;
            }
        }

        /// <summary>
        /// Renders the keybind profile list popup.
        /// </summary>
        public void RenderProfilePopup()
        {
            Popup.ClearContent();

            var add = EditorPrefabHolder.Instance.CreateAddButton(Popup.Content);
            add.Text = "Add new Profile";
            add.OnClick.ClearAll();
            var addContextClickable = add.gameObject.GetOrAddComponent<ContextClickable>();
            addContextClickable.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                {
                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Create Default", () => RTEditor.inst.ShowNameEditor("Keybind Profile Creator", "Profile Name", "Create", () =>
                        {
                            var name = RTEditor.inst.folderCreatorName.text;
                            if (string.IsNullOrEmpty(name))
                            {
                                EditorManager.inst.DisplayNotification($"Please set a name!", 2f, EditorManager.NotificationType.Error);
                                return;
                            }

                            CreateProfile(name, () => KeybindProfile.DefaultProfile);
                            RTEditor.inst.HideNameEditor();
                        })),
                        new ButtonFunction("Create Empty", () => RTEditor.inst.ShowNameEditor("Keybind Profile Creator", "Profile Name", "Create", () =>
                        {
                            var name = RTEditor.inst.folderCreatorName.text;
                            if (string.IsNullOrEmpty(name))
                            {
                                EditorManager.inst.DisplayNotification($"Please set a name!", 2f, EditorManager.NotificationType.Error);
                                return;
                            }

                            CreateProfile(name, () => new KeybindProfile("Default"));
                            RTEditor.inst.HideNameEditor();
                        })));
                    return;
                }

                RTEditor.inst.ShowNameEditor("Keybind Profile Creator", "Profile Name", "Create", () =>
                {
                    var name = RTEditor.inst.folderCreatorName.text;
                    if (string.IsNullOrEmpty(name))
                    {
                        EditorManager.inst.DisplayNotification($"Please set a name!", 2f, EditorManager.NotificationType.Error);
                        return;
                    }

                    CreateProfile(name, () => KeybindProfile.DefaultProfile);
                    RTEditor.inst.HideNameEditor();
                });
            };

            int num = 0;
            foreach (var profile in profiles)
            {
                int index = num;

                var name = profile.name;

                if (!RTString.SearchString(Popup.SearchTerm, name))
                {
                    num++;
                    continue;
                }

                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(Popup.Content, name);
                var storage = gameObject.GetComponent<FunctionButtonStorage>();
                storage.Text = name;
                storage.OnClick.ClearAll();

                var contextClickable = gameObject.GetOrAddComponent<ContextClickable>();
                contextClickable.onClick = pointerEventData =>
                {
                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonFunction("Open", () =>
                            {
                                SetCurrentProfile(profile);
                                Save();
                                RenderPopup();
                            }),
                            new ButtonFunction("Rename", () => RTEditor.inst.ShowNameEditor("Rename Keybind Profile", "Profile Name", "Rename", () =>
                            {
                                var name = RTEditor.inst.folderCreatorName.text;
                                if (string.IsNullOrEmpty(name))
                                {
                                    EditorManager.inst.DisplayNotification($"Please set a name!", 2f, EditorManager.NotificationType.Error);
                                    return;
                                }

                                var origName = profile.name;
                                RTFile.DeleteFile(RTFile.CombinePaths(KeybindsFolder, profile.GetFileName()));
                                profile.name = name;

                                int attempts = 0;
                                while (RTFile.FileExists(RTFile.CombinePaths(KeybindsFolder, profile.GetFileName())))
                                {
                                    profile.name = name + $" {attempts}";
                                    attempts++;
                                    if (attempts > 20)
                                    {
                                        EditorManager.inst.DisplayNotification($"Failed to create new profile.", 2f, EditorManager.NotificationType.Error);
                                        profile.name = origName;
                                        Save();
                                        return;
                                    }
                                }

                                Save();
                            })),
                            new ButtonFunction("Delete", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this profile? You cannot undo this.", () =>
                            {
                                if (profiles.Count == 1)
                                {
                                    EditorManager.inst.DisplayNotification($"Cannot delete only profile.", 2f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                profiles.RemoveAt(index);
                                RTFile.DeleteFile(RTFile.CombinePaths(KeybindsFolder, profile.GetFileName()));
                                Save();
                                RenderPopup();

                                RTEditor.inst.HideWarningPopup();
                            }, RTEditor.inst.HideWarningPopup)),
                            new ButtonFunction(true),
                            new ButtonFunction("Reset Keybinds", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to reset the keybinds in this profile? You cannot undo this.", () =>
                            {
                                profile.keybinds = KeybindProfile.GetDefaultKeybinds();
                                Save();
                                RTEditor.inst.HideWarningPopup();
                            }, RTEditor.inst.HideWarningPopup)),
                            new ButtonFunction("Clear Keybinds", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to clear all keybinds from this profile? You cannot undo this.", () =>
                            {
                                profile.keybinds.Clear();
                                Save();
                                RTEditor.inst.HideWarningPopup();
                            }, RTEditor.inst.HideWarningPopup)),
                            new ButtonFunction(true),
                            new ButtonFunction("Copy ID", () =>
                            {
                                LSText.CopyToClipboard(profile.id);
                                EditorManager.inst.DisplayNotification($"Copied keybind profile ID to clipboard!", 2f, EditorManager.NotificationType.Success);
                            }));
                        return;
                    }

                    SetCurrentProfile(profile);
                    Save();
                    RenderPopup();
                };

                var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(gameObject.transform, "Delete").GetComponent<DeleteButtonStorage>();
                RectValues.LeftAnchored.AnchoredPosition(580f, 0f).Pivot(1f, 1f).SizeDelta(32f, 32f).AssignToRectTransform(delete.transform.AsRT());
                delete.OnClick.NewListener(() => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this keybind? You cannot undo this.", () =>
                {
                    if (profiles.Count == 1)
                    {
                        EditorManager.inst.DisplayNotification($"Cannot delete only profile.", 2f, EditorManager.NotificationType.Warning);
                        return;
                    }

                    profiles.RemoveAt(index);
                    RTFile.DeleteFile(RTFile.CombinePaths(KeybindsFolder, profile.GetFileName()));
                    Save();
                    RenderPopup();

                    RTEditor.inst.HideWarningPopup();
                }, RTEditor.inst.HideWarningPopup));

                EditorThemeManager.ApplySelectable(storage.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(storage.label);
                EditorThemeManager.ApplyDeleteButton(delete);

                num++;
            }
        }

        /// <summary>
        /// Opens the keybind function list popup.
        /// </summary>
        public void OpenFunctionPopup()
        {
            FunctionPopup.Open();
            RenderFunctionPopup();
        }

        /// <summary>
        /// Renders the keybind function list popup.
        /// </summary>
        public void RenderFunctionPopup()
        {
            FunctionPopup.ClearContent();

            for (int i = 0; i < keybindFunctions.Count; i++)
            {
                int index = i;
                var function = keybindFunctions[i];
                var name = function.name;

                if (!RTString.SearchString(FunctionPopup.SearchTerm, name))
                    continue;

                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(FunctionPopup.Content, name);
                var storage = gameObject.GetComponent<FunctionButtonStorage>();
                storage.Text = name;
                storage.OnClick.NewListener(() =>
                {
                    if (CurrentKeybind)
                    {
                        CurrentKeybind.Name = name;
                        RenderDialog(CurrentKeybind);
                    }
                    FunctionPopup.Close();
                });

                EditorThemeManager.ApplySelectable(storage.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(storage.label);
            }
        }

        #endregion

        #region Keybind Functions

        /// <summary>
        /// List of keybind functions.
        /// </summary>
        public List<KeybindFunction> keybindFunctions;

        #region Main

        public void TogglePlayingSong(Keybind keybind)
        {
            if (AnimationEditor.inst.Dialog.IsCurrent)
            {
                AnimationEditor.inst.TogglePlaying();
                return;
            }

            EditorManager.inst.TogglePlayingSong();
        }

        public void TogglePreview(Keybind keybind) => RTEditor.inst.TogglePreview();

        public void Undo(Keybind keybind)
        {
            if (!RTEditor.inst.ienumRunning)
            {
                EditorManager.inst.DisplayNotification("Performing task, please wait...", 1f, EditorManager.NotificationType.Success);
                EditorManager.inst.Undo();
            }
            else
                EditorManager.inst.DisplayNotification("Wait until current task is complete!", 1f, EditorManager.NotificationType.Warning);
        }

        public void Redo(Keybind keybind)
        {
            if (!RTEditor.inst.ienumRunning)
            {
                EditorManager.inst.DisplayNotification("Performing task, please wait...", 1f, EditorManager.NotificationType.Success);
                EditorManager.inst.Redo();
            }
            else
                EditorManager.inst.DisplayNotification("Wait until current task is complete!", 1f, EditorManager.NotificationType.Warning);
        }

        public void Cut(Keybind keybind) => RTEditor.inst.Cut();

        public void Copy(Keybind keybind) => RTEditor.inst.Copy();

        public void Paste(Keybind keybind)
        {
            bool regen = true;

            if (keybind.TryGetSetting("Remove Prefab Instance ID", out string value) && bool.TryParse(value, out bool result))
                regen = result;

            RTEditor.inst.Paste(regen);
        }

        public void Duplicate(Keybind keybind)
        {
            bool regen = true;

            if (keybind.TryGetSetting("Remove Prefab Instance ID", out string value) && bool.TryParse(value, out bool result))
                regen = result;

            RTEditor.inst.Duplicate(regen);
        }

        public void Delete(Keybind keybind) => RTEditor.inst.Delete();

        #endregion

        #region Timeline

        public void SetLayer(Keybind keybind)
        {
            if (keybind.TryGetSetting("Layer", out string layerSetting) && int.TryParse(layerSetting, out int layer))
                EditorTimeline.inst.SetLayer(layer);
        }

        public void AddLayer(Keybind keybind)
        {
            if (keybind.TryGetSetting("Layer", out string layerSetting) && int.TryParse(layerSetting, out int layer))
                EditorTimeline.inst.SetLayer(EditorTimeline.inst.Layer + layer);
        }

        public void ToggleEventLayer(Keybind keybind) => EditorTimeline.inst.SetLayer(EditorTimeline.inst.layerType == EditorTimeline.LayerType.Objects ? EditorTimeline.LayerType.Events : EditorTimeline.LayerType.Objects);

        public void GoToCurrentTime(Keybind keybind) => EditorManager.inst.timelineScrollRectBar.value = AudioManager.inst.CurrentAudioSource.time / AudioManager.inst.CurrentAudioSource.clip.length;

        public void GoToStart(Keybind keybind) => EditorManager.inst.timelineScrollRectBar.value = 0f;

        public void GoToEnd(Keybind keybind) => EditorManager.inst.timelineScrollRectBar.value = 1f;

        public void ToggleBPMSnap(Keybind keybind)
        {
            var value = !RTEditor.inst.editorInfo.bpmSnapActive;
            RTEditor.inst.editorInfo.bpmSnapActive = value;
            EditorManager.inst.DisplayNotification($"Set BPM snap {(value ? "on": "off")}!", 2f, EditorManager.NotificationType.Success);
        }
        
        public void ForceSnapBPM(Keybind keybind)
        {
            var markers = GameData.Current.data.markers;
            var currentMarker = MarkerEditor.inst.currentMarker;
            if (RTMarkerEditor.inst.Dialog.IsCurrent && currentMarker >= 0 && currentMarker < markers.Count)
            {
                var marker = markers[currentMarker];
                var time = MarkerEditor.inst.left.Find("time/input").GetComponent<InputField>();

                time.text = RTEditor.SnapToBPM(marker.time).ToString();

                return;
            }

            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Objects && EditorTimeline.inst.SelectedObjects.Count > 0)
                foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                {
                    timelineObject.Time = RTEditor.SnapToBPM(timelineObject.Time);

                    switch (timelineObject.TimelineReference)
                    {
                        case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.START_TIME);
                                break;
                            }
                        case TimelineObject.TimelineReferenceType.PrefabObject: {
                                RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.TIME);
                                break;
                            }
                        case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.START_TIME);
                                break;
                            }
                    }

                    EditorTimeline.inst.RenderTimelineObject(timelineObject);
                }

            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events && RTEventEditor.inst.SelectedKeyframes.Count > 0 && EditorTimeline.inst.isOverMainTimeline)
                foreach (var timelineKeyframe in RTEventEditor.inst.SelectedKeyframes)
                {
                    if (timelineKeyframe.Index != 0 && !timelineKeyframe.Locked)
                        timelineKeyframe.Time = RTEditor.SnapToBPM(timelineKeyframe.Time);

                    timelineKeyframe.RenderPos();
                }

            if (ObjEditor.inst.ObjectView.activeInHierarchy && EditorTimeline.inst.CurrentSelection.isBeatmapObject && EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>().TimelineKeyframes.Where(x => x.Selected).Count() > 0 && !EditorTimeline.inst.isOverMainTimeline)
            {
                var startTime = EditorTimeline.inst.CurrentSelection.Time;
                foreach (var timelineKeyframe in EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>().TimelineKeyframes.Where(x => x.Selected))
                {
                    if (timelineKeyframe.Index != 0 && !timelineKeyframe.Locked)
                        timelineKeyframe.Time = RTEditor.SnapToBPM(timelineKeyframe.Time + startTime) - startTime;
                }

                var bm = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.CurrentSelection);
                RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                ObjectEditor.inst.Dialog.Timeline.RenderKeyframes(bm);
            }

            if (RTEditor.DraggingPlaysSound)
            {
                SoundManager.inst.PlaySound(DefaultSounds.LeftRight, 0.7f, 0.6f);
                SoundManager.inst.PlaySound(DefaultSounds.LeftRight, 0.8f, 0.1f);
            }
        }

        public void AddTimelineBin(Keybind keybind) => EditorTimeline.inst.AddBin();

        public void RemoveTimelineBin(Keybind keybind) => EditorTimeline.inst.RemoveBin();

        public void SetTimelineBin(Keybind keybind)
        {
            if (keybind.TryGetSetting("Count", out string str) && int.TryParse(str, out int count))
                EditorTimeline.inst.SetBinCount(count);
        }

        #endregion

        #region Object

        public void UpdateEverything(Keybind keybind)
        {
            RandomHelper.UpdateSeed();
            RTLevel.Reinit();

            // reset modifiers
            foreach (var beatmapObject in GameData.Current.beatmapObjects)
            {
                beatmapObject.modifiers.ForLoop(modifier =>
                {
                    modifier.RunInactive(modifier, beatmapObject);
                    ModifiersHelper.OnRemoveCache(modifier);
                    modifier.Result = default;
                });
            }
            foreach (var backgroundObject in GameData.Current.backgroundObjects)
            {
                backgroundObject.modifiers.ForLoop(modifier =>
                {
                    modifier.RunInactive(modifier, backgroundObject);
                    ModifiersHelper.OnRemoveCache(modifier);
                    modifier.Result = default;
                });
            }
            foreach (var prefabObject in GameData.Current.prefabObjects)
            {
                prefabObject.modifiers.ForLoop(modifier =>
                {
                    modifier.RunInactive(modifier, prefabObject);
                    ModifiersHelper.OnRemoveCache(modifier);
                    modifier.Result = default;
                });
            }
            foreach (var modifierBlock in GameData.Current.modifierBlocks)
            {
                modifierBlock.Modifiers.ForLoop(modifier =>
                {
                    modifier.RunInactive(modifier, null, null);
                    ModifiersHelper.OnRemoveCache(modifier);
                    modifier.Result = default;
                });
            }
            GameData.Current.modifiers.ForLoop(modifier =>
            {
                modifier.RunInactive(modifier, GameData.Current);
                ModifiersHelper.OnRemoveCache(modifier);
                modifier.Result = default;
            });

            PlayerManager.RespawnPlayers();
        }

        public void UpdateObject(Keybind keybind)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                switch (timelineObject.TimelineReference)
                {
                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                            var beatmapObject = timelineObject.GetData<BeatmapObject>();
                            beatmapObject.customParent = null;
                            beatmapObject.customShape = -1;
                            beatmapObject.customShapeOption = -1;
                            RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), recalculate: false);
                            beatmapObject.modifiers.ForEach(modifier =>
                            {
                                modifier.RunInactive(modifier, beatmapObject);
                                ModifiersHelper.OnRemoveCache(modifier);
                                modifier.Result = default;
                            });

                            break;
                        }
                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.customParent = null;
                            RTLevel.Current?.UpdatePrefab(prefabObject, recalculate: false);
                            prefabObject.modifiers.ForEach(modifier =>
                            {
                                modifier.RunInactive(modifier, prefabObject);
                                ModifiersHelper.OnRemoveCache(modifier);
                                modifier.Result = default;
                            });

                            break;
                        }
                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                            var backgroundObject = timelineObject.GetData<BackgroundObject>();
                            backgroundObject.customShape = -1;
                            backgroundObject.customShapeOption = -1;
                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject, recalculate: false);
                            backgroundObject.modifiers.ForEach(modifier =>
                            {
                                modifier.RunInactive(modifier, backgroundObject);
                                ModifiersHelper.OnRemoveCache(modifier);
                                modifier.Result = default;
                            });

                            break;
                        }
                }
            }

            RTLevel.Current?.RecalculateObjectStates();
        }
        
        public void SetSongTimeAutokill(Keybind keybind)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                switch (timelineObject.TimelineReference)
                {
                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                            var beatmapObject = timelineObject.GetData<BeatmapObject>();

                            beatmapObject.autoKillType = AutoKillType.SongTime;
                            beatmapObject.autoKillOffset = AudioManager.inst.CurrentAudioSource.time;
                            beatmapObject.editorData.collapse = true;

                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL, false);
                            break;
                        }
                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                            var prefabObject = timelineObject.GetData<PrefabObject>();

                            prefabObject.autoKillType = PrefabAutoKillType.SongTime;
                            prefabObject.autoKillOffset = AudioManager.inst.CurrentAudioSource.time;
                            prefabObject.editorData.collapse = true;

                            RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.AUTOKILL, false);
                            EditorTimeline.inst.RenderTimelineObject(timelineObject);

                            break;
                        }
                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                            var backgroundObject = timelineObject.GetData<BackgroundObject>();

                            backgroundObject.autoKillType = AutoKillType.SongTime;
                            backgroundObject.autoKillOffset = AudioManager.inst.CurrentAudioSource.time;
                            backgroundObject.editorData.collapse = true;

                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject, BackgroundObjectContext.AUTOKILL, false);
                            break;
                        }
                }
                EditorTimeline.inst.RenderTimelineObject(timelineObject);
            }
            RTLevel.Current?.Sort();
        }

        public void SwapLockSelection(Keybind keybind)
        {
            if (EditorManager.inst.IsOverObjTimeline && ObjectEditor.inst.Dialog.IsCurrent && EditorTimeline.inst.CurrentSelection.isBeatmapObject)
            {
                var selected = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>().TimelineKeyframes.Where(x => x.Selected);

                foreach (var timelineObject in selected)
                {
                    if (timelineObject.Index == 0)
                        continue;

                    timelineObject.Locked = !timelineObject.Locked;
                    timelineObject.Render();
                }

                return;
            }

            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
            {
                foreach (var timelineObject in RTEventEditor.inst.SelectedKeyframes)
                {
                    timelineObject.Locked = !timelineObject.Locked;
                    timelineObject.RenderIcons();
                }
                return;
            }

            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                timelineObject.Locked = !timelineObject.Locked;
                EditorTimeline.inst.RenderTimelineObject(timelineObject);
            }
        }

        public bool loggled = true;
        public void ToggleLockSelection(Keybind keybind)
        {
            if (EditorManager.inst.IsOverObjTimeline && ObjectEditor.inst.Dialog.IsCurrent && EditorTimeline.inst.CurrentSelection.isBeatmapObject)
            {
                var selected = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>().TimelineKeyframes.Where(x => x.Selected);

                foreach (var timelineObject in selected)
                {
                    if (timelineObject.Index == 0)
                        continue;

                    timelineObject.Locked = loggled;
                    timelineObject.Render();
                }

                loggled = !loggled;
                return;
            }

            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
            {
                foreach (var timelineObject in RTEventEditor.inst.SelectedKeyframes)
                {
                    timelineObject.Locked = loggled;
                    timelineObject.RenderIcons();
                }

                loggled = !loggled;
                return;
            }

            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                timelineObject.Locked = loggled;
                EditorTimeline.inst.RenderTimelineObject(timelineObject);
            }

            loggled = !loggled;
        }

        public void SwapCollapseSelection(Keybind keybind)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                timelineObject.Collapse = !timelineObject.Collapse;
                EditorTimeline.inst.RenderTimelineObject(timelineObject);
            }
        }

        public bool coggled = true;
        public void ToggleCollapseSelection(Keybind keybind)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                timelineObject.Collapse = coggled;
                EditorTimeline.inst.RenderTimelineObject(timelineObject);
            }

            coggled = !coggled;
        }

        public void AddObjectLayer(Keybind keybind)
        {
            int amount = keybind.GetSettingOrDefault("Amount", 1);

            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                timelineObject.Layer += amount;
                EditorTimeline.inst.RenderTimelineObject(timelineObject);
            }
        }

        public void SetObjectLayer(Keybind keybind)
        {
            int layer = keybind.GetSettingOrDefault("Layer", 0);

            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                timelineObject.Layer = layer;
                EditorTimeline.inst.RenderTimelineObject(timelineObject);
            }
        }

        public void CycleObjectTypeUp(Keybind keybind)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                if (!timelineObject.isBeatmapObject)
                    continue;

                var bm = timelineObject.GetData<BeatmapObject>();

                bm.objectType++;

                if ((int)bm.objectType > Enum.GetNames(typeof(BeatmapObject.ObjectType)).Length)
                    bm.objectType = 0;

                RTLevel.Current?.UpdateObject(bm, ObjectContext.OBJECT_TYPE);
                EditorTimeline.inst.RenderTimelineObject(timelineObject);
            }
            RTLevel.Current?.RecalculateObjectStates();
        }

        public void CycleObjectTypeDown(Keybind keybind)
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                if (!timelineObject.isBeatmapObject)
                    continue;

                var bm = timelineObject.GetData<BeatmapObject>();

                var e = (int)bm.objectType - 1;

                if (e < 0)
                    e = Enum.GetValues(bm.objectType.GetType()).Length - 1;

                bm.objectType = (BeatmapObject.ObjectType)e;

                RTLevel.Current?.UpdateObject(bm, ObjectContext.OBJECT_TYPE);
                EditorTimeline.inst.RenderTimelineObject(timelineObject);
            }
            RTLevel.Current?.RecalculateObjectStates();
        }

        public void SelectNextObject(Keybind keybind)
        {
            var currentSelection = EditorTimeline.inst.CurrentSelection;

            var index = EditorTimeline.inst.timelineObjects.IndexOf(currentSelection);

            if (index + 1 < EditorTimeline.inst.timelineObjects.Count)
                EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.timelineObjects[index + 1], EditorConfig.Instance.BringToSelection.Value);
        }

        public void SelectPreviousObject(Keybind keybind)
        {
            var currentSelection = EditorTimeline.inst.CurrentSelection;

            var index = EditorTimeline.inst.timelineObjects.IndexOf(currentSelection);

            if (index - 1 >= 0)
                EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.timelineObjects[index - 1], EditorConfig.Instance.BringToSelection.Value);
        }
        
        public void HideSelection(Keybind keybind)
        {
            int hiddenCount = 0;
            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                timelineObject.Hidden = true;
                switch (timelineObject.TimelineReference)
                {
                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                            RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.HIDE);

                            break;
                        }
                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                            RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.HIDE);

                            break;
                        }
                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                            RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.HIDE);

                            break;
                        }
                }
                hiddenCount++;
            }

            EditorManager.inst.DisplayNotification($"Hidden [{hiddenCount}] objects!", 2f, EditorManager.NotificationType.Success);
        }

        public void UnhideHiddenObjects(Keybind keybind)
        {
            int hiddenCount = 0;
            foreach (var timelineObject in EditorTimeline.inst.timelineObjects)
            {
                if (!timelineObject.Hidden)
                    continue;

                timelineObject.Hidden = false;
                switch (timelineObject.TimelineReference)
                {
                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                            RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.HIDE);

                            break;
                        }
                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                            RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.HIDE);

                            break;
                        }
                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                            RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.HIDE);

                            break;
                        }
                }
                hiddenCount++;
            }

            EditorManager.inst.DisplayNotification($"Unhidden [{hiddenCount}] objects!", 2f, EditorManager.NotificationType.Success);
        }

        public void ToggleHideSelection(Keybind keybind)
        {
            int hiddenCount = 0;
            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                timelineObject.Hidden = !timelineObject.Hidden;
                switch (timelineObject.TimelineReference)
                {
                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                            RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.HIDE);

                            break;
                        }
                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                            RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.HIDE);

                            break;
                        }
                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                            RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.HIDE);

                            break;
                        }
                }
                hiddenCount++;
            }

            EditorManager.inst.DisplayNotification($"Toggled hidden state of [{hiddenCount}] objects!", 2f, EditorManager.NotificationType.Success);
        }

        public void ToggleObjectDragging(Keybind keybind) => EditorConfig.Instance.ObjectDraggerEnabled.Value = !EditorConfig.Instance.ObjectDraggerEnabled.Value;

        public void ToggleObjectDragHelper(Keybind keybind) => EditorConfig.Instance.ObjectDraggerHelper.Value = !EditorConfig.Instance.ObjectDraggerHelper.Value;

        public void SetObjectDragHelperAxisX(Keybind keybind)
        {
            if (RTEditor.inst.SelectObjectHelper && RTEditor.inst.SelectObjectHelper.dragging)
                RTEditor.inst.SelectObjectHelper.firstDirection = SelectObject.Axis.PosX;
        }
        
        public void SetObjectDragHelperAxisY(Keybind keybind)
        {
            if (RTEditor.inst.SelectObjectHelper && RTEditor.inst.SelectObjectHelper.dragging)
                RTEditor.inst.SelectObjectHelper.firstDirection = SelectObject.Axis.PosY;
        }

        public void TransformPosition(Keybind keybind)
        {
            this.createKeyframe = keybind.TryGetSetting("Create Keyframe", out string createKeyframeSetting) && bool.TryParse(createKeyframeSetting, out bool createKeyframe) && createKeyframe;
            this.useNearest = keybind.TryGetSetting("Use Nearest", out string useNearestSetting) && bool.TryParse(useNearestSetting, out bool useNearest) && useNearest;
            this.usePrevious = keybind.TryGetSetting("Use Previous", out string usePreviousSetting) && bool.TryParse(usePreviousSetting, out bool usePrevious) && usePrevious;

            SetValues(0);
        }

        public void TransformScale(Keybind keybind)
        {
            this.createKeyframe = keybind.TryGetSetting("Create Keyframe", out string createKeyframeSetting) && bool.TryParse(createKeyframeSetting, out bool createKeyframe) && createKeyframe;
            this.useNearest = keybind.TryGetSetting("Use Nearest", out string useNearestSetting) && bool.TryParse(useNearestSetting, out bool useNearest) && useNearest;
            this.usePrevious = keybind.TryGetSetting("Use Previous", out string usePreviousSetting) && bool.TryParse(usePreviousSetting, out bool usePrevious) && usePrevious;

            SetValues(1);
        }

        public void TransformRotation(Keybind keybind)
        {
            this.createKeyframe = keybind.TryGetSetting("Create Keyframe", out string createKeyframeSetting) && bool.TryParse(createKeyframeSetting, out bool createKeyframe) && createKeyframe;
            this.useNearest = keybind.TryGetSetting("Use Nearest", out string useNearestSetting) && bool.TryParse(useNearestSetting, out bool useNearest) && useNearest;
            this.usePrevious = keybind.TryGetSetting("Use Previous", out string usePreviousSetting) && bool.TryParse(usePreviousSetting, out bool usePrevious) && usePrevious;

            SetValues(2);
        }

        public void FinishTransform(Keybind keybind) => EndDrag(keybind.TryGetSetting("Cancel", out string cancelSetting) && bool.TryParse(cancelSetting, out bool cancel) && cancel);

        public void ParentPicker(Keybind keybind)
        {
            if (!EditorTimeline.inst.CurrentSelection.isBeatmapObject && !EditorTimeline.inst.CurrentSelection.isPrefabObject)
                return;

            RTEditor.inst.parentPickerEnabled = true;
        }

        public void ResetIntegerVariables(Keybind keybind)
        {
            foreach (var beatmapObject in GameData.Current.beatmapObjects)
                beatmapObject.integerVariable = 0;
        }

        #endregion

        #region Prefab

        public void OpenPrefabCreator(Keybind keybind)
        {
            RTPrefabEditor.inst.createInternal = keybind.TryGetSetting("External", out string setting) && bool.TryParse(setting, out bool external) && !external;
            RTPrefabEditor.inst.OpenDialog();
        }

        public void CollapsePrefab(Keybind keybind)
        {
            if (EditorTimeline.inst.SelectedObjects.Count == 1 && EditorTimeline.inst.CurrentSelection.TryGetPrefabable(out IPrefabable prefabable) && !string.IsNullOrEmpty(prefabable.PrefabInstanceID))
                RTPrefabEditor.inst.Collapse(prefabable, prefabable is IEditable editable ? editable.EditorData : null);
        }

        public void ExpandPrefab(Keybind keybind)
        {
            if (EditorTimeline.inst.SelectedPrefabObjects.Count == 1 && EditorTimeline.inst.CurrentSelection && EditorTimeline.inst.CurrentSelection.isPrefabObject)
                RTPrefabEditor.inst.ExpandCurrentPrefab();
        }

        public void SpawnPrefab(Keybind keybind)
        {
            var search = keybind.GetSettingOrDefault("Search Prefab Using", 0);
            var reference = keybind.GetSettingOrDefault("Prefab Reference", string.Empty);
            if (string.IsNullOrEmpty(reference))
                return;

            var prefab = GameData.Current.GetPrefab(search, reference);
            if (prefab)
                RTPrefabEditor.inst.AddPrefabObjectToLevel(prefab);
        }

        public void SpawnSelectedQuickPrefab(Keybind keybind)
        {
            if (RTPrefabEditor.inst.currentQuickPrefab)
                RTPrefabEditor.inst.AddPrefabObjectToLevel(RTPrefabEditor.inst.currentQuickPrefab, RTPrefabEditor.inst.quickPrefabTarget?.InterpolateChain().ToClass());
            else
                EditorManager.inst.DisplayNotification("No selected quick prefab!", 1f, EditorManager.NotificationType.Error);
        }

        #endregion

        #region Marker

        public void CreateNewMarker(Keybind keybind) => MarkerEditor.inst.CreateNewMarker();

        public void JumpToNextMarker(Keybind keybind)
        {
            if (!GameData.Current || GameData.Current.data.markers.Count <= 0)
                return;

            RTMarkerEditor.inst.OrderMarkers();

            var currentMarker = GameData.Current.data.markers.FindLastIndex(x => x.time <= AudioManager.inst.CurrentAudioSource.time + 0.005f);

            if (currentMarker + 1 < 0)
                return;

            var marker = GameData.Current.data.markers[Mathf.Clamp(currentMarker + 1, 0, GameData.Current.data.markers.Count - 1)];

            if (RTMarkerEditor.inst.timelineMarkers.TryFind(x => x.Marker.id == marker.id, out TimelineMarker timelineMarker))
                RTMarkerEditor.inst.SetCurrentMarker(timelineMarker, true, EditorConfig.Instance.BringToSelection.Value, false);
        }

        public void JumpToPreviousMarker(Keybind keybind)
        {
            if (!GameData.Current || GameData.Current.data.markers.Count <= 0)
                return;

            RTMarkerEditor.inst.OrderMarkers();

            var currentMarker = GameData.Current.data.markers.FindLastIndex(x => x.time < AudioManager.inst.CurrentAudioSource.time - 0.005f);

            if (currentMarker < 0)
                return;

            var marker = GameData.Current.data.markers[Mathf.Clamp(currentMarker, 0, GameData.Current.data.markers.Count - 1)];

            if (RTMarkerEditor.inst.timelineMarkers.TryFind(x => x.Marker.id == marker.id, out TimelineMarker timelineMarker))
                RTMarkerEditor.inst.SetCurrentMarker(timelineMarker, true, EditorConfig.Instance.BringToSelection.Value, false);
        }

        #endregion

        #region Save / Load

        public void SaveLevel(Keybind keybind)
        {
            if (ProjectPlanner.inst && ProjectPlanner.inst.PlannerActive)
            {
                ProjectPlanner.inst.Save();
                return;
            }

            EditorLevelManager.inst.SaveLevel();
        }

        public void OpenLevelPopup(Keybind keybind) => EditorManager.inst.OpenBeatmapPopup();

        public void SaveLevelCopy(Keybind keybind)
        {
            EditorManager.inst.ClearPopups();
            RTEditor.inst.SaveAsPopup.Open();
        }

        public void CreateNewLevel(Keybind keybind)
        {
            EditorManager.inst.ClearPopups();
            EditorLevelManager.inst.NewLevelPopup.Open();
        }

        #endregion

        #region Keyframe

        public void SetFirstKeyframeInType(Keybind keybind)
        {
            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Objects)
            {
                if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                {
                    var bm = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                    ObjectEditor.inst.Dialog.Timeline.UpdateKeyframeOrder(bm);
                    ObjectEditor.inst.Dialog.Timeline.SetCurrentKeyframe(bm, ObjectEditor.inst.Dialog.Timeline.currentKeyframeType, 0, true);
                }
            }
            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
            {
                RTEventEditor.inst.SetCurrentEvent(EventEditor.inst.currentEventType, 0);
                AudioManager.inst.SetMusicTime(GameData.Current.events[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].time);
            }
        }

        public void SetLastKeyframeInType(Keybind keybind)
        {
            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Objects)
            {
                if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                {
                    var bm = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                    ObjectEditor.inst.Dialog.Timeline.UpdateKeyframeOrder(bm);
                    ObjectEditor.inst.Dialog.Timeline.SetCurrentKeyframe(bm, ObjectEditor.inst.Dialog.Timeline.currentKeyframeType, bm.events[ObjectEditor.inst.Dialog.Timeline.currentKeyframeType].Count - 1, true);
                }
            }
            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
            {
                RTEventEditor.inst.SetCurrentEvent(EventEditor.inst.currentEventType, GameData.Current.events[EventEditor.inst.currentEventType].Count - 1);
                AudioManager.inst.SetMusicTime(GameData.Current.events[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].time);
            }
        }

        public void SetNextKeyframeInType(Keybind keybind)
        {
            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Objects)
            {
                if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                {
                    var bm = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                    ObjectEditor.inst.Dialog.Timeline.UpdateKeyframeOrder(bm);
                    ObjectEditor.inst.Dialog.Timeline.SetCurrentKeyframe(bm, ObjectEditor.inst.Dialog.Timeline.currentKeyframeType, Mathf.Clamp(ObjectEditor.inst.Dialog.Timeline.currentKeyframeIndex + 1, 0, bm.events[ObjectEditor.inst.Dialog.Timeline.currentKeyframeType].Count - 1), true);
                }
            }
            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
            {
                int count = GameData.Current.events[EventEditor.inst.currentEventType].Count;
                int num = EventEditor.inst.currentEvent + 1 >= count ? count - 1 : EventEditor.inst.currentEvent + 1;

                EventEditor.inst.SetCurrentEvent(EventEditor.inst.currentEventType, num);
                AudioManager.inst.SetMusicTime(GameData.Current.events[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].time);
            }
        }

        public void SetPreviousKeyframeInType(Keybind keybind)
        {
            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Objects)
            {
                if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                {
                    var bm = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                    ObjectEditor.inst.Dialog.Timeline.UpdateKeyframeOrder(bm);
                    ObjectEditor.inst.Dialog.Timeline.SetCurrentKeyframe(bm, ObjectEditor.inst.Dialog.Timeline.currentKeyframeType, Mathf.Clamp(ObjectEditor.inst.Dialog.Timeline.currentKeyframeIndex - 1, 0, bm.events[ObjectEditor.inst.Dialog.Timeline.currentKeyframeType].Count - 1), true);
                }
            }
            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
            {
                int num = EventEditor.inst.currentEvent - 1 < 0 ? 0 : EventEditor.inst.currentEvent - 1;

                EventEditor.inst.SetCurrentEvent(EventEditor.inst.currentEventType, num);
                AudioManager.inst.SetMusicTime(GameData.Current.events[EventEditor.inst.currentEventType][EventEditor.inst.currentEvent].time);
            }
        }

        public void IncreaseKeyframeValue(Keybind keybind)
        {
            var type = keybind.TryGetSetting("Type", out string eventType) ? Parser.TryParse(eventType, 0) : 0;
            var index = keybind.TryGetSetting("Index", out string eventIndex) ? Parser.TryParse(eventIndex, 0) : 0;
            var value = keybind.TryGetSetting("Value Index", out string eventValue) ? Parser.TryParse(eventValue, 0) : 0;
            var amount = keybind.TryGetSetting("Amount", out string eventAmount) ? Parser.TryParse(eventAmount, 0f) : 0f;

            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                if (timelineObject.isBeatmapObject)
                {
                    var bm = timelineObject.GetData<BeatmapObject>();

                    type = Mathf.Clamp(type, 0, bm.events.Count - 1);
                    index = Mathf.Clamp(index, 0, bm.events[type].Count - 1);
                    value = Mathf.Clamp(value, 0, bm.events[type][index].values.Length - 1);

                    var val = bm.events[type][index].values[value];

                    if (type == 3 && val == 0)
                        val = Mathf.Clamp(val + amount, 0, ThemeManager.inst.Current.objectColors.Count - 1);
                    else
                        val += amount;

                    bm.events[type][index].values[value] = val;

                    RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                }
                if (timelineObject.isPrefabObject)
                {
                    var po = timelineObject.GetData<PrefabObject>();

                    type = Mathf.Clamp(type, 0, po.events.Count - 1);
                    value = Mathf.Clamp(value, 0, po.events[type].values.Length - 1);

                    po.events[type].values[value] += amount;

                    RTLevel.Current?.UpdatePrefab(po, PrefabObjectContext.TRANSFORM_OFFSET);
                }
            }
        }

        public void DecreaseKeyframeValue(Keybind keybind)
        {
            var type = keybind.TryGetSetting("Type", out string eventType) ? Parser.TryParse(eventType, 0) : 0;
            var index = keybind.TryGetSetting("Index", out string eventIndex) ? Parser.TryParse(eventIndex, 0) : 0;
            var value = keybind.TryGetSetting("Value Index", out string eventValue) ? Parser.TryParse(eventValue, 0) : 0;
            var amount = keybind.TryGetSetting("Amount", out string eventAmount) ? Parser.TryParse(eventAmount, 0f) : 0f;

            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                if (timelineObject.isBeatmapObject)
                {
                    var bm = timelineObject.GetData<BeatmapObject>();

                    type = Mathf.Clamp(type, 0, bm.events.Count - 1);
                    index = Mathf.Clamp(index, 0, bm.events[type].Count - 1);
                    value = Mathf.Clamp(value, 0, bm.events[type][index].values.Length - 1);

                    var val = bm.events[type][index].values[value];

                    if (type == 3 && val == 0)
                        val = Mathf.Clamp(val - amount, 0, ThemeManager.inst.Current.objectColors.Count - 1);
                    else
                        val -= amount;

                    bm.events[type][index].values[value] = val;

                    RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                }
                if (timelineObject.isPrefabObject)
                {
                    var po = timelineObject.GetData<PrefabObject>();

                    type = Mathf.Clamp(type, 0, po.events.Count - 1);
                    value = Mathf.Clamp(value, 0, po.events[type].values.Length - 1);

                    po.events[type].values[value] -= amount;

                    RTLevel.Current?.UpdatePrefab(po, PrefabObjectContext.TRANSFORM_OFFSET);
                }
            }
        }

        public void SetKeyframeValue(Keybind keybind)
        {
            var type = keybind.TryGetSetting("Type", out string eventType) ? Parser.TryParse(eventType, 0) : 0;
            var index = keybind.TryGetSetting("Index", out string eventIndex) ? Parser.TryParse(eventIndex, 0) : 0;
            var value = keybind.TryGetSetting("Value Index", out string eventValue) ? Parser.TryParse(eventValue, 0) : 0;
            var amount = keybind.TryGetSetting("Amount", out string eventAmount) ? Parser.TryParse(eventAmount, 0f) : 0f;

            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                if (timelineObject.isBeatmapObject)
                {
                    var bm = timelineObject.GetData<BeatmapObject>();

                    type = Mathf.Clamp(type, 0, bm.events.Count - 1);
                    index = Mathf.Clamp(index, 0, bm.events[type].Count - 1);
                    value = Mathf.Clamp(value, 0, bm.events[type][index].values.Length - 1);

                    var val = bm.events[type][index].values[value];

                    if (type == 3 && val == 0)
                        val = Mathf.Clamp(amount, 0, ThemeManager.inst.Current.objectColors.Count - 1);
                    else
                        val = amount;

                    bm.events[type][index].values[value] = val;

                    RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                }
                if (timelineObject.isPrefabObject)
                {
                    var po = timelineObject.GetData<PrefabObject>();

                    type = Mathf.Clamp(type, 0, po.events.Count - 1);
                    value = Mathf.Clamp(value, 0, po.events[type].values.Length - 1);

                    po.events[type].values[value] = amount;

                    RTLevel.Current?.UpdatePrefab(po, PrefabObjectContext.TRANSFORM_OFFSET);
                }
            }
        }

        #endregion

        #region Game

        public void ToggleZenMode(Keybind keybind)
        {
            CoreConfig.Instance.ChallengeModeSetting.Value = CoreConfig.Instance.ChallengeModeSetting.Value != ChallengeMode.Zen ? ChallengeMode.Zen : ChallengeMode.Normal;
            EditorManager.inst.DisplayNotification($"Set Zen Mode {(CoreConfig.Instance.ChallengeModeSetting.Value == ChallengeMode.Zen ? "On" : "Off")}", 2f, EditorManager.NotificationType.Success);
        }

        public void CycleGameMode(Keybind keybind)
        {
            var current = CoreConfig.Instance.ChallengeModeSetting.Value;
            var values = current.GetValues();
            var index = Array.IndexOf(values, current);
            index++;
            if (index >= values.Length)
                index = 0;
            CoreConfig.Instance.ChallengeModeSetting.Value = values[index];

            EditorManager.inst.DisplayNotification($"Set Game Mode to {CoreConfig.Instance.ChallengeModeSetting.Value.DisplayName} Mode!", 2f, EditorManager.NotificationType.Success);
        }

        public void AddPitch(Keybind keybind)
        {
            var pitch = RTLevel.Current && RTLevel.Current.eventEngine ? RTLevel.Current.eventEngine.pitchOffset : AudioManager.inst.pitch;
            pitch += keybind.GetSettingOrDefault("Pitch", 0.1f);
            AudioManager.inst.SetPitch(pitch);
        }

        public void SetPitch(Keybind keybind) => AudioManager.inst.SetPitch(keybind.GetSettingOrDefault("Pitch", 1.0f));

        public void UpdateSeed(Keybind keybind) => RTLevel.Current.InitSeed();

        #endregion

        #region Info

        public void ToggleShowHelp(Keybind keybind) => EditorManager.inst.SetShowHelp(!EditorManager.inst.showHelp);

        public void ToggleMouseTooltip(Keybind keybind)
        {
            var o = !EditorConfig.Instance.MouseTooltipDisplay.Value;
            EditorConfig.Instance.MouseTooltipDisplay.Value = o;
            EditorManager.inst.DisplayNotification($"Set tooltip {(o ? "on" : "off")}", 1.5f, EditorManager.NotificationType.Success);
        }

        #endregion

        public void ToggleProjectPlanner(Keybind keybind) => ProjectPlanner.inst?.ToggleState();

        public void NextProjectPlannerOST(Keybind keybind) => ProjectPlanner.inst?.NextOST();

        public void StopProjectPlannerOST(Keybind keybind) => ProjectPlanner.inst?.StopOST();

        public void TogglePlayingProjectPlannerOST(Keybind keybind)
        {
            if (ProjectPlanner.inst && ProjectPlanner.inst.OSTAudioSource)
                ProjectPlanner.inst.pausedOST = !ProjectPlanner.inst.pausedOST;
        }

        public void ShuffleProjectPlannerOST(Keybind keybind) => ProjectPlanner.inst?.ShuffleOST();

        public void StartProjectPlannerOST(Keybind keybind) => ProjectPlanner.inst?.StartOST();

        public void SwitchKeybindProfile(Keybind keybind)
        {
            if (!keybind.TryGetSetting("Profile ID", out string value) || string.IsNullOrEmpty(value) || !profiles.TryFind(x => x.id == value, out KeybindProfile keybindProfile))
                return;

            SetCurrentProfile(keybindProfile);
            RenderPopupIfOpen();

            EditorManager.inst.DisplayNotification($"Set the current keybind profile to {keybindProfile.name}!", 3f, EditorManager.NotificationType.Success);
        }

        #endregion

        #region Misc

        /// <summary>
        /// Creates a new keybind profile.
        /// </summary>
        /// <param name="name">Name of the profile to set.</param>
        /// <param name="getProfile">Default profile settings.</param>
        public void CreateProfile(string name, Func<KeybindProfile> getProfile)
        {
            if (string.IsNullOrEmpty(name))
            {
                EditorManager.inst.DisplayNotification($"Please set a name!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            RTFile.CreateDirectory(KeybindsFolder);
            var profile = getProfile.Invoke();
            profile.name = name;

            int attempts = 0;
            while (RTFile.FileExists(RTFile.CombinePaths(KeybindsFolder, profile.GetFileName())))
            {
                profile.name = name + $" {attempts}";
                attempts++;
                if (attempts > 20)
                {
                    EditorManager.inst.DisplayNotification($"Failed to create new profile.", 2f, EditorManager.NotificationType.Error);
                    return;
                }
            }

            profiles.Add(profile);
            SetCurrentProfile(profile);
            Save();
            RenderPopup();
        }

        /// <summary>
        /// Sets the current keybind profile.
        /// </summary>
        /// <param name="profile">Keybind profile to set.</param>
        public void SetCurrentProfile(KeybindProfile profile)
        {
            currentProfileID = profile.id;
            CurrentProfile = profile;
            Dialog.Close();
        }

        public void SetValues(int type)
        {
            if (EditorTimeline.inst.SelectedObjectCount > 1)
            {
                EditorManager.inst.DisplayNotification("Cannot shift multiple objects around currently.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
            {
                selectionType = SelectionType.Object;
                beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                SetCurrentKeyframe(type);
            }
            if (EditorTimeline.inst.CurrentSelection.isPrefabObject)
            {
                selectionType = SelectionType.Prefab;
                prefabObject = EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>();
                selectedKeyframe = prefabObject.events[type];
                originalValues = selectedKeyframe.values.Copy();
            }

            setKeyframeValues = false;

            firstDirection = SelectObject.Axis.Static;

            currentType = type;

            dragging = true;
        }

        public void UpdateValues()
        {
            if (!dragging)
                return;

            var vector = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
            var vector2 = Camera.main.ScreenToWorldPoint(vector) * (currentType == 1 ? 0.2f : currentType == 2 ? 2f : 1f);
            var vector3 = currentType != 1 ? new Vector3((int)vector2.x, (int)vector2.y, 0f) :
                new Vector3(RTMath.RoundToNearestDecimal(vector2.x, 1), RTMath.RoundToNearestDecimal(vector2.y, 1), 0f);

            switch (currentType)
            {
                case 0: {
                        if (!setKeyframeValues)
                        {
                            setKeyframeValues = true;
                            dragKeyframeValues = new Vector2(selectedKeyframe.values[0], selectedKeyframe.values[1]);
                            dragOffset = Input.GetKey(KeyCode.LeftShift) ? vector3 : vector2;
                        }

                        var finalVector = Input.GetKey(KeyCode.LeftShift) ? vector3 : vector2;

                        if (Input.GetKey(KeyCode.LeftControl) && firstDirection == SelectObject.Axis.Static)
                        {
                            if (dragOffset.x > finalVector.x)
                                firstDirection = SelectObject.Axis.PosX;

                            if (dragOffset.x < finalVector.x)
                                firstDirection = SelectObject.Axis.NegX;

                            if (dragOffset.y > finalVector.y)
                                firstDirection = SelectObject.Axis.PosY;

                            if (dragOffset.y < finalVector.y)
                                firstDirection = SelectObject.Axis.NegY;
                        }

                        if (Input.GetKey(KeyCode.X))
                        {
                            if (dragOffset.x > finalVector.x)
                                firstDirection = SelectObject.Axis.PosX;
                            if (dragOffset.x < finalVector.x)
                                firstDirection = SelectObject.Axis.NegX;
                        }

                        if (Input.GetKey(KeyCode.Y))
                        {
                            if (dragOffset.x > finalVector.x)
                                firstDirection = SelectObject.Axis.PosY;
                            if (dragOffset.x < finalVector.x)
                                firstDirection = SelectObject.Axis.NegY;
                        }

                        if (firstDirection == SelectObject.Axis.NegX || firstDirection == SelectObject.Axis.PosX)
                            selectedKeyframe.values[1] = dragKeyframeValues.y;

                        if (firstDirection == SelectObject.Axis.NegY || firstDirection == SelectObject.Axis.PosY)
                            selectedKeyframe.values[0] = dragKeyframeValues.x;

                        if (firstDirection == SelectObject.Axis.Static || firstDirection == SelectObject.Axis.PosX || firstDirection == SelectObject.Axis.NegX)
                            selectedKeyframe.values[0] = dragKeyframeValues.x - dragOffset.x + (Input.GetKey(KeyCode.LeftShift) ? vector3.x : vector2.x);
                        if (firstDirection == SelectObject.Axis.Static || firstDirection == SelectObject.Axis.PosY || firstDirection == SelectObject.Axis.NegY)
                            selectedKeyframe.values[1] = dragKeyframeValues.y - dragOffset.y + (Input.GetKey(KeyCode.LeftShift) ? vector3.y : vector2.y);

                        break;
                    }
                case 1: {
                        if (!setKeyframeValues)
                        {
                            setKeyframeValues = true;
                            dragKeyframeValues = new Vector2(selectedKeyframe.values[0], selectedKeyframe.values[1]);
                            dragOffset = Input.GetKey(KeyCode.LeftShift) ? vector3 : vector2;
                        }

                        var finalVector = Input.GetKey(KeyCode.LeftShift) ? vector3 : vector2;

                        if (Input.GetKey(KeyCode.LeftControl))
                        {
                            float total = Vector2.Distance(dragOffset, finalVector);

                            selectedKeyframe.values[0] = dragKeyframeValues.x + total;
                            selectedKeyframe.values[1] = dragKeyframeValues.y + total;
                        }
                        else
                        {
                            selectedKeyframe.values[0] = dragKeyframeValues.x - (dragOffset.x + finalVector.x);
                            selectedKeyframe.values[1] = dragKeyframeValues.y - (dragOffset.y + finalVector.y);
                        }

                        break;
                    }
                case 2: {
                        var position = selectionType == SelectionType.Prefab ? new Vector3(prefabObject.events[0].values[0], prefabObject.events[0].values[1], 0f) : beatmapObject.runtimeObject?.visualObject?.gameObject.transform.position ??
                            new Vector3(beatmapObject.events[0].FindLast(x => x.time < AudioManager.inst.CurrentAudioSource.time).values[0], beatmapObject.events[0].FindLast(x => x.time < AudioManager.inst.CurrentAudioSource.time).values[1], 0f);

                        if (!setKeyframeValues)
                        {
                            setKeyframeValues = true;
                            dragKeyframeValuesFloat = selectedKeyframe.values[0];
                            dragOffsetFloat = Input.GetKey(KeyCode.LeftShift) ? RTMath.RoundToNearestNumber(-RTMath.VectorAngle(position, vector2), 15f) : -RTMath.VectorAngle(transform.position, vector2);
                        }

                        selectedKeyframe.values[0] =
                            Input.GetKey(KeyCode.LeftShift) ? RTMath.RoundToNearestNumber(dragKeyframeValuesFloat - dragOffsetFloat + -RTMath.VectorAngle(position, vector2), 15f) :
                            dragKeyframeValuesFloat - dragOffsetFloat + -RTMath.VectorAngle(position, vector2);

                        break;
                    }
            }

            if (selectionType == SelectionType.Object)
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
            if (selectionType == SelectionType.Prefab)
                RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
        }

        public void SetCurrentKeyframe(int type)
        {
            selectedKeyframe = beatmapObject.GetOrCreateKeyframe(type, createKeyframe, useNearest, usePrevious);
            originalValues = selectedKeyframe.values.Copy();
        }

        public void EndDrag(bool cancel)
        {
            if (dragging)
                EditorManager.inst.DisplayNotification("Ended dragging.", 2f, EditorManager.NotificationType.Success);

            dragging = false;

            if (cancel || !selectedKeyframe || originalValues == null || !dragging)
                return;

            dragging = false;
            selectedKeyframe.values = originalValues.Copy();

            if (selectionType == SelectionType.Object)
            {
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                ObjectEditor.inst.Dialog.Timeline.RenderDialog(beatmapObject);
            }

            if (selectionType == SelectionType.Prefab)
            {
                RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                RTPrefabEditor.inst.RenderPrefabObjectTransforms(prefabObject);
            }
        }

        #endregion

        #endregion
    }
}
