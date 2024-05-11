using BetterLegacy.Components;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;
using HarmonyLib;
using LSFunctions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(MarkerEditor))]
    public class MarkerEditorPatch : MonoBehaviour
    {
        static MarkerEditor Instance { get => MarkerEditor.inst; set => MarkerEditor.inst = value; }

        static List<DataManager.GameData.BeatmapData.Marker> Markers => DataManager.inst.gameData.beatmapData.markers;

        static string className = "[<color=#FFAF38>MarkerEditor</color>] \n";

        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        static bool AwakePostfix(MarkerEditor __instance)
        {
            if (!Instance)
                Instance = __instance;
            else if (Instance != __instance)
            {
                Destroy(__instance.gameObject);
                return false;
            }

            CoreHelper.LogInit(className);

            Instance.StartCoroutine(Wait());
            return false;
        }

        static IEnumerator Wait()
        {
            while (!EditorManager.inst || !EditorPrefabHolder.Instance.NumberInputField)
                yield return null;

            var dialog = EditorManager.inst.GetDialog("Marker Editor").Dialog;

            Instance.dialog = dialog;
            Instance.left = dialog.Find("data/left");
            Instance.right = dialog.Find("data/right");

            var indexparent = new GameObject("index");
            indexparent.transform.SetParent(Instance.left);
            indexparent.transform.SetSiblingIndex(0);
            var rtindexpr = indexparent.AddComponent<RectTransform>();
            rtindexpr.pivot = new Vector2(0f, 1f);
            rtindexpr.sizeDelta = new Vector2(371f, 32f);

            var index = new GameObject("text");
            index.transform.parent = indexparent.transform;
            var rtindex = index.AddComponent<RectTransform>();
            index.AddComponent<CanvasRenderer>();
            var ttindex = index.AddComponent<Text>();

            rtindex.anchoredPosition = Vector2.zero;
            rtindex.anchorMax = Vector2.one;
            rtindex.anchorMin = Vector2.zero;
            rtindex.pivot = new Vector2(0f, 1f);
            rtindex.sizeDelta = Vector2.zero;

            ttindex.text = "Index: 0";
            ttindex.font = FontManager.inst.DefaultFont;
            ttindex.color = new Color(0.9f, 0.9f, 0.9f);
            ttindex.alignment = TextAnchor.MiddleLeft;
            ttindex.fontSize = 20;
            ttindex.horizontalOverflow = HorizontalWrapMode.Overflow;

            EditorThemeManager.AddLightText(ttindex);

            // Makes label consistent with other labels. Originally said "Marker Time" where other labels do not mention "Marker".
            var timeLabel = Instance.left.GetChild(3).GetChild(0).GetComponent<Text>();
            timeLabel.text = "Time";
            // Fixes "Name" label.
            var descriptionLabel = Instance.left.GetChild(5).GetChild(0).GetComponent<Text>();
            descriptionLabel.text = "Description";

            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Background_1, dialog.gameObject, new List<Component> { dialog.GetComponent<Image>() }));

            EditorThemeManager.AddInputField(Instance.right.Find("InputField").GetComponent<InputField>(), ThemeGroup.Search_Field_2);

            var scrollbar = Instance.right.transform.Find("Scrollbar").GetComponent<Scrollbar>();
            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Scrollbar_2, scrollbar.gameObject, new List<Component>
            {
                scrollbar.GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.W));

            var scrollbarHandle = scrollbar.handleRect.gameObject;
            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Scrollbar_2_Handle, scrollbarHandle, new List<Component>
            {
                scrollbar.image,
                scrollbar
            }, true, 1, SpriteManager.RoundedSide.W, true));

            EditorThemeManager.AddLightText(Instance.left.GetChild(1).GetChild(0).GetComponent<Text>());
            EditorThemeManager.AddLightText(timeLabel);
            EditorThemeManager.AddLightText(descriptionLabel);

            EditorThemeManager.AddInputField(Instance.left.Find("name").GetComponent<InputField>());
            EditorThemeManager.AddInputField(Instance.left.Find("desc").GetComponent<InputField>());

            var time = EditorPrefabHolder.Instance.NumberInputField.Duplicate(Instance.left, "time new", 4);
            Destroy(Instance.left.Find("time").gameObject);

            var timeStorage = time.GetComponent<InputFieldStorage>();
            EditorThemeManager.AddInputField(timeStorage.inputField);

            EditorThemeManager.AddSelectable(timeStorage.leftGreaterButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(timeStorage.leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(timeStorage.middleButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(timeStorage.rightButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(timeStorage.rightGreaterButton, ThemeGroup.Function_2, false);

            time.name = "time";

            if (!EditorPrefabHolder.Instance.Function2Button)
            {
                CoreHelper.LogError("No Function 2 button for some reason.");
                yield break;
            }

            var makeNote = EditorPrefabHolder.Instance.Function2Button.Duplicate(Instance.left, "convert to note", 8);
            var makeNoteStorage = makeNote.GetComponent<FunctionButtonStorage>();
            makeNoteStorage.text.text = "Convert to Planner Note";
            makeNoteStorage.button.onClick.ClearAll();

            EditorThemeManager.AddSelectable(makeNoteStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(makeNoteStorage.text, ThemeGroup.Function_2_Text);

            var snapToBPM = EditorPrefabHolder.Instance.Function2Button.Duplicate(Instance.left, "snap bpm", 5);
            var snapToBPMStorage = snapToBPM.GetComponent<FunctionButtonStorage>();
            snapToBPMStorage.text.text = "Snap BPM";
            snapToBPMStorage.button.onClick.ClearAll();

            EditorThemeManager.AddSelectable(snapToBPMStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(snapToBPMStorage.text, ThemeGroup.Function_2_Text);
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePostfix()
        {
            var config = EditorConfig.Instance;

            if (config.MarkerLoopActive.Value && DataManager.inst.gameData.beatmapData.markers.Count > 0)
            {
                int markerStart = config.MarkerLoopBegin.Value;
                int markerEnd = config.MarkerLoopEnd.Value;

                if (markerStart < 0)
                    markerStart = 0;
                if (markerStart > DataManager.inst.gameData.beatmapData.markers.Count - 1)
                    markerStart = DataManager.inst.gameData.beatmapData.markers.Count - 1;

                if (markerEnd < 0)
                    markerEnd = 0;
                if (markerEnd > DataManager.inst.gameData.beatmapData.markers.Count - 1)
                    markerEnd = DataManager.inst.gameData.beatmapData.markers.Count - 1;

                if (AudioManager.inst.CurrentAudioSource.time > DataManager.inst.gameData.beatmapData.markers[markerEnd].time)
                    AudioManager.inst.CurrentAudioSource.time = DataManager.inst.gameData.beatmapData.markers[markerStart].time;
            }
        }

        [HarmonyPatch("OpenDialog")]
        [HarmonyPrefix]
        static bool OpenDialogPostfix(int __0)
        {
            EditorManager.inst.ClearDialogs();
            EditorManager.inst.ShowDialog("Marker Editor");

            Instance.left.Find("color").GetComponent<GridLayoutGroup>().spacing = new Vector2(8f, 8f);
            Instance.left.Find("index/text").GetComponent<Text>().text = $"Index: {__0}";

            var marker = Markers[__0];

            var matchCollection = Regex.Matches(marker.desc, @"setLayer\((.*?)\)");

            if (matchCollection.Count > 0)
                foreach (var obj in matchCollection)
                {
                    var match = (Match)obj;

                    var matchGroup = match.Groups[1].ToString();
                    if (matchGroup.ToLower() == "events" || matchGroup.ToLower() == "check" || matchGroup.ToLower() == "event/check" || matchGroup.ToLower() == "event")
                        RTEditor.inst.SetLayer(RTEditor.LayerType.Events);
                    else if (matchGroup.ToLower() == "object" || matchGroup.ToLower() == "objects")
                        RTEditor.inst.SetLayer(RTEditor.LayerType.Objects);
                    else if (matchGroup.ToLower() == "toggle" || matchGroup.ToLower() == "swap")
                        RTEditor.inst.SetLayer(RTEditor.inst.layerType == RTEditor.LayerType.Objects ? RTEditor.LayerType.Events : RTEditor.LayerType.Objects);
                    else if (int.TryParse(matchGroup, out int layer))
                        RTEditor.inst.SetLayer(Mathf.Clamp(layer - 1, 0, int.MaxValue));
                }

            LSHelpers.DeleteChildren(Instance.left.Find("color"), false);
            int num = 0;
            foreach (var color in Instance.markerColors)
            {
                int index = num;
                var gameObject = EditorManager.inst.colorGUI.Duplicate(Instance.left.Find("color"), "marker color");
                gameObject.transform.localScale = Vector3.one;
                gameObject.transform.Find("Image").gameObject.SetActive(marker.color == index);
                var button = gameObject.GetComponent<Button>();
                button.image.color = color;
                button.onClick.ClearAll();
                button.onClick.AddListener(delegate
                {
                    Debug.Log($"{EditorManager.inst.className}Set Marker {__0}'s color to {index}");
                    Instance.SetColor(index);
                    Instance.UpdateColorSelection();
                });

                EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Null, gameObject, new List<Component>
                {
                    button.image,
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Background_1, gameObject.transform.GetChild(0).gameObject, new List<Component>
                {
                    gameObject.transform.GetChild(0).GetComponent<Image>(),
                }));

                num++;
            }

            var name = Instance.left.Find("name").GetComponent<InputField>();
            name.onValueChanged.ClearAll();
            name.text = marker.name.ToString();
            name.onValueChanged.AddListener(delegate (string val)
            {
                Instance.SetName(val);
            });

            var desc = Instance.left.Find("desc").GetComponent<InputField>();
            desc.onValueChanged.ClearAll();
            desc.text = marker.desc.ToString();
            desc.onValueChanged.AddListener(delegate (string val)
            {
                Instance.SetDescription(val);
            });

            var time = Instance.left.Find("time/input").GetComponent<InputField>();
            time.onValueChanged.ClearAll();
            time.text = marker.time.ToString();
            time.onValueChanged.AddListener(delegate (string val)
            {
                Instance.SetTime(val);
            });

            TriggerHelper.AddEventTriggerParams(time.gameObject, TriggerHelper.ScrollDelta(time));
            TriggerHelper.IncreaseDecreaseButtons(time, t: Instance.left.Find("time"));

            var set = Instance.left.Find("time/|").GetComponent<Button>();
            set.onClick.ClearAll();
            set.onClick.AddListener(delegate
            {
                time.text = AudioManager.inst.CurrentAudioSource.time.ToString();
            });

            if (Instance.left.Find("snap bpm"))
            {
                var button = Instance.left.Find("snap bpm").GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(delegate ()
                {
                    time.text = RTEditor.SnapToBPM(marker.time).ToString();
                });
            }

            if (Instance.left.Find("convert to note"))
            {
                var button = Instance.left.Find("convert to note").GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(delegate ()
                {
                    var note = new ProjectPlannerManager.NoteItem();
                    note.Active = true;
                    note.Name = marker.name;
                    note.Color = marker.color;
                    note.Position = new Vector2(Screen.width / 2, Screen.height / 2);
                    note.Text = marker.desc;
                    ProjectPlannerManager.inst.planners.Add(note);
                    ProjectPlannerManager.inst.GenerateNote(note);

                    ProjectPlannerManager.inst.SaveNotes();
                });
            }

            EditorManager.inst.ShowDialog("Marker Editor");
            Instance.UpdateMarkerList();
            Instance.RenderMarkers();

            return false;
        }

        [HarmonyPatch("RenderMarker")]
        [HarmonyPrefix]
        static bool RenderMarkersPostfix(int __0)
        {
            if (__0 >= 0 && Instance.markers.Count > __0)
            {
                var marker = Markers[__0];
                float time = marker.time;
                var markerObject = Instance.markers[__0];
                var markerColor = Instance.markerColors[Mathf.Clamp(marker.color, 0, Instance.markerColors.Count - 1)];

                var hoverTooltip = markerObject.GetComponent<HoverTooltip>();
                if (hoverTooltip)
                {
                    hoverTooltip.tooltipLangauges.Clear();
                    hoverTooltip.tooltipLangauges.Add(TooltipHelper.NewTooltip("<#" + LSColors.ColorToHex(markerColor) + ">" + marker.name + " [ " + marker.time + " ]</color>", marker.desc, new List<string>()));
                }

                ((RectTransform)markerObject.transform).sizeDelta = new Vector2(12f, 12f);
                ((RectTransform)markerObject.transform).anchoredPosition = new Vector2(time * EditorManager.inst.Zoom - 6f, -12f);
                markerObject.GetComponent<Image>().color = markerColor;

                var text = markerObject.transform.Find("Text");
                var textComponent = text.GetComponent<Text>();
                textComponent.text = marker.name;
                EditorThemeManager.ApplyLightText(textComponent);
                text.transform.AsRT().sizeDelta = new Vector2(EditorConfig.Instance.MarkerTextWidth.Value, 20f);
                markerObject.SetActive(true);

                var line = markerObject.transform.Find("line");
                line.GetComponent<Image>().color = EditorConfig.Instance.MarkerLineColor.Value;
                line.transform.AsRT().sizeDelta = new Vector2(EditorConfig.Instance.MarkerLineWidth.Value, 301f);
            }
            return false;
        }

        [HarmonyPatch("UpdateMarkerList")]
        [HarmonyPrefix]
        static bool UpdateMarkerList()
        {
            var parent = Instance.right.Find("markers/list");
            LSHelpers.DeleteChildren(parent);

            //Delete Markers
            {
                var delete = EditorPrefabHolder.Instance.Function1Button.Duplicate(parent, "delete markers");
                var deleteStorage = delete.GetComponent<FunctionButtonStorage>();

                var deleteText = deleteStorage.text;
                deleteText.text = "Delete Markers";

                var deleteButton = deleteStorage.button;
                deleteButton.onClick.ClearAll();
                deleteButton.onClick.AddListener(delegate ()
                {
                    RTEditor.inst.ShowWarningPopup("Are you sure you want to delete ALL markers? (This is irreversible!)", delegate ()
                    {
                        EditorManager.inst.DisplayNotification($"Deleted {Markers.Count} markers!", 2f, EditorManager.NotificationType.Success);
                        Markers.Clear();
                        MarkerEditor.inst.UpdateMarkerList();
                        MarkerEditor.inst.CreateMarkers();
                        EditorManager.inst.HideDialog("Warning Popup");
                        EditorManager.inst.HideDialog("Marker Editor");
                        CheckpointEditor.inst.SetCurrentCheckpoint(0);
                    }, delegate ()
                    {
                        EditorManager.inst.HideDialog("Warning Popup");
                    });
                });

                if (delete.GetComponent<HoverUI>())
                {
                    Destroy(delete.GetComponent<HoverUI>());
                }
                if (delete.GetComponent<HoverTooltip>())
                {
                    var tt = delete.GetComponent<HoverTooltip>();
                    tt.tooltipLangauges.Clear();
                    tt.tooltipLangauges.Add(TooltipHelper.NewTooltip("Delete all markers.", "Clicking this will delete every marker in the level.", new List<string>()));
                }

                EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete, delete, new List<Component>
                {
                    deleteButton.image,
                }, true, 1, SpriteManager.RoundedSide.W));

                EditorThemeManager.ApplyElement(new EditorThemeManager.Element(ThemeGroup.Delete_Text, deleteText.gameObject, new List<Component>
                {
                    deleteText,
                }));
            }

            int num = 0;
            foreach (var marker in Markers)
            {
                if (string.IsNullOrEmpty(Instance.sortedName) || marker.name.ToLower().Contains(Instance.sortedName.ToLower()) || marker.desc.ToLower().Contains(Instance.sortedName.ToLower()))
                {
                    var index = num;
                    var gameObject = Instance.markerButtonPrefab.Duplicate(parent, marker.name);

                    var name = gameObject.transform.Find("name").GetComponent<Text>();
                    var pos = gameObject.transform.Find("pos").GetComponent<Text>();
                    var image = gameObject.transform.Find("color").GetComponent<Image>();

                    name.text = marker.name;
                    pos.text = string.Format("{0:0}:{1:00}.{2:000}", Mathf.Floor(marker.time / 60f), Mathf.Floor(marker.time % 60f), Mathf.Floor(marker.time * 1000f % 1000f));

                    var markerColor = Instance.markerColors[Mathf.Clamp(marker.color, 0, Instance.markerColors.Count - 1)];
                    image.color = markerColor;
                    var button = gameObject.GetComponent<Button>();
                    button.onClick.AddListener(delegate ()
                    {
                        Instance.SetCurrentMarker(index, true);
                    });

                    TooltipHelper.AddHoverTooltip(gameObject, "<#" + LSColors.ColorToHex(markerColor) + ">" + marker.name + " [ " + marker.time + " ]</color>", marker.desc, new List<string>());

                    EditorThemeManager.ApplyGraphic(button.image, ThemeGroup.List_Button_2_Normal, true);
                    EditorThemeManager.ApplyGraphic(image, ThemeGroup.Null, true);
                    EditorThemeManager.ApplyGraphic(name, ThemeGroup.List_Button_2_Text);
                    EditorThemeManager.ApplyGraphic(pos, ThemeGroup.List_Button_2_Text);
                }
                num++;
            }
            return false;
        }

        [HarmonyPatch("CreateNewMarker", new Type[] { })]
        [HarmonyPrefix]
        static bool CreateNewMarkerPrefix()
        {
            Instance.CreateNewMarker(SettingEditor.inst.SnapActive ? RTEditor.SnapToBPM(EditorManager.inst.CurrentAudioPos) : EditorManager.inst.CurrentAudioPos);
            return false;
        }

        [HarmonyPatch("CreateNewMarker", new Type[] { typeof(float) })]
        [HarmonyPrefix]
        static bool CreateNewMarkerPrefix(float __0)
        {
            int index;
            if (!Markers.Has(x => __0 > x.time - 0.01f && __0 < x.time + 0.01f))
            {
                var marker = new DataManager.GameData.BeatmapData.Marker();
                marker.time = __0;
                marker.name = "";
                marker.color = Mathf.Clamp(EditorConfig.Instance.MarkerDefaultColor.Value, 0, Instance.markerColors.Count - 1);
                Markers.Add(marker);
                index = Markers.Count - 1;
            }
            else
            {
                index = Markers.FindIndex(x => __0 > x.time - 0.01f && __0 < x.time + 0.01f);
            }

            Instance.CreateMarkers();
            Instance.SetCurrentMarker(index);
            Instance.UpdateMarkerList();
            return false;
        }

        [HarmonyPatch("UpdateColorSelection")]
        [HarmonyPrefix]
        static bool UpdateColorSelectionPrefix()
        {
            var marker = Markers[Instance.currentMarker];
            int num = 0;
            foreach (var color in Instance.markerColors)
            {
                Instance.left.Find("color").GetChild(num).Find("Image").gameObject.SetActive(marker.color == num);
                num++;
            }
            return false;
        }

        [HarmonyPatch("DeleteMarker")]
        [HarmonyPrefix]
        static bool DeleteMarkerPrefix(int __0)
        {
            Markers.RemoveAt(__0);
            if (Markers.Count > 0)
                Instance.UpdateMarkerList();
            else
                CheckpointEditor.inst.SetCurrentCheckpoint(0);
            Instance.CreateMarkers();
            return false;
        }
    }
}
