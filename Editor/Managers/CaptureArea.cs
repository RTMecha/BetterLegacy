using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// A rect area that can capture an image.
    /// </summary>
    public class CaptureArea : MonoBehaviour
    {
        #region Init

        /// <summary>
        /// The <see cref="CaptureArea"/> global instance reference.
        /// </summary>
        public static CaptureArea inst;

        /// <summary>
        /// Initializes <see cref="CaptureArea"/>.
        /// </summary>
        public static void Init()
        {
            var canvas = UIManager.GenerateUICanvas("Capture Canvas", null);
            canvas.SetWorldSpace(RTLevel.UI_LAYER, RTLevel.Cameras.FG);
            var captureArea = canvas.GameObject.AddComponent<CaptureArea>();
            captureArea.canvas = canvas;
            captureArea.InternalInit();
        }

        void InternalInit()
        {
            inst = this;
            baseObject = Creator.NewUIObject("Base", canvas.GameObject.transform);
            var gameObject = Creator.NewUIObject("Capture Area", baseObject.transform);
            baseImage = gameObject.AddComponent<Image>();
            baseImage.color = new Color(0.1f, 0.1f, 0.1f, 0.01f);

            var origin = Creator.NewUIObject("Origin", gameObject.transform);
            origin.transform.AsRT().sizeDelta = new Vector2(32f, 32f);
            var originImage = origin.AddComponent<Image>();
            originImage.sprite = EditorSprites.CloseSprite;

            var outline = Creator.NewUIObject("Outline", gameObject.transform);
            RectValues.FullAnchored.AssignToRectTransform(outline.transform.AsRT());
            outlineImage = outline.AddComponent<Image>();
            outlineImage.type = Image.Type.Tiled;
            outlineImage.sprite = EditorManager.inst.SelectionBoxImage.sprite;
            var outlineClickable = outline.AddComponent<Clickable>();
            outlineClickable.onClick = pointerEventData =>
            {
                if (wasDragging)
                {
                    wasDragging = false;
                    return;
                }

                if (pointerEventData.button == PointerEventData.InputButton.Right)
                {
                    var buttonFunctions = new List<EditorElement>
                    {
                        new ButtonElement("Create Capture", CreateCapture),
                        new ButtonElement("Clear", ClearCapture),
                        new SpacerElement(),
                        new ButtonElement("Reset All", Settings.Reset),
                        new ButtonElement("Reset Resolution", () => Settings.Resolution = new Vector2Int(512, 512)),
                        new ButtonElement("Reset Position", () => Settings.pos = Vector2.zero),
                        new ButtonElement("Reset Zoom", () => Settings.Zoom = 1f),
                        new ButtonElement("Reset Rotation", () => Settings.rot = 0f),
                        new SpacerElement(),
                        ButtonElement.ToggleButton(View == ViewType.PlayerModel ? "Hide Objects" : "Hide Players", () => Settings.hidePlayers, () => Settings.hidePlayers = !Settings.hidePlayers),
                        ButtonElement.ToggleButton("Capture All Layers", () => Settings.captureAllLayers, () => Settings.captureAllLayers = !Settings.captureAllLayers),
                        ButtonElement.ToggleButton("Show Editor", () => Settings.showEditor, () => Settings.showEditor = !Settings.showEditor),
                        new SpacerElement(),
                        ButtonElement.ToggleButton($"Use Custom BG Color", () => Settings.useCustomBGColor, () => Settings.useCustomBGColor = !Settings.useCustomBGColor),
                        new ButtonElement($"Set Custom BG Color", () => RTColorPicker.inst.Show(
                            currentColor: Settings.customBGColor,
                            colorChanged: (col, hex) => { },
                            colorSaved: (col, hex) => Settings.customBGColor = col)),
                        new SpacerElement(),
                        new ButtonElement("Copy", () => copiedSettings = Settings.Copy()),
                        new ButtonElement("Paste", () =>
                        {
                            if (!copiedSettings)
                            {
                                EditorManager.inst.DisplayNotification($"No copied capture settings yet!", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            Settings.CopyData(copiedSettings);
                            EditorManager.inst.DisplayNotification($"Pasted copied capture settings!", 2f, EditorManager.NotificationType.Success);
                        })
                    };

                    switch (View)
                    {
                        case ViewType.Prefab: {
                                buttonFunctions.AddRange(new List<EditorElement>
                                {
                                    new SpacerElement(),
                                    new ButtonElement("Set 256 x 256", () => Settings.Resolution = new Vector2Int(256, 256)),
                                    new ButtonElement("Set 128 x 128", () => Settings.Resolution = new Vector2Int(128, 128)),
                                    new ButtonElement("Set 64 x 64", () => Settings.Resolution = new Vector2Int(64, 64)),
                                });
                                break;
                            }
                        case ViewType.Screenshot: {
                                buttonFunctions.Add(new SpacerElement());
                                var resolutions = CustomEnumHelper.GetValues<ResolutionType>();
                                foreach (var resolution in resolutions)
                                    buttonFunctions.Add(new ButtonElement($"Set {resolution.Width} x {resolution.Height}", () => Settings.Resolution = new Vector2Int(resolution.Width, resolution.Height)));
                                break;
                            }
                        case ViewType.PlayerModel: {
                                buttonFunctions.AddRange(new List<EditorElement>
                                {
                                    new SpacerElement(),
                                    new ButtonElement("Set 256 x 256", () => Settings.Resolution = new Vector2Int(256, 256)),
                                    new ButtonElement("Set 128 x 128", () => Settings.Resolution = new Vector2Int(128, 128)),
                                    new ButtonElement("Set 64 x 64", () => Settings.Resolution = new Vector2Int(64, 64)),
                                });
                                break;
                            }
                    }

                    buttonFunctions.AddRange(new List<EditorElement>
                    {
                        new SpacerElement(),
                        ButtonElement.SelectionButton(() => Settings.lockDragMode == CaptureSettings.LockDragMode.None, "No Lock", () =>
                        {
                            Settings.lockDragMode = CaptureSettings.LockDragMode.None;
                            EditorManager.inst.DisplayNotification($"Set lock drag to {Settings.lockDragMode}!", 2f, EditorManager.NotificationType.Success);
                        }),
                        ButtonElement.SelectionButton(() => Settings.lockDragMode == CaptureSettings.LockDragMode.PositionX, "Position X Lock", () =>
                        {
                            Settings.lockDragMode = CaptureSettings.LockDragMode.PositionX;
                            EditorManager.inst.DisplayNotification($"Set lock drag to {Settings.lockDragMode}!", 2f, EditorManager.NotificationType.Success);
                        }),
                        ButtonElement.SelectionButton(() => Settings.lockDragMode == CaptureSettings.LockDragMode.PositionY, "Position Y Lock", () =>
                        {
                            Settings.lockDragMode = CaptureSettings.LockDragMode.PositionY;
                            EditorManager.inst.DisplayNotification($"Set lock drag to {Settings.lockDragMode}!", 2f, EditorManager.NotificationType.Success);
                        }),
                    });

                    EditorContextMenu.inst.ShowContextMenu(buttonFunctions);
                    return;
                }

                CreateCapture();
            };
            outlineClickable.onBeginDrag = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                    return;

                wasDragging = true;
                StartDrag(Direction.Null, DragType.Position);
            };
            outlineClickable.onEndDrag = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    dragging = false;
            };
            outlineClickable.onScroll = Scroll;

            var left = Creator.NewUIObject("Left", gameObject.transform);
            new RectValues(Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(16f, 0f)).AssignToRectTransform(left.transform.AsRT());
            var leftImage = left.AddComponent<Image>();
            leftImage.color = LSColors.transparent;
            var leftClickable = left.AddComponent<Clickable>();
            leftClickable.onBeginDrag = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    StartDrag(Direction.Left, DragType.Resolution);
            };
            leftClickable.onEndDrag = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    dragging = false;
            };

            var right = Creator.NewUIObject("Right", gameObject.transform);
            new RectValues(Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 0.5f), new Vector2(16f, 0f)).AssignToRectTransform(right.transform.AsRT());
            var rightImage = right.AddComponent<Image>();
            rightImage.color = LSColors.transparent;
            var rightClickable = right.AddComponent<Clickable>();
            rightClickable.onBeginDrag = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    StartDrag(Direction.Right, DragType.Resolution);
            };
            rightClickable.onEndDrag = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    dragging = false;
            };

            var top = Creator.NewUIObject("Top", gameObject.transform);
            new RectValues(Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 16f)).AssignToRectTransform(top.transform.AsRT());
            var topImage = top.AddComponent<Image>();
            topImage.color = LSColors.transparent;
            var topClickable = top.AddComponent<Clickable>();
            topClickable.onBeginDrag = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    StartDrag(Direction.Top, DragType.Resolution);
            };
            topClickable.onEndDrag = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    dragging = false;
            };

            var bottom = Creator.NewUIObject("Bottom", gameObject.transform);
            new RectValues(Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0.5f, 0f), new Vector2(0f, 16f)).AssignToRectTransform(bottom.transform.AsRT());
            var bottomImage = bottom.AddComponent<Image>();
            bottomImage.color = LSColors.transparent;
            var bottomClickable = bottom.AddComponent<Clickable>();
            bottomClickable.onBeginDrag = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    StartDrag(Direction.Bottom, DragType.Resolution);
            };
            bottomClickable.onEndDrag = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    dragging = false;
            };

            var topLeft = Creator.NewUIObject("Top Left", gameObject.transform);
            new RectValues(Vector2.zero, Vector2.one, Vector2.one, Vector2.one, new Vector2(24f, 24f)).AssignToRectTransform(topLeft.transform.AsRT());
            var topLeftImage = topLeft.AddComponent<Image>();
            var topLeftClickable = topLeft.AddComponent<Clickable>();
            topLeftClickable.onBeginDrag = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    StartDrag(Direction.Null, DragType.Rotation);
            };
            topLeftClickable.onEndDrag = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    dragging = false;
            };

            var topRight = Creator.NewUIObject("Top Right", gameObject.transform);
            new RectValues(Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, 24f)).AssignToRectTransform(topRight.transform.AsRT());
            var topRightImage = topRight.AddComponent<Image>();
            var topRightClickable = topRight.AddComponent<Clickable>();
            topRightClickable.onBeginDrag = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    StartDrag(Direction.Null, DragType.Rotation);
            };
            topRightClickable.onEndDrag = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    dragging = false;
            };

            var bottomLeft = Creator.NewUIObject("Bottom Left", gameObject.transform);
            new RectValues(Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(24f, 24f)).AssignToRectTransform(bottomLeft.transform.AsRT());
            var bottomLeftImage = bottomLeft.AddComponent<Image>();
            var bottomLeftClickable = bottomLeft.AddComponent<Clickable>();
            bottomLeftClickable.onBeginDrag = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    StartDrag(Direction.Null, DragType.Rotation);
            };
            bottomLeftClickable.onEndDrag = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    dragging = false;
            };

            var bottomRight = Creator.NewUIObject("Bottom Right", gameObject.transform);
            new RectValues(Vector2.zero, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(24f, 24f)).AssignToRectTransform(bottomRight.transform.AsRT());
            var bottomRightImage = bottomRight.AddComponent<Image>();
            var bottomRightClickable = bottomRight.AddComponent<Clickable>();
            bottomRightClickable.onBeginDrag = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    StartDrag(Direction.Null, DragType.Rotation);
            };
            bottomRightClickable.onEndDrag = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    dragging = false;
            };

            var zoom = EditorPrefabHolder.Instance.Slider.Duplicate(gameObject.transform);
            new RectValues(new Vector2(32f, 0f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 512f)).AssignToRectTransform(zoom.transform.AsRT());
            zoom.transform.Find("Handle Slide Area").AsRT().sizeDelta = Vector2.zero;
            zoomSlider = zoom.GetComponent<Slider>();
            zoomSlider.wholeNumbers = false;
            zoomSlider.onValueChanged.ClearAll();
            zoomSlider.minValue = 0.1f;
            zoomSlider.maxValue = 10f;
            zoomSlider.direction = Slider.Direction.BottomToTop;
            zoomSlider.SetColorBlock(Color.white, Color.white, Color.white, Color.white, RTColors.errorColor);
            zoomSlider.onValueChanged.NewListener(_val =>
            {
                CoreHelper.Log($"Set capture zoom: {_val}");
                Settings.Zoom = _val;
            });
            TriggerHelper.AddEventTriggers(zoom, TriggerHelper.CreateEntry(EventTriggerType.Scroll, eventData => Scroll((PointerEventData)eventData)));

            zoomSlider.image.color = Color.white;
            new RectValues(Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, 16f)).AssignToRectTransform(zoomSlider.image.rectTransform);
            zoom.transform.Find("Image").GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.1f);

            var zoomBarLeft = Creator.NewUIObject("Bar Left", zoom.transform);
            RectValues.LeftAnchored.AnchorMin(0f, 0f).SizeDelta(4f, 0f).AssignToRectTransform(zoomBarLeft.transform.AsRT());
            zoomBarLeft.AddComponent<Image>();
            var zoomBarRight = Creator.NewUIObject("Bar Right", zoom.transform);
            RectValues.RightAnchored.AnchorMin(1f, 0f).SizeDelta(4f, 0f).AssignToRectTransform(zoomBarRight.transform.AsRT());
            zoomBarRight.AddComponent<Image>();
            var zoomBarTop = Creator.NewUIObject("Bar Top", zoom.transform);
            new RectValues(Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 4f)).AssignToRectTransform(zoomBarTop.transform.AsRT());
            zoomBarTop.AddComponent<Image>();
            var zoomBarBottom = Creator.NewUIObject("Bar Bottom", zoom.transform);
            new RectValues(Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0.5f, 0f), new Vector2(0f, 4f)).AssignToRectTransform(zoomBarBottom.transform.AsRT());
            zoomBarBottom.AddComponent<Image>();

            editors = Creator.NewUIObject("Editor", gameObject.transform);
            new RectValues(new Vector2(0f, 32f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(400f, 32f)).AssignToRectTransform(editors.transform.AsRT());

            var positionFieldsLayout = editors.AddComponent<HorizontalLayoutGroup>();

            var xPosition = EditorPrefabHolder.Instance.NumberInputField.Duplicate(editors.transform, "pos_x");
            xPositionField = xPosition.GetComponent<InputFieldStorage>();
            xPositionField.OnValueChanged.NewListener(_val =>
            {
                var variables = new Dictionary<string, float>
                {
                    { "posX", Settings.pos.x },
                    { "posY", Settings.pos.y },
                    { "zoom", Settings.Zoom },
                    { "rot", Settings.rot },
                };

                if (RTMath.TryParse(_val, Settings.pos.x, variables, out float result))
                    Settings.pos.x = result;
            });
            xPositionField.inputField.GetPlaceholderText().text = "Set Pos X...";
            CoreHelper.Delete(xPositionField.leftGreaterButton);
            CoreHelper.Delete(xPositionField.middleButton);
            CoreHelper.Delete(xPositionField.rightGreaterButton);
            EditorThemeManager.ApplyInputField(xPositionField);
            TriggerHelper.IncreaseDecreaseButtons(xPositionField);
            TriggerHelper.InversableField(xPositionField);

            var yPosition = EditorPrefabHolder.Instance.NumberInputField.Duplicate(editors.transform, "pos_y");
            yPositionField = yPosition.GetComponent<InputFieldStorage>();
            yPositionField.OnValueChanged.NewListener(_val =>
            {
                var variables = new Dictionary<string, float>
                {
                    { "posX", Settings.pos.x },
                    { "posY", Settings.pos.y },
                    { "zoom", Settings.Zoom },
                    { "rot", Settings.rot },
                };

                if (RTMath.TryParse(_val, Settings.pos.y, variables, out float result))
                    Settings.pos.y = result;
            });
            yPositionField.inputField.GetPlaceholderText().text = "Set Pos Y...";
            CoreHelper.Delete(yPositionField.leftGreaterButton);
            CoreHelper.Delete(yPositionField.middleButton);
            CoreHelper.Delete(yPositionField.rightGreaterButton);
            EditorThemeManager.ApplyInputField(yPositionField);
            TriggerHelper.IncreaseDecreaseButtons(yPositionField);
            TriggerHelper.InversableField(yPositionField);

            TriggerHelper.AddEventTriggers(xPositionField.inputField.gameObject,
                TriggerHelper.ScrollDelta(xPositionField.inputField, multi: true),
                TriggerHelper.ScrollDeltaVector2(xPositionField.inputField, yPositionField.inputField));
            TriggerHelper.AddEventTriggers(yPositionField.inputField.gameObject,
                TriggerHelper.ScrollDelta(yPositionField.inputField, multi: true),
                TriggerHelper.ScrollDeltaVector2(xPositionField.inputField, yPositionField.inputField));

            var rotation = EditorPrefabHolder.Instance.NumberInputField.Duplicate(editors.transform, "rot");
            rotationField = rotation.GetComponent<InputFieldStorage>();
            rotationField.OnValueChanged.NewListener(_val =>
            {
                var variables = new Dictionary<string, float>
                {
                    { "posX", Settings.pos.x },
                    { "posY", Settings.pos.y },
                    { "zoom", Settings.Zoom },
                    { "rot", Settings.rot },
                };

                if (RTMath.TryParse(_val, Settings.rot, variables, out float result))
                    Settings.rot = result;
            });
            rotationField.inputField.GetPlaceholderText().text = "Set Rot...";
            CoreHelper.Delete(rotationField.leftGreaterButton);
            CoreHelper.Delete(rotationField.middleButton);
            CoreHelper.Delete(rotationField.rightGreaterButton);
            EditorThemeManager.ApplyInputField(rotationField);
            TriggerHelper.IncreaseDecreaseButtons(rotationField);
            TriggerHelper.InversableField(rotationField);

            TriggerHelper.AddEventTriggers(rotationField.inputField.gameObject, TriggerHelper.ScrollDelta(rotationField.inputField, 15f, 3f));
        }

        void Scroll(PointerEventData pointerEventData)
        {
            if (!Settings)
                return;

            var result = Settings.Zoom;
            var largeKey = EditorConfig.Instance.ScrollwheelLargeAmountKey.Value;
            var smallKey = EditorConfig.Instance.ScrollwheelSmallAmountKey.Value;
            var regularKey = EditorConfig.Instance.ScrollwheelRegularAmountKey.Value;

            bool large = largeKey == KeyCode.None && !Input.GetKey(smallKey) && !Input.GetKey(regularKey) || Input.GetKey(largeKey);
            bool small = smallKey == KeyCode.None && !Input.GetKey(largeKey) && !Input.GetKey(regularKey) || Input.GetKey(smallKey);
            bool regular = regularKey == KeyCode.None && !Input.GetKey(smallKey) && !Input.GetKey(largeKey) || Input.GetKey(regularKey);

            var amount = 0.1f;
            var multiply = 10f;

            if (pointerEventData.scrollDelta.y < 0f)
                result -= small ? amount / multiply : large ? amount * multiply : regular ? amount : 0f;
            if (pointerEventData.scrollDelta.y > 0f)
                result += small ? amount / multiply : large ? amount * multiply : regular ? amount : 0f;

            Settings.Zoom = result;
        }

        void Update()
        {
            var active = Active;
            baseImage.gameObject.SetActive(active);
            if (!active || !Settings)
                return;

            var captureSettings = Settings;

            if (prevMatchSize != MatchSize && MatchSize)
                captureSettings.SetResolutionHeight(captureSettings.Resolution.x);

            if (dragging)
            {
                var mousePosition = startDragPos - (Vector2)RTLevel.Cameras.FG.ScreenToWorldPoint(Input.mousePosition);
                switch (dragType)
                {
                    case DragType.Resolution: {
                            switch (direction)
                            {
                                case Direction.Left: {
                                        captureSettings.SetResolutionWidth(cacheResolution.x - (int)(mousePosition.x * RESOLUTION_MULTIPLY));
                                        if (MatchSize)
                                            captureSettings.SetResolutionHeight(captureSettings.Resolution.x);
                                        captureSettings.Resolution = RTMath.Clamp(captureSettings.Resolution, Vector2Int.zero, ResolutionLimit);
                                        break;
                                    }
                                case Direction.Right: {
                                        captureSettings.SetResolutionWidth(cacheResolution.x + (int)(mousePosition.x * RESOLUTION_MULTIPLY));
                                        if (MatchSize)
                                            captureSettings.SetResolutionHeight(captureSettings.Resolution.x);
                                        captureSettings.Resolution = RTMath.Clamp(captureSettings.Resolution, Vector2Int.zero, ResolutionLimit);
                                        break;
                                    }
                                case Direction.Top: {
                                        captureSettings.SetResolutionHeight(cacheResolution.y - (int)(mousePosition.y * RESOLUTION_MULTIPLY));
                                        if (MatchSize)
                                            captureSettings.SetResolutionWidth(captureSettings.Resolution.y);
                                        captureSettings.Resolution = RTMath.Clamp(captureSettings.Resolution, Vector2Int.zero, ResolutionLimit);
                                        break;
                                    }
                                case Direction.Bottom: {
                                        captureSettings.SetResolutionHeight(cacheResolution.y + (int)(mousePosition.y * RESOLUTION_MULTIPLY));
                                        if (MatchSize)
                                            captureSettings.SetResolutionWidth(captureSettings.Resolution.y);
                                        captureSettings.Resolution = RTMath.Clamp(captureSettings.Resolution, Vector2Int.zero, ResolutionLimit);
                                        break;
                                    }
                            }
                            break;
                        }
                    case DragType.Position: {
                            if (Settings.lockDragMode == CaptureSettings.LockDragMode.PositionX)
                                mousePosition.x = 0f;
                            if (Settings.lockDragMode == CaptureSettings.LockDragMode.PositionY)
                                mousePosition.y = 0f;
                            captureSettings.pos = cachePos - mousePosition;
                            break;
                        }
                    case DragType.Rotation: {
                            captureSettings.rot = cacheRot - mousePosition.x;
                            break;
                        }
                }
            }

            baseImage.rectTransform.anchoredPosition = captureSettings.pos * POSITION_MULTIPLY;
            baseImage.rectTransform.sizeDelta = (Vector2)captureSettings.Resolution * SIZE_MULTIPLY * captureSettings.Zoom;
            baseImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, captureSettings.rot);

            if (zoomSlider && zoomSlider.value != captureSettings.Zoom)
                zoomSlider.SetValueWithoutNotify(captureSettings.Zoom);
            if (xPositionField && xPositionField.inputField && !xPositionField.inputField.isFocused)
                xPositionField.SetTextWithoutNotify(captureSettings.pos.x.ToString());
            if (yPositionField && yPositionField.inputField && !yPositionField.inputField.isFocused)
                yPositionField.SetTextWithoutNotify(captureSettings.pos.y.ToString());
            if (rotationField && rotationField.inputField && !rotationField.inputField.isFocused)
                rotationField.SetTextWithoutNotify(captureSettings.rot.ToString());

            if (editors)
                editors.SetActive(Settings.showEditor);

            prevView = View;
            prevMatchSize = MatchSize;
        }

        void OnDestroy()
        {
            inst = null;
            if (flashAnim)
                AnimationManager.inst.Remove(flashAnim.id);
            flashAnim = null;
            canvas = null;
            baseObject = null;
            baseImage = null;
            outlineImage = null;
        }

        #endregion

        #region Values

        /// <summary>
        /// Settings for the capture area.
        /// </summary>
        public CaptureSettings Settings => RTEditor.inst?.editorInfo?.captureSettings;

        /// <summary>
        /// If the capture area is currently visible.
        /// </summary>
        public bool Active => forceActive;
        bool forceActive;

        bool prevMatchSize;
        /// <summary>
        /// If the capture area's resolution should have a 1:1 aspect ratio.
        /// </summary>
        public bool MatchSize => View == ViewType.Prefab || (Settings?.matchSize ?? false);

        public Vector2Int ResolutionLimit => View switch
        {
            ViewType.Prefab => new Vector2Int(512, 512),
            _ => ResolutionType.p2160.Resolution.ToInt(),
        };

        ViewType prevView;
        /// <summary>
        /// The current view.
        /// </summary>
        public ViewType View
        {
            get
            {
                if (RTPrefabEditor.inst && (RTPrefabEditor.inst.PrefabCreatorDialog && RTPrefabEditor.inst.PrefabCreatorDialog.IsCurrent || RTPrefabEditor.inst.PrefabEditorDialog && RTPrefabEditor.inst.PrefabEditorDialog.IsCurrent))
                    return ViewType.Prefab;
                if (RTEditor.inst && RTEditor.inst.ScreenshotsDialog && RTEditor.inst.ScreenshotsDialog.IsCurrent)
                    return ViewType.Screenshot;
                if (PlayerEditor.inst && PlayerEditor.inst.Dialog && PlayerEditor.inst.Dialog.IsCurrent)
                    return ViewType.PlayerModel;
                return ViewType.Null;
            }
        }

        /// <summary>
        /// Editors where the capture appears in.
        /// </summary>
        public enum ViewType
        {
            /// <summary>
            /// Not assigned.
            /// </summary>
            Null,
            Prefab,
            Screenshot,
            PlayerModel,
        }

        public CaptureSettings copiedSettings;

        const float POSITION_MULTIPLY = 16f;
        const float SIZE_MULTIPLY = 0.8f; // 32 / 40
        const float RESOLUTION_MULTIPLY = 40f;

        RTAnimation flashAnim;

        #region UI

        public UICanvas canvas;

        public GameObject baseObject;
        public Image baseImage;
        public Image outlineImage;

        public Slider zoomSlider;

        public GameObject editors;

        public InputFieldStorage xPositionField;
        public InputFieldStorage yPositionField;
        public InputFieldStorage rotationField;

        #endregion

        #region Dragging

        /// <summary>
        /// Cached resolution value.
        /// </summary>
        public Vector2Int cacheResolution;

        /// <summary>
        /// Cached position value.
        /// </summary>
        public Vector2 cachePos;

        /// <summary>
        /// Cached rotation value.
        /// </summary>
        public float cacheRot;

        /// <summary>
        /// Position of the mouse when the user started dragging the capture area.
        /// </summary>
        public Vector2 startDragPos;
        /// <summary>
        /// If the capture area is being dragged.
        /// </summary>
        public bool dragging;
        /// <summary>
        /// Direction to drag resolution to.
        /// </summary>
        public Direction direction;
        /// <summary>
        /// Drag behavior.
        /// </summary>
        public DragType dragType;

        /// <summary>
        /// Direction to drag resolution to.
        /// </summary>
        public enum Direction
        {
            /// <summary>
            /// Not assigned.
            /// </summary>
            Null,
            Left,
            Right,
            Top,
            Bottom,
        }

        /// <summary>
        /// Drag behavior.
        /// </summary>
        public enum DragType
        {
            /// <summary>
            /// Drags resolution.
            /// </summary>
            Resolution,
            /// <summary>
            /// Drags position.
            /// </summary>
            Position,
            /// <summary>
            /// Drags rotation.
            /// </summary>
            Rotation,
        }

        bool wasDragging;

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Sets the capture active.
        /// </summary>
        /// <param name="active">If the capture should be active.</param>
        public void SetActive(bool active) => forceActive = active;

        /// <summary>
        /// Creates a capture from the current area.
        /// </summary>
        public void CreateCapture()
        {
            switch (View)
            {
                case ViewType.Prefab: {
                        if (RTPrefabEditor.inst.PrefabCreatorDialog.IsCurrent)
                        {
                            RTPrefabEditor.inst.NewPrefabIcon = Capture();
                            RTPrefabEditor.inst.RenderPrefabCreator();
                            return;
                        }

                        if (!RTPrefabEditor.inst.CurrentPrefabPanel)
                            break;

                        RTPrefabEditor.inst.CurrentPrefabPanel.Item.icon = Capture();
                        RTPrefabEditor.inst.CurrentPrefabPanel.RenderPrefabType();
                        RTPrefabEditor.inst.UpdatePrefabFile(RTPrefabEditor.inst.CurrentPrefabPanel);
                        RTPrefabEditor.inst.RenderPrefabEditorDialog(RTPrefabEditor.inst.CurrentPrefabPanel);
                        break;
                    }
                case ViewType.Screenshot: {
                        var capture = Capture();

                        string directory = RTFile.CombinePaths(RTFile.ApplicationDirectory, CoreConfig.Instance.ScreenshotsPath.Value);
                        RTFile.CreateDirectory(directory);

                        var file = RTFile.CombinePaths(directory, DateTime.Now.ToString(LegacyPlugin.DATE_TIME_FORMAT) + FileFormat.PNG.Dot());
                        File.WriteAllBytes(file, capture.texture.EncodeToPNG());

                        break;
                    }
                case ViewType.PlayerModel: {
                        var playerModel = PlayerEditor.inst.CurrentModel;
                        if (!playerModel)
                            return;

                        playerModel.icon = Capture();

                        break;
                    }
            }
        }

        /// <summary>
        /// Clears the capture from a related view.
        /// </summary>
        public void ClearCapture()
        {
            switch (View)
            {
                case ViewType.Prefab: {
                        if (RTPrefabEditor.inst.PrefabCreatorDialog.IsCurrent)
                        {
                            RTPrefabEditor.inst.NewPrefabIcon = null;
                            RTPrefabEditor.inst.RenderPrefabCreator();
                            return;
                        }

                        if (!RTPrefabEditor.inst.CurrentPrefabPanel)
                            break;

                        RTPrefabEditor.inst.CurrentPrefabPanel.Item.icon = null;
                        RTPrefabEditor.inst.CurrentPrefabPanel.Item.iconData = null;
                        RTPrefabEditor.inst.CurrentPrefabPanel.RenderPrefabType();
                        RTPrefabEditor.inst.UpdatePrefabFile(RTPrefabEditor.inst.CurrentPrefabPanel);
                        RTPrefabEditor.inst.RenderPrefabEditorDialog(RTPrefabEditor.inst.CurrentPrefabPanel);
                        break;
                    }
                case ViewType.PlayerModel: {
                        var playerModel = PlayerEditor.inst.CurrentModel;
                        if (!playerModel)
                            return;

                        playerModel.icon = null;

                        break;
                    }
            }
        }

        /// <summary>
        /// Creates a capture from the current area.
        /// </summary>
        /// <returns>Returns the sprite created from the captured area.</returns>
        public Sprite Capture()
        {
            var captureSettings = Settings;
            if (!Settings)
                return null;

            baseObject.SetActive(false);
            var rect = RTLevel.Cameras.FG.rect;
            RTGameManager.inst.SetCameraArea(new Rect(0f, 0f, 1f, 1f));
            var total = captureSettings.Resolution.x + captureSettings.Resolution.y;
            RTLevel.Current.eventEngine.SetZoom(total / 2 / 512f * 12.66f * captureSettings.Zoom);

            RTLevel.Current.eventEngine.SetCameraPosition(captureSettings.captureAllLayers ? captureSettings.pos : Vector2.zero);
            RTLevel.Current.eventEngine.SetCameraRotation(captureSettings.captureAllLayers ? captureSettings.rot : 0f);

            var trackerPos = RTEventManager.inst.delayTracker.transform.localPosition;
            RTEventManager.inst.delayTracker.transform.localPosition = Vector2.zero;

            var objectToHide = View == ViewType.PlayerModel ? ObjectManager.inst.objectParent : GameManager.inst.players;
            var playersActive = objectToHide.activeSelf;
            if (captureSettings.hidePlayers)
                objectToHide.SetActive(false);

            var clearFlags = RTLevel.Cameras.FG.clearFlags;
            var bgColor = RTLevel.Cameras.FG.backgroundColor;
            if (captureSettings.useCustomBGColor && !captureSettings.captureAllLayers)
            {
                RTLevel.Cameras.FG.clearFlags = CameraClearFlags.SolidColor;
                RTLevel.Cameras.FG.backgroundColor = captureSettings.customBGColor;
            }

            var icon = captureSettings.captureAllLayers ?
                SpriteHelper.CaptureAllFrame(
                    move: captureSettings.move,
                    width: captureSettings.Resolution.x,
                    height: captureSettings.Resolution.y,
                    offsetX: 0f,
                    offsetY: 0f,
                    rotationOffset: 0f) :
                SpriteHelper.CaptureFrame(
                    camera: RTLevel.Cameras.FG,
                    move: captureSettings.move,
                    width: captureSettings.Resolution.x,
                    height: captureSettings.Resolution.y,
                    offsetX: captureSettings.pos.x,
                    offsetY: captureSettings.pos.y,
                    rotationOffset: captureSettings.rot);

            var editorCam = RTEditor.inst && RTEditor.inst.Freecam;

            RTLevel.Current.eventEngine.SetCameraRotation(editorCam ?
                new Vector3(RTLevel.Current.eventEngine.editorCamPerRotate.x, RTLevel.Current.eventEngine.editorCamPerRotate.y, RTLevel.Current.eventEngine.editorCamRotate) :
                new Vector3(RTLevel.Current.eventEngine.camRotOffset.x, RTLevel.Current.eventEngine.camRotOffset.y, EventManager.inst.camRot));

            RTLevel.Current.eventEngine.SetCameraPosition(editorCam ?
                RTLevel.Current.eventEngine.editorCamPosition :
                EventManager.inst.camPos);

            // fixes bg camera position being offset if rotated for some reason...
            RTLevel.Cameras.BG.transform.SetLocalPositionX(0f);
            RTLevel.Cameras.BG.transform.SetLocalPositionY(0f);

            RTLevel.Current.eventEngine.SetZoom(editorCam ?
                RTLevel.Current.eventEngine.editorCamZoom :
                EventManager.inst.camZoom);

            RTLevel.Current.eventEngine.UpdateShake();

            RTEventManager.inst.delayTracker.transform.localPosition = trackerPos;

            baseObject.SetActive(true);

            // disable and re-enable the glitch camera to ensure the glitch camera is ordered last.
            RTEventManager.inst.glitchCam.enabled = false;
            RTEventManager.inst.glitchCam.enabled = true;

            // disable and re-enable the UI camera to ensure the UI camera is ordered last.
            RTLevel.Cameras.UI.enabled = false;
            RTLevel.Cameras.UI.enabled = true;

            if (captureSettings.hidePlayers)
                objectToHide.SetActive(true);

            if (captureSettings.useCustomBGColor && !captureSettings.captureAllLayers)
            {
                RTLevel.Cameras.FG.clearFlags = clearFlags;
                RTLevel.Cameras.FG.backgroundColor = bgColor;
            }

            RTGameManager.inst.SetCameraArea(rect);

            SoundManager.inst.PlaySound(DefaultSounds.menuflip);

            if (flashAnim)
                AnimationManager.inst.Remove(flashAnim.id);
            flashAnim = new RTAnimation("Flash");
            flashAnim.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<Color>(new List<IKeyframe<Color>>
                {
                    new ColorKeyframe(0f, Color.white, Ease.Linear),
                    new ColorKeyframe(0.4f, new Color(0.1f, 0.1f, 0.1f, 0.01f), Ease.SineOut),
                }, col =>
                {
                    if (baseImage)
                        baseImage.color = col;
                }, interpolateOnComplete: true),
            };
            flashAnim.onComplete = () =>
            {
                AnimationManager.inst.Remove(flashAnim.id);
                flashAnim = null;
            };
            AnimationManager.inst.Play(flashAnim);
            return icon;
        }

        void StartDrag(Direction direction, DragType dragType)
        {
            var captureSettings = Settings;
            if (!Settings)
                return;

            dragging = true;
            this.direction = direction;
            this.dragType = dragType;
            startDragPos = RTLevel.Cameras.FG.ScreenToWorldPoint(Input.mousePosition);

            cacheResolution = captureSettings.Resolution;
            cachePos = captureSettings.pos;
            cacheRot = captureSettings.rot;
        }

        #endregion
    }
}
