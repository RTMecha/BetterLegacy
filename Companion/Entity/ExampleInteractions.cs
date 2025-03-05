using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Managers;
using UnityEngine.UI;

namespace BetterLegacy.Companion.Entity
{
    /// <summary>
    /// Represents Example's interactions with the game.
    /// </summary>
    public class ExampleInteractions : ExampleModule
    {
        #region Default Instance

        /// <summary>
        /// The default interactions.
        /// </summary>
        public static ExampleInteractions Default
        {
            get
            {
                var interactions = new ExampleInteractions();
                interactions.InitDefault();
                return interactions;
            }
        }

        public override void InitDefault()
        {

        }

        #endregion

        #region Interactions

        /// <summary>
        /// When Example's head is clicked.
        /// </summary>
        public const string PET = "Pet";
        /// <summary>
        /// When you chat with Example.
        /// </summary>
        public const string CHAT = "Chat";
        /// <summary>
        /// When you hold one of Example's hands.
        /// </summary>
        public const string HOLD_HAND = "Hold Hand";
        /// <summary>
        /// When you touch Example's tail. Why would you do that.
        /// </summary>
        public const string TOUCHIE = "Touchie";
        /// <summary>
        /// When you interupt Example while he's dancing. Bruh.
        /// </summary>
        public const string INTERRUPT = "Interrupt";

        // TODO:
        // you can respond to Example's question about what a level is, which will add to his memory.
        /// <summary>
        /// Selects an object based on the image position.
        /// </summary>
        /// <param name="image">Image to check for objects under.</param>
        public void SelectObject(Image image)
        {
            var rect = EditorManager.RectTransformToScreenSpace(image.rectTransform);
            if (CoreHelper.InEditor && rect.Overlaps(EditorManager.RectTransformToScreenSpace(RTEditor.inst.OpenLevelPopup.GameObject.transform.Find("mask").AsRT())))
                foreach (var levelItem in RTEditor.inst.LevelPanels)
                {
                    if (levelItem.GameObject.activeInHierarchy && rect.Overlaps(EditorManager.RectTransformToScreenSpace(levelItem.GameObject.transform.AsRT())))
                    {
                        CompanionManager.Log($"Picked level: {levelItem.FolderPath}");
                        reference?.chatBubble?.Say($"What's \"{levelItem.Name}\"?");
                        break; // only select one level
                    }
                }
        }

        #endregion

        #region Core

        public override void Build()
        {

        }

        public override void Tick()
        {
            if (ProjectPlanner.inst && reference && reference.chatBubble && reference.brain && !reference.brain.talking)
                foreach (var schedule in ProjectPlanner.inst.schedules)
                {
                    if (!schedule.hasBeenChecked && schedule.IsActive)
                    {
                        schedule.hasBeenChecked = true;
                        reference.chatBubble.Say($"Reminding you about your schedule \"{schedule.Description}\" at {schedule.DateTime}");
                        ProjectPlanner.inst.SaveSchedules();
                    }
                }

        }

        public override void Clear()
        {
            attributes.Clear();
        }

        #endregion
    }
}
