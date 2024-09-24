using BetterLegacy.Components.Editor;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
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

namespace BetterLegacy.Components.Player
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
        /// 
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
                var levelData = GameData.Current.beatmapData.ModLevelData;
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
        /// The current gamemode the player is in.
        /// </summary>
        public static GameMode GameMode { get; set; }

        /// <summary>
        /// Base player actions.
        /// </summary>
        public MyGameActions Actions { get; set; }

        /// <summary>
        /// Secondary player actions.
        /// </summary>
        public FaceController faceController;

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

        public TextMeshPro textMesh;
        public MeshRenderer healthBase;

        public GameObject health;

        private RectTransform barRT;
        private Image barIm;
        private Image barBaseIm;

        public ParticleSystem burst;
        public ParticleSystem death;
        public ParticleSystem spawn;

        /// <summary>
        /// Custom player data reference.
        /// </summary>
        public CustomPlayer CustomPlayer { get; set; }
        
        /// <summary>
        /// Player model reference.
        /// </summary>
        public PlayerModel PlayerModel { get; set; }

        public Animator anim;
        public Rigidbody2D rb;

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

        #region Jumping

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
        int currentJumpCount = 0;
        int currentJumpBoostCount = 0;
        public float jumpBoostMultiplier = 0.5f; // to make sure the jump goes about the same distance as left and right boost

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

        public bool PlayerAlive => CustomPlayer && CustomPlayer.Health > 0 && !isDead;

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
            playerObjects.Add("Base", new PlayerObject("Base", gameObject));
            playerObjects["Base"].values.Add("Transform", gameObject.transform);
            var anim = gameObject.GetComponent<Animator>();
            anim.keepAnimatorControllerStateOnDisable = true;
            playerObjects["Base"].values.Add("Animator", anim);
            this.anim = anim;

            var rb = transform.Find("Player").gameObject;
            playerObjects.Add("RB Parent", new PlayerObject("RB Parent", rb));
            playerObjects["RB Parent"].values.Add("Transform", rb.transform);
            playerObjects["RB Parent"].values.Add("Rigidbody2D", rb.GetComponent<Rigidbody2D>());
            this.rb = rb.GetComponent<Rigidbody2D>();

            var circleCollider = rb.GetComponent<CircleCollider2D>();

            circleCollider.enabled = false;

            var polygonCollider = rb.AddComponent<PolygonCollider2D>();

            playerObjects["RB Parent"].values.Add("CircleCollider2D", circleCollider);
            playerObjects["RB Parent"].values.Add("PolygonCollider2D", polygonCollider);
            playerObjects["RB Parent"].values.Add("PlayerSelector", rb.AddComponent<PlayerSelector>());
            ((PlayerSelector)playerObjects["RB Parent"].values["PlayerSelector"]).id = playerIndex;

            var head = transform.Find("Player/Player").gameObject;
            playerObjects.Add("Head", new PlayerObject("Head", head));

            var headMesh = head.GetComponent<MeshFilter>();

            playerObjects["Head"].values.Add("MeshFilter", headMesh);

            polygonCollider.CreateCollider(headMesh);

            playerObjects["Head"].values.Add("MeshRenderer", head.GetComponent<MeshRenderer>());

            polygonCollider.isTrigger = CoreHelper.InEditor && ZenEditorIncludesSolid;
            polygonCollider.enabled = false;
            circleCollider.enabled = true;

            circleCollider.isTrigger = CoreHelper.InEditor && ZenEditorIncludesSolid;
            rb.GetComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            DestroyImmediate(rb.GetComponent<OnTriggerEnterPass>());

            var playerCollision = rb.AddComponent<PlayerCollision>();
            playerCollision.player = this;

            var boost = transform.Find("Player/boost").gameObject;
            boost.transform.localScale = Vector3.zero;
            playerObjects.Add("Boost", new PlayerObject("Boost", transform.Find("Player/boost").gameObject));
            playerObjects["Boost"].values.Add("MeshFilter", boost.GetComponent<MeshFilter>());
            playerObjects["Boost"].values.Add("MeshRenderer", boost.GetComponent<MeshRenderer>());

            playerObjects.Add("Tail Parent", new PlayerObject("Tail Parent", transform.Find("trail").gameObject));
            var tail1 = transform.Find("trail/1").gameObject;
            playerObjects.Add("Tail 1", new PlayerObject("Tail 1", tail1));
            var tail2 = transform.Find("trail/2").gameObject;
            playerObjects.Add("Tail 2", new PlayerObject("Tail 2", tail2));
            var tail3 = transform.Find("trail/3").gameObject;
            playerObjects.Add("Tail 3", new PlayerObject("Tail 3", tail3));

            playerObjects["Tail 1"].values.Add("MeshFilter", tail1.GetComponent<MeshFilter>());
            playerObjects["Tail 2"].values.Add("MeshFilter", tail2.GetComponent<MeshFilter>());
            playerObjects["Tail 3"].values.Add("MeshFilter", tail3.GetComponent<MeshFilter>());
            playerObjects["Tail 1"].values.Add("MeshRenderer", tail1.GetComponent<MeshRenderer>());
            playerObjects["Tail 2"].values.Add("MeshRenderer", tail2.GetComponent<MeshRenderer>());
            playerObjects["Tail 3"].values.Add("MeshRenderer", tail3.GetComponent<MeshRenderer>());
            playerObjects["Tail 1"].values.Add("TrailRenderer", tail1.GetComponent<TrailRenderer>());
            playerObjects["Tail 2"].values.Add("TrailRenderer", tail2.GetComponent<TrailRenderer>());
            playerObjects["Tail 3"].values.Add("TrailRenderer", tail3.GetComponent<TrailRenderer>());

            tail1.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            tail2.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            tail3.transform.localPosition = new Vector3(0f, 0f, 0.1f);

            // Set new parents
            {
                path.Add(new MovementPath(Vector3.zero, Quaternion.identity, rb.transform));

                // Boost
                var boostBase = Creator.NewGameObject("Boost Base", transform.Find("Player"));
                boostBase.layer = 8;
                boost.transform.SetParent(boostBase.transform);
                boost.transform.localPosition = Vector3.zero;
                boost.transform.localRotation = Quaternion.identity;

                playerObjects.Add("Boost Base", new PlayerObject("Boost Base", boostBase));

                var boostTail = boostBase.Duplicate(transform.Find("trail"), "Boost Tail");
                boostTail.layer = 8;

                playerObjects.Add("Boost Tail Base", new PlayerObject("Boost Tail Base", boostTail));
                var child = boostTail.transform.GetChild(0);

                bool showBoost = this.showBoostTail;

                playerObjects.Add("Boost Tail", new PlayerObject("Boost Tail", child.gameObject));
                playerObjects["Boost Tail"].values.Add("MeshRenderer", child.GetComponent<MeshRenderer>());
                playerObjects["Boost Tail"].values.Add("MeshFilter", child.GetComponent<MeshFilter>());

                path.Add(new MovementPath(Vector3.zero, Quaternion.identity, boostTail.transform, showBoost));

                for (int i = 1; i < 4; i++)
                {
                    var name = $"Tail {i} Base";
                    var tailBase = Creator.NewGameObject(name, transform.Find("trail"));
                    tailBase.layer = 8;
                    transform.Find($"trail/{i}").SetParent(tailBase.transform);
                    path.Add(new MovementPath(Vector3.zero, Quaternion.identity, tailBase.transform));

                    playerObjects.Add(name, new PlayerObject(name, tailBase));
                }

                path.Add(new MovementPath(Vector3.zero, Quaternion.identity, null));
            }

            // Add new stuff
            {
                var delayTarget = new GameObject("tail-tracker");
                delayTarget.transform.SetParent(rb.transform);
                delayTarget.transform.localPosition = new Vector3(-0.5f, 0f, 0.1f);
                delayTarget.transform.localRotation = Quaternion.identity;
                playerObjects.Add("Tail Tracker", new PlayerObject("Tail Tracker", delayTarget));

                var faceBase = new GameObject("face-base");
                faceBase.transform.SetParent(rb.transform);
                faceBase.transform.localPosition = Vector3.zero;
                faceBase.transform.localScale = Vector3.one;

                playerObjects.Add("Face Base", new PlayerObject("Face Base", faceBase));

                var faceParent = new GameObject("face-parent");
                faceParent.transform.SetParent(faceBase.transform);
                faceParent.transform.localPosition = Vector3.zero;
                faceParent.transform.localScale = Vector3.one;
                faceParent.transform.localRotation = Quaternion.identity;
                playerObjects.Add("Face Parent", new PlayerObject("Face Parent", faceParent));

                // PlayerDelayTracker
                var boostDelay = playerObjects["Boost Tail Base"].gameObject.AddComponent<PlayerDelayTracker>();
                boostDelay.leader = delayTarget.transform;
                boostDelay.player = this;
                playerObjects["Boost Tail Base"].values.Add("PlayerDelayTracker", boostDelay);

                for (int i = 1; i < 4; i++)
                {
                    var tail = playerObjects[string.Format("Tail {0} Base", i)].gameObject;
                    var PlayerDelayTracker = tail.AddComponent<PlayerDelayTracker>();
                    PlayerDelayTracker.offset = -i * tailDistance / 2f;
                    PlayerDelayTracker.positionOffset *= (-i + 4);
                    PlayerDelayTracker.player = this;
                    PlayerDelayTracker.leader = delayTarget.transform;
                    playerObjects[string.Format("Tail {0} Base", i)].values.Add("PlayerDelayTracker", PlayerDelayTracker);
                }

                var mat = head.transform.Find("death-explosion").GetComponent<ParticleSystemRenderer>().trailMaterial;

                //Trail
                {
                    var superTrail = new GameObject("super-trail");
                    superTrail.transform.SetParent(head.transform);
                    superTrail.transform.localPosition = Vector3.zero;
                    superTrail.transform.localScale = Vector3.one;
                    superTrail.layer = 8;

                    var trailRenderer = superTrail.AddComponent<TrailRenderer>();

                    playerObjects.Add("Head Trail", new PlayerObject("Head Trail", superTrail));
                    playerObjects["Head Trail"].values.Add("TrailRenderer", trailRenderer);

                    trailRenderer.material = mat;
                }

                //Particles
                {
                    var superParticles = new GameObject("super-particles");
                    superParticles.transform.SetParent(head.transform);
                    superParticles.transform.localPosition = Vector3.zero;
                    superParticles.transform.localScale = Vector3.one;
                    superParticles.layer = 8;

                    var particleSystem = superParticles.AddComponent<ParticleSystem>();
                    if (!superParticles.GetComponent<ParticleSystemRenderer>())
                    {
                        superParticles.AddComponent<ParticleSystemRenderer>();
                    }

                    var particleSystemRenderer = superParticles.GetComponent<ParticleSystemRenderer>();

                    playerObjects.Add("Head Particles", new PlayerObject("Head Particles", superParticles));
                    playerObjects["Head Particles"].values.Add("ParticleSystem", particleSystem);
                    playerObjects["Head Particles"].values.Add("ParticleSystemRenderer", particleSystemRenderer);

                    var main = particleSystem.main;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    main.playOnAwake = false;
                    particleSystemRenderer.renderMode = ParticleSystemRenderMode.Mesh;
                    particleSystemRenderer.alignment = ParticleSystemRenderSpace.View;

                    particleSystemRenderer.trailMaterial = mat;
                    particleSystemRenderer.material = mat;
                }

                //Trail
                {
                    var superTrail = new GameObject("boost-trail");
                    superTrail.transform.SetParent(boost.transform.parent);
                    superTrail.transform.localPosition = Vector3.zero;
                    superTrail.transform.localScale = Vector3.one;
                    superTrail.layer = 8;

                    var trailRenderer = superTrail.AddComponent<TrailRenderer>();

                    playerObjects.Add("Boost Trail", new PlayerObject("Boost Trail", superTrail));
                    playerObjects["Boost Trail"].values.Add("TrailRenderer", trailRenderer);

                    trailRenderer.material = mat;
                }

                //Boost Particles
                {
                    var superParticles = new GameObject("boost-particles");
                    superParticles.transform.SetParent(boost.transform.parent);
                    superParticles.transform.localPosition = Vector3.zero;
                    superParticles.transform.localScale = Vector3.one;
                    superParticles.layer = 8;

                    var particleSystem = superParticles.AddComponent<ParticleSystem>();
                    if (!superParticles.GetComponent<ParticleSystemRenderer>())
                    {
                        superParticles.AddComponent<ParticleSystemRenderer>();
                    }

                    var particleSystemRenderer = superParticles.GetComponent<ParticleSystemRenderer>();

                    playerObjects.Add("Boost Particles", new PlayerObject("Boost Particles", superParticles));
                    playerObjects["Boost Particles"].values.Add("ParticleSystem", particleSystem);
                    playerObjects["Boost Particles"].values.Add("ParticleSystemRenderer", particleSystemRenderer);

                    var main = particleSystem.main;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    main.loop = false;
                    main.playOnAwake = false;
                    particleSystemRenderer.renderMode = ParticleSystemRenderMode.Mesh;
                    particleSystemRenderer.alignment = ParticleSystemRenderSpace.View;

                    particleSystemRenderer.trailMaterial = mat;
                    particleSystemRenderer.material = mat;
                }

                //Tail Particles
                {
                    for (int i = 1; i < 4; i++)
                    {
                        var superParticles = new GameObject("tail-particles");
                        superParticles.transform.SetParent(playerObjects[string.Format("Tail {0} Base", i)].gameObject.transform);
                        superParticles.transform.localPosition = Vector3.zero;
                        superParticles.transform.localScale = Vector3.one;
                        superParticles.layer = 8;

                        var particleSystem = superParticles.AddComponent<ParticleSystem>();
                        if (!superParticles.GetComponent<ParticleSystemRenderer>())
                        {
                            superParticles.AddComponent<ParticleSystemRenderer>();
                        }

                        var particleSystemRenderer = superParticles.GetComponent<ParticleSystemRenderer>();

                        playerObjects.Add(string.Format("Tail {0} Particles", i), new PlayerObject(string.Format("Tail {0} Particles", i), superParticles));
                        playerObjects[string.Format("Tail {0} Particles", i)].values.Add("ParticleSystem", particleSystem);
                        playerObjects[string.Format("Tail {0} Particles", i)].values.Add("ParticleSystemRenderer", particleSystemRenderer);

                        var main = particleSystem.main;
                        main.simulationSpace = ParticleSystemSimulationSpace.World;
                        main.playOnAwake = false;
                        particleSystemRenderer.renderMode = ParticleSystemRenderMode.Mesh;
                        particleSystemRenderer.alignment = ParticleSystemRenderSpace.View;

                        particleSystemRenderer.trailMaterial = mat;
                        particleSystemRenderer.material = mat;
                    }
                }
            }

            health = PlayerManager.healthImages.Duplicate(PlayerManager.healthParent, $"Health {playerIndex}");

            for (int i = 0; i < 3; i++)
            {
                healthObjects.Add(new HealthObject(health.transform.GetChild(i).gameObject, health.transform.GetChild(i).GetComponent<Image>()));
            }

            var barBase = new GameObject("Bar Base");
            barBase.transform.SetParent(health.transform);
            barBase.transform.localScale = Vector3.one;

            var barBaseRT = barBase.AddComponent<RectTransform>();
            var barBaseLE = barBase.AddComponent<LayoutElement>();
            barBaseIm = barBase.AddComponent<Image>();

            barBaseLE.ignoreLayout = true;
            barBaseRT.anchoredPosition = new Vector2(-100f, 0f);
            barBaseRT.pivot = new Vector2(0f, 0.5f);
            barBaseRT.sizeDelta = new Vector2(200f, 32f);

            var bar = new GameObject("Bar");
            bar.transform.SetParent(barBase.transform);
            bar.transform.localScale = Vector3.one;

            barRT = bar.AddComponent<RectTransform>();
            barIm = bar.AddComponent<Image>();

            barRT.pivot = new Vector2(0f, 0.5f);
            barRT.anchoredPosition = new Vector2(-100f, 0f);

            health.SetActive(false);

            burst = playerObjects["Head"].gameObject.transform.Find("burst-explosion").GetComponent<ParticleSystem>();
            death = playerObjects["Head"].gameObject.transform.Find("death-explosion").GetComponent<ParticleSystem>();
            spawn = playerObjects["Head"].gameObject.transform.Find("spawn-implosion").GetComponent<ParticleSystem>();
        }

        public bool playerNeedsUpdating;
        void Start()
        {
            playerHitEvent += UpdateTail;
            Spawn();

            if (playerNeedsUpdating)
                UpdatePlayer();
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
            anim.SetTrigger("spawn");
            PlaySpawnParticles();

            try
            {
                path[0].pos = ((Transform)playerObjects["RB Parent"].values["Transform"]).position;
                path[0].rot = ((Transform)playerObjects["RB Parent"].values["Transform"]).rotation;
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

        #endregion

        #region Update Methods

        void Update()
        {
            if (UpdateMode == TailUpdateMode.Update)
                UpdateTailDistance();

            UpdateCustomTheme(); UpdateBoostTheme(); UpdateSpeeds(); UpdateTrailLengths(); UpdateTheme();
            if (canvas != null)
            {
                bool act = InputDataManager.inst.players.Count > 1 && ShowNameTags;
                canvas.SetActive(act);

                if (act && textMesh != null)
                {
                    textMesh.text = "<#" + LSColors.ColorToHex(GameManager.inst.LiveTheme.playerColors[playerIndex % 4]) + ">Player " + (playerIndex + 1).ToString() + " " + FontManager.TextTranslater.ConvertHealthToEquals(CustomPlayer.Health, initialHealthCount);
                    healthBase.material.color = LSColors.fadeColor(GameManager.inst.LiveTheme.playerColors[playerIndex % 4], 0.3f);
                    healthBase.transform.localScale = new Vector3((float)initialHealthCount * 2.25f, 1.5f, 1f);
                }
            }

            if (!PlayerModel)
                return;

            if (playerObjects["Boost Trail"].values["TrailRenderer"] != null && PlayerModel.boostPart.Trail.emitting)
            {
                var tf = playerObjects["Boost"].gameObject.transform;
                Vector2 v = new Vector2(tf.localScale.x, tf.localScale.y);

                ((TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"]).startWidth = PlayerModel.boostPart.Trail.startWidth * v.magnitude / 1.414213f;
                ((TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"]).endWidth = PlayerModel.boostPart.Trail.endWidth * v.magnitude / 1.414213f;
            }

            if (!PlayerAlive && !isDead && CustomPlayer && !PlayerManager.IsPractice)
                StartCoroutine(Kill());
        }

        bool canShoot = true;

        void FixedUpdate()
        {
            if (UpdateMode == TailUpdateMode.FixedUpdate)
                UpdateTailDistance();

            health?.SetActive(PlayerModel && PlayerModel.guiPart.active && GameManager.inst.timeline.activeSelf);
        }

        void LateUpdate()
        {
            if (UpdateMode == TailUpdateMode.LateUpdate)
                UpdateTailDistance();

            UpdateTailTransform(); UpdateTailDev(); UpdateTailSizes(); UpdateControls(); UpdateRotation();

            var player = playerObjects["RB Parent"].gameObject;

            // Here we handle the player's bounds to the camera. It is possible to include negative zoom in those bounds but it might not be a good idea since people have already utilized it.
            if (!OutOfBounds && !EventsConfig.Instance.EditorCamEnabled.Value && CoreHelper.Playing)
            {
                var cameraToViewportPoint = Camera.main.WorldToViewportPoint(player.transform.position);
                cameraToViewportPoint.x = Mathf.Clamp(cameraToViewportPoint.x, 0f, 1f);
                cameraToViewportPoint.y = Mathf.Clamp(cameraToViewportPoint.y, 0f, 1f);
                if (Camera.main.orthographicSize > 0f && (!includeNegativeZoom || Camera.main.orthographicSize < 0f) && CustomPlayer)
                {
                    float maxDistanceDelta = Time.deltaTime * 1500f;
                    player.transform.position = Vector3.MoveTowards(lastPos, Camera.main.ViewportToWorldPoint(cameraToViewportPoint), maxDistanceDelta);
                }
            }

            if (!PlayerModel || !PlayerModel.FaceControlActive || faceController == null)
                return;

            var vector = new Vector2(faceController.Move.Vector.x, faceController.Move.Vector.y);
            var fp = PlayerModel.FacePosition;
            if (vector.magnitude > 1f)
                vector = vector.normalized;

            if ((rotateMode == RotateMode.FlipX || rotateMode == RotateMode.RotateFlipX) && lastMovement.x < 0f)
                vector.x = -vector.x;
            if ((rotateMode == RotateMode.FlipY || rotateMode == RotateMode.RotateFlipY) && lastMovement.y < 0f)
                vector.y = -vector.y;

            playerObjects["Face Parent"].gameObject.transform.localPosition = new Vector3(vector.x * 0.3f + fp.x, vector.y * 0.3f + fp.y, 0f);
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

            timeHitOffset = Time.time - timeHit;
            if (timeHitOffset > PlayerModel.basePart.hitCooldown && isTakingHit)
            {
                isTakingHit = false;
                CanTakeDamage = true;
            }

            var idl = PlayerModel.basePart.moveSpeed;
            var bst = PlayerModel.basePart.boostSpeed;
            var bstcldwn = PlayerModel.basePart.boostCooldown;
            var bstmin = PlayerModel.basePart.minBoostTime;
            var bstmax = PlayerModel.basePart.maxBoostTime;

            idleSpeed = idl;
            boostSpeed = bst;

            boostCooldown = bstcldwn / pitch;
            minBoostTime = bstmin / pitch;
            maxBoostTime = bstmax / pitch;
        }

        void UpdateTailDistance()
        {
            path[0].pos = ((Transform)playerObjects["RB Parent"].values["Transform"]).position;
            path[0].rot = ((Transform)playerObjects["RB Parent"].values["Transform"]).rotation;
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
                    //if (isSpawning)
                    //{
                    //    path[i].pos = path[0].pos;
                    //    path[i].lastPos = path[0].pos;
                    //    path[i].rot = path[0].rot;
                    //    continue;
                    //}

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
                    var playerDelayTracker = (PlayerDelayTracker)playerObjects["Boost Tail Base"].values["PlayerDelayTracker"];
                    playerDelayTracker.offset = -i * tailDistance / 2f;
                    playerDelayTracker.positionOffset = 0.1f * (-i + 5);
                    playerDelayTracker.rotationOffset = 0.1f * (-i + 5);
                }

                if (playerObjects.TryGetValue($"Tail {i} Base", out PlayerObject tailObject))
                {
                    var playerDelayTracker = (PlayerDelayTracker)tailObject.values["PlayerDelayTracker"];
                    playerDelayTracker.offset = -num * tailDistance / 2f;
                    playerDelayTracker.positionOffset = 0.1f * (-num + 5);
                    playerDelayTracker.rotationOffset = 0.1f * (-num + 5);
                }
            }
        }

        void UpdateTailSizes()
        {
            if (!PlayerModel)
                return;

            for (int i = 0; i < PlayerModel.tailParts.Count; i++)
            {
                if (playerObjects.TryGetValue($"Tail {i + 1}", out PlayerObject tailObject))
                {
                    var t2 = PlayerModel.tailParts[i].scale;

                    tailObject.gameObject.transform.localScale = new Vector3(t2.x, t2.y, 1f);
                }
            }
        }

        void UpdateTrailLengths()
        {
            if (!PlayerModel)
                return;

            var pitch = CoreHelper.ForwardPitch;

            var headTrail = (TrailRenderer)playerObjects["Head Trail"].values["TrailRenderer"];
            var boostTrail = (TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"];

            headTrail.time = PlayerModel.headPart.Trail.time / pitch;
            boostTrail.time = PlayerModel.boostPart.Trail.time / pitch;

            for (int i = 0; i < PlayerModel.tailParts.Count; i++)
            {
                if (playerObjects.TryGetValue($"Tail {i + 1}", out PlayerObject tailObject))
                {
                    var tailTrail = (TrailRenderer)tailObject.values["TrailRenderer"];

                    tailTrail.time = PlayerModel.tailParts[i].Trail.time / pitch;
                }
            }
        }

        bool queuedBoost;

        void UpdateControls()
        {
            if (!CustomPlayer || !PlayerModel || !PlayerAlive)
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
                            AudioManager.inst.PlaySound("boost_recover");

                        currentJumpCount++;
                        currentJumpBoostCount++;
                    }

                    StartBoost();
                    return;
                }

                if (isBoosting && !isBoostCancelled && (Actions.Boost.WasReleased || startBoostTime + maxBoostTime <= Time.time))
                    InitMidBoost(true);
            }

            if (PlayerAlive && faceController != null && PlayerModel.bulletPart.active &&
                (!PlayerModel.bulletPart.constant && faceController.Shoot.WasPressed && canShoot ||
                    PlayerModel.bulletPart.constant && faceController.Shoot.IsPressed && canShoot))
                CreateBullet();

            var player = playerObjects["RB Parent"].gameObject;

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
                        AudioManager.inst.PlaySound("boost");

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

                var sp = PlayerModel.basePart.sprintSneakActive ? faceController.Sprint.IsPressed ? 1.3f : faceController.Sneak.IsPressed ? 0.1f : 1f : 1f;

                velocity.x = x * idleSpeed * pitch * sp * SpeedMultiplier;

                rb.velocity = velocity;

                return;
            }

            rb.gravityScale = 0f;

            if (PlayerAlive && Actions != null && CustomPlayer.active && CanMove && !CoreHelper.Paused &&
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

                    var sp = (bool)PlayerModel.basePart.sprintSneakActive ? faceController.Sprint.IsPressed ? 1.3f : faceController.Sneak.IsPressed ? 0.1f : 1f : 1f;

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
            if (PlayerAlive && CustomPlayer.active && CanMove && !CoreHelper.Paused && !CoreHelper.IsUsingInputField && movementMode == MovementMode.Mouse && CoreHelper.InEditorPreview && Application.isFocused && isKeyboard && !EventsConfig.Instance.EditorCamEnabled.Value)
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
            var player = playerObjects["RB Parent"].gameObject;

            if (CanRotate)
            {
                var b = Quaternion.AngleAxis(Mathf.Atan2(lastMovement.y, lastMovement.x) * 57.29578f, player.transform.forward);
                var c = Quaternion.Slerp(player.transform.rotation, b, 720f * Time.deltaTime);
                switch (rotateMode)
                {
                    case RotateMode.RotateToDirection:
                        {
                            player.transform.rotation = c;

                            playerObjects["Face Base"].gameObject.transform.localRotation = Quaternion.identity;

                            break;
                        }
                    case RotateMode.None:
                        {
                            player.transform.rotation = Quaternion.identity;

                            playerObjects["Face Base"].gameObject.transform.rotation = c;

                            break;
                        }
                    case RotateMode.FlipX:
                        {
                            b = Quaternion.AngleAxis(Mathf.Atan2(lastMovementTotal.y, lastMovementTotal.x) * 57.29578f, player.transform.forward);
                            c = Quaternion.Slerp(player.transform.rotation, b, 720f * Time.deltaTime);

                            var vectorRotation = c.eulerAngles;
                            if (vectorRotation.z > 90f && vectorRotation.z < 270f)
                                vectorRotation.z = -vectorRotation.z + 180f;

                            playerObjects["Face Base"].gameObject.transform.rotation = Quaternion.Euler(vectorRotation);

                            player.transform.rotation = Quaternion.identity;

                            if (lastMovement.x > 0.01f)
                            {
                                if (!stretch)
                                    player.transform.localScale = Vector3.one;
                                if (!animatingBoost)
                                    playerObjects["Boost Tail Base"].gameObject.transform.localScale = Vector3.one;
                                playerObjects["Tail 1 Base"].gameObject.transform.localScale = Vector3.one;
                                playerObjects["Tail 2 Base"].gameObject.transform.localScale = Vector3.one;
                                playerObjects["Tail 3 Base"].gameObject.transform.localScale = Vector3.one;
                            }
                            if (lastMovement.x < -0.01f)
                            {
                                var stretchScale = new Vector3(-1f, 1f, 1f);
                                if (!stretch)
                                    player.transform.localScale = stretchScale;
                                if (!animatingBoost)
                                    playerObjects["Boost Tail Base"].gameObject.transform.localScale = stretchScale;
                                playerObjects["Tail 1 Base"].gameObject.transform.localScale = stretchScale;
                                playerObjects["Tail 2 Base"].gameObject.transform.localScale = stretchScale;
                                playerObjects["Tail 3 Base"].gameObject.transform.localScale = stretchScale;
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

                            playerObjects["Face Base"].gameObject.transform.rotation = Quaternion.Euler(vectorRotation);

                            player.transform.rotation = Quaternion.identity;

                            if (lastMovement.y > 0.01f)
                            {
                                if (!stretch)
                                    player.transform.localScale = Vector3.one;
                                if (!animatingBoost)
                                    playerObjects["Boost Tail Base"].gameObject.transform.localScale = Vector3.one;
                                playerObjects["Tail 1 Base"].gameObject.transform.localScale = Vector3.one;
                                playerObjects["Tail 2 Base"].gameObject.transform.localScale = Vector3.one;
                                playerObjects["Tail 3 Base"].gameObject.transform.localScale = Vector3.one;
                            }
                            if (lastMovement.y < -0.01f)
                            {
                                var stretchScale = new Vector3(1f, -1f, 1f);
                                if (!stretch)
                                    player.transform.localScale = stretchScale;
                                if (!animatingBoost)
                                    playerObjects["Boost Tail Base"].gameObject.transform.localScale = stretchScale;
                                playerObjects["Tail 1 Base"].gameObject.transform.localScale = stretchScale;
                                playerObjects["Tail 2 Base"].gameObject.transform.localScale = stretchScale;
                                playerObjects["Tail 3 Base"].gameObject.transform.localScale = stretchScale;
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
                                    AnimationManager.inst.RemoveID(rotateResetAnimation.id);
                                    rotateResetAnimation = null;
                                }

                                player.transform.rotation = c;

                                playerObjects["Face Base"].gameObject.transform.localRotation = Quaternion.identity;
                            }

                            var time = Time.time - timeNotMovingOffset;

                            if (moved && !animatingRotateReset && time > 0.03f)
                            {
                                animatingRotateReset = true;

                                var z = player.transform.rotation.eulerAngles.z;

                                if (rotateResetAnimation != null)
                                {
                                    AnimationManager.inst.RemoveID(rotateResetAnimation.id);
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
                                        if (!player || !playerObjects["Face Base"].gameObject)
                                            return;

                                        player.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, x));

                                        playerObjects["Face Base"].gameObject.transform.localRotation = Quaternion.identity;
                                    }),
                                };

                                rotateResetAnimation.onComplete = () =>
                                {
                                    if (rotateResetAnimation == null)
                                        return;

                                    AnimationManager.inst.RemoveID(rotateResetAnimation.id);
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

                            playerObjects["Face Base"].gameObject.transform.localRotation = Quaternion.Euler(vectorRotation);

                            if (lastMovement.x > 0.01f)
                            {
                                if (!stretch)
                                    player.transform.localScale = Vector3.one;
                                if (!animatingBoost)
                                    playerObjects["Boost Tail Base"].gameObject.transform.localScale = Vector3.one;
                                playerObjects["Tail 1 Base"].gameObject.transform.localScale = Vector3.one;
                                playerObjects["Tail 2 Base"].gameObject.transform.localScale = Vector3.one;
                                playerObjects["Tail 3 Base"].gameObject.transform.localScale = Vector3.one;
                            }
                            if (lastMovement.x < -0.01f)
                            {
                                var stretchScale = new Vector3(-1f, 1f, 1f);
                                if (!stretch)
                                    player.transform.localScale = stretchScale;
                                if (!animatingBoost)
                                    playerObjects["Boost Tail Base"].gameObject.transform.localScale = stretchScale;
                                playerObjects["Tail 1 Base"].gameObject.transform.localScale = stretchScale;
                                playerObjects["Tail 2 Base"].gameObject.transform.localScale = stretchScale;
                                playerObjects["Tail 3 Base"].gameObject.transform.localScale = stretchScale;
                            }

                            break;
                        }
                    case RotateMode.RotateFlipY:
                        {
                            var vectorRotation = c.eulerAngles;
                            if (vectorRotation.z > 0f && vectorRotation.z < 180f)
                                vectorRotation.z = -vectorRotation.z + 90f;

                            player.transform.rotation = Quaternion.Euler(vectorRotation);

                            playerObjects["Face Base"].gameObject.transform.rotation = Quaternion.identity;

                            if (lastMovement.y > 0.01f)
                            {
                                if (!stretch)
                                    player.transform.localScale = Vector3.one;
                                if (!animatingBoost)
                                    playerObjects["Boost Tail Base"].gameObject.transform.localScale = Vector3.one;
                                playerObjects["Tail 1 Base"].gameObject.transform.localScale = Vector3.one;
                                playerObjects["Tail 2 Base"].gameObject.transform.localScale = Vector3.one;
                                playerObjects["Tail 3 Base"].gameObject.transform.localScale = Vector3.one;
                            }
                            if (lastMovement.y < -0.01f)
                            {
                                var stretchScale = new Vector3(1f, -1f, 1f);
                                if (!stretch)
                                    player.transform.localScale = stretchScale;
                                if (!animatingBoost)
                                    playerObjects["Boost Tail Base"].gameObject.transform.localScale = stretchScale;
                                playerObjects["Tail 1 Base"].gameObject.transform.localScale = stretchScale;
                                playerObjects["Tail 2 Base"].gameObject.transform.localScale = stretchScale;
                                playerObjects["Tail 3 Base"].gameObject.transform.localScale = stretchScale;
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

            if (playerObjects.TryGetValue("Head", out PlayerObject headObject))
            {
                if (headObject.values["MeshRenderer"] is MeshRenderer headMeshRenderer && headMeshRenderer)
                {
                    int col = PlayerModel.headPart.color;
                    var colHex = PlayerModel.headPart.customColor;
                    float alpha = PlayerModel.headPart.opacity;

                    headMeshRenderer.material.color = GetColor(col, alpha, colHex);
                }

                try
                {
                    int colStart = PlayerModel.headPart.color;
                    var colStartHex = PlayerModel.headPart.customColor;
                    float alphaStart = PlayerModel.headPart.opacity;

                    var main1 = burst.main;
                    var main2 = death.main;
                    var main3 = spawn.main;
                    main1.startColor = new ParticleSystem.MinMaxGradient(GetColor(colStart, alphaStart, colStartHex));
                    main2.startColor = new ParticleSystem.MinMaxGradient(GetColor(colStart, alphaStart, colStartHex));
                    main3.startColor = new ParticleSystem.MinMaxGradient(GetColor(colStart, alphaStart, colStartHex));
                }
                catch
                {

                }
            }

            if (playerObjects.TryGetValue("Boost", out PlayerObject boostObject) && boostObject.values["MeshRenderer"] is MeshRenderer boostMeshRenderer && boostMeshRenderer)
            {
                int colStart = PlayerModel.boostPart.color;
                var colStartHex = PlayerModel.boostPart.customColor;
                float alphaStart = PlayerModel.boostPart.opacity;

                boostMeshRenderer.material.color = GetColor(colStart, alphaStart, colStartHex);
            }

            if (playerObjects.TryGetValue("Boost Tail", out PlayerObject boostTailObject) && boostTailObject.values["MeshRenderer"] is MeshRenderer boostTailMeshRenderer && boostTailMeshRenderer)
            {
                int colStart = PlayerModel.boostTailPart.color;
                var colStartHex = PlayerModel.boostTailPart.customColor;
                float alphaStart = PlayerModel.boostTailPart.opacity;

                boostTailMeshRenderer.material.color = GetColor(colStart, alphaStart, colStartHex);
            }

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
                        healthObjects[i].image.color = GetColor(topCol, topAlpha, topColHex);

                barBaseIm.color = GetColor(baseCol, baseAlpha, baseColHex);
                barIm.color = GetColor(topCol, topAlpha, topColHex);
            }

            for (int i = 0; i < PlayerModel.tailParts.Count; i++)
            {
                int col = PlayerModel.tailParts[i].color;
                var colHex = PlayerModel.tailParts[i].customColor;
                float alpha = PlayerModel.tailParts[i].opacity;

                int colStart = PlayerModel.tailParts[i].Trail.startColor;
                var colStartHex = PlayerModel.tailParts[i].Trail.startCustomColor;
                float alphaStart = PlayerModel.tailParts[i].Trail.startOpacity;
                int colEnd = PlayerModel.tailParts[i].Trail.endColor;
                var colEndHex = PlayerModel.tailParts[i].Trail.endCustomColor;
                float alphaEnd = PlayerModel.tailParts[i].Trail.endOpacity;

                var psCol = PlayerModel.tailParts[i].Particles.color;
                var psColHex = PlayerModel.tailParts[i].Particles.customColor;
                var str = $"Tail {i + 1} Particles";

                if (playerObjects.TryGetValue(str, out PlayerObject tailParticlesObject) && tailParticlesObject.values.TryGetValue("ParticleSystem", out object psObj) && psObj is ParticleSystem ps)
                {
                    var main = ps.main;

                    main.startColor = GetColor(psCol, 1f, psColHex);

                    var tailObject = playerObjects[$"Tail {i + 1}"];
                    ((MeshRenderer)tailObject.values["MeshRenderer"]).material.color = GetColor(col, alpha, colHex);
                    var trailRenderer = (TrailRenderer)tailObject.values["TrailRenderer"];

                    trailRenderer.startColor = GetColor(colStart, alphaStart, colStartHex);
                    trailRenderer.endColor = GetColor(colEnd, alphaEnd, colEndHex);
                }
            }

            if (PlayerModel.headPart.Trail.emitting && playerObjects["Head Trail"].values.TryGetValue("TrailRenderer", out object objTrailRenderer) && objTrailRenderer is TrailRenderer headTrailRenderer && headTrailRenderer)
            {
                int colStart = PlayerModel.headPart.Trail.startColor;
                var colStartHex = PlayerModel.headPart.Trail.startCustomColor;
                float alphaStart = PlayerModel.headPart.Trail.startOpacity;
                int colEnd = PlayerModel.headPart.Trail.endColor;
                var colEndHex = PlayerModel.headPart.Trail.endCustomColor;
                float alphaEnd = PlayerModel.headPart.Trail.endOpacity;

                headTrailRenderer.startColor = GetColor(colStart, alphaStart, colStartHex);
                headTrailRenderer.endColor = GetColor(colEnd, alphaEnd, colEndHex);
            }

            if (PlayerModel.headPart.Particles.emitting && playerObjects["Head Particles"].values.TryGetValue("ParticleSystem", out object objHeadParticles) && objHeadParticles is ParticleSystem headParticleSystem && headParticleSystem)
            {
                var colStart = PlayerModel.headPart.Particles.color;
                var colStartHex = PlayerModel.headPart.Particles.customColor;

                var main = headParticleSystem.main;
                main.startColor = GetColor(colStart, 1f, colStartHex);
            }

            if (PlayerModel.boostPart.Trail.emitting && playerObjects["Boost Trail"].values.TryGetValue("TrailRenderer", out object objBoostTrail) && objBoostTrail is TrailRenderer boostTrailRenderer && boostTrailRenderer)
            {
                var colStart = PlayerModel.boostPart.Trail.startColor;
                var colStartHex = PlayerModel.boostPart.Trail.startCustomColor;
                var alphaStart = PlayerModel.boostPart.Trail.startOpacity;
                var colEnd = PlayerModel.boostPart.Trail.endColor;
                var colEndHex = PlayerModel.boostPart.Trail.endCustomColor;
                var alphaEnd = PlayerModel.boostPart.Trail.endOpacity;

                boostTrailRenderer.startColor = GetColor(colStart, alphaStart, colStartHex);
                boostTrailRenderer.endColor = GetColor(colEnd, alphaEnd, colEndHex);
            }

            if (PlayerModel.boostPart.Particles.emitting && playerObjects["Boost Particles"].values.TryGetValue("ParticleSystem", out object objBoostParticles) && objBoostParticles is ParticleSystem boostParticleSystem && boostParticleSystem)
            {
                var colStart = PlayerModel.boostPart.Particles.color;
                var colHex = PlayerModel.boostPart.Particles.customColor;

                var main = boostParticleSystem.main;
                main.startColor = GetColor(colStart, 1f, colHex);
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
            var player = playerObjects["RB Parent"].gameObject;

            if (!CanTakeDamage || !PlayerAlive)
                return;

            timeHit = Time.time;

            InitBeforeHit();
            if (PlayerAlive)
                anim.SetTrigger("hurt");
            if (CustomPlayer == null)
                return;

            if (!PlayerManager.IsPractice)
                CustomPlayer.Health--;
            playerHitEvent?.Invoke(CustomPlayer.Health, rb.position);
        }

        IEnumerator BoostCooldownLoop()
        {
            var player = playerObjects["RB Parent"].gameObject;
            var headTrail = (TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"];
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
                AudioManager.inst.PlaySound("boost_recover");

            if (showBoostTail)
            {
                path[1].active = true;
                var tweener = playerObjects["Boost Tail Base"].gameObject.transform.DOScale(Vector3.one, 0.1f / CoreHelper.ForwardPitch).SetEase(DataManager.inst.AnimationList[9].Animation);
                tweener.OnComplete(() => { animatingBoost = false; });
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
            Destroy(health);
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

            var ps = (ParticleSystem)playerObjects["Boost Particles"].values["ParticleSystem"];
            var emission = ps.emission;

            var headTrail = (TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"];

            if (emission.enabled)
                ps.Play();
            if (PlayerModel && PlayerModel.boostPart.Trail.emitting)
                headTrail.emitting = true;

            if (PlayBoostSound)
                AudioManager.inst.PlaySound("boost");

            CreatePulse();

            stretchVector = new Vector2(stretchAmount * 1.5f, -(stretchAmount * 1.5f));

            if (showBoostTail)
            {
                path[1].active = false;
                animatingBoost = true;
                playerObjects["Boost Tail Base"].gameObject.transform.DOScale(Vector3.zero, 0.05f / CoreHelper.ForwardPitch).SetEase(DataManager.inst.AnimationList[2].Animation);
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

            AudioManager.inst.PlaySound(CoreConfig.Instance.Language.Value == Language.Pirate ? "pirate_KillPlayer" : "HurtPlayer");
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

        public void PlaySpawnParticles() => spawn?.Play();

        public void PlayDeathParticles() => death?.Play();

        public void PlayHitParticles() => burst?.Play();

        #endregion

        #region Update Values

        public bool updated;

        public void UpdatePlayer()
        {
            if (!PlayerModel)
                return;

            var currentModel = PlayerModel;

            var rbParent = playerObjects["RB Parent"];
            var head = playerObjects["Head"];
            var boost = playerObjects["Boost"];
            var boostBase = playerObjects["Boost Base"];
            var boostTail = playerObjects["Boost Tail"];
            var boostTailBase = playerObjects["Boost Tail Base"];

            //New NameTag
            {
                Destroy(canvas);
                canvas = Creator.NewGameObject("Name Tag Canvas" + (playerIndex + 1).ToString(), transform);
                canvas.transform.localRotation = Quaternion.identity;

                var bae = ObjectManager.inst.objectPrefabs[0].options[0].Duplicate(canvas.transform);
                bae.transform.localScale = Vector3.one;
                bae.transform.localRotation = Quaternion.identity;

                bae.transform.GetChild(0).transform.localScale = new Vector3(6.5f, 1.5f, 1f);
                bae.transform.GetChild(0).transform.localPosition = new Vector3(0f, 2.5f, -0.3f);

                healthBase = bae.GetComponentInChildren<MeshRenderer>();
                healthBase.enabled = true;

                Destroy(bae.GetComponentInChildren<SelectObject>());
                Destroy(bae.GetComponentInChildren<SelectObjectInEditor>());
                Destroy(bae.GetComponentInChildren<Collider2D>());

                var tae = ObjectManager.inst.objectPrefabs[4].options[0].Duplicate(canvas.transform);
                tae.transform.localScale = Vector3.one;
                tae.transform.localRotation = Quaternion.identity;

                tae.transform.GetChild(0).transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                tae.transform.GetChild(0).transform.localPosition = new Vector3(0f, 2.5f, -0.3f);

                textMesh = tae.GetComponentInChildren<TextMeshPro>();

                var d = canvas.AddComponent<PlayerDelayTracker>();
                d.leader = rbParent.gameObject.transform;
                d.scaleParent = false;
                d.rotationParent = false;
                d.player = this;
                d.positionOffset = 0.9f;
            }

            //Set new transform values
            {
                //Head Shape
                {
                    int s = currentModel.headPart.shape.type;
                    int so = currentModel.headPart.shape.option;

                    s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                    so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                    ((MeshFilter)head.values["MeshFilter"]).mesh =
                        ObjectManager.inst.objectPrefabs[s != 4 && s != 6 ? s : 0].options[s != 4 && s != 6 ? so : 0].GetComponentInChildren<MeshFilter>().mesh;
                }

                //Boost Shape
                {
                    int s = currentModel.boostPart.shape.type;
                    int so = currentModel.boostPart.shape.option;

                    s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                    so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                    ((MeshFilter)boost.values["MeshFilter"]).mesh =
                        ObjectManager.inst.objectPrefabs[s != 4 && s != 6 ? s : 0].options[s != 4 && s != 6 ? so : 0].GetComponentInChildren<MeshFilter>().mesh;
                }

                //Tail Boost Shape
                {
                    int s = currentModel.boostTailPart.shape.type;
                    int so = currentModel.boostTailPart.shape.option;

                    s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                    so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                    ((MeshFilter)boostTail.values["MeshFilter"]).mesh =
                        ObjectManager.inst.objectPrefabs[s != 4 && s != 6 ? s : 0].options[s != 4 && s != 6 ? so : 0].GetComponentInChildren<MeshFilter>().mesh;
                }

                //Tail 1 Shape
                for (int i = 0; i < currentModel.tailParts.Count; i++)
                {
                    int s = currentModel.tailParts[i].shape.type;
                    int so = currentModel.tailParts[i].shape.option;

                    s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                    so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                    if (playerObjects.TryGetValue($"Tail {i + 1}", out PlayerObject tailObject))
                        ((MeshFilter)tailObject.values["MeshFilter"]).mesh =
                        ObjectManager.inst.objectPrefabs[s != 4 && s != 6 ? s : 0].options[s != 4 && s != 6 ? so : 0].GetComponentInChildren<MeshFilter>().mesh;
                }

                var h1 = currentModel.headPart.position;
                var h2 = currentModel.headPart.scale;
                var h3 = currentModel.headPart.rotation;

                head.gameObject.transform.localPosition = new Vector3(h1.x, h1.y, 0f);
                head.gameObject.transform.localScale = new Vector3(h2.x, h2.y, 1f);
                head.gameObject.transform.localEulerAngles = new Vector3(0f, 0f, h3);

                var b1 = currentModel.boostPart.position;
                var b2 = currentModel.boostPart.scale;
                var b3 = currentModel.boostPart.rotation;

                ((MeshRenderer)boost.values["MeshRenderer"]).enabled = currentModel.boostPart.active;
                boostBase.gameObject.transform.localPosition = new Vector3(b1.x, b1.y, 0.1f);
                boostBase.gameObject.transform.localScale = new Vector3(b2.x, b2.y, 1f);
                boostBase.gameObject.transform.localEulerAngles = new Vector3(0f, 0f, b3);

                tailDistance = currentModel.tailBase.distance;
                tailMode = (int)currentModel.tailBase.mode;

                tailGrows = currentModel.tailBase.grows;

                showBoostTail = currentModel.boostTailPart.active;

                boostTailBase.gameObject.SetActive(showBoostTail);

                var fp = currentModel.FacePosition;
                playerObjects["Face Parent"].gameObject.transform.localPosition = new Vector3(fp.x, fp.y, 0f);

                if (!isBoosting)
                    path[1].active = showBoostTail;

                jumpGravity = currentModel.basePart.jumpGravity;
                jumpIntensity = currentModel.basePart.jumpIntensity;
                jumpCount = currentModel.basePart.jumpCount;
                jumpBoostCount = currentModel.basePart.jumpBoostCount;
                bounciness = currentModel.basePart.bounciness;

                rb.sharedMaterial.bounciness = bounciness;
                //Stretch
                {
                    stretch = currentModel.stretchPart.active;
                    stretchAmount = currentModel.stretchPart.amount;
                    stretchEasing = currentModel.stretchPart.easing;
                }

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

                ((CircleCollider2D)rbParent.values["CircleCollider2D"]).isTrigger = PlayerManager.IsZenMode && ZenEditorIncludesSolid;
                ((PolygonCollider2D)rbParent.values["PolygonCollider2D"]).isTrigger = PlayerManager.IsZenMode && ZenEditorIncludesSolid;

                var colAcc = (bool)currentModel.basePart.collisionAccurate;
                ((CircleCollider2D)rbParent.values["CircleCollider2D"]).enabled = !colAcc;
                ((PolygonCollider2D)rbParent.values["PolygonCollider2D"]).enabled = colAcc;
                if (colAcc)
                    ((PolygonCollider2D)rbParent.values["PolygonCollider2D"]).CreateCollider((MeshFilter)playerObjects["Head"].values["MeshFilter"]);

                for (int i = 0; i < currentModel.tailParts.Count; i++)
                {
                    var t1 = currentModel.tailParts[i].position;
                    var t2 = currentModel.tailParts[i].scale;
                    var t3 = currentModel.tailParts[i].rotation;

                    if (playerObjects.TryGetValue($"Tail {i + 1}", out PlayerObject tailObject))
                    {
                        ((MeshRenderer)tailObject.values["MeshRenderer"]).enabled = currentModel.tailParts[i].active;
                        tailObject.gameObject.transform.localPosition = new Vector3(t1.x, t1.y, 0.1f);
                        tailObject.gameObject.transform.localScale = new Vector3(t2.x, t2.y, 1f);
                        tailObject.gameObject.transform.localEulerAngles = new Vector3(0f, 0f, t3);
                    }
                }

                //Health
                {
                    if (CustomPlayer)
                        CustomPlayer.Health = PlayerManager.IsNoHit ? 1 : currentModel.basePart.health;
                }

                //Health Images
                {
                    foreach (var health in healthObjects)
                    {
                        if (health.image)
                        {
                            health.image.sprite = RTFile.FileExists(RTFile.BasePath + "health.png") && !AssetsGlobal ? SpriteHelper.LoadSprite(RTFile.BasePath + "health.png") :
                                RTFile.FileExists(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/health.png") ? SpriteHelper.LoadSprite(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets/health.png") :
                                PlayerManager.healthSprite;
                        }
                    }
                }

                //Trail
                {
                    var headTrailRenderer = (TrailRenderer)playerObjects["Head Trail"].values["TrailRenderer"];

                    headTrailRenderer.gameObject.transform.localPosition = currentModel.headPart.Trail.positionOffset;

                    headTrailRenderer.enabled = currentModel.headPart.Trail.emitting;
                    headTrailRenderer.startWidth = currentModel.headPart.Trail.startWidth;
                    headTrailRenderer.endWidth = currentModel.headPart.Trail.endWidth;
                }

                //Particles
                if (playerObjects.TryGetValue("Head Particles", out PlayerObject headParticles))
                {
                    var headParticlesSystem = (ParticleSystem)headParticles.values["ParticleSystem"];
                    var headParticlesSystemRenderer = (ParticleSystemRenderer)headParticles.values["ParticleSystemRenderer"];

                    var s = currentModel.headPart.Particles.shape.type;
                    var so = currentModel.headPart.Particles.shape.option;

                    s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                    so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                    if (s != 4 && s != 6)
                    {
                        headParticlesSystemRenderer.mesh = ObjectManager.inst.objectPrefabs[s].options[so].GetComponentInChildren<MeshFilter>().mesh;
                    }

                    var main = headParticlesSystem.main;
                    var emission = headParticlesSystem.emission;

                    main.startLifetime = currentModel.headPart.Particles.lifeTime;
                    main.startSpeed = currentModel.headPart.Particles.speed;

                    emission.enabled = currentModel.headPart.Particles.emitting;
                    headParticlesSystem.emissionRate = currentModel.headPart.Particles.amount;

                    var rotationOverLifetime = headParticlesSystem.rotationOverLifetime;
                    rotationOverLifetime.enabled = true;
                    rotationOverLifetime.separateAxes = true;
                    rotationOverLifetime.xMultiplier = 0f;
                    rotationOverLifetime.yMultiplier = 0f;
                    rotationOverLifetime.zMultiplier = currentModel.headPart.Particles.rotation;

                    var forceOverLifetime = headParticlesSystem.forceOverLifetime;
                    forceOverLifetime.enabled = true;
                    forceOverLifetime.space = ParticleSystemSimulationSpace.World;
                    forceOverLifetime.xMultiplier = currentModel.headPart.Particles.force.x;
                    forceOverLifetime.yMultiplier = currentModel.headPart.Particles.force.y;
                    forceOverLifetime.zMultiplier = 0f;

                    var particlesTrail = headParticlesSystem.trails;
                    particlesTrail.enabled = currentModel.headPart.Particles.trailEmitting;

                    var colorOverLifetime = headParticlesSystem.colorOverLifetime;
                    colorOverLifetime.enabled = true;
                    var psCol = colorOverLifetime.color;

                    float alphaStart = currentModel.headPart.Particles.startOpacity;
                    float alphaEnd = currentModel.headPart.Particles.endOpacity;

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

                    var sizeOverLifetime = headParticlesSystem.sizeOverLifetime;
                    sizeOverLifetime.enabled = true;

                    var ssss = sizeOverLifetime.size;

                    var sizeStart = currentModel.headPart.Particles.startScale;
                    var sizeEnd = currentModel.headPart.Particles.endScale;

                    var curve = new AnimationCurve(new Keyframe[2]
                    {
                        new Keyframe(0f, sizeStart),
                        new Keyframe(1f, sizeEnd)
                    });

                    ssss.curve = curve;

                    sizeOverLifetime.size = ssss;
                }

                //Boost Trail
                {
                    var boostTrail = (TrailRenderer)playerObjects["Boost Trail"].values["TrailRenderer"];
                    boostTrail.enabled = currentModel.boostPart.Trail.emitting;
                    boostTrail.emitting = currentModel.boostPart.Trail.emitting;
                }

                //Boost Particles
                if (playerObjects.TryGetValue("Boost Particles", out PlayerObject boostParticles))
                {
                    var boostParticlesSystem = (ParticleSystem)boostParticles.values["ParticleSystem"];
                    var boostParticlesSystemRenderer = (ParticleSystemRenderer)boostParticles.values["ParticleSystemRenderer"];

                    var s = currentModel.boostPart.Particles.shape.type;
                    var so = currentModel.boostPart.Particles.shape.option;

                    s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                    so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                    if (s != 4 && s != 6)
                        boostParticlesSystemRenderer.mesh = ObjectManager.inst.objectPrefabs[s].options[so].GetComponentInChildren<MeshFilter>().mesh;

                    var main = boostParticlesSystem.main;
                    var emission = boostParticlesSystem.emission;

                    main.startLifetime = currentModel.boostPart.Particles.lifeTime;
                    main.startSpeed = currentModel.boostPart.Particles.speed;

                    emission.enabled = currentModel.boostPart.Particles.emitting;
                    boostParticlesSystem.emissionRate = 0f;
                    emission.burstCount = (int)currentModel.boostPart.Particles.amount;
                    main.duration = 1f;

                    var rotationOverLifetime = boostParticlesSystem.rotationOverLifetime;
                    rotationOverLifetime.enabled = true;
                    rotationOverLifetime.separateAxes = true;
                    rotationOverLifetime.xMultiplier = 0f;
                    rotationOverLifetime.yMultiplier = 0f;
                    rotationOverLifetime.zMultiplier = currentModel.boostPart.Particles.rotation;

                    var forceOverLifetime = boostParticlesSystem.forceOverLifetime;
                    forceOverLifetime.enabled = true;
                    forceOverLifetime.space = ParticleSystemSimulationSpace.World;
                    forceOverLifetime.xMultiplier = currentModel.boostPart.Particles.force.x;
                    forceOverLifetime.yMultiplier = currentModel.boostPart.Particles.force.y;
                    forceOverLifetime.zMultiplier = 0f;

                    var particlesTrail = boostParticlesSystem.trails;
                    particlesTrail.enabled = currentModel.boostPart.Particles.trailEmitting;

                    var colorOverLifetime = boostParticlesSystem.colorOverLifetime;
                    colorOverLifetime.enabled = true;
                    var psCol = colorOverLifetime.color;

                    float alphaStart = currentModel.boostPart.Particles.startOpacity;
                    float alphaEnd = currentModel.boostPart.Particles.endOpacity;

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

                    var sizeOverLifetime = boostParticlesSystem.sizeOverLifetime;
                    sizeOverLifetime.enabled = true;

                    var ssss = sizeOverLifetime.size;

                    var sizeStart = currentModel.boostPart.Particles.startScale;
                    var sizeEnd = currentModel.boostPart.Particles.endScale;

                    var curve = new AnimationCurve(new Keyframe[2]
                    {
                        new Keyframe(0f, sizeStart),
                        new Keyframe(1f, sizeEnd)
                    });

                    ssss.curve = curve;

                    sizeOverLifetime.size = ssss;
                }

                //Tails Trail / Particles
                {
                    for (int i = 0; i < PlayerModel.tailParts.Count; i++)
                    {
                        if (playerObjects.TryGetValue($"Tail {i + 1}", out PlayerObject tailObject) && playerObjects.TryGetValue($"Tail {i + 1} Particles", out PlayerObject tailParticles))
                        {
                            var headTrail = (TrailRenderer)tailObject.values["TrailRenderer"];
                            headTrail.enabled = currentModel.tailParts[i].Trail.emitting;
                            headTrail.emitting = currentModel.tailParts[i].Trail.emitting;
                            headTrail.startWidth = currentModel.tailParts[i].Trail.startWidth;
                            headTrail.endWidth = currentModel.tailParts[i].Trail.endWidth;

                            var tailParticleSystem = (ParticleSystem)playerObjects[string.Format("Tail {0} Particles", i + 1)].values["ParticleSystem"];
                            var tailParticleSystemRenderer = (ParticleSystemRenderer)playerObjects[string.Format("Tail {0} Particles", i + 1)].values["ParticleSystemRenderer"];

                            var s = currentModel.tailParts[i].Particles.shape.type;
                            var so = currentModel.tailParts[i].Particles.shape.option;

                            s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                            so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                            if (s != 4 && s != 6)
                                tailParticleSystemRenderer.mesh = ObjectManager.inst.objectPrefabs[s].options[so].GetComponentInChildren<MeshFilter>().mesh;

                            var main = tailParticleSystem.main;
                            var emission = tailParticleSystem.emission;

                            main.startLifetime = currentModel.tailParts[i].Particles.lifeTime;
                            main.startSpeed = currentModel.tailParts[i].Particles.speed;

                            emission.enabled = currentModel.tailParts[i].Particles.emitting;
                            tailParticleSystem.emissionRate = currentModel.tailParts[i].Particles.amount;

                            var rotationOverLifetime = tailParticleSystem.rotationOverLifetime;
                            rotationOverLifetime.enabled = true;
                            rotationOverLifetime.separateAxes = true;
                            rotationOverLifetime.xMultiplier = 0f;
                            rotationOverLifetime.yMultiplier = 0f;
                            rotationOverLifetime.zMultiplier = currentModel.tailParts[i].Particles.rotation;

                            var forceOverLifetime = tailParticleSystem.forceOverLifetime;
                            forceOverLifetime.enabled = true;
                            forceOverLifetime.space = ParticleSystemSimulationSpace.World;
                            forceOverLifetime.xMultiplier = currentModel.tailParts[i].Particles.force.x;
                            forceOverLifetime.yMultiplier = currentModel.tailParts[i].Particles.force.y;

                            var particlesTrail = tailParticleSystem.trails;
                            particlesTrail.enabled = currentModel.tailParts[i].Particles.trailEmitting;

                            var colorOverLifetime = tailParticleSystem.colorOverLifetime;
                            colorOverLifetime.enabled = true;
                            var psCol = colorOverLifetime.color;

                            float alphaStart = currentModel.tailParts[i].Particles.startOpacity;
                            float alphaEnd = currentModel.tailParts[i].Particles.endOpacity;

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

                            var sizeOverLifetime = tailParticleSystem.sizeOverLifetime;
                            sizeOverLifetime.enabled = true;

                            var ssss = sizeOverLifetime.size;

                            var sizeStart = currentModel.tailParts[i].Particles.startScale;
                            var sizeEnd = currentModel.tailParts[i].Particles.endScale;

                            var curve = new AnimationCurve(new Keyframe[2]
                            {
                                new Keyframe(0f, sizeStart),
                                new Keyframe(1f, sizeEnd)
                            });

                            ssss.curve = curve;

                            sizeOverLifetime.size = ssss;
                        }
                    }
                }
            }

            CreateAll();

            updated = true;
        }

        void UpdateCustomObjects()
        {
            if (customObjects.Count <= 0)
                return;

            foreach (var obj in customObjects)
            {
                var customObj = obj.Value;
                var customObject = customObj.customObject;

                if (customObject.shape.type == 9)
                    continue;

                var shape = customObj.customObject.shape;
                var pos = customObj.customObject.position;
                var sca = customObj.customObject.scale;
                var rot = customObj.customObject.rotation;

                var depth = customObj.customObject.depth;

                int s = Mathf.Clamp(shape.type, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                int so = Mathf.Clamp(shape.option, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                customObj.gameObject = ObjectManager.inst.objectPrefabs[s].options[so].Duplicate(transform);
                customObj.gameObject.transform.localScale = Vector3.one;
                customObj.gameObject.transform.localRotation = Quaternion.identity;

                var PlayerDelayTracker = customObj.gameObject.AddComponent<PlayerDelayTracker>();
                PlayerDelayTracker.offset = 0;
                PlayerDelayTracker.positionOffset = customObject.positionOffset;
                PlayerDelayTracker.scaleOffset = customObject.scaleOffset;
                PlayerDelayTracker.rotationOffset = customObject.rotationOffset;
                PlayerDelayTracker.scaleParent = customObject.scaleParent;
                PlayerDelayTracker.rotationParent = customObject.rotationParent;
                PlayerDelayTracker.player = this;

                switch (customObject.parent)
                {
                    case 0:
                        {
                            PlayerDelayTracker.leader = playerObjects["RB Parent"].gameObject.transform;
                            break;
                        }
                    case 1:
                        {
                            PlayerDelayTracker.leader = playerObjects["Boost"].gameObject.transform;
                            break;
                        }
                    case 2:
                        {
                            PlayerDelayTracker.leader = playerObjects["Boost Tail Base"].gameObject.transform;
                            break;
                        }
                    case 3:
                        {
                            PlayerDelayTracker.leader = playerObjects["Tail 1 Base"].gameObject.transform;
                            break;
                        }
                    case 4:
                        {
                            PlayerDelayTracker.leader = playerObjects["Tail 2 Base"].gameObject.transform;
                            break;
                        }
                    case 5:
                        {
                            PlayerDelayTracker.leader = playerObjects["Tail 3 Base"].gameObject.transform;
                            break;
                        }
                    case 6:
                        {
                            PlayerDelayTracker.leader = playerObjects["Face Parent"].gameObject.transform;
                            break;
                        }
                }

                var child = customObj.gameObject.transform.GetChild(0);
                child.localPosition = new Vector3(pos.x, pos.y, depth);
                child.localScale = new Vector3(sca.x, sca.y, 1f);
                child.localEulerAngles = new Vector3(0f, 0f, rot);

                customObj.gameObject.tag = "Helper";
                child.tag = "Helper";

                var renderer = customObj.gameObject.GetComponentInChildren<Renderer>();
                renderer.enabled = true;
                customObj.values["Renderer"] = renderer;

                Destroy(child.GetComponent<Collider2D>());

                if (s == 4 && child.gameObject.TryGetComponent(out TextMeshPro tmp))
                {
                    tmp.text = customObj.customObject.text;
                    customObj.values["TextMeshPro"] = tmp;
                }

                if (s == 6 && renderer is SpriteRenderer spriteRenderer)
                {
                    var path = RTFile.BasePath + customObj.customObject.text;

                    if (!RTFile.FileExists(path))
                    {
                        spriteRenderer.sprite = ArcadeManager.inst.defaultImage;
                        continue;
                    }

                    CoreHelper.StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{path}", delegate (Texture2D texture2D)
                    {
                        if (!spriteRenderer)
                            return;

                        spriteRenderer.sprite = SpriteHelper.CreateSprite(texture2D);
                    }));
                }
            }
        }

        void CreateAll()
        {
            var currentModel = PlayerModel;

            var dictionary = currentModel.customObjects;

            foreach (var obj in customObjects)
                Destroy(obj.Value.gameObject);
            customObjects.Clear();

            if (dictionary != null && dictionary.Count > 0)
                foreach (var obj in dictionary)
                {
                    var customObj = obj.Value;

                    var c = CreateCustomObject();
                    c.values["ID"] = customObj.id;
                    c.values["Shape"] = customObj.shape;
                    c.values["Position"] = customObj.position;
                    c.values["Scale"] = customObj.scale;
                    c.values["Rotation"] = customObj.rotation;
                    c.values["Color"] = customObj.color;
                    c.values["Custom Color"] = customObj.customColor;
                    c.values["Opacity"] = customObj.opacity;
                    c.values["Parent"] = customObj.parent;
                    c.values["Parent Position Offset"] = customObj.positionOffset;
                    c.values["Parent Scale Offset"] = customObj.scaleOffset;
                    c.values["Parent Rotation Offset"] = customObj.rotationOffset;
                    c.values["Parent Scale Active"] = customObj.scaleParent;
                    c.values["Parent Rotation Active"] = customObj.rotationParent;
                    c.values["Depth"] = customObj.depth;
                    c.customObject = customObj;

                    customObjects.Add(customObj.id, c);
                }

            UpdateCustomObjects();
        }

        public CustomGameObject CreateCustomObject()
        {
            var obj = new CustomGameObject();

            obj.name = "Object";
            obj.values = new Dictionary<string, object>();
            obj.values.Add("Shape", ShapeManager.inst.Shapes2D[0][0]);
            obj.values.Add("Position", new Vector2(0f, 0f));
            obj.values.Add("Scale", new Vector2(1f, 1f));
            obj.values.Add("Rotation", 0f);
            obj.values.Add("Color", 0);
            obj.values.Add("Custom Color", "FFFFFF");
            obj.values.Add("Opacity", 0f);
            obj.values.Add("Parent", 0);
            obj.values.Add("Parent Position Offset", 1f);
            obj.values.Add("Parent Scale Offset", 1f);
            obj.values.Add("Parent Rotation Offset", 1f);
            obj.values.Add("Parent Scale Active", false);
            obj.values.Add("Parent Rotation Active", true);
            obj.values.Add("Depth", 0f);
            obj.values.Add("Renderer", null);
            var id = LSText.randomNumString(16);
            obj.values.Add("ID", id);

            return obj;
        }

        void UpdateCustomTheme()
        {
            if (customObjects.Count > 0)
                foreach (var obj in customObjects.Values)
                {
                    UpdateVisibility(obj);

                    if (!obj.gameObject.activeSelf)
                        continue;

                    int col = obj.customObject.color;
                    string hex = obj.customObject.customColor;
                    float alpha = obj.customObject.opacity;

                    if (obj.values.TryGetValue("TextMeshPro", out object tmpObj) && tmpObj is TextMeshPro tmp)
                        tmp.color = GetColor(col, alpha, hex);
                    else if (obj.values["Renderer"] is Renderer renderer)
                        renderer.material.color = GetColor(col, alpha, hex);
                }
        }

        public void UpdateVisibility(CustomGameObject customGameObject)
        {
            if (customGameObject.gameObject != null)
            {
                Func<PlayerModel.CustomObject.Visiblity, bool> visibilityFunc = (x =>
                {
                    return
                    x.command == "isBoosting" && (!x.not && isBoosting || x.not && !isBoosting) ||
                    x.command == "isTakingHit" && (!x.not && isTakingHit || x.not && !isTakingHit) ||
                    x.command == "isZenMode" && (!x.not && PlayerManager.Invincible || x.not && !PlayerManager.Invincible) ||
                    x.command == "isHealthPercentageGreater" && (!x.not && (float)CustomPlayer.health / (float)initialHealthCount * 100f >= x.value || x.not && (float)CustomPlayer.health / (float)initialHealthCount * 100f < x.value) ||
                    x.command == "isHealthGreaterEquals" && (!x.not && CustomPlayer.health >= x.value || x.not && CustomPlayer.health < x.value) ||
                    x.command == "isHealthEquals" && (!x.not && CustomPlayer.health == x.value || x.not && CustomPlayer.health != x.value) ||
                    x.command == "isHealthGreater" && (!x.not && CustomPlayer.health > x.value || x.not && CustomPlayer.health <= x.value) ||
                    x.command == "isPressingKey" && (!x.not && Input.GetKey(GetKeyCode((int)x.value)) || x.not && !Input.GetKey(GetKeyCode((int)x.value)));
                });

                customGameObject.gameObject.SetActive(customGameObject.customObject.visibilitySettings.Count < 1 && customGameObject.customObject.active || customGameObject.customObject.visibilitySettings.Count > 0 &&
                    (!customGameObject.customObject.requireAll && customGameObject.customObject.visibilitySettings.Any(visibilityFunc) ||
                customGameObject.customObject.visibilitySettings.All(visibilityFunc)));
            }
        }

        public void UpdateTail(int _health, Vector3 _pos)
        {
            if (_health > initialHealthCount)
            {
                initialHealthCount = _health;
                var tailParent = playerObjects["Tail Parent"].gameObject.transform;

                if (tailGrows)
                {
                    var t = path[path.Count - 2].transform.gameObject.Duplicate(tailParent);
                    t.transform.SetParent(tailParent);
                    t.transform.localScale = Vector3.one;
                    t.name = $"Tail {path.Count - 2}";

                    path.Insert(path.Count - 2, new MovementPath(t.transform.localPosition, t.transform.localRotation, t.transform));
                }
            }

            for (int i = 2; i < path.Count; i++)
            {
                if (!path[i].transform)
                    continue;

                var inactive = i - 1 > _health;

                if (path[i].transform.childCount != 0)
                    path[i].transform.GetChild(0).gameObject.SetActive(!inactive);
                else
                    path[i].transform.gameObject.SetActive(!inactive);
            }

            if (!PlayerModel)
                return;

            var currentModel = PlayerModel;

            if (healthObjects.Count > 0)
                for (int i = 0; i < healthObjects.Count; i++)
                    healthObjects[i].gameObject.SetActive(i < _health && currentModel.guiPart.active && currentModel.guiPart.mode == PlayerModel.GUI.GUIHealthMode.Images);

            var text = health.GetComponent<Text>();
            if (currentModel.guiPart.active && (currentModel.guiPart.mode == PlayerModel.GUI.GUIHealthMode.Text || currentModel.guiPart.mode == PlayerModel.GUI.GUIHealthMode.EqualsBar))
            {
                text.enabled = true;
                if (currentModel.guiPart.mode == PlayerModel.GUI.GUIHealthMode.Text)
                    text.text = _health.ToString();
                else
                    text.text = FontManager.TextTranslater.ConvertHealthToEquals(_health, initialHealthCount);
            }
            else
                text.enabled = false;

            if (currentModel.guiPart.active && currentModel.guiPart.mode == PlayerModel.GUI.GUIHealthMode.Bar)
            {
                barBaseIm.gameObject.SetActive(true);
                var e = (float)_health / (float)initialHealthCount;
                barRT.sizeDelta = new Vector2(200f * e, 32f);
            }
            else
                barBaseIm.gameObject.SetActive(false);
        }

        public Color GetColor(int col, float alpha, string hex)
            => LSColors.fadeColor(col >= 0 && col < 4 ? CoreHelper.CurrentBeatmapTheme.playerColors[col] : col == 4 ? CoreHelper.CurrentBeatmapTheme.guiColor : col > 4 && col < 23 ? CoreHelper.CurrentBeatmapTheme.objectColors[col - 5] :
                col == 23 ? CoreHelper.CurrentBeatmapTheme.playerColors[playerIndex % 4] : col == 24 ? LSColors.HexToColor(hex) : col == 25 ? CoreHelper.CurrentBeatmapTheme.guiAccentColor : LSColors.pink500, alpha);

        #endregion

        #region Actions

        void CreatePulse()
        {
            if (!PlayerModel)
                return;

            var currentModel = PlayerModel;

            if (!currentModel.pulsePart.active)
                return;

            var player = playerObjects["RB Parent"].gameObject;

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

            var obj = new PlayerObject("Pulse", pulse.transform.GetChild(0).gameObject);

            MeshRenderer pulseRenderer = pulse.transform.GetChild(0).GetComponent<MeshRenderer>();
            obj.values.Add("MeshRenderer", pulseRenderer);
            obj.values.Add("Opacity", 0f);
            obj.values.Add("ColorTween", 0f);
            obj.values.Add("StartColor", currentModel.pulsePart.startColor);
            obj.values.Add("EndColor", currentModel.pulsePart.endColor);
            obj.values.Add("StartCustomColor", currentModel.pulsePart.startCustomColor);
            obj.values.Add("EndCustomColor", currentModel.pulsePart.endCustomColor);

            boosts.Add(obj);

            pulseRenderer.enabled = true;
            pulseRenderer.material = ((MeshRenderer)playerObjects["Head"].values["MeshRenderer"]).material;
            pulseRenderer.material.shader = ((MeshRenderer)playerObjects["Head"].values["MeshRenderer"]).material.shader;
            Color colorBase = ((MeshRenderer)playerObjects["Head"].values["MeshRenderer"]).material.color;

            int easingPos = currentModel.pulsePart.easingPosition;
            int easingSca = currentModel.pulsePart.easingScale;
            int easingRot = currentModel.pulsePart.easingRotation;
            int easingOpa = currentModel.pulsePart.easingOpacity;
            int easingCol = currentModel.pulsePart.easingColor;

            float duration = Mathf.Clamp(currentModel.pulsePart.duration, 0.001f, 20f) / CoreHelper.ForwardPitch;

            pulse.transform.GetChild(0).DOLocalMove(new Vector3(currentModel.pulsePart.endPosition.x, currentModel.pulsePart.endPosition.y, currentModel.pulsePart.depth), duration).SetEase(DataManager.inst.AnimationList[easingPos].Animation);
            var tweenScale = pulse.transform.DOScale(new Vector3(currentModel.pulsePart.endScale.x, currentModel.pulsePart.endScale.y, 1f), duration).SetEase(DataManager.inst.AnimationList[easingSca].Animation);
            pulse.transform.GetChild(0).DOLocalRotate(new Vector3(0f, 0f, currentModel.pulsePart.endRotation), duration).SetEase(DataManager.inst.AnimationList[easingRot].Animation);

            DOTween.To(delegate (float x)
            {
                obj.values["Opacity"] = x;
            }, currentModel.pulsePart.startOpacity, currentModel.pulsePart.endOpacity, duration).SetEase(DataManager.inst.AnimationList[easingOpa].Animation);
            DOTween.To(delegate (float x)
            {
                obj.values["ColorTween"] = x;
            }, 0f, 1f, duration).SetEase(DataManager.inst.AnimationList[easingCol].Animation);

            tweenScale.OnComplete(delegate ()
            {
                Destroy(pulse);
                boosts.Remove(obj);
            });
        }

        void UpdateBoostTheme()
        {
            if (boosts.Count < 1)
                return;

            foreach (var boost in boosts)
            {
                if (boost == null)
                    continue;

                int startCol = (int)boost.values["StartColor"];
                int endCol = (int)boost.values["EndColor"];

                var startHex = (string)boost.values["StartCustomColor"];
                var endHex = (string)boost.values["EndCustomColor"];

                float alpha = (float)boost.values["Opacity"];
                float colorTween = (float)boost.values["ColorTween"];

                Color startColor = GetColor(startCol, alpha, startHex);
                Color endColor = GetColor(endCol, alpha, endHex);

                if (((MeshRenderer)boost.values["MeshRenderer"]) != null)
                {
                    ((MeshRenderer)boost.values["MeshRenderer"]).material.color = Color.Lerp(startColor, endColor, colorTween);
                }
            }
        }

        public List<PlayerObject> boosts = new List<PlayerObject>();

        void PlaySound(AudioClip _clip, float pitch = 1f)
        {
            float p = pitch * CoreHelper.ForwardPitch;

            var audioSource = Camera.main.gameObject.AddComponent<AudioSource>();
            audioSource.clip = _clip;
            audioSource.playOnAwake = true;
            audioSource.loop = false;
            audioSource.volume = AudioManager.inst.sfxVol;
            audioSource.pitch = pitch;
            audioSource.Play();
            StartCoroutine(AudioManager.inst.DestroyWithDelay(audioSource, _clip.length / p));
        }

        // to do: aiming so you don't need to be facing the direction of the bullet
        void CreateBullet()
        {
            var currentModel = PlayerModel;

            if (currentModel == null || !currentModel.bulletPart.active)
                return;

            if (PlayShootSound)
                PlaySound(AudioManager.inst.GetSound("boost"), 0.7f);

            canShoot = false;

            var player = playerObjects["RB Parent"].gameObject;

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

            var obj = new PlayerObject("Bullet", pulse.transform.GetChild(0).gameObject);

            MeshRenderer pulseRenderer = pulse.transform.GetChild(0).GetComponent<MeshRenderer>();
            obj.values.Add("MeshRenderer", pulseRenderer);
            obj.values.Add("Opacity", 0f);
            obj.values.Add("ColorTween", 0f);
            obj.values.Add("StartColor", currentModel.bulletPart.startColor);
            obj.values.Add("EndColor", currentModel.bulletPart.endColor);
            obj.values.Add("StartCustomColor", currentModel.bulletPart.startCustomColor);
            obj.values.Add("EndCustomColor", currentModel.bulletPart.endCustomColor);

            boosts.Add(obj);

            pulseRenderer.enabled = true;
            pulseRenderer.material = ((MeshRenderer)playerObjects["Head"].values["MeshRenderer"]).material;
            pulseRenderer.material.shader = ((MeshRenderer)playerObjects["Head"].values["MeshRenderer"]).material.shader;
            Color colorBase = ((MeshRenderer)playerObjects["Head"].values["MeshRenderer"]).material.color;

            var collider2D = pulse.transform.GetChild(0).GetComponent<Collider2D>();
            collider2D.enabled = true;
            //collider2D.isTrigger = false;

            var rb2D = pulse.transform.GetChild(0).gameObject.AddComponent<Rigidbody2D>();
            rb2D.gravityScale = 0f;

            var bulletCollider = pulse.transform.GetChild(0).gameObject.AddComponent<BulletCollider>();
            bulletCollider.rb = (Rigidbody2D)playerObjects["RB Parent"].values["Rigidbody2D"];
            bulletCollider.kill = currentModel.bulletPart.autoKill;
            bulletCollider.player = this;
            bulletCollider.playerObject = obj;

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

            DOTween.To(delegate (float x)
            {
                obj.values["Opacity"] = x;
            }, currentModel.bulletPart.startOpacity, currentModel.bulletPart.endOpacity, posDuration).SetEase(DataManager.inst.AnimationList[easingOpa].Animation);
            DOTween.To(delegate (float x)
            {
                obj.values["ColorTween"] = x;
            }, 0f, 1f, posDuration).SetEase(DataManager.inst.AnimationList[easingCol].Animation);

            StartCoroutine(CanShoot());

            var tweener = DOTween.To(delegate (float x) { }, 1f, 1f, lifeTime).SetEase(DataManager.inst.AnimationList[easingOpa].Animation);
            bulletCollider.tweener = tweener;

            tweener.OnComplete(delegate ()
            {
                var tweenScale = pulse.transform.GetChild(0).DOScale(Vector3.zero, 0.2f).SetEase(DataManager.inst.AnimationList[2].Animation);
                bulletCollider.tweener = tweenScale;

                tweenScale.OnComplete(delegate ()
                {
                    Destroy(pulse);
                    boosts.Remove(obj);
                    obj = null;
                });
            });
        }

        IEnumerator CanShoot()
        {
            var currentModel = PlayerModel;
            if (currentModel != null)
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

        public Dictionary<string, PlayerObject> playerObjects = new Dictionary<string, PlayerObject>();
        public Dictionary<string, CustomGameObject> customObjects = new Dictionary<string, CustomGameObject>();

        public PlayerPart head;
        public PlayerPart boost;
        public PlayerPart boostTail;

        public List<PlayerPart> tailParts = new List<PlayerPart>();

        public class PlayerPart
        {
            public GameObject GameObject { get; set; }
            public Transform Transform { get; set; }
            public MeshRenderer MeshRenderer { get; set; }
            public MeshFilter MeshFilter { get; set; }
        }

        public class PlayerObject
        {
            public PlayerObject()
            {

            }

            public PlayerObject(string _name, GameObject _gm)
            {
                name = _name;
                gameObject = _gm;
                values = new Dictionary<string, object>();

                values.Add("Position", Vector3.zero);
                values.Add("Scale", Vector3.one);
                values.Add("Rotation", 0f);
                values.Add("Color", 0);
            }

            public PlayerObject(string _name, Dictionary<string, object> _values, GameObject _gm)
            {
                name = _name;
                values = _values;
                gameObject = _gm;
            }

            public string name;
            public GameObject gameObject;

            public Dictionary<string, object> values;
        }

        public class CustomGameObject : PlayerObject
        {
            public CustomGameObject() : base()
            {

            }

            public CustomGameObject(string name, GameObject gm) : base(name, gm)
            {

            }

            public CustomGameObject(string name, Dictionary<string, object> values, GameObject gm) : base(name, values, gm)
            {

            }

            public PlayerModel.CustomObject customObject;
        }

        public List<MovementPath> path = new List<MovementPath>();

        public class MovementPath
        {
            public MovementPath(Vector3 _pos, Quaternion _rot, Transform _tf)
            {
                pos = _pos;
                rot = _rot;
                transform = _tf;
            }

            public MovementPath(Vector3 _pos, Quaternion _rot, Transform _tf, bool active)
            {
                pos = _pos;
                rot = _rot;
                transform = _tf;
                this.active = active;
            }

            public bool active = true;

            public Vector3 lastPos;
            public Vector3 pos;

            public Quaternion rot;

            public Transform transform;
        }

        public List<HealthObject> healthObjects = new List<HealthObject>();

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
