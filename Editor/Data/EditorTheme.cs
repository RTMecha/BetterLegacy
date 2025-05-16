using System;
using System.Collections.Generic;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;

namespace BetterLegacy.Editor.Data
{
    public class EditorTheme : Exists
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
            "Layer 1" => ThemeGroup.Layer_1,
            "Layer 2" => ThemeGroup.Layer_2,
            "Layer 3" => ThemeGroup.Layer_3,
            "Layer 4" => ThemeGroup.Layer_4,
            "Layer 5" => ThemeGroup.Layer_5,
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

        public static EditorTheme Parse(JSONNode jn)
        {
            var type = typeof(ThemeGroup);

            var colorGroups = new Dictionary<ThemeGroup, Color>();
            for (int i = 0; i < jn["groups"].Count; i++)
            {
                var colorJN = jn["groups"][i]["color"];
                string name = jn["groups"][i]["name"];
                if (Enum.TryParse(name, out ThemeGroup group))
                    colorGroups[group] = colorJN.IsObject ? new Color(colorJN["r"].AsFloat, colorJN["g"].AsFloat, colorJN["b"].AsFloat, colorJN["a"].AsFloat) : RTColors.HexToColor(colorJN);
            }

            var themeGroups = Enum.GetNames(type);
            for (int i = 0; i < themeGroups.Length; i++)
            {
                var themeGroup = Parser.TryParse(themeGroups[i], true, ThemeGroup.Null);
                if (!colorGroups.ContainsKey(themeGroup))
                    colorGroups[themeGroup] = Color.black;
            }

            return new EditorTheme(jn["name"], colorGroups);
        }

        public JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["name"] = name;

            int num = 0;
            foreach (var colorGroup in ColorGroups)
            {
                jn["groups"][num]["name"] = colorGroup.Key.ToString();
                jn["groups"][num]["color"] = RTColors.ColorToHexOptional(colorGroup.Value);
                num++;
            }

            return jn;
        }
    }
}
