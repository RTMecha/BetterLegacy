using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Companion.Entity;
using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Story;

using UnityRandom = UnityEngine.Random;

namespace BetterLegacy.Core
{
    /// <summary>
    /// Helper class for parsing JSON as functions. Custom functions can either be registered via <see cref="customJSONFunctions"/> or overriding the function classes.
    /// </summary>
    /// <typeparam name="T">Type of the element this parser supports.</typeparam>
    public class JSONFunctionParser<T> : Exists
    {
        public JSONFunctionParser() { }

        /// <summary>
        /// Dictionary of custom JSON functions. A set of functions can be registered to this as one function.
        /// </summary>
        public Dictionary<string, JSONNode> customJSONFunctions = new Dictionary<string, JSONNode>();

        /// <summary>
        /// Loads custom JSON functions from a JSON file.
        /// </summary>
        /// <param name="assetPath">Asset path to load from. Supports asset packs.</param>
        public virtual void LoadCustomJSONFunctions(string assetPath)
        {
            var array = AssetPack.GetArray(assetPath);
            for (int i = 0; i < array.Count; i++)
                customJSONFunctions[array[i]["name"]] = array[i];
        }

        #region Parse

        /// <summary>
        /// Parses text.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <returns>Returns parsed text.</returns>
        public virtual string ParseText(string input, Dictionary<string, JSONNode> customVariables = null) => RTString.ParseText(input, customVariables);

        /// <summary>
        /// Parses an "if_func" JSON and returns the result. Supports both JSON Object and JSON Array.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <param name="thisElement">Object reference.</param>
        /// <param name="customVariables">Passed custom variables.</param>
        /// <returns>Returns true if the passed JSON functions is true, otherwise false.</returns>
        public bool ParseIfFunction(JSONNode jn, T thisElement = default, Dictionary<string, JSONNode> customVariables = null, bool checkSource = true)
        {
            if (jn == null)
                return true;

            if (jn.IsObject || jn.IsString)
                return ParseIfFunctionSingle(jn, thisElement, customVariables, checkSource);

            bool result = true;

            if (jn.IsArray)
            {
                for (int i = 0; i < jn.Count; i++)
                {
                    var checkJN = jn[i];
                    var value = ParseIfFunction(checkJN, thisElement, customVariables);

                    // if json is array then count it as an else if statement
                    var elseIf = checkJN.IsArray || checkJN.IsObject && checkJN["otherwise"].AsBool;

                    if (elseIf && !result && value)
                        result = true;

                    if (!elseIf && !value)
                        result = false;
                }
            }

            return result;
        }

        /// <summary>
        /// Parses a singular "if_func" JSON.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <param name="thisElement">Object reference.</param>
        /// <param name="customVariables">Passed custom variables.</param>
        /// <returns>Returns true if the passed JSON function is true, otherwise false.</returns>
        public bool ParseIfFunctionSingle(JSONNode jn, T thisElement = default, Dictionary<string, JSONNode> customVariables = null, bool checkSource = true)
        {
            if (jn == null)
                return false;
            
            if (jn.IsObject)
            {
                var jnSource = ParseVarFunction(jn["func_reference"], thisElement, customVariables);
                if (jnSource != null && jnSource.IsString && checkSource)
                {
                    var split = jnSource.Value.Split('.');
                    if (!split.IsEmpty())
                    {
                        var b = split[0];
                        switch (b)
                        {
                            case "Example": {
                                    if (!Example.Current)
                                        break;

                                    if (split.Length <= 1)
                                        return Example.Current.functions.ParseIfFunction(jn, Example.Current, customVariables, false);

                                    var sub = split[1];
                                    if (sub == "Model" && Example.Current.model)
                                    {
                                        if (split.Length > 2)
                                        {
                                            var type = split[2];
                                            if (type == "Parts" && split.Length > 3 && Example.Current.model.TryGetPart(split[3], out ExampleModel.BasePart part))
                                                return Example.Current.model.partFunctions.ParseIfFunction(jn, part, customVariables, false);
                                        }
                                        return Example.Current.model.functions.ParseIfFunction(jn, Example.Current.model, customVariables, false);
                                    }
                                    if (sub == "ChatBubble" && Example.Current.chatBubble)
                                        return Example.Current.chatBubble.functions.ParseIfFunction(jn, Example.Current.chatBubble, customVariables, false);
                                    if (sub == "Brain" && Example.Current.brain)
                                        return Example.Current.brain.functions.ParseIfFunction(jn, Example.Current.brain, customVariables, false);
                                    if (sub == "Commands" && Example.Current.commands)
                                        return Example.Current.commands.functions.ParseIfFunction(jn, Example.Current.commands, customVariables, false);
                                    if (sub == "Options" && Example.Current.options)
                                        return Example.Current.options.functions.ParseIfFunction(jn, Example.Current.options, customVariables, false);

                                    break;
                                }
                        }
                        return jn;
                    }
                }
            }

            var jnFunc = ParseVarFunction(jn["func"], thisElement, customVariables);
            if (jnFunc != null)
                ParseFunction(jnFunc, thisElement, customVariables);

            var parameters = jn["params"];
            string name = jn.IsString ? jn : jn["name"];
            var not = !jn.IsString && jn["not"].AsBool; // If true, then check if the function is not true.

            if (string.IsNullOrEmpty(name))
                return false;

            // parse ! operator
            while (name.StartsWith("!"))
            {
                name = name.Substring(1, name.Length - 1);
                not = !not;
            }

            return not ? !IfFunction(jn, name, parameters, thisElement, customVariables) : IfFunction(jn, name, parameters, thisElement, customVariables);
        }

        /// <summary>
        /// Parses an entire func JSON. Supports both JSON Object and JSON Array.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <param name="thisElement">Object reference.</param>
        /// <param name="customVariables">Passed custom variables.</param>
        public void ParseFunction(JSONNode jn, T thisElement = default, Dictionary<string, JSONNode> customVariables = null, bool checkSource = true)
        {
            // allow multiple functions to occur.
            if (jn.IsArray)
            {
                for (int i = 0; i < jn.Count; i++)
                    ParseFunction(ParseVarFunction(jn[i], thisElement, customVariables), thisElement, customVariables);

                return;
            }

            ParseFunctionSingle(jn, thisElement, customVariables, checkSource);
        }

        /// <summary>
        /// Parses a singular "func" JSON and performs an action based on the name and parameters.
        /// </summary>
        /// <param name="jn">The func JSON. Must have a name and a params array, but can be a string if the function has no parameters. If it has a "if_func", then it will parse and check if it's true.</param>
        /// <param name="thisElement">Object reference.</param>
        /// <param name="customVariables">Passed custom variables.</param>
        public void ParseFunctionSingle(JSONNode jn, T thisElement = default, Dictionary<string, JSONNode> customVariables = null, bool checkSource = true)
        {
            if (!jn.IsString)
            {
                var jnSource = ParseVarFunction(jn["func_reference"], thisElement, customVariables);
                if (jnSource != null && jnSource.IsString && checkSource)
                {
                    var split = jnSource.Value.Split('.');
                    if (!split.IsEmpty())
                    {
                        var b = split[0];
                        switch (b)
                        {
                            case "Example": {
                                    if (!Example.Current)
                                        break;

                                    if (split.Length <= 1)
                                    {
                                        Example.Current.functions.ParseFunction(jn, Example.Current, customVariables, false);
                                        break;
                                    }

                                    var sub = split[1];
                                    if (sub == "Model" && Example.Current.model)
                                    {
                                        if (split.Length > 2)
                                        {
                                            var type = split[2];
                                            if (type == "Parts" && split.Length > 3 && Example.Current.model.TryGetPart(split[3], out ExampleModel.BasePart part))
                                            {
                                                Example.Current.model.partFunctions.ParseFunction(jn, part, customVariables, false);
                                                break;
                                            }
                                        }
                                        Example.Current.model.functions.ParseFunction(jn, Example.Current.model, customVariables, false);
                                        break;
                                    }
                                    if (sub == "ChatBubble" && Example.Current.chatBubble)
                                    {
                                        Example.Current.chatBubble.functions.ParseFunction(jn, Example.Current.chatBubble, customVariables, false);
                                        break;
                                    }
                                    if (sub == "Brain" && Example.Current.brain)
                                    {
                                        Example.Current.brain.functions.ParseFunction(jn, Example.Current.brain, customVariables, false);
                                        break;
                                    }
                                    if (sub == "Commands" && Example.Current.commands)
                                    {
                                        Example.Current.commands.functions.ParseFunction(jn, Example.Current.commands, customVariables, false);
                                        break;
                                    }
                                    if (sub == "Options" && Example.Current.options)
                                    {
                                        Example.Current.options.functions.ParseFunction(jn, Example.Current.options, customVariables, false);
                                        break;
                                    }

                                    break;
                                }
                        }
                        return;
                    }
                }

                var jnIfFunc = ParseVarFunction(jn["if_func"], thisElement, customVariables);
                if (jnIfFunc != null)
                {
                    if (!ParseIfFunction(jnIfFunc, thisElement, customVariables))
                    {
                        var jnElseFunc = ParseVarFunction(jn["else"], thisElement, customVariables);
                        if (jnElseFunc != null)
                            ParseFunction(jnElseFunc, thisElement, customVariables);

                        return;
                    }
                }

                var jnFunc = ParseVarFunction(jn["sub_func"], thisElement, customVariables);
                if (jnFunc != null)
                {
                    ParseFunction(jn["sub_func"], thisElement, customVariables);
                    return;
                }
            }

            var parameters = jn.IsString ? null : jn["params"];
            string name = jn.IsString ? jn : jn["name"];

            Function(jn, name, parameters, thisElement, customVariables);
        }

        /// <summary>
        /// Parses a "func" JSON and returns a variable from it based on the name and parameters.
        /// </summary>
        /// <param name="jn">The func JSON. Can be a <see cref="Lang"/>, string, variable key or func.</param>
        /// <param name="thisElement">Object reference.</param>
        /// <param name="customVariables">Passed custom variables.</param>
        /// <returns>Returns the variable returned from the JSON function.</returns>
        public JSONNode ParseVarFunction(JSONNode jn, T thisElement = default, Dictionary<string, JSONNode> customVariables = null, bool checkSource = true)
        {
            // if json is null or it's an array, just return itself.
            if (jn == null || jn.IsArray)
                return jn;

            if (jn.IsObject)
            {
                var jnSource = ParseVarFunction(jn["func_reference"], thisElement, customVariables);
                if (jnSource != null && jnSource.IsString && checkSource)
                {
                    var split = jnSource.Value.Split('.');
                    if (!split.IsEmpty())
                    {
                        var b = split[0];
                        switch (b)
                        {
                            case "Example": {
                                    if (!Example.Current)
                                        break;

                                    if (split.Length <= 1)
                                        return Example.Current.functions.ParseVarFunction(jn, Example.Current, customVariables, false);

                                    var sub = split[1];
                                    if (sub == "Model" && Example.Current.model)
                                    {
                                        if (split.Length > 2)
                                        {
                                            var type = split[2];
                                            if (type == "Parts" && split.Length > 3 && Example.Current.model.TryGetPart(split[3], out ExampleModel.BasePart part))
                                                return Example.Current.model.partFunctions.ParseVarFunction(jn, part, customVariables, false);
                                        }
                                        return Example.Current.model.functions.ParseVarFunction(jn, Example.Current.model, customVariables, false);
                                    }
                                    if (sub == "ChatBubble" && Example.Current.chatBubble)
                                        return Example.Current.chatBubble.functions.ParseVarFunction(jn, Example.Current.chatBubble, customVariables, false);
                                    if (sub == "Brain" && Example.Current.brain)
                                        return Example.Current.brain.functions.ParseVarFunction(jn, Example.Current.brain, customVariables, false);
                                    if (sub == "Commands" && Example.Current.commands)
                                        return Example.Current.commands.functions.ParseVarFunction(jn, Example.Current.commands, customVariables, false);
                                    if (sub == "Options" && Example.Current.options)
                                        return Example.Current.options.functions.ParseVarFunction(jn, Example.Current.options, customVariables, false);

                                    break;
                                }
                        }
                        return jn;
                    }
                }
            }

            // item is a singular string
            if (jn.IsString)
                return customVariables != null && customVariables.TryGetValue(jn.Value, out JSONNode customVariable) ? ParseVarFunction(customVariable, thisElement, customVariables) : ParseText(jn, customVariables);

            // item is lang (need to make sure it actually IS a lang by checking for language names)
            if (Lang.TryParse(jn, out Lang lang))
                return ParseText(lang, customVariables);

            var parameters = jn["params"];
            string name = jn["name"];

            return VarFunction(jn, name, parameters, thisElement, customVariables);
        }

        #endregion

        #region Call

        public bool CallIfFunction(string name, T thisElement, Dictionary<string, JSONNode> customVariables, params JSONNode[] parameters)
        {
            var jn = Parser.NewJSONObject();
            jn["name"] = name;
            for (int i = 0; i < parameters.Length; i++)
                jn["params"][i] = parameters[i];
            return ParseIfFunction(jn, thisElement, customVariables, false);
        }
        
        public void CallFunction(string name, T thisElement, Dictionary<string, JSONNode> customVariables, params JSONNode[] parameters)
        {
            var jn = Parser.NewJSONObject();
            jn["name"] = name;
            for (int i = 0; i < parameters.Length; i++)
                jn["params"][i] = parameters[i];
            ParseFunction(jn, thisElement, customVariables, false);
        }

        public JSONNode CallVarFunction(string name, T thisElement, Dictionary<string, JSONNode> customVariables, params JSONNode[] parameters)
        {
            var jn = Parser.NewJSONObject();
            jn["name"] = name;
            for (int i = 0; i < parameters.Length; i++)
                jn["params"][i] = parameters[i];
            return ParseVarFunction(jn, thisElement, customVariables, false);
        }

        #endregion

        #region Function Lists

        public virtual bool IfFunction(JSONNode jn, string name, JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            try
            {
                switch (name)
                {
                    #region Main

                    case "True": return true;
                    case "False": return false;
                    case "GetSettingBool": {
                            if (parameters == null)
                                break;

                            return DataManager.inst.GetSettingBool(ParseVarFunction(parameters.Get(0, "setting"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsBool);
                        }
                    case "GetSettingIntEquals": {
                            if (parameters == null)
                                break;

                            return DataManager.inst.GetSettingInt(ParseVarFunction(parameters.Get(0, "setting"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsInt) == ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                        }
                    case "GetSettingIntLesserEquals": {
                            if (parameters == null)
                                break;

                            return DataManager.inst.GetSettingInt(ParseVarFunction(parameters.Get(0, "setting"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsInt) <= ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                        }
                    case "GetSettingIntGreaterEquals": {
                            if (parameters == null)
                                break;

                            return DataManager.inst.GetSettingInt(ParseVarFunction(parameters.Get(0, "setting"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsInt) >= ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                        }
                    case "GetSettingIntLesser": {
                            if (parameters == null)
                                break;

                            return DataManager.inst.GetSettingInt(ParseVarFunction(parameters.Get(0, "setting"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsInt) < ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                        }
                    case "GetSettingIntGreater": {
                            if (parameters == null)
                                break;

                            return DataManager.inst.GetSettingInt(ParseVarFunction(parameters.Get(0, "setting"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsInt) > ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                        }
                    case "IsScene": {
                            if (parameters == null)
                                break;

                            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == ParseVarFunction(parameters.Get(0, "scene"), thisElement, customVariables);
                        }

                    case "Equals": {
                            if (parameters == null)
                                break;

                            return ParseVarFunction(parameters.Get(0, "first"), thisElement, customVariables) == ParseVarFunction(parameters.Get(1, "second"), thisElement, customVariables);
                        }
                    case "LesserEquals": {
                            if (parameters == null)
                                break;

                            return ParseVarFunction(parameters.Get(0, "first"), thisElement, customVariables).AsFloat <= ParseVarFunction(parameters.Get(1, "second"), thisElement, customVariables).AsFloat;
                        }
                    case "GreaterEquals": {
                            if (parameters == null)
                                break;

                            return ParseVarFunction(parameters.Get(0, "first"), thisElement, customVariables).AsFloat >= ParseVarFunction(parameters.Get(1, "second"), thisElement, customVariables).AsFloat;
                        }
                    case "Lesser": {
                            if (parameters == null)
                                break;

                            return ParseVarFunction(parameters.Get(0, "first"), thisElement, customVariables).AsFloat < ParseVarFunction(parameters.Get(1, "second"), thisElement, customVariables).AsFloat;
                        }
                    case "Greater": {
                            if (parameters == null)
                                break;

                            return ParseVarFunction(parameters.Get(0, "first"), thisElement, customVariables).AsFloat > ParseVarFunction(parameters.Get(1, "second"), thisElement, customVariables).AsFloat;
                        }

                    case "HasAsset": {
                            if (parameters == null)
                                break;

                            return AssetPack.TryGetFile(parameters.Get(0, "path"), out string filePath);
                        }

                    case "ConfigSettingEquals": {
                            if (parameters == null)
                                break;

                            var configName = ParseVarFunction(parameters.Get(0, "config"), thisElement, customVariables);
                            if (!LegacyPlugin.configs.TryFind(x => x.Name == configName, out BaseConfig config))
                                break;

                            var section = ParseVarFunction(parameters.Get(1, "section"), thisElement, customVariables);
                            var key = ParseVarFunction(parameters.Get(2, "key"), thisElement, customVariables);
                            if (config.Settings.TryFind(x => x.Section == section && x.Key == key, out BaseSetting setting))
                                return setting.BoxedValue?.ToString() == ParseVarFunction(parameters.Get(3, "value"), thisElement, customVariables);

                            break;
                        }

                    #endregion

                    #region Player

                    case "PlayerCountEquals": {
                            if (parameters == null)
                                break;

                            return PlayerManager.Players.Count == ParseVarFunction(parameters.Get(0, "count"), thisElement, customVariables).AsInt;
                        }
                    case "PlayerCountLesserEquals": {
                            if (parameters == null)
                                break;

                            return PlayerManager.Players.Count <= ParseVarFunction(parameters.Get(0, "count"), thisElement, customVariables).AsInt;
                        }
                    case "PlayerCountGreaterEquals": {
                            if (parameters == null)
                                break;

                            return PlayerManager.Players.Count >= ParseVarFunction(parameters.Get(0, "count"), thisElement, customVariables).AsInt;
                        }
                    case "PlayerCountLesser": {
                            if (parameters == null)
                                break;

                            return PlayerManager.Players.Count < ParseVarFunction(parameters.Get(0, "count"), thisElement, customVariables).AsInt;
                        }
                    case "PlayerCountGreater": {
                            if (parameters == null)
                                break;

                            return PlayerManager.Players.Count > ParseVarFunction(parameters.Get(0, "count"), thisElement, customVariables).AsInt;
                        }

                    #endregion

                    #region Profile
                        
                    case "DisplayNameEquals": {
                            if (parameters == null)
                                break;

                            return CoreConfig.Instance.DisplayName.Value == ParseVarFunction(parameters.Get(0, "user"), thisElement, customVariables).Value;
                        }
                        
                    case "ProfileLoadIntEquals": {
                            if (parameters == null || !LegacyPlugin.player || LegacyPlugin.player.memory == null)
                                break;

                            var varName = ParseVarFunction(parameters.Get(0, "var_name"));
                            if (varName == null || !varName.IsString)
                                break;

                            var profileValue = LegacyPlugin.player.memory[ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables).Value];
                            if (profileValue == null)
                                profileValue = ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables);

                            return profileValue.AsInt == ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                        }
                    case "ProfileLoadIntLesserEquals": {
                            if (parameters == null || !LegacyPlugin.player || LegacyPlugin.player.memory == null)
                                break;

                            var varName = ParseVarFunction(parameters.Get(0, "var_name"));
                            if (varName == null || !varName.IsString)
                                break;

                            var profileValue = LegacyPlugin.player.memory[ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables).Value];
                            if (profileValue == null)
                                profileValue = ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables);

                            return profileValue.AsInt <= ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                        }
                    case "ProfileLoadIntGreaterEquals": {
                            if (parameters == null || !LegacyPlugin.player || LegacyPlugin.player.memory == null)
                                break;

                            var varName = ParseVarFunction(parameters.Get(0, "var_name"));
                            if (varName == null || !varName.IsString)
                                break;

                            var profileValue = LegacyPlugin.player.memory[ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables).Value];
                            if (profileValue == null)
                                profileValue = ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables);

                            return profileValue.AsInt >= ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                        }
                    case "ProfileLoadIntLesser": {
                            if (parameters == null || !LegacyPlugin.player || LegacyPlugin.player.memory == null)
                                break;

                            var varName = ParseVarFunction(parameters.Get(0, "var_name"));
                            if (varName == null || !varName.IsString)
                                break;

                            var profileValue = LegacyPlugin.player.memory[ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables).Value];
                            if (profileValue == null)
                                profileValue = ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables);

                            return profileValue.AsInt < ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                        }
                    case "ProfileLoadIntGreater": {
                            if (parameters == null || !LegacyPlugin.player || LegacyPlugin.player.memory == null)
                                break;

                            var varName = ParseVarFunction(parameters.Get(0, "var_name"));
                            if (varName == null || !varName.IsString)
                                break;

                            var profileValue = LegacyPlugin.player.memory[ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables).Value];
                            if (profileValue == null)
                                profileValue = ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables);

                            return profileValue.AsInt > ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                        }
                    case "ProfileLoadBool": {
                            if (parameters == null)
                                break;

                            return LegacyPlugin.player && LegacyPlugin.player.memory != null && LegacyPlugin.player.memory[ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables).Value].AsBool;
                        }

                    #endregion

                    #region Story Chapter

                    case "StoryChapterEquals": {
                            if (parameters == null)
                                break;

                            return StoryManager.inst.CurrentSave.LoadInt("Chapter", 0) == ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables).AsInt;
                        }
                    case "StoryChapterLesserEquals": {
                            if (parameters == null)
                                break;

                            return StoryManager.inst.CurrentSave.LoadInt("Chapter", 0) <= ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables).AsInt;
                        }
                    case "StoryChapterGreaterEquals": {
                            if (parameters == null)
                                break;

                            return StoryManager.inst.CurrentSave.LoadInt("Chapter", 0) >= ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables).AsInt;
                        }
                    case "StoryChapterLesser": {
                            if (parameters == null)
                                break;

                            return StoryManager.inst.CurrentSave.LoadInt("Chapter", 0) < ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables).AsInt;
                        }
                    case "StoryChapterGreater": {
                            if (parameters == null)
                                break;

                            return StoryManager.inst.CurrentSave.LoadInt("Chapter", 0) > ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables).AsInt;
                        }
                    case "StoryInstalled": {
                            return StoryManager.inst && RTFile.DirectoryExists(StoryManager.StoryAssetsPath);
                        }
                    case "StoryLoadIntEquals": {
                            if (parameters == null)
                                break;

                            return StoryManager.inst.CurrentSave.LoadInt(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default")).AsInt) == ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                        }
                    case "StoryLoadIntLesserEquals": {
                            if (parameters == null)
                                break;

                            return StoryManager.inst.CurrentSave.LoadInt(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default")).AsInt) <= ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                        }
                    case "StoryLoadIntGreaterEquals": {
                            if (parameters == null)
                                break;

                            return StoryManager.inst.CurrentSave.LoadInt(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default")).AsInt) >= ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                        }
                    case "StoryLoadIntLesser": {
                            if (parameters == null)
                                break;

                            return StoryManager.inst.CurrentSave.LoadInt(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default")).AsInt) < ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                        }
                    case "StoryLoadIntGreater": {
                            if (parameters == null)
                                break;

                            return StoryManager.inst.CurrentSave.LoadInt(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default")).AsInt) > ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
                        }
                    case "StoryLoadBool": {
                            if (parameters == null)
                                break;

                            return StoryManager.inst.CurrentSave.LoadBool(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsBool);
                        }

                    #endregion

                    #region LevelRanks

                    case "ChapterFullyRanked": {
                            if (parameters == null)
                                break;

                            var chapter = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables).AsInt;
                            var minRank = ParseVarFunction(parameters.GetOrDefault(1, "min_rank", StoryManager.CHAPTER_RANK_REQUIREMENT)).AsInt;
                            var maxRank = ParseVarFunction(parameters.GetOrDefault(2, "max_rank", 1)).AsInt;
                            var bonus = ParseVarFunction(parameters.Get(3, "bonus"), thisElement, customVariables).AsBool;

                            var levelIDs = bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters;

                            return
                                chapter < levelIDs.Count &&
                                levelIDs[chapter].levels.All(x => x.bonus ||
                                                StoryManager.inst.CurrentSave.Saves.TryFind(y => y.ID == x.id, out SaveData playerData) &&
                                                LevelManager.GetLevelRank(playerData) >= maxRank && LevelManager.GetLevelRank(playerData) <= minRank);
                        }
                    case "LevelRankEquals": {
                            if (parameters == null)
                                break;

                            return LevelManager.CurrentLevel.saveData && LevelManager.GetLevelRank(LevelManager.CurrentLevel) == ParseVarFunction(parameters.Get(0, "rank"), thisElement, customVariables).AsInt;
                        }
                    case "LevelRankLesserEquals": {
                            if (parameters == null)
                                break;

                            return LevelManager.CurrentLevel.saveData && LevelManager.GetLevelRank(LevelManager.CurrentLevel) <= ParseVarFunction(parameters.Get(0, "rank"), thisElement, customVariables).AsInt;
                        }
                    case "LevelRankGreaterEquals": {
                            if (parameters == null)
                                break;

                            return LevelManager.CurrentLevel.saveData && LevelManager.GetLevelRank(LevelManager.CurrentLevel) >= ParseVarFunction(parameters.Get(0, "rank"), thisElement, customVariables).AsInt;
                        }
                    case "LevelRankLesser": {
                            if (parameters == null)
                                break;

                            return LevelManager.CurrentLevel.saveData && LevelManager.GetLevelRank(LevelManager.CurrentLevel) < ParseVarFunction(parameters.Get(0, "rank"), thisElement, customVariables).AsInt;
                        }
                    case "LevelRankGreater": {
                            if (parameters == null)
                                break;

                            return LevelManager.CurrentLevel.saveData && LevelManager.GetLevelRank(LevelManager.CurrentLevel) > ParseVarFunction(parameters.Get(0, "rank"), thisElement, customVariables).AsInt;
                        }
                    case "StoryLevelRankEquals": {
                            if (parameters == null)
                                break;

                            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables).Value;

                            return StoryManager.inst.CurrentSave.Saves.TryFind(x => x.ID == id, out SaveData playerData) && LevelManager.GetLevelRank(LevelManager.CurrentLevel) == ParseVarFunction(parameters.Get(1, "rank"), thisElement, customVariables).AsInt;
                        }
                    case "StoryLevelRankLesserEquals": {
                            if (parameters == null)
                                break;

                            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables).Value;

                            return StoryManager.inst.CurrentSave.Saves.TryFind(x => x.ID == id, out SaveData playerData) && LevelManager.GetLevelRank(LevelManager.CurrentLevel) <= ParseVarFunction(parameters.Get(1, "rank"), thisElement, customVariables).AsInt;
                        }
                    case "StoryLevelRankGreaterEquals": {
                            if (parameters == null)
                                break;

                            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables).Value;

                            return StoryManager.inst.CurrentSave.Saves.TryFind(x => x.ID == id, out SaveData playerData) && LevelManager.GetLevelRank(LevelManager.CurrentLevel) >= ParseVarFunction(parameters.Get(1, "rank"), thisElement, customVariables).AsInt;
                        }
                    case "StoryLevelRankLesser": {
                            if (parameters == null)
                                break;

                            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables).Value;

                            return StoryManager.inst.CurrentSave.Saves.TryFind(x => x.ID == id, out SaveData playerData) && LevelManager.GetLevelRank(LevelManager.CurrentLevel) < ParseVarFunction(parameters.Get(1, "rank"), thisElement, customVariables).AsInt;
                        }
                    case "StoryLevelRankGreater": {
                            if (parameters == null)
                                break;

                            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables).Value;

                            return StoryManager.inst.CurrentSave.Saves.TryFind(x => x.ID == id, out SaveData playerData) && LevelManager.GetLevelRank(LevelManager.CurrentLevel) > ParseVarFunction(parameters.Get(1, "rank"), thisElement, customVariables).AsInt;
                        }

                     #endregion
                }

                if (!string.IsNullOrEmpty(name) && customJSONFunctions.TryGetValue(name, out JSONNode customJSONFunction))
                    return ParseIfFunction(customJSONFunction, thisElement, customVariables);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Had an error with parsing {jn}!\nException: {ex}");
            }

            return false;
        }

        public virtual void Function(JSONNode jn, string name, JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            switch (name)
            {
                #region Main

                #region LoadScene

                // Loads a Unity scene.
                // Supports both JSON array and JSON object.
                // Scenes:
                // - Main Menu
                // - Input Select
                // - Game
                // - Editor
                // - Interface
                // - post_level
                // - Arcade Select
                // 
                // - JSON Array Structure -
                // 0 = scene to load
                // 1 = show loading screen
                // 2 = set is arcade (used if the scene you're going to is input select, and afterwards you want to travel to the arcade scene.
                // Example:
                // [
                //   "Input Select",
                //   "False",
                //   "True" // true, so after all inputs have been assigned and user continues, go to arcade scene.
                // ]
                // 
                // - JSON Object Structure -
                // "scene"
                // "show_loading"
                // "is_arcade"
                // Example:
                // {
                //   "scene": "Input Select",
                //   "show_loading": "True",
                //   "is_arcade": "False" < if is_arcade is null or is false, load story mode after Input Select.
                // }
                case "LoadScene": {
                        if (parameters == null)
                            break;

                        var sceneName = ParseVarFunction(parameters.Get(0, "scene"), thisElement, customVariables);
                        if (sceneName == null || !sceneName.IsString)
                            break;

                        LevelManager.IsArcade = ParseVarFunction(parameters.Get(2, "is_arcade"), thisElement, customVariables);
                        var showLoading = ParseVarFunction(parameters.Get(1, "show_loading"), thisElement, customVariables);

                        if (showLoading != null)
                            SceneManager.inst.LoadScene(sceneName, Parser.TryParse(showLoading, true));
                        else
                            SceneManager.inst.LoadScene(sceneName);

                        break;
                    }

                #endregion

                #region UpdateSettingBool

                // Updates or adds a global setting of boolean true / false type.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = setting name
                // 1 = value
                // 2 = relative
                // Example:
                // [
                //   "IsArcade",
                //   "False",
                //   "True" < swaps the bool value instead of setting it.
                // ]
                // 
                // - JSON Object Structure -
                // "setting"
                // "value"
                // "relative"
                // Example:
                // {
                //   "setting": "IsArcade",
                //   "value": "True",
                //   "relative": "False"
                // }
                case "UpdateSettingBool": {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["setting"] == null || parameters["value"] == null))
                            break;

                        var isArray = parameters.IsArray;
                        var settingName = isArray ? parameters[0] : parameters["setting"];
                        DataManager.inst.UpdateSettingBool(settingName,
                            isArray && parameters.Count > 2 && parameters[2].AsBool || parameters.IsObject && parameters["relative"] != null && parameters["relative"].AsBool ? !DataManager.inst.GetSettingBool(settingName) : isArray ? parameters[1].AsBool : parameters["value"].AsBool);

                        break;
                    }

                #endregion

                #region UpdateSettingInt

                // Updates or adds a global setting of integer type.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = setting name
                // 1 = value
                // 2 = relative
                // Example:
                // [
                //   "SomeKindOfNumber",
                //   "False",
                //   "True" < adds onto the current numbers' value instead of just setting it.
                // ]
                // 
                // - JSON Object Structure -
                // "setting"
                // "value"
                // "relative"
                // Example:
                // {
                //   "setting": "SomeKindOfNumber",
                //   "value": "True",
                //   "relative": "False"
                // }
                case "UpdateSettingInt": {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["setting"] == null || parameters["value"] == null))
                            break;

                        var isArray = parameters.IsArray;
                        var settingName = isArray ? parameters[0] : parameters["setting"];

                        DataManager.inst.UpdateSettingInt(settingName,
                            (isArray ? parameters[1].AsInt : parameters["value"].AsInt) +
                            (isArray && parameters.Count > 2 && parameters[2].AsBool ||
                            parameters.IsObject && parameters["relative"] != null && parameters["relative"].AsBool ?
                                    DataManager.inst.GetSettingInt(settingName) : 0));

                        break;
                    }

                #endregion

                #region Wait

                // Waits a set amount of seconds and runs a function.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = time
                // 1 = function
                // Example:
                // [
                //   "1", < waits 1 second
                //   {
                //     "name": "PlaySound", < runs PlaySound func after 1 second.
                //     "params": [ "blip" ]
                //   }
                // ]
                // 
                // - JSON Object Structure -
                // "t"
                // "func"
                // Example:
                // {
                //   "t": "1",
                //   "func": {
                //     "name": "PlaySound",
                //     "params": {
                //       "sound": "blip"
                //     }
                //   }
                // }
                case "Wait": {
                        if (parameters == null)
                            break;

                        var t = ParseVarFunction(parameters.Get(0, "t"), thisElement, customVariables);
                        if (t == null)
                            break;

                        var func = ParseVarFunction(parameters.Get(1, "func"), thisElement, customVariables);
                        if (func == null)
                            break;

                        CoroutineHelper.PerformActionAfterSeconds(t, () =>
                        {
                            try
                            {
                                ParseFunction(func);
                            }
                            catch (Exception ex)
                            {
                                CoreHelper.LogError($"Had an exception with Wait function.\nException: {ex}");
                            }
                        });

                        break;
                    }

                #endregion

                #region Log

                // Sends a log. Can be viewed via the BepInEx console, UnityExplorer console (if Unity logging is on) or the Player.log file. Message will include the BetterLegacy class name.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = message
                // Example:
                // [
                //   "Hello world!"
                // ]
                // 
                // - JSON Object Structure -
                // "msg"
                // Example:
                // {
                //   "msg": "Hello world!"
                // }
                case "Log": {
                        if (parameters == null)
                            break;

                        var msg = ParseVarFunction(parameters.Get(0, "msg"), thisElement, customVariables);
                        if (msg == null)
                            break;
                        
                        CoreHelper.Log(msg);

                        break;
                    }

                #endregion

                #region Notify

                // Sends a notification.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = message
                // Example:
                // [
                //   "Hello world!",
                //   "FFFFFF",
                //   "20"
                // ]
                // 
                // - JSON Object Structure -
                // "msg"
                // "col"
                // "size"
                // Example:
                // {
                //   "msg": "Hello world!",
                //   "col": "000000",
                //   "size": "40"
                // }
                case "Notify": {
                        if (parameters == null)
                            break;

                        var color = Color.gray;
                        var jnColor = ParseVarFunction(parameters.Get(1, "col"), thisElement, customVariables);
                        if (jnColor != null)
                            color = RTColors.HexToColor(jnColor);

                        var fontSize = 30;
                        var jnFontSize = ParseVarFunction(parameters.Get(2, "size"), thisElement, customVariables);
                        if (jnFontSize != null)
                            fontSize = jnFontSize.AsInt;

                        CoreHelper.Notify(ParseVarFunction(parameters.Get(0, "msg"), thisElement, customVariables), color, fontSize);
                        break;
                    }

                #endregion

                #region ExitGame

                // Exits the game.
                // Function has no parameters.
                case "ExitGame": {
                        Application.Quit();
                        break;
                    }

                #endregion

                #region Config

                // Opens the Config Manager UI.
                // Function has no parameters.
                case "Config": {
                        ConfigManager.inst.Show();
                        break;
                    }

                #endregion

                #region ForLoop

                case "ForLoop": {
                        if (parameters == null)
                            break;

                        var varName = ParseVarFunction(parameters.Get(3, "var_name"), thisElement, customVariables);
                        if (varName == null || !varName.IsString || varName == string.Empty)
                            varName = "index";
                        var index = ParseVarFunction(parameters.Get(0, "index"), thisElement, customVariables).AsInt;
                        var count = ParseVarFunction(parameters.Get(1, "count"), thisElement, customVariables).AsInt;
                        var func = ParseVarFunction(parameters.Get(2, "func"), thisElement, customVariables);
                        if (func == null)
                            break;

                        index = RTMath.Clamp(index, 0, count - 1);

                        for (int j = index; j < count; j++)
                        {
                            var loopVar = new Dictionary<string, JSONNode>();
                            if (customVariables != null)
                            {
                                foreach (var keyValuePair in customVariables)
                                    loopVar[keyValuePair.Key] = keyValuePair.Value;
                            }

                            loopVar[varName.Value] = j;

                            ParseFunction(func, thisElement, loopVar);
                        }

                        break;
                    }

                #endregion

                #region CacheVariable

                case "CacheVariable": {
                        if (parameters == null)
                            break;

                        var varName = parameters.Get(0, "var_name");
                        if (varName == null || !varName.IsString)
                            break;

                        var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables);
                        if (value == null)
                            break;

                        customVariables[varName] = value;

                        break;
                    }

                #endregion

                #endregion

                #region Levels

                #region LoadLevel

                // Finds a level by its' ID and loads it. Only works if the user has already loaded levels.
                // Supports both JSON array and JSON object.
                //
                // - JSON Array Structure -
                // 0 = id
                // Example:
                // [
                //   "6365672" < loads level with this as its ID.
                // ]
                // 
                // - JSON Object Structure -
                // "id"
                // Example:
                // {
                //   "id": "6365672"
                // }
                case "LoadLevel": {
                        if (parameters == null)
                            break;

                        var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables);
                        if (id == null || !id.IsString)
                            break;

                        if (LevelManager.Levels.TryFind(x => x.id == id, out Level level))
                            LevelManager.Play(level);
                        else if (SteamWorkshopManager.inst && SteamWorkshopManager.inst.Initialized && SteamWorkshopManager.inst.Levels.TryFind(x => x.id == id, out Level steamLevel))
                            LevelManager.Play(steamLevel);

                        break;
                    }

                #endregion
                    
                #region LoadLevelPath

                // Loads a level via a path.
                // Supports both JSON array and JSON object.
                //
                // - JSON Array Structure -
                // 0 = id
                // Example:
                // [
                //   "6365672" < loads level with this as its ID.
                // ]
                // 
                // - JSON Object Structure -
                // "id"
                // Example:
                // {
                //   "id": "6365672"
                // }
                case "LoadLevelPath": {
                        if (parameters == null)
                            break;

                        var path = ParseVarFunction(parameters.Get(0, "path"), thisElement, customVariables);
                        if (path == null || !path.IsString)
                            break;

                        LevelManager.Load(path.Value);

                        break;
                    }

                #endregion

                #endregion

                #region Online

                #region SetDiscordStatus

                // Sets the users' Discord status.
                // Supports both JSON array and JSON object.
                //
                // - JSON Array Structure -
                // 0 = state
                // 1 = details
                // 2 = icon
                // 3 = art
                // Example:
                // [
                //   "Navigating Main Menu",
                //   "In Menus",
                //   "menu", < accepts values: arcade, editor, play, menu
                //   "pa_logo_white" < accepts values: pa_logo_white, pa_logo_black
                // ]
                //
                // - JSON Object Structure -
                // "state"
                // "details"
                // "icon"
                // "art"
                // Example:
                // {
                //   "state": "Interfacing or soemthing Idk",
                //   "details": "In the Interface",
                //   "icon": "play"
                //   // if art is null, then the default art will be pa_logo_white.
                // }
                case "SetDiscordStatus": {
                        if (parameters == null)
                            break;

                        CoreHelper.UpdateDiscordStatus(
                            parameters.Get(0, "state"),
                            parameters.Get(1, "details"),
                            parameters.Get(2, "icon"),
                            parameters.GetOrDefault(3, "art", "pa_logo_white"));

                        break;
                    }

                #endregion

                #region OpenLink

                case "OpenLink": {
                        var linkType = Parser.TryParse(ParseVarFunction(parameters.Get(0, "link_type"), thisElement, customVariables), URLSource.Artist);
                        var site = ParseVarFunction(parameters.Get(1, "site"), thisElement, customVariables);
                        var link = ParseVarFunction(parameters.Get(2, "link"), thisElement, customVariables);

                        var url = AlephNetwork.GetURL(linkType, site, link);
                        CoreHelper.Log($"Opening URL: {url}");
                        Application.OpenURL(url);

                        break;
                    }

                #endregion
                    
                #region ModderDiscord

                // Opens the System Error Discord server link.
                // Function has no parameters.
                case "ModderDiscord": {
                        Application.OpenURL(AlephNetwork.MOD_DISCORD_URL);

                        break;
                    }

                #endregion

                #region SourceCode

                // Opens the GitHub Source Code link.
                // Function has no parameters.
                case "SourceCode": {
                        Application.OpenURL(AlephNetwork.OPEN_SOURCE_URL);

                        break;
                    }

                #endregion

                #endregion

                #region Specific

                #region LoadStoryLevel

                case "LoadStoryLevel": {
                        if (parameters == null)
                            break;

                        var chapter = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables);
                        if (chapter == null)
                            break;

                        var level = ParseVarFunction(parameters.Get(1, "level"), thisElement, customVariables);
                        if (level == null)
                            break;

                        var cutsceneIndex = ParseVarFunction(parameters.Get(2, "cutscene_index"), thisElement, customVariables).AsInt;
                        var bonus = ParseVarFunction(parameters.Get(4, "bonus"), thisElement, customVariables).AsBool;
                        var skipCutscenes = ParseVarFunction(parameters.GetOrDefault(5, "skip_cutscenes", true), thisElement, customVariables).AsBool;

                        StoryManager.inst.ContinueStory = ParseVarFunction(parameters.Get(3, "continue"), thisElement, customVariables).AsBool;

                        ArcadeHelper.ResetModifiedStates();
                        StoryManager.inst.Play(chapter.AsInt, level.AsInt, cutsceneIndex, bonus, skipCutscenes);

                        break;
                    }

                #endregion

                #region LoadStoryCutscene

                case "LoadStoryCutscene": {
                        if (parameters == null)
                            break;

                        var chapter = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables);
                        if (chapter == null)
                            break;

                        var level = ParseVarFunction(parameters.Get(1, "level"), thisElement, customVariables);
                        if (level == null)
                            break;

                        var isArray = parameters.IsArray;
                        var cutsceneDestinationJN = ParseVarFunction(parameters.Get(2, "cutscene_destination"), thisElement, customVariables);
                        var cutsceneDestination = Parser.TryParse(cutsceneDestinationJN, CutsceneDestination.Pre);
                        var cutsceneIndex = ParseVarFunction(parameters.Get(3, "cutscene_index"), thisElement, customVariables).AsInt;
                        var bonus = ParseVarFunction(parameters.Get(4, "bonus"), thisElement, customVariables).AsBool;

                        StoryManager.inst.ContinueStory = false;

                        ArcadeHelper.ResetModifiedStates();
                        StoryManager.inst.PlayCutscene(chapter, level, cutsceneDestination, cutsceneIndex, bonus);

                        break;
                    }

                #endregion

                #region PlayAllCutscenes

                case "PlayAllCutscenes": {
                        if (parameters == null)
                            return;

                        var chapter = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables);
                        if (chapter == null)
                            break;

                        StoryManager.inst.PlayAllCutscenes(chapter, ParseVarFunction(parameters.Get(1, "bonus"), thisElement, customVariables).AsBool);

                        break;
                    }

                #endregion

                #region LoadStoryLevelPath

                case "LoadStoryLevelPath": {
                        if (parameters == null)
                            return;

                        var path = ParseVarFunction(parameters.Get(0, "path"), thisElement, customVariables);
                        if (path == null || !path.IsString)
                            break;

                        var songName = ParseVarFunction(parameters.Get(1, "song"), thisElement, customVariables);

                        StoryManager.inst.ContinueStory = ParseVarFunction(parameters.Get(2, "continue"), thisElement, customVariables).AsBool;

                        ArcadeHelper.ResetModifiedStates();
                        StoryManager.inst.Play(path, songName);

                        break;
                    }

                #endregion

                #region LoadNextStoryLevel

                case "LoadNextStoryLevel": {
                        StoryManager.inst.ContinueStory = true;

                        int chapter = StoryManager.inst.CurrentSave.ChapterIndex;
                        StoryManager.inst.Play(chapter, StoryManager.inst.CurrentSave.LoadInt($"DOC{RTString.ToStoryNumber(chapter)}Progress", 0), 0, StoryManager.inst.inBonusChapter);

                        break;
                    }

                #endregion

                #region LoadChapterTransition

                case "LoadChapterTransition": {
                        StoryManager.inst.ContinueStory = true;

                        var chapter = StoryManager.inst.CurrentSave.ChapterIndex;
                        StoryManager.inst.Play(chapter, StoryMode.Instance.chapters[chapter].Count, 0, StoryManager.inst.inBonusChapter);

                        break;
                    }

                #endregion

                #region SaveProfileValue

                case "SaveProfileValue": {
                        if (parameters == null || !LegacyPlugin.player || LegacyPlugin.player.memory == null)
                            break;

                        var varName = ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables);
                        if (varName == null || !varName.IsString)
                            break;
                        
                        var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables);
                        if (value == null)
                            break;

                        LegacyPlugin.player.memory[varName.Value] = value;
                        LegacyPlugin.SaveProfile();

                        break;
                    }

                #endregion

                #region StorySaveBool

                case "StorySaveBool": {
                        if (parameters == null)
                            break;

                        var saveName = ParseVarFunction(parameters.Get(0, "name"), thisElement, customVariables);
                        if (saveName == null || !saveName.IsString)
                            break;

                        var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables);

                        if (ParseVarFunction(parameters.Get(2, "toggle"), thisElement, customVariables).AsBool)
                            value = !StoryManager.inst.CurrentSave.LoadBool(saveName, false);

                        StoryManager.inst.CurrentSave.SaveBool(saveName, value.AsBool);

                        break;
                    }

                #endregion

                #region StorySaveInt

                case "StorySaveInt": {
                        if (parameters == null)
                            break;

                        var saveName = ParseVarFunction(parameters.Get(0, "name"), thisElement, customVariables);
                        if (saveName == null || !saveName.IsString)
                            break;

                        var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables);
                        
                        if (ParseVarFunction(parameters.Get(2, "relative"), thisElement, customVariables).AsBool)
                            value += StoryManager.inst.CurrentSave.LoadInt(saveName, 0);

                        StoryManager.inst.CurrentSave.SaveInt(saveName, value.AsInt);

                        break;
                    }

                #endregion

                #region StorySaveFloat

                case "StorySaveFloat": {
                        if (parameters == null)
                            break;

                        var saveName = ParseVarFunction(parameters.Get(0, "name"), thisElement, customVariables);
                        if (saveName == null || !saveName.IsString)
                            break;

                        var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables);
                        
                        if (ParseVarFunction(parameters.Get(2, "relative"), thisElement, customVariables).AsBool)
                            value += StoryManager.inst.CurrentSave.LoadFloat(saveName, 0);

                        StoryManager.inst.CurrentSave.SaveFloat(saveName, value.AsFloat);

                        break;
                    }

                #endregion

                #region StorySaveString

                case "StorySaveString": {
                        if (parameters == null)
                            break;

                        var saveName = ParseVarFunction(parameters.Get(0, "name"), thisElement, customVariables);
                        if (saveName == null || !saveName.IsString)
                            break;

                        var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables);
                        if (ParseVarFunction(parameters.Get(2, "relative"), thisElement, customVariables).AsBool)
                            value = StoryManager.inst.CurrentSave.LoadString(saveName, string.Empty) + value;

                        StoryManager.inst.CurrentSave.SaveString(saveName, value);

                        break;
                    }

                #endregion

                #region StorySaveJSON

                case "StorySaveJSON": {
                        if (parameters == null)
                            break;

                        var saveName = ParseVarFunction(parameters.Get(0, "name"), thisElement, customVariables);
                        if (saveName == null || !saveName.IsString)
                            break;

                        var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables);

                        StoryManager.inst.CurrentSave.SaveNode(saveName, value);

                        break;
                    }

                #endregion

                #endregion
            }

            if (!string.IsNullOrEmpty(name) && customJSONFunctions.TryGetValue(name, out JSONNode customJSONFunction))
                ParseFunction(customJSONFunction, thisElement, customVariables);
        }

        public virtual JSONNode VarFunction(JSONNode jn, string name, JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            switch (name)
            {
                #region Switch

                // Parses a variable from a switch argument.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = integer variable.
                // 1 = default item.
                // 2 = array of items to return based on the integer variable provided.
                // Example:
                // [
                //   2,
                //   "This is the default item!",
                //   [
                //     "Item 0",
                //     "Item 1",
                //     "Item 2", < since the integer variable is "2", this item will be returned.
                //     "Item 3"
                //   ]
                // ]
                // 
                // - JSON Object Structure -
                // "var"
                // "default"
                // "case"
                // Example:
                // {
                //   "var": "-1",
                //   "default": "i AM DEFAULT, no SPEAK TO me", < since "var" is out of the range of the case, it returns this default item.
                //   "case": [
                //     "Some kind of item.",
                //     "Another item...",
                //     {
                //       "value": "The item is an object?!" < items can be objects.
                //     }
                //   ]
                // }
                case "Switch": {
                        if (parameters == null)
                            break;

                        var variable = ParseVarFunction(parameters.Get(0, "var"), thisElement, customVariables);
                        var defaultItem = ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables);
                        var caseParam = ParseVarFunction(parameters.Get(2, "case"), thisElement, customVariables);

                        if (caseParam.IsArray && (!variable.IsNumber || variable < 0 || variable >= caseParam.Count))
                            return defaultItem;
                        if (caseParam.IsObject && (!variable.IsString || caseParam[variable.Value] == null))
                            return defaultItem;

                        return ParseVarFunction(variable.IsNumber ? caseParam[variable.AsInt] : caseParam[variable.Value], thisElement, customVariables);
                    }

                #endregion

                #region If

                // Parses a variable from an if argument.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // The item itself is an array, so these values represent items' values in the array.
                // "if" = check function.
                // "return" = returns a specified item.
                // "else" = if this item should be returned instead if the previous result is false. This value is optional.
                // Example:
                // [
                //   {
                //     "if": "True",
                //     "return": "I have a place."
                //   },
                //   {
                //     "if": "False", < because this is false and "else" is true, the return value of this item is returned.
                //     "else": True,
                //     "return": "I no longer have a place."
                //   }
                // ]
                // 
                // - JSON Object Structure -
                // "if"
                // "return"
                // Example:
                // {
                //   "if": "True",
                //   "return": {
                //     "value": "i AM value!!!"
                //   }
                // }
                case "If": {
                        if (parameters == null)
                            break;

                        if (parameters.IsArray)
                        {
                            JSONNode variable = null;
                            var result = false;
                            for (int i = 0; i < parameters.Count; i++)
                            {
                                var check = parameters[i];
                                if (check.IsString)
                                {
                                    if (!result)
                                        return check;
                                    continue;
                                }

                                var ifCheck = check["if"];

                                if (ifCheck == null && !result)
                                    return check.IsNull ? check : check["return"];

                                var elseCheck = check["else"].AsBool;
                                if (result && !elseCheck)
                                    continue;

                                result = ParseIfFunction(ifCheck, thisElement);
                                if (result)
                                    variable = check["return"];
                            }

                            return variable;
                        }

                        if (parameters["if"] != null && parameters["return"] != null && ParseIfFunction(parameters["if"], thisElement))
                            return parameters["return"];

                        break;
                    }

                #endregion

                #region Bool

                // Parses a true or false variable from an if argument.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // The item itself is an array, so these values represent items' values in the array.
                // "if" = check function.
                // "else" = if this item should be returned instead if the previous result is false. This value is optional.
                // Example:
                // [
                //   {
                //     "if": "True"
                //   },
                //   {
                //     "if": "False", < because this is false and "else" is true, the return value of this item is returned.
                //     "else": True
                //   }
                // ]
                // 
                // - JSON Object Structure -
                // "if"
                // Example:
                // {
                //   "if": "True" < "True" is returned because the boolean function is true
                // }
                case "Bool": {
                        if (parameters == null)
                            break;

                        if (parameters.IsArray)
                        {
                            var result = false;
                            for (int i = 0; i < parameters.Count; i++)
                            {
                                var check = parameters[i];
                                var elseCheck = check["else"].AsBool;
                                if (result && !elseCheck)
                                    continue;

                                result = ParseIfFunction(check["if"], thisElement);
                            }

                            return result.ToString();
                        }

                        return (parameters["if"] != null && parameters["return"] != null && ParseIfFunction(parameters["if"], thisElement)).ToString();
                    }

                #endregion

                #region ReadJSONFile

                // Parses a JSON file and returns it.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = file name.
                // 1 = set main directory.
                // Example:
                // [
                //   "story_mode",
                //   "{{BepInExAssetsDirectory}}Interfaces"
                // ]
                //
                // - JSON Object Structure -
                // "file"
                // "path"
                // Example:
                // {
                //   "file": "some_interface",
                //   "path": "beatmaps/interfaces", < (optional)
                // }
                case "ReadJSONFile": {
                        if (parameters == null)
                            break;

                        var file = ParseVarFunction(parameters.Get(0, "file"), thisElement, customVariables);
                        if (file == null)
                            break;

                        var mainDirectory = ParseVarFunction(parameters.Get(1, "path"));

                        if (mainDirectory == null || !mainDirectory.IsString || !mainDirectory.Value.Contains(RTFile.ApplicationDirectory))
                            mainDirectory = RTFile.CombinePaths(RTFile.ApplicationDirectory, mainDirectory);

                        var path = RTFile.CombinePaths(mainDirectory, file + FileFormat.LSI.Dot());

                        if (!RTFile.FileExists(path))
                        {
                            CoreHelper.LogError($"Interface {file} does not exist!");

                            break;
                        }

                        return JSON.Parse(RTFile.ReadFromFile(path));
                    }

                #endregion

                #region NoParse

                case "NoParse": {
                        if (parameters == null)
                            break;

                        return parameters.Get(0, "value");
                    }

                #endregion

                #region StoryLoadBoolVar

                // Parses a variable from the current story save.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = variable name to load from the story save.
                // 1 = default value if there is no value.
                // Example:
                // [
                //   "DOC02WATER",
                //   "False"
                // ]
                // 
                // - JSON Object Structure -
                // "load"
                // "default"
                // Example:
                // {
                //   "load": "NULL",
                //   "default": "False" < returns this value since NULL does not exist.
                // }
                case "StoryLoadBoolVar": {
                        return StoryManager.inst.CurrentSave.LoadBool(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables)).ToString();
                    }

                #endregion

                #region StoryLoadIntVar

                // Parses a variable from the current story save.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = variable name to load from the story save.
                // 1 = default value if there is no value.
                // Example:
                // [
                //   "DOC02WATER",
                //   "0"
                // ]
                // 
                // - JSON Object Structure -
                // "load"
                // "default"
                // Example:
                // {
                //   "load": "NULL",
                //   "default": "0" < returns this value since NULL does not exist.
                // }
                case "StoryLoadIntVar": {
                        return StoryManager.inst.CurrentSave.LoadInt(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables)).ToString();
                    }

                #endregion

                #region StoryLoadFloatVar

                // Parses a variable from the current story save.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = variable name to load from the story save.
                // 1 = default value if there is no value.
                // Example:
                // [
                //   "DOC02WATER",
                //   "0"
                // ]
                // 
                // - JSON Object Structure -
                // "load"
                // "default"
                // Example:
                // {
                //   "load": "NULL",
                //   "default": "0" < returns this value since NULL does not exist.
                // }
                case "StoryLoadFloatVar": {
                        return StoryManager.inst.CurrentSave.LoadFloat(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables)).ToString();
                    }

                #endregion

                #region StoryLoadStringVar

                // Parses a variable from the current story save.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = variable name to load from the story save.
                // 1 = default value if there is no value.
                // Example:
                // [
                //   "DOC02WATER",
                //   "False"
                // ]
                // 
                // - JSON Object Structure -
                // "load"
                // "default"
                // Example:
                // {
                //   "load": "NULL",
                //   "default": "False" < returns this value since NULL does not exist.
                // }
                case "StoryLoadStringVar": {
                        return StoryManager.inst.CurrentSave.LoadString(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables)).ToString();
                    }

                #endregion

                #region StoryLoadJSONVar

                // Parses a variable from the current story save.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = variable name to load from the story save.
                // 1 = default value if there is no value.
                // Example:
                // [
                //   "DOC02WATER",
                //   "False"
                // ]
                // 
                // - JSON Object Structure -
                // "load"
                // "default"
                // Example:
                // {
                //   "load": "NULL",
                //   "default": "False" < returns this value since NULL does not exist.
                // }
                case "StoryLoadJSONVar": {
                        return StoryManager.inst.CurrentSave.LoadJSON(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables));
                    }

                #endregion

                #region FormatString

                // Parses a variable from a formatted string.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = string to format.
                // 1 = array of values to format to the string.
                // Example:
                // [
                //   "Format {0}!", < returns "Format this!".
                //   [
                //     "this"
                //   ]
                // ]
                // 
                // - JSON Object Structure -
                // "format"
                // "args"
                // Example:
                // {
                //   "format": "Noo don't {0}!",
                //   "args": [
                //     {
                //       "name": "Switch",
                //       "params": {
                //         "var": "0",
                //         "default": "format default",
                //         "case": [
                //           "format me",
                //           "format yourself"
                //         ]
                //       }
                //     }
                //   ]
                // }
                case "FormatString": {
                        if (parameters == null)
                            break;

                        var str = ParseVarFunction(parameters.Get(0, "format"), thisElement, customVariables);
                        var args = ParseVarFunction(parameters.Get(1, "args"), thisElement, customVariables);
                        if (args == null || !args.IsArray)
                            return str;

                        var strArgs = new object[args.Count];
                        for (int i = 0; i < strArgs.Length; i++)
                            strArgs[i] = ParseVarFunction(args[i], thisElement, customVariables).Value;

                        return string.Format(str, strArgs);
                    }

                #endregion

                #region StringRemoveAt

                case "StringRemoveAt": {
                        var str = ParseVarFunction(parameters.Get(0, "str"), thisElement, customVariables);
                        if (str == null || !str.IsString || string.IsNullOrEmpty(str))
                            break;

                        var index = ParseVarFunction(parameters.Get(1, "index"), thisElement, customVariables);
                        var count = ParseVarFunction(parameters.Get(2, "count"), thisElement, customVariables);
                        if (count == null)
                            count = 1;
                        return str.Value.Remove(index.IsString && index.Value == "end" ? str.Value.Length - 1 : index.AsInt, count);
                    }

                #endregion

                #region StringReplace

                case "StringReplace": {
                        var str = ParseVarFunction(parameters.Get(0, "str"), thisElement, customVariables);
                        if (str == null || !str.IsString)
                            break;

                        var oldVar = ParseVarFunction(parameters.Get(1, "old"), thisElement, customVariables);
                        var newVar = ParseVarFunction(parameters.Get(2, "new"), thisElement, customVariables);
                        return str.Value.Replace(oldVar.Value, newVar.Value);
                    }

                #endregion
                    
                #region StringInsert

                case "StringInsert": {
                        var str = ParseVarFunction(parameters.Get(0, "str"), thisElement, customVariables);
                        if (str == null || !str.IsString)
                            break;

                        var index = ParseVarFunction(parameters.Get(1, "index"), thisElement, customVariables);
                        var value = ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables);
                        return str.Value.Insert(index.AsInt, value.Value);
                    }

                #endregion

                #region ToStoryNumber

                case "ToStoryNumber": {
                        if (parameters == null)
                            break;

                        var number = ParseVarFunction(parameters.Get(0, "num"), thisElement, customVariables);
                        if (number == null || !number.IsNumber)
                            break;

                        return RTString.ToStoryNumber(number.AsInt);
                    }

                #endregion

                #region LoadProfileValue

                case "LoadProfileValue": {
                        if (parameters == null || !LegacyPlugin.player || LegacyPlugin.player.memory == null)
                            break;

                        var varName = ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables);
                        if (varName == null || !varName.IsString)
                            break;
                        return LegacyPlugin.player.memory[varName.Value];
                    }

                #endregion

                #region StoryLevelID

                case "StoryLevelID": {
                        if (parameters == null)
                            break;

                        var chapterIndex = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables);
                        var levelIndex = ParseVarFunction(parameters.Get(1, "level"), thisElement, customVariables);
                        var bonus = ParseVarFunction(parameters.Get(2, "bonus"), thisElement, customVariables);
                        var chapters = bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters;

                        return chapters.TryGetAt(chapterIndex.AsInt, out StoryMode.Chapter chapter) && chapter.levels.TryGetAt(levelIndex.AsInt, out StoryMode.LevelSequence level) ? level.id : ParseVarFunction(parameters.Get(3, "default"), thisElement, customVariables);
                    }

                #endregion

                #region StoryLevelName

                case "StoryLevelName": {
                        if (parameters == null)
                            break;

                        var chapterIndex = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables);
                        var levelIndex = ParseVarFunction(parameters.Get(1, "level"), thisElement, customVariables);
                        var bonus = ParseVarFunction(parameters.Get(2, "bonus"), thisElement, customVariables);
                        var chapters = bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters;

                        return chapters.TryGetAt(chapterIndex.AsInt, out StoryMode.Chapter chapter) && chapter.levels.TryGetAt(levelIndex.AsInt, out StoryMode.LevelSequence level) ? level.name : ParseVarFunction(parameters.Get(3, "default"), thisElement, customVariables);
                    }

                #endregion
                    
                #region StoryLevelSongTitle

                case "StoryLevelSongTitle": {
                        if (parameters == null)
                            break;

                        var chapterIndex = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables);
                        var levelIndex = ParseVarFunction(parameters.Get(1, "level"), thisElement, customVariables);
                        var bonus = ParseVarFunction(parameters.Get(2, "bonus"), thisElement, customVariables);
                        var chapters = bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters;

                        return chapters.TryGetAt(chapterIndex.AsInt, out StoryMode.Chapter chapter) && chapter.levels.TryGetAt(levelIndex.AsInt, out StoryMode.LevelSequence level) ? level.songTitle : ParseVarFunction(parameters.Get(3, "default"), thisElement, customVariables);
                    }

                #endregion
                    
                #region StoryLevelCount

                case "StoryLevelCount": {
                        if (parameters == null)
                            break;

                        var chapterIndex = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables);
                        var bonus = ParseVarFunction(parameters.Get(1, "bonus"), thisElement, customVariables);
                        var chapters = bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters;

                        return chapters.TryGetAt(chapterIndex.AsInt, out StoryMode.Chapter chapter) ? chapter.Count : ParseVarFunction(parameters.Get(2, "default"), thisElement, customVariables);
                    }

                #endregion

                #region GetAsset

                case "GetAsset": {
                        if (parameters == null)
                            break;

                        var assetPath = parameters.Get(0, "path");
                        return AssetPack.GetFile(assetPath);
                    }

                #endregion

                #region RandomRange
                    
                case "RandomRange": {
                        if (parameters == null)
                            return 0f;

                        return UnityRandom.Range(parameters.Get(0, "min").AsFloat, parameters.Get(1, "max").AsFloat);
                    }

                #endregion
                    
                #region RandomRangeInt
                    
                case "RandomRangeInt": {
                        if (parameters == null)
                            return 0f;

                        return UnityRandom.Range(parameters.Get(0, "min").AsInt, parameters.Get(1, "max").AsInt);
                    }

                #endregion

                #region GetConfigSetting

                case "GetConfigSetting": {
                        if (parameters == null)
                            break;

                        var configName = ParseVarFunction(parameters.Get(0, "config"), thisElement, customVariables);
                        if (!LegacyPlugin.configs.TryFind(x => x.Name == configName, out BaseConfig config))
                            break;

                        var section = ParseVarFunction(parameters.Get(1, "section"), thisElement, customVariables);
                        var key = ParseVarFunction(parameters.Get(2, "key"), thisElement, customVariables);
                        if (config.Settings.TryFind(x => x.Section == section && x.Key == key, out BaseSetting setting))
                            return setting.BoxedValue?.ToString() ?? jn;

                        break;
                    }

                #endregion

                #region ParseMath

                // Parses a variable from a math evaluation.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = math evaluation.
                // 1 = variables array.
                // Example:
                // [
                //   "1 + 1", < returns 2
                //   [ ] < variables can be left empty
                // ]
                // 
                // - JSON Object Structure -
                // "evaluate"
                // "vars"
                // Example:
                // {
                //   "evaluate": "10 + VAR1 + VAR2", < returns 20
                //   "vars": [ < can include multiple variables
                //     {
                //       "n": "VAR1",
                //       "v": "5" < variable is just 5
                //     },
                //     {
                //       "n": "VAR2",
                //       "v": { < variable can be a parse function
                //         "name": "ParseMath",
                //         "params": [
                //           "1 * 5"
                //         ]
                //       }
                //     }
                //   ]
                // }
                case "ParseMath": {
                        if (parameters == null)
                            break;

                        Dictionary<string, float> vars = null;

                        var jnVars = ParseVarFunction(parameters.Get(1, "vars"), thisElement, customVariables);
                        if (jnVars != null)
                        {
                            vars = new Dictionary<string, float>();
                            for (int i = 0; i < jnVars.Count; i++)
                            {
                                var item = jnVars[i];
                                if (string.IsNullOrEmpty(item["n"]))
                                    continue;
                                var val = ParseVarFunction(item["v"], thisElement, customVariables);
                                if (!val.IsNumber)
                                    continue;
                                vars[name] = val;
                            }
                        }

                        if (customVariables != null)
                        {
                            if (vars == null)
                                vars = new Dictionary<string, float>();
                            foreach (var variable in customVariables)
                                if (variable.Value != null && variable.Value.IsNumber)
                                    vars[variable.Key] = variable.Value.AsFloat;
                        }

                        return RTMath.Parse(ParseVarFunction(parameters.Get(0, "evaluate"), thisElement, customVariables), vars).ToString();
                    }

                    #endregion
            }

            if (!string.IsNullOrEmpty(name) && customJSONFunctions.TryGetValue(name, out JSONNode customJSONFunction))
                return ParseVarFunction(customJSONFunction, thisElement, customVariables);

            return jn;
        }

        #endregion
    }
}
