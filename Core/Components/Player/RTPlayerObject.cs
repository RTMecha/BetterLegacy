using UnityEngine;
using UnityEngine.Rendering;

namespace BetterLegacy.Core.Components.Player
{
    /// <summary>
    /// Represents a part of the player.
    /// </summary>
    public class RTPlayerObject : MonoBehaviour
    {
        #region Values

        public bool active = true;
        public string id;

        public Transform parent;
        public GameObject visualObject;
        public Renderer renderer;
        public MeshFilter meshFilter;
        public PlayerDelayTracker delayTracker;

        public TrailRenderer trailRenderer;
        public ParticleSystem particleSystem;
        public ParticleSystemRenderer particleSystemRenderer;

        public bool isCustom;

        public RTPlayer Player { get; set; }
        public RTPlayerObject Parent { get; set; }

        #endregion

        #region Functions

        public virtual void UpdateObject(int index) => visualObject?.SetActive(active);

        public void SetStencil(CompareFunction comparison, StencilOp pass, StencilOp fail, StencilOp zFail, byte id, byte writeMask, byte readMask)
        {
            if (renderer)
                SetStencil(renderer.material, comparison, pass, fail, zFail, id, writeMask, readMask);
            if (trailRenderer)
                SetStencil(trailRenderer.material, comparison, pass, fail, zFail, id, writeMask, readMask);
            if (particleSystemRenderer)
                SetStencil(particleSystemRenderer.material, comparison, pass, fail, zFail, id, writeMask, readMask);
        }

        public void SetStencil(Material material, CompareFunction comparison, StencilOp pass, StencilOp fail, StencilOp zFail, byte id, byte writeMask, byte readMask)
        {
            if (!material)
                return;
            material.SetFloat("_StencilComp", (float)comparison);
            material.SetFloat("_Stencil", id);
            material.SetFloat("_StencilOp", (float)pass);
            material.SetFloat("_StencilFail", (float)fail);
            material.SetFloat("_StencilZFail", (float)zFail);
            material.SetFloat("_StencilWriteMask", writeMask);
            material.SetFloat("_StencilReadMask", readMask);
        }

        public override string ToString() => visualObject?.ToString() ?? id ?? base.ToString();

        #endregion
    }
}
