using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetAudioProperty : ModifierActionBase
    {
        #region Constructors

        public SetAudioProperty(MathOperation operation, AudioProperty audioProperty, ObjectProperty objectProperty, bool isMath)
        {
            this.operation = operation;
            this.audioProperty = audioProperty;
            this.objectProperty = objectProperty;
            this.isMath = isMath;
            Name = operation switch
            {
                MathOperation.Addition => "add" + audioProperty.ToString(),
                MathOperation.Subtract => "sub" + audioProperty.ToString(),
                _ => operation.ToString().ToLower() + audioProperty.ToString(),
            };
            if (objectProperty != ObjectProperty.None)
                Name += objectProperty.ToString();
            if (isMath)
                Name += "Math";
            SetupModifier();
            if (objectProperty == ObjectProperty.None)
                Modifier.values.Add(operation == MathOperation.Set && audioProperty == AudioProperty.Pitch ? "1" : operation == MathOperation.Addition && audioProperty == AudioProperty.Pitch ? "0.1" : "0");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Audio;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        readonly AudioProperty audioProperty;

        readonly MathOperation operation;

        readonly ObjectProperty objectProperty;

        readonly bool isMath;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            switch (audioProperty)
            {
                case AudioProperty.Pitch: {
                        if (!RTLevel.Current.eventEngine)
                            break;

                        RTMath.Operation(ref RTLevel.Current.eventEngine.pitchOffset, GetValue(modifier, modifierLoop), operation);
                        break;
                    }
                case AudioProperty.MusicTime: {
                        AudioManager.inst.SetMusicTime(RTMath.ReturnOperation(AudioManager.inst.CurrentAudioSource.time, GetValue(modifier, modifierLoop), operation));
                        break;
                    }
            }
        }

        float GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (objectProperty != ObjectProperty.None)
                return objectProperty switch
                {
                    ObjectProperty.StartTime => modifierLoop.reference is ILifetime lifetime ? lifetime.StartTime : 0f,
                    ObjectProperty.Autokill => modifierLoop.reference is ILifetime lifetime ? lifetime.StartTime + lifetime.SpawnDuration : 0f,
                    _ => 0f,
                };
            if (isMath)
            {
                if (modifierLoop.reference is not IEvaluatable evaluatable)
                    return 0f;

                var numberVariables = evaluatable.GetObjectVariables();
                if (modifierLoop.variables != null)
                {
                    foreach (var variable in modifierLoop.variables)
                    {
                        if (float.TryParse(variable.Value, out float num))
                            numberVariables[variable.Key] = num;
                    }
                }

                return RTMath.Parse(FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables), RTLevel.Current?.evaluationContext, numberVariables);
            }
            return modifier.GetFloat(0, 0f, modifierLoop.variables);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (objectProperty == ObjectProperty.None)
                modifierCard.SingleGenerator(modifier, reference, "Value", 0, 1f);
        }

        #endregion

        #region Sub Classes

        public enum AudioProperty
        {
            Pitch,
            MusicTime,
        }

        public enum ObjectProperty
        {
            None,
            StartTime,
            Autokill
        }

        #endregion
    }
}
