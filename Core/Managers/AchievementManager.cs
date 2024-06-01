using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Managers
{
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager inst;

        public void Awake()
        {
            inst = this;
        }

        public void SetAchievement(string name)
        {
            if (!achievements.Any(x => x.Name == name))
            {
                CoreHelper.LogError($"No achievement of name {name}");
                return;
            }

            var achievement = achievements.First(x => x.Name == name);

            if (achievement)
                achievement.Unlock();
        }

        public bool GetAchievement(string name)
        {
            if (!achievements.Any(x => x.Name == name))
            {
                CoreHelper.LogError($"No achievement of name {name}");
                return false;
            }

            return achievements.First(x => x.Name == name);
        }

        public static List<Achievement> achievements = new List<Achievement>();

    }
}
