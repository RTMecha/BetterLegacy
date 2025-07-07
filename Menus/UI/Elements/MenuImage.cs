using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Menus.UI.Interfaces;

namespace BetterLegacy.Menus.UI.Elements
{
    /// <summary>
    /// Base class used for handling image elements and other types in the interface. To be used either as a base for other elements or an image element on its own.
    /// </summary>
    public class MenuImage : Exists
    {
        #region Public Fields

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
        /// If the element should regenerate when the <see cref="MenuBase.GenerateUI"/> coroutine is run. If <see cref="MenuBase.regenerate"/> is true, then this will be ignored.
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

        public bool parsed = false;

        /// <summary>
        /// If the element can be clicked.
        /// </summary>
        public bool selectable = true;

        #endregion

        #region Private Fields

        public List<RTAnimation> animations = new List<RTAnimation>();

        #endregion

        #region Methods

        /// <summary>
        /// Provides a way to see the object in UnityExplorer.
        /// </summary>
        /// <returns>A string containing the objects' ID and name.</returns>
        public override string ToString() => $"{id} - {name}";

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

        #region Functions

        #endregion

        #region JSON

        public static MenuImage Parse(JSONNode jnElement, int j, int loop, Dictionary<string, Sprite> spriteAssets)
        {
            var element = new MenuImage();
            element.Read(jnElement, j, loop, spriteAssets);
            element.parsed = true;
            return element;
        }

        public virtual void Read(JSONNode jnElement, int j, int loop, Dictionary<string, Sprite> spriteAssets)
        {
            #region Base

            if (!string.IsNullOrEmpty(jnElement["id"]))
                id = jnElement["id"];
            if (string.IsNullOrEmpty(id))
                id = LSText.randomNumString(16);
            if (!string.IsNullOrEmpty(jnElement["name"]))
                name = InterfaceManager.inst.ParseVarFunction(jnElement["name"], this);
            if (!string.IsNullOrEmpty(jnElement["parent_layout"]))
                parentLayout = InterfaceManager.inst.ParseVarFunction(jnElement["parent_layout"], this);
            if (!string.IsNullOrEmpty(jnElement["parent"]))
                parent = InterfaceManager.inst.ParseVarFunction(jnElement["parent"], this);
            if (jnElement["sibling_index"] != null)
                siblingIndex = InterfaceManager.inst.ParseVarFunction(jnElement["sibling_index"], this).AsInt;

            #endregion

            #region Spawning

            if (jnElement["regen"] != null)
                regenerate = InterfaceManager.inst.ParseVarFunction(jnElement["regen"], this).AsBool;
            fromLoop = j > 0; // if element has been spawned from the loop or if its the first / only of its kind.
            this.loop = loop;

            #endregion

            #region UI

            if (!string.IsNullOrEmpty(jnElement["icon"]))
            {
                var jnIcon = InterfaceManager.inst.ParseVarFunction(jnElement["icon"], this);
                icon = jnIcon != null ? spriteAssets != null && spriteAssets.TryGetValue(jnIcon, out Sprite sprite) ? sprite : SpriteHelper.StringToSprite(jnIcon) : null;
            }
            if (!string.IsNullOrEmpty(jnElement["icon_path"]))
                iconPath = InterfaceManager.inst.ParseVarFunction(jnElement["icon_path"], this);
            if (jnElement["rect"] != null)
                rect = RectValues.TryParse(InterfaceManager.inst.ParseVarFunction(jnElement["rect"], this), RectValues.Default);
            if (jnElement["rounded"] != null)
                rounded = InterfaceManager.inst.ParseVarFunction(jnElement["rounded"], this).AsInt; // roundness can be prevented by setting rounded to 0.
            if (jnElement["rounded_side"] != null)
                roundedSide = (SpriteHelper.RoundedSide)InterfaceManager.inst.ParseVarFunction(jnElement["rounded_side"], this).AsInt; // default side should be Whole.
            if (jnElement["mask"] != null)
                mask = InterfaceManager.inst.ParseVarFunction(jnElement["mask"], this).AsBool;
            if (jnElement["reactive"] != null)
                reactiveSetting = ReactiveSetting.Parse(InterfaceManager.inst.ParseVarFunction(jnElement["reactive"], this), j);

            #endregion

            #region Color

            if (jnElement["col"] != null)
                color = InterfaceManager.inst.ParseVarFunction(jnElement["col"], this).AsInt;
            if (jnElement["opacity"] != null)
                opacity = InterfaceManager.inst.ParseVarFunction(jnElement["opacity"], this).AsFloat;
            if (jnElement["hue"] != null)
                hue = InterfaceManager.inst.ParseVarFunction(jnElement["hue"], this).AsFloat;
            if (jnElement["sat"] != null)
                sat = InterfaceManager.inst.ParseVarFunction(jnElement["sat"], this).AsFloat;
            if (jnElement["val"] != null)
                val = InterfaceManager.inst.ParseVarFunction(jnElement["val"], this).AsFloat;
            useOverrideColor = jnElement["override_col"] != null;
            if (useOverrideColor)
                overrideColor = LSColors.HexToColorAlpha(InterfaceManager.inst.ParseVarFunction(jnElement["override_col"], this));

            #endregion

            #region Anim

            if (jnElement["wait"] != null)
                wait = InterfaceManager.inst.ParseVarFunction(jnElement["wait"], this).AsBool;
            if (jnElement["anim_length"] != null)
                length = InterfaceManager.inst.ParseVarFunction(jnElement["anim_length"], this).AsFloat;
            else if (!parsed)
                length = 0f;

            #endregion

            #region Func

            if (jnElement["play_blip_sound"] != null)
                playBlipSound = InterfaceManager.inst.ParseVarFunction(jnElement["play_blip_sound"], this).AsBool;
            if (jnElement["selectable"] != null)
                selectable = InterfaceManager.inst.ParseVarFunction(jnElement["selectable"], this).AsBool;
            if (jnElement["func"] != null)
                funcJSON = InterfaceManager.inst.ParseVarFunction(jnElement["func"], this); // function to run when the element is clicked.
            if (jnElement["on_scroll_up_func"] != null)
                onScrollUpFuncJSON = InterfaceManager.inst.ParseVarFunction(jnElement["on_scroll_up_func"], this); // function to run when the element is scrolled on.
            if (jnElement["on_scroll_down_func"] != null)
                onScrollDownFuncJSON = InterfaceManager.inst.ParseVarFunction(jnElement["on_scroll_down_func"], this); // function to run when the element is scrolled on.
            if (jnElement["spawn_func"] != null)
                spawnFuncJSON = InterfaceManager.inst.ParseVarFunction(jnElement["spawn_func"], this); // function to run when the element spawns.
            if (jnElement["on_wait_end_func"] != null)
                onWaitEndFuncJSON = InterfaceManager.inst.ParseVarFunction(jnElement["on_wait_end_func"], this);
            if (jnElement["tick_func"] != null)
                tickFuncJSON = InterfaceManager.inst.ParseVarFunction(jnElement["tick_func"], this);

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

        #endregion
    }
}
