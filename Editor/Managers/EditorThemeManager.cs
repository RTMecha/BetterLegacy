using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

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
        public static void Update()
        {
            if (!CoreHelper.InEditor && EditorGUIElements.Count > 0)
                Clear();
        }

        public static void Clear()
        {
            var elements = EditorGUIElements;

            if (!elements.IsEmpty())
                for (int i = 0; i < elements.Count; i++)
                    elements[i]?.Clear();

            elements.Clear();
        }

        public static IEnumerator RenderElements()
        {
            var theme = CurrentTheme;

            for (int i = 0; i < EditorGUIElements.Count; i++)
                EditorGUIElements[i].ApplyTheme(theme);

            try
            {
                foreach (var temp in TemporaryEditorGUIElements)
                    temp.Value.ApplyTheme(theme);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            if (EditorTimeline.inst && EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
            {
                RTEventEditor.inst.RenderLayerBins();
                if (EventEditor.inst.dialogRight.gameObject.activeInHierarchy)
                    RTEventEditor.inst.RenderEventsDialog();
            }

            yield break;
        }

        public static void LoadEditorThemes()
        {
            EditorThemes.Clear();

            var jn = JSON.Parse(RTFile.ReadFromFile(AssetPack.GetFile("editor/data/editor_themes.json")));

            for (int i = 0; i < jn["themes"].Count; i++)
                EditorThemes.Add(EditorTheme.Parse(jn["themes"][i]));
        }

        public static void SaveEditorThemes()
        {
            var jn = Parser.NewJSONObject();
            for (int i = 0; i < EditorThemes.Count; i++)
                jn["themes"][i] = EditorThemes[i].ToJSON();
            RTFile.WriteToFile(AssetPack.GetFile("editor/data/editor_themes.json"), jn.ToString());
        }

        public static EditorTheme CurrentTheme => EditorThemes[Mathf.Clamp(currentTheme, 0, EditorThemes.Count - 1)];
        public static int currentTheme = 0;

        public static void AddElement(Element element)
        {
            EditorGUIElements.Add(element);
            element.ApplyTheme(CurrentTheme);
        }

        public static void ApplyElement(Element element)
        {
            element.ApplyTheme(CurrentTheme);

            if (!element.gameObject)
                return;

            var id = LSText.randomNumString(16);
            element.gameObject.AddComponent<EditorThemeElement>().Init(element, id);

            TemporaryEditorGUIElements[id] = element;
        }

        public static List<Element> EditorGUIElements { get; set; } = new List<Element>();
        public static Dictionary<string, Element> TemporaryEditorGUIElements { get; set; } = new Dictionary<string, Element>();

        public static List<EditorTheme> EditorThemes { get; set; } = new List<EditorTheme>();

        public static Dictionary<string, EditorTheme> EditorThemesDictionary => EditorThemes.ToDictionary(x => x.name, x => x);

        public static void AddDropdown(Dropdown dropdown)
        {
            AddGraphic(dropdown.image, ThemeGroup.Dropdown_1, true);
            AddGraphic(dropdown.captionText, ThemeGroup.Dropdown_1_Overlay);
            AddGraphic(dropdown.transform.Find("Arrow").GetComponent<Image>(), ThemeGroup.Dropdown_1_Overlay);
            if (dropdown.captionImage)
                AddGraphic(dropdown.captionImage, ThemeGroup.Dropdown_1_Overlay);

            var template = dropdown.template.gameObject;
            AddGraphic(template.GetComponent<Image>(), ThemeGroup.Dropdown_1, true, roundedSide: SpriteHelper.RoundedSide.Bottom);

            var templateItem = template.transform.Find("Viewport/Content/Item");
            AddGraphic(templateItem.Find("Item Background").GetComponent<Image>(), ThemeGroup.Dropdown_1_Item, true);
            AddGraphic(templateItem.Find("Item Checkmark").GetComponent<Image>(), ThemeGroup.Dropdown_1_Overlay);
            AddGraphic(dropdown.itemText, ThemeGroup.Dropdown_1_Overlay);
            if (dropdown.itemImage)
                AddGraphic(dropdown.itemImage, ThemeGroup.Dropdown_1_Overlay);
        }

        public static void ApplyDropdown(Dropdown dropdown)
        {
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

        public static void AddInputField(Vector2InputFieldStorage vector2InputFieldStorage)
        {
            if (vector2InputFieldStorage.x)
                AddInputField(vector2InputFieldStorage.x);
            if (vector2InputFieldStorage.y)
                AddInputField(vector2InputFieldStorage.y);
        }

        public static void AddInputField(InputFieldStorage inputFieldStorage)
        {
            if (inputFieldStorage.inputField)
                AddInputField(inputFieldStorage.inputField);
            if (inputFieldStorage.subButton)
                AddSelectable(inputFieldStorage.subButton, ThemeGroup.Function_2, false);
            if (inputFieldStorage.addButton)
                AddSelectable(inputFieldStorage.addButton, ThemeGroup.Function_2, false);
            if (inputFieldStorage.leftGreaterButton)
                AddSelectable(inputFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
            if (inputFieldStorage.leftButton)
                AddSelectable(inputFieldStorage.leftButton, ThemeGroup.Function_2, false);
            if (inputFieldStorage.middleButton)
                AddSelectable(inputFieldStorage.middleButton, ThemeGroup.Function_2, false);
            if (inputFieldStorage.rightButton)
                AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);
            if (inputFieldStorage.rightGreaterButton)
                AddSelectable(inputFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);
        }

        public static void ApplyInputField(InputFieldStorage inputFieldStorage)
        {
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

        public static void AddInputField(InputField inputField, ThemeGroup group = ThemeGroup.Input_Field, int rounded = 1, SpriteHelper.RoundedSide roundedSide = SpriteHelper.RoundedSide.W)
        {
            inputField.image.fillCenter = true;
            AddElement(new Element(group, inputField.gameObject, new List<Component>
            {
                inputField.image,
            }, true, rounded, roundedSide));

            AddElement(new Element(EditorTheme.GetGroup($"{EditorTheme.GetString(group)} Text"), inputField.textComponent.gameObject, new List<Component>
            {
                inputField.textComponent,
            }));
        }

        public static void AddInputField(TMP_InputField inputField, ThemeGroup group = ThemeGroup.Input_Field, int rounded = 1, SpriteHelper.RoundedSide roundedSide = SpriteHelper.RoundedSide.W)
        {
            inputField.image.fillCenter = true;
            AddElement(new Element(group, inputField.gameObject, new List<Component>
            {
                inputField.image,
            }, true, rounded, roundedSide));

            AddElement(new Element(EditorTheme.GetGroup($"{EditorTheme.GetString(group)} Text"), inputField.textComponent.gameObject, new List<Component>
            {
                inputField.textComponent,
            }));
        }

        public static void ApplyInputField(InputField inputField, ThemeGroup group = ThemeGroup.Input_Field, SpriteHelper.RoundedSide roundedSide = SpriteHelper.RoundedSide.W)
        {
            inputField.image.fillCenter = true;
            ApplyElement(new Element(group, inputField.gameObject, new List<Component>
            {
                inputField.image,
            }, true, 1, roundedSide));

            ApplyElement(new Element(EditorTheme.GetGroup($"{EditorTheme.GetString(group)} Text"), inputField.textComponent.gameObject, new List<Component>
            {
                inputField.textComponent,
            }));
        }

        public static void AddInputFields(GameObject gameObject, bool self, string name, bool selfInput = false, bool searchChildren = true)
        {
            if (!searchChildren)
            {
                var inputField = gameObject.GetComponent<InputField>();

                if (!inputField)
                    return;

                var input = selfInput ? inputField.transform : gameObject.transform.Find("input") ?? gameObject.transform.Find("Input") ?? gameObject.transform.Find("text-field");

                AddElement(new Element(ThemeGroup.Input_Field, input.gameObject, new List<Component>
                {
                    selfInput ? inputField.image : input.GetComponent<Image>(),
                }, true, 1, SpriteHelper.RoundedSide.W));

                AddElement(new Element(ThemeGroup.Input_Field_Text, inputField.textComponent.gameObject, new List<Component>
                {
                    inputField.textComponent,
                }));

                var buttonLeft = self ? gameObject.transform.Find("<") : gameObject.transform.parent.Find("<");
                var buttonRight = self ? gameObject.transform.Find(">") : gameObject.transform.parent.Find(">");

                if (!buttonLeft || !buttonRight)
                    return;

                var buttonLeftComponent = buttonLeft.GetComponent<Button>();
                var buttonRightComponent = buttonRight.GetComponent<Button>();

                UnityEngine.Object.Destroy(buttonLeftComponent.GetComponent<Animator>());
                buttonLeftComponent.transition = Selectable.Transition.ColorTint;

                UnityEngine.Object.Destroy(buttonRightComponent.GetComponent<Animator>());
                buttonRightComponent.transition = Selectable.Transition.ColorTint;

                AddSelectable(buttonLeftComponent, ThemeGroup.Function_2, false);
                AddSelectable(buttonRightComponent, ThemeGroup.Function_2, false);

                return;
            }

            for (int j = 0; j < gameObject.transform.childCount; j++)
            {
                var child = gameObject.transform.GetChild(j);

                var inputField = child.GetComponent<InputField>();

                if (!inputField)
                    continue;

                var input = selfInput ? inputField.transform : child.Find("input") ?? child.Find("Input") ?? child.Find("text-field");

                AddElement(new Element(ThemeGroup.Input_Field, input.gameObject, new List<Component>
                {
                    selfInput ? inputField.image : input.GetComponent<Image>(),
                }, true, 1, SpriteHelper.RoundedSide.W));

                AddElement(new Element(ThemeGroup.Input_Field_Text, inputField.textComponent.gameObject, new List<Component>
                {
                    inputField.textComponent,
                }));

                var buttonLeft = self ? child.Find("<") : child.parent.Find("<");
                var buttonRight = self ? child.Find(">") : child.parent.Find(">");

                if (!buttonLeft || !buttonRight)
                    continue;

                var buttonLeftComponent = buttonLeft.GetComponent<Button>();
                var buttonRightComponent = buttonRight.GetComponent<Button>();

                UnityEngine.Object.Destroy(buttonLeftComponent.GetComponent<Animator>());
                buttonLeftComponent.transition = Selectable.Transition.ColorTint;

                UnityEngine.Object.Destroy(buttonRightComponent.GetComponent<Animator>());
                buttonRightComponent.transition = Selectable.Transition.ColorTint;

                AddSelectable(buttonLeftComponent, ThemeGroup.Function_2, false);
                AddSelectable(buttonRightComponent, ThemeGroup.Function_2, false);
            }
        }

        public static void AddToggle(Toggle toggle, ThemeGroup checkGroup = ThemeGroup.Null, Graphic graphic = null)
        {
            toggle.image.fillCenter = true;
            AddElement(new Element(ThemeGroup.Toggle_1, toggle.gameObject, new List<Component>
            {
                toggle.image,
            }, true, 1, SpriteHelper.RoundedSide.W));

            var checkMarkGroup = checkGroup != ThemeGroup.Null ? checkGroup : ThemeGroup.Toggle_1_Check;
            AddElement(new Element(checkMarkGroup, toggle.graphic.gameObject, new List<Component>
            {
                toggle.graphic,
            }));

            if (graphic)
            {
                AddElement(new Element(checkMarkGroup, graphic.gameObject, new List<Component>
                {
                    graphic,
                }));
                return;
            }

            if (toggle.transform.Find("Text"))
                AddElement(new Element(checkMarkGroup, toggle.transform.Find("Text").gameObject, new List<Component>
                {
                    toggle.transform.Find("Text").GetComponent<Text>(),
                }));

            if (toggle.transform.Find("text"))
                AddElement(new Element(checkMarkGroup, toggle.transform.Find("text").gameObject, new List<Component>
                {
                    toggle.transform.Find("text").GetComponent<Text>(),
                }));
        }

        public static void ApplyToggle(Toggle toggle, ThemeGroup checkGroup = ThemeGroup.Null, Graphic graphic = null)
        {
            toggle.image.fillCenter = true;
            ApplyElement(new Element(ThemeGroup.Toggle_1, toggle.image.gameObject, new List<Component>
            {
                toggle.image,
            }, true, 1, SpriteHelper.RoundedSide.W));

            var checkMarkGroup = checkGroup != ThemeGroup.Null ? checkGroup : ThemeGroup.Toggle_1_Check;
            ApplyElement(new Element(checkMarkGroup, toggle.graphic.gameObject, new List<Component>
            {
                toggle.graphic,
            }));

            if (graphic)
            {
                ApplyElement(new Element(checkMarkGroup, graphic.gameObject, new List<Component>
                {
                    graphic,
                }));
                return;
            }

            if (toggle.transform.Find("Text"))
                ApplyElement(new Element(checkMarkGroup, toggle.transform.Find("Text").gameObject, new List<Component>
                {
                    toggle.transform.Find("Text").GetComponent<Text>(),
                }));

            if (toggle.transform.Find("text"))
                ApplyElement(new Element(checkMarkGroup, toggle.transform.Find("text").gameObject, new List<Component>
                {
                    toggle.transform.Find("text").GetComponent<Text>(),
                }));
        }

        public static void AddLightText(Text text)
        {
            AddElement(new Element(ThemeGroup.Light_Text, text.gameObject, new List<Component>
            {
                text,
            }));
        }

        public static void ApplyLightText(Text text)
        {
            ApplyElement(new Element(ThemeGroup.Light_Text, text.gameObject, new List<Component>
            {
                text,
            }));
        }

        public static void AddLightText(TextMeshProUGUI text)
        {
            AddElement(new Element(ThemeGroup.Light_Text, text.gameObject, new List<Component>
            {
                text,
            }));
        }

        public static void ApplyLightText(TextMeshProUGUI text)
        {
            ApplyElement(new Element(ThemeGroup.Light_Text, text.gameObject, new List<Component>
            {
                text,
            }));
        }

        public static void AddSelectable(Selectable selectable, ThemeGroup group, bool canSetRounded = true, int rounded = 1, SpriteHelper.RoundedSide roundedSide = SpriteHelper.RoundedSide.W)
        {
            AddElement(new Element(group, selectable.gameObject, new List<Component>
            {
                selectable.image,
                selectable,
            }, canSetRounded, rounded, roundedSide, true));
        }

        public static void ApplySelectable(Selectable selectable, ThemeGroup group, bool canSetRounded = true, int rounded = 1, SpriteHelper.RoundedSide roundedSide = SpriteHelper.RoundedSide.W)
        {
            ApplyElement(new Element(group, selectable.gameObject, new List<Component>
            {
                selectable.image,
                selectable,
            }, canSetRounded, rounded, roundedSide, true));
        }

        public static void AddGraphic(Graphic graphic, ThemeGroup group, bool canSetRounded = false, int rounded = 1, SpriteHelper.RoundedSide roundedSide = SpriteHelper.RoundedSide.W)
        {
            AddElement(new Element(group, graphic.gameObject, new List<Component>
            {
                graphic,
            }, canSetRounded, rounded, roundedSide));
        }

        public static void ApplyGraphic(Graphic graphic, ThemeGroup group, bool canSetRounded = false, int rounded = 1, SpriteHelper.RoundedSide roundedSide = SpriteHelper.RoundedSide.W)
        {
            ApplyElement(new Element(group, graphic.gameObject, new List<Component>
            {
                graphic,
            }, canSetRounded, rounded, roundedSide));
        }

        public static void AddScrollbar(Scrollbar scrollbar, Image backgroundImage = null, ThemeGroup scrollbarGroup = ThemeGroup.Background_1, ThemeGroup handleGroup = ThemeGroup.Scrollbar_1_Handle,
            bool canSetScrollbarRounded = true, bool canSetHandleRounded = true, int scrollbarRounded = 1, int handleRounded = 1,
            SpriteHelper.RoundedSide scrollbarRoundedSide = SpriteHelper.RoundedSide.W, SpriteHelper.RoundedSide handleRoundedSide = SpriteHelper.RoundedSide.W)
        {
            AddGraphic(backgroundImage ?? scrollbar.GetComponent<Image>(), scrollbarGroup, canSetScrollbarRounded, scrollbarRounded, scrollbarRoundedSide);

            AddElement(new Element(handleGroup, scrollbar.image.gameObject, new List<Component>
            {
                scrollbar.image,
                scrollbar
            }, canSetHandleRounded, handleRounded, handleRoundedSide, true));
        }

        public static void ApplyScrollbar(Scrollbar scrollbar, Image backgroundImage = null, ThemeGroup scrollbarGroup = ThemeGroup.Background_1, ThemeGroup handleGroup = ThemeGroup.Scrollbar_1_Handle,
            bool canSetScrollbarRounded = true, bool canSetHandleRounded = true, int scrollbarRounded = 1, int handleRounded = 1,
            SpriteHelper.RoundedSide scrollbarRoundedSide = SpriteHelper.RoundedSide.W, SpriteHelper.RoundedSide handleRoundedSide = SpriteHelper.RoundedSide.W)
        {
            ApplyGraphic(backgroundImage ?? scrollbar.GetComponent<Image>(), scrollbarGroup, canSetScrollbarRounded, scrollbarRounded, scrollbarRoundedSide);

            ApplyElement(new Element(handleGroup, scrollbar.image.gameObject, new List<Component>
            {
                scrollbar.image,
                scrollbar
            }, canSetHandleRounded, handleRounded, handleRoundedSide, true));
        }

        public static void AddSlider(Slider slider, Image backgroundImage = null, ThemeGroup sliderGroup = ThemeGroup.Slider_2, ThemeGroup handleGroup = ThemeGroup.Slider_2_Handle,
            bool canSetSliderRounded = true, bool canSetHandleRounded = true, int sliderRounded = 1, int handleRounded = 1,
            SpriteHelper.RoundedSide sliderRoundedSide = SpriteHelper.RoundedSide.W, SpriteHelper.RoundedSide handleRoundedSide = SpriteHelper.RoundedSide.W, bool selectable = false)
        {
            AddGraphic(backgroundImage ?? slider.GetComponent<Image>(), sliderGroup, canSetSliderRounded, sliderRounded, sliderRoundedSide);

            AddElement(new Element(handleGroup, slider.image.gameObject, new List<Component>
            {
                slider.image,
                slider
            }, canSetHandleRounded, handleRounded, handleRoundedSide, selectable));
        }

        public static void ApplySlider(Slider slider, Image backgroundImage = null, ThemeGroup sliderGroup = ThemeGroup.Slider_2, ThemeGroup handleGroup = ThemeGroup.Slider_2_Handle,
            bool canSetSliderRounded = true, bool canSetHandleRounded = true, int sliderRounded = 1, int handleRounded = 1,
            SpriteHelper.RoundedSide sliderRoundedSide = SpriteHelper.RoundedSide.W, SpriteHelper.RoundedSide handleRoundedSide = SpriteHelper.RoundedSide.W, bool selectable = false)
        {
            ApplyGraphic(backgroundImage ?? slider.GetComponent<Image>(), sliderGroup, canSetSliderRounded, sliderRounded, sliderRoundedSide);

            ApplyElement(new Element(handleGroup, slider.image.gameObject, new List<Component>
            {
                slider.image,
                slider
            }, canSetHandleRounded, handleRounded, handleRoundedSide, selectable));
        }

        public static void ClearSelectableColors(Selectable selectable) => selectable.colors = new ColorBlock()
        {
            normalColor = Color.white,
            highlightedColor = Color.white,
            pressedColor = Color.white,
            selectedColor = Color.white,
            disabledColor = Color.white,
            colorMultiplier = 1f,
            fadeDuration = 0.2f,
        };

        public class Element
        {
            public Element(ThemeGroup group, GameObject gameObject, List<Component> components, bool canSetRounded = false, int rounded = 0, SpriteHelper.RoundedSide roundedSide = SpriteHelper.RoundedSide.W, bool isSelectable = false)
            {
                themeGroup = group;
                this.gameObject = gameObject;
                this.components = components.ToArray(); // replaced List with Array
                this.canSetRounded = canSetRounded;
                this.rounded = rounded;
                this.roundedSide = roundedSide;
                this.isSelectable = isSelectable;
            }

            readonly ThemeGroup themeGroup = ThemeGroup.Null;

            public GameObject gameObject;

            Component[] components;

            readonly bool isSelectable = false;

            readonly bool canSetRounded = false;

            readonly int rounded;

            readonly SpriteHelper.RoundedSide roundedSide = SpriteHelper.RoundedSide.W;

            public void Clear()
            {
                gameObject = null;
                for (int i = 0; i < components.Length; i++)
                    components[i] = null;
                components = null;
            }

            public void ApplyTheme(EditorTheme theme)
            {
                try
                {
                    SetRounded();

                    if (themeGroup == ThemeGroup.Null)
                        return;

                    if (!theme.ColorGroups.TryGetValue(themeGroup, out Color color))
                        return;

                    if (!isSelectable)
                        SetColor(color);
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

                        SetColor(color, colorBlock);
                    }
                }
                catch
                {

                }
            }

            public void SetColor(Color color)
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
                            text.text = "";
                            text.text = str;
                        }
                    }
                }
            }

            public void SetColor(Color color, ColorBlock colorBlock)
            {
                foreach (var component in components)
                {
                    if (component is Image image)
                        image.color = color;
                    if (component is Selectable button)
                        button.colors = colorBlock;
                }
            }

            public void SetRounded()
            {
                if (!canSetRounded)
                    return;

                var canSet = EditorConfig.Instance.RoundedUI.Value;

                foreach (var component in components)
                {
                    if (component is Image image)
                    {
                        if (rounded != 0 && canSet)
                            SpriteHelper.SetRoundedSprite(image, rounded, roundedSide);
                        else
                            image.sprite = null;
                    }
                }
            }

            public override string ToString() => gameObject.name;
        }
    }
}
