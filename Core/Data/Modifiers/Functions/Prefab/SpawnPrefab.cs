using System.Collections.Generic;

using UnityEngine;

using LSFunctions;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SpawnPrefab : ModifierActionBase
    {
        #region Constructors

        public SpawnPrefab(bool offset, bool isGroup, bool isMulti, bool isCopy)
        {
            this.offset = offset;
            this.isGroup = isGroup;
            this.isMulti = isMulti;
            this.isCopy = isCopy;
            Name = "spawn";
            if (isMulti)
                Name += "Multi";
            Name += "Prefab";
            if (isCopy)
                Name += "Copy";
            if (offset)
                Name += "Offset";
            if (isGroup)
                Name += "Other";
            IsGroup = isGroup || isCopy;

            // TODO: figure out a better way to do value mapping
            indexMap = Name switch
            {
                "spawnPrefab" => new IndexMap
                {
                    prefabReference = 0,
                    posX = 1,
                    posY = 2,
                    scaX = 3,
                    scaY = 4,
                    rot = 5,
                    repeatCount = 6,
                    repeatOffsetTime = 7,
                    speed = 8,
                    dontDespawnOnInactive = 9,
                    time = 10,
                    timeRelative = 11,
                    searchPrefabUsing = 12,
                    removeAfterDespawn = 13,
                },
                "spawnPrefabOffset" => new IndexMap
                {
                    prefabReference = 0,
                    posX = 1,
                    posY = 2,
                    scaX = 3,
                    scaY = 4,
                    rot = 5,
                    repeatCount = 6,
                    repeatOffsetTime = 7,
                    speed = 8,
                    dontDespawnOnInactive = 9,
                    time = 10,
                    timeRelative = 11,
                    searchPrefabUsing = 12,
                    removeAfterDespawn = 13,
                },
                "spawnPrefabOffsetOther" => new IndexMap
                {
                    prefabReference = 0,
                    posX = 1,
                    posY = 2,
                    scaX = 3,
                    scaY = 4,
                    rot = 5,
                    repeatCount = 6,
                    repeatOffsetTime = 7,
                    speed = 8,
                    dontDespawnOnInactive = 9,
                    group = 10,
                    time = 11,
                    timeRelative = 12,
                    searchPrefabUsing = 13,
                    removeAfterDespawn = 14,
                },
                "spawnPrefabCopy" => new IndexMap
                {
                    prefabReference = 0,
                    group = 1,
                    time = 2,
                    timeRelative = 3,
                    searchPrefabUsing = 4,
                    dontDespawnOnInactive = 5,
                    removeAfterDespawn = 6,
                },
                "spawnMultiPrefab" => new IndexMap
                {
                    prefabReference = 0,
                    posX = 1,
                    posY = 2,
                    scaX = 3,
                    scaY = 4,
                    rot = 5,
                    repeatCount = 6,
                    repeatOffsetTime = 7,
                    speed = 8,
                    time = 9,
                    timeRelative = 10,
                    searchPrefabUsing = 11,
                    removeAfterDespawn = 12,
                },
                "spawnMultiPrefabOffset" => new IndexMap
                {
                    prefabReference = 0,
                    posX = 1,
                    posY = 2,
                    scaX = 3,
                    scaY = 4,
                    rot = 5,
                    repeatCount = 6,
                    repeatOffsetTime = 7,
                    speed = 8,
                    time = 9,
                    timeRelative = 10,
                    searchPrefabUsing = 11,
                    removeAfterDespawn = 12,
                },
                "spawnMultiPrefabOffsetOther" => new IndexMap
                {
                    prefabReference = 0,
                    posX = 1,
                    posY = 2,
                    scaX = 3,
                    scaY = 4,
                    rot = 5,
                    repeatCount = 6,
                    repeatOffsetTime = 7,
                    speed = 8,
                    group = 9,
                    time = 10,
                    timeRelative = 11,
                    searchPrefabUsing = 12,
                    removeAfterDespawn = 13,
                },
                "spawnMultiPrefabCopy" => new IndexMap
                {
                    prefabReference = 0,
                    group = 1,
                    time = 2,
                    timeRelative = 3,
                    searchPrefabUsing = 4,
                    removeAfterDespawn = 5,
                },
                _ => default,
            };

            if (!isCopy)
            {
                SetupModifier("0", "0", "0", "1", "1", "0", "0", "0", "1", "0", "True", "0", "False");
                if (isGroup)
                    Modifier.values.Insert(9, "Object Group");
                if (!isMulti)
                    Modifier.values.Insert(9, "False");
            }
            else
            {
                SetupModifier("0", "Prefab Group", "0", "True", "0", "False");
                if (!isMulti)
                    Modifier.values.Insert(5, "False");
            }
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Prefab;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        readonly bool offset;

        readonly bool isGroup;

        readonly bool isMulti;

        readonly bool isCopy;

        IndexMap indexMap;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifier.constant || !isMulti && modifier.HasResult())
                return;

            var prefab = GameData.Current.GetPrefab(modifier.GetInt(indexMap.searchPrefabUsing, 0, modifierLoop.variables), modifier.GetValue(indexMap.prefabReference, modifierLoop.variables));
            if (!prefab)
                return;

            var prefabObject = new PrefabObject();
            prefabObject.id = LSText.randomString(16);
            prefabObject.prefabID = prefab.id;

            bool remove = false;

            if (isCopy)
            {
                if (modifierLoop.reference is not IPrefabable prefabable || !GameData.Current.TryFindPrefabObjectWithTag(modifier, prefabable, modifier.GetValue(indexMap.group), out PrefabObject orig))
                    return;

                prefabObject.StartTime = modifier.GetBool(indexMap.timeRelative, true, modifierLoop.variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(indexMap.time, 0f, modifierLoop.variables) : modifier.GetFloat(indexMap.time, 0f, modifierLoop.variables);

                prefabObject.PasteInstanceData(orig);
                remove = modifier.GetBool(indexMap.removeAfterDespawn, false, modifierLoop.variables);
            }
            else
            {
                if (isMulti)
                {
                    if (!modifier.HasResult())
                        modifier.Result = new List<PrefabObject>();

                    var list = modifier.GetResult<List<PrefabObject>>();
                    list.Add(prefabObject);
                    modifier.Result = list;
                }

                var pos = new Vector3(modifier.GetFloat(indexMap.posX, 0f, modifierLoop.variables), modifier.GetFloat(indexMap.posY, 0f, modifierLoop.variables));
                var sca = new Vector2(modifier.GetFloat(indexMap.scaX, 0f, modifierLoop.variables), modifier.GetFloat(indexMap.scaY, 0f, modifierLoop.variables));
                var rot = modifier.GetFloat(indexMap.rot, 0f, modifierLoop.variables);
                var repeatCount = modifier.GetInt(indexMap.repeatCount, 0, modifierLoop.variables);
                var repeatOffsetTime = modifier.GetFloat(indexMap.repeatOffsetTime, 0f, modifierLoop.variables);
                var speed = modifier.GetFloat(indexMap.speed, 0f, modifierLoop.variables);
                var time = modifier.GetFloat(indexMap.time, 0f, modifierLoop.variables);
                var offsetAudio = modifier.GetBool(indexMap.timeRelative, true, modifierLoop.variables);
                remove = modifier.GetBool(indexMap.removeAfterDespawn, false, modifierLoop.variables);

                prefabObject.StartTime = offsetAudio ? AudioManager.inst.CurrentAudioSource.time + time : time;

                if (offset)
                {
                    var transformable = isGroup ? GameData.Current.FindTransformableWithTag(modifier, modifierLoop.reference.AsPrefabable(), modifier.GetValue(indexMap.group, modifierLoop.variables)) : modifierLoop.reference.AsTransformable();
                    if (transformable != null)
                    {
                        var animationResult = transformable.GetObjectTransform();
                        pos += animationResult.position;
                        sca *= animationResult.scale;
                        rot += animationResult.rotation;
                    }
                }

                if (prefab.defaultInstanceData)
                    prefabObject.PasteInstanceData(prefab.defaultInstanceData);

                prefabObject.events[0].values[0] = pos.x;
                prefabObject.events[0].values[1] = pos.y;
                prefabObject.events[1].values[0] = sca.x;
                prefabObject.events[1].values[1] = sca.y;
                prefabObject.events[2].values[0] = rot;

                prefabObject.RepeatCount = repeatCount;
                prefabObject.RepeatOffsetTime = repeatOffsetTime;
                prefabObject.Speed = speed;

                prefabObject.depth = 0f;
            }

            prefabObject.fromModifier = true;

            modifier.Result = prefabObject;
            GameData.Current.prefabObjects.Add(prefabObject);
            RTLevel.Current.postTick.Enqueue(() =>
            {
                RTLevelBase runtimeLevel = modifierLoop.reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : modifierLoop.reference.GetParentRuntime();
                runtimeLevel?.UpdatePrefab(prefabObject);

                var runtimePrefabObject = prefabObject.runtimeObject;
                if (runtimePrefabObject && remove)
                    runtimePrefabObject.onActiveChanged = enabled =>
                    {
                        if (enabled)
                            return;

                        RTLevel.Current.postTick.Enqueue(() =>
                        {
                            RTLevelBase runtimeLevel = modifierLoop.reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : modifierLoop.reference.GetParentRuntime();
                            runtimeLevel?.UpdatePrefab(prefabObject, false);

                            GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);

                            modifier.Result = null;
                        });
                    };
            });
        }

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (isMulti || modifier.Result is not PrefabObject prefabObject || modifier.GetBool(indexMap.dontDespawnOnInactive, false, modifierLoop.variables))
                return;

            RTLevelBase runtimeLevel = modifierLoop.reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : modifierLoop.reference.GetParentRuntime();
            runtimeLevel?.UpdatePrefab(prefabObject, false);

            GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);

            modifier.Result = default;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (isCopy)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);

                modifierCard.DropdownGenerator(modifier, reference, "Search Prefab Using", indexMap.searchPrefabUsing, CoreHelper.StringToOptionData("Index", "Name", "ID"));
                modifierCard.StringGenerator(modifier, reference, "Prefab Reference", indexMap.prefabReference);

                modifierCard.GroupFieldGenerator(modifier, reference, "Prefab Object Group", indexMap.group);

                modifierCard.SingleGenerator(modifier, reference, "Time", indexMap.time, 0f);
                modifierCard.BoolGenerator(modifier, reference, "Time Relative", indexMap.timeRelative, true);

                if (!isMulti)
                    modifierCard.BoolGenerator(modifier, reference, "Don't Despawn On Inactive", indexMap.dontDespawnOnInactive, false);
                modifierCard.BoolGenerator(modifier, reference, "Remove After Despawn", indexMap.removeAfterDespawn);
                return;
            }

            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", indexMap.group);
            }

            modifierCard.DropdownGenerator(modifier, reference, "Search Prefab Using", indexMap.searchPrefabUsing, CoreHelper.StringToOptionData("Index", "Name", "ID"));
            modifierCard.StringGenerator(modifier, reference, "Prefab Reference", indexMap.prefabReference);

            modifierCard.SingleGenerator(modifier, reference, "Position X", indexMap.posX, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Position Y", indexMap.posY, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Scale X", indexMap.scaX, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Scale Y", indexMap.scaY, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Rotation", indexMap.rot, 0f, 15f, 3f);

            modifierCard.IntegerGenerator(modifier, reference, "Repeat Count", indexMap.repeatCount, 0);
            modifierCard.SingleGenerator(modifier, reference, "Repeat Offset Time", indexMap.repeatOffsetTime, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Speed", indexMap.speed, 1f);

            modifierCard.SingleGenerator(modifier, reference, "Time", indexMap.time, 0f);
            modifierCard.BoolGenerator(modifier, reference, "Time Relative", indexMap.timeRelative, true);

            if (!isMulti)
                modifierCard.BoolGenerator(modifier, reference, "Don't Despawn On Inactive", indexMap.dontDespawnOnInactive, false);
            modifierCard.BoolGenerator(modifier, reference, "Remove After Despawn", indexMap.removeAfterDespawn);
        }

        #endregion

        #region Sub Classes

        public struct IndexMap
        {
            public int group;
            public int searchPrefabUsing;
            public int prefabReference;
            public int posX;
            public int posY;
            public int scaX;
            public int scaY;
            public int rot;
            public int repeatCount;
            public int repeatOffsetTime;
            public int speed;
            public int time;
            public int timeRelative;
            public int dontDespawnOnInactive;
            public int removeAfterDespawn;
        }

        #endregion
    }
}
