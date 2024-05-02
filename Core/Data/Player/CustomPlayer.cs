using BetterLegacy.Components.Player;
using BetterLegacy.Core.Managers;
using InControl;
using UnityEngine;
using XInputDotNetPure;
using BaseCustomPlayer = InputDataManager.CustomPlayer;

namespace BetterLegacy.Core.Data.Player
{
    public class CustomPlayer : BaseCustomPlayer
    {
        public CustomPlayer(bool _active, int _index, InputDevice _device) : base(_active, _index, _device)
        {

        }

        public CustomPlayer(bool _active, int _index) : base(_active, _index)
        {

        }

        public GameObject GameObject { get; set; }

        public RTPlayer Player { get; set; }

        public string CurrentPlayerModel { get; set; } = "0";

        public PlayerModel PlayerModel => PlayerManager.PlayerModels.ContainsKey(CurrentPlayerModel) ? PlayerManager.PlayerModels[CurrentPlayerModel] : PlayerManager.PlayerModels["0"];

        public new PlayerIndex GetPlayerIndex(int _index) => (PlayerIndex)_index;

		public new void ControllerDisconnected(InputDevice __0)
		{
			if (__0.SortOrder == SortOrder && GetDeviceModel(__0.Name) == deviceModel)
			{
				if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Input Select")
				{
					InputManager.OnDeviceAttached -= ControllerConnected;
					InputManager.OnDeviceDetached -= ControllerDisconnected;
					InputDataManager.inst.players.RemoveAt(index);
					int num = 0;
					foreach (var customPlayer in InputDataManager.inst.players)
					{
						InputDataManager.inst.players[num].index = num;
						InputDataManager.inst.players[num].playerIndex = GetPlayerIndex(InputDataManager.inst.players[num].index);
						num++;
					}
				}
				controllerConnected = false;
				device = null;
				if (Player)
					Player.Actions = null;
				HarmonyLib.AccessTools.Field(typeof(InputDataManager), "playerDisconnectedEvent").SetValue(InputDataManager.inst, this);
				Debug.LogFormat("{0}Disconnected Controler Was Attached to player. Controller [{1}] [{2}] -/- Player [{3}]", InputDataManager.className, __0.Name, __0.SortOrder, index);
			}
		}

		public new void ControllerConnected(InputDevice __0)
		{
			if (__0.SortOrder == SortOrder && GetDeviceModel(__0.Name) == deviceModel)
			{
				InputDataManager.inst.ThereIsNoPlayerUsingJoystick(__0);
				controllerConnected = true;
				device = __0;
				HarmonyLib.AccessTools.Field(typeof(InputDataManager), "playerReconnectedEvent").SetValue(InputDataManager.inst, this);
				var myGameActions = MyGameActions.CreateWithJoystickBindings();
				myGameActions.Device = __0;
				if (Player)
					Player.Actions = myGameActions;
				Debug.LogFormat("{0}Connected Controller Exists in players. Controller [{1}] [{2}] -> Player [{3}]", InputDataManager.className, __0.Name, __0.SortOrder, index);
			}
		}

		public new void ReconnectController(InputDevice __0)
		{
			var myGameActions = MyGameActions.CreateWithJoystickBindings();
			myGameActions.Device = device;
			if (Player)
				Player.Actions = myGameActions;
		}

		public int Health
        {
            get => health;
            set
            {
                health = value;
                if (Player)
                {
                    var rb = (Rigidbody2D)Player.playerObjects["RB Parent"].values["Rigidbody2D"];
                    Player.UpdateTail(health, rb.position);
                }
            }
        }

        public static implicit operator bool(CustomPlayer exists) => exists != null;
    }
}
