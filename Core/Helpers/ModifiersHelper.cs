using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Helpers
{
    /// <summary>
    /// Helper class for modifier functions.
    /// </summary>
    public static class ModifiersHelper
    {
        // TODO: cleanup these functions

        #region VG Convert

        public static int GetLevelTriggerType(string key) => key switch
        {
            "time" => 0,
            "timeInRange" => 0,
            nameof(ModifierFunctions.onPlayerHit) => 1,
            nameof(ModifierFunctions.onPlayerDeath) => 2,
            nameof(ModifierFunctions.onLevelStart) => 3,
            nameof(ModifierFunctions.onLevelRestart) => 4,
            nameof(ModifierFunctions.onLevelRewind) => 5,
            _ => -1,
        };
        
        public static int GetLevelActionType(string key) => key switch
        {
            "vnInk" => 0,
            "vnTimeline" => 1,
            "playerDialogue" => 2,
            nameof(ModifierFunctions.playerMoveAll) => 3,
            nameof(ModifierFunctions.playerLockBoostAll) => 4,
            nameof(ModifierFunctions.playerLockXAll) => 5,
            nameof(ModifierFunctions.playerLockYAll) => 6,
            "bgSpin" => 7,
            "bgMove" => 8,
            nameof(ModifierFunctions.playerBoostAll) => 9,
            nameof(ModifierFunctions.setMusicTime) => 10,
            nameof(ModifierFunctions.setPitch) => 11,
            nameof(ModifierFunctions.playDefaultSound) => 12,
            "setRuntimeVariable" => 13,
            nameof(ModifierFunctions.setTimelineLength) => 14,
            nameof(ModifierFunctions.playerEnableDamageAll) => 15,
            nameof(ModifierFunctions.showTitleCard) => 16,
            "setPitchProximity" => 17, // this is not homing.
            _ => -1,
        };

        public static string GetLevelTriggerName(int type) => type switch
        {
            0 => "timeInRange",
            1 => nameof(ModifierFunctions.onPlayerHit),
            2 => nameof(ModifierFunctions.onPlayerDeath),
            3 => nameof(ModifierFunctions.onLevelStart),
            4 => nameof(ModifierFunctions.onLevelRestart),
            5 => nameof(ModifierFunctions.onLevelRewind),
            _ => string.Empty,
        };

        public static string GetLevelActionName(int type) => type switch
        {
            0 => "vnInk",
            1 => "vnTimeline",
            2 => "playerDialogue",
            3 => nameof(ModifierFunctions.playerMoveAll),
            4 => nameof(ModifierFunctions.playerLockBoostAll),
            5 => nameof(ModifierFunctions.playerLockXAll),
            6 => nameof(ModifierFunctions.playerLockYAll),
            7 => "bgSpin",
            8 => "bgMove",
            9 => nameof(ModifierFunctions.playerBoostAll),
            10 => nameof(ModifierFunctions.setMusicTime),
            11 => nameof(ModifierFunctions.setPitch),
            12 => nameof(ModifierFunctions.playDefaultSound),
            13 => "setRuntimeVariable",
            14 => nameof(ModifierFunctions.setTimelineLength),
            15 => nameof(ModifierFunctions.playerEnableDamageAll),
            16 => nameof(ModifierFunctions.showTitleCard),
            17 => "setPitchProximity", // this is not homing.
            _ => string.Empty,
        };

        #endregion

        #region Internal Functions

        public static void SetVariables(Dictionary<string, string> variables, Dictionary<string, float> numberVariables)
        {
            if (variables == null)
                return;

            foreach (var variable in variables)
            {
                if (float.TryParse(variable.Value, out float num))
                    numberVariables[variable.Key] = num;
            }
        }

        public static AudioSource PlaySound(string id, AudioClip clip, float pitch, float volume, bool loop, float panStereo = 0f)
        {
            var audioSource = SoundManager.inst.PlaySound(clip, volume, pitch, loop, panStereo);
            // TODO: implement some way of cleaning up looping sounds
            //if (loop)
            //    ModifiersManager.audioSources.TryAdd(id, audioSource);
            return audioSource;
        }

        public static IEnumerator LoadMusicFileRaw(string path, Action<AudioClip> callback)
        {
            if (!RTFile.FileExists(path))
            {
                CoreHelper.Log($"Could not load Music file [{path}]");
                yield break;
            }

            var www = new WWW("file://" + path);
            while (!www.isDone)
                yield return null;

            var beatmapAudio = www.GetAudioClip(false, false);
            while (beatmapAudio.loadState != AudioDataLoadState.Loaded)
                yield return null;
            callback?.Invoke(beatmapAudio);
            beatmapAudio = null;
            www = null;

            yield break;
        }

        public static void SignalModifier(BeatmapObject beatmapObject, float delay)
        {
            if (delay == 0f)
            {
                if (beatmapObject.modifiers.TryFind(x => x.Name == ModifierFunctions.requireSignal.Name && x.type == Modifier.Type.Trigger, out Modifier modifier))
                    modifier.Result = "death hd";
                return;
            }
            CoroutineHelper.StartCoroutine(ISignalModifier(beatmapObject, delay));
        }

        static IEnumerator ISignalModifier(BeatmapObject beatmapObject, float delay)
        {
            if (delay != 0.0)
                yield return CoroutineHelper.Seconds(delay);

            if (beatmapObject.modifiers.TryFind(x => x.Name == ModifierFunctions.requireSignal.Name && x.type == Modifier.Type.Trigger, out Modifier modifier))
                modifier.Result = "death hd";
        }

        public static void ApplyAnimationTo(
            BeatmapObject applyTo, BeatmapObject takeFrom,
            bool useVisual, float time, float currentTime,
            bool animatePos, bool animateSca, bool animateRot,
            float delayPos, float delaySca, float delayRot)
        {
            if (!useVisual && takeFrom.cachedSequences)
            {
                // Animate position
                if (animatePos)
                    applyTo.positionOffset = takeFrom.cachedSequences.PositionSequence.GetValue(currentTime - time - delayPos);

                // Animate scale
                if (animateSca)
                {
                    var scaleSequence = takeFrom.cachedSequences.ScaleSequence.GetValue(currentTime - time - delaySca);
                    applyTo.scaleOffset = new Vector3(scaleSequence.x - 1f, scaleSequence.y - 1f, 0f);
                }

                // Animate rotation
                if (animateRot)
                    applyTo.rotationOffset = takeFrom.cachedSequences.RotationSequence.GetValue(currentTime - time - delayRot);
            }
            else if (useVisual && takeFrom.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.gameObject)
            {
                var transform = runtimeObject.visualObject.gameObject.transform.parent;

                // Animate position
                if (animatePos)
                    applyTo.positionOffset = transform.position;

                // Animate scale
                if (animateSca)
                    applyTo.scaleOffset = transform.lossyScale;

                // Animate rotation
                if (animateRot)
                    applyTo.rotationOffset = transform.rotation.eulerAngles;
            }
            else if (useVisual)
            {
                // Animate position
                if (animatePos)
                    applyTo.positionOffset = takeFrom.InterpolateChainPosition(currentTime - time - delayPos);

                // Animate scale
                if (animateSca)
                {
                    var scaleSequence = takeFrom.InterpolateChainScale(currentTime - time - delaySca);
                    applyTo.scaleOffset = new Vector3(scaleSequence.x - 1f, scaleSequence.y - 1f, 0f);
                }

                // Animate rotation
                if (animateRot)
                    applyTo.rotationOffset = takeFrom.InterpolateChainRotation(currentTime - time - delayRot);
            }
        }

        public static float GetAnimation(BeatmapObject reference, int fromType, int fromAxis, float t, AxisSource axisSource, int version = 1)
        {
            switch (axisSource)
            {
                case AxisSource.Sequence: {
                        if (!reference.cachedSequences)
                            break;
                        if (version == 0 && fromType == 2)
                            fromAxis = 2;
                        return fromType switch
                        {
                            0 => (reference.disablePositionSequence ? 0f : reference.cachedSequences.PositionSequence.GetValue(t).At(fromAxis)) + reference.fullTransform.position.At(fromAxis),
                            1 => (reference.disableScaleSequence ? 1f : reference.cachedSequences.ScaleSequence.GetValue(t).At(fromAxis)) * reference.fullTransform.scale.At(fromAxis),
                            2 => (reference.disableRotationSequence ? 0f : reference.cachedSequences.RotationSequence.GetValue(t).At(fromAxis)) + reference.fullTransform.rotation.At(fromAxis),
                            _ => 0f,
                        };
                    }
                case AxisSource.Visual: {
                        if (reference.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.gameObject)
                            return runtimeObject.visualObject.gameObject.transform.transform.parent.GetVector(fromType).At(fromAxis);
                        break;
                    }
                case AxisSource.Offset: {
                        if (version == 0 && fromType == 2)
                            fromAxis = 2;
                        return fromType switch
                        {
                            0 => reference.positionOffset.At(fromAxis),
                            1 => reference.scaleOffset.At(fromAxis),
                            2 => reference.rotationOffset.At(fromAxis),
                            _ => 0f,
                        };
                    }
                case AxisSource.SequenceOffset: {
                        if (version == 0 && fromType == 2)
                            fromAxis = 2;
                        if (!reference.cachedSequences)
                            return reference.GetTransformOffset(fromType).At(fromAxis);
                        return fromType switch
                        {
                            0 => (reference.disablePositionSequence ? 0f : reference.cachedSequences.PositionSequence.GetValue(t).At(fromAxis)) + reference.fullTransform.position.At(fromAxis) + reference.PositionOffset.At(fromAxis),
                            1 => ((reference.disableScaleSequence ? 1f : reference.cachedSequences.ScaleSequence.GetValue(t).At(fromAxis)) * reference.fullTransform.scale.At(fromAxis)) + reference.ScaleOffset.At(fromAxis),
                            2 => (reference.disablePositionSequence ? 0f : reference.cachedSequences.RotationSequence.GetValue(t).At(fromAxis)) + reference.fullTransform.rotation.At(fromAxis) + reference.RotationOffset.At(fromAxis),
                            _ => 0f,
                        };
                    }
            }

            return 0f;
        }

        public static float GetAnimation(BeatmapObject reference, int fromType, int fromAxis, float min, float max, float offset, float multiply, float t, float loop, AxisSource axisSource, int version)
        {
            switch (axisSource)
            {
                case AxisSource.Sequence: {
                        if (!reference.cachedSequences)
                            break;
                        if (version == 0 && fromType == 2)
                            fromAxis = 2;
                        return fromType switch
                        {
                            0 => Mathf.Clamp(((reference.disablePositionSequence ? 0f : reference.cachedSequences.PositionSequence.GetValue(t).At(fromAxis)) + reference.fullTransform.position.At(fromAxis) - offset) * multiply % loop, min, max),
                            1 => Mathf.Clamp((((reference.disableScaleSequence ? 1f : reference.cachedSequences.ScaleSequence.GetValue(t).At(fromAxis)) * reference.fullTransform.scale.At(fromAxis)) - offset) * multiply % loop, min, max),
                            2 => Mathf.Clamp(((reference.disableRotationSequence ? 0f : reference.cachedSequences.RotationSequence.GetValue(t).At(fromAxis)) + reference.fullTransform.rotation.At(fromAxis) - offset) * multiply % loop, min, max),
                            _ => 0f,
                        };
                    }
                case AxisSource.Visual: {
                        if (reference.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.gameObject)
                            return Mathf.Clamp((runtimeObject.visualObject.gameObject.transform.parent.GetVector(fromType).At(fromAxis) - offset) * multiply % loop, min, max);
                        break;
                    }
                case AxisSource.Offset: {
                        if (version == 0 && fromType == 2)
                            fromAxis = 2;
                        return fromType switch
                        {
                            0 => Mathf.Clamp((reference.positionOffset.At(fromAxis) - offset) * multiply % loop, min, max),
                            1 => Mathf.Clamp((reference.scaleOffset.At(fromAxis) - offset) * multiply % loop, min, max),
                            2 => Mathf.Clamp((reference.rotationOffset.At(fromAxis) - offset) * multiply % loop, min, max),
                            _ => 0f,
                        };
                    }
                case AxisSource.SequenceOffset: {
                        if (version == 0 && fromType == 2)
                            fromAxis = 2;
                        if (!reference.cachedSequences)
                            return Mathf.Clamp((reference.GetTransformOffset(fromType).At(fromAxis) - offset) * multiply % loop, min, max);
                        return fromType switch
                        {
                            0 => Mathf.Clamp(((reference.disablePositionSequence ? 0f : reference.cachedSequences.PositionSequence.GetValue(t).At(fromAxis)) + reference.PositionOffset.At(fromAxis) + reference.fullTransform.position.At(fromAxis) - offset) * multiply % loop, min, max),
                            1 => Mathf.Clamp((((reference.disableScaleSequence ? 1f : reference.cachedSequences.ScaleSequence.GetValue(t).At(fromAxis)) * reference.fullTransform.scale.At(fromAxis)) + reference.ScaleOffset.At(fromAxis) - offset) * multiply % loop, min, max),
                            2 => Mathf.Clamp(((reference.disableRotationSequence ? 0f : reference.cachedSequences.RotationSequence.GetValue(t).At(fromAxis)) + reference.fullTransform.rotation.At(fromAxis) + reference.RotationOffset.At(fromAxis) - offset) * multiply % loop, min, max),
                            _ => 0f,
                        };
                    }
            }

            return 0f;
        }

        public static float GetTime(BeatmapObject reference)
        {
            if (reference.FromPrefab)
            {
                var prefabObject = reference.GetPrefabObject();
                if (prefabObject && prefabObject.runtimeObject)
                    return prefabObject.runtimeObject.CurrentTime;
            }
            return reference.GetParentRuntime().CurrentTime;
        }

        public static void CopyColor(RTBeatmapObject applyTo, RTBeatmapObject takeFrom, bool applyColor1, bool applyColor2)
        {
            var applyToSolidObject = applyTo.visualObject as SolidObject;
            var takeFromSolidObject = takeFrom.visualObject as SolidObject;

            if (applyTo.visualObject.isGradient && applyToSolidObject && takeFrom.visualObject.isGradient && takeFromSolidObject) // both are gradients
            {
                var colors = takeFromSolidObject.GetColors();
                applyToSolidObject.SetColor(colors.startColor, colors.endColor);
            }

            if (applyTo.visualObject.isGradient && applyToSolidObject && !takeFrom.visualObject.isGradient) // only main object is a gradient
            {
                var color = takeFrom.visualObject.GetPrimaryColor();
                var colors = applyToSolidObject.GetColors();
                applyToSolidObject.SetColor(applyColor1 ? color : colors.startColor, applyColor2 ? color : colors.endColor);
            }

            if (!applyTo.visualObject.isGradient && takeFrom.visualObject.isGradient && takeFromSolidObject) // only copying object is a gradient
            {
                var colors = takeFromSolidObject.GetColors();
                applyTo.visualObject.SetColor(applyColor1 ? colors.startColor : applyColor2 ? colors.endColor : takeFromSolidObject.GetPrimaryColor());
            }

            if (!applyTo.visualObject.isGradient && !takeFrom.visualObject.isGradient) // neither are gradients
                applyTo.visualObject.SetColor(takeFrom.visualObject.GetPrimaryColor());
        }

        public static bool GetLevelRank(Level level, out int levelRankIndex)
        {
            var active = level && level.saveData;
            levelRankIndex = active ? LevelManager.GetLevelRank(level) : 0;
            return active;
        }

        public static string GetSaveFile(string file) => RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile", file + FileFormat.SES.Dot());

        public static void SetParent(IParentable child, BeatmapObject parent) => SetParent(child, parent.id);

        public static void SetParent(IParentable child, string parent)
        {
            // don't update parent if the parent is already the same
            if (child.Parent == parent)
                return;

            child.CustomParent = parent;
            child.UpdateParentChain();

            if (ObjectEditor.inst && ObjectEditor.inst.Dialog && ObjectEditor.inst.Dialog.IsCurrent && EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                ObjectEditor.inst.RenderParent(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
            if (RTPrefabEditor.inst && RTPrefabEditor.inst.PrefabObjectEditor && RTPrefabEditor.inst.PrefabObjectEditor.IsCurrent && EditorTimeline.inst.CurrentSelection.isPrefabObject)
                RTPrefabEditor.inst.RenderPrefabObjectParent(EditorTimeline.inst.CurrentSelection.GetData<PrefabObject>());
        }

        public static void SetObjectActive(IPrefabable prefabable, bool active)
        {
            if (prefabable != null && prefabable.GetRuntimeObject() is ICustomActivatable customActivatable)
                customActivatable.SetCustomActive(active);
        }

        public static ObjectTransform.Struct GetClonedTransform(int index, Vector3 pos, Vector2 sca, float rot)
        {
            var calcPos = index * pos;
            var calcSca = Vector2.one + index * sca;
            var calcRot = index * rot;

            return new ObjectTransform.Struct(calcPos, calcSca, calcRot);
        }

        #endregion
    }
}