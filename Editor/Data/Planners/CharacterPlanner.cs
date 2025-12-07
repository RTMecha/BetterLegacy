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
    public class CharacterPlanner : PlannerBase<CharacterPlanner>
    {
        public CharacterPlanner() : base() { }

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

        public string Format(bool clamp) => clamp ? LSText.ClampString(FormatDetails, 252) : FormatDetails;

        public string FormatDetails => "<b>Name</b>: " + Name + "<br><b>Gender</b>: " + Gender;

        public static string DefaultCharacterDescription => "<b>Name</b>: Viral Mecha" + Environment.NewLine +
                                    "<b>Gender</b>: He";

        public override Type PlannerType => Type.Character;

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
                    CoreHelper.Destroy(gameObject);
                }),
                new ButtonFunction(true),
                new ButtonFunction("Copy", () =>
                {
                    ProjectPlanner.inst.copiedPlanners.Clear();
                    ProjectPlanner.inst.copiedPlanners.Add(this);
                    EditorManager.inst.DisplayNotification("Copied character!", 2f, EditorManager.NotificationType.Success);
                }),
                new ButtonFunction("Copy Selected", ProjectPlanner.inst.CopySelectedPlanners),
                new ButtonFunction("Copy Current Tab", ProjectPlanner.inst.CopyCurrentTabPlanners),
                new ButtonFunction("Paste", ProjectPlanner.inst.PastePlanners),
                new ButtonFunction(true),
            };

            buttonFunctions.AddRange(EditorContextMenu.GetMoveIndexFunctions(ProjectPlanner.inst.characters, () => ProjectPlanner.inst.characters.IndexOf(this), () =>
            {
                for (int i = 0; i < ProjectPlanner.inst.characters.Count; i++)
                    ProjectPlanner.inst.characters[i].Init();
                ProjectPlanner.inst.RefreshList();
            }));

            EditorContextMenu.AddContextMenu(gameObject, leftClick: () =>
            {
                if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
                {
                    Selected = !Selected;
                    return;
                }

                ProjectPlanner.inst.OpenCharacterEditor(this);
            }, buttonFunctions);

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
                CoreHelper.Destroy(gameObject);
                RTEditor.inst.HideWarningPopup();
            }, RTEditor.inst.HideWarningPopup));

            EditorThemeManager.ApplyDeleteButton(delete);

            InitSelectedUI();

            gameObject.SetActive(false);
        }

        public override void ReadJSON(JSONNode jn)
        {
            Name = jn["name"];
            if (jn["gender"] != null)
                Gender = jn["gender"];
            if (jn["desc"] != null)
                Description = jn["desc"];

            for (int i = 0; i < jn["tr"].Count; i++)
                CharacterTraits.Add(jn["tr"][i]);

            for (int i = 0; i < jn["lo"].Count; i++)
                CharacterLore.Add(jn["lo"][i]);

            for (int i = 0; i < jn["ab"].Count; i++)
                CharacterAbilities.Add(jn["ab"][i]);

            if (jn["sprite"] != null)
                CharacterSprite = SpriteHelper.StringToSprite(jn["sprite"]);
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["name"] = Name;
            if (!string.IsNullOrEmpty(Gender))
                jn["gender"] = Gender;
            if (!string.IsNullOrEmpty(Description))
                jn["desc"] = Description;

            for (int i = 0; i < CharacterTraits.Count; i++)
                jn["tr"][i] = CharacterTraits[i];

            for (int i = 0; i < CharacterLore.Count; i++)
                jn["lo"][i] = CharacterLore[i];

            for (int i = 0; i < CharacterAbilities.Count; i++)
                jn["ab"][i] = CharacterAbilities[i];

            if (CharacterSprite)
                jn["sprite"] = SpriteHelper.SpriteToString(CharacterSprite);

            return jn;
        }

        public override CharacterPlanner CreateCopy() => new CharacterPlanner
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
