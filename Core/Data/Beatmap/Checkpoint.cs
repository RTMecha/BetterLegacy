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

		public Checkpoint(string name, float time, Vector2 pos)
		{
			this.name = name ?? DEFAULT_CHECKPOINT_NAME;
			this.time = time;
			this.pos = pos;
		}

        #region Values

		/// <summary>
		/// Name of the checkpoint.
		/// </summary>
		public string name = DEFAULT_CHECKPOINT_NAME;

		/// <summary>
		/// Time to reverse to when all players are dead.
		/// </summary>
		public float time;

		/// <summary>
		/// Default position to spawn players at.
		/// </summary>
		public Vector2 pos = Vector2.zero;

		/// <summary>
		/// Positions that the player is spawned at.
		/// </summary>
		public List<Vector2> positions = new List<Vector2>();

		/// <summary>
		/// Spawn position behavior.
		/// </summary>
		public SpawnPositionType spawnType;

		/// <summary>
		/// If the players should respawn when the checkpoint is triggered.
		/// </summary>
		public bool respawn = true;

		/// <summary>
		/// If the players should heal when the checkpoint is triggered.
		/// </summary>
		public bool heal = false;

		/// <summary>
		/// If true, the game will reverse to the checkpoint time. Otherwise if false, the game will reverse to the start of the song.
		/// </summary>
		public bool setTime = true;

		/// <summary>
		/// If the song should reverse at all when all players are dead.
		/// </summary>
		public bool reverse = true;

		#region Global

		/// <summary>
		/// The name of the first checkpoint.
		/// </summary>
		public const string BASE_CHECKPOINT_NAME = "Base Checkpoint";

		/// <summary>
		/// The default name of a checkpoint.
		/// </summary>
		public const string DEFAULT_CHECKPOINT_NAME = "Checkpoint";

		/// <summary>
		/// Represents what checkpoint a player spawns at.
		/// </summary>
		public enum SpawnPositionType
		{
			/// <summary>
			/// The default checkpoint position. All players spawn here.
			/// </summary>
			Single,
			/// <summary>
			/// Fills all checkpoint positions with a player individually up until the last, then it loops over.
			/// </summary>
			FillAll,
			/// <summary>
			/// Fills all checkpoint positions with a player individually up until the last, then it loops over. Each player is assigned to a random checkpoint, instead of from 0.
			/// </summary>
			RandomFillAll,
			/// <summary>
			/// All players end up at the same checkpoint, but randomly.
			/// </summary>
			IndividualRandom,
			/// <summary>
			/// All players end up at random checkpoints.
			/// </summary>
			Random,
		}

		#endregion

		#endregion

		#region Methods

		public static Checkpoint DeepCopy(Checkpoint orig) => new Checkpoint(orig.name, orig.time, orig.pos);

		public static Checkpoint ParseVG(JSONNode jn) => new Checkpoint(jn["n"], jn["t"].AsFloat, jn["p"].AsVector2());
		public static Checkpoint Parse(JSONNode jn) => new Checkpoint(jn["name"], jn["t"].AsFloat, jn["pos"].AsVector2());

		public JSONNode ToJSONVG()
		{
			var jn = JSON.Parse("{}");

			jn["n"] = name;
			if (time != 0f)
				jn["t"] = time.ToString();
			if (pos.x != 0f && pos.y != 0f)
				jn["p"] = pos.ToJSON();

			return jn;
		}

		public JSONNode ToJSON()
		{
			var jn = JSON.Parse("{}");

			jn["name"] = name;
			if (time != 0f)
				jn["t"] = time;
			jn["pos"] = pos.ToJSON();

			return jn;
		}

		/// <summary>
		/// Gets a position at a specific index.
		/// </summary>
		/// <param name="index">The position index.</param>
		/// <returns>Returns a checkpoint position.</returns>
		public Vector2 GetPosition(int index) => spawnType == SpawnPositionType.Single || positions == null || !positions.InRange(index) ? pos : positions[index];

		public override string ToString() => name;

        #endregion
    }
}
