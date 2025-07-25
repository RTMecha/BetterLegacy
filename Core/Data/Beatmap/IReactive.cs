using UnityEngine;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Indicates an object can react to the audio.
    /// </summary>
    public interface IReactive
    {
        /// <summary>
        /// Position reacts to the audio.
        /// </summary>
        public Vector3 ReactivePositionOffset { get; set; }
        /// <summary>
        /// Scale reacts to the audio.
        /// </summary>
        public Vector3 ReactiveScaleOffset { get; set; }
        /// <summary>
        /// Rotation reacts to the audio.
        /// </summary>
        public float ReactiveRotationOffset { get; set; }
    }
}
