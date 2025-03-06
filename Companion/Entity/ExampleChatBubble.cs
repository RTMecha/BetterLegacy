using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Managers;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Companion.Data;

namespace BetterLegacy.Companion.Entity
{
    /// <summary>
    /// Represents Example's chat bubble. This is how he talks.
    /// </summary>
    public class ExampleChatBubble : ExampleModule
    {
        #region Default Instance

        /// <summary>
        /// The default chat bubble.
        /// </summary>
        public static ExampleChatBubble Default
        {
            get
            {
                var chatBubble = new ExampleChatBubble();
                chatBubble.InitDefault();
                return chatBubble;
            }
        }

        public override void InitDefault()
        {

        }

        #endregion

        #region Chatting

        /// <summary>
        /// Makes Example say something.
        /// </summary>
        /// <param name="dialogue">Dialogue for Example to say.</param>
        /// <param name="textLength">Length of the text.</param>
        /// <param name="stayTime">Time the chat bubble should stay for.</param>
        /// <param name="time">Speed of the chat bubble animation.</param>
        public void Say(string dialogue, float textLength = 1.5f, float stayTime = 4f, float time = 0.7f, Action onComplete = null)
        {
            if (!reference || !reference.brain)
            {
                Debug.Log($"Uh oh... looks like either Example is dead or he's brain dead.........");
                return;
            }

            reference.brain.talking = true;

            dialogue = dialogue.Replace("{{Username}}", CoreConfig.Instance.DisplayName.Value);

            RTString.RegexMatches(dialogue, new Regex(@"{{Config_(.*?)_(.*?)}}"), match =>
            {
                var configName = match.Groups[1].ToString();
                var config = LegacyPlugin.configs.Find(x => x.Name == configName);
                var setting = config.Settings.Find(x => x.Key == match.Groups[2].ToString());

                dialogue = dialogue.Replace(match.Groups[0].ToString(), setting.BoxedValue.ToString());
            });

            if (currentChatAnimation)
                CompanionManager.inst.animationController.Remove(currentChatAnimation.id);

            if (!ExampleConfig.Instance.CanSpeak.Value)
            {
                dialogueBase.transform.localScale = new Vector3(0f, 0f, 1f);
                return;
            }

            CoreHelper.Log($"Example says: {dialogue}");

            currentChatAnimation = new RTAnimation($"DIALOGUE: {dialogue}");

            var ogMouth = reference.model.mouthOpenAmount;

            var keyframes = new List<IKeyframe<float>>();
            keyframes.Add(new FloatKeyframe(0f, 0.5f, Ease.Linear));

            float mouthTime = 0.1f;
            var totalLength = textLength * time * 10;
            for (int i = 0; i < (int)totalLength / 2; i++)
            {
                keyframes.Add(new FloatKeyframe(mouthTime * time, ogMouth * 1.85f, Ease.SineOut));
                mouthTime += 0.1f;
                keyframes.Add(new FloatKeyframe(mouthTime * time, 0.5f, Ease.SineIn));
                mouthTime += 0.1f;
            }

            int prevLetterNum = 0;

            currentChatAnimation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 90f, Ease.Linear),
                    new FloatKeyframe(1.5f * time, 0f, Ease.ElasticOut),
                }, dialogueBase.SetLocalRotationEulerZ, interpolateOnComplete: true),
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 1f, Ease.Linear),
                    new FloatKeyframe(textLength * time, dialogue.Length, Ease.SineOut),
                }, x =>
                {
                    if (prevLetterNum != (int)x)
                    {
                        prevLetterNum = (int)x;
                        SoundManager.inst.PlaySound(reference.model.baseCanvas, DefaultSounds.example_speak, UnityEngine.Random.Range(0.2f, 0.3f), UnityEngine.Random.Range(0.97f, 1.03f));
                    }

                    try
                    {
                        dialogueText.text = dialogue.Substring(0, (int)x + 1);
                    }
                    catch
                    {
                        dialogueText.text = dialogue.Substring(0, (int)x);
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
                    new Vector2Keyframe(0.2f * time, new Vector2(1.1f, 1.1f), Ease.SineOut),
                    new Vector2Keyframe(0.8f * time, Vector2.one, Ease.SineInOut),
                    new Vector2Keyframe(stayTime * time, Vector2.one, Ease.Linear),
                    new Vector2Keyframe((stayTime + 0.3f) * time, Vector2.zero, Ease.BackIn),
                    new Vector2Keyframe((stayTime + 0.6f) * time, Vector2.zero, Ease.Linear),
                }, SetScale, interpolateOnComplete: true),
            };

            currentChatAnimation.onComplete = () =>
            {
                CompanionManager.inst.animationController.Remove(currentChatAnimation.id);
                currentChatAnimation = null;
                if (reference && reference.brain)
                    reference.brain.talking = false;

                onComplete?.Invoke();
            };

            CompanionManager.inst.animationController.Play(currentChatAnimation);
        }

        RTAnimation currentChatAnimation;

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

            dialogueFunctions = new Func<bool>[]
            {
                SayAnyways, // 0
                HasLoadedLevel, // 1
                CanSay, // 2
                ApplicationNotFocused, // 3
                HasNotLoadedLevel, // 4
                LeftHandBeingDragged, // 5
                RightHandBeingDragged, // 6
                BeingDragged, // 7
                UserIsSleepyz, // 8
                UserIsMecha, // 9
                UserIsDiggy, // 10
                UserIsCubeCube, // 11
                UserIsTori, // 12
                UserIsPlayer, // 13
                TimeLongerThan10Hours, // 14
                ObjectsAliveCountHigherThan900, // 15
                LevelCountIsZero, // 16
                UserIsMoNsTeR, // 17
            };

            LoadDialogue();
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
        /// Path to Example's dialogue file.
        /// </summary>
        public virtual string DialoguePath => RTFile.GetAsset($"Example Parts/dialogue{FileFormat.JSON.Dot()}");

        public string lastDialogue;

        public Dictionary<string, DialogueGroup> dialogueDictionary = new Dictionary<string, DialogueGroup>();

        public bool saidDialogue = false;
        public bool canSay = false;

        public void Sleep()
        {
            var eyes = reference.model.GetPart("EYES");
            var allowBlinkingAttribute = reference.model.GetAttribute("ALLOW_BLINKING");
            var originalValue = allowBlinkingAttribute.Value;
            allowBlinkingAttribute.Value = 0.0;
            var animation = new RTAnimation("Sleepy");
            animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, 1f, Ease.Linear),
                        new FloatKeyframe(0.5f, 0.3f, Ease.SineInOut),
                        new FloatKeyframe(0.55f, 0f, Ease.SineIn),
                        new FloatKeyframe(0.6f, 0.3f, Ease.SineOut),
                        new FloatKeyframe(0.65f, 0f, Ease.SineIn),
                        new FloatKeyframe(0.7f, 0.3f, Ease.SineOut),
                        new FloatKeyframe(1f, 1f, Ease.SineInOut),
                        new FloatKeyframe(1.1f, 1f, Ease.Linear),
                    }, x => eyes.transform.localScale = new Vector3(1f, x, 1f)),
                };

            animation.onComplete = () =>
            {
                allowBlinkingAttribute.Value = originalValue;
                CompanionManager.inst.animationController.Remove(animation.id);
            };

            CompanionManager.inst.animationController.Play(animation);
        }

        public void SayDialogue(string dialogueName)
        {
            if (!dialogueDictionary.TryGetValue(dialogueName, out DialogueGroup dialogueGroup))
                return;

            var dialogues = dialogueGroup.dialogues;
            var random = UnityEngine.Random.Range(0, dialogues.Length);

            while (!dialogues[random].CanSay)
            {
                if (reference.brain.dancing)
                    break;
                random = UnityEngine.Random.Range(0, dialogues.Length);
            }

            if (reference.brain.dancing)
                return;

            Say(dialogues[random].text);
            dialogues[random].Action();
        }

        void LoadDialogue()
        {
            dialogueDictionary.Clear();
            var jn = JSON.Parse(RTFile.ReadFromFile(DialoguePath));

            for (int i = 0; i < jn["dialogue_groups"].Count; i++)
            {
                var dialogueGroup = jn["dialogue_groups"][i];

                var dialogues = new ExampleDialogue[dialogueGroup["dialogue"].Count];
                for (int j = 0; j < dialogueGroup["dialogue"].Count; j++)
                {
                    var dialogue = dialogueGroup["dialogue"][j];
                    int dialogueFunction = 0;
                    if (dialogue["func_index"] != null)
                        dialogueFunction = dialogue["func_index"].AsInt;

                    Func<bool> dialogueFunc =
                        dialogue["func"] != null ?
                        () =>
                        {
                            try
                            {
                                var evaluation = RTCode.EvaluateWithReturn(dialogue["func"]);
                                return evaluation is bool boolean && boolean;
                            }
                            catch
                            {
                                return false;
                            }
                        }
                    : dialogueFunctions[dialogueFunction];

                    Action action = dialogue["action"] != null ? RTCode.ConvertToAction(dialogue["action"]) : null;

                    dialogues[j] = new ExampleDialogue(dialogue["text"], dialogueFunc, action);
                }

                dialogueDictionary[dialogueGroup["name"]] = new DialogueGroup(dialogueGroup["name"], dialogues);
            }
        }

        public bool CanSay() => canSay;

        public bool ApplicationNotFocused() => !Application.isFocused;

        public bool HasNotLoadedLevel() => EditorManager.inst && !EditorManager.inst.hasLoadedLevel && RTEditor.inst.LevelPanels.Count > 0;

        public bool HasLoadedLevel() => EditorManager.inst && EditorManager.inst.hasLoadedLevel;

        public bool LeftHandBeingDragged() => reference.draggingLeftHand;

        public bool RightHandBeingDragged() => reference.draggingRightHand;

        public bool BeingDragged() => reference.dragging;

        public bool UserIsSleepyz() => CoreConfig.Instance.DisplayName.Value.ToLower() == "sleepyz" || CoreConfig.Instance.DisplayName.Value.ToLower() == "sleepyzgamer";

        public bool UserIsMecha() => CoreConfig.Instance.DisplayName.Value == "RTMecha";

        public bool UserIsDiggy() =>
            CoreConfig.Instance.DisplayName.Value.Remove(" ").ToLower() == "diggy" ||
            CoreConfig.Instance.DisplayName.Value.Remove(" ").ToLower() == "diggydog" ||
            CoreConfig.Instance.DisplayName.Value.Remove(" ").ToLower() == "diggydog176";

        public bool UserIsCubeCube() =>
            CoreConfig.Instance.DisplayName.Value.Remove(" ").ToLower() == "cubecube";

        public bool UserIsTori() => CoreConfig.Instance.DisplayName.Value.Remove(" ").ToLower() == "karasutori";
        public bool UserIsPlayer() => CoreConfig.Instance.DisplayName.Value.Remove(" ").ToLower() == "player";

        public bool TimeLongerThan10Hours() => Time.time > 36000f;

        public bool SayAnyways() => true;

        public bool ObjectsAliveCountHigherThan900() => GameData.IsValid && GameData.Current.beatmapObjects.FindAll(x => x.Alive).Count > 900;

        public bool LevelCountIsZero() => CoreHelper.InEditor && RTEditor.inst.LevelPanels.Count <= 0;

        public bool UserIsMoNsTeR() => CoreConfig.Instance.DisplayName.Value.Remove(" ").ToLower() == "monster";

        public Func<bool>[] dialogueFunctions;

        #endregion

        #region UI

        public void SetScale(Vector2 scale)
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
