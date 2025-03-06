﻿using BetterLegacy.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Companion.Data
{
    /// <summary>
    /// Represents a group of dialogue that Example can say.
    /// </summary>
    public class ExampleDialogueGroup : Exists
    {
        public ExampleDialogueGroup() { }

        public ExampleDialogueGroup(string key, ExampleDialogue[] dialogues)
        {
            this.key = key;
            this.dialogues = dialogues;
        }

        /// <summary>
        /// Key of the group.
        /// </summary>
        public string key;

        /// <summary>
        /// Dialogues array.
        /// </summary>
        public ExampleDialogue[] dialogues;

        /// <summary>
        /// Gets a random dialogue.
        /// </summary>
        /// <returns>Returns a random dialogue from the group.</returns>
        public ExampleDialogue GetDialogue()
        {
            if (dialogues.Length < 0)
                return null;

            if (dialogues.Length == 1)
                return dialogues[0];

            int num = UnityEngine.Random.Range(0, dialogues.Length);
            int attempts = 0;
            while (!dialogues[num].CanSay())
            {
                num = UnityEngine.Random.Range(0, dialogues.Length);

                // prevents the game from getting stuck if Example chooses to not say anything.
                attempts++;
                if (attempts > 100)
                    return null;
            }

            return dialogues[num];
        }
    }
}
