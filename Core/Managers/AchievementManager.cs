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
            StartCoroutine(GenerateUI());
        }

        public IEnumerator GenerateUI()
        {
            while (!FontManager.inst || !FontManager.inst.loadedFiles)
                yield return null;

            var canvas = UIManager.GenerateUICanvas("Achievement Canvas", null, true);

            var popup = Creator.NewUIObject("Popup", canvas.GameObject.transform);
            this.popup = popup.transform.AsRT();
            UIManager.SetRectTransform(this.popup, new Vector2(760f, -590f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400f, 100f));

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
            UIManager.SetRectTransform(description.transform.AsRT(), new Vector2(-100f, -48f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(290f, 100f));
            achievementDescription = description.AddComponent<Text>();
            achievementDescription.font = FontManager.inst.allFonts["Fredoka One"];
            achievementDescription.text = "test description";

            var bar = Creator.NewUIObject("Difficulty", this.popup);
            UIManager.SetRectTransform(bar.transform.AsRT(), new Vector2(-8f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 0.5f), new Vector2(8f, 0f));
            difficultyImage = bar.AddComponent<Image>();
            EditorThemeManager.ApplyGraphic(difficultyImage, ThemeGroup.Null, true, roundedSide: SpriteManager.RoundedSide.Left);

            yield break;
        }

        public void RemoveAchievement(string name)
        {
            if (!achievements.Any(x => x.Name == name))
            {
                CoreHelper.LogError($"No achievement of name {name}");
                return;
            }

            var achievement = achievements.First(x => x.Name == name);

            if (!achievement || achievement.unlocked)
            {
                achievement.unlocked = false;
                CoreHelper.Log($"Removed achievement {name}");
            }
        }

        public void SetAchievement(string name)
        {
            if (!achievements.Any(x => x.Name == name))
            {
                CoreHelper.LogError($"No achievement of name {name}");
                return;
            }

            var achievement = achievements.First(x => x.Name == name);

            if (achievement && !achievement.unlocked)
            {
                achievement.unlocked = true;
                ShowAchievement(achievement.Name, achievement.Description, achievement.Icon, achievement.DifficultyType.color);
            }
        }

        public bool GetAchievement(string name)
        {
            if (!achievements.Any(x => x.Name == name))
            {
                CoreHelper.LogError($"No achievement of name {name}");
                return false;
            }

            return achievements.First(x => x.Name == name);
        }

        public void ShowAchievement(string name, string description, Sprite icon, Color color = default)
        {
            CoreHelper.Log($"{CoreConfig.Instance.DisplayName.Value} Achieved - {name}");

            popup.gameObject.SetActive(true);
            achievementName?.SetText(LSText.ClampString(name, 34));
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
                    new FloatKeyframe(0f, -590f, Ease.Linear),
                    new FloatKeyframe(0.4f, -490f, Ease.BackOut),
                    new FloatKeyframe(3f, -490f, Ease.Linear),
                    new FloatKeyframe(3.4f, -590f, Ease.BackIn),
                    new FloatKeyframe(3.5f, -590f, Ease.Linear),
                }, delegate (float x)
                {
                    popup.anchoredPosition = new Vector2(760f, x);
                }),
            };
            animation.onComplete = delegate ()
            {
                popup.gameObject.SetActive(false);
                AnimationManager.inst.RemoveID(animation.id);
            };
            AnimationManager.inst.Play(animation);
        }

        public void ResetAchievements()
        {
            for (int i = 0; i < achievements.Count; i++)
                achievements[i].unlocked = false;
        }

        public static List<Achievement> achievements = new List<Achievement>
        {
            Achievement.TestAchievement,
        };

        public static List<AchievementFunction> requirements = new List<AchievementFunction>()
        {
            () => true,
        };
    }
}
