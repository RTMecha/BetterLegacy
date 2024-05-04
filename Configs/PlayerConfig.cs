using BepInEx.Configuration;
using BetterLegacy.Components.Player;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Managers;
using InControl;
using System;

namespace BetterLegacy.Configs
{
    /// <summary>
    /// Player Config for PA Legacy. Based on the CreativePlayers mod.
    /// </summary>
    public class PlayerConfig : BaseConfig
    {
        public static PlayerConfig Instance { get; set; }

        public override ConfigFile Config { get; set; }

        public PlayerConfig(ConfigFile config) : base(config)
        {
            Instance = this;
            Config = config;

            #region General

            TailUpdateMode = Config.Bind("Player - General", "Tail Update Mode", RTPlayer.TailUpdateMode.FixedUpdate, "Changes the way the tail updates movement. FixedUpdate is recommended if the game gets laggy, but otherwise Update / LateUpdate is good for a smooth tail.");

            PlaySoundB = Config.Bind("Player - General", "Play Boost Sound", true, "Plays a little sound when you boost.");
            PlaySoundR = Config.Bind("Player - General", "Play Boost Recover Sound", false, "Plays a little sound when you can boost again.");

            ZenEditorIncludesSolid = Config.Bind("Player - General", "Editor Zen Mode includes Solid", false, "Makes Player ignore solid objects in editor.");

            PlayerShootControl = Config.Bind("Player - General", "Shoot Control", InputControlType.Action3, "Controller button to press to shoot. Requires restart if changed.");
            PlayerShootKey = Config.Bind("Player - General", "Shoot Key", Key.Z, "Keyboard key to press to shoot. Requires restart if changed.");
            PlayerShootSound = Config.Bind("Player - General", "Play Shoot Sound", true, "Plays a little sound when you shoot.");
            AllowPlayersToTakeBulletDamage = Config.Bind("Player - General", "Shots hurt other players", false, "Disable this if you don't want players to kill each other.");
            EvaluateCode = Config.Bind("Player - General", "Evaluate Code", false, ".cs files from the player folder in the level path will run. E.G. boost.cs will run when the player boosts. Each code includes a stored \"playerIndex\" variable in case you want to check which player is performing the action.");

            PlayerNameTags = Config.Bind("Player - General", "Multiplayer NameTags", true, "If enabled and there's more than one person playing, nametags will show which player is which.");

            #endregion

            #region Loading

            AssetsGlobal = Config.Bind("Player - Loading", "Assets Global Source", false, "Assets will use BepInEx/plugins/Assets as the folder instead of the local level folder.");
            LoadFromGlobalPlayersInArcade = Config.Bind("Player - Loading", "Always use global source", false, "Makes the player models always load from beatmaps/players for entering an arcade level. If disabled, players will be loaded from the local players.lspl file.");

            PlayerManager.PlayerIndexes.Add(Config.Bind("Player - Loading", "Player 1 Model", "0", "The player uses this specific model ID."));
            PlayerManager.PlayerIndexes.Add(Config.Bind("Player - Loading", "Player 2 Model", "0", "The player uses this specific model ID."));
            PlayerManager.PlayerIndexes.Add(Config.Bind("Player - Loading", "Player 3 Model", "0", "The player uses this specific model ID."));
            PlayerManager.PlayerIndexes.Add(Config.Bind("Player - Loading", "Player 4 Model", "0", "The player uses this specific model ID."));

            #endregion

            SetConfigs();

            SetupSettingChanged();
        }

        #region General

        /// <summary>
        /// Changes the way the tail updates movement. FixedUpdate is recommended if the game gets laggy, but otherwise Update / LateUpdate is good for a smooth tail.
        /// </summary>
        public ConfigEntry<RTPlayer.TailUpdateMode> TailUpdateMode { get; set; }

        /// <summary>
        /// Plays a little sound when you boost.
        /// </summary>
        public ConfigEntry<bool> PlaySoundB { get; set; }

        /// <summary>
        /// Plays a little sound when you can boost again.
        /// </summary>
        public ConfigEntry<bool> PlaySoundR { get; set; }

        /// <summary>
        /// Makes Player ignore solid objects in editor.
        /// </summary>
        public ConfigEntry<bool> ZenEditorIncludesSolid { get; set; }

        /// <summary>
        /// Controller button to press to shoot. Requires restart if changed.
        /// </summary>
        public ConfigEntry<InputControlType> PlayerShootControl { get; set; }

        /// <summary>
        /// Keyboard key to press to shoot. Requires restart if changed.
        /// </summary>
        public ConfigEntry<Key> PlayerShootKey { get; set; }

        /// <summary>
        /// Plays a little sound when you shoot.
        /// </summary>
        public ConfigEntry<bool> PlayerShootSound { get; set; }

        /// <summary>
        /// Disable this if you don't want players to kill each other.
        /// </summary>
        public ConfigEntry<bool> AllowPlayersToTakeBulletDamage { get; set; }

        /// <summary>
        /// .cs files from the player folder in the level path will run. E.G. boost.cs will run when the player boosts. Each code includes a stored \"playerIndex\" variable in case you want to check which player is performing the action.
        /// </summary>
        public ConfigEntry<bool> EvaluateCode { get; set; }

        /// <summary>
        /// If enabled and there's more than one person playing, nametags will show which player is which.
        /// </summary>
        public ConfigEntry<bool> PlayerNameTags { get; set; }

        #endregion

        #region Loading

        /// <summary>
        /// Assets will use BepInEx/plugins/Assets as the folder instead of the local level folder.
        /// </summary>
        public ConfigEntry<bool> AssetsGlobal { get; set; }

        /// <summary>
        /// Makes the player models always load from beatmaps/players for entering an arcade level. If disabled, players will be loaded from the local players.lspl file.
        /// </summary>
        public ConfigEntry<bool> LoadFromGlobalPlayersInArcade { get; set; }

        #endregion

        public override void SetupSettingChanged()
        {
            Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(UpdateSettings);
        }

        void UpdateSettings(object sender, EventArgs e)
        {
            SetConfigs();
            PlayerManager.UpdatePlayers();
        }

        void SetConfigs()
        {
            RTPlayer.UpdateMode = TailUpdateMode.Value;
            RTPlayer.ShowNameTags = PlayerNameTags.Value;
            RTPlayer.AssetsGlobal = AssetsGlobal.Value;
            RTPlayer.PlayBoostSound = PlaySoundB.Value;
            RTPlayer.PlayBoostRecoverSound = PlaySoundR.Value;
            RTPlayer.ZenEditorIncludesSolid = ZenEditorIncludesSolid.Value;

            FaceController.ShootControl = PlayerShootControl.Value;
            FaceController.ShootKey = PlayerShootKey.Value;

            RTPlayer.PlayShootSound = PlayerShootSound.Value;
            RTPlayer.AllowPlayersToTakeBulletDamage = AllowPlayersToTakeBulletDamage.Value;
            RTPlayer.EvaluateCode = EvaluateCode.Value;
        }
    }
}
