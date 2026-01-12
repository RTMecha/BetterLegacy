
using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data
{
    public delegate bool AchievementFunction();

    /// <summary>
    /// Custom achievement class to be used for levels and the game.
    /// </summary>
    public class Achievement : PAObject<Achievement>
    {
        #region Constructors

        public Achievement()
        {
            id = GetNumberID();
        }

        public Achievement(string id, string name, string description, int difficulty, Sprite icon, bool hidden = false)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.difficulty = difficulty;
            this.icon = icon;
            this.hidden = hidden;
            unlocked = false;
        }

        public Achievement(string id, string name, string description, int difficulty, Sprite icon, bool hidden, string hint) : this(id, name, description, difficulty, icon, hidden)
        {
            this.hint = hint;
        }

        #endregion

        #region Values

        #region Constants

        /// <summary>
        /// The default hint.
        /// </summary>
        public const string DEFAULT_HINT = "Unlock this achievement to unhide it.";

        #endregion

        /// <summary>
        /// Name of the achievement.
        /// </summary>
        public string name = string.Empty;

        /// <summary>
        /// Short description on how to get the achievement (or not if achievement is hidden).
        /// </summary>
        public string description = string.Empty;

        /// <summary>
        /// Icon the achievement popup shows.
        /// </summary>
        public Sprite icon;

        /// <summary>
        /// Icon to display if the achievement is locked. Can be left as default.
        /// </summary>
        public Sprite lockedIcon;

        /// <summary>
        /// Difficulty of the achievement. Based on metadata difficulty.
        /// </summary>
        public int difficulty;

        /// <summary>
        /// If the achievement shows up in achievement lists when not unlocked.
        /// </summary>
        public bool hidden;

        /// <summary>
        /// Hint to display in achievement interfaces on how to get this achievement.
        /// </summary>
        public string hint = string.Empty;

        /// <summary>
        /// If the achievement has already been achieved or not.
        /// </summary>
        public bool unlocked;

        /// <summary>
        /// The metadata difficulty type.
        /// </summary>
        public DifficultyType DifficultyType { get => difficulty; set => difficulty = value; }

        /// <summary>
        /// If the achievements' unlocked state is shared across the game.
        /// </summary>
        public bool shared;

        #endregion

        #region Functions

        public override void CopyData(Achievement orig, bool newID = true)
        {
            id = newID ? GetNumberID() : orig.id;
            name = orig.name;
            description = orig.description;
            difficulty = orig.difficulty;
            icon = orig.icon;
            lockedIcon = orig.lockedIcon;
            hidden = orig.hidden;
            hint = orig.hint;
            unlocked = orig.unlocked;
            shared = orig.shared;
        }

        public override void ReadJSON(JSONNode jn)
        {
            id = jn["id"] ?? GetNumberID();
            name = jn["name"] ?? string.Empty;
            description = jn["desc"] ?? string.Empty;
            difficulty = jn["difficulty"].AsInt;
            if (jn["icon"] != null)
                icon = SpriteHelper.StringToSprite(jn["icon"]);
            if (jn["locked_icon"] != null)
                lockedIcon = SpriteHelper.StringToSprite(jn["locked_icon"]);
            if (jn["hint"] != null)
                hint = jn["hint"];
            hidden = jn["hidden"].AsBool;
            shared = jn["shared"].AsBool;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["id"] = id ?? GetNumberID();
            jn["name"] = name;
            if (!string.IsNullOrEmpty(description))
                jn["desc"] = description;
            if (difficulty != 0)
                jn["difficulty"] = difficulty;
            if (icon)
                jn["icon"] = SpriteHelper.SpriteToString(icon);
            if (lockedIcon)
                jn["locked_icon"] = SpriteHelper.SpriteToString(lockedIcon);
            if (hidden)
                jn["hidden"] = hidden;
            if (!string.IsNullOrEmpty(hint))
                jn["hint"] = hint;
            if (shared)
                jn["shared"] = shared;

            return jn;
        }

        /// <summary>
        /// Checks for the achievement's icon.
        /// </summary>
        /// <param name="file">File path to the icon.</param>
        public void CheckIconPath(string file)
        {
            if (RTFile.FileExists(file))
                icon = SpriteHelper.LoadSprite(file);
        }

        /// <summary>
        /// Checks for the achievement's locked icon.
        /// </summary>
        /// <param name="file">File path to the icon.</param>
        public void CheckLockedIconPath(string file)
        {
            if (RTFile.FileExists(file))
                lockedIcon = SpriteHelper.LoadSprite(file);
        }

        /// <summary>
        /// Gets the hint for how to unlock the achievement.
        /// </summary>
        /// <returns>Returns <see cref="hint"/> if it's not null or empty, otherwise returns <see cref="DEFAULT_HINT"/>.</returns>
        public string GetHint() => !string.IsNullOrEmpty(hint) ? hint : DEFAULT_HINT;

        public override string ToString() => $"{id} - {name} = {unlocked}";

        #endregion
    }
}
