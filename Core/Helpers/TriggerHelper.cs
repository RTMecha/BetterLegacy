using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Helpers
{
    public static class TriggerHelper
    {
        #region Main

        public static void AddEventTriggers(GameObject gameObject, params EventTrigger.Entry[] entries)
        {
            var eventTrigger = gameObject.GetComponent<EventTrigger>() ?? gameObject.AddComponent<EventTrigger>();
            eventTrigger.triggers.Clear();
            eventTrigger.triggers.AddRange(entries);
        }

        public static EventTrigger.Entry CreateEntry(EventTriggerType triggerType, Action<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry();
            entry.eventID = triggerType;
            entry.callback.AddListener(eventData => { action?.Invoke(eventData); });
            return entry;
        }

        public static EventTrigger.Entry ScrollDelta(InputField inputField, float amount = 0.1f, float mutliply = 10f, float min = 0f, float max = 0f, bool multi = false) => CreateEntry(EventTriggerType.Scroll, eventData =>
        {
            if (!float.TryParse(inputField.text, out float result))
                return;

            var pointerEventData = (PointerEventData)eventData;

            if (!(!multi || !Input.GetKey(KeyCode.LeftShift)))
                return;

            var largeKey = !multi ? EditorConfig.Instance.ScrollwheelLargeAmountKey.Value : EditorConfig.Instance.ScrollwheelVector2LargeAmountKey.Value;
            var smallKey = !multi ? EditorConfig.Instance.ScrollwheelSmallAmountKey.Value : EditorConfig.Instance.ScrollwheelVector2SmallAmountKey.Value;
            var regularKey = !multi ? EditorConfig.Instance.ScrollwheelRegularAmountKey.Value : EditorConfig.Instance.ScrollwheelVector2RegularAmountKey.Value;

            bool large = largeKey == KeyCode.None && !Input.GetKey(smallKey) && !Input.GetKey(regularKey) || Input.GetKey(largeKey);
            bool small = smallKey == KeyCode.None && !Input.GetKey(largeKey) && !Input.GetKey(regularKey) || Input.GetKey(smallKey);
            bool regular = regularKey == KeyCode.None && !Input.GetKey(smallKey) && !Input.GetKey(largeKey) || Input.GetKey(regularKey);

            if (pointerEventData.scrollDelta.y < 0f)
                result -= small ? amount / mutliply : large ? amount * mutliply : regular ? amount : 0f;
            if (pointerEventData.scrollDelta.y > 0f)
                result += small ? amount / mutliply : large ? amount * mutliply : regular ? amount : 0f;

            result = RTMath.ClampZero(result, min, max);
            inputField.text = result.ToString("f2");
        });

        public static EventTrigger.Entry ScrollDeltaInt(InputField inputField, int amount = 1, int min = 0, int max = 0, bool multi = false) => CreateEntry(EventTriggerType.Scroll, eventData =>
        {
            if (!int.TryParse(inputField.text, out int result))
                return;

            var pointerEventData = (PointerEventData)eventData;

            if (!(!multi || !Input.GetKey(KeyCode.LeftShift)))
                return;

            if (pointerEventData.scrollDelta.y < 0f)
                result -= amount * (Input.GetKey(EditorConfig.Instance.ScrollwheelLargeAmountKey.Value) ? 10 : 1);
            if (pointerEventData.scrollDelta.y > 0f)
                result += amount * (Input.GetKey(EditorConfig.Instance.ScrollwheelLargeAmountKey.Value) ? 10 : 1);

            result = RTMath.ClampZero(result, min, max);
            if (inputField.text != result.ToString())
                inputField.text = result.ToString();
        });

        public static EventTrigger.Entry ScrollDeltaVector2(InputField ifx, InputField ify, float amount, float mutliply, List<float> clamp = null) => CreateEntry(EventTriggerType.Scroll, eventData =>
        {
            if (!Input.GetKey(KeyCode.LeftShift) || !float.TryParse(ifx.text, out float x) || !float.TryParse(ify.text, out float y))
                return;

            var pointerEventData = (PointerEventData)eventData;

            var largeKey = EditorConfig.Instance.ScrollwheelVector2LargeAmountKey.Value;
            var smallKey = EditorConfig.Instance.ScrollwheelVector2SmallAmountKey.Value;
            var regularKey = EditorConfig.Instance.ScrollwheelVector2RegularAmountKey.Value;

            // Large Amount
            bool large = largeKey == KeyCode.None && !Input.GetKey(smallKey) && !Input.GetKey(regularKey) || Input.GetKey(largeKey);

            // Small Amount
            bool small = smallKey == KeyCode.None && !Input.GetKey(largeKey) && !Input.GetKey(regularKey) || Input.GetKey(smallKey);

            // Regular Amount
            bool regular = regularKey == KeyCode.None && !Input.GetKey(smallKey) && !Input.GetKey(largeKey) || Input.GetKey(regularKey);

            if (pointerEventData.scrollDelta.y < 0f)
            {
                x -= small ? amount / mutliply : large ? amount * mutliply : regular ? amount : 0f;
                y -= small ? amount / mutliply : large ? amount * mutliply : regular ? amount : 0f;
            }

            if (pointerEventData.scrollDelta.y > 0f)
            {
                x += small ? amount / mutliply : large ? amount * mutliply : regular ? amount : 0f;
                y += small ? amount / mutliply : large ? amount * mutliply : regular ? amount : 0f;
            }

            if (clamp != null && clamp.Count > 1)
            {
                x = Mathf.Clamp(x, clamp[0], clamp[1]);
                if (clamp.Count == 2)
                    y = Mathf.Clamp(y, clamp[0], clamp[1]);
                else
                    y = Mathf.Clamp(y, clamp[2], clamp[3]);
            }

            ifx.text = x.ToString("f2");
            ify.text = y.ToString("f2");
        });

        public static EventTrigger.Entry ScrollDeltaVector2Int(InputField ifx, InputField ify, int amount, List<int> clamp = null) => CreateEntry(EventTriggerType.Scroll, eventData =>
        {
            if (!Input.GetKey(KeyCode.LeftShift) || !int.TryParse(ifx.text, out int x) || !int.TryParse(ify.text, out int y))
                return;

            var pointerEventData = (PointerEventData)eventData;

            bool large = Input.GetKey(KeyCode.LeftControl);

            if (pointerEventData.scrollDelta.y < 0f)
            {
                x -= large ? amount * 10 : amount;
                y -= large ? amount * 10 : amount;
            }

            if (pointerEventData.scrollDelta.y > 0f)
            {
                x += large ? amount * 10 : amount;
                y += large ? amount * 10 : amount;
            }

            if (clamp != null)
            {
                x = Mathf.Clamp(x, clamp[0], clamp[1]);
                if (clamp.Count == 2)
                    y = Mathf.Clamp(y, clamp[0], clamp[1]);
                else
                    y = Mathf.Clamp(y, clamp[2], clamp[3]);
            }

            ifx.text = x.ToString();
            ify.text = y.ToString();
        });

        public static EventTrigger.Entry ScrollDelta(Dropdown dropdown) => CreateEntry(EventTriggerType.Scroll, baseEventData =>
        {
            if (!EditorConfig.Instance.ScrollOnEasing.Value)
                return;

            var pointerEventData = (PointerEventData)baseEventData;
            if (pointerEventData.scrollDelta.y > 0f)
                dropdown.value = dropdown.value == 0 ? dropdown.options.Count - 1 : dropdown.value - 1;
            if (pointerEventData.scrollDelta.y < 0f)
                dropdown.value = dropdown.value == dropdown.options.Count - 1 ? 0 : dropdown.value + 1;
        });

        public static void IncreaseDecreaseButtons(InputField inputField, float amount = 0.1f, float multiply = 10f, float min = 0f, float max = 0f, Transform t = null)
        {
            var tf = t ?? inputField.transform;

            float num = amount;

            var btR = tf.Find("<").GetComponent<Button>();
            var btL = tf.Find(">").GetComponent<Button>();

            btR.onClick.ClearAll();
            btR.onClick.AddListener(() =>
            {
                if (float.TryParse(inputField.text, out float result))
                    inputField.text = RTMath.ClampZero(result - (Input.GetKey(KeyCode.LeftAlt) ? amount / multiply : Input.GetKey(KeyCode.LeftControl) ? amount * multiply : amount), min, max).ToString();
            });

            btL.onClick.ClearAll();
            btL.onClick.AddListener(() =>
            {
                if (float.TryParse(inputField.text, out float result))
                    inputField.text = RTMath.ClampZero(result + (Input.GetKey(KeyCode.LeftAlt) ? amount / multiply : Input.GetKey(KeyCode.LeftControl) ? amount * multiply : amount), min, max).ToString();
            });

            if (tf.TryFind("<<", out Transform btLargeRTF) && btLargeRTF.gameObject.TryGetComponent(out Button btLargeR))
            {
                btLargeR.onClick.ClearAll();
                btLargeR.onClick.AddListener(() =>
                {
                    if (float.TryParse(inputField.text, out float result))
                        inputField.text = RTMath.ClampZero(result - ((Input.GetKey(KeyCode.LeftAlt) ? amount / multiply : Input.GetKey(KeyCode.LeftControl) ? amount * multiply : amount) * 10f), min, max).ToString();
                });
            }

            if (tf.TryFind(">>", out Transform btLargeLTF) && btLargeLTF.gameObject.TryGetComponent(out Button btLargeL))
            {
                btLargeL.onClick.ClearAll();
                btLargeL.onClick.AddListener(() =>
                {
                    if (float.TryParse(inputField.text, out float result))
                        inputField.text = RTMath.ClampZero(result + ((Input.GetKey(KeyCode.LeftAlt) ? amount / multiply : Input.GetKey(KeyCode.LeftControl) ? amount * multiply : amount) * 10f), min, max).ToString();
                });
            }
        }

        public static void IncreaseDecreaseButtonsInt(InputField inputField, int amount = 1, int min = 0, int max = 0, Transform t = null)
        {
            var tf = t ?? inputField.transform;

            float num = amount;

            var btR = tf.Find("<").GetComponent<Button>();
            var btL = tf.Find(">").GetComponent<Button>();

            btR.onClick.ClearAll();
            btR.onClick.AddListener(() =>
            {
                if (float.TryParse(inputField.text, out float result))
                    inputField.text = RTMath.ClampZero(result - (Input.GetKey(KeyCode.LeftControl) ? amount * 10 : amount), min, max).ToString();
            });

            btL.onClick.ClearAll();
            btL.onClick.AddListener(() =>
            {
                if (float.TryParse(inputField.text, out float result))
                    inputField.text = RTMath.ClampZero(result + (Input.GetKey(KeyCode.LeftControl) ? amount * 10 : amount), min, max).ToString();
            });
        }

        public static void IncreaseDecreaseButtons(InputFieldStorage inputFieldStorage, float amount = 0.1f, float multiply = 10f, float min = 0f, float max = 0f)
        {
            inputFieldStorage.leftButton.onClick.ClearAll();
            inputFieldStorage.leftButton.onClick.AddListener(() =>
            {
                if (float.TryParse(inputFieldStorage.inputField.text, out float result))
                    inputFieldStorage.inputField.text = RTMath.ClampZero(result - amount, min, max).ToString();
            });

            inputFieldStorage.rightButton.onClick.ClearAll();
            inputFieldStorage.rightButton.onClick.AddListener(() =>
            {
                if (float.TryParse(inputFieldStorage.inputField.text, out float result))
                    inputFieldStorage.inputField.text = RTMath.ClampZero(result + amount, min, max).ToString();
            });

            if (inputFieldStorage.leftGreaterButton == null || inputFieldStorage.rightGreaterButton == null)
                return;

            inputFieldStorage.leftGreaterButton.onClick.ClearAll();
            inputFieldStorage.leftGreaterButton.onClick.AddListener(() =>
            {
                if (float.TryParse(inputFieldStorage.inputField.text, out float result))
                    inputFieldStorage.inputField.text = RTMath.ClampZero(result - (amount * multiply), min, max).ToString();
            });

            inputFieldStorage.rightGreaterButton.onClick.ClearAll();
            inputFieldStorage.rightGreaterButton.onClick.AddListener(() =>
            {
                if (float.TryParse(inputFieldStorage.inputField.text, out float result))
                    inputFieldStorage.inputField.text = RTMath.ClampZero(result + (amount * multiply), min, max).ToString();
            });
        }
        
        public static void IncreaseDecreaseButtonsInt(InputFieldStorage inputFieldStorage, int amount = 1, int multiply = 10, float min = 0f, float max = 0f)
        {
            inputFieldStorage.leftButton.onClick.ClearAll();
            inputFieldStorage.leftButton.onClick.AddListener(() =>
            {
                if (int.TryParse(inputFieldStorage.inputField.text, out int result))
                    inputFieldStorage.inputField.text = RTMath.ClampZero(result - amount, min, max).ToString();
            });

            inputFieldStorage.rightButton.onClick.ClearAll();
            inputFieldStorage.rightButton.onClick.AddListener(() =>
            {
                if (int.TryParse(inputFieldStorage.inputField.text, out int result))
                    inputFieldStorage.inputField.text = RTMath.ClampZero(result + amount, min, max).ToString();
            });

            if (inputFieldStorage.leftGreaterButton == null || inputFieldStorage.rightGreaterButton == null)
                return;

            inputFieldStorage.leftGreaterButton.onClick.ClearAll();
            inputFieldStorage.leftGreaterButton.onClick.AddListener(() =>
            {
                if (int.TryParse(inputFieldStorage.inputField.text, out int result))
                    inputFieldStorage.inputField.text = RTMath.ClampZero(result - (amount * multiply), min, max).ToString();
            });

            inputFieldStorage.rightGreaterButton.onClick.ClearAll();
            inputFieldStorage.rightGreaterButton.onClick.AddListener(() =>
            {
                if (int.TryParse(inputFieldStorage.inputField.text, out int result))
                    inputFieldStorage.inputField.text = RTMath.ClampZero(result + (amount * multiply), min, max).ToString();
            });
        }

        public static void SetInteractable(bool interactable, params Selectable[] buttons)
        {
            foreach (var button in buttons)
                button.interactable = interactable;
        }

        #endregion

        #region Timeline

        public static EventTrigger.Entry StartDragTrigger() => CreateEntry(EventTriggerType.BeginDrag, eventData =>
        {
            var pointerEventData = (PointerEventData)eventData;
            EditorManager.inst.DragStartPos = pointerEventData.position * CoreHelper.ScreenScaleInverse;
            if (pointerEventData.button == PointerEventData.InputButton.Middle)
            {
                EditorTimeline.inst.StartTimelineDrag();
                return;
            }

            EditorManager.inst.SelectionBoxImage.gameObject.SetActive(true);
            EditorManager.inst.SelectionRect = default;
        });

        public static EventTrigger.Entry DragTrigger() => CreateEntry(EventTriggerType.Drag, eventData =>
        {
            if (EditorTimeline.inst.movingTimeline)
                return;

            var vector = ((PointerEventData)eventData).position * CoreHelper.ScreenScaleInverse;

            EditorManager.inst.SelectionRect.xMin = vector.x < EditorManager.inst.DragStartPos.x ? vector.x : EditorManager.inst.DragStartPos.x;
            EditorManager.inst.SelectionRect.xMax = vector.x < EditorManager.inst.DragStartPos.x ? EditorManager.inst.DragStartPos.x : vector.x;

            EditorManager.inst.SelectionRect.yMin = vector.y < EditorManager.inst.DragStartPos.y ? vector.y : EditorManager.inst.DragStartPos.y;
            EditorManager.inst.SelectionRect.yMax = vector.y < EditorManager.inst.DragStartPos.y ? EditorManager.inst.DragStartPos.y : vector.y;

            EditorManager.inst.SelectionBoxImage.rectTransform.offsetMin = EditorManager.inst.SelectionRect.min;
            EditorManager.inst.SelectionBoxImage.rectTransform.offsetMax = EditorManager.inst.SelectionRect.max;
        });

        public static EventTrigger.Entry EndDragTrigger() => CreateEntry(EventTriggerType.EndDrag, eventData =>
        {
            EditorManager.inst.DragEndPos = ((PointerEventData)eventData).position;
            EditorManager.inst.SelectionBoxImage.gameObject.SetActive(false);

            if (EditorTimeline.inst.movingTimeline)
            {
                EditorTimeline.inst.movingTimeline = false;
                return;
            }

            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Objects)
                CoroutineHelper.StartCoroutine(EditorTimeline.inst.GroupSelectObjects(Input.GetKey(KeyCode.LeftShift), Input.GetKey(KeyCode.LeftAlt)));
            else
                RTEventEditor.inst.StartCoroutine(RTEventEditor.inst.GroupSelectKeyframes(Input.GetKey(KeyCode.LeftShift), Input.GetKey(KeyCode.LeftAlt)));
        });

        #endregion

        #region Keyframes

        public static EventTrigger.Entry CreateTimelineKeyframeTrigger(TimelineKeyframe timelineKeyframe) => CreateEntry(EventTriggerType.PointerClick, eventData =>
        {
            var pointerEventData = (PointerEventData)eventData;

            if (pointerEventData.button == PointerEventData.InputButton.Right)
                return;

            if (timelineKeyframe.isObjectKeyframe)
                ObjectEditor.inst.SetCurrentKeyframe(timelineKeyframe.beatmapObject, timelineKeyframe.Type, timelineKeyframe.Index, false, InputDataManager.inst.editorActions.MultiSelect.IsPressed);
            else if (!EventEditor.inst.eventDrag)
                (InputDataManager.inst.editorActions.MultiSelect.IsPressed ?
                    (Action<int, int>)RTEventEditor.inst.AddSelectedEvent : RTEventEditor.inst.SetCurrentEvent)(timelineKeyframe.Type, timelineKeyframe.Index);
        });

        public static EventTrigger.Entry CreateTimelineKeyframeStartDragTrigger(TimelineKeyframe timelineKeyframe) => CreateEntry(EventTriggerType.BeginDrag, eventData =>
        {
            if (timelineKeyframe.Index == 0)
            {
                EditorManager.inst.DisplayNotification("Can't change the time of the first Keyframe.", 3f, EditorManager.NotificationType.Warning);
                return;
            }

            if (timelineKeyframe.isObjectKeyframe)
            {
                var pointerEventData = (PointerEventData)eventData;
                if (pointerEventData.button == PointerEventData.InputButton.Middle)
                {
                    EditorManager.inst.DragStartPos = pointerEventData.position * CoreHelper.ScreenScaleInverse;
                    ObjectEditor.inst.StartTimelineDrag();
                    return;
                }

                var beatmapObject = timelineKeyframe.beatmapObject;
                ObjEditor.inst.currentKeyframeKind = timelineKeyframe.Type;
                ObjEditor.inst.currentKeyframe = timelineKeyframe.Index;

                var list = beatmapObject.timelineObject.InternalTimelineObjects;
                if (list.FindIndex(x => x.Type == timelineKeyframe.Type && x.Index == timelineKeyframe.Index) != -1)
                    foreach (var otherTLO in beatmapObject.timelineObject.InternalTimelineObjects)
                        otherTLO.timeOffset = otherTLO.Type == ObjEditor.inst.currentKeyframeKind && otherTLO.Index == ObjEditor.inst.currentKeyframe ? 0f : otherTLO.Time - timelineKeyframe.Time;
                ObjEditor.inst.mouseOffsetXForKeyframeDrag = timelineKeyframe.Time - ObjectEditor.MouseTimelineCalc();
                ObjEditor.inst.timelineKeyframesDrag = true;
            }
            else
            {
                if (RTEventEditor.inst.SelectedKeyframes.FindIndex(x => x.Type == timelineKeyframe.Type && x.Index == timelineKeyframe.Index) != -1)
                {
                    foreach (var timelineObject in RTEventEditor.inst.SelectedKeyframes)
                    {
                        timelineObject.timeOffset =
                            timelineObject.Type == timelineKeyframe.Type && timelineObject.Index == timelineKeyframe.Index ? 0f :
                                timelineObject.Time - GameData.Current.events[timelineKeyframe.Type][timelineKeyframe.Index].time;
                    }
                }
                else
                    EventEditor.inst.SetCurrentEvent(timelineKeyframe.Type, timelineKeyframe.Index);

                float timelineTime = EditorTimeline.inst.GetTimelineTime(RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsKeyframes.Value);
                EventEditor.inst.mouseOffsetXForDrag = timelineKeyframe.eventKeyframe.time - timelineTime;
                EventEditor.inst.eventDrag = true;
            }
        });

        public static EventTrigger.Entry CreateTimelineKeyframeEndDragTrigger(TimelineKeyframe timelineKeyframe) => CreateEntry(EventTriggerType.EndDrag, eventData =>
        {
            if (timelineKeyframe.isObjectKeyframe)
            {
                if (EditorTimeline.inst.movingTimeline)
                {
                    EditorTimeline.inst.movingTimeline = false;
                    return;
                }

                var beatmapObject = timelineKeyframe.beatmapObject;
                ObjectEditor.inst.UpdateKeyframeOrder(beatmapObject);

                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));

                ObjectEditor.inst.RenderKeyframes(beatmapObject);
                ObjectEditor.inst.ResizeKeyframeTimeline(beatmapObject);
                ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
                ObjectEditor.inst.RenderMarkers(beatmapObject);
                ObjEditor.inst.timelineKeyframesDrag = false;
            }
            else
            {
                EventEditor.inst.eventDrag = false;
                RTLevel.Current?.UpdateEvents(timelineKeyframe.Type);

                RTEventEditor.inst.OpenDialog();
            }
        });

        public static EventTrigger.Entry CreateTimelineKeyframeSelectTrigger(TimelineKeyframe timelineKeyframe) => CreateEntry(EventTriggerType.PointerDown, eventData =>
        {
            if ((eventData as PointerEventData).button != PointerEventData.InputButton.Right)
                return;

            if (timelineKeyframe.isObjectKeyframe)
            {
                var beatmapObject = timelineKeyframe.beatmapObject;
                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Set Cursor to KF", () => AudioManager.inst.SetMusicTime(beatmapObject.StartTime + timelineKeyframe.Time)),
                    new ButtonFunction("Set KF to Cursor", () =>
                    {
                        var time = beatmapObject.StartTime - AudioManager.inst.CurrentAudioSource.time;
                        var selected = RTEventEditor.inst.SelectedKeyframes;
                        for (int i = 0; i < selected.Count; i++)
                            selected[i].Time = Mathf.Clamp(time, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                        ObjectEditor.inst.RenderKeyframes(beatmapObject);
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Copy", () => ObjectEditor.inst.CopyAllSelectedEvents(beatmapObject)),
                    new ButtonFunction("Paste", () => ObjectEditor.inst.PasteKeyframes(beatmapObject)),
                    new ButtonFunction("Copy Data", () =>
                    {
                        ObjectEditor.inst.CopyData(timelineKeyframe.Type, timelineKeyframe.eventKeyframe);
                        EditorManager.inst.DisplayNotification("Copied keyframe data!", 2f, EditorManager.NotificationType.Success);
                    }),
                    new ButtonFunction("Paste Data", () => ObjectEditor.inst.PasteKeyframeData(timelineKeyframe.Type, beatmapObject.timelineObject.InternalTimelineObjects.Where(x => x.Selected), beatmapObject)),
                    new ButtonFunction("Delete", RTEditor.inst.Delete),
                    new ButtonFunction(true),
                    new ButtonFunction("Set to Camera", () =>
                    {
                        switch (timelineKeyframe.Type)
                        {
                            case 0: {
                                    timelineKeyframe.eventKeyframe.values[0] = EventManager.inst.cam.transform.position.x;
                                    timelineKeyframe.eventKeyframe.values[1] = EventManager.inst.cam.transform.position.y;
                                    if (ObjectEditor.inst.Dialog.IsCurrent)
                                        ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
                                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                                    break;
                                }
                            case 1: {
                                    timelineKeyframe.eventKeyframe.values[0] = EventManager.inst.cam.orthographicSize / 20f;
                                    timelineKeyframe.eventKeyframe.values[1] = EventManager.inst.cam.orthographicSize / 20f;
                                    if (ObjectEditor.inst.Dialog.IsCurrent)
                                        ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
                                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                                    break;
                                }
                            case 2: {
                                    timelineKeyframe.eventKeyframe.values[0] = EventManager.inst.cam.transform.eulerAngles.x;
                                    if (ObjectEditor.inst.Dialog.IsCurrent)
                                        ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
                                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.KEYFRAMES);
                                    break;
                                }
                            case 3: {
                                    EditorManager.inst.DisplayNotification("Cannot apply any camera values to the color keyframe.", 3f, EditorManager.NotificationType.Warning);
                                    break;
                                }
                        }
                    })
                    );
            }
            else
            {
                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Set Cursor to KF", () => AudioManager.inst.SetMusicTime(timelineKeyframe.Time)),
                    new ButtonFunction("Set KF to Cursor", () =>
                    {
                        var time = AudioManager.inst.CurrentAudioSource.time;
                        var selected = RTEventEditor.inst.SelectedKeyframes;
                        for (int i = 0; i < selected.Count; i++)
                            selected[i].Time = Mathf.Clamp(time, 0f, AudioManager.inst.CurrentAudioSource.clip.length);

                        RTLevel.Current?.UpdateEvents(timelineKeyframe.Type);
                        RTEventEditor.inst.RenderEventObjects();
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Reset", () =>
                    {
                        var eventKeyframe = timelineKeyframe.eventKeyframe;
                        var defaultKeyframe = GameData.DefaultKeyframes[timelineKeyframe.Type];
                        for (int i = 0; i < eventKeyframe.values.Length; i++)
                            if (i < defaultKeyframe.values.Length)
                                eventKeyframe.values[i] = defaultKeyframe.values[i];
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Copy", RTEventEditor.inst.CopyAllSelectedEvents),
                    new ButtonFunction("Paste", () => RTEventEditor.inst.PasteEvents()),
                    new ButtonFunction("Copy Data", () => RTEventEditor.inst.CopyKeyframeData(RTEventEditor.inst.CurrentSelectedTimelineKeyframe)),
                    new ButtonFunction("Paste Data", () => RTEventEditor.inst.PasteKeyframeData(EventEditor.inst.currentEventType)),
                    new ButtonFunction("Delete", RTEditor.inst.Delete)
                    );
            }
        });

        #endregion

        #region Objects

        public static EventTrigger.Entry CreateBeatmapObjectStartDragTrigger(TimelineObject timelineObject) => CreateEntry(EventTriggerType.BeginDrag, eventData =>
        {
            var pointerEventData = (PointerEventData)eventData;
            if (pointerEventData.button == PointerEventData.InputButton.Middle)
            {
                EditorManager.inst.DragStartPos = pointerEventData.position * CoreHelper.ScreenScaleInverse;
                EditorTimeline.inst.StartTimelineDrag();
                return;
            }

            int bin = timelineObject.Bin;

            foreach (var otherTLO in EditorTimeline.inst.timelineObjects)
            {
                otherTLO.timeOffset = otherTLO.Time - timelineObject.Time;
                otherTLO.binOffset = otherTLO.Bin - bin;
            }

            timelineObject.timeOffset = 0f;
            timelineObject.binOffset = 0;

            float timelineTime = EditorTimeline.inst.GetTimelineTime(RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsObjects.Value);
            int num = 14 - Mathf.RoundToInt((Input.mousePosition.y - 25f) * EditorManager.inst.ScreenScaleInverse / 20f);
            ObjEditor.inst.mouseOffsetXForDrag = timelineObject.Time - timelineTime;
            ObjEditor.inst.mouseOffsetYForDrag = bin - num;
            ObjEditor.inst.beatmapObjectsDrag = true;
        });

        public static EventTrigger.Entry CreateBeatmapObjectEndDragTrigger(TimelineObject timelineObject) => CreateEntry(EventTriggerType.EndDrag, eventData =>
        {
            ObjEditor.inst.beatmapObjectsDrag = false;

            if (EditorTimeline.inst.movingTimeline)
            {
                EditorTimeline.inst.movingTimeline = false;
                return;
            }

            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                if (!ObjectEditor.UpdateObjects)
                    continue;

                if (timelineObject.isBeatmapObject)
                {
                    var beatmapObject = timelineObject.GetData<BeatmapObject>();
                    RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.START_TIME, false);
                    if (beatmapObject.desync)
                        RTLevel.Current?.UpdateObject(beatmapObject, RTLevel.ObjectContext.PARENT_CHAIN, false);
                }
                if (timelineObject.isPrefabObject)
                    RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), RTLevel.PrefabContext.TIME, false);
                if (timelineObject.isBackgroundObject)
                    RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), RTLevel.BackgroundObjectContext.START_TIME, false);
            }

            RTLevel.Current?.Sort();

            if (EditorTimeline.inst.TimelineBeatmapObjects.Count == 1)
            {
                switch (timelineObject.TimelineReference)
                {
                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                            ObjectEditor.inst.RenderDialog(timelineObject.GetData<BeatmapObject>());
                            break;
                        }
                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                            RTPrefabEditor.inst.RenderPrefabObjectDialog(timelineObject.GetData<PrefabObject>());
                            break;
                        }
                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                            RTBackgroundEditor.inst.RenderDialog(timelineObject.GetData<BackgroundObject>());
                            break;
                        }
                }
            }
        });

        public static EventTrigger.Entry CreateBeatmapObjectTrigger(TimelineObject timelineObject) => CreateEntry(EventTriggerType.PointerUp, eventData =>
        {
            var pointerEventData = (PointerEventData)eventData;
            if (ObjEditor.inst.beatmapObjectsDrag || pointerEventData.button == PointerEventData.InputButton.Middle)
                return;
            
            CoreHelper.Log($"Selecting [ {timelineObject.ID} ]");

            if (!RTEditor.inst.parentPickerEnabled && !RTEditor.inst.prefabPickerEnabled && EditorTimeline.inst.onSelectTimelineObject == null && pointerEventData.button == PointerEventData.InputButton.Right)
            {
                timelineObject.ShowContextMenu();
                return;
            }

            if (pointerEventData.button == PointerEventData.InputButton.Right)
                return;

            float timelineTime = EditorTimeline.inst.GetTimelineTime(RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsObjects.Value);
            ObjEditor.inst.mouseOffsetXForDrag = timelineObject.Time - timelineTime;
            EditorTimeline.inst.SelectObject(timelineObject);
        });

        #endregion

        #region Themes

        public static EventTrigger.Entry CreatePreviewClickTrigger(Image _preview, Image _dropper, InputField _hex, Color _col, string popupName = "") => CreateEntry(EventTriggerType.PointerClick, eventData =>
        {
            RTEditor.inst.ShowDialog("Color Picker");
            if (!string.IsNullOrEmpty(popupName))
                RTEditor.inst.HideDialog(popupName);

            var colorPickerTF = EditorManager.inst.GetDialog("Color Picker").Dialog.Find("content/Color Picker");
            var colorPicker = colorPickerTF.GetComponent<ColorPicker>();

            colorPicker.SwitchCurrentColor(_col);

            var save = colorPickerTF.Find("info/hex/save").GetComponent<Button>();

            save.onClick.ClearAll();
            save.onClick.AddListener(() =>
            {
                EditorManager.inst.ClearPopups();
                if (!string.IsNullOrEmpty(popupName))
                    RTEditor.inst.ShowDialog(popupName);

                double saturation;
                double num;
                LSColors.ColorToHSV(colorPicker.currentColor, out double _, out saturation, out num);
                _hex.text = colorPicker.currentHex;
                _preview.color = colorPicker.currentColor;

                if (!_dropper)
                    return;

                _dropper.color = RTColors.InvertColorHue(RTColors.InvertColorValue(colorPicker.currentColor));
            });
        });

        #endregion
    }
}
