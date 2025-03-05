using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SimpleJSON;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Animation;
using BetterLegacy.Configs;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Arcade.Interfaces;
using BetterLegacy.Companion.Data;

namespace BetterLegacy.Companion.Entity
{
    /// <summary>
    /// Represents Example's model.
    /// </summary>
    public class ExampleModel : ExampleModule
    {
        public ExampleModel() { }

        #region Default Instance

        /// <summary>
        /// The default Example model.
        /// </summary>
        public static ExampleModel Default
        {
            get
            {
                var model = new ExampleModel();
                model.InitDefault();

                return model;
            }
        }

        public override void InitDefault()
        {
            attributes.Clear();
            AddAttribute("POKING_EYES", 0.0, 0.0, 1.0);
            AddAttribute("ALLOW_BLINKING", 1.0, 0.0, 1.0);
            AddAttribute("PUPILS_CAN_CHANGE", 1.0, 0.0, 1.0);
            AddAttribute("FACE_CAN_LOOK", 1.0, 0.0, 1.0);

            parts.Clear();
            parts.Add(ParentPart.Default.ID("BASE").Name("Base")
            .OnTick(part =>
            {
                // render the models' position
                if (!part || !part.transform)
                    return;

                part.transform.localPosition = position + new Vector2(0f, (Ease.SineInOut(reference.timer.time * 0.5f % 2f) - 0.5f) * 2f);
                part.transform.localScale = scale;
                part.rotation = rotation;
            }));

            parts.Add(ImagePart.Default.ID("HEAD").ParentID("BASE").Name("Head").ImagePath(RTFile.GetAsset("Example Parts/example head.png"))
            .OnTick(part =>
            {
                // tick function

                if (!reference || !reference.dragging)
                    return;

                rotation = (reference.DragTarget.x - reference.dragPos.x) * reference.DragDelay;

                reference.dragPos += (reference.DragTarget - reference.dragPos) * reference.DragDelay;

                position = new Vector3(Mathf.Clamp(reference.dragPos.x, -970f, 970f), Mathf.Clamp(reference.dragPos.y, -560f, 560f));
            })
            .OnClick((part, pointerEventData) =>
            {
                // onClick function

                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                reference?.options?.Toggle();
            })
            .OnDown((part, pointerEventData) =>
            {
                // onDown function

                if (!reference || !reference.canDrag || reference.leaving)
                    return;

                // start dragging

                if (pointerEventData.button != PointerEventData.InputButton.Left || !reference.canDrag)
                    return;

                reference?.brain?.Interact(ExampleInteractions.PET);

                CompanionManager.inst.animationController.Remove(x => x.name == "End Drag Example" || x.name == "Drag Example" || x.name.ToLower().Contains("movement"));

                reference.startMousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y) * CoreHelper.ScreenScaleInverse;
                reference.startDragPos = new Vector2(position.x, position.y);
                reference.dragPos = new Vector3(position.x, position.y);
                reference.dragging = true;

                SoundManager.inst.PlaySound(baseCanvas, DefaultSounds.example_speak, UnityEngine.Random.Range(0.6f, 0.7f), UnityEngine.Random.Range(1.1f, 1.3f));

                if (reference.brain.dancing)
                {
                    reference.brain.Interact(ExampleInteractions.INTERRUPT);
                    reference.brain.StopDancing();
                }

                SetPose(Poses.START_DRAG);
            })
            .OnUp((part, pointerEventData) =>
            {
                // onUp function

                if (!reference || !reference.dragging || reference.leaving)
                    return;

                // end dragging

                facePosition = Vector3.zero;

                reference.dragging = false;

                SetPose(Poses.END_DRAG);
            }));

            parts.Add(ParentPart.Default.ID("FACE").ParentID("HEAD").Name("Face")
            .OnTick(part =>
            {
                // tick function

                if (!part || !part.transform)
                    return;

                if (GetAttribute("FACE_CAN_LOOK").Value == 1.0)
                {
                    var lerp = RTMath.Lerp(Vector2.zero, reference.brain.LookingAt - new Vector2(part.transform.position.x, part.transform.position.y), CompanionManager.FACE_LOOK_MULTIPLIER);
                    part.transform.localPosition = new Vector3(lerp.x, lerp.y, 0f);
                    facePosition = part.transform.localPosition;
                }
                else
                    part.transform.localPosition = facePosition;

                if (reference.dragging)
                    facePosition = new Vector2(
                        Mathf.Clamp(-((reference.DragTarget.x - reference.dragPos.x) * reference.DragDelay), -14f, 14f),
                        Mathf.Clamp(-((reference.DragTarget.y - reference.dragPos.y) * reference.DragDelay), -14f, 14f));
            }));

            #region Ears

            parts.Add(ParentPart.Default.ID("EARS").ParentID("HEAD").SiblingIndex(0).Name("Ears")
            .OnTick(part =>
            {
                // tick function

                part.rotation = facePosition.x * 0.8f;
            }));

            parts.Add(ImagePart.Default.ID("EAR_BOTTOM_LEFT").ParentID("EARS").Name("Ear Bottom Left").ImagePath(RTFile.GetAsset("Example Parts/example ear bottom.png"))
            .Rect(RectValues.Default.AnchoredPosition(25f, 35f).Rotation(-30f))
            .ImageRect(RectValues.Default.Pivot(0.5f, 0.2f).SizeDelta(44f, 52f)));

            parts.Add(ImagePart.Default.ID("EAR_TOP_LEFT").ParentID("EAR_BOTTOM_LEFT").Name("Ear Top Left").ImagePath(RTFile.GetAsset("Example Parts/example ear top.png"))
            .Rect(RectValues.Default)
            .ImageRect(RectValues.Default.AnchoredPosition(0f, 45f).Pivot(0.5f, 0.275f).SizeDelta(44f, 80f).Rotation(-90f))
            .OnClick((part, pointerEventData) =>
            {
                var animation = new RTAnimation("Ear Left Flick");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, 0f, Ease.Linear),
                        new FloatKeyframe(0.1f, -30f, Ease.SineOut),
                        new FloatKeyframe(0.7f, 0f, Ease.SineInOut),
                    }, x => part.parent.rotation = x),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, 0f, Ease.Linear),
                        new FloatKeyframe(0.05f, -50f, Ease.Linear),
                        new FloatKeyframe(0.3f, 30f, Ease.SineOut),
                        new FloatKeyframe(0.9f, 0f, Ease.SineInOut),
                    }, x => part.imageRotation = x),
                };
                animation.onComplete = () =>
                {
                    CompanionManager.inst.animationController.Remove(animation.id);
                };
                CompanionManager.inst.animationController.Play(animation);
            }));

            parts.Add(ImagePart.Default.ID("EAR_BOTTOM_RIGHT").ParentID("EARS").Name("Ear Bottom Right").ImagePath(RTFile.GetAsset("Example Parts/example ear bottom.png"))
            .Rect(RectValues.Default.AnchoredPosition(-25f, 35f).Rotation(30f))
            .ImageRect(RectValues.Default.Pivot(0.5f, 0.2f).SizeDelta(44f, 52f)));

            parts.Add(ImagePart.Default.ID("EAR_TOP_RIGHT").ParentID("EAR_BOTTOM_RIGHT").Name("Ear Top Right").ImagePath(RTFile.GetAsset("Example Parts/example ear top.png"))
            .Rect(RectValues.Default)
            .ImageRect(RectValues.Default.AnchoredPosition(0f, 45f).Pivot(0.5f, 0.275f).SizeDelta(44f, 80f).Rotation(90f))
            .OnClick((part, pointerEventData) =>
            {
                var animation = new RTAnimation("Ear Right Flick");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, 0f, Ease.Linear),
                        new FloatKeyframe(0.1f, 30f, Ease.SineOut),
                        new FloatKeyframe(0.7f, 0f, Ease.SineInOut),
                    }, x => part.parent.rotation = x),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, 0f, Ease.Linear),
                        new FloatKeyframe(0.05f, 50f, Ease.Linear),
                        new FloatKeyframe(0.3f, -30f, Ease.SineOut),
                        new FloatKeyframe(0.9f, 0f, Ease.SineInOut),
                    }, x => part.imageRotation = x),
                };
                animation.onComplete = () =>
                {
                    CompanionManager.inst.animationController.Remove(animation.id);
                };
                CompanionManager.inst.animationController.Play(animation);
            }));

            #endregion

            parts.Add(ImagePart.Default.ID("TAIL").ParentID("HEAD").SiblingIndex(0).Name("Tail").ImagePath(RTFile.GetAsset("Example Parts/example tail.png"))
            .ImageRect(RectValues.Default.AnchoredPosition(0f, -58f).SizeDelta(28f, 42f))
            .OnTick(part =>
            {
                // tick function

                if (!part || !part.transform)
                    return;

                float faceXPos = facePosition.x * CompanionManager.FACE_X_MULTIPLIER;
                part.rotation = -faceXPos;
                ((ImagePart)part).imageRotation = -faceXPos;
            })
            .OnClick((part, pointerEventData) =>
            {
                if (!reference.leaving)
                    reference?.brain?.Interact(ExampleInteractions.TOUCHIE);
            }));

            #region Eyes

            parts.Add(ImagePart.Default.ID("EYES").ParentID("FACE").Name("Eyes").ImagePath(RTFile.GetAsset("Example Parts/example eyes.png"))
            .ImageRect(RectValues.Default.SizeDelta(74f, 34f))
            .OnDown((part, pointerEventData) => GetAttribute("POKING_EYES").Value = 1.0)
            .OnUp((part, pointerEventData) => GetAttribute("POKING_EYES").Value = 0.0));

            parts.Add(ImagePart.Default.ID("PUPILS").ParentID("EYES").Name("Pupils").ImagePath(RTFile.GetAsset("Example Parts/example pupils.png"))
            .ImageRect(RectValues.Default.SizeDelta(47f, 22f))
            .OnTick(part =>
            {
                if (!part || !part.transform)
                    return;

                float t = reference.timer.time % CompanionManager.PUPILS_LOOK_RATE;

                // Here we add a tiny amount of movement to the pupils to make Example feel a lot more alive.
                if (t > CompanionManager.PUPILS_LOOK_RATE - 0.3f && GetAttribute("PUPILS_CAN_CHANGE").Value == 1.0)
                    pupilsOffset = new Vector2(UnityEngine.Random.Range(0f, 0.5f), UnityEngine.Random.Range(0f, 0.5f));

                GetAttribute("PUPILS_CAN_CHANGE").Value = (t <= CompanionManager.PUPILS_LOOK_RATE - 0.3f) ? 0.0 : 1.0;

                var pupils = part.transform;
                pupils.AsRT().anchoredPosition = RTMath.Lerp(Vector2.zero, reference.brain.LookingAt - new Vector2(pupils.position.x, pupils.position.y), 0.004f) + pupilsOffset;
            })
            .OnDown((part, pointerEventData) => GetAttribute("POKING_EYES").Value = 1.0)
            .OnUp((part, pointerEventData) => GetAttribute("POKING_EYES").Value = 0.0));

            parts.Add(ImagePart.Default.ID("BLINK").ParentID("EYES").Name("Blink").ImagePath(RTFile.GetAsset("Example Parts/example blink.png"))
            .ImageRect(RectValues.Default.SizeDelta(74f, 34f))
            .OnTick(part =>
            {
                if (!part || !part.gameObject)
                    return;

                float t = reference.timer.time % CompanionManager.BLINK_RATE;

                if (!reference.Dragging && GetAttribute("ALLOW_BLINKING").Value == 1.0 && GetAttribute("DANCING").Value == 0.0 && GetAttribute("POKING_EYES").Value == 0.0)
                {
                    var blinkingAttribute = GetAttribute("IS_BLINKING");
                    var canUnblinkAttribute = GetAttribute("CAN_UNBLINK");
                    if (t > CompanionManager.BLINK_RATE - 0.3f && canUnblinkAttribute.Value == 1.0)
                        blinkingAttribute.Value = RandomHelper.PercentChance(45) ? 1.0 : 0.0;

                    var active = t > CompanionManager.BLINK_RATE - 0.3f && blinkingAttribute.Value == 1.0;
                    canUnblinkAttribute.Value = active ? 0.0 : 1.0;
                    part.gameObject.SetActive(active);
                }
                else
                    part.gameObject.SetActive(true);
            }));

            parts.Add(ParentPart.Default.ID("BROWS").ParentID("FACE").Name("Brows")
            .Rect(RectValues.Default.AnchoredPosition(0f, 30f)));

            parts.Add(ImagePart.Default.ID("BROW_LEFT").ParentID("BROWS").Name("Brow Left").ImagePath(RTFile.GetAsset("Example Parts/example brow.png"))
            .Rect(RectValues.Default.AnchoredPosition(22f, 0f))
            .ImageRect(RectValues.Default.AnchoredPosition(18f, 0f).Pivot(1.7f, 0.5f).SizeDelta(20f, 6f)));

            parts.Add(ImagePart.Default.ID("BROW_RIGHT").ParentID("BROWS").Name("Brow Right").ImagePath(RTFile.GetAsset("Example Parts/example brow.png"))
            .Rect(RectValues.Default.AnchoredPosition(-22f, 0f))
            .ImageRect(RectValues.Default.AnchoredPosition(-18f, 0f).Pivot(-0.7f, 0.5f).SizeDelta(20f, 6f)));

            #endregion

            #region Snout

            parts.Add(ImagePart.Default.ID("SNOUT").ParentID("FACE").Name("Snout").ImagePath(RTFile.GetAsset("Example Parts/example snout.png"))
            .ImageRect(RectValues.Default.AnchoredPosition(0f, -31f).SizeDelta(60f, 31f)));

            parts.Add(ParentPart.Default.ID("MOUTH_BASE").ParentID("SNOUT").Name("Mouth Base")
            .Rect(RectValues.Default.AnchoredPosition(0f, -30f))
            .OnTick(part =>
            {
                // tick function

                if (part && part.transform)
                    part.transform.localPosition = new Vector3(facePosition.x, (facePosition.y * 0.5f) + -30f, 0f);
            }));

            parts.Add(ImagePart.Default.ID("MOUTH_UPPER").ParentID("MOUTH_BASE").Name("Mouth Upper").ImagePath(RTFile.GetAsset("Example Parts/example mouth.png"))
            .Rect(RectValues.Default.Scale(1f, 0.15f).Rotation(180f))
            .ImageRect(RectValues.Default.Pivot(0.5f, 1f).SizeDelta(32f, 16f)));

            parts.Add(ImagePart.Default.ID("MOUTH_LOWER").ParentID("MOUTH_BASE").Name("Mouth Lower").ImagePath(RTFile.GetAsset("Example Parts/example mouth.png"))
            .Rect(RectValues.Default.Scale(1f, 0f))
            .ImageRect(RectValues.Default.Pivot(0.5f, 1f).SizeDelta(32f, 16f))
            .OnTick(part =>
            {
                if (!part || !part.transform)
                    return;

                part.transform.SetLocalScaleY(Mathf.Clamp(mouthOpenAmount, 0f, 1f));
            }));

            parts.Add(ImagePart.Default.ID("LIPS").ParentID("MOUTH_BASE").Name("Lips").ImagePath(RTFile.GetAsset("Example Parts/example lips.png"))
            .ImageRect(RectValues.Default.AnchoredPosition(0f, 3f).Pivot(0.5f, 1f).SizeDelta(32f, 8f)));

            parts.Add(ImagePart.Default.ID("NOSE").ParentID("SNOUT").Name("Nose").ImagePath(RTFile.GetAsset("Example Parts/example nose.png"))
            .Rect(RectValues.Default.AnchoredPosition(0f, -20f))
            .ImageRect(RectValues.Default.SizeDelta(22f, 8f))
            .OnTick(part =>
            {
                // tick function

                if (!part || !part.transform)
                    return;

                part.transform.localPosition = new Vector3(facePosition.x, (facePosition.y * 0.5f) + -20f, 0f);
            }));

            #endregion

            #region Hands

            parts.Add(ParentPart.Default.ID("HANDS_BASE").ParentID("BASE").Name("Hands Base"));

            parts.Add(ImagePart.Default.ID("HAND_LEFT").ParentID("HANDS_BASE").Name("Hand Left").ImagePath(RTFile.GetAsset("Example Parts/example hand.png"))
            .Rect(RectValues.Default.AnchoredPosition(40f, 0f))
            .ImageRect(RectValues.Default.AnchoredPosition(0f, -80f).SizeDelta(42f, 42f))
            .OnTick(part =>
            {
                if (reference && part && part.transform && reference.draggingLeftHand)
                    part.transform.localPosition += ((Vector3)reference.DragTarget - part.transform.localPosition) * reference.DragDelay;
            })
            .OnDown((part, pointerEventData) =>
            {
                if (reference.leaving)
                    return;

                reference?.brain?.Interact(ExampleInteractions.HOLD_HAND);
                if (!reference || !reference.canDragLeftHand || !part || !part.transform)
                    return;

                reference.startMousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y) * CoreHelper.ScreenScaleInverse;
                reference.startDragPos = new Vector2(part.transform.localPosition.x, part.transform.localPosition.y);
                reference.draggingLeftHand = true;

                if (reference.brain.dancing)
                    reference.brain.StopDancing();
            })
            .OnUp((part, pointerEventData) =>
            {
                if (!reference || !reference.canDragLeftHand || !part || !part.transform || reference.leaving)
                    return;

                reference.draggingLeftHand = false;

                try
                {
                    SelectObject(part.image);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }

                var animation = new RTAnimation("Example Hand Reset")
                {
                    animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                        {
                            new Vector2Keyframe(0f, part.transform.AsRT().anchoredPosition, Ease.Linear),
                            new Vector2Keyframe(0.3f, new Vector2(40f, 0f), Ease.SineOut),
                            new Vector2Keyframe(0.32f, new Vector2(40f, 0f), Ease.Linear),
                        }, x =>
                        {
                            if (part && part.transform)
                                part.transform.AsRT().anchoredPosition = x;
                        }, interpolateOnComplete: true),
                    },
                };
                animation.onComplete = () =>
                {
                    CompanionManager.inst.animationController.Remove(animation.id);
                };
                CompanionManager.inst.animationController.Play(animation);
            }));

            parts.Add(ImagePart.Default.ID("HAND_RIGHT").ParentID("HANDS_BASE").Name("Hand Right").ImagePath(RTFile.GetAsset("Example Parts/example hand.png"))
            .Rect(RectValues.Default.AnchoredPosition(-40f, 0f))
            .ImageRect(RectValues.Default.AnchoredPosition(0f, -80f).SizeDelta(42f, 42f))
            .OnTick(part =>
            {
                if (reference && part && part.transform && reference.draggingRightHand)
                    part.transform.localPosition += ((Vector3)reference.DragTarget - part.transform.localPosition) * reference.DragDelay;
            })
            .OnDown((part, pointerEventData) =>
            {
                if (reference.leaving)
                    return;

                reference?.brain?.Interact(ExampleInteractions.HOLD_HAND);
                if (!reference || !reference.canDragRightHand || !part || !part.transform)
                    return;

                reference.startMousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y) * CoreHelper.ScreenScaleInverse;
                reference.startDragPos = new Vector2(part.transform.localPosition.x, part.transform.localPosition.y);
                reference.draggingRightHand = true;

                if (reference.brain.dancing)
                    reference.brain.StopDancing();
            })
            .OnUp((part, pointerEventData) =>
            {
                if (!reference || !reference.canDragRightHand || !part || !part.transform || reference.leaving)
                    return;

                reference.draggingRightHand = false;

                try
                {
                    SelectObject(part.image);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }

                var animation = new RTAnimation("Example Hand Reset")
                {
                    animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                        {
                            new Vector2Keyframe(0f, part.transform.AsRT().anchoredPosition, Ease.Linear),
                            new Vector2Keyframe(0.3f, new Vector2(-40f, 0f), Ease.SineOut),
                            new Vector2Keyframe(0.32f, new Vector2(-40f, 0f), Ease.Linear),
                        }, x =>
                        {
                            if (part && part.transform)
                                part.transform.AsRT().anchoredPosition = x;
                        }, interpolateOnComplete: true),
                    },
                };
                animation.onComplete = () =>
                {
                    CompanionManager.inst.animationController.Remove(animation.id);
                };
                CompanionManager.inst.animationController.Play(animation);
            }));

            #endregion

            startDancing = () =>
            {
                if (!reference || !reference.brain)
                    return;

                Debug.Log($"{CompanionManager.className}Example has started dancing!");
                PlayDanceAnimation();
            };
            stopDancing = () =>
            {
                if (!reference || !reference.brain)
                    return;

                Debug.Log($"{CompanionManager.className}Example has stopped dancing!");
                StopDanceAnimation();
            };

            RegisterPoses();
        }

        #endregion

        #region Core

        /// <summary>
        /// Builds Example's model.
        /// </summary>
        public override void Build()
        {
            preBuild?.Invoke(this);

            if (baseCanvas)
                CoreHelper.Destroy(baseCanvas);

            var uiCanvas = UIManager.GenerateUICanvas("Example Canvas", null, true);

            baseCanvas = uiCanvas.GameObject;
            canvas = uiCanvas.Canvas;
            canvasGroup = uiCanvas.CanvasGroup;

            blocker = Creator.NewUIObject("Interaction Blocker", baseCanvas.transform);
            blocker.transform.AsRT().anchoredPosition = Vector2.zero;
            blocker.transform.AsRT().sizeDelta = new Vector2(10000f, 10000f);
            var blockerImage = blocker.AddComponent<Image>();
            blockerImage.color = new Color(0f, 0f, 0f, 0.3f);
            blocker.SetActive(false);

            // build the parts
            for (int i = 0; i < parts.Count; i++)
            {
                var part = parts[i];

                part.Build();

                if (basePartID == part.id)
                    baseParent = part.transform;

                if (!string.IsNullOrEmpty(part.parentID))
                    part.parent = parts.Find(x => x.id == part.parentID);
            }

            // set the parents
            for (int i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                if (!part.parent)
                    continue;

                part.parent.child = part;

                if (!part.parent.transform)
                    continue;

                // cache previous transform values
                var pos = part.transform.localPosition;
                var sca = part.transform.localScale;
                var rot = part.transform.localRotation;

                part.transform.SetParent(part.parent.transform);
                if (part.siblingIndex >= 0 && part.siblingIndex < part.parent.transform.childCount)
                    part.transform.SetSiblingIndex(part.siblingIndex);

                // fixes offset transform values
                part.transform.localPosition = pos;
                part.transform.localScale = sca;
                part.transform.localRotation = rot;
            }

            if (baseParent)
                baseParent.transform.SetParent(baseCanvas.transform);

            CoreHelper.Log($"Base parent: {baseParent}");

            postBuild?.Invoke(this);
        }

        /// <summary>
        /// Updates Example's model.
        /// </summary>
        public override void Tick()
        {
            onPreTick?.Invoke(this);

            if (baseCanvas && canvas)
            {
                UpdateActive();
                canvas.scaleFactor = CoreHelper.ScreenScale;
            }

            if (!Application.isFocused || !Visible)
                return;

            for (int i = 0; i < parts.Count; i++)
                parts[i].Tick();

            onPostTick?.Invoke(this);

            if (canvasGroup)
                canvasGroup.alpha = ExampleConfig.Instance.IsTransparent.Value ? ExampleConfig.Instance.TransparencyOpacity.Value : 1f;
        }

        /// <summary>
        /// Clears Example's model.
        /// </summary>
        public override void Clear()
        {
            CoreHelper.Destroy(baseCanvas);
            parts.Clear();
            attributes.Clear();

            if (startDragAnimation)
            {
                CompanionManager.inst.animationController.Remove(startDragAnimation.id);
                startDragAnimation = null;
            }

            if (endDragAnimation)
            {
                CompanionManager.inst.animationController.Remove(endDragAnimation.id);
                endDragAnimation = null;
            }

            if (danceLoopAnimation)
            {
                CompanionManager.inst.animationController.Remove(danceLoopAnimation.id);
                danceLoopAnimation = null;
            }
        }

        #endregion

        #region Active State

        /// <summary>
        /// If the canvas is active.
        /// </summary>
        public bool Visible => baseCanvas && baseCanvas.activeSelf;

        /// <summary>
        /// Active state of the model.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Sets the active state and updates the canvas.
        /// </summary>
        /// <param name="active">Active state to set.</param>
        public void SetActive(bool active)
        {
            Active = active;
            UpdateActive();
        }

        /// <summary>
        /// Updates the canvas' active state.
        /// </summary>
        public void UpdateActive()
        {
            if (!baseCanvas)
                return;

            if (EditorManager.inst && EditorManager.inst.isEditing)
                baseCanvas.SetActive(Active && ExampleConfig.Instance.EnabledInEditor.Value);
            else if (GameManager.inst)
                baseCanvas.SetActive(Active && ExampleConfig.Instance.EnabledInGame.Value);
            else if (Menus.MenuManager.inst && Menus.MenuManager.inst.ic || Menus.InterfaceManager.inst && Menus.InterfaceManager.inst.CurrentInterface || LoadLevelsManager.inst)
                baseCanvas.SetActive(Active && ExampleConfig.Instance.EnabledInMenus.Value);
            else baseCanvas.SetActive(Active);
        }

        /// <summary>
        /// Activates the screen blocker.
        /// </summary>
        public void BlockScreen() => blocker.SetActive(true);

        /// <summary>
        /// Deactivates the screen blocker.
        /// </summary>
        public void UnblockScreen() => blocker.SetActive(false);

        #endregion

        #region Transforms

        /// <summary>
        /// Position of the model.
        /// </summary>
        public Vector2 position = new Vector2(0f, -1200f);

        /// <summary>
        /// Total scale of the model.
        /// </summary>
        public Vector2 scale = Vector2.one;

        /// <summary>
        /// Rotation of the model.
        /// </summary>
        public float rotation;

        /// <summary>
        /// Position of the model's face.
        /// </summary>
        public Vector2 facePosition;

        /// <summary>
        /// Amount the mouth should be open.
        /// </summary>
        public float mouthOpenAmount = 0.5f;

        /// <summary>
        /// Offset of the pupils when looking around.
        /// </summary>
        public Vector2 pupilsOffset;

        #endregion

        #region Functions

        /// <summary>
        /// Function to run before building.
        /// </summary>
        public static Action<ExampleModel> preBuild;
        /// <summary>
        /// Function to run after building.
        /// </summary>
        public static Action<ExampleModel> postBuild;
        /// <summary>
        /// Function to run before per-tick.
        /// </summary>
        public static Action<ExampleModel> onPreTick;
        /// <summary>
        /// Function to run after per-tick.
        /// </summary>
        public static Action<ExampleModel> onPostTick;
        /// <summary>
        /// Function to run when Example starts dancing.
        /// </summary>
        public Action startDancing;
        /// <summary>
        /// Function to run when Example is interrupted from dancing.
        /// </summary>
        public Action stopDancing;

        #endregion

        #region Parts

        /// <summary>
        /// List of the model's parts.
        /// </summary>
        public List<BasePart> parts = new List<BasePart>();

        /// <summary>
        /// Gets a part by its ID.
        /// </summary>
        /// <param name="id">ID to search a part for.</param>
        /// <returns>If a part exists in the list, returns the part, otherwise returns null.</returns>
        public BasePart GetPart(string id) => parts.Find(x => x.id == id);

        /// <summary>
        /// Tries to get a part by its ID.
        /// </summary>
        /// <param name="id">ID to search a part for.</param>
        /// <param name="part">Part result.</param>
        /// <returns>Returns true if a part exists, otherwise returns false.</returns>
        public bool TryGetPart(string id, out BasePart part)
        {
            part = GetPart(id);
            return part;
        }

        /// <summary>
        /// Top-most parent ID.
        /// </summary>
        public string basePartID = "BASE";

        /// <summary>
        /// Canvas that renders the model.
        /// </summary>
        public Canvas canvas;

        /// <summary>
        /// Base object that the model is parented to.
        /// </summary>
        public GameObject baseCanvas;

        /// <summary>
        /// Canvas group for setting transparency.
        /// </summary>
        public CanvasGroup canvasGroup;

        /// <summary>
        /// Blocks the mouse from interacting with the game.
        /// </summary>
        public GameObject blocker;

        /// <summary>
        /// Model's base parent.
        /// </summary>
        public Transform baseParent;

        /// <summary>
        /// Represents a part of the model.
        /// </summary>
        public abstract class BasePart : Exists
        {
            public BasePart(PartType partType) => this.partType = partType;

            /// <summary>
            /// GameObject reference.
            /// </summary>
            public GameObject gameObject;
            
            /// <summary>
            /// Transform reference.
            /// </summary>
            public Transform transform;

            /// <summary>
            /// Identification of the part.
            /// </summary>
            public string id;

            /// <summary>
            /// What to parent the part to.
            /// </summary>
            public string parentID;

            /// <summary>
            /// The sibling order of the part.
            /// </summary>
            public int siblingIndex = -1;

            /// <summary>
            /// The parent reference.
            /// </summary>
            public BasePart parent;
            public BasePart child;

            /// <summary>
            /// The children of the part.
            /// </summary>
            public List<BasePart> children;

            /// <summary>
            /// Name of the part.
            /// </summary>
            public string name;

            readonly PartType partType;
            /// <summary>
            /// What type of part this is.
            /// </summary>
            public PartType Type => partType;

            /// <summary>
            /// Part type.
            /// </summary>
            public enum PartType
            {
                /// <summary>
                /// Represents a <see cref="ParentPart"/>.
                /// </summary>
                Parent,
                /// <summary>
                /// Represents an <see cref="ImagePart"/>.
                /// </summary>
                Image,
            }

            /// <summary>
            /// CSharp code compiled to run per-tick.
            /// </summary>
            public string onTickCS;

            /// <summary>
            /// Function to run per-tick.
            /// </summary>
            public Action<BasePart> onTick;

            /// <summary>
            /// RectTransform values of the part.
            /// </summary>
            public RectValues rect = RectValues.Default;

            /// <summary>
            /// Full rotation of the part.
            /// </summary>
            public float rotation;

            /// <summary>
            /// Sets the parts' position.
            /// </summary>
            /// <param name="pos">Position to set.</param>
            public void SetPosition(Vector2 pos)
            {
                if (transform)
                    transform.localPosition = pos;
            }

            /// <summary>
            /// Sets the parts' scale.
            /// </summary>
            /// <param name="sca">Scale to set.</param>
            public void SetScale(Vector2 sca)
            {
                if (transform)
                    transform.localScale = sca;
            }
            
            /// <summary>
            /// Sets the parts' rotation.
            /// </summary>
            /// <param name="rot"></param>
            public void SetRotation(float rot)
            {
                if (transform)
                    transform.localRotation = Quaternion.Euler(0f, 0f, rot);
            }

            /// <summary>
            /// Builds the Example part.
            /// </summary>
            public abstract void Build();

            /// <summary>
            /// Ticks the Example part.
            /// </summary>
            public virtual void Tick()
            {
                onTick?.Invoke(this);
                SetRotation(rotation + rect.rotation);
            }

            /// <summary>
            /// Adds a function to run per-tick.
            /// </summary>
            /// <param name="action">Function to add.</param>
            public void AddTickFunction(Action<BasePart> action) => onTick += action;

            /// <summary>
            /// Sets a function to the per-tick function.
            /// </summary>
            /// <param name="action">Function to set.</param>
            public void SetTickFunction(Action<BasePart> action) => onTick = action;

            /// <summary>
            /// Adds a CSharp string function to run per-tick.
            /// </summary>
            /// <param name="code">CSharp code to add.</param>
            public void AddTickFunction(string code)
            {
                if (!string.IsNullOrEmpty(code))
                    onTick += part => RTCode.Evaluator.Compile(GetVariables(code));
            }

            /// <summary>
            /// Sets a CSharp string function to the per-tick function.
            /// </summary>
            /// <param name="code">CSharp code to set.</param>
            public void SetTickFunction(string code)
            {
                if (!string.IsNullOrEmpty(code))
                    onTick = part => RTCode.Evaluator.Compile(GetVariables(code));
            }

            /// <summary>
            /// Gets the parts' variables.
            /// </summary>
            /// <param name="input">Input to replace.</param>
            /// <returns>returns a string representing the part values.</returns>
            public string GetVariables(string input) => input
                .Replace("PART_ID", id)
                .Replace("PART_POS_X", transform.position.x.ToString())
                .Replace("PART_POS_Y", transform.position.y.ToString())
                .Replace("PART_LOCAL_POS_X", transform.localPosition.x.ToString())
                .Replace("PART_LOCAL_POS_Y", transform.localPosition.y.ToString());

            /// <summary>
            /// Parses a part from JSON.
            /// </summary>
            /// <param name="jn">JSON to parse.</param>
            public abstract void Parse(JSONNode jn);

            /// <summary>
            /// Converts the part to JSON.
            /// </summary>
            /// <returns>Returns a JSON object representing the part.</returns>
            public abstract JSONNode ToJSON();

            public override string ToString() => id;
        }

        /// <summary>
        /// Represents a parent part of the model.
        /// </summary>
        public class ParentPart : BasePart
        {
            public ParentPart() : base(PartType.Parent) { }

            #region Init

            public static ParentPart Default => new ParentPart();

            public ParentPart ID(string id)
            {
                this.id = id;
                return this;
            }

            public ParentPart ParentID(string parentID)
            {
                this.parentID = parentID;
                return this;
            }

            public ParentPart SiblingIndex(int siblingIndex)
            {
                this.siblingIndex = siblingIndex;
                return this;
            }

            public ParentPart Name(string name)
            {
                this.name = name;
                return this;
            }

            public ParentPart Rect(RectValues rect)
            {
                this.rect = rect;
                return this;
            }

            public ParentPart OnTick(string onTick)
            {
                onTickCS = onTick;
                return this;
            }

            public ParentPart OnTick(Action<BasePart> onTick)
            {
                SetTickFunction(onTick);
                return this;
            }

            #endregion

            public override void Build()
            {
                if (gameObject)
                    CoreHelper.Destroy(gameObject);

                gameObject = Creator.NewUIObject(name, CompanionManager.inst.transform);
                transform = gameObject.transform;

                rect.AssignToRectTransform(transform.AsRT());

                SetTickFunction(onTickCS);
            }

            public override void Parse(JSONNode jn)
            {
                id = jn["id"];
                parentID = jn["p"];
                siblingIndex = jn["sib_index"].AsInt;
                name = jn["n"];
                rect = RectValues.Parse(jn["rect"], RectValues.Default);

                onTickCS = jn["tick"];
            }

            public override JSONNode ToJSON()
            {
                var jn = JSON.Parse("{}");

                jn["id"] = id;
                jn["p"] = parentID;
                jn["sib_index"] = siblingIndex;
                jn["n"] = name;
                jn["rect"] = rect.ToJSON();

                jn["tick"] = onTickCS;

                return jn;
            }

            public override string ToString() => id;
        }

        /// <summary>
        /// Represents a image part of the model.
        /// </summary>
        public class ImagePart : BasePart
        {
            public ImagePart() : base(PartType.Image) { }

            /// <summary>
            /// Image of the part.
            /// </summary>
            public Image image;

            /// <summary>
            /// RectTransform values of the image.
            /// </summary>
            public RectValues imageRect = RectValues.Default;

            /// <summary>
            /// Full rotation of the image.
            /// </summary>
            public float imageRotation;

            /// <summary>
            /// Path to the sprite of the image.
            /// </summary>
            public string imagePath;

            /// <summary>
            /// Function to run when the part is clicked.
            /// </summary>
            public Action<ImagePart, PointerEventData> onClick;

            /// <summary>
            /// Function to run when the part is pressed down.
            /// </summary>
            public Action<ImagePart, PointerEventData> onDown;

            /// <summary>
            /// Function to run when the part is pressed up.
            /// </summary>
            public Action<ImagePart, PointerEventData> onUp;

            /// <summary>
            /// CSharp string to compile when the part is clicked.
            /// </summary>
            public string onClickCS;

            /// <summary>
            /// CSharp string to compile when the part is pressed down.
            /// </summary>
            public string onDownCS;

            /// <summary>
            /// CSharp string to compile when the part is pressed up.
            /// </summary>
            public string onUpCS;

            #region Init

            public static ImagePart Default => new ImagePart();

            public ImagePart ID(string id)
            {
                this.id = id;
                return this;
            }

            public ImagePart ParentID(string parentID)
            {
                this.parentID = parentID;
                return this;
            }

            public ImagePart SiblingIndex(int siblingIndex)
            {
                this.siblingIndex = siblingIndex;
                return this;
            }

            public ImagePart Name(string name)
            {
                this.name = name;
                return this;
            }

            public ImagePart Rect(RectValues rect)
            {
                this.rect = rect;
                return this;
            }

            public ImagePart ImageRect(RectValues imageRect)
            {
                this.imageRect = imageRect;
                return this;
            }

            public ImagePart ImagePath(string imagePath)
            {
                this.imagePath = imagePath;
                return this;
            }

            public ImagePart OnTick(string onTick)
            {
                onTickCS = onTick;
                return this;
            }

            public ImagePart OnTick(Action<BasePart> onTick)
            {
                SetTickFunction(onTick);
                return this;
            }

            public ImagePart OnClick(Action<ImagePart, PointerEventData> onClick)
            {
                this.onClick = onClick;
                return this;
            }

            public ImagePart OnDown(Action<ImagePart, PointerEventData> onDown)
            {
                this.onDown = onDown;
                return this;
            }

            public ImagePart OnUp(Action<ImagePart, PointerEventData> onUp)
            {
                this.onUp = onUp;
                return this;
            }

            #endregion

            public override void Build()
            {
                if (gameObject)
                    CoreHelper.Destroy(gameObject);

                gameObject = Creator.NewUIObject(name, CompanionManager.inst.transform);
                transform = gameObject.transform;

                var imageGameObject = Creator.NewUIObject("image", transform);
                image = imageGameObject.AddComponent<Image>();

                rect.AssignToRectTransform(transform.AsRT());
                imageRect.AssignToRectTransform(image.rectTransform);

                CoreHelper.StartCoroutine(AlephNetwork.DownloadImageTexture(imagePath, image.AssignTexture));

                SetTickFunction(onTickCS);

                if (onClick == null && onDown == null && onUp == null)
                    return;

                var clickable = imageGameObject.AddComponent<ExampleClickable>();
                clickable.onClick = pointerEventData => onClick?.Invoke(this, pointerEventData);
                clickable.onDown = pointerEventData => onDown?.Invoke(this, pointerEventData);
                clickable.onUp = pointerEventData => onUp?.Invoke(this, pointerEventData);
            }

            public override void Tick()
            {
                base.Tick();
                if (image)
                    image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, imageRotation + imageRect.rotation);
            }

            public override void Parse(JSONNode jn)
            {
                id = jn["id"];
                parentID = jn["p"];
                siblingIndex = jn["sib_index"].AsInt;
                name = jn["n"];
                rect = RectValues.Parse(jn["rect"], RectValues.Default);
                imageRect = RectValues.Parse(jn["image_rect"], RectValues.Default);
                imagePath = jn["image_path"];

                onTickCS = jn["tick"];
                onClickCS = jn["on_click"];
                onDownCS = jn["on_down"];
                onUpCS = jn["on_up"];

                if (!string.IsNullOrEmpty(onClickCS))
                    onClick = (part, eventData) => RTCode.Evaluate(GetVariables(onClickCS));
                if (!string.IsNullOrEmpty(onDownCS))
                    onDown = (part, eventData) => RTCode.Evaluate(GetVariables(onDownCS));
                if (!string.IsNullOrEmpty(onUpCS))
                    onUp = (part, eventData) => RTCode.Evaluate(GetVariables(onUpCS));
            }

            public override JSONNode ToJSON()
            {
                var jn = JSON.Parse("{}");

                jn["id"] = id;
                jn["p"] = parentID;
                jn["sib_index"] = siblingIndex;
                jn["n"] = name;
                jn["rect"] = rect.ToJSON();
                jn["image_rect"] = imageRect.ToJSON();
                jn["image_path"] = imagePath;

                jn["tick"] = onTickCS;
                jn["on_click"] = onClickCS;
                jn["on_down"] = onDownCS;
                jn["on_up"] = onUpCS;

                return jn;
            }

            public override string ToString() => id;
        }

        #endregion

        #region Animations

        #region Poses

        /// <summary>
        /// Registers the default poses.
        /// </summary>
        public virtual void RegisterPoses()
        {
            poses.Add(new ExamplePose(Poses.IDLE, (model, parameters) =>
            {
                BasePart handLeft = GetPart("HAND_LEFT");
                BasePart handRight = GetPart("HAND_RIGHT");

                BasePart handsBase = GetPart("HANDS_BASE");

                var animation = new RTAnimation("RESET");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, rotation, Ease.Linear),
                        new FloatKeyframe(parameters.transitionTime, 0f, Ease.SineInOut),
                    }, x => rotation = x, interpolateOnComplete: true),
                    new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                    {
                        new Vector3Keyframe(0f, scale, Ease.Linear),
                        new Vector3Keyframe(parameters.transitionTime, Vector3.one, Ease.SineInOut),
                    }, x => scale = x, interpolateOnComplete: true),

                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, handsBase.rotation, Ease.Linear),
                        new FloatKeyframe(parameters.transitionTime, 0f, Ease.SineInOut),
                        new FloatKeyframe(parameters.transitionTime + 0.01f, 0f, Ease.Linear),
                    }, x => handsBase.rotation = x, interpolateOnComplete: true),

                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, handLeft.rotation, Ease.Linear),
                        new FloatKeyframe(parameters.transitionTime, 0f, Ease.SineInOut),
                    }, x => handLeft.rotation = x, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, handRight.rotation, Ease.Linear),
                        new FloatKeyframe(parameters.transitionTime, 0f, Ease.SineInOut),
                    }, x => handRight.rotation = x, interpolateOnComplete: true),
                };
                animation.events = new List<Core.Animation.AnimationEvent>
                {
                    new Core.Animation.AnimationEvent(parameters.transitionTime, () =>
                    {
                        GetAttribute("FACE_CAN_LOOK").Value = 1.0;
                        GetAttribute("ALLOW_BLINKING").Value = 1.0;
                    }),
                };

                return animation;
            }));
            poses.Add(new ExamplePose(Poses.LEAVE, (model, parameters) => new RTAnimation("Cya")
            {
                animationHandlers = new List<AnimationHandlerBase>
                {
                    // Base
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, model.position.x, Ease.Linear),
                        new FloatKeyframe(0.5f, model.position.x, Ease.Linear),
                        new FloatKeyframe(1.5f, model.position.x + -400f, Ease.SineOut),
                    }, x => model.position.x = x, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, model.position.y, Ease.Linear),
                        new FloatKeyframe(0.5f, model.position.y - 10f, Ease.Linear),
                        new FloatKeyframe(0.8f, model.position.y + 80f, Ease.SineOut),
                        new FloatKeyframe(1.5f, model.position.y + -1200f, Ease.CircIn),
                    }, x => model.position.y = x, interpolateOnComplete: true),
                    new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                    {
                        new Vector2Keyframe(0f, model.scale, Ease.Linear),
                        new Vector2Keyframe(0.5f, new Vector2(0.99f, 1.01f), Ease.Linear),
                        new Vector2Keyframe(0.7f, new Vector2(1.15f, 0.95f), Ease.SineOut),
                        new Vector2Keyframe(0.9f, new Vector2(1f, 1f), Ease.SineInOut),
                        new Vector2Keyframe(1.5f, new Vector2(0.7f, 1.3f), Ease.SineIn),
                    }, vector2 => model.scale = vector2, interpolateOnComplete: true),

                    // Face
                    new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                    {
                        new Vector2Keyframe(0f, model.facePosition, Ease.Linear),
                        new Vector2Keyframe(0.7f, new Vector2(0f, 2f), Ease.SineInOut),
                        new Vector2Keyframe(1.3f, new Vector2(0f, -3f), Ease.SineInOut),
                    }, vector2 => model.facePosition = vector2, interpolateOnComplete: true),
                },
            }));
            poses.Add(new ExamplePose(Poses.WORRY, (model, parameters) =>
            {
                var browLeft = GetPart("BROW_LEFT");
                var browRight = GetPart("BROW_RIGHT");

                if (!browLeft || !browRight)
                    throw new NullReferenceException("Brow Left or Right are null");

                var animation = new RTAnimation("Worry");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, browLeft.rotation, Ease.Linear),
                        new FloatKeyframe(0.3f, -15f, Ease.SineOut),
                    }, x => browLeft.rotation = x, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, browRight.rotation, Ease.Linear),
                        new FloatKeyframe(0.3f, 15f, Ease.SineOut),
                    }, x => browRight.rotation = x, interpolateOnComplete: true),
                };

                return animation;
            }));
            poses.Add(new ExamplePose(Poses.ANGRY, (model, parameters) =>
            {
                var browLeft = GetPart("BROW_LEFT");
                var browRight = GetPart("BROW_RIGHT");

                if (!browLeft || !browRight)
                    throw new NullReferenceException("Brow Left or Right are null");

                var animation = new RTAnimation("Angry");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, browLeft.rotation, Ease.Linear),
                        new FloatKeyframe(0.3f, 15f, Ease.SineOut),
                    }, x => browLeft.rotation = x, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, browRight.rotation, Ease.Linear),
                        new FloatKeyframe(0.3f, -15f, Ease.SineOut),
                    }, x => browRight.rotation = x, interpolateOnComplete: true),
                };

                return animation;
            }));
            poses.Add(new ExamplePose(Poses.START_DRAG, (model, parameters) =>
            {
                GetAttribute("FACE_CAN_LOOK").Value = 0.0;

                Transform lips = GetPart("LIPS").transform;
                Transform handLeft = GetPart("HAND_LEFT").transform.GetChild(0);
                Transform handRight = GetPart("HAND_RIGHT").transform.GetChild(0);

                BasePart browLeft = GetPart("BROW_LEFT");
                BasePart browRight = GetPart("BROW_RIGHT");

                BasePart handsBase = GetPart("HANDS_BASE");
                BasePart head = GetPart("HEAD");

                ExceptionHelper.NullReference(lips, "Example Lips");
                ExceptionHelper.NullReference(handLeft, "Example Hand Left");
                ExceptionHelper.NullReference(handRight, "Example Hand Right");
                ExceptionHelper.NullReference(browLeft, "Example Brow Left");
                ExceptionHelper.NullReference(browRight, "Example Brow Right");
                ExceptionHelper.NullReference(handsBase, "Example Hands Base");
                ExceptionHelper.NullReference(head, "Example Head");

                var animation = new RTAnimation("Drag Example");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, head.transform.localPosition.y, Ease.Linear),
                        new FloatKeyframe(0.2f, 10f, Ease.SineOut),
                    }, head.transform.SetLocalPositionY, interpolateOnComplete: true),
                    new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                    {
                        new Vector2Keyframe(0f, Vector2.one, Ease.Linear),
                        new Vector2Keyframe(0.3f, new Vector2(1.05f, 0.95f), Ease.SineOut),
                    }, x => scale = x, interpolateOnComplete: true),

				    // Hands
				    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, handLeft.localPosition.y, Ease.Linear),
                        new FloatKeyframe(0.05f, handLeft.localPosition.y, Ease.Linear),
                        new FloatKeyframe(0.3f, -30f, Ease.SineOut),
                    }, handLeft.SetLocalPositionY, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, handRight.localPosition.y, Ease.Linear),
                        new FloatKeyframe(0.05f, handRight.localPosition.y, Ease.Linear),
                        new FloatKeyframe(0.3f, -30f, Ease.SineOut),
                    }, handRight.SetLocalPositionY, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, handsBase.rotation, Ease.Linear),
                        new FloatKeyframe(0.3f, 0f, Ease.SineOut),
                        new FloatKeyframe(0.31f, 0f, Ease.Linear),
                    }, x => handsBase.rotation = x, interpolateOnComplete: true),

				    // Brows
				    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, browLeft.rotation, Ease.Linear),
                        new FloatKeyframe(0.3f, -15f, Ease.SineOut),
                    }, x => browLeft.rotation = x, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, browRight.rotation, Ease.Linear),
                        new FloatKeyframe(0.3f, 15f, Ease.SineOut),
                    }, x => browRight.rotation = x, interpolateOnComplete: true),

                    // Mouth
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, mouthOpenAmount, Ease.Linear),
                        new FloatKeyframe(0.2f, 0.7f, Ease.SineOut),
                    }, x => mouthOpenAmount = x, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, lips.localScale.y, Ease.Linear),
                        new FloatKeyframe(0.2f, 0.5f, Ease.SineOut),
                    }, lips.SetLocalScaleY, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, lips.localPosition.y, Ease.Linear),
                        new FloatKeyframe(0.2f, 2f, Ease.SineOut),
                    }, lips.SetLocalPositionY, interpolateOnComplete: true),
                };
                return animation;
            }));
            poses.Add(new ExamplePose(Poses.END_DRAG, (model, parameters) =>
            {
                GetAttribute("FACE_CAN_LOOK").Value = 1.0;

                Transform lips = GetPart("LIPS").transform;
                Transform handLeft = GetPart("HAND_LEFT").transform;
                Transform handRight = GetPart("HAND_RIGHT").transform;

                BasePart browLeft = GetPart("BROW_LEFT");
                BasePart browRight = GetPart("BROW_RIGHT");

                BasePart handsBase = GetPart("HANDS_BASE");
                BasePart head = GetPart("HEAD");

                ExceptionHelper.NullReference(lips, "Example Lips");
                ExceptionHelper.NullReference(handLeft, "Example Hand Left");
                ExceptionHelper.NullReference(handRight, "Example Hand Right");
                ExceptionHelper.NullReference(browLeft, "Example Brow Left");
                ExceptionHelper.NullReference(browRight, "Example Brow Right");
                ExceptionHelper.NullReference(handsBase, "Example Hands Base");
                ExceptionHelper.NullReference(head, "Example Head");

                var animation = new RTAnimation("End Drag Example");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    // Base
                    new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                    {
                        new Vector2Keyframe(0f, scale, Ease.Linear),
                        new Vector2Keyframe(1.5f, Vector2.one, Ease.ElasticOut),
                    }, x => scale = x, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, rotation, Ease.Linear),
                        new FloatKeyframe(1f, 0f, Ease.BackOut),
                    }, x => rotation = x, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, head.transform.localPosition.y, Ease.Linear),
                        new FloatKeyframe(0.5f, 0f, Ease.BounceOut),
                    }, head.transform.SetLocalPositionY, interpolateOnComplete: true),

                    // Mouth
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, mouthOpenAmount, Ease.Linear),
                        new FloatKeyframe(0.2f, 0.5f, Ease.SineIn),
                    }, x => mouthOpenAmount = x, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, lips.localScale.y, Ease.Linear),
                        new FloatKeyframe(0.2f, 1f, Ease.SineIn),
                    }, lips.SetLocalScaleY, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, lips.localPosition.y, Ease.Linear),
                        new FloatKeyframe(0.2f, 0f, Ease.SineIn),
                    }, lips.SetLocalPositionY, interpolateOnComplete: true),

				    // Brows
				    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, browLeft.rotation, Ease.Linear),
                        new FloatKeyframe(0.5f, 0f, Ease.SineOut),
                    }, x => browLeft.rotation = x, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, browRight.rotation, Ease.Linear),
                        new FloatKeyframe(0.5f, 0f, Ease.SineOut),
                    }, x => browRight.rotation = x, interpolateOnComplete: true),
                    new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                    {
                        new Vector2Keyframe(0f, facePosition, Ease.Linear),
                        new Vector2Keyframe(0.5f, Vector2.zero, Ease.SineOut),
                    }, x => facePosition = x, interpolateOnComplete: true),

                    // Hands
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, handLeft.GetChild(0).localPosition.y, Ease.Linear),
                        new FloatKeyframe(0.1f, handLeft.GetChild(0).localPosition.y, Ease.Linear),
                        new FloatKeyframe(0.7f, -80f, Ease.BounceOut),
                    }, handLeft.GetChild(0).SetLocalPositionY, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, handRight.GetChild(0).localPosition.y, Ease.Linear),
                        new FloatKeyframe(0.1f, handRight.GetChild(0).localPosition.y, Ease.Linear),
                        new FloatKeyframe(0.7f, -80f, Ease.BounceOut),
                    }, handRight.GetChild(0).SetLocalPositionY, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, handLeft.localRotation.eulerAngles.z, Ease.Linear),
                        new FloatKeyframe(0.1f, handLeft.localRotation.eulerAngles.z, Ease.Linear),
                        new FloatKeyframe(0.7f, 0f, Ease.BounceOut),
                    }, handLeft.SetLocalRotationEulerZ, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, handRight.localRotation.eulerAngles.z, Ease.Linear),
                        new FloatKeyframe(0.1f, handRight.localRotation.eulerAngles.z, Ease.Linear),
                        new FloatKeyframe(0.7f, 0f, Ease.BounceOut),
                    }, handRight.SetLocalRotationEulerZ, interpolateOnComplete: true),
                };

                return animation;
            }));
        }

        /// <summary>
        /// Overrides an existing pose.
        /// </summary>
        /// <param name="id">ID of the pose to override.</param>
        /// <param name="pose">Pose to ovrride.</param>
        public void OverridePose(string id, ExamplePose pose)
        {
            var poseIndex = poses.FindIndex(x => x.name == id);
            poses[poseIndex] = pose;
        }

        /// <summary>
        /// Sets the current pose of the model.
        /// </summary>
        /// <param name="pose">Pose to set.</param>
        public virtual void SetPose(string pose, PoseParameters parameters = null, Action<RTAnimation> onCompleteAnim = null)
        {
            if (!parameters)
                parameters = PoseParameters.Default;

            for (int i = 0; i < poses.Count; i++)
            {
                var customPose = poses[i];
                if (pose != customPose.name)
                    continue;

                var animation = customPose.get?.Invoke(this, parameters);
                if (!animation)
                    throw new NullReferenceException($"No animation was registered.");

                animation.speed = parameters.speed;
                animation.onComplete = () =>
                {
                    onCompleteAnim?.Invoke(animation);
                    if (onCompleteAnim == null)
                        CompanionManager.inst.animationController.Remove(animation.id);
                };
                CompanionManager.inst.animationController.Play(animation);

                break;
            }
        }

        /// <summary>
        /// List of that can be used.
        /// </summary>
        public List<ExamplePose> poses = new List<ExamplePose>();

        /// <summary>
        /// Library of default poses.
        /// </summary>
        public static class Poses
        {
            /// <summary>
            /// Example resets his pose.
            /// </summary>
            public const string IDLE = "Idle";
            /// <summary>
            /// Example leaves the scene.
            /// </summary>
            public const string LEAVE = "Leave";
            /// <summary>
            /// Example expresses concern.
            /// </summary>
            public const string WORRY = "Worry";
            /// <summary>
            /// Example expresses anger.
            /// </summary>
            public const string ANGRY = "Angry";
            /// <summary>
            /// Example is happy about being dragged.
            /// </summary>
            public const string START_DRAG = "Start Drag";
            /// <summary>
            /// Example resets after being dragged.
            /// </summary>
            public const string END_DRAG = "End Drag";

            // todo:
            /*
                - sad
                - happy
                - surprise
                - sleeping
             */
        }

        #endregion

        /// <summary>
        /// Starts the dance animation loop.
        /// </summary>
        public virtual void PlayDanceAnimation()
        {
            if (!reference || !reference.brain)
            {
                CoreHelper.Log($"Example is dead wtf");
                return;
            }

            if (!reference.brain.canDance)
                return;

            if (danceLoopAnimation)
            {
                CompanionManager.inst.animationController.Remove(danceLoopAnimation.id);
                danceLoopAnimation = null;
            }

            GetAttribute("FACE_CAN_LOOK").Value = 0.0;
            GetAttribute("ALLOW_BLINKING").Value = 0.0;

            Transform lips = GetPart("LIPS").transform;
            Transform handLeft = GetPart("HAND_LEFT").transform;
            Transform handRight = GetPart("HAND_RIGHT").transform;

            BasePart browLeft = GetPart("BROW_LEFT");
            BasePart browRight = GetPart("BROW_RIGHT");

            BasePart handsBase = GetPart("HANDS_BASE");
            BasePart head = GetPart("HEAD");

            danceLoopAnimation = new RTAnimation("Dance Loop");
            danceLoopAnimation.loop = true;
            danceLoopAnimation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                {
                    new Vector3Keyframe(0f,   new Vector3(1.05f, 0.95f, 1f), Ease.Linear),
                    new Vector3Keyframe(0.4f, new Vector3(0.95f, 1.05f, 1f), Ease.SineOut),
                    new Vector3Keyframe(0.8f, new Vector3(1.05f, 0.95f, 1f), Ease.SineOut),
                    new Vector3Keyframe(1.2f, new Vector3(0.95f, 1.05f, 1f), Ease.SineOut),
                    new Vector3Keyframe(1.6f, new Vector3(1.05f, 0.95f, 1f), Ease.SineOut),
                }, x => scale = x),
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f,   0f, Ease.Linear),
                    new FloatKeyframe(0.4f, -2f, Ease.SineOut),
                    new FloatKeyframe(0.8f, 0f, Ease.SineIn),
                    new FloatKeyframe(1.2f, 2f, Ease.SineOut),
                    new FloatKeyframe(1.6f, 0f, Ease.SineIn),
                }, head.transform.SetLocalPositionX),
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f,   0f, Ease.Linear),
                    new FloatKeyframe(0.4f, 1f, Ease.SineOut),
                    new FloatKeyframe(0.8f, 0f, Ease.SineIn),
                    new FloatKeyframe(1.2f, 1f, Ease.SineOut),
                    new FloatKeyframe(1.6f, 0f, Ease.SineIn),
                }, head.transform.SetLocalPositionY),
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, -3f, Ease.Linear),
                    new FloatKeyframe(0.8f, 3f, Ease.SineOut),
                    new FloatKeyframe(1.6f, -3f, Ease.SineOut),
                }, x => facePosition.x = x),
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 0f, Ease.Linear),
                    new FloatKeyframe(0.4f, 6f, Ease.SineOut),
                    new FloatKeyframe(0.8f, 0f, Ease.SineIn),
                    new FloatKeyframe(1.2f, 6f, Ease.SineOut),
                    new FloatKeyframe(1.6f, 0f, Ease.SineIn),
                }, x => facePosition.y = x),
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f,   0f, Ease.Linear),
                    new FloatKeyframe(0.4f, 15f, Ease.SineOut),
                    new FloatKeyframe(0.8f, 0f, Ease.SineIn),
                    new FloatKeyframe(1.2f, -15f, Ease.SineOut),
                    new FloatKeyframe(1.6f, 0f, Ease.SineIn),
                }, x => handsBase.rotation = x),
            };
            danceLoopAnimation.speed = UnityEngine.Random.Range(0.5f, 2f);
            CompanionManager.inst.animationController.Play(danceLoopAnimation);
            reference.brain.dancing = true;
        }

        /// <summary>
        /// Stops the dance animation loop.
        /// </summary>
        public virtual void StopDanceAnimation()
        {
            if (!danceLoopAnimation)
                return;

            CompanionManager.inst.animationController.Remove(danceLoopAnimation.id);
            danceLoopAnimation = null;
            SetPose(Poses.IDLE);
        }

        RTAnimation startDragAnimation;
        RTAnimation endDragAnimation;
        RTAnimation danceLoopAnimation;

        #endregion

        #region Misc

        // TODO:
        // you can respond to Example's question about what a level is, which will add to his memory.
        void SelectObject(Image image)
        {
            var rect = EditorManager.RectTransformToScreenSpace(image.rectTransform);
            if (CoreHelper.InEditor && rect.Overlaps(EditorManager.RectTransformToScreenSpace(RTEditor.inst.OpenLevelPopup.GameObject.transform.Find("mask").AsRT())))
                foreach (var levelItem in RTEditor.inst.LevelPanels)
                {
                    if (levelItem.GameObject.activeInHierarchy && rect.Overlaps(EditorManager.RectTransformToScreenSpace(levelItem.GameObject.transform.AsRT())))
                    {
                        Debug.LogFormat($"{CompanionManager.className}Picked level: {levelItem.FolderPath}");
                        reference?.chatBubble?.Say($"What's \"{levelItem.Name}\"?");
                        break; // only select one level
                    }
                }
        }

        #endregion
    }
}
