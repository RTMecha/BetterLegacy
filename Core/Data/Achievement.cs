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
    public struct Achievement
    {
        public Achievement(string name, string description, int difficulty, Sprite icon, AchievementFunction requirement, bool hidden = false)
        {
            Name = name;
            Description = description;
            Difficulty = difficulty;
            Icon = icon;
            Requirement = requirement;
            Hidden = hidden;
            unlocked = false;
        }

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
        public DataManager.Difficulty DifficultyType => Difficulty == 0 ? DataManager.inst.difficulties.Last() : DataManager.inst.difficulties[Difficulty - 1];

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

        public static Achievement Parse(JSONNode jn)
        {
            var icon = jn["icon"];
            byte[] imageData = new byte[icon.Count];
            for (int j = 0; j < icon.Count; j++)
                imageData[j] = (byte)icon[j].AsInt;
            
            return new Achievement(jn["name"], jn["desc"], jn["difficulty"].AsInt, SpriteManager.LoadSprite(imageData), null, jn["hidden"].AsBool);
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            jn["name"] = Name;
            jn["desc"] = Description;
            jn["difficulty"] = Difficulty.ToString();
            jn["hidden"] = Hidden.ToString();

            var imageData = Icon.texture.EncodeToPNG();
            for (int j = 0; j < imageData.Length; j++)
                jn["icon"][j] = imageData[j];

            return jn;
        }

        public static implicit operator bool(Achievement achievement) => achievement.Requirement?.Invoke() == true;

        public static Achievement TestAchievement => new Achievement("Test", "Test this achievement!", 0, null, delegate ()
        {
            return true;
        });
    }
}
