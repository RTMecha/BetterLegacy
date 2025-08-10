using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Arcade.Interfaces;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data.Dialogs;

namespace BetterLegacy.Editor.Managers
{
    public class UploadedLevelsManager : MonoBehaviour
    {
        public static UploadedLevelsManager inst;

        public static void Init() => Creator.NewGameObject(nameof(UploadedLevelsManager), EditorManager.inst.transform.parent).AddComponent<UploadedLevelsManager>();

        void Awake()
        {
            inst = this;
            try
            {
                Dialog = new UploadedLevelsDialog();
                Dialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

		int levelCount;

		bool loadingOnlineLevels;

        public UploadedLevelsDialog Dialog { get; set; }

        public int page;
        public int sort;
        public bool ascend;
        static string SearchURL => $"{AlephNetwork.ArcadeServerURL}api/level/uploaded";

        public static Dictionary<string, Sprite> OnlineLevelIcons { get; set; } = new Dictionary<string, Sprite>();

        public void Search() => CoroutineHelper.StartCoroutine(GetLevels());

		IEnumerator GetLevels()
        {
			if (loadingOnlineLevels)
				yield break;

			loadingOnlineLevels = true;

            Dialog.ClearContent();

            var page = this.page;
            int currentPage = page + 1;

            var search = Dialog.SearchTerm;

            string query = AlephNetwork.BuildQuery(SearchURL, search, page, sort, ascend);

            CoreHelper.Log($"Search query: {query}");

            var headers = new Dictionary<string, string>();
			if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null)
				headers["Authorization"] = $"Bearer {LegacyPlugin.authData["access_token"].Value}";

			yield return CoroutineHelper.StartCoroutine(AlephNetwork.DownloadJSONFile(query, json =>
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

                            var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(Dialog.Content, $"Folder [{name}]");
                            var folderButtonStorage = gameObject.GetComponent<FunctionButtonStorage>();
                            var folderButtonFunction = gameObject.AddComponent<FolderButtonFunction>();

                            folderButtonStorage.label.text =
                                $"<b>Level Name</b>: {LSText.ClampString(name, 42)}\n" +
                                $"<b>Song Title</b>: {LSText.ClampString(title, 42)}\n" +
                                $"<b>Song Artist</b>: {LSText.ClampString(artist, 42)}";
                            RectValues.FullAnchored.AnchorMin(0.15f, 0f).SizeDelta(-32f, -8f).AssignToRectTransform(folderButtonStorage.label.rectTransform);

                            gameObject.transform.AsRT().sizeDelta = new Vector2(0f, 132f);

                            //folderButtonStorage.text.horizontalOverflow = horizontalOverflow;
                            //folderButtonStorage.text.verticalOverflow = verticalOverflow;
                            //folderButtonStorage.text.fontSize = fontSize;

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
                            EditorThemeManager.ApplyLightText(folderButtonStorage.label);

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
                                CoroutineHelper.StartCoroutine(AlephNetwork.DownloadBytes($"{ArcadeMenu.CoverURL}{id}{FileFormat.JPG.Dot()}", bytes =>
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
                                CoroutineHelper.StartCoroutine(RTMetaDataEditor.inst.RefreshTokens(Search));
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

        public void DownloadLevel(JSONNode jn, Action onDownload = null)
        {
            var name = jn["name"].Value;
            EditorManager.inst.DisplayNotification($"Downloading {name}, please wait...", 3f, EditorManager.NotificationType.Success);
            name = RTString.ReplaceFormatting(name); // for cases where a user has used symbols not allowed.
            name = RTFile.ValidateDirectory(name);
            var directory = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath, $"{name} [{jn["id"].Value}]");
            DownloadLevel(jn["id"], directory, name, onDownload);
        }

        public void DownloadLevel(string id, string directory, string name, Action onDownload = null)
        {
            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadBytes($"{ArcadeMenu.DownloadURL}{id}.zip", bytes =>
            {
                RTFile.DeleteDirectory(directory);
                Directory.CreateDirectory(directory);

                File.WriteAllBytes($"{directory}{FileFormat.ZIP.Dot()}", bytes);

                ZipFile.ExtractToDirectory($"{directory}{FileFormat.ZIP.Dot()}", directory);

                File.Delete($"{directory}{FileFormat.ZIP.Dot()}");

                EditorLevelManager.inst.LoadLevels();
                EditorManager.inst.DisplayNotification($"Downloaded {name}!", 1.5f, EditorManager.NotificationType.Success);

                onDownload?.Invoke();
            }, onError =>
            {
                EditorManager.inst.DisplayNotification($"Failed to download {name}.", 1.5f, EditorManager.NotificationType.Error);
                CoreHelper.LogError($"OnError: {onError}");
            }));
        }
	}
}
