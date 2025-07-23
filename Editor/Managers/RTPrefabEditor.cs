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

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Companion.Data.Parameters;
using BetterLegacy.Companion.Entity;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Popups;

namespace BetterLegacy.Editor.Managers
{
    public class RTPrefabEditor : MonoBehaviour
    {
        public static RTPrefabEditor inst;

        #region Variables

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

        public bool selectingPrefab;

        public GameObject prefabTypePrefab;
        public GameObject prefabTypeTogglePrefab;

        public Button prefabTypeReloadButton;

        public string NewPrefabTypeID { get; set; }
        public string NewPrefabDescription { get; set; }

        public List<PrefabPanel> PrefabPanels { get; set; } = new List<PrefabPanel>();

        public static bool ImportPrefabsDirectly { get; set; }

        public PrefabCreatorDialog PrefabCreator { get; set; }
        public PrefabObjectEditorDialog PrefabObjectEditor { get; set; }
        public PrefabExternalEditorDialog PrefabExternalEditor { get; set; }

        #endregion

        public static void Init() => PrefabEditor.inst?.gameObject?.AddComponent<RTPrefabEditor>();

        void Awake() => inst = this;

        void Start() => StartCoroutine(SetupUI());

        void Update() => PrefabObjectEditor?.ModifiersDialog?.Tick();

        // todo:
        // rework this UI generation code
        IEnumerator SetupUI()
        {
            while (!PrefabEditor.inst || !EditorManager.inst || !EditorManager.inst.EditorDialogsDictionary.ContainsKey("Prefab Popup") || EditorPrefabHolder.Instance == null || !EditorPrefabHolder.Instance.Function1Button)
                yield return null;

            // A
            {
                loadingPrefabTypes = true;
                PrefabEditor.inst.StartCoroutine(LoadPrefabs());
                PrefabEditor.inst.OffsetLine = PrefabEditor.inst.OffsetLinePrefab.Duplicate(EditorManager.inst.timeline.transform, "offset line");
                PrefabEditor.inst.OffsetLine.transform.AsRT().pivot = Vector2.one;

                var prefabPopup = EditorManager.inst.GetDialog("Prefab Popup").Dialog;
                PrefabEditor.inst.dialog = EditorManager.inst.GetDialog("Prefab Editor").Dialog;
                PrefabEditor.inst.externalPrefabDialog = prefabPopup.Find("external prefabs");
                PrefabEditor.inst.internalPrefabDialog = prefabPopup.Find("internal prefabs");

                var externalContextClickable = PrefabEditor.inst.externalPrefabDialog.gameObject.AddComponent<ContextClickable>();
                externalContextClickable.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Create folder", () =>
                        {
                            RTEditor.inst.ShowFolderCreator(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath), () => { RTEditor.inst.UpdatePrefabPath(true); RTEditor.inst.HideNameEditor(); });
                        }),
                        new ButtonFunction("Create Prefab", () =>
                        {
                            PrefabEditor.inst.OpenDialog();
                            createInternal = false;
                        }),
                        new ButtonFunction(true),
                        new ButtonFunction("Paste", PastePrefab)
                        );
                };
                
                var internalContextClickable = PrefabEditor.inst.internalPrefabDialog.gameObject.AddComponent<ContextClickable>();
                internalContextClickable.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Create Prefab", () =>
                        {
                            PrefabEditor.inst.OpenDialog();
                            createInternal = true;
                        })
                        );
                };

                PrefabEditor.inst.externalSearch = PrefabEditor.inst.externalPrefabDialog.Find("search-box/search").GetComponent<InputField>();
                PrefabEditor.inst.internalSearch = PrefabEditor.inst.internalPrefabDialog.Find("search-box/search").GetComponent<InputField>();
                PrefabEditor.inst.externalContent = PrefabEditor.inst.externalPrefabDialog.Find("mask/content");
                PrefabEditor.inst.internalContent = PrefabEditor.inst.internalPrefabDialog.Find("mask/content");

                var externalSelectGUI = PrefabEditor.inst.externalPrefabDialog.gameObject.AddComponent<SelectGUI>();
                var internalSelectGUI = PrefabEditor.inst.internalPrefabDialog.gameObject.AddComponent<SelectGUI>();
                externalSelectGUI.ogPos = PrefabEditor.inst.externalPrefabDialog.position;
                internalSelectGUI.ogPos = PrefabEditor.inst.internalPrefabDialog.position;
                externalSelectGUI.target = PrefabEditor.inst.externalPrefabDialog;
                internalSelectGUI.target = PrefabEditor.inst.internalPrefabDialog;

                PrefabEditor.inst.internalPrefabDialog.Find("Panel/Text").GetComponent<Text>().text = "Internal Prefabs";

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

            var selectToggle = selectQuickPrefabButton.gameObject.AddComponent<ContextClickable>();
            selectToggle.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Assign", () =>
                    {
                        selectQuickPrefabText.text = "<color=#669e37>Selecting</color>";
                        StartCoroutine(RefreshInternalPrefabs(true));
                    }),
                    new ButtonFunction("Remove", () =>
                    {
                        currentQuickPrefab = null;
                        RenderPopup();
                    }),
                    new ButtonFunction("Select Target", () =>
                    {
                        EditorTimeline.inst.onSelectTimelineObject = timelineObject =>
                        {
                            if (!timelineObject.isBeatmapObject)
                            {
                                quickPrefabTarget = null;
                                return;
                            }

                            quickPrefabTarget = timelineObject.GetData<BeatmapObject>();
                        };
                    }),
                    new ButtonFunction("Remove Target", () =>
                    {
                        quickPrefabTarget = null;
                    })
                    );
            };

            try
            {
                prefabCreatorName = PrefabEditor.inst.dialog.Find("data/name/input").GetComponent<InputField>();

                prefabCreatorOffsetSlider = PrefabEditor.inst.dialog.Find("data/offset/slider").GetComponent<Slider>();
                prefabCreatorOffset = PrefabEditor.inst.dialog.Find("data/offset/input").GetComponent<InputField>();

                // Editor Theme
                {
                    #region External

                    EditorThemeManager.AddGraphic(PrefabEditor.inst.externalPrefabDialog.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Bottom_Left_I);

                    var externalPanel = PrefabEditor.inst.externalPrefabDialog.Find("Panel");
                    externalPanel.AsRT().sizeDelta = new Vector2(32f, 32f);
                    EditorThemeManager.AddGraphic(externalPanel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

                    var externalClose = externalPanel.Find("x").GetComponent<Button>();
                    Destroy(externalClose.GetComponent<Animator>());
                    externalClose.transition = Selectable.Transition.ColorTint;
                    externalClose.image.rectTransform.anchoredPosition = Vector2.zero;
                    EditorThemeManager.AddSelectable(externalClose, ThemeGroup.Close);
                    EditorThemeManager.AddGraphic(externalClose.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

                    EditorThemeManager.AddLightText(externalPanel.Find("Text").GetComponent<Text>());

                    EditorThemeManager.AddScrollbar(PrefabEditor.inst.externalPrefabDialog.transform.Find("Scrollbar").GetComponent<Scrollbar>(), scrollbarRoundedSide: SpriteHelper.RoundedSide.Bottom_Right_I);

                    EditorThemeManager.AddInputField(PrefabEditor.inst.externalSearch, ThemeGroup.Search_Field_2);

                    #endregion

                    #region Internal

                    EditorThemeManager.AddGraphic(PrefabEditor.inst.internalPrefabDialog.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Bottom_Left_I);

                    var internalPanel = PrefabEditor.inst.internalPrefabDialog.Find("Panel");
                    internalPanel.AsRT().sizeDelta = new Vector2(32f, 32f);
                    EditorThemeManager.AddGraphic(internalPanel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

                    var internalClose = internalPanel.Find("x").GetComponent<Button>();
                    Destroy(internalClose.GetComponent<Animator>());
                    internalClose.transition = Selectable.Transition.ColorTint;
                    internalClose.image.rectTransform.anchoredPosition = Vector2.zero;
                    EditorThemeManager.AddSelectable(internalClose, ThemeGroup.Close);
                    EditorThemeManager.AddGraphic(internalClose.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

                    EditorThemeManager.AddLightText(internalPanel.Find("Text").GetComponent<Text>());

                    EditorThemeManager.AddScrollbar(PrefabEditor.inst.internalPrefabDialog.transform.Find("Scrollbar").GetComponent<Scrollbar>(), scrollbarRoundedSide: SpriteHelper.RoundedSide.Bottom_Right_I);

                    EditorThemeManager.AddInputField(PrefabEditor.inst.internalSearch, ThemeGroup.Search_Field_2);

                    EditorThemeManager.AddGraphic(PrefabEditor.inst.internalPrefabDialog.Find("select_prefab").GetComponent<Image>(), ThemeGroup.Background_2, true, roundedSide: SpriteHelper.RoundedSide.Bottom_Left_I);

                    EditorThemeManager.AddSelectable(selectQuickPrefabButton, ThemeGroup.Function_2);
                    EditorThemeManager.AddGraphic(selectQuickPrefabButton.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Function_2_Text);
                    EditorThemeManager.AddGraphic(selectQuickPrefabText, ThemeGroup.Light_Text);

                    #endregion
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            try
            {
                PrefabCreator = new PrefabCreatorDialog();
                PrefabCreator.Init();
                PrefabObjectEditor = new PrefabObjectEditorDialog();
                PrefabObjectEditor.Init();
                PrefabExternalEditor = new PrefabExternalEditorDialog();
                PrefabExternalEditor.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        #region Prefab Objects

        public bool advancedParent;

        public PrefabObject copiedInstanceData;

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
            RenderEditorColors(prefabObject);

            RenderPrefabObjectInspector(prefabObject);

            RenderPrefabObjectOffset(prefabObject, prefab);
            RenderPrefabObjectName(prefab);
            RenderPrefabObjectType(prefab);

            PrefabObjectEditor.SavePrefabButton.button.onClick.NewListener(() =>
            {
                RTEditor.inst.PrefabPopups.Open();
                RTEditor.inst.PrefabPopups.GameObject.transform.GetChild(0).gameObject.SetActive(false);

                if (PrefabEditor.inst.externalContent)
                    StartCoroutine(RenderExternalPrefabs());

                savingToPrefab = true;
                prefabToSaveFrom = prefab;

                EditorManager.inst.DisplayNotification("Select an External Prefab to apply changes to.", 2f);
            });

            if (ModCompatibility.UnityExplorerInstalled && PrefabObjectEditor.InspectPrefab)
                PrefabObjectEditor.InspectPrefab.button.onClick.NewListener(() => ModCompatibility.Inspect(prefab));

            RenderPrefabObjectInfo(prefab);

            CoroutineHelper.StartCoroutine(PrefabObjectEditor.ModifiersDialog.RenderModifiers(prefabObject));
        }

        public void RenderPrefabObjectTags(PrefabObject prefabObject) => RTEditor.inst.RenderTags(prefabObject, PrefabObjectEditor);

        public void RenderPrefabObjectStartTime(PrefabObject prefabObject)
        {
            PrefabObjectEditor.StartTimeField.lockToggle.onValueChanged.ClearAll();
            PrefabObjectEditor.StartTimeField.lockToggle.isOn = prefabObject.editorData.locked;
            PrefabObjectEditor.StartTimeField.lockToggle.onValueChanged.AddListener(_val =>
            {
                prefabObject.editorData.locked = _val;
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(prefabObject));
            });

            PrefabObjectEditor.CollapseToggle.onValueChanged.ClearAll();
            PrefabObjectEditor.CollapseToggle.isOn = prefabObject.editorData.collapse;
            PrefabObjectEditor.CollapseToggle.onValueChanged.AddListener(_val =>
            {
                prefabObject.editorData.collapse = _val;
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(prefabObject));
            });

            PrefabObjectEditor.StartTimeField.inputField.onValueChanged.ClearAll();
            PrefabObjectEditor.StartTimeField.inputField.text = prefabObject.StartTime.ToString();
            PrefabObjectEditor.StartTimeField.inputField.onValueChanged.AddListener(_val =>
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

            var startTimeContextMenu = PrefabObjectEditor.StartTimeField.inputField.gameObject.GetOrAddComponent<ContextClickable>();
            startTimeContextMenu.onClick = null;
            startTimeContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Go to Start Time", () => AudioManager.inst.SetMusicTime(prefabObject.StartTime)),
                    new ButtonFunction("Go to Spawn Time", () => AudioManager.inst.SetMusicTime(prefabObject.StartTime + prefabObject.GetPrefab().offset)));
            };

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

            PrefabObjectEditor.AutokillDropdown.onValueChanged.ClearAll();
            PrefabObjectEditor.AutokillDropdown.value = (int)prefabObject.autoKillType;
            PrefabObjectEditor.AutokillDropdown.onValueChanged.AddListener(_val =>
            {
                prefabObject.autoKillType = (PrefabAutoKillType)_val;
                RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.AUTOKILL);
            });

            PrefabObjectEditor.AutokillField.inputField.onValueChanged.ClearAll();
            PrefabObjectEditor.AutokillField.inputField.text = prefabObject.autoKillOffset.ToString();
            PrefabObjectEditor.AutokillField.inputField.onValueChanged.AddListener(_val =>
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
            PrefabObjectEditor.LeftContent.Find("parent label").gameObject.SetActive(RTEditor.ShowModdedUI);
            PrefabObjectEditor.LeftContent.Find("parent").gameObject.SetActive(RTEditor.ShowModdedUI);
            PrefabObjectEditor.LeftContent.Find("parent_more").gameObject.SetActive(RTEditor.ShowModdedUI && advancedParent);

            if (!RTEditor.ShowModdedUI)
                return;

            string parent = prefabObject.parent;

            var parentTextText = PrefabObjectEditor.LeftContent.Find("parent/text/text").GetComponent<Text>();
            var parentText = PrefabObjectEditor.LeftContent.Find("parent/text").GetComponent<Button>();
            var parentMore = PrefabObjectEditor.LeftContent.Find("parent/more").GetComponent<Button>();
            var parent_more = PrefabObjectEditor.LeftContent.Find("parent_more");
            var parentParent = PrefabObjectEditor.LeftContent.Find("parent/parent").GetComponent<Button>();
            var parentClear = PrefabObjectEditor.LeftContent.Find("parent/clear parent").GetComponent<Button>();
            var parentPicker = PrefabObjectEditor.LeftContent.Find("parent/parent picker").GetComponent<Button>();
            var spawnOnce = parent_more.Find("spawn_once").GetComponent<Toggle>();
            var parentInfo = parentText.GetComponent<HoverTooltip>();

            parentText.transform.AsRT().sizeDelta = new Vector2(!string.IsNullOrEmpty(parent) ? 201f : 241f, 32f);

            parentParent.onClick.ClearAll();
            parentParent.onClick.AddListener(() => ObjectEditor.inst.ShowParentSearch(EditorTimeline.inst.GetTimelineObject(prefabObject)));

            parentClear.onClick.ClearAll();

            parentPicker.onClick.ClearAll();
            parentPicker.onClick.AddListener(() => RTEditor.inst.parentPickerEnabled = true);

            parentClear.gameObject.SetActive(!string.IsNullOrEmpty(parent));

            parent_more.AsRT().sizeDelta = new Vector2(351f, 152f);

            if (string.IsNullOrEmpty(parent))
            {
                parentText.interactable = false;
                parentMore.interactable = false;
                parent_more.gameObject.SetActive(false);
                parentTextText.text = "No Parent Object";

                parentInfo.tooltipLangauges[0].hint = string.IsNullOrEmpty(parent) ? "Object not parented." : "No parent found.";
                parentText.onClick.ClearAll();
                parentMore.onClick.ClearAll();

                return;
            }

            string p = null;

            if (GameData.Current.beatmapObjects.TryFind(x => x.id == parent, out BeatmapObject beatmapObjectParent))
            {
                p = beatmapObjectParent.name;
                parentInfo.tooltipLangauges[0].hint = "Currently selected parent.";
            }
            else if (parent == "CAMERA_PARENT")
            {
                p = "[CAMERA]";
                parentInfo.tooltipLangauges[0].hint = "Object parented to the camera.";
            }

            parentText.interactable = p != null;
            parentMore.interactable = p != null;

            parent_more.gameObject.SetActive(p != null && advancedParent);

            parentClear.onClick.AddListener(() =>
            {
                prefabObject.parent = "";

                // Since parent has no affect on the timeline object, we will only need to update the physical object.
                RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.PARENT);
                RenderPrefabObjectParent(prefabObject);
            });

            if (p == null)
            {
                parentTextText.text = "No Parent Object";
                parentInfo.tooltipLangauges[0].hint = string.IsNullOrEmpty(parent) ? "Object not parented." : "No parent found.";
                parentText.onClick.ClearAll();
                parentMore.onClick.ClearAll();

                return;
            }

            parentTextText.text = p;

            parentText.onClick.ClearAll();
            parentText.onClick.AddListener(() =>
            {
                if (GameData.Current.beatmapObjects.Find(x => x.id == parent) != null &&
                    parent != "CAMERA_PARENT" &&
                    EditorTimeline.inst.timelineObjects.TryFind(x => x.ID == parent, out TimelineObject timelineObject))
                    EditorTimeline.inst.SetCurrentObject(timelineObject);
                else if (parent == "CAMERA_PARENT")
                {
                    EditorTimeline.inst.SetLayer(EditorTimeline.LayerType.Events);
                    EventEditor.inst.SetCurrentEvent(0, GameData.Current.ClosestEventKeyframe(0));
                }
            });

            parentMore.onClick.ClearAll();
            parentMore.onClick.AddListener(() =>
            {
                advancedParent = !advancedParent;
                parent_more.gameObject.SetActive(RTEditor.ShowModdedUI && advancedParent);
            });
            parent_more.gameObject.SetActive(RTEditor.ShowModdedUI && advancedParent);

            spawnOnce.onValueChanged.ClearAll();
            spawnOnce.gameObject.SetActive(true);
            spawnOnce.isOn = prefabObject.desync;
            spawnOnce.onValueChanged.AddListener(_val =>
            {
                prefabObject.desync = _val;
                RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.PARENT);
            });

            for (int i = 0; i < 3; i++)
            {
                var _p = parent_more.GetChild(i + 2);

                var parentOffset = prefabObject.parentOffsets[i];

                var index = i;

                // Parent Type
                var tog = _p.GetChild(2).GetComponent<Toggle>();
                tog.onValueChanged.ClearAll();
                tog.isOn = prefabObject.GetParentType(i);
                tog.onValueChanged.AddListener(_val =>
                {
                    prefabObject.SetParentType(index, _val);

                    // Since updating parent type has no affect on the timeline object, we will only need to update the physical object.
                    RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.PARENT);
                });

                // Parent Offset
                var pif = _p.GetChild(3).GetComponent<InputField>();
                var lel = _p.GetChild(3).GetComponent<LayoutElement>();
                lel.minWidth = 64f;
                lel.preferredWidth = 64f;
                pif.onValueChanged.ClearAll();
                pif.text = parentOffset.ToString();
                pif.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        prefabObject.SetParentOffset(index, num);

                        // Since updating parent type has no affect on the timeline object, we will only need to update the physical object.
                        RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.PARENT);
                    }
                });

                TriggerHelper.AddEventTriggers(pif.gameObject, TriggerHelper.ScrollDelta(pif));

                var additive = _p.GetChild(4).GetComponent<Toggle>();
                additive.onValueChanged.ClearAll();
                additive.gameObject.SetActive(true);
                var parallax = _p.GetChild(5).GetComponent<InputField>();
                parallax.onValueChanged.ClearAll();
                parallax.gameObject.SetActive(true);

                additive.isOn = prefabObject.parentAdditive[i] == '1';
                additive.onValueChanged.AddListener(_val =>
                {
                    prefabObject.SetParentAdditive(index, _val);
                    RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.PARENT);
                });
                parallax.text = prefabObject.parentParallax[index].ToString();
                parallax.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        prefabObject.parentParallax[index] = num;

                        // Since updating parent type has no affect on the timeline object, we will only need to update the physical object.
                        RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.PARENT);
                    }
                });

                TriggerHelper.AddEventTriggers(parallax.gameObject, TriggerHelper.ScrollDelta(parallax));
            }
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
        }

        public void RenderPrefabObjectRepeat(PrefabObject prefabObject)
        {
            PrefabObjectEditor.LeftContent.Find("repeat label").gameObject.SetActive(RTEditor.ShowModdedUI);
            PrefabObjectEditor.LeftContent.Find("repeat").gameObject.SetActive(RTEditor.ShowModdedUI);

            if (!RTEditor.ShowModdedUI)
                return;

            PrefabObjectEditor.RepeatCountField.inputField.onValueChanged.ClearAll();
            PrefabObjectEditor.RepeatCountField.inputField.text = Mathf.Clamp(prefabObject.RepeatCount, 0, 1000).ToString();
            PrefabObjectEditor.RepeatCountField.inputField.onValueChanged.AddListener(_val =>
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

            PrefabObjectEditor.RepeatOffsetTimeField.inputField.onValueChanged.ClearAll();
            PrefabObjectEditor.RepeatOffsetTimeField.inputField.text = Mathf.Clamp(prefabObject.RepeatOffsetTime, 0f, 60f).ToString();
            PrefabObjectEditor.RepeatOffsetTimeField.inputField.onValueChanged.AddListener(_val =>
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

            PrefabObjectEditor.SpeedField.inputField.onValueChanged.ClearAll();
            PrefabObjectEditor.SpeedField.inputField.text = prefabObject.Speed.ToString();
            PrefabObjectEditor.SpeedField.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    prefabObject.Speed = num;
            });

            TriggerHelper.IncreaseDecreaseButtons(PrefabObjectEditor.SpeedField, min: 0.1f, max: PrefabObject.MAX_PREFAB_OBJECT_SPEED);
            TriggerHelper.AddEventTriggers(PrefabObjectEditor.SpeedField.inputField.gameObject, TriggerHelper.ScrollDelta(PrefabObjectEditor.SpeedField.inputField, min: 0.1f, max: PrefabObject.MAX_PREFAB_OBJECT_SPEED));
        }
        
        public void RenderPrefabObjectInstanceData(PrefabObject prefabObject)
        {
            PrefabObjectEditor.CopyInstanceDataButton.button.onClick.NewListener(() =>
            {
                copiedInstanceData = prefabObject.Copy();
                EditorManager.inst.DisplayNotification($"Copied Prefab instance data.", 2f, EditorManager.NotificationType.Success);
                RenderPrefabObjectInstanceData(prefabObject);
            });
            PrefabObjectEditor.PasteInstanceDataButton.button.onClick.NewListener(() =>
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
            PrefabObjectEditor.RemoveInstanceDataButton.button.onClick.NewListener(() =>
            {
                copiedInstanceData = null;
                EditorManager.inst.DisplayNotification($"Removed copied Prefab instance data.", 2f, EditorManager.NotificationType.Success);
                RenderPrefabObjectInstanceData(prefabObject);
            });
        }

        public void RenderPrefabObjectLayer(PrefabObject prefabObject)
        {
            PrefabObjectEditor.EditorLayerField.gameObject.SetActive(RTEditor.NotSimple);

            if (RTEditor.NotSimple)
            {
                PrefabObjectEditor.EditorLayerField.image.color = EditorTimeline.GetLayerColor(prefabObject.editorData.Layer);
                PrefabObjectEditor.EditorLayerField.onValueChanged.ClearAll();
                PrefabObjectEditor.EditorLayerField.text = (prefabObject.editorData.Layer + 1).ToString();
                PrefabObjectEditor.EditorLayerField.onValueChanged.AddListener(_val =>
                {
                    if (int.TryParse(_val, out int n))
                    {
                        n = n - 1;
                        if (n < 0)
                            n = 0;

                        prefabObject.editorData.Layer = EditorTimeline.GetLayer(n);
                        EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(prefabObject));
                        RenderPrefabObjectLayer(prefabObject);
                    }
                    else
                        EditorManager.inst.DisplayNotification("Text is not correct format!", 1f, EditorManager.NotificationType.Error);
                });

                TriggerHelper.AddEventTriggers(PrefabObjectEditor.EditorLayerField.gameObject, TriggerHelper.ScrollDeltaInt(PrefabObjectEditor.EditorLayerField, min: 1, max: int.MaxValue));

                var editorLayerContextMenu = PrefabObjectEditor.EditorLayerField.gameObject.GetOrAddComponent<ContextClickable>();
                editorLayerContextMenu.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Go to Editor Layer", () => EditorTimeline.inst.SetLayer(prefabObject.editorData.Layer, EditorTimeline.LayerType.Objects))
                        );
                };
            }

            if (PrefabObjectEditor.EditorLayerToggles == null)
                return;

            PrefabObjectEditor.EditorSettingsParent.Find("layer").gameObject.SetActive(!RTEditor.NotSimple);

            if (RTEditor.NotSimple)
                return;

            for (int i = 0; i < PrefabObjectEditor.EditorLayerToggles.Length; i++)
            {
                var index = i;
                var toggle = PrefabObjectEditor.EditorLayerToggles[i];
                toggle.onValueChanged.ClearAll();
                toggle.isOn = index == prefabObject.editorData.Layer;
                toggle.onValueChanged.AddListener(_val =>
                {
                    prefabObject.editorData.Layer = index;
                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(prefabObject));
                    RenderPrefabObjectLayer(prefabObject);
                });
            }
        }

        public void RenderPrefabObjectBin(PrefabObject prefabObject)
        {
            PrefabObjectEditor.BinSlider.onValueChanged.ClearAll();
            PrefabObjectEditor.BinSlider.maxValue = EditorTimeline.inst.BinCount;
            PrefabObjectEditor.BinSlider.value = prefabObject.editorData.Bin;
            PrefabObjectEditor.BinSlider.onValueChanged.AddListener(_val =>
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
            PrefabObjectEditor.EditorIndexField.inputField.onEndEdit.ClearAll();
            PrefabObjectEditor.EditorIndexField.inputField.onValueChanged.ClearAll();
            PrefabObjectEditor.EditorIndexField.inputField.text = currentIndex.ToString();
            PrefabObjectEditor.EditorIndexField.inputField.onEndEdit.AddListener(_val =>
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

            var contextMenu = PrefabObjectEditor.EditorIndexField.inputField.gameObject.GetOrAddComponent<ContextClickable>();
            contextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Select Previous", () =>
                    {
                        if (currentIndex <= 0)
                        {
                            EditorManager.inst.DisplayNotification($"There are no previous objects to select.", 2f, EditorManager.NotificationType.Error);
                            return;
                        }

                        var prevObject = GameData.Current.prefabObjects[currentIndex - 1];

                        if (!prevObject)
                            return;

                        var timelineObject = EditorTimeline.inst.GetTimelineObject(prevObject);

                        if (timelineObject)
                            EditorTimeline.inst.SetCurrentObject(timelineObject, EditorConfig.Instance.BringToSelection.Value);
                    }),
                    new ButtonFunction("Select Previous", () =>
                    {
                        if (currentIndex >= GameData.Current.prefabObjects.Count - 1)
                        {
                            EditorManager.inst.DisplayNotification($"There are no previous objects to select.", 2f, EditorManager.NotificationType.Error);
                            return;
                        }

                        var nextObject = GameData.Current.prefabObjects[currentIndex + 1];

                        if (!nextObject)
                            return;

                        var timelineObject = EditorTimeline.inst.GetTimelineObject(nextObject);

                        if (timelineObject)
                            EditorTimeline.inst.SetCurrentObject(timelineObject, EditorConfig.Instance.BringToSelection.Value);
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Select First", () =>
                    {
                        if (GameData.Current.prefabObjects.IsEmpty())
                        {
                            EditorManager.inst.DisplayNotification($"There are no Prefab Objects!", 3f, EditorManager.NotificationType.Warning);
                            return;
                        }

                        var prevObject = GameData.Current.prefabObjects.First();

                        if (!prevObject)
                            return;

                        var timelineObject = EditorTimeline.inst.GetTimelineObject(prevObject);

                        if (timelineObject)
                            EditorTimeline.inst.SetCurrentObject(timelineObject, EditorConfig.Instance.BringToSelection.Value);
                    }),
                    new ButtonFunction("Select Last", () =>
                    {
                        if (GameData.Current.prefabObjects.IsEmpty())
                        {
                            EditorManager.inst.DisplayNotification($"There are no Prefab Objects!", 3f, EditorManager.NotificationType.Warning);
                            return;
                        }

                        var nextObject = GameData.Current.prefabObjects.Last();

                        if (!nextObject)
                            return;

                        var timelineObject = EditorTimeline.inst.GetTimelineObject(nextObject);

                        if (timelineObject)
                            EditorTimeline.inst.SetCurrentObject(timelineObject, EditorConfig.Instance.BringToSelection.Value);
                    }));
            };
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

            PrefabObjectEditor.InspectPrefabObject.button.onClick.NewListener(() => ModCompatibility.Inspect(prefabObject));
            PrefabObjectEditor.InspectRuntimeObjectButton.button.onClick.NewListener(() => ModCompatibility.Inspect(prefabObject.runtimeObject));
            PrefabObjectEditor.InspectTimelineObject.button.onClick.NewListener(() => ModCompatibility.Inspect(EditorTimeline.inst.GetTimelineObject(prefabObject)));
        }
        
        public void RenderPrefabObjectOffset(PrefabObject prefabObject, Prefab prefab)
        {
            PrefabObjectEditor.OffsetField.inputField.onValueChanged.ClearAll();
            PrefabObjectEditor.OffsetField.inputField.text = prefab.offset.ToString();
            PrefabObjectEditor.OffsetField.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float offset))
                {
                    prefab.offset = offset;
                    UpdateOffsets(prefabObject);
                }
            });
            TriggerHelper.IncreaseDecreaseButtons(PrefabObjectEditor.OffsetField.inputField, t: PrefabObjectEditor.OffsetField.transform);
            TriggerHelper.AddEventTriggers(PrefabObjectEditor.OffsetField.inputField.gameObject, TriggerHelper.ScrollDelta(PrefabObjectEditor.OffsetField.inputField));

            var offsetContextMenu = PrefabObjectEditor.OffsetField.inputField.gameObject.GetOrAddComponent<ContextClickable>();
            offsetContextMenu.onClick = null;
            offsetContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Set to Timeline Cursor", () =>
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
            };
        }

        public void RenderPrefabObjectName(Prefab prefab)
        {
            PrefabObjectEditor.NameField.onValueChanged.ClearAll();
            PrefabObjectEditor.NameField.text = prefab.name;
            PrefabObjectEditor.NameField.onValueChanged.AddListener(_val =>
            {
                prefab.name = _val;
                foreach (var prefabObject in GameData.Current.prefabObjects.FindAll(x => x.prefabID == prefab.id))
                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(prefabObject));
            });
        }

        public void RenderPrefabObjectType(Prefab prefab)
        {
            var prefabType = prefab.GetPrefabType();
            PrefabObjectEditor.PrefabTypeSelectorButton.button.image.color = prefabType.color;
            PrefabObjectEditor.PrefabTypeSelectorButton.label.text = prefabType.name;

            PrefabObjectEditor.PrefabTypeSelectorButton.button.onClick.NewListener(() =>
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

            var prefabables = GameData.Current.GetPrefabables().Where(x => x.SamePrefabInstance(prefabable));

            var objects = GameData.Current.beatmapObjects.FindAll(x => x.SamePrefabInstance(prefabable));
            var bgObjects = GameData.Current.backgroundObjects.FindAll(x => x.SamePrefabInstance(prefabable));

            if (prefabables.IsEmpty())
            {
                EditorManager.inst.DisplayNotification("No objects were found for the prefab to collapse.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            float startTime = 0f;

            if (!prefabables.IsEmpty())
                startTime = prefabables.Min(x => x.StartTime);

            int index = GameData.Current.prefabs.FindIndex(x => x.id == prefabID);
            var originalPrefab = GameData.Current.prefabs[index];
            PrefabObject prefabObject;

            if (createNew)
            {
                var newPrefab = originalPrefab.Copy();

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

                var newPrefab = new Prefab(originalPrefab.name, originalPrefab.type, originalPrefab.offset, objects, new List<PrefabObject>(), backgroundObjects: bgObjects);

                newPrefab.id = originalPrefab.id;
                newPrefab.typeID = originalPrefab.typeID;

                GameData.Current.prefabs[index] = newPrefab;
            }

            if (editorData)
            {
                prefabObject.editorData.Bin = editorData.Bin;
                prefabObject.editorData.Layer = editorData.Layer;
            }

            GameData.Current.prefabObjects.Add(prefabObject);

            EditorTimeline.inst.timelineObjects.ForLoopReverse((timelineObject, index) =>
            {
                if (timelineObject.isPrefabObject || !timelineObject.TryGetPrefabable(out IPrefabable otherPrefabable) || otherPrefabable.PrefabInstanceID != prefabInstanceID)
                    return;

                CoreHelper.Delete(timelineObject.GameObject);
                EditorTimeline.inst.timelineObjects.RemoveAt(index);
            });

            GameData.Current.beatmapObjects.ForLoopReverse((beatmapObject, index) =>
            {
                if (beatmapObject.prefabInstanceID != prefabInstanceID || beatmapObject.fromPrefab)
                    return;

                if (quickPrefabTarget && quickPrefabTarget.id == beatmapObject.id)
                    quickPrefabTarget = null;

                RTLevel.Current?.UpdateObject(beatmapObject, reinsert: false, recalculate: false);
                GameData.Current.beatmapObjects.RemoveAt(index);
            });
            GameData.Current.backgroundObjects.ForLoopReverse((backgroundObject, index) =>
            {
                if (backgroundObject.prefabInstanceID != prefabInstanceID || backgroundObject.fromPrefab)
                    return;

                RTLevel.Current?.UpdateBackgroundObject(backgroundObject, reinsert: false, recalculate: false);
                GameData.Current.backgroundObjects.RemoveAt(index);
            });

            RTLevel.Current?.AddPrefabToLevel(prefabObject, recalculate: false);

            GameData.Current.prefabObjects.FindAll(x => x.prefabID == originalPrefab.id).ForEach(x => RTLevel.Current?.UpdatePrefab(x, recalculate: false));
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
            string id = prefabObject.id;

            var sw = CoreHelper.StartNewStopwatch();

            Debug.Log($"{PrefabEditor.inst.className}Removing Prefab Object's spawned objects.");
            RTLevel.Current?.UpdatePrefab(prefabObject, false, false);

            EditorTimeline.inst.RemoveTimelineObject(EditorTimeline.inst.timelineObjects.Find(x => x.ID == id));

            GameData.Current.prefabObjects.RemoveAll(x => x.id == id);
            EditorTimeline.inst.DeselectAllObjects();

            Debug.Log($"{PrefabEditor.inst.className}Expanding Prefab Object.");
            new PrefabExpander(prefabObject).Select().Expand();

            EditorTimeline.inst.RenderTimelineObjects();

            prefabObject = null;
        }

        /// <summary>
        /// Creates an instance of a Prefab and imports it to the level.
        /// </summary>
        /// <param name="prefab">Prefab to import.</param>
        /// <param name="target">Object to target.</param>
        public void AddPrefabObjectToLevel(Prefab prefab, ObjectTransform? target = null)
        {
            var prefabObject = new PrefabObject
            {
                id = LSText.randomString(16),
                prefabID = prefab.id,
                StartTime = EditorConfig.Instance.BPMSnapsPrefabImport.Value ? RTEditor.SnapToBPM(EditorManager.inst.CurrentAudioPos) : EditorManager.inst.CurrentAudioPos,
            };

            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
                EditorTimeline.inst.SetLayer(EditorTimeline.LayerType.Objects);

            prefabObject.editorData.Layer = EditorManager.inst.layer;

            if (EditorConfig.Instance.SpawnPrefabsAtCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;
                prefabObject.events[0].values[0] = pos.x;
                prefabObject.events[0].values[1] = pos.y;
            }

            // Set default scale
            prefabObject.events[1].values[0] = 1f;
            prefabObject.events[1].values[1] = 1f;

            if (copiedInstanceData)
            {
                prefabObject.autoKillOffset = copiedInstanceData.autoKillOffset;
                prefabObject.autoKillType = copiedInstanceData.autoKillType;

                for (int i = 0; i < prefabObject.events.Count; i++)
                {
                    if (!copiedInstanceData.events.InRange(i))
                        return;

                    var copy = copiedInstanceData.events[i];
                    for (int j = 0; j < prefabObject.events[i].values.Length; j++)
                    {
                        if (copy.values.TryGetAt(j, out float val))
                            prefabObject.events[i].values[j] = val;
                    }
                    for (int j = 0; j < prefabObject.events[i].randomValues.Length; j++)
                    {
                        if (copy.randomValues.TryGetAt(j, out float val))
                            prefabObject.events[i].randomValues[j] = val;
                    }
                    prefabObject.events[i].random = copy.random;
                }

                prefabObject.CopyModifyableData(copiedInstanceData);
                prefabObject.CopyParentData(copiedInstanceData);
                prefabObject.RepeatCount = copiedInstanceData.RepeatCount;
                prefabObject.RepeatOffsetTime = copiedInstanceData.RepeatOffsetTime;
            }

            if (target.HasValue)
            {
                var anim = target.Value;
                prefabObject.events[0].values[0] = anim.position.x;
                prefabObject.events[0].values[1] = anim.position.y;
                prefabObject.events[1].values[0] = anim.scale.x;
                prefabObject.events[1].values[1] = anim.scale.y;
                prefabObject.events[2].values[0] = anim.rotation;
            }

            GameData.Current.prefabObjects.Add(prefabObject);

            RTLevel.Current?.AddPrefabToLevel(prefabObject, recalculate: false);
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

        /// <summary>
        /// List of Prefab types.
        /// </summary>
        public List<PrefabType> prefabTypes = new List<PrefabType>();

        void CreatePrefabTypesPopup()
        {
            var gameObject = Creator.NewUIObject("Prefab Types Popup", RTEditor.inst.popups, 9);

            var baseImage = gameObject.AddComponent<Image>();
            EditorThemeManager.AddGraphic(baseImage, ThemeGroup.Background_1);
            var baseSelectGUI = gameObject.AddComponent<SelectGUI>();

            gameObject.transform.AsRT().anchoredPosition = new Vector2(340f, 0f);
            gameObject.transform.AsRT().sizeDelta = new Vector2(400f, 600f);

            baseSelectGUI.target = gameObject.transform;
            baseSelectGUI.OverrideDrag = true;

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
            EditorThemeManager.AddSelectable(prefabTypeReloadButton, ThemeGroup.Function_2, false);

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

            EditorThemeManager.AddGraphic(gameObject.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Bottom_Left_I);
            EditorThemeManager.AddGraphic(maskImage, ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Bottom_Left_I);
            EditorThemeManager.AddGraphic(panelRT.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);
            EditorThemeManager.AddSelectable(closeButton, ThemeGroup.Close);
            EditorThemeManager.AddGraphic(closeButton.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);
            EditorThemeManager.AddLightText(title);

            EditorThemeManager.AddScrollbar(scrollRectSR.verticalScrollbar, scrollbarRoundedSide: SpriteHelper.RoundedSide.Bottom_Right_I);

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

            EditorHelper.AddEditorDropdown("View Prefab Types", "", "View", EditorSprites.SearchSprite, () =>
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

        public static bool loadingPrefabTypes = false;

        /// <summary>
        /// Loads all custom prefab types from the prefab types folder.
        /// </summary>
        public IEnumerator LoadPrefabTypes()
        {
            loadingPrefabTypes = true;
            prefabTypes.Clear();

            var defaultPrefabTypesJN = JSON.Parse(RTFile.ReadFromFile($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}default_prefabtypes{FileFormat.LSPT.Dot()}"));
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
            prefabTypeReloadButton.onClick.ClearAll();
            prefabTypeReloadButton.onClick.AddListener(() =>
            {
                StartCoroutine(LoadPrefabTypes());
                RenderPrefabTypesPopup(NewPrefabTypeID, onSelect);
            });

            RTEditor.inst.PrefabTypesPopup.ClearContent();

            var createPrefabType = PrefabEditor.inst.CreatePrefab.Duplicate(RTEditor.inst.PrefabTypesPopup.Content, "Create Prefab Type");
            createPrefabType.transform.AsRT().sizeDelta = new Vector2(402f, 32f);
            var createPrefabTypeText = createPrefabType.transform.Find("Text").GetComponent<Text>();
            createPrefabTypeText.text = "Create New Prefab Type";
            var createPrefabTypeButton = createPrefabType.GetComponent<Button>();
            createPrefabTypeButton.onClick.ClearAll();
            createPrefabTypeButton.onClick.AddListener(() =>
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

            EditorThemeManager.ApplyGraphic(createPrefabTypeButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(createPrefabTypeText, ThemeGroup.Add_Text);

            int num = 0;
            foreach (var prefabType in prefabTypes)
            {
                int index = num;
                var gameObject = prefabTypePrefab.Duplicate(RTEditor.inst.PrefabTypesPopup.Content, prefabType.name);

                var toggle = gameObject.transform.Find("Toggle").GetComponent<Toggle>();
                toggle.onValueChanged.ClearAll();
                toggle.isOn = current == prefabType.id;
                toggle.onValueChanged.AddListener(_val =>
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

                EditorThemeManager.AddInputField(inputField);

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

                EditorThemeManager.AddInputField(color);

                var deleteStorage = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.ClearAll();
                deleteStorage.button.interactable = !prefabType.isDefault;
                if (!prefabType.isDefault)
                    deleteStorage.button.onClick.AddListener(() =>
                    {
                        if (RTFile.FileExists(prefabType.filePath))
                            File.Delete(prefabType.filePath);

                        prefabTypes.RemoveAt(index);

                        RenderPrefabTypesPopup(current, onSelect);
                        SavePrefabTypes();
                    });

                EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);

                var setImageStorage = gameObject.transform.Find("Set Icon").GetComponent<FunctionButtonStorage>();
                setImageStorage.button.onClick.ClearAll();
                setImageStorage.button.interactable = !prefabType.isDefault;

                if (!prefabType.isDefault)
                    setImageStorage.button.onClick.AddListener(() =>
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

                setImageStorage.label.text = "Set Icon";

                EditorThemeManager.ApplyGraphic(setImageStorage.button.image, ThemeGroup.Function_1, true);
                EditorThemeManager.ApplyGraphic(setImageStorage.label, ThemeGroup.Function_1_Text);

                num++;
            }

            yield break;
        }

        #endregion

        #region Prefabs

        public BeatmapObject quickPrefabTarget;

        public Prefab currentQuickPrefab;

        public bool shouldCutPrefab;
        public string copiedPrefabPath;

        public bool prefabsLoading;

        GameObject prefabExternalUpAFolderButton;
        public GameObject prefabExternalAddButton;

        public bool filterUsed;

        public IEnumerator LoadPrefabs()
        {
            if (prefabsLoading)
                yield break;

            prefabsLoading = true;

            while (!PrefabEditor.inst || !PrefabEditor.inst.externalContent)
                yield return null;

            for (int i = PrefabPanels.Count - 1; i >= 0; i--)
            {
                var prefabPanel = PrefabPanels[i];
                if (prefabPanel.Dialog == PrefabDialog.External)
                {
                    Destroy(prefabPanel.GameObject);
                    PrefabPanels.RemoveAt(i);
                }
            }

            if (!prefabExternalAddButton)
            {
                CoreHelper.DestroyChildren(PrefabEditor.inst.externalContent);

                prefabExternalAddButton = CreatePrefabButton(PrefabEditor.inst.externalContent, "New External Prefab", eventData =>
                {
                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath), () => { RTEditor.inst.UpdatePrefabPath(true); RTEditor.inst.HideNameEditor(); })),
                            new ButtonFunction("Create prefab", () =>
                            {
                                PrefabEditor.inst.OpenDialog();
                                createInternal = false;
                            }),
                            new ButtonFunction("Paste Prefab", PastePrefab)
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
            else
            {
                var hover = prefabExternalAddButton.GetComponent<HoverUI>();
                hover.animateSca = true;
                hover.animatePos = false;
                hover.size = EditorConfig.Instance.PrefabButtonHoverSize.Value;
            }

            while (loadingPrefabTypes)
                yield return null;

            // Back
            if (!prefabExternalUpAFolderButton)
            {
                prefabExternalUpAFolderButton = EditorManager.inst.folderButtonPrefab.Duplicate(PrefabEditor.inst.externalContent, "back");
                var folderButtonStorageFolder = prefabExternalUpAFolderButton.GetComponent<FunctionButtonStorage>();
                var folderButtonFunctionFolder = prefabExternalUpAFolderButton.AddComponent<FolderButtonFunction>();

                var hoverUIFolder = prefabExternalUpAFolderButton.AddComponent<HoverUI>();
                hoverUIFolder.size = EditorConfig.Instance.PrefabButtonHoverSize.Value;
                hoverUIFolder.animatePos = false;
                hoverUIFolder.animateSca = true;

                folderButtonStorageFolder.label.text = "< Up a folder";

                folderButtonStorageFolder.button.onClick.ClearAll();
                folderButtonFunctionFolder.onClick = eventData =>
                {
                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath), () => { RTEditor.inst.UpdatePrefabPath(true); RTEditor.inst.HideNameEditor(); })),
                            new ButtonFunction("Paste Prefab", PastePrefab));

                        return;
                    }

                    if (RTEditor.inst.prefabPathField.text == RTEditor.inst.PrefabPath)
                    {
                        RTEditor.inst.prefabPathField.text = RTFile.GetDirectory(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath)).Replace(RTEditor.inst.BeatmapsPath + "/", "");
                        RTEditor.inst.UpdatePrefabPath(false);
                    }
                };

                EditorThemeManager.ApplySelectable(folderButtonStorageFolder.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(folderButtonStorageFolder.label);
            }

            prefabExternalUpAFolderButton.SetActive(RTFile.GetDirectory(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath)) != RTEditor.inst.BeatmapsPath);

            var directories = Directory.GetDirectories(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath), "*", SearchOption.TopDirectoryOnly);

            for (int i = 0; i < directories.Length; i++)
            {
                var directory = directories[i];
                var prefabPanel = new PrefabPanel(i);
                prefabPanel.Init(directory);
                PrefabPanels.Add(prefabPanel);
            }

            var files = Directory.GetFiles(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath), FileFormat.LSP.ToPattern(), SearchOption.TopDirectoryOnly);

            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var jn = JSON.Parse(RTFile.ReadFromFile(file));

                var prefab = Prefab.Parse(jn);
                prefab.beatmapObjects.ForEach(x => (x as BeatmapObject).RemovePrefabReference());
                prefab.filePath = RTFile.ReplaceSlash(file);

                var prefabPanel = new PrefabPanel(PrefabDialog.External, i);
                prefabPanel.Init(prefab);
                PrefabPanels.Add(prefabPanel);
            }

            prefabsLoading = false;

            yield break;
        }

        public IEnumerator UpdatePrefabs()
        {
            yield return inst.StartCoroutine(LoadPrefabs());
            StartCoroutine(RenderExternalPrefabs());
            EditorManager.inst.DisplayNotification("Updated external prefabs!", 2f, EditorManager.NotificationType.Success);
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
        /// Renders the External Prefab Editor.
        /// </summary>
        /// <param name="prefabPanel"></param>
        public void RenderPrefabExternalDialog(PrefabPanel prefabPanel)
        {
            var prefab = prefabPanel.Prefab;
            var prefabType = prefab.GetPrefabType();
            var isExternal = prefabPanel.Dialog == PrefabDialog.External;

            PrefabExternalEditor.NameField.onValueChanged.ClearAll();
            PrefabExternalEditor.NameField.onEndEdit.ClearAll();
            PrefabExternalEditor.NameField.text = prefab.name;
            PrefabExternalEditor.NameField.onValueChanged.AddListener(_val => prefab.name = _val);
            PrefabExternalEditor.NameField.onEndEdit.AddListener(_val =>
            {
                if (!isExternal)
                {
                    prefabPanel.RenderName();
                    prefabPanel.RenderTooltip();
                    EditorTimeline.inst.timelineObjects.ForLoop(timelineObject =>
                    {
                        if (timelineObject.isBeatmapObject || timelineObject.GetData<PrefabObject>().prefabID != prefab.id)
                            return;

                        timelineObject.RenderText(prefab.name);
                    });

                    return;
                }

                RTEditor.inst.DisablePrefabWatcher();

                RTFile.DeleteFile(prefab.filePath);

                var file = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath, $"{RTFile.FormatLegacyFileName(prefab.name)}{FileFormat.LSP.Dot()}");
                prefab.filePath = file;
                RTFile.WriteToFile(file, prefab.ToJSON().ToString());

                prefabPanel.RenderName();
                prefabPanel.RenderTooltip();

                RTEditor.inst.EnablePrefabWatcher();
            });

            PrefabExternalEditor.TypeButton.label.text = prefabType.name + " [ Click to Open Prefab Type Editor ]";
            PrefabExternalEditor.TypeButton.button.image.color = prefabType.color;
            PrefabExternalEditor.TypeButton.button.onClick.NewListener(() =>
            {
                OpenPrefabTypePopup(prefab.typeID, id =>
                {
                    prefab.type = PrefabType.LSIDToIndex.TryGetValue(id, out int prefabTypeIndexLS) ? prefabTypeIndexLS : PrefabType.VGIDToIndex.TryGetValue(id, out int prefabTypeIndexVG) ? prefabTypeIndexVG : 0;
                    prefab.typeID = id;

                    var prefabType = prefab.GetPrefabType();
                    PrefabExternalEditor.TypeButton.label.text = prefabType.name + " [ Click to Open Prefab Type Editor ]";
                    PrefabExternalEditor.TypeButton.button.image.color = prefabType.color;

                    if (isExternal && !string.IsNullOrEmpty(prefab.filePath))
                        RTFile.WriteToFile(prefab.filePath, prefab.ToJSON().ToString());

                    prefabPanel.RenderPrefabType(prefabType);
                    prefabPanel.RenderTooltip(prefab, prefabType);
                });
            });

            PrefabExternalEditor.DescriptionField.onValueChanged.ClearAll();
            PrefabExternalEditor.DescriptionField.onEndEdit.ClearAll();
            PrefabExternalEditor.DescriptionField.text = prefab.description;
            PrefabExternalEditor.DescriptionField.onValueChanged.AddListener(_val => prefab.description = _val);
            PrefabExternalEditor.DescriptionField.onEndEdit.AddListener(_val =>
            {
                if (!isExternal)
                {
                    prefabPanel.RenderTooltip();
                    return;
                }

                RTEditor.inst.DisablePrefabWatcher();

                if (!string.IsNullOrEmpty(prefab.filePath))
                    RTFile.WriteToFile(prefab.filePath, prefab.ToJSON().ToString());

                prefabPanel.RenderTooltip();

                RTEditor.inst.EnablePrefabWatcher();
            });

            PrefabExternalEditor.ImportPrefabButton.gameObject.SetActive(isExternal);
            PrefabExternalEditor.ImportPrefabButton.button.onClick.ClearAll();
            if (isExternal)
                PrefabExternalEditor.ImportPrefabButton.button.onClick.AddListener(() => ImportPrefabIntoLevel(prefab));

            PrefabExternalEditor.ConvertPrefabButton.gameObject.SetActive(isExternal);
            PrefabExternalEditor.ConvertPrefabButton.button.onClick.ClearAll();
            if (isExternal)
                PrefabExternalEditor.ConvertPrefabButton.button.onClick.AddListener(() => ConvertPrefab(prefab));
        }

        /// <summary>
        /// Creates a new prefab and saves it.
        /// </summary>
        public void CreateNewPrefab()
        {
            var selectedBeatmapObjects = EditorTimeline.inst.SelectedBeatmapObjects;
            var selectedBackgroundObjects = EditorTimeline.inst.SelectedBackgroundObjects;
            if (selectedBeatmapObjects.IsEmpty() && selectedBackgroundObjects.IsEmpty())
            {
                EditorManager.inst.DisplayNotification("Can't save prefab without any objects in it!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (string.IsNullOrEmpty(PrefabEditor.inst.NewPrefabName))
            {
                EditorManager.inst.DisplayNotification("Can't save prefab without a name!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var prefab = new Prefab(
                PrefabEditor.inst.NewPrefabName,
                PrefabEditor.inst.NewPrefabType,
                PrefabEditor.inst.NewPrefabOffset,
                selectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()).ToList(),
                EditorTimeline.inst.SelectedPrefabObjects.Select(x => x.GetData<PrefabObject>()).ToList(),
                null,
                selectedBackgroundObjects.Select(x => x.GetData<BackgroundObject>()).ToList());

            prefab.description = NewPrefabDescription;
            prefab.typeID = NewPrefabTypeID;

            foreach (var beatmapObject in prefab.beatmapObjects)
            {
                if (beatmapObject.shape == 6 && !string.IsNullOrEmpty(beatmapObject.text) && prefab.assets.sprites.TryFind(x => x.name == beatmapObject.text, out SpriteAsset spriteAsset))
                    GameData.Current.assets.sprites.Add(spriteAsset.Copy());
            }

            foreach (var backgroundObject in prefab.backgroundObjects)
            {
                if (backgroundObject.shape == 6 && !string.IsNullOrEmpty(backgroundObject.text) && prefab.assets.sprites.TryFind(x => x.name == backgroundObject.text, out SpriteAsset spriteAsset))
                    GameData.Current.assets.sprites.Add(spriteAsset.Copy());
            }

            if (createInternal)
            {
                EditorManager.inst.DisplayNotification($"Saving Internal Prefab [{prefab.name}] to level...", 1.5f, EditorManager.NotificationType.Warning);
                ImportPrefabIntoLevel(prefab);
                EditorManager.inst.DisplayNotification($"Saved Internal Prefab [{prefab.name}]!", 2f, EditorManager.NotificationType.Success);
            }
            else
                SavePrefab(prefab);

            PrefabCreator.Close();
            OpenPopup();
        }

        /// <summary>
        /// Saves a prefab to a file.
        /// </summary>
        /// <param name="prefab">Prefab to save.</param>
        public void SavePrefab(Prefab prefab)
        {
            RTEditor.inst.DisablePrefabWatcher();

            EditorManager.inst.DisplayNotification($"Saving External Prefab [{prefab.name}]...", 1.5f, EditorManager.NotificationType.Warning);

            prefab.beatmapObjects.ForEach(x => x.RemovePrefabReference());
            int count = PrefabPanels.Count;
            var file = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath, $"{RTFile.FormatLegacyFileName(prefab.name)}{FileFormat.LSP.Dot()}");
            prefab.filePath = file;

            var prefabPanel = new PrefabPanel(PrefabDialog.External, count);
            prefabPanel.Init(prefab);
            PrefabPanels.Add(prefabPanel);

            RTFile.WriteToFile(file, prefab.ToJSON().ToString());
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

            RTFile.DeleteFile(prefabPanel.FilePath);

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
                RTLevel.Current?.UpdatePrefab(x, false);

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

                RTLevel.Current?.UpdatePrefab(prefabObject, false);

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

            StartCoroutine(RefreshInternalPrefabs());
        }

        /// <summary>
        /// Opens the Internal and External Prefab popups.
        /// </summary>
        public void OpenPopup()
        {
            foreach (var editorPopup in RTEditor.inst.editorPopups)
            {
                if (editorPopup.Name == EditorPopup.PREFAB_POPUP)
                {
                    if (editorPopup.IsOpen)
                        continue;

                    editorPopup.Open();

                    continue;
                }

                editorPopup.Close();
            }

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

            selectQuickPrefabButton.onClick.ClearAll();
            selectQuickPrefabButton.onClick.AddListener(() =>
            {
                selectQuickPrefabText.text = "<color=#669e37>Selecting</color>";
                StartCoroutine(RefreshInternalPrefabs(true));
            });

            PrefabEditor.inst.externalSearch.onValueChanged.ClearAll();
            PrefabEditor.inst.externalSearch.onValueChanged.AddListener(_val =>
            {
                PrefabEditor.inst.externalSearchStr = _val;
                StartCoroutine(RenderExternalPrefabs());
            });

            PrefabEditor.inst.internalSearch.onValueChanged.ClearAll();
            PrefabEditor.inst.internalSearch.onValueChanged.AddListener(_val =>
            {
                PrefabEditor.inst.internalSearchStr = _val;
                StartCoroutine(RefreshInternalPrefabs());
            });

            savingToPrefab = false;
            prefabToSaveFrom = null;

            //Internal Config
            {
                var internalPrefab = PrefabEditor.inst.internalPrefabDialog;

                var internalPrefabGLG = internalPrefab.Find("mask/content").GetComponent<GridLayoutGroup>();

                internalPrefabGLG.spacing = EditorConfig.Instance.PrefabInternalSpacing.Value;
                internalPrefabGLG.cellSize = EditorConfig.Instance.PrefabInternalCellSize.Value;
                internalPrefabGLG.constraint = EditorConfig.Instance.PrefabInternalConstraintMode.Value;
                internalPrefabGLG.constraintCount = EditorConfig.Instance.PrefabInternalConstraint.Value;
                internalPrefabGLG.startAxis = EditorConfig.Instance.PrefabInternalStartAxis.Value;

                internalPrefab.AsRT().anchoredPosition = EditorConfig.Instance.PrefabInternalPopupPos.Value;
                internalPrefab.AsRT().sizeDelta = EditorConfig.Instance.PrefabInternalPopupSize.Value;

                internalPrefab.GetComponent<ScrollRect>().horizontal = EditorConfig.Instance.PrefabInternalHorizontalScroll.Value;
            }

            //External Config
            {
                var externalPrefab = PrefabEditor.inst.externalPrefabDialog;

                var externalPrefabGLG = externalPrefab.Find("mask/content").GetComponent<GridLayoutGroup>();

                externalPrefabGLG.spacing = EditorConfig.Instance.PrefabExternalSpacing.Value;
                externalPrefabGLG.cellSize = EditorConfig.Instance.PrefabExternalCellSize.Value;
                externalPrefabGLG.constraint = EditorConfig.Instance.PrefabExternalConstraintMode.Value;
                externalPrefabGLG.constraintCount = EditorConfig.Instance.PrefabExternalConstraint.Value;
                externalPrefabGLG.startAxis = EditorConfig.Instance.PrefabExternalStartAxis.Value;

                externalPrefab.AsRT().anchoredPosition = EditorConfig.Instance.PrefabExternalPopupPos.Value;
                externalPrefab.AsRT().sizeDelta = EditorConfig.Instance.PrefabExternalPopupSize.Value;

                externalPrefab.GetComponent<ScrollRect>().horizontal = EditorConfig.Instance.PrefabExternalHorizontalScroll.Value;
            }

            StartCoroutine(RenderExternalPrefabs());
            StartCoroutine(RefreshInternalPrefabs());
        }

        /// <summary>
        /// Refreshes the Prefab Creator selection list.
        /// </summary>
        public void ReloadSelectionContent()
        {
            LSHelpers.DeleteChildren(PrefabEditor.inst.gridContent);
            foreach (var timelineObject in EditorTimeline.inst.timelineObjects)
            {
                if (timelineObject.isPrefabObject)
                    continue;

                if (!RTString.SearchString(PrefabEditor.inst.gridSearch.text, timelineObject.Name))
                    continue;

                var selection = PrefabEditor.inst.selectionPrefab.Duplicate(PrefabEditor.inst.gridContent, "grid");
                var text = selection.transform.Find("text").GetComponent<Text>();
                text.text = timelineObject.Name;

                var selectionToggle = selection.GetComponent<Toggle>();

                selectionToggle.onValueChanged.ClearAll();
                selectionToggle.isOn = timelineObject.Selected;
                selectionToggle.onValueChanged.AddListener(_val => timelineObject.Selected = _val);
                EditorThemeManager.ApplyToggle(selectionToggle, text: text);
            }
        }

        /// <summary>
        /// Opens the Prefab Creator dialog.
        /// </summary>
        public void OpenDialog()
        {
            PrefabCreator.Open();
            RenderPrefabCreator();
        }

        /// <summary>
        /// Renders the Prefab Creator dialog.
        /// </summary>
        public void RenderPrefabCreator()
        {
            PrefabCreator.NameField.onValueChanged.NewListener(_val => PrefabEditor.inst.NewPrefabName = _val);
            RenderPrefabCreatorOffsetSlider();
            RenderPrefabCreatorOffsetField();

            var offsetInputContextMenu = PrefabCreator.OffsetField.gameObject.GetOrAddComponent<ContextClickable>();
            offsetInputContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Set to Timeline Cursor", () =>
                    {
                        PrefabEditor.inst.NewPrefabOffset -= (AudioManager.inst.CurrentAudioSource.time - EditorTimeline.inst.SelectedObjects.Min(x => x.Time) + PrefabEditor.inst.NewPrefabOffset);
                        RenderPrefabCreator();
                    }),
                    new ButtonFunction("Snap to BPM", () =>
                    {
                        var firstTime = EditorTimeline.inst.SelectedObjects.Min(x => x.Time);
                        PrefabEditor.inst.NewPrefabOffset = firstTime - RTEditor.SnapToBPM(firstTime - PrefabEditor.inst.NewPrefabOffset);
                        RenderPrefabCreator();
                    })
                    );
            };
            var offsetSliderContextMenu = PrefabCreator.OffsetField.gameObject.GetOrAddComponent<ContextClickable>();
            offsetSliderContextMenu.onClick = offsetInputContextMenu.onClick;

            TriggerHelper.AddEventTriggers(PrefabCreator.OffsetField.gameObject, TriggerHelper.ScrollDelta(PrefabCreator.OffsetField));

            RenderPrefabCreatorTypeSelector(NewPrefabTypeID);
            PrefabCreator.TypeButton.button.onClick.NewListener(() =>
            {
                OpenPrefabTypePopup(NewPrefabTypeID, id =>
                {
                    PrefabEditor.inst.NewPrefabType = PrefabType.LSIDToIndex.TryGetValue(id, out int prefabTypeIndexLS) ? prefabTypeIndexLS : PrefabType.VGIDToIndex.TryGetValue(id, out int prefabTypeIndexVG) ? prefabTypeIndexVG : 0;
                    NewPrefabTypeID = id;
                    RenderPrefabCreatorTypeSelector(id);
                });
            });
            PrefabCreator.DescriptionField.onValueChanged.NewListener(_val => NewPrefabDescription = _val);

            ReloadSelectionContent();
        }

        public void RenderPrefabCreatorOffsetSlider()
        {
            PrefabCreator.OffsetSlider.onValueChanged.ClearAll();
            PrefabCreator.OffsetSlider.value = PrefabEditor.inst.NewPrefabOffset;
            PrefabCreator.OffsetSlider.onValueChanged.AddListener(_val =>
            {
                PrefabEditor.inst.NewPrefabOffset = Mathf.Round(_val * 100f) / 100f;
                RenderPrefabCreatorOffsetField();
            });
        }
        
        public void RenderPrefabCreatorOffsetField()
        {
            PrefabCreator.OffsetField.onValueChanged.ClearAll();
            PrefabCreator.OffsetField.text = PrefabEditor.inst.NewPrefabOffset.ToString();
            PrefabCreator.OffsetField.onValueChanged.AddListener(_val =>
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
                PrefabCreator.TypeButton.label.text = prefabType.name + " [ Click to Open Prefab Type Editor ]";
                PrefabCreator.TypeButton.button.image.color = prefabType.color;
            }
        }

        /// <summary>
        /// Updates the currently selected quick prefab.
        /// </summary>
        /// <param name="prefab">Prefab to set. Can be null to clear the selection.</param>
        public void UpdateCurrentPrefab(Prefab prefab)
        {
            currentQuickPrefab = prefab;

            bool prefabExists = currentQuickPrefab != null;

            selectQuickPrefabText.text = (!prefabExists ? "-Select Prefab-" : "<color=#669e37>-Prefab-</color>") + "\n" + (!prefabExists ? "n/a" : currentQuickPrefab.name);
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

            RTEditor.inst.UpdatePrefabPath(true);
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

            // Here we add the Example prefab provided to you.
            if (!GameData.Current.prefabs.Exists(x => x.id == LegacyPlugin.ExamplePrefab.id) && config.PrefabExampleTemplate.Value)
                GameData.Current.prefabs.Add(LegacyPlugin.ExamplePrefab.Copy(false));

            yield return CoroutineHelper.Seconds(0.03f);

            var searchFieldContextMenu = RTEditor.inst.PrefabPopups.InternalPrefabs.SearchField.gameObject.GetOrAddComponent<ContextClickable>();
            searchFieldContextMenu.onClick = null;
            searchFieldContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction($"Filter: Used [{(filterUsed ? "On": "Off")}]", () =>
                    {
                        filterUsed = !filterUsed;
                        StartCoroutine(RefreshInternalPrefabs());
                    })
                    );
            };

            RTEditor.inst.PrefabPopups.InternalPrefabs.ClearContent();
            CreatePrefabButton(RTEditor.inst.PrefabPopups.InternalPrefabs.Content, "New Internal Prefab", eventData =>
            {
                if (eventData.button == PointerEventData.InputButton.Right)
                {
                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Create prefab", () =>
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
                if (ContainsName(prefab, PrefabDialog.Internal) && (!filterUsed || GameData.Current.prefabObjects.Any(x => x.prefabID == prefab.id)))
                    new PrefabPanel(PrefabDialog.Internal, i).Init(prefab, updateCurrentPrefab);
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
            var gameObject = PrefabEditor.inst.CreatePrefab.Duplicate(parent, "add new prefab");
            var text = gameObject.GetComponentInChildren<Text>();
            text.text = name;

            var hoverSize = EditorConfig.Instance.PrefabButtonHoverSize.Value;

            var hover = gameObject.AddComponent<HoverUI>();
            hover.animateSca = true;
            hover.animatePos = false;
            hover.size = hoverSize;

            var createNewButton = gameObject.GetComponent<Button>();
            createNewButton.onClick.ClearAll();

            var contextClickable = gameObject.AddComponent<ContextClickable>();
            contextClickable.onClick = action;

            EditorThemeManager.ApplyGraphic(createNewButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(text, ThemeGroup.Add_Text);

            return gameObject;
        }

        /// <summary>
        /// Renders the External Prefabs UI.
        /// </summary>
        public IEnumerator RenderExternalPrefabs()
        {
            foreach (var prefabPanel in PrefabPanels.Where(x => x.Dialog == PrefabDialog.External))
            {
                prefabPanel.SetActive(
                    prefabPanel.isFolder ?
                        RTString.SearchString(PrefabEditor.inst.externalSearchStr, Path.GetFileName(prefabPanel.FilePath)) :
                        ContainsName(prefabPanel.Prefab, PrefabDialog.External));
            }

            yield break;
        }

        /// <summary>
        /// Checks if the prefab is being searched for.
        /// </summary>
        /// <param name="prefab">Prefab reference.</param>
        /// <param name="dialog">Prefabs' dialog.</param>
        /// <returns>Returns true if the prefab is being searched for, otherwise returns false.</returns>
        public bool ContainsName(Prefab prefab, PrefabDialog dialog) => RTString.SearchString(dialog == PrefabDialog.External ? PrefabEditor.inst.externalSearchStr : PrefabEditor.inst.internalSearchStr, prefab.name, prefab.GetPrefabType().name);

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

        #endregion
    }
}
