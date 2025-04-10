using System.Collections.Generic;

using UnityEngine;

using SimpleJSON;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class Checkpoint : PAObject<Checkpoint>
	{
		public Checkpoint() : base() { }

		public Checkpoint(string name, float time, Vector2 pos) : this()
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

        public override void CopyData(Checkpoint orig, bool newID = true)
        {
			name = orig.name;
			time = orig.time;
			pos = orig.pos;
			positions = new List<Vector2>(orig.positions);
			spawnType = orig.spawnType;
			respawn = orig.respawn;
			heal = orig.heal;
			setTime = orig.setTime;
			reverse = orig.reverse;
        }

        public override void ReadJSONVG(JSONNode jn, Version version = default)
		{
			name = jn["n"];
			time = jn["t"].AsFloat;
			pos = jn["p"].AsVector2();
		}

        public override void ReadJSON(JSONNode jn)
        {
			name = jn["name"];
			time = jn["t"].AsFloat;
			pos = jn["pos"].AsVector2();
        }

        public override JSONNode ToJSONVG()
		{
			var jn = JSON.Parse("{}");

			jn["n"] = name;
			if (time != 0f)
				jn["t"] = time.ToString();
			if (pos.x != 0f && pos.y != 0f)
				jn["p"] = pos.ToJSON();

			return jn;
		}

		public override JSONNode ToJSON()
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

		#endregion

		#region Operators

		public override bool Equals(object obj) => obj is Checkpoint paObj && id == paObj.id;

		public override int GetHashCode() => base.GetHashCode();

		public override string ToString() => $"{id} - {name}";

		#endregion
	}
}
