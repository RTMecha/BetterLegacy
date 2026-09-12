using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class BlackHole : PlayerActionBase
    {
        #region Constructors

        public BlackHole(bool mirror, Selector selector) : base(mirror ? "whiteHole" : "blackHole", selector, "0.01") => this.mirror = mirror;

        #endregion

        #region Values

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        readonly bool mirror;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                switch (selector)
                {
                    case Selector.Nearest: {
                            var pos = beatmapObject.GetFullPosition();
                            var player = PlayerManager.inst.GetClosestPlayer(pos);
                            RunOnPlayer(modifier, modifierLoop, player, pos);
                            break;
                        }
                    case Selector.Index: {
                            if (PlayerManager.inst.players.TryGetAt(modifier.GetInt(0, 0, modifierLoop.variables), out PAPlayer player))
                                RunOnPlayer(modifier, modifierLoop, player, beatmapObject.GetFullPosition());
                            break;
                        }
                    case Selector.All: {
                            var pos = beatmapObject.GetFullPosition();
                            foreach (var player in PlayerManager.inst.players)
                                RunOnPlayer(modifier, modifierLoop, player, pos);
                            break;
                        }
                }
            });
        }

        void RunOnPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player, Vector2 pos)
        {
            if (!player.RuntimePlayer || !player.RuntimePlayer.rb)
                return;

            var value = modifier.GetFloat(Index(0), 0.01f, modifierLoop.variables);

            if (value == 0f)
                return;

            var moveDelay = 1f - Mathf.Pow(1f - Mathf.Clamp(value, 0.001f, 1f), Time.deltaTime * 60f * CoreHelper.ForwardPitch);

            if (!player || !player.RuntimePlayer)
                return;

            var transform = player.RuntimePlayer.rb.transform;

            var vector = new Vector3(transform.position.x, transform.position.y);
            var target = new Vector3(pos.x, pos.y);
            if (mirror)
                target = -target;

            transform.position += (mirror ? (target + vector) : (target - vector)) * moveDelay;
        }

        public override void RunOnPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player) { }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            base.RenderModifierCard(modifier, modifierCard, reference, modifyable);
            modifierCard.SingleGenerator(modifier, reference, "Value", Index(0), 1f);
        }

        #endregion
    }
}
