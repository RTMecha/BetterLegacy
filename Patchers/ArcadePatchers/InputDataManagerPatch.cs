using System.Collections.Generic;

using UnityEngine;

using HarmonyLib;

using InControl;

using BetterLegacy.Core;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Managers;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(InputDataManager))]
    public class InputDataManagerPatch : MonoBehaviour
    {
        public static InputDataManager Instance { get => InputDataManager.inst; set => InputDataManager.inst = value; }

        [HarmonyPatch(nameof(InputDataManager.AlivePlayers), MethodType.Getter)]
        [HarmonyPrefix]
        static bool GetAlivePlayers(ref List<InputDataManager.CustomPlayer> __result)
        {
            __result = Instance.players;
            return false;
        }

        [HarmonyPatch(nameof(InputDataManager.SetAllControllerRumble), new[] { typeof(float), typeof(float), typeof(bool) })]
        [HarmonyPrefix]
        static bool SetAllControllerRumble(float __0, float __1, bool __2 = true)
        {
            PlayerManager.ControllerRumble = new Vector2(__0, __1);

            return false;
        }

        [HarmonyPatch(nameof(InputDataManager.SetControllerRumble), new[] { typeof(int), typeof(float), typeof(float), typeof(bool) })]
        [HarmonyPrefix]
        static bool SetControllerRumble(int __0, float __1, float __2, bool __3 = true)
        {
            if (PlayerManager.Players.TryGetAt(__0, out PAPlayer player))
                player.rumble = new Vector2(__0, __1);

            return false;
        }

        [HarmonyPatch(nameof(InputDataManager.RemovePlayer))]
        [HarmonyPrefix]
        static bool RemovePlayerPrefix() => false;

        [HarmonyPatch(nameof(InputDataManager.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix()
        {
            if (!Instance.playersCanJoin || PlayerManager.Players.Count >= 8)
                return false;

            if (Instance.joystickListener.Join.WasPressed)
            {
                var activeDevice = InputManager.ActiveDevice;
                if (PlayerManager.DeviceNotConnected(activeDevice))
                    PlayerManager.Players.Add(new PAPlayer(true, PlayerManager.Players.Count, activeDevice));
            }

            if (Instance.keyboardListener.Join.WasPressed && PlayerManager.KeyboardNotConnected())
                PlayerManager.Players.Add(new PAPlayer(true, PlayerManager.Players.Count, null));

            return false;
        }

        [HarmonyPatch(nameof(InputDataManager.ClearInputs))]
        [HarmonyPrefix]
        static bool ClearInputsPrefix(bool __0)
        {
            Instance.ResetPlayers();
            PlayerManager.Players.Clear();
            if (__0)
                PlayerManager.Players.Add(PlayerManager.CreateDefaultPlayer());
            return false;
        }

        [HarmonyPatch(nameof(InputDataManager.ResetPlayers))]
        [HarmonyPrefix]
        static bool ResetPlayersPrefix()
        {
            for (int i = 0; i < PlayerManager.Players.Count; i++)
                PlayerManager.RemovePlayer(PlayerManager.Players[i]);
            Instance.StopAllControllerRumble();
            return false;
        }
    }
}
