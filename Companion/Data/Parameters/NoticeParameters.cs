using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;

namespace BetterLegacy.Companion.Data.Parameters
{
    /// <summary>
    /// Notice parameters passed to Example's brain.
    /// </summary>
    public class NoticeParameters : Exists
    {
        public NoticeParameters() { }
    }

    /// <summary>
    /// Passes data related to a player.
    /// </summary>
    public class PlayerNoticeParameters : NoticeParameters
    {
        public PlayerNoticeParameters() { }

        public PlayerNoticeParameters(CustomPlayer player)
        {
            this.player = player;
        }

        /// <summary>
        /// Player reference.
        /// </summary>
        public CustomPlayer player;
    }

    public class BeatmapObjectNoticeParameters : NoticeParameters
    {
        public BeatmapObjectNoticeParameters() { }

        public BeatmapObjectNoticeParameters(BeatmapObject beatmapObject) => this.beatmapObject = beatmapObject;

        public BeatmapObjectNoticeParameters(BeatmapObject beatmapObject, bool custom) : this(beatmapObject) => this.custom = custom;

        public BeatmapObject beatmapObject;

        public bool custom = false;
    }

    public class PrefabNoticeParameters : NoticeParameters
    {
        public PrefabNoticeParameters() { }

        public PrefabNoticeParameters(Prefab prefab) => this.prefab = prefab;

        public PrefabNoticeParameters(Prefab prefab, PrefabObject prefabObject) : this(prefab) => this.prefabObject = prefabObject;

        public Prefab prefab;

        public PrefabObject prefabObject;
    }

    public class SceneNoticeParameters : NoticeParameters
    {
        public SceneNoticeParameters() { }

        public SceneNoticeParameters(SceneName scene) => this.scene = scene;

        public SceneNoticeParameters(SceneName scene, SceneType type) : this(scene) => this.type = type;

        public SceneName scene;

        public SceneType type;
    }
}
