using UnityEngine;
using UnityEngine.Rendering;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Represents stencil properties in shaders.
    /// </summary>
    public class StencilProperties : Exists
    {
        public StencilProperties() { }

        #region Values

        /// <summary>
        /// Comparison function.
        /// </summary>
        public CompareFunction compare = CompareFunction.Always;

        /// <summary>
        /// Pass operation.
        /// </summary>
        public StencilOp pass = StencilOp.Keep;

        /// <summary>
        /// Fail operation.
        /// </summary>
        public StencilOp fail = StencilOp.Keep;

        /// <summary>
        /// Z fail operation.
        /// </summary>
        public StencilOp zFail = StencilOp.Keep;

        /// <summary>
        /// Stencil ID.
        /// </summary>
        public byte id = 0;

        /// <summary>
        /// Write mask.
        /// </summary>
        public byte writeMask = 255;

        /// <summary>
        /// Read mask.
        /// </summary>
        public byte readMask = 255;

        #endregion

        #region Functions

        /// <summary>
        /// Sets the stencil properties to a material.
        /// </summary>
        /// <param name="material">Material reference..</param>
        public void ApplyToMaterial(Material material) => ApplyToMaterial(material, compare, pass, fail, zFail, id, writeMask, readMask);

        /// <summary>
        /// Sets the stencil properties to a material.
        /// </summary>
        /// <param name="material">Material reference..</param>
        /// <param name="comparison">Compare function.</param>
        /// <param name="pass">Stencil pass operation.</param>
        /// <param name="fail">Stencil fail operation.</param>
        /// <param name="zFail">Stencil Z fail operation.</param>
        /// <param name="id">Stencil ID.</param>
        /// <param name="writeMask">Stencil write mask.</param>
        /// <param name="readMask">Stencil read mask.</param>
        public static void ApplyToMaterial(Material material, CompareFunction comparison, StencilOp pass, StencilOp fail, StencilOp zFail, byte id, byte writeMask, byte readMask)
        {
            material.SetFloat("_StencilComp", (float)comparison);
            material.SetFloat("_Stencil", id);
            material.SetFloat("_StencilOp", (float)pass);
            material.SetFloat("_StencilFail", (float)fail);
            material.SetFloat("_StencilZFail", (float)zFail);
            material.SetFloat("_StencilWriteMask", writeMask);
            material.SetFloat("_StencilReadMask", readMask);
        }

        #endregion
    }
}
