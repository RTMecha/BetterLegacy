using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
