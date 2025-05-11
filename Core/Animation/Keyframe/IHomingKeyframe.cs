using UnityEngine;

namespace BetterLegacy.Core.Animation.Keyframe
{
    public interface IHomingKeyframe
    {
        public Vector3 GetPosition();
        public Vector3 GetPosition(float time);
        public Vector3 Target { get; set; }
    }
}
