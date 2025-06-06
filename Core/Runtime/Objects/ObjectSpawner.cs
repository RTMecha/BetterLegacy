﻿using System;
using System.Collections.Generic;

using BetterLegacy.Core.Data;

namespace BetterLegacy.Core.Runtime.Objects
{
    /// <summary>
    /// Handles object spawning and despawning.
    /// </summary>
    public class ObjectSpawner : Exists
    {
        public IEnumerable<IRTObject> ActiveObjects => activeObjects;

        public readonly List<IRTObject> activateList = new List<IRTObject>();
        public readonly List<IRTObject> deactivateList = new List<IRTObject>();

        int activateIndex = 0;
        int deactivateIndex = 0;
        float currentTime = 0.0f;

        readonly HashSet<IRTObject> activeObjects = new HashSet<IRTObject>();

        public static event RuntimeObjectNotifier OnObjectSpawned;
        public static event RuntimeObjectNotifier OnObjectDespawned;

        public ObjectSpawner(IEnumerable<IRTObject> levelObjects)
        {
            // populate activate and deactivate lists
            activateList.AddRange(levelObjects);
            deactivateList.AddRange(levelObjects);

            // sort by start time
            activateList.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));

            // sort by kill time
            deactivateList.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));
        }

        public void Update(float time)
        {
            if (time >= currentTime)
                UpdateObjectsForward(time);
            else
                UpdateObjectsBackward(time);

            currentTime = time;
        }

        /// <summary>
        /// Insert one object only.
        /// </summary>
        /// <param name="levelObject">The object to insert into.</param>
        /// <param name="recalculate">Whether should this recalculate object states.</param>
        public void InsertObject(IRTObject levelObject, bool recalculate = true)
        {
            activateList.Add(levelObject);
            activateList.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));

            deactivateList.Add(levelObject);
            deactivateList.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));

            if (recalculate)
                RecalculateObjectStates();
        }

        /// <summary>
        /// Remove one object only.
        /// </summary>
        /// <param name="levelObject">The object to remove from.</param>
        /// <param name="recalculate">Whether should this recalculate object states.</param>
        public void RemoveObject(IRTObject levelObject, bool recalculate = true)
        {
            activateList.Remove(levelObject);
            deactivateList.Remove(levelObject);

            if (recalculate)
                RecalculateObjectStates();
        }

        /// <summary>
        /// Insert multiple objects.
        /// </summary>
        /// <param name="levelObjects">The list of objects to insert into.</param>
        /// <param name="recalculate">Whether should this recalculate object states.</param>
        public void InsertObjects(IEnumerable<IRTObject> levelObjects, bool recalculate = true)
        {
            activateList.AddRange(levelObjects);
            activateList.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));

            deactivateList.AddRange(levelObjects);
            deactivateList.Sort((a, b) => a.KillTime.CompareTo(b.KillTime));

            if (recalculate)
                RecalculateObjectStates();
        }

        /// <summary>
        /// Remove multiple objects.
        /// </summary>
        /// <param name="predicate">The predicate that matches the objects to remove.</param>
        /// <param name="recalculate">Whether should this recalculate object states.</param>
        public void RemoveObjects(Predicate<IRTObject> predicate, bool recalculate = true)
        {
            activateList.RemoveAll(predicate);
            deactivateList.RemoveAll(predicate);

            if (recalculate)
                RecalculateObjectStates();
        }

        /// <summary>
        /// Clear all objects.
        /// </summary>
        public void Clear()
        {
            activateIndex = 0;
            deactivateIndex = 0;
            currentTime = 0.0f;
            activeObjects.Clear();
            activateList.Clear();
            deactivateList.Clear();
        }

        /// <summary>
        /// Recalculate object states.
        /// </summary>
        public void RecalculateObjectStates()
        {
            activateIndex = 0;
            deactivateIndex = 0;
            activeObjects.Clear();
            UpdateObjectsForward(currentTime);
        }

        void UpdateObjectsForward(float time)
        {
            // Spawn
            while (activateIndex < activateList.Count && time >= activateList[activateIndex].StartTime)
            {
                var activate = activateList[activateIndex];
                activate.SetActive(true);
                OnObjectSpawned?.Invoke(activate);
                activeObjects.Add(activate);
                activateIndex++;
            }

            // Despawn
            while (deactivateIndex < deactivateList.Count && time >= deactivateList[deactivateIndex].KillTime)
            {
                var deactivate = deactivateList[deactivateIndex];
                deactivate.SetActive(false);
                OnObjectDespawned?.Invoke(deactivate);
                activeObjects.Remove(deactivate);
                deactivateIndex++;
            }
        }

        void UpdateObjectsBackward(float time)
        {
            // Spawn (backwards)
            while (deactivateIndex - 1 >= 0 && time < deactivateList[deactivateIndex - 1].KillTime)
            {
                var deactivate = deactivateList[deactivateIndex - 1];
                deactivate.SetActive(true);
                OnObjectSpawned?.Invoke(deactivate);
                activeObjects.Add(deactivate);
                deactivateIndex--;
            }

            // Despawn (backwards)
            while (activateIndex - 1 >= 0 && time < activateList[activateIndex - 1].StartTime)
            {
                var activate = activateList[activateIndex - 1];
                activate.SetActive(false);
                OnObjectDespawned?.Invoke(activate);
                activeObjects.Remove(activate);
                activateIndex--;
            }
        }
    }
}