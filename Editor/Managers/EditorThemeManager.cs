using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using TMPro;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Components;

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
                for (int i = 0; i < TemporaryEditorGUIElements.Count; i++)
                {
                    var element = TemporaryEditorGUIElements.ElementAt(i).Value;

                    element.ApplyTheme(theme);
                }
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

            if (element.gameObject == null)
                return;

            var id = LSText.randomNumString(16);
            element.gameObject.AddComponent<EditorThemeElement>().Init(element, id);

            TemporaryEditorGUIElements[id] = element;
        }

        public static List<Element> EditorGUIElements { get; set; } = new List<Element>();
        public static Dictionary<string, Element> TemporaryEditorGUIElements { get; set; } = new Dictionary<string, Element>();

        public static List<EditorTheme> EditorThemes { get; set; }

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

        public static void ApplyToggle(Toggle toggle, ThemeGroup checkGroup = ThemeGroup.Null, Text text = null)
        {
            toggle.image.fillCenter = true;
            ApplyElement(new Element(ThemeGroup.Toggle_1, toggle.gameObject, new List<Component>
            {
                toggle.image,
            }, true, 1, SpriteHelper.RoundedSide.W));

            var checkMarkGroup = checkGroup != ThemeGroup.Null ? checkGroup : ThemeGroup.Toggle_1_Check;
            ApplyElement(new Element(checkMarkGroup, toggle.graphic.gameObject, new List<Component>
            {
                toggle.graphic,
            }));

            if (text)
            {
                ApplyElement(new Element(checkMarkGroup, text.gameObject, new List<Component>
                {
                    text,
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

        public class EditorTheme
        {
            public EditorTheme(string name, Dictionary<ThemeGroup, Color> colorGroups)
            {
                this.name = name;
                ColorGroups = colorGroups;
            }

            public string name;

            public Dictionary<ThemeGroup, Color> ColorGroups { get; set; }

            public static ThemeGroup GetGroup(string group) => group switch
            {
                "Background" => ThemeGroup.Background_1,
                "Background 2" => ThemeGroup.Background_2,
                "Background 3" => ThemeGroup.Background_3,
                "Preview Cover" => ThemeGroup.Preview_Cover,
                "Scrollbar Handle" => ThemeGroup.Scrollbar_1_Handle,
                "Scrollbar Handle Normal" => ThemeGroup.Scrollbar_1_Handle_Normal,
                "Scrollbar Handle Highlight" => ThemeGroup.Scrollbar_1_Handle_Highlighted,
                "Scrollbar Handle Selected" => ThemeGroup.Scrollbar_1_Handle_Selected,
                "Scrollbar Handle Pressed" => ThemeGroup.Scrollbar_1_Handle_Pressed,
                "Scrollbar Handle Disabled" => ThemeGroup.Scrollbar_1_Handle_Disabled,
                "Scrollbar 2" => ThemeGroup.Scrollbar_2,
                "Scrollbar Handle 2" => ThemeGroup.Scrollbar_2_Handle,
                "Scrollbar Handle 2 Normal" => ThemeGroup.Scrollbar_2_Handle_Normal,
                "Scrollbar Handle 2 Highlight" => ThemeGroup.Scrollbar_2_Handle_Highlighted,
                "Scrollbar Handle 2 Selected" => ThemeGroup.Scrollbar_2_Handle_Selected,
                "Scrollbar Handle 2 Pressed" => ThemeGroup.Scrollbar_2_Handle_Pressed,
                "Scrollbar Handle 2 Disabled" => ThemeGroup.Scrollbar_2_Handle_Disabled,
                "Close" => ThemeGroup.Close,
                "Close Normal" => ThemeGroup.Close_Normal,
                "Close Highlight" => ThemeGroup.Close_Highlighted,
                "Close Selected" => ThemeGroup.Close_Selected,
                "Close Pressed" => ThemeGroup.Close_Pressed,
                "Close Disabled" => ThemeGroup.Close_Disabled,
                "Close X" => ThemeGroup.Close_X,
                "Picker" => ThemeGroup.Picker,
                "Picker Normal" => ThemeGroup.Picker_Normal,
                "Picker Highlight" => ThemeGroup.Picker_Highlighted,
                "Picker Selected" => ThemeGroup.Picker_Selected,
                "Picker Pressed" => ThemeGroup.Picker_Pressed,
                "Picker Disabled" => ThemeGroup.Picker_Disabled,
                "Picker Icon" => ThemeGroup.Picker_Icon,
                "Light Text" => ThemeGroup.Light_Text,
                "Dark Text" => ThemeGroup.Dark_Text,
                "Function 1" => ThemeGroup.Function_1,// 0F7BF8FF
                "Function 1 Text" => ThemeGroup.Function_1_Text,
                "Function 2" => ThemeGroup.Function_2,
                "Function 2 Normal" => ThemeGroup.Function_2_Normal,
                "Function 2 Highlight" => ThemeGroup.Function_2_Highlighted,
                "Function 2 Selected" => ThemeGroup.Function_2_Selected,
                "Function 2 Pressed" => ThemeGroup.Function_2_Pressed,
                "Function 2 Disabled" => ThemeGroup.Function_2_Disabled,
                "Function 2 Text" => ThemeGroup.Function_2_Text,
                "Function 3" => ThemeGroup.Function_3,
                "Function 3 Text" => ThemeGroup.Function_3_Text,
                "List Button 1" => ThemeGroup.List_Button_1,
                "List Button 1 Normal" => ThemeGroup.List_Button_1_Normal,
                "List Button 1 Highlight" => ThemeGroup.List_Button_1_Highlighted,
                "List Button 1 Selected" => ThemeGroup.List_Button_1_Selected,
                "List Button 1 Pressed" => ThemeGroup.List_Button_1_Pressed,
                "List Button 1 Disabled" => ThemeGroup.List_Button_1_Disabled,
                "List Button 2" => ThemeGroup.List_Button_2,
                "List Button 2 Normal" => ThemeGroup.List_Button_2_Normal,
                "List Button 2 Highlight" => ThemeGroup.List_Button_2_Highlighted,
                "List Button 2 Selected" => ThemeGroup.List_Button_2_Selected,
                "List Button 2 Pressed" => ThemeGroup.List_Button_2_Pressed,
                "List Button 2 Disabled" => ThemeGroup.List_Button_2_Disabled,
                "List Button 2 Text" => ThemeGroup.List_Button_2_Text,
                "Back Button" => ThemeGroup.Back_Button,
                "Back Button Text" => ThemeGroup.Back_Button_Text,
                "Folder Button" => ThemeGroup.Folder_Button,
                "Folder Button Text" => ThemeGroup.Folder_Button_Text,
                "Search Field 1" => ThemeGroup.Search_Field_1,
                "Search Field 1 Text" => ThemeGroup.Search_Field_1_Text,
                "Search Field 2" => ThemeGroup.Search_Field_2,
                "Search Field 2 Text" => ThemeGroup.Search_Field_2_Text,
                "Add" => ThemeGroup.Add,
                "Add Text" => ThemeGroup.Add_Text,
                "Delete" => ThemeGroup.Delete,
                "Delete Text" => ThemeGroup.Delete_Text,
                "Delete Keyframe BG" => ThemeGroup.Delete_Keyframe_BG,
                "Delete Keyframe Button" => ThemeGroup.Delete_Keyframe_Button,
                "Delete Keyframe Button Normal" => ThemeGroup.Delete_Keyframe_Button_Normal,
                "Delete Keyframe Button Highlight" => ThemeGroup.Delete_Keyframe_Button_Highlighted,
                "Delete Keyframe Button Selected" => ThemeGroup.Delete_Keyframe_Button_Selected,
                "Delete Keyframe Button Pressed" => ThemeGroup.Delete_Keyframe_Button_Pressed,
                "Delete Keyframe Button Disabled" => ThemeGroup.Delete_Keyframe_Button_Disabled,
                "Prefab" => ThemeGroup.Prefab,
                "Prefab Text" => ThemeGroup.Prefab_Text,
                "Object" => ThemeGroup.Object,
                "Object Text" => ThemeGroup.Object_Text,
                "Marker" => ThemeGroup.Marker,
                "Marker Text" => ThemeGroup.Marker_Text,
                "Checkpoint" => ThemeGroup.Checkpoint,
                "Checkpoint Text" => ThemeGroup.Checkpoint_Text,
                "Background Object" => ThemeGroup.Background_Object,
                "Background Object Text" => ThemeGroup.Background_Object_Text,
                "Timeline Bar" => ThemeGroup.Timeline_Bar,
                "Event/Check" => ThemeGroup.Event_Check,
                "Event/Check Text" => ThemeGroup.Event_Check_Text,
                "Dropdown 1" => ThemeGroup.Dropdown_1,
                "Dropdown 1 Overlay" => ThemeGroup.Dropdown_1_Overlay,
                "Dropdown 1 Item" => ThemeGroup.Dropdown_1_Item,
                "Toggle 1" => ThemeGroup.Toggle_1,
                "Toggle 1 Check" => ThemeGroup.Toggle_1_Check,
                "Input Field" => ThemeGroup.Input_Field,
                "Input Field Text" => ThemeGroup.Input_Field_Text,
                "Slider 1" => ThemeGroup.Slider_1,
                "Slider 1 Normal" => ThemeGroup.Slider_1_Normal,
                "Slider 1 Highlight" => ThemeGroup.Slider_1_Highlighted,
                "Slider 1 Selected" => ThemeGroup.Slider_1_Selected,
                "Slider 1 Pressed" => ThemeGroup.Slider_1_Pressed,
                "Slider 1 Disabled" => ThemeGroup.Slider_1_Disabled,
                "Slider 1 Handle" => ThemeGroup.Slider_1_Handle,
                "Slider" => ThemeGroup.Slider_2,
                "Slider Handle" => ThemeGroup.Slider_2_Handle,
                "Documentation" => ThemeGroup.Documentation,
                "Timeline Background" => ThemeGroup.Timeline_Background,
                "Timeline Scrollbar" => ThemeGroup.Timeline_Scrollbar,
                "Timeline Scrollbar Normal" => ThemeGroup.Timeline_Scrollbar_Normal,
                "Timeline Scrollbar Highlight" => ThemeGroup.Timeline_Scrollbar_Highlighted,
                "Timeline Scrollbar Selected" => ThemeGroup.Timeline_Scrollbar_Selected,
                "Timeline Scrollbar Pressed" => ThemeGroup.Timeline_Scrollbar_Pressed,
                "Timeline Scrollbar Disabled" => ThemeGroup.Timeline_Scrollbar_Disabled,
                "Timeline Scrollbar Base" => ThemeGroup.Timeline_Scrollbar_Base,
                "Timeline Time Scrollbar" => ThemeGroup.Timeline_Time_Scrollbar,
                "Title Bar Text" => ThemeGroup.Title_Bar_Text,
                "Title Bar Button" => ThemeGroup.Title_Bar_Button,
                "Title Bar Button Normal" => ThemeGroup.Title_Bar_Button_Normal,
                "Title Bar Button Highlight" => ThemeGroup.Title_Bar_Button_Highlighted,
                "Title Bar Button Selected" => ThemeGroup.Title_Bar_Button_Selected,
                "Title Bar Button Pressed" => ThemeGroup.Title_Bar_Button_Pressed,
                "Title Bar Dropdown" => ThemeGroup.Title_Bar_Dropdown,
                "Title Bar Dropdown Normal" => ThemeGroup.Title_Bar_Dropdown_Normal,
                "Title Bar Dropdown Highlight" => ThemeGroup.Title_Bar_Dropdown_Highlighted,
                "Title Bar Dropdown Selected" => ThemeGroup.Title_Bar_Dropdown_Selected,
                "Title Bar Dropdown Pressed" => ThemeGroup.Title_Bar_Dropdown_Pressed,
                "Title Bar Dropdown Disabled" => ThemeGroup.Title_Bar_Dropdown_Disabled,
                "Warning Confirm" => ThemeGroup.Warning_Confirm,
                "Warning Cancel" => ThemeGroup.Warning_Cancel,
                "Notification Background" => ThemeGroup.Notification_Background,
                "Notification Info" => ThemeGroup.Notification_Info,
                "Notification Success" => ThemeGroup.Notification_Success,
                "Notification Error" => ThemeGroup.Notification_Error,
                "Notification Warning" => ThemeGroup.Notification_Warning,
                "Copy" => ThemeGroup.Copy,
                "Copy Text" => ThemeGroup.Copy_Text,
                "Paste" => ThemeGroup.Paste,
                "Paste Text" => ThemeGroup.Paste_Text,
                "Tab Color 1" => ThemeGroup.Tab_Color_1,
                "Tab Color 1 Normal" => ThemeGroup.Tab_Color_1_Normal,
                "Tab Color 1 Highlight" => ThemeGroup.Tab_Color_1_Highlighted,
                "Tab Color 1 Selected" => ThemeGroup.Tab_Color_1_Selected,
                "Tab Color 1 Pressed" => ThemeGroup.Tab_Color_1_Pressed,
                "Tab Color 1 Disabled" => ThemeGroup.Tab_Color_1_Disabled,
                "Tab Color 2" => ThemeGroup.Tab_Color_2,
                "Tab Color 2 Normal" => ThemeGroup.Tab_Color_2_Normal,
                "Tab Color 2 Highlight" => ThemeGroup.Tab_Color_2_Highlighted,
                "Tab Color 2 Selected" => ThemeGroup.Tab_Color_2_Selected,
                "Tab Color 2 Pressed" => ThemeGroup.Tab_Color_2_Pressed,
                "Tab Color 2 Disabled" => ThemeGroup.Tab_Color_2_Disabled,
                "Tab Color 3" => ThemeGroup.Tab_Color_3,
                "Tab Color 3 Normal" => ThemeGroup.Tab_Color_3_Normal,
                "Tab Color 3 Highlight" => ThemeGroup.Tab_Color_3_Highlighted,
                "Tab Color 3 Selected" => ThemeGroup.Tab_Color_3_Selected,
                "Tab Color 3 Pressed" => ThemeGroup.Tab_Color_3_Pressed,
                "Tab Color 3 Disabled" => ThemeGroup.Tab_Color_3_Disabled,
                "Tab Color 4" => ThemeGroup.Tab_Color_4,
                "Tab Color 4 Normal" => ThemeGroup.Tab_Color_4_Normal,
                "Tab Color 4 Highlight" => ThemeGroup.Tab_Color_4_Highlighted,
                "Tab Color 4 Selected" => ThemeGroup.Tab_Color_4_Selected,
                "Tab Color 4 Pressed" => ThemeGroup.Tab_Color_4_Pressed,
                "Tab Color 4 Disabled" => ThemeGroup.Tab_Color_4_Disabled,
                "Tab Color 5" => ThemeGroup.Tab_Color_5,
                "Tab Color 5 Normal" => ThemeGroup.Tab_Color_5_Normal,
                "Tab Color 5 Highlight" => ThemeGroup.Tab_Color_5_Highlighted,
                "Tab Color 5 Selected" => ThemeGroup.Tab_Color_5_Selected,
                "Tab Color 5 Pressed" => ThemeGroup.Tab_Color_5_Pressed,
                "Tab Color 5 Disabled" => ThemeGroup.Tab_Color_5_Disabled,
                "Tab Color 6" => ThemeGroup.Tab_Color_6,
                "Tab Color 6 Normal" => ThemeGroup.Tab_Color_6_Normal,
                "Tab Color 6 Highlight" => ThemeGroup.Tab_Color_6_Highlighted,
                "Tab Color 6 Selected" => ThemeGroup.Tab_Color_6_Selected,
                "Tab Color 6 Pressed" => ThemeGroup.Tab_Color_6_Pressed,
                "Tab Color 6 Disabled" => ThemeGroup.Tab_Color_6_Disabled,
                "Tab Color 7" => ThemeGroup.Tab_Color_7,
                "Tab Color 7 Normal" => ThemeGroup.Tab_Color_7_Normal,
                "Tab Color 7 Highlight" => ThemeGroup.Tab_Color_7_Highlighted,
                "Tab Color 7 Selected" => ThemeGroup.Tab_Color_7_Selected,
                "Tab Color 7 Pressed" => ThemeGroup.Tab_Color_7_Pressed,
                "Tab Color 7 Disabled" => ThemeGroup.Tab_Color_7_Disabled,
                "Event Color 1" => ThemeGroup.Event_Color_1,// 1
                "Event Color 2" => ThemeGroup.Event_Color_2,// 2
                "Event Color 3" => ThemeGroup.Event_Color_3,// 3
                "Event Color 4" => ThemeGroup.Event_Color_4,// 4
                "Event Color 5" => ThemeGroup.Event_Color_5,// 5
                "Event Color 6" => ThemeGroup.Event_Color_6,// 6
                "Event Color 7" => ThemeGroup.Event_Color_7,// 7
                "Event Color 8" => ThemeGroup.Event_Color_8,// 8
                "Event Color 9" => ThemeGroup.Event_Color_9,// 9
                "Event Color 10" => ThemeGroup.Event_Color_10,// 10
                "Event Color 11" => ThemeGroup.Event_Color_11,// 11
                "Event Color 12" => ThemeGroup.Event_Color_12,// 12
                "Event Color 13" => ThemeGroup.Event_Color_13,// 13
                "Event Color 14" => ThemeGroup.Event_Color_14,// 14
                "Event Color 15" => ThemeGroup.Event_Color_15,// 15
                "Event Color 1 Keyframe" => ThemeGroup.Event_Color_1_Keyframe,// 1
                "Event Color 2 Keyframe" => ThemeGroup.Event_Color_2_Keyframe,// 2
                "Event Color 3 Keyframe" => ThemeGroup.Event_Color_3_Keyframe,// 3
                "Event Color 4 Keyframe" => ThemeGroup.Event_Color_4_Keyframe,// 4
                "Event Color 5 Keyframe" => ThemeGroup.Event_Color_5_Keyframe,// 5
                "Event Color 6 Keyframe" => ThemeGroup.Event_Color_6_Keyframe,// 6
                "Event Color 7 Keyframe" => ThemeGroup.Event_Color_7_Keyframe,// 7
                "Event Color 8 Keyframe" => ThemeGroup.Event_Color_8_Keyframe,// 8
                "Event Color 9 Keyframe" => ThemeGroup.Event_Color_9_Keyframe,// 9
                "Event Color 10 Keyframe" => ThemeGroup.Event_Color_10_Keyframe,// 10
                "Event Color 11 Keyframe" => ThemeGroup.Event_Color_11_Keyframe,// 11
                "Event Color 12 Keyframe" => ThemeGroup.Event_Color_12_Keyframe,// 12
                "Event Color 13 Keyframe" => ThemeGroup.Event_Color_13_Keyframe,// 13
                "Event Color 14 Keyframe" => ThemeGroup.Event_Color_14_Keyframe,// 14
                "Event Color 15 Keyframe" => ThemeGroup.Event_Color_15_Keyframe,// 15
                "Event Color 1 Editor" => ThemeGroup.Event_Color_1_Editor,// 1
                "Event Color 2 Editor" => ThemeGroup.Event_Color_2_Editor,// 2
                "Event Color 3 Editor" => ThemeGroup.Event_Color_3_Editor,// 3
                "Event Color 4 Editor" => ThemeGroup.Event_Color_4_Editor,// 4
                "Event Color 5 Editor" => ThemeGroup.Event_Color_5_Editor,// 5
                "Event Color 6 Editor" => ThemeGroup.Event_Color_6_Editor,// 6
                "Event Color 7 Editor" => ThemeGroup.Event_Color_7_Editor,// 7
                "Event Color 8 Editor" => ThemeGroup.Event_Color_8_Editor,// 8
                "Event Color 9 Editor" => ThemeGroup.Event_Color_9_Editor,// 9
                "Event Color 10 Editor" => ThemeGroup.Event_Color_10_Editor,// 10
                "Event Color 11 Editor" => ThemeGroup.Event_Color_11_Editor,// 11
                "Event Color 12 Editor" => ThemeGroup.Event_Color_12_Editor,// 12
                "Event Color 13 Editor" => ThemeGroup.Event_Color_13_Editor,// 13
                "Event Color 14 Editor" => ThemeGroup.Event_Color_14_Editor,// 14
                "Object Keyframe Color 1" => ThemeGroup.Object_Keyframe_Color_1,// 1
                "Object Keyframe Color 2" => ThemeGroup.Object_Keyframe_Color_2,// 2
                "Object Keyframe Color 3" => ThemeGroup.Object_Keyframe_Color_3,// 3
                "Object Keyframe Color 4" => ThemeGroup.Object_Keyframe_Color_4,// 4
                _ => ThemeGroup.Null,
            };

            public Color GetEventKeyframeColor(int type) => type switch
            {
                0 => ColorGroups[ThemeGroup.Event_Color_1_Keyframe],
                1 => ColorGroups[ThemeGroup.Event_Color_2_Keyframe],
                2 => ColorGroups[ThemeGroup.Event_Color_3_Keyframe],
                3 => ColorGroups[ThemeGroup.Event_Color_4_Keyframe],
                4 => ColorGroups[ThemeGroup.Event_Color_5_Keyframe],
                5 => ColorGroups[ThemeGroup.Event_Color_6_Keyframe],
                6 => ColorGroups[ThemeGroup.Event_Color_7_Keyframe],
                7 => ColorGroups[ThemeGroup.Event_Color_8_Keyframe],
                8 => ColorGroups[ThemeGroup.Event_Color_9_Keyframe],
                9 => ColorGroups[ThemeGroup.Event_Color_10_Keyframe],
                10 => ColorGroups[ThemeGroup.Event_Color_11_Keyframe],
                11 => ColorGroups[ThemeGroup.Event_Color_12_Keyframe],
                12 => ColorGroups[ThemeGroup.Event_Color_13_Keyframe],
                13 => ColorGroups[ThemeGroup.Event_Color_14_Keyframe],
                _ => Color.white,
            };

            public Color GetObjectKeyframeColor(int type) => type switch
            {
                0 => ColorGroups[ThemeGroup.Object_Keyframe_Color_1],
                1 => ColorGroups[ThemeGroup.Object_Keyframe_Color_2],
                2 => ColorGroups[ThemeGroup.Object_Keyframe_Color_3],
                3 => ColorGroups[ThemeGroup.Object_Keyframe_Color_4],
                _ => Color.white,
            };

            public static string GetString(ThemeGroup group) => group switch
            {
                ThemeGroup.Background_1 => "Background",
                ThemeGroup.Scrollbar_1_Handle => "Scrollbar Handle",
                ThemeGroup.Scrollbar_1_Handle_Normal => "Scrollbar Handle Normal",
                ThemeGroup.Scrollbar_1_Handle_Highlighted => "Scrollbar Handle Highlight",
                ThemeGroup.Scrollbar_1_Handle_Selected => "Scrollbar Handle Selected",
                ThemeGroup.Scrollbar_1_Handle_Pressed => "Scrollbar Handle Pressed",
                ThemeGroup.Scrollbar_1_Handle_Disabled => "Scrollbar Handle Disabled",
                ThemeGroup.Scrollbar_2 => "Scrollbar 2",
                ThemeGroup.Scrollbar_2_Handle => "Scrollbar Handle 2",
                ThemeGroup.Scrollbar_2_Handle_Normal => "Scrollbar Handle 2 Normal",
                ThemeGroup.Scrollbar_2_Handle_Highlighted => "Scrollbar Handle 2 Highlight",
                ThemeGroup.Scrollbar_2_Handle_Selected => "Scrollbar Handle 2 Selected",
                ThemeGroup.Scrollbar_2_Handle_Pressed => "Scrollbar Handle 2 Pressed",
                ThemeGroup.Scrollbar_2_Handle_Disabled => "Scrollbar Handle 2 Disabled",
                ThemeGroup.Close_Highlighted => "Close Highlight",
                ThemeGroup.Function_2_Highlighted => "Function 2 Highlight",
                ThemeGroup.List_Button_1_Highlighted => "List Button 1 Highlight",
                ThemeGroup.List_Button_2_Highlighted => "List Button 2 Highlight",
                ThemeGroup.Delete_Keyframe_Button_Highlighted => "Delete Keyframe Button Highlight",
                ThemeGroup.Event_Check => "Event/Check",
                ThemeGroup.Event_Check_Text => "Event/Check Text",
                ThemeGroup.Slider_2 => "Slider",
                ThemeGroup.Slider_2_Handle => "Slider Handle",
                ThemeGroup.Timeline_Scrollbar_Highlighted => "Timeline Scrollbar Highlight",
                ThemeGroup.Title_Bar_Button_Highlighted => "Title Bar Button Highlight",
                ThemeGroup.Title_Bar_Dropdown_Highlighted => "Title Bar Dropdown Highlight",
                ThemeGroup.Tab_Color_1_Highlighted => "Tab Color 1 Highlight",
                ThemeGroup.Tab_Color_2_Highlighted => "Tab Color 2 Highlight",
                ThemeGroup.Tab_Color_3_Highlighted => "Tab Color 3 Highlight",
                ThemeGroup.Tab_Color_4_Highlighted => "Tab Color 4 Highlight",
                ThemeGroup.Tab_Color_5_Highlighted => "Tab Color 5 Highlight",
                ThemeGroup.Tab_Color_6_Highlighted => "Tab Color 6 Highlight",
                ThemeGroup.Tab_Color_7_Highlighted => "Tab Color 7 Highlight",
                _ => group.ToString().Replace("_", " "),
            };

            public Color GetColor(string group) => ColorGroups[GetGroup(group)];

            public bool ContainsGroup(string group) => GetGroup(group) != ThemeGroup.Null;
        }

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
                        if (component is Image image)
                            image.color = color;
                        if (component is Text text)
                            text.color = color;
                        if (component is TextMeshProUGUI textMeshPro)
                            textMeshPro.color = color;
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
