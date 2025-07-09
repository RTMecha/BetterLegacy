using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Menus;
using BetterLegacy.Menus.UI.Interfaces;

// ignore naming styles since modifiers are named like this.
#pragma warning disable IDE1006 // Naming Styles
namespace BetterLegacy.Core.Helpers
{
    /// <summary>
    /// Library of modifier actions.
    /// </summary>
    public static class ModifierActions
    {
        #region Audio

        public static void setPitch<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (RTLevel.Current.eventEngine)
                RTLevel.Current.eventEngine.pitchOffset = modifier.GetFloat(0, 0f, variables);
        }

        public static void addPitch<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (RTLevel.Current.eventEngine)
                RTLevel.Current.eventEngine.pitchOffset += modifier.GetFloat(0, 0f, variables);
        }

        public static void setPitchMath<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IEvaluatable evaluatable)
                return;

            var numberVariables = evaluatable.GetObjectVariables();
            if (variables != null)
            {
                foreach (var variable in variables)
                {
                    if (float.TryParse(variable.Value, out float num))
                        numberVariables[variable.Key] = num;
                }
            }

            if (RTLevel.Current.eventEngine)
                RTLevel.Current.eventEngine.pitchOffset = RTMath.Parse(modifier.GetValue(0, variables), numberVariables);
        }

        public static void addPitchMath<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IEvaluatable evaluatable)
                return;

            var numberVariables = evaluatable.GetObjectVariables();
            if (variables != null)
            {
                foreach (var variable in variables)
                {
                    if (float.TryParse(variable.Value, out float num))
                        numberVariables[variable.Key] = num;
                }
            }

            if (RTLevel.Current.eventEngine)
                RTLevel.Current.eventEngine.pitchOffset += RTMath.Parse(modifier.GetValue(0, variables), numberVariables);
        }

        public static void animatePitch<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var time = modifier.GetFloat(0, 0f, variables);
            var pitch = modifier.GetFloat(1, 0f, variables);
            var relative = modifier.GetBool(2, true, variables);

            string easing = modifier.GetValue(3, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var setPitch = pitch;
            if (relative)
            {
                if (modifier.constant)
                    setPitch *= CoreHelper.TimeFrame;

                setPitch += AudioManager.inst.CurrentAudioSource.pitch;
            }

            if (!modifier.constant)
            {
                var animation = new RTAnimation("Animate Object Offset");

                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, AudioManager.inst.CurrentAudioSource.pitch, Ease.Linear),
                        new FloatKeyframe(Mathf.Clamp(time, 0f, 9999f), setPitch, Ease.GetEaseFunction(easing, Ease.Linear)),
                    }, x => RTLevel.Current.eventEngine.pitchOffset = x, interpolateOnComplete: true),
                };
                animation.SetDefaultOnComplete(false);
                AnimationManager.inst.Play(animation);
                return;
            }

            RTLevel.Current.eventEngine.pitchOffset = setPitch;
        }

        public static void setMusicTime<T>(Modifier<T> modifier, Dictionary<string, string> variables) => AudioManager.inst.SetMusicTime(modifier.GetFloat(0, 0f, variables));

        public static void setMusicTimeMath<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IEvaluatable evaluatable)
                return;

            var numberVariables = evaluatable.GetObjectVariables();
            if (variables != null)
            {
                foreach (var variable in variables)
                {
                    if (float.TryParse(variable.Value, out float num))
                        numberVariables[variable.Key] = num;
                }
            }

            AudioManager.inst.SetMusicTime(RTMath.Parse(modifier.GetValue(0, variables), numberVariables));
        }
        
        public static void setMusicTimeStartTime<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is ILifetime<AutoKillType> lifeTime)
                AudioManager.inst.SetMusicTime(lifeTime.StartTime);
        }
        
        public static void setMusicTimeAutokill<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is ILifetime<AutoKillType> lifeTime)
                AudioManager.inst.SetMusicTime(lifeTime.StartTime + lifeTime.SpawnDuration);
        }

        public static void setMusicPlaying<T>(Modifier<T> modifier, Dictionary<string, string> variables) => SoundManager.inst.SetPlaying(modifier.GetBool(0, false, variables));

        public static void playSound<T>(Modifier<T> modifier, Dictionary<string, string> variables) where T : PAObject<T>, new()
        {
            if (modifier.reference is not PAObject<T> obj)
                return;

            var path = modifier.GetValue(0, variables);
            var global = modifier.GetBool(1, false, variables);
            var pitch = modifier.GetFloat(2, 1f, variables);
            var vol = modifier.GetFloat(3, 1f, variables);
            var loop = modifier.GetBool(4, false, variables);

            if (GameData.Current && GameData.Current.assets.sounds.TryFind(x => x.name == path, out SoundAsset soundAsset) && soundAsset.audio)
            {
                ModifiersHelper.PlaySound(obj.id, soundAsset.audio, pitch, vol, loop);
                return;
            }

            ModifiersHelper.GetSoundPath(obj.id, path, global, pitch, vol, loop);
        }

        public static void playSoundOnline<T>(Modifier<T> modifier, Dictionary<string, string> variables) where T : PAObject<T>, new()
        {
            if (modifier.reference is not PAObject<T> obj)
                return;

            var url = modifier.GetValue(0, variables);
            var pitch = modifier.GetFloat(1, 1f, variables);
            var vol = modifier.GetFloat(2, 1f, variables);
            var loop = modifier.GetBool(3, false, variables);

            if (!string.IsNullOrEmpty(url))
                ModifiersHelper.DownloadSoundAndPlay(obj.id, url, pitch, vol, loop);
        }

        public static void playDefaultSound<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var pitch = modifier.GetFloat(1, 1f, variables);
            var vol = modifier.GetFloat(2, 1f, variables);
            var loop = modifier.GetBool(3, false, variables);

            if (!AudioManager.inst.library.soundClips.TryGetValue(modifier.GetValue(0), out AudioClip[] audioClips))
                return;

            var clip = audioClips[UnityEngine.Random.Range(0, audioClips.Length)];
            var audioSource = Camera.main.gameObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.playOnAwake = true;
            audioSource.loop = loop;
            audioSource.pitch = pitch * AudioManager.inst.CurrentAudioSource.pitch;
            audioSource.volume = vol * AudioManager.inst.sfxVol;
            audioSource.Play();

            float x = pitch * AudioManager.inst.CurrentAudioSource.pitch;
            if (x == 0f)
                x = 1f;
            if (x < 0f)
                x = -x;

            if (!loop)
                CoroutineHelper.StartCoroutine(AudioManager.inst.DestroyWithDelay(audioSource, clip.length / x));
            else if (modifier.reference is PAObjectBase obj && !ModifiersManager.audioSources.ContainsKey(obj.id))
                ModifiersManager.audioSources.Add(obj.id, audioSource);
        }

        public static void audioSource(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var levelObject = modifier.reference.runtimeObject;
            if (!levelObject || !levelObject.visualObject ||
                !levelObject.visualObject.gameObject)
                return;

            if (modifier.TryGetResult(out AudioModifier audioModifier))
            {
                audioModifier.pitch = modifier.GetFloat(2, 1f, variables);
                audioModifier.volume = modifier.GetFloat(3, 1f, variables);
                audioModifier.loop = modifier.GetBool(4, true, variables);
                audioModifier.timeOffset = modifier.GetBool(6, true, variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(5, 0f, variables) : modifier.GetFloat(5, 0f, variables);
                audioModifier.lengthOffset = modifier.GetFloat(7, 0f, variables);
                audioModifier.playing = modifier.GetBool(8, true, variables);
                return;
            }

            var path = modifier.GetValue(0, variables);

            string fullPath =
                !bool.TryParse(modifier.GetValue(1, variables), out bool global) || !global ?
                RTFile.CombinePaths(RTFile.BasePath, path) :
                RTFile.CombinePaths(RTFile.ApplicationDirectory, ModifiersManager.SOUNDLIBRARY_PATH, path);

            var audioDotFormats = RTFile.AudioDotFormats;
            for (int i = 0; i < audioDotFormats.Length; i++)
            {
                var audioDotFormat = audioDotFormats[i];
                if (!path.EndsWith(audioDotFormat) && RTFile.FileExists(fullPath + audioDotFormat))
                    fullPath += audioDotFormat;
            }

            if (!RTFile.FileExists(fullPath))
            {
                CoreHelper.LogError($"File does not exist {fullPath}");
                return;
            }

            if (fullPath.EndsWith(FileFormat.MP3.Dot()))
            {
                modifier.Result = levelObject.visualObject.gameObject.AddComponent<AudioModifier>();
                ((AudioModifier)modifier.Result).Init(LSAudio.CreateAudioClipUsingMP3File(fullPath), modifier.reference, modifier);
                return;
            }

            CoroutineHelper.StartCoroutine(ModifiersHelper.LoadMusicFileRaw(fullPath, audioClip =>
            {
                if (!audioClip)
                {
                    CoreHelper.LogError($"Failed to load audio {fullPath}");
                    return;
                }

                audioClip.name = path;

                if (!levelObject.visualObject || !levelObject.visualObject.gameObject)
                    return;

                modifier.Result = levelObject.visualObject.gameObject.AddComponent<AudioModifier>();
                ((AudioModifier)modifier.Result).Init(audioClip, modifier.reference, modifier);
            }));
        }

        #endregion

        #region Level

        public static void loadLevel<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var path = modifier.GetValue(0, variables);

            if (CoreHelper.IsEditing)
            {
                if (!EditorConfig.Instance.ModifiersCanLoadLevels.Value)
                    return;

                RTEditor.inst.ShowWarningPopup($"You are about to enter the level {path}, are you sure you want to continue? Any unsaved progress will be lost!", () =>
                {
                    string str = RTFile.BasePath;
                    if (EditorConfig.Instance.ModifiersSavesBackup.Value)
                    {
                        GameData.Current.SaveData(str + "level-modifier-backup.lsb", () =>
                        {
                            EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(RTFile.RemoveEndSlash(str))}", 2f, EditorManager.NotificationType.Success);
                        });
                    }

                    EditorLevelManager.inst.LoadLevel(new Level(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath, path)));
                }, RTEditor.inst.HideWarningPopup);

                return;
            }

            if (CoreHelper.InEditor)
                return;

            var levelPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, LevelManager.ListSlash, $"{path}");
            if (RTFile.FileExists(RTFile.CombinePaths(levelPath, Level.LEVEL_LSB)) || RTFile.FileExists(RTFile.CombinePaths(levelPath, Level.LEVEL_VGD)) || RTFile.FileExists(levelPath + FileFormat.ASSET.Dot()))
                LevelManager.Load(levelPath);
            else
                SoundManager.inst.PlaySound(DefaultSounds.Block);
        }

        public static void loadLevelID<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(0, variables);
            if (string.IsNullOrEmpty(id) || id == "0" || id == "-1")
                return;

            if (!CoreHelper.InEditor)
            {
                if (LevelManager.Levels.TryFind(x => x.id == modifier.value, out Level level))
                    LevelManager.Play(level);
                else
                    SoundManager.inst.PlaySound(DefaultSounds.Block);

                return;
            }

            if (!CoreHelper.IsEditing)
                return;

            if (EditorLevelManager.inst.LevelPanels.TryFind(x => x.Level && x.Level.metadata is MetaData metaData && metaData.ID == modifier.value, out LevelPanel editorWrapper))
            {
                if (!EditorConfig.Instance.ModifiersCanLoadLevels.Value)
                    return;

                var path = System.IO.Path.GetFileName(editorWrapper.FolderPath);

                RTEditor.inst.ShowWarningPopup($"You are about to enter the level {path}, are you sure you want to continue? Any unsaved progress will be lost!", () =>
                {
                    string str = RTFile.BasePath;
                    if (EditorConfig.Instance.ModifiersSavesBackup.Value)
                    {
                        GameData.Current.SaveData(str + "level-modifier-backup.lsb", () =>
                        {
                            EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(RTFile.RemoveEndSlash(str))}", 2f, EditorManager.NotificationType.Success);
                        });
                    }

                    EditorLevelManager.inst.LoadLevel(editorWrapper.Level);
                }, RTEditor.inst.HideWarningPopup);
            }
            else
                SoundManager.inst.PlaySound(DefaultSounds.Block);
        }

        public static void loadLevelInternal<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var path = modifier.GetValue(0, variables);

            if (!CoreHelper.InEditor)
            {
                var filePath = RTFile.CombinePaths(RTFile.BasePath, path);
                if (!CoreHelper.InEditor && (RTFile.FileExists(RTFile.CombinePaths(filePath, Level.LEVEL_LSB)) || RTFile.FileIsFormat(RTFile.CombinePaths(filePath, Level.LEVEL_VGD)) || RTFile.FileExists(filePath + FileFormat.ASSET.Dot())))
                    LevelManager.Load(filePath);
                else
                    SoundManager.inst.PlaySound(DefaultSounds.Block);

                return;
            }

            if (CoreHelper.IsEditing && RTFile.FileExists(RTFile.CombinePaths(RTFile.BasePath, EditorManager.inst.currentLoadedLevel, path, Level.LEVEL_LSB)))
            {
                if (!EditorConfig.Instance.ModifiersCanLoadLevels.Value)
                    return;

                RTEditor.inst.ShowWarningPopup($"You are about to enter the level {RTFile.CombinePaths(EditorManager.inst.currentLoadedLevel, path)}, are you sure you want to continue? Any unsaved progress will be lost!", () =>
                {
                    string str = RTFile.BasePath;
                    if (EditorConfig.Instance.ModifiersSavesBackup.Value)
                    {
                        GameData.Current.SaveData(RTFile.CombinePaths(str, "level-modifier-backup.lsb"), () =>
                        {
                            EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(RTFile.RemoveEndSlash(str))}", 2f, EditorManager.NotificationType.Success);
                        });
                    }

                    EditorLevelManager.inst.LoadLevel(new Level(RTFile.CombinePaths(EditorManager.inst.currentLoadedLevel, path)));
                }, RTEditor.inst.HideWarningPopup);
            }
        }

        public static void loadLevelPrevious<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor)
                return;

            LevelManager.Play(LevelManager.PreviousLevel);
        }

        public static void loadLevelHub<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor)
                return;

            LevelManager.Play(LevelManager.Hub);
        }

        public static void loadLevelInCollection<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(0, variables);
            if (!CoreHelper.InEditor && LevelManager.CurrentLevelCollection && LevelManager.CurrentLevelCollection.levels.TryFind(x => x.id == id, out Level level))
                LevelManager.Play(level);
        }

        public static void downloadLevel<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var levelInfo = new LevelInfo(modifier.GetValue(0, variables), modifier.GetValue(0, variables), modifier.GetValue(1, variables), modifier.GetValue(2, variables), modifier.GetValue(3, variables), modifier.GetValue(4, variables));

            LevelCollection.DownloadLevel(null, levelInfo, level =>
            {
                if (modifier.GetBool(5, true, variables))
                    LevelManager.Play(level);
            });
        }

        public static void endLevel<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor)
            {
                EditorManager.inst.DisplayNotification("End level func", 1f, EditorManager.NotificationType.Success);
                return;
            }

            var endLevelFunc = modifier.GetInt(0, 0, variables);

            if (endLevelFunc > 0)
            {
                RTBeatmap.Current.endLevelFunc = (EndLevelFunction)(endLevelFunc - 1);
                RTBeatmap.Current.endLevelData = modifier.GetValue(1, variables);
            }
            RTBeatmap.Current.endLevelUpdateProgress = modifier.GetBool(2, true, variables);

            LevelManager.EndLevel();
        }
        
        public static void setAudioTransition<T>(Modifier<T> modifier, Dictionary<string, string> variables) => LevelManager.songFadeTransition = modifier.GetFloat(0, 0.5f, variables);

        public static void setIntroFade<T>(Modifier<T> modifier, Dictionary<string, string> variables) => RTGameManager.doIntroFade = modifier.GetBool(0, true, variables);

        public static void setLevelEndFunc<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor)
                return;

            var endLevelFunc = modifier.GetInt(0, 0, variables);

            if (endLevelFunc > 0)
            {
                RTBeatmap.Current.endLevelFunc = (EndLevelFunction)(endLevelFunc - 1);
                RTBeatmap.Current.endLevelData = modifier.GetValue(1, variables);
            }
            RTBeatmap.Current.endLevelUpdateProgress = modifier.GetBool(2, true, variables);
        }

        #endregion

        #region Component

        public static void blur(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            if (modifier.reference.objectType == BeatmapObject.ObjectType.Empty)
                return;

            var runtimeObject = modifier.reference.runtimeObject;

            if (!runtimeObject || !runtimeObject.visualObject.renderer)
                return;

            var amount = modifier.GetFloat(0, 0f, variables);
            var renderer = runtimeObject.visualObject.renderer;

            if (!modifier.HasResult())
            {
                var onDestroy = runtimeObject.visualObject.gameObject.AddComponent<DestroyModifierResult>();
                onDestroy.Modifier = modifier;
                modifier.Result = runtimeObject.visualObject.gameObject;
                renderer.material = LegacyResources.blur;
            }

            if (modifier.commands.Count > 1 && modifier.GetBool(1, false, variables))
                renderer.material.SetFloat("_blurSizeXY", -(modifier.reference.Interpolate(3, 1) - 1f) * amount);
            else
                renderer.material.SetFloat("_blurSizeXY", amount);
        }
        
        public static void blurOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));
            if (list.IsEmpty())
                return;

            var amount = modifier.GetFloat(0, 0f, variables);

            foreach (var beatmapObject in list)
            {
                var runtimeObject = beatmapObject.runtimeObject;
                if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty || !runtimeObject || !runtimeObject.visualObject.renderer)
                    continue;

                var renderer = runtimeObject.visualObject.renderer;

                if (renderer.material != LegacyResources.blur)
                    renderer.material = LegacyResources.blur;
                renderer.material.SetFloat("_blurSizeXY", -(beatmapObject.Interpolate(3, 1) - 1f) * amount);
            }
        }
        
        public static void blurVariable(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            if (modifier.reference.objectType == BeatmapObject.ObjectType.Empty)
                return;

            var runtimeObject = modifier.reference.runtimeObject;

            if (!runtimeObject || !runtimeObject.visualObject.renderer)
                return;

            var amount = modifier.GetFloat(0, 0f, variables);
            var renderer = runtimeObject.visualObject.renderer;

            if (!modifier.HasResult())
            {
                var onDestroy = runtimeObject.visualObject.gameObject.AddComponent<DestroyModifierResult>();
                onDestroy.Modifier = modifier;
                modifier.Result = runtimeObject.visualObject.gameObject;
                renderer.material = LegacyResources.blur;
            }

            renderer.material.SetFloat("_blurSizeXY", modifier.reference.integerVariable * amount);
        }
        
        public static void blurVariableOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1));
            if (list.IsEmpty())
                return;

            var amount = modifier.GetFloat(0, 0f, variables);

            foreach (var beatmapObject in list)
            {
                var runtimeObject = beatmapObject.runtimeObject;
                if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty || !runtimeObject || !runtimeObject.visualObject.renderer)
                    continue;

                var renderer = runtimeObject.visualObject.renderer;

                if (renderer.material != LegacyResources.blur)
                    renderer.material = LegacyResources.blur;
                renderer.material.SetFloat("_blurSizeXY", beatmapObject.integerVariable * amount);
            }
        }
        
        public static void blurColored(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            if (modifier.reference.objectType == BeatmapObject.ObjectType.Empty)
                return;

            var runtimeObject = modifier.reference.runtimeObject;

            if (!runtimeObject || !runtimeObject.visualObject.renderer)
                return;

            var amount = modifier.GetFloat(0, 0f, variables);
            var renderer = runtimeObject.visualObject.renderer;

            if (!modifier.HasResult())
            {
                var onDestroy = runtimeObject.visualObject.gameObject.AddComponent<DestroyModifierResult>();
                onDestroy.Modifier = modifier;
                modifier.Result = runtimeObject.visualObject.gameObject;
                renderer.material.shader = LegacyResources.blurColored;
            }

            if (modifier.commands.Count > 1 && modifier.GetBool(1, false, variables))
                renderer.material.SetFloat("_Size", -(modifier.reference.Interpolate(3, 1) - 1f) * amount);
            else
                renderer.material.SetFloat("_Size", amount);
        }
        
        public static void blurColoredOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));
            if (list.IsEmpty())
                return;

            var amount = modifier.GetFloat(0, 0f, variables);

            foreach (var beatmapObject in list)
            {
                var runtimeObject = beatmapObject.runtimeObject;
                if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty || !runtimeObject || !runtimeObject.visualObject.renderer)
                    continue;

                var renderer = runtimeObject.visualObject.renderer;

                if (renderer.material != LegacyResources.blurColored)
                    renderer.material.shader = LegacyResources.blurColored;
                renderer.material.SetFloat("_Size", -(beatmapObject.Interpolate(3, 1) - 1f) * amount);
            }
        }
        
        public static void doubleSided(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var runtimeObject = modifier.reference.runtimeObject;
            if (runtimeObject && runtimeObject.visualObject is SolidObject solidObject && solidObject.gameObject)
                solidObject.UpdateRendering((int)modifier.reference.gradientType, (int)modifier.reference.renderLayerType, true, modifier.reference.gradientScale, modifier.reference.gradientRotation);
        }
        
        public static void particleSystem(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var runtimeObject = modifier.reference.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            var gameObject = runtimeObject.visualObject.gameObject;

            if (modifier.Result is not ParticleSystem a || !a)
            {
                var ps = gameObject.GetOrAddComponent<ParticleSystem>();
                var psr = gameObject.GetComponent<ParticleSystemRenderer>();

                var s = modifier.GetInt(1, 0, variables);
                var so = modifier.GetInt(2, 0, variables);

                s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                psr.mesh = ObjectManager.inst.objectPrefabs[s == 4 ? 0 : s == 6 ? 0 : s].options[so].GetComponentInChildren<MeshFilter>().mesh;

                psr.material = GameManager.inst.PlayerPrefabs[0].transform.GetChild(0).GetChild(0).GetComponent<TrailRenderer>().material;
                psr.material.color = Color.white;
                psr.trailMaterial = psr.material;
                psr.renderMode = ParticleSystemRenderMode.Mesh;

                var psMain = ps.main;

                psMain.simulationSpace = ParticleSystemSimulationSpace.World;

                var rotationOverLifetime = ps.rotationOverLifetime;
                rotationOverLifetime.enabled = true;
                rotationOverLifetime.separateAxes = true;
                rotationOverLifetime.xMultiplier = 0f;
                rotationOverLifetime.yMultiplier = 0f;

                var forceOverLifetime = ps.forceOverLifetime;
                forceOverLifetime.enabled = true;
                forceOverLifetime.space = ParticleSystemSimulationSpace.World;

                modifier.Result = ps;
                gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
            }

            if (modifier.Result is ParticleSystem particleSystem && particleSystem)
            {
                var ps = particleSystem;

                var psMain = ps.main;
                var psEmission = ps.emission;

                psMain.startSpeed = modifier.GetFloat(9, 5f, variables);

                psMain.loop = modifier.constant;
                ps.emissionRate = modifier.GetFloat(10, 1f, variables);
                //psEmission.burstCount = modifier.GetInt(16, 1, variables);
                psMain.duration = modifier.GetFloat(11, 1f, variables);

                var rotationOverLifetime = ps.rotationOverLifetime;
                rotationOverLifetime.zMultiplier = modifier.GetFloat(8, 0f, variables);

                var forceOverLifetime = ps.forceOverLifetime;
                forceOverLifetime.xMultiplier = modifier.GetFloat(12, 0f, variables);
                forceOverLifetime.yMultiplier = modifier.GetFloat(13, 0f, variables);

                var particlesTrail = ps.trails;
                particlesTrail.enabled = modifier.GetBool(14, true, variables);

                var colorOverLifetime = ps.colorOverLifetime;
                colorOverLifetime.enabled = true;
                var psCol = colorOverLifetime.color;

                float alphaStart = modifier.GetFloat(4, 1f, variables);
                float alphaEnd = modifier.GetFloat(5, 0f, variables);

                psCol.gradient.alphaKeys = new GradientAlphaKey[2] { new GradientAlphaKey(alphaStart, 0f), new GradientAlphaKey(alphaEnd, 1f) };
                psCol.gradient.colorKeys = new GradientColorKey[2] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) };
                psCol.gradient.mode = GradientMode.Blend;

                colorOverLifetime.color = psCol;

                var sizeOverLifetime = ps.sizeOverLifetime;
                sizeOverLifetime.enabled = true;

                var ssss = sizeOverLifetime.size;

                var sizeStart = modifier.GetFloat(6, 0f, variables);
                var sizeEnd = modifier.GetFloat(7, 0f, variables);

                var curve = new AnimationCurve(new Keyframe[2] { new Keyframe(0f, sizeStart), new Keyframe(1f, sizeEnd) });

                ssss.curve = curve;

                sizeOverLifetime.size = ssss;

                psMain.startLifetime = modifier.GetFloat(0, 1f, variables);
                psEmission.enabled = !(gameObject.transform.lossyScale.x < 0.001f && gameObject.transform.lossyScale.x > -0.001f || gameObject.transform.lossyScale.y < 0.001f && gameObject.transform.lossyScale.y > -0.001f) && gameObject.activeSelf && gameObject.activeInHierarchy;

                psMain.startColor = CoreHelper.CurrentBeatmapTheme.GetObjColor(modifier.GetInt(3, 0, variables));

                var shape = ps.shape;
                shape.angle = modifier.GetFloat(15, 90f, variables);

                if (!modifier.constant)
                    RTLevel.Current.postTick.Enqueue(() => ps.Emit(modifier.GetInt(16, 1, variables)));
            }
        }
        
        public static void trailRenderer(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var runtimeObject = modifier.reference.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            var gameObject = runtimeObject.visualObject.gameObject;

            if (!modifier.reference.trailRenderer)
            {
                modifier.reference.trailRenderer = gameObject.GetOrAddComponent<TrailRenderer>();

                modifier.reference.trailRenderer.material = GameManager.inst.PlayerPrefabs[0].transform.GetChild(0).GetChild(0).GetComponent<TrailRenderer>().material;
                modifier.reference.trailRenderer.material.color = Color.white;
            }
            else
            {
                var tr = modifier.reference.trailRenderer;

                tr.time = modifier.GetFloat(0, 1f, variables);
                tr.emitting = !(gameObject.transform.lossyScale.x < 0.001f && gameObject.transform.lossyScale.x > -0.001f || gameObject.transform.lossyScale.y < 0.001f && gameObject.transform.lossyScale.y > -0.001f) && gameObject.activeSelf && gameObject.activeInHierarchy;

                var t = gameObject.transform.lossyScale.magnitude * 0.576635f;
                tr.startWidth = modifier.GetFloat(1, 1f, variables) * t;
                tr.endWidth = modifier.GetFloat(2, 1f, variables) * t;

                var beatmapTheme = CoreHelper.CurrentBeatmapTheme;

                tr.startColor = RTColors.FadeColor(beatmapTheme.GetObjColor(modifier.GetInt(3, 0, variables)), modifier.GetFloat(4, 1f, variables));
                tr.endColor = RTColors.FadeColor(beatmapTheme.GetObjColor(modifier.GetInt(5, 0, variables)), modifier.GetFloat(6, 1f, variables));
            }
        }
        
        public static void trailRendererHex(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var runtimeObject = modifier.reference.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            var gameObject = runtimeObject.visualObject.gameObject;

            if (!modifier.reference.trailRenderer)
            {
                modifier.reference.trailRenderer = gameObject.GetOrAddComponent<TrailRenderer>();

                modifier.reference.trailRenderer.material = GameManager.inst.PlayerPrefabs[0].transform.GetChild(0).GetChild(0).GetComponent<TrailRenderer>().material;
                modifier.reference.trailRenderer.material.color = Color.white;
            }
            else
            {
                var tr = modifier.reference.trailRenderer;

                tr.time = modifier.GetFloat(0, 1f, variables);
                tr.emitting = !(gameObject.transform.lossyScale.x < 0.001f && gameObject.transform.lossyScale.x > -0.001f || gameObject.transform.lossyScale.y < 0.001f && gameObject.transform.lossyScale.y > -0.001f) && gameObject.activeSelf && gameObject.activeInHierarchy;

                var t = gameObject.transform.lossyScale.magnitude * 0.576635f;
                tr.startWidth = modifier.GetFloat(1, 1f, variables) * t;
                tr.endWidth = modifier.GetFloat(2, 1f, variables) * t;

                tr.startColor = RTColors.HexToColor(modifier.GetValue(3, variables));
                tr.endColor = RTColors.HexToColor(modifier.GetValue(4, variables));
            }
        }

        public static void rigidbody(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var runtimeObject = modifier.reference.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            var gravity = modifier.GetFloat(1, 0f, variables);
            var collisionMode = modifier.GetInt(2, 0, variables);
            var drag = modifier.GetFloat(3, 0f, variables);
            var velocityX = modifier.GetFloat(4, 0f, variables);
            var velocityY = modifier.GetFloat(5, 0f, variables);
            var bodyType = modifier.GetInt(6, 0, variables);

            if (!modifier.reference.rigidbody)
                modifier.reference.rigidbody = runtimeObject.visualObject.gameObject.GetOrAddComponent<Rigidbody2D>();

            modifier.reference.rigidbody.gravityScale = gravity;
            modifier.reference.rigidbody.collisionDetectionMode = (CollisionDetectionMode2D)Mathf.Clamp(collisionMode, 0, 1);
            modifier.reference.rigidbody.drag = drag;

            modifier.reference.rigidbody.bodyType = (RigidbodyType2D)Mathf.Clamp(bodyType, 0, 2);

            modifier.reference.rigidbody.velocity += new Vector2(velocityX, velocityY);
        }
        
        public static void rigidbodyOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(0, variables));
            if (list.IsEmpty())
                return;

            var gravity = modifier.GetFloat(1, 0f, variables);
            var collisionMode = modifier.GetInt(2, 0, variables);
            var drag = modifier.GetFloat(3, 0f, variables);
            var velocityX = modifier.GetFloat(4, 0f, variables);
            var velocityY = modifier.GetFloat(5, 0f, variables);
            var bodyType = modifier.GetInt(6, 0, variables);

            foreach (var beatmapObject in list)
            {
                var runtimeObject = beatmapObject.runtimeObject;
                if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty || !runtimeObject || !runtimeObject.visualObject.renderer)
                    continue;

                if (!beatmapObject.rigidbody)
                    beatmapObject.rigidbody = runtimeObject.visualObject.gameObject.GetOrAddComponent<Rigidbody2D>();

                beatmapObject.rigidbody.gravityScale = gravity;
                beatmapObject.rigidbody.collisionDetectionMode = (CollisionDetectionMode2D)Mathf.Clamp(collisionMode, 0, 1);
                beatmapObject.rigidbody.drag = drag;

                beatmapObject.rigidbody.bodyType = (RigidbodyType2D)Mathf.Clamp(bodyType, 0, 2);

                beatmapObject.rigidbody.velocity += new Vector2(velocityX, velocityY);
            }
        }

        #endregion

        #region Player

        public static void playerHit(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference || RTBeatmap.Current.Invincible || modifier.constant)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.Player)
                    player.Player.Hit(Mathf.Clamp(modifier.GetInt(0, 1, variables), 0, int.MaxValue));
            });
        }
        
        public static void playerHitIndex<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant)
                return;

            var damage = Mathf.Clamp(modifier.GetInt(1, 1, variables), 0, int.MaxValue);
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out CustomPlayer customPlayer) && customPlayer.Player)
                customPlayer.Player.Hit(damage);
        }
        
        public static void playerHitAll<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant)
                return;

            var damage = Mathf.Clamp(modifier.GetInt(0, 1, variables), 0, int.MaxValue);
            foreach (var player in PlayerManager.Players.Where(x => x.Player))
                player.Player.Hit(damage);
        }
        
        public static void playerHeal(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference || RTBeatmap.Current.Invincible || modifier.constant)
                return;

            var heal = Mathf.Clamp(modifier.GetInt(0, 1, variables), 0, int.MaxValue);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.Player)
                    player.Player.Heal(heal);
            });
        }
        
        public static void playerHealIndex<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant)
                return;

            var health = Mathf.Clamp(modifier.GetInt(1, 1, variables), 0, int.MaxValue);
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out CustomPlayer customPlayer) && customPlayer.Player)
                customPlayer.Player.Heal(health);
        }

        public static void playerHealAll<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant)
                return;

            var heal = Mathf.Clamp(modifier.GetInt(0, 1, variables), 0, int.MaxValue);
            bool healed = false;
            foreach (var player in PlayerManager.Players)
            {
                if (player.Player)
                    if (player.Player.Heal(heal, false))
                        healed = true;
            }

            if (healed)
                SoundManager.inst.PlaySound(DefaultSounds.HealPlayer);
        }
        
        public static void playerKill(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference || RTBeatmap.Current.Invincible || modifier.constant)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.Player)
                    player.Player.Kill();
            });
        }
        
        public static void playerKillIndex<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant)
                return;

            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out CustomPlayer customPlayer) && customPlayer.Player)
                customPlayer.Player.Kill();
        }
        
        public static void playerKillAll<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant)
                return;

            foreach (var player in PlayerManager.Players)
                if (player.Player)
                    player.Player.Kill();
        }
        
        public static void playerRespawn(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference || modifier.constant)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                var playerIndex = PlayerManager.GetClosestPlayerIndex(pos);

                if (playerIndex >= 0)
                    PlayerManager.RespawnPlayer(playerIndex);
            });
        }
        
        public static void playerRespawnIndex<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.constant)
                return;

            PlayerManager.RespawnPlayer(modifier.GetInt(0, 0));
        }
        
        public static void playerRespawnAll<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.constant)
                PlayerManager.RespawnPlayers();
        }
        
        public static void playerMove(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                var value = modifier.GetValue(0);

                Vector2 vector;
                if (value.Contains(','))
                {
                    var axis = modifier.value.Split(',');
                    modifier.SetValue(0, axis[0]);
                    modifier.commands.RemoveAt(modifier.commands.Count - 1);
                    modifier.commands.Insert(1, axis[1]);
                    vector = new Vector2(Parser.TryParse(axis[0], 0f), Parser.TryParse(axis[1], 0f));
                }
                else
                    vector = new Vector2(modifier.GetFloat(0, 0f, variables), modifier.GetFloat(1, 0f, variables));

                bool relative = modifier.GetBool(4, false, variables);
                if (!player)
                    return;

                var tf = player.Player.rb.transform;
                if (modifier.constant)
                    tf.localPosition = vector;
                else
                {
                    string easing = modifier.GetValue(3, variables);
                    if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                        easing = DataManager.inst.AnimationList[e].Name;

                    var animation = new RTAnimation("Player Move");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                        {
                            new Vector2Keyframe(0f, tf.localPosition, Ease.Linear),
                            new Vector2Keyframe(modifier.GetFloat(2, 1f, variables), new Vector2(vector.x + (relative ? tf.localPosition.x : 0f), vector.y + (relative ? tf.localPosition.y : 0f)), Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, vector2 => tf.localPosition = vector2, interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete(false);
                    AnimationManager.inst.Play(animation);
                }
            });
        }
        
        public static void playerMoveIndex<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var vector = new Vector2(modifier.GetFloat(1, 0f, variables), modifier.GetFloat(2, 0f, variables));
            var duration = modifier.GetFloat(3, 0f, variables);

            string easing = modifier.GetValue(4, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var relative = modifier.GetBool(5, false, variables);

            if (!PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out CustomPlayer customPlayer))
                return;

            var tf = customPlayer.Player.rb.transform;
            if (modifier.constant)
                tf.localPosition = vector;
            else
            {
                var animation = new RTAnimation("Player Move");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                    {
                        new Vector2Keyframe(0f, tf.localPosition, Ease.Linear),
                        new Vector2Keyframe(duration, new Vector2(vector.x + (relative ? tf.localPosition.x : 0f), vector.y + (relative ? tf.position.y : 0f)), Ease.GetEaseFunction(easing, Ease.Linear)),
                    }, vector2 => tf.localPosition = vector2, interpolateOnComplete: true),
                };
                animation.SetDefaultOnComplete(false);
                AnimationManager.inst.Play(animation);
            }
        }

        public static void playerMoveAll<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var value = modifier.GetValue(0);

            Vector2 vector;
            if (value.Contains(','))
            {
                var axis = modifier.value.Split(',');
                modifier.SetValue(0, axis[0]);
                modifier.commands.RemoveAt(modifier.commands.Count - 1);
                modifier.commands.Insert(1, axis[1]);
                vector = new Vector2(Parser.TryParse(axis[0], 0f), Parser.TryParse(axis[1], 0f));
            }
            else
                vector = new Vector2(modifier.GetFloat(0, 0f, variables), modifier.GetFloat(1, 0f, variables));

            var duration = modifier.GetFloat(2, 1f, variables);

            string easing = modifier.GetValue(3, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            bool relative = modifier.GetBool(4, false, variables);
            foreach (var player in PlayerManager.Players.Where(x => x.Player))
            {
                var tf = player.Player.rb.transform;
                if (duration == 0f || modifier.constant)
                {
                    if (relative)
                        tf.localPosition += (Vector3)vector;
                    else
                        tf.localPosition = vector;
                }
                else
                {
                    var animation = new RTAnimation("Player Move");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                        {
                            new Vector2Keyframe(0f, tf.localPosition, Ease.Linear),
                            new Vector2Keyframe(duration, new Vector2(vector.x + (relative ? tf.localPosition.x : 0f), vector.y + (relative ? tf.position.y : 0f)), Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, vector2 => tf.localPosition = vector2, interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete(false);
                    AnimationManager.inst.Play(animation);
                }
            }
        }
        
        public static void playerMoveX(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            var value = modifier.GetFloat(0, 0f, variables);
            var duration = modifier.GetFloat(1, 1f, variables);
            string easing = modifier.GetValue(2, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            bool relative = modifier.GetBool(3, false, variables);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player)
                    return;

                var tf = player.Player.rb.transform;
                if (modifier.constant)
                {
                    var v = tf.localPosition;
                    if (relative)
                        v.x += value;
                    else
                        v.x = value;
                    tf.localPosition = v;
                }
                else
                {
                    var animation = new RTAnimation("Player Move");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, tf.localPosition.x, Ease.Linear),
                            new FloatKeyframe(duration, value + (relative ? tf.localPosition.x : 0f), Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, tf.SetLocalPositionX, interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete(false);
                    AnimationManager.inst.Play(animation);
                }
            });
        }

        public static void playerMoveXIndex<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var value = modifier.GetFloat(1, 0f, variables);
            var duration = modifier.GetFloat(2, 0f, variables);

            string easing = modifier.GetValue(3, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var relative = modifier.GetBool(4, false, variables);

            if (!PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out CustomPlayer customPlayer))
                return;

            var tf = customPlayer.Player.rb.transform;
            if (modifier.constant)
            {
                var v = tf.localPosition;
                if (relative)
                    v.x += value;
                else
                    v.x = value;
                tf.localPosition = v;
            }
            else
            {
                var animation = new RTAnimation("Player Move");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, tf.localPosition.x, Ease.Linear),
                        new FloatKeyframe(duration, value + (relative ? tf.localPosition.x : 0f), Ease.GetEaseFunction(easing, Ease.Linear)),
                    }, tf.SetLocalPositionX, interpolateOnComplete: true),
                };
                animation.SetDefaultOnComplete(false);
                AnimationManager.inst.Play(animation);
            }
        }

        public static void playerMoveXAll<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var value = modifier.GetFloat(0, 0f, variables);
            var duration = modifier.GetFloat(1, 1f, variables);
            string easing = modifier.GetValue(2, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            bool relative = modifier.GetBool(3, false, variables);
            foreach (var player in PlayerManager.Players.Where(x => x.Player))
            {
                var tf = player.Player.rb.transform;
                if (modifier.constant)
                {
                    var v = tf.localPosition;
                    if (relative)
                        v.x += value;
                    else
                        v.x = value;
                    tf.localPosition = v;
                }
                else
                {
                    var animation = new RTAnimation("Player Move");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, tf.localPosition.x, Ease.Linear),
                            new FloatKeyframe(duration, value + (relative ? tf.localPosition.x : 0f), Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, tf.SetLocalPositionX, interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete(false);
                    AnimationManager.inst.Play(animation);
                }
            }
        }
        
        public static void playerMoveY(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            var value = modifier.GetFloat(0, 0f, variables);
            var duration = modifier.GetFloat(1, 1f, variables);
            string easing = modifier.GetValue(2, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            bool relative = modifier.GetBool(3, false, variables);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player)
                    return;

                var tf = player.Player.rb.transform;
                if (modifier.constant)
                {
                    var v = tf.localPosition;
                    if (relative)
                        v.y += value;
                    else
                        v.y = value;
                    tf.localPosition = v;
                }
                else
                {
                    var animation = new RTAnimation("Player Move");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, tf.localPosition.y, Ease.Linear),
                            new FloatKeyframe(duration, value + (relative ? tf.localPosition.y : 0f), Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, tf.SetLocalPositionY, interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete(false);
                    AnimationManager.inst.Play(animation);
                }
            });
        }

        public static void playerMoveYIndex<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var value = modifier.GetFloat(1, 0f, variables);
            var duration = modifier.GetFloat(2, 0f, variables);

            string easing = modifier.GetValue(3, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var relative = modifier.GetBool(4, false, variables);

            if (!PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out CustomPlayer customPlayer))
                return;

            var tf = customPlayer.Player.rb.transform;
            if (modifier.constant)
            {
                var v = tf.localPosition;
                if (relative)
                    v.y += value;
                else
                    v.y = value;
                tf.localPosition = v;
            }
            else
            {
                var animation = new RTAnimation("Player Move");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, tf.localPosition.y, Ease.Linear),
                        new FloatKeyframe(duration, value + (relative ? tf.localPosition.y : 0f), Ease.GetEaseFunction(easing, Ease.Linear)),
                    }, tf.SetLocalPositionY, interpolateOnComplete: true),
                };
                animation.SetDefaultOnComplete(false);
                AnimationManager.inst.Play(animation);
            }
        }

        public static void playerMoveYAll<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var value = modifier.GetFloat(0, 0f, variables);
            var duration = modifier.GetFloat(1, 1f, variables);
            string easing = modifier.GetValue(2, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            bool relative = modifier.GetBool(3, false, variables);
            foreach (var player in PlayerManager.Players.Where(x => x.Player))
            {
                var tf = player.Player.rb.transform;
                if (modifier.constant)
                {
                    var v = tf.localPosition;
                    if (relative)
                        v.y += value;
                    else
                        v.y = value;
                    tf.localPosition = v;
                }
                else
                {
                    var animation = new RTAnimation("Player Move");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, tf.localPosition.y, Ease.Linear),
                            new FloatKeyframe(duration, value + (relative ? tf.localPosition.y : 0f), Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, tf.SetLocalPositionY, interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete(false);
                    AnimationManager.inst.Play(animation);
                }
            }
        }
        
        public static void playerRotate(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            var value = modifier.GetFloat(0, 0f, variables);
            string easing = modifier.GetValue(2, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;
            var duration = modifier.GetFloat(1, 1f, variables);
            bool relative = modifier.GetBool(3, false, variables);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player)
                    return;

                var tf = player.Player.rb.transform;
                if (modifier.constant)
                {
                    var v = tf.localRotation.eulerAngles;
                    if (relative)
                        v.z += value;
                    else
                        v.z = value;
                    tf.localRotation = Quaternion.Euler(v);
                }
                else
                {
                    var animation = new RTAnimation("Player Move");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, tf.localRotation.eulerAngles.z, Ease.Linear),
                            new FloatKeyframe(duration, value + (relative ? tf.localRotation.eulerAngles.z : 0f), Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, tf.SetLocalRotationEulerZ, interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete(false);
                    AnimationManager.inst.Play(animation);
                }
            });
        }

        public static void playerRotateIndex<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var value = modifier.GetFloat(1, 0f, variables);
            var duration = modifier.GetFloat(2, 0f, variables);

            string easing = modifier.GetValue(3, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var relative = modifier.GetBool(4, false, variables);

            if (!PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out CustomPlayer customPlayer))
                return;

            var tf = customPlayer.Player.rb.transform;
            if (modifier.constant)
            {
                var v = tf.localRotation.eulerAngles;
                if (relative)
                    v.z += value;
                else
                    v.z = value;
                tf.localRotation = Quaternion.Euler(v);
            }
            else
            {
                var animation = new RTAnimation("Player Move");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, tf.localRotation.eulerAngles.z, Ease.Linear),
                        new FloatKeyframe(duration, value + (relative ? tf.localRotation.eulerAngles.z : 0f), Ease.GetEaseFunction(easing, Ease.Linear)),
                    }, tf.SetLocalRotationEulerZ, interpolateOnComplete: true),
                };
                animation.SetDefaultOnComplete(false);
                AnimationManager.inst.Play(animation);
            }
        }

        public static void playerRotateAll<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var value = modifier.GetFloat(0, 0f, variables);
            string easing = modifier.GetValue(2, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;
            var duration = modifier.GetFloat(1, 1f, variables);

            bool relative = modifier.GetBool(3, false, variables);
            foreach (var player in PlayerManager.Players.Where(x => x.Player))
            {
                var tf = player.Player.rb.transform;
                if (modifier.constant)
                {
                    var v = tf.localRotation.eulerAngles;
                    if (relative)
                        v.z += value;
                    else
                        v.z = value;
                    tf.localRotation = Quaternion.Euler(v);
                }
                else
                {
                    var animation = new RTAnimation("Player Move");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, tf.localRotation.eulerAngles.z, Ease.Linear),
                            new FloatKeyframe(duration, value + (relative ? tf.localRotation.eulerAngles.z : 0f), Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, tf.SetLocalRotationEulerZ, interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete(false);
                    AnimationManager.inst.Play(animation);
                }
            }
        }
        
        public static void playerMoveToObject(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.Player || !player.Player.rb)
                    return;

                player.Player.rb.position = new Vector3(pos.x, pos.y, 0f);
            });
        }

        public static void playerMoveIndexToObject(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            var index = modifier.GetInt(0, 0, variables);
            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                if (!PlayerManager.Players.TryGetAt(index, out CustomPlayer player) || !player.Player || !player.Player.rb)
                    return;

                player.Player.rb.position = new Vector3(pos.x, pos.y, 0f);
            });
        }

        public static void playerMoveAllToObject(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.Player || !player.Player.rb)
                    return;

                var y = player.Player.rb.position.y;
                player.Player.rb.position = new Vector2(pos.x, y);
            });
        }
        
        public static void playerMoveXToObject(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.Player || !player.Player.rb)
                    return;

                var y = player.Player.rb.position.y;
                player.Player.rb.position = new Vector2(pos.x, y);
            });
        }

        public static void playerMoveXIndexToObject(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            var index = modifier.GetInt(0, 0, variables);
            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                if (!PlayerManager.Players.TryGetAt(index, out CustomPlayer player) || !player.Player || !player.Player.rb)
                    return;

                var y = player.Player.rb.position.y;
                player.Player.rb.position = new Vector2(pos.x, y);
            });
        }

        public static void playerMoveXAllToObject(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                foreach (var player in PlayerManager.Players)
                {
                    if (!player.Player || !player.Player.rb)
                        continue;

                    var y = player.Player.rb.position.y;
                    player.Player.rb.position = new Vector2(pos.x, y);
                }
            });
        }

        public static void playerMoveYToObject(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.Player || !player.Player.rb)
                    return;

                var x = player.Player.rb.position.x;
                player.Player.rb.position = new Vector2(x, pos.y);
            });
        }

        public static void playerMoveYIndexToObject(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            var index = modifier.GetInt(0, 0, variables);
            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                if (!PlayerManager.Players.TryGetAt(index, out CustomPlayer player) || !player.Player || !player.Player.rb)
                    return;

                var x = player.Player.rb.position.x;
                player.Player.rb.position = new Vector2(x, pos.y);
            });
        }

        public static void playerMoveYAllToObject(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                foreach (var player in PlayerManager.Players)
                {
                    if (!player.Player || !player.Player.rb)
                        continue;

                    var x = player.Player.rb.position.x;
                    player.Player.rb.position = new Vector2(x, pos.y);
                }
            });
        }

        public static void playerRotateToObject(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.Player || !player.Player.rb)
                    return;

                player.Player.rb.transform.SetLocalRotationEulerZ(modifier.reference.GetFullRotation().z);
            });
        }

        public static void playerRotateIndexToObject(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            var index = modifier.GetInt(0, 0, variables);
            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                if (!PlayerManager.Players.TryGetAt(index, out CustomPlayer player) || !player.Player || !player.Player.rb)
                    return;

                player.Player.rb.transform.SetLocalRotationEulerZ(modifier.reference.GetFullRotation().z);
            });
        }

        public static void playerRotateAllToObject(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var rot = modifier.reference.GetFullRotation().z;

                foreach (var player in PlayerManager.Players)
                {
                    if (!player.Player || !player.Player.rb)
                        continue;

                    player.Player.rb.transform.SetLocalRotationEulerZ(rot);
                }
            });
        }
        
        public static void playerBoost(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.constant || !modifier.reference)
                return;

            var xStr = modifier.GetValue(0, variables);
            var yStr = modifier.GetValue(1, variables);
            var shouldBoostX = false;
            var shouldBoostY = false;
            var x = 0f;
            var y = 0f;

            if (!string.IsNullOrEmpty(xStr))
            {
                shouldBoostX = true;
                x = Parser.TryParse(xStr, 0f);
            }

            if (!string.IsNullOrEmpty(yStr))
            {
                shouldBoostY = true;
                y = Parser.TryParse(yStr, 0f);
            }

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.Player)
                    return;

                if (shouldBoostX)
                    player.Player.lastMoveHorizontal = x;
                if (shouldBoostY)
                    player.Player.lastMoveVertical = y;
                player.Player.Boost();
            });
        }
        
        public static void playerBoostIndex<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var xStr = modifier.GetValue(0, variables);
            var yStr = modifier.GetValue(1, variables);
            var shouldBoostX = false;
            var shouldBoostY = false;
            var x = 0f;
            var y = 0f;

            if (!string.IsNullOrEmpty(xStr))
            {
                shouldBoostX = true;
                x = Parser.TryParse(xStr, 0f);
            }

            if (!string.IsNullOrEmpty(yStr))
            {
                shouldBoostY = true;
                y = Parser.TryParse(yStr, 0f);
            }

            if (!modifier.constant && PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out CustomPlayer player) && player.Player)
            {
                if (shouldBoostX)
                    player.Player.lastMoveHorizontal = x;
                if (shouldBoostY)
                    player.Player.lastMoveVertical = y;
                player.Player.Boost();
            }
        }
        
        public static void playerBoostAll<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var xStr = modifier.GetValue(0, variables);
            var yStr = modifier.GetValue(1, variables);
            var shouldBoostX = false;
            var shouldBoostY = false;
            var x = 0f;
            var y = 0f;

            if (!string.IsNullOrEmpty(xStr))
            {
                shouldBoostX = true;
                x = Parser.TryParse(xStr, 0f);
            }
            
            if (!string.IsNullOrEmpty(yStr))
            {
                shouldBoostY = true;
                y = Parser.TryParse(yStr, 0f);
            }

            foreach (var player in PlayerManager.Players.Where(x => x.Player))
            {
                if (shouldBoostX)
                    player.Player.lastMoveHorizontal = x;
                if (shouldBoostY)
                    player.Player.lastMoveVertical = y;
                player.Player.Boost();
            }
        }
        
        public static void playerCancelBoost(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.constant || !modifier.reference)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.Player && player.Player.CanCancelBoosting)
                    player.Player.StopBoosting();
            });
        }

        public static void playerCancelBoostIndex<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out CustomPlayer player) && player.Player && player.Player.CanCancelBoosting)
                player.Player.StopBoosting();
        }

        public static void playerCancelBoostAll<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            foreach (var player in PlayerManager.Players)
            {
                if (player && player.Player && player.Player.CanCancelBoosting)
                    player.Player.StopBoosting();
            }
        }

        public static void playerDisableBoost(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.Player)
                    player.Player.CanBoost = false;
            });
        }
        
        public static void playerDisableBoostIndex<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out CustomPlayer player) && player.Player)
                player.Player.CanBoost = false;
        }
        
        public static void playerDisableBoostAll<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            foreach (var player in PlayerManager.Players.Where(x => x.Player))
                player.Player.CanBoost = false;
        }
        
        public static void playerEnableBoost(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            var enabled = modifier.GetBool(0, true, variables);
            
            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.Player)
                    player.Player.CanBoost = enabled;
            });
        }
        
        public static void playerEnableBoostIndex<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(1, true, variables);

            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out CustomPlayer player) && player.Player)
                player.Player.CanBoost = enabled;
        }
        
        public static void playerEnableBoostAll<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(0, true, variables);

            foreach (var player in PlayerManager.Players.Where(x => x.Player))
                player.Player.CanBoost = enabled;
        }
        
        public static void playerEnableMove(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            var enabled = modifier.GetBool(0, true, variables);
            var rotate = modifier.GetBool(1, true, variables);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = modifier.reference.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.Player)
                    return;

                player.Player.CanMove = enabled;
                player.Player.CanRotate = rotate;
            });
        }

        public static void playerEnableMoveIndex<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(1, true, variables);
            var rotate = modifier.GetBool(2, true, variables);

            if (!PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out CustomPlayer player) || !player.Player)
                return;

            player.Player.CanMove = enabled;
            player.Player.CanRotate = rotate;
        }

        public static void playerEnableMoveAll<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(0, true, variables);
            var rotate = modifier.GetBool(1, true, variables);

            foreach (var player in PlayerManager.Players)
            {
                if (!player.Player)
                    continue;

                player.Player.CanMove = enabled;
                player.Player.CanRotate = rotate;
            }
        }

        public static void playerSpeed<T>(Modifier<T> modifier, Dictionary<string, string> variables) => RTPlayer.SpeedMultiplier = modifier.GetFloat(0, 1f, variables);

        public static void playerVelocity(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {

        }
        
        public static void playerVelocityIndex(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {

        }
        
        public static void playerVelocityAll<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var x = modifier.GetFloat(1, 0f, variables);
            var y = modifier.GetFloat(2, 0f, variables);

            for (int i = 0; i < PlayerManager.Players.Count; i++)
            {
                var player = PlayerManager.Players[i];
                if (player.Player && player.Player.rb)
                    player.Player.rb.velocity = new Vector2(x, y);
            }
        }

        public static void playerVelocityX(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {

        }

        public static void playerVelocityXIndex(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {

        }

        public static void playerVelocityXAll<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var x = modifier.GetFloat(0, 0f, variables);

            for (int i = 0; i < PlayerManager.Players.Count; i++)
            {
                var player = PlayerManager.Players[i];
                if (!player.Player || !player.Player.rb)
                    continue;

                var velocity = player.Player.rb.velocity;
                velocity.x = x;
                player.Player.rb.velocity = velocity;
            }
        }

        public static void playerVelocityY(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {

        }

        public static void playerVelocityYIndex(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {

        }

        public static void playerVelocityYAll<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var y = modifier.GetFloat(0, 0f, variables);

            for (int i = 0; i < PlayerManager.Players.Count; i++)
            {
                var player = PlayerManager.Players[i];
                if (!player.Player || !player.Player.rb)
                    continue;

                var velocity = player.Player.rb.velocity;
                velocity.y = y;
                player.Player.rb.velocity = velocity;
            }
        }
        
        public static void setPlayerModel<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.constant)
                return;

            var index = modifier.GetInt(1, 0, variables);

            if (!PlayersData.Current.playerModels.ContainsKey(modifier.GetValue(0, variables)))
                return;

            PlayersData.Current.SetPlayerModel(index, modifier.GetValue(0, variables));
            PlayerManager.AssignPlayerModels();

            if (!PlayerManager.Players.TryGetAt(index, out CustomPlayer customPlayer) || !customPlayer.Player)
                return;

            customPlayer.UpdatePlayerModel();

            customPlayer.Player.playerNeedsUpdating = true;
            customPlayer.Player.UpdateModel();
        }
        
        public static void setGameMode<T>(Modifier<T> modifier, Dictionary<string, string> variables) => RTPlayer.GameMode = (GameMode)modifier.GetInt(0, 0);
        
        public static void gameMode<T>(Modifier<T> modifier, Dictionary<string, string> variables) => RTPlayer.GameMode = (GameMode)modifier.GetInt(0, 0);

        public static void blackHole(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            float p = Time.deltaTime * 60f * CoreHelper.ForwardPitch;

            float num = modifier.GetFloat(0, 0.01f, variables);

            if (modifier.GetBool(1, false, variables))
                num = -(modifier.reference.Interpolate(3, 1) - 1f) * num;

            if (num == 0f)
                return;

            float moveDelay = 1f - Mathf.Pow(1f - Mathf.Clamp(num, 0.001f, 1f), p);
            var players = PlayerManager.Players;

            var pos = modifier.reference.GetFullPosition();

            players.ForLoop(player =>
            {
                if (!player.Player || !player.Player.rb)
                    return;

                var transform = player.Player.rb.transform;

                var vector = new Vector3(transform.position.x, transform.position.y, 0f);
                var target = new Vector3(pos.x, pos.y, 0f);

                transform.position += (target - vector) * moveDelay;
            });
        }

        #endregion

        #region Mouse Cursor

        public static void showMouse<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var value = modifier.GetValue(0, variables);
            if (value == "0")
                value = "True";
            var enabled = Parser.TryParse(value, true);

            if (enabled)
                CursorManager.inst.ShowCursor();
            else if (CoreHelper.InEditorPreview)
                CursorManager.inst.HideCursor();
        }

        public static void hideMouse<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditorPreview)
                CursorManager.inst.HideCursor();
        }

        public static void setMousePosition<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (CoreHelper.IsEditing)
                return;

            var screenScale = Display.main.systemWidth / 1920f;
            float windowCenterX = (Display.main.systemWidth) / 2;
            float windowCenterY = (Display.main.systemHeight) / 2;

            var x = modifier.GetFloat(1, 0f);
            var y = modifier.GetFloat(2, 0f);

            CursorManager.inst.SetCursorPosition(new Vector2((x * screenScale) + windowCenterX, (y * screenScale) + windowCenterY));
        }
        
        public static void followMousePosition<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.value == "0")
                modifier.value = "1";

            if (modifier.reference is not ITransformable transformable)
                return;

            Vector2 mousePosition = Input.mousePosition;
            mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);

            float p = Time.deltaTime * 60f;
            float po = 1f - Mathf.Pow(1f - Mathf.Clamp(modifier.GetFloat(0, 1f, variables), 0.001f, 1f), p);
            float ro = 1f - Mathf.Pow(1f - Mathf.Clamp(modifier.GetFloat(1, 1f, variables), 0.001f, 1f), p);

            if (modifier.Result == null)
                modifier.Result = Vector2.zero;

            var dragPos = (Vector2)modifier.Result;

            var target = new Vector2(mousePosition.x, mousePosition.y);

            transformable.RotationOffset = new Vector3(0f, 0f, (target.x - dragPos.x) * ro);

            dragPos += (target - dragPos) * po;

            modifier.Result = dragPos;

            transformable.PositionOffset = dragPos;
        }

        #endregion

        #region Variable

        // local variables
        public static void getToggle<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var value = modifier.GetBool(1, false, variables);

            if (modifier.GetBool(2, false, variables))
                value = !value;

            variables[modifier.GetValue(0)] = value.ToString();
        }
        
        public static void getFloat<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = modifier.GetFloat(1, 0f, variables).ToString();
        }
        
        public static void getInt<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = modifier.GetInt(1, 0, variables).ToString();
        }

        public static void getString<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = modifier.GetValue(1, variables);
        }
        
        public static void getStringLower<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = modifier.GetValue(1, variables).ToLower();
        }
        
        public static void getStringUpper<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = modifier.GetValue(1, variables).ToUpper();
        }

        public static void getColor<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = modifier.GetInt(1, 0, variables).ToString();
        }

        public static void getEnum<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var index = (modifier.GetInt(1, 0, variables) * 2) + 4;
            if (modifier.commands.Count > index)
                variables[modifier.GetValue(0)] = modifier.GetValue(index, variables).ToString();
        }

        public static void getTag<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = modifier.reference is IModifyable<T> modifyable && modifyable.Tags.TryGetAt(modifier.GetInt(1, 0, variables), out string tag) ? tag : string.Empty;
        }

        public static void getPitch<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = AudioManager.inst.CurrentAudioSource.pitch.ToString();
        }

        public static void getMusicTime<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = AudioManager.inst.CurrentAudioSource.time.ToString();
        }

        public static void getAxis(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            int fromType = modifier.GetInt(1, 0, variables);
            int fromAxis = modifier.GetInt(2, 0, variables);

            float delay = modifier.GetFloat(3, 0f, variables);
            float multiply = modifier.GetFloat(4, 0f, variables);
            float offset = modifier.GetFloat(5, 0f, variables);
            float min = modifier.GetFloat(6, -9999f, variables);
            float max = modifier.GetFloat(7, 9999f, variables);
            bool visual = modifier.GetBool(8, false, variables);
            float loop = modifier.GetFloat(9, 9999f, variables);

            if (!GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(10, variables), out BeatmapObject bm))
                return;

            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);

            if (fromType < 0 || fromType > 2)
                return;

            variables[modifier.GetValue(0)] = ModifiersHelper.GetAnimation(bm, fromType, fromAxis, min, max, offset, multiply, delay, loop, visual).ToString();
        }

        public static void getMath<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IEvaluatable evaluatable)
                return;

            try
            {
                var numberVariables = evaluatable.GetObjectVariables();
                ModifiersHelper.SetVariables(variables, numberVariables);

                variables[modifier.GetValue(0)] = RTMath.Parse(modifier.GetValue(1, variables), numberVariables, evaluatable.GetObjectFunctions()).ToString();
            }
            catch { }
        }

        public static void getNearestPlayer<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not ITransformable transformable)
                return;

            var pos = transformable.GetFullPosition();
            variables[modifier.GetValue(0)] = PlayerManager.GetClosestPlayerIndex(pos).ToString();
        }

        public static void getCollidingPlayers(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var runtimeObject = modifier.reference.runtimeObject;
            if (runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.collider)
            {
                var collider = runtimeObject.visualObject.collider;

                var players = PlayerManager.Players;
                for (int i = 0; i < players.Count; i++)
                {
                    var player = players[i];
                    variables[modifier.GetValue(0) + "_" + i] = (player.Player && player.Player.CurrentCollider && player.Player.CurrentCollider.IsTouching(collider)).ToString();
                }
            }
        }

        public static void getPlayerHealth<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(1, 0, variables), out CustomPlayer customPlayer))
                variables[modifier.GetValue(0)] = customPlayer.Health.ToString();
        }
        
        public static void getPlayerPosX<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(1, 0, variables), out CustomPlayer customPlayer) && customPlayer.Player && customPlayer.Player.rb)
                variables[modifier.GetValue(0)] = customPlayer.Player.rb.transform.position.x.ToString();
        }
        
        public static void getPlayerPosY<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(1, 0, variables), out CustomPlayer customPlayer) && customPlayer.Player && customPlayer.Player.rb)
                variables[modifier.GetValue(0)] = customPlayer.Player.rb.transform.position.y.ToString();
        }

        public static void getPlayerRot<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(1, 0, variables), out CustomPlayer customPlayer) && customPlayer.Player && customPlayer.Player.rb)
                variables[modifier.GetValue(0)] = customPlayer.Player.rb.transform.eulerAngles.z.ToString();
        }

        public static void getEventValue<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (!RTLevel.Current.eventEngine)
                return;

            float multiply = modifier.GetFloat(4, 0f, variables);
            float offset = modifier.GetFloat(5, 0f, variables);
            float min = modifier.GetFloat(6, -9999f, variables);
            float max = modifier.GetFloat(7, 9999f, variables);
            float loop = modifier.GetFloat(8, 9999f, variables);

            var value = RTLevel.Current.eventEngine.Interpolate(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables), RTLevel.Current.CurrentTime - modifier.GetFloat(3, 0f, variables));

            value = Mathf.Clamp((value - offset) * multiply % loop, min, max);

            variables[modifier.GetValue(0)] = value.ToString();
        }

        public static void getSample<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = RTLevel.Current.GetSample(modifier.GetInt(1, 0, variables), modifier.GetFloat(2, 1f, variables)).ToString();
        }

        public static void getText(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var useVisual = modifier.GetBool(1, false, variables);
            if (useVisual && modifier.reference.runtimeObject && modifier.reference.runtimeObject.visualObject is TextObject textObject)
                variables[modifier.GetValue(0)] = textObject.GetText();
            else
                variables[modifier.GetValue(0)] = modifier.reference.text;
        }

        public static void getTextOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(2, variables), out BeatmapObject beatmapObject))
                return;

            var useVisual = modifier.GetBool(1, false, variables);
            if (useVisual && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject is TextObject textObject)
                variables[modifier.GetValue(0)] = textObject.GetText();
            else
                variables[modifier.GetValue(0)] = beatmapObject.text;
        }

        public static void getCurrentKey<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = CoreHelper.GetKeyCodeDown().ToString();
        }

        public static void getColorSlotHexCode<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var color = ThemeManager.inst.Current.GetObjColor(modifier.GetInt(1, 0, variables));
            color = RTColors.FadeColor(color, modifier.GetFloat(2, 1f, variables));
            color = RTColors.ChangeColorHSV(color, modifier.GetFloat(3, 0f, variables), modifier.GetFloat(4, 0f, variables), modifier.GetFloat(5, 0f, variables));

            variables[modifier.GetValue(0)] = RTColors.ColorToHexOptional(color);
        }

        public static void getFloatFromHexCode<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = RTColors.HexToFloat(modifier.GetValue(1, variables)).ToString();
        }

        public static void getHexCodeFromFloat<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = RTColors.FloatToHex(modifier.GetFloat(1, 1f, variables));
        }

        public static void getJSONString<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (!RTFile.TryReadFromFile(ModifiersHelper.GetSaveFile(modifier.GetValue(1, variables)), out string json))
                return;

            var jn = JSON.Parse(json);

            var fjn = jn[modifier.GetValue(2, variables)][modifier.GetValue(3, variables)]["string"];

            variables[modifier.GetValue(0)] = fjn;
        }
        
        public static void getJSONFloat<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (!RTFile.TryReadFromFile(ModifiersHelper.GetSaveFile(modifier.GetValue(1, variables)), out string json))
                return;

            var jn = JSON.Parse(json);

            var fjn = jn[modifier.GetValue(2, variables)][modifier.GetValue(3, variables)]["float"];

            variables[modifier.GetValue(0)] = fjn;
        }

        public static void getJSON<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            try
            {
                var jn = JSON.Parse(modifier.GetValue(1, variables));
                var json1 = modifier.GetValue(2, variables);
                if (!string.IsNullOrEmpty(json1))
                    jn = jn[json1];

                variables[modifier.GetValue(0)] = jn;
            }
            catch { }
        }

        public static void getSubString<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            try
            {
                var str = modifier.GetValue(1, variables);
                var subString = str.Substring(Mathf.Clamp(modifier.GetInt(2, 0, variables), 0, str.Length), Mathf.Clamp(modifier.GetInt(3, 0, variables), 0, str.Length));
                variables[modifier.GetValue(0)] = subString;
            }
            catch { }
        }

        public static void getSplitString<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var str = modifier.GetValue(0, variables);
            var ch = modifier.GetValue(1, variables);

            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(ch))
                return;

            var split = str.Split(ch[0]);
            for (int i = 0; i < split.Length; i++)
            {
                var index = i + 2;
                if (modifier.commands.InRange(index))
                    variables[modifier.GetValue(index)] = split[i];
            }
        }
        
        public static void getSplitStringAt<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var str = modifier.GetValue(0, variables);
            var ch = modifier.GetValue(1, variables);

            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(ch))
                return;

            var split = str.Split(ch[0]);
            variables[modifier.GetValue(2)] = split.GetAt(modifier.GetInt(3, 0, variables));
        }
        
        public static void getSplitStringCount<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var str = modifier.GetValue(0, variables);
            var ch = modifier.GetValue(1, variables);

            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(ch))
                return;

            var split = str.Split(ch[0]);
            variables[modifier.GetValue(2)] = split.Length.ToString();
        }

        public static void getStringLength<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = modifier.GetValue(1, variables).Length.ToString();
        }

        public static void getParsedString<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = RTString.ParseText(modifier.GetValue(1, variables));
        }

        public static void getRegex<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var regex = new Regex(modifier.GetValue(0, variables));
            var match = regex.Match(modifier.GetValue(1, variables));

            if (!match.Success)
                return;

            for (int i = 0; i < match.Groups.Count; i++)
            {
                var index = i + 2;
                if (modifier.commands.InRange(index))
                    variables[modifier.commands[index]] = match.Groups[i].ToString();
            }
        }

        public static void getFormatVariable<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            try
            {
                object[] args = new object[modifier.commands.Count - 2];
                for (int i = 2; i < modifier.commands.Count; i++)
                    args[i - 2] = modifier.GetValue(i, variables);

                variables[modifier.GetValue(0)] = string.Format(modifier.GetValue(1, variables), args);
            }
            catch { }
        }

        public static void getComparison<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = (modifier.GetValue(1, variables) == modifier.GetValue(2, variables)).ToString();
        }

        public static void getComparisonMath<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IEvaluatable evaluatable)
                return;

            try
            {
                var numberVariables = evaluatable.GetObjectVariables();
                var functions = evaluatable.GetObjectFunctions();

                variables[modifier.GetValue(0)] = (RTMath.Parse(modifier.GetValue(1, variables), numberVariables, functions) == RTMath.Parse(modifier.GetValue(2, variables), numberVariables, functions)).ToString();
            }
            catch { }
        }

        public static void getModifiedColor<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var color = RTColors.HexToColor(modifier.GetValue(1, variables));

            variables[modifier.GetValue(0)] = RTColors.ColorToHexOptional(RTColors.FadeColor(RTColors.ChangeColorHSV(color,
                    modifier.GetFloat(3, 0f, variables),
                    modifier.GetFloat(4, 0f, variables),
                    modifier.GetFloat(5, 0f, variables)), modifier.GetFloat(2, 1f, variables)));
        }

        public static void getMixedColors<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var colors = new List<Color>();
            for (int i = 1; i < modifier.commands.Count; i++)
                colors.Add(RTColors.HexToColor(modifier.GetValue(1, variables)));

            variables[modifier.GetValue(0)] = RTColors.MixColors(colors).ToString();
        }

        public static void getVisualColor(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference.runtimeObject && modifier.reference.runtimeObject.visualObject is SolidObject solidObject)
            {
                var colors = solidObject.GetColors();
                variables[modifier.GetValue(0)] = RTColors.ColorToHexOptional(colors.startColor);
                variables[modifier.GetValue(1)] = RTColors.ColorToHexOptional(colors.endColor);
            }
        }

        public static void getFloatAnimationKF<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var audioTime = modifier.GetFloat(1, 0f, variables);
            var type = modifier.GetInt(2, 0, variables);

            Sequence<float> sequence;

            // get cache
            if (modifier.HasResult() && modifier.GetBool(3, true, variables))
                sequence = modifier.GetResult<Sequence<float>>();
            else
            {
                var value = modifier.GetFloat(4, 0f, variables);

                var currentTime = 0f;

                var keyframes = new List<IKeyframe<float>>();
                keyframes.Add(new FloatKeyframe(currentTime, value, Ease.Linear));
                for (int i = 5; i < modifier.commands.Count; i += 4)
                {
                    var time = modifier.GetFloat(i, 0f, variables);
                    if (time < currentTime)
                        continue;

                    var x = modifier.GetFloat(i + 1, 0f, variables);
                    var relative = modifier.GetBool(i + 2, true, variables);

                    var easing = modifier.GetValue(i + 3, variables);
                    if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                        easing = DataManager.inst.AnimationList[e].Name;

                    var setvalue = x;
                    if (relative)
                        setvalue += value;

                    keyframes.Add(new FloatKeyframe(currentTime + time, setvalue, Ease.GetEaseFunction(easing, Ease.Linear)));

                    value = setvalue;
                    currentTime = time;
                }

                sequence = new Sequence<float>(keyframes);
                modifier.Result = sequence;
            }

            if (sequence != null)
                variables[modifier.GetValue(0)] = sequence.Interpolate(audioTime).ToString();
        }

        public static void getSignaledVariables<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.Result is Dictionary<string, string> otherVariables)
            {
                foreach (var variable in otherVariables)
                    variables[variable.Key] = variable.Value;

                if (!modifier.GetBool(0, true, variables)) // don't clear
                    return;

                otherVariables.Clear();
                modifier.Result = null;
            }
        }

        public static void signalLocalVariables<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(0, variables));

            if (list.IsEmpty())
                return;

            var sendVariables = new Dictionary<string, string>(variables);

            foreach (var beatmapObject in list)
            {
                beatmapObject.modifiers.FindAll(x => x.Name == nameof(getSignaledVariables)).ForLoop(modifier =>
                {
                    if (modifier.TryGetResult(out Dictionary<string, string> otherVariables))
                    {
                        foreach (var variable in sendVariables)
                            otherVariables[variable.Key] = variable.Value;
                        return;
                    }

                    modifier.Result = sendVariables;
                });
            }
        }

        public static void clearLocalVariables<T>(Modifier<T> modifier, Dictionary<string, string> variables) => variables.Clear();

        // object variable
        public static void addVariable<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.commands.Count == 2)
            {
                if (modifier.reference is not IPrefabable prefabable)
                    return;

                var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(1, variables));
                if (list.IsEmpty())
                    return;

                int num = modifier.GetInt(0, 0, variables);

                foreach (var beatmapObject in list)
                    beatmapObject.integerVariable += num;
            }
            else if (modifier.reference is IModifyable<T> modifyable)
                modifyable.IntVariable += modifier.GetInt(0, 0, variables);
        }
        
        public static void addVariableOther<T>(Modifier<T> modifier, Dictionary<string, string> variables) where T : IPrefabable
        {
            var prefabable = modifier.reference as IPrefabable;
            var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(1, variables));
            if (list.IsEmpty())
                return;

            int num = modifier.GetInt(0, 0, variables);

            foreach (var beatmapObject in list)
                beatmapObject.integerVariable += num;
        }
        
        public static void subVariable<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.commands.Count == 2)
            {
                if (modifier.reference is not IPrefabable prefabable)
                    return;

                var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(1, variables));
                if (list.IsEmpty())
                    return;

                int num = modifier.GetInt(0, 0, variables);

                foreach (var beatmapObject in list)
                    beatmapObject.integerVariable -= num;
            }
            else if (modifier.reference is IModifyable<T> modifyable)
                modifyable.IntVariable -= modifier.GetInt(0, 0, variables);
        }

        public static void subVariableOther<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(1, variables));
            if (list.IsEmpty())
                return;

            int num = modifier.GetInt(0, 0, variables);

            foreach (var beatmapObject in list)
                beatmapObject.integerVariable -= num;
        }

        public static void setVariable<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.commands.Count == 2)
            {
                if (modifier.reference is not IPrefabable prefabable)
                    return;

                var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(1, variables));
                if (list.IsEmpty())
                    return;

                int num = modifier.GetInt(0, 0, variables);

                foreach (var beatmapObject in list)
                    beatmapObject.integerVariable = num;
            }
            else if (modifier.reference is IModifyable<T> modifyable)
                modifyable.IntVariable = modifier.GetInt(0, 0, variables);
        }
        
        public static void setVariableOther<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(1, variables));
            if (list.IsEmpty())
                return;

            int num = modifier.GetInt(0, 0, variables);

            foreach (var beatmapObject in list)
                beatmapObject.integerVariable = num;
        }
        
        public static void setVariableRandom<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.commands.Count == 3)
            {
                if (modifier.reference is not IPrefabable prefabable)
                    return;

                var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(0, variables));
                if (list.IsEmpty())
                    return;

                int min = modifier.GetInt(1, 0, variables);
                int max = modifier.GetInt(2, 0, variables);

                foreach (var beatmapObject in list)
                    beatmapObject.integerVariable = UnityEngine.Random.Range(min, max < 0 ? max - 1 : max + 1);
            }
            else if (modifier.reference is IModifyable<T> modifyable)
            {
                var min = modifier.GetInt(0, 0, variables);
                var max = modifier.GetInt(1, 0, variables);
                modifyable.IntVariable = UnityEngine.Random.Range(min, max < 0 ? max - 1 : max + 1);
            }
        }
        
        public static void setVariableRandomOther<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(0, variables));
            if (list.IsEmpty())
                return;

            int min = modifier.GetInt(1, 0, variables);
            int max = modifier.GetInt(2, 0, variables);

            foreach (var beatmapObject in list)
                beatmapObject.integerVariable = UnityEngine.Random.Range(min, max < 0 ? max - 1 : max + 1);
        }
        
        public static void animateVariableOther<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable)
                return;

            var fromType = modifier.GetInt(1, 0, variables);
            var fromAxis = modifier.GetInt(2, 0, variables);
            var delay = modifier.GetFloat(3, 0, variables);
            var multiply = modifier.GetFloat(4, 0, variables);
            var offset = modifier.GetFloat(5, 0, variables);
            var min = modifier.GetFloat(6, -9999f, variables);
            var max = modifier.GetFloat(7, 9999f, variables);
            var loop = modifier.GetFloat(8, 9999f, variables);

            var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(0, variables));
            if (list.IsEmpty())
                return;

            for (int i = 0; i < list.Count; i++)
            {
                var beatmapObject = list[i];
                var cachedSequences = beatmapObject.cachedSequences;
                var time = AudioManager.inst.CurrentAudioSource.time;

                fromType = Mathf.Clamp(fromType, 0, beatmapObject.events.Count);
                fromAxis = Mathf.Clamp(fromAxis, 0, beatmapObject.events[fromType][0].values.Length);

                if (!cachedSequences)
                    continue;

                switch (fromType)
                {
                    // To Type Position
                    // To Axis X
                    // From Type Position
                    case 0: {
                            var sequence = cachedSequences.PositionSequence.Interpolate(time - beatmapObject.StartTime - delay);

                            beatmapObject.integerVariable = (int)Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : fromAxis == 1 ? sequence.y % loop : sequence.z % loop) * multiply - offset, min, max);
                            break;
                        }
                    // To Type Position
                    // To Axis X
                    // From Type Scale
                    case 1: {
                            var sequence = cachedSequences.ScaleSequence.Interpolate(time - beatmapObject.StartTime - delay);

                            beatmapObject.integerVariable = (int)Mathf.Clamp((fromAxis == 0 ? sequence.x % loop : sequence.y % loop) * multiply - offset, min, max);
                            break;
                        }
                    // To Type Position
                    // To Axis X
                    // From Type Rotation
                    case 2: {
                            var sequence = cachedSequences.RotationSequence.Interpolate(time - beatmapObject.StartTime - delay) * multiply;

                            beatmapObject.integerVariable = (int)Mathf.Clamp((sequence % loop) - offset, min, max);
                            break;
                        }
                }
            }
        }
        
        public static void clampVariable<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is IModifyable<T> modifyable)
                modifyable.IntVariable = Mathf.Clamp(modifyable.IntVariable, modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables));
        }
        
        public static void clampVariableOther<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(0, variables));

            var min = modifier.GetInt(1, 0, variables);
            var max = modifier.GetInt(2, 0, variables);

            if (!list.IsEmpty())
                foreach (var bm in list)
                    bm.integerVariable = Mathf.Clamp(bm.integerVariable, min, max);
        }

        #endregion

        #region Enable / Disable

        public static void enableObject(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var value = modifier.GetValue(0, variables);
            if (value == "0")
                value = "True";

            modifier.reference.runtimeObject?.SetBaseActive(Parser.TryParse(value, true));
        }
        
        public static void enableObjectTree(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var value = modifier.GetValue(0, variables);
            if (value == "0")
                value = "False";

            if (!modifier.HasResult())
            {
                var beatmapObject = Parser.TryParse(value, true) ? modifier.reference : modifier.reference.GetParentChain().Last();

                modifier.Result = beatmapObject.GetChildTree();
            }

            var enabled = modifier.GetBool(2, true, variables);

            var list = modifier.GetResult<List<BeatmapObject>>();

            for (int i = 0; i < list.Count; i++)
                list[i].runtimeObject?.SetBaseActive(enabled);
        }
        
        public static void enableObjectOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(2, true, variables);

            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(0, variables));

            if (!list.IsEmpty())
                foreach (var beatmapObject in list)
                    beatmapObject.runtimeObject?.SetBaseActive(enabled);
        }
        
        public static void enableObjectTreeOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.HasResult())
            {
                var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));
                var useSelf = modifier.GetBool(0, true, variables);

                var resultList = new List<BeatmapObject>();
                foreach (var bm in beatmapObjects)
                {
                    var beatmapObject = useSelf ? bm : bm.GetParentChain().Last();
                    resultList.AddRange(beatmapObject.GetChildTree());
                }

                modifier.Result = resultList;
            }

            var enabled = modifier.GetBool(3, true, variables);

            var list = modifier.GetResult<List<BeatmapObject>>();

            for (int i = 0; i < list.Count; i++)
                list[i].runtimeObject?.SetBaseActive(enabled);
        }

        // if this ever needs to be updated, add a "version" int number to modifiers that increment each time a major change was done to the modifier.
        public static void enableObjectGroup(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(0, true, variables);
            var state = modifier.GetInt(1, 0, variables);

            for (int i = 2; i < modifier.commands.Count; i++)
            {
                var innerEnabled = state == 0 || state == i - 1; // if state is 0, then all should be active / inactive. otherwise if state equals the modifier group, set only that object group active / inactive.
                if (!enabled)
                    innerEnabled = !innerEnabled;

                var tag = modifier.commands[i];
                if (string.IsNullOrEmpty(tag))
                    continue;

                var list = GameData.Current.FindObjectsWithTag(modifier, tag);
                if (list.IsEmpty())
                    continue;

                foreach (var beatmapObject in list)
                    beatmapObject.runtimeObject?.SetBaseActive(innerEnabled);
            }
        }

        public static void disableObject(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            modifier.reference.runtimeObject?.SetBaseActive(false);
        }

        public static void disableObjectTree(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.GetValue(0) == "0")
                modifier.SetValue(0, "False");

            if (!modifier.HasResult())
            {
                var beatmapObject = modifier.GetBool(0, true, variables) ? modifier.reference : modifier.reference.GetParentChain().Last();

                modifier.Result = beatmapObject.GetChildTree();
            }

            var list = modifier.GetResult<List<BeatmapObject>>();

            for (int i = 0; i < list.Count; i++)
                list[i].runtimeObject?.SetBaseActive(false);
        }

        public static void disableObjectOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(0, variables));

            if (!list.IsEmpty())
                foreach (var beatmapObject in list)
                    beatmapObject.runtimeObject?.SetBaseActive(false);
        }

        public static void disableObjectTreeOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.HasResult())
            {
                var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));
                var useSelf = modifier.GetBool(0, true, variables);

                var resultList = new List<BeatmapObject>();
                foreach (var bm in beatmapObjects)
                {
                    var beatmapObject = useSelf ? bm : bm.GetParentChain().Last();
                    resultList.AddRange(beatmapObject.GetChildTree());
                }

                modifier.Result = resultList;
            }

            var list = modifier.GetResult<List<BeatmapObject>>();

            for (int i = 0; i < list.Count; i++)
                list[i].runtimeObject?.SetBaseActive(false);
        }

        public static void setActive(Modifier<BackgroundObject> modifier, Dictionary<string, string> variables)
        {
            modifier.reference.Enabled = modifier.GetBool(0, false, variables);
        }
        
        public static void setActiveOther(Modifier<BackgroundObject> modifier, Dictionary<string, string> variables)
        {
            var active = modifier.GetBool(0, false, variables);
            var tag = modifier.GetValue(1, variables);
            var list = GameData.Current.backgroundObjects.FindAll(x => x.tags.Contains(tag));
            if (!list.IsEmpty())
                for (int i = 0; i < list.Count; i++)
                    list[i].Enabled = active;
        }

        #endregion

        #region JSON

        public static void saveFloat<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            ModifiersHelper.SaveProgress(modifier.GetValue(1, variables), modifier.GetValue(2, variables), modifier.GetValue(3, variables), modifier.GetFloat(0, 0f, variables));
        }
        
        public static void saveString<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            ModifiersHelper.SaveProgress(modifier.GetValue(1, variables), modifier.GetValue(2, variables), modifier.GetValue(3, variables), modifier.GetValue(0, variables));
        }
        
        public static void saveText(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference.runtimeObject && modifier.reference.runtimeObject.visualObject is TextObject textObject)
                ModifiersHelper.SaveProgress(modifier.GetValue(1, variables), modifier.GetValue(2, variables), modifier.GetValue(3, variables), textObject.textMeshPro.text);
        }
        
        public static void saveVariable(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference)
                ModifiersHelper.SaveProgress(modifier.GetValue(1, variables), modifier.GetValue(2, variables), modifier.GetValue(3, variables), modifier.reference.integerVariable);
        }
        
        public static void loadVariable(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile", modifier.GetValue(1, variables) + FileFormat.SES.Dot());
            if (!RTFile.FileExists(path))
                return;

            string json = RTFile.ReadFromFile(path);

            if (string.IsNullOrEmpty(json))
                return;

            var jn = JSON.Parse(json);

            var fjn = jn[modifier.GetValue(2, variables)][modifier.GetValue(3, variables)]["float"];
            if (!string.IsNullOrEmpty(fjn) && float.TryParse(fjn, out float eq))
                modifier.reference.integerVariable = (int)eq;
        }
        
        public static void loadVariableOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile", modifier.GetValue(1, variables) + FileFormat.SES.Dot());
            if (!RTFile.FileExists(path))
                return;

            string json = RTFile.ReadFromFile(path);

            if (string.IsNullOrEmpty(json))
                return;

            var jn = JSON.Parse(json);
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(0, variables));
            var fjn = jn[modifier.GetValue(2, variables)][modifier.GetValue(3, variables)]["float"];

            if (list.Count > 0 && !string.IsNullOrEmpty(fjn) && float.TryParse(fjn, out float eq))
                foreach (var bm in list)
                    bm.integerVariable = (int)eq;
        }

        #endregion

        #region Reactive

        public static void reactivePos(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var val = modifier.GetFloat(0, 0f, variables);
            var sampleX = modifier.GetInt(1, 0, variables);
            var sampleY = modifier.GetInt(2, 0, variables);
            var intensityX = modifier.GetFloat(3, 0f, variables);
            var intensityY = modifier.GetFloat(4, 0f, variables);

            modifier.reference?.runtimeObject?.visualObject?.SetOrigin(new Vector3(
                modifier.reference.origin.x + RTLevel.Current.GetSample(sampleX, intensityX * val),
                modifier.reference.origin.y + RTLevel.Current.GetSample(sampleY, intensityY * val),
                modifier.reference.Depth * 0.1f));
        }
        
        public static void reactiveSca(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var val = modifier.GetFloat(0, 0f, variables);
            var sampleX = modifier.GetInt(1, 0, variables);
            var sampleY = modifier.GetInt(2, 0, variables);
            var intensityX = modifier.GetFloat(3, 0f, variables);
            var intensityY = modifier.GetFloat(4, 0f, variables);

            modifier.reference?.runtimeObject?.visualObject?.SetScaleOffset(new Vector2(
                1f + RTLevel.Current.GetSample(sampleX, intensityX * val),
                1f + RTLevel.Current.GetSample(sampleY, intensityY * val)));
        }
        
        public static void reactiveRot(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            modifier.reference?.runtimeObject?.visualObject?.SetRotationOffset(RTLevel.Current.GetSample(modifier.GetInt(1, 0, variables), modifier.GetFloat(0, 0f, variables)));
        }
        
        public static void reactiveCol(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var runtimeObject = modifier.reference.runtimeObject;
            if (runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.renderer)
                runtimeObject.visualObject.SetColor(runtimeObject.visualObject.GetPrimaryColor() + ThemeManager.inst.Current.GetObjColor(modifier.GetInt(2, 0, variables)) * RTLevel.Current.GetSample(modifier.GetInt(1, 0, variables), modifier.GetFloat(0, 0f, variables)));
        }
        
        public static void reactiveColLerp(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var runtimeObject = modifier.reference.runtimeObject;
            if (runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.renderer)
                runtimeObject.visualObject.SetColor(RTMath.Lerp(runtimeObject.visualObject.GetPrimaryColor(), ThemeManager.inst.Current.GetObjColor(modifier.GetInt(2, 0, variables)), RTLevel.Current.GetSample(modifier.GetInt(1, 0, variables), modifier.GetFloat(0, 0f, variables))));
        }
        
        public static void reactivePosChain(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            var val = modifier.GetFloat(0, 0f, variables);
            var sampleX = modifier.GetInt(1, 0, variables);
            var sampleY = modifier.GetInt(2, 0, variables);
            var intensityX = modifier.GetFloat(3, 0f, variables);
            var intensityY = modifier.GetFloat(4, 0f, variables);

            float reactivePositionX = RTLevel.Current.GetSample(sampleX, intensityX * val);
            float reactivePositionY = RTLevel.Current.GetSample(sampleY, intensityY * val);

            modifier.reference.reactivePositionOffset = new Vector3(reactivePositionX, reactivePositionY);
        }
        
        public static void reactiveScaChain(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference)
                return;

            var val = modifier.GetFloat(0, 0f, variables);
            var sampleX = modifier.GetInt(1, 0, variables);
            var sampleY = modifier.GetInt(2, 0, variables);
            var intensityX = modifier.GetFloat(3, 0f, variables);
            var intensityY = modifier.GetFloat(4, 0f, variables);

            float reactiveScaleX = RTLevel.Current.GetSample(sampleX, intensityX * val);
            float reactiveScaleY = RTLevel.Current.GetSample(sampleY, intensityY * val);

            modifier.reference.reactiveScaleOffset = new Vector3(reactiveScaleX, reactiveScaleY, 1f);
        }
        
        public static void reactiveRotChain(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference)
                modifier.reference.reactiveRotationOffset = RTLevel.Current.GetSample(modifier.GetInt(1, 0, variables), modifier.GetFloat(0, 0f, variables));
        }

        #endregion

        #region Events

        public static void eventOffset<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (RTLevel.Current.eventEngine && RTLevel.Current.eventEngine.offsets != null)
                RTLevel.Current.eventEngine.SetOffset(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables), modifier.GetFloat(0, 1f, variables));
        }
        
        public static void eventOffsetVariable<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (RTLevel.Current.eventEngine && RTLevel.Current.eventEngine.offsets != null && modifier.reference is IModifyable<T> modifyable)
                RTLevel.Current.eventEngine.SetOffset(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables), modifyable.IntVariable * modifier.GetFloat(0, 1f, variables));
        }
        
        public static void eventOffsetMath<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (RTLevel.Current.eventEngine && RTLevel.Current.eventEngine.offsets != null && modifier.reference is IEvaluatable evaluatable)
            {
                var numberVariables = evaluatable.GetObjectVariables();
                ModifiersHelper.SetVariables(variables, numberVariables);
                RTLevel.Current.eventEngine.SetOffset(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables), RTMath.Parse(modifier.GetValue(0, variables), numberVariables, evaluatable.GetObjectFunctions()));
            }
        }
        
        public static void eventOffsetAnimate<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.constant || !RTLevel.Current.eventEngine || RTLevel.Current.eventEngine.offsets == null)
                return;

            string easing = modifier.GetValue(4, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var list = RTLevel.Current.eventEngine.offsets;

            var eventType = modifier.GetInt(1, 0, variables);
            var indexValue = modifier.GetInt(2, 0, variables);

            if (eventType < list.Count && indexValue < list[eventType].Count)
            {
                var value = modifier.GetBool(5, false, variables) ? list[eventType][indexValue] + modifier.GetFloat(0, 0f, variables) : modifier.GetFloat(0, 0f, variables);

                var animation = new RTAnimation("Event Offset Animation");
                animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, list[eventType][indexValue], Ease.Linear),
                            new FloatKeyframe(modifier.GetFloat(3, 1f, variables), value, Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, x => RTLevel.Current.eventEngine.SetOffset(eventType, indexValue, x), interpolateOnComplete: true)
                    };
                animation.SetDefaultOnComplete(false);
                AnimationManager.inst.Play(animation);
            }
        }
        
        public static void eventOffsetCopyAxis(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!RTLevel.Current.eventEngine || RTLevel.Current.eventEngine.offsets == null)
                return;

            var fromType = modifier.GetInt(1, 0, variables);
            var fromAxis = modifier.GetInt(2, 0, variables);
            var toType = modifier.GetInt(3, 0, variables);
            var toAxis = modifier.GetInt(4, 0, variables);
            var delay = modifier.GetFloat(5, 0f, variables);
            var multiply = modifier.GetFloat(6, 0f, variables);
            var offset = modifier.GetFloat(7, 0f, variables);
            var min = modifier.GetFloat(8, 0f, variables);
            var max = modifier.GetFloat(9, 0f, variables);
            var loop = modifier.GetFloat(10, 0f, variables);
            var useVisual = modifier.GetBool(11, false, variables);

            var time = AudioManager.inst.CurrentAudioSource.time;

            fromType = Mathf.Clamp(fromType, 0, modifier.reference.events.Count - 1);
            fromAxis = Mathf.Clamp(fromAxis, 0, modifier.reference.events[fromType][0].values.Length - 1);
            toType = Mathf.Clamp(toType, 0, RTLevel.Current.eventEngine.offsets.Count - 1);
            toAxis = Mathf.Clamp(toAxis, 0, RTLevel.Current.eventEngine.offsets[toType].Count - 1);

            if (!useVisual && modifier.reference.cachedSequences)
                RTLevel.Current.eventEngine.SetOffset(toType, toAxis, fromType switch
                {
                    0 => Mathf.Clamp((modifier.reference.cachedSequences.PositionSequence.Interpolate(time - modifier.reference.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                    1 => Mathf.Clamp((modifier.reference.cachedSequences.ScaleSequence.Interpolate(time - modifier.reference.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                    2 => Mathf.Clamp((modifier.reference.cachedSequences.RotationSequence.Interpolate(time - modifier.reference.StartTime - delay) - offset) * multiply % loop, min, max),
                    _ => 0f,
                });
            else if (modifier.reference.runtimeObject is RTBeatmapObject levelObject && levelObject.visualObject && levelObject.visualObject.gameObject)
                RTLevel.Current.eventEngine.SetOffset(toType, toAxis, Mathf.Clamp((levelObject.visualObject.gameObject.transform.GetVector(fromType).At(fromAxis) - offset) * multiply % loop, min, max));
        }
        
        public static void vignetteTracksPlayer<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (!RTLevel.Current.eventEngine)
                return;

            var players = PlayerManager.Players;
            if (players.IsEmpty())
                return;

            var player = players[0].Player;

            if (!player || !player.rb)
                return;

            var cameraToViewportPoint = Camera.main.WorldToViewportPoint(player.rb.position);
            RTLevel.Current.eventEngine.SetOffset(7, 4, cameraToViewportPoint.x);
            RTLevel.Current.eventEngine.SetOffset(7, 5, cameraToViewportPoint.y);
        }
        
        public static void lensTracksPlayer<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (!RTLevel.Current.eventEngine)
                return;

            var players = PlayerManager.Players;
            if (players.IsEmpty())
                return;

            var player = players[0].Player;

            if (!player || !player.rb)
                return;

            var cameraToViewportPoint = Camera.main.WorldToViewportPoint(player.rb.position);
            RTLevel.Current.eventEngine.SetOffset(8, 1, cameraToViewportPoint.x - 0.5f);
            RTLevel.Current.eventEngine.SetOffset(8, 2, cameraToViewportPoint.y - 0.5f);
        }

        #endregion

        #region Color

        public static void addColor(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference || !modifier.reference.runtimeObject || !modifier.reference.runtimeObject.visualObject)
                return;

            var multiply = modifier.GetFloat(0, 1f, variables);
            var index = modifier.GetInt(1, 0, variables);
            var hue = modifier.GetFloat(2, 0f, variables);
            var sat = modifier.GetFloat(3, 0f, variables);
            var val = modifier.GetFloat(4, 0f, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                modifier.reference.runtimeObject.visualObject.SetColor(modifier.reference.runtimeObject.visualObject.GetPrimaryColor() + RTColors.ChangeColorHSV(ThemeManager.inst.Current.GetObjColor(index), hue, sat, val) * multiply);
            });
        }
        
        public static void addColorOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1));

            if (list.IsEmpty())
                return;

            var multiply = modifier.GetFloat(0, 0f, variables);
            var index = modifier.GetInt(2, 0, variables);
            var hue = modifier.GetFloat(3, 0f, variables);
            var sat = modifier.GetFloat(4, 0f, variables);
            var val = modifier.GetFloat(5, 0f, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                foreach (var bm in list)
                {
                    if (bm.runtimeObject)
                        bm.runtimeObject.visualObject.SetColor(bm.runtimeObject.visualObject.GetPrimaryColor() + RTColors.ChangeColorHSV(ThemeManager.inst.Current.GetObjColor(index), hue, sat, val) * multiply);
                }
            });
        }
        
        public static void lerpColor(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference || !modifier.reference.runtimeObject || !modifier.reference.runtimeObject.visualObject)
                return;

            var multiply = modifier.GetFloat(0, 0f, variables);
            var index = modifier.GetInt(1, 0, variables);
            var hue = modifier.GetFloat(2, 0f, variables);
            var sat = modifier.GetFloat(3, 0f, variables);
            var val = modifier.GetFloat(4, 0f, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                modifier.reference.runtimeObject.visualObject.SetColor(RTMath.Lerp(modifier.reference.runtimeObject.visualObject.GetPrimaryColor(), RTColors.ChangeColorHSV(ThemeManager.inst.Current.GetObjColor(index), hue, sat, val), multiply));
            });
        }
        
        public static void lerpColorOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1));

            if (list.IsEmpty())
                return;

            var multiply = modifier.GetFloat(0, 0f, variables);
            var index = modifier.GetInt(2, 0, variables);
            var hue = modifier.GetFloat(3, 0f, variables);
            var sat = modifier.GetFloat(4, 0f, variables);
            var val = modifier.GetFloat(5, 0f, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var color = RTColors.ChangeColorHSV(ThemeManager.inst.Current.GetObjColor(index), hue, sat, val);
                for (int i = 0; i < list.Count; i++)
                {
                    var bm = list[i];
                    if (bm.runtimeObject && bm.runtimeObject.visualObject)
                        bm.runtimeObject.visualObject.SetColor(RTMath.Lerp(bm.runtimeObject.visualObject.GetPrimaryColor(), color, multiply));
                }
            });
        }
        
        public static void addColorPlayerDistance(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var runtimeObject = modifier.reference.runtimeObject;
            if (!modifier.reference || !runtimeObject || !runtimeObject.visualObject.gameObject)
                return;

            var offset = modifier.GetFloat(0, 0f, variables);
            var index = modifier.GetInt(1, 0, variables);
            var multiply = modifier.GetFloat(2, 0, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var player = PlayerManager.GetClosestPlayer(runtimeObject.visualObject.gameObject.transform.position);

                if (!player.Player || !player.Player.rb)
                    return;

                var distance = Vector2.Distance(player.Player.rb.transform.position, runtimeObject.visualObject.gameObject.transform.position);

                runtimeObject.visualObject.SetColor(runtimeObject.visualObject.GetPrimaryColor() + ThemeManager.inst.Current.GetObjColor(index) * -(distance * multiply - offset));
            });
        }
        
        public static void lerpColorPlayerDistance(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var runtimeObject = modifier.reference.runtimeObject;
            if (!modifier.reference || !runtimeObject || !runtimeObject.visualObject.gameObject)
                return;

            var offset = modifier.GetFloat(0, 0f, variables);
            var index = modifier.GetInt(1, 0, variables);
            var multiply = modifier.GetFloat(2, 0f, variables);
            var opacity = modifier.GetFloat(3, 0f, variables);
            var hue = modifier.GetFloat(4, 0f, variables);
            var sat = modifier.GetFloat(5, 0f, variables);
            var val = modifier.GetFloat(6, 0f, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var player = PlayerManager.GetClosestPlayer(runtimeObject.visualObject.gameObject.transform.position);

                if (!player.Player || !player.Player.rb)
                    return;

                var distance = Vector2.Distance(player.Player.rb.transform.position, runtimeObject.visualObject.gameObject.transform.position);

                runtimeObject.visualObject.SetColor(Color.Lerp(runtimeObject.visualObject.GetPrimaryColor(),
                                RTColors.FadeColor(RTColors.ChangeColorHSV(ThemeManager.inst.Current.GetObjColor(index), hue, sat, val), opacity),
                                -(distance * multiply - offset)));
            });
        }
        
        public static void setOpacity(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var runtimeObject = modifier.reference.runtimeObject;
            if (!modifier.reference || !runtimeObject || !runtimeObject.visualObject.gameObject)
                return;

            var opacity = modifier.GetFloat(0, 1f, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                runtimeObject.visualObject.SetColor(RTColors.FadeColor(runtimeObject.visualObject.GetPrimaryColor(), opacity));
            });
        }
        
        public static void setOpacityOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var opacity = modifier.GetFloat(0, 1f, variables);

            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));

            if (list.IsEmpty())
                return;

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                foreach (var bm in list)
                {
                    if (bm.runtimeObject && bm.runtimeObject.visualObject)
                        bm.runtimeObject.visualObject.SetColor(RTColors.FadeColor(bm.runtimeObject.visualObject.GetPrimaryColor(), opacity));
                }
            });
        }
        
        public static void copyColor(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var applyColor1 = modifier.GetBool(1, true, variables);
            var applyColor2 = modifier.GetBool(2, true, variables);

            if (GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(0, variables), out BeatmapObject beatmapObject) && modifier.reference.runtimeObject && beatmapObject.runtimeObject)
            {
                // queue post tick so the color overrides the sequence color
                RTLevel.Current.postTick.Enqueue(() =>
                {
                    ModifiersHelper.CopyColor(modifier.reference.runtimeObject, beatmapObject.runtimeObject, applyColor1, applyColor2);
                });
            }
        }
        
        public static void copyColorOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(0, variables));

            if (list.IsEmpty())
                return;

            var applyColor1 = modifier.GetBool(1, true, variables);
            var applyColor2 = modifier.GetBool(2, true, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var runtimeObject = modifier.reference.runtimeObject;

                foreach (var bm in list)
                {
                    var otherRuntimeObject = bm.runtimeObject;
                    if (!otherRuntimeObject)
                        continue;

                    ModifiersHelper.CopyColor(otherRuntimeObject, runtimeObject, applyColor1, applyColor2);
                }
            });
        }
        
        public static void applyColorGroup(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(0, variables));

            var beatmapObject = modifier.reference;
            var cachedSequences = beatmapObject.cachedSequences;
            if (list.IsEmpty() || !cachedSequences)
                return;

            var type = modifier.GetInt(1, 0, variables);
            var axis = modifier.GetInt(2, 0, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var time = RTLevel.Current.CurrentTime - beatmapObject.StartTime;
                Color color;
                Color secondColor;
                {
                    var prevKFIndex = beatmapObject.events[3].FindLastIndex(x => x.time < time);

                    if (prevKFIndex < 0)
                        return;

                    var prevKF = beatmapObject.events[3][prevKFIndex];
                    var nextKF = beatmapObject.events[3][Mathf.Clamp(prevKFIndex + 1, 0, beatmapObject.events[3].Count - 1)];
                    var easing = Ease.GetEaseFunction(nextKF.curve.ToString())(RTMath.InverseLerp(prevKF.time, nextKF.time, time));
                    int prevcolor = (int)prevKF.values[0];
                    int nextColor = (int)nextKF.values[0];
                    var lerp = RTMath.Lerp(0f, 1f, easing);
                    if (float.IsNaN(lerp) || float.IsInfinity(lerp))
                        lerp = 1f;

                    color = Color.Lerp(
                        CoreHelper.CurrentBeatmapTheme.GetObjColor(prevcolor),
                        CoreHelper.CurrentBeatmapTheme.GetObjColor(nextColor),
                        lerp);

                    lerp = RTMath.Lerp(prevKF.values[1], nextKF.values[1], easing);
                    if (float.IsNaN(lerp) || float.IsInfinity(lerp))
                        lerp = 0f;

                    color = RTColors.FadeColor(color, -(lerp - 1f));

                    var lerpHue = RTMath.Lerp(prevKF.values[2], nextKF.values[2], easing);
                    var lerpSat = RTMath.Lerp(prevKF.values[3], nextKF.values[3], easing);
                    var lerpVal = RTMath.Lerp(prevKF.values[4], nextKF.values[4], easing);

                    if (float.IsNaN(lerpHue))
                        lerpHue = nextKF.values[2];
                    if (float.IsNaN(lerpSat))
                        lerpSat = nextKF.values[3];
                    if (float.IsNaN(lerpVal))
                        lerpVal = nextKF.values[4];

                    color = RTColors.ChangeColorHSV(color, lerpHue, lerpSat, lerpVal);

                    prevcolor = (int)prevKF.values[5];
                    nextColor = (int)nextKF.values[5];
                    lerp = RTMath.Lerp(0f, 1f, easing);
                    if (float.IsNaN(lerp) || float.IsInfinity(lerp))
                        lerp = 1f;

                    secondColor = Color.Lerp(
                        CoreHelper.CurrentBeatmapTheme.GetObjColor(prevcolor),
                        CoreHelper.CurrentBeatmapTheme.GetObjColor(nextColor),
                        lerp);

                    lerp = RTMath.Lerp(prevKF.values[6], nextKF.values[6], easing);
                    if (float.IsNaN(lerp) || float.IsInfinity(lerp))
                        lerp = 0f;

                    secondColor = RTColors.FadeColor(secondColor, -(lerp - 1f));

                    lerpHue = RTMath.Lerp(prevKF.values[7], nextKF.values[7], easing);
                    lerpSat = RTMath.Lerp(prevKF.values[8], nextKF.values[8], easing);
                    lerpVal = RTMath.Lerp(prevKF.values[9], nextKF.values[9], easing);

                    if (float.IsNaN(lerpHue))
                        lerpHue = nextKF.values[7];
                    if (float.IsNaN(lerpSat))
                        lerpSat = nextKF.values[8];
                    if (float.IsNaN(lerpVal))
                        lerpVal = nextKF.values[9];

                    secondColor = RTColors.ChangeColorHSV(color, lerpHue, lerpSat, lerpVal);
                } // assign

                var isEmpty = modifier.reference.objectType == BeatmapObject.ObjectType.Empty;

                float t = !isEmpty ? type switch
                {
                    0 => axis == 0 ? cachedSequences.PositionSequence.Value.x : axis == 1 ? cachedSequences.PositionSequence.Value.y : cachedSequences.PositionSequence.Value.z,
                    1 => axis == 0 ? cachedSequences.ScaleSequence.Value.x : cachedSequences.ScaleSequence.Value.y,
                    2 => cachedSequences.RotationSequence.Value,
                    _ => 0f
                } : type switch
                {
                    0 => axis == 0 ? cachedSequences.PositionSequence.Interpolate(time).x : axis == 1 ? cachedSequences.PositionSequence.Interpolate(time).y : cachedSequences.PositionSequence.Interpolate(time).z,
                    1 => axis == 0 ? cachedSequences.ScaleSequence.Interpolate(time).x : cachedSequences.ScaleSequence.Interpolate(time).y,
                    2 => cachedSequences.RotationSequence.Interpolate(time),
                    _ => 0f
                };

                foreach (var bm in list)
                {
                    var otherLevelObject = bm.runtimeObject;
                    if (!otherLevelObject)
                        continue;

                    if (!otherLevelObject.visualObject.isGradient)
                        otherLevelObject.visualObject.SetColor(Color.Lerp(otherLevelObject.visualObject.GetPrimaryColor(), color, t));
                    else if (otherLevelObject.visualObject is SolidObject solidObject)
                    {
                        var colors = solidObject.GetColors();
                        solidObject.SetColor(Color.Lerp(colors.startColor, color, t), Color.Lerp(colors.endColor, secondColor, t));
                    }
                }
            });
        }
        
        public static void setColorHex(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var runtimeObject = modifier.reference.runtimeObject;
            if (!runtimeObject)
                return;

            var color1 = modifier.GetValue(0, variables);
            var color2 = modifier.GetValue(1, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                if (!runtimeObject.visualObject.isGradient)
                {
                    var color = runtimeObject.visualObject.GetPrimaryColor();
                    runtimeObject.visualObject.SetColor(string.IsNullOrEmpty(color1) ? color : color1.Length == 8 ? RTColors.HexToColor(color1) : RTColors.FadeColor(RTColors.HexToColor(color1), color.a));
                }
                else if (runtimeObject.visualObject is SolidObject solidObject)
                {
                    var colors = solidObject.GetColors();
                    solidObject.SetColor(
                        string.IsNullOrEmpty(color1) ? colors.startColor : color1.Length == 8 ? RTColors.HexToColor(color1) : RTColors.FadeColor(RTColors.HexToColor(color1), colors.startColor.a),
                        string.IsNullOrEmpty(color2) ? colors.endColor : color2.Length == 8 ? RTColors.HexToColor(color2) : RTColors.FadeColor(RTColors.HexToColor(color2), colors.endColor.a));
                }
            });
        }
        
        public static void setColorHexOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));

            if (list.IsEmpty())
                return;

            var color1 = modifier.GetValue(0, variables);
            var color2 = modifier.GetValue(2, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                foreach (var bm in list)
                {
                    var runtimeObject = bm.runtimeObject;
                    if (!runtimeObject)
                        continue;

                    if (!runtimeObject.visualObject.isGradient)
                    {
                        var color = runtimeObject.visualObject.GetPrimaryColor();
                        runtimeObject.visualObject.SetColor(string.IsNullOrEmpty(color1) ? color : color1.Length == 8 ? RTColors.HexToColor(color1) : RTColors.FadeColor(LSColors.HexToColorAlpha(color1), color.a));
                    }
                    else if (runtimeObject.visualObject is SolidObject solidObject)
                    {
                        var colors = solidObject.GetColors();
                        solidObject.SetColor(
                            string.IsNullOrEmpty(color1) ? colors.startColor : color1.Length == 8 ? RTColors.HexToColor(color1) : RTColors.FadeColor(LSColors.HexToColorAlpha(color1), colors.startColor.a),
                            string.IsNullOrEmpty(color2) ? colors.endColor : color2.Length == 8 ? RTColors.HexToColor(color2) : RTColors.FadeColor(LSColors.HexToColorAlpha(color2), colors.endColor.a));
                    }
                }
            });
        }

        public static void setColorRGBA(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var runtimeObject = modifier.reference.runtimeObject;
            if (!runtimeObject)
                return;

            var color1 = new Color(modifier.GetFloat(0, 1f, variables), modifier.GetFloat(1, 1f, variables), modifier.GetFloat(2, 1f, variables), modifier.GetFloat(3, 1f, variables));
            var color2 = new Color(modifier.GetFloat(4, 1f, variables), modifier.GetFloat(5, 1f, variables), modifier.GetFloat(6, 1f, variables), modifier.GetFloat(7, 1f, variables));

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                if (!runtimeObject.visualObject.isGradient)
                    runtimeObject.visualObject.SetColor(color1);
                else if (runtimeObject.visualObject is SolidObject solidObject)
                    solidObject.SetColor(color1, color2);
            });
        }

        public static void setColorRGBAOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(8, variables));

            if (list.IsEmpty())
                return;

            var color1 = new Color(modifier.GetFloat(0, 1f, variables), modifier.GetFloat(1, 1f, variables), modifier.GetFloat(2, 1f, variables), modifier.GetFloat(3, 1f, variables));
            var color2 = new Color(modifier.GetFloat(4, 1f, variables), modifier.GetFloat(5, 1f, variables), modifier.GetFloat(6, 1f, variables), modifier.GetFloat(7, 1f, variables));

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                foreach (var bm in list)
                {
                    var runtimeObject = bm.runtimeObject;
                    if (!runtimeObject)
                        continue;

                    if (!runtimeObject.visualObject.isGradient)
                        runtimeObject.visualObject.SetColor(color1);
                    else if (runtimeObject.visualObject is SolidObject solidObject)
                        solidObject.SetColor(color1, color2);
                }
            });
        }

        public static void animateColorKF<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not ILifetime<AutoKillType> lifetime)
                return;

            Sequence<Color> sequence1;
            Sequence<Color> sequence2;

            var audioTime = modifier.GetFloat(0, 0f, variables);
            var colorSource = modifier.GetInt(1, 0, variables);

            if (modifier.TryGetResult(out KeyValuePair<Sequence<Color>, Sequence<Color>> sequences))
            {
                sequence1 = sequences.Key;
                sequence2 = sequences.Value;
            }
            else
            {
                // custom start colors
                var colorSlot1Start = modifier.GetInt(2, 0, variables);
                var opacity1Start = modifier.GetFloat(3, 1f, variables);
                var hue1Start = modifier.GetFloat(4, 0f, variables);
                var saturation1Start = modifier.GetFloat(5, 0f, variables);
                var value1Start = modifier.GetFloat(6, 0f, variables);
                var colorSlot2Start = modifier.GetInt(7, 0, variables);
                var opacity2Start = modifier.GetFloat(8, 1f, variables);
                var hue2Start = modifier.GetFloat(9, 0f, variables);
                var saturation2Start = modifier.GetFloat(10, 0f, variables);
                var value2Start = modifier.GetFloat(11, 0f, variables);

                var currentTime = 0f;

                var keyframes1 = new List<IKeyframe<Color>>();
                keyframes1.Add(new CustomThemeKeyframe(currentTime, colorSource, colorSlot1Start, opacity1Start, hue1Start, saturation1Start, value1Start, Ease.Linear, false));
                var keyframes2 = new List<IKeyframe<Color>>();
                keyframes2.Add(new CustomThemeKeyframe(currentTime, colorSource, colorSlot2Start, opacity2Start, hue2Start, saturation2Start, value2Start, Ease.Linear, false));
                for (int i = 12; i < modifier.commands.Count; i += 14)
                {
                    var time = modifier.GetFloat(i + 1, 0f, variables);
                    if (time < currentTime)
                        continue;

                    var colorSlot1 = modifier.GetInt(i + 2, 0, variables);
                    var opacity1 = modifier.GetFloat(i + 3, 1f, variables);
                    var hue1 = modifier.GetFloat(i + 4, 0f, variables);
                    var saturation1 = modifier.GetFloat(i + 5, 0f, variables);
                    var value1 = modifier.GetFloat(i + 6, 0f, variables);
                    var colorSlot2 = modifier.GetInt(i + 7, 0, variables);
                    var opacity2 = modifier.GetFloat(i + 8, 1f, variables);
                    var hue2 = modifier.GetFloat(i + 9, 0f, variables);
                    var saturation2 = modifier.GetFloat(i + 10, 0f, variables);
                    var value2 = modifier.GetFloat(i + 11, 0f, variables);
                    var relative = modifier.GetBool(i + 12, true, variables);

                    var easing = modifier.GetValue(i + 13, variables);
                    if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                        easing = DataManager.inst.AnimationList[e].Name;

                    var ease = Ease.GetEaseFunction(easing, Ease.Linear);
                    keyframes1.Add(new CustomThemeKeyframe(currentTime + time, colorSource, colorSlot1, opacity1, hue1, saturation1, value1, ease, false));
                    keyframes2.Add(new CustomThemeKeyframe(currentTime + time, colorSource, colorSlot2, opacity2, hue2, saturation2, value2, ease, false));

                    currentTime = time;
                }

                sequence1 = new Sequence<Color>(keyframes1);
                sequence2 = new Sequence<Color>(keyframes2);

                modifier.Result = new KeyValuePair<Sequence<Color>, Sequence<Color>>(sequence1, sequence2);
            }

            var beatmapObject = modifier.reference as BeatmapObject;
            var backgroundObject = modifier.reference as BackgroundObject;

            var startTime = lifetime.StartTime;

            RTLevel.Current.postTick.Enqueue(() =>
            {
                var primaryColor = Color.white;
                var secondaryColor = Color.white;

                primaryColor = sequence1.Interpolate(audioTime - startTime);
                secondaryColor = sequence2.Interpolate(audioTime - startTime);

                if (beatmapObject && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject is SolidObject solidObject)
                {
                    if (solidObject.isGradient)
                        solidObject.SetColor(primaryColor, secondaryColor);
                    else
                        solidObject.SetColor(primaryColor);
                }

                if (backgroundObject && backgroundObject.runtimeObject)
                    backgroundObject.runtimeObject.SetColor(primaryColor, secondaryColor);
            });
        }

        public static void animateColorKFHex<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not ILifetime<AutoKillType> lifetime)
                return;

            Sequence<Color> sequence1;
            Sequence<Color> sequence2;

            var audioTime = modifier.GetFloat(0, 0f, variables);

            if (modifier.TryGetResult(out KeyValuePair<Sequence<Color>, Sequence<Color>> sequences))
            {
                sequence1 = sequences.Key;
                sequence2 = sequences.Value;
            }
            else
            {
                // custom start colors
                var color1Start = modifier.GetValue(1, variables);
                var color2Start = modifier.GetValue(2, variables);

                var currentTime = 0f;

                var keyframes1 = new List<IKeyframe<Color>>();
                keyframes1.Add(new ColorKeyframe(currentTime, RTColors.HexToColor(color1Start), Ease.Linear));
                var keyframes2 = new List<IKeyframe<Color>>();
                keyframes2.Add(new ColorKeyframe(currentTime, RTColors.HexToColor(color2Start), Ease.Linear));
                for (int i = 3; i < modifier.commands.Count; i += 6)
                {
                    var time = modifier.GetFloat(i + 1, 0f, variables);
                    if (time < currentTime)
                        continue;

                    var color1 = modifier.GetValue(i + 2, variables);
                    var color2 = modifier.GetValue(i + 3, variables);
                    var relative = modifier.GetBool(i + 4, true, variables);

                    var easing = modifier.GetValue(i + 5, variables);
                    if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                        easing = DataManager.inst.AnimationList[e].Name;

                    var ease = Ease.GetEaseFunction(easing, Ease.Linear);
                    keyframes1.Add(new ColorKeyframe(currentTime + time, RTColors.HexToColor(color1), ease));
                    keyframes2.Add(new ColorKeyframe(currentTime + time, RTColors.HexToColor(color2), ease));

                    currentTime = time;
                }

                sequence1 = new Sequence<Color>(keyframes1);
                sequence2 = new Sequence<Color>(keyframes2);

                modifier.Result = new KeyValuePair<Sequence<Color>, Sequence<Color>>(sequence1, sequence2);
            }

            var beatmapObject = modifier.reference as BeatmapObject;
            var backgroundObject = modifier.reference as BackgroundObject;

            var startTime = lifetime.StartTime;

            RTLevel.Current.postTick.Enqueue(() =>
            {
                var primaryColor = Color.white;
                var secondaryColor = Color.white;

                primaryColor = sequence1.Interpolate(audioTime - startTime);
                primaryColor = sequence2.Interpolate(audioTime - startTime);

                if (beatmapObject && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject is SolidObject solidObject)
                {
                    if (solidObject.isGradient)
                        solidObject.SetColor(primaryColor, secondaryColor);
                    else
                        solidObject.SetColor(primaryColor);
                }

                if (backgroundObject && backgroundObject.runtimeObject)
                    backgroundObject.runtimeObject.SetColor(primaryColor, secondaryColor);
            });
        }

        #endregion

        #region Shape

        public static void setShape<T>(Modifier<T> modifier, Dictionary<string, string> variables) where T : IShapeable
        {
            var shapeable = modifier.reference as IShapeable;
            shapeable.SetCustomShape(modifier.GetInt(0, 0, variables), modifier.GetInt(1, 0, variables));
            if (shapeable is BeatmapObject beatmapObject)
                RTLevel.Current.UpdateObject(beatmapObject, RTLevel.ObjectContext.SHAPE);
            else if (shapeable is BackgroundObject backgroundObject)
                backgroundObject.runtimeObject?.UpdateShape(backgroundObject.Shape, backgroundObject.ShapeOption);
        }

        public static void setPolygonShape(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference.runtimeObject || modifier.reference.runtimeObject.visualObject is not PolygonObject polygonObject)
                return;

            var radius = RTMath.Clamp(modifier.GetFloat(0, 0.5f, variables), 0.1f, 10f);
            var sides = RTMath.Clamp(modifier.GetInt(1, 3, variables), 3, 32);
            var roundness = RTMath.Clamp(modifier.GetFloat(2, 0f, variables), 0f, 1f);
            var thickness = RTMath.Clamp(modifier.GetFloat(3, 1f, variables), 0f, 1f);
            var slices = RTMath.Clamp(modifier.GetInt(4, 3, variables), 0, sides);
            var thicknessOffset = new Vector2(modifier.GetFloat(5, 0f, variables), modifier.GetFloat(6, 0f, variables));
            var thicknessScale = new Vector2(modifier.GetFloat(7, 1f, variables), modifier.GetFloat(8, 1f, variables));
            var rotation = modifier.GetFloat(9, 0f, variables);

            var meshParams = new VGShapes.MeshParams
            {
                radius = radius,
                VertexCount = sides,
                cornerRoundness = roundness,
                thickness = thickness,
                SliceCount = slices,
                thicknessOffset = thicknessOffset,
                thicknessScale = thicknessScale,
                rotation = rotation,
            };

            if (modifier.TryGetResult(out VGShapes.MeshParams cache) && meshParams.Equals(cache))
                return;

            polygonObject.UpdatePolygon(radius, sides, roundness, thickness, slices, thicknessOffset, thicknessScale, rotation);
            modifier.Result = meshParams;
        }

        public static void setPolygonShapeOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var radius = RTMath.Clamp(modifier.GetFloat(1, 0.5f, variables), 0.1f, 10f);
            var sides = RTMath.Clamp(modifier.GetInt(2, 3, variables), 3, 32);
            var roundness = RTMath.Clamp(modifier.GetFloat(3, 0f, variables), 0f, 1f);
            var thickness = RTMath.Clamp(modifier.GetFloat(4, 1f, variables), 0f, 1f);
            var slices = RTMath.Clamp(modifier.GetInt(5, 3, variables), 0, sides);
            var thicknessOffset = new Vector2(modifier.GetFloat(6, 0f, variables), modifier.GetFloat(7, 0f, variables));
            var thicknessScale = new Vector2(modifier.GetFloat(8, 1f, variables), modifier.GetFloat(9, 1f, variables));
            var rotation = modifier.GetFloat(10, 0f, variables);

            var meshParams = new VGShapes.MeshParams
            {
                VertexCount = sides,
                cornerRoundness = roundness,
                thickness = thickness,
                SliceCount = slices,
                thicknessOffset = thicknessOffset,
                thicknessScale = thicknessScale,
                rotation = rotation,
            };

            if (modifier.TryGetResult(out VGShapes.MeshParams cache) && meshParams.Equals(cache))
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(0, variables));

            for (int i = 0; i < list.Count; i++)
            {
                var beatmapObject = list[i];
                if (beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject is PolygonObject polygonObject)
                    polygonObject.UpdatePolygon(radius, sides, roundness, thickness, slices, thicknessOffset, thicknessScale, rotation);
            }

            modifier.Result = meshParams;
        }

        public static void actorFrameTexture(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference.ShapeType != ShapeType.Image || !modifier.reference.runtimeObject || modifier.reference.runtimeObject.visualObject is not ImageObject imageObject)
                return;

            var camera = modifier.GetInt(0, 0, variables) == 0 ? EventManager.inst.cam : EventManager.inst.camPer;

            var frame = SpriteHelper.CaptureFrame(camera, modifier.GetInt(1, 512, variables), modifier.GetInt(2, 512, variables), modifier.GetFloat(3, 0f, variables), modifier.GetFloat(4, 0f, variables));

            var renderer = (SpriteRenderer)imageObject.renderer;

            if (modifier.HasResult() && renderer.sprite != LegacyPlugin.PALogoSprite)
            {
                if (renderer.sprite)
                    CoreHelper.Destroy(renderer.sprite.texture);
                CoreHelper.Destroy(renderer.sprite);
            }
            else
            {
                imageObject.gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
                modifier.Result = true;
            }

            renderer.sprite = frame;
        }

        public static void setImage(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.constant || modifier.reference.ShapeType != ShapeType.Image || !modifier.reference.runtimeObject || modifier.reference.runtimeObject.visualObject is not ImageObject imageObject)
                return;

            var value = modifier.GetValue(0, variables);
            var sprite = GameData.Current.assets.GetSprite(value);
            if (sprite)
            {
                imageObject.SetSprite(sprite);
                return;
            }

            var path = RTFile.CombinePaths(RTFile.BasePath, value);

            if (!RTFile.FileExists(path))
            {
                imageObject.SetDefaultSprite();
                return;
            }

            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadImageTexture("file://" + path, imageObject.SetTexture, imageObject.SetDefaultSprite));
        }
        
        public static void setImageOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.constant)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));

            if (list.IsEmpty())
                return;

            var value = modifier.GetValue(0, variables);

            var sprite = GameData.Current.assets.GetSprite(value);
            if (sprite)
            {
                foreach (var bm in list)
                {
                    if (bm.ShapeType == ShapeType.Image && bm.runtimeObject && bm.runtimeObject.visualObject is ImageObject imageObject)
                        imageObject.SetSprite(sprite);
                }
                return;
            }

            var path = RTFile.CombinePaths(RTFile.BasePath, value);
            if (!RTFile.FileExists(path))
            {
                foreach (var bm in list)
                {
                    if (bm.ShapeType == ShapeType.Image && bm.runtimeObject && bm.runtimeObject.visualObject is ImageObject imageObject)
                        imageObject.SetDefaultSprite();
                }
                return;
            }

            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadImageTexture("file://" + path, (Texture2D texture2D) =>
            {
                foreach (var bm in list)
                {
                    if (bm.ShapeType == ShapeType.Image && bm.runtimeObject && bm.runtimeObject.visualObject is ImageObject imageObject)
                        imageObject.SetTexture(texture2D);
                }
            }, onError =>
            {
                foreach (var bm in list)
                {
                    if (bm.ShapeType == ShapeType.Image && bm.runtimeObject && bm.runtimeObject.visualObject is ImageObject imageObject)
                        imageObject.SetDefaultSprite();
                }
            }));
        }

        public static void setText(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference.ShapeType != ShapeType.Text || !modifier.reference.runtimeObject || modifier.reference.runtimeObject.visualObject is not TextObject textObject)
                return;

            if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                textObject.SetText(modifier.GetValue(0, variables));
            else
                textObject.text = modifier.GetValue(0, variables);
        }

        public static void setTextOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));

            if (list.IsEmpty())
                return;

            var text = modifier.GetValue(0, variables);

            foreach (var bm in list)
            {
                if (bm.ShapeType != ShapeType.Text || !bm.runtimeObject || bm.runtimeObject.visualObject is not TextObject textObject)
                    continue;

                if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                    textObject.SetText(text);
                else
                    textObject.text = text;
            }
        }

        public static void addText(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference.ShapeType != ShapeType.Text || !modifier.reference.runtimeObject || modifier.reference.runtimeObject.visualObject is not TextObject textObject)
                return;

            if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                textObject.SetText(textObject.textMeshPro.text + modifier.GetValue(0, variables));
            else
                textObject.text += modifier.GetValue(0, variables);
        }

        public static void addTextOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));

            if (list.IsEmpty())
                return;

            var text = modifier.GetValue(0, variables);

            foreach (var bm in list)
            {
                if (bm.ShapeType != ShapeType.Text || !bm.runtimeObject || bm.runtimeObject.visualObject is not TextObject textObject)
                    continue;

                if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                    textObject.SetText(textObject.textMeshPro.text + text);
                else
                    textObject.text += text;
            }
        }

        public static void removeText(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference.ShapeType != ShapeType.Text || !modifier.reference.runtimeObject || modifier.reference.runtimeObject.visualObject is not TextObject textObject)
                return;

            string text = string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty :
                textObject.textMeshPro.text.Substring(0, textObject.textMeshPro.text.Length - Mathf.Clamp(modifier.GetInt(0, 1, variables), 0, textObject.textMeshPro.text.Length));

            if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                textObject.SetText(text);
            else
                textObject.text = text;
        }

        public static void removeTextOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));

            if (list.IsEmpty())
                return;

            var remove = modifier.GetInt(0, 1, variables);

            foreach (var bm in list)
            {
                var levelObject = bm.runtimeObject;
                if (bm.ShapeType != ShapeType.Text || !levelObject || levelObject.visualObject is not TextObject textObject)
                    continue;

                string text = string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty :
                    textObject.textMeshPro.text.Substring(0, textObject.textMeshPro.text.Length - Mathf.Clamp(remove, 0, textObject.textMeshPro.text.Length));

                if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                    textObject.SetText(text);
                else
                    textObject.text = text;
            }
        }

        public static void removeTextAt(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference.ShapeType != ShapeType.Text || !modifier.reference.runtimeObject || modifier.reference.runtimeObject.visualObject is not TextObject textObject)
                return;

            var remove = modifier.GetInt(0, 1, variables);
            string text = string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty : textObject.textMeshPro.text.Length > remove ?
                textObject.textMeshPro.text.Remove(remove, 1) : string.Empty;

            if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                textObject.SetText(text);
            else
                textObject.text = text;
        }

        public static void removeTextOtherAt(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));

            if (list.IsEmpty())
                return;

            var remove = modifier.GetInt(0, 1, variables);

            foreach (var bm in list)
            {
                if (bm.ShapeType != ShapeType.Text || !bm.runtimeObject || bm.runtimeObject.visualObject is not TextObject textObject)
                    continue;

                string text = string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty : textObject.textMeshPro.text.Length > remove ?
                    textObject.textMeshPro.text.Remove(remove, 1) : string.Empty;

                if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                    textObject.SetText(text);
                else
                    textObject.text = text;
            }
        }

        public static void formatText(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!CoreConfig.Instance.AllowCustomTextFormatting.Value && modifier.reference.ShapeType == ShapeType.Text &&
                modifier.reference.runtimeObject is RTBeatmapObject levelObject && levelObject.visualObject is TextObject textObject)
                textObject.SetText(RTString.FormatText(modifier.reference, textObject.text, variables));
        }

        public static void textSequence(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference.ShapeType != ShapeType.Text || !modifier.reference.runtimeObject || modifier.reference.runtimeObject.visualObject is not TextObject textObject)
                return;

            var value = modifier.GetValue(9, variables);
            var text = !string.IsNullOrEmpty(value) ? value : modifier.reference.text;

            if (!modifier.setTimer)
            {
                modifier.setTimer = true;
                modifier.ResultTimer = AudioManager.inst.CurrentAudioSource.time;
            }

            var offsetTime = modifier.ResultTimer;
            if (!modifier.GetBool(11, false, variables))
                offsetTime = modifier.reference.StartTime;

            var time = AudioManager.inst.CurrentAudioSource.time - offsetTime + modifier.GetFloat(10, 0f, variables);
            var length = modifier.GetFloat(0, 1f, variables);
            var glitch = modifier.GetBool(1, true, variables);

            var p = time / length;

            var textWithoutFormatting = text;
            var tagLocations = new List<Vector2Int>();
            RTString.RegexMatches(text, new Regex(@"<(.*?)>"), match =>
            {
                textWithoutFormatting = textWithoutFormatting.Replace(match.Groups[0].ToString(), "");
                tagLocations.Add(new Vector2Int(match.Index, match.Length - 1));
            });

            var stringLength2 = (int)Mathf.Lerp(0, textWithoutFormatting.Length, p);
            textObject.textMeshPro.maxVisibleCharacters = stringLength2;

            if (glitch && (int)RTMath.Lerp(0, textWithoutFormatting.Length, p) <= textWithoutFormatting.Length)
            {
                int insert = Mathf.Clamp(stringLength2 - 1, 0, text.Length);
                for (int i = 0; i < tagLocations.Count; i++)
                {
                    var tagLocation = tagLocations[i];
                    if (insert >= tagLocation.x)
                        insert += tagLocation.y + 1;
                }

                text = text.Insert(insert, LSText.randomString(1));
            }

            if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                textObject.SetText(text);
            else
                textObject.text = text;

            if ((modifier.Result is not int result || result != stringLength2) && textWithoutFormatting[Mathf.Clamp(stringLength2 - 1, 0, textWithoutFormatting.Length - 1)] != ' ')
            {
                modifier.Result = stringLength2;
                float pitch = modifier.GetFloat(6, 1f, variables);
                float volume = modifier.GetFloat(7, 1f, variables);
                float pitchVary = modifier.GetFloat(8, 0f, variables);

                if (pitchVary != 0f)
                    pitch += UnityEngine.Random.Range(-pitchVary, pitchVary);

                // Don't play any sounds.
                if (!modifier.GetBool(2, true, variables))
                    return;

                // Don't play custom sound.
                if (!modifier.GetBool(3, false, variables))
                {
                    SoundManager.inst.PlaySound(DefaultSounds.Click, volume, volume);
                    return;
                }

                var soundName = modifier.GetValue(4, variables);
                if (GameData.Current.assets.sounds.TryFind(x => x.name == soundName, out SoundAsset soundAsset) && soundAsset.audio)
                    SoundManager.inst.PlaySound(soundAsset.audio, volume, pitch);
                else if (SoundManager.inst.TryGetSound(soundName, out AudioClip audioClip))
                    SoundManager.inst.PlaySound(audioClip, volume, pitch);
                else
                    ModifiersHelper.GetSoundPath(modifier.reference.id, soundName, modifier.GetBool(5, false, variables), pitch, volume, false);
            }
        }

        // modify shape
        public static void backgroundShape(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var levelObject = modifier.reference.runtimeObject;
            if (modifier.HasResult() || modifier.reference.IsSpecialShape || !levelObject || !levelObject.visualObject || !levelObject.visualObject.gameObject)
                return;

            if (ShapeManager.inst.Shapes3D.TryGetAt(modifier.reference.Shape, out ShapeGroup shapeGroup) && shapeGroup.TryGetShape(modifier.reference.ShapeOption, out Shape shape))
            {
                levelObject.visualObject.gameObject.GetComponent<MeshFilter>().mesh = shape.mesh;
                modifier.Result = "frick";
                levelObject.visualObject.gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
            }
        }

        public static void sphereShape(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var levelObject = modifier.reference.runtimeObject;
            if (modifier.HasResult() || modifier.reference.IsSpecialShape || !levelObject || !levelObject.visualObject || !levelObject.visualObject.gameObject)
                return;

            levelObject.visualObject.gameObject.GetComponent<MeshFilter>().mesh = GameManager.inst.PlayerPrefabs[1].GetComponentInChildren<MeshFilter>().mesh;
            modifier.Result = "frick";
            levelObject.visualObject.gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
        }

        public static void translateShape(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var runtimeObject = modifier.reference.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject.gameObject)
                return;

            var pos = new Vector2(modifier.GetFloat(1, 0f, variables), modifier.GetFloat(2, 0f, variables));
            var sca = new Vector2(modifier.GetFloat(3, 0f, variables), modifier.GetFloat(4, 0f, variables));
            var rot = modifier.GetFloat(5, 0f, variables);

            if (!modifier.HasResult())
            {
                var meshFilter = runtimeObject.visualObject.gameObject.GetComponent<MeshFilter>();
                var collider2D = runtimeObject.visualObject.collider as PolygonCollider2D;
                var mesh = meshFilter.mesh;

                var translateShapeCache = new TranslateShapeCache
                {
                    meshFilter = meshFilter,
                    collider2D = collider2D,
                    vertices = mesh?.vertices ?? null,
                    points = collider2D?.points ?? null,

                    pos = pos,
                    sca = sca,
                    rot = rot,
                };
                modifier.Result = translateShapeCache;
                // force translate for first frame
                translateShapeCache.Translate(pos, sca, rot, true);

                runtimeObject.visualObject.gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
                return;
            }

            if (modifier.TryGetResult(out TranslateShapeCache shapeCache))
                shapeCache.Translate(pos, sca, rot);
        }

        #endregion

        #region Animation

        public static void animateObject<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var time = modifier.GetFloat(0, 0f, variables);
            var type = modifier.GetInt(1, 0, variables);
            var x = modifier.GetFloat(2, 0f, variables);
            var y = modifier.GetFloat(3, 0f, variables);
            var z = modifier.GetFloat(4, 0f, variables);
            var relative = modifier.GetBool(5, true, variables);

            string easing = modifier.GetValue(6, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var applyDeltaTime = modifier.GetBool(7, true, variables);

            ITransformable transformable;
            if (modifier.referenceType == ModifierReferenceType.CustomPlayer)
            {
                var id = modifier.GetValue(7, variables);
                if (modifier.reference is CustomPlayer customPlayer && customPlayer.Player && customPlayer.Player.customObjects.TryFind(x => x.id == id, out RTPlayer.CustomObject customObject))
                    transformable = customObject;
                else
                    transformable = null;
            }
            else
                transformable = modifier.reference as ITransformable;

            if (transformable == null)
                return;

            Vector3 vector = transformable.GetTransformOffset(type);

            var setVector = new Vector3(x, y, z);
            if (relative)
            {
                if (modifier.constant && applyDeltaTime)
                    setVector *= CoreHelper.TimeFrame;

                setVector += vector;
            }

            if (!modifier.constant)
            {
                var animation = new RTAnimation("Animate Object Offset");

                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                    {
                        new Vector3Keyframe(0f, vector, Ease.Linear),
                        new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.GetEaseFunction(easing, Ease.Linear)),
                    }, vector3 => transformable.SetTransform(type, vector3), interpolateOnComplete: true),
                };
                animation.SetDefaultOnComplete(false);
                AnimationManager.inst.Play(animation);
                return;
            }

            transformable.SetTransform(type, setVector);
        }
        
        public static void animateObjectOther<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(7));

            if (list.IsEmpty())
                return;

            var time = modifier.GetFloat(0, 0f, variables);
            var type = modifier.GetInt(1, 0, variables);
            var x = modifier.GetFloat(2, 0f, variables);
            var y = modifier.GetFloat(3, 0f, variables);
            var z = modifier.GetFloat(4, 0f, variables);
            var relative = modifier.GetBool(5, true, variables);

            string easing = modifier.GetValue(6, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var applyDeltaTime = modifier.GetBool(8, true, variables);

            foreach (var bm in list)
            {
                Vector3 vector = bm.GetTransformOffset(type);

                var setVector = new Vector3(x, y, z);
                if (relative)
                {
                    if (modifier.constant && applyDeltaTime)
                        setVector *= CoreHelper.TimeFrame;

                    setVector += vector;
                }

                if (!modifier.constant)
                {
                    var animation = new RTAnimation("Animate Other Object Offset");

                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                        {
                            new Vector3Keyframe(0f, vector, Ease.Linear),
                            new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, vector3 => bm.SetTransform(type, vector3), interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete(false);
                    AnimationManager.inst.Play(animation);
                    continue;
                }

                bm.SetTransform(type, setVector);
            }
        }

        // tests modifier keyframing
        // todo: see if i can get homing to work via adding a keyframe depending on audio time
        public static void animateObjectKF<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not ITransformable transformable || modifier.reference is not ILifetime<AutoKillType> lifetime)
                return;

            var audioTime = modifier.GetFloat(0, 0f, variables);
            var type = modifier.GetInt(1, 0, variables);

            Sequence<Vector3> sequence;

            if (modifier.HasResult())
                sequence = modifier.GetResult<Sequence<Vector3>>();
            else
            {
                // get starting position.
                var vector = transformable.GetTransformOffset(type);

                // a custom start position can be registered if you want.
                var xStart = modifier.GetValue(2, variables);
                var yStart = modifier.GetValue(3, variables);
                var zStart = modifier.GetValue(4, variables);
                if (float.TryParse(xStart, out float xS))
                    vector.x = xS;
                if (float.TryParse(yStart, out float yS))
                    vector.y = yS;
                if (float.TryParse(zStart, out float zS))
                    vector.z = zS;

                var currentTime = 0f;

                var keyframes = new List<IKeyframe<Vector3>>();
                keyframes.Add(new Vector3Keyframe(currentTime, vector, Ease.Linear));
                for (int i = 5; i < modifier.commands.Count; i += 6)
                {
                    var time = modifier.GetFloat(i, 0f, variables);
                    if (time < currentTime)
                        continue;

                    var x = modifier.GetFloat(i + 1, 0f, variables);
                    var y = modifier.GetFloat(i + 2, 0f, variables);
                    var z = modifier.GetFloat(i + 3, 0f, variables);
                    var relative = modifier.GetBool(i + 4, true, variables);

                    var easing = modifier.GetValue(i + 5, variables);
                    if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                        easing = DataManager.inst.AnimationList[e].Name;

                    var setVector = new Vector3(x, y, z);
                    if (relative)
                        setVector += vector;

                    keyframes.Add(new Vector3Keyframe(currentTime + time, setVector, Ease.GetEaseFunction(easing, Ease.Linear)));

                    vector = setVector;
                    currentTime = time;
                }

                sequence = new Sequence<Vector3>(keyframes);
            }

            if (sequence != null)
                transformable.SetTransform(type, sequence.Interpolate(audioTime - lifetime.StartTime));
        }

        public static void animateSignal<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable || modifier.reference is not ITransformable transformable)
                return;

            var time = modifier.GetFloat(0, 0f, variables);
            var type = modifier.GetInt(1, 0, variables);
            var x = modifier.GetFloat(2, 0f, variables);
            var y = modifier.GetFloat(3, 0f, variables);
            var z = modifier.GetFloat(4, 0f, variables);
            var relative = modifier.GetBool(5, true, variables);
            var signalGroup = modifier.GetValue(7, variables);
            var delay = modifier.GetFloat(8, 0f, variables);

            if (!modifier.GetBool(9, true, variables))
            {
                var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, signalGroup);

                foreach (var bm in list)
                {
                    if (!bm.modifiers.IsEmpty() && !bm.modifiers.FindAll(x => x.Name == "requireSignal" && x.type == ModifierBase.Type.Trigger).IsEmpty() &&
                        bm.modifiers.TryFind(x => x.Name == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> m))
                        m.Result = null;
                }
            }

            string easing = modifier.GetValue(6, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var applyDeltaTime = modifier.GetBool(10, true, variables);

            Vector3 vector = transformable.GetTransformOffset(type);

            var setVector = new Vector3(x, y, z);
            if (relative)
            {
                if (modifier.constant && applyDeltaTime)
                    setVector *= CoreHelper.TimeFrame;

                setVector += vector;
            }

            if (!modifier.constant)
            {
                var animation = new RTAnimation("Animate Object Offset");

                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                    {
                        new Vector3Keyframe(0f, vector, Ease.Linear),
                        new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.HasEaseFunction(easing) ? Ease.GetEaseFunction(easing) : Ease.Linear),
                    }, vector3 => transformable.SetTransform(type, vector3), interpolateOnComplete: true),
                };
                animation.onComplete = () =>
                {
                    AnimationManager.inst.Remove(animation.id);

                    var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, signalGroup);

                    foreach (var bm in list)
                        CoroutineHelper.StartCoroutine(ModifiersHelper.ActivateModifier(bm, delay));
                };
                AnimationManager.inst.Play(animation);
                return;
            }

            transformable.SetTransform(type, setVector);
        }

        public static void animateSignalOther<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(7, variables));

            if (list.IsEmpty())
                return;

            var time = modifier.GetFloat(0, 0f, variables);
            var type = modifier.GetInt(1, 0, variables);
            var x = modifier.GetFloat(2, 0f, variables);
            var y = modifier.GetFloat(3, 0f, variables);
            var z = modifier.GetFloat(4, 0f, variables);
            var relative = modifier.GetBool(5, true, variables);
            var signalGroup = modifier.GetValue(8, variables);
            var delay = modifier.GetFloat(9, 0f, variables);

            if (!modifier.GetBool(10, true, variables))
            {
                var list2 = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, signalGroup);

                foreach (var bm in list2)
                {
                    if (!bm.modifiers.IsEmpty() && !bm.modifiers.FindAll(x => x.Name == "requireSignal" && x.type == ModifierBase.Type.Trigger).IsEmpty() &&
                        bm.modifiers.TryFind(x => x.Name == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> m))
                    {
                        m.Result = null;
                    }
                }
            }

            string easing = modifier.GetValue(6, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var applyDeltaTime = modifier.GetBool(11, true, variables);

            foreach (var bm in list)
            {
                Vector3 vector = bm.GetTransformOffset(type);

                var setVector = new Vector3(x, y, z);
                if (relative)
                {
                    if (modifier.constant && applyDeltaTime)
                        setVector *= CoreHelper.TimeFrame;

                    setVector += vector;
                }

                if (!modifier.constant)
                {
                    var animation = new RTAnimation("Animate Other Object Offset");

                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                        {
                            new Vector3Keyframe(0f, vector, Ease.Linear),
                            new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, vector3 => bm.SetTransform(type, vector3), interpolateOnComplete: true),
                    };
                    animation.onComplete = () =>
                    {
                        AnimationManager.inst.Remove(animation.id);

                        var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, signalGroup);

                        foreach (var bm in list)
                            CoroutineHelper.StartCoroutine(ModifiersHelper.ActivateModifier(bm, delay));
                    };
                    AnimationManager.inst.Play(animation);
                    break;
                }

                bm.SetTransform(type, setVector);
            }
        }
        
        public static void animateObjectMath<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not ITransformable transformable || modifier.reference is not IEvaluatable evaluatable)
                return;

            var numberVariables = evaluatable.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);

            var functions = evaluatable.GetObjectFunctions();

            float time = (float)RTMath.Parse(modifier.GetValue(0, variables), numberVariables, functions);
            var type = modifier.GetInt(1, 0, variables);
            float x = (float)RTMath.Parse(modifier.GetValue(2, variables), numberVariables, functions);
            float y = (float)RTMath.Parse(modifier.GetValue(3, variables), numberVariables, functions);
            float z = (float)RTMath.Parse(modifier.GetValue(4, variables), numberVariables, functions);
            var relative = modifier.GetBool(5, true, variables);

            string easing = modifier.GetValue(6, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var applyDeltaTime = modifier.GetBool(7, true, variables);

            Vector3 vector = transformable.GetTransformOffset(type);

            var setVector = new Vector3(x, y, z);
            if (relative)
            {
                if (modifier.constant && applyDeltaTime)
                    setVector *= CoreHelper.TimeFrame;

                setVector += vector;
            }

            if (!modifier.constant)
            {
                var animation = new RTAnimation("Animate Object Offset");

                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                    {
                        new Vector3Keyframe(0f, vector, Ease.Linear),
                        new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.GetEaseFunction(easing, Ease.Linear)),
                    }, vector3 => transformable.SetTransform(type, vector3), interpolateOnComplete: true),
                };
                animation.SetDefaultOnComplete(false);
                AnimationManager.inst.Play(animation);
                return;
            }

            transformable.SetTransform(type, setVector);
        }
        
        public static void animateObjectMathOther<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable || modifier.reference is not IEvaluatable evaluatable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(7, variables));

            if (list.IsEmpty())
                return;

            var numberVariables = evaluatable.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);

            var functions = evaluatable.GetObjectFunctions();

            // for optimization sake, we evaluate this outside of the foreach loop. normally I'd place this inside and replace "otherVar" with bm.integerVariable.ToString(), however I feel that would result in a worse experience so the tradeoff is not worth it.
            float time = (float)RTMath.Parse(modifier.GetValue(0, variables), numberVariables, functions);
            var type = modifier.GetInt(1, 0, variables);
            float x = (float)RTMath.Parse(modifier.GetValue(2, variables), numberVariables, functions);
            float y = (float)RTMath.Parse(modifier.GetValue(3, variables), numberVariables, functions);
            float z = (float)RTMath.Parse(modifier.GetValue(4, variables), numberVariables, functions);
            var relative = modifier.GetBool(5, true, variables);

            string easing = modifier.GetValue(6, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var applyDeltaTime = modifier.GetBool(8, true, variables);

            foreach (var bm in list)
            {
                Vector3 vector = bm.GetTransformOffset(type);

                var setVector = new Vector3(x, y, z);
                if (relative)
                {
                    if (modifier.constant && applyDeltaTime)
                        setVector *= CoreHelper.TimeFrame;

                    setVector += vector;
                }

                if (!modifier.constant)
                {
                    var animation = new RTAnimation("Animate Other Object Offset");

                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                        {
                            new Vector3Keyframe(0f, vector, Ease.Linear),
                            new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, vector3 => bm.SetTransform(type, vector3), interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete(false);
                    AnimationManager.inst.Play(animation);
                    continue;
                }

                bm.SetTransform(type, setVector);
            }
        }
        
        public static void animateSignalMath<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IEvaluatable evaluatable || modifier.reference is not IPrefabable prefabable || modifier.reference is not ITransformable transformable)
                return;

            var numberVariables = evaluatable.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);

            var functions = evaluatable.GetObjectFunctions();

            float time = (float)RTMath.Parse(modifier.GetValue(0, variables), numberVariables, functions);
            var type = modifier.GetInt(1, 0, variables);
            float x = (float)RTMath.Parse(modifier.GetValue(2, variables), numberVariables, functions);
            float y = (float)RTMath.Parse(modifier.GetValue(3, variables), numberVariables, functions);
            float z = (float)RTMath.Parse(modifier.GetValue(4, variables), numberVariables, functions);
            var relative = modifier.GetBool(5, true, variables);
            var signalGroup = modifier.GetValue(7, variables);
            float signalTime = (float)RTMath.Parse(modifier.GetValue(8, variables), numberVariables, functions);

            if (!modifier.GetBool(9, true, variables))
            {
                var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, signalGroup);

                foreach (var bm in list)
                {
                    if (!bm.modifiers.IsEmpty() && !bm.modifiers.FindAll(x => x.Name == "requireSignal" && x.type == ModifierBase.Type.Trigger).IsEmpty() &&
                        bm.modifiers.TryFind(x => x.Name == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> m))
                        m.Result = null;
                }
            }

            string easing = modifier.GetValue(6, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var applyDeltaTime = modifier.GetBool(10, true, variables);

            Vector3 vector = transformable.GetTransformOffset(type);

            var setVector = new Vector3(x, y, z);
            if (relative)
            {
                if (modifier.constant && applyDeltaTime)
                    setVector *= CoreHelper.TimeFrame;

                setVector += vector;
            }

            if (!modifier.constant)
            {
                var animation = new RTAnimation("Animate Object Offset");

                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                    {
                        new Vector3Keyframe(0f, vector, Ease.Linear),
                        new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.GetEaseFunction(easing, Ease.Linear)),
                    }, vector3 => transformable.SetTransform(type, vector3), interpolateOnComplete: true),
                };
                animation.onComplete = () =>
                {
                    AnimationManager.inst.Remove(animation.id);

                    var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, signalGroup);

                    foreach (var bm in list)
                        CoroutineHelper.StartCoroutine(ModifiersHelper.ActivateModifier(bm, signalTime));
                };
                AnimationManager.inst.Play(animation);
                return;
            }

            transformable.SetTransform(type, setVector);
        }
        
        public static void animateSignalMathOther<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable || modifier.reference is not IEvaluatable evaluatable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(7, variables));

            if (list.IsEmpty())
                return;

            var numberVariables = evaluatable.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);

            var functions = evaluatable.GetObjectFunctions();

            var time = (float)RTMath.Parse(modifier.GetValue(0, variables), numberVariables, functions);
            var type = modifier.GetInt(1, 0, variables);
            var x = (float)RTMath.Parse(modifier.GetValue(2, variables), numberVariables, functions);
            var y = (float)RTMath.Parse(modifier.GetValue(3, variables), numberVariables, functions);
            var z = (float)RTMath.Parse(modifier.GetValue(4, variables), numberVariables, functions);
            var relative = modifier.GetBool(5, true, variables);
            var signalGroup = modifier.GetValue(8, variables);
            var signalTime = (float)RTMath.Parse(modifier.GetValue(9, variables), numberVariables);

            if (!modifier.GetBool(10, true, variables))
            {
                var list2 = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, signalGroup);

                foreach (var bm in list2)
                {
                    if (!bm.modifiers.IsEmpty() && bm.modifiers.FindAll(x => x.Name == "requireSignal" && x.type == ModifierBase.Type.Trigger).Count > 0 &&
                        bm.modifiers.TryFind(x => x.Name == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> m))
                        m.Result = null;
                }
            }

            string easing = modifier.GetValue(6, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var applyDeltaTime = modifier.GetBool(11, true, variables);

            foreach (var bm in list)
            {
                Vector3 vector = bm.GetTransformOffset(type);

                var setVector = new Vector3(x, y, z);
                if (relative)
                {
                    if (modifier.constant && applyDeltaTime)
                        setVector *= CoreHelper.TimeFrame;

                    setVector += vector;
                }

                if (!modifier.constant)
                {
                    var animation = new RTAnimation("Animate Other Object Offset");

                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                        {
                            new Vector3Keyframe(0f, vector, Ease.Linear),
                            new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.GetEaseFunction(easing, Ease.Linear)),
                        }, vector3 => bm.SetTransform(type, vector3), interpolateOnComplete: true),
                    };
                    animation.onComplete = () =>
                    {
                        AnimationManager.inst.Remove(animation.id);

                        var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, signalGroup);

                        foreach (var bm in list)
                            CoroutineHelper.StartCoroutine(ModifiersHelper.ActivateModifier(bm, signalTime));
                    };
                    AnimationManager.inst.Play(animation);
                    continue;
                }

                bm.SetTransform(type, setVector);
            }
        }
        
        public static void gravity<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not ITransformable transformable)
                return;

            var gravityX = modifier.GetFloat(1, 0f, variables);
            var gravityY = modifier.GetFloat(2, 0f, variables);
            var time = modifier.GetFloat(3, 1f, variables);
            var curve = modifier.GetInt(4, 2, variables);

            if (modifier.Result == null)
            {
                modifier.Result = Vector2.zero;
                modifier.ResultTimer = Time.time;
            }
            else
                modifier.Result = RTMath.Lerp(Vector2.zero, new Vector2(gravityX, gravityY), (RTMath.Recursive(Time.time - modifier.ResultTimer, curve)) * (time * CoreHelper.TimeFrame));

            var vector = (Vector2)modifier.Result;

            var rotation = modifier.reference is BeatmapObject beatmapObject ? beatmapObject.InterpolateChainRotation(includeSelf: false) : 0f;

            transformable.PositionOffset = RTMath.Rotate(vector, -rotation);
        }
        
        public static void gravityOther<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable)
                return;

            var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(0, variables));

            if (beatmapObjects.IsEmpty())
                return;

            var gravityX = modifier.GetFloat(1, 0f, variables);
            var gravityY = modifier.GetFloat(2, 0f, variables);
            var time = modifier.GetFloat(3, 1f, variables);
            var curve = modifier.GetInt(4, 2, variables);

            if (modifier.Result == null)
            {
                modifier.Result = Vector2.zero;
                modifier.ResultTimer = Time.time;
            }
            else
                modifier.Result = RTMath.Lerp(Vector2.zero, new Vector2(gravityX, gravityY), (RTMath.Recursive(Time.time - modifier.ResultTimer, curve)) * (time * CoreHelper.TimeFrame));

            var vector = (Vector2)modifier.Result;

            foreach (var beatmapObject in beatmapObjects)
            {
                var rotation = beatmapObject.InterpolateChainRotation(includeSelf: false);

                beatmapObject.positionOffset = RTMath.Rotate(vector, -rotation);
            }
        }
        
        public static void copyAxis<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not ITransformable transformable)
                return;

            var fromType = modifier.GetInt(1, 0, variables);
            var fromAxis = modifier.GetInt(2, 0, variables);
            var toType = modifier.GetInt(3, 0, variables);
            var toAxis = modifier.GetInt(4, 0, variables);
            var delay = modifier.GetFloat(5, 0f, variables);
            var multiply = modifier.GetFloat(6, 0f, variables);
            var offset = modifier.GetFloat(7, 0f, variables);
            var min = modifier.GetFloat(8, -9999f, variables);
            var max = modifier.GetFloat(9, 9999f, variables);
            var loop = modifier.GetFloat(10, 9999f, variables);
            var useVisual = modifier.GetBool(11, false, variables);

            if (!modifier.HasResult())
            {
                if (modifier.reference is IPrefabable prefabable && GameData.Current.TryFindObjectWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(0), out BeatmapObject result))
                    modifier.Result = result;
            }

            if (!modifier.TryGetResult(out BeatmapObject bm))
                return;

            var time = RTLevel.Current.CurrentTime;

            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);

            if (toType < 0 || toType > 3)
                return;

            if (!useVisual && bm.cachedSequences)
            {
                if (fromType == 3)
                {
                    if (toType == 3 && toAxis == 0 && bm.cachedSequences.ColorSequence != null &&
                        modifier.reference is BeatmapObject beatmapObject && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject)
                    {
                        var sequence = bm.cachedSequences.ColorSequence.Interpolate(time - bm.StartTime - delay);
                        var visualObject = beatmapObject.runtimeObject.visualObject;
                        visualObject.SetColor(RTMath.Lerp(visualObject.GetPrimaryColor(), sequence, multiply));
                    }
                    return;
                }
                transformable.SetTransform(toType, toAxis, fromType switch
                {
                    0 => Mathf.Clamp((bm.cachedSequences.PositionSequence.Interpolate(time - bm.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                    1 => Mathf.Clamp((bm.cachedSequences.ScaleSequence.Interpolate(time - bm.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                    2 => Mathf.Clamp((bm.cachedSequences.RotationSequence.Interpolate(time - bm.StartTime - delay) - offset) * multiply % loop, min, max),
                    _ => 0f,
                });
            }
            else if (useVisual && bm.runtimeObject is RTBeatmapObject levelObject && levelObject.visualObject && levelObject.visualObject.gameObject)
                transformable.SetTransform(toType, toAxis, Mathf.Clamp((levelObject.visualObject.gameObject.transform.GetVector(fromType).At(fromAxis) - offset) * multiply % loop, min, max));
            else if (useVisual)
                transformable.SetTransform(toType, toAxis, Mathf.Clamp(fromType switch
                {
                    0 => bm.InterpolateChainPosition().At(fromAxis),
                    1 => bm.InterpolateChainScale().At(fromAxis),
                    2 => bm.InterpolateChainRotation(),
                    _ => 0f,
                }, min, max));
        }
        
        public static void copyAxisMath<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable || modifier.reference is not IEvaluatable evaluatable || modifier.reference is not ITransformable transformable)
                return;

            try
            {
                var fromType = modifier.GetInt(1, 0, variables);
                var fromAxis = modifier.GetInt(2, 0, variables);
                var toType = modifier.GetInt(3, 0, variables);
                var toAxis = modifier.GetInt(4, 0, variables);
                var delay = modifier.GetFloat(5, 0f, variables);
                var min = modifier.GetFloat(6, -9999f, variables);
                var max = modifier.GetFloat(7, 9999f, variables);
                var evaluation = modifier.GetValue(8, variables);
                var useVisual = modifier.GetBool(9, false, variables);

                if (!GameData.Current.TryFindObjectWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(0, variables), out BeatmapObject bm))
                    return;

                var time = RTLevel.Current.CurrentTime;

                fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);

                if (toType < 0 || toType > 3)
                    return;

                if (!useVisual && bm.cachedSequences)
                {
                    if (fromType == 3)
                    {
                        if (toType == 3 && toAxis == 0 && bm.cachedSequences.ColorSequence != null &&
                            modifier.reference is BeatmapObject beatmapObject && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject &&
                            beatmapObject.runtimeObject.visualObject.renderer)
                        {
                            // queue post tick so the color overrides the sequence color
                            RTLevel.Current.postTick.Enqueue(() =>
                            {
                                var sequence = bm.cachedSequences.ColorSequence.Interpolate(time - bm.StartTime - delay);

                                var renderer = beatmapObject.runtimeObject.visualObject.renderer;

                                var numberVariables = beatmapObject.GetObjectVariables();
                                ModifiersHelper.SetVariables(variables, numberVariables);

                                numberVariables["colorR"] = sequence.r;
                                numberVariables["colorG"] = sequence.g;
                                numberVariables["colorB"] = sequence.b;
                                numberVariables["colorA"] = sequence.a;
                                bm.SetOtherObjectVariables(numberVariables);

                                float value = RTMath.Parse(evaluation, numberVariables);

                                renderer.material.color = RTMath.Lerp(renderer.material.color, sequence, Mathf.Clamp(value, min, max));
                            });
                        }
                    }
                    else
                    {
                        var numberVariables = evaluatable.GetObjectVariables();
                        ModifiersHelper.SetVariables(variables, numberVariables);

                        if (bm.cachedSequences)
                            numberVariables["axis"] = fromType switch
                            {
                                0 => bm.cachedSequences.PositionSequence.Interpolate(time - bm.StartTime - delay).At(fromAxis),
                                1 => bm.cachedSequences.ScaleSequence.Interpolate(time - bm.StartTime - delay).At(fromAxis),
                                2 => bm.cachedSequences.RotationSequence.Interpolate(time - bm.StartTime - delay),
                                _ => 0f,
                            };
                        bm.SetOtherObjectVariables(numberVariables);

                        float value = RTMath.Parse(evaluation, numberVariables);

                        transformable.SetTransform(toType, toAxis, Mathf.Clamp(value, min, max));
                    }
                }
                else if (useVisual && bm.runtimeObject is RTBeatmapObject levelObject && levelObject.visualObject && levelObject.visualObject.gameObject)
                {
                    var axis = levelObject.visualObject.gameObject.transform.GetVector(fromType).At(fromAxis);

                    var numberVariables = evaluatable.GetObjectVariables();
                    ModifiersHelper.SetVariables(variables, numberVariables);

                    numberVariables["axis"] = axis;
                    bm.SetOtherObjectVariables(numberVariables);

                    float value = RTMath.Parse(evaluation, numberVariables);

                    transformable.SetTransform(toType, toAxis, Mathf.Clamp(value, min, max));
                }
                else if (useVisual)
                {
                    var numberVariables = evaluatable.GetObjectVariables();
                    ModifiersHelper.SetVariables(variables, numberVariables);

                    numberVariables["axis"] = fromType switch
                    {
                        0 => bm.InterpolateChainPosition().At(fromAxis),
                        1 => bm.InterpolateChainScale().At(fromAxis),
                        2 => bm.InterpolateChainRotation(),
                        _ => 0f,
                    };
                    bm.SetOtherObjectVariables(numberVariables);

                    float value = RTMath.Parse(evaluation, numberVariables);

                    transformable.SetTransform(toType, toAxis, Mathf.Clamp(value, min, max));
                }
            }
            catch
            {

            } // try catch for cases where the math is broken
        }
        
        public static void copyAxisGroup<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable || modifier.reference is not ITransformable transformable || modifier.reference is not IEvaluatable evaluatable)
                return;

            var evaluation = modifier.GetValue(0, variables);

            var toType = modifier.GetInt(1, 0, variables);
            var toAxis = modifier.GetInt(2, 0, variables);

            if (toType < 0 || toType > 4)
                return;

            try
            {
                var beatmapObjects = GameData.Current.beatmapObjects;
                var prefabObjects = GameData.Current.prefabObjects;

                var time = RTLevel.Current.CurrentTime;
                var numberVariables = evaluatable.GetObjectVariables();
                ModifiersHelper.SetVariables(variables, numberVariables);

                if (!modifier.HasResult())
                {
                    var result = new List<BeatmapObject>();

                    for (int i = 3; i < modifier.commands.Count; i += 8)
                    {
                        var group = modifier.GetValue(i + 1);

                        if (GameData.Current.TryFindObjectWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, group, out BeatmapObject beatmapObject))
                            result.Add(beatmapObject);
                    }

                    modifier.Result = result;
                }

                if (!modifier.TryGetResult(out List<BeatmapObject> list))
                    return;

                int groupIndex = 0;
                for (int i = 3; i < modifier.commands.Count; i += 8)
                {
                    var name = modifier.GetValue(i, variables);
                    var group = modifier.GetValue(i + 1, variables);
                    var fromType = modifier.GetInt(i + 2, 0, variables);
                    var fromAxis = modifier.GetInt(i + 3, 0, variables);
                    var delay = modifier.GetFloat(i + 4, 0f, variables);
                    var min = modifier.GetFloat(i + 5, 0f, variables);
                    var max = modifier.GetFloat(i + 6, 0f, variables);
                    var useVisual = modifier.GetBool(i + 7, false, variables);

                    var beatmapObject = list[groupIndex];

                    if (!beatmapObject)
                    {
                        groupIndex++;
                        continue;
                    }

                    if (!useVisual && beatmapObject.cachedSequences)
                        numberVariables[name] = fromType switch
                        {
                            0 => Mathf.Clamp(beatmapObject.cachedSequences.PositionSequence.Interpolate(time - beatmapObject.StartTime - delay).At(fromAxis), min, max),
                            1 => Mathf.Clamp(beatmapObject.cachedSequences.ScaleSequence.Interpolate(time - beatmapObject.StartTime - delay).At(fromAxis), min, max),
                            2 => Mathf.Clamp(beatmapObject.cachedSequences.RotationSequence.Interpolate(time - beatmapObject.StartTime - delay), min, max),
                            _ => 0f,
                        };
                    else if (useVisual && beatmapObject.runtimeObject is RTBeatmapObject levelObject && levelObject.visualObject && levelObject.visualObject.gameObject)
                        numberVariables[name] = Mathf.Clamp(levelObject.visualObject.gameObject.transform.GetVector(fromType).At(fromAxis), min, max);
                    else if (useVisual)
                        numberVariables[name] = fromType switch
                        {
                            0 => Mathf.Clamp(beatmapObject.InterpolateChainPosition().At(fromAxis), min, max),
                            1 => Mathf.Clamp(beatmapObject.InterpolateChainScale().At(fromAxis), min, max),
                            2 => Mathf.Clamp(beatmapObject.InterpolateChainRotation(), min, max),
                            _ => 0f,
                        };

                    if (fromType == 4)
                        numberVariables[name] = Mathf.Clamp(beatmapObject.integerVariable, min, max);

                    groupIndex++;
                }

                transformable.SetTransform(toType, toAxis, RTMath.Parse(evaluation, numberVariables));
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }
        
        public static void copyPlayerAxis<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not ITransformable transformable)
                return;

            var fromType = modifier.GetInt(1, 0, variables);
            var fromAxis = modifier.GetInt(2, 0, variables);

            var toType = modifier.GetInt(3, 0, variables);
            var toAxis = modifier.GetInt(4, 0, variables);

            var delay = modifier.GetFloat(5, 0f, variables);
            var multiply = modifier.GetFloat(6, 0f, variables);
            var offset = modifier.GetFloat(7, 0f, variables);
            var min = modifier.GetFloat(8, -9999f, variables);
            var max = modifier.GetFloat(9, 9999f, variables);

            var players = PlayerManager.Players;

            if (players.TryFind(x => x.Player && x.Player.rb, out CustomPlayer customPlayer))
                transformable.SetTransform(toType, toAxis, Mathf.Clamp((customPlayer.Player.rb.transform.GetLocalVector(fromType).At(fromAxis) - offset) * multiply, min, max));
        }
        
        public static void legacyTail(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.reference || modifier.commands.IsEmpty() || !GameData.Current)
                return;

            var totalTime = modifier.GetFloat(0, 200f, variables);

            var list = modifier.Result is List<LegacyTracker> ? (List<LegacyTracker>)modifier.Result : new List<LegacyTracker>();

            if (!modifier.HasResult())
            {
                list.Add(new LegacyTracker(modifier.reference, Vector3.zero, Vector3.zero, Quaternion.identity, 0f, 0f));

                for (int i = 1; i < modifier.commands.Count; i += 3)
                {
                    var group = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(i, variables));

                    if (modifier.commands.Count <= i + 2 || group.Count < 1)
                        break;

                    var distance = modifier.GetFloat(i + 1, 2f, variables);
                    var time = modifier.GetFloat(i + 2, 12f, variables);

                    for (int j = 0; j < group.Count; j++)
                    {
                        var beatmapObject = group[j];
                        list.Add(new LegacyTracker(beatmapObject, beatmapObject.positionOffset, beatmapObject.positionOffset, Quaternion.Euler(beatmapObject.rotationOffset), distance, time));
                    }
                }

                modifier.Result = list;
            }

            var animationResult = modifier.reference.InterpolateChain();
            list[0].pos = animationResult.position;
            list[0].rot = Quaternion.Euler(0f, 0f, animationResult.rotation);

            float num = Time.deltaTime * totalTime;

            for (int i = 1; i < list.Count; i++)
            {
                var tracker = list[i];
                var prevTracker = list[i - 1];
                if (Vector3.Distance(tracker.pos, prevTracker.pos) > tracker.distance)
                {
                    var vector = Vector3.Lerp(tracker.pos, prevTracker.pos, Time.deltaTime * tracker.time);
                    var quaternion = Quaternion.Lerp(tracker.rot, prevTracker.rot, Time.deltaTime * tracker.time);
                    list[i].pos = vector;
                    list[i].rot = quaternion;
                }

                num *= Vector3.Distance(prevTracker.lastPos, tracker.pos);
                tracker.beatmapObject.positionOffset = Vector3.MoveTowards(prevTracker.lastPos, tracker.pos, num);
                prevTracker.lastPos = tracker.pos;
                tracker.beatmapObject.rotationOffset = tracker.rot.eulerAngles;
            }
        }
        
        public static void applyAnimation(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(0, variables), out BeatmapObject from))
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(10, variables));

            if (!modifier.HasResult())
                modifier.Result = RTLevel.Current?.CurrentTime ?? 0f;
            var time = modifier.GetResult<float>();

            var animatePos = modifier.GetBool(1, true, variables);
            var animateSca = modifier.GetBool(2, true, variables);
            var animateRot = modifier.GetBool(3, true, variables);
            var delayPos = modifier.GetFloat(4, 0f, variables);
            var delaySca = modifier.GetFloat(5, 0f, variables);
            var delayRot = modifier.GetFloat(6, 0f, variables);
            var useVisual = modifier.GetBool(7, false, variables);
            var length = modifier.GetFloat(8, 1f, variables);
            var speed = modifier.GetFloat(9, 1f, variables);

            if (!modifier.constant)
                AnimationManager.inst.RemoveName("Apply Object Animation " + modifier.reference.id);

            for (int i = 0; i < list.Count; i++)
            {
                var bm = list[i];

                if (!modifier.constant)
                {
                    var animation = new RTAnimation("Apply Object Animation " + modifier.reference.id);
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, 0f, Ease.Linear),
                            new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                        }, x => ModifiersHelper.ApplyAnimationTo(bm, from, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
                    };
                    animation.onComplete = () =>
                    {
                        AnimationManager.inst.Remove(animation.id);
                        animation = null;
                        modifier.Result = null;
                    };
                    AnimationManager.inst.Play(animation);
                    continue;
                }

                ModifiersHelper.ApplyAnimationTo(bm, from, useVisual, time, RTLevel.Current.CurrentTime, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
            }
        }
        
        public static void applyAnimationFrom(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(0, variables), out BeatmapObject bm))
                return;

            if (!modifier.HasResult())
                modifier.Result = RTLevel.Current?.CurrentTime ?? 0f;
            var time = modifier.GetResult<float>();

            var animatePos = modifier.GetBool(1, true, variables);
            var animateSca = modifier.GetBool(2, true, variables);
            var animateRot = modifier.GetBool(3, true, variables);
            var delayPos = modifier.GetFloat(4, 0f, variables);
            var delaySca = modifier.GetFloat(5, 0f, variables);
            var delayRot = modifier.GetFloat(6, 0f, variables);
            var useVisual = modifier.GetBool(7, false, variables);
            var length = modifier.GetFloat(8, 1f, variables);
            var speed = modifier.GetFloat(9, 1f, variables);

            if (!modifier.constant)
            {
                AnimationManager.inst.RemoveName("Apply Object Animation " + modifier.reference.id);

                var animation = new RTAnimation("Apply Object Animation " + modifier.reference.id);
                animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, 0f, Ease.Linear),
                            new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                        }, x => ModifiersHelper.ApplyAnimationTo(modifier.reference, bm, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
                    };
                animation.onComplete = () =>
                {
                    AnimationManager.inst.Remove(animation.id);
                    animation = null;
                    modifier.Result = null;
                };
                AnimationManager.inst.Play(animation);
                return;
            }

            ModifiersHelper.ApplyAnimationTo(modifier.reference, bm, useVisual, time, RTLevel.Current.CurrentTime, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
        }
        
        public static void applyAnimationTo(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(0, variables));

            if (!modifier.HasResult())
                modifier.Result = RTLevel.Current?.CurrentTime ?? 0f;
            var time = modifier.GetResult<float>();

            var animatePos = modifier.GetBool(1, true, variables);
            var animateSca = modifier.GetBool(2, true, variables);
            var animateRot = modifier.GetBool(3, true, variables);
            var delayPos = modifier.GetFloat(4, 0f, variables);
            var delaySca = modifier.GetFloat(5, 0f, variables);
            var delayRot = modifier.GetFloat(6, 0f, variables);
            var useVisual = modifier.GetBool(7, false, variables);
            var length = modifier.GetFloat(8, 1f, variables);
            var speed = modifier.GetFloat(9, 1f, variables);

            if (!modifier.constant)
                AnimationManager.inst.RemoveName("Apply Object Animation " + modifier.reference.id);

            for (int i = 0; i < list.Count; i++)
            {
                var bm = list[i];

                if (!modifier.constant)
                {
                    var animation = new RTAnimation("Apply Object Animation " + modifier.reference.id);
                    animation.animationHandlers = new List<AnimationHandlerBase>
                        {
                            new AnimationHandler<float>(new List<IKeyframe<float>>
                            {
                                new FloatKeyframe(0f, 0f, Ease.Linear),
                                new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                            }, x => ModifiersHelper.ApplyAnimationTo(bm, modifier.reference, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
                        };
                    animation.onComplete = () =>
                    {
                        AnimationManager.inst.Remove(animation.id);
                        animation = null;
                        modifier.Result = null;
                    };
                    AnimationManager.inst.Play(animation);
                    continue;
                }

                ModifiersHelper.ApplyAnimationTo(bm, modifier.reference, useVisual, time, RTLevel.Current.CurrentTime, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
            }
        }
        
        public static void applyAnimationMath(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(0, variables), out BeatmapObject from))
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(10, variables));

            if (!modifier.HasResult())
                modifier.Result = RTLevel.Current?.CurrentTime ?? 0f;
            var time = modifier.GetResult<float>();

            var numberVariables = modifier.reference.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);
            var functions = modifier.reference.GetObjectFunctions();

            var animatePos = modifier.GetBool(1, true, variables);
            var animateSca = modifier.GetBool(2, true, variables);
            var animateRot = modifier.GetBool(3, true, variables);
            var delayPos = RTMath.Parse(modifier.GetValue(4, variables), numberVariables, functions);
            var delaySca = RTMath.Parse(modifier.GetValue(5, variables), numberVariables, functions);
            var delayRot = RTMath.Parse(modifier.GetValue(6, variables), numberVariables, functions);
            var useVisual = modifier.GetBool(7, false, variables);
            var length = RTMath.Parse(modifier.GetValue(8, variables), numberVariables, functions);
            var speed = RTMath.Parse(modifier.GetValue(9, variables), numberVariables, functions);
            var timeOffset = RTMath.Parse(modifier.GetValue(11, variables), numberVariables, functions);

            if (!modifier.constant)
                AnimationManager.inst.RemoveName("Apply Object Animation " + modifier.reference.id);

            for (int i = 0; i < list.Count; i++)
            {
                var bm = list[i];

                if (!modifier.constant)
                {
                    var animation = new RTAnimation("Apply Object Animation " + modifier.reference.id);
                    animation.animationHandlers = new List<AnimationHandlerBase>
                        {
                            new AnimationHandler<float>(new List<IKeyframe<float>>
                            {
                                new FloatKeyframe(0f, 0f, Ease.Linear),
                                new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                            }, x => ModifiersHelper.ApplyAnimationTo(bm, from, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
                        };
                    animation.onComplete = () =>
                    {
                        AnimationManager.inst.Remove(animation.id);
                        animation = null;
                        modifier.Result = null;
                    };
                    AnimationManager.inst.Play(animation);
                    return;
                }

                ModifiersHelper.ApplyAnimationTo(bm, from, useVisual, time, timeOffset, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
            }
        }
        
        public static void applyAnimationFromMath(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!GameData.Current.TryFindObjectWithTag(modifier, modifier.value, out BeatmapObject bm))
                return;

            if (!modifier.HasResult())
                modifier.Result = RTLevel.Current?.CurrentTime ?? 0f;
            var time = modifier.GetResult<float>();

            var numberVariables = modifier.reference.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);
            var functions = modifier.reference.GetObjectFunctions();

            var animatePos = modifier.GetBool(1, true, variables);
            var animateSca = modifier.GetBool(2, true, variables);
            var animateRot = modifier.GetBool(3, true, variables);
            var delayPos = RTMath.Parse(modifier.GetValue(4, variables), numberVariables, functions);
            var delaySca = RTMath.Parse(modifier.GetValue(5, variables), numberVariables, functions);
            var delayRot = RTMath.Parse(modifier.GetValue(6, variables), numberVariables, functions);
            var useVisual = modifier.GetBool(7, false, variables);
            var length = RTMath.Parse(modifier.GetValue(8, variables), numberVariables, functions);
            var speed = RTMath.Parse(modifier.GetValue(9, variables), numberVariables, functions);
            var timeOffset = RTMath.Parse(modifier.GetValue(10, variables), numberVariables, functions);

            if (!modifier.constant)
            {
                AnimationManager.inst.RemoveName("Apply Object Animation " + modifier.reference.id);

                var animation = new RTAnimation("Apply Object Animation " + modifier.reference.id);
                animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, 0f, Ease.Linear),
                            new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                        }, x => ModifiersHelper.ApplyAnimationTo(modifier.reference, bm, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
                    };
                animation.onComplete = () =>
                {
                    AnimationManager.inst.Remove(animation.id);
                    animation = null;
                    modifier.Result = null;
                };
                AnimationManager.inst.Play(animation);
                return;
            }

            ModifiersHelper.ApplyAnimationTo(modifier.reference, bm, useVisual, time, timeOffset, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
        }
        
        public static void applyAnimationToMath(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(0, variables));

            if (!modifier.HasResult())
                modifier.Result = RTLevel.Current?.CurrentTime ?? 0f;
            var time = modifier.GetResult<float>();

            var numberVariables = modifier.reference.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);
            var functions = modifier.reference.GetObjectFunctions();

            var animatePos = modifier.GetBool(1, true, variables);
            var animateSca = modifier.GetBool(2, true, variables);
            var animateRot = modifier.GetBool(3, true, variables);
            var delayPos = RTMath.Parse(modifier.GetValue(4, variables), numberVariables, functions);
            var delaySca = RTMath.Parse(modifier.GetValue(5, variables), numberVariables, functions);
            var delayRot = RTMath.Parse(modifier.GetValue(6, variables), numberVariables, functions);
            var useVisual = modifier.GetBool(7, false, variables);
            var length = RTMath.Parse(modifier.GetValue(8, variables), numberVariables, functions);
            var speed = RTMath.Parse(modifier.GetValue(9, variables), numberVariables, functions);
            var timeOffset = RTMath.Parse(modifier.GetValue(10, variables), numberVariables, functions);

            if (!modifier.constant)
                AnimationManager.inst.RemoveName("Apply Object Animation " + modifier.reference.id);

            for (int i = 0; i < list.Count; i++)
            {
                var bm = list[i];

                if (!modifier.constant)
                {
                    var animation = new RTAnimation("Apply Object Animation " + modifier.reference.id);
                    animation.animationHandlers = new List<AnimationHandlerBase>
                        {
                            new AnimationHandler<float>(new List<IKeyframe<float>>
                            {
                                new FloatKeyframe(0f, 0f, Ease.Linear),
                                new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                            }, x => ModifiersHelper.ApplyAnimationTo(bm, modifier.reference, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
                        };
                    animation.onComplete = () =>
                    {
                        AnimationManager.inst.Remove(animation.id);
                        animation = null;
                        modifier.Result = null;
                    };
                    AnimationManager.inst.Play(animation);
                    continue;
                }

                ModifiersHelper.ApplyAnimationTo(bm, modifier.reference, useVisual, time, timeOffset, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
            }
        }

        #endregion

        #region Prefab

        public static void spawnPrefab<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.constant || modifier.HasResult())
                return;

            var prefab = ModifiersHelper.GetPrefab(modifier.GetInt(12, 0, variables), modifier.GetValue(0, variables));

            if (!prefab)
                return;

            var posX = modifier.GetFloat(1, 0f, variables);
            var posY = modifier.GetFloat(2, 0f, variables);
            var scaX = modifier.GetFloat(3, 0f, variables);
            var scaY = modifier.GetFloat(4, 0f, variables);
            var rot = modifier.GetFloat(5, 0f, variables);
            var repeatCount = modifier.GetInt(6, 0, variables);
            var repeatOffsetTime = modifier.GetFloat(7, 0f, variables);
            var speed = modifier.GetFloat(8, 0f, variables);

            var prefabObject = ModifiersHelper.AddPrefabObjectToLevel(prefab,
                modifier.GetBool(11, true, variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(10, 0f, variables) : modifier.GetFloat(10, 0f, variables),
                new Vector2(posX, posY),
                new Vector2(scaX, scaY),
                rot, repeatCount, repeatOffsetTime, speed);

            modifier.Result = prefabObject;
            GameData.Current.prefabObjects.Add(prefabObject);
            RTLevel.Current?.AddPrefabToLevel(prefabObject);
        }

        public static void spawnPrefabOffset(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.constant || modifier.HasResult())
                return;

            var prefab = ModifiersHelper.GetPrefab(modifier.GetInt(12, 0, variables), modifier.GetValue(0, variables));

            if (!prefab)
                return;

            var animationResult = modifier.reference.InterpolateChain();

            var posX = modifier.GetFloat(1, 0f, variables);
            var posY = modifier.GetFloat(2, 0f, variables);
            var scaX = modifier.GetFloat(3, 0f, variables);
            var scaY = modifier.GetFloat(4, 0f, variables);
            var rot = modifier.GetFloat(5, 0f, variables);
            var repeatCount = modifier.GetInt(6, 0, variables);
            var repeatOffsetTime = modifier.GetFloat(7, 0f, variables);
            var speed = modifier.GetFloat(8, 0f, variables);

            var prefabObject = ModifiersHelper.AddPrefabObjectToLevel(prefab,
                modifier.GetBool(11, true) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(10, 0f, variables) : modifier.GetFloat(10, 0f, variables),
                new Vector2(posX, posY) + (Vector2)animationResult.position,
                new Vector2(scaX, scaY) * animationResult.scale,
                rot + animationResult.rotation, repeatCount, repeatOffsetTime, speed);

            modifier.Result = prefabObject;
            GameData.Current.prefabObjects.Add(prefabObject);
            RTLevel.Current?.AddPrefabToLevel(prefabObject);
        }
        
        public static void spawnPrefabOffsetOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.constant || modifier.HasResult())
                return;

            var prefab = ModifiersHelper.GetPrefab(modifier.GetInt(13, 0, variables), modifier.GetValue(0, variables));

            if (!prefab)
                return;

            if (!GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(10, variables), out BeatmapObject beatmapObject))
                return;

            var animationResult = beatmapObject.InterpolateChain();

            var posX = modifier.GetFloat(1, 0f, variables);
            var posY = modifier.GetFloat(2, 0f, variables);
            var scaX = modifier.GetFloat(3, 0f, variables);
            var scaY = modifier.GetFloat(4, 0f, variables);
            var rot = modifier.GetFloat(5, 0f, variables);
            var repeatCount = modifier.GetInt(6, 0, variables);
            var repeatOffsetTime = modifier.GetFloat(7, 0f, variables);
            var speed = modifier.GetFloat(8, 0f, variables);

            var prefabObject = ModifiersHelper.AddPrefabObjectToLevel(prefab,
                modifier.GetBool(12, true, variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(11, 0f, variables) : modifier.GetFloat(11, 0f, variables),
                new Vector2(posX, posY) + (Vector2)animationResult.position,
                new Vector2(scaX, scaY) * animationResult.scale,
                rot + animationResult.rotation, repeatCount, repeatOffsetTime, speed);

            modifier.Result = prefabObject;
            GameData.Current.prefabObjects.Add(prefabObject);
            RTLevel.Current?.AddPrefabToLevel(prefabObject);
        }
        
        public static void spawnMultiPrefab<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.constant)
                return;

            var prefab = ModifiersHelper.GetPrefab(modifier.GetInt(11, 0, variables), modifier.GetValue(0, variables));

            if (!prefab)
                return;

            var posX = modifier.GetFloat(1, 0f, variables);
            var posY = modifier.GetFloat(2, 0f, variables);
            var scaX = modifier.GetFloat(3, 0f, variables);
            var scaY = modifier.GetFloat(4, 0f, variables);
            var rot = modifier.GetFloat(5, 0f, variables);
            var repeatCount = modifier.GetInt(6, 0, variables);
            var repeatOffsetTime = modifier.GetFloat(7, 0f, variables);
            var speed = modifier.GetFloat(8, 0f, variables);

            if (!modifier.HasResult())
                modifier.Result = new List<PrefabObject>();

            var list = modifier.GetResult<List<PrefabObject>>();
            var prefabObject = ModifiersHelper.AddPrefabObjectToLevel(prefab,
                modifier.GetBool(10, true, variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(9, 0f, variables) : modifier.GetFloat(9, 0f, variables),
                new Vector2(posX, posY),
                new Vector2(scaX, scaY),
                rot, repeatCount, repeatOffsetTime, speed);

            list.Add(prefabObject);
            modifier.Result = list;

            GameData.Current.prefabObjects.Add(prefabObject);
            RTLevel.Current?.AddPrefabToLevel(prefabObject);
        }
        
        public static void spawnMultiPrefabOffset(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.constant)
                return;

            var prefab = ModifiersHelper.GetPrefab(modifier.GetInt(11, 0, variables), modifier.GetValue(0, variables));

            if (!prefab)
                return;

            var animationResult = modifier.reference.InterpolateChain();

            var posX = modifier.GetFloat(1, 0f, variables);
            var posY = modifier.GetFloat(2, 0f, variables);
            var scaX = modifier.GetFloat(3, 0f, variables);
            var scaY = modifier.GetFloat(4, 0f, variables);
            var rot = modifier.GetFloat(5, 0f, variables);
            var repeatCount = modifier.GetInt(6, 0, variables);
            var repeatOffsetTime = modifier.GetFloat(7, 0f, variables);
            var speed = modifier.GetFloat(8, 0f, variables);

            if (!modifier.HasResult())
                modifier.Result = new List<PrefabObject>();

            var list = modifier.GetResult<List<PrefabObject>>();
            var prefabObject = ModifiersHelper.AddPrefabObjectToLevel(prefab,
                modifier.GetBool(10, true, variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(9, 0f, variables) : modifier.GetFloat(9, 0f, variables),
                new Vector2(posX, posY) + (Vector2)animationResult.position,
                new Vector2(scaX, scaY) * animationResult.scale,
                rot + animationResult.rotation, repeatCount, repeatOffsetTime, speed);

            list.Add(prefabObject);
            modifier.Result = list;

            GameData.Current.prefabObjects.Add(prefabObject);
            RTLevel.Current?.AddPrefabToLevel(prefabObject);
        }
        
        public static void spawnMultiPrefabOffsetOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.constant)
                return;

            var prefab = ModifiersHelper.GetPrefab(modifier.GetInt(12, 0, variables), modifier.GetValue(0, variables));

            if (!prefab)
                return;

            if (!GameData.Current.TryFindObjectWithTag(modifier, modifier.GetValue(9, variables), out BeatmapObject beatmapObject))
                return;

            var animationResult = beatmapObject.InterpolateChain();

            var posX = modifier.GetFloat(1, 0f, variables);
            var posY = modifier.GetFloat(2, 0f, variables);
            var scaX = modifier.GetFloat(3, 0f, variables);
            var scaY = modifier.GetFloat(4, 0f, variables);
            var rot = modifier.GetFloat(5, 0f, variables);
            var repeatCount = modifier.GetInt(6, 0, variables);
            var repeatOffsetTime = modifier.GetFloat(7, 0f, variables);
            var speed = modifier.GetFloat(8, 0f, variables);

            if (!modifier.HasResult())
                modifier.Result = new List<PrefabObject>();

            var list = modifier.GetResult<List<PrefabObject>>();
            var prefabObject = ModifiersHelper.AddPrefabObjectToLevel(prefab,
                modifier.GetBool(11, true, variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(10, 0f, variables) : modifier.GetFloat(10, 0f, variables),
                new Vector2(posX, posY) + (Vector2)animationResult.position,
                new Vector2(scaX, scaY) * animationResult.scale,
                rot + animationResult.rotation, repeatCount, repeatOffsetTime, speed);

            list.Add(prefabObject);
            modifier.Result = list;

            GameData.Current.prefabObjects.Add(prefabObject);
            RTLevel.Current?.AddPrefabToLevel(prefabObject);
        }
        
        public static void clearSpawnedPrefabs<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(0, variables));

            for (int i = 0; i < list.Count; i++)
            {
                var beatmapObject = list[i];

                for (int j = 0; j < beatmapObject.modifiers.Count; j++)
                {
                    var otherModifier = beatmapObject.modifiers[j];

                    if (otherModifier.TryGetResult(out PrefabObject prefabObjectResult))
                    {
                        RTLevel.Current?.UpdatePrefab(prefabObjectResult, false);

                        GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObjectResult.id);

                        otherModifier.Result = null;
                        continue;
                    }

                    if (!otherModifier.TryGetResult(out List<PrefabObject> result))
                        continue;

                    for (int k = 0; k < result.Count; k++)
                    {
                        var prefabObject = result[k];

                        RTLevel.Current?.UpdatePrefab(prefabObject, false);
                        GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);
                    }

                    result.Clear();
                    otherModifier.Result = null;
                }
            }
        }

        #endregion

        #region Ranking

        public static void saveLevelRank<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor || modifier.constant || !LevelManager.CurrentLevel)
                return;

            LevelManager.UpdateCurrentLevelProgress();
        }
        
        public static void clearHits<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            RTBeatmap.Current.hits.Clear();
        }
        
        public static void addHit<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var vector = Vector3.zero;
            if (modifier.reference is BeatmapObject beatmapObject)
            {
                if (modifier.GetBool(0, true, variables))
                    vector = beatmapObject.GetFullPosition();
                else
                {
                    var player = PlayerManager.GetClosestPlayer(beatmapObject.GetFullPosition());
                    if (player && player.Player)
                        vector = player.Player.rb.position;
                }
            }

            var timeValue = modifier.GetValue(1, variables);
            float time = AudioManager.inst.CurrentAudioSource.time;
            if (!string.IsNullOrEmpty(timeValue) && modifier.reference is IEvaluatable evaluatable)
            {
                var numberVariables = evaluatable.GetObjectVariables();
                ModifiersHelper.SetVariables(variables, numberVariables);

                time = RTMath.Parse(timeValue, numberVariables, evaluatable.GetObjectFunctions());
            }

            RTBeatmap.Current.hits.Add(new PlayerDataPoint(vector, time));
        }
        
        public static void subHit<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (!RTBeatmap.Current.hits.IsEmpty())
                RTBeatmap.Current.hits.RemoveAt(RTBeatmap.Current.hits.Count - 1);
        }
        
        public static void clearDeaths<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            RTBeatmap.Current.deaths.Clear();
        }
        
        public static void addDeath<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var vector = Vector3.zero;
            if (modifier.reference is BeatmapObject beatmapObject)
            {
                if (modifier.GetBool(0, true, variables))
                    vector = beatmapObject.GetFullPosition();
                else
                {
                    var player = PlayerManager.GetClosestPlayer(beatmapObject.GetFullPosition());
                    if (player && player.Player)
                        vector = player.Player.rb.position;
                }
            }

            var timeValue = modifier.GetValue(1, variables);
            float time = AudioManager.inst.CurrentAudioSource.time;
            if (!string.IsNullOrEmpty(timeValue) && modifier.reference is IEvaluatable evaluatable)
            {
                var numberVariables = evaluatable.GetObjectVariables();
                ModifiersHelper.SetVariables(variables, numberVariables);

                time = RTMath.Parse(timeValue, numberVariables, evaluatable.GetObjectFunctions());
            }

            RTBeatmap.Current.deaths.Add(new PlayerDataPoint(vector, time));
        }
        
        public static void subDeath<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (!RTBeatmap.Current.deaths.IsEmpty())
                RTBeatmap.Current.deaths.RemoveAt(RTBeatmap.Current.deaths.Count - 1);
        }

        #endregion

        #region Updates

        public static void updateObjects<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.constant)
                CoroutineHelper.StartCoroutine(RTLevel.IReinit());
        }
        
        public static void updateObject(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(0));

            if (modifier.constant || list.IsEmpty())
                return;

            foreach (var bm in list)
                RTLevel.Current?.UpdateObject(bm, recalculate: false);
            RTLevel.Current?.RecalculateObjectStates();
        }
        
        public static void setParent(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.constant)
                return;

            var group = modifier.GetValue(0, variables);
            if (group == string.Empty)
                ModifiersHelper.SetParent(modifier.reference, string.Empty);
            else if (GameData.Current.TryFindObjectWithTag(modifier, group, out BeatmapObject beatmapObject) && modifier.reference.CanParent(beatmapObject))
                ModifiersHelper.SetParent(modifier.reference, beatmapObject.id);
            else
                CoreHelper.LogError($"CANNOT PARENT OBJECT!\nName: {modifier.reference.name}\nID: {modifier.reference.id}");
        }
        
        public static void setParentOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.constant)
                return;

            var reference = modifier.reference;
            var group = modifier.GetValue(2, variables);

            if (!string.IsNullOrEmpty(group) && GameData.Current.TryFindObjectWithTag(modifier, group, out BeatmapObject beatmapObject))
                reference = beatmapObject;

            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(0, variables));

            var isEmpty = modifier.GetBool(1, false, variables);

            bool failed = false;
            list.ForLoop(beatmapObject =>
            {
                if (isEmpty)
                    ModifiersHelper.SetParent(beatmapObject, string.Empty);
                else if (beatmapObject.CanParent(reference))
                    ModifiersHelper.SetParent(beatmapObject, reference.id);
                else
                    failed = true;
            });

            if (failed)
                CoreHelper.LogError($"CANNOT PARENT OBJECT!\nName: {modifier.reference.name}\nID: {modifier.reference.id}");
        }
        
        public static void detachParent(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (!modifier.constant && modifier.reference)
                modifier.reference.detatched = modifier.GetBool(0, true, variables);
        }
        
        public static void detachParentOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.constant)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1));
            var detach = modifier.GetBool(0, true);

            for (int i = 0; i < list.Count; i++)
            {
                var beatmapObject = list[i];
                beatmapObject.detatched = detach;
            }
        }

        #endregion

        #region Physics

        public static void setCollision(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.collider)
                runtimeObject.visualObject.colliderEnabled = modifier.GetBool(0, false, variables);
        }

        public static void setCollisionOther(Modifier<BeatmapObject> modifier, Dictionary<string, string> variables)
        {
            var colliderEnabled = modifier.GetBool(0, false, variables);
            var list = GameData.Current.FindObjectsWithTag(modifier, modifier.GetValue(1, variables));

            foreach (var beatmapObject in list)
            {
                if (beatmapObject.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.collider)
                    runtimeObject.visualObject.colliderEnabled = colliderEnabled;
            }
        }

        #endregion

        #region Checkpoints

        public static void createCheckpoint<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            // if active checpoints matches the stored checkpoint, do not create a new checkpoint.
            if (modifier.TryGetResult(out Checkpoint prevCheckpoint) && prevCheckpoint.id == RTBeatmap.Current.ActiveCheckpoint.id)
                return;

            var checkpoint = new Checkpoint();
            checkpoint.time = modifier.GetBool(1, true, variables) ? RTLevel.FixedTime + modifier.GetFloat(0, 0f, variables) : modifier.GetFloat(0, 0f, variables);
            checkpoint.pos = new Vector2(modifier.GetFloat(2, 0f, variables), modifier.GetFloat(3, 0f, variables));
            checkpoint.heal = modifier.GetBool(4, false, variables);
            checkpoint.respawn = modifier.GetBool(5, true, variables);
            checkpoint.reverse = modifier.GetBool(6, true, variables);
            checkpoint.setTime = modifier.GetBool(7, true, variables);
            checkpoint.spawnType = (Checkpoint.SpawnPositionType)modifier.GetInt(8, 0, variables);
            for (int i = 9; i < modifier.commands.Count; i += 2)
                checkpoint.positions.Add(new Vector2(modifier.GetFloat(i, 0f, variables), modifier.GetFloat(i + 1, 0f, variables)));

            RTBeatmap.Current.SetCheckpoint(checkpoint);
            modifier.Result = checkpoint;
        }

        public static void resetCheckpoint<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            RTBeatmap.Current.ResetCheckpoint(modifier.GetBool(0, false, variables));
        }

        #endregion

        #region Interfaces

        public static void loadInterface<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (CoreHelper.IsEditing) // don't want interfaces to load in editor
            {
                EditorManager.inst.DisplayNotification($"Cannot load interface in the editor!", 1f, EditorManager.NotificationType.Warning);
                return;
            }

            var value = modifier.GetValue(0, variables);
            var path = RTFile.CombinePaths(RTFile.BasePath, value + FileFormat.LSI.Dot());

            if (!RTFile.FileExists(path))
            {
                CoreHelper.LogError($"Interface with file name: \"{value}\" does not exist.");
                return;
            }

            InterfaceManager.inst.ParseInterface(path);

            InterfaceManager.inst.MainDirectory = RTFile.BasePath;

            RTBeatmap.Current.Pause();
            ArcadeHelper.endedLevel = false;
        }

        public static void pauseLevel<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor)
            {
                EditorManager.inst.DisplayNotification("Cannot pause in the editor. This modifier only works in the Arcade.", 3f, EditorManager.NotificationType.Warning);
                return;
            }

            PauseMenu.Pause();
        }

        public static void quitToMenu<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor && !EditorManager.inst.isEditing && EditorConfig.Instance.ModifiersCanLoadLevels.Value)
            {
                string str = RTFile.BasePath;
                if (EditorConfig.Instance.ModifiersSavesBackup.Value)
                {
                    GameData.Current.SaveData(RTFile.CombinePaths(str, $"level-modifier-backup{FileFormat.LSB.Dot()}"), () =>
                    {
                        EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(RTFile.RemoveEndSlash(str))}", 2f, EditorManager.NotificationType.Success);
                    });
                }

                EditorManager.inst.QuitToMenu();
            }

            if (!CoreHelper.InEditor)
                ArcadeHelper.QuitToMainMenu();
        }

        public static void quitToArcade<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor && !EditorManager.inst.isEditing && EditorConfig.Instance.ModifiersCanLoadLevels.Value)
            {
                string str = RTFile.BasePath;
                if (EditorConfig.Instance.ModifiersSavesBackup.Value)
                {
                    GameData.Current.SaveData(RTFile.CombinePaths(str, $"level-modifier-backup{FileFormat.LSB.Dot()}"), () =>
                    {
                        EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(RTFile.RemoveEndSlash(str))}", 2f, EditorManager.NotificationType.Success);
                    });
                }

                GameManager.inst.QuitToArcade();

                return;
            }

            if (!CoreHelper.InEditor)
                ArcadeHelper.QuitToArcade();
        }

        #endregion

        #region Misc

        public static void setBGActive<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            var active = modifier.GetBool(0, false, variables);
            var tag = modifier.GetValue(1, variables);
            var list = GameData.Current.backgroundObjects.FindAll(x => x.tags.Contains(tag));
            if (!list.IsEmpty())
                for (int i = 0; i < list.Count; i++)
                    list[i].Enabled = active;
        }

        public static void signalModifier<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(1, variables));
            var delay = modifier.GetFloat(0, 0f, variables);

            foreach (var bm in list)
                CoroutineHelper.StartCoroutine(ModifiersHelper.ActivateModifier(bm, delay));
        }
        
        public static void activateModifier<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (modifier.reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier.prefabInstanceOnly, modifier.groupAlive, prefabable, modifier.GetValue(0, variables));

            var doMultiple = modifier.GetBool(1, true, variables);
            var index = modifier.GetInt(2, -1, variables);

            // 3 is modifier names
            var modifierNames = new List<string>();
            for (int i = 3; i < modifier.commands.Count; i++)
                modifierNames.Add(modifier.GetValue(i, variables));

            for (int i = 0; i < list.Count; i++)
            {
                if (doMultiple)
                {
                    var modifiers = list[i].modifiers.FindAll(x => x.type == ModifierBase.Type.Action && modifierNames.Contains(x.Name));

                    for (int j = 0; j < modifiers.Count; j++)
                    {
                        var otherModifier = modifiers[i];
                        otherModifier.Action?.Invoke(otherModifier, variables);
                    }
                    continue;
                }

                if (index >= 0 && index < list[i].modifiers.Count)
                {
                    var otherModifier = list[i].modifiers[index];
                    otherModifier.Action?.Invoke(otherModifier, variables);
                }
            }
        }
        
        public static void editorNotify<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor)
                EditorManager.inst.DisplayNotification(
                    /*text: */ modifier.GetValue(0, variables),
                    /*time: */ modifier.GetFloat(1, 0.5f, variables),
                    /*type: */ (EditorManager.NotificationType)modifier.GetInt(2, 0, variables));
        }
        
        public static void setWindowTitle<T>(Modifier<T> modifier, Dictionary<string, string> variables) => WindowController.SetTitle(modifier.GetValue(0, variables));

        public static void setDiscordStatus<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            string[] discordSubIcons = new string[]
            {
                "arcade",
                "editor",
                "play",
                "menu",
            };

            string[] discordIcons = new string[]
            {
                "pa_logo_white",
                "pa_logo_black",
            };

            if (int.TryParse(modifier.commands[2], out int discordSubIcon) && int.TryParse(modifier.commands[3], out int discordIcon))
                CoreHelper.UpdateDiscordStatus(
                    string.Format(modifier.value, MetaData.Current.song.title, $"{(!CoreHelper.InEditor ? "Game" : "Editor")}", $"{(!CoreHelper.InEditor ? "Level" : "Editing")}", $"{(!CoreHelper.InEditor ? "Arcade" : "Editor")}"),
                    string.Format(modifier.commands[1], MetaData.Current.song.title, $"{(!CoreHelper.InEditor ? "Game" : "Editor")}", $"{(!CoreHelper.InEditor ? "Level" : "Editing")}", $"{(!CoreHelper.InEditor ? "Arcade" : "Editor")}"),
                    discordSubIcons[Mathf.Clamp(discordSubIcon, 0, discordSubIcons.Length - 1)], discordIcons[Mathf.Clamp(discordIcon, 0, discordIcons.Length - 1)]);
        }

        #endregion

        #region DEVONLY

        public static void loadSceneDEVONLY<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (CoreHelper.InStory)
                SceneManager.inst.LoadScene(modifier.GetValue(0, variables), modifier.commands.Count > 1 && modifier.GetBool(1, true, variables));
        }
        
        public static void loadStoryLevelDEVONLY<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (CoreHelper.InStory)
                Story.StoryManager.inst.Play(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables), modifier.GetInt(4, 0, variables), modifier.GetBool(0, false, variables), modifier.GetBool(3, false, variables));
        }
        
        public static void storySaveBoolDEVONLY<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (CoreHelper.InStory)
                Story.StoryManager.inst.CurrentSave.SaveBool(modifier.GetValue(0, variables), modifier.GetBool(1, false, variables));
        }

        public static void storySaveIntDEVONLY<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (CoreHelper.InStory)
                Story.StoryManager.inst.CurrentSave.SaveInt(modifier.GetValue(0, variables), modifier.GetInt(1, 0, variables));
        }

        public static void storySaveFloatDEVONLY<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (CoreHelper.InStory)
                Story.StoryManager.inst.CurrentSave.SaveFloat(modifier.GetValue(0, variables), modifier.GetFloat(1, 0f, variables));
        }

        public static void storySaveStringDEVONLY<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (CoreHelper.InStory)
                Story.StoryManager.inst.CurrentSave.SaveString(modifier.GetValue(0, variables), modifier.GetValue(1, variables));
        }

        public static void storySaveIntVariableDEVONLY<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (CoreHelper.InStory && modifier.reference is IModifyable<T> modifyable)
                Story.StoryManager.inst.CurrentSave.SaveInt(modifier.GetValue(0, variables), modifyable.IntVariable);
        }

        public static void getStorySaveBoolDEVONLY<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = !CoreHelper.InStory ? modifier.GetBool(2, false, variables).ToString() : Story.StoryManager.inst.CurrentSave.LoadBool(modifier.GetValue(1, variables), modifier.GetBool(2, false, variables)).ToString();
        }
        
        public static void getStorySaveIntDEVONLY<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = !CoreHelper.InStory ? modifier.GetInt(2, 0, variables).ToString() : Story.StoryManager.inst.CurrentSave.LoadInt(modifier.GetValue(1, variables), modifier.GetInt(2, 0, variables)).ToString();
        }
        
        public static void getStorySaveFloatDEVONLY<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = !CoreHelper.InStory ? modifier.GetFloat(2, 0f, variables).ToString() : Story.StoryManager.inst.CurrentSave.LoadFloat(modifier.GetValue(1, variables), modifier.GetFloat(2, 0f, variables)).ToString();
        }
        
        public static void getStorySaveStringDEVONLY<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = !CoreHelper.InStory ? modifier.GetValue(2, variables) : Story.StoryManager.inst.CurrentSave.LoadString(modifier.GetValue(1, variables), modifier.GetValue(2, variables)).ToString();
        }

        public static void exampleEnableDEVONLY<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (Companion.Entity.Example.Current && Companion.Entity.Example.Current.model)
                Companion.Entity.Example.Current.model.SetActive(modifier.GetBool(0, false, variables));
        }
        
        public static void exampleSayDEVONLY<T>(Modifier<T> modifier, Dictionary<string, string> variables)
        {
            if (Companion.Entity.Example.Current && Companion.Entity.Example.Current.chatBubble)
                Companion.Entity.Example.Current.chatBubble.Say(modifier.GetValue(0, variables));
        }

        #endregion

        public static class PlayerActions
        {
            public static void setCustomActive(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                var id = modifier.GetValue(1, variables);
                if (modifier.reference.Player && modifier.reference.Player.customObjects.TryFind(x => x.id == id, out RTPlayer.CustomObject customObject))
                    customObject.active = modifier.GetBool(0, false, variables);
            }

            public static void kill(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                modifier.reference.Health = 0;
            }

            public static void hit(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                if (modifier.reference.Player)
                    modifier.reference.Player.Hit();
            }

            public static void boost(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                if (modifier.reference.Player)
                    modifier.reference.Player.Boost();
            }

            public static void shoot(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                if (modifier.reference.Player)
                    modifier.reference.Player.Shoot();
            }

            public static void pulse(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                if (modifier.reference.Player)
                    modifier.reference.Player.Pulse();
            }

            public static void jump(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                if (modifier.reference.Player)
                    modifier.reference.Player.Jump();
            }

            public static void signalModifier(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                var list = GameData.Current.FindObjectsWithTag(modifier.GetValue(1, variables));
                var delay = modifier.GetFloat(0, 0f, variables);

                foreach (var bm in list)
                    CoroutineHelper.StartCoroutine(ModifiersHelper.ActivateModifier(bm, delay));
            }

            public static void playAnimation(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                var id = modifier.GetValue(0, variables);
                var referenceID = modifier.GetValue(1, variables);
                if (modifier.reference.Player && modifier.reference.Player.customObjects.TryFind(x => x.id == id, out RTPlayer.CustomObject customObject) && customObject.reference && customObject.reference.animations.TryFind(x => x.ReferenceID == referenceID, out PAAnimation animation))
                {
                    var runtimeAnimation = new RTAnimation("Custom Animation");
                    modifier.reference.Player.ApplyAnimation(runtimeAnimation, animation, customObject);
                    modifier.reference.Player.animationController.Play(runtimeAnimation);
                }
            }

            public static void setIdleAnimation(Modifier<CustomPlayer> modifier, Dictionary<string, string> variables)
            {
                var id = modifier.GetValue(0, variables);
                var referenceID = modifier.GetValue(1, variables);
                if (modifier.reference.Player && modifier.reference.Player.customObjects.TryFind(x => x.id == id, out RTPlayer.CustomObject customObject) && customObject.reference && customObject.reference.animations.TryFind(x => x.ReferenceID == referenceID, out PAAnimation animation))
                    customObject.currentIdleAnimation = animation.ReferenceID;
            }
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles