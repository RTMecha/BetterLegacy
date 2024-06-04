using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor;
using BetterLegacy.Editor.Managers;
using LSFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterLegacy.Core.Helpers
{
    public static class TriggerHelper
    {
        public static void AddEventTriggerParams(GameObject gameObject, params EventTrigger.Entry[] entries) => AddEventTrigger(gameObject, entries.ToList());

        public static void AddEventTrigger(GameObject _if, List<EventTrigger.Entry> entries, bool clear = true)
        {
            if (!_if.GetComponent<EventTrigger>())
            {
                _if.AddComponent<EventTrigger>();
            }
            var et = _if.GetComponent<EventTrigger>();
            if (clear)
                et.triggers.Clear();
            foreach (var entry in entries)
                et.triggers.Add(entry);
        }

        public static bool FloatNearZero(float f, float range = 0.01f) => f < range && f > -range;

        public static EventTrigger.Entry CreateEntry(EventTriggerType triggerType, Action<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry();
            entry.eventID = triggerType;
            entry.callback.AddListener(delegate (BaseEventData eventData)
            {
                action?.Invoke(eventData);
            });
            return entry;
        }

        public static EventTrigger.Entry ScrollDelta(InputField inputField, float amount = 0.1f, float mutliply = 10f, float min = 0f, float max = 0f, bool multi = false)
        {
            var entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.Scroll;
            entry.callback.AddListener(delegate (BaseEventData eventData)
            {
                var pointerEventData = (PointerEventData)eventData;

                if (float.TryParse(inputField.text, out float result))
                {
                    if (!multi || !Input.GetKey(KeyCode.LeftShift))
                    {
                        var config = EditorConfig.Instance;

                        var largeKey = !multi ? config.ScrollwheelLargeAmountKey.Value : config.ScrollwheelVector2LargeAmountKey.Value;
                        var smallKey = !multi ? config.ScrollwheelSmallAmountKey.Value : config.ScrollwheelVector2SmallAmountKey.Value;
                        var regularKey = !multi ? config.ScrollwheelRegularAmountKey.Value : config.ScrollwheelVector2RegularAmountKey.Value;

                        // Large Amount
                        bool large = largeKey == KeyCode.None && !Input.GetKey(smallKey) && !Input.GetKey(regularKey) || Input.GetKey(largeKey);

                        // Small Amount
                        bool small = smallKey == KeyCode.None && !Input.GetKey(largeKey) && !Input.GetKey(regularKey) || Input.GetKey(smallKey);

                        // Regular Amount
                        bool regular = regularKey == KeyCode.None && !Input.GetKey(smallKey) && !Input.GetKey(largeKey) || Input.GetKey(regularKey);

                        if (pointerEventData.scrollDelta.y < 0f)
                            result -= small ? amount / mutliply : large ? amount * mutliply : regular ? amount : 0f;
                        if (pointerEventData.scrollDelta.y > 0f)
                            result += small ? amount / mutliply : large ? amount * mutliply : regular ? amount : 0f;

                        if (min != 0f || max != 0f)
                            result = Mathf.Clamp(result, min, max);

                        inputField.text = result.ToString("f2");
                    }
                }
            });
            return entry;
        }

        public static EventTrigger.Entry ScrollDeltaInt(InputField inputField, int amount = 1, int min = 0, int max = 0, bool multi = false)
        {
            var entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.Scroll;
            entry.callback.AddListener(delegate (BaseEventData eventData)
            {
                var pointerEventData = (PointerEventData)eventData;

                if (int.TryParse(inputField.text, out int result))
                {
                    if (!multi || !Input.GetKey(KeyCode.LeftShift))
                    {
                        bool large = Input.GetKey(KeyCode.LeftControl);

                        if (pointerEventData.scrollDelta.y < 0f)
                            result -= amount * (large ? 10 : 1);
                        if (pointerEventData.scrollDelta.y > 0f)
                            result += amount * (large ? 10 : 1);

                        if (min != 0f || max != 0f)
                            result = Mathf.Clamp(result, min, max);

                        if (inputField.text != result.ToString())
                            inputField.text = result.ToString();
                    }
                }
            });
            return entry;
        }

        public static EventTrigger.Entry ScrollDeltaVector2(InputField ifx, InputField ify, float amount, float mutliply, List<float> clamp = null)
        {
            var entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.Scroll;
            entry.callback.AddListener(delegate (BaseEventData eventData)
            {
                var pointerEventData = (PointerEventData)eventData;
                if (Input.GetKey(KeyCode.LeftShift) && float.TryParse(ifx.text, out float x) && float.TryParse(ify.text, out float y))
                {
                    var config = EditorConfig.Instance;

                    var largeKey = config.ScrollwheelVector2LargeAmountKey.Value;
                    var smallKey = config.ScrollwheelVector2SmallAmountKey.Value;
                    var regularKey = config.ScrollwheelVector2RegularAmountKey.Value;

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
                }
            });
            return entry;
        }

        public static EventTrigger.Entry ScrollDeltaVector2Int(InputField ifx, InputField ify, int amount, List<int> clamp = null)
        {
            var entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.Scroll;
            entry.callback.AddListener(delegate (BaseEventData eventData)
            {
                var pointerEventData = (PointerEventData)eventData;
                if (Input.GetKey(KeyCode.LeftShift) && int.TryParse(ifx.text, out int x) && int.TryParse(ify.text, out int y))
                {
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
                }
            });
            return entry;
        }

        public static void IncreaseDecreaseButtons(InputField inputField, float amount = 0.1f, float multiply = 10f, float min = 0f, float max = 0f, Transform t = null)
        {
            var tf = t ?? inputField.transform;

            float num = amount;

            var btR = tf.Find("<").GetComponent<Button>();
            var btL = tf.Find(">").GetComponent<Button>();

            btR.onClick.ClearAll();
            btR.onClick.AddListener(delegate ()
            {
                if (float.TryParse(inputField.text, out float result))
                {
                    result -= Input.GetKey(KeyCode.LeftAlt) ? amount / multiply : Input.GetKey(KeyCode.LeftControl) ? amount * multiply : amount;

                    if (min != 0f || max != 0f)
                        result = Mathf.Clamp(result, min, max);

                    inputField.text = result.ToString();
                }
            });

            btL.onClick.ClearAll();
            btL.onClick.AddListener(delegate ()
            {
                if (float.TryParse(inputField.text, out float result))
                {
                    result += Input.GetKey(KeyCode.LeftAlt) ? amount / multiply : Input.GetKey(KeyCode.LeftControl) ? amount * multiply : amount;

                    if (min != 0f || max != 0f)
                        result = Mathf.Clamp(result, min, max);

                    inputField.text = result.ToString();
                }
            });

            if (tf.TryFind("<<", out Transform btLargeRTF) && btLargeRTF.gameObject.TryGetComponent(out Button btLargeR))
            {
                btLargeR.onClick.ClearAll();
                btLargeR.onClick.AddListener(delegate ()
                {
                    if (float.TryParse(inputField.text, out float result))
                    {
                        result -= (Input.GetKey(KeyCode.LeftAlt) ? amount / multiply : Input.GetKey(KeyCode.LeftControl) ? amount * multiply : amount) * 10f;

                        if (min != 0f || max != 0f)
                            result = Mathf.Clamp(result, min, max);

                        inputField.text = result.ToString();
                    }
                });
            }

            if (tf.TryFind(">>", out Transform btLargeLTF) && btLargeLTF.gameObject.TryGetComponent(out Button btLargeL))
            {
                btLargeL.onClick.ClearAll();
                btLargeL.onClick.AddListener(delegate ()
                {
                    if (float.TryParse(inputField.text, out float result))
                    {
                        result += (Input.GetKey(KeyCode.LeftAlt) ? amount / multiply : Input.GetKey(KeyCode.LeftControl) ? amount * multiply : amount) * 10f;

                        if (min != 0f || max != 0f)
                            result = Mathf.Clamp(result, min, max);

                        inputField.text = result.ToString();
                    }
                });
            }
        }

        public static void IncreaseDecreaseButtonsInt(InputField inputField, int amount = 1, int min = 0, int max = 0, Transform t = null)
        {
            var tf = t ?? inputField.transform;

            float num = amount;

            var btR = tf.Find("<").GetComponent<Button>();
            var btL = tf.Find(">").GetComponent<Button>();

            btR.onClick.RemoveAllListeners();
            btR.onClick.AddListener(delegate ()
            {
                if (float.TryParse(inputField.text, out float result))
                {
                    result -= Input.GetKey(KeyCode.LeftControl) ? amount * 10 : amount;

                    if (min != 0f || max != 0f)
                        result = Mathf.Clamp(result, min, max);

                    inputField.text = result.ToString();
                }
            });

            btL.onClick.RemoveAllListeners();
            btL.onClick.AddListener(delegate ()
            {
                if (float.TryParse(inputField.text, out float result))
                {
                    result += Input.GetKey(KeyCode.LeftControl) ? amount * 10 : amount;

                    if (min != 0f || max != 0f)
                        result = Mathf.Clamp(result, min, max);

                    inputField.text = result.ToString();
                }
            });
        }

        public static void IncreaseDecreaseButtons(InputFieldStorage inputFieldStorage, float amount = 0.1f, float multiply = 10f, float min = 0f, float max = 0f)
        {
            inputFieldStorage.leftButton.onClick.ClearAll();
            inputFieldStorage.leftButton.onClick.AddListener(delegate ()
            {
                if (float.TryParse(inputFieldStorage.inputField.text, out float result))
                    inputFieldStorage.inputField.text = RTMath.ClampZero(result - amount, min, max).ToString();
            });

            inputFieldStorage.rightButton.onClick.ClearAll();
            inputFieldStorage.rightButton.onClick.AddListener(delegate ()
            {
                if (float.TryParse(inputFieldStorage.inputField.text, out float result))
                    inputFieldStorage.inputField.text = RTMath.ClampZero(result + amount, min, max).ToString();
            });

            if (inputFieldStorage.leftGreaterButton == null || inputFieldStorage.rightGreaterButton == null)
                return;

            inputFieldStorage.leftGreaterButton.onClick.ClearAll();
            inputFieldStorage.leftGreaterButton.onClick.AddListener(delegate ()
            {
                if (float.TryParse(inputFieldStorage.inputField.text, out float result))
                    inputFieldStorage.inputField.text = RTMath.ClampZero(result - (amount * multiply), min, max).ToString();
            });

            inputFieldStorage.rightGreaterButton.onClick.ClearAll();
            inputFieldStorage.rightGreaterButton.onClick.AddListener(delegate ()
            {
                if (float.TryParse(inputFieldStorage.inputField.text, out float result))
                    inputFieldStorage.inputField.text = RTMath.ClampZero(result + (amount * multiply), min, max).ToString();
            });
        }
        
        public static void IncreaseDecreaseButtonsInt(InputFieldStorage inputFieldStorage, int amount = 1, int multiply = 10, float min = 0f, float max = 0f)
        {
            inputFieldStorage.leftButton.onClick.ClearAll();
            inputFieldStorage.leftButton.onClick.AddListener(delegate ()
            {
                if (int.TryParse(inputFieldStorage.inputField.text, out int result))
                    inputFieldStorage.inputField.text = RTMath.ClampZero(result - amount, min, max).ToString();
            });

            inputFieldStorage.rightButton.onClick.ClearAll();
            inputFieldStorage.rightButton.onClick.AddListener(delegate ()
            {
                if (int.TryParse(inputFieldStorage.inputField.text, out int result))
                    inputFieldStorage.inputField.text = RTMath.ClampZero(result + amount, min, max).ToString();
            });

            if (inputFieldStorage.leftGreaterButton == null || inputFieldStorage.rightGreaterButton == null)
                return;

            inputFieldStorage.leftGreaterButton.onClick.ClearAll();
            inputFieldStorage.leftGreaterButton.onClick.AddListener(delegate ()
            {
                if (int.TryParse(inputFieldStorage.inputField.text, out int result))
                    inputFieldStorage.inputField.text = RTMath.ClampZero(result - (amount * multiply), min, max).ToString();
            });

            inputFieldStorage.rightGreaterButton.onClick.ClearAll();
            inputFieldStorage.rightGreaterButton.onClick.AddListener(delegate ()
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

        #region Timeline

        public static EventTrigger.Entry StartDragTrigger()
        {
            var editorManager = EditorManager.inst;
            var entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.BeginDrag;
            entry.callback.AddListener(delegate (BaseEventData eventData)
            {
                var pointerEventData = (PointerEventData)eventData;
                editorManager.SelectionBoxImage.gameObject.SetActive(true);
                editorManager.DragStartPos = pointerEventData.position * editorManager.ScreenScaleInverse;
                editorManager.SelectionRect = default(Rect);
            });
            return entry;
        }

        public static EventTrigger.Entry DragTrigger()
        {
            var editorManager = EditorManager.inst;
            var entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.Drag;
            entry.callback.AddListener(delegate (BaseEventData eventData)
            {
                var vector = ((PointerEventData)eventData).position * editorManager.ScreenScaleInverse;

                editorManager.SelectionRect.xMin = vector.x < editorManager.DragStartPos.x ? vector.x : editorManager.DragStartPos.x;
                editorManager.SelectionRect.xMax = vector.x < editorManager.DragStartPos.x ? editorManager.DragStartPos.x : vector.x;

                editorManager.SelectionRect.yMin = vector.y < editorManager.DragStartPos.y ? vector.y : editorManager.DragStartPos.y;
                editorManager.SelectionRect.yMax = vector.y < editorManager.DragStartPos.y ? editorManager.DragStartPos.y : vector.y;

                editorManager.SelectionBoxImage.rectTransform.offsetMin = editorManager.SelectionRect.min;
                editorManager.SelectionBoxImage.rectTransform.offsetMax = editorManager.SelectionRect.max;
            });
            return entry;
        }

        public static EventTrigger.Entry EndDragTrigger()
        {
            var editorManager = EditorManager.inst;

            var entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.EndDrag;
            entry.callback.AddListener(delegate (BaseEventData eventData)
            {
                EditorManager.inst.DragEndPos = ((PointerEventData)eventData).position;
                EditorManager.inst.SelectionBoxImage.gameObject.SetActive(false);
                if (RTEditor.inst.layerType == RTEditor.LayerType.Objects)
                    RTEditor.inst.StartCoroutine(ObjectEditor.inst.GroupSelectObjects(Input.GetKey(KeyCode.LeftShift)));
                else
                    RTEventEditor.inst.StartCoroutine(RTEventEditor.inst.GroupSelectKeyframes(Input.GetKey(KeyCode.LeftShift)));
            });
            return entry;
        }

        #endregion

        #region Keyframes

        public static EventTrigger.Entry CreateKeyframeStartDragTrigger(BeatmapObject beatmapObject, TimelineObject timelineObject)
        {
            var entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.BeginDrag;
            entry.callback.AddListener(delegate (BaseEventData eventData)
            {
                if (timelineObject.Index == 0)
                {
                    EditorManager.inst.DisplayNotification("Can't change time of first Keyframe", 2f, EditorManager.NotificationType.Warning);
                    return;
                }

                ObjEditor.inst.currentKeyframeKind = timelineObject.Type;
                ObjEditor.inst.currentKeyframe = timelineObject.Index;

                var list = beatmapObject.timelineObject.InternalSelections;
                if (list.FindIndex(x => x.Type == timelineObject.Type && x.Index == timelineObject.Index) != -1)
                {
                    foreach (var otherTLO in beatmapObject.timelineObject.InternalSelections)
                    {
                        otherTLO.timeOffset = otherTLO.Type == ObjEditor.inst.currentKeyframeKind && otherTLO.Index == ObjEditor.inst.currentKeyframe ? 0f : otherTLO.Time - timelineObject.Time;
                    }
                }
                ObjEditor.inst.mouseOffsetXForKeyframeDrag = timelineObject.Time - ObjectEditor.MouseTimelineCalc();
                ObjEditor.inst.timelineKeyframesDrag = true;
            });
            return entry;
        }

        public static EventTrigger.Entry CreateKeyframeEndDragTrigger(BeatmapObject beatmapObject, TimelineObject timelineObject)
        {
            var entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.EndDrag;
            entry.callback.AddListener(delegate (BaseEventData eventData)
            {
                ObjectEditor.inst.UpdateKeyframeOrder(beatmapObject);

                ObjectEditor.inst.RenderTimelineObject(ObjectEditor.inst.GetTimelineObject(beatmapObject));

                ObjectEditor.inst.RenderKeyframes(beatmapObject);
                ObjectEditor.inst.ResizeKeyframeTimeline(beatmapObject);
                ObjectEditor.inst.RenderObjectKeyframesDialog(beatmapObject);
                ObjEditor.inst.timelineKeyframesDrag = false;
            });
            return entry;
        }

        public static EventTrigger.Entry CreateKeyframeSelectTrigger(BeatmapObject beatmapObject, TimelineObject timelineObject)
        {
            var entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerDown;
            entry.callback.AddListener(delegate (BaseEventData eventData)
            {
                if ((eventData as PointerEventData).button == PointerEventData.InputButton.Middle)
                    AudioManager.inst.SetMusicTime(beatmapObject.StartTime + timelineObject.Time);
            });
            return entry;
        }

        #endregion

        #region Objects

        public static EventTrigger.Entry CreateBeatmapObjectStartDragTrigger(TimelineObject timelineObject)
        {
            var entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.BeginDrag;
            entry.callback.AddListener(delegate (BaseEventData eventData)
            {
                int bin = timelineObject.Bin;

                foreach (var otherTLO in RTEditor.inst.timelineObjects)
                {
                    otherTLO.timeOffset = otherTLO.Time - timelineObject.Time;
                    otherTLO.binOffset = otherTLO.Bin - bin;
                }

                timelineObject.timeOffset = 0f;
                timelineObject.binOffset = 0;

                float timelineTime = EditorManager.inst.GetTimelineTime();
                int num = 14 - Mathf.RoundToInt((Input.mousePosition.y - 25f) * EditorManager.inst.ScreenScaleInverse / 20f);
                ObjEditor.inst.mouseOffsetXForDrag = timelineObject.Time - timelineTime;
                ObjEditor.inst.mouseOffsetYForDrag = bin - num;
                ObjEditor.inst.beatmapObjectsDrag = true;
            });
            return entry;
        }

        public static EventTrigger.Entry CreateBeatmapObjectEndDragTrigger(TimelineObject timelineObject)
        {
            var entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.EndDrag;
            entry.callback.AddListener(delegate (BaseEventData eventData)
            {
                ObjEditor.inst.beatmapObjectsDrag = false;

                foreach (var timelineObject in ObjectEditor.inst.SelectedObjects)
                {
                    ObjectEditor.inst.RenderTimelineObject(timelineObject);
                    if (ObjectEditor.UpdateObjects)
                    {
                        if (timelineObject.IsBeatmapObject)
                            Updater.UpdateProcessor(timelineObject.GetData<BeatmapObject>(), "Start Time");
                        if (timelineObject.IsPrefabObject)
                            Updater.UpdatePrefab(timelineObject.GetData<PrefabObject>(), "Start Time");
                    }
                }

                if (RTEditor.inst.TimelineBeatmapObjects.Count == 1 && timelineObject.IsBeatmapObject)
                    RTEditor.inst.StartCoroutine(ObjectEditor.RefreshObjectGUI(timelineObject.Data as BeatmapObject));
            });
            return entry;
        }

        public static EventTrigger.Entry CreateBeatmapObjectTrigger(TimelineObject timelineObject)
        {
            var entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerUp;
            entry.callback.AddListener(delegate (BaseEventData eventData)
            {
                var pointerEventData = (PointerEventData)eventData;
                if (!ObjEditor.inst.beatmapObjectsDrag)
                {
                    CoreHelper.Log($"Selecting [ {timelineObject.ID} ]");

                    if (!RTEditor.inst.parentPickerEnabled && !RTEditor.inst.prefabPickerEnabled)
                    {
                        if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
                            ObjectEditor.inst.AddSelectedObject(timelineObject);
                        else
                            ObjectEditor.inst.SetCurrentObject(timelineObject);

                        float timelineTime = EditorManager.inst.GetTimelineTime();
                        ObjEditor.inst.mouseOffsetXForDrag = timelineObject.Time - timelineTime;
                        return;
                    }

                    if (RTEditor.inst.prefabPickerEnabled && timelineObject.IsBeatmapObject && pointerEventData.button != PointerEventData.InputButton.Right)
                    {
                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                        if (string.IsNullOrEmpty(beatmapObject.prefabInstanceID))
                        {
                            EditorManager.inst.DisplayNotification("Object is not assigned to a prefab!", 2f, EditorManager.NotificationType.Error);
                            return;
                        }

                        if (RTEditor.inst.selectingMultiple)
                        {
                            foreach (var otherTimelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                var otherBeatmapObject = otherTimelineObject.GetData<BeatmapObject>();

                                otherBeatmapObject.prefabID = beatmapObject.prefabID;
                                otherBeatmapObject.prefabInstanceID = beatmapObject.prefabInstanceID;
                                ObjectEditor.inst.RenderTimelineObject(otherTimelineObject);
                            }
                        }
                        else if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                        {
                            var currentBeatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();

                            currentBeatmapObject.prefabID = beatmapObject.prefabID;
                            currentBeatmapObject.prefabInstanceID = beatmapObject.prefabInstanceID;
                            ObjectEditor.inst.RenderTimelineObject(ObjectEditor.inst.CurrentSelection);
                            ObjectEditor.inst.OpenDialog(currentBeatmapObject);
                        }

                        RTEditor.inst.prefabPickerEnabled = false;

                        return;
                    }

                    if (RTEditor.inst.prefabPickerEnabled && timelineObject.IsPrefabObject && pointerEventData.button != PointerEventData.InputButton.Right)
                    {
                        var prefabObject = timelineObject.GetData<PrefabObject>();
                        var prefabInstanceID = LSText.randomString(16);

                        if (RTEditor.inst.selectingMultiple)
                        {
                            foreach (var otherTimelineObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject))
                            {
                                var otherBeatmapObject = otherTimelineObject.GetData<BeatmapObject>();

                                otherBeatmapObject.prefabID = prefabObject.prefabID;
                                otherBeatmapObject.prefabInstanceID = prefabInstanceID;
                                ObjectEditor.inst.RenderTimelineObject(otherTimelineObject);
                            }
                        }
                        else if (ObjectEditor.inst.CurrentSelection.IsBeatmapObject)
                        {
                            var currentBeatmapObject = ObjectEditor.inst.CurrentSelection.GetData<BeatmapObject>();

                            currentBeatmapObject.prefabID = prefabObject.prefabID;
                            currentBeatmapObject.prefabInstanceID = prefabInstanceID;
                            ObjectEditor.inst.RenderTimelineObject(ObjectEditor.inst.CurrentSelection);
                            ObjectEditor.inst.OpenDialog(currentBeatmapObject);
                        }

                        RTEditor.inst.prefabPickerEnabled = false;

                        return;
                    }

                    if (RTEditor.inst.parentPickerEnabled && timelineObject.IsBeatmapObject && pointerEventData.button != PointerEventData.InputButton.Right)
                    {
                        if (RTEditor.inst.selectingMultiple)
                        {
                            bool success = false;
                            foreach (var otherTimelineObject in ObjectEditor.inst.SelectedObjects)
                            {
                                if (otherTimelineObject.IsPrefabObject)
                                {
                                    var prefabObject = otherTimelineObject.GetData<PrefabObject>();
                                    prefabObject.parent = timelineObject.ID;
                                    Updater.UpdatePrefab(prefabObject);
                                    PrefabEditorManager.inst.RenderPrefabObjectDialog(prefabObject);

                                    success = true;
                                    continue;
                                }
                                success = SetParent(otherTimelineObject, timelineObject);
                            }

                            if (!success)
                                EditorManager.inst.DisplayNotification("Cannot set parent to child / self!", 1f, EditorManager.NotificationType.Warning);
                            else
                                RTEditor.inst.parentPickerEnabled = false;

                            return;
                        }

                        if (ObjectEditor.inst.CurrentSelection.IsPrefabObject)
                        {
                            var prefabObject = ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>();
                            prefabObject.parent = timelineObject.ID;
                            Updater.UpdatePrefab(prefabObject);
                            PrefabEditorManager.inst.RenderPrefabObjectDialog(prefabObject);
                            RTEditor.inst.parentPickerEnabled = false;

                            return;
                        }

                        var tryParent = SetParent(ObjectEditor.inst.CurrentSelection, timelineObject);

                        if (!tryParent)
                            EditorManager.inst.DisplayNotification("Cannot set parent to child / self!", 1f, EditorManager.NotificationType.Warning);
                        else
                            RTEditor.inst.parentPickerEnabled = false;
                    }
                }
            });
            return entry;
        }

        public static bool SetParent(TimelineObject currentSelection, TimelineObject timelineObjectToParentTo)
        {
            var dictionary = new Dictionary<string, bool>();

            foreach (var obj in DataManager.inst.gameData.beatmapObjects)
            {
                bool flag = true;
                if (!string.IsNullOrEmpty(obj.parent))
                {
                    string parentID = currentSelection.ID;
                    while (!string.IsNullOrEmpty(parentID))
                    {
                        if (parentID == obj.parent)
                        {
                            flag = false;
                            break;
                        }
                        int num2 = DataManager.inst.gameData.beatmapObjects.FindIndex(x => x.parent == parentID);
                        if (num2 != -1)
                        {
                            parentID = DataManager.inst.gameData.beatmapObjects[num2].id;
                        }
                        else
                        {
                            parentID = null;
                        }
                    }
                }
                if (!dictionary.ContainsKey(obj.id))
                    dictionary.Add(obj.id, flag);
            }

            if (dictionary.ContainsKey(currentSelection.ID))
                dictionary[currentSelection.ID] = false;

            if (dictionary.ContainsKey(timelineObjectToParentTo.ID) && dictionary[timelineObjectToParentTo.ID])
            {
                currentSelection.GetData<BeatmapObject>().parent = timelineObjectToParentTo.ID;
                Updater.UpdateProcessor(currentSelection.GetData<BeatmapObject>());

                RTEditor.inst.StartCoroutine(ObjectEditor.RefreshObjectGUI(currentSelection.GetData<BeatmapObject>()));
            }

            return dictionary.ContainsKey(timelineObjectToParentTo.ID) && dictionary[timelineObjectToParentTo.ID];
        }

        #endregion

        #region Events

        public static EventTrigger.Entry CreateEventObjectTrigger(TimelineObject kf)
        {
            var entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener(delegate (BaseEventData eventData)
            {
                if (!EventEditor.inst.eventDrag && (eventData as PointerEventData).button != PointerEventData.InputButton.Middle)
                    (InputDataManager.inst.editorActions.MultiSelect.IsPressed ? (Action<int, int>)EventEditor.inst.AddedSelectedEvent : EventEditor.inst.SetCurrentEvent)(kf.Type, kf.Index);
            });
            return entry;
        }

        public static EventTrigger.Entry CreateEventEndDragTrigger()
        {
            var eventEndDragTrigger = new EventTrigger.Entry();
            eventEndDragTrigger.eventID = EventTriggerType.EndDrag;
            eventEndDragTrigger.callback.AddListener(eventData =>
            {
                EventEditor.inst.eventDrag = false;
                EventEditor.inst.UpdateEventOrder();
                EventManager.inst.updateEvents();
            });
            return eventEndDragTrigger;
        }

        public static EventTrigger.Entry CreateEventStartDragTrigger(TimelineObject kf)
        {
            var startDragTrigger = new EventTrigger.Entry();
            startDragTrigger.eventID = EventTriggerType.BeginDrag;
            startDragTrigger.callback.AddListener(eventData =>
            {
                if (kf.Index != 0)
                {
                    if (RTEventEditor.inst.SelectedKeyframes.FindIndex(x => x.Type == kf.Type && x.Index == kf.Index) != -1)
                    {
                        foreach (var timelineObject in RTEventEditor.inst.SelectedKeyframes)
                        {
                            timelineObject.timeOffset = timelineObject.Type == kf.Type && timelineObject.Index == kf.Index ? 0f :
                            timelineObject.Time - DataManager.inst.gameData.eventObjects.allEvents[kf.Type][kf.Index].eventTime;
                        }
                    }
                    else
                        EventEditor.inst.SetCurrentEvent(kf.Type, kf.Index);

                    float timelineTime = EditorManager.inst.GetTimelineTime();
                    EventEditor.inst.mouseOffsetXForDrag = DataManager.inst.gameData.eventObjects.allEvents[kf.Type][kf.Index].eventTime - timelineTime;
                    EventEditor.inst.eventDrag = true;
                }
                else
                    EditorManager.inst.DisplayNotification("Can't change time of first Event", 2f, EditorManager.NotificationType.Warning);
            });
            return startDragTrigger;
        }

        public static EventTrigger.Entry CreateEventSelectTrigger(TimelineObject timelineObject)
        {
            var entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerDown;
            entry.callback.AddListener(delegate (BaseEventData eventData)
            {
                if ((eventData as PointerEventData).button == PointerEventData.InputButton.Middle)
                {
                    AudioManager.inst.SetMusicTime(timelineObject.Time);
                }
            });
            return entry;
        }

        #endregion

        #region Themes

        public static EventTrigger.Entry CreatePreviewClickTrigger(Image _preview, Image _dropper, InputField _hex, Color _col, string popupName = "")
        {
            EventTrigger.Entry previewClickTrigger = new EventTrigger.Entry();
            previewClickTrigger.eventID = EventTriggerType.PointerClick;
            previewClickTrigger.callback.AddListener(delegate (BaseEventData eventData)
            {
                EditorManager.inst.ShowDialog("Color Picker");
                if (!string.IsNullOrEmpty(popupName))
                {
                    EditorManager.inst.HideDialog(popupName);
                }

                var colorPickerTF = EditorManager.inst.GetDialog("Color Picker").Dialog.Find("content/Color Picker");
                var colorPicker = colorPickerTF.GetComponent<ColorPicker>();

                colorPicker.SwitchCurrentColor(_col);

                var save = colorPickerTF.Find("info/hex/save").GetComponent<Button>();

                save.onClick.RemoveAllListeners();
                save.onClick.AddListener(delegate ()
                {
                    EditorManager.inst.ClearPopups();
                    if (!string.IsNullOrEmpty(popupName))
                        EditorManager.inst.ShowDialog(popupName);

                    double saturation;
                    double num;
                    LSColors.ColorToHSV(colorPicker.currentColor, out double _, out saturation, out num);
                    _hex.text = colorPicker.currentHex;
                    _preview.color = colorPicker.currentColor;

                    if (_dropper == null)
                        return;

                    _dropper.color = CoreHelper.InvertColorHue(CoreHelper.InvertColorValue(colorPicker.currentColor));
                });
            });
            return previewClickTrigger;
        }

        #endregion
    }
}
