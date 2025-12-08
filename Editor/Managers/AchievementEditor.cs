using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using SimpleJSON;
using Crosstales.FB;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Popups;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Editor class that manages custom achievements.
    /// </summary>
    public class AchievementEditor : BaseManager<AchievementEditor, EditorManagerSettings>
    {
        #region Values

        /// <summary>
        /// Dialog of the editor.
        /// </summary>
        public AchievementEditorDialog Dialog { get; set; }

        /// <summary>
        /// Popup of the editor.
        /// </summary>
        public ContentPopup Popup { get; set; }

        /// <summary>
        /// The current achievement.
        /// </summary>
        public Achievement CurrentAchievement { get; set; }

        /// <summary>
        /// List of the current levels' achievements.
        /// </summary>
        public List<Achievement> achievements = new List<Achievement>();

        /// <summary>
        /// Achievement list button prefab.
        /// </summary>
        public GameObject achievementListButtonPrefab;

        /// <summary>
        /// List of copied achievements.
        /// </summary>
        public List<Achievement> copiedAchievements = new List<Achievement>();

        /// <summary>
        /// The level ID from which the copied achievements were from. If the ID matches the current level, the achievements' IDs will be shuffled.
        /// </summary>
        public string copiedLevelID;

        #endregion

        #region Functions

        public override void OnInit()
        {
            try
            {
                Dialog = new AchievementEditorDialog();
                Dialog.Init();

                achievementListButtonPrefab = CheckpointEditor.inst.checkpointListButtonPrefab.Duplicate(transform);
                CoreHelper.Delete(achievementListButtonPrefab.transform.Find("time"));
                EditorPrefabHolder.Instance.DeleteButton.Duplicate(achievementListButtonPrefab.transform, "delete");

                EditorHelper.AddEditorDropdown("Edit Achievements", string.Empty, EditorHelper.EDIT_DROPDOWN, EditorSprites.ExclaimSprite, OpenDialog);

                Popup = RTEditor.inst.GeneratePopup(EditorPopup.ACHIEVEMENTS_POPUP, "Achievements", Vector2.zero, new Vector2(600f, 400f));
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        /// <summary>
        /// Loads the current levels' achievements.
        /// </summary>
        public void LoadAchievements()
        {
            achievements.Clear();
            try
            {
                var path = EditorLevelManager.inst.CurrentLevel.GetFile(Level.ACHIEVEMENTS_LSA);
                if (!RTFile.FileExists(path))
                    return;

                var jn = JSON.Parse(RTFile.ReadFromFile(path));
                for (int i = 0; i < jn["achievements"].Count; i++)
                {
                    var achievement = Achievement.Parse(jn["achievements"][i]);
                    if (jn["achievements"][i]["icon_path"] != null)
                        achievement.CheckIconPath(EditorLevelManager.inst.CurrentLevel.GetFile(jn["achievements"][i]["icon_path"]));
                    if (jn["achievements"][i]["locked_icon_path"] != null)
                        achievement.CheckIconPath(EditorLevelManager.inst.CurrentLevel.GetFile(jn["achievements"][i]["locked_icon_path"]));
                    achievements.Add(achievement);
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Something went wrong with loading achievements. Is something corrupt?\nException: {ex}");
            }
        }

        /// <summary>
        /// Saves the current levels' achievements.
        /// </summary>
        public void SaveAchievements()
        {
            try
            {
                if (achievements.IsEmpty())
                {
                    RTFile.DeleteFile(EditorLevelManager.inst.CurrentLevel.GetFile(Level.ACHIEVEMENTS_LSA));
                    return;
                }

                var jn = Parser.NewJSONObject();
                for (int i = 0; i < achievements.Count; i++)
                    jn["achievements"][i] = achievements[i].ToJSON();
                RTFile.WriteToFile(EditorLevelManager.inst.CurrentLevel.GetFile(Level.ACHIEVEMENTS_LSA), jn.ToString());
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Something went wrong with saving achievements. Is something corrupt?\nException: {ex}");
            }
        }

        /// <summary>
        /// Creates a new achievement.
        /// </summary>
        public void CreateNewAchievement()
        {
            var achievement = new Achievement()
            {
                name = "NEW ACHIEVEMENT",
                description = "This is the default description!",
            };
            achievements.Add(achievement);
            OpenDialog(achievement);
        }

        /// <summary>
        /// Copies the currently selected achievement.
        /// </summary>
        public void CopyAchievement() => CopyAchievement(CurrentAchievement);

        /// <summary>
        /// Copies an achievement.
        /// </summary>
        /// <param name="achievement">Achievement to copy.</param>
        public void CopyAchievement(Achievement achievement)
        {
            if (!achievement)
            {
                EditorManager.inst.DisplayNotification($"Select an achievement to copy first.", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            copiedAchievements.Clear();
            copiedAchievements.Add(achievement.Copy(false));
            copiedLevelID = EditorLevelManager.inst.CurrentLevel.id;
            EditorManager.inst.DisplayNotification("Copied achievement.", 1f, EditorManager.NotificationType.Success);
        }

        /// <summary>
        /// Pastes the copied achievements into the achievements list.
        /// </summary>
        public void PasteAchievements()
        {
            if (copiedAchievements.IsEmpty())
            {
                EditorManager.inst.DisplayNotification($"No copied achievements yet!", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            var newID = copiedLevelID == EditorLevelManager.inst.CurrentLevel.id;
            achievements.AddRange(copiedAchievements.Select(x => x.Copy(newID)));
            EditorManager.inst.DisplayNotification("Pasted achievements.", 1f, EditorManager.NotificationType.Success);
            RenderAchievementList();
        }

        /// <summary>
        /// Deletes the currently selected achievement.
        /// </summary>
        public void DeleteAchievement() => DeleteAchievement(CurrentAchievement);

        /// <summary>
        /// Deletes the currently selected achievement.
        /// </summary>
        /// <param name="achievement">Achievement to delete.</param>
        public void DeleteAchievement(Achievement achievement)
        {
            if (!achievement)
            {
                EditorManager.inst.DisplayNotification($"Select an achievement to delete first.", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            var index = achievements.IndexOf(achievement);
            achievements.RemoveAt(index);
            EditorManager.inst.DisplayNotification("Deleted achievement.", 1f, EditorManager.NotificationType.Success);
            OpenDialog(achievements.TryGetAt(index - 1, out Achievement prevAchievement) ? prevAchievement : null);
        }

        /// <summary>
        /// Sets the currently editing achievement.
        /// </summary>
        /// <param name="index">Index of the achievement.</param>
        public void SetCurrentAchievement(int index)
        {
            if (achievements.TryGetAt(index, out Achievement achievement))
                OpenDialog(achievement);
        }

        /// <summary>
        /// Opens the editor dialog.
        /// </summary>
        public void OpenDialog() => OpenDialog(null);

        /// <summary>
        /// Opens the editor dialog.
        /// </summary>
        /// <param name="achievement">Achievement to edit.</param>
        public void OpenDialog(Achievement achievement)
        {
            CurrentAchievement = achievement;

            Dialog.Open();
            RenderDialog(achievement);
        }

        /// <summary>
        /// Renders the editor dialog.
        /// </summary>
        /// <param name="achievement">Achievement to edit.</param>
        public void RenderDialog(Achievement achievement)
        {
            RenderAchievementList();

            Dialog.LeftContent.gameObject.SetActive(achievement);
            if (!achievement)
                return;

            Dialog.IDText.text = achievement.id;

            EditorContextMenu.AddContextMenu(Dialog.IDBase.gameObject,
                leftClick: () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied ID from {achievement.name}!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard(achievement.id);
                },
                new ButtonElement("Copy ID", () =>
                {
                    EditorManager.inst.DisplayNotification($"Copied ID from {achievement.name}!", 2f, EditorManager.NotificationType.Success);
                    LSText.CopyToClipboard(achievement.id);
                }),
                new ButtonElement("Shuffle ID", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to shuffle the ID of this achievement?", () =>
                {
                    achievement.id = PAObjectBase.GetNumberID();
                    RenderDialog(achievement);
                    RTEditor.inst.HideWarningPopup();
                }, RTEditor.inst.HideWarningPopup)));

            Dialog.NameField.SetTextWithoutNotify(achievement.name);
            Dialog.NameField.onValueChanged.NewListener(_val =>
            {
                achievement.name = _val;
                RenderAchievementList();
            });

            Dialog.DescriptionField.SetTextWithoutNotify(achievement.description);
            Dialog.DescriptionField.onValueChanged.NewListener(_val => achievement.description = _val);

            var icon = achievement.icon ?? LegacyPlugin.AtanPlaceholder;
            Dialog.IconImage.sprite = icon;
            Dialog.SelectIconButton.OnClick.NewListener(() =>
            {
                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonElement($"Select Icon ({RTEditor.SYSTEM_BROWSER})", () =>
                    {
                        string imageFile = FileBrowser.OpenSingleFile("Select an image!", RTEditor.inst.BasePath, new string[] { "png" });
                        if (string.IsNullOrEmpty(imageFile))
                            return;

                        achievement.icon = SpriteHelper.LoadSprite(imageFile);
                        RenderDialog(achievement);
                    }),
                    new ButtonElement($"Select Icon ({RTEditor.EDITOR_BROWSER})", () =>
                    {
                        RTEditor.inst.BrowserPopup.Open();
                        RTFileBrowser.inst.UpdateBrowserFile(new string[] { FileFormat.PNG.Dot(), FileFormat.JPG.Dot() }, imageFile =>
                        {
                            if (string.IsNullOrEmpty(imageFile))
                                return;

                            RTEditor.inst.BrowserPopup.Close();
                            achievement.icon = SpriteHelper.LoadSprite(imageFile);
                            RenderDialog(achievement);
                        });
                    }));
            });
            Dialog.LockedIconImage.sprite = achievement.lockedIcon ?? icon;
            Dialog.SelectLockedIconButton.OnClick.NewListener(() =>
            {
                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonElement($"Select Icon ({RTEditor.SYSTEM_BROWSER})", () =>
                    {
                        string imageFile = FileBrowser.OpenSingleFile("Select an image!", RTEditor.inst.BasePath, new string[] { "png" });
                        if (string.IsNullOrEmpty(imageFile))
                            return;

                        achievement.lockedIcon = SpriteHelper.LoadSprite(imageFile);
                        RenderDialog(achievement);
                    }),
                    new ButtonElement($"Select Icon ({RTEditor.EDITOR_BROWSER})", () =>
                    {
                        RTEditor.inst.BrowserPopup.Open();
                        RTFileBrowser.inst.UpdateBrowserFile(new string[] { FileFormat.PNG.Dot(), FileFormat.JPG.Dot() }, imageFile =>
                        {
                            if (string.IsNullOrEmpty(imageFile))
                                return;

                            RTEditor.inst.BrowserPopup.Close();
                            achievement.lockedIcon = SpriteHelper.LoadSprite(imageFile);
                            RenderDialog(achievement);
                        });
                    }));
            });
            Dialog.RemoveLockedIconButton.OnClick.NewListener(() =>
            {
                achievement.lockedIcon = null;
                RenderDialog(achievement);
            });

            Dialog.HiddenToggle.SetIsOnWithoutNotify(achievement.hidden);
            Dialog.HiddenToggle.OnValueChanged.NewListener(_val => achievement.hidden = _val);

            Dialog.HintField.SetTextWithoutNotify(achievement.hint);
            Dialog.HintField.onValueChanged.NewListener(_val => achievement.hint = _val);

            RenderDifficulty(achievement);

            Dialog.SharedToggle.SetIsOnWithoutNotify(achievement.shared);
            Dialog.SharedToggle.OnValueChanged.NewListener(_val => achievement.shared = _val);

            Dialog.PreviewButton.OnClick.NewListener(DisplayAchievement);
        }

        /// <summary>
        /// Renders the difficulty of an achievement.
        /// </summary>
        /// <param name="achievement">Achievement to render.</param>
        public void RenderDifficulty(Achievement achievement)
        {
            LSHelpers.DeleteChildren(Dialog.DifficultyParent);

            var values = CustomEnumHelper.GetValues<DifficultyType>();
            var count = values.Length - 1;

            foreach (var difficulty in values)
            {
                if (difficulty.Ordinal < 0) // skip unknown difficulty
                    continue;

                var gameObject = RTMetaDataEditor.inst.difficultyToggle.Duplicate(Dialog.DifficultyParent, difficulty.DisplayName.ToLower(), difficulty == count - 1 ? 0 : difficulty + 1);
                gameObject.transform.localScale = Vector3.one;

                gameObject.transform.AsRT().sizeDelta = new Vector2(69f, 32f);

                var text = gameObject.transform.Find("Background/Text").GetComponent<Text>();
                text.color = LSColors.ContrastColor(difficulty.Color);
                text.text = difficulty == count - 1 ? "Anim" : difficulty.DisplayName;
                text.fontSize = 17;
                var toggle = gameObject.GetComponent<Toggle>();
                toggle.image.color = difficulty.Color;
                toggle.group = null;
                toggle.SetIsOnWithoutNotify(achievement.DifficultyType == difficulty);
                toggle.onValueChanged.NewListener(_val =>
                {
                    achievement.DifficultyType = difficulty;
                    RenderDifficulty(achievement);
                });

                EditorThemeManager.ApplyGraphic(toggle.image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(toggle.graphic, ThemeGroup.Background_1);
            }
        }

        /// <summary>
        /// Displays a preview of the achievement.
        /// </summary>
        public void DisplayAchievement()
        {
            var achievement = CurrentAchievement;
            if (achievement)
                AchievementManager.inst.ShowAchievement(achievement.name, achievement.description, achievement.icon ?? LegacyPlugin.AtanPlaceholder, achievement.DifficultyType.Color);
        }

        /// <summary>
        /// Renders the achievements list.
        /// </summary>
        public void RenderAchievementList()
        {
            Dialog.ClearContent();

            var add = EditorPrefabHolder.Instance.CreateAddButton(Dialog.Content);
            add.Text = "Add new Achievement";
            add.OnClick.NewListener(CreateNewAchievement);

            int num = 0;
            foreach (var achievement in achievements)
            {
                if (!RTString.SearchString(Dialog.SearchTerm, achievement.name))
                {
                    num++;
                    continue;
                }

                var index = num;
                var gameObject = achievementListButtonPrefab.Duplicate(Dialog.Content, $"{achievement.name}_checkpoint");
                gameObject.transform.AsRT().sizeDelta = new Vector2(350f, 38f);

                var selected = gameObject.transform.Find("dot").GetComponent<Image>();
                var name = gameObject.transform.Find("name").GetComponent<Text>();

                name.text = achievement.name;
                selected.enabled = achievement == CurrentAchievement;

                var button = gameObject.GetComponent<Button>();
                button.onClick.ClearAll();
                EditorContextMenu.AddContextMenu(gameObject, leftClick: () => SetCurrentAchievement(index),
                    new ButtonElement("Edit", () => SetCurrentAchievement(index)),
                    new ButtonElement("Delete", () => DeleteAchievement(achievement)),
                    new SpacerElement(),
                    new ButtonElement("Copy", () => CopyAchievement(achievement)),
                    new ButtonElement("Copy All", () =>
                    {
                        copiedAchievements.Clear();
                        copiedAchievements.AddRange(achievements.Select(x => x.Copy(false)));
                        copiedLevelID = EditorLevelManager.inst.CurrentLevel.id;
                        EditorManager.inst.DisplayNotification("Copied achievements.", 1f, EditorManager.NotificationType.Success);
                    }),
                    new ButtonElement("Paste", PasteAchievements));

                var deleteButton = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
                deleteButton.OnClick.NewListener(() => DeleteAchievement(achievement));

                EditorThemeManager.ApplyGraphic(button.image, ThemeGroup.List_Button_2_Normal, true);
                EditorThemeManager.ApplyGraphic(selected, ThemeGroup.List_Button_2_Text);
                EditorThemeManager.ApplyGraphic(name, ThemeGroup.List_Button_2_Text);

                EditorThemeManager.ApplyDeleteButton(deleteButton);

                num++;
            }
        }

        /// <summary>
        /// Opens the achievement list popup.
        /// </summary>
        /// <param name="onSelect">Function to run when the achievement is selected.</param>
        public void OpenPopup(Action<Achievement> onSelect)
        {
            Popup.Open();
            RenderPopup(onSelect);
        }

        /// <summary>
        /// Renders the achievement list popup.
        /// </summary>
        /// <param name="onSelect">Function to run when the achievement is selected.</param>
        public void RenderPopup(Action<Achievement> onSelect)
        {
            Popup.ClearContent();
            Popup.SearchField.onValueChanged.NewListener(_val => RenderPopup(onSelect));

            for (int i = 0; i < achievements.Count; i++)
            {
                var achievement = achievements[i];
                if (!RTString.SearchString(Popup.SearchTerm, achievement.name, new SearchMatcher(achievement.id, SearchMatchType.Exact)))
                    continue;

                Popup.GenerateListButton($"{achievement.id} - {achievement.name}", pointerEventData =>
                {
                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonElement("Edit", () => OpenDialog(achievement)));
                        return;
                    }

                    if (onSelect != null)
                    {
                        onSelect.Invoke(achievement);
                        return;
                    }

                    OpenDialog(achievement);
                });
            }
        }

        #endregion
    }
}
