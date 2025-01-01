using BetterLegacy.Arcade.Interfaces;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Managers
{
    public class UploadedLevelsManager : MonoBehaviour
    {
        public static UploadedLevelsManager inst;

        public Transform content;
        InputField pageField;

        public static void Init() => Creator.NewGameObject(nameof(UploadedLevelsManager), EditorManager.inst.transform.parent).AddComponent<UploadedLevelsManager>();

        void Awake()
        {
            inst = this;
            StartCoroutine(GenerateUI());
        }

        IEnumerator GenerateUI()
        {
            var editorDialogObject = EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog.gameObject.Duplicate(EditorManager.inst.dialogs);
            editorDialogObject.name = "UploadedDialog";
            editorDialogObject.transform.position = new Vector3(1537.5f, 714.945f, 0f) * EditorManager.inst.ScreenScale;
            editorDialogObject.transform.localScale = Vector3.one;
            editorDialogObject.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

            var editorDialogTitle = editorDialogObject.transform.GetChild(0);
            editorDialogTitle.GetComponent<Image>().color = LSColors.HexToColor("F05355");
            var title = editorDialogTitle.GetChild(0).GetComponent<Text>();
            title.text = "- Levels -";

            var editorDialogSpacer = editorDialogObject.transform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            Destroy(editorDialogObject.transform.GetChild(2).gameObject);

            EditorHelper.AddEditorDialog("Uploaded Dialog", editorDialogObject);

            var search = EditorPrefabHolder.Instance.StringInputField.Duplicate(editorDialogObject.transform.Find("spacer"), "search");
            RectValues.Default.AnchoredPosition(-200f, 0f).SizeDelta(300f, 32f).AssignToRectTransform(search.transform.AsRT());
            var searchField = search.GetComponent<InputField>();
            searchField.onValueChanged.ClearAll();
            searchField.text = "";
            searchField.GetPlaceholderText().text = "Search levels...";
            searchField.onValueChanged.AddListener(_val => { this.search = _val; });

            var page = EditorPrefabHolder.Instance.NumberInputField.Duplicate(editorDialogObject.transform.Find("spacer"), "page");
            RectValues.Default.AnchoredPosition(-40f, 0f).SizeDelta(0f, 32f).AssignToRectTransform(page.transform.AsRT());
            var pageStorage = page.GetComponent<InputFieldStorage>();
            pageField = pageStorage.inputField;

            pageStorage.inputField.onValueChanged.ClearAll();
            pageStorage.inputField.text = this.page.ToString();
            pageStorage.inputField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int p))
                    this.page = Mathf.Clamp(p, 0, int.MaxValue);
            });

            pageStorage.leftGreaterButton.onClick.ClearAll();
            pageStorage.leftGreaterButton.onClick.AddListener(() =>
            {
                if (int.TryParse(pageStorage.inputField.text, out int p))
                    pageStorage.inputField.text = Mathf.Clamp(p - 10, 0, int.MaxValue).ToString();
            });

            pageStorage.leftButton.onClick.ClearAll();
            pageStorage.leftButton.onClick.AddListener(() =>
            {
                if (int.TryParse(pageStorage.inputField.text, out int p))
                    pageStorage.inputField.text = Mathf.Clamp(p - 1, 0, int.MaxValue).ToString();
            });

            pageStorage.rightButton.onClick.ClearAll();
            pageStorage.rightButton.onClick.AddListener(() =>
            {
                if (int.TryParse(pageStorage.inputField.text, out int p))
                    pageStorage.inputField.text = Mathf.Clamp(p + 1, 0, int.MaxValue).ToString();
            });

            pageStorage.rightGreaterButton.onClick.ClearAll();
            pageStorage.rightGreaterButton.onClick.AddListener(() =>
            {
                if (int.TryParse(pageStorage.inputField.text, out int p))
                    pageStorage.inputField.text = Mathf.Clamp(p + 10, 0, int.MaxValue).ToString();
            });

            Destroy(pageStorage.middleButton.gameObject);

            var searchButton = EditorPrefabHolder.Instance.Function2Button.Duplicate(editorDialogObject.transform.Find("spacer"), "search button");
            RectValues.Default.AnchoredPosition(310f, 0f).SizeDelta(100f, 32f).AssignToRectTransform(searchButton.transform.AsRT());
            var searchButtonStorage = searchButton.GetComponent<FunctionButtonStorage>();
            searchButtonStorage.text.text = "Search";
            searchButtonStorage.button.onClick.ClearAll();
            searchButtonStorage.button.onClick.AddListener(Search);

            EditorThemeManager.AddInputField(searchField);
            EditorThemeManager.AddInputField(pageStorage.inputField);
            EditorThemeManager.AddSelectable(pageStorage.leftGreaterButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(pageStorage.leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(pageStorage.rightButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(pageStorage.rightGreaterButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(searchButtonStorage.button, ThemeGroup.Function_2);

            var scrollView = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View").Duplicate(editorDialogObject.transform, "Scroll View");
            content = scrollView.transform.Find("Viewport/Content");
            scrollView.transform.localScale = Vector3.one;

            LSHelpers.DeleteChildren(content);

            var scrollViewLE = scrollView.AddComponent<LayoutElement>();
            scrollViewLE.ignoreLayout = true;

            scrollView.transform.AsRT().anchoredPosition = new Vector2(392.5f, 320f);
            scrollView.transform.AsRT().sizeDelta = new Vector2(735f, 638f);

            EditorThemeManager.AddGraphic(editorDialogObject.GetComponent<Image>(), ThemeGroup.Background_1);

            EditorHelper.AddEditorDropdown("View Uploaded", "", "Steam", SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_levels.png"), () =>
            {
                EditorManager.inst.ShowDialog("Uploaded Dialog");
                Search();
            });

            yield break;
        }

		int levelCount;

		bool loadingOnlineLevels;

        int page;
        string search;
        static string SearchURL => $"{AlephNetwork.ARCADE_SERVER_URL}api/level/uploads";

        public static Dictionary<string, Sprite> OnlineLevelIcons { get; set; } = new Dictionary<string, Sprite>();

        public void Search() => CoreHelper.StartCoroutine(GetLevels());

		IEnumerator GetLevels()
        {
			if (loadingOnlineLevels)
				yield break;

			loadingOnlineLevels = true;

            LSHelpers.DeleteChildren(content);

            var page = this.page;
            int currentPage = page + 1;

            var search = this.search;

            string query =
                string.IsNullOrEmpty(search) && page == 0 ? SearchURL :
                    string.IsNullOrEmpty(search) && page != 0 ? $"{SearchURL}?page={page}" :
                        !string.IsNullOrEmpty(search) && page == 0 ? $"{SearchURL}?query={AlephNetwork.ReplaceSpace(search)}" :
                            !string.IsNullOrEmpty(search) ? $"{SearchURL}?query={AlephNetwork.ReplaceSpace(search)}&page={page}" : "";

            CoreHelper.Log($"Search query: {query}");

            var headers = new Dictionary<string, string>();
			if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null)
				headers["Authorization"] = $"Bearer {LegacyPlugin.authData["access_token"].Value}";

			yield return CoreHelper.StartCoroutine(AlephNetwork.DownloadJSONFile($"{AlephNetwork.ARCADE_SERVER_URL}api/level/uploads", json =>
			{
				try
				{
					var jn = JSON.Parse(json);

					if (jn["items"] != null)
					{
						for (int i = 0; i < jn["items"].Count; i++)
                        {
							var item = jn["items"][i];

                            string id = item["id"];

                            string artist = item["artist"];
                            string title = item["title"];
                            string name = item["name"];
                            string creator = item["creator"];
                            string description = item["description"];
                            var difficulty = item["difficulty"].AsInt;

                            if (id == null || id == "0")
                                continue;

                            var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(content, $"Folder [{name}]");
                            var folderButtonStorage = gameObject.GetComponent<FunctionButtonStorage>();
                            var folderButtonFunction = gameObject.AddComponent<FolderButtonFunction>();

                            folderButtonStorage.text.text =
                                $"<b>Level Name</b>: {LSText.ClampString(name, 42)}\n" +
                                $"<b>Song Title</b>: {LSText.ClampString(title, 42)}\n" +
                                $"<b>Song Artist</b>: {LSText.ClampString(artist, 42)}";
                            RectValues.FullAnchored.AnchorMin(0.15f, 0f).SizeDelta(-32f, -8f).AssignToRectTransform(folderButtonStorage.text.rectTransform);

                            gameObject.transform.AsRT().sizeDelta = new Vector2(0f, 132f);

                            //folderButtonStorage.text.horizontalOverflow = horizontalOverflow;
                            //folderButtonStorage.text.verticalOverflow = verticalOverflow;
                            //folderButtonStorage.text.fontSize = fontSize;

                            var difficultyColor = difficulty >= 0 && difficulty < DataManager.inst.difficulties.Count ?
                                DataManager.inst.difficulties[difficulty].color : LSColors.themeColors["none"].color;

                            folderButtonStorage.button.onClick.ClearAll();
                            folderButtonFunction.onClick = eventData =>
                            {
                                RTEditor.inst.ShowWarningPopup("Are you sure you want to download this level to your editor folder?", () =>
                                {
                                    RTEditor.inst.HideWarningPopup();
                                    DownloadLevel(item);
                                }, RTEditor.inst.HideWarningPopup);
                            };

                            EditorThemeManager.ApplySelectable(folderButtonStorage.button, ThemeGroup.List_Button_1);
                            EditorThemeManager.ApplyLightText(folderButtonStorage.text);

                            var iconBase = Creator.NewUIObject("icon base", gameObject.transform);
                            var iconBaseImage = iconBase.AddComponent<Image>();
                            iconBase.AddComponent<Mask>().showMaskGraphic = false;
                            iconBase.transform.AsRT().anchoredPosition = new Vector2(-300f, 0f);
                            iconBase.transform.AsRT().sizeDelta = new Vector2(90f, 90f);
                            EditorThemeManager.ApplyGraphic(iconBaseImage, ThemeGroup.Null, true);

                            var icon = Creator.NewUIObject("icon", iconBase.transform);
                            var iconImage = icon.AddComponent<Image>();

                            icon.transform.AsRT().anchoredPosition = Vector3.zero;
                            icon.transform.AsRT().sizeDelta = new Vector2(90f, 90f);

                            if (OnlineLevelIcons.TryGetValue(id, out Sprite sprite))
                                iconImage.sprite = sprite;
                            else
                            {
                                CoreHelper.StartCoroutine(AlephNetwork.DownloadBytes($"{ArcadeMenu.CoverURL}{id}{FileFormat.JPG.Dot()}", bytes =>
                                {
                                    var sprite = SpriteHelper.LoadSprite(bytes);
                                    OnlineLevelIcons.Add(id, sprite);
                                    if (iconImage)
                                        iconImage.sprite = sprite;
                                }, onError =>
                                {
                                    var sprite = SteamWorkshop.inst.defaultSteamImageSprite;
                                    OnlineLevelIcons.Add(id, sprite);
                                    if (iconImage)
                                        iconImage.sprite = sprite;
                                }));
                            }

                        }
                    }

					if (jn["count"] != null)
						levelCount = jn["count"].AsInt;
				}
				catch (Exception ex)
				{
					CoreHelper.LogException(ex);
				}
			}, (string onError, long responseCode, string errorMsg) =>
            {
                switch (responseCode)
                {
                    case 404:
                        EditorManager.inst.DisplayNotification("404 not found.", 2f, EditorManager.NotificationType.Error);
                        return;
                    case 401:
                        {
                            if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null && LegacyPlugin.authData["refresh_token"] != null)
                            {
                                CoreHelper.StartCoroutine(RTMetaDataEditor.inst.RefreshTokens(Search));
                                return;
                            }
                            RTMetaDataEditor.inst.ShowLoginPopup(Search);
                            break;
                        }
                    default:
                        EditorManager.inst.DisplayNotification($"Level search failed. Error code: {onError}", 2f, EditorManager.NotificationType.Error);
                        break;
                }

                if (errorMsg != null)
                    CoreHelper.LogError($"Error Message: {errorMsg}");
            }, headers));

			loadingOnlineLevels = false;
		}

        public void DownloadLevel(JSONNode jn)
        {
            var name = jn["name"].Value;
            EditorManager.inst.DisplayNotification($"Downloading {name}, please wait...", 3f, EditorManager.NotificationType.Success);
            name = RTString.ReplaceFormatting(name); // for cases where a user has used symbols not allowed.
            name = RTFile.ValidateDirectory(name);
            var directory = $"{RTFile.ApplicationDirectory}{RTEditor.editorListSlash}{name} [{jn["id"].Value}]";

            CoreHelper.StartCoroutine(AlephNetwork.DownloadBytes($"{ArcadeMenu.DownloadURL}{jn["id"].Value}.zip", bytes =>
            {
                if (RTFile.DirectoryExists(directory))
                    Directory.Delete(directory, true);

                Directory.CreateDirectory(directory);

                File.WriteAllBytes($"{directory}.zip", bytes);

                ZipFile.ExtractToDirectory($"{directory}.zip", directory);

                File.Delete($"{directory}.zip");

                CoreHelper.StartCoroutine(RTEditor.inst.LoadLevels());
                EditorManager.inst.DisplayNotification($"Downloaded {name}!", 1.5f, EditorManager.NotificationType.Success);
            }, onError =>
            {
                EditorManager.inst.DisplayNotification($"Failed to download {name}.", 1.5f, EditorManager.NotificationType.Error);
                CoreHelper.LogError($"OnError: {onError}");
            }));
        }
	}
}
