using System;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Exception related to <see cref="GameData"/>.
    /// </summary>
    public class GameDataException : Exception
    {
        public GameDataException(string message) : base(message) { }
    }
}
