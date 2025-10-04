using UnityEngine;

namespace BetterLegacy.Core.Data
{
    public struct OutlineData
    {
        public OutlineData(Color color, int type, float width)
        {
            this.color = color;
            this.type = type;
            this.width = width;
        }

        public Color color;
        public int type;
        public float width;
    }
}
