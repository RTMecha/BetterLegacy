using UnityEngine;

using XInputDotNetPure;
using InControl;
using SteamworksFacepunch;

using BetterLegacy.Configs;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Menus.UI.Interfaces;

namespace BetterLegacy.Core.Data.Player
{
    /// <summary>
    /// Represents a player in a Project Arrhythmia level.
    /// </summary>
    public class PAPlayer : Exists, IModifierReference, ICustomActivatable, IPacket
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
                sortOrder = device.SortOrder;
            }
            else
                deviceName = "keyboard";

            deviceType = GetDeviceType(deviceName);
            deviceModel = GetDeviceModel(deviceName);
            InputManager.OnDeviceAttached += ControllerConnected;
            InputManager.OnDeviceDetached += ControllerDisconnected;
            SetupInput();
            IsLocalPlayer = true;
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
            IsLocalPlayer = true;
        }

        #region Values

        /// <summary>
        /// The main player.
        /// </summary>
        public static PAPlayer Main => PlayerManager.Players[0];

        /// <summary>
        /// Identification string.
        /// </summary>
        public string id = PAObjectBase.GetStringID();

        /// <summary>
        /// If the player has been spawned.
        /// </summary>
        public bool active;

        /// <summary>
        /// The current lives count of the player.
        /// </summary>
        public int lives = -1;
        /// <summary>
        /// If the player is out of lives.
        /// </summary>
        public bool OutOfLives => lives == 0;

        /// <summary>
        /// Health the player has.
        /// </summary>
        public int health = 3;

        /// <summary>
        /// The maximum amount of health the players have.
        /// </summary>
        public static int MaxHealth { get; set; } = 3;

        /// <summary>
        /// Health the player has. If it reaches 0, the player dies.
        /// </summary>
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

        /// <summary>
        /// Index of the player in <see cref="PlayerManager.Players"/>.
        /// </summary>
        public int index;

        /// <summary>
        /// Controller device name.
        /// </summary>
        public string deviceName;

        /// <summary>
        /// Controller device type.
        /// </summary>
        public ControllerType deviceType;

        /// <summary>
        /// Controller device model.
        /// </summary>
        public string deviceModel;

        /// <summary>
        /// Player controller reference.
        /// </summary>
        public InputDevice device;

        /// <summary>
        /// <see cref="InputDevice.SortOrder"/> cache.
        /// </summary>
        public int sortOrder;

        /// <summary>
        /// If the players' controller is connected.
        /// </summary>
        public bool controllerConnected;

        /// <summary>
        /// Index of the player according to <see cref="PlayerIndex"/>.
        /// </summary>
        public PlayerIndex playerIndex;

        /// <summary>
        /// Amount the controller is rumbling.
        /// </summary>
        public Vector2 rumble;

        /// <summary>
        /// If the player is currently alive.
        /// </summary>
        public bool Alive => RuntimePlayer;

        /// <summary>
        /// The custom active state.
        /// </summary>
        public bool CustomActive { get; set; } = true;

        /// <summary>
        /// The runtime player reference.
        /// </summary>
        public RTPlayer RuntimePlayer { get; set; }

        /// <summary>
        /// The current player model ID.
        /// </summary>
        public string currentPlayerModel = PlayerModel.DEFAULT_ID;
        /// <summary>
        /// The current player model ID.
        /// </summary>
        public string CurrentModel
        {
            get => !ProjectArrhythmia.State.InEditor && PlayersData.AllowCustomModels && PlayerConfig.Instance.LoadFromGlobalPlayersInArcade.Value ? PlayerManager.PlayerIndexes[index].Value : currentPlayerModel;
            set
            {
                currentPlayerModel = value;
                UpdatePlayerModel();
            }
        }

        /// <summary>
        /// The current player model cache.
        /// </summary>
        public PlayerModel PlayerModel { get; set; } = PlayerModel.DefaultPlayer;

        public PlayerInput Input { get; set; }

        /// <summary>
        /// If the player is a local player.
        /// </summary>
        public bool IsLocalPlayer { get; set; }

        /// <summary>
        /// Steam ID of the player.
        /// </summary>
        public SteamId? ID { get; set; }

        public RTLevelBase ParentRuntime { get; set; }

        public ModifierReferenceType ReferenceType => ModifierReferenceType.PAPlayer;

        public int IntVariable { get; set; }

        #endregion

        #region Functions

        public void ReadPacket(NetworkReader reader)
        {
            id = reader.ReadString();
            active = reader.ReadBoolean();
            lives = reader.ReadInt32();
            Health = reader.ReadInt32();
            index = reader.ReadInt32();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(id);
            writer.Write(active);
            writer.Write(lives);
            writer.Write(Health);
            writer.Write(index);
        }

        public void SetCustomActive(bool active)
        {
            CustomActive = active;
            RuntimePlayer?.SetActive(active);
        }

        /// <summary>
        /// Updates player model data.
        /// </summary>
        public void UpdatePlayerModel()
        {
            PlayerModel = PlayersData.GetPlayerModel(CurrentModel);
            if (RuntimePlayer)
                RuntimePlayer.Model = PlayerModel;
        }

        /// <summary>
        /// Gets the players' maximum amount of health.
        /// </summary>
        /// <returns>Returns the challenge mode default health if the challenge mode default health is greater than 0, otherwise returns the players' local health.</returns>
        public int GetMaxHealth() => RTBeatmap.Current.challengeMode.DefaultHealth > 0 ? RTBeatmap.Current.challengeMode.DefaultHealth : GetControl()?.Health ?? 3;

        /// <summary>
        /// Gets the players' maximum amount of lives.
        /// </summary>
        /// <returns>Returns the challenge mode lives count if the challenge mode lives count is greater than 0, otherwise returns the player controls' lives count.</returns>
        public int GetMaxLives() => RTBeatmap.Current.challengeMode.Lives > 0 ? RTBeatmap.Current.challengeMode.Lives : GetControl()?.lives ?? -1;

        /// <summary>
        /// Gets the players' control data.
        /// </summary>
        /// <returns>Returns the player model converted to a player control if <see cref="LevelData.allowPlayerModelControls"/> is true, otherwise returns <see cref="GetCustomControl"/>.</returns>
        public PlayerControl GetControl() => GameData.Current.data.level.allowPlayerModelControls ? PlayerModel.ToPlayerControl() : GetCustomControl();

        /// <summary>
        /// Gets the player control data.
        /// </summary>
        /// <returns>Returns the player control data associated with this player.</returns>
        public PlayerControl GetCustomControl() => PlayersData.Current && PlayersData.Current.playerControls.TryGetAt(index, out PlayerControl playerControl) ? playerControl : new PlayerControl();

        /// <summary>
        /// Gets the <see cref="PlayerIndex"/> value for the player.
        /// </summary>
        /// <param name="index">Index of the player controller.</param>
        /// <returns>Returns the <paramref name="index"/> casted to <see cref="PlayerIndex"/>. This is done so more than 4 players is possible.</returns>
        public PlayerIndex GetPlayerIndex(int index) => (PlayerIndex)index;

        /// <summary>
        /// Gets the <see cref="ControllerType"/> of the players' controller.
        /// </summary>
        /// <param name="deviceName">Device name.</param>
        /// <returns>Returns the controller type of the device.</returns>
        public ControllerType GetDeviceType(string deviceName)
        {
            deviceName = deviceName.ToLower().Remove(" ");
            if (deviceName.Contains("xinput") || deviceName.Contains("xbox") || deviceName.Contains("microsoft"))
                return ControllerType.XBox;
            else if (deviceName.Contains("playstation") || deviceName.Contains("ps") || deviceName.Contains("sony"))
                return ControllerType.PS;
            if (string.IsNullOrEmpty(deviceName) || deviceName.Contains("keyboard"))
                return ControllerType.Keyboard;
            return ControllerType.Unknown;
        }

        /// <summary>
        /// Gets the device model of the players' controller.
        /// </summary>
        /// <param name="deviceName">Device name.</param>
        /// <returns>Returns the device model name of the device.</returns>
        public string GetDeviceModel(string deviceName) => GetDeviceType(deviceName) switch
        {
            ControllerType.XBox => "XBox Controller",
            ControllerType.PS => "PlayStation Controller",
            ControllerType.Keyboard => "Keyboard",
            _ => "Unknown Controller",
        };

        /// <summary>
        /// Function for <see cref="InputManager.OnDeviceDetached"/>
        /// </summary>
        /// <param name="device">The device that was disconnected.</param>
        public void ControllerDisconnected(InputDevice device)
        {
            if (device.SortOrder != sortOrder || GetDeviceModel(device.Name) != deviceModel)
                return;

            if (InputSelectInterface.Current)
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
            Input = null;

            InputDataManager.inst.SetAllControllerRumble(0f);
            ControllerDisconnectedInterface.Init(index);
            Debug.LogFormat("{0}Disconnected Controler was attached to player. Controller [{1}] [{2}] -/- Player [{3}]", InputDataManager.className, device.Name, device.SortOrder, index);
        }

        /// <summary>
        /// Function for <see cref="InputManager.OnDeviceAttached"/>
        /// </summary>
        /// <param name="device">The device that was connected.</param>
        public void ControllerConnected(InputDevice device)
        {
            if (device.SortOrder != sortOrder || GetDeviceModel(device.Name) != deviceModel)
                return;

            InputDataManager.inst.ThereIsNoPlayerUsingJoystick(device);
            controllerConnected = true;
            this.device = device;

            var input = PlayerInput.Controller;
            input.Device = device;
            Input = input;

            ControllerDisconnectedInterface.Current?.Reconnected();
            Debug.LogFormat("{0}Connected Controller exists in players. Controller [{1}] [{2}] -> Player [{3}]", InputDataManager.className, device.Name, device.SortOrder, index);
        }

        /// <summary>
        /// Resets the players' health to the current default.
        /// </summary>
        public void ResetHealth() => Health = GetDefaultHealth();

        /// <summary>
        /// Gets the default health for this player.
        /// </summary>
        /// <returns>Returns the challenge mode default health if the user is not editing and the challenge mode default health is greater than 0, otherwise returns the players' local health.</returns>
        public int GetDefaultHealth() => !ProjectArrhythmia.State.IsEditing && RTBeatmap.Current.challengeMode.DefaultHealth > 0 ? RTBeatmap.Current.challengeMode.DefaultHealth : GetControl()?.Health ?? 3;

        /// <summary>
        /// Initializes the player input.
        /// </summary>
        public void SetupInput()
        {
            if (device == null)
            {
                Input = (ProjectArrhythmia.State.InEditor || PlayerConfig.Instance.AllowControllerIfSinglePlayer.Value) && PlayerManager.IsSingleplayer ?
                    PlayerInput.ControllerAndKeyboard :
                    PlayerInput.Keyboard;
                return;
            }

            var input = PlayerInput.Controller;
            input.Device = device;
            Input = input;
        }

        public IRTObject GetRuntimeObject() => RuntimePlayer;

        public IPrefabable AsPrefabable() => null;
        public ITransformable AsTransformable() => RuntimePlayer;

        public ModifierLoop GetModifierLoop() => RuntimePlayer?.controlLoop;

        #endregion
    }
}
