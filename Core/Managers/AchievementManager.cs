using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using System.Collections;
using UnityEngine.UI;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Configs;
using LSFunctions;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;

namespace BetterLegacy.Core.Managers
{
    public class AchievementStorage : MonoBehaviour
    {
        [SerializeField]
        public Image back;

        [SerializeField]
        public Image iconBase;

        [SerializeField]
        public Image icon;

        [SerializeField]
        public Text title;

        [SerializeField]
        public Text description;

        [SerializeField]
        public Image difficulty;
    }

    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager inst;

        public UICanvas canvas;

        public GameObject achievementPrefab;

        public static void Init() => Creator.NewGameObject(nameof(AchievementManager), SystemManager.inst.transform).AddComponent<AchievementManager>();

        void Awake()
        {
            inst = this;
            LoadAchievements();
            StartCoroutine(GenerateUI());
        }

        Sprite LoadIcon(string name)
        {
            var path = $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Achievements/{name}.png";
            return !RTFile.FileExists(path) ? LegacyPlugin.AtanPlaceholder : SpriteHelper.LoadSprite(path);
        }

        void LoadAchievements()
        {
            CreateGlobalAchievement("welcome", "Welcome", "Welcome to BetterLegacy!", 0, "welcome");
            CreateGlobalAchievement("editor", "Create Something Awesome!", "Open the Project Arrhythmia editor.", 1, "editor");
            CreateGlobalAchievement("no_boost", "No Boosting!", "Do not boost once in a level.", 4, "no_boost");
            CreateGlobalAchievement("complete_animation", "Masterpiece.", "Complete an animation level.", 1, "complete_animation");
            CreateGlobalAchievement("costume_party", "Costume Party", "Play a level with a custom player model.", 2, "costume_party");
            CreateGlobalAchievement("f_rank", "At least we made it...", "Complete a level with an F rank", 1, "f_rank");
            CreateGlobalAchievement("expert_plus_ss_rank", "A true expert!", "Complete an expert+ difficulty level with an SS rank.", 5, "expert_plus_ss_rank");
            CreateGlobalAchievement("master_ss_rank", "A true master!", "Complete a master difficulty level with an SS rank.", 6, "master_ss_rank");
            CreateGlobalAchievement("editor_reverse_speed", "!yaw gnorw eht s'thaT", "Reverse the level in the editor.", 3, "editor_reverse_speed");
            CreateGlobalAchievement("editor_layer_lol", "nice", "nice.", 4, "editor_layer_lol");
            CreateGlobalAchievement("editor_layer_funny", "Thai Funny", "Go to editor layer 555.", 4, "editor_layer_funny");
            CreateGlobalAchievement("example_chat", "Having a Conversation", "Talk with Example.", 4, "example_chat");
            CreateGlobalAchievement("example_touch", "No touchie touchie!", "Do not the Example.", 4, "example_touch");
            CreateGlobalAchievement("editor_zoom_break", "Breaking Boundaries", "Change the zoom keyframe to a value lower than 0.", 3, "editor_zoom_break");
            CreateGlobalAchievement("no_volume", "Is the Sound Off?", "Complete a level with volume turned down to 0.", 4, "no_volume");
            CreateGlobalAchievement("queue_ten", "Data Management", "Play 10 levels in a row in a Queue.", 4, "queue_ten");
            CreateGlobalAchievement("friendship", "Friendship!", "Play with friends!", 4, "friendship");
            CreateGlobalAchievement("holy_keyframes", "Holy Keyframes!", "Look at an object in the editor with over 1000 keyframes.", 6, "holy_keyframes");
            CreateGlobalAchievement("serious_dedication", "Serious Dedication", "Spend 10 hours in a level in the editor.", 5, "serious_dedication");
            CreateGlobalAchievement("true_dedication", "True Dedication", "Spend 24 hours in a level in the editor.", 6, "true_dedication");
            CreateGlobalAchievement("upload_level", "Upload a Level!", "Publish a level to the arcade server.", 2, "upload_level");
            CreateGlobalAchievement("youve_been_trolled", "You've Been Trolled", "Play a meme / joke level.", 3, "youve_been_trolled");
            CreateGlobalAchievement("no_fps", "No FPS?", "Play a high detail level.", 4, "no_fps");
            CreateGlobalAchievement("ten_levels", "That's some data.", "Complete 10 levels.", 2, "ten_levels");
            CreateGlobalAchievement("fifty_levels", "B", "That's more data!", 3, "fifty_levels");
            CreateGlobalAchievement("one_hundred_levels", "That's a lot of data!", "Complete 100 levels.", 4, "one_hundred_levels");
            CreateGlobalAchievement("hackerman", "Hackerman", "Open UnityExplorer in the editor.", 0, "hackerman");
            CreateGlobalAchievement("death_hd", "Death HD", "Die in the editor.", 3, "death_hd");
            CreateGlobalAchievement("select_player", "This is not a clicker game!", "Select the player in the editor preview window.", 4, "select_player");
            CreateGlobalAchievement("time_machine", "Time Machine", "Convert a VG file to an LS file, or the other way around.", 4, "time_machine");
            CreateGlobalAchievement("time_traveler", "Time Traveler", "Play a level made in the modern (alpha / public) editor.", 2, "time_traveler");
            CreateGlobalAchievement("discover_hidden_levels", "Still in Alpha", "Discover a secret.", 4, "discover_hidden_levels");

            // Story related
            CreateGlobalAchievement("story_doc01_complete", "The Purpose", "Complete Story Mode chapter 1.", 1, "story_doc01_complete");
            CreateGlobalAchievement("story_doc01_secret", "Fall into the Night", "Discover the first secret...", 2, "story_doc01_secret");

            // todo:
            // story_doc01_full
        }

        public void CreateGlobalAchievement(string id, string name, string desc, int difficulty, string iconFileName)
        {
            try
            {
                var achievement = new Achievement(id, name, desc, difficulty, LoadIcon(iconFileName));
                achievement.unlocked = unlockedGlobalAchievements.TryGetValue(id, out bool unlocked) && unlocked;
                globalAchievements.Add(achievement);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
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

        public void CheckLevelBeginAchievements()
        {
            if (PlayerManager.Players.Count > 1)
                UnlockAchievement("friendship");
            var tags = LevelManager.CurrentLevel.metadata.song.tags;
            if (tags.Contains("joke") || tags.Contains("joke_level") || tags.Contains("meme") || tags.Contains("meme_level"))
                UnlockAchievement("youve_been_trolled");
            if (tags.Contains("high_detail") || tags.Contains("lag"))
                UnlockAchievement("no_fps");
        }

        public void CheckLevelEndAchievements(MetaData metadata, DataManager.LevelRank levelRank)
        {
            if (metadata.song.difficulty == 6)
                UnlockAchievement("complete_animation");
            if (metadata.song.difficulty != 6 && LevelManager.BoostCount == 0)
                UnlockAchievement("no_boost");
            if (levelRank.name == "F")
                UnlockAchievement("f_rank");
            if (metadata.song.difficulty == 4 && levelRank.name == "SS")
                UnlockAchievement("expert_plus_ss_rank");
            if (metadata.song.difficulty == 5 && levelRank.name == "SS")
                UnlockAchievement("master_ss_rank");
            if (LevelManager.CurrentMusicVolume == 0)
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
            if (achievement != null && achievement.unlocked)
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
            var list = global ? globalAchievements : customAchievements;
            if (!list.TryFind(x => x.ID == id, out Achievement achievement))
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
            if (achievement != null && !achievement.unlocked)
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
            var list = global ? globalAchievements : customAchievements;
            if (!list.TryFind(x => x.ID == id, out Achievement achievement))
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
            var list = global ? globalAchievements : customAchievements;
            if (!list.TryFind(x => x.ID == id, out Achievement achievement))
            {
                CoreHelper.LogError($"No achievement of ID {id}");
                return false;
            }

            return achievement.unlocked;
        }

        public void ShowAchievement(string id, bool global = true)
        {
            var list = global ? globalAchievements : customAchievements;
            if (!list.TryFind(x => x.ID == id, out Achievement achievement))
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
        public void ShowAchievement(Achievement achievement) => ShowAchievement(achievement.Name, achievement.Description, achievement.Icon, achievement.DifficultyType.color);

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
                animation.onComplete = () => { CoreHelper.Destroy(achievement); };
                AnimationManager.inst.Play(animation);

                AudioManager.inst.PlaySound("loadsound");
                CoreHelper.Log($"{CoreConfig.Instance.DisplayName.Value} Achieved - {name}");
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
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
            for (int i = 0; i < customAchievements.Count; i++)
                customAchievements[i].unlocked = false;
        }

        public static Dictionary<string, bool> unlockedGlobalAchievements = new Dictionary<string, bool>();

        public static List<Achievement> globalAchievements = new List<Achievement>();

        public static List<Achievement> customAchievements = new List<Achievement>
        {
            Achievement.TestAchievement,
        };
    }
}
