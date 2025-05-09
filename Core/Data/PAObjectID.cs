using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LSFunctions;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Base PA object with ID.
    /// </summary>
    public class PAObjectID : Exists
    {
        public PAObjectID() => id = LSText.randomString(16);

        /// <summary>
        /// Identification of the object.
        /// </summary>
        public string id;

        /// <summary>
        /// Creates a 16 length string ID.
        /// </summary>
        /// <returns>Returns a string ID of 16 length.</returns>
        public static string GetStringID() => LSText.randomString(16);

        /// <summary>
        /// Creates a 16 length number ID.
        /// </summary>
        /// <returns>Returns a number ID of 16 length.</returns>
        public static string GetNumberID() => LSText.randomNumString(16);

        /// <summary>
        /// Creates a 8 length number ID.
        /// </summary>
        /// <returns>Returns a number ID of 8 length.</returns>
        public static string GetShortNumberID() => LSText.randomNumString(8);
    }
}
