using UnityEngine;

using XInputDotNetPure;
using InControl;

using BetterLegacy.Configs;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Menus.UI.Interfaces;

using BaseCustomPlayer = InputDataManager.CustomPlayer;

namespace BetterLegacy.Core.Data.Player
{
    public class CustomPlayer : BaseCustomPlayer
    {
        public CustomPlayer(bool active, int index, InputDevice device) : base(active, index)
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
            Debug.LogFormat("{0}Created new Custom Player [{1}]", new object[] { "[<color=#4CAF50>InputDataManager</color>] \n", this.index });
        }

        public CustomPlayer(bool active, int index) : base(active, index) { }

        public static CustomPlayer Main => InputDataManager.inst.players[0] as CustomPlayer;

        /// <summary>
        /// If the player is currently alive.
        /// </summary>
        public bool Alive => Player;

        public RTPlayer Player { get; set; }

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

        public void UpdatePlayerModel()
        {
            PlayerModel = PlayerModel.DeepCopy(PlayersData.GetPlayerModel(CurrentModel), false);
            if (Player)
                Player.Model = PlayerModel;
        }

        public void Test()
        {
            PlayerModel.modifiers.Add(ModifiersManager.defaultPlayerModifiers.Find(x => x.Name == "kill"));
        }

        public PlayerModel PlayerModel { get; set; } = PlayerModel.DefaultPlayer;

        public new PlayerIndex GetPlayerIndex(int _index) => (PlayerIndex)_index;

        public new void ControllerDisconnected(InputDevice device)
        {
            if (device.SortOrder != SortOrder || GetDeviceModel(device.Name) != deviceModel)
                return;

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Input Select")
            {
                InputManager.OnDeviceAttached -= ControllerConnected;
                InputManager.OnDeviceDetached -= ControllerDisconnected;
                InputDataManager.inst.players.RemoveAt(index);
                for (int i = 0; i < InputDataManager.inst.players.Count; i++)
                {
                    InputDataManager.inst.players[i].index = i;
                    InputDataManager.inst.players[i].playerIndex = GetPlayerIndex(InputDataManager.inst.players[i].index);
                }
            }

            controllerConnected = false;
            base.device = null;
            if (Player)
                Player.Actions = null;

            InputDataManager.inst.SetAllControllerRumble(0f);
            ControllerDisconnectedMenu.Init(index);
            Debug.LogFormat("{0}Disconnected Controler was attached to player. Controller [{1}] [{2}] -/- Player [{3}]", InputDataManager.className, device.Name, device.SortOrder, index);
        }

        public new void ControllerConnected(InputDevice device)
        {
            if (device.SortOrder != SortOrder || GetDeviceModel(device.Name) != deviceModel)
                return;

            InputDataManager.inst.ThereIsNoPlayerUsingJoystick(device);
            controllerConnected = true;
            base.device = device;

            var myGameActions = MyGameActions.CreateWithJoystickBindings();
            myGameActions.Device = device;
            if (Player)
                Player.Actions = myGameActions;

            ControllerDisconnectedMenu.Current?.Reconnected();
            Debug.LogFormat("{0}Connected Controller exists in players. Controller [{1}] [{2}] -> Player [{3}]", InputDataManager.className, device.Name, device.SortOrder, index);
        }

        public new void ReconnectController(InputDevice __0)
        {
            var myGameActions = MyGameActions.CreateWithJoystickBindings();
            myGameActions.Device = device;
            if (Player)
                Player.Actions = myGameActions;
        }

        public void UpdateModifiers()
        {
            if (CoreHelper.Paused || PlayerModel == null || PlayerModel.modifiers == null || PlayerModel.modifiers.IsEmpty())
                return;

            for (int i = 0; i < PlayerModel.modifiers.Count; i++)
                PlayerModel.modifiers[i].reference = this;

            ModifiersHelper.RunModifiersLoop(PlayerModel.modifiers, Alive);
        }

        public PlayerInventory inventory = new PlayerInventory();

        public void ResetHealth() => Health = PlayerModel?.basePart?.health ?? 3;

        public static int MaxHealth { get; set; } = 3;

        public int Health
        {
            get => Mathf.Clamp(health, 0, MaxHealth);
            set
            {
                health = Mathf.Clamp(value, 0, MaxHealth);
                if (Player && Player.rb)
                    Player.UpdateTail(health, Player.rb.position);
            }
        }

        public static implicit operator bool(CustomPlayer exists) => exists != null;
    }
}
