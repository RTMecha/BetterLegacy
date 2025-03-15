using SimpleJSON;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class LevelEditorData : Exists
    {
        public LevelEditorData()
        {

        }

        public float timelinePos;

        public float mainTimelineZoom;

        public static LevelEditorData Parse(JSONNode jn, bool add = true)
        {
            var editorData = new LevelEditorData();

            if (!string.IsNullOrEmpty(jn["timeline_pos"]))
                editorData.timelinePos = jn["timeline_pos"].AsFloat;
            else
                editorData.timelinePos = 0f;

            if (!string.IsNullOrEmpty(jn["timeline_zoom"]))
                editorData.mainTimelineZoom = jn["timeline_zoom"].AsFloat;

            if (add)
                editorData.openAmount++;

            return editorData;
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            jn["timeline"]["at"] = timelinePos.ToString();
            jn["timeline"]["z"] = mainTimelineZoom.ToString();
            jn["timeline"]["tsc"] = timelineScroll.ToString();
            jn["timeline"]["l"] = startLayer.ToString();
            jn["timeline"]["lt"] = startLayerType.ToString();
            jn["editor"]["t"] = timeEdit.ToString();
            jn["editor"]["a"] = openAmount.ToString();
            jn["misc"]["sn"] = snapActive.ToString();

            return jn;
        }

        public int startLayer;
        public int startLayerType;
        public float timelineScroll;
        public float timeEdit;
        public int openAmount;
        public bool snapActive;
    }
}
