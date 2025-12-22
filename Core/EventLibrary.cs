using System.Collections.Generic;

using BetterLegacy.Core.Data.Beatmap;

namespace BetterLegacy.Core
{
    /// <summary>
    /// Library for level events.
    /// </summary>
    public static class EventLibrary
    {
        /// <summary>
        /// The display names of all the events in BetterLegacy.
        /// </summary>
        public static string[] displayNames = new string[]
        {
            #region Vanilla

            "Move",
            "Zoom",
            "Rotate",
            "Shake",
            "Theme",
            "Chroma",
            "Bloom",
            "Vignette",
            "Lens",
            "Grain",

            #endregion

            #region Modded

            "Color Grading",
            "Ripples",
            "Radial Blur",
            "Color Split",
            "Offset",
            "Gradient",
            "Double Vision",
            "Scan Lines",
            "Blur",
            "Pixelize",
            "BG",
            "Invert",
            "Timeline",
            "Player",
            "Follow Player",
            "Audio",
            "Video BG Parent",
            "Video BG",
            "Sharpen",
            "Bars",
            "Danger",
            "3D Rotation",
            "Camera Depth",
            "Window Base",
            "Window Position X",
            "Window Position Y",
            "Player Force",
            "Mosaic",
            "Analog Glitch",
            "Digital Glitch",
            "Shockwave",

            #endregion
        };

        /// <summary>
        /// The JSON names of all the events in BetterLegacy.
        /// </summary>
        public static string[] jsonNames = new string[]
        {
            #region Vanilla

            "pos", // 0
			"zoom", // 1
			"rot", // 2
			"shake", // 3
			"theme", // 4
			"chroma", // 5
			"bloom", // 6
			"vignette", // 7
			"lens", // 8
			"grain", // 9

            #endregion

            #region Modded

            "cg", // 10
			"rip", // 11
			"rb", // 12
			"cs", // 13
			"offset", // 14
			"grd", // 15
			"dbv", // 16
			"scan", // 17
			"blur", // 18
			"pixel", // 19
			"bg", // 20
			"invert", // 21
			"timeline", // 22
			"player", // 23
			"follow_player", // 24
			"audio", // 25
			"vidbg_p", // 26
			"vidbg", // 27
			"sharp", // 28
			"bars", // 29
			"danger", // 30
			"xyrot", // 31
			"camdepth", // 32
			"winbase", // 33
			"winposx", // 34
			"winposy", // 35
			"playerforce", // 36
			"mosaic", // 37
			"analog_glitch", // 38
			"digital_glitch", // 39
            "shockwave", // 40

            #endregion
        };

        /// <summary>
        /// The vanilla event keyframe value counts.
        /// </summary>
        public static int[] vanillaLegacyValueCounts = new int[]
        {
            2, // Move
            1, // Zoom
            1, // Rotate
            1, // Shake
            1, // Theme
            1, // Chroma
            1, // Bloom
            6, // Vignette
            1, // Lens
            3, // Grain
        };

        /// <summary>
        /// Total amount of event keyframe types in BetterLegacy.
        /// </summary>
        public static int Count => cachedDefaultKeyframes.Count;

        public const int EVENT_COUNT = 41;

        /// <summary>
        /// The default event keyframes in BetterLegacy.
        /// </summary>
        public static List<EventKeyframe> cachedDefaultKeyframes = GetDefaultKeyframes();

        /// <summary>
        /// The default event keyframes in BetterLegacy.
        /// </summary>
        public static List<EventKeyframe> GetDefaultKeyframes() => new List<EventKeyframe>
        {
            #region Vanilla

            // layer 1
            /*[ 0 ]*/ DefaultKeyframes.Move,
			/*[ 1 ]*/ DefaultKeyframes.Zoom,
			/*[ 2 ]*/ DefaultKeyframes.Rotate,
			/*[ 3 ]*/ DefaultKeyframes.Shake,
			/*[ 4 ]*/ DefaultKeyframes.Theme,
			/*[ 5 ]*/ DefaultKeyframes.Chroma,
			/*[ 6 ]*/ DefaultKeyframes.Bloom,
			/*[ 7 ]*/ DefaultKeyframes.Vignette,
			/*[ 8 ]*/ DefaultKeyframes.Lens,
			/*[ 9 ]*/ DefaultKeyframes.Grain,

            #endregion

            #region Modded

            /*[ 10 ]*/ DefaultKeyframes.ColorGrading,
			/*[ 11 ]*/ DefaultKeyframes.Ripples,
			/*[ 12 ]*/ DefaultKeyframes.RadialBlur,
			/*[ 13 ]*/ DefaultKeyframes.ColorSplit,

            // layer 2
			/*[ 14 ]*/ DefaultKeyframes.MoveOffset,
			/*[ 15 ]*/ DefaultKeyframes.Gradient,
			/*[ 16 ]*/ DefaultKeyframes.DoubleVision,
			/*[ 17 ]*/ DefaultKeyframes.ScanLines,
			/*[ 18 ]*/ DefaultKeyframes.Blur,
			/*[ 19 ]*/ DefaultKeyframes.Pixelize,
			/*[ 20 ]*/ DefaultKeyframes.BG,
			/*[ 21 ]*/ DefaultKeyframes.Invert,
			/*[ 22 ]*/ DefaultKeyframes.Timeline,
			/*[ 23 ]*/ DefaultKeyframes.Player,
			/*[ 24 ]*/ DefaultKeyframes.FollowPlayer,
			/*[ 25 ]*/ DefaultKeyframes.Audio,
			/*[ 26 ]*/ DefaultKeyframes.VideoParent,
			/*[ 27 ]*/ DefaultKeyframes.Video,

            // layer 3
			/*[ 28 ]*/ DefaultKeyframes.Sharpen,
			/*[ 29 ]*/ DefaultKeyframes.Bars,
			/*[ 30 ]*/ DefaultKeyframes.Danger,
			/*[ 31 ]*/ DefaultKeyframes.DepthRotation,
			/*[ 32 ]*/ DefaultKeyframes.CameraDepth,
			/*[ 33 ]*/ DefaultKeyframes.WindowBase,
			/*[ 34 ]*/ DefaultKeyframes.WindowPositionX,
			/*[ 35 ]*/ DefaultKeyframes.WindowPositionY,
			/*[ 36 ]*/ DefaultKeyframes.PlayerForce,
			/*[ 37 ]*/ DefaultKeyframes.Mosaic,
			/*[ 38 ]*/ DefaultKeyframes.AnalogGlitch,
			/*[ 39 ]*/ DefaultKeyframes.DigitalGlitch,
			/*[ 40 ]*/ DefaultKeyframes.Shockwave,

            #endregion
        };

        /// <summary>
        /// Library of default event keyframes.
        /// </summary>
        public static class DefaultKeyframes
        {
            #region Vanilla

            /// <summary>
            /// <see cref="Indexes.MOVE"/>
            /// </summary>
            public static EventKeyframe Move => new EventKeyframe
            {
                time = 0f,
                values = new float[2],
            };

            /// <summary>
            /// <see cref="Indexes.ZOOM"/>
            /// </summary>
            public static EventKeyframe Zoom => new EventKeyframe
            {
                time = 0f,
                values = new float[1] { 20f },
            };

            /// <summary>
            /// <see cref="Indexes.ROTATE"/>
            /// </summary>
            public static EventKeyframe Rotate => new EventKeyframe
            {
                time = 0f,
                values = new float[1],
            };

            /// <summary>
            /// <see cref="Indexes.SHAKE"/>
            /// </summary>
            public static EventKeyframe Shake => new EventKeyframe
            {
                time = 0f,
                values = new float[5]
                {
                    0f, // Shake Intensity
					1f, // Shake X
					1f, // Shake Y
					0f, // Shake Interpolation
					1f, // Shake Speed
                },
            };

            /// <summary>
            /// <see cref="Indexes.THEME"/>
            /// </summary>
            public static EventKeyframe Theme => new EventKeyframe
            {
                time = 0f,
                values = new float[1],
            };

            /// <summary>
            /// <see cref="Indexes.CHROMA"/>
            /// </summary>
            public static EventKeyframe Chroma => new EventKeyframe
            {
                time = 0f,
                values = new float[1],
            };

            /// <summary>
            /// <see cref="Indexes.BLOOM"/>
            /// </summary>
            public static EventKeyframe Bloom => new EventKeyframe
            {
                time = 0f,
                values = new float[8]
                {
                    0f, // Bloom Intensity
					7f, // Bloom Diffusion
					1f, // Bloom Threshold
					0f, // Bloom Anamorphic Ratio
					18f, // Bloom Color
					0f, // Bloom Hue
					0f, // Bloom Sat
					0f, // Bloom Val
				},
            };

            /// <summary>
            /// <see cref="Indexes.VIGNETTE"/>
            /// </summary>
            public static EventKeyframe Vignette => new EventKeyframe
            {
                time = 0f,
                values = new float[10]
                {
                    0f, // Vignette Intensity
					0f, // Vignette Smoothness
					0f, // Vignette Rounded
					0f, // Vignette Roundness
					0f, // Vignette Center X
					0f, // Vignette Center Y
					18f, // Vignette Color
					0f, // Vignette Hue
					0f, // Vignette Sat
					0f, // Vignette Val
                },
            };

            /// <summary>
            /// <see cref="Indexes.LENS"/>
            /// </summary>
            public static EventKeyframe Lens => new EventKeyframe
            {
                time = 0f,
                values = new float[6]
                {
                    0f,
                    0f,
                    0f,
                    1f,
                    1f,
                    1f
                },
            };

            /// <summary>
            /// <see cref="Indexes.GRAIN"/>
            /// </summary>
            public static EventKeyframe Grain => new EventKeyframe
            {
                time = 0f,
                values = new float[3],
            };

            #endregion

            #region Modded

            /// <summary>
            /// <see cref="Indexes.COLORGRADING"/>
            /// </summary>
            public static EventKeyframe ColorGrading => new EventKeyframe
            {
                time = 0f,
                values = new float[9],
            };

            /// <summary>
            /// <see cref="Indexes.RIPPLES"/>
            /// </summary>
            public static EventKeyframe Ripples => new EventKeyframe
            {
                time = 0f,
                values = new float[6]
                {
                    0f,
                    0f,
                    1f,
                    0f,
                    0f,
                    0f,
                },
            };

            /// <summary>
            /// <see cref="Indexes.RADIALBLUR"/>
            /// </summary>
            public static EventKeyframe RadialBlur => new EventKeyframe
            {
                time = 0f,
                values = new float[2]
                {
                    0f,
                    6f
                },
            };

            /// <summary>
            /// <see cref="Indexes.COLORSPLIT"/>
            /// </summary>
            public static EventKeyframe ColorSplit => new EventKeyframe
            {
                time = 0f,
                values = new float[2],
            };

            /// <summary>
            /// <see cref="Indexes.MOVE_OFFSET"/>
            /// </summary>
            public static EventKeyframe MoveOffset => new EventKeyframe
            {
                time = 0f,
                values = new float[2],
            };

            /// <summary>
            /// <see cref="Indexes.GRADIENT"/>
            /// </summary>
            public static EventKeyframe Gradient => new EventKeyframe
            {
                time = 0f,
                values = new float[13]
                {
                    0f,
                    0f,
                    18f,
                    18f,
                    0f,
                    1f, // Top Opacity
					0f, // Top Hue
					0f, // Top Sat
					0f, // Top Val
					1f, // Bottom Opacity
					0f, // Bottom Hue
					0f, // Bottom Sat
					0f, // Bottom Val
				},
            };

            /// <summary>
            /// <see cref="Indexes.DOUBLEVISION"/>
            /// </summary>
            public static EventKeyframe DoubleVision => new EventKeyframe
            {
                time = 0f,
                values = new float[2],
            };

            /// <summary>
            /// <see cref="Indexes.SCANLINES"/>
            /// </summary>
            public static EventKeyframe ScanLines => new EventKeyframe
            {
                time = 0f,
                values = new float[3],
            };

            /// <summary>
            /// <see cref="Indexes.BLUR"/>
            /// </summary>
            public static EventKeyframe Blur => new EventKeyframe
            {
                time = 0f,
                values = new float[2]
                {
                    0f,
                    6f
                },
            };

            /// <summary>
            /// <see cref="Indexes.PIXELIZE"/>
            /// </summary>
            public static EventKeyframe Pixelize => new EventKeyframe
            {
                time = 0f,
                values = new float[1],
            };

            /// <summary>
            /// <see cref="Indexes.BG"/>
            /// </summary>
            public static EventKeyframe BG => new EventKeyframe
            {
                time = 0f,
                values = new float[5]
                {
                    18f, // Color
					0f, // Active
					0f, // Hue
					0f, // Sat
					0f, // Val
				},
            };

            /// <summary>
            /// <see cref="Indexes.INVERT"/>
            /// </summary>
            public static EventKeyframe Invert => new EventKeyframe
            {
                time = 0f,
                values = new float[1],
            };

            /// <summary>
            /// <see cref="Indexes.TIMELINE"/>
            /// </summary>
            public static EventKeyframe Timeline => new EventKeyframe
            {
                time = 0f,
                values = new float[11]
                {
                    0f,
                    0f,
                    -342f,
                    1f,
                    1f,
                    0f,
                    18f,
                    1f, // Opacity
					0f, // Hue
					0f, // Sat
					0f, // Val
				},
            };

            /// <summary>
            /// <see cref="Indexes.PLAYER"/>
            /// </summary>
            public static EventKeyframe Player => new EventKeyframe
            {
                time = 0f,
                values = new float[6],
            };

            /// <summary>
            /// <see cref="Indexes.FOLLOW_PLAYER"/>
            /// </summary>
            public static EventKeyframe FollowPlayer => new EventKeyframe
            {
                time = 0f,
                values = new float[10]
                {
                    0f, // Active
					0f, // Move
					0f, // Rotate
					0.5f,
                    0f,
                    9999f,
                    -9999f,
                    9999f,
                    -9999f,
                    1f,
                },
            };

            /// <summary>
            /// <see cref="Indexes.AUDIO"/>
            /// </summary>
            public static EventKeyframe Audio => new EventKeyframe
            {
                time = 0f,
                values = new float[3]
                {
                    1f,
                    1f,
                    0f
                },
            };

            /// <summary>
            /// <see cref="Indexes.VIDEO_PARENT"/>
            /// </summary>
            public static EventKeyframe VideoParent => new EventKeyframe
            {
                time = 0f,
                values = new float[9]
                {
                    0f, // Position X
                    0f, // Position Y
                    0f, // Position Z
                    1f, // Scale X
                    1f, // Scale Y
                    1f, // Scale Z
                    0f, // Rotation X
                    0f, // Rotation Y
                    0f, // Rotation Z
                },
            };

            /// <summary>
            /// <see cref="Indexes.VIDEO"/>
            /// </summary>
            public static EventKeyframe Video => new EventKeyframe
            {
                time = 0f,
                values = new float[10]
                {
                    0f, // Position X
                    0f, // Position Y
                    120f, // Position Z
                    240f, // Scale X
                    135f, // Scale Y
                    1f, // Scale Z
                    0f, // Rotation X
                    0f, // Rotation Y
                    0f, // Rotation Z
                    0f, // Render Layer (Foreground / Background)
                },
            };

            /// <summary>
            /// <see cref="Indexes.SHARPEN"/>
            /// </summary>
            public static EventKeyframe Sharpen => new EventKeyframe
            {
                time = 0f,
                values = new float[1]
                {
                    0f, // Sharpen Amount
                },
            };

            /// <summary>
            /// <see cref="Indexes.BARS"/>
            /// </summary>
            public static EventKeyframe Bars => new EventKeyframe
            {
                time = 0f,
                values = new float[2]
                {
                    0f, // Amount
					0f, // Mode
                },
            };

            /// <summary>
            /// <see cref="Indexes.DANGER"/>
            /// </summary>
            public static EventKeyframe Danger => new EventKeyframe
            {
                time = 0f,
                values = new float[7]
                {
                    0f, // Intensity
					0f, // Size
					18f, // Color
					1f, // Opacity
					0f, // Hue
					0f, // Sat
					0f, // Val
                },
            };

            /// <summary>
            /// <see cref="Indexes.DEPTH_ROTATION"/>
            /// </summary>
            public static EventKeyframe DepthRotation => new EventKeyframe
            {
                time = 0f,
                values = new float[2]
                {
                    0f, // X
					0f, // Y
                },
            };

            /// <summary>
            /// <see cref="Indexes.CAMERA_DEPTH"/>
            /// </summary>
            public static EventKeyframe CameraDepth => new EventKeyframe
            {
                time = 0f,
                values = new float[4]
                {
                    -10f, // Depth
					0f, // Zoom
					0f, // Global Position
					1f, // Near Clip Plane Align
                },
            };

            /// <summary>
            /// <see cref="Indexes.WINDOW_BASE"/>
            /// </summary>
            public static EventKeyframe WindowBase => new EventKeyframe
            {
                time = 0f,
                values = new float[4]
                {
                    0f, // Force Resolution (1 = true, includes position)
					1280f, // X
					720f, // Y
					0f, // Allow Position
                },
            };

            /// <summary>
            /// <see cref="Indexes.WINDOW_POSITION_X"/>
            /// </summary>
            public static EventKeyframe WindowPositionX => new EventKeyframe
            {
                time = 0f,
                values = new float[1]
                {
                    0f, // Position X
                },
            };

            /// <summary>
            /// <see cref="Indexes.WINDOW_POSITION_Y"/>
            /// </summary>
            public static EventKeyframe WindowPositionY => new EventKeyframe
            {
                time = 0f,
                values = new float[1]
                {
                    0f, // Position Y
                },
            };

            /// <summary>
            /// <see cref="Indexes.PLAYER_FORCE"/>
            /// </summary>
            public static EventKeyframe PlayerForce => new EventKeyframe
            {
                time = 0f,
                values = new float[2]
                {
                    0f, // Player Force X
					0f, // Player Force Y
                },
            };

            /// <summary>
            /// <see cref="Indexes.MOSAIC"/>
            /// </summary>
            public static EventKeyframe Mosaic => new EventKeyframe
            {
                time = 0f,
                values = new float[1]
                {
                    0f, // Intensity
                },
            };

            /// <summary>
            /// <see cref="Indexes.ANALOG_GLITCH"/>
            /// </summary>
            public static EventKeyframe AnalogGlitch => new EventKeyframe
            {
                time = 0f,
                values = new float[5]
                {
                    0f, // Enabled
                    0f, // ColorDrift
                    0f, // HorizontalShake
                    0f, // ScanLineJitter
                    0f, // VerticalJump
                },
            };

            /// <summary>
            /// <see cref="Indexes.DIGITAL_GLITCH"/>
            /// </summary>
            public static EventKeyframe DigitalGlitch => new EventKeyframe
            {
                time = 0f,
                values = new float[1]
                {
                    0f, // Intensity
                },
            };
            /// <summary>
            /// <see cref="Indexes.SHOCKWAVE"/>
            /// </summary>
            public static EventKeyframe Shockwave => new EventKeyframe
            {
                time = 0f,
                values = new float[]
                {
                    0f, // Intensity
                    20f, // Ring
                    0f, // Center X
                    0f, // Center Y
                    1f, // Scale X
                    1f, // Scale Y
                    0f, // Rotation
                    0f, // Warp
                    0f, // Elapsed
                },
            };

            #endregion
        }

        /// <summary>
        /// Library of event type indexes.
        /// </summary>
        public static class Indexes
        {
            #region Vanilla

            // layer 1

            public const int MOVE = 0;
            public const int ZOOM = 1;
            public const int ROTATE = 2;
            public const int SHAKE = 3;
            public const int THEME = 4;
            public const int CHROMA = 5;
            public const int BLOOM = 6;
            public const int VIGNETTE = 7;
            public const int LENS = 8;
            public const int GRAIN = 9;

            #endregion

            #region Modded

            public const int COLORGRADING = 10;
            public const int RIPPLES = 11;
            public const int RADIALBLUR = 12;
            public const int COLORSPLIT = 13;

            // layer 2

            public const int MOVE_OFFSET = 14;
            public const int GRADIENT = 15;
            public const int DOUBLEVISION = 16;
            public const int SCANLINES = 17;
            public const int BLUR = 18;
            public const int PIXELIZE = 19;
            public const int BG = 20;
            public const int INVERT = 21;
            public const int TIMELINE = 22;
            public const int PLAYER = 23;
            public const int FOLLOW_PLAYER = 24;
            public const int AUDIO = 25;
            public const int VIDEO_PARENT = 26;
            public const int VIDEO = 27;

            // layer 3

            public const int SHARPEN = 28;
            public const int BARS = 29;
            public const int DANGER = 30;
            public const int DEPTH_ROTATION = 31;
            public const int CAMERA_DEPTH = 32;
            public const int WINDOW_BASE = 33;
            public const int WINDOW_POSITION_X = 34;
            public const int WINDOW_POSITION_Y = 35;
            public const int PLAYER_FORCE = 36;
            public const int MOSAIC = 37;
            public const int ANALOG_GLITCH = 38;
            public const int DIGITAL_GLITCH = 39;
            public const int SHOCKWAVE = 40;

            #endregion
        }
    }
}
