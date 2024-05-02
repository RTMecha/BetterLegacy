using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Patchers
{
	[HarmonyPatch(typeof(FileManager))]
    public class FileManagerPatch
	{
		[HarmonyPatch(typeof(FileManager), "LoadImageFileRaw", MethodType.Enumerator)]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> LoadImageFileRawTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			var match = new CodeMatcher(instructions).Start();

			match = match.RemoveInstructionsInRange(108, 120);

			return match.InstructionEnumeration();
		}

	}
}
