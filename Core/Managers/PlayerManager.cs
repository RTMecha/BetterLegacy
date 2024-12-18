﻿using BepInEx.Configuration;
using BetterLegacy.Components.Player;
using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using LSFunctions;
using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Core.Managers
{
    public class PlayerManager : MonoBehaviour
    {
        public static PlayerManager inst;

        /// <summary>
        /// Inits PlayerManager.
        /// </summary>
        public static void Init() => Creator.NewGameObject(nameof(PlayerManager), SystemManager.inst.transform).AddComponent<PlayerManager>();

        void Awake()
        {
            inst = this;

            for (int i = 0; i < 5; i++)
            {
                PlayerModels.Add(i.ToString(), null);
            }
        }

        public static Dictionary<string, PlayerModel> PlayerModels { get; set; } = new Dictionary<string, PlayerModel>();

        public static Dictionary<int, string> PlayerModelsIndex { get; set; } = new Dictionary<int, string>
        {
            { 0, "0" },
            { 1, "0" },
            { 2, "0" },
            { 3, "0" },
        };

        public static List<Setting<string>> PlayerIndexes { get; set; } = new List<Setting<string>>();

        public static List<CustomPlayer> Players => InputDataManager.inst.players.Select(x => x as CustomPlayer).ToList();

        public static bool IsSingleplayer => Players.Count == 1;

        public static GameObject healthImages;
        public static Transform healthParent;
        public static Sprite healthSprite;

        public static void SetupImages(GameManager __instance)
        {
            var health = __instance.playerGUI.transform.Find("Health");
            health.gameObject.SetActive(true);
            health.GetChild(0).gameObject.SetActive(true);
            for (int i = 1; i < 4; i++)
            {
                Destroy(health.GetChild(i).gameObject);
            }

            for (int i = 3; i < 5; i++)
            {
                Destroy(health.GetChild(0).GetChild(i).gameObject);
            }
            var gm = health.GetChild(0).gameObject;
            healthImages = gm;
            var text = gm.AddComponent<Text>();

            text.alignment = TextAnchor.MiddleCenter;
            text.font = Font.GetDefault();
            text.enabled = false;

            if (gm.transform.Find("Image"))
            {
                healthSprite = gm.transform.Find("Image").GetComponent<Image>().sprite;
            }

            gm.transform.SetParent(null);
            healthParent = health;
        }

        public static bool allowController;

        public static bool IncludeOtherPlayersInRank { get; set; }

        public static float AcurracyDivisionAmount { get; set; } = 10f;

        /// <summary>
        /// Gets the player closest to a vector.
        /// </summary>
        /// <param name="vector2">Position to check closeness to.</param>
        /// <returns>Returns a CustomPlayer closest to the Vector2 parameter.</returns>
        public static CustomPlayer GetClosestPlayer(Vector2 pos)
        {
            var players = Players;
            var index = GetClosestPlayerIndex(pos);
            return index >= 0 && index < players.Count ? players[index] : null;
        }

        public static int GetClosestPlayerIndex(Vector2 pos)
        {
            var players = Players;
            if (IsSingleplayer)
            {
                var singleplayer = players[0];
                return singleplayer && singleplayer.Player ? 0 : -1;
            }

            if (players.Count < 1)
                return -1;

            var orderedList = players
                .Where(x => x.Player && x.Player.rb)
                .OrderBy(x => Vector2.Distance(x.Player.rb.position, pos));

            if (orderedList.Count() < 1)
                return -1;

            var player = orderedList.ElementAt(0);

            return player ? player.index : -1;
        }

        public static Vector2 CenterOfPlayers()
        {
            var players = Players;
            if (IsSingleplayer)
            {
                var customPlayer = players[0];
                return customPlayer.Player && customPlayer.Player.rb ? customPlayer.Player.rb.transform.position : Vector2.zero;
            }

            if (players.Count == 0)
                return Vector2.zero;

            int count = 0;
            var result = Vector2.zero;
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player && player.Player && player.Player.rb)
                {
                    result += player.Player.rb.position;
                    count++;
                }
            }

            return result / count;
        }

        #region Level Difficulties

        public static void SetChallengeMode(int mode) => DataManager.inst.UpdateSettingInt("ArcadeDifficulty", mode);

        public static ChallengeMode ChallengeMode => (ChallengeMode)DataManager.inst.GetSettingInt("ArcadeDifficulty", 0);

        public static bool IsZenMode => ChallengeMode == ChallengeMode.ZenMode;
        public static bool IsNormal => ChallengeMode == ChallengeMode.Normal;
        public static bool Is1Life => ChallengeMode == ChallengeMode.OneLife;
        public static bool IsNoHit => ChallengeMode == ChallengeMode.OneHit;
        public static bool IsPractice => ChallengeMode == ChallengeMode.Practice;

        public static string[] ChallengeModeNames => new string[] { "ZEN", "NORMAL", "ONE LIFE", "ONE HIT", "PRACTICE" };

        public static bool Invincible => CoreHelper.InEditor ? (EditorManager.inst.isEditing || RTPlayer.ZenModeInEditor) : IsZenMode;

        public static float[] GameSpeeds => new float[] { 0.1f, 0.5f, 0.8f, 1f, 1.2f, 1.5f, 2f, 3f, };

        public static int ArcadeGameSpeed => DataManager.inst.GetSettingEnum("ArcadeGameSpeed", 2);

        public static void SetGameSpeed(int speed) => DataManager.inst.UpdateSettingEnum("ArcadeGameSpeed", speed);

        #endregion

        #region Spawning

        public static void SpawnPlayer(CustomPlayer customPlayer, Vector3 pos)
        {
            var gameObject = GameManager.inst.PlayerPrefabs[0].Duplicate(GameManager.inst.players.transform, "Player " + (customPlayer.index + 1));
            gameObject.layer = 8;
            gameObject.SetActive(true);
            Destroy(gameObject.GetComponent<Player>());
            Destroy(gameObject.GetComponentInChildren<PlayerTrail>());

            gameObject.transform.localPosition = new Vector3(0f, 0f, 0f);
            gameObject.transform.Find("Player").localPosition = new Vector3(pos.x, pos.y, 0f);
            gameObject.transform.localRotation = Quaternion.identity;

            var player = gameObject.GetComponent<RTPlayer>();

            if (!player)
                player = gameObject.AddComponent<RTPlayer>();

            player.CustomPlayer = customPlayer;
            player.PlayerModel = customPlayer.PlayerModel;
            player.playerIndex = customPlayer.index;
            customPlayer.Player = player;
            customPlayer.GameObject = player.gameObject;

            if (GameManager.inst.players.activeSelf)
            {
                player.Spawn();
                player.UpdatePlayer();
            }
            else
                player.playerNeedsUpdating = true;

            if (customPlayer.device == null)
            {
                player.Actions = (CoreHelper.InEditor || allowController) && InputDataManager.inst.players.Count == 1 ? CoreHelper.CreateWithBothBindings() : InputDataManager.inst.keyboardListener;
                player.isKeyboard = true;
                player.faceController = (CoreHelper.InEditor || allowController) && InputDataManager.inst.players.Count == 1 ? FaceController.CreateWithBothBindings() : FaceController.CreateWithKeyboardBindings();
            }
            else
            {
                var myGameActions = MyGameActions.CreateWithJoystickBindings();
                myGameActions.Device = customPlayer.device;
                player.Actions = myGameActions;
                player.isKeyboard = false;

                var faceController = FaceController.CreateWithJoystickBindings();
                faceController.Device = customPlayer.device;
                player.faceController = faceController;
            }

            foreach (var path in player.path)
            {
                path.pos = new Vector3(pos.x, pos.y);
                path.lastPos = new Vector3(pos.x, pos.y);

                if (path.transform)
                    path.transform.position = path.pos;
            }
            
            if (Is1Life || IsNoHit)
            {
                player.playerDeathEvent += _val =>
                {
                    if (InputDataManager.inst.players.All(x => x is CustomPlayer customPlayer && (customPlayer.Player == null || !customPlayer.Player.PlayerAlive)))
                    {
                        GameManager.inst.lastCheckpointState = -1;
                        GameManager.inst.ResetCheckpoints();
                        if (!CoreHelper.InEditor)
                        {
                            GameManager.inst.hits.Clear();
                            GameManager.inst.deaths.Clear();
                        }
                        GameManager.inst.gameState = GameManager.State.Reversing;
                    }
                };
            }
            else
            {
                player.playerDeathEvent += _val =>
                {
                    if (InputDataManager.inst.players.All(x => x is CustomPlayer customPlayer && (customPlayer.Player == null || !customPlayer.Player.PlayerAlive)))
                        GameManager.inst.gameState = GameManager.State.Reversing;
                };
            }

            if (IncludeOtherPlayersInRank || player.playerIndex == 0)
            {
                player.playerDeathEvent += _val =>
                {
                    if (!CoreHelper.InEditor)
                        GameManager.inst.deaths.Add(new SaveManager.SaveGroup.Save.PlayerDataPoint(_val, GameManager.inst.UpcomingCheckpointIndex, AudioManager.inst.CurrentAudioSource.time));
                    else
                        AchievementManager.inst.UnlockAchievement("death_hd");
                };

                if (!CoreHelper.InEditor)
                    player.playerHitEvent += (int _health, Vector3 _val) =>
                    {
                        GameManager.inst.hits.Add(new SaveManager.SaveGroup.Save.PlayerDataPoint(_val, GameManager.inst.UpcomingCheckpointIndex, AudioManager.inst.CurrentAudioSource.time));
                    };
            }

            customPlayer.active = true;
        }

        public static GameObject SpawnPlayer(PlayerModel playerModel, Transform transform, int index, Vector3 pos)
        {
            var gameObject = GameManager.inst.PlayerPrefabs[0].Duplicate(transform, "Player");
            gameObject.layer = 8;
            gameObject.SetActive(true);
            Destroy(gameObject.GetComponent<Player>());
            Destroy(gameObject.GetComponentInChildren<PlayerTrail>());

            gameObject.transform.localPosition = new Vector3(0f, 0f, 0f);
            gameObject.transform.Find("Player").localPosition = new Vector3(pos.x, pos.y, 0f);
            gameObject.transform.localRotation = Quaternion.identity;

            var player = gameObject.GetComponent<RTPlayer>();

            if (!player)
                player = gameObject.AddComponent<RTPlayer>();

            player.PlayerModel = playerModel;
            player.playerIndex = index;

            if (transform.gameObject.activeInHierarchy)
            {
                player.Spawn();
                player.UpdatePlayer();
            }
            else
                player.playerNeedsUpdating = true;

            foreach (var path in player.path)
            {
                path.pos = new Vector3(pos.x, pos.y, 0f);
                path.lastPos = new Vector3(pos.x, pos.y, 0f);

                if (path.transform)
                    path.transform.position = path.pos;
            }

            return gameObject;
        }

        public static void RespawnPlayers() => RespawnPlayers(GetSpawnPosition());

        public static void RespawnPlayers(Vector2 pos)
        {
            foreach (var player in Players)
            {
                if (!player.Player)
                    continue;
                DestroyImmediate(player.Player.health);
                DestroyImmediate(player.Player.gameObject);
            }

            AssignPlayerModels();
            GameManager.inst.SpawnPlayers(pos);
        }

        public static void RespawnPlayer(int index) => RespawnPlayer(index, GetSpawnPosition());

        public static void RespawnPlayer(int index, Vector2 pos)
        {
            if (index < 0 || index >= Players.Count)
                return;

            var player = Players[index];
            if (player.Player)
            {
                DestroyImmediate(player.Player.health);
                DestroyImmediate(player.Player.gameObject);
            }

            if (PlayerModelsIndex.TryGetValue(index, out string id) && PlayerModels.ContainsKey(id))
                player.CurrentPlayerModel = id;

            GameManager.inst.SpawnPlayers(pos);
        }

        public static Vector2 GetSpawnPosition()
        {
            var nextIndex = GameData.Current.beatmapData.checkpoints.FindIndex(x => x.time > AudioManager.inst.CurrentAudioSource.time);
            var prevIndex = nextIndex - 1;
            if (prevIndex < 0)
                prevIndex = 0;

            return GameData.IsValid && GameData.Current.beatmapData.checkpoints.Count > prevIndex && GameData.Current.beatmapData.checkpoints[prevIndex] != null ?
                GameData.Current.beatmapData.checkpoints[prevIndex].pos : EventManager.inst.cam.transform.position;
        }

        #endregion

        #region Models

        public const string PLAYERS_PATH = "beatmaps/players";

        public static void SaveLocalModels()
        {
            string location = RTFile.CombinePaths(RTFile.BasePath, Level.PLAYERS_LSB);

            var jn = JSON.Parse("{}");

            for (int i = 0; i < 4; i++)
                jn["indexes"][i] = PlayerModelsIndex[i];

            if (PlayerModels.Count > 5)
                for (int i = 5; i < PlayerModels.Count; i++)
                {
                    var current = PlayerModels.ElementAt(i).Value;
                    jn["models"][i - 5] = current.ToJSON();
                }

            RTFile.WriteToFile(location, jn.ToString());
        }

        public static void LoadLocalModels()
        {
            if (!CoreHelper.InStory && PlayerConfig.Instance.LoadFromGlobalPlayersInArcade.Value && GameData.IsValid && GameData.Current.beatmapData.ModLevelData.allowCustomPlayerModels)
                inst.StartCoroutine(ILoadGlobalModels());
            else
                inst.StartCoroutine(ILoadLocalModels());
        }

        public static IEnumerator ILoadLocalModels()
        {
            if (CoreHelper.InStory && LevelManager.CurrentLevel is Story.StoryLevel storyLevel && !string.IsNullOrEmpty(storyLevel.jsonPlayers))
            {
                LoadPlayerJSON(storyLevel.jsonPlayers);

                yield break;
            }

            string location = RTFile.CombinePaths(RTFile.BasePath, Level.PLAYERS_LSB);
            if (!RTFile.FileExists(location))
            {
                for (int i = 0; i < PlayerModelsIndex.Count; i++)
                    PlayerModelsIndex[i] = CoreHelper.InEditor ? "0" : LevelManager.CurrentLevel != null && LevelManager.CurrentLevel.IsVG ? "4" : "0";
                yield break;
            }

            var json = FileManager.inst.LoadJSONFileRaw(location);
            LoadPlayerJSON(json);
            yield break;
        }

        public static void ClearModels()
        {
            var list = new List<string>();

            foreach (var modelPair in PlayerModels)
                if (!PlayerModel.DefaultModels.Any(x => x.basePart.id == modelPair.Key))
                    list.Add(modelPair.Key);

            foreach (var str in list)
                PlayerModels.Remove(str);

            for (int i = 0; i < GameManager.inst.PlayerPrefabs.Length; i++)
                if (GameManager.inst.PlayerPrefabs[i].name.Contains("Clone"))
                    Destroy(GameManager.inst.PlayerPrefabs[i]);
        }

        static void LoadPlayerJSON(string json)
        {
            ClearModels();

            var jn = JSON.Parse(json);

            for (int i = 0; i < jn["indexes"].Count; i++)
                PlayerModelsIndex[i] = jn["indexes"][i];

            for (int i = 0; i < jn["models"].Count; i++)
            {
                var model = PlayerModel.Parse(jn["models"][i]);
                string id = model.basePart.id;

                if (!PlayerModels.ContainsKey(id))
                    PlayerModels.Add(id, model);
            }

            try
            {
                if (PlayerModelsIndex.Any(x => x.Value != "0") && (!MetaData.IsValid || MetaData.Current.song == null || MetaData.Current.song.difficulty != 6))
                    AchievementManager.inst.UnlockAchievement("costume_party");
            }
            catch (System.Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        public static void CreateNewPlayerModel()
        {
            var model = new PlayerModel(true);
            model.basePart.name = "New Model";
            model.basePart.id = LSText.randomNumString(16);

            PlayerModels[model.basePart.id] = model;
        }

        public static bool SaveGlobalModels()
        {
            bool success = true;
            if (CoreHelper.InEditor)
                EditorManager.inst.DisplayNotification("Saving Player Models...", 1f, EditorManager.NotificationType.Warning);

            foreach (var model in PlayerModels)
            {
                if (!PlayerModel.DefaultModels.Any(x => x.basePart.id == model.Key))
                {
                    try
                    {
                        RTFile.WriteToFile(RTFile.CombinePaths(RTFile.ApplicationDirectory, PLAYERS_PATH, $"{RTFile.FormatLegacyFileName(model.Value.basePart.name)}{FileFormat.LSPL.Dot()}"), model.Value.ToJSON().ToString(3));
                    }
                    catch (System.Exception ex)
                    {
                        success = false;
                        CoreHelper.LogException(ex);
                    }
                }
            }
            if (CoreHelper.InEditor)
                EditorManager.inst.DisplayNotification("Saved Player Models!", 1f, EditorManager.NotificationType.Success);
            return success;
        }

        public static void LoadGlobalModels()
        {
            inst.StartCoroutine(ILoadGlobalModels());
        }

        public static IEnumerator ILoadGlobalModels()
        {
            var fullPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, PLAYERS_PATH);
            RTFile.CreateDirectory(fullPath);

            var files = Directory.GetFiles(fullPath);

            if (files.Length > 0)
            {
                ClearModels();

                foreach (var file in files)
                {
                    if (!Path.GetFileName(file).EndsWith(FileFormat.LSPL.Dot()))
                        continue;

                    var model = PlayerModel.Parse(JSON.Parse(RTFile.ReadFromFile(file)));
                    string id = model.basePart.id;

                    if (id != "0")
                    PlayerModels[id] = model;
                }

                if (CoreHelper.InEditor || (GameData.IsValid && !GameData.Current.beatmapData.ModLevelData.allowCustomPlayerModels) || !PlayerConfig.Instance.LoadFromGlobalPlayersInArcade.Value)
                    LoadIndexes();
                else if (PlayerConfig.Instance.LoadFromGlobalPlayersInArcade.Value)
                    for (int i = 0; i < PlayerModelsIndex.Count; i++)
                        PlayerModelsIndex[i] = PlayerIndexes[i].Value;
            }

            if (GameData.IsValid && !GameData.Current.beatmapData.ModLevelData.allowCustomPlayerModels && PlayerConfig.Instance.LoadFromGlobalPlayersInArcade.Value)
                AssignPlayerModels();

            yield break;
        }

        public static void LoadIndexes()
        {
            string location = RTFile.CombinePaths(RTFile.BasePath, Level.PLAYERS_LSB);

            if (RTFile.FileExists(location))
            {
                var json = FileManager.inst.LoadJSONFileRaw(location);
                var jn = JSON.Parse(json);

                for (int i = 0; i < jn["indexes"].Count; i++)
                {
                    if (PlayerModels.ContainsKey(jn["indexes"][i]))
                    {
                        PlayerModelsIndex[i] = jn["indexes"][i];
                        CoreHelper.Log($"Loaded PlayerModel Index: {jn["indexes"][i]}");
                    }
                    else
                        CoreHelper.LogError($"Failed to load PlayerModel Index: {jn["indexes"][i]}\nPlayer with that ID does not exist");
                }
            }
            else if (!PlayerConfig.Instance.LoadFromGlobalPlayersInArcade.Value || (GameData.IsValid && !GameData.Current.beatmapData.ModLevelData.allowCustomPlayerModels))
            {
                CoreHelper.LogError("player.lsb file does not exist, setting to default player");
                for (int i = 0; i < PlayerModelsIndex.Count; i++)
                    PlayerModelsIndex[i] = CoreHelper.InEditor ? "0" : LevelManager.CurrentLevel != null && LevelManager.CurrentLevel.IsVG ? "4" : "0";
            }

            AssignPlayerModels();
        }

        public static void ClearPlayerModels()
        {
            var list = new List<string>();
            foreach (var keyValue in PlayerModels)
            {
                var key = keyValue.Key;
                if (!PlayerModel.DefaultModels.Any(x => x.basePart.id == key))
                {
                    list.Add(keyValue.Key);
                }
            }

            foreach (var key in list)
            {
                PlayerModels.Remove(key);
            }

            LoadLocalModels();
        }

        public static void SetPlayerModel(int index, string id)
        {
            if (!PlayerModels.ContainsKey(id))
                return;

            PlayerModelsIndex[index] = id;
            if (Players.Count > index && Players[index])
            {
                Players[index].CurrentPlayerModel = id;
                Players[index].Player?.UpdatePlayer();
            }
        }

        public static void DuplicatePlayerModel(string id)
        {
            if (PlayerModels.TryGetValue(id, out PlayerModel orig))
            {
                var model = PlayerModel.DeepCopy(orig);
                model.basePart.name += " Clone";
                model.basePart.id = LSText.randomNumString(16);

                PlayerModels[model.basePart.id] = model;
            }
        }

        public static void UpdatePlayers()
        {
            if (InputDataManager.inst)
                foreach (var player in Players.Where(x => x.Player).Select(x => x.Player))
                {
                    if (CoreHelper.InEditor || IsZenMode)
                        player.UpdatePlayer();
                }
        }

        public static string GetPlayerModelIndex(int index) => PlayerModelsIndex[index];

        public static void SetPlayerModelIndex(int index, int _id)
        {
            string e = PlayerModels.ElementAt(_id).Key;

            PlayerModelsIndex[index] = e;
        }

        public static int GetPlayerModelInt(PlayerModel _model) => PlayerModels.Values.ToList().IndexOf(_model);

        public static void AssignPlayerModels()
        {
            if (Players.Count > 0)
                for (int i = 0; i < Players.Count; i++)
                {
                    if (PlayerModelsIndex.Count > i && PlayerModels.ContainsKey(PlayerModelsIndex[i]) && Players[i] != null)
                    {
                        Players[i].CurrentPlayerModel = PlayerModelsIndex[i];
                        if (Players[i].Player)
                            Players[i].Player.PlayerModel = Players[i].PlayerModel;
                    }
                }
        }

        #endregion
    }
}
