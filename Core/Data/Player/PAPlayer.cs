using UnityEngine;

using XInputDotNetPure;
using InControl;

using BetterLegacy.Configs;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Menus.UI.Interfaces;

namespace BetterLegacy.Core.Data.Player
{
    public class PAPlayer : Exists, IModifierReference
    {
        public PAPlayer(bool active, int index, InputDevice device)
        {
            this.active = active;
            this.index = index;
            this.device = device;

            playerIndex = GetPlayerIndex(index);

            if (device != null)
            {
                deviceName = device.Name;
                controllerConnected = true;
                SortOrder = device.SortOrder;
            }
            else
                deviceName = "keyboard";

            deviceType = GetDeviceType(deviceName);
            deviceModel = GetDeviceModel(deviceName);
            InputManager.OnDeviceAttached += ControllerConnected;
            InputManager.OnDeviceDetached += ControllerDisconnected;
            Debug.Log($"{InputDataManager.className}Created new Custom Player [{this.index}]");
        }

        public PAPlayer(bool active, int index)
        {
            this.active = active;
            this.index = index;
            playerIndex = GetPlayerIndex(index);
            device = InputDevice.Null;
            deviceName = "keyboard";
            deviceType = GetDeviceType(deviceName);
            deviceModel = GetDeviceModel(deviceName);
        }

        #region Values

        public static PAPlayer Main => PlayerManager.Players[0];

        public string id = PAObjectBase.GetStringID();

        public bool active;

        public int lives = -1;
        /// <summary>
        /// If the player is out of lives.
        /// </summary>
        public bool OutOfLives => lives == 0;

        public int health = 3;

        public int index;

        public string deviceName;

        public ControllerType deviceType;

        public string deviceModel;

        public InputDevice device;

        public int SortOrder;

        public bool controllerConnected;

        public PlayerIndex playerIndex;

        public Vector2 rumble;

        /// <summary>
        /// If the player is currently alive.
        /// </summary>
        public bool Alive => RuntimePlayer;

        public RTPlayer RuntimePlayer { get; set; }


        public string currentPlayerModel = PlayerModel.DEFAULT_ID;
        public string CurrentModel
        {
            get => !CoreHelper.InEditor && PlayersData.AllowCustomModels && PlayerConfig.Instance.LoadFromGlobalPlayersInArcade.Value ? PlayerManager.PlayerIndexes[index].Value : currentPlayerModel;
            set
            {
                currentPlayerModel = value;
                UpdatePlayerModel();
            }
        }

        public PlayerModel PlayerModel { get; set; } = PlayerModel.DefaultPlayer;

        public PlayerInventory inventory = new PlayerInventory();

        public static int MaxHealth { get; set; } = 3;

        public int Health
        {
            get => Mathf.Clamp(health, 0, MaxHealth);
            set
            {
                health = Mathf.Clamp(value, 0, MaxHealth);
                if (RuntimePlayer && RuntimePlayer.rb)
                    RuntimePlayer.UpdateTail(health, RuntimePlayer.rb.position);
            }
        }

        public RTLevelBase ParentRuntime { get; set; }

        public ModifierReferenceType ReferenceType => ModifierReferenceType.PAPlayer;

        public int IntVariable { get; set; }

        public IRTObject GetRuntimeObject() => RuntimePlayer;

        public IPrefabable AsPrefabable() => null;
        public ITransformable AsTransformable() => RuntimePlayer;

        #endregion

        #region Methods

        public void UpdatePlayerModel()
        {
            PlayerModel = PlayersData.GetPlayerModel(CurrentModel);
            if (RuntimePlayer)
                RuntimePlayer.Model = PlayerModel;
        }

        public int GetMaxHealth() => RTBeatmap.Current.challengeMode.DefaultHealth > 0 ? RTBeatmap.Current.challengeMode.DefaultHealth : GetControl()?.Health ?? 3;

        public int GetMaxLives() => RTBeatmap.Current.challengeMode.Lives > 0 ? RTBeatmap.Current.challengeMode.Lives : GetControl()?.lives ?? -1;

        public PlayerControl GetControl() => GameData.Current.data.level.allowPlayerModelControls ? PlayerModel.ToPlayerControl() : GetCustomControl();

        public PlayerControl GetCustomControl() => PlayersData.Current && PlayersData.Current.playerControls.TryGetAt(index, out PlayerControl playerControl) ? playerControl : new PlayerControl();

        public PlayerIndex GetPlayerIndex(int _index) => (PlayerIndex)_index;

        public ControllerType GetDeviceType(string deviceName)
        {
            deviceName = deviceName.ToLower().Replace(" ", "");
            if (deviceName.Contains("xinput") || deviceName.Contains("xbox") || deviceName.Contains("microsoft"))
                return ControllerType.XBox;
            else if (deviceName.Contains("playstation") || deviceName.Contains("ps") || deviceName.Contains("sony"))
                return ControllerType.PS;
            if (string.IsNullOrEmpty(deviceName) || deviceName.Contains("keyboard"))
                return ControllerType.Keyboard;
            return ControllerType.Unknown;
        }

        public string GetDeviceModel(string deviceName) => GetDeviceType(deviceName) switch
        {
            ControllerType.XBox => "XBox Controller",
            ControllerType.PS => "PlayStation Controller",
            ControllerType.Keyboard => "Keyboard",
            _ => "Unknown Controller",
        };

        public void ControllerDisconnected(InputDevice device)
        {
            if (device.SortOrder != SortOrder || GetDeviceModel(device.Name) != deviceModel)
                return;

            if (InputSelectMenu.Current)
            {
                InputManager.OnDeviceAttached -= ControllerConnected;
                InputManager.OnDeviceDetached -= ControllerDisconnected;
                PlayerManager.Players.RemoveAt(index);
                for (int i = 0; i < PlayerManager.Players.Count; i++)
                {
                    PlayerManager.Players[i].index = i;
                    PlayerManager.Players[i].playerIndex = GetPlayerIndex(i);
                }
            }

            controllerConnected = false;
            this.device = null;
            if (RuntimePlayer)
                RuntimePlayer.Actions = null;

            InputDataManager.inst.SetAllControllerRumble(0f);
            ControllerDisconnectedMenu.Init(index);
            Debug.LogFormat("{0}Disconnected Controler was attached to player. Controller [{1}] [{2}] -/- Player [{3}]", InputDataManager.className, device.Name, device.SortOrder, index);
        }

        public void ControllerConnected(InputDevice device)
        {
            if (device.SortOrder != SortOrder || GetDeviceModel(device.Name) != deviceModel)
                return;

            InputDataManager.inst.ThereIsNoPlayerUsingJoystick(device);
            controllerConnected = true;
            this.device = device;

            var myGameActions = MyGameActions.CreateWithJoystickBindings();
            myGameActions.Device = device;
            if (RuntimePlayer)
                RuntimePlayer.Actions = myGameActions;

            ControllerDisconnectedMenu.Current?.Reconnected();
            Debug.LogFormat("{0}Connected Controller exists in players. Controller [{1}] [{2}] -> Player [{3}]", InputDataManager.className, device.Name, device.SortOrder, index);
        }

        public void ResetHealth() => Health = GetControl()?.Health ?? 3;

        #endregion
    }
}
