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
using System.Threading.Tasks;
using UnityEngine;

namespace BetterLegacy.Menus.UI.Interfaces
{
    public class EndLevelMenu : MenuBase
    {
        public static EndLevelMenu Current { get; set; }

        public EndLevelMenu() : base(false)
        {
            if (!CoreHelper.InGame || CoreHelper.InEditor)
            {
                CoreHelper.LogError($"End of level cannot occur outside of a level.");
                return;
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
                var newLevelRank = DataManager.inst.levelRanks.Find(x => prevHits >= x.minHits && prevHits <= x.maxHits);

                if (PlayerManager.IsZenMode)
                {
                    levelRank = DataManager.inst.levelRanks.Find(x => x.name == "-");
                    newLevelRank = null;
                }

                // TODO: custom achievements
                //CoreHelper.Log($"Setting Achievements");
                //if (levelRank.name == "SS")
                //    SteamWrapper.inst.achievements.SetAchievement("SS_RANK");
                //else if (levelRank.name == "F")
                //    SteamWrapper.inst.achievements.SetAchievement("F_RANK");

                CoreHelper.Log($"Setting End UI");
                var sayings = LSText.WordWrap(levelRank.sayings[Random.Range(0, levelRank.sayings.Length)], 32);
                string easy = LSColors.GetThemeColorHex("easy");
                string normal = LSColors.GetThemeColorHex("normal");
                string hard = LSColors.GetThemeColorHex("hard");
                string expert = LSColors.GetThemeColorHex("expert");

                if (CoreConfig.Instance.ReplayLevel.Value)
                {
                    AudioManager.inst.SetMusicTime(0f);
                    AudioManager.inst.CurrentAudioSource.Play();
                }
                else
                {
                    AudioManager.inst.SetMusicTime(AudioManager.inst.CurrentAudioSource.clip.length - 0.01f);
                    AudioManager.inst.CurrentAudioSource.Pause();
                }

                layouts.Add("results", new MenuVerticalLayout
                {
                    name = "results",
                    rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(-700f, 140f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400f, 400f)),
                });
                
                layouts.Add("sayings", new MenuVerticalLayout
                {
                    name = "sayings",
                    rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(420f, -300f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400f, 400f)),
                });

                layouts.Add("buttons", new MenuHorizontalLayout
                {
                    name = "buttons",
                    rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(-240f, -330f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1200f, 64f)),
                    spacing = 32f,
                    childControlWidth = true,
                    childForceExpandWidth = true,
                });

                elements.Add(new MenuImage
                {
                    id = "35255236785",
                    name = "Background",
                    siblingIndex = 0,
                    rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero),
                    color = 0,
                    val = -999f,
                    opacity = 0.7f,
                    length = 0f,
                });

                elements.Add(new MenuText
                {
                    id = "264726346",
                    name = "Top Title",
                    text = $"Level Summary > <b>\"{metadata.LevelBeatmap.name}\"</b> ({metadata.artist.Name} - {metadata.song.title}) | BetterLegacy {LegacyPlugin.ModVersion}",
                    rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(0f, 460f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f)),
                    textRectJSON = SimpleJSON.JSON.Parse("{\"anc_pos\": { \"x\": \"-850\",\"y\": \"0\" } }"),
                    hideBG = true,
                    textVal = 40f,
                    length = 0.6f,
                });

                elements.Add(new MenuText
                {
                    id = "800",
                    name = "Top Bar",
                    text = "<size=56>----------------------------------------------------------------",
                    rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(0f, 400f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f)),
                    textRectJSON = SimpleJSON.JSON.Parse("{\"anc_pos\": { \"x\": \"-870\",\"y\": \"0\" } }"),
                    hideBG = true,
                    textVal = 40f,
                    length = 0.6f,
                });

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

                        if (prevHits > GameManager.inst.hits.Count && newLevelRank != null)
                        {
                            text += $"       <voffset=0em><size=300%><#{LSColors.ColorToHex(newLevelRank.color)}><b>{newLevelRank.name}</b></color><size=150%> <voffset=0.325em><b>-></b> <voffset=0em><size=300%><#{LSColors.ColorToHex(levelRank.color)}><b>{levelRank.name}</b></color>";
                        }
                        else
                        {
                            text += $"       <voffset=0em><size=300%><#{LSColors.ColorToHex(levelRank.color)}><b>{levelRank.name}</b></color>";
                        }
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
                        text = "<voffset=0em>" + text + $"       <voffset=0em><size=100%><b>Song length is {FontManager.TextTranslater.SecondsToTime(AudioManager.inst.CurrentAudioSource.clip.length)}</b></color>";
                    if (line == 13)
                        text = "<voffset=0em>" + text + $"       <voffset=0em><size=100%><b>You spent {FontManager.TextTranslater.SecondsToTime(LevelManager.timeInLevel)} in the level.</b></color>";
                    //if (line >= 9 && sayings.Count > line - 9)
                    //{
                    //    text = text + "       <alpha=#ff>" + sayings[line - 9];
                    //}

                    elements.Add(new MenuText
                    {
                        id = LSText.randomNumString(16),
                        name = "Result Text",
                        text = text,
                        parentLayout = "results",
                        length = 0.01f,
                        rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 46f)),
                        hideBG = true,
                        textVal = 40f,
                    });

                    line++;
                }

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
                        rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 32f)),
                        hideBG = true,
                        textVal = 40f,
                    });
                }

                LevelManager.currentQueueIndex += 1;
                if (LevelManager.ArcadeQueue.Count > 1 && LevelManager.currentQueueIndex < LevelManager.ArcadeQueue.Count)
                {
                    CoreHelper.Log($"Selecting next Arcade level in queue [{LevelManager.currentQueueIndex + 1} / {LevelManager.ArcadeQueue.Count}]");
                    LevelManager.CurrentLevel = LevelManager.ArcadeQueue[LevelManager.currentQueueIndex];

                    elements.Add(new MenuButton
                    {
                        id = "0",
                        name = "Next Button",
                        text = "<b><align=center>[ NEXT ]", // continue if story mode.
                        parentLayout = "buttons",
                        autoAlignSelectionPosition = true,
                        rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 64f)),
                        opacity = 0.1f,
                        val = -40f,
                        textVal = 40f,
                        selectedOpacity = 1f,
                        selectedVal = 40f,
                        selectedTextVal = -40f,
                        length = 0.3f,
                        playBlipSound = true,
                        func = Continue,
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
                    rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 64f)),
                    func = ToArcade,
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
                    rectJSON = MenuImage.GenerateRectTransformJSON(Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 64f)),
                    func = RestartLevel,
                });
                elements.Add(new MenuText
                {
                    id = "801",
                    name = "Bottom Bar",
                    text = "<size=56>----------------------------------------------------------------",
                    rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(0f, -400f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f)),
                    textRectJSON = SimpleJSON.JSON.Parse("{\"anc_pos\": { \"x\": \"-870\",\"y\": \"0\" } }"),
                    hideBG = true,
                    textVal = 40f,
                    length = 0.6f,
                });

                elements.Add(new MenuText
                {
                    id = "264726346",
                    name = "Bottom Title",
                    text = $"<align=right><#F05355><b>Project Arrhythmia</b></color> | Unified Operating System | Version {ProjectArrhythmia.GameVersion}",
                    rectJSON = MenuImage.GenerateRectTransformJSON(new Vector2(0f, -460f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 100f)),
                    textRectJSON = SimpleJSON.JSON.Parse("{\"anc_pos\": { \"x\": \"850\",\"y\": \"0\" } }"),
                    hideBG = true,
                    textVal = 40f,
                    length = 0.6f,
                });
            }

            CoreHelper.StartCoroutine(GenerateUI());
        }

        public override void UpdateTheme()
        {
            Theme = CoreHelper.CurrentBeatmapTheme;

            base.UpdateTheme();
        }

        public static void RestartLevel()
        {
            if (CoreHelper.InEditor)
                return;

            if (ArcadeHelper.endedLevel)
            {
                LevelManager.currentQueueIndex -= 1;
                AudioManager.inst.SetMusicTime(0f);
                GameManager.inst.hits.Clear();
                GameManager.inst.deaths.Clear();
                ArcadeHelper.endedLevel = false;

                UnPause();
                LevelManager.LevelEnded = false;

                return;
            }

            AudioManager.inst.SetMusicTime(0f);
            GameManager.inst.hits.Clear();
            GameManager.inst.deaths.Clear();
            UnPause();
            ArcadeHelper.endedLevel = false;
        }

        public static void ToArcade()
        {
            Current?.Clear();
            Current = null;
            if (InterfaceManager.inst.CurrentMenu is PauseMenu)
                InterfaceManager.inst.CurrentMenu = null;
            GameManager.inst.QuitToArcade();
        }

        public static void Continue()
        {
            Current?.Clear();
            Current = null;
            if (InterfaceManager.inst.CurrentMenu is PauseMenu)
                InterfaceManager.inst.CurrentMenu = null;
            SceneManager.inst.LoadScene("Game");
        }

        // BetterLegacy.Menus.UI.Interfaces.EndLevelMenu.Init()
        public static void Init()
        {
            if (InterfaceManager.inst.CurrentMenu is PauseMenu)
            {
                PauseMenu.Current = null;
            }

            InterfaceManager.inst.CurrentMenu?.Clear();
            InterfaceManager.inst.CurrentMenu = null;

            Current = new EndLevelMenu();
        }

        public static void UnPause()
        {
            if (!CoreHelper.Paused)
                return;

            Current?.Clear();
            Current = null;
            if (InterfaceManager.inst.CurrentMenu is PauseMenu)
                InterfaceManager.inst.CurrentMenu = null;
            AudioManager.inst.CurrentAudioSource.UnPause();
            GameManager.inst.gameState = GameManager.State.Playing;
        }
    }
}
