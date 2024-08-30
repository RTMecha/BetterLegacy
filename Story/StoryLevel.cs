using BetterLegacy.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Video;

namespace BetterLegacy.Story
{
    public class StoryLevel : Level
    {
        public StoryLevel() : base()
        {
            isStory = true;
        }

        public string name;

        public string json;

        public string jsonPlayers;
        public VideoClip videoClip;
    }
}
