using UnityEngine;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Indicates an object can be dynamically transformed.
    /// </summary>
    public interface ITransformable
    {
        /// <summary>
        /// Dynamic position offset.
        /// </summary>
        public Vector3 PositionOffset { get; set; }

        /// <summary>
        /// Dynamic scale offset.
        /// </summary>
        public Vector3 ScaleOffset { get; set; }

        /// <summary>
        /// Dynamic rotation offset.
        /// </summary>
        public Vector3 RotationOffset { get; set; }

        /// <summary>
        /// Resets the transform offsets.
        /// </summary>
        public void ResetOffsets();

        /// <summary>
        /// Gets a transform offset from the object.
        /// </summary>
        /// <param name="type">
        /// The type of transform value to get.<br></br>
        /// 0 -> <see cref="positionOffset"/><br></br>
        /// 1 -> <see cref="scaleOffset"/><br></br>
        /// 2 -> <see cref="rotationOffset"/>
        /// </param>
        /// <returns>Returns a transform offset.</returns>
        public Vector3 GetTransformOffset(int type);

        /// <summary>
        /// Sets a transform offset of the object.
        /// </summary>
        /// <param name="type">
        /// The type of transform value to get.<br></br>
        /// 0 -> <see cref="positionOffset"/><br></br>
        /// 1 -> <see cref="scaleOffset"/><br></br>
        /// 2 -> <see cref="rotationOffset"/>
        /// </param>
        /// <param name="value">Value to assign to the offset.</param>
        public void SetTransform(int type, Vector3 value);

        /// <summary>
        /// Sets a transform offset of the object.
        /// </summary>
        /// <param name="type">
        /// The type of transform value to get.<br></br>
        /// 0 -> <see cref="positionOffset"/><br></br>
        /// 1 -> <see cref="scaleOffset"/><br></br>
        /// 2 -> <see cref="rotationOffset"/>
        /// </param>
        /// <param name="axis">The axis of the transform value to get.</param>
        /// <param name="value">Value to assign to the offset's axis.</param>
        public void SetTransform(int type, int axis, float value);
    }
}
