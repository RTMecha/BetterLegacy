using UnityEngine;
 
using BetterLegacy.Core.Components;
using BetterLegacy.Editor;

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
            uiCam.allowHDR = false; // prevent graininess in negative zoom
            uiCam.allowMSAA = false;
            uiCam.clearFlags = CameraClearFlags.Depth;
            uiCam.cullingMask = 2080; // 2080 = layer 11
            uiCam.depth = 2;
            uiCam.orthographic = true;
            uiCam.farClipPlane = 10000f;
            uiCam.nearClipPlane = -10000f;
        }
    }
}
