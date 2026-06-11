using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime.Objects.Visual;

namespace BetterLegacy.Core.Runtime.Objects
{
    public class RTParticles : Exists, IRTObject
    {
        public RTParticles(BeatmapObject beatmapObject, RTLevelBase parentRuntime)
        {
            this.beatmapObject = beatmapObject;

            ParentRuntime = parentRuntime;
            StartTime = beatmapObject.StartTime;
            KillTime = beatmapObject.StartTime + beatmapObject.ParticlesSpawnDuration;

            solidObject = beatmapObject.runtimeObject?.visualObject as SolidObject;
        }

        public RTLevelBase ParentRuntime { get; set; }

        public float StartTime { get; set; }

        public float KillTime { get; set; }

        public bool Active { get; set; }

        public BeatmapObject beatmapObject;

        public SolidObject solidObject;

        public void Clear()
        {
            solidObject?.StopParticles(UnityEngine.ParticleSystemStopBehavior.StopEmittingAndClear);
            beatmapObject = null;
            solidObject = null;
        }

        public void SetActive(bool active)
        {
            Active = active;
            if (active)
                solidObject.StartParticles();
            else
                solidObject?.StopParticles();
        }

        public void Interpolate(float time)
        {
            if (!solidObject)
                return;

            solidObject.InterpolateParticles(time - StartTime);
            solidObject.SetParticleScale(beatmapObject.runtimeObject.CurrentScale);
        }
    }
}
