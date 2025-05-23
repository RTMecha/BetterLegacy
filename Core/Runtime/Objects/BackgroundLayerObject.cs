using UnityEngine;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Runtime.Objects
{
    public class BackgroundLayerObject : Exists
    {
        public BackgroundLayerObject() { }

        public GameObject gameObject;

        public BackgroundLayer backgroundLayer;

        public void Clear()
        {
            if (gameObject)
                CoreHelper.Destroy(gameObject);
            backgroundLayer = null;
        }
    }
}
