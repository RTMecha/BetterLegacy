namespace BetterLegacy
{

    /// <summary>
    /// File formats enum.
    /// </summary>
    public enum FileFormat
    {
        #region Main

        NULL,

        OGG,
        WAV,
        MP3,

        PNG,
        JPG,

        MP4,
        MOV,

        ZIP,

        JSON,
        TXT,
        CS,
        /// <summary>
        /// System Error Profile file format.
        /// </summary>
        SEP,
        /// <summary>
        /// System Error Save file format.
        /// </summary>
        SES,
        /// <summary>
        /// Unity compiled asset file format.
        /// </summary>
        ASSET,

        DLL,
        EXE,

        #endregion

        #region LS

        /// <summary>
        /// Legacy Level / metadata file format.
        /// </summary>
        LSB,
        /// <summary>
        /// Legacy Theme file format.
        /// </summary>
        LST,
        /// <summary>
        /// Legacy Prefab file format.
        /// </summary>
        LSP,
        /// <summary>
        /// Legacy Config file format.
        /// </summary>
        LSC,
        /// <summary>
        /// Legacy Level Collection file format.
        /// </summary>
        LSCO,
        /// <summary>
        /// Legacy Editor file format.
        /// </summary>
        LSE,
        /// <summary>
        /// Legacy Player model file format.
        /// </summary>
        LSPL,
        /// <summary>
        /// Legacy Settings / saves file format.
        /// </summary>
        LSS,
        /// <summary>
        /// Legacy Interface file format.
        /// </summary>
        LSI,
        /// <summary>
        /// Legacy Old interface file format.
        /// </summary>
        LSM,
        /// <summary>
        /// Legacy Prefab Type file format.
        /// </summary>
        LSPT,
        /// <summary>
        /// Legacy Shape file format.
        /// </summary>
        LSSH,
        /// <summary>
        /// Legacy Project Planner file format.
        /// </summary>
        LSN,
        /// <summary>
        /// Legacy QuickElement file format.
        /// </summary>
        LSQE,
        /// <summary>
        /// Legacy Level file list.
        /// </summary>
        LSF,

        #endregion

        #region VG

        /// <summary>
        /// Alpha Level file format.
        /// </summary>
        VGD,
        /// <summary>
        /// Alpha Metadata file format.
        /// </summary>
        VGM,
        /// <summary>
        /// Alpha Theme file format.
        /// </summary>
        VGT,
        /// <summary>
        /// Alpha Prefab file format.
        /// </summary>
        VGP,
        /// <summary>
        /// Alpha Settings / saves file format.
        /// </summary>
        VGS,

        #endregion
    }

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
        Arcade_Select
    }

    public enum LoadingDisplayType
    {
        Doggo,
        Waveform,
        Bar,
        Percentage,
        EqualsBar
    }

    /// <summary>
    /// Used for obtaining a specific yield instruction.
    /// </summary>
    public enum YieldType
    {
        /// <summary>
        /// Returns: null
        /// </summary>
        None,
        /// <summary>
        /// Returns: new WaitForSeconds(delay)
        /// </summary>
        Delay,
        /// <summary>
        /// Returns: null
        /// </summary>
        Null,
        /// <summary>
        /// Returns: new WaitForEndOfFrame()
        /// </summary>
        EndOfFrame,
        /// <summary>
        /// Returns: new WaitForFixedUpdate()
        /// </summary>
        FixedUpdate,
    }
    
    public enum Rank
    {
        /// <summary>
        /// Does not set a rank.
        /// </summary>
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
        Ranking,
    }

    public enum MenuMusicLoadMode
    {
        /// <summary>
        /// Takes from Project Arrhythmia/settings/menus
        /// </summary>
        Settings,
        /// <summary>
        /// Takes from the story folder.
        /// </summary>
        ArcadeFolder,
        /// <summary>
        /// Takes from the story folder.
        /// </summary>
        StoryFolder,
        /// <summary>
        /// Takes from the editor folder.
        /// </summary>
        EditorFolder,
        /// <summary>
        /// Takes from the interfaces/music folder.
        /// </summary>
        InterfacesFolder,
        /// <summary>
        /// Takes from the global folder.
        /// </summary>
        GlobalFolder,
    }

    public enum ServerVisibility
    {
        Public,
        Unlisted,
        Private
    }

    public enum UserPreferenceType
    {
        None,
        Beginner,
        Legacy,
        Alpha,
        Modded
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

    /// <summary>
    /// The type of Waveform the editor timeline should render.
    /// </summary>
    public enum WaveformType
    {
        /// <summary>
        /// The vanilla rendering type.
        /// </summary>
        Legacy,
        /// <summary>
        /// Old rendering type.
        /// </summary>
        Beta,
        /// <summary>
        /// New rendering type based on alpha waveform.
        /// </summary>
        Modern,
        LegacyFast,
        BetaFast,
        ModernFast
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

    /// <summary>
    /// PA Easings.
    /// </summary>
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

    /// <summary>
    /// Where a Prefab comes from.
    /// </summary>
    public enum PrefabDialog
    {
        /// <summary>
        /// Prefab comes from internal level file.
        /// </summary>
        Internal,
        /// <summary>
        /// Prefab comes from external prefabs folder.
        /// </summary>
        External
    }

    /// <summary>
    /// The type of shake to be used when playing a level.
    /// </summary>
    public enum ShakeType
    {
        /// <summary>
        /// The original Legacy shake behavior.
        /// </summary>
        Original,
        /// <summary>
        /// Shake behavior based on Catalyst. Allows for the extra shake event values.
        /// </summary>
        Catalyst
    }

    /// <summary>
    /// What type of value an object is.
    /// </summary>
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

    /// <summary>
    /// The challenge mode the user wants to play a level with.
    /// </summary>
    public enum ChallengeMode
    {
        /// <summary>
        /// No damage is taken.
        /// </summary>
        ZenMode,
        /// <summary>
        /// Damage is taken.
        /// </summary>
        Normal,
        /// <summary>
        /// Player restarts the level when dead.
        /// </summary>
        OneLife,
        /// <summary>
        /// Player restarts the level when hit.
        /// </summary>
        OneHit,
        /// <summary>
        /// Damage is taken, but health is not subtracted so the Player will not die.
        /// </summary>
        Practice
    }

    /// <summary>
    /// The GameMode type of a level.
    /// </summary>
    public enum GameMode
    {
        /// <summary>
        /// The normal top-down movement.
        /// </summary>
        Regular,
        /// <summary>
        /// Left/right movement with jumping affected by gravity.
        /// </summary>
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

    /// <summary>
    /// Which era of Project Arrhythmia a file is from.
    /// </summary>
    public enum ArrhythmiaType
    {
        /// <summary>
        /// Unknown format.
        /// </summary>
        NULL,
        /// <summary>
        /// Lime Studios / Lilscript format used in old dev and Legacy versions.
        /// </summary>
        LS,
        /// <summary>
        /// Vitamin Games format used in the modern versions.
        /// </summary>
        VG
    }

    public enum AxisMode
    {
        Both,
        XOnly,
        YOnly,
    }

    /// <summary>
    /// Used for referencing InputControlType without the number offsets.
    /// </summary>
    public enum PlayerInputControlType
    {
        None,
        LeftStickUp,
        LeftStickDown,
        LeftStickLeft,
        LeftStickRight,
        LeftStickButton,
        RightStickUp,
        RightStickDown,
        RightStickLeft,
        RightStickRight,
        RightStickButton,
        DPadUp,
        DPadDown,
        DPadLeft,
        DPadRight,
        LeftTrigger,
        RightTrigger,
        LeftBumper,
        RightBumper,
        Action1,
        Action2,
        Action3,
        Action4,
        Action5,
        Action6,
        Action7,
        Action8,
        Action9,
        Action10,
        Action11,
        Action12,
        Back,
        Start,
        Select,
        System,
        Options,
        Pause,
        Menu,
        Share,
        Home,
        View,
        Power,
        Capture,
        Plus,
        Minus,
        PedalLeft,
        PedalRight,
        PedalMiddle,
        GearUp,
        GearDown,
        Pitch,
        Roll,
        Yaw,
        ThrottleUp,
        ThrottleDown,
        ThrottleLeft,
        ThrottleRight,
        POVUp,
        POVDown,
        POVLeft,
        POVRight,
        TiltX,
        TiltY,
        TiltZ,
        ScrollWheel,
        TouchPadTap,
        TouchPadButton,
        TouchPadXAxis,
        TouchPadYAxis,
        LeftSL,
        LeftSR,
        RightSL,
        RightSR,
        Command,
        LeftStickX,
        LeftStickY,
        RightStickX,
        RightStickY,
        DPadX,
        DPadY,
        Analog0,
        Analog1,
        Analog2,
        Analog3,
        Analog4,
        Analog5,
        Analog6,
        Analog7,
        Analog8,
        Analog9,
        Analog10,
        Analog11,
        Analog12,
        Analog13,
        Analog14,
        Analog15,
        Analog16,
        Analog17,
        Analog18,
        Analog19,
        Button0,
        Button1,
        Button2,
        Button3,
        Button4,
        Button5,
        Button6,
        Button7,
        Button8,
        Button9,
        Button10,
        Button11,
        Button12,
        Button13,
        Button14,
        Button15,
        Button16,
        Button17,
        Button18,
        Button19,
        Count
    }

    public enum DefaultMusic
    {
        loading,
        menu,
        barrels,
        nostalgia,
        arcade_dream,
        distance,
        truepianoskg,
        dread,
        in_the_distance,
        io,
        jukio_distance
    }

    public enum DefaultSounds
    {
        UpDown,
        LeftRight,
        Block,
        Select,
        Click,

        rewind,
        record_scratch,
        checkpoint,
        boost,
        boost_recover,
        shoot,
        pirate_KillPlayer,

        KillPlayer,
        SpawnPlayer,
        HealPlayer,
        HurtPlayer,
        HurtPlayer2,
        HurtPlayer3,

        glitch,
        menuflip,

        blip,
        loadsound,

        example_speak,
        hal_speak,
        anna_speak,
        para_speak,
        t_speak,
    }
}
