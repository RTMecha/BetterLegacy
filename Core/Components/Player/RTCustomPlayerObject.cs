using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using TMPro;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Components.Player
{
    /// <summary>
    /// Represents a custom object from the model.
    /// </summary>
    public class RTCustomPlayerObject : RTPlayerObject, ITransformable, IModifierReference, ICustomActivatable
    {
        #region Values

        public ModifierLoop loop = new ModifierLoop(null, new Dictionary<string, string>());

        public CustomPlayerObject reference;
        public TextMeshPro text;
        public bool idle = true;
        public string currentIdleAnimation = PlayerModel.IDLE_ANIM;

        public bool CustomActive { get; set; } = true;

        public ObjectTransform.Struct anim = ObjectTransform.Struct.Default;

        public Vector3 positionOffset;
        public Vector3 scaleOffset;
        public Vector3 rotationOffset;

        public Vector3 PositionOffset { get => positionOffset; set => positionOffset = value; }
            
        public Vector3 ScaleOffset { get => scaleOffset; set => scaleOffset = value; }

        public Vector3 RotationOffset { get => rotationOffset; set => rotationOffset = value; }

        public MathOperation PositionOperation { get; set; }

        public MathOperation ScaleOperation { get; set; }

        public MathOperation RotationOperation { get; set; }

        public RTLevelBase ParentRuntime { get; set; }

        public ModifierReferenceType ReferenceType => ModifierReferenceType.PlayerObject;

        public int IntVariable { get; set; }

        #endregion

        #region Functions

        void Awake() => isCustom = true;

        public override void UpdateObject(int index)
        {
            if (!Player)
                throw new NullReferenceException($"Player is null! {id}");

            loop.reference = this;
            loop.Run(reference.Modifiers);

            var active = this.active &&
                (reference.visibilitySettings.IsEmpty() ? reference.active :
                    reference.requireAll ?
                        reference.visibilitySettings.All(x => Player.CheckVisibility(x)) :
                        reference.visibilitySettings.Any(x => Player.CheckVisibility(x)));

            visualObject.SetActive(active);

            if (!active)
                return;

            if (text)
                text.color = RTColors.GetPlayerColor(index, reference.color, reference.opacity, reference.customColor);
            else if (renderer)
                renderer.material.color = RTColors.GetPlayerColor(index, reference.color, reference.opacity, reference.customColor);

            if (!idle || reference.animations.IsEmpty())
            {
                var origPos = reference.position;
                var origSca = reference.scale;
                var origRot = reference.rotation;

                visualObject.transform.localPosition = new Vector3(origPos.x + positionOffset.x, origPos.y + positionOffset.y, reference.depth + positionOffset.z) + anim.position;
                visualObject.transform.localScale = new Vector3(origSca.x + scaleOffset.x, origSca.y + scaleOffset.y, 1f + scaleOffset.z) * anim.scale;
                visualObject.transform.localEulerAngles = new Vector3(rotationOffset.x, rotationOffset.y, origRot + rotationOffset.z + anim.rotation);
                return;
            }

            bool hasIdle = false;
            reference.animations.ForLoop(animation =>
            {
                if (string.IsNullOrEmpty(animation.ReferenceID) || animation.ReferenceID.ToLower().Remove(" ") != currentIdleAnimation)
                    return;

                if (ProjectArrhythmia.State.InEditor && AnimationEditor.inst && AnimationEditor.inst.CurrentAnimation && AnimationEditor.inst.CurrentAnimation.id == animation.id)
                    return;

                var length = animation.GetLength();
                var origPos = reference.position;
                var origSca = reference.scale;
                var origRot = reference.rotation;

                if (animation.animatePosition)
                {
                    var position = GameData.InterpolateVector3Keyframes(animation.positionKeyframes, Player.time % length);
                    visualObject.transform.localPosition = (new Vector3(origPos.x, origPos.y, reference.depth) + position + positionOffset + anim.position);
                }
                else
                    visualObject.transform.localPosition = new Vector3(origPos.x + positionOffset.x, origPos.y + positionOffset.y, reference.depth + positionOffset.z) + anim.position;

                if (animation.animateScale)
                {
                    var scale = GameData.InterpolateVector2Keyframes(animation.scaleKeyframes, Player.time % length);
                    visualObject.transform.localScale = new Vector3(origSca.x * scale.x + scaleOffset.x, origSca.y * scale.y + scaleOffset.y, 1f + scaleOffset.z) * anim.scale;
                }
                else
                    visualObject.transform.localScale = new Vector3(origSca.x + scaleOffset.x, origSca.y + scaleOffset.y, 1f + scaleOffset.z) * anim.scale;

                if (animation.animateRotation)
                {
                    var rotation = GameData.InterpolateFloatKeyframes(animation.rotationKeyframes, Player.time % length, 0);
                    visualObject.transform.localEulerAngles = new Vector3(rotationOffset.x, rotationOffset.y, origRot + rotation + rotationOffset.z + anim.rotation);
                }
                else
                    visualObject.transform.localEulerAngles = new Vector3(rotationOffset.x, rotationOffset.y, origRot + rotationOffset.z + anim.rotation);
                hasIdle = true;
            });

            // no idle animation was found so update transforms
            if (!hasIdle)
            {
                var origPos = reference.position;
                var origSca = reference.scale;
                var origRot = reference.rotation;

                visualObject.transform.localPosition = new Vector3(origPos.x + positionOffset.x, origPos.y + positionOffset.y, reference.depth + positionOffset.z) + anim.position;
                visualObject.transform.localScale = new Vector3(origSca.x + scaleOffset.x, origSca.y + scaleOffset.y, 1f + scaleOffset.z) * anim.scale;
                visualObject.transform.localEulerAngles = new Vector3(rotationOffset.x, rotationOffset.y, origRot + rotationOffset.z + anim.rotation);
            }
        }

        public void ResetOffsets()
        {
            anim = ObjectTransform.Struct.Default;
            positionOffset = Vector3.zero;
            scaleOffset = Vector3.zero;
            rotationOffset = Vector3.zero;

            PositionOperation = MathOperation.Addition;
            ScaleOperation = MathOperation.Addition;
            RotationOperation = MathOperation.Addition;
        }

        public Vector3 GetTransformOffset(int type) => type switch
        {
            0 => positionOffset,
            1 => scaleOffset,
            _ => rotationOffset,
        };

        public void SetTransform(int toType, Vector3 value)
        {
            switch (toType)
            {
                case 0: {
                        positionOffset = value;
                        break;
                    }
                case 1: {
                        scaleOffset = value;
                        break;
                    }
                case 2: {
                        rotationOffset = value;
                        break;
                    }
            }
        }

        public void SetTransform(int toType, int toAxis, float value)
        {
            switch (toType)
            {
                case 0: {
                        positionOffset[toAxis] = value;
                        break;
                    }
                case 1: {
                        scaleOffset[toAxis] = value;
                        break;
                    }
                case 2: {
                        rotationOffset[toAxis] = value;
                        break;
                    }
            }
        }
        public Vector3 GetFullPosition() => visualObject.transform.position;

        public Vector3 GetFullScale() => visualObject.transform.lossyScale;

        public Vector3 GetFullRotation(bool includeSelf) => visualObject.transform.eulerAngles;

        public IRTObject GetRuntimeObject() => null;

        public IPrefabable AsPrefabable() => null;

        public ITransformable AsTransformable() => this;

        public ModifierLoop GetModifierLoop() => loop;

        public void InterpolateAnimation(PAAnimation animation, float t)
        {
            var allEvents = animation.Events;
            for (int i = 0; i < 3; i++)
            {
                if (i >= allEvents.Count)
                    break;

                var events = animation.GetEventKeyframes(i);
                if (events.IsEmpty())
                    continue;

                switch (i)
                {
                    case 0: {
                            anim.position.x = animation.Interpolate(i, 0, t);
                            anim.position.y = animation.Interpolate(i, 1, t);
                            anim.position.z = animation.Interpolate(i, 2, t);
                            break;
                        }
                    case 1: {
                            anim.scale.x = animation.Interpolate(i, 0, t);
                            anim.scale.y = animation.Interpolate(i, 1, t);
                            break;
                        }
                    case 2: {
                            anim.rotation = animation.Interpolate(i, 0, t);
                            break;
                        }
                }
            }
        }

        public void SetCustomActive(bool active)
        {
            CustomActive = active;
            this.active = active;
        }

        public override string ToString() => reference ? reference.name : base.ToString();

        #endregion
    }

}
