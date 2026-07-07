using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Menus.UI.Interfaces;

namespace BetterLegacy.Menus.UI.Elements
{
    /// <summary>
    /// Base class used for handling image elements and other types in the interface. To be used either as a base for other elements or an image element on its own.
    /// </summary>
    public class MenuImage : Exists, IPacket
    {
        #region Values

        /// <summary>
        /// GameObject reference.
        /// </summary>
        public GameObject gameObject;

        /// <summary>
        /// Identification of the element.
        /// </summary>
        public string id;

        /// <summary>
        /// Name of the element.
        /// </summary>
        public string name;

        /// <summary>
        /// The layout to parent this element to.
        /// </summary>
        public string parentLayout;

        /// <summary>
        /// The element ID used to parent this element to another if <see cref="parentLayout"/> does not have an associated layout.
        /// </summary>
        public string parent;

        /// <summary>
        /// Unity's UI layering depends on an objects' sibling index, so it can be set via here if you want an element to appear below layouts.
        /// </summary>
        public int siblingIndex = -1;

        /// <summary>
        /// If the element should regenerate when the <see cref="BaseInterface.GenerateUI"/> coroutine is run. If <see cref="BaseInterface.regenerate"/> is true, then this will be ignored.
        /// </summary>
        public bool regenerate = true;

        /// <summary>
        /// While the interface is generating, should the interface wait while the element is spawning?
        /// </summary>
        public bool wait = true;

        /// <summary>
        /// Spawn length of the element, to be used for spacing each element out. Time is not always exactly a second.
        /// </summary>
        public float length = 1f;

        /// <summary>
        /// RectTransform values.
        /// </summary>
        public RectValues rect = RectValues.Default;

        /// <summary>
        /// Function JSON to parse whenever the element is clicked.
        /// </summary>
        public JSONNode funcJSON;

        /// <summary>
        /// Function JSON called whenever the element is clicked.
        /// </summary>
        public Action func;

        /// <summary>
        /// Function JSON to parse whenever the element is scrolled up on.
        /// </summary>
        public JSONNode onScrollUpFuncJSON;

        /// <summary>
        /// Function JSON to parse whenever the element is scrolled down on.
        /// </summary>
        public JSONNode onScrollDownFuncJSON;

        /// <summary>
        /// Function to call whenever the element is scrolled up on.
        /// </summary>
        public Action onScrollUpFunc;

        /// <summary>
        /// Function to call whenever the element is scrolled down on.
        /// </summary>
        public Action onScrollDownFunc;

        /// <summary>
        /// Function JSON to parse when the element spawns.
        /// </summary>
        public JSONNode spawnFuncJSON;

        /// <summary>
        /// Function called when the element spawns.
        /// </summary>
        public Action spawnFunc;

        /// <summary>
        /// Function JSON called when waiting on the element ends.
        /// </summary>
        public JSONNode onWaitEndFuncJSON;

        /// <summary>
        /// Function called when waiting on the element ends.
        /// </summary>
        public Action onWaitEndFunc;

        /// <summary>
        /// Function JSON to parse per tick.
        /// </summary>
        public JSONNode tickFuncJSON;

        /// <summary>
        /// Function to call per tick.
        /// </summary>
        public Action tickFunc;

        /// <summary>
        /// Interaction component.
        /// </summary>
        public Clickable clickable;

        /// <summary>
        /// Base image of the element.
        /// </summary>
        public Image image;

        /// <summary>
        /// Icon to apply to <see cref="image"/>, or <see cref="MenuText.iconUI"/> if the element type is <see cref="MenuText"/> or <see cref="MenuButton"/>.
        /// </summary>
        public Sprite icon;

        /// <summary>
        /// Icon's path to load if we must.
        /// </summary>
        public string iconPath;

        /// <summary>
        /// Opacity of the image.
        /// </summary>
        public float opacity = 1f;

        /// <summary>
        /// Theme color slot for the image to use.
        /// </summary>
        public int color;

        /// <summary>
        /// Hue color offset.
        /// </summary>
        public float hue;

        /// <summary>
        /// Saturation color offset.
        /// </summary>
        public float sat;

        /// <summary>
        /// Value color offset.
        /// </summary>
        public float val;

        /// <summary>
        /// If the current color should use <see cref="overrideColor"/> instead of a color slot based on <see cref="color"/>.
        /// </summary>
        public bool useOverrideColor;

        /// <summary>
        /// Custom color to use.
        /// </summary>
        public Color overrideColor;

        /// <summary>
        /// True if the element is spawning (playing spawn animations, etc), otherwise false.
        /// </summary>
        public bool isSpawning;

        /// <summary>
        /// Time elapsed since spawn.
        /// </summary>
        public float time;

        /// <summary>
        /// Time when element spawned.
        /// </summary>
        public float timeOffset;

        /// <summary>
        /// If the image should have a mask component added to it.
        /// </summary>
        public bool mask;

        /// <summary>
        /// If a "Blip" sound should play when the user clicks on the element.
        /// </summary>
        public bool playBlipSound;

        /// <summary>
        /// How rounded the element should be. If the value is 0, then the element is not rounded.
        /// </summary>
        public int rounded = 1;

        /// <summary>
        /// The side that should be rounded, if <see cref="rounded"/> if higher than 0.
        /// </summary>
        public SpriteHelper.RoundedSide roundedSide = SpriteHelper.RoundedSide.W;

        /// <summary>
        /// How many times the element should loop.
        /// </summary>
        public int loop;

        /// <summary>
        /// If the element was spawned from a loop.
        /// </summary>
        public bool fromLoop;

        /// <summary>
        /// Contains all reactive settings.
        /// </summary>
        public ReactiveSetting reactiveSetting;

        /// <summary>
        /// If the interface element was parsed.
        /// </summary>
        public bool parsed = false;

        /// <summary>
        /// If the element can be clicked.
        /// </summary>
        public bool selectable = true;

        /// <summary>
        /// Dictionary of cached variables.
        /// </summary>
        public Dictionary<string, JSONNode> cachedVariables;

        /// <summary>
        /// List of animations running on the element.
        /// </summary>
        public List<RTAnimation> animations = new List<RTAnimation>();

        #endregion

        #region Functions

        public static void ReadPacketList(List<MenuImage> elements, NetworkReader reader)
        {
            var elementCount = reader.ReadInt32();
            for (int i = 0; i < elementCount; i++)
            {
                var type = reader.ReadString();
                switch (type)
                {
                    case "inputfield": {
                            elements.Add(Packet.CreateFromPacket<MenuInputField>(reader));
                            break;
                        }
                    case "button": {
                            elements.Add(Packet.CreateFromPacket<MenuButton>(reader));
                            break;
                        }
                    case "text": {
                            elements.Add(Packet.CreateFromPacket<MenuText>(reader));
                            break;
                        }
                    case "event": {
                            elements.Add(Packet.CreateFromPacket<MenuEvent>(reader));
                            break;
                        }
                    default: {
                            elements.Add(Packet.CreateFromPacket<MenuImage>(reader));
                            break;
                        }
                }
            }
        }

        public static void WritePacketList(List<MenuImage> elements, NetworkWriter writer)
        {
            writer.Write(elements.Count);
            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (element is MenuInputField menuInputField)
                {
                    writer.Write("inputfield");
                    menuInputField.WritePacket(writer);
                    continue;
                }
                if (element is MenuButton menuButton)
                {
                    writer.Write("button");
                    menuButton.WritePacket(writer);
                    continue;
                }
                if (element is MenuText menuText)
                {
                    writer.Write("text");
                    menuText.WritePacket(writer);
                    continue;
                }
                if (element is MenuEvent menuEvent)
                {
                    writer.Write("event");
                    menuEvent.WritePacket(writer);
                    continue;
                }
                writer.Write("image");
                element.WritePacket(writer);
            }
        }

        public virtual void ReadPacket(NetworkReader reader)
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

        public virtual void WritePacket(NetworkWriter writer)
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
        /// Creates a new MenuImage element with all the same values as <paramref name="orig"/>.
        /// </summary>
        /// <param name="orig">The element to copy.</param>
        /// <param name="newID">If a new ID should be generated.</param>
        /// <returns>Returns a copied MenuImage element.</returns>
        public static MenuImage DeepCopy(MenuImage orig, bool newID = true) => new MenuImage
        {
            #region Base

            id = newID ? LSText.randomNumString(16) : orig.id,
            name = orig.name,
            parentLayout = orig.parentLayout,
            parent = orig.parent,
            siblingIndex = orig.siblingIndex,

            #endregion

            #region Spawning

            regenerate = orig.regenerate,
            fromLoop = false, // if element has been spawned from the loop or if its the first / only of its kind.
            loop = orig.loop,

            #endregion

            #region UI

            icon = orig.icon,
            iconPath = orig.iconPath,
            rect = orig.rect,
            rounded = orig.rounded, // roundness can be prevented by setting rounded to 0.
            roundedSide = orig.roundedSide, // default side should be Whole.
            mask = orig.mask,
            reactiveSetting = orig.reactiveSetting,

            #endregion

            #region Color

            color = orig.color,
            opacity = orig.opacity,
            hue = orig.hue,
            sat = orig.sat,
            val = orig.val,

            overrideColor = orig.overrideColor,
            useOverrideColor = orig.useOverrideColor,

            #endregion

            #region Anim

            wait = orig.wait,
            length = orig.length,

            #endregion

            #region Func

            playBlipSound = orig.playBlipSound,
            selectable = orig.selectable,
            funcJSON = orig.funcJSON, // function to run when the element is clicked.
            onScrollUpFuncJSON = orig.onScrollUpFuncJSON,
            onScrollDownFuncJSON = orig.onScrollDownFuncJSON,
            spawnFuncJSON = orig.spawnFuncJSON, // function to run when the element spawns.
            onWaitEndFuncJSON = orig.onWaitEndFuncJSON,
            tickFuncJSON = orig.tickFuncJSON,
            func = orig.func,
            onScrollUpFunc = orig.onScrollUpFunc,
            onScrollDownFunc = orig.onScrollDownFunc,
            spawnFunc = orig.spawnFunc,
            onWaitEndFunc = orig.onWaitEndFunc,
            tickFunc = orig.tickFunc,

            #endregion
        };

        /// <summary>
        /// Runs when the elements' GameObject has been created.
        /// </summary>
        public virtual void Spawn()
        {
            isSpawning = length != 0f;
            timeOffset = Time.time;
        }

        /// <summary>
        /// Runs while the element is spawning.
        /// </summary>
        public virtual void UpdateSpawnCondition()
        {
            time += (Time.time - timeOffset) * InterfaceManager.InterfaceSpeed;

            timeOffset = Time.time;

            if (time > length)
                isSpawning = false;
        }

        /// <summary>
        /// Clears any external or internal data.
        /// </summary>
        public virtual void Clear()
        {
            for (int i = 0; i < animations.Count; i++)
                AnimationManager.inst.Remove(animations[i].id);
            animations.Clear();
        }

        #region JSON

        /// <summary>
        /// Parses a <see cref="MenuImage"/> interface element from JSON.
        /// </summary>
        /// <param name="jnElement">JSON to parse.</param>
        /// <param name="j">Loop index.</param>
        /// <param name="loop">How many times the element is set to loop.</param>
        /// <param name="spriteAssets">Sprite assets.</param>
        /// <param name="customVariables">Passed custom variables.</param>
        /// <returns>Returns a parsed interface element.</returns>
        public static MenuImage Parse(JSONNode jnElement, int j, int loop, Dictionary<string, Sprite> spriteAssets, Dictionary<string, JSONNode> customVariables = null)
        {
            var element = new MenuImage();
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
        public virtual void Read(JSONNode jnElement, int j, int loop, Dictionary<string, Sprite> spriteAssets, Dictionary<string, JSONNode> customVariables = null)
        {
            #region Base

            if (!string.IsNullOrEmpty(jnElement["id"]))
                id = jnElement["id"];
            if (string.IsNullOrEmpty(id))
                id = LSText.randomNumString(16);
            var jnName = InterfaceManager.inst.ParseVarFunction(jnElement["name"], this, customVariables);
            if (jnName != null && jnName.IsString)
                name = jnName;
            var jnParentLayout = InterfaceManager.inst.ParseVarFunction(jnElement["parent_layout"], this, customVariables);
            if (jnParentLayout != null && jnParentLayout.IsString)
                parentLayout = jnParentLayout;
            var jnParent = InterfaceManager.inst.ParseVarFunction(jnElement["parent"], this, customVariables);
            if (jnParent != null && jnParent.IsString)
                parent = jnParent;
            var jnSiblingIndex = InterfaceManager.inst.ParseVarFunction(jnElement["sibling_index"], this, customVariables);
            if (jnSiblingIndex != null)
                siblingIndex = jnSiblingIndex.AsInt;

            cachedVariables = customVariables;

            #endregion

            #region Spawning

            var jnRegen = InterfaceManager.inst.ParseVarFunction(jnElement["regen"], this, customVariables);
            if (jnRegen != null)
                regenerate = jnRegen.AsBool;
            fromLoop = j > 0; // if element has been spawned from the loop or if its the first / only of its kind.
            this.loop = loop;

            #endregion

            #region UI

            var jnIcon = InterfaceManager.inst.ParseVarFunction(jnElement["icon"], this, customVariables);
            if (jnIcon != null && jnIcon.IsString)
                icon = jnIcon != null ? spriteAssets != null && spriteAssets.TryGetValue(jnIcon, out Sprite sprite) ? sprite : SpriteHelper.StringToSprite(jnIcon) : null;
            var jnIconPath = InterfaceManager.inst.ParseVarFunction(jnElement["icon_path"], this, customVariables);
            if (jnIconPath != null && jnIconPath.IsString)
                iconPath = jnIconPath;
            var jnRect = InterfaceManager.inst.ParseVarFunction(jnElement["rect"], this, customVariables);
            if (jnRect != null)
                rect = RectValues.TryParse(jnRect, RectValues.Default);
            var jnRounded = InterfaceManager.inst.ParseVarFunction(jnElement["rounded"], this, customVariables);
            if (jnRounded != null)
                rounded = jnRounded.AsInt; // roundness can be prevented by setting rounded to 0.
            var jnRoundedSide = InterfaceManager.inst.ParseVarFunction(jnElement["rounded_side"], this, customVariables);
            if (jnRoundedSide != null)
                roundedSide = (SpriteHelper.RoundedSide)jnRoundedSide.AsInt; // default side should be Whole.
            var jnMask = InterfaceManager.inst.ParseVarFunction(jnElement["mask"], this, customVariables);
            if (jnMask != null)
                mask = jnMask.AsBool;
            var jnReactive = InterfaceManager.inst.ParseVarFunction(jnElement["reactive"], this, customVariables);
            if (jnReactive != null)
                reactiveSetting = ReactiveSetting.Parse(jnReactive, j);

            #endregion

            #region Color

            var jnCol = InterfaceManager.inst.ParseVarFunction(jnElement["col"], this, customVariables);
            if (jnCol != null)
                color = jnCol.AsInt;
            var jnOpacity = InterfaceManager.inst.ParseVarFunction(jnElement["opacity"], this, customVariables);
            if (jnOpacity != null)
                opacity = jnOpacity.AsFloat;
            var jnHue = InterfaceManager.inst.ParseVarFunction(jnElement["hue"], this, customVariables);
            if (jnHue != null)
                hue = jnHue.AsFloat;
            var jnSat = InterfaceManager.inst.ParseVarFunction(jnElement["sat"], this, customVariables);
            if (jnSat != null)
                sat = jnSat.AsFloat;
            var jnVal = InterfaceManager.inst.ParseVarFunction(jnElement["val"], this, customVariables);
            if (jnVal != null)
                val = jnVal.AsFloat;
            var jnOverrideCol = InterfaceManager.inst.ParseVarFunction(jnElement["override_col"], this, customVariables);
            useOverrideColor = jnOverrideCol != null;
            if (useOverrideColor)
                overrideColor = RTColors.HexToColor(jnOverrideCol);

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
            var jnSelectable = InterfaceManager.inst.ParseVarFunction(jnElement["selectable"], this, customVariables);
            if (jnSelectable != null)
                selectable = jnSelectable.AsBool;
            var jnFunc = InterfaceManager.inst.ParseVarFunction(jnElement["func"], this, customVariables);
            if (jnFunc != null)
                funcJSON = jnFunc; // function to run when the element is clicked.
            var jnOnScrollUpFunc = InterfaceManager.inst.ParseVarFunction(jnElement["on_scroll_up_func"], this, customVariables);
            if (jnOnScrollUpFunc != null)
                onScrollUpFuncJSON = jnOnScrollUpFunc; // function to run when the element is scrolled on.
            var jnOnScrollDownFunc = InterfaceManager.inst.ParseVarFunction(jnElement["on_scroll_down_func"], this, customVariables);
            if (jnOnScrollDownFunc != null)
                onScrollDownFuncJSON = jnOnScrollDownFunc; // function to run when the element is scrolled on.
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

        #endregion

        /// <summary>
        /// Sets a transform value of the element, depending on type and axis specified.
        /// </summary>
        /// <param name="type">The type of transform to set. 0 = position, 1 = scale, 2 = rotation.</param>
        /// <param name="axis">The axis to set. 0 = X, 1 = Y, 2 = Z.</param>
        /// <param name="value">The value to set the type and axis to.</param>
        public void SetTransform(int type, int axis, float value)
        {
            if (!gameObject)
                return;

            switch (type)
            {
                case 0: gameObject.transform.SetLocalPosition(axis, value); break;
                case 1: gameObject.transform.SetLocalScale(axis, value); break;
                case 2: gameObject.transform.SetLocalRotationEuler(axis, value); break;
            }
        }

        /// <summary>
        /// Gets a transform value from the element, depending on the type and axis specified.
        /// </summary>
        /// <param name="type">The type of transform to set. 0 = position, 1 = scale, 2 = rotation.</param>
        /// <param name="axis">The axis to set. 0 = X, 1 = Y, 2 = Z.</param>
        /// <returns>Returns the transform value from the type and axis.</returns>
        public float GetTransform(int type, int axis) => !gameObject || axis < 0 || axis > 2 ? 0f : type switch
        {
            0 => gameObject.transform.localPosition[axis],
            1 => gameObject.transform.localScale[axis],
            2 => gameObject.transform.localEulerAngles[axis],
            _ => 0f
        };

        public override string ToString() => $"{id} - {name}";

        #endregion
    }
}
