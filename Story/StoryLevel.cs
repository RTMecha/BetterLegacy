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
    /// <summary>
    /// Stores data to be used for playing levels in the story mode. Level does not need to have a full path, it can be purely an asset.
    /// </summary>
    public class StoryLevel : Level
    {
        public StoryLevel() : base()
        {
            isStory = true;
        }

        /// <summary>
        /// Name of the story level.
        /// </summary>
        public string name;

        /// <summary>
        /// The full level.lsb JSON.
        /// </summary>
        public string json;

        /// <summary>
        /// The players.lsb JSON.
        /// </summary>
        public string jsonPlayers;

        /// <summary>
        /// Used for any cases where we want to play a VideoClip to showcase the Video BG feature.
        /// </summary>
        public VideoClip videoClip;
    }
}
