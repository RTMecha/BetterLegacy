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
using BetterLegacy.Core.Data;
using BetterLegacy.Story;

namespace BetterLegacy.Menus.UI.Elements
{
    /// <summary>
    /// Base class used for handling image elements and other types in the interface. To be used either as a base for other elements or an image element on its own.
    /// </summary>
    public class MenuImage
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

        #endregion

        #region Private Fields

        List<RTAnimation> animations = new List<RTAnimation>();

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

            length = orig.length,
            wait = orig.wait,

            #endregion

            #region Func

            playBlipSound = orig.playBlipSound,
            funcJSON = orig.funcJSON, // function to run when the element is clicked.
            spawnFuncJSON = orig.spawnFuncJSON, // function to run when the element spawns.
            func = orig.func,
            spawnFunc = orig.spawnFunc,

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
            time = (Time.time - timeOffset) / (InputDataManager.inst.menuActions.Submit.IsPressed ? length * MenuConfig.Instance.SpeedUpSpeedMultiplier.Value : length * MenuConfig.Instance.RegularSpeedMultiplier.Value);

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

        public string ParseText(string input)
        {
            CoreHelper.RegexMatches(input, new Regex(@"{{LevelRank=([0-9]+)}}"), match =>
            {
                DataManager.LevelRank levelRank =
                    LevelManager.Levels.TryFind(x => x.id == match.Groups[1].ToString(), out Level level) ? LevelManager.GetLevelRank(level) :
                    CoreHelper.InEditor ?
                        LevelManager.EditorRank :
                        DataManager.inst.levelRanks[0];

                input = input.Replace(match.Groups[0].ToString(), CoreHelper.FormatLevelRank(levelRank));
            });

            CoreHelper.RegexMatches(input, new Regex(@"{{StoryLevelRank=([0-9]+)}}"), match =>
            {
                DataManager.LevelRank levelRank =
                    StoryManager.inst.Saves.TryFind(x => x.ID == match.Groups[1].ToString(), out LevelManager.PlayerData playerData) ? LevelManager.GetLevelRank(playerData) :
                    CoreHelper.InEditor ?
                        LevelManager.EditorRank :
                        DataManager.inst.levelRanks[0];

                input = input.Replace(match.Groups[0].ToString(), CoreHelper.FormatLevelRank(levelRank));
            });

            CoreHelper.RegexMatches(input, new Regex(@"{{LoadStoryString=(.*?),(.*?)}}"), match =>
            {
                input = input.Replace(match.Groups[0].ToString(), StoryManager.inst.LoadString(match.Groups[1].ToString(), match.Groups[2].ToString()));
            });

            input = input
                .Replace("{{CurrentPlayingChapterNumber}}", (StoryManager.inst.currentPlayingChapterIndex + 1).ToString("00"))
                .Replace("{{CurrentPlayingLevelNumber}}", (StoryManager.inst.currentPlayingLevelSequenceIndex + 1).ToString("00"));

            return input;
        }

        #region Functions

        /// <summary>
        /// Parses an entire func JSON. Supports both JSON Object and JSON Array.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        public void ParseFunction(JSONNode jn)
        {
            if (jn.IsArray)
            {
                for (int i = 0; i < jn.Count; i++)
                    ParseFunctionSingle(jn[i]);

                return;
            } // Allow multiple functions to occur.

            ParseFunctionSingle(jn);
        }

        /// <summary>
        /// Parses an "if_func" JSON and returns the result. Supports both JSON Object and JSON Array.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns true if the passed JSON functions is true, otherwise false.</returns>
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
                    var value = ParseIfFunctionSingle(jn[i]);
                    if (!jn[i]["otherwise"].AsBool && !value)
                        canProceed = false;

                    if (jn[i]["otherwise"].AsBool && value)
                        canProceed = true;

                    //if (jn[i]["and"].AsBool && !value)
                    //    canProceed = false;
                }
            }

            return canProceed;
        }

        /// <summary>
        /// Parses a singular "if_func" JSON.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns true if the passed JSON function is true, otherwise false.</returns>
        public bool ParseIfFunctionSingle(JSONNode jn)
        {
            var parameters = jn["params"];
            string name = jn["name"];
            var not = jn["not"].AsBool; // If true, then check if the function is not true.

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
                case "IsScene":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["scene"] == null)
                            break;

                        var value = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == (parameters.IsArray ? parameters[0] : parameters["scene"]);
                        return !not ? value : !value;
                    }
                case "StoryChapterEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["chapter"] == null)
                            break;

                        var value = StoryManager.inst.LoadInt("Chapter", 0) == (parameters.IsArray ? parameters[0].AsInt : parameters["chapter"].AsInt);
                        return !not ? value : !value;
                    }
                case "StoryChapterLesserEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["chapter"] == null)
                            break;

                        var value = StoryManager.inst.LoadInt("Chapter", 0) <= (parameters.IsArray ? parameters[0].AsInt : parameters["chapter"].AsInt);
                        return !not ? value : !value;
                    }
                case "StoryChapterGreaterEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["chapter"] == null)
                            break;

                        var value = StoryManager.inst.LoadInt("Chapter", 0) >= (parameters.IsArray ? parameters[0].AsInt : parameters["chapter"].AsInt);
                        return !not ? value : !value;
                    }
                case "StoryChapterLesser":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["chapter"] == null)
                            break;

                        var value = StoryManager.inst.LoadInt("Chapter", 0) < (parameters.IsArray ? parameters[0].AsInt : parameters["chapter"].AsInt);
                        return !not ? value : !value;
                    }
                case "StoryChapterGreater":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["chapter"] == null)
                            break;

                        var value = StoryManager.inst.LoadInt("Chapter", 0) > (parameters.IsArray ? parameters[0].AsInt : parameters["chapter"].AsInt);
                        return !not ? value : !value;
                    }
                case "DisplayNameEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["user"] == null)
                            break;

                        var value = CoreConfig.Instance.DisplayName.Value == (parameters.IsArray ? parameters[0].Value : parameters["user"].Value);
                        return !not ? value : !value;
                    }
                case "StoryInstalled":
                    {
                        var value = StoryManager.inst && RTFile.DirectoryExists(StoryManager.StoryAssetsPath);
                        return !not ? value : !value;
                    }
                case "StoryLoadIntEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 3 || parameters.IsObject && parameters["load"] == null)
                            break;

                        var value = StoryManager.inst.LoadInt(parameters.IsArray ? parameters[0] : parameters["load"], Parser.TryParse(parameters.IsArray ? parameters[1] : parameters["default"], 0)) == Parser.TryParse(parameters.IsArray ? parameters[2] : parameters["value"], 0);
                        return !not ? value : !value;
                    }
                case "StoryLoadIntLesserEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 3 || parameters.IsObject && parameters["load"] == null)
                            break;

                        var value = StoryManager.inst.LoadInt(parameters.IsArray ? parameters[0] : parameters["load"], Parser.TryParse(parameters.IsArray ? parameters[1] : parameters["default"], 0)) <= Parser.TryParse(parameters.IsArray ? parameters[2] : parameters["value"], 0);
                        return !not ? value : !value;
                    }
                case "StoryLoadIntGreaterEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 3 || parameters.IsObject && parameters["load"] == null)
                            break;

                        var value = StoryManager.inst.LoadInt(parameters.IsArray ? parameters[0] : parameters["load"], Parser.TryParse(parameters.IsArray ? parameters[1] : parameters["default"], 0)) >= Parser.TryParse(parameters.IsArray ? parameters[2] : parameters["value"], 0);
                        return !not ? value : !value;
                    }
                case "StoryLoadIntLesser":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 3 || parameters.IsObject && parameters["load"] == null)
                            break;

                        var value = StoryManager.inst.LoadInt(parameters.IsArray ? parameters[0] : parameters["load"], Parser.TryParse(parameters.IsArray ? parameters[1] : parameters["default"], 0)) < Parser.TryParse(parameters.IsArray ? parameters[2] : parameters["value"], 0);
                        return !not ? value : !value;
                    }
                case "StoryLoadIntGreater":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 3 || parameters.IsObject && parameters["load"] == null)
                            break;

                        var value = StoryManager.inst.LoadInt(parameters.IsArray ? parameters[0] : parameters["load"], Parser.TryParse(parameters.IsArray ? parameters[1] : parameters["default"], 0)) > Parser.TryParse(parameters.IsArray ? parameters[2] : parameters["value"], 0);
                        return !not ? value : !value;
                    }
                case "StoryLoadBool":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 3 || parameters.IsObject && parameters["load"] == null)
                            break;

                        var value = StoryManager.inst.LoadBool(parameters.IsArray ? parameters[0] : parameters["load"], Parser.TryParse(parameters.IsArray ? parameters[1] : parameters["default"], false));
                        return !not ? value : !value;
                    }
                #region LevelRanks

                case "ChapterFullyRanked":
                    {
                        var isArray = parameters.IsArray;
                        var chapter = Parser.TryParse(isArray ? parameters[0] : parameters["chapter"], 0);
                        var minRank = (isArray ? parameters.Count < 2 : parameters["min_rank"] == null) ? StoryManager.CHAPTER_RANK_REQUIREMENT :
                                    Parser.TryParse(isArray ? parameters[1] : parameters["min_rank"], 0);
                        var maxRank = (isArray ? parameters.Count < 3 : parameters["max_rank"] == null) ? 1 :
                                    Parser.TryParse(isArray ? parameters[2] : parameters["max_rank"], 0);
                        var bonus = (isArray ? parameters.Count < 4 : parameters["bonus"] == null) ? false :
                                    Parser.TryParse(isArray ? parameters[3] : parameters["bonus"], false);

                        var levelIDs = bonus ? StoryMode.Instance.bonusChapters : StoryMode.Instance.chapters;

                        var value =
                            chapter < levelIDs.Count &&
                            levelIDs[chapter].levels.All(x => x.bonus ||
                                            StoryManager.inst.Saves.TryFind(y => y.ID == x.id, out LevelManager.PlayerData playerData) &&
                                            LevelManager.levelRankIndexes[LevelManager.GetLevelRank(playerData).name] >= maxRank &&
                                            LevelManager.levelRankIndexes[LevelManager.GetLevelRank(playerData).name] <= minRank);

                        return !not ? value : !value;
                    }
                case "LevelRankEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["rank"] == null)
                            break;

                        var value = LevelManager.CurrentLevel.playerData != null && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(LevelManager.CurrentLevel).name] == (parameters.IsArray ? parameters[0].AsInt : parameters["rank"].AsInt);
                        return !not ? value : !value;
                    }
                case "LevelRankLesserEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["rank"] == null)
                            break;

                        var value = LevelManager.CurrentLevel.playerData != null && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(LevelManager.CurrentLevel).name] <= (parameters.IsArray ? parameters[0].AsInt : parameters["rank"].AsInt);
                        return !not ? value : !value;
                    }
                case "LevelRankGreaterEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["rank"] == null)
                            break;

                        var value = LevelManager.CurrentLevel.playerData != null && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(LevelManager.CurrentLevel).name] >= (parameters.IsArray ? parameters[0].AsInt : parameters["rank"].AsInt);
                        return !not ? value : !value;
                    }
                case "LevelRankLesser":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["rank"] == null)
                            break;

                        var value = LevelManager.CurrentLevel.playerData != null && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(LevelManager.CurrentLevel).name] < (parameters.IsArray ? parameters[0].AsInt : parameters["rank"].AsInt);
                        return !not ? value : !value;
                    }
                case "LevelRankGreater":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["rank"] == null)
                            break;

                        var value = LevelManager.CurrentLevel.playerData != null && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(LevelManager.CurrentLevel).name] > (parameters.IsArray ? parameters[0].AsInt : parameters["rank"].AsInt);
                        return !not ? value : !value;
                    }
                case "StoryLevelRankEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["id"] == null || parameters["rank"] == null))
                            break;

                        var isArray = parameters.IsArray;
                        var id = isArray ? parameters[0].Value : parameters["id"].Value;
                        
                        var value = StoryManager.inst.Saves.TryFind(x => x.ID == id, out LevelManager.PlayerData playerData) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(playerData).name] == (parameters.IsArray ? parameters[1].AsInt : parameters["rank"].AsInt);
                        return !not ? value : !value;
                    }
                case "StoryLevelRankLesserEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["id"] == null || parameters["rank"] == null))
                            break;

                        var isArray = parameters.IsArray;
                        var id = isArray ? parameters[0].Value : parameters["id"].Value;
                        
                        var value = StoryManager.inst.Saves.TryFind(x => x.ID == id, out LevelManager.PlayerData playerData) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(playerData).name] <= (parameters.IsArray ? parameters[1].AsInt : parameters["rank"].AsInt);
                        return !not ? value : !value;
                    }
                case "StoryLevelRankGreaterEquals":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["id"] == null || parameters["rank"] == null))
                            break;

                        var isArray = parameters.IsArray;
                        var id = isArray ? parameters[0].Value : parameters["id"].Value;
                        
                        var value = StoryManager.inst.Saves.TryFind(x => x.ID == id, out LevelManager.PlayerData playerData) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(playerData).name] >= (parameters.IsArray ? parameters[1].AsInt : parameters["rank"].AsInt);
                        return !not ? value : !value;
                    }
                case "StoryLevelRankLesser":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["id"] == null || parameters["rank"] == null))
                            break;

                        var isArray = parameters.IsArray;
                        var id = isArray ? parameters[0].Value : parameters["id"].Value;
                        
                        var value = StoryManager.inst.Saves.TryFind(x => x.ID == id, out LevelManager.PlayerData playerData) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(playerData).name] < (parameters.IsArray ? parameters[1].AsInt : parameters["rank"].AsInt);
                        return !not ? value : !value;
                    }
                case "StoryLevelRankGreater":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["id"] == null || parameters["rank"] == null))
                            break;

                        var isArray = parameters.IsArray;
                        var id = isArray ? parameters[0].Value : parameters["id"].Value;
                        
                        var value = StoryManager.inst.Saves.TryFind(x => x.ID == id, out LevelManager.PlayerData playerData) && LevelManager.levelRankIndexes[LevelManager.GetLevelRank(playerData).name] > (parameters.IsArray ? parameters[1].AsInt : parameters["rank"].AsInt);
                        return !not ? value : !value;
                    }
                    #endregion
            }

            return false;
        }

        /// <summary>
        /// Parses a singular "func" JSON and performs an action based on the name and parameters.
        /// </summary>
        /// <param name="jn">The func JSON. Must have a name and a params array. If it has a "if_func", then it will parse and check if it's true.</param>
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
                #region Main Functions

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
                case "Wait":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["t"] == null || parameters["func"] == null))
                            break;

                        var isArray = parameters.IsArray;
                        var t = isArray ? parameters[0].AsFloat : parameters["t"].AsFloat;
                        JSONNode func = isArray ? parameters[1] : parameters["func"];

                        CoreHelper.PerformActionAfterSeconds(t, () =>
                        {
                            try
                            {
                                ParseFunction(func);
                            }
                            catch
                            {

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

                #endregion

                #region Interface

                #region Close

                // Closes the interface and returns to the game (if user is in the Game scene).
                // Function has no parameters.
                case "Close":
                    {
                        string id = InterfaceManager.inst.CurrentMenu?.id;
                        InterfaceManager.inst.CloseMenus();
                        InterfaceManager.inst.StopMusic();

                        if (CoreHelper.InGame)
                        {
                            AudioManager.inst.CurrentAudioSource.UnPause();
                            GameManager.inst.gameState = GameManager.State.Playing;
                            InterfaceManager.inst.interfaces.RemoveAll(x => x.id == id);
                        }

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
                            InterfaceManager.inst.SetCurrentInterface(parameters.IsArray ? parameters[0] : parameters["id"]);

                        break;
                    }

                #endregion

                #region Reload

                // Reloads the interface and sets it to the main menu. Only recommended if you want to return to the main menu and unload every other interface.
                // Function has no parameters.
                case "Reload":
                    {
                        var splashTextPath = $"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}splashes.txt";
                        if (RTFile.FileExists(splashTextPath))
                        {
                            var splashes = CoreHelper.GetLines(RTFile.ReadFromFile(splashTextPath));
                            var splashIndex = UnityEngine.Random.Range(0, splashes.Length);
                            LegacyPlugin.SplashText = splashes[splashIndex];
                        }
                        ChangeLogMenu.Seen = false;
                        InterfaceManager.inst.randomIndex = -1;
                        InterfaceManager.inst.StartupInterface();

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
                            break;

                        if (parameters.IsArray && parameters.Count > 2 || parameters.IsObject && parameters["path"] != null)
                            InterfaceManager.inst.MainDirectory = ParseText(RTFile.ParsePaths(parameters.IsArray ? parameters[2] : parameters["path"]));

                        if (!InterfaceManager.inst.MainDirectory.Contains(RTFile.ApplicationDirectory))
                            InterfaceManager.inst.MainDirectory = RTFile.CombinePaths(RTFile.ApplicationDirectory, InterfaceManager.inst.MainDirectory);

                        var path = RTFile.CombinePaths(InterfaceManager.inst.MainDirectory, $"{(parameters.IsArray ? parameters[0].Value : parameters["file"].Value)}.lsi");

                        if (!RTFile.FileExists(path))
                        {
                            CoreHelper.LogError($"Interface {(parameters.IsArray ? parameters[0] : parameters["file"])} does not exist!");

                            break;
                        }

                        var interfaceJN = JSON.Parse(RTFile.ReadFromFile(path));

                        var menu = CustomMenu.Parse(interfaceJN);
                        menu.filePath = path;

                        if (InterfaceManager.inst.interfaces.Has(x => x.id == menu.id))
                        {
                            if (parameters.IsArray && (parameters.Count < 2 || Parser.TryParse(parameters[1], false)) || parameters.IsObject && Parser.TryParse(parameters["load"], true))
                                InterfaceManager.inst.SetCurrentInterface(menu.id);

                            break;
                        }

                        InterfaceManager.inst.interfaces.Add(menu);

                        if (parameters.IsArray && (parameters.Count < 2 || Parser.TryParse(parameters[1], false)) || parameters.IsObject && Parser.TryParse(parameters["load"], true))
                            InterfaceManager.inst.SetCurrentInterface(menu.id);

                        break;
                    }

                #endregion

                #region ClearInterfaces

                // Clears all interfaces from the interfaces list.
                // Function has no parameters.
                case "ClearInterfaces":
                    {
                        InterfaceManager.inst.interfaces.Clear();
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

                        InterfaceManager.inst.MainDirectory = RTFile.ParsePaths(parameters.IsArray ? parameters[0] : parameters["path"]);

                        if (!InterfaceManager.inst.MainDirectory.Contains(RTFile.ApplicationDirectory))
                            InterfaceManager.inst.MainDirectory = RTFile.CombinePaths(RTFile.ApplicationDirectory, InterfaceManager.inst.MainDirectory);

                        break;
                    }

                #endregion

                #endregion

                #region Audio

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
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["sound"] == null || InterfaceManager.inst.CurrentMenu == null)
                            break;

                        string sound = parameters.IsArray ? parameters[0] : parameters["sound"];

                        if (AudioManager.inst.library.soundClips.ContainsKey(sound))
                        {
                            AudioManager.inst.PlaySound(sound);
                            break;
                        }

                        var filePath = $"{Path.GetDirectoryName(InterfaceManager.inst.CurrentMenu.filePath)}{sound}";
                        if (!RTFile.FileExists(filePath))
                            return;

                        var audioType = RTFile.GetAudioType(filePath);
                        if (audioType == AudioType.MPEG)
                            AudioManager.inst.PlaySound(LSAudio.CreateAudioClipUsingMP3File(filePath));
                        else
                            CoreHelper.StartCoroutine(AlephNetworkManager.DownloadAudioClip($"file://{filePath}", audioType, AudioManager.inst.PlaySound));

                        break;
                    }

                #endregion

                #region PlayMusic

                // Plays a music. Can either be a default internal song or one located in the menu path.
                // Supports both JSON array and JSON object.
                //
                // - JSON Array Structure -
                // 0 = song
                // 1 = fade duration
                // 2 = loop
                // Example:
                // [
                //   "distance" < plays the distance song.
                //   "2" < sets fade duration to 2.
                //   "False" < doesn't loop
                // ]
                //
                // - JSON Object Structure -
                // "sound"
                // Example:
                // {
                //   "sound": "some kind of song.ogg" < since this song does not exist in the SoundLibrary, search for a file with the name. If it exists, play the song.
                // }
                case "PlayMusic":
                    {
                        if (parameters == null || parameters.IsArray && (parameters.Count < 1 || parameters[0].Value.ToLower() == "default") || parameters.IsObject && (parameters["name"] == null || parameters["name"].Value.ToLower() == "default"))
                        {
                            InterfaceManager.inst.PlayMusic();
                            break;
                        }

                        var isArray = parameters.IsArray;
                        var isObject = parameters.IsObject;
                        string music = isArray ? parameters[0] : parameters["name"];
                        var fadeDuration = 0.5f;
                        if (isArray && parameters.Count > 1 || isObject && parameters["fade_duration"] != null)
                            fadeDuration = isArray ? parameters[1].AsFloat : parameters["fade_duration"].AsFloat;

                        var loop = true;
                        if (isArray && parameters.Count > 2 || isObject && parameters["loop"] != null)
                            loop = isArray ? parameters[2].AsBool : parameters["loop"].AsBool;

                        var filePath = $"{Path.GetDirectoryName(InterfaceManager.inst.CurrentMenu.filePath)}{music}";
                        if (!RTFile.FileExists(filePath))
                        {
                            InterfaceManager.inst.PlayMusic(AudioManager.inst.GetMusic(music), fadeDuration: fadeDuration, loop: loop);
                            return;
                        }

                        var audioType = RTFile.GetAudioType(filePath);
                        if (audioType == AudioType.MPEG)
                            InterfaceManager.inst.PlayMusic(LSAudio.CreateAudioClipUsingMP3File(filePath), fadeDuration: fadeDuration, loop: loop);
                        else
                            CoreHelper.StartCoroutine(AlephNetworkManager.DownloadAudioClip($"file://{filePath}", audioType, audioClip => InterfaceManager.inst.PlayMusic(audioClip, fadeDuration: fadeDuration, loop: loop)));

                        break;
                    }

                #endregion

                #region StopMusic

                // Stops the currently playing music. Can be good for moments where we want silence.
                // Function has no parameters.
                case "StopMusic":
                    {
                        InterfaceManager.inst.StopMusic();
                        break;
                    }

                #endregion

                #region PauseMusic

                // Pauses the current music if it's currently playing.
                case "PauseMusic":
                    {
                        if (CoreHelper.InGame && parameters != null && (parameters.IsArray && !parameters[0].AsBool || parameters.IsObject && !parameters["game_audio"].AsBool))
                            InterfaceManager.inst.CurrentAudioSource.Pause();
                        else
                            AudioManager.inst.CurrentAudioSource.Pause();

                        break;
                    }

                #endregion

                #region ResumeMusic

                // Resumes the current music if it was paused.
                case "ResumeMusic":
                    {
                        if (CoreHelper.InGame && parameters != null && (parameters.IsArray && !parameters[0].AsBool || parameters.IsObject && !parameters["game_audio"].AsBool))
                            InterfaceManager.inst.CurrentAudioSource.UnPause();
                        else
                            AudioManager.inst.CurrentAudioSource.UnPause();

                        break;
                    }

                #endregion

                #endregion

                #region Elements

                #region SetElementActive

                // Sets an element active or inactive.
                // Supports both JSON array and JSON object.
                //
                // - JSON Array Structure -
                // 0 = id
                // 1 = actiive
                // Example:
                // [
                //   "525778246", < finds an element with this ID.
                //   "False" < sets the element inactive.
                // ]
                //
                // - JSON Object Structure -
                // "id"
                // "active"
                // Example:
                // {
                //   "id": "525778246",
                //   "active": "True" < sets the element active
                // }
                case "SetElementActive":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && parameters["id"] == null || InterfaceManager.inst.CurrentMenu == null)
                            return;

                        if (InterfaceManager.inst.CurrentMenu.elements.TryFind(x => x.id == (parameters.IsArray ? parameters[0] : parameters["id"]), out MenuImage menuImage) &&
                            menuImage.gameObject && bool.TryParse(parameters.IsArray ? parameters[1] : parameters["active"], out bool active))
                        {
                            menuImage.gameObject.SetActive(active);
                        }

                        break;
                    }

                #endregion

                #region SetLayoutActive

                // Sets a layout active or inactive.
                // Supports both JSON array and JSON object.
                //
                // - JSON Array Structure -
                // 0 = name
                // 1 = active
                // Example:
                // [
                //   "layout_name", < finds a layout with this ID.
                //   "False" < sets the layout inactive.
                // ]
                //
                // - JSON Object Structure -
                // "name"
                // "active"
                // Example:
                // {
                //   "id": "layout_name",
                //   "active": "True" < sets the layout active
                // }
                case "SetLayoutActive":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && parameters["name"] == null || InterfaceManager.inst.CurrentMenu == null)
                            return;

                        if (InterfaceManager.inst.CurrentMenu.layouts.TryGetValue(parameters.IsArray ? parameters[0] : parameters["name"], out Layouts.MenuLayoutBase layout) &&
                            layout.gameObject && bool.TryParse(parameters.IsArray ? parameters[1] : parameters["active"], out bool active))
                        {
                            layout.gameObject.SetActive(active);
                        }

                        break;
                    }

                #endregion

                #region AnimateID

                // Finds an element with a matching ID and animates it.
                // Supports both JSON array and JSON object.
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
                // 
                // - JSON Object Structure -
                // "id"
                // "type"
                // "loop"
                // "events" ("x", "y", "z")
                // Example:
                // {
                //   "id": "0",
                //   "type": "1", < animates scale
                //   "loop": "False", < loop doesn't need to exist.
                //   "events": {
                //     "x": [
                //       {
                //         "t": "0",
                //         "val": "0"
                //       }
                //     ],
                //     "y": [
                //       {
                //         "t": "0",
                //         "val": "0"
                //       }
                //     ],
                //     "y": [
                //       {
                //         "t": "0",
                //         "val": "0"
                //       }
                //     ]
                //   },
                //   "done_func": { < function code here
                //   }
                // }
                case "AnimateID":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["id"] == null)
                            return;

                        var isArray = parameters.IsArray;
                        string id = isArray ? parameters[0] : parameters["id"]; // ID of an object to animate
                        var type = Parser.TryParse(isArray ? parameters[1] : parameters["type"], 0); // which type to animate (e.g. 0 = position, 1 = scale, 2 = rotation)
                        var isColor = type == 3;

                        if (InterfaceManager.inst.CurrentMenu.elements.TryFind(x => x.id == id, out MenuImage element))
                        {
                            var animation = new RTAnimation($"Interface Element Animation {element.id}"); // no way element animation reference :scream:

                            animation.loop = isArray ? parameters[2].AsBool : parameters["loop"].AsBool;

                            var events = isArray ? parameters[3] : parameters["events"];

                            JSONNode lastX = null;
                            float x = 0f;
                            if (!isColor && events["x"] != null)
                            {
                                List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                for (int i = 0; i < events["x"].Count; i++)
                                {
                                    var kf = events["x"][i];
                                    var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? element.GetTransform(type, 0) : 0f);
                                    x = kf["rel"].AsBool ? x + val : val;
                                    keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, x, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                    lastX = kf["val"];
                                }
                                animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, x => { element.SetTransform(type, 0, x); }));
                            }
                            if (isColor && events["x"] != null)
                            {
                                List<IKeyframe<Color>> keyframes = new List<IKeyframe<Color>>();
                                for (int i = 0; i < events["x"].Count; i++)
                                {
                                    var kf = events["x"][i];
                                    var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? element.GetTransform(type, 0) : 0f);
                                    x = kf["rel"].AsBool ? x + val : val;
                                    keyframes.Add(new ThemeKeyframe(kf["t"].AsFloat, (int)x, 0.0f, 0.0f, 0.0f, 0.0f, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                    lastX = kf["val"];
                                }
                                animation.animationHandlers.Add(new AnimationHandler<Color>(keyframes, x =>
                                {
                                    element.useOverrideColor = true;
                                    element.overrideColor = x;
                                }));
                            }

                            JSONNode lastY = null;
                            float y = 0f;
                            if (!isColor && events["y"] != null)
                            {
                                List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                for (int i = 0; i < events["y"].Count; i++)
                                {
                                    var kf = events["y"][i];
                                    var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? element.GetTransform(type, 1) : 0f);
                                    y = kf["rel"].AsBool ? y + val : val;
                                    keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, y, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                    lastY = kf["val"];
                                }
                                animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, x => { element.SetTransform(type, 1, x); }));
                            }

                            JSONNode lastZ = null;
                            float z = 0f;
                            if (!isColor && events["z"] != null)
                            {
                                List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                for (int i = 0; i < events["z"].Count; i++)
                                {
                                    var kf = events["z"][i];
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
                                    if (isArray && parameters.Count > 4 && parameters[4] != null || parameters["done_func"] != null)
                                        ParseFunction(isArray ? parameters[4] : parameters["done_func"]);

                                    return;
                                }

                                AnimationManager.inst.Remove(animation.id);
                                animations.RemoveAll(x => x.id == animation.id);

                                if (!isColor && lastX != null)
                                    element.SetTransform(type, 0, x);
                                if (!isColor && lastY != null)
                                    element.SetTransform(type, 1, y);
                                if (!isColor && lastZ != null)
                                    element.SetTransform(type, 2, z);
                                if (isColor && lastX != null)
                                    element.overrideColor = CoreHelper.CurrentBeatmapTheme.GetObjColor((int)x);

                                if (isArray && parameters.Count > 4 && parameters[4] != null || parameters["done_func"] != null)
                                    ParseFunction(isArray ? parameters[4] : parameters["done_func"]);
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

                        if (InterfaceManager.inst.CurrentMenu.elements.TryFind(x => x.name == elementName, out MenuImage element))
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

                                AnimationManager.inst.Remove(animation.id);
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

                #region StopAnimations

                // Stops all local animations created from the element.
                // Supports both JSON array and JSON object.
                //
                // - JSON Array Structure -
                // 0 = stop (runs onComplete method)
                // 1 = id
                // 2 = name
                // Example:
                // [
                //   "True", < makes the animation run its on complete function.
                //   "0", < makes the animation run its on complete function.
                //   "0" < makes the animation run its on complete function.
                // ]
                //
                // - JSON Object Structure -
                // "stop"
                // "id"
                // "name"
                // Example:
                // {
                //   "run_done_func": "False", < doesn't run on complete functions.
                //   "id": "0", < tries to find an element with the matching ID.
                //   "name": "355367" < checks if the animations' name contains this. If it does, then stop the animation. (name is based on the element ID it animates)
                // }
                case "StopAnimations":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["run_done_func"] == null)
                            return;

                        var stop = parameters.IsArray ? parameters[0].AsBool : parameters["run_done_func"].AsBool;

                        var animations = this.animations;
                        string id = parameters.IsArray && parameters.Count > 1 ? parameters[1] : parameters.IsObject && parameters["id"] != null ? parameters["id"] : "";
                        if (!string.IsNullOrEmpty(id) && InterfaceManager.inst.CurrentMenu.elements.TryFind(x => x.id == id, out MenuImage menuImage))
                            animations = menuImage.animations;

                        string animName = parameters.IsArray && parameters.Count > 2 ? parameters[2] : parameters.IsObject && parameters["name"] != null ? parameters["name"] : "";

                        for (int i = 0; i < animations.Count; i++)
                        {
                            var animation = animations[i];
                            if (!string.IsNullOrEmpty(animName) && !animation.name.Replace("Interface Element Animation ", "").Contains(animName))
                                continue;

                            if (stop)
                                animation.onComplete?.Invoke();

                            animation.Stop();
                            AnimationManager.inst.Remove(animation.id);
                        }

                        break;
                    }

                #endregion

                #region SetColor

                // Sets the elements' color slot.
                // Supports both JSON array and JSON object.
                //
                // - JSON Array Structure -
                // 0 = color
                // Example:
                // [
                //   "2"
                // ]
                //
                // - JSON Object Structure -
                // "col"
                // Example:
                // {
                //   "col": "17" < uses Beatmap Theme object color slots, so max should be 17 (including 0).
                // }
                case "SetColor":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["col"] == null)
                            return;

                        color = parameters.IsArray ? parameters[0].AsInt : parameters["col"].AsInt;

                        break;
                    }

                #endregion

                #region SetText

                // Sets an objects' text.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = id
                // 0 = text
                // Example:
                // [
                //   "100",
                //   "This is a text example!"
                // ]
                // 
                // - JSON Object Structure -
                // "id"
                // "text"
                // Example:
                // {
                //   "id": "100",
                //   "text": "This is a text example!"
                // }
                case "SetText":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["id"] == null || parameters["text"] == null) || InterfaceManager.inst.CurrentMenu == null)
                            return;

                        var isArray = parameters.IsArray;
                        string array = isArray ? parameters[0] : parameters["id"];
                        string text = isArray ? parameters[1] : parameters["text"];

                        if (InterfaceManager.inst.CurrentMenu.elements.TryFind(x => x.id == array, out MenuImage menuImage) && menuImage is MenuText menuText)
                        {
                            menuText.text = text;
                            menuText.textUI.maxVisibleCharacters = text.Length;
                            menuText.textUI.text = text;
                        }

                        break;
                    }

                #endregion

                #region RemoveElement

                // Finds an element with a matching ID, destroys its object and removes it.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = id
                // Example:
                // [
                //   "522666" < ID to find
                // ]
                // 
                // - JSON Object Structure -
                // "id"
                // Example:
                // {
                //   "id": "85298259"
                // }
                case "RemoveElement":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["id"] == null)
                            break;

                        var id = parameters.IsArray ? parameters[0] : parameters["id"];
                        if (InterfaceManager.inst.CurrentMenu.elements.TryFind(x => x.id == id, out MenuImage element))
                        {
                            element.Clear();
                            if (element.gameObject)
                                CoreHelper.Destroy(element.gameObject);
                            InterfaceManager.inst.CurrentMenu.elements.Remove(element);
                        }

                        break;
                    }

                #endregion

                #region RemoveMultipleElements

                // Finds an element with a matching ID, destroys its object and removes it.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = ids
                // Example:
                // [
                //   [
                //     "522666",
                //     "2672",
                //     "824788",
                //   ]
                // ]
                // 
                // - JSON Object Structure -
                // "ids"
                // Example:
                // {
                //   "ids": [
                //     "522666",
                //     "2672",
                //     "824788",
                //   ]
                // }
                case "RemoveMultipleElements":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["ids"] == null)
                            break;

                        var ids = parameters.IsArray ? parameters[0] : parameters["ids"];
                        if (ids == null)
                            break;

                        for (int i = 0; i < ids.Count; i++)
                        {
                            var id = ids[i];
                            if (InterfaceManager.inst.CurrentMenu.elements.TryFind(x => x.id == id, out MenuImage element))
                            {
                                element.Clear();
                                if (element.gameObject)
                                    CoreHelper.Destroy(element.gameObject);
                                InterfaceManager.inst.CurrentMenu.elements.Remove(element);
                            }
                        }

                        break;
                    }

                #endregion

                #region AddElement

                // Adds a list of elements to the interface.
                // Supports both JSON array and JSON object.
                // 
                // - JSON Array Structure -
                // 0 = elements
                // Example:
                // [
                //   {
                //     "type": "Image",
                //     "id": "5343663626",
                //     "name": "BG",
                //     "rect": {
                //       "anc_pos": {
                //         "x": "0",
                //         "y": "0"
                //       }
                //     }
                //   }
                // ]
                // 
                // - JSON Object Structure -
                // "elements"
                // Example:
                // {
                //   "type": "Image",
                //   "id": "5343663626",
                //   "name": "BG",
                //   "rect": {
                //     "anc_pos": {
                //       "x": "0",
                //       "y": "0"
                //     }
                //   }
                // }
                case "AddElement":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["elements"] == null)
                            break;

                        var customMenu = InterfaceManager.inst.CurrentMenu;
                        customMenu.elements.AddRange(CustomMenu.ParseElements(parameters.IsArray ? parameters[0] : parameters["elements"], customMenu.prefabs, customMenu.spriteAssets));

                        CoreHelper.StartCoroutine(customMenu.GenerateUI());

                        break;
                    }

                #endregion

                #endregion

                #region Effects

                #region SetDefaultEvents

                case "SetDefaultEvents":
                    {
                        if (CoreHelper.InGame || !MenuEffectsManager.inst)
                            break;

                        MenuEffectsManager.inst.UpdateChroma(0.1f);

                        break;
                    }

                #endregion

                #region AnimateEvent

                // Animates a specific type of event (e.g. camera).
                // Supports both JSON array and JSON object.
                //
                // - JSON Array Structure -
                // 0 = type (name, "MoveCamera", "ZoomCamera", "RotateCamera")
                // 1 = looping (boolean true or false)
                // 2 = keyframes
                // 3 = anim done func
                // Example:
                // [
                //   "MoveCamera",
                //   "False",
                //   {
                //     "x": [
                //       {
                //         "t": "0",
                //         "val": "0"
                //       }
                //     ],
                //     "y": [
                //       {
                //         "t": "0",
                //         "val": "0"
                //       }
                //     ]
                //   }
                // ]
                // 
                // - JSON Object Structure -
                // "type"
                // "loop"
                // "events"
                // "done_func"
                // Example: (zooms the camera in and out)
                // {
                //   "type": "ZoomCamera",
                //   "loop": "True",
                //   "events": {
                //     "x": [
                //       {
                //         "t": "0",
                //         "val": "5" < 5 is the default camera zoom.
                //       },
                //       {
                //         "t": "1",
                //         "val": "7",
                //         "ct": "InOutSine"
                //       },
                //       {
                //         "t": "2",
                //         "val": "5",
                //         "ct": "InOutSine"
                //       }
                //     ]
                //   }
                // }
                case "AnimateEvent":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["type"] == null)
                            return;

                        var isArray = parameters.IsArray;
                        var type = isArray ? parameters[0] : parameters["type"];

                        if (type.IsNumber)
                            return;

                        var events = isArray ? parameters[2] : parameters["events"];
                        var animation = new RTAnimation($"Interface Element Animation {id}"); // no way element animation reference :scream:

                        animation.loop = isArray ? parameters[1].AsBool : parameters["loop"].AsBool;

                        switch (type.Value)
                        {
                            case "MoveCamera":
                                {
                                    JSONNode lastX = null;
                                    float x = 0f;
                                    if (events["x"] != null)
                                    {
                                        List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                        for (int i = 0; i < events["x"].Count; i++)
                                        {
                                            var kf = events["x"][i];
                                            var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localPosition.x : 0f);
                                            x = kf["rel"].AsBool ? x + val : val;
                                            keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, x, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                            lastX = kf["val"];
                                        }
                                        animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.MoveCameraX));
                                    }

                                    JSONNode lastY = null;
                                    float y = 0f;
                                    if (events["y"] != null)
                                    {
                                        List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                        for (int i = 0; i < events["y"].Count; i++)
                                        {
                                            var kf = events["y"][i];
                                            var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localPosition.y : 0f);
                                            y = kf["rel"].AsBool ? y + val : val;
                                            keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, y, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                            lastY = kf["val"];
                                        }
                                        animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.MoveCameraY));
                                    }

                                    animation.onComplete = () =>
                                    {
                                        if (animation.loop)
                                        {
                                            if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                                ParseFunction(isArray ? parameters[3] : parameters["done_func"]);

                                            return;
                                        }

                                        AnimationManager.inst.Remove(animation.id);
                                        animations.RemoveAll(x => x.id == animation.id);

                                        if (lastX != null)
                                            MenuEffectsManager.inst.MoveCameraX(x);
                                        if (lastY != null)
                                            MenuEffectsManager.inst.MoveCameraY(y);

                                        if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                            ParseFunction(isArray ? parameters[3] : parameters["done_func"]);
                                    };

                                    break;
                                }
                            case "ZoomCamera":
                                {
                                    JSONNode lastX = null;
                                    float x = 0f;
                                    if (events["x"] != null)
                                    {
                                        List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                        for (int i = 0; i < events["x"].Count; i++)
                                        {
                                            var kf = events["x"][i];
                                            var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.orthographicSize : 0f);
                                            x = kf["rel"].AsBool ? x + val : val;
                                            keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, x, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                            lastX = kf["val"];
                                        }
                                        animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.ZoomCamera));
                                    }

                                    animation.onComplete = () =>
                                    {
                                        if (animation.loop)
                                        {
                                            if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                                ParseFunction(isArray ? parameters[3] : parameters["done_func"]);

                                            return;
                                        }

                                        AnimationManager.inst.Remove(animation.id);
                                        animations.RemoveAll(x => x.id == animation.id);

                                        if (lastX != null)
                                            MenuEffectsManager.inst.ZoomCamera(x);

                                        if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                            ParseFunction(isArray ? parameters[3] : parameters["done_func"]);
                                    };

                                    break;
                                }
                            case "RotateCamera":
                                {
                                    JSONNode lastX = null;
                                    float x = 0f;
                                    if (events["x"] != null)
                                    {
                                        List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                        for (int i = 0; i < events["x"].Count; i++)
                                        {
                                            var kf = events["x"][i];
                                            var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                            x = kf["rel"].AsBool ? x + val : val;
                                            keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, x, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                            lastX = kf["val"];
                                        }
                                        animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.RotateCamera));
                                    }

                                    animation.onComplete = () =>
                                    {
                                        if (animation.loop)
                                        {
                                            if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                                ParseFunction(isArray ? parameters[3] : parameters["done_func"]);

                                            return;
                                        }

                                        AnimationManager.inst.Remove(animation.id);
                                        animations.RemoveAll(x => x.id == animation.id);

                                        if (lastX != null)
                                            MenuEffectsManager.inst.RotateCamera(x);

                                        if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                            ParseFunction(isArray ? parameters[3] : parameters["done_func"]);
                                    };

                                    break;
                                }
                            case "Chroma":
                            case "Chromatic":
                                {
                                    JSONNode lastX = null;
                                    float x = 0f;
                                    if (events["x"] != null)
                                    {
                                        List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                        for (int i = 0; i < events["x"].Count; i++)
                                        {
                                            var kf = events["x"][i];
                                            var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                            x = kf["rel"].AsBool ? x + val : val;
                                            keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, x, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                            lastX = kf["val"];
                                        }
                                        animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateChroma));
                                    }

                                    animation.onComplete = () =>
                                    {
                                        if (animation.loop)
                                        {
                                            if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                                ParseFunction(isArray ? parameters[3] : parameters["done_func"]);

                                            return;
                                        }

                                        AnimationManager.inst.Remove(animation.id);
                                        animations.RemoveAll(x => x.id == animation.id);

                                        if (lastX != null)
                                            MenuEffectsManager.inst.UpdateChroma(x);

                                        if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                            ParseFunction(isArray ? parameters[3] : parameters["done_func"]);
                                    };

                                    break;
                                }
                            case "Bloom":
                                {
                                    JSONNode lastX = null;
                                    float x = 0f;
                                    if (events["x"] != null)
                                    {
                                        List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                        for (int i = 0; i < events["x"].Count; i++)
                                        {
                                            var kf = events["x"][i];
                                            var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                            x = kf["rel"].AsBool ? x + val : val;
                                            keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, x, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                            lastX = kf["val"];
                                        }
                                        animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateBloomIntensity));
                                    }
                                    
                                    JSONNode lastY = null;
                                    float y = 0f;
                                    if (events["y"] != null)
                                    {
                                        List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                        for (int i = 0; i < events["y"].Count; i++)
                                        {
                                            var kf = events["y"][i];
                                            var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                            y = kf["rel"].AsBool ? y + val : val;
                                            keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, y, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                            lastY = kf["val"];
                                        }
                                        animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateBloomDiffusion));
                                    }
                                    
                                    JSONNode lastZ = null;
                                    float z = 0f;
                                    if (events["z"] != null)
                                    {
                                        List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                        for (int i = 0; i < events["z"].Count; i++)
                                        {
                                            var kf = events["z"][i];
                                            var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                            z = kf["rel"].AsBool ? z + val : val;
                                            keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, z, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                            lastZ = kf["val"];
                                        }
                                        animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateBloomThreshold));
                                    }
                                    
                                    JSONNode lastX2 = null;
                                    float x2 = 0f;
                                    if (events["x2"] != null)
                                    {
                                        List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                        for (int i = 0; i < events["x2"].Count; i++)
                                        {
                                            var kf = events["x2"][i];
                                            var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                            x2 = kf["rel"].AsBool ? x2 + val : val;
                                            keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, x2, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                            lastX2 = kf["val"];
                                        }
                                        animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateBloomAnamorphicRatio));
                                    }

                                    animation.onComplete = () =>
                                    {
                                        if (animation.loop)
                                        {
                                            if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                                ParseFunction(isArray ? parameters[3] : parameters["done_func"]);

                                            return;
                                        }

                                        AnimationManager.inst.Remove(animation.id);
                                        animations.RemoveAll(x => x.id == animation.id);

                                        if (lastX != null)
                                            MenuEffectsManager.inst.UpdateBloomIntensity(x);
                                        if (lastY != null)
                                            MenuEffectsManager.inst.UpdateBloomIntensity(y);
                                        if (lastZ != null)
                                            MenuEffectsManager.inst.UpdateBloomThreshold(z);
                                        if (lastX2 != null)
                                            MenuEffectsManager.inst.UpdateBloomAnamorphicRatio(x2);

                                        if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                            ParseFunction(isArray ? parameters[3] : parameters["done_func"]);
                                    };

                                    break;
                                }
                            case "Lens":
                            case "LensDistort":
                                {
                                    JSONNode lastX = null;
                                    float x = 0f;
                                    if (events["x"] != null)
                                    {
                                        List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                        for (int i = 0; i < events["x"].Count; i++)
                                        {
                                            var kf = events["x"][i];
                                            var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                            x = kf["rel"].AsBool ? x + val : val;
                                            keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, x, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                            lastX = kf["val"];
                                        }
                                        animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateLensDistortIntensity));
                                    }

                                    JSONNode lastY = null;
                                    float y = 0f;
                                    if (events["y"] != null)
                                    {
                                        List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                        for (int i = 0; i < events["y"].Count; i++)
                                        {
                                            var kf = events["y"][i];
                                            var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                            y = kf["rel"].AsBool ? y + val : val;
                                            keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, y, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                            lastY = kf["val"];
                                        }
                                        animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateLensDistortCenterX));
                                    }

                                    JSONNode lastZ = null;
                                    float z = 0f;
                                    if (events["z"] != null)
                                    {
                                        List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                        for (int i = 0; i < events["z"].Count; i++)
                                        {
                                            var kf = events["z"][i];
                                            var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                            z = kf["rel"].AsBool ? z + val : val;
                                            keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, z, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                            lastZ = kf["val"];
                                        }
                                        animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateLensDistortCenterY));
                                    }

                                    JSONNode lastX2 = null;
                                    float x2 = 0f;
                                    if (events["x2"] != null)
                                    {
                                        List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                        for (int i = 0; i < events["x2"].Count; i++)
                                        {
                                            var kf = events["x2"][i];
                                            var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                            x2 = kf["rel"].AsBool ? x2 + val : val;
                                            keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, x2, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                            lastX2 = kf["val"];
                                        }
                                        animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateLensDistortIntensityX));
                                    }

                                    JSONNode lastY2 = null;
                                    float y2 = 0f;
                                    if (events["y2"] != null)
                                    {
                                        List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                        for (int i = 0; i < events["y2"].Count; i++)
                                        {
                                            var kf = events["y2"][i];
                                            var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                            y2 = kf["rel"].AsBool ? y2 + val : val;
                                            keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, y2, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                            lastY2 = kf["val"];
                                        }
                                        animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateLensDistortIntensityY));
                                    }

                                    JSONNode lastZ2 = null;
                                    float z2 = 0f;
                                    if (events["z2"] != null)
                                    {
                                        List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>();
                                        for (int i = 0; i < events["z2"].Count; i++)
                                        {
                                            var kf = events["z2"][i];
                                            var val = kf["val"].AsFloat + (i == 0 && kf["rel"].AsBool ? Camera.main.transform.localEulerAngles.z : 0f);
                                            z2 = kf["rel"].AsBool ? z2 + val : val;
                                            keyframes.Add(new FloatKeyframe(kf["t"].AsFloat, z2, kf["ct"] != null && Ease.HasEaseFunction(kf["ct"]) ? Ease.GetEaseFunction(kf["ct"]) : Ease.Linear));

                                            lastZ2 = kf["val"];
                                        }
                                        animation.animationHandlers.Add(new AnimationHandler<float>(keyframes, MenuEffectsManager.inst.UpdateLensDistortScale));
                                    }

                                    animation.onComplete = () =>
                                    {
                                        if (animation.loop)
                                        {
                                            if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                                ParseFunction(isArray ? parameters[3] : parameters["done_func"]);

                                            return;
                                        }

                                        AnimationManager.inst.Remove(animation.id);
                                        animations.RemoveAll(x => x.id == animation.id);

                                        if (lastX != null)
                                            MenuEffectsManager.inst.UpdateLensDistortIntensity(x);
                                        if (lastY != null)
                                            MenuEffectsManager.inst.UpdateLensDistortCenterX(y);
                                        if (lastZ != null)
                                            MenuEffectsManager.inst.UpdateLensDistortCenterY(z);
                                        if (lastX2 != null)
                                            MenuEffectsManager.inst.UpdateLensDistortIntensityX(x2);
                                        if (lastY2 != null)
                                            MenuEffectsManager.inst.UpdateLensDistortIntensityY(y2);
                                        if (lastZ2 != null)
                                            MenuEffectsManager.inst.UpdateLensDistortScale(z2);

                                        if (isArray && parameters.Count > 3 && parameters[3] != null || parameters["done_func"] != null)
                                            ParseFunction(isArray ? parameters[3] : parameters["done_func"]);
                                    };

                                    break;
                                }
                        }

                        animations.Add(animation);
                        AnimationManager.inst.Play(animation);

                        break;
                    }

                #endregion

                #region SetEvent

                case "SetEvent":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["type"] == null)
                            return;

                        var isArray = parameters.IsArray;
                        var type = isArray ? parameters[0] : parameters["type"];

                        if (type.IsNumber)
                            return;

                        var values = isArray ? parameters[1] : parameters["values"];

                        switch (type.Value)
                        {
                            case "MoveCamera":
                                {
                                    if (values["x"] != null)
                                    {
                                        MenuEffectsManager.inst.MoveCameraX(values["x"].AsFloat);
                                    }
                                    
                                    if (values["y"] != null)
                                    {
                                        MenuEffectsManager.inst.MoveCameraX(values["y"].AsFloat);
                                    }

                                    break;
                                }
                            case "ZoomCamera":
                                {
                                    if (values["amount"] != null)
                                    {
                                        MenuEffectsManager.inst.ZoomCamera(values["amount"].AsFloat);
                                    }

                                    break;
                                }
                            case "RotateCamera":
                                {
                                    if (values["amount"] != null)
                                    {
                                        MenuEffectsManager.inst.RotateCamera(values["amount"].AsFloat);
                                    }

                                    break;
                                }
                            case "Chroma":
                            case "Chromatic":
                                {
                                    if (values["intensity"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateChroma(values["intensity"].AsFloat);
                                    }

                                    break;
                                }
                            case "Bloom":
                                {
                                    if (values["intensity"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateBloomIntensity(values["intensity"].AsFloat);
                                    }

                                    if (values["diffusion"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateBloomDiffusion(values["diffusion"].AsFloat);
                                    }

                                    if (values["threshold"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateBloomThreshold(values["threshold"].AsFloat);
                                    }

                                    if (values["anamorphic_ratio"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateBloomAnamorphicRatio(values["anamorphic_ratio"].AsFloat);
                                    }
                                    
                                    if (values["col"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateBloomColor(InterfaceManager.inst.CurrentMenu.Theme.GetFXColor(values["col"].AsInt));
                                    }

                                    break;
                                }
                            case "Lens":
                            case "LensDistort":
                                {
                                    if (values["intensity"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateLensDistortIntensity(values["intensity"].AsFloat);
                                    }

                                    if (values["center_x"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateLensDistortCenterX(values["center_x"].AsFloat);
                                    }
                                    
                                    if (values["center_y"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateLensDistortCenterY(values["center_y"].AsFloat);
                                    }
                                    
                                    if (values["intensity_x"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateLensDistortIntensityX(values["intensity_x"].AsFloat);
                                    }
                                    
                                    if (values["intensity_y"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateLensDistortIntensityY(values["intensity_y"].AsFloat);
                                    }
                                    
                                    if (values["scale"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateLensDistortScale(values["scale"].AsFloat);
                                    }

                                    break;
                                }
                            case "Vignette":
                                {
                                    if (values["intensity"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateVignetteIntensity(values["intensity"].AsFloat);
                                    }

                                    if (values["center_x"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateVignetteCenterX(values["center_x"].AsFloat);
                                    }

                                    if (values["center_y"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateVignetteCenterY(values["center_y"].AsFloat);
                                    }

                                    if (values["smoothness"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateVignetteSmoothness(values["smoothness"].AsFloat);
                                    }

                                    if (values["roundness"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateVignetteRoundness(values["roundness"].AsFloat);
                                    }

                                    if (values["rounded"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateVignetteRounded(values["rounded"].AsBool);
                                    }
                                    
                                    if (values["col"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateVignetteColor(InterfaceManager.inst.CurrentMenu.Theme.GetFXColor(values["col"].AsInt));
                                    }

                                    break;
                                }
                            case "AnalogGlitch":
                                {
                                    if (values["enabled"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateAnalogGlitchEnabled(values["enabled"].AsBool);
                                    }

                                    if (values["scan_line_jitter"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateAnalogGlitchScanLineJitter(values["scan_line_jitter"].AsFloat);
                                    }

                                    if (values["vertical_jump"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateAnalogGlitchVerticalJump(values["vertical_jump"].AsFloat);
                                    }

                                    if (values["horizontal_shake"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateAnalogGlitchHorizontalShake(values["horizontal_shake"].AsFloat);
                                    }

                                    if (values["color_drift"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateAnalogGlitchColorDrift(values["color_drift"].AsFloat);
                                    }

                                    break;
                                }
                            case "DigitalGlitch":
                                {
                                    if (values["intensity"] != null)
                                    {
                                        MenuEffectsManager.inst.UpdateDigitalGlitch(values["intensity"].AsFloat);
                                    }

                                    break;
                                }
                        }

                        break;
                    }

                #endregion

                #endregion

                #region LoadLevel

                // Finds a level by its' ID and loads it. On,y work if the user has already loaded levels.
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
                case "LoadLevel":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && parameters["id"] == null)
                            break;

                        if (LevelManager.Levels.TryFind(x => x.id == (parameters.IsArray ? parameters[0] : parameters["id"]), out Level level))
                            CoreHelper.StartCoroutine(LevelManager.Play(level));

                        break;
                    }

                #endregion

                #region InitLevelMenu

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
                case "InitLevelMenu":
                    {
                        var directory = InterfaceManager.inst.MainDirectory;
                        if (parameters != null && (parameters.IsArray && parameters.Count > 0 || parameters["directory"] != null))
                            directory = parameters.IsArray ? parameters[1] : parameters["directory"];
                        if (string.IsNullOrEmpty(directory))
                            directory = InterfaceManager.inst.MainDirectory;

                        var directories = Directory.GetDirectories(directory);
                        var list = directories.Where(x => Level.Verify(x + "/")).Select(x => new Level(x.Replace("\\", "/") + "/")).ToList();

                        Arcade.LevelListMenu.Init(list);
                        break;
                    }

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

                #region OpenLink

                case "OpenLink":
                    {
                        var linkType = Parser.TryParse(parameters.IsArray ? parameters[0] : parameters["link_type"], CoreHelper.LinkType.Artist);

                        var url = CoreHelper.GetURL(linkType, parameters.IsArray ? parameters[1].AsInt : parameters["site"], parameters.IsArray ? parameters[2] : parameters["link"]);

                        Application.OpenURL(url);

                        break;
                    }

                #endregion

                #endregion

                #region Specific Functions

                #region Profile

                case "Profile":
                    {
                        InterfaceManager.inst.CloseMenus();
                        var profileMenu = new ProfileMenu();
                        InterfaceManager.inst.CurrentMenu = profileMenu;

                        break;
                    }

                #endregion

                #region BeginStoryMode

                case "InitPAChat":
                    {
                        PAChatMenu.Init();
                        break;
                    }

                // Begins the BetterLegacy story mode.
                // Function has no parameters.
                case "BeginStoryMode":
                    {
                        LevelManager.IsArcade = false;
                        SceneManager.inst.LoadScene("Input Select");
                        LevelManager.OnInputsSelected = () => { SceneManager.inst.LoadScene("Interface"); };

                        break;
                    }

                case "LoadStoryLevel":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 1 || parameters.IsObject && (parameters["chapter"] == null || parameters["level"] == null))
                            return;

                        var isArray = parameters.IsArray;
                        var chapter = isArray ? parameters[0].AsInt : parameters["chapter"].AsInt;
                        var level = isArray ? parameters[1].AsInt : parameters["level"].AsInt;
                        var bonus = isArray && parameters.Count > 3 ? parameters[3].AsBool : parameters["bonus"] != null ? parameters["bonus"].AsBool : false;
                        var skipCutscenes = isArray && parameters.Count > 4 ? parameters[4].AsBool : parameters["skip_cutscenes"] != null ? parameters["skip_cutscenes"].AsBool : true;

                        StoryManager.inst.ContinueStory = isArray && parameters.Count > 2 && parameters[2].AsBool || parameters.IsObject && parameters["continue"].AsBool;

                        StoryManager.inst.Play(chapter, level, bonus, skipCutscenes);

                        break;
                    }
                    
                case "LoadNextStoryLevel":
                    {
                        var isArray = parameters.IsArray;

                        StoryManager.inst.ContinueStory =
                            parameters == null || (parameters.IsArray && parameters.Count >= 1 && parameters[0].AsBool || parameters.IsObject && parameters["continue"] != null && parameters["continue"].AsBool);

                        int chapter = StoryManager.inst.LoadInt("Chapter", 0);
                        StoryManager.inst.Play(chapter, StoryManager.inst.LoadInt($"DOC{(chapter + 1).ToString("00")}Progress", 0));

                        break;
                    }
                    
                case "LoadCurrentStoryInterface":
                    {
                        InterfaceManager.inst.StartupStoryInterface();

                        break;
                    }

                case "LoadStoryInterface":
                    {
                        InterfaceManager.inst.StartupStoryInterface(parameters.IsArray ? parameters[0].AsInt : parameters["chapter"].AsInt, parameters.IsArray ? parameters[1].AsInt : parameters["level"].AsInt);

                        break;
                    }

                case "StorySaveBool":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["name"] == null || parameters["value"] == null))
                            return;

                        var isArray = parameters.IsArray;
                        string saveName = isArray ? parameters[0] : parameters["name"];
                        var value = isArray ? parameters[1].AsBool : parameters["value"].AsBool;
                        if (isArray ? parameters.Count > 2 && parameters[2].AsBool : parameters["toggle"] != null && parameters["toggle"].AsBool)
                            value = !StoryManager.inst.LoadBool(saveName, value);

                        StoryManager.inst.SaveBool(saveName, value);

                        break;
                    }
                    
                case "StorySaveInt":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["name"] == null || parameters["value"] == null))
                            return;

                        var isArray = parameters.IsArray;
                        string saveName = isArray ? parameters[0] : parameters["name"];
                        var value = isArray ? parameters[1].AsInt : parameters["value"].AsInt;
                        if (isArray ? parameters.Count > 2 && parameters[2].AsBool : parameters["relative"] != null && parameters["relative"].AsBool)
                            value += StoryManager.inst.LoadInt(saveName, value);

                        StoryManager.inst.SaveInt(saveName, value);

                        break;
                    }
                    
                case "StorySaveFloat":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["name"] == null || parameters["value"] == null))
                            return;

                        var isArray = parameters.IsArray;
                        string saveName = isArray ? parameters[0] : parameters["name"];
                        var value = isArray ? parameters[1].AsFloat : parameters["value"].AsFloat;
                        if (isArray ? parameters.Count > 2 && parameters[2].AsBool : parameters["relative"] != null && parameters["relative"].AsBool)
                            value += StoryManager.inst.LoadFloat(saveName, value);

                        StoryManager.inst.SaveFloat(saveName, value);

                        break;
                    }
                    
                case "StorySaveString":
                    {
                        if (parameters == null || parameters.IsArray && parameters.Count < 2 || parameters.IsObject && (parameters["name"] == null || parameters["value"] == null))
                            return;

                        var isArray = parameters.IsArray;
                        string saveName = isArray ? parameters[0] : parameters["name"];
                        var value = isArray ? parameters[1].Value : parameters["value"].Value;
                        if (isArray ? parameters.Count > 2 && parameters[2].AsBool : parameters["relative"] != null && parameters["relative"].AsBool)
                            value += StoryManager.inst.LoadString(saveName, value);

                        StoryManager.inst.SaveString(saveName, value);

                        break;
                    }

                #endregion

                #region ModderDiscord

                // Opens the System Error Discord server link.
                // Function has no parameters.
                case "ModderDiscord":
                    {
                        Application.OpenURL("https://discord.gg/nB27X2JZcY");

                        break;
                    }

                #endregion

                #endregion
            }
        }

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
                name = jnElement["name"];
            if (!string.IsNullOrEmpty(jnElement["parent_layout"]))
                parentLayout = jnElement["parent_layout"];
            if (!string.IsNullOrEmpty(jnElement["parent"]))
                parent = jnElement["parent"];
            if (jnElement["sibling_index"] != null)
                siblingIndex = jnElement["sibling_index"].AsInt;

            #endregion

            #region Spawning

            if (jnElement["regen"] != null)
                regenerate = jnElement["regen"].AsBool;
            fromLoop = j > 0; // if element has been spawned from the loop or if its the first / only of its kind.
            this.loop = loop;

            #endregion

            #region UI

            if (!string.IsNullOrEmpty(jnElement["icon"]))
                icon = jnElement["icon"] != null ? spriteAssets != null && spriteAssets.TryGetValue(jnElement["icon"], out Sprite sprite) ? sprite : SpriteHelper.StringToSprite(jnElement["icon"]) : null;
            if (!string.IsNullOrEmpty(jnElement["icon_path"]))
                iconPath = RTFile.ParsePaths(jnElement["icon_path"]);
            if (jnElement["rect"] != null)
                rect = RectValues.TryParse(jnElement["rect"], RectValues.Default);
            if (jnElement["rounded"] != null)
                rounded = jnElement["rounded"].AsInt; // roundness can be prevented by setting rounded to 0.
            if (jnElement["rounded_side"] != null)
                roundedSide = (SpriteHelper.RoundedSide)jnElement["rounded_side"].AsInt; // default side should be Whole.
            if (jnElement["mask"] != null)
                mask = jnElement["mask"].AsBool;
            if (jnElement["reactive"] != null)
                reactiveSetting = ReactiveSetting.Parse(jnElement["reactive"], j);

            #endregion

            #region Color

            if (jnElement["col"] != null)
                color = jnElement["col"].AsInt;
            if (jnElement["opacity"] != null)
                opacity = jnElement["opacity"].AsFloat;
            if (jnElement["hue"] != null)
                hue = jnElement["hue"].AsFloat;
            if (jnElement["sat"] != null)
                sat = jnElement["sat"].AsFloat;
            if (jnElement["val"] != null)
                val = jnElement["val"].AsFloat;
            if (jnElement["override_col"] != null)
                overrideColor = LSColors.HexToColorAlpha(jnElement["override_col"]);
            useOverrideColor = jnElement["override_col"] != null;

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
        public float GetTransform(int type, int axis)
        {
            if (!gameObject || axis < 0 || axis > 2)
                return 0f;

            return type switch
            {
                0 => gameObject.transform.localPosition[axis],
                1 => gameObject.transform.localScale[axis],
                2 => gameObject.transform.localEulerAngles[axis],
                _ => 0f
            };
        }

        #endregion
    }
}
