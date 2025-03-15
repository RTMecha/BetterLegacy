using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class Checkpoint : Exists
	{
		public Checkpoint() { }

		public Checkpoint(bool active, string name, float time, Vector2 pos)
		{
			this.active = active;
			this.name = name ?? DEFAULT_CHECKPOINT_NAME;
			this.time = time;
			this.pos = pos;
		}

		public static Checkpoint DeepCopy(Checkpoint orig) => new Checkpoint(orig.active, orig.name, orig.time, orig.pos);

		public static Checkpoint ParseVG(JSONNode jn) => new Checkpoint(true, jn["n"], jn["t"].AsFloat, jn["p"].AsVector2());
		public static Checkpoint Parse(JSONNode jn) => new Checkpoint(jn["active"].AsBool, jn["name"], jn["t"].AsFloat, jn["pos"].AsVector2());

		public JSONNode ToJSONVG()
        {
			var jn = JSON.Parse("{}");

			jn["n"] = name;
			jn["t"] = time.ToString();
			jn["p"] = pos.ToJSON();

			return jn;
		}
		
		public JSONNode ToJSON()
        {
			var jn = JSON.Parse("{}");

			// save "False" because vanilla Legacy can't handle "active" being null.
			jn["active"] = "False";
			jn["name"] = name;
			jn["t"] = time.ToString();
			jn["pos"] = pos.ToJSON();

			return jn;
		}

		public bool active;

		public string name = DEFAULT_CHECKPOINT_NAME;

		public float time;

		public Vector2 pos = Vector2.zero;

		/// <summary>
		/// The name of the first checkpoint.
		/// </summary>
		public const string BASE_CHECKPOINT_NAME = "Base Checkpoint";

		/// <summary>
		/// The default name of a checkpoint.
		/// </summary>
		public const string DEFAULT_CHECKPOINT_NAME = "Checkpoint";
	}
}
