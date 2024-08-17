using System.Collections.Generic;

using UnityEngine;

namespace BetterLegacy.Core.Animation
{
    /// <summary>
    /// All animation code is based on https://github.com/Reimnop/Catalyst
    /// </summary>
    public class AnimationManager : MonoBehaviour
    {
        public static AnimationManager inst;

        public static void Init() => Creator.NewGameObject(nameof(AnimationManager), SystemManager.inst.transform).AddComponent<AnimationManager>();

        void Awake() => inst = this;

        void Update()
        {
            for (int i = 0; i < animations.Count; i++)
            {
                if (animations[i].playing)
                    animations[i].Update();
            }
        }

        public void Play(RTAnimation animation)
        {
            if (!animations.Has(x => x.id == animation.id))
                animations.Add(animation);
            animation.ResetTime();
            animation.Play();
        }

        public void RemoveName(string name) => animations.RemoveAll(x => x.name == name);

        public void RemoveID(string id) => animations.RemoveAll(x => x.id == id);

        public List<RTAnimation> animations = new List<RTAnimation>();
    }
}
