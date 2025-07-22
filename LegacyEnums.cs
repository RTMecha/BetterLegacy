// all main enums

namespace BetterLegacy
{
    #region Custom

    using System.Linq;
    using System.Runtime.CompilerServices;

    using UnityEngine;

    using BetterLegacy.Core;
    using BetterLegacy.Core.Data;

    /* INFO
     This file serves to test a custom enum system.
     Custom enums are built to act like Java's enums.
     However, in this case enums can be iterated via the ICustomEnum<T>.GetEnumValues() extension.
     Or if this class is utilized in pre-net9.0, then EnumHelper.GetValues<T>();

     The only thing that custom enums can't do compared to regular enums is the switch case statement. Example:
     switch (objectType)
     {
        case ObjectType.Normal: {
            break;
        }
        case ObjectType.Helper: {
            break;
        }
     }
     'case ObjectType.Normal' cannot be a class because it is not constant. This is the only issue, but can be resolved by doing:
     switch (objectType)
     {
        case 0: {
            break;
        }
        case 1: {
            break;
        }
     }
     However, there is a work-around.
     switch (objectType.Name)
     {
        case nameof(ObjectType.Normal): {
            break;
        }
        case nameof(ObjectType.Helper): {
            break;
        }
     }
     This gets around the issue and allows you to directly reference the enum name.

     Onto how custom enums should work.

     - Each 'Value' of a custom enum should be static and readonly, meaning they cannot be modified, just like actual enums.

     - The custom enum class must implement a static ENUM array, I.E: static ObjectType[] ENUMS = [NORMAL, HELPER, ...]

     - Inner values of an enum value should only be get. I.E: public float Opacity { get; }
     */

    /// <summary>
    /// Indicates an object can be classified as a custom enum.
    /// </summary>
    public interface ICustomEnum
    {
        /// <summary>
        /// Ordinal value of the enum value.
        /// </summary>
        public int Ordinal { get; }

        /// <summary>
        /// Name of the enum value.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The name to display in-game. Capable of multi-language.
        /// </summary>
        public Lang DisplayName { get; }

        /// <summary>
        /// Gets the amount of enum values.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Gets an array containing all enum values.
        /// </summary>
        /// <returns>Returns an array containing all enum values.</returns>
        public ICustomEnum[] GetBoxedValues();

        /// <summary>
        /// Gets an enum value at an index.
        /// </summary>
        /// <param name="index">Index of the enum value.</param>
        /// <returns>Returns the enum value at the index.</returns>
        public ICustomEnum GetBoxedValue(int index);

        /// <summary>
        /// Gets an enum value with a matching name.
        /// </summary>
        /// <param name="name">Name of the enum value.</param>
        /// <returns>Returns the enum value with a matching name.</returns>
        public ICustomEnum GetBoxedValue(string name);

        /// <summary>
        /// Checks if the index is in the range of enum values.
        /// </summary>
        /// <param name="index">Index of the value.</param>
        /// <returns>Returns true if the index is in the range of the values.</returns>
        public bool InRange(int index);
    }

    /// <summary>
    /// Indicates an object can be classified as a custom enum.
    /// </summary>
    /// <typeparam name="T">Type of the enum class.</typeparam>
    public interface ICustomEnum<T> : ICustomEnum
    {
        /// <summary>
        /// Gets an array containing all enum values.
        /// </summary>
        /// <returns>Returns an array containing all enum values.</returns>
        public T[] GetValues();

        /// <summary>
        /// Gets an enum value at an index.
        /// </summary>
        /// <param name="index">Index of the enum value.</param>
        /// <returns>Returns the enum value at the index.</returns>
        public T GetValue(int index);

        /// <summary>
        /// Overrides the custom enum values.
        /// </summary>
        /// <param name="values">Values to set.</param>
        public void SetValues(T[] values);
    }

    /// <summary>
    /// This class demonstrates a regular enum type, with just ordinal and name values per enum value.
    /// </summary>
    public class CustomEnum : Exists, ICustomEnum<CustomEnum>
    {
        public CustomEnum() { }

        public CustomEnum(int ordinal, string name)
        {
            Ordinal = ordinal;
            Name = name;
            DisplayName = name;
        }

        #region Enum Values

        public static readonly CustomEnum ONE = new(0, nameof(ONE));
        public static readonly CustomEnum TWO = new(1, nameof(TWO));

        static CustomEnum[] ENUMS = new CustomEnum[] { ONE, TWO };

        #endregion

        public int Ordinal { get; }
        public string Name { get; }
        public Lang DisplayName { get; }

        #region Implementation

        public int Count => ENUMS.Length;

        public CustomEnum[] GetValues() => ENUMS;

        public CustomEnum GetValue(int index) => ENUMS[index];

        public void SetValues(CustomEnum[] values) => ENUMS = values;

        public ICustomEnum[] GetBoxedValues() => ENUMS;

        public ICustomEnum GetBoxedValue(int index) => ENUMS[index];

        public ICustomEnum GetBoxedValue(string name) => ENUMS.First(x => x.Name == name);

        public bool InRange(int index) => ENUMS.InRange(index);

        #endregion

        #region Comparison

        public override bool Equals(object obj) => obj is CustomEnum other && Ordinal == other.Ordinal && Name == other.Name && DisplayName == other.DisplayName;

        public override int GetHashCode() => Core.Helpers.CoreHelper.CombineHashCodes(Ordinal, Name, DisplayName);

        public override string ToString() => $"Ordinal: {Ordinal} Name: {Name} Display Name: {DisplayName}";

        public static implicit operator int(CustomEnum value) => value.Ordinal;

        public static implicit operator string(CustomEnum value) => value.Name;

        public static implicit operator CustomEnum(int value) => CustomEnumHelper.GetValue<CustomEnum>(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(CustomEnum a, CustomEnum b) => a.Equals(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(CustomEnum a, CustomEnum b) => !(a == b);

        #endregion
    }

    /// <summary>
    /// Custom Enum helper class.
    /// </summary>
    public static class CustomEnumHelper
    {
        /// <summary>
        /// Gets all enum values from the <see cref="ICustomEnum{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type of the custom enum.</typeparam>
        /// <returns>Returns an array of enum values.</returns>
        public static T[] GetValues<T>() where T : ICustomEnum<T>, new() => new T().GetValues();

        /// <summary>
        /// Modifies the enum values of a custom enum.
        /// </summary>
        /// <typeparam name="T">Type of the custom enum.</typeparam>
        /// <param name="values">Values to set.</param>
        public static void SetValues<T>(T[] values) where T : ICustomEnum<T>, new() => new T().SetValues(values);

        /// <summary>
        /// Gets a specific enum value at an index from the <see cref="ICustomEnum{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type of the custom enum.</typeparam>
        /// <param name="index">Index to get at.</param>
        /// <returns>Returns the custom enum value at the index.</returns>
        public static T GetValue<T>(int index) where T : ICustomEnum<T>, new() => (T)GetValue(new T(), index);

        /// <summary>
        /// Gets a specific enum value at an index from the <see cref="ICustomEnum"/>.
        /// </summary>
        /// <typeparam name="T">Type of the custom enum.</typeparam>
        /// <param name="customEnum">Custom Enum.</param>
        /// <param name="index">Index to get at.</param>
        /// <returns>Returns the custom enum value at the index.</returns>
        public static ICustomEnum GetValue(ICustomEnum customEnum, int index)
        {
            var values = customEnum.GetBoxedValues();
            for (int i = 0; i < values.Length; i++)
                if (values[i].Ordinal == index)
                    return values[i];

            return default;
        }

        /// <summary>
        /// Gets a specific enum value at an index from the <see cref="ICustomEnum{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type of the custom enum.</typeparam>
        /// <param name="index">Index to get at.</param>
        /// <param name="defaultValue">Default value to return if no values were found.</param>
        /// <returns>Returns the custom enum value at the index.</returns>
        public static T GetValueOrDefault<T>(int index, T defaultValue) where T : ICustomEnum<T>, new() => TryGetValue(index, out T result) ? result : defaultValue;

        /// <summary>
        /// Gets a matching enum value from the <see cref="ICustomEnum{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type of the custom enum.</typeparam>
        /// <param name="match">Predicate match.</param>
        /// <param name="defaultValue">Default value to return if no values were found.</param>
        /// <returns>Returns the found custom enum value, or the default if no matching values were found..</returns>
        public static T GetValueOrDefault<T>(System.Predicate<T> match, T defaultValue) where T : ICustomEnum<T>, new() => TryGetValue(match, out T result) ? result : defaultValue;

        /// <summary>
        /// Checks if an item is in the range of enum values.
        /// </summary>
        /// <param name="customEnum">Custom enum reference.</param>
        /// <param name="index">Index of the value to check.</param>
        /// <returns>Returns true if the index is in the range of the enum values, otherwise returns false.</returns>
        public static bool InRange(this ICustomEnum customEnum, int index)
        {
            var values = customEnum.GetBoxedValues();
            for (int i = 0; i < values.Length; i++)
            {
                var value = values[i];
                if (value.Ordinal == index)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Tries to get a specific enum value at an index from the <paramref name="customEnum"/>.
        /// </summary>
        /// <typeparam name="T">Type of the custom enum.</typeparam>
        /// <param name="index">Index to get at.</param>
        /// <param name="result">Found value.</param>
        /// <returns>Returns true if a value was found, otherwise returns false.</returns>
        public static bool TryGetValue<T>(int index, out T result) where T : ICustomEnum<T>, new() => TryGetValue(new T(), index, out result);

        /// <summary>
        /// Tries to get a specific enum value at an index from the <paramref name="customEnum"/>.
        /// </summary>
        /// <typeparam name="T">Type of the custom enum.</typeparam>
        /// <param name="customEnum">Custom enum type.</param>
        /// <param name="index">Index to get at.</param>
        /// <param name="result">Found value.</param>
        /// <returns>Returns true if a value was found, otherwise returns false.</returns>
        public static bool TryGetValue<T>(ICustomEnum<T> customEnum, int index, out T result) where T : ICustomEnum
        {
            var values = customEnum.GetValues();
            for (int i = 0; i < values.Length; i++)
            {
                var value = values[i];
                if (value.Ordinal == index)
                {
                    result = value;
                    return true;
                }
            }
            result = default;
            return false;
        }

        public static bool TryGetValue<T>(System.Predicate<T> match, out T result) where T: ICustomEnum<T>, new()
        {
            var values = GetValues<T>();
            for (int i = 0; i < values.Length; i++)
            {
                var value = values[i];
                if (match(value))
                {
                    result = value;
                    return true;
                }
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Tries to get an enum value from a match.
        /// </summary>
        /// <typeparam name="T">Type of the custom enum.</typeparam>
        /// <param name="customEnum">Custom enum reference.</param>
        /// <param name="match">Predicate match.</param>
        /// <param name="result">Found enum value.</param>
        /// <returns>Returns true if an enum value was found, otherwise returns false.</returns>
        public static bool TryGetValue<T>(this ICustomEnum<T> customEnum, System.Predicate<T> match, out T result)
        {
            var values = customEnum.GetValues();
            for (int i = 0; i < values.Length; i++)
            {
                var value = values[i];
                if (match(value))
                {
                    result = value;
                    return true;
                }
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Adds a custom enum value.
        /// </summary>
        /// <typeparam name="T">Type of the custom enum.</typeparam>
        /// <param name="value">Value to add.</param>
        public static void AddValue<T>(T value) where T : ICustomEnum<T>, new()
        {
            if (value is null)
                return;

            var baseReference = new T();
            var values = baseReference.GetValues();
            var newValues = new T[values.Length + 1];
            System.Array.Copy(values, newValues, values.Length);
            newValues[values.Length] = value;
            baseReference.SetValues(newValues);
        }

        /// <summary>
        /// Gets the amount of enum values.
        /// </summary>
        /// <typeparam name="T">Type of the custom enum.</typeparam>
        /// <returns>Returns the amount of values a custom enum has.</returns>
        public static int GetCount<T>() where T : ICustomEnum, new() => GetCount(new T());

        /// <summary>
        /// Gets the amount of enum values.
        /// </summary>
        /// <param name="customEnum">Custom enum type.</param>
        /// <returns>Returns the amount of values a custom enum has.</returns>
        public static int GetCount(ICustomEnum customEnum) => customEnum.Count;

        public static string[] GetNames(this ICustomEnum customEnum)
        {
            var values = customEnum.GetBoxedValues();
            var names = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
                names[i] = values[i].Name;
            return names;
        }
        
        public static string[] GetDisplayNames(this ICustomEnum customEnum)
        {
            var values = customEnum.GetBoxedValues();
            var names = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
                names[i] = values[i].DisplayName;
            return names;
        }

        /// <summary>
        /// Compares two custom enum values and checks if they match.
        /// </summary>
        /// <typeparam name="T">Type of the custom enum.</typeparam>
        /// <param name="a">Reference enum value.</param>
        /// <param name="b">Enum value to compare to.</param>
        /// <returns>Returns true if the custom enum values match, otherwise returns false.</returns>
        public static bool Is<T>(this T a, T b) where T : ICustomEnum<T> => a is not null && b is not null && a.Equals(b);
    }

    #endregion

    #region Core

    /// <summary>
    /// File formats enum.
    /// </summary>
    public enum FileFormat
    {
        #region Main

        /// <summary>
        /// No file format.
        /// </summary>
        NULL,

        /// <summary>
        /// Audio file format.
        /// </summary>
        OGG,
        /// <summary>
        /// Audio file format.
        /// </summary>
        WAV,
        /// <summary>
        /// Audio file format.
        /// </summary>
        MP3,

        /// <summary>
        /// Image file format.
        /// </summary>
        PNG,
        /// <summary>
        /// Compressed image file format.
        /// </summary>
        JPG,

        /// <summary>
        /// Video file format.
        /// </summary>
        MP4,
        /// <summary>
        /// Video file format.
        /// </summary>
        MOV,

        /// <summary>
        /// ZIP file format/
        /// </summary>
        ZIP,

        /// <summary>
        /// JavaScript Object Notation file format.
        /// </summary>
        JSON,
        /// <summary>
        /// Text file format.
        /// </summary>
        TXT,
        /// <summary>
        /// C# file format.
        /// </summary>
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

        /// <summary>
        /// Dynamic Link Library file format.
        /// </summary>
        DLL,
        /// <summary>
        /// Executable file format.
        /// </summary>
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
        /// <summary>
        /// Legacy Achievement file format.
        /// </summary>
        LSA,

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

    /// <summary>
    /// Resolution setting.
    /// </summary>
    public class ResolutionType : Exists, ICustomEnum<ResolutionType>
    {
        public ResolutionType() { }

        public ResolutionType(int ordinal, string name, Vector2 resolution)
        {
            Ordinal = ordinal;
            Name = name;
            DisplayName = name.Remove("p");

            Resolution = resolution;
        }

        #region Enum Values

        public static readonly ResolutionType p270 = new(0, nameof(p270), new Vector2(480f, 270f));
        public static readonly ResolutionType p360 = new(1, nameof(p360), new Vector2(640f, 360f));
        public static readonly ResolutionType p540 = new(2, nameof(p540), new Vector2(960f, 540f));
        public static readonly ResolutionType p720 = new(3, nameof(p720), new Vector2(1280f, 720f));
        public static readonly ResolutionType p768 = new(4, nameof(p768), new Vector2(1360f, 768f));
        public static readonly ResolutionType p810 = new(5, nameof(p810), new Vector2(1440f, 810f));
        public static readonly ResolutionType p900 = new(6, nameof(p900), new Vector2(1600f, 900f));
        public static readonly ResolutionType p1080 = new(7, nameof(p1080), new Vector2(1920f, 1080f));
        public static readonly ResolutionType p1440 = new(8, nameof(p1440), new Vector2(2560f, 1440f));
        public static readonly ResolutionType p2160 = new(9, nameof(p2160), new Vector2(3840f, 2160f));

        static ResolutionType[] ENUMS = new ResolutionType[] { p270, p360, p540, p720, p768, p810, p900, p1080, p1440, p2160 };

        #endregion

        public int Ordinal { get; }
        public string Name { get; }
        public Lang DisplayName { get; }

        /// <summary>
        /// Resolution size.
        /// </summary>
        public Vector2 Resolution { get; }

        #region Implementation

        public int Count => ENUMS.Length;

        public ResolutionType[] GetValues() => ENUMS;

        public ResolutionType GetValue(int index) => ENUMS[index];

        public void SetValues(ResolutionType[] values) => ENUMS = values;

        public ICustomEnum[] GetBoxedValues() => ENUMS;

        public ICustomEnum GetBoxedValue(int index) => ENUMS[index];

        public ICustomEnum GetBoxedValue(string name) => ENUMS.First(x => x.Name == name);

        public bool InRange(int index) => ENUMS.InRange(index);

        #endregion

        #region Comparison

        public override bool Equals(object obj) => obj is ResolutionType other && Ordinal == other.Ordinal && Name == other.Name && DisplayName == other.DisplayName;

        public override int GetHashCode() => Core.Helpers.CoreHelper.CombineHashCodes(Ordinal, Name, DisplayName, Resolution.x, Resolution.y);

        public override string ToString() => $"Ordinal: {Ordinal} Name: {Name} Display Name: {DisplayName} Resolution: {Resolution}";

        public static implicit operator int(ResolutionType value) => value.Ordinal;

        public static implicit operator string(ResolutionType value) => value.Name;

        public static implicit operator ResolutionType(int value) => CustomEnumHelper.GetValue<ResolutionType>(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ResolutionType a, ResolutionType b) => a.Equals(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ResolutionType a, ResolutionType b) => !(a == b);

        #endregion
    }

    /// <summary>
    /// Where a URL is from.
    /// </summary>
    public enum URLSource
    {
        Song,
        Artist,
        Creator
    }

    /// <summary>
    /// What general type a scene is.
    /// </summary>
    public enum SceneType
    {
        /// <summary>
        /// Scene is an interface.
        /// </summary>
        Interface,
        /// <summary>
        /// Scene is the game.
        /// </summary>
        Game,
        /// <summary>
        /// Scene is the editor.
        /// </summary>
        Editor,
    }

    /// <summary>
    /// All scenes in PA Legacy.
    /// </summary>
    public enum SceneName
    {
        /// <summary>
        /// The main menu interface scene.
        /// </summary>
        Main_Menu,
        /// <summary>
        /// The player register interface scene.
        /// </summary>
        Input_Select,
        /// <summary>
        /// The game scene where levels are played.
        /// </summary>
        Game,
        /// <summary>
        /// The editor scene.
        /// </summary>
        Editor,
        /// <summary>
        /// The story interface scene.
        /// </summary>
        Interface,
        /// <summary>
        /// Unused story demo end scene.
        /// </summary>
        post_level,
        /// <summary>
        /// The arcade menu scene.
        /// </summary>
        Arcade_Select
    }

    /// <summary>
    /// Language the game should be in.
    /// </summary>
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
        Polish,
        AncientAutobot,
        Matoran
    }

    /// <summary>
    /// Music loaded into the SoundLibrary.
    /// </summary>
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
        jukio_distance,
        little_calculations,
        reflections,
        fracture,
        xilioh,
    }

    /// <summary>
    /// Sounds loaded into the SoundLibrary.
    /// </summary>
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

        pop
    }

    /// <summary>
    /// What the loading UI should display as.
    /// </summary>
    public enum LoadingDisplayType
    {
        /// <summary>
        /// Displays a little doggo.
        /// </summary>
        Doggo,
        /// <summary>
        /// Displays a waveform.
        /// </summary>
        Waveform,
        /// <summary>
        /// Displays a bar.
        /// </summary>
        Bar,
        /// <summary>
        /// Displays a percentage (like 60%)
        /// </summary>
        Percentage,
        /// <summary>
        /// Displays an equals bar (like [ ====    ])
        /// </summary>
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

    /// <summary>
    /// Visibility of the item on the Arcade server.
    /// </summary>
    public enum ServerVisibility
    {
        /// <summary>
        /// Shows for everyone.
        /// </summary>
        Public,
        /// <summary>
        /// Only shows if the user searches for a specific ID.
        /// </summary>
        Unlisted,
        /// <summary>
        /// Only shows for the publisher.
        /// </summary>
        Private
    }

    /// <summary>
    /// PA Easings.
    /// </summary>
    public enum Easing
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
        Function,
        Unrecognized
    }

    /// <summary>
    /// Represents a math symbol such as "+" or "-".
    /// </summary>
    public enum MathOperation
    {
        Addition,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        Set,
    }

    /// <summary>
    /// Represents the type of transform value.
    /// </summary>
    public enum TransformType
    {
        Position,
        Scale,
        Rotation,
        Color
    }

    /// <summary>
    /// Represents how an exception is handled when it's caught.
    /// </summary>
    public enum HandleException
    {
        /// <summary>
        /// Throws the exception.
        /// </summary>
        Throw,
        /// <summary>
        /// Breaks the sequence.
        /// </summary>
        Break,
        /// <summary>
        /// Continues the sequences.
        /// </summary>
        Continue,
        /// <summary>
        /// Does nothing.
        /// </summary>
        Nothing,
    }

    #endregion

    #region Beatmap

    /// <summary>
    /// Represents all shape types.
    /// </summary>
    public enum ShapeType
    {
        Square = 0,
        Circle = 1,
        Triangle = 2,
        Arrow = 3,
        /// <summary>
        /// Shape that displays text.
        /// </summary>
        Text = 4,
        Hexagon = 5,
        /// <summary>
        /// Shape that displays an image.
        /// </summary>
        Image = 6,
        Pentagon = 7,
        /// <summary>
        /// Contains misc shapes.
        /// </summary>
        Misc = 8,
        /// <summary>
        /// Shape with custom polygon settings.
        /// </summary>
        Polygon = 9,
        Particles = 10,
    }

    /// <summary>
    /// Gradient rendering type.
    /// </summary>
    public enum GradientType
    {
        /// <summary>
        /// The regular render method.
        /// </summary>
        Normal = 0,
        /// <summary>
        /// Renders a linear gradient going from right to left.
        /// </summary>
        RightLinear = 1,
        /// <summary>
        /// Renders a linear gradient going from left to right.
        /// </summary>
        LeftLinear = 2,
        /// <summary>
        /// Renders a radial gradient going from out to in.
        /// </summary>
        OutInRadial = 3,
        /// <summary>
        /// Renders a radial gradient going from in to out.
        /// </summary>
        InOutRadial = 4,
    }

    /// <summary>
    /// Object despawn behavior.
    /// </summary>
    public enum AutoKillType
    {
        /// <summary>
        /// Object will not despawn. Good for character models or general persistent objects.
        /// </summary>
        NoAutokill,
        /// <summary>
        /// Object will despawn once all animations are done.
        /// </summary>
        LastKeyframe,
        /// <summary>
        /// Object will despawn once all animations are done and at an offset.
        /// </summary>
        LastKeyframeOffset,
        /// <summary>
        /// Object will despawn after a fixed time.
        /// </summary>
        FixedTime,
        /// <summary>
        /// Object will despawn at song time.
        /// </summary>
        SongTime
    }

    public enum PrefabAutoKillType
    {
        Regular,
        StartTimeOffset,
        SongTime
    }

    public enum RandomType
    {
        // actual random
        None,
        Normal,
        /// <summary>
        /// Support for really old levels (for some reason).
        /// </summary>
        BETA_SUPPORT,
        Toggle,
        Scale,

        // homing
        HomingStatic,
        HomingDynamic,
    }

    #endregion

    #region Arcade / Game

    /// <summary>
    /// Difficulty of a level.
    /// </summary>
    public class DifficultyType : Exists, ICustomEnum<DifficultyType>
    {
        public DifficultyType() { }

        public DifficultyType(int ordinal, string name, Color color)
        {
            Ordinal = ordinal;
            Name = name;
            DisplayName = name;
            Color = color;
        }

        public DifficultyType(int ordinal, string name, string displayName, Color color)
        {
            Ordinal = ordinal;
            Name = name;
            DisplayName = displayName;
            Color = color;
        }

        #region Enum Values

        public static readonly DifficultyType Unknown = new(-1, nameof(Unknown), "Unknown Difficulty", RTColors.HexToColor("424242"));
        public static readonly DifficultyType Easy = new(0, nameof(Easy), RTColors.HexToColor("29B6F6"));
        public static readonly DifficultyType Normal = new(1, nameof(Normal), RTColors.HexToColor("9CCC65"));
        public static readonly DifficultyType Hard = new(2, nameof(Hard), RTColors.HexToColor("FFB039"));
        public static readonly DifficultyType Expert = new(3, nameof(Expert), RTColors.HexToColor("E47272"));
        public static readonly DifficultyType ExpertPlus = new(4, nameof(ExpertPlus), "Expert+", RTColors.HexToColor("373737"));
        public static readonly DifficultyType Master = new(5, nameof(Master), new Color(0.25f, 0.01f, 0.01f));
        public static readonly DifficultyType Animation = new(6, nameof(Animation), RTColors.HexToColor("999999"));

        static DifficultyType[] ENUMS = new DifficultyType[] { Unknown, Easy, Normal, Hard, Expert, ExpertPlus, Master, Animation };

        #endregion

        public int Ordinal { get; }
        public string Name { get; }
        public Lang DisplayName { get; }

        public Color Color { get; }

        #region Implementation

        public int Count => ENUMS.Length;

        public DifficultyType[] GetValues() => ENUMS;

        public DifficultyType GetValue(int index) => ENUMS[index];

        public void SetValues(DifficultyType[] values) => ENUMS = values;

        public ICustomEnum[] GetBoxedValues() => ENUMS;

        public ICustomEnum GetBoxedValue(int index) => ENUMS[index];

        public ICustomEnum GetBoxedValue(string name) => ENUMS.First(x => x.Name == name);

        public bool InRange(int index) => ENUMS.InRange(index);

        #endregion

        #region Comparison

        public override bool Equals(object obj) => obj is DifficultyType other && Ordinal == other.Ordinal && Name == other.Name && DisplayName == other.DisplayName;

        public override int GetHashCode() => Core.Helpers.CoreHelper.CombineHashCodes(Ordinal, Name, DisplayName, Color.r, Color.g, Color.b, Color.a);

        public override string ToString() => $"Ordinal: {Ordinal} Name: {Name} Display Name: {DisplayName}";

        public static implicit operator int(DifficultyType value) => value.Ordinal;

        public static implicit operator string(DifficultyType value) => value.Name;

        public static implicit operator DifficultyType(int value) => CustomEnumHelper.GetValue<DifficultyType>(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(DifficultyType a, DifficultyType b) => a.Equals(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(DifficultyType a, DifficultyType b) => !(a == b);

        #endregion
    }

    /// <summary>
    /// Ranking award.
    /// </summary>
    public class Rank : Exists, ICustomEnum<Rank>
    {
        public Rank() { }

        public Rank(int ordinal, string name, Color color, int minHits, int maxHits)
        {
            Ordinal = ordinal;
            Name = name;
            DisplayName = name;
            Color = color;
            MinHits = minHits;
            MaxHits = maxHits;
        }

        public Rank(int ordinal, string name, string displayName, Color color, int minHits, int maxHits)
        {
            Ordinal = ordinal;
            Name = name;
            DisplayName = displayName;
            Color = color;
            MinHits = minHits;
            MaxHits = maxHits;
        }

        #region Enum Values

        /// <summary>
        /// Does not set a rank.
        /// </summary>
        public static readonly Rank Null = new(0, nameof(Null), "-", RTColors.HexToColor("424242"), -1, -1);
        /// <summary>
        /// 0 hits.
        /// </summary>
        public static readonly Rank SS = new(1, nameof(SS), RTColors.HexToColor("29B6F6"), 0, 0);
        /// <summary>
        /// 1 hit.
        /// </summary>
        public static readonly Rank S = new(2, nameof(S), RTColors.HexToColor("9CCC65"), 1, 1);
        /// <summary>
        /// 2 to 3 hits.
        /// </summary>
        public static readonly Rank A = new(3, nameof(A), RTColors.HexToColor("9CCC65"), 2, 3);
        /// <summary>
        /// 4 to 6 hits.
        /// </summary>
        public static readonly Rank B = new(4, nameof(B), RTColors.HexToColor("FFB039"), 4, 6);
        /// <summary>
        /// 7 to 9 hits.
        /// </summary>
        public static readonly Rank C = new(5, nameof(C), RTColors.HexToColor("FFB039"), 7, 9);
        /// <summary>
        /// 10 to 15 hits.
        /// </summary>
        public static readonly Rank D = new(6, nameof(D), RTColors.HexToColor("E47272"), 10, 15);
        /// <summary>
        /// 16+ hits.
        /// </summary>
        public static readonly Rank F = new(7, nameof(F), RTColors.HexToColor("E47272"), 16, int.MaxValue);

        static Rank[] ENUMS = new Rank[] { Null, SS, S, A, B, C, D, F };

        #endregion

        public int Ordinal { get; }
        public string Name { get; }
        public Lang DisplayName { get; }

        /// <summary>
        /// Color of the rank.
        /// </summary>
        public Color Color { get; }

        /// <summary>
        /// Minimum hits to earn the rank.
        /// </summary>
        public int MinHits { get; }

        /// <summary>
        /// Maximum hits to earn the rank.
        /// </summary>
        public int MaxHits { get; }

        /// <summary>
        /// Formats a <see cref="Rank"/> to have the proper style and color.
        /// </summary>
        /// <param name="rank">Rank to format.</param>
        /// <returns>Returns a formatted Rank string to display on text.</returns>
        public string Format() => $"<#{RTColors.ColorToHex(Color)}><b>{DisplayName}</b></color>";

        /// <summary>
        /// Rank to use in the editor.
        /// </summary>
        public static Rank EditorRank => Configs.EditorConfig.Instance.EditorRank.Value;

        #region Implementation

        public int Count => ENUMS.Length;

        public Rank[] GetValues() => ENUMS;

        public Rank GetValue(int index) => ENUMS[index];

        public void SetValues(Rank[] values) => ENUMS = values;

        public ICustomEnum[] GetBoxedValues() => ENUMS;

        public ICustomEnum GetBoxedValue(int index) => ENUMS[index];

        public ICustomEnum GetBoxedValue(string name) => ENUMS.First(x => x.Name == name);

        public bool InRange(int index) => ENUMS.InRange(index);

        #endregion

        #region Comparison

        public override bool Equals(object obj) => obj is Rank other && Ordinal == other.Ordinal && Name == other.Name && DisplayName == other.DisplayName && MinHits == other.MinHits && MaxHits == other.MaxHits;

        public override int GetHashCode() => Core.Helpers.CoreHelper.CombineHashCodes(Ordinal, Name, DisplayName, Color.r, Color.g, Color.b, Color.a, MinHits, MaxHits);

        public override string ToString() => $"Ordinal: {Ordinal} Name: {Name} Display Name: {DisplayName}";

        public static implicit operator int(Rank value) => value.Ordinal;

        public static implicit operator string(Rank value) => value.Name;

        public static implicit operator Rank(int value) => CustomEnumHelper.GetValue<Rank>(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Rank a, Rank b) => a.Equals(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Rank a, Rank b) => !(a == b);

        #endregion
    }

    /// <summary>
    /// How the Steam Workshop Query should be sorted.
    /// </summary>
    public enum QuerySort
    {
        None,
        UploadDate,
        Votes,
        VotesUp,
        TotalVotes,
        TotalSubs,
        Trend
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

    /// <summary>
    /// Game speed / pitch setting.
    /// </summary>
    public class GameSpeed : Exists, ICustomEnum<GameSpeed>
    {
        public GameSpeed() { }

        public GameSpeed(int ordinal, string name, float pitch)
        {
            Ordinal = ordinal;
            Name = name;
            DisplayName = name;
            Pitch = pitch;
        }

        public GameSpeed(int ordinal, string name, string displayName, float pitch)
        {
            Ordinal = ordinal;
            Name = name;
            DisplayName = displayName;
            Pitch = pitch;
        }

        #region Enum Values

        public static readonly GameSpeed X0_1 = new(0, nameof(X0_1), "0.1x", 0.1f);
        public static readonly GameSpeed X0_5 = new(1, nameof(X0_5), "0.5x", 0.5f);
        public static readonly GameSpeed X0_8 = new(2, nameof(X0_8), "0.8x", 0.8f);
        public static readonly GameSpeed X1_0 = new(3, nameof(X1_0), "1.0x", 1.0f);
        public static readonly GameSpeed X1_2 = new(4, nameof(X1_2), "1.2x", 1.2f);
        public static readonly GameSpeed X1_5 = new(5, nameof(X1_5), "1.5x", 1.5f);
        public static readonly GameSpeed X2_0 = new(6, nameof(X2_0), "2.0x", 2.0f);

        static GameSpeed[] ENUMS = new GameSpeed[] { X0_1, X0_5, X0_8, X1_0, X1_2, X1_5, X2_0 };

        #endregion

        public int Ordinal { get; }
        public string Name { get; }
        public Lang DisplayName { get; }

        public float Pitch { get; }

        #region Implementation

        public int Count => ENUMS.Length;

        public GameSpeed[] GetValues() => ENUMS;

        public GameSpeed GetValue(int index) => ENUMS[index];

        public void SetValues(GameSpeed[] values) => ENUMS = values;

        public ICustomEnum[] GetBoxedValues() => ENUMS;

        public ICustomEnum GetBoxedValue(int index) => ENUMS[index];

        public ICustomEnum GetBoxedValue(string name) => ENUMS.First(x => x.Name == name);

        public bool InRange(int index) => ENUMS.InRange(index);

        #endregion

        #region Comparison

        public override bool Equals(object obj) => obj is GameSpeed other && Ordinal == other.Ordinal && Name == other.Name && DisplayName == other.DisplayName && Pitch == other.Pitch;

        public override int GetHashCode() => Core.Helpers.CoreHelper.CombineHashCodes(Ordinal, Name, DisplayName, Pitch);

        public override string ToString() => $"Ordinal: {Ordinal} Name: {Name} Display Name: {DisplayName}";

        public static implicit operator int(GameSpeed value) => value.Ordinal;

        public static implicit operator string(GameSpeed value) => value.Name;

        public static implicit operator GameSpeed(int value) => CustomEnumHelper.GetValue<GameSpeed>(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(GameSpeed a, GameSpeed b) => a.Equals(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(GameSpeed a, GameSpeed b) => !(a == b);

        #endregion
    }

    /// <summary>
    /// Custom challenge setting.
    /// </summary>
    public class ChallengeMode : Exists, ICustomEnum<ChallengeMode>
    {
        public ChallengeMode() { }

        public ChallengeMode(int ordinal, string name, bool invincible, bool damageable, int defaultHealth, int lives) : this(ordinal, name, name, invincible, damageable, defaultHealth, lives) { }

        public ChallengeMode(int ordinal, string name, string displayName, bool invincible, bool damageable, int defaultHealth, int lives)
        {
            Ordinal = ordinal;
            Name = name;
            DisplayName = displayName;
            Invincible = invincible;
            Damageable = damageable;
            DefaultHealth = defaultHealth;
            Lives = lives;
        }

        #region Enum Values

        /// <summary>
        /// No damage is taken.
        /// </summary>
        public static readonly ChallengeMode Zen = new(0, nameof(Zen), true, false, -1, -1);
        /// <summary>
        /// Damage is taken, but health is not subtracted so the Player will not die.
        /// </summary>
        public static readonly ChallengeMode Practice = new(1, nameof(Practice), true, true, -1, -1);
        /// <summary>
        /// Damage is taken and health is subtracted.
        /// </summary>
        public static readonly ChallengeMode Normal = new(2, nameof(Normal), false, true, -1, -1);
        /// <summary>
        /// The level restarts when all Players are dead.
        /// </summary>
        public static readonly ChallengeMode OneLife = new(3, nameof(OneLife), "1 Life", false, true, -1, 1);
        /// <summary>
        /// The level restarts when any Player takes damage.
        /// </summary>
        public static readonly ChallengeMode OneHit = new(4, nameof(OneHit), "1 Hit", false, true, 1, 1);

        static ChallengeMode[] ENUMS = new ChallengeMode[] { Zen, Practice, Normal, OneLife, OneHit, };

        #endregion

        public int Ordinal { get; }
        public string Name { get; }
        public Lang DisplayName { get; }

        /// <summary>
        /// If the player's health is not subtracted when colliding with normal objects. If false, <see cref="Damageable"/> must be on for noticable affects.
        /// </summary>
        public bool Invincible { get; }

        /// <summary>
        /// If the player takes damage, but the health is not subtracted.
        /// </summary>
        public bool Damageable { get; }

        /// <summary>
        /// The default health the player spawns with. If set to -1, custom spawn health is allowed.
        /// </summary>
        public int DefaultHealth { get; }

        /// <summary>
        /// How many lives the player has until they have to restart the level. -1 represents infinite lives.
        /// </summary>
        public int Lives { get; }

        #region Implementation

        public int Count => ENUMS.Length;

        public ChallengeMode[] GetValues() => ENUMS;

        public ChallengeMode GetValue(int index) => ENUMS[index];

        public void SetValues(ChallengeMode[] values) => ENUMS = values;

        public ICustomEnum[] GetBoxedValues() => ENUMS;

        public ICustomEnum GetBoxedValue(int index) => ENUMS[index];

        public ICustomEnum GetBoxedValue(string name) => ENUMS.First(x => x.Name == name);

        public bool InRange(int index) => ENUMS.InRange(index);

        #endregion

        #region Comparison

        public override bool Equals(object obj) => obj is ChallengeMode other && Ordinal == other.Ordinal && Name == other.Name && DisplayName == other.DisplayName && Invincible == other.Invincible && Damageable == other.Damageable && DefaultHealth == other.DefaultHealth && Lives == other.Lives;

        public override int GetHashCode() => Core.Helpers.CoreHelper.CombineHashCodes(Ordinal, Name, DisplayName, Invincible, Damageable, DefaultHealth, Lives);

        public override string ToString() => $"Ordinal: {Ordinal} Name: {Name} Display Name: {DisplayName} Invincible: {Invincible} Damagable: {Damageable} Default Health: {DefaultHealth} Lives: {Lives}";

        public static implicit operator int(ChallengeMode value) => value.Ordinal;

        public static implicit operator string(ChallengeMode value) => value.Name;

        public static implicit operator ChallengeMode(int value) => CustomEnumHelper.GetValue<ChallengeMode>(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ChallengeMode a, ChallengeMode b) => a.Equals(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ChallengeMode a, ChallengeMode b) => !(a == b);

        #endregion
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

    /// <summary>
    /// The function that should occur when a level ends.
    /// </summary>
    public enum EndLevelFunction
    {
        /// <summary>
        /// Initializes the EndLevelMenu.
        /// </summary>
        EndLevelMenu,
        /// <summary>
        /// Loads the Arcade scene.
        /// </summary>
        QuitToArcade,
        /// <summary>
        /// Returns to the hub level if there was one loaded.
        /// </summary>
        ReturnToHub,
        /// <summary>
        /// Returns to the previously loaded level.
        /// </summary>
        ReturnToPrevious,
        /// <summary>
        /// Continues the collection.
        /// </summary>
        ContinueCollection,
        /// <summary>
        /// Loads another level.
        /// </summary>
        LoadLevel,
        /// <summary>
        /// Loads another level in the current collection.
        /// </summary>
        LoadLevelInCollection,
        /// <summary>
        /// Parses an interface.
        /// </summary>
        ParseInterface,
        /// <summary>
        /// Loops the level.
        /// </summary>
        Loop,
        /// <summary>
        /// Fully restarts the level, clearing hit and death data.
        /// </summary>
        Restart
    }

    #endregion

    #region Editor

    public enum UserPreferenceType
    {
        None,
        Beginner,
        Legacy,
        Alpha,
        Modded
    }

    /// <summary>
    /// How <i>advanced</i> a feature is.
    /// </summary>
    public enum Complexity
    {
        /// <summary>
        /// Only show the most basic of features.
        /// </summary>
        Simple,
        /// <summary>
        /// Only show vanilla features.
        /// </summary>
        Normal,
        /// <summary>
        /// Show all features.
        /// </summary>
        Advanced
    }

    /// <summary>
    /// Editor theme enum.
    /// </summary>
    public class EditorThemeType : Exists, ICustomEnum<EditorThemeType>
    {
        public EditorThemeType() { }

        public EditorThemeType(int ordinal, string name)
        {
            Ordinal = ordinal;
            Name = name;
            DisplayName = name;
        }

        #region Enum Values

        public static readonly EditorThemeType Legacy = new(0, nameof(Legacy));
        public static readonly EditorThemeType Dark = new(1, nameof(Dark));
        public static readonly EditorThemeType Light = new(2, nameof(Light));
        public static readonly EditorThemeType Vision = new(3, nameof(Vision));
        public static readonly EditorThemeType Butter = new(4, nameof(Butter));
        public static readonly EditorThemeType Arrhythmia = new(5, nameof(Arrhythmia));
        public static readonly EditorThemeType Modern = new(6, nameof(Modern));
        public static readonly EditorThemeType Beats = new(7, nameof(Beats));
        public static readonly EditorThemeType Archives = new(8, nameof(Archives));
        public static readonly EditorThemeType Void = new(9, nameof(Void));

        static EditorThemeType[] ENUMS = new EditorThemeType[] { Legacy, Dark, Light, Vision, Butter, Arrhythmia, Modern, Beats, Archives, Void };

        #endregion

        public int Ordinal { get; }
        public string Name { get; }
        public Lang DisplayName { get; }

        #region Implementation

        public int Count => ENUMS.Length;

        public EditorThemeType[] GetValues() => ENUMS;

        public EditorThemeType GetValue(int index) => ENUMS[index];

        public void SetValues(EditorThemeType[] values) => ENUMS = values;

        public ICustomEnum[] GetBoxedValues() => ENUMS;

        public ICustomEnum GetBoxedValue(int index) => ENUMS[index];

        public ICustomEnum GetBoxedValue(string name) => ENUMS.First(x => x.Name == name);

        public bool InRange(int index) => ENUMS.InRange(index);

        #endregion

        #region Comparison

        public override bool Equals(object obj) => obj is EditorThemeType other && Ordinal == other.Ordinal && Name == other.Name && DisplayName == other.DisplayName;

        public override int GetHashCode() => Core.Helpers.CoreHelper.CombineHashCodes(Ordinal, Name, DisplayName);

        public override string ToString() => $"Ordinal: {Ordinal} Name: {Name} Display Name: {DisplayName}";

        public static implicit operator int(EditorThemeType value) => value.Ordinal;

        public static implicit operator string(EditorThemeType value) => value.Name;

        public static implicit operator EditorThemeType(int value) => CustomEnumHelper.GetValue<EditorThemeType>(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(EditorThemeType a, EditorThemeType b) => a.Equals(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(EditorThemeType a, EditorThemeType b) => !(a == b);

        #endregion
    }

    /// <summary>
    /// Theme group used for applying Editor Themes.
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
        Layer_1,
        Layer_2,
        Layer_3,
        Layer_4,
        Layer_5,
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

    /// <summary>
    /// The type of Waveform the editor timeline should render.
    /// </summary>
    public enum WaveformType
    {
        Split,
        Centered,
        Bottom,
        SplitDetailed,
        CenteredDetailed,
        BottomDetailed
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

    public enum BinSliderControlActive
    {
        KeyHeld,
        KeyToggled,
        Always,
        Never
    }

    public enum BinClamp
    {
        /// <summary>
        /// Bin clamps between min and max.
        /// </summary>
        Clamp,
        /// <summary>
        /// Bin modulos from max to min or min to max.
        /// </summary>
        Loop,
        /// <summary>
        /// Bin continues on.
        /// </summary>
        None,
    }

    #endregion

    #region Menus

    /// <summary>
    /// Source of the menu music.
    /// </summary>
    public enum MenuMusicLoadMode
    {
        /// <summary>
        /// Takes from Project Arrhythmia/settings/menus
        /// </summary>
        Settings,
        /// <summary>
        /// Takes from the arcade folder.
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

    #endregion

    #region Misc

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

    /// <summary>
    /// Type of the players' current device.
    /// </summary>
    public enum ControllerType
    {
        XBox,
        PS,
        Keyboard,
        Unknown
    }

    #endregion
}
