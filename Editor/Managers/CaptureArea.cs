using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
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
            canvas.SetWorldSpace(RTLevel.FOREGROUND_LAYER, RTLevel.Cameras.FG);
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
                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Create Capture", CreateCapture),
                        new ButtonFunction("Clear", ClearCapture),
                        new ButtonFunction(true),
                        new ButtonFunction("Reset All", RTEditor.inst.editorInfo.captureSettings.Reset),
                        new ButtonFunction("Reset Resolution", () => RTEditor.inst.editorInfo.captureSettings.Resolution = new Vector2Int(512, 512)),
                        new ButtonFunction("Reset Position", () => RTEditor.inst.editorInfo.captureSettings.pos = Vector2.zero),
                        new ButtonFunction("Reset Rotation", () => RTEditor.inst.editorInfo.captureSettings.rot = 0f));
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
                                        break;
                                    }
                                case Direction.Right: {
                                        captureSettings.SetResolutionWidth(cacheResolution.x + (int)(mousePosition.x * RESOLUTION_MULTIPLY));
                                        if (MatchSize)
                                            captureSettings.SetResolutionHeight(captureSettings.Resolution.x);
                                        break;
                                    }
                                case Direction.Top: {
                                        captureSettings.SetResolutionHeight(cacheResolution.y - (int)(mousePosition.y * RESOLUTION_MULTIPLY));
                                        if (MatchSize)
                                            captureSettings.SetResolutionWidth(captureSettings.Resolution.y);
                                        break;
                                    }
                                case Direction.Bottom: {
                                        captureSettings.SetResolutionHeight(cacheResolution.y + (int)(mousePosition.y * RESOLUTION_MULTIPLY));
                                        if (MatchSize)
                                            captureSettings.SetResolutionWidth(captureSettings.Resolution.y);
                                        break;
                                    }
                            }
                            break;
                        }
                    case DragType.Position: {
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
            baseImage.rectTransform.sizeDelta = (Vector2)captureSettings.Resolution * SIZE_MULTIPLY;
            baseImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, captureSettings.rot);

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
        public bool Active => forceActive || View != ViewType.Null;
        bool forceActive;

        bool prevMatchSize;
        /// <summary>
        /// If the capture area's resolution should have a 1:1 aspect ratio.
        /// </summary>
        public bool MatchSize => View == ViewType.Prefab || (Settings?.matchSize ?? false);

        ViewType prevView;
        /// <summary>
        /// The current view.
        /// </summary>
        public ViewType View
        {
            get
            {
                if (RTPrefabEditor.inst && (RTPrefabEditor.inst.PrefabEditorDialog && RTPrefabEditor.inst.PrefabEditorDialog.IsCurrent))
                    return ViewType.Prefab;
                if (RTEditor.inst && RTEditor.inst.ScreenshotsDialog && RTEditor.inst.ScreenshotsDialog.IsCurrent)
                    return ViewType.Screenshot;
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
        }

        const float POSITION_MULTIPLY = 16f;
        const float SIZE_MULTIPLY = 0.8f; // 32 / 40
        const float RESOLUTION_MULTIPLY = 40f;

        RTAnimation flashAnim;

        #region UI

        public UICanvas canvas;

        public GameObject baseObject;
        public Image baseImage;
        public Image outlineImage;

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

                        var file = RTFile.CombinePaths(directory, DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss") + FileFormat.PNG.Dot());
                        File.WriteAllBytes(file, capture.texture.EncodeToPNG());

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
                        if (!RTPrefabEditor.inst.CurrentPrefabPanel)
                            break;

                        RTPrefabEditor.inst.CurrentPrefabPanel.Item.icon = null;
                        RTPrefabEditor.inst.CurrentPrefabPanel.Item.iconData = null;
                        RTPrefabEditor.inst.CurrentPrefabPanel.RenderPrefabType();
                        RTPrefabEditor.inst.UpdatePrefabFile(RTPrefabEditor.inst.CurrentPrefabPanel);
                        RTPrefabEditor.inst.RenderPrefabEditorDialog(RTPrefabEditor.inst.CurrentPrefabPanel);
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
            var total = captureSettings.Resolution.x + captureSettings.Resolution.y;
            RTLevel.Current.eventEngine.SetZoom(((total / 2) / 512f) * 12.66f);

            var icon = SpriteHelper.CaptureFrame(
                camera: RTLevel.Cameras.FG,
                move: captureSettings.move,
                width: captureSettings.Resolution.x,
                height: captureSettings.Resolution.y,
                offsetX: captureSettings.pos.x,
                offsetY: captureSettings.pos.y,
                rotationOffset: captureSettings.rot);

            RTLevel.Current.eventEngine.SetZoom(EventManager.inst.camZoom);
            baseObject.SetActive(true);
            RTLevel.Cameras.UI.enabled = false;
            RTLevel.Cameras.UI.enabled = true;

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
