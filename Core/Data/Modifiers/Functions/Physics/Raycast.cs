using System.Collections.Generic;
using UnityEngine;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor;
using BetterLegacy.Editor.Data.Elements;
namespace BetterLegacy.Core.Data.Modifiers.Functions
// Do not change this to be 4 seperate variables. I understand it may seem simplier on the surface. Which is why it was my first implementation. It was my first solution.
// After I tested it with the 4 variables, I found it annoying that I had to define 4 things when I would inheritly use all 4 together. So I inspected the code to find a different solution, led me to the player colliding modifier
// After, I decided to use that, and it was simplier. I tested both possibilities and can confirm within reason, the second outcome is simplier overall.
// The modifier has 1 variable per modifier, with 4 unchanging suffixes, meaning its always N+4 things to remember. (5 things at 1 modifier, 6 things at 2, 7 things at 3, etc)
// With each value being seperate, it's 4 unique variables per modifier, being N*4 (Becomes noticably harder to remember at 2+ raycast)
// This is the reason it should not be reverted to the old method, it will overall make the system coincidentally, more complex.
// I also feel it is very easy to remember "_hit" "_x" "_y" "_direction" "_index" (Hit isn't really useful since direction can be used to check, but I still added it just incase.)

// The decision was not made on impulse.
{
    public class Raycast : ModifierActionBase
    {
        #region Constructors
        readonly bool checkPlayers;
        public Raycast(bool checkPlayers)
        {
            this.checkPlayers = checkPlayers;
            Name = checkPlayers ? "raycastPlayer" : "raycast";
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
