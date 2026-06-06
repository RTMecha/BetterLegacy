using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Arcade.Interfaces;
using BetterLegacy.Companion.Entity;
using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Interfaces;
using BetterLegacy.Story;

namespace BetterLegacy.Core
{
    /// <summary>
    /// Helper class for parsing JSON as functions. Custom functions can either be registered via <see cref="customJSONFunctions"/> or overriding the function classes.
    /// </summary>
    /// <typeparam name="T">Type of the element this parser supports.</typeparam>
    public class JSONFunctionParser<T> : Exists
    {
        public JSONFunctionParser() => RegisterFunctions();

        #region Values

        public Dictionary<string, Func<JSONNode, T, Dictionary<string, JSONNode>, bool>> predicates = new Dictionary<string, Func<JSONNode, T, Dictionary<string, JSONNode>, bool>>();

        public Dictionary<string, Action<JSONNode, T, Dictionary<string, JSONNode>>> actions = new Dictionary<string, Action<JSONNode, T, Dictionary<string, JSONNode>>>();

        public Dictionary<string, Func<JSONNode, T, Dictionary<string, JSONNode>, JSONNode>> variables = new Dictionary<string, Func<JSONNode, T, Dictionary<string, JSONNode>, JSONNode>>();

        /// <summary>
        /// Dictionary of custom JSON functions. A set of functions can be registered to this as one function.
        /// </summary>
        public Dictionary<string, JSONNode> customJSONFunctions = new Dictionary<string, JSONNode>();

        #endregion

        #region Functions

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

        /// <summary>
        /// Registers the default functions.
        /// </summary>
        public virtual void RegisterFunctions()
        {
            #region Predicates

            #region Main

            RegisterPredicate(True);
            RegisterPredicate(False);
            RegisterPredicate(GetSettingBool);
            RegisterPredicate(GetSettingIntEquals);
            RegisterPredicate(GetSettingIntLesserEquals);
            RegisterPredicate(GetSettingIntGreaterEquals);
            RegisterPredicate(GetSettingIntLesser);
            RegisterPredicate(GetSettingIntGreater);
            RegisterPredicate(IsScene);
            RegisterPredicate(Equals);
            RegisterPredicate(LesserEquals);
            RegisterPredicate(GreaterEquals);
            RegisterPredicate(Lesser);
            RegisterPredicate(Greater);
            RegisterPredicate(HasAsset);
            RegisterPredicate(ConfigSettingEquals);

            #endregion

            #region Player

            RegisterPredicate(PlayerCountEquals);
            RegisterPredicate(PlayerCountLesserEquals);
            RegisterPredicate(PlayerCountGreaterEquals);
            RegisterPredicate(PlayerCountLesser);
            RegisterPredicate(PlayerCountGreater);

            #endregion

            #region Profile

            RegisterPredicate(DisplayNameEquals);
            RegisterPredicate(ProfileLoadIntEquals);
            RegisterPredicate(ProfileLoadIntLesserEquals);
            RegisterPredicate(ProfileLoadIntGreaterEquals);
            RegisterPredicate(ProfileLoadIntLesser);
            RegisterPredicate(ProfileLoadIntGreater);
            RegisterPredicate(ProfileLoadBool);

            #endregion

            #region Story

            RegisterPredicate(StoryChapterEquals);
            RegisterPredicate(StoryChapterLesserEquals);
            RegisterPredicate(StoryChapterGreaterEquals);
            RegisterPredicate(StoryChapterLesser);
            RegisterPredicate(StoryChapterGreater);
            RegisterPredicate(StoryInstalled);
            RegisterPredicate(StoryLoadIntEquals);
            RegisterPredicate(StoryLoadIntLesserEquals);
            RegisterPredicate(StoryLoadIntGreaterEquals);
            RegisterPredicate(StoryLoadIntLesser);
            RegisterPredicate(StoryLoadIntGreater);
            RegisterPredicate(StoryLoadBool);

            #endregion

            #region Ranks

            RegisterPredicate(ChapterFullyRanked);
            RegisterPredicate(LevelRankEquals);
            RegisterPredicate(LevelRankLesserEquals);
            RegisterPredicate(LevelRankGreaterEquals);
            RegisterPredicate(LevelRankLesser);
            RegisterPredicate(LevelRankGreater);
            RegisterPredicate(StoryLevelRankEquals);
            RegisterPredicate(StoryLevelRankLesserEquals);
            RegisterPredicate(StoryLevelRankGreaterEquals);
            RegisterPredicate(StoryLevelRankLesser);
            RegisterPredicate(StoryLevelRankGreater);

            #endregion

            #endregion

            #region Actions

            #region Main

            RegisterAction(LoadScene);
            RegisterAction(UpdateSettingBool);
            RegisterAction(UpdateSettingInt);
            RegisterAction(Wait);
            RegisterAction(Log);
            RegisterAction(Notify);
            RegisterAction(ExitGame);
            RegisterAction(Config);
            RegisterAction(Profile);
            RegisterAction(OpenChangelog);
            RegisterAction(ForLoop);
            RegisterAction(CacheVariable);
            RegisterAction(PlaySound);

            #endregion

            #region Achievements

            RegisterAction(UnlockAchievement);
            RegisterAction(LockAchievement);

            #endregion

            #region Level

            RegisterAction(OpenPlayLevelInterface);
            RegisterAction(LoadLevel);
            RegisterAction(LoadLevelPath);
            RegisterAction(UpdateCurrentLevelProgress);
            RegisterAction(InitLevelMenu);
            RegisterAction(LoadLevels);
            RegisterAction(OnInputsSelected);

            #endregion

            #region Online

            RegisterAction(SetDiscordStatus);
            RegisterAction(OpenLink);
            RegisterAction(ModderDiscord);
            RegisterAction(SourceCode);

            #endregion

            #region Story

            RegisterAction(BeginStoryMode);
            RegisterAction(LoadCurrentStoryInterface);
            RegisterAction(LoadStoryInterface);
            RegisterAction(LoadStoryLevel);
            RegisterAction(SetOnLevelCompleteFunc);
            RegisterAction(LoadStoryCutscene);
            RegisterAction(PlayAllCutscenes);
            RegisterAction(LoadStoryLevelPath);
            RegisterAction(LoadNextStoryLevel);
            RegisterAction(LoadChapterTransition);
            RegisterAction(SaveProfileValue);
            RegisterAction(StorySaveBool);
            RegisterAction(StorySaveInt);
            RegisterAction(StorySaveFloat);
            RegisterAction(StorySaveString);
            RegisterAction(StorySaveJSON);

            #endregion

            #endregion

            #region Variables

            #region Main

            RegisterVariable(Switch);
            RegisterVariable(If);
            RegisterVariable(Bool);
            RegisterVariable(ReadJSONFile);
            RegisterVariable(NoParse);
            RegisterVariable(FormatString);
            RegisterVariable(StringRemoveAt);
            RegisterVariable(StringReplace);
            RegisterVariable(StringInsert);
            RegisterVariable(LoadProfileValue);
            RegisterVariable(GetAsset);
            RegisterVariable(RandomRange);
            RegisterVariable(RandomRangeInt);
            RegisterVariable(GetConfigSetting);
            RegisterVariable(ParseMath);
            RegisterVariable(CombinePaths);

            #endregion

            #region Story

            RegisterVariable(StoryLoadBoolVar);
            RegisterVariable(StoryLoadIntVar);
            RegisterVariable(StoryLoadFloatVar);
            RegisterVariable(StoryLoadStringVar);
            RegisterVariable(StoryLoadJSONVar);
            RegisterVariable(ToStoryNumber);
            RegisterVariable(StoryLevelID);
            RegisterVariable(StoryLevelName);
            RegisterVariable(StoryLevelSongTitle);
            RegisterVariable(StoryLevelCount);

            #endregion

            #endregion
        }

        public void RegisterPredicates(params Func<JSONNode, T, Dictionary<string, JSONNode>, bool>[] predicates)
        {
            for (int i = 0; i < predicates.Length; i++)
                RegisterPredicate(predicates[i]);
        }
        
        public void RegisterPredicate(Func<JSONNode, T, Dictionary<string, JSONNode>, bool> predicate)
        {
            var method = predicate.Method;
            if (method == null)
                return;
            predicates[method.Name] = predicate;
        }
        
        public void RegisterActions(params Action<JSONNode, T, Dictionary<string, JSONNode>>[] actions)
        {
            for (int i = 0; i < actions.Length; i++)
                RegisterAction(actions[i]);
        }
        
        public void RegisterAction(Action<JSONNode, T, Dictionary<string, JSONNode>> action)
        {
            var method = action.Method;
            if (method == null)
                return;
            actions[method.Name] = action;
        }

        public void RegisterVariables(params Func<JSONNode, T, Dictionary<string, JSONNode>, JSONNode>[] variables)
        {
            for (int i = 0; i < variables.Length; i++)
                RegisterVariable(variables[i]);
        }
        
        public void RegisterVariable(Func<JSONNode, T, Dictionary<string, JSONNode>, JSONNode> variable)
        {
            var method = variable.Method;
            if (method == null)
                return;
            variables[method.Name] = variable;
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
                            case "LevelCollection": {
                                    if (split.Length <= 1)
                                        return LevelManager.inst.levelCollectionFunctions.ParseIfFunction(jn, LevelManager.CurrentLevelCollection, customVariables, false);

                                    var sub = split[1];
                                    if (LevelManager.LevelCollections.TryFind(x => x.id == sub, out LevelCollection levelCollection))
                                        return LevelManager.inst.levelCollectionFunctions.ParseIfFunction(jn, levelCollection, customVariables, false);
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
                // todo: cleanup this code and make it a proper API-like system
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
                            case "LevelCollection": {
                                    if (split.Length <= 1)
                                    {
                                        LevelManager.inst.levelCollectionFunctions.ParseFunction(jn, LevelManager.CurrentLevelCollection, customVariables, false);
                                        break;
                                    }

                                    var sub = split[1];
                                    if (LevelManager.LevelCollections.TryFind(x => x.id == sub, out LevelCollection levelCollection))
                                        LevelManager.inst.levelCollectionFunctions.ParseFunction(jn, levelCollection, customVariables, false);
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
                            case "LevelCollection": {
                                    if (split.Length <= 1)
                                        return LevelManager.inst.levelCollectionFunctions.ParseVarFunction(jn, LevelManager.CurrentLevelCollection, customVariables, false);

                                    var sub = split[1];
                                    if (LevelManager.LevelCollections.TryFind(x => x.id == sub, out LevelCollection levelCollection))
                                        return LevelManager.inst.levelCollectionFunctions.ParseVarFunction(jn, levelCollection, customVariables, false);
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

        #region Predicate

        #region Main

        public virtual bool True(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null) => true;

        public virtual bool False(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null) => false;

        public virtual bool GetSettingBool(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return DataManager.inst.GetSettingBool(
                ParseVarFunction(parameters.Get(0, "setting"), thisElement, customVariables),
                ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsBool);
        }
        
        public virtual bool GetSettingIntEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return DataManager.inst.GetSettingInt(
                ParseVarFunction(parameters.Get(0, "setting"), thisElement, customVariables),
                ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsInt) == ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool GetSettingIntLesserEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return DataManager.inst.GetSettingInt(
                ParseVarFunction(parameters.Get(0, "setting"), thisElement, customVariables),
                ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsInt) <= ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool GetSettingIntGreaterEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return DataManager.inst.GetSettingInt(
                ParseVarFunction(parameters.Get(0, "setting"), thisElement, customVariables),
                ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsInt) >= ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool GetSettingIntLesser(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return DataManager.inst.GetSettingInt(
                ParseVarFunction(parameters.Get(0, "setting"), thisElement, customVariables),
                ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsInt) < ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool GetSettingIntGreater(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return DataManager.inst.GetSettingInt(
                ParseVarFunction(parameters.Get(0, "setting"), thisElement, customVariables),
                ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsInt) > ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool IsScene(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == ParseVarFunction(parameters.Get(0, "scene"), thisElement, customVariables);
        }

        public virtual bool Equals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return ParseVarFunction(parameters.Get(0, "first"), thisElement, customVariables) == ParseVarFunction(parameters.Get(1, "second"), thisElement, customVariables);
        }
        
        public virtual bool LesserEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return ParseVarFunction(parameters.Get(0, "first"), thisElement, customVariables).AsFloat <= ParseVarFunction(parameters.Get(1, "second"), thisElement, customVariables).AsFloat;
        }
        
        public virtual bool GreaterEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return ParseVarFunction(parameters.Get(0, "first"), thisElement, customVariables).AsFloat >= ParseVarFunction(parameters.Get(1, "second"), thisElement, customVariables).AsFloat;
        }
        
        public virtual bool Lesser(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return ParseVarFunction(parameters.Get(0, "first"), thisElement, customVariables).AsFloat < ParseVarFunction(parameters.Get(1, "second"), thisElement, customVariables).AsFloat;
        }
        
        public virtual bool Greater(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return ParseVarFunction(parameters.Get(0, "first"), thisElement, customVariables).AsFloat > ParseVarFunction(parameters.Get(1, "second"), thisElement, customVariables).AsFloat;
        }
        
        public virtual bool HasAsset(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return AssetPack.TryGetFile(parameters.Get(0, "path"), out string filePath);
        }
        
        public virtual bool ConfigSettingEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            var configName = ParseVarFunction(parameters.Get(0, "config"), thisElement, customVariables);
            if (!LegacyPlugin.configs.TryFind(x => x.Name == configName, out BaseConfig config))
                return false;

            var section = ParseVarFunction(parameters.Get(1, "section"), thisElement, customVariables);
            var key = ParseVarFunction(parameters.Get(2, "key"), thisElement, customVariables);
            return config.Settings.TryFind(x => x.Section == section && x.Key == key, out BaseSetting setting) &&
                setting.BoxedValue?.ToString() == ParseVarFunction(parameters.Get(3, "value"), thisElement, customVariables);
        }

        #endregion

        #region Player

        public virtual bool PlayerCountEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return PlayerManager.Players.Count == ParseVarFunction(parameters.Get(0, "count"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool PlayerCountLesserEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return PlayerManager.Players.Count <= ParseVarFunction(parameters.Get(0, "count"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool PlayerCountGreaterEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return PlayerManager.Players.Count >= ParseVarFunction(parameters.Get(0, "count"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool PlayerCountLesser(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return PlayerManager.Players.Count < ParseVarFunction(parameters.Get(0, "count"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool PlayerCountGreater(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return PlayerManager.Players.Count > ParseVarFunction(parameters.Get(0, "count"), thisElement, customVariables).AsInt;
        }

        #endregion

        #region Profile

        public virtual bool DisplayNameEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return CoreConfig.Instance.DisplayName.Value == ParseVarFunction(parameters.Get(0, "user"), thisElement, customVariables).Value;
        }
        
        public virtual bool ProfileLoadIntEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null || !LegacyPlugin.player || LegacyPlugin.player.memory == null)
                return false;

            var varName = ParseVarFunction(parameters.Get(0, "var_name"));
            if (varName == null || !varName.IsString)
                return false;

            var profileValue = LegacyPlugin.player.memory[ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables).Value];
            if (profileValue == null)
                profileValue = ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables);

            return profileValue.AsInt == ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
        }

        public virtual bool ProfileLoadIntLesserEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null || !LegacyPlugin.player || LegacyPlugin.player.memory == null)
                return false;

            var varName = ParseVarFunction(parameters.Get(0, "var_name"));
            if (varName == null || !varName.IsString)
                return false;

            var profileValue = LegacyPlugin.player.memory[ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables).Value];
            if (profileValue == null)
                profileValue = ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables);

            return profileValue.AsInt <= ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool ProfileLoadIntGreaterEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null || !LegacyPlugin.player || LegacyPlugin.player.memory == null)
                return false;

            var varName = ParseVarFunction(parameters.Get(0, "var_name"));
            if (varName == null || !varName.IsString)
                return false;

            var profileValue = LegacyPlugin.player.memory[ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables).Value];
            if (profileValue == null)
                profileValue = ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables);

            return profileValue.AsInt >= ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool ProfileLoadIntLesser(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null || !LegacyPlugin.player || LegacyPlugin.player.memory == null)
                return false;

            var varName = ParseVarFunction(parameters.Get(0, "var_name"));
            if (varName == null || !varName.IsString)
                return false;

            var profileValue = LegacyPlugin.player.memory[ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables).Value];
            if (profileValue == null)
                profileValue = ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables);

            return profileValue.AsInt < ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool ProfileLoadIntGreater(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null || !LegacyPlugin.player || LegacyPlugin.player.memory == null)
                return false;

            var varName = ParseVarFunction(parameters.Get(0, "var_name"));
            if (varName == null || !varName.IsString)
                return false;

            var profileValue = LegacyPlugin.player.memory[ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables).Value];
            if (profileValue == null)
                profileValue = ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables);

            return profileValue.AsInt > ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool ProfileLoadBool(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return LegacyPlugin.player && LegacyPlugin.player.memory != null && LegacyPlugin.player.memory[ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables).Value].AsBool;
        }

        #endregion

        #region Story

        public virtual bool StoryChapterEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return StoryManager.inst.CurrentSave.LoadInt("Chapter", 0) == ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool StoryChapterLesserEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return StoryManager.inst.CurrentSave.LoadInt("Chapter", 0) <= ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool StoryChapterGreaterEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return StoryManager.inst.CurrentSave.LoadInt("Chapter", 0) >= ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool StoryChapterLesser(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return StoryManager.inst.CurrentSave.LoadInt("Chapter", 0) < ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool StoryChapterGreater(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return StoryManager.inst.CurrentSave.LoadInt("Chapter", 0) > ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool StoryInstalled(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null) => StoryManager.inst && RTFile.DirectoryExists(StoryManager.StoryAssetsPath);

        public virtual bool StoryLoadIntEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return StoryManager.inst.CurrentSave.LoadInt(
                ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables),
                ParseVarFunction(parameters.Get(1, "default")).AsInt) == ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool StoryLoadIntLesserEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return StoryManager.inst.CurrentSave.LoadInt(
                ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables),
                ParseVarFunction(parameters.Get(1, "default")).AsInt) <= ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool StoryLoadIntGreaterEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return StoryManager.inst.CurrentSave.LoadInt(
                ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables),
                ParseVarFunction(parameters.Get(1, "default")).AsInt) >= ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
        }

        public virtual bool StoryLoadIntLesser(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return StoryManager.inst.CurrentSave.LoadInt(
                ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables),
                ParseVarFunction(parameters.Get(1, "default")).AsInt) < ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
        }

        public virtual bool StoryLoadIntGreater(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return StoryManager.inst.CurrentSave.LoadInt(
                ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables),
                ParseVarFunction(parameters.Get(1, "default")).AsInt) > ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables).AsInt;
        }

        public virtual bool StoryLoadBool(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return StoryManager.inst.CurrentSave.LoadBool(
                ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables),
                ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsBool);
        }

        #endregion

        #region Ranks

        public virtual bool ChapterFullyRanked(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

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

        public virtual bool LevelRankEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return LevelManager.CurrentLevel.saveData && LevelManager.GetLevelRank(LevelManager.CurrentLevel) == ParseVarFunction(parameters.Get(0, "rank"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool LevelRankLesserEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return LevelManager.CurrentLevel.saveData && LevelManager.GetLevelRank(LevelManager.CurrentLevel) <= ParseVarFunction(parameters.Get(0, "rank"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool LevelRankGreaterEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return LevelManager.CurrentLevel.saveData && LevelManager.GetLevelRank(LevelManager.CurrentLevel) >= ParseVarFunction(parameters.Get(0, "rank"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool LevelRankLesser(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return LevelManager.CurrentLevel.saveData && LevelManager.GetLevelRank(LevelManager.CurrentLevel) < ParseVarFunction(parameters.Get(0, "rank"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool LevelRankGreater(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            return LevelManager.CurrentLevel.saveData && LevelManager.GetLevelRank(LevelManager.CurrentLevel) > ParseVarFunction(parameters.Get(0, "rank"), thisElement, customVariables).AsInt;
        }

        public virtual bool StoryLevelRankEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables).Value;

            return StoryManager.inst.CurrentSave.Saves.TryFind(x => x.ID == id, out SaveData playerData) && LevelManager.GetLevelRank(playerData) == ParseVarFunction(parameters.Get(1, "rank"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool StoryLevelRankLesserEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables).Value;

            return StoryManager.inst.CurrentSave.Saves.TryFind(x => x.ID == id, out SaveData playerData) && LevelManager.GetLevelRank(playerData) <= ParseVarFunction(parameters.Get(1, "rank"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool StoryLevelRankGreaterEquals(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables).Value;

            return StoryManager.inst.CurrentSave.Saves.TryFind(x => x.ID == id, out SaveData playerData) && LevelManager.GetLevelRank(playerData) >= ParseVarFunction(parameters.Get(1, "rank"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool StoryLevelRankLesser(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables).Value;

            return StoryManager.inst.CurrentSave.Saves.TryFind(x => x.ID == id, out SaveData playerData) && LevelManager.GetLevelRank(playerData) < ParseVarFunction(parameters.Get(1, "rank"), thisElement, customVariables).AsInt;
        }
        
        public virtual bool StoryLevelRankGreater(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return false;

            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables).Value;

            return StoryManager.inst.CurrentSave.Saves.TryFind(x => x.ID == id, out SaveData playerData) && LevelManager.GetLevelRank(playerData) > ParseVarFunction(parameters.Get(1, "rank"), thisElement, customVariables).AsInt;
        }

        #endregion

        #endregion

        #region Action

        #region Main

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
        public virtual void LoadScene(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            var sceneName = ParseVarFunction(parameters.Get(0, "scene"), thisElement, customVariables);
            if (sceneName == null || !sceneName.IsString)
                return;

            LevelManager.IsArcade = ParseVarFunction(parameters.Get(2, "is_arcade"), thisElement, customVariables);
            var showLoading = ParseVarFunction(parameters.Get(1, "show_loading"), thisElement, customVariables);

            if (showLoading != null)
                SceneManager.inst.LoadScene(sceneName, Parser.TryParse(showLoading, true));
            else
                SceneManager.inst.LoadScene(sceneName);
        }

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
        public virtual void UpdateSettingBool(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["setting"] == null || parameters["value"] == null))
                return;

            var isArray = parameters.IsArray;
            var settingName = isArray ? parameters[0] : parameters["setting"];
            DataManager.inst.UpdateSettingBool(settingName,
                isArray && parameters.Count > 2 && parameters[2].AsBool || parameters.IsObject && parameters["relative"] != null && parameters["relative"].AsBool ? !DataManager.inst.GetSettingBool(settingName) : isArray ? parameters[1].AsBool : parameters["value"].AsBool);
        }

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
        public virtual void UpdateSettingInt(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["setting"] == null || parameters["value"] == null))
                return;

            var isArray = parameters.IsArray;
            var settingName = isArray ? parameters[0] : parameters["setting"];

            DataManager.inst.UpdateSettingInt(settingName,
                (isArray ? parameters[1].AsInt : parameters["value"].AsInt) +
                (isArray && parameters.Count > 2 && parameters[2].AsBool ||
                parameters.IsObject && parameters["relative"] != null && parameters["relative"].AsBool ?
                        DataManager.inst.GetSettingInt(settingName) : 0));
        }

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
        public virtual void Wait(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            var t = ParseVarFunction(parameters.Get(0, "t"), thisElement, customVariables);
            if (t == null)
                return;

            var func = ParseVarFunction(parameters.Get(1, "func"), thisElement, customVariables);
            if (func == null)
                return;

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
        }

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
        public virtual void Log(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            var msg = ParseVarFunction(parameters.Get(0, "msg"), thisElement, customVariables);
            if (msg == null)
                return;

            CoreHelper.Log(msg);
        }

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
        public virtual void Notify(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            var color = Color.gray;
            var jnColor = ParseVarFunction(parameters.Get(1, "col"), thisElement, customVariables);
            if (jnColor != null)
                color = RTColors.HexToColor(jnColor);

            var fontSize = 30;
            var jnFontSize = ParseVarFunction(parameters.Get(2, "size"), thisElement, customVariables);
            if (jnFontSize != null)
                fontSize = jnFontSize.AsInt;

            CoreHelper.Notify(ParseVarFunction(parameters.Get(0, "msg"), thisElement, customVariables), color, fontSize);
        }

        // Exits the game.
        // Function has no parameters.
        public virtual void ExitGame(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null) => LegacyPlugin.QuitGame();

        // Opens the Config Manager UI.
        // Function has no parameters.
        public virtual void Config(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null) => ConfigManager.inst.Show();

        public virtual void Profile(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null) => ProfileInterface.Init();

        public virtual void OpenChangelog(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null) => InterfaceManager.inst.OpenChangelog();

        public virtual void ForLoop(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            var varName = ParseVarFunction(parameters.Get(3, "var_name"), thisElement, customVariables);
            if (varName == null || !varName.IsString || varName == string.Empty)
                varName = "index";
            var index = ParseVarFunction(parameters.Get(0, "index"), thisElement, customVariables).AsInt;
            var count = ParseVarFunction(parameters.Get(1, "count"), thisElement, customVariables).AsInt;
            var func = ParseVarFunction(parameters.Get(2, "func"), thisElement, customVariables);
            if (func == null)
                return;

            index = RTMath.Clamp(index, 0, count - 1);

            for (int i = index; i < count; i++)
            {
                var loopVar = new Dictionary<string, JSONNode>();
                if (customVariables != null)
                {
                    foreach (var keyValuePair in customVariables)
                        loopVar[keyValuePair.Key] = keyValuePair.Value;
                }

                loopVar[varName.Value] = i;

                ParseFunction(func, thisElement, loopVar);
            }
        }
        
        public virtual void CacheVariable(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            var varName = parameters.Get(0, "var_name");
            if (varName == null || !varName.IsString)
                return;

            var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables);
            if (value == null)
                return;

            customVariables[varName] = value;
        }

        // Plays a sound. Can either be a default one already loaded in the SoundLibrary or a custom one from the menu's folder.
        // Supports both JSON array and JSON object.
        //
        // - JSON Array Structure -
        // 0 = sound
        // 1 = volume
        // 2 = pitch
        // Example:
        // [
        //   "blip" < plays the blip sound.
        //   "0.3" < sound is quiet.
        //   "2" < sound is fast.
        // ]
        //
        // - JSON Object Structure -
        // "sound"
        // "vol"
        // "pitch"
        // Example:
        // {
        //   "sound": "some kind of sound.ogg" < since this sound does not exist in the SoundLibrary, search for a file with the name. If it exists, play the sound.
        //   "vol": "1" < default
        //   "pitch": "0.5" < slow
        // }
        public virtual void PlaySound(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            string sound = ParseVarFunction(parameters.Get(0, "sound"));
            if (string.IsNullOrEmpty(sound))
                return;

            float volume = 1f;
            var volumeJN = ParseVarFunction(parameters.Get(1, "vol"));
            if (volumeJN != null)
                volume = volumeJN;

            float pitch = 1f;
            var pitchJN = ParseVarFunction(parameters.Get(2, "pitch"));
            if (pitchJN != null)
                pitch = pitchJN;

            if (SoundManager.inst.TryGetSound(sound, out AudioClip audioClip))
            {
                SoundManager.inst.PlaySound(SystemManager.inst.gameObject, audioClip, volume, pitch);
                return;
            }

            if (!InterfaceManager.inst || !InterfaceManager.inst.CurrentInterface || !InterfaceManager.inst.CurrentInterface.TryGetFile(sound, out string filePath))
                return;

            var audioType = RTFile.GetAudioType(filePath);
            if (audioType == AudioType.MPEG)
                SoundManager.inst.PlaySound(SystemManager.inst.gameObject, LSAudio.CreateAudioClipUsingMP3File(filePath), volume, pitch);
            else
                CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip($"file://{filePath}", audioType, audioClip => SoundManager.inst.PlaySound(SystemManager.inst.gameObject, audioClip, volume, pitch)));
        }

        #endregion

        #region Achievements

        public virtual void UnlockAchievement(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> variables = null)
        {
            if (parameters != null)
                AchievementManager.inst.UnlockAchievement(parameters.Get(0, "id"));
        }
        
        public virtual void LockAchievement(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> variables = null)
        {
            if (parameters != null)
                AchievementManager.inst.LockAchievement(parameters.Get(0, "id"));
        }

        #endregion

        #region Levels

        public virtual void OpenPlayLevelInterface(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            var source = ParseVarFunction(parameters.Get(0, "source"), thisElement, customVariables).AsInt;
            switch (source)
            {
                case 0: {
                        var id = ParseVarFunction(parameters.Get(1, "id"), thisElement, customVariables);
                        if (id == null || !id.IsString)
                            break;

                        var onReturnFunc = ParseVarFunction(parameters.Get(2, "return_func"), thisElement, customVariables);
                        Action onReturn = onReturnFunc == null ? null : () =>
                        {
                            if (onReturnFunc != null)
                                ParseFunction(onReturnFunc, thisElement, customVariables);
                        };
                        if (LevelManager.CurrentLevelCollection && LevelManager.CurrentLevelCollection.levels.TryFind(x => x && x.id == id, out Level collectionLevel))
                            PlayLevelInterface.Init(collectionLevel, onReturn: onReturn);
                        else if (LevelManager.Levels.TryFind(x => x.id == id, out Level level))
                            PlayLevelInterface.Init(level, onReturn: onReturn);
                        else if (RTSteamManager.inst && RTSteamManager.inst.Initialized && RTSteamManager.inst.Levels.TryFind(x => x.id == id, out Level steamLevel))
                            PlayLevelInterface.Init(steamLevel, onReturn: onReturn);

                        break;
                    } // Arcade ID
                case 1: {
                        var path = ParseVarFunction(parameters.Get(1, "path"), thisElement, customVariables);
                        if (path == null || !path.IsString)
                            break;

                        if (!Level.TryVerify(path, true, out Level level))
                            break;
                        var onReturnFunc = ParseVarFunction(parameters.Get(2, "return_func"), thisElement, customVariables);
                        Action onReturn = onReturnFunc == null ? null : () =>
                        {
                            if (onReturnFunc != null)
                                ParseFunction(onReturnFunc, thisElement, customVariables);
                        };
                        PlayLevelInterface.Init(level, onReturn: onReturn);

                        break;
                    } // Path
                case 2: {
                        var chapter = ParseVarFunction(parameters.Get(1, "chapter"), thisElement, customVariables);
                        if (chapter == null)
                            break;

                        var level = ParseVarFunction(parameters.Get(2, "level"), thisElement, customVariables);
                        if (level == null)
                            break;

                        var cutsceneIndex = ParseVarFunction(parameters.Get(3, "cutscene_index"), thisElement, customVariables).AsInt;
                        var bonus = ParseVarFunction(parameters.Get(5, "bonus"), thisElement, customVariables).AsBool;
                        var skipCutscenes = ParseVarFunction(parameters.GetOrDefault(6, "skip_cutscenes", true), thisElement, customVariables).AsBool;
                        var allowModes = ParseVarFunction(parameters.GetOrDefault(7, "allow_modes", true), thisElement, customVariables).AsBool;
                        var onReturnFunc = ParseVarFunction(parameters.Get(8, "return_func"), thisElement, customVariables);

                        StoryManager.inst.ContinueStory = ParseVarFunction(parameters.Get(4, "continue"), thisElement, customVariables).AsBool;

                        // get story chapter and story level.
                        var storyChapter = (bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters)[chapter];
                        var storyLevel = storyChapter.GetLevel(level);
                        var path = storyLevel.filePath;

                        ArcadeHelper.ResetModifiedStates();
                        StoryManager.inst.SetupStorySelection(new StorySelection
                        {
                            chapter = chapter.AsInt,
                            level = level.AsInt,
                            cutsceneIndex = cutsceneIndex,
                            bonus = bonus,
                            skipCutscenes = skipCutscenes,
                            allowModes = allowModes,
                        }, level => PlayLevelInterface.Init(
                            level: level,
                            onPlay: () => LevelManager.Play(level, () => StoryManager.inst.EndFunctionInterface(storyChapter, storyLevel, path)),
                            onReturn: onReturnFunc == null ? null : () => ParseFunction(onReturnFunc, thisElement, customVariables)));
                        // weird code but oh well

                        break;
                    } // Story
            }
        }

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
        public virtual void LoadLevel(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables);
            if (id == null || !id.IsString)
                return;

            if (LevelManager.Levels.TryFind(x => x.id == id, out Level level))
                LevelManager.Play(level);
            else if (RTSteamManager.inst && RTSteamManager.inst.Initialized && RTSteamManager.inst.Levels.TryFind(x => x.id == id, out Level steamLevel))
                LevelManager.Play(steamLevel);
        }

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
        public virtual void LoadLevelPath(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            var path = ParseVarFunction(parameters.Get(0, "path"), thisElement, customVariables);
            if (path == null || !path.IsString)
                return;

            LevelManager.Load(path.Value);
        }

        public virtual void UpdateCurrentLevelProgress(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (ProjectArrhythmia.State.InStory)
                StoryManager.inst.CurrentSave.UpdateCurrentLevelProgress();
            else
                LevelManager.UpdateCurrentLevelProgress();
        }

        // Initializes the level list menu from a specific path.
        // Supports both JSON array and JSON object.
        // 
        // - JSON Array Structure -
        // 0 = directory
        // Example:
        // [
        //   "{{AppDirectory}}beatmaps/editor" < must contain levels with ".lsb" format.
        // ]
        // 
        // - JSON Object Structure -
        // "directory"
        // Example:
        // {
        //   "directory": "" < if left empty, will use the interfaces' directory.
        // }
        public virtual void InitLevelMenu(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            var directory = InterfaceManager.inst.MainDirectory;

            if (parameters != null)
            {
                var directoryJN = parameters.Get(0, "directory");
                if (directoryJN != null)
                    directory = directoryJN;
            }

            if (string.IsNullOrEmpty(directory))
                directory = InterfaceManager.inst.MainDirectory;

            LevelListInterface.Init(Directory.GetDirectories(directory).Where(x => Level.Verify(x)).Select(x => new Level(RTFile.ReplaceSlash(x))).ToList());
        }

        public virtual void LoadLevels(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null) => LoadLevelsInterface.Init(() =>
        {
            if (parameters["on_loading_end"] != null)
                ParseFunction(parameters["on_loading_end"], thisElement, customVariables);
        });
        
        public virtual void OnInputsSelected(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null) => InputSelectInterface.OnInputsSelected = () =>
        {
            if (parameters["continue"] != null)
                ParseFunction(parameters["continue"], thisElement, customVariables);
        };

        #endregion

        #region Online

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
        public virtual void SetDiscordStatus(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            DiscordHelper.UpdateDiscordStatus(
                parameters.Get(0, "state"),
                parameters.Get(1, "details"),
                parameters.Get(2, "icon"),
                parameters.GetOrDefault(3, "art", DiscordHelper.LOGO_LEGACY));
        }

        public virtual void OpenLink(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            var linkType = Parser.TryParse(ParseVarFunction(parameters.Get(0, "link_type"), thisElement, customVariables), URLSource.Artist);
            var site = ParseVarFunction(parameters.Get(1, "site"), thisElement, customVariables);
            var link = ParseVarFunction(parameters.Get(2, "link"), thisElement, customVariables);

            var url = AlephNetwork.GetURL(linkType, site, link);
            if (string.IsNullOrEmpty(url))
                return;
            CoreHelper.Log($"Opening URL: {url}");
            Application.OpenURL(url);
        }

        // Opens the System Error Discord server link.
        // Function has no parameters.
        public virtual void ModderDiscord(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null) => Application.OpenURL(AlephNetwork.MOD_DISCORD_URL);

        // Opens the GitHub Source Code link.
        // Function has no parameters.
        public virtual void SourceCode(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null) => Application.OpenURL(AlephNetwork.OPEN_SOURCE_URL);

        #endregion

        #region Story

        // Begins the BetterLegacy story mode.
        // Function has no parameters.
        public virtual void BeginStoryMode(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            LevelManager.IsArcade = false;
            SceneHelper.LoadInputSelect(SceneHelper.LoadInterfaceScene);
        }

        public virtual void LoadCurrentStoryInterface(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null) => InterfaceManager.inst.StartupStoryInterface();

        public virtual void LoadStoryInterface(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null) => InterfaceManager.inst.StartupStoryInterface(ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables).AsInt, ParseVarFunction(parameters.Get(1, "bonus"), thisElement, customVariables).AsBool);

        public virtual void LoadStoryLevel(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            var chapter = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables);
            if (chapter == null)
                return;

            var level = ParseVarFunction(parameters.Get(1, "level"), thisElement, customVariables);
            if (level == null)
                return;

            var cutsceneIndex = ParseVarFunction(parameters.Get(2, "cutscene_index"), thisElement, customVariables).AsInt;
            var bonus = ParseVarFunction(parameters.Get(3, "bonus"), thisElement, customVariables).AsBool;
            var allowModes = ParseVarFunction(parameters.GetOrDefault(4, "allow_modes", false), thisElement, customVariables).AsBool;

            ArcadeHelper.ResetModifiedStates();
            StoryManager.inst.Play(new StorySelection
            {
                chapter = chapter.AsInt,
                level = level.AsInt,
                cutsceneDestination = CutsceneDestination.None,
                cutsceneIndex = cutsceneIndex,
                bonus = bonus,
                allowModes = allowModes,
            });
        }
        
        public virtual void SetOnLevelCompleteFunc(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            StoryManager.inst.functions.onLevelCompleteFunc = parameters.Get(0, "func");
            StoryManager.inst.functions.overrideOnCompleteFunc = parameters.Get(1, "override").AsBool;
        }
        
        public virtual void LoadStoryCutscene(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            var chapter = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables);
            if (chapter == null)
                return;

            var level = ParseVarFunction(parameters.Get(1, "level"), thisElement, customVariables);
            if (level == null)
                return;

            var isArray = parameters.IsArray;
            var cutsceneDestinationJN = ParseVarFunction(parameters.Get(2, "cutscene_destination"), thisElement, customVariables);
            var cutsceneDestination = Parser.TryParse(cutsceneDestinationJN, CutsceneDestination.Pre);
            var cutsceneIndex = ParseVarFunction(parameters.Get(3, "cutscene_index"), thisElement, customVariables).AsInt;
            var bonus = ParseVarFunction(parameters.Get(4, "bonus"), thisElement, customVariables).AsBool;

            StoryManager.inst.ContinueStory = false;

            ArcadeHelper.ResetModifiedStates();
            StoryManager.inst.Play(new StorySelection
            {
                chapter = chapter,
                level = level,
                cutsceneDestination = cutsceneDestination,
                cutsceneIndex = cutsceneIndex,
                bonus = bonus,
            });
        }
        
        public virtual void PlayAllCutscenes(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            var chapter = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables);
            if (chapter == null)
                return;

            StoryManager.inst.PlayAllCutscenes(chapter, ParseVarFunction(parameters.Get(1, "bonus"), thisElement, customVariables).AsBool);
        }
        
        public virtual void LoadStoryLevelPath(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            var path = ParseVarFunction(parameters.Get(0, "path"), thisElement, customVariables);
            if (path == null || !path.IsString)
                return;

            var songName = ParseVarFunction(parameters.Get(1, "song"), thisElement, customVariables);

            StoryManager.inst.ContinueStory = ParseVarFunction(parameters.Get(2, "continue"), thisElement, customVariables).AsBool;

            ArcadeHelper.ResetModifiedStates();
            StoryManager.inst.Play(path, songName);
        }
        
        public virtual void LoadNextStoryLevel(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            StoryManager.inst.ContinueStory = true;

            int chapter = StoryManager.inst.CurrentSave.ChapterIndex;
            StoryManager.inst.Play(new StorySelection
            {
                chapter = chapter,
                level = StoryManager.inst.CurrentSave.LoadInt($"DOC{RTString.ToStoryNumber(chapter)}Progress", 0),
                cutsceneDestination = CutsceneDestination.None,
                cutsceneIndex = 0,
                bonus = StoryManager.inst.inBonusChapter
            });
        }
        
        public virtual void LoadChapterTransition(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            StoryManager.inst.ContinueStory = true;

            var chapter = StoryManager.inst.CurrentSave.ChapterIndex;
            StoryManager.inst.Play(new StorySelection
            {
                chapter = chapter,
                level = StoryMode.Instance.chapters[chapter].Count,
                cutsceneIndex = 0,
                cutsceneDestination = CutsceneDestination.None,
                bonus = StoryManager.inst.inBonusChapter
            });
        }
        
        public virtual void SaveProfileValue(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null || !LegacyPlugin.player || LegacyPlugin.player.memory == null)
                return;

            var varName = ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables);
            if (varName == null || !varName.IsString)
                return;

            var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables);
            if (value == null)
                return;

            LegacyPlugin.player.memory[varName.Value] = value;
            LegacyPlugin.SaveProfile();
        }
        
        public virtual void StorySaveBool(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            var saveName = ParseVarFunction(parameters.Get(0, "name"), thisElement, customVariables);
            if (saveName == null || !saveName.IsString)
                return;

            var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables);

            if (ParseVarFunction(parameters.Get(2, "toggle"), thisElement, customVariables).AsBool)
                value = !StoryManager.inst.CurrentSave.LoadBool(saveName, false);

            StoryManager.inst.CurrentSave.SaveBool(saveName, value.AsBool);
        }
        
        public virtual void StorySaveInt(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            var saveName = ParseVarFunction(parameters.Get(0, "name"), thisElement, customVariables);
            if (saveName == null || !saveName.IsString)
                return;

            var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables);

            if (ParseVarFunction(parameters.Get(2, "relative"), thisElement, customVariables).AsBool)
                value += StoryManager.inst.CurrentSave.LoadInt(saveName, 0);

            StoryManager.inst.CurrentSave.SaveInt(saveName, value.AsInt);
        }
        
        public virtual void StorySaveFloat(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            var saveName = ParseVarFunction(parameters.Get(0, "name"), thisElement, customVariables);
            if (saveName == null || !saveName.IsString)
                return;

            var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables);

            if (ParseVarFunction(parameters.Get(2, "relative"), thisElement, customVariables).AsBool)
                value += StoryManager.inst.CurrentSave.LoadFloat(saveName, 0);

            StoryManager.inst.CurrentSave.SaveFloat(saveName, value.AsFloat);
        }
        
        public virtual void StorySaveString(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            var saveName = ParseVarFunction(parameters.Get(0, "name"), thisElement, customVariables);
            if (saveName == null || !saveName.IsString)
                return;

            var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables);
            if (ParseVarFunction(parameters.Get(2, "relative"), thisElement, customVariables).AsBool)
                value = StoryManager.inst.CurrentSave.LoadString(saveName, string.Empty) + value;

            StoryManager.inst.CurrentSave.SaveString(saveName, value);
        }
        
        public virtual void StorySaveJSON(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return;

            var saveName = ParseVarFunction(parameters.Get(0, "name"), thisElement, customVariables);
            if (saveName == null || !saveName.IsString)
                return;

            var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables);

            StoryManager.inst.CurrentSave.SaveNode(saveName, value);
        }

        #endregion

        #endregion

        #region Variable

        #region Main

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
        public virtual JSONNode Switch(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return null;

            var variable = ParseVarFunction(parameters.Get(0, "var"), thisElement, customVariables);
            var defaultItem = ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables);
            var caseParam = ParseVarFunction(parameters.Get(2, "case"), thisElement, customVariables);

            if (caseParam.IsArray && (!variable.IsNumber || variable < 0 || variable >= caseParam.Count))
                return defaultItem;
            if (caseParam.IsObject && (!variable.IsString || caseParam[variable.Value] == null))
                return defaultItem;

            return ParseVarFunction(variable.IsNumber ? caseParam[variable.AsInt] : caseParam[variable.Value], thisElement, customVariables);
        }

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
        public virtual JSONNode If(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return null;

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
            return null;
        }

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
        public virtual JSONNode Bool(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return null;

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
        public virtual JSONNode ReadJSONFile(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return null;

            var file = ParseVarFunction(parameters.Get(0, "file"), thisElement, customVariables);
            if (file == null)
                return null;

            var mainDirectory = ParseVarFunction(parameters.Get(1, "path"));

            if (mainDirectory == null || !mainDirectory.IsString || !mainDirectory.Value.Contains(RTFile.ApplicationDirectory))
                mainDirectory = RTFile.CombinePaths(RTFile.ApplicationDirectory, mainDirectory);

            var path = RTFile.CombinePaths(mainDirectory, file + FileFormat.LSI.Dot());

            if (!RTFile.FileExists(path))
            {
                CoreHelper.LogError($"Interface {file} does not exist!");
                return null;
            }

            return JSON.Parse(RTFile.ReadFromFile(path));
        }
        
        public virtual JSONNode NoParse(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return null;

            return parameters.Get(0, "value");
        }

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
        public virtual JSONNode FormatString(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return null;

            var str = ParseVarFunction(parameters.Get(0, "format"), thisElement, customVariables);
            var args = ParseVarFunction(parameters.Get(1, "args"), thisElement, customVariables);
            if (args == null || !args.IsArray)
                return str;

            var strArgs = new object[args.Count];
            for (int i = 0; i < strArgs.Length; i++)
                strArgs[i] = ParseVarFunction(args[i], thisElement, customVariables).Value;

            return string.Format(str, strArgs);
        }
        
        public virtual JSONNode StringRemoveAt(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            var str = ParseVarFunction(parameters.Get(0, "str"), thisElement, customVariables);
            if (str == null || !str.IsString || string.IsNullOrEmpty(str))
                return null;

            var index = ParseVarFunction(parameters.Get(1, "index"), thisElement, customVariables);
            var count = ParseVarFunction(parameters.Get(2, "count"), thisElement, customVariables);
            if (count == null)
                count = 1;
            return str.Value.Remove(index.IsString && index.Value == "end" ? str.Value.Length - 1 : index.AsInt, count);
        }
        
        public virtual JSONNode StringReplace(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            var str = ParseVarFunction(parameters.Get(0, "str"), thisElement, customVariables);
            if (str == null || !str.IsString)
                return null;

            var oldVar = ParseVarFunction(parameters.Get(1, "old"), thisElement, customVariables);
            var newVar = ParseVarFunction(parameters.Get(2, "new"), thisElement, customVariables);
            return str.Value.Replace(oldVar.Value, newVar.Value);
        }
        
        public virtual JSONNode StringInsert(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            var str = ParseVarFunction(parameters.Get(0, "str"), thisElement, customVariables);
            if (str == null || !str.IsString)
                return null;

            var index = ParseVarFunction(parameters.Get(1, "index"), thisElement, customVariables);
            var value = ParseVarFunction(parameters.Get(2, "value"), thisElement, customVariables);
            return str.Value.Insert(index.AsInt, value.Value);
        }
        
        public virtual JSONNode LoadProfileValue(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null || !LegacyPlugin.player || LegacyPlugin.player.memory == null)
                return null;

            var varName = ParseVarFunction(parameters.Get(0, "var_name"), thisElement, customVariables);
            if (varName == null || !varName.IsString)
                return null;
            return LegacyPlugin.player.memory[varName.Value];
        }
        
        public virtual JSONNode GetAsset(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return null;

            var assetPath = parameters.Get(0, "path");
            return AssetPack.GetFile(assetPath);
        }
        
        public virtual JSONNode RandomRange(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return 0f;

            return UnityRandom.Range(parameters.Get(0, "min").AsFloat, parameters.Get(1, "max").AsFloat);
        }
        
        public virtual JSONNode RandomRangeInt(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return 0f;

            return UnityRandom.Range(parameters.Get(0, "min").AsInt, parameters.Get(1, "max").AsInt);
        }
        
        public virtual JSONNode GetConfigSetting(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return null;

            var configName = ParseVarFunction(parameters.Get(0, "config"), thisElement, customVariables);
            if (!LegacyPlugin.configs.TryFind(x => x.Name == configName, out BaseConfig config))
                return null;

            var section = ParseVarFunction(parameters.Get(1, "section"), thisElement, customVariables);
            var key = ParseVarFunction(parameters.Get(2, "key"), thisElement, customVariables);
            if (config.Settings.TryFind(x => x.Section == section && x.Key == key, out BaseSetting setting))
                return setting.BoxedValue?.ToString();
            return null;
        }

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
        public virtual JSONNode ParseMath(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return null;

            Dictionary<string, float> vars = null;

            var jnVars = ParseVarFunction(parameters.Get(1, "vars"), thisElement, customVariables);
            if (jnVars != null)
            {
                vars = new Dictionary<string, float>();
                for (int i = 0; i < jnVars.Count; i++)
                {
                    var item = jnVars[i];
                    var name = item["n"];
                    if (string.IsNullOrEmpty(name))
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
        
        public virtual JSONNode CombinePaths(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return null;

            if (parameters.IsArray && parameters.Count > 2)
            {
                var paths = new string[parameters.Count];
                for (int i = 0; i < paths.Length; i++)
                    paths[i] = parameters[i];
                return RTFile.CombinePaths(paths);
            }

            return RTFile.CombinePaths(parameters.Get(0, "path1"), parameters.Get(1, "path2"));
        }

        #endregion

        #region Story

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
        public virtual JSONNode StoryLoadBoolVar(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return null;
            return StoryManager.inst.CurrentSave.LoadBool(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables)).ToString();
        }

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
        public virtual JSONNode StoryLoadIntVar(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return null;
            return StoryManager.inst.CurrentSave.LoadInt(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables)).ToString();
        }

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
        public virtual JSONNode StoryLoadFloatVar(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return null;
            return StoryManager.inst.CurrentSave.LoadFloat(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables)).ToString();
        }

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
        public virtual JSONNode StoryLoadStringVar(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return null;
            return StoryManager.inst.CurrentSave.LoadString(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables), ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables)).ToString();
        }

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
        public virtual JSONNode StoryLoadJSONVar(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return null;
            return StoryManager.inst.CurrentSave.LoadJSON(ParseVarFunction(parameters.Get(0, "load"), thisElement, customVariables));
        }
        
        public virtual JSONNode ToStoryNumber(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return null;

            var number = ParseVarFunction(parameters.Get(0, "num"), thisElement, customVariables);
            if (number == null || !number.IsNumber)
                return null;

            return RTString.ToStoryNumber(number.AsInt);
        }
        
        public virtual JSONNode StoryLevelID(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return null;

            var chapterIndex = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables);
            var levelIndex = ParseVarFunction(parameters.Get(1, "level"), thisElement, customVariables);
            var bonus = ParseVarFunction(parameters.Get(2, "bonus"), thisElement, customVariables);
            var chapters = bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters;

            return chapters.TryGetAt(chapterIndex.AsInt, out StoryMode.Chapter chapter) && chapter.levels.TryGetAt(levelIndex.AsInt, out StoryMode.LevelSequence level) ? level.id : ParseVarFunction(parameters.Get(3, "default"), thisElement, customVariables);
        }
        
        public virtual JSONNode StoryLevelName(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return null;

            var chapterIndex = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables);
            var levelIndex = ParseVarFunction(parameters.Get(1, "level"), thisElement, customVariables);
            var bonus = ParseVarFunction(parameters.Get(2, "bonus"), thisElement, customVariables);
            var chapters = bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters;

            return chapters.TryGetAt(chapterIndex.AsInt, out StoryMode.Chapter chapter) && chapter.levels.TryGetAt(levelIndex.AsInt, out StoryMode.LevelSequence level) ? level.name : ParseVarFunction(parameters.Get(3, "default"), thisElement, customVariables);
        }
        
        public virtual JSONNode StoryLevelSongTitle(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return null;

            var chapterIndex = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables);
            var levelIndex = ParseVarFunction(parameters.Get(1, "level"), thisElement, customVariables);
            var bonus = ParseVarFunction(parameters.Get(2, "bonus"), thisElement, customVariables);
            var chapters = bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters;

            return chapters.TryGetAt(chapterIndex.AsInt, out StoryMode.Chapter chapter) && chapter.levels.TryGetAt(levelIndex.AsInt, out StoryMode.LevelSequence level) ? level.songTitle : ParseVarFunction(parameters.Get(3, "default"), thisElement, customVariables);
        }
        
        public virtual JSONNode StoryLevelCount(JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (parameters == null)
                return null;

            var chapterIndex = ParseVarFunction(parameters.Get(0, "chapter"), thisElement, customVariables);
            var bonus = ParseVarFunction(parameters.Get(1, "bonus"), thisElement, customVariables);
            var chapters = bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters;

            return chapters.TryGetAt(chapterIndex.AsInt, out StoryMode.Chapter chapter) ? chapter.Count : ParseVarFunction(parameters.Get(2, "default"), thisElement, customVariables);
        }
        
        #endregion

        #endregion

        public virtual bool IfFunction(JSONNode jn, string name, JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            if (predicates.TryGetValue(name, out var predicate))
                return predicate.Invoke(parameters, thisElement, customVariables);

            if (customJSONFunctions.TryGetValue(name, out JSONNode customJSONFunction))
                return ParseIfFunction(customJSONFunction, thisElement, customVariables);
            return false;
        }

        public virtual void Function(JSONNode jn, string name, JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (string.IsNullOrEmpty(name))
                return;

            if (actions.TryGetValue(name, out var action))
            {
                action.Invoke(parameters, thisElement, customVariables);
                return;
            }

            if (customJSONFunctions.TryGetValue(name, out JSONNode customJSONFunction))
                ParseFunction(customJSONFunction, thisElement, customVariables);
        }

        public virtual JSONNode VarFunction(JSONNode jn, string name, JSONNode parameters, T thisElement = default, Dictionary<string, JSONNode> customVariables = null)
        {
            if (string.IsNullOrEmpty(name))
                return jn;

            if (variables.TryGetValue(name, out var variable))
                return variable.Invoke(parameters, thisElement, customVariables) ?? jn;
            if (customJSONFunctions.TryGetValue(name, out JSONNode customJSONFunction))
                return ParseVarFunction(customJSONFunction, thisElement, customVariables);
            return jn;
        }

        #endregion

        #endregion
    }
}
