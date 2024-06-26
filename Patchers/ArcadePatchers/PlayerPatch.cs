using BetterLegacy.Core.Helpers;
using HarmonyLib;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(Player))]
    public class PlayerPatch
    {
        [HarmonyPatch("StopMovement")]
        [HarmonyPrefix]
        static bool StopMovementPrefix(Player __instance)
        {
            CoreHelper.Log("StopMovement() Method invoked");
            return false;
        }

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static bool StartPrefix(Player __instance)
        {
            CoreHelper.Log("Start() Method invoked");
            return false;
        }

        [HarmonyPatch("SetColor")]
        [HarmonyPrefix]
        static bool SetColorPrefix()
        {
            return false;
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool UpdatePrefix()
        {
            return false;
        }

        [HarmonyPatch("LateUpdate")]
        [HarmonyPrefix]
        static bool LateUpdatePrefix()
        {
            return false;
        }

        [HarmonyPatch("FixedUpdate")]
        [HarmonyPrefix]
        static bool FixedUpdatePrefix()
        {
            return false;
        }

        [HarmonyPatch("OnChildTriggerEnter")]
        [HarmonyPrefix]
        static bool OnChildTriggerEnterPrefix()
        {
            return false;
        }

        [HarmonyPatch("OnChildTriggerEnterMesh")]
        [HarmonyPrefix]
        static bool OnChildTriggerEnterMeshPrefix()
        {
            return false;
        }

        [HarmonyPatch("OnChildTriggerStay")]
        [HarmonyPrefix]
        static bool OnChildTriggerStayPrefix()
        {
            return false;
        }

        [HarmonyPatch("OnChildTriggerStayMesh")]
        [HarmonyPrefix]
        static bool OnChildTriggerStayMeshPrefix()
        {
            return false;
        }

        [HarmonyPatch("BoostCooldownLoop")]
        [HarmonyPrefix]
        static bool BoostCooldownLoopPrefix()
        {
            CoreHelper.Log("BoostCooldownLoop() Method invoked");
            return false;
        }

        [HarmonyPatch("PlayerHit")]
        [HarmonyPrefix]
        static bool PlayerHitPrefix()
        {
            CoreHelper.Log("PlayerHit() Method invoked");
            return false;
        }

        [HarmonyPatch("Kill")]
        [HarmonyPrefix]
        static bool KillPrefix()
        {
            CoreHelper.Log("Kill() Method invoked");
            return false;
        }

        [HarmonyPatch("Spawn")]
        [HarmonyPrefix]
        static bool SpawnPrefix()
        {
            CoreHelper.Log("Spawn() Method invoked");
            return false;
        }
    }
}
