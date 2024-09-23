﻿using BetterLegacy.Components;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Core.Prefabs;
using LSFunctions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Class used to browse files while still in the editor, allowing users to select files outside of the game directory.
    /// </summary>
    public class RTFileBrowser : MonoBehaviour
    {
        public static RTFileBrowser inst;

        void Awake()
        {
            inst = this;
            title = transform.Find("Panel/Text").GetComponent<TextMeshProUGUI>();

            try
            {
                transform.Find("content").gameObject.AddComponent<Button>();
                contentContextMenu = transform.Find("content").gameObject.AddComponent<ContextClickable>();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        ContextClickable contentContextMenu;

        public void UpdateBrowser(string _folder, string[] fileExtensions, Action<string> onSelectFile = null)
        {
            contentContextMenu.onClick = null;
            if (!RTFile.DirectoryExists(_folder))
            {
                EditorManager.inst.DisplayNotification("Folder doesn't exist.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            contentContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                RTEditor.inst.ShowContextMenu(300f,
                    new RTEditor.ButtonFunction("Create folder", () =>
                    {
                        RTEditor.inst.ShowFolderCreator(_folder, () => { UpdateBrowser(_folder, fileExtensions, onSelectFile); });
                    }));
            };

            title.text = $"<b>File Browser</b> ({FontManager.TextTranslater.ArrayToString(fileExtensions).ToLower()})";

            var dir = transform.Find("folder-bar").GetComponent<InputField>();
            dir.onValueChanged.ClearAll();
            dir.onValueChanged.AddListener(_val => { UpdateBrowser(_val, fileExtensions, onSelectFile); });

            LSHelpers.DeleteChildren(viewport);
            CoreHelper.Log($"Update Browser: [{_folder}]");
            var directoryInfo = new DirectoryInfo(_folder);
            defaultDir = _folder;

            string[] directories = Directory.GetDirectories(defaultDir);
            string[] files = Directory.GetFiles(defaultDir);

            if (directoryInfo.Parent != null)
            {
                string backStr = directoryInfo.Parent.FullName;
                var gameObject = backPrefab.Duplicate(viewport, backStr);
                var backButton = gameObject.GetComponent<Button>();
                backButton.onClick.AddListener(() => { UpdateBrowser(backStr, fileExtensions, onSelectFile); });

                EditorThemeManager.ApplyGraphic(backButton.image, ThemeGroup.Back_Button, true);
                EditorThemeManager.ApplyGraphic(gameObject.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Back_Button_Text);
                EditorThemeManager.ApplyGraphic(gameObject.transform.GetChild(1).GetComponent<Text>(), ThemeGroup.Back_Button_Text);
            }

            string[] array = directories;
            for (int i = 0; i < array.Length; i++)
            {
                string folder = array[i];
                string name = new DirectoryInfo(folder).Name;
                var gameObject = folderPrefab.Duplicate(viewport, name);
                var folderPrefabStorage = gameObject.GetComponent<FunctionButtonStorage>();
                folderPrefabStorage.text.text = name;
                folderPrefabStorage.button.onClick.ClearAll();
                folderPrefabStorage.button.onClick.AddListener(() => { UpdateBrowser(folder, fileExtensions, onSelectFile); });

                var contextClickable = gameObject.AddComponent<ContextClickable>();
                contextClickable.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    RTEditor.inst.ShowContextMenu(300f,
                        new RTEditor.ButtonFunction("Open", () => { UpdateBrowser(folder, fileExtensions, onSelectFile); }),
                        new RTEditor.ButtonFunction(true),
                        new RTEditor.ButtonFunction("Create folder", () =>
                        {
                            RTEditor.inst.ShowFolderCreator(_folder, () => { UpdateBrowser(_folder, fileExtensions, onSelectFile); });
                        }),
                        new RTEditor.ButtonFunction("Create folder inside", () =>
                        {
                            RTEditor.inst.ShowFolderCreator(folder, () => { UpdateBrowser(folder, fileExtensions, onSelectFile); });
                        })
                        );
                };

                EditorThemeManager.ApplyGraphic(folderPrefabStorage.button.image, ThemeGroup.Folder_Button, true);
                EditorThemeManager.ApplyGraphic(folderPrefabStorage.text, ThemeGroup.Folder_Button_Text);
            }

            array = files;
            for (int i = 0; i < array.Length; i++)
            {
                string fileName = array[i];
                var fileInfoFolder = new FileInfo(fileName);
                string name = fileInfoFolder.Name;
                if (!fileExtensions.Any(x => x.ToLower() == fileInfoFolder.Extension.ToLower()))
                    continue;

                var gameObject = filePrefab.Duplicate(viewport, name);
                var folderPrefabStorage = gameObject.GetComponent<FunctionButtonStorage>();
                folderPrefabStorage.text.text = name;
                folderPrefabStorage.button.onClick.ClearAll();
                folderPrefabStorage.button.onClick.AddListener(() => { onSelectFile?.Invoke(fileInfoFolder.FullName); });

                var contextClickable = gameObject.AddComponent<ContextClickable>();
                contextClickable.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    if (fileExtensions.Any(x => x.ToLower() == ".wav" || x.ToLower() == ".ogg" || x.ToLower() == ".mp3"))
                        RTEditor.inst.ShowContextMenu(300f,
                            new RTEditor.ButtonFunction("Use", () => { onSelectFile?.Invoke(fileInfoFolder.FullName); }),
                            new RTEditor.ButtonFunction("Preview", () => { PreviewAudio(fileInfoFolder.FullName); }),
                            new RTEditor.ButtonFunction("Create folder", () =>
                            {
                                RTEditor.inst.ShowFolderCreator(_folder, () => { UpdateBrowser(_folder, fileExtensions, onSelectFile); });
                            })
                            );
                    else
                        RTEditor.inst.ShowContextMenu(300f,
                            new RTEditor.ButtonFunction("Use", () => { onSelectFile?.Invoke(fileInfoFolder.FullName); }),
                            new RTEditor.ButtonFunction("Create folder", () =>
                            {
                                RTEditor.inst.ShowFolderCreator(_folder, () => { UpdateBrowser(_folder, fileExtensions, onSelectFile); });
                            })
                            );
                };

                EditorThemeManager.ApplyGraphic(folderPrefabStorage.button.image, ThemeGroup.File_Button, true);
                EditorThemeManager.ApplyGraphic(folderPrefabStorage.text, ThemeGroup.File_Button_Text);
            }

            folderBar.text = defaultDir;
        }

        public void UpdateBrowser(string _folder, string fileExtension, string specificName = "", Action<string> onSelectFile = null)
        {
            contentContextMenu.onClick = null;
            if (!RTFile.DirectoryExists(_folder))
            {
                EditorManager.inst.DisplayNotification("Folder doesn't exist.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            contentContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                RTEditor.inst.ShowContextMenu(300f,
                    new RTEditor.ButtonFunction("Create folder", () =>
                    {
                        RTEditor.inst.ShowFolderCreator(_folder, () => { UpdateBrowser(_folder, fileExtension, specificName, onSelectFile); });
                    }));
            };

            title.text = $"<b>File Browser</b> ({fileExtension.ToLower()})";

            var dir = transform.Find("folder-bar").GetComponent<InputField>();
            dir.onValueChanged.ClearAll();
            dir.onValueChanged.AddListener(_val => { UpdateBrowser(_val, fileExtension, specificName, onSelectFile); });

            LSHelpers.DeleteChildren(viewport);
            CoreHelper.Log($"Update Browser: [{_folder}]");
            var directoryInfo = new DirectoryInfo(_folder);
            defaultDir = _folder;

            string[] directories = Directory.GetDirectories(defaultDir);
            string[] files = Directory.GetFiles(defaultDir);

            if (directoryInfo.Parent != null)
            {
                string backStr = directoryInfo.Parent.FullName;
                var gameObject = backPrefab.Duplicate(viewport, backStr);
                var backButton = gameObject.GetComponent<Button>();
                backButton.onClick.ClearAll();
                backButton.onClick.AddListener(() => { UpdateBrowser(backStr, fileExtension, specificName, onSelectFile); });

                EditorThemeManager.ApplyGraphic(backButton.image, ThemeGroup.Back_Button, true);
                EditorThemeManager.ApplyGraphic(gameObject.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Back_Button_Text);
                EditorThemeManager.ApplyGraphic(gameObject.transform.GetChild(1).GetComponent<Text>(), ThemeGroup.Back_Button_Text);
            }

            string[] array = directories;
            for (int i = 0; i < array.Length; i++)
            {
                string folder = array[i];
                string name = new DirectoryInfo(folder).Name;
                var gameObject = folderPrefab.Duplicate(viewport, name);
                var folderPrefabStorage = gameObject.GetComponent<FunctionButtonStorage>();
                folderPrefabStorage.text.text = name;
                folderPrefabStorage.button.onClick.ClearAll();
                folderPrefabStorage.button.onClick.AddListener(() => { UpdateBrowser(folder, fileExtension, specificName, onSelectFile); });

                var contextClickable = gameObject.AddComponent<ContextClickable>();
                contextClickable.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    RTEditor.inst.ShowContextMenu(300f,
                        new RTEditor.ButtonFunction("Open", () => { UpdateBrowser(folder, fileExtension, specificName, onSelectFile); }),
                        new RTEditor.ButtonFunction(true),
                        new RTEditor.ButtonFunction("Create folder", () =>
                        {
                            RTEditor.inst.ShowFolderCreator(_folder, () => { UpdateBrowser(_folder, fileExtension, specificName, onSelectFile); });
                        }),
                        new RTEditor.ButtonFunction("Create folder inside", () =>
                        {
                            RTEditor.inst.ShowFolderCreator(folder, () => { UpdateBrowser(folder, fileExtension, specificName, onSelectFile); });
                        })
                        );
                };

                EditorThemeManager.ApplyGraphic(folderPrefabStorage.button.image, ThemeGroup.Folder_Button, true);
                EditorThemeManager.ApplyGraphic(folderPrefabStorage.text, ThemeGroup.Folder_Button_Text);
            }

            array = files;
            for (int i = 0; i < array.Length; i++)
            {
                string fileName = array[i];
                var fileInfoFolder = new FileInfo(fileName);
                string name = fileInfoFolder.Name;
                if (fileInfoFolder.Extension.ToLower() != fileExtension.ToLower() || !(specificName == "" || specificName.ToLower() + fileExtension.ToLower() == name.ToLower()))
                    continue;

                var gameObject = filePrefab.Duplicate(viewport, name);
                var folderPrefabStorage = gameObject.GetComponent<FunctionButtonStorage>();
                folderPrefabStorage.text.text = name;
                folderPrefabStorage.button.onClick.ClearAll();
                folderPrefabStorage.button.onClick.AddListener(() => { onSelectFile?.Invoke(fileInfoFolder.FullName); });

                var contextClickable = gameObject.AddComponent<ContextClickable>();
                contextClickable.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    if (fileExtension.ToLower() == ".wav" || fileExtension.ToLower() == ".ogg" || fileExtension.ToLower() == ".mp3")
                        RTEditor.inst.ShowContextMenu(300f,
                            new RTEditor.ButtonFunction("Use", () => { onSelectFile?.Invoke(fileInfoFolder.FullName); }),
                            new RTEditor.ButtonFunction("Preview", () => { PreviewAudio(fileInfoFolder.FullName); }),
                            new RTEditor.ButtonFunction("Create folder", () =>
                            {
                                RTEditor.inst.ShowFolderCreator(_folder, () => { UpdateBrowser(_folder, fileExtension, specificName, onSelectFile); });
                            })
                            );
                    else
                        RTEditor.inst.ShowContextMenu(300f,
                            new RTEditor.ButtonFunction("Use", () => { onSelectFile?.Invoke(fileInfoFolder.FullName); }),
                            new RTEditor.ButtonFunction("Create folder", () =>
                            {
                                RTEditor.inst.ShowFolderCreator(_folder, () => { UpdateBrowser(_folder, fileExtension, specificName, onSelectFile); });
                            })
                            );
                };

                EditorThemeManager.ApplyGraphic(folderPrefabStorage.button.image, ThemeGroup.File_Button, true);
                EditorThemeManager.ApplyGraphic(folderPrefabStorage.text, ThemeGroup.File_Button_Text);
            }

            folderBar.text = defaultDir;
        }

        public void UpdateBrowser(string _folder, string specificName = "", Action<string> onSelectFolder = null)
        {
            contentContextMenu.onClick = null;
            if (!RTFile.DirectoryExists(_folder))
            {
                EditorManager.inst.DisplayNotification("Folder doesn't exist.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            contentContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                RTEditor.inst.ShowContextMenu(300f,
                    new RTEditor.ButtonFunction("Create folder", () =>
                    {
                        RTEditor.inst.ShowFolderCreator(_folder, () => { UpdateBrowser(_folder, specificName, onSelectFolder); });
                    }));
            };

            title.text = $"<b>File Browser</b> (Right click on a folder to use)";

            var dir = transform.Find("folder-bar").GetComponent<InputField>();
            dir.onValueChanged.ClearAll();
            dir.onValueChanged.AddListener(_val => { UpdateBrowser(_val, specificName, onSelectFolder); });

            LSHelpers.DeleteChildren(viewport);
            CoreHelper.Log($"Update Browser: [{_folder}]");
            var directoryInfo = new DirectoryInfo(_folder);
            defaultDir = _folder;

            string[] directories = Directory.GetDirectories(defaultDir);

            if (directoryInfo.Parent != null)
            {
                string backStr = directoryInfo.Parent.FullName;
                var gameObject = backPrefab.Duplicate(viewport, backStr);
                var backButton = gameObject.GetComponent<Button>();
                backButton.onClick.ClearAll();
                backButton.onClick.AddListener(() => { UpdateBrowser(backStr, specificName, onSelectFolder); });

                EditorThemeManager.ApplyGraphic(backButton.image, ThemeGroup.Back_Button, true);
                EditorThemeManager.ApplyGraphic(gameObject.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Back_Button_Text);
                EditorThemeManager.ApplyGraphic(gameObject.transform.GetChild(1).GetComponent<Text>(), ThemeGroup.Back_Button_Text);
            }

            string[] array = directories;
            for (int i = 0; i < array.Length; i++)
            {
                string folder = array[i];
                string name = new DirectoryInfo(folder).Name;
                var gameObject = folderPrefab.Duplicate(viewport, name);
                var folderPrefabStorage = gameObject.GetComponent<FunctionButtonStorage>();
                folderPrefabStorage.text.text = name;
                folderPrefabStorage.button.onClick.ClearAll();
                folderPrefabStorage.button.onClick.AddListener(() => { UpdateBrowser(folder, specificName, onSelectFolder); });

                var contextClickable = gameObject.AddComponent<ContextClickable>();
                contextClickable.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    RTEditor.inst.ShowContextMenu(300f,
                        new RTEditor.ButtonFunction("Use", () => { onSelectFolder?.Invoke(folder); }),
                        new RTEditor.ButtonFunction("Open", () => { UpdateBrowser(folder, specificName, onSelectFolder); }),
                        new RTEditor.ButtonFunction(true),
                        new RTEditor.ButtonFunction("Create folder", () =>
                        {
                            RTEditor.inst.ShowFolderCreator(_folder, () => { UpdateBrowser(_folder, specificName, onSelectFolder); });
                        }),
                        new RTEditor.ButtonFunction("Create folder inside", () =>
                        {
                            RTEditor.inst.ShowFolderCreator(folder, () => { UpdateBrowser(folder, specificName, onSelectFolder); });
                        })
                        );
                };

                EditorThemeManager.ApplyGraphic(folderPrefabStorage.button.image, ThemeGroup.Folder_Button, true);
                EditorThemeManager.ApplyGraphic(folderPrefabStorage.text, ThemeGroup.Folder_Button_Text, true);
            }

            folderBar.text = defaultDir;
        }

        void PreviewAudio(string file)
        {
            CoreHelper.StartCoroutineAsync(AlephNetworkManager.DownloadAudioClip($"file://{file}", RTFile.GetAudioType(file), audioClip =>
            {
                CoreHelper.ReturnToUnity(() =>
                {
                    var length = EditorConfig.Instance.FileBrowserAudioPreviewLength.Value;
                    DestroyAudioPreview();
                    if (currentDestroyPreviewCoroutine != null)
                        StopCoroutine(currentDestroyPreviewCoroutine);
                    currentDestroyPreviewCoroutine = null;
                    currentPreview = Camera.main.gameObject.AddComponent<AudioSource>();
                    currentPreview.clip = audioClip;
                    currentPreview.playOnAwake = true;
                    currentPreview.loop = true;
                    currentPreview.volume = AudioManager.inst.sfxVol;
                    currentPreview.time = length < 0f || audioClip.length <= length ? 0f : UnityEngine.Random.Range(0f, audioClip.length);
                    currentPreview.Play();
                    DestroyAudioPreviewAfterSeconds(Mathf.Clamp(length, 0f, audioClip.length));
                });
            }));
        }

        void DestroyAudioPreviewAfterSeconds(float t) => currentDestroyPreviewCoroutine = CoreHelper.StartCoroutine(CoreHelper.PerformActionAfterSeconds(t, DestroyAudioPreview));

        void DestroyAudioPreview()
        {
            if (!currentPreview)
                return;

            CoreHelper.Destroy(currentPreview);
            currentPreview = null;
            currentDestroyPreviewCoroutine = null;
        }

        Coroutine currentDestroyPreviewCoroutine;

        AudioSource currentPreview;

        public Transform viewport;

        public InputField folderBar;

        public GameObject filePrefab;

        public GameObject backPrefab;

        public GameObject folderPrefab;

        public InputField oggFileInput;

        public string defaultDir = "";

        public TextMeshProUGUI title;
    }
}
