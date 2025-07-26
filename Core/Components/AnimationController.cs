using System;
using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Core.Animation;

namespace BetterLegacy.Core.Components
{
    /// <summary>
    /// Same as AnimationManager except not a global manager and can be used for any single instance case.
    /// </summary>
    public class AnimationController : MonoBehaviour, IAnimationController
    {
        public List<RTAnimation> animations = new List<RTAnimation>();
        public List<RTAnimation> Animations { get => animations; set => animations = value; }

        public float speed = 1f;
        public float Speed { get => speed; set => speed = value; }

        /// <summary>
        /// If the animation controller should run per tick via the Update method.
        /// </summary>
        public bool autoRun = true;

        void Update()
        {
            if (!autoRun)
                return;

            this.UpdateAnimations();
        }
    }
}
