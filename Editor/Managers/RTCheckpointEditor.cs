using System;
using System.Collections.Generic;

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
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Timeline;
using BetterLegacy.Editor.Managers.Settings;

namespace BetterLegacy.Editor.Managers
{
    public class RTCheckpointEditor : BaseEditor<RTCheckpointEditor, RTCheckpointEditorSettings, CheckpointEditor>
    {
        /* TODO:
        - Actually implement multi-position checkpoints
         */

        #region Values

        public override CheckpointEditor BaseInstance { get => CheckpointEditor.inst; set => CheckpointEditor.inst = value; }

        /// <summary>
        /// Dialog of the editor.
        /// </summary>
        public CheckpointEditorDialog Dialog { get; set; }

        /// <summary>
        /// The currently selected checkpoint.
        /// </summary>
        public TimelineCheckpoint CurrentCheckpoint { get; set; } = new TimelineCheckpoint();

        /// <summary>
        /// List of initialized timeline checkpoints.
        /// </summary>
        public List<TimelineCheckpoint> timelineCheckpoints = new List<TimelineCheckpoint>();

        /// <summary>
        /// Parent for timeline checkpoints.
        /// </summary>
        public Transform parent;

        /// <summary>
        /// Copied checkpoint.
        /// </summary>
        public Checkpoint checkpointCopy;

        /// <summary>
        /// If a checkpoint is being dragged.
        /// </summary>
        public bool dragging;

        /// <summary>
        /// The main Checkpoint Preview.
        /// </summary>
        public CheckpointPreview checkpointPreview = new CheckpointPreview();

        public List<CheckpointPreview> checkpointPreviews = new List<CheckpointPreview>();

        /// <summary>
        /// Type of color <see cref="checkpointPreviewImage"/> should use.
        /// </summary>
        public enum CheckpointPreviewColorType
        {
            /// <summary>
            /// Uses <see cref="LSColors.gray500"/>.
            /// </summary>
            Default,
            /// <summary>
            /// Uses the current <see cref="BeatmapTheme.guiColor"/>.
            /// </summary>
            ThemeGUI,
            /// <summary>
            /// Uses <see cref="EditorConfig.CheckpointPreviewCustomColor"/>.
            /// </summary>
            Custom,
        }

        #endregion

        #region Functions

        public override void OnInit()
        {
            try
            {
                Dialog = new CheckpointEditorDialog();
                Dialog.Init();

                parent = EventEditor.inst.EventHolders.transform.GetChild(14);

                checkpointPreview.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog

            CheckpointEditor.inst.GUILine.GetComponent<EventTrigger>().triggers.Add(TriggerHelper.CreateEntry(EventTriggerType.PointerDown, eventData =>
            {
                if (((PointerEventData)eventData).button == PointerEventData.InputButton.Right)
                    CreateNewCheckpoint(EditorTimeline.inst.GetTimelineTime(), Vector2.zero);
            }));
        }

        public override void OnTick()
        {
            if (!GameData.Current || !GameData.Current.data)
                return;

            var previewActive = Dialog.IsCurrent && EditorManager.inst.isEditing && CurrentCheckpoint && CurrentCheckpoint.Checkpoint && EditorConfig.Instance.CheckpointPreviewEnabled.Value;
            checkpointPreview.gameObject.SetActive(previewActive);
            if (previewActive)
            {
                var pos = CurrentCheckpoint.Checkpoint.pos;
                checkpointPreview.gameObject.transform.localPosition = new Vector3(pos.x, pos.y, -1f);
                checkpointPreview.image.color = EditorConfig.Instance.CheckpointPreviewColorType.Value.GetColor();
                checkpointPreview.image.rectTransform.sizeDelta = EditorConfig.Instance.CheckpointPreviewSize.Value;
            }

            for (int i = 0; i < checkpointPreviews.Count; i++)
            {
                var checkpointPreview = checkpointPreviews[i];
                var active = previewActive && CurrentCheckpoint.Checkpoint.positions.InRange(i);
                checkpointPreview.gameObject.SetActive(active);
                if (active)
                {
                    var pos = CurrentCheckpoint.Checkpoint.positions[i];
                    checkpointPreview.gameObject.transform.localPosition = new Vector3(pos.x, pos.y, -1f);
                    checkpointPreview.image.color = EditorConfig.Instance.CheckpointPreviewColorType.Value.GetColor();
                    checkpointPreview.image.rectTransform.sizeDelta = EditorConfig.Instance.CheckpointPreviewSize.Value;
                }
            }

            HandleDragging();
        }

        /// <summary>
        /// Sets the current checkpoint at an index.
        /// </summary>
        /// <param name="index">Index of the checkpoint to set.</param>
        public void SetCurrentCheckpoint(int index)
        {
            if (GameData.Current && GameData.Current.data && GameData.Current.data.checkpoints.TryGetAt(index, out Checkpoint checkpoint))
                OpenDialog(checkpoint);
        }

        /// <summary>
        /// Creates a new checkpoint at the current level time and camera position.
        /// </summary>
        public void CreateNewCheckpoint() => CreateNewCheckpoint(RTLevel.Current.FixedTime, EventManager.inst.cam.transform.position);

        /// <summary>
        /// Creates a new checkpoint.
        /// </summary>
        /// <param name="time">Time of the checkpoint to set.</param>
        /// <param name="pos">Position of the checkpoint to set.</param>
        public void CreateNewCheckpoint(float time, Vector2 pos)
        {
            if (!GameData.Current || !GameData.Current.data)
                return;

            var checkpoint = new Checkpoint(Checkpoint.DEFAULT_CHECKPOINT_NAME, Mathf.Clamp(time, 0f, AudioManager.inst.CurrentAudioSource.clip.length), pos);
            GameData.Current.data.checkpoints.Add(checkpoint);

            (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events ? (Action)CreateCheckpoints : CreateGhostCheckpoints).Invoke();

            OpenDialog(checkpoint);
            GameManager.inst.UpdateTimeline();
            RTBeatmap.Current.ResetCheckpoint(true);
        }

        /// <summary>
        /// Deletes a checkpoint at an index.
        /// </summary>
        /// <param name="index">Index of the checkpoint to delete.</param>
        public void DeleteCheckpoint(int index)
        {
            if (!GameData.Current || !GameData.Current.data)
                return;

            if (index == 0 || GameData.Current.data.checkpoints.Count <= 1)
                return;

            Debug.Log($"{CheckpointEditor.inst.className}Deleting checkpoint at [{index}] index.");
            GameData.Current.data.checkpoints.RemoveAt(index);
            if (GameData.Current.data.checkpoints.Count > 0)
                SetCurrentCheckpoint(Mathf.Clamp(index - 1, 0, GameData.Current.data.checkpoints.Count - 1));

            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
            {
                CreateCheckpoints();
                return;
            }

            CreateGhostCheckpoints();
        }

        /// <summary>
        /// Copies the currently selected checkpoint.
        /// </summary>
        public void CopyCheckpoint() => checkpointCopy = CurrentCheckpoint.Checkpoint.Copy();

        /// <summary>
        /// Pastes the copied checkpoint.
        /// </summary>
        public void PasteCheckpoint()
        {
            if (!checkpointCopy)
                return;

            var checkpoint = checkpointCopy.Copy();
            checkpoint.time = RTLevel.Current.FixedTime;
            if (EditorConfig.Instance.BPMSnapsPasted.Value && RTEditor.inst.editorInfo.bpmSnapActive)
                checkpoint.time = RTEditor.SnapToBPM(checkpoint.time);
            GameData.Current.data.checkpoints.Add(checkpoint);
            UpdateCheckpointTimeline();
            RTBeatmap.Current.ResetCheckpoint();
        }

        /// <summary>
        /// Opens the checkpoint editor.
        /// </summary>
        /// <param name="checkpoint">Checkpoint to edit.</param>
        public void OpenDialog(Checkpoint checkpoint)
        {
            if (!checkpoint)
                return;

            Dialog.Open();

            CurrentCheckpoint = checkpoint.timelineCheckpoint;
            RenderDialog(checkpoint);
        }

        /// <summary>
        /// Renders the checkpoint editor.
        /// </summary>
        /// <param name="checkpoint">Checkpoint to edit.</param>
        public void RenderDialog(Checkpoint checkpoint)
        {
            if (!checkpoint || !GameData.Current || !GameData.Current.data)
                return;

            var index = GameData.Current.data.checkpoints.IndexOf(checkpoint);
            Dialog.SearchField.onValueChanged.NewListener(_val => RenderCheckpointList());
            RenderCheckpointList();

            var isFirst = index == 0;
            var isLast = index == GameData.Current.data.checkpoints.Count - 1;

            var text = isFirst ? "S" : isLast ? "E" : index.ToString();

            Dialog.TimeField.SetInteractible(!isFirst);
            Dialog.JumpToStartButton.interactable = !isFirst;
            Dialog.JumpToPrevButton.interactable = !isFirst;
            Dialog.JumpToNextButton.interactable = !isLast;
            Dialog.JumpToLastButton.interactable = !isLast;
            Dialog.DeleteButton.button.interactable = !isFirst;

            Dialog.JumpToStartButton.onClick.NewListener(() => SetCurrentCheckpoint(0));
            Dialog.JumpToPrevButton.onClick.NewListener(() => SetCurrentCheckpoint(index - 1));
            Dialog.DeleteButton.button.onClick.NewListener(() => DeleteCheckpoint(index));

            Dialog.KeyframeIndexer.text = text;

            Dialog.JumpToNextButton.onClick.NewListener(() => SetCurrentCheckpoint(index + 1));
            Dialog.JumpToLastButton.onClick.NewListener(() => SetCurrentCheckpoint(GameData.Current.data.checkpoints.Count - 1));

            Dialog.TimeField.inputField.SetTextWithoutNotify(checkpoint.time.ToString());
            Dialog.TimeField.inputField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    checkpoint.time = num;
                    RenderCheckpoint(index);
                    GameManager.inst.UpdateTimeline();
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.TimeField);

            Dialog.NameField.SetTextWithoutNotify(checkpoint.name.ToString());
            Dialog.NameField.onValueChanged.NewListener(_val =>
            {
                checkpoint.name = _val;
                RenderCheckpointList();
            });

            Dialog.TimeField.eventTrigger.triggers.Clear();
            if (!isFirst)
                Dialog.TimeField.eventTrigger.triggers.Add(TriggerHelper.ScrollDelta(Dialog.TimeField.inputField));

            Dialog.PositionFields.x.inputField.SetTextWithoutNotify(checkpoint.pos.x.ToString());
            Dialog.PositionFields.x.inputField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    checkpoint.pos.x = num;
                    RenderCheckpoint(index);
                }
            });

            Dialog.PositionFields.y.inputField.SetTextWithoutNotify(checkpoint.pos.y.ToString());
            Dialog.PositionFields.y.inputField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    checkpoint.pos.y = num;
                    RenderCheckpoint(index);
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.PositionFields.x, 5f);
            TriggerHelper.IncreaseDecreaseButtons(Dialog.PositionFields.y, 5f);

            TriggerHelper.AddEventTriggers(Dialog.PositionFields.x.inputField.gameObject,
                TriggerHelper.ScrollDelta(Dialog.PositionFields.x.inputField),
                TriggerHelper.ScrollDeltaVector2(Dialog.PositionFields.x.inputField, Dialog.PositionFields.y.inputField));
            TriggerHelper.AddEventTriggers(Dialog.PositionFields.y.inputField.gameObject,
                TriggerHelper.ScrollDelta(Dialog.PositionFields.y.inputField),
                TriggerHelper.ScrollDeltaVector2(Dialog.PositionFields.x.inputField, Dialog.PositionFields.y.inputField));

            Dialog.RespawnToggle.toggle.SetIsOnWithoutNotify(checkpoint.respawn);
            Dialog.RespawnToggle.toggle.onValueChanged.NewListener(_val => checkpoint.respawn = _val);
            
            Dialog.HealToggle.toggle.SetIsOnWithoutNotify(checkpoint.heal);
            Dialog.HealToggle.toggle.onValueChanged.NewListener(_val => checkpoint.heal = _val);
            
            Dialog.SetTimeToggle.toggle.SetIsOnWithoutNotify(checkpoint.setTime);
            Dialog.SetTimeToggle.toggle.onValueChanged.NewListener(_val => checkpoint.setTime = _val);
            
            Dialog.ReverseToggle.toggle.SetIsOnWithoutNotify(checkpoint.reverse);
            Dialog.ReverseToggle.toggle.onValueChanged.NewListener(_val => checkpoint.reverse = _val);

            Dialog.SpawnPositionDropdown.SetValueWithoutNotify((int)checkpoint.spawnType);
            Dialog.SpawnPositionDropdown.onValueChanged.NewListener(_val => checkpoint.spawnType = (Checkpoint.SpawnPositionType)_val);

            RenderCheckpointPositions(checkpoint);

            RenderCheckpoints();
        }

        /// <summary>
        /// Renders the multi position list in the checkpoint editor.
        /// </summary>
        /// <param name="checkpoint">Checkpoint to edit.</param>
        public void RenderCheckpointPositions(Checkpoint checkpoint)
        {
            for (int i = 0; i < checkpointPreviews.Count; i++)
                CoreHelper.Delete(checkpointPreviews[i].gameObject);
            checkpointPreviews.Clear();
            for (int i = 0; i < checkpoint.positions.Count; i++)
            {
                var checkpointPreview = new CheckpointPreview();
                checkpointPreview.Init();
                checkpointPreviews.Add(checkpointPreview);
            }

            Dialog.PositionContent.DeleteChildren();
            var num = 0;
            foreach (var pos in checkpoint.positions)
            {
                var index = num;

                var gameObject = EditorPrefabHolder.Instance.Vector2InputFields.Duplicate(Dialog.PositionContent);
                var storage = gameObject.GetComponent<Vector2InputFieldStorage>();

                storage.x.SetTextWithoutNotify(pos.x.ToString());
                storage.x.OnValueChanged.NewListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                        checkpoint.positions[index] = new Vector2(num, checkpoint.positions[index].y);
                });
                storage.y.SetTextWithoutNotify(pos.y.ToString());
                storage.y.OnValueChanged.NewListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                        checkpoint.positions[index] = new Vector2(checkpoint.positions[index].x, num);
                });

                TriggerHelper.AddEventTriggers(storage.x.inputField.gameObject,
                    TriggerHelper.ScrollDelta(storage.x.inputField, multi: true),
                    TriggerHelper.ScrollDeltaVector2(storage.x.inputField, storage.y.inputField));
                TriggerHelper.AddEventTriggers(storage.y.inputField.gameObject,
                    TriggerHelper.ScrollDelta(storage.y.inputField, multi: true),
                    TriggerHelper.ScrollDeltaVector2(storage.x.inputField, storage.y.inputField));

                EditorThemeManager.ApplyInputField(storage.x);
                EditorThemeManager.ApplyInputField(storage.y);

                var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(gameObject.transform);
                var deleteStorage = delete.GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.NewListener(() =>
                {
                    checkpoint.positions.RemoveAt(index);
                    RenderCheckpointPositions(checkpoint);
                });
                EditorThemeManager.ApplyDeleteButton(deleteStorage);

                delete.GetComponent<LayoutElement>().ignoreLayout = false;
                delete.transform.AsRT().sizeDelta = new Vector2(32f, 32f);
                storage.x.transform.AsRT().sizeDelta = new Vector2(150f, 32f);
                storage.y.transform.AsRT().sizeDelta = new Vector2(150f, 32f);
                storage.x.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);
                storage.y.transform.Find("input").AsRT().sizeDelta = new Vector2(110f, 32f);

                num++;
            }

            var add = EditorPrefabHolder.Instance.CreateAddButton(Dialog.PositionContent, "add position");
            add.Text = "Add Position";
            add.OnClick.ClearAll();

            var contextClickable = add.gameObject.AddComponent<ContextClickable>();
            contextClickable.onClick = PointerEventData =>
            {
                checkpoint.positions.Add(EventManager.inst.cam.transform.position);
                RenderCheckpointPositions(checkpoint);
            };
        }

        /// <summary>
        /// Clears the timeline checkpoints.
        /// </summary>
        public void ClearTimelineCheckpoints()
        {
            if (timelineCheckpoints.IsEmpty())
                return;

            timelineCheckpoints.ForLoopReverse((timelineCheckpoint, index) =>
            {
                CoreHelper.Delete(timelineCheckpoint.GameObject);
                timelineCheckpoints.RemoveAt(index);
            });
        }

        /// <summary>
        /// Updates the checkpoint timeline.<br></br>
        /// If <see cref="EditorTimeline.layerType"/> is <see cref="EditorTimeline.LayerType.Events"/>, create normal timeline checkpoints, otherwise create ghost checkpoints.
        /// </summary>
        public void UpdateCheckpointTimeline()
        {
            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
                CreateCheckpoints();
            else
                CreateGhostCheckpoints();
        }

        /// <summary>
        /// Creates ghost checkpoints that display on the timeline. Ghost checkpoints aren't interactible and are purely just a visual indicator.
        /// </summary>
        public void CreateGhostCheckpoints()
        {
            ClearTimelineCheckpoints();
            if (!GameData.Current || !GameData.Current.data)
                return;

            int num = 0;
            foreach (var checkpoint in GameData.Current.data.checkpoints)
            {
                var timelineCheckpoint = new TimelineCheckpoint(checkpoint);
                timelineCheckpoint.InitGhost(num);
                timelineCheckpoint.Render();
                timelineCheckpoints.Add(timelineCheckpoint);
                num++;
            }
            RenderCheckpoints();
        }

        /// <summary>
        /// Creates normal timeline checkpoints that display on the timeline.
        /// </summary>
        public void CreateCheckpoints()
        {
            ClearTimelineCheckpoints();
            if (!GameData.Current || !GameData.Current.data)
                return;

            int num = 0;
            foreach (var checkpoint in GameData.Current.data.checkpoints)
            {
                var timelineCheckpoint = new TimelineCheckpoint(checkpoint);
                timelineCheckpoint.Init(num);
                timelineCheckpoint.Render();
                timelineCheckpoints.Add(timelineCheckpoint);
                num++;
            }
        }

        /// <summary>
        /// Renders all timeline checkpoints.
        /// </summary>
        public void RenderCheckpoints()
        {
            for (int i = 0; i < timelineCheckpoints.Count; i++)
                timelineCheckpoints[i].Render();
        }

        /// <summary>
        /// Renders a timeline checkpoint at an index.
        /// </summary>
        /// <param name="index">Index of the timeline checkpoint to render.</param>
        public void RenderCheckpoint(int index)
        {
            if (!timelineCheckpoints.TryGetAt(index, out TimelineCheckpoint timelineCheckpoint))
                return;

            timelineCheckpoint.Render();
        }

        /// <summary>
        /// Renders the checkpoint search list.
        /// </summary>
        public void RenderCheckpointList()
        {
            if (!GameData.Current || !GameData.Current.data)
            {
                CoreHelper.LogError($"Failed to render checkpoint list as GameData is null.");
                return;
            }

            Dialog.ClearContent();

            int num = 0;
            foreach (var checkpoint in GameData.Current.data.checkpoints)
            {
                if (!RTString.SearchString(Dialog.SearchTerm, checkpoint.name))
                {
                    num++;
                    continue;
                }

                var index = num;
                var gameObject = CheckpointEditor.inst.checkpointListButtonPrefab.Duplicate(Dialog.Content, $"{checkpoint.name}_checkpoint");
                gameObject.transform.localScale = Vector3.one;

                var selected = gameObject.transform.Find("dot").GetComponent<Image>();
                var name = gameObject.transform.Find("name").GetComponent<Text>();
                var time = gameObject.transform.Find("time").GetComponent<Text>();

                name.text = checkpoint.name;
                time.text = RTString.SecondsToTime(checkpoint.time);
                selected.enabled = num == CurrentCheckpoint.Index;

                var button = gameObject.GetComponent<Button>();
                button.onClick.NewListener(() => SetCurrentCheckpoint(index));

                EditorThemeManager.ApplyGraphic(button.image, ThemeGroup.List_Button_2_Normal, true);
                EditorThemeManager.ApplyGraphic(selected, ThemeGroup.List_Button_2_Text);
                EditorThemeManager.ApplyGraphic(name, ThemeGroup.List_Button_2_Text);
                EditorThemeManager.ApplyGraphic(time, ThemeGroup.List_Button_2_Text);

                num++;
            }
        }

        void HandleDragging()
        {
            if (Input.GetMouseButtonUp(0))
            {
                dragging = false;
                for (int i = 0; i < timelineCheckpoints.Count; i++)
                    timelineCheckpoints[i].dragging = false;
            }

            if (!dragging)
                return;

            if (CurrentCheckpoint && CurrentCheckpoint.dragging)
            {
                var time = Mathf.Clamp(EditorTimeline.inst.GetTimelineTime(RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsCheckpoints.Value), 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                CurrentCheckpoint.Checkpoint.time = time;
                CurrentCheckpoint.RenderPosition();

                if (Dialog.IsCurrent)
                    Dialog.TimeField.inputField.text = time.ToString("f3");
            }
            //for (int i = 0; i < timelineCheckpoints.Count; i++)
            //{
            //    var timelineCheckpoint = timelineCheckpoints[i];
            //    if (!timelineCheckpoint.dragging)
            //        continue;

            //    timelineCheckpoint.Checkpoint.time = time;
            //    timelineCheckpoint.RenderPosition();
            //}

        }

        #endregion

        /// <summary>
        /// Represents a checkpoint that displays in the preview.
        /// </summary>
        public class CheckpointPreview : Exists
        {
            /// <summary>
            /// Game Object of the checkpoint preview.
            /// </summary>
            public GameObject gameObject;

            /// <summary>
            /// Image of the checkpoint preview.
            /// </summary>
            public Image image;

            /// <summary>
            /// Initializes the checkpoint preview.
            /// </summary>
            public void Init()
            {
                gameObject = Creator.NewUIObject("Checkpoint", LSRenderManager.inst.FrontWorldCanvas);
                image = gameObject.AddComponent<Image>();
                image.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile("core/sprites/checkpoint.png"));
                image.rectTransform.sizeDelta = EditorConfig.Instance.CheckpointPreviewSize.Value;
            }
        }
    }

    public static class CheckpointExtension
    {
        /// <summary>
        /// Gets the color associated with the color type.
        /// </summary>
        /// <returns>Returns the color associated with the color type. Check <see cref="RTCheckpointEditor.CheckpointPreviewColorType"/> for each color.</returns>
        public static Color GetColor(this RTCheckpointEditor.CheckpointPreviewColorType checkpointPreviewColorType) => checkpointPreviewColorType switch
        {
            RTCheckpointEditor.CheckpointPreviewColorType.ThemeGUI => ThemeManager.inst.Current.guiColor,
            RTCheckpointEditor.CheckpointPreviewColorType.Custom => EditorConfig.Instance.CheckpointPreviewCustomColor.Value,
            _ => LSColors.gray500,
        };
    }
}
