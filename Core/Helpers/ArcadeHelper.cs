using System;
using System.Collections;
using System.Windows.Forms;

using UnityEngine;
using UnityEngine.EventSystems;

using LSFunctions;

using SimpleJSON;
using InControl;

using BetterLegacy.Arcade.Interfaces;
using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Menus;

namespace BetterLegacy.Core.Helpers
{
    /// <summary>
    /// Helper class for the game / arcade.
    /// </summary>
    public static class ArcadeHelper
    {
        #region Values

        /// <summary>
        /// If the level has ended.
        /// </summary>
        public static bool endedLevel;

        /// <summary>
        /// If the user has entered the Arcade menu from a level.
        /// </summary>
        public static bool fromLevel;

        /// <summary>
        /// If the song has reached the end.
        /// </summary>
        public static bool SongEnded => CoreHelper.InGame && AudioManager.inst.CurrentAudioSource.time >= GameManager.inst.songLength - (GameData.Current?.data?.level?.LevelEndOffset ?? 0.1f) && (!GameData.Current || !GameData.Current.data || !GameData.Current.data.level || GameData.Current.data.level.autoEndLevel);

        /// <summary>
        /// Replays the level during the End Level Menu.
        /// </summary>
        public static bool ReplayLevel =>
            (!GameData.Current || GameData.Current.data == null || GameData.Current.data.level == null || !GameData.Current.data.level.forceReplayLevelOff) &&
            CoreConfig.Instance.ReplayLevel.Value;

        #endregion

        #region Functions

        /// <summary>
        /// Enters the Input Select menu.
        /// </summary>
        public static void LoadInputSelect()
        {
            LevelManager.Levels.Clear();
            InterfaceManager.inst.CloseMenus();
            SceneHelper.LoadInputSelect();
        }

        /// <summary>
        /// Returns to the loaded hub level.
        /// </summary>
        public static void ReturnToHub()
        {
            if (!LevelManager.Hub)
                return;

            LevelManager.LevelEnded = false;

            InterfaceManager.inst.CloseMenus();
            LevelManager.Play(LevelManager.Hub);
        }

        /// <summary>
        /// Returns to the first collection / queue level.
        /// </summary>
        public static void FirstLevel()
        {
            if (LevelManager.CurrentLevelCollection != null)
            {
                var prevIndex = LevelManager.currentLevelIndex;
                LevelManager.currentLevelIndex = LevelManager.CurrentLevelCollection.EntryLevelIndex;
                if (LevelManager.currentLevelIndex < 0)
                    LevelManager.currentLevelIndex = 0;

                while (LevelManager.currentLevelIndex < LevelManager.CurrentLevelCollection.Count - 1 && LevelManager.CurrentLevelCollection.levelInformation[LevelManager.currentLevelIndex].skip) // skip the level during normal playthrough
                    LevelManager.currentLevelIndex++;

                if (LevelManager.CurrentLevelCollection.Count > 1)
                    LevelManager.CurrentLevel = LevelManager.CurrentLevelCollection[LevelManager.currentLevelIndex];

                CoreHelper.Log($"Update collection index - Prev: {prevIndex} > Current: {LevelManager.currentLevelIndex}");
                LevelManager.LevelEnded = false;

                InterfaceManager.inst.CloseMenus();
                LevelManager.Play(LevelManager.CurrentLevelCollection[LevelManager.currentLevelIndex]);

                return;
            }

            if (!LevelManager.HasQueue)
                return;

            var prev = LevelManager.currentQueueIndex;
            LevelManager.currentQueueIndex = 0;

            CoreHelper.Log($"Update queue - Prev: {prev} > Current: {LevelManager.currentQueueIndex}");

            LevelManager.LevelEnded = false;

            InterfaceManager.inst.CloseMenus();
            LevelManager.Play(LevelManager.NextLevel);
        }

        /// <summary>
        /// Plays the next Arcade level in the current collection / queue.
        /// </summary>
        public static void NextLevel()
        {
            if (LevelManager.CurrentLevelCollection)
            {
                var prevIndex = LevelManager.currentLevelIndex;
                LevelManager.currentLevelIndex++;
                while (LevelManager.currentLevelIndex < LevelManager.CurrentLevelCollection.Count - 1 && LevelManager.CurrentLevelCollection.levelInformation[LevelManager.currentLevelIndex].skip) // skip the level during normal playthrough
                    LevelManager.currentLevelIndex++;

                CoreHelper.Log($"Update collection index - Prev: {prevIndex} > Current: {LevelManager.currentLevelIndex}");

                if (LevelManager.currentLevelIndex >= LevelManager.CurrentLevelCollection.Count)
                    return;

                LevelManager.LevelEnded = false;

                InterfaceManager.inst.CloseMenus();
                var nextLevel = LevelManager.CurrentLevelCollection[LevelManager.currentLevelIndex];
                if (!nextLevel)
                {
                    LevelManager.CurrentLevelCollection.DownloadLevel(LevelManager.CurrentLevelCollection.levelInformation[LevelManager.currentLevelIndex], LevelManager.Play);
                    return;
                }

                LevelManager.Play(nextLevel);

                return;
            }

            // Arcade queue handling
            var prev = LevelManager.currentQueueIndex;
            LevelManager.currentQueueIndex++;

            CoreHelper.Log($"Update queue - Prev: {prev} > Current: {LevelManager.currentQueueIndex}");

            if (LevelManager.IsEndOfQueue)
                return;

            LevelManager.LevelEnded = false;

            InterfaceManager.inst.CloseMenus();
            LevelManager.Play(LevelManager.NextLevel);
        }

        /// <summary>
        /// Restarts the current level.
        /// </summary>
        public static void RestartLevel()
        {
            if (CoreHelper.InEditor || !CoreHelper.InGame)
                return;

            if (endedLevel)
                LevelManager.LevelEnded = false;

            RTBeatmap.Current.hits.Clear();
            RTBeatmap.Current.deaths.Clear();

            PlayerManager.SpawnPlayersOnStart();

            AudioManager.inst.SetMusicTime(GameData.Current.data.level.LevelStartOffset);
            AudioManager.inst.SetPitch(1f);
            RTBeatmap.Current.ResetCheckpoint();
            endedLevel = false;
        }

        /// <summary>
        /// Quits to the Arcade menu.
        /// </summary>
        public static void QuitToArcade()
        {
            InterfaceManager.inst.CloseMenus();

            CoreHelper.Log("Quitting to arcade...");
            LevelManager.Clear();
            ResetModifiedStates();

            LevelManager.LevelEnded = false;
            LevelManager.Hub = null;
            LevelManager.PreviousLevel = null;

            if (CoreHelper.InEditor)
            {
                ArcadeManager.inst.skippedLoad = false;
                ArcadeManager.inst.forcedSkip = false;
                LevelManager.IsArcade = true;
                SceneHelper.LoadInputSelect();
                return;
            }

            if (!LevelManager.IsArcade)
            {
                SceneHelper.LoadScene(SceneName.Main_Menu);
                return;
            }
            SceneHelper.LoadScene(SceneName.Arcade_Select);
        }

        /// <summary>
        /// Quits to the Main menu.
        /// </summary>
        public static void QuitToMainMenu()
        {
            InterfaceManager.inst.CloseMenus();

            CoreHelper.Log("Quitting to main menu...");
            LevelManager.Clear();
            ResetModifiedStates();

            LevelManager.LevelEnded = false;
            LevelManager.Hub = null;
            LevelManager.PreviousLevel = null;

            SceneHelper.LoadScene(SceneName.Main_Menu);
        }

        /// <summary>
        /// Resets the transition and level end states.
        /// </summary>
        public static void ResetModifiedStates()
        {
            LevelManager.ResetTransition();
            RTBeatmap.Current?.ResetEndLevelVariables();
        }

        /// <summary>
        /// Removes old stuff.
        /// </summary>
        public static void DeleteComponents()
        {
            CoreHelper.Destroy(GameObject.Find("Interface"));
            CoreHelper.Destroy(GameObject.Find("EventSystem").GetComponent<InControlInputModule>());
            CoreHelper.Destroy(GameObject.Find("EventSystem").GetComponent<BaseInput>());
            GameObject.Find("EventSystem").AddComponent<StandaloneInputModule>();
            CoreHelper.Destroy(GameObject.Find("Main Camera").GetComponent<InterfaceLoader>());
            CoreHelper.Destroy(GameObject.Find("Main Camera").GetComponent<ArcadeController>());
            CoreHelper.Destroy(GameObject.Find("Main Camera").GetComponent<FlareLayer>());
            CoreHelper.Destroy(GameObject.Find("Main Camera").GetComponent<GUILayer>());
        }

        /// <summary>
        /// Reloads the current Arcade menu.
        /// </summary>
        public static void ReloadMenu()
        {
            DeleteComponents();

            var currentCollection = LevelManager.CurrentLevelCollection;
            if (!currentCollection)
            {
                ArcadeMenu.Init();
                return;
            }

            LevelListMenu.close = () => LevelCollectionMenu.Init(currentCollection);
            LevelListMenu.Init(currentCollection.levels);
        }

        /// <summary>
        /// Copies the current Arcade queue.
        /// </summary>
        public static void CopyArcadeQueue()
        {
            var jn = Parser.NewJSONObject();

            for (int i = 0; i < LevelManager.ArcadeQueue.Count; i++)
            {
                var queue = LevelManager.ArcadeQueue[i];
                jn["queue"][i]["id"] = queue.id;

                if (!queue.metadata)
                    continue;

                if (!string.IsNullOrEmpty(queue.metadata.serverID))
                    jn["queue"][i]["server_id"] = queue.metadata.serverID;

                if (!queue.metadata.beatmap)
                    continue;

                if (queue.metadata.beatmap.workshopID != -1)
                    jn["queue"][i]["workhsop_id"] = queue.metadata.beatmap.workshopID;
                jn["queue"][i]["name"] = queue.metadata.beatmap.name;
            }

            LSText.CopyToClipboard(jn.ToString(3));
        }

        /// <summary>
        /// If the clipboard is in the correct format, pastes the clipboard into an Arcade queue.
        /// </summary>
        public static void PasteArcadeQueue()
        {
            try
            {
                if (!Clipboard.ContainsText())
                    return;

                var text = Clipboard.GetText();

                var jn = JSON.Parse(text);

                if (jn["queue"] == null)
                    return;

                LevelManager.ArcadeQueue.Clear();

                for (int i = 0; i < jn["queue"].Count; i++)
                {
                    var jnQueue = jn["queue"][i];

                    var hasLocal = LevelManager.Levels.TryFindIndex(x => x.id == jnQueue["id"], out int localIndex);
                    var hasSteam = SteamWorkshopManager.inst.Levels.TryFindIndex(x => x.id == jnQueue["id"], out int steamIndex);

                    if ((hasLocal || hasSteam) && !LevelManager.ArcadeQueue.Has(x => x.id == jnQueue["id"]))
                    {
                        var currentLevel = hasSteam ? SteamWorkshopManager.inst.Levels[steamIndex] : LevelManager.Levels[localIndex];

                        LevelManager.ArcadeQueue.Add(currentLevel);
                    }
                    else if (!hasLocal && !hasSteam)
                        CoreHelper.LogError($"Level with ID {jnQueue["id"]} (Name: {jnQueue["name"]}) does not currently exist in your Local folder / Steam subscribed items.\n" +
                            $"Find the level on the server: {jnQueue["server_id"]}\n" +
                            $"or find the level on the Steam Workshop: {jnQueue["workhsop_id"]}");
                }

                if (ArcadeMenu.Current)
                    ArcadeMenu.Current.RefreshQueueLevels(true);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Pasted text was probably not in the correct format.\n{ex}");
            }

        }

        /// <summary>
        /// Function to run when loading ends.
        /// </summary>
        public static IEnumerator OnLoadingEnd()
        {
            yield return CoroutineHelper.Seconds(0.1f);
            SoundManager.inst.PlaySound(DefaultSounds.loadsound);
            ArcadeMenu.Init();
            yield break;
        }

        #endregion
    }
}
