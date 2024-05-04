using System.Collections.Generic;

using UnityEngine;

namespace BetterLegacy.Core.Managers
{
    /// <summary>
    /// This class is used to share mod variables and functions, as well as check if a mod is installed.
    /// </summary>
    public class ModCompatibility : MonoBehaviour
    {
        public static ModCompatibility inst;

        public static GameObject bepinex;

        public static Dictionary<string, object> sharedFunctions = new Dictionary<string, object>();

        void Awake()
        {
            inst = this;

            bepinex = GameObject.Find("BepInEx_Manager");

            UnityExplorerInstalled = bepinex.GetComponentByName("ExplorerBepInPlugin");
        }

        public static bool UnityExplorerInstalled { get; private set; }
    }
}
