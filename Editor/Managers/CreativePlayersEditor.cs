using BetterLegacy.Components;
using BetterLegacy.Core;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using LSFunctions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Managers
{
    public class CreativePlayersEditor : MonoBehaviour
    {
        public static CreativePlayersEditor inst;

        public static GameObject editorDialogObject;
        public static Transform editorDialogTransform;
        public static Transform editorDialogTitle;
        public static Transform editorDialogSpacer;
        public static Transform editorDialogContent;
        public static Transform editorDialogText;

        public static Dropdown playerModelDropdown;
        public static InputField playerModelIndexIF;
        public static int playerModelIndex = 0;

        public static GameObject objectDialog;
        public static Transform objectDialogTF;
        public static Transform objectDialogContent;
        public static Text objectTitle;
        public static Text objectText;

        public static GameObject customObjectButtonPrefab;

        public static InputField searchField;
        public static string searchTerm;

        public static string customSearchTerm;

        public static List<GameObject> playerObjects = new List<GameObject>();

        public static Font editorFont;

        public static string currentID;

        public static bool debug = false;

        public RectTransform visibilityContent;

        public static void Init()
        {
            var editorSystems = EditorManager.inst.transform.parent;
            var gameObject = new GameObject("PlayerEditorManager");
            gameObject.transform.SetParent(editorSystems);
            gameObject.AddComponent<CreativePlayersEditor>();
        }

        void Awake()
        {
            inst = this;

            editorFont = FontManager.inst.Inconsolata;

            editorDialogObject = Instantiate(EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog.gameObject);
            editorDialogTransform = editorDialogObject.transform;
            editorDialogObject.name = "PlayerEditorDialog";
            editorDialogObject.layer = 5;
            editorDialogTransform.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs").transform);
            editorDialogTransform.localScale = Vector3.one;
            editorDialogTransform.position = new Vector3(1537.5f, 714.945f, 0f) * EditorManager.inst.ScreenScale;
            editorDialogObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 32f);

            editorDialogTitle = editorDialogTransform.GetChild(0);
            editorDialogTitle.GetComponent<Image>().color = LSColors.HexToColor("E57373");
            editorDialogTitle.GetChild(0).GetComponent<Text>().text = "- Player Editor -";

            editorDialogSpacer = editorDialogTransform.GetChild(1);
            editorDialogSpacer.GetComponent<RectTransform>().sizeDelta = new Vector2(765f, 54f);
            //GridLayoutGroup playerFunctionsGroup = editorDialogSpacer.gameObject.AddComponent<GridLayoutGroup>();
            //playerFunctionsGroup.cellSize = new Vector2(248f, 50f);
            //playerFunctionsGroup.spacing = new Vector2(8f, 8f);

            var label = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content").transform.GetChild(2).gameObject;
            var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");
            var vector2Input = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position");
            var boolInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle");
            var sliderFullInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/bpm");
            var stringInput = GameObject.Find("TimelineBar/GameObject/Time Input");
            var dropdownInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown");
            var colorsInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/color/color");

            //Dropdown
            {
                GameObject x = Instantiate(dropdownInput);
                x.transform.SetParent(editorDialogSpacer);
                x.transform.localScale = Vector3.one;
                x.name = "models";

                Destroy(x.GetComponent<HoverTooltip>());
                Destroy(x.GetComponent<HideDropdownOptions>());

                var xrt = x.GetComponent<RectTransform>();
                xrt.anchoredPosition = new Vector2(124f, 30f);
                xrt.sizeDelta = new Vector2(248f, 50f);

                playerModelDropdown = x.GetComponent<Dropdown>();
                playerModelDropdown.onValueChanged.RemoveAllListeners();
                playerModelDropdown.options.Clear();
                playerModelDropdown.options = new List<Dropdown.OptionData>();
            }

            //Player Index
            {
                var b1 = Instantiate(singleInput.transform);
                b1.transform.SetParent(editorDialogSpacer);
                b1.transform.localScale = Vector3.one;
                b1.name = "index";

                Destroy(b1.GetComponent<InputFieldSwapper>());
                Destroy(b1.GetComponent<EventInfo>());

                var b1RT = b1.GetComponent<RectTransform>();
                b1RT.anchoredPosition = new Vector2(256f, -8f);
                b1RT.sizeDelta = new Vector2(100f, 50f);

                b1.transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(50f, 32f);

                playerModelIndexIF = b1.GetComponent<InputField>();
                playerModelIndexIF.onValueChanged.ClearAll();
            }

            //Button 1
            {
                var b1 = Instantiate(EditorManager.inst.folderButtonPrefab);
                b1.transform.SetParent(editorDialogSpacer);
                b1.transform.localScale = Vector3.one;
                b1.name = "save";

                var b1RT = b1.GetComponent<RectTransform>();
                b1RT.anchoredPosition = new Vector2(436f, 55f);
                b1RT.anchoredPosition = new Vector2(436f, 55f);
                //b1RT.anchoredPosition = new Vector2(366f, 55f);
                b1RT.sizeDelta = new Vector2(100f, 50f);

                b1.transform.GetChild(0).GetComponent<Text>().text = "Save All";
                var butt = b1.GetComponent<Button>();
                butt.onClick.RemoveAllListeners();
                butt.onClick.AddListener(delegate ()
                {
                    PlayerManager.SaveGlobalModels();
                });
            }

            //Button 2
            {
                var b1 = Instantiate(EditorManager.inst.folderButtonPrefab);
                b1.transform.SetParent(editorDialogSpacer);
                b1.transform.localScale = Vector3.one;
                b1.name = "create";

                var b1RT = b1.GetComponent<RectTransform>();
                b1RT.anchoredPosition = new Vector2(546f, 55f);
                //b1RT.anchoredPosition = new Vector2(476f, 55f);
                b1RT.sizeDelta = new Vector2(120f, 50f);

                b1.transform.GetChild(0).GetComponent<Text>().text = "Create New";
                var butt = b1.GetComponent<Button>();
                butt.onClick.RemoveAllListeners();
                butt.onClick.AddListener(delegate ()
                {
                    var num = playerModelDropdown.options.Count;

                    PlayerManager.CreateNewPlayerModel();
                    PlayerManager.SetPlayerModel(playerModelIndex, PlayerManager.PlayerModels.ElementAt(num).Key);
                    PlayerManager.RespawnPlayers();

                    inst.StartCoroutine(RenderDialog());
                });
            }

            //Button 3
            {
                var b1 = Instantiate(EditorManager.inst.folderButtonPrefab);
                b1.transform.SetParent(editorDialogSpacer);
                b1.transform.localScale = Vector3.one;
                b1.name = "reload";

                var b1RT = b1.GetComponent<RectTransform>();
                b1RT.anchoredPosition = new Vector2(675f, 55f);
                //b1RT.anchoredPosition = new Vector2(605f, 55f);
                b1RT.sizeDelta = new Vector2(80f, 50f);

                b1.transform.GetChild(0).GetComponent<Text>().text = "Reload";
                var butt = b1.GetComponent<Button>();
                butt.onClick.RemoveAllListeners();
                butt.onClick.AddListener(delegate ()
                {
                    PlayerManager.LoadGlobalModels();
                    PlayerManager.RespawnPlayers();

                    inst.StartCoroutine(RenderDialog());
                });
            }

            //Button 4
            bool v = false;
            if (v)
            {
                var b1 = Instantiate(EditorManager.inst.folderButtonPrefab);
                b1.transform.SetParent(editorDialogSpacer);
                b1.transform.localScale = Vector3.one;
                b1.name = "clone";

                var b1RT = b1.GetComponent<RectTransform>();
                b1RT.anchoredPosition = new Vector2(685f, 55f);
                b1RT.sizeDelta = new Vector2(70f, 50f);

                b1.transform.GetChild(0).GetComponent<Text>().text = "Clone";
                var butt = b1.GetComponent<Button>();
                butt.onClick.RemoveAllListeners();
                butt.onClick.AddListener(delegate ()
                {
                    EditorManager.inst.DisplayNotification("no func", 2f, EditorManager.NotificationType.Error);
                    inst.StartCoroutine(RenderDialog());
                });
            }

            editorDialogText = editorDialogTransform.GetChild(2);

            editorDialogText.SetSiblingIndex(1);

            editorDialogText.GetComponent<Text>().text = "You are currently editing the current player model. Have fun messing around with what you can do here!";

            var search = Instantiate(EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("search-box").gameObject);
            search.transform.SetParent(editorDialogTransform);
            search.transform.localScale = Vector3.one;
            search.name = "search";

            searchField = search.transform.GetChild(0).GetComponent<InputField>();

            searchField.onValueChanged.ClearAll();
            searchField.text = "";
            searchField.onValueChanged.AddListener(delegate (string _val)
            {
                searchTerm = _val;
                inst.StartCoroutine(RenderDialog());
            });

            search.transform.GetChild(0).Find("Placeholder").GetComponent<Text>().text = "Search for value...";

            var scrollView = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View"));
            editorDialogContent = scrollView.transform;
            editorDialogContent.SetParent(editorDialogTransform);
            editorDialogContent.localScale = Vector3.one;
            scrollView.name = "Scroll View";

            //var glg = scrollView.AddComponent<GridLayoutGroup>();
            //glg.cellSize = new Vector2(124f, 64f);
            //glg.spacing = new Vector2(4f, 4f);

            LSHelpers.DeleteChildren(scrollView.transform.Find("Viewport/Content"), false);

            scrollView.GetComponent<RectTransform>().sizeDelta = new Vector2(765f, 512f);

            EditorHelper.AddEditorDialog("Player Editor", editorDialogObject);

            objectDialog = Instantiate(EditorManager.inst.GetDialog("Background Editor").Dialog.gameObject);
            objectDialogTF = objectDialog.transform;
            objectDialog.name = "PlayerObjectEditorDialog";
            objectDialog.layer = 5;
            objectDialogTF.SetParent(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs").transform);
            objectDialogTF.transform.localScale = Vector3.one;
            objectDialogTF.position = new Vector3(1537.5f, 714.945f, 0f) * EditorManager.inst.ScreenScale;

            //Modify
            {
                var rt = objectDialog.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, 32f);
                rt.sizeDelta = new Vector2(0f, 32f);

                var data = objectDialogTF.Find("data");
                var left = data.Find("left");
                var right = data.Find("right");
                var titlebar = data.Find("titlebar");
                objectDialogContent = right.Find("backgrounds/viewport/content");

                customObjectButtonPrefab = objectDialogContent.GetChild(0).gameObject;
                customObjectButtonPrefab.transform.SetParent(null);

                var bg = right.Find("backgrounds");
                bg.gameObject.name = "objects";
                bg.localRotation = Quaternion.identity;

                right.Find("search/Placeholder").GetComponent<Text>().text = "Search for object...";
                right.Find("create/Text").GetComponent<Text>().text = "Create New Custom Object";

                titlebar.Find("left/title").GetComponent<Text>().text = "- Current Object Props -";
                titlebar.Find("right/title").GetComponent<Text>().text = "- Custom Object List -";

                var toggle = left.GetChild(1).GetChild(0).gameObject;
                toggle.transform.SetParent(null);

                var objtodel = new List<GameObject>
                    {
                        left.GetChild(2).gameObject,
                        left.GetChild(3).gameObject,
                        left.GetChild(8).gameObject,
                        left.GetChild(9).gameObject,
                        left.GetChild(12).gameObject,
                        left.GetChild(13).gameObject,
                        left.GetChild(14).gameObject,
                        left.GetChild(15).gameObject,
                        left.GetChild(16).gameObject
                    };

                foreach (var del in objtodel)
                    DestroyImmediate(del);

                left.GetChild(0).GetChild(0).GetComponent<Text>().text = "Name";

                left.Find("name/name").GetComponent<RectTransform>().sizeDelta = new Vector2(366f, 32f);

                var scaLabel = left.GetChild(4).gameObject;
                var scaField = left.GetChild(5).gameObject;

                left.Find("position/x/text-field").GetComponent<RectTransform>().sizeDelta = new Vector2(111f, 32f);
                left.Find("position/y/text-field").GetComponent<RectTransform>().sizeDelta = new Vector2(111f, 32f);
                left.Find("position").GetComponent<HorizontalLayoutGroup>().childControlWidth = false;

                left.Find("scale/x/text-field").GetComponent<RectTransform>().sizeDelta = new Vector2(111f, 32f);
                left.Find("scale/y/text-field").GetComponent<RectTransform>().sizeDelta = new Vector2(111f, 32f);
                left.Find("scale").GetComponent<HorizontalLayoutGroup>().childControlWidth = false;

                var rotLabel = Instantiate(scaLabel);
                rotLabel.transform.SetParent(left);
                rotLabel.transform.localScale = Vector3.one;
                rotLabel.transform.SetSiblingIndex(6);
                rotLabel.name = "label";

                DestroyImmediate(rotLabel.transform.GetChild(1).gameObject);
                rotLabel.transform.GetChild(0).GetComponent<Text>().text = "Rotation";

                var rotField = Instantiate(scaField);
                rotField.transform.SetParent(left);
                rotField.transform.localScale = Vector3.one;
                rotField.transform.SetSiblingIndex(7);
                rotField.name = "rotation";

                DestroyImmediate(rotField.transform.GetChild(1).gameObject);

                //Custom Color
                {
                    var rotOffsetLabel = Instantiate(rotLabel);
                    rotOffsetLabel.transform.SetParent(left);
                    rotOffsetLabel.transform.localScale = Vector3.one;
                    rotOffsetLabel.name = "label";

                    rotOffsetLabel.transform.GetChild(0).GetComponent<Text>().text = "Custom Color";

                    var custom = Instantiate(left.Find("name").gameObject);
                    custom.transform.SetParent(left);
                    custom.transform.localScale = Vector3.one;
                    custom.name = "custom color";
                }

                //Opacity
                {
                    var opacityLabel = Instantiate(rotLabel);
                    opacityLabel.transform.SetParent(left);
                    opacityLabel.transform.localScale = Vector3.one;
                    opacityLabel.name = "label";

                    opacityLabel.transform.GetChild(0).GetComponent<Text>().text = "Opacity";

                    var opacity = Instantiate(rotField);
                    opacity.transform.SetParent(left);
                    opacity.transform.localScale = Vector3.one;
                    opacity.name = "opacity";
                }

                //Depth
                {
                    var depthLabel = Instantiate(rotLabel);
                    depthLabel.transform.SetParent(left);
                    depthLabel.transform.localScale = Vector3.one;
                    depthLabel.name = "label";

                    depthLabel.transform.GetChild(0).GetComponent<Text>().text = "Depth";

                    var depth = Instantiate(rotField);
                    depth.transform.SetParent(left);
                    depth.transform.localScale = Vector3.one;
                    depth.name = "depth";
                }

                //Parent Dropdown
                {
                    var posOffsetLabel = Instantiate(rotLabel);
                    posOffsetLabel.transform.SetParent(left);
                    posOffsetLabel.transform.localScale = Vector3.one;
                    posOffsetLabel.name = "label";

                    posOffsetLabel.transform.GetChild(0).GetComponent<Text>().text = "Parent";

                    GameObject x = Instantiate(dropdownInput);
                    x.transform.SetParent(left);
                    x.transform.localScale = Vector3.one;
                    x.name = "parent";

                    Destroy(x.GetComponent<HoverTooltip>());
                    Destroy(x.GetComponent<HideDropdownOptions>());

                    var xrt = x.GetComponent<RectTransform>();
                    xrt.anchoredPosition = new Vector2(124f, 30f);
                    xrt.sizeDelta = new Vector2(248f, 32f);

                    var parentDropdown = x.GetComponent<Dropdown>();
                    parentDropdown.onValueChanged.RemoveAllListeners();
                    parentDropdown.options.Clear();
                    parentDropdown.options = new List<Dropdown.OptionData>()
                        {
                            new Dropdown.OptionData("Head"),
                            new Dropdown.OptionData("Boost"),
                            new Dropdown.OptionData("Boost Tail"),
                            new Dropdown.OptionData("Tail 1"),
                            new Dropdown.OptionData("Tail 2"),
                            new Dropdown.OptionData("Tail 3"),
                            new Dropdown.OptionData("Face"),
                        };
                }

                //Parent Offset
                {
                    var posOffsetLabel = Instantiate(rotLabel);
                    posOffsetLabel.transform.SetParent(left);
                    posOffsetLabel.transform.localScale = Vector3.one;
                    posOffsetLabel.name = "label";

                    posOffsetLabel.transform.GetChild(0).GetComponent<Text>().text = "Parent Pos Offset";

                    var posOffset = Instantiate(rotField);
                    posOffset.transform.SetParent(left);
                    posOffset.transform.localScale = Vector3.one;
                    posOffset.name = "parent pos offset";

                    var scaOffsetLabel = Instantiate(rotLabel);
                    scaOffsetLabel.transform.SetParent(left);
                    scaOffsetLabel.transform.localScale = Vector3.one;
                    scaOffsetLabel.name = "label";

                    scaOffsetLabel.transform.GetChild(0).GetComponent<Text>().text = "Parent Sca Offset";

                    var scaOffset = Instantiate(rotField);
                    scaOffset.transform.SetParent(left);
                    scaOffset.transform.localScale = Vector3.one;
                    scaOffset.name = "parent sca offset";

                    var scaActiveParent = new GameObject("p");
                    scaActiveParent.transform.SetParent(scaOffset.transform);
                    scaActiveParent.transform.localScale = Vector3.one;
                    scaActiveParent.transform.SetAsFirstSibling();
                    scaActiveParent.AddComponent<RectTransform>();

                    var scaActive = Instantiate(toggle);
                    scaActive.transform.SetParent(scaActiveParent.transform);
                    scaActive.transform.localScale = Vector3.one;
                    scaActive.name = "parent sca active";
                    scaActive.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                    var rotOffsetLabel = Instantiate(rotLabel);
                    rotOffsetLabel.transform.SetParent(left);
                    rotOffsetLabel.transform.localScale = Vector3.one;
                    rotOffsetLabel.name = "label";

                    rotOffsetLabel.transform.GetChild(0).GetComponent<Text>().text = "Parent Rot Offset";

                    var rotOffset = Instantiate(rotField);
                    rotOffset.transform.SetParent(left);
                    rotOffset.transform.localScale = Vector3.one;
                    rotOffset.name = "parent rot offset";

                    var rotActiveParent = new GameObject("p");
                    rotActiveParent.transform.SetParent(rotOffset.transform);
                    rotActiveParent.transform.localScale = Vector3.one;
                    rotActiveParent.transform.SetAsFirstSibling();
                    rotActiveParent.AddComponent<RectTransform>();

                    var rotActive = Instantiate(toggle);
                    rotActive.transform.SetParent(rotActiveParent.transform);
                    rotActive.transform.localScale = Vector3.one;
                    rotActive.name = "parent rot active";
                    rotActive.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                }

                //Shape
                {
                    var depthLabel = Instantiate(rotLabel);
                    depthLabel.transform.SetParent(left);
                    depthLabel.transform.localScale = Vector3.one;
                    depthLabel.name = "label";

                    depthLabel.transform.GetChild(0).GetComponent<Text>().text = "Shape";

                    //Shape
                    {
                        var bar = Instantiate(singleInput);
                        if (bar.GetComponent<InputField>())
                            Destroy(bar.GetComponent<InputField>());
                        if (bar.GetComponent<EventInfo>())
                            Destroy(bar.GetComponent<EventInfo>());
                        if (bar.GetComponent<EventTrigger>())
                            Destroy(bar.GetComponent<EventTrigger>());

                        LSHelpers.DeleteChildren(bar.transform);
                        bar.transform.SetParent(left);
                        bar.transform.localScale = Vector3.one;
                        bar.name = "shape";

                        bar.GetComponent<Image>().enabled = true;
                        bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

                        GameObject x = Instantiate(dropdownInput);
                        x.transform.SetParent(bar.transform);
                        x.transform.localScale = Vector3.one;
                        x.name = "x";

                        RectTransform xRT = x.GetComponent<RectTransform>();
                        xRT.anchoredPosition = new Vector2(624f, -16f);
                        xRT.sizeDelta = new Vector2(248f, 32f);

                        Destroy(x.GetComponent<HoverTooltip>());

                        Dropdown dropdown = x.GetComponent<Dropdown>();
                        dropdown.options.Clear();
                        dropdown.onValueChanged.RemoveAllListeners();

                        dropdown.options = GetShapes();

                        var hide = x.AddComponent<HideDropdownOptions>();
                        hide.DisabledOptions = new List<bool>
                            {
                                false,
                                false,
                                false,
                                false,
                                true,
                                false,
                                true,
                                false,
                                false,
                            };
                    }

                    //Shape Option
                    {
                        var bar = Instantiate(singleInput);
                        if (bar.GetComponent<InputField>())
                            Destroy(bar.GetComponent<InputField>());
                        if (bar.GetComponent<EventInfo>())
                            Destroy(bar.GetComponent<EventInfo>());
                        if (bar.GetComponent<EventTrigger>())
                            Destroy(bar.GetComponent<EventTrigger>());

                        LSHelpers.DeleteChildren(bar.transform);
                        bar.transform.SetParent(left);
                        bar.transform.localScale = Vector3.one;
                        bar.name = "shape option";

                        bar.GetComponent<Image>().enabled = true;
                        bar.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);

                        GameObject x = Instantiate(dropdownInput);
                        x.transform.SetParent(bar.transform);
                        x.transform.localScale = Vector3.one;
                        x.name = "x";

                        RectTransform xRT = x.GetComponent<RectTransform>();
                        xRT.anchoredPosition = new Vector2(624f, -16f);
                        xRT.sizeDelta = new Vector2(248f, 32f);

                        Destroy(x.GetComponent<HoverTooltip>());
                        Destroy(x.GetComponent<HideDropdownOptions>());

                        Dropdown dropdown = x.GetComponent<Dropdown>();
                        dropdown.options.Clear();
                        dropdown.onValueChanged.RemoveAllListeners();

                        dropdown.options = GetShapeOptions(0);
                    }
                }

                //Visibility
                {
                    var posOffsetLabel = Instantiate(rotLabel);
                    posOffsetLabel.transform.SetParent(left);
                    posOffsetLabel.transform.localScale = Vector3.one;
                    posOffsetLabel.name = "label";

                    posOffsetLabel.transform.GetChild(0).GetComponent<Text>().text = "Visibility";

                    var requireAllLabel = Instantiate(rotLabel);
                    requireAllLabel.transform.SetParent(left);
                    requireAllLabel.transform.localScale = Vector3.one;
                    requireAllLabel.name = "label";

                    requireAllLabel.transform.GetChild(0).GetComponent<Text>().text = "Require All";

                    var scaActiveParent = new GameObject("require all");
                    scaActiveParent.transform.SetParent(left);
                    scaActiveParent.transform.localScale = Vector3.one;
                    var requireAllRT = scaActiveParent.AddComponent<RectTransform>();
                    requireAllRT.sizeDelta = new Vector2(351f, 32f);

                    var scaActive = Instantiate(toggle);
                    scaActive.transform.SetParent(scaActiveParent.transform);
                    scaActive.transform.localScale = Vector3.one;
                    scaActive.name = "toggle";
                    scaActive.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                    // Visibility list
                    var visibilityScrollRect = new GameObject("ScrollRect Visibility");
                    visibilityScrollRect.transform.SetParent(left);
                    visibilityScrollRect.transform.localScale = Vector3.one;
                    var visibilityScrollRectRT = visibilityScrollRect.AddComponent<RectTransform>();
                    visibilityScrollRectRT.anchoredPosition = new Vector2(0f, 16f);
                    visibilityScrollRectRT.sizeDelta = new Vector2(400f, 250f);
                    var visibilityScrollRectSR = visibilityScrollRect.AddComponent<ScrollRect>();

                    var visibilityMaskGO = new GameObject("Mask");
                    visibilityMaskGO.transform.SetParent(visibilityScrollRectRT);
                    visibilityMaskGO.transform.localScale = Vector3.one;
                    var visibilityMaskRT = visibilityMaskGO.AddComponent<RectTransform>();
                    visibilityMaskRT.anchoredPosition = new Vector2(0f, 0f);
                    visibilityMaskRT.anchorMax = new Vector2(1f, 1f);
                    visibilityMaskRT.anchorMin = new Vector2(0f, 0f);
                    visibilityMaskRT.sizeDelta = new Vector2(0f, 0f);
                    var visibilityMaskImage = visibilityMaskGO.AddComponent<Image>();
                    visibilityMaskImage.color = new Color(1f, 1f, 1f, 0.04f);
                    visibilityMaskGO.AddComponent<Mask>();

                    var visibilityContentGO = new GameObject("Content");
                    visibilityContentGO.transform.SetParent(visibilityMaskRT);
                    visibilityContentGO.transform.localScale = Vector3.one;
                    visibilityContent = visibilityContentGO.AddComponent<RectTransform>();

                    visibilityContent.anchoredPosition = new Vector2(0f, -16f);
                    visibilityContent.anchorMax = new Vector2(0f, 1f);
                    visibilityContent.anchorMin = new Vector2(0f, 1f);
                    visibilityContent.pivot = new Vector2(0f, 1f);
                    visibilityContent.sizeDelta = new Vector2(400f, 250f);

                    var visibilityContentCSF = visibilityContentGO.AddComponent<ContentSizeFitter>();
                    visibilityContentCSF.horizontalFit = ContentSizeFitter.FitMode.MinSize;
                    visibilityContentCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

                    var visibilityContentVLG = visibilityContentGO.AddComponent<VerticalLayoutGroup>();
                    visibilityContentVLG.childControlHeight = false;
                    visibilityContentVLG.childForceExpandHeight = false;
                    visibilityContentVLG.spacing = 4f;

                    var visibilityContentLE = visibilityContentGO.AddComponent<LayoutElement>();
                    visibilityContentLE.layoutPriority = 10000;
                    visibilityContentLE.minWidth = 349;

                    visibilityScrollRectSR.content = visibilityContent;
                }
                //{
                //    var posOffsetLabel = Instantiate(rotLabel);
                //    posOffsetLabel.transform.SetParent(left);
                //    posOffsetLabel.transform.localScale = Vector3.one;
                //    posOffsetLabel.name = "label";

                //    posOffsetLabel.transform.GetChild(0).GetComponent<Text>().text = "Visibility";

                //    GameObject x = Instantiate(dropdownInput);
                //    x.transform.SetParent(left);
                //    x.transform.localScale = Vector3.one;
                //    x.name = "visibility";

                //    Destroy(x.GetComponent<HoverTooltip>());
                //    Destroy(x.GetComponent<HideDropdownOptions>());

                //    var xrt = x.GetComponent<RectTransform>();
                //    xrt.anchoredPosition = new Vector2(124f, 30f);
                //    xrt.sizeDelta = new Vector2(248f, 32f);

                //    var parentDropdown = x.GetComponent<Dropdown>();
                //    parentDropdown.onValueChanged.RemoveAllListeners();
                //    parentDropdown.options.Clear();
                //    parentDropdown.options = new List<Dropdown.OptionData>()
                //    {
                //        new Dropdown.OptionData("Always"),
                //        new Dropdown.OptionData("Boosting"),
                //        new Dropdown.OptionData("Being Hit"),
                //        new Dropdown.OptionData("In Zen Mode"),
                //        new Dropdown.OptionData("Health Percentage"),
                //        new Dropdown.OptionData("Health Equals Greater"),
                //        new Dropdown.OptionData("Health Equals"),
                //        new Dropdown.OptionData("Health Greater"),
                //        new Dropdown.OptionData("Key Held"),
                //    };
                //}

                ////Visibility Health Percentage
                //{
                //    var rotOffsetLabel = Instantiate(rotLabel);
                //    rotOffsetLabel.transform.SetParent(left);
                //    rotOffsetLabel.transform.localScale = Vector3.one;
                //    rotOffsetLabel.name = "label";

                //    rotOffsetLabel.transform.GetChild(0).GetComponent<Text>().text = "Visibility Properties";

                //    var rotOffset = Instantiate(rotField);
                //    rotOffset.transform.SetParent(left);
                //    rotOffset.transform.localScale = Vector3.one;
                //    rotOffset.name = "visibility health percentage";

                //    var rotActiveParent = new GameObject("p");
                //    rotActiveParent.transform.SetParent(rotOffset.transform);
                //    rotActiveParent.transform.localScale = Vector3.one;
                //    rotActiveParent.transform.SetAsFirstSibling();
                //    rotActiveParent.AddComponent<RectTransform>();

                //    var rotActive = Instantiate(toggle);
                //    rotActive.transform.SetParent(rotActiveParent.transform);
                //    rotActive.transform.localScale = Vector3.one;
                //    rotActive.name = "visibility not";
                //    rotActive.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                //}

                //Color
                {
                    var colorParent = left.Find("color");
                    LSHelpers.DeleteChildren(colorParent);

                    Destroy(colorParent.GetComponent<GridLayoutGroup>());

                    GameObject x = Instantiate(colorsInput);
                    x.transform.SetParent(colorParent);
                    x.transform.localScale = Vector3.one;
                    x.name = "color";
                    if (x.GetComponent<HoverTooltip>())
                        Destroy(x.GetComponent<HoverTooltip>());

                    for (int i = 19; i < 27; i++)
                    {
                        var colorButton = Instantiate(x.transform.GetChild(0).gameObject);
                        colorButton.name = i.ToString();
                        colorButton.transform.SetParent(x.transform);
                        colorButton.transform.localScale = Vector3.one;
                    }

                    x.GetComponent<RectTransform>().sizeDelta = new Vector2(351f, 32f);
                    x.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                    colorParent.GetComponent<RectTransform>().sizeDelta = new Vector2(351f, 152f);
                }

                //Set UI Parents
                {
                    var listtoadd = new List<Transform>();
                    for (int i = 0; i < left.transform.childCount; i++)
                        listtoadd.Add(left.transform.GetChild(i));

                    var bmb = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View");

                    var e = Instantiate(bmb);

                    var scrollView2 = e.transform;

                    scrollView2.SetParent(left);
                    scrollView2.localScale = Vector3.one;
                    scrollView2.name = "Object Scroll View";

                    var scrollViewRT = scrollView2.GetComponent<RectTransform>();
                    scrollViewRT.sizeDelta = new Vector2(366f, 690f);

                    var content = scrollView2.Find("Viewport/Content");
                    LSHelpers.DeleteChildren(content);

                    foreach (var l in listtoadd)
                    {
                        l.SetParent(content);
                    }
                }

                //Close
                {
                    var l = titlebar.Find("left");

                    var close = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/x");

                    var exit = Instantiate(close.gameObject);
                    exit.transform.SetParent(l.transform);
                    exit.transform.localScale = Vector3.one;
                    exit.name = "close";

                    exit.GetComponent<RectTransform>().anchoredPosition = new Vector2(-350f, 0f);

                    var exitButton = exit.GetComponent<Button>();
                    exitButton.onClick.ClearAll();
                    exitButton.onClick.AddListener(delegate ()
                    {
                        EditorManager.inst.HideDialog("Player Object Editor");
                    });
                }
            }

            #region Dropdown

            EditorHelper.AddEditorDropdown("Player Editor", "", "Edit", SpriteManager.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_player.png"), delegate ()
            {
                OpenDialog();
            });

            #endregion

            StartCoroutine(GenerateItems());

            EditorHelper.AddEditorDialog("Player Object Editor", objectDialog);
        }

        void Update()
        {
            if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.D))
            {
                DuplicateCustomObject();
            }
        }

        public Dictionary<string, object> UIElements { get; set; }

        public T GetElement<T>(string str) => (T)UIElements[str];

        GameObject BarGenerator(GameObject singleInput, GameObject label, Transform parent, string name, string key)
        {
            var bar = singleInput.Duplicate(parent, name);
            bar.transform.localScale = Vector3.one;
            Destroy(bar.GetComponent<InputField>());
            Destroy(bar.GetComponent<EventInfo>());
            Destroy(bar.GetComponent<EventTrigger>());

            UIElements.Add(key + " GameObject", bar);

            LSHelpers.DeleteChildren(bar.transform);

            TooltipHelper.AddHoverTooltip(bar, key, "");

            var l = label.Duplicate(bar.transform, "label", 0);
            l.transform.localScale = Vector3.one;
            l.transform.GetChild(0).GetComponent<Text>().text = key;
            l.transform.AsRT().sizeDelta = new Vector2(354f, 20f);

            l.transform.GetChild(0).AsRT().anchoredPosition = new Vector2(10f, -5f);

            var image = bar.GetComponent<Image>();
            image.enabled = true;
            image.color = new Color(1f, 1f, 1f, 0.03f);

            return bar;
        }

        public IEnumerator GenerateItems()
        {
            UIElements = new Dictionary<string, object>();

            #region UI Elements

            var label = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content").transform.GetChild(3).gameObject;
            var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");
            var vector2Input = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position");
            var boolInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle");
            var dropdownInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown");
            var sliderFullInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/bpm");
            var stringInput = GameObject.Find("TimelineBar/GameObject/Time Input");
            var colorsInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/color/color");

            #endregion

            var parent = editorDialogContent.Find("Viewport/Content");
            foreach (var key in PlayerModel.Values)
            {
                // String
                if (key == "Base Name" || key.Contains("Color") && key.Contains("Custom"))
                {
                    var bar = BarGenerator(singleInput, label, parent, "input [STRING]", key);

                    var x = Instantiate(stringInput);
                    x.transform.SetParent(bar.transform);
                    x.transform.localScale = Vector3.one;
                    Destroy(x.GetComponent<HoverTooltip>());

                    x.transform.AsRT().sizeDelta = new Vector2(366f, 32f);

                    var xif = x.GetComponent<InputField>();
                    xif.onValueChanged.RemoveAllListeners();
                    xif.characterValidation = InputField.CharacterValidation.None;
                    xif.characterLimit = 0;
                    if (key.Contains("Color") && key.Contains("Custom"))
                        xif.characterLimit = 8;
                    xif.textComponent.fontSize = 18;
                    xif.onEndEdit.RemoveAllListeners();
                    xif.onEndEdit.AddListener(delegate (string _val)
                    {
                        inst.StartCoroutine(RenderDialog());
                    });

                    UIElements.Add(key, xif);
                }

                // Integer
                if (key == "Base Health" || key == "Boost Particles Amount")
                {
                    var bar = BarGenerator(singleInput, label, parent, "input [INT]", key);

                    var vector2 = Instantiate(vector2Input);
                    vector2.transform.SetParent(bar.transform);
                    vector2.transform.localScale = Vector3.one;

                    Destroy(vector2.transform.Find("x").GetComponent<EventInfo>());
                    vector2.transform.Find("x").localScale = Vector3.one;
                    vector2.transform.Find("x").GetChild(0).localScale = Vector3.one;
                    var vxif = vector2.transform.Find("x").GetComponent<InputField>();
                    vxif.onValueChanged.RemoveAllListeners();
                    vxif.characterValidation = InputField.CharacterValidation.Integer;

                    TriggerHelper.AddEventTriggerParams(vector2.transform.Find("x").gameObject, TriggerHelper.ScrollDeltaInt(vxif, 1, 1, 100));

                    TriggerHelper.IncreaseDecreaseButtonsInt(vxif, 1);

                    DestroyImmediate(vector2.transform.Find("y").gameObject);

                    UIElements.Add(key, vxif);
                }

                // Shape Dropdowns
                if (key.Contains("Shape"))
                {
                    // Shape
                    {
                        var bar = BarGenerator(singleInput, label, parent, "input [ENUM]", key);

                        var x = Instantiate(dropdownInput);
                        x.transform.SetParent(bar.transform);
                        x.transform.localScale = Vector3.one;

                        var xRT = x.transform.AsRT();
                        xRT.anchoredPosition = new Vector2(624f, -16f);
                        xRT.sizeDelta = new Vector2(366f, 32f);

                        Destroy(x.GetComponent<HoverTooltip>());

                        var dropdown = x.GetComponent<Dropdown>();
                        dropdown.options.Clear();
                        dropdown.onValueChanged.RemoveAllListeners();

                        dropdown.options = GetShapes();

                        var hide = x.GetComponent<HideDropdownOptions>();
                        hide.remove = true;
                        hide.DisabledOptions = new List<bool>
                        {
                            false,
                            false,
                            false,
                            false,
                            true,
                            false,
                            true,
                            false,
                            false,
                        };

                        UIElements.Add(key, dropdown);
                    }

                    // Shape Option
                    {
                        var bar = BarGenerator(singleInput, label, parent, "input [ENUM]", key + " Option");

                        var x = Instantiate(dropdownInput);
                        x.transform.SetParent(bar.transform);
                        x.transform.localScale = Vector3.one;

                        var xRT = x.transform.AsRT();
                        xRT.anchoredPosition = new Vector2(624f, -16f);
                        xRT.sizeDelta = new Vector2(366f, 32f);

                        Destroy(x.GetComponent<HoverTooltip>());
                        Destroy(x.GetComponent<HideDropdownOptions>());

                        var dropdown = x.GetComponent<Dropdown>();
                        dropdown.options.Clear();
                        dropdown.onValueChanged.RemoveAllListeners();

                        dropdown.options = GetShapeOptions(0);

                        UIElements.Add(key + " Option", dropdown);
                    }
                }

                // Vector2
                if (key.Contains("Position") && !key.Contains("Easing") && !key.Contains("Duration") || key.Contains("Scale") && !key.Contains("Particles") && !key.Contains("Easing") && !key.Contains("Duration") || key.Contains("Force") || key.Contains("Origin"))
                {
                    var bar = BarGenerator(singleInput, label, parent, "input [VECTOR2]", key);

                    var vector2 = Instantiate(vector2Input);
                    vector2.transform.SetParent(bar.transform);
                    vector2.transform.localScale = Vector3.one;

                    Destroy(vector2.transform.Find("x").GetComponent<EventInfo>());
                    vector2.transform.Find("x").localScale = Vector3.one;
                    vector2.transform.Find("x").GetChild(0).localScale = Vector3.one;
                    var vxif = vector2.transform.Find("x").GetComponent<InputField>();
                    vxif.onValueChanged.RemoveAllListeners();

                    Destroy(vector2.transform.Find("y").GetComponent<EventInfo>());
                    vector2.transform.Find("y").localScale = Vector3.one;
                    vector2.transform.Find("y").GetChild(0).localScale = Vector3.one;
                    var vyif = vector2.transform.Find("y").GetComponent<InputField>();
                    vyif.onValueChanged.RemoveAllListeners();

                    TriggerHelper.AddEventTriggerParams(vector2.transform.Find("x").gameObject, TriggerHelper.ScrollDelta(vxif, multi: true), TriggerHelper.ScrollDeltaVector2(vxif, vyif, 0.1f, 10f));
                    TriggerHelper.AddEventTriggerParams(vector2.transform.Find("y").gameObject, TriggerHelper.ScrollDelta(vyif, multi: true), TriggerHelper.ScrollDeltaVector2(vxif, vyif, 0.1f, 10f));

                    TriggerHelper.IncreaseDecreaseButtons(vxif, 1f);
                    TriggerHelper.IncreaseDecreaseButtons(vyif, 1f);

                    UIElements.Add(key + " X", vxif);
                    UIElements.Add(key + " Y", vyif);
                }

                // Single
                if (key.Contains("Scale") && (key.Contains("Start") || key.Contains("End")) && !key.Contains("Pulse") && !key.Contains("Bullet") ||
                    key.Contains("Rotation") && !key.Contains("Easing") ||
                    key == "Tail Base Distance" ||
                    key.Contains("Opacity") && !key.Contains("Easing") ||
                    key.Contains("Lifetime") ||
                    key.Contains("Start Width") ||
                    key.Contains("End Width") ||
                    key.Contains("Trail Time") ||
                    key == "Boost Particles Duration" ||
                    key.Contains("Amount") && !key.Contains("Boost") ||
                    key.Contains("Speed") || key.Contains("Depth") || key == "Pulse Duration" ||
                    key.Contains("Cooldown") || key.Contains("Boost Time") || key == "Bullet Lifetime" || key.Contains("Duration") ||
                    key == "Tail Base Time")
                {
                    var bar = BarGenerator(singleInput, label, parent, "input [FLOAT]", key);

                    var vector2 = Instantiate(vector2Input);
                    vector2.transform.SetParent(bar.transform);
                    vector2.transform.localScale = Vector3.one;

                    Destroy(vector2.transform.Find("x").GetComponent<EventInfo>());
                    vector2.transform.Find("x").localScale = Vector3.one;
                    vector2.transform.Find("x").GetChild(0).localScale = Vector3.one;
                    var vxif = vector2.transform.Find("x").GetComponent<InputField>();
                    vxif.onValueChanged.RemoveAllListeners();

                    TriggerHelper.AddEventTriggerParams(vector2.transform.Find("x").gameObject, TriggerHelper.ScrollDelta(vxif));

                    TriggerHelper.IncreaseDecreaseButtons(vxif, 1f);

                    Destroy(vector2.transform.Find("y").gameObject);

                    UIElements.Add(key, vxif);
                }

                // Tail Base Mode Dropdown
                if (key == "Tail Base Mode")
                {
                    var bar = BarGenerator(singleInput, label, parent, "input [ENUM]", key);

                    var x = Instantiate(dropdownInput);
                    x.transform.SetParent(bar.transform);
                    x.transform.localScale = Vector3.one;

                    var xRT = x.transform.AsRT();
                    xRT.anchoredPosition = new Vector2(624f, -16f);
                    xRT.sizeDelta = new Vector2(366f, 32f);

                    Destroy(x.GetComponent<HoverTooltip>());

                    var dropdown = x.GetComponent<Dropdown>();
                    dropdown.options.Clear();
                    dropdown.onValueChanged.RemoveAllListeners();

                    dropdown.options = new List<Dropdown.OptionData>
                    {
                        new Dropdown.OptionData("Legacy"),
                        new Dropdown.OptionData("Dev+")
                    };

                    UIElements.Add(key, dropdown);
                }

                // Base Rotate Mode
                if (key == "Base Rotate Mode")
                {
                    var bar = BarGenerator(singleInput, label, parent, "input [ENUM]", key);

                    var x = Instantiate(dropdownInput);
                    x.transform.SetParent(bar.transform);
                    x.transform.localScale = Vector3.one;

                    var xRT = x.transform.AsRT();
                    xRT.anchoredPosition = new Vector2(624f, -16f);
                    xRT.sizeDelta = new Vector2(366f, 32f);

                    Destroy(x.GetComponent<HoverTooltip>());

                    var dropdown = x.GetComponent<Dropdown>();
                    dropdown.options.Clear();
                    dropdown.onValueChanged.RemoveAllListeners();

                    dropdown.options = new List<Dropdown.OptionData>
                    {
                        new Dropdown.OptionData("Face Direction"),
                        new Dropdown.OptionData("None"),
                        new Dropdown.OptionData("Flip X"),
                        new Dropdown.OptionData("Flip Y")
                    };

                    UIElements.Add(key, dropdown);
                }

                // GUI Health Mode
                if (key == "GUI Health Mode")
                {
                    var bar = BarGenerator(singleInput, label, parent, "input [ENUM]", key);

                    var x = Instantiate(dropdownInput);
                    x.transform.SetParent(bar.transform);
                    x.transform.localScale = Vector3.one;

                    var xRT = x.transform.AsRT();
                    xRT.anchoredPosition = new Vector2(624f, -16f);
                    xRT.sizeDelta = new Vector2(366f, 32f);

                    Destroy(x.GetComponent<HoverTooltip>());

                    var dropdown = x.GetComponent<Dropdown>();
                    dropdown.options.Clear();
                    dropdown.onValueChanged.RemoveAllListeners();

                    dropdown.options = new List<Dropdown.OptionData>
                    {
                        new Dropdown.OptionData("Images"),
                        new Dropdown.OptionData("Text"),
                        new Dropdown.OptionData("Equals Bar"),
                        new Dropdown.OptionData("Bar"),
                    };

                    UIElements.Add(key, dropdown);
                }

                // Easing Dropdown
                if (key.Contains("Easing"))
                {
                    var bar = BarGenerator(singleInput, label, parent, "input [ENUM]", key);

                    var x = Instantiate(dropdownInput);
                    x.transform.SetParent(bar.transform);
                    x.transform.localScale = Vector3.one;

                    var xRT = x.transform.AsRT();
                    xRT.anchoredPosition = new Vector2(624f, -16f);
                    xRT.sizeDelta = new Vector2(366f, 32f);

                    Destroy(x.GetComponent<HoverTooltip>());

                    var dropdown = x.GetComponent<Dropdown>();
                    dropdown.options.Clear();
                    dropdown.onValueChanged.RemoveAllListeners();

                    dropdown.options = EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList();

                    UIElements.Add(key, dropdown);
                }

                // Color
                if (key.Contains("Color") && !key.Contains("Easing") && !key.Contains("Custom") && !key.Contains("Duration"))
                {
                    var bar = singleInput.Duplicate(parent, "input [COLOR]");
                    bar.transform.localScale = Vector3.one;
                    Destroy(bar.GetComponent<InputField>());
                    Destroy(bar.GetComponent<EventInfo>());
                    Destroy(bar.GetComponent<EventTrigger>());

                    UIElements.Add(key + " GameObject", bar);

                    LSHelpers.DeleteChildren(bar.transform);
                    bar.transform.AsRT().sizeDelta = new Vector2(750f, 116f);

                    TooltipHelper.AddHoverTooltip(bar, key, "");

                    var l = label.Duplicate(bar.transform, "label", 0);
                    l.transform.localScale = Vector3.one;
                    l.transform.GetChild(0).GetComponent<Text>().text = key;
                    l.transform.AsRT().sizeDelta = new Vector2(354f, 20f);

                    l.transform.GetChild(0).AsRT().anchoredPosition = new Vector2(10f, -5f);

                    var image = bar.GetComponent<Image>();
                    image.enabled = true;
                    image.color = new Color(1f, 1f, 1f, 0.03f);

                    var x = Instantiate(colorsInput);
                    x.transform.SetParent(bar.transform);
                    x.transform.localScale = Vector3.one;
                    Destroy(x.GetComponent<HoverTooltip>());

                    for (int i = 19; i < 26; i++)
                    {
                        var colorButton = Instantiate(x.transform.GetChild(0).gameObject);
                        colorButton.name = i.ToString();
                        colorButton.transform.SetParent(x.transform);
                        colorButton.transform.localScale = Vector3.one;
                    }

                    var list = new List<Toggle>();
                    for (int i = 0; i < x.transform.childCount; i++)
                    {
                        if (x.transform.GetChild(i).gameObject.TryGetComponent(out Toggle toggle))
                        {
                            list.Add(toggle);
                        }
                    }

                    UIElements.Add(key, list);
                }

                // Bool
                if (key.Contains("Active") || key.Contains("Emitting") || key == "Pulse Rotate to Head" || key == "Tail Base Grows" || key == "Base Collision Accurate" || key == "Bullet Constant" || key == "Bullet Hurt Players" || key == "Bullet AutoKill")
                {
                    var bar = singleInput.Duplicate(parent, "input [BOOL]");
                    bar.transform.localScale = Vector3.one;
                    Destroy(bar.GetComponent<InputField>());
                    Destroy(bar.GetComponent<EventInfo>());
                    Destroy(bar.GetComponent<EventTrigger>());

                    UIElements.Add(key + " GameObject", bar);

                    LSHelpers.DeleteChildren(bar.transform);

                    TooltipHelper.AddHoverTooltip(bar, key, "");

                    var l = label.Duplicate(bar.transform, "label", 0);
                    l.transform.localScale = Vector3.one;
                    l.transform.GetChild(0).GetComponent<Text>().text = key;
                    l.transform.AsRT().sizeDelta = new Vector2(688f, 20f);

                    l.transform.GetChild(0).AsRT().anchoredPosition = new Vector2(10f, -5f);

                    var image = bar.GetComponent<Image>();
                    image.enabled = true;
                    image.color = new Color(1f, 1f, 1f, 0.03f);

                    var x = Instantiate(boolInput);
                    x.transform.SetParent(bar.transform);
                    x.transform.localScale = Vector3.one;

                    var xt = x.GetComponent<Toggle>();
                    xt.onValueChanged.RemoveAllListeners();

                    UIElements.Add(key, xt);
                }

                // Button
                if (key == "Custom Objects")
                {
                    //var bar = Instantiate(singleInput);
                    //DestroyImmediate(bar.GetComponent<InputField>());
                    //DestroyImmediate(bar.GetComponent<InputFieldSwapper>());
                    //Destroy(bar.GetComponent<EventInfo>());
                    //Destroy(bar.GetComponent<EventTrigger>());

                    //UIElements.Add(key + " GameObject", bar);

                    //LSHelpers.DeleteChildren(bar.transform);
                    //bar.transform.SetParent(editorDialogContent.Find("Viewport/Content"));
                    //bar.transform.localScale = Vector3.one;
                    //bar.name = "input [BUTTON]";

                    //TooltipHelper.AddTooltip(bar, key, "");

                    //var l = label.Duplicate(bar.transform, "label", 0);
                    //l.transform.localScale = Vector3.one;
                    //l.transform.GetChild(0).GetComponent<Text>().text = key;
                    //l.transform.AsRT().sizeDelta = new Vector2(354f, 20f);

                    //l.transform.GetChild(0).AsRT().anchoredPosition = new Vector2(10f, -5f);

                    var bar = BarGenerator(singleInput, label, parent, "input [BUTTON]", key);
                    DestroyImmediate(bar.GetComponent<InputField>());
                    DestroyImmediate(bar.GetComponent<InputFieldSwapper>());

                    var image = bar.GetComponent<Image>();
                    image.enabled = true;
                    image.color = new Color(1f, 1f, 1f, 0.03f);

                    var btt = bar.AddComponent<Button>();
                    //btt.onClick.AddListener(delegate ()
                    //{
                    //    if (((Dictionary<string, object>)dict["Custom Objects"]).Count > 0)
                    //        currentID = ((Dictionary<string, object>)dict["Custom Objects"]).ElementAt(0).Key;

                    //    RenderCustomDialog(dict);
                    //    RenderCustomObjects(dict);
                    //});

                    UIElements.Add(key, btt);
                }
            }

            var b = BarGenerator(singleInput, label, parent, "input [NULL]", "Cannot edit default Player Models!");
            b.transform.Find("label").AsRT().sizeDelta = new Vector2(554f, 20f);

            TooltipHelper.AddHoverTooltip(b, "Default player models cannot be edited.", "");

            yield break;
        }

        public int VisibilityToInt(string vis)
        {
            switch (vis)
            {
                case "isBoosting": return 0;
                case "isTakingHit": return 1;
                case "isZenMode": return 2;
                case "isHealthPercentageGreater": return 3;
                case "isHealthGreaterEquals": return 4;
                case "isHealthEquals": return 5;
                case "isHealthGreater": return 6;
                case "isPressingKey": return 7;
                default: return 0;
            }
        }

        public string IntToVisibility(int val)
        {
            switch (val)
            {
                case 0: return "isBoosting";
                case 1: return "isTakingHit";
                case 2: return "isZenMode";
                case 3: return "isHealthPercentageGreater";
                case 4: return "isHealthGreaterEquals";
                case 5: return "isHealthEquals";
                case 6: return "isHealthGreater";
                case 7: return "isPressingKey";
                default: return "isBoosting";
            }
        }

        public void RenderCustomDialog(PlayerModel playerModel)
        {
            EditorManager.inst.ShowDialog("Player Object Editor");
            var content = objectDialogTF.Find("data/left/Object Scroll View/Viewport/Content");

            var customObjects = playerModel.customObjects;

            if (customObjects.Count > 0 && !string.IsNullOrEmpty(currentID) && customObjects.ContainsKey(currentID))
            {
                content.gameObject.SetActive(true);
                var currentObject = customObjects[currentID];

                var nameIF = content.Find("name/name").GetComponent<InputField>();
                nameIF.onValueChanged.ClearAll();
                nameIF.onEndEdit.RemoveAllListeners();
                nameIF.text = currentObject.name;
                nameIF.onValueChanged.AddListener(delegate (string _val)
                {
                    currentObject.name = _val;
                });
                nameIF.onEndEdit.AddListener(delegate (string _val)
                {
                    RenderCustomObjects(playerModel);
                });

                //Position
                {
                    var val = currentObject.position;
                    var x = content.Find("position/x").GetComponent<InputField>();
                    x.onValueChanged.ClearAll();
                    x.text = val.x.ToString();

                    var y = content.Find("position/y").GetComponent<InputField>();
                    y.onValueChanged.ClearAll();
                    y.text = val.y.ToString();

                    x.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (float.TryParse(_val, out float xv) && float.TryParse(y.text, out float yv))
                        {
                            currentObject.position = new Vector2(xv, yv);

                            UpdatePlayers();
                        }
                    });

                    y.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (float.TryParse(x.text, out float xv) && float.TryParse(_val, out float yv))
                        {
                            currentObject.position = new Vector2(xv, yv);

                            UpdatePlayers();
                        }
                    });

                    TriggerHelper.AddEventTrigger(x.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(x, multi: true), TriggerHelper.ScrollDeltaVector2(x, y, 0.1f, 10f) });
                    TriggerHelper.AddEventTrigger(y.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(y, multi: true), TriggerHelper.ScrollDeltaVector2(x, y, 0.1f, 10f) });

                    TriggerHelper.IncreaseDecreaseButtons(x, 1f, 10f);
                    TriggerHelper.IncreaseDecreaseButtons(y, 1f, 10f);
                }

                //Scale
                {
                    var val = currentObject.scale;
                    var x = content.Find("scale/x").GetComponent<InputField>();
                    x.onValueChanged.ClearAll();
                    x.text = val.x.ToString();

                    var y = content.Find("scale/y").GetComponent<InputField>();
                    y.onValueChanged.ClearAll();
                    y.text = val.y.ToString();

                    x.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (float.TryParse(_val, out float xv) && float.TryParse(y.text, out float yv))
                        {
                            currentObject.scale = new Vector2(xv, yv);

                            UpdatePlayers();
                        }
                    });

                    y.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (float.TryParse(x.text, out float xv) && float.TryParse(_val, out float yv))
                        {
                            currentObject.scale = new Vector2(xv, yv);

                            UpdatePlayers();
                        }
                    });

                    TriggerHelper.AddEventTrigger(x.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(x, multi: true), TriggerHelper.ScrollDeltaVector2(x, y, 0.1f, 10f) });
                    TriggerHelper.AddEventTrigger(y.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(y, multi: true), TriggerHelper.ScrollDeltaVector2(x, y, 0.1f, 10f) });

                    TriggerHelper.IncreaseDecreaseButtons(x, 1f, 10f);
                    TriggerHelper.IncreaseDecreaseButtons(y, 1f, 10f);
                }

                //Rotation
                {
                    var val = currentObject.rotation;
                    var x = content.Find("rotation/x").GetComponent<InputField>();
                    x.onValueChanged.ClearAll();
                    x.text = val.ToString();

                    x.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (float.TryParse(_val, out float rot))
                        {
                            currentObject.rotation = rot;

                            UpdatePlayers();
                        }
                    });

                    TriggerHelper.AddEventTrigger(x.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(x, 15f, 3f) });

                    TriggerHelper.IncreaseDecreaseButtons(x, 15f, 3f);
                }

                //Depth
                {
                    var val = currentObject.depth;
                    var x = content.Find("depth/x").GetComponent<InputField>();
                    x.onValueChanged.ClearAll();
                    x.text = val.ToString();

                    x.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (float.TryParse(_val, out float depth))
                        {
                            currentObject.depth = depth;

                            UpdatePlayers();
                        }
                    });

                    TriggerHelper.AddEventTrigger(x.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(x, 0.1f, 10f) });

                    TriggerHelper.IncreaseDecreaseButtons(x, 1f, 10f);
                }

                //Color
                {
                    var x = content.Find("color/color");

                    for (int i = 0; i < 26; i++)
                    {
                        var strr = (i + 1).ToString();
                        if (i < 4)
                        {
                            x.transform.Find(strr).GetComponent<Image>().color = GameManager.inst.LiveTheme.playerColors[i];
                        }
                        if (i == 4)
                        {
                            x.transform.Find(strr).GetComponent<Image>().color = GameManager.inst.LiveTheme.guiColor;
                        }
                        if (i > 4 && i < 23)
                        {
                            int colnum = i - 5;
                            x.transform.Find(strr).GetComponent<Image>().color = GameManager.inst.LiveTheme.objectColors[colnum];
                        }
                        if (i == 23)
                        {
                            x.transform.Find(strr).GetComponent<Image>().color = GameManager.inst.LiveTheme.playerColors[playerModelIndex];
                        }
                        if (i == 24)
                        {
                            x.transform.Find(strr).GetComponent<Image>().color = LSColors.HexToColor(currentObject.customColor);
                        }
                        if (i == 25)
                        {
                            x.transform.Find(strr).GetComponent<Image>().color = CoreHelper.CurrentBeatmapTheme.guiAccentColor;
                        }
                    }

                    UpdateCustomColorButtons(x, currentObject);
                }

                //Custom Color
                {
                    var x = content.Find("custom color/name").GetComponent<InputField>();
                    x.onValueChanged.ClearAll();
                    x.text = currentObject.customColor;
                    x.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (_val.Length == 6)
                        {
                            currentObject.customColor = _val;

                            UpdatePlayers();
                        }
                    });
                }

                //Opacity
                {
                    var val = currentObject.opacity;
                    var x = content.Find("opacity/x").GetComponent<InputField>();
                    x.onValueChanged.ClearAll();
                    x.text = val.ToString();

                    x.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (float.TryParse(_val, out float opacity))
                        {
                            currentObject.opacity = Mathf.Clamp(opacity, 0f, 1f);

                            UpdatePlayers();
                        }
                    });

                    TriggerHelper.AddEventTrigger(x.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(x, 0.1f, 10f, 0f, 1f) });

                    TriggerHelper.IncreaseDecreaseButtons(x, 1f, 10f, 0f, 1f);
                }

                //Parent
                {
                    var val = (int)currentObject.parent;
                    var x = content.Find("parent").GetComponent<Dropdown>();
                    x.onValueChanged.RemoveAllListeners();
                    x.value = val;
                    x.onValueChanged.AddListener(delegate (int _val)
                    {
                        currentObject.parent = _val;

                        UpdatePlayers();
                    });
                }

                //Parent Pos Offset
                {
                    var val = currentObject.positionOffset;
                    var x = content.Find("parent pos offset/x").GetComponent<InputField>();
                    x.onValueChanged.ClearAll();
                    x.text = val.ToString();

                    x.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            currentObject.positionOffset = Mathf.Clamp(num, 0.001f, 1f);

                            UpdatePlayers();
                        }
                    });

                    TriggerHelper.AddEventTrigger(x.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(x, 0.1f, 10f, 0.001f, 1f) });

                    TriggerHelper.IncreaseDecreaseButtons(x, 1f, 10f, 0.001f, 1f);
                }

                //Parent Sca Offset
                {
                    var val = currentObject.scaleOffset;
                    var x = content.Find("parent sca offset/x").GetComponent<InputField>();
                    x.onValueChanged.ClearAll();
                    x.text = val.ToString();

                    x.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            currentObject.scaleOffset = Mathf.Clamp(num, 0.001f, 1f);

                            UpdatePlayers();
                        }
                    });

                    TriggerHelper.AddEventTrigger(x.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(x, 0.1f, 10f, 0.001f, 1f) });

                    TriggerHelper.IncreaseDecreaseButtons(x, 1f, 10f, 0.001f, 1f);
                }

                //Parent Sca Active
                {
                    var val = (bool)currentObject.scaleParent;
                    var x = content.Find("parent sca offset/p/parent sca active").GetComponent<Toggle>();
                    x.onValueChanged.RemoveAllListeners();
                    x.isOn = val;
                    x.onValueChanged.AddListener(delegate (bool _val)
                    {
                        currentObject.scaleParent = _val;

                        UpdatePlayers();
                    });
                }

                //Parent Rot Offset
                {
                    var val = currentObject.rotationOffset;
                    var x = content.Find("parent rot offset/x").GetComponent<InputField>();
                    x.onValueChanged.ClearAll();
                    x.text = val.ToString();

                    x.onValueChanged.AddListener(delegate (string _val)
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            currentObject.rotationOffset = Mathf.Clamp(num, 0.001f, 1f);

                            UpdatePlayers();
                        }
                    });

                    TriggerHelper.AddEventTrigger(x.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(x, 0.1f, 10f, 0.001f, 1f) });

                    TriggerHelper.IncreaseDecreaseButtons(x, 1f, 10f, 0.001f, 1f);
                }

                //Parent Rot Active
                {
                    var val = (bool)currentObject.rotationParent;
                    var x = content.Find("parent rot offset/p/parent rot active").GetComponent<Toggle>();
                    x.onValueChanged.RemoveAllListeners();
                    x.isOn = val;
                    x.onValueChanged.AddListener(delegate (bool _val)
                    {
                        currentObject.rotationParent = _val;

                        UpdatePlayers();
                    });
                }

                //Shape
                {
                    var val = currentObject.shape;
                    //Shape
                    {
                        var shape = content.Find("shape/x").GetComponent<Dropdown>();
                        shape.options.Clear();
                        shape.onValueChanged.RemoveAllListeners();

                        shape.options = GetShapes();

                        shape.value = val.type;

                        var shapeOption = content.Find("shape option/x").GetComponent<Dropdown>();
                        shapeOption.options.Clear();
                        shapeOption.onValueChanged.RemoveAllListeners();

                        shapeOption.options = GetShapeOptions(val.type);

                        shapeOption.value = val.option;

                        shape.onValueChanged.AddListener(delegate (int _val)
                        {
                            currentObject.shape = ShapeManager.inst.Shapes2D[_val][0];

                            RenderCustomDialog(playerModel);

                            UpdatePlayers();
                        });

                        shapeOption.onValueChanged.AddListener(delegate (int _val)
                        {
                            currentObject.shape = ShapeManager.inst.Shapes2D[shape.value][_val];

                            RenderCustomDialog(playerModel);

                            UpdatePlayers();
                        });
                    }
                }

                var requireAll = content.Find("require all/toggle").GetComponent<Toggle>();
                requireAll.onValueChanged.ClearAll();
                requireAll.isOn = currentObject.requireAll;
                requireAll.onValueChanged.AddListener(delegate (bool _val)
                {
                    currentObject.requireAll = _val;
                });

                LSHelpers.DeleteChildren(visibilityContent);
                var add = PrefabEditor.inst.CreatePrefab.Duplicate(visibilityContent, "Add");
                add.transform.Find("Text").GetComponent<Text>().text = "Add Visiblity Setting";
                ((RectTransform)add.transform).sizeDelta = new Vector2(760f, 32f);
                var addButton = add.GetComponent<Button>();
                addButton.onClick.ClearAll();
                addButton.onClick.AddListener(delegate ()
                {
                    var newVisibility = new PlayerModel.CustomObject.Visiblity();
                    newVisibility.command = IntToVisibility(0);
                    currentObject.visibilitySettings.Add(newVisibility);
                    RenderCustomDialog(playerModel);
                });

                var label = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content").transform.GetChild(3).gameObject;
                var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");
                var boolInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle");
                var dropdownInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown");
                var close = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/x");
                int num = 0;
                foreach (var visibility in currentObject.visibilitySettings)
                {
                    int index = num;
                    var bar = Instantiate(singleInput);

                    Destroy(bar.GetComponent<EventInfo>());
                    Destroy(bar.GetComponent<EventTrigger>());
                    Destroy(bar.GetComponent<InputField>());
                    Destroy(bar.GetComponent<InputFieldSwapper>());

                    bar.GetComponent<HorizontalLayoutGroup>().spacing = 4;

                    LSHelpers.DeleteChildren(bar.transform);
                    bar.transform.SetParent(visibilityContent);
                    bar.transform.localScale = Vector3.one;
                    bar.name = "input [ENUM]";

                    //var l = Instantiate(label);
                    //l.transform.SetParent(bar.transform);
                    //l.transform.SetAsFirstSibling();
                    //l.transform.localScale = Vector3.one;
                    //l.transform.GetChild(0).GetComponent<Text>().text = visibility.command;
                    //l.transform.AsRT().sizeDelta = new Vector2(522f, 20f);

                    //l.transform.GetChild(0).AsRT().anchoredPosition = new Vector2(10f, -5f);

                    var image = bar.GetComponent<Image>();
                    image.enabled = true;
                    image.color = new Color(1f, 1f, 1f, 0.03f);

                    var buttonObject = Instantiate(boolInput);
                    buttonObject.transform.SetParent(bar.transform);
                    buttonObject.transform.localScale = Vector3.one;

                    var xt = buttonObject.GetComponent<Toggle>();
                    xt.onValueChanged.RemoveAllListeners();
                    xt.isOn = visibility.not;
                    xt.onValueChanged.AddListener(delegate (bool _val)
                    {
                        visibility.not = _val;
                    });

                    var x = Instantiate(dropdownInput);
                    x.transform.SetParent(bar.transform);
                    x.transform.localScale = Vector3.one;

                    Destroy(x.GetComponent<HoverTooltip>());

                    Destroy(x.GetComponent<HideDropdownOptions>());

                    var dropdown = x.GetComponent<Dropdown>();
                    dropdown.onValueChanged.RemoveAllListeners();
                    dropdown.options.Clear();
                    dropdown.options = new List<Dropdown.OptionData>
                    {
                        new Dropdown.OptionData("Is Boosting"),
                        new Dropdown.OptionData("Is Taking Hit"),
                        new Dropdown.OptionData("Is Zen Mode"),
                        new Dropdown.OptionData("Is Health Percentage Greater"),
                        new Dropdown.OptionData("Is Health Greater Equals"),
                        new Dropdown.OptionData("Is Health Equals"),
                        new Dropdown.OptionData("Is Health Greater"),
                        new Dropdown.OptionData("Is Pressing Key"),
                    };
                    dropdown.value = VisibilityToInt(visibility.command);
                    dropdown.onValueChanged.AddListener(delegate (int _val)
                    {
                        visibility.command = IntToVisibility(_val);
                    });

                    // Value
                    {
                        var valueObject = singleInput.Duplicate(bar.transform, "input [FLOAT]");

                        Destroy(valueObject.GetComponent<EventInfo>());
                        Destroy(valueObject.GetComponent<EventTrigger>());
                        Destroy(valueObject.GetComponent<InputField>());

                        valueObject.transform.localScale = Vector3.one;
                        valueObject.transform.GetChild(0).localScale = Vector3.one;
                        valueObject.transform.AsRT().sizeDelta = new Vector2(50f, 32f);

                        //var l = label.Duplicate(valueObject.transform, "", 0);
                        //l.transform.GetChild(0).GetComponent<Text>().text = setting.Key;
                        //l.transform.AsRT().sizeDelta = new Vector2(541f, 20f);

                        //l.transform.GetChild(0).AsRT().anchoredPosition = new Vector2(10f, -5f);

                        var image2 = valueObject.GetComponent<Image>();
                        image2.enabled = true;
                        image2.color = new Color(1f, 1f, 1f, 0.03f);

                        var input = valueObject.transform.Find("input");
                        input.AsRT().sizeDelta = new Vector2(50f, 32f);

                        var xif = input.gameObject.AddComponent<InputField>();
                        xif.onValueChanged.RemoveAllListeners();
                        xif.textComponent = input.Find("Text").GetComponent<Text>();
                        xif.placeholder = input.Find("Placeholder").GetComponent<Text>();
                        xif.characterValidation = InputField.CharacterValidation.Integer;
                        xif.text = visibility.value.ToString();
                        xif.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (float.TryParse(_val, out float result))
                            {
                                visibility.value = result;
                            }
                        });

                        TriggerHelper.AddEventTrigger(xif.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(xif) });

                        DestroyImmediate(valueObject.transform.Find("<").gameObject);
                        DestroyImmediate(valueObject.transform.Find(">").gameObject);
                    }

                    var delete = close.gameObject.Duplicate(bar.transform, "delete");

                    delete.transform.AsRT().anchoredPosition = new Vector2(-5f, 0f);

                    var deleteButton = delete.GetComponent<Button>();
                    deleteButton.onClick.ClearAll();
                    deleteButton.onClick.AddListener(delegate ()
                    {
                        currentObject.visibilitySettings.RemoveAt(index);
                        RenderCustomDialog(playerModel);
                    });
                    num++;
                }

                ////Visibility
                //{
                //    var val = (int)currentObject["Visibility"];
                //    var x = content.Find("visibility").GetComponent<Dropdown>();
                //    x.onValueChanged.RemoveAllListeners();
                //    x.value = val;
                //    x.onValueChanged.AddListener(delegate (int _val)
                //    {
                //        currentObject["Visibility"] = _val;
                //        currentObject["Visibility Value"] = 0f;

                //        RenderCustomDialog(_dictionary);
                //        UpdatePlayers();
                //    });
                //}

                ////Visibility Health Percentage
                //{
                //    var val = (float)currentObject["Visibility Value"];
                //    var x = content.Find("visibility health percentage/x").GetComponent<InputField>();
                //    x.onValueChanged.ClearAll();
                //    x.text = val.ToString();

                //    x.onValueChanged.AddListener(delegate (string _val)
                //    {
                //        var num = float.Parse(_val);

                //        currentObject["Visibility Value"] = num;

                //        UpdatePlayers();
                //    });

                //    TriggerHelper.AddEventTrigger(x.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDelta(x, 0.1f, 10f, 0f, 100f) });

                //    TriggerHelper.IncreaseDecreaseButtons(x, 1f, 10f, 0f, 100f);
                //}

                ////Visibility Not
                //{
                //    var val = (bool)currentObject["Visibility Not"];
                //    var x = content.Find("visibility health percentage/p/visibility not").GetComponent<Toggle>();
                //    x.onValueChanged.RemoveAllListeners();
                //    x.isOn = val;
                //    x.onValueChanged.AddListener(delegate (bool _val)
                //    {
                //        currentObject["Visibility Not"] = _val;

                //        UpdatePlayers();
                //    });
                //}
            }
            else
                content.gameObject.SetActive(false);
        }

        public void RenderCustomObjects(PlayerModel playerModel)
        {
            var create = objectDialogTF.Find("data/right/create").GetComponent<Button>();
            create.onClick.ClearAll();
            create.onClick.AddListener(delegate ()
            {
                CoreHelper.Log("Trying to create new object");

                var addObject = new PlayerModel.CustomObject(playerModel);
                addObject.id = LSText.randomNumString(16);
                playerModel.customObjects.Add(addObject.id, addObject);

                var customObjects = playerModel.customObjects;

                currentID = customObjects.ElementAt(customObjects.Count - 1).Key;

                UpdatePlayers();
                RenderCustomObjects(playerModel);
                RenderCustomDialog(playerModel);
            });

            var search = objectDialogTF.Find("data/right/search").GetComponent<InputField>();
            search.onValueChanged.ClearAll();
            search.text = customSearchTerm;
            search.onValueChanged.AddListener(delegate (string _val)
            {
                customSearchTerm = _val;
                RenderCustomObjects(playerModel);
            });

            LSHelpers.DeleteChildren(objectDialogContent);

            var close = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("Panel/x");

            var customObjects = playerModel.customObjects;
            foreach (var custom in customObjects)
            {
                var customObject = custom.Value;
                var name = customObject.name;
                if (string.IsNullOrEmpty(customSearchTerm) || name.ToLower().Contains(customSearchTerm.ToLower()))
                {
                    string tmpID = custom.Key;

                    var gameObject = Instantiate(customObjectButtonPrefab);
                    gameObject.transform.SetParent(objectDialogContent);
                    gameObject.transform.localScale = Vector3.one;
                    gameObject.transform.localRotation = Quaternion.identity;
                    gameObject.name = custom.Key;

                    gameObject.transform.Find("name").GetComponent<Text>().text = name;

                    var button = gameObject.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(delegate ()
                    {
                        currentID = tmpID;
                        RenderCustomDialog(playerModel);
                        RenderCustomObjects(playerModel);
                    });

                    var dot = gameObject.transform.Find("dot").GetComponent<Image>();
                    if (tmpID == currentID)
                        dot.enabled = true;
                    else
                        dot.enabled = false;

                    Destroy(gameObject.transform.Find("time").gameObject);

                    var dup = Instantiate(close.gameObject);
                    var dupTF = dup.transform;
                    dupTF.SetParent(gameObject.transform);
                    dupTF.localScale = Vector3.one;
                    dup.name = "duplicate";
                    dupTF.transform.localRotation = Quaternion.identity;

                    dupTF.transform.GetChild(0).localRotation = Quaternion.Euler(new Vector3(0f, 0f, 45f));

                    dup.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f);

                    TooltipHelper.AddHoverTooltip(dup, "Duplicate Object", "Creates a complete copy of this object.");

                    var dupButton = dup.GetComponent<Button>();
                    var cb = dupButton.colors;
                    cb.normalColor = new Color(0.3169f, 0.74f, 0.8918f, 1f);
                    dupButton.colors = cb;

                    dupButton.onClick.ClearAll();
                    dupButton.onClick.AddListener(delegate ()
                    {
                        DuplicateCustomObject();
                    });

                    var dupLE = dup.AddComponent<LayoutElement>();
                    dupLE.preferredWidth = 32f;

                    var delete = Instantiate(close.gameObject);
                    var deleteTF = delete.transform;
                    deleteTF.SetParent(gameObject.transform);
                    deleteTF.localScale = Vector3.one;
                    delete.name = "delete";
                    deleteTF.transform.localRotation = Quaternion.Euler(Vector3.zero);

                    delete.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f);

                    var deleteButton = delete.GetComponent<Button>();
                    deleteButton.onClick.ClearAll();
                    deleteButton.onClick.AddListener(delegate ()
                    {
                        playerModel.customObjects.Remove(tmpID);

                        var customObjects = playerModel.customObjects;

                        currentID = customObjects.Count > 0 ? customObjects.ElementAt(customObjects.Count - 1).Key : "";

                        UpdatePlayers();
                        RenderCustomObjects(playerModel);
                        RenderCustomDialog(playerModel);
                    });

                    var le = delete.AddComponent<LayoutElement>();
                    le.preferredWidth = 32f;
                }
            }
        }

        public void OpenDialog()
        {
            if (inst == null)
                return;

            CoreHelper.Log("Attempting to open Player Editor!");
            EditorManager.inst.ShowDialog("Player Editor");
            inst.StartCoroutine(RenderDialog());
        }

        public IEnumerator RenderDialog()
        {
            playerModelDropdown.options.Clear();

            var obj = PlayerManager.PlayerModels;
            var currentIndex = PlayerManager.GetPlayerModelIndex(playerModelIndex);
            var currentModel = PlayerManager.PlayerModels[currentIndex];

            int c = PlayerManager.GetPlayerModelInt(currentModel);

            playerModelDropdown.onValueChanged.RemoveAllListeners();
            playerModelDropdown.options = obj.Values.Select(x => new Dropdown.OptionData(x.basePart.name)).ToList();
            playerModelDropdown.value = c;
            playerModelDropdown.onValueChanged.AddListener(delegate (int _val)
            {
                CoreHelper.Log($"Setting Player {playerModelIndex}'s model index to {_val}");
                PlayerManager.SetPlayerModelIndex(playerModelIndex, _val);
                inst.StartCoroutine(RenderDialog());

                PlayerManager.RespawnPlayers();
            });

            playerModelIndexIF.onValueChanged.ClearAll();
            playerModelIndexIF.text = playerModelIndex.ToString();
            playerModelIndexIF.onValueChanged.AddListener(delegate (string _val)
            {
                if (int.TryParse(_val, out int num))
                {
                    num = Mathf.Clamp(num, 0, 3);
                    playerModelIndex = num;
                    inst.StartCoroutine(RenderDialog());
                }
            });

            TriggerHelper.AddEventTrigger(playerModelIndexIF.gameObject, new List<EventTrigger.Entry> { TriggerHelper.ScrollDeltaInt(playerModelIndexIF, 1, 0, 3) });
            TriggerHelper.IncreaseDecreaseButtonsInt(playerModelIndexIF, 1, 0, 3);

            #region UI Elements

            var label = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content").transform.GetChild(3).gameObject;
            var singleInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position/x");
            var vector2Input = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/EventObjectDialog/data/right/move/position");
            var boolInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle");
            var dropdownInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/autokill/tod-dropdown");
            var sliderFullInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/bpm");
            var stringInput = GameObject.Find("TimelineBar/GameObject/Time Input");
            var colorsInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/right/color/color");

            #endregion

            LSHelpers.SetActiveChildren(editorDialogContent.Find("Viewport/Content"), false);

            if (!PlayerModel.DefaultModels.Any(x => currentModel.basePart.id == x.basePart.id))
            {
                inst.GetElement<GameObject>("Cannot edit default Player Models! GameObject").SetActive(false);
                foreach (var key in PlayerModel.Values)
                {
                    if (key != "Base ID" && (string.IsNullOrEmpty(searchTerm) || key.ToLower().Contains(searchTerm.ToLower())))
                    {
                        inst.GetElement<GameObject>(key + " GameObject").SetActive(true);

                        if (inst.UIElements.ContainsKey(key + " Option GameObject"))
                            inst.GetElement<GameObject>(key + " Option GameObject").SetActive(true);

                        //String
                        if (key == "Base Name" || key.Contains("Color") && key.Contains("Custom"))
                        {
                            var xif = inst.GetElement<InputField>(key);
                            xif.onValueChanged.RemoveAllListeners();
                            xif.characterValidation = InputField.CharacterValidation.None;
                            xif.characterLimit = 0;
                            if (key.Contains("Color") && key.Contains("Custom"))
                                xif.characterLimit = 8;
                            xif.text = (string)currentModel[key];
                            xif.textComponent.fontSize = 18;
                            xif.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (!(key.Contains("Color") && key.Contains("Custom")))
                                    currentModel[key] = _val;
                                else
                                    currentModel[key] = _val.Length == 6 || _val.Length == 8 ? _val : LSColors.ColorToHex(LSColors.pink500);
                            });
                            //xif.onEndEdit.RemoveAllListeners();
                            //xif.onEndEdit.AddListener(delegate (string _val)
                            //{
                            //    inst.StartCoroutine(RenderDialog());
                            //});
                        }

                        //Integer
                        if (key == "Base Health" || key == "Boost Particles Amount")
                        {
                            var vxif = inst.GetElement<InputField>(key);
                            vxif.onValueChanged.RemoveAllListeners();
                            vxif.characterValidation = InputField.CharacterValidation.Integer;
                            vxif.text = ((int)currentModel[key]).ToString();

                            vxif.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (int.TryParse(_val, out int num))
                                {
                                    currentModel[key] = num;
                                    UpdatePlayers();
                                }
                            });
                        }

                        //Shape Dropdowns
                        if (key.Contains("Shape"))
                        {
                            // Shape
                            {
                                var dropdown = inst.GetElement<Dropdown>(key);
                                dropdown.onValueChanged.RemoveAllListeners();
                                dropdown.options.Clear();
                                dropdown.options = GetShapes();
                                dropdown.value = ((Shape)currentModel[key]).type;

                                dropdown.onValueChanged.AddListener(delegate (int _val)
                                {
                                    currentModel[key] = ShapeManager.inst.Shapes2D[_val][0];
                                    inst.StartCoroutine(RenderDialog());
                                    UpdatePlayers();
                                });
                            }

                            // Shape Option
                            {
                                var dropdown = inst.GetElement<Dropdown>(key + " Option");
                                dropdown.onValueChanged.RemoveAllListeners();
                                dropdown.options.Clear();
                                dropdown.options = GetShapeOptions(((Shape)currentModel[key]).type);
                                dropdown.value = ((Shape)currentModel[key]).option;

                                var v = ((Shape)currentModel[key]).type;

                                dropdown.onValueChanged.AddListener(delegate (int _val)
                                {
                                    currentModel[key] = ShapeManager.inst.Shapes2D[v][_val];
                                    UpdatePlayers();
                                });
                            }
                        }

                        // Vector2
                        if (key.Contains("Position") && !key.Contains("Easing") && !key.Contains("Duration") || key.Contains("Scale") && !key.Contains("Particles") && !key.Contains("Easing") && !key.Contains("Duration") || key.Contains("Force") || key.Contains("Origin"))
                        {
                            var vtmp = (Vector2)currentModel[key];

                            var vxif = inst.GetElement<InputField>(key + " X");
                            vxif.onValueChanged.RemoveAllListeners();
                            vxif.text = vtmp.x.ToString();
                            vxif.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (float.TryParse(_val, out float result))
                                {
                                    currentModel[key] = new Vector2(result, vtmp.y);
                                    UpdatePlayers();
                                }
                            });

                            var vyif = inst.GetElement<InputField>(key + " Y");
                            vyif.onValueChanged.RemoveAllListeners();
                            vyif.text = vtmp.y.ToString();
                            vyif.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (float.TryParse(_val, out float result))
                                {
                                    currentModel[key] = new Vector2(vtmp.x, result);
                                    UpdatePlayers();
                                }
                            });
                        }

                        //Single
                        if (key.Contains("Scale") && (key.Contains("Start") || key.Contains("End")) && !key.Contains("Pulse") && !key.Contains("Bullet") ||
                            key.Contains("Rotation") && !key.Contains("Easing") ||
                            key == "Tail Base Distance" ||
                            key.Contains("Opacity") && !key.Contains("Easing") ||
                            key.Contains("Lifetime") ||
                            key.Contains("Start Width") ||
                            key.Contains("End Width") ||
                            key.Contains("Trail Time") ||
                            key == "Boost Particles Duration" ||
                            key.Contains("Amount") && !key.Contains("Boost") ||
                            key.Contains("Speed") || key.Contains("Depth") || key == "Pulse Duration" ||
                            key.Contains("Cooldown") || key.Contains("Boost Time") || key == "Bullet Lifetime" || key.Contains("Duration") ||
                            key == "Tail Base Time")
                        {
                            var vxif = inst.GetElement<InputField>(key);
                            vxif.onValueChanged.RemoveAllListeners();
                            vxif.text = ((float)currentModel[key]).ToString();
                            vxif.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (float.TryParse(_val, out float result))
                                {
                                    currentModel[key] = result;
                                    UpdatePlayers();
                                }
                            });
                        }

                        //Tail Base Mode Dropdown
                        if (key == "Tail Base Mode")
                        {
                            var dropdown = inst.GetElement<Dropdown>(key);
                            dropdown.onValueChanged.RemoveAllListeners();
                            dropdown.value = (int)currentModel[key];
                            dropdown.onValueChanged.AddListener(delegate (int _val)
                            {
                                currentModel[key] = _val;
                                UpdatePlayers();
                            });
                        }

                        //Base Rotate Mode
                        if (key == "Base Rotate Mode")
                        {
                            var dropdown = inst.GetElement<Dropdown>(key);
                            dropdown.onValueChanged.RemoveAllListeners();
                            dropdown.value = (int)currentModel[key];
                            dropdown.onValueChanged.AddListener(delegate (int _val)
                            {
                                currentModel[key] = _val;
                                UpdatePlayers();
                            });
                        }

                        //GUI Health Mode
                        if (key == "GUI Health Mode")
                        {
                            var dropdown = inst.GetElement<Dropdown>(key);
                            dropdown.onValueChanged.RemoveAllListeners();
                            dropdown.value = (int)currentModel[key];
                            dropdown.onValueChanged.AddListener(delegate (int _val)
                            {
                                currentModel[key] = _val;
                                UpdatePlayers();
                            });
                        }

                        //Easing Dropdown
                        if (key.Contains("Easing"))
                        {
                            var dropdown = inst.GetElement<Dropdown>(key);
                            dropdown.onValueChanged.RemoveAllListeners();

                            dropdown.value = (int)currentModel[key];

                            dropdown.onValueChanged.AddListener(delegate (int _val)
                            {
                                currentModel[key] = _val;
                                UpdatePlayers();
                            });
                        }

                        //Color
                        if (key.Contains("Color") && !key.Contains("Easing") && !key.Contains("Custom") && !key.Contains("Duration"))
                        {
                            var list = inst.GetElement<List<Toggle>>(key);

                            for (int i = 0; i < 25; i++)
                            {
                                if (i < 4)
                                {
                                    list[i].image.color = CoreHelper.CurrentBeatmapTheme.playerColors[i];
                                }
                                if (i == 4)
                                {
                                    list[i].image.color = CoreHelper.CurrentBeatmapTheme.guiColor;
                                }
                                if (i > 4 && i < 23)
                                {
                                    int num = i - 5;
                                    list[i].image.color = CoreHelper.CurrentBeatmapTheme.objectColors[num];
                                }
                                if (i == 23)
                                {
                                    list[i].image.color = CoreHelper.CurrentBeatmapTheme.playerColors[playerModelIndex];
                                    TooltipHelper.AddHoverTooltip(list[i].gameObject, "Current Player Color", "This represents the color the player would normally always use. For example: Player One uses color 1, Player Two uses color 2, etc.");
                                }
                                if (i == 24)
                                {
                                    Color col = Color.white;

                                    if (PlayerModel.Values.TryFind(x => x == key.Replace("Color", "Custom Color"), out string item))
                                    {
                                        col = LSColors.HexToColor((string)currentModel[item]);
                                    }

                                    list[i].image.color = col;
                                    TooltipHelper.AddHoverTooltip(list[i].gameObject, "Custom Color", "Uses a custom hex code set by you. Do remember this will break levels that use themes heavily such as fade black/white themes.");
                                }
                                if (i == 25)
                                {
                                    list[i].image.color = CoreHelper.CurrentBeatmapTheme.guiAccentColor;
                                }
                            }

                            UpdateColorButtons(list, currentModel, key);
                        }

                        //Bool
                        if (key.Contains("Active") || key.Contains("Emitting") || key == "Pulse Rotate to Head" || key == "Tail Base Grows" || key == "Base Collision Accurate" || key == "Bullet Constant" || key == "Bullet Hurt Players" || key == "Bullet AutoKill")
                        {
                            var xt = inst.GetElement<Toggle>(key);
                            xt.onValueChanged.RemoveAllListeners();
                            xt.isOn = (bool)currentModel[key];
                            xt.onValueChanged.AddListener(delegate (bool _val)
                            {
                                currentModel[key] = _val;
                                UpdatePlayers();
                            });
                        }

                        //Button
                        if (key == "Custom Objects")
                        {
                            var btt = inst.GetElement<Button>(key);
                            btt.onClick.ClearAll();
                            btt.onClick.AddListener(delegate ()
                            {
                                if (currentModel.customObjects.Count > 0)
                                    currentID = currentModel.customObjects.ElementAt(0).Key;

                                RenderCustomDialog(currentModel);
                                RenderCustomObjects(currentModel);
                            });
                        }
                    }
                    else if (key != "Base ID")
                    {
                        inst.GetElement<GameObject>(key + " GameObject").SetActive(false);
                    }
                }
            }
            else
            {
                inst.GetElement<GameObject>("Cannot edit default Player Models! GameObject").SetActive(true);
            }

            yield break;
        }

        public void UpdatePlayers()
        {
            PlayerManager.UpdatePlayers();
        }

        public void UpdateColorButtons(List<Toggle> toggles, PlayerModel currentModel, string key)
        {
            for (int i = 0; i < toggles.Count; i++)
            {
                int index = i;
                toggles[i].onValueChanged.ClearAll();
                toggles[i].isOn = index == (int)currentModel[key];
                toggles[i].onValueChanged.AddListener(delegate (bool _val)
                {
                    if (_val)
                    {
                        currentModel[key] = index;
                        UpdateColorButtons(toggles, currentModel, key);
                    }
                });
            }
        }

        public void UpdateCustomColorButtons(Transform _tf, PlayerModel.CustomObject customObject)
        {
            for (int i = 0; i < _tf.childCount; i++)
            {
                var toggle = _tf.GetChild(i).GetComponent<Toggle>();

                int index = i;

                toggle.onValueChanged.ClearAll();
                toggle.isOn = index == customObject.color;
                toggle.onValueChanged.AddListener(delegate (bool _val)
                {
                    customObject.color = index;
                    UpdatePlayers();

                    UpdateCustomColorButtons(_tf, customObject);
                });
            }
        }

        public void DuplicateCustomObject()
        {
            if (string.IsNullOrEmpty(currentID) || EditorManager.inst.ActiveDialogs.Find(x => x.Dialog.name == "PlayerObjectEditorDialog") == null)
                return;

            var currentModel = PlayerManager.PlayerModels[PlayerManager.GetPlayerModelIndex(playerModelIndex)];

            var customObject = PlayerModel.CustomObject.DeepCopy(currentModel, currentModel.customObjects[currentID]);

            currentModel.customObjects.Add(customObject.id, customObject);

            var obj = PlayerManager.PlayerModels;

            var customObjects = currentModel.customObjects;

            currentID = customObjects.Count > 0 ? customObjects.ElementAt(customObjects.Count - 1).Key : "";

            UpdatePlayers();
            RenderCustomObjects(currentModel);
            RenderCustomDialog(currentModel);
        }

        public static List<Dropdown.OptionData> GetShapes()
        {
            var list = new List<Dropdown.OptionData>
            {
                new Dropdown.OptionData("Square"),
                new Dropdown.OptionData("Circle"),
                new Dropdown.OptionData("Triangle"),
                new Dropdown.OptionData("Arrow"),
                new Dropdown.OptionData("Text (Cannot use)"),
                new Dropdown.OptionData("Hexagon"),
                new Dropdown.OptionData("Image (Cannot use)"),
                new Dropdown.OptionData("Pentagon"),
                new Dropdown.OptionData("Misc")
            };

            return list;
        }

        public static List<Dropdown.OptionData> GetShapeOptions(int _shape)
        {
            var list = new List<Dropdown.OptionData>();

            switch (_shape)
            {
                case 0:
                    {
                        list.Add(new Dropdown.OptionData("Square"));
                        list.Add(new Dropdown.OptionData("Square Outline"));
                        list.Add(new Dropdown.OptionData("Square Outline Thin"));
                        list.Add(new Dropdown.OptionData("Diamond"));
                        list.Add(new Dropdown.OptionData("Diamond Outline"));
                        list.Add(new Dropdown.OptionData("Diamond Outline Thin"));
                        break;
                    }
                case 1:
                    {
                        list.Add(new Dropdown.OptionData("Circle"));
                        list.Add(new Dropdown.OptionData("Circle Outline"));
                        list.Add(new Dropdown.OptionData("Semi-Circle"));
                        list.Add(new Dropdown.OptionData("Semi-Circle Outline"));
                        list.Add(new Dropdown.OptionData("Circle Outline Thin"));
                        list.Add(new Dropdown.OptionData("Quarter Circle"));
                        list.Add(new Dropdown.OptionData("Quarter Circle Outline"));
                        list.Add(new Dropdown.OptionData("Eighth Circle"));
                        list.Add(new Dropdown.OptionData("Eighth Circle Outline"));
                        list.Add(new Dropdown.OptionData("Circle Outline Thinner"));
                        list.Add(new Dropdown.OptionData("Semi-Circle Outline Thin"));
                        list.Add(new Dropdown.OptionData("Semi-Circle Outline Thinner"));
                        list.Add(new Dropdown.OptionData("Quarter Circle Outline Thin"));
                        list.Add(new Dropdown.OptionData("Quarter Circle Outline Thinner"));
                        list.Add(new Dropdown.OptionData("Eighth Circle Outline Thin"));
                        list.Add(new Dropdown.OptionData("Eighth Circle Outline Thinner"));
                        break;
                    }
                case 2:
                    {
                        list.Add(new Dropdown.OptionData("Triangle"));
                        list.Add(new Dropdown.OptionData("Triangle Outline"));
                        list.Add(new Dropdown.OptionData("Right Triangle"));
                        list.Add(new Dropdown.OptionData("Right Triangle Outline"));
                        list.Add(new Dropdown.OptionData("Triangle Outline Thin"));
                        break;
                    }
                case 3:
                    {
                        list.Add(new Dropdown.OptionData("Full Arrow"));
                        list.Add(new Dropdown.OptionData("Top Arrow"));
                        list.Add(new Dropdown.OptionData("Chevron Arrow"));
                        break;
                    }
                case 4:
                    {
                        list.Add(new Dropdown.OptionData("Text (Not usable)"));
                        break;
                    }
                case 5:
                    {
                        list.Add(new Dropdown.OptionData("Hexagon"));
                        list.Add(new Dropdown.OptionData("Hexagon Outline"));
                        list.Add(new Dropdown.OptionData("Hexagon Outline Thin"));
                        list.Add(new Dropdown.OptionData("Half Hexagon"));
                        list.Add(new Dropdown.OptionData("Half Hexagon Outline"));
                        list.Add(new Dropdown.OptionData("Half Hexagon Outline Thin"));
                        break;
                    }
                case 6:
                    {
                        list.Add(new Dropdown.OptionData("Image (Not usable)"));
                        break;
                    }
                case 7:
                    {
                        list.Add(new Dropdown.OptionData("Pentagon"));
                        list.Add(new Dropdown.OptionData("Pentagon Outline"));
                        list.Add(new Dropdown.OptionData("Pentagon Outline Thin"));
                        list.Add(new Dropdown.OptionData("Half Pentagon"));
                        list.Add(new Dropdown.OptionData("Half Pentagon Outline"));
                        list.Add(new Dropdown.OptionData("Half Pentagon Outline Thin"));
                        break;
                    }
                case 8:
                    {
                        list.Add(new Dropdown.OptionData("PA Logo Top"));
                        list.Add(new Dropdown.OptionData("PA Logo Bottom"));
                        list.Add(new Dropdown.OptionData("PA Logo"));
                        list.Add(new Dropdown.OptionData("Star"));
                        list.Add(new Dropdown.OptionData("Moon"));
                        list.Add(new Dropdown.OptionData("Moon Thin"));
                        list.Add(new Dropdown.OptionData("Half Moon"));
                        list.Add(new Dropdown.OptionData("Half Moon Thin"));
                        list.Add(new Dropdown.OptionData("Quarter Moon"));
                        list.Add(new Dropdown.OptionData("Quarter Moon Thin"));
                        list.Add(new Dropdown.OptionData("System Error Wing"));
                        list.Add(new Dropdown.OptionData("Heart"));
                        list.Add(new Dropdown.OptionData("Heart Outline"));
                        list.Add(new Dropdown.OptionData("Heart Outline Thin"));
                        list.Add(new Dropdown.OptionData("Half Heart"));
                        list.Add(new Dropdown.OptionData("Half Heart Outline"));
                        list.Add(new Dropdown.OptionData("Half Heart Outline Thin"));
                        break;
                    }
            }

            return list;
        }
    }
}
