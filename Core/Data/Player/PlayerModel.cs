using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Player
{
    /// <summary>
    /// Represents the player a player displays in-game.
    /// </summary>
    public class PlayerModel : PAObject<PlayerModel>, IPacket, IModifyable, IUploadable
    {
        public PlayerModel()
        {
            basePart = new Base();
            guiPart = new GUI();
            headPart = new PlayerObject();
            boostPart = new PlayerObject();
            boostPart.color = 25;
            pulsePart = new Pulse();
            bulletPart = new Bullet();
            tailBase = new TailBase();
            boostTailPart = new PlayerObject();
            boostTailPart.color = 25;
            boostTailPart.active = false;

            tailParts = GetDefaultTail();
        }

        #region Values
        
        #region Default Models

        public const string DEFAULT_ID = "0";
        public const string CIRCLE_ID = "1";
        public const string ALPHA_ID = "2";
        public const string BETA_ID = "3";
        public const string DEV_ID = "4";

        /// <summary>
        /// List of all default player models.
        /// </summary>
        public static List<PlayerModel> DefaultModels
        {
            get
            {
                if (defaultModels == null)
                    defaultModels = new List<PlayerModel>
                    {
                        DefaultPlayer,
                        CirclePlayer,
                        AlphaPlayer,
                        BetaPlayer,
                        DevPlayer
                    };

                return defaultModels;
            }
        }

        /// <summary>
        /// The default Legacy player.
        /// </summary>
        public static PlayerModel DefaultPlayer
        {
            get
            {
                if (!defaultPlayer)
                {
                    defaultPlayer = new PlayerModel();
                    defaultPlayer.IsDefault = true;
                    defaultPlayer.basePart.id = DEFAULT_ID;
                    defaultPlayer.basePart.name = "Regular";
                }

                return defaultPlayer;
            }
        }

        /// <summary>
        /// The extra Legacy player. Based on the 5-8 players.
        /// </summary>
        public static PlayerModel CirclePlayer
        {
            get
            {
                if (!circlePlayer)
                {
                    circlePlayer = new PlayerModel();
                    circlePlayer.IsDefault = true;
                    circlePlayer.basePart.id = CIRCLE_ID;
                    circlePlayer.basePart.name = "Circle";
                    circlePlayer.headPart.shape = 1;
                    circlePlayer.boostPart.shape = 1;
                    circlePlayer.pulsePart.shape = 1;
                    circlePlayer.bulletPart.shape = 1;
                    circlePlayer.boostTailPart.shape = 1;

                    for (int i = 0; i < circlePlayer.tailParts.Count; i++)
                        circlePlayer.tailParts[i].shape = 1;
                }

                return circlePlayer;
            }
        }

        /// <summary>
        /// The original player from Arrhythmia.
        /// </summary>
        public static PlayerModel AlphaPlayer
        {
            get
            {
                if (!alphaPlayer)
                {
                    alphaPlayer = new PlayerModel();
                    alphaPlayer.IsDefault = true;
                    alphaPlayer.basePart.id = ALPHA_ID;
                    alphaPlayer.basePart.name = "Alpha";
                    alphaPlayer.basePart.canBoost = false;
                    alphaPlayer.guiPart.active = true;
                    alphaPlayer.guiPart.mode = GUI.GUIHealthMode.Images;
                    alphaPlayer.headPart.scale = new Vector2(1.1f, 1f);
                    alphaPlayer.headPart.Trail.emitting = true;
                    alphaPlayer.headPart.Trail.time = 0.3f;
                    alphaPlayer.headPart.Trail.startWidth = 1f;
                    alphaPlayer.headPart.Trail.endWidth = 1f;

                    for (int i = 0; i < alphaPlayer.tailParts.Count; i++)
                    {
                        alphaPlayer.tailParts[i].active = false;
                        alphaPlayer.tailParts[i].Trail.emitting = false;
                    }
                }

                return alphaPlayer;
            }
        }

        /// <summary>
        /// The pre-Legacy player with a boost tail part.
        /// </summary>
        public static PlayerModel BetaPlayer
        {
            get
            {
                if (!betaPlayer)
                {
                    betaPlayer = new PlayerModel();
                    betaPlayer.IsDefault = true;
                    betaPlayer.basePart.id = BETA_ID;
                    betaPlayer.basePart.name = "Beta";
                    betaPlayer.basePart.moveSpeed = 28f;
                    betaPlayer.basePart.boostSpeed = 95f;
                    betaPlayer.basePart.boostCooldown = 0.11f;
                    betaPlayer.basePart.maxBoostTime = 0.16f;
                    betaPlayer.boostTailPart.active = true;
                    betaPlayer.boostTailPart.rotation = 45f;

                    for (int i = 0; i < betaPlayer.tailParts.Count; i++)
                    {
                        betaPlayer.tailParts[i].scale = new Vector2(0.6f, 0.6f);
                        betaPlayer.tailParts[i].Trail.startWidth = 0.6f;
                        betaPlayer.tailParts[i].Trail.endWidth = 0.5f;
                    }
                }

                return betaPlayer;
            }
        }

        /// <summary>
        /// The modern VG player.
        /// </summary>
        public static PlayerModel DevPlayer
        {
            get
            {
                if (!devPlayer)
                {
                    devPlayer = new PlayerModel();
                    devPlayer.IsDefault = true;
                    devPlayer.basePart.id = DEV_ID;
                    devPlayer.basePart.name = "DevPlus";
                    devPlayer.tailBase.mode = TailBase.TailMode.DevPlus;
                    devPlayer.basePart.moveSpeed = 22f;
                    devPlayer.basePart.boostCooldown = 0f;
                }

                return devPlayer;
            }
        }

        /// <summary>
        /// Gets the default tail parts list.
        /// </summary>
        /// <returns>Returns the default player tail.</returns>
        public static List<PlayerObject> GetDefaultTail()
        {
            var tailParts = new List<PlayerObject>(4);
            float t = 0.5f;
            for (int i = 0; i < 3; i++)
            {
                var tail = new PlayerObject();
                tail.Trail.emitting = true;
                tail.color = 25;
                tail.scale = new Vector2(t, t);
                tail.Trail.time = 0.075f;
                tail.Trail.startWidth = t;
                tail.Trail.endWidth = t / 2f;
                tail.Trail.startColor = 25;
                tail.Trail.endColor = 25;

                // 1 Trail Time = 0.075
                // 1 Trail Start Width = 0.5
                // 1 Trail End Width = 0.3

                // 2 Trail Time = 0.075
                // 2 Trail Start Width = 0.4
                // 2 Trail End Width = 0.2

                // 3 Trail Time = 0.075
                // 3 Trail Start Width = 0.3
                // 3 Trail End Width = 0.1

                tailParts.Add(tail);
                t -= 0.1f;
            }
            return tailParts;
        }

        public const string IDLE_ANIM = "idle";
        public const string SPAWN_ANIM = "spawn";
        public const string BOOST_ANIM = "boost";
        public const string HEAL_ANIM = "heal";
        public const string HIT_ANIM = "hit";
        public const string DEATH_ANIM = "death";
        public const string SHOOT_ANIM = "shoot";
        public const string JUMP_ANIM = "jump";

        #region Internal

        static List<PlayerModel> defaultModels;
        static PlayerModel defaultPlayer;
        static PlayerModel circlePlayer;
        static PlayerModel alphaPlayer;
        static PlayerModel betaPlayer;
        static PlayerModel devPlayer;

        #endregion

        #endregion

        public Version Version { get; set; } = LegacyPlugin.ModVersion;
        public bool needsUpdate;

        public string creator;

        public bool IsDefault { get; set; }

        public Assets assets = new Assets();

        public Base basePart;

        public GUI guiPart;

        public PlayerObject headPart;

        public Vector2 facePosition = new Vector2(0.3f, 0f);

        public bool faceControlActive = true;

        public PlayerObject boostPart;

        public Pulse pulsePart;

        public Bullet bulletPart;

        public PlayerObject boostTailPart;

        public TailBase tailBase;

        public List<PlayerObject> tailParts = new List<PlayerObject>();

        public List<CustomPlayerObject> customObjects = new List<CustomPlayerObject>();

        public PlayerModelPanel editorPanel;

        /// <summary>
        /// Data of the icon.
        /// </summary>
        public string iconData;
        /// <summary>
        /// Icon of the model.
        /// </summary>
        public Sprite icon;

        #region Modifiers

        public ModifierReferenceType ReferenceType => ModifierReferenceType.PlayerModel;

        public List<string> Tags { get; set; } = new List<string>();

        public List<Modifier> modifiers = new List<Modifier>();

        public List<Modifier> Modifiers { get => modifiers; set => modifiers = value; }

        public bool IgnoreLifespan { get; set; }

        public bool OrderModifiers { get; set; } = true;

        public int IntVariable { get; set; }

        public bool ModifiersActive => false;

        #endregion

        #region Server

        public string ServerID { get; set; }

        public string UploaderName { get; set; }

        public string UploaderID { get; set; }

        public List<ServerUser> Uploaders { get; set; } = new List<ServerUser>();

        public ServerVisibility Visibility { get; set; }

        public string Changelog { get; set; }

        public List<string> ArcadeTags { get; set; } = new List<string>();

        public string ObjectVersion { get; set; }

        public string dateCreated = string.Empty;
        public string dateEdited = DateTime.Now.ToString(LegacyPlugin.DATE_TIME_FORMAT);
        public string datePublished = string.Empty;
        public int versionNumber;

        public string DatePublished { get => datePublished; set => datePublished = value; }

        public int VersionNumber { get => versionNumber; set => versionNumber = value; }

        #endregion

        #endregion

        #region Functions

        public override void CopyData(PlayerModel orig, bool newID = true)
        {
            creator = orig.creator;

            dateCreated = orig.dateCreated;
            dateEdited = orig.dateEdited;
            datePublished = orig.datePublished;
            versionNumber = orig.versionNumber;

            faceControlActive = orig.faceControlActive;
            facePosition = orig.facePosition;

            assets.CopyData(orig.assets);
            basePart.CopyData(orig.basePart, newID);
            guiPart.CopyData(orig.guiPart);
            headPart.CopyData(orig.headPart);
            boostPart.CopyData(orig.boostPart);
            pulsePart.CopyData(orig.pulsePart);
            bulletPart.CopyData(orig.bulletPart);
            tailBase.CopyData(orig.tailBase);
            boostTailPart.CopyData(orig.boostTailPart);
            tailParts.Clear();
            for (int i = 0; i < orig.tailParts.Count; i++)
                tailParts.Add(orig.tailParts[i].Copy(false));
            customObjects.Clear();
            for (int i = 0; i < orig.customObjects.Count; i++)
                customObjects.Add(orig.customObjects[i].Copy(false));
            this.CopyModifyableData(orig);
            this.CopyUploadableData(orig);

            iconData = orig.iconData;
            if (string.IsNullOrEmpty(iconData) && orig.icon)
                iconData = SpriteHelper.SpriteToString(orig.icon);
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (!string.IsNullOrEmpty(jn["version"]))
                Version = new Version(jn["version"]);
            else
                needsUpdate = true;

            creator = jn["creator"];

            if (!string.IsNullOrEmpty(jn["date_edited"]))
                dateEdited = jn["date_edited"];
            if (!string.IsNullOrEmpty(jn["date_created"]))
                dateCreated = jn["date_created"];
            else
                dateCreated = DateTime.Now.ToString(LegacyPlugin.DATE_TIME_FORMAT);
            if (!string.IsNullOrEmpty(jn["date_published"]))
                datePublished = jn["date_published"];
            if (jn["version_number"] != null)
                versionNumber = jn["version_number"].AsInt;

            this.ReadUploadableJSON(jn);

            if (jn["assets"] != null)
                assets.ReadJSON(jn["assets"]);
            
            basePart.ReadJSON(jn["base"]);
            if (jn["stretch"] != null)
            {
                if (jn["stretch"]["active"] != null)
                    basePart.stretchActive = jn["active"].AsBool;
                if (jn["stretch"]["amount"] != null)
                    basePart.stretchAmount = jn["amount"].AsFloat;
                if (jn["stretch"]["easing"] != null)
                    basePart.stretchEasing = jn["easing"].AsInt;
            }
            guiPart.ReadJSON(jn["gui"]);
            headPart.ReadJSON(jn["head"]);
            if (jn["face"] != null)
            {
                if (jn["face"]["position"]["x"] != null)
                    facePosition.x = jn["face"]["position"]["x"].AsFloat;
                if (jn["face"]["position"]["y"] != null)
                    facePosition.y = jn["face"]["position"]["y"].AsFloat;
                if (jn["face"]["con_active"] != null)
                    faceControlActive = jn["face"]["con_active"].AsBool;
            }
            boostPart.ReadJSON(jn["boost"]);
            pulsePart.ReadJSON(jn["pulse"]);
            bulletPart.ReadJSON(jn["bullet"]);
            tailBase.ReadJSON(jn["tail_base"]);
            boostTailPart.ReadJSON(jn["tail_boost"]);

            if (jn["tail"] != null && jn["tail"].Count > 0)
            {
                tailParts.Clear();
                for (int i = 0; i < jn["tail"].Count; i++)
                    tailParts.Add(PlayerObject.Parse(jn["tail"][i]));
            }
            else
                tailParts = GetDefaultTail();

            this.ReadModifiersJSON(jn);
            if (!modifiers.IsEmpty())
                this.UpdateFunctions();

            if (jn["custom_objects"] != null && jn["custom_objects"].Count > 0)
                for (int i = 0; i < jn["custom_objects"].Count; i++)
                {
                    var customObject = CustomPlayerObject.Parse(jn["custom_objects"][i]);
                    if (!string.IsNullOrEmpty(customObject.id))
                        customObjects.Add(customObject);
                }

            iconData = jn["icon"];

            needsUpdate = false;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["version"] = Version.ToString();

            if (!string.IsNullOrEmpty(creator))
                jn["creator"] = creator;

            jn["date_created"] = dateCreated;
            jn["date_edited"] = DateTime.Now.ToString(LegacyPlugin.DATE_TIME_FORMAT);
            if (!string.IsNullOrEmpty(datePublished))
                jn["date_published"] = datePublished;
            if (versionNumber != 0)
                jn["version_number"] = versionNumber;

            this.WriteUploadableJSON(jn);

            if (assets && !assets.IsEmpty())
                jn["assets"] = assets.ToJSON();

            if (basePart)
                jn["base"] = basePart.ToJSON();
            if (guiPart && guiPart.ShouldSerialize)
                jn["gui"] = guiPart.ToJSON();
            if (facePosition.x != 0.3f || facePosition.y != 0f)
                jn["face"]["position"] = facePosition.ToJSON();
            if (!faceControlActive)
                jn["face"]["con_active"] = faceControlActive;
            if (headPart)
                jn["head"] = headPart.ToJSON();
            if (boostPart)
                jn["boost"] = boostPart.ToJSON();
            if (pulsePart)
                jn["pulse"] = pulsePart.ToJSON();
            if (bulletPart)
                jn["bullet"] = bulletPart.ToJSON();
            if (tailBase && tailBase.ShouldSerialize)
                jn["tail_base"] = tailBase.ToJSON();
            if (boostTailPart)
                jn["tail_boost"] = boostTailPart.ToJSON();

            if (tailParts != null)
                for (int i = 0; i < tailParts.Count; i++)
                    jn["tail"][i] = tailParts[i].ToJSON();

            this.WriteModifiersJSON(jn);

            if (customObjects != null && !customObjects.IsEmpty())
                for (int i = 0; i < customObjects.Count; i++)
                    jn["custom_objects"][i] = customObjects[i].ToJSON();

            if (icon)
                jn["icon"] = SpriteHelper.SpriteToString(icon);
            else if (!string.IsNullOrEmpty(iconData))
                jn["icon"] = iconData;

            return jn;
        }

        public void ReadPacket(NetworkReader reader)
        {
            #region Interface

            this.ReadUploadablePacket(reader);
            this.ReadModifiersPacket(reader);

            #endregion

            Version = new Version(reader.ReadString());
            creator = reader.ReadString();

            dateCreated = reader.ReadString();
            dateEdited = reader.ReadString();

            facePosition = reader.ReadVector2();
            faceControlActive = reader.ReadBoolean();

            assets = Packet.CreateFromPacket<Assets>(reader);
            basePart = Packet.CreateFromPacket<Base>(reader);
            guiPart = Packet.CreateFromPacket<GUI>(reader);
            try
            {
                headPart = Packet.CreateFromPacket<PlayerObject>(reader);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to create the head from a packet due to the exception: {ex}");
            }
            try
            {
                boostPart = Packet.CreateFromPacket<PlayerObject>(reader);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to create the boost from a packet due to the exception: {ex}");
            }
            pulsePart = Packet.CreateFromPacket<Pulse>(reader);
            bulletPart = Packet.CreateFromPacket<Bullet>(reader);
            tailBase = Packet.CreateFromPacket<TailBase>(reader);
            try
            {
                boostTailPart = Packet.CreateFromPacket<PlayerObject>(reader);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to create the boost tail from a packet due to the exception: {ex}");
            }
            Packet.ReadPacketList(tailParts, reader);
            Packet.ReadPacketList(customObjects, reader);
            icon = reader.ReadSprite();
        }

        public void WritePacket(NetworkWriter writer)
        {
            #region Interface

            this.WriteUploadablePacket(writer);
            this.WriteModifiersPacket(writer);

            #endregion

            writer.Write(Version.ToString());
            writer.Write(creator);

            writer.Write(dateCreated);
            writer.Write(dateEdited);

            writer.Write(facePosition);
            writer.Write(faceControlActive);

            assets.WritePacket(writer);
            basePart.WritePacket(writer);
            guiPart.WritePacket(writer);
            headPart.WritePacket(writer);
            boostPart.WritePacket(writer);
            pulsePart.WritePacket(writer);
            bulletPart.WritePacket(writer);
            tailBase.WritePacket(writer);
            boostTailPart.WritePacket(writer);
            Packet.WritePacketList(tailParts, writer);
            Packet.WritePacketList(customObjects, writer);
            writer.Write(icon);
        }

        /// <summary>
        /// Gets a tail part at an index.
        /// </summary>
        /// <param name="index">Index of the tail part to get.</param>
        /// <returns>Returns a tail part from the model.</returns>
        public PlayerObject GetTail(int index) => tailParts[Mathf.Clamp(index, 0, tailParts.Count - 1)];

        /// <summary>
        /// Adds a tail to the tail parts list. The new tail is a copy of the last.
        /// </summary>
        public void AddTail() => tailParts.Add(tailParts.Last().Copy());

        /// <summary>
        /// Removes a tail at an index.
        /// </summary>
        /// <param name="index">Index to remove a tail from.</param>
        public void RemoveTail(int index)
        {
            // 2 because tail parts shouldn't be removed fully
            if (tailParts.Count > 2 && tailParts.InRange(index))
                tailParts.RemoveAt(index);
        }

        /// <summary>
        /// Gets a player control from the model.
        /// </summary>
        /// <returns>Returns a player control based on the models' values.</returns>
        public PlayerControl ToPlayerControl() => new PlayerControl
        {
            Health = basePart.health,
            lives = basePart.lives,
            moveSpeed = basePart.moveSpeed,
            boostSpeed = basePart.boostSpeed,
            boostCooldown = basePart.boostCooldown,
            minBoostTime = basePart.minBoostTime,
            maxBoostTime = basePart.maxBoostTime,
            jumpGravity = basePart.jumpGravity,
            jumpIntensity = basePart.jumpIntensity,
            bounciness = basePart.bounciness,
            jumpCount = basePart.jumpCount,
            jumpBoostCount = basePart.jumpBoostCount,
            airBoostOnly = basePart.airBoostOnly,
            hitCooldown = basePart.hitCooldown,
            collisionAccurate = basePart.collisionAccurate,
            sprintSneakActive = basePart.sprintSneakActive,
            sprintSpeed = basePart.sprintSpeed,
            sneakSpeed = basePart.sneakSpeed,
            canBoost = basePart.canBoost,
        };

        /// <summary>
        /// Loads and gets the models' icon.
        /// </summary>
        public Sprite GetIcon()
        {
            if (icon)
                return icon;

            if (string.IsNullOrEmpty(iconData))
                return null;

            icon = SpriteHelper.StringToSprite(iconData);
            return icon;
        }

        public override string ToString() => basePart ? basePart.name : base.ToString();

        #endregion

        #region Sub classes

        public class Base : PAObject<Base>, IPacket
        {
            public Base() { }

            #region Values

            public string name;

            #region Visual

            public Easing rotationCurveType = Easing.OutCirc;

            public float rotationSpeed = 0.2f;

            public BaseRotateMode rotateMode = BaseRotateMode.RotateToDirection;

            public bool stretchActive = false;

            public float stretchAmount = 0.4f;

            public int stretchEasing = 6;

            public PAAnimation boostAnimation;
            public PAAnimation hitAnimation;

            #endregion

            #region Control

            #region Main

            public int health = 3;

            public int lives = -1;

            public float hitCooldown = 2.5f;

            public bool canBoost = true;

            public bool collisionAccurate = false;

            #endregion

            #region Move

            public float moveSpeed = 20f;

            public float boostSpeed = 85f;

            public float boostCooldown = 0.1f;

            public float minBoostTime = 0.07f;

            public float maxBoostTime = 0.18f;

            public bool sprintSneakActive = false;

            public float sprintSpeed = 1.3f;

            public float sneakSpeed = 0.1f;

            #endregion

            #region Jump

            public float jumpGravity = 10f;

            public float jumpIntensity = 40f;

            public float bounciness = 0.1f;

            public int jumpCount = 1;
            public int jumpBoostCount = 1;

            public bool airBoostOnly = false;

            #endregion

            #endregion

            public enum BaseRotateMode
            {
                RotateToDirection,
                None,
                FlipX,
                FlipY,
                RotateReset,
                RotateFlipX,
                RotateFlipY
            }

            #endregion

            #region Functions

            public override void CopyData(Base orig, bool newID = true)
            {
                name = orig.name;
                id = newID ? GetNumberID() : orig.id;
                health = orig.health;
                lives = orig.lives;
                moveSpeed = orig.moveSpeed;
                boostSpeed = orig.boostSpeed;
                boostCooldown = orig.boostCooldown;
                minBoostTime = orig.minBoostTime;
                maxBoostTime = orig.maxBoostTime;
                hitCooldown = orig.hitCooldown;
                rotateMode = orig.rotateMode;
                rotationSpeed = orig.rotationSpeed;
                rotationCurveType = orig.rotationCurveType;
                collisionAccurate = orig.collisionAccurate;
                sprintSneakActive = orig.sprintSneakActive;
                sprintSpeed = orig.sprintSpeed;
                sneakSpeed = orig.sneakSpeed;
                jumpGravity = orig.jumpGravity;
                jumpIntensity = orig.jumpIntensity;
                jumpCount = orig.jumpCount;
                jumpBoostCount = orig.jumpBoostCount;
                bounciness = orig.bounciness;
                canBoost = orig.canBoost;
                airBoostOnly = orig.airBoostOnly;
            }

            public override void ReadJSON(JSONNode jn)
            {
                if (jn == null)
                    return;

                id = !string.IsNullOrEmpty(jn["id"]) ? jn["id"] : GetNumberID();
                if (!string.IsNullOrEmpty(jn["name"]))
                    name = jn["name"];

                if (jn["health"] != null)
                    health = jn["health"].AsInt;
                if (jn["lives"] != null)
                    lives = jn["lives"].AsInt;
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
                if (jn["rotate_mode"] != null)
                    rotateMode = (BaseRotateMode)jn["rotate_mode"].AsInt;
                if (jn["rotate_ct"] != null)
                    rotationCurveType = (Easing)jn["rotate_ct"].AsInt;
                if (jn["rotate_s"] != null)
                    rotationSpeed = jn["rotate_s"].AsFloat;
                if (jn["collision_acc"] != null)
                    collisionAccurate = jn["collision_acc"].AsBool;
                if (jn["sprsneak"] != null)
                    sprintSneakActive = jn["sprsneak"].AsBool;
                if (jn["spr_sneak"] != null)
                    sprintSneakActive = jn["spr_sneak"].AsBool;
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
                if (jn["can_boost"] != null)
                    canBoost = jn["can_boost"].AsBool;
                if (jn["air_boost_only"] != null)
                    airBoostOnly = jn["air_boost_only"].AsBool;
            }

            public override JSONNode ToJSON()
            {
                var jn = Parser.NewJSONObject();

                if (!string.IsNullOrEmpty(name))
                    jn["name"] = name;
                if (string.IsNullOrEmpty(id))
                    id = GetNumberID();
                jn["id"] = id;

                #region Visual

                if (rotateMode != BaseRotateMode.RotateToDirection)
                    jn["rotate_mode"] = (int)rotateMode;
                if (rotationSpeed != 0.2f)
                    jn["rotate_s"] = rotationSpeed;
                if (rotationCurveType != Easing.OutCirc)
                    jn["rotate_ct"] = (int)rotationCurveType;

                #endregion

                #region Control

                #region Main

                if (health != 3)
                    jn["health"] = health;
                if (lives != -1)
                    jn["lives"] = lives;

                if (hitCooldown != 2.5f)
                    jn["hit_cooldown"] = hitCooldown;
                if (canBoost)
                    jn["can_boost"] = canBoost;
                if (collisionAccurate)
                    jn["collision_acc"] = collisionAccurate;

                #endregion

                #region Move

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
                if (sprintSneakActive)
                    jn["spr_sneak"] = sprintSneakActive;
                if (sprintSpeed != 1.3f)
                    jn["sprint_speed"] = sprintSpeed;
                if (sneakSpeed != 0.1f)
                    jn["sneak_speed"] = sneakSpeed;

                #endregion

                #region Jump

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

                #endregion

                #endregion

                return jn;
            }

            public void ReadPacket(NetworkReader reader)
            {
                id = reader.ReadString();
                name = reader.ReadString();

                #region Visual

                rotateMode = (BaseRotateMode)reader.ReadByte();
                rotationSpeed = reader.ReadSingle();
                rotationCurveType = (Easing)reader.ReadByte();

                #endregion

                #region Control

                #region Main

                health = reader.ReadInt32();
                lives = reader.ReadInt32();
                hitCooldown = reader.ReadSingle();
                canBoost = reader.ReadBoolean();
                collisionAccurate = reader.ReadBoolean();

                #endregion

                #region Move

                moveSpeed = reader.ReadSingle();
                boostSpeed = reader.ReadSingle();
                boostCooldown = reader.ReadSingle();
                minBoostTime = reader.ReadSingle();
                maxBoostTime = reader.ReadSingle();
                sprintSneakActive = reader.ReadBoolean();
                sprintSpeed = reader.ReadSingle();
                sneakSpeed = reader.ReadSingle();

                #endregion

                #region Jump

                jumpGravity = reader.ReadSingle();
                jumpIntensity = reader.ReadSingle();
                bounciness = reader.ReadSingle();
                jumpCount = reader.ReadInt32();
                jumpBoostCount = reader.ReadInt32();
                airBoostOnly = reader.ReadBoolean();

                #endregion

                #endregion
            }

            public void WritePacket(NetworkWriter writer)
            {
                writer.Write(id);
                writer.Write(name);

                #region Visual

                writer.Write((byte)rotateMode);
                writer.Write(rotationSpeed);
                writer.Write((byte)rotationCurveType);

                #endregion

                #region Control

                #region Main

                writer.Write(health);
                writer.Write(lives);
                writer.Write(hitCooldown);
                writer.Write(canBoost);
                writer.Write(collisionAccurate);

                #endregion

                #region Move

                writer.Write(moveSpeed);
                writer.Write(boostSpeed);
                writer.Write(boostCooldown);
                writer.Write(minBoostTime);
                writer.Write(maxBoostTime);
                writer.Write(sprintSneakActive);
                writer.Write(sprintSpeed);
                writer.Write(sneakSpeed);

                #endregion

                #region Jump

                writer.Write(jumpGravity);
                writer.Write(jumpIntensity);
                writer.Write(bounciness);
                writer.Write(jumpCount);
                writer.Write(jumpBoostCount);
                writer.Write(airBoostOnly);

                #endregion

                #endregion
            }

            #endregion
        }

        public class GUI : PAObject<GUI>, IPacket
        {
            public GUI() { }

            #region Values

            public override bool ShouldSerialize =>
                active ||
                mode != GUIHealthMode.Images ||
                topColor != 23 ||
                topCustomColor != RTColors.WHITE_HEX_CODE ||
                topOpacity != 1f ||
                baseColor != 4 ||
                baseCustomColor != RTColors.WHITE_HEX_CODE ||
                baseOpacity != 1f;

            public bool active = false;

            public GUIHealthMode mode;

            public int topColor = 23;

            public int baseColor = 4;

            public string topCustomColor = RTColors.WHITE_HEX_CODE;

            public string baseCustomColor = RTColors.WHITE_HEX_CODE;

            public float topOpacity = 1f;

            public float baseOpacity = 1f;

            public enum GUIHealthMode
            {
                Images,
                Text,
                EqualsBar,
                Bar
            }

            #endregion

            #region Functions

            public override void CopyData(GUI orig, bool newID = true)
            {
                active = orig.active;
                mode = orig.mode;
                topColor = orig.topColor;
                baseColor = orig.baseColor;
                topCustomColor = orig.topCustomColor;
                baseCustomColor = orig.baseCustomColor;
                topOpacity = orig.topOpacity;
                baseOpacity = orig.baseOpacity;
            }

            public override void ReadJSON(JSONNode jn)
            {
                if (jn == null)
                    return;

                if (jn["active"] != null)
                    active = jn["active"].AsBool;

                if (jn["health"] != null)
                {
                    active = jn["health"]["active"].AsBool;
                    mode = (GUIHealthMode)jn["health"]["mode"].AsInt;
                }

                if (jn["mode"] != null)
                    mode = (GUIHealthMode)jn["mode"].AsInt;
                if (jn["top_color"] != null)
                    topColor = jn["top_color"].AsInt;
                if (jn["base_color"] != null)
                    baseColor = jn["base_color"].AsInt;
                if (jn["top_custom_color"] != null)
                    topCustomColor = jn["top_custom_color"];
                if (jn["base_custom_color"] != null)
                    baseCustomColor = jn["base_custom_color"];
                if (jn["top_opacity"] != null)
                    topOpacity = jn["top_opacity"].AsFloat;
                if (jn["base_opacity"] != null)
                    baseOpacity = jn["base_opacity"].AsFloat;
            }

            public override JSONNode ToJSON()
            {
                var jn = Parser.NewJSONObject();

                if (active)
                    jn["active"] = active;
                if (mode != GUIHealthMode.Images)
                    jn["mode"] = (int)mode;

                if (topColor != 23)
                    jn["top_color"] = topColor;
                if (topCustomColor != RTColors.WHITE_HEX_CODE)
                    jn["top_custom_color"] = topCustomColor;
                if (topOpacity != 1f)
                    jn["top_opacity"] = topOpacity;

                if (baseColor != 4)
                    jn["base_color"] = baseColor;
                if (baseCustomColor != RTColors.WHITE_HEX_CODE)
                    jn["base_custom_color"] = baseCustomColor;
                if (baseOpacity != 1f)
                    jn["base_opacity"] = baseOpacity;

                return jn;
            }

            public void ReadPacket(NetworkReader reader)
            {
                active = reader.ReadBoolean();
                mode = (GUIHealthMode)reader.ReadByte();

                topColor = reader.ReadInt32();
                topCustomColor = reader.ReadString();
                topOpacity = reader.ReadSingle();

                baseColor = reader.ReadInt32();
                baseCustomColor = reader.ReadString();
                baseOpacity = reader.ReadSingle();
            }

            public void WritePacket(NetworkWriter writer)
            {
                writer.Write(active);
                writer.Write((byte)mode);

                writer.Write(topColor);
                writer.Write(topCustomColor);
                writer.Write(topOpacity);

                writer.Write(baseColor);
                writer.Write(baseCustomColor);
                writer.Write(baseOpacity);
            }

            #endregion
        }

        public class Pulse : PAObject<Pulse>, IPacket, IShapeable
        {
            public Pulse() { }

            #region Values

            #region Shape

            public int shape;

            public int shapeOption;

            public string text = string.Empty;

            public PolygonShape polygonShape = new PolygonShape();

            public int Shape { get => shape; set => shape = value; }

            public int ShapeOption { get => shapeOption; set => shapeOption = value; }

            public string Text { get => text; set => text = value; }

            public bool AutoTextAlign { get; set; }

            public PolygonShape Polygon { get => polygonShape; set => polygonShape = value; }

            public ShapeType ShapeType
            {
                get => (ShapeType)shape;
                set => shape = (int)value;
            }

            public bool IsSpecialShape => ShapeType == ShapeType.Text || ShapeType == ShapeType.Image || ShapeType == ShapeType.Polygon;

            #endregion

            #region Main

            public bool active = false;

            public bool rotateToHead = true;

            public int startColor = 0;

            public string startCustomColor = "FFFFFF";

            public int endColor = 0;

            public string endCustomColor = "FFFFFF";

            public int easingColor = 4;

            public float startOpacity = 0.5f;

            public float endOpacity = 0f;

            public int easingOpacity = 3;

            public float depth = 0.1f;

            public Vector2 startPosition = Vector2.zero;

            public Vector2 endPosition = Vector2.zero;

            public int easingPosition = 4;

            public Vector2 startScale = Vector2.zero;

            public Vector2 endScale = new Vector2(12f, 12f);

            public int easingScale = 4;

            public float startRotation = 0f;

            public float endRotation = 0f;

            public int easingRotation = 4;

            public float duration = 1f;

            #endregion

            #endregion

            #region Functions

            public override void CopyData(Pulse orig, bool newID = true)
            {
                active = orig.active;
                this.CopyShapeableData(orig);
                rotateToHead = orig.rotateToHead;
                startColor = orig.startColor;
                startCustomColor = orig.startCustomColor;
                endColor = orig.endColor;
                endCustomColor = orig.endCustomColor;
                easingColor = orig.easingColor;
                startOpacity = orig.startOpacity;
                endOpacity = orig.endOpacity;
                easingOpacity = orig.easingOpacity;
                depth = orig.depth;
                startPosition = orig.startPosition;
                endPosition = orig.endPosition;
                easingPosition = orig.easingPosition;
                startScale = orig.startScale;
                endScale = orig.endScale;
                easingScale = orig.easingScale;
                startRotation = orig.startRotation;
                endRotation = orig.endRotation;
                easingRotation = orig.easingRotation;
                duration = orig.duration;
            }

            public override void ReadJSON(JSONNode jn)
            {
                if (jn == null)
                    return;

                if (jn["active"] != null)
                    active = jn["active"].AsBool;

                this.ReadShapeJSON(jn);

                if (jn["rothead"] != null)
                    rotateToHead = jn["rothead"].AsBool;

                if (jn["col"]["start"] != null)
                    startColor = jn["col"]["start"].AsInt;

                if (jn["col"]["starthex"] != null)
                    startCustomColor = jn["col"]["starthex"];

                if (jn["col"]["end"] != null)
                    endColor = jn["col"]["end"].AsInt;

                if (jn["col"]["endhex"] != null)
                    endCustomColor = jn["col"]["endhex"];

                if (jn["col"]["easing"] != null)
                    easingColor = jn["col"]["easing"].AsInt;

                if (jn["opa"]["start"] != null)
                    startOpacity = jn["opa"]["start"].AsFloat;

                if (jn["opa"]["end"] != null)
                    endOpacity = jn["opa"]["end"].AsFloat;

                if (jn["opa"]["easing"] != null)
                    easingOpacity = jn["opa"]["easing"].AsInt;

                if (jn["d"] != null)
                    depth = jn["d"].AsFloat;

                if (jn["pos"]["start"]["x"] != null)
                    startPosition.x = jn["pos"]["start"]["x"].AsFloat;
                if (jn["pos"]["start"]["y"] != null)
                    startPosition.y = jn["pos"]["start"]["y"].AsFloat;
                
                if (jn["pos"]["end"]["x"] != null)
                    endPosition.x = jn["pos"]["end"]["x"].AsFloat;
                if (jn["pos"]["end"]["y"] != null)
                    endPosition.y = jn["pos"]["end"]["y"].AsFloat;

                if (jn["pos"]["easing"] != null)
                    easingPosition = jn["pos"]["easing"].AsInt;
                
                if (jn["sca"]["start"]["x"] != null)
                    startScale.x = jn["sca"]["start"]["x"].AsFloat;
                if (jn["sca"]["start"]["y"] != null)
                    startScale.y = jn["sca"]["start"]["y"].AsFloat;
                
                if (jn["sca"]["end"]["x"] != null)
                    endScale.x = jn["sca"]["end"]["x"].AsFloat;
                if (jn["sca"]["end"]["y"] != null)
                    endScale.y = jn["sca"]["end"]["y"].AsFloat;

                if (jn["sca"]["easing"] != null)
                    easingScale = jn["sca"]["easing"].AsInt;

                if (jn["rot"]["start"] != null)
                    startRotation = jn["rot"]["start"].AsFloat;

                if (jn["rot"]["end"] != null)
                    endRotation = jn["rot"]["end"].AsFloat;

                if (jn["rot"]["easing"] != null)
                    easingRotation = jn["rot"]["easing"].AsInt;

                if (jn["lt"] != null)
                    duration = jn["lt"].AsFloat;
            }

            public override JSONNode ToJSON()
            {
                var jn = Parser.NewJSONObject();

                jn["active"] = active;

                this.WriteShapeJSON(jn);

                jn["rothead"] = rotateToHead;

                jn["d"] = depth;

                #region Position

                jn["pos"]["start"] = startPosition.ToJSON();
                jn["pos"]["end"] = endPosition.ToJSON();
                jn["pos"]["easing"] = easingPosition;

                #endregion

                #region Scale

                jn["sca"]["start"] = startScale.ToJSON();
                jn["sca"]["end"] = endScale.ToJSON();
                jn["sca"]["easing"] = easingScale;

                #endregion

                #region Rotation

                jn["rot"]["start"] = startRotation;
                jn["rot"]["end"] = endRotation;
                jn["rot"]["easing"] = easingRotation;

                #endregion

                #region Color

                jn["col"]["start"] = startColor;
                if (!string.IsNullOrEmpty(startCustomColor))
                    jn["col"]["starthex"] = startCustomColor;
                jn["col"]["end"] = endColor;
                if (!string.IsNullOrEmpty(endCustomColor))
                    jn["col"]["endhex"] = endCustomColor;
                jn["col"]["easing"] = easingColor;

                jn["opa"]["start"] = startOpacity;
                jn["opa"]["end"] = endOpacity;
                jn["opa"]["easing"] = easingOpacity;

                #endregion

                jn["lt"] = duration;

                return jn;
            }

            public void ReadPacket(NetworkReader reader)
            {
                #region Interface

                this.ReadShapePacket(reader);

                #endregion

                active = reader.ReadBoolean();
                rotateToHead = reader.ReadBoolean();

                depth = reader.ReadSingle();

                #region Position

                startPosition = reader.ReadVector2();
                endPosition = reader.ReadVector2();
                easingPosition = reader.ReadByte();

                #endregion

                #region Scale

                startScale = reader.ReadVector2();
                endScale = reader.ReadVector2();
                easingScale = reader.ReadByte();

                #endregion

                #region Rotation

                startRotation = reader.ReadSingle();
                endRotation = reader.ReadSingle();
                easingRotation = reader.ReadByte();

                #endregion

                #region Color

                startColor = reader.ReadInt32();
                startCustomColor = reader.ReadString();
                endColor = reader.ReadInt32();
                endCustomColor = reader.ReadString();
                easingColor = reader.ReadByte();

                startOpacity = reader.ReadSingle();
                endOpacity = reader.ReadSingle();
                easingOpacity = reader.ReadByte();

                #endregion

                duration = reader.ReadSingle();
            }

            public void WritePacket(NetworkWriter writer)
            {
                #region Interface

                this.WriteShapePacket(writer);

                #endregion

                writer.Write(active);
                writer.Write(rotateToHead);

                writer.Write(depth);

                #region Position

                writer.Write(startPosition);
                writer.Write(endPosition);
                writer.Write(easingPosition);

                #endregion

                #region Scale

                writer.Write(startScale);
                writer.Write(endScale);
                writer.Write(easingScale);

                #endregion

                #region Rotation

                writer.Write(startRotation);
                writer.Write(endRotation);
                writer.Write(easingRotation);

                #endregion

                #region Color

                writer.Write(startColor);
                writer.Write(startCustomColor);
                writer.Write(endColor);
                writer.Write(endCustomColor);
                writer.Write(easingColor);

                writer.Write(startOpacity);
                writer.Write(endOpacity);
                writer.Write(easingOpacity);

                #endregion

                writer.Write(duration);
            }

            public void SetCustomShape(int shape, int shapeOption) { }

            #endregion
        }

        public class Bullet : PAObject<Bullet>, IPacket, IShapeable
        {
            public Bullet() { }

            #region Values

            #region Shape

            public int shape;

            public int shapeOption;

            public string text = string.Empty;

            public PolygonShape polygonShape = new PolygonShape();

            public int Shape { get => shape; set => shape = value; }

            public int ShapeOption { get => shapeOption; set => shapeOption = value; }

            public string Text { get => text; set => text = value; }

            public bool AutoTextAlign { get; set; }

            public PolygonShape Polygon { get => polygonShape; set => polygonShape = value; }

            public ShapeType ShapeType
            {
                get => (ShapeType)shape;
                set => shape = (int)value;
            }

            public bool IsSpecialShape => ShapeType == ShapeType.Text || ShapeType == ShapeType.Image || ShapeType == ShapeType.Polygon;

            #endregion

            #region Main

            public bool active = false;

            public bool autoKill = true;

            public float speed = 6f;

            public float lifeTime = 1f;

            public float cooldown = 0.2f;

            public bool constant = true;

            public bool hurtPlayers = false;

            public Vector2 origin = Vector2.zero;

            public Shape shapeObj = ShapeManager.inst.Shapes2D[0][0];

            public float depth = 0.1f;

            public int startColor = 0;

            public string startCustomColor = "FFFFFF";

            public int endColor = 0;

            public string endCustomColor = "FFFFFF";

            public int easingColor = 4;

            public float durationColor = 1f;

            public float startOpacity = 1f;

            public float endOpacity = 1f;

            public int easingOpacity = 3;

            public float durationOpacity = 1f;

            public Vector2 startPosition = Vector2.zero;

            public Vector2 endPosition = Vector2.zero;

            public int easingPosition = 4;

            public float durationPosition = 1f;

            public Vector2 startScale = Vector2.zero;

            public Vector2 endScale = new Vector2(3f, 1f);

            public int easingScale = 4;

            public float durationScale = 0.1f;

            public float startRotation = 0f;

            public float endRotation = 0f;

            public int easingRotation = 4;

            public float durationRotation = 1f;

            #endregion

            #endregion

            #region Functions

            public override void CopyData(Bullet orig, bool newID = true)
            {
                active = orig.active;
                autoKill = orig.autoKill;
                speed = orig.speed;
                lifeTime = orig.lifeTime;
                cooldown = orig.cooldown;
                constant = orig.constant;
                hurtPlayers = orig.hurtPlayers;
                origin = orig.origin;
                this.CopyShapeableData(orig);
                startColor = orig.startColor;
                endColor = orig.endColor;
                easingColor = orig.easingColor;
                durationColor = orig.durationColor;

                startOpacity = orig.startOpacity;
                endOpacity = orig.endOpacity;
                easingOpacity = orig.easingOpacity;
                durationOpacity = orig.durationOpacity;

                startCustomColor = orig.startCustomColor;
                endCustomColor = orig.endCustomColor;

                depth = orig.depth;

                startPosition = orig.startPosition;
                endPosition = orig.endPosition;
                easingPosition = orig.easingPosition;
                durationPosition = orig.durationPosition;

                startScale = orig.startScale;
                endScale = orig.endScale;
                easingScale = orig.easingScale;
                durationScale = orig.durationScale;

                startRotation = orig.startRotation;
                endRotation = orig.endRotation;
                easingRotation = orig.easingRotation;
                durationRotation = orig.durationRotation;
            }

            public override void ReadJSON(JSONNode jn)
            {
                if (jn == null)
                    return;

                if (jn["active"] != null)
                    active = jn["active"].AsBool;

                this.ReadShapeJSON(jn);

                if (jn["ak"] != null)
                    autoKill = jn["ak"].AsBool;

                if (jn["speed"] != null)
                    speed = jn["speed"].AsFloat;

                if (jn["lt"] != null)
                    lifeTime = jn["lt"].AsFloat;

                if (jn["delay"] != null)
                    cooldown = jn["delay"].AsFloat;
                
                if (jn["cooldown"] != null)
                    cooldown = jn["cooldown"].AsFloat;

                if (jn["constant"] != null)
                    constant = jn["constant"].AsBool;

                if (jn["hit"] != null)
                    hurtPlayers = jn["hit"].AsBool;

                if (jn["o"]["x"] != null)
                    origin.x = jn["o"]["x"].AsFloat;
                if (jn["o"]["y"] != null)
                    origin.y = jn["o"]["y"].AsFloat;

                if (jn["col"]["start"] != null)
                    startColor = jn["col"]["start"].AsInt;
                if (jn["col"]["starthex"] != null)
                    startCustomColor = jn["col"]["starthex"];
                if (jn["col"]["end"] != null)
                    endColor = jn["col"]["end"].AsInt;
                if (jn["col"]["endhex"] != null)
                    endCustomColor = jn["col"]["endhex"];
                if (jn["col"]["easing"] != null)
                    easingColor = jn["col"]["easing"].AsInt;
                if (jn["col"]["dur"] != null)
                    durationColor = jn["col"]["dur"].AsFloat;

                if (jn["opa"]["start"] != null)
                    startOpacity = jn["opa"]["start"].AsFloat;
                if (jn["opa"]["end"] != null)
                    endOpacity = jn["opa"]["end"].AsFloat;
                if (jn["opa"]["easing"] != null)
                    easingOpacity = jn["opa"]["easing"].AsInt;
                if (jn["opa"]["dur"] != null)
                    durationOpacity = jn["opa"]["dur"].AsFloat;

                if (jn["d"] != null)
                    depth = jn["d"].AsFloat;

                if (jn["pos"]["start"]["x"] != null)
                    startPosition.x = jn["pos"]["start"]["x"].AsFloat;
                if (jn["pos"]["start"]["y"] != null)
                    startPosition.y = jn["pos"]["start"]["y"].AsFloat;
                if (jn["pos"]["end"]["x"] != null)
                    endPosition.x = jn["pos"]["end"]["x"].AsFloat;
                if (jn["pos"]["end"]["y"] != null)
                    endPosition.y = jn["pos"]["end"]["y"].AsFloat;
                if (jn["pos"]["easing"] != null)
                    easingPosition = jn["pos"]["easing"].AsInt;
                if (jn["pos"]["dur"] != null)
                    durationPosition = jn["pos"]["dur"].AsFloat;

                if (jn["sca"]["start"]["x"] != null)
                    startScale.x = jn["sca"]["start"]["x"].AsFloat;
                if (jn["sca"]["start"]["y"] != null)
                    startScale.y = jn["sca"]["start"]["y"].AsFloat;
                if (jn["sca"]["end"]["x"] != null)
                    endScale.x = jn["sca"]["end"]["x"].AsFloat;
                if (jn["sca"]["end"]["y"] != null)
                    endScale.y = jn["sca"]["end"]["y"].AsFloat;
                if (jn["sca"]["easing"] != null)
                    easingScale = jn["sca"]["easing"].AsInt;
                if (jn["sca"]["dur"] != null)
                    durationPosition = jn["sca"]["dur"].AsFloat;

                if (jn["rot"]["start"] != null)
                    startRotation = jn["rot"]["start"].AsFloat;
                if (jn["rot"]["end"] != null)
                    endRotation = jn["rot"]["end"].AsFloat;
                if (jn["rot"]["easing"] != null)
                    easingRotation = jn["rot"]["easing"].AsInt;
                if (jn["rot"]["dur"] != null)
                    durationRotation = jn["rot"]["dur"].AsFloat;
            }

            public override JSONNode ToJSON()
            {
                var jn = Parser.NewJSONObject();

                jn["active"] = active;

                this.WriteShapeJSON(jn);

                jn["ak"] = autoKill;
                jn["speed"] = speed;
                jn["lt"] = lifeTime;
                jn["cooldown"] = cooldown;
                jn["constant"] = constant;

                jn["hit"] = hurtPlayers;

                jn["o"]["x"] = origin.x;
                jn["o"]["y"] = origin.y;

                jn["d"] = depth;

                #region Position

                jn["pos"]["start"]["x"] = startPosition.x;
                jn["pos"]["start"]["y"] = startPosition.y;
                jn["pos"]["end"]["x"] = endPosition.x;
                jn["pos"]["end"]["y"] = endPosition.y;
                jn["pos"]["easing"] = easingPosition;
                jn["pos"]["dur"] = durationPosition;

                #endregion

                #region Scale

                jn["sca"]["start"]["x"] = startScale.x;
                jn["sca"]["start"]["y"] = startScale.y;
                jn["sca"]["end"]["x"] = endScale.x;
                jn["sca"]["end"]["y"] = endScale.y;
                jn["sca"]["easing"] = easingScale;
                jn["sca"]["dur"] = durationScale;

                #endregion

                #region Rotation

                jn["rot"]["start"] = startRotation;
                jn["rot"]["end"] = endRotation;
                jn["rot"]["easing"] = easingRotation;
                jn["rot"]["dur"] = durationRotation;

                #endregion

                #region Color

                jn["col"]["start"] = startColor;
                if (!string.IsNullOrEmpty(startCustomColor))
                    jn["col"]["starthex"] = startCustomColor;
                jn["col"]["end"] = endColor;
                if (!string.IsNullOrEmpty(endCustomColor))
                    jn["col"]["endhex"] = endCustomColor;
                jn["col"]["easing"] = easingColor;
                jn["col"]["dur"] = durationColor;

                jn["opa"]["start"] = startOpacity;
                jn["opa"]["end"] = endOpacity;
                jn["opa"]["easing"] = easingOpacity;
                jn["opa"]["dur"] = durationOpacity;

                #endregion

                return jn;
            }

            public void ReadPacket(NetworkReader reader)
            {
                #region Interface

                this.ReadShapePacket(reader);

                #endregion

                active = reader.ReadBoolean();
                autoKill = reader.ReadBoolean();
                speed = reader.ReadSingle();
                lifeTime = reader.ReadSingle();
                cooldown = reader.ReadSingle();
                constant = reader.ReadBoolean();

                hurtPlayers = reader.ReadBoolean();

                origin = reader.ReadVector2();
                depth = reader.ReadSingle();

                #region Position

                startPosition = reader.ReadVector2();
                endPosition = reader.ReadVector2();
                easingPosition = reader.ReadByte();
                durationPosition = reader.ReadSingle();

                #endregion

                #region Scale

                startScale = reader.ReadVector2();
                endScale = reader.ReadVector2();
                easingScale = reader.ReadByte();
                durationScale = reader.ReadSingle();

                #endregion

                #region Rotation

                startRotation = reader.ReadSingle();
                endRotation = reader.ReadSingle();
                easingRotation = reader.ReadByte();
                durationRotation = reader.ReadSingle();

                #endregion

                #region Color

                startColor = reader.ReadInt32();
                startCustomColor = reader.ReadString();
                endColor = reader.ReadInt32();
                endCustomColor = reader.ReadString();
                easingColor = reader.ReadByte();
                durationColor = reader.ReadSingle();

                startOpacity = reader.ReadSingle();
                endOpacity = reader.ReadSingle();
                easingOpacity = reader.ReadByte();
                durationOpacity = reader.ReadSingle();

                #endregion
            }

            public void WritePacket(NetworkWriter writer)
            {
                #region Interface

                this.WriteShapePacket(writer);

                #endregion

                writer.Write(active);
                writer.Write(autoKill);
                writer.Write(speed);
                writer.Write(lifeTime);
                writer.Write(cooldown);
                writer.Write(constant);

                writer.Write(hurtPlayers);

                writer.Write(origin);
                writer.Write(depth);

                #region Position

                writer.Write(startPosition);
                writer.Write(endPosition);
                writer.Write(easingPosition);
                writer.Write(durationPosition);

                #endregion

                #region Scale

                writer.Write(startScale);
                writer.Write(endScale);
                writer.Write(easingScale);
                writer.Write(durationScale);

                #endregion

                #region Rotation

                writer.Write(startRotation);
                writer.Write(endRotation);
                writer.Write(easingRotation);
                writer.Write(durationRotation);

                #endregion

                #region Color

                writer.Write(startColor);
                writer.Write(startCustomColor);
                writer.Write(endColor);
                writer.Write(endCustomColor);
                writer.Write(easingColor);
                writer.Write(durationColor);

                writer.Write(startOpacity);
                writer.Write(endOpacity);
                writer.Write(easingOpacity);
                writer.Write(durationOpacity);

                #endregion
            }

            public void SetCustomShape(int shape, int shapeOption) { }

            #endregion
        }

        public class TailBase : PAObject<TailBase>, IPacket
        {
            public TailBase() { }

            #region Values

            public override bool ShouldSerialize =>
                distance != 2f ||
                mode != TailMode.Legacy ||
                grows ||
                time != 200f;

            /// <summary>
            /// The distance between each tail element.
            /// </summary>
            public float distance = 2f;

            /// <summary>
            /// Tail behavior.
            /// </summary>
            public TailMode mode;

            /// <summary>
            /// If the tail should grow when more health is given than the default.
            /// </summary>
            public bool grows;

            /// <summary>
            /// Delta time of all tail elements.
            /// </summary>
            public float time = 200f;

            /// <summary>
            /// Player tail (yes, tail, not trail) behavior.
            /// </summary>
            public enum TailMode
            {
                /// <summary>
                /// Legacy tail behavior, where each element follows the next and is not fixed to the back of the player.
                /// </summary>
                Legacy,
                /// <summary>
                /// Modern tail behavior, where each element strictly follows behind the player.
                /// </summary>
                DevPlus,
                // Beta, // i'd like to add this one if I knew how the beta player was coded
            }

            #endregion

            #region Functions

            public override void CopyData(TailBase orig, bool newID = true)
            {
                distance = orig.distance;
                mode = orig.mode;
                grows = orig.grows;
                time = orig.time;
            }

            public override void ReadJSON(JSONNode jn)
            {
                if (jn == null)
                    return;

                if (jn["distance"] != null)
                    distance = jn["distance"].AsFloat;

                if (jn["mode"] != null)
                    mode = (TailMode)jn["mode"].AsInt;

                if (jn["grows"] != null)
                    grows = jn["grows"].AsBool;

                if (jn["time"] != null)
                    time = jn["time"].AsFloat;
                if (time == 0f)
                    time = 200f;
            }

            public override JSONNode ToJSON()
            {
                var jn = Parser.NewJSONObject();

                if (distance != 2f)
                    jn["distance"] = distance;
                if (mode != TailMode.Legacy)
                    jn["mode"] = (int)mode;
                if (grows)
                    jn["grows"] = grows;
                if (time != 200f)
                    jn["time"] = time;

                return jn;
            }

            public void ReadPacket(NetworkReader reader)
            {
                distance = reader.ReadSingle();
                mode = (TailMode)reader.ReadByte();
                grows = reader.ReadBoolean();
                time = reader.ReadSingle();
            }

            public void WritePacket(NetworkWriter writer)
            {
                writer.Write(distance);
                writer.Write((byte)mode);
                writer.Write(grows);
                writer.Write(time);
            }

            #endregion
        }

        #endregion
    }
}
