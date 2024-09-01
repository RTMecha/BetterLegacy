﻿namespace BetterLegacy
{
    public enum EditorTheme
    {
        Legacy,
        Dark,
        Light,
        Vision,
        Butter,
        Arrhythmia,
        Modern,
        Beats,
        Archives,
        Void
    }

    public enum EditorFont
    {
        Inconsolata_Variable,
        Fredoka_One,
        Pusab,
        Revue,
        Transformers_Movie,
        Ancient_Autobot,
        Determination_Mono,
        Flow_Circular,
        Arrhythmia,
        Angsana,
        About_Friend,
        VAG_Rounded,
    }

    public enum SceneType
    {
        Interface,
        Game,
        Editor,
    }

    public enum SceneName
    {
        Main_Menu,
        Input_Select,
        Game,
        Editor,
        Interface,
        post_level,
        Arcade_Level
    }

    public enum LoadingDisplayType
    {
        Doggo,
        Waveform,
        Bar,
        Percentage,
        EqualsBar
    }

    public enum YieldType
    {
        None,
        Delay,
        Null,
        EndOfFrame,
        FixedUpdate,
    }
    
    public enum Rank
    {
        Null,
        SS,
        S,
        A,
        B,
        C,
        D,
        F
    }

    /// <summary>
    /// How a level list should be sorted.
    /// </summary>
    public enum LevelSort
    {
        /// <summary>
        /// The default. Levels without icons are usually sorted first.
        /// </summary>
        Cover,
        Artist,
        Creator,
        File,
        Title,
        Difficulty,
        DateEdited,
        DateCreated,
        DatePublished,
    }

    public enum MenuMusicLoadMode
    {
        /// <summary>
        /// Takes from Project Arrhythmia/settings/menus
        /// </summary>
        Settings,
        /// <summary>
        /// Takes from story folder.
        /// </summary>
        StoryFolder,
        /// <summary>
        /// Takes from editor folder.
        /// </summary>
        EditorFolder,
        /// <summary>
        /// Takes from global folder.
        /// </summary>
        GlobalFolder
    }

    public enum ServerVisibility
    {
        Public,
        Unlisted,
        Private
    }

    /// <summary>
    /// How <i>complex</i> something is.<br></br><br></br>Can be used for <i>Examples</i>' tutorials or for displaying editor features. If used for editor features, Normal is vanilla and Advanced is fully modded.
    /// </summary>
    public enum Complexity
    {
        /// <summary>
        /// If used for editor feature display, only show the most basic of features.
        /// </summary>
        Simple,
        /// <summary>
        /// If used for editor feature display, only show vanilla features.
        /// </summary>
        Normal,
        /// <summary>
        /// If used for editor feature display, show all features.
        /// </summary>
        Advanced
    }

    public enum WaveformType
    {
        Legacy,
        Beta,
        LegacyFast,
        BetaFast
    }

    public enum VerticalDirection
    {
        Up,
        Down
    }

    public enum HorizontalDirection
    {
        Left,
        Right
    }

    public enum Easings
    {
        Linear,
        Instant,
        InSine,
        OutSine,
        InOutSine,
        InElastic,
        OutElastic,
        InOutElastic,
        InBack,
        OutBack,
        InOutBack,
        InBounce,
        OutBounce,
        InOutBounce,
        InQuad,
        OutQuad,
        InOutQuad,
        InCirc,
        OutCirc,
        InOutCirc,
        InExpo,
        OutExpo,
        InOutExpo
    }

    public enum PrefabDialog
    {
        Internal,
        External
    }

    public enum ShakeType
    {
        Original,
        Catalyst
    }

    public enum ValueType
    {
        Bool,
        Int,
        Float,
        IntSlider,
        FloatSlider,
        String,
        Vector2,
        Vector2Int,
        Vector3,
        Vector3Int,
        Enum,
        Color,
        Function
    }

    /// <summary>
    /// Theem group used for applying Editor Themes.
    /// </summary>
    public enum ThemeGroup
    {
        /// <summary>
        /// If no theme color should be applied to the element. Used for cases where we want the element to be rounded but not take from a theme color.
        /// </summary>
        Null,

        Background_1,
        Background_2,
        Background_3,
        Preview_Cover,
        Scrollbar_1_Handle,
        Scrollbar_1_Handle_Normal,
        Scrollbar_1_Handle_Highlighted,
        Scrollbar_1_Handle_Selected,
        Scrollbar_1_Handle_Pressed,
        Scrollbar_1_Handle_Disabled,

        Scrollbar_2,
        Scrollbar_2_Handle,
        Scrollbar_2_Handle_Normal,
        Scrollbar_2_Handle_Highlighted,
        Scrollbar_2_Handle_Selected,
        Scrollbar_2_Handle_Pressed,
        Scrollbar_2_Handle_Disabled,

        Close,
        Close_Normal,
        Close_Highlighted,
        Close_Selected,
        Close_Pressed,
        Close_Disabled,
        Close_X,

        Picker,
        Picker_Normal,
        Picker_Highlighted,
        Picker_Selected,
        Picker_Pressed,
        Picker_Disabled,
        Picker_Icon,

        Light_Text,
        Dark_Text,

        Function_1,
        Function_1_Text,
        Function_2,
        Function_2_Normal,
        Function_2_Highlighted,
        Function_2_Selected,
        Function_2_Pressed,
        Function_2_Disabled,
        Function_2_Text,
        Function_3,
        Function_3_Text,

        List_Button_1,
        List_Button_1_Normal,
        List_Button_1_Highlighted,
        List_Button_1_Selected,
        List_Button_1_Pressed,
        List_Button_1_Disabled,
        List_Button_2,
        List_Button_2_Normal,
        List_Button_2_Highlighted,
        List_Button_2_Selected,
        List_Button_2_Pressed,
        List_Button_2_Disabled,
        List_Button_2_Text,

        Back_Button,
        Back_Button_Text,
        Folder_Button,
        Folder_Button_Text,
        File_Button,
        File_Button_Text,

        Search_Field_1,
        Search_Field_1_Text,
        Search_Field_2,
        Search_Field_2_Text,
        Add,
        Add_Text,
        Delete,
        Delete_Text,
        Delete_Keyframe_BG,
        Delete_Keyframe_Button,
        Delete_Keyframe_Button_Normal,
        Delete_Keyframe_Button_Highlighted,
        Delete_Keyframe_Button_Selected,
        Delete_Keyframe_Button_Pressed,
        Delete_Keyframe_Button_Disabled,

        Prefab,
        Prefab_Text,
        Object,
        Object_Text,
        Marker,
        Marker_Text,
        Checkpoint,
        Checkpoint_Text,
        Background_Object,
        Background_Object_Text,
        Timeline_Bar,
        Event_Check,
        Event_Check_Text,

        Dropdown_1,
        Dropdown_1_Overlay,
        Dropdown_1_Item,
        Toggle_1,
        Toggle_1_Check,
        Input_Field,
        Input_Field_Text,
        Slider_1,
        Slider_1_Normal,
        Slider_1_Highlighted,
        Slider_1_Selected,
        Slider_1_Pressed,
        Slider_1_Disabled,
        Slider_1_Handle,

        Slider_2,
        Slider_2_Handle,

        Documentation,

        Timeline_Background,
        Timeline_Scrollbar,
        Timeline_Scrollbar_Normal,
        Timeline_Scrollbar_Highlighted,
        Timeline_Scrollbar_Selected,
        Timeline_Scrollbar_Pressed,
        Timeline_Scrollbar_Disabled,
        Timeline_Scrollbar_Base,
        Timeline_Time_Scrollbar,

        Title_Bar_Text,
        Title_Bar_Button,
        Title_Bar_Button_Normal,
        Title_Bar_Button_Highlighted,
        Title_Bar_Button_Selected,
        Title_Bar_Button_Pressed,
        Title_Bar_Button_Disabled,
        Title_Bar_Dropdown,
        Title_Bar_Dropdown_Normal,
        Title_Bar_Dropdown_Highlighted,
        Title_Bar_Dropdown_Selected,
        Title_Bar_Dropdown_Pressed,
        Title_Bar_Dropdown_Disabled,

        Warning_Confirm,
        Warning_Cancel,

        Notification_Background,
        Notification_Info,
        Notification_Success,
        Notification_Error,
        Notification_Warning,

        Copy,
        Copy_Text,
        Paste,
        Paste_Text,

        Tab_Color_1,
        Tab_Color_1_Normal,
        Tab_Color_1_Highlighted,
        Tab_Color_1_Selected,
        Tab_Color_1_Pressed,
        Tab_Color_1_Disabled,
        Tab_Color_2,
        Tab_Color_2_Normal,
        Tab_Color_2_Highlighted,
        Tab_Color_2_Selected,
        Tab_Color_2_Pressed,
        Tab_Color_2_Disabled,
        Tab_Color_3,
        Tab_Color_3_Normal,
        Tab_Color_3_Highlighted,
        Tab_Color_3_Selected,
        Tab_Color_3_Pressed,
        Tab_Color_3_Disabled,
        Tab_Color_4,
        Tab_Color_4_Normal,
        Tab_Color_4_Highlighted,
        Tab_Color_4_Selected,
        Tab_Color_4_Pressed,
        Tab_Color_4_Disabled,
        Tab_Color_5,
        Tab_Color_5_Normal,
        Tab_Color_5_Highlighted,
        Tab_Color_5_Selected,
        Tab_Color_5_Pressed,
        Tab_Color_5_Disabled,
        Tab_Color_6,
        Tab_Color_6_Normal,
        Tab_Color_6_Highlighted,
        Tab_Color_6_Selected,
        Tab_Color_6_Pressed,
        Tab_Color_6_Disabled,
        Tab_Color_7,
        Tab_Color_7_Normal,
        Tab_Color_7_Highlighted,
        Tab_Color_7_Selected,
        Tab_Color_7_Pressed,
        Tab_Color_7_Disabled,

        Event_Color_1,
        Event_Color_2,
        Event_Color_3,
        Event_Color_4,
        Event_Color_5,
        Event_Color_6,
        Event_Color_7,
        Event_Color_8,
        Event_Color_9,
        Event_Color_10,
        Event_Color_11,
        Event_Color_12,
        Event_Color_13,
        Event_Color_14,
        Event_Color_15,

        Event_Color_1_Keyframe,
        Event_Color_2_Keyframe,
        Event_Color_3_Keyframe,
        Event_Color_4_Keyframe,
        Event_Color_5_Keyframe,
        Event_Color_6_Keyframe,
        Event_Color_7_Keyframe,
        Event_Color_8_Keyframe,
        Event_Color_9_Keyframe,
        Event_Color_10_Keyframe,
        Event_Color_11_Keyframe,
        Event_Color_12_Keyframe,
        Event_Color_13_Keyframe,
        Event_Color_14_Keyframe,
        Event_Color_15_Keyframe,

        Event_Color_1_Editor,
        Event_Color_2_Editor,
        Event_Color_3_Editor,
        Event_Color_4_Editor,
        Event_Color_5_Editor,
        Event_Color_6_Editor,
        Event_Color_7_Editor,
        Event_Color_8_Editor,
        Event_Color_9_Editor,
        Event_Color_10_Editor,
        Event_Color_11_Editor,
        Event_Color_12_Editor,
        Event_Color_13_Editor,
        Event_Color_14_Editor,
        Event_Color_15_Editor,

        Object_Keyframe_Color_1,
        Object_Keyframe_Color_2,
        Object_Keyframe_Color_3,
        Object_Keyframe_Color_4,
    }

    public enum Language
    {
        English,
        Spanish,
        Japanese,
        Thai,
        Russian,
        Pirate,
        French,
        Dutch,
        German,
        Chinese,
        AncientAutobot,
        Matoran
    }

    public enum DifficultyMode
    {
        ZenMode,
        Normal,
        OneLife,
        OneHit,
        Practice
    }

    public enum GameMode
    {
        Regular,
        Platformer
    }

    public enum Resolutions
    {
        p270,
        p360,
        p540,
        p720,
        p768,
        p810,
        p900,
        p1080,
        p1440,
        p2160
    }

    public enum FileType
    {
        LS,
        VG
    }

    public enum AxisMode
    {
        Both,
        XOnly,
        YOnly,
    }
}
