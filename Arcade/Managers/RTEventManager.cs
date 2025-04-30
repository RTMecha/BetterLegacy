using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using LSFunctions;

using DG.Tweening;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor;

using Ease = BetterLegacy.Core.Animation.Ease;
using Random = UnityEngine.Random;

namespace BetterLegacy.Arcade.Managers
{
    public class RTEventManager : MonoBehaviour
    {
        public EventDelayTracker delayTracker;

        public static RTEventManager inst;

        public EditorCameraController editorCamera;

        public Camera glitchCam;
        public AnalogGlitch analogGlitch;
        public DigitalGlitch digitalGlitch;

        public Camera uiCam;

        public static bool windowPositionResolutionChanged = false;

        void Awake()
        {
            if (inst == null)
                inst = this;
            else if (inst != this)
                Destroy(gameObject);

            editorCamera = EditorCameraController.Bind();

            // Create a new camera since putting the glitch effects on the Main Camera make the objects disappear.
            var glitchCamera = new GameObject("Glitch Camera");
            glitchCamera.transform.SetParent(EventManager.inst.cam.transform);
            glitchCamera.transform.localPosition = Vector3.zero;
            glitchCamera.transform.localScale = Vector3.one;

            glitchCam = glitchCamera.AddComponent<Camera>();
            glitchCam.allowMSAA = false;
            glitchCam.clearFlags = CameraClearFlags.Depth;
            glitchCam.cullingMask = 310;
            glitchCam.depth = 2f;
            glitchCam.farClipPlane = 10000f;
            glitchCam.forceIntoRenderTexture = true;
            glitchCam.nearClipPlane = 9999.9f;
            glitchCam.orthographic = true;
            glitchCam.rect = new Rect(0.001f, 0.001f, 0.999f, 0.999f);

            digitalGlitch = glitchCamera.AddComponent<DigitalGlitch>();
            digitalGlitch._shader = LegacyResources.digitalGlitchShader;

            analogGlitch = glitchCamera.AddComponent<AnalogGlitch>();
            analogGlitch._shader = LegacyResources.analogGlitchShader;

            var uiCamera = new GameObject("UI Camera");
            uiCamera.transform.SetParent(EventManager.inst.cam.transform.parent);
            uiCamera.transform.localPosition = Vector3.zero;
            uiCamera.transform.localScale = Vector3.one;

            uiCam = uiCamera.AddComponent<Camera>();
            uiCam.allowMSAA = false;
            uiCam.clearFlags = CameraClearFlags.Depth;
            uiCam.cullingMask = 2080; // 2080 = layer 11
            uiCam.depth = 2;
            uiCam.orthographic = true;
            uiCam.farClipPlane = 10000f;
            uiCam.nearClipPlane = -10000f;
        }

        void FixedUpdate()
        {
            if (!delayTracker.leader && InputDataManager.inst.players.Count > 0 && GameManager.inst.players.transform.Find("Player 1/Player"))
                delayTracker.leader = GameManager.inst.players.transform.Find("Player 1/Player");
        }
    }
}
