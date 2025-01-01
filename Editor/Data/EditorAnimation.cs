﻿using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Handles editor UI's animation.
    /// </summary>
    public class EditorAnimation : Exists
    {
        public EditorAnimation(string name) => this.name = name;

        public string name;

        #region Configs

        public Setting<bool> ActiveConfig { get; set; }

        // Position
        public Setting<bool> PosActiveConfig { get; set; }
        public Setting<Vector2> PosOpenConfig { get; set; }
        public Setting<Vector2> PosCloseConfig { get; set; }
        public Setting<Vector2> PosOpenDurationConfig { get; set; }
        public Setting<Vector2> PosCloseDurationConfig { get; set; }
        public Setting<Easings> PosXOpenEaseConfig { get; set; }
        public Setting<Easings> PosXCloseEaseConfig { get; set; }
        public Setting<Easings> PosYOpenEaseConfig { get; set; }
        public Setting<Easings> PosYCloseEaseConfig { get; set; }

        // Scale
        public Setting<bool> ScaActiveConfig { get; set; }
        public Setting<Vector2> ScaOpenConfig { get; set; }
        public Setting<Vector2> ScaCloseConfig { get; set; }
        public Setting<Vector2> ScaOpenDurationConfig { get; set; }
        public Setting<Vector2> ScaCloseDurationConfig { get; set; }
        public Setting<Easings> ScaXOpenEaseConfig { get; set; }
        public Setting<Easings> ScaXCloseEaseConfig { get; set; }
        public Setting<Easings> ScaYOpenEaseConfig { get; set; }
        public Setting<Easings> ScaYCloseEaseConfig { get; set; }

        // Rotation
        public Setting<bool> RotActiveConfig { get; set; }
        public Setting<float> RotOpenConfig { get; set; }
        public Setting<float> RotCloseConfig { get; set; }
        public Setting<float> RotOpenDurationConfig { get; set; }
        public Setting<float> RotCloseDurationConfig { get; set; }
        public Setting<Easings> RotOpenEaseConfig { get; set; }
        public Setting<Easings> RotCloseEaseConfig { get; set; }

        #endregion

        #region Values

        public bool Active => ActiveConfig.Value;

        public bool PosActive => PosActiveConfig.Value;
        public Vector2 PosStart => PosCloseConfig.Value;
        public Vector2 PosEnd => PosOpenConfig.Value;
        public float PosXStartDuration => PosOpenDurationConfig.Value.x;
        public float PosXEndDuration => PosCloseDurationConfig.Value.x;
        public string PosXStartEase => PosXOpenEaseConfig.Value.ToString();
        public string PosXEndEase => PosXCloseEaseConfig.Value.ToString();
        public float PosYStartDuration => PosOpenDurationConfig.Value.y;
        public float PosYEndDuration => PosCloseDurationConfig.Value.y;
        public string PosYStartEase => PosYOpenEaseConfig.Value.ToString();
        public string PosYEndEase => PosYCloseEaseConfig.Value.ToString();

        public bool ScaActive => ScaActiveConfig.Value;
        public Vector2 ScaStart => ScaCloseConfig.Value;
        public Vector2 ScaEnd => ScaOpenConfig.Value;
        public float ScaXStartDuration => ScaOpenDurationConfig.Value.x;
        public float ScaXEndDuration => ScaCloseDurationConfig.Value.x;
        public string ScaXStartEase => ScaXOpenEaseConfig.Value.ToString();
        public string ScaXEndEase => ScaXCloseEaseConfig.Value.ToString();
        public float ScaYStartDuration => ScaOpenDurationConfig.Value.y;
        public float ScaYEndDuration => ScaCloseDurationConfig.Value.y;
        public string ScaYStartEase => ScaYOpenEaseConfig.Value.ToString();
        public string ScaYEndEase => ScaYCloseEaseConfig.Value.ToString();

        public bool RotActive => RotActiveConfig.Value;
        public float RotStart => RotCloseConfig.Value;
        public float RotEnd => RotOpenConfig.Value;
        public float RotStartDuration => RotOpenDurationConfig.Value;
        public float RotEndDuration => RotCloseDurationConfig.Value;
        public string RotStartEase => RotOpenEaseConfig.Value.ToString();
        public string RotEndEase => RotCloseEaseConfig.Value.ToString();

        #endregion
    }
}