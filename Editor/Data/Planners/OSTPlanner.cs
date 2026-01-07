using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using TMPro;
using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Planners
{
    public class OSTPlanner : PlannerBase<OSTPlanner>
    {
        public OSTPlanner() : base() { }

        public string Path { get; set; }
        public bool UseGlobal { get; set; }
        public string Name { get; set; }

        public int Index { get; set; }

        public TextMeshProUGUI TextUI { get; set; }
        public OpenHyperlinks Hyperlinks { get; set; }

        public bool playing;

        public bool Valid => RTFile.FileExists(UseGlobal ? Path : $"{RTFile.ApplicationDirectory}{Path}") && (Path.Contains(".ogg") || Path.Contains(".wav") || Path.Contains(".mp3"));

        public override Type PlannerType => Type.OST;

        public void Play()
        {
            var filePath = UseGlobal ? Path : $"{RTFile.ApplicationDirectory}{Path}";

            if (!RTFile.FileExists(filePath))
                return;

            var audioType = RTFile.GetAudioType(Path);

            if (audioType == AudioType.UNKNOWN)
                return;

            ProjectPlanner.inst.StopOST(); // stops the currently playing OST

            if (audioType == AudioType.MPEG)
            {
                PlayAudio(LSAudio.CreateAudioClipUsingMP3File(filePath));
                return;
            }

            CoroutineHelper.StartCoroutineAsync(AlephNetwork.DownloadAudioClip(filePath, audioType, audioClip => LegacyPlugin.MainTick += () => PlayAudio(audioClip)));
        }

        void PlayAudio(AudioClip audioClip)
        {
            CoreHelper.Log($"Started playing OST {Name}");

            var audioSource = Camera.main.gameObject.AddComponent<AudioSource>();

            ProjectPlanner.inst.OSTAudioSource = audioSource;
            ProjectPlanner.inst.currentOSTID = ID;
            ProjectPlanner.inst.currentOST = Index;
            ProjectPlanner.inst.playing = true;

            audioSource.clip = audioClip;
            audioSource.playOnAwake = true;
            audioSource.loop = false;
            audioSource.volume = SoundManager.inst.MusicVolume * AudioManager.inst.masterVol;
            audioSource.Play();

            playing = true;
            Notify();
        }

        void Notify()
        {
            var name = Name;
            ProjectPlanner.inst.SetupPlannerLinks(name, null, false, _val => name = _val);
            CoreHelper.Notify($"Now playing: {name}", EditorThemeManager.CurrentTheme.ColorGroups.GetOrDefault(ThemeGroup.Light_Text, Color.white));
        }

        public override void Init()
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = ProjectPlanner.inst.prefabs[6].Duplicate(ProjectPlanner.inst.content, "ost");
            gameObject.transform.localScale = Vector3.one;
            GameObject = gameObject;

            var button = gameObject.GetComponent<Button>();
            button.onClick.ClearAll();

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);

            TextUI = gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>();
            TextUI.text = Name;
            EditorThemeManager.ApplyLightText(TextUI);

            Hyperlinks = gameObject.AddComponent<OpenHyperlinks>();
            Hyperlinks.Text = TextUI;
            Hyperlinks.onClick = eventData =>
            {
                if (Hyperlinks.IsLinkHighlighted)
                    return;

                if (eventData.button == UnityEngine.EventSystems.PointerEventData.InputButton.Right)
                {
                    var buttonFunctions = new List<EditorElement>
                    {
                        new ButtonElement("Edit", () => ProjectPlanner.inst.OpenOSTEditor(this)),
                        new ButtonElement("Delete", () =>
                        {
                            ProjectPlanner.inst.osts.RemoveAll(x => x is OSTPlanner && x.ID == ID);
                            ProjectPlanner.inst.SaveOST();

                            if (ProjectPlanner.inst.currentOSTID == ID)
                                ProjectPlanner.inst.StopOST();

                            CoreHelper.Destroy(gameObject);
                        }),
                        new SpacerElement(),
                        new ButtonElement("Copy", () =>
                        {
                            ProjectPlanner.inst.copiedPlanners.Clear();
                            ProjectPlanner.inst.copiedPlanners.Add(this);
                            EditorManager.inst.DisplayNotification("Copied OST!", 2f, EditorManager.NotificationType.Success);
                        }),
                        new ButtonElement("Copy Selected", ProjectPlanner.inst.CopySelectedPlanners),
                        new ButtonElement("Copy Current Tab", ProjectPlanner.inst.CopyCurrentTabPlanners),
                        new ButtonElement("Paste", ProjectPlanner.inst.PastePlanners),
                        new SpacerElement(),
                    };

                    buttonFunctions.AddRange(EditorContextMenu.GetMoveIndexFunctions(ProjectPlanner.inst.osts, () => ProjectPlanner.inst.osts.IndexOf(this), () =>
                    {
                        for (int i = 0; i < ProjectPlanner.inst.osts.Count; i++)
                            ProjectPlanner.inst.osts[i].Init();
                        ProjectPlanner.inst.RefreshList();
                    }));

                    EditorContextMenu.inst.ShowContextMenu(buttonFunctions);
                    return;
                }

                if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
                {
                    Selected = !Selected;
                    return;
                }

                ProjectPlanner.inst.OpenOSTEditor(this);
            };

            var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
            delete.OnClick.NewListener(() => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this OST?", () =>
            {
                ProjectPlanner.inst.osts.RemoveAll(x => x is OSTPlanner && x.ID == ID);
                ProjectPlanner.inst.SaveOST();

                if (ProjectPlanner.inst.currentOSTID == ID)
                    ProjectPlanner.inst.StopOST();

                CoreHelper.Destroy(gameObject);
            }));

            EditorThemeManager.ApplyDeleteButton(delete);

            ProjectPlanner.inst.SetupPlannerLinks(Name, TextUI, Hyperlinks);

            InitSelectedUI();

            gameObject.SetActive(false);
        }

        public override void ReadJSON(JSONNode jn)
        {
            Name = jn["name"];
            Path = !string.IsNullOrEmpty(jn["path"]) ? jn["path"] : string.Empty;
            UseGlobal = jn["use_global"].AsBool;
            Index = jn["index"].AsInt;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["name"] = Name;
            if (!string.IsNullOrEmpty(Path))
                jn["path"] = Path;
            jn["use_global"] = UseGlobal;
            jn["index"] = Index;

            return jn;
        }

        public override OSTPlanner CreateCopy() => new OSTPlanner
        {
            Path = Path,
            UseGlobal = UseGlobal,
            Name = Name,
            Index = Index,
        };

        public override bool SamePlanner(PlannerBase other) => other is OSTPlanner ost && ost.Name == Name;
    }
}
