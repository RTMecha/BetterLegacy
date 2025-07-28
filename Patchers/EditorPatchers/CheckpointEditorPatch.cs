using System;

using UnityEngine;

using HarmonyLib;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(CheckpointEditor))]
    public class CheckpointEditorPatch : MonoBehaviour
    {
        public static Checkpoint currentCheckpoint;
        public static CheckpointEditor Instance { get => CheckpointEditor.inst; set => CheckpointEditor.inst = value; }

        [HarmonyPatch(nameof(CheckpointEditor.Awake))]
        [HarmonyPrefix]
        static bool AwakePrefix(CheckpointEditor __instance)
        {
            if (Instance == null)
                Instance = __instance;
            else if (Instance != __instance)
            {
                Destroy(__instance.gameObject);
                return false;
            }

            __instance.className = "[<color=#65B6F7>CheckpointEditor</color>] \n"; // this is done due to the CheckpointEditor className being BackgroundEditor for some reason...

            CoreHelper.LogInit(__instance.className);

            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.Start))]
        [HarmonyPrefix]
        static bool StartPrefix() => false;
        
        [HarmonyPatch(nameof(CheckpointEditor.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix() => false;

        [HarmonyPatch(nameof(CheckpointEditor.OpenDialog))]
        [HarmonyPrefix]
        static bool OpenDialogPrefix(int __0)
        {
            RTCheckpointEditor.inst.SetCurrentCheckpoint(__0);
            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.CreateNewCheckpoint), new Type[] { })]
        [HarmonyPrefix]
        static bool CreateNewCheckpointPrefix()
        {
            RTCheckpointEditor.inst.CreateNewCheckpoint(EditorManager.inst.CurrentAudioPos, EventManager.inst.cam.transform.position);
            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.CreateNewCheckpoint), new Type[] { typeof(float), typeof(Vector2) })]
        [HarmonyPrefix]
        static bool CreateNewCheckpointPrefix(float __0, Vector2 __1)
        {
            RTCheckpointEditor.inst.CreateNewCheckpoint(__0, __1);
            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.DeleteCheckpoint))]
        [HarmonyPrefix]
        static bool DeleteCheckpointPrefix(int __0)
        {
            RTCheckpointEditor.inst.DeleteCheckpoint(__0);
            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.CreateGhostCheckpoints))]
        [HarmonyPrefix]
        static bool CreateGhostCheckpointsPrefix()
        {
            RTCheckpointEditor.inst.CreateGhostCheckpoints();
            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.CreateCheckpoints))]
        [HarmonyPrefix]
        static bool CreateCheckpointsPrefix()
        {
            RTCheckpointEditor.inst.CreateCheckpoints();
            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.RenderCheckpoint))]
        [HarmonyPrefix]
        static bool RenderCheckpointPrefix(int __0)
        {
            RTCheckpointEditor.inst.RenderCheckpoint(__0);
            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.RenderCheckpoints))]
        [HarmonyPrefix]
        static bool RenderCheckpointsPrefix()
        {
            if (!GameData.Current || !GameData.Current.data || GameData.Current.data.checkpoints == null)
                return false;

            for (int i = 0; i < GameData.Current.data.checkpoints.Count; i++)
                RTCheckpointEditor.inst.RenderCheckpoint(i);

            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.RenderCheckpointList))]
        [HarmonyPrefix]
        static bool RenderCheckpointListPrefix(string __0, int __1)
        {
            RTCheckpointEditor.inst.RenderCheckpointList();
            return false;
        }
    }
}
