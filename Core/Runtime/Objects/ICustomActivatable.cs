using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Core.Runtime.Objects
{
    /// <summary>
    /// Indicates a runtime object can be dynamically enabled / disabled.
    /// </summary>
    public interface ICustomActivatable
    {
        /// <summary>
        /// Sets the runtime objects' custom active state.
        /// </summary>
        /// <param name="active">Active state.</param>
        public void SetCustomActive(bool active);
    }
}
