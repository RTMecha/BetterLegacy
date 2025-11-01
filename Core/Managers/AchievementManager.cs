using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Managers
{
    /// <summary>
    /// Manager class for handling BetterLegacy achievements.
    /// </summary>
    public class AchievementManager : BaseManager<AchievementManager, ManagerSettings>
    {
        #region Values

        /// <summary>
        /// Shared custom achievement unlocked states.
        /// </summary>
        public static Dictionary<string, bool> unlockedCustomAchievements = new Dictionary<string, bool>();

        /// <summary>
        /// What <see cref="globalAchievements"/> are unlocked.
        /// </summary>
        public static Dictionary<string, bool> unlockedGlobalAchievements = new Dictionary<string, bool>();

        /// <summary>
        /// List of built-in BetterLegacy achievements.
        /// </summary>
        public static List<Achievement> globalAchievements = new List<Achievement>();

        /// <summary>
        /// Achievement UI canvas parent.
        /// </summary>
        public UICanvas canvas;

        /// <summary>
        /// Achievement notification prefab.
        /// </summary>
        public GameObject achievementPrefab;

        #endregion

        #region Functions

        public override void OnInit()
        {
            LoadAchievements();
            StartCoroutine(GenerateUI());
        }

        /// <summary>
        /// Loads all global mod achievements.
        /// </summary>
        public void LoadAchievements()
        {
            globalAchievements.Clear();

            var array = AssetPack.GetArray($"core/achievements{FileFormat.JSON.Dot()}");
            for (int i = 0; i < array.Count; i++)
            {
                var item = array[i];

                try
                {
                    var id = item["id"];

                    var iconPath = item["icon_ref"] == null ? null : AssetPack.GetFile($"core/achievements/{item["icon_ref"].Value}{FileFormat.PNG.Dot()}");
                    var icon = !RTFile.FileExists(iconPath) ? LegacyPlugin.AtanPlaceholder : SpriteHelper.LoadSprite(iconPath);

                    var achievement = new Achievement(id, item["name"], item["desc"], item["difficulty"].AsInt, icon, item["hidden"].AsBool, item["hint"] ?? string.Empty);
                    achievement.unlocked = unlockedGlobalAchievements.TryGetValue(id, out bool unlocked) && unlocked;
                    globalAchievements.Add(achievement);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
            }
        }

        IEnumerator GenerateUI()
        {
            while (!FontManager.inst || !FontManager.inst.loadedFiles)
                yield return null;

            canvas = UIManager.GenerateUICanvas("Achievement Canvas", null, true);
            var layoutGroup = canvas.GameObject.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 8f;
            layoutGroup.childAlignment = TextAnchor.LowerRight;

            var popup = Creator.NewUIObject("Popup", canvas.GameObject.transform);
            UIManager.SetRectTransform(popup.transform.AsRT(), new Vector2(0f, -100f), Vector2.right, Vector2.right, Vector2.right, new Vector2(400f, 100f));

            var back = popup.AddComponent<Image>();

            var iconBase = Creator.NewUIObject("Icon Base", popup.transform);
            UIManager.SetRectTransform(iconBase.transform.AsRT(), new Vector2(-150f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(64f, 64f));
            var iconBaseImage = iconBase.AddComponent<Image>();
            var iconBaseMask = iconBase.AddComponent<Mask>();
            iconBaseMask.showMaskGraphic = false;

            var icon = Creator.NewUIObject("Icon", iconBase.transform);
            UIManager.SetRectTransform(icon.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);
            var iconImage = icon.AddComponent<Image>();

            var name = Creator.NewUIObject("Name", popup.transform);
            UIManager.SetRectTransform(name.transform.AsRT(), new Vector2(-100f, -16f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(290f, 100f));
            var achievementName = name.AddComponent<Text>();
            try
            {
                achievementName.font = FontManager.inst.allFonts["Arrhythmia"];
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
            achievementName.text = "test name";

            var description = Creator.NewUIObject("Description", popup.transform);
            UIManager.SetRectTransform(description.transform.AsRT(), new Vector2(-100f, -50f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(290f, 100f));
            var achievementDescription = description.AddComponent<Text>();
            try
            {
                achievementDescription.font = FontManager.inst.allFonts["Fredoka One"];
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
            achievementDescription.text = "test description";

            var bar = Creator.NewUIObject("Difficulty", popup.transform);
            UIManager.SetRectTransform(bar.transform.AsRT(), new Vector2(-8f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 0.5f), new Vector2(8f, 0f));
            var difficultyImage = bar.AddComponent<Image>();

            try
            {
                achievementPrefab = popup;
                var achievementStorage = achievementPrefab.AddComponent<AchievementStorage>();
                achievementStorage.back = back;
                achievementStorage.iconBase = iconBaseImage;
                achievementStorage.icon = iconImage;
                achievementStorage.title = achievementName;
                achievementStorage.description = achievementDescription;
                achievementStorage.difficulty = difficultyImage;
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            popup.gameObject.SetActive(false);

            while (!AudioManager.inst && !AnimationManager.inst)
                yield return null;

            if (!RTFile.FileExists($"{RTFile.ApplicationDirectory}{RTFile.BepInExPluginsPath}EditorOnStartup.dll")) // show editor achievement if PA is open with the EditorOnStartup mod
                UnlockAchievement("welcome");
            else
                UnlockAchievement("editor");

            yield break;
        }

        #region Unlock / Lock

        /// <summary>
        /// Checks for level started related achievements.
        /// </summary>
        public void CheckLevelBeginAchievements()
        {
            if (PlayerManager.Players.Count > 1)
                UnlockAchievement("friendship");
            var tags = LevelManager.CurrentLevel.metadata.tags;
            if (tags.Contains("joke") || tags.Contains("joke_level") || tags.Contains("meme") || tags.Contains("meme_level"))
                UnlockAchievement("youve_been_trolled");
            if (tags.Contains("high_detail") || tags.Contains("lag"))
                UnlockAchievement("no_fps");
        }

        /// <summary>
        /// Checks for level finished related achievements.
        /// </summary>
        /// <param name="metadata">Metadata of the level.</param>
        /// <param name="levelRank">Level rank that was gained.</param>
        public void CheckLevelEndAchievements(MetaData metadata, Rank rank)
        {
            if (metadata.song.difficulty == 6)
                UnlockAchievement("complete_animation");
            if (metadata.song.difficulty != 6 && RTBeatmap.Current && RTBeatmap.Current.boosts.Count == 0)
                UnlockAchievement("no_boost");
            if (rank == Rank.F)
                UnlockAchievement("f_rank");
            if (metadata.song.difficulty == 4 && rank == Rank.SS)
                UnlockAchievement("expert_plus_ss_rank");
            if (metadata.song.difficulty == 5 && rank == Rank.SS)
                UnlockAchievement("master_ss_rank");
            if (RTBeatmap.Current.CurrentMusicVolume == 0)
                UnlockAchievement("no_volume");

            if (LevelManager.currentQueueIndex >= 9)
                UnlockAchievement("queue_ten");
        }

        /// <summary>
        /// Locks the achievement, marking it incomplete.
        /// </summary>
        /// <param name="achievement">Achievement to lock.</param>
        public void LockAchievement(Achievement achievement)
        {
            if (achievement && achievement.unlocked)
            {
                achievement.unlocked = false;
                LegacyPlugin.SaveProfile();
                CoreHelper.Log($"Locked achievement {name}");
            }
        }

        /// <summary>
        /// Locks the achievement, marking it incomplete.
        /// </summary>
        /// <param name="id">ID to find a matching achievement and lock.</param>
        public void LockAchievement(string id, bool global = true)
        {
            if (!global)
            {
                LevelManager.CurrentLevel?.saveData?.LockAchievement(id);
                return;
            }

            if (!globalAchievements.TryFind(x => x.id == id, out Achievement achievement))
            {
                CoreHelper.LogError($"No achievement of ID {id}");
                return;
            }

            LockAchievement(achievement);
        }

        /// <summary>
        /// Unlocks the achievement, marking it complete.
        /// </summary>
        /// <param name="achievement">Achievement to unlock.</param>
        public void UnlockAchievement(Achievement achievement)
        {
            if (achievement && !achievement.unlocked)
            {
                achievement.unlocked = true;
                LegacyPlugin.SaveProfile();
                ShowAchievement(achievement);
            }
        }

        /// <summary>
        /// Unlocks the achievement, marking it complete.
        /// </summary>
        /// <param name="id">ID to find a matching achievement and unlock.</param>
        public void UnlockAchievement(string id, bool global = true)
        {
            if (!global)
            {
                LevelManager.CurrentLevel?.saveData?.UnlockAchievement(id);
                return;
            }

            if (!globalAchievements.TryFind(x => x.id == id, out Achievement achievement))
            {
                CoreHelper.LogError($"No achievement of ID {id}");
                return;
            }

            UnlockAchievement(achievement);
        }

        /// <summary>
        /// Checks if an achievement with a matching ID exists and if it is unlocked.
        /// </summary>
        /// <param name="id">ID to find a matching achievement.</param>
        /// <returns>Returns true if an achievement is found and it is unlocked, otherwise returns false.</returns>
        public bool AchievementUnlocked(string id, bool global = true)
        {
            if (!global)
                return LevelManager.CurrentLevel && LevelManager.CurrentLevel.saveData && LevelManager.CurrentLevel.saveData.AchievementUnlocked(id);

            if (!globalAchievements.TryFind(x => x.id == id, out Achievement achievement))
            {
                CoreHelper.LogError($"No achievement of ID {id}");
                return false;
            }

            return achievement.unlocked;
        }

        /// <summary>
        /// Resets all achievements.
        /// </summary>
        public void ResetGlobalAchievements()
        {
            for (int i = 0; i < globalAchievements.Count; i++)
                globalAchievements[i].unlocked = false;
            LegacyPlugin.SaveProfile();
        }

        /// <summary>
        /// Resets all achievements.
        /// </summary>
        public void ResetCustomAchievements()
        {
            var customAchievements = LevelManager.CurrentLevel.GetAchievements();

            if (customAchievements == null)
                return;

            for (int i = 0; i < customAchievements.Count; i++)
                customAchievements[i].unlocked = false;
            LevelManager.SaveProgress();
        }

        #endregion
        
        #region UI

        /// <summary>
        /// Displays the achievement popup.
        /// </summary>
        /// <param name="id">ID of the achievement to find.</param>
        /// <param name="global">If it should search the global list.</param>
        public void ShowAchievement(string id, bool global = true)
        {
            var list = global ? globalAchievements : LevelManager.CurrentLevel.GetAchievements();

            if (list == null)
                return;

            if (!list.TryFind(x => x.id == id, out Achievement achievement))
            {
                CoreHelper.LogError($"No achievement of ID {id}");
                return;
            }

            ShowAchievement(achievement);
        }

        /// <summary>
        /// Displays the achievement popup.
        /// </summary>
        /// <param name="achievement">Achievement to apply to the popup UI.</param>
        public void ShowAchievement(Achievement achievement)
        {
            if (achievement)
                ShowAchievement(achievement.name, achievement.description, achievement.icon, achievement.DifficultyType.Color);
        }

        /// <summary>
        /// Displays the achievement popup.
        /// </summary>
        /// <param name="name">Name of the achievement.</param>
        /// <param name="description">Description of the achievement.</param>
        /// <param name="icon">Icon of the achievement.</param>
        /// <param name="color">Difficulty color of the achievement.</param>
        public void ShowAchievement(string name, string description, Sprite icon, Color color = default)
        {
            try
            {
                var achievement = achievementPrefab.Duplicate(canvas.GameObject.transform, "Achievement");
                achievement.SetActive(true);

                var achievementStorage = achievement.GetComponent<AchievementStorage>();
                achievementStorage.title.text = LSText.ClampString(name.ToUpper(), 34);
                achievementStorage.description.text = LSText.ClampString(description, 83);
                achievementStorage.icon.sprite = icon;
                achievementStorage.difficulty.color = color;

                EditorThemeManager.ApplyGraphic(achievementStorage.back, ThemeGroup.Background_1, true);
                EditorThemeManager.ApplyGraphic(achievementStorage.iconBase, ThemeGroup.Null, true);
                EditorThemeManager.ApplyLightText(achievementStorage.title);
                EditorThemeManager.ApplyLightText(achievementStorage.description);
                EditorThemeManager.ApplyGraphic(achievementStorage.difficulty, ThemeGroup.Null, true, roundedSide: SpriteHelper.RoundedSide.Left);

                var animation = new RTAnimation("Achievement Notification");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, 0f, Ease.Linear),
                        new FloatKeyframe(0.4f, 1f, Ease.BackOut),
                        new FloatKeyframe(3f, 1f, Ease.BackOut),
                        new FloatKeyframe(3.4f, 0f, Ease.BackIn),
                        new FloatKeyframe(3.5f, 0f, Ease.Linear),
                    }, achievement.transform.SetLocalScaleY),
                };
                animation.onComplete = () => CoreHelper.Destroy(achievement);
                AnimationManager.inst.Play(animation);

                SoundManager.inst.PlaySound(DefaultSounds.loadsound);
                CoreHelper.Log($"{CoreConfig.Instance.DisplayName.Value} Achieved - {name}");
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        #endregion

        #endregion
    }
}
