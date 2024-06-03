using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BetterLegacy.Core;
using LSFunctions;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// The new Player editor.
    /// </summary>
    public class PlayerEditor : MonoBehaviour
    {
        public static PlayerEditor inst;

        public static Dropdown playerModelDropdown;
        public static InputField playerModelIndexIF;
        public static int playerModelIndex = 0;

        public static void Init() => Creator.NewGameObject("PlayerEditor", EditorManager.inst.transform.parent).AddComponent<PlayerEditor>();

        void Awake()
        {
            inst = this;
            StartCoroutine(GenerateUI());
        }

        public enum Tab
        {
            Global,
            Base, // includes stretch
            GUI,
            Boost,
            Pulse,
            Bullet,
            Tail, // All tail related parts go here
            Custom
        }

        public static Tab ParseTab(string str)
        {
            return
                str.Contains("Base") && !str.Contains("GUI") && !str.Contains("Tail") || str.Contains("Stretch") ? Tab.Base :
                str.Contains("GUI") ? Tab.GUI :
                str.Contains("Boost") && !str.Contains("Tail") ? Tab.Boost :
                str.Contains("Pulse") ? Tab.Pulse :
                str.Contains("Bullet") ? Tab.Bullet :
                str.Contains("Tail") ? Tab.Tail : Tab.Custom;
        }

        public IEnumerator GenerateUI()
        {
            var dialog = EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog.gameObject.Duplicate(EditorManager.inst.dialogs, "PlayerEditorDialog");

            dialog.transform.localScale = Vector3.one;
            dialog.transform.position = new Vector3(1537.5f, 714.945f, 0f) * EditorManager.inst.ScreenScale;
            dialog.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

            var dialogTitle = dialog.transform.GetChild(0);
            dialogTitle.GetComponent<Image>().color = LSColors.HexToColor("E57373");
            dialogTitle.GetChild(0).GetComponent<Text>().text = "- Player Editor -";

            dialog.transform.GetChild(1).AsRT().sizeDelta = new Vector2(765f, 54f);

            Destroy(dialog.transform.Find("Text").gameObject);

            var search = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("search-box").gameObject.Duplicate(dialog.transform, "search");

            var searchField = search.transform.GetChild(0).GetComponent<InputField>();

            searchField.onValueChanged.ClearAll();
            searchField.text = "";
            searchField.onValueChanged.AddListener(delegate (string _val)
            {
                searchTerm = _val;
                StartCoroutine(Refresh());
            });

            ((Text)searchField.placeholder).text = "Search for value...";

            var scrollView = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View").Duplicate(dialog.transform, "Scroll View");

            scrollView.transform.AsRT().sizeDelta = new Vector2(765f, 512f);

            EditorHelper.AddEditorDialog("Player Editor New", dialog);
            EditorHelper.AddEditorDropdown("Player Editor New", "", "Edit", SpriteManager.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_player.png"), delegate ()
            {
                EditorManager.inst.ShowDialog("Player Editor New");
                StartCoroutine(Refresh());
            });

            var content = scrollView.transform.Find("Viewport/Content");

            var labelPrefab = EditorManager.inst.folderButtonPrefab.transform.GetChild(0).gameObject;

            LSHelpers.DeleteChildren(content);

            for (int i = 0; i < PlayerModel.Values.Count; i++)
            {
                var name = PlayerModel.Values[i];
                var gameObject = Creator.NewUIObject(name, content);
                gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

                var label = labelPrefab.Duplicate(gameObject.transform, "label");
                var labelText = label.GetComponent<Text>();
                labelText.text = name;
                UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

                RTEditor.EditorProperty.ValueType valueType = RTEditor.EditorProperty.ValueType.Function;
                if (name == "Base Name" || name.Contains("Color") && name.Contains("Custom"))
                {
                    valueType = RTEditor.EditorProperty.ValueType.String;

                    var input = EditorPrefabHolder.Instance.NumberInputField.transform.Find("input").gameObject.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(260f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(200f, 32f));
                }

                editorUIs.Add(new PlayerEditorUI
                {
                    Name = name,
                    GameObject = gameObject,
                    Tab = ParseTab(name),
                    ValueType = valueType,
                });
            }

            yield break;
        }

        public IEnumerator Refresh()
        {
            var models = PlayerManager.PlayerModels;
            var currentIndex = PlayerManager.GetPlayerModelIndex(playerModelIndex);
            var currentModel = PlayerManager.PlayerModels[currentIndex];

            for (int i = 0; i < editorUIs.Count; i++)
            {
                var ui = editorUIs[i];
                var active = CoreHelper.SearchString(searchTerm, ui.Name) && ui.Tab == CurrentTab;
                ui.GameObject?.SetActive(active);
                if (!active)
                    continue;

                try
                {
                    var value = PlayerManager.PlayerModels[PlayerManager.PlayerModelsIndex[0]][i];
                    var key = PlayerModel.Values[i];

                    switch (ui.ValueType)
                    {
                        case RTEditor.EditorProperty.ValueType.String:
                            {
                                var inputField = ui.GameObject.transform.Find("input").GetComponent<InputField>();
                                inputField.onValueChanged.ClearAll();
                                inputField.text = value.ToString();
                                inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (!(key.Contains("Color") && key.Contains("Custom")))
                                        currentModel[key] = _val;
                                    else
                                        currentModel[key] = _val.Length == 6 || _val.Length == 8 ? _val : LSColors.ColorToHex(LSColors.pink500);
                                });

                                break;
                            }
                    }
                }
                catch (Exception ex)
                {
                    CoreHelper.LogError($"Exception: {ex}");
                }
            }

            yield break;
        }

        public void Save()
        {

        }

        public void Load()
        {

        }

        public Tab CurrentTab { get; set; }
        public string searchTerm;
        public List<PlayerEditorUI> editorUIs = new List<PlayerEditorUI>();

        public class PlayerEditorUI
        {
            public string Name { get; set; }
            public GameObject GameObject { get; set; }
            public Tab Tab { get; set; }
            public RTEditor.EditorProperty.ValueType ValueType { get; set; }
        }
    }
}
