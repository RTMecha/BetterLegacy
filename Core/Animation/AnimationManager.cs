using System.Collections.Generic;

using UnityEngine;

namespace BetterLegacy.Core.Animation
{
    /// <summary>
    /// Manager class that plays and updates custom animations.
    /// <br></br>All animation code is based on https://github.com/Reimnop/Catalyst
    /// </summary>
    public class AnimationManager : MonoBehaviour, IAnimationController
    {
        #region Init

        /// <summary>
        /// The <see cref="AnimationManager"/> global instance reference.
        /// </summary>
        public static AnimationManager inst;

        /// <summary>
        /// Initializes <see cref="AnimationManager"/>.
        /// </summary>
        public static void Init() => Creator.NewGameObject(nameof(AnimationManager), SystemManager.inst.transform).AddComponent<AnimationManager>();

        void Awake() => inst = this;

        #endregion

        public List<RTAnimation> animations = new List<RTAnimation>();
        public List<RTAnimation> Animations { get => animations; set => animations = value; }

        public float speed = 1f;
        public float Speed { get => speed; set => speed = value; }

        void Update() => this.UpdateAnimations();
    }
}
