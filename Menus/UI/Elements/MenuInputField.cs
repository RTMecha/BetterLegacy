﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Menus.UI.Elements
{
    /// <summary>
    /// Class for handling input field elements in the interface. Based on <see cref="MenuImage"/>.
    /// </summary>
    public class MenuInputField : MenuImage
    {
        #region Public Fields

        /// <summary>
        /// The input field component of the element.
        /// </summary>
        public InputField inputField;

        /// <summary>
        /// Text to display when the InputField is empty.
        /// </summary>
        public string placeholder;

        /// <summary>
        /// The text that has been written to the InputField.
        /// </summary>
        public string text;

        /// <summary>
        /// Function to run when the user is typing in the InputField.
        /// </summary>
        public Action<string> valueChangedFunc;

        /// <summary>
        /// Function to run when the user is finished typing.
        /// </summary>
        public Action<string> endEditFunc;

        /// <summary>
        /// Function JSON to parse whenever the element is typed in.
        /// </summary>
        public JSONNode valueChangedFuncJSON;

        /// <summary>
        /// Function JSON to parse when the user is finished typing.
        /// </summary>
        public JSONNode endEditFuncJSON;

        public string varName;

        /// <summary>
        /// Alignment of the main text.
        /// </summary>
        public TextAnchor textAnchor = TextAnchor.MiddleLeft;

        /// <summary>
        /// Alignement of the placeholder text.
        /// </summary>
        public TextAnchor placeholderAnchor = TextAnchor.MiddleLeft;

        /// <summary>
        /// Font size of the main text.
        /// </summary>
        public int textFontSize = 20;

        /// <summary>
        /// Font size of the placeholder text.
        /// </summary>
        public int placeholderFontSize = 20;

        /// <summary>
        /// Triggers for use when scrolling on the InputField.
        /// </summary>
        public UnityEngine.EventSystems.EventTrigger.Entry[] triggers;

        /// <summary>
        /// Theme color slot for the text to use.
        /// </summary>
        public int textColor;

        /// <summary>
        /// Hue color offset.
        /// </summary>
        public float textHue;

        /// <summary>
        /// Saturation color offset.
        /// </summary>
        public float textSat;

        /// <summary>
        /// Value color offset.
        /// </summary>
        public float textVal;

        /// <summary>
        /// If the current text color should use <see cref="overrideTextColor"/> instead of a color slot based on <see cref="textColor"/>.
        /// </summary>
        public bool useOverrideTextColor;

        /// <summary>
        /// Custom color to use.
        /// </summary>
        public Color overrideTextColor;

        /// <summary>
        /// Theme color slot for the placeholder to use.
        /// </summary>
        public int placeholderColor;

        /// <summary>
        /// Hue color offset.
        /// </summary>
        public float placeholderHue;

        /// <summary>
        /// Saturation color offset.
        /// </summary>
        public float placeholderSat;

        /// <summary>
        /// Value color offset.
        /// </summary>
        public float placeholderVal;

        /// <summary>
        /// If the current placeholder color should use <see cref="overridePlaceholderColor"/> instead of a color slot based on <see cref="placeholderColor"/>.
        /// </summary>
        public bool useOverridePlaceholderColor;

        /// <summary>
        /// Custom color to use.
        /// </summary>
        public Color overridePlaceholderColor;

        #endregion

        #region Methods

        /// <summary>
        /// Creates a new MenuText element with all the same values as <paramref name="orig"/>.
        /// </summary>
        /// <param name="orig">The element to copy.</param>
        /// <param name="newID">If a new ID should be generated.</param>
        /// <returns>Returns a copied MenuInputField element.</returns>
        public static MenuInputField DeepCopy(MenuInputField orig, bool newID = true) => new MenuInputField
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

            text = orig.text,
            textAnchor = orig.textAnchor,
            textFontSize = orig.textFontSize,
            placeholder = orig.placeholder,
            placeholderAnchor = orig.placeholderAnchor,
            placeholderFontSize = orig.placeholderFontSize,
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
            textColor = orig.textColor,
            textHue = orig.textHue,
            textSat = orig.textSat,
            textVal = orig.textVal,
            placeholderColor = orig.placeholderColor,
            placeholderHue = orig.placeholderHue,
            placeholderSat = orig.placeholderSat,
            placeholderVal = orig.placeholderVal,

            overrideColor = orig.overrideColor,
            overrideTextColor = orig.overrideTextColor,
            overridePlaceholderColor = orig.overridePlaceholderColor,
            useOverrideColor = orig.useOverrideColor,
            useOverrideTextColor = orig.useOverrideTextColor,
            useOverridePlaceholderColor = orig.useOverridePlaceholderColor,

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
            tickFunc = orig.tickFunc,
            func = orig.func,
            onScrollUpFunc = orig.onScrollUpFunc,
            onScrollDownFunc = orig.onScrollDownFunc,
            spawnFunc = orig.spawnFunc,
            onWaitEndFunc = orig.onWaitEndFunc,
            tickFuncJSON = orig.tickFuncJSON,

            varName = orig.varName,
            endEditFunc = orig.endEditFunc,
            endEditFuncJSON = orig.endEditFuncJSON,
            valueChangedFunc = orig.valueChangedFunc,
            valueChangedFuncJSON = orig.valueChangedFuncJSON,

            #endregion
        };

        #region JSON

        /// <summary>
        /// Parses a <see cref="MenuInputField"/> interface element from JSON.
        /// </summary>
        /// <param name="jnElement">JSON to parse.</param>
        /// <param name="j">Loop index.</param>
        /// <param name="loop">How many times the element is set to loop.</param>
        /// <param name="spriteAssets">Sprite assets.</param>
        /// <param name="customVariables">Passed custom variables.</param>
        /// <returns>Returns a parsed interface element.</returns>
        public static new MenuInputField Parse(JSONNode jnElement, int j, int loop, Dictionary<string, Sprite> spriteAssets, Dictionary<string, JSONNode> customVariables = null)
        {
            var element = new MenuInputField();
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
            if (!string.IsNullOrEmpty(jnElement["name"]))
                name = InterfaceManager.inst.ParseVarFunction(jnElement["name"], this, customVariables);
            if (!string.IsNullOrEmpty(jnElement["parent_layout"]))
                parentLayout = InterfaceManager.inst.ParseVarFunction(jnElement["parent_layout"], this, customVariables);
            if (!string.IsNullOrEmpty(jnElement["parent"]))
                parent = InterfaceManager.inst.ParseVarFunction(jnElement["parent"], this, customVariables);
            if (jnElement["sibling_index"] != null)
                siblingIndex = InterfaceManager.inst.ParseVarFunction(jnElement["sibling_index"], this, customVariables).AsInt;

            cachedVariables = customVariables;

            #endregion

            #region Spawning

            if (jnElement["regen"] != null)
                regenerate = InterfaceManager.inst.ParseVarFunction(jnElement["regen"], this, customVariables).AsBool;
            fromLoop = j > 0; // if element has been spawned from the loop or if its the first / only of its kind.
            this.loop = loop;

            #endregion

            #region UI

            if (jnElement["placeholder"] != null)
                placeholder = InterfaceManager.inst.ParseVarFunction(jnElement["placeholder"], this, customVariables);
            if (jnElement["text"] != null)
                text = InterfaceManager.inst.ParseVarFunction(jnElement["text"], this, customVariables);
            if (!string.IsNullOrEmpty(jnElement["icon"]))
            {
                var jnIcon = InterfaceManager.inst.ParseVarFunction(jnElement["icon"], this, customVariables);
                icon = jnIcon != null ? spriteAssets != null && spriteAssets.TryGetValue(jnIcon, out Sprite sprite) ? sprite : SpriteHelper.StringToSprite(jnIcon) : null;
            }
            if (!string.IsNullOrEmpty(jnElement["icon_path"]))
                iconPath = InterfaceManager.inst.ParseVarFunction(jnElement["icon_path"], this, customVariables);
            if (jnElement["rect"] != null)
                rect = RectValues.TryParse(InterfaceManager.inst.ParseVarFunction(jnElement["rect"], this, customVariables), RectValues.Default);
            if (jnElement["rounded"] != null)
                rounded = InterfaceManager.inst.ParseVarFunction(jnElement["rounded"], this, customVariables).AsInt; // roundness can be prevented by setting rounded to 0.
            if (jnElement["rounded_side"] != null)
                roundedSide = (SpriteHelper.RoundedSide)InterfaceManager.inst.ParseVarFunction(jnElement["rounded_side"], this, customVariables).AsInt; // default side should be Whole.
            if (jnElement["mask"] != null)
                mask = InterfaceManager.inst.ParseVarFunction(jnElement["mask"], this, customVariables).AsBool;
            if (jnElement["reactive"] != null)
                reactiveSetting = ReactiveSetting.Parse(InterfaceManager.inst.ParseVarFunction(jnElement["reactive"], this, customVariables), j);

            #endregion

            #region Color

            if (jnElement["col"] != null)
                color = InterfaceManager.inst.ParseVarFunction(jnElement["col"], this, customVariables).AsInt;
            if (jnElement["opacity"] != null)
                opacity = InterfaceManager.inst.ParseVarFunction(jnElement["opacity"], this, customVariables).AsFloat;
            if (jnElement["hue"] != null)
                hue = InterfaceManager.inst.ParseVarFunction(jnElement["hue"], this, customVariables).AsFloat;
            if (jnElement["sat"] != null)
                sat = InterfaceManager.inst.ParseVarFunction(jnElement["sat"], this, customVariables).AsFloat;
            if (jnElement["val"] != null)
                val = InterfaceManager.inst.ParseVarFunction(jnElement["val"], this, customVariables).AsFloat;
            if (jnElement["text_col"] != null)
                textColor = InterfaceManager.inst.ParseVarFunction(jnElement["text_col"], this, customVariables).AsInt;
            if (jnElement["text_hue"] != null)
                textHue = InterfaceManager.inst.ParseVarFunction(jnElement["text_hue"], this, customVariables).AsFloat;
            if (jnElement["text_sat"] != null)
                textSat = InterfaceManager.inst.ParseVarFunction(jnElement["text_sat"], this, customVariables).AsFloat;
            if (jnElement["text_val"] != null)
                textVal = InterfaceManager.inst.ParseVarFunction(jnElement["text_val"], this, customVariables).AsFloat;
            if (jnElement["place_col"] != null)
                placeholderColor = InterfaceManager.inst.ParseVarFunction(jnElement["place_col"], this, customVariables).AsInt;
            if (jnElement["place_hue"] != null)
                placeholderHue = InterfaceManager.inst.ParseVarFunction(jnElement["place_hue"], this, customVariables).AsFloat;
            if (jnElement["place_sat"] != null)
                placeholderSat = InterfaceManager.inst.ParseVarFunction(jnElement["place_sat"], this, customVariables).AsFloat;
            if (jnElement["place_val"] != null)
                placeholderVal = InterfaceManager.inst.ParseVarFunction(jnElement["place_val"], this, customVariables).AsFloat;
            useOverrideColor = jnElement["override_col"] != null;
            useOverrideTextColor = jnElement["override_text_col"] != null;
            useOverridePlaceholderColor = jnElement["override_place_col"] != null;
            if (useOverrideColor)
                overrideColor = LSColors.HexToColorAlpha(InterfaceManager.inst.ParseVarFunction(jnElement["override_col"], this, customVariables));
            if (useOverrideTextColor)
                overrideTextColor = LSColors.HexToColorAlpha(InterfaceManager.inst.ParseVarFunction(jnElement["override_text_col"], this, customVariables));
            if (useOverridePlaceholderColor)
                overridePlaceholderColor = LSColors.HexToColorAlpha(InterfaceManager.inst.ParseVarFunction(jnElement["override_place_col"], this, customVariables));

            #endregion

            #region Anim

            if (jnElement["wait"] != null)
                wait = InterfaceManager.inst.ParseVarFunction(jnElement["wait"], this, customVariables).AsBool;
            if (jnElement["anim_length"] != null)
                length = InterfaceManager.inst.ParseVarFunction(jnElement["anim_length"], this, customVariables).AsFloat;
            else if (!parsed)
                length = 0f;

            #endregion

            #region Func

            if (jnElement["play_blip_sound"] != null)
                playBlipSound = InterfaceManager.inst.ParseVarFunction(jnElement["play_blip_sound"], this, customVariables).AsBool;
            if (jnElement["selectable"] != null)
                selectable = InterfaceManager.inst.ParseVarFunction(jnElement["selectable"], this, customVariables).AsBool;
            if (jnElement["func"] != null)
                funcJSON = InterfaceManager.inst.ParseVarFunction(jnElement["func"], this, customVariables); // function to run when the element is clicked.
            if (jnElement["on_scroll_up_func"] != null)
                onScrollUpFuncJSON = InterfaceManager.inst.ParseVarFunction(jnElement["on_scroll_up_func"], this, customVariables); // function to run when the element is scrolled on.
            if (jnElement["on_scroll_down_func"] != null)
                onScrollDownFuncJSON = InterfaceManager.inst.ParseVarFunction(jnElement["on_scroll_down_func"], this, customVariables); // function to run when the element is scrolled on.
            if (jnElement["spawn_func"] != null)
                spawnFuncJSON = InterfaceManager.inst.ParseVarFunction(jnElement["spawn_func"], this, customVariables); // function to run when the element spawns.
            if (jnElement["on_wait_end_func"] != null)
                onWaitEndFuncJSON = InterfaceManager.inst.ParseVarFunction(jnElement["on_wait_end_func"], this, customVariables);
            if (jnElement["tick_func"] != null)
                tickFuncJSON = InterfaceManager.inst.ParseVarFunction(jnElement["tick_func"], this, customVariables);
            if (jnElement["value_changed_func"] != null)
                valueChangedFuncJSON = InterfaceManager.inst.ParseVarFunction(jnElement["value_changed_func"], this, customVariables);
            if (jnElement["end_edit_func"] != null)
                endEditFuncJSON = InterfaceManager.inst.ParseVarFunction(jnElement["end_edit_func"], this, customVariables);
            varName = jnElement["func_var_name"];

            #endregion
        }

        #endregion

        /// <summary>
        /// Writes the InputField text to a function.
        /// </summary>
        /// <param name="text">Text to pass.</param>
        public void Write(string text)
        {
            valueChangedFunc?.Invoke(text);
            this.text = text;
            if (valueChangedFuncJSON == null || string.IsNullOrEmpty(varName))
                return;

            valueChangedFuncJSON[varName] = text;
            InterfaceManager.inst.ParseFunction(valueChangedFuncJSON, this);
        }

        /// <summary>
        /// Runs when InputField editing has ended and writes the text to a function.
        /// </summary>
        /// <param name="text">Text to pass.</param>
        public void Finish(string text)
        {
            endEditFunc?.Invoke(text);
            this.text = text;
            if (endEditFuncJSON == null || string.IsNullOrEmpty(varName))
                return;

            endEditFuncJSON[varName] = text;
            InterfaceManager.inst.ParseFunction(endEditFuncJSON, this);
        }

        #endregion
    }
}
