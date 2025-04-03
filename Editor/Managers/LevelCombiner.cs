using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;

namespace BetterLegacy.Editor.Managers
{
    public class LevelCombiner : MonoBehaviour
    {
        public static LevelCombiner inst;

        #region UI Objects

        public EditorDialog Dialog { get; set; }

        public static GameObject editorDialogObject;
        public static Transform editorDialogTransform;
        public static Transform editorDialogContent;
        public static Transform editorDialogText;

        public static InputField searchField;
        public static string searchTerm;

        public static InputField saveField;

        #endregion

        #region Variables

        public static EditorManager.MetadataWrapper first;
        public static EditorManager.MetadataWrapper second;
        public static string savePath = "Combined Level";

        #endregion

        public static void Init() => Creator.NewGameObject(nameof(LevelCombiner), EditorManager.inst.transform.parent).AddComponent<LevelCombiner>();

        void Awake()
        {
            if (inst == null)
                inst = this;
            else if (inst != this)
                Destroy(gameObject);

            editorDialogObject = EditorPrefabHolder.Instance.Dialog.Duplicate(EditorManager.inst.dialogs, "LevelCombinerDialog");
            editorDialogTransform = editorDialogObject.transform;
            editorDialogTransform.AsRT().anchoredPosition = new Vector2(0f, 16f);
            editorDialogTransform.AsRT().sizeDelta = new Vector2(0f, 32f);
            var dialogStorage = editorDialogObject.GetComponent<EditorDialogStorage>();

            dialogStorage.topPanel.color = LSColors.HexToColor("E57373");
            dialogStorage.title.text = "- Level Combiner -";

            editorDialogTransform.GetChild(1).AsRT().sizeDelta = new Vector2(765f, 12f);

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

            var search = RTEditor.inst.OpenLevelPopup.GameObject.transform.Find("search-box").gameObject.Duplicate(editorDialogTransform, "search");

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

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(editorDialogTransform, "Scroll View");

            editorDialogContent = scrollView.transform.Find("Viewport/Content");

            editorDialogContent.GetComponent<VerticalLayoutGroup>().spacing = 4f;

            scrollView.transform.AsRT().sizeDelta = new Vector2(765f, 320f);

            EditorHelper.AddEditorDialog(EditorDialog.LEVEL_COMBINER, editorDialogObject);

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

                var save = RTEditor.inst.OpenLevelPopup.GameObject.transform.Find("search-box").gameObject.Duplicate(editorDialogTransform, "save");

                saveField = save.transform.GetChild(0).GetComponent<InputField>();
                UIManager.SetRectTransform(saveField.image.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(700f, 32f));
                saveField.onValueChanged.ClearAll();
                saveField.characterLimit = 0;
                saveField.text = savePath;
                saveField.onValueChanged.AddListener(_val => savePath = _val);

                EditorThemeManager.AddInputField(saveField);

                ((Text)saveField.placeholder).text = "Set a path...";

                // Dropdown
                {
                    var dropdownBase = Creator.NewUIObject("save type", editorDialogTransform);
                    dropdownBase.transform.AsRT().sizeDelta = new Vector2(765f, 64f);

                    var dropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(dropdownBase.transform, "dropdown").GetComponent<Dropdown>();
                    RectValues.Default.SizeDelta(400f, 32f).AssignToRectTransform(dropdown.transform.AsRT());
                    dropdown.onValueChanged.ClearAll();
                    dropdown.options = CoreHelper.StringToOptionData("LS", "VG");
                    dropdown.value = 0;
                    dropdown.onValueChanged.AddListener(_val => EditorConfig.Instance.CombinerOutputFormat.Value = (ArrhythmiaType)(_val + 1));
                    EditorThemeManager.AddDropdown(dropdown);
                }

                //Button 1
                {
                    var buttonBase = Creator.NewUIObject("combine", editorDialogTransform);
                    buttonBase.transform.AsRT().anchoredPosition = new Vector2(436f, 55f);
                    buttonBase.transform.AsRT().sizeDelta = new Vector2(100f, 50f);

                    var button = EditorPrefabHolder.Instance.Function2Button.Duplicate(buttonBase.transform, "combine");
                    button.transform.localScale = Vector3.one;

                    var buttonStorage = button.GetComponent<FunctionButtonStorage>();

                    UIManager.SetRectTransform(button.transform.AsRT(), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400f, 50f));

                    buttonStorage.label.text = "Combine & Save";
                    buttonStorage.button.onClick.ClearAll();
                    buttonStorage.button.onClick.AddListener(Combine);

                    EditorThemeManager.AddSelectable(buttonStorage.button, ThemeGroup.Function_2);
                    EditorThemeManager.AddGraphic(buttonStorage.label, ThemeGroup.Function_2_Text);
                }
            }

            // Dropdown
            var levelCombinerDropdown = EditorHelper.AddEditorDropdown("Level Combiner", "", "File", SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_combine_t.png"), OpenDialog, 4);
            EditorHelper.SetComplexity(levelCombinerDropdown, Complexity.Normal);

            EditorThemeManager.AddGraphic(editorDialogObject.GetComponent<Image>(), ThemeGroup.Background_1);
            EditorThemeManager.AddLightText(infoText);

            try
            {
                Dialog = new EditorDialog(EditorDialog.LEVEL_COMBINER);
                Dialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        public void OpenDialog()
        {
            Dialog.Open();
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

        /// <summary>
        /// Combines all selected editor levels into one.
        /// </summary>
        public void Combine() => Combine(savePath);

        /// <summary>
        /// Combines all selected editor levels into one.
        /// </summary>
        /// <param name="savePath">Path to save the level to.</param>
        public void Combine(string savePath) => Combine(savePath, RTEditor.inst.LevelPanels.Where(x => x.combinerSelected && x.Level && RTFile.FileExists(x.Level.GetFile(x.Level.CurrentFile))));

        /// <summary>
        /// Combines editor levels into one.
        /// </summary>
        /// <param name="selected">Editor levels to combine.</param>
        public void Combine(IEnumerable<LevelPanel> selected) => Combine(savePath, selected);

        /// <summary>
        /// Combines editor levels into one.
        /// </summary>
        /// <param name="savePath">Path to save the level to.</param>
        /// <param name="selected">Editor levels to combine.</param>
        public void Combine(string savePath, IEnumerable<LevelPanel> selected)
        {
            var combineList = new List<GameData>();

            foreach (var editorWrapper in selected)
            {
                Debug.Log($"{EditorManager.inst.className}Parsing GameData from {editorWrapper.Level.FolderName}");
                combineList.Add(editorWrapper.Level.LoadGameData());
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

            var levelFile = EditorConfig.Instance.CombinerOutputFormat.Value switch
            {
                ArrhythmiaType.LS => Level.LEVEL_LSB,
                ArrhythmiaType.VG => Level.LEVEL_VGD,
                _ => "",
            };

            string save = savePath;
            if (!save.Contains(levelFile) && save.LastIndexOf('/') == save.Length - 1)
                save += levelFile;
            else if (!save.Contains("/" + levelFile))
                save += "/" + levelFile;

            if (!save.Contains(RTEditor.inst.BeatmapsPath) && !save.Contains(RTEditor.inst.EditorPath))
                save = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath, save);
            else if (!save.Contains(RTEditor.inst.BeatmapsPath))
                save = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, save);
            else if (!save.Contains(RTEditor.inst.BasePath))
                save = RTFile.CombinePaths(RTEditor.inst.BasePath, save);

            foreach (var levelPanel in selected)
            {
                var file = levelPanel.Level.GetFile(levelPanel.Level.CurrentFile);
                if (!RTFile.FileExists(file))
                    return;

                var directory = Path.GetDirectoryName(save);
                RTFile.CreateDirectory(directory);

                var files1 = Directory.GetFiles(Path.GetDirectoryName(file));

                foreach (var file2 in files1)
                {
                    string dir = Path.GetDirectoryName(file2);
                    RTFile.CreateDirectory(dir);

                    var copyTo = file2.Replace(Path.GetDirectoryName(file), directory);
                    if (EditorConfig.Instance.CombinerOutputFormat.Value == ArrhythmiaType.VG)
                        copyTo = copyTo
                            .Replace(Level.LEVEL_OGG, Level.AUDIO_OGG)
                            .Replace(Level.LEVEL_WAV, Level.AUDIO_WAV)
                            .Replace(Level.LEVEL_MP3, Level.AUDIO_MP3)
                            .Replace(Level.LEVEL_JPG, Level.COVER_JPG)
                            ;

                    var fileName = Path.GetFileName(file2);
                    if (fileName != Level.LEVEL_LSB && fileName != Level.LEVEL_VGD && fileName != Level.METADATA_LSB && fileName != Level.METADATA_VGM && !RTFile.FileExists(copyTo))
                        File.Copy(file2, copyTo);
                }
            }

            if (EditorConfig.Instance.CombinerOutputFormat.Value == ArrhythmiaType.LS)
            {
                selected.First().Level.metadata?.WriteToFile(save.Replace(Level.LEVEL_LSB, Level.METADATA_LSB));

                combinedGameData.SaveData(save, () =>
                {
                    EditorManager.inst.DisplayNotification($"Combined {RTString.ArrayToString(selected.Select(x => x.Name).ToArray())} to {savePath} in the LS format!", 3f, EditorManager.NotificationType.Success);
                }, true);
            }
            else
            {
                selected.First().Level.metadata?.WriteToFileVG(save.Replace(Level.LEVEL_VGD, Level.METADATA_VGM).Replace(Level.LEVEL_LSB, Level.METADATA_VGM));

                combinedGameData.SaveDataVG(save.Replace(FileFormat.LSB.Dot(), FileFormat.VGD.Dot()), () =>
                {
                    EditorManager.inst.DisplayNotification($"Combined {RTString.ArrayToString(selected.Select(x => x.Name).ToArray())} to {savePath} in the VG format!", 3f, EditorManager.NotificationType.Success);
                });
            }
        }
    }
}
