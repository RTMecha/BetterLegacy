using BetterLegacy.Core.Helpers;
using LSFunctions;
using SimpleJSON;
using System.Linq;
using UnityEngine;

namespace BetterLegacy.Core.Data
{
    public delegate bool AchievementFunction();

    /// <summary>
    /// Custom achievement class to be used for levels and the game.
    /// </summary>
    public class Achievement
    {
        public Achievement(string id, string name, string description, int difficulty, Sprite icon, bool hidden = false)
        {
            ID = id;
            Name = name;
            Description = description;
            Difficulty = difficulty;
            Icon = icon;
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

            achievement.Unlock();
        }

        public static Achievement DeepCopy(Achievement orig, bool newID = true) => new Achievement(newID ? LSText.randomNumString(16) : orig.ID, orig.Name, orig.Description, orig.Difficulty, orig.Icon, orig.Hidden);

        public static Achievement Parse(JSONNode jn, bool parseUnlock = false)
            => new Achievement(jn["id"], jn["name"], jn["desc"], jn["difficulty"].AsInt, SpriteHelper.StringToSprite(jn["icon"]), jn["hidden"].AsBool)
            {
                unlocked = parseUnlock && jn["unlocked"] != null && jn["unlocked"].AsBool,
            };

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

            jn["icon"] = SpriteHelper.SpriteToString(Icon);

            return jn;
        }

        public static Achievement TestAchievement => new Achievement("265265", "Test", "Test this achievement!", 3, LegacyPlugin.AtanPlaceholder);

        public override string ToString() => $"{ID} - {Name} = {unlocked}";
    }
}
