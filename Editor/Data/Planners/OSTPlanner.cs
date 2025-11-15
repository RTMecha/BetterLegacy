using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using TMPro;

using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Planners
{
    public class OSTPlanner : PlannerBase
    {
        public OSTPlanner() : base(Type.OST) { }

        public string Path { get; set; }
        public bool UseGlobal { get; set; }
        public string Name { get; set; }

        public int Index { get; set; }

        public TextMeshProUGUI TextUI { get; set; }
        public OpenHyperlinks Hyperlinks { get; set; }

        public bool playing;

        public bool Valid => RTFile.FileExists(UseGlobal ? Path : $"{RTFile.ApplicationDirectory}{Path}") && (Path.Contains(".ogg") || Path.Contains(".wav") || Path.Contains(".mp3"));

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
                if (!Hyperlinks.IsLinkHighlighted)
                    ProjectPlanner.inst.OpenOSTEditor(this);
            };

            var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
            delete.button.onClick.NewListener(() =>
            {
                ProjectPlanner.inst.osts.RemoveAll(x => x is OSTPlanner && x.ID == ID);
                ProjectPlanner.inst.SaveOST();

                if (ProjectPlanner.inst.currentOSTID == ID)
                    ProjectPlanner.inst.StopOST();

                CoreHelper.Destroy(gameObject);
            });

            EditorThemeManager.ApplyGraphic(delete.button.image, ThemeGroup.Delete, true);
            EditorThemeManager.ApplyGraphic(delete.image, ThemeGroup.Delete_Text);

            ProjectPlanner.inst.SetupPlannerLinks(Name, TextUI, Hyperlinks);

            gameObject.SetActive(false);
        }
    }
}
