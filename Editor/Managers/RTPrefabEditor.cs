using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using Crosstales.FB;
using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Companion.Data.Parameters;
using BetterLegacy.Companion.Entity;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Data.Popups;
using BetterLegacy.Editor.Data.Timeline;
using BetterLegacy.Editor.Managers.Settings;

namespace BetterLegacy.Editor.Managers
{
    // todo:
    // - cleanup this class (consider splitting it into separate managers?)

    /// <summary>
    /// Manages editing <see cref="Prefab"/>s, <see cref="PrefabObject"/>s and <see cref="PrefabType"/>s.
    /// <br></br>Wraps <see cref="PrefabEditor"/>.
    /// </summary>
    public class RTPrefabEditor : BaseEditor<RTPrefabEditor, RTPrefabEditorSettings, PrefabEditor>
    {
        #region Values

        public override PrefabEditor BaseInstance { get => PrefabEditor.inst; set => PrefabEditor.inst = value; }

        public PrefabCreatorDialog PrefabCreatorDialog { get; set; }
        public PrefabObjectEditorDialog PrefabObjectEditor { get; set; }
        public PrefabEditorDialog PrefabEditorDialog { get; set; }

        public RectTransform prefabPopups;
        public Button selectQuickPrefabButton;
        public Text selectQuickPrefabText;

        public InputField prefabCreatorName;
        public InputField prefabCreatorOffset;
        public Slider prefabCreatorOffsetSlider;

        public bool savingToPrefab;
        public Prefab prefabToSaveFrom;

        public string externalSearchStr;
        public string internalSearchStr;

        public bool createInternal;

        public GameObject prefabTypePrefab;
        public GameObject prefabTypeTogglePrefab;

        public Button prefabTypeReloadButton;

        public string NewPrefabTypeID { get; set; }
        public string NewPrefabDescription { get; set; }
        public Sprite NewPrefabIcon { get; set; }

        public PrefabPanel CurrentPrefabPanel { get; set; }
        public List<PrefabPanel> PrefabPanels { get; set; } = new List<PrefabPanel>();

        public static bool ImportPrefabsDirectly { get; set; }

        /// <summary>
        /// Function to run on Prefab Panel selection.
        /// </summary>
        public Action<PrefabPanel> onSelectPrefab;

        /// <summary>
        /// Beatmap Object to target on Prefab Object created.
        /// </summary>
        public BeatmapObject quickPrefabTarget;

        /// <summary>
        /// If the internal Prefabs list is currently allowing Quick Prefab selection.
        /// </summary>
        public bool selectingQuickPrefab;
        /// <summary>
        /// The currently selected quick prefab.
        /// </summary>
        public Prefab currentQuickPrefab;

        public bool shouldCutPrefab;
        public string copiedPrefabPath;

        public bool prefabsLoading;

        GameObject prefabExternalUpAFolderButton;
        GameObject prefabExternalAddButton;

        /// <summary>
        /// If used Prefabs should only show in the Internal Prefab list.
        /// </summary>
        public bool filterUsed;

        /// <summary>
        /// Selected <see cref="BeatmapTheme"/>s for the Prefab Creator.
        /// </summary>
        public List<BeatmapTheme> selectedBeatmapThemes = new List<BeatmapTheme>();
        /// <summary>
        /// Selected <see cref="ModifierBlock"/>s for the Prefab Creator.
        /// </summary>
        public List<ModifierBlock> selectedModifierBlocks = new List<ModifierBlock>();
        /// <summary>
        /// Selected <see cref="SpriteAsset"/>s for the Prefab Creator.
        /// </summary>
        public List<SpriteAsset> selectedSpriteAssets = new List<SpriteAsset>();

        /// <summary>
        /// List of Prefab types.
        /// </summary>
        public List<PrefabType> prefabTypes = new List<PrefabType>();

        /// <summary>
        /// If parent settings should display.
        /// </summary>
        public bool advancedParent;

        /// <summary>
        /// The currently copied instance data.
        /// </summary>
        public PrefabObject copiedInstanceData;

        /// <summary>
        /// If the Prefab Editor prefab icon is collapsed.
        /// </summary>
        public bool CollapseIcon { get; set; } = false;

        /// <summary>
        /// If the Prefab Creator prefab icon is collapsed.
        /// </summary>
        public bool CollapseCreatorIcon { get; set; } = false;

        /// <summary>
        /// Currently selected items for the Prefab Creator that aren't a <see cref="TimelineObject"/>.
        /// </summary>
        public Dictionary<string, bool> selectedForPrefabCreator = new Dictionary<string, bool>();

        /// <summary>
        /// The current selection tab for the Prefab Creator.
        /// </summary>
        public SelectionType prefabCreatorSelectionTab = EditorConfig.Instance.PrefabCreatorDefaultSelectionTab.Value;

        /// <summary>
        /// Selection type for Prefab content.
        /// </summary>
        public enum SelectionType
        {
            /// <summary>
            /// Selection representing a <see cref="TimelineObject"/> which can be a <see cref="BeatmapObject"/>, a <see cref="BackgroundObject"/> and a <see cref="PrefabObject"/>.
            /// </summary>
            TimelineObjects,
            /// <summary>
            /// Selection representing a <see cref="BeatmapTheme"/>.
            /// </summary>
            BeatmapThemes,
            /// <summary>
            /// Selection representing a <see cref="ModifierBlock"/>.
            /// </summary>
            ModifierBlocks,
            /// <summary>
            /// Selection representing a <see cref="SpriteAsset"/>.
            /// </summary>
            Images,
        }

        #endregion

        #region Functions

        public override void OnManagerStart() => CoroutineHelper.WaitUntil(
            () => PrefabEditor.inst && EditorManager.inst && EditorManager.inst.EditorDialogsDictionary.ContainsKey("Prefab Popup") && EditorPrefabHolder.Instance != null && EditorPrefabHolder.Instance.Function1Button,
            Setup);

        public override void OnTick() => PrefabObjectEditor?.ModifiersDialog?.Tick();

        // todo:
        // rework this UI generation code
        void Setup()
        {
            //while (!PrefabEditor.inst || !EditorManager.inst || !EditorManager.inst.EditorDialogsDictionary.ContainsKey("Prefab Popup") || EditorPrefabHolder.Instance == null || !EditorPrefabHolder.Instance.Function1Button)
            //    yield return null;

            // A
            {
                loadingPrefabTypes = true;
                LoadPrefabs();
                PrefabEditor.inst.OffsetLine = PrefabEditor.inst.OffsetLinePrefab.Duplicate(EditorManager.inst.timeline.transform, "offset line");
                PrefabEditor.inst.OffsetLine.transform.AsRT().pivot = Vector2.one;

                var prefabPopup = EditorManager.inst.GetDialog("Prefab Popup").Dialog;
                PrefabEditor.inst.dialog = EditorManager.inst.GetDialog("Prefab Editor").Dialog;
                PrefabEditor.inst.externalPrefabDialog = prefabPopup.Find("external prefabs");
                PrefabEditor.inst.internalPrefabDialog = prefabPopup.Find("internal prefabs");

                EditorContextMenu.AddContextMenu(PrefabEditor.inst.externalPrefabDialog.gameObject,
                        new ButtonElement("Create folder", () =>
                        {
                            RTEditor.inst.ShowFolderCreator(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath), () => { LoadPrefabs(RenderExternalPrefabs); ; RTEditor.inst.HideNameEditor(); });
                        }),
                        new ButtonElement("Create Prefab", () =>
                        {
                            PrefabEditor.inst.OpenDialog();
                            createInternal = false;
                        }),
                        new SpacerElement(),
                        new ButtonElement("Paste", PastePrefab));
                
                EditorContextMenu.AddContextMenu(PrefabEditor.inst.internalPrefabDialog.gameObject,
                    new ButtonElement("Create Prefab", () =>
                    {
                        PrefabEditor.inst.OpenDialog();
                        createInternal = true;
                    }));

                PrefabEditor.inst.externalSearch = PrefabEditor.inst.externalPrefabDialog.Find("search-box/search").GetComponent<InputField>();
                PrefabEditor.inst.internalSearch = PrefabEditor.inst.internalPrefabDialog.Find("search-box/search").GetComponent<InputField>();
                PrefabEditor.inst.externalContent = PrefabEditor.inst.externalPrefabDialog.Find("mask/content");
                PrefabEditor.inst.internalContent = PrefabEditor.inst.internalPrefabDialog.Find("mask/content");

                PrefabEditor.inst.gridSearch = PrefabEditor.inst.dialog.Find("data/selection/search-box/search").GetComponent<InputField>();
                PrefabEditor.inst.gridContent = PrefabEditor.inst.dialog.Find("data/selection/mask/content");

                Destroy(PrefabEditor.inst.dialog.Find("data/type/types").GetComponent<VerticalLayoutGroup>());
            }

            // C
            {
                var transform = PrefabEditor.inst.dialog.Find("data/type/types");

                var list = new List<GameObject>();
                for (int i = 1; i < transform.childCount; i++)
                {
                    var tf = transform.Find($"col_{i}");
                    if (tf)
                        list.Add(tf.gameObject);
                }

                foreach (var go in list)
                    Destroy(go);

                prefabTypeTogglePrefab = transform.GetChild(0).gameObject;
                prefabTypeTogglePrefab.transform.SetParent(transform);
            }

            CreatePrefabTypesPopup();
            StartCoroutine(LoadPrefabTypes());

            prefabPopups = EditorManager.inst.GetDialog("Prefab Popup").Dialog.AsRT();
            selectQuickPrefabButton = PrefabEditor.inst.internalPrefabDialog.Find("select_prefab/select_toggle").GetComponent<Button>();
            selectQuickPrefabText = PrefabEditor.inst.internalPrefabDialog.Find("select_prefab/selected_prefab").GetComponent<Text>();

            EditorContextMenu.AddContextMenu(selectQuickPrefabButton.gameObject,
                new ButtonElement("Assign", () =>
                {
                    selectQuickPrefabText.text = "<color=#669e37>Selecting</color>";
                    StartCoroutine(RefreshInternalPrefabs(true));
                }),
                new ButtonElement("Remove", () =>
                {
                    currentQuickPrefab = null;
                    RenderPopup();
                }),
                new ButtonElement("Select Target", () => EditorTimeline.inst.onSelectTimelineObject = timelineObject =>
                {
                    if (!timelineObject.isBeatmapObject)
                    {
                        quickPrefabTarget = null;
                        return;
                    }

                    quickPrefabTarget = timelineObject.GetData<BeatmapObject>();
                }),
                new ButtonElement("Remove Target", () => quickPrefabTarget = null));

            try
            {
                prefabCreatorName = PrefabEditor.inst.dialog.Find("data/name/input").GetComponent<InputField>();

                prefabCreatorOffsetSlider = PrefabEditor.inst.dialog.Find("data/offset/slider").GetComponent<Slider>();
                prefabCreatorOffset = PrefabEditor.inst.dialog.Find("data/offset/input").GetComponent<InputField>();

                // Editor Theme
                {
                    #region External

                    EditorThemeManager.ApplyGraphic(PrefabEditor.inst.externalPrefabDialog.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Bottom_Left_I);

                    var externalPanel = PrefabEditor.inst.externalPrefabDialog.Find("Panel");
                    externalPanel.AsRT().sizeDelta = new Vector2(32f, 32f);
                    EditorThemeManager.ApplyGraphic(externalPanel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

                    var externalClose = externalPanel.Find("x").GetComponent<Button>();
                    Destroy(externalClose.GetComponent<Animator>());
                    externalClose.transition = Selectable.Transition.ColorTint;
                    externalClose.image.rectTransform.anchoredPosition = Vector2.zero;
                    EditorThemeManager.ApplySelectable(externalClose, ThemeGroup.Close);
                    EditorThemeManager.ApplyGraphic(externalClose.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

                    EditorThemeManager.ApplyLightText(externalPanel.Find("Text").GetComponent<Text>());

                    EditorThemeManager.ApplyScrollbar(PrefabEditor.inst.externalPrefabDialog.transform.Find("Scrollbar").GetComponent<Scrollbar>(), scrollbarRoundedSide: SpriteHelper.RoundedSide.Bottom_Right_I);

                    EditorThemeManager.ApplyInputField(PrefabEditor.inst.externalSearch, ThemeGroup.Search_Field_2);

                    #endregion

                    #region Internal

                    EditorThemeManager.ApplyGraphic(PrefabEditor.inst.internalPrefabDialog.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Bottom_Left_I);

                    var internalPanel = PrefabEditor.inst.internalPrefabDialog.Find("Panel");
                    internalPanel.AsRT().sizeDelta = new Vector2(32f, 32f);
                    EditorThemeManager.ApplyGraphic(internalPanel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

                    var internalClose = internalPanel.Find("x").GetComponent<Button>();
                    Destroy(internalClose.GetComponent<Animator>());
                    internalClose.transition = Selectable.Transition.ColorTint;
                    internalClose.image.rectTransform.anchoredPosition = Vector2.zero;
                    EditorThemeManager.ApplySelectable(internalClose, ThemeGroup.Close);
                    EditorThemeManager.ApplyGraphic(internalClose.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

                    EditorThemeManager.ApplyLightText(internalPanel.Find("Text").GetComponent<Text>());

                    EditorThemeManager.ApplyScrollbar(PrefabEditor.inst.internalPrefabDialog.transform.Find("Scrollbar").GetComponent<Scrollbar>(), scrollbarRoundedSide: SpriteHelper.RoundedSide.Bottom_Right_I);

                    EditorThemeManager.ApplyInputField(PrefabEditor.inst.internalSearch, ThemeGroup.Search_Field_2);

                    EditorThemeManager.ApplyGraphic(PrefabEditor.inst.internalPrefabDialog.Find("select_prefab").GetComponent<Image>(), ThemeGroup.Background_2, true, roundedSide: SpriteHelper.RoundedSide.Bottom_Left_I);

                    EditorThemeManager.ApplySelectable(selectQuickPrefabButton, ThemeGroup.Function_2);
                    EditorThemeManager.ApplyGraphic(selectQuickPrefabButton.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Function_2_Text);
                    EditorThemeManager.ApplyGraphic(selectQuickPrefabText, ThemeGroup.Light_Text);

                    #endregion
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            try
            {
                PrefabCreatorDialog = new PrefabCreatorDialog();
                PrefabCreatorDialog.Init();
                PrefabObjectEditor = new PrefabObjectEditorDialog();
                PrefabObjectEditor.Init();
                PrefabEditorDialog = new PrefabEditorDialog();
                PrefabEditorDialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        #region Prefab Objects

        public void UpdateOffsets(PrefabObject currentPrefab)
        {
            var prefabObjects = GameData.Current.prefabObjects.FindAll(x => x.prefabID == currentPrefab.prefabID);
            var isObjectLayer = EditorTimeline.inst.layerType == EditorTimeline.LayerType.Objects;
            for (int i = 0; i < prefabObjects.Count; i++)
            {
                var prefabObject = prefabObjects[i];

                if (isObjectLayer && prefabObject.editorData.Layer == EditorTimeline.inst.Layer)
                    EditorTimeline.inst.GetTimelineObject(prefabObject).RenderPosLength();

                RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TIME, false);
            }
            RTLevel.Current?.RecalculateObjectStates();
        }

        public void OpenPrefabObjectDialog()
        {
            if (EditorTimeline.inst.CurrentSelection && EditorTimeline.inst.CurrentSelection.isPrefabObject)
            {
                OpenPrefabObjectDialog(EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>());
                return;
            }

            EditorManager.inst.DisplayNotification("Prefab Object was null, so cannot open the editor.", 3f, EditorManager.NotificationType.Error);
            EditorDialog.CurrentDialog?.Close();
        }
        
        public void OpenPrefabObjectDialog(PrefabObject prefabObject)
        {
            if (!prefabObject)
            {
                EditorManager.inst.DisplayNotification("Prefab Object was null, so cannot open the editor.", 3f, EditorManager.NotificationType.Error);
                EditorDialog.CurrentDialog?.Close();
                return;
            }

            PrefabObjectEditor.Open();
            RenderPrefabObjectDialog(prefabObject);
        }

        #region Render Dialog

        public void RenderPrefabObjectDialog(PrefabObject prefabObject)
        {
            var prefab = prefabObject.GetPrefab();

            RenderPrefabObjectTags(prefabObject);

            RenderPrefabObjectStartTime(prefabObject);
            RenderPrefabObjectAutokill(prefabObject, prefab);
            RenderPrefabObjectParent(prefabObject);
            RenderPrefabObjectTransforms(prefabObject);
            RenderPrefabObjectRepeat(prefabObject);
            RenderPrefabObjectSpeed(prefabObject);

            RenderPrefabObjectInstanceData(prefabObject);

            RenderPrefabObjectLayer(prefabObject);
            RenderPrefabObjectBin(prefabObject);
            RenderPrefabObjectIndex(prefabObject);
            RenderPrefabObjectGroup(prefabObject);
            RenderEditorColors(prefabObject);

            RenderPrefabObjectInspector(prefabObject);

            RenderPrefabObjectOffset(prefabObject, prefab);
            RenderPrefabObjectName(prefab);
            RenderPrefabObjectType(prefab);
            RenderPrefabObjectDefault(prefabObject, prefab);

            PrefabObjectEditor.SavePrefabButton.OnClick.NewListener(() =>
            {
                RTEditor.inst.PrefabPopups.Open();
                RTEditor.inst.PrefabPopups.GameObject.transform.GetChild(0).gameObject.SetActive(false);

                if (PrefabEditor.inst.externalContent)
                    StartCoroutine(IRenderExternalPrefabs());

                savingToPrefab = true;
                prefabToSaveFrom = prefab;

                EditorManager.inst.DisplayNotification("Select an External Prefab to apply changes to.", 2f);
            });

            if (ModCompatibility.UnityExplorerInstalled && PrefabObjectEditor.InspectPrefab)
                PrefabObjectEditor.InspectPrefab.OnClick.NewListener(() => ModCompatibility.Inspect(prefab));

            RenderPrefabObjectInfo(prefab);

            CoroutineHelper.StartCoroutine(PrefabObjectEditor.ModifiersDialog.RenderModifiers(prefabObject));
        }

        public void RenderPrefabObjectTags(PrefabObject prefabObject) => RTEditor.inst.RenderTags(prefabObject, PrefabObjectEditor);

        public void RenderPrefabObjectStartTime(PrefabObject prefabObject)
        {
            PrefabObjectEditor.StartTimeField.lockToggle.SetIsOnWithoutNotify(prefabObject.editorData.locked);
            PrefabObjectEditor.StartTimeField.lockToggle.onValueChanged.NewListener(_val =>
            {
                prefabObject.editorData.locked = _val;
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(prefabObject));
            });

            PrefabObjectEditor.CollapseToggle.SetIsOnWithoutNotify(prefabObject.editorData.collapse);
            PrefabObjectEditor.CollapseToggle.onValueChanged.NewListener(_val =>
            {
                prefabObject.editorData.collapse = _val;
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(prefabObject));
            });

            PrefabObjectEditor.StartTimeField.SetTextWithoutNotify(prefabObject.StartTime.ToString());
            PrefabObjectEditor.StartTimeField.OnValueChanged.NewListener(_val =>
            {
                if (prefabObject.editorData.locked)
                    return;

                if (float.TryParse(_val, out float n))
                {
                    n = Mathf.Clamp(n, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                    prefabObject.StartTime = n;
                    RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TIME);
                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(prefabObject));
                }
                else
                    EditorManager.inst.DisplayNotification("Text is not correct format!", 1f, EditorManager.NotificationType.Error);
            });

            TriggerHelper.IncreaseDecreaseButtons(PrefabObjectEditor.StartTimeField);
            TriggerHelper.AddEventTriggers(PrefabObjectEditor.StartTimeField.inputField.gameObject, TriggerHelper.ScrollDelta(PrefabObjectEditor.StartTimeField.inputField));

            EditorContextMenu.AddContextMenu(PrefabObjectEditor.StartTimeField.inputField.gameObject,
                new ButtonElement("Go to Start Time", () => AudioManager.inst.SetMusicTime(prefabObject.StartTime)),
                new ButtonElement("Go to Spawn Time", () => AudioManager.inst.SetMusicTime(prefabObject.StartTime + prefabObject.GetPrefab().offset)));

            PrefabObjectEditor.StartTimeField.middleButton.onClick.NewListener(() =>
            {
                if (prefabObject.editorData.locked)
                    return;

                PrefabObjectEditor.StartTimeField.inputField.text = AudioManager.inst.CurrentAudioSource.time.ToString();
            });
        }

        public void RenderPrefabObjectAutokill(PrefabObject prefabObject, Prefab prefab)
        {
            PrefabObjectEditor.LeftContent.Find("tod-dropdown label").gameObject.SetActive(RTEditor.ShowModdedUI);
            PrefabObjectEditor.AutokillDropdown.gameObject.SetActive(RTEditor.ShowModdedUI);
            PrefabObjectEditor.AutokillField.gameObject.SetActive(RTEditor.ShowModdedUI);

            if (!RTEditor.ShowModdedUI)
                return;

            PrefabObjectEditor.AutokillDropdown.SetValueWithoutNotify((int)prefabObject.autoKillType);
            PrefabObjectEditor.AutokillDropdown.onValueChanged.NewListener(_val =>
            {
                prefabObject.autoKillType = (PrefabAutoKillType)_val;
                RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.AUTOKILL);
            });

            PrefabObjectEditor.AutokillField.SetTextWithoutNotify(prefabObject.autoKillOffset.ToString());
            PrefabObjectEditor.AutokillField.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    prefabObject.autoKillOffset = num;
                    if (prefabObject.autoKillType != PrefabAutoKillType.Regular)
                        RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.AUTOKILL);
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(PrefabObjectEditor.AutokillField);
            TriggerHelper.AddEventTriggers(PrefabObjectEditor.AutokillField.inputField.gameObject, TriggerHelper.ScrollDelta(PrefabObjectEditor.AutokillField.inputField));

            PrefabObjectEditor.AutokillField.middleButton.onClick.NewListener(() =>
            {
                prefabObject.autoKillOffset = prefabObject.autoKillType == PrefabAutoKillType.StartTimeOffset ? prefabObject.StartTime + prefab.offset :
                                                prefabObject.autoKillType == PrefabAutoKillType.SongTime ? AudioManager.inst.CurrentAudioSource.time : -1f;

                if (prefabObject.autoKillType != PrefabAutoKillType.Regular)
                    RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.AUTOKILL);
                RenderPrefabObjectAutokill(prefabObject, prefab);
            });
        }

        public void RenderPrefabObjectParent(PrefabObject prefabObject)
        {
            PrefabObjectEditor.OffsetParentDesyncToggle.SetIsOnWithoutNotify(prefabObject.offsetParentDesyncTime);
            PrefabObjectEditor.OffsetParentDesyncToggle.OnValueChanged.NewListener(_val =>
            {
                prefabObject.offsetParentDesyncTime = _val;
                RTLevel.Current?.UpdatePrefab(prefabObject);
            });
            PrefabObjectEditor.ParentSelfToggle.SetIsOnWithoutNotify(prefabObject.parentSelf);
            PrefabObjectEditor.ParentSelfToggle.OnValueChanged.NewListener(_val =>
            {
                prefabObject.parentSelf = _val;
                RTLevel.Current?.UpdatePrefab(prefabObject);
            });
            RTEditor.inst.RenderParent(prefabObject, PrefabObjectEditor);
        }

        public void RenderPrefabObjectTransforms(PrefabObject prefabObject)
        {
            var types = new string[]
            {
                "position",
                "scale",
                "rotation"
            };

            for (int i = 0; i < 3; i++)
            {
                int index = i;

                var currentKeyframe = prefabObject.events[index];

                var inputFieldX = PrefabObjectEditor.TransformFields[index][0];
                var r_inputFieldX = PrefabObjectEditor.RandomTransformFields[index][0];

                inputFieldX.inputField.SetTextWithoutNotify(currentKeyframe.values[0].ToString());
                inputFieldX.inputField.onValueChanged.NewListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentKeyframe.values[0] = num;
                        RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                    }
                });
                inputFieldX.inputField.onEndEdit.NewListener(_val =>
                {
                    var variables = new Dictionary<string, float>
                    {
                        { "currentValue", currentKeyframe.values[0] }
                    };

                    if (!float.TryParse(_val, out float n) && RTMath.TryParse(_val, currentKeyframe.values[0], variables, out float calc))
                        inputFieldX.inputField.text = calc.ToString();
                });

                var r_type = "r_" + types[index];

                r_inputFieldX.inputField.SetTextWithoutNotify(currentKeyframe.randomValues[0].ToString());
                r_inputFieldX.inputField.onValueChanged.NewListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentKeyframe.randomValues[0] = num;
                        RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                    }
                });
                r_inputFieldX.inputField.onEndEdit.NewListener(_val =>
                {
                    var variables = new Dictionary<string, float>
                    {
                        { "currentValue", currentKeyframe.values[0] }
                    };

                    if (!float.TryParse(_val, out float n) && RTMath.TryParse(_val, currentKeyframe.values[0], variables, out float calc))
                        r_inputFieldX.inputField.text = calc.ToString();
                });

                var toggles = index switch
                {
                    0 => PrefabObjectEditor.PositionRandomToggles,
                    1 => PrefabObjectEditor.ScaleRandomToggles,
                    2 => PrefabObjectEditor.RotationRandomToggles,
                    _ => null,
                };
                if (toggles != null)
                {
                    for (int j = 0; j < toggles.Length; j++)
                    {
                        var toggle = toggles[j];
                        var randomIndex = j >= 2 ? j + 1 : j;
                        toggle.SetIsOnWithoutNotify(currentKeyframe.random == randomIndex);
                        toggle.onValueChanged.NewListener(_val =>
                        {
                            currentKeyframe.random = randomIndex;
                            RenderPrefabObjectTransforms(prefabObject);
                            RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                        });
                    }
                }

                PrefabObjectEditor.LeftContent.Find(r_type + " label").gameObject.SetActive(currentKeyframe.random != 0 && RTEditor.ShowModdedUI);
                PrefabObjectEditor.LeftContent.Find(r_type).gameObject.SetActive(currentKeyframe.random != 0 && RTEditor.ShowModdedUI);

                if (index != 2)
                {
                    var inputFieldY = PrefabObjectEditor.TransformFields[index][1];
                    var r_inputFieldY = PrefabObjectEditor.RandomTransformFields[index][1];

                    inputFieldY.inputField.SetTextWithoutNotify(currentKeyframe.values[1].ToString());
                    inputFieldY.inputField.onValueChanged.NewListener(_val =>
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            currentKeyframe.values[1] = num;
                            RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                        }
                    });
                    inputFieldY.inputField.onEndEdit.NewListener(_val =>
                    {
                        var variables = new Dictionary<string, float>
                        {
                            { "currentValue", currentKeyframe.values[1] }
                        };

                        if (!float.TryParse(_val, out float n) && RTMath.TryParse(_val, currentKeyframe.values[1], variables, out float calc))
                            inputFieldY.inputField.text = calc.ToString();
                    });

                    TriggerHelper.IncreaseDecreaseButtons(inputFieldX);
                    TriggerHelper.IncreaseDecreaseButtons(inputFieldY);

                    TriggerHelper.AddEventTriggers(inputFieldX.inputField.gameObject,
                        TriggerHelper.ScrollDelta(inputFieldX.inputField, multi: true),
                        TriggerHelper.ScrollDeltaVector2(inputFieldX.inputField, inputFieldY.inputField, 0.1f, 10f));
                    TriggerHelper.AddEventTriggers(inputFieldY.inputField.gameObject,
                        TriggerHelper.ScrollDelta(inputFieldY.inputField, multi: true),
                        TriggerHelper.ScrollDeltaVector2(inputFieldX.inputField, inputFieldY.inputField, 0.1f, 10f));

                    r_inputFieldY.inputField.SetTextWithoutNotify(currentKeyframe.randomValues[1].ToString());
                    r_inputFieldY.inputField.onValueChanged.NewListener(_val =>
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            currentKeyframe.randomValues[1] = num;
                            RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                        }
                    });
                    r_inputFieldY.inputField.onEndEdit.NewListener(_val =>
                    {
                        var variables = new Dictionary<string, float>
                        {
                            { "currentValue", currentKeyframe.randomValues[1] }
                        };

                        if (!float.TryParse(_val, out float n) && RTMath.TryParse(_val, currentKeyframe.randomValues[1], variables, out float calc))
                            r_inputFieldY.inputField.text = calc.ToString();
                    });

                    TriggerHelper.IncreaseDecreaseButtons(r_inputFieldX);
                    TriggerHelper.IncreaseDecreaseButtons(r_inputFieldY);

                    TriggerHelper.AddEventTriggers(r_inputFieldX.gameObject,
                        TriggerHelper.ScrollDelta(r_inputFieldX.inputField, multi: true),
                        TriggerHelper.ScrollDeltaVector2(r_inputFieldX.inputField, r_inputFieldY.inputField, 0.1f, 10f));
                    TriggerHelper.AddEventTriggers(r_inputFieldY.inputField.gameObject,
                        TriggerHelper.ScrollDelta(r_inputFieldY.inputField, multi: true),
                        TriggerHelper.ScrollDeltaVector2(r_inputFieldX.inputField, r_inputFieldY.inputField, 0.1f, 10f));
                }
                else
                {
                    TriggerHelper.IncreaseDecreaseButtons(inputFieldX, 15f, 3f);
                    TriggerHelper.AddEventTriggers(inputFieldX.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldX.inputField, 15f, 3f));

                    TriggerHelper.IncreaseDecreaseButtons(r_inputFieldX, 15f, 3f);
                    TriggerHelper.AddEventTriggers(r_inputFieldX.inputField.gameObject, TriggerHelper.ScrollDelta(r_inputFieldX.inputField, 15f, 3f));
                }

                var randomIntervalActive = currentKeyframe.RandomType != RandomType.None && currentKeyframe.RandomType != RandomType.Toggle;

                var randomIntervalField = PrefabObjectEditor.RandomIntervalFields[index];
                randomIntervalField.gameObject.SetActive(randomIntervalActive);
                if (!randomIntervalActive)
                    continue;

                randomIntervalField.SetTextWithoutNotify((currentKeyframe.randomValues.Length > 2 ? currentKeyframe.randomValues[2] : 0f).ToString());
                randomIntervalField.onValueChanged.NewListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentKeyframe.randomValues[2] = num;
                        RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                    }
                });

                TriggerHelper.InversableField(randomIntervalField);
                TriggerHelper.AddEventTriggers(randomIntervalField.gameObject, TriggerHelper.ScrollDelta(randomIntervalField, max: float.MaxValue));
            }

            PrefabObjectEditor.DepthField.SetTextWithoutNotify(prefabObject.depth.ToString());
            PrefabObjectEditor.DepthField.OnValueChanged.NewListener(_val =>
            {
                if (!float.TryParse(_val, out float num))
                    return;

                prefabObject.depth = num;
                RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
            });
            TriggerHelper.IncreaseDecreaseButtons(PrefabObjectEditor.DepthField);
            TriggerHelper.AddEventTriggers(PrefabObjectEditor.DepthField.gameObject, TriggerHelper.ScrollDelta(PrefabObjectEditor.DepthField.inputField));
        }

        public void RenderPrefabObjectRepeat(PrefabObject prefabObject)
        {
            PrefabObjectEditor.LeftContent.Find("repeat label").gameObject.SetActive(RTEditor.ShowModdedUI);
            PrefabObjectEditor.LeftContent.Find("repeat").gameObject.SetActive(RTEditor.ShowModdedUI);

            if (!RTEditor.ShowModdedUI)
                return;

            PrefabObjectEditor.RepeatCountField.SetTextWithoutNotify(Mathf.Clamp(prefabObject.RepeatCount, 0, 1000).ToString());
            PrefabObjectEditor.RepeatCountField.OnValueChanged.NewListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                {
                    num = Mathf.Clamp(num, 0, 1000);
                    prefabObject.RepeatCount = num;
                    RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.REPEAT);
                }
            });

            TriggerHelper.IncreaseDecreaseButtonsInt(PrefabObjectEditor.RepeatCountField, max: 1000);
            TriggerHelper.AddEventTriggers(PrefabObjectEditor.RepeatCountField.inputField.gameObject, TriggerHelper.ScrollDeltaInt(PrefabObjectEditor.RepeatCountField.inputField, max: 1000));

            PrefabObjectEditor.RepeatOffsetTimeField.SetTextWithoutNotify(Mathf.Clamp(prefabObject.RepeatOffsetTime, 0f, 60f).ToString());
            PrefabObjectEditor.RepeatOffsetTimeField.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    num = Mathf.Clamp(num, 0f, 60f);
                    prefabObject.RepeatOffsetTime = num;
                    RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TIME);
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(PrefabObjectEditor.RepeatOffsetTimeField, max: 60f);
            TriggerHelper.AddEventTriggers(PrefabObjectEditor.RepeatOffsetTimeField.inputField.gameObject, TriggerHelper.ScrollDelta(PrefabObjectEditor.RepeatOffsetTimeField.inputField, max: 60f));
        }

        public void RenderPrefabObjectSpeed(PrefabObject prefabObject)
        {
            PrefabObjectEditor.LeftContent.Find("speed label").gameObject.SetActive(RTEditor.ShowModdedUI);
            PrefabObjectEditor.SpeedField.gameObject.SetActive(RTEditor.ShowModdedUI);

            if (!RTEditor.ShowModdedUI)
                return;

            PrefabObjectEditor.SpeedField.SetTextWithoutNotify(prefabObject.Speed.ToString());
            PrefabObjectEditor.SpeedField.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    prefabObject.Speed = num;
            });

            TriggerHelper.IncreaseDecreaseButtons(PrefabObjectEditor.SpeedField, min: 0.1f, max: PrefabObject.MAX_PREFAB_OBJECT_SPEED);
            TriggerHelper.AddEventTriggers(PrefabObjectEditor.SpeedField.inputField.gameObject, TriggerHelper.ScrollDelta(PrefabObjectEditor.SpeedField.inputField, min: 0.1f, max: PrefabObject.MAX_PREFAB_OBJECT_SPEED));
        }
        
        public void RenderPrefabObjectInstanceData(PrefabObject prefabObject)
        {
            PrefabObjectEditor.CopyInstanceDataButton.OnClick.NewListener(() =>
            {
                copiedInstanceData = prefabObject.Copy();
                EditorManager.inst.DisplayNotification($"Copied Prefab instance data.", 2f, EditorManager.NotificationType.Success);
                RenderPrefabObjectInstanceData(prefabObject);
            });
            PrefabObjectEditor.PasteInstanceDataButton.OnClick.NewListener(() =>
            {
                if (!copiedInstanceData)
                {
                    EditorManager.inst.DisplayNotification($"No copied data.", 2f, EditorManager.NotificationType.Warning);
                    return;
                }

                PasteInstanceData(prefabObject);
                RenderPrefabObjectDialog(prefabObject);

                EditorManager.inst.DisplayNotification($"Pasted Prefab instance data.", 2f, EditorManager.NotificationType.Success);
            });
            PrefabObjectEditor.RemoveInstanceDataButton.gameObject.SetActive(copiedInstanceData);
            PrefabObjectEditor.RemoveInstanceDataButton.OnClick.NewListener(() =>
            {
                copiedInstanceData = null;
                EditorManager.inst.DisplayNotification($"Removed copied Prefab instance data.", 2f, EditorManager.NotificationType.Success);
                RenderPrefabObjectInstanceData(prefabObject);
            });
        }

        public void RenderPrefabObjectLayer(PrefabObject prefabObject) => RTEditor.inst.RenderEditorLayer(prefabObject, PrefabObjectEditor);

        public void RenderPrefabObjectBin(PrefabObject prefabObject)
        {
            PrefabObjectEditor.BinSlider.onValueChanged.ClearAll();
            PrefabObjectEditor.BinSlider.maxValue = EditorTimeline.inst.BinCount;
            PrefabObjectEditor.BinSlider.SetValueWithoutNotify(prefabObject.editorData.Bin);
            PrefabObjectEditor.BinSlider.onValueChanged.NewListener(_val =>
            {
                prefabObject.editorData.Bin = Mathf.Clamp((int)_val, 0, EditorTimeline.inst.BinCount);

                // Since bin has no effect on the physical object, we will only need to update the timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(prefabObject));
            });
        }

        public void RenderPrefabObjectIndex(PrefabObject prefabObject)
        {
            if (!PrefabObjectEditor.EditorIndexField)
                return;

            PrefabObjectEditor.LeftContent.Find("indexer_label").gameObject.SetActive(RTEditor.ShowModdedUI);
            PrefabObjectEditor.EditorIndexField.gameObject.SetActive(RTEditor.ShowModdedUI);

            if (!RTEditor.ShowModdedUI)
                return;

            var currentIndex = GameData.Current.prefabObjects.FindIndex(x => x.id == prefabObject.id);
            PrefabObjectEditor.EditorIndexField.OnValueChanged.ClearAll();
            PrefabObjectEditor.EditorIndexField.SetTextWithoutNotify(currentIndex.ToString());
            PrefabObjectEditor.EditorIndexField.OnEndEdit.NewListener(_val =>
            {
                if (currentIndex < 0)
                {
                    EditorManager.inst.DisplayNotification($"Object is not in the Beatmap Object list.", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                if (int.TryParse(_val, out int index))
                {
                    index = Mathf.Clamp(index, 0, GameData.Current.prefabObjects.Count - 1);
                    if (currentIndex == index)
                        return;

                    GameData.Current.prefabObjects.Move(currentIndex, index);
                    EditorTimeline.inst.UpdateTransformIndex();
                    RenderPrefabObjectIndex(prefabObject);
                }
            });

            PrefabObjectEditor.EditorIndexField.leftGreaterButton.onClick.NewListener(() =>
            {
                var index = GameData.Current.prefabObjects.FindIndex(x => x.id == prefabObject.id);
                if (index <= 0)
                {
                    EditorManager.inst.DisplayNotification("Could not move object back since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                GameData.Current.prefabObjects.Move(index, 0);
                EditorTimeline.inst.UpdateTransformIndex();
                RenderPrefabObjectIndex(prefabObject);
            });
            PrefabObjectEditor.EditorIndexField.leftButton.onClick.NewListener(() =>
            {
                var index = GameData.Current.prefabObjects.FindIndex(x => x.id == prefabObject.id);
                if (index <= 0)
                {
                    EditorManager.inst.DisplayNotification("Could not move object back since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                GameData.Current.prefabObjects.Move(index, index - 1);
                EditorTimeline.inst.UpdateTransformIndex();
                RenderPrefabObjectIndex(prefabObject);
            });
            PrefabObjectEditor.EditorIndexField.rightButton.onClick.NewListener(() =>
            {
                var index = GameData.Current.prefabObjects.FindIndex(x => x.id == prefabObject.id);
                if (index >= GameData.Current.prefabObjects.Count - 1)
                {
                    EditorManager.inst.DisplayNotification("Could not move object forwards since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                GameData.Current.prefabObjects.Move(index, index + 1);
                EditorTimeline.inst.UpdateTransformIndex();
                RenderPrefabObjectIndex(prefabObject);
            });
            PrefabObjectEditor.EditorIndexField.rightGreaterButton.onClick.NewListener(() =>
            {
                var index = GameData.Current.prefabObjects.FindIndex(x => x.id == prefabObject.id);
                if (index >= GameData.Current.prefabObjects.Count - 1)
                {
                    EditorManager.inst.DisplayNotification("Could not move object forwards since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                GameData.Current.prefabObjects.Move(index, GameData.Current.prefabObjects.Count - 1);
                EditorTimeline.inst.UpdateTransformIndex();
                RenderPrefabObjectIndex(prefabObject);
            });

            TriggerHelper.AddEventTriggers(PrefabObjectEditor.EditorIndexField.gameObject, TriggerHelper.CreateEntry(EventTriggerType.Scroll, eventData =>
            {
                var pointerEventData = (PointerEventData)eventData;

                if (!int.TryParse(PrefabObjectEditor.EditorIndexField.inputField.text, out int index))
                    return;

                if (pointerEventData.scrollDelta.y < 0f)
                    index -= (Input.GetKey(EditorConfig.Instance.ScrollwheelLargeAmountKey.Value) ? 10 : 1);
                if (pointerEventData.scrollDelta.y > 0f)
                    index += (Input.GetKey(EditorConfig.Instance.ScrollwheelLargeAmountKey.Value) ? 10 : 1);

                if (index < 0)
                {
                    EditorManager.inst.DisplayNotification("Could not move object back since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                    return;
                }
                if (index > GameData.Current.prefabObjects.Count - 1)
                {
                    EditorManager.inst.DisplayNotification("Could not move object forwards since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                GameData.Current.prefabObjects.Move(currentIndex, index);
                EditorTimeline.inst.UpdateTransformIndex();
                RenderPrefabObjectIndex(prefabObject);
            }));

            EditorContextMenu.AddContextMenu(PrefabObjectEditor.EditorIndexField.inputField.gameObject,
                EditorContextMenu.GetIndexerFunctions(currentIndex, GameData.Current.prefabObjects));
        }

        public void RenderPrefabObjectGroup(PrefabObject prefabObject)
        {
            PrefabObjectEditor.EditorGroupField.SetTextWithoutNotify(prefabObject.EditorData.editorGroup);
            PrefabObjectEditor.EditorGroupField.onValueChanged.NewListener(_val =>
            {
                prefabObject.EditorData.editorGroup = _val;
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(prefabObject));
            });
        }

        public void RenderEditorColors(PrefabObject prefabObject)
        {
            PrefabObjectEditor.BaseColorField.SetTextWithoutNotify(prefabObject.editorData.color);
            PrefabObjectEditor.BaseColorField.onValueChanged.NewListener(_val =>
            {
                prefabObject.editorData.color = _val;
                prefabObject.timelineObject?.RenderVisibleState(false);
            });
            var baseColorContextMenu = PrefabObjectEditor.BaseColorField.gameObject.GetOrAddComponent<ContextClickable>();
            baseColorContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                    prefabObject.timelineObject?.ShowColorContextMenu(PrefabObjectEditor.BaseColorField, prefabObject.editorData.color);
            };

            PrefabObjectEditor.SelectColorField.SetTextWithoutNotify(prefabObject.editorData.selectedColor);
            PrefabObjectEditor.SelectColorField.onValueChanged.NewListener(_val =>
            {
                prefabObject.editorData.selectedColor = _val;
                prefabObject.timelineObject?.RenderVisibleState(false);
            });
            var selectColorContextMenu = PrefabObjectEditor.SelectColorField.gameObject.GetOrAddComponent<ContextClickable>();
            selectColorContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                    prefabObject.timelineObject?.ShowColorContextMenu(PrefabObjectEditor.SelectColorField, prefabObject.editorData.selectedColor);
            };

            PrefabObjectEditor.TextColorField.SetTextWithoutNotify(prefabObject.editorData.textColor);
            PrefabObjectEditor.TextColorField.onValueChanged.NewListener(_val =>
            {
                prefabObject.editorData.textColor = _val;
                prefabObject.timelineObject?.Render();
            });
            var textColorContextMenu = PrefabObjectEditor.TextColorField.gameObject.GetOrAddComponent<ContextClickable>();
            textColorContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                    prefabObject.timelineObject?.ShowColorContextMenu(PrefabObjectEditor.TextColorField, prefabObject.editorData.textColor);
            };

            PrefabObjectEditor.MarkColorField.SetTextWithoutNotify(prefabObject.editorData.markColor);
            PrefabObjectEditor.MarkColorField.onValueChanged.NewListener(_val =>
            {
                prefabObject.editorData.markColor = _val;
                prefabObject.timelineObject?.Render();
            });
            var markColorContextMenu = PrefabObjectEditor.MarkColorField.gameObject.GetOrAddComponent<ContextClickable>();
            markColorContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                    prefabObject.timelineObject?.ShowColorContextMenu(PrefabObjectEditor.MarkColorField, prefabObject.editorData.markColor);
            };
        }

        public void RenderPrefabObjectInspector(PrefabObject prefabObject)
        {
            if (!ModCompatibility.UnityExplorerInstalled)
                return;

            PrefabObjectEditor.LeftContent.Find("inspect label").gameObject.SetActive(RTEditor.ShowModdedUI);
            PrefabObjectEditor.InspectPrefabObject.gameObject.SetActive(RTEditor.ShowModdedUI);
            PrefabObjectEditor.InspectRuntimeObjectButton.gameObject.SetActive(RTEditor.ShowModdedUI);
            PrefabObjectEditor.InspectTimelineObject.gameObject.SetActive(RTEditor.ShowModdedUI);

            if (!RTEditor.ShowModdedUI)
                return;

            PrefabObjectEditor.InspectPrefabObject.OnClick.NewListener(() => ModCompatibility.Inspect(prefabObject));
            PrefabObjectEditor.InspectRuntimeObjectButton.OnClick.NewListener(() => ModCompatibility.Inspect(prefabObject.runtimeObject));
            PrefabObjectEditor.InspectTimelineObject.OnClick.NewListener(() => ModCompatibility.Inspect(EditorTimeline.inst.GetTimelineObject(prefabObject)));
        }
        
        public void RenderPrefabObjectOffset(PrefabObject prefabObject, Prefab prefab)
        {
            PrefabObjectEditor.OffsetField.SetTextWithoutNotify(prefab.offset.ToString());
            PrefabObjectEditor.OffsetField.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float offset))
                {
                    prefab.offset = offset;
                    UpdateOffsets(prefabObject);
                }
            });
            TriggerHelper.IncreaseDecreaseButtons(PrefabObjectEditor.OffsetField.inputField, t: PrefabObjectEditor.OffsetField.transform);
            TriggerHelper.AddEventTriggers(PrefabObjectEditor.OffsetField.inputField.gameObject, TriggerHelper.ScrollDelta(PrefabObjectEditor.OffsetField.inputField));

            EditorContextMenu.AddContextMenu(PrefabObjectEditor.OffsetField.inputField.gameObject,
                new ButtonElement("Set to Timeline Cursor", () =>
                {
                    var distance = AudioManager.inst.CurrentAudioSource.time - prefabObject.StartTime;

                    prefab.offset -= distance;

                    var prefabObjects = GameData.Current.prefabObjects.FindAll(x => x.prefabID == prefabObject.prefabID);
                    var isObjectLayer = EditorTimeline.inst.layerType == EditorTimeline.LayerType.Objects;
                    for (int i = 0; i < prefabObjects.Count; i++)
                    {
                        var prefabObj = prefabObjects[i];
                        prefabObj.StartTime += distance;

                        if (isObjectLayer && prefabObj.editorData.Layer == EditorTimeline.inst.Layer)
                            EditorTimeline.inst.GetTimelineObject(prefabObj).RenderPosLength();

                        RTLevel.Current?.UpdatePrefab(prefabObj, PrefabObjectContext.TIME, false);
                    }
                    RTLevel.Current?.RecalculateObjectStates();
                }));
        }

        public void RenderPrefabObjectName(Prefab prefab)
        {
            PrefabObjectEditor.NameField.SetTextWithoutNotify(prefab.name);
            PrefabObjectEditor.NameField.onValueChanged.NewListener(_val =>
            {
                prefab.name = _val;
                foreach (var prefabObject in GameData.Current.prefabObjects.FindAll(x => x.prefabID == prefab.id))
                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(prefabObject));
            });
        }

        public void RenderPrefabObjectType(Prefab prefab)
        {
            var prefabType = prefab.GetPrefabType();
            PrefabObjectEditor.PrefabTypeSelectorButton.Color = prefabType.color;
            PrefabObjectEditor.PrefabTypeSelectorButton.Text = prefabType.name;
            PrefabObjectEditor.PrefabTypeSelectorButton.OnClick.NewListener(() =>
            {
                OpenPrefabTypePopup(prefab.typeID, id =>
                {
                    prefab.type = PrefabType.LSIDToIndex.TryGetValue(id, out int prefabTypeIndexLS) ? prefabTypeIndexLS : PrefabType.VGIDToIndex.TryGetValue(id, out int prefabTypeIndexVG) ? prefabTypeIndexVG : 0;
                    prefab.typeID = id;

                    RenderPrefabObjectType(prefab);
                    EditorTimeline.inst.RenderTimelineObjects();
                });
            });
        }

        public void RenderPrefabObjectDefault(PrefabObject prefabObject, Prefab prefab)
        {
            PrefabObjectEditor.DefaultInstanceDataButton.Text = prefab.defaultInstanceData ? "Remove" : "Set as Default";
            PrefabObjectEditor.DefaultInstanceDataButton.OnClick.NewListener(() =>
            {
                prefab.defaultInstanceData = prefab.defaultInstanceData ? null : prefabObject.Copy();
                EditorManager.inst.DisplayNotification($"Set the currently selected Prefab Object's data as the default for this Prefab.", 4f, EditorManager.NotificationType.Success);
                RenderPrefabObjectDefault(prefabObject, prefab);
            });
        }

        public void RenderPrefabObjectInfo(Prefab prefab)
        {
            PrefabObjectEditor.ObjectCountText.text = "Object Count: " + prefab.beatmapObjects.Count.ToString();
            PrefabObjectEditor.PrefabObjectCountText.text = "Prefab Object Count: " + prefab.prefabObjects.Count;
            PrefabObjectEditor.BackgroundObjectCountText.text = "Background Object Count: " + prefab.backgroundObjects.Count;
            PrefabObjectEditor.TimelineObjectCountText.text = "Timeline Object Count: " + GameData.Current.prefabObjects.FindAll(x => x.prefabID == prefab.id).Count;
        }

        #endregion

        /// <summary>
        /// Collapses all objects related to the currently selected prefabable object into a Prefab instance.
        /// </summary>
        public void CollapseCurrentPrefab(bool createNew = false)
        {
            if (!EditorTimeline.inst.CurrentSelection.TryGetData(out IPrefabable prefabable))
            {
                EditorManager.inst.DisplayNotification("Can't collapse non-object.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (EditorConfig.Instance.ShowCollapsePrefabWarning.Value)
            {
                RTEditor.inst.ShowWarningPopup("Are you sure you want to collapse this Prefab group and save the changes to the Internal Prefab?", () =>
                {
                    Collapse(prefabable, (prefabable as IEditable)?.EditorData, createNew);
                    RTEditor.inst.HideWarningPopup();
                }, RTEditor.inst.HideWarningPopup);

                return;
            }

            Collapse(prefabable, (prefabable as IEditable)?.EditorData, createNew);
        }

        /// <summary>
        /// Expands the contents of the currently selected Prefab instance.
        /// </summary>
        public void ExpandCurrentPrefab()
        {
            if (!EditorTimeline.inst.CurrentSelection.isPrefabObject)
            {
                EditorManager.inst.DisplayNotification("Can't expand non-prefab!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            Expand(EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>());
        }

        /// <summary>
        /// Collapses all objects related to a prefabable object into a Prefab instance.
        /// </summary>
        /// <param name="prefabable">Prefabable object.</param>
        /// <param name="editorData">Object editor data to apply to the new Prefab instance.</param>
        /// <param name="createNew">If a new Prefab should be created.</param>
        public void Collapse(IPrefabable prefabable, ObjectEditorData editorData, bool createNew = false)
        {
            var prefabID = prefabable.PrefabID;
            var prefabInstanceID = prefabable.PrefabInstanceID;

            if (string.IsNullOrEmpty(prefabInstanceID))
            {
                EditorManager.inst.DisplayNotification("Object does not have a Prefab Object reference.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var prefabables = GameData.Current.GetPrefabables().Where(x => x.PrefabInstanceID == prefabInstanceID);

            var objects = GameData.Current.beatmapObjects.FindAll(x => x.SamePrefabInstance(prefabable));
            var bgObjects = GameData.Current.backgroundObjects.FindAll(x => x.SamePrefabInstance(prefabable));
            var prefabObjects = GameData.Current.prefabObjects.FindAll(x => x.PrefabInstanceID == prefabInstanceID);

            if (prefabables.IsEmpty())
            {
                EditorManager.inst.DisplayNotification("No objects were found for the prefab to collapse.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            float startTime = 0f;
            if (!prefabables.IsEmpty())
                startTime = prefabables.Min(x => x.StartTime);

            // todo: add history
            //EditorManager.inst.history.Add(new History.Command("Collapse Prefab",
            //    () =>
            //    {

            //    },
            //    () =>
            //    {

            //    }), true);

            int index = GameData.Current.prefabs.FindIndex(x => x.id == prefabID);
            var originalPrefab = GameData.Current.prefabs[index];
            Prefab newPrefab = originalPrefab.Copy();
            PrefabObject prefabObject;

            newPrefab.CopyObjects(objects, prefabObjects, backgroundObjects: bgObjects, prefabs: originalPrefab.prefabs);

            if (createNew)
            {
                prefabObject = new PrefabObject(newPrefab.id, startTime - newPrefab.offset);

                newPrefab.typeID = originalPrefab.typeID;

                int num = GameData.Current.prefabs.FindAll(x => Regex.Replace(x.name, "( +\\[\\d+])", string.Empty) == newPrefab.name).Count;
                if (num > 0)
                    newPrefab.name = $"{newPrefab.name} [{num}]";

                GameData.Current.prefabs.Add(newPrefab);
            }
            else
            {
                prefabObject = GameData.Current.prefabObjects.TryFind(x => x.id == prefabInstanceID && x.expanded, out PrefabObject originalPrefabObject) ? originalPrefabObject : new PrefabObject(originalPrefab.id);

                prefabObject.StartTime = startTime - originalPrefab.offset;
                prefabObject.SetDefaultTransformOffsets();

                foreach (var other in prefabObjects)
                {
                    var otherPefab = other.GetPrefab();
                    if (otherPefab && otherPefab.id != originalPrefab.id && !newPrefab.prefabs.Has(x => x.id == otherPefab.id))
                        newPrefab.prefabs.Add(otherPefab.Copy(false));
                }

                newPrefab.id = originalPrefab.id;
                newPrefab.typeID = originalPrefab.typeID;

                GameData.Current.prefabs[index] = newPrefab;
            }

            if (editorData)
            {
                prefabObject.editorData.Bin = editorData.Bin;
                prefabObject.editorData.Layer = editorData.Layer;
            }

            prefabObject.orderModifiers = EditorConfig.Instance.CreateObjectModifierOrderDefault.Value;

            if (copiedInstanceData)
                prefabObject.PasteInstanceData(copiedInstanceData);
            else if (newPrefab.defaultInstanceData)
                prefabObject.PasteInstanceData(newPrefab.defaultInstanceData);

            EditorTimeline.inst.timelineObjects.ForLoopReverse((timelineObject, index) =>
            {
                if (!timelineObject.TryGetPrefabable(out IPrefabable otherPrefabable) || otherPrefabable.PrefabInstanceID != prefabInstanceID)
                    return;

                CoreHelper.Delete(timelineObject.GameObject);
                EditorTimeline.inst.timelineObjects.RemoveAt(index);
            });

            GameData.Current.beatmapObjects.ForLoopReverse((beatmapObject, index) =>
            {
                if (beatmapObject.prefabInstanceID != prefabInstanceID || beatmapObject.FromPrefab)
                    return;

                if (quickPrefabTarget && quickPrefabTarget.id == beatmapObject.id)
                    quickPrefabTarget = null;

                RTLevel.Current?.UpdateObject(beatmapObject, reinsert: false, recalculate: false);
                GameData.Current.beatmapObjects.RemoveAt(index);
            });
            GameData.Current.backgroundObjects.ForLoopReverse((backgroundObject, index) =>
            {
                if (backgroundObject.prefabInstanceID != prefabInstanceID || backgroundObject.FromPrefab)
                    return;

                RTLevel.Current?.UpdateBackgroundObject(backgroundObject, reinsert: false, recalculate: false);
                GameData.Current.backgroundObjects.RemoveAt(index);
            });
            GameData.Current.prefabObjects.ForLoopReverse((prefabObject, index) =>
            {
                if (prefabObject.prefabInstanceID != prefabInstanceID || prefabObject.FromPrefab)
                    return;

                RTLevelBase runtimeLevel = prefabObject.runtimeObject?.ParentRuntime ?? RTLevel.Current;

                runtimeLevel?.RemovePrefab(prefabObject);
                GameData.Current.prefabObjects.RemoveAt(index);
            });

            GameData.Current.prefabObjects.Add(prefabObject);
            RTLevel.Current?.UpdatePrefab(prefabObject, recalculate: false);

            GameData.Current.prefabObjects.FindAll(x => x.prefabID == originalPrefab.id && string.IsNullOrEmpty(x.PrefabInstanceID)).ForEach(x =>
            {
                RTLevelBase runtimeLevel = x.runtimeObject?.ParentRuntime ?? RTLevel.Current;

                runtimeLevel?.UpdatePrefab(x, recalculate: runtimeLevel is not RTLevel);
            });
            RTLevel.Current?.RecalculateObjectStates();

            EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.GetTimelineObject(prefabObject));

            EditorManager.inst.DisplayNotification("Replaced all instances of Prefab!", 2f, EditorManager.NotificationType.Success);
        }

        /// <summary>
        /// Expands the contents of a Prefab instance.
        /// </summary>
        /// <param name="prefabObject">Prefab instance.</param>
        public void Expand(PrefabObject prefabObject)
        {
            List<TimelineObject> selected = null;
            PrefabExpander.Expanded expanded = null;
            EditorManager.inst.history.Add(new History.Command("Expand Prefab",
                () =>
                {
                    string id = prefabObject.id;

                    Debug.Log($"{PrefabEditor.inst.className}Removing Prefab Object's spawned objects.");
                    RTLevel.Current?.UpdatePrefab(prefabObject, false);

                    EditorTimeline.inst.RemoveTimelineObject(EditorTimeline.inst.timelineObjects.Find(x => x.ID == id));

                    GameData.Current.prefabObjects.RemoveAll(x => x.id == id);
                    selected = EditorTimeline.inst.SelectedObjects;
                    EditorTimeline.inst.DeselectAllObjects();
                    EditorTimeline.inst.RenderTimelineObjects();

                    Debug.Log($"{PrefabEditor.inst.className}Expanding Prefab Object.");
                    new PrefabExpander(prefabObject).Select().Expand(e =>
                    {
                        expanded = e;
                    });
                },
                () =>
                {
                    if (!expanded)
                        return;

                    RTEditor.inst.RemoveBeatmap(expanded);

                    GameData.Current.prefabObjects.Add(prefabObject);

                    RTLevel.Current.UpdatePrefab(prefabObject);

                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(prefabObject));
                    EditorTimeline.inst.SetCurrentObject(prefabObject.timelineObject);
                }), true);
        }

        /// <summary>
        /// Creates an instance of a Prefab and imports it to the level.
        /// </summary>
        /// <param name="prefab">Prefab to import.</param>
        /// <param name="target">Object to target.</param>
        public void AddPrefabObjectToLevel(Prefab prefab, ObjectTransform target = null)
        {
            var prefabObject = new PrefabObject
            {
                id = LSText.randomString(16),
                prefabID = prefab.id,
                StartTime = RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsPrefabImport.Value ? RTEditor.SnapToBPM(EditorManager.inst.CurrentAudioPos) : EditorManager.inst.CurrentAudioPos,
            };

            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
                EditorTimeline.inst.SetLayer(EditorTimeline.LayerType.Objects);

            prefabObject.editorData.Layer = EditorManager.inst.layer;

            // Set default scale
            prefabObject.events[1].values[0] = 1f;
            prefabObject.events[1].values[1] = 1f;

            if (copiedInstanceData)
                prefabObject.PasteInstanceData(copiedInstanceData);
            else if (prefab.defaultInstanceData)
                prefabObject.PasteInstanceData(prefab.defaultInstanceData);
            else if (AssetPack.TryReadFromFile("editor/data/default_prefab_object.json", out string defaultPrefabObjectFile))
            {
                try
                {
                    prefabObject.ReadJSON(JSON.Parse(defaultPrefabObjectFile));
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
            }

            prefabObject.orderModifiers = EditorConfig.Instance.CreateObjectModifierOrderDefault.Value;

            if (target)
            {
                prefabObject.events[0].values[0] = target.position.x;
                prefabObject.events[0].values[1] = target.position.y;
                prefabObject.events[1].values[0] = target.scale.x;
                prefabObject.events[1].values[1] = target.scale.y;
                prefabObject.events[2].values[0] = target.rotation;
            }
            else if (EditorConfig.Instance.SpawnPrefabsAtCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;
                prefabObject.events[0].values[0] = pos.x;
                prefabObject.events[0].values[1] = pos.y;
            }

            for (int i = 0; i < prefab.beatmapThemes.Count; i++)
                GameData.Current.AddTheme(prefab.beatmapThemes[i]); // only add, don't overwrite
            if (!prefab.beatmapThemes.IsEmpty())
                RTThemeEditor.inst.LoadInternalThemes();

            GameData.Current.prefabObjects.Add(prefabObject);

            RTLevel.Current?.UpdatePrefab(prefabObject);
            RTLevel.Current?.RecalculateObjectStates();

            EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.GetTimelineObject(prefabObject));
            EditorTimeline.inst.UpdateTransformIndex();

            Example.Current?.brain?.Notice(ExampleBrain.Notices.IMPORT_PREFAB, new PrefabNoticeParameters(prefab, prefabObject));
        }

        /// <summary>
        /// Pastes the copied Prefab instance data to a Prefab Object.
        /// </summary>
        /// <param name="prefabObject">Prefab Object to paste data to.</param>
        public void PasteInstanceData(PrefabObject prefabObject)
        {
            prefabObject.PasteInstanceData(copiedInstanceData);
            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(prefabObject));
            RTLevel.Current.UpdatePrefab(prefabObject);
        }

        #endregion

        #region Prefab Types

        void CreatePrefabTypesPopup()
        {
            var gameObject = Creator.NewUIObject("Prefab Types Popup", RTEditor.inst.popups, 9);

            var baseImage = gameObject.AddComponent<Image>();
            EditorThemeManager.ApplyGraphic(baseImage, ThemeGroup.Background_1);
            var baseSelectGUI = gameObject.AddComponent<DraggableUI>();

            gameObject.transform.AsRT().anchoredPosition = new Vector2(340f, 0f);
            gameObject.transform.AsRT().sizeDelta = new Vector2(400f, 600f);

            baseSelectGUI.target = gameObject.transform;
            baseSelectGUI.mode = DraggableUI.DragMode.RequiredDrag;

            var panel = EditorManager.inst.GetDialog("Save As Popup").Dialog.Find("New File Popup/Panel").gameObject.Duplicate(gameObject.transform, "Panel");
            var panelRT = (RectTransform)panel.transform;
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(32f, 32f);

            var title = panel.transform.Find("Text").GetComponent<Text>();
            title.text = "Prefab Type Editor / Selector";
            var closeButton = panel.transform.Find("x").GetComponent<Button>();
            closeButton.onClick.NewListener(() => RTEditor.inst.PrefabTypesPopup.Close());

            var refresh = Creator.NewUIObject("Refresh", panel.transform);
            UIManager.SetRectTransform(refresh.transform.AsRT(), new Vector2(-52f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(32f, 32f));

            var refreshImage = refresh.AddComponent<Image>();
            refreshImage.sprite = EditorSprites.ReloadSprite;

            prefabTypeReloadButton = refresh.AddComponent<Button>();
            prefabTypeReloadButton.image = refreshImage;
            EditorThemeManager.ApplySelectable(prefabTypeReloadButton, ThemeGroup.Function_2, false);

            var scrollRect = Creator.NewUIObject("ScrollRect", gameObject.transform);
            scrollRect.transform.AsRT().anchoredPosition = new Vector2(0f, 0f);
            scrollRect.transform.AsRT().sizeDelta = new Vector2(400f, 600f);
            var scrollRectSR = scrollRect.AddComponent<ScrollRect>();
            scrollRectSR.scrollSensitivity = 20f;

            var mask = Creator.NewUIObject("Mask", scrollRect.transform);
            RectValues.FullAnchored.AssignToRectTransform(mask.transform.AsRT());

            var maskImage = mask.AddComponent<Image>();
            var maskMask = mask.AddComponent<Mask>();
            maskMask.showMaskGraphic = false;

            var content = Creator.NewUIObject("Content", mask.transform);
            RectValues.Default.AnchoredPosition(0f, -16f).AnchorMax(0f, 1f).AnchorMin(0f, 1f).Pivot(0f, 1f).SizeDelta(400f, 104f).AssignToRectTransform(content.transform.AsRT());

            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var contentVLG = content.AddComponent<VerticalLayoutGroup>();
            contentVLG.childControlHeight = false;
            contentVLG.childForceExpandHeight = false;
            contentVLG.spacing = 4f;

            scrollRectSR.content = content.transform.AsRT();

            var scrollbar = EditorManager.inst.GetDialog("Parent Selector").Dialog.Find("Scrollbar").gameObject.Duplicate(scrollRect.transform, "Scrollbar");
            scrollbar.transform.AsRT().anchoredPosition = Vector2.zero;
            scrollbar.transform.AsRT().sizeDelta = new Vector2(32f, 600f);
            scrollRectSR.verticalScrollbar = scrollbar.GetComponent<Scrollbar>();

            EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Bottom_Left_I);
            EditorThemeManager.ApplyGraphic(maskImage, ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Bottom_Left_I);
            EditorThemeManager.ApplyGraphic(panelRT.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);
            EditorThemeManager.ApplySelectable(closeButton, ThemeGroup.Close);
            EditorThemeManager.ApplyGraphic(closeButton.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);
            EditorThemeManager.ApplyLightText(title);

            EditorThemeManager.ApplyScrollbar(scrollRectSR.verticalScrollbar, scrollbarRoundedSide: SpriteHelper.RoundedSide.Bottom_Right_I);

            // Prefab Type Prefab
            prefabTypePrefab = Creator.NewUIObject("Prefab Type", transform);
            prefabTypePrefab.transform.AsRT().sizeDelta = new Vector2(400f, 32f);
            var image = prefabTypePrefab.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.1f);

            var horizontalLayoutGroup = prefabTypePrefab.AddComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.childControlWidth = false;
            horizontalLayoutGroup.childForceExpandWidth = false;
            horizontalLayoutGroup.spacing = 4;

            var toggleType = prefabTypeTogglePrefab.Duplicate(prefabTypePrefab.transform, "Toggle");
            toggleType.transform.localScale = Vector3.one;
            toggleType.transform.AsRT().sizeDelta = new Vector2(32f, 32f);
            Destroy(toggleType.transform.Find("text").gameObject);
            toggleType.transform.Find("Background/Checkmark").GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

            var toggleTog = toggleType.GetComponent<Toggle>();
            toggleTog.enabled = true;
            toggleTog.group = null;

            var icon = Creator.NewUIObject("Icon", toggleType.transform);
            icon.transform.AsRT().anchoredPosition = Vector2.zero;
            icon.transform.AsRT().sizeDelta = new Vector2(32f, 32f);

            var iconImage = icon.AddComponent<Image>();

            var nameGO = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(prefabTypePrefab.transform, "Name");
            nameGO.transform.localScale = Vector3.one;
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.sizeDelta = new Vector2(132f, 32f);

            var nameTextRT = nameRT.Find("Text").AsRT();
            nameTextRT.anchoredPosition = new Vector2(0f, 0f);
            nameTextRT.sizeDelta = new Vector2(0f, 0f);

            nameTextRT.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

            var colorGO = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(prefabTypePrefab.transform, "Color");
            colorGO.transform.localScale = Vector3.one;
            var colorRT = colorGO.GetComponent<RectTransform>();
            colorRT.sizeDelta = new Vector2(90f, 32f);

            var colorTextRT = colorRT.Find("Text").AsRT();
            colorTextRT.anchoredPosition = new Vector2(0f, 0f);
            colorTextRT.sizeDelta = new Vector2(0f, 0f);

            colorTextRT.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

            var setIcon = EditorPrefabHolder.Instance.Function1Button.Duplicate(prefabTypePrefab.transform, "Set Icon");
            setIcon.transform.AsRT().sizeDelta = new Vector2(95f, 32f);

            Destroy(setIcon.GetComponent<LayoutElement>());

            var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(prefabTypePrefab.transform, "Delete");
            delete.transform.localScale = Vector3.one;
            delete.transform.AsRT().anchoredPosition = Vector2.zero;

            Destroy(delete.GetComponent<LayoutElement>());

            EditorHelper.AddEditorPopup(EditorPopup.PREFAB_TYPES_POPUP, gameObject);
            gameObject.SetActive(false);

            EditorHelper.AddEditorDropdown("View Prefab Types", string.Empty, EditorHelper.VIEW_DROPDOWN, EditorSprites.SearchSprite, () =>
            {
                OpenPrefabTypePopup(NewPrefabTypeID, id =>
                {
                    PrefabEditor.inst.NewPrefabType = PrefabType.LSIDToIndex.TryGetValue(id, out int prefabTypeIndexLS) ? prefabTypeIndexLS : PrefabType.VGIDToIndex.TryGetValue(id, out int prefabTypeIndexVG) ? prefabTypeIndexVG : 0;
                    NewPrefabTypeID = id;
                    RenderPrefabCreatorTypeSelector(id);
                });
            });

            RTEditor.inst.PrefabTypesPopup = new ContentPopup(EditorPopup.PREFAB_TYPES_POPUP);
            RTEditor.inst.PrefabTypesPopup.GameObject = gameObject;
            RTEditor.inst.PrefabTypesPopup.Content = content.transform.AsRT();
        }

        /// <summary>
        /// Saves all custom prefab types to the prefab types folder.
        /// </summary>
        public void SavePrefabTypes()
        {
            var prefabTypesPath = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabTypePath);
            foreach (var prefabType in prefabTypes.Where(x => !x.isDefault))
            {
                var jn = prefabType.ToJSON();
                prefabType.filePath = RTFile.CombinePaths(prefabTypesPath, RTFile.FormatLegacyFileName(prefabType.name) + FileFormat.LSPT.Dot());
                RTFile.WriteToFile(prefabType.filePath, jn.ToString(3));
            }
        }

        public bool loadingPrefabTypes = false;

        /// <summary>
        /// Loads all custom prefab types from the prefab types folder.
        /// </summary>
        public IEnumerator LoadPrefabTypes()
        {
            loadingPrefabTypes = true;
            prefabTypes.Clear();

            var defaultPrefabTypesJN = JSON.Parse(RTFile.ReadFromFile(RTFile.GetAsset($"builtin/default_prefabtypes{FileFormat.LSPT.Dot()}")));
            for (int i = 0; i < defaultPrefabTypesJN["prefab_types"].Count; i++)
            {
                var prefabType = PrefabType.Parse(defaultPrefabTypesJN["prefab_types"][i]);
                prefabType.isDefault = true;
                prefabTypes.Add(prefabType);
            }

            var prefabTypesPath = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabTypePath);
            RTFile.CreateDirectory(prefabTypesPath);

            var files = Directory.GetFiles(prefabTypesPath, FileFormat.LSPT.ToPattern(), SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
            {
                var jn = JSON.Parse(RTFile.ReadFromFile(files[i]));
                var prefabType = PrefabType.Parse(jn);
                prefabType.filePath = RTFile.ReplaceSlash(files[i]);
                prefabTypes.Add(prefabType);
            }

            NewPrefabTypeID = prefabTypes[0].id;

            loadingPrefabTypes = false;

            yield break;
        }

        /// <summary>
        /// Opens the prefab types popup.
        /// </summary>
        /// <param name="current">The currently selected type ID.</param>
        /// <param name="onSelect">Action to occur when selecting.</param>
        public void OpenPrefabTypePopup(string current, Action<string> onSelect)
        {
            RTEditor.inst.PrefabTypesPopup.Open();
            RenderPrefabTypesPopup(current, onSelect);
        }

        /// <summary>
        /// Renders the prefab types popup.
        /// </summary>
        /// <param name="current">The currently selected type ID.</param>
        /// <param name="onSelect">Action to occur when selecting.</param>
        public void RenderPrefabTypesPopup(string current, Action<string> onSelect) => StartCoroutine(IRenderPrefabTypesPopup(current, onSelect));

        IEnumerator IRenderPrefabTypesPopup(string current, Action<string> onSelect)
        {
            prefabTypeReloadButton.onClick.NewListener(() =>
            {
                StartCoroutine(LoadPrefabTypes());
                RenderPrefabTypesPopup(NewPrefabTypeID, onSelect);
            });

            RTEditor.inst.PrefabTypesPopup.ClearContent();

            var add = EditorPrefabHolder.Instance.CreateAddButton(RTEditor.inst.PrefabTypesPopup.Content);
            add.Text = "Create Prefab Type";
            add.OnClick.NewListener(() =>
            {
                string name = "New Type";
                int n = 0;
                while (prefabTypes.Has(x => x.name == name))
                {
                    name = $"New Type [{n}]";
                    n++;
                }

                var prefabType = new PrefabType(name, LSColors.pink500);
                prefabType.icon = LegacyPlugin.AtanPlaceholder;

                prefabTypes.Add(prefabType);

                SavePrefabTypes();

                RenderPrefabTypesPopup(current, onSelect);
            });

            int num = 0;
            foreach (var prefabType in prefabTypes)
            {
                int index = num;
                var gameObject = prefabTypePrefab.Duplicate(RTEditor.inst.PrefabTypesPopup.Content, prefabType.name);

                var toggle = gameObject.transform.Find("Toggle").GetComponent<Toggle>();
                toggle.SetIsOnWithoutNotify(current == prefabType.id);
                toggle.onValueChanged.NewListener(_val =>
                {
                    onSelect?.Invoke(prefabType.id);
                    RenderPrefabTypesPopup(prefabType.id, onSelect);
                });

                toggle.image.color = prefabType.color;

                EditorThemeManager.ApplyGraphic(toggle.image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(toggle.graphic, ThemeGroup.Background_1);

                var icon = gameObject.transform.Find("Toggle/Icon").GetComponent<Image>();
                icon.sprite = prefabType.icon;

                var inputField = gameObject.transform.Find("Name").GetComponent<InputField>();
                inputField.onValueChanged.ClearAll();
                inputField.onEndEdit.ClearAll();
                inputField.characterValidation = InputField.CharacterValidation.None;
                inputField.characterLimit = 0;
                inputField.text = prefabType.name;
                inputField.interactable = !prefabType.isDefault;
                if (!prefabType.isDefault)
                {
                    inputField.onValueChanged.AddListener(_val =>
                    {
                        string oldName = prefabTypes[index].name;

                        string name = _val;
                        int n = 0;
                        while (prefabTypes.Has(x => x.name == name))
                        {
                            name = $"{_val}[{n}]";
                            n++;
                        }

                        prefabTypes[index].name = name;

                        if (!RTFile.FileExists(prefabType.filePath))
                            return;

                        File.Delete(prefabType.filePath);
                    });
                    inputField.onEndEdit.AddListener(_val =>
                    {
                        SavePrefabTypes();
                        RenderPrefabTypesPopup(current, onSelect);
                    });
                }

                EditorThemeManager.ApplyInputField(inputField);

                var color = gameObject.transform.Find("Color").GetComponent<InputField>();
                color.onValueChanged.ClearAll();
                color.onEndEdit.ClearAll();
                color.characterValidation = InputField.CharacterValidation.None;
                color.characterLimit = 0;
                color.text = RTColors.ColorToHex(prefabType.color);
                color.interactable = !prefabType.isDefault;
                if (!prefabType.isDefault)
                {
                    color.onValueChanged.AddListener(prefabType.AssignColor);
                    color.onEndEdit.AddListener(_val =>
                    {
                        RenderPrefabTypesPopup(current, onSelect);
                        SavePrefabTypes();
                    });
                }

                EditorThemeManager.ApplyInputField(color);

                var deleteStorage = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.OnClick.ClearAll();
                deleteStorage.Interactable = !prefabType.isDefault;
                if (!prefabType.isDefault)
                    deleteStorage.OnClick.AddListener(() =>
                    {
                        if (RTFile.FileExists(prefabType.filePath))
                            File.Delete(prefabType.filePath);

                        prefabTypes.RemoveAt(index);

                        RenderPrefabTypesPopup(current, onSelect);
                        SavePrefabTypes();
                    });

                EditorThemeManager.ApplyDeleteButton(deleteStorage);

                var setImageStorage = gameObject.transform.Find("Set Icon").GetComponent<FunctionButtonStorage>();
                setImageStorage.OnClick.ClearAll();
                setImageStorage.Interactable = !prefabType.isDefault;

                if (!prefabType.isDefault)
                    setImageStorage.OnClick.AddListener(() =>
                    {
                        RTEditor.inst.BrowserPopup.Open();
                        RTFileBrowser.inst.UpdateBrowserFile(new string[] { FileFormat.PNG.Dot() }, onSelectFile: _val =>
                        {
                            prefabType.icon = SpriteHelper.LoadSprite(_val);
                            icon.sprite = prefabType.icon;

                            SavePrefabTypes();
                            RTEditor.inst.BrowserPopup.Close();
                        });
                    });

                setImageStorage.Text = "Set Icon";

                EditorThemeManager.ApplyGraphic(setImageStorage.button.image, ThemeGroup.Function_1, true);
                EditorThemeManager.ApplyGraphic(setImageStorage.label, ThemeGroup.Function_1_Text);

                num++;
            }

            yield break;
        }

        #endregion

        #region Prefabs

        /// <summary>
        /// Loads the external Prefabs.
        /// </summary>
        /// <param name="onLoad">Function to run when the Prefabs have finished loading.</param>
        public void LoadPrefabs(Action onLoad = null) => CoroutineHelper.StartCoroutine(ILoadPrefabs(onLoad));

        /// <summary>
        /// Loads the external Prefabs.
        /// </summary>
        /// <param name="onLoad">Function to run when the Prefabs have finished loading.</param>
        public IEnumerator ILoadPrefabs(Action onLoad = null)
        {
            if (prefabsLoading)
                yield break;

            prefabsLoading = true;

            while (!PrefabEditor.inst || !PrefabEditor.inst.externalContent)
                yield return null;

            if (PrefabEditorDialog && PrefabEditorDialog.IsCurrent)
                PrefabEditorDialog.Close();

            PrefabPanels.ForLoopReverse((prefabPanel, index) =>
            {
                if (prefabPanel.Source != ObjectSource.External)
                    return;

                CoreHelper.Delete(prefabPanel.GameObject);
                PrefabPanels.RemoveAt(index);
            });

            if (!prefabExternalAddButton)
            {
                CoreHelper.DestroyChildren(PrefabEditor.inst.externalContent);

                prefabExternalAddButton = CreatePrefabButton(PrefabEditor.inst.externalContent, "New External Prefab", eventData =>
                {
                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonElement("Create folder", () => RTEditor.inst.ShowFolderCreator(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath), () => { RTPrefabEditor.inst.LoadPrefabs(RTPrefabEditor.inst.RenderExternalPrefabs); RTEditor.inst.HideNameEditor(); })),
                            new ButtonElement("Create prefab", () =>
                            {
                                PrefabEditor.inst.OpenDialog();
                                createInternal = false;
                            }),
                            new ButtonElement("Paste Prefab", PastePrefab)
                            );

                        return;
                    }

                    if (savingToPrefab && prefabToSaveFrom != null)
                    {
                        savingToPrefab = false;
                        SavePrefab(prefabToSaveFrom);

                        RTEditor.inst.PrefabPopups.Close();

                        prefabToSaveFrom = null;

                        EditorManager.inst.DisplayNotification("Applied all changes to new External Prefab.", 2f, EditorManager.NotificationType.Success);

                        return;
                    }

                    PrefabEditor.inst.OpenDialog();
                    createInternal = false;
                });
            }

            var hover = prefabExternalAddButton.GetOrAddComponent<HoverUI>();
            hover.animateSca = true;
            hover.animatePos = false;
            hover.size = EditorConfig.Instance.PrefabButtonHoverSize.Value;

            while (loadingPrefabTypes)
                yield return null;

            // Back
            if (!prefabExternalUpAFolderButton)
            {
                prefabExternalUpAFolderButton = EditorManager.inst.folderButtonPrefab.Duplicate(PrefabEditor.inst.externalContent, "back");
                PrefabPanel.externalBaseRect.AssignToRectTransform(prefabExternalUpAFolderButton.transform.AsRT());
                var folderButtonStorageFolder = prefabExternalUpAFolderButton.GetComponent<FunctionButtonStorage>();
                var folderButtonFunctionFolder = prefabExternalUpAFolderButton.AddComponent<FolderButtonFunction>();

                var hoverUIFolder = prefabExternalUpAFolderButton.AddComponent<HoverUI>();
                hoverUIFolder.size = EditorConfig.Instance.PrefabButtonHoverSize.Value;
                hoverUIFolder.animatePos = false;
                hoverUIFolder.animateSca = true;

                folderButtonStorageFolder.Text = "< Up a folder";

                folderButtonStorageFolder.OnClick.ClearAll();
                folderButtonFunctionFolder.onClick = eventData =>
                {
                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonElement("Create folder", () => RTEditor.inst.ShowFolderCreator(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath), () => { LoadPrefabs(RenderExternalPrefabs); RTEditor.inst.HideNameEditor(); })),
                            new ButtonElement("Paste Prefab", PastePrefab));

                        return;
                    }

                    if (RTEditor.inst.PrefabPopups.External.PathField.text == RTEditor.inst.PrefabPath)
                    {
                        RTEditor.inst.PrefabPopups.External.PathField.text = RTFile.GetDirectory(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath)).Remove(RTEditor.inst.BeatmapsPath + "/");
                        RTEditor.inst.UpdatePrefabPath(false);
                    }
                };

                EditorThemeManager.ApplySelectable(folderButtonStorageFolder.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(folderButtonStorageFolder.label);
            }

            prefabExternalUpAFolderButton.SetActive(RTFile.GetDirectory(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath)) != RTEditor.inst.BeatmapsPath);

            var directories = Directory.GetDirectories(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath), "*", SearchOption.TopDirectoryOnly);
            int index = 0;
            for (int i = 0; i < directories.Length; i++)
            {
                var directory = directories[i];
                var prefabPanel = new PrefabPanel(index);
                prefabPanel.Init(directory);
                PrefabPanels.Add(prefabPanel);
                index++;
            }

            var files = Directory.GetFiles(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath), FileFormat.LSP.ToPattern(), SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var jn = JSON.Parse(RTFile.ReadFromFile(file));

                var prefab = Prefab.Parse(jn);
                prefab.beatmapObjects.ForEach(x => x?.RemovePrefabReference());
                prefab.filePath = RTFile.ReplaceSlash(file);

                var prefabPanel = new PrefabPanel(ObjectSource.External, index);
                prefabPanel.Init(prefab);
                PrefabPanels.Add(prefabPanel);
                index++;
            }
            
            files = Directory.GetFiles(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath), FileFormat.VGP.ToPattern(), SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var jn = JSON.Parse(RTFile.ReadFromFile(file));

                var prefab = Prefab.ParseVG(jn);
                prefab.beatmapObjects.ForEach(x => x?.RemovePrefabReference());
                prefab.filePath = RTFile.ReplaceSlash(file);

                var prefabPanel = new PrefabPanel(ObjectSource.External, index);
                prefabPanel.Init(prefab);
                PrefabPanels.Add(prefabPanel);
                index++;
            }

            prefabsLoading = false;

            onLoad?.Invoke();

            yield break;
        }

        /// <summary>
        /// Converts a prefab to the VG format and saves it to a file.
        /// </summary>
        /// <param name="prefab">Prefab to convert.</param>
        public void ConvertPrefab(Prefab prefab)
        {
            var exportPath = EditorConfig.Instance.ConvertPrefabLSToVGExportPath.Value;

            if (string.IsNullOrEmpty(exportPath))
            {
                exportPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, RTEditor.DEFAULT_EXPORTS_PATH);
                RTFile.CreateDirectory(exportPath);
            }

            exportPath = RTFile.AppendEndSlash(exportPath);

            if (!RTFile.DirectoryExists(RTFile.RemoveEndSlash(exportPath)))
            {
                EditorManager.inst.DisplayNotification("Directory does not exist.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var vgjn = prefab.ToJSONVG();

            var fileName = $"{RTFile.FormatAlphaFileName(prefab.name)}{FileFormat.VGP.Dot()}";
            RTFile.WriteToFile(RTFile.CombinePaths(exportPath, fileName), vgjn.ToString());

            EditorManager.inst.DisplayNotification($"Converted Prefab {prefab.name} from LS format to VG format and saved to {fileName}!", 4f, EditorManager.NotificationType.Success);

            AchievementManager.inst.UnlockAchievement("time_machine");
        }

        /// <summary>
        /// Opens the Prefab Editor.
        /// </summary>
        /// <param name="prefabPanel">Prefab to edit.</param>
        public void OpenPrefabEditorDialog(PrefabPanel prefabPanel)
        {
            PrefabEditorDialog.Open();
            RenderPrefabEditorDialog(prefabPanel);
        }

        /// <summary>
        /// Renders the Prefab Editor.
        /// </summary>
        /// <param name="prefabPanel">Prefab to edit.</param>
        public void RenderPrefabEditorDialog(PrefabPanel prefabPanel)
        {
            CurrentPrefabPanel = prefabPanel;

            var prefab = prefabPanel.Item;
            var prefabType = prefab.GetPrefabType();
            var isExternal = prefabPanel.IsExternal;

            RenderPrefabEditorTypeSelector(prefabType);
            PrefabEditorDialog.TypeButton.OnClick.NewListener(() => OpenPrefabTypePopup(prefab.typeID, id =>
            {
                prefab.type = PrefabType.LSIDToIndex.TryGetValue(id, out int prefabTypeIndexLS) ? prefabTypeIndexLS : PrefabType.VGIDToIndex.TryGetValue(id, out int prefabTypeIndexVG) ? prefabTypeIndexVG : 0;
                prefab.typeID = id;

                var prefabType = prefab.GetPrefabType();
                RenderPrefabEditorTypeSelector(prefabType);

                UpdatePrefabFile(prefabPanel);
                prefabPanel.RenderPrefabType(prefabType);
            }));

            PrefabEditorDialog.CreatorField.SetTextWithoutNotify(prefab.creator);
            PrefabEditorDialog.CreatorField.onValueChanged.NewListener(_val => prefab.creator = _val);
            PrefabEditorDialog.CreatorField.onEndEdit.NewListener(_val => UpdatePrefabFile(prefabPanel));

            PrefabEditorDialog.NameField.SetTextWithoutNotify(prefab.name);
            PrefabEditorDialog.NameField.onValueChanged.NewListener(_val => prefab.name = _val);
            PrefabEditorDialog.NameField.onEndEdit.NewListener(_val =>
            {
                if (!isExternal)
                {
                    prefabPanel.RenderLabel();
                    prefabPanel.RenderTooltip();
                    EditorTimeline.inst.timelineObjects.ForLoop(timelineObject =>
                    {
                        if (!timelineObject.isPrefabObject || timelineObject.GetData<PrefabObject>().prefabID != prefab.id)
                            return;

                        timelineObject.RenderText(prefab.name);
                    });

                    return;
                }

                RTEditor.inst.DisablePrefabWatcher();

                RTFile.DeleteFile(prefab.filePath);

                var file = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath, prefab.GetFileName());
                prefab.filePath = file;
                prefab.WriteToFile(file);

                prefabPanel.RenderLabel();
                prefabPanel.RenderTooltip();

                RTEditor.inst.EnablePrefabWatcher();
            });

            PrefabEditorDialog.DescriptionField.SetTextWithoutNotify(prefab.description);
            PrefabEditorDialog.DescriptionField.onValueChanged.NewListener(_val => prefab.description = _val);
            PrefabEditorDialog.DescriptionField.onEndEdit.NewListener(_val => UpdatePrefabFile(prefabPanel));

            PrefabEditorDialog.VersionField.SetTextWithoutNotify(prefab.ObjectVersion);
            PrefabEditorDialog.VersionField.onValueChanged.NewListener(_val => prefab.ObjectVersion = _val);
            PrefabEditorDialog.VersionField.onEndEdit.NewListener(_val =>
            {
                UpdatePrefabFile(prefabPanel);
                RenderPrefabEditorDialog(prefabPanel);
            });
            EditorContextMenu.AddContextMenu(PrefabEditorDialog.VersionField.gameObject, EditorContextMenu.GetObjectVersionFunctions(prefab, () =>
            {
                UpdatePrefabFile(prefabPanel);
                RenderPrefabEditorDialog(prefabPanel);
            }));

            PrefabEditorDialog.IconImage.sprite = prefab.icon;
            EditorContextMenu.AddContextMenu(PrefabEditorDialog.IconImage.gameObject,
                new ButtonElement("Select File", () => OpenIconSelector(prefabPanel)),
                new ButtonElement("Extract Icon", () =>
                {
                    if (!prefab.icon)
                    {
                        EditorManager.inst.DisplayNotification("Prefab does not have an icon.", 1.5f, EditorManager.NotificationType.Warning);
                        return;
                    }

                    var jpgFile = FileBrowser.SaveFile(extension: "jpg");
                    CoreHelper.Log("Selected file: " + jpgFile);
                    if (string.IsNullOrEmpty(jpgFile))
                        return;

                    File.WriteAllBytes(jpgFile, prefab.icon.texture.EncodeToJPG());
                }),
                new ButtonElement("Capture Icon", () =>
                {
                    prefab.icon = CaptureArea.inst?.Capture();
                    prefabPanel.RenderPrefabType();
                    UpdatePrefabFile(prefabPanel);
                    RenderPrefabEditorDialog(prefabPanel);
                }),
                new ButtonElement("Clear Icon", () =>
                {
                    prefab.icon = null;
                    prefab.iconData = null;
                    prefabPanel.RenderPrefabType();
                    UpdatePrefabFile(prefabPanel);
                    RenderPrefabEditorDialog(prefabPanel);
                }));

            PrefabEditorDialog.CollapseIcon(CollapseIcon);
            PrefabEditorDialog.SelectIconButton.onClick.NewListener(() => OpenIconSelector(prefabPanel));
            PrefabEditorDialog.CollapseToggle.SetIsOnWithoutNotify(CollapseIcon);
            PrefabEditorDialog.CollapseToggle.onValueChanged.NewListener(_val =>
            {
                PrefabEditorDialog.CollapseIcon(_val);
                CollapseIcon = _val;
            });

            PrefabEditorDialog.ImportPrefabButton.gameObject.SetActive(isExternal);
            PrefabEditorDialog.ImportPrefabButton.OnClick.NewListener(() => { if (isExternal) ImportPrefabIntoLevel(prefab); });

            PrefabEditorDialog.ConvertPrefabButton.gameObject.SetActive(isExternal);
            PrefabEditorDialog.ConvertPrefabButton.OnClick.NewListener(() => { if (isExternal) ConvertPrefab(prefab); });

            EditorServerManager.inst.RenderTagDialog(prefab, PrefabEditorDialog, EditorServerManager.DefaultTagRelation.Prefab);
            EditorServerManager.inst.RenderServerDialog(
                url: AlephNetwork.PrefabURL,
                uploadable: prefab,
                dialog: PrefabEditorDialog,
                upload: () => UploadPrefab(prefabPanel),
                pull: () => PullServerPrefab(prefabPanel),
                delete: () => DeleteServerPrefab(prefabPanel),
                verify: null);
        }

        public void RenderPrefabEditorTypeSelector(PrefabType prefabType)
        {
            PrefabEditorDialog.TypeButton.Text = prefabType.name + " [ Click to Open Prefab Type Editor ]";
            PrefabEditorDialog.TypeButton.Color = prefabType.color;
        }

        public void OpenIconSelector()
        {
            string jpgFile = FileBrowser.OpenSingleFile("jpg");
            CoreHelper.Log("Selected file: " + jpgFile);
            if (string.IsNullOrEmpty(jpgFile))
                return;

            CoroutineHelper.StartCoroutine(EditorManager.inst.GetSprite(jpgFile, new EditorManager.SpriteLimits(new Vector2(512f, 512f)), cover =>
            {
                NewPrefabIcon = cover;
                RenderPrefabCreator();
            }, errorFile => EditorManager.inst.DisplayNotification("Please resize your image to be less than or equal to 512 x 512 pixels. It must also be a jpg.", 2f, EditorManager.NotificationType.Error)));
        }
        
        public void OpenIconSelector(PrefabPanel prefabPanel)
        {
            string jpgFile = FileBrowser.OpenSingleFile("jpg");
            CoreHelper.Log("Selected file: " + jpgFile);
            if (string.IsNullOrEmpty(jpgFile))
                return;

            CoroutineHelper.StartCoroutine(EditorManager.inst.GetSprite(jpgFile, new EditorManager.SpriteLimits(new Vector2(512f, 512f)), cover =>
            {
                prefabPanel.Item.icon = cover;
                prefabPanel.RenderPrefabType();
                UpdatePrefabFile(prefabPanel);
                RenderPrefabEditorDialog(prefabPanel);
                for (int i = 0; i < EditorTimeline.inst.timelineObjects.Count; i++)
                {
                    var timelineObject = EditorTimeline.inst.timelineObjects[i];
                    if (timelineObject.TryGetPrefabable(out IPrefabable prefabable))
                        timelineObject.RenderIcons(prefabable.GetPrefab());
                }
            }, errorFile => EditorManager.inst.DisplayNotification("Please resize your image to be less than or equal to 512 x 512 pixels. It must also be a jpg.", 2f, EditorManager.NotificationType.Error)));
        }

        public void UpdatePrefabFile(PrefabPanel prefabPanel)
        {
            prefabPanel.RenderTooltip();
            if (!prefabPanel.IsExternal)
                return;

            var prefab = prefabPanel.Item;

            RTEditor.inst.DisablePrefabWatcher();

            if (!string.IsNullOrEmpty(prefab.filePath))
                prefab.WriteToFile(prefab.filePath);

            RTEditor.inst.EnablePrefabWatcher();
        }

        /// <summary>
        /// Creates a new prefab and saves it.
        /// </summary>
        public void CreateNewPrefab()
        {
            var selectedBeatmapObjects = EditorTimeline.inst.SelectedBeatmapObjects;
            var selectedBackgroundObjects = EditorTimeline.inst.SelectedBackgroundObjects;
            var selectedPrefabObjects = EditorTimeline.inst.SelectedPrefabObjects;
            if (selectedBeatmapObjects.IsEmpty() && selectedBackgroundObjects.IsEmpty() && selectedPrefabObjects.IsEmpty())
            {
                EditorManager.inst.DisplayNotification("Can't save prefab without any objects in it!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (string.IsNullOrEmpty(PrefabEditor.inst.NewPrefabName))
            {
                EditorManager.inst.DisplayNotification("Can't save prefab without a name!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var beatmapObjects = selectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()).ToList();
            var prefabObjects = selectedPrefabObjects.Select(x => x.GetData<PrefabObject>()).ToList();
            var backgroundObjects = selectedBackgroundObjects.Select(x => x.GetData<BackgroundObject>()).ToList();

            var prefab = new Prefab(
                PrefabEditor.inst.NewPrefabName,
                PrefabEditor.inst.NewPrefabType,
                PrefabEditor.inst.NewPrefabOffset,
                beatmapObjects,
                prefabObjects,
                null,
                backgroundObjects);

            foreach (var prefabObject in prefabObjects)
            {
                var otherPefab = prefabObject.GetPrefab();
                if (otherPefab && !prefab.prefabs.Has(x => x.id == otherPefab.id))
                    prefab.prefabs.Add(otherPefab.Copy(false));
            }

            prefab.creator = CoreConfig.Instance.DisplayName.Value;
            prefab.description = NewPrefabDescription;
            prefab.icon = NewPrefabIcon;
            prefab.typeID = NewPrefabTypeID;
            prefab.beatmapThemes = new List<BeatmapTheme>(selectedBeatmapThemes.Select(x => x.Copy(false)));
            prefab.modifierBlocks = new List<ModifierBlock>(selectedModifierBlocks.Select(x => x.Copy(false)));
            prefab.assets.sprites = new List<SpriteAsset>(selectedSpriteAssets.Select(x => x.Copy(false)));

            foreach (var beatmapObject in prefab.beatmapObjects)
            {
                if (beatmapObject.shape == 6 && !string.IsNullOrEmpty(beatmapObject.text) && GameData.Current.assets.sprites.TryFind(x => x.name == beatmapObject.text, out SpriteAsset spriteAsset) && !prefab.assets.sprites.Has(x => x.name == spriteAsset.name))
                    prefab.assets.sprites.Add(spriteAsset.Copy());
            }

            foreach (var backgroundObject in prefab.backgroundObjects)
            {
                if (backgroundObject.shape == 6 && !string.IsNullOrEmpty(backgroundObject.text) && GameData.Current.assets.sprites.TryFind(x => x.name == backgroundObject.text, out SpriteAsset spriteAsset) && !prefab.assets.sprites.Has(x => x.name == spriteAsset.name))
                    prefab.assets.sprites.Add(spriteAsset.Copy());
            }

            if (createInternal)
            {
                EditorManager.inst.DisplayNotification($"Saving Internal Prefab [{prefab.name}] to level...", 1.5f, EditorManager.NotificationType.Warning);
                ImportPrefabIntoLevel(prefab);
                EditorManager.inst.DisplayNotification($"Saved Internal Prefab [{prefab.name}]!", 2f, EditorManager.NotificationType.Success);
            }
            else
                SavePrefab(prefab);

            PrefabCreatorDialog.Close();
            OpenPopup();

            if (prefab.prefabPanel)
                OpenPrefabEditorDialog(prefab.prefabPanel);
        }

        /// <summary>
        /// Saves a prefab to a file.
        /// </summary>
        /// <param name="prefab">Prefab to save.</param>
        public void SavePrefab(Prefab prefab)
        {
            EditorManager.inst.DisplayNotification($"Saving External Prefab [{prefab.name}]...", 1.5f, EditorManager.NotificationType.Warning);

            prefab.beatmapObjects.ForEach(x => x.RemovePrefabReference());
            int count = PrefabPanels.Count;
            if (string.IsNullOrEmpty(prefab.filePath))
                prefab.filePath = $"{RTFile.FormatLegacyFileName(prefab.name)}{FileFormat.LSP.Dot()}";
            var file = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath, prefab.GetFileName());
            if (RTFile.FileExists(file))
            {
                RTEditor.inst.ShowWarningPopup("A Prefab with this name already exists. Do you wish to overwrite it?", () =>
                {
                    RTEditor.inst.DisablePrefabWatcher();

                    prefab.filePath = file;
                    prefab.WriteToFile(file);

                    if (PrefabPanels.TryFind(x => x.Path == file, out PrefabPanel originalPrefabPanel))
                    {
                        originalPrefabPanel.Item = prefab;
                        prefab.prefabPanel = originalPrefabPanel;
                        originalPrefabPanel.Render();
                        originalPrefabPanel.SetActive(ContainsName(prefab, originalPrefabPanel.Source));
                    }
                    else
                    {
                        var prefabPanel = new PrefabPanel(ObjectSource.External, count);
                        prefabPanel.Init(prefab);
                        PrefabPanels.Add(prefabPanel);
                    }

                    EditorManager.inst.DisplayNotification($"Saved External Prefab [{prefab.name}]!", 2f, EditorManager.NotificationType.Success);

                    RTEditor.inst.EnablePrefabWatcher();
                    RTEditor.inst.HideWarningPopup();
                }, RTEditor.inst.HideWarningPopup);
                return;
            }
            RTEditor.inst.DisablePrefabWatcher();

            prefab.filePath = file;
            prefab.WriteToFile(file);

            if (PrefabPanels.TryFind(x => x.Path == file, out PrefabPanel originalPrefabPanel))
            {
                originalPrefabPanel.Item = prefab;
                prefab.prefabPanel = originalPrefabPanel;
                originalPrefabPanel.Render();
                originalPrefabPanel.SetActive(ContainsName(prefab, originalPrefabPanel.Source));
            }
            else
            {
                var prefabPanel = new PrefabPanel(ObjectSource.External, count);
                prefabPanel.Init(prefab);
                PrefabPanels.Add(prefabPanel);
            }

            EditorManager.inst.DisplayNotification($"Saved External Prefab [{prefab.name}]!", 2f, EditorManager.NotificationType.Success);

            RTEditor.inst.EnablePrefabWatcher();
        }

        /// <summary>
        /// Deletes an external prefab.
        /// </summary>
        /// <param name="prefabPanel">Prefab panel to delete and destroy.</param>
        public void DeleteExternalPrefab(PrefabPanel prefabPanel)
        {
            RTEditor.inst.DisablePrefabWatcher();

            RTFile.DeleteFile(prefabPanel.Path);

            Destroy(prefabPanel.GameObject);
            PrefabPanels.RemoveAt(prefabPanel.index);

            int num = 0;
            foreach (var p in PrefabPanels)
            {
                p.index = num;
                num++;
            }

            RTEditor.inst.EnablePrefabWatcher();
        }

        /// <summary>
        /// Deletes and internal prefab.
        /// </summary>
        /// <param name="index">Index of the prefab to remove.</param>
        public void DeleteInternalPrefab(int index)
        {
            string id = GameData.Current.prefabs[index].id;

            GameData.Current.prefabs.RemoveAt(index);

            GameData.Current.prefabObjects.FindAll(x => x.prefabID == id).ForEach(x =>
            {
                RTLevel.Current?.UpdatePrefab(x, false, false);

                if (EditorTimeline.inst.timelineObjects.TryFindIndex(y => y.ID == x.id, out int index))
                {
                    Destroy(EditorTimeline.inst.timelineObjects[index].GameObject);
                    EditorTimeline.inst.timelineObjects.RemoveAt(index);
                }
            });

            GameData.Current.prefabObjects.RemoveAll(x => x.prefabID == id);

            foreach (var timelineObject in EditorTimeline.inst.timelineObjects)
            {
                if (!timelineObject.TryGetPrefabable(out IPrefabable prefabable) || prefabable.PrefabID != id)
                    continue;

                prefabable.RemovePrefabReference();
                timelineObject.RenderVisibleState(false);
            }

            RTLevel.Current?.RecalculateObjectStates();

            StartCoroutine(RefreshInternalPrefabs());
        }

        /// <summary>
        /// Deletes and internal prefab.
        /// </summary>
        /// <param name="id">ID of the prefab to remove.</param>
        public void DeleteInternalPrefab(Prefab prefab)
        {
            var id = prefab.id;

            if (GameData.Current.prefabs.TryFindIndex(x => x.id == id, out int index))
                GameData.Current.prefabs.RemoveAt(index);

            for (int i = 0; i < GameData.Current.prefabObjects.Count; i++)
            {
                var prefabObject = GameData.Current.prefabObjects[i];
                if (prefabObject.prefabID != id)
                    continue;

                RTLevel.Current?.UpdatePrefab(prefabObject, false, false);

                if (EditorTimeline.inst.timelineObjects.TryFindIndex(x => x.ID == prefabObject.id, out int j))
                {
                    Destroy(EditorTimeline.inst.timelineObjects[j].GameObject);
                    EditorTimeline.inst.timelineObjects.RemoveAt(j);
                }
            }

            GameData.Current.prefabObjects.RemoveAll(x => x.prefabID == id);

            foreach (var timelineObject in EditorTimeline.inst.timelineObjects)
            {
                if (!timelineObject.TryGetPrefabable(out IPrefabable prefabable) || prefabable.PrefabID != id)
                    continue;

                prefabable.RemovePrefabReference();
                timelineObject.RenderVisibleState(false);
            }

            RTLevel.Current?.RecalculateObjectStates();

            StartCoroutine(RefreshInternalPrefabs());
        }

        /// <summary>
        /// Opens the Internal and External Prefab popups.
        /// </summary>
        public void OpenPopup()
        {
            if (!EditorLevelManager.inst.HasLoadedLevel())
                return;

            RTEditor.inst.PrefabPopups.Open();
            RenderPopup();
        }

        /// <summary>
        /// Renders the Internal and External Prefab popups.
        /// </summary>
        public void RenderPopup()
        {
            UpdateCurrentPrefab(currentQuickPrefab);

            PrefabEditor.inst.internalPrefabDialog.gameObject.SetActive(true);
            PrefabEditor.inst.externalPrefabDialog.gameObject.SetActive(true);

            selectQuickPrefabButton.onClick.NewListener(() =>
            {
                selectQuickPrefabText.text = "<color=#669e37>Selecting</color>";
                StartCoroutine(RefreshInternalPrefabs(true));
            });

            PrefabEditor.inst.externalSearch.onValueChanged.NewListener(_val =>
            {
                PrefabEditor.inst.externalSearchStr = _val;
                StartCoroutine(IRenderExternalPrefabs());
            });

            PrefabEditor.inst.internalSearch.onValueChanged.NewListener(_val =>
            {
                PrefabEditor.inst.internalSearchStr = _val;
                StartCoroutine(RefreshInternalPrefabs());
            });

            savingToPrefab = false;
            prefabToSaveFrom = null;

            StartCoroutine(IRenderExternalPrefabs());
            StartCoroutine(RefreshInternalPrefabs());
        }

        /// <summary>
        /// Opens the Prefab Creator dialog.
        /// </summary>
        public void OpenDialog()
        {
            PrefabCreatorDialog.Open();
            selectedForPrefabCreator.Clear();
            selectedBeatmapThemes.Clear();
            selectedModifierBlocks.Clear();
            selectedSpriteAssets.Clear();
            RenderPrefabCreator();
            RTEditor.inst.PrefabPopups.Close();
        }

        /// <summary>
        /// Renders the Prefab Creator dialog.
        /// </summary>
        public void RenderPrefabCreator()
        {
            PrefabCreatorDialog.NameField.onValueChanged.NewListener(_val => PrefabEditor.inst.NewPrefabName = _val);
            RenderPrefabCreatorOffsetSlider();
            RenderPrefabCreatorOffsetField();

            EditorContextMenu.AddContextMenu(PrefabCreatorDialog.OffsetField.gameObject,
                    new ButtonElement("Set to Timeline Cursor", () =>
                    {
                        PrefabEditor.inst.NewPrefabOffset -= (AudioManager.inst.CurrentAudioSource.time - EditorTimeline.inst.SelectedObjects.Min(x => x.Time) + PrefabEditor.inst.NewPrefabOffset);
                        RenderPrefabCreator();
                    }),
                    new ButtonElement("Snap to BPM", () =>
                    {
                        var firstTime = EditorTimeline.inst.SelectedObjects.Min(x => x.Time);
                        PrefabEditor.inst.NewPrefabOffset = firstTime - RTEditor.SnapToBPM(firstTime - PrefabEditor.inst.NewPrefabOffset);
                        RenderPrefabCreator();
                    }));
            EditorContextMenu.AddContextMenu(PrefabCreatorDialog.OffsetSlider.gameObject,
                    new ButtonElement("Set to Timeline Cursor", () =>
                    {
                        PrefabEditor.inst.NewPrefabOffset -= (AudioManager.inst.CurrentAudioSource.time - EditorTimeline.inst.SelectedObjects.Min(x => x.Time) + PrefabEditor.inst.NewPrefabOffset);
                        RenderPrefabCreator();
                    }),
                    new ButtonElement("Snap to BPM", () =>
                    {
                        var firstTime = EditorTimeline.inst.SelectedObjects.Min(x => x.Time);
                        PrefabEditor.inst.NewPrefabOffset = firstTime - RTEditor.SnapToBPM(firstTime - PrefabEditor.inst.NewPrefabOffset);
                        RenderPrefabCreator();
                    }));
            //EditorContextMenu.AddContextMenu(PrefabCreatorDialog.OffsetField.gameObject, EditorContextMenu.GetObjectTimeFunctions(
            //    getObjectTime: () => EditorTimeline.inst.SelectedObjects.Min(x => x.Time),
            //    setTime: _val =>
            //    {
            //        PrefabEditor.inst.NewPrefabOffset = _val;
            //        RenderPrefabCreator();
            //    }));
            //EditorContextMenu.AddContextMenu(PrefabCreatorDialog.OffsetSlider.gameObject, EditorContextMenu.GetObjectTimeFunctions(
            //    getObjectTime: () => EditorTimeline.inst.SelectedObjects.Min(x => x.Time),
            //    setTime: _val =>
            //    {
            //        PrefabEditor.inst.NewPrefabOffset = _val;
            //        RenderPrefabCreator();
            //    }));

            TriggerHelper.AddEventTriggers(PrefabCreatorDialog.OffsetField.gameObject, TriggerHelper.ScrollDelta(PrefabCreatorDialog.OffsetField));

            RenderPrefabCreatorTypeSelector(NewPrefabTypeID);
            PrefabCreatorDialog.TypeButton.OnClick.NewListener(() => OpenPrefabTypePopup(NewPrefabTypeID, id =>
            {
                PrefabEditor.inst.NewPrefabType = PrefabType.LSIDToIndex.TryGetValue(id, out int prefabTypeIndexLS) ? prefabTypeIndexLS : PrefabType.VGIDToIndex.TryGetValue(id, out int prefabTypeIndexVG) ? prefabTypeIndexVG : 0;
                NewPrefabTypeID = id;
                RenderPrefabCreatorTypeSelector(id);
            }));
            PrefabCreatorDialog.DescriptionField.onValueChanged.NewListener(_val => NewPrefabDescription = _val);

            PrefabCreatorDialog.IconImage.sprite = NewPrefabIcon;
            EditorContextMenu.AddContextMenu(PrefabCreatorDialog.IconImage.gameObject,
                new ButtonElement("Select File", () => OpenIconSelector()),
                new ButtonElement("Extract Icon", () =>
                {
                    if (!NewPrefabIcon)
                    {
                        EditorManager.inst.DisplayNotification("Prefab Creator does not have an icon.", 1.5f, EditorManager.NotificationType.Warning);
                        return;
                    }

                    var jpgFile = FileBrowser.SaveFile(extension: "jpg");
                    CoreHelper.Log("Selected file: " + jpgFile);
                    if (string.IsNullOrEmpty(jpgFile))
                        return;

                    File.WriteAllBytes(jpgFile, NewPrefabIcon.texture.EncodeToJPG());
                }),
                new ButtonElement("Capture Icon", () =>
                {
                    NewPrefabIcon = CaptureArea.inst?.Capture();
                    RenderPrefabCreator();
                }),
                new ButtonElement("Clear Icon", () =>
                {
                    NewPrefabIcon = null;
                    RenderPrefabCreator();
                }));

            PrefabCreatorDialog.CollapseIcon(CollapseCreatorIcon);
            PrefabCreatorDialog.SelectIconButton.onClick.NewListener(() => OpenIconSelector());
            PrefabCreatorDialog.CollapseToggle.SetIsOnWithoutNotify(CollapseCreatorIcon);
            PrefabCreatorDialog.CollapseToggle.onValueChanged.NewListener(_val =>
            {
                PrefabCreatorDialog.CollapseIcon(_val);
                CollapseCreatorIcon = _val;
            });

            ReloadSelectionContent();
        }

        public void RenderPrefabCreatorOffsetSlider()
        {
            PrefabCreatorDialog.OffsetSlider.SetValueWithoutNotify(PrefabEditor.inst.NewPrefabOffset);
            PrefabCreatorDialog.OffsetSlider.onValueChanged.NewListener(_val =>
            {
                PrefabEditor.inst.NewPrefabOffset = Mathf.Round(_val * 100f) / 100f;
                RenderPrefabCreatorOffsetField();
            });
        }
        
        public void RenderPrefabCreatorOffsetField()
        {
            PrefabCreatorDialog.OffsetField.SetTextWithoutNotify(PrefabEditor.inst.NewPrefabOffset.ToString());
            PrefabCreatorDialog.OffsetField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    PrefabEditor.inst.NewPrefabOffset = num;
                    RenderPrefabCreatorOffsetSlider();
                }
            });
        }

        public void RenderPrefabCreatorTypeSelector(string id)
        {
            if (prefabTypes.TryFind(x => x.id == id, out PrefabType prefabType))
            {
                PrefabCreatorDialog.TypeButton.Text = prefabType.name + " [ Click to Open Prefab Type Editor ]";
                PrefabCreatorDialog.TypeButton.Color = prefabType.color;
            }
        }
        
        /// <summary>
        /// Refreshes the Prefab Creator selection list.
        /// </summary>
        public void ReloadSelectionContent()
        {
            PrefabCreatorDialog.SelectionSearchField.onValueChanged.NewListener(_val => ReloadSelectionContent());
            for (int i = 0; i < PrefabCreatorDialog.SelectionTabButtons.Count; i++)
            {
                int index = i;
                PrefabCreatorDialog.SelectionTabButtons[i].onClick.NewListener(() =>
                {
                    prefabCreatorSelectionTab = (SelectionType)index;
                    ReloadSelectionContent();
                });
            }

            PrefabCreatorDialog.ClearSelectionContent();
            switch (prefabCreatorSelectionTab)
            {
                case SelectionType.TimelineObjects: {
                        foreach (var timelineObject in EditorTimeline.inst.timelineObjects)
                        {
                            if (!RTString.SearchString(PrefabCreatorDialog.SelectionSearchTerm, timelineObject.Name))
                                continue;

                            var selection = PrefabEditor.inst.selectionPrefab.Duplicate(PrefabCreatorDialog.SelectionContent, "grid");
                            var text = selection.transform.Find("text").GetComponent<Text>();
                            text.text = timelineObject.Name;
                            text.rectTransform.sizeDelta = new Vector2(300f, 32f);

                            var selectionToggle = selection.GetComponent<Toggle>();
                            selectionToggle.SetIsOnWithoutNotify(timelineObject.Selected);
                            selectionToggle.onValueChanged.NewListener(_val => timelineObject.Selected = _val);
                            EditorThemeManager.ApplyToggle(selectionToggle, graphic: text);
                        }
                        break;
                    }
                case SelectionType.BeatmapThemes: {
                        foreach (var beatmapTheme in GameData.Current.beatmapThemes)
                        {
                            if (!RTString.SearchString(PrefabCreatorDialog.SelectionSearchTerm, beatmapTheme.name))
                                continue;

                            var selection = PrefabEditor.inst.selectionPrefab.Duplicate(PrefabCreatorDialog.SelectionContent, "grid");
                            var text = selection.transform.Find("text").GetComponent<Text>();
                            text.text = beatmapTheme.name;
                            text.rectTransform.sizeDelta = new Vector2(300f, 32f);

                            var selectionToggle = selection.GetComponent<Toggle>();
                            selectionToggle.SetIsOnWithoutNotify(selectedForPrefabCreator.GetValueOrDefault(beatmapTheme.id, false));
                            selectionToggle.onValueChanged.NewListener(_val =>
                            {
                                selectedForPrefabCreator[beatmapTheme.id] = _val;
                                if (!_val)
                                    selectedBeatmapThemes.Remove(x => x.id == beatmapTheme.id);
                                else
                                    selectedBeatmapThemes.Add(beatmapTheme);
                            });
                            EditorThemeManager.ApplyToggle(selectionToggle, graphic: text);
                        }
                        break;
                    }
                case SelectionType.ModifierBlocks: {
                        foreach (var modifierBlock in GameData.Current.modifierBlocks)
                        {
                            if (!RTString.SearchString(PrefabCreatorDialog.SelectionSearchTerm, modifierBlock.Name))
                                continue;

                            var selection = PrefabEditor.inst.selectionPrefab.Duplicate(PrefabCreatorDialog.SelectionContent, "grid");
                            var text = selection.transform.Find("text").GetComponent<Text>();
                            text.text = modifierBlock.Name;
                            text.rectTransform.sizeDelta = new Vector2(300f, 32f);

                            var selectionToggle = selection.GetComponent<Toggle>();
                            selectionToggle.SetIsOnWithoutNotify(selectedForPrefabCreator.GetValueOrDefault(modifierBlock.id, false));
                            selectionToggle.onValueChanged.NewListener(_val =>
                            {
                                selectedForPrefabCreator[modifierBlock.id] = _val;
                                if (!_val)
                                    selectedModifierBlocks.Remove(x => x.id == modifierBlock.id);
                                else
                                    selectedModifierBlocks.Add(modifierBlock);
                            });
                            EditorThemeManager.ApplyToggle(selectionToggle, graphic: text);
                        }
                        break;
                    }
                case SelectionType.Images: {
                        foreach (var spriteAsset in GameData.Current.assets.sprites)
                        {
                            if (!RTString.SearchString(PrefabCreatorDialog.SelectionSearchTerm, spriteAsset.name))
                                continue;

                            var selection = PrefabEditor.inst.selectionPrefab.Duplicate(PrefabCreatorDialog.SelectionContent, "grid");
                            var text = selection.transform.Find("text").GetComponent<Text>();
                            text.text = spriteAsset.name;
                            text.rectTransform.sizeDelta = new Vector2(300f, 32f);

                            var selectionToggle = selection.GetComponent<Toggle>();
                            selectionToggle.SetIsOnWithoutNotify(selectedForPrefabCreator.GetValueOrDefault(spriteAsset.name, false));
                            selectionToggle.onValueChanged.NewListener(_val =>
                            {
                                selectedForPrefabCreator[spriteAsset.name] = _val;
                                if (!_val)
                                    selectedSpriteAssets.Remove(x => x.name == spriteAsset.name);
                                else
                                    selectedSpriteAssets.Add(spriteAsset);
                            });
                            EditorThemeManager.ApplyToggle(selectionToggle, graphic: text);
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Updates the currently selected quick prefab.
        /// </summary>
        /// <param name="prefab">Prefab to set. Can be null to clear the selection.</param>
        public void UpdateCurrentPrefab(Prefab prefab)
        {
            selectingQuickPrefab = false;
            currentQuickPrefab = prefab;

            selectQuickPrefabText.text = (!prefab ? "-Select Prefab-" : "<color=#669e37>-Prefab-</color>") + "\n" + (!prefab ? "n/a" : prefab.name);
        }

        /// <summary>
        /// Pastes the copied prefab.
        /// </summary>
        public void PastePrefab()
        {
            if (string.IsNullOrEmpty(copiedPrefabPath))
            {
                EditorManager.inst.DisplayNotification("No prefab has been copied yet!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (!RTFile.FileExists(copiedPrefabPath))
            {
                EditorManager.inst.DisplayNotification("Copied prefab no longer exists.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var copiedPrefabsFolder = RTFile.GetDirectory(copiedPrefabPath);
            CoreHelper.Log($"Copied Folder: {copiedPrefabsFolder}");

            var prefabsPath = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath);
            if (copiedPrefabsFolder == prefabsPath)
            {
                EditorManager.inst.DisplayNotification("Source and destination are the same.", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            var destination = copiedPrefabPath.Replace(copiedPrefabsFolder, prefabsPath);
            CoreHelper.Log($"Destination: {destination}");
            if (RTFile.FileExists(destination))
            {
                EditorManager.inst.DisplayNotification("File already exists.", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            if (shouldCutPrefab)
            {
                if (RTFile.MoveFile(copiedPrefabPath, destination))
                    EditorManager.inst.DisplayNotification($"Succesfully moved {Path.GetFileName(destination)}!", 2f, EditorManager.NotificationType.Success);
            }
            else
            {
                if (RTFile.CopyFile(copiedPrefabPath, destination))
                    EditorManager.inst.DisplayNotification($"Succesfully pasted {Path.GetFileName(destination)}!", 2f, EditorManager.NotificationType.Success);
            }

            LoadPrefabs(RenderExternalPrefabs);
        }

        /// <summary>
        /// Refreshes the Internal Prefabs UI.
        /// </summary>
        /// <param name="updateCurrentPrefab">If the current quick prefab should be set instead of importing.</param>
        public IEnumerator RefreshInternalPrefabs(bool updateCurrentPrefab = false)
        {
            if (!GameData.Current)
                yield break;

            var config = EditorConfig.Instance;

            yield return CoroutineHelper.Seconds(0.03f);

            selectingQuickPrefab = updateCurrentPrefab;

            EditorContextMenu.AddContextMenu(RTEditor.inst.PrefabPopups.Internal.SearchField.gameObject,
                ButtonElement.ToggleButton("Filter: Used", () => filterUsed, () =>
                {
                    filterUsed = !filterUsed;
                    StartCoroutine(RefreshInternalPrefabs());
                }));

            RTEditor.inst.PrefabPopups.Internal.ClearContent();
            CreatePrefabButton(RTEditor.inst.PrefabPopups.Internal.Content, "New Internal Prefab", eventData =>
            {
                if (eventData.button == PointerEventData.InputButton.Right)
                {
                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonElement("Create prefab", () =>
                        {
                            PrefabEditor.inst.OpenDialog();
                            createInternal = true;
                        })
                        );

                    return;
                }

                if (RTEditor.inst.prefabPickerEnabled)
                    RTEditor.inst.prefabPickerEnabled = false;

                PrefabEditor.inst.OpenDialog();
                createInternal = true;
            });

            var prefabs = GameData.Current.prefabs;
            for (int i = 0; i < prefabs.Count; i++)
            {
                var prefab = prefabs[i];
                if (ContainsName(prefab, ObjectSource.Internal) && (!filterUsed || GameData.Current.prefabObjects.Any(x => x.prefabID == prefab.id)))
                    new PrefabPanel(ObjectSource.Internal, i).Init(prefab);
            }

            yield break;
        }

        /// <summary>
        /// Creates the "New * Prefab" button.
        /// </summary>
        /// <param name="parent">Parent to set.</param>
        /// <param name="name">Name to display.</param>
        /// <param name="action">Action to run when button is clicked.</param>
        /// <returns>Returns the created game object.</returns>
        public GameObject CreatePrefabButton(Transform parent, string name, Action<PointerEventData> action)
        {
            var add = EditorPrefabHolder.Instance.CreateAddButton(parent, "add new prefab");
            add.Text = name;
            add.OnClick.ClearAll();

            var hover = add.gameObject.AddComponent<HoverUI>();
            hover.animateSca = true;
            hover.animatePos = false;
            hover.size = EditorConfig.Instance.PrefabButtonHoverSize.Value;

            var contextClickable = add.gameObject.AddComponent<ContextClickable>();
            contextClickable.onClick = action;

            return add.gameObject;
        }

        public void RenderExternalPrefabs() => CoroutineHelper.StartCoroutine(IRenderExternalPrefabs());

        /// <summary>
        /// Renders the External Prefabs UI.
        /// </summary>
        public IEnumerator IRenderExternalPrefabs()
        {
            foreach (var prefabPanel in PrefabPanels.Where(x => x.Source == ObjectSource.External))
            {
                prefabPanel.SetActive(
                    prefabPanel.isFolder ?
                        RTString.SearchString(PrefabEditor.inst.externalSearchStr, Path.GetFileName(prefabPanel.Path)) :
                        ContainsName(prefabPanel.Item, ObjectSource.External));
            }

            yield break;
        }

        /// <summary>
        /// Checks if the prefab is being searched for.
        /// </summary>
        /// <param name="prefab">Prefab reference.</param>
        /// <param name="dialog">Prefabs' dialog.</param>
        /// <returns>Returns true if the prefab is being searched for, otherwise returns false.</returns>
        public bool ContainsName(Prefab prefab, ObjectSource source) => RTString.SearchString(source == ObjectSource.External ? PrefabEditor.inst.externalSearchStr : PrefabEditor.inst.internalSearchStr, prefab.name, prefab.GetPrefabType().name);

        /// <summary>
        /// Imports a prefab into the internal prefabs list.
        /// </summary>
        /// <param name="prefab">Prefab to import.</param>
        public Prefab ImportPrefabIntoLevel(Prefab prefab)
        {
            Debug.Log($"{PrefabEditor.inst.className}Adding Prefab: [{prefab.name}]");
            var tmpPrefab = prefab.Copy();
            int num = GameData.Current.prefabs.FindAll(x => Regex.Replace(x.name, "( +\\[\\d+])", string.Empty) == tmpPrefab.name).Count;
            if (num > 0)
                tmpPrefab.name = $"{tmpPrefab.name} [{num}]";

            GameData.Current.prefabs.Add(tmpPrefab);
            StartCoroutine(RefreshInternalPrefabs());

            Example.Current?.brain?.Notice(ExampleBrain.Notices.IMPORT_PREFAB, new PrefabNoticeParameters(tmpPrefab));

            return tmpPrefab;
        }

        public bool UpdateLevelPrefab(Prefab prefab)
        {
            Debug.Log($"{PrefabEditor.inst.className}Updating Prefab: [{prefab.name}]");
            if (!GameData.Current.prefabs.TryFind(x => x.name == prefab.name, out Prefab internalPrefab))
                return false;

            var origID = internalPrefab.id;
            internalPrefab.CopyData(prefab, false);
            internalPrefab.id = origID;
            StartCoroutine(RefreshInternalPrefabs());

            GameData.Current.prefabObjects.FindAll(x => x.prefabID == prefab.id && string.IsNullOrEmpty(x.PrefabInstanceID)).ForEach(x =>
            {
                RTLevelBase runtimeLevel = x.runtimeObject?.ParentRuntime ?? RTLevel.Current;

                runtimeLevel?.UpdatePrefab(x, recalculate: runtimeLevel is not RTLevel);
            });
            RTLevel.Current?.RecalculateObjectStates();

            Example.Current?.brain?.Notice(ExampleBrain.Notices.IMPORT_PREFAB, new PrefabNoticeParameters(internalPrefab));

            return true;
        }

        // RTPrefabEditor.inst.UploadPrefab(RTPrefabEditor.inst.CurrentPrefabPanel);
        public void UploadPrefab(PrefabPanel prefabPanel)
        {
            var prefab = prefabPanel.Item;
            if (!prefab)
                return;

            EditorServerManager.inst.Upload(
                url: $"{AlephNetwork.ArcadeServerURL}api/prefab",
                fileName: RTFile.FormatLegacyFileName(prefab.name),
                uploadable: prefab,
                transfer: tempDirectory =>
                {
                    prefab.WriteToFile(RTFile.CombinePaths(tempDirectory, $"prefab{FileFormat.LSP.Dot()}"));
                    var icon = prefab.icon;
                    if (icon)
                        File.WriteAllBytes(RTFile.CombinePaths(tempDirectory, $"icon{FileFormat.JPG.Dot()}"), icon.texture.EncodeToJPG());

                    var prefabType = prefab.GetPrefabType();
                    if (!prefabType || prefabType == PrefabType.InvalidType)
                        return;

                    RTFile.WriteToFile(RTFile.CombinePaths(tempDirectory, $"type{FileFormat.LSPT.Dot()}"), prefabType.ToJSON().ToString());
                    if (!prefabType.icon)
                        return;

                    File.WriteAllBytes(RTFile.CombinePaths(tempDirectory, $"type_icon{FileFormat.PNG.Dot()}"), prefabType.icon.texture.EncodeToPNG());
                },
                saveFile: () =>
                {
                    UpdatePrefabFile(prefabPanel);
                },
                onUpload: () =>
                {
                    RenderPrefabEditorDialog(prefabPanel);
                });
        }

        public void DeleteServerPrefab(PrefabPanel prefabPanel)
        {
            var prefab = prefabPanel.Item;
            if (!prefab)
                return;

            EditorServerManager.inst.Delete(
                url: $"{AlephNetwork.ArcadeServerURL}api/prefab",
                uploadable: prefab,
                saveFile: () =>
                {
                    UpdatePrefabFile(prefabPanel);
                },
                onDelete: () =>
                {
                    RenderPrefabEditorDialog(prefabPanel);
                });
        }

        public void PullServerPrefab(PrefabPanel prefabPanel)
        {
            var prefab = prefabPanel.Item;
            if (!prefab)
                return;

            EditorServerManager.inst.Pull(
                url: AlephNetwork.PrefabDownloadURL,
                uploadable: prefab,
                pull: jn => EditorServerManager.inst.DownloadPrefab(jn["id"], jn["name"]));
        }

        #endregion

        #endregion
    }
}
