using BepInEx.Configuration;
using BetterLegacy.Components.Player;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Managers;
using InControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Configs
{
    public class PlayerConfig : BaseConfig
    {
        public static PlayerConfig Instance { get; set; }

        public override ConfigFile Config { get; set; }

        public PlayerConfig(ConfigFile config) : base(config)
        {
            Instance = this;
            Config = config;

			Debugger = Config.Bind("Debug", "CreativePlayers Logs Enabled", false);

			PlayerNameTags = Config.Bind("Game", "Multiplayer NameTags", false, "If enabled and if there's more than one person playing, nametags will show which player is which (WIP).");
			AssetsGlobal = Config.Bind("Game", "Assets Global Source", false, "Assets will use BepInEx/plugins/Assets as the folder instead of the local level folder.");
			LoadFromGlobalPlayersInArcade = Config.Bind("Loading", "Always use global source", false, "Makes the player models always load from beatmaps/players for entering an arcade level. If disabled, players will be loaded from the local players.lspl file.");

			TailUpdateMode = Config.Bind("Player", "Tail Update Mode", RTPlayer.TailUpdateMode.FixedUpdate, "Changes the way the tail updates movement. FixedUpdate is recommended if the game gets laggy, but otherwise Update / LateUpdate is good for a smooth tail.");

			PlaySoundB = Config.Bind("Player", "Play Boost Sound", true, "Plays a little sound when you boost.");
			PlaySoundR = Config.Bind("Player", "Play Boost Recover Sound", false, "Plays a little sound when you can boost again.");

			ZenEditorIncludesSolid = Config.Bind("Player", "Editor Zen Mode includes Solid", false, "Makes Player ignore solid objects in editor.");

			PlayerShootControl = Config.Bind("Player", "Shoot Control", InputControlType.Action3, "Controller button to press to shoot. Requires restart if changed.");
			PlayerShootKey = Config.Bind("Player", "Shoot Key", Key.Z, "Keyboard key to press to shoot. Requires restart if changed.");
			PlayerShootSound = Config.Bind("Player", "Play Shoot Sound", true, "Plays a little sound when you shoot.");
			AllowPlayersToTakeBulletDamage = Config.Bind("Player", "Shots hurt other players", false, "Disable this if you don't want players to kill each other.");
			EvaluateCode = Config.Bind("Player", "Evaluate Code", false, ".cs files from the player folder in the level path will run. E.G. boost.cs will run when the player boosts. Each code includes a stored \"playerIndex\" variable in case you want to check which player is performing the action.");

			PlayerManager.PlayerIndexes.Add(Config.Bind("Loading", "Player 1 Model", "0", "The player uses this specific model ID."));
			PlayerManager.PlayerIndexes.Add(Config.Bind("Loading", "Player 2 Model", "0", "The player uses this specific model ID."));
			PlayerManager.PlayerIndexes.Add(Config.Bind("Loading", "Player 3 Model", "0", "The player uses this specific model ID."));
			PlayerManager.PlayerIndexes.Add(Config.Bind("Loading", "Player 4 Model", "0", "The player uses this specific model ID."));

			SetConfigs();

			SetupSettingChanged();
		}

		public ConfigEntry<bool> ZenModeInEditor { get; set; }
		public ConfigEntry<bool> ZenEditorIncludesSolid { get; set; }
		public ConfigEntry<bool> PlayerNameTags { get; set; }

		public ConfigEntry<bool> LoadFromGlobalPlayersInArcade { get; set; }

		public ConfigEntry<bool> PlaySoundB { get; set; }
		public ConfigEntry<bool> PlaySoundR { get; set; }
		public ConfigEntry<RTPlayer.TailUpdateMode> TailUpdateMode { get; set; }

		public ConfigEntry<InputControlType> PlayerShootControl { get; set; }
		public ConfigEntry<Key> PlayerShootKey { get; set; }
		public ConfigEntry<bool> PlayerShootSound { get; set; }
		public ConfigEntry<bool> AllowPlayersToTakeBulletDamage { get; set; }

		public ConfigEntry<bool> AssetsGlobal { get; set; }

		public ConfigEntry<bool> Debugger { get; set; }

		public ConfigEntry<bool> EvaluateCode { get; set; }

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
