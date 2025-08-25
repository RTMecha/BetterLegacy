using System.Collections.Generic;

namespace BetterLegacy.Core.Data.Modifiers
{
    /// <summary>
    /// Indicates an object can run modifiers.
    /// </summary>
    /// <typeparam name="T">Type of the object that can run modifiers.</typeparam>
    public interface IModifyable
    {
        /// <summary>
        /// The reference type of the modifyable.
        /// </summary>
        public ModifierReferenceType ReferenceType { get; }

        /// <summary>
        /// The tags used to identify a group of objects or object properties.
        /// </summary>
        public List<string> Tags { get; set; }

        /// <summary>
        /// Modifiers the object contains.
        /// </summary>
        public List<Modifier> Modifiers { get; set; }

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
