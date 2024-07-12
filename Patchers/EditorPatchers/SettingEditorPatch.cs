using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;
using HarmonyLib;
using LSFunctions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(SettingEditor))]
    public class SettingEditorPatch : MonoBehaviour
    {
        static SettingEditor Instance { get => SettingEditor.inst; set => SettingEditor.inst = value; }

        static Dictionary<string, Text> info = new Dictionary<string, Text>();
        static Image doggo;

        static Transform markerColorsContent;
        static Transform layerColorsContent;

        static GameObject colorPrefab;

        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        static bool AwakePostfix(SettingEditor __instance)
        {
            if (Instance == null)
                Instance = __instance;
            else if (Instance != __instance)
            {
                Destroy(__instance.gameObject);
                return false;
            }

            CoreHelper.LogInit(__instance.className);

            var transform = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog").transform;

            //var dialog = EditorManager.inst.GetDialog("Settings Editor").Dialog;

            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Background_1, transform.gameObject, new List<Component>
            {
                transform.GetComponent<Image>(),
            }));

            var snap = transform.Find("snap");

            var slider = snap.Find("bpm/slider").gameObject.GetComponent<Slider>();
            slider.maxValue = 999f;
            slider.minValue = 0f;

            DestroyImmediate(snap.Find("bpm/<").gameObject);
            DestroyImmediate(snap.Find("bpm/>").gameObject);
            EditorThemeManager.AddToggle(snap.Find("toggle/toggle").GetComponent<Toggle>());
            EditorThemeManager.AddLightText(snap.Find("toggle/title").GetComponent<Text>());
            EditorThemeManager.AddLightText(snap.Find("bpm/title").GetComponent<Text>());
            snap.Find("toggle/title").AsRT().sizeDelta = new Vector2(100f, 32f);
            snap.Find("bpm/title").AsRT().sizeDelta = new Vector2(100f, 32f);

            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Light_Text, snap.transform.Find("title_/Panel/icon").gameObject, new List<Component>
            {
                snap.transform.Find("title_/Panel/icon").GetComponent<Image>(),
            }));
            EditorThemeManager.AddLightText(snap.transform.Find("title_/title").GetComponent<Text>());

            var bpmSlider = snap.Find("bpm/slider").GetComponent<Slider>();
            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Slider_2, bpmSlider.transform.Find("Background").gameObject, new List<Component>
            {
                bpmSlider.transform.Find("Background").GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Slider_2_Handle, bpmSlider.gameObject, new List<Component>
            {
                bpmSlider.image,
            }, true, 1, SpriteManager.RoundedSide.W));
            EditorThemeManager.AddInputField(snap.Find("bpm/input").GetComponent<InputField>());

            var snapOffset = snap.Find("bpm").gameObject.Duplicate(transform.Find("snap"), "bpm offset");
            var snapOffsetText = snapOffset.transform.Find("title").GetComponent<Text>();
            snapOffsetText.text = "BPM Offset";
            snapOffsetText.rectTransform.sizeDelta = new Vector2(100f, 32f);
            EditorThemeManager.AddLightText(snapOffsetText);

            var bpmOffsetSlider = snapOffset.transform.Find("slider").GetComponent<Slider>();
            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Slider_2, bpmOffsetSlider.transform.Find("Background").gameObject, new List<Component>
            {
                bpmOffsetSlider.transform.Find("Background").GetComponent<Image>(),
            }, true, 1, SpriteManager.RoundedSide.W));

            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Slider_2_Handle, bpmOffsetSlider.gameObject, new List<Component>
            {
                bpmOffsetSlider.image,
            }, true, 1, SpriteManager.RoundedSide.W));
            EditorThemeManager.AddInputField(snapOffset.transform.Find("input").GetComponent<InputField>());

            snap.AsRT().sizeDelta = new Vector2(765f, 140f);

            var title1 = snap.GetChild(0).gameObject.Duplicate(transform, "info title");
            var editorInformationText = title1.transform.Find("title").GetComponent<Text>();
            editorInformationText.text = "Editor Information";

            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Light_Text, title1.transform.Find("Panel/icon").gameObject, new List<Component>
            {
                title1.transform.Find("Panel/icon").GetComponent<Image>(),
            }));
            EditorThemeManager.AddLightText(editorInformationText);

            info.Clear();
            var scrollView = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View").gameObject.Duplicate(transform, "Scroll View");
            scrollView.transform.AsRT().sizeDelta = new Vector2(765f, 120f);
            LSHelpers.DeleteChildren(scrollView.transform.Find("Viewport/Content"));

            string[] array = new string[]
            {
                "Object Count",
                "Total Object Count",
                "Event Count",
                "Theme Count",
                "Prefab External Count",
                "Prefab Internal Count",
                "Prefab Objects Count",
                "No Autokill Count",
                "Keyframe Offsets > Song Length Count",
                "Text Object Count",
                "Text Symbol Total Count",
                "Timeline Objects in Current Layer Count",
                "Markers Count",
                "Objects Alive Count",
                "Time in Editor",
                "Level opened amount",
                "Song Progress",
                "Camera Position",
                "Camera Zoom",
                "Camera Rotation",
            };

            for (int i = 0; i < array.Length; i++)
            {
                var baseInfo = new GameObject("Info");
                baseInfo.transform.SetParent(scrollView.transform.Find("Viewport/Content"));
                baseInfo.transform.localScale = Vector3.one;
                var iRT = baseInfo.AddComponent<RectTransform>();
                iRT.sizeDelta = new Vector2(750f, 32f);
                var iImage = baseInfo.AddComponent<Image>();

                iImage.color = new Color(1f, 1f, 1f, 0.12f);

                EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.List_Button_1_Normal, baseInfo, new List<Component>
                {
                    iImage,
                }, true, 1, SpriteManager.RoundedSide.W));

                var iHLG = baseInfo.AddComponent<HorizontalLayoutGroup>();

                var title = new GameObject("Title");
                title.transform.SetParent(iRT);
                title.transform.localScale = Vector3.one;
                title.AddComponent<RectTransform>();

                var titleText = title.AddComponent<Text>();
                titleText.font = FontManager.inst.DefaultFont;
                titleText.fontSize = 19;
                titleText.alignment = TextAnchor.MiddleLeft;
                titleText.text = "  " + array[i];

                EditorThemeManager.AddLightText(titleText);

                var infoGO = new GameObject("Title");
                infoGO.transform.SetParent(iRT);
                infoGO.transform.localScale = Vector3.one;
                infoGO.AddComponent<RectTransform>();

                var infoText = infoGO.AddComponent<Text>();
                infoText.font = FontManager.inst.DefaultFont;
                infoText.fontSize = 19;
                infoText.alignment = TextAnchor.MiddleRight;
                infoText.text = "[ 0 ]";

                EditorThemeManager.AddLightText(infoText);

                info.Add(array[i], infoText);
            }

            // Doggo
            var loadingDoggo = new GameObject("loading doggo");
            loadingDoggo.transform.parent = transform;
            var loadingDoggoRect = loadingDoggo.AddComponent<RectTransform>();
            loadingDoggo.AddComponent<CanvasRenderer>();
            doggo = loadingDoggo.AddComponent<Image>();
            var loadingDoggoLE = loadingDoggo.AddComponent<LayoutElement>();

            loadingDoggoRect.anchoredPosition = new Vector2(UnityEngine.Random.Range(-320f, 320f), UnityEngine.Random.Range(-300f, -275f));
            float sizeRandom = 64f * UnityEngine.Random.Range(0.5f, 1f);
            loadingDoggoRect.sizeDelta = new Vector2(sizeRandom, sizeRandom);

            loadingDoggoLE.ignoreLayout = true;

            var title2 = transform.Find("snap").GetChild(0).gameObject.Duplicate(transform, "marker colors title");
            var markerColorsText = title2.transform.Find("title").GetComponent<Text>();
            markerColorsText.text = "Marker Colors";
            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Light_Text, title2.transform.Find("Panel/icon").gameObject, new List<Component>
            {
                title2.transform.Find("Panel/icon").GetComponent<Image>(),
            }));

            EditorThemeManager.AddLightText(markerColorsText);

            // Marker Colors
            {
                var markersScrollView = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View").gameObject.Duplicate(transform, "Scroll View");
                markersScrollView.transform.AsRT().sizeDelta = new Vector2(765f, 120f);
                LSHelpers.DeleteChildren(markersScrollView.transform.Find("Viewport/Content"));

                markerColorsContent = markersScrollView.transform.Find("Viewport/Content");
            }

            var title3 = transform.Find("snap").GetChild(0).gameObject.Duplicate(transform, "layer colors title");
            var layerColorsText = title3.transform.Find("title").GetComponent<Text>();
            layerColorsText.text = "Layer Colors";
            EditorThemeManager.AddElement(new EditorThemeManager.Element(ThemeGroup.Light_Text, title3.transform.Find("Panel/icon").gameObject, new List<Component>
            {
                title3.transform.Find("Panel/icon").GetComponent<Image>(),
            }));

            EditorThemeManager.AddLightText(layerColorsText);

            // Layer Colors
            {
                var layersScrollView = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View").gameObject.Duplicate(transform, "Scroll View");
                layersScrollView.transform.AsRT().sizeDelta = new Vector2(765f, 120f);
                LSHelpers.DeleteChildren(layersScrollView.transform.Find("Viewport/Content"));

                layerColorsContent = layersScrollView.transform.Find("Viewport/Content");
            }

            Instance.StartCoroutine(Wait());

            return false;
        }

        static IEnumerator Wait()
        {
            yield return new WaitForSeconds(0.4f);

            colorPrefab = new GameObject("Color");
            colorPrefab.transform.SetParent(SettingEditor.inst.transform);
            var tagPrefabRT = colorPrefab.AddComponent<RectTransform>();
            var tagPrefabImage = colorPrefab.AddComponent<Image>();
            tagPrefabImage.color = new Color(1f, 1f, 1f, 0.12f);
            var tagPrefabLayout = colorPrefab.AddComponent<HorizontalLayoutGroup>();
            tagPrefabLayout.childControlWidth = false;
            tagPrefabLayout.childForceExpandWidth = false;

            var input = RTEditor.inst.defaultIF.Duplicate(tagPrefabRT, "Input");
            input.transform.localScale = Vector3.one;
            ((RectTransform)input.transform).sizeDelta = new Vector2(136f, 32f);
            var text = input.transform.Find("Text").GetComponent<Text>();
            text.alignment = TextAnchor.MiddleLeft;
            text.fontSize = 17;

            var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(tagPrefabRT, "Delete");
            UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(748f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.one, new Vector2(32f, 32f));

            var transform = EditorManager.inst.GetDialog("Settings Editor").Dialog;
            var analyzeBPM = EditorPrefabHolder.Instance.Function2Button.Duplicate(transform.Find("snap/toggle"), "analyze");
            analyzeBPM.transform.AsRT().sizeDelta = new Vector2(140f, 32f);
            var analyzeBPMStorage = analyzeBPM.GetComponent<FunctionButtonStorage>();
            analyzeBPMStorage.text.text = "Analyze BPM";
            analyzeBPMStorage.button.onClick.ClearAll();
            analyzeBPMStorage.button.onClick.AddListener(delegate ()
            {
                CoreHelper.StartCoroutineAsync(UniBpmAnalyzer.IAnalyzeBPM(AudioManager.inst.CurrentAudioSource.clip, delegate (int bpm)
                {
                    EditorManager.inst.DisplayNotification($"Detected a BPM of {bpm}! Applied it to editor settings.", 2f, EditorManager.NotificationType.Success);

                    transform.Find("snap/bpm/input").GetComponent<InputField>().text = bpm.ToString();
                }));
            });

            EditorThemeManager.AddSelectable(analyzeBPMStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(analyzeBPMStorage.text, ThemeGroup.Function_2_Text);
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePostfix()
        {
            if (EditorManager.inst && EditorManager.inst.isEditing && EditorManager.inst.hasLoadedLevel &&
                DataManager.inst.gameData != null && DataManager.inst.gameData.eventObjects != null &&
                PrefabEditorManager.inst)
            {
                var transform = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog").transform;

                if (info.ContainsKey("Object Count") && info["Object Count"])
                {
                    info["Object Count"].text = $"[ {DataManager.inst.gameData.beatmapObjects.Where(x => !x.fromPrefab).Count()} ]  ";
                }
                if (info.ContainsKey("Total Object Count") && info["Total Object Count"])
                {
                    info["Total Object Count"].text = $"[ {DataManager.inst.gameData.beatmapObjects.Count} ]  ";
                }
                if (info.ContainsKey("Event Count") && info["Event Count"])
                {
                    int num = 0;
                    for (int i = 0; i < DataManager.inst.gameData.eventObjects.allEvents.Count; i++)
                        num += DataManager.inst.gameData.eventObjects.allEvents[i].Count;

                    info["Event Count"].text = $"[ {num} ]  ";
                }
                if (info.ContainsKey("Theme Count") && info["Theme Count"])
                {
                    info["Theme Count"].text = $"[ {DataManager.inst.AllThemes.Count} ]  ";
                }
                if (info.ContainsKey("Prefab External Count") && info["Prefab External Count"])
                {
                    info["Prefab External Count"].text = $"[ {PrefabEditorManager.inst.PrefabPanels.Count} ]  ";
                }
                if (info.ContainsKey("Prefab Internal Count") && info["Prefab Internal Count"])
                {
                    info["Prefab Internal Count"].text = $"[ {DataManager.inst.gameData.prefabs.Count} ]  ";
                }
                if (info.ContainsKey("Prefab Objects Count") && info["Prefab Objects Count"])
                {
                    info["Prefab Objects Count"].text = $"[ {DataManager.inst.gameData.prefabObjects.Count} ]  ";
                }
                if (info.ContainsKey("No Autokill Count") && info["No Autokill Count"])
                {
                    info["No Autokill Count"].text = $"[ {DataManager.inst.gameData.beatmapObjects.Where(x => x.autoKillType == DataManager.GameData.BeatmapObject.AutoKillType.OldStyleNoAutokill).Count()} ]  ";
                }
                if (info.ContainsKey("Keyframe Offsets > Song Length Count") && info["Keyframe Offsets > Song Length Count"])
                {
                    info["Keyframe Offsets > Song Length Count"].text = $"[ {DataManager.inst.gameData.beatmapObjects.Where(x => x.autoKillOffset > AudioManager.inst.CurrentAudioSource.clip.length).Count()} ]  ";
                }
                if (info.ContainsKey("Text Object Count") && info["Text Object Count"])
                {
                    info["Text Object Count"].text = $"[ {DataManager.inst.gameData.beatmapObjects.Where(x => x.shape == 4 && x.objectType != DataManager.GameData.BeatmapObject.ObjectType.Empty).Count()} ]  ";
                }
                if (info.ContainsKey("Text Symbol Total Count") && info["Text Symbol Total Count"])
                {
                    int num = 0;
                    foreach (var bm in DataManager.inst.gameData.beatmapObjects.Where(x => x.shape == 4 && x.objectType != DataManager.GameData.BeatmapObject.ObjectType.Empty))
                    {
                        num += bm.text.Length;
                    }

                    info["Text Symbol Total Count"].text = $"[ {num} ]  ";
                }
                if (info.ContainsKey("Timeline Objects in Current Layer Count") && info["Timeline Objects in Current Layer Count"])
                {
                    info["Timeline Objects in Current Layer Count"].text = $"[ {RTEditor.inst.timelineObjects.Where(x => x.Layer == EditorManager.inst.layer).Count()} ]  ";
                }
                if (info.ContainsKey("Markers Count") && info["Markers Count"])
                {
                    info["Markers Count"].text = $"[ {DataManager.inst.gameData.beatmapData.markers.Count} ]  ";
                }
                if (info.ContainsKey("Objects Alive Count") && info["Objects Alive Count"])
                {
                    info["Objects Alive Count"].text = $"[ {DataManager.inst.gameData.beatmapObjects.Where(x => x is BeatmapObject beatmapObject && beatmapObject.Alive).Count()} ]  ";
                }
                if (info.ContainsKey("Time in Editor") && info["Time in Editor"])
                {
                    info["Time in Editor"].text = $"[ {FontManager.TextTranslater.SecondsToTime(RTEditor.inst.timeEditing)} ]  ";
                }
                if (info.ContainsKey("Level opened amount") && info["Level opened amount"])
                {
                    info["Level opened amount"].text = $"[ {RTEditor.inst.openAmount} ]  ";
                }
                if (info.ContainsKey("Song Progress") && info["Song Progress"])
                {
                    info["Song Progress"].text = $"[ {FontManager.TextTranslater.Percentage(AudioManager.inst.CurrentAudioSource.time, AudioManager.inst.CurrentAudioSource.clip.length)}% ]  ";
                }

                if (info.ContainsKey("Camera Position") && info["Camera Position"])
                    info["Camera Position"].text = $"[ X: {Camera.main.transform.position.x}, Y: {Camera.main.transform.position.y} ]  ";

                if (info.ContainsKey("Camera Zoom") && info["Camera Zoom"])
                    info["Camera Zoom"].text = $"[ {Camera.main.orthographicSize} ]  ";

                if (info.ContainsKey("Camera Rotation") && info["Camera Rotation"])
                    info["Camera Rotation"].text = $"[ {Camera.main.transform.rotation.eulerAngles.z} ]  ";

                if (doggo)
                    doggo.sprite = EditorManager.inst.loadingImage.sprite;
            }
        }

        static void SetBPMSlider(Slider slider, InputField input)
        {
            slider.onValueChanged.RemoveAllListeners();
            slider.value = SettingEditor.inst.SnapBPM;
            slider.onValueChanged.AddListener(delegate (float _val)
            {
                DataManager.inst.metaData.song.BPM = _val;
                SettingEditor.inst.SnapBPM = _val;
                SetBPMInputField(slider, input);
                RTEditor.inst.SetTimelineGridSize();
            });
        }

        static void SetBPMInputField(Slider slider, InputField input)
        {
            input.onValueChanged.RemoveAllListeners();
            input.text = SettingEditor.inst.SnapBPM.ToString();
            input.onValueChanged.AddListener(delegate (string _val)
            {
                var bpm = Parser.TryParse(_val, 120f);
                DataManager.inst.metaData.song.BPM = bpm;
                SettingEditor.inst.SnapBPM = bpm;
                SetBPMSlider(slider, input);
                RTEditor.inst.SetTimelineGridSize();
            });
        }

        static void SetBPMOffsetSlider(Slider slider, InputField input)
        {
            slider.onValueChanged.RemoveAllListeners();
            slider.value = RTEditor.inst.bpmOffset;
            slider.onValueChanged.AddListener(delegate (float _val)
            {
                RTEditor.inst.bpmOffset = _val;
                SetBPMOffsetInputField(slider, input);
                RTEditor.inst.SetTimelineGridSize();
                RTEditor.inst.SaveSettings();
            });
        }

        static void SetBPMOffsetInputField(Slider slider, InputField input)
        {
            input.onValueChanged.RemoveAllListeners();
            input.text = RTEditor.inst.bpmOffset.ToString();
            input.onValueChanged.AddListener(delegate (string _val)
            {
                var bpm = Parser.TryParse(_val, 0f);
                RTEditor.inst.bpmOffset = bpm;
                SetBPMOffsetSlider(slider, input);
                RTEditor.inst.SetTimelineGridSize();
                RTEditor.inst.SaveSettings();
            });
        }

        [HarmonyPatch("Render")]
        [HarmonyPrefix]
        static bool RenderPrefix()
        {
            EditorManager.inst.CancelInvoke("LoadingIconUpdate");
            EditorManager.inst.InvokeRepeating("LoadingIconUpdate", 0f, UnityEngine.Random.Range(0.01f, 0.4f));

            EditorManager.inst.ClearDialogs();
            EditorManager.inst.ShowDialog("Settings Editor");

            var transform = EditorManager.inst.GetDialog("Settings Editor").Dialog;
            var loadingDoggoRect = transform.Find("loading doggo").GetComponent<RectTransform>();

            loadingDoggoRect.anchoredPosition = new Vector2(UnityEngine.Random.Range(-320f, 320f), UnityEngine.Random.Range(-310f, -340f));
            float sizeRandom = 64 * UnityEngine.Random.Range(0.5f, 1f);
            loadingDoggoRect.sizeDelta = new Vector2(sizeRandom, sizeRandom);

            var toggle = transform.Find("snap/toggle/toggle").GetComponent<Toggle>();
            toggle.onValueChanged.RemoveAllListeners();
            toggle.isOn = SettingEditor.inst.SnapActive;
            toggle.onValueChanged.AddListener(delegate (bool _val)
            {
                SettingEditor.inst.SnapActive = _val;
            });

            var slider = transform.Find("snap/bpm/slider").GetComponent<Slider>();
            var input = transform.Find("snap/bpm/input").GetComponent<InputField>();
            SetBPMSlider(slider, input);
            SetBPMInputField(slider, input);

            TriggerHelper.AddEventTriggerParams(input.gameObject,
                TriggerHelper.ScrollDelta(input, 1f));

            var sliderOffset = transform.Find("snap/bpm offset/slider").GetComponent<Slider>();
            var inputOffset = transform.Find("snap/bpm offset/input").GetComponent<InputField>();
            SetBPMOffsetSlider(sliderOffset, inputOffset);
            SetBPMOffsetInputField(sliderOffset, inputOffset);

            TriggerHelper.AddEventTriggerParams(inputOffset.gameObject,
                TriggerHelper.ScrollDelta(inputOffset));

            RenderMarkerColors();
            RenderLayerColors();

            return false;
        }

        public static void RenderMarkerColors()
        {
            LSHelpers.DeleteChildren(markerColorsContent);

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(markerColorsContent, "Add");

            ((RectTransform)add.transform).sizeDelta = new Vector2(402f, 32f);
            var addText = add.transform.Find("Text").GetComponent<Text>();
            addText.text = "Add Marker Color";
            var addButton = add.GetComponent<Button>();
            addButton.onClick.ClearAll();
            addButton.onClick.AddListener(delegate ()
            {
                MarkerEditor.inst.markerColors.Add(LSColors.pink500);
                RTEditor.inst.SaveGlobalSettings();
                RenderMarkerColors();
            });

            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text);

            int num = 0;
            foreach (var markerColor in MarkerEditor.inst.markerColors)
            {
                int index = num;

                var gameObject = colorPrefab.Duplicate(markerColorsContent, "Color");
                gameObject.transform.AsRT().sizeDelta = new Vector2(402f, 32f);
                var image = gameObject.GetComponent<Image>();
                image.color = markerColor;

                EditorThemeManager.ApplyGraphic(image, ThemeGroup.Null, true, 2);

                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.onValueChanged.ClearAll();
                input.onEndEdit.ClearAll();
                input.text = LSColors.ColorToHex(markerColor);
                input.onValueChanged.AddListener(delegate (string _val)
                {
                    MarkerEditor.inst.markerColors[index] = _val.Length == 6 ? LSColors.HexToColor(_val) : LSColors.pink500;
                    image.color = MarkerEditor.inst.markerColors[index];
                });
                input.onEndEdit.AddListener(delegate (string _val)
                {
                    RTEditor.inst.SaveGlobalSettings();
                });

                EditorThemeManager.ApplyInputField(input);

                var deleteStorage = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.ClearAll();
                deleteStorage.button.onClick.AddListener(delegate ()
                {
                    MarkerEditor.inst.markerColors.RemoveAt(index);
                    RenderMarkerColors();
                    RTEditor.inst.SaveGlobalSettings();
                });

                EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete);
                EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);

                num++;
            }
        }

        public static void RenderLayerColors()
        {
            LSHelpers.DeleteChildren(layerColorsContent);

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(layerColorsContent, "Add");

            ((RectTransform)add.transform).sizeDelta = new Vector2(402f, 32f);
            var addText = add.transform.Find("Text").GetComponent<Text>();
            addText.text = "Add Layer Color";
            var addButton = add.GetComponent<Button>();
            addButton.onClick.ClearAll();
            addButton.onClick.AddListener(delegate ()
            {
                EditorManager.inst.layerColors.Add(LSColors.pink500);
                RTEditor.inst.SaveGlobalSettings();
                RenderLayerColors();
            });

            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text);

            int num = 0;
            foreach (var layerColor in EditorManager.inst.layerColors)
            {
                int index = num;

                var gameObject = colorPrefab.Duplicate(layerColorsContent, "Color");
                gameObject.transform.AsRT().sizeDelta = new Vector2(402f, 32f);
                var image = gameObject.GetComponent<Image>();
                image.color = layerColor;

                EditorThemeManager.ApplyGraphic(image, ThemeGroup.Null, true, 2);

                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.onValueChanged.ClearAll();
                input.onEndEdit.ClearAll();
                input.text = LSColors.ColorToHex(layerColor);
                input.onValueChanged.AddListener(delegate (string _val)
                {
                    EditorManager.inst.layerColors[index] = _val.Length == 6 ? LSColors.HexToColor(_val) : LSColors.pink500;
                    image.color = EditorManager.inst.layerColors[index];
                });
                input.onEndEdit.AddListener(delegate (string _val)
                {
                    RTEditor.inst.SaveGlobalSettings();
                });

                EditorThemeManager.ApplyInputField(input);

                var deleteStorage = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.ClearAll();
                deleteStorage.button.onClick.AddListener(delegate ()
                {
                    EditorManager.inst.layerColors.RemoveAt(index);
                    RenderLayerColors();
                    RTEditor.inst.SaveGlobalSettings();
                });

                EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete);
                EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);

                num++;
            }
        }
    }
}
