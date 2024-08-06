using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Managers;
using HarmonyLib;
using LSFunctions;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(CheckpointEditor))]
    public class CheckpointEditorPatch : MonoBehaviour
    {
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

            __instance.className = "[<color=#65B6F7>CheckpointEditor</color>] \n";

            CoreHelper.LogInit(__instance.className);

            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.OpenDialog))]
        [HarmonyPrefix]
        static bool OpenDialogPrefix(int __0)
        {
            int index = __0;

            EditorManager.inst.ClearDialogs();
            EditorManager.inst.ShowDialog("Checkpoint Editor");

            if (Instance.right == null)
                Instance.right = EditorManager.inst.GetDialog("Checkpoint Editor").Dialog.Find("data/right");

            if (Instance.left == null)
                Instance.left = EditorManager.inst.GetDialog("Checkpoint Editor").Dialog.Find("data/left");

            Instance.currentObj = index;

            var checkpoint = GameData.Current.beatmapData.checkpoints[index];

            var search = Instance.right.Find("search").GetComponent<InputField>();
            search.onValueChanged.ClearAll();
            search.onValueChanged.AddListener(_val => { Instance.RenderCheckpointList(_val, index); });
            Instance.RenderCheckpointList(search.text, index);

            var first = Instance.left.transform.Find("edit/<<").GetComponent<Button>();
            var prev = Instance.left.transform.Find("edit/<").GetComponent<Button>();
            var next = Instance.left.transform.Find("edit/>").GetComponent<Button>();
            var last = Instance.left.transform.Find("edit/>>").GetComponent<Button>();

            var isFirst = __0 == 0;
            var isLast = __0 == GameData.Current.beatmapData.checkpoints.Count - 1;

            var text = isFirst ? "S" : isLast ? "E" : index.ToString();

            var delete = Instance.left.transform.Find("edit/del").GetComponent<Button>();

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
                last.onClick.AddListener(() => Instance.SetCurrentCheckpoint(DataManager.inst.gameData.beatmapData.checkpoints.Count - 1));
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
            var checkpoint = new DataManager.GameData.BeatmapData.Checkpoint();
            checkpoint.time = Mathf.Clamp(__0, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
            checkpoint.pos = __1;
            GameData.Current.beatmapData.checkpoints.Add(checkpoint);

            (RTEditor.inst.layerType == RTEditor.LayerType.Events ? (Action)Instance.CreateCheckpoints : Instance.CreateGhostCheckpoints).Invoke();

            Instance.SetCurrentCheckpoint(GameData.Current.beatmapData.checkpoints.Count - 1);
            GameManager.inst.UpdateTimeline();
            GameManager.inst.ResetCheckpoints();
            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.DeleteCheckpoint))]
        [HarmonyPrefix]
        static bool DeleteCheckpointPrefix(int __0)
        {
            Debug.Log($"{Instance.className}Deleting checkpoint at [{__0}] index.");
            GameData.Current.beatmapData.checkpoints.RemoveAt(__0);
            if (GameData.Current.beatmapData.checkpoints.Count > 0)
                Instance.SetCurrentCheckpoint(Mathf.Clamp(Instance.currentObj - 1, 0, GameData.Current.beatmapData.checkpoints.Count - 1));

            if (RTEditor.inst.layerType == RTEditor.LayerType.Events)
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

        [HarmonyPatch(nameof(CheckpointEditor.RenderCheckpoint))]
        [HarmonyPrefix]
        static bool RenderCheckpointPrefix(int __0)
        {
            if (__0 < 0 || Instance.checkpoints.Count <= __0)
                return false;
            var gameObject = Instance.checkpoints[__0];

            float time = GameData.Current.beatmapData.checkpoints[__0].time;
            gameObject.transform.AsRT().anchoredPosition = new Vector2(time * EditorManager.inst.Zoom - (float)(EditorManager.BaseUnit / 2), 0f);
            gameObject.SetActive(true);
            if (RTEditor.inst.layerType != RTEditor.LayerType.Events)
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
            if (!GameData.IsValid || GameData.Current.beatmapData == null || GameData.Current.beatmapData.checkpoints == null)
                return false;

            for (int i = 0; i < DataManager.inst.gameData.beatmapData.checkpoints.Count; i++)
                Instance.RenderCheckpoint(i);

            return false;
        }

        [HarmonyPatch(nameof(CheckpointEditor.RenderCheckpointList))]
        [HarmonyPrefix]
        static bool RenderCheckpointListPrefix(string __0, int __1)
        {
            if (Instance.right == null)
                Instance.right = EditorManager.inst.GetDialog("Checkpoint Editor").Dialog.Find("data/right");

            var transform = Instance.right.Find("checkpoints/viewport/content");
            LSHelpers.DeleteChildren(transform, false);

            int num = 0;
            foreach (var checkpoint in GameData.Current.beatmapData.checkpoints)
            {
                if (!CoreHelper.SearchString(__0, checkpoint.name))
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
                time.text = FontManager.TextTranslater.SecondsToTime(checkpoint.time);
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
