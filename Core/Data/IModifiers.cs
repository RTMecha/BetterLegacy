using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BetterLegacy.Core.Data.Beatmap;

namespace BetterLegacy.Core.Data
{
    public interface IModifiers<T>
    {
        /// <summary>
        /// The tags used to identify a group of objects or object properties.
        /// </summary>
        public List<string> Tags { get; set; }

        /// <summary>
        /// Modifiers the object contains.
        /// </summary>
        public List<Modifier<T>> Modifiers { get; set; }

        /// <summary>
        /// If modifiers ignore the lifespan restriction.
        /// </summary>
        public bool IgnoreLifespan { get; set; }

        /// <summary>
        /// If the order of triggers and actions matter.
        /// </summary>
        public bool OrderModifiers { get; set; }

        /// <summary>
        /// Variable set and used by modifiers.
        /// </summary>
        public int IntVariable { get; set; }

        /// <summary>
        /// If the modifiers are currently active.
        /// </summary>
        public bool ModifiersActive { get; }
    }
}
