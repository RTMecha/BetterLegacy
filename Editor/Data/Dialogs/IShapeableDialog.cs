using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Data.Dialogs
{
    /// <summary>
    /// Indicates a Dialog contains a shape toggle list.
    /// </summary>
    public interface IShapeableDialog
    {
        /// <summary>
        /// Label for shapes (and gradients if the shapeable supports gradients)
        /// </summary>
        public Text ShapesLabel { get; set; }

        /// <summary>
        /// Shape types parent.
        /// </summary>
        public RectTransform ShapeTypesParent { get; set; }

        /// <summary>
        /// Shape options parent.
        /// </summary>
        public RectTransform ShapeOptionsParent { get; set; }

        /// <summary>
        /// List of shape type toggles.
        /// </summary>
        public List<Toggle> ShapeToggles { get; set; }

        /// <summary>
        /// List of shape option toggles.
        /// </summary>
        public List<List<Toggle>> ShapeOptionToggles { get; set; }

        /// <summary>
        /// List of shapes the editor does not support yet.
        /// </summary>
        public List<int> UnsupportedShapes { get; set; }
    }

    public static class ShapeableDialogExtension
    {
        /// <summary>
        /// Checks if a shape type is compatible with the object.
        /// </summary>
        /// <param name="shape">Shape type to check.</param>
        /// <returns>Returns <see langword="true"/> if the shape type is supported, otherwise returns <see langword="false"/>.</returns>
        public static bool IsSupportedShapeType(this IShapeableDialog dialog, int shape) => dialog.UnsupportedShapes == null || !dialog.UnsupportedShapes.Contains(shape);
    }
}
