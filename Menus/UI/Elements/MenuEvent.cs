using System.Collections.Generic;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

namespace BetterLegacy.Menus.UI.Elements
{
    /// <summary>
    /// Element only used for automatically triggering a function when spawned.
    /// </summary>
    public class MenuEvent : MenuImage
    {
        /// <summary>
        /// Automatically runs the <see cref="MenuImage.ParseFunction(SimpleJSON.JSONNode)"/> method.
        /// </summary>
        public void TriggerEvent()
        {
            func?.Invoke();
            if (funcJSON != null)
                InterfaceManager.inst.ParseFunction(funcJSON, this, cachedVariables);
            Spawn();
        }

        /// <summary>
        /// Parses a <see cref="MenuEvent"/> interface element from JSON.
        /// </summary>
        /// <param name="jnElement">JSON to parse.</param>
        /// <param name="j">Loop index.</param>
        /// <param name="loop">How many times the element is set to loop.</param>
        /// <param name="spriteAssets">Sprite assets.</param>
        /// <param name="customVariables">Passed custom variables.</param>
        /// <returns>Returns a parsed interface element.</returns>
        public static new MenuEvent Parse(JSONNode jnElement, int j, int loop, Dictionary<string, Sprite> spriteAssets, Dictionary<string, JSONNode> customVariables = null)
        {
            var element = new MenuEvent();
            element.Read(jnElement, j, loop, spriteAssets, customVariables);
            element.parsed = true;
            return element;
        }

        /// <summary>
        /// Reads interface element data from JSON.
        /// </summary>
        /// <param name="jnElement">JSON to read.</param>
        /// <param name="j">Loop index.</param>
        /// <param name="loop">How many times the element is set to loop.</param>
        /// <param name="spriteAssets">Sprite assets.</param>
        /// <param name="customVariables">Passed custom variables.</param>
        public override void Read(JSONNode jnElement, int j, int loop, Dictionary<string, Sprite> spriteAssets, Dictionary<string, JSONNode> customVariables = null)
        {
            #region Base

            if (!string.IsNullOrEmpty(jnElement["id"]))
                id = jnElement["id"];
            if (string.IsNullOrEmpty(id))
                id = LSText.randomNumString(16);
            var jnName = InterfaceManager.inst.ParseVarFunction(jnElement["name"], this, customVariables);
            if (jnName != null && jnName.IsString)
                name = jnName;

            cachedVariables = customVariables;

            #endregion

            #region Spawning

            var jnRegen = InterfaceManager.inst.ParseVarFunction(jnElement["regen"], this, customVariables);
            if (jnRegen != null)
                regenerate = jnRegen.AsBool;
            fromLoop = j > 0; // if element has been spawned from the loop or if its the first / only of its kind.
            this.loop = loop;

            #endregion

            #region Anim

            var jnWait = InterfaceManager.inst.ParseVarFunction(jnElement["wait"], this, customVariables);
            if (jnWait != null)
                wait = jnWait.AsBool;
            var jnAnimLength = InterfaceManager.inst.ParseVarFunction(jnElement["anim_length"], this, customVariables);
            if (jnAnimLength != null)
                length = jnAnimLength.AsFloat;
            else if (!parsed)
                length = 0f;

            #endregion

            #region Func

            var jnPlayBlipSound = InterfaceManager.inst.ParseVarFunction(jnElement["play_blip_sound"], this, customVariables);
            if (jnPlayBlipSound != null)
                playBlipSound = jnPlayBlipSound.AsBool;
            var jnFunc = InterfaceManager.inst.ParseVarFunction(jnElement["func"], this, customVariables);
            if (jnFunc != null)
                funcJSON = jnFunc; // function to run when the element is clicked.
            var jnSpawnFunc = InterfaceManager.inst.ParseVarFunction(jnElement["spawn_func"], this, customVariables);
            if (jnSpawnFunc != null)
                spawnFuncJSON = jnSpawnFunc; // function to run when the element spawns.
            var jnOnWaitEndFunc = InterfaceManager.inst.ParseVarFunction(jnElement["on_wait_end_func"], this, customVariables);
            if (jnOnWaitEndFunc != null)
                onWaitEndFuncJSON = jnOnWaitEndFunc;
            var jnTickFunc = InterfaceManager.inst.ParseVarFunction(jnElement["tick_func"], this, customVariables);
            if (jnTickFunc != null)
                tickFuncJSON = jnTickFunc;

            #endregion
        }

        public static MenuEvent DeepCopy(MenuEvent orig, bool newID = true) => (MenuEvent)MenuImage.DeepCopy(orig, newID);
    }
}
