using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

namespace BetterLegacy.Core.Data.Player
{
    public class PlayerModel : Exists, IModifyable<CustomPlayer>
    {
        public PlayerModel(bool setValues = true)
        {
            if (setValues)
            {
                basePart = new Base(this);
                stretchPart = new Stretch(this);
                guiPart = new GUI(this);
                headPart = new Generic(this);
                boostPart = new Generic(this);
                boostPart.color = 25;
                pulsePart = new Pulse(this);
                bulletPart = new Bullet(this);
                tailBase = new TailBase(this);
                boostTailPart = new Generic(this);
                boostTailPart.active = false;

                float t = 0.5f;
                for (int i = 0; i < 3; i++)
                {
                    var tail = new Generic(this);
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
            }
        }

        public Version Version { get; set; } = LegacyPlugin.ModVersion;
        public bool needsUpdate;

        public bool IsDefault { get; set; }

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
                    var circle = new Shape("Circle", 1, 0);
                    circlePlayer.IsDefault = true;
                    circlePlayer.basePart.id = CIRCLE_ID;
                    circlePlayer.basePart.name = "Circle";
                    circlePlayer.headPart.shape = circle;
                    circlePlayer.boostPart.shape = circle;
                    circlePlayer.pulsePart.shape = circle;
                    circlePlayer.bulletPart.shape = circle;
                    circlePlayer.boostTailPart.shape = circle;

                    for (int i = 0; i < circlePlayer.tailParts.Count; i++)
                    {
                        circlePlayer.tailParts[i].shape = circle;
                    }
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
                    betaPlayer.boostTailPart.active = true;
                    betaPlayer.boostTailPart.color = 4;
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
                    devPlayer.basePart.boostCooldown = 0f;
                }

                return devPlayer;
            }
        }

        #region Internal

        static List<PlayerModel> defaultModels;
        static PlayerModel defaultPlayer;
        static PlayerModel circlePlayer;
        static PlayerModel alphaPlayer;
        static PlayerModel betaPlayer;
        static PlayerModel devPlayer;

        #endregion

        #endregion

        public static PlayerModel DeepCopy(PlayerModel orig, bool newID = true)
        {
            var playerModel = new PlayerModel(false);
            playerModel.assets.CopyData(orig.assets);

            playerModel.basePart = Base.DeepCopy(playerModel, orig.basePart, newID);
            playerModel.stretchPart = Stretch.DeepCopy(playerModel, orig.stretchPart);
            playerModel.guiPart = GUI.DeepCopy(playerModel, orig.guiPart);
            playerModel.headPart = Generic.DeepCopy(playerModel, orig.headPart);
            playerModel.boostPart = Generic.DeepCopy(playerModel, orig.boostPart);
            playerModel.pulsePart = Pulse.DeepCopy(playerModel, orig.pulsePart);
            playerModel.bulletPart = Bullet.DeepCopy(playerModel, orig.bulletPart);
            playerModel.tailBase = TailBase.DeepCopy(playerModel, orig.tailBase);
            playerModel.boostTailPart = Generic.DeepCopy(playerModel, orig.boostTailPart);
            for (int i = 0; i < orig.tailParts.Count; i++)
                playerModel.tailParts.Add(Generic.DeepCopy(playerModel, orig.tailParts[i]));
            for (int i = 0; i < orig.customObjects.Count; i++)
                playerModel.customObjects.Add(CustomObject.DeepCopy(playerModel, orig.customObjects[i], false));

            for (int i = 0; i < orig.modifiers.Count; i++)
                playerModel.modifiers.Add(orig.modifiers[i].Copy(null));

            return playerModel;
        }

        public static PlayerModel Parse(JSONNode jn)
        {
            var playerModel = new PlayerModel(false);
            if (!string.IsNullOrEmpty(jn["version"]))
                playerModel.Version = new Version(jn["version"]);
            else
                playerModel.needsUpdate = true;

            if (jn["assets"] != null)
                playerModel.assets.ReadJSON(jn["assets"]);

            playerModel.basePart = Base.Parse(jn["base"], playerModel);
            playerModel.stretchPart = Stretch.Parse(jn["stretch"], playerModel);
            playerModel.guiPart = GUI.Parse(jn["gui"], playerModel);
            playerModel.headPart = Generic.Parse(jn["head"], playerModel);
            if (jn["face"] != null)
            {
                if (jn["face"]["position"] != null && jn["face"]["position"]["x"] != null && jn["face"]["position"]["y"] != null)
                    playerModel.facePosition = jn["face"]["position"].AsVector2();
                if (jn["face"]["con_active"] != null)
                    playerModel.faceControlActive = jn["face"]["con_active"].AsBool;
            }
            playerModel.boostPart = Generic.Parse(jn["boost"], playerModel);
            playerModel.pulsePart = jn["pulse"] == null ? new Pulse(playerModel) : Pulse.Parse(jn["pulse"], playerModel);
            playerModel.bulletPart = jn["bullet"] == null ? new Bullet(playerModel) : Bullet.Parse(jn["bullet"], playerModel);
            playerModel.tailBase = jn["tail_base"] == null ? new TailBase(playerModel) : TailBase.Parse(jn["tail_base"], playerModel);
            playerModel.boostTailPart = jn["tail_boost"] == null ? Generic.DeepCopy(playerModel, DefaultPlayer.boostTailPart) : Generic.Parse(jn["tail_boost"], playerModel);

            if (jn["tail"] != null && jn["tail"].Count > 0)
                for (int i = 0; i < jn["tail"].Count; i++)
                    playerModel.tailParts.Add(Generic.Parse(jn["tail"][i], playerModel));
            else
            {
                float t = 0.5f;
                for (int i = 0; i < 3; i++)
                {
                    var tail = new Generic(playerModel);
                    tail.Trail.emitting = true;
                    tail.color = 25;
                    tail.scale = new Vector2(t, t);
                    tail.Trail.startColor = 25;
                    tail.Trail.endColor = 25;
                    playerModel.tailParts.Add(tail);
                    t -= 0.1f;
                }
            }

            if (jn["modifiers"] != null && jn["modifiers"].Count > 0)
                for (int i = 0; i < jn["modifiers"].Count; i++)
                {
                    var modifier = Modifier<CustomPlayer>.Parse(jn["modifiers"][i]);
                    if (ModifiersHelper.VerifyModifier(modifier, ModifiersManager.defaultPlayerModifiers))
                        playerModel.modifiers.Add(modifier);
                }

            if (jn["custom_objects"] != null && jn["custom_objects"].Count > 0)
                for (int i = 0; i < jn["custom_objects"].Count; i++)
                {
                    var customObject = CustomObject.Parse(jn["custom_objects"][i], playerModel);
                    if (!string.IsNullOrEmpty(customObject.id))
                        playerModel.customObjects.Add(customObject);
                }

            playerModel.needsUpdate = false;

            return playerModel;
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            jn["version"] = Version.ToString();

            if (assets && !assets.IsEmpty())
                jn["assets"] = assets.ToJSON();

            if (basePart)
                jn["base"] = basePart.ToJSON();
            if (stretchPart)
                jn["stretch"] = stretchPart.ToJSON();
            if (guiPart)
                jn["gui"] = guiPart.ToJSON();
            jn["face"]["position"] = facePosition.ToJSON();
            jn["face"]["con_active"] = faceControlActive.ToString();
            if (headPart)
                jn["head"] = headPart.ToJSON();
            if (boostPart)
                jn["boost"] = boostPart.ToJSON();
            if (pulsePart)
                jn["pulse"] = pulsePart.ToJSON();
            if (bulletPart)
                jn["bullet"] = bulletPart.ToJSON();
            if (tailBase)
                jn["tail_base"] = tailBase.ToJSON();
            if (boostTailPart)
                jn["tail_boost"] = boostTailPart.ToJSON();

            if (tailParts != null)
                for (int i = 0; i < tailParts.Count; i++)
                jn["tail"][i] = tailParts[i].ToJSON();

            if (modifiers != null && !modifiers.IsEmpty())
                for (int i = 0; i < modifiers.Count; i++)
                    jn["modifiers"][i] = modifiers[i].ToJSON();

            if (customObjects != null && !customObjects.IsEmpty())
                for (int i = 0; i < customObjects.Count; i++)
                    jn["custom_objects"][i] = customObjects[i].ToJSON();

            return jn;
        }

        public Generic GetTail(int index) => tailParts[Mathf.Clamp(index, 0, tailParts.Count - 1)];

        public object this[int index]
        {
            get => this[Values[index]];
            set => this[Values[index]] = value;
        }

        public object this[string s]
        {
            get
            {
                switch (s)
                {
                    #region Base

                    case "Base ID": return basePart.id; // 1
                    case "Base Name": return basePart.name; // 2
                    case "Base Health": return basePart.health; // 3
                    case "Base Move Speed": return basePart.moveSpeed; // 4
                    case "Base Boost Speed": return basePart.boostSpeed; // 5
                    case "Base Boost Cooldown": return basePart.boostCooldown; // 6
                    case "Base Min Boost Time": return basePart.minBoostTime; // 7
                    case "Base Max Boost Time": return basePart.maxBoostTime; // 8
                    case "Base Hit Cooldown": return basePart.hitCooldown; // 8
                    case "Base Rotate Mode": return basePart.rotateMode; // 9
                    case "Base Collision Accurate": return basePart.collisionAccurate; // 10
                    case "Base Sprint Sneak Active": return basePart.sprintSneakActive; // 11

                    case "Base Jump Gravity": return basePart.jumpGravity;
                    case "Base Jump Intensity": return basePart.jumpIntensity;
                    case "Base Jump Count": return basePart.jumpCount;
                    case "Base Jump Boost Count": return basePart.jumpBoostCount;
                    case "Base Bounciness": return basePart.bounciness;
                    case "Base Can Boost": return basePart.canBoost;

                    #endregion

                    #region Stretch

                    case "Stretch Active": return stretchPart.active; // 12
                    case "Stretch Amount": return stretchPart.amount; // 13
                    case "Stretch Easing": return stretchPart.easing; // 14

                    #endregion

                    #region GUI

                    case "GUI Health Active": return guiPart.active; // 15
                    case "GUI Health Mode": return guiPart.mode; // 16
                    case "GUI Health Top Color": return guiPart.topColor; // 17
                    case "GUI Health Base Color": return guiPart.baseColor; // 18
                    case "GUI Health Top Custom Color": return guiPart.topCustomColor; // 19
                    case "GUI Health Base Custom Color": return guiPart.baseCustomColor; // 20
                    case "GUI Health Top Opacity": return guiPart.topOpacity; // 21
                    case "GUI Health Base Opacity": return guiPart.baseOpacity; // 22

                    #endregion
                    
                    #region Head

                    case "Head Shape": return headPart.shape; // 23
                    case "Head Position": return headPart.position; // 24
                    case "Head Scale": return headPart.scale; // 25
                    case "Head Rotation": return headPart.rotation; // 26
                    case "Head Color": return headPart.color; // 27
                    case "Head Custom Color": return headPart.customColor; // 28
                    case "Head Opacity": return headPart.opacity; // 29

                    #endregion

                    #region Head Trail

                    case "Head Trail Emitting": return headPart.Trail.emitting; // 30
                    case "Head Trail Time": return headPart.Trail.time; // 31
                    case "Head Trail Start Width": return headPart.Trail.startWidth; // 32
                    case "Head Trail End Width": return headPart.Trail.endWidth; // 33
                    case "Head Trail Start Color": return headPart.Trail.startColor; // 34
                    case "Head Trail Start Custom Color": return headPart.Trail.startCustomColor; // 35
                    case "Head Trail Start Opacity": return headPart.Trail.startOpacity; // 36
                    case "Head Trail End Color": return headPart.Trail.endColor; // 34
                    case "Head Trail End Custom Color": return headPart.Trail.endCustomColor; // 35
                    case "Head Trail End Opacity": return headPart.Trail.endOpacity; // 36
                    case "Head Trail Position Offset": return headPart.Trail.positionOffset; // 37

                    #endregion

                    #region Head Particles

                    case "Head Particles Emitting": return headPart.Particles.emitting; // 38
                    case "Head Particles Shape": return headPart.Particles.shape; // 39
                    case "Head Particles Color": return headPart.Particles.color; // 40
                    case "Head Particles Custom Color": return headPart.Particles.customColor; // 41
                    case "Head Particles Start Opacity": return headPart.Particles.startOpacity; // 42
                    case "Head Particles End Opacity": return headPart.Particles.endOpacity; // 43
                    case "Head Particles Start Scale": return headPart.Particles.startScale; // 44
                    case "Head Particles End Scale": return headPart.Particles.endScale; // 45
                    case "Head Particles Rotation": return headPart.Particles.rotation; // 46
                    case "Head Particles Lifetime": return headPart.Particles.lifeTime; // 47
                    case "Head Particles Speed": return headPart.Particles.speed; // 48
                    case "Head Particles Amount": return headPart.Particles.amount; // 49
                    case "Head Particles Force": return headPart.Particles.force; // 50
                    case "Head Particles Trail Emitting": return headPart.Particles.trailEmitting; // 51

                    #endregion

                    #region Face

                    case "Face Position": return facePosition; // 52
                    case "Face Control Active": return faceControlActive; // 53

                    #endregion

                    #region Boost

                    case "Boost Active": return boostPart.active; // 54 dang it
                    case "Boost Shape": return boostPart.shape; // 54
                    case "Boost Position": return boostPart.position; // 55
                    case "Boost Scale": return boostPart.scale; // 56
                    case "Boost Rotation": return boostPart.rotation; // 57
                    case "Boost Color": return boostPart.color; // 58
                    case "Boost Custom Color": return boostPart.customColor; // 59
                    case "Boost Opacity": return boostPart.opacity; // 60

                    #endregion

                    #region Boost Trail

                    case "Boost Trail Emitting": return boostPart.Trail.emitting; // 61
                    case "Boost Trail Time": return boostPart.Trail.time; // 62
                    case "Boost Trail Start Width": return boostPart.Trail.startWidth; // 63
                    case "Boost Trail End Width": return boostPart.Trail.endWidth; // 64
                    case "Boost Trail Start Color": return boostPart.Trail.startColor; // 65
                    case "Boost Trail Start Custom Color": return boostPart.Trail.startCustomColor; // 66
                    case "Boost Trail Start Opacity": return boostPart.Trail.startOpacity; // 67
                    case "Boost Trail End Color": return boostPart.Trail.endColor; // 68
                    case "Boost Trail End Custom Color": return boostPart.Trail.endCustomColor; // 69
                    case "Boost Trail End Opacity": return boostPart.Trail.endOpacity; // 70
                    case "Boost Trail Position Offset": return boostPart.Trail.positionOffset; // 71

                    #endregion

                    #region Boost Particles

                    case "Boost Particles Emitting": return boostPart.Particles.emitting; // 72
                    case "Boost Particles Shape": return boostPart.Particles.shape; // 73
                    case "Boost Particles Color": return boostPart.Particles.color; // 74
                    case "Boost Particles Custom Color": return boostPart.Particles.customColor; // 75
                    case "Boost Particles Start Opacity": return boostPart.Particles.startOpacity; // 76
                    case "Boost Particles End Opacity": return boostPart.Particles.endOpacity; // 77
                    case "Boost Particles Start Scale": return boostPart.Particles.startScale; // 78
                    case "Boost Particles End Scale": return boostPart.Particles.endScale; // 79
                    case "Boost Particles Rotation": return boostPart.Particles.rotation; // 80
                    case "Boost Particles Lifetime": return boostPart.Particles.lifeTime; // 81
                    case "Boost Particles Speed": return boostPart.Particles.speed; // 82
                    case "Boost Particles Amount": return (int)boostPart.Particles.amount; // 83
                    case "Boost Particles Force": return boostPart.Particles.force; // 84
                    case "Boost Particles Trail Emitting": return boostPart.Particles.trailEmitting; // 85

                    #endregion

                    #region Pulse

                    case "Pulse Active": return pulsePart.active; // 86
                    case "Pulse Shape": return pulsePart.shape; // 87
                    case "Pulse Rotate to Head": return pulsePart.rotateToHead; // 88
                    case "Pulse Start Color": return pulsePart.startColor; // 89
                    case "Pulse Start Custom Color": return pulsePart.startCustomColor; // 90
                    case "Pulse End Color": return pulsePart.endColor; // 91
                    case "Pulse End Custom Color": return pulsePart.endCustomColor; // 92
                    case "Pulse Easing Color": return pulsePart.easingColor; // 93
                    case "Pulse Start Opacity": return pulsePart.startOpacity; // 94
                    case "Pulse End Opacity": return pulsePart.endOpacity; // 95
                    case "Pulse Easing Opacity": return pulsePart.easingOpacity; // 96
                    case "Pulse Depth": return pulsePart.depth; // 97
                    case "Pulse Start Position": return pulsePart.startPosition; // 98
                    case "Pulse End Position": return pulsePart.endPosition; // 99
                    case "Pulse Easing Position": return pulsePart.easingPosition; // 100
                    case "Pulse Start Scale": return pulsePart.startScale; // 101
                    case "Pulse End Scale": return pulsePart.endScale; // 102
                    case "Pulse Easing Scale": return pulsePart.easingScale; // 103
                    case "Pulse Start Rotation": return pulsePart.startRotation; // 104
                    case "Pulse End Rotation": return pulsePart.endRotation; // 104
                    case "Pulse Easing Rotation": return pulsePart.easingRotation; // 105
                    case "Pulse Duration": return pulsePart.duration; // 106

                    #endregion

                    #region Bullet

                    case "Bullet Active": return bulletPart.active; // 107
                    case "Bullet AutoKill": return bulletPart.autoKill; // 108
                    case "Bullet Speed Amount": return bulletPart.speed; // 109
                    case "Bullet Lifetime": return bulletPart.lifeTime; // 110
                    case "Bullet Delay Amount": return bulletPart.delay; // 111
                    case "Bullet Constant": return bulletPart.constant; // 112
                    case "Bullet Hurt Players": return bulletPart.hurtPlayers; // 113
                    case "Bullet Origin": return bulletPart.origin; // 114
                    case "Bullet Shape": return bulletPart.shape; // 115
                    case "Bullet Start Color": return bulletPart.startColor; // 116
                    case "Bullet Start Custom Color": return bulletPart.startCustomColor; // 117
                    case "Bullet End Color": return bulletPart.endColor; // 118
                    case "Bullet End Custom Color": return bulletPart.endCustomColor; // 119
                    case "Bullet Easing Color": return bulletPart.easingColor; // 120
                    case "Bullet Duration Color": return bulletPart.durationColor; // 121
                    case "Bullet Start Opacity": return bulletPart.startOpacity; // 122
                    case "Bullet End Opacity": return bulletPart.endOpacity; // 123
                    case "Bullet Easing Opacity": return bulletPart.easingOpacity; // 124
                    case "Bullet Duration Opacity": return bulletPart.durationOpacity; // 125
                    case "Bullet Depth": return bulletPart.depth; // 126
                    case "Bullet Start Position": return bulletPart.startPosition; // 127
                    case "Bullet End Position": return bulletPart.endPosition; // 128
                    case "Bullet Easing Position": return bulletPart.easingPosition; // 129
                    case "Bullet Duration Position": return bulletPart.durationPosition; // 130
                    case "Bullet Start Scale": return bulletPart.startScale; // 131
                    case "Bullet End Scale": return bulletPart.endScale; // 132
                    case "Bullet Easing Scale": return bulletPart.easingScale; // 133
                    case "Bullet Duration Scale": return bulletPart.durationScale; // 134
                    case "Bullet Start Rotation": return bulletPart.startRotation; // 135
                    case "Bullet End Rotation": return bulletPart.endRotation; // 136
                    case "Bullet Easing Rotation": return bulletPart.easingRotation; // 137
                    case "Bullet Duration Rotation": return bulletPart.durationRotation; // 138

                    #endregion

                    #region Tail Base

                    case "Tail Base Distance": return tailBase.distance; // 139
                    case "Tail Base Mode": return tailBase.mode; // 140
                    case "Tail Base Grows": return tailBase.grows; // 141
                    case "Tail Base Time": return tailBase.time; // 142

                    #endregion

                    #region Tail Boost

                    case "Tail Boost Active": return boostTailPart.active; // 143
                    case "Tail Boost Shape": return boostTailPart.shape; // 144
                    case "Tail Boost Position": return boostTailPart.position; // 145
                    case "Tail Boost Scale": return boostTailPart.scale; // 146
                    case "Tail Boost Rotation": return boostTailPart.rotation; // 147
                    case "Tail Boost Color": return boostTailPart.color; // 148
                    case "Tail Boost Custom Color": return boostTailPart.customColor; // 149
                    case "Tail Boost Opacity": return boostTailPart.opacity; // 150

                    #endregion

                    #region Tail 1

                    case "Tail 1 Active": return tailParts[0].active; // 151
                    case "Tail 1 Shape": return tailParts[0].shape; // 151
                    case "Tail 1 Position": return tailParts[0].position; // 152
                    case "Tail 1 Scale": return tailParts[0].scale; // 153
                    case "Tail 1 Rotation": return tailParts[0].rotation; // 154
                    case "Tail 1 Color": return tailParts[0].color; // 155
                    case "Tail 1 Custom Color": return tailParts[0].customColor; // 156
                    case "Tail 1 Opacity": return tailParts[0].opacity; // 157

                    #endregion

                    #region Tail 1 Trail

                    case "Tail 1 Trail Emitting": return tailParts[0].Trail.emitting; // 158
                    case "Tail 1 Trail Time": return tailParts[0].Trail.time; // 159
                    case "Tail 1 Trail Start Width": return tailParts[0].Trail.startWidth; // 160
                    case "Tail 1 Trail End Width": return tailParts[0].Trail.endWidth; // 161
                    case "Tail 1 Trail Start Color": return tailParts[0].Trail.startColor; // 162
                    case "Tail 1 Trail Start Custom Color": return tailParts[0].Trail.startCustomColor; // 163
                    case "Tail 1 Trail Start Opacity": return tailParts[0].Trail.startOpacity; // 164
                    case "Tail 1 Trail End Color": return tailParts[0].Trail.endColor; // 165
                    case "Tail 1 Trail End Custom Color": return tailParts[0].Trail.endCustomColor; // 166
                    case "Tail 1 Trail End Opacity": return tailParts[0].Trail.endOpacity; // 167
                    case "Tail 1 Trail Position Offset": return tailParts[0].Trail.positionOffset; // 168

                    #endregion

                    #region Tail 1 Particles

                    case "Tail 1 Particles Emitting": return tailParts[0].Particles.emitting; // 169
                    case "Tail 1 Particles Shape": return tailParts[0].Particles.shape; // 170
                    case "Tail 1 Particles Color": return tailParts[0].Particles.color; // 171
                    case "Tail 1 Particles Custom Color": return tailParts[0].Particles.customColor; // 172
                    case "Tail 1 Particles Start Opacity": return tailParts[0].Particles.startOpacity; // 173
                    case "Tail 1 Particles End Opacity": return tailParts[0].Particles.endOpacity; // 174
                    case "Tail 1 Particles Start Scale": return tailParts[0].Particles.startScale; // 175
                    case "Tail 1 Particles End Scale": return tailParts[0].Particles.endScale; // 176
                    case "Tail 1 Particles Rotation": return tailParts[0].Particles.rotation; // 176
                    case "Tail 1 Particles Lifetime": return tailParts[0].Particles.lifeTime; // 177
                    case "Tail 1 Particles Speed": return tailParts[0].Particles.speed; // 178
                    case "Tail 1 Particles Amount": return tailParts[0].Particles.amount; // 179
                    case "Tail 1 Particles Force": return tailParts[0].Particles.force; // 180
                    case "Tail 1 Particles Trail Emitting": return tailParts[0].Particles.trailEmitting; // 181

                    #endregion

                    #region Tail 2

                    case "Tail 2 Active": return tailParts[1].active; // 151
                    case "Tail 2 Shape": return tailParts[1].shape; // 182
                    case "Tail 2 Position": return tailParts[1].position; // 183
                    case "Tail 2 Scale": return tailParts[1].scale; // 184
                    case "Tail 2 Rotation": return tailParts[1].rotation; // 185
                    case "Tail 2 Color": return tailParts[1].color; // 186
                    case "Tail 2 Custom Color": return tailParts[1].customColor; // 187
                    case "Tail 2 Opacity": return tailParts[1].opacity; // 188

                    #endregion

                    #region Tail 2 Trail

                    case "Tail 2 Trail Emitting": return tailParts[1].Trail.emitting; // 189
                    case "Tail 2 Trail Time": return tailParts[1].Trail.time; // 190
                    case "Tail 2 Trail Start Width": return tailParts[1].Trail.startWidth; // 191
                    case "Tail 2 Trail End Width": return tailParts[1].Trail.endWidth; // 192
                    case "Tail 2 Trail Start Color": return tailParts[1].Trail.startColor; // 193
                    case "Tail 2 Trail Start Custom Color": return tailParts[1].Trail.startCustomColor; // 194
                    case "Tail 2 Trail Start Opacity": return tailParts[1].Trail.startOpacity; // 195
                    case "Tail 2 Trail End Color": return tailParts[1].Trail.endColor; // 196
                    case "Tail 2 Trail End Custom Color": return tailParts[1].Trail.endCustomColor; // 197
                    case "Tail 2 Trail End Opacity": return tailParts[1].Trail.endOpacity; // 198
                    case "Tail 2 Trail Position Offset": return tailParts[1].Trail.positionOffset; // 199

                    #endregion

                    #region Tail 2 Particles

                    case "Tail 2 Particles Emitting": return tailParts[1].Particles.emitting; // 200
                    case "Tail 2 Particles Shape": return tailParts[1].Particles.shape; // 201
                    case "Tail 2 Particles Color": return tailParts[1].Particles.color; // 202
                    case "Tail 2 Particles Custom Color": return tailParts[1].Particles.customColor; // 203
                    case "Tail 2 Particles Start Opacity": return tailParts[1].Particles.startOpacity; // 204
                    case "Tail 2 Particles End Opacity": return tailParts[1].Particles.endOpacity; // 205
                    case "Tail 2 Particles Start Scale": return tailParts[1].Particles.startScale; // 206
                    case "Tail 2 Particles End Scale": return tailParts[1].Particles.endScale; // 207
                    case "Tail 2 Particles Rotation": return tailParts[1].Particles.rotation; // 208
                    case "Tail 2 Particles Lifetime": return tailParts[1].Particles.lifeTime; // 209
                    case "Tail 2 Particles Speed": return tailParts[1].Particles.speed; // 210
                    case "Tail 2 Particles Amount": return tailParts[1].Particles.amount; // 211
                    case "Tail 2 Particles Force": return tailParts[1].Particles.force; // 212
                    case "Tail 2 Particles Trail Emitting": return tailParts[1].Particles.trailEmitting; // 213

                    #endregion

                    #region Tail 3

                    case "Tail 3 Active": return tailParts[2].active; // 151
                    case "Tail 3 Shape": return tailParts[2].shape; // 214
                    case "Tail 3 Position": return tailParts[2].position; // 215
                    case "Tail 3 Scale": return tailParts[2].scale; // 216
                    case "Tail 3 Rotation": return tailParts[2].rotation; // 217
                    case "Tail 3 Color": return tailParts[2].color; // 218
                    case "Tail 3 Custom Color": return tailParts[2].customColor; // 219
                    case "Tail 3 Opacity": return tailParts[2].opacity; // 220

                    #endregion

                    #region Tail 3 Trail

                    case "Tail 3 Trail Emitting": return tailParts[2].Trail.emitting; // 221
                    case "Tail 3 Trail Time": return tailParts[2].Trail.time; // 222
                    case "Tail 3 Trail Start Width": return tailParts[2].Trail.startWidth; // 223
                    case "Tail 3 Trail End Width": return tailParts[2].Trail.endWidth; // 224
                    case "Tail 3 Trail Start Color": return tailParts[2].Trail.startColor; // 225
                    case "Tail 3 Trail Start Custom Color": return tailParts[2].Trail.startCustomColor; // 226
                    case "Tail 3 Trail Start Opacity": return tailParts[2].Trail.startOpacity; // 227
                    case "Tail 3 Trail End Color": return tailParts[2].Trail.endColor; // 228
                    case "Tail 3 Trail End Custom Color": return tailParts[2].Trail.endCustomColor; // 229
                    case "Tail 3 Trail End Opacity": return tailParts[2].Trail.endOpacity; // 230
                    case "Tail 3 Trail Position Offset": return tailParts[2].Trail.positionOffset; // 231

                    #endregion

                    #region Tail 3 Particles

                    case "Tail 3 Particles Emitting": return tailParts[2].Particles.emitting; // 232
                    case "Tail 3 Particles Shape": return tailParts[2].Particles.shape; // 233
                    case "Tail 3 Particles Color": return tailParts[2].Particles.color; // 234
                    case "Tail 3 Particles Custom Color": return tailParts[2].Particles.customColor; // 235
                    case "Tail 3 Particles Start Opacity": return tailParts[2].Particles.startOpacity; // 236
                    case "Tail 3 Particles End Opacity": return tailParts[2].Particles.endOpacity; // 237
                    case "Tail 3 Particles Start Scale": return tailParts[2].Particles.startScale; // 238
                    case "Tail 3 Particles End Scale": return tailParts[2].Particles.endScale; // 239
                    case "Tail 3 Particles Rotation": return tailParts[2].Particles.rotation; // 240
                    case "Tail 3 Particles Lifetime": return tailParts[2].Particles.lifeTime; // 241
                    case "Tail 3 Particles Speed": return tailParts[2].Particles.speed; // 242
                    case "Tail 3 Particles Amount": return tailParts[2].Particles.amount; // 243
                    case "Tail 3 Particles Force": return tailParts[2].Particles.force; // 244
                    case "Tail 3 Particles Trail Emitting": return tailParts[2].Particles.trailEmitting; // 245

                    #endregion

                    case "Custom Objects": return customObjects; // 246

                    default: throw new ArgumentOutOfRangeException($"Key \"{s}\" does not exist in Player Model.");
                }
            }
            set
            {
                switch (s)
                {
                    #region Base

                    case "Base ID": basePart.id = (string)value; // 1
                        break;
                    case "Base Name": basePart.name = (string)value; // 2
                        break;
                    case "Base Health": basePart.health = (int)value; // 3
                        break;
                    case "Base Move Speed": basePart.moveSpeed = (float)value; // 4
                        break;
                    case "Base Boost Speed": basePart.boostSpeed = (float)value; // 5
                        break;
                    case "Base Boost Cooldown": basePart.boostCooldown = (float)value; // 6
                        break;
                    case "Base Min Boost Time": basePart.minBoostTime = (float)value; // 7
                        break;
                    case "Base Max Boost Time": basePart.maxBoostTime = (float)value; // 8
                        break;
                    case "Base Hit Cooldown": basePart.hitCooldown = (float)value; // 8
                        break;
                    case "Base Rotate Mode": basePart.rotateMode = (Base.BaseRotateMode)value; // 9
                        break;
                    case "Base Collision Accurate": basePart.collisionAccurate = (bool)value; // 10
                        break;
                    case "Base Sprint Sneak Active": basePart.sprintSneakActive = (bool)value; // 11
                        break;

                    case "Base Jump Gravity": basePart.jumpGravity = (float)value;
                        break;
                    case "Base Jump Intensity": basePart.jumpIntensity = (float)value;
                        break;
                    case "Base Jump Count": basePart.jumpCount = (int)value;
                        break;
                    case "Base Jump Boost Count": basePart.jumpBoostCount = (int)value;
                        break;
                    case "Base Bounciness": basePart.bounciness = (float)value;
                        break;
                    case "Base Can Boost": basePart.canBoost = (bool)value;
                        break;

                    #endregion

                    #region Stretch

                    case "Stretch Active": stretchPart.active = (bool)value; // 12
                        break;
                    case "Stretch Amount": stretchPart.amount = (float)value; // 13
                        break;
                    case "Stretch Easing": stretchPart.easing = (int)value; // 14
                        break;

                    #endregion

                    #region GUI

                    case "GUI Health Active": guiPart.active = (bool)value; // 15
                        break;
                    case "GUI Health Mode": guiPart.mode = (GUI.GUIHealthMode)value; // 16
                        break;
                    case "GUI Health Top Color": guiPart.topColor = (int)value; // 17
                        break;
                    case "GUI Health Base Color": guiPart.baseColor = (int)value; // 18
                        break;
                    case "GUI Health Top Custom Color": guiPart.topCustomColor = (string)value; // 19
                        break;
                    case "GUI Health Base Custom Color": guiPart.baseCustomColor = (string)value; // 20
                        break;
                    case "GUI Health Top Opacity": guiPart.topOpacity = (float)value; // 21
                        break;
                    case "GUI Health Base Opacity": guiPart.baseOpacity = (float)value; // 22
                        break;

                    #endregion

                    #region Head

                    case "Head Shape": headPart.shape = (Shape)value; // 23
                        break;
                    case "Head Position": headPart.position = (Vector2)value; // 24
                        break;
                    case "Head Scale": headPart.scale = (Vector2)value; // 25
                        break;
                    case "Head Rotation": headPart.rotation = (float)value; // 26
                        break;
                    case "Head Color": headPart.color = (int)value; // 27
                        break;
                    case "Head Custom Color": headPart.customColor = (string)value; // 28
                        break;
                    case "Head Opacity": headPart.opacity = (float)value; // 29
                        break;

                    #endregion

                    #region Head Trail

                    case "Head Trail Emitting": headPart.Trail.emitting = (bool)value; // 30
                        break;
                    case "Head Trail Time": headPart.Trail.time = (float)value; // 31
                        break;
                    case "Head Trail Start Width": headPart.Trail.startWidth = (float)value; // 32
                        break;
                    case "Head Trail End Width": headPart.Trail.endWidth = (float)value; // 33
                        break;
                    case "Head Trail Start Color": headPart.Trail.startColor = (int)value; // 34
                        break;
                    case "Head Trail Start Custom Color": headPart.Trail.startCustomColor = (string)value; // 35
                        break;
                    case "Head Trail Start Opacity": headPart.Trail.startOpacity = (float)value; // 36
                        break;
                    case "Head Trail End Color": headPart.Trail.endColor = (int)value; // 34
                        break;
                    case "Head Trail End Custom Color": headPart.Trail.endCustomColor = (string)value; // 35
                        break;
                    case "Head Trail End Opacity": headPart.Trail.endOpacity = (float)value; // 36
                        break;
                    case "Head Trail Position Offset": headPart.Trail.positionOffset = (Vector2)value; // 37
                        break;

                    #endregion

                    #region Head Particles

                    case "Head Particles Emitting": headPart.Particles.emitting = (bool)value; // 38
                        break;
                    case "Head Particles Shape": headPart.Particles.shape = (Shape)value; // 39
                        break;
                    case "Head Particles Color": headPart.Particles.color = (int)value; // 40
                        break;
                    case "Head Particles Custom Color": headPart.Particles.customColor = (string)value; // 41
                        break;
                    case "Head Particles Start Opacity": headPart.Particles.startOpacity = (float)value; // 42
                        break;
                    case "Head Particles End Opacity": headPart.Particles.endOpacity = (float)value; // 43
                        break;
                    case "Head Particles Start Scale": headPart.Particles.startScale = (float)value; // 44
                        break;
                    case "Head Particles End Scale": headPart.Particles.endScale = (float)value; // 45
                        break;
                    case "Head Particles Rotation": headPart.Particles.rotation = (float)value; // 46
                        break;
                    case "Head Particles Lifetime": headPart.Particles.lifeTime = (float)value; // 47
                        break;
                    case "Head Particles Speed": headPart.Particles.speed = (float)value; // 48
                        break;
                    case "Head Particles Amount": headPart.Particles.amount = (float)value; // 49
                        break;
                    case "Head Particles Force": headPart.Particles.force = (Vector2)value; // 50
                        break;
                    case "Head Particles Trail Emitting": headPart.Particles.trailEmitting = (bool)value; // 51
                        break;

                    #endregion

                    #region Face

                    case "Face Position": facePosition = (Vector2)value; // 52
                        break;
                    case "Face Control Active": faceControlActive = (bool)value; // 53
                        break;

                    #endregion

                    #region Boost

                    case "Boost Active": boostPart.active = (bool)value; // 54 dang it
                        break;
                    case "Boost Shape": boostPart.shape = (Shape)value; // 54
                        break;
                    case "Boost Position": boostPart.position = (Vector2)value; // 55
                        break;
                    case "Boost Scale": boostPart.scale = (Vector2)value; // 56
                        break;
                    case "Boost Rotation": boostPart.rotation = (float)value; // 57
                        break;
                    case "Boost Color": boostPart.color = (int)value; // 58
                        break;
                    case "Boost Custom Color": boostPart.customColor = (string)value; // 59
                        break;
                    case "Boost Opacity": boostPart.opacity = (float)value; // 60
                        break;

                    #endregion

                    #region Boost Trail

                    case "Boost Trail Emitting": boostPart.Trail.emitting = (bool)value; // 61
                        break;
                    case "Boost Trail Time": boostPart.Trail.time = (float)value; // 62
                        break;
                    case "Boost Trail Start Width": boostPart.Trail.startWidth = (float)value; // 63
                        break;
                    case "Boost Trail End Width": boostPart.Trail.endWidth = (float)value; // 64
                        break;
                    case "Boost Trail Start Color": boostPart.Trail.startColor = (int)value; // 65
                        break;
                    case "Boost Trail Start Custom Color": boostPart.Trail.startCustomColor = (string)value; // 66
                        break;
                    case "Boost Trail Start Opacity": boostPart.Trail.startOpacity = (float)value; // 67
                        break;
                    case "Boost Trail End Color": boostPart.Trail.endColor = (int)value; // 68
                        break;
                    case "Boost Trail End Custom Color": boostPart.Trail.endCustomColor = (string)value; // 69
                        break;
                    case "Boost Trail End Opacity": boostPart.Trail.endOpacity = (float)value; // 70
                        break;
                    case "Boost Trail Position Offset": boostPart.Trail.positionOffset = (Vector2)value; // 71
                        break;

                    #endregion

                    #region Boost Particles

                    case "Boost Particles Emitting": boostPart.Particles.emitting = (bool)value; // 72
                        break;
                    case "Boost Particles Shape": boostPart.Particles.shape = (Shape)value; // 73
                        break;
                    case "Boost Particles Color": boostPart.Particles.color = (int)value; // 74
                        break;
                    case "Boost Particles Custom Color": boostPart.Particles.customColor = (string)value; // 75
                        break;
                    case "Boost Particles Start Opacity": boostPart.Particles.startOpacity = (float)value; // 76
                        break;
                    case "Boost Particles End Opacity": boostPart.Particles.endOpacity = (float)value; // 77
                        break;
                    case "Boost Particles Start Scale": boostPart.Particles.startScale = (float)value; // 78
                        break;
                    case "Boost Particles End Scale": boostPart.Particles.endScale = (float)value; // 79
                        break;
                    case "Boost Particles Rotation": boostPart.Particles.rotation = (float)value; // 80
                        break;
                    case "Boost Particles Lifetime": boostPart.Particles.lifeTime = (float)value; // 81
                        break;
                    case "Boost Particles Speed": boostPart.Particles.speed = (float)value; // 82
                        break;
                    case "Boost Particles Amount": boostPart.Particles.amount = (int)value; // 83
                        break;
                    case "Boost Particles Force": boostPart.Particles.force = (Vector2)value; // 84
                        break;
                    case "Boost Particles Trail Emitting": boostPart.Particles.trailEmitting = (bool)value; // 85
                        break;

                    #endregion

                    #region Pulse

                    case "Pulse Active": pulsePart.active = (bool)value; // 86
                        break;
                    case "Pulse Shape": pulsePart.shape = (Shape)value; // 87
                        break;
                    case "Pulse Rotate to Head": pulsePart.rotateToHead = (bool)value; // 88
                        break;
                    case "Pulse Start Color": pulsePart.startColor = (int)value; // 89
                        break;
                    case "Pulse Start Custom Color": pulsePart.startCustomColor = (string)value; // 90
                        break;
                    case "Pulse End Color": pulsePart.endColor = (int)value; // 91
                        break;
                    case "Pulse End Custom Color": pulsePart.endCustomColor = (string)value; // 92
                        break;
                    case "Pulse Easing Color": pulsePart.easingColor = (int)value; // 93
                        break;
                    case "Pulse Start Opacity": pulsePart.startOpacity = (float)value; // 94
                        break;
                    case "Pulse End Opacity": pulsePart.endOpacity = (float)value; // 95
                        break;
                    case "Pulse Easing Opacity": pulsePart.easingOpacity = (int)value; // 96
                        break;
                    case "Pulse Depth": pulsePart.depth = (float)value; // 97
                        break;
                    case "Pulse Start Position": pulsePart.startPosition = (Vector2)value; // 98
                        break;
                    case "Pulse End Position": pulsePart.endPosition = (Vector2)value; // 99
                        break;
                    case "Pulse Easing Position": pulsePart.easingPosition = (int)value; // 100
                        break;
                    case "Pulse Start Scale": pulsePart.startScale = (Vector2)value; // 101
                        break;
                    case "Pulse End Scale": pulsePart.endScale = (Vector2)value; // 102
                        break;
                    case "Pulse Easing Scale": pulsePart.easingScale = (int)value; // 103
                        break;
                    case "Pulse Start Rotation": pulsePart.startRotation = (float)value; // 104
                        break;
                    case "Pulse End Rotation": pulsePart.endRotation = (float)value; // 104
                        break;
                    case "Pulse Easing Rotation": pulsePart.easingRotation = (int)value; // 105
                        break;
                    case "Pulse Duration": pulsePart.duration = (float)value; // 106
                        break;

                    #endregion

                    #region Bullet

                    case "Bullet Active": bulletPart.active = (bool)value; // 107
                        break;
                    case "Bullet AutoKill": bulletPart.autoKill = (bool)value; // 108
                        break;
                    case "Bullet Speed Amount": bulletPart.speed = (float)value; // 109
                        break;
                    case "Bullet Lifetime": bulletPart.lifeTime = (float)value; // 110
                        break;
                    case "Bullet Delay Amount": bulletPart.delay = (float)value; // 111
                        break;
                    case "Bullet Constant": bulletPart.constant = (bool)value; // 112
                        break;
                    case "Bullet Hurt Players": bulletPart.hurtPlayers = (bool)value; // 113
                        break;
                    case "Bullet Origin": bulletPart.origin = (Vector2)value; // 114
                        break;
                    case "Bullet Shape": bulletPart.shape = (Shape)value; // 115
                        break;
                    case "Bullet Start Color": bulletPart.startColor = (int)value; // 116
                        break;
                    case "Bullet Start Custom Color": bulletPart.startCustomColor = (string)value; // 117
                        break;
                    case "Bullet End Color": bulletPart.endColor = (int)value; // 118
                        break;
                    case "Bullet End Custom Color": bulletPart.endCustomColor = (string)value; // 119
                        break;
                    case "Bullet Easing Color": bulletPart.easingColor = (int)value; // 120
                        break;
                    case "Bullet Duration Color": bulletPart.durationColor = (float)value; // 121
                        break;
                    case "Bullet Start Opacity": bulletPart.startOpacity = (float)value; // 122
                        break;
                    case "Bullet End Opacity": bulletPart.endOpacity = (float)value; // 123
                        break;
                    case "Bullet Easing Opacity": bulletPart.easingOpacity = (int)value; // 124
                        break;
                    case "Bullet Duration Opacity": bulletPart.durationOpacity = (float)value; // 125
                        break;
                    case "Bullet Depth": bulletPart.depth = (float)value; // 126
                        break;
                    case "Bullet Start Position": bulletPart.startPosition = (Vector2)value; // 127
                        break;
                    case "Bullet End Position": bulletPart.endPosition = (Vector2)value; // 128
                        break;
                    case "Bullet Easing Position": bulletPart.easingPosition = (int)value; // 129
                        break;
                    case "Bullet Duration Position": bulletPart.durationPosition = (float)value; // 130
                        break;
                    case "Bullet Start Scale": bulletPart.startScale = (Vector2)value; // 131
                        break;
                    case "Bullet End Scale": bulletPart.endScale = (Vector2)value; // 132
                        break;
                    case "Bullet Easing Scale": bulletPart.easingScale = (int)value; // 133
                        break;
                    case "Bullet Duration Scale": bulletPart.durationScale = (float)value; // 134
                        break;
                    case "Bullet Start Rotation": bulletPart.startRotation = (float)value; // 135
                        break;
                    case "Bullet End Rotation": bulletPart.endRotation = (float)value; // 136
                        break;
                    case "Bullet Easing Rotation": bulletPart.easingRotation = (int)value; // 137
                        break;
                    case "Bullet Duration Rotation": bulletPart.durationRotation = (float)value; // 138
                        break;

                    #endregion

                    #region Tail Base

                    case "Tail Base Distance": tailBase.distance = (float)value; // 139
                        break;
                    case "Tail Base Mode": tailBase.mode = (TailBase.TailMode)value; // 140
                        break;
                    case "Tail Base Grows": tailBase.grows = (bool)value; // 141
                        break;
                    case "Tail Base Time": tailBase.time = (float)value; // 142
                        break;

                    #endregion

                    #region Tail Boost

                    case "Tail Boost Active": boostTailPart.active = (bool)value; // 143
                        break;
                    case "Tail Boost Shape": boostTailPart.shape = (Shape)value; // 144
                        break;
                    case "Tail Boost Position": boostTailPart.position = (Vector2)value; // 145
                        break;
                    case "Tail Boost Scale": boostTailPart.scale = (Vector2)value; // 146
                        break;
                    case "Tail Boost Rotation": boostTailPart.rotation = (float)value; // 147
                        break;
                    case "Tail Boost Color": boostTailPart.color = (int)value; // 148
                        break;
                    case "Tail Boost Custom Color": boostTailPart.customColor = (string)value; // 149
                        break;
                    case "Tail Boost Opacity": boostTailPart.opacity = (float)value; // 150
                        break;

                    #endregion

                    #region Tail 1

                    case "Tail 1 Active": tailParts[0].active = (bool)value; // 151
                        break;
                    case "Tail 1 Shape": tailParts[0].shape = (Shape)value; // 151
                        break;
                    case "Tail 1 Position": tailParts[0].position = (Vector2)value; // 152
                        break;
                    case "Tail 1 Scale": tailParts[0].scale = (Vector2)value; // 153
                        break;
                    case "Tail 1 Rotation": tailParts[0].rotation = (float)value; // 154
                        break;
                    case "Tail 1 Color": tailParts[0].color = (int)value; // 155
                        break;
                    case "Tail 1 Custom Color": tailParts[0].customColor = (string)value; // 156
                        break;
                    case "Tail 1 Opacity": tailParts[0].opacity = (float)value; // 157
                        break;

                    #endregion

                    #region Tail 1 Trail

                    case "Tail 1 Trail Emitting": tailParts[0].Trail.emitting = (bool)value; // 158
                        break;
                    case "Tail 1 Trail Time": tailParts[0].Trail.time = (float)value; // 159
                        break;
                    case "Tail 1 Trail Start Width": tailParts[0].Trail.startWidth = (float)value; // 160
                        break;
                    case "Tail 1 Trail End Width": tailParts[0].Trail.endWidth = (float)value; // 161
                        break;
                    case "Tail 1 Trail Start Color": tailParts[0].Trail.startColor = (int)value; // 162
                        break;
                    case "Tail 1 Trail Start Custom Color": tailParts[0].Trail.startCustomColor = (string)value; // 163
                        break;
                    case "Tail 1 Trail Start Opacity": tailParts[0].Trail.startOpacity = (float)value; // 164
                        break;
                    case "Tail 1 Trail End Color": tailParts[0].Trail.endColor = (int)value; // 165
                        break;
                    case "Tail 1 Trail End Custom Color": tailParts[0].Trail.endCustomColor = (string)value; // 166
                        break;
                    case "Tail 1 Trail End Opacity": tailParts[0].Trail.endOpacity = (float)value; // 167
                        break;
                    case "Tail 1 Trail Position Offset": tailParts[0].Trail.positionOffset = (Vector2)value; // 168
                        break;

                    #endregion

                    #region Tail 1 Particles

                    case "Tail 1 Particles Emitting": tailParts[0].Particles.emitting = (bool)value; // 169
                        break;
                    case "Tail 1 Particles Shape": tailParts[0].Particles.shape = (Shape)value; // 170
                        break;
                    case "Tail 1 Particles Color": tailParts[0].Particles.color = (int)value; // 171
                        break;
                    case "Tail 1 Particles Custom Color": tailParts[0].Particles.customColor = (string)value; // 172
                        break;
                    case "Tail 1 Particles Start Opacity": tailParts[0].Particles.startOpacity = (float)value; // 173
                        break;
                    case "Tail 1 Particles End Opacity": tailParts[0].Particles.endOpacity = (float)value; // 174
                        break;
                    case "Tail 1 Particles Start Scale": tailParts[0].Particles.startScale = (float)value; // 175
                        break;
                    case "Tail 1 Particles End Scale": tailParts[0].Particles.endScale = (float)value; // 176
                        break;
                    case "Tail 1 Particles Rotation": tailParts[0].Particles.rotation = (float)value; // 176
                        break;
                    case "Tail 1 Particles Lifetime": tailParts[0].Particles.lifeTime = (float)value; // 177
                        break;
                    case "Tail 1 Particles Speed": tailParts[0].Particles.speed = (float)value; // 178
                        break;
                    case "Tail 1 Particles Amount": tailParts[0].Particles.amount = (float)value; // 179
                        break;
                    case "Tail 1 Particles Force": tailParts[0].Particles.force = (Vector2)value; // 180
                        break;
                    case "Tail 1 Particles Trail Emitting": tailParts[0].Particles.trailEmitting = (bool)value; // 181
                        break;

                    #endregion

                    #region Tail 2

                    case "Tail 2 Active": tailParts[1].active = (bool)value; // 151
                        break;
                    case "Tail 2 Shape": tailParts[1].shape = (Shape)value; // 182
                        break;
                    case "Tail 2 Position": tailParts[1].position = (Vector2)value; // 183
                        break;
                    case "Tail 2 Scale": tailParts[1].scale = (Vector2)value; // 184
                        break;
                    case "Tail 2 Rotation": tailParts[1].rotation = (float)value; // 185
                        break;
                    case "Tail 2 Color": tailParts[1].color = (int)value; // 186
                        break;
                    case "Tail 2 Custom Color": tailParts[1].customColor = (string)value; // 187
                        break;
                    case "Tail 2 Opacity": tailParts[1].opacity = (float)value; // 188
                        break;

                    #endregion

                    #region Tail 2 Trail

                    case "Tail 2 Trail Emitting": tailParts[1].Trail.emitting = (bool)value; // 189
                        break;
                    case "Tail 2 Trail Time": tailParts[1].Trail.time = (float)value; // 190
                        break;
                    case "Tail 2 Trail Start Width": tailParts[1].Trail.startWidth = (float)value; // 191
                        break;
                    case "Tail 2 Trail End Width": tailParts[1].Trail.endWidth = (float)value; // 192
                        break;
                    case "Tail 2 Trail Start Color": tailParts[1].Trail.startColor = (int)value; // 193
                        break;
                    case "Tail 2 Trail Start Custom Color": tailParts[1].Trail.startCustomColor = (string)value; // 194
                        break;
                    case "Tail 2 Trail Start Opacity": tailParts[1].Trail.startOpacity = (float)value; // 195
                        break;
                    case "Tail 2 Trail End Color": tailParts[1].Trail.endColor = (int)value; // 196
                        break;
                    case "Tail 2 Trail End Custom Color": tailParts[1].Trail.endCustomColor = (string)value; // 197
                        break;
                    case "Tail 2 Trail End Opacity": tailParts[1].Trail.endOpacity = (float)value; // 198
                        break;
                    case "Tail 2 Trail Position Offset": tailParts[1].Trail.positionOffset = (Vector2)value; // 199
                        break;

                    #endregion

                    #region Tail 2 Particles

                    case "Tail 2 Particles Emitting": tailParts[1].Particles.emitting = (bool)value; // 200
                        break;
                    case "Tail 2 Particles Shape": tailParts[1].Particles.shape = (Shape)value; // 201
                        break;
                    case "Tail 2 Particles Color": tailParts[1].Particles.color = (int)value; // 202
                        break;
                    case "Tail 2 Particles Custom Color": tailParts[1].Particles.customColor = (string)value; // 203
                        break;
                    case "Tail 2 Particles Start Opacity": tailParts[1].Particles.startOpacity = (float)value; // 204
                        break;
                    case "Tail 2 Particles End Opacity": tailParts[1].Particles.endOpacity = (float)value; // 205
                        break;
                    case "Tail 2 Particles Start Scale": tailParts[1].Particles.startScale = (float)value; // 206
                        break;
                    case "Tail 2 Particles End Scale": tailParts[1].Particles.endScale = (float)value; // 207
                        break;
                    case "Tail 2 Particles Rotation": tailParts[1].Particles.rotation = (float)value; // 208
                        break;
                    case "Tail 2 Particles Lifetime": tailParts[1].Particles.lifeTime = (float)value; // 209
                        break;
                    case "Tail 2 Particles Speed": tailParts[1].Particles.speed = (float)value; // 210
                        break;
                    case "Tail 2 Particles Amount": tailParts[1].Particles.amount = (float)value; // 211
                        break;
                    case "Tail 2 Particles Force": tailParts[1].Particles.force = (Vector2)value; // 212
                        break;
                    case "Tail 2 Particles Trail Emitting": tailParts[1].Particles.trailEmitting = (bool)value; // 213
                        break;

                    #endregion

                    #region Tail 3

                    case "Tail 3 Active": tailParts[2].active = (bool)value; // 151
                        break;
                    case "Tail 3 Shape": tailParts[2].shape = (Shape)value; // 214
                        break;
                    case "Tail 3 Position": tailParts[2].position = (Vector2)value; // 215
                        break;
                    case "Tail 3 Scale": tailParts[2].scale = (Vector2)value; // 216
                        break;
                    case "Tail 3 Rotation": tailParts[2].rotation = (float)value; // 217
                        break;
                    case "Tail 3 Color": tailParts[2].color = (int)value; // 218
                        break;
                    case "Tail 3 Custom Color": tailParts[2].customColor = (string)value; // 219
                        break;
                    case "Tail 3 Opacity": tailParts[2].opacity = (float)value; // 220
                        break;

                    #endregion

                    #region Tail 3 Trail

                    case "Tail 3 Trail Emitting": tailParts[2].Trail.emitting = (bool)value; // 221
                        break;
                    case "Tail 3 Trail Time": tailParts[2].Trail.time = (float)value; // 222
                        break;
                    case "Tail 3 Trail Start Width": tailParts[2].Trail.startWidth = (float)value; // 223
                        break;
                    case "Tail 3 Trail End Width": tailParts[2].Trail.endWidth = (float)value; // 224
                        break;
                    case "Tail 3 Trail Start Color": tailParts[2].Trail.startColor = (int)value; // 225
                        break;
                    case "Tail 3 Trail Start Custom Color": tailParts[2].Trail.startCustomColor = (string)value; // 226
                        break;
                    case "Tail 3 Trail Start Opacity": tailParts[2].Trail.startOpacity = (float)value; // 227
                        break;
                    case "Tail 3 Trail End Color": tailParts[2].Trail.endColor = (int)value; // 228
                        break;
                    case "Tail 3 Trail End Custom Color": tailParts[2].Trail.endCustomColor = (string)value; // 229
                        break;
                    case "Tail 3 Trail End Opacity": tailParts[2].Trail.endOpacity = (float)value; // 230
                        break;
                    case "Tail 3 Trail Position Offset": tailParts[2].Trail.positionOffset = (Vector2)value; // 231
                        break;

                    #endregion

                    #region Tail 3 Particles

                    case "Tail 3 Particles Emitting": tailParts[2].Particles.emitting = (bool)value; // 232
                        break;
                    case "Tail 3 Particles Shape": tailParts[2].Particles.shape = (Shape)value; // 233
                        break;
                    case "Tail 3 Particles Color": tailParts[2].Particles.color = (int)value; // 234
                        break;
                    case "Tail 3 Particles Custom Color": tailParts[2].Particles.customColor = (string)value; // 235
                        break;
                    case "Tail 3 Particles Start Opacity": tailParts[2].Particles.startOpacity = (float)value; // 236
                        break;
                    case "Tail 3 Particles End Opacity": tailParts[2].Particles.endOpacity = (float)value; // 237
                        break;
                    case "Tail 3 Particles Start Scale": tailParts[2].Particles.startScale = (float)value; // 238
                        break;
                    case "Tail 3 Particles End Scale": tailParts[2].Particles.endScale = (float)value; // 239
                        break;
                    case "Tail 3 Particles Rotation": tailParts[2].Particles.rotation = (float)value; // 240
                        break;
                    case "Tail 3 Particles Lifetime": tailParts[2].Particles.lifeTime = (float)value; // 241
                        break;
                    case "Tail 3 Particles Speed": tailParts[2].Particles.speed = (float)value; // 242
                        break;
                    case "Tail 3 Particles Amount": tailParts[2].Particles.amount = (float)value; // 243
                        break;
                    case "Tail 3 Particles Force": tailParts[2].Particles.force = (Vector2)value; // 244
                        break;
                    case "Tail 3 Particles Trail Emitting": tailParts[2].Particles.trailEmitting = (bool)value; // 245
                        break;

                    #endregion

                    case "Custom Objects": customObjects = (List<CustomObject>)value; // 246
                        break;

                    default: throw new ArgumentOutOfRangeException($"Key \"{s}\" does not exist in Player Model.");
                }
            }
        }

        public static List<string> Values => new List<string>
        {
            #region Base

            "Base ID",
            "Base Name",
            "Base Health",
            "Base Move Speed",
            "Base Boost Speed",
            "Base Boost Cooldown",
            "Base Min Boost Time",
            "Base Max Boost Time",
            "Base Hit Cooldown",
            "Base Rotate Mode",
            "Base Collision Accurate",
            "Base Sprint Sneak Active",

            "Base Jump Gravity",
            "Base Jump Intensity",
            "Base Jump Count",
            "Base Jump Boost Count",
            "Base Bounciness",
            "Base Can Boost",

            #endregion

            #region Stretch

            "Stretch Active",
            "Stretch Amount",
            "Stretch Easing",

            #endregion

            #region GUI

            "GUI Health Active",
            "GUI Health Mode",
            "GUI Health Top Color",
            "GUI Health Base Color",
            "GUI Health Top Custom Color",
            "GUI Health Base Custom Color",
            "GUI Health Top Opacity",
            "GUI Health Base Opacity",

            #endregion

            #region Head

            "Head Shape",
            "Head Position",
            "Head Scale",
            "Head Rotation",
            "Head Color",
            "Head Custom Color",
            "Head Opacity",

            #endregion

            #region Head Trail

            "Head Trail Emitting",
            "Head Trail Time",
            "Head Trail Start Width",
            "Head Trail End Width",
            "Head Trail Start Color",
            "Head Trail Start Custom Color",
            "Head Trail Start Opacity",
            "Head Trail End Color",
            "Head Trail End Custom Color",
            "Head Trail End Opacity",
            "Head Trail Position Offset",

            #endregion

            #region Head Particles

            "Head Particles Emitting",
            "Head Particles Shape",
            "Head Particles Color",
            "Head Particles Custom Color",
            "Head Particles Start Opacity",
            "Head Particles End Opacity",
            "Head Particles Start Scale",
            "Head Particles End Scale",
            "Head Particles Rotation",
            "Head Particles Lifetime",
            "Head Particles Speed",
            "Head Particles Amount",
            "Head Particles Force",
            "Head Particles Trail Emitting",

            #endregion

            #region Face

            "Face Position",
            "Face Control Active",

            #endregion
            
            #region Boost

            "Boost Active",
            "Boost Shape",
            "Boost Position",
            "Boost Scale",
            "Boost Rotation",
            "Boost Color",
            "Boost Custom Color",
            "Boost Opacity",

            #endregion
            
            #region Boost Trail

            "Boost Trail Emitting",
            "Boost Trail Time",
            "Boost Trail Start Width",
            "Boost Trail End Width",
            "Boost Trail Start Color",
            "Boost Trail Start Custom Color",
            "Boost Trail Start Opacity",
            "Boost Trail End Color",
            "Boost Trail End Custom Color",
            "Boost Trail End Opacity",
            "Boost Trail Position Offset",

            #endregion

            #region Boost Particles

            "Boost Particles Emitting",
            "Boost Particles Shape",
            "Boost Particles Color",
            "Boost Particles Custom Color",
            "Boost Particles Start Opacity",
            "Boost Particles End Opacity",
            "Boost Particles Start Scale",
            "Boost Particles End Scale",
            "Boost Particles Rotation",
            "Boost Particles Lifetime",
            "Boost Particles Speed",
            "Boost Particles Amount",
            "Boost Particles Force",
            "Boost Particles Trail Emitting",

            #endregion
            
            #region Pulse

            "Pulse Active",
            "Pulse Shape",
            "Pulse Rotate to Head",
            "Pulse Start Color",
            "Pulse Start Custom Color",
            "Pulse End Color",
            "Pulse End Custom Color",
            "Pulse Easing Color",
            "Pulse Start Opacity",
            "Pulse End Opacity",
            "Pulse Easing Opacity",
            "Pulse Depth",
            "Pulse Start Position",
            "Pulse End Position",
            "Pulse Easing Position",
            "Pulse Start Scale",
            "Pulse End Scale",
            "Pulse Easing Scale",
            "Pulse Start Rotation",
            "Pulse End Rotation",
            "Pulse Easing Rotation",
            "Pulse Duration",

            #endregion
            
            #region Bullet

            "Bullet Active",
            "Bullet AutoKill",
            "Bullet Speed Amount",
            "Bullet Lifetime",
            "Bullet Delay Amount",
            "Bullet Constant",
            "Bullet Hurt Players",
            "Bullet Origin",
            "Bullet Shape",
            "Bullet Start Color",
            "Bullet Start Custom Color",
            "Bullet End Color",
            "Bullet End Custom Color",
            "Bullet Easing Color",
            "Bullet Duration Color",
            "Bullet Start Opacity",
            "Bullet End Opacity",
            "Bullet Easing Opacity",
            "Bullet Duration Opacity",
            "Bullet Depth",
            "Bullet Start Position",
            "Bullet End Position",
            "Bullet Easing Position",
            "Bullet Duration Position",
            "Bullet Start Scale",
            "Bullet End Scale",
            "Bullet Easing Scale",
            "Bullet Duration Scale",
            "Bullet Start Rotation",
            "Bullet End Rotation",
            "Bullet Easing Rotation",
            "Bullet Duration Rotation",

            #endregion
            
            #region Tail Base

            "Tail Base Distance",
            "Tail Base Mode",
            "Tail Base Grows",
            "Tail Base Time",

            #endregion
            
            #region Tail Boost

            "Tail Boost Active",
            "Tail Boost Shape",
            "Tail Boost Position",
            "Tail Boost Scale",
            "Tail Boost Rotation",
            "Tail Boost Color",
            "Tail Boost Custom Color",
            "Tail Boost Opacity",

            #endregion
            
            #region Tail 1

            "Tail 1 Active",
            "Tail 1 Shape",
            "Tail 1 Position",
            "Tail 1 Scale",
            "Tail 1 Rotation",
            "Tail 1 Color",
            "Tail 1 Custom Color",
            "Tail 1 Opacity",

            #endregion
            
            #region Tail 1 Trail

            "Tail 1 Trail Emitting",
            "Tail 1 Trail Time",
            "Tail 1 Trail Start Width",
            "Tail 1 Trail End Width",
            "Tail 1 Trail Start Color",
            "Tail 1 Trail Start Custom Color",
            "Tail 1 Trail Start Opacity",
            "Tail 1 Trail End Color",
            "Tail 1 Trail End Custom Color",
            "Tail 1 Trail End Opacity",
            "Tail 1 Trail Position Offset",

            #endregion

            #region Tail 1 Particles

            "Tail 1 Particles Emitting",
            "Tail 1 Particles Shape",
            "Tail 1 Particles Color",
            "Tail 1 Particles Custom Color",
            "Tail 1 Particles Start Opacity",
            "Tail 1 Particles End Opacity",
            "Tail 1 Particles Start Scale",
            "Tail 1 Particles End Scale",
            "Tail 1 Particles Rotation",
            "Tail 1 Particles Lifetime",
            "Tail 1 Particles Speed",
            "Tail 1 Particles Amount",
            "Tail 1 Particles Force",
            "Tail 1 Particles Trail Emitting",

            #endregion
            
            #region Tail 2

            "Tail 2 Active",
            "Tail 2 Shape",
            "Tail 2 Position",
            "Tail 2 Scale",
            "Tail 2 Rotation",
            "Tail 2 Color",
            "Tail 2 Custom Color",
            "Tail 2 Opacity",

            #endregion
            
            #region Tail 2 Trail

            "Tail 2 Trail Emitting",
            "Tail 2 Trail Time",
            "Tail 2 Trail Start Width",
            "Tail 2 Trail End Width",
            "Tail 2 Trail Start Color",
            "Tail 2 Trail Start Custom Color",
            "Tail 2 Trail Start Opacity",
            "Tail 2 Trail End Color",
            "Tail 2 Trail End Custom Color",
            "Tail 2 Trail End Opacity",
            "Tail 2 Trail Position Offset",

            #endregion

            #region Tail 2 Particles

            "Tail 2 Particles Emitting",
            "Tail 2 Particles Shape",
            "Tail 2 Particles Color",
            "Tail 2 Particles Custom Color",
            "Tail 2 Particles Start Opacity",
            "Tail 2 Particles End Opacity",
            "Tail 2 Particles Start Scale",
            "Tail 2 Particles End Scale",
            "Tail 2 Particles Rotation",
            "Tail 2 Particles Lifetime",
            "Tail 2 Particles Speed",
            "Tail 2 Particles Amount",
            "Tail 2 Particles Force",
            "Tail 2 Particles Trail Emitting",

            #endregion
            
            #region Tail 3

            "Tail 3 Active",
            "Tail 3 Shape",
            "Tail 3 Position",
            "Tail 3 Scale",
            "Tail 3 Rotation",
            "Tail 3 Color",
            "Tail 3 Custom Color",
            "Tail 3 Opacity",

            #endregion
            
            #region Tail 3 Trail

            "Tail 3 Trail Emitting",
            "Tail 3 Trail Time",
            "Tail 3 Trail Start Width",
            "Tail 3 Trail End Width",
            "Tail 3 Trail Start Color",
            "Tail 3 Trail Start Custom Color",
            "Tail 3 Trail Start Opacity",
            "Tail 3 Trail End Color",
            "Tail 3 Trail End Custom Color",
            "Tail 3 Trail End Opacity",
            "Tail 3 Trail Position Offset",

            #endregion

            #region Tail 3 Particles

            "Tail 3 Particles Emitting",
            "Tail 3 Particles Shape",
            "Tail 3 Particles Color",
            "Tail 3 Particles Custom Color",
            "Tail 3 Particles Start Opacity",
            "Tail 3 Particles End Opacity",
            "Tail 3 Particles Start Scale",
            "Tail 3 Particles End Scale",
            "Tail 3 Particles Rotation",
            "Tail 3 Particles Lifetime",
            "Tail 3 Particles Speed",
            "Tail 3 Particles Amount",
            "Tail 3 Particles Force",
            "Tail 3 Particles Trail Emitting",

            #endregion
            
            "Custom Objects",
        };

        public Assets assets = new Assets();

        public Base basePart;

        public Stretch stretchPart;

        public GUI guiPart;

        public Generic headPart;

        public Vector2 facePosition = new Vector2(0.3f, 0f);

        public bool faceControlActive = true;

        public Generic boostPart;

        public Pulse pulsePart;

        public Bullet bulletPart;

        public Generic boostTailPart;

        public TailBase tailBase;

        public List<Generic> tailParts = new List<Generic>();

        public void AddTail() => tailParts.Add(Generic.DeepCopy(this, tailParts.Last()));

        public void RemoveTail(int index)
        {
            if (tailParts.Count > 1 || !tailParts.InRange(index))
                return;
            tailParts.RemoveAt(index);
        }

        public List<CustomObject> customObjects = new List<CustomObject>();

        public List<string> Tags { get; set; }

        public List<Modifier<CustomPlayer>> modifiers = new List<Modifier<CustomPlayer>>();

        public List<Modifier<CustomPlayer>> Modifiers { get => modifiers; set => modifiers = value; }

        public bool IgnoreLifespan { get; set; }

        public bool OrderModifiers { get; set; }

        public int IntVariable { get; set; }

        public bool ModifiersActive => false;

        public class Base : Exists
        {
            public Base(PlayerModel playerModelRef) => this.playerModelRef = playerModelRef;

            PlayerModel playerModelRef;

            public static Base DeepCopy(PlayerModel playerModelRef, Base orig, bool newID = true) => new Base(playerModelRef)
            {
                name = orig.name,
                id = newID ? LSFunctions.LSText.randomNumString(16) : orig.id,
                health = orig.health,
                moveSpeed = orig.moveSpeed,
                boostSpeed = orig.boostSpeed,
                boostCooldown = orig.boostCooldown,
                minBoostTime = orig.minBoostTime,
                maxBoostTime = orig.maxBoostTime,
                hitCooldown = orig.hitCooldown,
                rotateMode = orig.rotateMode,
                collisionAccurate = orig.collisionAccurate,
                sprintSneakActive = orig.sprintSneakActive,
                jumpGravity = orig.jumpGravity,
                jumpIntensity = orig.jumpIntensity,
                jumpCount = orig.jumpCount,
                jumpBoostCount = orig.jumpBoostCount,
                bounciness = orig.bounciness,
                canBoost = orig.canBoost,
            };

            public static Base Parse(JSONNode jn, PlayerModel playerModel)
            {
                var b = new Base(playerModel);

                if (!string.IsNullOrEmpty(jn["name"]))
                    b.name = jn["name"];
                if (!string.IsNullOrEmpty(jn["id"]))
                    b.id = jn["id"];
                else
                    b.id = LSFunctions.LSText.randomNumString(16);
                if (!string.IsNullOrEmpty(jn["health"]))
                    b.health = jn["health"].AsInt;
                if (!string.IsNullOrEmpty(jn["move_speed"]))
                    b.moveSpeed = jn["move_speed"].AsFloat;
                if (!string.IsNullOrEmpty(jn["boost_speed"]))
                    b.boostSpeed = jn["boost_speed"].AsFloat;
                if (!string.IsNullOrEmpty(jn["boost_cooldown"]))
                    b.boostCooldown = jn["boost_cooldown"].AsFloat;
                if (!string.IsNullOrEmpty(jn["boost_min_time"]))
                    b.minBoostTime = jn["boost_min_time"].AsFloat;
                if (!string.IsNullOrEmpty(jn["boost_max_time"]))
                    b.maxBoostTime = jn["boost_max_time"].AsFloat;
                if (!string.IsNullOrEmpty(jn["hit_cooldown"]))
                    b.hitCooldown = jn["hit_cooldown"].AsFloat;
                if (!string.IsNullOrEmpty(jn["rotate_mode"]))
                    b.rotateMode = (BaseRotateMode)jn["rotate_mode"].AsInt;
                if (!string.IsNullOrEmpty(jn["collision_acc"]))
                    b.collisionAccurate = jn["collision_acc"].AsBool;
                if (!string.IsNullOrEmpty(jn["sprsneak"]))
                    b.sprintSneakActive = jn["sprsneak"].AsBool;

                if (!string.IsNullOrEmpty(jn["jump_gravity"]))
                    b.jumpGravity = jn["jump_gravity"].AsFloat;
                if (!string.IsNullOrEmpty(jn["jump_intensity"]))
                    b.jumpIntensity = jn["jump_intensity"].AsFloat;
                if (!string.IsNullOrEmpty(jn["bounciness"]))
                    b.bounciness = jn["bounciness"].AsFloat;
                if (!string.IsNullOrEmpty(jn["jump_count"]))
                    b.jumpCount = jn["jump_count"].AsInt;
                if (!string.IsNullOrEmpty(jn["jump_boost_count"]))
                    b.jumpBoostCount = jn["jump_boost_count"].AsInt;
                if (!string.IsNullOrEmpty(jn["can_boost"]))
                    b.canBoost = jn["can_boost"].AsBool;

                return b;
            }

            public JSONNode ToJSON()
            {
                var jn = JSON.Parse("{}");

                if (!string.IsNullOrEmpty(name))
                    jn["name"] = name;
                if (!string.IsNullOrEmpty(id))
                    jn["id"] = id;
                else
                {
                    id = LSText.randomNumString(16);
                    jn["id"] = id;
                }

                if (health != 3)
                    jn["health"] = health.ToString();
                if (moveSpeed != 20f)
                    jn["move_speed"] = moveSpeed.ToString();
                if (boostSpeed != 85f)
                    jn["boost_speed"] = boostSpeed.ToString();
                if (boostCooldown != 0.1f)
                    jn["boost_cooldown"] = boostCooldown.ToString();
                if (minBoostTime != 0.07f)
                    jn["boost_min_time"] = minBoostTime.ToString();
                if (maxBoostTime != 0.18f)
                    jn["boost_max_time"] = maxBoostTime.ToString();
                if (hitCooldown != 2.5f)
                    jn["hit_cooldown"] = hitCooldown.ToString();
                if (rotateMode != BaseRotateMode.RotateToDirection)
                    jn["rotate_mode"] = ((int)rotateMode).ToString();
                if (collisionAccurate)
                    jn["collision_acc"] = collisionAccurate.ToString();
                if (sprintSneakActive)
                    jn["sprsneak"] = sprintSneakActive.ToString();
                if (jumpGravity != 40f)
                    jn["jump_gravity"] = jumpGravity.ToString();
                if (jumpIntensity != 10f)
                    jn["jump_intensity"] = jumpIntensity.ToString();
                if (bounciness != 0.1f)
                    jn["bounciness"] = bounciness.ToString();
                if (jumpCount != 1)
                    jn["jump_count"] = jumpCount.ToString();
                if (jumpBoostCount != 1)
                    jn["jump_boost_count"] = jumpBoostCount.ToString();
                if (canBoost)
                    jn["can_boost"] = canBoost.ToString();

                return jn;
            }

            public string name;

            public string id;

            public int health = 3;

            public float moveSpeed = 20f;

            public float boostSpeed = 85f;

            public float boostCooldown = 0.1f;

            public float minBoostTime = 0.07f;

            public float maxBoostTime = 0.18f;

            public float jumpGravity = 10f;

            public float jumpIntensity = 40f;

            public float bounciness = 0.1f;

            public int jumpCount = 1;
            public int jumpBoostCount = 1;

            public bool canBoost = true;

            public float hitCooldown = 2.5f;

            public BaseRotateMode rotateMode = BaseRotateMode.RotateToDirection;

            public bool collisionAccurate = false;

            public bool sprintSneakActive = false;

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

            public PAAnimation boostAnimation;
            public PAAnimation hitAnimation;
        }

        public class Stretch : Exists
        {
            public Stretch(PlayerModel playerModelRef) => this.playerModelRef = playerModelRef;

            PlayerModel playerModelRef;

            public static Stretch DeepCopy(PlayerModel playerModelRef, Stretch orig) => new Stretch(playerModelRef)
            {
                active = orig.active,
                amount = orig.amount,
                easing = orig.easing
            };

            public static Stretch Parse(JSONNode jn, PlayerModel playerModel)
            {
                var stretch = new Stretch(playerModel);

                if (!string.IsNullOrEmpty(jn["active"]))
                    stretch.active = jn["active"].AsBool;
                if (!string.IsNullOrEmpty(jn["amount"]))
                    stretch.amount = jn["amount"].AsFloat;
                if (!string.IsNullOrEmpty(jn["easing"]))
                    stretch.easing = jn["easing"].AsInt;

                return stretch;
            }

            public JSONNode ToJSON()
            {
                var jn = JSON.Parse("{}");

                jn["active"] = active.ToString();
                jn["amount"] = amount.ToString();
                jn["easing"] = easing.ToString();

                return jn;
            }

            public bool active = false;

            public float amount = 0.4f;

            public int easing = 6;
        }

        public class GUI : Exists
        {
            public GUI(PlayerModel playerModelRef) => this.playerModelRef = playerModelRef;

            PlayerModel playerModelRef;

            public static GUI DeepCopy(PlayerModel playerModelRef, GUI orig) => new GUI(playerModelRef)
            {
                active = orig.active,
                mode = orig.mode,
                topColor = orig.topColor,
                baseColor = orig.baseColor,
                topCustomColor = orig.topCustomColor,
                baseCustomColor = orig.baseCustomColor,
                topOpacity = orig.topOpacity,
                baseOpacity = orig.baseOpacity,
            };

            public static GUI Parse(JSONNode jn, PlayerModel playerModel)
            {
                var gui = new GUI(playerModel);

                if (jn["health"] != null)
                {
                    gui.active = jn["health"]["active"].AsBool;
                    gui.mode = (GUIHealthMode)jn["health"]["mode"].AsInt;
                }

                if (!string.IsNullOrEmpty(jn["active"]))
                    gui.active = jn["active"].AsBool;
                if (!string.IsNullOrEmpty(jn["mode"]))
                    gui.mode = (GUIHealthMode)jn["mode"].AsInt;
                if (!string.IsNullOrEmpty(jn["top_color"]))
                    gui.topColor = jn["top_color"].AsInt;
                if (!string.IsNullOrEmpty(jn["base_color"]))
                    gui.baseColor = jn["base_color"].AsInt;
                if (!string.IsNullOrEmpty(jn["top_custom_color"]))
                    gui.topCustomColor = jn["top_custom_color"];
                if (!string.IsNullOrEmpty(jn["base_custom_color"]))
                    gui.baseCustomColor = jn["base_custom_color"];
                if (!string.IsNullOrEmpty(jn["top_opacity"]))
                    gui.topOpacity = jn["top_opacity"].AsFloat;
                if (!string.IsNullOrEmpty(jn["base_opacity"]))
                    gui.baseOpacity = jn["base_opacity"].AsFloat;

                return gui;
            }

            public JSONNode ToJSON()
            {
                var jn = JSON.Parse("{}");

                jn["active"] = active.ToString();
                jn["mode"] = ((int)mode).ToString();
                jn["top_color"] = topColor.ToString();
                jn["base_color"] = baseColor.ToString();
                jn["top_custom_color"] = topCustomColor;
                jn["base_custom_color"] = baseCustomColor;
                jn["top_opacity"] = topOpacity.ToString();
                jn["base_opacity"] = baseOpacity.ToString();

                return jn;
            }

            public bool active = false;

            public GUIHealthMode mode;

            public int topColor = 23;

            public int baseColor = 4;

            public string topCustomColor = "FFFFFF";

            public string baseCustomColor = "FFFFFF";

            public float topOpacity = 1f;

            public float baseOpacity = 1f;

            public enum GUIHealthMode
            {
                Images,
                Text,
                EqualsBar,
                Bar
            }
        }

        public class Generic : Exists
        {
            public Generic(PlayerModel playerModelRef)
            {
                this.playerModelRef = playerModelRef;
                Trail = new Trail(playerModelRef);
                Particles = new Particles(playerModelRef);
            }

            PlayerModel playerModelRef;

            public static Generic DeepCopy(PlayerModel playerModelRef, Generic orig) => new Generic(playerModelRef)
            {
                active = orig.active,
                shape = orig.shape,
                text = orig.text,
                polygonShape = orig.polygonShape.Copy(),
                position = orig.position,
                scale = orig.scale,
                rotation = orig.rotation,
                color = orig.color,
                customColor = orig.customColor,
                opacity = orig.opacity,
                Trail = Trail.DeepCopy(playerModelRef, orig.Trail),
                Particles = Particles.DeepCopy(playerModelRef, orig.Particles),
            };

            public static Generic Parse(JSONNode jn, PlayerModel playerModel)
            {
                var generic = new Generic(playerModel);

                if (jn["active"] != null)
                    generic.active = jn["active"].AsBool;

                generic.shape = ShapeManager.inst.Shapes2D[jn["s"].AsInt][jn["so"].AsInt];

                if (jn["csp"] != null)
                    generic.polygonShape = PolygonShape.Parse(jn["csp"]);

                if (!string.IsNullOrEmpty(jn["t"]))
                    generic.text = jn["t"];

                if (jn["pos"] != null && !string.IsNullOrEmpty(jn["pos"]["x"]) && !string.IsNullOrEmpty(jn["pos"]["y"]))
                    generic.position = new Vector2(jn["pos"]["x"].AsFloat, jn["pos"]["y"].AsFloat);
                
                if (jn["sca"] != null && !string.IsNullOrEmpty(jn["sca"]["x"]) && !string.IsNullOrEmpty(jn["sca"]["y"]))
                    generic.scale = new Vector2(jn["sca"]["x"].AsFloat, jn["sca"]["y"].AsFloat);

                if (jn["rot"] != null && !string.IsNullOrEmpty(jn["rot"]["x"]))
                    generic.rotation = jn["rot"]["x"].AsFloat;

                if (jn["col"] != null && !string.IsNullOrEmpty(jn["col"]["x"]))
                    generic.color = jn["col"]["x"].AsInt;

                if (jn["col"] != null && !string.IsNullOrEmpty(jn["col"]["hex"]))
                    generic.customColor = jn["col"]["hex"];

                if (jn["opa"] != null && !string.IsNullOrEmpty(jn["opa"]["x"]))
                    generic.opacity = jn["opa"]["x"].AsFloat;

                generic.Trail = Trail.Parse(jn["trail"], playerModel);
                generic.Particles = Particles.Parse(jn["particles"], playerModel);

                return generic;
            }

            public JSONNode ToJSON()
            {
                var jn = JSON.Parse("{}");

                jn["active"] = active.ToString();

                if (shape.type != 0)
                    jn["s"] = shape.type.ToString();
                if (shape.option != 0)
                    jn["so"] = shape.option.ToString();

                if (!string.IsNullOrEmpty(text))
                    jn["t"] = text;
                if (polygonShape)
                    jn["csp"] = polygonShape.ToJSON();

                jn["pos"]["x"] = position.x.ToString();
                jn["pos"]["y"] = position.y.ToString();

                jn["sca"]["x"] = scale.x.ToString();
                jn["sca"]["y"] = scale.y.ToString();

                jn["rot"]["x"] = rotation.ToString();

                jn["col"]["x"] = color.ToString();

                if (color == 24 && customColor != "FFFFFF" && !string.IsNullOrEmpty(customColor))
                    jn["col"]["hex"] = customColor;

                if (opacity != 1f)
                    jn["opa"]["x"] = opacity.ToString();

                jn["trail"] = Trail.ToJSON();
                jn["particles"] = Particles.ToJSON();

                return jn;
            }

            public bool active = true;

            public Shape shape = ShapeManager.inst.Shapes2D[0][0];

            public string text = string.Empty;

            public PolygonShape polygonShape = new PolygonShape();

            public Vector2 position = Vector2.zero;

            public Vector2 scale = Vector2.one;

            public float rotation;

            public int color = 23;

            public string customColor = "FFFFFF";

            public float opacity = 1f;

            public Trail Trail { get; set; }
            public Particles Particles { get; set; }
        }

        public class Trail : Exists
        {
            public Trail(PlayerModel playerModelRef) => this.playerModelRef = playerModelRef;

            PlayerModel playerModelRef;

            public static Trail DeepCopy(PlayerModel playerModelRef, Trail orig) => new Trail(playerModelRef)
            {
                emitting = orig.emitting,
                time = orig.time,
                startWidth = orig.startWidth,
                endWidth = orig.endWidth,
                startColor = orig.startColor,
                endColor = orig.endColor,
                startCustomColor = orig.startCustomColor,
                endCustomColor = orig.endCustomColor,
                startOpacity = orig.startOpacity,
                endOpacity = orig.endOpacity,
                positionOffset = orig.positionOffset,
            };

            public static Trail Parse(JSONNode jn, PlayerModel playerModel)
            {
                var trail = new Trail(playerModel);

                if (!string.IsNullOrEmpty(jn["em"]))
                    trail.emitting = jn["em"].AsBool;

                if (!string.IsNullOrEmpty(jn["t"]))
                    trail.time = jn["t"].AsFloat;

                if (jn["w"] != null && !string.IsNullOrEmpty(jn["w"]["start"]))
                    trail.startWidth = jn["w"]["start"].AsFloat;

                if (jn["w"] != null && !string.IsNullOrEmpty(jn["w"]["end"]))
                    trail.endWidth = jn["w"]["end"].AsFloat;

                if (jn["c"] != null && !string.IsNullOrEmpty(jn["c"]["start"]))
                    trail.startColor = jn["c"]["start"].AsInt;
                
                if (jn["c"] != null && !string.IsNullOrEmpty(jn["c"]["end"]))
                    trail.endColor = jn["c"]["end"].AsInt;
                
                if (jn["c"] != null && !string.IsNullOrEmpty(jn["c"]["starthex"]))
                    trail.startCustomColor = jn["c"]["starthex"];
                
                if (jn["c"] != null && !string.IsNullOrEmpty(jn["c"]["endhex"]))
                    trail.endCustomColor = jn["c"]["starthex"];
                
                if (jn["o"] != null && !string.IsNullOrEmpty(jn["o"]["start"]))
                    trail.startOpacity = jn["o"]["start"].AsFloat;
                
                if (jn["o"] != null && !string.IsNullOrEmpty(jn["o"]["end"]))
                    trail.endOpacity = jn["o"]["end"].AsFloat;

                if (jn["pos"] != null && !string.IsNullOrEmpty(jn["pos"]["x"]) && !string.IsNullOrEmpty(jn["pos"]["y"]))
                    trail.positionOffset = new Vector2(jn["pos"]["x"].AsFloat, jn["pos"]["y"].AsFloat);

                return trail;
            }

            public JSONNode ToJSON()
            {
                var jn = JSON.Parse("{}");

                jn["em"] = emitting.ToString();
                jn["t"] = time.ToString();
                jn["w"]["start"] = startWidth.ToString();
                jn["w"]["end"] = endWidth.ToString();
                jn["c"]["start"] = startColor.ToString();
                jn["c"]["end"] = endColor.ToString();
                if (!string.IsNullOrEmpty(startCustomColor))
                    jn["c"]["starthex"] = startCustomColor.ToString();
                if (!string.IsNullOrEmpty(endCustomColor))
                    jn["c"]["endhex"] = endCustomColor.ToString();
                jn["o"]["start"] = startOpacity.ToString();
                jn["o"]["end"] = endOpacity.ToString();
                jn["pos"]["x"] = positionOffset.x.ToString();
                jn["pos"]["y"] = positionOffset.y.ToString();

                return jn;
            }

            public bool emitting;

            public float time = 1f;

            public float startWidth = 1f;

            public float endWidth = 1f;

            public int startColor = 23;

            public string startCustomColor = "FFFFFF";

            public float startOpacity = 1f;

            public int endColor = 23;

            public string endCustomColor = "FFFFFF";

            public float endOpacity = 0f;

            public Vector2 positionOffset = Vector2.zero;
        }

        public class Particles : Exists
        {
            public Particles(PlayerModel playerModelRef) => this.playerModelRef = playerModelRef;

            PlayerModel playerModelRef;

            public static Particles DeepCopy(PlayerModel playerModelRef, Particles orig) => new Particles(playerModelRef)
            {
                emitting = orig.emitting,
                shape = orig.shape,
                color = orig.color,
                customColor = orig.customColor,
                startOpacity = orig.startOpacity,
                endOpacity = orig.endOpacity,
                startScale = orig.startScale,
                endScale = orig.endScale,
                rotation = orig.rotation,
                lifeTime = orig.lifeTime,
                speed = orig.speed,
                force = orig.force,
                trailEmitting = orig.trailEmitting,
                amount = orig.amount,
            };

            public static Particles Parse(JSONNode jn, PlayerModel playerModel)
            {
                var particles = new Particles(playerModel);

                if (!string.IsNullOrEmpty(jn["em"]))
                    particles.emitting = jn["em"].AsBool;

                int s = 0;
                int so = 0;

                if (!string.IsNullOrEmpty(jn["s"]))
                    s = jn["s"].AsInt;
                
                if (!string.IsNullOrEmpty(jn["so"]))
                    so = jn["so"].AsInt;

                particles.shape = ShapeManager.inst.Shapes2D[s][so];

                if (!string.IsNullOrEmpty(jn["col"]))
                    particles.color = jn["col"].AsInt;
                
                if (!string.IsNullOrEmpty(jn["colhex"]))
                    particles.customColor = jn["colhex"];

                if (!string.IsNullOrEmpty(jn["opa"]["start"]))
                    particles.startOpacity = jn["opa"]["start"].AsFloat;
                
                if (!string.IsNullOrEmpty(jn["opa"]["end"]))
                    particles.endOpacity = jn["opa"]["end"].AsFloat;
                
                if (!string.IsNullOrEmpty(jn["sca"]["start"]))
                    particles.startScale = jn["sca"]["start"].AsFloat;
                
                if (!string.IsNullOrEmpty(jn["sca"]["end"]))
                    particles.endScale = jn["sca"]["end"].AsFloat;

                if (!string.IsNullOrEmpty(jn["rot"]))
                    particles.rotation = jn["rot"].AsFloat;
                
                if (!string.IsNullOrEmpty(jn["lt"]))
                    particles.lifeTime = jn["lt"].AsFloat;
                
                if (!string.IsNullOrEmpty(jn["sp"]))
                    particles.speed = jn["sp"].AsFloat;
                
                if (!string.IsNullOrEmpty(jn["am"]))
                    particles.speed = jn["am"].AsFloat;

                if (jn["frc"] != null && !string.IsNullOrEmpty(jn["frc"]["x"]) && !string.IsNullOrEmpty(jn["frc"]["y"]))
                    particles.force = new Vector2(jn["frc"]["x"].AsFloat, jn["frc"]["y"].AsFloat);

                if (!string.IsNullOrEmpty(jn["trem"]))
                    particles.trailEmitting = jn["trem"].AsBool;

                return particles;
            }

            public JSONNode ToJSON()
            {
                var jn = JSON.Parse("{}");

                jn["em"] = emitting.ToString();
                if (shape.type != 0)
                    jn["s"] = shape.type.ToString();
                if (shape.option != 0)
                    jn["so"] = shape.option.ToString();

                jn["col"] = color.ToString();
                if (!string.IsNullOrEmpty(customColor))
                    jn["colhex"] = customColor;

                jn["opa"]["start"] = startOpacity.ToString();
                jn["opa"]["end"] = endOpacity.ToString();
                
                jn["sca"]["start"] = startScale.ToString();
                jn["sca"]["end"] = endScale.ToString();

                jn["rot"] = rotation.ToString();
                jn["lt"] = lifeTime.ToString();
                jn["sp"] = speed.ToString();
                jn["am"] = amount.ToString();

                jn["frc"]["x"] = force.x.ToString();
                jn["frc"]["y"] = force.y.ToString();

                jn["trem"] = trailEmitting.ToString();

                return jn;
            }

            public bool emitting;

            public Shape shape = ShapeManager.inst.Shapes2D[0][0];

            public int color = 23;

            public string customColor = "FFFFFF";

            public float startOpacity = 1f;

            public float endOpacity = 0f;

            public float startScale = 1f;

            public float endScale = 0f;

            public float rotation = 0f;

            public float lifeTime = 5f;

            public float speed = 5f;

            public float amount = 5f;

            public Vector2 force = Vector2.zero;

            public bool trailEmitting = false;
        }

        public class Pulse : Exists
        {
            public Pulse(PlayerModel playerModelRef) => this.playerModelRef = playerModelRef;

            PlayerModel playerModelRef;

            public static Pulse DeepCopy(PlayerModel playerModelRef, Pulse orig) => new Pulse(playerModelRef)
            {
                active = orig.active,
                shape = orig.shape,
                rotateToHead = orig.rotateToHead,
                startColor = orig.startColor,
                startCustomColor = orig.startCustomColor,
                endColor = orig.endColor,
                endCustomColor = orig.endCustomColor,
                easingColor = orig.easingColor,
                startOpacity = orig.startOpacity,
                endOpacity = orig.endOpacity,
                easingOpacity = orig.easingOpacity,
                depth = orig.depth,
                startPosition = orig.startPosition,
                endPosition = orig.endPosition,
                easingPosition = orig.easingPosition,
                startScale = orig.startScale,
                endScale = orig.endScale,
                easingScale = orig.easingScale,
                startRotation = orig.startRotation,
                endRotation = orig.endRotation,
                easingRotation = orig.easingRotation,
                duration = orig.duration,
            };

            public static Pulse Parse(JSONNode jn, PlayerModel playerModel)
            {
                var pulse = new Pulse(playerModel);

                if (!string.IsNullOrEmpty(jn["active"]))
                    pulse.active = jn["active"].AsBool;

                int s = 0;
                int so = 0;

                if (!string.IsNullOrEmpty(jn["s"]))
                    s = jn["s"].AsInt;
                
                if (!string.IsNullOrEmpty(jn["so"]))
                    so = jn["so"].AsInt;

                pulse.shape = ShapeManager.inst.Shapes2D[s][so];

                if (!string.IsNullOrEmpty(jn["rothead"]))
                    pulse.rotateToHead = jn["rothead"].AsBool;

                if (jn["col"] != null && !string.IsNullOrEmpty(jn["col"]["start"]))
                    pulse.startColor = jn["col"]["start"].AsInt;
                
                if (jn["col"] != null && !string.IsNullOrEmpty(jn["col"]["starthex"]))
                    pulse.startCustomColor = jn["col"]["starthex"];
                
                if (jn["col"] != null && !string.IsNullOrEmpty(jn["col"]["end"]))
                    pulse.endColor = jn["col"]["end"].AsInt;
                
                if (jn["col"] != null && !string.IsNullOrEmpty(jn["col"]["endhex"]))
                    pulse.endCustomColor = jn["col"]["endhex"];

                if (jn["col"] != null && !string.IsNullOrEmpty(jn["col"]["easing"]))
                    pulse.easingColor = jn["col"]["easing"].AsInt;

                if (jn["opa"] != null && !string.IsNullOrEmpty(jn["opa"]["start"]))
                    pulse.startOpacity = jn["opa"]["start"].AsFloat;
                
                if (jn["opa"] != null && !string.IsNullOrEmpty(jn["opa"]["end"]))
                    pulse.endOpacity = jn["opa"]["end"].AsFloat;
                
                if (jn["opa"] != null && !string.IsNullOrEmpty(jn["opa"]["easing"]))
                    pulse.easingOpacity = jn["opa"]["easing"].AsInt;

                if (!string.IsNullOrEmpty(jn["d"]))
                    pulse.depth = jn["d"].AsFloat;

                if (jn["pos"] != null && jn["pos"]["start"]["x"] != null && jn["pos"]["start"]["x"] != null)
                    pulse.startPosition = jn["pos"]["start"].AsVector2();
                //pulse.startPosition = new Vector2(jn["pos"]["start"]["x"].AsFloat, jn["pos"]["start"]["y"].AsFloat);

                if (jn["pos"] != null && jn["pos"]["end"]["x"] != null && jn["pos"]["end"]["x"] != null)
                    pulse.endPosition = jn["pos"]["end"].AsVector2();
                //pulse.endPosition = new Vector2(jn["pos"]["end"]["x"].AsFloat, jn["pos"]["end"]["y"].AsFloat);

                if (jn["pos"] != null && !string.IsNullOrEmpty(jn["pos"]["easing"]))
                    pulse.easingPosition = jn["pos"]["easing"].AsInt;

                if (jn["sca"] != null && jn["sca"]["start"]["x"] != null && jn["sca"]["start"]["x"] != null)
                    pulse.startScale = jn["sca"]["start"].AsVector2();
                //pulse.startScale = new Vector2(jn["sca"]["start"]["x"].AsFloat, jn["sca"]["start"]["y"].AsFloat);

                if (jn["sca"] != null && jn["sca"]["end"]["x"] != null && jn["sca"]["end"]["x"] != null)
                    pulse.endScale = jn["sca"]["end"].AsVector2();
                //pulse.endScale = new Vector2(jn["sca"]["end"]["x"].AsFloat, jn["sca"]["end"]["y"].AsFloat);
                
                if (jn["sca"] != null && !string.IsNullOrEmpty(jn["sca"]["easing"]))
                    pulse.easingScale = jn["sca"]["easing"].AsInt;

                if (jn["rot"] != null && !string.IsNullOrEmpty(jn["rot"]["start"]))
                    pulse.startRotation = jn["rot"]["start"].AsFloat;
                
                if (jn["rot"] != null && !string.IsNullOrEmpty(jn["rot"]["end"]))
                    pulse.endRotation = jn["rot"]["end"].AsFloat;

                if (jn["rot"] != null && !string.IsNullOrEmpty(jn["rot"]["easing"]))
                    pulse.easingRotation = jn["rot"]["easing"].AsInt;

                if (!string.IsNullOrEmpty(jn["lt"]))
                    pulse.duration = jn["lt"].AsFloat;

                return pulse;
            }

            public JSONNode ToJSON()
            {
                var jn = JSON.Parse("{}");

                jn["active"] = active.ToString();

                if (shape.type != 0)
                    jn["s"] = shape.type.ToString();
                
                if (shape.option != 0)
                    jn["so"] = shape.option.ToString();

                jn["rothead"] = rotateToHead.ToString();

                jn["col"]["start"] = startColor.ToString();
                if (!string.IsNullOrEmpty(startCustomColor))
                    jn["col"]["starthex"] = startCustomColor.ToString();
                jn["col"]["end"] = endColor.ToString();
                if (!string.IsNullOrEmpty(endCustomColor))
                    jn["col"]["endhex"] = endCustomColor.ToString();
                jn["col"]["easing"] = easingColor.ToString();

                jn["opa"]["start"] = startOpacity.ToString();
                jn["opa"]["end"] = endOpacity.ToString();
                jn["opa"]["easing"] = easingOpacity.ToString();

                jn["d"] = depth.ToString();

                jn["pos"]["start"] = startPosition.ToJSON();
                jn["pos"]["end"] = endPosition.ToJSON();
                jn["pos"]["easing"] = easingPosition.ToString();
                
                jn["sca"]["start"] = startScale.ToJSON();
                jn["sca"]["end"] = endScale.ToJSON();
                jn["sca"]["easing"] = easingScale.ToString();

                jn["rot"]["start"] = startRotation.ToString();
                jn["rot"]["end"] = endRotation.ToString();
                jn["rot"]["easing"] = easingRotation.ToString();

                jn["lt"] = duration.ToString();

                return jn;
            }

            public bool active = false;

            public Shape shape = ShapeManager.inst.Shapes2D[0][0];

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
        }

        public class Bullet : Exists
        {
            public Bullet(PlayerModel playerModelRef) => this.playerModelRef = playerModelRef;

            PlayerModel playerModelRef;

            public static Bullet DeepCopy(PlayerModel playerModelRef, Bullet orig) => new Bullet(playerModelRef)
            {
                active = orig.active,
                autoKill = orig.autoKill,
                speed = orig.speed,
                lifeTime = orig.lifeTime,
                delay = orig.delay,
                constant = orig.constant,
                hurtPlayers = orig.hurtPlayers,
                origin = orig.origin,
                shape = Shape.DeepCopy(orig.shape),
                startColor = orig.startColor,
                endColor = orig.endColor,
                easingColor = orig.easingColor,
                durationColor = orig.durationColor,

                startOpacity = orig.startOpacity,
                endOpacity = orig.endOpacity,
                easingOpacity = orig.easingOpacity,
                durationOpacity = orig.durationOpacity,

                startCustomColor = orig.startCustomColor,
                endCustomColor = orig.endCustomColor,

                depth = orig.depth,

                startPosition = orig.startPosition,
                endPosition = orig.endPosition,
                easingPosition = orig.easingPosition,
                durationPosition = orig.durationPosition,

                startScale = orig.startScale,
                endScale = orig.endScale,
                easingScale = orig.easingScale,
                durationScale = orig.durationScale,

                startRotation = orig.startRotation,
                endRotation = orig.endRotation,
                easingRotation = orig.easingRotation,
                durationRotation = orig.durationRotation,
            };

            public static Bullet Parse(JSONNode jn, PlayerModel playerModel)
            {
                var bullet = new Bullet(playerModel);

                if (!string.IsNullOrEmpty(jn["active"]))
                    bullet.active = jn["active"].AsBool;

                if (!string.IsNullOrEmpty(jn["ak"]))
                    bullet.autoKill = jn["ak"].AsBool;

                if (!string.IsNullOrEmpty(jn["speed"]))
                    bullet.speed = jn["speed"].AsFloat;

                if (!string.IsNullOrEmpty(jn["lt"]))
                    bullet.lifeTime = jn["lt"].AsFloat;

                if (!string.IsNullOrEmpty(jn["delay"]))
                    bullet.delay = jn["delay"].AsFloat;

                if (!string.IsNullOrEmpty(jn["constant"]))
                    bullet.constant = jn["constant"].AsBool;

                if (!string.IsNullOrEmpty(jn["hit"]))
                    bullet.hurtPlayers = jn["hit"].AsBool;

                if (jn["o"] != null && !string.IsNullOrEmpty(jn["o"]["x"]) && !string.IsNullOrEmpty(jn["o"]["y"]))
                    bullet.origin = new Vector2(jn["o"]["x"].AsFloat, jn["o"]["y"].AsFloat);

                int bulletS = !string.IsNullOrEmpty(jn["s"]) ? jn["s"].AsInt : 0;
                int bulletSO = !string.IsNullOrEmpty(jn["so"]) ? jn["so"].AsInt : 0;

                bullet.shape = ShapeManager.inst.Shapes2D[bulletS][bulletSO];

                if (!string.IsNullOrEmpty(jn["col"]["start"]))
                    bullet.startColor = jn["col"]["start"].AsInt;
                if (!string.IsNullOrEmpty(jn["col"]["end"]))
                    bullet.endColor = jn["col"]["end"].AsInt;
                if (!string.IsNullOrEmpty(jn["col"]["easing"]))
                    bullet.easingColor = jn["col"]["easing"].AsInt;
                if (!string.IsNullOrEmpty(jn["col"]["dur"]))
                    bullet.durationColor = jn["col"]["dur"].AsFloat;

                if (!string.IsNullOrEmpty(jn["opa"]["start"]))
                    bullet.startOpacity = jn["opa"]["start"].AsFloat;
                if (!string.IsNullOrEmpty(jn["opa"]["end"]))
                    bullet.endOpacity = jn["opa"]["end"].AsFloat;
                if (!string.IsNullOrEmpty(jn["opa"]["easing"]))
                    bullet.easingOpacity = jn["opa"]["easing"].AsInt;
                if (!string.IsNullOrEmpty(jn["opa"]["dur"]))
                    bullet.durationOpacity = jn["opa"]["dur"].AsFloat;

                if (!string.IsNullOrEmpty(jn["col"]["starthex"]))
                    bullet.startCustomColor = jn["col"]["starthex"];
                if (!string.IsNullOrEmpty(jn["col"]["endhex"]))
                    bullet.endCustomColor = jn["col"]["endhex"];

                if (!string.IsNullOrEmpty(jn["d"]))
                    bullet.depth = jn["d"].AsFloat;

                if (!string.IsNullOrEmpty(jn["pos"]["start"]["x"]) && !string.IsNullOrEmpty(jn["pos"]["start"]["y"]))
                    bullet.startPosition = new Vector2(jn["pos"]["start"]["x"].AsFloat, jn["pos"]["start"]["y"].AsFloat);
                if (!string.IsNullOrEmpty(jn["pos"]["end"]["x"]) && !string.IsNullOrEmpty(jn["pos"]["end"]["y"]))
                    bullet.endPosition = new Vector2(jn["pos"]["end"]["x"].AsFloat, jn["pos"]["end"]["y"].AsFloat);
                if (!string.IsNullOrEmpty(jn["pos"]["easing"]))
                    bullet.easingPosition = jn["pos"]["easing"].AsInt;
                if (!string.IsNullOrEmpty(jn["pos"]["dur"]))
                    bullet.durationPosition = jn["pos"]["dur"].AsFloat;

                if (!string.IsNullOrEmpty(jn["sca"]["start"]["x"]) && !string.IsNullOrEmpty(jn["sca"]["start"]["y"]))
                    bullet.startScale = new Vector2(jn["sca"]["start"]["x"].AsFloat, jn["sca"]["start"]["y"].AsFloat);
                if (!string.IsNullOrEmpty(jn["sca"]["end"]["x"]) && !string.IsNullOrEmpty(jn["sca"]["end"]["y"]))
                    bullet.endScale = new Vector2(jn["sca"]["end"]["x"].AsFloat, jn["sca"]["end"]["y"].AsFloat);
                if (!string.IsNullOrEmpty(jn["sca"]["easing"]))
                    bullet.easingScale = jn["sca"]["easing"].AsInt;
                if (!string.IsNullOrEmpty(jn["sca"]["dur"]))
                    bullet.durationScale = jn["sca"]["dur"].AsFloat;

                if (!string.IsNullOrEmpty(jn["rot"]["start"]))
                    bullet.startRotation = jn["rot"]["start"].AsFloat;
                if (!string.IsNullOrEmpty(jn["rot"]["end"]))
                    bullet.endRotation = jn["rot"]["end"].AsFloat;
                if (!string.IsNullOrEmpty(jn["rot"]["easing"]))
                    bullet.easingRotation = jn["rot"]["easing"].AsInt;
                if (!string.IsNullOrEmpty(jn["rot"]["dur"]))
                    bullet.durationRotation = jn["rot"]["dur"].AsFloat;

                return bullet;
            }

            public JSONNode ToJSON()
            {
                var jn = JSON.Parse("{}");

                jn["active"] = active.ToString();

                jn["ak"] = autoKill.ToString();

                jn["speed"] = speed.ToString();

                jn["lt"] = lifeTime.ToString();

                jn["delay"] = delay.ToString();

                jn["constant"] = constant.ToString();

                jn["hit"] = hurtPlayers.ToString();

                jn["o"]["x"] = origin.x.ToString();
                jn["o"]["y"] = origin.y.ToString();

                if (shape.type != 0)
                    jn["s"] = shape.type.ToString();
                if (shape.option != 0)
                    jn["so"] = shape.option.ToString();

                jn["col"]["start"] = startColor.ToString();
                if (!string.IsNullOrEmpty(startCustomColor))
                    jn["col"]["starthex"] = startCustomColor;
                jn["col"]["end"] = endColor.ToString();
                if (!string.IsNullOrEmpty(endCustomColor))
                    jn["col"]["endhex"] = endCustomColor;
                jn["col"]["easing"] = easingColor.ToString();
                jn["col"]["dur"] = durationColor.ToString();

                jn["opa"]["start"] = startOpacity.ToString();
                jn["opa"]["end"] = endOpacity.ToString();
                jn["opa"]["easing"] = easingOpacity.ToString();
                jn["opa"]["dur"] = durationOpacity.ToString();

                jn["d"] = depth.ToString();

                jn["pos"]["start"]["x"] = startPosition.x.ToString();
                jn["pos"]["start"]["y"] = startPosition.y.ToString();
                jn["pos"]["end"]["x"] = endPosition.x.ToString();
                jn["pos"]["end"]["y"] = endPosition.y.ToString();
                jn["pos"]["easing"] = easingPosition.ToString();
                jn["pos"]["dur"] = durationPosition.ToString();

                jn["sca"]["start"]["x"] = startScale.x.ToString();
                jn["sca"]["start"]["y"] = startScale.y.ToString();
                jn["sca"]["end"]["x"] = endScale.x.ToString();
                jn["sca"]["end"]["y"] = endScale.y.ToString();
                jn["sca"]["easing"] = easingScale.ToString();
                jn["sca"]["dur"] = durationScale.ToString();

                jn["rot"]["start"] = startRotation.ToString();
                jn["rot"]["end"] = endRotation.ToString();
                jn["rot"]["easing"] = easingRotation.ToString();
                jn["rot"]["dur"] = durationRotation.ToString();

                return jn;
            }

            public bool active = false;

            public bool autoKill = true;

            public float speed = 6f;

            public float lifeTime = 1f;

            public float delay = 0.2f;

            public bool constant = true;

            public bool hurtPlayers = false;

            public Vector2 origin = Vector2.zero;

            public Shape shape = ShapeManager.inst.Shapes2D[0][0];

            public float depth = 0.1f;

            //public List<EventKeyframe> colorKeyframes = new List<EventKeyframe>()
            //{
            //    new EventKeyframe()
            //    {
            //        eventTime = 0f,
            //        eventValues = new float[7]
            //        {
            //            0,
            //            1f,
            //            1f,
            //            1f,
            //            1f,
            //            1f,
            //            1f,
            //        }
            //    },
            //    new EventKeyframe()
            //    {
            //        curveType = DataManager.inst.AnimationList[4],
            //        eventTime = 1f,
            //        eventValues = new float[7]
            //        {
            //            23,
            //            1f,
            //            1f,
            //            1f,
            //            1f,
            //            1f,
            //            1f,
            //        }
            //    }
            //};

            //public List<EventKeyframe> opacityKeyframes = new List<EventKeyframe>()
            //{
            //    new EventKeyframe()
            //    {
            //        eventTime = 0f,
            //        eventValues = new float[]
            //        {
            //            1f,
            //        }
            //    },
            //    new EventKeyframe()
            //    {
            //        curveType = DataManager.inst.AnimationList[3],
            //        eventTime = 1f,
            //        eventValues = new float[]
            //        {
            //            1f,
            //        }
            //    },
            //};

            //public List<EventKeyframe> positionKeyframes = new List<EventKeyframe>()
            //{
            //    new EventKeyframe()
            //    {
            //        eventTime = 0f,
            //        eventValues = new float[]
            //        {
            //            0f,
            //            0f
            //        }
            //    },
            //    new EventKeyframe()
            //    {
            //        curveType = DataManager.inst.AnimationList[4],
            //        eventTime = 1f,
            //        eventValues = new float[]
            //        {
            //            0f,
            //            0f
            //        }
            //    },
            //};

            //public List<EventKeyframe> scaleKeyframes = new List<EventKeyframe>()
            //{
            //    new EventKeyframe()
            //    {
            //        eventTime = 0f,
            //        eventValues = new float[]
            //        {
            //            0f,
            //            0f
            //        }
            //    },
            //    new EventKeyframe()
            //    {
            //        curveType = DataManager.inst.AnimationList[4],
            //        eventTime = 0.1f,
            //        eventValues = new float[]
            //        {
            //            3f,
            //            1f
            //        }
            //    },
            //};

            //public List<EventKeyframe> rotationKeyframes = new List<EventKeyframe>()
            //{
            //    new EventKeyframe()
            //    {
            //        eventTime = 0f,
            //        eventValues = new float[]
            //        {
            //            0f,
            //        }
            //    },
            //    new EventKeyframe()
            //    {
            //        curveType = DataManager.inst.AnimationList[3],
            //        eventTime = 1f,
            //        eventValues = new float[]
            //        {
            //            0f,
            //        }
            //    },
            //};

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
        }

        public class TailBase : Exists
        {
            public TailBase(PlayerModel playerModelRef) => this.playerModelRef = playerModelRef;

            PlayerModel playerModelRef;

            public static TailBase DeepCopy(PlayerModel playerModelRef, TailBase orig) => new TailBase(playerModelRef)
            {
                distance = orig.distance,
                mode = orig.mode,
                grows = orig.grows,
                time = orig.time,
            };

            public static TailBase Parse(JSONNode jn, PlayerModel playerModel)
            {
                var tailBase = new TailBase(playerModel);

                if (!string.IsNullOrEmpty(jn["distance"]))
                    tailBase.distance = jn["distance"].AsFloat;
                
                if (!string.IsNullOrEmpty(jn["mode"]))
                    tailBase.mode = (TailMode)jn["mode"].AsInt;

                if (!string.IsNullOrEmpty(jn["grows"]))
                    tailBase.grows = jn["grows"].AsBool;

                if (!string.IsNullOrEmpty(jn["time"]))
                    tailBase.time = jn["time"].AsFloat;
                if (tailBase.time == 0f)
                    tailBase.time = 200f;

                return tailBase;
            }

            public JSONNode ToJSON()
            {
                var jn = JSON.Parse("{}");

                if (distance != 2f)
                    jn["distance"] = distance.ToString();

                jn["mode"] = ((int)mode).ToString();

                jn["grows"] = grows.ToString();

                jn["time"] = time.ToString();

                return jn;
            }

            public float distance = 2f;

            public TailMode mode;

            public bool grows;

            public float time = 200f;

            public enum TailMode
            {
                Legacy,
                DevPlus
            }
        }

        public class CustomObject : Generic
        {
            public CustomObject(PlayerModel playerModelRef) : base(playerModelRef) => this.playerModelRef = playerModelRef;

            PlayerModel playerModelRef;

            public static CustomObject DeepCopy(PlayerModel playerModelRef, CustomObject orig, bool newID = true) => new CustomObject(playerModelRef)
            {
                shape = orig.shape,
                text = orig.text,
                polygonShape = orig.polygonShape.Copy(),
                position = orig.position,
                scale = orig.scale,
                rotation = orig.rotation,
                color = orig.color,
                customColor = orig.customColor,
                opacity = orig.opacity,
                Trail = Trail.DeepCopy(playerModelRef, orig.Trail),
                Particles = Particles.DeepCopy(playerModelRef, orig.Particles),
                id = newID ? LSText.randomNumString(16) : orig.id,
                name = orig.name,
                depth = orig.depth,
                parent = orig.parent,
                customParent = orig.customParent,
                positionOffset = orig.positionOffset,
                scaleOffset = orig.scaleOffset,
                rotationOffset = orig.rotationOffset,
                rotationParent = orig.rotationParent,
                scaleParent = orig.scaleParent,
                active = orig.active,
                requireAll = orig.requireAll,
                visibilitySettings = new List<Visiblity>(orig.visibilitySettings.Select(x => new Visiblity
                {
                    command = x.command,
                    not = x.not,
                    value = x.value
                })),
                animations = orig.animations.Select(x => PAAnimation.DeepCopy(x)).ToList(),
            };

            public static new CustomObject Parse(JSONNode jn, PlayerModel playerModel)
            {
                var customObject = new CustomObject(playerModel);

                int s = 0;
                int so = 0;

                if (!string.IsNullOrEmpty(jn["s"]))
                    s = jn["s"].AsInt;

                if (!string.IsNullOrEmpty(jn["so"]))
                    so = jn["so"].AsInt;

                customObject.shape = ShapeManager.inst.Shapes2D[s][so];

                if (jn["csp"] != null)
                    customObject.polygonShape = PolygonShape.Parse(jn["csp"]);

                if (!string.IsNullOrEmpty(jn["t"]))
                    customObject.text = jn["t"];

                if (jn["pos"] != null && !string.IsNullOrEmpty(jn["pos"]["x"]) && !string.IsNullOrEmpty(jn["pos"]["y"]))
                    customObject.position = new Vector2(jn["pos"]["x"].AsFloat, jn["pos"]["y"].AsFloat);

                if (jn["sca"] != null && !string.IsNullOrEmpty(jn["sca"]["x"]) && !string.IsNullOrEmpty(jn["sca"]["y"]))
                    customObject.scale = new Vector2(jn["sca"]["x"].AsFloat, jn["sca"]["y"].AsFloat);

                if (jn["rot"] != null && !string.IsNullOrEmpty(jn["rot"]["x"]))
                    customObject.rotation = jn["rot"]["x"].AsFloat;

                if (jn["col"] != null && !string.IsNullOrEmpty(jn["col"]["x"]))
                    customObject.color = jn["col"]["x"].AsInt;

                if (jn["col"] != null && !string.IsNullOrEmpty(jn["col"]["hex"]))
                    customObject.customColor = jn["col"]["hex"];

                if (jn["opa"] != null && !string.IsNullOrEmpty(jn["opa"]["x"]))
                    customObject.opacity = jn["opa"]["x"].AsFloat;

                if (jn["trail"] != null)
                    customObject.Trail = Trail.Parse(jn["trail"], playerModel);
                if (jn["particles"] != null)
                    customObject.Particles = Particles.Parse(jn["particles"], playerModel);

                if (!string.IsNullOrEmpty(jn["id"]))
                    customObject.id = jn["id"];
                
                if (!string.IsNullOrEmpty(jn["n"]))
                    customObject.name = jn["n"];

                if (!string.IsNullOrEmpty(jn["d"]))
                    customObject.depth = jn["d"].AsFloat;

                customObject.parent = jn["p"].AsInt;
                customObject.customParent = jn["idp"];

                if (!string.IsNullOrEmpty(jn["ppo"]))
                    customObject.positionOffset = jn["ppo"].AsFloat;
                
                if (!string.IsNullOrEmpty(jn["pso"]))
                    customObject.scaleOffset = jn["pso"].AsFloat;
                
                if (!string.IsNullOrEmpty(jn["pro"]))
                    customObject.rotationOffset = jn["pro"].AsFloat;
                
                if (!string.IsNullOrEmpty(jn["psa"]))
                    customObject.scaleParent = jn["psa"].AsBool;
                
                if (!string.IsNullOrEmpty(jn["pra"]))
                    customObject.rotationParent = jn["pra"].AsBool;

                int visible = -1;
                if (!string.IsNullOrEmpty(jn["v"]))
                    visible = jn["v"].AsInt;

                bool not = false;
                if (!string.IsNullOrEmpty(jn["vn"]))
                    not = jn["vn"].AsBool;

                float visibleValue = 0f;
                if (!string.IsNullOrEmpty(jn["vhp"]))
                    visibleValue = jn["vhp"].AsFloat;

                switch (visible)
                {
                    case 0:
                        {
                            customObject.active = true;
                            break;
                        } // always
                    case 1:
                        {
                            customObject.visibilitySettings.Add(new Visiblity
                            {
                                command = "isBoosting",
                                not = not,
                                value = visibleValue
                            });
                            break;
                        } // isBoosting
                    case 2:
                        {
                            customObject.visibilitySettings.Add(new Visiblity
                            {
                                command = "isTakingHit",
                                not = not,
                                value = visibleValue
                            });
                            break;
                        } // isTakingHit
                    case 3:
                        {
                            customObject.visibilitySettings.Add(new Visiblity
                            {
                                command = "isZenMode",
                                not = not,
                                value = visibleValue
                            });
                            break;
                        } // isZenMode
                    case 4:
                        {
                            customObject.visibilitySettings.Add(new Visiblity
                            {
                                command = "isHealthPercentageGreaterEquals",
                                not = not,
                                value = visibleValue
                            });
                            break;
                        } // isHealthPercentageGreater
                    case 5:
                        {
                            customObject.visibilitySettings.Add(new Visiblity
                            {
                                command = "isHealthGreaterEquals",
                                not = not,
                                value = visibleValue
                            });
                            break;
                        } // isHealthGreaterEquals
                    case 6:
                        {
                            customObject.visibilitySettings.Add(new Visiblity
                            {
                                command = "isHealthEquals",
                                not = not,
                                value = visibleValue
                            });
                            break;
                        } // isHealthEquals
                    case 7:
                        {
                            customObject.visibilitySettings.Add(new Visiblity
                            {
                                command = "isHealthGreater",
                                not = not,
                                value = visibleValue
                            });
                            break;
                        } // isHealthGreater
                    case 8:
                        {
                            customObject.visibilitySettings.Add(new Visiblity
                            {
                                command = "isPressingKey",
                                not = not,
                                value = visibleValue
                            });
                            break;
                        } // isPressingKey
                }

                if (!string.IsNullOrEmpty(jn["req_all"]))
                    customObject.requireAll = jn["req_all"].AsBool;

                for (int i = 0; i < jn["visible"].Count; i++)
                {
                    var visiblity = new Visiblity();
                    visiblity.command = jn["visible"][i]["cmd"];
                    visiblity.not = jn["visible"][i]["not"].AsBool;
                    visiblity.value = jn["visible"][i]["val"].AsFloat;
                    customObject.visibilitySettings.Add(visiblity);
                }

                if (jn["anims"] != null)
                {
                    for (int i = 0; i < jn["anims"].Count; i++)
                        customObject.animations.Add(PAAnimation.Parse(jn["anims"][i]));
                }
                
                return customObject;
            }

            public new JSONNode ToJSON()
            {
                var jn = JSON.Parse("{}");

                if (shape.type != 0)
                    jn["s"] = shape.type.ToString();
                if (shape.option != 0)
                    jn["so"] = shape.option.ToString();

                if (!string.IsNullOrEmpty(text))
                    jn["t"] = text;
                if (polygonShape)
                    jn["csp"] = polygonShape.ToJSON();

                jn["pos"]["x"] = position.x.ToString();
                jn["pos"]["y"] = position.y.ToString();

                jn["sca"]["x"] = scale.x.ToString();
                jn["sca"]["y"] = scale.y.ToString();

                jn["rot"]["x"] = rotation.ToString();

                jn["col"]["x"] = color.ToString();

                if (color == 24 && customColor != "FFFFFF")
                    jn["col"]["hex"] = customColor;

                if (opacity != 1f)
                    jn["opa"]["x"] = opacity.ToString();

                jn["trail"] = Trail.ToJSON();
                jn["particles"] = Particles.ToJSON();

                jn["id"] = id;
                if (!string.IsNullOrEmpty(name))
                    jn["n"] = name;
                jn["d"] = depth.ToString();
                jn["p"] = parent;
                if (!string.IsNullOrEmpty(customParent))
                    jn["idp"] = customParent;
                jn["ppo"] = positionOffset.ToString();
                jn["pso"] = scaleOffset.ToString();
                jn["pro"] = rotationOffset.ToString();
                jn["psa"] = scaleParent.ToString();
                jn["pra"] = rotationParent.ToString();

                jn["req_all"] = requireAll.ToString();

                for (int i = 0; i < visibilitySettings.Count; i++)
                {
                    jn["visible"][i]["cmd"] = visibilitySettings[i].command;
                    jn["visible"][i]["not"] = visibilitySettings[i].not.ToString();
                    jn["visible"][i]["val"] = visibilitySettings[i].value.ToString();
                }

                for (int i = 0; i < animations.Count; i++)
                    jn["anims"][i] = animations[i].ToJSON();

                return jn;
            }

            public string id;
            public string name;

            public float depth = 0.1f;

            public string customParent;
            public int parent;

            public float positionOffset = 1f;

            public float scaleOffset = 1f;

            public float rotationOffset = 1f;

            public bool scaleParent = true;

            public bool rotationParent = true;

            public bool requireAll;

            public List<Visiblity> visibilitySettings = new List<Visiblity>();
            public class Visiblity
            {
                public Visiblity() { }

                public bool not;
                public string command = "";
                public float value;
            }

            public List<PAAnimation> animations = new List<PAAnimation>();
        }
    }
}
