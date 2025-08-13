using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Popups;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Editor class that manages custom animations.
    /// </summary>
    public class AnimationEditor : MonoBehaviour
    {
        /*
         TOOD:
        - Global animation library
         */

        #region Init

        /// <summary>
        /// The <see cref="AnimationEditor"/> global instance reference.
        /// </summary>
        public static AnimationEditor inst;

        /// <summary>
        /// Initializes <see cref="AnimationEditor"/>.
        /// </summary>
        public static void Init() => Creator.NewGameObject(nameof(AnimationEditor), EditorManager.inst.transform.parent).AddComponent<AnimationEditor>();

        void Awake()
        {
            inst = this;

            try
            {
                Dialog = new AnimationEditorDialog();
                Dialog.Init();
                Dialog.GameObject.AddComponent<ActiveState>().onStateChanged = enabled =>
                {
                    if (!enabled)
                    {
                        CurrentObject = null;
                        CurrentAnimation = null;
                    }    
                };

                Popup = RTEditor.inst.GeneratePopup(EditorPopup.ANIMATIONS_POPUP, "Animations", Vector2.zero, new Vector2(600f, 400f));

                EditorHelper.AddEditorDropdown("View Animations", string.Empty, EditorHelper.VIEW_DROPDOWN, EditorSprites.PlaySprite, () =>
                {
                    if (!EditorManager.inst.hasLoadedLevel)
                    {
                        EditorManager.inst.DisplayNotification($"Load a level first!", 2f, EditorManager.NotificationType.Warning);
                        return;
                    }

                    OpenPopup(GameData.Current.animations);
                });
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        void Update()
        {
            if (CurrentObject == null || !Dialog.IsCurrent || !CurrentAnimation)
                return;

            var t = Dialog.Timeline.Cursor.value;
            var allEvents = CurrentAnimation.Events;
            for (int i = 0; i < 3; i++)
            {
                if (i >= allEvents.Count)
                    break;

                var events = CurrentAnimation.GetEventKeyframes(i);
                if (events.IsEmpty())
                    continue;

                if (i == 2)
                {
                    CurrentObject.SetTransform(i, 2, CurrentAnimation.Interpolate(i, 0, t));
                    continue;
                }

                for (int j = 0; j < events[0].values.Length; j++)
                    CurrentObject.SetTransform(i, j, CurrentAnimation.Interpolate(i, j, t));
            }
        }

        #endregion

        #region Values

        public AnimationEditorDialog Dialog { get; set; }

        public ContentPopup Popup { get; set; }

        public PAAnimation CurrentAnimation { get; set; }

        public List<PAAnimation> copiedAnimations = new List<PAAnimation>();

        public ITransformable CurrentObject { get; set; }

        #endregion

        #region Methods

        public void Test()
        {
            var boostAnimation = new PAAnimation("Test", "Test description");
            boostAnimation.ReferenceID = "boost";
            boostAnimation.positionKeyframes.Add(new EventKeyframe(0.3f, new float[] { 10f, 0f, 0f }, "OutSine"));
            boostAnimation.positionKeyframes.Add(new EventKeyframe(0.4f, new float[] { 0f, 0f, 0f }, "InSine"));

            var idleAnimation = new PAAnimation("Test", "Test description");
            idleAnimation.ReferenceID = "idle";
            idleAnimation.positionKeyframes.Add(new EventKeyframe(1f, new float[] { 3f, 0f, 0f }, "InOutSine"));
            idleAnimation.positionKeyframes.Add(new EventKeyframe(2f, new float[] { 0f, 0f, 0f }, "InOutSine"));

            if (PlayerManager.Players.TryGetAt(0, out PAPlayer player) && player.RuntimePlayer && player.PlayerModel && player.RuntimePlayer.Model.customObjects.TryGetAt(0, out CustomPlayerObject customObject))
            {
                customObject.animations.Clear();
                customObject.animations.Add(boostAnimation);
                customObject.animations.Add(idleAnimation);
                player.RuntimePlayer.UpdateModel();
            }
        }

        #region Editor

        public void OpenDialog(PAAnimation animation)
        {
            Dialog.Open();
            RenderDialog(animation);
        }

        public void RenderDialog(PAAnimation animation)
        {
            CurrentAnimation = animation;

            Dialog.IDText.text = $"ID: {animation.id}";
            var idClickable = Dialog.IDBase.gameObject.GetOrAddComponent<Clickable>();
            idClickable.onClick = pointerEventData =>
            {
                EditorManager.inst.DisplayNotification($"Copied ID from {animation.name}!", 2f, EditorManager.NotificationType.Success);
                LSText.CopyToClipboard(animation.id);
            };

            Dialog.ReferenceField.SetTextWithoutNotify(animation.ReferenceID);
            Dialog.ReferenceField.onValueChanged.NewListener(_val => animation.ReferenceID = _val);
            var referenceContextMenu = Dialog.ReferenceField.gameObject.GetOrAddComponent<ContextClickable>();
            referenceContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction(PlayerModel.IDLE_ANIM, () => Dialog.ReferenceField.text = PlayerModel.IDLE_ANIM),
                    new ButtonFunction(PlayerModel.BOOST_ANIM, () => Dialog.ReferenceField.text = PlayerModel.BOOST_ANIM),
                    new ButtonFunction(PlayerModel.HEAL_ANIM, () => Dialog.ReferenceField.text = PlayerModel.HEAL_ANIM),
                    new ButtonFunction(PlayerModel.HIT_ANIM, () => Dialog.ReferenceField.text = PlayerModel.HIT_ANIM),
                    new ButtonFunction(PlayerModel.DEATH_ANIM, () => Dialog.ReferenceField.text = PlayerModel.DEATH_ANIM),
                    new ButtonFunction(PlayerModel.SHOOT_ANIM, () => Dialog.ReferenceField.text = PlayerModel.SHOOT_ANIM),
                    new ButtonFunction(PlayerModel.JUMP_ANIM, () => Dialog.ReferenceField.text = PlayerModel.JUMP_ANIM)
                    );
            };

            Dialog.NameField.SetTextWithoutNotify(animation.name);
            Dialog.NameField.onValueChanged.NewListener(_val => animation.name = _val);
            Dialog.DescriptionField.SetTextWithoutNotify(animation.description);
            Dialog.DescriptionField.onValueChanged.NewListener(_val => animation.description = _val);

            Dialog.StartTimeField.SetTextWithoutNotify(animation.StartTime.ToString());
            Dialog.StartTimeField.OnValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    animation.StartTime = num;
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.StartTimeField, max: float.MaxValue);
            TriggerHelper.AddEventTriggers(Dialog.StartTimeField.gameObject, TriggerHelper.ScrollDelta(Dialog.StartTimeField.inputField, max: float.MaxValue));

            Dialog.AnimatePositionToggle.SetIsOnWithoutNotify(animation.animatePosition);
            Dialog.AnimatePositionToggle.onValueChanged.NewListener(_val => animation.animatePosition = _val);
            Dialog.AnimateScaleToggle.SetIsOnWithoutNotify(animation.animateScale);
            Dialog.AnimateScaleToggle.onValueChanged.NewListener(_val => animation.animateScale = _val);
            Dialog.AnimateRotationToggle.SetIsOnWithoutNotify(animation.animateRotation);
            Dialog.AnimateRotationToggle.onValueChanged.NewListener(_val => animation.animateRotation = _val);
            Dialog.AnimateColorToggle.SetIsOnWithoutNotify(animation.animateColor);
            Dialog.AnimateColorToggle.onValueChanged.NewListener(_val => animation.animateColor = _val);
            Dialog.TransitionToggle.SetIsOnWithoutNotify(animation.transition);
            Dialog.TransitionToggle.onValueChanged.NewListener(_val => animation.transition = _val);

            RenderCustomUIDisplay(animation);

            Dialog.Timeline.RenderKeyframes(animation);
            Dialog.Timeline.RenderDialog(animation);
            Dialog.Timeline.ResizeKeyframeTimeline(animation);
        }

        /// <summary>
        /// Renders the custom keyframe UI.
        /// </summary>
        public void RenderCustomUIDisplay(PAAnimation animation)
        {
            Dialog.keyframeDialogs[0].InitCustomUI(
                animation.EditorData.GetDisplay("position/x", CustomUIDisplay.DefaultPositionXDisplay),
                animation.EditorData.GetDisplay("position/y", CustomUIDisplay.DefaultPositionYDisplay),
                animation.EditorData.GetDisplay("position/z", CustomUIDisplay.DefaultPositionZDisplay));
            Dialog.keyframeDialogs[0].EventValueElements[2].GameObject.SetActive(RTEditor.ShowModdedUI);
            Dialog.keyframeDialogs[1].InitCustomUI(
                animation.EditorData.GetDisplay("scale/x", CustomUIDisplay.DefaultScaleXDisplay),
                animation.EditorData.GetDisplay("scale/y", CustomUIDisplay.DefaultScaleYDisplay));
            Dialog.keyframeDialogs[2].InitCustomUI(
                animation.EditorData.GetDisplay("rotation/x", CustomUIDisplay.DefaultRotationDisplay));
        }

        #endregion

        #region List

        /// <summary>
        /// Opens the animation list popup.
        /// </summary>
        /// <param name="animations">List of animations to display.</param>
        /// <param name="onPlay">Function to run when the user wants to play the animation.</param>
        public void OpenPopup(List<PAAnimation> animations, Action<PAAnimation> onPlay = null, Action<PAAnimation> onSelect = null, ITransformable currentObject = null)
        {
            Popup.Open();
            RenderPopup(animations, onPlay, onSelect, currentObject);
        }

        /// <summary>
        /// Renders the animation list popup.
        /// </summary>
        /// <param name="animations">List of animations to display.</param>
        /// <param name="onPlay">Function to run when the user wants to play the animation.</param>
        public void RenderPopup(List<PAAnimation> animations, Action<PAAnimation> onPlay = null, Action<PAAnimation> onSelect = null, ITransformable currentObject = null)
        {
            CurrentObject = currentObject;

            Popup.ClearContent();
            Popup.SearchField.onValueChanged.NewListener(_val => RenderPopup(animations, onPlay, onSelect, currentObject));

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(Popup.Content);
            add.transform.AsRT().sizeDelta = new Vector2(350f, 32f);
            var addText = add.transform.Find("Text").GetComponent<Text>();
            addText.text = "Add new Animation";
            var addButton = add.GetComponent<Button>();
            addButton.onClick.ClearAll();
            var addContextMenu = add.GetOrAddComponent<ContextClickable>();
            addContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                {
                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Create New", () =>
                        {
                            animations.Add(new PAAnimation("New Animation", "This is the default description!"));
                            RenderPopup(animations, onPlay, onSelect, currentObject);
                        }),
                        new ButtonFunction("Create From Object", () => EditorTimeline.inst.onSelectTimelineObject = timelineObject =>
                        {
                            if (!timelineObject.isBeatmapObject)
                            {
                                EditorManager.inst.DisplayNotification("Cannot apply animation from non-beatmap object.", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            var beatmapObject = timelineObject.GetData<BeatmapObject>();
                            var animation = new PAAnimation(beatmapObject.name, "This is the default description!");
                            animation.CopyAnimatableData(beatmapObject);
                            animations.Add(animation);
                            RenderPopup(animations, onPlay, onSelect, currentObject);
                        }),
                        new ButtonFunction(true),
                        new ButtonFunction("Copy All", () =>
                        {
                            copiedAnimations = new List<PAAnimation>(animations.Select(x => x.Copy()));
                            EditorManager.inst.DisplayNotification($"Copied all animations.", 2f, EditorManager.NotificationType.Success);
                        }),
                        new ButtonFunction("Paste", () =>
                        {
                            if (copiedAnimations.IsEmpty())
                            {
                                EditorManager.inst.DisplayNotification($"No copied animations yet!", 2F, EditorManager.NotificationType.Warning);
                                return;
                            }

                            animations.AddRange(copiedAnimations.Select(x => x.Copy()));
                            RenderPopup(animations, onPlay, onSelect, currentObject);
                            EditorManager.inst.DisplayNotification($"Pasted animations.", 2f, EditorManager.NotificationType.Success);
                        }));
                    return;
                }

                animations.Add(new PAAnimation("New Animation", "This is the default description!"));
                RenderPopup(animations, onPlay, onSelect, currentObject);
            };

            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text);

            for (int i = 0; i < animations.Count; i++)
            {
                var index = i;
                var animation = animations[i];

                if (!RTString.SearchString(Popup.SearchTerm, animation.name, animation.ReferenceID))
                    continue;

                var gameObject = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(Popup.Content, "anim");
                var storage = gameObject.GetComponent<SpriteFunctionButtonStorage>();
                storage.button.onClick.ClearAll();
                var contextClickable = gameObject.GetOrAddComponent<ContextClickable>();
                contextClickable.onClick = pointerEventData =>
                {
                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                    {
                        var buttonFunctions = new List<ButtonFunction>()
                        {
                            new ButtonFunction("Edit", () => OpenDialog(animation)),
                            new ButtonFunction("Play", () =>
                            {
                                if (onPlay == null)
                                {
                                    EditorManager.inst.DisplayNotification($"Cannot play the animation as no object is associated with it.", 2f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                onPlay.Invoke(animation);
                                EditorManager.inst.DisplayNotification($"Played animation!", 2f, EditorManager.NotificationType.Success);
                            }),
                            new ButtonFunction("Delete", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this animation?", () =>
                            {
                                animations.RemoveAt(index);
                                RenderPopup(animations, onPlay, onSelect, currentObject);
                                RTEditor.inst.HideWarningPopup();
                            }, RTEditor.inst.HideWarningPopup)),
                            new ButtonFunction(true),
                            new ButtonFunction("Apply To Object", () => EditorTimeline.inst.onSelectTimelineObject = timelineObject =>
                            {
                                if (!timelineObject.isBeatmapObject)
                                {
                                    EditorManager.inst.DisplayNotification("Cannot apply animation to non-beatmap object.", 2f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                beatmapObject.CopyAnimatableData(animation);
                                if (ObjectEditor.inst.Dialog.IsCurrent && EditorTimeline.inst.CurrentSelection.ID == beatmapObject.id)
                                    ObjectEditor.inst.Dialog.Timeline.RenderKeyframes(beatmapObject);
                                RTLevel.Current.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                            }),
                            new ButtonFunction("Copy From Object", () => EditorTimeline.inst.onSelectTimelineObject = timelineObject =>
                            {
                                if (!timelineObject.isBeatmapObject)
                                {
                                    EditorManager.inst.DisplayNotification("Cannot apply animation from non-beatmap object.", 2f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                animation.CopyAnimatableData(beatmapObject);
                                if (Dialog.IsCurrent && Dialog.Timeline.CurrentObject == animation)
                                    RenderDialog(animation);
                            }),
                            new ButtonFunction(true),
                            new ButtonFunction("Copy", () =>
                            {
                                copiedAnimations.Clear();
                                copiedAnimations.Add(animation.Copy());
                                EditorManager.inst.DisplayNotification($"Copied animation.", 2f, EditorManager.NotificationType.Success);
                            }),
                            new ButtonFunction("Copy All", () =>
                            {
                                copiedAnimations = new List<PAAnimation>(animations.Select(x => x.Copy()));
                                EditorManager.inst.DisplayNotification($"Copied all animations.", 2f, EditorManager.NotificationType.Success);
                            }),
                            new ButtonFunction("Paste", () =>
                            {
                                if (copiedAnimations.IsEmpty())
                                {
                                    EditorManager.inst.DisplayNotification($"No copied animations yet!", 2F, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                animations.AddRange(copiedAnimations.Select(x => x.Copy()));
                                RenderPopup(animations, onPlay, onSelect, currentObject);
                                EditorManager.inst.DisplayNotification($"Pasted animations.", 2f, EditorManager.NotificationType.Success);
                            }),
                            new ButtonFunction(true),
                            new ButtonFunction("Copy Keyframes", () => KeyframeTimeline.CopyAllKeyframes(animation)),
                            new ButtonFunction(true),
                            new ButtonFunction("Move Up", () =>
                            {
                                if (index <= 0)
                                {
                                    EditorManager.inst.DisplayNotification("Could not move modifier up since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                                    return;
                                }

                                animations.Move(index, index - 1);
                                RenderPopup(animations, onPlay, onSelect, currentObject);
                            }),
                            new ButtonFunction("Move Down", () =>
                            {
                                if (index >= animations.Count - 1)
                                {
                                    EditorManager.inst.DisplayNotification("Could not move modifier up since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                                    return;
                                }

                                animations.Move(index, index + 1);
                                RenderPopup(animations, onPlay, onSelect, currentObject);
                            }),
                            new ButtonFunction("Move to Start", () =>
                            {
                                animations.Move(index, 0);
                                RenderPopup(animations, onPlay, onSelect, currentObject);
                            }),
                            new ButtonFunction("Move to End", () =>
                            {
                                animations.Move(index, animations.Count - 1);
                                RenderPopup(animations, onPlay, onSelect, currentObject);
                            }),
                        };

                        EditorContextMenu.inst.ShowContextMenu(buttonFunctions);
                        return;
                    }

                    if (onSelect != null)
                    {
                        onSelect.Invoke(animation);
                        return;
                    }

                    OpenDialog(animation);
                };

                storage.image.sprite = EditorSprites.PlaySprite;
                storage.label.text = animation.name + $" [ {animation.ReferenceID} ]";

                EditorThemeManager.ApplyGraphic(storage.image, ThemeGroup.Light_Text);
                EditorThemeManager.ApplySelectable(storage.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(storage.label);
            }
        }

        #endregion

        #endregion
    }
}
