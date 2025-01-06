using BetterLegacy.Configs;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using InControl;
using UnityEngine;
using XInputDotNetPure;
using BaseCustomPlayer = InputDataManager.CustomPlayer;

namespace BetterLegacy.Core.Data.Player
{
    public class CustomPlayer : BaseCustomPlayer
    {
        public CustomPlayer(bool _active, int _index, InputDevice _device) : base(_active, _index, _device) { }

        public CustomPlayer(bool _active, int _index) : base(_active, _index) { }

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
            HarmonyLib.AccessTools.Field(typeof(InputDataManager), "playerDisconnectedEvent").SetValue(InputDataManager.inst, this);
            Debug.LogFormat("{0}Disconnected Controler was attached to player. Controller [{1}] [{2}] -/- Player [{3}]", InputDataManager.className, device.Name, device.SortOrder, index);
        }

        public new void ControllerConnected(InputDevice device)
        {
            if (device.SortOrder == SortOrder && GetDeviceModel(device.Name) == deviceModel)
            {
                InputDataManager.inst.ThereIsNoPlayerUsingJoystick(device);
                controllerConnected = true;
                base.device = device;
                HarmonyLib.AccessTools.Field(typeof(InputDataManager), "playerReconnectedEvent").SetValue(InputDataManager.inst, this);
                var myGameActions = MyGameActions.CreateWithJoystickBindings();
                myGameActions.Device = device;
                if (Player)
                    Player.Actions = myGameActions;
                Debug.LogFormat("{0}Connected Controller exists in players. Controller [{1}] [{2}] -> Player [{3}]", InputDataManager.className, device.Name, device.SortOrder, index);
            }
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
            if (CoreHelper.Paused || PlayerModel == null || PlayerModel.modifiers == null || PlayerModel.modifiers.Count <= 0)
                return;

            for (int i = 0; i < PlayerModel.modifiers.Count; i++)
                PlayerModel.modifiers[i].reference = this;

            ModifiersHelper.RunModifiersLoop(PlayerModel.modifiers, Alive);
        }

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
