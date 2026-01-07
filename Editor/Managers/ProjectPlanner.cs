using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using TMPro;
using SimpleJSON;
using Crosstales.FB;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Planners;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Built-in planner used for improving creation workflow. Includes documents, todo lists, timelines, etc.
    /// </summary>
    public class ProjectPlanner : BaseManager<ProjectPlanner, EditorManagerSettings>
    {
        #region Values

        public Transform plannerBase;
        public Transform planner;
        public Transform topBarBase;
        public Transform contentBase;
        public Transform contentScroll;
        public Transform content;
        public Transform notesParent;
        public GridLayoutGroup contentLayout;

        public RectTransform notificationsParent;

        public Transform contextMenuParent;
        public Transform popupsParent;

        public Transform assetsParent;

        public GameObject documentFullView;
        public TMP_InputField documentInputField;
        public OpenHyperlinks documentHyperlinks;
        public TextMeshProUGUI documentTitle;
        public Toggle documentInteractibleToggle;

        public GameObject characterFullView;
        public Image characterSprite;
        public TextMeshProUGUI characterDetails;
        public OpenHyperlinks characterDetailsHyperlinks;
        public OpenHyperlinks characterDescriptionHyperlinks;
        public TMP_InputField characterDescriptionInputField;
        public Transform characterAttributesContent;

        public AudioSource OSTAudioSource { get; set; }
        public int currentOST;
        public string currentOSTID;
        public bool playing = false;
        public List<OSTPlanner> recentOST = new List<OSTPlanner>();
        public bool forceShuffleOST;
        public bool pausedOST;

        public List<Toggle> tabs = new List<Toggle>();

        public PlannerBase.Type CurrentTab { get; set; }
        public string SearchTerm { get; set; }

        public string[] tabNames = new string[]
        {
            "Documents",
            "TO DO",
            "Characters",
            "Timelines",
            "Schedules",
            "Notes",
            "OST",
        };

        public Vector2[] tabCellSizes = new Vector2[]
        {
            new Vector2(232f, 400f),
            new Vector2(1280f, 64f),
            new Vector2(630f, 400f),
            new Vector2(1280f, 250f),
            new Vector2(1280f, 64f),
            new Vector2(410f, 200f),
            new Vector2(1280f, 64f),
        };

        public int[] tabConstraintCounts = new int[]
        {
            5,
            1,
            2,
            1,
            1,
            3,
            1,
        };

        public GameObject tagPrefab;

        public GameObject tabPrefab;

        public GameObject baseCardPrefab;

        public GameObject tmpTextPrefab;

        public List<GameObject> prefabs = new List<GameObject>();

        public Sprite gradientSprite;

        public List<DocumentPlanner> documents = new List<DocumentPlanner>();
        public List<TODOPlanner> todos = new List<TODOPlanner>();
        public List<CharacterPlanner> characters = new List<CharacterPlanner>();
        public List<TimelinePlanner> timelines = new List<TimelinePlanner>();
        public List<SchedulePlanner> schedules = new List<SchedulePlanner>();
        public List<NotePlanner> notes = new List<NotePlanner>();
        public List<OSTPlanner> osts = new List<OSTPlanner>();

        public List<PlannerBase> copiedPlanners = new List<PlannerBase>();

        public GameObject timelineButtonPrefab;

        public GameObject timelineAddPrefab;

        public Texture2D horizontalDrag;
        public Texture2D verticalDrag;

        public enum InterruptOSTBehaviorType
        {
            Continue,
            LowerVolume,
            Pause,
        }

        public enum LoopOSTBehaviorType
        {
            None,
            LoopSingle,
            LoopAll,
        }

        #region Editor

        public GameObject textEditorPrefab;

        public Image editorTitlePanel;

        public InputField documentEditorName;
        public InputField documentEditorText;

        public InputField todoEditorText;
        public Button todoEditorMoveUpButton;
        public Button todoEditorMoveDownButton;

        public InputField characterEditorName;
        public InputField characterEditorGender;
        public InputField characterEditorOrigin;
        public InputField characterEditorDescription;
        public Transform characterEditorTraitsContent;
        public Transform characterEditorLoreContent;
        public Transform characterEditorAbilitiesContent;

        public InputField timelineEditorName;
        public InputField eventEditorName;
        public InputField eventEditorDescription;
        public InputField eventEditorPath;
        public Dropdown eventEditorType;

        public InputField scheduleEditorDescription;
        public InputField scheduleEditorYear;
        public Dropdown scheduleEditorMonth;
        public InputField scheduleEditorDay;
        public InputField scheduleEditorHour;
        public InputField scheduleEditorMinute;

        public InputField noteEditorName;
        public InputField noteEditorText;
        public Transform noteEditorColorsParent;
        public Transform colorBase;
        public List<Toggle> noteEditorColors = new List<Toggle>();
        public Button noteEditorReset;

        public InputField ostEditorPath;
        public InputField ostEditorName;
        public Button ostEditorPlay;
        public Button ostEditorUseGlobal;
        public Text ostEditorUseGlobalText;
        public Button ostEditorStop;
        public Button ostEditorShuffle;
        public InputField ostEditorIndex;

        public List<GameObject> editors = new List<GameObject>();

        List<PlannerBase> activeTabPlannerItems = new List<PlannerBase>();

        DocumentPlanner currentDocumentPlanner;
        TODOPlanner currentTODOPlanner;
        CharacterPlanner currentCharacterPlanner;
        TimelinePlanner currentTimelinePlanner;
        SchedulePlanner currentSchedulePlanner;
        NotePlanner currentNotePlanner;
        OSTPlanner currentOSTPlanner;

        #endregion

        #endregion

        #region Functions
        
        #region Init

        public override void OnInit()
        {
            plannerBase = GameObject.Find("Editor Systems/Editor GUI/sizer").transform.GetChild(1);
            plannerBase.gameObject.SetActive(true);

            planner = plannerBase.GetChild(0);
            topBarBase = planner.GetChild(0);

            EditorThemeManager.ApplyGraphic(planner.GetComponent<Image>(), ThemeGroup.Background_1);
            EditorThemeManager.ApplyGraphic(topBarBase.GetComponent<Image>(), ThemeGroup.Background_1);

            var assets = Creator.NewGameObject("Planner Assets", transform);
            assetsParent = assets.transform;

            tabPrefab = topBarBase.GetChild(0).gameObject;
            tabPrefab.transform.SetParent(assetsParent);
            tabPrefab.transform.AsRT().sizeDelta = new Vector2(200f, 54f);

            LSHelpers.DeleteChildren(topBarBase);

            Destroy(topBarBase.GetComponent<ToggleGroup>());
            tabPrefab.GetComponent<Toggle>().group = null;

            for (int i = 0; i < tabNames.Length; i++)
            {
                var name = tabNames[i];
                var tab = tabPrefab.Duplicate(topBarBase, name);
                tab.transform.localScale = Vector3.one;

                var background = tab.transform.Find("Background");
                var text = background.Find("Text").GetComponent<Text>();
                var image = background.GetComponent<Image>();

                text.fontSize = 26;
                text.fontStyle = FontStyle.Bold;
                text.text = name;
                tab.AddComponent<ContrastColors>().Init(text, image);
                var toggle = tab.GetComponent<Toggle>();
                tabs.Add(tab.GetComponent<Toggle>());

                EditorThemeManager.ApplyGraphic(image, EditorThemeManager.GetTabThemeGroup(tabs.Count - 1), true);
                EditorThemeManager.ApplyGraphic(toggle.graphic, ThemeGroup.Background_1);
            }

            var spacer = Creator.NewUIObject("topbar spacer", topBarBase);
            spacer.transform.AsRT().sizeDelta = new Vector2(195f, 32f);

            var close = EditorPrefabHolder.Instance.CloseButton.Duplicate(topBarBase, "close");
            close.transform.localScale = Vector3.one;

            close.transform.AsRT().sizeDelta = new Vector2(48f, 48f);

            var closeButton = close.GetComponent<Button>();
            closeButton.onClick.NewListener(Close);

            EditorThemeManager.ApplySelectable(closeButton, ThemeGroup.Close);

            var closeX = close.transform.GetChild(0).gameObject;
            EditorThemeManager.ApplyGraphic(close.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

            EditorHelper.AddEditorDropdown("Open Project Planner", string.Empty, "Edit", EditorSprites.DocumentSprite, () =>
            {
                Open();
                EditorManager.inst.HideAllDropdowns();
            });

            contentBase = planner.Find("content/recent");
            Destroy(contentBase.GetComponent<VerticalLayoutGroup>());
            contentBase.gameObject.name = "content base";

            contentScroll = contentBase.Find("recent scroll");
            contentScroll.gameObject.name = "content scroll";
            contentScroll.AsRT().anchoredPosition = new Vector2(690f, -572f);
            contentScroll.AsRT().sizeDelta = new Vector2(1384f, 892f);

            contentScroll.GetComponent<ScrollRect>().horizontal = false;

            contentScroll.Find("Viewport").GetComponent<Mask>().showMaskGraphic = false;
            content = contentScroll.Find("Viewport/Content");
            contentLayout = content.GetComponent<GridLayoutGroup>();

            baseCardPrefab = content.GetChild(0).gameObject;
            baseCardPrefab.transform.SetParent(assetsParent);
            baseCardPrefab.SetActive(true);
            var baseCardPrefabButton = baseCardPrefab.GetComponent<Button>();
            var normalColor = new Color(0.1294f, 0.1294f, 0.1294f, 1f);
            var lightColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            baseCardPrefabButton.colors = UIManager.SetColorBlock(baseCardPrefabButton.colors, normalColor, lightColor, lightColor, normalColor, LSColors.red700);

            tmpTextPrefab = baseCardPrefab.transform.Find("artist").gameObject;

            var scrollBarVertical = contentScroll.Find("Scrollbar Vertical");
            scrollBarVertical.GetComponent<Image>().color = new Color(0.11f, 0.11f, 0.11f, 1f);
            var handleImage = scrollBarVertical.Find("Sliding Area/Handle").GetComponent<Image>();
            handleImage.color = new Color(0.878f, 0.878f, 0.878f, 1f);
            handleImage.sprite = null;

            EditorThemeManager.ApplyScrollbar(scrollBarVertical.GetComponent<Scrollbar>(), scrollBarVertical.GetComponent<Image>(), scrollbarRoundedSide: SpriteHelper.RoundedSide.Bottom_Right_I);

            contentBase.Find("Image").AsRT().anchoredPosition = new Vector2(690f, /*-94f*/ -104f);
            contentBase.Find("Image").AsRT().sizeDelta = new Vector2(1384f, 48f);
            EditorThemeManager.ApplyGraphic(contentBase.Find("Image").GetComponent<Image>(), ThemeGroup.Background_1);

            // List handlers
            {
                var searchBase = EditorLevelManager.inst.OpenLevelPopup.SearchField.transform.parent.gameObject.Duplicate(contentBase.Find("Image"), "search base");
                searchBase.transform.localScale = Vector3.one;
                searchBase.transform.AsRT().anchoredPosition = Vector2.zero;
                searchBase.transform.AsRT().sizeDelta = new Vector2(0f, 48f);
                searchBase.transform.GetChild(0).AsRT().sizeDelta = new Vector2(0f, 48f);

                var searchField = searchBase.transform.GetChild(0).GetComponent<InputField>();
                searchField.GetPlaceholderText().text = "Search...";
                searchField.onValueChanged.NewListener(_val =>
                {
                    CoreHelper.Log($"Searching {_val}");
                    SearchTerm = _val;
                    RefreshList();
                });

                EditorThemeManager.ApplyInputField(searchField, ThemeGroup.Search_Field_1, 1, SpriteHelper.RoundedSide.Bottom);

                var addNewItem = EditorPrefabHolder.Instance.Function2Button.Duplicate(contentBase, "new", 1);
                addNewItem.transform.AsRT().anchoredPosition = new Vector2(120f, 970f);
                addNewItem.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                var addNewItemStorage = addNewItem.GetComponent<FunctionButtonStorage>();
                addNewItemStorage.Text = "Add New Item";
                addNewItemStorage.OnClick.NewListener(() =>
                {
                    CoreHelper.Log($"Create new {tabNames[(int)CurrentTab]}");
                    var path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PlannersPath);
                    switch (CurrentTab)
                    {
                        case PlannerBase.Type.Document: {
                                CreateDocument(
                                    name: $"New Story {documents.Count + 1}",
                                    text: "<align=center>Plan out your story!");
                                break;
                            }
                        case PlannerBase.Type.TODO: {
                                CreateTODO(
                                    check: false,
                                    text: "Do this.");
                                break;
                            }
                        case PlannerBase.Type.Character: {
                                CreateCharacter(
                                    name: "New Character");
                                break;
                            }
                        case PlannerBase.Type.Timeline: {
                                CreateTimeline(
                                    name: "Classic Arrhythmia",
                                    events: new List<TimelinePlanner.Event>
                                    {
                                        new TimelinePlanner.Event
                                        {
                                            Name = "Beginning",
                                            Description = $"Introduces players / viewers to Hal.)",
                                            EventType = TimelinePlanner.Event.Type.Cutscene,
                                            Path = string.Empty
                                        },
                                        new TimelinePlanner.Event
                                        {
                                            Name = "Tokyo Skies",
                                            Description = $"Players learn very basic stuff about Classic Arrhythmia / Project Arrhythmia mechanics.{Environment.NewLine}{Environment.NewLine}(Click on this button to open the level.)",
                                            EventType = TimelinePlanner.Event.Type.Level,
                                            Path = string.Empty
                                        },
                                    });
                                break;
                            }
                        case PlannerBase.Type.Schedule: {
                                CreateSchedule(
                                    dateTime: DateTime.Now.AddDays(1),
                                    description: "Tomorrow!");
                                break;
                            }
                        case PlannerBase.Type.Note: {
                                CreateNote(
                                    active: true,
                                    name: "New Note",
                                    color: UnityRandom.Range(0, MarkerEditor.inst.markerColors.Count),
                                    position: new Vector2(Screen.width / 2, Screen.height / 2),
                                    text: "This note appears in the editor and can be dragged to anywhere.");
                                break;
                            }
                        case PlannerBase.Type.OST: {
                                CreateOST(
                                    name: "Kaixo - Fragments",
                                    path: "Set this path to wherever you have a song located.",
                                    useGlobal: false);
                                break;
                            }
                        default: {
                                CoreHelper.LogWarning($"How did you do that...?");
                                break;
                            }
                    }
                    RenderTabs();
                    RefreshList();
                });

                EditorThemeManager.ApplySelectable(addNewItemStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.ApplyGraphic(addNewItemStorage.label, ThemeGroup.Function_2_Text);

                var reload = EditorPrefabHolder.Instance.Function2Button.Duplicate(contentBase, "reload", 2);
                reload.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                reload.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                var reloadStorage = reload.GetComponent<FunctionButtonStorage>();
                reloadStorage.Text = "Reload";
                reloadStorage.OnClick.NewListener(Load);

                EditorThemeManager.ApplySelectable(reloadStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.ApplyGraphic(reloadStorage.label, ThemeGroup.Function_2_Text);

                var paste = EditorPrefabHolder.Instance.Function1Button.Duplicate(contentBase, "paste", 3);
                RectValues.Default.AnchoredPosition(-340f, 462f).SizeDelta(200f, 32f).AssignToRectTransform(paste.transform.AsRT());
                var pasteStorage = paste.GetComponent<FunctionButtonStorage>();
                pasteStorage.Text = "Paste";
                pasteStorage.OnClick.NewListener(PastePlanners);

                EditorThemeManager.ApplyGraphic(pasteStorage.button.image, ThemeGroup.Paste);
                EditorThemeManager.ApplyGraphic(pasteStorage.label, ThemeGroup.Paste_Text);

                var path = EditorPrefabHolder.Instance.StringInputField.Duplicate(contentBase, "path", 4);
                new RectValues(new Vector2(1750f, 970f), Vector2.zero, Vector2.zero, RectValues.CenterPivot, new Vector2(300f, 32f)).AssignToRectTransform(path.transform.AsRT());
                var pathField = path.GetComponent<InputField>();

                pathField.SetTextWithoutNotify(RTEditor.inst.PlannersPath);
                pathField.onValueChanged.ClearAll();
                pathField.onEndEdit.NewListener(_val =>
                {
                    RTEditor.inst.PlannersPath = _val;
                    Load();
                });
                pathField.textComponent.alignment = TextAnchor.MiddleLeft;
                pathField.GetPlaceholderText().alignment = TextAnchor.MiddleLeft;
                pathField.GetPlaceholderText().text = "Set a folder...";

                EditorThemeManager.ApplyInputField(pathField);

                EditorContextMenu.AddContextMenu(path, leftClick: null,
                    new ButtonElement("Set Folder", () =>
                    {
                        RTFileBrowser.inst.Popup.Open();
                        RTFileBrowser.inst.UpdateBrowserFolder(_val =>
                        {
                            if (!RTFile.ReplaceSlash(_val).Contains(RTFile.ApplicationDirectory + "beatmaps/"))
                            {
                                EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            var str = RTFile.ReplaceSlash(_val).Remove(RTFile.ApplicationDirectory + "beatmaps/");
                            pathField.SetTextWithoutNotify(str);
                            RTEditor.inst.PlannersPath = str;
                            Load();
                            EditorManager.inst.DisplayNotification($"Set Planner path to {RTEditor.inst.PlannersPath}!", 2f, EditorManager.NotificationType.Success);
                            RTFileBrowser.inst.Popup.Close();
                        });
                    }),
                    new ButtonElement("Open in File Explorer", () => RTFile.OpenInFileBrowser.Open(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PlannersPath))));
            }

            gradientSprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/linear_gradient{FileFormat.PNG.Dot()}"));

            // Item Prefabs
            {
                textEditorPrefab = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(assetsParent, "text editor");
                CoreHelper.Delete(textEditorPrefab.transform.Find("edit"));

                // Document
                {
                    var prefab = baseCardPrefab.Duplicate(assetsParent, "document prefab");
                    var albumArt = prefab.transform.GetChild(0);
                    var title = prefab.transform.GetChild(1);
                    var artist = prefab.transform.GetChild(2);

                    albumArt.SetSiblingIndex(2);

                    prefab.GetComponent<Image>().sprite = null;
                    prefab.AddComponent<Mask>().showMaskGraphic = true;

                    albumArt.name = "gradient";
                    title.name = "name";
                    artist.name = "words";

                    title.AsRT().anchoredPosition = new Vector2(0f, 150f);
                    title.AsRT().sizeDelta = new Vector2(-32f, 80f);

                    var albumArtImage = albumArt.GetComponent<Image>();
                    albumArtImage.sprite = gradientSprite;
                    albumArtImage.color = new Color(0.1294f, 0.1294f, 0.1294f, 1f);

                    RectValues.FullAnchored.AssignToRectTransform(albumArt.transform.AsRT());

                    artist.AsRT().anchoredPosition = new Vector2(0f, -60f);
                    artist.AsRT().sizeDelta = new Vector2(-32f, 260f);
                    var tmp = artist.GetComponent<TextMeshProUGUI>();
                    tmp.alignment = TextAlignmentOptions.TopLeft;
                    tmp.enableWordWrapping = true;
                    tmp.text = "This is your story.";

                    var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(prefab.transform, "delete");
                    RectValues.Default.AnchoredPosition(96f, 180f).SizeDelta(26f, 26f).AssignToRectTransform(delete.transform.AsRT());

                    prefabs.Add(prefab);
                }

                // TODO
                {
                    var prefab = baseCardPrefab.Duplicate(assetsParent, "todo prefab");
                    var albumArt = prefab.transform.GetChild(0);
                    var title = prefab.transform.GetChild(1);
                    var artist = prefab.transform.GetChild(2);

                    Destroy(albumArt.gameObject);
                    Destroy(artist.gameObject);

                    prefab.GetComponent<Image>().sprite = null;

                    title.name = "text";

                    title.AsRT().anchoredPosition = Vector2.zero;
                    title.AsRT().sizeDelta = new Vector2(-120f, 64f);

                    var toggle = EditorPrefabHolder.Instance.Toggle.Duplicate(prefab.transform, "checked");
                    toggle.transform.AsRT().anchoredPosition = new Vector2(32f, -32f);
                    toggle.transform.AsRT().sizeDelta = Vector2.zero;
                    toggle.transform.GetChild(0).AsRT().pivot = new Vector2(0.5f, 0.5f);
                    toggle.transform.GetChild(0).AsRT().sizeDelta = new Vector2(38f, 38f);
                    toggle.transform.GetChild(0).GetChild(0).AsRT().sizeDelta = new Vector2(36f, 36f);

                    var tmp = title.GetComponent<TextMeshProUGUI>();
                    tmp.alignment = TextAlignmentOptions.Left;
                    tmp.enableWordWrapping = false;
                    tmp.text = "Do this.";

                    var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(prefab.transform, "delete");
                    RectValues.Default.AnchoredPosition(605f, 0f).SizeDelta(38f, 38f).AssignToRectTransform(delete.transform.AsRT());

                    prefabs.Add(prefab);
                }

                // Character
                {
                    var prefab = baseCardPrefab.Duplicate(assetsParent, "character prefab");
                    var albumArt = prefab.transform.GetChild(0);
                    var title = prefab.transform.GetChild(1);
                    var artist = prefab.transform.GetChild(2);

                    prefab.GetComponent<Image>().sprite = null;

                    albumArt.name = "profile";
                    title.name = "details";
                    artist.name = "description";

                    albumArt.AsRT().anchoredPosition = new Vector2(-160f, 40f);
                    albumArt.AsRT().sizeDelta = new Vector2(256f, 256f);

                    title.AsRT().anchoredPosition = new Vector2(0f, -150f);
                    title.AsRT().sizeDelta = new Vector2(-32f, 60f);

                    RectValues.Default.AnchoredPosition(130f, 0f).SizeDelta(300f, 200f).AssignToRectTransform(artist.AsRT());

                    var tmpTitle = title.GetComponent<TextMeshProUGUI>();
                    tmpTitle.lineSpacing = -20;
                    tmpTitle.fontSize = 20;
                    tmpTitle.fontStyle = FontStyles.Normal;
                    tmpTitle.alignment = TextAlignmentOptions.TopLeft;
                    tmpTitle.enableWordWrapping = true;
                    tmpTitle.text = CharacterPlanner.DefaultCharacterDescription;

                    var tmpArtist = artist.GetComponent<TextMeshProUGUI>();
                    tmpArtist.alignment = TextAlignmentOptions.Top;
                    tmpArtist.fontSize = 18;
                    tmpArtist.enableWordWrapping = true;
                    tmpArtist.text = "Description";

                    var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(prefab.transform, "delete");
                    UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(-16f, -16f), Vector2.one, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(24f, 24f));

                    prefabs.Add(prefab);
                }

                // Timeline
                {
                    var prefab = Creator.NewUIObject("timeline prefab", assetsParent);

                    UIManager.SetRectTransform(prefab.transform.AsRT(), Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero);
                    var prefabImage = prefab.AddComponent<Image>();
                    prefabImage.color = new Color(0f, 0f, 0f, 0.2f);

                    var prefabScroll = Creator.NewUIObject("Scroll", prefab.transform);
                    UIManager.SetRectTransform(prefabScroll.transform.AsRT(), new Vector2(640f, -125f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(1260f, 200f));
                    var prefabScrollRect = prefabScroll.AddComponent<ScrollRect>();

                    var prefabViewport = Creator.NewUIObject("Viewport", prefabScroll.transform);
                    UIManager.SetRectTransform(prefabViewport.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0f));
                    var prefabViewportImage = prefabViewport.AddComponent<Image>();
                    var prefabViewportMask = prefabViewport.AddComponent<Mask>();
                    prefabViewportMask.showMaskGraphic = false;

                    var prefabContent = Creator.NewUIObject("Content", prefabViewport.transform);
                    UIManager.SetRectTransform(prefabContent.transform.AsRT(), Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(100f, 100f));
                    var prefabContentGLG = prefabContent.AddComponent<GridLayoutGroup>();
                    prefabContentGLG.cellSize = new Vector2(422f, 200f);
                    prefabContentGLG.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                    prefabContentGLG.constraintCount = 1;
                    prefabContentGLG.spacing = new Vector2(8f, 0f);
                    var prefabContentCSF = prefabContent.AddComponent<ContentSizeFitter>();
                    prefabContentCSF.horizontalFit = ContentSizeFitter.FitMode.MinSize;

                    prefabScrollRect.content = prefabContent.transform.AsRT();
                    prefabScrollRect.viewport = prefabViewport.transform.AsRT();
                    prefabScrollRect.vertical = false;

                    var scrollBar = EditorTimeline.inst.wholeTimeline.Find("Scrollbar").gameObject.Duplicate(prefab.transform, "Scrollbar");
                    scrollBar.transform.AsRT().anchoredPosition = Vector2.zero;
                    scrollBar.transform.AsRT().pivot = new Vector2(0.5f, 0f);
                    scrollBar.transform.AsRT().sizeDelta = new Vector2(0f, 25f);

                    prefabScrollRect.horizontalScrollbar = scrollBar.GetComponent<Scrollbar>();

                    var editPrefab = EditorPrefabHolder.Instance.CloseButton.Duplicate(prefab.transform, "edit");
                    UIManager.SetRectTransform(editPrefab.transform.AsRT(), new Vector2(-38f, -12f), Vector2.one, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(20f, 20f));
                    editPrefab.transform.GetChild(0).AsRT().sizeDelta = new Vector2(0f, 0f);
                    var editPrefabButton = editPrefab.GetComponent<Button>();
                    editPrefabButton.colors = UIManager.SetColorBlock(editPrefabButton.colors, new Color(0.9f, 0.9f, 0.9f, 1f), Color.white, Color.white, new Color(0.9f, 0.9f, 0.9f, 1f), LSColors.red700);
                    var spritePrefabImage = editPrefab.transform.GetChild(0).GetComponent<Image>();
                    spritePrefabImage.color = new Color(0.037f, 0.037f, 0.037f, 1f);
                    spritePrefabImage.sprite = EditorSprites.EditSprite;

                    var deletePrefab = EditorPrefabHolder.Instance.DeleteButton.Duplicate(prefab.transform, "delete");
                    UIManager.SetRectTransform(deletePrefab.transform.AsRT(), new Vector2(-12f, -12f), Vector2.one, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(20f, 20f));

                    timelineButtonPrefab = baseCardPrefab.Duplicate(assetsParent, "timeline button prefab");
                    var albumArt = timelineButtonPrefab.transform.GetChild(0);
                    var title = timelineButtonPrefab.transform.GetChild(1);
                    var artist = timelineButtonPrefab.transform.GetChild(2);

                    DestroyImmediate(albumArt.gameObject);

                    timelineButtonPrefab.GetComponent<Image>().sprite = null;

                    title.name = "name";
                    artist.name = "description";

                    title.AsRT().anchoredPosition = new Vector2(0f, 50f);
                    title.AsRT().sizeDelta = new Vector2(-32f, 40f);
                    artist.AsRT().anchoredPosition = new Vector2(0f, -28f);
                    artist.AsRT().sizeDelta = new Vector2(-32f, 140f);

                    var tmpTitle = title.GetComponent<TextMeshProUGUI>();
                    tmpTitle.alignment = TextAlignmentOptions.TopLeft;
                    tmpTitle.fontSize = 21;
                    var tmpArtist = artist.GetComponent<TextMeshProUGUI>();
                    tmpArtist.alignment = TextAlignmentOptions.TopLeft;
                    tmpArtist.fontSize = 16;
                    tmpArtist.enableWordWrapping = true;
                    tmpArtist.text = $"Players learn very basic stuff about Classic Arrhythmia / Project Arrhythmia mechanics.{Environment.NewLine}{Environment.NewLine}(Click on this button to open the level.)";

                    var edit = EditorPrefabHolder.Instance.CloseButton.Duplicate(timelineButtonPrefab.transform, "edit");
                    edit.transform.AsRT().anchoredPosition = new Vector2(-46f, -16f);
                    edit.transform.AsRT().pivot = new Vector2(0.5f, 0.5f);
                    edit.transform.AsRT().sizeDelta = new Vector2(24f, 24f);
                    edit.transform.GetChild(0).AsRT().sizeDelta = new Vector2(0f, 0f);
                    var editButton = edit.GetComponent<Button>();
                    editButton.colors = UIManager.SetColorBlock(editButton.colors, new Color(0.9f, 0.9f, 0.9f, 1f), Color.white, Color.white, new Color(0.9f, 0.9f, 0.9f, 1f), LSColors.red700);
                    var spriteImage = edit.transform.GetChild(0).GetComponent<Image>();
                    spriteImage.color = new Color(0.037f, 0.037f, 0.037f, 1f);
                    spriteImage.sprite = EditorSprites.EditSprite;

                    var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(timelineButtonPrefab.transform, "delete");
                    new RectValues(new Vector2(-16f, -16f), Vector2.one, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(24f, 24f)).AssignToRectTransform(delete.transform.AsRT());
                    
                    var moveBack = EditorPrefabHolder.Instance.SpriteButton.Duplicate(timelineButtonPrefab.transform, "<");
                    new RectValues(new Vector2(-136f, -16f), Vector2.one, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(24f, 24f)).AssignToRectTransform(moveBack.transform.AsRT());
                    moveBack.GetComponent<Image>().sprite = EditorSprites.LeftArrow;
                    
                    var moveForward = EditorPrefabHolder.Instance.SpriteButton.Duplicate(timelineButtonPrefab.transform, ">");
                    new RectValues(new Vector2(-96f, -16f), Vector2.one, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(24f, 24f)).AssignToRectTransform(moveForward.transform.AsRT());
                    moveForward.GetComponent<Image>().sprite = EditorSprites.RightArrow;

                    timelineAddPrefab = baseCardPrefab.Duplicate(assetsParent, "timeline add prefab");
                    var albumArtAdd = timelineAddPrefab.transform.GetChild(0);
                    var titleAdd = timelineAddPrefab.transform.GetChild(1);
                    var artistAdd = timelineAddPrefab.transform.GetChild(2);

                    timelineAddPrefab.GetComponent<Image>().sprite = null;

                    CoreHelper.Delete(albumArtAdd);

                    artistAdd.SetParent(prefab.transform);
                    artistAdd.SetSiblingIndex(2);
                    RectValues.FullAnchored.AnchorMin(0f, 1f).Pivot(0.5f, 1f).SizeDelta(-10f, 32f).AssignToRectTransform(artistAdd.AsRT());
                    artistAdd.name = "name";

                    var tmpArtistAdd = artistAdd.GetComponent<TextMeshProUGUI>();
                    tmpArtistAdd.alignment = TextAlignmentOptions.TopLeft;
                    tmpArtistAdd.color = Color.white;
                    tmpArtistAdd.fontSize = 25;

                    titleAdd.AsRT().anchoredPosition = Vector2.zero;
                    titleAdd.AsRT().sizeDelta = new Vector2(0f, 400f);
                    titleAdd.name = "add";

                    timelineAddPrefab.transform.AsRT().sizeDelta = new Vector2(200f, 200f);

                    var tmpTitleAdd = titleAdd.GetComponent<TextMeshProUGUI>();
                    tmpTitleAdd.fontSize = 50;
                    tmpTitleAdd.text = "Add Event";

                    prefabs.Add(prefab);
                }

                // Schedule
                {
                    var prefab = baseCardPrefab.Duplicate(assetsParent, "schedule prefab");
                    var albumArt = prefab.transform.GetChild(0);
                    var title = prefab.transform.GetChild(1);
                    var artist = prefab.transform.GetChild(2);

                    Destroy(albumArt.gameObject);
                    Destroy(artist.gameObject);

                    prefab.GetComponent<Image>().sprite = null;

                    title.name = "text";

                    title.AsRT().anchoredPosition = new Vector2(-20f, 0f);
                    title.AsRT().sizeDelta = new Vector2(-80f, 64f);

                    var tmp = title.GetComponent<TextMeshProUGUI>();
                    tmp.alignment = TextAlignmentOptions.Left;
                    tmp.enableWordWrapping = false;
                    tmp.text = DateTime.Now.ToString("g");

                    var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(prefab.transform, "delete");
                    RectValues.Default.AnchoredPosition(605f, 0f).SizeDelta(38f, 38f).AssignToRectTransform(delete.transform.AsRT());

                    prefabs.Add(prefab);
                }

                // Note
                {
                    var prefab = Creator.NewUIObject("note prefab", assetsParent);

                    prefab.transform.AsRT().sizeDelta = new Vector2(300f, 150f);
                    var prefabImage = prefab.AddComponent<Image>();
                    prefabImage.color = new Color(0.251f, 0.251f, 0.251f, 1f);

                    //var inputField = prefab.AddComponent<TMP_InputField>();

                    var prefabPanel = Creator.NewUIObject("panel", prefab.transform);
                    UIManager.SetRectTransform(prefabPanel.transform.AsRT(), Vector2.zero, Vector2.one, new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 32f));
                    var prefabPanelImage = prefabPanel.AddComponent<Image>();

                    var noteTitle = baseCardPrefab.transform.Find("title").gameObject.Duplicate(prefabPanel.transform, "title");
                    noteTitle.transform.AsRT().anchoredPosition = new Vector2(-26f, 0f);
                    noteTitle.transform.AsRT().sizeDelta = new Vector2(-70f, 40f);

                    var tmpNoteTitle = noteTitle.GetComponent<TextMeshProUGUI>();
                    tmpNoteTitle.alignment = TextAlignmentOptions.Left;
                    tmpNoteTitle.enableWordWrapping = false;
                    tmpNoteTitle.fontSize = 16;

                    var toggle = EditorPrefabHolder.Instance.Toggle.Duplicate(prefabPanel.transform, "active");
                    UIManager.SetRectTransform(toggle.transform.AsRT(), Vector2.zero, new Vector2(0.87f, 0.5f), new Vector2(0.87f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
                    UIManager.SetRectTransform(toggle.transform.GetChild(0).AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(24f, 24f));

                    var editPrefab = EditorPrefabHolder.Instance.CloseButton.Duplicate(prefabPanel.transform, "edit");
                    UIManager.SetRectTransform(editPrefab.transform.AsRT(), new Vector2(-44f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26f, 26f));
                    editPrefab.transform.GetChild(0).AsRT().sizeDelta = new Vector2(4f, 4f);
                    var editPrefabButton = editPrefab.GetComponent<Button>();
                    editPrefabButton.colors = UIManager.SetColorBlock(editPrefabButton.colors, new Color(0.9f, 0.9f, 0.9f, 1f), Color.white, Color.white, new Color(0.9f, 0.9f, 0.9f, 1f), LSColors.red700);
                    var spritePrefabImage = editPrefab.transform.GetChild(0).GetComponent<Image>();
                    spritePrefabImage.color = new Color(0.037f, 0.037f, 0.037f, 1f);
                    spritePrefabImage.sprite = EditorSprites.EditSprite;

                    var closeB = EditorPrefabHolder.Instance.CloseButton.Duplicate(prefabPanel.transform, "close");
                    UIManager.SetRectTransform(closeB.transform.AsRT(), new Vector2(-16f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26f, 26f));

                    var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(prefabPanel.transform, "delete");
                    UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(-16f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26f, 26f));

                    var noteText = baseCardPrefab.transform.Find("artist").gameObject.Duplicate(prefab.transform, "text");
                    UIManager.SetRectTransform(noteText.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-16f, 0f));

                    var tmpNoteText = noteText.GetComponent<TextMeshProUGUI>();
                    tmpNoteText.alignment = TextAlignmentOptions.TopLeft;
                    tmpNoteText.alpha = 1f;
                    tmpNoteText.enableWordWrapping = true;
                    tmpNoteText.fontSize = 14;

                    //inputField.textComponent = tmpNoteText;

                    prefabs.Add(prefab);
                }

                // OST
                {
                    var prefab = baseCardPrefab.Duplicate(assetsParent, "ost prefab");
                    var albumArt = prefab.transform.GetChild(0);
                    var title = prefab.transform.GetChild(1);
                    var artist = prefab.transform.GetChild(2);

                    Destroy(albumArt.gameObject);
                    Destroy(artist.gameObject);

                    prefab.GetComponent<Image>().sprite = null;

                    title.name = "text";

                    title.AsRT().anchoredPosition = new Vector2(-20f, 0f);
                    title.AsRT().sizeDelta = new Vector2(-80f, 64f);

                    var tmp = title.GetComponent<TextMeshProUGUI>();
                    tmp.alignment = TextAlignmentOptions.Left;
                    tmp.enableWordWrapping = false;
                    tmp.text = "Kaixo - Pyrolysis";

                    var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(prefab.transform, "delete");
                    UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(605f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(38f, 38f));

                    prefabs.Add(prefab);
                }
            }

            // Mouse Drag Textures
            {
                horizontalDrag = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                var bytes = File.ReadAllBytes(AssetPack.GetFile($"core/sprites/mouse_scroll_h{FileFormat.PNG.Dot()}"));
                horizontalDrag.LoadImage(bytes);

                horizontalDrag.wrapMode = TextureWrapMode.Clamp;
                horizontalDrag.filterMode = FilterMode.Point;
                horizontalDrag.Apply();

                verticalDrag = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                bytes = File.ReadAllBytes(AssetPack.GetFile($"core/sprites/mouse_scroll_v{FileFormat.PNG.Dot()}"));
                verticalDrag.LoadImage(bytes);

                verticalDrag.wrapMode = TextureWrapMode.Clamp;
                verticalDrag.filterMode = FilterMode.Point;
                verticalDrag.Apply();
            }

            notesParent = Creator.NewUIObject("notes", plannerBase).transform.AsRT(); // floating notes

            // Document Full View
            {
                var fullView = Creator.NewUIObject("document full view", contentBase);
                documentFullView = fullView;
                var fullViewImage = fullView.AddComponent<Image>();
                fullViewImage.color = new Color(0.082f, 0.082f, 0.078f, 1f);
                UIManager.SetRectTransform(fullView.transform.AsRT(), new Vector2(690f, -548f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(1384f, 936f));

                EditorThemeManager.ApplyGraphic(fullViewImage, ThemeGroup.Background_1);

                var docTitle = tmpTextPrefab.Duplicate(fullView.transform, "title");

                docTitle.transform.AsRT().anchoredPosition = new Vector2(0f, 15f);
                docTitle.transform.AsRT().sizeDelta = new Vector2(-32, 840f);

                documentTitle = docTitle.GetComponent<TextMeshProUGUI>();
                documentTitle.alignment = TextAlignmentOptions.TopLeft;
                documentTitle.fontSize = 32;

                var docTextInput = Creator.NewUIObject("text", fullView.transform);
                new RectValues(new Vector2(0f, -50f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-32f, 812f)).AssignToRectTransform(docTextInput.transform.AsRT());
                docTextInput.AddComponent<Image>();
                docTextInput.AddComponent<Mask>();

                documentInputField = docTextInput.AddComponent<TMP_InputField>();

                var docText = tmpTextPrefab.Duplicate(docTextInput.transform, "text");
                RectValues.FullAnchored.AssignToRectTransform(docText.transform.AsRT());
                var t = docText.GetComponent<TextMeshProUGUI>();
                t.overflowMode = TextOverflowModes.Overflow;
                t.alignment = TextAlignmentOptions.TopLeft;
                t.enableWordWrapping = true;

                documentInputField.textViewport = docText.transform.AsRT();
                documentInputField.textComponent = t;
                documentInputField.lineType = TMP_InputField.LineType.MultiLineNewline;
                documentInputField.scrollSensitivity = 20f;
                documentInputField.interactable = false;

                documentHyperlinks = docText.AddComponent<OpenHyperlinks>();
                documentHyperlinks.enabled = true;
                var docTextButton = docText.gameObject.AddComponent<Button>();
                docTextButton.enabled = true;

                EditorThemeManager.ApplyLightText(documentTitle);
                EditorThemeManager.ApplyInputField(documentInputField);

                var docToggle = EditorPrefabHolder.Instance.ToggleButton.Duplicate(fullView.transform);
                RectValues.Default.AnchoredPosition(500f, 420f).SizeDelta(300f, 32f).AssignToRectTransform(docToggle.transform.AsRT());
                var docToggleStorage = docToggle.GetComponent<ToggleButtonStorage>();
                documentInteractibleToggle = docToggleStorage.toggle;
                docToggleStorage.Text = "Interactible";
                docToggleStorage.SetIsOnWithoutNotify(false);
                docToggleStorage.OnValueChanged.NewListener(_val =>
                {
                    documentInputField.interactable = _val;
                    // documentInputField.readOnly = _val
                    docTextButton.enabled = !_val;
                    documentHyperlinks.enabled = !_val;
                });
                EditorThemeManager.ClearSelectableColors(docTextButton);

                EditorThemeManager.ApplyToggle(documentInteractibleToggle, graphic: docToggleStorage.label);

                fullView.SetActive(false);
            }

            // Character Full View
            {
                var fullView = Creator.NewUIObject("character full view", contentBase);
                characterFullView = fullView;
                var fullViewImage = fullView.AddComponent<Image>();
                fullViewImage.color = new Color(0.082f, 0.082f, 0.078f, 1f);
                UIManager.SetRectTransform(fullView.transform.AsRT(), new Vector2(690f, -548f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(1384f, 936f));

                EditorThemeManager.ApplyGraphic(fullViewImage, ThemeGroup.Background_1);

                var characterSpriteBase = Creator.NewUIObject("sprite", fullView.transform);
                RectValues.Default.AnchoredPosition(-500f, 300f).SizeDelta(300f, 300f).AssignToRectTransform(characterSpriteBase.transform.AsRT());
                var characterSpriteBaseImage = characterSpriteBase.AddComponent<Image>();
                characterSpriteBase.AddComponent<Mask>().showMaskGraphic = false;

                characterSprite = Creator.NewUIObject("image", characterSpriteBase.transform).AddComponent<Image>();
                RectValues.FullAnchored.AssignToRectTransform(characterSprite.rectTransform);

                var characterDetails = tmpTextPrefab.Duplicate(fullView.transform, "details");
                characterDetails.transform.AsRT().anchoredPosition = new Vector2(150f, 70f);
                characterDetails.transform.AsRT().sizeDelta = new Vector2(200f, 100f);

                this.characterDetails = characterDetails.GetComponent<TextMeshProUGUI>();
                this.characterDetails.alignment = TextAlignmentOptions.TopLeft;
                this.characterDetails.fontSize = 32;
                this.characterDetails.overflowMode = TextOverflowModes.Masking;
                characterDetailsHyperlinks = characterDetails.AddComponent<OpenHyperlinks>();

                var characterAttributesScrollView = new ScrollViewElement(ScrollViewElement.Direction.Vertical);
                characterAttributesScrollView.Init(EditorElement.InitSettings.Default.Parent(fullView.transform).Rect(RectValues.Default.AnchoredPosition(-450f, -230f).SizeDelta(400f, 400f)));
                characterAttributesContent = characterAttributesScrollView.Content;
                EditorThemeManager.ApplyGraphic(characterAttributesScrollView.GameObject.GetOrAddComponent<Image>(), ThemeGroup.Background_2, true);

                var characterDescription = Creator.NewUIObject("description", fullView.transform);
                RectValues.Default.AnchoredPosition(220f, 0f).SizeDelta(900f, 900f).AssignToRectTransform(characterDescription.transform.AsRT());
                characterDescription.AddComponent<Image>();
                characterDescription.AddComponent<Mask>();

                characterDescriptionInputField = characterDescription.AddComponent<TMP_InputField>();

                var characterDescriptionInputFieldText = tmpTextPrefab.Duplicate(characterDescription.transform, "text");
                RectValues.FullAnchored.AssignToRectTransform(characterDescriptionInputFieldText.transform.AsRT());
                var t = characterDescriptionInputFieldText.GetComponent<TextMeshProUGUI>();
                t.overflowMode = TextOverflowModes.Overflow;
                t.alignment = TextAlignmentOptions.TopLeft;
                t.enableWordWrapping = true;

                characterDescriptionInputField.textViewport = characterDescriptionInputFieldText.transform.AsRT();
                characterDescriptionInputField.textComponent = t;
                characterDescriptionInputField.lineType = TMP_InputField.LineType.MultiLineNewline;
                characterDescriptionInputField.scrollSensitivity = 20f;
                characterDescriptionInputField.interactable = false;

                characterDescriptionHyperlinks = characterDescriptionInputFieldText.AddComponent<OpenHyperlinks>();
                var docTextButton = characterDescriptionInputFieldText.AddComponent<Button>();
                docTextButton.enabled = true;

                EditorThemeManager.ApplyGraphic(characterSprite, ThemeGroup.Null, true);
                EditorThemeManager.ApplyLightText(this.characterDetails);
                EditorThemeManager.ApplyInputField(characterDescriptionInputField);

                fullView.SetActive(false);
            }

            // Editor
            {
                var editorBase = Creator.NewUIObject("editor base", contentBase);
                editorBase.transform.AsRT().anchoredPosition = new Vector2(691f, -40f);
                editorBase.transform.AsRT().sizeDelta = new Vector2(537f, 936f);
                var editorBaseImage = editorBase.AddComponent<Image>();
                editorBaseImage.color = new Color(0.078f, 0.067f, 0.067f, 1f);

                EditorThemeManager.ApplyGraphic(editorBaseImage, ThemeGroup.Background_3);

                var editor = Creator.NewUIObject("editor", editorBase.transform);
                editor.transform.AsRT().anchoredPosition = Vector3.zero;
                editor.transform.AsRT().sizeDelta = new Vector2(524f, 936f);

                var panel = Creator.NewUIObject("panel", editor.transform);
                UIManager.SetRectTransform(panel.transform.AsRT(), new Vector2(0f, 436f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(14f, 64f));
                editorTitlePanel = panel.AddComponent<Image>();
                editorTitlePanel.color = new Color(0.310f, 0.467f, 0.737f, 1f);
                EditorThemeManager.ApplyGraphic(editorTitlePanel, ThemeGroup.Null, true);

                var editorTitle = baseCardPrefab.transform.Find("artist").gameObject.Duplicate(panel.transform, "title");
                RectValues.FullAnchored.AssignToRectTransform(editorTitle.transform.AsRT());
                var tmpEditorTitle = editorTitle.GetComponent<TextMeshProUGUI>();
                tmpEditorTitle.alignment = TextAlignmentOptions.Center;
                tmpEditorTitle.fontSize = 32;
                tmpEditorTitle.text = "<b>- Editor -</b>";
                panel.AddComponent<ContrastColors>().Init(tmpEditorTitle, editorTitlePanel);
                panel.SetActive(false);

                // Document
                {
                    var g1 = Creator.NewUIObject("Document", editor.transform);
                    UIManager.SetRectTransform(g1.transform.AsRT(), new Vector2(0f, -32f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, -64f));

                    var vlg = g1.AddComponent<VerticalLayoutGroup>();
                    vlg.childControlHeight = false;
                    vlg.childForceExpandHeight = false;
                    vlg.spacing = 4f;

                    var labelInitSettings = EditorElement.InitSettings.Default.Parent(g1.transform).Rect(RectValues.Default.SizeDelta(524f, 32f));

                    new LabelsElement("Edit Name").Init(labelInitSettings);

                    var text1 = textEditorPrefab.Duplicate(g1.transform, "text");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    documentEditorName = text1.GetComponent<InputField>();
                    EditorThemeManager.ApplyInputField(documentEditorName);

                    new LabelsElement("Edit Text").Init(labelInitSettings);

                    var text2 = textEditorPrefab.Duplicate(g1.transform, "text");
                    text2.transform.AsRT().sizeDelta = new Vector2(537f, 720f);
                    text2.gameObject.SetActive(true);

                    documentEditorText = text2.GetComponent<InputField>();
                    documentEditorText.textComponent.alignment = TextAnchor.UpperLeft;
                    ((Text)documentEditorText.placeholder).alignment = TextAnchor.UpperLeft;
                    documentEditorText.lineType = InputField.LineType.MultiLineNewline;
                    EditorThemeManager.ApplyInputField(documentEditorText);

                    g1.SetActive(false);
                    editors.Add(g1);
                }

                // TODO
                {
                    var g1 = Creator.NewUIObject("TODO", editor.transform);
                    UIManager.SetRectTransform(g1.transform.AsRT(), new Vector2(0f, -32f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, -64f));

                    var vlg = g1.AddComponent<VerticalLayoutGroup>();
                    vlg.childControlHeight = false;
                    vlg.childForceExpandHeight = false;
                    vlg.spacing = 4f;

                    var labelInitSettings = EditorElement.InitSettings.Default.Parent(g1.transform).Rect(RectValues.Default.SizeDelta(524f, 32f));

                    new LabelsElement("Edit Text").Init(labelInitSettings);

                    var text1 = textEditorPrefab.Duplicate(g1.transform, "text");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    todoEditorText = text1.GetComponent<InputField>();
                    EditorThemeManager.ApplyInputField(todoEditorText);

                    new LabelsElement("Change TODO Priority").Init(labelInitSettings);

                    var moveUp = EditorPrefabHolder.Instance.Function2Button.Duplicate(g1.transform);
                    var moveUpStorage = moveUp.GetComponent<FunctionButtonStorage>();
                    moveUp.SetActive(true);
                    moveUp.name = "move up";
                    moveUp.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                    moveUp.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                    moveUpStorage.Text = "Move Up";
                    todoEditorMoveUpButton = moveUpStorage.button;
                    EditorThemeManager.ApplySelectable(todoEditorMoveUpButton, ThemeGroup.Function_2);
                    EditorThemeManager.ApplyGraphic(moveUpStorage.label, ThemeGroup.Function_2_Text);

                    var moveDown = EditorPrefabHolder.Instance.Function2Button.Duplicate(g1.transform);
                    var moveDownStorage = moveDown.GetComponent<FunctionButtonStorage>();
                    moveDown.SetActive(true);
                    moveDown.name = "move down";
                    moveDown.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                    moveDown.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                    moveDownStorage.Text = "Move Down";
                    todoEditorMoveDownButton = moveDownStorage.button;
                    EditorThemeManager.ApplySelectable(todoEditorMoveDownButton, ThemeGroup.Function_2);
                    EditorThemeManager.ApplyGraphic(moveDownStorage.label, ThemeGroup.Function_2_Text);

                    g1.SetActive(false);
                    editors.Add(g1);
                }

                // Character
                {
                    var scrollViewElement = new ScrollViewElement(ScrollViewElement.Direction.Vertical);
                    scrollViewElement.Init(EditorElement.InitSettings.Default.Parent(editor.transform).Rect(RectValues.FullAnchored.AnchoredPosition(0f, -32f).SizeDelta(0f, -64f)));
                    var content = scrollViewElement.Content;

                    var labelInitSettings = EditorElement.InitSettings.Default.Parent(content).Rect(RectValues.Default.SizeDelta(524f, 32f));

                    new LabelsElement("Edit Name").Init(labelInitSettings);

                    var text1 = textEditorPrefab.Duplicate(content, "name");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    characterEditorName = text1.GetComponent<InputField>();
                    EditorThemeManager.ApplyInputField(characterEditorName);

                    new LabelsElement("Edit Gender").Init(labelInitSettings);

                    var text2 = textEditorPrefab.Duplicate(content, "gender");
                    text2.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text2.gameObject.SetActive(true);

                    characterEditorGender = text2.GetComponent<InputField>();
                    EditorThemeManager.ApplyInputField(characterEditorGender);

                    new LabelsElement("Edit Origin").Init(labelInitSettings);

                    var text3 = textEditorPrefab.Duplicate(content, "origin");
                    text3.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text3.gameObject.SetActive(true);

                    characterEditorOrigin = text3.GetComponent<InputField>();
                    EditorThemeManager.ApplyInputField(characterEditorOrigin);

                    new LabelsElement("Edit Description").Init(labelInitSettings);

                    var text4 = textEditorPrefab.Duplicate(content, "description");
                    text4.transform.AsRT().sizeDelta = new Vector2(537f, 500f);
                    text4.gameObject.SetActive(true);

                    characterEditorDescription = text4.GetComponent<InputField>();
                    characterEditorDescription.textComponent.alignment = TextAnchor.UpperLeft;
                    ((Text)characterEditorDescription.placeholder).alignment = TextAnchor.UpperLeft;
                    characterEditorDescription.lineType = InputField.LineType.MultiLineNewline;
                    EditorThemeManager.ApplyInputField(characterEditorDescription);

                    new LabelsElement("Select Profile Image").Init(labelInitSettings);

                    new ButtonElement("Select", SelectCharacterImage).Init(EditorElement.InitSettings.Default.Parent(content));

                    new LabelsElement("Traits").Init(labelInitSettings);

                    // Traits
                    {
                        var tagScrollView = Creator.NewUIObject("Traits Scroll View", content);
                        tagScrollView.transform.AsRT().sizeDelta = new Vector2(522f, 120f);
                        var scroll = tagScrollView.AddComponent<ScrollRect>();

                        scroll.horizontal = false;
                        scroll.vertical = true;

                        var image = tagScrollView.AddComponent<Image>();
                        image.color = new Color(1f, 1f, 1f, 0.01f);

                        tagScrollView.AddComponent<Mask>();

                        var tagViewport = Creator.NewUIObject("Viewport", tagScrollView.transform);
                        UIManager.SetRectTransform(tagViewport.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);

                        var tagContent = Creator.NewUIObject("Content", tagViewport.transform);

                        var tagContentGLG = tagContent.AddComponent<GridLayoutGroup>();
                        tagContentGLG.cellSize = new Vector2(500f, 32f);
                        tagContentGLG.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                        tagContentGLG.constraintCount = 1;
                        tagContentGLG.childAlignment = TextAnchor.MiddleLeft;
                        tagContentGLG.spacing = new Vector2(8f, 8f);

                        var tagContentCSF = tagContent.AddComponent<ContentSizeFitter>();
                        tagContentCSF.horizontalFit = ContentSizeFitter.FitMode.MinSize;
                        tagContentCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

                        scroll.viewport = tagViewport.transform.AsRT();
                        scroll.content = tagContent.transform.AsRT();

                        characterEditorTraitsContent = tagContent.transform.AsRT();
                    }

                    new LabelsElement("Lore").Init(labelInitSettings);

                    // Lore
                    {
                        var tagScrollView = Creator.NewUIObject("Lore Scroll View", content);
                        tagScrollView.transform.AsRT().sizeDelta = new Vector2(522f, 120f);
                        var scroll = tagScrollView.AddComponent<ScrollRect>();

                        scroll.horizontal = false;
                        scroll.vertical = true;

                        var image = tagScrollView.AddComponent<Image>();
                        image.color = new Color(1f, 1f, 1f, 0.01f);

                        tagScrollView.AddComponent<Mask>();

                        var tagViewport = Creator.NewUIObject("Viewport", tagScrollView.transform);
                        UIManager.SetRectTransform(tagViewport.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);

                        var tagContent = Creator.NewUIObject("Content", tagViewport.transform);

                        var tagContentGLG = tagContent.AddComponent<GridLayoutGroup>();
                        tagContentGLG.cellSize = new Vector2(500f, 32f);
                        tagContentGLG.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                        tagContentGLG.constraintCount = 1;
                        tagContentGLG.childAlignment = TextAnchor.MiddleLeft;
                        tagContentGLG.spacing = new Vector2(8f, 8f);

                        var tagContentCSF = tagContent.AddComponent<ContentSizeFitter>();
                        tagContentCSF.horizontalFit = ContentSizeFitter.FitMode.MinSize;
                        tagContentCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

                        scroll.viewport = tagViewport.transform.AsRT();
                        scroll.content = tagContent.transform.AsRT();

                        characterEditorLoreContent = tagContent.transform.AsRT();
                    }

                    new LabelsElement("Abilities").Init(labelInitSettings);

                    // Abilities
                    {
                        var tagScrollView = Creator.NewUIObject("Abilities Scroll View", content);
                        tagScrollView.transform.AsRT().sizeDelta = new Vector2(522f, 120f);
                        var scroll = tagScrollView.AddComponent<ScrollRect>();

                        scroll.horizontal = false;
                        scroll.vertical = true;

                        var image = tagScrollView.AddComponent<Image>();
                        image.color = new Color(1f, 1f, 1f, 0.01f);

                        tagScrollView.AddComponent<Mask>();

                        var tagViewport = Creator.NewUIObject("Viewport", tagScrollView.transform);
                        UIManager.SetRectTransform(tagViewport.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);

                        var tagContent = Creator.NewUIObject("Content", tagViewport.transform);

                        var tagContentGLG = tagContent.AddComponent<GridLayoutGroup>();
                        tagContentGLG.cellSize = new Vector2(500f, 32f);
                        tagContentGLG.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                        tagContentGLG.constraintCount = 1;
                        tagContentGLG.childAlignment = TextAnchor.MiddleLeft;
                        tagContentGLG.spacing = new Vector2(8f, 8f);

                        var tagContentCSF = tagContent.AddComponent<ContentSizeFitter>();
                        tagContentCSF.horizontalFit = ContentSizeFitter.FitMode.MinSize;
                        tagContentCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

                        scroll.viewport = tagViewport.transform.AsRT();
                        scroll.content = tagContent.transform.AsRT();

                        characterEditorAbilitiesContent = tagContent.transform.AsRT();
                    }

                    // Tag Prefab
                    {
                        tagPrefab = Creator.NewUIObject("Tag", assetsParent);
                        var tagPrefabImage = tagPrefab.AddComponent<Image>();
                        tagPrefabImage.color = new Color(1f, 1f, 1f, 0.12f);
                        var tagPrefabLayout = tagPrefab.AddComponent<HorizontalLayoutGroup>();
                        tagPrefabLayout.childControlWidth = false;
                        tagPrefabLayout.childForceExpandWidth = false;

                        var input = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(tagPrefab.transform, "Input");
                        input.transform.localScale = Vector3.one;
                        input.transform.AsRT().sizeDelta = new Vector2(500f, 32f);
                        var text = input.transform.Find("Text").GetComponent<Text>();
                        text.alignment = TextAnchor.MiddleLeft;
                        text.fontSize = 17;

                        var delete = EditorPrefabHolder.Instance.DeleteButton.gameObject.Duplicate(tagPrefab.transform, "Delete");
                        UIManager.SetRectTransform(delete.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.one, Vector2.one, new Vector2(32f, 32f));
                    }

                    scrollViewElement.SetActive(false);
                    editors.Add(scrollViewElement.GameObject);
                }

                // Timeline
                {
                    var g1 = Creator.NewUIObject("Timeline", editor.transform);
                    UIManager.SetRectTransform(g1.transform.AsRT(), new Vector2(0f, -32f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, -64f));

                    var vlg = g1.AddComponent<VerticalLayoutGroup>();
                    vlg.childControlHeight = false;
                    vlg.childForceExpandHeight = false;
                    vlg.spacing = 4f;

                    var labelInitSettings = EditorElement.InitSettings.Default.Parent(g1.transform).Rect(RectValues.Default.SizeDelta(524f, 32f));

                    new LabelsElement("Edit Name").Init(labelInitSettings);

                    var text1 = textEditorPrefab.Duplicate(g1.transform, "text");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    timelineEditorName = text1.GetComponent<InputField>();
                    EditorThemeManager.ApplyInputField(timelineEditorName);

                    g1.SetActive(false);
                    editors.Add(g1);
                }

                // Event
                {
                    var g1 = Creator.NewUIObject("Event", editor.transform);
                    UIManager.SetRectTransform(g1.transform.AsRT(), new Vector2(0f, -32f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, -64f));

                    var vlg = g1.AddComponent<VerticalLayoutGroup>();
                    vlg.childControlHeight = false;
                    vlg.childForceExpandHeight = false;
                    vlg.spacing = 4f;

                    var labelInitSettings = EditorElement.InitSettings.Default.Parent(g1.transform).Rect(RectValues.Default.SizeDelta(524f, 32f));

                    new LabelsElement("Edit Name").Init(labelInitSettings);

                    var text1 = textEditorPrefab.Duplicate(g1.transform, "name");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    eventEditorName = text1.GetComponent<InputField>();
                    EditorThemeManager.ApplyInputField(eventEditorName);

                    new LabelsElement("Edit Description").Init(labelInitSettings);

                    var text2 = textEditorPrefab.Duplicate(g1.transform, "description");
                    text2.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text2.gameObject.SetActive(true);

                    eventEditorDescription = text2.GetComponent<InputField>();
                    eventEditorDescription.textComponent.alignment = TextAnchor.UpperLeft;
                    ((Text)eventEditorDescription.placeholder).alignment = TextAnchor.UpperLeft;
                    eventEditorDescription.lineType = InputField.LineType.MultiLineNewline;
                    EditorThemeManager.ApplyInputField(eventEditorDescription);

                    new LabelsElement("Edit Path").Init(labelInitSettings);

                    var text3 = textEditorPrefab.Duplicate(g1.transform, "path");
                    text3.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text3.gameObject.SetActive(true);

                    eventEditorPath = text3.GetComponent<InputField>();
                    EditorThemeManager.ApplyInputField(eventEditorPath);

                    new LabelsElement("Edit Type").Init(labelInitSettings);

                    var renderType = EditorPrefabHolder.Instance.Dropdown.Duplicate(g1.transform, "type");
                    eventEditorType = renderType.GetComponent<Dropdown>();
                    eventEditorType.options = CoreHelper.StringToOptionData("Level", "Cutscene", "Story");
                    EditorThemeManager.ApplyDropdown(eventEditorType);

                    g1.SetActive(false);
                    editors.Add(g1);
                }

                // Schedule
                {
                    var g1 = Creator.NewUIObject("Schedule", editor.transform);
                    UIManager.SetRectTransform(g1.transform.AsRT(), new Vector2(0f, -32f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, -64f));

                    var vlg = g1.AddComponent<VerticalLayoutGroup>();
                    vlg.childControlHeight = false;
                    vlg.childForceExpandHeight = false;
                    vlg.spacing = 4f;

                    var labelInitSettings = EditorElement.InitSettings.Default.Parent(g1.transform).Rect(RectValues.Default.SizeDelta(524f, 32f));

                    new LabelsElement("Edit Name").Init(labelInitSettings);

                    var text1 = textEditorPrefab.Duplicate(g1.transform, "name");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    scheduleEditorDescription = text1.GetComponent<InputField>();
                    EditorThemeManager.ApplyInputField(scheduleEditorDescription);

                    new LabelsElement("Edit Year").Init(labelInitSettings);

                    var text2 = textEditorPrefab.Duplicate(g1.transform, "year");
                    text2.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text2.gameObject.SetActive(true);

                    scheduleEditorYear = text2.GetComponent<InputField>();
                    EditorThemeManager.ApplyInputField(scheduleEditorYear);

                    new LabelsElement("Edit Month").Init(labelInitSettings);

                    var renderType = EditorPrefabHolder.Instance.Dropdown.Duplicate(g1.transform, "month");
                    scheduleEditorMonth = renderType.GetComponent<Dropdown>();
                    scheduleEditorMonth.options = CoreHelper.StringToOptionData("January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December");
                    EditorThemeManager.ApplyDropdown(scheduleEditorMonth);

                    new LabelsElement("Edit Day").Init(labelInitSettings);

                    var text3 = textEditorPrefab.Duplicate(g1.transform, "day");
                    text3.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text3.gameObject.SetActive(true);

                    scheduleEditorDay = text3.GetComponent<InputField>();
                    EditorThemeManager.ApplyInputField(scheduleEditorDay);

                    new LabelsElement("Edit Hour").Init(labelInitSettings);

                    var text4 = textEditorPrefab.Duplicate(g1.transform, "hour");
                    text4.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text4.gameObject.SetActive(true);

                    scheduleEditorHour = text4.GetComponent<InputField>();
                    EditorThemeManager.ApplyInputField(scheduleEditorHour);

                    new LabelsElement("Edit Minute").Init(labelInitSettings);

                    var text5 = textEditorPrefab.Duplicate(g1.transform, "minute");
                    text5.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text5.gameObject.SetActive(true);

                    scheduleEditorMinute = text5.GetComponent<InputField>();
                    EditorThemeManager.ApplyInputField(scheduleEditorMinute);

                    g1.SetActive(false);
                    editors.Add(g1);
                }

                // Note
                {
                    var g1 = Creator.NewUIObject("Note", editor.transform);
                    UIManager.SetRectTransform(g1.transform.AsRT(), new Vector2(0f, -32f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, -64f));

                    var vlg = g1.AddComponent<VerticalLayoutGroup>();
                    vlg.childControlHeight = false;
                    vlg.childForceExpandHeight = false;
                    vlg.spacing = 4f;

                    var labelInitSettings = EditorElement.InitSettings.Default.Parent(g1.transform).Rect(RectValues.Default.SizeDelta(524f, 32f));

                    new LabelsElement("Edit Name").Init(labelInitSettings);

                    var text1 = textEditorPrefab.Duplicate(g1.transform, "text");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    noteEditorName = text1.GetComponent<InputField>();
                    EditorThemeManager.ApplyInputField(noteEditorName);

                    new LabelsElement("Edit Text").Init(labelInitSettings);

                    var text2 = textEditorPrefab.Duplicate(g1.transform, "text");
                    text2.transform.AsRT().sizeDelta = new Vector2(537f, 360f);
                    text2.gameObject.SetActive(true);

                    noteEditorText = text2.GetComponent<InputField>();
                    noteEditorText.textComponent.alignment = TextAnchor.UpperLeft;
                    ((Text)noteEditorText.placeholder).alignment = TextAnchor.UpperLeft;
                    noteEditorText.lineType = InputField.LineType.MultiLineNewline;
                    EditorThemeManager.ApplyInputField(noteEditorText);

                    new LabelsElement("Edit Color").Init(labelInitSettings);

                    var colorBase = EditorPrefabHolder.Instance.ColorsLayout;
                    this.colorBase = colorBase.transform;
                    var colors = colorBase.Duplicate(g1.transform, "colors");
                    noteEditorColorsParent = colors.transform;
                    noteEditorColorsParent.AsRT().sizeDelta = new Vector2(537f, 64f);

                    LSHelpers.DeleteChildren(noteEditorColorsParent);
                    for (int i = 0; i < 18; i++)
                    {
                        var col = Instantiate(colorBase.transform.Find("1").gameObject);
                        col.name = (i + 1).ToString();
                        col.transform.SetParent(noteEditorColorsParent);
                        var toggle = col.GetComponent<Toggle>();
                        noteEditorColors.Add(toggle);

                        EditorThemeManager.ApplyGraphic(toggle.image, ThemeGroup.Null, true);
                        EditorThemeManager.ApplyGraphic(toggle.graphic, ThemeGroup.Background_3);
                    }

                    new LabelsElement("Reset Position and Scale").Init(labelInitSettings);

                    var reset = EditorPrefabHolder.Instance.Function2Button.gameObject.Duplicate(g1.transform);
                    var resetStorage = reset.GetComponent<FunctionButtonStorage>();
                    reset.SetActive(true);
                    reset.name = "reset";
                    reset.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                    reset.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                    resetStorage.Text = "Reset";
                    noteEditorReset = resetStorage.button;
                    EditorThemeManager.ApplySelectable(noteEditorReset, ThemeGroup.Function_2);
                    EditorThemeManager.ApplyGraphic(resetStorage.label, ThemeGroup.Function_2_Text);

                    g1.SetActive(false);
                    editors.Add(g1);
                }

                // OST
                {
                    var g1 = Creator.NewUIObject("OST", editor.transform);
                    UIManager.SetRectTransform(g1.transform.AsRT(), new Vector2(0f, -32f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, -64f));

                    var vlg = g1.AddComponent<VerticalLayoutGroup>();
                    vlg.childControlHeight = false;
                    vlg.childForceExpandHeight = false;
                    vlg.spacing = 4f;

                    var labelInitSettings = EditorElement.InitSettings.Default.Parent(g1.transform).Rect(RectValues.Default.SizeDelta(524f, 32f));

                    new LabelsElement("Edit Path").Init(labelInitSettings);

                    var text1 = textEditorPrefab.Duplicate(g1.transform, "path");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    ostEditorPath = text1.GetComponent<InputField>();
                    EditorThemeManager.ApplyInputField(ostEditorPath);

                    new LabelsElement("Edit Name").Init(labelInitSettings);

                    var text2 = textEditorPrefab.Duplicate(g1.transform, "name");
                    text2.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text2.gameObject.SetActive(true);

                    ostEditorName = text2.GetComponent<InputField>();
                    EditorThemeManager.ApplyInputField(ostEditorName);

                    new LabelsElement("Use Global Path").Init(labelInitSettings);

                    var global = EditorPrefabHolder.Instance.Function2Button.Duplicate(g1.transform);
                    var globalStorage = global.GetComponent<FunctionButtonStorage>();
                    global.SetActive(true);
                    global.name = "global";
                    global.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                    global.transform.AsRT().sizeDelta = new Vector2(200f, 32f);

                    ostEditorUseGlobal = globalStorage.button;
                    ostEditorUseGlobalText = globalStorage.label;
                    globalStorage.Text = "False";
                    EditorThemeManager.ApplySelectable(ostEditorUseGlobal, ThemeGroup.Function_2);
                    EditorThemeManager.ApplyGraphic(ostEditorUseGlobalText, ThemeGroup.Function_2_Text);

                    new LabelsElement("Edit Index").Init(labelInitSettings);

                    var text3 = textEditorPrefab.Duplicate(g1.transform, "index");
                    text3.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text3.gameObject.SetActive(true);

                    ostEditorIndex = text3.GetComponent<InputField>();
                    EditorThemeManager.ApplyInputField(ostEditorIndex);

                    new SpacerElement().Init(labelInitSettings);

                    new LabelsElement("Start Playing OST From Here").Init(labelInitSettings);

                    var play = EditorPrefabHolder.Instance.Function2Button.Duplicate(g1.transform);
                    var playStorage = play.GetComponent<FunctionButtonStorage>();
                    play.SetActive(true);
                    play.name = "play";
                    play.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                    play.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                    playStorage.Text = "Play";
                    ostEditorPlay = playStorage.button;
                    EditorThemeManager.ApplySelectable(ostEditorPlay, ThemeGroup.Function_2);
                    EditorThemeManager.ApplyGraphic(playStorage.label, ThemeGroup.Function_2_Text);

                    new LabelsElement("Stop Playing").Init(labelInitSettings);

                    var stop = EditorPrefabHolder.Instance.Function2Button.Duplicate(g1.transform);
                    var stopStorage = stop.GetComponent<FunctionButtonStorage>();
                    stop.SetActive(true);
                    stop.name = "stop";
                    stop.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                    stop.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                    stopStorage.Text = "Stop";
                    ostEditorStop = stopStorage.button;
                    EditorThemeManager.ApplySelectable(ostEditorStop, ThemeGroup.Function_2);
                    EditorThemeManager.ApplyGraphic(stopStorage.label, ThemeGroup.Function_2_Text);

                    new LabelsElement("Shuffle All OST").Init(labelInitSettings);

                    var shuffle = EditorPrefabHolder.Instance.Function2Button.Duplicate(g1.transform);
                    var shuffleStorage = shuffle.GetComponent<FunctionButtonStorage>();
                    shuffle.SetActive(true);
                    shuffle.name = "shuffle";
                    shuffle.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                    shuffle.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                    shuffleStorage.Text = "Shuffle";
                    ostEditorShuffle = shuffleStorage.button;
                    EditorThemeManager.ApplySelectable(ostEditorShuffle, ThemeGroup.Function_2);
                    EditorThemeManager.ApplyGraphic(shuffleStorage.label, ThemeGroup.Function_2_Text);

                    g1.SetActive(false);
                    editors.Add(g1);
                }
            }

            notificationsParent = EditorManager.inst.notification.Duplicate(plannerBase).transform.AsRT();
            CoreHelper.Delete(notificationsParent.TryGetChild(0));
            RectValues.BottomLeftAnchored.AnchoredPosition(8f, 8f).SizeDelta(221f, 632f).AssignToRectTransform(notificationsParent.AsRT());

            popupsParent = Creator.NewUIObject("Popups", plannerBase).transform;
            RectValues.Default.AnchoredPosition(-382.5f, 184.05f).SizeDelta(1155f, 648.1f).AssignToRectTransform(popupsParent.AsRT());

            RenderTabs();
            Load();
        }

        public override void OnTick()
        {
            foreach (var note in notes)
            {
                note.TopBar?.SetColor(note.TopColor);
                if (note.TitleUI)
                    note.TitleUI.color = RTColors.InvertColorHue(RTColors.InvertColorValue(note.TopColor));

                if (!Active || CurrentTab != PlannerBase.Type.Note)
                    note.GameObject?.SetActive(note.Active);

                note.ActiveUI?.gameObject?.SetActive(Active && CurrentTab == PlannerBase.Type.Note);

                var currentParent = !Active || CurrentTab != PlannerBase.Type.Note ? notesParent : content;

                if (note.GameObject && note.GameObject.transform.parent != (currentParent))
                {
                    note.GameObject.transform.SetParent(currentParent);
                    if (Active && CurrentTab == PlannerBase.Type.Note)
                        note.GameObject.transform.localScale = Vector3.one;
                }

                if (!note.Dragging && note.GameObject && (!Active || CurrentTab != PlannerBase.Type.Note))
                {
                    note.GameObject.transform.localPosition = note.Position;
                    note.GameObject.transform.localScale = note.Scale;
                    note.GameObject.transform.AsRT().sizeDelta = note.Size;
                }

                if (note.GameObject && note.GameObject.transform.Find("panel/edit"))
                {
                    note.GameObject.transform.Find("panel/edit").gameObject.SetActive(!Active || CurrentTab != PlannerBase.Type.Note);
                }
            }

            // handle OST

            if (!Active && EditorConfig.Instance.StopOSTOnExitPlanner.Value)
                return;

            if (!OSTAudioSource)
                return;

            if (!Active && SoundManager.inst.Playing)
            {
                switch (EditorConfig.Instance.InterruptOSTBehavior.Value)
                {
                    case InterruptOSTBehaviorType.LowerVolume: {
                            if (OSTAudioSource.isPlaying != !pausedOST)
                            {
                                if (pausedOST)
                                    OSTAudioSource.Pause();
                                else
                                    OSTAudioSource.UnPause();
                            }

                            OSTAudioSource.volume = SoundManager.inst.MusicVolume * (EditorConfig.Instance.LowerOSTVol.Value / 9f);
                            break;
                        }
                    case InterruptOSTBehaviorType.Pause: {
                            OSTAudioSource.Pause();
                            break;
                        }
                }
            }
            else
            {
                if (OSTAudioSource.isPlaying != !pausedOST)
                {
                    if (pausedOST)
                        OSTAudioSource.Pause();
                    else
                        OSTAudioSource.UnPause();
                }

                //if (!OSTAudioSource.isPlaying && !pausedOST)
                //    OSTAudioSource.UnPause();
                //else if (OSTAudioSource.isPlaying)
                //    OSTAudioSource.Pause();

                OSTAudioSource.volume = SoundManager.inst.MusicVolume * (EditorConfig.Instance.OSTVol.Value / 9f);
            }

            var list = osts;
            if (OSTAudioSource.clip && OSTAudioSource.time > OSTAudioSource.clip.length - 0.1f && playing)
            {
                if (EditorConfig.Instance.OSTLoop.Value == LoopOSTBehaviorType.LoopSingle)
                {
                    OSTAudioSource.time = 0f;
                    return;
                }

                NextOST();
            }
        }

        #endregion

        #region Create

        /// <summary>
        /// Creates a new Document planner.
        /// </summary>
        /// <param name="name">Name of the document.</param>
        /// <param name="text">Text of the document.</param>
        /// <param name="save">If documents should be saved.</param>
        /// <returns>Returns a new document.</returns>
        public DocumentPlanner CreateDocument(string name, string text, bool save = true)
        {
            var document = new DocumentPlanner();
            document.Name = name;
            document.Text = text;

            AddPlanner(document);
            if (save)
                SaveDocuments();
            if (EditorConfig.Instance.OpenNewPlanner.Value)
                OpenDocumentEditor(document);
            return document;
        }

        /// <summary>
        /// Creates a new TODO planner.
        /// </summary>
        /// <param name="check">Checked state of the TODO.</param>
        /// <param name="text">Text of the TODO</param>
        /// <param name="save">If TODOs should be saved.</param>
        /// <returns>Returns a new TODO.</returns>
        public TODOPlanner CreateTODO(bool check, string text, bool save = true)
        {
            var todo = new TODOPlanner();
            todo.Checked = check;
            todo.Text = text;

            AddPlanner(todo);
            if (save)
                SaveTODO();
            if (EditorConfig.Instance.OpenNewPlanner.Value)
                OpenTODOEditor(todo);
            return todo;
        }

        /// <summary>
        /// Creates a new Character planner.
        /// </summary>
        /// <param name="name">Name of the character.</param>
        /// <param name="save">If characters should be saved.</param>
        /// <returns>Returns a new character.</returns>
        public CharacterPlanner CreateCharacter(string name, bool save = true)
        {
            var character = new CharacterPlanner();

            for (int i = 0; i < 3; i++)
            {
                character.Traits.Add("???");
                character.Lore.Add("???");
                character.Abilities.Add("???");
            }

            character.Sprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"editor/example{FileFormat.PNG.Dot()}"));
            character.Name = name;
            character.Gender = "He";
            character.Description = "This is the default description";

            AddPlanner(character);
            if (save)
                SaveCharacters();
            if (EditorConfig.Instance.OpenNewPlanner.Value)
                OpenCharacterEditor(character);
            return character;
        }

        /// <summary>
        /// Creates a new Timeline planner.
        /// </summary>
        /// <param name="name">Name of the timeline.</param>
        /// <param name="events">Events list of the timeline.</param>
        /// <param name="save">If timelines should be saved.</param>
        /// <returns>Returns a new timeline.</returns>
        public TimelinePlanner CreateTimeline(string name, List<TimelinePlanner.Event> events, bool save = true)
        {
            var timeline = new TimelinePlanner();
            timeline.Name = name;
            timeline.Events = events;

            AddPlanner(timeline);
            if (save)
                SaveTimelines();
            if (EditorConfig.Instance.OpenNewPlanner.Value)
                OpenTimelineEditor(timeline);
            return timeline;
        }

        /// <summary>
        /// Creates a new Schedule planner.
        /// </summary>
        /// <param name="dateTime">Date time of the schedule.</param>
        /// <param name="description">Description of the schedule.</param>
        /// <param name="save">If schedules should be saved.</param>
        /// <returns>Returns a new schedule.</returns>
        public SchedulePlanner CreateSchedule(DateTime dateTime, string description, bool save = true)
        {
            var schedule = new SchedulePlanner();
            schedule.Date = dateTime.ToString("g");
            schedule.Description = description;

            AddPlanner(schedule);
            if (save)
                SaveSchedules();
            if (EditorConfig.Instance.OpenNewPlanner.Value)
                OpenScheduleEditor(schedule);
            return schedule;
        }

        /// <summary>
        /// Creates a new Note planner.
        /// </summary>
        /// <param name="active">Active state of the note.</param>
        /// <param name="name">Name of the note.</param>
        /// <param name="color">Color of the note.</param>
        /// <param name="position">Position of the note.</param>
        /// <param name="text">Text of the note.</param>
        /// <param name="save">If notes should be saved.</param>
        /// <returns>Returns a new note.</returns>
        public NotePlanner CreateNote(bool active, string name, int color, Vector2 position, string text, bool save = true)
        {
            var note = new NotePlanner();
            note.Active = active;
            note.Name = name;
            note.Color = color;
            note.Position = position;
            note.Text = text;

            AddPlanner(note);
            if (save)
                SaveNotes();
            if (EditorConfig.Instance.OpenNewPlanner.Value)
                OpenNoteEditor(note);
            return note;
        }

        /// <summary>
        /// Creates a new OST planner.
        /// </summary>
        /// <param name="name">Name of the OST.</param>
        /// <param name="path">Path to the OST file.</param>
        /// <param name="useGlobal">If a global path should be used.</param>
        /// <param name="save">If OSTs should be saved.</param>
        /// <returns>Returns a new OST.</returns>
        public OSTPlanner CreateOST(string name, string path, bool useGlobal, bool save = true)
        {
            var ost = new OSTPlanner();
            ost.Name = name;
            ost.Path = path;
            ost.UseGlobal = useGlobal;

            AddPlanner(ost);

            var list = osts.OrderBy(x => x.Index).ToList();

            ost.Index = list.Count - 1;

            if (save)
                SaveOST();
            if (EditorConfig.Instance.OpenNewPlanner.Value)
                OpenOSTEditor(ost);
            return ost;
        }

        #endregion

        #region Save / Load

        /// <summary>
        /// Adds a planner to the planners.
        /// </summary>
        /// <param name="item">Planner to add.</param>
        public void AddPlanner(PlannerBase item)
        {
            switch (item.PlannerType)
            {
                case PlannerBase.Type.Document: {
                        documents.Add(item as DocumentPlanner);
                        break;
                    }
                case PlannerBase.Type.TODO: {
                        todos.Add(item as TODOPlanner);
                        break;
                    }
                case PlannerBase.Type.Character: {
                        characters.Add(item as CharacterPlanner);
                        break;
                    }
                case PlannerBase.Type.Timeline: {
                        timelines.Add(item as TimelinePlanner);
                        break;
                    }
                case PlannerBase.Type.Schedule: {
                        schedules.Add(item as SchedulePlanner);
                        break;
                    }
                case PlannerBase.Type.Note: {
                        notes.Add(item as NotePlanner);
                        break;
                    }
                case PlannerBase.Type.OST: {
                        osts.Add(item as OSTPlanner);
                        break;
                    }
            }
            item.Init();
        }

        /// <summary>
        /// Clears all the planners.
        /// </summary>
        public void ClearPlanners()
        {
            documents.Clear();
            todos.Clear();
            characters.Clear();
            timelines.Clear();
            schedules.Clear();
            notes.Clear();
            osts.Clear();
        }

        /// <summary>
        /// Saves all the planners.
        /// </summary>
        public void Save()
        {
            EditorManager.inst.DisplayNotification("Saving Planners!", 1f, EditorManager.NotificationType.Warning);
            SaveDocuments();
            SaveTODO();
            SaveCharacters();
            SaveTimelines();
            SaveSchedules();
            SaveNotes();
            SaveOST();
            EditorManager.inst.DisplayNotification("Saved Planners!", 2f, EditorManager.NotificationType.Success);
        }

        /// <summary>
        /// Loads all the planners.
        /// </summary>
        public void Load()
        {
            ClearPlanners();
            LSHelpers.DeleteChildren(content);

            var path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PlannersPath);
            if (string.IsNullOrEmpty(Path.GetFileName(path)))
                return;

            RTFile.CreateDirectory(path);

            LoadDocuments();
            LoadTODO();
            LoadCharacters();
            LoadTimelines();
            LoadSchedules();
            LoadNotes();
            LoadOST();

            RefreshList();
        }

        /// <summary>
        /// Saves the document planners.
        /// </summary>
        public void SaveDocuments()
        {
            var path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PlannersPath);
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = Parser.NewJSONObject();

            var list = documents;

            for (int i = 0; i < list.Count; i++)
                jn["documents"][i] = list[i].ToJSON();

            RTFile.WriteToFile(RTFile.CombinePaths(path, $"documents{FileFormat.LSN.Dot()}"), jn.ToString(3));
        }

        /// <summary>
        /// Loads the document planners.
        /// </summary>
        public void LoadDocuments()
        {
            var path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PlannersPath, $"documents{FileFormat.LSN.Dot()}");
            if (!RTFile.FileExists(path))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            for (int i = 0; i < jn["documents"].Count; i++)
                AddPlanner(DocumentPlanner.Parse(jn["documents"][i]));
        }

        /// <summary>
        /// Saves the TODO planners.
        /// </summary>
        public void SaveTODO()
        {
            var path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PlannersPath);
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = Parser.NewJSONObject();

            var list = todos;

            for (int i = 0; i < list.Count; i++)
                jn["todo"][i] = list[i].ToJSON();

            RTFile.WriteToFile(RTFile.CombinePaths(path, $"todo{FileFormat.LSN.Dot()}"), jn.ToString(3));
        }

        /// <summary>
        /// Loads the TODO planners.
        /// </summary>
        public void LoadTODO()
        {
            var path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PlannersPath, $"todo{FileFormat.LSN.Dot()}");
            if (!RTFile.FileExists(path))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            for (int i = 0; i < jn["todo"].Count; i++)
                AddPlanner(TODOPlanner.Parse(jn["todo"][i]));
        }

        /// <summary>
        /// Saves the character planners.
        /// </summary>
        public void SaveCharacters()
        {
            var path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PlannersPath);
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = Parser.NewJSONObject();

            var list = characters;

            for (int i = 0; i < list.Count; i++)
                jn["characters"][i] = list[i].ToJSON();

            RTFile.WriteToFile(RTFile.CombinePaths(path, $"characters{FileFormat.LSN.Dot()}"), jn.ToString(3));
        }

        /// <summary>
        /// Loads the character planners.
        /// </summary>
        public void LoadCharacters()
        {
            var path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PlannersPath, $"characters{FileFormat.LSN.Dot()}");
            if (!RTFile.FileExists(path))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            for (int i = 0; i < jn["characters"].Count; i++)
                AddPlanner(CharacterPlanner.Parse(jn["characters"][i]));
        }

        /// <summary>
        /// Saves the timeline planners.
        /// </summary>
        public void SaveTimelines()
        {
            var path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PlannersPath);
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = Parser.NewJSONObject();

            var list = timelines;

            for (int i = 0; i < list.Count; i++)
                jn["timelines"][i] = list[i].ToJSON();

            RTFile.WriteToFile(RTFile.CombinePaths(path, $"timelines{FileFormat.LSN.Dot()}"), jn.ToString(3));
        }

        /// <summary>
        /// Loads the timeline planners.
        /// </summary>
        public void LoadTimelines()
        {
            var path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PlannersPath, $"timelines{FileFormat.LSN.Dot()}");
            if (!RTFile.FileExists(path))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            for (int i = 0; i < jn["timelines"].Count; i++)
                AddPlanner(TimelinePlanner.Parse(jn["timelines"][i]));
        }

        /// <summary>
        /// Saves the schedule planners.
        /// </summary>
        public void SaveSchedules()
        {
            var path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PlannersPath);
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = Parser.NewJSONObject();

            var list = schedules;

            for (int i = 0; i < list.Count; i++)
                jn["schedules"][i] = list[i].ToJSON();

            RTFile.WriteToFile(RTFile.CombinePaths(path, $"schedules{FileFormat.LSN.Dot()}"), jn.ToString(3));
        }

        /// <summary>
        /// Loads the schedule planners.
        /// </summary>
        public void LoadSchedules()
        {
            var path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PlannersPath, $"schedules{FileFormat.LSN.Dot()}");
            if (!RTFile.FileExists(path))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            for (int i = 0; i < jn["schedules"].Count; i++)
                AddPlanner(SchedulePlanner.Parse(jn["schedules"][i]));
        }

        /// <summary>
        /// Saves the note planners.
        /// </summary>
        public void SaveNotes()
        {
            var path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PlannersPath);
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = Parser.NewJSONObject();

            var list = notes;

            for (int i = 0; i < list.Count; i++)
                jn["notes"][i] = list[i].ToJSON();

            RTFile.WriteToFile(RTFile.CombinePaths(path, $"notes{FileFormat.LSN.Dot()}"), jn.ToString(3));
        }

        /// <summary>
        /// Loads the note planners.
        /// </summary>
        public void LoadNotes()
        {
            var path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PlannersPath, $"notes{FileFormat.LSN.Dot()}");
            if (!RTFile.FileExists(path))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            for (int i = 0; i < jn["notes"].Count; i++)
                AddPlanner(NotePlanner.Parse(jn["notes"][i]));
        }

        /// <summary>
        /// Saves the OST planners.
        /// </summary>
        public void SaveOST()
        {
            var path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PlannersPath);
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = Parser.NewJSONObject();

            var list = osts;

            for (int i = 0; i < list.Count; i++)
                jn["ost"][i] = list[i].ToJSON();

            RTFile.WriteToFile(RTFile.CombinePaths(path, $"ost{FileFormat.LSN.Dot()}"), jn.ToString(3));
        }

        /// <summary>
        /// Loads the OST planners.
        /// </summary>
        public void LoadOST()
        {
            var path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PlannersPath, $"ost{FileFormat.LSN.Dot()}");
            if (!RTFile.FileExists(path))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            for (int i = 0; i < jn["ost"].Count; i++)
                AddPlanner(OSTPlanner.Parse(jn["ost"][i]));
        }

        #endregion

        #region Render UI

        /// <summary>
        /// Sets the current planner tab.
        /// </summary>
        /// <param name="tab">Tab to set.</param>
        public void OpenTab(PlannerBase.Type tab)
        {
            CurrentTab = tab;
            Open();
            RenderTabs();
            RefreshList();
        }

        /// <summary>
        /// Renders the planner tabs.
        /// </summary>
        public void RenderTabs()
        {
            contentLayout.cellSize = tabCellSizes[(int)CurrentTab];
            contentLayout.constraintCount = tabConstraintCounts[(int)CurrentTab];
            documentFullView.SetActive(false);
            characterFullView.SetActive(false);
            int num = 0;
            foreach (var tab in tabs)
            {
                int index = num;

                tab.SetIsOnWithoutNotify(index == (int)CurrentTab);
                tab.onValueChanged.NewListener(_val =>
                {
                    CurrentTab = (PlannerBase.Type)index;
                    RenderTabs();
                    RefreshList();
                });

                num++;
            }
            SetEditorsInactive();
        }

        /// <summary>
        /// Refreshes the planner list.
        /// </summary>
        public void RefreshList()
        {
            for (int i = 0; i < activeTabPlannerItems.Count; i++)
            {
                var planner = activeTabPlannerItems[i];
                if (planner && planner.GameObject)
                    planner.GameObject.SetActive(false);
            }
            activeTabPlannerItems.Clear();
            switch (CurrentTab)
            {
                case PlannerBase.Type.Document: {
                        for (int i = 0; i < documents.Count; i++)
                        {
                            var planner = documents[i];
                            if (!planner)
                                continue;
                            activeTabPlannerItems.Add(planner);
                            if (planner.GameObject)
                                planner.GameObject.SetActive(planner.PlannerType == CurrentTab && RTString.SearchString(SearchTerm, planner.Name));
                        }
                        break;
                    }
                case PlannerBase.Type.TODO: {
                        for (int i = 0; i < todos.Count; i++)
                        {
                            var planner = todos[i];
                            if (!planner)
                                continue;
                            activeTabPlannerItems.Add(planner);
                            if (planner.GameObject)
                                planner.GameObject.SetActive(planner.PlannerType == CurrentTab && (string.IsNullOrEmpty(SearchTerm) || CheckOn(SearchTerm.ToLower()) && planner.Checked || CheckOff(SearchTerm.ToLower()) && !planner.Checked || RTString.SearchString(SearchTerm, planner.Text)));
                        }
                        break;
                    }
                case PlannerBase.Type.Character: {
                        for (int i = 0; i < characters.Count; i++)
                        {
                            var planner = characters[i];
                            if (!planner)
                                continue;
                            activeTabPlannerItems.Add(planner);
                            if (planner.GameObject)
                                planner.GameObject.SetActive(planner.PlannerType == CurrentTab && RTString.SearchString(SearchTerm, planner.Name, planner.Description));
                        }
                        break;
                    }
                case PlannerBase.Type.Timeline: {
                        for (int i = 0; i < timelines.Count; i++)
                        {
                            var planner = timelines[i];
                            if (!planner)
                                continue;
                            activeTabPlannerItems.Add(planner);
                            if (planner.GameObject)
                                planner.GameObject.SetActive(planner.PlannerType == CurrentTab && planner.Events.Has(x => RTString.SearchString(SearchTerm, x.Name)));
                        }
                        break;
                    }
                case PlannerBase.Type.Schedule: {
                        for (int i = 0; i < schedules.Count; i++)
                        {
                            var planner = schedules[i];
                            if (!planner)
                                continue;
                            activeTabPlannerItems.Add(planner);
                            if (planner.GameObject)
                                planner.GameObject.SetActive(planner.PlannerType == CurrentTab && RTString.SearchString(SearchTerm, planner.Description, planner.Date));
                        }
                        break;
                    }
                case PlannerBase.Type.Note: {
                        for (int i = 0; i < notes.Count; i++)
                        {
                            var planner = notes[i];
                            if (!planner)
                                continue;
                            activeTabPlannerItems.Add(planner);
                            if (planner.GameObject)
                                planner.GameObject.SetActive(planner.PlannerType == CurrentTab && RTString.SearchString(SearchTerm, planner.Name, planner.Text));
                        }
                        break;
                    }
                case PlannerBase.Type.OST: {
                        for (int i = 0; i < osts.Count; i++)
                        {
                            var planner = osts[i];
                            if (!planner)
                                continue;
                            activeTabPlannerItems.Add(planner);
                            if (planner.GameObject)
                                planner.GameObject.SetActive(planner.PlannerType == CurrentTab && RTString.SearchString(SearchTerm, planner.Name));
                        }
                        break;
                    }
            }
        }

        bool CheckOn(string searchTerm)
            => searchTerm == "\"true\"" || searchTerm == "\"on\"" || searchTerm == "\"done\"" || searchTerm == "\"finished\"" || searchTerm == "\"checked\"";

        bool CheckOff(string searchTerm)
            => searchTerm == "\"false\"" || searchTerm == "\"off\"" || searchTerm == "\"not done\"" || searchTerm == "\"not finished\"" || searchTerm == "\"unfinished\"" || searchTerm == "\"unchecked\"";

        /// <summary>
        /// Hides the editors.
        /// </summary>
        public void SetEditorsInactive()
        {
            for (int i = 0; i < editors.Count; i++)
                editors[i].SetActive(false);
            editorTitlePanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// Opens the Document editor.
        /// </summary>
        /// <param name="document">Document planner to edit.</param>
        public void OpenDocumentEditor(DocumentPlanner document)
        {
            currentDocumentPlanner = document;
            editors[0].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_1];

            RenderDocumentEditorName(document);

            HandleDocumentEditor(document);
            HandleDocumentEditorDisplay(document);
        }

        /// <summary>
        /// Renders the Document editor name.
        /// </summary>
        /// <param name="document">Document planner to edit.</param>
        public void RenderDocumentEditorName(DocumentPlanner document)
        {
            var origName = document.Name;
            documentEditorName.SetTextWithoutNotify(document.Name);
            documentEditorName.onValueChanged.NewListener(_val =>
            {
                document.Name = _val;
                document.NameUI.text = _val;
                documentTitle.text = _val;
            });
            documentEditorName.onEndEdit.NewListener(_val =>
            {
                if (!documents.Has(x => x.Name == _val))
                {
                    SaveDocuments();
                    return;
                }

                document.Name = origName;
                document.NameUI.text = origName;
                documentTitle.text = origName;
                RenderDocumentEditorName(document);
            });
        }

        void HandleDocumentEditor(DocumentPlanner document)
        {
            documentEditorText.SetTextWithoutNotify(document.Text);
            documentEditorText.onValueChanged.NewListener(_val =>
            {
                document.Text = _val;
                document.TextUI.text = _val;
                SetupPlannerLinks(document.Text, document.TextUI, null, false);

                HandleDocumentEditorDisplay(document);
            });
            documentEditorText.onEndEdit.NewListener(_val => SaveDocuments());
        }

        void HandleDocumentEditorDisplay(DocumentPlanner document)
        {
            documentFullView.SetActive(true);
            documentTitle.text = document.Name;
            documentInputField.SetTextWithoutNotify(document.Text);
            documentInputField.onValueChanged.NewListener(_val =>
            {
                document.Text = _val;
                document.TextUI.text = _val;
                SetupPlannerLinks(document.Text, document.TextUI, null, false);

                HandleDocumentEditor(document);
            });
            documentInputField.onEndEdit.NewListener(_val =>
            {
                SaveDocuments();
                SetupPlannerLinks(document.Text, documentInputField, documentHyperlinks);
            });
            SetupPlannerLinks(document.Text, documentInputField, documentHyperlinks);
        }

        /// <summary>
        /// Opens the TODO editor.
        /// </summary>
        /// <param name="todo">TODO planner to edit.</param>
        public void OpenTODOEditor(TODOPlanner todo)
        {
            currentTODOPlanner = todo;
            editors[1].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_2];

            RenderTODOEditor(todo);
        }

        /// <summary>
        /// Renders the TODO editor.
        /// </summary>
        /// <param name="todo">TODO planner to edit.</param>
        public void RenderTODOEditor(TODOPlanner todo)
        {
            var origText = todo.Text;
            todoEditorText.SetTextWithoutNotify(todo.Text);
            todoEditorText.onValueChanged.NewListener(_val =>
            {
                todo.Text = _val;
                todo.TextUI.text = _val;
                SetupPlannerLinks(todo.Text, todo.TextUI, todo.Hyperlinks);
            });
            todoEditorText.onEndEdit.NewListener(_val =>
            {
                if (!todos.Has(x => x.Text == _val))
                {
                    SaveTODO();
                    return;
                }

                todo.Text = origText;
                todo.TextUI.text = origText;
                SetupPlannerLinks(todo.Text, todo.TextUI, todo.Hyperlinks);
                RenderTODOEditor(todo);
            });
            todoEditorMoveUpButton.onClick.NewListener(() =>
            {
                if (!todos.TryFindIndex(x => x.ID == todo.ID, out int index))
                    return;

                if (index - 1 < 0)
                    return;

                todos.Move(index, index - 1);
                SaveTODO();

                foreach (var todo in todos)
                    todo.Init();
                RefreshList();
            });
            todoEditorMoveDownButton.onClick.NewListener(() =>
            {
                if (!todos.TryFindIndex(x => x.ID == todo.ID, out int index))
                    return;

                if (index >= todos.Count - 1)
                    return;

                todos.Move(index, index + 1);
                SaveTODO();

                foreach (var todo in todos)
                    todo.Init();
                RefreshList();
            });
        }

        /// <summary>
        /// Opens the Character editor.
        /// </summary>
        /// <param name="character">Character planner to edit.</param>
        public void OpenCharacterEditor(CharacterPlanner character)
        {
            currentCharacterPlanner = character;
            editors[2].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_3];

            RenderCharacterEditor(character);
        }

        /// <summary>
        /// Renders the Character editor.
        /// </summary>
        /// <param name="character">Character planner to edit.</param>
        public void RenderCharacterEditor(CharacterPlanner character)
        {
            RenderCharacterEditorDisplay(character);

            characterEditorName.SetTextWithoutNotify(character.Name);
            characterEditorName.onValueChanged.NewListener(_val =>
            {
                character.Name = _val;
                character.DetailsUI.text = character.FormatDetails;
                SetupPlannerLinks(character.DetailsUI.text, character.DetailsUI, character.DetailsHyperlinks);

                RenderCharacterEditorDisplay(character);
            });
            characterEditorName.onEndEdit.NewListener(_val => SaveCharacters());

            characterEditorGender.SetTextWithoutNotify(character.Gender);
            characterEditorGender.onValueChanged.NewListener(_val =>
            {
                character.Gender = _val;
                character.DetailsUI.text = character.FormatDetails;
                SetupPlannerLinks(character.DetailsUI.text, character.DetailsUI, character.DetailsHyperlinks);

                RenderCharacterEditorDisplay(character);
            });
            characterEditorGender.onEndEdit.NewListener(_val => SaveCharacters());

            characterEditorOrigin.SetTextWithoutNotify(character.Origin);
            characterEditorOrigin.onValueChanged.NewListener(_val =>
            {
                character.Origin = _val;
                character.DetailsUI.text = character.FormatDetails;
                SetupPlannerLinks(character.DetailsUI.text, character.DetailsUI, character.DetailsHyperlinks);

                RenderCharacterEditorDisplay(character);
            });
            characterEditorOrigin.onEndEdit.NewListener(_val => SaveCharacters());

            characterEditorDescription.SetTextWithoutNotify(character.Description);
            characterEditorDescription.onValueChanged.NewListener(_val =>
            {
                character.Description = _val;
                character.DescriptionUI.text = character.Description;
                SetupPlannerLinks(character.Description, character.DescriptionUI, character.DescriptionHyperlinks);

                RenderCharacterEditorDisplay(character);
            });
            characterEditorDescription.onEndEdit.NewListener(_val => SaveCharacters());

            RenderCharacterEditorTraits(character);
            RenderCharacterEditorLore(character);
            RenderCharacterEditorAbilities(character);
        }

        /// <summary>
        /// Renders the Character editor display area.
        /// </summary>
        /// <param name="character">Character planner to edit.</param>
        public void RenderCharacterEditorDisplay(CharacterPlanner character)
        {
            characterFullView.SetActive(true);
            characterDetails.text = character.FormatDetails;
            characterDescriptionInputField.SetTextWithoutNotify(character.Description);
            characterDescriptionInputField.onValueChanged.NewListener(_val =>
            {
                character.Description = _val;
                character.DescriptionUI.text = character.Description;
                SetupPlannerLinks(character.Description, character.DescriptionUI, character.DescriptionHyperlinks);

                RenderCharacterEditor(character);
            });
            characterDescriptionInputField.onEndEdit.NewListener(_val =>
            {
                SaveCharacters();
                SetupPlannerLinks(character.Description, character.DescriptionUI, character.DescriptionHyperlinks);
            });
            characterSprite.sprite = character.Sprite;

            CoreHelper.DestroyChildren(characterAttributesContent);

            new LabelElement("Traits") { fontStyle = FontStyle.Bold }.Init(EditorElement.InitSettings.Default.Parent(characterAttributesContent));
            for (int i = 0; i < character.Traits.Count; i++)
            {
                var g = tmpTextPrefab.Duplicate(characterAttributesContent, "trait");
                var text = g.GetComponent<TextMeshProUGUI>();

                text.text = character.Traits[i];
                text.alignment = TextAlignmentOptions.Left;

                EditorThemeManager.ApplyLightText(text);
            }
            
            new LabelElement("Lore") { fontStyle = FontStyle.Bold }.Init(EditorElement.InitSettings.Default.Parent(characterAttributesContent));
            for (int i = 0; i < character.Lore.Count; i++)
            {
                var g = tmpTextPrefab.Duplicate(characterAttributesContent, "lore");
                var text = g.GetComponent<TextMeshProUGUI>();

                text.text = character.Lore[i];
                text.alignment = TextAlignmentOptions.Left;

                EditorThemeManager.ApplyLightText(text);
            }
            
            new LabelElement("Abilities") { fontStyle = FontStyle.Bold }.Init(EditorElement.InitSettings.Default.Parent(characterAttributesContent));
            for (int i = 0; i < character.Abilities.Count; i++)
            {
                var g = tmpTextPrefab.Duplicate(characterAttributesContent, "abilities");
                var text = g.GetComponent<TextMeshProUGUI>();

                text.text = character.Abilities[i];
                text.alignment = TextAlignmentOptions.Left;

                EditorThemeManager.ApplyLightText(text);
            }

            SetupPlannerLinks(characterDetails.text, characterDetails, characterDetailsHyperlinks);
            SetupPlannerLinks(character.Description, characterDescriptionInputField, characterDescriptionHyperlinks);
        }

        /// <summary>
        /// Renders the Character editor traits.
        /// </summary>
        /// <param name="character">Character planner to edit.</param>
        public void RenderCharacterEditorTraits(CharacterPlanner character)
        {
            LSHelpers.DeleteChildren(characterEditorTraitsContent);

            int num = 0;
            foreach (var tag in character.Traits)
            {
                int index = num;
                var gameObject = tagPrefab.Duplicate(characterEditorTraitsContent, index.ToString());
                gameObject.transform.localScale = Vector3.one;
                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.SetTextWithoutNotify(tag);
                input.onValueChanged.NewListener(_val =>
                {
                    character.Traits[index] = _val;

                    RenderCharacterEditorDisplay(character);
                });
                input.onEndEdit.NewListener(_val => SaveCharacters());

                var delete = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                delete.OnClick.NewListener(() =>
                {
                    character.Traits.RemoveAt(index);
                    SaveCharacters();
                    RenderCharacterEditorTraits(character);

                    RenderCharacterEditorDisplay(character);
                });

                EditorThemeManager.ApplyInputField(input);
                EditorThemeManager.ApplyDeleteButton(delete);

                num++;
            }

            var add = EditorPrefabHolder.Instance.CreateAddButton(characterEditorTraitsContent);
            add.Text = "Add Trait";
            add.OnClick.NewListener(() =>
            {
                character.Traits.Add("New Detail");
                SaveCharacters();
                RenderCharacterEditorTraits(character);

                RenderCharacterEditorDisplay(character);
            });
        }

        /// <summary>
        /// Renders the Character editor lore.
        /// </summary>
        /// <param name="character">Character planner to edit.</param>
        public void RenderCharacterEditorLore(CharacterPlanner character)
        {
            LSHelpers.DeleteChildren(characterEditorLoreContent);

            int num = 0;
            foreach (var tag in character.Lore)
            {
                int index = num;
                var gameObject = tagPrefab.Duplicate(characterEditorLoreContent, index.ToString());
                gameObject.transform.localScale = Vector3.one;
                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.SetTextWithoutNotify(tag);
                input.onValueChanged.NewListener(_val =>
                {
                    character.Lore[index] = _val;

                    RenderCharacterEditorDisplay(character);
                });
                input.onEndEdit.NewListener(_val => SaveCharacters());

                var delete = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                delete.OnClick.NewListener(() =>
                {
                    character.Lore.RemoveAt(index);
                    SaveCharacters();
                    RenderCharacterEditorLore(character);

                    RenderCharacterEditorDisplay(character);
                });

                EditorThemeManager.ApplyInputField(input);
                EditorThemeManager.ApplyDeleteButton(delete);

                num++;
            }

            var add = EditorPrefabHolder.Instance.CreateAddButton(characterEditorLoreContent);
            add.Text = "Add Lore";
            add.OnClick.NewListener(() =>
            {
                character.Lore.Add("New Detail");
                SaveCharacters();
                RenderCharacterEditorLore(character);

                RenderCharacterEditorDisplay(character);
            });
        }

        /// <summary>
        /// Renders the Character editor abilities.
        /// </summary>
        /// <param name="character">Character planner to edit.</param>
        public void RenderCharacterEditorAbilities(CharacterPlanner character)
        {
            LSHelpers.DeleteChildren(characterEditorAbilitiesContent);

            int num = 0;
            foreach (var tag in character.Abilities)
            {
                int index = num;
                var gameObject = tagPrefab.Duplicate(characterEditorAbilitiesContent, index.ToString());
                gameObject.transform.localScale = Vector3.one;
                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.SetTextWithoutNotify(tag);
                input.onValueChanged.NewListener(_val =>
                {
                    character.Abilities[index] = _val;

                    RenderCharacterEditorDisplay(character);
                });
                input.onEndEdit.NewListener(_val => SaveCharacters());

                var delete = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                delete.OnClick.NewListener(() =>
                {
                    character.Abilities.RemoveAt(index);
                    SaveCharacters();
                    RenderCharacterEditorAbilities(character);

                    RenderCharacterEditorDisplay(character);
                });

                EditorThemeManager.ApplyInputField(input);
                EditorThemeManager.ApplyDeleteButton(delete);

                num++;
            }

            var add = EditorPrefabHolder.Instance.CreateAddButton(characterEditorAbilitiesContent);
            add.Text = "Add Ability";
            add.OnClick.NewListener(() =>
            {
                character.Abilities.Add("New Detail");
                SaveCharacters();
                RenderCharacterEditorAbilities(character);

                RenderCharacterEditorDisplay(character);
            });
        }

        /// <summary>
        /// Opens the Timeline editor.
        /// </summary>
        /// <param name="timeline">Timeline planner to edit.</param>
        public void OpenTimelineEditor(TimelinePlanner timeline)
        {
            currentTimelinePlanner = timeline;
            SetEditorsInactive();
            editors[3].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_4];

            RenderTimelineEditor(timeline);
        }

        /// <summary>
        /// Renders the Timeline editor.
        /// </summary>
        /// <param name="timeline">Timeline planner to edit.</param>
        public void RenderTimelineEditor(TimelinePlanner timeline)
        {
            var origName = timeline.Name;
            timelineEditorName.SetTextWithoutNotify(timeline.Name);
            timelineEditorName.onValueChanged.NewListener(_val =>
            {
                timeline.Name = _val;
                timeline.NameUI.text = _val;
            });
            timelineEditorName.onEndEdit.NewListener(_val =>
            {
                if (!timelines.Has(x => x.Name == _val))
                {
                    SaveTimelines();
                    return;
                }

                timeline.Name = origName;
                timeline.NameUI.text = origName;
                RenderTimelineEditor(timeline);
            });
        }

        /// <summary>
        /// Opens the Event editor.
        /// </summary>
        /// <param name="_event">Event planner to edit.</param>
        public void OpenEventEditor(TimelinePlanner.Event _event)
        {
            SetEditorsInactive();
            editors[4].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_4];

            RenderEventEditor(_event);
        }

        /// <summary>
        /// Renders the Event editor.
        /// </summary>
        /// <param name="_event">Event planner to edit.</param>
        public void RenderEventEditor(TimelinePlanner.Event _event)
        {
            eventEditorName.SetTextWithoutNotify(_event.Name);
            eventEditorName.onValueChanged.NewListener(_val =>
            {
                _event.Name = _val;
                _event.NameUI.text = $"{_event.EventType}: {_event.Name}";
            });
            eventEditorName.onEndEdit.NewListener(_val => SaveTimelines());

            eventEditorDescription.SetTextWithoutNotify(_event.Description);
            eventEditorDescription.onValueChanged.NewListener(_val =>
            {
                _event.Description = _val;
                _event.DescriptionUI.text = _val;
                SetupPlannerLinks(_val, _event.DescriptionUI, _event.Hyperlinks);
            });
            eventEditorDescription.onEndEdit.NewListener(_val => SaveTimelines());

            eventEditorPath.SetTextWithoutNotify(_event.Path ?? string.Empty);
            eventEditorPath.onValueChanged.NewListener(_val => _event.Path = _val);
            eventEditorPath.onEndEdit.NewListener(_val => SaveTimelines());

            eventEditorType.SetValueWithoutNotify((int)_event.EventType);
            eventEditorType.onValueChanged.NewListener(_val =>
            {
                _event.EventType = (TimelinePlanner.Event.Type)_val;
                _event.NameUI.text = $"{_event.EventType}: {_event.Name}";
                SaveTimelines();
            });
        }

        /// <summary>
        /// Opens the Schedule editor.
        /// </summary>
        /// <param name="schedule">Schedule planner to edit.</param>
        public void OpenScheduleEditor(SchedulePlanner schedule)
        {
            currentSchedulePlanner = schedule;
            editors[5].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_5];

            RenderScheduleEditor(schedule);
        }

        /// <summary>
        /// Renders the Schedule editor.
        /// </summary>
        /// <param name="schedule">Schedule planner to edit.</param>
        public void RenderScheduleEditor(SchedulePlanner schedule)
        {
            scheduleEditorDescription.SetTextWithoutNotify(schedule.Description);
            scheduleEditorDescription.onValueChanged.NewListener(_val =>
            {
                schedule.Description = _val;
                schedule.TextUI.text = schedule.Text;
                schedule.hasBeenChecked = false;
                SetupPlannerLinks(schedule.Text, schedule.TextUI, schedule.Hyperlinks);
            });
            scheduleEditorDescription.onEndEdit.NewListener(_val => SaveSchedules());

            scheduleEditorYear.SetTextWithoutNotify(schedule.DateTime.Year.ToString());
            scheduleEditorYear.onValueChanged.NewListener(_val =>
            {
                if (!int.TryParse(_val, out int year))
                    return;

                var dateTime = schedule.DateTime;

                schedule.DateTime = DateTime.Parse(schedule.FormatDate(dateTime.Day, dateTime.Month, year, dateTime.Hour >= 12 ? dateTime.Hour - 12 : dateTime.Hour, dateTime.Minute, dateTime.Hour >= 12 && dateTime.Hour < 24 ? "PM" : "AM"));

                schedule.Date = schedule.DateTime.ToString("g");
                schedule.TextUI.text = schedule.Text;
                schedule.hasBeenChecked = false;

                SaveSchedules();
            });

            scheduleEditorMonth.SetValueWithoutNotify(schedule.DateTime.Month - 1);
            scheduleEditorMonth.onValueChanged.NewListener(_val =>
            {
                var dateTime = schedule.DateTime;

                schedule.DateTime = DateTime.Parse(schedule.FormatDate(dateTime.Day, _val + 1, dateTime.Year, dateTime.Hour >= 12 ? dateTime.Hour - 12 : dateTime.Hour, dateTime.Minute, dateTime.Hour >= 12 && dateTime.Hour < 24 ? "PM" : "AM"));

                schedule.Date = schedule.DateTime.ToString("g");
                schedule.TextUI.text = schedule.Text;
                schedule.hasBeenChecked = false;

                SaveSchedules();
            });

            scheduleEditorDay.SetTextWithoutNotify(schedule.DateTime.Day.ToString());
            scheduleEditorDay.onValueChanged.NewListener(_val =>
            {
                if (!int.TryParse(_val, out int day))
                    return;

                var dateTime = schedule.DateTime;

                schedule.DateTime = DateTime.Parse(schedule.FormatDate(day, dateTime.Month, dateTime.Year, dateTime.Hour >= 12 ? dateTime.Hour - 12 : dateTime.Hour, dateTime.Minute, dateTime.Hour >= 12 && dateTime.Hour < 24 ? "PM" : "AM"));

                schedule.Date = schedule.DateTime.ToString("g");
                schedule.TextUI.text = schedule.Text;
                schedule.hasBeenChecked = false;

                SaveSchedules();
            });

            scheduleEditorHour.SetTextWithoutNotify(schedule.DateTime.Hour.ToString());
            scheduleEditorHour.onValueChanged.NewListener(_val =>
            {
                if (!int.TryParse(_val, out int hour))
                    return;

                var dateTime = schedule.DateTime;

                hour = Mathf.Clamp(hour, 0, 23);

                schedule.DateTime = DateTime.Parse(schedule.FormatDate(dateTime.Day, dateTime.Month, dateTime.Year, hour >= 12 ? hour - 12 : hour, dateTime.Minute, hour >= 12 && hour < 24 ? "PM" : "AM"));

                schedule.Date = schedule.DateTime.ToString("g");
                schedule.TextUI.text = schedule.Text;
                schedule.hasBeenChecked = false;

                SaveSchedules();
            });

            scheduleEditorMinute.SetTextWithoutNotify(schedule.DateTime.Minute.ToString());
            scheduleEditorMinute.onValueChanged.NewListener(_val =>
            {
                if (!int.TryParse(_val, out int minute))
                    return;

                var dateTime = schedule.DateTime;

                schedule.DateTime = DateTime.Parse(schedule.FormatDate(dateTime.Day, dateTime.Month, dateTime.Year, dateTime.Hour >= 12 ? dateTime.Hour - 12 : dateTime.Hour, minute, dateTime.Hour >= 12 && dateTime.Hour < 24 ? "PM" : "AM"));

                schedule.Date = schedule.DateTime.ToString("g");
                schedule.TextUI.text = schedule.Text;
                schedule.hasBeenChecked = false;

                SaveSchedules();
            });
        }

        /// <summary>
        /// Opens the Note editor.
        /// </summary>
        /// <param name="note">Note planner to edit.</param>
        public void OpenNoteEditor(NotePlanner note)
        {
            currentNotePlanner = note;
            editors[6].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_6];

            RenderNoteEditor(note);
        }

        /// <summary>
        /// Renders the Note editor.
        /// </summary>
        /// <param name="note">Note planner to edit.</param>
        public void RenderNoteEditor(NotePlanner note)
        {
            var origName = note.Name;
            noteEditorName.SetTextWithoutNotify(note.Name);
            noteEditorName.onValueChanged.NewListener(_val =>
            {
                note.Name = _val;
                note.TitleUI.text = $"Note - {note.Name}";
            });
            noteEditorName.onEndEdit.NewListener(_val =>
            {
                if (!notes.Has(x => x.Name == _val))
                {
                    SaveNotes();
                    return;
                }

                note.Name = _val;
                note.TitleUI.text = $"Note - {note.Name}";
                RenderNoteEditor(note);
            });

            noteEditorText.SetTextWithoutNotify(note.Text);
            noteEditorText.onValueChanged.NewListener(_val =>
            {
                note.Text = _val;
                note.TextUI.text = _val;
                SetupPlannerLinks(note.Text, note.TextUI, note.Hyperlinks);
            });
            noteEditorText.onEndEdit.NewListener(_val => SaveNotes());

            RenderNoteEditorColors(note);

            noteEditorReset.onClick.NewListener(() =>
            {
                note.ResetTransform();
                SaveNotes();
            });
        }

        /// <summary>
        /// Renders the Note editor colors.
        /// </summary>
        /// <param name="note">Note planner to edit.</param>
        public void RenderNoteEditorColors(NotePlanner note)
        {
            noteEditorColors.Clear();
            LSHelpers.DeleteChildren(noteEditorColorsParent);
            for (int i = 0; i < MarkerEditor.inst.markerColors.Count; i++)
            {
                var col = colorBase.Find("1").gameObject.Duplicate(noteEditorColorsParent, (i + 1).ToString());
                col.transform.localScale = Vector3.one;
                noteEditorColors.Add(col.GetComponent<Toggle>());
            }

            SetNoteColors(note);
        }

        /// <summary>
        /// Updates the Note editor colors.
        /// </summary>
        /// <param name="note">Note planner to render and edit the colors for.</param>
        public void SetNoteColors(NotePlanner note)
        {
            int num = 0;
            foreach (var toggle in noteEditorColors)
            {
                int index = num;

                var color = index < MarkerEditor.inst.markerColors.Count ? MarkerEditor.inst.markerColors[index] : RTColors.errorColor;
                toggle.image.color = color;
                toggle.graphic.color = new Color(0.078f, 0.067f, 0.067f, 1f);
                toggle.SetIsOnWithoutNotify(index == note.Color);
                toggle.onValueChanged.NewListener(_val =>
                {
                    note.Color = index;
                    SetNoteColors(note);
                });

                num++;
            }
        }

        /// <summary>
        /// Opens the OST editor.
        /// </summary>
        /// <param name="ost">OST planner to edit.</param>
        public void OpenOSTEditor(OSTPlanner ost)
        {
            currentOSTPlanner = ost;
            editors[7].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_7];

            RenderOSTEditor(ost);
        }

        /// <summary>
        /// Renders the OST editor.
        /// </summary>
        /// <param name="ost">OST planner to edit.</param>
        public void RenderOSTEditor(OSTPlanner ost)
        {
            ostEditorPath.SetTextWithoutNotify(ost.Path);
            ostEditorPath.onValueChanged.NewListener(_val => { ost.Path = _val; });
            ostEditorPath.onEndEdit.NewListener(_val => SaveOST());

            var origName = ost.Name;
            ostEditorName.SetTextWithoutNotify(ost.Name);
            ostEditorName.onValueChanged.NewListener(_val =>
            {
                ost.Name = _val;
                ost.TextUI.text = _val;
                SetupPlannerLinks(ost.Name, ost.TextUI, ost.Hyperlinks);
            });
            ostEditorName.onEndEdit.NewListener(_val =>
            {
                if (!osts.Has(x => x.Name == _val))
                {
                    SaveOST();
                    return;
                }

                ost.Name = origName;
                ost.TextUI.text = origName;
                SetupPlannerLinks(ost.Name, ost.TextUI, ost.Hyperlinks);
                RenderOSTEditor(ost);
            });
            ostEditorPlay.onClick.NewListener(ost.Play);
            ostEditorStop.onClick.NewListener(StopOST);
            ostEditorShuffle.onClick.NewListener(ShuffleOST);
            ostEditorUseGlobal.onClick.NewListener(() =>
            {
                ost.UseGlobal = !ost.UseGlobal;
                ostEditorUseGlobalText.text = ost.UseGlobal.ToString();
                SaveOST();
            });

            ostEditorUseGlobalText.text = ost.UseGlobal.ToString();

            ostEditorIndex.SetTextWithoutNotify(ost.Index.ToString());
            ostEditorIndex.onValueChanged.NewListener(_val =>
            {
                if (!int.TryParse(_val, out int num))
                    return;

                ost.Index = num;
                UpdateOSTIndexes();
                RenderOSTEditor(ost);
                SaveOST();
            });
            TriggerHelper.AddEventTriggers(ostEditorIndex.gameObject, TriggerHelper.ScrollDeltaInt(ostEditorIndex));
        }

        #endregion

        #region Open / Close UI

        /// <summary>
        /// If the planner is active.
        /// </summary>
        public bool Active => EditorManager.inst.editorState == EditorManager.EditorState.Intro;

        /// <summary>
        /// Opens the planner.
        /// </summary>
        public void Open()
        {
            EditorManager.inst.editorState = EditorManager.EditorState.Intro;
            UpdateStateUI();
        }

        /// <summary>
        /// Closes the planner.
        /// </summary>
        public void Close()
        {
            EditorManager.inst.editorState = EditorManager.EditorState.Main;
            UpdateStateUI();
        }

        /// <summary>
        /// Toggles the planner state.
        /// </summary>
        public void ToggleState() => SetState(EditorManager.inst.editorState == EditorManager.EditorState.Main ? EditorManager.EditorState.Intro : EditorManager.EditorState.Main);

        /// <summary>
        /// Sets the planner state.
        /// </summary>
        /// <param name="editorState">Editor state.</param>
        public void SetState(EditorManager.EditorState editorState)
        {
            EditorManager.inst.editorState = editorState;
            UpdateStateUI();
        }

        /// <summary>
        /// Updates the state of the editor.
        /// </summary>
        public void UpdateStateUI()
        {
            var editorState = EditorManager.inst.editorState;
            EditorManager.inst.GUIMain.SetActive(editorState == EditorManager.EditorState.Main);
            EditorManager.inst.GUIIntro.SetActive(editorState == EditorManager.EditorState.Intro);

            if (editorState == EditorManager.EditorState.Main && EditorConfig.Instance.StopOSTOnExitPlanner.Value)
                StopOST();
        }

        #endregion

        #region Misc

        /// <summary>
        /// Sets up the planner links.
        /// </summary>
        /// <param name="input">Text input.</param>
        /// <param name="inputField">Input field reference.</param>
        /// <param name="hyperlinks">Hyperlinks component.</param>
        /// <param name="registerFunctions">If functions should be registered.</param>
        public void SetupPlannerLinks(string input, TMP_InputField inputField, OpenHyperlinks hyperlinks, bool registerFunctions = true) => SetupPlannerLinks(input, hyperlinks, registerFunctions, _val => inputField.SetTextWithoutNotify(_val));

        /// <summary>
        /// Sets up the planner links.
        /// </summary>
        /// <param name="input">Text input.</param>
        /// <param name="text">TextMeshPro text reference.</param>
        /// <param name="hyperlinks">Hyperlinks component.</param>
        /// <param name="registerFunctions">If functions should be registered.</param>
        public void SetupPlannerLinks(string input, TextMeshProUGUI text, OpenHyperlinks hyperlinks, bool registerFunctions = true) => SetupPlannerLinks(input, hyperlinks, registerFunctions, _val => text.text = _val);

        /// <summary>
        /// Sets up the planner links.
        /// </summary>
        /// <param name="input">Text input.</param>
        /// <param name="hyperlinks">Hyperlinks component.</param>
        /// <param name="registerFunctions">If functions should be registered.</param>
        /// <param name="setText">Function to run on planner links setup.</param>
        public void SetupPlannerLinks(string input, OpenHyperlinks hyperlinks, bool registerFunctions, Action<string> setText)
        {
            if (registerFunctions && !hyperlinks)
            {
                LogError("Hyperlinks component is null.");
                return;
            }

            if (registerFunctions)
                hyperlinks.ClearLinks();

            if (string.IsNullOrEmpty(input))
                return;

            var removeLink = registerFunctions ? "</link>" : string.Empty;

            if (input.Contains("refdoc") && input.Contains("</refdoc>"))
            {
                RTString.RegexMatches(input, new Regex("<refdoc=\"(.*?)\">"), match =>
                {
                    var name = match.Groups[1].ToString();
                    if (registerFunctions && documents.TryFind(x => x.Name == name, out DocumentPlanner document))
                    {
                        var link = "refdoc" + LSText.randomNumString(16);
                        hyperlinks.RegisterLink(link, () =>
                        {
                            OpenTab(PlannerBase.Type.Document);
                            OpenDocumentEditor(document);
                        });
                        input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                    }
                    else
                        input = input.Replace(match.Groups[0].ToString(), string.Empty);
                });
                RTString.RegexMatches(input, new Regex(@"<refdoc=(.*?)>"), match =>
                {
                    var name = match.Groups[1].ToString();
                    if (registerFunctions && documents.TryFind(x => x.Name == name, out DocumentPlanner document))
                    {
                        var link = "refdoc" + LSText.randomNumString(16);
                        hyperlinks.RegisterLink(link, () =>
                        {
                            OpenTab(PlannerBase.Type.Document);
                            OpenDocumentEditor(document);
                        });
                        input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                    }
                    else
                        input = input.Replace(match.Groups[0].ToString(), string.Empty);
                });
                input = input.Replace("</refdoc>", removeLink);
            }
            if (input.Contains("refcharacter") && input.Contains("</refcharacter>"))
            {
                RTString.RegexMatches(input, new Regex("<refcharacter=\"(.*?)\">"), match =>
                {
                    var name = match.Groups[1].ToString();
                    if (registerFunctions && characters.TryFind(x => x.Name == name, out CharacterPlanner character))
                    {
                        var link = "refcharacter" + LSText.randomNumString(16);
                        hyperlinks.RegisterLink(link, () =>
                        {
                            OpenTab(PlannerBase.Type.Character);
                            OpenCharacterEditor(character);
                        });
                        input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                    }
                    else
                        input = input.Replace(match.Groups[0].ToString(), string.Empty);
                });
                RTString.RegexMatches(input, new Regex(@"<refcharacter=(.*?)>"), match =>
                {
                    var name = match.Groups[1].ToString();
                    if (registerFunctions && characters.TryFind(x => x.Name == name, out CharacterPlanner character))
                    {
                        var link = "refcharacter" + LSText.randomNumString(16);
                        hyperlinks.RegisterLink(link, () =>
                        {
                            OpenTab(PlannerBase.Type.Character);
                            OpenCharacterEditor(character);
                        });
                        input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                    }
                    else
                        input = input.Replace(match.Groups[0].ToString(), string.Empty);
                });
                input = input.Replace("</refcharacter>", removeLink);
            }
            if (input.Contains("reflevelfolder") && input.Contains("</reflevelfolder>"))
            {
                RTString.RegexMatches(input, new Regex("<reflevelfolder=\"(.*?)\">"), match =>
                {
                    var name = match.Groups[1].ToString();

                    if (registerFunctions && RTFile.DirectoryExists(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, name)))
                    {
                        var link = "reflevelfolder" + LSText.randomNumString(16);
                        hyperlinks.RegisterLink(link, () =>
                        {
                            Close();
                            EditorLevelManager.inst.OpenLevelPopup.PathField.text = name;
                            RTEditor.inst.UpdateEditorPath(false);
                        });
                        input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                    }
                    else
                        input = input.Replace(match.Groups[0].ToString(), string.Empty);
                });
                RTString.RegexMatches(input, new Regex(@"<reflevelfolder=(.*?)>"), match =>
                {
                    var name = match.Groups[1].ToString();

                    if (registerFunctions && RTFile.DirectoryExists(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, name)))
                    {
                        var link = "reflevelfolder" + LSText.randomNumString(16);
                        hyperlinks.RegisterLink(link, () =>
                        {
                            Close();
                            EditorLevelManager.inst.OpenLevelPopup.PathField.text = name;
                            RTEditor.inst.UpdateEditorPath(false);
                        });
                        input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                    }
                    else
                        input = input.Replace(match.Groups[0].ToString(), string.Empty);
                });
                input = input.Replace("</reflevelfolder>", removeLink);
            }
            if (input.Contains("reflevel") && input.Contains("</reflevel>"))
            {
                RTString.RegexMatches(input, new Regex("<reflevel=\"(.*?)\",([0-9.]+)>"), match =>
                {
                    var name = match.Groups[1].ToString();
                    var time = Parser.TryParse(match.Groups[2].ToString(), 0f);
                    try
                    {
                        string path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTFile.ReplaceSlash(name).Remove("/" + Level.LEVEL_LSB));
                        if (registerFunctions && Level.TryVerify(path, true, out Level actualLevel))
                        {
                            var link = "reflevel" + LSText.randomNumString(16);
                            hyperlinks.RegisterLink(link, () =>
                            {
                                Close();
                                if (EditorLevelManager.inst.CurrentLevel && EditorLevelManager.inst.CurrentLevel.path == path)
                                {
                                    if (time >= 0f && time < AudioManager.inst.CurrentAudioSource.clip.length)
                                        AudioManager.inst.SetMusicTime(time);
                                    return;
                                }

                                EditorLevelManager.inst.onLoadLevel = level =>
                                {
                                    if (time >= 0f && time < AudioManager.inst.CurrentAudioSource.clip.length)
                                        AudioManager.inst.SetMusicTime(time);
                                };
                                EditorLevelManager.inst.LoadLevel(actualLevel);
                            });
                            input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                        }
                        else
                            input = input.Replace(match.Groups[0].ToString(), string.Empty);
                    }
                    catch
                    {

                    }
                });
                RTString.RegexMatches(input, new Regex(@"<reflevel=(.*?),([0-9.]+)>"), match =>
                {
                    var name = match.Groups[1].ToString();
                    var time = Parser.TryParse(match.Groups[2].ToString(), 0f);
                    try
                    {
                        string path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTFile.ReplaceSlash(name).Remove("/" + Level.LEVEL_LSB));
                        if (registerFunctions && Level.TryVerify(path, true, out Level actualLevel))
                        {
                            var link = "reflevel" + LSText.randomNumString(16);
                            hyperlinks.RegisterLink(link, () =>
                            {
                                Close();
                                if (EditorLevelManager.inst.CurrentLevel && EditorLevelManager.inst.CurrentLevel.path == path)
                                {
                                    if (time >= 0f && time < AudioManager.inst.CurrentAudioSource.clip.length)
                                        AudioManager.inst.SetMusicTime(time);
                                    return;
                                }

                                EditorLevelManager.inst.onLoadLevel = level =>
                                {
                                    if (time >= 0f && time < AudioManager.inst.CurrentAudioSource.clip.length)
                                        AudioManager.inst.SetMusicTime(time);
                                };
                                EditorLevelManager.inst.LoadLevel(actualLevel);
                            });
                            input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                        }
                        else
                            input = input.Replace(match.Groups[0].ToString(), string.Empty);
                    }
                    catch
                    {

                    }
                });
                RTString.RegexMatches(input, new Regex("<reflevel=\"(.*?)\">"), match =>
                {
                    try
                    {
                        var name = match.Groups[1].ToString();
                        string path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTFile.ReplaceSlash(name).Remove("/" + Level.LEVEL_LSB));
                        if (registerFunctions && Level.TryVerify(path, true, out Level actualLevel))
                        {
                            var link = "reflevel" + LSText.randomNumString(16);
                            hyperlinks.RegisterLink(link, () =>
                            {
                                if (EditorLevelManager.inst.CurrentLevel && EditorLevelManager.inst.CurrentLevel.path == path)
                                    return;

                                Close();
                                EditorLevelManager.inst.LoadLevel(actualLevel);
                            });
                            input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                        }
                        else
                            input = input.Replace(match.Groups[0].ToString(), string.Empty);
                    }
                    catch
                    {

                    }
                });
                RTString.RegexMatches(input, new Regex(@"<reflevel=(.*?)>"), match =>
                {
                    try
                    {
                        var name = match.Groups[1].ToString();
                        string path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTFile.ReplaceSlash(name).Remove("/" + Level.LEVEL_LSB));
                        if (registerFunctions && Level.TryVerify(path, true, out Level actualLevel))
                        {
                            var link = "reflevel" + LSText.randomNumString(16);
                            hyperlinks.RegisterLink(link, () =>
                            {
                                if (EditorLevelManager.inst.CurrentLevel && EditorLevelManager.inst.CurrentLevel.path == path)
                                    return;

                                Close();
                                EditorLevelManager.inst.LoadLevel(actualLevel);
                            });
                            input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                        }
                        else
                            input = input.Replace(match.Groups[0].ToString(), string.Empty);
                    }
                    catch
                    {

                    }
                });
                input = input.Replace("</reflevel>", removeLink);
            }
            if (input.Contains("tab") && input.Contains("</tab>"))
            {
                RTString.RegexMatches(input, new Regex(@"<tab=([0-9])>"), match =>
                {
                    var tab = match.Groups[1].ToString();
                    if (registerFunctions && int.TryParse(tab, out int tabIndex))
                    {
                        var link = "tab" + LSText.randomNumString(16);
                        hyperlinks.RegisterLink(link, () => OpenTab((PlannerBase.Type)tabIndex));
                        input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                    }
                    else
                        input = input.Replace(match.Groups[0].ToString(), string.Empty);
                });
                input = input.Replace("</tab>", removeLink);
            }
            if (input.Contains("url") && input.Contains("</url>"))
            {
                RTString.RegexMatches(input, new Regex("<url=\"(.*?)\",([0-9.]+),\"(.*?)\">"), match =>
                {
                    try
                    {
                        var url = AlephNetwork.GetURL(Parser.TryParse(match.Groups[1].ToString(), URLSource.Song), Parser.TryParse(match.Groups[2].ToString(), 0), match.Groups[3].ToString());
                        if (registerFunctions && !string.IsNullOrEmpty(url))
                        {
                            var link = "url" + LSText.randomNumString(16);
                            hyperlinks.RegisterLink(link, () => Application.OpenURL(url));
                            input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                        }
                        else
                            input = input.Replace(match.Groups[0].ToString(), string.Empty);
                    }
                    catch
                    {

                    }
                });
                RTString.RegexMatches(input, new Regex("<url=\"(.*?)\",([0-9.]+),(.*?)>"), match =>
                {
                    try
                    {
                        var url = AlephNetwork.GetURL(Parser.TryParse(match.Groups[1].ToString(), URLSource.Song), Parser.TryParse(match.Groups[2].ToString(), 0), match.Groups[3].ToString());
                        if (registerFunctions && !string.IsNullOrEmpty(url))
                        {
                            var link = "url" + LSText.randomNumString(16);
                            hyperlinks.RegisterLink(link, () => Application.OpenURL(url));
                            input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                        }
                        else
                            input = input.Replace(match.Groups[0].ToString(), string.Empty);
                    }
                    catch
                    {

                    }
                });
                RTString.RegexMatches(input, new Regex("<url=(.*?),([0-9.]+),\"(.*?)\">"), match =>
                {
                    try
                    {
                        var url = AlephNetwork.GetURL(Parser.TryParse(match.Groups[1].ToString(), URLSource.Song), Parser.TryParse(match.Groups[2].ToString(), 0), match.Groups[3].ToString());
                        if (registerFunctions && !string.IsNullOrEmpty(url))
                        {
                            var link = "url" + LSText.randomNumString(16);
                            hyperlinks.RegisterLink(link, () => Application.OpenURL(url));
                            input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                        }
                        else
                            input = input.Replace(match.Groups[0].ToString(), string.Empty);
                    }
                    catch
                    {

                    }
                });
                RTString.RegexMatches(input, new Regex(@"<url=(.*?),([0-9.]+),(.*?)>"), match =>
                {
                    try
                    {
                        var url = AlephNetwork.GetURL(Parser.TryParse(match.Groups[1].ToString(), URLSource.Song), Parser.TryParse(match.Groups[2].ToString(), 0), match.Groups[3].ToString());
                        if (registerFunctions && !string.IsNullOrEmpty(url))
                        {
                            var link = "url" + LSText.randomNumString(16);
                            hyperlinks.RegisterLink(link, () => Application.OpenURL(url));
                            input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                        }
                        else
                            input = input.Replace(match.Groups[0].ToString(), string.Empty);
                    }
                    catch
                    {

                    }
                });
                RTString.RegexMatches(input, new Regex("<url=\"(.*?)\">"), match =>
                {
                    try
                    {
                        var url = match.Groups[1].ToString();
                        if (registerFunctions && !string.IsNullOrEmpty(url))
                        {
                            var link = "url" + LSText.randomNumString(16);
                            hyperlinks.RegisterLink(link, () => Application.OpenURL(url));
                            input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                        }
                        else
                            input = input.Replace(match.Groups[0].ToString(), string.Empty);
                    }
                    catch
                    {

                    }
                });
                RTString.RegexMatches(input, new Regex(@"<url=(.*?)>"), match =>
                {
                    try
                    {
                        var url = match.Groups[1].ToString();
                        if (registerFunctions && !string.IsNullOrEmpty(url))
                        {
                            var link = "url" + LSText.randomNumString(16);
                            hyperlinks.RegisterLink(link, () => Application.OpenURL(url));
                            input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                        }
                        else
                            input = input.Replace(match.Groups[0].ToString(), string.Empty);
                    }
                    catch
                    {

                    }
                });
                input = input.Replace("</url>", removeLink);
            }
            if (input.Contains("copytext") && input.Contains("</copytext>"))
            {
                RTString.RegexMatches(input, new Regex("<copytext=\"(.*?)\">"), match =>
                {
                    var text = match.Groups[1].ToString();
                    if (registerFunctions && !string.IsNullOrEmpty(text))
                    {
                        var link = "copytext" + LSText.randomNumString(16);
                        hyperlinks.RegisterLink(link, () => LSText.CopyToClipboard(text));
                        input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                    }
                    else
                        input = input.Replace(match.Groups[0].ToString(), string.Empty);
                });
                RTString.RegexMatches(input, new Regex(@"<copytext=(.*?)>"), match =>
                {
                    var text = match.Groups[1].ToString();
                    if (registerFunctions && !string.IsNullOrEmpty(text))
                    {
                        var link = "copytext" + LSText.randomNumString(16);
                        hyperlinks.RegisterLink(link, () => LSText.CopyToClipboard(text));
                        input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                    }
                    else
                        input = input.Replace(match.Groups[0].ToString(), string.Empty);
                });
                input = input.Replace("</copytext>", removeLink);
            }
            if (input.Contains("shownote") && input.Contains("</shownote>"))
            {
                RTString.RegexMatches(input, new Regex("<shownote=\"(.*?)\">"), match =>
                {
                    var name = match.Groups[1].ToString();
                    if (registerFunctions && !string.IsNullOrEmpty(name) && notes.TryFind(x => x.Name == name, out NotePlanner note))
                    {
                        var link = "shownote" + LSText.randomNumString(16);
                        hyperlinks.RegisterLink(link, () =>
                        {
                            note.Active = true;
                            SaveNotes();
                        });
                        input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                    }
                    else
                        input = input.Replace(match.Groups[0].ToString(), string.Empty);
                });
                RTString.RegexMatches(input, new Regex(@"<shownote=(.*?)>"), match =>
                {
                    var name = match.Groups[1].ToString();
                    if (registerFunctions && !string.IsNullOrEmpty(name) && notes.TryFind(x => x.Name == name, out NotePlanner note))
                    {
                        var link = "shownote" + LSText.randomNumString(16);
                        hyperlinks.RegisterLink(link, () =>
                        {
                            note.Active = true;
                            SaveNotes();
                        });
                        input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                    }
                    else
                        input = input.Replace(match.Groups[0].ToString(), string.Empty);
                });
                input = input.Replace("</shownote>", removeLink);
            }
            if (input.Contains("hidenote") && input.Contains("</hidenote>"))
            {
                RTString.RegexMatches(input, new Regex("<hidenote=\"(.*?)\">"), match =>
                {
                    var name = match.Groups[1].ToString();
                    if (registerFunctions && !string.IsNullOrEmpty(name) && notes.TryFind(x => x.Name == name, out NotePlanner note))
                    {
                        var link = "hidenote" + LSText.randomNumString(16);
                        hyperlinks.RegisterLink(link, () =>
                        {
                            note.Active = false;
                            SaveNotes();
                        });
                        input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                    }
                    else
                        input = input.Replace(match.Groups[0].ToString(), string.Empty);
                });
                RTString.RegexMatches(input, new Regex(@"<hidenote=(.*?)>"), match =>
                {
                    var name = match.Groups[1].ToString();
                    if (registerFunctions && !string.IsNullOrEmpty(name) && notes.TryFind(x => x.Name == name, out NotePlanner note))
                    {
                        var link = "hidenote" + LSText.randomNumString(16);
                        hyperlinks.RegisterLink(link, () =>
                        {
                            note.Active = false;
                            SaveNotes();
                        });
                        input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                    }
                    else
                        input = input.Replace(match.Groups[0].ToString(), string.Empty);
                });
                input = input.Replace("</hidenote>", removeLink);
            }
            if (input.Contains("togglenote") && input.Contains("</togglenote>"))
            {
                RTString.RegexMatches(input, new Regex("<togglenote=\"(.*?)\">"), match =>
                {
                    var name = match.Groups[1].ToString();
                    if (registerFunctions && !string.IsNullOrEmpty(name) && notes.TryFind(x => x.Name == name, out NotePlanner note))
                    {
                        var link = "togglenote" + LSText.randomNumString(16);
                        hyperlinks.RegisterLink(link, () =>
                        {
                            note.Active = !note.Active;
                            SaveNotes();
                        });
                        input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                    }
                    else
                        input = input.Replace(match.Groups[0].ToString(), string.Empty);
                });
                RTString.RegexMatches(input, new Regex(@"<togglenote=(.*?)>"), match =>
                {
                    var name = match.Groups[1].ToString();
                    if (registerFunctions && !string.IsNullOrEmpty(name) && notes.TryFind(x => x.Name == name, out NotePlanner note))
                    {
                        var link = "togglenote" + LSText.randomNumString(16);
                        hyperlinks.RegisterLink(link, () =>
                        {
                            note.Active = !note.Active;
                            SaveNotes();
                        });
                        input = input.Replace(match.Groups[0].ToString(), $"<link={link}>");
                    }
                    else
                        input = input.Replace(match.Groups[0].ToString(), string.Empty);
                });
                input = input.Replace("</togglenote>", removeLink);
            }

            setText?.Invoke(input);
        }
        
        /// <summary>
        /// Copies the selected planners.
        /// </summary>
        public void CopySelectedPlanners()
        {
            copiedPlanners.AddRange(documents.Where(x => x.Selected));
            copiedPlanners.AddRange(todos.Where(x => x.Selected));
            copiedPlanners.AddRange(characters.Where(x => x.Selected));
            copiedPlanners.AddRange(timelines.Where(x => x.Selected));
            copiedPlanners.AddRange(schedules.Where(x => x.Selected));
            copiedPlanners.AddRange(notes.Where(x => x.Selected));
            copiedPlanners.AddRange(osts.Where(x => x.Selected));
            EditorManager.inst.DisplayNotification("Copied all selected planners.", 2f, EditorManager.NotificationType.Success);
        }

        /// <summary>
        /// Copies the current tabs' planners.
        /// </summary>
        public void CopyCurrentTabPlanners()
        {
            switch (CurrentTab)
            {
                case PlannerBase.Type.Document: {
                        copiedPlanners.AddRange(documents);
                        break;
                    }
                case PlannerBase.Type.TODO: {
                        copiedPlanners.AddRange(todos);
                        break;
                    }
                case PlannerBase.Type.Character: {
                        copiedPlanners.AddRange(characters);
                        break;
                    }
                case PlannerBase.Type.Timeline: {
                        copiedPlanners.AddRange(timelines);
                        break;
                    }
                case PlannerBase.Type.Schedule: {
                        copiedPlanners.AddRange(schedules);
                        break;
                    }
                case PlannerBase.Type.Note: {
                        copiedPlanners.AddRange(notes);
                        break;
                    }
                case PlannerBase.Type.OST: {
                        copiedPlanners.AddRange(osts);
                        break;
                    }
            }
            EditorManager.inst.DisplayNotification("Copied all planners in the current tab.", 2f, EditorManager.NotificationType.Success);
        }

        /// <summary>
        /// Pastes the planners copied from <see cref="copiedPlanners"/>.
        /// </summary>
        public void PastePlanners()
        {
            int pastedCount = 0;
            for (int i = 0; i < copiedPlanners.Count; i++)
            {
                var plannerItem = copiedPlanners[i];
                if (HasPlanner(plannerItem))
                    continue;

                switch (plannerItem.PlannerType)
                {
                    case PlannerBase.Type.Document: {
                            if (plannerItem is not DocumentPlanner document)
                                break;

                            var copy = document.CreateCopy();
                            documents.Add(copy);
                            copy.Init();
                            pastedCount++;
                            break;
                        }
                    case PlannerBase.Type.TODO: {
                            if (plannerItem is not TODOPlanner todo)
                                break;

                            var copy = todo.CreateCopy();
                            todos.Add(copy);
                            copy.Init();
                            pastedCount++;
                            break;
                        }
                    case PlannerBase.Type.Character: {
                            if (plannerItem is not CharacterPlanner character)
                                break;

                            var copy = character.CreateCopy();
                            characters.Add(copy);
                            copy.Init();
                            pastedCount++;
                            break;
                        }
                    case PlannerBase.Type.Timeline: {
                            if (plannerItem is not TimelinePlanner timeline)
                                break;

                            var copy = timeline.CreateCopy();
                            timelines.Add(copy);
                            copy.Init();
                            pastedCount++;
                            break;
                        }
                    case PlannerBase.Type.Schedule: {
                            if (plannerItem is not SchedulePlanner schedule)
                                break;

                            var copy = schedule.CreateCopy();
                            schedules.Add(copy);
                            copy.Init();
                            pastedCount++;
                            break;
                        }
                    case PlannerBase.Type.Note: {
                            if (plannerItem is not NotePlanner note)
                                break;

                            var copy = note.CreateCopy();
                            notes.Add(copy);
                            copy.Init();
                            pastedCount++;
                            break;
                        }
                    case PlannerBase.Type.OST: {
                            if (plannerItem is not OSTPlanner ost)
                                break;

                            var copy = ost.CreateCopy();
                            osts.Add(copy);
                            copy.Init();
                            pastedCount++;
                            break;
                        }
                }
            }

            Save();
            RefreshList();

            if (pastedCount > 0)
                EditorManager.inst.DisplayNotification("Pasted the copied planners.", 2f, EditorManager.NotificationType.Success);
        }

        /// <summary>
        /// Checks if a planner already exists.
        /// </summary>
        /// <param name="planner">Planner to check.</param>
        /// <returns>Returns true if the planner already exists in the planner lists, otherwise returns false.</returns>
        public bool HasPlanner(PlannerBase planner) => planner.PlannerType switch
        {
            PlannerBase.Type.Document => documents.Has(x => x.SamePlanner(planner)),
            PlannerBase.Type.TODO => todos.Has(x => x.SamePlanner(planner)),
            PlannerBase.Type.Character => characters.Has(x => x.SamePlanner(planner)),
            PlannerBase.Type.Timeline => timelines.Has(x => x.SamePlanner(planner)),
            PlannerBase.Type.Schedule => schedules.Has(x => x.SamePlanner(planner)),
            PlannerBase.Type.Note => notes.Has(x => x.SamePlanner(planner)),
            PlannerBase.Type.OST => osts.Has(x => x.SamePlanner(planner)),
            _ => false,
        };

        /// <summary>
        /// Starts the OST from the beginning.
        /// </summary>
        public void StartOST()
        {
            StopOST();
            if (!osts.IsEmpty())
                osts[0].Play();
        }

        /// <summary>
        /// Stops the currently playing OST.
        /// </summary>
        public void StopOST()
        {
            forceShuffleOST = false;
            pausedOST = false;

            Destroy(OSTAudioSource);

            for (int i = 0; i < osts.Count; i++)
                osts[i].playing = false;

            playing = false;
        }

        /// <summary>
        /// Shuffles the OST.
        /// </summary>
        public void ShuffleOST()
        {
            StopOST();
            forceShuffleOST = true;
            if (!osts.IsEmpty())
                osts[UnityRandom.Range(0, osts.Count)].Play();
        }

        /// <summary>
        /// Starts playing the next OST.
        /// </summary>
        public void NextOST()
        {
            var list = osts;

            if (EditorConfig.Instance.OSTShuffle.Value || forceShuffleOST)
            {
                recentOST.TrimStart(20);

                OSTPlanner ost = null;
                int attempts = 0;
                int num = 0;
                while (attempts < 40)
                {
                    num = UnityRandom.Range(0, osts.Count);
                    ost = list[num];
                    if (!recentOST.Has(x => x.Path == ost.Path))
                        break;

                    attempts++;
                }
                if (!ost)
                    return;

                recentOST.Add(ost);
                ost.Play();
            }
            else
            {
                int num = 1;
                // Here we skip any OST where a song file does not exist.
                while (currentOST + num < list.Count && !list[num].Valid)
                    num++;

                list[currentOST].playing = false;
                playing = false;

                if (currentOST + num >= list.Count)
                {
                    if (EditorConfig.Instance.OSTLoop.Value != LoopOSTBehaviorType.LoopAll)
                        return;

                    currentOST = 0;
                    num = 0;
                }

                list[currentOST + num].Play();
            }
        }

        /// <summary>
        /// Updates the indexes of the OST planners.
        /// </summary>
        public void UpdateOSTIndexes()
        {
            var list = osts.OrderBy(x => x.Index).ToList();
            for (int i = 0; i < list.Count; i++)
            {
                var ost = list[i];
                ost.Index = i;
                ost.GameObject.transform.SetSiblingIndex(i);
            }
        }

        void SelectCharacterImage()
        {
            if (!currentCharacterPlanner)
            {
                EditorManager.inst.DisplayNotification("No character planner selected!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var editorPath = RTFile.ApplicationDirectory;
            string jpgFile = FileBrowser.OpenSingleFile("Select an image!", editorPath, "png", "jpg");

            if (string.IsNullOrEmpty(jpgFile))
                return;

            currentCharacterPlanner.Sprite = SpriteHelper.LoadSprite(jpgFile);
            currentCharacterPlanner.ProfileUI.sprite = currentCharacterPlanner.Sprite;
            SaveCharacters();

            RenderCharacterEditorDisplay(currentCharacterPlanner);
        }

        #endregion

        #endregion
    }
}
