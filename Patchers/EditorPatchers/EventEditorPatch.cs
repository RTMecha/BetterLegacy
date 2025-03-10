using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Managers;
using HarmonyLib;
using LSFunctions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EventKeyframeSelection = EventEditor.KeyframeSelection;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(EventEditor))]
    public class EventEditorPatch : MonoBehaviour
    {
        static EventEditor Instance { get => EventEditor.inst; set => EventEditor.inst = value; }

        [HarmonyPatch(nameof(EventEditor.Awake))]
        [HarmonyPrefix]
        static bool AwakePrefix(EventEditor __instance)
        {
            // Sets the instance
            if (Instance == null)
                Instance = __instance;
            else if (Instance != __instance)
            {
                Destroy(__instance.gameObject);
                return false;
            }

            CoreHelper.LogInit(__instance.className);

            for (int i = 0; i < 9; i++)
            {
                __instance.previewTheme.objectColors.Add(LSColors.pink900);
            }

            var beatmapTheme = __instance.previewTheme;

            __instance.previewTheme = new BeatmapTheme
            {
                id = beatmapTheme.id,
                name = beatmapTheme.name,
                expanded = beatmapTheme.expanded,
                backgroundColor = beatmapTheme.backgroundColor,
                guiAccentColor = beatmapTheme.guiColor,
                guiColor = beatmapTheme.guiColor,
                playerColors = beatmapTheme.playerColors,
                objectColors = beatmapTheme.objectColors,
                backgroundColors = beatmapTheme.backgroundColors,
                effectColors = new List<Color>
                {
                    LSColors.pink500,
                    LSColors.pink500,
                    LSColors.pink500,
                    LSColors.pink500,
                    LSColors.pink500,
                    LSColors.pink500,
                    LSColors.pink500,
                    LSColors.pink500,
                    LSColors.pink500,
                    LSColors.pink500,
                    LSColors.pink500,
                    LSColors.pink500,
                    LSColors.pink500,
                    LSColors.pink500,
                    LSColors.pink500,
                    LSColors.pink500,
                    LSColors.pink500,
                    LSColors.pink500,
                },
            };

            return false;
        }

        [HarmonyPatch(nameof(EventEditor.Start))]
        [HarmonyPrefix]
        static bool StartPrefix()
        {
            if (ObjEditor.inst && ObjEditor.inst.timelineObjectPrefabLock)
            {
                var gameObject = EventEditor.inst.TimelinePrefab.Duplicate(EventEditor.inst.transform, EventEditor.inst.TimelinePrefab.name);

                var lockedUI = ObjEditor.inst.timelineObjectPrefabLock.Duplicate(gameObject.transform, "lock");
                lockedUI.transform.AsRT().anchoredPosition = new Vector2(6f, 0f);
                lockedUI.transform.AsRT().sizeDelta = new Vector2(15f, 15f);

                EventEditor.inst.TimelinePrefab = gameObject;
            }

            return false;
        }

        [HarmonyPatch(nameof(EventEditor.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix()
        {
            if (Input.GetMouseButtonUp(0))
            {
                Instance.eventDrag = false;
                RTEditor.inst.dragOffset = -1f;
            }

            if (Instance.eventDrag)
            {
                var timelineTime = EditorTimeline.inst.GetTimelineTime();
                foreach (var timelineObject in RTEventEditor.inst.SelectedKeyframes)
                {
                    if (timelineObject.Index == 0 || timelineObject.Locked)
                        continue;

                    GameData.Current.eventObjects.allEvents[timelineObject.Type][timelineObject.Index].eventTime =
                        Mathf.Clamp(timelineTime + Instance.mouseOffsetXForDrag + timelineObject.timeOffset, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                }

                if (!RTEventEditor.inst.SelectedKeyframes.All(x => x.Locked) && RTEditor.inst.dragOffset != timelineTime + Instance.mouseOffsetXForDrag)
                {
                    if (RTEditor.DraggingPlaysSound && (RTEditor.inst.editorInfo.bpmSnapActive || !RTEditor.DraggingPlaysSoundBPM))
                        SoundManager.inst.PlaySound(DefaultSounds.LeftRight, RTEditor.inst.editorInfo.bpmSnapActive ? 0.6f : 0.1f, 0.8f);

                    RTEditor.inst.dragOffset = timelineTime + Instance.mouseOffsetXForDrag;

                    RTEventEditor.inst.RenderEventObjects();
                    RTEventEditor.inst.RenderEventsDialog();
                }
            }

            return false;
        }

        public static float preNumber = 0f;

        [HarmonyPatch(nameof(EventEditor.CopyAllSelectedEvents))]
        [HarmonyPrefix]
        static bool CopyAllSelectedEventsPrefix()
        {
            RTEventEditor.inst.CopyAllSelectedEvents();
            return false;
        }

        [HarmonyPatch(nameof(EventEditor.AddedSelectedEvent))]
        [HarmonyPrefix]
        static bool AddedSelectedEventPrefix(int __0, int __1)
        {
            RTEventEditor.inst.AddSelectedEvent(__0, __1);
            return false;
        }

        [HarmonyPatch(nameof(EventEditor.SetCurrentEvent))]
        [HarmonyPrefix]
        static bool SetCurrentEventPrefix(int __0, int __1)
        {
            RTEventEditor.inst.SetCurrentEvent(__0, __1);
            return false;
        }

        [HarmonyPatch(nameof(EventEditor.CreateNewEventObject), new Type[] { typeof(int) })]
        [HarmonyPrefix]
        static bool CreateNewEventObjectPrefix(int __0)
        {
            RTEventEditor.inst.CreateNewEventObject(__0);
            return false;
        }

        [HarmonyPatch(nameof(EventEditor.CreateNewEventObject), new Type[] { typeof(float), typeof(int) })]
        [HarmonyPrefix]
        static bool CreateNewEventObjectPrefix(float __0, int __1)
        {
            RTEventEditor.inst.CreateNewEventObject(__0, __1);
            return false;
        }

        [HarmonyPatch(nameof(EventEditor.NewKeyframeFromTimeline))]
        [HarmonyPrefix]
        static bool NewKeyframeFromTimelinePrefix(int __0)
        {
            RTEventEditor.inst.NewKeyframeFromTimeline(__0);
            return false;
        }

        [HarmonyPatch(nameof(EventEditor.CreateEventObjects))]
        [HarmonyPrefix]
        static bool CreateEventObjectsPrefix()
        {
            RTEventEditor.inst.CreateEventObjects();
            return false;
        }

        [HarmonyPatch(nameof(EventEditor.RenderEventObjects))]
        [HarmonyPrefix]
        static bool RenderEventObjectsPatch()
        {
            RTEventEditor.inst.RenderEventObjects();
            return false;
        }

        [HarmonyPatch(nameof(EventEditor.OpenDialog))]
        [HarmonyPrefix]
        static bool OpenDialogPrefix()
        {
            RTEventEditor.inst.OpenDialog();
            return false;
        }

        [HarmonyPatch(nameof(EventEditor.RenderThemeContent))]
        [HarmonyPrefix]
        static bool RenderThemeContentPrefix(Transform __0, string __1)
        {
            RTThemeEditor.inst.RenderThemeContent(__0, __1);
            return false;
        }

        [HarmonyPatch(nameof(EventEditor.RenderThemeEditor))]
        [HarmonyPrefix]
        static bool RenderThemeEditorPrefix(int __0 = -1)
        {
            RTThemeEditor.inst.RenderThemeEditor(__0);
            return false;
        }

        [HarmonyPatch(nameof(EventEditor.RenderEventsDialog))]
        [HarmonyPrefix]
        static bool RenderEventsDialogPrefix()
        {
            RTEventEditor.inst.RenderEventsDialog();
            return false;
        }

        [HarmonyPatch(nameof(EventEditor.UpdateEventOrder))]
        [HarmonyPrefix]
        static bool UpdateEventOrderPrefix()
        {
            RTEventEditor.inst.UpdateEventOrder();
            return false;
        }

        [HarmonyPatch(nameof(EventEditor.DeleteEvent), new Type[] { typeof(int), typeof(int) })]
        [HarmonyPrefix]
        static bool DeleteEventPrefix(int __0, int __1)
        {
            RTEventEditor.inst.DeleteKeyframe(__0, __1);
            return false;
        }

        [HarmonyPatch(nameof(EventEditor.DeleteEvent), new Type[] { typeof(List<EventKeyframeSelection>) })]
        [HarmonyPrefix]
        static bool DeleteEventPrefix(ref IEnumerator __result)
        {
            __result = RTEventEditor.inst.DeleteKeyframes();
            return false;
        }
    }
}
