using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ActorFrameTexture : ModifierActionBase
    {
        #region Constructors

        public ActorFrameTexture() => SetupModifier("0", "512", "512", "0", "0", "1", "0", "False", "False", "", "True", "0", "0", "1", "1", "False");

        #endregion

        #region Values

        public override string Name => "actorFrameTexture";

        public override CategoryType Category => CategoryType.Shape;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject || beatmapObject.ShapeType == ShapeType.Image)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject)
                return;

            var width = modifier.GetInt(1, 512, modifierLoop.variables);
            var height = modifier.GetInt(2, 512, modifierLoop.variables);
            var offsetX = modifier.GetFloat(3, 0f, modifierLoop.variables);
            var offsetY = modifier.GetFloat(4, 0f, modifierLoop.variables);
            var zoom = modifier.GetFloat(5, 1f, modifierLoop.variables);
            var rotate = modifier.GetFloat(6, 0f, modifierLoop.variables);
            var allCameras = modifier.GetBool(7, false, modifierLoop.variables);
            var clearTexture = modifier.GetBool(8, false, modifierLoop.variables);
            var customColor = modifier.GetValue(9, modifierLoop.variables);
            var calculateZoom = modifier.GetBool(10, true, modifierLoop.variables);
            var textureOffset = new Vector2(modifier.GetFloat(11, 0f, modifierLoop.variables), modifier.GetFloat(12, 0f, modifierLoop.variables));
            var textureScale = new Vector2(modifier.GetFloat(13, 1f, modifierLoop.variables), modifier.GetFloat(14, 0f, modifierLoop.variables));
            var hidePlayers = modifier.GetBool(15, false, modifierLoop.variables);

            var renderer = runtimeObject.visualObject.renderer;
            if (!renderer)
                return;

            if (runtimeObject.visualObject is not SolidObject solidObject)
                return;

            // Get render texture
            var result = modifier.GetResultOrDefault(() =>
            {
                var cache = new Cache()
                {
                    width = width,
                    height = height,
                    renderTexture = new RenderTexture(width, height, 24)
                    {
                        name = SpriteHelper.DEFAULT_TEXTURE_NAME,
                        wrapMode = TextureWrapMode.Clamp,
                        useDynamicScale = true,
                    },
                    obj = beatmapObject,
                    isEditing = ProjectArrhythmia.State.IsEditing,
                };
                renderer.material.SetTexture("_MainTex", cache.renderTexture);
                DestroyModifierResult.Init(solidObject.gameObject, modifier);
                return cache;
            });
            if (result.width != width || result.height != height || result.isEditing != ProjectArrhythmia.State.IsEditing)
            {
                CoreHelper.Destroy(result.renderTexture);

                result = new Cache()
                {
                    width = width,
                    height = height,
                    renderTexture = new RenderTexture(width, height, 24)
                    {
                        name = SpriteHelper.DEFAULT_TEXTURE_NAME,
                        wrapMode = TextureWrapMode.Clamp,
                        useDynamicScale = true,
                    },
                    obj = beatmapObject,
                    isEditing = ProjectArrhythmia.State.IsEditing,
                };
                renderer.material.SetTexture("_MainTex", result.renderTexture);
                modifier.Result = result;
            }

            renderer.material.mainTextureOffset = textureOffset;
            renderer.material.mainTextureScale = textureScale;

            if (allCameras)
            {
                RTLevel.Current.eventEngine.SetCameraPosition(new Vector2(offsetX, offsetY));
                RTLevel.Current.eventEngine.SetCameraRotation(rotate);

                EventManager.inst.camParent.transform.localPosition = Vector2.zero;
                var trackerPos = RTEventManager.inst.delayTracker.transform.localPosition;
                RTEventManager.inst.delayTracker.transform.localPosition = Vector2.zero;

                var rect = RTLevel.Cameras.FG.rect;
                RTLevel.Cameras.SetCameraArea(new Rect(0f, 0f, 1f, 1f));
                var total = width + height;
                RTLevel.Current.eventEngine.SetZoom(calculateZoom ? (width + height) / 2 / 512f * 12.66f * zoom : zoom);

                //var playersActive = GameManager.inst.players.activeSelf;
                //if (hidePlayers)
                //    GameManager.inst.players.SetActive(false);

                //var clearFlags = RTLevel.Cameras.FG.clearFlags;
                //var bgColor = RTLevel.Cameras.FG.backgroundColor;
                //RTLevel.Cameras.FG.clearFlags = CameraClearFlags.SolidColor;
                //RTLevel.Cameras.FG.backgroundColor = RTLevel.Cameras.BG.backgroundColor;

                foreach (var camera in RTLevel.Cameras.GetCameras())
                {
                    // create
                    var renderTexture = result.renderTexture;

                    var currentActiveRT = RenderTexture.active;
                    RenderTexture.active = renderTexture;

                    var enabled = renderer.enabled;
                    renderer.enabled = false;
                    // Assign render texture to camera and render the camera
                    camera.targetTexture = renderTexture;
                    camera.Render();
                    renderTexture.Create();

                    // Reset to defaults
                    renderer.enabled = enabled;
                    camera.targetTexture = null;
                    RenderTexture.active = currentActiveRT;

                    camera.transform.localPosition = Vector3.zero;
                    camera.transform.localEulerAngles = Vector3.zero;
                }

                var editorCam = RTEditor.inst && RTEditor.inst.editorInfo.freecamEnabled;

                RTLevel.Current.eventEngine.SetCameraRotation(editorCam ?
                    new Vector3(RTEditor.inst.editorInfo.freecamPerRotate.x, RTEditor.inst.editorInfo.freecamPerRotate.y, RTEditor.inst.editorInfo.freecamRotate) :
                    new Vector3(RTLevel.Current.eventEngine.camRotOffset.x, RTLevel.Current.eventEngine.camRotOffset.y, EventManager.inst.camRot));

                RTLevel.Current.eventEngine.SetCameraPosition(editorCam ?
                    RTEditor.inst.editorInfo.freecamPosition :
                    EventManager.inst.camPos);

                // fixes bg camera position being offset if rotated for some reason...
                RTLevel.Cameras.BG.transform.SetLocalPositionX(0f);
                RTLevel.Cameras.BG.transform.SetLocalPositionY(0f);

                RTLevel.Current.eventEngine.SetZoom(editorCam ?
                    RTEditor.inst.editorInfo.freecamZoom :
                    EventManager.inst.camZoom);

                RTLevel.Current.eventEngine.UpdateShake();

                RTEventManager.inst.delayTracker.transform.localPosition = trackerPos;

                //RTLevel.Cameras.FG.clearFlags = clearFlags;
                //RTLevel.Cameras.FG.backgroundColor = bgColor;

                // disable and re-enable the glitch camera to ensure the glitch camera is ordered last.
                RTEventManager.inst.glitchCam.enabled = false;
                RTEventManager.inst.glitchCam.enabled = true;

                // disable and re-enable the UI camera to ensure the UI camera is ordered last.
                RTLevel.Cameras.UI.enabled = false;
                RTLevel.Cameras.UI.enabled = true;

                //if (hidePlayers)
                //    GameManager.inst.players.SetActive(true);

                //if (useCustomBGColor)
                //{
                //    RTLevel.Cameras.FG.clearFlags = clearFlags;
                //    RTLevel.Cameras.FG.backgroundColor = bgColor;
                //}

                RTLevel.Cameras.SetCameraArea(rect);
            }
            else
            {
                var camera = modifier.GetInt(0, 0, modifierLoop.variables) switch
                {
                    1 => RTLevel.Cameras.BG,
                    2 => RTLevel.Cameras.UI,
                    _ => RTLevel.Cameras.FG,
                };

                camera.transform.localPosition = new Vector3(offsetX, offsetY);
                camera.transform.localEulerAngles = new Vector3(0f, 0f, rotate);

                RTLevel.Current.eventEngine.SetCameraPosition(Vector3.zero);
                RTLevel.Current.eventEngine.SetCameraRotation(0f);

                EventManager.inst.camParent.transform.localPosition = Vector2.zero;
                var trackerPos = RTEventManager.inst.delayTracker.transform.localPosition;
                RTEventManager.inst.delayTracker.transform.localPosition = Vector2.zero;

                var rect = camera.rect;
                camera.rect = new Rect(0f, 0f, 1f, 1f);
                RTLevel.Current.eventEngine.SetZoom(calculateZoom ? (width + height) / 2 / 512f * 12.66f * zoom : zoom);

                var playersActive = GameManager.inst.players.activeSelf;
                if (hidePlayers)
                    GameManager.inst.players.SetActive(false);

                // create
                var renderTexture = result.renderTexture;

                var clearFlags = camera.clearFlags;
                var bgColor = camera.backgroundColor;
                if (clearTexture)
                {
                    renderTexture.Release();
                    camera.clearFlags = CameraClearFlags.SolidColor;
                }
                try
                {
                    camera.backgroundColor = !string.IsNullOrEmpty(customColor) ? RTColors.HexToColor(customColor) : RTLevel.Cameras.BG.backgroundColor;
                }
                catch
                {
                    camera.backgroundColor = RTLevel.Cameras.BG.backgroundColor;
                }

                var currentActiveRT = RenderTexture.active;
                RenderTexture.active = renderTexture;

                var enabled = renderer.enabled;
                renderer.enabled = false;
                // Assign render texture to camera and render the camera
                camera.targetTexture = renderTexture;
                camera.Render();
                renderTexture.Create();

                // Reset to defaults
                renderer.enabled = enabled;
                camera.targetTexture = null;
                RenderTexture.active = currentActiveRT;

                camera.transform.localPosition = Vector3.zero;
                camera.transform.localEulerAngles = Vector3.zero;

                var editorCam = RTEditor.inst && RTEditor.inst.editorInfo.freecamEnabled;

                RTLevel.Current.eventEngine.SetCameraRotation(editorCam ?
                    new Vector3(RTEditor.inst.editorInfo.freecamPerRotate.x, RTEditor.inst.editorInfo.freecamPerRotate.y, RTEditor.inst.editorInfo.freecamRotate) :
                    new Vector3(RTLevel.Current.eventEngine.camRotOffset.x, RTLevel.Current.eventEngine.camRotOffset.y, EventManager.inst.camRot));

                RTLevel.Current.eventEngine.SetCameraPosition(editorCam ?
                    RTEditor.inst.editorInfo.freecamPosition :
                    EventManager.inst.camPos);

                // fixes bg camera position being offset if rotated for some reason...
                RTLevel.Cameras.BG.transform.SetLocalPositionX(0f);
                RTLevel.Cameras.BG.transform.SetLocalPositionY(0f);

                RTLevel.Current.eventEngine.SetZoom(editorCam ?
                    RTEditor.inst.editorInfo.freecamZoom :
                    EventManager.inst.camZoom);

                RTLevel.Current.eventEngine.UpdateShake();

                RTEventManager.inst.delayTracker.transform.localPosition = trackerPos;

                camera.clearFlags = clearFlags;
                camera.backgroundColor = bgColor;

                // disable and re-enable the glitch camera to ensure the glitch camera is ordered last.
                RTEventManager.inst.glitchCam.enabled = false;
                RTEventManager.inst.glitchCam.enabled = true;

                // disable and re-enable the UI camera to ensure the UI camera is ordered last.
                RTLevel.Cameras.UI.enabled = false;
                RTLevel.Cameras.UI.enabled = true;

                camera.rect = rect;

                if (hidePlayers)
                    GameManager.inst.players.SetActive(playersActive);
            }
        }

        public override void OnRemoveCache(Modifier modifier)
        {
            if (modifier.TryGetResult(out Cache cache))
                CoreHelper.Destroy(cache.renderTexture);
                if (cache.obj && cache.obj.runtimeObject && cache.obj.runtimeObject.visualObject is SolidObject solidObject && solidObject.material)
                    solidObject.UpdateRendering(
                        gradientType: solidObject.gradientType,
                        renderType: solidObject.renderType,
                        doubleSided: solidObject.doubleSided,
                        gradientScale: solidObject.gradientScale,
                        gradientRotation: solidObject.gradientRotation,
                        colorBlendMode: solidObject.colorBlendMode);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.DropdownGenerator(modifier, reference, "Camera", 0, CoreHelper.StringToOptionData("Foreground", "Background", "UI"));
            modifierCard.BoolGenerator(modifier, reference, "All Cameras", 7);
            modifierCard.IntegerGenerator(modifier, reference, "Width", 1, 512);
            modifierCard.IntegerGenerator(modifier, reference, "Height", 2, 512);
            modifierCard.SingleGenerator(modifier, reference, "Pos X", 3, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Pos Y", 4, 0f);
            modifierCard.BoolGenerator(modifier, reference, "Calculate Zoom", 10, true);
            modifierCard.SingleGenerator(modifier, reference, "Zoom", 5, 1f);
            modifierCard.SingleGenerator(modifier, reference, "Rotate", 6, 0f, 15f, 3f);

            modifierCard.SingleGenerator(modifier, reference, "Texture Offset X", 11);
            modifierCard.SingleGenerator(modifier, reference, "Texture Offset Y", 12);
            modifierCard.SingleGenerator(modifier, reference, "Texture Scale X", 13, 1f);
            modifierCard.SingleGenerator(modifier, reference, "Texture Scale Y", 14, 1f);
            modifierCard.BoolGenerator(modifier, reference, "Clear Texture", 8);

            var primaryHexCode = modifierCard.StringGenerator(modifier, reference, "BG Color", 9);
            EditorContextMenu.AddContextMenu(primaryHexCode,
                EditorContextMenu.GetEditorColorFunctions(primaryHexCode.transform.Find("Input").GetComponent<InputField>(), () => modifier.GetValue(9)));

            modifierCard.BoolGenerator(modifier, reference, "Hide Players", 15);
        }

        #endregion

        #region Sub Classes

        public class Cache
        {
            public int width;
            public int height;
            public RenderTexture renderTexture;
            public BeatmapObject obj;
            public bool isEditing;
        }

        #endregion
    }
}
