using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Layouts;
using LSFunctions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

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

            InterfaceManager.inst.CurrentMenu = this;

            GameManager.inst.players.SetActive(false);
            InputDataManager.inst.SetAllControllerRumble(0f);

            GameManager.inst.timeline.gameObject.SetActive(false);
            LSHelpers.ShowCursor();
            
            var metadata = LevelManager.CurrentLevel.metadata;

            int prevHits = LevelManager.CurrentLevel.playerData != null ? LevelManager.CurrentLevel.playerData.Hits : -1;
            LevelManager.UpdateCurrentLevelProgress();

            CoreHelper.Log($"Setting More Info");
            {
                int dataPointMax = 24;
                int[] hitsNormalized = new int[dataPointMax + 1];
                foreach (var playerDataPoint in GameManager.inst.hits)
                {
                    int num5 = (int)RTMath.SuperLerp(0f, AudioManager.inst.CurrentAudioSource.clip.length, 0f, (float)dataPointMax, playerDataPoint.time);
                    hitsNormalized[num5]++;
                }

                CoreHelper.Log($"Setting Level Ranks");
                var levelRank = DataManager.inst.levelRanks.Find(x => hitsNormalized.Sum() >= x.minHits && hitsNormalized.Sum() <= x.maxHits);
                var prevLevelRank = DataManager.inst.levelRanks.Find(x => prevHits >= x.minHits && prevHits <= x.maxHits);

                if (PlayerManager.IsZenMode)
                {
                    levelRank = DataManager.inst.levelRanks.Find(x => x.name == "-");
                    prevLevelRank = null;
                }

                AchievementManager.inst.CheckLevelEndAchievements(metadata, levelRank);

                CoreHelper.Log($"Setting End UI");
                string easy = LSColors.GetThemeColorHex("easy");
                string normal = LSColors.GetThemeColorHex("normal");
                string hard = LSColors.GetThemeColorHex("hard");
                string expert = LSColors.GetThemeColorHex("expert");

                AudioManager.inst.SetMusicTime(CoreConfig.Instance.ReplayLevel.Value ? 0f : AudioManager.inst.CurrentAudioSource.clip.length - 0.01f);
                SoundManager.inst.SetPlaying(CoreConfig.Instance.ReplayLevel.Value);

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

                elements.AddRange(GenerateTopBar($"Level Summary > <b>\"{metadata.beatmap.name}\"</b> ({metadata.artist.Name} - {metadata.song.title})"));

                int line = 5;
                for (int i = 0; i < 11; i++)
                {
                    string text = "<b>";
                    for (int j = 0; j < dataPointMax; j++)
                    {
                        int sum = hitsNormalized.Take(j + 1).Sum();
                        int sumLerp = (int)RTMath.SuperLerp(0f, 15f, 0f, (float)11, (float)sum);
                        string color = sum == 0 ? easy : sum <= 3 ? normal : sum <= 9 ? hard : expert;

                        for (int k = 0; k < 2; k++)
                        {
                            if (sumLerp == i)
                            {
                                text = text + "<color=" + color + "ff>▓</color>";
                            }
                            else if (sumLerp > i)
                            {
                                text += "<alpha=#22>▓";
                            }
                            else if (sumLerp < i)
                            {
                                text = text + "<color=" + color + "44>▓</color>";
                            }
                        }
                    }
                    text += "</b>";
                    if (line == 5)
                    {
                        text = "<voffset=0.6em>" + text;

                        if (prevHits > GameManager.inst.hits.Count && prevLevelRank != null)
                            text += $"       <voffset=0em><size=300%>{RTString.FormatLevelRank(prevLevelRank)}<size=150%> <voffset=0.325em><b>-></b> <voffset=0em><size=300%>{RTString.FormatLevelRank(levelRank)}";
                        else
                            text += $"       <voffset=0em><size=300%>{RTString.FormatLevelRank(levelRank)}";
                    }

                    if (line == 7)
                        text = "<voffset=0.6em>" + text +  $"       <voffset=0em><size=300%><color=#{LSColors.ColorToHex(levelRank.color)}><b>{LevelManager.CalculateAccuracy(GameManager.inst.hits.Count, AudioManager.inst.CurrentAudioSource.clip.length)}%</b></color>";
                    if (line == 9)
                        text = "<voffset=0em>" + text + $"       <voffset=0em><size=100%><b>You died a total of {GameManager.inst.deaths.Count} times.</b></color>";
                    if (line == 10)
                        text = "<voffset=0em>" + text + $"       <voffset=0em><size=100%><b>You got hit a total of {GameManager.inst.hits.Count} times</b></color>";
                    if (line == 11)
                        text = "<voffset=0em>" + text + $"       <voffset=0em><size=100%><b>You boosted a total of {LevelManager.BoostCount} times</b></color>";
                    if (line == 12)
                        text = "<voffset=0em>" + text + $"       <voffset=0em><size=100%><b>Song length is {RTString.SecondsToTime(AudioManager.inst.CurrentAudioSource.clip.length)}</b></color>";
                    if (line == 13)
                        text = "<voffset=0em>" + text + $"       <voffset=0em><size=100%><b>You spent {RTString.SecondsToTime(LevelManager.timeInLevel)} in the level.</b></color>";

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

                var saying = levelRank.sayings[Random.Range(0, levelRank.sayings.Length)];

                if (Example.ExampleManager.inst && Example.ExampleManager.inst.Visible)
                {
                    var matches = Regex.Matches(saying, @"{{QuickElement=(.*?)}}");
                    foreach (var obj in matches)
                    {
                        var match = (Match)obj;
                        saying = saying.Replace(match.Groups[0].ToString(), "");
                    }

                    Example.ExampleManager.inst.Say(saying);
                }
                else
                {
                    var sayings = LSText.WordWrap(saying, 32);
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

                if (LevelManager.NextLevelInCollection || !LevelManager.IsNextEndOfQueue)
                {
                    if (LevelManager.NextLevelInCollection != null)
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

                if (LevelManager.Hub != null)
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
                    func = () => ArcadeHelper.RestartLevel(true, Close),
                });

                elements.AddRange(GenerateBottomBar());
            }

            InterfaceManager.inst.CurrentGenerateUICoroutine = CoreHelper.StartCoroutine(GenerateUI());
        }

        public override void UpdateTheme()
        {
            Theme = CoreHelper.CurrentBeatmapTheme;

            base.UpdateTheme();
        }

        public static void Init()
        {
            InterfaceManager.inst.CloseMenus();

            Current = new EndLevelMenu();
        }

        public static void Close()
        {
            InterfaceManager.inst.CloseMenus();
            AudioManager.inst.CurrentAudioSource.UnPause();
            AudioManager.inst.CurrentAudioSource.Play();
            GameManager.inst.gameState = GameManager.State.Playing;
        }
    }
}
