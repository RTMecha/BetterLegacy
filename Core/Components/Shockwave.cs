using UnityEngine;

// thank you Benjamin Swee https://youtu.be/lQ7GNoT_LrE?si=-x_Q6wjJR9DHOi2Y

namespace BetterLegacy.Core.Components
{
    /// <summary>
    /// A shockwave effect.
    /// </summary>
    [ExecuteInEditMode, ImageEffectAllowedInSceneView]
    public class Shockwave : MonoBehaviour
    {
        /// <summary>
        /// Shader of the effect.
        /// </summary>
        public Shader shader;
        Material material;

        /// <summary>
        /// Center of the shockwave.
        /// </summary>
        public Vector2 center;

        /// <summary>
        /// Intensity of the shockwave.
        /// </summary>
        public float intensity;

        /// <summary>
        /// Amount of rings the shockwave has.
        /// </summary>
        public float ringAmount;

        /// <summary>
        /// Scale of the shockwave. If the X or Y values are 0, then the effect will be turned off.
        /// </summary>
        public Vector2 scale;

        /// <summary>
        /// Rotation of the shockwave.
        /// </summary>
        public float rotation;

        /// <summary>
        /// Warps the shockwave rings.
        /// </summary>
        public float warp;

        /// <summary>
        /// Elapsed time of the shockwave.
        /// </summary>
        public float elapsed;

        void Awake()
        {
            shader = LegacyResources.shockwaveShader;
        }

        void OnRenderImage(RenderTexture src, RenderTexture destination)
        {
            if (!shader)
                return;

            int width = src.width;
            int height = src.height;
            if (!material)
                material = new Material(shader);

            material.SetVector("_Center", center);
            material.SetFloat("_Intensity", intensity);
            material.SetFloat("_RingAmount", ringAmount);
            material.SetVector("_Scale", scale);
            material.SetFloat("_Rotation", rotation);
            material.SetFloat("_Warp", warp);
            material.SetFloat("_Elapsed", elapsed);

            var startRenderTexture = RenderTexture.GetTemporary(width, height, 0, src.format);
            Graphics.Blit(src, startRenderTexture, material);
            Graphics.Blit(startRenderTexture, destination);

            RenderTexture.ReleaseTemporary(startRenderTexture);
        }
    }
}
