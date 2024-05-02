using BetterLegacy.Core;
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

        [HarmonyPatch("Awake")]
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

            Debug.Log($"{__instance.className}" +
                $"---------------------------------------------------------------------\n" +
                $"---------------------------- INITIALIZED ----------------------------\n" +
                $"---------------------------------------------------------------------\n");

            return false;
        }

        [HarmonyPatch("OpenDialog")]
        [HarmonyPrefix]
        static bool OpenDialog(int __0)
        {
            int check = __0;

            EditorManager.inst.ClearDialogs();
            EditorManager.inst.ShowDialog("Checkpoint Editor");

            if (Instance.right == null)
                Instance.right = EditorManager.inst.GetDialog("Checkpoint Editor").Dialog.Find("data/right");

            if (Instance.left == null)
                Instance.left = EditorManager.inst.GetDialog("Checkpoint Editor").Dialog.Find("data/left");

            Instance.currentObj = __0;

            var checkpoint = DataManager.inst.gameData.beatmapData.checkpoints[__0];

            var search = Instance.right.Find("search").GetComponent<InputField>();
            search.onValueChanged.RemoveAllListeners();
            search.onValueChanged.AddListener(delegate (string _val)
            {
                Instance.RenderCheckpointList(_val, __0);
            });
            Instance.RenderCheckpointList(search.text, __0);

            var first = Instance.left.transform.Find("edit/<<").GetComponent<Button>();
            var prev = Instance.left.transform.Find("edit/<").GetComponent<Button>();
            var next = Instance.left.transform.Find("edit/>").GetComponent<Button>();
            var last = Instance.left.transform.Find("edit/>>").GetComponent<Button>();

            var isFirst = __0 == 0;
            var isLast = __0 == DataManager.inst.gameData.beatmapData.checkpoints.Count - 1;

            var text = isFirst ? "S" : isLast ? "E" : check.ToString();

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

            first.onClick.RemoveAllListeners();
            prev.onClick.RemoveAllListeners();
            delete.onClick.RemoveAllListeners();
            time.onValueChanged.RemoveAllListeners();
            time.text = checkpoint.time.ToString();

            if (!isFirst)
            {
                first.onClick.AddListener(() => Instance.SetCurrentCheckpoint(0));

                prev.onClick.AddListener(() => Instance.SetCurrentCheckpoint(__0 - 1));

                delete.onClick.AddListener(() => Instance.DeleteCheckpoint(__0));

                time.onValueChanged.AddListener(delegate (string _val)
                {
                    if (float.TryParse(_val, out float num))
                    {
                        checkpoint.time = num;
                        Instance.RenderCheckpoint(check);
                        GameManager.inst.UpdateTimeline();
                    }
                });

                TriggerHelper.IncreaseDecreaseButtons(time, t: Instance.left.Find("time"));
            }

            Instance.left.transform.Find("edit/|").GetComponentInChildren<Text>().text = text;
            next.interactable = !isLast;
            last.interactable = !isLast;

            next.onClick.RemoveAllListeners();
            last.onClick.RemoveAllListeners();

            if (!isLast)
            {
                next.onClick.AddListener(() => Instance.SetCurrentCheckpoint(__0 + 1));

                last.onClick.AddListener(() => Instance.SetCurrentCheckpoint(DataManager.inst.gameData.beatmapData.checkpoints.Count - 1));
            }

            name.onValueChanged.RemoveAllListeners();
            name.text = checkpoint.name.ToString();
            name.onValueChanged.AddListener(delegate (string _val)
            {
                checkpoint.name = _val;
                Instance.RenderCheckpointList(search.text, __0);
            });

            var timeEventTrigger = Instance.left.Find("time").GetComponent<EventTrigger>();
            timeEventTrigger.triggers.Clear();
            if (!isFirst)
                timeEventTrigger.triggers.Add(TriggerHelper.ScrollDelta(time));

            var positionX = Instance.left.Find("position/x").GetComponent<InputField>();
            var positionY = Instance.left.Find("position/y").GetComponent<InputField>();

            positionX.onValueChanged.RemoveAllListeners();
            positionX.text = checkpoint.pos.x.ToString();
            positionX.onValueChanged.AddListener(delegate (string _val)
            {
                if (float.TryParse(_val, out float num))
                {
                    checkpoint.pos.x = num;
                    Instance.RenderCheckpoint(check);
                }
            });

            positionY.onValueChanged.RemoveAllListeners();
            positionY.text = checkpoint.pos.y.ToString();
            positionY.onValueChanged.AddListener(delegate (string _val)
            {
                if (float.TryParse(_val, out float num))
                {
                    checkpoint.pos.y = num;
                    Instance.RenderCheckpoint(check);
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(positionX, 5f);
            TriggerHelper.IncreaseDecreaseButtons(positionY, 5f);

            TriggerHelper.AddEventTriggerParams(positionX.gameObject, TriggerHelper.ScrollDelta(positionX), TriggerHelper.ScrollDeltaVector2(positionX, positionY, 0.1f, 10f));
            TriggerHelper.AddEventTriggerParams(positionY.gameObject, TriggerHelper.ScrollDelta(positionY), TriggerHelper.ScrollDeltaVector2(positionX, positionY, 0.1f, 10f));

            Instance.RenderCheckpoints();

            return false;
        }

        [HarmonyPatch("CreateNewCheckpoint", new Type[] { })]
        [HarmonyPrefix]
        static bool CreateNewCheckpointPrefix()
        {
            Instance.CreateNewCheckpoint(EditorManager.inst.CurrentAudioPos, EventManager.inst.cam.transform.position);
            return false;
        }

        [HarmonyPatch("CreateNewCheckpoint", new Type[] { typeof(float), typeof(Vector2) })]
        [HarmonyPrefix]
        static bool CreateNewCheckpointPrefix(CheckpointEditor __instance, float __0, Vector2 __1)
        {
            var checkpoint = new DataManager.GameData.BeatmapData.Checkpoint();
            checkpoint.time = Mathf.Clamp(__0, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
            checkpoint.pos = __1;
            DataManager.inst.gameData.beatmapData.checkpoints.Add(checkpoint);

            (RTEditor.inst.layerType == RTEditor.LayerType.Events ? (Action)__instance.CreateCheckpoints : __instance.CreateGhostCheckpoints).Invoke();

            __instance.SetCurrentCheckpoint(DataManager.inst.gameData.beatmapData.checkpoints.Count - 1);
            GameManager.inst.UpdateTimeline();
            GameManager.inst.ResetCheckpoints();
            return false;
        }

        [HarmonyPatch("DeleteCheckpoint")]
        [HarmonyPrefix]
        static bool DeleteCheckpointPrefix(int __0)
        {
            Debug.Log($"{Instance.className}Deleting checkpoint at [{__0}] index.");
            DataManager.inst.gameData.beatmapData.checkpoints.RemoveAt(__0);
            if (DataManager.inst.gameData.beatmapData.checkpoints.Count > 0)
                Instance.SetCurrentCheckpoint(Mathf.Clamp(Instance.currentObj - 1, 0, DataManager.inst.gameData.beatmapData.checkpoints.Count - 1));

            if (RTEditor.inst.layerType == RTEditor.LayerType.Events)
            {
                CheckpointEditor.inst.CreateCheckpoints();
                return false;
            }

            if (CheckpointEditor.inst.checkpoints.Count > 0)
            {
                foreach (var obj2 in CheckpointEditor.inst.checkpoints)
                    Destroy(obj2);

                CheckpointEditor.inst.checkpoints.Clear();
            }

            CheckpointEditor.inst.CreateGhostCheckpoints();

            return false;
        }

        [HarmonyPatch("RenderCheckpoint")]
        [HarmonyPrefix]
        static bool RenderCheckpointPrefix(CheckpointEditor __instance, int __0)
        {
            if (__0 >= 0 && __instance.checkpoints.Count > __0)
            {
                float time = DataManager.inst.gameData.beatmapData.checkpoints[__0].time;
                __instance.checkpoints[__0].transform.AsRT().anchoredPosition = new Vector2(time * EditorManager.inst.Zoom - (float)(EditorManager.BaseUnit / 2), 0f);
                if (RTEditor.inst.layerType == RTEditor.LayerType.Events)
                {
                    var image = __instance.checkpoints[__0].GetComponent<Image>();
                    if (__instance.currentObj == __0 && EditorManager.inst.currentDialog.Type == EditorManager.EditorDialog.DialogType.Checkpoint)
                    {
                        for (int i = 0; i < __instance.checkpoints.Count; i++)
                            image.color = __instance.deselectedColor;
                        image.color = __instance.selectedColor;
                    }
                    else
                        image.color = __instance.deselectedColor;
                }
                __instance.checkpoints[__0].SetActive(true);
            }
            return false;
        }

        [HarmonyPatch("RenderCheckpoints")]
        [HarmonyPrefix]
        static bool RenderCheckpointsPrefix()
        {
            if (DataManager.inst.gameData == null || DataManager.inst.gameData.beatmapData == null || DataManager.inst.gameData.beatmapData.checkpoints == null)
                return false;

            int num = 0;
            foreach (var checkpoint in DataManager.inst.gameData.beatmapData.checkpoints)
            {
                Instance.RenderCheckpoint(num);
                num++;
            }
            return false;
        }

        [HarmonyPatch("RenderCheckpointList")]
        [HarmonyPrefix]
        static bool RenderCheckpointListPrefix(string __0, int __1)
        {
            if (Instance.right == null)
                Instance.right = EditorManager.inst.GetDialog("Checkpoint Editor").Dialog.Find("data/right");

            var transform = Instance.right.Find("checkpoints/viewport/content");
            LSHelpers.DeleteChildren(transform, false);

            int num = 0;
            foreach (var checkpoint in DataManager.inst.gameData.beatmapData.checkpoints)
            {
                if (string.IsNullOrEmpty(__0) || checkpoint.name.ToLower().Contains(__0.ToLower()))
                {
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
                    button.onClick.AddListener(delegate
                    {
                        Instance.SetCurrentCheckpoint(index);
                    });

                    EditorThemeManager.ApplyGraphic(button.image, ThemeGroup.List_Button_2_Normal, true);
                    EditorThemeManager.ApplyGraphic(selected, ThemeGroup.List_Button_2_Text);
                    EditorThemeManager.ApplyGraphic(name, ThemeGroup.List_Button_2_Text);
                    EditorThemeManager.ApplyGraphic(time, ThemeGroup.List_Button_2_Text);
                }
                num++;
            }
            return false;
        }
    }
}
