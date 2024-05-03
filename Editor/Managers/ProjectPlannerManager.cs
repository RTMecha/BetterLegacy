using BetterLegacy.Components;
using BetterLegacy.Components.Editor;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Core.Prefabs;
using Crosstales.FB;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Managers
{
    public class ProjectPlannerManager : MonoBehaviour
    {
        public static ProjectPlannerManager inst;

        #region Variables

        public Transform plannerBase;
        public Transform planner;
        public Transform topBarBase;
        public Transform contentBase;
        public Transform contentScroll;
        public Transform content;
        public Transform notesParent;
        public GridLayoutGroup contentLayout;

        public Transform assetsParent;

        public GameObject documentFullView;
        public TMP_InputField documentInputField;
        public TextMeshProUGUI documentTitle;

        public AudioSource OSTAudioSource { get; set; }
        public int currentOST;
        public string currentOSTID;
        public bool playing = false;

        public List<Toggle> tabs = new List<Toggle>();

        public int CurrentTab { get; set; }
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
            new Vector2(296f, 270f),
            new Vector2(1280f, 250f),
            new Vector2(1280f, 64f),
            new Vector2(339f, 150f),
            new Vector2(1280f, 64f),
        };

        public int[] tabConstraintCounts = new int[]
        {
            5,
            1,
            4,
            1,
            1,
            3,
            1,
        };

        public GameObject tagPrefab;

        public GameObject tabPrefab;

        public GameObject closePrefab;

        public GameObject baseCardPrefab;

        public List<GameObject> prefabs = new List<GameObject>();

        public Sprite gradientSprite;

        public List<PlannerItem> planners = new List<PlannerItem>();

        public string PlannersPath { get; set; } = "planners";

        public GameObject timelineButtonPrefab;

        public GameObject timelineAddPrefab;

        public Texture2D horizontalDrag;
        public Texture2D verticalDrag;

        #endregion

        void Awake()
        {
            inst = this;
            plannerBase = GameObject.Find("Editor Systems/Editor GUI/sizer").transform.GetChild(1);
            plannerBase.gameObject.SetActive(true);

            planner = plannerBase.GetChild(0);
            topBarBase = planner.GetChild(0);

            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Background_1, planner.gameObject, new List<Component>
            {
                planner.GetComponent<Image>(),
            }));

            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Background_1, topBarBase.gameObject, new List<Component>
            {
                topBarBase.GetComponent<Image>(),
            }));

            var assets = new GameObject("Planner Assets");
            assetsParent = assets.transform;
            assetsParent.transform.SetParent(transform);

            tabPrefab = topBarBase.GetChild(0).gameObject;
            tabPrefab.transform.SetParent(assetsParent);
            tabPrefab.transform.AsRT().sizeDelta = new Vector2(200f, 54f);

            LSHelpers.DeleteChildren(topBarBase);

            Destroy(topBarBase.GetComponent<ToggleGroup>());
            tabPrefab.GetComponent<Toggle>().group = null;

            for (int i = 0; i < tabNames.Length; i++)
                GenerateTab(tabNames[i]);

            closePrefab = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/Open File Popup/Panel/x").Duplicate(assetsParent, "x");

            Spacer("topbar spacer", topBarBase, new Vector2(195f, 32f));

            var close = closePrefab.Duplicate(topBarBase, "close");
            close.transform.localScale = Vector3.one;

            close.transform.AsRT().sizeDelta = new Vector2(48f, 48f);

            var closeButton = close.GetComponent<Button>();
            closeButton.onClick.ClearAll();
            closeButton.onClick.AddListener(delegate ()
            {
                Close();
            });

            EditorThemeManager.AddSelectable(closeButton, ThemeGroup.Close);

            var closeX = close.transform.GetChild(0).gameObject;
            EditorThemeManager.AddGraphic(close.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

            EditorHelper.AddEditorDropdown("Open Project Planner", "", "Edit", SpriteManager.LoadSprite(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "editor_gui_planner.png"), delegate ()
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

            var scrollBarVertical = contentScroll.Find("Scrollbar Vertical");
            scrollBarVertical.GetComponent<Image>().color = new Color(0.11f, 0.11f, 0.11f, 1f);
            var handleImage = scrollBarVertical.Find("Sliding Area/Handle").GetComponent<Image>();
            handleImage.color = new Color(0.878f, 0.878f, 0.878f, 1f);
            handleImage.sprite = null;

            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Background_1, scrollBarVertical.gameObject, new List<Component>
            {
                scrollBarVertical.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.Bottom_Right_I));

            var scrollbarHandle = scrollBarVertical.transform.Find("Sliding Area/Handle").gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Scrollbar_1_Handle, handleImage.gameObject, new List<Component>
            {
                handleImage,
                scrollBarVertical.GetComponent<Scrollbar>()
            }, true, 1, SpriteManager.RoundedSide.W, true));

            contentBase.Find("Image").AsRT().anchoredPosition = new Vector2(690f, /*-94f*/ -104f);
            contentBase.Find("Image").AsRT().sizeDelta = new Vector2(1384f, 48f);
            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Background_1, contentBase.Find("Image").gameObject, new List<Component>
            {
                contentBase.Find("Image").GetComponent<Image>(),
            }));

            // List handlers
            {
                var searchBase = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups/Open File Popup/search-box").Duplicate(contentBase.Find("Image"), "search base");
                searchBase.transform.localScale = Vector3.one;
                searchBase.transform.AsRT().anchoredPosition = Vector2.zero;
                searchBase.transform.AsRT().sizeDelta = new Vector2(0f, 48f);
                searchBase.transform.GetChild(0).AsRT().sizeDelta = new Vector2(0f, 48f);

                var searchField = searchBase.transform.GetChild(0).GetComponent<InputField>();
                searchField.onValueChanged.ClearAll();
                ((Text)searchField.placeholder).text = "Search...";
                searchField.onValueChanged.AddListener(delegate (string _val)
                {
                    CoreHelper.Log($"Searching {_val}");
                    SearchTerm = _val;
                    RefreshList();
                });

                EditorThemeManager.AddInputField(searchField, ThemeGroup.Search_Field_1, 1, SpriteManager.RoundedSide.Bottom);

                var tfv = ObjEditor.inst.ObjectView.transform;

                var addNewItem = EditorPrefabHolder.Instance.Function2Button.Duplicate(contentBase, "new", 1);
                addNewItem.SetActive(true);
                var addNewItemStorage = addNewItem.GetComponent<FunctionButtonStorage>();
                addNewItem.transform.AsRT().anchoredPosition = new Vector2(120f, 970f);
                addNewItem.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                var addNewItemText = addNewItemStorage.text;
                addNewItemText.text = "Add New Item";
                addNewItemStorage.button.onClick.ClearAll();
                addNewItemStorage.button.onClick.AddListener(delegate ()
                {
                    CoreHelper.Log($"Create new {tabNames[CurrentTab]}");
                    var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
                    switch (CurrentTab)
                    {
                        case 0:
                            {
                                var list = planners.Where(x => x.PlannerType == PlannerItem.Type.Document && x is DocumentItem).Select(x => x as DocumentItem);

                                var document = new DocumentItem();
                                document.Name = $"New Story {list.Count() + 1}";
                                document.Text = "<align=center>Plan out your story!";
                                planners.Add(document);
                                GenerateDocument(document);

                                SaveDocuments();

                                break;
                            } // Document
                        case 1:
                            {
                                var list = planners.Where(x => x.PlannerType == PlannerItem.Type.TODO && x is TODOItem).Select(x => x as TODOItem);

                                var todo = new TODOItem();
                                todo.Checked = false;
                                todo.Text = "Do this.";
                                todo.Priority = list.Count();
                                planners.Add(todo);
                                GenerateTODO(todo);

                                SaveTODO();

                                break;
                            } // TODO
                        case 2:
                            {
                                var list = planners.Where(x => x.PlannerType == PlannerItem.Type.Character && x is CharacterItem).Select(x => x as CharacterItem);

                                if (string.IsNullOrEmpty(Path.GetFileName(path)))
                                    return;

                                if (!RTFile.DirectoryExists(path + "/characters"))
                                {
                                    Directory.CreateDirectory(path + "/characters");
                                }

                                var fullPath = path + "/characters/New Character";

                                int num = 1;
                                while (RTFile.DirectoryExists(fullPath))
                                {
                                    fullPath = $"{path}/characters/New Character {num}";
                                    num++;
                                }

                                Directory.CreateDirectory(fullPath);

                                var character = new CharacterItem();

                                for (int i = 0; i < 3; i++)
                                {
                                    character.CharacterTraits.Add("???");
                                    character.CharacterLore.Add("???");
                                    character.CharacterAbilities.Add("???");
                                }

                                character.CharacterSprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "CA Hal.png");
                                character.Name = Path.GetFileName(fullPath);
                                character.FullPath = fullPath;
                                character.Gender = "He";
                                character.Description = "This is the default description";
                                planners.Add(character);
                                GenerateCharacter(character);
                                character.Save();

                                break;
                            } // Character
                        case 3:
                            {
                                var timeline = new TimelineItem();
                                timeline.Name = "Classic Arrhythmia";
                                timeline.Levels.Add(new TimelineItem.Event
                                {
                                    Name = "Beginning",
                                    Description = $"Introduces players / viewers to Hal.)",
                                    ElementType = TimelineItem.Event.Type.Cutscene,
                                    Path = ""
                                });
                                timeline.Levels.Add(new TimelineItem.Event
                                {
                                    Name = "Tokyo Skies",
                                    Description = $"Players learn very basic stuff about Classic Arrhythmia / Project Arrhythmia mechanics.{Environment.NewLine}{Environment.NewLine}(Click on this button to open the level.)",
                                    ElementType = TimelineItem.Event.Type.Level,
                                    Path = ""
                                });

                                planners.Add(timeline);
                                GenerateTimeline(timeline);

                                SaveTimelines();

                                break;
                            } // Timeline
                        case 4:
                            {
                                var schedule = new ScheduleItem();
                                schedule.Date = DateTime.Now.AddDays(1).ToString("g");
                                schedule.Description = "Tomorrow!";
                                planners.Add(schedule);
                                GenerateSchedule(schedule);

                                SaveSchedules();

                                break;
                            } // Schedule
                        case 5:
                            {
                                var note = new NoteItem();
                                note.Active = true;
                                note.Name = "New Note";
                                note.Color = UnityEngine.Random.Range(0, MarkerEditor.inst.markerColors.Count);
                                note.Position = new Vector2(Screen.width / 2, Screen.height / 2);
                                note.Text = "This note appears in the editor and can be dragged to anywhere.";
                                planners.Add(note);
                                GenerateNote(note);

                                SaveNotes();

                                break;
                            } // Note
                        case 6:
                            {
                                var ost = new OSTItem();
                                ost.Name = "Kaixo - Fragments";
                                ost.Path = "Set this path to wherever you have a song located.";
                                planners.Add(ost);
                                GenerateOST(ost);

                                var list = planners.Where(x => x.PlannerType == PlannerItem.Type.OST && x is OSTItem).Select(x => x as OSTItem).OrderBy(x => x.Index).ToList();

                                ost.Index = list.Count - 1;

                                SaveOST();

                                break;
                            } // OST
                        default:
                            {
                                CoreHelper.LogWarning($"How did you do that...?");
                                break;
                            }
                    }
                    RenderTabs();
                    RefreshList();
                });

                addNewItemStorage.button.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddSelectable(addNewItemStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Function_2_Text, addNewItemText.gameObject, new List<Component>
                {
                    addNewItemText,
                }));

                var reload = EditorPrefabHolder.Instance.Function2Button.Duplicate(contentBase, "reload", 2);
                reload.SetActive(true);
                var reloadStorage = reload.GetComponent<FunctionButtonStorage>();
                reload.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                reload.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                var reloadText = reloadStorage.text;
                reloadText.text = "Reload";
                reloadStorage.button.onClick.ClearAll();
                reloadStorage.button.onClick.AddListener(delegate ()
                {
                    Load();
                });

                reloadStorage.button.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddSelectable(reloadStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Function_2_Text, reloadText.gameObject, new List<Component>
                {
                    reloadText,
                }));
            }

            gradientSprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "linear_gradient.png");

            // Item Prefabs
            {
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

                    albumArt.AsRT().anchoredPosition = Vector2.zero;
                    albumArt.AsRT().sizeDelta = new Vector2(232f, 400f);

                    artist.AsRT().anchoredPosition = new Vector2(0f, -60f);
                    artist.AsRT().sizeDelta = new Vector2(-32f, 260f);
                    var tmp = artist.GetComponent<TextMeshProUGUI>();
                    tmp.alignment = TextAlignmentOptions.TopLeft;
                    tmp.enableWordWrapping = true;
                    tmp.text = "This is your story.";

                    var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(prefab.transform, "delete");
                    UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(96f, 180f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26f, 26f));

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

                    var toggle = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle").Duplicate(prefab.transform, "checked");
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
                    UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(605f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(38f, 38f));

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

                    albumArt.AsRT().anchoredPosition = new Vector2(80f, 66f);
                    albumArt.AsRT().sizeDelta = new Vector2(115f, 115f);

                    title.AsRT().anchoredPosition = new Vector2(0f, 2f);
                    title.AsRT().sizeDelta = new Vector2(-32f, 240f);

                    artist.AsRT().anchoredPosition = new Vector2(60f, -64f);
                    artist.AsRT().sizeDelta = new Vector2(-130f, 130f);

                    var tmpTitle = title.GetComponent<TextMeshProUGUI>();
                    tmpTitle.lineSpacing = -20;
                    tmpTitle.fontSize = 13;
                    tmpTitle.fontStyle = FontStyles.Normal;
                    tmpTitle.alignment = TextAlignmentOptions.TopLeft;
                    tmpTitle.enableWordWrapping = true;
                    tmpTitle.text = CharacterItem.DefaultCharacterDescription;

                    var tmpArtist = artist.GetComponent<TextMeshProUGUI>();
                    tmpArtist.alignment = TextAlignmentOptions.TopRight;
                    tmpArtist.fontSize = 12;
                    tmpArtist.enableWordWrapping = true;
                    tmpArtist.text = "Description";

                    var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(prefab.transform, "delete");
                    UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(-16f, -16f), Vector2.one, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(24f, 24f));

                    prefabs.Add(prefab);
                }

                // Timeline
                {
                    var prefab = new GameObject("timeline prefab");
                    prefab.transform.SetParent(assetsParent);
                    prefab.transform.localScale = Vector3.one;

                    var prefabRT = prefab.AddComponent<RectTransform>();
                    UIManager.SetRectTransform(prefabRT, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero);
                    var prefabImage = prefab.AddComponent<Image>();
                    prefabImage.color = new Color(0f, 0f, 0f, 0.2f);

                    var editPrefab = closePrefab.Duplicate(prefabRT, "edit");
                    UIManager.SetRectTransform(editPrefab.transform.AsRT(), new Vector2(-38f, -12f), Vector2.one, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(20f, 20f));
                    editPrefab.transform.GetChild(0).AsRT().sizeDelta = new Vector2(0f, 0f);
                    var editPrefabButton = editPrefab.GetComponent<Button>();
                    editPrefabButton.colors = UIManager.SetColorBlock(editPrefabButton.colors, new Color(0.9f, 0.9f, 0.9f, 1f), Color.white, Color.white, new Color(0.9f, 0.9f, 0.9f, 1f), LSColors.red700);
                    var spritePrefabImage = editPrefab.transform.GetChild(0).GetComponent<Image>();
                    spritePrefabImage.color = new Color(0.037f, 0.037f, 0.037f, 1f);
                    spritePrefabImage.sprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "editor_gui_edit.png");

                    var deletePrefab = EditorPrefabHolder.Instance.DeleteButton.Duplicate(prefabRT, "delete");
                    UIManager.SetRectTransform(deletePrefab.transform.AsRT(), new Vector2(-12f, -12f), Vector2.one, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(20f, 20f));

                    var prefabScroll = new GameObject("Scroll");
                    prefabScroll.transform.SetParent(prefabRT);
                    prefabScroll.transform.localScale = Vector3.one;
                    var prefabScrollRT = prefabScroll.AddComponent<RectTransform>();
                    prefabScrollRT.anchoredPosition = new Vector2(590f, -125f);
                    prefabScrollRT.anchorMax = new Vector2(0f, 1f);
                    prefabScrollRT.anchorMin = new Vector2(0f, 1f);
                    prefabScrollRT.sizeDelta = new Vector2(1180f, 200f);
                    var prefabScrollRect = prefabScroll.AddComponent<ScrollRect>();

                    var prefabViewport = new GameObject("Viewport");
                    prefabViewport.transform.SetParent(prefabScrollRT);
                    prefabViewport.transform.localScale = Vector3.one;
                    var prefabViewportRT = prefabViewport.AddComponent<RectTransform>();
                    prefabViewportRT.anchoredPosition = Vector3.zero;
                    prefabViewportRT.anchorMax = Vector3.one;
                    prefabViewportRT.anchorMin = Vector3.zero;
                    prefabViewportRT.pivot = new Vector2(0f, 1f);
                    var prefabViewportImage = prefabViewport.AddComponent<Image>();
                    var prefabViewportMask = prefabViewport.AddComponent<Mask>();
                    prefabViewportMask.showMaskGraphic = false;

                    var prefabContent = new GameObject("Content");
                    prefabContent.transform.SetParent(prefabViewportRT);
                    prefabContent.transform.localScale = Vector3.one;
                    var prefabContentRT = prefabContent.AddComponent<RectTransform>();
                    prefabContentRT.anchoredPosition = Vector3.zero;
                    prefabContentRT.anchorMax = new Vector2(0f, 1f);
                    prefabContentRT.anchorMin = new Vector2(0f, 1f);
                    prefabContentRT.pivot = new Vector2(0f, 1f);
                    var prefabContentGLG = prefabContent.AddComponent<GridLayoutGroup>();
                    prefabContentGLG.cellSize = new Vector2(422f, 200f);
                    prefabContentGLG.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                    prefabContentGLG.constraintCount = 1;
                    prefabContentGLG.spacing = new Vector2(8f, 0f);
                    var prefabContentCSF = prefabContent.AddComponent<ContentSizeFitter>();
                    prefabContentCSF.horizontalFit = ContentSizeFitter.FitMode.MinSize;

                    prefabScrollRect.content = prefabContentRT;
                    prefabScrollRect.viewport = prefabViewportRT;
                    prefabScrollRect.vertical = false;

                    var scrollBar = GameObject.Find("Editor Systems/Editor GUI/sizer/main/whole-timeline/Scrollbar").Duplicate(prefabRT, "Scrollbar");
                    scrollBar.transform.AsRT().anchoredPosition = Vector2.zero;
                    scrollBar.transform.AsRT().pivot = new Vector2(0.5f, 0f);
                    scrollBar.transform.AsRT().sizeDelta = new Vector2(0f, 25f);

                    prefabScrollRect.horizontalScrollbar = scrollBar.GetComponent<Scrollbar>();

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

                    var edit = closePrefab.Duplicate(timelineButtonPrefab.transform, "edit");
                    edit.transform.AsRT().anchoredPosition = new Vector2(-46f, -16f);
                    edit.transform.AsRT().pivot = new Vector2(0.5f, 0.5f);
                    edit.transform.AsRT().sizeDelta = new Vector2(24f, 24f);
                    edit.transform.GetChild(0).AsRT().sizeDelta = new Vector2(0f, 0f);
                    var editButton = edit.GetComponent<Button>();
                    editButton.colors = UIManager.SetColorBlock(editButton.colors, new Color(0.9f, 0.9f, 0.9f, 1f), Color.white, Color.white, new Color(0.9f, 0.9f, 0.9f, 1f), LSColors.red700);
                    var spriteImage = edit.transform.GetChild(0).GetComponent<Image>();
                    spriteImage.color = new Color(0.037f, 0.037f, 0.037f, 1f);
                    spriteImage.sprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "editor_gui_edit.png");

                    var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(timelineButtonPrefab.transform, "delete");
                    delete.transform.AsRT().anchoredPosition = new Vector2(-16f, -16f);
                    delete.transform.AsRT().pivot = new Vector2(0.5f, 0.5f);
                    delete.transform.AsRT().sizeDelta = new Vector2(24f, 24f);
                    UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(-16f, -16f), Vector2.one, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(24f, 24f));

                    timelineAddPrefab = baseCardPrefab.Duplicate(assetsParent, "timeline add prefab");
                    var albumArtAdd = timelineAddPrefab.transform.GetChild(0);
                    var titleAdd = timelineAddPrefab.transform.GetChild(1);
                    var artistAdd = timelineAddPrefab.transform.GetChild(2);

                    timelineAddPrefab.GetComponent<Image>().sprite = null;

                    DestroyImmediate(albumArtAdd.gameObject);
                    artistAdd.SetParent(prefabRT);
                    artistAdd.AsRT().anchoredPosition = new Vector2(-56f, 105f);
                    artistAdd.AsRT().sizeDelta = new Vector2(-128f, 40f);
                    artistAdd.name = "name";

                    var tmpArtistAdd = artistAdd.GetComponent<TextMeshProUGUI>();
                    tmpArtistAdd.alignment = TextAlignmentOptions.TopLeft;
                    tmpArtistAdd.color = Color.white;

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
                    UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(605f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(38f, 38f));

                    prefabs.Add(prefab);
                }

                // Note
                {
                    var prefab = new GameObject("note prefab");
                    prefab.transform.SetParent(assetsParent);
                    prefab.transform.localScale = Vector3.one;

                    var prefabRT = prefab.AddComponent<RectTransform>();
                    prefabRT.sizeDelta = new Vector2(300f, 150f);
                    var prefabImage = prefab.AddComponent<Image>();
                    prefabImage.color = new Color(0.251f, 0.251f, 0.251f, 1f);

                    //var inputField = prefab.AddComponent<TMP_InputField>();

                    var prefabPanel = new GameObject("panel");
                    prefabPanel.transform.SetParent(prefabRT);
                    prefabPanel.transform.localScale = Vector3.one;

                    var prefabPanelRT = prefabPanel.AddComponent<RectTransform>();
                    prefabPanelRT.anchoredPosition = Vector2.zero;
                    prefabPanelRT.anchorMax = Vector2.one;
                    prefabPanelRT.anchorMin = new Vector2(0f, 1f);
                    prefabPanelRT.pivot = Vector2.zero;
                    prefabPanelRT.sizeDelta = new Vector2(0f, 32f);
                    var prefabPanelImage = prefabPanel.AddComponent<Image>();

                    var noteTitle = baseCardPrefab.transform.Find("title").gameObject.Duplicate(prefabPanelRT, "title");
                    noteTitle.transform.AsRT().anchoredPosition = new Vector2(-26f, 0f);
                    noteTitle.transform.AsRT().sizeDelta = new Vector2(-70f, 40f);

                    var tmpNoteTitle = noteTitle.GetComponent<TextMeshProUGUI>();
                    tmpNoteTitle.alignment = TextAlignmentOptions.Left;
                    tmpNoteTitle.enableWordWrapping = false;
                    tmpNoteTitle.fontSize = 16;

                    var toggle = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle").Duplicate(prefabPanelRT, "active");
                    UIManager.SetRectTransform(toggle.transform.AsRT(), Vector2.zero, new Vector2(0.87f, 0.5f), new Vector2(0.87f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
                    UIManager.SetRectTransform(toggle.transform.GetChild(0).AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(24f, 24f));

                    var editPrefab = closePrefab.Duplicate(prefabPanelRT, "edit");
                    UIManager.SetRectTransform(editPrefab.transform.AsRT(), new Vector2(-44f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26f, 26f));
                    editPrefab.transform.GetChild(0).AsRT().sizeDelta = new Vector2(4f, 4f);
                    var editPrefabButton = editPrefab.GetComponent<Button>();
                    editPrefabButton.colors = UIManager.SetColorBlock(editPrefabButton.colors, new Color(0.9f, 0.9f, 0.9f, 1f), Color.white, Color.white, new Color(0.9f, 0.9f, 0.9f, 1f), LSColors.red700);
                    var spritePrefabImage = editPrefab.transform.GetChild(0).GetComponent<Image>();
                    spritePrefabImage.color = new Color(0.037f, 0.037f, 0.037f, 1f);
                    spritePrefabImage.sprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "editor_gui_edit.png");

                    var closeB = closePrefab.Duplicate(prefabPanelRT, "close");
                    UIManager.SetRectTransform(closeB.transform.AsRT(), new Vector2(-16f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26f, 26f));

                    var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(prefabPanelRT, "delete");
                    UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(-16f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26f, 26f));

                    var noteText = baseCardPrefab.transform.Find("artist").gameObject.Duplicate(prefabRT, "text");
                    noteText.transform.AsRT().anchoredPosition = Vector2.zero;
                    noteText.transform.AsRT().anchorMax = Vector2.one;
                    noteText.transform.AsRT().anchorMin = Vector2.zero;
                    noteText.transform.AsRT().sizeDelta = new Vector2(-16f, 0f);

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
                var bytes = File.ReadAllBytes(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "editor_gui_mouse_scroll_h.png");
                horizontalDrag.LoadImage(bytes);

                horizontalDrag.wrapMode = TextureWrapMode.Clamp;
                horizontalDrag.filterMode = FilterMode.Point;
                horizontalDrag.Apply();

                verticalDrag = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                bytes = File.ReadAllBytes(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "editor_gui_mouse_scroll_v.png");
                verticalDrag.LoadImage(bytes);

                verticalDrag.wrapMode = TextureWrapMode.Clamp;
                verticalDrag.filterMode = FilterMode.Point;
                verticalDrag.Apply();
            }

            // Notes
            {
                var notesParent = new GameObject("notes");
                notesParent.transform.SetParent(plannerBase);
                notesParent.transform.localScale = Vector3.one;

                var notesParentRT = notesParent.AddComponent<RectTransform>();

                this.notesParent = notesParentRT;
            }

            // Document Full View
            {
                var fullView = new GameObject("full view");
                documentFullView = fullView;
                fullView.transform.SetParent(contentBase);
                fullView.transform.localScale = Vector3.one;
                var fullViewRT = fullView.AddComponent<RectTransform>();
                var fullViewImage = fullView.AddComponent<Image>();
                fullViewImage.color = new Color(0.082f, 0.082f, 0.078f, 1f);
                fullViewRT.anchoredPosition = new Vector2(690f, -548f);
                fullViewRT.sizeDelta = new Vector2(1384f, 936f);
                fullViewRT.anchorMax = new Vector2(0f, 1f);
                fullViewRT.anchorMin = new Vector2(0f, 1f);

                EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Background_1, fullView, new List<Component>
                {
                    fullViewImage,
                }));

                documentInputField = fullView.AddComponent<TMP_InputField>();

                var docText = baseCardPrefab.transform.Find("artist").gameObject.Duplicate(fullViewRT, "text");
                docText.transform.AsRT().anchoredPosition = new Vector2(0f, -50f);
                docText.transform.AsRT().sizeDelta = new Vector2(-32f, 840f);
                var t = docText.GetComponent<TextMeshProUGUI>();

                documentInputField.textComponent = t;

                var docTitle = baseCardPrefab.transform.Find("artist").gameObject.Duplicate(fullViewRT, "title");

                docTitle.transform.AsRT().anchoredPosition = new Vector2(0f, 15f);
                docTitle.transform.AsRT().sizeDelta = new Vector2(-32, 840f);

                documentTitle = docTitle.GetComponent<TextMeshProUGUI>();
                documentTitle.alignment = TextAlignmentOptions.TopLeft;
                documentTitle.fontSize = 32;

                t.alignment = TextAlignmentOptions.TopLeft;
                t.enableWordWrapping = true;

                EditorThemeManager.AddLightText(documentTitle);
                EditorThemeManager.AddInputField(documentInputField);

                fullView.SetActive(false);
            }

            // Editor
            {
                var editorBase = new GameObject("editor base");
                editorBase.transform.SetParent(contentBase);
                editorBase.transform.localScale = Vector3.one;
                var editorBaseRT = editorBase.AddComponent<RectTransform>();
                editorBaseRT.anchoredPosition = new Vector2(691f, -40f);
                editorBaseRT.sizeDelta = new Vector2(537f, 936f);
                var editorBaseImage = editorBase.AddComponent<Image>();
                editorBaseImage.color = new Color(0.078f, 0.067f, 0.067f, 1f);

                EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Background_3, editorBase, new List<Component>
                {
                    editorBaseImage,
                }));

                var editor = new GameObject("editor");
                editor.transform.SetParent(editorBaseRT);
                editor.transform.localScale = Vector3.one;
                var editorRT = editor.AddComponent<RectTransform>();
                editorRT.anchoredPosition = Vector3.zero;
                editorRT.sizeDelta = new Vector2(524f, 936f);

                var panel = new GameObject("panel");
                panel.transform.SetParent(editorRT);
                panel.transform.localScale = Vector3.one;
                var panelRT = panel.AddComponent<RectTransform>();
                panelRT.anchoredPosition = new Vector2(0f, 436f);
                panelRT.anchorMax = new Vector2(1f, 0.5f);
                panelRT.anchorMin = new Vector2(0f, 0.5f);
                panelRT.sizeDelta = new Vector2(14f, 64f);
                editorTitlePanel = panel.AddComponent<Image>();
                editorTitlePanel.color = new Color(0.310f, 0.467f, 0.737f, 1f);
                EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Null, panel, new List<Component>
                {
                    editorTitlePanel,
                }, true, 1, SpriteManager.RoundedSide.W));

                var editorTitle = baseCardPrefab.transform.Find("artist").gameObject.Duplicate(panelRT, "title");
                editorTitle.transform.AsRT().anchoredPosition = Vector2.zero;
                editorTitle.transform.AsRT().anchorMax = Vector2.one;
                editorTitle.transform.AsRT().anchorMin = Vector2.zero;
                editorTitle.transform.AsRT().sizeDelta = new Vector2(569f, 0f);
                var tmpEditorTitle = editorTitle.GetComponent<TextMeshProUGUI>();
                tmpEditorTitle.alignment = TextAlignmentOptions.Center;
                tmpEditorTitle.fontSize = 32;
                tmpEditorTitle.text = "<b>- Editor -</b>";
                panel.AddComponent<ContrastColors>().Init(tmpEditorTitle, editorTitlePanel);
                panel.SetActive(false);

                // Document
                {
                    var g1 = new GameObject("Document");
                    g1.transform.SetParent(editorRT);
                    g1.transform.localScale = Vector3.one;

                    var g1RT = g1.AddComponent<RectTransform>();
                    g1RT.anchoredPosition = new Vector2(0f, -32f);
                    g1RT.anchorMax = Vector2.one;
                    g1RT.anchorMin = Vector2.zero;
                    g1RT.sizeDelta = new Vector2(0f, -64f);

                    var vlg = g1.AddComponent<VerticalLayoutGroup>();
                    vlg.childControlHeight = false;
                    vlg.childForceExpandHeight = false;
                    vlg.spacing = 4f;

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Name";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text1 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1RT, "text");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    documentEditorName = text1.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(documentEditorName);

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Text";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text2 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1RT, "text");
                    text2.transform.AsRT().sizeDelta = new Vector2(537f, 720f);
                    text2.gameObject.SetActive(true);

                    documentEditorText = text2.GetComponent<InputField>();
                    documentEditorText.textComponent.alignment = TextAnchor.UpperLeft;
                    ((Text)documentEditorText.placeholder).alignment = TextAnchor.UpperLeft;
                    documentEditorText.lineType = InputField.LineType.MultiLineNewline;
                    EditorThemeManager.AddInputField(documentEditorText);

                    g1.SetActive(false);
                    editors.Add(g1);
                }

                // TODO
                {
                    var g1 = new GameObject("TODO");
                    g1.transform.SetParent(editorRT);
                    g1.transform.localScale = Vector3.one;

                    var g1RT = g1.AddComponent<RectTransform>();
                    g1RT.anchoredPosition = new Vector2(0f, -32f);
                    g1RT.anchorMax = Vector2.one;
                    g1RT.anchorMin = Vector2.zero;
                    g1RT.sizeDelta = new Vector2(0f, -64f);

                    var vlg = g1.AddComponent<VerticalLayoutGroup>();
                    vlg.childControlHeight = false;
                    vlg.childForceExpandHeight = false;
                    vlg.spacing = 4f;

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Text";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text1 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1RT, "text");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    todoEditorText = text1.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(todoEditorText);

                    g1.SetActive(false);
                    editors.Add(g1);
                }

                // Character
                {
                    var g1 = new GameObject("Character");
                    g1.transform.SetParent(editorRT);
                    g1.transform.localScale = Vector3.one;

                    var g1RT = g1.AddComponent<RectTransform>();
                    g1RT.anchoredPosition = new Vector2(0f, -32f);
                    g1RT.anchorMax = Vector2.one;
                    g1RT.anchorMin = Vector2.zero;
                    g1RT.sizeDelta = new Vector2(0f, -64f);

                    var vlg = g1.AddComponent<VerticalLayoutGroup>();
                    vlg.childControlHeight = false;
                    vlg.childForceExpandHeight = false;
                    vlg.spacing = 4f;

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Name";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text1 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1RT, "name");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    characterEditorName = text1.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(characterEditorName);

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Gender";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text2 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1RT, "gender");
                    text2.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text2.gameObject.SetActive(true);

                    characterEditorGender = text2.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(characterEditorGender);

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Description";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text3 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1RT, "description");
                    text3.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text3.gameObject.SetActive(true);

                    characterEditorDescription = text3.GetComponent<InputField>();
                    characterEditorDescription.textComponent.alignment = TextAnchor.UpperLeft;
                    ((Text)characterEditorDescription.placeholder).alignment = TextAnchor.UpperLeft;
                    characterEditorDescription.lineType = InputField.LineType.MultiLineNewline;
                    EditorThemeManager.AddInputField(characterEditorDescription);

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Select Profile Image";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var reload = EditorPrefabHolder.Instance.Function2Button.Duplicate(g1RT);
                    var pickProfileStorage = reload.GetComponent<FunctionButtonStorage>();
                    reload.SetActive(true);
                    reload.name = "pick profile";
                    reload.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                    reload.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                    pickProfileStorage.text.text = "Select";
                    characterEditorProfileSelector = pickProfileStorage.button;
                    EditorThemeManager.ApplySelectable(characterEditorProfileSelector, ThemeGroup.Function_2);
                    EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Function_2_Text, pickProfileStorage.text.gameObject, new List<Component>
                    {
                        pickProfileStorage.text,
                    }));

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Character Traits";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    // Character Traits
                    {
                        var tagScrollView = new GameObject("Character Traits Scroll View");
                        tagScrollView.transform.SetParent(g1RT);
                        tagScrollView.transform.localScale = Vector3.one;

                        var tagScrollViewRT = tagScrollView.AddComponent<RectTransform>();
                        tagScrollViewRT.sizeDelta = new Vector2(522f, 120f);
                        var scroll = tagScrollView.AddComponent<ScrollRect>();

                        scroll.horizontal = false;
                        scroll.vertical = true;

                        var image = tagScrollView.AddComponent<Image>();
                        image.color = new Color(1f, 1f, 1f, 0.01f);

                        var mask = tagScrollView.AddComponent<Mask>();

                        var tagViewport = new GameObject("Viewport");
                        tagViewport.transform.SetParent(tagScrollViewRT);
                        tagViewport.transform.localScale = Vector3.one;

                        var tagViewPortRT = tagViewport.AddComponent<RectTransform>();
                        tagViewPortRT.anchoredPosition = Vector2.zero;
                        tagViewPortRT.anchorMax = Vector2.one;
                        tagViewPortRT.anchorMin = Vector2.zero;
                        tagViewPortRT.sizeDelta = Vector2.zero;

                        var tagContent = new GameObject("Content");
                        tagContent.transform.SetParent(tagViewPortRT);
                        tagContent.transform.localScale = Vector3.one;

                        var tagContentRT = tagContent.AddComponent<RectTransform>();

                        var tagContentGLG = tagContent.AddComponent<GridLayoutGroup>();
                        tagContentGLG.cellSize = new Vector2(536f, 32f);
                        tagContentGLG.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                        tagContentGLG.constraintCount = 1;
                        tagContentGLG.childAlignment = TextAnchor.MiddleLeft;
                        tagContentGLG.spacing = new Vector2(8f, 8f);

                        var tagContentCSF = tagContent.AddComponent<ContentSizeFitter>();
                        tagContentCSF.horizontalFit = ContentSizeFitter.FitMode.MinSize;
                        tagContentCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

                        scroll.viewport = tagViewPortRT;
                        scroll.content = tagContentRT;

                        characterEditorTraitsContent = tagContentRT;
                    }

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Lore";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    // Lore
                    {
                        var tagScrollView = new GameObject("Lore Scroll View");
                        tagScrollView.transform.SetParent(g1RT);
                        tagScrollView.transform.localScale = Vector3.one;

                        var tagScrollViewRT = tagScrollView.AddComponent<RectTransform>();
                        tagScrollViewRT.sizeDelta = new Vector2(522f, 120f);
                        var scroll = tagScrollView.AddComponent<ScrollRect>();

                        scroll.horizontal = false;
                        scroll.vertical = true;

                        var image = tagScrollView.AddComponent<Image>();
                        image.color = new Color(1f, 1f, 1f, 0.01f);

                        var mask = tagScrollView.AddComponent<Mask>();

                        var tagViewport = new GameObject("Viewport");
                        tagViewport.transform.SetParent(tagScrollViewRT);
                        tagViewport.transform.localScale = Vector3.one;

                        var tagViewPortRT = tagViewport.AddComponent<RectTransform>();
                        tagViewPortRT.anchoredPosition = Vector2.zero;
                        tagViewPortRT.anchorMax = Vector2.one;
                        tagViewPortRT.anchorMin = Vector2.zero;
                        tagViewPortRT.sizeDelta = Vector2.zero;

                        var tagContent = new GameObject("Content");
                        tagContent.transform.SetParent(tagViewPortRT);
                        tagContent.transform.localScale = Vector3.one;

                        var tagContentRT = tagContent.AddComponent<RectTransform>();

                        var tagContentGLG = tagContent.AddComponent<GridLayoutGroup>();
                        tagContentGLG.cellSize = new Vector2(536f, 32f);
                        tagContentGLG.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                        tagContentGLG.constraintCount = 1;
                        tagContentGLG.childAlignment = TextAnchor.MiddleLeft;
                        tagContentGLG.spacing = new Vector2(8f, 8f);

                        var tagContentCSF = tagContent.AddComponent<ContentSizeFitter>();
                        tagContentCSF.horizontalFit = ContentSizeFitter.FitMode.MinSize;
                        tagContentCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

                        scroll.viewport = tagViewPortRT;
                        scroll.content = tagContentRT;

                        characterEditorLoreContent = tagContentRT;
                    }

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Abilities";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    // Abilities
                    {
                        var tagScrollView = new GameObject("Abilities Scroll View");
                        tagScrollView.transform.SetParent(g1RT);
                        tagScrollView.transform.localScale = Vector3.one;

                        var tagScrollViewRT = tagScrollView.AddComponent<RectTransform>();
                        tagScrollViewRT.sizeDelta = new Vector2(522f, 120f);
                        var scroll = tagScrollView.AddComponent<ScrollRect>();

                        scroll.horizontal = false;
                        scroll.vertical = true;

                        var image = tagScrollView.AddComponent<Image>();
                        image.color = new Color(1f, 1f, 1f, 0.01f);

                        var mask = tagScrollView.AddComponent<Mask>();

                        var tagViewport = new GameObject("Viewport");
                        tagViewport.transform.SetParent(tagScrollViewRT);
                        tagViewport.transform.localScale = Vector3.one;

                        var tagViewPortRT = tagViewport.AddComponent<RectTransform>();
                        tagViewPortRT.anchoredPosition = Vector2.zero;
                        tagViewPortRT.anchorMax = Vector2.one;
                        tagViewPortRT.anchorMin = Vector2.zero;
                        tagViewPortRT.sizeDelta = Vector2.zero;

                        var tagContent = new GameObject("Content");
                        tagContent.transform.SetParent(tagViewPortRT);
                        tagContent.transform.localScale = Vector3.one;

                        var tagContentRT = tagContent.AddComponent<RectTransform>();

                        var tagContentGLG = tagContent.AddComponent<GridLayoutGroup>();
                        tagContentGLG.cellSize = new Vector2(536f, 32f);
                        tagContentGLG.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                        tagContentGLG.constraintCount = 1;
                        tagContentGLG.childAlignment = TextAnchor.MiddleLeft;
                        tagContentGLG.spacing = new Vector2(8f, 8f);

                        var tagContentCSF = tagContent.AddComponent<ContentSizeFitter>();
                        tagContentCSF.horizontalFit = ContentSizeFitter.FitMode.MinSize;
                        tagContentCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

                        scroll.viewport = tagViewPortRT;
                        scroll.content = tagContentRT;

                        characterEditorAbilitiesContent = tagContentRT;
                    }

                    // Tag Prefab
                    {
                        tagPrefab = new GameObject("Tag");
                        tagPrefab.transform.SetParent(assetsParent);
                        var tagPrefabRT = tagPrefab.AddComponent<RectTransform>();
                        var tagPrefabImage = tagPrefab.AddComponent<Image>();
                        tagPrefabImage.color = new Color(1f, 1f, 1f, 0.12f);
                        var tagPrefabLayout = tagPrefab.AddComponent<HorizontalLayoutGroup>();
                        tagPrefabLayout.childControlWidth = false;
                        tagPrefabLayout.childForceExpandWidth = false;

                        var input = RTEditor.inst.defaultIF.Duplicate(tagPrefabRT, "Input");
                        input.transform.localScale = Vector3.one;
                        ((RectTransform)input.transform).sizeDelta = new Vector2(500f, 32f);
                        var text = input.transform.Find("Text").GetComponent<Text>();
                        text.alignment = TextAnchor.MiddleLeft;
                        text.fontSize = 17;

                        var delete = EditorPrefabHolder.Instance.DeleteButton.gameObject.Duplicate(tagPrefabRT, "Delete");
                        UIManager.SetRectTransform(delete.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.one, Vector2.one, new Vector2(32f, 32f));
                    }

                    g1.SetActive(false);
                    editors.Add(g1);
                }

                // Timeline
                {
                    var g1 = new GameObject("Timeline");
                    g1.transform.SetParent(editorRT);
                    g1.transform.localScale = Vector3.one;

                    var g1RT = g1.AddComponent<RectTransform>();
                    g1RT.anchoredPosition = new Vector2(0f, -32f);
                    g1RT.anchorMax = Vector2.one;
                    g1RT.anchorMin = Vector2.zero;
                    g1RT.sizeDelta = new Vector2(0f, -64f);

                    var vlg = g1.AddComponent<VerticalLayoutGroup>();
                    vlg.childControlHeight = false;
                    vlg.childForceExpandHeight = false;
                    vlg.spacing = 4f;

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Name";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text1 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1RT, "text");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    timelineEditorName = text1.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(timelineEditorName);

                    g1.SetActive(false);
                    editors.Add(g1);
                }

                // Event
                {
                    var g1 = new GameObject("Event");
                    g1.transform.SetParent(editorRT);
                    g1.transform.localScale = Vector3.one;

                    var g1RT = g1.AddComponent<RectTransform>();
                    g1RT.anchoredPosition = new Vector2(0f, -32f);
                    g1RT.anchorMax = Vector2.one;
                    g1RT.anchorMin = Vector2.zero;
                    g1RT.sizeDelta = new Vector2(0f, -64f);

                    var vlg = g1.AddComponent<VerticalLayoutGroup>();
                    vlg.childControlHeight = false;
                    vlg.childForceExpandHeight = false;
                    vlg.spacing = 4f;

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Name";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text1 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1RT, "name");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    eventEditorName = text1.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(eventEditorName);

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Description";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text2 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1RT, "description");
                    text2.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text2.gameObject.SetActive(true);

                    eventEditorDescription = text2.GetComponent<InputField>();
                    eventEditorDescription.textComponent.alignment = TextAnchor.UpperLeft;
                    ((Text)eventEditorDescription.placeholder).alignment = TextAnchor.UpperLeft;
                    eventEditorDescription.lineType = InputField.LineType.MultiLineNewline;
                    EditorThemeManager.AddInputField(eventEditorDescription);

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Path";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text3 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1RT, "path");
                    text3.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text3.gameObject.SetActive(true);

                    eventEditorPath = text3.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(eventEditorPath);

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Type";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var renderType = ObjEditor.inst.ObjectView.transform.Find("autokill/tod-dropdown").gameObject.Duplicate(g1RT, "type");
                    eventEditorType = renderType.GetComponent<Dropdown>();
                    eventEditorType.options.Clear();
                    eventEditorType.options = new List<Dropdown.OptionData>
                    {
                        new Dropdown.OptionData("Level"),
                        new Dropdown.OptionData("Cutscene"),
                        new Dropdown.OptionData("Story"),
                    };
                    EditorThemeManager.AddDropdown(eventEditorType);

                    g1.SetActive(false);
                    editors.Add(g1);
                }

                // Schedule
                {
                    var g1 = new GameObject("Event");
                    g1.transform.SetParent(editorRT);
                    g1.transform.localScale = Vector3.one;

                    var g1RT = g1.AddComponent<RectTransform>();
                    g1RT.anchoredPosition = new Vector2(0f, -32f);
                    g1RT.anchorMax = Vector2.one;
                    g1RT.anchorMin = Vector2.zero;
                    g1RT.sizeDelta = new Vector2(0f, -64f);

                    var vlg = g1.AddComponent<VerticalLayoutGroup>();
                    vlg.childControlHeight = false;
                    vlg.childForceExpandHeight = false;
                    vlg.spacing = 4f;

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Name";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text1 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1RT, "name");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    scheduleEditorDescription = text1.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(scheduleEditorDescription);

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Year";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text2 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1RT, "year");
                    text2.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text2.gameObject.SetActive(true);

                    scheduleEditorYear = text2.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(scheduleEditorYear);

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Month";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var renderType = ObjEditor.inst.ObjectView.transform.Find("autokill/tod-dropdown").gameObject.Duplicate(g1RT, "month");
                    scheduleEditorMonth = renderType.GetComponent<Dropdown>();
                    scheduleEditorMonth.options.Clear();
                    scheduleEditorMonth.options = new List<Dropdown.OptionData>
                    {
                        new Dropdown.OptionData("January"),
                        new Dropdown.OptionData("February"),
                        new Dropdown.OptionData("March"),
                        new Dropdown.OptionData("April"),
                        new Dropdown.OptionData("May"),
                        new Dropdown.OptionData("June"),
                        new Dropdown.OptionData("July"),
                        new Dropdown.OptionData("August"),
                        new Dropdown.OptionData("September"),
                        new Dropdown.OptionData("October"),
                        new Dropdown.OptionData("November"),
                        new Dropdown.OptionData("December"),
                    };
                    EditorThemeManager.AddDropdown(scheduleEditorMonth);

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Day";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text3 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1RT, "day");
                    text3.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text3.gameObject.SetActive(true);

                    scheduleEditorDay = text3.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(scheduleEditorDay);

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Hour";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text4 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1RT, "hour");
                    text4.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text4.gameObject.SetActive(true);

                    scheduleEditorHour = text4.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(scheduleEditorHour);

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Minute";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text5 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1RT, "minute");
                    text5.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text5.gameObject.SetActive(true);

                    scheduleEditorMinute = text5.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(scheduleEditorMinute);

                    g1.SetActive(false);
                    editors.Add(g1);
                }

                // Note
                {
                    var g1 = new GameObject("Note");
                    g1.transform.SetParent(editorRT);
                    g1.transform.localScale = Vector3.one;

                    var g1RT = g1.AddComponent<RectTransform>();
                    g1RT.anchoredPosition = new Vector2(0f, -32f);
                    g1RT.anchorMax = Vector2.one;
                    g1RT.anchorMin = Vector2.zero;
                    g1RT.sizeDelta = new Vector2(0f, -64f);

                    var vlg = g1.AddComponent<VerticalLayoutGroup>();
                    vlg.childControlHeight = false;
                    vlg.childForceExpandHeight = false;
                    vlg.spacing = 4f;

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Name";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text1 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1RT, "text");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    noteEditorName = text1.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(noteEditorName);

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Text";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text2 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1RT, "text");
                    text2.transform.AsRT().sizeDelta = new Vector2(537f, 360f);
                    text2.gameObject.SetActive(true);

                    noteEditorText = text2.GetComponent<InputField>();
                    noteEditorText.textComponent.alignment = TextAnchor.UpperLeft;
                    ((Text)noteEditorText.placeholder).alignment = TextAnchor.UpperLeft;
                    noteEditorText.lineType = InputField.LineType.MultiLineNewline;
                    EditorThemeManager.AddInputField(noteEditorText);

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Color";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var colorBase = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/color/color");
                    this.colorBase = colorBase.transform;
                    var colors = colorBase.Duplicate(g1RT, "colors");
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

                        EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Null, col, new List<Component>
                        {
                            toggle.image,
                        }, true, 1, SpriteManager.RoundedSide.W));

                        EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Background_3, toggle.graphic.gameObject, new List<Component>
                        {
                            toggle.graphic,
                        }));
                    }

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(300f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Reset Position and Scale";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var reset = EditorPrefabHolder.Instance.Function2Button.gameObject.Duplicate(g1RT);
                    var resetStorage = reset.GetComponent<FunctionButtonStorage>();
                    reset.SetActive(true);
                    reset.name = "reset";
                    reset.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                    reset.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                    resetStorage.text.text = "Reset";
                    noteEditorReset = resetStorage.button;
                    EditorThemeManager.ApplySelectable(noteEditorReset, ThemeGroup.Function_2);
                    EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Function_2_Text, resetStorage.text.gameObject, new List<Component>
                    {
                        resetStorage.text,
                    }));

                    g1.SetActive(false);
                    editors.Add(g1);
                }

                // OST
                {
                    var g1 = new GameObject("OST");
                    g1.transform.SetParent(editorRT);
                    g1.transform.localScale = Vector3.one;

                    var g1RT = g1.AddComponent<RectTransform>();
                    g1RT.anchoredPosition = new Vector2(0f, -32f);
                    g1RT.anchorMax = Vector2.one;
                    g1RT.anchorMin = Vector2.zero;
                    g1RT.sizeDelta = new Vector2(0f, -64f);

                    var vlg = g1.AddComponent<VerticalLayoutGroup>();
                    vlg.childControlHeight = false;
                    vlg.childForceExpandHeight = false;
                    vlg.spacing = 4f;

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Path";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text1 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1RT, "path");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    ostEditorPath = text1.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(ostEditorPath);

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Name";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text2 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1RT, "name");
                    text2.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text2.gameObject.SetActive(true);

                    ostEditorName = text2.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(ostEditorName);

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(334.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Start Playing OST From Here";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var play = EditorPrefabHolder.Instance.Function2Button.Duplicate(g1RT);
                    var playStorage = play.GetComponent<FunctionButtonStorage>();
                    play.SetActive(true);
                    play.name = "play";
                    play.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                    play.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                    playStorage.text.text = "Play";
                    ostEditorPlay = playStorage.button;
                    EditorThemeManager.ApplySelectable(ostEditorPlay, ThemeGroup.Function_2);
                    EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Function_2_Text, playStorage.text.gameObject, new List<Component>
                    {
                        playStorage.text,
                    }));

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Stop Playing";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var stop = EditorPrefabHolder.Instance.Function2Button.Duplicate(g1RT);
                    var stopStorage = stop.GetComponent<FunctionButtonStorage>();
                    stop.SetActive(true);
                    stop.name = "stop";
                    stop.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                    stop.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                    stopStorage.text.text = "Stop";
                    ostEditorStop = stopStorage.button;
                    EditorThemeManager.ApplySelectable(ostEditorStop, ThemeGroup.Function_2);
                    EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Function_2_Text, stopStorage.text.gameObject, new List<Component>
                    {
                        stopStorage.text,
                    }));

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Use Global Path";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var global = EditorPrefabHolder.Instance.Function2Button.Duplicate(g1RT);
                    var globalStorage = global.GetComponent<FunctionButtonStorage>();
                    global.SetActive(true);
                    global.name = "global";
                    global.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                    global.transform.AsRT().sizeDelta = new Vector2(200f, 32f);

                    ostEditorUseGlobal = globalStorage.button;
                    ostEditorUseGlobalText = globalStorage.text;
                    ostEditorUseGlobalText.text = "False";
                    EditorThemeManager.ApplySelectable(ostEditorUseGlobal, ThemeGroup.Function_2);
                    EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Function_2_Text, ostEditorUseGlobalText.gameObject, new List<Component>
                    {
                        ostEditorUseGlobalText,
                    }));

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1RT, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Index";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text3 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1RT, "index");
                    text3.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text3.gameObject.SetActive(true);

                    ostEditorIndex = text3.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(ostEditorIndex);

                    g1.SetActive(false);
                    editors.Add(g1);
                }
            }

            RenderTabs();
            Load();
        }

        public Image editorTitlePanel;

        public InputField documentEditorName;
        public InputField documentEditorText;

        public InputField todoEditorText;

        public InputField characterEditorName;
        public InputField characterEditorGender;
        public InputField characterEditorDescription;
        public Button characterEditorProfileSelector;
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
        public InputField ostEditorIndex;

        public List<GameObject> editors = new List<GameObject>();

        void Start()
        {

        }

        void Update()
        {
            foreach (var note in planners.Where(x => x.PlannerType == PlannerItem.Type.Note && x is NoteItem).Select(x => x as NoteItem))
            {
                note.TopBar?.SetColor(note.TopColor);
                if (note.TitleUI)
                    note.TitleUI.color = CoreHelper.InvertColorHue(CoreHelper.InvertColorValue(note.TopColor));

                if (!PlannerActive || CurrentTab != 5)
                    note.GameObject?.SetActive(note.Active);

                note.ActiveUI?.gameObject?.SetActive(PlannerActive && CurrentTab == 5);

                var currentParent = !PlannerActive || CurrentTab != 5 ? notesParent : content;

                if (note.GameObject && note.GameObject.transform.parent != (currentParent))
                {
                    note.GameObject.transform.SetParent(currentParent);
                    if (PlannerActive && CurrentTab == 5)
                        note.GameObject.transform.localScale = Vector3.one;
                }

                if (!note.Dragging && note.GameObject && (!PlannerActive || CurrentTab != 5))
                {
                    note.GameObject.transform.localPosition = note.Position;
                    note.GameObject.transform.localScale = note.Scale;
                    note.GameObject.transform.AsRT().sizeDelta = note.Size;
                }

                if (note.GameObject && note.GameObject.transform.Find("panel/edit"))
                {
                    note.GameObject.transform.Find("panel/edit").gameObject.SetActive(!PlannerActive || CurrentTab != 5);
                }
            }

            if (EditorManager.inst.editorState == EditorManager.EditorState.Main)
                return;

            if (OSTAudioSource)
                OSTAudioSource.volume = AudioManager.inst.musicVol;

            var list = planners.Where(x => x.PlannerType == PlannerItem.Type.OST && x is OSTItem).Select(x => x as OSTItem).ToList();
            if (OSTAudioSource && OSTAudioSource.clip && OSTAudioSource.time > OSTAudioSource.clip.length - 0.1f && playing)
            {
                int num = 1;
                // Here we skip any OST where a song file does not exist.
                while (currentOST + num < list.Count && !list[num].Valid)
                    num++;

                list[currentOST].playing = false;
                playing = false;

                if (currentOST + num >= list.Count)
                    return;

                list[currentOST + num].Play();
            }
        }

        #region Save / Load

        public void Load()
        {
            planners.Clear();
            LSHelpers.DeleteChildren(content);

            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (string.IsNullOrEmpty(Path.GetFileName(path)))
                return;

            if (!RTFile.DirectoryExists(path))
                Directory.CreateDirectory(path);

            LoadDocuments();
            LoadTODO();
            LoadTimelines();
            LoadSchedules();
            LoadNotes();
            LoadOST();

            if (!RTFile.DirectoryExists(path + "/characters"))
            {
                Directory.CreateDirectory(path + "/characters");
            }
            else
            {
                var directories = Directory.GetDirectories(path + "/characters", "*", SearchOption.TopDirectoryOnly);
                if (directories.Length > 0)
                {
                    foreach (var folder in directories)
                    {
                        var character = new CharacterItem(folder);
                        planners.Add(character);
                        GenerateCharacter(character);
                    }
                }
            }

            RefreshList();
        }

        public void SaveDocuments()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = JSON.Parse("{}");

            var list = planners.Where(x => x.PlannerType == PlannerItem.Type.Document && x is DocumentItem).Select(x => x as DocumentItem).ToList();

            for (int i = 0; i < list.Count; i++)
            {
                var document = list[i];
                jn["documents"][i]["name"] = document.Name;
                jn["documents"][i]["text"] = document.Text;
            }

            RTFile.WriteToFile(path + "/documents.lsn", jn.ToString(3));
        }

        public void LoadDocuments()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (!RTFile.FileExists(path + "/documents.lsn"))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path + "/documents.lsn"));

            for (int i = 0; i < jn["documents"].Count; i++)
            {
                var document = new DocumentItem();

                document.Name = jn["documents"][i]["name"];
                document.Text = jn["documents"][i]["text"];

                GenerateDocument(document);

                planners.Add(document);
            }
        }

        public void SaveTODO()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = JSON.Parse("{}");

            var list = planners.Where(x => x.PlannerType == PlannerItem.Type.TODO && x is TODOItem).Select(x => x as TODOItem).OrderBy(x => x.Priority).ToList();

            for (int i = 0; i < list.Count; i++)
            {
                jn["todo"][i]["ch"] = list[i].Checked.ToString();
                jn["todo"][i]["pr"] = list[i].Priority.ToString();
                jn["todo"][i]["text"] = list[i].Text;
            }

            RTFile.WriteToFile(path + "/todo.lsn", jn.ToString(3));
        }

        public void LoadTODO()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (!RTFile.FileExists(path + "/todo.lsn"))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path + "/todo.lsn"));

            var todos = new List<TODOItem>();
            for (int i = 0; i < jn["todo"].Count; i++)
            {
                var todo = new TODOItem();
                todo.Checked = jn["todo"][i]["ch"].AsBool;
                todo.Priority = jn["todo"][i]["pr"].AsInt;
                todo.Text = jn["todo"][i]["text"];
                todos.Add(todo);
            }

            todos = todos.OrderBy(x => x.Priority).ToList();

            todos.ForEach(x =>
            {
                GenerateTODO(x);
            });

            planners.AddRange(todos);

            todos.Clear();
            todos = null;
        }

        public void SaveTimelines()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = JSON.Parse("{}");

            var list = planners.Where(x => x.PlannerType == PlannerItem.Type.Timeline && x is TimelineItem).Select(x => x as TimelineItem).ToList();

            for (int i = 0; i < list.Count; i++)
            {
                jn["timelines"][i]["name"] = list[i].Name;

                for (int j = 0; j < list[i].Levels.Count; j++)
                {
                    var level = list[i].Levels[j];
                    jn["timelines"][i]["levels"][j]["n"] = level.Name;
                    jn["timelines"][i]["levels"][j]["p"] = level.Path;
                    jn["timelines"][i]["levels"][j]["t"] = ((int)level.ElementType).ToString();
                    jn["timelines"][i]["levels"][j]["d"] = level.Description;
                }
            }

            RTFile.WriteToFile(path + "/timelines.lsn", jn.ToString(3));
        }

        public void LoadTimelines()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (!RTFile.FileExists(path + "/timelines.lsn"))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path + "/timelines.lsn"));

            for (int i = 0; i < jn["timelines"].Count; i++)
            {
                var timeline = new TimelineItem();

                timeline.Name = jn["timelines"][i]["name"];

                for (int j = 0; j < jn["timelines"][i]["levels"].Count; j++)
                {
                    timeline.Levels.Add(new TimelineItem.Event
                    {
                        Name = jn["timelines"][i]["levels"][j]["n"],
                        Path = jn["timelines"][i]["levels"][j]["p"],
                        ElementType = (TimelineItem.Event.Type)jn["timelines"][i]["levels"][j]["t"].AsInt,
                        Description = jn["timelines"][i]["levels"][j]["d"],
                    });
                }

                GenerateTimeline(timeline);

                planners.Add(timeline);
            }
        }

        public void SaveSchedules()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = JSON.Parse("{}");

            var list = planners.Where(x => x.PlannerType == PlannerItem.Type.Schedule && x is ScheduleItem).Select(x => x as ScheduleItem).ToList();

            for (int i = 0; i < list.Count; i++)
            {
                var schedule = list[i];
                jn["schedules"][i]["date"] = schedule.Date;
                jn["schedules"][i]["desc"] = schedule.Description;
                if (schedule.hasBeenChecked)
                    jn["schedules"][i]["checked"] = schedule.hasBeenChecked;
            }

            RTFile.WriteToFile(path + "/schedules.lsn", jn.ToString(3));
        }

        public void LoadSchedules()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (!RTFile.FileExists(path + "/schedules.lsn"))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path + "/schedules.lsn"));

            for (int i = 0; i < jn["schedules"].Count; i++)
            {
                var schedule = new ScheduleItem();
                schedule.Date = jn["schedules"][i]["date"];
                schedule.Description = jn["schedules"][i]["desc"];
                if (jn["schedules"][i]["checked"] != null)
                    schedule.hasBeenChecked = jn["schedules"][i]["checked"].AsBool;

                if (DateTime.TryParse(schedule.Date, out DateTime dateTime))
                    schedule.DateTime = dateTime;

                GenerateSchedule(schedule);

                planners.Add(schedule);
            }
        }

        public void SaveNotes()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = JSON.Parse("{}");

            var list = planners.Where(x => x.PlannerType == PlannerItem.Type.Note && x is NoteItem).Select(x => x as NoteItem).ToList();

            for (int i = 0; i < list.Count; i++)
            {
                var note = list[i];

                jn["notes"][i]["active"] = note.Active.ToString();
                jn["notes"][i]["name"] = note.Name;
                jn["notes"][i]["pos"]["x"] = note.Position.x.ToString();
                jn["notes"][i]["pos"]["y"] = note.Position.y.ToString();
                jn["notes"][i]["sca"]["x"] = note.Scale.x.ToString();
                jn["notes"][i]["sca"]["y"] = note.Scale.y.ToString();
                jn["notes"][i]["size"]["x"] = note.Size.x.ToString();
                jn["notes"][i]["size"]["y"] = note.Size.y.ToString();
                jn["notes"][i]["col"] = note.Color.ToString();
                jn["notes"][i]["text"] = note.Text;
            }

            RTFile.WriteToFile(path + "/notes.lsn", jn.ToString(3));
        }

        public void LoadNotes()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (!RTFile.FileExists(path + "/notes.lsn"))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path + "/notes.lsn"));

            for (int i = 0; i < jn["notes"].Count; i++)
            {
                var note = new NoteItem();

                note.Active = jn["notes"][i]["active"].AsBool;
                if (!string.IsNullOrEmpty(jn["notes"][i]["name"]))
                    note.Name = jn["notes"][i]["name"];
                else
                    note.Name = "";

                note.Position = new Vector2(jn["notes"][i]["pos"]["x"].AsFloat, jn["notes"][i]["pos"]["y"].AsFloat);
                note.Scale = new Vector2(jn["notes"][i]["sca"]["x"].AsFloat, jn["notes"][i]["sca"]["y"].AsFloat);
                if (jn["notes"][i]["size"] != null)
                    note.Size = new Vector2(jn["notes"][i]["size"]["x"].AsFloat, jn["notes"][i]["size"]["y"].AsFloat);
                note.Text = jn["notes"][i]["text"];
                note.Color = jn["notes"][i]["col"].AsInt;

                GenerateNote(note);

                planners.Add(note);
            }
        }

        public void SaveOST()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = JSON.Parse("{}");

            var list = planners.Where(x => x.PlannerType == PlannerItem.Type.OST && x is OSTItem).Select(x => x as OSTItem).ToList();

            for (int i = 0; i < list.Count; i++)
            {
                var ost = list[i];
                ost.Index = i;

                jn["ost"][i]["name"] = ost.Name;
                jn["ost"][i]["path"] = ost.Path.ToString();
                jn["ost"][i]["use_global"] = ost.UseGlobal.ToString();
                jn["ost"][i]["index"] = ost.Index.ToString();
            }

            RTFile.WriteToFile(path + "/ost.lsn", jn.ToString(3));
        }

        public void LoadOST()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (!RTFile.FileExists(path + "/ost.lsn"))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path + "/ost.lsn"));

            for (int i = 0; i < jn["ost"].Count; i++)
            {
                var ost = new OSTItem();

                ost.Name = jn["ost"][i]["name"];
                ost.Path = jn["ost"][i]["path"];
                ost.UseGlobal = jn["ost"][i]["use_global"].AsBool;
                ost.Index = jn["ost"][i]["index"].AsInt;

                GenerateOST(ost);

                planners.Add(ost);
            }
        }

        #endregion

        #region Refresh GUI

        public void RenderTabs()
        {
            contentLayout.cellSize = tabCellSizes[CurrentTab];
            contentLayout.constraintCount = tabConstraintCounts[CurrentTab];
            documentFullView.SetActive(false);
            int num = 0;
            foreach (var tab in tabs)
            {
                int index = num;

                tab.onValueChanged.ClearAll();
                tab.isOn = index == CurrentTab;
                tab.onValueChanged.AddListener(delegate (bool value)
                {
                    CurrentTab = index;
                    RenderTabs();
                    RefreshList();
                });

                num++;
            }
            SetEditorsInactive();
        }

        public void RefreshList()
        {
            foreach (var plan in planners)
            {
                plan.GameObject?.SetActive(plan.PlannerType == (PlannerItem.Type)CurrentTab && (string.IsNullOrEmpty(SearchTerm) ||
                    plan is DocumentItem document && !string.IsNullOrEmpty(document.Name) && document.Name.ToLower().Contains(SearchTerm.ToLower()) ||
                    plan is TODOItem todo && (CheckOn(SearchTerm.ToLower()) && todo.Checked || CheckOff(SearchTerm.ToLower()) && !todo.Checked || todo.Text.ToLower().Contains(SearchTerm.ToLower())) ||
                    plan is CharacterItem character && (!string.IsNullOrEmpty(character.Name) && character.Name.ToLower().Contains(SearchTerm.ToLower()) || !string.IsNullOrEmpty(character.Description) && character.Description.ToLower().Contains(SearchTerm.ToLower())) ||
                    plan is TimelineItem timeline && timeline.Levels.Has(x => x.Name.ToLower().Contains(SearchTerm.ToLower())) ||
                    plan is ScheduleItem schedule && (!string.IsNullOrEmpty(schedule.Description) && schedule.Description.ToLower().Contains(SearchTerm.ToLower()) || schedule.Date.ToLower().Contains(SearchTerm.ToLower())) ||
                    plan is NoteItem note && !string.IsNullOrEmpty(note.Text) && note.Text.ToLower().Contains(SearchTerm.ToLower()) ||
                    plan is OSTItem ost && !string.IsNullOrEmpty(ost.Name) && ost.Name.ToLower().Contains(SearchTerm.ToLower())));
            }
        }

        bool CheckOn(string searchTerm)
            => searchTerm == "\"true\"" || searchTerm == "\"on\"" || searchTerm == "\"done\"" || searchTerm == "\"finished\"" || searchTerm == "\"checked\"";

        bool CheckOff(string searchTerm)
            => searchTerm == "\"false\"" || searchTerm == "\"off\"" || searchTerm == "\"not done\"" || searchTerm == "\"not finished\"" || searchTerm == "\"unfinished\"" || searchTerm == "\"unchecked\"";

        public void SetEditorsInactive()
        {
            for (int i = 0; i < editors.Count; i++)
                editors[i].SetActive(false);
            editorTitlePanel.gameObject.SetActive(false);
        }

        public bool DocumentFullViewActive { get; set; }
        public void OpenDocumentEditor(DocumentItem document)
        {
            editors[0].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_1];

            documentEditorName.onValueChanged.ClearAll();
            documentEditorName.onEndEdit.ClearAll();
            documentEditorName.text = document.Name;
            documentEditorName.onValueChanged.AddListener(delegate (string _val)
            {
                document.Name = _val;
                document.NameUI.text = _val;
                documentTitle.text = document.Name;
            });
            documentEditorName.onEndEdit.AddListener(delegate (string _val)
            {
                SaveDocuments();
            });

            HandleDocumentEditor(document);
            HandleDocumentEditorPreview(document);
        }

        void HandleDocumentEditor(DocumentItem document)
        {
            documentEditorText.onValueChanged.ClearAll();
            documentEditorText.onEndEdit.ClearAll();
            documentEditorText.text = document.Text;
            documentEditorText.onValueChanged.AddListener(delegate (string _val)
            {
                document.Text = _val;
                document.TextUI.text = _val;

                HandleDocumentEditorPreview(document);
            });
            documentEditorText.onEndEdit.AddListener(delegate (string _val)
            {
                SaveDocuments();
            });
        }

        void HandleDocumentEditorPreview(DocumentItem document)
        {
            DocumentFullViewActive = true;
            documentFullView.SetActive(true);
            documentTitle.text = document.Name;
            documentInputField.onValueChanged.RemoveAllListeners();
            documentInputField.onEndEdit.RemoveAllListeners();
            documentInputField.text = document.Text;
            documentInputField.onValueChanged.AddListener(delegate (string _val)
            {
                document.Text = _val;
                document.TextUI.text = _val;

                HandleDocumentEditor(document);
            });
            documentInputField.onEndEdit.AddListener(delegate (string _val)
            {
                SaveDocuments();
            });
        }

        public void OpenTODOEditor(TODOItem todo)
        {
            editors[1].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_2];

            todoEditorText.onValueChanged.ClearAll();
            todoEditorText.text = todo.Text;
            todoEditorText.onValueChanged.AddListener(delegate (string _val)
            {
                todo.Text = _val;
                todo.TextUI.text = _val;
            });
            todoEditorText.onEndEdit.ClearAll();
            todoEditorText.onEndEdit.AddListener(delegate (string _val)
            {
                SaveTODO();
            });
        }

        public void OpenCharacterEditor(CharacterItem character)
        {
            editors[2].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_3];

            characterEditorName.onValueChanged.ClearAll();
            characterEditorName.text = character.Name;
            characterEditorName.onValueChanged.AddListener(delegate (string _val)
            {
                character.Name = _val;
                character.DetailsUI.text = character.FormatDetails;
            });
            characterEditorName.onEndEdit.ClearAll();
            characterEditorName.onEndEdit.AddListener(delegate (string _val)
            {
                character.Save();
            });

            characterEditorGender.onValueChanged.ClearAll();
            characterEditorGender.text = character.Gender;
            characterEditorGender.onValueChanged.AddListener(delegate (string _val)
            {
                character.Gender = _val;
                character.DetailsUI.text = character.FormatDetails;
            });
            characterEditorGender.onEndEdit.ClearAll();
            characterEditorGender.onEndEdit.AddListener(delegate (string _val)
            {
                character.Save();
            });

            characterEditorDescription.onValueChanged.ClearAll();
            characterEditorDescription.text = character.Description;
            characterEditorDescription.onValueChanged.AddListener(delegate (string _val)
            {
                character.Description = _val;
                character.DescriptionUI.text = character.Description;
            });
            characterEditorDescription.onEndEdit.ClearAll();
            characterEditorDescription.onEndEdit.AddListener(delegate (string _val)
            {
                character.Save();
            });

            characterEditorProfileSelector.onClick.ClearAll();
            characterEditorProfileSelector.onClick.AddListener(delegate ()
            {
                var editorPath = RTFile.ApplicationDirectory;
                string jpgFile = FileBrowser.OpenSingleFile("Select an image!", editorPath, new string[] { "png", "jpg" });

                if (!string.IsNullOrEmpty(jpgFile))
                {
                    character.CharacterSprite = SpriteManager.LoadSprite(jpgFile);
                    character.ProfileUI.sprite = character.CharacterSprite;
                    character.Save();
                }

                //EditorManager.inst.ShowDialog("Browser Popup");
                //RTFileBrowser.inst.UpdateBrowser(Directory.GetCurrentDirectory(), new string[] { ".png" }, onSelectFile: delegate (string _val)
                //{
                //    character.CharacterSprite = SpriteManager.LoadSprite(_val);

                //    character.Save();

                //    EditorManager.inst.HideDialog("Browser Popup");
                //});
            });

            // Character Traits
            {
                LSHelpers.DeleteChildren(characterEditorTraitsContent);

                int num = 0;
                foreach (var tag in character.CharacterTraits)
                {
                    int index = num;
                    var gameObject = tagPrefab.Duplicate(characterEditorTraitsContent, index.ToString());
                    gameObject.transform.localScale = Vector3.one;
                    var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                    input.onValueChanged.ClearAll();
                    input.onEndEdit.ClearAll();
                    input.text = tag;
                    input.onValueChanged.AddListener(delegate (string _val)
                    {
                        character.CharacterTraits[index] = _val;
                        character.DetailsUI.text = character.FormatDetails;
                    });
                    input.onEndEdit.AddListener(delegate (string _val)
                    {
                        character.Save();
                    });

                    var delete = gameObject.transform.Find("Delete").GetComponent<Button>();
                    delete.onClick.ClearAll();
                    delete.onClick.AddListener(delegate ()
                    {
                        character.CharacterTraits.RemoveAt(index);
                        character.DetailsUI.text = character.FormatDetails;
                        character.Save();
                        OpenCharacterEditor(character);
                    });

                    num++;
                }

                var add = PrefabEditor.inst.CreatePrefab.Duplicate(characterEditorTraitsContent, "Add");
                add.transform.localScale = Vector3.one;
                add.transform.Find("Text").GetComponent<Text>().text = "Add Trait";
                var addButton = add.GetComponent<Button>();
                addButton.onClick.ClearAll();
                addButton.onClick.AddListener(delegate ()
                {
                    character.CharacterTraits.Add("New Detail");
                    character.DetailsUI.text = character.FormatDetails;
                    character.Save();
                    OpenCharacterEditor(character);
                });
            }

            // Lore
            {
                LSHelpers.DeleteChildren(characterEditorLoreContent);

                int num = 0;
                foreach (var tag in character.CharacterLore)
                {
                    int index = num;
                    var gameObject = tagPrefab.Duplicate(characterEditorLoreContent, index.ToString());
                    gameObject.transform.localScale = Vector3.one;
                    var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                    input.onValueChanged.ClearAll();
                    input.onEndEdit.ClearAll();
                    input.text = tag;
                    input.onValueChanged.AddListener(delegate (string _val)
                    {
                        character.CharacterLore[index] = _val;
                        character.DetailsUI.text = character.FormatDetails;
                    });
                    input.onEndEdit.AddListener(delegate (string _val)
                    {
                        character.Save();
                    });

                    var delete = gameObject.transform.Find("Delete").GetComponent<Button>();
                    delete.onClick.ClearAll();
                    delete.onClick.AddListener(delegate ()
                    {
                        character.CharacterLore.RemoveAt(index);
                        character.DetailsUI.text = character.FormatDetails;
                        character.Save();
                        OpenCharacterEditor(character);
                    });

                    num++;
                }

                var add = PrefabEditor.inst.CreatePrefab.Duplicate(characterEditorLoreContent, "Add");
                add.transform.localScale = Vector3.one;
                add.transform.Find("Text").GetComponent<Text>().text = "Add Lore";
                var addButton = add.GetComponent<Button>();
                addButton.onClick.ClearAll();
                addButton.onClick.AddListener(delegate ()
                {
                    character.CharacterLore.Add("New Detail");
                    character.DetailsUI.text = character.FormatDetails;
                    character.Save();
                    OpenCharacterEditor(character);
                });
            }

            // Abilities
            {
                LSHelpers.DeleteChildren(characterEditorAbilitiesContent);

                int num = 0;
                foreach (var tag in character.CharacterAbilities)
                {
                    int index = num;
                    var gameObject = tagPrefab.Duplicate(characterEditorAbilitiesContent, index.ToString());
                    gameObject.transform.localScale = Vector3.one;
                    var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                    input.onValueChanged.ClearAll();
                    input.onEndEdit.ClearAll();
                    input.text = tag;
                    input.onValueChanged.AddListener(delegate (string _val)
                    {
                        character.CharacterAbilities[index] = _val;
                        character.DetailsUI.text = character.FormatDetails;
                    });
                    input.onEndEdit.AddListener(delegate (string _val)
                    {
                        character.Save();
                    });

                    var delete = gameObject.transform.Find("Delete").GetComponent<Button>();
                    delete.onClick.ClearAll();
                    delete.onClick.AddListener(delegate ()
                    {
                        character.CharacterAbilities.RemoveAt(index);
                        character.DetailsUI.text = character.FormatDetails;
                        character.Save();
                        OpenCharacterEditor(character);
                    });

                    num++;
                }

                var add = PrefabEditor.inst.CreatePrefab.Duplicate(characterEditorAbilitiesContent, "Add");
                add.transform.localScale = Vector3.one;
                add.transform.Find("Text").GetComponent<Text>().text = "Add Ability";
                var addButton = add.GetComponent<Button>();
                addButton.onClick.ClearAll();
                addButton.onClick.AddListener(delegate ()
                {
                    character.CharacterAbilities.Add("New Detail");
                    character.DetailsUI.text = character.FormatDetails;
                    character.Save();
                    OpenCharacterEditor(character);
                });
            }
        }

        public void OpenTimelineEditor(TimelineItem timeline)
        {
            SetEditorsInactive();
            editors[3].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_4];

            timelineEditorName.onValueChanged.ClearAll();
            timelineEditorName.text = timeline.Name;
            timelineEditorName.onValueChanged.AddListener(delegate (string _val)
            {
                timeline.Name = _val;
                timeline.NameUI.text = _val;
            });
            timelineEditorName.onEndEdit.ClearAll();
            timelineEditorName.onEndEdit.AddListener(delegate (string _val)
            {
                SaveTimelines();
            });
        }

        public void OpenEventEditor(TimelineItem.Event level)
        {
            SetEditorsInactive();
            editors[4].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_4];

            eventEditorName.onValueChanged.ClearAll();
            eventEditorName.text = level.Name;
            eventEditorName.onValueChanged.AddListener(delegate (string _val)
            {
                level.Name = _val;
                level.NameUI.text = $"{level.ElementType}: {level.Name}";
            });
            eventEditorName.onEndEdit.ClearAll();
            eventEditorName.onEndEdit.AddListener(delegate (string _val)
            {
                SaveTimelines();
            });

            eventEditorDescription.onValueChanged.ClearAll();
            eventEditorDescription.text = level.Description;
            eventEditorDescription.onValueChanged.AddListener(delegate (string _val)
            {
                level.Description = _val;
                level.DescriptionUI.text = _val;
            });
            eventEditorDescription.onEndEdit.ClearAll();
            eventEditorDescription.onEndEdit.AddListener(delegate (string _val)
            {
                SaveTimelines();
            });

            eventEditorPath.onValueChanged.ClearAll();
            eventEditorPath.text = level.Path == null ? "" : level.Path;
            eventEditorPath.onValueChanged.AddListener(delegate (string _val)
            {
                level.Path = _val;
            });
            eventEditorPath.onEndEdit.ClearAll();
            eventEditorPath.onEndEdit.AddListener(delegate (string _val)
            {
                SaveTimelines();
            });

            eventEditorType.onValueChanged.ClearAll();
            eventEditorType.value = (int)level.ElementType;
            eventEditorType.onValueChanged.AddListener(delegate (int _val)
            {
                level.ElementType = (TimelineItem.Event.Type)_val;
                level.NameUI.text = $"{level.ElementType}: {level.Name}";
                SaveTimelines();
            });
        }

        public void OpenScheduleEditor(ScheduleItem schedule)
        {
            editors[5].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_5];

            scheduleEditorDescription.onValueChanged.ClearAll();
            scheduleEditorDescription.text = schedule.Description;
            scheduleEditorDescription.onValueChanged.AddListener(delegate (string _val)
            {
                schedule.Description = _val;
                schedule.TextUI.text = schedule.Text;
                schedule.hasBeenChecked = false;
            });
            scheduleEditorDescription.onEndEdit.ClearAll();
            scheduleEditorDescription.onEndEdit.AddListener(delegate (string _val)
            {
                SaveSchedules();
            });

            scheduleEditorYear.onValueChanged.ClearAll();
            scheduleEditorYear.text = schedule.DateTime.Year.ToString();
            scheduleEditorYear.onValueChanged.AddListener(delegate (string _val)
            {
                if (int.TryParse(_val, out int year))
                {
                    var dateTime = schedule.DateTime;

                    schedule.DateTime = DateTime.Parse(schedule.FormatDate(dateTime.Day, dateTime.Month, year, dateTime.Hour >= 12 ? dateTime.Hour - 12 : dateTime.Hour, dateTime.Minute, dateTime.Hour >= 12 && dateTime.Hour < 24 ? "PM" : "AM"));

                    schedule.Date = schedule.DateTime.ToString("g");
                    schedule.TextUI.text = schedule.Text;
                    schedule.hasBeenChecked = false;

                    SaveSchedules();
                }
            });

            scheduleEditorMonth.onValueChanged.ClearAll();
            scheduleEditorMonth.value = schedule.DateTime.Month - 1;
            scheduleEditorMonth.onValueChanged.AddListener(delegate (int _val)
            {
                var dateTime = schedule.DateTime;

                schedule.DateTime = DateTime.Parse(schedule.FormatDate(dateTime.Day, _val + 1, dateTime.Year, dateTime.Hour >= 12 ? dateTime.Hour - 12 : dateTime.Hour, dateTime.Minute, dateTime.Hour >= 12 && dateTime.Hour < 24 ? "PM" : "AM"));

                schedule.Date = schedule.DateTime.ToString("g");
                schedule.TextUI.text = schedule.Text;
                schedule.hasBeenChecked = false;

                SaveSchedules();
            });

            scheduleEditorDay.onValueChanged.ClearAll();
            scheduleEditorDay.text = schedule.DateTime.Day.ToString();
            scheduleEditorDay.onValueChanged.AddListener(delegate (string _val)
            {
                if (int.TryParse(_val, out int day))
                {
                    var dateTime = schedule.DateTime;

                    schedule.DateTime = DateTime.Parse(schedule.FormatDate(day, dateTime.Month, dateTime.Year, dateTime.Hour >= 12 ? dateTime.Hour - 12 : dateTime.Hour, dateTime.Minute, dateTime.Hour >= 12 && dateTime.Hour < 24 ? "PM" : "AM"));

                    schedule.Date = schedule.DateTime.ToString("g");
                    schedule.TextUI.text = schedule.Text;
                    schedule.hasBeenChecked = false;

                    SaveSchedules();
                }
            });

            scheduleEditorHour.onValueChanged.ClearAll();
            scheduleEditorHour.text = schedule.DateTime.Hour.ToString();
            scheduleEditorHour.onValueChanged.AddListener(delegate (string _val)
            {
                if (int.TryParse(_val, out int hour))
                {
                    var dateTime = schedule.DateTime;

                    hour = Mathf.Clamp(hour, 0, 23);

                    schedule.DateTime = DateTime.Parse(schedule.FormatDate(dateTime.Day, dateTime.Month, dateTime.Year, hour >= 12 ? hour - 12 : hour, dateTime.Minute, hour >= 12 && hour < 24 ? "PM" : "AM"));

                    schedule.Date = schedule.DateTime.ToString("g");
                    schedule.TextUI.text = schedule.Text;
                    schedule.hasBeenChecked = false;

                    SaveSchedules();
                }
            });

            scheduleEditorMinute.onValueChanged.ClearAll();
            scheduleEditorMinute.text = schedule.DateTime.Minute.ToString();
            scheduleEditorMinute.onValueChanged.AddListener(delegate (string _val)
            {
                if (int.TryParse(_val, out int minute))
                {
                    var dateTime = schedule.DateTime;

                    schedule.DateTime = DateTime.Parse(schedule.FormatDate(dateTime.Day, dateTime.Month, dateTime.Year, dateTime.Hour >= 12 ? dateTime.Hour - 12 : dateTime.Hour, minute, dateTime.Hour >= 12 && dateTime.Hour < 24 ? "PM" : "AM"));

                    schedule.Date = schedule.DateTime.ToString("g");
                    schedule.TextUI.text = schedule.Text;
                    schedule.hasBeenChecked = false;

                    SaveSchedules();
                }
            });
        }

        public void OpenNoteEditor(NoteItem note)
        {
            editors[6].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_6];

            noteEditorName.onValueChanged.ClearAll();
            noteEditorName.text = note.Name;
            noteEditorName.onValueChanged.AddListener(delegate (string _val)
            {
                note.Name = _val;
                note.TitleUI.text = $"Note - {note.Name}";
            });
            noteEditorName.onEndEdit.ClearAll();
            noteEditorName.onEndEdit.AddListener(delegate (string _val)
            {
                SaveNotes();
            });

            noteEditorText.onValueChanged.ClearAll();
            noteEditorText.text = note.Text;
            noteEditorText.onValueChanged.AddListener(delegate (string _val)
            {
                note.Text = _val;
                note.TextUI.text = _val;
            });
            noteEditorText.onEndEdit.ClearAll();
            noteEditorText.onEndEdit.AddListener(delegate (string _val)
            {
                SaveNotes();
            });

            noteEditorColors.Clear();
            LSHelpers.DeleteChildren(noteEditorColorsParent);
            for (int i = 0; i < MarkerEditor.inst.markerColors.Count; i++)
            {
                var col = colorBase.Find("1").gameObject.Duplicate(noteEditorColorsParent, (i + 1).ToString());
                col.transform.localScale = Vector3.one;
                noteEditorColors.Add(col.GetComponent<Toggle>());
            }

            SetNoteColors(note);

            noteEditorReset.onClick.ClearAll();
            noteEditorReset.onClick.AddListener(delegate ()
            {
                note.Position = Vector2.zero;
                note.Scale = Vector2.one;
                note.Size = new Vector2(300f, 150f);
                SaveNotes();
            });
        }

        public void SetNoteColors(NoteItem note)
        {
            int num = 0;
            foreach (var toggle in noteEditorColors)
            {
                int index = num;

                var color = index < MarkerEditor.inst.markerColors.Count ? MarkerEditor.inst.markerColors[index] : LSColors.red700;
                toggle.onValueChanged.ClearAll();
                toggle.isOn = index == note.Color;
                toggle.image.color = color;
                ((Image)toggle.graphic).color = new Color(0.078f, 0.067f, 0.067f, 1f);
                toggle.onValueChanged.AddListener(delegate (bool _val)
                {
                    note.Color = index;
                    SetNoteColors(note);
                });

                num++;
            }
        }

        public void OpenOSTEditor(OSTItem ost)
        {
            editors[7].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_7];

            ostEditorPath.onValueChanged.ClearAll();
            ostEditorPath.onEndEdit.ClearAll();
            ostEditorPath.text = ost.Path;
            ostEditorPath.onValueChanged.AddListener(delegate (string _val)
            {
                ost.Path = _val;
            });
            ostEditorPath.onEndEdit.AddListener(delegate (string _val)
            {
                SaveOST();
            });

            ostEditorName.onValueChanged.ClearAll();
            ostEditorName.onEndEdit.ClearAll();
            ostEditorName.text = ost.Name;
            ostEditorName.onValueChanged.AddListener(delegate (string _val)
            {
                ost.Name = _val;
                ost.TextUI.text = _val;
            });
            ostEditorName.onEndEdit.AddListener(delegate (string _val)
            {
                SaveOST();
            });

            ostEditorPlay.onClick.ClearAll();
            ostEditorPlay.onClick.AddListener(delegate ()
            {
                ost.Play();
            });

            ostEditorStop.onClick.ClearAll();
            ostEditorStop.onClick.AddListener(delegate ()
            {
                StopOST();
            });

            ostEditorUseGlobal.onClick.ClearAll();
            ostEditorUseGlobal.onClick.AddListener(delegate ()
            {
                ost.UseGlobal = !ost.UseGlobal;
                ostEditorUseGlobalText.text = ost.UseGlobal.ToString();
                SaveOST();
            });

            ostEditorUseGlobalText.text = ost.UseGlobal.ToString();

            ostEditorIndex.onValueChanged.ClearAll();
            ostEditorIndex.text = ost.Index.ToString();
            ostEditorIndex.onValueChanged.AddListener(delegate (string _val)
            {
                if (int.TryParse(_val, out int num))
                {
                    ost.Index = num;

                    var list = planners.Where(x => x.PlannerType == PlannerItem.Type.OST && x is OSTItem).Select(x => x as OSTItem).OrderBy(x => x.Index).ToList();

                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].ID == ost.ID && ostEditorIndex.text != i.ToString())
                            StartCoroutine(SetIndex(i));

                        list[i].Index = i;
                        list[i].GameObject.transform.SetSiblingIndex(i);
                    }
                }
            });
            TriggerHelper.AddEventTriggerParams(ostEditorIndex.gameObject, TriggerHelper.ScrollDeltaInt(ostEditorIndex));
        }

        IEnumerator SetIndex(int i)
        {
            yield return null;
            ostEditorIndex.text = i.ToString();
        }

        void StopOST()
        {
            Destroy(OSTAudioSource);

            var list = planners.Where(x => x.PlannerType == PlannerItem.Type.OST && x is OSTItem).Select(x => x as OSTItem).ToList();

            for (int i = 0; i < list.Count; i++)
            {
                list[i].playing = false;
            }

            playing = false;
        }

        #endregion

        #region Generate UI

        public GameObject Spacer(string name, Transform parent, Vector2 size)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            gameObject.transform.localScale = Vector3.one;

            var rt = gameObject.AddComponent<RectTransform>();

            rt.sizeDelta = size;

            return gameObject;
        }

        public GameObject GenerateTab(string name)
        {
            var gameObject = tabPrefab.Duplicate(topBarBase, name);
            gameObject.transform.localScale = Vector3.one;

            var background = gameObject.transform.Find("Background");
            var text = background.Find("Text").GetComponent<Text>();
            var image = background.GetComponent<Image>();

            text.fontSize = 26;
            text.fontStyle = FontStyle.Bold;
            text.text = name;
            gameObject.AddComponent<ContrastColors>().Init(text, image);
            var toggle = gameObject.GetComponent<Toggle>();
            tabs.Add(gameObject.GetComponent<Toggle>());

            EditorThemeManager.AddGraphic(image, EditorThemeManager.EditorTheme.GetGroup($"Tab Color {tabs.Count}"), true);
            EditorThemeManager.AddGraphic(toggle.graphic, ThemeGroup.Background_1);

            return gameObject;
        }

        public GameObject GenerateDocument(DocumentItem document)
        {
            var gameObject = prefabs[0].Duplicate(content, "document");
            gameObject.transform.localScale = Vector3.one;
            document.GameObject = gameObject;

            var button = gameObject.GetComponent<Button>();
            button.onClick.ClearAll();
            button.onClick.AddListener(delegate ()
            {
                OpenDocumentEditor(document);
            });

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);

            document.NameUI = gameObject.transform.Find("name").GetComponent<TextMeshProUGUI>();
            document.NameUI.text = document.Name;
            EditorThemeManager.ApplyLightText(document.NameUI);

            document.TextUI = gameObject.transform.Find("words").GetComponent<TextMeshProUGUI>();
            document.TextUI.text = document.Text;
            EditorThemeManager.ApplyLightText(document.TextUI);

            var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
            delete.button.onClick.ClearAll();
            delete.button.onClick.AddListener(delegate ()
            {
                planners.RemoveAll(x => x is DocumentItem && x.ID == document.ID);
                SaveDocuments();
                Destroy(gameObject);
            });

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete, delete.gameObject, new List<Component>
            {
                delete.button.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete_Text, delete.image.gameObject, new List<Component>
            {
                delete.image,
            }));

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Background_1, gameObject.transform.Find("gradient").gameObject, new List<Component>
            {
                gameObject.transform.Find("gradient").GetComponent<Image>(),
            }));

            return gameObject;
        }

        public GameObject GenerateTODO(TODOItem todo)
        {
            var gameObject = prefabs[1].Duplicate(content, "todo");
            gameObject.transform.localScale = Vector3.one;
            todo.GameObject = gameObject;

            var button = gameObject.GetComponent<Button>();
            button.onClick.ClearAll();
            button.onClick.AddListener(delegate ()
            {
                OpenTODOEditor(todo);
            });

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);

            todo.TextUI = gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>();
            todo.TextUI.text = todo.Text;
            EditorThemeManager.ApplyLightText(todo.TextUI);

            var toggle = gameObject.transform.Find("checked").GetComponent<Toggle>();
            todo.CheckedUI = toggle;
            toggle.onValueChanged.ClearAll();
            toggle.isOn = todo.Checked;
            toggle.onValueChanged.AddListener(delegate (bool _val)
            {
                todo.Checked = _val;
                SaveTODO();
            });

            EditorThemeManager.ApplyToggle(toggle);

            var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
            delete.button.onClick.ClearAll();
            delete.button.onClick.AddListener(delegate ()
            {
                planners.RemoveAll(x => x is TODOItem && x.ID == todo.ID);
                SaveTODO();
                Destroy(gameObject);
            });

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete, delete.gameObject, new List<Component>
            {
                delete.button.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete_Text, delete.image.gameObject, new List<Component>
            {
                delete.image,
            }));

            return gameObject;
        }

        public GameObject GenerateCharacter(CharacterItem character)
        {
            var gameObject = prefabs[2].Duplicate(content, "character");
            gameObject.transform.localScale = Vector3.one;
            character.GameObject = gameObject;

            var button = gameObject.GetComponent<Button>();
            button.onClick.ClearAll();
            button.onClick.AddListener(delegate ()
            {
                OpenCharacterEditor(character);
            });

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);

            character.ProfileUI = gameObject.transform.Find("profile").GetComponent<Image>();

            character.DetailsUI = gameObject.transform.Find("details").GetComponent<TextMeshProUGUI>();
            EditorThemeManager.ApplyLightText(character.DetailsUI);

            character.DescriptionUI = gameObject.transform.Find("description").GetComponent<TextMeshProUGUI>();
            EditorThemeManager.ApplyLightText(character.DescriptionUI);

            character.ProfileUI.sprite = character.CharacterSprite;
            character.DetailsUI.overflowMode = TextOverflowModes.Truncate;
            character.DetailsUI.text = character.Format(true);
            character.DescriptionUI.text = character.Description;

            var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
            delete.button.onClick.ClearAll();
            delete.button.onClick.AddListener(delegate ()
            {
                planners.RemoveAll(x => x is CharacterItem && x.ID == character.ID);

                if (RTFile.DirectoryExists(character.FullPath))
                {
                    var directory = character.FullPath;
                    var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
                    var directories = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);

                    foreach (var file in files)
                    {
                        File.Delete(file);
                    }

                    foreach (var dir in directories)
                    {
                        Directory.Delete(dir);
                    }

                    Directory.Delete(directory);
                }

                Destroy(gameObject);
            });

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete, delete.gameObject, new List<Component>
            {
                delete.button.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete_Text, delete.image.gameObject, new List<Component>
            {
                delete.image,
            }));

            return gameObject;
        }

        public GameObject GenerateTimeline(TimelineItem timeline)
        {
            var gameObject = prefabs[3].Duplicate(content, "timeline");
            gameObject.transform.localScale = Vector3.one;
            timeline.GameObject = gameObject;

            timeline.Content = gameObject.transform.Find("Scroll/Viewport/Content");

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.List_Button_1_Normal, gameObject, new List<Component>
            {
                gameObject.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.W));

            var scrollbar = gameObject.transform.Find("Scrollbar");
            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.List_Button_1_Normal, scrollbar.gameObject, new List<Component>
            {
                scrollbar.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.Bottom));

            var scrollbarHandle = scrollbar.transform.Find("Sliding Area/Handle").gameObject;
            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Scrollbar_1_Handle, scrollbarHandle.gameObject, new List<Component>
            {
                scrollbarHandle.GetComponent<Image>(),
                scrollbar.GetComponent<Scrollbar>()
            }, true, 1, SpriteManager.RoundedSide.W, true));

            timeline.NameUI = gameObject.transform.Find("name").GetComponent<TextMeshProUGUI>();
            timeline.NameUI.text = timeline.Name;
            EditorThemeManager.ApplyLightText(timeline.NameUI);

            var edit = gameObject.transform.Find("edit").GetComponent<Button>();
            edit.onClick.ClearAll();
            edit.onClick.AddListener(delegate ()
            {
                OpenTimelineEditor(timeline);
            });

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Function_3, edit.gameObject, new List<Component>
            {
                edit.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Function_3_Text, edit.transform.GetChild(0).gameObject, new List<Component>
            {
                edit.transform.GetChild(0).GetComponent<Image>(),
            }));

            var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
            delete.button.onClick.ClearAll();
            delete.button.onClick.AddListener(delegate ()
            {
                planners.RemoveAll(x => x is TimelineItem && x.ID == timeline.ID);
                SaveTimelines();
                Destroy(gameObject);
            });

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete, delete.gameObject, new List<Component>
            {
                delete.button.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete_Text, delete.image.gameObject, new List<Component>
            {
                delete.image,
            }));

            timeline.UpdateTimeline();

            return gameObject;
        }

        public GameObject GenerateSchedule(ScheduleItem schedule)
        {
            var gameObject = prefabs[4].Duplicate(content, "schedule");
            gameObject.transform.localScale = Vector3.one;
            schedule.GameObject = gameObject;

            var button = gameObject.GetComponent<Button>();
            button.onClick.ClearAll();
            button.onClick.AddListener(delegate ()
            {
                OpenScheduleEditor(schedule);
            });

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);

            schedule.TextUI = gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>();
            schedule.TextUI.text = schedule.Text;
            EditorThemeManager.ApplyLightText(schedule.TextUI);

            var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
            delete.button.onClick.ClearAll();
            delete.button.onClick.AddListener(delegate ()
            {
                planners.RemoveAll(x => x is ScheduleItem && x.ID == schedule.ID);
                SaveSchedules();
                Destroy(gameObject);
            });

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete, delete.gameObject, new List<Component>
            {
                delete.button.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete_Text, delete.image.gameObject, new List<Component>
            {
                delete.image,
            }));

            return gameObject;
        }

        public static bool DisplayEdges { get; set; }
        public GameObject GenerateNote(NoteItem note)
        {
            var gameObject = prefabs[5].Duplicate(content, "note");
            gameObject.transform.localScale = Vector3.one;
            note.GameObject = gameObject;

            var noteDraggable = gameObject.AddComponent<NoteDraggable>();
            noteDraggable.note = note;
            noteDraggable.onClick = delegate ()
            {
                OpenNoteEditor(note);
            };

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Background_3, gameObject, new List<Component>
            {
                gameObject.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.Bottom));

            // Left
            {
                var left = new GameObject("left");
                left.transform.SetParent(gameObject.transform);
                left.transform.localScale = Vector3.one;
                var leftRT = left.AddComponent<RectTransform>();
                leftRT.anchoredPosition = Vector2.zero;
                leftRT.anchorMax = new Vector2(1f, 1f);
                leftRT.anchorMin = new Vector2(1f, 0f);
                leftRT.pivot = new Vector2(0.5f, 0.5f);
                leftRT.sizeDelta = new Vector2(4f, 0f);
                var leftImage = left.AddComponent<Image>();
                leftImage.color = new Color(1f, 1f, 1f, DisplayEdges ? 1f : 0f);
                var noteDraggableLeft = left.AddComponent<NoteDraggable>();
                noteDraggableLeft.part = NoteDraggable.DragPart.Left;
                noteDraggableLeft.note = note;
            }

            // Right
            {
                var left = new GameObject("right");
                left.transform.SetParent(gameObject.transform);
                left.transform.localScale = Vector3.one;
                var leftRT = left.AddComponent<RectTransform>();
                leftRT.anchoredPosition = Vector2.zero;
                leftRT.anchorMax = new Vector2(0f, 1f);
                leftRT.anchorMin = new Vector2(0f, 0f);
                leftRT.pivot = new Vector2(0.5f, 0.5f);
                leftRT.sizeDelta = new Vector2(4f, 0f);
                var leftImage = left.AddComponent<Image>();
                leftImage.color = new Color(1f, 1f, 1f, DisplayEdges ? 1f : 0f);
                var noteDraggableLeft = left.AddComponent<NoteDraggable>();
                noteDraggableLeft.part = NoteDraggable.DragPart.Right;
                noteDraggableLeft.note = note;
            }

            // Up
            {
                var left = new GameObject("up");
                left.transform.SetParent(gameObject.transform);
                left.transform.localScale = Vector3.one;
                var leftRT = left.AddComponent<RectTransform>();
                leftRT.anchoredPosition = new Vector2(0f, 30f);
                leftRT.anchorMax = Vector2.one;
                leftRT.anchorMin = new Vector2(0f, 1f);
                leftRT.pivot = new Vector2(0.5f, 0.5f);
                leftRT.sizeDelta = new Vector2(0f, 4f);
                var leftImage = left.AddComponent<Image>();
                leftImage.color = new Color(1f, 1f, 1f, DisplayEdges ? 1f : 0f);
                var noteDraggableLeft = left.AddComponent<NoteDraggable>();
                noteDraggableLeft.part = NoteDraggable.DragPart.Up;
                noteDraggableLeft.note = note;
            }

            // Down
            {
                var left = new GameObject("down");
                left.transform.SetParent(gameObject.transform);
                left.transform.localScale = Vector3.one;
                var leftRT = left.AddComponent<RectTransform>();
                leftRT.anchoredPosition = Vector2.zero;
                leftRT.anchorMax = new Vector2(1f, 0f);
                leftRT.anchorMin = new Vector2(0f, 0f);
                leftRT.pivot = new Vector2(0.5f, 0.5f);
                leftRT.sizeDelta = new Vector2(0f, 4f);
                var leftImage = left.AddComponent<Image>();
                leftImage.color = new Color(1f, 1f, 1f, DisplayEdges ? 1f : 0f);
                var noteDraggableLeft = left.AddComponent<NoteDraggable>();
                noteDraggableLeft.part = NoteDraggable.DragPart.Down;
                noteDraggableLeft.note = note;
            }

            var edit = gameObject.transform.Find("panel/edit").GetComponent<Button>();
            edit.onClick.ClearAll();
            edit.onClick.AddListener(delegate ()
            {
                CurrentTab = 5;
                Open();
                RenderTabs();
                RefreshList();
                OpenNoteEditor(note);
            });

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Function_3, edit.gameObject, new List<Component>
            {
                edit.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Function_3_Text, edit.transform.GetChild(0).gameObject, new List<Component>
            {
                edit.transform.GetChild(0).GetComponent<Image>(),
            }));

            note.TitleUI = gameObject.transform.Find("panel/title").GetComponent<TextMeshProUGUI>();
            note.TitleUI.text = $"Note - {note.Name}";

            note.ActiveUI = gameObject.transform.Find("panel/active").GetComponent<Toggle>();
            note.TopBar = gameObject.transform.Find("panel").GetComponent<Image>();
            note.TextUI = gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>();
            note.TextUI.text = note.Text;
            EditorThemeManager.ApplyLightText(note.TextUI);

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Background_3, note.TopBar.gameObject, new List<Component>
            {
                 note.TopBar,
            }, true, 1, SpriteManager.RoundedSide.Top));
            note.TitleUI.gameObject.AddComponent<ContrastColors>().Init(note.TitleUI, note.TopBar);

            note.ActiveUI.onValueChanged.ClearAll();
            note.ActiveUI.isOn = note.Active;
            note.ActiveUI.onValueChanged.AddListener(delegate (bool _val)
            {
                note.Active = _val;
                SaveNotes();
            });

            EditorThemeManager.ApplyToggle(note.ActiveUI);

            var delete = gameObject.transform.Find("panel/delete").GetComponent<DeleteButtonStorage>();
            delete.button.onClick.ClearAll();
            delete.button.onClick.AddListener(delegate ()
            {
                planners.RemoveAll(x => x is NoteItem && x.ID == note.ID);
                SaveNotes();
                Destroy(gameObject);
            });

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete, delete.gameObject, new List<Component>
            {
                delete.button.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete_Text, delete.image.gameObject, new List<Component>
            {
                delete.image,
            }));

            var close = gameObject.transform.Find("panel/close").GetComponent<Button>();
            close.onClick.ClearAll();
            close.onClick.AddListener(delegate ()
            {
                note.ActiveUI.isOn = false;
            });

            EditorThemeManager.ApplySelectable(close, ThemeGroup.Close);
            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Close_X, close.transform.GetChild(0).gameObject, new List<Component>
            {
                close.transform.GetChild(0).GetComponent<Image>(),
            }));

            gameObject.AddComponent<NoteCloseDelete>().Init(delete.gameObject, close.gameObject);

            return gameObject;
        }

        public GameObject GenerateOST(OSTItem ost)
        {
            var gameObject = prefabs[6].Duplicate(content, "ost");
            gameObject.transform.localScale = Vector3.one;
            ost.GameObject = gameObject;

            var button = gameObject.GetComponent<Button>();
            button.onClick.ClearAll();
            button.onClick.AddListener(delegate ()
            {
                OpenOSTEditor(ost);
            });

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);

            ost.TextUI = gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>();
            ost.TextUI.text = ost.Name;
            EditorThemeManager.ApplyLightText(ost.TextUI);

            var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
            delete.button.onClick.ClearAll();
            delete.button.onClick.AddListener(delegate ()
            {
                planners.RemoveAll(x => x is OSTItem && x.ID == ost.ID);
                SaveOST();

                if (currentOSTID == ost.ID)
                    StopOST();

                Destroy(gameObject);
            });

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete, delete.gameObject, new List<Component>
            {
                delete.button.image,
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete_Text, delete.image.gameObject, new List<Component>
            {
                delete.image,
            }));

            return gameObject;
        }

        #endregion

        #region Open / Close UI

        public bool PlannerActive => EditorManager.inst.editorState == EditorManager.EditorState.Intro;

        public void Open()
        {
            EditorManager.inst.editorState = EditorManager.EditorState.Intro;
            UpdateStateUI();
        }

        public void Close()
        {
            EditorManager.inst.editorState = EditorManager.EditorState.Main;
            UpdateStateUI();
        }

        public void ToggleState() => SetState(EditorManager.inst.editorState == EditorManager.EditorState.Main ? EditorManager.EditorState.Intro : EditorManager.EditorState.Main);

        public void SetState(EditorManager.EditorState editorState)
        {
            EditorManager.inst.editorState = editorState;
            UpdateStateUI();
        }

        public void UpdateStateUI()
        {
            var editorState = EditorManager.inst.editorState;
            EditorManager.inst.GUIMain.SetActive(editorState == EditorManager.EditorState.Main);
            EditorManager.inst.GUIIntro.SetActive(editorState == EditorManager.EditorState.Intro);

            if (editorState == EditorManager.EditorState.Main)
                StopOST();
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Base Planner class. FullPath must be the directory without a slash at the end. For example: Application Directory/beatmaps/planners/to do/item
        /// </summary>
        public class PlannerItem
        {
            public PlannerItem()
            {

            }

            public string ID { get; set; } = LSText.randomNumString(10);

            public GameObject GameObject { get; set; }

            public Type PlannerType { get; set; }

            public enum Type
            {
                Document,
                TODO,
                Character,
                Timeline,
                Schedule,
                Note,
                OST
            }
        }

        public class DocumentItem : PlannerItem
        {
            public DocumentItem()
            {
                PlannerType = Type.Document;
            }

            public string Name { get; set; }
            public TextMeshProUGUI NameUI { get; set; }
            public string Text { get; set; }
            public TextMeshProUGUI TextUI { get; set; }
        }

        public class TODOItem : PlannerItem
        {
            public TODOItem()
            {
                PlannerType = Type.TODO;
            }

            public string Text { get; set; }
            public TextMeshProUGUI TextUI { get; set; }
            public bool Checked { get; set; }
            public Toggle CheckedUI { get; set; }
            public int Priority { get; set; }
        }

        public class CharacterItem : PlannerItem
        {
            public CharacterItem()
            {
                PlannerType = Type.Character;
            }

            public CharacterItem(string fullPath)
            {
                CharacterSprite = RTFile.FileExists(fullPath + "/profile.png") ? SpriteManager.LoadSprite(fullPath + "/profile.png") : SteamWorkshop.inst.defaultSteamImageSprite;

                if (RTFile.FileExists(fullPath + "/info.lsn"))
                {
                    var jn = JSON.Parse(RTFile.ReadFromFile(fullPath + "/info.lsn"));

                    Name = jn["name"];
                    Gender = jn["gender"];
                    Description = jn["desc"];

                    for (int i = 0; i < jn["tr"].Count; i++)
                    {
                        CharacterTraits.Add(jn["tr"][i]);
                    }

                    for (int i = 0; i < jn["lo"].Count; i++)
                    {
                        CharacterLore.Add(jn["lo"][i]);
                    }

                    for (int i = 0; i < jn["ab"].Count; i++)
                    {
                        CharacterAbilities.Add(jn["ab"][i]);
                    }
                }

                FullPath = fullPath;
                PlannerType = Type.Character;
            }

            public string Name { get; set; }
            public string Gender { get; set; }
            public List<string> CharacterTraits { get; set; } = new List<string>();
            public List<string> CharacterLore { get; set; } = new List<string>();
            public List<string> CharacterAbilities { get; set; } = new List<string>();
            public string Description { get; set; }
            public Sprite CharacterSprite { get; set; }

            public string FullPath { get; set; }

            public TextMeshProUGUI DetailsUI { get; set; }
            public TextMeshProUGUI DescriptionUI { get; set; }
            public Image ProfileUI { get; set; }

            public string Format(bool clamp)
            {
                var str = "<b>Name</b>: " + Name + "<br><b>Gender</b>: " + Gender + "<br><b>Character Traits</b>:<br>";

                for (int i = 0; i < CharacterTraits.Count; i++)
                    str += "- " + CharacterTraits[i] + "<br>";

                str += "<br><b>Lore</b>:<br>";

                for (int i = 0; i < CharacterLore.Count; i++)
                    str += "- " + CharacterLore[i] + "<br>";

                str += "<br><b>Abilities</b>:<br>";

                for (int i = 0; i < CharacterAbilities.Count; i++)
                    str += "- " + CharacterAbilities[i] + (i == CharacterAbilities.Count - 1 ? "" : "<br>");

                if (clamp)
                    return LSText.ClampString(str, 252);
                return str;
            }

            public string FormatDetails
            {
                get
                {
                    //var stringBuilder = new StringBuilder();

                    //stringBuilder.AppendLine($"<b>Name</b>: {Name}<br>");
                    //stringBuilder.AppendLine($"<b>Gender</b>: {Gender}<br>");

                    //stringBuilder.AppendLine($"<b>Character Traits</b>:<br>");
                    //for (int i = 0; i < CharacterTraits.Count; i++)
                    //{
                    //    stringBuilder.AppendLine($"- {CharacterTraits[i]}<br>");
                    //}
                    //stringBuilder.AppendLine($"<br>");

                    //stringBuilder.AppendLine($"<b>Lore</b>:<br>");
                    //for (int i = 0; i < CharacterLore.Count; i++)
                    //{
                    //    stringBuilder.AppendLine($"- {CharacterLore[i]}<br>");
                    //}
                    //stringBuilder.AppendLine($"<br>");

                    //stringBuilder.AppendLine($"<b>Abilities</b>:<br>");
                    //for (int i = 0; i < CharacterAbilities.Count; i++)
                    //{
                    //    stringBuilder.AppendLine($"- {CharacterAbilities[i]}<br>");
                    //}

                    var str = "";

                    str += "<b>Name</b>: " + Name + "<br><b>Gender</b>: " + Gender + "<br><b>Character Traits</b>:<br>";

                    for (int i = 0; i < CharacterTraits.Count; i++)
                        str += "- " + CharacterTraits[i] + "<br>";

                    str += "<br><b>Lore</b>:<br>";

                    for (int i = 0; i < CharacterLore.Count; i++)
                        str += "- " + CharacterLore[i] + "<br>";

                    str += "<br><b>Abilities</b>:<br>";

                    for (int i = 0; i < CharacterAbilities.Count; i++)
                        str += "- " + CharacterAbilities[i] + (i == CharacterAbilities.Count - 1 ? "" : "<br>");

                    return str;
                }
            }

            public static string DefaultCharacterDescription => "<b>Name</b>: Viral Mecha" + Environment.NewLine +
                                        "<b>Gender</b>: He" + Environment.NewLine + Environment.NewLine +
                                        "<b>Character Traits</b>:" + Environment.NewLine +
                                        "- ???" + Environment.NewLine +
                                        "- ???" + Environment.NewLine +
                                        "- ???" + Environment.NewLine + Environment.NewLine +
                                        "<b>Lore</b>:" + Environment.NewLine +
                                        "- ???" + Environment.NewLine +
                                        "- ???" + Environment.NewLine +
                                        "- ???" + Environment.NewLine + Environment.NewLine +
                                        "<b>Abilities</b>:" + Environment.NewLine +
                                        "- ???" + Environment.NewLine +
                                        "- ???" + Environment.NewLine +
                                        "- ???";

            public void Save()
            {
                var jn = JSON.Parse("{}");

                jn["name"] = Name;
                jn["gender"] = Gender;
                jn["desc"] = Description;

                for (int i = 0; i < CharacterTraits.Count; i++)
                    jn["tr"][i] = CharacterTraits[i];

                for (int i = 0; i < CharacterLore.Count; i++)
                    jn["lo"][i] = CharacterLore[i];

                for (int i = 0; i < CharacterAbilities.Count; i++)
                    jn["ab"][i] = CharacterAbilities[i];

                RTFile.WriteToFile(FullPath + "/info.lsn", jn.ToString(3));

                SpriteManager.SaveSprite(CharacterSprite, FullPath + "/profile.png");
            }
        }

        public class TimelineItem : PlannerItem
        {
            public TimelineItem()
            {
                PlannerType = Type.Timeline;
            }

            public string Name { get; set; }

            public TextMeshProUGUI NameUI { get; set; }

            public List<Event> Levels { get; set; } = new List<Event>();

            public Transform Content { get; set; }

            public GameObject Add { get; set; }

            public void UpdateTimeline(bool destroy = true)
            {
                if (destroy)
                {
                    LSHelpers.DeleteChildren(Content);
                    int num = 0;
                    foreach (var level in Levels)
                    {
                        int index = num;
                        var gameObject = inst.timelineButtonPrefab.Duplicate(Content, "event");
                        gameObject.transform.localScale = Vector3.one;
                        level.GameObject = gameObject;

                        level.Button = gameObject.GetComponent<Button>();
                        level.Button.onClick.ClearAll();
                        level.Button.onClick.AddListener(delegate ()
                        {
                            string path = $"{RTFile.ApplicationDirectory}beatmaps/{level.Path.Replace("\\", "/").Replace("/level.lsb", "")}";
                            if (!string.IsNullOrEmpty(level.Path) && RTFile.DirectoryExists(path) &&
                            (RTFile.FileExists(path + "/level.ogg") || RTFile.FileExists(path + "/level.wav") || RTFile.FileExists(path + "/level.mp3")))
                            {
                                inst.Close();
                                RTEditor.inst.StartCoroutine(RTEditor.inst.LoadLevel(path));
                            }
                        });

                        EditorThemeManager.ApplySelectable(level.Button, ThemeGroup.List_Button_1);

                        level.NameUI = gameObject.transform.Find("name").GetComponent<TextMeshProUGUI>();
                        level.NameUI.text = $"{level.ElementType}: {level.Name}";
                        EditorThemeManager.ApplyLightText(level.NameUI);
                        level.DescriptionUI = gameObject.transform.Find("description").GetComponent<TextMeshProUGUI>();
                        level.DescriptionUI.text = level.Description;
                        EditorThemeManager.ApplyLightText(level.DescriptionUI);

                        var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
                        delete.button.onClick.ClearAll();
                        delete.button.onClick.AddListener(delegate ()
                        {
                            Levels.RemoveAt(index);
                            UpdateTimeline();
                            inst.SaveTimelines();
                        });

                        EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete, delete.gameObject, new List<Component>
                        {
                            delete.button.image,
                        }, true, 1, SpriteManager.RoundedSide.W));

                        EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete_Text, delete.image.gameObject, new List<Component>
                        {
                            delete.image,
                        }));

                        var edit = gameObject.transform.Find("edit").GetComponent<Button>();
                        edit.onClick.ClearAll();
                        edit.onClick.AddListener(delegate ()
                        {
                            CoreHelper.Log($"Editing {Name}");
                            inst.OpenEventEditor(level);
                        });

                        EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Function_3, edit.gameObject, new List<Component>
                        {
                            edit.image,
                        }, true, 1, SpriteManager.RoundedSide.W));

                        EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Function_3_Text, edit.transform.GetChild(0).gameObject, new List<Component>
                        {
                            edit.transform.GetChild(0).GetComponent<Image>(),
                        }));

                        num++;
                    }

                    Add = inst.timelineAddPrefab.Duplicate(Content, "add");
                    var button = Add.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(delegate ()
                    {
                        var level = new Event
                        {
                            Name = "New Level",
                            Description = "Set my path to a level in your beatmaps folder and then click me!",
                            Path = ""
                        };

                        Levels.Add(level);
                        UpdateTimeline();
                        inst.SaveTimelines();
                    });

                    EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                    EditorThemeManager.ApplyLightText(Add.transform.GetChild(0).GetComponent<TextMeshProUGUI>());
                }
                else
                {
                    foreach (var level in Levels)
                    {
                        if (!level.GameObject)
                        {
                            var gameObject = inst.timelineButtonPrefab.Duplicate(Content, "event");
                            gameObject.transform.localScale = Vector3.one;
                            level.GameObject = gameObject;
                        }

                        if (!level.NameUI)
                            level.NameUI = level.GameObject.transform.Find("name").GetComponent<TextMeshProUGUI>();
                        if (!level.DescriptionUI)
                            level.DescriptionUI = level.GameObject.transform.Find("description").GetComponent<TextMeshProUGUI>();

                        level.NameUI.text = $"{level.ElementType}: {level.Name}";
                        level.DescriptionUI.text = level.Description;
                    }
                }
            }

            public class Event
            {
                public GameObject GameObject { get; set; }
                public Button Button { get; set; }
                public TextMeshProUGUI NameUI { get; set; }
                public TextMeshProUGUI DescriptionUI { get; set; }

                public string Name { get; set; }
                public string Description { get; set; }
                public string Path { get; set; }
                public Type ElementType { get; set; }

                public enum Type
                {
                    Level,
                    Cutscene,
                    Story
                }
            }
        }

        public class ScheduleItem : PlannerItem
        {
            public ScheduleItem()
            {
                PlannerType = Type.Schedule;
            }

            public TextMeshProUGUI TextUI { get; set; }
            public string Text => $"{Date} - {Description}";
            public string Date { get; set; } = DateTime.Now.AddDays(1).ToString("g");

            public string FormatDateFull(int day, int month, int year, int hour, int minute)
            {
                return $"{day}/{(month < 10 ? "0" + month.ToString() : month.ToString())}/{year} {(hour > 12 ? hour - 12 : hour)}:{minute} {(hour > 12 ? "PM" : "AM")}";
            }

            public string FormatDate(int day, int month, int year, int hour, int minute, string apm)
            {
                return $"{day}/{(month < 10 ? "0" + month.ToString() : month.ToString())}/{year} {(hour)}:{minute} {apm}";
            }

            public string DateFormat => $"{DateTime.Day}/{(DateTime.Month < 10 ? "0" + DateTime.Month.ToString() : DateTime.Month.ToString())}/{DateTime.Year} {(DateTime.Hour > 12 ? DateTime.Hour - 12 : DateTime.Hour)}:{DateTime.Minute} {(DateTime.Hour > 12 ? "PM" : "AM")}";
            public DateTime DateTime { get; set; } = DateTime.Now.AddDays(1);
            public string Description { get; set; }

            public bool IsActive => DateTime.Day == DateTime.Now.Day && DateTime.Month == DateTime.Now.Month && DateTime.Year == DateTime.Now.Year;
            public bool hasBeenChecked;
        }

        public class NoteItem : PlannerItem
        {
            public NoteItem()
            {
                PlannerType = Type.Note;
            }

            public bool Dragging { get; set; }

            public bool Active { get; set; }
            public string Name { get; set; }
            public Vector2 Position { get; set; } = Vector2.zero;
            public Vector2 Scale { get; set; } = new Vector2(1f, 1f);
            public Vector2 Size { get; set; } = new Vector2(300f, 150f);
            public int Color { get; set; }
            public string Text { get; set; }

            public Toggle ActiveUI { get; set; }
            public Image TopBar { get; set; }
            public TextMeshProUGUI TitleUI { get; set; }
            public TextMeshProUGUI TextUI { get; set; }

            public Color TopColor => Color >= 0 && Color < MarkerEditor.inst.markerColors.Count ? MarkerEditor.inst.markerColors[Color] : LSColors.red700;
        }

        public class OSTItem : PlannerItem
        {
            public OSTItem()
            {
                PlannerType = Type.OST;
            }

            public string Path { get; set; }
            public bool UseGlobal { get; set; }
            public string Name { get; set; }

            public int Index { get; set; }

            public TextMeshProUGUI TextUI { get; set; }

            public bool playing;

            public bool Valid => RTFile.FileExists(UseGlobal ? Path : $"{RTFile.ApplicationDirectory}{Path}") && (Path.Contains(".ogg") || Path.Contains(".wav") || Path.Contains(".mp3"));

            public void Play()
            {
                var filePath = UseGlobal ? Path : $"{RTFile.ApplicationDirectory}{Path}";

                if (!RTFile.FileExists(filePath))
                    return;

                var audioType = RTFile.GetAudioType(Path);

                if (audioType == AudioType.UNKNOWN)
                    return;

                if (audioType == AudioType.MPEG)
                {
                    var audioClip = LSAudio.CreateAudioClipUsingMP3File(filePath);

                    inst.StopOST();

                    var audioSource = Camera.main.gameObject.AddComponent<AudioSource>();

                    inst.OSTAudioSource = audioSource;
                    inst.currentOSTID = ID;
                    inst.currentOST = Index;
                    inst.playing = true;

                    audioSource.clip = audioClip;
                    audioSource.playOnAwake = true;
                    audioSource.loop = false;
                    audioSource.volume = DataManager.inst.GetSettingInt("MusicVolume", 9) / 9f * AudioManager.inst.masterVol;
                    audioSource.Play();

                    playing = true;

                    return;
                }

                inst.StartCoroutine(AlephNetworkManager.DownloadAudioClip(filePath, audioType, delegate (AudioClip audioClip)
                {
                    inst.StopOST();

                    var audioSource = Camera.main.gameObject.AddComponent<AudioSource>();

                    inst.OSTAudioSource = audioSource;
                    inst.currentOSTID = ID;
                    inst.currentOST = Index;
                    inst.playing = true;

                    audioSource.clip = audioClip;
                    audioSource.playOnAwake = true;
                    audioSource.loop = false;
                    audioSource.volume = DataManager.inst.GetSettingInt("MusicVolume", 9) / 9f * AudioManager.inst.masterVol;
                    audioSource.Play();

                    playing = true;
                }));
            }
        }

        #endregion
    }
}
