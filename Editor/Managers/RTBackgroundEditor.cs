using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Popups;

namespace BetterLegacy.Editor.Managers
{
    public class RTBackgroundEditor : MonoBehaviour
    {
        public static RTBackgroundEditor inst;

        public BackgroundObject CurrentSelectedBG => EditorTimeline.inst.CurrentSelection.GetData<BackgroundObject>();

        public List<BackgroundObject> copiedBackgroundObjects = new List<BackgroundObject>();

        public BackgroundObject backgroundObjCopy;

        public static void Init() => EditorManager.inst.transform.parent.Find("BackgroundEditor").gameObject.AddComponent<RTBackgroundEditor>();

        public GameObject shapeButtonCopy;

        public BackgroundEditorDialog Dialog { get; set; }

        void Awake()
        {
            inst = this;

            try
            {
                Dialog = new BackgroundEditorDialog();
                Dialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        public void CreateNewBackground()
        {
            var backgroundObject = new BackgroundObject
            {
                name = "Background",
                pos = Vector2.zero,
                scale = new Vector2(2f, 2f),
                color = 1,
                StartTime = AudioManager.inst.CurrentAudioSource.time,
            };

            GameData.Current.backgroundObjects.Add(backgroundObject);

            RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
            SetCurrentBackground(backgroundObject);
        }

        public void CopyBackground() => CopyBackground(CurrentSelectedBG);

        public void CopyBackground(BackgroundObject backgroundObject)
        {
            CoreHelper.Log($"Copied Background Object");
            backgroundObjCopy = backgroundObject.Copy();
            BackgroundEditor.inst.hasCopiedObject = true;
        }

        public void PasteBackgrounds()
        {
            if (copiedBackgroundObjects == null || copiedBackgroundObjects.Count < 1)
                return;

            var overwrite = EditorConfig.Instance.PasteBackgroundObjectsOverwrites.Value;
            if (overwrite)
            {
                GameData.Current.backgroundObjects.ForLoopReverse((backgroundObject, index) =>
                {
                    if (backgroundObject.fromPrefab)
                        return;

                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject, false, false);
                    GameData.Current.backgroundObjects.RemoveAt(index);
                });
            }

            for (int i = 0; i < copiedBackgroundObjects.Count; i++)
                GameData.Current.backgroundObjects.Add(copiedBackgroundObjects[i].Copy());

            SetCurrentBackground(GameData.Current.backgroundObjects.Count > 0 ? GameData.Current.backgroundObjects[0] : null);

            RTLevel.Current?.UpdateBackgroundObjects();
            EditorManager.inst.DisplayNotification($"Pasted all copied Background Objects into level{(overwrite ? " and cleared the original list" : "")}.", 2f, EditorManager.NotificationType.Success);
        }

        public void PasteBackground()
        {
            if (!BackgroundEditor.inst.hasCopiedObject || backgroundObjCopy == null)
            {
                EditorManager.inst.DisplayNotification("No copied background yet!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var backgroundObject = backgroundObjCopy.Copy();
            GameData.Current.backgroundObjects.Add(backgroundObject);

            RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
            SetCurrentBackground(backgroundObject);
        }

        public void DeleteBackground() => DeleteBackground(CurrentSelectedBG);
        
        public void DeleteBackground(BackgroundObject backgroundObject)
        {
            if (!backgroundObject)
            {
                RenderDialog();
                return;
            }

            EditorTimeline.inst.DeleteObject(backgroundObject.timelineObject);
        }

        public void SetCurrentBackground(BackgroundObject backgroundObject)
        {
            if (backgroundObject)
                EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
            else
                OpenDialog(null);
        }

        public void CreateBackgrounds(int amount)
        {
            amount = Mathf.Clamp(amount, 0, 1000);

            for (int i = 0; i < amount; i++)
            {
                var backgroundObject = new BackgroundObject();
                backgroundObject.name = "bg - " + i;

                float num = UnityEngine.Random.Range(2, 6);
                backgroundObject.scale = UnityEngine.Random.value > 0.5f ? new Vector2((float)UnityEngine.Random.Range(2, 8), (float)UnityEngine.Random.Range(2, 8)) : new Vector2(num, num);

                backgroundObject.pos = new Vector2((float)UnityEngine.Random.Range(-48, 48), (float)UnityEngine.Random.Range(-32, 32));
                backgroundObject.color = UnityEngine.Random.Range(1, 6);
                backgroundObject.depth = UnityEngine.Random.Range(0, 6);

                if (UnityEngine.Random.value > 0.5f)
                {
                    backgroundObject.reactiveType = (BackgroundObject.ReactiveType)UnityEngine.Random.Range(1, 5);

                    backgroundObject.reactiveScale = UnityEngine.Random.Range(0.01f, 0.04f);
                }

                if (backgroundObject.reactiveType == BackgroundObject.ReactiveType.Custom)
                {
                    backgroundObject.reactivePosIntensity = new Vector2(UnityEngine.Random.Range(0, 100) > 65 ? UnityEngine.Random.Range(0f, 1f) : 0f, UnityEngine.Random.Range(0, 100) > 65 ? UnityEngine.Random.Range(0f, 1f) : 0f);
                    backgroundObject.reactiveScaIntensity = new Vector2(UnityEngine.Random.Range(0, 100) > 45 ? UnityEngine.Random.Range(0f, 1f) : 0f, UnityEngine.Random.Range(0, 100) > 45 ? UnityEngine.Random.Range(0f, 1f) : 0f);
                    backgroundObject.reactiveRotIntensity = UnityEngine.Random.Range(0, 100) > 45 ? UnityEngine.Random.Range(0f, 1f) : 0f;
                    backgroundObject.reactiveCol = UnityEngine.Random.Range(1, 6);
                }

                backgroundObject.shape = UnityEngine.Random.Range(0, ShapeManager.inst.Shapes3D.Count);
                backgroundObject.shapeOption = UnityEngine.Random.Range(0, ShapeManager.inst.Shapes3D[backgroundObject.shape].Count);

                backgroundObject.editorData.Bin = EditorTimeline.inst.CalculateMaxBin(i);

                GameData.Current.backgroundObjects.Add(backgroundObject);
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
            }

            RTLevel.Current?.UpdateBackgroundObjects();
            UpdateBackgroundList();
        }

        public void DeleteAllBackgrounds()
        {
            var count = GameData.Current.backgroundObjects.Count;
            GameData.Current.backgroundObjects.ForLoopReverse(backgroundObject =>
            {
                RTLevel.Current?.UpdateBackgroundObject(backgroundObject, false, false);
                EditorTimeline.inst.DeleteObject(EditorTimeline.inst.GetTimelineObject(backgroundObject), false, false);
            });
            RTLevel.Current?.backgroundEngine?.spawner?.RecalculateObjectStates();
            GameData.Current.backgroundObjects.Clear();
            UpdateBackgroundList();
            SetCurrentBackground(null);

            EditorManager.inst.DisplayNotification($"Deleted {count} backgrounds!", 2f, EditorManager.NotificationType.Success);
        }

        #region Dialog

        public void OpenDialog() => OpenDialog(null);

        public void OpenDialog(BackgroundObject backgroundObject)
        {
            Dialog.Open();
            RenderDialog(backgroundObject);
        }

        public void RenderDialog() => RenderDialog(null);

        public void RenderDialog(BackgroundObject backgroundObject)
        {
            Dialog.LeftContent.gameObject.SetActive(backgroundObject);

            if (!backgroundObject)
            {
                UpdateBackgroundList();
                return;
            }

            #region  Base

            RenderActive(backgroundObject);
            RenderName(backgroundObject);
            RenderTags(backgroundObject);

            RenderStartTime(backgroundObject);
            RenderAutokill(backgroundObject);

            RenderDepth(backgroundObject);
            RenderIterations(backgroundObject);
            RenderPosition(backgroundObject);
            RenderScale(backgroundObject);
            RenderZPosition(backgroundObject);
            RenderZScale(backgroundObject);
            RenderRotation(backgroundObject);
            Render3DRotation(backgroundObject);
            RenderShape(backgroundObject);
            RenderFade(backgroundObject);
            RenderLayers(backgroundObject);
            RenderBin(backgroundObject);
            RenderIndex(backgroundObject);
            RenderPrefabReference(backgroundObject);

            #endregion

            #region Reactive

            RenderReactiveRanges(backgroundObject);
            RenderReactive(backgroundObject);
            RenderReactivePosition(backgroundObject);
            RenderReactiveScale(backgroundObject);
            RenderReactiveRotation(backgroundObject);
            RenderReactiveColor(backgroundObject);
            RenderReactiveZPosition(backgroundObject);

            #endregion

            #region Colors

            RenderColor(backgroundObject);
            RenderFadeColor(backgroundObject);

            #endregion

            StartCoroutine(RenderModifiers(backgroundObject));

            UpdateBackgroundList();
        }

        #region Base

        public void RenderActive(BackgroundObject backgroundObject)
        {
            Dialog.ActiveToggle.onValueChanged.ClearAll();
            Dialog.ActiveToggle.isOn = backgroundObject.active;
            Dialog.ActiveToggle.onValueChanged.AddListener(_val =>
            {
                backgroundObject.active = _val;
                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
            });
        }

        public void RenderName(BackgroundObject backgroundObject)
        {
            Dialog.NameField.onValueChanged.ClearAll();
            Dialog.NameField.text = backgroundObject.name;
            Dialog.NameField.onValueChanged.AddListener(_val =>
            {
                backgroundObject.name = _val;
            });
        }

        public void RenderTags(BackgroundObject backgroundObject)
        {
            LSHelpers.DeleteChildren(Dialog.TagsContent);

            int num = 0;
            foreach (var tag in backgroundObject.tags)
            {
                int index = num;
                var gameObject = EditorPrefabHolder.Instance.Tag.Duplicate(Dialog.TagsContent, index.ToString());
                gameObject.transform.localScale = Vector3.one;
                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.onValueChanged.ClearAll();
                input.text = tag;
                input.onValueChanged.AddListener(_val => { backgroundObject.tags[index] = _val; });

                var inputFieldSwapper = gameObject.AddComponent<InputFieldSwapper>();
                inputFieldSwapper.Init(input, InputFieldSwapper.Type.String);

                var deleteStorage = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.ClearAll();
                deleteStorage.button.onClick.AddListener(() =>
                {
                    backgroundObject.tags.RemoveAt(index);
                    RenderTags(backgroundObject);
                });

                EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.Input_Field, true);

                EditorThemeManager.ApplyInputField(input);

                EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);

                num++;
            }

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(Dialog.TagsContent, "Add");
            add.transform.localScale = Vector3.one;
            var addText = add.transform.Find("Text").GetComponent<Text>();
            addText.text = "Add Tag";
            var addButton = add.GetComponent<Button>();
            addButton.onClick.ClearAll();
            addButton.onClick.AddListener(() =>
            {
                backgroundObject.tags.Add("New Tag");
                RenderTags(backgroundObject);
            });

            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text, true);
        }

        public void RenderStartTime(BackgroundObject backgroundObject)
        {
            var startTimeField = Dialog.StartTimeField;

            startTimeField.lockToggle.onValueChanged.ClearAll();
            startTimeField.lockToggle.isOn = backgroundObject.editorData.locked;
            startTimeField.lockToggle.onValueChanged.AddListener(_val =>
            {
                backgroundObject.editorData.locked = _val;

                // Since locking has no effect on the physical object, we will only need to update the timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
            });

            startTimeField.inputField.onValueChanged.ClearAll();
            startTimeField.inputField.text = backgroundObject.StartTime.ToString();
            startTimeField.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    if (EditorConfig.Instance.ClampedTimelineDrag.Value)
                        num = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                    backgroundObject.StartTime = num;

                    // StartTime affects both physical object and timeline object.
                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                }
            });

            TriggerHelper.AddEventTriggers(Dialog.StartTimeField.gameObject, TriggerHelper.ScrollDelta(startTimeField.inputField, max: AudioManager.inst.CurrentAudioSource.clip.length));

            startTimeField.leftGreaterButton.interactable = (backgroundObject.StartTime > 0f);
            startTimeField.leftGreaterButton.onClick.NewListener(() =>
            {
                float moveTime = backgroundObject.StartTime - 1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                startTimeField.inputField.text = moveTime.ToString();

                // StartTime affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
            });
            startTimeField.leftButton.interactable = (backgroundObject.StartTime > 0f);
            startTimeField.leftButton.onClick.NewListener(() =>
            {
                float moveTime = backgroundObject.StartTime - 0.1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                startTimeField.inputField.text = moveTime.ToString();

                // StartTime affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
            });
            startTimeField.middleButton.onClick.NewListener(() =>
            {
                startTimeField.inputField.text = EditorManager.inst.CurrentAudioPos.ToString();

                // StartTime affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
            });
            startTimeField.rightButton.onClick.NewListener(() =>
            {
                float moveTime = backgroundObject.StartTime + 0.1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                startTimeField.inputField.text = moveTime.ToString();

                // StartTime affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
            });
            startTimeField.rightGreaterButton.onClick.NewListener(() =>
            {
                float moveTime = backgroundObject.StartTime + 1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                startTimeField.inputField.text = moveTime.ToString();

                // StartTime affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
            });
        }

        public void RenderAutokill(BackgroundObject backgroundObject)
        {
            Dialog.AutokillDropdown.onValueChanged.ClearAll();
            Dialog.AutokillDropdown.value = (int)backgroundObject.autoKillType;
            Dialog.AutokillDropdown.onValueChanged.AddListener(_val =>
            {
                backgroundObject.autoKillType = (AutoKillType)_val;
                // AutoKillType affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                RenderAutokill(backgroundObject);
            });

            if (backgroundObject.autoKillType == AutoKillType.FixedTime ||
                backgroundObject.autoKillType == AutoKillType.SongTime ||
                backgroundObject.autoKillType == AutoKillType.LastKeyframeOffset)
            {
                Dialog.AutokillField.gameObject.SetActive(true);

                Dialog.AutokillField.onValueChanged.ClearAll();
                Dialog.AutokillField.text = backgroundObject.autoKillOffset.ToString();
                Dialog.AutokillField.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        if (backgroundObject.autoKillType == AutoKillType.SongTime)
                        {
                            float startTime = backgroundObject.StartTime;
                            if (num < startTime)
                                num = startTime + 0.1f;
                        }

                        if (num < 0f)
                            num = 0f;

                        backgroundObject.autoKillOffset = num;

                        // AutoKillType affects both physical object and timeline object.
                        EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
                        RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                    }
                });

                Dialog.AutokillSetButton.gameObject.SetActive(true);
                Dialog.AutokillSetButton.onClick.ClearAll();
                Dialog.AutokillSetButton.onClick.AddListener(() =>
                {
                    float num = 0f;

                    if (backgroundObject.autoKillType == AutoKillType.SongTime)
                        num = AudioManager.inst.CurrentAudioSource.time;
                    else num = AudioManager.inst.CurrentAudioSource.time - backgroundObject.StartTime;

                    if (num < 0f)
                        num = 0f;

                    Dialog.AutokillField.text = num.ToString();
                });

                // Add Scrolling for easy changing of values.
                TriggerHelper.AddEventTriggers(Dialog.AutokillField.gameObject, TriggerHelper.ScrollDelta(Dialog.AutokillField, 0.1f, 10f, 0f, float.PositiveInfinity));
            }
            else
            {
                Dialog.AutokillField.gameObject.SetActive(false);
                Dialog.AutokillField.onValueChanged.ClearAll();
                Dialog.AutokillSetButton.gameObject.SetActive(false);
                Dialog.AutokillSetButton.onClick.ClearAll();
            }

            Dialog.CollapseToggle.onValueChanged.ClearAll();
            Dialog.CollapseToggle.isOn = backgroundObject.editorData.collapse;
            Dialog.CollapseToggle.onValueChanged.AddListener(_val =>
            {
                backgroundObject.editorData.collapse = _val;

                // Since autokill collapse has no affect on the physical object, we will only need to update the timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
            });
        }

        public void RenderDepth(BackgroundObject backgroundObject)
        {
            Dialog.DepthField.inputField.onValueChanged.ClearAll();
            Dialog.DepthField.inputField.text = backgroundObject.depth.ToString();
            Dialog.DepthField.inputField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                {
                    backgroundObject.depth = num;
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                }
            });

            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.DepthField);
            TriggerHelper.AddEventTriggers(Dialog.DepthField.inputField.gameObject, TriggerHelper.ScrollDeltaInt(Dialog.DepthField.inputField));
        }

        public void RenderIterations(BackgroundObject backgroundObject)
        {
            Dialog.IterationsField.inputField.onValueChanged.ClearAll();
            Dialog.IterationsField.inputField.text = backgroundObject.iterations.ToString();
            Dialog.IterationsField.inputField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                {
                    backgroundObject.iterations = num;
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                }
            });

            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.IterationsField);
            TriggerHelper.AddEventTriggers(Dialog.IterationsField.inputField.gameObject, TriggerHelper.ScrollDeltaInt(Dialog.IterationsField.inputField));
        }

        public void RenderPosition(BackgroundObject backgroundObject)
        {
            Dialog.PositionFields.x.inputField.onValueChanged.ClearAll();
            Dialog.PositionFields.x.inputField.text = backgroundObject.pos.x.ToString();
            Dialog.PositionFields.x.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.pos.x = num;
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                }
            });

            Dialog.PositionFields.y.inputField.onValueChanged.ClearAll();
            Dialog.PositionFields.y.inputField.text = backgroundObject.pos.y.ToString();
            Dialog.PositionFields.y.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.pos.y = num;
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.PositionFields.x);
            TriggerHelper.IncreaseDecreaseButtons(Dialog.PositionFields.y);

            TriggerHelper.AddEventTriggers(Dialog.PositionFields.x.inputField.gameObject,
                TriggerHelper.ScrollDelta(Dialog.PositionFields.x.inputField, multi: true),
                TriggerHelper.ScrollDeltaVector2(Dialog.PositionFields.x.inputField, Dialog.PositionFields.y.inputField, 0.1f, 10f));
            TriggerHelper.AddEventTriggers(Dialog.PositionFields.y.inputField.gameObject,
                TriggerHelper.ScrollDelta(Dialog.PositionFields.y.inputField, multi: true),
                TriggerHelper.ScrollDeltaVector2(Dialog.PositionFields.x.inputField, Dialog.PositionFields.y.inputField, 0.1f, 10f));
        }
        
        public void RenderScale(BackgroundObject backgroundObject)
        {
            Dialog.ScaleFields.x.inputField.onValueChanged.ClearAll();
            Dialog.ScaleFields.x.inputField.text = backgroundObject.scale.x.ToString();
            Dialog.ScaleFields.x.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.scale.x = num;
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                }
            });

            Dialog.ScaleFields.y.inputField.onValueChanged.ClearAll();
            Dialog.ScaleFields.y.inputField.text = backgroundObject.scale.y.ToString();
            Dialog.ScaleFields.y.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.scale.y = num;
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.ScaleFields.x);
            TriggerHelper.IncreaseDecreaseButtons(Dialog.ScaleFields.y);

            TriggerHelper.AddEventTriggers(Dialog.ScaleFields.x.inputField.gameObject,
                TriggerHelper.ScrollDelta(Dialog.ScaleFields.x.inputField, multi: true),
                TriggerHelper.ScrollDeltaVector2(Dialog.ScaleFields.x.inputField, Dialog.ScaleFields.y.inputField, 0.1f, 10f));
            TriggerHelper.AddEventTriggers(Dialog.ScaleFields.y.inputField.gameObject,
                TriggerHelper.ScrollDelta(Dialog.ScaleFields.y.inputField, multi: true),
                TriggerHelper.ScrollDeltaVector2(Dialog.ScaleFields.x.inputField, Dialog.ScaleFields.y.inputField, 0.1f, 10f));
        }

        public void RenderZPosition(BackgroundObject backgroundObject)
        {
            Dialog.ZPositionField.inputField.onValueChanged.ClearAll();
            Dialog.ZPositionField.inputField.text = backgroundObject.zposition.ToString();
            Dialog.ZPositionField.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.zposition = num;
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.ZPositionField);
            TriggerHelper.AddEventTriggers(Dialog.ZPositionField.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.ZPositionField.inputField));
        }

        public void RenderZScale(BackgroundObject backgroundObject)
        {
            Dialog.ZScaleField.inputField.onValueChanged.ClearAll();
            Dialog.ZScaleField.inputField.text = backgroundObject.zscale.ToString();
            Dialog.ZScaleField.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.zscale = float.Parse(_val);
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.ZScaleField);
            TriggerHelper.AddEventTriggers(Dialog.ZScaleField.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.ZScaleField.inputField));
        }

        public void RenderRotation(BackgroundObject backgroundObject)
        {
            Dialog.RotationField.onValueChanged.ClearAll();
            Dialog.RotationField.text = backgroundObject.rot.ToString();
            Dialog.RotationField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.rot = num;
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                    RenderRotation(backgroundObject);
                }
            });

            Dialog.RotationSlider.onValueChanged.ClearAll();
            Dialog.RotationSlider.maxValue = 360f;
            Dialog.RotationSlider.minValue = -360f;
            Dialog.RotationSlider.value = backgroundObject.rot;
            Dialog.RotationSlider.onValueChanged.AddListener(_val =>
            {
                backgroundObject.rot = _val;
                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                RenderRotation(backgroundObject);
            });
        }

        public void Render3DRotation(BackgroundObject backgroundObject)
        {
            Dialog.DepthRotation.x.inputField.onValueChanged.ClearAll();
            Dialog.DepthRotation.x.inputField.text = backgroundObject.rotation.x.ToString();
            Dialog.DepthRotation.x.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.rotation.x = num;
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                }
            });

            Dialog.DepthRotation.y.inputField.onValueChanged.ClearAll();
            Dialog.DepthRotation.y.inputField.text = backgroundObject.rotation.y.ToString();
            Dialog.DepthRotation.y.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    CurrentSelectedBG.rotation.y = num;
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.DepthRotation.x, 15f, 3f);
            TriggerHelper.IncreaseDecreaseButtons(Dialog.DepthRotation.y, 15f, 3f);

            TriggerHelper.AddEventTriggers(Dialog.DepthRotation.x.inputField.gameObject,
                TriggerHelper.ScrollDelta(Dialog.DepthRotation.x.inputField, multi: true),
                TriggerHelper.ScrollDeltaVector2(Dialog.DepthRotation.x.inputField, Dialog.DepthRotation.y.inputField, 0.1f, 10f));
            TriggerHelper.AddEventTriggers(Dialog.DepthRotation.y.inputField.gameObject,
                TriggerHelper.ScrollDelta(Dialog.DepthRotation.y.inputField, multi: true),
                TriggerHelper.ScrollDeltaVector2(Dialog.DepthRotation.x.inputField, Dialog.DepthRotation.y.inputField, 0.1f, 10f));
        }

        public void RenderShape(BackgroundObject backgroundObject)
        {
            var shape = Dialog.LeftContent.Find("shape");
            var shapeSettings = Dialog.LeftContent.Find("shapesettings");

            shape.GetComponent<GridLayoutGroup>().spacing = new Vector2(7.6f, 0f);

            DestroyImmediate(shape.GetComponent<ToggleGroup>());

            var toDestroy = new List<GameObject>();

            for (int i = 0; i < shape.childCount; i++)
                toDestroy.Add(shape.GetChild(i).gameObject);

            for (int i = 0; i < shapeSettings.childCount; i++)
            {
                if (i != 4 && i != 6)
                    for (int j = 0; j < shapeSettings.GetChild(i).childCount; j++)
                        toDestroy.Add(shapeSettings.GetChild(i).GetChild(j).gameObject);
            }

            foreach (var obj in toDestroy)
                DestroyImmediate(obj);

            toDestroy = null;

            // Re-add everything
            for (int i = 0; i < ShapeManager.inst.Shapes3D.Count; i++)
            {
                var st = (ShapeType)i;

                var obj = ObjectEditor.inst.shapeButtonPrefab.Duplicate(shape, (i + 1).ToString(), i);
                if (obj.transform.Find("Image") && obj.transform.Find("Image").gameObject.TryGetComponent(out Image image))
                {
                    image.sprite = ShapeManager.inst.Shapes3D[i].icon;
                    EditorThemeManager.ApplyGraphic(image, ThemeGroup.Toggle_1_Check);
                }

                var shapeToggle = obj.GetComponent<Toggle>();
                EditorThemeManager.ApplyToggle(shapeToggle, ThemeGroup.Background_1);

                shapeToggle.group = null;

                if (st != ShapeType.Text && st != ShapeType.Image && st != ShapeType.Polygon)
                {
                    if (!shapeSettings.Find((i + 1).ToString()))
                        shapeSettings.Find("6").gameObject.Duplicate(shapeSettings, (i + 1).ToString());

                    var so = shapeSettings.Find((i + 1).ToString());

                    var rect = (RectTransform)so;
                    if (!so.GetComponent<ScrollRect>())
                    {
                        var scroll = so.gameObject.AddComponent<ScrollRect>();
                        so.gameObject.AddComponent<Mask>();
                        var ad = so.gameObject.AddComponent<Image>();

                        scroll.horizontal = true;
                        scroll.vertical = false;
                        scroll.content = rect;
                        scroll.viewport = rect;
                        ad.color = new Color(1f, 1f, 1f, 0.01f);
                    }

                    for (int j = 0; j < ShapeManager.inst.Shapes3D[i].Count; j++)
                    {
                        var opt = ObjectEditor.inst.shapeButtonPrefab.Duplicate(shapeSettings.GetChild(i), (j + 1).ToString(), j);
                        if (opt.transform.Find("Image") && opt.transform.Find("Image").gameObject.TryGetComponent(out Image image1))
                        {
                            image1.sprite = ShapeManager.inst.Shapes3D[i][j].icon;
                            EditorThemeManager.ApplyGraphic(image1, ThemeGroup.Toggle_1_Check);
                        }

                        var layoutElement = opt.AddComponent<LayoutElement>();
                        layoutElement.layoutPriority = 1;
                        layoutElement.minWidth = 32f;

                        ((RectTransform)opt.transform).sizeDelta = new Vector2(32f, 32f);

                        var shapeOptionToggle = opt.GetComponent<Toggle>();
                        shapeOptionToggle.group = null;
                        EditorThemeManager.ApplyToggle(shapeOptionToggle, checkGroup: ThemeGroup.Background_1);

                        if (!opt.GetComponent<HoverUI>())
                        {
                            var he = opt.AddComponent<HoverUI>();
                            he.animatePos = false;
                            he.animateSca = true;
                            he.size = 1.1f;
                        }
                    }

                    ObjectEditor.inst.LastGameObject(shapeSettings.GetChild(i));
                }
            }

            LSHelpers.SetActiveChildren(shapeSettings, false);

            if (backgroundObject.shape >= shapeSettings.childCount)
            {
                Debug.Log($"{BackgroundEditor.inst.className}Somehow, the object ended up being at a higher shape than normal.");
                backgroundObject.SetShape(shapeSettings.childCount - 1 - 1, 0);

                RenderDialog(backgroundObject);
                return;
            }

            if (backgroundObject.shape == 4)
            {
                // Make the text larger for better readability.
                shapeSettings.transform.AsRT().sizeDelta = new Vector2(351f, 74f);
                var child = shapeSettings.GetChild(4);
                child.AsRT().sizeDelta = new Vector2(351f, 74f);
                child.Find("Text").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
                child.Find("Placeholder").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
                child.GetComponent<InputField>().lineType = InputField.LineType.MultiLineNewline;
            }
            else
            {
                shapeSettings.AsRT().sizeDelta = new Vector2(351f, 32f);
                shapeSettings.GetChild(4).AsRT().sizeDelta = new Vector2(351f, 32f);
            }

            shapeSettings.GetChild(backgroundObject.shape).gameObject.SetActive(true);
            for (int i = 1; i <= ShapeManager.inst.Shapes3D.Count; i++)
            {
                int buttonTmp = i - 1;

                if (shape.Find(i.ToString()))
                {
                    var shoggle = shape.Find(i.ToString()).GetComponent<Toggle>();
                    shoggle.onValueChanged.ClearAll();
                    shoggle.isOn = backgroundObject.shape == buttonTmp;
                    shoggle.onValueChanged.AddListener(_val =>
                    {
                        if (!_val)
                            return;

                        backgroundObject.SetShape(buttonTmp, 0);

                        RenderDialog(backgroundObject);
                    });

                    if (!shape.Find(i.ToString()).GetComponent<HoverUI>())
                    {
                        var hoverUI = shape.Find(i.ToString()).gameObject.AddComponent<HoverUI>();
                        hoverUI.animatePos = false;
                        hoverUI.animateSca = true;
                        hoverUI.size = 1.1f;
                    }
                }
            }

            if (backgroundObject.shape == 4 || backgroundObject.shape == 6 || backgroundObject.shape == 9)
            {
                EditorManager.inst.DisplayNotification($"{(backgroundObject.shape == 4 ? "Text" : "Image")} background not supported.", 2f, EditorManager.NotificationType.Error);
                backgroundObject.SetShape(0, 0);
                return;
            }

            for (int i = 0; i < shapeSettings.GetChild(backgroundObject.shape).childCount - 1; i++)
            {
                int buttonTmp = i;
                var shoggle = shapeSettings.GetChild(backgroundObject.shape).GetChild(i).GetComponent<Toggle>();

                shoggle.onValueChanged.ClearAll();
                shoggle.isOn = backgroundObject.shapeOption == i;
                shoggle.onValueChanged.AddListener(_val =>
                {
                    if (!_val)
                        return;

                    backgroundObject.SetShape(backgroundObject.shape, buttonTmp);

                    RenderDialog(backgroundObject);
                });
            }
        }

        public void RenderFade(BackgroundObject backgroundObject)
        {
            Dialog.FadeToggle.toggle.onValueChanged.ClearAll();
            Dialog.FadeToggle.toggle.isOn = backgroundObject.drawFade;
            Dialog.FadeToggle.toggle.onValueChanged.AddListener(_val =>
            {
                backgroundObject.drawFade = _val;
                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
            });
        }

        public void RenderLayers(BackgroundObject backgroundObject)
        {
            Dialog.EditorLayerField.onValueChanged.ClearAll();
            Dialog.EditorLayerField.text = (backgroundObject.editorData.Layer + 1).ToString();
            Dialog.EditorLayerField.image.color = EditorTimeline.GetLayerColor(backgroundObject.editorData.Layer);
            Dialog.EditorLayerField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                {
                    num = Mathf.Clamp(num - 1, 0, int.MaxValue);
                    backgroundObject.editorData.Layer = num;

                    // Since layers have no effect on the physical object, we will only need to update the timeline object.
                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));

                    //editorLayersImage.color = RTEditor.GetLayerColor(beatmapObject.editorData.Layer);
                    RenderLayers(backgroundObject);
                }
            });

            if (Dialog.EditorLayerField.gameObject)
                TriggerHelper.AddEventTriggers(Dialog.EditorLayerField.gameObject, TriggerHelper.ScrollDeltaInt(Dialog.EditorLayerField, 1, 1, int.MaxValue));

            var editorLayerContextMenu = Dialog.EditorLayerField.gameObject.GetOrAddComponent<ContextClickable>();
            editorLayerContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Go to Editor Layer", () => EditorTimeline.inst.SetLayer(backgroundObject.editorData.Layer, EditorTimeline.LayerType.Objects))
                    );
            };
        }

        public void RenderBin(BackgroundObject backgroundObject)
        {
            Dialog.BinSlider.onValueChanged.ClearAll();
            Dialog.BinSlider.maxValue = EditorTimeline.inst.BinCount;
            Dialog.BinSlider.value = backgroundObject.editorData.Bin;
            Dialog.BinSlider.onValueChanged.AddListener(_val =>
            {
                backgroundObject.editorData.Bin = Mathf.Clamp((int)_val, 0, EditorTimeline.inst.BinCount);

                // Since bin has no effect on the physical object, we will only need to update the timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
            });
        }

        public void RenderIndex(BackgroundObject backgroundObject)
        {
            if (!Dialog.EditorIndexField)
                return;

            Dialog.LeftContent.Find("indexer_label").gameObject.SetActive(RTEditor.ShowModdedUI);
            Dialog.EditorIndexField.gameObject.SetActive(RTEditor.ShowModdedUI);

            if (!RTEditor.ShowModdedUI)
                return;

            var currentIndex = GameData.Current.backgroundObjects.FindIndex(x => x.id == backgroundObject.id);
            Dialog.EditorIndexField.inputField.onEndEdit.ClearAll();
            Dialog.EditorIndexField.inputField.onValueChanged.ClearAll();
            Dialog.EditorIndexField.inputField.text = currentIndex.ToString();
            Dialog.EditorIndexField.inputField.onEndEdit.AddListener(_val =>
            {
                if (currentIndex < 0)
                {
                    EditorManager.inst.DisplayNotification($"Object is not in the Beatmap Object list.", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                if (int.TryParse(_val, out int index))
                {
                    index = Mathf.Clamp(index, 0, GameData.Current.backgroundObjects.Count - 1);
                    if (currentIndex == index)
                        return;

                    GameData.Current.backgroundObjects.Move(currentIndex, index);
                    RenderIndex(backgroundObject);
                    EditorTimeline.inst.UpdateTransformIndex();
                }
            });

            Dialog.EditorIndexField.leftGreaterButton.onClick.NewListener(() =>
            {
                var index = GameData.Current.backgroundObjects.FindIndex(x => x == backgroundObject);
                if (index <= 0)
                {
                    EditorManager.inst.DisplayNotification("Could not move object back since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                GameData.Current.backgroundObjects.Move(index, 0);
                EditorTimeline.inst.UpdateTransformIndex();
                RenderIndex(backgroundObject);
            });
            Dialog.EditorIndexField.leftButton.onClick.NewListener(() =>
            {
                var index = GameData.Current.backgroundObjects.FindIndex(x => x == backgroundObject);
                if (index <= 0)
                {
                    EditorManager.inst.DisplayNotification("Could not move object back since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                GameData.Current.backgroundObjects.Move(index, index - 1);
                EditorTimeline.inst.UpdateTransformIndex();
                RenderIndex(backgroundObject);
            });
            Dialog.EditorIndexField.rightButton.onClick.NewListener(() =>
            {
                var index = GameData.Current.backgroundObjects.FindIndex(x => x == backgroundObject);
                if (index >= GameData.Current.backgroundObjects.Count - 1)
                {
                    EditorManager.inst.DisplayNotification("Could not move object forwards since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                GameData.Current.backgroundObjects.Move(index, index + 1);
                EditorTimeline.inst.UpdateTransformIndex();
                RenderIndex(backgroundObject);
            });
            Dialog.EditorIndexField.rightGreaterButton.onClick.NewListener(() =>
            {
                var index = GameData.Current.backgroundObjects.FindIndex(x => x == backgroundObject);
                if (index >= GameData.Current.backgroundObjects.Count - 1)
                {
                    EditorManager.inst.DisplayNotification("Could not move object forwards since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                GameData.Current.backgroundObjects.Move(index, GameData.Current.backgroundObjects.Count - 1);
                EditorTimeline.inst.UpdateTransformIndex();
                RenderIndex(backgroundObject);
            });

            TriggerHelper.AddEventTriggers(Dialog.EditorIndexField.gameObject, TriggerHelper.CreateEntry(EventTriggerType.Scroll, eventData =>
            {
                var pointerEventData = (PointerEventData)eventData;

                if (!int.TryParse(Dialog.EditorIndexField.inputField.text, out int index))
                    return;

                if (pointerEventData.scrollDelta.y < 0f)
                    index -= (Input.GetKey(EditorConfig.Instance.ScrollwheelLargeAmountKey.Value) ? 10 : 1);
                if (pointerEventData.scrollDelta.y > 0f)
                    index += (Input.GetKey(EditorConfig.Instance.ScrollwheelLargeAmountKey.Value) ? 10 : 1);

                if (index < 0)
                {
                    EditorManager.inst.DisplayNotification("Could not move object back since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                    return;
                }
                if (index > GameData.Current.backgroundObjects.Count - 1)
                {
                    EditorManager.inst.DisplayNotification("Could not move object forwards since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                GameData.Current.backgroundObjects.Move(currentIndex, index);
                EditorTimeline.inst.UpdateTransformIndex();
                RenderIndex(backgroundObject);
            }));

            var contextMenu = Dialog.EditorIndexField.inputField.gameObject.GetOrAddComponent<ContextClickable>();
            contextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Select Previous", () =>
                    {
                        if (currentIndex <= 0)
                        {
                            EditorManager.inst.DisplayNotification($"There are no previous objects to select.", 2f, EditorManager.NotificationType.Error);
                            return;
                        }

                        var prevObject = GameData.Current.backgroundObjects[currentIndex - 1];

                        if (!prevObject)
                            return;

                        var timelineObject = EditorTimeline.inst.GetTimelineObject(prevObject);

                        if (timelineObject)
                            EditorTimeline.inst.SetCurrentObject(timelineObject, EditorConfig.Instance.BringToSelection.Value);
                    }),
                    new ButtonFunction("Select Previous", () =>
                    {
                        if (currentIndex >= GameData.Current.backgroundObjects.Count - 1)
                        {
                            EditorManager.inst.DisplayNotification($"There are no previous objects to select.", 2f, EditorManager.NotificationType.Error);
                            return;
                        }

                        var nextObject = GameData.Current.backgroundObjects[currentIndex + 1];

                        if (!nextObject)
                            return;

                        var timelineObject = EditorTimeline.inst.GetTimelineObject(nextObject);

                        if (timelineObject)
                            EditorTimeline.inst.SetCurrentObject(timelineObject, EditorConfig.Instance.BringToSelection.Value);
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Select First", () =>
                    {
                        var prevObject = GameData.Current.backgroundObjects.First();

                        if (!prevObject)
                            return;

                        var timelineObject = EditorTimeline.inst.GetTimelineObject(prevObject);

                        if (timelineObject)
                            EditorTimeline.inst.SetCurrentObject(timelineObject, EditorConfig.Instance.BringToSelection.Value);
                    }),
                    new ButtonFunction("Select Last", () =>
                    {
                        var nextObject = GameData.Current.backgroundObjects.Last();

                        if (!nextObject)
                            return;

                        var timelineObject = EditorTimeline.inst.GetTimelineObject(nextObject);

                        if (timelineObject)
                            EditorTimeline.inst.SetCurrentObject(timelineObject, EditorConfig.Instance.BringToSelection.Value);
                    }));
            };
        }

        public void RenderPrefabReference(BackgroundObject backgroundObject)
        {
            bool fromPrefab = !string.IsNullOrEmpty(backgroundObject.prefabID);
            Dialog.CollapsePrefabLabel.SetActive(fromPrefab);
            Dialog.CollapsePrefabButton.gameObject.SetActive(fromPrefab);
            Dialog.CollapsePrefabButton.button.onClick.ClearAll();

            var collapsePrefabContextMenu = Dialog.CollapsePrefabButton.button.gameObject.GetOrAddComponent<ContextClickable>();
            collapsePrefabContextMenu.onClick = null;
            collapsePrefabContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                {
                    if (EditorConfig.Instance.ShowCollapsePrefabWarning.Value)
                    {
                        RTEditor.inst.ShowWarningPopup("Are you sure you want to collapse this Prefab group and save the changes to the Internal Prefab?", () =>
                        {
                            RTPrefabEditor.inst.Collapse(backgroundObject, backgroundObject.editorData);
                            RTEditor.inst.HideWarningPopup();
                        }, RTEditor.inst.HideWarningPopup);

                        return;
                    }

                    RTPrefabEditor.inst.Collapse(backgroundObject, backgroundObject.editorData);
                    return;
                }

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Apply", () => RTPrefabEditor.inst.Collapse(backgroundObject, backgroundObject.editorData)),
                    new ButtonFunction("Create New", () => RTPrefabEditor.inst.CollapseNew(backgroundObject, backgroundObject.editorData))
                    );
            };

            Dialog.AssignPrefabButton.button.onClick.NewListener(() =>
            {
                RTEditor.inst.selectingMultiple = false;
                RTEditor.inst.prefabPickerEnabled = true;
            });

            Dialog.RemovePrefabButton.button.onClick.NewListener(() =>
            {
                backgroundObject.RemovePrefabReference();
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
                OpenDialog(backgroundObject);
            });
        }

        #endregion

        #region Reactive

        public void RenderReactiveRanges(BackgroundObject backgroundObject)
        {
            for (int i = 0; i < Dialog.ReactiveRanges.Count; i++)
            {
                var index = i;
                var toggle = Dialog.ReactiveRanges[i];
                toggle.onValueChanged.ClearAll();
                toggle.isOn = i == (int)backgroundObject.reactiveType;
                toggle.onValueChanged.AddListener(_val =>
                {
                    backgroundObject.reactiveType = (BackgroundObject.ReactiveType)index;
                    RenderDialog(backgroundObject);
                });
            }
        }

        public void RenderReactive(BackgroundObject backgroundObject)
        {
            Dialog.ReactiveIntensityField.onValueChanged.ClearAll();
            Dialog.ReactiveIntensityField.text = backgroundObject.reactiveScale.ToString();
            Dialog.ReactiveIntensityField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.reactiveScale = num;
                    RenderReactive(backgroundObject);
                }
            });

            Dialog.ReactiveIntensitySlider.onValueChanged.ClearAll();
            Dialog.ReactiveIntensitySlider.value = backgroundObject.reactiveScale;
            Dialog.ReactiveIntensitySlider.onValueChanged.AddListener(_val =>
            {
                backgroundObject.reactiveScale = _val;
                RenderReactive(backgroundObject);
            });

            Dialog.CustomReactive.ForLoop(gameObject => gameObject.SetActive(backgroundObject.reactiveType == BackgroundObject.ReactiveType.Custom));
        }

        public void RenderReactivePosition(BackgroundObject backgroundObject)
        {
            if (backgroundObject.reactiveType != BackgroundObject.ReactiveType.Custom)
                return;

            Dialog.ReactivePositionSamplesFields.x.inputField.onValueChanged.ClearAll();
            Dialog.ReactivePositionSamplesFields.x.inputField.text = backgroundObject.reactivePosSamples.x.ToString();
            Dialog.ReactivePositionSamplesFields.x.inputField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                {
                    backgroundObject.reactivePosSamples.x = num;
                }
            });

            Dialog.ReactivePositionSamplesFields.y.inputField.onValueChanged.ClearAll();
            Dialog.ReactivePositionSamplesFields.y.inputField.text = backgroundObject.reactivePosSamples.y.ToString();
            Dialog.ReactivePositionSamplesFields.y.inputField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                {
                    backgroundObject.reactivePosSamples.y = num;
                }
            });

            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.ReactivePositionSamplesFields.x, max: 255);
            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.ReactivePositionSamplesFields.y, max: 255);

            Dialog.ReactivePositionIntensityFields.x.inputField.onValueChanged.ClearAll();
            Dialog.ReactivePositionIntensityFields.x.inputField.text = backgroundObject.reactivePosIntensity.x.ToString();
            Dialog.ReactivePositionIntensityFields.x.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.reactivePosIntensity.x = num;
                }
            });

            Dialog.ReactivePositionIntensityFields.y.inputField.onValueChanged.ClearAll();
            Dialog.ReactivePositionIntensityFields.y.inputField.text = backgroundObject.reactivePosIntensity.y.ToString();
            Dialog.ReactivePositionIntensityFields.y.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.reactivePosIntensity.y = num;
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.ReactivePositionIntensityFields.x);
            TriggerHelper.IncreaseDecreaseButtons(Dialog.ReactivePositionIntensityFields.y);
        }

        public void RenderReactiveScale(BackgroundObject backgroundObject)
        {
            if (backgroundObject.reactiveType != BackgroundObject.ReactiveType.Custom)
                return;

            Dialog.ReactiveScaleSamplesFields.x.inputField.onValueChanged.ClearAll();
            Dialog.ReactiveScaleSamplesFields.x.inputField.text = backgroundObject.reactiveScaSamples.x.ToString();
            Dialog.ReactiveScaleSamplesFields.x.inputField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                {
                    backgroundObject.reactiveScaSamples.x = num;
                }
            });

            Dialog.ReactiveScaleSamplesFields.y.inputField.onValueChanged.ClearAll();
            Dialog.ReactiveScaleSamplesFields.y.inputField.text = backgroundObject.reactiveScaSamples.y.ToString();
            Dialog.ReactiveScaleSamplesFields.y.inputField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                {
                    backgroundObject.reactiveScaSamples.y = num;
                }
            });

            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.ReactiveScaleSamplesFields.x, max: 255);
            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.ReactiveScaleSamplesFields.y, max: 255);

            Dialog.ReactiveScaleIntensityFields.x.inputField.onValueChanged.ClearAll();
            Dialog.ReactiveScaleIntensityFields.x.inputField.text = backgroundObject.reactiveScaIntensity.x.ToString();
            Dialog.ReactiveScaleIntensityFields.x.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.reactiveScaIntensity.x = num;
                }
            });

            Dialog.ReactiveScaleIntensityFields.y.inputField.onValueChanged.ClearAll();
            Dialog.ReactiveScaleIntensityFields.y.inputField.text = backgroundObject.reactiveScaIntensity.y.ToString();
            Dialog.ReactiveScaleIntensityFields.y.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.reactiveScaIntensity.y = num;
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.ReactiveScaleIntensityFields.x);
            TriggerHelper.IncreaseDecreaseButtons(Dialog.ReactiveScaleIntensityFields.y);
        }

        public void RenderReactiveRotation(BackgroundObject backgroundObject)
        {
            if (backgroundObject.reactiveType != BackgroundObject.ReactiveType.Custom)
                return;

            Dialog.ReactiveRotationSampleField.inputField.onValueChanged.ClearAll();
            Dialog.ReactiveRotationSampleField.inputField.text = backgroundObject.reactiveRotSample.ToString();
            Dialog.ReactiveRotationSampleField.inputField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                {
                    backgroundObject.reactiveRotSample = num;
                }
            });

            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.ReactiveRotationSampleField, max: 255);

            Dialog.ReactiveRotationIntensityField.inputField.onValueChanged.ClearAll();
            Dialog.ReactiveRotationIntensityField.inputField.text = backgroundObject.reactiveRotIntensity.ToString();
            Dialog.ReactiveRotationIntensityField.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.reactiveRotIntensity = num;
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.ReactiveRotationIntensityField);
        }

        public void RenderReactiveColor(BackgroundObject backgroundObject)
        {
            if (backgroundObject.reactiveType != BackgroundObject.ReactiveType.Custom)
                return;

            LSHelpers.DeleteChildren(Dialog.ReactiveColorsParent);
            ThemeManager.inst.Current.backgroundColors.ForLoop((color, index) => SetColorToggle(backgroundObject, color, backgroundObject.reactiveCol, index, Dialog.ReactiveColorsParent, SetReactiveColor));

            Dialog.ReactiveColorSampleField.inputField.onValueChanged.ClearAll();
            Dialog.ReactiveColorSampleField.inputField.text = backgroundObject.reactiveColSample.ToString();
            Dialog.ReactiveColorSampleField.inputField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                {
                    backgroundObject.reactiveColSample = num;
                }
            });

            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.ReactiveColorSampleField, max: 255);

            Dialog.ReactiveColorIntensityField.inputField.onValueChanged.ClearAll();
            Dialog.ReactiveColorIntensityField.inputField.text = backgroundObject.reactiveColIntensity.ToString();
            Dialog.ReactiveColorIntensityField.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.reactiveColIntensity = num;
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.ReactiveColorIntensityField);
        }

        public void RenderReactiveZPosition(BackgroundObject backgroundObject)
        {
            if (backgroundObject.reactiveType != BackgroundObject.ReactiveType.Custom)
                return;

            Dialog.ReactiveZPositionSampleField.inputField.onValueChanged.ClearAll();
            Dialog.ReactiveZPositionSampleField.inputField.text = backgroundObject.reactiveZSample.ToString();
            Dialog.ReactiveZPositionSampleField.inputField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                {
                    backgroundObject.reactiveZSample = num;
                }
            });

            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.ReactiveZPositionSampleField, max: 255);

            Dialog.ReactiveZPositionIntensityField.inputField.onValueChanged.ClearAll();
            Dialog.ReactiveZPositionIntensityField.inputField.text = backgroundObject.reactiveZIntensity.ToString();
            Dialog.ReactiveZPositionIntensityField.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.reactiveZIntensity = num;
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.ReactiveZPositionIntensityField);
        }

        #endregion

        #region Colors

        public void RenderColor(BackgroundObject backgroundObject)
        {
            LSHelpers.DeleteChildren(Dialog.ColorsParent);
            ThemeManager.inst.Current.backgroundColors.ForLoop((color, index) => SetColorToggle(backgroundObject, color, backgroundObject.color, index, Dialog.ColorsParent, SetColor));

            Dialog.HueSatVal.x.inputField.onValueChanged.ClearAll();
            Dialog.HueSatVal.x.inputField.text = backgroundObject.hue.ToString();
            Dialog.HueSatVal.x.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.hue = num;
                }
            });

            Dialog.HueSatVal.y.inputField.onValueChanged.ClearAll();
            Dialog.HueSatVal.y.inputField.text = backgroundObject.saturation.ToString();
            Dialog.HueSatVal.y.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.saturation = num;
                }
            });

            Dialog.HueSatVal.z.inputField.onValueChanged.ClearAll();
            Dialog.HueSatVal.z.inputField.text = backgroundObject.value.ToString();
            Dialog.HueSatVal.z.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.value = num;
                }
            });
        }

        public void RenderFadeColor(BackgroundObject backgroundObject)
        {
            LSHelpers.DeleteChildren(Dialog.FadeColorsParent);
            ThemeManager.inst.Current.backgroundColors.ForLoop((color, index) => SetColorToggle(backgroundObject, color, backgroundObject.fadeColor, index, Dialog.FadeColorsParent, SetFadeColor));

            Dialog.FadeHueSatVal.x.inputField.onValueChanged.ClearAll();
            Dialog.FadeHueSatVal.x.inputField.text = backgroundObject.fadeHue.ToString();
            Dialog.FadeHueSatVal.x.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.fadeHue = num;
                }
            });

            Dialog.FadeHueSatVal.y.inputField.onValueChanged.ClearAll();
            Dialog.FadeHueSatVal.y.inputField.text = backgroundObject.fadeSaturation.ToString();
            Dialog.FadeHueSatVal.y.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.fadeSaturation = num;
                }
            });

            Dialog.FadeHueSatVal.z.inputField.onValueChanged.ClearAll();
            Dialog.FadeHueSatVal.z.inputField.text = backgroundObject.fadeValue.ToString();
            Dialog.FadeHueSatVal.z.inputField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.fadeValue = num;
                }
            });
        }

        #endregion

        public void SetColorToggle(BackgroundObject backgroundObject, Color color, int currentColor, int colTmp, Transform parent, Action<BackgroundObject, int> onSetColor)
        {
            var gameObject = EditorManager.inst.colorGUI.Duplicate(parent, "color gui");
            gameObject.transform.localScale = Vector3.one;
            var button = gameObject.GetComponent<Button>();
            button.image.color = LSColors.fadeColor(color, 1f);
            gameObject.transform.Find("Image").gameObject.SetActive(currentColor == colTmp);

            button.onClick.AddListener(() => onSetColor.Invoke(backgroundObject, colTmp));

            EditorThemeManager.ApplyGraphic(button.image, ThemeGroup.Null, true);
            EditorThemeManager.ApplyGraphic(gameObject.transform.Find("Image").GetComponent<Image>(), ThemeGroup.Background_1);
        }

        public void SetColor(BackgroundObject backgroundObject, int col)
        {
            backgroundObject.color = col;
            RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
            UpdateColorList(backgroundObject, "color");
        }

        public void SetFadeColor(BackgroundObject backgroundObject, int col)
        {
            backgroundObject.fadeColor = col;
            RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
            UpdateColorList(backgroundObject, "fade-color");
        }

        public void SetReactiveColor(BackgroundObject backgroundObject, int col)
        {
            backgroundObject.reactiveCol = col;
            RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
            UpdateColorList(backgroundObject, "reactive-color");
        }

        void UpdateColorList(BackgroundObject backgroundObject, string name)
        {
            var colorList = Dialog.LeftContent.Find(name);

            for (int i = 0; i < ThemeManager.inst.Current.backgroundColors.Count; i++)
                if (colorList.childCount > i)
                    colorList.GetChild(i).Find("Image").gameObject.SetActive(name switch
                    {
                        "fade-color" => i == backgroundObject.fadeColor,
                        "reactive-color" => i == backgroundObject.reactiveCol,
                        _ => i == backgroundObject.color,
                    });
        }

        public void UpdateBackgroundList()
        {
            Dialog.ClearContent();
            int num = 0;
            foreach (var backgroundObject in GameData.Current.backgroundObjects)
            {
                if (!RTString.SearchString(Dialog.SearchTerm, backgroundObject.name))
                {
                    num++;
                    continue;
                }

                int index = num;
                var gameObject = BackgroundEditor.inst.backgroundButtonPrefab.Duplicate(Dialog.Content, $"BG {index}");
                gameObject.transform.localScale = Vector3.one;

                var name = gameObject.transform.Find("name").GetComponent<Text>();
                var text = gameObject.transform.Find("pos").GetComponent<Text>();
                var image = gameObject.transform.Find("color").GetComponent<Image>();

                name.text = backgroundObject.name;
                text.text = $"({backgroundObject.pos.x}, {backgroundObject.pos.y})";

                image.color = ThemeManager.inst.Current.GetBGColor(backgroundObject.color);

                var contextClickable = gameObject.AddComponent<ContextClickable>();
                contextClickable.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                    {
                        SetCurrentBackground(backgroundObject);
                        return;
                    }

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Select", () => SetCurrentBackground(backgroundObject)),
                        new ButtonFunction("Delete", () => DeleteBackground(backgroundObject)),
                        new ButtonFunction(true),
                        new ButtonFunction("Inspect", () => ModCompatibility.Inspect(backgroundObject))
                        );
                };

                EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.List_Button_2_Normal, true);
                EditorThemeManager.ApplyGraphic(image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(name, ThemeGroup.List_Button_2_Text);
                EditorThemeManager.ApplyGraphic(text, ThemeGroup.List_Button_2_Text);

                num++;
            }
        }

        #region Modifiers

        public Transform content;
        public Transform scrollView;

        public bool showModifiers;

        public GameObject modifierCardPrefab;
        public GameObject modifierAddPrefab;

        public Toggle modifiersIgnoreToggle;
        public Toggle modifiersOrderToggle;
        public Toggle modifiersActiveToggle;

        public void CreateModifiersOnAwake()
        {
            var ignoreLifespan = EditorPrefabHolder.Instance.ToggleButton.Duplicate(Dialog.LeftContent, "ignore life");
            var ignoreLifespanToggleButton = ignoreLifespan.GetComponent<ToggleButtonStorage>();
            ignoreLifespanToggleButton.label.text = "Ignore Lifespan";

            modifiersIgnoreToggle = ignoreLifespanToggleButton.toggle;

            EditorThemeManager.AddToggle(modifiersIgnoreToggle, graphic: ignoreLifespanToggleButton.label);
            TooltipHelper.AssignTooltip(ignoreLifespan, "Modifiers Ignore Lifespan");

            var orderMatters = EditorPrefabHolder.Instance.ToggleButton.Duplicate(Dialog.LeftContent, "order");
            var orderMattersToggleButton = orderMatters.GetComponent<ToggleButtonStorage>();
            orderMattersToggleButton.label.text = "Order Matters";

            modifiersOrderToggle = orderMattersToggleButton.toggle;

            EditorThemeManager.AddToggle(modifiersOrderToggle, graphic: orderMattersToggleButton.label);
            
            var showModifiers = EditorPrefabHolder.Instance.ToggleButton.Duplicate(Dialog.LeftContent, "active");
            var showModifiersToggleButton = showModifiers.GetComponent<ToggleButtonStorage>();
            showModifiersToggleButton.label.text = "Show Modifiers";

            modifiersActiveToggle = showModifiersToggleButton.toggle;
            modifiersActiveToggle.onValueChanged.ClearAll();
            modifiersActiveToggle.isOn = this.showModifiers;
            modifiersActiveToggle.onValueChanged.AddListener(_val =>
            {
                this.showModifiers = _val;
                scrollView.gameObject.SetActive(this.showModifiers);
                if (CurrentSelectedBG)
                    StartCoroutine(RenderModifiers(CurrentSelectedBG));
            });

            EditorThemeManager.AddToggle(modifiersActiveToggle, graphic: showModifiersToggleButton.label);

            scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(Dialog.LeftContent, "Modifiers Scroll View").transform;

            content = scrollView.Find("Viewport/Content");

            scrollView.gameObject.SetActive(this.showModifiers);

            modifierCardPrefab = Creator.NewUIObject("Modifier Prefab", transform);
            modifierCardPrefab.transform.AsRT().sizeDelta = new Vector2(336f, 128f);

            var mcpImage = modifierCardPrefab.AddComponent<Image>();
            mcpImage.color = new Color(1f, 1f, 1f, 0.03f);

            var mcpVLG = modifierCardPrefab.AddComponent<VerticalLayoutGroup>();
            mcpVLG.childControlHeight = false;
            mcpVLG.childControlWidth = false;
            mcpVLG.childForceExpandWidth = false;

            var mcpCSF = modifierCardPrefab.AddComponent<ContentSizeFitter>();
            mcpCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var mcpSpacerTop = Creator.NewUIObject("Spacer Top", modifierCardPrefab.transform);
            mcpSpacerTop.transform.AsRT().sizeDelta = new Vector2(350f, 8f);

            var mcpLabel = Creator.NewUIObject("Label", modifierCardPrefab.transform);
            RectValues.LeftAnchored.AnchoredPosition(0f, -8f).SizeDelta(352f, 32f).AssignToRectTransform(mcpLabel.transform.AsRT());

            var mcpText = Creator.NewUIObject("Text", mcpLabel.transform);
            RectValues.LeftAnchored.SizeDelta(300f, 32f).AssignToRectTransform(mcpText.transform.AsRT());

            var mcpTextText = mcpText.AddComponent<Text>();
            mcpTextText.alignment = TextAnchor.MiddleLeft;
            mcpTextText.font = FontManager.inst.DefaultFont;
            mcpTextText.fontSize = 19;
            mcpTextText.color = new Color(0.9373f, 0.9216f, 0.9373f);

            var collapse = EditorPrefabHolder.Instance.CollapseToggle.Duplicate(mcpLabel.transform, "Collapse");
            collapse.transform.localScale = Vector3.one;
            var collapseLayoutElement = collapse.GetComponent<LayoutElement>() ?? collapse.AddComponent<LayoutElement>();
            collapseLayoutElement.minWidth = 32f;
            RectValues.Default.AnchoredPosition(70f, 0f).SizeDelta(32f, 32f).AssignToRectTransform(collapse.transform.AsRT());

            var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(mcpLabel.transform, "Delete");
            delete.transform.localScale = Vector3.one;
            var deleteLayoutElement = delete.GetComponent<LayoutElement>() ?? delete.AddComponent<LayoutElement>();
            deleteLayoutElement.minWidth = 32f;
            RectValues.Default.AnchoredPosition(140f, 0f).SizeDelta(32f, 32f).AssignToRectTransform(delete.transform.AsRT());

            var copy = EditorPrefabHolder.Instance.DeleteButton.Duplicate(mcpLabel.transform, "Copy");
            copy.transform.localScale = Vector3.one;
            var duplicateLayoutElement = copy.GetComponent<LayoutElement>() ?? copy.AddComponent<LayoutElement>();
            duplicateLayoutElement.minWidth = 32f;
            RectValues.Default.AnchoredPosition(106f, 0f).SizeDelta(32f, 32f).AssignToRectTransform(copy.transform.AsRT());

            copy.GetComponent<DeleteButtonStorage>().image.sprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"copy{FileFormat.PNG.Dot()}"));

            var notifier = Creator.NewUIObject("Notifier", mcpLabel.transform);
            var notifierImage = notifier.AddComponent<Image>();
            RectValues.Default.AnchoredPosition(44f, 0f).SizeDelta(8f, 32f).AssignToRectTransform(notifierImage.rectTransform);

            var mcpSpacerMid = Creator.NewUIObject("Spacer Middle", modifierCardPrefab.transform);
            mcpSpacerMid.transform.AsRT().sizeDelta = new Vector2(350f, 8f);

            var layout = Creator.NewUIObject("Layout", modifierCardPrefab.transform);

            var layoutVLG = layout.AddComponent<VerticalLayoutGroup>();
            layoutVLG.childControlHeight = false;
            layoutVLG.childForceExpandHeight = false;
            layoutVLG.spacing = 4f;

            var layoutCSF = layout.AddComponent<ContentSizeFitter>();
            layoutCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var mcpSpacerBot = Creator.NewUIObject("Spacer Bottom", modifierCardPrefab.transform);
            mcpSpacerBot.transform.AsRT().sizeDelta = new Vector2(350f, 8f);

            modifierAddPrefab = EditorManager.inst.folderButtonPrefab.Duplicate(transform, "add modifier");

            var text = modifierAddPrefab.transform.GetChild(0).GetComponent<Text>();
            text.text = "+";
            text.alignment = TextAnchor.MiddleCenter;

            booleanBar = Boolean();

            numberInput = NumberInput();

            stringInput = StringInput();

            dropdownBar = Dropdown();
        }

        public static Modifier<BackgroundObject> copiedModifier;
        public IEnumerator RenderModifiers(BackgroundObject backgroundObject)
        {
            LSHelpers.DeleteChildren(content);

            modifiersIgnoreToggle.onValueChanged.ClearAll();
            modifiersIgnoreToggle.isOn = backgroundObject.ignoreLifespan;
            modifiersIgnoreToggle.onValueChanged.AddListener(_val =>
            {
                backgroundObject.ignoreLifespan = _val;
                RTLevel.Current?.UpdateBackgroundObject(backgroundObject, RTLevel.BackgroundObjectContext.MODIFIERS);
            });

            modifiersOrderToggle.onValueChanged.ClearAll();
            modifiersOrderToggle.isOn = backgroundObject.orderModifiers;
            modifiersOrderToggle.onValueChanged.AddListener(_val =>
            {
                backgroundObject.orderModifiers = _val;
                RTLevel.Current?.UpdateBackgroundObject(backgroundObject, RTLevel.BackgroundObjectContext.MODIFIERS);
            });

            if (!showModifiers)
                yield break;

            content.parent.parent.AsRT().sizeDelta = new Vector2(351f, 500f);

            int num = 0;
            foreach (var modifier in backgroundObject.modifiers)
            {
                int index = num;
                var gameObject = modifierCardPrefab.Duplicate(content, modifier.Name);
                EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.List_Button_1_Normal, true);
                gameObject.transform.localScale = Vector3.one;
                var modifierTitle = gameObject.transform.Find("Label/Text").GetComponent<Text>();
                modifierTitle.text = modifier.Name;
                EditorThemeManager.ApplyLightText(modifierTitle);

                var collapse = gameObject.transform.Find("Label/Collapse").GetComponent<Toggle>();
                collapse.onValueChanged.ClearAll();
                collapse.isOn = modifier.collapse;
                collapse.onValueChanged.AddListener(_val =>
                {
                    modifier.collapse = _val;
                    StartCoroutine(RenderModifiers(backgroundObject));
                });

                TooltipHelper.AssignTooltip(collapse.gameObject, "Collapse Modifier");
                EditorThemeManager.ApplyToggle(collapse, ThemeGroup.List_Button_1_Normal);

                for (int i = 0; i < collapse.transform.Find("dots").childCount; i++)
                    EditorThemeManager.ApplyGraphic(collapse.transform.Find("dots").GetChild(i).GetComponent<Image>(), ThemeGroup.Dark_Text);

                var delete = gameObject.transform.Find("Label/Delete").GetComponent<DeleteButtonStorage>();
                delete.button.onClick.NewListener(() =>
                {
                    backgroundObject.modifiers.RemoveAt(index);
                    backgroundObject.positionOffset = Vector3.zero;
                    backgroundObject.scaleOffset = Vector3.zero;
                    backgroundObject.rotationOffset = Vector3.zero;

                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                    StartCoroutine(RenderModifiers(backgroundObject));
                });

                TooltipHelper.AssignTooltip(delete.gameObject, "Delete Modifier");
                EditorThemeManager.ApplyGraphic(delete.button.image, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(delete.image, ThemeGroup.Delete_Text);

                var copy = gameObject.transform.Find("Label/Copy").GetComponent<DeleteButtonStorage>();
                copy.button.onClick.NewListener(() =>
                {
                    copiedModifier = Modifier<BackgroundObject>.DeepCopy(modifier, backgroundObject);
                    StartCoroutine(RenderModifiers(backgroundObject));
                    EditorManager.inst.DisplayNotification("Copied Modifier!", 1.5f, EditorManager.NotificationType.Success);
                });

                TooltipHelper.AssignTooltip(copy.gameObject, "Copy Modifier");
                EditorThemeManager.ApplyGraphic(copy.button.image, ThemeGroup.Copy, true);
                EditorThemeManager.ApplyGraphic(copy.image, ThemeGroup.Copy_Text);

                var notifier = gameObject.AddComponent<ModifierActiveNotifier>();
                notifier.modifierBase = modifier;
                notifier.notifier = gameObject.transform.Find("Label/Notifier").gameObject.GetComponent<Image>();
                TooltipHelper.AssignTooltip(notifier.notifier.gameObject, "Notifier Modifier");
                EditorThemeManager.ApplyGraphic(notifier.notifier, ThemeGroup.Warning_Confirm, true);

                if (modifier.collapse)
                {
                    num++;
                    continue;
                }

                var layout = gameObject.transform.Find("Layout");

                var constant = booleanBar.Duplicate(layout, "Constant");
                constant.transform.localScale = Vector3.one;

                var constantText = constant.transform.Find("Text").GetComponent<Text>();
                constantText.text = "Constant";

                var constantToggle = constant.transform.Find("Toggle").GetComponent<Toggle>();
                constantToggle.onValueChanged.ClearAll();
                constantToggle.isOn = modifier.constant;
                constantToggle.onValueChanged.AddListener(_val =>
                {
                    modifier.constant = _val;
                    modifier.active = false;
                });

                TooltipHelper.AssignTooltip(constantToggle.gameObject, "Constant Modifier");
                EditorThemeManager.ApplyLightText(constantText);
                EditorThemeManager.ApplyToggle(constantToggle);

                if (modifier.type == ModifierBase.Type.Trigger)
                {
                    var not = booleanBar.Duplicate(layout, "Not");
                    not.transform.localScale = Vector3.one;
                    var notText = not.transform.Find("Text").GetComponent<Text>();
                    notText.text = "Not";

                    var notToggle = not.transform.Find("Toggle").GetComponent<Toggle>();
                    notToggle.onValueChanged.ClearAll();
                    notToggle.isOn = modifier.not;
                    notToggle.onValueChanged.AddListener(_val =>
                    {
                        modifier.not = _val;
                        modifier.active = false;
                    });

                    TooltipHelper.AssignTooltip(notToggle.gameObject, "Trigger Not Modifier");
                    EditorThemeManager.ApplyLightText(notText);
                    EditorThemeManager.ApplyToggle(notToggle);

                    var elseIf = booleanBar.Duplicate(layout, "Not");
                    elseIf.transform.localScale = Vector3.one;
                    var elseIfText = elseIf.transform.Find("Text").GetComponent<Text>();
                    elseIfText.text = "Else If";

                    var elseIfToggle = elseIf.transform.Find("Toggle").GetComponent<Toggle>();
                    elseIfToggle.onValueChanged.ClearAll();
                    elseIfToggle.isOn = modifier.elseIf;
                    elseIfToggle.onValueChanged.AddListener(_val =>
                    {
                        modifier.elseIf = _val;
                        modifier.active = false;
                    });

                    TooltipHelper.AssignTooltip(elseIfToggle.gameObject, "Trigger Else If Modifier");
                    EditorThemeManager.ApplyLightText(elseIfText);
                    EditorThemeManager.ApplyToggle(elseIfToggle);
                }

                if (!modifier.verified)
                {
                    modifier.verified = true;
                    modifier.VerifyModifier(ModifiersManager.defaultBackgroundObjectModifiers);
                }

                if (!modifier.IsValid(ModifiersManager.defaultBackgroundObjectModifiers))
                {
                    EditorManager.inst.DisplayNotification("Modifier does not have a command name and is lacking values.", 2f, EditorManager.NotificationType.Error);
                    continue;
                }

                gameObject.AddComponent<Button>();
                var modifierContextMenu = gameObject.AddComponent<ContextClickable>();
                modifierContextMenu.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    var buttonFunctions = new List<ButtonFunction>()
                    {
                        new ButtonFunction("Add", () =>
                        {
                            DefaultModifiersPopup.Open();
                            RefreshDefaultModifiersList(backgroundObject);
                        }),
                        new ButtonFunction("Add Above", () =>
                        {
                            DefaultModifiersPopup.Open();
                            RefreshDefaultModifiersList(backgroundObject, index);
                        }),
                        new ButtonFunction("Add Below", () =>
                        {
                            DefaultModifiersPopup.Open();
                            RefreshDefaultModifiersList(backgroundObject, index + 1);
                        }),
                        new ButtonFunction("Delete", () =>
                        {
                            backgroundObject.modifiers.RemoveAt(index);
                            backgroundObject.positionOffset = Vector3.zero;
                            backgroundObject.scaleOffset = Vector3.zero;
                            backgroundObject.rotationOffset = Vector3.zero;

                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject);

                            StartCoroutine(RenderModifiers(backgroundObject));
                        }),
                        new ButtonFunction(true),
                        new ButtonFunction("Copy", () =>
                        {
                            copiedModifier = Modifier<BackgroundObject>.DeepCopy(modifier, backgroundObject);
                            StartCoroutine(RenderModifiers(backgroundObject));
                            EditorManager.inst.DisplayNotification("Copied Modifier!", 1.5f, EditorManager.NotificationType.Success);
                        }),
                        new ButtonFunction("Paste", () =>
                        {
                            if (copiedModifier == null)
                                return;

                            backgroundObject.modifiers.Add(Modifier<BackgroundObject>.DeepCopy(copiedModifier, backgroundObject));
                            StartCoroutine(RenderModifiers(backgroundObject));
                            EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
                        }),
                        new ButtonFunction("Paste Above", () =>
                        {
                            if (copiedModifier == null)
                                return;

                            backgroundObject.modifiers.Insert(index, Modifier<BackgroundObject>.DeepCopy(copiedModifier, backgroundObject));
                            StartCoroutine(RenderModifiers(backgroundObject));
                            EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
                        }),
                        new ButtonFunction("Paste Below", () =>
                        {
                            if (copiedModifier == null)
                                return;

                            backgroundObject.modifiers.Insert(index + 1, Modifier<BackgroundObject>.DeepCopy(copiedModifier, backgroundObject));
                            StartCoroutine(RenderModifiers(backgroundObject));
                            EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
                        }),
                        new ButtonFunction(true),
                        new ButtonFunction("Sort Modifiers", () =>
                        {
                            backgroundObject.modifiers = backgroundObject.modifiers.OrderBy(x => x.type == ModifierBase.Type.Action).ToList();
                            StartCoroutine(RenderModifiers(backgroundObject));
                        }),
                        new ButtonFunction("Move Up", () =>
                        {
                            if (index <= 0)
                            {
                                EditorManager.inst.DisplayNotification("Could not move modifier up since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                                return;
                            }

                            backgroundObject.modifiers.Move(index, index - 1);
                            StartCoroutine(RenderModifiers(backgroundObject));
                        }),
                        new ButtonFunction("Move Down", () =>
                        {
                            if (index >= backgroundObject.modifiers.Count - 1)
                            {
                                EditorManager.inst.DisplayNotification("Could not move modifier up since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                                return;
                            }

                            backgroundObject.modifiers.Move(index, index + 1);
                            StartCoroutine(RenderModifiers(backgroundObject));
                        }),
                        new ButtonFunction("Move to Start", () =>
                        {
                            backgroundObject.modifiers.Move(index, 0);
                            StartCoroutine(RenderModifiers(backgroundObject));
                        }),
                        new ButtonFunction("Move to End", () =>
                        {
                            backgroundObject.modifiers.Move(index, backgroundObject.modifiers.Count - 1);
                            StartCoroutine(RenderModifiers(backgroundObject));
                        }),
                        new ButtonFunction(true),
                        new ButtonFunction("Update Modifier", () =>
                        {
                            modifier.active = false;
                            modifier.Inactive?.Invoke(modifier);
                        })
                    };
                    if (ModCompatibility.UnityExplorerInstalled)
                        buttonFunctions.Add(new ButtonFunction("Inspect", () => ModCompatibility.Inspect(modifier)));

                    EditorContextMenu.inst.ShowContextMenu(buttonFunctions);
                };

                var cmd = modifier.Name;
                switch (cmd)
                {
                    case "setActive":
                        {
                            ObjectModifiersEditor.inst.BoolGenerator(modifier, layout, "Active", 0, false);

                            break;
                        }
                    case "setActiveOther":
                        {
                            ObjectModifiersEditor.inst.BoolGenerator(modifier, layout, "Active", 0, false);
                            ObjectModifiersEditor.inst.StringGenerator(modifier, layout, "BG Group", 1);

                            break;
                        }
                    case "timeLesserEquals":
                    case "timeGreaterEquals":
                    case "timeLesser":
                    case "timeGreater":
                        {
                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "Time", 0, 0f);

                            break;
                        }
                    case "animateObject":
                    case "animateObjectOther":
                        {
                            if (cmd.Contains("Other"))
                                ObjectModifiersEditor.inst.StringGenerator(modifier, layout, "BG Group", 7);

                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "Time", 0, 1f);
                            ObjectModifiersEditor.inst.DropdownGenerator(modifier, layout, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "X", 2, 0f);
                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "Y", 3, 0f);
                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "Z", 4, 0f);
                            ObjectModifiersEditor.inst.BoolGenerator(modifier, layout, "Relative", 5, true);
                            ObjectModifiersEditor.inst.DropdownGenerator(modifier, layout, "Easing", 6, EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList());

                            break;
                        }
                    case "copyAxis":
                        {
                            ObjectModifiersEditor.inst.StringGenerator(modifier, layout, "Object Group", 0);
                            ObjectModifiersEditor.inst.DropdownGenerator(modifier, layout, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                            ObjectModifiersEditor.inst.DropdownGenerator(modifier, layout, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));
                            ObjectModifiersEditor.inst.DropdownGenerator(modifier, layout, "To Type", 3, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                            ObjectModifiersEditor.inst.DropdownGenerator(modifier, layout, "To Axis (3D)", 4, CoreHelper.StringToOptionData("X", "Y", "Z"));

                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "Delay", 5, 0f);
                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "Multiply", 6, 1f);
                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "Offset", 7, 0f);
                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "Min", 8, -99999f);
                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "Max", 9, 99999f);
                            ObjectModifiersEditor.inst.SingleGenerator(modifier, layout, "Loop", 10, 99999f);

                            break;
                        }
                }

                num++;
            }

            //Add Modifier
            {
                var gameObject = modifierAddPrefab.Duplicate(content, "add modifier");
                TooltipHelper.AssignTooltip(gameObject, "Add Modifier");

                var button = gameObject.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(() =>
                {
                    DefaultModifiersPopup.Open();
                    RefreshDefaultModifiersList(backgroundObject);
                });

                EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(gameObject.transform.GetChild(0).GetComponent<Text>());
            }

            // Paste Modifier
            PasteGenerator(backgroundObject);
            LayoutRebuilder.ForceRebuildLayoutImmediate(content.AsRT());

            yield break;
        }

        public void SetObjectColors(Toggle[] toggles, int index, int i, Modifier<BackgroundObject> modifier)
        {
            modifier.commands[index] = i.ToString();

            int num = 0;
            foreach (var toggle in toggles)
            {
                int toggleIndex = num;
                toggle.onValueChanged.ClearAll();
                toggle.isOn = num == i;
                toggle.onValueChanged.AddListener(_val => SetObjectColors(toggles, index, toggleIndex, modifier));

                toggle.GetComponent<Image>().color = ThemeManager.inst.Current.GetObjColor(toggleIndex);

                if (!toggle.GetComponent<HoverUI>())
                {
                    var hoverUI = toggle.gameObject.AddComponent<HoverUI>();
                    hoverUI.animatePos = false;
                    hoverUI.animateSca = true;
                    hoverUI.size = 1.1f;
                }
                num++;
            }
        }

        GameObject pasteModifier;
        public void PasteGenerator(BackgroundObject backgroundObject)
        {
            if (copiedModifier == null)
                return;

            if (pasteModifier)
                CoreHelper.Destroy(pasteModifier);

            pasteModifier = EditorPrefabHolder.Instance.Function1Button.Duplicate(content, "paste modifier");
            pasteModifier.transform.AsRT().sizeDelta = new Vector2(350f, 32f);
            var buttonStorage = pasteModifier.GetComponent<FunctionButtonStorage>();
            buttonStorage.label.text = "Paste";
            buttonStorage.button.onClick.NewListener(() =>
            {
                backgroundObject.modifiers.Add(Modifier<BackgroundObject>.DeepCopy(copiedModifier, backgroundObject));
                StartCoroutine(RenderModifiers(backgroundObject));
                EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
                RTLevel.Current?.UpdateBackgroundObject(backgroundObject, RTLevel.BackgroundObjectContext.MODIFIERS);
            });

            TooltipHelper.AssignTooltip(pasteModifier, "Paste Modifier");
            EditorThemeManager.ApplyGraphic(buttonStorage.button.image, ThemeGroup.Paste, true);
            EditorThemeManager.ApplyGraphic(buttonStorage.label, ThemeGroup.Paste_Text);
        }

        #endregion

        #region Default Modifiers

        public ContentPopup DefaultModifiersPopup { get; set; }

        public string searchTerm;
        public int addIndex = -1;
        public void RefreshDefaultModifiersList(BackgroundObject backgroundObject, int addIndex = -1)
        {
            this.addIndex = addIndex;
            var defaultModifiers = ModifiersManager.defaultBackgroundObjectModifiers;

            DefaultModifiersPopup.ClearContent();

            for (int i = 0; i < defaultModifiers.Count; i++)
            {
                var defaultModifier = defaultModifiers[i];
                if (!ObjectModifiersEditor.inst.SearchModifier(searchTerm, defaultModifier))
                    continue;

                var name = $"{defaultModifier.Name} ({defaultModifier.type})";

                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(DefaultModifiersPopup.Content, name);

                var modifierName = gameObject.transform.GetChild(0).GetComponent<Text>();
                modifierName.text = name;

                var button = gameObject.GetComponent<Button>();
                button.onClick.NewListener(() =>
                {
                    var modifier = Modifier<BackgroundObject>.DeepCopy(defaultModifier, backgroundObject);
                    if (addIndex == -1)
                        backgroundObject.modifiers.Add(modifier);
                    else
                        backgroundObject.modifiers.Insert(Mathf.Clamp(addIndex, 0, backgroundObject.modifiers.Count), modifier);
                    StartCoroutine(RenderModifiers(backgroundObject));
                    DefaultModifiersPopup.Close();
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject, RTLevel.BackgroundObjectContext.MODIFIERS);
                });

                EditorThemeManager.ApplyLightText(modifierName);
                EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
            }
        }

        #endregion

        #region UI Part Handlers

        GameObject booleanBar;

        GameObject numberInput;

        GameObject stringInput;

        GameObject dropdownBar;

        GameObject Base(string name)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(transform);
            gameObject.transform.localScale = Vector3.one;

            var rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0f, 32f);

            var horizontalLayoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.childControlWidth = false;
            horizontalLayoutGroup.childForceExpandWidth = false;
            horizontalLayoutGroup.spacing = 8f;

            var text = new GameObject("Text");
            text.transform.SetParent(rectTransform);
            text.transform.localScale = Vector3.one;
            var textRT = text.AddComponent<RectTransform>();
            textRT.anchoredPosition = new Vector2(10f, -5f);
            textRT.anchorMax = Vector2.one;
            textRT.anchorMin = Vector2.zero;
            textRT.pivot = new Vector2(0f, 1f);
            textRT.sizeDelta = new Vector2(247f, 32f);

            var textText = text.AddComponent<Text>();
            textText.alignment = TextAnchor.MiddleLeft;
            textText.font = FontManager.inst.DefaultFont;
            textText.fontSize = 19;
            textText.color = new Color(0.9373f, 0.9216f, 0.9373f);

            return gameObject;
        }

        GameObject Boolean()
        {
            var gameObject = Base("Bool");
            var rectTransform = (RectTransform)gameObject.transform;

            ((RectTransform)rectTransform.Find("Text")).sizeDelta = new Vector2(266f, 32f);

            var toggleBase = new GameObject("Toggle");
            toggleBase.transform.SetParent(rectTransform);
            toggleBase.transform.localScale = Vector3.one;

            var toggleBaseRT = toggleBase.AddComponent<RectTransform>();

            toggleBaseRT.anchorMax = Vector2.one;
            toggleBaseRT.anchorMin = Vector2.zero;
            toggleBaseRT.sizeDelta = new Vector2(32f, 32f);

            var toggle = toggleBase.AddComponent<Toggle>();

            var background = new GameObject("Background");
            background.transform.SetParent(toggleBaseRT);
            background.transform.localScale = Vector3.one;

            var backgroundRT = background.AddComponent<RectTransform>();
            backgroundRT.anchoredPosition = Vector3.zero;
            backgroundRT.anchorMax = new Vector2(0f, 1f);
            backgroundRT.anchorMin = new Vector2(0f, 1f);
            backgroundRT.pivot = new Vector2(0f, 1f);
            backgroundRT.sizeDelta = new Vector2(32f, 32f);
            var backgroundImage = background.AddComponent<Image>();

            var checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(backgroundRT);
            checkmark.transform.localScale = Vector3.one;

            var checkmarkRT = checkmark.AddComponent<RectTransform>();
            checkmarkRT.anchoredPosition = Vector3.zero;
            checkmarkRT.anchorMax = new Vector2(0.5f, 0.5f);
            checkmarkRT.anchorMin = new Vector2(0.5f, 0.5f);
            checkmarkRT.pivot = new Vector2(0.5f, 0.5f);
            checkmarkRT.sizeDelta = new Vector2(20f, 20f);
            var checkmarkImage = checkmark.AddComponent<Image>();
            checkmarkImage.sprite = SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_checkmark.png");
            checkmarkImage.color = new Color(0.1294f, 0.1294f, 0.1294f);

            toggle.image = backgroundImage;
            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkmarkImage;

            return gameObject;
        }

        GameObject NumberInput()
        {
            var gameObject = Base("Number");
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.localScale = Vector2.one;

            ((RectTransform)rectTransform.Find("Text")).sizeDelta = new Vector2(146f, 32f);

            var input = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(rectTransform, "Input");
            input.transform.localScale = Vector2.one;
            ((RectTransform)input.transform.Find("Text")).sizeDelta = Vector2.zero;

            var buttonL = Button("<", SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_left_small.png"));
            buttonL.transform.SetParent(rectTransform);
            buttonL.transform.localScale = Vector3.one;

            ((RectTransform)buttonL.transform).sizeDelta = new Vector2(16f, 32f);

            var buttonR = Button(">", SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/editor_gui_right_small.png"));
            buttonR.transform.SetParent(rectTransform);
            buttonR.transform.localScale = Vector3.one;

            ((RectTransform)buttonR.transform).sizeDelta = new Vector2(16f, 32f);

            return gameObject;
        }

        GameObject StringInput()
        {
            var gameObject = Base("String");
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.localScale = Vector2.one;

            ((RectTransform)rectTransform.Find("Text")).sizeDelta = new Vector2(146f, 32f);

            var input = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(rectTransform, "Input");
            input.transform.localScale = Vector2.one;
            ((RectTransform)input.transform).sizeDelta = new Vector2(152f, 32f);
            ((RectTransform)input.transform.Find("Text")).sizeDelta = Vector2.zero;

            return gameObject;
        }

        GameObject Dropdown()
        {
            var gameObject = Base("Dropdown");
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.localScale = Vector2.one;

            ((RectTransform)rectTransform.Find("Text")).sizeDelta = new Vector2(146f, 32f);

            var dropdownInput = EditorPrefabHolder.Instance.CurvesDropdown.Duplicate(rectTransform, "Dropdown");
            dropdownInput.transform.localScale = Vector2.one;

            return gameObject;
        }

        GameObject Button(string name, Sprite sprite)
        {
            var gameObject = new GameObject(name);
            var rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.localScale = Vector2.one;

            var image = gameObject.AddComponent<Image>();
            image.color = new Color(0.8784f, 0.8784f, 0.8784f);
            image.sprite = sprite;

            var button = gameObject.AddComponent<Button>();
            button.colors = UIManager.SetColorBlock(button.colors, Color.white, new Color(0.898f, 0.451f, 0.451f, 1f), Color.white, Color.white, Color.red);

            return gameObject;
        }

        #endregion

        #endregion
    }
}
