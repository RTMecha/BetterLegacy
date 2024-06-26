﻿using BepInEx.Configuration;
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

        public PlayerConfig() : base("Player") // Set config name via base("")
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
        /// Plays a little sound when you boost.
        /// </summary>
        public Setting<bool> PlaySoundB { get; set; }

        /// <summary>
        /// Plays a little sound when you can boost again.
        /// </summary>
        public Setting<bool> PlaySoundR { get; set; }

        /// <summary>
        /// Makes Player ignore solid objects in editor.
        /// </summary>
        public Setting<bool> ZenEditorIncludesSolid { get; set; }

        /// <summary>
        /// Controller button to press to shoot. Requires restart if changed.
        /// </summary>
        public Setting<InputControlType> PlayerShootControl { get; set; }

        /// <summary>
        /// Keyboard key to press to shoot. Requires restart if changed.
        /// </summary>
        public Setting<Key> PlayerShootKey { get; set; }

        /// <summary>
        /// Plays a little sound when you shoot.
        /// </summary>
        public Setting<bool> PlayerShootSound { get; set; }

        /// <summary>
        /// Disable this if you don't want players to kill each other.
        /// </summary>
        public Setting<bool> AllowPlayersToTakeBulletDamage { get; set; }

        /// <summary>
        /// .cs files from the player folder in the level path will run. E.G. boost.cs will run when the player boosts. Each code includes a stored \"playerIndex\" variable in case you want to check which player is performing the action.
        /// </summary>
        public Setting<bool> EvaluateCode { get; set; }

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

        #endregion

        /// <summary>
        /// Bind the individual settings of the config.
        /// </summary>
        public override void BindSettings()
        {
            Load();

            #region General

            TailUpdateMode = BindEnum(this, "General", "Tail Update Mode", RTPlayer.TailUpdateMode.FixedUpdate, "Changes the way the tail updates movement. FixedUpdate is recommended if the game gets laggy, but otherwise Update / LateUpdate is good for a smooth tail.");

            PlaySoundB = Bind(this, "General", "Play Boost Sound", true, "Plays a little sound when you boost.");
            PlaySoundR = Bind(this, "General", "Play Boost Recover Sound", false, "Plays a little sound when you can boost again.");

            ZenEditorIncludesSolid = Bind(this, "General", "Editor Zen Mode includes Solid", false, "Makes Player ignore solid objects in editor.");

            PlayerShootControl = BindEnum(this, "General", "Shoot Control", InputControlType.Action3, "Controller button to press to shoot. Requires restart if changed.");
            PlayerShootKey = BindEnum(this, "General", "Shoot Key", Key.Z, "Keyboard key to press to shoot. Requires restart if changed.");
            PlayerShootSound = Bind(this, "General", "Play Shoot Sound", true, "Plays a little sound when you shoot.");
            AllowPlayersToTakeBulletDamage = Bind(this, "General", "Shots hurt other players", false, "Disable this if you don't want players to kill each other.");
            EvaluateCode = Bind(this, "General", "Evaluate Code", false, ".cs files from the player folder in the level path will run. E.G. boost.cs will run when the player boosts. Each code includes a stored \"playerIndex\" variable in case you want to check which player is performing the action.");

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
            RTPlayer.EvaluateCode = EvaluateCode.Value;
        }

        #endregion
    }
}
