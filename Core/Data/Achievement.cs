using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BetterLegacy.Core.Data
{
    public delegate bool AchievementFunction();

    /// <summary>
    /// Custom achievement class to be used for levels and the game.
    /// </summary>
    public class Achievement
    {
        public Achievement(string id, string name, string description, int difficulty, Sprite icon, AchievementFunction requirement, bool hidden = false)
        {
            ID = id;
            Name = name;
            Description = description;
            Difficulty = difficulty;
            Icon = icon;
            Requirement = requirement;
            Hidden = hidden;
            unlocked = false;
        }

        public string ID { get; set; }

        /// <summary>
        /// Name of the achievement.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Short description on how to get the achievement (or not if achievement is hidden).
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Icon the achievement popup shows.
        /// </summary>
        public Sprite Icon { get; set; }

        /// <summary>
        /// Difficulty of the achievement. Based on metadata difficulty.
        /// </summary>
        public int Difficulty { get; set; }

        /// <summary>
        /// The requirement index from the requirements list.
        /// </summary>
        public int RequirementIndex { get; set; }

        /// <summary>
        /// The delegate requirement of the achievement.
        /// </summary>
        public AchievementFunction Requirement { get; set; }

        /// <summary>
        /// If the achievement shows up in achievement lists when not unlocked.
        /// </summary>
        public bool Hidden { get; set; }

        /// <summary>
        /// If the achievement has already been achieved or not.
        /// </summary>
        public bool unlocked;

        /// <summary>
        /// The metadata difficulty type.
        /// </summary>
        public DataManager.Difficulty DifficultyType => Difficulty == 0 ? DataManager.inst.difficulties.Last() : Difficulty - 1 >= 0 && Difficulty - 1 < DataManager.inst.difficulties.Count ? DataManager.inst.difficulties[Difficulty - 1] : new DataManager.Difficulty("Unknown Difficulty", Color.red);

        public void AssignRequirement(int index)
        {
            RequirementIndex = index;
            Requirement = AchievementManager.requirements[Mathf.Clamp(index, 0, AchievementManager.requirements.Count - 1)];
        }

        public void Unlock()
        {
            if (unlocked)
                return;

            unlocked = true;
            CoreHelper.Log($"{Name} achieved!");
        }

        public void Lock()
        {
            if (!unlocked)
                return;

            unlocked = false;
        }

        public static void Test()
        {
            var achievement = TestAchievement;

            if (achievement)
                achievement.Unlock();
        }

        public static Achievement Parse(JSONNode jn, bool parseUnlock = false)
        {
            var requirement = AchievementManager.requirements[jn["requirement"] == null ? 0 : Mathf.Clamp(jn["requirement"].AsInt, 0, AchievementManager.requirements.Count - 1)];

            return new Achievement(jn["id"], jn["name"], jn["desc"], jn["difficulty"].AsInt, SpriteManager.StringToSprite(jn["icon"]), requirement, jn["hidden"].AsBool)
            {
                unlocked = parseUnlock && jn["unlocked"] != null && jn["unlocked"].AsBool,
            };
        }

        public JSONNode ToJSON(bool saveUnlock = false)
        {
            var jn = JSON.Parse("{}");

            jn["id"] = ID;
            jn["name"] = Name;
            jn["desc"] = Description;
            jn["difficulty"] = Difficulty.ToString();
            jn["hidden"] = Hidden.ToString();
            if (saveUnlock)
                jn["unlocked"] = unlocked.ToString();

            if (RequirementIndex != 0)
                jn["requirement"] = RequirementIndex.ToString();

            jn["icon"] = SpriteManager.SpriteToString(Icon);

            return jn;
        }

        public static implicit operator bool(Achievement achievement) => achievement.Requirement?.Invoke() == true;

        public static Achievement TestAchievement => new Achievement("0", "Test", "Test this achievement!", 3, LegacyPlugin.AtanPlaceholder, delegate ()
        {
            return true;
        });
    }
}
