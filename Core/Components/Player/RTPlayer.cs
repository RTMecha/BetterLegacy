using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Components;
using DG.Tweening;
using LSFunctions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XInputDotNetPure;

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

        /// <summary>
        /// If player does not take damage in editor.
        /// </summary>
        public static bool ZenModeInEditor { get; set; }

        /// <summary>
        /// If zen mode in editor should also consider solid.
        /// </summary>
        public static bool ZenEditorIncludesSolid { get; set; }

        /// <summary>
        /// If multiplayer nametags should display when there are more than one player on-screen.
        /// </summary>
        public static bool ShowNameTags { get; set; }

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
        /// How the player tail should update.
        /// </summary>
        public static TailUpdateMode UpdateMode { get; set; } = TailUpdateMode.FixedUpdate;

        /// <summary>
        /// If custom assets should be loaded from a global source.
        /// </summary>
        public static bool AssetsGlobal { get; set; }

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
        /// How fast all players are.
        /// </summary>
        public static float SpeedMultiplier { get; set; } = 1f;

        /// <summary>
        /// The current force to apply to players.
        /// </summary>
        public static Vector2 PlayerForce { get; set; }

        /// <summary>
        /// Updates player properties based on <see cref="LevelData"/>.
        /// </summary>
        public static void SetGameDataProperties()
        {
            try
            {
                var levelData = GameData.Current.beatmapData.levelData;
                LockBoost = levelData.lockBoost;
                SpeedMultiplier = levelData.speedMultiplier;
                GameMode = (GameMode)levelData.gameMode;
                JumpGravity = levelData.jumpGravity;
                JumpIntensity = levelData.jumpIntensity;
                MaxJumpCount = levelData.maxJumpCount;
                MaxJumpBoostCount = levelData.maxJumpBoostCount;
                CustomPlayer.MaxHealth = levelData.maxHealth;

                if (CoreHelper.InEditor && PlayerManager.Players.Count > 0)
                {
                    foreach (var customPlayer in PlayerManager.Players)
                        if (customPlayer.PlayerModel)
                            customPlayer.Health = customPlayer.PlayerModel.basePart.health;
                }
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Could not set properties {ex}");
            }

        }

        #region Base

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
        public CustomPlayer CustomPlayer { get; set; }

        /// <summary>
        /// Player model reference.
        /// </summary>
        public PlayerModel PlayerModel { get; set; }

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

        public GameObject healthText;

        private Image barIm;
        private Image barBaseIm;

        public ParticleSystem burst;
        public ParticleSystem death;
        public ParticleSystem spawn;

        public Transform tailParent;
        public GameObject tailTracker;

        public Animator anim;
        public Rigidbody2D rb;
        public CircleCollider2D circleCollider2D;
        public PolygonCollider2D polygonCollider2D;

        public Transform customObjectParent;

        public Collider2D CurrentCollider => PlayerModel != null && PlayerModel.basePart.collisionAccurate ? polygonCollider2D : circleCollider2D;

        #endregion

        #region Bool

        public bool canBoost = true;
        public bool canMove = true;
        public bool canRotate = true;
        public bool canTakeDamage;

        public bool isTakingHit;
        public bool isBoosting;
        public bool isBoostCancelled;
        public bool isDead = true;

        public bool isKeyboard;
        public bool animatingBoost;

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

        #region Properties

        public bool CanTakeDamage
        {
            get => (CoreHelper.InEditor || CoreHelper.InStory || !PlayerManager.IsZenMode) && !CoreHelper.Paused && !CoreHelper.IsEditing && canTakeDamage;
            set => canTakeDamage = value;
        }

        public bool CanMove
        {
            get => canMove;
            set => canMove = value;
        }

        public bool CanRotate
        {
            get => canRotate;
            set => canRotate = value;
        }

        public bool CanBoost
        {
            get => CoreHelper.InEditorPreview && canBoost && !isBoosting && (PlayerModel == null || PlayerModel.basePart.canBoost) && !CoreHelper.Paused && !CoreHelper.IsUsingInputField;
            set => canBoost = value;
        }

        public bool Alive => CustomPlayer && CustomPlayer.Health > 0 && !isDead;

        #endregion

        #region Delegates

        public delegate void PlayerHitDelegate(int _health, Vector3 _pos);

        public delegate void PlayerDeathDelegate(Vector3 _pos);

        public event PlayerHitDelegate playerHitEvent;

        public event PlayerDeathDelegate playerDeathEvent;

        #endregion

        #region Spawn

        void Awake()
        {
            customObjectParent = Creator.NewGameObject("Custom Objects", transform).transform;
            customObjectParent.transform.localPosition = Vector3.zero;

            var anim = gameObject.GetComponent<Animator>();
            anim.keepAnimatorControllerStateOnDisable = true;
            this.anim = anim;

            var rb = transform.Find("Player").gameObject;
            this.rb = rb.GetComponent<Rigidbody2D>();

            basePart = new PlayerObject
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
                rb.AddComponent<PlayerSelector>().id = playerIndex;

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

            this.head = new PlayerObject
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

            var faceBase = Creator.NewGameObject("face-base", rb.transform);
            faceBase.transform.localPosition = Vector3.zero;

            var faceParent = Creator.NewGameObject("face-parent", faceBase.transform);
            faceParent.transform.localPosition = Vector3.zero;
            faceParent.transform.localRotation = Quaternion.identity;

            face = new PlayerObject
            {
                id = "6",
                parent = faceBase.transform,
                gameObject = faceParent,
            };
            playerObjects.Add(face);

            path.Add(new MovementPath(Vector3.zero, Quaternion.identity, rb.transform)); // base path

            tailParent = transform.Find("trail");
            tailTracker = Creator.NewGameObject("tail-tracker", rb.transform);
            tailTracker.transform.localPosition = new Vector3(-0.5f, 0f, 0.1f);
            tailTracker.transform.localRotation = Quaternion.identity;

            var boost = transform.Find("Player/boost").gameObject;
            boost.transform.localScale = Vector3.zero;

            var boostBase = Creator.NewGameObject("Boost Base", transform.Find("Player"));
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

            this.boost = new PlayerObject
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

            var boostTail = boostBase.Duplicate(transform.Find("trail"), "Boost Tail");
            boostTail.layer = 8;

            var child = boostTail.transform.GetChild(0);

            var boostDelayTracker = boostTail.AddComponent<PlayerDelayTracker>();
            boostDelayTracker.leader = tailTracker.transform;
            boostDelayTracker.player = this;

            this.boostTail = new PlayerObject
            {
                id = "2",
                parent = boostTail.transform,
                gameObject = child.gameObject,
                meshFilter = child.GetComponent<MeshFilter>(),
                renderer = child.GetComponent<MeshRenderer>(),
                delayTracker = boostDelayTracker,
            };

            path.Add(new MovementPath(Vector3.zero, Quaternion.identity, boostTail.transform, showBoostTail));

            for (int i = 1; i < 4; i++)
            {
                var name = $"Tail {i} Base";
                var tailBase = Creator.NewGameObject(name, transform.Find("trail"));
                var tail = tailParent.Find($"{i}");
                tail.SetParent(tailBase.transform);
                tailBase.layer = 8;

                var playerDelayTracker = tailBase.AddComponent<PlayerDelayTracker>();
                playerDelayTracker.offset = -i * tailDistance / 2f;
                playerDelayTracker.positionOffset *= (-i + 4);
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

                var tailPart = new PlayerObject
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
                tail.transform.localPosition = new Vector3(0f, 0f, 0.1f);
                path.Add(new MovementPath(Vector3.zero, Quaternion.identity, tailBase.transform));
            }

            path.Add(new MovementPath(Vector3.zero, Quaternion.identity, null));

            healthText = PlayerManager.healthImages.Duplicate(PlayerManager.healthParent, $"Health {playerIndex}");

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

            bar.transform.AsRT().pivot = new Vector2(0f, 0.5f);
            bar.transform.AsRT().anchoredPosition = new Vector2(-100f, 0f);

            healthText.SetActive(false);
        }

        public bool playerNeedsUpdating;
        void Start()
        {
            playerHitEvent += UpdateTail;

            if (playerNeedsUpdating)
            {
                Spawn();
                UpdateModel();
            }
        }

        public bool isSpawning;
        public void Spawn()
        {
            CanTakeDamage = false;
            CanBoost = false;
            CanMove = false;
            isDead = false;
            isBoosting = false;
            isSpawning = true;

            if (spawnAnimation != null)
            {
                AnimationManager.inst.Remove(spawnAnimation.id);
                spawnAnimation = null;
            }

            bool initMidSpawn = false;
            bool initAfterSpawn = false;
            spawnAnimation = new RTAnimation("Player Spawn");
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
                }), // base
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
                }), // Player
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
                }), // Trail 1
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
                }), // Trail 2
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
                }), // Trail 3
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 1.4f, Ease.Linear),
                    new FloatKeyframe(1f, 1.4f, Ease.Linear),
                    new FloatKeyframe(1.1f, 0f, Ease.Linear),
                }, x =>
                {
                    if (boost != null && boost.gameObject)
                        boost.gameObject.transform.localScale = new Vector3(x, x, 0.2f);
                }), // Boost
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, 1f, Ease.Linear),
                    new FloatKeyframe(0.13333334f, 0.13333334f, Ease.Linear),
                    new FloatKeyframe(1f, 1f, Ease.Linear),
                    new FloatKeyframe(1.1f, 1f, Ease.Linear),
                }, x =>
                {
                    if (!initMidSpawn && x >= 0.13333334f)
                    {
                        initMidSpawn = true;
                        InitMidSpawn();
                    }
                    if (!initAfterSpawn && x >= 1f)
                    {
                        initAfterSpawn = true;
                        InitAfterSpawn();
                    }
                }), // Events
            };
            spawnAnimation.onComplete = () =>
            {
                AnimationManager.inst.Remove(spawnAnimation.id);
                spawnAnimation = null;

                if (transform)
                    transform.localScale = new Vector3(1f, 1f, 1f);

                if (rb && rb.transform)
                    rb.transform.localScale = new Vector3(1f, 1f, 1f);

                if (tailParts != null)
                    for (int i = 0; i < tailParts.Count; i++)
                        if (tailParts[i].parent)
                            tailParts[i].parent.localScale = Vector3.one;

                if (boost != null && boost.gameObject)
                    boost.gameObject.transform.localScale = new Vector3(0f, 0f, 0.2f);
            };
            AnimationManager.inst.Play(spawnAnimation);
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

        public RTAnimation spawnAnimation;

        #endregion

        #region Update Methods

        void Update()
        {
            if (UpdateMode == TailUpdateMode.Update)
                UpdateTailDistance();

            UpdateCustomTheme(); UpdateBoostTheme(); UpdateSpeeds(); UpdateTrailLengths(); UpdateTheme();
            if (canvas)
            {
                bool act = InputDataManager.inst.players.Count > 1 && CustomPlayer && ShowNameTags;
                canvas.SetActive(act);

                if (act && nametagText)
                {
                    var index = PlayersData.Main.GetMaxIndex(playerIndex, 4);

                    nametagText.text = "<#" + LSColors.ColorToHex(GameManager.inst.LiveTheme.GetPlayerColor(index)) + ">Player " + (playerIndex + 1).ToString() + " " + RTString.ConvertHealthToEquals(CustomPlayer.Health, initialHealthCount);
                    nametagBase.material.color = LSColors.fadeColor(GameManager.inst.LiveTheme.GetPlayerColor(index), 0.3f);
                    nametagBase.transform.localScale = new Vector3((float)initialHealthCount * 2.25f, 1.5f, 1f);
                }
            }

            if (!PlayerModel)
                return;

            if (boost.trailRenderer && PlayerModel.boostPart.Trail.emitting)
            {
                var tf = boost.gameObject.transform;
                Vector2 v = new Vector2(tf.localScale.x, tf.localScale.y);

                boost.trailRenderer.startWidth = PlayerModel.boostPart.Trail.startWidth * v.magnitude / 1.414213f;
                boost.trailRenderer.endWidth = PlayerModel.boostPart.Trail.endWidth * v.magnitude / 1.414213f;
            }

            if (!Alive && !isDead && CustomPlayer && !PlayerManager.IsPractice)
                StartCoroutine(Kill());
        }

        bool canShoot = true;

        void FixedUpdate()
        {
            if (UpdateMode == TailUpdateMode.FixedUpdate)
                UpdateTailDistance();

            healthText?.SetActive(PlayerModel && PlayerModel.guiPart.active && GameManager.inst.timeline.activeSelf);
        }

        void LateUpdate()
        {
            UpdateControls(); UpdateRotation();

            if (UpdateMode == TailUpdateMode.LateUpdate)
                UpdateTailDistance();

            UpdateTailTransform(); UpdateTailDev(); UpdateTailSizes();

            var player = rb.gameObject;

            // Here we handle the player's bounds to the camera. It is possible to include negative zoom in those bounds but it might not be a good idea since people have already utilized it.
            if (!OutOfBounds && !EventsConfig.Instance.EditorCameraEnabled && CoreHelper.Playing)
            {
                var cameraToViewportPoint = Camera.main.WorldToViewportPoint(player.transform.position);
                cameraToViewportPoint.x = Mathf.Clamp(cameraToViewportPoint.x, 0f, 1f);
                cameraToViewportPoint.y = Mathf.Clamp(cameraToViewportPoint.y, 0f, 1f);
                if (Camera.main.orthographicSize > 0f && (!includeNegativeZoom || Camera.main.orthographicSize < 0f) && CustomPlayer)
                {
                    var pos = Camera.main.ViewportToWorldPoint(cameraToViewportPoint);
                    pos.z = player.transform.position.z;
                    player.transform.position = pos;
                }
            }

            if (!PlayerModel || !PlayerModel.FaceControlActive || FaceController == null)
                return;

            var vector = new Vector2(FaceController.Move.Vector.x, FaceController.Move.Vector.y);
            var fp = PlayerModel.FacePosition;
            if (vector.magnitude > 1f)
                vector = vector.normalized;

            if ((rotateMode == RotateMode.FlipX || rotateMode == RotateMode.RotateFlipX) && lastMovement.x < 0f)
                vector.x = -vector.x;
            if ((rotateMode == RotateMode.FlipY || rotateMode == RotateMode.RotateFlipY) && lastMovement.y < 0f)
                vector.y = -vector.y;

            face.gameObject.transform.localPosition = new Vector3(vector.x * 0.3f + fp.x, vector.y * 0.3f + fp.y, 0f);
        }

        float timeHit;
        float timeHitOffset;

        void UpdateSpeeds()
        {
            float pitch = CoreHelper.ForwardPitch;

            if (CoreHelper.Paused)
                pitch = 0f;

            anim.speed = pitch;

            if (!PlayerModel)
                return;

            var idleSpeed = PlayerModel.basePart.moveSpeed;
            var boostSpeed = PlayerModel.basePart.boostSpeed;
            var boostCooldown = PlayerModel.basePart.boostCooldown;
            var minBoostTime = PlayerModel.basePart.minBoostTime;
            var maxBoostTime = PlayerModel.basePart.maxBoostTime;
            var hitCooldown = PlayerModel.basePart.hitCooldown;

            if (GameData.IsValid && GameData.Current.beatmapData != null && GameData.Current.beatmapData.levelData is LevelData levelData && levelData.limitPlayer)
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
            for (int i = 1; i < path.Count; i++)
            {
                int num = i - 1;

                if (i == 2 && !path[1].active)
                    num = i - 2;

                if (Vector3.Distance(path[i].pos, path[num].pos) <= tailDistance)
                    continue;

                Vector3 pos = Vector3.Lerp(path[i].pos, path[num].pos, Time.deltaTime * 12f);
                Quaternion rot = Quaternion.Lerp(path[i].rot, path[num].rot, Time.deltaTime * 12f);

                if (tailMode == 0)
                {
                    path[i].pos = pos;
                    path[i].rot = rot;
                }

                if (tailMode > 1)
                {
                    path[i].pos = new Vector3(RTMath.RoundToNearestDecimal(pos.x, 1), RTMath.RoundToNearestDecimal(pos.y, 1), RTMath.RoundToNearestDecimal(pos.z, 1));

                    var r = rot.eulerAngles;

                    path[i].rot = Quaternion.Euler((int)r.x, (int)r.y, (int)r.z);
                }
            }
        }

        void UpdateTailTransform()
        {
            if (tailMode == 1 || CoreHelper.Paused)
                return;

            var tailBaseTime = PlayerModel.tailBase.time;
            float num = Time.deltaTime * (tailBaseTime == 0f ? 200f : tailBaseTime);
            for (int i = 1; i < path.Count; i++)
            {
                if (path.Count >= i && path[i].transform != null && path[i].transform.gameObject.activeSelf)
                {
                    num *= Vector3.Distance(path[i].lastPos, path[i].pos);
                    path[i].transform.position = Vector3.MoveTowards(path[i].lastPos, path[i].pos, num);
                    path[i].lastPos = path[i].transform.position;
                    path[i].transform.rotation = path[i].rot;
                }
            }
        }

        void UpdateTailDev()
        {
            if (tailMode != 1 || CoreHelper.Paused)
                return;

            for (int i = 1; i < path.Count; i++)
            {
                int num = i;
                if (showBoostTail && path[1].active)
                    num += 1;

                if (i == 1)
                {
                    var playerDelayTracker = boostTail.delayTracker;
                    playerDelayTracker.offset = -i * tailDistance / 2f;
                    playerDelayTracker.positionOffset = 0.1f * (-i + 5);
                    playerDelayTracker.rotationOffset = 0.1f * (-i + 5);
                }

                int tailIndex = i - 2;
                if (tailIndex < 0 || tailIndex >= tailParts.Count)
                    continue;

                var delayTracker = tailParts[tailIndex].delayTracker;
                delayTracker.offset = -num * tailDistance / 2f;
                delayTracker.positionOffset = 0.1f * (-num + 5);
                delayTracker.rotationOffset = 0.1f * (-num + 5);
            }
        }

        void UpdateTailSizes()
        {
            if (!PlayerModel)
                return;

            for (int i = 0; i < PlayerModel.tailParts.Count; i++)
            {
                if (i >= tailParts.Count)
                    continue;

                var t2 = PlayerModel.tailParts[i].scale;

                tailParts[i].gameObject.transform.localScale = new Vector3(t2.x, t2.y, 1f);
            }
        }

        void UpdateTrailLengths()
        {
            if (!PlayerModel)
                return;

            var pitch = CoreHelper.ForwardPitch;

            var headTrail = head.trailRenderer;
            var boostTrail = boost.trailRenderer;

            headTrail.time = PlayerModel.headPart.Trail.time / pitch;
            boostTrail.time = PlayerModel.boostPart.Trail.time / pitch;

            for (int i = 0; i < PlayerModel.tailParts.Count; i++)
            {
                if (i >= tailParts.Count)
                    continue;

                tailParts[i].trailRenderer.time = PlayerModel.tailParts[i].Trail.time / pitch;
            }
        }

        bool queuedBoost;

        void UpdateControls()
        {
            if (!CustomPlayer || !PlayerModel || !Alive)
                return;

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

                    StartBoost();
                    return;
                }

                if (isBoosting && !isBoostCancelled && (Actions.Boost.WasReleased || startBoostTime + maxBoostTime <= Time.time))
                    InitMidBoost(true);
            }

            if (Alive && FaceController != null && PlayerModel.bulletPart.active &&
                (!PlayerModel.bulletPart.constant && FaceController.Shoot.WasPressed && canShoot ||
                    PlayerModel.bulletPart.constant && FaceController.Shoot.IsPressed && canShoot))
                CreateBullet();

            var player = rb.gameObject;

            if (JumpMode)
            {
                rb.gravityScale = jumpGravity * JumpGravity;

                if (Actions == null)
                    return;

                var pitch = CoreHelper.ForwardPitch;
                float x = Actions.Move.Vector.x;
                float y = Actions.Move.Vector.y;

                if (isBoosting)
                {
                    var vector = new Vector2(x, y * jumpBoostMultiplier);

                    rb.velocity = PlayerForce + vector * boostSpeed * pitch * SpeedMultiplier;
                    return;
                }

                var velocity = rb.velocity;
                if (Actions.Boost.WasPressed && (jumpCount != 0 && colliding || jumpCount == -1 || currentJumpCount < Mathf.Clamp(jumpCount, -1, MaxJumpCount)))
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

                if (x != 0f)
                    lastMoveHorizontal = x;

                var sp = PlayerModel.basePart.sprintSneakActive ? FaceController.Sprint.IsPressed ? 1.3f : FaceController.Sneak.IsPressed ? 0.1f : 1f : 1f;

                velocity.x = x * idleSpeed * pitch * sp * SpeedMultiplier;

                rb.velocity = velocity;

                return;
            }

            rb.gravityScale = 0f;

            if (Alive && Actions != null && CustomPlayer.active && CanMove && !CoreHelper.Paused &&
                (CoreConfig.Instance.AllowControlsInputField.Value || !CoreHelper.IsUsingInputField) &&
                movementMode == MovementMode.KeyboardController && (!CoreHelper.InEditor || !EventsConfig.Instance.EditorCamEnabled.Value))
            {
                colliding = false;
                float x = Actions.Move.Vector.x;
                float y = Actions.Move.Vector.y;
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

                var pitch = CoreHelper.ForwardPitch;

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

                    var sp = PlayerModel.basePart.sprintSneakActive ? FaceController.Sprint.IsPressed ? 1.3f : FaceController.Sneak.IsPressed ? 0.1f : 1f : 1f;

                    rb.velocity = PlayerForce + vector * idleSpeed * pitch * sp * SpeedMultiplier;
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

                            if (rotateMode == RotateMode.FlipX)
                            {
                                if (lastMovement.x > 0f)
                                    player.transform.localScale = new Vector3(xt, yt, 1f);
                                if (lastMovement.x < 0f)
                                    player.transform.localScale = new Vector3(-xt, yt, 1f);
                            }

                            if (rotateMode == RotateMode.FlipY)
                            {
                                if (lastMovement.y > 0f)
                                    player.transform.localScale = new Vector3(xt, yt, 1f);
                                if (lastMovement.y < 0f)
                                    player.transform.localScale = new Vector3(xt, -yt, 1f);
                            }
                            if (rotateMode == RotateMode.None)
                                player.transform.localScale = new Vector3(xt, yt, 1f);
                        }
                    }
                    else if (stretch)
                    {
                        float xt = 1f + stretchVector.x;
                        float yt = 1f + stretchVector.y;

                        if (rotateMode == RotateMode.FlipX)
                        {
                            if (lastMovement.x > 0f)
                                player.transform.localScale = new Vector3(xt, yt, 1f);
                            if (lastMovement.x < 0f)
                                player.transform.localScale = new Vector3(-xt, yt, 1f);
                        }
                        if (rotateMode == RotateMode.FlipY)
                        {
                            if (lastMovement.y > 0f)
                                player.transform.localScale = new Vector3(xt, yt, 1f);
                            if (lastMovement.y < 0f)
                                player.transform.localScale = new Vector3(xt, -yt, 1f);
                        }
                        if (rotateMode == RotateMode.None)
                        {
                            player.transform.localScale = new Vector3(xt, yt, 1f);
                        }
                    }
                }
                anim.SetFloat("Speed", Mathf.Abs(vector.x + vector.y));

                if (rb.velocity != Vector2.zero)
                    lastVelocity = rb.velocity;
            }
            else if (CanMove)
            {
                rb.velocity = Vector3.zero;
            }

            // Currently unused.
            if (Alive && CustomPlayer.active && CanMove && !CoreHelper.Paused && !CoreHelper.IsUsingInputField && movementMode == MovementMode.Mouse && CoreHelper.InEditorPreview && Application.isFocused && isKeyboard && !EventsConfig.Instance.EditorCamEnabled.Value)
            {
                Vector2 screenCenter = new Vector2(1920 / 2 * (int)CoreHelper.ScreenScale, 1080 / 2 * (int)CoreHelper.ScreenScale);
                Vector2 mousePos = new Vector2(System.Windows.Forms.Cursor.Position.X - screenCenter.x, -(System.Windows.Forms.Cursor.Position.Y - (screenCenter.y * 2)) - screenCenter.y);

                if (lastMousePos != new Vector2(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y))
                {
                    System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)screenCenter.x, (int)screenCenter.y);
                }

                var mousePosition = Input.mousePosition;
                mousePosition = Camera.main.WorldToScreenPoint(mousePosition);

                float num = idleSpeed * 0.00025f;
                if (isBoosting)
                    num = boostSpeed * 0.0001f;

                //player.transform.position += new Vector3(mousePos.x * num, mousePos.y * num, 0f);
                player.transform.localPosition = new Vector3(mousePosition.x, mousePosition.y, 0f);
                lastMousePos = new Vector2(mousePosition.x, mousePosition.y);
            }

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
                    case RotateMode.RotateToDirection:
                        {
                            player.transform.rotation = c;

                            face.parent.localRotation = Quaternion.identity;
                            
                            break;
                        }
                    case RotateMode.None:
                        {
                            player.transform.rotation = Quaternion.identity;

                            face.parent.rotation = c;

                            break;
                        }
                    case RotateMode.FlipX:
                        {
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
                    case RotateMode.FlipY:
                        {
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
                    case RotateMode.RotateReset:
                        {
                            if (!moved)
                            {
                                animatingRotateReset = false;

                                if (rotateResetAnimation != null)
                                {
                                    AnimationManager.inst.Remove(rotateResetAnimation.id);
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
                                    AnimationManager.inst.Remove(rotateResetAnimation.id);
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

                                    AnimationManager.inst.Remove(rotateResetAnimation.id);
                                    rotateResetAnimation = null;
                                    animatingRotateReset = false;
                                };

                                AnimationManager.inst.Play(rotateResetAnimation);
                            }

                            break;
                        }
                    case RotateMode.RotateFlipX:
                        {
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
                    case RotateMode.RotateFlipY:
                        {
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

        bool animatingRotateReset;
        bool moved;
        float timeNotMovingOffset;
        Vector2 lastMovementTotal;
        RTAnimation rotateResetAnimation;

        void UpdateTheme()
        {
            if (!PlayerModel)
                return;

            var index = PlayersData.Main.GetMaxIndex(playerIndex);

            if (head.gameObject)
            {
                if (head.renderer)
                    head.renderer.material.color = CoreHelper.GetPlayerColor(index, PlayerModel.headPart.color, PlayerModel.headPart.opacity, PlayerModel.headPart.customColor);

                try
                {
                    int colStart = PlayerModel.headPart.color;
                    var colStartHex = PlayerModel.headPart.customColor;
                    float alphaStart = PlayerModel.headPart.opacity;

                    var main1 = burst.main;
                    var main2 = death.main;
                    var main3 = spawn.main;
                    main1.startColor = new ParticleSystem.MinMaxGradient(CoreHelper.GetPlayerColor(index, colStart, alphaStart, colStartHex));
                    main2.startColor = new ParticleSystem.MinMaxGradient(CoreHelper.GetPlayerColor(index, colStart, alphaStart, colStartHex));
                    main3.startColor = new ParticleSystem.MinMaxGradient(CoreHelper.GetPlayerColor(index, colStart, alphaStart, colStartHex));
                }
                catch
                {

                }
            }

            if (boost.renderer)
                boost.renderer.material.color = CoreHelper.GetPlayerColor(index, PlayerModel.boostPart.color, PlayerModel.boostPart.opacity, PlayerModel.boostPart.customColor);

            if (boostTail.renderer)
                boostTail.renderer.material.color = CoreHelper.GetPlayerColor(index, PlayerModel.boostTailPart.color, PlayerModel.boostTailPart.opacity, PlayerModel.boostTailPart.customColor);

            //GUI Bar
            {
                int baseCol = PlayerModel.guiPart.baseColor;
                int topCol = PlayerModel.guiPart.topColor;
                string baseColHex = PlayerModel.guiPart.baseCustomColor;
                string topColHex = PlayerModel.guiPart.topCustomColor;
                float baseAlpha = PlayerModel.guiPart.baseOpacity;
                float topAlpha = PlayerModel.guiPart.topOpacity;

                for (int i = 0; i < healthObjects.Count; i++)
                    if (healthObjects[i].image)
                        healthObjects[i].image.color = CoreHelper.GetPlayerColor(index, topCol, topAlpha, topColHex);

                barBaseIm.color = CoreHelper.GetPlayerColor(index, baseCol, baseAlpha, baseColHex);
                barIm.color = CoreHelper.GetPlayerColor(index, topCol, topAlpha, topColHex);
            }

            for (int i = 0; i < PlayerModel.tailParts.Count; i++)
            {
                if (i >= tailParts.Count)
                    continue;
                var modelPart = PlayerModel.tailParts[i];

                var tailPart = tailParts[i];

                var main = tailPart.particleSystem.main;

                main.startColor = CoreHelper.GetPlayerColor(index, modelPart.Particles.color, 1f, modelPart.Particles.customColor);

                tailPart.renderer.material.color = CoreHelper.GetPlayerColor(index, modelPart.color, modelPart.opacity, modelPart.customColor);

                tailPart.trailRenderer.startColor = CoreHelper.GetPlayerColor(index, modelPart.Trail.startColor, modelPart.Trail.startOpacity, modelPart.Trail.startCustomColor);
                tailPart.trailRenderer.endColor = CoreHelper.GetPlayerColor(index, modelPart.Trail.endColor, modelPart.Trail.endOpacity, modelPart.Trail.endCustomColor);
            }

            if (PlayerModel.headPart.Trail.emitting && head.trailRenderer)
            {
                head.trailRenderer.startColor = CoreHelper.GetPlayerColor(index, PlayerModel.headPart.Trail.startColor, PlayerModel.headPart.Trail.startOpacity, PlayerModel.headPart.Trail.startCustomColor);
                head.trailRenderer.endColor = CoreHelper.GetPlayerColor(index, PlayerModel.headPart.Trail.endColor, PlayerModel.headPart.Trail.endOpacity, PlayerModel.headPart.Trail.endCustomColor);
            }

            if (PlayerModel.headPart.Particles.emitting && head.particleSystem)
            {
                var colStart = PlayerModel.headPart.Particles.color;
                var colStartHex = PlayerModel.headPart.Particles.customColor;

                var main = head.particleSystem.main;
                main.startColor = CoreHelper.GetPlayerColor(index, colStart, 1f, colStartHex);
            }

            if (PlayerModel.boostPart.Trail.emitting && boost.trailRenderer)
            {
                boost.trailRenderer.startColor = CoreHelper.GetPlayerColor(index, PlayerModel.boostPart.Trail.startColor, PlayerModel.boostPart.Trail.startOpacity, PlayerModel.boostPart.Trail.startCustomColor);
                boost.trailRenderer.endColor = CoreHelper.GetPlayerColor(index, PlayerModel.boostPart.Trail.endColor, PlayerModel.boostPart.Trail.endOpacity, PlayerModel.boostPart.Trail.endCustomColor);
            }

            if (PlayerModel.boostPart.Particles.emitting && boost.particleSystem)
            {
                var main = boost.particleSystem.main;
                main.startColor = CoreHelper.GetPlayerColor(index, PlayerModel.boostPart.Particles.color, 1f, PlayerModel.boostPart.Particles.customColor);
            }
        }

        #endregion

        #region Collision Handlers

        public bool colliding;

        bool CollisionCheck(Collider2D other) => other.tag != "Helper" && other.tag != "Player" && other.name != $"bullet (Player {playerIndex + 1})";
        bool CollisionCheck(Collider other) => other.tag != "Helper" && other.tag != "Player" && other.name != $"bullet (Player {playerIndex + 1})";

        public void OnObjectCollisionEnter(Collider2D other) => HandleCollision(other);

        public void OnObjectCollisionEnter(Collider other) => HandleCollision(other);

        public void OnObjectCollisionStay(Collider2D other) => HandleCollision(other, false);

        public void OnObjectCollisionStay(Collider other) => HandleCollision(other, false);

        void HandleCollision(Collider2D other, bool stay = true)
        {
            if (CanTakeDamage && (!CoreHelper.InEditor || !ZenModeInEditor) && (!stay || !isBoosting) && CollisionCheck(other))
                Hit();
        }
        
        void HandleCollision(Collider other, bool stay = true)
        {
            if (CanTakeDamage && (!CoreHelper.InEditor || !ZenModeInEditor) && (!stay || !isBoosting) && CollisionCheck(other))
                Hit();
        }

        #endregion

        #region Init

        public void Hit()
        {
            if (!CanTakeDamage || !Alive)
                return;

            timeHit = Time.time;

            InitBeforeHit();
            if (Alive)
                anim.SetTrigger("hurt");
            if (CustomPlayer == null)
                return;

            if (!PlayerManager.IsPractice)
                CustomPlayer.Health--;
            playerHitEvent?.Invoke(CustomPlayer.Health, rb.position);
        }

        IEnumerator BoostCooldownLoop()
        {
            var headTrail = boost.trailRenderer;
            if (PlayerModel && PlayerModel.boostPart.Trail.emitting)
                headTrail.emitting = false;

            AnimationManager.inst.Play(new RTAnimation("Player Stretch")
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

            yield return new WaitForSeconds(boostCooldown / CoreHelper.ForwardPitch);
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

        IEnumerator Kill()
        {
            isDead = true;
            playerDeathEvent?.Invoke(rb.position);
            CustomPlayer.active = false;
            CustomPlayer.health = 0;
            anim.SetTrigger("kill");
            InputDataManager.inst.SetControllerRumble(playerIndex, 1f);
            yield return new WaitForSecondsRealtime(0.2f);
            Destroy(healthText);
            Destroy(gameObject);
            InputDataManager.inst.StopControllerRumble(playerIndex);
            yield break;
        }

        public void InitMidSpawn()
        {
            CanMove = true;
            CanBoost = true;
        }

        public void InitAfterSpawn()
        {
            if (boostCoroutine != null)
                StopCoroutine(boostCoroutine);
            CanMove = true;
            CanBoost = true;
            CanTakeDamage = true;
            isSpawning = false;
        }

        public void StartBoost()
        {
            if (!CanBoost || isBoosting)
                return;

            startBoostTime = Time.time;
            InitBeforeBoost();
            anim.SetTrigger("boost");

            var ps = boost.particleSystem;
            var emission = ps.emission;

            var headTrail = boost.trailRenderer;

            if (emission.enabled)
                ps.Play();
            if (PlayerModel && PlayerModel.boostPart.Trail.emitting)
                headTrail.emitting = true;

            if (PlayBoostSound)
                SoundManager.inst.PlaySound(DefaultSounds.boost);

            CreatePulse();

            stretchVector = new Vector2(stretchAmount * 1.5f, -(stretchAmount * 1.5f));

            if (showBoostTail)
            {
                path[1].active = false;
                animatingBoost = true;
                boostTail.parent.DOScale(Vector3.zero, 0.05f / CoreHelper.ForwardPitch).SetEase(DataManager.inst.AnimationList[2].Animation);
            }

            LevelManager.BoostCount++;
        }

        public void InitBeforeBoost()
        {
            CanBoost = false;
            isBoosting = true;
            CanTakeDamage = false;
        }

        public void InitMidBoost(bool _forceToNormal = false)
        {
            if (_forceToNormal)
            {
                float num = Time.time - startBoostTime;
                StartCoroutine(BoostCancel((num < minBoostTime) ? (minBoostTime - num) : 0f));
                return;
            }
            isBoosting = false;
            CanTakeDamage = true;
        }

        public IEnumerator BoostCancel(float _offset)
        {
            isBoostCancelled = true;
            yield return new WaitForSeconds(_offset);
            isBoosting = false;
            if (!isTakingHit)
            {
                CanTakeDamage = true;
                anim.SetTrigger("boost_cancel");
            }
            else
            {
                float num = (Time.time - startHurtTime) / 2.5f;
                if (num < 1f)
                    anim.Play("Hurt", -1, num);
                else
                {
                    anim.SetTrigger("boost_cancel");
                    InitAfterHit();
                }
            }
            yield return new WaitForSeconds(0.1f);
            InitAfterBoost();
            anim.SetTrigger("boost_cancel");
            yield break;
        }

        //Look into making custom damage offsets
        public IEnumerator DamageSetDelay(float _offset)
        {
            yield return new WaitForSeconds(_offset);
            CoreHelper.Log($"Player {playerIndex} can now be damaged.");
            CanTakeDamage = true;
            yield break;
        }

        public void InitAfterBoost()
        {
            isBoosting = false;
            isBoostCancelled = false;
            boostCoroutine = StartCoroutine(BoostCooldownLoop());
        }

        public void InitBeforeHit()
        {
            CoreHelper.Log($"Player {playerIndex} InitBeforeHit");
            startHurtTime = Time.time;
            CanBoost = true;
            isBoosting = false;
            isTakingHit = true;
            CanTakeDamage = false;

            SoundManager.inst.PlaySound(CoreConfig.Instance.Language.Value == Language.Pirate ? DefaultSounds.pirate_KillPlayer : DefaultSounds.HurtPlayer);
        }

        // Empty method for animation controller (todo: see if animation controller can live without this or am I misunderstanding how this works?)
        public void InitAfterHit() { }

        public void ResetMovement()
        {
            if (boostCoroutine != null)
                StopCoroutine(boostCoroutine);

            isBoosting = false;
            CanMove = true;
            CanBoost = true;
        }

        public void PlaySpawnParticles() => spawn.Play();

        public void PlayDeathParticles() => death.Play();

        public void PlayHitParticles() => burst.Play();

        #endregion

        #region Update Values

        public bool updated;

        void Assign(PlayerObject playerObject, PlayerModel.Generic generic, bool changeActive = true)
        {
            if (changeActive)
                playerObject.renderer.enabled = generic.active;
            if (changeActive && !generic.active)
                return;

            playerObject.meshFilter.mesh = GetShape(generic.shape.type, generic.shape.option).mesh;
            playerObject.gameObject.transform.localPosition = new Vector3(generic.position.x, generic.position.y);
            playerObject.gameObject.transform.localScale = new Vector3(generic.scale.x, generic.scale.y, 1f);
            playerObject.gameObject.transform.localEulerAngles = new Vector3(0f, 0f, generic.rotation);
            
            var trailRenderer = playerObject.trailRenderer;
            if (trailRenderer)
            {
                trailRenderer.enabled = generic.Trail.emitting;
                trailRenderer.emitting = generic.Trail.emitting;
                trailRenderer.startWidth = generic.Trail.startWidth;
                trailRenderer.endWidth = generic.Trail.endWidth;
            }

            var particleSystem = playerObject.particleSystem;

            if (playerObject.particleSystemRenderer)
                playerObject.particleSystemRenderer.mesh = GetShape(generic.Particles.shape.type, generic.Particles.shape.option).mesh;

            if (!particleSystem)
                return;

            var main = particleSystem.main;
            var emission = particleSystem.emission;

            main.startLifetime = generic.Particles.lifeTime;
            main.startSpeed = generic.Particles.speed;

            emission.enabled = generic.Particles.emitting;
            particleSystem.emissionRate = generic.Particles.amount;

            var rotationOverLifetime = particleSystem.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.separateAxes = true;
            rotationOverLifetime.xMultiplier = 0f;
            rotationOverLifetime.yMultiplier = 0f;
            rotationOverLifetime.zMultiplier = generic.Particles.rotation;

            var forceOverLifetime = particleSystem.forceOverLifetime;
            forceOverLifetime.enabled = true;
            forceOverLifetime.space = ParticleSystemSimulationSpace.World;
            forceOverLifetime.xMultiplier = generic.Particles.force.x;
            forceOverLifetime.yMultiplier = generic.Particles.force.y;

            var particlesTrail = particleSystem.trails;
            particlesTrail.enabled = generic.Particles.trailEmitting;

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var psCol = colorOverLifetime.color;

            float alphaStart = generic.Particles.startOpacity;
            float alphaEnd = generic.Particles.endOpacity;

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

            var sizeStart = generic.Particles.startScale;
            var sizeEnd = generic.Particles.endScale;

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

        public void UpdateModel()
        {
            if (!PlayerModel)
                return;

            var currentModel = PlayerModel;

            //New NameTag
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

                nametagText = tae.GetComponentInChildren<TextMeshPro>();

                var d = canvas.AddComponent<PlayerDelayTracker>();
                d.leader = rb.transform;
                d.scaleParent = false;
                d.rotationParent = false;
                d.player = this;
                d.positionOffset = 0.9f;
            }

            for (int i = 0; i < currentModel.tailParts.Count; i++)
            {
                if (i >= tailParts.Count)
                    continue;

                Assign(tailParts[i], currentModel.tailParts[i]);
            }

            Assign(head, currentModel.headPart);
            Assign(boost, currentModel.boostPart);

            tailDistance = currentModel.tailBase.distance;
            tailMode = (int)currentModel.tailBase.mode;

            tailGrows = currentModel.tailBase.grows;

            showBoostTail = currentModel.boostTailPart.active;

            boostTail.parent.gameObject.SetActive(showBoostTail);
            if (showBoostTail)
                boostTail.meshFilter.mesh = GetShape(currentModel.boostTailPart.shape.type, currentModel.boostTailPart.shape.option).mesh;

            var fp = currentModel.FacePosition;
            face.gameObject.transform.localPosition = new Vector3(fp.x, fp.y, 0f);

            if (!isBoosting)
                path[1].active = showBoostTail;

            jumpGravity = currentModel.basePart.jumpGravity;
            jumpIntensity = currentModel.basePart.jumpIntensity;
            jumpCount = currentModel.basePart.jumpCount;
            jumpBoostCount = currentModel.basePart.jumpBoostCount;
            bounciness = currentModel.basePart.bounciness;

            rb.sharedMaterial.bounciness = bounciness;

            stretch = currentModel.stretchPart.active;
            stretchAmount = currentModel.stretchPart.amount;
            stretchEasing = currentModel.stretchPart.easing;

            var bt1 = currentModel.boostTailPart.position;
            var bt2 = currentModel.boostTailPart.scale;
            var bt3 = currentModel.boostTailPart.rotation;

            boostTail.gameObject.SetActive(showBoostTail);

            if (showBoostTail)
            {
                boostTail.gameObject.transform.localPosition = new Vector3(bt1.x, bt1.y, 0.1f);
                boostTail.gameObject.transform.localScale = new Vector3(bt2.x, bt2.y, 1f);
                boostTail.gameObject.transform.localEulerAngles = new Vector3(0f, 0f, bt3);
            }

            rotateMode = (RotateMode)(int)currentModel.basePart.rotateMode;

            circleCollider2D.isTrigger = PlayerManager.Invincible && ZenEditorIncludesSolid;
            polygonCollider2D.isTrigger = PlayerManager.Invincible && ZenEditorIncludesSolid;

            var colAcc = currentModel.basePart.collisionAccurate;
            circleCollider2D.enabled = !colAcc;
            polygonCollider2D.enabled = colAcc;
            if (colAcc)
                polygonCollider2D.CreateCollider(head.meshFilter);

            if (CustomPlayer)
                CustomPlayer.Health = PlayerManager.IsNoHit ? 1 : currentModel.basePart.health;

            //Health Images
            foreach (var health in healthObjects)
            {
                if (health.image)
                    health.image.sprite = RTFile.FileExists(RTFile.CombinePaths(RTFile.BasePath, $"health{FileFormat.PNG.Dot()}")) && !AssetsGlobal ? SpriteHelper.LoadSprite(RTFile.CombinePaths(RTFile.BasePath, $"health{FileFormat.PNG.Dot()}")) :
                        RTFile.FileExists(RTFile.GetAsset($"health{FileFormat.PNG.Dot()}")) ? SpriteHelper.LoadSprite(RTFile.GetAsset($"health{FileFormat.PNG.Dot()}")) :
                        PlayerManager.healthSprite;
            }

            UpdateCustomObjects();

            updated = true;
        }
        
        public void UpdateCustomObjects()
        {
            var currentModel = PlayerModel;

            var currentModelCustomObjects = currentModel.customObjects;

            foreach (var obj in customObjects)
                Destroy(obj.gameObject);
            customObjects.Clear();

            playerObjects.RemoveAll(x => x.isCustom);

            if (currentModelCustomObjects == null || currentModelCustomObjects.Count < 1)
                return;

            for (int i = 0; i < currentModelCustomObjects.Count; i++)
            {
                var reference = currentModelCustomObjects[i];

                if (reference.shape.type == 9)
                    continue;

                var customObj = new CustomObject()
                {
                    id = reference.id,
                    reference = reference,
                };

                var shape = reference.shape;
                var pos = reference.position;
                var sca = reference.scale;
                var rot = reference.rotation;

                var depth = reference.depth;

                int s = Mathf.Clamp(shape.type, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                int so = Mathf.Clamp(shape.option, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                customObj.gameObject = ObjectManager.inst.objectPrefabs[s].options[so].Duplicate(customObjectParent);
                customObj.gameObject.transform.localScale = Vector3.one;
                customObj.gameObject.transform.localRotation = Quaternion.identity;

                customObj.delayTracker = customObj.gameObject.AddComponent<PlayerDelayTracker>();
                customObj.delayTracker.offset = 0;
                customObj.delayTracker.positionOffset = reference.positionOffset;
                customObj.delayTracker.scaleOffset = reference.scaleOffset;
                customObj.delayTracker.rotationOffset = reference.rotationOffset;
                customObj.delayTracker.scaleParent = reference.scaleParent;
                customObj.delayTracker.rotationParent = reference.rotationParent;
                customObj.delayTracker.player = this;

                var child = customObj.gameObject.transform.GetChild(0);
                child.localPosition = new Vector3(pos.x, pos.y, depth);
                child.localScale = new Vector3(sca.x, sca.y, 1f);
                child.localEulerAngles = new Vector3(0f, 0f, rot);

                customObj.gameObject.tag = "Helper";
                child.tag = "Helper";

                var renderer = customObj.gameObject.GetComponentInChildren<Renderer>();
                renderer.enabled = true;
                customObj.renderer = renderer;

                Destroy(child.GetComponent<Collider2D>());

                if (s == 4 && child.gameObject.TryGetComponent(out TextMeshPro tmp))
                {
                    tmp.text = customObj.reference.text;
                    customObj.text = tmp;
                }

                if (s == 6 && renderer is SpriteRenderer spriteRenderer)
                {
                    var path = RTFile.CombinePaths(RTFile.BasePath, reference.text);

                    if (!RTFile.FileExists(path))
                    {
                        spriteRenderer.sprite = ArcadeManager.inst.defaultImage;
                        continue;
                    }

                    CoreHelper.StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{path}", texture2D =>
                    {
                        if (!spriteRenderer)
                            return;

                        spriteRenderer.sprite = SpriteHelper.CreateSprite(texture2D);
                    }));
                }

                playerObjects.Add(customObj);
                customObjects.Add(customObj);
            }

            UpdateParents();
        }

        public void UpdateParents()
        {
            foreach (var customObject in customObjects)
                UpdateParent(customObject);
        }

        void UpdateParent(CustomObject customObject)
        {
            var reference = customObject.reference;
            var delayTracker = customObject.delayTracker;

            delayTracker.positionOffset = reference.positionOffset;
            delayTracker.scaleOffset = reference.scaleOffset;
            delayTracker.rotationOffset = reference.rotationOffset;
            delayTracker.scaleParent = reference.scaleParent;
            delayTracker.rotationParent = reference.rotationParent;
            delayTracker.leader = !string.IsNullOrEmpty(reference.customParent) && playerObjects.TryFind(x => x.id == reference.customParent && x.id != customObject.id, out PlayerObject parent) ?
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

        void UpdateCustomTheme()
        {
            if (customObjects.Count < 1)
                return;

            var index = PlayersData.Main.GetMaxIndex(playerIndex);
            foreach (var playerObject in playerObjects)
            {
                if (!playerObject.isCustom)
                {
                    playerObject.gameObject.SetActive(playerObject.active);
                    continue;
                }

                var customObject = playerObject as CustomObject;

                if (!CustomPlayer || !customObject.gameObject)
                    continue;
                var active = customObject.active && (customObject.reference.visibilitySettings.Count < 1 && customObject.reference.active || customObject.reference.visibilitySettings.Count > 0 &&
                        (!customObject.reference.requireAll && customObject.reference.visibilitySettings.Any(x => CheckVisibility(x)) ||
                    customObject.reference.visibilitySettings.All(x => CheckVisibility(x))));

                customObject.gameObject.SetActive(active);

                if (!active)
                    continue;

                var reference = customObject.reference;
                if (customObject.text)
                    customObject.text.color = CoreHelper.GetPlayerColor(index, reference.color, reference.opacity, reference.customColor);
                else if (customObject.renderer)
                    customObject.renderer.material.color = CoreHelper.GetPlayerColor(index, reference.color, reference.opacity, reference.customColor);
            }
        }

        public void UpdateVisibility(CustomObject customGameObject)
        {
            if (CustomPlayer && customGameObject.gameObject)
                customGameObject.gameObject.SetActive(customGameObject.active && (customGameObject.reference.visibilitySettings.Count < 1 && customGameObject.reference.active || customGameObject.reference.visibilitySettings.Count > 0 &&
                    (!customGameObject.reference.requireAll && customGameObject.reference.visibilitySettings.Any(x => CheckVisibility(x)) ||
                customGameObject.reference.visibilitySettings.All(x => CheckVisibility(x)))));
        }

        public bool CheckVisibility(PlayerModel.CustomObject.Visiblity visiblity)
        {
            var value = visiblity.command switch
            {
                "isBoosting" => isBoosting,
                "isTakingHit" => isTakingHit,
                "isZenMode" => PlayerManager.Invincible,
                "isHealthPercentageGreater" => (float)CustomPlayer.health / initialHealthCount * 100f >= visiblity.value,
                "isHealthGreaterEquals" => CustomPlayer.health >= visiblity.value,
                "isHealthEquals" => CustomPlayer.health == visiblity.value,
                "isHealthGreater" => CustomPlayer.health > visiblity.value,
                "isPressingKey" => Input.GetKey(GetKeyCode((int)visiblity.value)),
                _ => true,
            };

            return visiblity.not ? !value : value;
        }

        /// <summary>
        /// 
        /// </summary>
        public void GrowTail()
        {
            int num = tailParts.Count + 1;
            var tailBase = tailParts.Last().parent.gameObject.Duplicate(tailParent, $"Tail {num} Base");
            tailBase.transform.localScale = Vector3.one;
            var tail = tailBase.transform.GetChild(0);
            tail.name = num.ToString();

            path.Insert(path.Count - 2, new MovementPath(tailBase.transform.localPosition, tailBase.transform.localRotation, tailBase.transform));

            var playerDelayTracker = tailBase.AddComponent<PlayerDelayTracker>();
            playerDelayTracker.offset = -num * tailDistance / 2f;
            playerDelayTracker.positionOffset *= (-num + 4);
            playerDelayTracker.player = this;
            playerDelayTracker.leader = tailTracker.transform;

            var tailParticles = tailBase.transform.Find("tail-particles");

            var tailPart = new PlayerObject
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
            tail.transform.localPosition = new Vector3(0f, 0f, 0.1f);

            Assign(tailPart, PlayerModel.tailParts.Last());
        }

        public void UpdateTail(int health, Vector3 _pos)
        {
            if (health > initialHealthCount)
            {
                initialHealthCount = health;

                if (tailGrows)
                    GrowTail();
            }

            for (int i = 2; i < path.Count; i++)
            {
                if (!path[i].transform)
                    continue;

                var inactive = i - 1 > health;

                if (path[i].transform.childCount != 0)
                    path[i].transform.GetChild(0).gameObject.SetActive(!inactive);
                else
                    path[i].transform.gameObject.SetActive(!inactive);
            }

            var currentModel = PlayerModel;

            if (!currentModel)
                return;

            if (healthObjects.Count > 0)
                for (int i = 0; i < healthObjects.Count; i++)
                    healthObjects[i].gameObject.SetActive(i < health && currentModel.guiPart.active && currentModel.guiPart.mode == PlayerModel.GUI.GUIHealthMode.Images);

            var text = healthText.GetComponent<Text>();
            if (currentModel.guiPart.active && (currentModel.guiPart.mode == PlayerModel.GUI.GUIHealthMode.Text || currentModel.guiPart.mode == PlayerModel.GUI.GUIHealthMode.EqualsBar))
            {
                text.enabled = true;
                if (currentModel.guiPart.mode == PlayerModel.GUI.GUIHealthMode.Text)
                    text.text = health.ToString();
                else
                    text.text = RTString.ConvertHealthToEquals(health, initialHealthCount);
            }
            else
                text.enabled = false;

            if (currentModel.guiPart.active && currentModel.guiPart.mode == PlayerModel.GUI.GUIHealthMode.Bar)
            {
                barBaseIm.gameObject.SetActive(true);
                var e = (float)health / (float)initialHealthCount;
                barIm.rectTransform.sizeDelta = new Vector2(200f * e, 32f);
            }
            else
                barBaseIm.gameObject.SetActive(false);
        }

        #endregion

        #region Actions

        void CreatePulse()
        {
            if (!PlayerModel)
                return;

            var currentModel = PlayerModel;

            if (!currentModel.pulsePart.active)
                return;

            var player = rb.gameObject;

            int s = Mathf.Clamp(currentModel.pulsePart.shape.type, 0, ObjectManager.inst.objectPrefabs.Count - 1);
            int so = Mathf.Clamp(currentModel.pulsePart.shape.option, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

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
            var pulseObject = new PulseObject
            {
                renderer = pulseRenderer,
                startColor = currentModel.pulsePart.startColor,
                endColor = currentModel.pulsePart.endColor,
                startCustomColor = currentModel.pulsePart.startCustomColor,
                endCustomColor = currentModel.pulsePart.endCustomColor,
            };

            boosts.Add(pulseObject);

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
                boosts.Remove(pulseObject);
            });
        }

        void UpdateBoostTheme()
        {
            if (boosts.Count < 1)
                return;

            var index = PlayersData.Main.GetMaxIndex(playerIndex);

            foreach (var boost in boosts)
            {
                if (boost == null)
                    continue;

                int startCol = boost.startColor;
                int endCol = boost.endColor;

                var startHex = boost.startCustomColor;
                var endHex = boost.endCustomColor;

                float alpha = boost.opacity;
                float colorTween = boost.colorTween;

                Color startColor = CoreHelper.GetPlayerColor(index, startCol, alpha, startHex);
                Color endColor = CoreHelper.GetPlayerColor(index, endCol, alpha, endHex);

                if (boost.renderer)
                    boost.renderer.material.color = Color.Lerp(startColor, endColor, colorTween);
            }
        }

        public List<PulseObject> boosts = new List<PulseObject>();

        // to do: aiming so you don't need to be facing the direction of the bullet
        void CreateBullet()
        {
            var currentModel = PlayerModel;

            if (currentModel == null || !currentModel.bulletPart.active)
                return;

            if (PlayShootSound)
                SoundManager.inst.PlaySound(gameObject, DefaultSounds.shoot, pitch: CoreHelper.ForwardPitch);

            canShoot = false;

            var player = rb.gameObject;

            int s = Mathf.Clamp(currentModel.bulletPart.shape.type, 0, ObjectManager.inst.objectPrefabs.Count - 1);
            int so = Mathf.Clamp(currentModel.bulletPart.shape.option, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

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
                pulse.tag = "Helper";
                pulse.transform.GetChild(0).tag = "Helper";
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
            var pulseObject = new PulseObject
            {
                renderer = pulseRenderer,
                startColor = currentModel.bulletPart.startColor,
                endColor = currentModel.bulletPart.endColor,
                startCustomColor = currentModel.bulletPart.startCustomColor,
                endCustomColor = currentModel.bulletPart.endCustomColor,
            };

            boosts.Add(pulseObject);

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
            bulletCollider.playerObject = pulseObject;

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
                    boosts.Remove(pulseObject);
                    pulseObject = null;
                });
            });
        }

        IEnumerator CanShoot()
        {
            var currentModel = PlayerModel;
            if (currentModel)
            {
                var delay = currentModel.bulletPart.delay;
                yield return new WaitForSeconds(delay);
            }
            canShoot = true;

            yield break;
        }

        #endregion

        public KeyCode GetKeyCode(int key)
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

                if (IndexToInt(CustomPlayer.playerIndex) > 0)
                {
                    string str = (IndexToInt(CustomPlayer.playerIndex) * 2).ToString() + "0";
                    num += int.Parse(str);
                }

                return (KeyCode)num;
            }

            return KeyCode.None;
        }

        public int IndexToInt(PlayerIndex playerIndex) => (int)playerIndex;

        #region Objects

        public List<CustomObject> customObjects = new List<CustomObject>();

        public PlayerObject basePart;
        public PlayerObject head;
        public PlayerObject face;
        public PlayerObject boost;
        public PlayerObject boostTail;

        public List<PlayerObject> tailParts = new List<PlayerObject>();

        public List<PlayerObject> playerObjects = new List<PlayerObject>();

        public List<MovementPath> path = new List<MovementPath>();

        public List<HealthObject> healthObjects = new List<HealthObject>();

        public class CustomObject : PlayerObject
        {
            public CustomObject() => isCustom = true;

            public PlayerModel.CustomObject reference;
            public TextMeshPro text;
        }

        public class PulseObject : PlayerObject
        {
            public float opacity;
            public float colorTween;
            public int startColor;
            public int endColor;
            public string startCustomColor;
            public string endCustomColor;
        }

        public class PlayerObject : Exists
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

        public class MovementPath
        {
            public MovementPath(Vector3 pos, Quaternion rot, Transform transform)
            {
                this.pos = pos;
                this.rot = rot;
                this.transform = transform;
            }

            public MovementPath(Vector3 pos, Quaternion rot, Transform transform, bool active)
            {
                this.pos = pos;
                this.rot = rot;
                this.transform = transform;
                this.active = active;
            }

            public bool active = true;

            public Vector3 lastPos;
            public Vector3 pos;

            public Quaternion rot;

            public Transform transform;
        }

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
