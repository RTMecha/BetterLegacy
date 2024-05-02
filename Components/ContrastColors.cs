using LSFunctions;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Components
{
    public class ContrastColors : MonoBehaviour
    {
        public Graphic Graphic { get; set; }
        public Graphic BaseGraphic { get; set; }

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
