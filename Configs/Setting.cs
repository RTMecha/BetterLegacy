using BetterLegacy.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BetterLegacy.Configs
{

    public abstract class BaseSetting
    {
        public BaseSetting(string section, string key, string description, Action settingChanged)
        {
            Section = section;
            Key = key;
            Description = description;
            SettingChanged = settingChanged;
        }

        public string Section { get; set; }
        public string Key { get; set; }
        public string Description { get; set; }
        public abstract object BoxedValue { get; set; }
        public abstract object DefaultValue { get; set; }
        public Action SettingChanged { get; set; }
        public abstract void OnSettingChanged(object instance);
    }

    public class Setting<T> : BaseSetting
    {
        public Setting(string section, string key, T defaultValue, string description, T minValue = default, T maxValue = default, Action settingChanged = null) : base(section, key, description, settingChanged)
        {
            DefaultValue = defaultValue;
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public override object BoxedValue { get => Value; set => Value = (T)value; }

        public override object DefaultValue { get; set; }
        T value;
        public T Value
        {
            get => value;
            set
            {
                this.value = (T)Clamp(value);
                if (fireSettingChanged)
                {
                    OnSettingChanged(this);
                    SettingChanged?.Invoke();
                }
            }
        }

        public bool fireSettingChanged = false;
        public BaseConfig Config { get; set; }

        public T MinValue { get; set; }
        public T MaxValue { get; set; }

        public T Default { get => (T)DefaultValue; set => DefaultValue = value; }

        public void Reset() => BoxedValue = DefaultValue;

        public override void OnSettingChanged(object instance)
        {
            Config?.OnSettingChanged(instance);
        }

        public object Clamp(T value)
        {
            if (value is float floatValue && MinValue is float floatMin && MaxValue is float floatMax && (floatMin != default || floatMax != default))
            {
                return Mathf.Clamp(floatValue, floatMin, floatMax);
            }
            if (value is int intValue && MinValue is int intMin && MaxValue is int intMax && (intMin != default || intMax != default))
            {
                return Mathf.Clamp(intValue, intMin, intMax);
            }
            if (value is Vector2 vector2Value && MinValue is Vector2 vector2Min && MaxValue is Vector2 vector2Max && (vector2Min != default || vector2Max != default))
            {
                return RTMath.Clamp(vector2Value, vector2Min, vector2Max);
            }
            if (value is Vector3 vector3Value && MinValue is Vector3 vector3Min && MaxValue is Vector3 vector3Max && (vector3Min != default || vector3Max != default))
            {
                return RTMath.Clamp(vector3Value, vector3Min, vector3Max);
            }

            return value;
        }
    }
}
