using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Companion.Entity
{
    /// <summary>
    /// How not found attributes should be handled.
    /// </summary>
    public enum ExampleAttributeRetrieval
    {
        /// <summary>
        /// Throws a <see cref="NullReferenceException"/>.
        /// </summary>
        Throw,
        /// <summary>
        /// Adds a new attribute.
        /// </summary>
        Add,
        /// <summary>
        /// Returns a null reference.
        /// </summary>
        Nothing,
    }
}
