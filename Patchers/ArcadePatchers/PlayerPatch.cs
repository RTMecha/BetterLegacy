using BetterLegacy.Core.Helpers;
using HarmonyLib;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(Player))]
    // Patch used for checking if any of the methods run and preventing them.
    public class PlayerPatch
    {
        [HarmonyPatch(nameof(Player.StopMovement))]
        [HarmonyPrefix]
        static bool StopMovementPrefix()
        {
            CoreHelper.Log($"{nameof(Player.StopMovement)}() Method invoked");
            return false;
        }

        [HarmonyPatch(nameof(Player.Start))]
        [HarmonyPrefix]
        static bool StartPrefix(Player __instance)
        {
            CoreHelper.Log($"{nameof(Player.Start)}() Method invoked");
            return false;
        }

        [HarmonyPatch(nameof(Player.SetColor))]
        [HarmonyPrefix]
        static bool SetColorPrefix() => false;

        [HarmonyPatch(nameof(Player.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix() => false;

        [HarmonyPatch(nameof(Player.LateUpdate))]
        [HarmonyPrefix]
        static bool LateUpdatePrefix() => false;

        [HarmonyPatch(nameof(Player.FixedUpdate))]
        [HarmonyPrefix]
        static bool FixedUpdatePrefix() => false;

        [HarmonyPatch(nameof(Player.OnChildTriggerEnter))]
        [HarmonyPrefix]
        static bool OnChildTriggerEnterPrefix() => false;

        [HarmonyPatch(nameof(Player.OnChildTriggerEnterMesh))]
        [HarmonyPrefix]
        static bool OnChildTriggerEnterMeshPrefix() => false;

        [HarmonyPatch(nameof(Player.OnChildTriggerStay))]
        [HarmonyPrefix]
        static bool OnChildTriggerStayPrefix() => false;

        [HarmonyPatch(nameof(Player.OnChildTriggerStayMesh))]
        [HarmonyPrefix]
        static bool OnChildTriggerStayMeshPrefix() => false;

        [HarmonyPatch(nameof(Player.BoostCooldownLoop))]
        [HarmonyPrefix]
        static bool BoostCooldownLoopPrefix()
        {
            CoreHelper.Log($"{nameof(Player.BoostCooldownLoop)}() Method invoked");
            return false;
        }

        [HarmonyPatch(nameof(Player.PlayerHit))]
        [HarmonyPrefix]
        static bool PlayerHitPrefix()
        {
            CoreHelper.Log($"{nameof(Player.PlayerHit)}() Method invoked");
            return false;
        }

        [HarmonyPatch(nameof(Player.Kill))]
        [HarmonyPrefix]
        static bool KillPrefix()
        {
            CoreHelper.Log($"{nameof(Player.Kill)}() Method invoked");
            return false;
        }

        [HarmonyPatch(nameof(Player.Spawn))]
        [HarmonyPrefix]
        static bool SpawnPrefix()
        {
            CoreHelper.Log($"{nameof(Player.Spawn)}() Method invoked");
            return false;
        }
    }
}
