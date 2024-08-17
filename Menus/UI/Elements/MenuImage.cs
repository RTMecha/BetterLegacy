using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Components;

using TMPro;
using SimpleJSON;
using BetterLegacy.Core;
using BetterLegacy.Configs;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using LSFunctions;
using System.IO;
using BetterLegacy.Menus.UI.Interfaces;

namespace BetterLegacy.Menus.UI.Elements
{
    /// <summary>
    /// Base class used for handling image elements and other types in the interface. To be used either as a base for other elements or an image element on its own.
    /// </summary>
    public class MenuImage
    {
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
        /// Spawn length of the element, to be used for spacing each element out. Time is not always exactly a second.
        /// </summary>
        public float length = 1f;

        /// <summary>
        /// RectTransform values in a JSON format.
        /// </summary>
        public JSONNode rectJSON;

        /// <summary>
        /// Function JSON to parse whenever the element is clicked.
        /// </summary>
        public JSONNode funcJSON;

        /// <summary>
        /// Function JSON called whenever the element is clicked.
        /// </summary>
        public Action func;

        /// <summary>
        /// Function JSON to parse when the element spawns.
        /// </summary>
        public JSONNode spawnFuncJSON;

        /// <summary>
        /// Function called when the element spawns.
        /// </summary>
        public Action spawnFunc;

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
        /// Opacity of the image.
        /// </summary>
        public float opacity;

        /// <summary>
        /// Theme color slot for the image to use.
        /// </summary>
        public int color;

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
        public SpriteManager.RoundedSide roundedSide = SpriteManager.RoundedSide.W;

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

        #region Methods

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
            time = Time.time - timeOffset * (InputDataManager.inst.menuActions.Submit.IsPressed ? 0.3f : 1f);
            if (time > length)
                isSpawning = false;
        }

        public void ParseFunction(JSONNode jn)
        {
            if (jn.IsArray)
            {
                for (int i = 0; i < jn.Count; i++)
                    ParseFunctionSingle(jn[i]);

                return;
            }

            ParseFunctionSingle(jn);
        }

        public virtual void Clear()
        {
            for (int i = 0; i < animations.Count; i++)
                AnimationManager.inst.RemoveID(animations[i].id);
            animations.Clear();
        }

        public bool ParseIfFunction(JSONNode jn)
        {
            if (jn == null)
                return true;

            if (jn.IsObject)
                return ParseIfFunctionSingle(jn);

            bool canProceed = true;

            if (jn.IsArray)
            {
                for (int i = 0; i < jn.Count; i++)
                {
                    if (!ParseIfFunctionSingle(jn[i]))
                        canProceed = false;
                }
            }

            return canProceed;
        }

        public bool ParseIfFunctionSingle(JSONNode jn)
        {
            var parameters = jn["params"];
            string name = jn["name"];
            var not = jn["not"].AsBool; // If true,then check if the function is not true.

            switch (name)
            {
                case "True": return true;
                case "False": return false;
                case "GetSettingBool":
                    {
                        if (parameters == null)
                            break;

                        var value = DataManager.inst.GetSettingBool(parameters.IsArray ? parameters[0] : parameters["setting"], parameters.IsArray ? parameters[1].AsBool : parameters["default"].AsBool);
                        return !not ? value : !value;
                    }
                case "GetSettingIntEquals":
                    {
                        if (parameters == null)
                            break;

                        var value = DataManager.inst.GetSettingInt(parameters.IsArray ? parameters[0] : parameters["setting"], parameters.IsArray ? parameters[1].AsInt : parameters["default"].AsInt) == (parameters.IsArray ? parameters[2].AsInt : parameters["value"].AsInt);
                        return !not ? value : !value;
                    }
                case "GetSettingIntLesserEquals":
                    {
                        if (parameters == null)
                            break;

                        var value = DataManager.inst.GetSettingInt(parameters.IsArray ? parameters[0] : parameters["setting"], parameters.IsArray ? parameters[1].AsInt : parameters["default"].AsInt) <= (parameters.IsArray ? parameters[2].AsInt : parameters["value"].AsInt);
                        return !not ? value : !value;
                    }
                case "GetSettingIntGreaterEquals":
                    {
                        if (parameters == null)
                            break;

                        var value = DataManager.inst.GetSettingInt(parameters.IsArray ? parameters[0] : parameters["setting"], parameters.IsArray ? parameters[1].AsInt : parameters["default"].AsInt) >= (parameters.IsArray ? parameters[2].AsInt : parameters["value"].AsInt);
                        return !not ? value : !value;
                    }
                case "GetSettingIntLesser":
                    {
                        if (parameters == null)
                            break;

                        var value = DataManager.inst.GetSettingInt(parameters.IsArray ? parameters[0] : parameters["setting"], parameters.IsArray ? parameters[1].AsInt : parameters["default"].AsInt) < (parameters.IsArray ? parameters[2].AsInt : parameters["value"].AsInt);
                        return !not ? value : !value;
                    }
                case "GetSettingIntGreater":
                    {
                        if (parameters == null)
                            break;

                        var value = DataManager.inst.GetSettingInt(parameters.IsArray ? parameters[0] : parameters["setting"], parameters.IsArray ? parameters[1].AsInt : parameters["default"].AsInt) > (parameters.IsArray ? parameters[2].AsInt : parameters["value"].AsInt);
                        return !not ? value : !value;
                    }
            }

            return false;
        }

        /// <summary>
        /// Parses the "func" JSON and performs an action based on the name and parameters.
        /// </summary>
        /// <param name="jn">The func JSON. Must have a name and a params array. If it has a "if_func"</param>
        public void ParseFunctionSingle(JSONNode jn, bool allowIfFunc = true)
        {
            if (jn["if_func"] != null && allowIfFunc)
            {
                if (!ParseIfFunction(jn["if_func"]))
                    return;
            }

            var parameters = jn["params"];
            string name = jn["name"];

            switch (name)
            {
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
                case "LoadScene":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["scene"] == null)
                            break;

                        var isArcade = parameters.IsArray && parameters.Count >= 3 ? parameters[2].AsBool : parameters.IsObject ? parameters["is_arcade"].AsBool : false;

                        LevelManager.IsArcade = isArcade;
                        DataManager.inst.UpdateSettingBool("IsArcade", isArcade);

                        if (parameters.IsArray && parameters.Count >= 2 || parameters.IsObject && parameters["show_loading"] != null)
                            SceneManager.inst.LoadScene(parameters.IsArray ? parameters[0] : parameters["scene"], Parser.TryParse(parameters.IsArray ? parameters[1] : parameters["show_loading"], true));
                        else
                            SceneManager.inst.LoadScene(parameters.IsArray ? parameters[0] : parameters["scene"]);

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
                case "UpdateSettingBool":
                    {
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
                case "UpdateSettingInt":
                    {
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
                case "Log":
                    {
                        if (parameters != null && (parameters.IsArray && parameters.Count >= 1 || parameters.IsObject && parameters["msg"]))
                            CoreHelper.Log(parameters.IsArray ? parameters[0] : parameters["msg"]);

                        break;
                    }

                #endregion

                #region ExitGame

                // Exits the game.
                // Function has no parameters.
                case "ExitGame":
                    {
                        Application.Quit();
                        break;
                    }

                #endregion

                #region Config

                // Opens the Config Manager UI.
                // Function has no parameters.
                case "Config":
                    {
                        ConfigManager.inst.Show();
                        break;
                    }

                #endregion

                #region SetCurrentInterface

                // Finds an interface with a matching ID and opens it.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = id
                // Example:
                // [
                //   "0" < main menus' ID is 0, so load that one. No other interface should have this ID.
                // ]
                // 
                // - JSON Object Structure -
                // "id"
                // Example:
                // {
                //   "id": "0"
                // }
                case "SetCurrentInterface":
                    {
                        if (parameters != null && (parameters.IsArray && parameters.Count >= 1 || parameters.IsObject && parameters["id"] != null))
                            NewMenuManager.inst.SetCurrentInterface(parameters.IsArray ? parameters[0] : parameters["id"]);

                        break;
                    }

                #endregion

                #region Reload

                // Reloads the interface and sets it to the main menu. Only recommended if you want to return to the main menu and unload every other interface.
                // Function has no parameters.
                case "Reload":
                    {
                        NewMenuManager.inst.StartupInterface();

                        break;
                    }

                #endregion

                #region Parse

                // Loads an interface and opens it, clearing the current interface.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = file name without extension (files' extension must be lsi).
                // 1 = if interface should be opened.
                // 2 = set main directory.
                // Example:
                // [
                //   "story_mode",
                //   "True",
                //   "{{BepInExAssetsDirectory}}Interfaces"
                // ]
                //
                // - JSON Object Structure -
                // "file"
                // "load"
                // "path"
                // Example:
                // {
                //   "file": "some_interface",
                //   "load": "False",
                //   "path": "beatmaps/interfaces" < doesn't need to exist
                // }
                case "Parse":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["file"] == null)
                            return;

                        if (parameters.IsArray && parameters.Count > 2 || parameters.IsObject && parameters["path"] != null)
                            NewMenuManager.inst.MainDirectory = RTFile.ApplicationDirectory + FontManager.TextTranslater.ReplaceProperties(parameters.IsArray ? parameters[2] : parameters["path"]);

                        var path = RTFile.CombinePath(NewMenuManager.inst.MainDirectory, $"{(parameters.IsArray ? parameters[0].Value : parameters["file"].Value)}.lsi");

                        if (!RTFile.FileExists(path))
                        {
                            CoreHelper.LogError($"Interface {(parameters.IsArray ? parameters[0] : parameters["file"])} does not exist!");

                            return;
                        }

                        var interfaceJN = JSON.Parse(RTFile.ReadFromFile(path));

                        var menu = CustomMenu.Parse(interfaceJN);
                        menu.filePath = path;

                        if (NewMenuManager.inst.interfaces.Has(x => x.id == menu.id))
                        {
                            if (parameters.IsArray && (parameters.Count < 2 || Parser.TryParse(parameters[1], false)) || parameters.IsObject && Parser.TryParse(parameters["load"], true))
                                NewMenuManager.inst.SetCurrentInterface(menu.id);

                            return;
                        }    

                        NewMenuManager.inst.interfaces.Add(menu);

                        if (parameters.IsArray && (parameters.Count < 2 || Parser.TryParse(parameters[1], false)) || parameters.IsObject && Parser.TryParse(parameters["load"], true))
                            NewMenuManager.inst.SetCurrentInterface(menu.id);

                        break;
                    }

                #endregion

                #region DemoStoryMode

                // Demos the BetterLegacy story mode. Temporary until the story mode is implemented.
                // Function has no parameters.
                case "DemoStoryMode":
                    {
                        DataManager.inst.UpdateSettingBool("IsArcade", false);
                        LevelManager.IsArcade = false;
                        SceneManager.inst.LoadScene("Input Select");
                        LevelManager.OnInputsSelected = () => { CoreHelper.StartCoroutine(Story.StoryManager.inst.Demo(true)); };

                        break;
                    }

                #endregion

                #region SetCurrentPath

                // Sets the main directory for the menus to use in some cases.
                // Supports both JSON array and JSON object.
                //
                // - JSON Array Structure -
                // 0 = path
                // Example:
                // [
                //   "beatmaps/interfaces/"
                // ]
                //
                // - JSON Object Structure -
                // "path"
                // Example:
                // {
                //   "path": "{{BepInExAssetsDirectory}}Interfaces" < doesn't always need to end in a slash. A {{AppDirectory}} variable exists, but not recommended to use here since it's automatically applied to the start of the path.
                // }
                case "SetCurrentPath":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["path"] == null)
                            return;

                        NewMenuManager.inst.MainDirectory = RTFile.ApplicationDirectory + FontManager.TextTranslater.ReplaceProperties(parameters.IsArray ? parameters[0] : parameters["path"]);

                        break;
                    }

                #endregion

                #region PlaySound

                // Plays a sound. Can either be a default one already loaded in the SoundLibrary or a custom one from the menu's folder.
                // Supports both JSON array and JSON object.
                //
                // - JSON Array Structure -
                // 0 = sound
                // Example:
                // [
                //   "blip" < plays the blip sound.
                // ]
                //
                // - JSON Object Structure -
                // "sound"
                // Example:
                // {
                //   "sound": "some kind of sound.ogg" < since this sound does not exist in the SoundLibrary, search for a file with the name. If it exists, play the sound.
                // }
                case "PlaySound":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["sound"] == null || NewMenuManager.inst.CurrentMenu == null)
                            return;

                        string sound = parameters.IsArray ? parameters[0] : parameters["sound"];

                        if (AudioManager.inst.library.soundClips.ContainsKey(sound))
                        {
                            AudioManager.inst.PlaySound(sound);
                            return;
                        }

                        var filePath = $"{Path.GetDirectoryName(NewMenuManager.inst.CurrentMenu.filePath)}{sound}";
                        var audioType = RTFile.GetAudioType(filePath);
                        if (audioType == AudioType.MPEG)
                            AudioManager.inst.PlaySound(LSAudio.CreateAudioClipUsingMP3File(filePath));
                        else
                            CoreHelper.StartCoroutine(AlephNetworkManager.DownloadAudioClip($"file://{filePath}", audioType, AudioManager.inst.PlaySound));

                        break;
                    }

                #endregion

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
                case "SetDiscordStatus":
                    {
                        if (parameters == null)
                            return;

                        if (parameters.IsObject)
                        {
                            CoreHelper.UpdateDiscordStatus(parameters["state"], parameters["details"], parameters["icon"], parameters["art"] != null ? parameters["art"] : "pa_logo_white");

                            return;
                        }

                        if (parameters.IsArray && parameters.Count < 1)
                            return;

                        CoreHelper.UpdateDiscordStatus(parameters[0], parameters[1], parameters[2], parameters.Count > 3 ? parameters[3] : "pa_logo_white");

                        break;
                    }

                #endregion

                #region AnimateID

                // Finds an element with a matching ID and animates it.
                // Supports only JSON array.
                //
                // - JSON Array Structure -
                // 0 = ID
                // 1 = type (integer, 0 = position, 1 = scale, 2 = rotation)
                // 2 = looping (boolean true or false)
                // 3 = keyframes
                // 4 = anim done func
                // Example:
                // [
                //   "0", < ID
                //   "0", < type
                //   "True", < looping
                //   {
                //     "x": [
                //       {
                //         "t": "0", < usually a good idea to have the first keyframes' start time set to 0.
                //         "val": "0",
                //         "rel": "True", < if true and the keyframe is the first keyframe, offset from current transform value.
                //         "ct": "Linear" < Easing / Curve Type.
                //       },
                //       {
                //         "t": "1",
                //         "val": "10", < moves X somewhere.
                //         "rel": "True" < if true, adds to previous keyframe value.
                //         // ct doesn't always need to exist. If it doesn't, then it'll automatically be Linear easing.
                //       }
                //     ],
                //     "y": [
                //       {
                //         "t": "0",
                //         "val": "0",
                //         // relative is false by default, so no need to do "rel": "False". With it set to false, the objects' Y position will be snapped to 0 instead of offsetting from its original position.
                //       }
                //     ],
                //     "z": [
                //       {
                //         "t": "0",
                //         "val": "0",
                //       }
                //     ]
                //   }, < keyframes
                //   {
                //     "name": "Log",
                //     "params": [
                //       "Animation done!"
                //     ]
                //   } < function to run when animation is complete.
                // ]
                case "AnimateID":
                    {
                        if (parameters == null || parameters.Count < 1)
                            return;

                        var id = parameters[0]; // ID of an object to animate
                        var type = Parser.TryParse(parameters[1], 0); // which type to animate (e.g. 0 = position, 1 = scale, 2 = rotation)

                        if (NewMenuManager.inst.CurrentMenu.elements.TryFind(x => x.id == id, out MenuImage element))
                        {
                            var animation = new RTAnimation("Interface Element Animation"); // no way element animation reference :scream:

                            animation.loop = parameters[2].AsBool;

                            JSONNode lastX = null;
                            float x = 0f;
                            if (parameters[3]["x"] != null)
                            {
                                List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                for (int i = 0; i < parameters[3]["x"].Count; i++)
                                {
                                    var kf = parameters[3]["x"][i];
                                    var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? element.GetTransform(type, 0) : 0f);
                                    x = kf["rel"].AsBool ? x + val : val;
                                    keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, x, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                    lastX = kf["val"];
                                }
                                animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, x => { element.SetTransform(type, 0, x); }));
                            }

                            JSONNode lastY = null;
                            float y = 0f;
                            if (parameters[3]["y"] != null)
                            {
                                List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                for (int i = 0; i < parameters[3]["y"].Count; i++)
                                {
                                    var kf = parameters[3]["y"][i];
                                    var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? element.GetTransform(type, 1) : 0f);
                                    y = kf["rel"].AsBool ? y + val : val;
                                    keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, y, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                    lastY = kf["val"];
                                }
                                animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, x => { element.SetTransform(type, 1, x); }));
                            }

                            JSONNode lastZ = null;
                            float z = 0f;
                            if (parameters[3]["z"] != null)
                            {
                                List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                for (int i = 0; i < parameters[3]["z"].Count; i++)
                                {
                                    var kf = parameters[3]["z"][i];
                                    var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? element.GetTransform(type, 2) : 0f);
                                    z = kf["rel"].AsBool ? z + val : val;
                                    keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, z, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                    lastZ = kf["val"];
                                }
                                animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, x => { element.SetTransform(type, 2, x); }));
                            }

                            animation.onComplete = () =>
                            {
                                if (animation.loop)
                                {
                                    if (parameters.Count > 4 && parameters[4] != null)
                                        ParseFunction(parameters[4]);
                                    return;
                                }

                                AnimationManager.inst.RemoveID(animation.id);
                                animations.RemoveAll(x => x.id == animation.id);

                                if (lastX != null)
                                    element.SetTransform(type, 0, x);
                                if (lastY != null)
                                    element.SetTransform(type, 1, y);
                                if (lastZ != null)
                                    element.SetTransform(type, 2, z);

                                if (parameters.Count <= 4 || parameters[4] == null)
                                    return;

                                ParseFunction(parameters[4]);
                            };

                            animations.Add(animation);
                            AnimationManager.inst.Play(animation);
                        }

                        break;
                    }

                #endregion

                #region AnimateName

                // Same as animate ID, except instead of searching for an elements' ID, you search for a name.
                // No example needed.
                case "AnimateName": // in case you'd rather find an objects' name instead of ID.
                    {
                        if (parameters == null || parameters.Count < 1)
                            return;

                        var elementName = parameters[0]; // Name of an object to animate
                        var type = Parser.TryParse(parameters[1], 0); // which type to animate (e.g. 0 = position, 1 = scale, 2 = rotation)

                        if (NewMenuManager.inst.CurrentMenu.elements.TryFind(x => x.name == elementName, out MenuImage element))
                        {
                            var animation = new RTAnimation("Interface Element Animation"); // no way element animation reference :scream:

                            animation.loop = parameters[2].AsBool;

                            JSONNode lastX = null;
                            float x = 0f;
                            if (parameters[3]["x"] != null)
                            {
                                List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                for (int i = 0; i < parameters[3]["x"].Count; i++)
                                {
                                    var kf = parameters[3]["x"][i];
                                    var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? element.GetTransform(type, 0) : 0f);
                                    x = kf["rel"].AsBool ? x + val : val;
                                    keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, x, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                    lastX = kf["val"];
                                }
                                animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, x => { element.SetTransform(type, 0, x); }));
                            }

                            JSONNode lastY = null;
                            float y = 0f;
                            if (parameters[3]["y"] != null)
                            {
                                List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                for (int i = 0; i < parameters[3]["y"].Count; i++)
                                {
                                    var kf = parameters[3]["y"][i];
                                    var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? element.GetTransform(type, 1) : 0f);
                                    y = kf["rel"].AsBool ? y + val : val;
                                    keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, y, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                    lastY = kf["val"];
                                }
                                animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, x => { element.SetTransform(type, 1, x); }));
                            }

                            JSONNode lastZ = null;
                            float z = 0f;
                            if (parameters[3]["z"] != null)
                            {
                                List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                for (int i = 0; i < parameters[3]["z"].Count; i++)
                                {
                                    var kf = parameters[3]["z"][i];
                                    var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? element.GetTransform(type, 2) : 0f);
                                    z = kf["rel"].AsBool ? z + val : val;
                                    keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, z, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                    lastZ = kf["val"];
                                }
                                animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, x => { element.SetTransform(type, 2, x); }));
                            }

                            animation.onComplete = () =>
                            {
                                if (animation.loop)
                                {
                                    if (parameters.Count > 4 && parameters[4] != null)
                                        ParseFunction(parameters[4]);
                                    return;
                                }

                                AnimationManager.inst.RemoveID(animation.id);
                                animations.RemoveAll(x => x.id == animation.id);

                                if (lastX != null)
                                    element.SetTransform(type, 0, x);
                                if (lastY != null)
                                    element.SetTransform(type, 1, y);
                                if (lastZ != null)
                                    element.SetTransform(type, 2, z);

                                if (parameters.Count <= 4 || parameters[4] == null)
                                    return;

                                ParseFunction(parameters[4]);
                            };

                            animations.Add(animation);
                            AnimationManager.inst.Play(animation);
                        }

                        break;
                    }

                #endregion
            }
        }

        public void SetTransform(int type, int axis, float value)
        {
            if (!gameObject)
                return;

            switch (type)
            {
                case 0: ((Action<float>)(axis == 0 ? gameObject.transform.SetLocalPositionX : axis == 1 ? gameObject.transform.SetLocalPositionY : gameObject.transform.SetLocalPositionZ)).Invoke(value);
                    break;
                case 1: ((Action<float>)(axis == 0 ? gameObject.transform.SetLocalScaleX : axis == 1 ? gameObject.transform.SetLocalScaleY : gameObject.transform.SetLocalScaleZ)).Invoke(value);
                    break;
                case 2: ((Action<float>)(axis == 0 ? gameObject.transform.SetLocalRotationEulerX : axis == 1 ? gameObject.transform.SetLocalRotationEulerY : gameObject.transform.SetLocalRotationEulerZ)).Invoke(value);
                    break;
            }
        }

        public float GetTransform(int type, int axis)
        {
            if (!gameObject)
                return 0f;

            switch (type)
            {
                case 0: return axis == 0 ? gameObject.transform.localPosition.x : axis == 1 ? gameObject.transform.localPosition.y : gameObject.transform.localPosition.z;
                case 1: return axis == 0 ? gameObject.transform.localScale.x : axis == 1 ? gameObject.transform.localScale.y : gameObject.transform.localScale.z;
                case 2: return axis == 0 ? gameObject.transform.localEulerAngles.x : axis == 1 ? gameObject.transform.localEulerAngles.y : gameObject.transform.localEulerAngles.z;
            }

            return 0f;
        }

        /// <summary>
        /// Provides a way to see the object in UnityExplorer.
        /// </summary>
        /// <returns>A string containing the objects' ID and name.</returns>
        public override string ToString() => $"{id} - {name}";

        /// <summary>
        /// Generates a JSON based on a direct RectTransforms' values.
        /// </summary>
        /// <param name="rectTransform">RectTransform to convert to JSON.</param>
        /// <returns></returns>
        public static JSONNode GenerateRectTransformJSON(RectTransform rectTransform) => GenerateRectTransformJSON(rectTransform.anchoredPosition, rectTransform.anchorMax, rectTransform.anchorMin, rectTransform.pivot, rectTransform.sizeDelta);

        /// <summary>
        /// Generates JSON based on a RectTransforms' values.
        /// </summary>
        /// <param name="anc_pos">From anchoredPosition.</param>
        /// <param name="anc_max">From anchorMax.</param>
        /// <param name="anc_min">From anchorMin.</param>
        /// <param name="pivot">From pivot.</param>
        /// <param name="size">From size.</param>
        /// <returns></returns>
        public static JSONNode GenerateRectTransformJSON(Vector2 anc_pos, Vector2 anc_max, Vector2 anc_min, Vector2 pivot, Vector2 size)
        {
            var jn = JSON.Parse("{}");
            jn["anc_pos"] = anc_pos.ToJSON();
            jn["anc_max"] = anc_max.ToJSON();
            jn["anc_min"] = anc_min.ToJSON();
            jn["pivot"] = pivot.ToJSON();
            jn["size"] = size.ToJSON();
            return jn;
        }

        #endregion

        #region Private Fields

        List<RTAnimation> animations = new List<RTAnimation>();

        #endregion
    }
}
