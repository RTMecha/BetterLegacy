using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Core.Components.Player
{
    /// <summary>
    /// Represents a health UI image.
    /// </summary>
    public class HealthObject
    {
        #region Constructors

        public HealthObject(GameObject gameObject, Image image)
        {
            this.gameObject = gameObject;
            this.image = image;
        }

        #endregion

        #region Values

        /// <summary>
        /// Game object reference.
        /// </summary>
        public GameObject gameObject;

        /// <summary>
        /// Image reference.
        /// </summary>
        public Image image;

        #endregion
    }
}
