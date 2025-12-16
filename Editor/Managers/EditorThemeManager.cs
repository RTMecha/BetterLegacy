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
        public static void ApplyGraphic(Graphic graphic, ThemeGroup group, bool canSetRounded = false, int rounded = 1, SpriteHelper.RoundedSide roundedSide = SpriteHelper.RoundedSide.W)
        {
            if (!graphic)
                return;

            ApplyElement(new EditorThemeElement(group, graphic.gameObject, new Component[]
            {
                graphic,
            }, canSetRounded, rounded, roundedSide));
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
            ApplyGraphic(inputField.textComponent, EditorTheme.GetGroup($"{EditorTheme.GetString(group)} Text"));
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
            ApplyGraphic(inputField.textComponent, EditorTheme.GetGroup($"{EditorTheme.GetString(group)} Text"));
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
        /// Applies a theme to an array of component.
        /// </summary>
        /// <param name="theme">Theme to get the color from.</param>
        /// <param name="themeGroup">Color group to assign.</param>
        /// <param name="isSelectable">If the components are selectable.</param>
        /// <param name="components">Array of components.</param>
        public static void ApplyTheme(EditorTheme theme, ThemeGroup themeGroup, bool isSelectable, params Component[] components)
        {
            try
            {
                if (themeGroup == ThemeGroup.Null)
                    return;

                if (!theme.ColorGroups.TryGetValue(themeGroup, out Color color))
                    return;

                if (!isSelectable)
                    SetColor(color, components);
                else
                {
                    var colorBlock = new ColorBlock
                    {
                        colorMultiplier = 1f,
                        fadeDuration = 0.1f
                    };

                    var space = EditorTheme.GetString(themeGroup);
                    var normalGroup = EditorTheme.GetGroup(space + " Normal");
                    var highlightGroup = EditorTheme.GetGroup(space + " Highlight");
                    var selectedGroup = EditorTheme.GetGroup(space + " Selected");
                    var pressedGroup = EditorTheme.GetGroup(space + " Pressed");
                    var disabledGroup = EditorTheme.GetGroup(space + " Disabled");

                    if (theme.ColorGroups.TryGetValue(normalGroup, out Color normalColor))
                        colorBlock.normalColor = normalColor;

                    if (theme.ColorGroups.TryGetValue(highlightGroup, out Color highlightedColor))
                        colorBlock.highlightedColor = highlightedColor;

                    if (theme.ColorGroups.TryGetValue(selectedGroup, out Color selectedColor))
                        colorBlock.selectedColor = selectedColor;

                    if (theme.ColorGroups.TryGetValue(pressedGroup, out Color pressedColor))
                        colorBlock.pressedColor = pressedColor;

                    if (theme.ColorGroups.TryGetValue(disabledGroup, out Color disabledColor))
                        colorBlock.disabledColor = disabledColor;

                    SetColor(color, colorBlock, components);
                }
            }
            catch
            {

            }
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

        #endregion
    }
}
