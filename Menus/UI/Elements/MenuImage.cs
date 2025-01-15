using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Helpers;
using SimpleJSON;
using BetterLegacy.Core;
using BetterLegacy.Configs;
using BetterLegacy.Core.Managers;
using LSFunctions;
using System.IO;
using BetterLegacy.Menus.UI.Interfaces;
using BetterLegacy.Core.Data;
using BetterLegacy.Story;
using BetterLegacy.Menus.UI.Layouts;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Arcade.Interfaces;
using BetterLegacy.Core.Components;

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
            funcJSON = orig.funcJSON, // function to run when the element is clicked.
            onScrollUpFuncJSON = orig.onScrollUpFuncJSON,
            onScrollDownFuncJSON = orig.onScrollDownFuncJSON,
            spawnFuncJSON = orig.spawnFuncJSON, // function to run when the element spawns.
            onWaitEndFuncJSON = orig.onWaitEndFuncJSON,
            func = orig.func,
            onScrollUpFunc = orig.onScrollUpFunc,
            onScrollDownFunc = orig.onScrollDownFunc,
            spawnFunc = orig.spawnFunc,
            onWaitEndFunc = orig.onWaitEndFunc,

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

        public string ParseText(string input)
        {
            RTString.RegexMatches(input, new Regex(@"{{LevelRank=([0-9]+)}}"), match =>
            {
                DataManager.LevelRank levelRank =
                    LevelManager.Levels.TryFind(x => x.id == match.Groups[1].ToString(), out Level level) ? LevelManager.GetLevelRank(level) :
                    CoreHelper.InEditor ?
                        LevelManager.EditorRank :
                        DataManager.inst.levelRanks[0];

                input = input.Replace(match.Groups[0].ToString(), RTString.FormatLevelRank(levelRank));
            });

            RTString.RegexMatches(input, new Regex(@"{{StoryLevelRank=([0-9]+)}}"), match =>
            {
                DataManager.LevelRank levelRank =
                    StoryManager.inst.Saves.TryFind(x => x.ID == match.Groups[1].ToString(), out PlayerData playerData) ? LevelManager.GetLevelRank(playerData) :
                    CoreHelper.InEditor ?
                        LevelManager.EditorRank :
                        DataManager.inst.levelRanks[0];

                input = input.Replace(match.Groups[0].ToString(), RTString.FormatLevelRank(levelRank));
            });

            RTString.RegexMatches(input, new Regex(@"{{LoadStoryString=(.*?),(.*?)}}"), match =>
            {
                input = input.Replace(match.Groups[0].ToString(), StoryManager.inst.LoadString(match.Groups[1].ToString(), match.Groups[2].ToString()));
            });

            RTString.RegexMatches(input, new Regex(@"{{RandomNumber=([0-9]+)}}"), match =>
            {
                input = input.Replace(match.Groups[0].ToString(), LSText.randomNumString(Parser.TryParse(match.Groups[1].ToString(), 0)));
            });

            RTString.RegexMatches(input, new Regex(@"{{RandomText=([0-9]+)}}"), match =>
            {
                input = input.Replace(match.Groups[0].ToString(), LSText.randomString(Parser.TryParse(match.Groups[1].ToString(), 0)));
            });

            return input
                .Replace("{{CurrentPlayingChapterNumber}}", RTString.ToStoryNumber(StoryManager.inst.currentPlayingChapterIndex))
                .Replace("{{CurrentPlayingLevelNumber}}", RTString.ToStoryNumber(StoryManager.inst.currentPlayingLevelSequenceIndex))
                .Replace("{{SaveSlotNumber}}", RTString.ToStoryNumber(StoryManager.inst.SaveSlot))
                ;
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
            if (jnElement["on_scroll_up_func"] != null)
                onScrollUpFuncJSON = jnElement["on_scroll_up_func"]; // function to run when the element is scrolled on.
            if (jnElement["on_scroll_down_func"] != null)
                onScrollDownFuncJSON = jnElement["on_scroll_down_func"]; // function to run when the element is scrolled on.
            if (jnElement["spawn_func"] != null)
                spawnFuncJSON = jnElement["spawn_func"]; // function to run when the element spawns.
            if (jnElement["on_wait_end_func"] != null)
                onWaitEndFuncJSON = jnElement["on_wait_end_func"];

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

        public static implicit operator bool(MenuImage element) => element != null;
    }
}
