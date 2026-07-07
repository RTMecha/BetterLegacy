using SimpleJSON;

using BetterLegacy.Core.Data.Network;

namespace BetterLegacy.Menus.UI
{
    /// <summary>
    /// Class that manages reactiveness to audio.
    /// </summary>
    public struct ReactiveSetting : IPacket
    {
        #region Values

        /// <summary>
        /// Reactive is enabled.
        /// </summary>
        public bool init;
        /// <summary>
        /// What property should react to the audio.
        /// </summary>
        public ControlType controls;
        /// <summary>
        /// Sample channels that should be used.
        /// </summary>
        public int[] channels;
        /// <summary>
        /// Reactive intensity.
        /// </summary>
        public float[] intensity;

        #endregion

        #region Functions

        /// <summary>
        /// Parses a <see cref="ReactiveSetting"/> from JSON.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <param name="offset">Channel offset.</param>
        /// <returns>Returns a parsed <see cref="ReactiveSetting"/>.</returns>
        public static ReactiveSetting Parse(JSONNode jn, int offset)
        {
            if (jn == null || jn["channels"] == null || jn["intensity"] == null || jn["controls"] == null)
                return default;

            var reactiveSetting = new ReactiveSetting()
            {
                init = true,
                channels = new int[jn["channels"].Count],
                intensity = new float[jn["intensity"].Count],
                controls = (ControlType)jn["controls"].AsInt,
            };

            var increaseLoop = jn["increase_loop"].AsBool;

            for (int i = 0; i < jn["channels"].Count; i++)
                reactiveSetting.channels[i] = jn["channels"][i].AsInt + (increaseLoop ? offset : 0);
            for (int i = 0; i < jn["intensity"].Count; i++)
                reactiveSetting.intensity[i] = jn["intensity"][i].AsFloat;

            return reactiveSetting;
        }

        public void ReadPacket(NetworkReader reader)
        {
            init = reader.ReadBoolean();
            controls = (ControlType)reader.ReadByte();
            channels = new int[reader.ReadInt32()];
            for (int i = 0; i < channels.Length; i++)
                channels[i] = reader.ReadInt32();
            intensity = new float[reader.ReadInt32()];
            for (int i = 0; i < intensity.Length; i++)
                intensity[i] = reader.ReadSingle();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(init);
            writer.Write((byte)controls);
            writer.Write(channels.Length);
            for (int i = 0; i < channels.Length; i++)
                writer.Write(channels[i]);
            writer.Write(intensity.Length);
            for (int i = 0; i < intensity.Length; i++)
                writer.Write(intensity[i]);
        }

        #endregion

        #region Sub Classes

        /// <summary>
        /// What property should react to the audio.
        /// </summary>
        public enum ControlType
        {
            /// <summary>
            /// Position reacts to audio.
            /// </summary>
            Position,
            /// <summary>
            /// Scale reacts to audio.
            /// </summary>
            Scale,
            /// <summary>
            /// Rotation reacts to audio.
            /// </summary>
            Rotation,
            /// <summary>
            /// Color reacts to audio.
            /// </summary>
            Color,
        }

        #endregion
    }
}
