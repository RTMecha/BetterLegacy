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

namespace BetterLegacy.Menus.UI
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
                        if (parameters == null)
                            break;

                        if (parameters.Count >= 2)
                            SceneManager.inst.LoadScene(parameters[0], Parser.TryParse(parameters[1], true));
                        else if (parameters.Count >= 1)
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

                        var path = $"{RTFile.ApplicationDirectory}beatmaps/interfaces/{parameters[0]}.lsi";

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
                            CoreHelper.LogError($"Interface {parameters[0]} is already in the list!");

                            return;
                        }    

                        NewMenuManager.inst.interfaces.Add(menu);

                        if (parameters.Count < 2 || Parser.TryParse(parameters[1], false))
                            NewMenuManager.inst.SetCurrentInterface(menu.id);

                        break;
                    }
                case "DemoStoryMode":
                    {
                        CoreHelper.StartCoroutine(Story.StoryManager.inst.Demo());
                        break;
                    }
            }
        }

        #endregion
    }
}
