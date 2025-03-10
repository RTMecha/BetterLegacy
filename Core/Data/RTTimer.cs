using UnityEngine;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Represents a Unity timer.
    /// </summary>
    public struct RTTimer
    {
        public RTTimer(float time = 0f, float timeOffset = 0f, float offset = 0f)
        {
            this.time = time;
            this.timeOffset = timeOffset;
            this.offset = offset;
            UpdateTimeOffset();
        }

        /// <summary>
        /// Time since start.
        /// </summary>
        public float time;
        /// <summary>
        /// Offsets the timer.
        /// </summary>
        public float offset;

        float timeOffset;

        /// <summary>
        /// Resets the timer.
        /// </summary>
        public void UpdateTimeOffset() => timeOffset = Time.time;

        /// <summary>
        /// Updates the timer.
        /// </summary>
        public void Update() => time = Time.time - timeOffset + offset;

        public override string ToString() => time.ToString();
    }
}
