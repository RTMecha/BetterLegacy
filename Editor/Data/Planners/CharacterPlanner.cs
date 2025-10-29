﻿using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using TMPro;
using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Planners
{
    public class CharacterPlanner : PlannerBase
    {
        public CharacterPlanner() : base(Type.Character) { }

        public CharacterPlanner(string fullPath) : this()
        {
            CharacterSprite = RTFile.FileExists(RTFile.CombinePaths(fullPath, $"profile{FileFormat.PNG.Dot()}")) ? SpriteHelper.LoadSprite(RTFile.CombinePaths(fullPath, $"profile{FileFormat.PNG.Dot()}")) : LegacyPlugin.AtanPlaceholder;

            if (RTFile.FileExists(RTFile.CombinePaths(fullPath, $"info{FileFormat.LSN.Dot()}")))
            {
                var jn = JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(fullPath, $"info{FileFormat.LSN.Dot()}")));

                Name = jn["name"];
                Gender = jn["gender"];
                Description = jn["desc"];

                for (int i = 0; i < jn["tr"].Count; i++)
                    CharacterTraits.Add(jn["tr"][i]);

                for (int i = 0; i < jn["lo"].Count; i++)
                    CharacterLore.Add(jn["lo"][i]);

                for (int i = 0; i < jn["ab"].Count; i++)
                    CharacterAbilities.Add(jn["ab"][i]);
            }

            FullPath = fullPath;
            PlannerType = Type.Character;
        }

        public string Name { get; set; }
        public string Gender { get; set; }
        public List<string> CharacterTraits { get; set; } = new List<string>();
        public List<string> CharacterLore { get; set; } = new List<string>();
        public List<string> CharacterAbilities { get; set; } = new List<string>();
        public string Description { get; set; }
        public Sprite CharacterSprite { get; set; }

        public string FullPath { get; set; }

        public TextMeshProUGUI DetailsUI { get; set; }
        public TextMeshProUGUI DescriptionUI { get; set; }
        public Image ProfileUI { get; set; }

        public string Format(bool clamp)
        {
            var str = "<b>Name</b>: " + Name + "<br><b>Gender</b>: " + Gender + "<br><b>Character Traits</b>:<br>";

            for (int i = 0; i < CharacterTraits.Count; i++)
                str += "- " + CharacterTraits[i] + "<br>";

            str += "<br><b>Lore</b>:<br>";

            for (int i = 0; i < CharacterLore.Count; i++)
                str += "- " + CharacterLore[i] + "<br>";

            str += "<br><b>Abilities</b>:<br>";

            for (int i = 0; i < CharacterAbilities.Count; i++)
                str += "- " + CharacterAbilities[i] + (i == CharacterAbilities.Count - 1 ? "" : "<br>");

            if (clamp)
                return LSText.ClampString(str, 252);
            return str;
        }

        public string FormatDetails
        {
            get
            {
                //var stringBuilder = new StringBuilder();

                //stringBuilder.AppendLine($"<b>Name</b>: {Name}<br>");
                //stringBuilder.AppendLine($"<b>Gender</b>: {Gender}<br>");

                //stringBuilder.AppendLine($"<b>Character Traits</b>:<br>");
                //for (int i = 0; i < CharacterTraits.Count; i++)
                //{
                //    stringBuilder.AppendLine($"- {CharacterTraits[i]}<br>");
                //}
                //stringBuilder.AppendLine($"<br>");

                //stringBuilder.AppendLine($"<b>Lore</b>:<br>");
                //for (int i = 0; i < CharacterLore.Count; i++)
                //{
                //    stringBuilder.AppendLine($"- {CharacterLore[i]}<br>");
                //}
                //stringBuilder.AppendLine($"<br>");

                //stringBuilder.AppendLine($"<b>Abilities</b>:<br>");
                //for (int i = 0; i < CharacterAbilities.Count; i++)
                //{
                //    stringBuilder.AppendLine($"- {CharacterAbilities[i]}<br>");
                //}

                var str = "";

                str += "<b>Name</b>: " + Name + "<br><b>Gender</b>: " + Gender + "<br><b>Character Traits</b>:<br>";

                for (int i = 0; i < CharacterTraits.Count; i++)
                    str += "- " + CharacterTraits[i] + "<br>";

                str += "<br><b>Lore</b>:<br>";

                for (int i = 0; i < CharacterLore.Count; i++)
                    str += "- " + CharacterLore[i] + "<br>";

                str += "<br><b>Abilities</b>:<br>";

                for (int i = 0; i < CharacterAbilities.Count; i++)
                    str += "- " + CharacterAbilities[i] + (i == CharacterAbilities.Count - 1 ? "" : "<br>");

                return str;
            }
        }

        public static string DefaultCharacterDescription => "<b>Name</b>: Viral Mecha" + Environment.NewLine +
                                    "<b>Gender</b>: He" + Environment.NewLine + Environment.NewLine +
                                    "<b>Character Traits</b>:" + Environment.NewLine +
                                    "- ???" + Environment.NewLine +
                                    "- ???" + Environment.NewLine +
                                    "- ???" + Environment.NewLine + Environment.NewLine +
                                    "<b>Lore</b>:" + Environment.NewLine +
                                    "- ???" + Environment.NewLine +
                                    "- ???" + Environment.NewLine +
                                    "- ???" + Environment.NewLine + Environment.NewLine +
                                    "<b>Abilities</b>:" + Environment.NewLine +
                                    "- ???" + Environment.NewLine +
                                    "- ???" + Environment.NewLine +
                                    "- ???";

        public void Save()
        {
            var jn = JSON.Parse("{}");

            jn["name"] = Name;
            jn["gender"] = Gender;
            jn["desc"] = Description;

            for (int i = 0; i < CharacterTraits.Count; i++)
                jn["tr"][i] = CharacterTraits[i];

            for (int i = 0; i < CharacterLore.Count; i++)
                jn["lo"][i] = CharacterLore[i];

            for (int i = 0; i < CharacterAbilities.Count; i++)
                jn["ab"][i] = CharacterAbilities[i];

            RTFile.WriteToFile(FullPath + "/info.lsn", jn.ToString(3));

            SpriteHelper.SaveSprite(CharacterSprite, FullPath + "/profile.png");
        }

        public override void Init()
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = ProjectPlanner.inst.prefabs[2].Duplicate(ProjectPlanner.inst.content, "character");
            gameObject.transform.localScale = Vector3.one;
            GameObject = gameObject;

            var button = gameObject.GetComponent<Button>();
            button.onClick.ClearAll();
            button.onClick.AddListener(() => ProjectPlanner.inst.OpenCharacterEditor(this));

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);

            ProfileUI = gameObject.transform.Find("profile").GetComponent<Image>();

            DetailsUI = gameObject.transform.Find("details").GetComponent<TextMeshProUGUI>();
            EditorThemeManager.ApplyLightText(DetailsUI);

            DescriptionUI = gameObject.transform.Find("description").GetComponent<TextMeshProUGUI>();
            EditorThemeManager.ApplyLightText(DescriptionUI);

            ProfileUI.sprite = CharacterSprite;
            DetailsUI.overflowMode = TextOverflowModes.Truncate;
            DetailsUI.text = Format(true);
            DescriptionUI.text = Description;

            var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
            delete.button.onClick.ClearAll();
            delete.button.onClick.AddListener(() =>
            {
                ProjectPlanner.inst.characters.RemoveAll(x => x is CharacterPlanner && x.ID == ID);

                RTFile.DeleteDirectory(FullPath);

                CoreHelper.Destroy(gameObject);
            });

            EditorThemeManager.ApplyGraphic(delete.button.image, ThemeGroup.Delete, true);
            EditorThemeManager.ApplyGraphic(delete.image, ThemeGroup.Delete_Text);
        }
    }
}
