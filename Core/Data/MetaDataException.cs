using System;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Exception related to <see cref="MetaData"/>.
    /// </summary>
    public class MetaDataException : Exception
    {
        public MetaDataException(string message) : base(message) { }
    }
}
