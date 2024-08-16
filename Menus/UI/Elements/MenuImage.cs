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
    /// Base class used for handling image elements and other types in the interface. To be used either as a base for other elements or an image element on its own.
    /// </summary>
    public class MenuImage
    {
        /// <summary>
        /// GameObject reference.
        /// </summary>
        public GameObject gameObject;

        /// <summary>
        /// Identification of the element.
        /// </summary>
        public string id;

        /// <summary>
        /// Name of the element.
        /// </summary>
        public string name;

        /// <summary>
        /// The layout to parent this element to.
        /// </summary>
        public string parentLayout;

        /// <summary>
        /// The element ID used to parent this element to another if <see cref="parentLayout"/> does not have an associated layout.
        /// </summary>
        public string parent;

        /// <summary>
        /// Unity's UI layering depends on an objects' sibling index, so it can be set via here if you want an element to appear below layouts.
        /// </summary>
        public int siblingIndex = -1;

        /// <summary>
        /// Spawn length of the element, to be used for spacing each element out. Time is not always exactly a second.
        /// </summary>
        public float length = 1f;

        /// <summary>
        /// RectTransform values in a JSON format.
        /// </summary>
        public JSONNode rectJSON;

        /// <summary>
        /// Function JSON to parse whenever the element is clicked.
        /// </summary>
        public JSONNode funcJSON;

        /// <summary>
        /// Interaction component.
        /// </summary>
        public Clickable clickable;

        /// <summary>
        /// Base image of the element.
        /// </summary>
        public Image image;

        /// <summary>
        /// Icon to apply to <see cref="image"/>, or <see cref="MenuText.iconUI"/> if the element type is <see cref="MenuText"/> or <see cref="MenuButton"/>.
        /// </summary>
        public Sprite icon;

        /// <summary>
        /// Opacity of the image.
        /// </summary>
        public float opacity;

        /// <summary>
        /// Theme color slot for the image to use.
        /// </summary>
        public int color;

        /// <summary>
        /// True if the element is spawning (playing spawn animations, etc), otherwise false.
        /// </summary>
        public bool isSpawning;

        /// <summary>
        /// Time elapsed since spawn.
        /// </summary>
        public float time;

        /// <summary>
        /// Time when element spawned.
        /// </summary>
        public float timeOffset;

        /// <summary>
        /// If a "Blip" sound should play when the user clicks on the element.
        /// </summary>
        public bool playBlipSound;

        /// <summary>
        /// How rounded the element should be. If the value is 0, then the element is not rounded.
        /// </summary>
        public int rounded = 1;

        /// <summary>
        /// The side that should be rounded, if <see cref="rounded"/> if higher than 0.
        /// </summary>
        public SpriteManager.RoundedSide roundedSide = SpriteManager.RoundedSide.W;

        /// <summary>
        /// How many times the element should loop.
        /// </summary>
        public int loop;

        /// <summary>
        /// If the element was spawned from a loop.
        /// </summary>
        public bool fromLoop;

        /// <summary>
        /// Contains all reactive settings.
        /// </summary>
        public ReactiveSetting reactiveSetting;

        #region Methods

        /// <summary>
        /// Runs when the elements' GameObject has been created.
        /// </summary>
        public virtual void Spawn()
        {
            isSpawning = length != 0f;
            timeOffset = Time.time;
        }

        /// <summary>
        /// Runs while the element is spawning.
        /// </summary>
        public virtual void UpdateSpawnCondition()
        {
            time = Time.time - timeOffset * (InputDataManager.inst.menuActions.Submit.IsPressed ? 0.3f : 1f);
            if (time > length)
                isSpawning = false;
        }

        /// <summary>
        /// Parses the "func" JSON and performs an action based on the name and parameters.
        /// </summary>
        /// <param name="jn">The func JSON. Must have a name and a params array.</param>
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
                        NewMenuManager.inst.StartupInterface();

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
                case "SetDiscordStatus":
                    {
                        if (parameters == null || parameters.Count < 1)
                            return;

                        CoreHelper.UpdateDiscordStatus(parameters[0], parameters[1], parameters[2], parameters.Count > 3 ? parameters[3] : "pa_logo_white");

                        break;
                    }
            }
        }

        /// <summary>
        /// Provides a way to see the object in UnityExplorer.
        /// </summary>
        /// <returns>A string containing the objects' ID and name.</returns>
        public override string ToString() => $"{id} - {name}";

        #endregion
    }
}
