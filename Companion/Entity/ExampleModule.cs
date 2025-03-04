using BetterLegacy.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Companion.Entity
{
    public abstract class ExampleModule : Exists
    {
        public abstract void InitDefault();

        public void SetReference(Example reference) => this.reference = reference;
        public Example reference;

        /// <summary>
        /// Builds the module.
        /// </summary>
        public abstract void Build();
        /// <summary>
        /// Runs the module per-tick.
        /// </summary>
        public abstract void Tick();
        /// <summary>
        /// Clears the module.
        /// </summary>
        public abstract void Clear();

        #region Attributes

        /// <summary>
        /// List of the module's attributes.
        /// </summary>
        public List<ExampleAttribute> attributes = new List<ExampleAttribute>();

        /// <summary>
        /// Adds an attribute to the module.
        /// </summary>
        /// <param name="id">ID of the attribute.</param>
        /// <param name="value">Default value.</param>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        /// <returns>If an attribute with the same ID is found, return the attribute, otherwise returns a new <see cref="ExampleAttribute"/>.</returns>
        public ExampleAttribute AddAttribute(string id, double value, double min, double max) => AddAttribute(id, value, min, max, false);

        /// <summary>
        /// Adds an attribute to the module.
        /// </summary>
        /// <param name="id">ID of the attribute.</param>
        /// <param name="value">Default value.</param>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        /// <param name="integer">If the value snaps to whole numbers.</param>
        /// <returns>If an attribute with the same ID is found, return the attribute, otherwise returns a new <see cref="ExampleAttribute"/>.</returns>
        public ExampleAttribute AddAttribute(string id, double value, double min, double max, bool integer)
        {
            var attribute = GetAttribute(id, value, min, max, ExampleAttributeRetrieval.Nothing);

            if (!attribute)
            {
                attribute = new ExampleAttribute(id, value, min, max, integer);
                attributes.Add(attribute);
            }

            return attribute;
        }

        /// <summary>
        /// Gets an attribute by its ID.
        /// </summary>
        /// <param name="id">ID to search an attribute for.</param>
        /// <returns>Returns a <see cref="ExampleAttribute"/> based on the ID. If the attribute wasn't found in <see cref="attributes"/>, then add the attribute and returns the new attribute.</returns>
        public ExampleAttribute GetAttribute(string id) => GetAttribute(id, 0.0, 0.0, 0.0, ExampleAttributeRetrieval.Add);

        /// <summary>
        /// Gets an attribute by its ID.
        /// </summary>
        /// <param name="id">ID to search an attribute for.</param>
        /// <param name="value">Default value if no attribute was found.</param>
        /// <param name="min">Default minimum if no attribute was found.</param>
        /// <param name="max">Default maximum if no attribute was found.</param>
        /// <returns>Returns a <see cref="ExampleAttribute"/> based on the ID. If the attribute wasn't found in <see cref="attributes"/>, then add the attribute and returns the new attribute.</returns>
        public ExampleAttribute GetAttribute(string id, double value, double min, double max) => GetAttribute(id, value, min, max, ExampleAttributeRetrieval.Add);

        /// <summary>
        /// Gets an attribute by its ID.
        /// </summary>
        /// <param name="id">ID to search an attribute for.</param>
        /// <param name="value">Default value if no attribute was found.</param>
        /// <param name="min">Default minimum if no attribute was found.</param>
        /// <param name="max">Default maximum if no attribute was found.</param>
        /// <param name="behavior">How non-existant attributes should be handled.</param>
        /// <returns>Returns a <see cref="ExampleAttribute"/> based on the ID. If the attribute wasn't found in <see cref="attributes"/>, then use the <paramref name="behavior"/>.</returns>
        public ExampleAttribute GetAttribute(string id, double value, double min, double max, ExampleAttributeRetrieval behavior) => GetAttribute(id, value, min, max, false, behavior);

        /// <summary>
        /// Gets an attribute by its ID.
        /// </summary>
        /// <param name="id">ID to search an attribute for.</param>
        /// <param name="value">Default value if no attribute was found.</param>
        /// <param name="min">Default minimum if no attribute was found.</param>
        /// <param name="max">Default maximum if no attribute was found.</param>
        /// <param name="integer">If the value snaps to whole numbers.</param>
        /// <param name="behavior">How non-existant attributes should be handled.</param>
        /// <returns>Returns a <see cref="ExampleAttribute"/> based on the ID. If the attribute wasn't found in <see cref="attributes"/>, then use the <paramref name="behavior"/>.</returns>
        public ExampleAttribute GetAttribute(string id, double value, double min, double max, bool integer, ExampleAttributeRetrieval behavior)
        {
            var attribute = attributes.Find(x => x.id == id);
            if (!attribute)
            {
                switch (behavior)
                {
                    case ExampleAttributeRetrieval.Throw: throw new NullReferenceException("Attribute is null.");
                    case ExampleAttributeRetrieval.Add:
                        {
                            attribute = new ExampleAttribute(id, value, min, max, integer);
                            attributes.Add(attribute);
                            break;
                        }
                }
            }

            return attribute;
        }

        #endregion
    }
}
