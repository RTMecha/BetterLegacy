using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;

namespace BetterLegacy.Editor.Managers
{
    public class RTCheckpointEditor : MonoBehaviour
    {
        #region Init

        public static RTCheckpointEditor inst;

        public static void Init() => EditorManager.inst.transform.parent.Find("CheckpointEditor").gameObject.AddComponent<RTCheckpointEditor>();

        void Awake()
        {
            inst = this;

            try
            {
                Dialog = new CheckpointEditorDialog();
                Dialog.Init();

                parent = EventEditor.inst.EventHolders.transform.GetChild(14);
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

        void Update()
        {
            if (!GameData.Current || !GameData.Current.data)
                return;

            if (Dialog.IsCurrent && EditorManager.inst.isEditing)
            {
                if (CurrentCheckpoint)
                {
                    Vector3 vector = CurrentCheckpoint.Checkpoint.pos;
                    vector.z = -1f;
                    LSRenderManager.inst.CreateSprite("checkpoint_view", "checkpoint", vector, new Vector2(6f, 6f), 0f, LSColors.gray500);
                }
            }
            else
                LSRenderManager.inst.DeleteSprite("checkpoint_view");

            HandleDragging();
        }

        #endregion

        #region Values

        public CheckpointEditorDialog Dialog { get; set; }

        public TimelineCheckpoint CurrentCheckpoint { get; set; }

        public List<TimelineCheckpoint> timelineCheckpoints = new List<TimelineCheckpoint>();

        public Transform parent;

        public Checkpoint checkpointCopy;

        public bool dragging;

        #endregion

        #region Methods

        public void SetCurrentCheckpoint(int index)
        {
            if (GameData.Current.data.checkpoints.TryGetAt(index, out Checkpoint checkpoint))
                OpenDialog(checkpoint);
        }

        public void CreateNewCheckpoint() => CreateNewCheckpoint(RTLevel.Current.FixedTime, EventManager.inst.cam.transform.position);

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

        public void DeleteCheckpoint(int index)
        {
            if (!GameData.Current || !GameData.Current.data)
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

        public void CopyCheckpoint() => checkpointCopy = CurrentCheckpoint.Checkpoint.Copy();

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

        public void OpenDialog(Checkpoint checkpoint)
        {
            if (!checkpoint)
                return;

            Dialog.Open();

            CurrentCheckpoint = checkpoint.timelineCheckpoint;
            RenderDialog(checkpoint);
        }

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

            RenderCheckpoints();
        }

        public void ClearTimelineCheckpoints()
        {
            if (timelineCheckpoints.IsEmpty())
                return;

            timelineCheckpoints.ForLoop((timelineCheckpoint, index) =>
            {
                CoreHelper.Delete(timelineCheckpoint.GameObject);
                timelineCheckpoints.RemoveAt(index);
            });
        }

        public void UpdateCheckpointTimeline()
        {
            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
                CreateCheckpoints();
            else
                CreateGhostCheckpoints();
        }

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

        public void RenderCheckpoints()
        {
            for (int i = 0; i < timelineCheckpoints.Count; i++)
                timelineCheckpoints[i].Render();
        }

        public void RenderCheckpoint(int index)
        {
            if (!timelineCheckpoints.TryGetAt(index, out TimelineCheckpoint timelineCheckpoint))
                return;

            timelineCheckpoint.Render();
        }

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
                selected.enabled = checkpoint == CurrentCheckpoint;

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
    }
}
