using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Companion.Data;
using BetterLegacy.Companion.Data.Parameters;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Companion.Entity
{
    /// <summary>
    /// Represents Example's chat bubble. This is how he talks.
    /// </summary>
    public class ExampleChatBubble : ExampleModule
    {
        #region Default Instance

        public ExampleChatBubble() { }

        /// <summary>
        /// The default chat bubble.
        /// </summary>
        public static Func<ExampleChatBubble> getDefault = () =>
        {
            var brain = new ExampleChatBubble();
            brain.InitDefault();

            return brain;
        };

        public override void InitDefault()
        {
            RegisterDialogues();
        }

        #endregion

        #region Core

        public override void Build()
        {
            var dialogueBase = Creator.NewUIObject("Dialogue Base", reference.model.baseCanvas.transform);
            dialogueBase.transform.AsRT().anchoredPosition = Vector2.zero;

            this.dialogueBase = dialogueBase.transform.AsRT();

            var dialogueImage = Creator.NewUIObject("Image", dialogueBase.transform);
            dialogueImage.transform.AsRT().anchoredPosition = Vector2.zero;
            dialogueImage.transform.AsRT().sizeDelta = new Vector2(300f, 150f);

            this.dialogueImage = dialogueImage.AddComponent<Image>();

            var dialogueText = Creator.NewUIObject("Text", dialogueImage.transform);
            dialogueText.transform.AsRT().anchoredPosition = Vector2.zero;
            dialogueText.transform.AsRT().sizeDelta = new Vector2(280f, 140f);

            var text = dialogueText.AddComponent<Text>();
            text.font = Font.GetDefault();
            text.fontSize = 22;
            text.color = new Color(0.06f, 0.06f, 0.06f, 1f);
            this.dialogueText = text;

            SetScale(Vector2.zero);
        }

        public override void Tick()
        {
            if (!dialogueBase || !reference || !reference.model)
                return;

            float addToDialogueY = 200f;
            if (reference.model.position.y > 355f)
                addToDialogueY = -200f;

            // Set dialogue box position instead of parent because only position needs to be tracked instead of scale, rotation, etc.
            dialogueBase.localPosition = new Vector3(Mathf.Clamp(reference.model.position.x, -820f, 820f), reference.model.position.y + addToDialogueY);
        }

        public override void Clear()
        {
            attributes.Clear();
        }

        #endregion

        #region Dialogue

        /// <summary>
        /// The groups of dialogues Example can say.
        /// </summary>
        public List<ExampleDialogueGroup> dialogues = new List<ExampleDialogueGroup>();

        /// <summary>
        /// Registers dialogues.
        /// </summary>
        public virtual void RegisterDialogues()
        {
            dialogues.Add(new ExampleDialogueGroup(Dialogues.SPAWN, new ExampleDialogue[]
            {
                new ExampleDialogue((companion, parameters) => "What would you like me to do?"),
                new ExampleDialogue((companion, parameters) => "Make something awesome! Or not, if you're not a creator."),
                new ExampleDialogue((companion, parameters) => "Tori has entered the editor and I'm scared. I'm joking, keep building to your hearts desire!",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_TORI)),
                new ExampleDialogue((companion, parameters) => "DIGGYYYYYYY BROOOOOOOOOOOOO",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_DIGGY)),
                new ExampleDialogue((companion, parameters) => "A *BS*USB? Helloooo?!",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_DIGGY)),
                new ExampleDialogue((companion, parameters) => "Sleepy time",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_SLEEPYZ)),
                new ExampleDialogue((companion, parameters) => "i forgor",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_SLEEPYZ)),
            }));
            dialogues.Add(new ExampleDialogueGroup(Dialogues.GREETING, new ExampleDialogue[]
            {
                new ExampleDialogue((companion, parameters) => "Hi!!!!!!!"),
                new ExampleDialogue((companion, parameters) => "Hello!!"),
                new ExampleDialogue((companion, parameters) => $"Hey {CoreConfig.Instance.DisplayName.Value}!"),
                new ExampleDialogue((companion, parameters) => $"Hiya {CoreConfig.Instance.DisplayName.Value}!!"),
            }));
            dialogues.Add(new ExampleDialogueGroup(Dialogues.LOVE, new ExampleDialogue[]
            {
                new ExampleDialogue((companion, parameters) => "Awww..."),
                new ExampleDialogue((companion, parameters) => "Thank you!"),
                new ExampleDialogue((companion, parameters) => $"Thank you, {CoreConfig.Instance.DisplayName.Value}!!!"),
                new ExampleDialogue((companion, parameters) => $"Thank you, {CoreConfig.Instance.DisplayName.Value}!!! That means a lot to me...",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.IS_SAD)),
            }));
            dialogues.Add(new ExampleDialogueGroup(Dialogues.HATE, new ExampleDialogue[]
            {
                new ExampleDialogue((companion, parameters) => "..."),
                new ExampleDialogue((companion, parameters) => "man"),
            }));
            dialogues.Add(new ExampleDialogueGroup(Dialogues.LOADED_NEW_LEVEL, new ExampleDialogue[]
            {
                new ExampleDialogue((companion, parameters) => "Ooh, it's a new level!?"),
                new ExampleDialogue((companion, parameters) => "What's this level gonna be?"),

                // happy
                new ExampleDialogue((companion, parameters) => "Yay, a new level!",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.IS_HAPPY)),
                new ExampleDialogue((companion, parameters) => "Hello new level!!",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.IS_HAPPY)),
                new ExampleDialogue((companion, parameters) => "I'm excited to see what this level is going to be!",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.IS_HAPPY)),

                // sad
                new ExampleDialogue((companion, parameters) => "Oh, it's a new level.",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.IS_SAD)),
                new ExampleDialogue((companion, parameters) => "Oh, it's a new level...",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.IS_SAD)),
            }));
            dialogues.Add(new ExampleDialogueGroup(Dialogues.LOADED_LEVEL, new ExampleDialogue[]
            {
                new ExampleDialogue((companion, parameters) => "What are you building today?"),

                // happy
                new ExampleDialogue((companion, parameters) => "Level has loaded! Have fun building.",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.IS_HAPPY)),
                new ExampleDialogue((companion, parameters) => "Have fun!",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.IS_HAPPY)),
                new ExampleDialogue((companion, parameters) => "Level has loaded! I hope you enjoy the building process.",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.IS_HAPPY)),
                new ExampleDialogue((companion, parameters) => "I hope you enjoy the building process!",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.IS_HAPPY)),

                // sad
                new ExampleDialogue((companion, parameters) => "Oh, it's a level.",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.IS_SAD)),
            }));
            dialogues.Add(new ExampleDialogueGroup(Dialogues.CREATE_OBJECT, new ExampleDialogue[]
            {
                new ExampleDialogue((companion, parameters) => "Ooh, a new object! What will this become?"),
                new ExampleDialogue((companion, parameters) => "What's that gonna be?"),
                new ExampleDialogue((companion, parameters) => "Hello new object!"),
                new ExampleDialogue((companion, parameters) => "What will this object be for?"),
                new ExampleDialogue((companion, parameters) => "Hey, it's a new object!"),
                new ExampleDialogue((companion, parameters) => $"What are you gonna use this for, {CoreConfig.Instance.DisplayName.Value}?"),
                new ExampleDialogue((companion, parameters) => "Look at that new object, isn't it cute?"),
                new ExampleDialogue((companion, parameters) => "oh god not another object",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_TORI)),
            }));
            dialogues.Add(new ExampleDialogueGroup(Dialogues.OCCASIONAL, new ExampleDialogue[]
            {
                new ExampleDialogue((companion, parameters) => "WHERE ARE MY FILES?!",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.NO_ASSETS)),
                new ExampleDialogue((companion, parameters) =>
                {
                    var index = UnityEngine.Random.Range(0, ProjectPlanner.inst.todos.Count);
                    while (ProjectPlanner.inst.todos[index].Checked)
                        index = UnityEngine.Random.Range(0, ProjectPlanner.inst.todos.Count);

                    var todo = ProjectPlanner.inst.todos[index];

                    return $"Have you done the \"{todo.Text}\" yet?";
                },
                    parameters => ExampleConfig.Instance.CanRemindTODO.Value && RandomHelper.PercentChance(ExampleConfig.Instance.RemindRarity.Value) && ProjectPlanner.inst && ProjectPlanner.inst.todos.Has(x => !x.Checked)),
                new ExampleDialogue((companion, parameters) => "Seems like you have no levels... maybe you should make one?",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.NO_EDITOR_LEVELS)),
                new ExampleDialogue((companion, parameters) => "Maybe you should make something...?",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.NO_EDITOR_LEVELS)),
                new ExampleDialogue((companion, parameters) => "Uh oh... I hope your computer isn't crashing...",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.OBJECTS_ALIVE_COUNT_HIGH)),
                new ExampleDialogue((companion, parameters) => "Hmmm...",
                    parameters => reference.brain.interactedTimer.time > 600f),
                new ExampleDialogue((companion, parameters) => "What's up?"),
                new ExampleDialogue((companion, parameters) => "You got this!",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.IS_HAPPY)),
                new ExampleDialogue((companion, parameters) => "How are you doing so far?"),
                new ExampleDialogue((companion, parameters) => "How are you doing so far? I hope you're doing okay."),
                new ExampleDialogue((companion, parameters) => "How are you going so far?"),
                new ExampleDialogue((companion, parameters) => "How are you going so far? I hope you're doing okay."),
                new ExampleDialogue((companion, parameters) => $"Hey, {CoreConfig.Instance.DisplayName.Value}"),
                new ExampleDialogue((companion, parameters) => "Hey! You should probably have a break...",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.TIME_LONGER_THAN_10_HOURS)),
                new ExampleDialogue((companion, parameters) => "Hey! You should touch grass.",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.TIME_LONGER_THAN_10_HOURS)),
                new ExampleDialogue((companion, parameters) => "Jeez.",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.TIME_LONGER_THAN_10_HOURS)),
                new ExampleDialogue((companion, parameters) => "*Caw caw*",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_TORI)),
                new ExampleDialogue((companion, parameters) => "CrowBirb moment",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_TORI)),
                new ExampleDialogue((companion, parameters) => "A four dimensional tesseract.",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_CUBECUBE)),
                new ExampleDialogue((companion, parameters) => "The snail is coming.",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_CUBECUBE) && reference.interactions.Check(ExampleInteractions.Checks.IS_SAD)),
                new ExampleDialogue((companion, parameters) => "He will take your Z and W again.",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_CUBECUBE) && reference.interactions.Check(ExampleInteractions.Checks.IS_SAD)),
                new ExampleDialogue((companion, parameters) => "Snufusnargan",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_DIGGY)),
                new ExampleDialogue((companion, parameters) => "What the heck, its MoNsTeR?",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_MONSTER)),
                new ExampleDialogue((companion, parameters) => "Hey Mecha! Keep going, you got this!!",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_RTMECHA)),
                new ExampleDialogue((companion, parameters) => "Please don't break anything...",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_SLEEPYZ)),
                new ExampleDialogue((companion, parameters) => "In parkour ci-",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_APPY)),
                new ExampleDialogue((companion, parameters) => "We do a bit of trolling",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_APPY)),
                new ExampleDialogue((companion, parameters) => "a",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_APPY)),
                new ExampleDialogue((companion, parameters) => "Hey, you might want to set a username. Set it via pressing the ConfigManager toggle key ({CoreConfig.Instance.OpenConfigKey.Value}), going to the User section of the Core tab and then changing the name there.",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_DEFAULT)),
                new ExampleDialogue((companion, parameters) => $"Where's your username? Set it via pressing the ConfigManager toggle key ({CoreConfig.Instance.OpenConfigKey.Value}), going to the User section of the Core tab and then changing the name there.",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_DEFAULT)),
                new ExampleDialogue((companion, parameters) => "Maybe you should open a level?",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.HAS_NOT_LOADED_LEVEL)),
                new ExampleDialogue((companion, parameters) => "I wonder what these levels are like...",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.HAS_NOT_LOADED_LEVEL)),
                new ExampleDialogue((companion, parameters) => "What are you waiting for?",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.HAS_NOT_LOADED_LEVEL)),
                new ExampleDialogue((companion, parameters) => "Hello...?",
                    parameters => !reference.interactions.Check(ExampleInteractions.Checks.APPLICATION_FOCUSED)),
                new ExampleDialogue((companion, parameters) => "Are you there?",
                    parameters => !reference.interactions.Check(ExampleInteractions.Checks.APPLICATION_FOCUSED)),
                new ExampleDialogue((companion, parameters) => "Uhhh...",
                    parameters => !reference.interactions.Check(ExampleInteractions.Checks.APPLICATION_FOCUSED)),
                new ExampleDialogue((companion, parameters) => "Where'd you go?",
                    parameters => !reference.interactions.Check(ExampleInteractions.Checks.APPLICATION_FOCUSED)),
                new ExampleDialogue((companion, parameters) => "Welp.", parameters =>
                    {
                        if (reference.interactions.Check(ExampleInteractions.Checks.IS_HAPPY))
                            return false;

                        var attribute = reference.brain.GetAttribute("SEEN_WELP_DIALOGUE");
                        if (attribute.Value == 0.0)
                        {
                            reference.brain.SetAttribute("SEEN_WELP_DIALOGUE", 1.0, MathOperation.Set);
                            return true;
                        }
                        return false;
                    }),
            }));
            dialogues.Add(new ExampleDialogueGroup(Dialogues.RANDOM_IDEA, new ExampleDialogue[]
            {
                new ExampleDialogue((companion, parameters) => "Make a bomb!",
                    parameters => parameters is IdeaDialogueParameters ideaParameters && ideaParameters.ideaContext == IdeaDialogueParameters.IdeaContext.Random),
                new ExampleDialogue((companion, parameters) => "Make a bullet!",
                    parameters => parameters is IdeaDialogueParameters ideaParameters && ideaParameters.ideaContext == IdeaDialogueParameters.IdeaContext.Random),
                new ExampleDialogue((companion, parameters) => "Make a beam!",
                    parameters => parameters is IdeaDialogueParameters ideaParameters && ideaParameters.ideaContext == IdeaDialogueParameters.IdeaContext.Random),
                new ExampleDialogue((companion, parameters) => "Make a pulse!",
                    parameters => parameters is IdeaDialogueParameters ideaParameters && ideaParameters.ideaContext == IdeaDialogueParameters.IdeaContext.Random),
                new ExampleDialogue((companion, parameters) => "Make a character!",
                    parameters => parameters is IdeaDialogueParameters ideaParameters && ideaParameters.ideaContext == IdeaDialogueParameters.IdeaContext.Random),
                new ExampleDialogue((companion, parameters) => "Make a background!",
                    parameters => parameters is IdeaDialogueParameters ideaParameters && ideaParameters.ideaContext == IdeaDialogueParameters.IdeaContext.Random),
                new ExampleDialogue((companion, parameters) => "Make a circle character!",
                    parameters => parameters is IdeaDialogueParameters ideaParameters && ideaParameters.ideaContext == IdeaDialogueParameters.IdeaContext.Character),
                new ExampleDialogue((companion, parameters) => "Make a square character!",
                    parameters => parameters is IdeaDialogueParameters ideaParameters && ideaParameters.ideaContext == IdeaDialogueParameters.IdeaContext.Character),
            }));
            dialogues.Add(new ExampleDialogueGroup(Dialogues.END_LEVEL_SCREEN, new ExampleDialogue[]
            {
                new ExampleDialogue((companion, parameters) =>
                {
                    if (parameters is LevelDialogueParameters levelParameters)
                    {
                        switch (levelParameters.rank)
                        {
                            case Rank.Null: {
                                    return $"That level was awesome!";
                                }
                            case Rank.SS: {
                                    return $"Woah!!! Great job, {CoreConfig.Instance.DisplayName.Value}!!";
                                }
                            case Rank.S: {
                                    return $"Woah!! Great job, {CoreConfig.Instance.DisplayName.Value}!";
                                }
                            case Rank.A: {
                                    return $"Good job, {CoreConfig.Instance.DisplayName.Value}!";
                                }
                            case Rank.B: {
                                    return $"Good job, {CoreConfig.Instance.DisplayName.Value}.";
                                }
                            case Rank.C: {
                                    return $"You can do better, {CoreConfig.Instance.DisplayName.Value}!";
                                }
                            case Rank.D: {
                                    return $"You can do better, {CoreConfig.Instance.DisplayName.Value}.";
                                }
                            case Rank.F: {
                                    return $"Aww... I know you can do better!";
                                }
                        }
                    }

                    return NULL_DIALOGUE;
                }, parameters => parameters is LevelDialogueParameters),
            }));
            dialogues.Add(new ExampleDialogueGroup(Dialogues.EDITOR_SAVED_LEVEL, new ExampleDialogue[]
            {
                new ExampleDialogue((companion, parameters) => "Your level was saved!"),
                new ExampleDialogue((companion, parameters) => "Your level has been saved!"),
                new ExampleDialogue((companion, parameters) => "The level was saved!"),
                new ExampleDialogue((companion, parameters) => "The level has been saved!"),
                new ExampleDialogue((companion, parameters) => "The current level was saved!"),
                new ExampleDialogue((companion, parameters) => "The current level has been saved!"),
                new ExampleDialogue((companion, parameters) => "oh thank goodness it saved....",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_TORI)),
                new ExampleDialogue((companion, parameters) => "I hope the file didn't break...",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_SLEEPYZ)),
                new ExampleDialogue((companion, parameters) => "That's a lot of objects to save...!",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.OBJECTS_ALIVE_COUNT_HIGH)),
            }));
            dialogues.Add(new ExampleDialogueGroup(Dialogues.EDITOR_AUTOSAVED, new ExampleDialogue[]
            {
                new ExampleDialogue((companion, parameters) => "I saved this level for you."),
                new ExampleDialogue((companion, parameters) => "I saved this level for you!"),
                new ExampleDialogue((companion, parameters) => "I saved this level for you just in case your game crashes...",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.TIME_LONGER_THAN_10_HOURS)),
                new ExampleDialogue((companion, parameters) => "oh thank goodness it saved....",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_TORI)),
                new ExampleDialogue((companion, parameters) => "I hope the file didn't break...",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_SLEEPYZ)),
                new ExampleDialogue((companion, parameters) => "That's a lot of objects to save...!",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.OBJECTS_ALIVE_COUNT_HIGH)),
            }));
            dialogues.Add(new ExampleDialogueGroup(Dialogues.PLAYER_HIT, new ExampleDialogue[]
            {
                new ExampleDialogue((companion, parameters) => "Ouch."),
                new ExampleDialogue((companion, parameters) => "Oh no.."),
            }));
            dialogues.Add(new ExampleDialogueGroup(Dialogues.PLAYER_DEATH, new ExampleDialogue[]
            {
                new ExampleDialogue((companion, parameters) => "lol you died",
                    parameters => RandomHelper.PercentChance(1) && reference.interactions.Check(ExampleInteractions.Checks.IS_SAD)),
                new ExampleDialogue((companion, parameters) => "death hd",
                    parameters => reference.interactions.Check(ExampleInteractions.Checks.USER_IS_DIGGY)),
                new ExampleDialogue((companion, parameters) =>
                {
                    if (parameters is PlayerDialogueParameters playerParameters && playerParameters.player)
                        return $"Nooo player {playerParameters.player.index + 1}!!!";

                    return NULL_DIALOGUE;
                }, parameters => parameters is PlayerDialogueParameters),
            }));
        }

        /// <summary>
        /// Overrides an existing dialogue.
        /// </summary>
        /// <param name="key">Key of the dialogue group to override.</param>
        /// <param name="dialogueGroup">Dialogue group to override.</param>
        public void OverrideDialogue(string key, ExampleDialogueGroup dialogueGroup)
        {
            if (dialogues.TryFindIndex(x => x.key == key, out int index))
                dialogues[index] = dialogueGroup;
        }

        /// <summary>
        /// Finds a dialogue and makes Example say it.
        /// </summary>
        /// <param name="key">Key of the dialogue group.</param>
        /// <param name="parameters">Dialogue parameters to pass.</param>
        public void SayDialogue(string key, DialogueParameters parameters = null)
        {
            var dialogueGroup = dialogues.Find(x => x.key == key);
            if (!dialogueGroup)
                return;

            var dialogue = dialogueGroup.GetDialogue(parameters);
            if (dialogue)
                Say(dialogue, parameters);
        }

        /// <summary>
        /// Makes Example say something.
        /// </summary>
        /// <param name="dialogue">Dialogue for Example to say.</param>
        /// <param name="parameters">Dialogue parameters.</param>
        public void Say(ExampleDialogue dialogue, DialogueParameters parameters = null)
        {
            if (!parameters)
                parameters = new DialogueParameters(UnityEngine.Random.Range(0, dialogue.dialogueCount));

            var text = dialogue?.get?.Invoke(Example.Current, parameters);

            if (!reference || !reference.brain)
            {
                Debug.Log($"Uh oh... looks like either Example is dead or he's brain dead.........");
                return;
            }

            reference.brain.talking = true;

            text = text.Replace("{{Username}}", CoreConfig.Instance.DisplayName.Value);

            RTString.RegexMatches(text, new Regex(@"{{Config_(.*?)_(.*?)}}"), match =>
            {
                var configName = match.Groups[1].ToString();
                var config = LegacyPlugin.configs.Find(x => x.Name == configName);
                var setting = config.Settings.Find(x => x.Key == match.Groups[2].ToString());

                text = text.Replace(match.Groups[0].ToString(), setting.BoxedValue.ToString());
            });

            if (currentChatAnimation)
                CompanionManager.inst.animationController.Remove(currentChatAnimation.id);

            if (!ExampleConfig.Instance.CanSpeak.Value)
            {
                dialogueBase.transform.localScale = new Vector3(0f, 0f, 1f);
                return;
            }

            CoreHelper.Log($"Example says: {text}");

            currentChatAnimation = new RTAnimation($"DIALOGUE: {text}");

            var ogMouth = reference.model.mouthOpenAmount;

            var keyframes = new List<IKeyframe<float>>();
            keyframes.Add(new FloatKeyframe(0f, 0.5f, Ease.Linear));

            float mouthTime = 0.1f;
            var totalLength = parameters.textLength * parameters.time * 10;
            for (int i = 0; i < (int)totalLength / 2; i++)
            {
                keyframes.Add(new FloatKeyframe(mouthTime * parameters.time, ogMouth * 1.85f, Ease.SineOut));
                mouthTime += 0.1f;
                keyframes.Add(new FloatKeyframe(mouthTime * parameters.time, 0.5f, Ease.SineIn));
                mouthTime += 0.1f;
            }

            int prevLetterNum = 0;

            currentChatAnimation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 90f, Ease.Linear),
                    new FloatKeyframe(1.5f * parameters.time, 0f, Ease.ElasticOut),
                }, dialogueBase.SetLocalRotationEulerZ, interpolateOnComplete: true),
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 1f, Ease.Linear),
                    new FloatKeyframe(parameters.textLength * parameters.time, text.Length, Ease.SineOut),
                }, x =>
                {
                    if (prevLetterNum != (int)x)
                    {
                        prevLetterNum = (int)x;
                        SoundManager.inst.PlaySound(reference.model.baseCanvas, DefaultSounds.example_speak, UnityEngine.Random.Range(0.2f, 0.3f), UnityEngine.Random.Range(0.97f, 1.03f));
                    }

                    try
                    {
                        dialogueText.text = text.Substring(0, (int)x + 1);
                    }
                    catch
                    {
                        dialogueText.text = text.Substring(0, (int)x);
                    }
                }, interpolateOnComplete: true),
                new AnimationHandler<float>(keyframes, x =>
                {
                    if (reference && reference.model)
                        reference.model.mouthOpenAmount = Mathf.Clamp(0f, x, 1.5f);
                }, interpolateOnComplete: true),

                // Scale dialogue
                new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                {
                    new Vector2Keyframe(0f, Vector2.zero, Ease.Linear),
                    new Vector2Keyframe(0.2f * parameters.time, new Vector2(1.1f, 1.1f), Ease.SineOut),
                    new Vector2Keyframe(0.8f * parameters.time, Vector2.one, Ease.SineInOut),
                    new Vector2Keyframe(parameters.stayTime * parameters.time, Vector2.one, Ease.Linear),
                    new Vector2Keyframe((parameters.stayTime + 0.3f) * parameters.time, Vector2.zero, Ease.BackIn),
                    new Vector2Keyframe((parameters.stayTime + 0.6f) * parameters.time, Vector2.zero, Ease.Linear),
                }, SetScale, interpolateOnComplete: true),
            };

            currentChatAnimation.onComplete = () =>
            {
                CompanionManager.inst.animationController.Remove(currentChatAnimation.id);
                currentChatAnimation = null;
                if (reference && reference.brain)
                    reference.brain.talking = false;

                parameters.onComplete?.Invoke();
            };

            CompanionManager.inst.animationController.Play(currentChatAnimation);

        }

        /// <summary>
        /// Library of default dialogues.
        /// </summary>
        public static class Dialogues
        {
            /// <summary>
            /// When Example spawns.
            /// </summary>
            public const string SPAWN = "Spawn";

            /// <summary>
            /// Say hi!
            /// </summary>
            public const string GREETING = "Greeting";

            /// <summary>
            /// Do you like Example?
            /// </summary>
            public const string LOVE = "Love";

            /// <summary>
            /// Do you dislike Example?
            /// </summary>
            public const string HATE = "Hate";

            /// <summary>
            /// Loaded a new editor level.
            /// </summary>
            public const string LOADED_NEW_LEVEL = "Loaded New Level";

            /// <summary>
            /// Loaded an editor level.
            /// </summary>
            public const string LOADED_LEVEL = "Loaded Level";

            /// <summary>
            /// Created an object.
            /// </summary>
            public const string CREATE_OBJECT = "Create Object";

            /// <summary>
            /// Something Example says every now and then.
            /// </summary>
            public const string OCCASIONAL = "Occasional";

            /// <summary>
            /// Example gives a random idea.
            /// </summary>
            public const string RANDOM_IDEA = "Random Idea";

            /// <summary>
            /// Example comments on your end level result.
            /// </summary>
            public const string END_LEVEL_SCREEN = "End Level Screen";

            /// <summary>
            /// Example says something about the editor saving.
            /// </summary>
            public const string EDITOR_SAVED_LEVEL = "Editor Saved Level";

            /// <summary>
            /// Example says something about the autosave.
            /// </summary>
            public const string EDITOR_AUTOSAVED = "Editor Autosaved";

            /// <summary>
            /// Example says something about a player getting hit.
            /// </summary>
            public const string PLAYER_HIT = "Player Hit";

            /// <summary>
            /// Example says something about a player dying.
            /// </summary>
            public const string PLAYER_DEATH = "Player Death";
        }

        RTAnimation currentChatAnimation;

        public const string NULL_DIALOGUE = "I have nothing to say.";

        #endregion

        #region UI

        void SetScale(Vector2 scale)
        {
            if (dialogueBase)
                dialogueBase.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        }

        public RectTransform dialogueBase;
        public Image dialogueImage;
        public Text dialogueText;

        #endregion
    }
}
