using LSFunctions;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Components
{
    /// <summary>
    /// Contrasts a UI element with its base.
    /// </summary>
    public class ContrastColors : MonoBehaviour
    {
        /// <summary>
        /// Graphic to assign a contrasted color to.
        /// </summary>
        public Graphic Graphic { get; set; }

        /// <summary>
        /// Graphic to contrast.
        /// </summary>
        public Graphic BaseGraphic { get; set; }

        /// <summary>
        /// Assigns a graphic and base graphic for contrasting.
        /// </summary>
        /// <param name="graphic">Graphic to assign a contrasted color to.</param>
        /// <param name="baseGraphic">Graphic to contrast.</param>
        public void Init(Graphic graphic, Graphic baseGraphic)
        {
            Graphic = graphic;
            BaseGraphic = baseGraphic;
        }

        void Update()
        {
            if (Graphic && BaseGraphic)
                Graphic.color = LSColors.ContrastColor(BaseGraphic.color);
        }
    }
}
