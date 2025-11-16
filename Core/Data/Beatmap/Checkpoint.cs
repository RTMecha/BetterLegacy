using System.Collections.Generic;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Editor.Data.Timeline;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class Checkpoint : PAObject<Checkpoint>
	{
		public Checkpoint() => id = GetShortNumberID();

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
		/// If true, the game will be set to the checkpoint time.
		/// </summary>
		public bool setTime = true;

		/// <summary>
		/// If the song should reverse at all when all players are dead.
		/// </summary>
		public bool reverse = true;

        /// <summary>
        /// Timeline Checkpoint reference.
        /// </summary>
        public TimelineCheckpoint timelineCheckpoint;

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
		/// The default checkpoint.
		/// </summary>
		public static Checkpoint Default => new Checkpoint(BASE_CHECKPOINT_NAME, 0f, Vector2.zero);

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
			/// All players end up at the same checkpoint, but randomly.
			/// </summary>
			RandomSingle,
			/// <summary>
			/// Fills all checkpoint positions with a player individually up until the last, then it loops over. Each player is assigned to a random checkpoint, instead of from 0.
			/// </summary>
			RandomFillAll,
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
			name = jn["n"] ?? string.Empty;
			time = jn["t"].AsFloat;
			pos = jn["p"].AsVector2();
			heal = true; // checkpoints in VG heal the player so this is on by default.
		}

        public override void ReadJSON(JSONNode jn)
        {
            if (jn["id"] != null)
                id = jn["id"];
            else
                id = GetShortNumberID();
			name = jn["name"] ?? string.Empty;
			time = jn["t"].AsFloat;
			pos = jn["pos"].AsVector2();
            spawnType = (SpawnPositionType)jn["pos_type"].AsInt;
            for (int i = 0; i < jn["pos_list"].Count; i++)
                positions.Add(Parser.TryParse(jn["pos_list"][i], Vector2.zero));
            if (jn["respawn"] != null)
                respawn = jn["respawn"].AsBool;
            if (jn["heal"] != null)
                heal = jn["heal"].AsBool;
            if (jn["set_time"] != null)
                setTime = jn["set_time"].AsBool;
            if (jn["reverse"] != null)
                reverse = jn["reverse"].AsBool;
        }

        public override JSONNode ToJSONVG()
		{
			var jn = JSON.Parse("{}");

			jn["n"] = name ?? string.Empty;
			if (time != 0f)
				jn["t"] = time;
			if (pos.x != 0f && pos.y != 0f)
				jn["p"] = pos.ToJSON();

			return jn;
		}

		public override JSONNode ToJSON()
		{
			var jn = Parser.NewJSONObject();

            jn["id"] = id ?? GetShortNumberID();
			jn["name"] = name ?? string.Empty;
			if (time != 0f)
				jn["t"] = time;
			jn["pos"] = pos.ToJSON();
            if (spawnType != SpawnPositionType.Single)
                jn["pos_type"] = (int)spawnType;
            for (int i = 0; i < positions.Count; i++)
                jn["pos_list"][i] = positions[i].ToJSON();
            if (!respawn)
                jn["respawn"] = respawn;
            if (heal)
                jn["heal"] = heal;
            if (!setTime)
                jn["set_time"] = setTime;
            if (!reverse)
                jn["reverse"] = reverse;

			return jn;
		}

        /// <summary>
        /// Gets the position at an index.
        /// </summary>
        /// <param name="index">Index of the position to get.</param>
        /// <returns>Returns a multi position at an index if the index is in the range of <see cref="positions"/>, otherwise returns <see cref="pos"/>.</returns>
        public Vector2 GetPosition(int index) => index == -1 ? pos : positions[index % positions.Count];

		#endregion

		#region Operators

		public override bool Equals(object obj) => obj is Checkpoint paObj && id == paObj.id;

		public override int GetHashCode() => base.GetHashCode();

		public override string ToString() => $"{id} - {name}";

		#endregion
	}
}
