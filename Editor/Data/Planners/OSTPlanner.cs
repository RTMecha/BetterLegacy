using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;
using LSFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

            if (audioType == AudioType.MPEG)
            {
                var audioClip = LSAudio.CreateAudioClipUsingMP3File(filePath);

                ProjectPlanner.inst.StopOST();

                var audioSource = Camera.main.gameObject.AddComponent<AudioSource>();

                ProjectPlanner.inst.OSTAudioSource = audioSource;
                ProjectPlanner.inst.currentOSTID = ID;
                ProjectPlanner.inst.currentOST = Index;
                ProjectPlanner.inst.playing = true;

                audioSource.clip = audioClip;
                audioSource.playOnAwake = true;
                audioSource.loop = false;
                audioSource.volume = DataManager.inst.GetSettingInt("MusicVolume", 9) / 9f * AudioManager.inst.masterVol;
                audioSource.Play();

                playing = true;

                return;
            }

            CoreHelper.StartCoroutine(AlephNetwork.DownloadAudioClip(filePath, audioType, audioClip =>
            {
                ProjectPlanner.inst.StopOST();

                var audioSource = Camera.main.gameObject.AddComponent<AudioSource>();

                ProjectPlanner.inst.OSTAudioSource = audioSource;
                ProjectPlanner.inst.currentOSTID = ID;
                ProjectPlanner.inst.currentOST = Index;
                ProjectPlanner.inst.playing = true;

                audioSource.clip = audioClip;
                audioSource.playOnAwake = true;
                audioSource.loop = false;
                audioSource.volume = DataManager.inst.GetSettingInt("MusicVolume", 9) / 9f * AudioManager.inst.masterVol;
                audioSource.Play();

                playing = true;
            }));
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
            button.onClick.AddListener(() => ProjectPlanner.inst.OpenOSTEditor(this));

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);

            TextUI = gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>();
            TextUI.text = Name;
            EditorThemeManager.ApplyLightText(TextUI);

            var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
            delete.button.onClick.ClearAll();
            delete.button.onClick.AddListener(() =>
            {
                ProjectPlanner.inst.osts.RemoveAll(x => x is OSTPlanner && x.ID == ID);
                ProjectPlanner.inst.SaveOST();

                if (ProjectPlanner.inst.currentOSTID == ID)
                    ProjectPlanner.inst.StopOST();

                CoreHelper.Destroy(gameObject);
            });

            EditorThemeManager.ApplyGraphic(delete.button.image, ThemeGroup.Delete, true);
            EditorThemeManager.ApplyGraphic(delete.image, ThemeGroup.Delete_Text);
        }
    }
}
