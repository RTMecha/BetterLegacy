using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using SimpleJSON;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Helper struct for storing RectTransform values.
    /// </summary>
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

        /// <summary>
        /// Sets only the <see cref="anchoredPosition"/> value of RectValues.
        /// </summary>
        /// <param name="x">X value of anchoredPosition.</param>
        /// <param name="y">Y value of anchoredPosition.</param>
        /// <returns>Returns the current RectValue.</returns>
        public RectValues AnchoredPosition(float x, float y) => AnchoredPosition(new Vector2(x, y));

        RectValues AnchoredPosition(Vector2 anchoredPosition)
        {
            this.anchoredPosition = anchoredPosition;
            return this;
        }

        /// <summary>
        /// Sets only the <see cref="anchorMax"/> value of RectValues.
        /// </summary>
        /// <param name="x">X value of anchorMax.</param>
        /// <param name="y">Y value of anchorMax.</param>
        /// <returns>Returns the current RectValue.</returns>
        public RectValues AnchorMax(float x, float y) => AnchorMax(new Vector2(x, y));

        RectValues AnchorMax(Vector2 anchorMax)
        {
            this.anchorMax = anchorMax;
            return this;
        }

        /// <summary>
        /// Sets only the <see cref="anchorMin"/> value of RectValues.
        /// </summary>
        /// <param name="x">X value of anchorMin.</param>
        /// <param name="y">Y value of anchorMin.</param>
        /// <returns>Returns the current RectValue.</returns>
        public RectValues AnchorMin(float x, float y) => AnchorMin(new Vector2(x, y));

        RectValues AnchorMin(Vector2 anchorMin)
        {
            this.anchorMin = anchorMin;
            return this;
        }

        /// <summary>
        /// Sets only the <see cref="pivot"/> value of RectValues.
        /// </summary>
        /// <param name="x">X value of pivot.</param>
        /// <param name="y">Y value of pivot.</param>
        /// <returns>Returns the current RectValue.</returns>
        public RectValues Pivot(float x, float y) => Pivot(new Vector2(x, y));

        RectValues Pivot(Vector2 pivot)
        {
            this.pivot = pivot;
            return this;
        }

        /// <summary>
        /// Sets only the <see cref="sizeDelta"/> value of RectValues.
        /// </summary>
        /// <param name="x">X value of sizeDelta.</param>
        /// <param name="y">Y value of sizeDelta.</param>
        /// <returns>Returns the current RectValue.</returns>
        public RectValues SizeDelta(float x, float y) => SizeDelta(new Vector2(x, y));

        RectValues SizeDelta(Vector2 sizeDelta)
        {
            this.sizeDelta = sizeDelta;
            return this;
        }

        /// <summary>
        /// Sets only the <see cref="rotation"/> value of RectValues.
        /// </summary>
        /// <param name="rotation">Rotation value.</param>
        /// <returns>Returns the current RectValue.</returns>
        public RectValues Rotation(float rotation)
        {
            this.rotation = rotation;
            return this;
        }

        /// <summary>
        /// The default values when a <see cref="RectTransform"/> component is added to a <see cref="GameObject"/>.
        /// </summary>
        public static RectValues Default => new RectValues
        {
            anchoredPosition = Vector2.zero,
            anchorMax = CenterPivot,
            anchorMin = CenterPivot,
            pivot = CenterPivot,
            sizeDelta = new Vector2(100f, 100f),
            rotation = 0f,
        };

        /// <summary>
        /// Forces a <see cref="RectTransform"/> to take up the entire space of the parent, with both positive and negative corners anchoring to the parents' respective corners.
        /// </summary>
        public static RectValues FullAnchored => new RectValues
        {
            anchoredPosition = Vector2.zero,
            anchorMax = Vector2.one,
            anchorMin = Vector2.zero,
            pivot = CenterPivot,
            sizeDelta = Vector2.zero,
            rotation = 0f,
        };

        /// <summary>
        /// Forces a <see cref="RectTransform"/> to anchor to the left and right sides of the parent.
        /// </summary>
        public static RectValues HorizontalAnchored => new RectValues
        {
            anchoredPosition = Vector2.zero,
            anchorMax = new Vector2(1f, 0.5f),
            anchorMin = new Vector2(0f, 0.5f),
            pivot = CenterPivot,
            sizeDelta = Vector2.zero,
            rotation = 0f,
        };

        /// <summary>
        /// Forces a <see cref="RectTransform"/> to anchor to the up and down sides of the parent.
        /// </summary>
        public static RectValues VerticalAnchored => new RectValues
        {
            anchoredPosition = Vector2.zero,
            anchorMax = new Vector2(0.5f, 1f),
            anchorMin = new Vector2(0.5f, 0f),
            pivot = CenterPivot,
            sizeDelta = Vector2.zero,
            rotation = 0f,
        };

        /// <summary>
        /// Vector2 center pivot point.
        /// </summary>
        public static Vector2 CenterPivot => new Vector2(0.5f, 0.5f);

        public Vector2 anchoredPosition;
        public Vector2 anchorMax;
        public Vector2 anchorMin;
        public Vector2 pivot;
        public Vector2 sizeDelta;
        public float rotation;

        /// <summary>
        /// Parses a "rect" JSON.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed RectValue.</returns>
        public static RectValues Parse(JSONNode jn) => new RectValues
        {
            anchoredPosition = Parser.TryParse(jn["anc_pos"], Vector2.zero),
            anchorMax = Parser.TryParse(jn["anc_max"], CenterPivot),
            anchorMin = Parser.TryParse(jn["anc_min"], CenterPivot),
            pivot = Parser.TryParse(jn["pivot"], CenterPivot),
            sizeDelta = Parser.TryParse(jn["size"], new Vector2(100f, 100f)),
            rotation = jn["rot"].AsFloat,
        };

        /// <summary>
        /// Tries to parse a "rect" JSON.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <param name="defaultValue">Default value to set if <paramref name="jn"/> is null.</param>
        /// <returns>If <paramref name="jn"/> is not null, return a parsed RectValue, otherwise return <paramref name="defaultValue"/>.</returns>
        public static RectValues TryParse(JSONNode jn, RectValues defaultValue) => jn == null ? defaultValue : Parse(jn);

        /// <summary>
        /// Converts the current RectValue to JSON.
        /// </summary>
        /// <returns>Returns a converted JSON.</returns>
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

        /// <summary>
        /// Creates a new RectValues based on a <see cref="RectTransform"/>.
        /// </summary>
        /// <param name="rectTransform">RectTransform to take values from.</param>
        /// <returns>Returns a RectValue based on a RectTransform.</returns>
        public static RectValues FromRectTransform(RectTransform rectTransform) => new RectValues
        {
            anchoredPosition = rectTransform.anchoredPosition,
            anchorMax = rectTransform.anchorMax,
            anchorMin= rectTransform.anchorMin,
            pivot = rectTransform.pivot,
            sizeDelta = rectTransform.sizeDelta,
            rotation = rectTransform.eulerAngles.z,
        };

        /// <summary>
        /// Assigns the current RectValue to a RectTransform.
        /// </summary>
        /// <param name="rectTransform">RectTransform to assign to.</param>
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
