﻿using System.Linq;

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

        /// <summary>
        /// Name of the achievement.
        /// </summary>
        public string name;

        /// <summary>
        /// Short description on how to get the achievement (or not if achievement is hidden).
        /// </summary>
        public string description;

        /// <summary>
        /// Icon the achievement popup shows.
        /// </summary>
        public Sprite icon;

        /// <summary>
        /// Difficulty of the achievement. Based on metadata difficulty.
        /// </summary>
        public int difficulty;

        /// <summary>
        /// If the achievement shows up in achievement lists when not unlocked.
        /// </summary>
        public bool hidden;

        /// <summary>
        /// If the achievement has already been achieved or not.
        /// </summary>
        public bool unlocked;

        /// <summary>
        /// The metadata difficulty type.
        /// </summary>
        public DifficultyType DifficultyType => difficulty == 0 ? CustomEnumHelper.GetCount<DifficultyType>() - 1 : difficulty - 1;

        public void Unlock()
        {
            if (unlocked)
                return;

            unlocked = true;
            CoreHelper.Log($"{name} achieved!");
        }

        public void Lock()
        {
            if (!unlocked)
                return;

            unlocked = false;
        }

        public override void CopyData(Achievement orig, bool newID = true)
        {
            id = newID ? GetNumberID() : orig.id;
            name = orig.name;
            description = orig.description;
            difficulty = orig.difficulty;
            icon = orig.icon;
            hidden = orig.hidden;
            unlocked = orig.unlocked;
        }

        public override void ReadJSON(JSONNode jn)
        {
            id = jn["id"];
            name = jn["name"];
            difficulty = jn["difficulty"].AsInt;
            icon = SpriteHelper.StringToSprite(jn["icon"]);
            hidden = jn["hidden"].AsBool;
        }

        public override JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            jn["id"] = id;
            jn["name"] = name;
            jn["desc"] = description;
            jn["difficulty"] = difficulty.ToString();
            jn["hidden"] = hidden.ToString();

            jn["icon"] = SpriteHelper.SpriteToString(icon);

            return jn;
        }

        public static Achievement TestAchievement => new Achievement("265265", "Test", "Test this achievement!", 3, LegacyPlugin.AtanPlaceholder);

        public override string ToString() => $"{id} - {name} = {unlocked}";
    }
}
