using UnityEngine;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Prefabs;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class PolygonShapeEditor : Exists
    {
        public InputFieldStorage RadiusField { get; set; }

        public InputFieldStorage SidesField { get; set; }

        public InputFieldStorage RoundnessField { get; set; }

        public InputFieldStorage ThicknessField { get; set; }

        public Vector2InputFieldStorage ThicknessOffsetFields { get; set; }

        public Vector2InputFieldStorage ThicknessScaleFields { get; set; }

        public InputFieldStorage ThicknessAngleField { get; set; }

        public InputFieldStorage SlicesField { get; set; }

        public InputFieldStorage AngleField { get; set; }

        public InputFieldStorage AlternateField { get; set; }

        public void Apply(Transform transform)
        {
            RadiusField = transform.Find("radius").GetComponent<InputFieldStorage>();
            SidesField = transform.Find("sides").GetComponent<InputFieldStorage>();
            RoundnessField = transform.Find("roundness").GetComponent<InputFieldStorage>();
            ThicknessField = transform.Find("thickness").GetComponent<InputFieldStorage>();
            ThicknessOffsetFields = transform.Find("thickness offset").GetComponent<Vector2InputFieldStorage>();
            ThicknessScaleFields = transform.Find("thickness scale").GetComponent<Vector2InputFieldStorage>();
            ThicknessAngleField = transform.Find("thickness angle").GetComponent<InputFieldStorage>();
            SlicesField = transform.Find("slices").GetComponent<InputFieldStorage>();
            AngleField = transform.Find("rotation").GetComponent<InputFieldStorage>();
            AlternateField = transform.Find("alternate").GetComponent<InputFieldStorage>();
        }
    }
}
