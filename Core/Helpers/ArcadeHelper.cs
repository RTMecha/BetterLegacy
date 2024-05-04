using System.Collections.Generic;
using System.Linq;
using BetterLegacy.Configs;
using BetterLegacy.Core.Managers;
using LSFunctions;

using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Core.Helpers
{
    public static class ArcadeHelper
	{
		public static GameObject buttonPrefab;
		public static bool endedLevel;

		public static void EndOfLevel()
		{
			endedLevel = true;
			var __instance = GameManager.inst;
			GameManager.inst.players.SetActive(false);
			InputDataManager.inst.SetAllControllerRumble(0f);

			__instance.timeline.gameObject.SetActive(false);
			__instance.menuUI.GetComponentInChildren<Image>().enabled = true;
			LSHelpers.ShowCursor();

			var ic = __instance.menuUI.GetComponent<InterfaceController>();

			var metadata = LevelManager.CurrentLevel.metadata;

			if (DataManager.inst.GetSettingBool("IsArcade", false))
			{
				CoreHelper.Log($"Setting Player Data");
				int prevHits = LevelManager.CurrentLevel.playerData != null ? LevelManager.CurrentLevel.playerData.Hits : -1;

				LevelManager.PlayedLevelCount++;

				if (LevelManager.Saves.Where(x => x.Completed).Count() >= 100)
				{
					SteamWrapper.inst.achievements.SetAchievement("GREAT_TESTER");
				}

				if (!PlayerManager.IsZenMode && !PlayerManager.IsPractice)
				{
					if (LevelManager.CurrentLevel.playerData == null)
					{
						LevelManager.CurrentLevel.playerData = new LevelManager.PlayerData
						{
							ID = LevelManager.CurrentLevel.id,
						};
					}

					if (LevelManager.CurrentLevel.playerData.Deaths < __instance.deaths.Count)
						LevelManager.CurrentLevel.playerData.Deaths = __instance.deaths.Count;
					if (LevelManager.CurrentLevel.playerData.Hits < __instance.hits.Count)
						LevelManager.CurrentLevel.playerData.Hits = __instance.hits.Count;
					LevelManager.CurrentLevel.playerData.Completed = true;
					if (LevelManager.CurrentLevel.playerData.Boosts < LevelManager.BoostCount)
						LevelManager.CurrentLevel.playerData.Boosts = LevelManager.BoostCount;

					if (LevelManager.Saves.Has(x => x.ID == LevelManager.CurrentLevel.id))
					{
						var saveIndex = LevelManager.Saves.FindIndex(x => x.ID == LevelManager.CurrentLevel.id);
						LevelManager.Saves[saveIndex] = LevelManager.CurrentLevel.playerData;
					}
					else
						LevelManager.Saves.Add(LevelManager.CurrentLevel.playerData);
				}

				LevelManager.SaveProgress();

				CoreHelper.Log($"Setting More Info");
				//More Info
				{
					var moreInfo = ic.interfaceBranches.Find(x => x.name == "end_of_level_more_info");
					moreInfo.elements[5] = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "You died a total of " + __instance.deaths.Count + " times.", "end_of_level_more_info");
					moreInfo.elements[6] = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "You got hit a total of " + __instance.hits.Count + " times.", "end_of_level_more_info");
					moreInfo.elements[7] = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "You boosted a total of " + LevelManager.BoostCount + " times.", "end_of_level_more_info");
					moreInfo.elements[8] = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "Total song time: " + AudioManager.inst.CurrentAudioSource.clip.length, "end_of_level_more_info");
					moreInfo.elements[9] = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, "Time in level: " + LevelManager.timeInLevel, "end_of_level_more_info");
				}

				int endOfLevelIndex = ic.interfaceBranches.FindIndex(x => x.name == "end_of_level");
				int getSongIndex = ic.interfaceBranches.FindIndex(x => x.name == "getsong");

				int line = 5;
				int dataPointMax = 24;
				int[] hitsNormalized = new int[dataPointMax + 1];
				foreach (var playerDataPoint in __instance.hits)
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

				CoreHelper.Log($"Setting Achievements");
				if (levelRank.name == "SS")
					SteamWrapper.inst.achievements.SetAchievement("SS_RANK");
				else if (levelRank.name == "F")
					SteamWrapper.inst.achievements.SetAchievement("F_RANK");

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

						if (prevHits == -1)
						{
							text += string.Format("       <voffset=0em><size=300%><color=#{0}><b>{1}</b></color>", LSColors.ColorToHex(levelRank.color), levelRank.name);
						}
						else if (prevHits > __instance.hits.Count && newLevelRank != null)
						{
							text += string.Format("       <voffset=0em><size=300%><color=#{0}><b>{1}</b></color><size=150%> <voffset=0.325em><b>-></b> <voffset=0em><size=300%><color=#{2}><b>{3}</b></color>", new object[]
							{
								LSColors.ColorToHex(newLevelRank.color),
								newLevelRank.name,
								LSColors.ColorToHex(levelRank.color),
								levelRank.name
							});
						}
						else
						{
							text += string.Format("       <voffset=0em><size=300%><color=#{0}><b>{1}</b></color>", LSColors.ColorToHex(levelRank.color), levelRank.name);
						}
					}

					if (line == 7)
					{
						text = "<voffset=0.6em>" + text;

						text += $"       <voffset=0em><size=300%><color=#{LSColors.ColorToHex(levelRank.color)}><b>{LevelManager.CalculateAccuracy(__instance.hits.Count, AudioManager.inst.CurrentAudioSource.clip.length)}%</b></color>";
					}

					if (line >= 9 && sayings.Count > line - 9)
					{
						text = text + "       <alpha=#ff>" + sayings[line - 9];
					}

					var interfaceElement = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, text);
					interfaceElement.branch = "end_of_level";
					ic.interfaceBranches[endOfLevelIndex].elements[line] = interfaceElement;
					line++;
				}
				var levelSummary = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Text, string.Format("Level Summary - <b>{0}</b> by {1}", metadata.song.title, metadata.artist.Name));
				levelSummary.branch = "end_of_level";
				ic.interfaceBranches[endOfLevelIndex].elements[2] = levelSummary;

				InterfaceController.InterfaceElement buttons = null;
				LevelManager.current += 1;
				if (LevelManager.ArcadeQueue.Count > 1 && LevelManager.current < LevelManager.ArcadeQueue.Count)
				{
					CoreHelper.Log($"Selecting next Arcade level in queue [{LevelManager.current + 1} / {LevelManager.ArcadeQueue.Count}]");
					LevelManager.CurrentLevel = LevelManager.ArcadeQueue[LevelManager.current];
					buttons = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Buttons, (metadata.artist.getUrl() != null) ? "[NEXT]:next&&[TO ARCADE]:toarcade&&[REPLAY]:replay&&[MORE INFO]:end_of_level_more_info&&[GET SONG]:getsong" : "[NEXT]:next&&[TO ARCADE]:toarcade&&[REPLAY]:replay&&[MORE INFO]:end_of_level_more_info");
				}
				else
				{
					buttons = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Buttons, (metadata.artist.getUrl() != null) ? "[TO ARCADE]:toarcade&&[REPLAY]:replay&&[MORE INFO]:end_of_level_more_info&&[GET SONG]:getsong" : "[TO ARCADE]:toarcade&&[REPLAY]:replay&&[MORE INFO]:end_of_level_more_info");
				}

				buttons.settings.Add("alignment", "center");
				buttons.settings.Add("orientation", "grid");
				buttons.settings.Add("width", "1");
				buttons.settings.Add("grid_h", "5");
				buttons.settings.Add("grid_v", "1");
				buttons.branch = "end_of_level";
				ic.interfaceBranches[endOfLevelIndex].elements[17] = buttons;
				var openLink = new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Event, "openlink::" + metadata.artist.getUrl());
				openLink.branch = "getsong";
				ic.interfaceBranches[getSongIndex].elements[0] = openLink;

				var interfaceBranch = new InterfaceController.InterfaceBranch("next");
				interfaceBranch.elements.Add(new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Event, "loadscene::Game::true", "next"));
				ic.interfaceBranches.Add(interfaceBranch);

				var interfaceBranch2 = new InterfaceController.InterfaceBranch("replay");
				interfaceBranch2.elements.Add(new InterfaceController.InterfaceElement(InterfaceController.InterfaceElement.Type.Event, "restartlevel", "replay"));
				ic.interfaceBranches.Add(interfaceBranch2);
			}
			ic.SwitchBranch("end_of_level");
		}
	}
}
