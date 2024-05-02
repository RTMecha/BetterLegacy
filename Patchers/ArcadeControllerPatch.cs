using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using UnityEngine;

using LSFunctions;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core;
using BetterLegacy.Core.Managers;

namespace BetterLegacy.Patchers
{
	[HarmonyPatch(typeof(ArcadeController))]
    public class ArcadeControllerPatch
    {
		[HarmonyPatch("Start")]
		[HarmonyPrefix]
		static bool StartPrefix(ArcadeController __instance)
        {
			CoreHelper.Log("Trying to generate new arcade UI...");

			if (ArcadeHelper.buttonPrefab == null)
            {
				ArcadeHelper.buttonPrefab = __instance.ic.ButtonPrefab.Duplicate(null);
				UnityEngine.Object.DontDestroyOnLoad(ArcadeHelper.buttonPrefab);
			}

			InputDataManager.inst.playersCanJoin = false;
			RTArcade.ReloadMenu();

			return true;
        }
	}
}
