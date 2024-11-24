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

        public PlayerConfig() : base(nameof(PlayerConfig)) // Set config name via base("")
        {
            Instance = this;
            BindSettings();
            SetConfigs();
            SetupSettingChanged();
        }

        #region Settings

        #region General

        /// <summary>
        /// Changes the way the tail updates movement. FixedUpdate is recommended if the game gets laggy, but otherwise Update / LateUpdate is good for a smooth tail.
        /// </summary>
        public Setting<RTPlayer.TailUpdateMode> TailUpdateMode { get; set; }

        /// <summary>
        /// If boosting should be queued when you press it during boost cooldown.
        /// </summary>
        public Setting<bool> QueueBoost { get; set; }
        
        /// <summary>
        /// Plays a little sound when you boost.
        /// </summary>
        public Setting<bool> PlaySoundB { get; set; }

        /// <summary>
        /// Plays a little sound when you can boost again.
        /// </summary>
        public Setting<bool> PlaySoundR { get; set; }

        /// <summary>
        /// If the boost recovery sound should only play if the player has the boost tail.
        /// </summary>
        public Setting<bool> PlaySoundRBoostTail { get; set; }

        /// <summary>
        /// Makes Player ignore solid objects in editor.
        /// </summary>
        public Setting<bool> ZenEditorIncludesSolid { get; set; }

        /// <summary>
        /// Plays a little sound when you shoot.
        /// </summary>
        public Setting<bool> PlayerShootSound { get; set; }

        /// <summary>
        /// Disable this if you don't want players to kill each other.
        /// </summary>
        public Setting<bool> AllowPlayersToTakeBulletDamage { get; set; }

        /// <summary>
        /// If enabled and there's more than one person playing, nametags will show which player is which.
        /// </summary>
        public Setting<bool> PlayerNameTags { get; set; }

        #endregion

        #region Loading

        /// <summary>
        /// Assets will use BepInEx/plugins/Assets as the folder instead of the local level folder.
        /// </summary>
        public Setting<bool> AssetsGlobal { get; set; }

        /// <summary>
        /// Makes the player models always load from beatmaps/players for entering an arcade level. If disabled, players will be loaded from the local players.lspl file.
        /// </summary>
        public Setting<bool> LoadFromGlobalPlayersInArcade { get; set; }

        #endregion

        #region Controls

        /// <summary>
        /// Controller button to press to shoot. Requires restart if changed.
        /// </summary>
        public Setting<InputControlType> PlayerShootControl { get; set; }

        /// <summary>
        /// Keyboard key to press to shoot. Requires restart if changed.
        /// </summary>
        public Setting<Key> PlayerShootKey { get; set; }

        public Setting<bool> AllowControllerIfSinglePlayer { get; set; }

        #endregion

        #endregion

        /// <summary>
        /// Bind the individual settings of the config.
        /// </summary>
        public override void BindSettings()
        {
            Load();

            #region General

            TailUpdateMode = BindEnum(this, "General", "Tail Update Mode", RTPlayer.TailUpdateMode.FixedUpdate, "Changes the way the tail updates movement. FixedUpdate is recommended if the game gets laggy, but otherwise Update / LateUpdate is good for a smooth tail.");
            QueueBoost = Bind(this, "General", "Queue Boost", true, "If boosting should be queued when you press it during boost cooldown.");

            PlaySoundB = Bind(this, "General", "Play Boost Sound", true, "Plays a little sound when you boost.");
            PlaySoundR = Bind(this, "General", "Play Boost Recover Sound", false, "Plays a little sound when you can boost again.");
            PlaySoundRBoostTail = Bind(this, "General", "Boost Recover only with Boost Tail", true, "If the boost recovery sound should only play if the player has the boost tail.");

            ZenEditorIncludesSolid = Bind(this, "General", "Editor Zen Mode includes Solid", false, "Makes Player ignore solid objects in editor.");

            PlayerShootSound = Bind(this, "General", "Play Shoot Sound", true, "Plays a little sound when you shoot.");
            AllowPlayersToTakeBulletDamage = Bind(this, "General", "Shots hurt other players", false, "Disable this if you don't want players to kill each other.");

            PlayerNameTags = Bind(this, "General", "Multiplayer NameTags", true, "If enabled and there's more than one person playing, nametags will show which player is which.");

            #endregion

            #region Loading

            AssetsGlobal = Bind(this, "Loading", "Assets Global Source", false, "Assets will use BepInEx/plugins/Assets as the folder instead of the local level folder.");
            LoadFromGlobalPlayersInArcade = Bind(this, "Loading", "Always use global source", false, "Makes the player models always load from beatmaps/players for entering an arcade level. If disabled, players will be loaded from the local players.lspl file.");

            PlayerManager.PlayerIndexes.Add(Bind(this, "Loading", "Player 1 Model", "0", "The player uses this specific model ID."));
            PlayerManager.PlayerIndexes.Add(Bind(this, "Loading", "Player 2 Model", "0", "The player uses this specific model ID."));
            PlayerManager.PlayerIndexes.Add(Bind(this, "Loading", "Player 3 Model", "0", "The player uses this specific model ID."));
            PlayerManager.PlayerIndexes.Add(Bind(this, "Loading", "Player 4 Model", "0", "The player uses this specific model ID."));

            #endregion

            #region Controls

            AllowControllerIfSinglePlayer = Bind(this, "Controls", "Allow Controller If Single Player", true, "If controller should be usable on singleplayer.");
            PlayerShootControl = BindEnum(this, "Controls", "Shoot Control", InputControlType.Action3, "Controller button to press to shoot. Requires restart if changed.");
            PlayerShootKey = BindEnum(this, "Controls", "Shoot Key", Key.Z, "Keyboard key to press to shoot. Requires restart if changed.");

            #endregion

            Save();
        }

        #region Settings Changed

        public override void SetupSettingChanged()
        {
            SettingChanged += UpdateSettings;
        }

        void UpdateSettings()
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
        }

        #endregion
    }
}
