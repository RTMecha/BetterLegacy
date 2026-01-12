using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Managers.Settings;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Manages editing <see cref="BackgroundObject"/>s.
    /// <br></br>Wraps <see cref="BackgroundEditor"/>.
    /// </summary>
    public class RTBackgroundEditor : BaseEditor<RTBackgroundEditor, RTBackgroundEditorSettings, BackgroundEditor>
    {
        #region Values

        public override BackgroundEditor BaseInstance { get => BackgroundEditor.inst; set => BackgroundEditor.inst = value; }

        /// <summary>
        /// Dialog of the editor.
        /// </summary>
        public BackgroundEditorDialog Dialog { get; set; }

        /// <summary>
        /// List of copied background objects.
        /// </summary>
        public List<BackgroundObject> copiedBackgroundObjects = new List<BackgroundObject>();

        /// <summary>
        /// The copied background object.
        /// </summary>
        public BackgroundObject backgroundObjCopy;

        #endregion

        #region Functions

        public override void OnInit()
        {
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

        public override void OnTick() => Dialog?.ModifiersDialog?.Tick();

        /// <summary>
        /// Creates a new <see cref="BackgroundObject"/>.
        /// </summary>
        public void CreateNewBackground() => CreateNewBackground(AudioManager.inst.CurrentAudioSource.time);

        /// <summary>
        /// Creates a new <see cref="BackgroundObject"/>.
        /// </summary>
        /// <param name="time">Time of the new object.</param>
        public void CreateNewBackground(float time)
        {
            if (RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsCreated.Value && EditorConfig.Instance.BPMSnapsObjects.Value)
                time = RTEditor.SnapToBPM(time);

            var backgroundObject = new BackgroundObject
            {
                name = "Background",
                pos = Vector2.zero,
                scale = new Vector2(2f, 2f),
                color = 1,
                StartTime = time,
            };
            backgroundObject.editorData.Layer = EditorTimeline.inst.Layer;
            backgroundObject.orderModifiers = EditorConfig.Instance.CreateObjectModifierOrderDefault.Value;

            GameData.Current.backgroundObjects.Add(backgroundObject);

            RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
            SetCurrentBackground(backgroundObject);
        }

        /// <summary>
        /// Copies the currently selected <see cref="BackgroundObject"/>.
        /// </summary>
        public void CopyBackground() => CopyBackground(EditorTimeline.inst.CurrentSelection.GetData<BackgroundObject>());

        /// <summary>
        /// Copies a <see cref="BackgroundObject"/>.
        /// </summary>
        /// <param name="backgroundObject">Background object to copy.</param>
        public void CopyBackground(BackgroundObject backgroundObject)
        {
            CoreHelper.Log($"Copied Background Object");
            backgroundObjCopy = backgroundObject.Copy();
            BackgroundEditor.inst.hasCopiedObject = true;
        }

        /// <summary>
        /// Pastes the copied <see cref="BackgroundObject"/>s.
        /// </summary>
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
            {
                var backgroundObject = copiedBackgroundObjects[i].Copy();
                backgroundObject.editorData.Layer = EditorTimeline.inst.Layer;
                GameData.Current.backgroundObjects.Add(backgroundObject);
            }

            SetCurrentBackground(GameData.Current.backgroundObjects.Count > 0 ? GameData.Current.backgroundObjects[0] : null);

            RTLevel.Current?.UpdateBackgroundObjects();
            EditorManager.inst.DisplayNotification($"Pasted all copied Background Objects into level{(overwrite ? " and cleared the original list" : string.Empty)}.", 2f, EditorManager.NotificationType.Success);
        }

        /// <summary>
        /// Pastes the copied <see cref="BackgroundObject"/>.
        /// </summary>
        public void PasteBackground()
        {
            if (!BackgroundEditor.inst.hasCopiedObject || backgroundObjCopy == null)
            {
                EditorManager.inst.DisplayNotification("No copied background yet!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var backgroundObject = backgroundObjCopy.Copy();
            backgroundObject.editorData.Layer = EditorTimeline.inst.Layer;
            GameData.Current.backgroundObjects.Add(backgroundObject);

            RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
            EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
            SetCurrentBackground(backgroundObject);
        }

        /// <summary>
        /// Deletes the currently selected <see cref="BackgroundObject"/>.
        /// </summary>
        public void DeleteBackground() => DeleteBackground(EditorTimeline.inst.CurrentSelection.GetData<BackgroundObject>());
        
        /// <summary>
        /// Deletes a <see cref="BackgroundObject"/>.
        /// </summary>
        /// <param name="backgroundObject">Background object to delete.</param>
        public void DeleteBackground(BackgroundObject backgroundObject)
        {
            if (!backgroundObject)
            {
                RenderDialog();
                return;
            }

            EditorTimeline.inst.DeleteObject(backgroundObject.timelineObject);
        }

        /// <summary>
        /// Sets the currently selected <see cref="BackgroundObject"/>.
        /// </summary>
        /// <param name="backgroundObject">Background object to set.</param>
        public void SetCurrentBackground(BackgroundObject backgroundObject)
        {
            if (backgroundObject)
                EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
            else
                OpenDialog(null);
        }

        /// <summary>
        /// Creates an amount of <see cref="BackgroundObject"/>s.
        /// </summary>
        /// <param name="amount">Amount to create.</param>
        public void CreateBackgrounds(int amount)
        {
            amount = Mathf.Clamp(amount, 0, 1000);

            for (int i = 0; i < amount; i++)
            {
                var backgroundObject = new BackgroundObject();
                backgroundObject.name = "bg - " + i;

                float num = UnityRandom.Range(2f, 6f);
                backgroundObject.scale = UnityRandom.value > 0.5f ? new Vector2(UnityRandom.Range(2f, 8f), UnityRandom.Range(2f, 8f)) : new Vector2(num, num);

                backgroundObject.pos = new Vector2(UnityRandom.Range(-48f, 48f), UnityRandom.Range(-32f, 32f));
                backgroundObject.color = UnityRandom.Range(1, 6);
                backgroundObject.depth = UnityRandom.Range(0, 6);

                if (UnityRandom.value > 0.5f)
                {
                    backgroundObject.reactiveType = (BackgroundObject.ReactiveType)UnityRandom.Range(1, 5);

                    backgroundObject.reactiveScale = UnityRandom.Range(0.01f, 0.04f);
                }

                if (backgroundObject.reactiveType == BackgroundObject.ReactiveType.Custom)
                {
                    backgroundObject.reactivePosIntensity = new Vector2(UnityRandom.Range(0, 100) > 65 ? UnityRandom.Range(0f, 1f) : 0f, UnityRandom.Range(0, 100) > 65 ? UnityRandom.Range(0f, 1f) : 0f);
                    backgroundObject.reactiveScaIntensity = new Vector2(UnityRandom.Range(0, 100) > 45 ? UnityRandom.Range(0f, 1f) : 0f, UnityRandom.Range(0, 100) > 45 ? UnityRandom.Range(0f, 1f) : 0f);
                    backgroundObject.reactiveRotIntensity = UnityRandom.Range(0, 100) > 45 ? UnityRandom.Range(0f, 1f) : 0f;
                    backgroundObject.reactiveCol = UnityRandom.Range(1, 6);
                }

                backgroundObject.shape = UnityRandom.Range(0, ShapeManager.inst.Shapes3D.Count);
                backgroundObject.shapeOption = UnityRandom.Range(0, ShapeManager.inst.Shapes3D[backgroundObject.shape].Count);

                backgroundObject.editorData.Layer = EditorTimeline.inst.Layer;
                backgroundObject.editorData.Bin = EditorTimeline.inst.CalculateMaxBin(i);

                GameData.Current.backgroundObjects.Add(backgroundObject);
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
            }

            RTLevel.Current?.UpdateBackgroundObjects();
            UpdateBackgroundList();
        }

        /// <summary>
        /// Removes all <see cref="BackgroundObject"/>s from the level.
        /// </summary>
        public void DeleteAllBackgrounds()
        {
            var count = GameData.Current.backgroundObjects.Count;
            GameData.Current.backgroundObjects.ForLoopReverse(backgroundObject =>
            {
                RTLevel.Current?.UpdateBackgroundObject(backgroundObject, false, false);
                EditorTimeline.inst.DeleteObject(EditorTimeline.inst.GetTimelineObject(backgroundObject), false, false);
            });
            RTLevel.Current?.backgroundEngine?.Recalculate();
            GameData.Current.backgroundObjects.Clear();
            UpdateBackgroundList();
            SetCurrentBackground(null);

            EditorManager.inst.DisplayNotification($"Deleted {count} backgrounds!", 2f, EditorManager.NotificationType.Success);
        }

        #region Dialog

        /// <summary>
        /// Opens the dialog.
        /// </summary>
        public void OpenDialog() => OpenDialog(null);

        /// <summary>
        /// Opens the dialog.
        /// </summary>
        /// <param name="backgroundObject">Background object to render.</param>
        public void OpenDialog(BackgroundObject backgroundObject)
        {
            Dialog.Open();
            RenderDialog(backgroundObject);
        }

        /// <summary>
        /// Renders the dialog.
        /// </summary>
        public void RenderDialog() => RenderDialog(null);

        /// <summary>
        /// Renders the dialog.
        /// </summary>
        /// <param name="backgroundObject">Background object to render.</param>
        public void RenderDialog(BackgroundObject backgroundObject)
        {
            Dialog.LeftContent.gameObject.SetActive(backgroundObject);

            if (!backgroundObject)
            {
                UpdateBackgroundList();
                return;
            }

            #region Base

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
            RenderFlat(backgroundObject);
            RenderFade(backgroundObject);
            RenderLayers(backgroundObject);
            RenderBin(backgroundObject);
            RenderIndex(backgroundObject);
            RenderGroup(backgroundObject);
            RenderEditorColors(backgroundObject);
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

            CoroutineHelper.StartCoroutine(Dialog.ModifiersDialog.RenderModifiers(backgroundObject));

            UpdateBackgroundList();
        }

        #region Base

        public void RenderActive(BackgroundObject backgroundObject)
        {
            Dialog.ActiveToggle.SetIsOnWithoutNotify(backgroundObject.active);
            Dialog.ActiveToggle.onValueChanged.NewListener(_val =>
            {
                backgroundObject.active = _val;
                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
            });
        }

        public void RenderName(BackgroundObject backgroundObject)
        {
            Dialog.NameField.SetTextWithoutNotify(backgroundObject.name);
            Dialog.NameField.onValueChanged.NewListener(_val =>
            {
                backgroundObject.name = _val;
                backgroundObject.timelineObject?.RenderText(_val);
            });

            TriggerHelper.InversableField(Dialog.NameField, InputFieldSwapper.Type.String);
        }

        public void RenderTags(BackgroundObject backgroundObject) => RTEditor.inst.RenderTags(backgroundObject, Dialog);

        public void RenderStartTime(BackgroundObject backgroundObject)
        {
            var startTimeField = Dialog.StartTimeField;

            startTimeField.lockToggle.SetIsOnWithoutNotify(backgroundObject.editorData.locked);
            startTimeField.lockToggle.onValueChanged.NewListener(_val =>
            {
                backgroundObject.editorData.locked = _val;

                // Since locking has no effect on the physical object, we will only need to update the timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
            });

            startTimeField.inputField.SetTextWithoutNotify(backgroundObject.StartTime.ToString());
            startTimeField.inputField.onValueChanged.NewListener(_val =>
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
            Dialog.AutokillDropdown.SetValueWithoutNotify((int)backgroundObject.autoKillType);
            Dialog.AutokillDropdown.onValueChanged.NewListener(_val =>
            {
                backgroundObject.autoKillType = (AutoKillType)_val;
                // AutoKillType affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                RenderAutokill(backgroundObject);
            });

            if (backgroundObject.autoKillType == AutoKillType.FixedTime || backgroundObject.autoKillType == AutoKillType.SongTime || backgroundObject.autoKillType == AutoKillType.LastKeyframeOffset)
            {
                Dialog.AutokillField.gameObject.SetActive(true);

                Dialog.AutokillField.SetTextWithoutNotify(backgroundObject.autoKillOffset.ToString());
                Dialog.AutokillField.onValueChanged.NewListener(_val =>
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
                Dialog.AutokillSetButton.onClick.NewListener(() =>
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

            Dialog.CollapseToggle.SetIsOnWithoutNotify(backgroundObject.editorData.collapse);
            Dialog.CollapseToggle.onValueChanged.NewListener(_val =>
            {
                backgroundObject.editorData.collapse = _val;

                // Since autokill collapse has no affect on the physical object, we will only need to update the timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
            });
        }

        public void RenderDepth(BackgroundObject backgroundObject)
        {
            Dialog.DepthField.SetTextWithoutNotify(backgroundObject.depth.ToString());
            Dialog.DepthField.OnValueChanged.NewListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                {
                    backgroundObject.depth = num;
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                }
            });

            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.DepthField);
            TriggerHelper.AddEventTriggers(Dialog.DepthField.inputField.gameObject, TriggerHelper.ScrollDeltaInt(Dialog.DepthField.inputField));
            TriggerHelper.InversableField(Dialog.DepthField);
        }

        public void RenderIterations(BackgroundObject backgroundObject)
        {
            Dialog.IterationsField.SetTextWithoutNotify(backgroundObject.iterations.ToString());
            Dialog.IterationsField.OnValueChanged.NewListener(_val =>
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
            Dialog.PositionFields.x.SetTextWithoutNotify(backgroundObject.pos.x.ToString());
            Dialog.PositionFields.x.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.pos.x = num;
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                }
            });

            Dialog.PositionFields.y.SetTextWithoutNotify(backgroundObject.pos.y.ToString());
            Dialog.PositionFields.y.OnValueChanged.NewListener(_val =>
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

            TriggerHelper.InversableField(Dialog.PositionFields.x);
            TriggerHelper.InversableField(Dialog.PositionFields.y);
        }
        
        public void RenderScale(BackgroundObject backgroundObject)
        {
            Dialog.ScaleFields.x.SetTextWithoutNotify(backgroundObject.scale.x.ToString());
            Dialog.ScaleFields.x.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.scale.x = num;
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                }
            });

            Dialog.ScaleFields.y.SetTextWithoutNotify(backgroundObject.scale.y.ToString());
            Dialog.ScaleFields.y.OnValueChanged.NewListener(_val =>
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

            TriggerHelper.InversableField(Dialog.ScaleFields.x);
            TriggerHelper.InversableField(Dialog.ScaleFields.y);
        }

        public void RenderZPosition(BackgroundObject backgroundObject)
        {
            Dialog.ZPositionField.SetTextWithoutNotify(backgroundObject.zposition.ToString());
            Dialog.ZPositionField.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.zposition = num;
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.ZPositionField);
            TriggerHelper.AddEventTriggers(Dialog.ZPositionField.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.ZPositionField.inputField));
            TriggerHelper.InversableField(Dialog.ZPositionField);
        }

        public void RenderZScale(BackgroundObject backgroundObject)
        {
            Dialog.ZScaleField.SetTextWithoutNotify(backgroundObject.zscale.ToString());
            Dialog.ZScaleField.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.zscale = float.Parse(_val);
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.ZScaleField);
            TriggerHelper.AddEventTriggers(Dialog.ZScaleField.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.ZScaleField.inputField));
            TriggerHelper.InversableField(Dialog.ZPositionField);
        }

        public void RenderRotation(BackgroundObject backgroundObject)
        {
            Dialog.RotationField.SetTextWithoutNotify(backgroundObject.rot.ToString());
            Dialog.RotationField.onValueChanged.NewListener(_val =>
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
            Dialog.RotationSlider.SetValueWithoutNotify(backgroundObject.rot);
            Dialog.RotationSlider.onValueChanged.NewListener(_val =>
            {
                backgroundObject.rot = _val;
                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                RenderRotation(backgroundObject);
            });

            TriggerHelper.AddEventTriggers(Dialog.RotationField.gameObject, TriggerHelper.ScrollDelta(Dialog.RotationField, 15f, 3f));
            TriggerHelper.InversableField(Dialog.RotationField);
        }

        public void Render3DRotation(BackgroundObject backgroundObject)
        {
            Dialog.DepthRotation.x.SetTextWithoutNotify(backgroundObject.rotation.x.ToString());
            Dialog.DepthRotation.x.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.rotation.x = num;
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                }
            });

            Dialog.DepthRotation.y.SetTextWithoutNotify(backgroundObject.rotation.y.ToString());
            Dialog.DepthRotation.y.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.rotation.y = num;
                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.DepthRotation.x, 15f, 3f);
            TriggerHelper.IncreaseDecreaseButtons(Dialog.DepthRotation.y, 15f, 3f);

            TriggerHelper.AddEventTriggers(Dialog.DepthRotation.x.inputField.gameObject,
                TriggerHelper.ScrollDelta(Dialog.DepthRotation.x.inputField, 15f, 3f, multi: true),
                TriggerHelper.ScrollDeltaVector2(Dialog.DepthRotation.x.inputField, Dialog.DepthRotation.y.inputField, 15f, 3f));
            TriggerHelper.AddEventTriggers(Dialog.DepthRotation.y.inputField.gameObject,
                TriggerHelper.ScrollDelta(Dialog.DepthRotation.y.inputField, 15f, 3f, multi: true),
                TriggerHelper.ScrollDeltaVector2(Dialog.DepthRotation.x.inputField, Dialog.DepthRotation.y.inputField, 15f, 3f));

            TriggerHelper.InversableField(Dialog.DepthRotation.x);
            TriggerHelper.InversableField(Dialog.DepthRotation.y);
        }

        public void RenderShape(BackgroundObject backgroundObject) => RTEditor.inst.RenderShapeable(
            shapeable: backgroundObject,
            dialog: Dialog,
            onUpdate: context => backgroundObject.runtimeObject?.UpdateShape(backgroundObject.Shape, backgroundObject.ShapeOption, backgroundObject.flat));

        public void RenderFlat(BackgroundObject backgroundObject)
        {
            Dialog.FlatToggle.SetIsOnWithoutNotify(backgroundObject.flat);
            Dialog.FlatToggle.OnValueChanged.NewListener(_val =>
            {
                backgroundObject.flat = _val;
                RTLevel.Current.UpdateBackgroundObject(backgroundObject);
            });
        }

        public void RenderFade(BackgroundObject backgroundObject)
        {
            Dialog.FadeToggle.toggle.SetIsOnWithoutNotify(backgroundObject.drawFade);
            Dialog.FadeToggle.toggle.onValueChanged.NewListener(_val =>
            {
                backgroundObject.drawFade = _val;
                RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
            });
        }

        public void RenderLayers(BackgroundObject backgroundObject) => RTEditor.inst.RenderEditorLayer(backgroundObject, Dialog);

        public void RenderBin(BackgroundObject backgroundObject)
        {
            Dialog.BinSlider.onValueChanged.ClearAll();
            Dialog.BinSlider.maxValue = EditorTimeline.inst.BinCount;
            Dialog.BinSlider.SetValueWithoutNotify(backgroundObject.editorData.Bin);
            Dialog.BinSlider.onValueChanged.NewListener(_val =>
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
            Dialog.EditorIndexField.inputField.SetTextWithoutNotify(currentIndex.ToString());
            Dialog.EditorIndexField.inputField.onEndEdit.NewListener(_val =>
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

            EditorContextMenu.AddContextMenu(Dialog.EditorIndexField.inputField.gameObject,
                EditorContextMenu.GetIndexerFunctions(currentIndex, GameData.Current.backgroundObjects));
        }

        public void RenderGroup(BackgroundObject backgroundObject)
        {
            Dialog.EditorGroupField.SetTextWithoutNotify(backgroundObject.EditorData.editorGroup);
            Dialog.EditorGroupField.onValueChanged.NewListener(_val =>
            {
                backgroundObject.EditorData.editorGroup = _val;
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
            });
        }

        public void RenderEditorColors(BackgroundObject backgroundObject)
        {
            Dialog.BaseColorField.SetTextWithoutNotify(backgroundObject.editorData.color);
            Dialog.BaseColorField.onValueChanged.NewListener(_val =>
            {
                backgroundObject.editorData.color = _val;
                backgroundObject.timelineObject?.RenderVisibleState(false);
            });
            var baseColorContextMenu = Dialog.BaseColorField.gameObject.GetOrAddComponent<ContextClickable>();
            baseColorContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                    backgroundObject.timelineObject?.ShowColorContextMenu(Dialog.BaseColorField, backgroundObject.editorData.color);
            };

            Dialog.SelectColorField.SetTextWithoutNotify(backgroundObject.editorData.selectedColor);
            Dialog.SelectColorField.onValueChanged.NewListener(_val =>
            {
                backgroundObject.editorData.selectedColor = _val;
                backgroundObject.timelineObject?.RenderVisibleState(false);
            });
            var selectColorContextMenu = Dialog.SelectColorField.gameObject.GetOrAddComponent<ContextClickable>();
            selectColorContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                    backgroundObject.timelineObject?.ShowColorContextMenu(Dialog.SelectColorField, backgroundObject.editorData.selectedColor);
            };

            Dialog.TextColorField.SetTextWithoutNotify(backgroundObject.editorData.textColor);
            Dialog.TextColorField.onValueChanged.NewListener(_val =>
            {
                backgroundObject.editorData.textColor = _val;
                backgroundObject.timelineObject?.RenderText(backgroundObject.name);
            });
            var textColorContextMenu = Dialog.TextColorField.gameObject.GetOrAddComponent<ContextClickable>();
            textColorContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                    backgroundObject.timelineObject?.ShowColorContextMenu(Dialog.TextColorField, backgroundObject.editorData.textColor);
            };

            Dialog.MarkColorField.SetTextWithoutNotify(backgroundObject.editorData.markColor);
            Dialog.MarkColorField.onValueChanged.NewListener(_val =>
            {
                backgroundObject.editorData.markColor = _val;
                backgroundObject.timelineObject?.RenderText(backgroundObject.name);
            });
            var markColorContextMenu = Dialog.MarkColorField.gameObject.GetOrAddComponent<ContextClickable>();
            markColorContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                    backgroundObject.timelineObject?.ShowColorContextMenu(Dialog.MarkColorField, backgroundObject.editorData.markColor);
            };
        }

        public void RenderPrefabReference(BackgroundObject backgroundObject) => RTEditor.inst.RenderPrefabable(backgroundObject, Dialog);

        #endregion

        #region Reactive

        public void RenderReactiveRanges(BackgroundObject backgroundObject)
        {
            for (int i = 0; i < Dialog.ReactiveRanges.Count; i++)
            {
                var index = i;
                var toggle = Dialog.ReactiveRanges[i];
                toggle.SetIsOnWithoutNotify(i == (int)backgroundObject.reactiveType);
                toggle.onValueChanged.NewListener(_val =>
                {
                    backgroundObject.reactiveType = (BackgroundObject.ReactiveType)index;
                    RenderDialog(backgroundObject);
                });
            }
        }

        public void RenderReactive(BackgroundObject backgroundObject)
        {
            Dialog.ReactiveIntensityField.SetTextWithoutNotify(backgroundObject.reactiveScale.ToString());
            Dialog.ReactiveIntensityField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    backgroundObject.reactiveScale = num;
                    RenderReactive(backgroundObject);
                }
            });

            Dialog.ReactiveIntensitySlider.SetValueWithoutNotify(backgroundObject.reactiveScale);
            Dialog.ReactiveIntensitySlider.onValueChanged.NewListener(_val =>
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

            Dialog.ReactivePositionSamplesFields.x.SetTextWithoutNotify(backgroundObject.reactivePosSamples.x.ToString());
            Dialog.ReactivePositionSamplesFields.x.OnValueChanged.NewListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                    backgroundObject.reactivePosSamples.x = num;
            });

            Dialog.ReactivePositionSamplesFields.y.SetTextWithoutNotify(backgroundObject.reactivePosSamples.y.ToString());
            Dialog.ReactivePositionSamplesFields.y.OnValueChanged.NewListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                    backgroundObject.reactivePosSamples.y = num;
            });

            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.ReactivePositionSamplesFields.x, max: 255);
            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.ReactivePositionSamplesFields.y, max: 255);

            Dialog.ReactivePositionIntensityFields.x.SetTextWithoutNotify(backgroundObject.reactivePosIntensity.x.ToString());
            Dialog.ReactivePositionIntensityFields.x.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    backgroundObject.reactivePosIntensity.x = num;
            });

            Dialog.ReactivePositionIntensityFields.y.SetTextWithoutNotify(backgroundObject.reactivePosIntensity.y.ToString());
            Dialog.ReactivePositionIntensityFields.y.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    backgroundObject.reactivePosIntensity.y = num;
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.ReactivePositionIntensityFields.x);
            TriggerHelper.IncreaseDecreaseButtons(Dialog.ReactivePositionIntensityFields.y);
        }

        public void RenderReactiveScale(BackgroundObject backgroundObject)
        {
            if (backgroundObject.reactiveType != BackgroundObject.ReactiveType.Custom)
                return;

            Dialog.ReactiveScaleSamplesFields.x.SetTextWithoutNotify(backgroundObject.reactiveScaSamples.x.ToString());
            Dialog.ReactiveScaleSamplesFields.x.OnValueChanged.NewListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                    backgroundObject.reactiveScaSamples.x = num;
            });

            Dialog.ReactiveScaleSamplesFields.y.SetTextWithoutNotify(backgroundObject.reactiveScaSamples.y.ToString());
            Dialog.ReactiveScaleSamplesFields.y.OnValueChanged.NewListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                    backgroundObject.reactiveScaSamples.y = num;
            });

            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.ReactiveScaleSamplesFields.x, max: 255);
            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.ReactiveScaleSamplesFields.y, max: 255);

            Dialog.ReactiveScaleIntensityFields.x.SetTextWithoutNotify(backgroundObject.reactiveScaIntensity.x.ToString());
            Dialog.ReactiveScaleIntensityFields.x.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    backgroundObject.reactiveScaIntensity.x = num;
            });

            Dialog.ReactiveScaleIntensityFields.y.SetTextWithoutNotify(backgroundObject.reactiveScaIntensity.y.ToString());
            Dialog.ReactiveScaleIntensityFields.y.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    backgroundObject.reactiveScaIntensity.y = num;
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.ReactiveScaleIntensityFields.x);
            TriggerHelper.IncreaseDecreaseButtons(Dialog.ReactiveScaleIntensityFields.y);
        }

        public void RenderReactiveRotation(BackgroundObject backgroundObject)
        {
            if (backgroundObject.reactiveType != BackgroundObject.ReactiveType.Custom)
                return;

            Dialog.ReactiveRotationSampleField.SetTextWithoutNotify(backgroundObject.reactiveRotSample.ToString());
            Dialog.ReactiveRotationSampleField.OnValueChanged.NewListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                    backgroundObject.reactiveRotSample = num;
            });

            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.ReactiveRotationSampleField, max: 255);

            Dialog.ReactiveRotationIntensityField.SetTextWithoutNotify(backgroundObject.reactiveRotIntensity.ToString());
            Dialog.ReactiveRotationIntensityField.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    backgroundObject.reactiveRotIntensity = num;
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.ReactiveRotationIntensityField);
        }

        public void RenderReactiveColor(BackgroundObject backgroundObject)
        {
            if (backgroundObject.reactiveType != BackgroundObject.ReactiveType.Custom)
                return;

            LSHelpers.DeleteChildren(Dialog.ReactiveColorsParent);
            ThemeManager.inst.Current.backgroundColors.ForLoop((color, index) => SetColorToggle(backgroundObject, color, backgroundObject.reactiveCol, index, Dialog.ReactiveColorsParent, SetReactiveColor));

            Dialog.ReactiveColorSampleField.inputField.SetTextWithoutNotify(backgroundObject.reactiveColSample.ToString());
            Dialog.ReactiveColorSampleField.inputField.onValueChanged.NewListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                    backgroundObject.reactiveColSample = num;
            });

            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.ReactiveColorSampleField, max: 255);

            Dialog.ReactiveColorIntensityField.inputField.SetTextWithoutNotify(backgroundObject.reactiveColIntensity.ToString());
            Dialog.ReactiveColorIntensityField.inputField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    backgroundObject.reactiveColIntensity = num;
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.ReactiveColorIntensityField);
        }

        public void RenderReactiveZPosition(BackgroundObject backgroundObject)
        {
            if (backgroundObject.reactiveType != BackgroundObject.ReactiveType.Custom)
                return;

            Dialog.ReactiveZPositionSampleField.SetTextWithoutNotify(backgroundObject.reactiveZSample.ToString());
            Dialog.ReactiveZPositionSampleField.OnValueChanged.NewListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                    backgroundObject.reactiveZSample = num;
            });

            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.ReactiveZPositionSampleField, max: 255);

            Dialog.ReactiveZPositionIntensityField.SetTextWithoutNotify(backgroundObject.reactiveZIntensity.ToString());
            Dialog.ReactiveZPositionIntensityField.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    backgroundObject.reactiveZIntensity = num;
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.ReactiveZPositionIntensityField);
        }

        #endregion

        #region Colors

        public void RenderColor(BackgroundObject backgroundObject)
        {
            LSHelpers.DeleteChildren(Dialog.ColorsParent);
            ThemeManager.inst.Current.backgroundColors.ForLoop((color, index) => SetColorToggle(backgroundObject, color, backgroundObject.color, index, Dialog.ColorsParent, SetColor));

            Dialog.HueSatVal.x.inputField.SetTextWithoutNotify(backgroundObject.hue.ToString());
            Dialog.HueSatVal.x.inputField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    backgroundObject.hue = num;

                LSHelpers.DeleteChildren(Dialog.ColorsParent);
                ThemeManager.inst.Current.backgroundColors.ForLoop((color, index) => SetColorToggle(backgroundObject, color, backgroundObject.color, index, Dialog.ColorsParent, SetColor));
            });

            Dialog.HueSatVal.y.inputField.SetTextWithoutNotify(backgroundObject.saturation.ToString());
            Dialog.HueSatVal.y.inputField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    backgroundObject.saturation = num;

                LSHelpers.DeleteChildren(Dialog.ColorsParent);
                ThemeManager.inst.Current.backgroundColors.ForLoop((color, index) => SetColorToggle(backgroundObject, color, backgroundObject.color, index, Dialog.ColorsParent, SetColor));
            });

            Dialog.HueSatVal.z.inputField.SetTextWithoutNotify(backgroundObject.value.ToString());
            Dialog.HueSatVal.z.inputField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    backgroundObject.value = num;

                LSHelpers.DeleteChildren(Dialog.ColorsParent);
                ThemeManager.inst.Current.backgroundColors.ForLoop((color, index) => SetColorToggle(backgroundObject, color, backgroundObject.color, index, Dialog.ColorsParent, SetColor));
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.HueSatVal.x);
            TriggerHelper.IncreaseDecreaseButtons(Dialog.HueSatVal.y);
            TriggerHelper.IncreaseDecreaseButtons(Dialog.HueSatVal.z);
            TriggerHelper.AddEventTriggers(Dialog.HueSatVal.x.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.HueSatVal.x.inputField));
            TriggerHelper.AddEventTriggers(Dialog.HueSatVal.y.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.HueSatVal.y.inputField));
            TriggerHelper.AddEventTriggers(Dialog.HueSatVal.z.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.HueSatVal.z.inputField));
            TriggerHelper.InversableField(Dialog.HueSatVal.x);
            TriggerHelper.InversableField(Dialog.HueSatVal.y);
            TriggerHelper.InversableField(Dialog.HueSatVal.z);
        }

        public void RenderFadeColor(BackgroundObject backgroundObject)
        {
            LSHelpers.DeleteChildren(Dialog.FadeColorsParent);
            ThemeManager.inst.Current.backgroundColors.ForLoop((color, index) => SetColorToggle(backgroundObject, color, backgroundObject.fadeColor, index, Dialog.FadeColorsParent, SetFadeColor));

            Dialog.FadeHueSatVal.x.inputField.SetTextWithoutNotify(backgroundObject.fadeHue.ToString());
            Dialog.FadeHueSatVal.x.inputField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    backgroundObject.fadeHue = num;

                LSHelpers.DeleteChildren(Dialog.FadeColorsParent);
                ThemeManager.inst.Current.backgroundColors.ForLoop((color, index) => SetColorToggle(backgroundObject, color, backgroundObject.fadeColor, index, Dialog.FadeColorsParent, SetFadeColor));
            });

            Dialog.FadeHueSatVal.y.inputField.SetTextWithoutNotify(backgroundObject.fadeSaturation.ToString());
            Dialog.FadeHueSatVal.y.inputField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    backgroundObject.fadeSaturation = num;

                LSHelpers.DeleteChildren(Dialog.FadeColorsParent);
                ThemeManager.inst.Current.backgroundColors.ForLoop((color, index) => SetColorToggle(backgroundObject, color, backgroundObject.fadeColor, index, Dialog.FadeColorsParent, SetFadeColor));
            });

            Dialog.FadeHueSatVal.z.inputField.SetTextWithoutNotify(backgroundObject.fadeValue.ToString());
            Dialog.FadeHueSatVal.z.inputField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    backgroundObject.fadeValue = num;

                LSHelpers.DeleteChildren(Dialog.FadeColorsParent);
                ThemeManager.inst.Current.backgroundColors.ForLoop((color, index) => SetColorToggle(backgroundObject, color, backgroundObject.fadeColor, index, Dialog.FadeColorsParent, SetFadeColor));
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.FadeHueSatVal.x);
            TriggerHelper.IncreaseDecreaseButtons(Dialog.FadeHueSatVal.y);
            TriggerHelper.IncreaseDecreaseButtons(Dialog.FadeHueSatVal.z);
            TriggerHelper.AddEventTriggers(Dialog.FadeHueSatVal.x.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.FadeHueSatVal.x.inputField));
            TriggerHelper.AddEventTriggers(Dialog.FadeHueSatVal.y.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.FadeHueSatVal.y.inputField));
            TriggerHelper.AddEventTriggers(Dialog.FadeHueSatVal.z.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.FadeHueSatVal.z.inputField));
            TriggerHelper.InversableField(Dialog.FadeHueSatVal.x);
            TriggerHelper.InversableField(Dialog.FadeHueSatVal.y);
            TriggerHelper.InversableField(Dialog.FadeHueSatVal.z);
        }

        #endregion

        public void SetColorToggle(BackgroundObject backgroundObject, Color color, int currentColor, int colTmp, Transform parent, Action<BackgroundObject, int> onSetColor)
        {
            var gameObject = EditorManager.inst.colorGUI.Duplicate(parent, "color gui");
            gameObject.transform.localScale = Vector3.one;
            var button = gameObject.GetComponent<Button>();
            button.image.color = RTColors.FadeColor(color, 1f);
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

        /// <summary>
        /// Updates the list that displays <see cref="BackgroundObject"/>s in the level.
        /// </summary>
        public void UpdateBackgroundList()
        {
            if (!GameData.Current)
                return;

            Dialog.ClearContent();
            int num = 0;
            foreach (var backgroundObject in GameData.Current.backgroundObjects)
            {
                if (!RTString.SearchString(Dialog.SearchTerm, backgroundObject.name))
                {
                    num++;
                    continue;
                }

                if (backgroundObject.FromPrefab)
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

                EditorContextMenu.AddContextMenu(gameObject, leftClick: () => SetCurrentBackground(backgroundObject),
                    new ButtonElement("Select", () => SetCurrentBackground(backgroundObject)),
                    new ButtonElement("Delete", () => DeleteBackground(backgroundObject)),
                    new SpacerElement(),
                    new ButtonElement("Inspect", () => ModCompatibility.Inspect(backgroundObject)));

                EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.List_Button_2_Normal, true);
                EditorThemeManager.ApplyGraphic(image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(name, ThemeGroup.List_Button_2_Text);
                EditorThemeManager.ApplyGraphic(text, ThemeGroup.List_Button_2_Text);

                num++;
            }
        }

        #endregion

        #endregion
    }
}
