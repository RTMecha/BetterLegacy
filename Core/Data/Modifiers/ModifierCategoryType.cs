namespace BetterLegacy.Core.Data.Modifiers
{
    /// <summary>
    /// Modifier category.
    /// </summary>
    public enum ModifierCategoryType
    {
        /// <summary>
        /// Contains main modifier functions.
        /// </summary>
        Main,
        /// <summary>
        /// Contains editor related modifiers.
        /// </summary>
        Editor,
        /// <summary>
        /// Contains modifiers that handle modifier interaction.
        /// </summary>
        Modifier,
        /// <summary>
        /// Contains audio related modifier functions.
        /// </summary>
        Audio,
        /// <summary>
        /// Contains level related modifier functions.
        /// </summary>
        Level,
        /// <summary>
        /// Contains Unity component related functions.
        /// </summary>
        Component,
        /// <summary>
        /// Contains modifiers that affect rendering.
        /// </summary>
        Rendering,
        /// <summary>
        /// Contains player related modifiers.
        /// </summary>
        Player,
        /// <summary>
        /// Contains modifiers that detect player input.
        /// </summary>
        Controls,
        /// <summary>
        /// Contains modifiers that handle active states.
        /// </summary>
        Enable,
        /// <summary>
        /// Contains JSON save / load modifiers.
        /// </summary>
        JSON,
        /// <summary>
        /// Contains modifiers that can change events.
        /// </summary>
        Events,
        /// <summary>
        /// Contains modifiers that affect colors.
        /// </summary>
        Color,
        /// <summary>
        /// Contains modifiers that affect an objects shape / text / image.
        /// </summary>
        Shape,
        /// <summary>
        /// Contains modifiers that can animate.
        /// </summary>
        Animation,
        /// <summary>
        /// Contains modifiers related to prefabs.
        /// </summary>
        Prefab,
        /// <summary>
        /// Contains modifiers that affect runtime.
        /// </summary>
        Runtime,
        /// <summary>
        /// Contains modifiers that affect physics.
        /// </summary>
        Physics,
        /// <summary>
        /// Contain modifiers that can set checkpoints and check markers / checkpoints.
        /// </summary>
        Checkpoints,
        /// <summary>
        /// Contains modifiers that can open interfaces.
        /// </summary>
        Interfaces,
        /// <summary>
        /// Contains modifiers that can do something to the application or something outside of the application.
        /// </summary>
        Application,
    }
}
