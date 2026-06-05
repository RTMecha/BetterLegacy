using SimpleJSON;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Particle system data.
    /// </summary>
    public class ParticleSystemData : PAObject<ParticleSystemData>
    {
        #region Constructors

        public ParticleSystemData() { }

        #endregion

        #region Values

        /// <summary>
        /// Spawn rate of particles per second.
        /// </summary>
        public float spawnRatePerSecond = 0f;

        /// <summary>
        /// Spawn rate of particles per unit.
        /// </summary>
        public float spawnRatePerUnit = 0f;

        /// <summary>
        /// Space the particles should spawn by.
        /// </summary>
        public bool worldSpace = true;

        /// <summary>
        /// Particle despawn behavior.
        /// </summary>
        public AutoKillType autoKillType;

        /// <summary>
        /// Autokill time offset.
        /// </summary>
        public float autoKillOffset;

        /// <summary>
        /// Shape type for the particle emitter.
        /// </summary>
        public EmitterShapeType emitterShapeType;

        /// <summary>
        /// Shape type for the particle emitter.
        /// </summary>
        public enum EmitterShapeType
        {
            /// <summary>
            /// Particles spawn in a rectangular shape.
            /// </summary>
            Rectangle,
            /// <summary>
            /// Particle spawn in a circular shape.
            /// </summary>
            Circle,
        }

        /// <summary>
        /// Rotational arc of the particle emitter.
        /// </summary>
        public float emitterArc = 360f;

        /// <summary>
        /// Radius of the particle emitter.
        /// </summary>
        public float emitterRadius = 1f;

        /// <summary>
        /// Start speed of the particles.
        /// </summary>
        public float startSpeed = 1f;

        #endregion

        #region Functions

        public override void CopyData(ParticleSystemData orig, bool newID = true)
        {
            spawnRatePerSecond = orig.spawnRatePerSecond;
            spawnRatePerUnit = orig.spawnRatePerUnit;
            worldSpace = orig.worldSpace;
            autoKillType = orig.autoKillType;
            autoKillOffset = orig.autoKillOffset;
            emitterShapeType = orig.emitterShapeType;
            emitterArc = orig.emitterArc;
            emitterRadius = orig.emitterRadius;
            startSpeed = orig.startSpeed;
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (jn["sr_ps"] != null)
                spawnRatePerSecond = jn["sr_ps"].AsFloat;
            if (jn["sr_pu"] != null)
                spawnRatePerUnit = jn["sr_pu"].AsFloat;
            if (jn["s"] != null)
                worldSpace = jn["s"].AsInt == 1;
            if (jn["akt"] != null)
                autoKillType = (AutoKillType)jn["akt"].AsInt;
            if (jn["ako"] != null)
                autoKillOffset = jn["ako"].AsFloat;
            if (jn["es"] != null)
                emitterShapeType = (EmitterShapeType)jn["es"].AsInt;
            if (jn["ea"] != null)
                emitterArc = jn["ea"].AsFloat;
            if (jn["er"] != null)
                emitterRadius = jn["er"].AsFloat;
            if (jn["ss"] != null)
                startSpeed = jn["ss"].AsFloat;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            if (spawnRatePerSecond != 0f)
                jn["sr_ps"] = spawnRatePerSecond;
            if (spawnRatePerUnit != 0f)
                jn["sr_pu"] = spawnRatePerUnit;
            if (!worldSpace)
                jn["s"] = 0;
            jn["akt"] = (int)autoKillType;
            jn["ako"] = autoKillOffset;
            if (emitterShapeType != EmitterShapeType.Rectangle)
                jn["es"] = (int)emitterShapeType;
            if (emitterArc != 360f)
                jn["ea"] = emitterArc;
            if (emitterRadius != 1f)
                jn["er"] = emitterRadius;
            if (startSpeed != 1f)
                jn["ss"] = startSpeed;

            return jn;
        }

        #endregion
    }
}
