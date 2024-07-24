using HarmonyLib;
using System.Collections.Generic;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(FileManager))]
    public class FileManagerPatch
    {
        [HarmonyPatch(nameof(FileManager.LoadImageFileRaw), MethodType.Enumerator)]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> LoadImageFileRawTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var match = new CodeMatcher(instructions).Start();

            match = match.RemoveInstructionsInRange(108, 120);

            return match.InstructionEnumeration();
        }
    }
}
