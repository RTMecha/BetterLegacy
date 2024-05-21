using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLegacy.Arcade;
using BetterLegacy.Components;
using BetterLegacy.Components.Editor;
using BetterLegacy.Components.Player;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Core.Optimization.Objects;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Menus;
using InControl;
using LSFunctions;
using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Configs
{
    public class TestConfig : BaseConfig
    {
        public static TestConfig Instance { get; set; }

        public TestConfig() : base("Test") // Set config name via base("")
        {
            Instance = this;
            BindSettings();

            SetupSettingChanged();
        }

        #region Settings

        public Setting<int> SomeSetting { get; set; }

        #endregion

        /// <summary>
        /// Bind the individual settings of the config.
        /// </summary>
        public override void BindSettings()
        {
            Load();
            // Binding settings go in the middle here.
            Save();
        }

        #region Settings Changed

        public override void SetupSettingChanged()
        {

        }

        #endregion
    }
}
