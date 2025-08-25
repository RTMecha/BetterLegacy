using SimpleJSON;

using BetterLegacy.Core.Data.Modifiers;

namespace BetterLegacy.Core.Data.Player
{
    /// <summary>
    /// Controls how a player behaves in a level.
    /// </summary>
    public class PlayerControl : PAObject<PlayerControl>
    {
        public PlayerControl() : base() { }

        #region Values

        /// <summary>
        /// Players' local game mode, separate from the global game mode.
        /// </summary>
        public int gameMode = -1;

        int health = 3;
        /// <summary>
        /// Amount of health the player has.
        /// </summary>
        public int Health { get => RTMath.Clamp(health, 1, int.MaxValue); set => health = RTMath.Clamp(value, 1, int.MaxValue); }

        /// <summary>
        /// Amount of lives the player has until the level requires a restart.
        /// </summary>
        public int lives = -1;

        /// <summary>
        /// Default speed of the player.
        /// </summary>
        public float moveSpeed = 20f;

        /// <summary>
        /// Speed of the players' boost.
        /// </summary>
        public float boostSpeed = 85f;

        /// <summary>
        /// Cooldown between each boost.
        /// </summary>
        public float boostCooldown = 0.1f;

        /// <summary>
        /// Minimum time the player can boost.
        /// </summary>
        public float minBoostTime = 0.07f;

        /// <summary>
        /// Maximum time the player can boost.
        /// </summary>
        public float maxBoostTime = 0.18f;

        /// <summary>
        /// Gravity of <see cref="GameMode.Platformer"/>.
        /// </summary>
        public float jumpGravity = 10f;

        /// <summary>
        /// Intensity of the players' jump in platformer mode.
        /// </summary>
        public float jumpIntensity = 40f;

        /// <summary>
        /// Bounciness physics in platformer mode.
        /// </summary>
        public float bounciness = 0.1f;

        /// <summary>
        /// Amount of times the player can jump.
        /// </summary>
        public int jumpCount = 1;

        /// <summary>
        /// Amount of times the player can boost in the air.
        /// </summary>
        public int jumpBoostCount = 1;

        /// <summary>
        /// Only allow the player to boost in the air if they fall without jumping.
        /// </summary>
        public bool airBoostOnly;

        /// <summary>
        /// Cooldown between each hit.
        /// </summary>
        public float hitCooldown = 2.5f;

        /// <summary>
        /// If the collision of the player is accurate.
        /// </summary>
        public bool collisionAccurate = false;

        /// <summary>
        /// If the player can sprint and sneak.
        /// </summary>
        public bool sprintSneakActive = false;

        public float sprintSpeed = 1.3f;

        public float sneakSpeed = 0.1f;

        /// <summary>
        /// If the player can boost. Managed by both the model and the control.
        /// </summary>
        public bool canBoost = true;

        /// <summary>
        /// Modifier block to run per player tick.
        /// </summary>
        public ModifierBlock TickModifierBlock { get; set; } = new ModifierBlock(ModifierReferenceType.PAPlayer);

        /// <summary>
        /// Modifier block to run per player boost.
        /// </summary>
        public ModifierBlock BoostModifierBlock { get; set; } = new ModifierBlock(ModifierReferenceType.PAPlayer);

        /// <summary>
        /// Modifier block to run per player collision.
        /// </summary>
        public ModifierBlock CollideModifierBlock { get; set; } = new ModifierBlock(ModifierReferenceType.PAPlayer);

        /// <summary>
        /// Modifier block to run per player death.
        /// </summary>
        public ModifierBlock DeathModifierBlock { get; set; } = new ModifierBlock(ModifierReferenceType.PAPlayer);

        #endregion

        #region Methods

        public override void CopyData(PlayerControl orig, bool newID = true)
        {
            id = newID ? GetStringID() : orig.id;
            health = orig.health;
            moveSpeed = orig.moveSpeed;
            boostSpeed = orig.boostSpeed;
            boostCooldown = orig.boostCooldown;
            minBoostTime = orig.minBoostTime;
            maxBoostTime = orig.maxBoostTime;
            jumpGravity = orig.jumpGravity;
            jumpIntensity = orig.jumpIntensity;
            bounciness = orig.bounciness;
            jumpCount = orig.jumpCount;
            jumpBoostCount = orig.jumpBoostCount;
            airBoostOnly = orig.airBoostOnly;
            hitCooldown = orig.hitCooldown;
            collisionAccurate = orig.collisionAccurate;
            sprintSneakActive = orig.sprintSneakActive;
            sprintSpeed = orig.sprintSpeed;
            sneakSpeed = orig.sneakSpeed;
            TickModifierBlock = orig.TickModifierBlock?.Copy();
            BoostModifierBlock = orig.BoostModifierBlock?.Copy();
            CollideModifierBlock = orig.CollideModifierBlock?.Copy();
            DeathModifierBlock = orig.DeathModifierBlock?.Copy();
        }

        public override void ReadJSON(JSONNode jn)
        {
            id = jn["id"] ?? GetStringID();
            if (jn["health"] != null)
                health = jn["health"].AsInt;
            if (jn["move_speed"] != null)
                moveSpeed = jn["move_speed"].AsFloat;
            if (jn["boost_speed"] != null)
                boostSpeed = jn["boost_speed"].AsFloat;
            if (jn["boost_cooldown"] != null)
                boostCooldown = jn["boost_cooldown"].AsFloat;
            if (jn["boost_min_time"] != null)
                minBoostTime = jn["boost_min_time"].AsFloat;
            if (jn["boost_max_time"] != null)
                maxBoostTime = jn["boost_max_time"].AsFloat;
            if (jn["hit_cooldown"] != null)
                hitCooldown = jn["hit_cooldown"].AsFloat;
            if (jn["collision_acc"] != null)
                collisionAccurate = jn["collision_acc"].AsBool;
            if (jn["sprint_sneak"] != null)
                sprintSneakActive = jn["sprint_sneak"].AsBool;
            if (jn["sprint_speed"] != null)
                sprintSpeed = jn["sprint_speed"].AsFloat;
            if (jn["sneak_speed"] != null)
                sneakSpeed = jn["sneak_speed"].AsFloat;

            if (jn["jump_gravity"] != null)
                jumpGravity = jn["jump_gravity"].AsFloat;
            if (jn["jump_intensity"] != null)
                jumpIntensity = jn["jump_intensity"].AsFloat;
            if (jn["bounciness"] != null)
                bounciness = jn["bounciness"].AsFloat;
            if (jn["jump_count"] != null)
                jumpCount = jn["jump_count"].AsInt;
            if (jn["jump_boost_count"] != null)
                jumpBoostCount = jn["jump_boost_count"].AsInt;
            if (jn["air_boost_only"] != null)
                airBoostOnly = jn["air_boost_only"].AsBool;
            if (jn["can_boost"] != null)
                canBoost = jn["can_boost"].AsBool;

            if (jn["tick_modifier_block"] != null)
            {
                TickModifierBlock = ModifierBlock.Parse(jn["tick_modifier_block"]);
                TickModifierBlock.ReferenceType = ModifierReferenceType.PAPlayer;
            }
            if (jn["boost_modifier_block"] != null)
            {
                BoostModifierBlock = ModifierBlock.Parse(jn["boost_modifier_block"]);
                BoostModifierBlock.ReferenceType = ModifierReferenceType.PAPlayer;
            }
            if (jn["collide_modifier_block"] != null)
            {
                CollideModifierBlock = ModifierBlock.Parse(jn["collide_modifier_block"]);
                CollideModifierBlock.ReferenceType = ModifierReferenceType.PAPlayer;
            }
            if (jn["death_modifier_block"] != null)
            {
                DeathModifierBlock = ModifierBlock.Parse(jn["death_modifier_block"]);
                DeathModifierBlock.ReferenceType = ModifierReferenceType.PAPlayer;
            }
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            if (string.IsNullOrEmpty(id))
                id = GetNumberID();
            jn["id"] = id;

            if (health != 3)
                jn["health"] = health;
            if (moveSpeed != 20f)
                jn["move_speed"] = moveSpeed;
            if (boostSpeed != 85f)
                jn["boost_speed"] = boostSpeed;
            if (boostCooldown != 0.1f)
                jn["boost_cooldown"] = boostCooldown;
            if (minBoostTime != 0.07f)
                jn["boost_min_time"] = minBoostTime;
            if (maxBoostTime != 0.18f)
                jn["boost_max_time"] = maxBoostTime;
            if (hitCooldown != 2.5f)
                jn["hit_cooldown"] = hitCooldown;
            if (collisionAccurate)
                jn["collision_acc"] = collisionAccurate;
            if (sprintSneakActive)
                jn["sprint_sneak"] = sprintSneakActive;
            if (sprintSpeed != 1.3f)
                jn["sprint_speed"] = sprintSpeed;
            if (sneakSpeed != 0.1f)
                jn["sneak_speed"] = sneakSpeed;

            if (jumpGravity != 40f)
                jn["jump_gravity"] = jumpGravity;
            if (jumpIntensity != 10f)
                jn["jump_intensity"] = jumpIntensity;
            if (bounciness != 0.1f)
                jn["bounciness"] = bounciness;
            if (jumpCount != 1)
                jn["jump_count"] = jumpCount;
            if (jumpBoostCount != 1)
                jn["jump_boost_count"] = jumpBoostCount;
            if (airBoostOnly)
                jn["air_boost_only"] = airBoostOnly;

            if (canBoost)
                jn["can_boost"] = canBoost;

            if (TickModifierBlock && !TickModifierBlock.Modifiers.IsEmpty())
                jn["tick_modifier_block"] = TickModifierBlock.ToJSON();
            if (BoostModifierBlock && !BoostModifierBlock.Modifiers.IsEmpty())
                jn["boost_modifier_block"] = BoostModifierBlock.ToJSON();
            if (CollideModifierBlock && !CollideModifierBlock.Modifiers.IsEmpty())
                jn["collide_modifier_block"] = CollideModifierBlock.ToJSON();
            if (DeathModifierBlock && !DeathModifierBlock.Modifiers.IsEmpty())
                jn["death_modifier_block"] = DeathModifierBlock.ToJSON();

            return jn;
        }

        #endregion
    }
}
