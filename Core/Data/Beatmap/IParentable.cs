using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Indicates an object can be parented to a <see cref="BeatmapObject"/>.
    /// </summary>
    public interface IParentable
    {
        /// <summary>
        /// ID of the object to parent all spawned base objects to.
        /// </summary>
        public string Parent { get; set; }

        /// <summary>
        /// Parent delay values.
        /// </summary>
        public float[] ParentOffsets { get; set; }

        /// <summary>
        /// Parent toggle values.
        /// </summary>
        public string ParentType { get; set; }

        /// <summary>
        /// Multiplies from the parents' position, allowing for parallaxing.
        /// </summary>
        public float[] ParentParallax { get; set; }

        /// <summary>
        /// If parent chains should be accounted for when parent offset / delay is used.
        /// </summary>
        public string ParentAdditive { get; set; }

        /// <summary>
        /// If the object should stop following the parent chain after spawn.
        /// </summary>
        public bool ParentDesync { get; set; }
    }
}
