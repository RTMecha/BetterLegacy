using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using SimpleJSON;

namespace BetterLegacy.Core.Data
{
    public struct RectValues
    {
        public RectValues(Vector2 anchoredPosition, Vector2 anchorMax, Vector2 anchorMin, Vector2 pivot, Vector2 sizeDelta, float rotation = 0f)
        {
            this.anchoredPosition = anchoredPosition;
            this.anchorMax = anchorMax;
            this.anchorMin = anchorMin;
            this.pivot = pivot;
            this.sizeDelta = sizeDelta;
            this.rotation = rotation;
        }

        public RectValues AnchoredPosition(float x, float y) => AnchoredPosition(new Vector2(x, y));

        RectValues AnchoredPosition(Vector2 anchoredPosition)
        {
            this.anchoredPosition = anchoredPosition;
            return this;
        }

        public RectValues AnchorMax(float x, float y) => AnchorMax(new Vector2(x, y));

        RectValues AnchorMax(Vector2 anchorMax)
        {
            this.anchorMax = anchorMax;
            return this;
        }

        public RectValues AnchorMin(float x, float y) => AnchorMin(new Vector2(x, y));

        RectValues AnchorMin(Vector2 anchorMin)
        {
            this.anchorMin = anchorMin;
            return this;
        }

        public RectValues Pivot(float x, float y) => Pivot(new Vector2(x, y));

        RectValues Pivot(Vector2 pivot)
        {
            this.pivot = pivot;
            return this;
        }

        public RectValues SizeDelta(float x, float y) => SizeDelta(new Vector2(x, y));

        RectValues SizeDelta(Vector2 sizeDelta)
        {
            this.sizeDelta = sizeDelta;
            return this;
        }
        
        public RectValues Rotation(float rotation)
        {
            this.rotation = rotation;
            return this;
        }

        public static RectValues Default => new RectValues
        {
            anchoredPosition = Vector2.zero,
            anchorMax = CenterPivot,
            anchorMin = CenterPivot,
            pivot = CenterPivot,
            sizeDelta = new Vector2(100f, 100f),
            rotation = 0f,
        };

        public static RectValues FullAnchored => new RectValues
        {
            anchoredPosition = Vector2.zero,
            anchorMax = Vector2.one,
            anchorMin = Vector2.zero,
            pivot = CenterPivot,
            sizeDelta = Vector2.zero,
            rotation = 0f,
        };

        public static RectValues HorizontalAnchored => new RectValues
        {
            anchoredPosition = Vector2.zero,
            anchorMax = new Vector2(1f, 0.5f),
            anchorMin = new Vector2(0f, 0.5f),
            pivot = CenterPivot,
            sizeDelta = Vector2.zero,
            rotation = 0f,
        };
        
        public static RectValues VerticalAnchored => new RectValues
        {
            anchoredPosition = Vector2.zero,
            anchorMax = new Vector2(0.5f, 1f),
            anchorMin = new Vector2(0.5f, 0f),
            pivot = CenterPivot,
            sizeDelta = Vector2.zero,
            rotation = 0f,
        };

        public static Vector2 CenterPivot => new Vector2(0.5f, 0.5f);

        public Vector2 anchoredPosition;
        public Vector2 anchorMax;
        public Vector2 anchorMin;
        public Vector2 pivot;
        public Vector2 sizeDelta;
        public float rotation;

        public static RectValues Parse(JSONNode jn) => new RectValues
        {
            anchoredPosition = Parser.TryParse(jn["anc_pos"], Vector2.zero),
            anchorMax = Parser.TryParse(jn["anc_max"], CenterPivot),
            anchorMin = Parser.TryParse(jn["anc_min"], CenterPivot),
            pivot = Parser.TryParse(jn["pivot"], CenterPivot),
            sizeDelta = Parser.TryParse(jn["size"], new Vector2(100f, 100f)),
            rotation = jn["rot"].AsFloat,
        };

        public static RectValues TryParse(JSONNode jn, RectValues defaultValue) => jn == null ? defaultValue : Parse(jn);

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            if (anchoredPosition != Vector2.zero)
                jn["anc_pos"] = anchoredPosition.ToJSON();
            if (anchorMax != CenterPivot)
                jn["anc_max"] = anchorMax.ToJSON();
            if (anchorMin != CenterPivot)
                jn["anc_min"] = anchorMin.ToJSON();
            if (pivot != CenterPivot)
                jn["pivot"] = pivot.ToJSON();
            if (sizeDelta != new Vector2(100f, 100f))
                jn["size"] = sizeDelta.ToJSON();

            if (rotation != 0f)
                jn["rot"] = rotation.ToString();

            return jn;
        }

        public static RectValues FromRectTransform(RectTransform rectTransform) => new RectValues
        {
            anchoredPosition = rectTransform.anchoredPosition,
            anchorMax = rectTransform.anchorMax,
            anchorMin= rectTransform.anchorMin,
            pivot = rectTransform.pivot,
            sizeDelta = rectTransform.sizeDelta,
            rotation = rectTransform.eulerAngles.z,
        };

        public void AssignToRectTransform(RectTransform rectTransform)
        {
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.anchorMax = anchorMax;
            rectTransform.anchorMin = anchorMin;
            rectTransform.pivot = pivot;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.SetLocalRotationEulerZ(rotation);
        }

        public static implicit operator RectValues(JSONNode jn) => Parse(jn);
    }
}
