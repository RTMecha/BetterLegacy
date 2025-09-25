using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

using SimpleJSON;

using BetterLegacy.Core;

namespace BetterLegacy
{
    public class LegacyResources
    {
        public static Material blur;
        public static Material GetBlur()
        {
            var assetBundle = AssetBundle.LoadFromFile(RTFile.GetAsset($"builtin/objectmaterials.asset")); // Get AssetBundle from assets folder.
            var assetToLoad = assetBundle.LoadAsset<Material>("blur.mat"); // Load asset
            var blurMat = UnityEngine.Object.Instantiate(assetToLoad); // Instantiate so we can keep the material
            assetBundle.Unload(false); // Unloads AssetBundle

            return blurMat;
        }

        public static Shader GetBlurColored()
        {
            var assetBundle = AssetBundle.LoadFromFile(RTFile.GetAsset($"builtin/shadercolored.asset"));
            var blurColored = assetBundle.LoadAsset<Shader>("simpleblur.shader");
            assetBundle.Unload(false);
            return blurColored;
        }

        public static Shader blurColored;

        public static Shader analogGlitchShader;
        public static Material analogGlitchMaterial;
        public static Shader digitalGlitchShader;
        public static Material digitalGlitchMaterial;

        public static void GetKinoGlitch()
        {
            var assetBundle = AssetBundle.LoadFromFile(RTFile.GetAsset($"builtin/effects.asset")); // Get AssetBundle from assets folder.
            analogGlitchMaterial = assetBundle.LoadAsset<Material>("analogglitchmaterial.mat"); // Load asset
            digitalGlitchMaterial = assetBundle.LoadAsset<Material>("digitalglitchmaterial.mat"); // Load asset
            analogGlitchShader = assetBundle.LoadAsset<Shader>("analogglitch.shader"); // Load asset
            digitalGlitchShader = assetBundle.LoadAsset<Shader>("digitalglitch.shader"); // Load asset
        }

        public static Shader objectShader;
        public static Material objectMaterial;

        public static Shader gradientShader;
        public static Material gradientMaterial;

        public static Shader radialGradientShader;
        public static Material radialGradientMaterial;

        public static Shader objectDoubleSidedShader;
        public static Material objectDoubleSidedMaterial;

        public static Shader gradientDoubleSidedShader;
        public static Material gradientDoubleSidedMaterial;

        public static Shader radialGradientDoubleSidedShader;
        public static Material radialGradientDoubleSidedMaterial;

        public static void GetObjectMaterials()
        {
            blur = GetBlur();
            blurColored = GetBlurColored();

            var assetBundle = AssetBundle.LoadFromFile(RTFile.GetAsset($"builtin/bmobjectmaterials.asset")); // Get AssetBundle from assets folder.

            // normal
            objectShader = assetBundle.LoadAsset<Shader>("objectshader.shader"); // Load asset
            gradientShader = assetBundle.LoadAsset<Shader>("gradientshader.shader"); // Load asset
            radialGradientShader = assetBundle.LoadAsset<Shader>("radialshader.shader"); // Load asset

            // double sided
            objectDoubleSidedShader = assetBundle.LoadAsset<Shader>("doubleobjectshader.shader"); // Load asset
            gradientDoubleSidedShader = assetBundle.LoadAsset<Shader>("doublegradientshader.shader"); // Load asset
            radialGradientDoubleSidedShader = assetBundle.LoadAsset<Shader>("doubleradialshader.shader"); // Load asset

            // normal
            objectMaterial = new Material(objectShader);
            gradientMaterial = new Material(gradientShader);
            radialGradientMaterial = new Material(radialGradientShader);

            // double sided
            objectDoubleSidedMaterial = new Material(objectDoubleSidedShader);
            gradientDoubleSidedMaterial = new Material(gradientDoubleSidedShader);
            radialGradientDoubleSidedMaterial = new Material(radialGradientDoubleSidedShader);
        }

        public static Material canvasImageMask;
        // from https://stackoverflow.com/questions/59138535/unity-ui-image-alpha-overlap
        public static void GetGUIAssets()
        {
            var assetBundle = AssetBundle.LoadFromFile(RTFile.GetAsset("builtin/gui.asset"));
            canvasImageMask = new Material(assetBundle.LoadAsset<Shader>("canvasimagemask.shader"));
            assetBundle.Unload(false);
        }


        public static AssetBundle postProcessResourcesAssetBundle;
        public static PostProcessResources postProcessResources;

        public static void GetEffects()
        {
            postProcessResourcesAssetBundle = AssetBundle.LoadFromFile(RTFile.GetAsset("builtin/effectresources.asset"));
            postProcessResources = postProcessResourcesAssetBundle.LoadAsset<PostProcessResources>("postprocessresources.asset");
        }

        public static Dictionary<Rank, string[]> sayings = new Dictionary<Rank, string[]>()
        {
            { Rank.Null, null },
            { Rank.SS, null },
            { Rank.S, null },
            { Rank.A, null },
            { Rank.B, null },
            { Rank.C, null },
            { Rank.D, null },
            { Rank.F, null },
        };

        public static void GetSayings()
        {
            if (!AssetPack.TryReadFromFile($"core/sayings{FileFormat.JSON.Dot()}", out string file))
                return;

            var sayingsJN = JSON.Parse(file)["sayings"];

            if (sayingsJN == null)
                return;

            var ranks = Rank.Null.GetValues();
            foreach (var rank in ranks)
            {
                if (sayingsJN[rank.Name.ToLower()] != null)
                    sayings[rank] = sayingsJN[rank.Name.ToLower()].Children.Select(x => x.Value).ToArray();
            }
        }
    }
}
