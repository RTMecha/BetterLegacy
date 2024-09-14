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
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager inst;

        public RectTransform popup;
        public Image iconImage;
        public Text achievementName;
        public Text achievementDescription;
        public Image difficultyImage;

        public static void Init() => Creator.NewGameObject(nameof(AchievementManager), SystemManager.inst.transform).AddComponent<AchievementManager>();

        public void Awake()
        {
            inst = this;
            LoadAchievements();
            StartCoroutine(GenerateUI());
        }

        Sprite LoadIcon(string name) => SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}Achievements/{name}.png");

        void LoadAchievements()
        {
            CreateGlobalAchievement("welcome", "Welcome", "Welcome to BetterLegacy!", 0, "welcome");
            CreateGlobalAchievement("editor", "Create Something Awesome!", "Open the Project Arrhythmia editor.", 1, "editor");
            CreateGlobalAchievement("no_boost", "No Boosting!", "Do not boost once in a level.", 4, "no_boost");
            CreateGlobalAchievement("complete_animation", "Masterpiece.", "Complete an animation level.", 1, "complete_animation");
            CreateGlobalAchievement("costume_party", "Costume Party", "Play a level with a custom player model.", 2, "costume_party");
            CreateGlobalAchievement("expert_plus_ss_rank", "A true expert!", "Complete an expert+ difficulty level with SS rank.", 5, "expert_plus_ss_rank");
            CreateGlobalAchievement("master_ss_rank", "A true master!", "Complete a master difficulty level with SS rank.", 6, "master_ss_rank");
            CreateGlobalAchievement("editor_reverse_speed", "!yaw gnorw eht s'thaT", "Reverse the level in the editor.", 3, "editor_reverse_speed");
        }

        void CreateGlobalAchievement(string id, string name, string desc, int difficulty, string iconFileName)
        {
            var achievement = new Achievement(id, name, desc, difficulty, LoadIcon(iconFileName));
            achievement.unlocked = unlockedGlobalAchievements.ContainsKey(id) && unlockedGlobalAchievements[id];
            globalAchievements.Add(achievement);
        }

        IEnumerator GenerateUI()
        {
            while (!FontManager.inst || !FontManager.inst.loadedFiles)
                yield return null;

            var canvas = UIManager.GenerateUICanvas("Achievement Canvas", null, true);

            var popup = Creator.NewUIObject("Popup", canvas.GameObject.transform);
            this.popup = popup.transform.AsRT();
            UIManager.SetRectTransform(this.popup, new Vector2(0f, -100f), Vector2.right, Vector2.right, Vector2.right, new Vector2(400f, 100f));

            var popupImage = popup.AddComponent<Image>();
            EditorThemeManager.ApplyGraphic(popupImage, ThemeGroup.Background_1, true);

            var iconBase = Creator.NewUIObject("Icon Base", this.popup);
            UIManager.SetRectTransform(iconBase.transform.AsRT(), new Vector2(-150f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(64f, 64f));
            var iconBaseImage = iconBase.AddComponent<Image>();
            var iconBaseMask = iconBase.AddComponent<Mask>();
            iconBaseMask.showMaskGraphic = false;
            EditorThemeManager.ApplyGraphic(iconBaseImage, ThemeGroup.Null, true);

            var icon = Creator.NewUIObject("Icon", iconBase.transform);
            UIManager.SetRectTransform(icon.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);
            iconImage = icon.AddComponent<Image>();

            var name = Creator.NewUIObject("Name", this.popup);
            UIManager.SetRectTransform(name.transform.AsRT(), new Vector2(-100f, -16f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(290f, 100f));
            achievementName = name.AddComponent<Text>();
            achievementName.font = FontManager.inst.allFonts["Arrhythmia"];
            achievementName.text = "test name";

            var description = Creator.NewUIObject("Description", this.popup);
            UIManager.SetRectTransform(description.transform.AsRT(), new Vector2(-100f, -50f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(290f, 100f));
            achievementDescription = description.AddComponent<Text>();
            achievementDescription.font = FontManager.inst.allFonts["Fredoka One"];
            achievementDescription.text = "test description";

            var bar = Creator.NewUIObject("Difficulty", this.popup);
            UIManager.SetRectTransform(bar.transform.AsRT(), new Vector2(-8f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 0.5f), new Vector2(8f, 0f));
            difficultyImage = bar.AddComponent<Image>();
            EditorThemeManager.ApplyGraphic(difficultyImage, ThemeGroup.Null, true, roundedSide: SpriteHelper.RoundedSide.Left);

            popup.gameObject.SetActive(false);

            while (!AudioManager.inst && !AnimationManager.inst)
                yield return null;

            if (!RTFile.FileExists($"{RTFile.ApplicationDirectory}{RTFile.BepInExPluginsPath}EditorOnStartup.dll")) // show editor achievement if PA is open with the EditorOnStartup mod
                UnlockAchievement("welcome");
            else
                UnlockAchievement("editor");

            yield break;
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
            if (!list.Any(x => x.ID == id))
            {
                CoreHelper.LogError($"No achievement of ID {id}");
                return;
            }

            LockAchievement(list.First(x => x.ID == id));
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
            if (!list.Any(x => x.ID == id))
            {
                CoreHelper.LogError($"No achievement of ID {id}");
                return;
            }

            UnlockAchievement(list.First(x => x.ID == id));
        }

        /// <summary>
        /// Checks if an achievement with a matching ID exists and if it is unlocked.
        /// </summary>
        /// <param name="id">ID to find a matching achievement.</param>
        /// <returns>Returns true if an achievement is found and it is unlocked, otherwise returns false.</returns>
        public bool AchievementUnlocked(string id, bool global = true)
        {
            var list = global ? globalAchievements : customAchievements;
            if (!list.Any(x => x.ID == id))
            {
                CoreHelper.LogError($"No achievement of ID {id}");
                return false;
            }

            return list.First(x => x.ID == id).unlocked;
        }

        /// <summary>
        /// Displays the achievement popup.
        /// </summary>
        /// <param name="achievement">Achievement to apply to the popup UI.</param>
        public void ShowAchievement(Achievement achievement) => ShowAchievement(achievement.Name, achievement.Description, achievement.Icon, achievement.DifficultyType.color);

        public void ShowAchievement(string name, string description, Sprite icon, Color color = default)
        {
            CoreHelper.Log($"{CoreConfig.Instance.DisplayName.Value} Achieved - {name}");

            popup.gameObject.SetActive(true);
            achievementName?.SetText(LSText.ClampString(name.ToUpper(), 34));
            achievementDescription?.SetText(LSText.ClampString(description, 83));
            iconImage.sprite = icon;
            difficultyImage?.SetColor(color);

            AudioManager.inst.PlaySound("loadsound");

            AnimationManager.inst.RemoveName("Achievement Notification");

            var animation = new RTAnimation("Achievement Notification");
            animation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, -100f, Ease.Linear),
                    new FloatKeyframe(0.4f, 0f, Ease.BackOut),
                    new FloatKeyframe(3f, 0f, Ease.Linear),
                    new FloatKeyframe(3.4f, -100f, Ease.BackIn),
                    new FloatKeyframe(3.5f, -100f, Ease.Linear),
                }, x => { popup.anchoredPosition = new Vector2(0f, x); }),
            };
            animation.onComplete = () =>
            {
                popup.gameObject.SetActive(false);
                popup.anchoredPosition = new Vector2(0f, -100f);
                AnimationManager.inst.RemoveID(animation.id);
            };
            AnimationManager.inst.Play(animation);
        }

        /// <summary>
        /// Resets all achievements.
        /// </summary>
        public void ResetGlobalAchievements()
        {
            for (int i = 0; i < customAchievements.Count; i++)
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
