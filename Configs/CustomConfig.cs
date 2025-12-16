
using UnityEngine;

using BetterLegacy.Core;

namespace BetterLegacy.Configs
{
    public class CustomConfig : BaseConfig
    {
        public CustomConfig(string name) : base(name) // Set config name via base("")
        {
            BindSettings();

            SetupSettingChanged();
        }

        public string tabName = "Custom";
        public Color tabColor = RTColors.errorColor;
        public string tabDesc = "Custom config";
        public override string TabName => tabName;
        public override Color TabColor => new Color(1f, 0.143f, 0.22f, 1f);
        public override string TabDesc => tabDesc;

        /// <summary>
        /// Bind the individual settings of the config.
        /// </summary>
        public override void BindSettings()
        {
            Load();

            if (AssetPack.TryReadFromFile($"configs/{RTFile.FormatLegacyFileName(Name)}{FileFormat.LSC.Dot()}", out string configFile))
            {
                var jn = SimpleJSON.JSON.Parse(configFile);
                for (int i = 0; i < jn["settings"].Count; i++)
                {
                    var setting = jn["settings"][i];
                    switch (setting["type"].Value.ToLower())
                    {
                        case "bool": {
                                Bind(this, setting["section"], setting["key"], setting["default"].AsBool, setting["desc"]);
                                break;
                            }
                        case "float": {
                                Bind(this, setting["section"], setting["key"], setting["default"].AsFloat, setting["desc"], setting["min"].AsFloat, setting["max"].AsFloat);
                                break;
                            }
                        case "int": {
                                Bind(this, setting["section"], setting["key"], setting["default"].AsInt, setting["desc"], setting["min"].AsInt, setting["max"].AsInt);
                                break;
                            }
                        case "string": {
                                Bind(this, setting["section"], setting["key"], setting["default"].Value, setting["desc"]);
                                break;
                            }
                    }
                }
            }

            Save();
        }

        #region Settings Changed

        public override void SetupSettingChanged() { }

        #endregion
    }
}
