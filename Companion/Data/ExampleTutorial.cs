using System;
using System.Linq;
using System.Runtime.CompilerServices;

using BetterLegacy.Companion.Entity;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;

namespace BetterLegacy.Companion.Data
{
    public class ExampleTutorial : Exists, ICustomEnum<ExampleTutorial>
    {
        public ExampleTutorial() { }

        public ExampleTutorial(int ordinal, string name, Action<Example>[] actions)
        {
            Ordinal = ordinal;
            Name = name;
            DisplayName = name;

            this.actions = actions;
        }

        #region Enum Values

        public static readonly ExampleTutorial NONE = new(-1, nameof(NONE), Array.Empty<Action<Example>>());

        public static readonly ExampleTutorial CREATE_LEVEL = new(0, nameof(CREATE_LEVEL), new Action<Example>[]
        {
            // start
            // open new level popup
            reference => { reference?.chatBubble?.Say("First, go to the New Level Creator popup."); },
            // step 1
            // search for a song to use
            reference => { reference?.chatBubble?.Say("Next, search for a song to use."); },
            // step 2
            // name the level
            reference => { reference?.chatBubble?.Say("Now name the level."); },
            // step 3
            // name the song
            reference => { reference?.chatBubble?.Say("Then name the song."); },
            // step 4
            // create level
            reference => { reference?.chatBubble?.Say("And finally, click Create."); },
            // step 5
            // new level has loaded
            reference => { reference?.chatBubble?.Say("You just made a level."); },
        });

        static ExampleTutorial[] ENUMS = new ExampleTutorial[] { CREATE_LEVEL };

        #endregion

        public int Ordinal { get; }
        public string Name { get; }
        public Lang DisplayName { get; }

        /// <summary>
        /// List of actions to progress through.
        /// </summary>
        readonly Action<Example>[] actions;

        /// <summary>
        /// Amount of actions in this tutorial.
        /// </summary>
        public int ActionCount => actions.Length;

        public Action<Example> this[int index] => actions[index];

        #region Implementation

        public int Count => ENUMS.Length;

        public ExampleTutorial[] GetValues() => ENUMS;

        public ExampleTutorial GetValue(int index) => ENUMS[index];

        public void SetValues(ExampleTutorial[] values) => ENUMS = values;

        public ICustomEnum[] GetBoxedValues() => ENUMS;

        public ICustomEnum GetBoxedValue(int index) => ENUMS[index];

        public ICustomEnum GetBoxedValue(string name) => ENUMS.First(x => x.Name == name);

        public bool InRange(int index) => ENUMS.InRange(index);

        #endregion

        #region Comparison

        public override bool Equals(object obj) => obj is ExampleTutorial other && Ordinal == other.Ordinal && Name == other.Name && DisplayName == other.DisplayName;

        public override int GetHashCode() => Core.Helpers.CoreHelper.CombineHashCodes(Ordinal, Name, DisplayName);

        public override string ToString() => $"Ordinal: {Ordinal} Name: {Name} Display Name: {DisplayName}";

        public static implicit operator int(ExampleTutorial value) => value.Ordinal;

        public static implicit operator string(ExampleTutorial value) => value.Name;

        public static implicit operator ExampleTutorial(int value) => CustomEnumHelper.GetValue<ExampleTutorial>(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ExampleTutorial a, ExampleTutorial b) => a.Equals(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ExampleTutorial a, ExampleTutorial b) => !(a == b);

        #endregion
    }
}
