using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;
using LSFunctions;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Managers
{
    public class LevelCombiner : MonoBehaviour
    {
        public static LevelCombiner inst;

        #region UI Objects

        public static GameObject editorDialogObject;
        public static Transform editorDialogTransform;
        public static Transform editorDialogTitle;
        public static Transform editorDialogSpacer;
        public static Transform editorDialogContent;
        public static Transform editorDialogText;

        public static InputField searchField;
        public static string searchTerm;

        public static InputField saveField;

        #endregion

        #region Variables

        public static EditorManager.MetadataWrapper first;
        public static EditorManager.MetadataWrapper second;
        public static string savePath;

        #endregion

        public static void Init() => Creator.NewGameObject(nameof(LevelCombiner), EditorManager.inst.transform.parent).AddComponent<LevelCombiner>();

        void Awake()
        {
            if (inst == null)
                inst = this;
            else if (inst != this)
                Destroy(gameObject);

            editorDialogObject = EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog.gameObject.Duplicate(EditorManager.inst.dialogs, "LevelCombinerDialog");

            editorDialogTransform = editorDialogObject.transform;
            editorDialogObject.layer = 5;
            editorDialogTransform.localScale = Vector3.one;
            editorDialogTransform.position = new Vector3(1537.5f, 714.945f, 0f) * EditorManager.inst.ScreenScale;
            editorDialogObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 32f);

            editorDialogTitle = editorDialogTransform.GetChild(0);
            editorDialogTitle.GetComponent<Image>().color = LSColors.HexToColor("E57373");
            editorDialogTitle.GetChild(0).GetComponent<Text>().text = "- Level Combiner -";

            editorDialogSpacer = editorDialogTransform.GetChild(1);
            editorDialogSpacer.GetComponent<RectTransform>().sizeDelta = new Vector2(765f, 12f);

            editorDialogText = editorDialogTransform.GetChild(2);

            editorDialogText.SetSiblingIndex(1);

            var infoText = editorDialogText.GetComponent<Text>();
            infoText.text = "To combine levels into one, select multiple levels from the list below, set a save path and click save.";

            // Label
            {
                var label1 = editorDialogText.gameObject.Duplicate(editorDialogTransform, "label");
                label1.transform.localScale = Vector3.one;

                label1.transform.AsRT().sizeDelta = new Vector2(765f, 32f);
                var labelText = label1.GetComponent<Text>();
                labelText.text = "Select levels to combine";

                EditorThemeManager.AddLightText(labelText);
            }

            var search = Instantiate(EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("search-box").gameObject);
            search.transform.SetParent(editorDialogTransform);
            search.transform.localScale = Vector3.one;
            search.name = "search";

            searchField = search.transform.GetChild(0).GetComponent<InputField>();

            searchField.onValueChanged.ClearAll();
            searchField.text = "";
            searchField.onValueChanged.AddListener(_val =>
            {
                searchTerm = _val;
                StartCoroutine(RenderDialog());
            });

            EditorThemeManager.AddInputField(searchField, ThemeGroup.Search_Field_1, 1, SpriteHelper.RoundedSide.Bottom);

            search.transform.GetChild(0).Find("Placeholder").GetComponent<Text>().text = "Search for level...";

            var scrollView = Instantiate(GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View"));
            scrollView.transform.SetParent(editorDialogTransform);
            scrollView.transform.localScale = Vector3.one;
            scrollView.name = "Scroll View";

            editorDialogContent = scrollView.transform.Find("Viewport/Content");

            LSHelpers.DeleteChildren(editorDialogContent);

            editorDialogContent.GetComponent<VerticalLayoutGroup>().spacing = 4f;


            scrollView.transform.AsRT().sizeDelta = new Vector2(765f, 392f);

            EditorHelper.AddEditorDialog("Level Combiner", editorDialogObject);

            // Save
            {
                // Label
                {
                    var label1 = editorDialogText.gameObject.Duplicate(editorDialogTransform, "label");
                    label1.transform.localScale = Vector3.one;

                    label1.transform.AsRT().sizeDelta = new Vector2(765f, 32f);
                    var labelText = label1.GetComponent<Text>();
                    labelText.text = "Save path";

                    EditorThemeManager.AddLightText(labelText);
                }

                var save = Instantiate(EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("search-box").gameObject);
                save.transform.SetParent(editorDialogTransform);
                save.transform.localScale = Vector3.one;
                save.name = "search";

                saveField = save.transform.GetChild(0).GetComponent<InputField>();
                UIManager.SetRectTransform(saveField.image.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(700f, 32f));
                saveField.onValueChanged.ClearAll();
                saveField.characterLimit = 0;
                saveField.text = RTFile.ApplicationDirectory + RTEditor.editorListSlash + "Combined Level/level.lsb";
                savePath = RTFile.ApplicationDirectory + RTEditor.editorListSlash + "Combined Level/level.lsb";
                saveField.onValueChanged.AddListener(_val => { savePath = _val; });

                EditorThemeManager.AddInputField(saveField);

                ((Text)saveField.placeholder).text = "Set a path...";

                //Button 1
                {
                    var buttonBase = new GameObject("combine");
                    buttonBase.transform.SetParent(editorDialogTransform);
                    buttonBase.transform.localScale = Vector3.one;

                    var buttonBaseRT = buttonBase.AddComponent<RectTransform>();
                    buttonBaseRT.anchoredPosition = new Vector2(436f, 55f);
                    buttonBaseRT.sizeDelta = new Vector2(100f, 50f);

                    var button = EditorPrefabHolder.Instance.Function2Button.Duplicate(buttonBase.transform, "combine");
                    button.transform.localScale = Vector3.one;

                    var buttonStorage = button.GetComponent<FunctionButtonStorage>();

                    UIManager.SetRectTransform(button.transform.AsRT(), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400f, 50f));

                    buttonStorage.text.text = "Combine & Save";
                    buttonStorage.button.onClick.ClearAll();
                    buttonStorage.button.onClick.AddListener(Combine);

                    EditorThemeManager.AddSelectable(buttonStorage.button, ThemeGroup.Function_2);
                    EditorThemeManager.AddGraphic(buttonStorage.text, ThemeGroup.Function_2_Text);
                }
            }

            // Dropdown
            var levelCombinerDropdown = EditorHelper.AddEditorDropdown("Level Combiner", "", "File", SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_combine_t.png"), OpenDialog, 4);
            EditorHelper.SetComplexity(levelCombinerDropdown, Complexity.Normal);

            EditorThemeManager.AddGraphic(editorDialogObject.GetComponent<Image>(), ThemeGroup.Background_1);
            EditorThemeManager.AddLightText(infoText);
        }

        public void OpenDialog()
        {
            EditorManager.inst.ShowDialog("Level Combiner");
            StartCoroutine(RenderDialog());
        }

        public IEnumerator RenderDialog()
        {
            LSHelpers.DeleteChildren(editorDialogContent);

            foreach (var editorWrapper in RTEditor.inst.LevelPanels)
                editorWrapper.InitLevelCombiner();

            yield break;
        }

        public GameData combinedGameData;

        public void Combine()
        {
            var combineList = new List<GameData>();
            var list = new List<string>();
            var paths = new List<string>();
            var selected = RTEditor.inst.LevelPanels.Where(x => x.combinerSelected);

            foreach (var editorWrapper in selected)
            {
                var levelPath = RTFile.CombinePaths(editorWrapper.FolderPath, Level.LEVEL_LSB);
                if (!RTFile.FileExists(levelPath))
                    continue;

                Debug.Log($"{EditorManager.inst.className}Parsing GameData from {Path.GetFileName(editorWrapper.FolderPath)}");
                paths.Add(levelPath);
                list.Add(editorWrapper.Name);
                combineList.Add(GameData.Parse(SimpleJSON.JSON.Parse(RTFile.ReadFromFile(levelPath))));
            }

            Debug.Log($"{EditorManager.inst.className}Can Combine: {combineList.Count > 0 && !string.IsNullOrEmpty(savePath)}" +
                $"\nGameData Count: {combineList.Count}" +
                $"\nSavePath: {savePath}");

            if (combineList.Count < 2)
            {
                EditorManager.inst.DisplayNotification("More than one level needs to be selected.", 1f, EditorManager.NotificationType.Error);
                return;
            }

            if (string.IsNullOrEmpty(savePath))
            {
                EditorManager.inst.DisplayNotification("Cannot combine with an empty path!", 1f, EditorManager.NotificationType.Error);
                return;
            }

            var combinedGameData = GameData.Combiner.Combine(combineList.ToArray());
            this.combinedGameData = combinedGameData;

            string save = savePath;
            if (!save.Contains(Level.LEVEL_LSB) && save.LastIndexOf('/') == save.Length - 1)
                save += Level.LEVEL_LSB;
            else if (!save.Contains("/" + Level.LEVEL_LSB))
                save += "/" + Level.LEVEL_LSB;

            if (!save.Contains(RTFile.ApplicationDirectory) && !save.Contains(RTEditor.editorListSlash))
                save = RTFile.ApplicationDirectory + RTEditor.editorListSlash + save;
            else if (!save.Contains(RTFile.ApplicationDirectory))
                save = RTFile.ApplicationDirectory + save;

            foreach (var file in paths)
            {
                if (!RTFile.FileExists(file))
                    return;

                var directory = Path.GetDirectoryName(save);
                RTFile.CreateDirectory(directory);

                var files1 = Directory.GetFiles(Path.GetDirectoryName(file));

                foreach (var file2 in files1)
                {
                    string dir = Path.GetDirectoryName(file2);
                    RTFile.CreateDirectory(dir);

                    if (Path.GetFileName(file2) != Level.LEVEL_LSB && !RTFile.FileExists(file2.Replace(Path.GetDirectoryName(file), directory)))
                        File.Copy(file2, file2.Replace(Path.GetDirectoryName(file), directory));
                }
            }

            if (EditorConfig.Instance.CombinerOutputFormat.Value == ArrhythmiaType.LS)
                combinedGameData.SaveData(save, () =>
                {
                    EditorManager.inst.DisplayNotification($"Combined {RTString.ArrayToString(list.ToArray())} to {savePath}!", 3f, EditorManager.NotificationType.Success);
                }, true);
            else
                combinedGameData.SaveDataVG(save.Replace(FileFormat.LSB.Dot(), FileFormat.VGD.Dot()), () =>
                {
                    EditorManager.inst.DisplayNotification($"Combined {RTString.ArrayToString(list.ToArray())} to {savePath}!", 3f, EditorManager.NotificationType.Success);
                });
        }
    }
}
