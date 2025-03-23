using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using TMPro;
using SimpleJSON;
using Crosstales.FB;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data.Planners;

namespace BetterLegacy.Editor.Managers
{
    public class ProjectPlanner : MonoBehaviour
    {
        #region Init

        public static ProjectPlanner inst;

        public static void Init() => Creator.NewGameObject(nameof(ProjectPlanner), EditorManager.inst.transform.parent).AddComponent<ProjectPlanner>();

        void Awake()
        {
            inst = this;
            plannerBase = GameObject.Find("Editor Systems/Editor GUI/sizer").transform.GetChild(1);
            plannerBase.gameObject.SetActive(true);

            planner = plannerBase.GetChild(0);
            topBarBase = planner.GetChild(0);

            EditorThemeManager.AddGraphic(planner.GetComponent<Image>(), ThemeGroup.Background_1);
            EditorThemeManager.AddGraphic(topBarBase.GetComponent<Image>(), ThemeGroup.Background_1);

            var assets = Creator.NewGameObject("Planner Assets", transform);
            assetsParent = assets.transform;

            tabPrefab = topBarBase.GetChild(0).gameObject;
            tabPrefab.transform.SetParent(assetsParent);
            tabPrefab.transform.AsRT().sizeDelta = new Vector2(200f, 54f);

            LSHelpers.DeleteChildren(topBarBase);

            Destroy(topBarBase.GetComponent<ToggleGroup>());
            tabPrefab.GetComponent<Toggle>().group = null;

            for (int i = 0; i < tabNames.Length; i++)
                GenerateTab(tabNames[i]);

            Spacer("topbar spacer", topBarBase, new Vector2(195f, 32f));

            var close = EditorPrefabHolder.Instance.CloseButton.Duplicate(topBarBase, "close");
            close.transform.localScale = Vector3.one;

            close.transform.AsRT().sizeDelta = new Vector2(48f, 48f);

            var closeButton = close.GetComponent<Button>();
            closeButton.onClick.ClearAll();
            closeButton.onClick.AddListener(() => { Close(); });

            EditorThemeManager.AddSelectable(closeButton, ThemeGroup.Close);

            var closeX = close.transform.GetChild(0).gameObject;
            EditorThemeManager.AddGraphic(close.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

            EditorHelper.AddEditorDropdown("Open Project Planner", "", "Edit", SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_planner.png"), () =>
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

            EditorThemeManager.AddScrollbar(scrollBarVertical.GetComponent<Scrollbar>(), scrollBarVertical.GetComponent<Image>(), scrollbarRoundedSide: SpriteHelper.RoundedSide.Bottom_Right_I);

            contentBase.Find("Image").AsRT().anchoredPosition = new Vector2(690f, /*-94f*/ -104f);
            contentBase.Find("Image").AsRT().sizeDelta = new Vector2(1384f, 48f);
            EditorThemeManager.AddGraphic(contentBase.Find("Image").GetComponent<Image>(), ThemeGroup.Background_1);

            // List handlers
            {
                var searchBase = RTEditor.inst.OpenLevelPopup.SearchField.transform.parent.gameObject.Duplicate(contentBase.Find("Image"), "search base");
                searchBase.transform.localScale = Vector3.one;
                searchBase.transform.AsRT().anchoredPosition = Vector2.zero;
                searchBase.transform.AsRT().sizeDelta = new Vector2(0f, 48f);
                searchBase.transform.GetChild(0).AsRT().sizeDelta = new Vector2(0f, 48f);

                var searchField = searchBase.transform.GetChild(0).GetComponent<InputField>();
                searchField.onValueChanged.ClearAll();
                ((Text)searchField.placeholder).text = "Search...";
                searchField.onValueChanged.AddListener(_val =>
                {
                    CoreHelper.Log($"Searching {_val}");
                    SearchTerm = _val;
                    RefreshList();
                });

                EditorThemeManager.AddInputField(searchField, ThemeGroup.Search_Field_1, 1, SpriteHelper.RoundedSide.Bottom);

                var tfv = ObjEditor.inst.ObjectView.transform;

                var addNewItem = EditorPrefabHolder.Instance.Function2Button.Duplicate(contentBase, "new", 1);
                addNewItem.SetActive(true);
                var addNewItemStorage = addNewItem.GetComponent<FunctionButtonStorage>();
                addNewItem.transform.AsRT().anchoredPosition = new Vector2(120f, 970f);
                addNewItem.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                var addNewItemText = addNewItemStorage.label;
                addNewItemText.text = "Add New Item";
                addNewItemStorage.button.onClick.ClearAll();
                addNewItemStorage.button.onClick.AddListener(() =>
                {
                    CoreHelper.Log($"Create new {tabNames[CurrentTab]}");
                    var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
                    switch (CurrentTab)
                    {
                        case 0:
                            {
                                var list = documents;

                                var document = new DocumentPlanner();
                                document.Name = $"New Story {list.Count + 1}";
                                document.Text = "<align=center>Plan out your story!";

                                AddPlanner(document);
                                SaveDocuments();

                                break;
                            } // Document
                        case 1:
                            {
                                var list = todos;

                                var todo = new TODOPlanner();
                                todo.Checked = false;
                                todo.Text = "Do this.";

                                AddPlanner(todo);
                                SaveTODO();

                                break;
                            } // TODO
                        case 2:
                            {
                                if (string.IsNullOrEmpty(Path.GetFileName(path)))
                                    return;

                                var charactersPath = RTFile.CombinePaths(path, "characters");
                                RTFile.CreateDirectory(charactersPath);

                                var fullPath = RTFile.CombinePaths(charactersPath, "New Character");

                                int num = 1;
                                while (RTFile.DirectoryExists(fullPath))
                                {
                                    fullPath = RTFile.CombinePaths(charactersPath, $"New Character {num}");
                                    num++;
                                }

                                RTFile.CreateDirectory(fullPath);

                                var character = new CharacterPlanner();

                                for (int i = 0; i < 3; i++)
                                {
                                    character.CharacterTraits.Add("???");
                                    character.CharacterLore.Add("???");
                                    character.CharacterAbilities.Add("???");
                                }

                                character.CharacterSprite = SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "CA Hal.png");
                                character.Name = Path.GetFileName(fullPath);
                                character.FullPath = fullPath;
                                character.Gender = "He";
                                character.Description = "This is the default description";

                                AddPlanner(character);
                                character.Save();

                                break;
                            } // Character
                        case 3:
                            {
                                var timeline = new TimelinePlanner();
                                timeline.Name = "Classic Arrhythmia";
                                timeline.Levels.Add(new TimelinePlanner.Event
                                {
                                    Name = "Beginning",
                                    Description = $"Introduces players / viewers to Hal.)",
                                    ElementType = TimelinePlanner.Event.Type.Cutscene,
                                    Path = ""
                                });
                                timeline.Levels.Add(new TimelinePlanner.Event
                                {
                                    Name = "Tokyo Skies",
                                    Description = $"Players learn very basic stuff about Classic Arrhythmia / Project Arrhythmia mechanics.{Environment.NewLine}{Environment.NewLine}(Click on this button to open the level.)",
                                    ElementType = TimelinePlanner.Event.Type.Level,
                                    Path = ""
                                });

                                AddPlanner(timeline);
                                SaveTimelines();

                                break;
                            } // Timeline
                        case 4:
                            {
                                var schedule = new SchedulePlanner();
                                schedule.Date = DateTime.Now.AddDays(1).ToString("g");
                                schedule.Description = "Tomorrow!";

                                AddPlanner(schedule);
                                SaveSchedules();

                                break;
                            } // Schedule
                        case 5:
                            {
                                var note = new NotePlanner();
                                note.Active = true;
                                note.Name = "New Note";
                                note.Color = UnityEngine.Random.Range(0, MarkerEditor.inst.markerColors.Count);
                                note.Position = new Vector2(Screen.width / 2, Screen.height / 2);
                                note.Text = "This note appears in the editor and can be dragged to anywhere.";

                                AddPlanner(note);
                                SaveNotes();

                                break;
                            } // Note
                        case 6:
                            {
                                var ost = new OSTPlanner();
                                ost.Name = "Kaixo - Fragments";
                                ost.Path = "Set this path to wherever you have a song located.";

                                AddPlanner(ost);

                                var list = osts.OrderBy(x => x.Index).ToList();

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
                EditorThemeManager.AddGraphic(addNewItemText, ThemeGroup.Function_2_Text);

                var reload = EditorPrefabHolder.Instance.Function2Button.Duplicate(contentBase, "reload", 2);
                reload.SetActive(true);
                var reloadStorage = reload.GetComponent<FunctionButtonStorage>();
                reload.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                reload.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                var reloadText = reloadStorage.label;
                reloadText.text = "Reload";
                reloadStorage.button.onClick.ClearAll();
                reloadStorage.button.onClick.AddListener(() => { Load(); });

                reloadStorage.button.transition = Selectable.Transition.ColorTint;
                EditorThemeManager.AddSelectable(reloadStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(reloadText, ThemeGroup.Function_2_Text);
            }

            gradientSprite = SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "linear_gradient.png");

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
                    tmpTitle.text = CharacterPlanner.DefaultCharacterDescription;

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
                    var prefab = Creator.NewUIObject("timeline prefab", assetsParent);

                    UIManager.SetRectTransform(prefab.transform.AsRT(), Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero);
                    var prefabImage = prefab.AddComponent<Image>();
                    prefabImage.color = new Color(0f, 0f, 0f, 0.2f);

                    var editPrefab = EditorPrefabHolder.Instance.CloseButton.Duplicate(prefab.transform, "edit");
                    UIManager.SetRectTransform(editPrefab.transform.AsRT(), new Vector2(-38f, -12f), Vector2.one, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(20f, 20f));
                    editPrefab.transform.GetChild(0).AsRT().sizeDelta = new Vector2(0f, 0f);
                    var editPrefabButton = editPrefab.GetComponent<Button>();
                    editPrefabButton.colors = UIManager.SetColorBlock(editPrefabButton.colors, new Color(0.9f, 0.9f, 0.9f, 1f), Color.white, Color.white, new Color(0.9f, 0.9f, 0.9f, 1f), LSColors.red700);
                    var spritePrefabImage = editPrefab.transform.GetChild(0).GetComponent<Image>();
                    spritePrefabImage.color = new Color(0.037f, 0.037f, 0.037f, 1f);
                    spritePrefabImage.sprite = SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "editor_gui_edit.png");

                    var deletePrefab = EditorPrefabHolder.Instance.DeleteButton.Duplicate(prefab.transform, "delete");
                    UIManager.SetRectTransform(deletePrefab.transform.AsRT(), new Vector2(-12f, -12f), Vector2.one, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(20f, 20f));

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
                    spriteImage.sprite = SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "editor_gui_edit.png");

                    var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(timelineButtonPrefab.transform, "delete");
                    UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(-16f, -16f), Vector2.one, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(24f, 24f));

                    timelineAddPrefab = baseCardPrefab.Duplicate(assetsParent, "timeline add prefab");
                    var albumArtAdd = timelineAddPrefab.transform.GetChild(0);
                    var titleAdd = timelineAddPrefab.transform.GetChild(1);
                    var artistAdd = timelineAddPrefab.transform.GetChild(2);

                    timelineAddPrefab.GetComponent<Image>().sprite = null;

                    DestroyImmediate(albumArtAdd.gameObject);
                    artistAdd.SetParent(prefab.transform);
                    artistAdd.AsRT().anchoredPosition = new Vector2(-240f, 110f);
                    artistAdd.AsRT().sizeDelta = new Vector2(-128f, 40f);
                    artistAdd.name = "name";

                    var tmpArtistAdd = artistAdd.GetComponent<TextMeshProUGUI>();
                    tmpArtistAdd.alignment = TextAlignmentOptions.TopLeft;
                    tmpArtistAdd.color = Color.white;
                    tmpArtistAdd.fontSize = 30;

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
                    spritePrefabImage.sprite = SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "editor_gui_edit.png");

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

            notesParent = Creator.NewUIObject("notes", plannerBase).transform.AsRT(); // floating notes

            // Document Full View
            {
                var fullView = Creator.NewUIObject("full view", contentBase);
                documentFullView = fullView;
                var fullViewImage = fullView.AddComponent<Image>();
                fullViewImage.color = new Color(0.082f, 0.082f, 0.078f, 1f);
                UIManager.SetRectTransform(fullView.transform.AsRT(), new Vector2(690f, -548f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(1384f, 936f));

                EditorThemeManager.AddGraphic(fullViewImage, ThemeGroup.Background_1);

                documentInputField = fullView.AddComponent<TMP_InputField>();

                var docText = baseCardPrefab.transform.Find("artist").gameObject.Duplicate(fullView.transform, "text");
                docText.transform.AsRT().anchoredPosition = new Vector2(0f, -50f);
                docText.transform.AsRT().sizeDelta = new Vector2(-32f, 840f);
                var t = docText.GetComponent<TextMeshProUGUI>();

                documentInputField.textComponent = t;

                var docTitle = baseCardPrefab.transform.Find("artist").gameObject.Duplicate(fullView.transform, "title");

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
                var editorBase = Creator.NewUIObject("editor base", contentBase);
                editorBase.transform.AsRT().anchoredPosition = new Vector2(691f, -40f);
                editorBase.transform.AsRT().sizeDelta = new Vector2(537f, 936f);
                var editorBaseImage = editorBase.AddComponent<Image>();
                editorBaseImage.color = new Color(0.078f, 0.067f, 0.067f, 1f);

                EditorThemeManager.AddGraphic(editorBaseImage, ThemeGroup.Background_3);

                var editor = Creator.NewUIObject("editor", editorBase.transform);
                editor.transform.AsRT().anchoredPosition = Vector3.zero;
                editor.transform.AsRT().sizeDelta = new Vector2(524f, 936f);

                var panel = Creator.NewUIObject("panel", editor.transform);
                UIManager.SetRectTransform(panel.transform.AsRT(), new Vector2(0f, 436f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(14f, 64f));
                editorTitlePanel = panel.AddComponent<Image>();
                editorTitlePanel.color = new Color(0.310f, 0.467f, 0.737f, 1f);
                EditorThemeManager.AddGraphic(editorTitlePanel, ThemeGroup.Null, true);

                var editorTitle = baseCardPrefab.transform.Find("artist").gameObject.Duplicate(panel.transform, "title");
                UIManager.SetRectTransform(editorTitle.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(569f, 0f));
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

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Name";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text1 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1.transform, "text");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    documentEditorName = text1.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(documentEditorName);

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Text";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text2 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1.transform, "text");
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
                    var g1 = Creator.NewUIObject("TODO", editor.transform);
                    UIManager.SetRectTransform(g1.transform.AsRT(), new Vector2(0f, -32f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, -64f));

                    var vlg = g1.AddComponent<VerticalLayoutGroup>();
                    vlg.childControlHeight = false;
                    vlg.childForceExpandHeight = false;
                    vlg.spacing = 4f;

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Text";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text1 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1.transform, "text");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    if (text1.transform.Find("edit"))
                        Destroy(text1.transform.Find("edit").gameObject);

                    todoEditorText = text1.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(todoEditorText);

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(334.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Change TODO Priority";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var moveUp = EditorPrefabHolder.Instance.Function2Button.Duplicate(g1.transform);
                    var moveUpStorage = moveUp.GetComponent<FunctionButtonStorage>();
                    moveUp.SetActive(true);
                    moveUp.name = "move up";
                    moveUp.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                    moveUp.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                    moveUpStorage.label.text = "Move Up";
                    todoEditorMoveUpButton = moveUpStorage.button;
                    EditorThemeManager.AddSelectable(todoEditorMoveUpButton, ThemeGroup.Function_2);
                    EditorThemeManager.AddGraphic(moveUpStorage.label, ThemeGroup.Function_2_Text);

                    var moveDown = EditorPrefabHolder.Instance.Function2Button.Duplicate(g1.transform);
                    var moveDownStorage = moveDown.GetComponent<FunctionButtonStorage>();
                    moveDown.SetActive(true);
                    moveDown.name = "move down";
                    moveDown.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                    moveDown.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                    moveDownStorage.label.text = "Move Down";
                    todoEditorMoveDownButton = moveDownStorage.button;
                    EditorThemeManager.AddSelectable(todoEditorMoveDownButton, ThemeGroup.Function_2);
                    EditorThemeManager.AddGraphic(moveDownStorage.label, ThemeGroup.Function_2_Text);

                    g1.SetActive(false);
                    editors.Add(g1);
                }

                // Character
                {
                    var g1 = Creator.NewUIObject("Character", editor.transform);
                    UIManager.SetRectTransform(g1.transform.AsRT(), new Vector2(0f, -32f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, -64f));

                    var vlg = g1.AddComponent<VerticalLayoutGroup>();
                    vlg.childControlHeight = false;
                    vlg.childForceExpandHeight = false;
                    vlg.spacing = 4f;

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Name";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text1 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1.transform, "name");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    characterEditorName = text1.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(characterEditorName);

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Gender";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text2 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1.transform, "gender");
                    text2.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text2.gameObject.SetActive(true);

                    characterEditorGender = text2.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(characterEditorGender);

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Description";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text3 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1.transform, "description");
                    text3.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text3.gameObject.SetActive(true);

                    characterEditorDescription = text3.GetComponent<InputField>();
                    characterEditorDescription.textComponent.alignment = TextAnchor.UpperLeft;
                    ((Text)characterEditorDescription.placeholder).alignment = TextAnchor.UpperLeft;
                    characterEditorDescription.lineType = InputField.LineType.MultiLineNewline;
                    EditorThemeManager.AddInputField(characterEditorDescription);

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Select Profile Image";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var reload = EditorPrefabHolder.Instance.Function2Button.Duplicate(g1.transform);
                    var pickProfileStorage = reload.GetComponent<FunctionButtonStorage>();
                    reload.SetActive(true);
                    reload.name = "pick profile";
                    reload.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                    reload.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                    pickProfileStorage.label.text = "Select";
                    characterEditorProfileSelector = pickProfileStorage.button;
                    EditorThemeManager.AddSelectable(characterEditorProfileSelector, ThemeGroup.Function_2);
                    EditorThemeManager.AddGraphic(pickProfileStorage.label, ThemeGroup.Function_2_Text);

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
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
                        var tagScrollView = Creator.NewUIObject("Character Traits Scroll View", g1.transform);
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
                        tagContentGLG.cellSize = new Vector2(536f, 32f);
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

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
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
                        var tagScrollView = Creator.NewUIObject("Lore Scroll View", g1.transform);
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
                        tagContentGLG.cellSize = new Vector2(536f, 32f);
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

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
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
                        var tagScrollView = Creator.NewUIObject("Abilities Scroll View", g1.transform);
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
                        tagContentGLG.cellSize = new Vector2(536f, 32f);
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

                    g1.SetActive(false);
                    editors.Add(g1);
                }

                // Timeline
                {
                    var g1 = Creator.NewUIObject("Timeline", editor.transform);
                    UIManager.SetRectTransform(g1.transform.AsRT(), new Vector2(0f, -32f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, -64f));

                    var vlg = g1.AddComponent<VerticalLayoutGroup>();
                    vlg.childControlHeight = false;
                    vlg.childForceExpandHeight = false;
                    vlg.spacing = 4f;

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Name";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text1 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1.transform, "text");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    timelineEditorName = text1.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(timelineEditorName);

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

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Name";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text1 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1.transform, "name");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    eventEditorName = text1.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(eventEditorName);

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Description";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text2 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1.transform, "description");
                    text2.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text2.gameObject.SetActive(true);

                    eventEditorDescription = text2.GetComponent<InputField>();
                    eventEditorDescription.textComponent.alignment = TextAnchor.UpperLeft;
                    ((Text)eventEditorDescription.placeholder).alignment = TextAnchor.UpperLeft;
                    eventEditorDescription.lineType = InputField.LineType.MultiLineNewline;
                    EditorThemeManager.AddInputField(eventEditorDescription);

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Path";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text3 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1.transform, "path");
                    text3.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text3.gameObject.SetActive(true);

                    eventEditorPath = text3.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(eventEditorPath);

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Type";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var renderType = EditorPrefabHolder.Instance.Dropdown.Duplicate(g1.transform, "type");
                    eventEditorType = renderType.GetComponent<Dropdown>();
                    eventEditorType.options = CoreHelper.StringToOptionData("Level", "Cutscene", "Story");
                    EditorThemeManager.AddDropdown(eventEditorType);

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

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Name";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text1 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1.transform, "name");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    scheduleEditorDescription = text1.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(scheduleEditorDescription);

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Year";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text2 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1.transform, "year");
                    text2.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text2.gameObject.SetActive(true);

                    scheduleEditorYear = text2.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(scheduleEditorYear);

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Month";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var renderType = ObjEditor.inst.ObjectView.transform.Find("autokill/tod-dropdown").gameObject.Duplicate(g1.transform, "month");
                    scheduleEditorMonth = renderType.GetComponent<Dropdown>();
                    scheduleEditorMonth.options = CoreHelper.StringToOptionData("January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December");
                    EditorThemeManager.AddDropdown(scheduleEditorMonth);

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Day";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text3 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1.transform, "day");
                    text3.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text3.gameObject.SetActive(true);

                    scheduleEditorDay = text3.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(scheduleEditorDay);

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Hour";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text4 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1.transform, "hour");
                    text4.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text4.gameObject.SetActive(true);

                    scheduleEditorHour = text4.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(scheduleEditorHour);

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Minute";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text5 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1.transform, "minute");
                    text5.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text5.gameObject.SetActive(true);

                    scheduleEditorMinute = text5.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(scheduleEditorMinute);

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

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Name";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text1 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1.transform, "text");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    noteEditorName = text1.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(noteEditorName);

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Text";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text2 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1.transform, "text");
                    text2.transform.AsRT().sizeDelta = new Vector2(537f, 360f);
                    text2.gameObject.SetActive(true);

                    noteEditorText = text2.GetComponent<InputField>();
                    noteEditorText.textComponent.alignment = TextAnchor.UpperLeft;
                    ((Text)noteEditorText.placeholder).alignment = TextAnchor.UpperLeft;
                    noteEditorText.lineType = InputField.LineType.MultiLineNewline;
                    EditorThemeManager.AddInputField(noteEditorText);

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Color";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

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

                        EditorThemeManager.AddGraphic(toggle.image, ThemeGroup.Null, true);
                        EditorThemeManager.AddGraphic(toggle.graphic, ThemeGroup.Background_3);
                    }

                    // Label
                    {
                        var label = EditorPrefabHolder.Instance.Labels.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(300f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Reset Position and Scale";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var reset = EditorPrefabHolder.Instance.Function2Button.gameObject.Duplicate(g1.transform);
                    var resetStorage = reset.GetComponent<FunctionButtonStorage>();
                    reset.SetActive(true);
                    reset.name = "reset";
                    reset.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                    reset.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                    resetStorage.label.text = "Reset";
                    noteEditorReset = resetStorage.button;
                    EditorThemeManager.AddSelectable(noteEditorReset, ThemeGroup.Function_2);
                    EditorThemeManager.AddGraphic(resetStorage.label, ThemeGroup.Function_2_Text);

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

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Path";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text1 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1.transform, "path");
                    text1.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text1.gameObject.SetActive(true);

                    ostEditorPath = text1.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(ostEditorPath);

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Name";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text2 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1.transform, "name");
                    text2.transform.AsRT().sizeDelta = new Vector2(537f, 64f);
                    text2.gameObject.SetActive(true);

                    ostEditorName = text2.GetComponent<InputField>();
                    EditorThemeManager.AddInputField(ostEditorName);

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(334.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Start Playing OST From Here";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var play = EditorPrefabHolder.Instance.Function2Button.Duplicate(g1.transform);
                    var playStorage = play.GetComponent<FunctionButtonStorage>();
                    play.SetActive(true);
                    play.name = "play";
                    play.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                    play.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                    playStorage.label.text = "Play";
                    ostEditorPlay = playStorage.button;
                    EditorThemeManager.AddSelectable(ostEditorPlay, ThemeGroup.Function_2);
                    EditorThemeManager.AddGraphic(playStorage.label, ThemeGroup.Function_2_Text);

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Stop Playing";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var stop = EditorPrefabHolder.Instance.Function2Button.Duplicate(g1.transform);
                    var stopStorage = stop.GetComponent<FunctionButtonStorage>();
                    stop.SetActive(true);
                    stop.name = "stop";
                    stop.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                    stop.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
                    stopStorage.label.text = "Stop";
                    ostEditorStop = stopStorage.button;
                    EditorThemeManager.ApplySelectable(ostEditorStop, ThemeGroup.Function_2);
                    EditorThemeManager.AddGraphic(stopStorage.label, ThemeGroup.Function_2_Text);

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Use Global Path";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var global = EditorPrefabHolder.Instance.Function2Button.Duplicate(g1.transform);
                    var globalStorage = global.GetComponent<FunctionButtonStorage>();
                    global.SetActive(true);
                    global.name = "global";
                    global.transform.AsRT().anchoredPosition = new Vector2(370f, 970f);
                    global.transform.AsRT().sizeDelta = new Vector2(200f, 32f);

                    ostEditorUseGlobal = globalStorage.button;
                    ostEditorUseGlobalText = globalStorage.label;
                    ostEditorUseGlobalText.text = "False";
                    EditorThemeManager.AddSelectable(ostEditorUseGlobal, ThemeGroup.Function_2);
                    EditorThemeManager.AddGraphic(ostEditorUseGlobalText, ThemeGroup.Function_2_Text);

                    // Label
                    {
                        var label = ObjEditor.inst.ObjectView.transform.Find("label").gameObject.Duplicate(g1.transform, "label");
                        label.transform.AsRT().pivot = new Vector2(0f, 1f);
                        label.transform.AsRT().sizeDelta = new Vector2(537f, 32f);
                        label.transform.GetChild(0).AsRT().sizeDelta = new Vector2(234.4f, 32f);
                        var labelText = label.transform.GetChild(0).GetComponent<Text>();
                        labelText.text = "Edit Index";
                        EditorThemeManager.AddLightText(labelText);

                        if (label.transform.childCount == 2)
                            Destroy(label.transform.GetChild(1).gameObject);
                    }

                    var text3 = ObjEditor.inst.ObjectView.transform.Find("shapesettings/5").gameObject.Duplicate(g1.transform, "index");
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

        void Update()
        {
            foreach (var note in notes)
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

            var list = osts;
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

        #endregion

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

        public GameObject baseCardPrefab;

        public List<GameObject> prefabs = new List<GameObject>();

        public Sprite gradientSprite;

        public List<DocumentPlanner> documents = new List<DocumentPlanner>();
        public List<TODOPlanner> todos = new List<TODOPlanner>();
        public List<CharacterPlanner> characters = new List<CharacterPlanner>();
        public List<TimelinePlanner> timelines = new List<TimelinePlanner>();
        public List<SchedulePlanner> schedules = new List<SchedulePlanner>();
        public List<NotePlanner> notes = new List<NotePlanner>();
        public List<OSTPlanner> osts = new List<OSTPlanner>();

        public string PlannersPath { get; set; } = "planners";

        public GameObject timelineButtonPrefab;

        public GameObject timelineAddPrefab;

        public Texture2D horizontalDrag;
        public Texture2D verticalDrag;

        #region Editor

        public Image editorTitlePanel;

        public InputField documentEditorName;
        public InputField documentEditorText;

        public InputField todoEditorText;
        public Button todoEditorMoveUpButton;
        public Button todoEditorMoveDownButton;

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

        #endregion

        #endregion

        #region Save / Load

        public void AddPlanner(PlannerBase item)
        {
            switch (item.PlannerType)
            {
                case PlannerBase.Type.Document:
                    {
                        documents.Add(item as DocumentPlanner);
                        break;
                    }
                case PlannerBase.Type.TODO:
                    {
                        todos.Add(item as TODOPlanner);
                        break;
                    }
                case PlannerBase.Type.Character:
                    {
                        characters.Add(item as CharacterPlanner);
                        break;
                    }
                case PlannerBase.Type.Timeline:
                    {
                        timelines.Add(item as TimelinePlanner);
                        break;
                    }
                case PlannerBase.Type.Schedule:
                    {
                        schedules.Add(item as SchedulePlanner);
                        break;
                    }
                case PlannerBase.Type.Note:
                    {
                        notes.Add(item as NotePlanner);
                        break;
                    }
                case PlannerBase.Type.OST:
                    {
                        osts.Add(item as OSTPlanner);
                        break;
                    }
            }
            item.Init();
        }

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

        public void Load()
        {
            ClearPlanners();
            LSHelpers.DeleteChildren(content);

            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (string.IsNullOrEmpty(Path.GetFileName(path)))
                return;

            RTFile.CreateDirectory(path);

            LoadDocuments();
            LoadTODO();
            LoadTimelines();
            LoadSchedules();
            LoadNotes();
            LoadOST();

            var characters = RTFile.CombinePaths(path, "characters");
            RTFile.CreateDirectory(characters);

            var directories = Directory.GetDirectories(characters, "*", SearchOption.TopDirectoryOnly);
            if (directories.Length > 0)
                foreach (var folder in directories)
                    AddPlanner(new CharacterPlanner(folder));

            RefreshList();
        }

        public void SaveDocuments()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = JSON.Parse("{}");

            var list = documents;

            for (int i = 0; i < list.Count; i++)
            {
                var document = list[i];
                jn["documents"][i]["name"] = document.Name;
                jn["documents"][i]["text"] = document.Text;
            }

            RTFile.WriteToFile(RTFile.CombinePaths(path, $"documents{FileFormat.LSN.Dot()}"), jn.ToString(3));
        }

        public void LoadDocuments()
        {
            var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, $"beatmaps/{PlannersPath}", $"documents{FileFormat.LSN.Dot()}");
            if (!RTFile.FileExists(path))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            for (int i = 0; i < jn["documents"].Count; i++)
            {
                var document = new DocumentPlanner();

                document.Name = jn["documents"][i]["name"];
                document.Text = jn["documents"][i]["text"];
                AddPlanner(document);
            }
        }

        public void SaveTODO()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = JSON.Parse("{}");

            var list = todos;

            for (int i = 0; i < list.Count; i++)
            {
                jn["todo"][i]["ch"] = list[i].Checked.ToString();
                jn["todo"][i]["text"] = list[i].Text;
            }

            RTFile.WriteToFile(RTFile.CombinePaths(path, $"todo{FileFormat.LSN.Dot()}"), jn.ToString(3));
        }

        public void LoadTODO()
        {
            var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, $"beatmaps/{PlannersPath}", $"todo{FileFormat.LSN.Dot()}");
            if (!RTFile.FileExists(path))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            for (int i = 0; i < jn["todo"].Count; i++)
            {
                var todo = new TODOPlanner();
                todo.Checked = jn["todo"][i]["ch"].AsBool;
                todo.Text = jn["todo"][i]["text"];
                AddPlanner(todo);
            }
        }

        public void SaveTimelines()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = JSON.Parse("{}");

            var list = timelines;

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

            RTFile.WriteToFile(RTFile.CombinePaths(path, $"timeline{FileFormat.LSN.Dot()}"), jn.ToString(3));
        }

        public void LoadTimelines()
        {
            var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, $"beatmaps/{PlannersPath}", $"timelines{FileFormat.LSN.Dot()}");
            if (!RTFile.FileExists(path))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            for (int i = 0; i < jn["timelines"].Count; i++)
            {
                var timeline = new TimelinePlanner();

                timeline.Name = jn["timelines"][i]["name"];

                for (int j = 0; j < jn["timelines"][i]["levels"].Count; j++)
                {
                    timeline.Levels.Add(new TimelinePlanner.Event
                    {
                        Name = jn["timelines"][i]["levels"][j]["n"],
                        Path = jn["timelines"][i]["levels"][j]["p"],
                        ElementType = (TimelinePlanner.Event.Type)jn["timelines"][i]["levels"][j]["t"].AsInt,
                        Description = jn["timelines"][i]["levels"][j]["d"],
                    });
                }

                AddPlanner(timeline);
            }
        }

        public void SaveSchedules()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = JSON.Parse("{}");

            var list = schedules;

            for (int i = 0; i < list.Count; i++)
            {
                var schedule = list[i];
                jn["schedules"][i]["date"] = schedule.Date;
                jn["schedules"][i]["desc"] = schedule.Description;
                if (schedule.hasBeenChecked)
                    jn["schedules"][i]["checked"] = schedule.hasBeenChecked;
            }

            RTFile.WriteToFile(RTFile.CombinePaths(path, $"schedules{FileFormat.LSN.Dot()}"), jn.ToString(3));
        }

        public void LoadSchedules()
        {
            var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, $"beatmaps/{PlannersPath}", $"schedules{FileFormat.LSN.Dot()}");
            if (!RTFile.FileExists(path))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            for (int i = 0; i < jn["schedules"].Count; i++)
            {
                var schedule = new SchedulePlanner();
                schedule.Date = jn["schedules"][i]["date"];
                schedule.Description = jn["schedules"][i]["desc"];
                if (jn["schedules"][i]["checked"] != null)
                    schedule.hasBeenChecked = jn["schedules"][i]["checked"].AsBool;

                if (DateTime.TryParse(schedule.Date, out DateTime dateTime))
                    schedule.DateTime = dateTime;

                AddPlanner(schedule);
            }
        }

        public void SaveNotes()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = JSON.Parse("{}");

            var list = notes;

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

            RTFile.WriteToFile(RTFile.CombinePaths(path, $"notes{FileFormat.LSN.Dot()}"), jn.ToString(3));
        }

        public void LoadNotes()
        {
            var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, $"beatmaps/{PlannersPath}", $"notes{FileFormat.LSN.Dot()}");
            if (!RTFile.FileExists(path))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            for (int i = 0; i < jn["notes"].Count; i++)
            {
                var note = new NotePlanner();

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

                AddPlanner(note);
            }
        }

        public void SaveOST()
        {
            var path = $"{RTFile.ApplicationDirectory}beatmaps/{PlannersPath}";
            if (string.IsNullOrEmpty(Path.GetFileName(path)) || !RTFile.DirectoryExists(path))
                return;

            var jn = JSON.Parse("{}");

            var list = osts;

            for (int i = 0; i < list.Count; i++)
            {
                var ost = list[i];
                ost.Index = i;

                jn["ost"][i]["name"] = ost.Name;
                jn["ost"][i]["path"] = ost.Path.ToString();
                jn["ost"][i]["use_global"] = ost.UseGlobal.ToString();
                jn["ost"][i]["index"] = ost.Index.ToString();
            }

            RTFile.WriteToFile(RTFile.CombinePaths(path, $"ost{FileFormat.LSN.Dot()}"), jn.ToString(3));
        }

        public void LoadOST()
        {
            var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, $"beatmaps/{PlannersPath}", $"ost{FileFormat.LSN.Dot()}");
            if (!RTFile.FileExists(path))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            for (int i = 0; i < jn["ost"].Count; i++)
            {
                var ost = new OSTPlanner();

                ost.Name = jn["ost"][i]["name"];
                ost.Path = jn["ost"][i]["path"];
                ost.UseGlobal = jn["ost"][i]["use_global"].AsBool;
                ost.Index = jn["ost"][i]["index"].AsInt;

                AddPlanner(ost);
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
                tab.onValueChanged.AddListener(_val =>
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
            for (int i = 0; i < documents.Count; i++)
            {
                var planner = documents[i];

                if (planner && planner.GameObject)
                    planner.GameObject.SetActive(planner.PlannerType == (PlannerBase.Type)CurrentTab && RTString.SearchString(SearchTerm, planner.Name));
            }
            
            for (int i = 0; i < todos.Count; i++)
            {
                var planner = todos[i];

                if (planner && planner.GameObject)
                    planner.GameObject.SetActive(planner.PlannerType == (PlannerBase.Type)CurrentTab && (string.IsNullOrEmpty(SearchTerm) || CheckOn(SearchTerm.ToLower()) && planner.Checked || CheckOff(SearchTerm.ToLower()) && !planner.Checked || RTString.SearchString(SearchTerm, planner.Text)));
            }
            
            for (int i = 0; i < characters.Count; i++)
            {
                var planner = characters[i];

                if (planner && planner.GameObject)
                    planner.GameObject.SetActive(planner.PlannerType == (PlannerBase.Type)CurrentTab && RTString.SearchString(SearchTerm, planner.Name, planner.Description));
            }
            
            for (int i = 0; i < timelines.Count; i++)
            {
                var planner = timelines[i];

                if (planner && planner.GameObject)
                    planner.GameObject.SetActive(planner.PlannerType == (PlannerBase.Type)CurrentTab && planner.Levels.Has(x => RTString.SearchString(SearchTerm, x.Name)));
            }
            
            for (int i = 0; i < schedules.Count; i++)
            {
                var planner = schedules[i];

                if (planner && planner.GameObject)
                    planner.GameObject.SetActive(planner.PlannerType == (PlannerBase.Type)CurrentTab && RTString.SearchString(SearchTerm, planner.Description, planner.Date));
            }
            
            for (int i = 0; i < notes.Count; i++)
            {
                var planner = notes[i];

                if (planner && planner.GameObject)
                    planner.GameObject.SetActive(planner.PlannerType == (PlannerBase.Type)CurrentTab && RTString.SearchString(SearchTerm, planner.Name, planner.Text));
            }
            
            for (int i = 0; i < osts.Count; i++)
            {
                var planner = osts[i];

                if (planner && planner.GameObject)
                    planner.GameObject.SetActive(planner.PlannerType == (PlannerBase.Type)CurrentTab && RTString.SearchString(SearchTerm, planner.Name));
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
        public void OpenDocumentEditor(DocumentPlanner document)
        {
            editors[0].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_1];

            documentEditorName.onValueChanged.ClearAll();
            documentEditorName.onEndEdit.ClearAll();
            documentEditorName.text = document.Name;
            documentEditorName.onValueChanged.AddListener(_val =>
            {
                document.Name = _val;
                document.NameUI.text = _val;
                documentTitle.text = document.Name;
            });
            documentEditorName.onEndEdit.AddListener(_val => SaveDocuments());

            HandleDocumentEditor(document);
            HandleDocumentEditorPreview(document);
        }

        void HandleDocumentEditor(DocumentPlanner document)
        {
            documentEditorText.onValueChanged.ClearAll();
            documentEditorText.onEndEdit.ClearAll();
            documentEditorText.text = document.Text;
            documentEditorText.onValueChanged.AddListener(_val =>
            {
                document.Text = _val;
                document.TextUI.text = _val;

                HandleDocumentEditorPreview(document);
            });
            documentEditorText.onEndEdit.AddListener(_val => SaveDocuments());
        }

        void HandleDocumentEditorPreview(DocumentPlanner document)
        {
            DocumentFullViewActive = true;
            documentFullView.SetActive(true);
            documentTitle.text = document.Name;
            documentInputField.onValueChanged.ClearAll();
            documentInputField.onEndEdit.ClearAll();
            documentInputField.text = document.Text;
            documentInputField.onValueChanged.AddListener(_val =>
            {
                document.Text = _val;
                document.TextUI.text = _val;

                HandleDocumentEditor(document);
            });
            documentInputField.onEndEdit.AddListener(_val => SaveDocuments());
        }

        public void OpenTODOEditor(TODOPlanner todo)
        {
            editors[1].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_2];

            todoEditorText.onValueChanged.ClearAll();
            todoEditorText.text = todo.Text;
            todoEditorText.onValueChanged.AddListener(_val =>
            {
                todo.Text = _val;
                todo.TextUI.text = _val;
            });
            todoEditorText.onEndEdit.ClearAll();
            todoEditorText.onEndEdit.AddListener(_val => SaveTODO());
            todoEditorMoveUpButton.onClick.ClearAll();
            todoEditorMoveUpButton.onClick.AddListener(() =>
            {
                if (!todos.TryFindIndex(x => x.ID == todo.ID, out int index))
                    return;

                if (index - 1 < 0)
                    return;

                todos.Move(index, index - 1);
                SaveTODO();

                foreach (var todo in todos)
                    todo.Init();
            });
            todoEditorMoveDownButton.onClick.ClearAll();
            todoEditorMoveDownButton.onClick.AddListener(() =>
            {
                if (!todos.TryFindIndex(x => x.ID == todo.ID, out int index))
                    return;

                if (index >= todos.Count - 1)
                    return;

                todos.Move(index, index + 1);
                SaveTODO();

                foreach (var todo in todos)
                    todo.Init();
            });
        }

        public void OpenCharacterEditor(CharacterPlanner character)
        {
            editors[2].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_3];

            characterEditorName.onValueChanged.ClearAll();
            characterEditorName.text = character.Name;
            characterEditorName.onValueChanged.AddListener(_val =>
            {
                character.Name = _val;
                character.DetailsUI.text = character.FormatDetails;
            });
            characterEditorName.onEndEdit.ClearAll();
            characterEditorName.onEndEdit.AddListener(_val => character.Save());

            characterEditorGender.onValueChanged.ClearAll();
            characterEditorGender.text = character.Gender;
            characterEditorGender.onValueChanged.AddListener(_val =>
            {
                character.Gender = _val;
                character.DetailsUI.text = character.FormatDetails;
            });
            characterEditorGender.onEndEdit.ClearAll();
            characterEditorGender.onEndEdit.AddListener(_val => character.Save());

            characterEditorDescription.onValueChanged.ClearAll();
            characterEditorDescription.text = character.Description;
            characterEditorDescription.onValueChanged.AddListener(_val =>
            {
                character.Description = _val;
                character.DescriptionUI.text = character.Description;
            });
            characterEditorDescription.onEndEdit.ClearAll();
            characterEditorDescription.onEndEdit.AddListener(_val => character.Save());

            characterEditorProfileSelector.onClick.ClearAll();
            characterEditorProfileSelector.onClick.AddListener(() =>
            {
                var editorPath = RTFile.ApplicationDirectory;
                string jpgFile = FileBrowser.OpenSingleFile("Select an image!", editorPath, "png", "jpg");

                if (!string.IsNullOrEmpty(jpgFile))
                {
                    character.CharacterSprite = SpriteHelper.LoadSprite(jpgFile);
                    character.ProfileUI.sprite = character.CharacterSprite;
                    character.Save();
                }

                //EditorManager.inst.ShowDialog("Browser Popup");
                //RTFileBrowser.inst.UpdateBrowser(Directory.GetCurrentDirectory(), new string[] { ".png" }, onSelectFile: _val =>
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
                    input.onValueChanged.AddListener(_val =>
                    {
                        character.CharacterTraits[index] = _val;
                        character.DetailsUI.text = character.FormatDetails;
                    });
                    input.onEndEdit.AddListener(_val => { character.Save(); });

                    var delete = gameObject.transform.Find("Delete").GetComponent<Button>();
                    delete.onClick.ClearAll();
                    delete.onClick.AddListener(() =>
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
                addButton.onClick.AddListener(() =>
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
                    input.onValueChanged.AddListener(_val =>
                    {
                        character.CharacterLore[index] = _val;
                        character.DetailsUI.text = character.FormatDetails;
                    });
                    input.onEndEdit.AddListener(_val => { character.Save(); });

                    var delete = gameObject.transform.Find("Delete").GetComponent<Button>();
                    delete.onClick.ClearAll();
                    delete.onClick.AddListener(() =>
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
                addButton.onClick.AddListener(() =>
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
                    input.onValueChanged.AddListener(_val =>
                    {
                        character.CharacterAbilities[index] = _val;
                        character.DetailsUI.text = character.FormatDetails;
                    });
                    input.onEndEdit.AddListener(_val => { character.Save(); });

                    var delete = gameObject.transform.Find("Delete").GetComponent<Button>();
                    delete.onClick.ClearAll();
                    delete.onClick.AddListener(() =>
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
                addButton.onClick.AddListener(() =>
                {
                    character.CharacterAbilities.Add("New Detail");
                    character.DetailsUI.text = character.FormatDetails;
                    character.Save();
                    OpenCharacterEditor(character);
                });
            }
        }

        public void OpenTimelineEditor(TimelinePlanner timeline)
        {
            SetEditorsInactive();
            editors[3].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_4];

            timelineEditorName.onValueChanged.ClearAll();
            timelineEditorName.text = timeline.Name;
            timelineEditorName.onValueChanged.AddListener(_val =>
            {
                timeline.Name = _val;
                timeline.NameUI.text = _val;
            });
            timelineEditorName.onEndEdit.ClearAll();
            timelineEditorName.onEndEdit.AddListener(_val => SaveTimelines());
        }

        public void OpenEventEditor(TimelinePlanner.Event level)
        {
            SetEditorsInactive();
            editors[4].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_4];

            eventEditorName.onValueChanged.ClearAll();
            eventEditorName.text = level.Name;
            eventEditorName.onValueChanged.AddListener(_val =>
            {
                level.Name = _val;
                level.NameUI.text = $"{level.ElementType}: {level.Name}";
            });
            eventEditorName.onEndEdit.ClearAll();
            eventEditorName.onEndEdit.AddListener(_val => SaveTimelines());

            eventEditorDescription.onValueChanged.ClearAll();
            eventEditorDescription.text = level.Description;
            eventEditorDescription.onValueChanged.AddListener(_val =>
            {
                level.Description = _val;
                level.DescriptionUI.text = _val;
            });
            eventEditorDescription.onEndEdit.ClearAll();
            eventEditorDescription.onEndEdit.AddListener(_val => SaveTimelines());

            eventEditorPath.onValueChanged.ClearAll();
            eventEditorPath.text = level.Path == null ? "" : level.Path;
            eventEditorPath.onValueChanged.AddListener(_val => { level.Path = _val; });
            eventEditorPath.onEndEdit.ClearAll();
            eventEditorPath.onEndEdit.AddListener(_val => SaveTimelines());

            eventEditorType.onValueChanged.ClearAll();
            eventEditorType.value = (int)level.ElementType;
            eventEditorType.onValueChanged.AddListener(_val =>
            {
                level.ElementType = (TimelinePlanner.Event.Type)_val;
                level.NameUI.text = $"{level.ElementType}: {level.Name}";
                SaveTimelines();
            });
        }

        public void OpenScheduleEditor(SchedulePlanner schedule)
        {
            editors[5].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_5];

            scheduleEditorDescription.onValueChanged.ClearAll();
            scheduleEditorDescription.text = schedule.Description;
            scheduleEditorDescription.onValueChanged.AddListener(_val =>
            {
                schedule.Description = _val;
                schedule.TextUI.text = schedule.Text;
                schedule.hasBeenChecked = false;
            });
            scheduleEditorDescription.onEndEdit.ClearAll();
            scheduleEditorDescription.onEndEdit.AddListener(_val => SaveSchedules());

            scheduleEditorYear.onValueChanged.ClearAll();
            scheduleEditorYear.text = schedule.DateTime.Year.ToString();
            scheduleEditorYear.onValueChanged.AddListener(_val =>
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
            scheduleEditorMonth.onValueChanged.AddListener(_val =>
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
            scheduleEditorDay.onValueChanged.AddListener(_val =>
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
            scheduleEditorHour.onValueChanged.AddListener(_val =>
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
            scheduleEditorMinute.onValueChanged.AddListener(_val =>
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

        public void OpenNoteEditor(NotePlanner note)
        {
            editors[6].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_6];

            noteEditorName.onValueChanged.ClearAll();
            noteEditorName.text = note.Name;
            noteEditorName.onValueChanged.AddListener(_val =>
            {
                note.Name = _val;
                note.TitleUI.text = $"Note - {note.Name}";
            });
            noteEditorName.onEndEdit.ClearAll();
            noteEditorName.onEndEdit.AddListener(_val => SaveNotes());

            noteEditorText.onValueChanged.ClearAll();
            noteEditorText.text = note.Text;
            noteEditorText.onValueChanged.AddListener(_val =>
            {
                note.Text = _val;
                note.TextUI.text = _val;
            });
            noteEditorText.onEndEdit.ClearAll();
            noteEditorText.onEndEdit.AddListener(_val => SaveNotes());

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
            noteEditorReset.onClick.AddListener(() =>
            {
                note.Position = Vector2.zero;
                note.Scale = Vector2.one;
                note.Size = new Vector2(300f, 150f);
                SaveNotes();
            });
        }

        public void SetNoteColors(NotePlanner note)
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
                toggle.onValueChanged.AddListener(_val =>
                {
                    note.Color = index;
                    SetNoteColors(note);
                });

                num++;
            }
        }

        public void OpenOSTEditor(OSTPlanner ost)
        {
            editors[7].SetActive(true); editorTitlePanel.gameObject.SetActive(true);
            editorTitlePanel.color = EditorThemeManager.CurrentTheme.ColorGroups[ThemeGroup.Tab_Color_7];

            ostEditorPath.onValueChanged.ClearAll();
            ostEditorPath.onEndEdit.ClearAll();
            ostEditorPath.text = ost.Path;
            ostEditorPath.onValueChanged.AddListener(_val => { ost.Path = _val; });
            ostEditorPath.onEndEdit.AddListener(_val => { SaveOST(); });

            ostEditorName.onValueChanged.ClearAll();
            ostEditorName.onEndEdit.ClearAll();
            ostEditorName.text = ost.Name;
            ostEditorName.onValueChanged.AddListener(_val =>
            {
                ost.Name = _val;
                ost.TextUI.text = _val;
            });
            ostEditorName.onEndEdit.AddListener(_val => SaveOST());

            ostEditorPlay.onClick.ClearAll();
            ostEditorPlay.onClick.AddListener(ost.Play);

            ostEditorStop.onClick.ClearAll();
            ostEditorStop.onClick.AddListener(StopOST);

            ostEditorUseGlobal.onClick.ClearAll();
            ostEditorUseGlobal.onClick.AddListener(() =>
            {
                ost.UseGlobal = !ost.UseGlobal;
                ostEditorUseGlobalText.text = ost.UseGlobal.ToString();
                SaveOST();
            });

            ostEditorUseGlobalText.text = ost.UseGlobal.ToString();

            ostEditorIndex.onValueChanged.ClearAll();
            ostEditorIndex.text = ost.Index.ToString();
            ostEditorIndex.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                {
                    ost.Index = num;

                    var list = osts.OrderBy(x => x.Index).ToList();

                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].ID == ost.ID && ostEditorIndex.text != i.ToString())
                            StartCoroutine(SetIndex(i));

                        list[i].Index = i;
                        list[i].GameObject.transform.SetSiblingIndex(i);
                    }
                }
            });
            TriggerHelper.AddEventTriggers(ostEditorIndex.gameObject, TriggerHelper.ScrollDeltaInt(ostEditorIndex));
        }

        IEnumerator SetIndex(int i)
        {
            yield return null;
            ostEditorIndex.text = i.ToString();
        }

        public void StopOST()
        {
            Destroy(OSTAudioSource);

            var list = osts.ToList();

            for (int i = 0; i < list.Count; i++)
                list[i].playing = false;

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
    }
}
