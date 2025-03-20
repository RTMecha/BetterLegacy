using System;
using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Core.Animation;

namespace BetterLegacy.Core.Components
{
    /// <summary>
    /// Same as AnimationManager except not a global manager and can be used for any single instance case.
    /// </summary>
    public class AnimationController : MonoBehaviour
    {

        public List<RTAnimation> animations = new List<RTAnimation>();

        public float speed = 1f;

        public bool autoRun = true;

        void Update()
        {
            if (!autoRun)
                return;

            UpdateAnimations();
        }

        public void UpdateAnimations()
        {
            for (int i = 0; i < animations.Count; i++)
            {
                if (animations[i].playing)
                {
                    animations[i].globalSpeed = speed;
                    animations[i].Update();
                }
            }
        }

        /// <summary>
        /// Adds an animation to the update list and plays it.
        /// </summary>
        /// <param name="animation">The animation to play.</param>
        public void Play(RTAnimation animation)
        {
            if (!animations.Has(x => x.id == animation.id))
                animations.Add(animation);
            animation.Start();
        }

        /// <summary>
        /// Stops all the animations in the animation list and clears it.
        /// </summary>
        public void StopAll()
        {
            for (int i = 0; i < animations.Count; i++)
            {
                var anim = animations[i];
                anim.Stop();
            }
            animations.Clear();
        }

        #region Remove

        /// <summary>
        /// Removes all animations with a matching name.
        /// </summary>
        /// <param name="name">Name of the animations to remove.</param>
        public void RemoveName(string name) => animations.RemoveAll(x => x.name == name);

        /// <summary>
        /// Removes all animations with a matching ID.
        /// </summary>
        /// <param name="id">ID of the animations to remove.</param>
        public void Remove(string id) => animations.RemoveAll(x => x.id == id);

        /// <summary>
        /// Removes all animations via a predicate.
        /// </summary>
        /// <param name="predicate">Animations to match.</param>
        public void Remove(Predicate<RTAnimation> predicate) => animations.RemoveAll(predicate);

        #endregion

        #region Search methods

        /// <summary>
        /// Finds an animation with a matching name.
        /// </summary>
        /// <param name="name">Name of the animation to find.</param>
        /// <returns>Returns an animation if found, otherwise returns null.</returns>
        public RTAnimation FindAnimationByName(string name) => animations.Find(x => x.name == name);

        /// <summary>
        /// Finds an animation with a matching ID.
        /// </summary>
        /// <param name="id">ID of the animation to find.</param>
        /// <returns>Returns an animation if found, otherwise returns null.</returns>
        public RTAnimation FindAnimation(string id) => animations.Find(x => x.id == id);

        /// <summary>
        /// Finds a list of all animations with a matching name.
        /// </summary>
        /// <param name="name">Name of the animations to find.</param>
        /// <returns>Returns a list of animations.</returns>
        public List<RTAnimation> FindAnimationsByName(string name) => animations.FindAll(x => x.name == name);

        /// <summary>
        /// Finds a list of all animations with a matching ID.
        /// </summary>
        /// <param name="id">ID of the animations to find.</param>
        /// <returns>Returns a list of animations.</returns>
        public List<RTAnimation> FindAnimations(string id) => animations.FindAll(x => x.id == id);

        /// <summary>
        /// Finds a list of all animations via a predicate.
        /// </summary>
        /// <param name="predicate">Animation to match.</param>
        /// <returns>Returns an animation if found, otherwise returns null.</returns>
        public RTAnimation FindAnimation(Predicate<RTAnimation> predicate) => animations.Find(predicate);

        /// <summary>
        /// Finds a list of all animations via a predicate.
        /// </summary>
        /// <param name="predicate">Animations to match.</param>
        /// <returns>Returns a list of animations.</returns>
        public List<RTAnimation> FindAnimations(Predicate<RTAnimation> predicate) => animations.FindAll(predicate);

        /// <summary>
        /// Tries to find an animation with a matching name.
        /// </summary>
        /// <param name="name">Name of the animation to find.</param>
        /// <param name="animation">The animation result.</param>
        /// <returns>Returns true if an animation is found, otherwise false.</returns>
        public bool TryFindAnimationByName(string name, out RTAnimation animation) => animations.TryFind(x => x.name == name, out animation);

        /// <summary>
        /// Tries to find an animation with a matching ID.
        /// </summary>
        /// <param name="id">ID of the animation to find.</param>
        /// <param name="animation">The animation result.</param>
        /// <returns>Returns true if an animation is found, otherwise false.</returns>
        public bool TryFindAnimation(string id, out RTAnimation animation) => animations.TryFind(x => x.id == id, out animation);

        /// <summary>
        /// Tries to find all animations with a matching name.
        /// </summary>
        /// <param name="name">Name of the animation to find.</param>
        /// <param name="animations">The animations list result.</param>
        /// <returns>Returns true if an amount of animations are found, otherwise false.</returns>
        public bool TryFindAnimationsByName(string name, out List<RTAnimation> animations) => this.animations.TryFindAll(x => x.name == name, out animations);

        /// <summary>
        /// Tries to find all animations with a matching ID.
        /// </summary>
        /// <param name="id">ID of the animation to find.</param>
        /// <param name="animations">The animations list result.</param>
        /// <returns>Returns true if an amount of animations are found, otherwise false.</returns>
        public bool TryFindAnimations(string id, out List<RTAnimation> animations) => this.animations.TryFindAll(x => x.id == id, out animations);

        /// <summary>
        /// Tries to find an animation via a predicate.
        /// </summary>
        /// <param name="predicate">Animation to match.</param>
        /// <param name="animation">The animation result.</param>
        /// <returns>Returns true if an animation is found, otherwise false.</returns>
        public bool TryFindAnimation(Predicate<RTAnimation> predicate, out RTAnimation animation) => animations.TryFind(predicate, out animation);

        /// <summary>
        /// Tries to find all animations via a predicate.
        /// </summary>
        /// <param name="predicate">Animations to match.</param>
        /// <param name="animations">The animations list result.</param>
        /// <returns>Returns true if an amount of animations are found, otherwise false.</returns>
        public bool TryFindAnimations(Predicate<RTAnimation> predicate, out List<RTAnimation> animations) => this.animations.TryFindAll(predicate, out animations);

        #endregion
    }
}
