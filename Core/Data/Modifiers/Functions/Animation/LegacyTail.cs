using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class LegacyTail : ModifierActionBase
    {
        #region Constructors

        public LegacyTail()
        {
            SetupModifier("200");
            IsGroup = true;
        }

        #endregion

        #region Values

        public override string Name => "legacyTail";

        public override ModifierCategoryType Category => ModifierCategoryType.Animation;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject || modifier.values.IsEmpty() || !GameData.Current)
                return;

            var totalTime = modifier.GetFloat(0, 200f, modifierLoop.variables);

            var list = modifier.Result is List<Cache> ? (List<Cache>)modifier.Result : new List<Cache>();

            if (!modifier.HasResult())
            {
                list.Add(new Cache(beatmapObject, Vector3.zero, Vector3.zero, Quaternion.identity, 0f, 0f));

                for (int i = 1; i < modifier.values.Count; i += 3)
                {
                    var group = GameData.Current.FindObjectsWithTag(modifier, beatmapObject, modifier.GetValue(i, modifierLoop.variables));

                    if (modifier.values.Count <= i + 2 || group.Count < 1)
                        break;

                    var distance = modifier.GetFloat(i + 1, 2f, modifierLoop.variables);
                    var time = modifier.GetFloat(i + 2, 12f, modifierLoop.variables);

                    for (int j = 0; j < group.Count; j++)
                    {
                        var tail = group[j];
                        list.Add(new Cache(tail, tail.positionOffset, tail.positionOffset, Quaternion.Euler(tail.rotationOffset), distance, time));
                    }
                }

                modifier.Result = list;
            }

            var animationResult = beatmapObject.InterpolateChain();
            list[0].pos = animationResult.position;
            list[0].rot = Quaternion.Euler(animationResult.rotation);

            float num = Time.deltaTime * totalTime;

            for (int i = 1; i < list.Count; i++)
            {
                var tracker = list[i];
                var prevTracker = list[i - 1];
                if (Vector3.Distance(tracker.pos, prevTracker.pos) > tracker.distance)
                {
                    var vector = Vector3.Lerp(tracker.pos, prevTracker.pos, Time.deltaTime * tracker.time);
                    var quaternion = Quaternion.Lerp(tracker.rot, prevTracker.rot, Time.deltaTime * tracker.time);
                    list[i].pos = vector;
                    list[i].rot = quaternion;
                }

                num *= Vector3.Distance(prevTracker.lastPos, tracker.pos);
                tracker.beatmapObject.positionOffset = Vector3.MoveTowards(prevTracker.lastPos, tracker.pos, num);
                prevTracker.lastPos = tracker.pos;
                tracker.beatmapObject.rotationOffset = tracker.rot.eulerAngles;
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.SingleGenerator(modifier, reference, "Total Time", 0, 200f);

            var path = ModifiersEditor.inst.stringInput.Duplicate(modifierCard.layout, "usage");
            path.transform.localScale = Vector3.one;
            var labelText = path.transform.Find("Text").GetComponent<Text>();
            labelText.text = "Update Object to Update Modifier";
            path.transform.Find("Text").AsRT().sizeDelta = new Vector2(350f, 32f);
            CoreHelper.Destroy(path.transform.Find("Input").gameObject);

            int a = 0;
            for (int i = 1; i < modifier.values.Count; i += 3)
            {
                int groupIndex = i;
                var label = modifierCard.LabelGenerator($"- Tail Group {a + 1}");

                modifierCard.DeleteGenerator(modifier, reference, label.transform, () =>
                {
                    for (int j = 0; j < 3; j++)
                        modifier.values.RemoveAt(groupIndex);
                });

                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", i);
                modifierCard.SingleGenerator(modifier, reference, "Distance", i + 1, 2f);
                modifierCard.SingleGenerator(modifier, reference, "Time", i + 2, 12f);
                a++;
            }

            modifierCard.AddGenerator(modifier, reference, "Add Group", () =>
            {
                var lastIndex = modifier.values.Count - 1;
                var length = "2";
                var time = "12";
                if (lastIndex - 1 > 2)
                {
                    length = modifier.values[lastIndex - 1];
                    time = modifier.values[lastIndex];
                }

                modifier.values.Add("Object Group");
                modifier.values.Add(length);
                modifier.values.Add(time);
            });
        }

        #endregion

        #region Sub Classes

        public class Cache
        {
            public Cache(BeatmapObject beatmapObject, Vector3 pos, Vector3 lastPos, Quaternion rot, float distance, float time)
            {
                this.beatmapObject = beatmapObject;
                this.pos = pos;
                this.lastPos = lastPos;
                this.rot = rot;
                this.distance = distance;
                this.time = time;
            }

            public float distance;
            public float time;

            public Vector3 lastPos;
            public Vector3 pos;

            public Quaternion rot;

            public BeatmapObject beatmapObject;
        }

        #endregion
    }
}
