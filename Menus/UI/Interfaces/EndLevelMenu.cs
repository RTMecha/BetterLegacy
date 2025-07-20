using System.Linq;
using UnityEngine;

using LSFunctions;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Companion.Data.Parameters;
using BetterLegacy.Companion.Entity;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Layouts;

namespace BetterLegacy.Menus.UI.Interfaces
{
    public class EndLevelMenu : MenuBase
    {
        public static EndLevelMenu Current { get; set; }

        public EndLevelMenu() : base()
        {
            if (!CoreHelper.InGame || CoreHelper.InEditor)
            {
                CoreHelper.LogError($"End of level cannot occur outside of a level.");
                return;
            }

            if (RTEventManager.windowPositionResolutionChanged)
            {
                RTEventManager.windowPositionResolutionChanged = false;
                WindowController.ResetResolution();
            }

            GameManager.inst.players.SetActive(false);
            InputDataManager.inst.SetAllControllerRumble(0f);
            onGenerateUIFinish = () => InputDataManager.inst.SetAllControllerRumble(0f);

            GameManager.inst.timeline.gameObject.SetActive(false);

            var metadata = LevelManager.CurrentLevel.metadata;

            int prevHits = LevelManager.CurrentLevel.saveData ? LevelManager.CurrentLevel.saveData.Hits : -1;

            CoreHelper.Log($"Setting More Info");
            var hitsNormalized = LevelManager.GetHitsNormalized(RTBeatmap.Current.hits);
            CoreHelper.Log($"Setting Level Ranks");
            var rank = Rank.Null.TryGetValue(x => hitsNormalized.Sum() >= x.MinHits && hitsNormalized.Sum() <= x.MaxHits, out Rank _rank) ? _rank : Rank.Null;
            var prevLevelRank = Rank.Null.TryGetValue(x => prevHits >= x.MinHits && prevHits <= x.MaxHits, out Rank _prevRank) ? _prevRank : Rank.Null;

            if (!RTBeatmap.Current.challengeMode.Damageable)
            {
                rank = Rank.Null;
                prevLevelRank = Rank.Null;
            }

            AchievementManager.inst.CheckLevelEndAchievements(metadata, rank);

            CoreHelper.Log($"Setting End UI");
            string easy = LSColors.GetThemeColorHex("easy");
            string normal = LSColors.GetThemeColorHex("normal");
            string hard = LSColors.GetThemeColorHex("hard");
            string expert = LSColors.GetThemeColorHex("expert");

            AudioManager.inst.SetMusicTime(ArcadeHelper.ReplayLevel ? 0f : AudioManager.inst.CurrentAudioSource.clip.length - 0.01f);
            SoundManager.inst.SetPlaying(ArcadeHelper.ReplayLevel);

            layouts.Add("results", new MenuVerticalLayout
            {
                name = "results",
                rect = RectValues.Default.AnchoredPosition(-700f, 140f).SizeDelta(400f, 400f),
            });

            layouts.Add("buttons", new MenuHorizontalLayout
            {
                name = "buttons",
                rect = RectValues.Default.AnchoredPosition(-240f, -330f).SizeDelta(1200f, 64f),
                spacing = 32f,
                childControlWidth = true,
                childForceExpandWidth = true,
            });

            elements.Add(new MenuImage
            {
                id = "35255236785",
                name = "Background",
                siblingIndex = 0,
                rect = RectValues.FullAnchored,
                color = 0,
                val = -999f,
                opacity = 0.7f,
                length = 0f,
            });

            elements.AddRange(GenerateTopBar($"Level Summary > <b>\"{metadata.beatmap.name}\"</b> ({metadata.artist.name} - {metadata.song.title})"));

            int line = 5;
            for (int i = 0; i < 11; i++) // code is based on the Legacy code atm. can't think of a better way of doing this
            {
                string text = "<b>";
                for (int j = 0; j < LevelManager.DATA_POINT_MAX; j++)
                {
                    int sum = hitsNormalized.Take(j + 1).Sum();
                    int sumLerp = (int)RTMath.SuperLerp(0f, 15f, 0f, (float)11, (float)sum);
                    string color = sum == 0 ? easy : sum <= 3 ? normal : sum <= 9 ? hard : expert;

                    for (int k = 0; k < 2; k++)
                    {
                        if (sumLerp == i)
                            text = text + "<color=" + color + "ff>▓</color>";
                        else if (sumLerp > i)
                            text += "<alpha=#22>▓";
                        else if (sumLerp < i)
                            text = text + "<color=" + color + "44>▓</color>";
                    }
                }
                text += "</b>";
                if (line == 5)
                {
                    text = "<voffset=0.6em>" + text;

                    if (prevHits > RTBeatmap.Current.hits.Count && prevLevelRank != Rank.Null)
                        text += $"       <voffset=0em><size=300%>{prevLevelRank.Format()}<size=150%> <voffset=0.325em><b>-></b> <voffset=0em><size=300%>{rank.Format()}";
                    else
                        text += $"       <voffset=0em><size=300%>{rank.Format()}";
                }

                if (line == 7)
                    text = "<voffset=0.6em>" + text + $"       <voffset=0em><size=300%><color=#{LSColors.ColorToHex(rank.Color)}><b>{LevelManager.CalculateAccuracy(RTBeatmap.Current.hits.Count, AudioManager.inst.CurrentAudioSource.clip.length)}%</b></color>";
                if (line == 9)
                    text = "<voffset=0em>" + text + $"       <voffset=0em><size=100%><b>You died a total of {RTBeatmap.Current.deaths.Count} times.</b></color>";
                if (line == 10)
                    text = "<voffset=0em>" + text + $"       <voffset=0em><size=100%><b>You got hit a total of {RTBeatmap.Current.hits.Count} times</b></color>";
                if (line == 11)
                    text = "<voffset=0em>" + text + $"       <voffset=0em><size=100%><b>You boosted a total of {RTBeatmap.Current.boosts.Count} times</b></color>";
                if (line == 12)
                    text = "<voffset=0em>" + text + $"       <voffset=0em><size=100%><b>Song length is {RTString.SecondsToTime(AudioManager.inst.CurrentAudioSource.clip.length)}</b></color>";
                if (line == 13)
                    text = "<voffset=0em>" + text + $"       <voffset=0em><size=100%><b>You spent {RTString.SecondsToTime(RTBeatmap.Current.levelTimer.time)} in the level.</b></color>";

                elements.Add(new MenuText
                {
                    id = LSText.randomNumString(16),
                    name = "Result Text",
                    text = text,
                    parentLayout = "results",
                    length = 0.01f,
                    rect = RectValues.Default.SizeDelta(300f, 46f),
                    hideBG = true,
                    textVal = 40f,
                });

                line++;
            }

            if (Example.Current && Example.Current.model && Example.Current.model.Visible)
                Example.Current.chatBubble?.SayDialogue(ExampleChatBubble.Dialogues.END_LEVEL_SCREEN, new LevelDialogueParameters(LevelManager.CurrentLevel, rank));
            else
            {
                var rankSayings = metadata && metadata.customSayings.TryGetValue(rank, out string[] customSayings) ? customSayings : LegacyResources.sayings[rank];
                var sayings = LSText.WordWrap(rankSayings[Random.Range(0, rankSayings.Length)], 32);
                layouts.Add("sayings", new MenuVerticalLayout
                {
                    name = "sayings",
                    rect = RectValues.Default.AnchoredPosition(420f, -300f).SizeDelta(400f, 400f),
                });

                for (int i = 0; i < sayings.Count; i++)
                {
                    var text = sayings[i];
                    elements.Add(new MenuText
                    {
                        id = LSText.randomNumString(16),
                        name = "Sayings Text",
                        text = text,
                        parentLayout = "sayings",
                        length = 0.1f,
                        rect = RectValues.Default.SizeDelta(300f, 32f),
                        hideBG = true,
                        textVal = 40f,
                    });
                }
            }

            var nextLevel = LevelManager.NextLevelInCollection;
            if (LevelManager.CurrentLevelCollection && (metadata.song.DifficultyType == DifficultyType.Animation || nextLevel && nextLevel.saveData && nextLevel.saveData.Unlocked || !RTBeatmap.Current.challengeMode.Invincible || LevelManager.currentLevelIndex + 1 != LevelManager.CurrentLevelCollection.Count) || !LevelManager.IsNextEndOfQueue)
            {
                if (nextLevel)
                    CoreHelper.Log($"Selecting next Arcade level in collection [{LevelManager.currentLevelIndex + 2} / {LevelManager.CurrentLevelCollection.Count}]");
                else
                    CoreHelper.Log($"Selecting next Arcade level in queue [{LevelManager.currentQueueIndex + 2} / {LevelManager.ArcadeQueue.Count}]");

                elements.Add(new MenuButton
                {
                    id = "0",
                    name = "Next Button",
                    text = "<b><align=center>[ NEXT ]", // continue if story mode.
                    parentLayout = "buttons",
                    autoAlignSelectionPosition = true,
                    rect = RectValues.Default.SizeDelta(100f, 64f),
                    opacity = 0.1f,
                    val = -40f,
                    textVal = 40f,
                    selectedOpacity = 1f,
                    selectedVal = 40f,
                    selectedTextVal = -40f,
                    length = 0.3f,
                    playBlipSound = true,
                    func = ArcadeHelper.NextLevel,
                });
            }

            if (LevelManager.HasQueue)
            {
                elements.Add(new MenuButton
                {
                    id = "674",
                    name = "Return Button",
                    text = "<b><align=center>[ RESTART QUEUE ]",
                    parentLayout = "buttons",
                    autoAlignSelectionPosition = true,
                    rect = RectValues.Default.SizeDelta(100f, 64f),
                    opacity = 0.1f,
                    val = -40f,
                    textVal = 40f,
                    selectedOpacity = 1f,
                    selectedVal = 40f,
                    selectedTextVal = -40f,
                    length = 0.3f,
                    playBlipSound = true,
                    func = ArcadeHelper.FirstLevel,
                });
            }

            if (LevelManager.Hub)
            {
                elements.Add(new MenuButton
                {
                    id = "625235",
                    name = "Return Button",
                    text = "<b><align=center>[ RETURN TO HUB ]",
                    parentLayout = "buttons",
                    autoAlignSelectionPosition = true,
                    rect = RectValues.Default.SizeDelta(100f, 64f),
                    opacity = 0.1f,
                    val = -40f,
                    textVal = 40f,
                    selectedOpacity = 1f,
                    selectedVal = 40f,
                    selectedTextVal = -40f,
                    length = 0.3f,
                    playBlipSound = true,
                    func = ArcadeHelper.ReturnToHub,
                });
            }

            elements.Add(new MenuButton
            {
                id = "1",
                name = "Arcade Button",
                text = "<b><align=center>[ TO ARCADE ]",
                parentLayout = "buttons",
                autoAlignSelectionPosition = true,
                opacity = 0.1f,
                val = -40f,
                textVal = 40f,
                selectedOpacity = 1f,
                selectedVal = 40f,
                selectedTextVal = -40f,
                length = 0.3f,
                playBlipSound = true,
                rect = RectValues.Default.SizeDelta(100f, 64f),
                func = ArcadeHelper.QuitToArcade,
            });

            elements.Add(new MenuButton
            {
                id = "2",
                name = "Arcade Button",
                text = "<b><align=center>[ REPLAY ]",
                parentLayout = "buttons",
                autoAlignSelectionPosition = true,
                opacity = 0.1f,
                val = -40f,
                textVal = 40f,
                selectedOpacity = 1f,
                selectedVal = 40f,
                selectedTextVal = -40f,
                length = 0.3f,
                playBlipSound = true,
                rect = RectValues.Default.SizeDelta(100f, 64f),
                func = Close,
            });

            elements.AddRange(GenerateBottomBar());

            InterfaceManager.inst.SetCurrentInterface(this);
        }

        public override void UpdateTheme()
        {
            Theme = CoreHelper.CurrentBeatmapTheme;

            base.UpdateTheme();
        }

        /// <summary>
        /// Initializes the end level menu.
        /// </summary>
        public static void Init() => Current = new EndLevelMenu();

        public static void Close()
        {
            ArcadeHelper.RestartLevel();
            InterfaceManager.inst.CloseMenus();
            RTBeatmap.Current?.Resume();
        }
    }
}
