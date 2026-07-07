using System.Collections.Generic;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Helpers;

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

        public override void ReadPacket(NetworkReader reader)
        {
            #region Base

            id = reader.ReadString();
            name = reader.ReadString();
            parentLayout = reader.ReadString();
            parent = reader.ReadString();
            siblingIndex = reader.ReadInt32();

            #endregion

            #region Spawning

            regenerate = reader.ReadBoolean();
            fromLoop = false; // if element has been spawned from the loop or if its the first / only of its kind.
            loop = reader.ReadInt32();

            #endregion

            #region UI

            icon = reader.ReadSprite();
            iconPath = reader.ReadString();
            rect = Packet.CreateFromPacket<RectValues>(reader);
            rounded = reader.ReadInt32(); // roundness can be prevented by setting rounded to 0.
            roundedSide = (SpriteHelper.RoundedSide)reader.ReadInt32(); // default side should be Whole.
            mask = reader.ReadBoolean();
            reactiveSetting = Packet.CreateFromPacket<ReactiveSetting>(reader);

            #endregion

            #region Color

            color = reader.ReadInt32();
            opacity = reader.ReadSingle();
            hue = reader.ReadSingle();
            sat = reader.ReadSingle();
            val = reader.ReadSingle();

            overrideColor = reader.ReadColor();
            useOverrideColor = reader.ReadBoolean();

            #endregion

            #region Anim

            wait = reader.ReadBoolean();
            length = reader.ReadSingle();

            #endregion

            #region Func

            playBlipSound = reader.ReadBoolean();
            selectable = reader.ReadBoolean();
            funcJSON = reader.ReadJSON(); // function to run when the element is clicked.
            onScrollUpFuncJSON = reader.ReadJSON();
            onScrollDownFuncJSON = reader.ReadJSON();
            spawnFuncJSON = reader.ReadJSON(); // function to run when the element spawns.
            onWaitEndFuncJSON = reader.ReadJSON();
            tickFuncJSON = reader.ReadJSON();
            //func = orig.func,
            //onScrollUpFunc = orig.onScrollUpFunc,
            //onScrollDownFunc = orig.onScrollDownFunc,
            //spawnFunc = orig.spawnFunc,
            //onWaitEndFunc = orig.onWaitEndFunc,
            //tickFunc = orig.tickFunc,

            #endregion
        }

        public override void WritePacket(NetworkWriter writer)
        {
            #region Base

            writer.Write(id);
            writer.Write(name);
            writer.Write(parentLayout);
            writer.Write(parent);
            writer.Write(siblingIndex);

            #endregion

            #region Spawning

            writer.Write(regenerate);
            writer.Write(loop);

            #endregion

            #region UI

            writer.Write(icon);
            writer.Write(iconPath);
            rect.WritePacket(writer);
            writer.Write(rounded);
            writer.Write((int)roundedSide);
            writer.Write(mask);
            reactiveSetting.WritePacket(writer);

            #endregion

            #region Color

            writer.Write(color);
            writer.Write(opacity);
            writer.Write(hue);
            writer.Write(sat);
            writer.Write(val);

            writer.Write(overrideColor);
            writer.Write(useOverrideColor);

            #endregion

            #region Anim

            writer.Write(wait);
            writer.Write(length);

            #endregion

            #region Func

            writer.Write(playBlipSound);
            writer.Write(selectable);
            writer.Write(funcJSON);
            writer.Write(onScrollUpFuncJSON);
            writer.Write(onScrollDownFuncJSON);
            writer.Write(spawnFuncJSON);
            writer.Write(onWaitEndFuncJSON);
            writer.Write(tickFuncJSON);

            #endregion
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
