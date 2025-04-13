using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Configs;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Managers
{
    /// <summary>
    /// Manager class that wraps <see cref="InputDataManager"/> and manages all player related things.
    /// </summary>
    public class PlayerManager : MonoBehaviour
    {
        #region Init

        /// <summary>
        /// The <see cref="PlayerManager"/> global instance reference.
        /// </summary>
        public static PlayerManager inst;

        /// <summary>
        /// Initializes <see cref="PlayerManager"/>.
        /// </summary>
        public static void Init() => Creator.NewGameObject(nameof(PlayerManager), SystemManager.inst.transform).AddComponent<PlayerManager>();

        void Awake()
        {
            inst = this;
        }

        #endregion

        #region Main

        #region Properties

        /// <summary>
        /// Player model custom IDs.
        /// </summary>
        public static List<Setting<string>> PlayerIndexes { get; set; } = new List<Setting<string>>();

        /// <summary>
        /// Wrapped players list.
        /// </summary>
        public static List<CustomPlayer> Players => InputDataManager.inst.players.Select(x => x as CustomPlayer).ToList();

        /// <summary>
        /// If the game is currently in single player.
        /// </summary>
        public static bool IsSingleplayer => InputDataManager.inst.players.Count == 1;

        /// <summary>
        /// If the game has no players loaded.
        /// </summary>
        public static bool NoPlayers => InputDataManager.inst.players == null || InputDataManager.inst.players.IsEmpty();

        /// <summary>
        /// If other players should be considered in the level ranking.
        /// </summary>
        public static bool IncludeOtherPlayersInRank { get; set; }

        public static float AcurracyDivisionAmount { get; set; } = 10f;

        #endregion

        #region Fields

        public static GameObject healthImages;
        public static Transform healthParent;
        public static Sprite healthSprite;

        public static bool allowController;

        #endregion

        #region Methods

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

        /// <summary>
        /// Gets the player closest to a vector.
        /// </summary>
        /// <param name="vector2">Position to check closeness to.</param>
        /// <returns>Returns a CustomPlayer closest to the Vector2 parameter.</returns>
        public static CustomPlayer GetClosestPlayer(Vector2 pos)
        {
            var players = Players;
            var index = GetClosestPlayerIndex(pos);
            return players.InRange(index) ? players[index] : null;
        }

        /// <summary>
        /// Gets the player closest to a vector.
        /// </summary>
        /// <param name="vector2">Position to check closeness to.</param>
        /// <returns>Returns an index of the CustomPlayer closest to the Vector2 parameter.</returns>
        public static int GetClosestPlayerIndex(Vector2 pos)
        {
            var players = Players;
            if (IsSingleplayer)
            {
                var singleplayer = players[0];
                return singleplayer && singleplayer.Player ? 0 : -1;
            }

            if (players.IsEmpty())
                return -1;

            var orderedList = players
                .Where(x => x.Player && x.Player.rb)
                .OrderBy(x => Vector2.Distance(x.Player.rb.position, pos));

            if (orderedList.Count() < 1)
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
                return customPlayer.Player && customPlayer.Player.rb ? customPlayer.Player.rb.transform.position : Vector2.zero;
            }

            if (players.IsEmpty())
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

        /// <summary>
        /// Checks if there are any players loaded and if there are none, adds a default player.
        /// </summary>
        public static void ValidatePlayers()
        {
            if (NoPlayers)
                InputDataManager.inst.players.Add(CreateDefaultPlayer());
        }

        /// <summary>
        /// Creates the default player that uses a keyboard.
        /// </summary>
        /// <returns>Returns a <see cref="CustomPlayer"/> with the default values.</returns>
        public static CustomPlayer CreateDefaultPlayer() => new CustomPlayer(true, 0, null);

        #endregion

        #endregion

        #region Level Difficulties

        /// <summary>
        /// Sets the challenge mode.
        /// </summary>
        /// <param name="mode">Mode to set.</param>
        public static void SetChallengeMode(ChallengeMode mode) => DataManager.inst.UpdateSettingInt("ArcadeDifficulty", (int)mode);

        /// <summary>
        /// Sets the challenge mode.
        /// </summary>
        /// <param name="mode">Mode to set.</param>
        public static void SetChallengeMode(int mode) => DataManager.inst.UpdateSettingInt("ArcadeDifficulty", mode);

        /// <summary>
        /// Challenge mode the level should use.
        /// </summary>
        public static ChallengeMode ChallengeMode => (ChallengeMode)DataManager.inst.GetSettingInt("ArcadeDifficulty", 0);

        /// <summary>
        /// Players take no damage.
        /// </summary>
        public static bool IsZenMode => ChallengeMode == ChallengeMode.ZenMode;
        /// <summary>
        /// Players take damage and can die if health hits zero.
        /// </summary>
        public static bool IsNormal => ChallengeMode == ChallengeMode.Normal;
        /// <summary>
        /// Players take damage and only have 1 life. When they die, restart the level.
        /// </summary>
        public static bool Is1Life => ChallengeMode == ChallengeMode.OneLife;
        /// <summary>
        /// Players take damage and only have 1 health. When they die, restart the level.
        /// </summary>
        public static bool IsNoHit => ChallengeMode == ChallengeMode.OneHit;
        /// <summary>
        /// Players take damage but lose health and don't die.
        /// </summary>
        public static bool IsPractice => ChallengeMode == ChallengeMode.Practice;

        /// <summary>
        /// Names of the challenge modes.
        /// </summary>
        public static string[] ChallengeModeNames => new string[] { "ZEN", "NORMAL", "ONE LIFE", "ONE HIT", "PRACTICE" };

        /// <summary>
        /// If the player is invincible.
        /// </summary>
        public static bool Invincible => CoreHelper.InEditor ? (EditorManager.inst.isEditing || RTPlayer.ZenModeInEditor) : IsZenMode;

        /// <summary>
        /// Customizable game speeds.
        /// </summary>
        public static float[] GameSpeeds => new float[] { 0.1f, 0.5f, 0.8f, 1f, 1.2f, 1.5f, 2f, 3f, };

        /// <summary>
        /// The current game speed index.
        /// </summary>
        public static int ArcadeGameSpeed => DataManager.inst.GetSettingEnum("ArcadeGameSpeed", 2);

        /// <summary>
        /// Sets the current game speed index.
        /// </summary>
        /// <param name="speed">Game speed index.</param>
        public static void SetGameSpeed(int speed) => DataManager.inst.UpdateSettingEnum("ArcadeGameSpeed", speed);

        #endregion

        #region Spawning

        /// <summary>
        /// Spawns all players at a checkpoint.
        /// </summary>
        /// <param name="checkpoint">Checkpoint to spawn at.</param>
        public static void SpawnPlayers(Checkpoint checkpoint)
        {
            AssignPlayerModels();
            var players = Players;

            int randomIndex = UnityEngine.Random.Range(-1, checkpoint.positions.Count);
            var positions = new Vector2[players.Count];

            for (int i = 0; i < players.Count; i++)
            {
                if (checkpoint.positions.IsEmpty())
                {
                    positions[i] = checkpoint.pos;
                    continue;
                }

                positions[i] = checkpoint.spawnType switch
                {
                    Checkpoint.SpawnPositionType.Single => checkpoint.pos,
                    Checkpoint.SpawnPositionType.RandomSingle => randomIndex == -1 ? checkpoint.pos : checkpoint.positions[randomIndex],
                    Checkpoint.SpawnPositionType.Random => checkpoint.positions[UnityEngine.Random.Range(0, checkpoint.positions.Count)],
                    _ => checkpoint.positions[i % checkpoint.positions.Count],
                };
            }

            if (checkpoint.spawnType == Checkpoint.SpawnPositionType.RandomFillAll)
                positions = positions.ToList().Shuffle().ToArray();

            bool spawned = false;
            foreach (var customPlayer in players)
            {
                if (!customPlayer.Player)
                {
                    spawned = true;
                    SpawnPlayer(customPlayer, positions[customPlayer.index]);
                    continue;
                }

                CoreHelper.Log($"Player {customPlayer.index} already exists!");
            }

            if (spawned && RTEventManager.inst && RTEventManager.inst.playersActive && PlayerConfig.Instance.PlaySpawnSound.Value)
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
            foreach (var customPlayer in Players)
            {
                if (!customPlayer.Player)
                {
                    spawned = true;
                    SpawnPlayer(customPlayer, pos);
                    continue;
                }

                CoreHelper.Log($"Player {customPlayer.index} already exists!");
            }

            if (spawned && RTEventManager.inst && RTEventManager.inst.playersActive && PlayerConfig.Instance.PlaySpawnSound.Value)
                SoundManager.inst.PlaySound(DefaultSounds.SpawnPlayer);
        }

        /// <summary>
        /// Spawns a player at a position.
        /// </summary>
        /// <param name="customPlayer">Player to spawn.</param>
        /// <param name="pos">Position to spawn at.</param>
        public static void SpawnPlayer(CustomPlayer customPlayer, Vector3 pos)
        {
            if (customPlayer.PlayerModel && customPlayer.PlayerModel.basePart)
                customPlayer.Health = IsNoHit ? 1 : customPlayer.PlayerModel.basePart.health;

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
            player.Model = customPlayer.PlayerModel;
            player.playerIndex = customPlayer.index;
            player.initialHealthCount = customPlayer.Health;
            customPlayer.Player = player;

            if (GameManager.inst.players.activeSelf)
            {
                player.Spawn();
                player.UpdateModel();
            }
            else
                player.playerNeedsUpdating = true;

            if (customPlayer.device == null)
            {
                var setBoth = (CoreHelper.InEditor || allowController) && IsSingleplayer;
                player.Actions = setBoth ? CoreHelper.CreateWithBothBindings() : InputDataManager.inst.keyboardListener;
                player.isKeyboard = true;
                player.FaceController = setBoth ? FaceController.CreateWithBothBindings() : FaceController.CreateWithKeyboardBindings();
            }
            else
            {
                var myGameActions = MyGameActions.CreateWithJoystickBindings();
                myGameActions.Device = customPlayer.device;
                player.Actions = myGameActions;
                player.isKeyboard = false;

                var faceController = FaceController.CreateWithJoystickBindings();
                faceController.Device = customPlayer.device;
                player.FaceController = faceController;
            }

            player.SetPath(pos);

            if (Is1Life || IsNoHit)
            {
                player.playerDeathEvent += _val =>
                {
                    if (InputDataManager.inst.players.All(x => x is CustomPlayer customPlayer && (customPlayer.Player == null || !customPlayer.Player.Alive)))
                    {
                        RTGameManager.inst.ResetCheckpoint();

                        // todo: implement hits to editor
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
                    if (InputDataManager.inst.players.All(x => x is CustomPlayer customPlayer && (customPlayer.Player == null || !customPlayer.Player.Alive)))
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

            var player = gameObject.GetComponent<RTPlayer>();

            if (!player)
                player = gameObject.AddComponent<RTPlayer>();

            player.Model = playerModel;
            player.playerIndex = index;

            if (transform.gameObject.activeInHierarchy)
            {
                player.Spawn();
                player.UpdateModel();
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

            if (!GameData.Current || GameData.Current.data is not LevelBeatmapData beatmapData || beatmapData.level is not LevelData levelData || levelData.spawnPlayers)
                SpawnPlayers(GameData.Current.data.checkpoints[0]);
        }

        /// <summary>
        /// Destroys all player related game objects.
        /// </summary>
        public static void DestroyPlayers()
        {
            foreach (var player in Players)
            {
                if (!player.Player)
                    continue;

                player.Player.ClearObjects();
                player.Player = null;
            }
        }

        /// <summary>
        /// Destroys a players' game objects.
        /// </summary>
        /// <param name="index">Index of a player to destroy.</param>
        public static void DestroyPlayer(int index)
        {
            var players = Players;
            if (!players.InRange(index))
                return;

            var player = Players[index];
            if (!player.Player)
                return;

            player.Player.ClearObjects();
            player.Player = null;
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
        public static void RespawnPlayer(int index) => RespawnPlayer(index, GetSpawnPosition());

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
            if (player.Player)
                player.Player.ClearObjects();

            player.CurrentModel = PlayersData.Current.GetPlayerModel(index).basePart.id;

            SpawnPlayers(pos);
        }

        // TODO: replace this with Checkpoint.ActiveCheckpoint.
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

        #endregion

        #region Models

        /// <summary>
        /// Path to the global player models folder.
        /// </summary>
        public const string PLAYERS_PATH = "beatmaps/players";

        /// <summary>
        /// Updates all players.
        /// </summary>
        public static void UpdatePlayerModels()
        {
            if (InputDataManager.inst)
                foreach (var player in Players.Where(x => x && x.Player))
                {
                    player.UpdatePlayerModel();
                    player.Player.UpdateModel();
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
    }
}
