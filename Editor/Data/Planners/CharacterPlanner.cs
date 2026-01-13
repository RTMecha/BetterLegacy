using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using TMPro;
using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Planners
{
    /// <summary>
    /// Used for planning out a character.
    /// </summary>
    public class CharacterPlanner : PlannerBase<CharacterPlanner>
    {
        public CharacterPlanner() : base() { }

        #region Values

        #region Data

        public override Type PlannerType => Type.Character;

        /// <summary>
        /// Name of the character.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gender of the character.
        /// </summary>
        public string Gender { get; set; }

        /// <summary>
        /// Traits of the character.
        /// </summary>
        public List<string> Traits { get; set; } = new List<string>();

        /// <summary>
        /// Lore of the character.
        /// </summary>
        public List<string> Lore { get; set; } = new List<string>();

        /// <summary>
        /// Abilities of the character.
        /// </summary>
        public List<string> Abilities { get; set; } = new List<string>();

        /// <summary>
        /// Description of the character.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Avatar of the character.
        /// </summary>
        public Sprite Sprite { get; set; }

        /// <summary>
        /// Origin (place) of the character.
        /// </summary>
        public string Origin { get; set; }

        /// <summary>
        /// Formatted details.
        /// </summary>
        public string FormatDetails => "<b>Name</b>: " + RTString.ClampString(Name, 252, false, string.Empty) + "<br><b>Gender</b>: " + RTString.ClampString(Gender, 252, false, string.Empty) + "<br><b>Origin</b>: " + RTString.ClampString(Origin, 252, false, string.Empty);

        #endregion

        #region UI

        /// <summary>
        /// Details text display.
        /// </summary>
        public TextMeshProUGUI DetailsUI { get; set; }

        /// <summary>
        /// Description text display.
        /// </summary>
        public TextMeshProUGUI DescriptionUI { get; set; }

        /// <summary>
        /// Details hyperlinks.
        /// </summary>
        public OpenHyperlinks DetailsHyperlinks { get; set; }

        /// <summary>
        /// Description hyperlinks.
        /// </summary>
        public OpenHyperlinks DescriptionHyperlinks { get; set; }

        /// <summary>
        /// Avatar UI display.
        /// </summary>
        public Image ProfileUI { get; set; }

        #endregion

        #endregion

        #region Functions

        public override void Init()
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = ProjectPlanner.inst.prefabs[(int)PlannerType].Duplicate(ProjectPlanner.inst.content, "character");
            gameObject.transform.localScale = Vector3.one;
            GameObject = gameObject;

            var button = gameObject.GetComponent<Button>();
            button.onClick.ClearAll();
            gameObject.AddComponent<ContextClickable>().onClick = Click;

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);

            ProfileUI = gameObject.transform.Find("profile").GetComponent<Image>();

            DetailsUI = gameObject.transform.Find("details").GetComponent<TextMeshProUGUI>();
            EditorThemeManager.ApplyLightText(DetailsUI);
            DetailsHyperlinks = gameObject.AddComponent<OpenHyperlinks>();
            DetailsHyperlinks.Text = DetailsUI;
            DetailsHyperlinks.onClick = pointerEventData =>
            {
                if (DetailsHyperlinks.IsLinkHighlighted)
                    return;
                
                if (DescriptionHyperlinks.IsLinkHighlighted)
                    return;

                Click(pointerEventData);
            };

            DescriptionUI = gameObject.transform.Find("description").GetComponent<TextMeshProUGUI>();
            EditorThemeManager.ApplyLightText(DescriptionUI);
            DescriptionHyperlinks = gameObject.AddComponent<OpenHyperlinks>();
            DescriptionHyperlinks.Text = DescriptionUI;

            ProfileUI.sprite = Sprite;
            DetailsUI.overflowMode = TextOverflowModes.Masking;
            DetailsUI.text = FormatDetails;
            DescriptionUI.text = Description;

            var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
            delete.OnClick.NewListener(() => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this character?", () =>
            {
                ProjectPlanner.inst.characters.RemoveAll(x => x is CharacterPlanner && x.ID == ID);
                CoreHelper.Destroy(gameObject);
            }));

            EditorThemeManager.ApplyDeleteButton(delete);

            ProjectPlanner.inst.SetupPlannerLinks(DetailsUI.text, DetailsUI, DetailsHyperlinks);
            ProjectPlanner.inst.SetupPlannerLinks(Description, DescriptionUI, DescriptionHyperlinks);

            InitSelectedUI();

            gameObject.SetActive(false);
        }

        public override void Render()
        {
            ProfileUI.sprite = Sprite;
            DetailsUI.text = FormatDetails;
            DescriptionUI.text = Description;

            ProjectPlanner.inst.SetupPlannerLinks(DetailsUI.text, DetailsUI, DetailsHyperlinks);
            ProjectPlanner.inst.SetupPlannerLinks(Description, DescriptionUI, DescriptionHyperlinks);
        }

        public override void ReadJSON(JSONNode jn)
        {
            Name = jn["name"];
            if (jn["gender"] != null)
                Gender = jn["gender"];
            if (jn["desc"] != null)
                Description = jn["desc"];

            for (int i = 0; i < jn["tr"].Count; i++)
                Traits.Add(jn["tr"][i]);

            for (int i = 0; i < jn["lo"].Count; i++)
                Lore.Add(jn["lo"][i]);

            for (int i = 0; i < jn["ab"].Count; i++)
                Abilities.Add(jn["ab"][i]);

            if (jn["sprite"] != null)
                Sprite = SpriteHelper.StringToSprite(jn["sprite"]);

            if (jn["origin"] != null)
                Origin = jn["origin"];
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["name"] = Name ?? string.Empty;
            if (!string.IsNullOrEmpty(Gender))
                jn["gender"] = Gender;
            if (!string.IsNullOrEmpty(Description))
                jn["desc"] = Description;

            for (int i = 0; i < Traits.Count; i++)
                jn["tr"][i] = Traits[i];

            for (int i = 0; i < Lore.Count; i++)
                jn["lo"][i] = Lore[i];

            for (int i = 0; i < Abilities.Count; i++)
                jn["ab"][i] = Abilities[i];

            if (Sprite)
                jn["sprite"] = SpriteHelper.SpriteToString(Sprite);
            if (!string.IsNullOrEmpty(Origin))
                jn["origin"] = Origin;

            return jn;
        }

        public override CharacterPlanner CreateCopy() => new CharacterPlanner
        {
            Name = Name,
            Gender = Gender,
            Traits = new List<string>(Traits),
            Lore = new List<string>(Lore),
            Abilities = new List<string>(Abilities),
            Description = Description,
            Sprite = Sprite,
            Origin = Origin,
        };

        public override bool SamePlanner(PlannerBase other) => other is CharacterPlanner character && character.Name == Name;

        void Click(PointerEventData pointerEventData)
        {
            if (pointerEventData.button != PointerEventData.InputButton.Right)
            {
                if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
                {
                    Selected = !Selected;
                    return;
                }

                ProjectPlanner.inst.OpenCharacterEditor(this);
                return;
            }

            var buttonFunctions = new List<EditorElement>
            {
                new ButtonElement("Edit", () => ProjectPlanner.inst.OpenCharacterEditor(this)),
                new ButtonElement("Delete", () =>
                {
                    ProjectPlanner.inst.characters.RemoveAll(x => x is CharacterPlanner && x.ID == ID);
                    CoreHelper.Destroy(GameObject);
                }),
                new SpacerElement(),
                new ButtonElement("Copy", () =>
                {
                    ProjectPlanner.inst.copiedPlanners.Clear();
                    ProjectPlanner.inst.copiedPlanners.Add(this);
                    EditorManager.inst.DisplayNotification("Copied character!", 2f, EditorManager.NotificationType.Success);
                }),
                new ButtonElement("Copy Selected", ProjectPlanner.inst.CopySelectedPlanners),
                new ButtonElement("Copy Current Tab", ProjectPlanner.inst.CopyCurrentTabPlanners),
                new ButtonElement("Paste", ProjectPlanner.inst.PastePlanners),
                new SpacerElement(),
            };

            buttonFunctions.AddRange(EditorContextMenu.GetMoveIndexFunctions(ProjectPlanner.inst.characters, () => ProjectPlanner.inst.characters.IndexOf(this), () =>
            {
                for (int i = 0; i < ProjectPlanner.inst.characters.Count; i++)
                    ProjectPlanner.inst.characters[i].Init();
                ProjectPlanner.inst.RefreshList();
            }));

            EditorContextMenu.inst.ShowContextMenu(buttonFunctions);
        }

        #endregion
    }
}
