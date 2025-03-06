using BetterLegacy.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Companion.Data
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
}
