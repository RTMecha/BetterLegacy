﻿using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;

namespace BetterLegacy.Core.Components
{
    /// <summary>
    /// Object collision component used for modifiers. Detects both mouse and player bullets.
    /// </summary>
    public class Detector : MonoBehaviour
    {
        public BeatmapObject beatmapObject;

        public bool hovered;

        public bool bulletOver;

        List<Collider2D> colliders = new List<Collider2D>();

        void Update() => bulletOver = false;

        void OnMouseEnter() => hovered = true;

        void OnMouseExit() => hovered = false;

        bool CheckCollider(Collider other) => other.tag != Tags.PLAYER && other.gameObject.name.Contains("bullet (Player");
        bool CheckCollider(Collider2D other) => other.tag != Tags.PLAYER && other.gameObject.name.Contains("bullet (Player") && !colliders.Contains(other);

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!CheckCollider(other))
                return;

            bulletOver = true;
            if (!colliders.Contains(other))
                colliders.Add(other);
        }

        void OnTriggerEnter(Collider other)
        {
            if (CheckCollider(other))
                bulletOver = true;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!CheckCollider(other))
                return;

            bulletOver = false;
            if (colliders.Contains(other))
                colliders.Remove(other);
        }

        void OnTriggerExit(Collider other)
        {
            if (CheckCollider(other))
                bulletOver = false;
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (CheckCollider(other))
                bulletOver = true;
        }

        void OnTriggerStay(Collider other)
        {
            if (CheckCollider(other))
                bulletOver = true;
        }
    }
}
