using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using DG.Tweening;
using TMPro;
using XInputDotNetPure;

using BetterLegacy.Companion.Data.Parameters;
using BetterLegacy.Companion.Entity;
using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Editor.Components;

using Ease = BetterLegacy.Core.Animation.Ease;

namespace BetterLegacy.Core.Components.Player
{
    /// <summary>
    /// Modded player component.
    /// </summary>
    public class RTPlayer : MonoBehaviour
    {
        //Player Parent Tree (original):
        //player-complete (has Player component)
        //player-complete/Player
        //player-complete/Player/Player (has OnTriggerEnterPass component)
        //player-complete/Player/Player/death-explosion
        //player-complete/Player/Player/burst-explosion
        //player-complete/Player/Player/spawn-implosion
        //player-complete/Player/boost
        //player-complete/trail (has PlayerTrail component)
        //player-complete/trail/1
        //player-complete/trail/2
        //player-complete/trail/3

        #region Base

        /// <summary>
        /// If multiplayer nametags should display when there are more than one player on-screen.
        /// </summary>
        public static bool ShowNameTags { get; set; }

        /// <summary>
        /// Base player actions.
        /// </summary>
        public MyGameActions Actions { get; set; }

        /// <summary>
        /// Secondary player actions.
        /// </summary>
        public FaceController FaceController { get; set; }

        /// <summary>
        /// Custom player data reference.
        /// </summary>
        public PAPlayer Core { get; set; }

        /// <summary>
        /// Player model reference.
        /// </summary>
        public PlayerModel Model { get; set; }

        /// <summary>
        /// Current index of the player in the players list.
        /// </summary>
        public int playerIndex;

        /// <summary>
        /// How much health the player has when they spawn.
        /// </summary>
        public int initialHealthCount;

        /// <summary>
        /// Coroutine generated from when the player boosts.
        /// </summary>
        public Coroutine boostCoroutine;

        public GameObject canvas;

        public TextMeshPro nametagText;
        public MeshRenderer nametagBase;

        public Text healthText;

        private Image barIm;
        private Image barBaseIm;

        public ParticleSystem burst;
        public ParticleSystem death;
        public ParticleSystem spawn;

        /// <summary>
        /// The players' tail.
        /// </summary>
        public Transform tailParent;

        /// <summary>
        /// The tail tracker used for Dev+ tail parts.
        /// </summary>
        public GameObject tailTracker;

        /// <summary>
        /// Base rigidbody component.
        /// </summary>
        public Rigidbody2D rb;

        CircleCollider2D circleCollider2D;
        PolygonCollider2D polygonCollider2D;

        /// <summary>
        /// The collider that is currently being used.
        /// </summary>
        public Collider2D CurrentCollider => Core && Core.GetControl().collisionAccurate ? polygonCollider2D : circleCollider2D;

        public Transform customObjectParent;

        public RTPlayerObject basePart;
        public RTPlayerObject head;
        public RTPlayerObject face;
        public RTPlayerObject boost;
        public RTPlayerObject boostTail;

        public List<RTPlayerObject> playerObjects = new List<RTPlayerObject>();

        /// <summary>
        /// A list of the players' tail parts. Only includes the tail parts that represent the health.
        /// </summary>
        public List<RTPlayerObject> tailParts = new List<RTPlayerObject>();

        /// <summary>
        /// A list of the custom objects spawned from the players' model.
        /// </summary>
        public List<RTCustomPlayerObject> customObjects = new List<RTCustomPlayerObject>();

        /// <summary>
        /// Used for pathing the players' tail.
        /// </summary>
        public List<MovementPath> path = new List<MovementPath>();

        /// <summary>
        /// A list of images that represent the players' health in GUI image form.
        /// </summary>
        public List<HealthObject> healthObjects = new List<HealthObject>();

        /// <summary>
        /// A list of temporary spawned objects.
        /// </summary>
        public List<EmittedObject> emitted = new List<EmittedObject>();

        #endregion

        #region Game Mode

        /// <summary>
        /// The current gamemode the player is in.
        /// </summary>
        public static GameMode GameMode { get; set; }

        /// <summary>
        /// The total gravity all players have when in platformer mode.
        /// </summary>
        public static float JumpGravity { get; set; } = 1f;

        /// <summary>
        /// The total amount of force all players have when they jump using platformer mode.
        /// </summary>
        public static float JumpIntensity { get; set; } = 1f;

        /// <summary>
        /// If players are in platformer mode.
        /// </summary>
        public static bool JumpMode => GameMode == GameMode.Platformer;

        /// <summary>
        /// Max amount of jumps the players have until they can no longer jump.
        /// </summary>
        public static int MaxJumpCount { get; set; } = 10;

        /// <summary>
        /// Max amount of boosts the players have after their jumps run out.
        /// </summary>
        public static int MaxJumpBoostCount { get; set; } = 1;

        /// <summary>
        /// Local jump gravity.
        /// </summary>
        public float jumpGravity = 10f;

        /// <summary>
        /// Local jump intensity.
        /// </summary>
        public float jumpIntensity = 40f;

        /// <summary>
        /// Player bounciness.
        /// </summary>
        public float bounciness = 0.1f;

        /// <summary>
        /// Local max amount of times the player can jump until they can no longer do so. -1 is treated as infinity.
        /// </summary>
        public int jumpCount = 1;

        /// <summary>
        /// Local max amount of times the player can boost after their jumps run out until they can no longer do so. -1 is treated as infinity.
        /// </summary>
        public int jumpBoostCount = 1;

        float jumpBoostMultiplier = 0.5f; // to make sure the jump goes about the same distance as left and right boost

        int currentJumpCount = 0;
        int currentJumpBoostCount = 0;

        #endregion

        #region Velocities

        public static bool MultiplyByPitch => GameData.Current && GameData.Current.data && GameData.Current.data.level.multiplyPlayerSpeed;

        /// <summary>
        /// How fast all players are.
        /// </summary>
        public static float SpeedMultiplier { get; set; } = 1f;

        /// <summary>
        /// The current force to apply to players.
        /// </summary>
        public static Vector2 PlayerForce { get; set; }

        public Vector3 lastPos;
        public float lastMoveHorizontal;
        public float lastMoveVertical;
        public Vector3 lastVelocity;

        public Vector2 lastMovement;

        public float startHurtTime;
        public float startBoostTime;
        public float maxBoostTime = 0.18f;
        public float minBoostTime = 0.07f;
        public float boostCooldown = 0.1f;
        public float idleSpeed = 20f;
        public float boostSpeed = 85f;

        public float SprintSneakSpeed => Model.basePart.sprintSneakActive ? FaceController.Sprint.IsPressed ? 1.3f : FaceController.Sneak.IsPressed ? 0.1f : 1f : 1f;

        /// <summary>
        /// If negative zoom should be included with calculating player bounds.
        /// </summary>
        public bool includeNegativeZoom = false;
        /// <summary>
        /// The kind of movement used for the player. Will move mouse mode to GameModes.
        /// </summary>
        public MovementMode movementMode = MovementMode.KeyboardController;

        /// <summary>
        /// Current rotation method assigned by the player model.
        /// </summary>
        public RotateMode rotateMode = RotateMode.RotateToDirection;

        public Vector2 lastMousePos;

        public bool stretch = true;
        public float stretchAmount = 0.1f;
        public int stretchEasing = 6;
        public Vector2 stretchVector = Vector2.zero;

        #endregion

        #region Enums

        /// <summary>
        /// How the player should rotate.
        /// </summary>
        public enum RotateMode
        {
            /// <summary>
            /// The regular method of rotation. Rotates the player head to the direction the player is moving in.
            /// </summary>
            RotateToDirection,
            /// <summary>
            /// Does not rotate.
            /// </summary>
            None,
            /// <summary>
            /// Mirrors  the player model depending on whether they're moving left or right.
            /// </summary>
            FlipX,
            /// <summary>
            /// Flips the player model depending on whether they're moving up or down.
            /// </summary>
            FlipY,
            /// <summary>
            /// Rotates the player like <see cref="RotateMode.RotateToDirection"/>, except rotation is reset when the player is not moving.
            /// </summary>
            RotateReset,
            /// <summary>
            /// Rotates the player like <see cref="RotateToDirection"/> but also mirrors them like <see cref="FlipX"/>.
            /// </summary>
            RotateFlipX,
            /// <summary>
            /// Rotates the player like <see cref="RotateToDirection"/> but also flips them like <see cref="FlipY"/>.
            /// </summary>
            RotateFlipY
        }

        /// <summary>
        /// Unused. Will move mouse to <see cref="GameMode"/>.
        /// </summary>
        public enum MovementMode
        {
            KeyboardController,
            Mouse
        }

        #endregion

        #region Tail

        /// <summary>
        /// How the player tail should update.
        /// </summary>
        public static TailUpdateMode UpdateMode { get; set; } = TailUpdateMode.FixedUpdate;

        public bool tailGrows = false;
        public bool showBoostTail = false;
        public float tailDistance = 2f;
        public int tailMode;
        public enum TailUpdateMode
        {
            Update,
            FixedUpdate,
            LateUpdate
        }

        #endregion

        #region States

        #region Global

        /// <summary>
        /// If the boost sound should play when the player boosts.
        /// </summary>
        public static bool PlayBoostSound { get; set; }

        /// <summary>
        /// If the boost recover sound should play when the player can boost again.
        /// </summary>
        public static bool PlayBoostRecoverSound { get; set; }

        /// <summary>
        /// If the shoot sound should play when the player shoots.
        /// </summary>
        public static bool PlayShootSound { get; set; }

        /// <summary>
        /// If custom assets should be loaded from a global source.
        /// </summary>
        public static bool AssetsGlobal { get; set; }

        /// <summary>
        /// If zen mode in editor should also consider solid.
        /// </summary>
        public static bool ZenEditorIncludesSolid { get; set; }

        /// <summary>
        /// If players can take damage from another players' bullet.
        /// </summary>
        public static bool AllowPlayersToTakeBulletDamage { get; set; }

        /// <summary>
        /// If players are allowed out of bounds.
        /// </summary>
        public static bool OutOfBounds { get; set; } = false;

        /// <summary>
        /// If all players' boosts should be locked.
        /// </summary>
        public static bool LockBoost { get; set; } = false;

        /// <summary>
        /// If players can take damage from colliding with other players.
        /// </summary>
        public static bool AllowPlayersToHitOthers { get; set; }

        /// <summary>
        /// If players boosting changes their collision trigger state.
        /// </summary>
        public static bool ChangeIsTriggerOnBoost { get; set; }

        #endregion

        public bool colliding;
        public bool triggerColliding;
        public bool isColliderTrigger;
        public bool updated;
        public bool playerNeedsUpdating;

        public bool isTakingHit;
        public bool isBoosting;
        public bool isBoostCancelled;
        public bool isDead = true;

        public bool isKeyboard;
        public bool animatingBoost;

        public bool isSpawning;

        public float time;

        public static bool resetVelocity = true;

        /// <summary>
        /// If the player can take damage.
        /// </summary>
        public bool CanTakeDamage
        {
            get => RTBeatmap.Current.challengeMode.Damageable && !CoreHelper.Paused && !CoreHelper.IsEditing && canTakeDamage;
            set => canTakeDamage = value;
        }

        /// <summary>
        /// If the player can move.
        /// </summary>
        public bool CanMove
        {
            get => RTLevel.Current && RTLevel.Current.eventEngine && RTLevel.Current.eventEngine.playersCanMove && canMove;
            set => canMove = value;
        }

        /// <summary>
        /// If the player can rotate.
        /// </summary>
        public bool CanRotate
        {
            get => RTLevel.Current && RTLevel.Current.eventEngine && RTLevel.Current.eventEngine.playersCanMove && canRotate;
            set => canRotate = value;
        }

        /// <summary>
        /// If the player can boost.
        /// </summary>
        public bool CanBoost
        {
            get => CoreHelper.InEditorPreview && canBoost && !isBoosting && (!Core || Core.GetControl().canBoost) && !CoreHelper.Paused && !CoreHelper.IsUsingInputField;
            set => canBoost = value;
        }

        /// <summary>
        /// If boosting can be cancelled.
        /// </summary>
        public bool CanCancelBoosting => isBoosting && !isBoostCancelled;

        /// <summary>
        /// If the player is alive.
        /// </summary>
        public bool Alive => Core && Core.Health > 0 && !isDead;

        /// <summary>
        /// Text of the nametag.
        /// </summary>
        public string NametagText
        {
            get
            {
                try
                {
                    return !Core ? string.Empty : "<#" + LSColors.ColorToHex(ThemeManager.inst.Current.GetPlayerColor(PlayersData.Current.GetMaxIndex(playerIndex, 4))) + ">Player " + (PlayersData.Current.GetMaxIndex(playerIndex, 4) + 1).ToString() + " " + RTString.ConvertHealthToEquals(Core.Health, initialHealthCount);
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Updates player properties based on <see cref="LevelData"/>.
        /// </summary>
        public static void SetGameDataProperties()
        {
            try
            {
                if (!GameData.Current || !GameData.Current.data || !GameData.Current.data.level)
                    return;

                var levelData = GameData.Current.data.level;
                LockBoost = levelData.lockBoost;
                SpeedMultiplier = levelData.speedMultiplier;
                GameMode = (GameMode)levelData.gameMode;
                JumpGravity = levelData.jumpGravity;
                JumpIntensity = levelData.jumpIntensity;
                MaxJumpCount = levelData.maxJumpCount;
                MaxJumpBoostCount = levelData.maxJumpBoostCount;
                PAPlayer.MaxHealth = levelData.maxHealth;

                if (CoreHelper.InEditor && !PlayerManager.NoPlayers)
                {
                    foreach (var player in PlayerManager.Players)
                        player.Health = RTBeatmap.Current && RTBeatmap.Current.challengeMode.DefaultHealth > 0 ? RTBeatmap.Current.challengeMode.DefaultHealth : player.GetControl()?.Health ?? 3;
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Could not set properties {ex}");
            }

        }

        #region Internal

        float timeOffset;

        float timeHit;
        float timeHitOffset;

        bool canBoost = true;
        bool canMove = true;
        bool canRotate = true;
        bool canTakeDamage;

        bool canShoot = true;

        bool queuedBoost;

        bool animatingRotateReset;
        bool moved;
        float timeNotMovingOffset;
        Vector2 lastMovementTotal;
        RTAnimation rotateResetAnimation;

        #endregion

        #endregion

        #region Delegates

        public delegate void PlayerHitDelegate(int _health, Vector3 _pos);

        public delegate void PlayerDeathDelegate(Vector3 _pos);

        public event PlayerHitDelegate playerHitEvent;

        public event PlayerDeathDelegate playerDeathEvent;

        #endregion

        #region Animations

        public AnimationController animationController;

        #region Main

        public RTAnimation spawnAnimation;

        public RTAnimation boostAnimation;

        public RTAnimation boostEndAnimation;

        public RTAnimation hitAnimation;

        public RTAnimation deathAnimation;

        #endregion

        #region Custom

        public RTAnimation spawnAnimationCustom;

        public RTAnimation boostAnimationCustom;

        public RTAnimation boostEndAnimationCustom;

        public RTAnimation healAnimationCustom;

        public RTAnimation hitAnimationCustom;

        public RTAnimation deathAnimationCustom;

        public RTAnimation shootAnimationCustom;

        public RTAnimation jumpAnimationCustom;

        #endregion

        void InitSpawnAnimation()
        {
            if (spawnAnimation)
            {
                animationController.Remove(spawnAnimation.id);
                spawnAnimation = null;
            }

            spawnAnimation = new RTAnimation("Player Spawn Animation");
            spawnAnimation.animationHandlers = new List<AnimationHandlerBase>()
            {
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 0f, Ease.Linear),
                    new FloatKeyframe(0.2f, 1.2f, Ease.SineOut),
                    new FloatKeyframe(0.23333333f, 1f, Ease.SineInOut),
                }, x =>
                {
                    if (transform)
                        transform.localScale = new Vector3(x, x, 1f);
                }, interpolateOnComplete: true), // base
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 0f, Ease.Linear),
                    new FloatKeyframe(0.23333333f, 1.2f, Ease.SineOut),
                    new FloatKeyframe(0.36666667f, 0.8f, Ease.SineInOut),
                    new FloatKeyframe(0.43333334f, 1.2f, Ease.SineInOut),
                    new FloatKeyframe(0.5f, 0.8f, Ease.SineInOut),
                    new FloatKeyframe(0.56666666f, 1.2f, Ease.SineInOut),
                    new FloatKeyframe(0.6333333f, 0.8f, Ease.SineInOut),
                    new FloatKeyframe(0.7f, 1.2f, Ease.SineInOut),
                    new FloatKeyframe(0.76666665f, 1.2f, Ease.SineInOut),
                    new FloatKeyframe(0.8333333f, 0.8f, Ease.SineInOut),
                    new FloatKeyframe(0.9f, 1.2f, Ease.SineInOut),
                    new FloatKeyframe(0.93333334f, 0.8f, Ease.SineInOut),
                    new FloatKeyframe(1f, 1f, Ease.SineInOut),
                }, x =>
                {
                    if (rb && rb.transform)
                        rb.transform.localScale = new Vector3(x, x, 1f);
                }, interpolateOnComplete: true), // Player
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 0f, Ease.Linear),
                    new FloatKeyframe(0.33333334f, 0f, Ease.Linear),
                    new FloatKeyframe(0.43333334f, 1f, Ease.BackOut),
                    new FloatKeyframe(0.5f, 1f, Ease.Linear),
                }, x =>
                {
                    if (tailParts.Count > 0 && tailParts[0].parent)
                        tailParts[0].parent.localScale = new Vector3(x, x, 1f);
                }, interpolateOnComplete: true), // Trail 1
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 0f, Ease.Linear),
                    new FloatKeyframe(0.43333334f, 0f, Ease.Linear),
                    new FloatKeyframe(0.53333336f, 1f, Ease.BackOut),
                    new FloatKeyframe(0.6f, 1f, Ease.Linear),
                }, x =>
                {
                    if (tailParts.Count > 1 && tailParts[1].parent)
                        tailParts[1].parent.localScale = new Vector3(x, x, 1f);
                }, interpolateOnComplete: true), // Trail 2
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 0f, Ease.Linear),
                    new FloatKeyframe(0.53333336f, 0f, Ease.Linear),
                    new FloatKeyframe(0.6333333f, 1f, Ease.BackOut),
                    new FloatKeyframe(0.7f, 1f, Ease.Linear),
                }, x =>
                {
                    if (tailParts.Count > 2 && tailParts[2].parent)
                        tailParts[2].parent.localScale = new Vector3(x, x, 1f);
                }, interpolateOnComplete: true), // Trail 3
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 1.4f, Ease.Linear),
                    new FloatKeyframe(1f, 1.4f, Ease.Linear),
                    new FloatKeyframe(1.1f, 0f, Ease.Linear),
                }, x =>
                {
                    if (boost != null && boost.parent)
                        boost.parent.localScale = new Vector3(x, x, 0.2f);
                }, interpolateOnComplete: true), // Boost
            };
            spawnAnimation.onComplete = () =>
            {
                animationController.Remove(spawnAnimation.id);
                spawnAnimation = null;

                InitAfterSpawn();
            };
            spawnAnimation.events = new List<Animation.AnimationEvent>
            {
                new Animation.AnimationEvent(0.04f, InitMidSpawn),
            };
            animationController.Play(spawnAnimation);

            if (spawnAnimationCustom)
                animationController.Play(spawnAnimationCustom);
        }

        void InitBoostAnimation()
        {
            if (hitAnimation)
                return;

            if (boostAnimation)
            {
                animationController.Remove(boostAnimation.id);
                boostAnimation = null;
            }

            boostAnimation = new RTAnimation("Player Boost Animation");
            boostAnimation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                {
                    new Vector3Keyframe(0f, new Vector3(1f, 1f, 1f), Ease.Linear),
                    new Vector3Keyframe(0.02f, new Vector3(1.2f, 1f, 1f), Ease.SineOut),
                }, vector => { if (rb) rb.transform.localScale = vector; }, interpolateOnComplete: true), // rb
                new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                {
                    new Vector3Keyframe(0f, new Vector3(0f, 0f, 0.2f), Ease.Linear),
                    new Vector3Keyframe(0.05f, new Vector3(0f, 0f, 0.2f), Ease.Linear),
                    new Vector3Keyframe(0.1f, new Vector3(1.5f, 1.5f, 0.2f), Ease.SineOut),
                }, vector => { if (boost && boost.parent) boost.parent.localScale = vector; }, interpolateOnComplete: true), // boost
                new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                {
                    new Vector3Keyframe(0f, new Vector3(1f, 1f, 1f), Ease.Linear),
                    new Vector3Keyframe(0.09f, new Vector3(1.2f, 0.8f, 1f), Ease.SineOut),
                }, vector =>
                {
                    for (int i = 0; i < tailParts.Count; i++)
                    {
                        if (tailParts[i].parent)
                            tailParts[i].parent.localScale = vector;
                    }

                }, interpolateOnComplete: true), // tail parts
            };
            boostAnimation.onComplete = () =>
            {
                animationController.Remove(boostAnimation.id);
                boostAnimation = null;
            };
            animationController.Play(boostAnimation);

            if (boostAnimationCustom)
                animationController.Play(boostAnimationCustom);
        }

        void InitBoostEndAnimation()
        {
            if (hitAnimation)
                return;

            if (boostAnimation)
            {
                animationController.Remove(boostAnimation.id);
                boostAnimation = null;
            }
            
            if (boostEndAnimation)
            {
                animationController.Remove(boostEndAnimation.id);
                boostEndAnimation = null;
            }

            boostEndAnimation = new RTAnimation("Player Boost Animation");
            boostEndAnimation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                {
                    new Vector3Keyframe(0f, rb.transform.localScale, Ease.Linear),
                    new Vector3Keyframe(0.022222223f, new Vector3(1f, 1f, 1f), Ease.SineInOut),
                }, vector => { if (rb) rb.transform.localScale = vector; }, interpolateOnComplete: true), // rb
                new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                {
                    new Vector3Keyframe(0f, boost.parent.localScale, Ease.Linear),
                    new Vector3Keyframe(0.033333335f, new Vector3(0f, 0f, 0.2f), Ease.SineInOut),
                }, vector => { if (boost && boost.parent) boost.parent.localScale = vector; }, interpolateOnComplete: true), // boost
                new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                {
                    new Vector3Keyframe(0f, new Vector3(1.2f, 0.8f, 1f), Ease.Linear),
                    new Vector3Keyframe(0.08888889f, new Vector3(1f, 1f, 1f), Ease.SineInOut),
                }, vector =>
                {
                    for (int i = 0; i < tailParts.Count; i++)
                    {
                        if (tailParts[i].parent)
                            tailParts[i].parent.localScale = vector;
                    }

                }, interpolateOnComplete: true), // tail parts
            };
            boostEndAnimation.onComplete = () =>
            {
                animationController.Remove(boostEndAnimation.id);
                boostEndAnimation = null;
            };
            animationController.Play(boostEndAnimation);

            if (boostEndAnimationCustom)
                animationController.Play(boostEndAnimationCustom);
        }

        void InitHealAnimation()
        {
            if (hitAnimation)
            {
                animationController.Remove(hitAnimation.id);
                hitAnimation = null;
            }

            var healAnimation = new RTAnimation("Player Heal Animation");
            healAnimation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                {
                    new Vector3Keyframe(0f, new Vector3(1f, 1f, 1f), Ease.Linear),
                    new Vector3Keyframe(0.1f, new Vector3(1.2f, 1f, 1f), Ease.SineOut),
                    new Vector3Keyframe(0.2f, new Vector3(1f, 1f, 1f), Ease.SineIn),
                }, vector => { if (rb) rb.transform.localScale = vector; }, interpolateOnComplete: true), // rb
                new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                {
                    new Vector3Keyframe(0f, new Vector3(0f, 0f, 0.2f), Ease.Linear),
                    new Vector3Keyframe(0.05f, new Vector3(0f, 0f, 0.2f), Ease.Linear),
                    new Vector3Keyframe(0.15f, new Vector3(1.5f, 1.5f, 0.2f), Ease.SineOut),
                    new Vector3Keyframe(0.25f, new Vector3(0f, 0f, 0.2f), Ease.SineIn),
                }, vector => { if (boost && boost.parent) boost.parent.localScale = vector; }, interpolateOnComplete: true), // boost
            };

            List<AnimationHandlerBase> animationHandlers = new List<AnimationHandlerBase>();
            float t = 0f;
            for (int i = 0; i < tailParts.Count; i++)
            {
                var tailPart = tailParts[i];
                animationHandlers.Add(new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                {
                    new Vector3Keyframe(0f + t, new Vector3(1f, 1f, 1f), Ease.Linear),
                    new Vector3Keyframe(0.09f + t, new Vector3(1.2f, 1.2f, 1f), Ease.SineOut),
                    new Vector3Keyframe(0.13f + t, new Vector3(1f, 1f, 1f), Ease.SineIn),
                }, vector =>
                {
                    if (tailPart.parent)
                        tailPart.parent.localScale = vector;
                }, interpolateOnComplete: true));
                t += 0.02f;
            }
            healAnimation.animationHandlers.AddRange(animationHandlers);

            healAnimation.onComplete = () =>
            {
                animationController.Remove(healAnimation.id);
                healAnimation = null;
            };
            animationController.Play(healAnimation);

            if (healAnimationCustom)
                animationController.Play(healAnimationCustom);
        }

        void InitHitAnimation()
        {
            animationController.RemoveName("Player Heal Animation");

            if (hitAnimation)
            {
                animationController.Remove(hitAnimation.id);
                hitAnimation = null;
            }

            hitAnimation = new RTAnimation("Player Hit Animation");
            hitAnimation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                {
                    new Vector3Keyframe(0f, new Vector3(1.2f, 1.2f, 1f), Ease.Linear),
                    new Vector3Keyframe(0.1666667f, new Vector3(0.8f, 0.8f, 1f), Ease.SineInOut),
                    new Vector3Keyframe(0.4583333f, new Vector3(1.2f, 1.2f, 1f), Ease.SineInOut),
                    new Vector3Keyframe(0.6666666f, new Vector3(0.8f, 0.8f, 1f), Ease.SineInOut),
                    new Vector3Keyframe(0.8749999f, new Vector3(1.2f, 1.2f, 1f), Ease.SineInOut),
                    new Vector3Keyframe(1.083333f, new Vector3(0.8f, 0.8f, 1f), Ease.SineInOut),
                    new Vector3Keyframe(1.25f, new Vector3(1.2f, 1.2f, 1f), Ease.SineInOut),
                    new Vector3Keyframe(1.416667f, new Vector3(1.2f, 1.2f, 1f), Ease.SineInOut),
                    new Vector3Keyframe(1.625f, new Vector3(0.8f, 0.8f, 1f), Ease.SineInOut),
                    new Vector3Keyframe(1.875f, new Vector3(1.2f, 1.2f, 1f), Ease.SineInOut),
                    new Vector3Keyframe(2.083333f, new Vector3(0.8f, 0.8f, 1f), Ease.SineInOut),
                    new Vector3Keyframe(2.25f, new Vector3(1.2f, 1.2f, 1f), Ease.SineInOut),
                    new Vector3Keyframe(2.375f, new Vector3(0.8f, 0.8f, 1f), Ease.SineInOut),
                    new Vector3Keyframe(2.5f, new Vector3(0.8f, 0.8f, 1f), Ease.SineInOut),
                }, vector => { if (rb && !isBoosting) rb.transform.localScale = vector; }, interpolateOnComplete: true), // rb
                new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                {
                    new Vector3Keyframe(0f, new Vector3(0f, 0f, 0.2f), Ease.Linear),
                    new Vector3Keyframe(0.1f, new Vector3(1.5f, 1.5f, 0.2f), Ease.SineOut),
                    new Vector3Keyframe(2.4f, new Vector3(1.5f, 1.5f, 0.2f), Ease.SineOut),
                    new Vector3Keyframe(2.5f, new Vector3(0f, 0f, 0.2f), Ease.SineOut),
                }, vector => { if (boost && boost.parent && !isBoosting) boost.parent.localScale = vector; }, interpolateOnComplete: true), // boost
            };
            hitAnimation.onComplete = () =>
            {
                animationController.Remove(hitAnimation.id);
                hitAnimation = null;
            };
            animationController.Play(hitAnimation);

            if (hitAnimationCustom)
                animationController.Play(hitAnimationCustom);
        }

        void InitDeathAnimation()
        {
            if (deathAnimation)
            {
                animationController.Remove(deathAnimation.id);
                deathAnimation = null;
            }

            deathAnimation = new RTAnimation("Player Death Animation");
            deathAnimation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                {
                    new Vector3Keyframe(0f, new Vector3(1.1f, 1.1f, 1f), Ease.Linear),
                    new Vector3Keyframe(0.2f, new Vector3(1.4f, 1.4f, 1f), Ease.SineOut),
                    new Vector3Keyframe(0.4f, Vector3.zero, Ease.SineOut),
                    new Vector3Keyframe(0.6f, Vector3.zero, Ease.Linear),
                }, vector => { if (rb) rb.transform.localScale = vector; }), // rb
                new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                {
                    new Vector3Keyframe(0f, new Vector3(1f, 1f, 1f), Ease.Linear),
                    new Vector3Keyframe(0.2f, Vector3.zero, Ease.SineIn),
                }, vector => { if (tailParent) tailParent.localScale = vector; }), // boost
            };
            deathAnimation.events = new List<Animation.AnimationEvent>
            {
                new Animation.AnimationEvent(0f, PlayDeathParticles),
                new Animation.AnimationEvent(0.6f, ClearObjects),
            };
            deathAnimation.onComplete = () =>
            {
                animationController.Remove(deathAnimation.id);
                deathAnimation = null;
            };
            animationController.Play(deathAnimation);

            if (deathAnimationCustom)
                animationController.Play(deathAnimationCustom);
        }

        #endregion

        #region Init

        void Awake()
        {
            customObjectParent = Creator.NewGameObject("Custom Objects", transform).transform;
            customObjectParent.transform.localPosition = Vector3.zero;

            animationController = gameObject.AddComponent<AnimationController>();
            var anim = gameObject.GetComponent<Animator>();
            //anim.keepAnimatorControllerStateOnDisable = true;
            anim.enabled = false;

            var rb = transform.Find("Player").gameObject;
            this.rb = rb.GetComponent<Rigidbody2D>();

            basePart = new RTPlayerObject
            {
                id = "0",
                parent = transform,
                gameObject = rb,
            };
            playerObjects.Add(basePart);

            var circleCollider = rb.GetComponent<CircleCollider2D>();

            circleCollider.enabled = false;

            var polygonCollider = rb.AddComponent<PolygonCollider2D>();
            circleCollider2D = circleCollider;
            polygonCollider2D = polygonCollider;

            if (CoreHelper.InEditor)
                rb.AddComponent<PlayerSelector>().player = this;

            var head = transform.Find("Player/Player").gameObject;

            var headMesh = head.GetComponent<MeshFilter>();
            var headRenderer = head.GetComponent<MeshRenderer>();

            polygonCollider.CreateCollider(headMesh);

            polygonCollider.isTrigger = CoreHelper.InEditor && ZenEditorIncludesSolid;
            polygonCollider.enabled = false;
            circleCollider.enabled = true;

            circleCollider.isTrigger = CoreHelper.InEditor && ZenEditorIncludesSolid;
            rb.GetComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            DestroyImmediate(rb.GetComponent<OnTriggerEnterPass>());

            var playerCollision = rb.AddComponent<PlayerCollision>();
            playerCollision.player = this;

            burst = head.transform.Find("burst-explosion").GetComponent<ParticleSystem>();
            death = head.transform.Find("death-explosion").GetComponent<ParticleSystem>();
            spawn = head.transform.Find("spawn-implosion").GetComponent<ParticleSystem>();

            var mat = death.GetComponent<ParticleSystemRenderer>().trailMaterial;

            var headTrail = Creator.NewGameObject("super-trail", head.transform);
            headTrail.transform.localPosition = Vector3.zero;
            headTrail.layer = 8;

            var headTrailRenderer = headTrail.AddComponent<TrailRenderer>();
            headTrailRenderer.material = mat;

            var headParticles = Creator.NewGameObject("super-particles", head.transform);
            headParticles.transform.localPosition = Vector3.zero;
            headParticles.layer = 8;

            var headParticleSystem = headParticles.AddComponent<ParticleSystem>();
            var headParticleSystemRenderer = headParticles.GetComponent<ParticleSystemRenderer>();

            var headParticleSystemMain = headParticleSystem.main;
            headParticleSystemMain.simulationSpace = ParticleSystemSimulationSpace.World;
            headParticleSystemMain.playOnAwake = false;
            headParticleSystemRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            headParticleSystemRenderer.alignment = ParticleSystemRenderSpace.View;

            headParticleSystemRenderer.trailMaterial = mat;
            headParticleSystemRenderer.material = mat;

            this.head = new RTPlayerObject
            {
                id = "73362742",
                parent = rb.transform,
                gameObject = head,
                meshFilter = headMesh,
                renderer = headRenderer,
                trailRenderer = headTrailRenderer,
                particleSystem = headParticleSystem,
                particleSystemRenderer = headParticleSystemRenderer,
            };
            playerObjects.Add(this.head);

            var spawnPos = rb.transform.localPosition;

            var faceBase = Creator.NewGameObject("face-base", rb.transform);
            faceBase.transform.localPosition = Vector3.zero;

            var faceParent = Creator.NewGameObject("face-parent", faceBase.transform);
            faceParent.transform.localPosition = Vector3.zero;
            faceParent.transform.localRotation = Quaternion.identity;

            face = new RTPlayerObject
            {
                id = "6",
                parent = faceBase.transform,
                gameObject = faceParent,
            };
            playerObjects.Add(face);

            path.Add(new MovementPath(spawnPos, Quaternion.identity, rb.transform)); // base path

            tailParent = transform.Find("trail");
            tailTracker = Creator.NewGameObject("tail-tracker", rb.transform);
            tailTracker.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            tailTracker.transform.localRotation = Quaternion.identity;

            var boost = transform.Find("Player/boost").gameObject;
            boost.transform.localScale = Vector3.zero;

            var boostBase = Creator.NewGameObject("Boost Base", transform.Find("Player"));
            boostBase.transform.localPosition = Vector3.zero;
            boostBase.transform.localRotation = Quaternion.identity;
            boostBase.layer = 8;
            boost.transform.SetParent(boostBase.transform);
            boost.transform.localPosition = Vector3.zero;
            boost.transform.localRotation = Quaternion.identity;

            var boostTrail = Creator.NewGameObject("boost-trail", boost.transform.parent);
            boostTrail.transform.localPosition = Vector3.zero;
            boostTrail.layer = 8;

            var boostTrailRenderer = boostTrail.AddComponent<TrailRenderer>();
            boostTrailRenderer.material = mat;

            var boostParticles = Creator.NewGameObject("boost-particles", boost.transform.parent);
            boostParticles.transform.localPosition = Vector3.zero;
            boostParticles.transform.localScale = Vector3.one;
            boostParticles.layer = 8;

            var boostParticleSystem = boostParticles.AddComponent<ParticleSystem>();
            var boostParticleSystemRenderer = boostParticles.GetComponent<ParticleSystemRenderer>();

            var main = boostParticleSystem.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = false;
            main.playOnAwake = false;
            boostParticleSystemRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            boostParticleSystemRenderer.alignment = ParticleSystemRenderSpace.View;

            boostParticleSystemRenderer.trailMaterial = mat;
            boostParticleSystemRenderer.material = mat;

            this.boost = new RTPlayerObject
            {
                id = "1",
                parent = boostBase.transform,
                gameObject = boost,
                meshFilter = boost.GetComponent<MeshFilter>(),
                renderer = boost.GetComponent<MeshRenderer>(),
                trailRenderer = boostTrailRenderer,
                particleSystem = boostParticleSystem,
                particleSystemRenderer = boostParticleSystemRenderer,
            };
            playerObjects.Add(this.boost);

            var boostTail = boostBase.Duplicate(transform.Find("trail"), "Boost Tail");
            boostTail.layer = 8;

            var child = boostTail.transform.GetChild(0);

            var boostDelayTracker = boostTail.AddComponent<PlayerDelayTracker>();
            boostDelayTracker.leader = tailTracker.transform;
            boostDelayTracker.player = this;

            this.boostTail = new RTPlayerObject
            {
                id = "2",
                parent = boostTail.transform,
                gameObject = child.gameObject,
                meshFilter = child.GetComponent<MeshFilter>(),
                renderer = child.GetComponent<MeshRenderer>(),
                trailRenderer = boostTail.GetComponentInChildren<TrailRenderer>(),
                particleSystem = boostTail.GetComponentInChildren<ParticleSystem>(),
                particleSystemRenderer = boostTail.GetComponentInChildren<ParticleSystemRenderer>(),
                delayTracker = boostDelayTracker,
            };
            playerObjects.Add(this.boostTail);

            path.Add(new MovementPath(spawnPos, Quaternion.identity, boostTail.transform, showBoostTail));

            for (int i = 1; i < 4; i++)
            {
                var name = $"Tail {i} Base";
                var tailBase = Creator.NewGameObject(name, transform.Find("trail"));
                var tail = tailParent.Find($"{i}");
                tail.SetParent(tailBase.transform);
                tailBase.layer = 8;

                var playerDelayTracker = tailBase.AddComponent<PlayerDelayTracker>();
                playerDelayTracker.player = this;
                playerDelayTracker.leader = tailTracker.transform;

                var tailParticles = Creator.NewGameObject("tail-particles", tailBase.transform);
                tailParticles.transform.localPosition = Vector3.zero;
                tailParticles.layer = 8;

                var tailParticleSystem = tailParticles.AddComponent<ParticleSystem>();
                var tailParticleSystemRenderer = tailParticles.GetComponent<ParticleSystemRenderer>();

                var tailPSMain = tailParticleSystem.main;
                tailPSMain.simulationSpace = ParticleSystemSimulationSpace.World;
                tailPSMain.playOnAwake = false;
                tailParticleSystemRenderer.renderMode = ParticleSystemRenderMode.Mesh;
                tailParticleSystemRenderer.alignment = ParticleSystemRenderSpace.View;

                tailParticleSystemRenderer.trailMaterial = mat;
                tailParticleSystemRenderer.material = mat;

                var tailPart = new RTPlayerObject
                {
                    id = (i + 99).ToString(),
                    parent = tailBase.transform,
                    gameObject = tail.gameObject,
                    meshFilter = tail.GetComponent<MeshFilter>(),
                    renderer = tail.GetComponent<MeshRenderer>(),
                    delayTracker = playerDelayTracker,
                    trailRenderer = tail.GetComponent<TrailRenderer>(),
                    particleSystem = tailParticleSystem,
                    particleSystemRenderer = tailParticleSystemRenderer,
                };
                tailParts.Add(tailPart);
                playerObjects.Add(tailPart);
                tail.transform.localPosition = new Vector3(0f, 0f, 0.1f);
                path.Add(new MovementPath(spawnPos, Quaternion.identity, tailBase.transform));
            }

            path.Add(new MovementPath(spawnPos, Quaternion.identity, null));

            healthText = PlayerManager.healthImages.Duplicate(PlayerManager.healthParent, $"Health {playerIndex}").GetComponent<Text>();

            for (int i = 0; i < 3; i++)
                healthObjects.Add(new HealthObject(healthText.transform.GetChild(i).gameObject, healthText.transform.GetChild(i).GetComponent<Image>()));

            var barBase = Creator.NewUIObject("Bar Base", healthText.transform);

            var barBaseLE = barBase.AddComponent<LayoutElement>();
            barBaseIm = barBase.AddComponent<Image>();

            barBaseLE.ignoreLayout = true;
            barBase.transform.AsRT().anchoredPosition = new Vector2(-100f, 0f);
            barBase.transform.AsRT().pivot = new Vector2(0f, 0.5f);
            barBase.transform.AsRT().sizeDelta = new Vector2(200f, 32f);

            var bar = Creator.NewUIObject("Bar", barBase.transform);

            barIm = bar.AddComponent<Image>();
            new RectValues(Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 0.5f), new Vector2(0f, 0f)).AssignToRectTransform(barIm.rectTransform);

            healthText.gameObject.SetActive(false);
        }

        void Start()
        {
            playerHitEvent += UpdateTail;

            if (playerNeedsUpdating)
            {
                playerNeedsUpdating = false;
                Spawn();
                UpdateModel();
            }
        }

        void OnDestroy()
        {
            if (Core)
                Core.RuntimePlayer = null;
            Core = null;
            Model = null;
        }

        #endregion

        #region Update Methods

        void Update()
        {
            time += Time.time - timeOffset;

            timeOffset = Time.time;

            if (!isColliderTrigger)
                SetTriggerCollision(false);

            if (UpdateMode == TailUpdateMode.Update)
                UpdateTailDistance();

            UpdateCustomTheme(); UpdateBoostTheme(); UpdateSpeeds(); UpdateTrailLengths(); UpdateTheme();
            if (canvas)
            {
                bool act = PlayerManager.Players.Count > 1 && Core && ShowNameTags;
                canvas.SetActive(act);

                if (act && nametagText)
                {
                    var index = PlayersData.Current.GetMaxIndex(playerIndex, 4);

                    nametagText.text = NametagText;
                    nametagBase.material.color = RTColors.FadeColor(ThemeManager.inst.Current.GetPlayerColor(index), 0.3f);
                    nametagBase.transform.localScale = new Vector3(initialHealthCount * 2.25f, 1.5f, 1f);
                }
            }

            if (!Model)
                return;

            if (boost.trailRenderer && Model.boostPart.Trail.emitting)
            {
                var tf = boost.gameObject.transform;
                Vector2 v = new Vector2(tf.localScale.x, tf.localScale.y);

                boost.trailRenderer.startWidth = Model.boostPart.Trail.startWidth * v.magnitude / 1.414213f;
                boost.trailRenderer.endWidth = Model.boostPart.Trail.endWidth * v.magnitude / 1.414213f;
            }

            if (!Alive && !isDead && Core && !RTBeatmap.Current.challengeMode.Invincible)
                StartCoroutine(IKill());
        }

        void FixedUpdate()
        {
            if (UpdateMode == TailUpdateMode.FixedUpdate)
                UpdateTailDistance();

            if (healthText)
                healthText.gameObject.SetActive(Model && Model.guiPart.active && GameManager.inst.timeline.activeSelf);
        }

        void LateUpdate()
        {
            UpdateControls(); UpdateRotation();

            if (UpdateMode == TailUpdateMode.LateUpdate)
                UpdateTailDistance();

            UpdateTailTransform(); UpdateTailDev(); UpdateTailSizes();

            // Here we handle the player's bounds to the camera. It is possible to include negative zoom in those bounds but it might not be a good idea since people have already utilized it.
            if (!OutOfBounds && !EventsConfig.Instance.EditorCameraEnabled && CoreHelper.Playing)
            {
                var cameraToViewportPoint = Camera.main.WorldToViewportPoint(rb.position);
                cameraToViewportPoint.x = Mathf.Clamp(cameraToViewportPoint.x, 0f, 1f);
                cameraToViewportPoint.y = Mathf.Clamp(cameraToViewportPoint.y, 0f, 1f);
                if (Camera.main.orthographicSize > 0f && (!includeNegativeZoom || Camera.main.orthographicSize < 0f) && Core)
                    rb.position = Camera.main.ViewportToWorldPoint(cameraToViewportPoint);
            }

            if (!Model || !Model.faceControlActive || FaceController == null)
                return;

            var vector = new Vector2(FaceController.Move.Vector.x, FaceController.Move.Vector.y);
            var fp = Model.facePosition;
            if (vector.magnitude > 1f)
                vector = vector.normalized;

            if ((rotateMode == RotateMode.FlipX || rotateMode == RotateMode.RotateFlipX) && lastMovement.x < 0f)
                vector.x = -vector.x;
            if ((rotateMode == RotateMode.FlipY || rotateMode == RotateMode.RotateFlipY) && lastMovement.y < 0f)
                vector.y = -vector.y;

            face.gameObject.transform.localPosition = new Vector3(vector.x * 0.3f + fp.x, vector.y * 0.3f + fp.y, 0f);
        }

        void UpdateSpeeds()
        {
            float pitch = MultiplyByPitch ? CoreHelper.ForwardPitch : 1f;

            if (CoreHelper.Paused)
                pitch = 0f;

            animationController.speed = pitch;

            if (GameData.Current.data.level.allowPlayerModelControls && !Model)
                return;

            var control = Core.GetControl();

            var idleSpeed = control.moveSpeed;
            var boostSpeed = control.boostSpeed;
            var boostCooldown = control.boostCooldown;
            var minBoostTime = control.minBoostTime;
            var maxBoostTime = control.maxBoostTime;
            var hitCooldown = control.hitCooldown;

            if (GameData.Current && GameData.Current.data && GameData.Current.data.level is LevelData levelData && levelData.limitPlayer)
            {
                idleSpeed = Mathf.Clamp(idleSpeed, levelData.limitMoveSpeed.x, levelData.limitMoveSpeed.y);
                boostSpeed = Mathf.Clamp(boostSpeed, levelData.limitBoostSpeed.x, levelData.limitBoostSpeed.y);
                boostCooldown = Mathf.Clamp(boostCooldown, levelData.limitBoostCooldown.x, levelData.limitBoostCooldown.y);
                minBoostTime = Mathf.Clamp(minBoostTime, levelData.limitBoostMinTime.x, levelData.limitBoostMinTime.y);
                maxBoostTime = Mathf.Clamp(maxBoostTime, levelData.limitBoostMaxTime.x, levelData.limitBoostMaxTime.y);
                hitCooldown = Mathf.Clamp(hitCooldown, levelData.limitHitCooldown.x, levelData.limitHitCooldown.y);
            }

            timeHitOffset = Time.time - timeHit;

            if (timeHitOffset > hitCooldown && isTakingHit)
            {
                isTakingHit = false;
                CanTakeDamage = true;
            }

            this.idleSpeed = idleSpeed;
            this.boostSpeed = boostSpeed;

            this.boostCooldown = boostCooldown / pitch;
            this.minBoostTime = minBoostTime / pitch;
            this.maxBoostTime = maxBoostTime / pitch;
        }

        void UpdateTailDistance()
        {
            path[0].pos = rb.transform.position;
            path[0].rot = rb.transform.rotation;
            if (tailMode == 1 || CoreHelper.Paused)
                return;

            for (int i = 1; i < path.Count; i++)
            {
                int prev = i - 1;

                while (prev > 0 && !path[prev].active) // when the last tail part is inactive, we don't want it to be a part of the path
                    prev--;

                if (Vector3.Distance(path[i].pos, path[prev].pos) <= tailDistance)
                    continue;

                path[i].pos = Vector3.Lerp(path[i].pos, path[prev].pos, Time.deltaTime * 12f);
                path[i].rot = Quaternion.Lerp(path[i].rot, path[prev].rot, Time.deltaTime * 12f);
            }
        }

        void UpdateTailTransform()
        {
            if (tailMode == 1 || CoreHelper.Paused)
                return;

            var tailBaseTime = Model.tailBase.time;
            float num = Time.deltaTime * (tailBaseTime == 0f ? 200f : tailBaseTime);
            for (int i = 1; i < path.Count; i++)
            {
                if (!path[i].transform || !path[i].active)
                    continue;

                num *= Vector3.Distance(path[i].lastPos, path[i].pos);
                path[i].transform.position = Vector3.MoveTowards(path[i].lastPos, path[i].pos, num);
                path[i].lastPos = path[i].transform.position;
                path[i].transform.rotation = path[i].rot;
            }
        }

        void UpdateTailDev()
        {
            if (tailMode != 1 || CoreHelper.Paused)
                return;

            int num = 1;
            for (int i = 1; i < path.Count; i++)
            {
                if (i == 1)
                {
                    if (showBoostTail && path[1].active)
                        num++;

                    var delayTracker = boostTail.delayTracker;
                    delayTracker.offset = -i * tailDistance / 2f;
                    delayTracker.positionOffset = 0.1f * (-i + 5);
                    delayTracker.rotationOffset = 0.1f * (-i + 5);
                }
                else if (tailParts.TryGetAt(i - 2, out RTPlayerObject tailPart))
                {
                    var delayTracker = tailPart.delayTracker;
                    delayTracker.offset = -num * tailDistance / 2f;
                    delayTracker.positionOffset = 0.1f * (-num + 5);
                    delayTracker.rotationOffset = 0.1f * (-num + 5);

                    if (path[i].active)
                        num++;
                }
            }
        }

        void UpdateTailSizes()
        {
            if (!Model)
                return;

            for (int i = 0; i < Model.tailParts.Count; i++)
            {
                if (!tailParts.InRange(i))
                    continue;

                var t2 = Model.tailParts[i].scale;

                tailParts[i].gameObject.transform.localScale = new Vector3(t2.x, t2.y, 1f);
            }
        }

        void UpdateTrailLengths()
        {
            if (!Model)
                return;

            var pitch = MultiplyByPitch ? CoreHelper.ForwardPitch : 1f;

            var headTrail = head.trailRenderer;
            var boostTrail = boost.trailRenderer;

            headTrail.time = Model.headPart.Trail.time / pitch;
            boostTrail.time = Model.boostPart.Trail.time / pitch;

            for (int i = 0; i < tailParts.Count; i++)
                tailParts[i].trailRenderer.time = Model.GetTail(i).Trail.time / pitch;
        }

        void UpdateControls()
        {
            if (!Core || !Model || !Alive)
            {
                rb.velocity = Vector2.zero;
                return;
            }

            if (CanMove && Actions != null)
            {
                var boostWasPressed = Actions.Boost.WasPressed;

                if (boostWasPressed && !CanBoost && !LockBoost && PlayerConfig.Instance.QueueBoost.Value)
                    queuedBoost = true;
                if (Actions.Boost.WasReleased)
                    queuedBoost = false;

                if ((boostWasPressed || queuedBoost) && (JumpMode || CanBoost) && !LockBoost && (!JumpMode || (jumpCount == 0 || !colliding) && (currentJumpCount == Mathf.Clamp(jumpCount, -1, MaxJumpCount) || jumpBoostCount == -1 || currentJumpBoostCount < Mathf.Clamp(jumpBoostCount, -1, MaxJumpBoostCount))))
                {
                    queuedBoost = false;

                    if (JumpMode)
                    {
                        if (PlayBoostSound && CanBoost)
                            SoundManager.inst.PlaySound(DefaultSounds.boost_recover);

                        currentJumpCount++;
                        currentJumpBoostCount++;
                    }

                    Boost();
                    return;
                }
            }

            if (CanCancelBoosting && (Actions != null && Actions.Boost.WasReleased || startBoostTime + maxBoostTime <= Time.time))
                StopBoosting();

            if (Alive && FaceController != null && Model.bulletPart.active && (Model.bulletPart.constant ? FaceController.Shoot.IsPressed : FaceController.Shoot.WasPressed) && canShoot)
                Shoot();

            var player = rb.gameObject;

            if (JumpMode)
            {
                rb.gravityScale = jumpGravity * JumpGravity;

                if (Actions == null)
                    return;

                var pitch = MultiplyByPitch ? CoreHelper.ForwardPitch : 1f;
                float x = Actions.Move.Vector.x;
                float y = Actions.Move.Vector.y;

                if (isBoosting)
                {
                    var vector = new Vector2(x, y * jumpBoostMultiplier);

                    rb.velocity = PlayerForce + vector * boostSpeed * pitch * SpeedMultiplier;
                    return;
                }

                if (Actions.Boost.WasPressed)
                    Jump();

                if (x != 0f)
                    lastMoveHorizontal = x;

                var velocity = rb.velocity;
                velocity.x = x * idleSpeed * pitch * SprintSneakSpeed * SpeedMultiplier;
                rb.velocity = velocity;

                return;
            }

            rb.gravityScale = 0f;

            if (Alive && Actions != null && Core.active && CanMove && !CoreHelper.Paused &&
                (CoreConfig.Instance.AllowControlsInputField.Value || !CoreHelper.IsUsingInputField) &&
                movementMode == MovementMode.KeyboardController && (!CoreHelper.InEditor || !EventsConfig.Instance.EditorCamEnabled.Value))
            {
                colliding = false;
                var x = Actions.Move.Vector.x;
                var y = Actions.Move.Vector.y;
                var pitch = MultiplyByPitch ? CoreHelper.ForwardPitch : 1f;

                if (x != 0f)
                {
                    lastMoveHorizontal = x;
                    if (y == 0f)
                        lastMoveVertical = 0f;
                }
                if (y != 0f)
                {
                    lastMoveVertical = y;
                    if (x == 0f)
                        lastMoveHorizontal = 0f;
                }

                Vector2 vector;
                if (isBoosting)
                {
                    vector = new Vector2(lastMoveHorizontal, lastMoveVertical);
                    vector = vector.normalized;

                    rb.velocity = PlayerForce + vector * boostSpeed * pitch * SpeedMultiplier;
                    if (stretch && rb.velocity.magnitude > 0f)
                    {
                        float e = 1f + rb.velocity.magnitude * stretchAmount / 20f;
                        player.transform.localScale = new Vector3(1f * e + stretchVector.x, 1f / e + stretchVector.y, 1f);
                    }
                }
                else
                {
                    vector = new Vector2(x, y);
                    if (vector.magnitude > 1f)
                        vector = vector.normalized;

                    var velocity = PlayerForce + vector * idleSpeed * pitch * SprintSneakSpeed * SpeedMultiplier;

                    if (velocity != Vector2.zero || resetVelocity)
                        rb.velocity = velocity;

                    if (stretch && rb.velocity.magnitude > 0f)
                    {
                        if (rotateMode != RotateMode.None && rotateMode != RotateMode.FlipX && rotateMode != RotateMode.RotateFlipX)
                        {
                            float e = 1f + rb.velocity.magnitude * stretchAmount / 20f;
                            player.transform.localScale = new Vector3(1f * e + stretchVector.x, 1f / e + stretchVector.y, 1f);
                        }

                        // I really need to figure out how to get stretching to work with non-RotateMode.RotateToDirection. One solution is to setup an additional parent that can be used to stretch, but not sure about doing that atm.
                        if (rotateMode == RotateMode.None || rotateMode == RotateMode.FlipX || rotateMode == RotateMode.FlipY)
                        {
                            float e = 1f + rb.velocity.magnitude * stretchAmount / 20f;

                            float xm = lastMoveHorizontal;
                            if (xm > 0f)
                                xm = -xm;

                            float ym = lastMoveVertical;
                            if (ym > 0f)
                                ym = -ym;

                            float xt = 1f * e + ym + stretchVector.x;
                            float yt = 1f * e + xm + stretchVector.y;

                            switch (rotateMode)
                            {
                                case RotateMode.RotateFlipX:
                                case RotateMode.FlipX: {
                                        if (lastMovement.x > 0f)
                                            player.transform.localScale = new Vector3(xt, yt, 1f);
                                        if (lastMovement.x < 0f)
                                            player.transform.localScale = new Vector3(-xt, yt, 1f);
                                        break;
                                    }
                                case RotateMode.RotateFlipY:
                                case RotateMode.FlipY: {
                                        if (lastMovement.y > 0f)
                                            player.transform.localScale = new Vector3(xt, yt, 1f);
                                        if (lastMovement.y < 0f)
                                            player.transform.localScale = new Vector3(xt, -yt, 1f);
                                        break;
                                    }
                                default: {
                                        player.transform.localScale = new Vector3(xt, yt, 1f);
                                        break;
                                    }
                            }

                        }
                    }
                    else if (stretch)
                    {
                        float xt = 1f + stretchVector.x;
                        float yt = 1f + stretchVector.y;

                        switch (rotateMode)
                        {
                            case RotateMode.RotateFlipX:
                            case RotateMode.FlipX: {
                                    if (lastMovement.x > 0f)
                                        player.transform.localScale = new Vector3(xt, yt, 1f);
                                    if (lastMovement.x < 0f)
                                        player.transform.localScale = new Vector3(-xt, yt, 1f);
                                    break;
                                }
                            case RotateMode.RotateFlipY:
                            case RotateMode.FlipY: {
                                    if (lastMovement.y > 0f)
                                        player.transform.localScale = new Vector3(xt, yt, 1f);
                                    if (lastMovement.y < 0f)
                                        player.transform.localScale = new Vector3(xt, -yt, 1f);
                                    break;
                                }
                            default: {
                                    player.transform.localScale = new Vector3(xt, yt, 1f);
                                    break;
                                }
                        }
                    }
                }

                if (rb.velocity != Vector2.zero)
                    lastVelocity = rb.velocity;
            }
            else if (CanMove && resetVelocity)
                rb.velocity = Vector3.zero;
        }

        void UpdateRotation()
        {
            var player = rb.gameObject;

            if (CanRotate)
            {
                var b = Quaternion.AngleAxis(Mathf.Atan2(lastMovement.y, lastMovement.x) * 57.29578f, player.transform.forward);
                var c = Quaternion.Slerp(player.transform.rotation, b, 720f * Time.deltaTime);
                switch (rotateMode)
                {
                    case RotateMode.RotateToDirection: {
                            player.transform.rotation = c;

                            face.parent.localRotation = Quaternion.identity;
                            
                            break;
                        }
                    case RotateMode.None: {
                            player.transform.rotation = Quaternion.identity;

                            face.parent.rotation = c;

                            break;
                        }
                    case RotateMode.FlipX: {
                            b = Quaternion.AngleAxis(Mathf.Atan2(lastMovementTotal.y, lastMovementTotal.x) * 57.29578f, player.transform.forward);
                            c = Quaternion.Slerp(player.transform.rotation, b, 720f * Time.deltaTime);

                            var vectorRotation = c.eulerAngles;
                            if (vectorRotation.z > 90f && vectorRotation.z < 270f)
                                vectorRotation.z = -vectorRotation.z + 180f;

                            face.parent.rotation = Quaternion.Euler(vectorRotation);

                            player.transform.rotation = Quaternion.identity;

                            if (lastMovement.x > 0.01f)
                            {
                                if (!stretch)
                                    player.transform.localScale = Vector3.one;
                                if (!animatingBoost)
                                    boostTail.parent.localScale = Vector3.one;
                                for (int i = 0; i < tailParts.Count; i++)
                                    if (tailParts[i].parent)
                                        tailParts[i].parent.localScale = Vector3.one;
                            }
                            if (lastMovement.x < -0.01f)
                            {
                                var stretchScale = new Vector3(-1f, 1f, 1f);
                                if (!stretch)
                                    player.transform.localScale = stretchScale;
                                if (!animatingBoost)
                                    boostTail.parent.localScale = stretchScale;
                                for (int i = 0; i < tailParts.Count; i++)
                                    if (tailParts[i].parent)
                                        tailParts[i].parent.localScale = stretchScale;
                            }

                            break;
                        }
                    case RotateMode.FlipY: {
                            b = Quaternion.AngleAxis(Mathf.Atan2(lastMovementTotal.y, lastMovementTotal.x) * 57.29578f, player.transform.forward);
                            c = Quaternion.Slerp(player.transform.rotation, b, 720f * Time.deltaTime);

                            var vectorRotation = c.eulerAngles;
                            if (vectorRotation.z > 0f && vectorRotation.z < 180f)
                                vectorRotation.z = -vectorRotation.z + 90f;

                            face.parent.rotation = Quaternion.Euler(vectorRotation);

                            player.transform.rotation = Quaternion.identity;

                            if (lastMovement.y > 0.01f)
                            {
                                if (!stretch)
                                    player.transform.localScale = Vector3.one;
                                if (!animatingBoost)
                                    boostTail.parent.localScale = Vector3.one;
                                for (int i = 0; i < tailParts.Count; i++)
                                    if (tailParts[i].parent)
                                        tailParts[i].parent.localScale = Vector3.one;
                            }
                            if (lastMovement.y < -0.01f)
                            {
                                var stretchScale = new Vector3(1f, -1f, 1f);
                                if (!stretch)
                                    player.transform.localScale = stretchScale;
                                if (!animatingBoost)
                                    boostTail.parent.localScale = stretchScale;
                                for (int i = 0; i < tailParts.Count; i++)
                                    if (tailParts[i].parent)
                                        tailParts[i].parent.localScale = stretchScale;
                            }

                            break;
                        }
                    case RotateMode.RotateReset: {
                            if (!moved)
                            {
                                animatingRotateReset = false;

                                if (rotateResetAnimation != null)
                                {
                                    animationController.Remove(rotateResetAnimation.id);
                                    rotateResetAnimation = null;
                                }

                                player.transform.rotation = c;

                                face.parent.localRotation = Quaternion.identity;
                            }

                            var time = Time.time - timeNotMovingOffset;

                            if (moved && !animatingRotateReset && time > 0.03f)
                            {
                                animatingRotateReset = true;

                                var z = player.transform.rotation.eulerAngles.z;

                                if (rotateResetAnimation != null)
                                {
                                    animationController.Remove(rotateResetAnimation.id);
                                    rotateResetAnimation = null;
                                }

                                rotateResetAnimation = new RTAnimation("Player Rotate Reset");
                                rotateResetAnimation.animationHandlers = new List<AnimationHandlerBase>
                                {
                                    new AnimationHandler<float>(new List<IKeyframe<float>>
                                    {
                                        new FloatKeyframe(0f, z, Ease.Linear),
                                        new FloatKeyframe(0.2f, 0f, Ease.CircOut),
                                    }, x =>
                                    {
                                        if (!player || !face.parent)
                                            return;

                                        player.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, x));

                                        face.parent.localRotation = Quaternion.identity;
                                    }),
                                };

                                rotateResetAnimation.onComplete = () =>
                                {
                                    if (rotateResetAnimation == null)
                                        return;

                                    animationController.Remove(rotateResetAnimation.id);
                                    rotateResetAnimation = null;
                                    animatingRotateReset = false;
                                };

                                animationController.Play(rotateResetAnimation);
                            }

                            break;
                        }
                    case RotateMode.RotateFlipX: {
                            var vectorRotation = c.eulerAngles;
                            if (vectorRotation.z > 90f && vectorRotation.z < 270f)
                                vectorRotation.z += 180f;

                            player.transform.rotation = Quaternion.Euler(vectorRotation);

                            face.parent.localRotation = Quaternion.Euler(vectorRotation);

                            if (lastMovement.x > 0.01f)
                            {
                                if (!stretch)
                                    player.transform.localScale = Vector3.one;
                                if (!animatingBoost)
                                    boostTail.parent.localScale = Vector3.one;
                                for (int i = 0; i < tailParts.Count; i++)
                                    if (tailParts[i].parent)
                                        tailParts[i].parent.localScale = Vector3.one;
                            }
                            if (lastMovement.x < -0.01f)
                            {
                                var stretchScale = new Vector3(-1f, 1f, 1f);
                                if (!stretch)
                                    player.transform.localScale = stretchScale;
                                if (!animatingBoost)
                                    boostTail.parent.localScale = stretchScale;
                                for (int i = 0; i < tailParts.Count; i++)
                                    if (tailParts[i].parent)
                                        tailParts[i].parent.localScale = stretchScale;
                            }

                            break;
                        }
                    case RotateMode.RotateFlipY: {
                            var vectorRotation = c.eulerAngles;
                            if (vectorRotation.z > 0f && vectorRotation.z < 180f)
                                vectorRotation.z = -vectorRotation.z + 90f;

                            player.transform.rotation = Quaternion.Euler(vectorRotation);

                            face.parent.rotation = Quaternion.identity;

                            if (lastMovement.y > 0.01f)
                            {
                                if (!stretch)
                                    player.transform.localScale = Vector3.one;
                                if (!animatingBoost)
                                    boostTail.parent.localScale = Vector3.one;
                                for (int i = 0; i < tailParts.Count; i++)
                                    if (tailParts[i].parent)
                                        tailParts[i].parent.localScale = Vector3.one;
                            }
                            if (lastMovement.y < -0.01f)
                            {
                                var stretchScale = new Vector3(1f, -1f, 1f);
                                if (!stretch)
                                    player.transform.localScale = stretchScale;
                                if (!animatingBoost)
                                    boostTail.parent.localScale = stretchScale;
                                for (int i = 0; i < tailParts.Count; i++)
                                    if (tailParts[i].parent)
                                        tailParts[i].parent.localScale = stretchScale;
                            }

                            break;
                        }
                }
            }

            var posCalc = (player.transform.position - lastPos);

            if (!JumpMode && (rotateMode == RotateMode.RotateToDirection || rotateMode == RotateMode.RotateReset || rotateMode == RotateMode.RotateFlipX || rotateMode == RotateMode.RotateFlipY))
            {
                if (posCalc.x < -0.001f || posCalc.x > 0.001f || posCalc.y < -0.001f || posCalc.y > 0.001f)
                    lastMovement = posCalc;
            }
            else // Decreases the range of movement detection for cases where 0.001 range is too sensitive.
            {
                if (posCalc.x < -0.01f || posCalc.x > 0.01f)
                    lastMovement.x = posCalc.x;
                if (posCalc.y < -0.01f || posCalc.y > 0.01f)
                    lastMovement.y = posCalc.y;
            }

            if (posCalc.x < -0.01f || posCalc.x > 0.01f || posCalc.y < -0.01f || posCalc.y > 0.01f)
            {
                lastMovementTotal = posCalc;
                timeNotMovingOffset = Time.time;
            }

            moved = posCalc == Vector3.zero;

            lastPos = player.transform.position;

            var dfs = player.transform.localPosition;
            dfs.z = 0f;
            player.transform.localPosition = dfs;
        }

        void UpdateTheme()
        {
            if (!Model)
                return;

            var index = PlayersData.Current.GetMaxIndex(playerIndex);

            if (head.gameObject)
            {
                if (head.renderer)
                    head.renderer.material.color = RTColors.GetPlayerColor(index, Model.headPart.color, Model.headPart.opacity, Model.headPart.customColor);

                try
                {
                    int colStart = Model.headPart.color;
                    var colStartHex = Model.headPart.customColor;
                    float alphaStart = Model.headPart.opacity;

                    var main1 = burst.main;
                    var main2 = death.main;
                    var main3 = spawn.main;
                    main1.startColor = new ParticleSystem.MinMaxGradient(RTColors.GetPlayerColor(index, colStart, alphaStart, colStartHex));
                    main2.startColor = new ParticleSystem.MinMaxGradient(RTColors.GetPlayerColor(index, colStart, alphaStart, colStartHex));
                    main3.startColor = new ParticleSystem.MinMaxGradient(RTColors.GetPlayerColor(index, colStart, alphaStart, colStartHex));
                }
                catch
                {

                }
            }

            if (boost.renderer)
                boost.renderer.material.color = RTColors.GetPlayerColor(index, Model.boostPart.color, Model.boostPart.opacity, Model.boostPart.customColor);

            if (boostTail.renderer)
                boostTail.renderer.material.color = RTColors.GetPlayerColor(index, Model.boostTailPart.color, Model.boostTailPart.opacity, Model.boostTailPart.customColor);

            //GUI Bar
            {
                int baseCol = Model.guiPart.baseColor;
                int topCol = Model.guiPart.topColor;
                string baseColHex = Model.guiPart.baseCustomColor;
                string topColHex = Model.guiPart.topCustomColor;
                float baseAlpha = Model.guiPart.baseOpacity;
                float topAlpha = Model.guiPart.topOpacity;

                for (int i = 0; i < healthObjects.Count; i++)
                    if (healthObjects[i].image)
                        healthObjects[i].image.color = RTColors.GetPlayerColor(index, topCol, topAlpha, topColHex);

                barBaseIm.color = RTColors.GetPlayerColor(index, baseCol, baseAlpha, baseColHex);
                barIm.color = RTColors.GetPlayerColor(index, topCol, topAlpha, topColHex);
            }

            for (int i = 0; i < tailParts.Count; i++)
            {
                var modelPart = Model.GetTail(i);

                var tailPart = tailParts[i];

                var main = tailPart.particleSystem.main;

                main.startColor = RTColors.GetPlayerColor(index, modelPart.Particles.color, 1f, modelPart.Particles.customColor);

                tailPart.renderer.material.color = RTColors.GetPlayerColor(index, modelPart.color, modelPart.opacity, modelPart.customColor);

                tailPart.trailRenderer.startColor = RTColors.GetPlayerColor(index, modelPart.Trail.startColor, modelPart.Trail.startOpacity, modelPart.Trail.startCustomColor);
                tailPart.trailRenderer.endColor = RTColors.GetPlayerColor(index, modelPart.Trail.endColor, modelPart.Trail.endOpacity, modelPart.Trail.endCustomColor);
            }

            if (Model.headPart.Trail.emitting && head.trailRenderer)
            {
                head.trailRenderer.startColor = RTColors.GetPlayerColor(index, Model.headPart.Trail.startColor, Model.headPart.Trail.startOpacity, Model.headPart.Trail.startCustomColor);
                head.trailRenderer.endColor = RTColors.GetPlayerColor(index, Model.headPart.Trail.endColor, Model.headPart.Trail.endOpacity, Model.headPart.Trail.endCustomColor);
            }

            if (Model.headPart.Particles.emitting && head.particleSystem)
            {
                var colStart = Model.headPart.Particles.color;
                var colStartHex = Model.headPart.Particles.customColor;

                var main = head.particleSystem.main;
                main.startColor = RTColors.GetPlayerColor(index, colStart, 1f, colStartHex);
            }

            if (Model.boostPart.Trail.emitting && boost.trailRenderer)
            {
                boost.trailRenderer.startColor = RTColors.GetPlayerColor(index, Model.boostPart.Trail.startColor, Model.boostPart.Trail.startOpacity, Model.boostPart.Trail.startCustomColor);
                boost.trailRenderer.endColor = RTColors.GetPlayerColor(index, Model.boostPart.Trail.endColor, Model.boostPart.Trail.endOpacity, Model.boostPart.Trail.endCustomColor);
            }

            if (Model.boostPart.Particles.emitting && boost.particleSystem)
            {
                var main = boost.particleSystem.main;
                main.startColor = RTColors.GetPlayerColor(index, Model.boostPart.Particles.color, 1f, Model.boostPart.Particles.customColor);
            }
        }

        void UpdateCustomTheme()
        {
            if (customObjects.IsEmpty())
                return;

            var index = PlayersData.Current.GetMaxIndex(playerIndex);
            playerObjects.ForLoop(playerObject =>
            {
                if (!playerObject.isCustom)
                {
                    playerObject.gameObject.SetActive(playerObject.active);
                    return;
                }

                var customObject = playerObject as RTCustomPlayerObject;

                if (!Core || !customObject.gameObject)
                    return;

                var active = customObject.active &&
                    (customObject.reference.visibilitySettings.IsEmpty() ? customObject.reference.active :
                        customObject.reference.requireAll ?
                            customObject.reference.visibilitySettings.All(x => CheckVisibility(x)) :
                            customObject.reference.visibilitySettings.Any(x => CheckVisibility(x)));

                customObject.gameObject.SetActive(active);

                if (!active)
                    return;

                var reference = customObject.reference;
                if (customObject.text)
                    customObject.text.color = RTColors.GetPlayerColor(index, reference.color, reference.opacity, reference.customColor);
                else if (customObject.renderer)
                    customObject.renderer.material.color = RTColors.GetPlayerColor(index, reference.color, reference.opacity, reference.customColor);

                if (!customObject.idle || reference.animations.IsEmpty())
                {
                    var origPos = reference.position;
                    var origSca = reference.scale;
                    var origRot = reference.rotation;

                    customObject.gameObject.transform.localPosition = new Vector3(origPos.x + customObject.positionOffset.x, origPos.y + customObject.positionOffset.y, reference.depth + customObject.positionOffset.z);
                    customObject.gameObject.transform.localScale = new Vector3(origSca.x + customObject.scaleOffset.x, origSca.y + customObject.scaleOffset.y, 1f + customObject.scaleOffset.z);
                    customObject.gameObject.transform.localEulerAngles = new Vector3(customObject.rotationOffset.x, customObject.rotationOffset.y, origRot + customObject.rotationOffset.z);
                    return;
                }

                reference.animations.ForLoop(animation =>
                {
                    if (string.IsNullOrEmpty(animation.ReferenceID) || animation.ReferenceID.ToLower().Remove(" ") != customObject.currentIdleAnimation)
                        return;

                    var length = animation.GetLength();
                    var origPos = reference.position;
                    var origSca = reference.scale;
                    var origRot = reference.rotation;

                    if (animation.animatePosition)
                    {
                        var position = GameData.InterpolateVector3Keyframes(animation.positionKeyframes, time % length);
                        customObject.gameObject.transform.localPosition = (new Vector3(origPos.x, origPos.y, reference.depth) + position + customObject.positionOffset);
                    }
                    else
                        customObject.gameObject.transform.localPosition = new Vector3(origPos.x + customObject.positionOffset.x, origPos.y + customObject.positionOffset.y, reference.depth + customObject.positionOffset.z);

                    if (animation.animateScale)
                    {
                        var scale = GameData.InterpolateVector2Keyframes(animation.scaleKeyframes, time % length);
                        customObject.gameObject.transform.localScale = (new Vector3(origSca.x * scale.x + customObject.scaleOffset.x, origSca.y * scale.y + customObject.scaleOffset.y, 1f + customObject.scaleOffset.z));
                    }
                    else
                        customObject.gameObject.transform.localScale = new Vector3(origSca.x + customObject.scaleOffset.x, origSca.y + customObject.scaleOffset.y, 1f + customObject.scaleOffset.z);

                    if (animation.animateRotation)
                    {
                        var rotation = GameData.InterpolateFloatKeyframes(animation.rotationKeyframes, time % length, 0);
                        customObject.gameObject.transform.localEulerAngles = new Vector3(customObject.rotationOffset.x, customObject.rotationOffset.y, origRot + rotation + customObject.rotationOffset.z);
                    }
                    else
                        customObject.gameObject.transform.localEulerAngles = new Vector3(customObject.rotationOffset.x, customObject.rotationOffset.y, origRot + customObject.rotationOffset.z);
                });
            });
        }

        void UpdateBoostTheme()
        {
            if (emitted.IsEmpty())
                return;

            var index = PlayersData.Current.GetMaxIndex(playerIndex);

            emitted.ForLoop(boost =>
            {
                if (!boost)
                    return;

                int startCol = boost.startColor;
                int endCol = boost.endColor;

                var startHex = boost.startCustomColor;
                var endHex = boost.endCustomColor;

                float alpha = boost.opacity;
                float colorTween = boost.colorTween;

                Color startColor = RTColors.GetPlayerColor(index, startCol, alpha, startHex);
                Color endColor = RTColors.GetPlayerColor(index, endCol, alpha, endHex);

                if (boost.renderer)
                    boost.renderer.material.color = Color.Lerp(startColor, endColor, colorTween);
            });
        }

        #endregion

        #region Actions

        /// <summary>
        /// Spawns the player.
        /// </summary>
        public void Spawn()
        {
            CanTakeDamage = false;
            CanBoost = false;
            CanMove = false;
            isDead = false;
            isBoosting = false;
            isSpawning = true;

            InitSpawnAnimation();
            PlaySpawnParticles();

            try
            {
                path[0].pos = rb.transform.position;
                path[0].rot = rb.transform.rotation;
                var pos = path[0].pos;
                var rot = path[0].rot;
                for (int i = 1; i < path.Count; i++)
                {
                    var path = this.path[i];
                    path.pos = new Vector3(pos.x, pos.y);
                    path.rot = rot;
                    path.lastPos = new Vector3(pos.x, pos.y);
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            CoreHelper.Log($"Spawned Player {playerIndex}");
        }

        /// <summary>
        /// Heals the player.
        /// </summary>
        /// <param name="health">Amount of health to add.</param>
        /// <returns>Returns true if the player was successfully healed.</returns>
        public bool Heal(int health, bool playSound = true)
        {
            if (!Core || !Alive || health <= 0)
                return false;

            int prevHealth = Core.Health;
            Core.Health += health;

            if (prevHealth != Core.Health)
            {
                InitHealAnimation();
                if (playSound)
                    SoundManager.inst.PlaySound(DefaultSounds.HealPlayer);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Hits the player.
        /// </summary>
        /// <param name="damage">Amount of health to take away.</param>
        public void Hit(int damage)
        {
            if (!CanTakeDamage || !Alive || damage <= 0)
                return;

            timeHit = Time.time;

            InitBeforeHit();
            if (Core && Core.Health > 0)
            {
                PlayHitParticles();
                InitHitAnimation();
            }
            if (!Core)
                return;

            if (!RTBeatmap.Current.challengeMode.Invincible)
                Core.Health -= damage;
            playerHitEvent?.Invoke(Core.Health, rb.position);
        }

        /// <summary>
        /// Hits the player.
        /// </summary>
        public void Hit()
        {
            if (!CanTakeDamage || !Alive)
                return;

            timeHit = Time.time;

            InitBeforeHit();
            if (Core && Core.Health > 0)
            {
                PlayHitParticles();
                InitHitAnimation();
            }
            if (!Core)
                return;

            if (!RTBeatmap.Current.challengeMode.Invincible)
                Core.Health--;
            playerHitEvent?.Invoke(Core.Health, rb.position);
        }

        /// <summary>
        /// Kills the player.
        /// </summary>
        public void Kill()
        {
            if (Core)
                Core.Health = 0;
        }

        /// <summary>
        /// Makes the player boost.
        /// </summary>
        public void Boost()
        {
            if (!CanBoost || isBoosting)
                return;

            startBoostTime = Time.time;
            InitBeforeBoost();
            InitBoostAnimation();

            var ps = boost.particleSystem;
            var emission = ps.emission;

            if (emission.enabled)
                ps.Play();
            if (Model && Model.boostPart.Trail.emitting)
                boost.trailRenderer.emitting = true;

            if (PlayBoostSound)
                SoundManager.inst.PlaySound(DefaultSounds.boost);

            Pulse();

            stretchVector = new Vector2(stretchAmount * 1.5f, -(stretchAmount * 1.5f));

            SetTriggerCollision(true);

            if (showBoostTail)
            {
                path[1].active = false;
                animatingBoost = true;
                boostTail.parent.DOScale(Vector3.zero, 0.05f / CoreHelper.ForwardPitch).SetEase(DataManager.inst.AnimationList[2].Animation);
            }
        }

        /// <summary>
        /// Stops the player from boosting.
        /// </summary>
        public void StopBoosting()
        {
            float num = Time.time - startBoostTime;
            StartCoroutine(BoostCancel((num < minBoostTime) ? (minBoostTime - num) : 0f));
        }

        /// <summary>
        /// Forces the player to jump if the gamemode is set to <see cref="GameMode.Platformer"/>.
        /// </summary>
        public void Jump()
        {
            if (!JumpMode)
                return;

            var velocity = rb.velocity;
            if ((jumpCount != 0 && colliding || jumpCount == -1 || currentJumpCount < Mathf.Clamp(jumpCount, -1, MaxJumpCount)))
            {
                velocity.y = jumpIntensity * JumpIntensity;

                if (PlayBoostSound)
                    SoundManager.inst.PlaySound(DefaultSounds.boost);

                if (colliding)
                {
                    currentJumpCount = 0;
                    currentJumpBoostCount = 0;
                    colliding = false;
                }
                currentJumpCount++;
            }

            rb.velocity = velocity;

            if (jumpAnimationCustom)
                animationController.Play(jumpAnimationCustom);
        }

        /// <summary>
        /// Sets all path points to a single position.
        /// </summary>
        /// <param name="pos">Position to set.</param>
        public void SetPath(Vector2 pos)
        {
            foreach (var path in path)
            {
                path.pos = new Vector3(pos.x, pos.y);
                path.lastPos = new Vector3(pos.x, pos.y);

                if (path.transform)
                    path.transform.position = path.pos;
            }
        }

        /// <summary>
        /// Sets a path point to a position.
        /// </summary>
        /// <param name="index">Index of the path.</param>
        /// <param name="pos">Position to set.</param>
        public void SetPath(int index, Vector2 pos)
        {
            if (!path.InRange(index))
                return;
            path[index].pos = new Vector3(pos.x, pos.y);
            path[index].lastPos = new Vector3(pos.x, pos.y);
            if (path[index].transform)
                path[index].transform.position = path[index].pos;
        }

        /// <summary>
        /// Destroys the player objects.
        /// </summary>
        public void ClearObjects()
        {
            if (healthText)
                DestroyImmediate(healthText.gameObject);
            DestroyImmediate(gameObject);
        }

        /// <summary>
        /// Emits a pulse effect from the player.
        /// </summary>
        public void Pulse()
        {
            if (!Model)
                return;

            var currentModel = Model;

            if (!currentModel.pulsePart.active)
                return;

            var player = rb.gameObject;

            int s = Mathf.Clamp(currentModel.pulsePart.shape, 0, ObjectManager.inst.objectPrefabs.Count - 1);
            int so = Mathf.Clamp(currentModel.pulsePart.shapeOption, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

            if (s == 4 || s == 6)
            {
                s = 0;
                so = 0;
            }

            var pulse = ObjectManager.inst.objectPrefabs[s].options[so].Duplicate(ObjectManager.inst.objectParent.transform);
            pulse.transform.localScale = new Vector3(currentModel.pulsePart.startScale.x, currentModel.pulsePart.startScale.y, 1f);
            pulse.transform.position = player.transform.position;
            pulse.transform.GetChild(0).localPosition = new Vector3(currentModel.pulsePart.startPosition.x, currentModel.pulsePart.startPosition.y, currentModel.pulsePart.depth);
            pulse.transform.GetChild(0).localRotation = Quaternion.Euler(new Vector3(0f, 0f, currentModel.pulsePart.startRotation));

            if (currentModel.pulsePart.rotateToHead)
                pulse.transform.localRotation = player.transform.localRotation;

            //Destroy
            {
                Destroy(pulse.transform.GetChild(0).GetComponent<SelectObjectInEditor>());
                Destroy(pulse.transform.GetChild(0).GetComponent<BoxCollider2D>());
                Destroy(pulse.transform.GetChild(0).GetComponent<PolygonCollider2D>());
                Destroy(pulse.transform.GetChild(0).gameObject.GetComponent<SelectObject>());
            }

            MeshRenderer pulseRenderer = pulse.transform.GetChild(0).GetComponent<MeshRenderer>();
            var pulseObject = new EmittedObject
            {
                renderer = pulseRenderer,
                startColor = currentModel.pulsePart.startColor,
                endColor = currentModel.pulsePart.endColor,
                startCustomColor = currentModel.pulsePart.startCustomColor,
                endCustomColor = currentModel.pulsePart.endCustomColor,
            };

            emitted.Add(pulseObject);

            pulseRenderer.enabled = true;
            pulseRenderer.material = head.renderer.material;
            pulseRenderer.material.shader = head.renderer.material.shader;
            Color colorBase = head.renderer.material.color;

            int easingPos = currentModel.pulsePart.easingPosition;
            int easingSca = currentModel.pulsePart.easingScale;
            int easingRot = currentModel.pulsePart.easingRotation;
            int easingOpa = currentModel.pulsePart.easingOpacity;
            int easingCol = currentModel.pulsePart.easingColor;

            float duration = Mathf.Clamp(currentModel.pulsePart.duration, 0.001f, 20f) / CoreHelper.ForwardPitch;

            pulse.transform.GetChild(0).DOLocalMove(new Vector3(currentModel.pulsePart.endPosition.x, currentModel.pulsePart.endPosition.y, currentModel.pulsePart.depth), duration).SetEase(DataManager.inst.AnimationList[easingPos].Animation);
            var tweenScale = pulse.transform.DOScale(new Vector3(currentModel.pulsePart.endScale.x, currentModel.pulsePart.endScale.y, 1f), duration).SetEase(DataManager.inst.AnimationList[easingSca].Animation);
            pulse.transform.GetChild(0).DOLocalRotate(new Vector3(0f, 0f, currentModel.pulsePart.endRotation), duration).SetEase(DataManager.inst.AnimationList[easingRot].Animation);

            DOTween.To(x =>
            {
                pulseObject.opacity = x;
            }, currentModel.pulsePart.startOpacity, currentModel.pulsePart.endOpacity, duration).SetEase(DataManager.inst.AnimationList[easingOpa].Animation);
            DOTween.To(x =>
            {
                pulseObject.colorTween = x;
            }, 0f, 1f, duration).SetEase(DataManager.inst.AnimationList[easingCol].Animation);

            tweenScale.OnComplete(() =>
            {
                Destroy(pulse);
                emitted.Remove(pulseObject);
            });
        }

        // todo: aiming so you don't need to be facing the direction of the bullet (maybe create a parent that rotates in the direction of the right stick?)
        /// <summary>
        /// Shoots a bullet from the player that can damage objects or other players.
        /// </summary>
        public void Shoot()
        {
            var currentModel = Model;

            if (currentModel == null || !currentModel.bulletPart.active)
                return;

            if (shootAnimationCustom)
                animationController.Play(shootAnimationCustom);

            if (PlayShootSound)
                SoundManager.inst.PlaySound(gameObject, DefaultSounds.shoot, pitch: CoreHelper.ForwardPitch);

            canShoot = false;

            var player = rb.gameObject;

            int s = Mathf.Clamp(currentModel.bulletPart.shapeObj.type, 0, ObjectManager.inst.objectPrefabs.Count - 1);
            int so = Mathf.Clamp(currentModel.bulletPart.shapeObj.option, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

            if (s == 4 || s == 6)
            {
                s = 0;
                so = 0;
            }

            var pulse = ObjectManager.inst.objectPrefabs[s].options[so].Duplicate(ObjectManager.inst.objectParent.transform);
            pulse.transform.localScale = new Vector3(currentModel.bulletPart.startScale.x, currentModel.bulletPart.startScale.y, 1f);

            var vec = new Vector3(currentModel.bulletPart.origin.x, currentModel.bulletPart.origin.y, 0f);
            if (rotateMode == RotateMode.FlipX && lastMovement.x < 0f)
                vec.x = -vec.x;

            pulse.transform.position = player.transform.position + vec;
            pulse.transform.GetChild(0).localPosition = new Vector3(currentModel.bulletPart.startPosition.x, currentModel.bulletPart.startPosition.y, currentModel.bulletPart.depth);
            pulse.transform.GetChild(0).localRotation = Quaternion.Euler(new Vector3(0f, 0f, currentModel.bulletPart.startRotation));

            if (!AllowPlayersToTakeBulletDamage || !currentModel.bulletPart.hurtPlayers)
            {
                pulse.tag = Tags.HELPER;
                pulse.transform.GetChild(0).tag = Tags.HELPER;
            }

            pulse.transform.GetChild(0).gameObject.name = "bullet (Player " + (playerIndex + 1).ToString() + ")";

            float speed = Mathf.Clamp(currentModel.bulletPart.speed, 0.001f, 20f) / CoreHelper.ForwardPitch;
            var b = pulse.AddComponent<Bullet>();
            b.speed = speed;
            b.player = this;
            b.Assign();

            pulse.transform.localRotation = player.transform.localRotation;

            //Destroy
            {
                Destroy(pulse.transform.GetChild(0).GetComponent<SelectObjectInEditor>());
                Destroy(pulse.transform.GetChild(0).GetComponent<SelectObject>());
            }

            MeshRenderer pulseRenderer = pulse.transform.GetChild(0).GetComponent<MeshRenderer>();
            var pulseObject = new EmittedObject
            {
                renderer = pulseRenderer,
                startColor = currentModel.bulletPart.startColor,
                endColor = currentModel.bulletPart.endColor,
                startCustomColor = currentModel.bulletPart.startCustomColor,
                endCustomColor = currentModel.bulletPart.endCustomColor,
            };

            emitted.Add(pulseObject);

            pulseRenderer.enabled = true;
            pulseRenderer.material = head.renderer.material;
            pulseRenderer.material.shader = head.renderer.material.shader;
            Color colorBase = head.renderer.material.color;

            var collider2D = pulse.transform.GetChild(0).GetComponent<Collider2D>();
            collider2D.enabled = true;
            //collider2D.isTrigger = false;

            var rb2D = pulse.transform.GetChild(0).gameObject.AddComponent<Rigidbody2D>();
            rb2D.gravityScale = 0f;

            var bulletCollider = pulse.transform.GetChild(0).gameObject.AddComponent<BulletCollider>();
            bulletCollider.rb = rb;
            bulletCollider.kill = currentModel.bulletPart.autoKill;
            bulletCollider.player = this;
            bulletCollider.emit = pulseObject;

            int easingPos = currentModel.bulletPart.easingPosition;
            int easingSca = currentModel.bulletPart.easingScale;
            int easingRot = currentModel.bulletPart.easingRotation;
            int easingOpa = currentModel.bulletPart.easingOpacity;
            int easingCol = currentModel.bulletPart.easingColor;

            float posDuration = Mathf.Clamp(currentModel.bulletPart.durationPosition, 0.001f, 20f) / CoreHelper.ForwardPitch;
            float scaDuration = Mathf.Clamp(currentModel.bulletPart.durationScale, 0.001f, 20f) / CoreHelper.ForwardPitch;
            float rotDuration = Mathf.Clamp(currentModel.bulletPart.durationScale, 0.001f, 20f) / CoreHelper.ForwardPitch;
            float lifeTime = Mathf.Clamp(currentModel.bulletPart.lifeTime, 0.001f, 20f) / CoreHelper.ForwardPitch;

            pulse.transform.GetChild(0).DOLocalMove(new Vector3(currentModel.bulletPart.endPosition.x, currentModel.bulletPart.endPosition.y, currentModel.bulletPart.depth), posDuration).SetEase(DataManager.inst.AnimationList[easingPos].Animation);
            pulse.transform.DOScale(new Vector3(currentModel.bulletPart.endScale.x, currentModel.bulletPart.endScale.y, 1f), scaDuration).SetEase(DataManager.inst.AnimationList[easingSca].Animation);
            pulse.transform.GetChild(0).DOLocalRotate(new Vector3(0f, 0f, currentModel.bulletPart.endRotation), rotDuration).SetEase(DataManager.inst.AnimationList[easingRot].Animation);

            DOTween.To(x =>
            {
                pulseObject.opacity = x;
            }, currentModel.bulletPart.startOpacity, currentModel.bulletPart.endOpacity, posDuration).SetEase(DataManager.inst.AnimationList[easingOpa].Animation);
            DOTween.To(x =>
            {
                pulseObject.colorTween = x;
            }, 0f, 1f, posDuration).SetEase(DataManager.inst.AnimationList[easingCol].Animation);

            StartCoroutine(CanShoot());

            var tweener = DOTween.To(x => { }, 1f, 1f, lifeTime).SetEase(DataManager.inst.AnimationList[easingOpa].Animation);
            bulletCollider.tweener = tweener;

            tweener.OnComplete(() =>
            {
                var tweenScale = pulse.transform.GetChild(0).DOScale(Vector3.zero, 0.2f).SetEase(DataManager.inst.AnimationList[2].Animation);
                bulletCollider.tweener = tweenScale;

                tweenScale.OnComplete(() =>
                {
                    Destroy(pulse);
                    emitted.Remove(pulseObject);
                    pulseObject = null;
                });
            });
        }

        /// <summary>
        /// Changes how the players collision works.
        /// </summary>
        /// <param name="enabled">True if the player can phase through walls.</param>
        public void SetTriggerCollision(bool enabled)
        {
            if (!ChangeIsTriggerOnBoost)
            {
                circleCollider2D.isTrigger = false;
                polygonCollider2D.isTrigger = false;
                return;
            }

            circleCollider2D.isTrigger = enabled;
            polygonCollider2D.isTrigger = enabled;
        }

        #region Particles

        public void PlaySpawnParticles()
        {
            CoreHelper.Log($"Spawn particles");
            spawn.Play();
        }

        public void PlayDeathParticles()
        {
            CoreHelper.Log($"Death particles");
            death.Play();
        }

        public void PlayHitParticles()
        {
            CoreHelper.Log($"Hit particles");
            burst.Play();
        }

        #endregion

        #region Internal

        internal void HandleCollision(Component other, bool stay = true)
        {
            isColliderTrigger = other.tag == Tags.PLAYER || other is Collider2D collider2D && collider2D.isTrigger || other is Collider collider && collider.isTrigger;
            triggerColliding = true;
            if (CanTakeDamage && (!stay || !isBoosting) && CollisionCheck(other))
                Hit();

            if (!Core)
                return;

            var control = Core.GetControl();
            var modifierBlock = control.CollideModifierBlock;
            if (modifierBlock)
                ModifiersHelper.RunModifiersLoop(modifierBlock.Modifiers, Core, new Dictionary<string, string>());
        }

        bool CollisionCheck(Component other) => other.tag != Tags.HELPER && (other.tag == Tags.PLAYER && AllowPlayersToHitOthers || other.tag != Tags.PLAYER) && other.name != $"bullet (Player {playerIndex + 1})";

        IEnumerator BoostCooldownLoop()
        {
            var headTrail = boost.trailRenderer;
            if (Model && Model.boostPart.Trail.emitting)
                headTrail.emitting = false;

            animationController.Play(new RTAnimation("Player Stretch")
            {
                animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, stretchAmount * 1.5f, Ease.Linear),
                        new FloatKeyframe(1.5f, 0f, Ease.GetEaseFunction(DataManager.inst.AnimationList[stretchEasing].Name)),
                    }, x => { stretchVector = new Vector2(x, -x); }),
                },
            });

            yield return CoroutineHelper.Seconds(boostCooldown / CoreHelper.ForwardPitch);
            CanBoost = true;
            if (PlayBoostRecoverSound && (!PlayerConfig.Instance.PlaySoundRBoostTail.Value || showBoostTail))
                SoundManager.inst.PlaySound(DefaultSounds.boost_recover);

            if (showBoostTail)
            {
                path[1].active = true;
                var tweener = boostTail.parent.DOScale(Vector3.one, 0.1f / CoreHelper.ForwardPitch).SetEase(DataManager.inst.AnimationList[9].Animation);
                tweener.OnComplete(() => animatingBoost = false);
            }
            yield break;
        }

        IEnumerator IKill()
        {
            if (RTBeatmap.Current)
                RTBeatmap.Current.playerDied = true;
            isDead = true;
            playerDeathEvent?.Invoke(rb.position);
            Core.active = false;
            Core.health = 0;
            //anim.SetTrigger("kill");
            InitDeathAnimation();
            InputDataManager.inst.SetControllerRumble(playerIndex, 1f);
            Example.Current?.brain?.Notice(ExampleBrain.Notices.PLAYER_DEATH, new PlayerNoticeParameters(Core));
            yield return CoroutineHelper.SecondsRealtime(0.2f);
            InputDataManager.inst.StopControllerRumble(playerIndex);
            yield break;
        } // if you want to kill the player, just set health to zero.

        void InitMidSpawn()
        {
            CanMove = true;
            CanBoost = true;
        }

        void InitAfterSpawn()
        {
            if (boostCoroutine != null)
                StopCoroutine(boostCoroutine);
            CanMove = true;
            CanBoost = true;
            CanTakeDamage = true;
            isSpawning = false;
        }

        void InitBeforeBoost()
        {
            if (RTBeatmap.Current && rb)
                RTBeatmap.Current.boosts.Add(new PlayerDataPoint(rb.position));
            CanBoost = false;
            isBoosting = true;
            CanTakeDamage = false;
        }

        IEnumerator BoostCancel(float offset)
        {
            isBoostCancelled = true;
            yield return CoroutineHelper.Seconds(offset);
            SetTriggerCollision(false);
            isBoosting = false;
            if (!isTakingHit)
                CanTakeDamage = true;

            yield return CoroutineHelper.Seconds(0.1f);
            InitAfterBoost();
            InitBoostEndAnimation();
            yield break;
        }

        void InitAfterBoost()
        {
            SetTriggerCollision(false);
            isBoosting = false;
            isBoostCancelled = false;
            boostCoroutine = StartCoroutine(BoostCooldownLoop());
        }

        void InitBeforeHit()
        {
            startHurtTime = Time.time;
            CanBoost = true;
            SetTriggerCollision(false);
            isBoosting = false;
            if (RTBeatmap.Current)
                RTBeatmap.Current.playerHit = true;
            isTakingHit = true;
            CanTakeDamage = false;

            Example.Current?.brain?.Notice(ExampleBrain.Notices.PLAYER_HIT, new PlayerNoticeParameters(Core));

            SoundManager.inst.PlaySound(CoreConfig.Instance.Language.Value == Language.Pirate ? DefaultSounds.pirate_KillPlayer : DefaultSounds.HurtPlayer);
        }

        IEnumerator CanShoot()
        {
            var currentModel = Model;
            if (currentModel)
            {
                var cooldown = currentModel.bulletPart.cooldown;
                yield return CoroutineHelper.Seconds(cooldown);
            }
            canShoot = true;

            yield break;
        }

        KeyCode GetKeyCode(int key)
        {
            if (key < 91)
                switch (key)
                {
                    case 0: return KeyCode.None;
                    case 1: return KeyCode.Backspace;
                    case 2: return KeyCode.Tab;
                    case 3: return KeyCode.Clear;
                    case 4: return KeyCode.Return;
                    case 5: return KeyCode.Pause;
                    case 6: return KeyCode.Escape;
                    case 7: return KeyCode.Space;
                    case 8: return KeyCode.Quote;
                    case 9: return KeyCode.Comma;
                    case 10: return KeyCode.Minus;
                    case 11: return KeyCode.Period;
                    case 12: return KeyCode.Slash;
                    case 13: return KeyCode.Alpha0;
                    case 14: return KeyCode.Alpha1;
                    case 15: return KeyCode.Alpha2;
                    case 16: return KeyCode.Alpha3;
                    case 17: return KeyCode.Alpha4;
                    case 18: return KeyCode.Alpha5;
                    case 19: return KeyCode.Alpha6;
                    case 20: return KeyCode.Alpha7;
                    case 21: return KeyCode.Alpha8;
                    case 22: return KeyCode.Alpha9;
                    case 23: return KeyCode.Semicolon;
                    case 24: return KeyCode.Equals;
                    case 25: return KeyCode.LeftBracket;
                    case 26: return KeyCode.RightBracket;
                    case 27: return KeyCode.Backslash;
                    case 28: return KeyCode.A;
                    case 29: return KeyCode.B;
                    case 30: return KeyCode.C;
                    case 31: return KeyCode.D;
                    case 32: return KeyCode.E;
                    case 33: return KeyCode.F;
                    case 34: return KeyCode.G;
                    case 35: return KeyCode.H;
                    case 36: return KeyCode.I;
                    case 37: return KeyCode.J;
                    case 38: return KeyCode.K;
                    case 39: return KeyCode.L;
                    case 40: return KeyCode.M;
                    case 41: return KeyCode.N;
                    case 42: return KeyCode.O;
                    case 43: return KeyCode.P;
                    case 44: return KeyCode.Q;
                    case 45: return KeyCode.R;
                    case 46: return KeyCode.S;
                    case 47: return KeyCode.T;
                    case 48: return KeyCode.U;
                    case 49: return KeyCode.V;
                    case 50: return KeyCode.W;
                    case 51: return KeyCode.X;
                    case 52: return KeyCode.Y;
                    case 53: return KeyCode.Z;
                    case 54: return KeyCode.Keypad0;
                    case 55: return KeyCode.Keypad1;
                    case 56: return KeyCode.Keypad2;
                    case 57: return KeyCode.Keypad3;
                    case 58: return KeyCode.Keypad4;
                    case 59: return KeyCode.Keypad5;
                    case 60: return KeyCode.Keypad6;
                    case 61: return KeyCode.Keypad7;
                    case 62: return KeyCode.Keypad8;
                    case 63: return KeyCode.Keypad9;
                    case 64: return KeyCode.KeypadDivide;
                    case 65: return KeyCode.KeypadMultiply;
                    case 66: return KeyCode.KeypadMinus;
                    case 67: return KeyCode.KeypadPlus;
                    case 68: return KeyCode.KeypadEnter;
                    case 69: return KeyCode.UpArrow;
                    case 70: return KeyCode.DownArrow;
                    case 71: return KeyCode.RightArrow;
                    case 72: return KeyCode.LeftArrow;
                    case 73: return KeyCode.Insert;
                    case 74: return KeyCode.Home;
                    case 75: return KeyCode.End;
                    case 76: return KeyCode.PageUp;
                    case 77: return KeyCode.PageDown;
                    case 78: return KeyCode.RightShift;
                    case 79: return KeyCode.LeftShift;
                    case 80: return KeyCode.RightControl;
                    case 81: return KeyCode.LeftControl;
                    case 82: return KeyCode.RightAlt;
                    case 83: return KeyCode.LeftAlt;
                    case 84: return KeyCode.Mouse0;
                    case 85: return KeyCode.Mouse1;
                    case 86: return KeyCode.Mouse2;
                    case 87: return KeyCode.Mouse3;
                    case 88: return KeyCode.Mouse4;
                    case 89: return KeyCode.Mouse5;
                    case 90: return KeyCode.Mouse6;
                }

            if (key > 90)
            {
                int num = key + 259;

                if (IndexToInt(Core.playerIndex) > 0)
                {
                    string str = (IndexToInt(Core.playerIndex) * 2).ToString() + "0";
                    num += int.Parse(str);
                }

                return (KeyCode)num;
            }

            return KeyCode.None;
        }

        int IndexToInt(PlayerIndex playerIndex) => (int)playerIndex;

        #endregion

        #endregion

        #region Model

        /// <summary>
        /// Updates the players' model.
        /// </summary>
        public void UpdateModel()
        {
            time = 0f;
            if (!Model)
                return;

            var currentModel = Model;

            InitNametag();

            #region Cache

            var control = Core?.GetControl() ?? currentModel.ToPlayerControl();

            tailDistance = currentModel.tailBase.distance;
            tailMode = (int)currentModel.tailBase.mode;
            tailGrows = currentModel.tailBase.grows;

            showBoostTail = currentModel.boostTailPart.active;

            jumpGravity = control.jumpGravity;
            jumpIntensity = control.jumpIntensity;
            jumpCount = control.jumpCount;
            jumpBoostCount = control.jumpBoostCount;
            bounciness = control.bounciness;

            stretch = currentModel.stretchPart.active;
            stretchAmount = currentModel.stretchPart.amount;
            stretchEasing = currentModel.stretchPart.easing;

            rotateMode = (RotateMode)(int)currentModel.basePart.rotateMode;

            #endregion

            Assign(head, currentModel.headPart);
            Assign(boost, currentModel.boostPart, true);
            Assign(boostTail, currentModel.boostTailPart);

            int initialHealthCount = Core ? this.initialHealthCount : currentModel.basePart.health;

            while (tailParts.Count > initialHealthCount)
            {
                CoreHelper.Delete(tailParts[tailParts.Count - 1].parent);
                tailParts.RemoveAt(tailParts.Count - 1);
                path.RemoveAt(path.Count - 2);
            }

            for (int i = 0; i < tailParts.Count; i++)
                Assign(tailParts[i], currentModel.GetTail(i));

            face.gameObject.transform.localPosition = new Vector3(currentModel.facePosition.x, currentModel.facePosition.y, 0f);

            if (!isBoosting)
                path[1].active = showBoostTail;

            rb.sharedMaterial.bounciness = bounciness;

            circleCollider2D.isTrigger = RTBeatmap.Current.Invincible && ZenEditorIncludesSolid;
            polygonCollider2D.isTrigger = RTBeatmap.Current.Invincible && ZenEditorIncludesSolid;

            var colAcc = Core && Core.GetControl().collisionAccurate;
            circleCollider2D.enabled = !colAcc;
            polygonCollider2D.enabled = colAcc;
            if (colAcc)
                polygonCollider2D.CreateCollider(head.meshFilter);

            if (Core && CoreHelper.InEditor)
                Core.Health = RTBeatmap.Current.challengeMode.DefaultHealth > 0 ? RTBeatmap.Current.challengeMode.DefaultHealth : Core.GetControl()?.Health ?? 3;

            var healthSprite = RTFile.FileExists(RTFile.CombinePaths(RTFile.BasePath, $"health{FileFormat.PNG.Dot()}")) && !AssetsGlobal ? SpriteHelper.LoadSprite(RTFile.CombinePaths(RTFile.BasePath, $"health{FileFormat.PNG.Dot()}")) :
                        RTFile.FileExists(RTFile.GetAsset($"health{FileFormat.PNG.Dot()}")) ? SpriteHelper.LoadSprite(RTFile.GetAsset($"health{FileFormat.PNG.Dot()}")) :
                        PlayerManager.healthSprite;

            //Health Images
            foreach (var health in healthObjects)
                if (health.image)
                    health.image.sprite = healthSprite;

            UpdateCustomObjects();

            if (tailGrows)
                GrowTail(initialHealthCount);

            UpdateGUI();

            updated = true;
        }

        /// <summary>
        /// Updates the custom objects of the players' model.
        /// </summary>
        public void UpdateCustomObjects()
        {
            var currentModel = Model;

            var currentModelCustomObjects = currentModel.customObjects;

            foreach (var obj in customObjects)
                Destroy(obj.parent.gameObject);
            customObjects.Clear();

            playerObjects.RemoveAll(x => x.isCustom);

            if (spawnAnimationCustom)
            {
                animationController.Remove(spawnAnimationCustom.id);
                spawnAnimationCustom = null;
            }
            
            if (boostAnimationCustom)
            {
                animationController.Remove(boostAnimationCustom.id);
                boostAnimationCustom = null;
            }
            
            if (healAnimationCustom)
            {
                animationController.Remove(healAnimationCustom.id);
                healAnimationCustom = null;
            }
            
            if (hitAnimationCustom)
            {
                animationController.Remove(hitAnimationCustom.id);
                hitAnimationCustom = null;
            }
            
            if (deathAnimationCustom)
            {
                animationController.Remove(deathAnimationCustom.id);
                deathAnimationCustom = null;
            }
            
            if (shootAnimationCustom)
            {
                animationController.Remove(shootAnimationCustom.id);
                shootAnimationCustom = null;
            }
            
            if (jumpAnimationCustom)
            {
                animationController.Remove(jumpAnimationCustom.id);
                jumpAnimationCustom = null;
            }

            if (currentModelCustomObjects == null || currentModelCustomObjects.IsEmpty())
                return;

            for (int i = 0; i < currentModelCustomObjects.Count; i++)
            {
                var reference = currentModelCustomObjects[i];

                var customObj = new RTCustomPlayerObject()
                {
                    id = reference.id,
                    reference = reference,
                };

                int s = Mathf.Clamp(reference.shape, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                int so = Mathf.Clamp(reference.shapeOption, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);
                var shapeType = (ShapeType)s;

                customObj.parent = ObjectManager.inst.objectPrefabs[s].options[so].Duplicate(customObjectParent).transform;
                customObj.parent.gameObject.SetActive(true);
                customObj.gameObject = customObj.parent.GetChild(0).gameObject;
                customObj.parent.transform.localPosition = Vector3.zero;
                customObj.parent.transform.localScale = Vector3.one;
                customObj.parent.transform.localRotation = Quaternion.identity;
                Destroy(customObj.gameObject.GetComponent<SelectObjectInEditor>());

                customObj.delayTracker = customObj.parent.gameObject.AddComponent<PlayerDelayTracker>();
                customObj.delayTracker.offset = 0;
                customObj.delayTracker.positionOffset = reference.positionOffset;
                customObj.delayTracker.scaleOffset = reference.scaleOffset;
                customObj.delayTracker.rotationOffset = reference.rotationOffset;
                customObj.delayTracker.scaleParent = reference.scaleParent;
                customObj.delayTracker.rotationParent = reference.rotationParent;
                customObj.delayTracker.player = this;

                customObj.gameObject.transform.localPosition = new Vector3(reference.position.x, reference.position.y, reference.depth);
                customObj.gameObject.transform.localScale = new Vector3(reference.scale.x, reference.scale.y, 1f);
                customObj.gameObject.transform.localEulerAngles = new Vector3(0f, 0f, reference.rotation);

                customObj.gameObject.tag = Tags.HELPER;

                var renderer = customObj.gameObject.GetComponentInChildren<Renderer>();
                renderer.enabled = true;
                customObj.renderer = renderer;

                UpdateCustomAnimations(customObj);

                playerObjects.Add(customObj);
                customObjects.Add(customObj);

                switch (shapeType)
                {
                    case ShapeType.Text: {
                            if (customObj.gameObject.gameObject.TryGetComponent(out TextMeshPro tmp))
                            {
                                tmp.text = customObj.reference.text;
                                customObj.text = tmp;
                            }

                            break;
                        }
                    case ShapeType.Image: {
                            if (renderer is SpriteRenderer spriteRenderer)
                            {
                                var sprite = currentModel.assets.GetSprite(reference.text);
                                if (sprite)
                                {
                                    sprite.texture.filterMode = FilterMode.Point;
                                    spriteRenderer.sprite = sprite;
                                    break;
                                }

                                var path = RTFile.CombinePaths(RTFile.BasePath, reference.text);

                                if (!RTFile.FileExists(path))
                                {
                                    spriteRenderer.sprite = LegacyPlugin.PALogoSprite;
                                    break;
                                }

                                CoroutineHelper.StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{path}", texture2D =>
                                {
                                    if (!spriteRenderer)
                                        return;

                                    texture2D.filterMode = FilterMode.Point;
                                    spriteRenderer.sprite = SpriteHelper.CreateSprite(texture2D);
                                }));
                            }

                            break;
                        }
                    case ShapeType.Polygon: {
                            var sides = reference.polygonShape.Sides;
                            var roundness = reference.polygonShape.Roundness;
                            var thickness = reference.polygonShape.Thickness;
                            var slices = reference.polygonShape.Slices;
                            var thicknessOffset = reference.polygonShape.ThicknessOffset;
                            var thicknessScale = reference.polygonShape.ThicknessScale;
                            var angle = reference.polygonShape.Angle;

                            VGShapes.RoundedRingMesh(customObj.gameObject, 0.5f, sides, roundness, thickness, slices, thicknessOffset, thicknessScale, angle);

                            break;
                        }
                    default: {
                            Destroy(customObj.gameObject.GetComponent<Collider2D>());

                            break;
                        }
                }
            }

            UpdateParents();
        }

        void UpdateCustomAnimations(RTCustomPlayerObject customObject)
        {
            var reference = customObject.reference;

            if (!reference || reference.animations == null || reference.animations.IsEmpty())
                return;

            for (int j = 0; j < reference.animations.Count; j++)
            {
                var animation = reference.animations[j];
                if (string.IsNullOrEmpty(animation.ReferenceID))
                    continue;

                RTAnimation runtimeAnim = null;

                switch (animation.ReferenceID.ToLower().Remove(" "))
                {
                    case "boost": {
                            if (!boostAnimationCustom)
                                boostAnimationCustom = new RTAnimation("Player Boost Custom Animation");
                            runtimeAnim = boostAnimationCustom;
                            break;
                        }
                    case "heal": {
                            if (!healAnimationCustom)
                                healAnimationCustom = new RTAnimation("Player Heal Custom Animation");
                            runtimeAnim = healAnimationCustom;
                            break;
                        }
                    case "hit": {
                            if (!hitAnimationCustom)
                                hitAnimationCustom = new RTAnimation("Player Hit Custom Animation");
                            runtimeAnim = hitAnimationCustom;
                            break;
                        }
                    case "death": {
                            if (!deathAnimationCustom)
                                deathAnimationCustom = new RTAnimation("Player Death Custom Animation");
                            runtimeAnim = deathAnimationCustom;
                            break;
                        }
                    case "shoot": {
                            if (!shootAnimationCustom)
                                shootAnimationCustom = new RTAnimation("Player Shoot Custom Animation");
                            runtimeAnim = shootAnimationCustom;
                            break;
                        }
                    case "jump": {
                            if (!jumpAnimationCustom)
                                jumpAnimationCustom = new RTAnimation("Player Jump Custom Animation");
                            runtimeAnim = jumpAnimationCustom;
                            break;
                        }
                }

                ApplyAnimation(runtimeAnim, animation, customObject);
            }
        }

        /// <summary>
        /// Applies a PA animation to a runtime animation.
        /// </summary>
        /// <param name="runtimeAnim">The RTAnimation.</param>
        /// <param name="animation">The PA Animation reference.</param>
        /// <param name="customObject">Custom object to animate.</param>
        public void ApplyAnimation(RTAnimation runtimeAnim, PAAnimation animation, RTCustomPlayerObject customObject)
        {
            var reference = customObject.reference;

            if (!runtimeAnim || !reference)
                return;

            if (animation.animatePosition)
                for (int i = 0; i < animation.positionKeyframes.Count; i++)
                {
                    var positionKeyframes = ObjectConverter.GetVector3Keyframes(animation.positionKeyframes, ObjectConverter.DefaultVector3Keyframe);

                    if (animation.transition && customObject.gameObject)
                        positionKeyframes[0].SetValue(customObject.gameObject.transform.localPosition);

                    runtimeAnim.animationHandlers.Add(new AnimationHandler<Vector3>(positionKeyframes, vector =>
                    {
                        customObject.idle = false;
                        if (customObject.gameObject)
                            customObject.gameObject.transform.localPosition = (new Vector3(reference.position.x, reference.position.y, reference.depth) + vector);
                    }, () => customObject.idle = true));
                }
            if (animation.animateScale)
                for (int i = 0; i < animation.scaleKeyframes.Count; i++)
                {
                    var scaleKeyframes = ObjectConverter.GetVector2Keyframes(animation.scaleKeyframes, ObjectConverter.DefaultVector2Keyframe);

                    if (animation.transition && customObject.gameObject)
                        scaleKeyframes[0].SetValue(customObject.gameObject.transform.localScale);

                    runtimeAnim.animationHandlers.Add(new AnimationHandler<Vector2>(scaleKeyframes, vector =>
                    {
                        customObject.idle = false;
                        if (customObject.gameObject)
                            customObject.gameObject.transform.localScale = (new Vector3(reference.scale.x, reference.scale.y, 1f) * vector);
                    }, () => customObject.idle = true));
                }
            if (animation.animateRotation)
                for (int i = 0; i < animation.rotationKeyframes.Count; i++)
                {
                    var rotationKeyframes = ObjectConverter.GetFloatKeyframes(animation.rotationKeyframes, 0, ObjectConverter.DefaultFloatKeyframe);

                    if (animation.transition && customObject.gameObject)
                        rotationKeyframes[0].SetValue(customObject.gameObject.transform.localEulerAngles.z);

                    runtimeAnim.animationHandlers.Add(new AnimationHandler<float>(rotationKeyframes, x =>
                    {
                        customObject.idle = false;
                        if (customObject.gameObject)
                            customObject.gameObject.transform.localEulerAngles = (new Vector3(0f, 0f, reference.rotation + x));
                    }, () => customObject.idle = true));
                }
        }

        /// <summary>
        /// Updates the parents of the custom objects.
        /// </summary>
        public void UpdateParents()
        {
            foreach (var customObject in customObjects)
                UpdateParent(customObject);
        }

        /// <summary>
        /// Checks if a visibility setting is active.
        /// </summary>
        /// <param name="visiblity">Visibility to check.</param>
        /// <returns>Returns true if visibility is active, otherwise returns false.</returns>
        public bool CheckVisibility(CustomPlayerObject.Visibility visiblity)
        {
            var value = visiblity.command switch
            {
                "isBoosting" => isBoosting,
                "isTakingHit" => isTakingHit,
                "isZenMode" => RTBeatmap.Current.Invincible,
                "isHealthPercentageGreater" => (float)Core.health / initialHealthCount * 100f >= visiblity.value,
                "isHealthGreaterEquals" => Core.health >= visiblity.value,
                "isHealthEquals" => Core.health == visiblity.value,
                "isHealthGreater" => Core.health > visiblity.value,
                "isPressingKey" => Input.GetKey(GetKeyCode((int)visiblity.value)),
                _ => true,
            };

            return visiblity.not ? !value : value;
        }

        /// <summary>
        /// Grows the players' tail.
        /// </summary>
        public void GrowTail(int initialHealthCount)
        {
            for (int i = 0; i < initialHealthCount; i++)
            {
                var modelPart = Model.GetTail(i);
                RTPlayerObject tailPart;

                if (i >= tailParts.Count)
                {
                    int num = tailParts.Count + 1;
                    var last = tailParts.Last();
                    var tailBase = last.parent.gameObject.Duplicate(tailParent, $"Tail {num} Base");
                    tailBase.transform.localScale = Vector3.one;
                    var tail = tailBase.transform.GetChild(0);
                    tail.name = num.ToString();

                    var playerDelayTracker = tailBase.GetOrAddComponent<PlayerDelayTracker>();
                    playerDelayTracker.offset = -num * tailDistance / 2f;
                    playerDelayTracker.positionOffset *= (-num + 4);
                    playerDelayTracker.player = this;
                    playerDelayTracker.leader = tailTracker.transform;

                    var tailParticles = tailBase.transform.Find("tail-particles");

                    tailPart = new RTPlayerObject
                    {
                        id = (num + 99).ToString(),
                        parent = tailBase.transform,
                        gameObject = tail.gameObject,
                        meshFilter = tail.GetComponent<MeshFilter>(),
                        renderer = tail.GetComponent<MeshRenderer>(),
                        delayTracker = playerDelayTracker,
                        trailRenderer = tail.GetComponent<TrailRenderer>(),
                        particleSystem = tailParticles.GetComponent<ParticleSystem>(),
                        particleSystemRenderer = tailParticles.GetComponent<ParticleSystemRenderer>(),
                    };
                    tailParts.Add(tailPart);
                    playerObjects.Add(tailPart);
                    tail.transform.localPosition = new Vector3(0f, 0f, modelPart.depth);
                    tailBase.transform.localPosition = last.parent.localPosition;
                    path.Insert(path.Count - 1, new MovementPath(tailBase.transform.localPosition, tailBase.transform.localRotation, tailBase.transform));
                }
                else
                    tailPart = tailParts[i];

                Assign(tailPart, modelPart);

                if (i >= healthObjects.Count)
                {
                    var last = healthObjects.Last();
                    var healthObject = last.gameObject.Duplicate(healthText.transform);

                    healthObjects.Add(new HealthObject(healthObject, healthObject.GetComponent<Image>()));
                }
            }
        }

        /// <summary>
        /// Updates the players' tail.
        /// </summary>
        /// <param name="health">Health to update.</param>
        /// <param name="pos">Position the player was updated at.</param>
        public void UpdateTail(int health, Vector3 pos)
        {
            // increase tail length if tailGrows is true
            if (health > initialHealthCount)
            {
                initialHealthCount = health;

                if (tailGrows || tailParts.Count < Model.tailParts.Count)
                    GrowTail(tailGrows ? initialHealthCount : Model.tailParts.Count);
            }

            for (int i = 0; i < tailParts.Count; i++)
            {
                var tailPart = tailParts[i];
                if (tailPart.parent)
                    tailPart.parent.gameObject.SetActive(i < health);
            }

            UpdateGUI();
        }

        /// <summary>
        /// Initializes the nametag.
        /// </summary>
        public void InitNametag()
        {
            Destroy(canvas);
            canvas = Creator.NewGameObject("Name Tag Canvas" + (playerIndex + 1).ToString(), transform);
            canvas.transform.localRotation = Quaternion.identity;

            var nametagBase = ObjectManager.inst.objectPrefabs[0].options[0].Duplicate(canvas.transform);
            nametagBase.transform.localScale = Vector3.one;
            nametagBase.transform.localRotation = Quaternion.identity;

            nametagBase.transform.GetChild(0).transform.localScale = new Vector3(6.5f, 1.5f, 1f);
            nametagBase.transform.GetChild(0).transform.localPosition = new Vector3(0f, 2.5f, -0.3f);

            this.nametagBase = nametagBase.GetComponentInChildren<MeshRenderer>();
            this.nametagBase.enabled = true;

            Destroy(nametagBase.GetComponentInChildren<SelectObject>());
            Destroy(nametagBase.GetComponentInChildren<SelectObjectInEditor>());
            Destroy(nametagBase.GetComponentInChildren<Collider2D>());

            var tae = ObjectManager.inst.objectPrefabs[4].options[0].Duplicate(canvas.transform);
            tae.transform.localScale = Vector3.one;
            tae.transform.localRotation = Quaternion.identity;

            tae.transform.GetChild(0).transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            tae.transform.GetChild(0).transform.localPosition = new Vector3(0f, 2.5f, -0.3f);
            Destroy(tae.GetComponentInChildren<SelectObjectInEditor>());

            nametagText = tae.GetComponentInChildren<TextMeshPro>();
            nametagText.text = NametagText;
            nametagText.color = Color.white;

            var d = canvas.AddComponent<PlayerDelayTracker>();
            d.leader = rb.transform;
            d.scaleParent = false;
            d.rotationParent = false;
            d.player = this;
            d.positionOffset = 0.9f;

            canvas.SetActive(PlayerManager.Players.Count > 1 && Core && ShowNameTags);
        }

        public void UpdateGUI()
        {
            var currentModel = Model;

            if (!currentModel || !Core)
                return;

            var health = Core.Health;

            if (!healthObjects.IsEmpty())
                for (int i = 0; i < healthObjects.Count; i++)
                    healthObjects[i].gameObject.SetActive(i < health && currentModel.guiPart.active && currentModel.guiPart.mode == PlayerModel.GUI.GUIHealthMode.Images);

            var text = healthText;
            var isText = currentModel.guiPart.active && (currentModel.guiPart.mode == PlayerModel.GUI.GUIHealthMode.Text || currentModel.guiPart.mode == PlayerModel.GUI.GUIHealthMode.EqualsBar);
            var isBar = currentModel.guiPart.active && currentModel.guiPart.mode == PlayerModel.GUI.GUIHealthMode.Bar;

            text.enabled = isText;
            if (isText)
                text.text = currentModel.guiPart.mode == PlayerModel.GUI.GUIHealthMode.Text ? health.ToString() : RTString.ConvertHealthToEquals(health, initialHealthCount);

            barBaseIm.gameObject.SetActive(isBar);
            if (isBar)
                barIm.rectTransform.sizeDelta = new Vector2(200f * (float)health / (float)initialHealthCount, 0f);
        }

        void Assign(RTPlayerObject rtPlayerObject, IPlayerObject playerObject, bool isBoost = false)
        {
            rtPlayerObject.renderer.enabled = playerObject.Active;
            var trailRenderer = rtPlayerObject.trailRenderer;
            var particleSystem = rtPlayerObject.particleSystem;
            if (!playerObject.Active)
            {
                if (trailRenderer)
                {
                    trailRenderer.enabled = false;
                    trailRenderer.emitting = false;
                }
                if (particleSystem)
                {
                    var p = particleSystem.emission;
                    p.enabled = false;

                    if (rtPlayerObject.particleSystemRenderer)
                        rtPlayerObject.particleSystemRenderer.enabled = false;
                }
                return;
            }

            if (playerObject.ShapeType == ShapeType.Polygon)
            {
                VGShapes.RoundedRingMesh(rtPlayerObject.meshFilter, null,
                    playerObject.Polygon.Radius,
                    playerObject.Polygon.Sides,
                    playerObject.Polygon.Roundness,
                    playerObject.Polygon.Thickness,
                    playerObject.Polygon.Slices,
                    playerObject.Polygon.ThicknessOffset,
                    playerObject.Polygon.ThicknessScale,
                    playerObject.Polygon.Angle);
            }
            else
                rtPlayerObject.meshFilter.mesh = GetShape(playerObject.Shape, playerObject.ShapeOption).mesh;

            rtPlayerObject.gameObject.transform.localPosition = new Vector3(playerObject.Position.x, playerObject.Position.y);
            rtPlayerObject.gameObject.transform.localScale = new Vector3(playerObject.Scale.x, playerObject.Scale.y, 1f);
            rtPlayerObject.gameObject.transform.localEulerAngles = new Vector3(0f, 0f, playerObject.Rotation);
            
            if (trailRenderer)
            {
                trailRenderer.enabled = !isBoost && playerObject.Trail.emitting;
                trailRenderer.emitting = !isBoost && playerObject.Trail.emitting;
                trailRenderer.startWidth = playerObject.Trail.startWidth;
                trailRenderer.endWidth = playerObject.Trail.endWidth;
                trailRenderer.transform.localPosition = playerObject.Trail.positionOffset;
            }

            if (rtPlayerObject.particleSystemRenderer)
                rtPlayerObject.particleSystemRenderer.mesh = GetShape(playerObject.Particles.shape, playerObject.Particles.shapeOption).mesh;

            if (!particleSystem)
                return;

            var main = particleSystem.main;
            var emission = particleSystem.emission;

            main.startLifetime = playerObject.Particles.lifeTime;
            main.startSpeed = playerObject.Particles.speed;

            emission.enabled = playerObject.Particles.emitting;
            particleSystem.emissionRate = isBoost ? 0f : playerObject.Particles.amount;
            if (isBoost)
            {
                emission.burstCount = (int)playerObject.Particles.amount;
                main.duration = 1f;
            }

            var rotationOverLifetime = particleSystem.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.separateAxes = true;
            rotationOverLifetime.xMultiplier = 0f;
            rotationOverLifetime.yMultiplier = 0f;
            rotationOverLifetime.zMultiplier = playerObject.Particles.rotation;

            var forceOverLifetime = particleSystem.forceOverLifetime;
            forceOverLifetime.enabled = true;
            forceOverLifetime.space = ParticleSystemSimulationSpace.World;
            forceOverLifetime.xMultiplier = playerObject.Particles.force.x;
            forceOverLifetime.yMultiplier = playerObject.Particles.force.y;

            var particlesTrail = particleSystem.trails;
            particlesTrail.enabled = playerObject.Particles.trailEmitting;

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var psCol = colorOverLifetime.color;

            float alphaStart = playerObject.Particles.startOpacity;
            float alphaEnd = playerObject.Particles.endOpacity;

            var gradient = new Gradient();
            gradient.alphaKeys = new GradientAlphaKey[2]
            {
                new GradientAlphaKey(alphaStart, 0f),
                new GradientAlphaKey(alphaEnd, 1f)
            };
            gradient.colorKeys = new GradientColorKey[2]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            };

            psCol.gradient = gradient;

            colorOverLifetime.color = psCol;

            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;

            var ssss = sizeOverLifetime.size;

            var sizeStart = playerObject.Particles.startScale;
            var sizeEnd = playerObject.Particles.endScale;

            var curve = new AnimationCurve(new Keyframe[2]
            {
                new Keyframe(0f, sizeStart),
                new Keyframe(1f, sizeEnd)
            });

            ssss.curve = curve;

            sizeOverLifetime.size = ssss;
        }

        Shape GetShape(int type, int option)
        {
            type = Mathf.Clamp(type, 0, ShapeManager.inst.Shapes2D.Count - 1);
            option = Mathf.Clamp(option, 0, ShapeManager.inst.Shapes2D[type].Count - 1);

            return type == 4 || type == 6 ? ShapeManager.inst.Shapes2D[0][0] : ShapeManager.inst.Shapes2D[type][option];
        }

        void UpdateParent(RTCustomPlayerObject customObject)
        {
            var reference = customObject.reference;
            var delayTracker = customObject.delayTracker;

            delayTracker.positionOffset = reference.positionOffset;
            delayTracker.scaleOffset = reference.scaleOffset;
            delayTracker.rotationOffset = reference.rotationOffset;
            delayTracker.scaleParent = reference.scaleParent;
            delayTracker.rotationParent = reference.rotationParent;
            delayTracker.leader = !string.IsNullOrEmpty(reference.customParent) && playerObjects.TryFind(x => x.id == reference.customParent && x.id != customObject.id, out RTPlayerObject parent) ?
                parent.gameObject.transform :
                reference.parent switch
                {
                    0 => rb.transform,
                    1 => boost.parent,
                    2 => boostTail.parent,
                    3 => tailParts[0].parent,
                    4 => tailParts[1].parent,
                    5 => tailParts[2].parent,
                    6 => face.gameObject.transform,
                    _ => tailParts[reference.parent - 4].parent,
                };
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Represents a custom object from the model.
        /// </summary>
        public class RTCustomPlayerObject : RTPlayerObject, ITransformable
        {
            public RTCustomPlayerObject() => isCustom = true;

            public CustomPlayerObject reference;
            public TextMeshPro text;
            public bool idle = true;
            public string currentIdleAnimation = "idle";

            public Vector3 positionOffset;
            public Vector3 scaleOffset;
            public Vector3 rotationOffset;

            public Vector3 PositionOffset { get => positionOffset; set => positionOffset = value; }
            
            public Vector3 ScaleOffset { get => scaleOffset; set => scaleOffset = value; }

            public Vector3 RotationOffset { get => rotationOffset; set => rotationOffset = value; }

            public void ResetOffsets()
            {
                positionOffset = Vector3.zero;
                scaleOffset = Vector3.zero;
                rotationOffset = Vector3.zero;
            }

            public Vector3 GetTransformOffset(int type) => type switch
            {
                0 => positionOffset,
                1 => scaleOffset,
                _ => rotationOffset,
            };

            public void SetTransform(int toType, Vector3 value)
            {
                switch (toType)
                {
                    case 0: {
                            positionOffset = value;
                            break;
                        }
                    case 1: {
                            scaleOffset = value;
                            break;
                        }
                    case 2: {
                            rotationOffset = value;
                            break;
                        }
                }
            }

            public void SetTransform(int toType, int toAxis, float value)
            {
                switch (toType)
                {
                    case 0: {
                            positionOffset[toAxis] = value;
                            break;
                        }
                    case 1: {
                            scaleOffset[toAxis] = value;
                            break;
                        }
                    case 2: {
                            rotationOffset[toAxis] = value;
                            break;
                        }
                }
            }
            public Vector3 GetFullPosition() => gameObject.transform.position;

            public Vector3 GetFullScale() => gameObject.transform.lossyScale;

            public Vector3 GetFullRotation(bool includeSelf) => gameObject.transform.eulerAngles;
        }

        /// <summary>
        /// Represents a spawned object.
        /// </summary>
        public class EmittedObject : RTPlayerObject
        {
            public float opacity;
            public float colorTween;
            public int startColor;
            public int endColor;
            public string startCustomColor;
            public string endCustomColor;
        }

        /// <summary>
        /// Represents a part of the player.
        /// </summary>
        public class RTPlayerObject : Exists
        {
            public bool active = true;
            public string id;

            public Transform parent;
            public GameObject gameObject;
            public Renderer renderer;
            public MeshFilter meshFilter;
            public PlayerDelayTracker delayTracker;

            public TrailRenderer trailRenderer;
            public ParticleSystem particleSystem;
            public ParticleSystemRenderer particleSystemRenderer;

            public bool isCustom;
        }

        /// <summary>
        /// Represents the path of the Legacy tail.
        /// </summary>
        public class MovementPath
        {
            public MovementPath(Vector3 pos, Quaternion rot, Transform transform)
            {
                this.pos = pos;
                this.rot = rot;
                this.transform = transform;
                lastPos = pos;
            }

            public MovementPath(Vector3 pos, Quaternion rot, Transform transform, bool active)
            {
                this.pos = pos;
                this.rot = rot;
                this.transform = transform;
                this.active = active;
                lastPos = pos;
            }

            public bool active = true;

            public Vector3 lastPos;
            public Vector3 pos;

            public Quaternion rot;

            public Transform transform;
        }

        /// <summary>
        /// Represents a health UI image.
        /// </summary>
        public class HealthObject
        {
            public HealthObject(GameObject gameObject, Image image)
            {
                this.gameObject = gameObject;
                this.image = image;
            }

            public GameObject gameObject;
            public Image image;
        }

        #endregion
    }
}
