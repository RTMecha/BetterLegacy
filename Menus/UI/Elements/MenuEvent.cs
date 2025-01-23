using LSFunctions;
using SimpleJSON;
using System.Collections.Generic;
using UnityEngine;

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
                InterfaceManager.inst.ParseFunction(funcJSON, this);
            Spawn();
        }

        public static new MenuEvent Parse(JSONNode jnElement, int j, int loop, Dictionary<string, Sprite> spriteAssets)
        {
            var element = new MenuEvent();
            element.Read(jnElement, j, loop, spriteAssets);
            element.parsed = true;
            return element;
        }

        public override void Read(JSONNode jnElement, int j, int loop, Dictionary<string, Sprite> spriteAssets)
        {
            #region Base

            if (!string.IsNullOrEmpty(jnElement["id"]))
                id = jnElement["id"];
            if (string.IsNullOrEmpty(id))
                id = LSText.randomNumString(16);
            if (!string.IsNullOrEmpty(jnElement["name"]))
                name = jnElement["name"];

            #endregion

            #region Spawning

            if (jnElement["regen"] != null)
                regenerate = jnElement["regen"].AsBool;
            fromLoop = j > 0; // if element has been spawned from the loop or if its the first / only of its kind.
            this.loop = loop;

            #endregion

            #region Anim

            if (jnElement["wait"] != null)
                wait = jnElement["wait"].AsBool;
            if (jnElement["anim_length"] != null)
                length = jnElement["anim_length"].AsFloat;
            else if (!parsed)
                length = 0f;

            #endregion

            #region Func

            if (jnElement["play_blip_sound"] != null)
                playBlipSound = jnElement["play_blip_sound"].AsBool;
            if (jnElement["func"] != null)
                funcJSON = jnElement["func"]; // function to run when the element is clicked.
            if (jnElement["spawn_func"] != null)
                spawnFuncJSON = jnElement["spawn_func"]; // function to run when the element spawns.
            if (jnElement["tick_func"] != null)
                tickFuncJSON = jnElement["tick_func"];

            #endregion
        }

        public static MenuEvent DeepCopy(MenuEvent orig, bool newID = true) => (MenuEvent)MenuImage.DeepCopy(orig, newID);
    }
}
