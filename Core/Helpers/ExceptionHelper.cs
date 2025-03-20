using System;

namespace BetterLegacy.Core.Helpers
{
    public static class ExceptionHelper
    {
        public static void NullReference(object obj, string name)
        {
            if (obj == null)
                throw new NullReferenceException($"{name} is null.");
        }
    }
}
