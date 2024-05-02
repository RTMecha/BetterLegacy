using System;

namespace BetterLegacy.Core.Animation
{
    public class NoKeyframeException : Exception
    {
        public NoKeyframeException(string message) : base(message)
        {
        }
    }
}
