using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using HarmonyLib;

using LSFunctions;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Core;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(CheckpointEditor))]
    public class CheckpointEditorPatch : MonoBehaviour
    {
        public static Checkpoint currentCheckpoint;
        public static CheckpointEditor Instance { get => CheckpointEditor.inst; set => CheckpointEditor.inst = value; }

        [HarmonyPatch(nameof(CheckpointEditor.Awake))]
        [HarmonyPrefix]
        static bool AwakePrefix(CheckpointEditor __instance)
        {
            if (Instance == null)
                Instance = __instance;
            else if (Instance != __instance)
            {
                Destroy(__instance.gameObject);
                return false;
            }

            __instance.className = "[<color=#65B6F7>CheckpointEditor</color>] \n"; // this is done due to the CheckpointEditor className being BackgroundEditor for some reason...

            CoreHelper.LogInit(__instance.className);

            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix()
        {
            if (!GameData.Current)
                return false;

            if (RTEditor.inst.CheckpointEditorDialog.IsCurrent && EditorManager.inst.isEditing)
            {
                if (GameData.Current.data.checkpoints != null)
                {
                    Vector3 vector = GameData.Current.data.checkpoints[Instance.currentObj].pos;
                    vector.z = -1f;
                    LSRenderManager.inst.CreateSprite("checkpoint_view", "checkpoint", vector, new Vector2(6f, 6f), 0f, LSColors.gray500);
                }
            }
            else
                LSRenderManager.inst.DeleteSprite("checkpoint_view");

            if (Input.GetMouseButtonUp(0))
                for (int i = 0; i < Instance.checkpointsDrag.Count; i++)
                    Instance.checkpointsDrag[i] = false;

            for (int j = 0; j < Instance.checkpointsDrag.Count; j++)
            {
                if (Instance.checkpointsDrag[j])
                {
                    GameData.Current.data.checkpoints[j].time = Mathf.Clamp(EditorTimeline.inst.GetTimelineTime(), 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                    Instance.left.Find("time/time").GetComponent<InputField>().text = GameData.Current.data.checkpoints[j].time.ToString("f3");
                    Instance.RenderCheckpoint(j);
                }
            }

            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.OpenDialog))]
        [HarmonyPrefix]
        static bool OpenDialogPrefix(int __0)
        {
            int index = __0;

            RTEditor.inst.CheckpointEditorDialog.Open();

            Instance.currentObj = index;

            if (GameData.Current == null || GameData.Current.data == null || GameData.Current.data.checkpoints == null)
                return false;

            var checkpoint = GameData.Current.data.checkpoints[index];
            currentCheckpoint = checkpoint;

            var search = Instance.right.Find("search").GetComponent<InputField>();
            search.onValueChanged.ClearAll();
            search.onValueChanged.AddListener(_val => Instance.RenderCheckpointList(_val, index));
            Instance.RenderCheckpointList(search.text, index);

            var first = Instance.left.Find("edit/<<").GetComponent<Button>();
            var prev = Instance.left.Find("edit/<").GetComponent<Button>();
            var next = Instance.left.Find("edit/>").GetComponent<Button>();
            var last = Instance.left.Find("edit/>>").GetComponent<Button>();

            var isFirst = __0 == 0;
            var isLast = __0 == GameData.Current.data.checkpoints.Count - 1;

            var text = isFirst ? "S" : isLast ? "E" : index.ToString();

            var delete = Instance.left.Find("edit/del").GetComponent<Button>();

            var name = Instance.left.Find("name").GetComponent<InputField>();

            var time = Instance.left.Find("time/time").GetComponent<InputField>();

            Instance.left.Find("time/<<").GetComponent<Button>().interactable = !isFirst;
            Instance.left.Find("time/<").GetComponent<Button>().interactable = !isFirst;
            Instance.left.Find("time/>").GetComponent<Button>().interactable = !isFirst;
            Instance.left.Find("time/>>").GetComponent<Button>().interactable = !isFirst;

            first.interactable = !isFirst;
            prev.interactable = !isFirst;
            delete.interactable = !isFirst;
            time.interactable = !isFirst;

            first.onClick.ClearAll();
            prev.onClick.ClearAll();
            delete.onClick.ClearAll();
            time.onValueChanged.ClearAll();
            time.text = checkpoint.time.ToString();

            if (!isFirst)
            {
                first.onClick.AddListener(() => Instance.SetCurrentCheckpoint(0));

                prev.onClick.AddListener(() => Instance.SetCurrentCheckpoint(index - 1));

                delete.onClick.AddListener(() => Instance.DeleteCheckpoint(index));

                time.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        checkpoint.time = num;
                        Instance.RenderCheckpoint(index);
                        GameManager.inst.UpdateTimeline();
                    }
                });

                TriggerHelper.IncreaseDecreaseButtons(time, t: Instance.left.Find("time"));
            }

            Instance.left.transform.Find("edit/|").GetComponentInChildren<Text>().text = text;
            next.interactable = !isLast;
            last.interactable = !isLast;

            next.onClick.ClearAll();
            last.onClick.ClearAll();

            if (!isLast)
            {
                next.onClick.AddListener(() => Instance.SetCurrentCheckpoint(index + 1));
                last.onClick.AddListener(() => Instance.SetCurrentCheckpoint(GameData.Current.data.checkpoints.Count - 1));
            }

            name.onValueChanged.ClearAll();
            name.text = checkpoint.name.ToString();
            name.onValueChanged.AddListener(_val =>
            {
                checkpoint.name = _val;
                Instance.RenderCheckpointList(search.text, index);
            });

            var timeEventTrigger = Instance.left.Find("time").GetComponent<EventTrigger>();
            timeEventTrigger.triggers.Clear();
            if (!isFirst)
                timeEventTrigger.triggers.Add(TriggerHelper.ScrollDelta(time));

            var positionX = Instance.left.Find("position/x").GetComponent<InputField>();
            var positionY = Instance.left.Find("position/y").GetComponent<InputField>();

            positionX.onValueChanged.ClearAll();
            positionX.text = checkpoint.pos.x.ToString();
            positionX.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    checkpoint.pos.x = num;
                    Instance.RenderCheckpoint(index);
                }
            });

            positionY.onValueChanged.ClearAll();
            positionY.text = checkpoint.pos.y.ToString();
            positionY.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    checkpoint.pos.y = num;
                    Instance.RenderCheckpoint(index);
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(positionX, 5f);
            TriggerHelper.IncreaseDecreaseButtons(positionY, 5f);

            TriggerHelper.AddEventTriggers(positionX.gameObject, TriggerHelper.ScrollDelta(positionX), TriggerHelper.ScrollDeltaVector2(positionX, positionY, 0.1f, 10f));
            TriggerHelper.AddEventTriggers(positionY.gameObject, TriggerHelper.ScrollDelta(positionY), TriggerHelper.ScrollDeltaVector2(positionX, positionY, 0.1f, 10f));

            Instance.RenderCheckpoints();

            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.CreateNewCheckpoint), new Type[] { })]
        [HarmonyPrefix]
        static bool CreateNewCheckpointPrefix()
        {
            Instance.CreateNewCheckpoint(EditorManager.inst.CurrentAudioPos, EventManager.inst.cam.transform.position);
            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.CreateNewCheckpoint), new Type[] { typeof(float), typeof(Vector2) })]
        [HarmonyPrefix]
        static bool CreateNewCheckpointPrefix(float __0, Vector2 __1)
        {
            GameData.Current.data.checkpoints.Add(new Checkpoint(Checkpoint.DEFAULT_CHECKPOINT_NAME, Mathf.Clamp(__0, 0f, AudioManager.inst.CurrentAudioSource.clip.length), __1));

            (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events ? (Action)Instance.CreateCheckpoints : Instance.CreateGhostCheckpoints).Invoke();

            Instance.SetCurrentCheckpoint(GameData.Current.data.checkpoints.Count - 1);
            GameManager.inst.UpdateTimeline();
            RTGameManager.inst.ResetCheckpoint(true);
            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.DeleteCheckpoint))]
        [HarmonyPrefix]
        static bool DeleteCheckpointPrefix(int __0)
        {
            Debug.Log($"{Instance.className}Deleting checkpoint at [{__0}] index.");
            GameData.Current.data.checkpoints.RemoveAt(__0);
            if (GameData.Current.data.checkpoints.Count > 0)
                Instance.SetCurrentCheckpoint(Mathf.Clamp(Instance.currentObj - 1, 0, GameData.Current.data.checkpoints.Count - 1));

            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
            {
                Instance.CreateCheckpoints();
                return false;
            }

            if (Instance.checkpoints.Count > 0)
            {
                foreach (var obj2 in Instance.checkpoints)
                    Destroy(obj2);

                Instance.checkpoints.Clear();
            }

            Instance.CreateGhostCheckpoints();

            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.CreateGhostCheckpoints))]
        [HarmonyPrefix]
        static bool CreateGhostCheckpointsPrefix()
        {
            if (Instance.checkpoints.Count > 0)
            {
                foreach (var gameObject in Instance.checkpoints)
                    CoreHelper.Destroy(gameObject);
                Instance.checkpoints.Clear();
                Instance.checkpointsDrag.Clear();
            }

            int num = 0;
            foreach (var checkpoint in GameData.Current.data.checkpoints)
            {
                var gameObject = Instance.ghostCheckpointPrefab.Duplicate(EditorManager.inst.timeline.transform, "Checkpoint " + num);
                gameObject.transform.localScale = Vector3.one;

                Instance.checkpoints.Insert(num, gameObject);

                float time = checkpoint.time;
                gameObject.transform.AsRT().sizeDelta = new Vector2(8f, 300f);
                gameObject.transform.AsRT().anchoredPosition = new Vector2(time * EditorManager.inst.Zoom - (float)(EditorManager.BaseUnit / 2), 0f);
                gameObject.SetActive(true);
                num++;
            }
            Instance.RenderCheckpoints();
            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.CreateCheckpoints))]
        [HarmonyPrefix]
        static bool CreateCheckpointsPrefix()
        {
            if (Instance.checkpoints.Count > 0)
            {
                foreach (var gameObject in Instance.checkpoints)
                    CoreHelper.Destroy(gameObject);
                Instance.checkpoints.Clear();
                Instance.checkpointsDrag.Clear();
            }

            var parent = EventEditor.inst.EventHolders.transform.GetChild(14);
            int num = 0;
            foreach (var checkpoint in GameData.Current.data.checkpoints)
            {
                GameObject gameObject2 = Instance.checkpointPrefab.Duplicate(parent, "Checkpoint " + num);
                gameObject2.transform.localScale = Vector3.one;

                Instance.checkpoints.Insert(num, gameObject2);
                Instance.checkpointsDrag.Add(false);

                gameObject2.transform.AsRT().sizeDelta = new Vector2(8f, 20f);

                TriggerHelper.AddEventTriggers(gameObject2,
                    Instance.CreateCheckpointTrigger(EventTriggerType.Submit, num),
                    Instance.CreateCheckpointTrigger(EventTriggerType.PointerClick, num),
                    Instance.CreateEventStartDragTrigger(EventTriggerType.BeginDrag, num),
                    Instance.CreateEventEndDragTrigger(EventTriggerType.EndDrag, num));
                num++;
            }
            Instance.RenderCheckpoints();
            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.RenderCheckpoint))]
        [HarmonyPrefix]
        static bool RenderCheckpointPrefix(int __0)
        {
            if (__0 < 0 || Instance.checkpoints.Count <= __0)
                return false;
            var gameObject = Instance.checkpoints[__0];

            float time = GameData.Current.data.checkpoints[__0].time;
            gameObject.transform.AsRT().anchoredPosition = new Vector2(time * EditorManager.inst.Zoom - (float)(EditorManager.BaseUnit / 2), 0f);
            gameObject.SetActive(true);
            if (EditorTimeline.inst.layerType != EditorTimeline.LayerType.Events)
                return false;

            var image = gameObject.GetComponent<Image>();
            if (Instance.currentObj != __0 || EditorManager.inst.currentDialog.Type != EditorManager.EditorDialog.DialogType.Checkpoint)
            {
                image.color = Instance.deselectedColor;
                return false;
            }

            for (int i = 0; i < Instance.checkpoints.Count; i++)
                image.color = Instance.deselectedColor;

            image.color = Instance.selectedColor;

            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.RenderCheckpoints))]
        [HarmonyPrefix]
        static bool RenderCheckpointsPrefix()
        {
            if (!GameData.Current || GameData.Current.data == null || GameData.Current.data.checkpoints == null)
                return false;

            for (int i = 0; i < GameData.Current.data.checkpoints.Count; i++)
                Instance.RenderCheckpoint(i);

            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.RenderCheckpointList))]
        [HarmonyPrefix]
        static bool RenderCheckpointListPrefix(string __0, int __1)
        {
            var transform = Instance.right.Find("checkpoints/viewport/content");
            LSHelpers.DeleteChildren(transform, false);

            int num = 0;
            foreach (var checkpoint in GameData.Current.data.checkpoints)
            {
                if (!RTString.SearchString(__0, checkpoint.name))
                {
                    num++;
                    continue;
                }

                var index = num;
                var gameObject = Instance.checkpointListButtonPrefab.Duplicate(transform, $"{checkpoint.name}_checkpoint");
                gameObject.transform.localScale = Vector3.one;

                var selected = gameObject.transform.Find("dot").GetComponent<Image>();
                var name = gameObject.transform.Find("name").GetComponent<Text>();
                var time = gameObject.transform.Find("time").GetComponent<Text>();

                name.text = checkpoint.name;
                time.text = RTString.SecondsToTime(checkpoint.time);
                selected.enabled = num == __1;

                var button = gameObject.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(() => { Instance.SetCurrentCheckpoint(index); });

                EditorThemeManager.ApplyGraphic(button.image, ThemeGroup.List_Button_2_Normal, true);
                EditorThemeManager.ApplyGraphic(selected, ThemeGroup.List_Button_2_Text);
                EditorThemeManager.ApplyGraphic(name, ThemeGroup.List_Button_2_Text);
                EditorThemeManager.ApplyGraphic(time, ThemeGroup.List_Button_2_Text);

                num++;
            }
            return false;
        }
    }
}
