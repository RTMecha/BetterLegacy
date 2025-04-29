using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

using BepInEx;
using HarmonyLib;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Patchers;

using Version = BetterLegacy.Core.Data.Version;

namespace BetterLegacy
{
    public class LegacyResources
    {
        public static Material blur;
        public static Material GetBlur()
        {
            var assetBundle = AssetBundle.LoadFromFile($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}objectmaterials.asset"); // Get AssetBundle from assets folder.
            var assetToLoad = assetBundle.LoadAsset<Material>("blur.mat"); // Load asset
            var blurMat = UnityEngine.Object.Instantiate(assetToLoad); // Instantiate so we can keep the material
            assetBundle.Unload(false); // Unloads AssetBundle

            return blurMat;
        }

        public static Shader GetBlurColored()
        {
            var assetBundle = AssetBundle.LoadFromFile($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}shadercolored.asset");
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
            var assetBundle = AssetBundle.LoadFromFile($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}effects.asset"); // Get AssetBundle from assets folder.
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

            var assetBundle = AssetBundle.LoadFromFile($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}bmobjectmaterials.asset"); // Get AssetBundle from assets folder.

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
    }
}
