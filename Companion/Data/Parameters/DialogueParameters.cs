using System;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Data.Player;

namespace BetterLegacy.Companion.Data.Parameters
{
    /// <summary>
    /// Dialogue parameters passed to Example's chat bubble.
    /// </summary>
    public class DialogueParameters : Exists
    {
        public DialogueParameters() { }

        public DialogueParameters(int dialogueOption)
        {
            this.dialogueOption = dialogueOption;
        }

        public DialogueParameters(float textLength, float stayTime, float time, int dialogueOption = -1, Action onComplete = null)
        {
            this.textLength = textLength;
            this.stayTime = stayTime;
            this.time = time;
            this.dialogueOption = dialogueOption;
            this.onComplete = onComplete;
        }

        /// <summary>
        /// Length of the text.
        /// </summary>
        public float textLength = 1.5f;

        /// <summary>
        /// Time the chat bubble should stay for.
        /// </summary>
        public float stayTime = 4f;

        /// <summary>
        /// Speed of the chat bubble animation.
        /// </summary>
        public float time = 0.7f;

        /// <summary>
        /// For dialogues with multiple options. If set to -1, dialogue should default to random values.
        /// </summary>
        public int dialogueOption = -1;

        /// <summary>
        /// Action to run when the dialogue is finished.
        /// </summary>
        public Action onComplete;

        /// <summary>
        /// If the dialogue can be overridden by other dialogues.
        /// </summary>
        public bool allowOverride = true;

        public override string ToString() => $"Dialogue Parameters: [textLength = {textLength}, stayTime = {stayTime}, time = {time}]";
    }

    /// <summary>
    /// Dialogue parameters passed from a level.
    /// </summary>
    public class LevelDialogueParameters : DialogueParameters
    {
        public LevelDialogueParameters(Level level, Rank rank) : base()
        {
            this.level = level;
            this.rank = rank;
        }

        public LevelDialogueParameters(Level level, Rank rank, float textLength, float stayTime, float time, int dialogueOption = -1, Action onComplete = null) : base(textLength, stayTime, time, dialogueOption, onComplete)
        {
            this.level = level;
            this.rank = rank;
        }

        /// <summary>
        /// Level reference.
        /// </summary>
        public Level level;

        /// <summary>
        /// Level rank the user got.
        /// </summary>
        public Rank rank;
    }

    /// <summary>
    /// Dialogue parameters passed from a player.
    /// </summary>
    public class PlayerDialogueParameters : DialogueParameters
    {
        public PlayerDialogueParameters(CustomPlayer player) : base()
        {
            this.player = player;
        }

        /// <summary>
        /// Player reference.
        /// </summary>
        public CustomPlayer player;
    }

    /// <summary>
    /// Dialogue parameters passed from an idea request.
    /// </summary>
    public class IdeaDialogueParameters : DialogueParameters
    {
        public IdeaDialogueParameters(IdeaContext ideaContext) : base() => this.ideaContext = ideaContext;

        /// <summary>
        /// Context of the idea to give.
        /// </summary>
        public IdeaContext ideaContext;

        public enum IdeaContext
        {
            Random,
            Level,
            Prefab,
            Character,
        }
    }
}
