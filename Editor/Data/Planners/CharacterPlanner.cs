using System;
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
                str += "- " + CharacterAbilities[i] + (i == CharacterAbilities.Count - 1 ? string.Empty : "<br>");

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

                var str = string.Empty;

                str += "<b>Name</b>: " + Name + "<br><b>Gender</b>: " + Gender + "<br><b>Character Traits</b>:<br>";

                for (int i = 0; i < CharacterTraits.Count; i++)
                    str += "- " + CharacterTraits[i] + "<br>";

                str += "<br><b>Lore</b>:<br>";

                for (int i = 0; i < CharacterLore.Count; i++)
                    str += "- " + CharacterLore[i] + "<br>";

                str += "<br><b>Abilities</b>:<br>";

                for (int i = 0; i < CharacterAbilities.Count; i++)
                    str += "- " + CharacterAbilities[i] + (i == CharacterAbilities.Count - 1 ? string.Empty : "<br>");

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

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);

            var buttonFunctions = new List<ButtonFunction>
            {
                new ButtonFunction("Edit", () => ProjectPlanner.inst.OpenCharacterEditor(this)),
                new ButtonFunction("Delete", () =>
                {
                    ProjectPlanner.inst.characters.RemoveAll(x => x is CharacterPlanner && x.ID == ID);
                    RTFile.DeleteDirectory(FullPath);
                    CoreHelper.Destroy(gameObject);
                }),
                new ButtonFunction(true),
                new ButtonFunction("Copy", () =>
                {
                    ProjectPlanner.inst.copiedPlanners.Clear();
                    ProjectPlanner.inst.copiedPlanners.Add(this);
                    EditorManager.inst.DisplayNotification("Copied character!", 2f, EditorManager.NotificationType.Success);
                }),
                new ButtonFunction("Paste", ProjectPlanner.inst.PastePlanners),
                new ButtonFunction(true),
            };

            buttonFunctions.AddRange(EditorContextMenu.GetMoveIndexFunctions(ProjectPlanner.inst.characters, () => ProjectPlanner.inst.characters.IndexOf(this), () =>
            {
                for (int i = 0; i < ProjectPlanner.inst.characters.Count; i++)
                    ProjectPlanner.inst.characters[i].Init();
            }));

            EditorContextMenu.AddContextMenu(gameObject, leftClick: () => ProjectPlanner.inst.OpenCharacterEditor(this), buttonFunctions);

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
            delete.OnClick.NewListener(() => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this character?", () =>
            {
                ProjectPlanner.inst.characters.RemoveAll(x => x is CharacterPlanner && x.ID == ID);
                RTFile.DeleteDirectory(FullPath);
                CoreHelper.Destroy(gameObject);
                RTEditor.inst.HideWarningPopup();
            }, RTEditor.inst.HideWarningPopup));

            EditorThemeManager.ApplyDeleteButton(delete);

            gameObject.SetActive(false);
        }

        public CharacterPlanner CreateCopy() => new CharacterPlanner
        {
            Name = Name,
            Gender = Gender,
            CharacterTraits = new List<string>(CharacterTraits),
            CharacterLore = new List<string>(CharacterLore),
            CharacterAbilities = new List<string>(CharacterAbilities),
            Description = Description,
            CharacterSprite = CharacterSprite,
        };

        public override bool SamePlanner(PlannerBase other) => other is CharacterPlanner character && character.Name == Name;
    }
}
