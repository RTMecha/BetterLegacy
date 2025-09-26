using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using SimpleJSON;

using BetterLegacy.Companion.Data;
using BetterLegacy.Companion.Data.Parameters;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

namespace BetterLegacy.Companion.Entity
{
    /// <summary>
    /// Represents Example's model.
    /// </summary>
    public class ExampleModel : ExampleModule
    {
        #region Default Instance

        public ExampleModel() { }

        /// <summary>
        /// The default Example model.
        /// </summary>
        public static Func<ExampleModel> getDefault = () =>
        {
            var model = new ExampleModel();
            model.InitDefault();

            return model;
        };

        public override void InitDefault()
        {
            RegisterAttributes();
            RegisterFunctions();
            RegisterParts();
            RegisterPoses();

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
        }

        public virtual void RegisterAttributes()
        {
            attributes.Clear();
            if (AssetPack.TryReadFromFile("companion/model/attributes.json", out string attributesFile))
            {
                var jn = JSON.Parse(attributesFile);
                if (jn != null && jn["override"].AsBool)
                {
                    for (int i = 0; i < jn["attributes"].Count; i++)
                    {
                        var jnAttribute = jn["attributes"][i];
                        AddAttribute(jnAttribute["id"], jnAttribute["value"].AsDouble, jnAttribute["min"].AsDouble, jnAttribute["max"].AsDouble, jnAttribute["is_int"].AsBool);
                    }

                    return;
                }
            }
            AddAttribute("POKING_EYES", 0.0, 0.0, 1.0);
            AddAttribute("ALLOW_BLINKING", 1.0, 0.0, 1.0);
            AddAttribute("PUPILS_CAN_CHANGE", 1.0, 0.0, 1.0);
            AddAttribute("FACE_CAN_LOOK", 1.0, 0.0, 1.0);
            AddAttribute("PUPILS_CAN_LOOK", 1.0, 0.0, 1.0);
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

            if ((!CoreConfig.Instance.BorderlessFullscreen.Value && !Application.isFocused) || !Visible)
                return;

            for (int i = 0; i < parts.Count; i++)
                parts[i].Tick();

            onPostTick?.Invoke(this);

            if (canvasGroup)
                canvasGroup.alpha = (ExampleConfig.Instance.IsTransparent.Value ? ExampleConfig.Instance.TransparencyOpacity.Value : 1f) * opacity;
        }

        /// <summary>
        /// Clears Example's model.
        /// </summary>
        public override void Clear()
        {
            base.Clear();
            CoreHelper.Destroy(baseCanvas);
            parts.Clear();

            canvas = null;
            baseCanvas = null;
            canvasGroup = null;
            blocker = null;
            baseParent = null;

            poses.Clear();

            if (danceLoopAnimation)
                CompanionManager.inst.animationController.Remove(danceLoopAnimation.id);
            danceLoopAnimation = null;

            startDancing = null;
            stopDancing = null;
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
            else if (Menus.InterfaceManager.inst && Menus.InterfaceManager.inst.CurrentInterface)
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

        /// <summary>
        /// Opacity of the canvas.
        /// </summary>
        public float opacity = 1f;

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
        /// Registers the model's parts.
        /// </summary>
        public virtual void RegisterParts()
        {
            parts.Clear();

            // parse custom model
            JSONNode jn = null;
            if (AssetPack.TryReadFromFile("companion/model/parts.json", out string partsFile))
            {
                jn = JSON.Parse(partsFile);
                if (jn != null && jn["override"].AsBool)
                {
                    for (int i = 0; i < jn["parts"].Count; i++)
                    {
                        var jnPart = jn["parts"][i];
                        var type = Parser.TryParse(jnPart["type"], BasePart.PartType.Parent);
                        BasePart part = type switch
                        {
                            BasePart.PartType.Parent => ParentPart.Default,
                            BasePart.PartType.Image => ImagePart.Default,
                            _ => null,
                        };
                        part.model = this;
                        part.Parse(jnPart);
                        parts.Add(part);
                    }

                    return;
                }
            }

            parts.Add(ParentPart.Default.Model(this).ID(Parts.BASE).Name("Base")
            .OnTick(part =>
            {
                // render the models' transforms
                part?.SetPosition(position + new Vector2(0f, (Ease.SineInOut(reference.timer.time * 0.5f % 2f) - 0.5f) * 2f));
                part?.SetScale(scale);
                part?.SetRotation(rotation);
            }));

            parts.Add(ImagePart.Default.Model(this).ID(Parts.HEAD).ParentID(Parts.BASE).Name("Head").ImagePath(AssetPack.GetFile("companion/model/example_head.png"))
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

                if (pointerEventData.button != PointerEventData.InputButton.Left || !reference || !reference.canDrag || reference.leaving)
                    return;

                // start dragging

                reference?.brain?.Interact(ExampleBrain.Interactions.PET);

                CompanionManager.inst.animationController.Remove(x => x.name == "End Drag Example" || x.name == "Drag Example" || x.name.ToLower().Contains("movement"));

                reference.startMousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y) * CoreHelper.ScreenScaleInverse;
                reference.startDragPos = new Vector2(position.x, position.y);
                reference.dragPos = new Vector3(position.x, position.y);
                reference.dragging = true;

                SoundManager.inst.PlaySound(baseCanvas, DefaultSounds.example_speak, UnityEngine.Random.Range(0.6f, 0.7f), UnityEngine.Random.Range(1.1f, 1.3f));

                SetPose(Poses.START_DRAG);

                if (reference.brain.CurrentAction)
                {
                    reference.brain?.Interact(ExampleBrain.Interactions.INTERRUPT);
                    reference.brain.StopCurrentAction();
                }
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

            parts.Add(ParentPart.Default.Model(this).ID(Parts.FACE).ParentID(Parts.HEAD).Name("Face")
            .OnTick(part =>
            {
                // tick function

                if (!part || !part.transform)
                    return;

                if (GetAttribute("FACE_CAN_LOOK").Value == 1.0)
                {
                    var lerp = RTMath.Lerp(Vector2.zero, reference.brain.LookingAt - new Vector2(part.transform.position.x, part.transform.position.y), CompanionManager.FACE_LOOK_MULTIPLIER);
                    part?.SetPosition(new Vector3(lerp.x, lerp.y, 0f));
                    facePosition = part.position;
                }
                else
                    part?.SetPosition(facePosition);

                if (reference.dragging)
                    facePosition = new Vector2(
                        Mathf.Clamp(-((reference.DragTarget.x - reference.dragPos.x) * reference.DragDelay), -14f, 14f),
                        Mathf.Clamp(-((reference.DragTarget.y - reference.dragPos.y) * reference.DragDelay), -14f, 14f));
            }));

            #region Ears

            parts.Add(ParentPart.Default.Model(this).ID(Parts.EARS).ParentID(Parts.HEAD).SiblingIndex(0).Name("Ears")
            .OnTick(part =>
            {
                part.rotation = facePosition.x * 0.8f;
            }));

            parts.Add(ImagePart.Default.Model(this).ID(Parts.EAR_BOTTOM_LEFT).ParentID(Parts.EARS).Name("Ear Bottom Left").ImagePath(AssetPack.GetFile("companion/model/example_ear_bottom.png"))
            .Rect(RectValues.Default.AnchoredPosition(25f, 35f).Rotation(-30f))
            .ImageRect(RectValues.Default.Pivot(0.5f, 0.2f).SizeDelta(44f, 52f)));

            parts.Add(ImagePart.Default.Model(this).ID(Parts.EAR_TOP_LEFT).ParentID(Parts.EAR_BOTTOM_LEFT).Name("Ear Top Left").ImagePath(AssetPack.GetFile("companion/model/example_ear_top.png"))
            .Rect(RectValues.Default)
            .ImageRect(RectValues.Default.AnchoredPosition(0f, 45f).Pivot(0.5f, 0.275f).SizeDelta(44f, 80f).Rotation(-90f))
            .OnClick((part, pointerEventData) =>
            {
                SetPose(Poses.EAR_LEFT_FLICK);
            }));

            parts.Add(ImagePart.Default.Model(this).ID(Parts.EAR_BOTTOM_RIGHT).ParentID(Parts.EARS).Name("Ear Bottom Right").ImagePath(AssetPack.GetFile("companion/model/example_ear_bottom.png"))
            .Rect(RectValues.Default.AnchoredPosition(-25f, 35f).Rotation(30f))
            .ImageRect(RectValues.Default.Pivot(0.5f, 0.2f).SizeDelta(44f, 52f)));

            parts.Add(ImagePart.Default.Model(this).ID(Parts.EAR_TOP_RIGHT).ParentID(Parts.EAR_BOTTOM_RIGHT).Name("Ear Top Right").ImagePath(AssetPack.GetFile("companion/model/example_ear_top.png"))
            .Rect(RectValues.Default)
            .ImageRect(RectValues.Default.AnchoredPosition(0f, 45f).Pivot(0.5f, 0.275f).SizeDelta(44f, 80f).Rotation(90f))
            .OnClick((part, pointerEventData) =>
            {
                SetPose(Poses.EAR_RIGHT_FLICK);
            }));

            #endregion

            parts.Add(ImagePart.Default.Model(this).ID(Parts.TAIL).ParentID(Parts.HEAD).SiblingIndex(0).Name("Tail").ImagePath(AssetPack.GetFile("companion/model/example_tail.png"))
            .ImageRect(RectValues.Default.AnchoredPosition(0f, -58f).SizeDelta(28f, 42f))
            .OnTick(part =>
            {
                float faceXPos = facePosition.x * CompanionManager.FACE_X_MULTIPLIER;
                part.rotation = -faceXPos;
                ((ImagePart)part).imageRotation = -faceXPos;
            })
            .OnClick((part, pointerEventData) =>
            {
                if (!reference.leaving)
                    reference?.brain?.Interact(ExampleBrain.Interactions.TOUCHIE);
            }));

            #region Eyes

            parts.Add(ImagePart.Default.Model(this).ID(Parts.EYES).ParentID(Parts.FACE).Name("Eyes").ImagePath(AssetPack.GetFile("companion/model/example_eyes.png"))
            .ImageRect(RectValues.Default.SizeDelta(74f, 34f))
            .OnDown((part, pointerEventData) => GetAttribute("POKING_EYES").Value = 1.0)
            .OnUp((part, pointerEventData) => GetAttribute("POKING_EYES").Value = 0.0));

            parts.Add(ImagePart.Default.Model(this).ID(Parts.PUPILS).ParentID(Parts.EYES).Name("Pupils").ImagePath(AssetPack.GetFile("companion/model/example_pupils.png"))
            .ImageRect(RectValues.Default.SizeDelta(47f, 22f))
            .OnTick(part =>
            {
                if (!part || !part.transform || GetAttribute("PUPILS_CAN_LOOK").Value == 0.0)
                    return;

                float t = reference.timer.time % CompanionManager.PUPILS_LOOK_RATE;

                // Here we add a tiny amount of movement to the pupils to make Example feel a lot more alive.
                if (t > CompanionManager.PUPILS_LOOK_RATE - 0.3f && GetAttribute("PUPILS_CAN_CHANGE").Value == 1.0)
                    pupilsOffset = new Vector2(UnityEngine.Random.Range(0f, 0.5f), UnityEngine.Random.Range(0f, 0.5f));

                GetAttribute("PUPILS_CAN_CHANGE").Value = (t <= CompanionManager.PUPILS_LOOK_RATE - 0.3f) ? 0.0 : 1.0;

                part.SetPosition(RTMath.Lerp(Vector2.zero, reference.brain.LookingAt - new Vector2(part.transform.position.x, part.transform.position.y), 0.004f) + pupilsOffset);
            })
            .OnDown((part, pointerEventData) => GetAttribute("POKING_EYES").Value = 1.0)
            .OnUp((part, pointerEventData) => GetAttribute("POKING_EYES").Value = 0.0));

            parts.Add(ImagePart.Default.Model(this).ID(Parts.TOP_EYELIDS).ParentID(Parts.EYES).Name("Top Eyelids").ImagePath(AssetPack.GetFile("companion/model/example_eyelids.png"))
            .Position(new Vector2(0f, 18f)).Scale(new Vector2(1f, 0f))
            .Rect(RectValues.Default.Pivot(0.5f, 1f).SizeDelta(74f, 18f))
            .ImageRect(RectValues.FullAnchored));
            
            parts.Add(ImagePart.Default.Model(this).ID(Parts.BOTTOM_EYELIDS).ParentID(Parts.EYES).Name("Bottom Eyelids").ImagePath(AssetPack.GetFile("companion/model/example_eyelids.png"))
            .Position(new Vector2(0f, -18f)).Scale(new Vector2(1f, 0f))
            .Rect(RectValues.Default.Pivot(0.5f, 0f).SizeDelta(74f, 18f))
            .ImageRect(RectValues.FullAnchored));

            parts.Add(ImagePart.Default.Model(this).ID(Parts.BLINK).ParentID(Parts.EYES).Name("Blink").ImagePath(AssetPack.GetFile("companion/model/example_blink.png"))
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

            parts.Add(ParentPart.Default.Model(this).ID(Parts.BROWS).ParentID(Parts.FACE).Name("Brows")
            .Rect(RectValues.Default.AnchoredPosition(0f, 30f)));

            parts.Add(ImagePart.Default.Model(this).ID(Parts.BROW_LEFT).ParentID(Parts.BROWS).Name("Brow Left").ImagePath(AssetPack.GetFile("companion/model/example_brow.png"))
            .Rect(RectValues.Default.AnchoredPosition(22f, 0f))
            .ImageRect(RectValues.Default.AnchoredPosition(18f, 0f).Pivot(1.7f, 0.5f).SizeDelta(20f, 6f)));

            parts.Add(ImagePart.Default.Model(this).ID(Parts.BROW_RIGHT).ParentID(Parts.BROWS).Name("Brow Right").ImagePath(AssetPack.GetFile("companion/model/example_brow.png"))
            .Rect(RectValues.Default.AnchoredPosition(-22f, 0f))
            .ImageRect(RectValues.Default.AnchoredPosition(-18f, 0f).Pivot(-0.7f, 0.5f).SizeDelta(20f, 6f)));

            #endregion

            #region Snout

            parts.Add(ImagePart.Default.Model(this).ID(Parts.SNOUT).ParentID(Parts.FACE).Name("Snout").ImagePath(AssetPack.GetFile("companion/model/example_snout.png"))
            .ImageRect(RectValues.Default.AnchoredPosition(0f, -31f).SizeDelta(60f, 31f)));

            parts.Add(ParentPart.Default.Model(this).ID(Parts.MOUTH_BASE).ParentID(Parts.SNOUT).Name("Mouth Base")
            .Rect(RectValues.Default.AnchoredPosition(0f, -30f))
            .OnTick(part =>
            {
                part?.SetPosition(new Vector2(facePosition.x, facePosition.y * 0.5f));
            }));

            parts.Add(ImagePart.Default.Model(this).ID(Parts.MOUTH_UPPER).ParentID(Parts.MOUTH_BASE).Name("Mouth Upper").ImagePath(AssetPack.GetFile("companion/model/example_mouth.png"))
            .Rect(RectValues.Default.Rotation(180f)).Scale(new Vector2(1f, 0.15f))
            .ImageRect(RectValues.Default.Pivot(0.5f, 1f).SizeDelta(32f, 16f)));

            parts.Add(ImagePart.Default.Model(this).ID(Parts.MOUTH_LOWER).ParentID(Parts.MOUTH_BASE).Name("Mouth Lower").ImagePath(AssetPack.GetFile("companion/model/example_mouth.png"))
            .ImageRect(RectValues.Default.Pivot(0.5f, 1f).SizeDelta(32f, 16f))
            .OnTick(part =>
            {
                part?.SetScale(new Vector2(part.scale.x, Mathf.Clamp(mouthOpenAmount, 0f, 1f)));
            }));

            parts.Add(ImagePart.Default.Model(this).ID(Parts.LIPS).ParentID(Parts.MOUTH_BASE).Name("Lips").ImagePath(AssetPack.GetFile("companion/model/example_lips.png"))
            .ImageRect(RectValues.Default.AnchoredPosition(0f, 3f).Pivot(0.5f, 1f).SizeDelta(32f, 8f)));

            parts.Add(ImagePart.Default.Model(this).ID(Parts.NOSE).ParentID(Parts.SNOUT).Name("Nose").ImagePath(AssetPack.GetFile("companion/model/example_nose.png"))
            .Rect(RectValues.Default.AnchoredPosition(0f, -20f))
            .ImageRect(RectValues.Default.SizeDelta(22f, 8f))
            .OnTick(part =>
            {
                part?.SetPosition(new Vector2(facePosition.x, facePosition.y * 0.5f));
            }));

            #endregion

            #region Hands

            parts.Add(ParentPart.Default.Model(this).ID(Parts.HANDS_BASE).ParentID("BASE").Name("Hands Base"));

            parts.Add(ImagePart.Default.Model(this).ID(Parts.HAND_LEFT).ParentID(Parts.HANDS_BASE).Name("Hand Left").ImagePath(AssetPack.GetFile("companion/model/example_hand.png"))
            .Position(new Vector2(40f, 0f))
            .ImageRect(RectValues.Default.AnchoredPosition(0f, -80f).SizeDelta(42f, 42f))
            .OnTick(part =>
            {
                if (reference && part && reference.draggingLeftHand)
                    part?.SetPosition(part.position + ((reference.DragTarget - part.position) * reference.DragDelay));
            })
            .OnDown((part, pointerEventData) =>
            {
                if (reference.leaving)
                    return;

                reference?.brain?.Interact(ExampleBrain.Interactions.HOLD_HAND);
                if (!reference || !reference.canDragLeftHand || !part)
                    return;

                reference.startMousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y) * CoreHelper.ScreenScaleInverse;
                reference.startDragPos = new Vector2(part.position.x, part.position.y);
                reference.draggingLeftHand = true;

                if (reference.brain.CurrentAction)
                    reference.brain.StopCurrentAction();
            })
            .OnUp((part, pointerEventData) =>
            {
                if (!reference || !reference.canDragLeftHand || !part || reference.leaving)
                    return;

                reference.draggingLeftHand = false;

                try
                {
                    reference.brain.SelectObject(part.image);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }

                SetPose(Poses.END_LEFT_HAND_DRAG);
            }));

            parts.Add(ImagePart.Default.Model(this).ID(Parts.HAND_RIGHT).ParentID(Parts.HANDS_BASE).Name("Hand Right").ImagePath(AssetPack.GetFile("companion/model/example_hand.png"))
            .Position(new Vector2(-40f, 0f))
            .ImageRect(RectValues.Default.AnchoredPosition(0f, -80f).SizeDelta(42f, 42f))
            .OnTick(part =>
            {
                if (reference && part && reference.draggingRightHand)
                    part.SetPosition(part.position + ((reference.DragTarget - part.position) * reference.DragDelay));
            })
            .OnDown((part, pointerEventData) =>
            {
                if (reference.leaving)
                    return;

                reference?.brain?.Interact(ExampleBrain.Interactions.HOLD_HAND);
                if (!reference || !reference.canDragRightHand || !part)
                    return;

                reference.startMousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y) * CoreHelper.ScreenScaleInverse;
                reference.startDragPos = new Vector2(part.position.x, part.position.y);
                reference.draggingRightHand = true;

                if (reference.brain.CurrentAction)
                    reference.brain.StopCurrentAction();
            })
            .OnUp((part, pointerEventData) =>
            {
                if (!reference || !reference.canDragRightHand || !part || reference.leaving)
                    return;

                reference.draggingRightHand = false;

                try
                {
                    reference.brain.SelectObject(part.image);
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }

                SetPose(Poses.END_RIGHT_HAND_DRAG);
            }));

            #endregion

            // add custom objects
            if (jn != null)
            {
                for (int i = 0; i < jn["parts"].Count; i++)
                {
                    var jnPart = jn["parts"][i];
                    var type = Parser.TryParse(jnPart["type"], BasePart.PartType.Parent);
                    BasePart part = type switch
                    {
                        BasePart.PartType.Parent => ParentPart.Default,
                        BasePart.PartType.Image => ImagePart.Default,
                        _ => null,
                    };
                    part.model = this;
                    part.Parse(jnPart);
                    parts.Add(part);
                }
            }
        }

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
        /// Registers functions to the model.
        /// </summary>
        public virtual void RegisterFunctions()
        {
            partFunctions = new PartFunctions();
            var array = AssetPack.GetArray("companion/model/functions.json");
            for (int i = 0; i < array.Count; i++)
                partFunctions.customJSONFunctions[array[i]["name"]] = array[i];
        }

        /// <summary>
        /// Gets JSON variables from the object.
        /// </summary>
        /// <returns>Returns a dictionary of object variables.</returns>
        public Dictionary<string, JSONNode> GetVariables()
        {
            var dictionary = new Dictionary<string, JSONNode>();
            dictionary["posX"] = position.x;
            dictionary["posY"] = position.y;
            dictionary["scaX"] = scale.x;
            dictionary["scaY"] = scale.y;
            dictionary["rot"] = rotation;
            dictionary["facePosX"] = facePosition.x;
            dictionary["facePosY"] = facePosition.y;
            dictionary["pupilsOffsetX"] = pupilsOffset.x;
            dictionary["pupilsOffsetY"] = pupilsOffset.y;
            dictionary["mouthOpenAmount"] = mouthOpenAmount;
            if (reference)
            {
                dictionary["timer"] = reference.timer.time;
                dictionary["dragTargetX"] = reference.DragTarget.x;
                dictionary["dragTargetY"] = reference.DragTarget.y;
                dictionary["dragPosX"] = reference.dragPos.x;
                dictionary["dragPosY"] = reference.dragPos.y;
                dictionary["dragDelay"] = reference.DragDelay;
            }
            if (reference && reference.brain)
            {
                var lookingAt = reference.brain.LookingAt;
                dictionary["lookingAtPosX"] = lookingAt.x;
                dictionary["lookingAtPosY"] = lookingAt.y;
            }
            return dictionary;
        }

        /// <summary>
        /// Parser for part functions.
        /// </summary>
        public JSONFunctionParser<BasePart> partFunctions;

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

            public ExampleModel model;

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
            /// JSON code compiled to run per-tick.
            /// </summary>
            public JSONNode onTickJSON;

            /// <summary>
            /// Function to run per-tick.
            /// </summary>
            public Action<BasePart> onTick;

            /// <summary>
            /// RectTransform values of the part.
            /// </summary>
            public RectValues rect = RectValues.Default;

            /// <summary>
            /// Local position of the part.
            /// </summary>
            public Vector2 position;

            /// <summary>
            /// Local scale of the part.
            /// </summary>
            public Vector2 scale = Vector2.one;

            /// <summary>
            /// Full rotation of the part.
            /// </summary>
            public float rotation;

            /// <summary>
            /// Sets the parts' position.
            /// </summary>
            /// <param name="pos">Position to set.</param>
            public void SetPosition(Vector2 pos) => position = pos;

            /// <summary>
            /// Sets the parts' scale.
            /// </summary>
            /// <param name="sca">Scale to set.</param>
            public void SetScale(Vector2 sca) => scale = sca;

            /// <summary>
            /// Sets the parts' rotation.
            /// </summary>
            /// <param name="rot"></param>
            public void SetRotation(float rot) => rotation = rot;

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

                if (onTickJSON != null)
                    model?.partFunctions?.ParseFunction(onTickJSON, this, GetVariables());

                if (!transform)
                    return;

                transform.localPosition = position + rect.anchoredPosition;
                transform.localScale = (scale * rect.scale).AsVector3(1f);
                transform.localRotation = Quaternion.Euler(0f, 0f, rotation + rect.rotation);
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
            /// Gets JSON variables from the object.
            /// </summary>
            /// <returns>Returns a dictionary of object variables.</returns>
            public virtual Dictionary<string, JSONNode> GetVariables()
            {
                var dictionary = model?.GetVariables() ?? new Dictionary<string, JSONNode>();
                if (transform)
                {
                    dictionary["partTransformPosX"] = transform.position.x;
                    dictionary["partTransformPosY"] = transform.position.y;
                    dictionary["partTransformScaX"] = transform.localScale.x;
                    dictionary["partTransformScaY"] = transform.localScale.y;
                    dictionary["partTransformRotZ"] = transform.localEulerAngles.z;
                }
                dictionary["partPosX"] = position.x;
                dictionary["partPosY"] = position.y;
                dictionary["partScaX"] = scale.x;
                dictionary["partScaY"] = scale.y;
                dictionary["partRot"] = rotation;
                return dictionary;
            }

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

            public ParentPart Model(ExampleModel model)
            {
                this.model = model;
                return this;
            }

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

            public ParentPart Position(Vector2 pos)
            {
                SetPosition(pos);
                return this;
            }
            
            public ParentPart Scale(Vector2 sca)
            {
                SetScale(sca);
                return this;
            }
            
            public ParentPart Rotation(float rot)
            {
                SetRotation(rot);
                return this;
            }

            public ParentPart OnTick(JSONNode onTickJSON)
            {
                this.onTickJSON = onTickJSON;
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
            }

            public override void Parse(JSONNode jn)
            {
                id = jn["id"];
                parentID = jn["p"];
                if (jn["sib_index"] != null)
                    siblingIndex = jn["sib_index"].AsInt;
                name = jn["name"];
                rect = RectValues.Parse(jn["rect"], RectValues.Default);

                position = Parser.TryParse(jn["pos"], Vector2.zero);
                scale = Parser.TryParse(jn["sca"], Vector2.one);
                rotation = jn["rot"].AsFloat;

                if (jn["on_tick"] != null)
                    onTickJSON = jn["on_tick"];
            }

            public override JSONNode ToJSON()
            {
                var jn = Parser.NewJSONObject();

                if (!string.IsNullOrEmpty(id))
                    jn["id"] = id;
                if (!string.IsNullOrEmpty(parentID))
                    jn["p"] = parentID;
                jn["sib_index"] = siblingIndex;
                if (!string.IsNullOrEmpty(name))
                    jn["name"] = name;
                jn["rect"] = rect.ToJSON();

                if (position.x != 0f || position.y != 0f)
                    jn["pos"] = position.ToJSON();
                if (scale.x != 1f || scale.y != 1f)
                    jn["Sca"] = scale.ToJSON();
                if (rotation != 0f)
                    jn["rot"] = rotation;

                if (onTickJSON != null)
                    jn["tick"] = onTickJSON;

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
            /// JSON code compiled to run when the part is clicked.
            /// </summary>
            public JSONNode onClickJSON;

            /// <summary>
            /// JSON code compiled to run when the part is pressed down.
            /// </summary>
            public JSONNode onDownJSON;

            /// <summary>
            /// JSON code compiled to run when the part is pressed up.
            /// </summary>
            public JSONNode onUpJSON;

            #region Init

            public static ImagePart Default => new ImagePart();

            public ImagePart Model(ExampleModel model)
            {
                this.model = model;
                return this;
            }

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

            public ImagePart Position(Vector2 pos)
            {
                SetPosition(pos);
                return this;
            }

            public ImagePart Scale(Vector2 sca)
            {
                SetScale(sca);
                return this;
            }

            public ImagePart Rotation(float rot)
            {
                SetRotation(rot);
                return this;
            }

            public ImagePart OnTick(JSONNode onTickJSON)
            {
                this.onTickJSON = onTickJSON;
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

                var imagePath = this.imagePath;
                if (!string.IsNullOrEmpty(imagePath))
                {
                    if (!imagePath.Contains(RTFile.ApplicationDirectory))
                        imagePath = AssetPack.GetFile(imagePath);

                    CoroutineHelper.StartCoroutine(AlephNetwork.DownloadImageTexture(imagePath, image.AssignTexture));
                }

                if (onClick == null && onDown == null && onUp == null && onClickJSON == null && onDownJSON == null && onUpJSON == null)
                    return;

                var clickable = imageGameObject.AddComponent<ExampleClickable>();
                clickable.onClick = pointerEventData =>
                {
                    if (onClickJSON != null)
                    {
                        var variables = GetVariables();
                        variables["pointerEventButton"] = pointerEventData.button.ToString();
                        model?.partFunctions?.ParseFunction(onClickJSON, this, variables);
                    }
                    onClick?.Invoke(this, pointerEventData);
                };
                clickable.onDown = pointerEventData =>
                {
                    if (onDownJSON != null)
                    {
                        var variables = GetVariables();
                        variables["pointerEventButton"] = pointerEventData.button.ToString();
                        model?.partFunctions?.ParseFunction(onDownJSON, this, variables);
                    }
                    onDown?.Invoke(this, pointerEventData);
                };
                clickable.onUp = pointerEventData =>
                {
                    if (onUpJSON != null)
                    {
                        var variables = GetVariables();
                        variables["pointerEventButton"] = pointerEventData.button.ToString();
                        model?.partFunctions?.ParseFunction(onUpJSON, this, variables);
                    }
                    onUp?.Invoke(this, pointerEventData);
                };
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
                if (jn["sib_index"] != null)
                    siblingIndex = jn["sib_index"].AsInt;
                name = jn["name"];
                rect = RectValues.Parse(jn["rect"], RectValues.Default);
                imageRect = RectValues.Parse(jn["image_rect"], RectValues.Default);
                imagePath = jn["image_path"];

                position = Parser.TryParse(jn["pos"], Vector2.zero);
                scale = Parser.TryParse(jn["sca"], Vector2.one);
                rotation = jn["rot"].AsFloat;
                imageRotation = jn["image_rot"].AsFloat;

                if (jn["on_tick"] != null)
                    onTickJSON = jn["on_tick"];
                if (jn["on_click"] != null)
                    onClickJSON = jn["on_click"];
                if (jn["on_down"] != null)
                    onDownJSON = jn["on_down"];
                if (jn["on_up"] != null)
                    onUpJSON = jn["on_up"];
            }

            public override JSONNode ToJSON()
            {
                var jn = Parser.NewJSONObject();

                if (!string.IsNullOrEmpty(id))
                    jn["id"] = id;
                if (!string.IsNullOrEmpty(parentID))
                    jn["p"] = parentID;
                jn["sib_index"] = siblingIndex;
                if (!string.IsNullOrEmpty(name))
                    jn["name"] = name;
                jn["rect"] = rect.ToJSON();
                jn["image_rect"] = imageRect.ToJSON();
                if (!string.IsNullOrEmpty(imagePath))
                    jn["image_path"] = imagePath;

                if (position.x != 0f || position.y != 0f)
                    jn["pos"] = position.ToJSON();
                if (scale.x != 1f || scale.y != 1f)
                    jn["Sca"] = scale.ToJSON();
                if (rotation != 0f)
                    jn["rot"] = rotation;

                if (onTickJSON != null)
                    jn["on_tick"] = onTickJSON;
                if (onClickJSON != null)
                    jn["on_click"] = onClickJSON;
                if (onDownJSON != null)
                    jn["on_down"] = onDownJSON;
                if (onUpJSON != null)
                    jn["on_up"] = onUpJSON;

                return jn;
            }

            public override string ToString() => id;
        }

        /// <summary>
        /// Library of default parts.
        /// </summary>
        public static class Parts
        {
            /// <summary>
            /// Example's base parent.
            /// </summary>
            public const string BASE = "BASE";

            /// <summary>
            /// Example's head.
            /// </summary>
            public const string HEAD = "HEAD";

            /// <summary>
            /// Example's face parent.
            /// </summary>
            public const string FACE = "FACE";

            /// <summary>
            /// Example's ears parent.
            /// </summary>
            public const string EARS = "EARS";

            /// <summary>
            /// Example's left ear bottom part.
            /// </summary>
            public const string EAR_BOTTOM_LEFT = "EAR_BOTTOM_LEFT";

            /// <summary>
            /// Example's left ear top part.
            /// </summary>
            public const string EAR_TOP_LEFT = "EAR_TOP_LEFT";

            /// <summary>
            /// Example's right ear bottom part.
            /// </summary>
            public const string EAR_BOTTOM_RIGHT = "EAR_BOTTOM_RIGHT";

            /// <summary>
            /// Example's right ear top part.
            /// </summary>
            public const string EAR_TOP_RIGHT = "EAR_TOP_RIGHT";

            /// <summary>
            /// Example's tail.
            /// </summary>
            public const string TAIL = "TAIL";

            /// <summary>
            /// Example's eyes.
            /// </summary>
            public const string EYES = "EYES";

            /// <summary>
            /// Example's pupils.
            /// </summary>
            public const string PUPILS = "PUPILS";

            /// <summary>
            /// Example's top eyelids.
            /// </summary>
            public const string TOP_EYELIDS = "TOP_EYELIDS";

            /// <summary>
            /// Example's bottom eyelids.
            /// </summary>
            public const string BOTTOM_EYELIDS = "BOTTOM_EYELIDS";

            /// <summary>
            /// Example's blinking part.
            /// </summary>
            public const string BLINK = "BLINK";

            /// <summary>
            /// Example's brows parent.
            /// </summary>
            public const string BROWS = "BROWS";

            /// <summary>
            /// Example's left brow.
            /// </summary>
            public const string BROW_LEFT = "BROW_LEFT";

            /// <summary>
            /// Example's right brow.
            /// </summary>
            public const string BROW_RIGHT = "BROW_RIGHT";

            /// <summary>
            /// Example's snout.
            /// </summary>
            public const string SNOUT = "SNOUT";

            /// <summary>
            /// Example's mouth parent.
            /// </summary>
            public const string MOUTH_BASE = "MOUTH_BASE";

            /// <summary>
            /// Example's upper mouth part.
            /// </summary>
            public const string MOUTH_UPPER = "MOUTH_UPPER";

            /// <summary>
            /// Example's lower mouth part.
            /// </summary>
            public const string MOUTH_LOWER = "MOUTH_LOWER";

            /// <summary>
            /// Example's lips.
            /// </summary>
            public const string LIPS = "LIPS";

            /// <summary>
            /// Example's nose.
            /// </summary>
            public const string NOSE = "NOSE";

            /// <summary>
            /// Example's hands parent.
            /// </summary>
            public const string HANDS_BASE = "HANDS_BASE";

            /// <summary>
            /// Example's left hand.
            /// </summary>
            public const string HAND_LEFT = "HAND_LEFT";

            /// <summary>
            /// Example's right hand.
            /// </summary>
            public const string HAND_RIGHT = "HAND_RIGHT";
        }

        public class PartFunctions : JSONFunctionParser<BasePart>
        {
            public override bool IfFunction(JSONNode jn, string name, JSONNode parameters, BasePart thisElement = null, Dictionary<string, JSONNode> customVariables = null)
            {
                switch (name)
                {
                    case "Transform": return thisElement && thisElement.transform;
                    case "CanDrag": return thisElement && thisElement.model && thisElement.model.reference && thisElement.model.reference.canDrag;
                    case "CanDragLeftHand": return thisElement && thisElement.model && thisElement.model.reference && thisElement.model.reference.canDragLeftHand;
                    case "CanDragRightHand": return thisElement && thisElement.model && thisElement.model.reference && thisElement.model.reference.canDragRightHand;
                    case "Dragging": return thisElement && thisElement.model && thisElement.model.reference && thisElement.model.reference.dragging;
                    case "DraggingLeftHand": return thisElement && thisElement.model && thisElement.model.reference && thisElement.model.reference.draggingLeftHand;
                    case "DraggingRightHand": return thisElement && thisElement.model && thisElement.model.reference && thisElement.model.reference.draggingRightHand;
                    case "Leaving": return thisElement && thisElement.model && thisElement.model.reference && thisElement.model.reference.leaving;
                    case "CurrentAction": return thisElement && thisElement.model && thisElement.model.reference && thisElement.model.reference.brain && thisElement.model.reference.brain.CurrentAction;
                }

                return base.IfFunction(jn, name, parameters, thisElement, customVariables);
            }

            public override void Function(JSONNode jn, string name, JSONNode parameters, BasePart thisElement = null, Dictionary<string, JSONNode> customVariables = null)
            {
                switch (name)
                {
                    case "StartDrag": {
                            if (!thisElement || !thisElement.model || !thisElement.model.reference)
                                return;

                            var reference = thisElement.model.reference;

                            CompanionManager.inst.animationController.Remove(x => x.name == "End Drag Example" || x.name == "Drag Example" || x.name.ToLower().Contains("movement"));

                            reference.startMousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y) * CoreHelper.ScreenScaleInverse;
                            reference.startDragPos = new Vector2(thisElement.model.position.x, thisElement.model.position.y);
                            reference.dragPos = new Vector3(thisElement.model.position.x, thisElement.model.position.y);
                            reference.dragging = true;

                            return;
                        }
                    case "Drag": {
                            var reference = thisElement.model.reference;
                            thisElement.model.rotation = (reference.DragTarget.x - reference.dragPos.x) * reference.DragDelay;

                            reference.dragPos += (reference.DragTarget - reference.dragPos) * reference.DragDelay;

                            thisElement.model.position = new Vector3(Mathf.Clamp(reference.dragPos.x, -970f, 970f), Mathf.Clamp(reference.dragPos.y, -560f, 560f));

                            break;
                        }
                    case "EndDrag": {
                            if (!thisElement || !thisElement.model || !thisElement.model.reference)
                                return;

                            var reference = thisElement.model.reference;

                            thisElement.model.facePosition = Vector3.zero;

                            reference.dragging = false;

                            return;
                        }
                    case "StartLeftHandDrag": {
                            if (!thisElement || !thisElement.model || !thisElement.model.reference)
                                return;

                            var reference = thisElement.model.reference;

                            reference.startMousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y) * CoreHelper.ScreenScaleInverse;
                            reference.startDragPos = new Vector2(thisElement.position.x, thisElement.position.y);
                            reference.draggingLeftHand = true;

                            return;
                        }
                    case "EndLeftHandDrag": {
                            if (thisElement && thisElement.model && thisElement.model.reference)
                                thisElement.model.reference.draggingLeftHand = false;

                            try
                            {
                                if (thisElement is ImagePart part)
                                    thisElement?.model?.reference?.brain.SelectObject(part.image);
                            }
                            catch (Exception ex)
                            {
                                CoreHelper.LogException(ex);
                            }

                            return;
                        }
                    case "StartRightHandDrag": {
                            if (!thisElement || !thisElement.model || !thisElement.model.reference)
                                return;

                            var reference = thisElement.model.reference;

                            reference.startMousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y) * CoreHelper.ScreenScaleInverse;
                            reference.startDragPos = new Vector2(thisElement.position.x, thisElement.position.y);
                            reference.draggingRightHand = true;

                            return;
                        }
                    case "EndRightHandDrag": {
                            if (thisElement && thisElement.model && thisElement.model.reference)
                                thisElement.model.reference.draggingRightHand = false;

                            try
                            {
                                if (thisElement is ImagePart part)
                                    thisElement?.model?.reference?.brain.SelectObject(part.image);
                            }
                            catch (Exception ex)
                            {
                                CoreHelper.LogException(ex);
                            }

                            return;
                        }

                    case "SetPose": {
                            if (!thisElement || !thisElement.model)
                                return;

                            var pose = ParseVarFunction(parameters.Get(0, "pose"), thisElement, customVariables);
                            if (!Parser.IsCompatibleString(pose))
                                return;

                            thisElement.model.SetPose(pose);

                            return;
                        }

                    case "SetAttribute": {
                            if (parameters == null || !thisElement || !thisElement.model)
                                return;

                            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables);
                            if (!Parser.IsCompatibleString(id))
                                return;

                            var value = ParseVarFunction(parameters.Get(1, "value"), thisElement, customVariables).AsDouble;
                            var min = ParseVarFunction(parameters.Get(2, "min"), thisElement, customVariables).AsDouble;
                            var max = ParseVarFunction(parameters.Get(3, "max"), thisElement, customVariables).AsDouble;

                            thisElement.model.GetAttribute(id, value, min, max).Value = value;
                            return;
                        }

                    case "SetFacePosition": {
                            if (!thisElement || !thisElement.model)
                                return;

                            thisElement.model.facePosition = new Vector2(ParseVarFunction(parameters.Get(0, "x"), thisElement, customVariables).AsFloat, ParseVarFunction(parameters.Get(1, "y"), thisElement, customVariables).AsFloat);

                            return;
                        }

                    #region PlaySound

                    // Plays a sound. Can either be a default one already loaded in the SoundLibrary or a custom one from the menu's folder.
                    // Supports both JSON array and JSON object.
                    //
                    // - JSON Array Structure -
                    // 0 = sound
                    // 1 = volume
                    // 2 = pitch
                    // Example:
                    // [
                    //   "blip" < plays the blip sound.
                    //   "0.3" < sound is quiet.
                    //   "2" < sound is fast.
                    // ]
                    //
                    // - JSON Object Structure -
                    // "sound"
                    // "vol"
                    // "pitch"
                    // Example:
                    // {
                    //   "sound": "some kind of sound.ogg" < since this sound does not exist in the SoundLibrary, search for a file with the name. If it exists, play the sound.
                    //   "vol": "1" < default
                    //   "pitch": "0.5" < slow
                    // }
                    case "PlaySound": {
                            if (parameters == null || !thisElement || !thisElement.model)
                                return;

                            string sound = ParseVarFunction(parameters.Get(0, "sound"));
                            if (string.IsNullOrEmpty(sound))
                                return;

                            float volume = 1f;
                            var volumeJN = ParseVarFunction(parameters.Get(1, "vol"));
                            if (volumeJN != null)
                                volume = volumeJN;
                        
                            float pitch = 1f;
                            var pitchJN = ParseVarFunction(parameters.Get(2, "pitch"));
                            if (pitchJN != null)
                                pitch = pitchJN;

                            if (SoundManager.inst.TryGetSound(sound, out AudioClip audioClip))
                            {
                                SoundManager.inst.PlaySound(thisElement.model.baseCanvas, audioClip, volume, pitch);
                                return;
                            }

                            //var filePath = $"{Path.GetDirectoryName(inst.CurrentInterface.filePath)}{sound}";
                            //if (!RTFile.FileExists(filePath))
                            //    return;

                            //var audioType = RTFile.GetAudioType(filePath);
                            //if (audioType == AudioType.MPEG)
                            //    SoundManager.inst.PlaySound(LSAudio.CreateAudioClipUsingMP3File(filePath), volume, pitch);
                            //else
                            //    CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip($"file://{filePath}", audioType, audioClip => SoundManager.inst.PlaySound(audioClip, volume, pitch)));

                            return;
                        }

                    #endregion

                    case "ToggleOptions": {
                            thisElement?.model?.reference?.options?.Toggle();
                            break;
                        }
                    case "Interact": {
                            thisElement?.model?.reference?.brain?.Interact(ParseVarFunction(parameters.Get(0, "context"), thisElement, customVariables));
                            break;
                        }

                    case "SetPartPosition": {
                            if (parameters == null)
                                return;

                            thisElement?.SetPosition(new Vector2(ParseVarFunction(parameters.Get(0, "x"), thisElement, customVariables).AsFloat, ParseVarFunction(parameters.Get(1, "y"), thisElement, customVariables).AsFloat));
                            return;
                        }
                    case "SetPartScale": {
                            if (parameters == null)
                                return;

                            thisElement?.SetScale(new Vector2(ParseVarFunction(parameters.Get(0, "x"), thisElement, customVariables).AsFloat, ParseVarFunction(parameters.Get(1, "y"), thisElement, customVariables).AsFloat));
                            return;
                        }
                    case "SetPartRotation": {
                            if (parameters == null)
                                return;

                            thisElement?.SetRotation(ParseVarFunction(parameters.Get(0, "x"), thisElement, customVariables).AsFloat);
                            return;
                        }

                    case "SetPartImageRotation": {
                            if (parameters == null || thisElement is not ImagePart imagePart)
                                return;

                            imagePart.imageRotation = ParseVarFunction(parameters.Get(0, "x"), thisElement, customVariables).AsFloat;
                            return;
                        }

                    case "StopCurrentAction": {
                            if (thisElement && thisElement.model && thisElement.model.reference && thisElement.model.reference.brain && thisElement.model.reference.brain.CurrentAction)
                                thisElement.model.reference.brain.StopCurrentAction();
                            break;
                        }

                    case "UpdatePupilsOffset": {
                            if (thisElement && thisElement.model)
                                thisElement.model.pupilsOffset = new Vector2(UnityEngine.Random.Range(0f, 0.5f), UnityEngine.Random.Range(0f, 0.5f));
                            return;
                        }
                    case "UpdateBlinking": {
                        if (!thisElement || !thisElement.gameObject || !thisElement.model)
                            return;

                            var reference = thisElement.model.reference;

                            float t = reference.timer.time % CompanionManager.BLINK_RATE;

                            if (!reference.Dragging && thisElement.model.GetAttribute("ALLOW_BLINKING").Value == 1.0 && thisElement.model.GetAttribute("DANCING").Value == 0.0 && thisElement.model.GetAttribute("POKING_EYES").Value == 0.0)
                            {
                                var blinkingAttribute = thisElement.model.GetAttribute("IS_BLINKING");
                                var canUnblinkAttribute = thisElement.model.GetAttribute("CAN_UNBLINK");
                                if (t > CompanionManager.BLINK_RATE - 0.3f && canUnblinkAttribute.Value == 1.0)
                                    blinkingAttribute.Value = RandomHelper.PercentChance(45) ? 1.0 : 0.0;

                                var active = t > CompanionManager.BLINK_RATE - 0.3f && blinkingAttribute.Value == 1.0;
                                canUnblinkAttribute.Value = active ? 0.0 : 1.0;
                                thisElement.gameObject.SetActive(active);
                            }
                            else
                                thisElement.gameObject.SetActive(true);
                            return;
                        }
                }
                base.Function(jn, name, parameters, thisElement, customVariables);
            }

            public override JSONNode VarFunction(JSONNode jn, string name, JSONNode parameters, BasePart thisElement = null, Dictionary<string, JSONNode> customVariables = null)
            {
                switch (name)
                {
                    case "GetAttribute": {
                            if (parameters == null || !thisElement || !thisElement.model)
                                return 0.0;

                            var id = ParseVarFunction(parameters.Get(0, "id"), thisElement, customVariables);
                            if (!Parser.IsCompatibleString(id))
                                return 0.0;

                            var defaultValue = ParseVarFunction(parameters.Get(1, "default"), thisElement, customVariables).AsDouble;
                            var min = ParseVarFunction(parameters.Get(2, "min"), thisElement, customVariables).AsDouble;
                            var max = ParseVarFunction(parameters.Get(3, "max"), thisElement, customVariables).AsDouble;

                            return thisElement.model.GetAttribute(id, defaultValue, min, max).Value;
                        }
                }

                return base.VarFunction(jn, name, parameters, thisElement, customVariables);
            }
        }

        #endregion

        #region Animations

        #region Poses

        /// <summary>
        /// List of that can be used.
        /// </summary>
        public List<ExamplePose> poses = new List<ExamplePose>();

        /// <summary>
        /// Registers the model's poses.
        /// </summary>
        public virtual void RegisterPoses()
        {
            poses.Clear();
            poses.Add(new ExamplePose(Poses.IDLE, (model, parameters) =>
            {
                var handsBase = model.GetPart(Parts.HANDS_BASE);
                var handLeft = model.GetPart(Parts.HAND_LEFT);
                var handRight = model.GetPart(Parts.HAND_RIGHT);

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
                        model.GetAttribute("FACE_CAN_LOOK").Value = 1.0;
                        model.GetAttribute("ALLOW_BLINKING").Value = 1.0;
                    }),
                };

                return animation;
            }));
            poses.Add(new ExamplePose(Poses.ENTER, (model, parameters) =>
            {
                var handLeft = model.GetPart(Parts.HAND_LEFT);
                var handRight = model.GetPart(Parts.HAND_RIGHT);

                var pose = parameters is RandomPoseParameters randomPose && randomPose.poseSelection != null ?
                    randomPose.poseSelection() :
                    UnityEngine.Random.Range(0, 3);

                var animation = new RTAnimation("Hiii");
                switch (pose)
                {
                    case 1: {
                            ExceptionHelper.NullReference(handLeft, "Example Hand Left");
                            ExceptionHelper.NullReference(handRight, "Example Hand Right");

                            animation.animationHandlers = new List<AnimationHandlerBase>
                            {
                                new AnimationHandler<float>(new List<IKeyframe<float>>
                                {
                                    new FloatKeyframe(0f, 0f, Ease.Linear),
                                    new FloatKeyframe(0.6f, 800f, Ease.SineOut),
                                    new FloatKeyframe(1f, 750f, Ease.SineInOut),
                                }, x => model.position.x = x, interpolateOnComplete: true),
                                new AnimationHandler<float>(new List<IKeyframe<float>>
                                {
                                    new FloatKeyframe(0f, -1200f, Ease.Linear),
                                    new FloatKeyframe(0.45f, -380f, Ease.SineOut),
                                    new FloatKeyframe(0.8f, -410f, Ease.SineInOut),
                                }, x => model.position.y = x, interpolateOnComplete: true),
                                new AnimationHandler<float>(new List<IKeyframe<float>>
                                {
                                    new FloatKeyframe(0f, 0f, Ease.Linear),
                                    new FloatKeyframe(0.5f, 150f, Ease.SineOut),
                                    new FloatKeyframe(0.8f, 130f, Ease.SineInOut),
                                    new FloatKeyframe(1.2f, 150f, Ease.SineInOut),
                                    new FloatKeyframe(1.7f, 130f, Ease.SineInOut),
                                    new FloatKeyframe(2f, 0f, Ease.SineInOut),
                                }, x => handLeft.rotation = x, interpolateOnComplete: true),
                                new AnimationHandler<float>(new List<IKeyframe<float>>
                                {
                                    new FloatKeyframe(0f, 0f, Ease.Linear),
                                    new FloatKeyframe(0.5f, -150f, Ease.SineOut),
                                    new FloatKeyframe(0.8f, -130f, Ease.SineInOut),
                                    new FloatKeyframe(1.2f, -150f, Ease.SineInOut),
                                    new FloatKeyframe(1.7f, -130f, Ease.SineInOut),
                                    new FloatKeyframe(2f, 0f, Ease.SineInOut),
                                }, x => handRight.rotation = x, interpolateOnComplete: true),
                            };

                            break;
                        }
                    case 2: {
                            ExceptionHelper.NullReference(handLeft, "Example Hand Left");
                            ExceptionHelper.NullReference(handRight, "Example Hand Right");

                            animation.animationHandlers = new List<AnimationHandlerBase>
                            {
                                new AnimationHandler<float>(new List<IKeyframe<float>>
                                {
                                    new FloatKeyframe(0f, 750f, Ease.Linear),
                                    new FloatKeyframe(1f, 750f, Ease.Linear),
                                }, x => model.position.x = x, interpolateOnComplete: true),
                                new AnimationHandler<float>(new List<IKeyframe<float>>
                                {
                                    new FloatKeyframe(0f, -410f, Ease.Linear),
                                    new FloatKeyframe(1f, -410f, Ease.Linear),
                                }, x => model.position.y = x, interpolateOnComplete: true),
                                new AnimationHandler<float>(new List<IKeyframe<float>>
                                {
                                    new FloatKeyframe(0f, 0f, Ease.Linear),
                                    new FloatKeyframe(1f, 1, Ease.Linear),
                                }, x => model.opacity = x, interpolateOnComplete: true),
                            };

                            break;
                        }
                    default: {
                            ExceptionHelper.NullReference(handLeft, "Example Hand Left");

                            animation.animationHandlers = new List<AnimationHandlerBase>
                            {
                                new AnimationHandler<float>(new List<IKeyframe<float>>
                                {
                                    new FloatKeyframe(0f, 0f, Ease.Linear),
                                    new FloatKeyframe(0.5f, 800f, Ease.SineOut),
                                    new FloatKeyframe(0.8f, 750f, Ease.SineInOut),
                                }, x => model.position.x = x, interpolateOnComplete: true),
                                new AnimationHandler<float>(new List<IKeyframe<float>>
                                {
                                    new FloatKeyframe(0f, -1200f, Ease.Linear),
                                    new FloatKeyframe(0.3f, -380f, Ease.SineOut),
                                    new FloatKeyframe(0.6f, -410f, Ease.SineInOut),
                                }, x => model.position.y = x, interpolateOnComplete: true),
                                new AnimationHandler<float>(new List<IKeyframe<float>>
                                {
                                    new FloatKeyframe(0f, 0f, Ease.Linear),
                                    new FloatKeyframe(0.5f, 150f, Ease.SineOut),
                                    new FloatKeyframe(0.8f, 130f, Ease.SineInOut),
                                    new FloatKeyframe(1.2f, 150f, Ease.SineInOut),
                                    new FloatKeyframe(1.7f, 130f, Ease.SineInOut),
                                    new FloatKeyframe(2f, 0f, Ease.SineInOut),
                                }, x => handLeft.rotation = x, interpolateOnComplete: true),
                            };

                            break;
                        }
                }

                return animation;
            }));
            poses.Add(new ExamplePose(Poses.LEAVE, (model, parameters) =>
            {
                var pose = parameters is RandomPoseParameters randomPose && randomPose.poseSelection != null ?
                    randomPose.poseSelection() :
                    UnityEngine.Random.Range(0, 3);

                var animation = new RTAnimation("Cya");
                switch (pose)
                {
                    case 1: {
                            animation.animationHandlers = new List<AnimationHandlerBase>
                            {
                                new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                                {
                                    new Vector2Keyframe(0f, Vector2.one, Ease.Linear),
                                    new Vector2Keyframe(1.5f, Vector2.zero, Ease.BackIn),
                                }, vector2 => model.scale = vector2, interpolateOnComplete: true),
                            };

                            break;
                        }
                    case 2: {
                            animation.animationHandlers = new List<AnimationHandlerBase>
                            {
                                new AnimationHandler<float>(new List<IKeyframe<float>>
                                {
                                    new FloatKeyframe(0f, 1f, Ease.Linear),
                                    new FloatKeyframe(1.5f, 0f, Ease.Linear),
                                }, x => model.opacity = x, interpolateOnComplete: true),
                            };

                            break;
                        }
                    default: {
                            animation.animationHandlers = new List<AnimationHandlerBase>
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
                            };

                            break;
                        }
                }

                return animation;
            }));
            poses.Add(new ExamplePose(Poses.WORRY, (model, parameters) =>
            {
                var browLeft = model.GetPart(Parts.BROW_LEFT);
                var browRight = model.GetPart(Parts.BROW_RIGHT);

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
                var browLeft = model.GetPart(Parts.BROW_LEFT);
                var browRight = model.GetPart(Parts.BROW_RIGHT);

                var topEyelids = model.GetPart(Parts.TOP_EYELIDS);
                var bottomEyelids = model.GetPart(Parts.BOTTOM_EYELIDS);

                ExceptionHelper.NullReference(browLeft, "Example Brow Left");
                ExceptionHelper.NullReference(browRight, "Example Brow Right");

                ExceptionHelper.NullReference(topEyelids, "Example Top Eyelids Image Part");
                ExceptionHelper.NullReference(bottomEyelids, "Example Bottom Eyelids Image Part");

                var animation = new RTAnimation("Angry");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    // Brows
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

                    // Eyelids
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, topEyelids.scale.y, Ease.Linear),
                        new FloatKeyframe(0.3f, 0.5f, Ease.SineOut),
                    }, x => topEyelids.scale.y = x),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, bottomEyelids.scale.y, Ease.Linear),
                        new FloatKeyframe(0.3f, 0.5f, Ease.SineOut),
                    }, x => bottomEyelids.scale.y = x),
                };

                return animation;
            }));
            poses.Add(new ExamplePose(Poses.START_DRAG, (model, parameters) =>
            {
                GetAttribute("FACE_CAN_LOOK").Value = 0.0;

                var head = model.GetPart(Parts.HEAD);

                var browLeft = model.GetPart(Parts.BROW_LEFT);
                var browRight = model.GetPart(Parts.BROW_RIGHT);

                var lips = model.GetPart(Parts.LIPS);

                var handsBase = model.GetPart(Parts.HANDS_BASE);
                var handLeft = model.GetPart(Parts.HAND_LEFT).transform.GetChild(0);
                var handRight = model.GetPart(Parts.HAND_RIGHT).transform.GetChild(0);

                var topEyelids = model.GetPart(Parts.TOP_EYELIDS);
                var bottomEyelids = model.GetPart(Parts.BOTTOM_EYELIDS);

                ExceptionHelper.NullReference(lips, "Example Lips");
                ExceptionHelper.NullReference(handLeft, "Example Hand Left");
                ExceptionHelper.NullReference(handRight, "Example Hand Right");
                ExceptionHelper.NullReference(browLeft, "Example Brow Left");
                ExceptionHelper.NullReference(browRight, "Example Brow Right");
                ExceptionHelper.NullReference(handsBase, "Example Hands Base");
                ExceptionHelper.NullReference(head, "Example Head");

                ExceptionHelper.NullReference(topEyelids, "Example Top Eyelids Image Part");
                ExceptionHelper.NullReference(bottomEyelids, "Example Bottom Eyelids Image Part");

                var animation = new RTAnimation("Drag Example");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, head.position.y, Ease.Linear),
                        new FloatKeyframe(0.2f, 10f, Ease.SineOut),
                    }, x => head.position.y = x, interpolateOnComplete: true),
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
                        new FloatKeyframe(0f, lips.scale.y, Ease.Linear),
                        new FloatKeyframe(0.2f, 0.5f, Ease.SineOut),
                    }, x => lips.scale.y = x, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, lips.position.y, Ease.Linear),
                        new FloatKeyframe(0.2f, 2f, Ease.SineOut),
                    }, x => lips.position.y = x, interpolateOnComplete: true),

                    // Eye lids
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, topEyelids.scale.y, Ease.Linear),
                        new FloatKeyframe(0.3f, 0f, Ease.SineOut),
                    }, x => topEyelids.scale.y = x),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, bottomEyelids.scale.y, Ease.Linear),
                        new FloatKeyframe(0.3f, 0f, Ease.SineOut),
                    }, x => bottomEyelids.scale.y = x),
                };
                return animation;
            }));
            poses.Add(new ExamplePose(Poses.END_DRAG, (model, parameters) =>
            {
                GetAttribute("FACE_CAN_LOOK").Value = 1.0;

                var head = model.GetPart(Parts.HEAD);

                var browLeft = model.GetPart(Parts.BROW_LEFT);
                var browRight = model.GetPart(Parts.BROW_RIGHT);

                var lips = model.GetPart(Parts.LIPS);

                var handsBase = model.GetPart(Parts.HANDS_BASE);
                var handLeft = model.GetPart(Parts.HAND_LEFT);
                var handRight = model.GetPart(Parts.HAND_RIGHT);

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
                        new FloatKeyframe(0f, head.position.y, Ease.Linear),
                        new FloatKeyframe(0.5f, 0f, Ease.BounceOut),
                    }, x => head.position.y = x, interpolateOnComplete: true),

                    // Mouth
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, mouthOpenAmount, Ease.Linear),
                        new FloatKeyframe(0.2f, 0.5f, Ease.SineIn),
                    }, x => mouthOpenAmount = x, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, lips.scale.y, Ease.Linear),
                        new FloatKeyframe(0.2f, 1f, Ease.SineIn),
                    }, x => lips.scale.y = x, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, lips.position.y, Ease.Linear),
                        new FloatKeyframe(0.2f, 0f, Ease.SineIn),
                    }, x => lips.position.y = x, interpolateOnComplete: true),

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
                        new FloatKeyframe(0f, handLeft.transform.GetChild(0).localPosition.y, Ease.Linear),
                        new FloatKeyframe(0.1f, handLeft.transform.GetChild(0).localPosition.y, Ease.Linear),
                        new FloatKeyframe(0.7f, -80f, Ease.BounceOut),
                    }, handLeft.transform.GetChild(0).SetLocalPositionY, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, handRight.transform.GetChild(0).localPosition.y, Ease.Linear),
                        new FloatKeyframe(0.1f, handRight.transform.GetChild(0).localPosition.y, Ease.Linear),
                        new FloatKeyframe(0.7f, -80f, Ease.BounceOut),
                    }, handRight.transform.GetChild(0).SetLocalPositionY, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, handLeft.rotation, Ease.Linear),
                        new FloatKeyframe(0.1f, handLeft.rotation, Ease.Linear),
                        new FloatKeyframe(0.7f, 0f, Ease.BounceOut),
                    }, handLeft.SetRotation, interpolateOnComplete: true),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, handRight.rotation, Ease.Linear),
                        new FloatKeyframe(0.1f, handRight.rotation, Ease.Linear),
                        new FloatKeyframe(0.7f, 0f, Ease.BounceOut),
                    }, handRight.SetRotation, interpolateOnComplete: true),
                };

                return animation;
            }));
            poses.Add(new ExamplePose(Poses.LOOK_AT, (model, parameters) =>
            {
                var face = model.GetPart(Parts.FACE);
                var pupils = model.GetPart(Parts.PUPILS);

                var animation = new RTAnimation("Look At");

                if (parameters is LookAtPoseParameters lookAtParameters)
                {
                    var animationHandlers = new List<AnimationHandlerBase>();
                    if (lookAtParameters.disableFaceAuto)
                    {
                        model.GetAttribute("FACE_CAN_LOOK").Value = 0.0;
                        animationHandlers.Add(new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                        {
                            new Vector2Keyframe(0f, face.position, Ease.Linear),
                            new Vector2Keyframe(parameters.transitionTime, lookAtParameters.faceLookAt, Ease.SineOut),
                        }, face.SetPosition, interpolateOnComplete: true));
                    }
                    if (lookAtParameters.disablePupilsAuto)
                    {
                        model.GetAttribute("PUPILS_CAN_LOOK").Value = 0.0;
                        animationHandlers.Add(new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                        {
                            new Vector2Keyframe(0f, pupils.position, Ease.Linear),
                            new Vector2Keyframe(parameters.transitionTime, lookAtParameters.pupilsLookAt, Ease.SineOut),
                        }, pupils.SetPosition, interpolateOnComplete: true));
                    }
                    animation.animationHandlers = animationHandlers;
                }

                return animation;
            }));
            poses.Add(new ExamplePose(Poses.EAR_LEFT_FLICK, (model, parameters) =>
            {
                var part = model.GetPart(Parts.EAR_TOP_LEFT) as ImagePart;

                ExceptionHelper.NullReference(part, "Example Ear Left Image Part");

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

                return animation;
            }));
            poses.Add(new ExamplePose(Poses.EAR_RIGHT_FLICK, (model, parameters) =>
            {
                var part = model.GetPart(Parts.EAR_TOP_RIGHT) as ImagePart;

                ExceptionHelper.NullReference(part, "Example Ear Right Image Part");

                var animation = new RTAnimation("Ear Right Flick");
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

                return animation;
            }));
            poses.Add(new ExamplePose(Poses.END_LEFT_HAND_DRAG, (model, parameters) =>
            {
                var handLeft = model.GetPart(Parts.HAND_LEFT);

                ExceptionHelper.NullReference(handLeft, "Example Hand Left");

                var animation = new RTAnimation("Example Hand Reset");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                    {
                        new Vector2Keyframe(0f, handLeft.position, Ease.Linear),
                        new Vector2Keyframe(0.3f, new Vector2(40f, 0f), Ease.SineOut),
                        new Vector2Keyframe(0.32f, new Vector2(40f, 0f), Ease.Linear),
                    }, handLeft.SetPosition, interpolateOnComplete: true),
                };

                return animation;
            }));
            poses.Add(new ExamplePose(Poses.END_RIGHT_HAND_DRAG, (model, parameters) =>
            {
                var handLeft = model.GetPart(Parts.HAND_RIGHT);

                ExceptionHelper.NullReference(handLeft, "Example Hand Right");

                var animation = new RTAnimation("Example Hand Reset");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                    {
                        new Vector2Keyframe(0f, handLeft.position, Ease.Linear),
                        new Vector2Keyframe(0.3f, new Vector2(-40f, 0f), Ease.SineOut),
                        new Vector2Keyframe(0.32f, new Vector2(-40f, 0f), Ease.Linear),
                    }, handLeft.SetPosition, interpolateOnComplete: true),
                };

                return animation;
            }));
            poses.Add(new ExamplePose(Poses.TIRED, (model, parameters) =>
            {
                var topEyelids = model.GetPart(Parts.TOP_EYELIDS);
                var bottomEyelids = model.GetPart(Parts.BOTTOM_EYELIDS);

                ExceptionHelper.NullReference(topEyelids, "Example Top Eyelids Image Part");
                ExceptionHelper.NullReference(bottomEyelids, "Example Bottom Eyelids Image Part");

                var animation = new RTAnimation("Tired");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, topEyelids.scale.y, Ease.Linear),
                        new FloatKeyframe(0.3f, 0.5f, Ease.SineOut),
                    }, x => topEyelids.scale.y = x),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, bottomEyelids.scale.y, Ease.Linear),
                        new FloatKeyframe(0.3f, 0.5f, Ease.SineOut),
                    }, x => bottomEyelids.scale.y = x),
                };

                return animation;
            }));
        }

        /// <summary>
        /// Overrides an existing pose.
        /// </summary>
        /// <param name="key">Key of the pose to override.</param>
        /// <param name="pose">Pose to override.</param>
        public void OverridePose(string key, ExamplePose pose)
        {
            if (poses.TryFindIndex(x => x.key == key, out int index))
                poses[index] = pose;
        }

        /// <summary>
        /// Sets the current pose of the model.
        /// </summary>
        /// <param name="key">Pose to set.</param>
        /// <param name="parameters">Pose parameters to pass.</param>
        /// <param name="onCompleteAnim">Function to run when pose animation is completed.</param>
        public virtual void SetPose(string key, PoseParameters parameters = null, Action<RTAnimation> onCompleteAnim = null)
        {
            if (!parameters)
                parameters = new PoseParameters();

            for (int i = 0; i < poses.Count; i++)
            {
                var pose = poses[i];
                if (key != pose.key)
                    continue;

                var animation = pose.get?.Invoke(this, parameters);
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
        /// Library of default poses.
        /// </summary>
        public static class Poses
        {
            /// <summary>
            /// Example resets his pose.
            /// </summary>
            public const string IDLE = "Idle";

            /// <summary>
            /// Example enters the scene.
            /// </summary>
            public const string ENTER = "Enter";

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

            /// <summary>
            /// Example looks at something.
            /// </summary>
            public const string LOOK_AT = "LOOK_AT";

            /// <summary>
            /// Example's left ear flicks.
            /// </summary>
            public const string EAR_LEFT_FLICK = "Ear Left Flick";

            /// <summary>
            /// Example's right ear flicks.
            /// </summary>
            public const string EAR_RIGHT_FLICK = "Ear Right Flick";

            /// <summary>
            /// Examples' hand resets after being dragged.
            /// </summary>
            public const string END_LEFT_HAND_DRAG = "End Left Hand Drag";
            
            /// <summary>
            /// Examples' hand resets after being dragged.
            /// </summary>
            public const string END_RIGHT_HAND_DRAG = "End Right Hand Drag";

            /// <summary>
            /// Example is tired.
            /// </summary>
            public const string TIRED = "Tired";

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

            var handsBase = GetPart(Parts.HANDS_BASE);
            var head = GetPart(Parts.HEAD);

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
                }, x => head.position.x = x),
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f,   0f, Ease.Linear),
                    new FloatKeyframe(0.4f, 1f, Ease.SineOut),
                    new FloatKeyframe(0.8f, 0f, Ease.SineIn),
                    new FloatKeyframe(1.2f, 1f, Ease.SineOut),
                    new FloatKeyframe(1.6f, 0f, Ease.SineIn),
                }, x => head.position.y = x),
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

            SetPose(Poses.WORRY);
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

        RTAnimation danceLoopAnimation;

        #endregion
    }
}
