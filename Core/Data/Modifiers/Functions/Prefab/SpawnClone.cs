using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SpawnClone : ModifierActionBase
    {
        #region Constructors

        public SpawnClone(bool isMath)
        {
            this.isMath = isMath;
            Name = "spawnClone";
            if (isMath)
                Name += "Math";
            Modifier = isMath ?
                CreateModifier(Name, "0", "2", "1", "(cloneIndex * 5)", "(cloneIndex * 0)", "(cloneIndex * 0)", "(cloneIndex * 0)", "(cloneIndex * 0)", "(cloneIndex * 0)", "currentTimeOffset + objectStartTime + timeOffset", "0", "True", "True", "False") :
                CreateModifier(Name, "0", "2", "1", "5", "0", "0", "0", "0", "0", "0", "0", "True", "True", "False");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Prefab;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        readonly bool isMath;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return;

            var startIndex = modifier.GetInt(0, 0, modifierLoop.variables);
            var endCount = modifier.GetInt(1, 0, modifierLoop.variables);
            var increment = modifier.GetInt(2, 1, modifierLoop.variables);

            var distance = -(startIndex - endCount);
            var allowed = increment != 0 && endCount > startIndex && (distance < 0 ? increment < 0 : increment > 0);

            var posX = modifier.GetValue(3, modifierLoop.variables);
            var posY = modifier.GetValue(4, modifierLoop.variables);
            var posZ = modifier.GetValue(5, modifierLoop.variables);
            var scaX = modifier.GetValue(6, modifierLoop.variables);
            var scaY = modifier.GetValue(7, modifierLoop.variables);
            var rot = modifier.GetValue(8, modifierLoop.variables);
            var timeOffset = modifier.GetValue(9, modifierLoop.variables);

            var disabled = modifier.GetValue(10, modifierLoop.variables);
            var offsetPrefab = modifier.GetBool(11, true, modifierLoop.variables);
            var copyOffsets = modifier.GetBool(12, true, modifierLoop.variables);
            var disableSelf = modifier.GetBool(13, false, modifierLoop.variables);

            var basePos = Vector3.zero;
            var baseSca = Vector2.one;
            var baseRot = 0f;
            var baseTime = 0f;

            if (disableSelf)
                beatmapObject.runtimeObject?.SetCustomActive(false);

            if (modifier.TryGetResult(out Cache cache))
            {
                if (cache.startIndex == startIndex && cache.endCount == endCount && cache.increment == increment && cache.disabled == disabled && allowed)
                {
                    var index = 0;
                    for (int i = startIndex; i < endCount; i += increment)
                    {
                        ObjectTransform.Struct transform = default;
                        float calcTime;
                        if (isMath)
                        {
                            var numberVariables = new Dictionary<string, float>()
                            {
                                { "currentPosX", basePos.x },
                                { "currentPosY", basePos.y },
                                { "currentPosZ", basePos.z },
                                { "currentScaX", baseSca.x },
                                { "currentScaY", baseSca.y },
                                { "currentRot", baseRot },
                                { "currentTimeOffset", baseTime },
                                { "cloneIndex", i },
                            };
                            beatmapObject.SetObjectVariables(numberVariables);
                            ModifiersHelper.SetVariables(modifierLoop.variables, numberVariables);

                            transform.position = new Vector3(
                                RTMath.Parse(posX, RTLevel.Current?.evaluationContext, numberVariables),
                                RTMath.Parse(posY, RTLevel.Current?.evaluationContext, numberVariables),
                                RTMath.Parse(posZ, RTLevel.Current?.evaluationContext, numberVariables));
                            transform.scale = new Vector2(
                                RTMath.Parse(scaX, RTLevel.Current?.evaluationContext, numberVariables),
                                RTMath.Parse(scaY, RTLevel.Current?.evaluationContext, numberVariables));
                            transform.rotation = RTMath.Parse(rot, RTLevel.Current?.evaluationContext, numberVariables);
                            calcTime = RTMath.Parse(timeOffset, RTLevel.Current?.evaluationContext, numberVariables);
                        }
                        else
                        {
                            var pos = new Vector3(Parser.TryParse(posX, 0f), Parser.TryParse(posY, 0f), Parser.TryParse(posZ, 0f));
                            var sca = new Vector2(Parser.TryParse(scaX, 0f), Parser.TryParse(scaY, 0f));
                            transform = ModifiersHelper.GetClonedTransform(i, pos, sca, Parser.TryParse(rot, 0f));
                            calcTime = 0f;
                        }

                        var prefabObject = cache.spawned.GetAtOrDefault(index, null);
                        if (!prefabObject)
                        {
                            basePos = transform.position;
                            baseSca = transform.scale;
                            baseRot = transform.rotation;
                            if (isMath)
                                baseTime = calcTime;
                            index++;
                            continue;
                        }

                        var copy = cache.copies.GetAtOrDefault(index, null);

                        if (offsetPrefab)
                        {
                            prefabObject.events[0].values[0] = transform.position.x;
                            prefabObject.events[0].values[1] = transform.position.y;
                            prefabObject.depth = transform.position.z;
                            prefabObject.events[1].values[0] = transform.scale.x;
                            prefabObject.events[1].values[1] = transform.scale.y;
                            prefabObject.events[2].values[0] = transform.rotation;
                        }
                        else if (copy)
                        {
                            copy.fullTransform.position = transform.position;
                            copy.fullTransform.scale = new Vector3(transform.scale.x, transform.scale.y, 1f);
                            copy.fullTransform.rotation = new Vector3(0f, 0f, transform.rotation);
                        }

                        if (copy && copyOffsets)
                        {
                            copy.PositionOffset = beatmapObject.PositionOffset;
                            copy.ScaleOffset = beatmapObject.ScaleOffset;
                            copy.RotationOffset = beatmapObject.RotationOffset;
                        }

                        basePos = transform.position;
                        baseSca = transform.scale;
                        baseRot = transform.rotation;
                        if (isMath)
                            baseTime = calcTime;
                        index++;
                    }

                    if (offsetPrefab)
                        RTLevel.Current.postTick.Enqueue(() =>
                        {
                            for (int i = 0; i < cache.spawned.Count; i++)
                            {
                                var prefabObject = cache.spawned[i];
                                if (prefabObject)
                                    prefabObject.GetParentRuntime()?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                            }
                        });
                    return;
                }

                // if this code is reached it means the start index, end count, incremement or disabled values changed.
                modifier.OnRemoveCache();
                modifier.Result = default;
            }

            if (!allowed)
                return;

            var disabledArray = !string.IsNullOrEmpty(disabled) ? disabled.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries) : null;

            var spawned = new List<PrefabObject>();
            var copies = new List<BeatmapObject>();

            var children = beatmapObject.GetChildTree();
            var prefab = new Prefab("clone", 0, Mathf.Min(children.Min(x => x.StartTime) - beatmapObject.StartTime, 0f), children, null);

            // ensure the same modifier does not recursively duplicate.
            for (int i = 0; i < prefab.beatmapObjects.Count; i++)
            {
                var child = prefab.beatmapObjects[i];
                for (int j = 0; j < child.modifiers.Count; j++)
                {
                    var childModifier = child.modifiers[j];
                    if (childModifier.id == modifier.id)
                        childModifier.enabled = false;
                }
            }

            for (int i = startIndex; i < endCount; i += increment)
            {
                ObjectTransform.Struct transform = default;
                float calcTime;
                if (isMath)
                {
                    var numberVariables = new Dictionary<string, float>()
                    {
                        { "currentPosX", basePos.x },
                        { "currentPosY", basePos.y },
                        { "currentPosZ", basePos.z },
                        { "currentScaX", baseSca.x },
                        { "currentScaY", baseSca.y },
                        { "currentRot", baseRot },
                        { "currentTimeOffset", baseTime },
                        { "cloneIndex", i },
                    };
                    beatmapObject.SetObjectVariables(numberVariables);
                    ModifiersHelper.SetVariables(modifierLoop.variables, numberVariables);

                    transform.position = new Vector3(
                        RTMath.Parse(posX, RTLevel.Current?.evaluationContext, numberVariables),
                        RTMath.Parse(posY, RTLevel.Current?.evaluationContext, numberVariables),
                        RTMath.Parse(posZ, RTLevel.Current?.evaluationContext, numberVariables));
                    transform.scale = new Vector2(
                        RTMath.Parse(scaX, RTLevel.Current?.evaluationContext, numberVariables),
                        RTMath.Parse(scaY, RTLevel.Current?.evaluationContext, numberVariables));
                    transform.rotation = RTMath.Parse(rot, RTLevel.Current?.evaluationContext, numberVariables);
                    calcTime = RTMath.Parse(timeOffset, RTLevel.Current?.evaluationContext, numberVariables);
                }
                else
                {
                    var pos = new Vector3(Parser.TryParse(posX, 0f), Parser.TryParse(posY, 0f), Parser.TryParse(posZ, 0f));
                    var sca = new Vector2(Parser.TryParse(scaX, 0f), Parser.TryParse(scaY, 0f));
                    transform = ModifiersHelper.GetClonedTransform(i, pos, sca, Parser.TryParse(rot, 0f));
                    calcTime = baseTime + Parser.TryParse(timeOffset, 0f);
                }

                // enabled (string array based)
                if (disabledArray != null && disabledArray.Contains(i.ToString()))
                {
                    basePos = transform.position;
                    baseSca = transform.scale;
                    baseRot = transform.rotation;
                    if (!isMath)
                        baseTime = calcTime;
                    spawned.Add(null);
                    continue;
                }

                var prefabObject = new PrefabObject();
                prefabObject.prefabID = prefab.id;

                prefabObject.StartTime = beatmapObject.StartTime + calcTime;

                if (offsetPrefab)
                {
                    prefabObject.events[0].values[0] = transform.position.x;
                    prefabObject.events[0].values[1] = transform.position.y;
                    prefabObject.depth = transform.position.z;
                    prefabObject.events[1].values[0] = transform.scale.x;
                    prefabObject.events[1].values[1] = transform.scale.y;
                    prefabObject.events[2].values[0] = transform.rotation;
                }

                prefabObject.RepeatCount = 0;
                prefabObject.RepeatOffsetTime = 0f;
                prefabObject.Speed = 1f;

                prefabObject.fromModifier = true;

                spawned.Add(prefabObject);
                GameData.Current.prefabObjects.Add(prefabObject);
                prefabObject.CachedPrefab = prefab;

                basePos = transform.position;
                baseSca = transform.scale;
                baseRot = transform.rotation;
                baseTime = calcTime;
            }

            RTLevel.Current.postTick.Enqueue(() =>
            {
                RTLevelBase runtimeLevel = modifierLoop.reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : modifierLoop.reference.GetParentRuntime();
                if (offsetPrefab)
                {
                    for (int i = 0; i < spawned.Count; i++)
                    {
                        var prefabObject = spawned[i];
                        runtimeLevel?.UpdatePrefab(prefabObject);
                        if (prefabObject && prefabObject.runtimeObject && prefabObject.runtimeObject.Spawner && prefabObject.runtimeObject.Spawner.BeatmapObjects.TryFind(x => x.originalID == beatmapObject.id, out BeatmapObject copy))
                        {
                            copies.Add(copy);

                            if (copyOffsets)
                            {
                                copy.PositionOffset = beatmapObject.PositionOffset;
                                copy.ScaleOffset = beatmapObject.ScaleOffset;
                                copy.RotationOffset = beatmapObject.RotationOffset;
                            }
                        }
                        else
                            copies.Add(null);
                    }
                    return;
                }

                var basePos = Vector3.zero;
                var baseSca = Vector2.one;
                var baseRot = 0f;

                var index = 0;
                for (int i = startIndex; i < endCount; i += increment)
                {
                    ObjectTransform.Struct transform = default;
                    float calcTime;
                    if (isMath)
                    {
                        var numberVariables = new Dictionary<string, float>()
                        {
                            { "currentPosX", basePos.x },
                            { "currentPosY", basePos.y },
                            { "currentPosZ", basePos.z },
                            { "currentScaX", baseSca.x },
                            { "currentScaY", baseSca.y },
                            { "currentRot", baseRot },
                            { "currentTimeOffset", baseTime },
                            { "cloneIndex", i },
                        };
                        beatmapObject.SetObjectVariables(numberVariables);
                        ModifiersHelper.SetVariables(modifierLoop.variables, numberVariables);

                        transform.position = new Vector3(
                            RTMath.Parse(posX, RTLevel.Current?.evaluationContext, numberVariables),
                            RTMath.Parse(posY, RTLevel.Current?.evaluationContext, numberVariables),
                            RTMath.Parse(posZ, RTLevel.Current?.evaluationContext, numberVariables));
                        transform.scale = new Vector2(
                            RTMath.Parse(scaX, RTLevel.Current?.evaluationContext, numberVariables),
                            RTMath.Parse(scaY, RTLevel.Current?.evaluationContext, numberVariables));
                        transform.rotation = RTMath.Parse(rot, RTLevel.Current?.evaluationContext, numberVariables);
                        calcTime = RTMath.Parse(timeOffset, RTLevel.Current?.evaluationContext, numberVariables);
                    }
                    else
                    {
                        var pos = new Vector3(Parser.TryParse(posX, 0f), Parser.TryParse(posY, 0f), Parser.TryParse(posZ, 0f));
                        var sca = new Vector2(Parser.TryParse(scaX, 0f), Parser.TryParse(scaY, 0f));
                        transform = ModifiersHelper.GetClonedTransform(i, pos, sca, Parser.TryParse(rot, 0f));
                        calcTime = 0f;
                    }

                    var prefabObject = spawned[index];
                    if (!prefabObject)
                    {
                        basePos = transform.position;
                        baseSca = transform.scale;
                        baseRot = transform.rotation;
                        if (isMath)
                            baseTime = calcTime;
                        copies.Add(null);
                        index++;
                        continue;
                    }

                    runtimeLevel?.UpdatePrefab(prefabObject);
                    if (prefabObject.runtimeObject && prefabObject.runtimeObject.Spawner && prefabObject.runtimeObject.Spawner.BeatmapObjects.TryFind(x => x.originalID == beatmapObject.id, out BeatmapObject copy))
                    {
                        copy.fullTransform.position = transform.position;
                        copy.fullTransform.scale = new Vector3(transform.scale.x, transform.scale.y, 1f);
                        copy.fullTransform.rotation = new Vector3(0f, 0f, transform.rotation);
                        copies.Add(copy);

                        if (copyOffsets)
                        {
                            copy.PositionOffset = beatmapObject.PositionOffset;
                            copy.ScaleOffset = beatmapObject.ScaleOffset;
                            copy.RotationOffset = beatmapObject.RotationOffset;
                        }
                    }
                    else
                        copies.Add(null);

                    basePos = transform.position;
                    baseSca = transform.scale;
                    baseRot = transform.rotation;
                    if (isMath)
                        baseTime = calcTime;
                    index++;
                }
            });

            modifier.Result = new Cache
            {
                startIndex = startIndex,
                endCount = endCount,
                increment = increment,
                disabled = disabled,
                spawned = spawned,
                copies = copies,
            };
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.IntegerGenerator(modifier, reference, "Start Index", 0);
            modifierCard.IntegerGenerator(modifier, reference, "End Count", 1);
            modifierCard.IntegerGenerator(modifier, reference, "Increment", 2, 1);
            modifierCard.SingleGenerator(modifier, reference, "Pos X", 3);
            modifierCard.SingleGenerator(modifier, reference, "Pos Y", 4);
            modifierCard.SingleGenerator(modifier, reference, "Pos Z", 5);
            modifierCard.SingleGenerator(modifier, reference, "Sca X", 6);
            modifierCard.SingleGenerator(modifier, reference, "Sca Y", 7);
            modifierCard.SingleGenerator(modifier, reference, "Rot", 8, amount: 15f, multiply: 3f);
            modifierCard.SingleGenerator(modifier, reference, "Time Offset", 9);
            modifierCard.StringGenerator(modifier, reference, "Disabled Array", 10);
            modifierCard.BoolGenerator(modifier, reference, "Use Prefab Offsets", 11);
            modifierCard.BoolGenerator(modifier, reference, "Copy Offsets", 12);
            modifierCard.BoolGenerator(modifier, reference, "Disable Self", 13);
        }

        public override void OnRemoveCache(Modifier modifier)
        {
            if (modifier.enabled && modifier.TryGetResult(out Cache cache))
                RTLevel.Current.postTick.Enqueue(() =>
                {
                    for (int i = 0; i < cache.spawned.Count; i++)
                    {
                        var prefabObject = cache.spawned[i];
                        if (!prefabObject)
                            continue;

                        prefabObject.GetParentRuntime()?.RemovePrefab(prefabObject);
                        GameData.Current.prefabObjects.Remove(x => x.id == prefabObject.id);
                    }
                    cache.spawned.Clear();
                    RTLevel.Current.RecalculateObjectStates();
                });
        }

        #endregion

        #region Sub Classes

        public class Cache
        {
            public int startIndex;
            public int endCount;
            public int increment;
            public string disabled;
            /// <summary>
            /// Spawned prefabs containing copies of the object.
            /// </summary>
            public List<PrefabObject> spawned;
            /// <summary>
            /// Copies of the object.
            /// </summary>
            public List<BeatmapObject> copies;
        }

        #endregion
    }
}
