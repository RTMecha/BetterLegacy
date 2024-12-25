using BetterLegacy.Core.Managers;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Core.Data.Level
{
    public class PlayerData
    {
        public PlayerData() { }

        public string LevelName { get; set; }
        public string ID { get; set; }
        public bool Completed { get; set; }
        public int Hits { get; set; } = -1;
        public int Deaths { get; set; } = -1;
        public int Boosts { get; set; } = -1;
        public int PlayedTimes { get; set; }
        public float TimeInLevel { get; set; }
        public float Percentage { get; set; }
        public float LevelLength { get; set; }
        public bool Unlocked { get; set; }

        public void Update()
        {
            if (Hits > GameManager.inst.hits.Count)
                Hits = GameManager.inst.hits.Count;

            if (Deaths > GameManager.inst.deaths.Count)
                Deaths = GameManager.inst.deaths.Count;

            var l = AudioManager.inst.CurrentAudioSource.clip.length;
            if (LevelLength != l)
                LevelLength = l;

            float calc = AudioManager.inst.CurrentAudioSource.time / AudioManager.inst.CurrentAudioSource.clip.length * 100f;

            if (Percentage < calc)
                Percentage = calc;
        }

        public void Update(int deaths, int hits, int boosts, bool completed)
        {
            if (Deaths == -1 || Deaths > deaths)
                Deaths = deaths;
            if (Hits == -1 || Hits > hits)
                Hits = hits;
            if (Boosts == -1 || Boosts > boosts)
                Boosts = boosts;
            Completed = completed;
        }

        public static PlayerData Parse(JSONNode jn) => new PlayerData
        {
            LevelName = jn["n"],
            ID = jn["id"],
            Completed = jn["c"].AsBool,
            Hits = jn["h"].AsInt,
            Deaths = jn["d"].AsInt,
            Boosts = jn["b"].AsInt,
            PlayedTimes = jn["pt"].AsInt,
            TimeInLevel = jn["t"].AsFloat,
            Percentage = jn["p"].AsFloat,
            LevelLength = jn["l"].AsFloat,
            Unlocked = jn["u"].AsBool,
        };

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");
            if (!string.IsNullOrEmpty(LevelName))
                jn["n"] = LevelName;
            jn["id"] = ID;
            jn["c"] = Completed;
            jn["h"] = Hits;
            jn["d"] = Deaths;
            jn["b"] = Boosts;
            jn["pt"] = PlayedTimes;
            jn["t"] = TimeInLevel;
            jn["p"] = Percentage;
            jn["l"] = LevelLength;
            jn["u"] = Unlocked;
            return jn;
        }

        public override string ToString() => $"{ID} - Hits: {Hits} Deaths: {Deaths}";
    }
}
