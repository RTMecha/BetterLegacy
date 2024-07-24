using BetterLegacy.Core.Data.Player;
using HarmonyLib;
using InControl;
using System.Collections.Generic;
using UnityEngine;

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
            __result = Instance.players.FindAll(x => x is CustomPlayer && (x as CustomPlayer).Player && (x as CustomPlayer).Player.PlayerAlive);
            return false;
        }

        [HarmonyPatch(nameof(InputDataManager.SetAllControllerRumble), new[] { typeof(float), typeof(float), typeof(bool) })]
        [HarmonyPrefix]
        static bool SetAllControllerRumble(float __0, float __1, bool __2 = true)
        {
            if (DataManager.inst.GetSettingBool("ControllerVibrate", true))
            {
                foreach (var customPlayer in Instance.players)
                    customPlayer.device?.Vibrate(Mathf.Clamp(__0, 0f, 0.5f), Mathf.Clamp(__1, 0f, 0.5f));

            }
            return false;
        }

        [HarmonyPatch(nameof(InputDataManager.SetControllerRumble), new[] { typeof(int), typeof(float), typeof(float), typeof(bool) })]
        [HarmonyPrefix]
        static bool SetControllerRumble(int __0, float __1, float __2, bool __3 = true)
        {
            foreach (var customPlayer in Instance.players)
            {
                if (customPlayer is CustomPlayer && (customPlayer as CustomPlayer).Player && (customPlayer as CustomPlayer).Player.playerIndex == __0)
                    customPlayer.device?.Vibrate(Mathf.Clamp(__1, 0f, 0.5f), Mathf.Clamp(__2, 0f, 0.5f));
            }
            return false;
        }

        [HarmonyPatch(nameof(InputDataManager.RemovePlayer))]
        [HarmonyPrefix]
        static bool RemovePlayerPrefix(InputDataManager.CustomPlayer __0)
        {
            int index = __0.index;
            if (__0 is CustomPlayer customPlayer && customPlayer.Player)
            {
                Instance.StopControllerRumble(index);
                customPlayer.Player.Actions = null;
                customPlayer.Player.faceController = null;
                if (customPlayer.Player.gameObject)
                {
                    Destroy(customPlayer.Player.gameObject);
                }
            }

            Instance.StopAllControllerRumble();
            Instance.players.RemoveAt(index);
            return false;
        }

        [HarmonyPatch(nameof(InputDataManager.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix()
        {
            var inst = Instance;
            if (!inst.playersCanJoin || inst.players.Count >= 8)
                return false;

            if (inst.JoinButtonWasPressedOnListener(inst.joystickListener))
            {
                var activeDevice = InputManager.ActiveDevice;
                if (inst.ThereIsNoPlayerUsingJoystick(activeDevice))
                    inst.players.Add(new CustomPlayer(true, inst.players.Count, activeDevice));
            }
            if (inst.JoinButtonWasPressedOnListener(inst.keyboardListener) && inst.ThereIsNoPlayerUsingKeyboard())
                inst.players.Add(new CustomPlayer(true, inst.players.Count, null));

            return false;
        }
    }
}
