using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetRaycast : ModifierActionBase
    {
        // the modifier variables are specifed in the tooltip.

        #region Constructors

        public GetRaycast(bool checkPlayers)
        {
            this.checkPlayers = checkPlayers;
            Name = checkPlayers ? "getRaycastPlayer" : "getRaycast";
            if (checkPlayers)
                SetupModifier(true, "RAYCAST_VAR", "0", "100");
            else
                SetupModifier(true, "RAYCAST_VAR", "Object Group", "0", "100");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Physics;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        public override Sprite Icon => EditorSprites.DownArrow;

        readonly bool checkPlayers;
        int MinIndex => checkPlayers ? 1 : 2;
        int MaxIndex => checkPlayers ? 2 : 3;
        readonly List<Collider2D> forcedOn = new List<Collider2D>();
        readonly List<RaycastHit2D> hitResults = new List<RaycastHit2D>();
        readonly ContactFilter2D triggerFilter = new ContactFilter2D { useTriggers = true, useLayerMask = false };

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not ITransformable transformable)
                return;

            var origin = (Vector2)transformable.GetFullPosition();
            var rotZ = transformable.GetFullRotation(true).z;
            var dir = (Vector2)(Quaternion.Euler(0f, 0f, rotZ) * Vector2.right);
            var min = modifier.GetFloat(MinIndex, 0f, modifierLoop.variables);
            var max = modifier.GetFloat(MaxIndex, 100f, modifierLoop.variables);
            var start = origin + dir * min;
            var length = Mathf.Max(0f, max - min);
            Collider2D self = null;
            if (modifierLoop.reference is BeatmapObject sourceObject && sourceObject.runtimeObject && sourceObject.runtimeObject.visualObject)
                self = sourceObject.runtimeObject.visualObject.collider;
            var candidates = new HashSet<Collider2D>();
            var playerIndexByCollider = checkPlayers ? new Dictionary<Collider2D, int>() : null;
            if (checkPlayers)
            {
                for (int i = 0; i < PlayerManager.inst.players.Count; i++)
                {
                    var player = PlayerManager.inst.players[i];
                    if (!player.RuntimePlayer || !player.RuntimePlayer.CurrentCollider)
                        continue;
                    var col = player.RuntimePlayer.CurrentCollider;
                    candidates.Add(col);
                    playerIndexByCollider[col] = i;
                }
            }
            else
            {
                if (modifierLoop.reference is not IPrefabable prefabable)
                    return;
                var tag = FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables);
                var group = GameData.Current.FindObjectsWithTag(modifier, prefabable, tag);
                for (int i = 0; i < group.Count; i++)
                {
                    var col = group[i].runtimeObject?.visualObject?.collider;
                    if (col && col != self)
                        candidates.Add(col);
                }
            }

            forcedOn.Clear();
            foreach (var col in candidates)
                if (!col.enabled)
                {
                    col.enabled = true;
                    forcedOn.Add(col);
                }
            Physics2D.SyncTransforms();
            hitResults.Clear();
            Physics2D.Raycast(start, dir, triggerFilter, hitResults, length);
            for (int i = 0; i < forcedOn.Count; i++)
                forcedOn[i].enabled = false;
            var didHit = false;
            var point = Vector2.zero;
            var hitDistance = 0f;
            var playerIndex = -1;
            var best = float.MaxValue;
            for (int i = 0; i < hitResults.Count; i++)
            {
                var hit = hitResults[i];
                if (hit.collider == self || !candidates.Contains(hit.collider))
                    continue;
                if (hit.distance >= best)
                    continue;
                best = hit.distance;
                didHit = true;
                point = hit.point;
                hitDistance = hit.distance + min;
                if (checkPlayers && playerIndexByCollider.TryGetValue(hit.collider, out int idx))
                    playerIndex = idx;
            }
            var key = FormatStringVariables(modifier.GetValue(0), modifierLoop.variables);
            modifierLoop.variables[key + "_hit"] = didHit.ToString();
            modifierLoop.variables[key + "_x"] = point.x.ToString();
            modifierLoop.variables[key + "_y"] = point.y.ToString();
            modifierLoop.variables[key + "_distance"] = hitDistance.ToString();
            if (checkPlayers)
                modifierLoop.variables[key + "_index"] = playerIndex.ToString();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            if (!checkPlayers)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 1);
            }
            modifierCard.SingleGenerator(modifier, reference, "Min Distance", MinIndex, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Max Distance", MaxIndex, 100f);
        }

        #endregion
    }
}
