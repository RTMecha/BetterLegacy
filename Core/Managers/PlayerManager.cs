using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using InControl;

using BetterLegacy.Configs;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Runtime;

namespace BetterLegacy.Core.Managers
{
    /// <summary>
    /// Manages player runtime, player models, etc.
    /// <br></br>Wraps <see cref="InputDataManager"/>.
    /// </summary>
    public class PlayerManager : BaseManager<PlayerManager, PlayerManagerSettings>
    {
        #region Values

        /// <summary>
        /// Player model custom IDs.
        /// </summary>
        public static List<Setting<string>> PlayerIndexes { get; set; } = new List<Setting<string>>();

        /// <summary>
        /// Wrapped players list.
        /// </summary>
        public static List<PAPlayer> Players { get; set; } = new List<PAPlayer>();

        /// <summary>
        /// If the game is currently in single player.
        /// </summary>
        public static bool IsSingleplayer => Players.Count == 1;

        /// <summary>
        /// If the game has no players loaded.
        /// </summary>
        public static bool NoPlayers => Players == null || Players.IsEmpty();

        /// <summary>
        /// If any of the players are not the modded versions.
        /// </summary>
        public static bool InvalidPlayers => NoPlayers || Players.Any(x => x is not PAPlayer);

        /// <summary>
        /// If other players should be considered in the level ranking.
        /// </summary>
        public static bool IncludeOtherPlayersInRank { get; set; }

        public static Vector2 ControllerRumble { get; set; }

        public static GameObject healthImages;
        public static Transform healthParent;
        public static Sprite healthSprite;

        /// <summary>
        /// Path to the global player models folder.
        /// </summary>
        public const string PLAYERS_PATH = "beatmaps/players";

        #endregion

        #region Functions

        public override void OnInit()
        {
            // destroy players on pre load scene
            SceneHelper.OnPreLoadScene += scene => DestroyPlayers();
        }

        public override void OnTick()
        {
            foreach (var player in Players)
            {
                var shake = !CoreConfig.Instance.ControllerRumble.Value ? Vector2.zero : ControllerRumble + player.rumble;
                player.device?.Vibrate(Mathf.Clamp(shake.x, 0f, 0.5f), Mathf.Clamp(shake.y, 0f, 0.5f));
            }
        }

        public static void SetupImages(GameManager __instance)
        {
            var health = __instance.playerGUI.transform.Find("Health");
            health.gameObject.SetActive(true);
            health.GetChild(0).gameObject.SetActive(true);
            for (int i = 1; i < 4; i++)
                Destroy(health.GetChild(i).gameObject);

            for (int i = 3; i < 5; i++)
                Destroy(health.GetChild(0).GetChild(i).gameObject);
            
            var gm = health.GetChild(0).gameObject;
            healthImages = gm;
            var text = gm.AddComponent<Text>();

            text.alignment = TextAnchor.MiddleCenter;
            text.font = Font.GetDefault();
            text.enabled = false;

            if (gm.transform.Find("Image"))
                healthSprite = gm.transform.Find("Image").GetComponent<Image>().sprite;

            gm.transform.SetParent(null);
            healthParent = health;
        }

        #region Player Search

        /// <summary>
        /// Gets the player with a specified priority.
        /// </summary>
        /// <param name="priority">Priority to search.</param>
        /// <param name="pos">Position vector for position-based priorities.</param>
        /// <param name="index">Index of the player for index-based priorities.</param>
        /// <returns>Returns the found <see cref="PAPlayer"/>.</returns>
        public static PAPlayer GetPlayer(HomingPriority priority, Vector2 pos, int index) => Players.GetAtOrDefault(GetPlayerIndex(priority, pos, index), null);

        /// <summary>
        /// Gets the player with a specified priority.
        /// </summary>
        /// <param name="priority">Priority to search.</param>
        /// <param name="pos">Position vector for position-based priorities.</param>
        /// <param name="index">Index of the player for index-based priorities.</param>
        /// <returns>Returns the index of the found <see cref="PAPlayer"/>.</returns>
        public static int GetPlayerIndex(HomingPriority priority, Vector2 pos, int index) => priority switch
        {
            HomingPriority.Closest => GetClosestPlayerIndex(pos),
            HomingPriority.Furthest => GetFurthestPlayerIndex(pos),
            HomingPriority.Index => index,
            HomingPriority.HighestHealth => GetHighestHealthPlayerIndex(),
            HomingPriority.LowestHealth => GetLowestHealthPlayerIndex(),
            _ => -1,
        };

        /// <summary>
        /// Gets the player closest to a vector.
        /// </summary>
        /// <param name="pos">Position to check closeness to.</param>
        /// <returns>Returns a <see cref="PAPlayer"/> closest to the Vector2 parameter.</returns>
        public static PAPlayer GetClosestPlayer(Vector2 pos) => Players.GetAtOrDefault(GetClosestPlayerIndex(pos), null);

        /// <summary>
        /// Gets the player closest to a vector.
        /// </summary>
        /// <param name="pos">Position to check closeness to.</param>
        /// <returns>Returns an index of the <see cref="PAPlayer"/> closest to the Vector2 parameter.</returns>
        public static int GetClosestPlayerIndex(Vector2 pos)
        {
            var players = Players;
            if (IsSingleplayer)
            {
                var singleplayer = players[0];
                return singleplayer && singleplayer.RuntimePlayer ? 0 : -1;
            }

            if (players.IsEmpty())
                return -1;

            var orderedList = players
                .Where(x => x.RuntimePlayer && x.RuntimePlayer.rb)
                .OrderBy(x => Vector2.Distance(x.RuntimePlayer.rb.position, pos));
            if (orderedList.IsEmpty())
                return -1;

            var player = orderedList.ElementAt(0);
            return player ? player.index : -1;
        }

        /// <summary>
        /// Gets the player furthest from a vector.
        /// </summary>
        /// <param name="pos">Position to check furthness from.</param>
        /// <returns>Returns a <see cref="PAPlayer"/> furthest from the Vector2 parameter.</returns>
        public static PAPlayer GetFurthestPlayer(Vector2 pos) => Players.GetAtOrDefault(GetFurthestPlayerIndex(pos), null);

        /// <summary>
        /// Gets the player furthest from a vector.
        /// </summary>
        /// <param name="pos">Position to check furthness from.</param>
        /// <returns>Returns an index of the <see cref="PAPlayer"/> furthest to the Vector2 parameter.</returns>
        public static int GetFurthestPlayerIndex(Vector2 pos)
        {
            var players = Players;
            if (IsSingleplayer)
            {
                var singlePlayer = players[0];
                return singlePlayer && singlePlayer.RuntimePlayer ? 0 : -1;
            }

            if (players.IsEmpty())
                return -1;

            var orderedList = players
                .Where(x => x.RuntimePlayer && x.RuntimePlayer.rb)
                .OrderByDescending(x => Vector2.Distance(x.RuntimePlayer.rb.position, pos));
            if (orderedList.IsEmpty())
                return -1;

            var player = orderedList.ElementAt(0);
            return player ? player.index : -1;
        }

        /// <summary>
        /// Gets the player with the highest amount of health.
        /// </summary>
        /// <returns>Returns a <see cref="PAPlayer"/> with the highest amount of health.</returns>
        public static PAPlayer GetHighestHealthPlayer() => Players.GetAtOrDefault(GetHighestHealthPlayerIndex(), null);

        /// <summary>
        /// Gets the player with the highest amount of health.
        /// </summary>
        /// <returns>Returns an index of the <see cref="PAPlayer"/> with the highest amount of health.</returns>
        public static int GetHighestHealthPlayerIndex()
        {
            var players = Players;
            if (IsSingleplayer)
            {
                var singlePlayer = players[0];
                return singlePlayer && singlePlayer.RuntimePlayer ? 0 : -1;
            }

            if (players.IsEmpty())
                return -1;

            var orderedList = players
                .OrderByDescending(x => x.Health);
            if (orderedList.IsEmpty())
                return -1;

            var player = orderedList.ElementAt(0);
            return player ? player.index : -1;
        }

        /// <summary>
        /// Gets the player with the highest amount of health.
        /// </summary>
        /// <returns>Returns a <see cref="PAPlayer"/> with the highest amount of health.</returns>
        public static PAPlayer GetLowestHealthPlayer() => Players.GetAtOrDefault(GetLowestHealthPlayerIndex(), null);

        /// <summary>
        /// Gets the player with the lowest amount of health.
        /// </summary>
        /// <returns>Returns an index of the <see cref="PAPlayer"/> with the lowest amount of health.</returns>
        public static int GetLowestHealthPlayerIndex()
        {
            var players = Players;
            if (IsSingleplayer)
            {
                var singlePlayer = players[0];
                return singlePlayer && singlePlayer.RuntimePlayer ? 0 : -1;
            }

            if (players.IsEmpty())
                return -1;

            var orderedList = players
                .OrderBy(x => x.Health);
            if (orderedList.IsEmpty())
                return -1;

            var player = orderedList.ElementAt(0);
            return player ? player.index : -1;
        }

        /// <summary>
        /// Calculates the center of all players positions.
        /// </summary>
        /// <returns>Returns a center vector of all players.</returns>
        public static Vector2 CenterOfPlayers()
        {
            var players = Players;
            if (IsSingleplayer)
            {
                var customPlayer = players[0];
                return customPlayer.RuntimePlayer && customPlayer.RuntimePlayer.rb ? customPlayer.RuntimePlayer.rb.transform.position : Vector2.zero;
            }

            if (players.IsEmpty())
                return Vector2.zero;

            int count = 0;
            var result = Vector2.zero;
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player && player.RuntimePlayer && player.RuntimePlayer.rb)
                {
                    result += player.RuntimePlayer.rb.position;
                    count++;
                }
            }

            return result / count;
        }

        #endregion

        #region Validation / Connection

        /// <summary>
        /// Checks if there are any players loaded and if there are none, adds a default player.
        /// </summary>
        public static void ValidatePlayers()
        {
            var invalid = InvalidPlayers;

            if (!invalid)
                return;

            Players.Clear();
            Players.Add(CreateDefaultPlayer());
        }

        /// <summary>
        /// Creates the default player that uses a keyboard.
        /// </summary>
        /// <returns>Returns a <see cref="PAPlayer"/> with the default values.</returns>
        public static PAPlayer CreateDefaultPlayer() => new PAPlayer(true, 0, null);

        public static PAPlayer FindPlayerUsingDevice(InputDevice inputDevice) => Players.Find(x => x.device == inputDevice);

        public static bool DeviceNotConnected(InputDevice inputDevice) => !Players.Has(x => x.device == inputDevice);

        public static PAPlayer FindPlayerUsingKeyboard() => Players.Find(x => x.deviceName == "keyboard" || x.deviceType == ControllerType.Keyboard);

        public static bool KeyboardNotConnected() => !Players.Has(x => x.deviceName == "keyboard" || x.deviceType == ControllerType.Keyboard);

        public static void RemovePlayer(PAPlayer player)
        {
            if (player.RuntimePlayer)
                CoreHelper.Delete(player.RuntimePlayer);
            player.Input = null;
            player.RuntimePlayer = null;
            Players.Remove(player);
        }

        #endregion
        
        #region Spawning

        /// <summary>
        /// Spawns all players at a checkpoint.
        /// </summary>
        /// <param name="checkpoint">Checkpoint to spawn at.</param>
        public static void SpawnPlayers(Checkpoint checkpoint)
        {
            AssignPlayerModels();
            var positions = GetSpawnPositions(checkpoint);
            bool spawned = false;
            foreach (var player in Players)
            {
                // no lives? too bad.
                if (player.OutOfLives)
                {
                    CoreHelper.Log($"Player {player.index} is out of lives.");
                    continue;
                }

                if (!player.RuntimePlayer)
                {
                    spawned = true;
                    SpawnPlayer(player, positions[player.index]);
                    continue;
                }

                CoreHelper.Log($"Player {player.index} already exists!");
            }

            if (spawned && RTLevel.Current && RTLevel.Current.eventEngine && RTLevel.Current.eventEngine.playersActive && PlayerConfig.Instance.PlaySpawnSound.Value)
                SoundManager.inst.PlaySound(DefaultSounds.SpawnPlayer);
        }

        /// <summary>
        /// Spawns all players at a position.
        /// </summary>
        /// <param name="pos">Position to spawn at.</param>
        public static void SpawnPlayers(Vector2 pos)
        {
            AssignPlayerModels();

            bool spawned = false;
            foreach (var player in Players)
            {
                // no lives? too bad.
                if (player.OutOfLives)
                {
                    CoreHelper.Log($"Player {player.index} is out of lives.");
                    continue;
                }

                if (!player.RuntimePlayer)
                {
                    spawned = true;
                    SpawnPlayer(player, pos);
                    continue;
                }

                CoreHelper.Log($"Player {player.index} already exists!");
            }

            if (spawned && RTLevel.Current && RTLevel.Current.eventEngine && RTLevel.Current.eventEngine.playersActive && PlayerConfig.Instance.PlaySpawnSound.Value)
                SoundManager.inst.PlaySound(DefaultSounds.SpawnPlayer);
        }

        /// <summary>
        /// Spawns a player at a position.
        /// </summary>
        /// <param name="customPlayer">Player to spawn.</param>
        /// <param name="pos">Position to spawn at.</param>
        public static void SpawnPlayer(PAPlayer player, Vector3 pos)
        {
            player.ResetHealth();

            var gameObject = GameManager.inst.PlayerPrefabs[0].Duplicate(GameManager.inst.players.transform, "Player " + (player.index + 1));
            gameObject.layer = 8;
            gameObject.SetActive(true);
            Destroy(gameObject.GetComponent<Player>());
            Destroy(gameObject.GetComponentInChildren<PlayerTrail>());

            gameObject.transform.localPosition = new Vector3(0f, 0f, 0f);
            gameObject.transform.Find("Player").localPosition = new Vector3(pos.x, pos.y, 0f);
            gameObject.transform.localRotation = Quaternion.identity;

            var runtimePlayer = gameObject.GetOrAddComponent<RTPlayer>();

            runtimePlayer.Core = player;
            runtimePlayer.Model = player.PlayerModel;
            runtimePlayer.playerIndex = player.index;
            runtimePlayer.initialHealthCount = player.Health;
            player.RuntimePlayer = runtimePlayer;

            runtimePlayer.Init();
            runtimePlayer.UpdateTail(player.Health, pos);

            if (GameManager.inst.players.activeSelf)
            {
                runtimePlayer.UpdateModel();
                runtimePlayer.Spawn();
            }
            else
                runtimePlayer.playerNeedsUpdating = true;

            player.SetupInput();

            runtimePlayer.SetPath(pos);

            runtimePlayer.playerDeathEvent += _val =>
            {
                player.lives--;

                if (RTBeatmap.Current.respawnImmediately && !player.OutOfLives)
                {
                    var positions = GetSpawnPositions(RTBeatmap.Current.ActiveCheckpoint);
                    SpawnPlayer(player, positions[player.index]);
                    return;
                }

                if (Players.All(x => x is PAPlayer player && (!player.Alive || !player.RuntimePlayer.Alive)))
                {
                    if (RTBeatmap.Current.challengeMode.Lives > 0)
                        RTBeatmap.Current.lives--;

                    if (RTBeatmap.Current.OutOfLives) // reset checkpoint and deaths when out of lives
                    {
                        RTBeatmap.Current.ResetCheckpoint();
                        RTBeatmap.Current.Reset(false);
                    }

                    GameManager.inst.gameState = GameManager.State.Reversing;
                }
            };

            if (IncludeOtherPlayersInRank || runtimePlayer.playerIndex == 0)
            {
                runtimePlayer.playerDeathEvent += _val =>
                {
                    if (!CoreHelper.InEditor)
                        RTBeatmap.Current.deaths.Add(new PlayerDataPoint(_val));
                    else
                        AchievementManager.inst.UnlockAchievement("death_hd");
                };

                if (!CoreHelper.InEditor)
                    runtimePlayer.playerHitEvent += (int _health, Vector3 _val) =>
                    {
                        RTBeatmap.Current.hits.Add(new PlayerDataPoint(_val));
                    };
            }

            player.active = true;
        }

        /// <summary>
        /// Spawns a player object with a custom model, parent and index at a position.
        /// </summary>
        /// <param name="playerModel">Model to assign to the player.</param>
        /// <param name="transform">Parent.</param>
        /// <param name="index">Index of the player.</param>
        /// <param name="pos">Position to spawn at.</param>
        /// <returns>Returns the spawned players' game object.</returns>
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

            var player = gameObject.GetOrAddComponent<RTPlayer>();

            player.Model = playerModel;
            player.playerIndex = index;

            player.Init();

            if (transform.gameObject.activeInHierarchy)
            {
                player.UpdateModel();
                player.Spawn();
            }
            else
                player.playerNeedsUpdating = true;

            player.SetPath(pos);

            return gameObject;
        }

        /// <summary>
        /// Spawns all players at the start of the level. If <see cref="LevelData.spawnPlayers"/> is off, then don't spawn players.
        /// </summary>
        public static void SpawnPlayersOnStart()
        {
            ValidatePlayers();
            DestroyPlayers();

            for (int i = 0; i < Players.Count; i++)
            {
                var player = Players[i];
                player.lives = RTBeatmap.Current.challengeMode.Lives > 0 ? RTBeatmap.Current.challengeMode.Lives : player.GetControl()?.lives ?? -1;
            }

            if (GameData.Current && GameData.Current.data && (!GameData.Current.data.level || GameData.Current.data.level.spawnPlayers))
                SpawnPlayers(GameData.Current.data.checkpoints[0]);
        }

        /// <summary>
        /// Destroys all player related game objects.
        /// </summary>
        public static void DestroyPlayers()
        {
            foreach (var player in Players)
                DestroyPlayer(player);
        }

        /// <summary>
        /// Destroys a players' game objects.
        /// </summary>
        /// <param name="index">Index of a player to destroy.</param>
        public static void DestroyPlayer(int index)
        {
            var players = Players;
            if (players.TryGetAt(index, out PAPlayer player))
                DestroyPlayer(player);
        }

        /// <summary>
        /// Destroys a players' game objects.
        /// </summary>
        /// <param name="player">Player to destroy.</param>
        public static void DestroyPlayer(PAPlayer player)
        {
            if (!player)
                return;

            if (player.RuntimePlayer)
                player.RuntimePlayer.Clear();
            player.RuntimePlayer = null;
        }

        /// <summary>
        /// Respawns all players at the default spawn position.
        /// </summary>
        public static void RespawnPlayers() => RespawnPlayers(GetSpawnPosition());

        /// <summary>
        /// Respawns all players at a set spawn position.
        /// </summary>
        public static void RespawnPlayers(Vector2 pos)
        {
            DestroyPlayers();
            AssignPlayerModels();
            SpawnPlayers(pos);
        }

        /// <summary>
        /// Respawns a specific player at the default spawn position.
        /// </summary>
        /// <param name="index">Index of the player to respawn.</param>
        public static void RespawnPlayer(int index) => RespawnPlayer(index, GetSpawnPositions(RTBeatmap.Current.ActiveCheckpoint)[index]);

        /// <summary>
        /// Respawns a specific player at a set spawn position.
        /// </summary>
        /// <param name="index">Index of the player to respawn.</param>
        /// <param name="pos">Position to spawn at.</param>
        public static void RespawnPlayer(int index, Vector2 pos)
        {
            var players = Players;
            if (!players.InRange(index))
                return;

            var player = Players[index];
            player.RuntimePlayer?.Clear();
            player.CurrentModel = PlayersData.Current.GetPlayerModel(index).basePart.id;

            SpawnPlayers(pos);
        }

        /// <summary>
        /// Gets the last active checkpoint and its position.
        /// </summary>
        /// <returns>Returns the last active checkpoint position.</returns>
        public static Vector2 GetSpawnPosition()
        {
            var nextIndex = GameData.Current.data.checkpoints.FindIndex(x => x.time > AudioManager.inst.CurrentAudioSource.time);
            var prevIndex = nextIndex - 1;
            if (prevIndex < 0)
                prevIndex = 0;

            return GameData.Current && GameData.Current.data.checkpoints.InRange(prevIndex) && GameData.Current.data.checkpoints[prevIndex] != null ?
                GameData.Current.data.checkpoints[prevIndex].pos : EventManager.inst.cam.transform.position;
        }

        /// <summary>
        /// Gets the checkpoint spawn positions.
        /// </summary>
        /// <param name="checkpoint">Checkpoint to get the position of.</param>
        /// <returns>Returns the checkpoint position.</returns>
        public static Vector2[] GetSpawnPositions(Checkpoint checkpoint)
        {
            var players = Players;
            //var hash = RandomHelper.GetHash(checkpoint.id ?? string.Empty, AudioManager.inst.CurrentAudioSource.time.ToString(), RandomHelper.CurrentSeed);
            //int randomIndex = !checkpoint ? -1 : RandomHelper.IntFromRange(hash, -1, checkpoint.positions.Count - 1);
            var randomIndex = !checkpoint ? -1 : UnityRandom.Range(-1, checkpoint.positions.Count);
            var positions = new Vector2[players.Count];

            for (int i = 0; i < players.Count; i++)
            {
                if (!checkpoint)
                {
                    positions[i] = Vector2.zero;
                    continue;
                }

                if (checkpoint.positions.IsEmpty())
                {
                    positions[i] = checkpoint.pos;
                    continue;
                }

                positions[i] = checkpoint.spawnType switch
                {
                    Checkpoint.SpawnPositionType.Single => checkpoint.pos,
                    Checkpoint.SpawnPositionType.RandomSingle => checkpoint.GetPosition(randomIndex),
                    //Checkpoint.SpawnPositionType.Random => checkpoint.GetPosition(RandomHelper.IntFromRange(RandomHelper.GetHash(checkpoint.id ?? string.Empty, (AudioManager.inst.CurrentAudioSource.time * i).ToString(), RandomHelper.CurrentSeed), -1, checkpoint.positions.Count - 1)),
                    Checkpoint.SpawnPositionType.Random => checkpoint.GetPosition(UnityRandom.Range(-1, checkpoint.positions.Count)),
                    _ => checkpoint.positions[i % checkpoint.positions.Count],
                };
            }

            if (checkpoint && checkpoint.spawnType == Checkpoint.SpawnPositionType.RandomFillAll)
                positions = positions.ToList().OrderBy(x => RandomHelper.GetHash(x.x, x.y, AudioManager.inst.CurrentAudioSource.time.ToString(), RandomHelper.CurrentSeed)).ToArray();

            return positions;
        }

        #endregion

        #region Models

        /// <summary>
        /// Updates all players.
        /// </summary>
        public static void UpdatePlayerModels()
        {
            foreach (var player in Players)
            {
                player.UpdatePlayerModel();
                player.RuntimePlayer?.UpdateModel();
            }
        }

        public static void AssignPlayerModels()
        {
            var players = Players;
            if (!players.IsEmpty())
                for (int i = 0; i < players.Count; i++)
                    players[i].CurrentModel = PlayersData.Current.GetPlayerModel(i).basePart.id;
        }

        #endregion

        #endregion
    }
}
