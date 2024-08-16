using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Components;

using TMPro;
using SimpleJSON;
using BetterLegacy.Core;
using BetterLegacy.Configs;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using LSFunctions;
using System.IO;
using BetterLegacy.Menus.UI.Interfaces;

namespace BetterLegacy.Menus.UI.Elements
{
    /// <summary>
    /// TODO: Make this the base for other menu items.
    /// Needs to be used for MenuImage.
    /// </summary>

    public class MenuImage
    {
        public GameObject gameObject;

        public string id;
        public string name;

        public string parentLayout;
        public string parent;
        public int siblingIndex = -1;

        public float length = 1f;

        public JSONNode rectJSON;
        public JSONNode funcJSON;

        public Clickable clickable;
        public Image image;
        public Sprite icon;

        public float opacity;
        public int color;

        public bool isSpawning;

        public float time;
        public float timeOffset;

        public bool playBlipSound;
        public int rounded = 1;
        public SpriteManager.RoundedSide roundedSide = SpriteManager.RoundedSide.W;

        public int loop;
        public bool fromLoop;

        public ReactiveSetting reactiveSetting;

        #region Methods

        public virtual void Spawn()
        {
            isSpawning = length != 0f;
            timeOffset = Time.time;
        }

        public virtual void Update()
        {
            time = Time.time - timeOffset * (InputDataManager.inst.menuActions.Submit.IsPressed ? 0.3f : 1f);
            if (time > length)
                isSpawning = false;
        }

        public void ParseFunction(JSONNode jn)
        {
            var parameters = jn["params"];

            string name = jn["name"];
            switch (name)
            {
                case "LoadScene":
                    {
                        if (parameters == null || parameters.Count < 1)
                            break;

                        LevelManager.IsArcade = parameters.Count >= 3 && Parser.TryParse(parameters[2], false);
                        DataManager.inst.UpdateSettingBool("IsArcade", parameters.Count >= 3 && Parser.TryParse(parameters[2], false));

                        if (parameters.Count >= 2)
                            SceneManager.inst.LoadScene(parameters[0], Parser.TryParse(parameters[1], true));
                        else
                            SceneManager.inst.LoadScene(parameters[0]);

                        break;
                    }
                case "Log":
                    {
                        if (parameters != null && parameters.Count >= 1)
                            CoreHelper.Log(parameters[0]);

                        break;
                    }
                case "ExitGame":
                    {
                        Application.Quit();
                        break;
                    }
                case "Config":
                    {
                        ConfigManager.inst.Show();
                        break;
                    }
                case "SetCurrentInterface":
                    {
                        if (parameters != null && parameters.Count >= 1)
                            NewMenuManager.inst.SetCurrentInterface(parameters[0]);

                        break;
                    }
                case "Reload":
                    {
                        NewMenuManager.inst.Test();

                        break;
                    }
                case "Parse":
                    {
                        if (parameters == null || parameters.Count < 1)
                            return;

                        if (parameters.Count > 2)
                            NewMenuManager.inst.MainDirectory = FontManager.TextTranslater.ReplaceProperties(parameters[2]);

                        var path = $"{NewMenuManager.inst.MainDirectory}{parameters[0].Value}.lsi";

                        if (!RTFile.FileExists(path))
                        {
                            CoreHelper.LogError($"Interface {parameters[0]} does not exist!");

                            return;
                        }

                        var interfaceJN = JSON.Parse(RTFile.ReadFromFile(path));

                        var menu = CustomMenu.Parse(interfaceJN);
                        menu.filePath = path;

                        if (NewMenuManager.inst.interfaces.Has(x => x.id == menu.id))
                        {
                            if (parameters.Count < 2 || Parser.TryParse(parameters[1], false))
                                NewMenuManager.inst.SetCurrentInterface(menu.id);

                            return;
                        }    

                        NewMenuManager.inst.interfaces.Add(menu);

                        if (parameters.Count < 2 || Parser.TryParse(parameters[1], false))
                            NewMenuManager.inst.SetCurrentInterface(menu.id);

                        break;
                    }
                case "DemoStoryMode":
                    {
                        DataManager.inst.UpdateSettingBool("IsArcade", false);
                        LevelManager.IsArcade = false;
                        SceneManager.inst.LoadScene("Input Select");
                        LevelManager.OnInputsSelected = () => { CoreHelper.StartCoroutine(Story.StoryManager.inst.Demo(true)); };

                        break;
                    }
                case "SetCurrentPath":
                    {
                        if (parameters == null || parameters.Count < 1)
                            return;

                        NewMenuManager.inst.MainDirectory = FontManager.TextTranslater.ReplaceProperties(parameters[0]);

                        break;
                    }
                case "PlaySound":
                    {
                        if (parameters == null || parameters.Count < 1 || NewMenuManager.inst.CurrentMenu == null)
                            return;

                        var filePath = $"{Path.GetDirectoryName(NewMenuManager.inst.CurrentMenu.filePath)}{parameters[0]}";
                        var audioType = RTFile.GetAudioType(filePath);
                        if (audioType == AudioType.MPEG)
                            AudioManager.inst.PlaySound(LSAudio.CreateAudioClipUsingMP3File(filePath));
                        else
                            CoreHelper.StartCoroutine(AlephNetworkManager.DownloadAudioClip($"file://{filePath}", audioType, AudioManager.inst.PlaySound));

                        break;
                    }
            }
        }

        #endregion
    }
}
