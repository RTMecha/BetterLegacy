using System.Collections.Generic;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Indicates an object can be parented to a <see cref="BeatmapObject"/>.
    /// </summary>
    public interface IParentable
    {
        /// <summary>
        /// ID of the parentable.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// ID of the object to parent this to. This value is not saved and is temporarily used for swapping parents.
        /// </summary>
        public string CustomParent { get; set; }

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

        /// <summary>
        /// Offset to animate the desync at.
        /// </summary>
        public float ParentDesyncOffset { get; set; }

        /// <summary>
        /// If the object should stop following the parent chain dynamically.
        /// </summary>
        public bool ParentDetatched { get; set; }

        /// <summary>
        /// Cached parent reference.
        /// </summary>
        public BeatmapObject CachedParent { get; set; }

        /// <summary>
        /// Tries to set an objects' parent. If the parent the user is trying to assign an object to a child of the object, then don't set parent.
        /// </summary>
        /// <param name="beatmapObjectToParentTo">Object to try parenting to.</param>
        /// <param name="renderParent">If parent editor should render.</param>
        /// <returns>Returns true if the <see cref="BeatmapObject"/> was successfully parented, otherwise returns false.</returns>
        public bool TrySetParent(string beatmapObjectToParentTo, bool renderParent = true);

        /// <summary>
        /// Checks if another object can be parented to this object.
        /// </summary>
        /// <param name="obj">Object to check the parent compatibility of.</param>
        /// <param name="beatmapObjects">Objects to search through.</param>
        /// <returns>Returns true if <paramref name="obj"/> can be parented to this.</returns>
        public bool CanParent(BeatmapObject obj, List<BeatmapObject> beatmapObjects);

        /// <summary>
        /// Updates the runtime objects' parent chain.
        /// </summary>
        public void UpdateParentChain();
    }
}
