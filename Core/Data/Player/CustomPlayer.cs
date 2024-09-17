using BetterLegacy.Components.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using InControl;
using System;
using System.Linq;
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

        public PlayerModel PlayerModel => PlayerManager.PlayerModels.TryGetValue(CurrentPlayerModel, out PlayerModel playerModel) ? playerModel : PlayerModel.DefaultPlayer;

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

            Func<Modifier<CustomPlayer>, bool> modifierPredicate = x => x.Action == null && x.type == ModifierBase.Type.Action || x.Trigger == null && x.type == ModifierBase.Type.Trigger || x.Inactive == null;

            if (PlayerModel.modifiers.Any(modifierPredicate))
                PlayerModel.modifiers.Where(modifierPredicate).ToList().ForEach(modifier =>
                {
                    modifier.Action = ModifiersHelper.PlayerAction;
                    modifier.Trigger = ModifiersHelper.PlayerTrigger;
                    modifier.Inactive = ModifiersHelper.PlayerInactive;
                });

            var actions = PlayerModel.modifiers.Where(x => x.type == ModifierBase.Type.Action);
            var triggers = PlayerModel.modifiers.Where(x => x.type == ModifierBase.Type.Trigger);

            if (triggers.Count() > 0)
            {
                if (triggers.All(x => !x.active && (x.Trigger(x) && !x.not || !x.Trigger(x) && x.not)))
                {
                    foreach (var act in actions.Where(x => !x.active))
                    {
                        if (!act.constant)
                            act.active = true;

                        act.running = true;
                        act.Action?.Invoke(act);
                    }

                    foreach (var trig in triggers.Where(x => !x.constant))
                        trig.active = true;
                }
                else
                {
                    foreach (var act in actions.Where(x => x.active || x.running))
                    {
                        act.active = false;
                        act.running = false;
                        act.Inactive?.Invoke(act);
                    }
                }
            }
            else
            {
                foreach (var act in actions.Where(x => !x.active))
                {
                    if (!act.constant)
                        act.active = true;

                    act.running = true;
                    act.Action?.Invoke(act);
                }
            }
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
