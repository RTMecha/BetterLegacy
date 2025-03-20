using SimpleJSON;

namespace BetterLegacy.Menus.UI
{
    public struct ReactiveSetting
    {
        public enum ControlType
        {
            Position,
            Scale,
            Rotation,
            Color,
        }

        public bool init;
        public ControlType controls;
        public int[] channels;
        public float[] intensity;

        public static ReactiveSetting Parse(JSONNode jn, int num)
        {
            if (jn == null || jn["channels"] == null || jn["intensity"] == null || jn["controls"] == null)
                return default;

            var reactiveSetting = new ReactiveSetting()
            {
                init = true,
                channels = new int[jn["channels"].Count],
                intensity = new float[jn["intensity"].Count],
                controls = (ControlType)jn["controls"].AsInt,
            };

            var increaseLoop = jn["increase_loop"].AsBool;

            for (int i = 0; i < jn["channels"].Count; i++)
                reactiveSetting.channels[i] = jn["channels"][i].AsInt + (increaseLoop ? num : 0);
            for (int i = 0; i < jn["intensity"].Count; i++)
                reactiveSetting.intensity[i] = jn["intensity"][i].AsFloat;

            return reactiveSetting;
        }
    }
}
