using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;
using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Class that applies Editor Themes and Rounded setting onto every UI element in the editor.
    /// </summary>
    public class EditorThemeManager
    {
        #region Values

        /// <summary>
        /// The currently selected editor theme.
        /// </summary>
        public static EditorTheme CurrentTheme => EditorThemes[Mathf.Clamp(currentTheme, 0, EditorThemes.Count - 1)];
        /// <summary>
        /// The currently selected editor theme.
        /// </summary>
        public static int currentTheme = 0;
        /// <summary>
        /// List of all loaded editor themes.
        /// </summary>
        public static List<EditorTheme> EditorThemes { get; set; } = new List<EditorTheme>();

        /// <summary>
        /// If only the active elements should be re-rendered.
        /// </summary>
        public static bool onlyRenderActive;

        /// <summary>
        /// Lookup for theme groups that have associated <see cref="ColorBlock"/> groups.
        /// </summary>
        public static Dictionary<ThemeGroup, SelectableThemeGroup> selectableThemeGroups = new Dictionary<ThemeGroup, SelectableThemeGroup>
        {
            {
                ThemeGroup.Scrollbar_1_Handle, new SelectableThemeGroup(
                    ThemeGroup.Scrollbar_1_Handle_Normal,
                    ThemeGroup.Scrollbar_1_Handle_Highlighted,
                    ThemeGroup.Scrollbar_1_Handle_Selected,
                    ThemeGroup.Scrollbar_1_Handle_Pressed,
                    ThemeGroup.Scrollbar_1_Handle_Disabled)
            }, // Scrollbar_1_Handle
            {
                ThemeGroup.Scrollbar_2_Handle, new SelectableThemeGroup(
                    ThemeGroup.Scrollbar_2_Handle_Normal,
                    ThemeGroup.Scrollbar_2_Handle_Highlighted,
                    ThemeGroup.Scrollbar_2_Handle_Selected,
                    ThemeGroup.Scrollbar_2_Handle_Pressed,
                    ThemeGroup.Scrollbar_2_Handle_Disabled)
            }, // Scrollbar_2_Handle
            {
                ThemeGroup.Close, new SelectableThemeGroup(
                    ThemeGroup.Close_Normal,
                    ThemeGroup.Close_Highlighted,
                    ThemeGroup.Close_Selected,
                    ThemeGroup.Close_Pressed,
                    ThemeGroup.Close_Disabled)
            }, // Close
            {
                ThemeGroup.Picker, new SelectableThemeGroup(
                    ThemeGroup.Picker_Normal,
                    ThemeGroup.Picker_Highlighted,
                    ThemeGroup.Picker_Selected,
                    ThemeGroup.Picker_Pressed,
                    ThemeGroup.Picker_Disabled)
            }, // Picker
            {
                ThemeGroup.Function_2, new SelectableThemeGroup(
                    ThemeGroup.Function_2_Normal,
                    ThemeGroup.Function_2_Highlighted,
                    ThemeGroup.Function_2_Selected,
                    ThemeGroup.Function_2_Pressed,
                    ThemeGroup.Function_2_Disabled)
            }, // Function_2
            {
                ThemeGroup.List_Button_1, new SelectableThemeGroup(
                    ThemeGroup.List_Button_1_Normal,
                    ThemeGroup.List_Button_1_Highlighted,
                    ThemeGroup.List_Button_1_Selected,
                    ThemeGroup.List_Button_1_Pressed,
                    ThemeGroup.List_Button_1_Disabled)
            }, // List_Button_1
            {
                ThemeGroup.List_Button_2, new SelectableThemeGroup(
                    ThemeGroup.List_Button_2_Normal,
                    ThemeGroup.List_Button_2_Highlighted,
                    ThemeGroup.List_Button_2_Selected,
                    ThemeGroup.List_Button_2_Pressed,
                    ThemeGroup.List_Button_2_Disabled)
            }, // List_Button_2
            {
                ThemeGroup.Delete_Keyframe_Button, new SelectableThemeGroup(
                    ThemeGroup.Delete_Keyframe_Button_Normal,
                    ThemeGroup.Delete_Keyframe_Button_Highlighted,
                    ThemeGroup.Delete_Keyframe_Button_Selected,
                    ThemeGroup.Delete_Keyframe_Button_Pressed,
                    ThemeGroup.Delete_Keyframe_Button_Disabled)
            }, // Delete_Keyframe_Button
            {
                ThemeGroup.Slider_1, new SelectableThemeGroup(
                    ThemeGroup.Slider_1_Normal,
                    ThemeGroup.Slider_1_Highlighted,
                    ThemeGroup.Slider_1_Selected,
                    ThemeGroup.Slider_1_Pressed,
                    ThemeGroup.Slider_1_Disabled)
            }, // Slider_1
            {
                ThemeGroup.Timeline_Scrollbar, new SelectableThemeGroup(
                    ThemeGroup.Timeline_Scrollbar_Normal,
                    ThemeGroup.Timeline_Scrollbar_Highlighted,
                    ThemeGroup.Timeline_Scrollbar_Selected,
                    ThemeGroup.Timeline_Scrollbar_Pressed,
                    ThemeGroup.Timeline_Scrollbar_Disabled)
            }, // Timeline_Scrollbar
            {
                ThemeGroup.Title_Bar_Button, new SelectableThemeGroup(
                    ThemeGroup.Title_Bar_Button_Normal,
                    ThemeGroup.Title_Bar_Button_Highlighted,
                    ThemeGroup.Title_Bar_Button_Selected,
                    ThemeGroup.Title_Bar_Button_Pressed,
                    ThemeGroup.Title_Bar_Button_Disabled)
            }, // Title_Bar_Button
            {
                ThemeGroup.Title_Bar_Dropdown, new SelectableThemeGroup(
                    ThemeGroup.Title_Bar_Dropdown_Normal,
                    ThemeGroup.Title_Bar_Dropdown_Highlighted,
                    ThemeGroup.Title_Bar_Dropdown_Selected,
                    ThemeGroup.Title_Bar_Dropdown_Pressed,
                    ThemeGroup.Title_Bar_Dropdown_Disabled)
            }, // Title_Bar_Dropdown
            {
                ThemeGroup.Tab_Color_1, new SelectableThemeGroup(
                    ThemeGroup.Tab_Color_1_Normal,
                    ThemeGroup.Tab_Color_1_Highlighted,
                    ThemeGroup.Tab_Color_1_Selected,
                    ThemeGroup.Tab_Color_1_Pressed,
                    ThemeGroup.Tab_Color_1_Disabled)
            }, // Tab_Color_1
            {
                ThemeGroup.Tab_Color_2, new SelectableThemeGroup(
                    ThemeGroup.Tab_Color_2_Normal,
                    ThemeGroup.Tab_Color_2_Highlighted,
                    ThemeGroup.Tab_Color_2_Selected,
                    ThemeGroup.Tab_Color_2_Pressed,
                    ThemeGroup.Tab_Color_2_Disabled)
            }, // Tab_Color_2
            {
                ThemeGroup.Tab_Color_3, new SelectableThemeGroup(
                    ThemeGroup.Tab_Color_3_Normal,
                    ThemeGroup.Tab_Color_3_Highlighted,
                    ThemeGroup.Tab_Color_3_Selected,
                    ThemeGroup.Tab_Color_3_Pressed,
                    ThemeGroup.Tab_Color_3_Disabled)
            }, // Tab_Color_3
            {
                ThemeGroup.Tab_Color_4, new SelectableThemeGroup(
                    ThemeGroup.Tab_Color_4_Normal,
                    ThemeGroup.Tab_Color_4_Highlighted,
                    ThemeGroup.Tab_Color_4_Selected,
                    ThemeGroup.Tab_Color_4_Pressed,
                    ThemeGroup.Tab_Color_4_Disabled)
            }, // Tab_Color_4
            {
                ThemeGroup.Tab_Color_5, new SelectableThemeGroup(
                    ThemeGroup.Tab_Color_5_Normal,
                    ThemeGroup.Tab_Color_5_Highlighted,
                    ThemeGroup.Tab_Color_5_Selected,
                    ThemeGroup.Tab_Color_5_Pressed,
                    ThemeGroup.Tab_Color_5_Disabled)
            }, // Tab_Color_5
            {
                ThemeGroup.Tab_Color_6, new SelectableThemeGroup(
                    ThemeGroup.Tab_Color_6_Normal,
                    ThemeGroup.Tab_Color_6_Highlighted,
                    ThemeGroup.Tab_Color_6_Selected,
                    ThemeGroup.Tab_Color_6_Pressed,
                    ThemeGroup.Tab_Color_6_Disabled)
            }, // Tab_Color_6
            {
                ThemeGroup.Tab_Color_7, new SelectableThemeGroup(
                    ThemeGroup.Tab_Color_7_Normal,
                    ThemeGroup.Tab_Color_7_Highlighted,
                    ThemeGroup.Tab_Color_7_Selected,
                    ThemeGroup.Tab_Color_7_Pressed,
                    ThemeGroup.Tab_Color_7_Disabled)
            }, // Tab_Color_7
        };

        /// <summary>
        /// Lookup for theme groups that have associated <see cref="InputField"/> groups.
        /// </summary>
        public static Dictionary<ThemeGroup, InputFieldThemeGroup> inputFieldThemeGroups = new Dictionary<ThemeGroup, InputFieldThemeGroup>
        {
            {
                ThemeGroup.Search_Field_1, new InputFieldThemeGroup(
                    ThemeGroup.Search_Field_1,
                    ThemeGroup.Search_Field_1_Text)
            }, // Search_Field_1
            {
                ThemeGroup.Search_Field_2, new InputFieldThemeGroup(
                    ThemeGroup.Search_Field_2,
                    ThemeGroup.Search_Field_2_Text)
            }, // Search_Field_2
            {
                ThemeGroup.Input_Field, new InputFieldThemeGroup(
                    ThemeGroup.Input_Field,
                    ThemeGroup.Input_Field_Text)
            }, // Input_Field
        };

        /// <summary>
        /// The default color block.
        /// </summary>
        public static ColorBlock DefaultColorBlock => new ColorBlock
        {
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };

        #endregion

        #region Functions

        /// <summary>
        /// Renders all theme elements.
        /// </summary>
        public static void RenderElements() => CoroutineHelper.StartCoroutine(IRenderElements());

        /// <summary>
        /// Renders all theme elements.
        /// </summary>
        public static IEnumerator IRenderElements()
        {
            var theme = CurrentTheme;

            var elements = onlyRenderActive ? UnityObject.FindObjectsOfType<EditorThemeObject>() : Resources.FindObjectsOfTypeAll<EditorThemeObject>();
            for (int i = 0; i < elements.Length; i++)
                elements[i].element?.ApplyTheme(theme);

            if (EditorTimeline.inst && EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
            {
                RTEventEditor.inst.RenderLayerBins();
                if (EventEditor.inst.dialogRight.gameObject.activeInHierarchy)
                    RTEventEditor.inst.RenderDialog();
            }

            yield break;
        }

        /// <summary>
        /// Loads the list of editor themes.
        /// </summary>
        public static void LoadEditorThemes()
        {
            EditorThemes.Clear();

            var jn = JSON.Parse(RTFile.ReadFromFile(AssetPack.GetFile("editor/data/editor_themes.json")));

            for (int i = 0; i < jn["themes"].Count; i++)
                EditorThemes.Add(EditorTheme.Parse(jn["themes"][i]));

            var values = new EditorThemeType[EditorThemes.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = new EditorThemeType(i, EditorThemes[i].name);
            CustomEnumHelper.SetValues(values);
        }

        /// <summary>
        /// Saves the current list of editor themes.
        /// </summary>
        public static void SaveEditorThemes()
        {
            var jn = Parser.NewJSONObject();
            for (int i = 0; i < EditorThemes.Count; i++)
                jn["themes"][i] = EditorThemes[i].ToJSON();
            RTFile.WriteToFile(AssetPack.GetFile("editor/data/editor_themes.json"), jn.ToString());
        }

        /// <summary>
        /// Checks if a theme group is a selectable theme group.
        /// </summary>
        /// <param name="themeGroup">Theme group to check.</param>
        /// <returns>Returns true if the theme group has associated selectable theme groups.</returns>
        public static bool IsSelectable(ThemeGroup themeGroup) => themeGroup switch
        {
            ThemeGroup.Close => true,
            ThemeGroup.Delete_Keyframe_Button => true,
            ThemeGroup.Function_2 => true,
            ThemeGroup.List_Button_1 => true,
            ThemeGroup.List_Button_2 => true,
            ThemeGroup.Picker => true,
            ThemeGroup.Scrollbar_1_Handle => true,
            ThemeGroup.Scrollbar_2_Handle => true,
            ThemeGroup.Slider_1 => true,
            ThemeGroup.Tab_Color_1 => true,
            ThemeGroup.Tab_Color_2 => true,
            ThemeGroup.Tab_Color_3 => true,
            ThemeGroup.Tab_Color_4 => true,
            ThemeGroup.Tab_Color_5 => true,
            ThemeGroup.Tab_Color_6 => true,
            ThemeGroup.Tab_Color_7 => true,
            ThemeGroup.Timeline_Scrollbar => true,
            ThemeGroup.Title_Bar_Button => true,
            ThemeGroup.Title_Bar_Dropdown => true,
            _ => false,
        };

        #region Apply

        /// <summary>
        /// Applies a theme to an editor element.
        /// </summary>
        /// <param name="element">Element to apply.</param>
        public static void ApplyElement(EditorThemeElement element)
        {
            element.ApplyTheme(CurrentTheme);

            if (!element.gameObject)
                return;

            var component = element.gameObject.GetOrAddComponent<EditorThemeObject>();
            component.element = element;
        }

        /// <summary>
        /// Applies a theme to a selectable object.
        /// </summary>
        /// <param name="selectable">Selectable to apply.</param>
        /// <param name="group">Color group to assign.</param>
        /// <param name="canSetRounded">If the selectable can be rounded.</param>
        /// <param name="rounded">Rounded value of the selectable.</param>
        /// <param name="roundedSide">Rounded side of the selectable.</param>
        public static void ApplySelectable(Selectable selectable, ThemeGroup group, bool canSetRounded = true, int rounded = 1, SpriteHelper.RoundedSide roundedSide = SpriteHelper.RoundedSide.W)
        {
            if (!selectable)
                return;

            ApplyElement(new EditorThemeElement(group, selectable.gameObject, new Component[]
            {
                selectable.image,
                selectable,
            }, canSetRounded, rounded, roundedSide, true));
        }

        /// <summary>
        /// Applies a theme to a graphic.
        /// </summary>
        /// <param name="graphic">Graphic to apply.</param>
        /// <param name="group">Color group to assign.</param>
        /// <param name="canSetRounded">If the graphic can be rounded.</param>
        /// <param name="rounded">Rounded value of the graphic.</param>
        /// <param name="roundedSide">Rounded side of the graphic.</param>
        public static void ApplyGraphic(Graphic graphic, ThemeGroup group, bool canSetRounded = false, int rounded = 1, SpriteHelper.RoundedSide roundedSide = SpriteHelper.RoundedSide.W, float opacity = 1f)
        {
            if (!graphic)
                return;

            ApplyElement(new EditorThemeElement(group, graphic.gameObject, new Component[]
            {
                graphic,
            }, canSetRounded, rounded, roundedSide, opacity: opacity));
        }

        /// <summary>
        /// Applies a theme to a dropdown.
        /// </summary>
        /// <param name="dropdown">Dropdown to apply.</param>
        public static void ApplyDropdown(Dropdown dropdown)
        {
            if (!dropdown)
                return;

            ApplyGraphic(dropdown.image, ThemeGroup.Dropdown_1, true);
            ApplyGraphic(dropdown.captionText, ThemeGroup.Dropdown_1_Overlay);
            ApplyGraphic(dropdown.transform.Find("Arrow").GetComponent<Image>(), ThemeGroup.Dropdown_1_Overlay);
            if (dropdown.captionImage)
                ApplyGraphic(dropdown.captionImage, ThemeGroup.Dropdown_1_Overlay);

            var template = dropdown.template.gameObject;
            ApplyGraphic(template.GetComponent<Image>(), ThemeGroup.Dropdown_1, true, roundedSide: SpriteHelper.RoundedSide.Bottom);

            var templateItem = template.transform.Find("Viewport/Content/Item");
            ApplyGraphic(templateItem.Find("Item Background").GetComponent<Image>(), ThemeGroup.Dropdown_1_Item, true);
            ApplyGraphic(templateItem.Find("Item Checkmark").GetComponent<Image>(), ThemeGroup.Dropdown_1_Overlay);
            ApplyGraphic(dropdown.itemText, ThemeGroup.Dropdown_1_Overlay);
            if (dropdown.itemImage)
                ApplyGraphic(dropdown.itemImage, ThemeGroup.Dropdown_1_Overlay);
        }

        /// <summary>
        /// Applies a theme to a number input field.
        /// </summary>
        /// <param name="inputFieldStorage">Number input field to apply.</param>
        public static void ApplyInputField(InputFieldStorage inputFieldStorage)
        {
            if (!inputFieldStorage)
                return;

            if (inputFieldStorage.inputField)
                ApplyInputField(inputFieldStorage.inputField);
            if (inputFieldStorage.subButton)
                ApplySelectable(inputFieldStorage.subButton, ThemeGroup.Function_2, false);
            if (inputFieldStorage.addButton)
                ApplySelectable(inputFieldStorage.addButton, ThemeGroup.Function_2, false);
            if (inputFieldStorage.leftGreaterButton)
                ApplySelectable(inputFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
            if (inputFieldStorage.leftButton)
                ApplySelectable(inputFieldStorage.leftButton, ThemeGroup.Function_2, false);
            if (inputFieldStorage.middleButton)
                ApplySelectable(inputFieldStorage.middleButton, ThemeGroup.Function_2, false);
            if (inputFieldStorage.rightButton)
                ApplySelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);
            if (inputFieldStorage.rightGreaterButton)
                ApplySelectable(inputFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);
        }

        /// <summary>
        /// Applies a a theme to a Vector2 input fields.
        /// </summary>
        /// <param name="vector2InputFieldStorage">Vector2 input fields to apply.</param>
        public static void ApplyInputField(Vector2InputFieldStorage vector2InputFieldStorage)
        {
            if (!vector2InputFieldStorage)
                return;

            if (vector2InputFieldStorage.x)
                ApplyInputField(vector2InputFieldStorage.x);
            if (vector2InputFieldStorage.y)
                ApplyInputField(vector2InputFieldStorage.y);
        }

        /// <summary>
        /// Applies a a theme to a Vector3 input fields.
        /// </summary>
        /// <param name="vector3InputFieldStorage">Vector3 input fields to apply.</param>
        public static void ApplyInputField(Vector3InputFieldStorage vector3InputFieldStorage)
        {
            if (!vector3InputFieldStorage)
                return;

            if (vector3InputFieldStorage.x)
                ApplyInputField(vector3InputFieldStorage.x);
            if (vector3InputFieldStorage.y)
                ApplyInputField(vector3InputFieldStorage.y);
            if (vector3InputFieldStorage.z)
                ApplyInputField(vector3InputFieldStorage.z);
        }

        /// <summary>
        /// Applies a a theme to a Vector4 input fields.
        /// </summary>
        /// <param name="vector4InputFieldStorage">Vector4 input fields to apply.</param>
        public static void ApplyInputField(Vector4InputFieldStorage vector4InputFieldStorage)
        {
            if (!vector4InputFieldStorage)
                return;

            if (vector4InputFieldStorage.x)
                ApplyInputField(vector4InputFieldStorage.x);
            if (vector4InputFieldStorage.y)
                ApplyInputField(vector4InputFieldStorage.y);
            if (vector4InputFieldStorage.z)
                ApplyInputField(vector4InputFieldStorage.z);
            if (vector4InputFieldStorage.w)
                ApplyInputField(vector4InputFieldStorage.w);
        }

        /// <summary>
        /// Applies a theme to an input field.
        /// </summary>
        /// <param name="inputField">Input field to apply.</param>
        /// <param name="group">Color group to assign.</param>
        /// <param name="rounded">Rounded value of the input field.</param>
        /// <param name="roundedSide">Rounded side of the input field.</param>
        public static void ApplyInputField(TMP_InputField inputField, ThemeGroup group = ThemeGroup.Input_Field, int rounded = 1, SpriteHelper.RoundedSide roundedSide = SpriteHelper.RoundedSide.W)
        {
            if (!inputField)
                return;

            if (!inputField.image)
                inputField.image = inputField.transform.childCount > 0 ? inputField.transform.GetChild(0).GetComponent<Image>() ?? inputField.GetComponent<Image>() : inputField.GetComponent<Image>();

            if (inputField.image)
                inputField.image.fillCenter = true;
            ApplyGraphic(inputField.image, group, true, rounded, roundedSide);
            if (inputFieldThemeGroups.TryGetValue(group, out InputFieldThemeGroup inputFieldThemeGroup))
            {
                ApplyGraphic(inputField.textComponent, inputFieldThemeGroup.text);
                ApplyGraphic(inputField.placeholder, inputFieldThemeGroup.placeholder, opacity: 0.5f);
            }
        }

        /// <summary>
        /// Applies a theme to an input field.
        /// </summary>
        /// <param name="inputField">Input field to apply.</param>
        /// <param name="group">Color group to assign.</param>
        /// <param name="rounded">Rounded value of the input field.</param>
        /// <param name="roundedSide">Rounded side of the input field.</param>
        public static void ApplyInputField(InputField inputField, ThemeGroup group = ThemeGroup.Input_Field, int rounded = 1, SpriteHelper.RoundedSide roundedSide = SpriteHelper.RoundedSide.W)
        {
            if (!inputField)
                return;

            if (!inputField.image)
                inputField.image = inputField.transform.childCount > 0 ? inputField.transform.GetChild(0).GetComponent<Image>() ?? inputField.GetComponent<Image>() : inputField.GetComponent<Image>();

            if (inputField.image)
                inputField.image.fillCenter = true;
            ApplyGraphic(inputField.image, group, true, rounded, roundedSide);
            if (inputFieldThemeGroups.TryGetValue(group, out InputFieldThemeGroup inputFieldThemeGroup))
            {
                ApplyGraphic(inputField.textComponent, inputFieldThemeGroup.text);
                if (inputField.placeholder)
                    ApplyGraphic(inputField.placeholder, inputFieldThemeGroup.placeholder, opacity: 0.5f);
                else if (inputField.transform.TryFind("Placeholder", out Transform uPlaceholder))
                    ApplyGraphic(uPlaceholder.GetComponent<Graphic>(), inputFieldThemeGroup.placeholder, opacity: 0.5f);
                else if (inputField.transform.TryFind("placeholder", out Transform lPlaceholder))
                    ApplyGraphic(lPlaceholder.GetComponent<Graphic>(), inputFieldThemeGroup.placeholder, opacity: 0.5f);
            }
        }

        /// <summary>
        /// Applies a theme to a delete button.
        /// </summary>
        /// <param name="delete">Delete button to apply.</param>
        public static void ApplyDeleteButton(DeleteButtonStorage delete)
        {
            if (!delete)
                return;

            ApplyGraphic(delete.button.image, ThemeGroup.Delete, true);
            ApplyGraphic(delete.image, ThemeGroup.Delete_Text);
        }

        /// <summary>
        /// Applies a theme to a toggle.
        /// </summary>
        /// <param name="toggle">Toggle to apply.</param>
        /// <param name="checkGroup">Check mark color group.</param>
        /// <param name="graphic">Extra graphic to apply.</param>
        public static void ApplyToggle(Toggle toggle, ThemeGroup checkGroup = ThemeGroup.Null, Graphic graphic = null)
        {
            if (!toggle)
                return;

            toggle.image.fillCenter = true;
            ApplyGraphic(toggle.image, ThemeGroup.Toggle_1, true);

            var checkMarkGroup = checkGroup != ThemeGroup.Null ? checkGroup : ThemeGroup.Toggle_1_Check;
            ApplyGraphic(toggle.graphic, checkMarkGroup);

            if (graphic)
            {
                ApplyGraphic(graphic, checkMarkGroup);
                return;
            }

            if (toggle.transform.Find("Text"))
                ApplyGraphic(toggle.transform.Find("Text").GetComponent<Text>(), checkMarkGroup);
            if (toggle.transform.Find("text"))
                ApplyGraphic(toggle.transform.Find("text").GetComponent<Text>(), checkMarkGroup);
        }

        /// <summary>
        /// Applies a theme to a toggle.
        /// </summary>
        /// <param name="toggle">Toggle to apply.</param>
        /// <param name="checkGroup">Check mark color group.</param>
        public static void ApplyToggle(ToggleButtonStorage toggleButtonStorage, ThemeGroup checkGroup = ThemeGroup.Null) => ApplyToggle(toggleButtonStorage.toggle, checkGroup, toggleButtonStorage.label);

        /// <summary>
        /// Applies a theme to a label.
        /// </summary>
        /// <param name="text">Text to apply.</param>
        public static void ApplyLightText(Text text) => ApplyGraphic(text, ThemeGroup.Light_Text);

        /// <summary>
        /// Applies a theme to a label.
        /// </summary>
        /// <param name="text">Text to apply.</param>
        public static void ApplyLightText(TextMeshProUGUI text) => ApplyGraphic(text, ThemeGroup.Light_Text);

        /// <summary>
        /// Applies a theme to a scrollbar.
        /// </summary>
        /// <param name="scrollbar">Scrollbar to apply.</param>
        /// <param name="backgroundImage">Background image to apply.</param>
        /// <param name="scrollbarGroup">Scrollbar color group to assign.</param>
        /// <param name="handleGroup">Handle color group to assign.</param>
        /// <param name="canSetScrollbarRounded">If the scrollbar can be rounded.</param>
        /// <param name="canSetHandleRounded">If the handle can be rounded.</param>
        /// <param name="scrollbarRounded">Rounded value of the scrollbar.</param>
        /// <param name="handleRounded">Rounded value of the handle.</param>
        /// <param name="scrollbarRoundedSide">Rounded side of the scrollbar.</param>
        /// <param name="handleRoundedSide">Rounded side of the handle.</param>
        public static void ApplyScrollbar(Scrollbar scrollbar, Image backgroundImage = null, ThemeGroup scrollbarGroup = ThemeGroup.Background_1, ThemeGroup handleGroup = ThemeGroup.Scrollbar_1_Handle,
            bool canSetScrollbarRounded = true, bool canSetHandleRounded = true, int scrollbarRounded = 1, int handleRounded = 1,
            SpriteHelper.RoundedSide scrollbarRoundedSide = SpriteHelper.RoundedSide.W, SpriteHelper.RoundedSide handleRoundedSide = SpriteHelper.RoundedSide.W)
        {
            if (!scrollbar)
                return;

            ApplyGraphic(backgroundImage ?? scrollbar.GetComponent<Image>(), scrollbarGroup, canSetScrollbarRounded, scrollbarRounded, scrollbarRoundedSide);
            ApplySelectable(scrollbar, handleGroup, canSetHandleRounded, handleRounded, handleRoundedSide);
        }

        /// <summary>
        /// Applies a theme to a slider.
        /// </summary>
        /// <param name="slider">Slider to apply.</param>
        /// <param name="backgroundImage">Background image to apply.</param>
        /// <param name="sliderGroup">Slider color group to assign.</param>
        /// <param name="handleGroup">Handle color group to assign.</param>
        /// <param name="canSetSliderRounded">If the slider can be rounded.</param>
        /// <param name="canSetHandleRounded">If the handle can be rounded.</param>
        /// <param name="sliderRounded">Rounded value of the slider.</param>
        /// <param name="handleRounded">Rounded value of the handle.</param>
        /// <param name="sliderRoundedSide">Rounded side of the slider.</param>
        /// <param name="handleRoundedSide">Rounded side of the handle.</param>
        /// <param name="selectable">If the slider should be handled as a selectable.</param>
        public static void ApplySlider(Slider slider, Image backgroundImage = null, ThemeGroup sliderGroup = ThemeGroup.Slider_2, ThemeGroup handleGroup = ThemeGroup.Slider_2_Handle,
            bool canSetSliderRounded = true, bool canSetHandleRounded = true, int sliderRounded = 1, int handleRounded = 1,
            SpriteHelper.RoundedSide sliderRoundedSide = SpriteHelper.RoundedSide.W, SpriteHelper.RoundedSide handleRoundedSide = SpriteHelper.RoundedSide.W, bool selectable = false)
        {
            if (!slider)
                return;

            ApplyGraphic(backgroundImage ?? slider.GetComponent<Image>(), sliderGroup, canSetSliderRounded, sliderRounded, sliderRoundedSide);
            if (selectable)
                ApplySelectable(slider, handleGroup, canSetHandleRounded, handleRounded, handleRoundedSide);
            else
                ApplyGraphic(slider.image, handleGroup, canSetHandleRounded, handleRounded, handleRoundedSide);
        }

        #endregion

        /// <summary>
        /// Resets selectable colors to the default.
        /// </summary>
        /// <param name="selectable">Selectable to clear.</param>
        public static void ClearSelectableColors(Selectable selectable)
        {
            if (selectable)
                selectable.colors = new ColorBlock()
                {
                    normalColor = Color.white,
                    highlightedColor = Color.white,
                    pressedColor = Color.white,
                    selectedColor = Color.white,
                    disabledColor = Color.white,
                    colorMultiplier = 1f,
                    fadeDuration = 0.2f,
                };
        }

        /// <summary>
        /// Sets the color of an array of components.
        /// </summary>
        /// <param name="color">Color to assign.</param>
        /// <param name="components">Array of components.</param>
        public static void SetColor(Color color, params Component[] components)
        {
            try
            {
                foreach (var component in components)
                {
                    if (component is Graphic graphic)
                        graphic.color = color;
                }
            }
            catch
            {
                foreach (var component in components)
                {
                    if (component is Text text)
                    {
                        var str = text.text;
                        text.text = string.Empty;
                        text.text = str;
                    }
                }
            }
        }

        /// <summary>
        /// Sets the color of an array of components.
        /// </summary>
        /// <param name="color">Color to assign.</param>
        /// <param name="colorBlock">Color block to assign to selectables.</param>
        /// <param name="components">Array of components.</param>
        public static void SetColor(Color color, ColorBlock colorBlock, params Component[] components)
        {
            foreach (var component in components)
            {
                if (component is Image image)
                    image.color = color;
                if (component is Selectable button)
                    button.colors = colorBlock;
            }
        }

        /// <summary>
        /// Applies the rounded setting to graphics.
        /// </summary>
        /// <param name="canSetRounded">If the element can be rounded.</param>
        /// <param name="rounded">Rounded value.</param>
        /// <param name="roundedSide">Sounded side.</param>
        /// <param name="components">Array of components.</param>
        public static void SetRounded(bool canSetRounded, int rounded, SpriteHelper.RoundedSide roundedSide, params Component[] components)
        {
            try
            {
                if (!canSetRounded)
                    return;

                var canSet = EditorConfig.Instance.RoundedUI.Value;

                foreach (var component in components)
                {
                    if (component is not Image image)
                        continue;

                    image.fillCenter = true;

                    if (rounded != 0 && canSet)
                        SpriteHelper.SetRoundedSprite(image, rounded, roundedSide);
                    else
                        image.sprite = null;
                }
            }
            catch
            {

            }
        }

        /// <summary>
        /// Gets a layer's theme group.
        /// </summary>
        /// <param name="layer">Layer number.</param>
        /// <returns>Returns a theme group based on the layer.</returns>
        public static ThemeGroup GetLayerThemeGroup(int layer) => layer switch
        {
            0 => ThemeGroup.Layer_1,
            1 => ThemeGroup.Layer_2,
            2 => ThemeGroup.Layer_3,
            3 => ThemeGroup.Layer_4,
            4 => ThemeGroup.Layer_5,
            5 => ThemeGroup.Event_Check,
            _ => ThemeGroup.Null,
        };

        /// <summary>
        /// Gets a theme group that represents a tab.
        /// </summary>
        /// <param name="tab">Tab number.</param>
        /// <returns>Returns a theme group based on a tab.</returns>
        public static ThemeGroup GetTabThemeGroup(int tab) => tab switch
        {
            0 => ThemeGroup.Tab_Color_1,
            1 => ThemeGroup.Tab_Color_2,
            2 => ThemeGroup.Tab_Color_3,
            3 => ThemeGroup.Tab_Color_4,
            4 => ThemeGroup.Tab_Color_5,
            5 => ThemeGroup.Tab_Color_6,
            6 => ThemeGroup.Tab_Color_7,
            _ => ThemeGroup.Null,
        };

        /// <summary>
        /// Gets an event's theme group.
        /// </summary>
        /// <param name="type">Type of the event.</param>
        /// <returns>Returns a theme group based on an event.</returns>
        public static ThemeGroup GetEventColorThemeGroup(int type) => type switch
        {
            0 => ThemeGroup.Event_Color_1,
            1 => ThemeGroup.Event_Color_2,
            2 => ThemeGroup.Event_Color_3,
            3 => ThemeGroup.Event_Color_4,
            4 => ThemeGroup.Event_Color_5,
            5 => ThemeGroup.Event_Color_6,
            6 => ThemeGroup.Event_Color_7,
            7 => ThemeGroup.Event_Color_8,
            8 => ThemeGroup.Event_Color_9,
            9 => ThemeGroup.Event_Color_10,
            10 => ThemeGroup.Event_Color_11,
            11 => ThemeGroup.Event_Color_12,
            12 => ThemeGroup.Event_Color_13,
            13 => ThemeGroup.Event_Color_14,
            14 => ThemeGroup.Event_Color_15,
            _ => ThemeGroup.Null,
        };

        /// <summary>
        /// Gets an event's theme group.
        /// </summary>
        /// <param name="type">Type of the event.</param>
        /// <returns>Returns a theme group based on an event.</returns>
        public static ThemeGroup GetEventColorKeyframeThemeGroup(int type) => type switch
        {
            0 => ThemeGroup.Event_Color_1_Keyframe,
            1 => ThemeGroup.Event_Color_2_Keyframe,
            2 => ThemeGroup.Event_Color_3_Keyframe,
            3 => ThemeGroup.Event_Color_4_Keyframe,
            4 => ThemeGroup.Event_Color_5_Keyframe,
            5 => ThemeGroup.Event_Color_6_Keyframe,
            6 => ThemeGroup.Event_Color_7_Keyframe,
            7 => ThemeGroup.Event_Color_8_Keyframe,
            8 => ThemeGroup.Event_Color_9_Keyframe,
            9 => ThemeGroup.Event_Color_10_Keyframe,
            10 => ThemeGroup.Event_Color_11_Keyframe,
            11 => ThemeGroup.Event_Color_12_Keyframe,
            12 => ThemeGroup.Event_Color_13_Keyframe,
            13 => ThemeGroup.Event_Color_14_Keyframe,
            14 => ThemeGroup.Event_Color_15_Keyframe,
            _ => ThemeGroup.Null,
        };

        /// <summary>
        /// Gets an event's theme group.
        /// </summary>
        /// <param name="type">Type of the event.</param>
        /// <returns>Returns a theme group based on an event.</returns>
        public static ThemeGroup GetEventColorEditorThemeGroup(int type) => type switch
        {
            0 => ThemeGroup.Event_Color_1_Editor,
            1 => ThemeGroup.Event_Color_2_Editor,
            2 => ThemeGroup.Event_Color_3_Editor,
            3 => ThemeGroup.Event_Color_4_Editor,
            4 => ThemeGroup.Event_Color_5_Editor,
            5 => ThemeGroup.Event_Color_6_Editor,
            6 => ThemeGroup.Event_Color_7_Editor,
            7 => ThemeGroup.Event_Color_8_Editor,
            8 => ThemeGroup.Event_Color_9_Editor,
            9 => ThemeGroup.Event_Color_10_Editor,
            10 => ThemeGroup.Event_Color_11_Editor,
            11 => ThemeGroup.Event_Color_12_Editor,
            12 => ThemeGroup.Event_Color_13_Editor,
            13 => ThemeGroup.Event_Color_14_Editor,
            14 => ThemeGroup.Event_Color_15_Editor,
            _ => ThemeGroup.Null,
        };

        /// <summary>
        /// Gets an object event's theme group.
        /// </summary>
        /// <param name="type">Type of the object event.</param>
        /// <returns>Returns a theme group based on an object event.</returns>
        public static ThemeGroup GetObjectKeyframeThemeGroup(int type) => type switch
        {
            0 => ThemeGroup.Object_Keyframe_Color_1,
            1 => ThemeGroup.Object_Keyframe_Color_2,
            2 => ThemeGroup.Object_Keyframe_Color_3,
            3 => ThemeGroup.Object_Keyframe_Color_4,
            _ => ThemeGroup.Null,
        };

        /// <summary>
        /// Gets a notification types' theme group.
        /// </summary>
        /// <param name="type">Type of the notification.</param>
        /// <returns>Returns a theme group based on a notification type.</returns>
        public static ThemeGroup GetNotificationThemeGroup(EditorManager.NotificationType type) => type switch
        {
            EditorManager.NotificationType.Info => ThemeGroup.Notification_Info,
            EditorManager.NotificationType.Success => ThemeGroup.Notification_Success,
            EditorManager.NotificationType.Error => ThemeGroup.Notification_Error,
            EditorManager.NotificationType.Warning => ThemeGroup.Notification_Warning,
            _ => ThemeGroup.Null,
        };

        #endregion
    }

    /// <summary>
    /// Contains theme groups for <see cref="Selectable"/> elements.
    /// </summary>
    public class SelectableThemeGroup
    {
        public SelectableThemeGroup() { }

        public SelectableThemeGroup(ThemeGroup normal, ThemeGroup highlighted, ThemeGroup selected, ThemeGroup pressed, ThemeGroup disabled)
        {
            this.normal = normal;
            this.highlighted = highlighted;
            this.selected = selected;
            this.pressed = pressed;
            this.disabled = disabled;
        }

        /// <summary>
        /// Color for when a selectable is in a normal state.
        /// </summary>
        public ThemeGroup normal;

        /// <summary>
        /// Color for when a selectable is in a highlighted state.
        /// </summary>
        public ThemeGroup highlighted;

        /// <summary>
        /// Color for when a selectable is in a selected state.
        /// </summary>
        public ThemeGroup selected;

        /// <summary>
        /// Color for when a selectable is in a pressed state.
        /// </summary>
        public ThemeGroup pressed;

        /// <summary>
        /// Color for when a selectable is in a disabled state.
        /// </summary>
        public ThemeGroup disabled;

        /// <summary>
        /// Converts the <see cref="SelectableThemeGroup"/> to a <see cref="ColorBlock"/>.
        /// </summary>
        /// <param name="editorTheme"><see cref="EditorTheme"/> to get the colors from.</param>
        /// <returns>Returns a <see cref="ColorBlock"/> based on the current <see cref="SelectableThemeGroup"/> and the colors from <paramref name="editorTheme"/>.</returns>
        public ColorBlock ToColorBlock(EditorTheme editorTheme)
        {
            var colorBlock = EditorThemeManager.DefaultColorBlock;
            if (editorTheme.ColorGroups.TryGetValue(normal, out Color normalColor))
                colorBlock.normalColor = normalColor;
            if (editorTheme.ColorGroups.TryGetValue(highlighted, out Color highlightedColor))
                colorBlock.highlightedColor = highlightedColor;
            if (editorTheme.ColorGroups.TryGetValue(selected, out Color selectedColor))
                colorBlock.selectedColor = selectedColor;
            if (editorTheme.ColorGroups.TryGetValue(pressed, out Color pressedColor))
                colorBlock.pressedColor = pressedColor;
            if (editorTheme.ColorGroups.TryGetValue(disabled, out Color disabledColor))
                colorBlock.disabledColor = disabledColor;
            return colorBlock;
        }
    }

    /// <summary>
    /// Contains theme groups for <see cref="InputField"/> elements.
    /// </summary>
    public class InputFieldThemeGroup
    {
        public InputFieldThemeGroup() { }

        public InputFieldThemeGroup(ThemeGroup input, ThemeGroup text, ThemeGroup placeholder)
        {
            this.input = input;
            this.text = text;
            this.placeholder = placeholder;
        }

        public InputFieldThemeGroup(ThemeGroup input, ThemeGroup text) : this(input, text, text) { }
        
        /// <summary>
        /// Theme group of the input area.
        /// </summary>
        public ThemeGroup input;

        /// <summary>
        /// Theme group of the display text.
        /// </summary>
        public ThemeGroup text;

        /// <summary>
        /// Theme group of the placeholder graphic.
        /// </summary>
        public ThemeGroup placeholder; // placeholder should be transparent.
    }
}
