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
using BetterLegacy.Editor.Data.Elements;
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

        public static void setPitch(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTLevel.Current.eventEngine)
                RTLevel.Current.eventEngine.pitchOffset = modifier.GetFloat(0, 0f, variables);
        }

        public static void addPitch(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTLevel.Current.eventEngine)
                RTLevel.Current.eventEngine.pitchOffset += modifier.GetFloat(0, 0f, variables);
        }

        public static void setPitchMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IEvaluatable evaluatable)
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

        public static void addPitchMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IEvaluatable evaluatable)
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

        public static void animatePitch(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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

        public static void setMusicTime(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => AudioManager.inst.SetMusicTime(modifier.GetFloat(0, 0f, variables));

        public static void setMusicTimeMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IEvaluatable evaluatable)
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
        
        public static void setMusicTimeStartTime(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is ILifetime<AutoKillType> lifeTime)
                AudioManager.inst.SetMusicTime(lifeTime.StartTime);
        }
        
        public static void setMusicTimeAutokill(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is ILifetime<AutoKillType> lifeTime)
                AudioManager.inst.SetMusicTime(lifeTime.StartTime + lifeTime.SpawnDuration);
        }

        public static void setMusicPlaying(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => SoundManager.inst.SetPlaying(modifier.GetBool(0, false, variables));

        public static void playSound(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var path = modifier.GetValue(0, variables);
            var global = modifier.GetBool(1, false, variables);
            var pitch = modifier.GetFloat(2, 1f, variables);
            var vol = modifier.GetFloat(3, 1f, variables);
            var loop = modifier.GetBool(4, false, variables);
            var panStereo = modifier.GetFloat(5, 0f, variables);

            var id = reference is PAObjectBase obj ? obj.id : reference is RTPlayer.RTPlayerObject playerObject ? playerObject.id : string.Empty;
            if (string.IsNullOrEmpty(id))
                loop = false;

            if (GameData.Current && GameData.Current.assets.sounds.TryFind(x => x.name == path, out SoundAsset soundAsset) && soundAsset.audio)
            {
                ModifiersHelper.PlaySound(id, soundAsset.audio, pitch, vol, loop, panStereo);
                return;
            }

            ModifiersHelper.GetSoundPath(id, path, global, pitch, vol, loop, panStereo);
        }

        public static void playSoundOnline(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var url = modifier.GetValue(0, variables);
            var pitch = modifier.GetFloat(1, 1f, variables);
            var vol = modifier.GetFloat(2, 1f, variables);
            var loop = modifier.GetBool(3, false, variables);
            var panStereo = modifier.GetFloat(4, 0f, variables);

            var id = reference is PAObjectBase obj ? obj.id : reference is RTPlayer.RTPlayerObject playerObject ? playerObject.id : string.Empty;
            if (string.IsNullOrEmpty(id))
                loop = false;

            if (!string.IsNullOrEmpty(url))
                ModifiersHelper.DownloadSoundAndPlay(id, url, pitch, vol, loop, panStereo);
        }

        public static void playDefaultSound(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var pitch = modifier.GetFloat(1, 1f, variables);
            var vol = modifier.GetFloat(2, 1f, variables);
            var loop = modifier.GetBool(3, false, variables);
            var panStereo = modifier.GetFloat(4, 0f, variables);

            if (!AudioManager.inst.library.soundClips.TryGetValue(modifier.GetValue(0), out AudioClip[] audioClips))
                return;

            var clip = audioClips[UnityEngine.Random.Range(0, audioClips.Length)];
            var audioSource = Camera.main.gameObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.playOnAwake = true;
            audioSource.loop = loop;
            audioSource.pitch = pitch * AudioManager.inst.CurrentAudioSource.pitch;
            audioSource.volume = vol * AudioManager.inst.sfxVol;
            audioSource.panStereo = panStereo;
            audioSource.Play();

            float x = pitch * AudioManager.inst.CurrentAudioSource.pitch;
            if (x == 0f)
                x = 1f;
            if (x < 0f)
                x = -x;

            var id = reference is PAObjectBase obj ? obj.id : reference is RTPlayer.RTPlayerObject playerObject ? playerObject.id : string.Empty;
            if (string.IsNullOrEmpty(id))
                loop = false;

            if (!loop)
                CoroutineHelper.StartCoroutine(AudioManager.inst.DestroyWithDelay(audioSource, clip.length / x));
            else if (!ModifiersManager.audioSources.ContainsKey(id))
                ModifiersManager.audioSources.Add(id, audioSource);
        }

        public static void audioSource(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            if (modifier.TryGetResult(out AudioModifier audioModifier))
            {
                audioModifier.pitch = modifier.GetFloat(2, 1f, variables);
                audioModifier.volume = modifier.GetFloat(3, 1f, variables);
                audioModifier.loop = modifier.GetBool(4, true, variables);
                audioModifier.timeOffset = modifier.GetBool(6, true, variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(5, 0f, variables) : modifier.GetFloat(5, 0f, variables);
                audioModifier.lengthOffset = modifier.GetFloat(7, 0f, variables);
                audioModifier.playing = modifier.GetBool(8, true, variables);
                audioModifier.panStereo = modifier.GetFloat(9, 0f, variables);
                audioModifier.Tick();
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
                modifier.Result = runtimeObject.visualObject.gameObject.AddComponent<AudioModifier>();
                ((AudioModifier)modifier.Result).Init(LSAudio.CreateAudioClipUsingMP3File(fullPath), beatmapObject, modifier);
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

                if (!runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                    return;

                var audioModifier = runtimeObject.visualObject.gameObject.AddComponent<AudioModifier>();
                modifier.Result = audioModifier;
                audioModifier.Init(audioClip, beatmapObject, modifier);
                audioModifier.pitch = modifier.GetFloat(2, 1f, variables);
                audioModifier.volume = modifier.GetFloat(3, 1f, variables);
                audioModifier.loop = modifier.GetBool(4, true, variables);
                audioModifier.timeOffset = modifier.GetBool(6, true, variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(5, 0f, variables) : modifier.GetFloat(5, 0f, variables);
                audioModifier.lengthOffset = modifier.GetFloat(7, 0f, variables);
                audioModifier.playing = modifier.GetBool(8, true, variables);
                audioModifier.panStereo = modifier.GetFloat(9, 0f, variables);
                audioModifier.Tick();
            }));
        }

        #endregion

        #region Level

        public static void loadLevel(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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

        public static void loadLevelID(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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

            if (EditorLevelManager.inst.LevelPanels.TryFind(x => x.Item && x.Item.metadata is MetaData metaData && metaData.ID == modifier.value, out LevelPanel levelPanel))
            {
                if (!EditorConfig.Instance.ModifiersCanLoadLevels.Value)
                    return;

                var path = System.IO.Path.GetFileName(levelPanel.Path);

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

                    EditorLevelManager.inst.LoadLevel(levelPanel.Item);
                }, RTEditor.inst.HideWarningPopup);
            }
            else
                SoundManager.inst.PlaySound(DefaultSounds.Block);
        }

        public static void loadLevelInternal(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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

        public static void loadLevelPrevious(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor)
                return;

            LevelManager.Play(LevelManager.PreviousLevel);
        }

        public static void loadLevelHub(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor)
                return;

            LevelManager.Play(LevelManager.Hub);
        }

        public static void loadLevelInCollection(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(0, variables);
            if (!CoreHelper.InEditor && LevelManager.CurrentLevelCollection && LevelManager.CurrentLevelCollection.levels.TryFind(x => x.id == id, out Level level))
                LevelManager.Play(level);
        }

        public static void downloadLevel(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var levelInfo = new LevelInfo(modifier.GetValue(0, variables), modifier.GetValue(0, variables), modifier.GetValue(1, variables), modifier.GetValue(2, variables), modifier.GetValue(3, variables), modifier.GetValue(4, variables));

            LevelCollection.DownloadLevel(null, levelInfo, level =>
            {
                if (modifier.GetBool(5, true, variables))
                    LevelManager.Play(level);
            });
        }

        public static void endLevel(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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
        
        public static void setAudioTransition(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => LevelManager.songFadeTransition = modifier.GetFloat(0, 0.5f, variables);

        public static void setIntroFade(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => RTGameManager.doIntroFade = modifier.GetBool(0, true, variables);

        public static void setLevelEndFunc(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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

        public static void getCurrentLevelID(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (LevelManager.CurrentLevel)
                variables[modifier.GetValue(0)] = LevelManager.CurrentLevel.id;
            if (CoreHelper.InEditor && EditorLevelManager.inst.CurrentLevel)
                variables[modifier.GetValue(0)] = EditorLevelManager.inst.CurrentLevel.id;
        }

        public static void getCurrentLevelRank(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = LevelManager.GetLevelRank(RTBeatmap.Current.hits).Ordinal.ToString();
        }

        #endregion

        #region Component

        public static void blur(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty)
                return;

            var runtimeObject = beatmapObject.runtimeObject;

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
                renderer.material.SetFloat("_blurSizeXY", -(beatmapObject.Interpolate(3, 1) - 1f) * amount);
            else
                renderer.material.SetFloat("_blurSizeXY", amount);
        }
        
        public static void blurOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
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
        
        public static void blurVariable(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty)
                return;

            var runtimeObject = beatmapObject.runtimeObject;

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

            renderer.material.SetFloat("_blurSizeXY", beatmapObject.integerVariable * amount);
        }
        
        public static void blurVariableOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1));
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
        
        public static void blurColored(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty)
                return;

            var runtimeObject = beatmapObject.runtimeObject;

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
                renderer.material.SetFloat("_Size", -(beatmapObject.Interpolate(3, 1) - 1f) * amount);
            else
                renderer.material.SetFloat("_Size", amount);
        }
        
        public static void blurColoredOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
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
        
        public static void doubleSided(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (runtimeObject && runtimeObject.visualObject is SolidObject solidObject && solidObject.gameObject)
                solidObject.UpdateRendering((int)beatmapObject.gradientType, (int)beatmapObject.renderLayerType, true, beatmapObject.gradientScale, beatmapObject.gradientRotation);
        }
        
        public static void particleSystem(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
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
        
        public static void trailRenderer(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            var gameObject = runtimeObject.visualObject.gameObject;

            if (!beatmapObject.trailRenderer)
            {
                beatmapObject.trailRenderer = gameObject.GetOrAddComponent<TrailRenderer>();

                beatmapObject.trailRenderer.material = GameManager.inst.PlayerPrefabs[0].transform.GetChild(0).GetChild(0).GetComponent<TrailRenderer>().material;
                beatmapObject.trailRenderer.material.color = Color.white;
            }
            else
            {
                var tr = beatmapObject.trailRenderer;

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
        
        public static void trailRendererHex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            var gameObject = runtimeObject.visualObject.gameObject;

            if (!beatmapObject.trailRenderer)
            {
                beatmapObject.trailRenderer = gameObject.GetOrAddComponent<TrailRenderer>();

                beatmapObject.trailRenderer.material = GameManager.inst.PlayerPrefabs[0].transform.GetChild(0).GetChild(0).GetComponent<TrailRenderer>().material;
                beatmapObject.trailRenderer.material.color = Color.white;
            }
            else
            {
                var tr = beatmapObject.trailRenderer;

                tr.time = modifier.GetFloat(0, 1f, variables);
                tr.emitting = !(gameObject.transform.lossyScale.x < 0.001f && gameObject.transform.lossyScale.x > -0.001f || gameObject.transform.lossyScale.y < 0.001f && gameObject.transform.lossyScale.y > -0.001f) && gameObject.activeSelf && gameObject.activeInHierarchy;

                var t = gameObject.transform.lossyScale.magnitude * 0.576635f;
                tr.startWidth = modifier.GetFloat(1, 1f, variables) * t;
                tr.endWidth = modifier.GetFloat(2, 1f, variables) * t;

                tr.startColor = RTColors.HexToColor(modifier.GetValue(3, variables));
                tr.endColor = RTColors.HexToColor(modifier.GetValue(4, variables));
            }
        }

        public static void rigidbody(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            var gravity = modifier.GetFloat(1, 0f, variables);
            var collisionMode = modifier.GetInt(2, 0, variables);
            var drag = modifier.GetFloat(3, 0f, variables);
            var velocityX = modifier.GetFloat(4, 0f, variables);
            var velocityY = modifier.GetFloat(5, 0f, variables);
            var bodyType = modifier.GetInt(6, 0, variables);

            if (!beatmapObject.rigidbody)
                beatmapObject.rigidbody = runtimeObject.visualObject.gameObject.GetOrAddComponent<Rigidbody2D>();

            beatmapObject.rigidbody.gravityScale = gravity;
            beatmapObject.rigidbody.collisionDetectionMode = (CollisionDetectionMode2D)Mathf.Clamp(collisionMode, 0, 1);
            beatmapObject.rigidbody.drag = drag;

            beatmapObject.rigidbody.bodyType = (RigidbodyType2D)Mathf.Clamp(bodyType, 0, 2);

            beatmapObject.rigidbody.velocity += new Vector2(velocityX, velocityY);
        }
        
        public static void rigidbodyOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));
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

        public static void setRenderType(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is BeatmapObject beatmapObject && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject)
                beatmapObject.runtimeObject.visualObject.SetRenderType(modifier.GetInt(0, 0, variables));
        }

        public static void setRenderTypeOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));
            if (list.IsEmpty())
                return;

            var renderType = modifier.GetInt(1, 0, variables);
            foreach (var beatmapObject in list)
            {
                if (beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject)
                    beatmapObject.runtimeObject.visualObject.SetRenderType(renderType);
            }
        }

        #endregion

        #region Player

        public static void playerHit(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant || reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);
                player?.RuntimePlayer?.Hit(Mathf.Clamp(modifier.GetInt(0, 1, variables), 0, int.MaxValue));
            });
        }
        
        public static void playerHitIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant)
                return;

            var damage = Mathf.Clamp(modifier.GetInt(1, 1, variables), 0, int.MaxValue);
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player))
                player.RuntimePlayer?.Hit(damage);
        }
        
        public static void playerHitAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant)
                return;

            var damage = Mathf.Clamp(modifier.GetInt(0, 1, variables), 0, int.MaxValue);
            foreach (var player in PlayerManager.Players)
                player.RuntimePlayer?.Hit(damage);
        }
        
        public static void playerHeal(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant || reference is not BeatmapObject beatmapObject)
                return;

            var heal = Mathf.Clamp(modifier.GetInt(0, 1, variables), 0, int.MaxValue);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);
                player?.RuntimePlayer?.Heal(heal);
            });
        }
        
        public static void playerHealIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant)
                return;

            var health = Mathf.Clamp(modifier.GetInt(1, 1, variables), 0, int.MaxValue);
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) && player.RuntimePlayer)
                player.RuntimePlayer.Heal(health);
        }

        public static void playerHealAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant)
                return;

            var heal = Mathf.Clamp(modifier.GetInt(0, 1, variables), 0, int.MaxValue);
            bool healed = false;
            foreach (var player in PlayerManager.Players)
            {
                if (player.RuntimePlayer && player.RuntimePlayer.Heal(heal, false))
                    healed = true;
            }

            if (healed)
                SoundManager.inst.PlaySound(DefaultSounds.HealPlayer);
        }
        
        public static void playerKill(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant || reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.RuntimePlayer)
                    player.RuntimePlayer.Kill();
            });
        }
        
        public static void playerKillIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant)
                return;

            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) && player.RuntimePlayer)
                player.RuntimePlayer.Kill();
        }
        
        public static void playerKillAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTBeatmap.Current.Invincible || modifier.constant)
                return;

            foreach (var player in PlayerManager.Players)
                if (player.RuntimePlayer)
                    player.RuntimePlayer.Kill();
        }
        
        public static void playerRespawn(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var playerIndex = PlayerManager.GetClosestPlayerIndex(pos);

                if (playerIndex >= 0)
                    PlayerManager.RespawnPlayer(playerIndex);
            });
        }
        
        public static void playerRespawnIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!modifier.constant)
                PlayerManager.RespawnPlayer(modifier.GetInt(0, 0));
        }
        
        public static void playerRespawnAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!modifier.constant)
                PlayerManager.RespawnPlayers();
        }
        
        // todo: implement these
        public static void playerLockXAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {

        }
        
        public static void playerLockYAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {

        }

        public static void playerLockBoostAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.commands.Count > 3 && !string.IsNullOrEmpty(modifier.commands[1]) && bool.TryParse(modifier.GetValue(0, variables), out bool lockBoost))
                RTPlayer.LockBoost = lockBoost;
        }

        public static void playerMove(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
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

                var duration = modifier.GetFloat(3, 0f, variables);
                bool relative = modifier.GetBool(4, false, variables);
                if (!player || !player.RuntimePlayer)
                    return;

                var tf = player.RuntimePlayer.rb.transform;
                if (duration == 0f || modifier.constant)
                {
                    if (relative)
                        tf.localPosition += (Vector3)vector;
                    else
                        tf.localPosition = vector;
                }
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
        
        public static void playerMoveIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var vector = new Vector2(modifier.GetFloat(1, 0f, variables), modifier.GetFloat(2, 0f, variables));
            var duration = modifier.GetFloat(3, 0f, variables);

            string easing = modifier.GetValue(4, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var relative = modifier.GetBool(5, false, variables);

            if (!PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) || !player.RuntimePlayer)
                return;

            var tf = player.RuntimePlayer.rb.transform;
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

        public static void playerMoveAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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
            foreach (var player in PlayerManager.Players)
            {
                if (!player.RuntimePlayer)
                    continue;

                var tf = player.RuntimePlayer.rb.transform;
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
        
        public static void playerMoveX(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
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
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer)
                    return;

                var tf = player.RuntimePlayer.rb.transform;
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

        public static void playerMoveXIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var value = modifier.GetFloat(1, 0f, variables);
            var duration = modifier.GetFloat(2, 0f, variables);

            string easing = modifier.GetValue(3, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var relative = modifier.GetBool(4, false, variables);

            if (!PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) || !player.RuntimePlayer)
                return;

            var tf = player.RuntimePlayer.rb.transform;
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

        public static void playerMoveXAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var value = modifier.GetFloat(0, 0f, variables);
            var duration = modifier.GetFloat(1, 1f, variables);
            string easing = modifier.GetValue(2, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            bool relative = modifier.GetBool(3, false, variables);
            foreach (var player in PlayerManager.Players)
            {
                if (!player.RuntimePlayer)
                    continue;

                var tf = player.RuntimePlayer.rb.transform;
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
        
        public static void playerMoveY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
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
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer)
                    return;

                var tf = player.RuntimePlayer.rb.transform;
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

        public static void playerMoveYIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var value = modifier.GetFloat(1, 0f, variables);
            var duration = modifier.GetFloat(2, 0f, variables);

            string easing = modifier.GetValue(3, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var relative = modifier.GetBool(4, false, variables);

            if (!PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) || !player.RuntimePlayer)
                return;

            var tf = player.RuntimePlayer.rb.transform;
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

        public static void playerMoveYAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var value = modifier.GetFloat(0, 0f, variables);
            var duration = modifier.GetFloat(1, 1f, variables);
            string easing = modifier.GetValue(2, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            bool relative = modifier.GetBool(3, false, variables);
            foreach (var player in PlayerManager.Players)
            {
                if (!player.RuntimePlayer)
                    continue;

                var tf = player.RuntimePlayer.rb.transform;
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
        
        public static void playerRotate(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
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
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer)
                    return;

                var tf = player.RuntimePlayer.rb.transform;
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

        public static void playerRotateIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var value = modifier.GetFloat(1, 0f, variables);
            var duration = modifier.GetFloat(2, 0f, variables);

            string easing = modifier.GetValue(3, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var relative = modifier.GetBool(4, false, variables);

            if (!PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) || !player.RuntimePlayer)
                return;

            var tf = player.RuntimePlayer.rb.transform;
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

        public static void playerRotateAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var value = modifier.GetFloat(0, 0f, variables);
            string easing = modifier.GetValue(2, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;
            var duration = modifier.GetFloat(1, 1f, variables);

            bool relative = modifier.GetBool(3, false, variables);
            foreach (var player in PlayerManager.Players)
            {
                if (!player.RuntimePlayer || !player.RuntimePlayer.rb)
                    continue;

                var tf = player.RuntimePlayer.rb.transform;
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
        
        public static void playerMoveToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                player.RuntimePlayer.rb.position = new Vector3(pos.x, pos.y, 0f);
            });
        }

        public static void playerMoveIndexToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var index = modifier.GetInt(0, 0, variables);
            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                if (!PlayerManager.Players.TryGetAt(index, out PAPlayer player) || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                player.RuntimePlayer.rb.position = new Vector3(pos.x, pos.y, 0f);
            });
        }

        public static void playerMoveAllToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                var y = player.RuntimePlayer.rb.position.y;
                player.RuntimePlayer.rb.position = new Vector2(pos.x, y);
            });
        }
        
        public static void playerMoveXToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                var y = player.RuntimePlayer.rb.position.y;
                player.RuntimePlayer.rb.position = new Vector2(pos.x, y);
            });
        }

        public static void playerMoveXIndexToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var index = modifier.GetInt(0, 0, variables);
            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                if (!PlayerManager.Players.TryGetAt(index, out PAPlayer player) || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                var y = player.RuntimePlayer.rb.position.y;
                player.RuntimePlayer.rb.position = new Vector2(pos.x, y);
            });
        }

        public static void playerMoveXAllToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                foreach (var player in PlayerManager.Players)
                {
                    if (!player.RuntimePlayer || !player.RuntimePlayer.rb)
                        continue;

                    var y = player.RuntimePlayer.rb.position.y;
                    player.RuntimePlayer.rb.position = new Vector2(pos.x, y);
                }
            });
        }

        public static void playerMoveYToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                var x = player.RuntimePlayer.rb.position.x;
                player.RuntimePlayer.rb.position = new Vector2(x, pos.y);
            });
        }

        public static void playerMoveYIndexToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var index = modifier.GetInt(0, 0, variables);
            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                if (!PlayerManager.Players.TryGetAt(index, out PAPlayer player) || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                var x = player.RuntimePlayer.rb.position.x;
                player.RuntimePlayer.rb.position = new Vector2(x, pos.y);
            });
        }

        public static void playerMoveYAllToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                foreach (var player in PlayerManager.Players)
                {
                    if (!player.RuntimePlayer || !player.RuntimePlayer.rb)
                        continue;

                    var x = player.RuntimePlayer.rb.position.x;
                    player.RuntimePlayer.rb.position = new Vector2(x, pos.y);
                }
            });
        }

        public static void playerRotateToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                player.RuntimePlayer.rb.transform.SetLocalRotationEulerZ(beatmapObject.GetFullRotation(true).z);
            });
        }

        public static void playerRotateIndexToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var index = modifier.GetInt(0, 0, variables);
            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                if (!PlayerManager.Players.TryGetAt(index, out PAPlayer player) || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                player.RuntimePlayer.rb.transform.SetLocalRotationEulerZ(beatmapObject.GetFullRotation(true).z);
            });
        }

        public static void playerRotateAllToObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var rot = beatmapObject.GetFullRotation(true).z;

                foreach (var player in PlayerManager.Players)
                {
                    if (!player.RuntimePlayer || !player.RuntimePlayer.rb)
                        continue;

                    player.RuntimePlayer.rb.transform.SetLocalRotationEulerZ(rot);
                }
            });
        }
        
        public static void playerDrag(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var usePosition = modifier.GetBool(0, false, variables);
            var useScale = modifier.GetBool(1, false, variables);
            var useRotation = modifier.GetBool(2, false, variables);

            var prevPos = !usePosition ? Vector3.zero : beatmapObject.GetFullPosition();
            var prevSca = !useScale ? Vector3.zero : beatmapObject.GetFullScale();
            var prevRot = !useRotation ? Vector3.zero : beatmapObject.GetFullRotation(true);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();

                var player = PlayerManager.GetClosestPlayer(pos);
                if (!player || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                var rb = player.RuntimePlayer.rb;

                Vector2 distance = Vector2.zero;
                if (usePosition)
                    distance = pos - prevPos;
                if (useScale)
                {
                    var playerDistance = Vector3.Distance(pos, rb.position);

                    var sca = beatmapObject.GetFullScale();
                    distance += (Vector2)(sca - prevSca) * playerDistance;
                }
                if (useRotation)
                {
                    var rot = beatmapObject.GetFullRotation(true);
                    distance += (Vector2)(RTMath.Rotate(rb.position + (Vector2)pos, rot.z) - RTMath.Rotate(rb.position + (Vector2)pos, prevRot.z));
                }

                rb.position += distance;
            });
        }

        public static void playerBoost(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || reference is not BeatmapObject beatmapObject)
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
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer)
                    return;

                if (shouldBoostX)
                    player.RuntimePlayer.lastMoveHorizontal = x;
                if (shouldBoostY)
                    player.RuntimePlayer.lastMoveVertical = y;
                player.RuntimePlayer.Boost();
            });
        }
        
        public static void playerBoostIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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

            if (!modifier.constant && PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) && player.RuntimePlayer)
            {
                if (shouldBoostX)
                    player.RuntimePlayer.lastMoveHorizontal = x;
                if (shouldBoostY)
                    player.RuntimePlayer.lastMoveVertical = y;
                player.RuntimePlayer.Boost();
            }
        }
        
        public static void playerBoostAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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

            foreach (var player in PlayerManager.Players)
            {
                if (!player.RuntimePlayer)
                    continue;

                if (shouldBoostX)
                    player.RuntimePlayer.lastMoveHorizontal = x;
                if (shouldBoostY)
                    player.RuntimePlayer.lastMoveVertical = y;
                player.RuntimePlayer.Boost();
            }
        }
        
        public static void playerCancelBoost(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.RuntimePlayer && player.RuntimePlayer.CanCancelBoosting)
                    player.RuntimePlayer.StopBoosting();
            });
        }

        public static void playerCancelBoostIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) && player.RuntimePlayer && player.RuntimePlayer.CanCancelBoosting)
                player.RuntimePlayer.StopBoosting();
        }

        public static void playerCancelBoostAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            foreach (var player in PlayerManager.Players)
            {
                if (player && player.RuntimePlayer && player.RuntimePlayer.CanCancelBoosting)
                    player.RuntimePlayer.StopBoosting();
            }
        }

        public static void playerDisableBoost(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.RuntimePlayer)
                    player.RuntimePlayer.CanBoost = false;
            });
        }
        
        public static void playerDisableBoostIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) && player.RuntimePlayer)
                player.RuntimePlayer.CanBoost = false;
        }
        
        public static void playerDisableBoostAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            foreach (var player in PlayerManager.Players)
            {
                if (player.RuntimePlayer)
                    player.RuntimePlayer.CanBoost = false;
            }
        }
        
        public static void playerEnableBoost(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var enabled = modifier.GetBool(0, true, variables);
            
            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.RuntimePlayer)
                    player.RuntimePlayer.CanBoost = enabled;
            });
        }
        
        public static void playerEnableBoostIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(1, true, variables);

            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) && player.RuntimePlayer)
                player.RuntimePlayer.CanBoost = enabled;
        }
        
        public static void playerEnableBoostAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(0, true, variables);

            foreach (var player in PlayerManager.Players)
            {
                if (player.RuntimePlayer)
                    player.RuntimePlayer.CanBoost = enabled;
            }
        }
        
        public static void playerEnableMove(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var enabled = modifier.GetBool(0, true, variables);
            var rotate = modifier.GetBool(1, true, variables);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer)
                    return;

                player.RuntimePlayer.CanMove = enabled;
                player.RuntimePlayer.CanRotate = rotate;
            });
        }

        public static void playerEnableMoveIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(1, true, variables);
            var rotate = modifier.GetBool(2, true, variables);

            if (!PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, variables), out PAPlayer player) || !player.RuntimePlayer)
                return;

            player.RuntimePlayer.CanMove = enabled;
            player.RuntimePlayer.CanRotate = rotate;
        }

        public static void playerEnableMoveAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(0, true, variables);
            var rotate = modifier.GetBool(1, true, variables);

            foreach (var player in PlayerManager.Players)
            {
                if (!player.RuntimePlayer)
                    continue;

                player.RuntimePlayer.CanMove = enabled;
                player.RuntimePlayer.CanRotate = rotate;
            }
        }

        public static void playerSpeed(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => RTPlayer.SpeedMultiplier = modifier.GetFloat(0, 1f, variables);

        public static void playerVelocity(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var x = modifier.GetFloat(0, 0f, variables);
            var y = modifier.GetFloat(1, 0f, variables);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (player && player.RuntimePlayer)
                    player.RuntimePlayer.rb.velocity = new Vector2(x, y);
            });
        }
        
        public static void playerVelocityIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var index = modifier.GetInt(0, 0, variables);
            var x = modifier.GetFloat(1, 0f, variables);
            var y = modifier.GetFloat(2, 0f, variables);

            if (PlayerManager.Players.TryGetAt(index, out PAPlayer player) && player.RuntimePlayer && player.RuntimePlayer.rb)
                player.RuntimePlayer.rb.velocity = new Vector2(x, y);
        }

        public static void playerVelocityAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var x = modifier.GetFloat(0, 0f, variables);
            var y = modifier.GetFloat(1, 0f, variables);

            for (int i = 0; i < PlayerManager.Players.Count; i++)
            {
                var player = PlayerManager.Players[i];
                if (player.RuntimePlayer && player.RuntimePlayer.rb)
                    player.RuntimePlayer.rb.velocity = new Vector2(x, y);
            }
        }

        public static void playerVelocityX(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var x = modifier.GetFloat(0, 0f, variables);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer)
                    return;

                var velocity = player.RuntimePlayer.rb.velocity;
                velocity.x = x;
                player.RuntimePlayer.rb.velocity = velocity;
            });
        }

        public static void playerVelocityXIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var index = modifier.GetInt(0, 0, variables);
            var x = modifier.GetFloat(1, 0f, variables);

            if (!PlayerManager.Players.TryGetAt(index, out PAPlayer player) || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                return;

            var velocity = player.RuntimePlayer.rb.velocity;
            velocity.x = x;
            player.RuntimePlayer.rb.velocity = velocity;
        }

        public static void playerVelocityXAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var x = modifier.GetFloat(0, 0f, variables);

            for (int i = 0; i < PlayerManager.Players.Count; i++)
            {
                var player = PlayerManager.Players[i];
                if (!player.RuntimePlayer || !player.RuntimePlayer.rb)
                    continue;

                var velocity = player.RuntimePlayer.rb.velocity;
                velocity.x = x;
                player.RuntimePlayer.rb.velocity = velocity;
            }
        }

        public static void playerVelocityY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var y = modifier.GetFloat(0, 0f, variables);

            // queue post tick so the position of the object is accurate.
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var pos = beatmapObject.GetFullPosition();
                var player = PlayerManager.GetClosestPlayer(pos);

                if (!player || !player.RuntimePlayer)
                    return;

                var velocity = player.RuntimePlayer.rb.velocity;
                velocity.y = y;
                player.RuntimePlayer.rb.velocity = velocity;
            });
        }

        public static void playerVelocityYIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var index = modifier.GetInt(0, 0, variables);
            var y = modifier.GetFloat(1, 0f, variables);

            if (!PlayerManager.Players.TryGetAt(index, out PAPlayer player) || !player.RuntimePlayer || !player.RuntimePlayer.rb)
                return;

            var velocity = player.RuntimePlayer.rb.velocity;
            velocity.y = y;
            player.RuntimePlayer.rb.velocity = velocity;
        }

        public static void playerVelocityYAll(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var y = modifier.GetFloat(0, 0f, variables);

            for (int i = 0; i < PlayerManager.Players.Count; i++)
            {
                var player = PlayerManager.Players[i];
                if (!player.RuntimePlayer || !player.RuntimePlayer.rb)
                    continue;

                var velocity = player.RuntimePlayer.rb.velocity;
                velocity.y = y;
                player.RuntimePlayer.rb.velocity = velocity;
            }
        }
        
        public static void setPlayerModel(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant)
                return;

            var index = modifier.GetInt(1, 0, variables);

            if (!PlayersData.Current.playerModels.ContainsKey(modifier.GetValue(0, variables)))
                return;

            PlayersData.Current.SetPlayerModel(index, modifier.GetValue(0, variables));
            PlayerManager.AssignPlayerModels();

            if (!PlayerManager.Players.TryGetAt(index, out PAPlayer player) || !player.RuntimePlayer)
                return;

            player.UpdatePlayerModel();

            player.RuntimePlayer.playerNeedsUpdating = true;
            player.RuntimePlayer.UpdateModel();
        }
        
        public static void setGameMode(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => RTPlayer.GameMode = (GameMode)modifier.GetInt(0, 0);
        
        public static void gameMode(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => RTPlayer.GameMode = (GameMode)modifier.GetInt(0, 0);

        public static void blackHole(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            float p = Time.deltaTime * 60f * CoreHelper.ForwardPitch;

            float num = modifier.GetFloat(0, 0.01f, variables);

            if (modifier.GetBool(1, false, variables))
                num = -(beatmapObject.Interpolate(3, 1) - 1f) * num;

            if (num == 0f)
                return;

            float moveDelay = 1f - Mathf.Pow(1f - Mathf.Clamp(num, 0.001f, 1f), p);
            var players = PlayerManager.Players;

            var pos = beatmapObject.GetFullPosition();

            players.ForLoop(player =>
            {
                if (!player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                var transform = player.RuntimePlayer.rb.transform;

                var vector = new Vector3(transform.position.x, transform.position.y, 0f);
                var target = new Vector3(pos.x, pos.y, 0f);

                transform.position += (target - vector) * moveDelay;
            });
        }

        public static void whiteHole(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            // todo
        }

        #endregion

        #region Mouse Cursor

        public static void showMouse(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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

        public static void hideMouse(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditorPreview)
                CursorManager.inst.HideCursor();
        }

        public static void setMousePosition(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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
        
        public static void followMousePosition(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.value == "0")
                modifier.value = "1";

            if (reference is not ITransformable transformable)
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
        public static void getToggle(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var value = modifier.GetBool(1, false, variables);

            if (modifier.GetBool(2, false, variables))
                value = !value;

            variables[modifier.GetValue(0)] = value.ToString();
        }
        
        public static void getFloat(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = modifier.GetFloat(1, 0f, variables).ToString();
        }
        
        public static void getInt(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = modifier.GetInt(1, 0, variables).ToString();
        }

        public static void getString(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = modifier.GetValue(1, variables);
        }
        
        public static void getStringLower(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = modifier.GetValue(1, variables).ToLower();
        }
        
        public static void getStringUpper(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = modifier.GetValue(1, variables).ToUpper();
        }

        public static void getColor(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = modifier.GetInt(1, 0, variables).ToString();
        }

        public static void getEnum(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var index = (modifier.GetInt(1, 0, variables) * 2) + 4;
            if (modifier.commands.Count > index)
                variables[modifier.GetValue(0)] = modifier.GetValue(index, variables).ToString();
        }

        public static void getTag(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = reference is IModifyable modifyable && modifyable.Tags.TryGetAt(modifier.GetInt(1, 0, variables), out string tag) ? tag : string.Empty;
        }

        public static void getPitch(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = AudioManager.inst.CurrentAudioSource.pitch.ToString();
        }

        public static void getMusicTime(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = AudioManager.inst.CurrentAudioSource.time.ToString();
        }

        public static void getAxis(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var prefabable = reference.AsPrefabable();
            if (prefabable == null)
                return;

            int fromType = modifier.GetInt(1, 0, variables);
            int fromAxis = modifier.GetInt(2, 0, variables);

            float delay = modifier.GetFloat(3, 0f, variables);
            float multiply = modifier.GetFloat(4, 0f, variables);
            float offset = modifier.GetFloat(5, 0f, variables);
            float min = modifier.GetFloat(6, -9999f, variables);
            float max = modifier.GetFloat(7, 9999f, variables);
            bool useVisual = modifier.GetBool(8, false, variables);
            float loop = modifier.GetFloat(9, 9999f, variables);

            var beatmapObject = modifier.GetResultOrDefault(() => GameData.Current.FindObjectWithTag(modifier, prefabable, modifier.GetValue(10, variables)));
            if (!beatmapObject)
                return;

            fromType = Mathf.Clamp(fromType, 0, beatmapObject.events.Count);
            if (!useVisual)
                fromAxis = Mathf.Clamp(fromAxis, 0, beatmapObject.events[fromType][0].values.Length);

            if (fromType < 0 || fromType > 2)
                return;

            variables[modifier.GetValue(0)] = ModifiersHelper.GetAnimation(prefabable, beatmapObject, fromType, fromAxis, min, max, offset, multiply, delay, loop, useVisual).ToString();
        }

        public static void getMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IEvaluatable evaluatable)
            {
                var numberVariables = new Dictionary<string, float>();
                ModifiersHelper.SetVariables(variables, numberVariables);

                variables[modifier.GetValue(0)] = RTMath.Parse(modifier.GetValue(1, variables), numberVariables).ToString();
                return;
            }

            try
            {
                var numberVariables = evaluatable.GetObjectVariables();
                ModifiersHelper.SetVariables(variables, numberVariables);

                variables[modifier.GetValue(0)] = RTMath.Parse(modifier.GetValue(1, variables), numberVariables, evaluatable.GetObjectFunctions()).ToString();
            }
            catch { }
        }

        public static void getNearestPlayer(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not ITransformable transformable)
                return;

            var pos = transformable.GetFullPosition();
            variables[modifier.GetValue(0)] = PlayerManager.GetClosestPlayerIndex(pos).ToString();
        }

        public static void getCollidingPlayers(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.collider)
            {
                var collider = runtimeObject.visualObject.collider;

                var players = PlayerManager.Players;
                for (int i = 0; i < players.Count; i++)
                {
                    var player = players[i];
                    variables[modifier.GetValue(0) + "_" + i] = (player.RuntimePlayer && player.RuntimePlayer.CurrentCollider && player.RuntimePlayer.CurrentCollider.IsTouching(collider)).ToString();
                }
            }
        }

        public static void getPlayerHealth(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(1, 0, variables), out PAPlayer player))
                variables[modifier.GetValue(0)] = player.Health.ToString();
        }

        public static void getPlayerLives(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(1, 0, variables), out PAPlayer player))
                variables[modifier.GetValue(0)] = player.lives.ToString();
        }

        public static void getPlayerPosX(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(1, 0, variables), out PAPlayer player) && player.RuntimePlayer && player.RuntimePlayer.rb)
                variables[modifier.GetValue(0)] = player.RuntimePlayer.rb.transform.position.x.ToString();
        }
        
        public static void getPlayerPosY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(1, 0, variables), out PAPlayer player) && player.RuntimePlayer && player.RuntimePlayer.rb)
                variables[modifier.GetValue(0)] = player.RuntimePlayer.rb.transform.position.y.ToString();
        }

        public static void getPlayerRot(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (PlayerManager.Players.TryGetAt(modifier.GetInt(1, 0, variables), out PAPlayer player) && player.RuntimePlayer && player.RuntimePlayer.rb)
                variables[modifier.GetValue(0)] = player.RuntimePlayer.rb.transform.eulerAngles.z.ToString();
        }

        public static void getEventValue(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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

        public static void getSample(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = RTLevel.Current.GetSample(modifier.GetInt(1, 0, variables), modifier.GetFloat(2, 1f, variables)).ToString();
        }

        public static void getText(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var useVisual = modifier.GetBool(1, false, variables);
            if (useVisual && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject is TextObject textObject)
                variables[modifier.GetValue(0)] = textObject.GetText();
            else
                variables[modifier.GetValue(0)] = beatmapObject.text;
        }

        public static void getTextOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            if (!GameData.Current.TryFindObjectWithTag(modifier, prefabable, modifier.GetValue(2, variables), out BeatmapObject beatmapObject))
                return;

            var useVisual = modifier.GetBool(1, false, variables);
            if (useVisual && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject is TextObject textObject)
                variables[modifier.GetValue(0)] = textObject.GetText();
            else
                variables[modifier.GetValue(0)] = beatmapObject.text;
        }

        public static void getCurrentKey(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = CoreHelper.GetKeyCodeDown().ToString();
        }

        public static void getColorSlotHexCode(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var color = ThemeManager.inst.Current.GetObjColor(modifier.GetInt(1, 0, variables));
            color = RTColors.FadeColor(color, modifier.GetFloat(2, 1f, variables));
            color = RTColors.ChangeColorHSV(color, modifier.GetFloat(3, 0f, variables), modifier.GetFloat(4, 0f, variables), modifier.GetFloat(5, 0f, variables));

            variables[modifier.GetValue(0)] = RTColors.ColorToHexOptional(color);
        }

        public static void getFloatFromHexCode(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = RTColors.HexToFloat(modifier.GetValue(1, variables)).ToString();
        }

        public static void getHexCodeFromFloat(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = RTColors.FloatToHex(modifier.GetFloat(1, 1f, variables));
        }

        public static void getJSONString(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!RTFile.TryReadFromFile(ModifiersHelper.GetSaveFile(modifier.GetValue(1, variables)), out string json))
                return;

            var jn = JSON.Parse(json);

            var fjn = jn[modifier.GetValue(2, variables)][modifier.GetValue(3, variables)]["string"];

            variables[modifier.GetValue(0)] = fjn;
        }
        
        public static void getJSONFloat(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!RTFile.TryReadFromFile(ModifiersHelper.GetSaveFile(modifier.GetValue(1, variables)), out string json))
                return;

            var jn = JSON.Parse(json);

            var fjn = jn[modifier.GetValue(2, variables)][modifier.GetValue(3, variables)]["float"];

            variables[modifier.GetValue(0)] = fjn;
        }

        public static void getJSON(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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

        public static void getSubString(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            try
            {
                var str = modifier.GetValue(1, variables);
                var subString = str.Substring(Mathf.Clamp(modifier.GetInt(2, 0, variables), 0, str.Length), Mathf.Clamp(modifier.GetInt(3, 0, variables), 0, str.Length));
                variables[modifier.GetValue(0)] = subString;
            }
            catch { }
        }

        public static void getSplitString(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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
        
        public static void getSplitStringAt(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var str = modifier.GetValue(0, variables);
            var ch = modifier.GetValue(1, variables);

            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(ch))
                return;

            var split = str.Split(ch[0]);
            variables[modifier.GetValue(2)] = split.GetAt(modifier.GetInt(3, 0, variables));
        }
        
        public static void getSplitStringCount(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var str = modifier.GetValue(0, variables);
            var ch = modifier.GetValue(1, variables);

            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(ch))
                return;

            var split = str.Split(ch[0]);
            variables[modifier.GetValue(2)] = split.Length.ToString();
        }

        public static void getStringLength(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = modifier.GetValue(1, variables).Length.ToString();
        }

        public static void getParsedString(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = RTString.ParseText(modifier.GetValue(1, variables));
        }

        public static void getRegex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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

        public static void getFormatVariable(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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

        public static void getComparison(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = (modifier.GetValue(1, variables) == modifier.GetValue(2, variables)).ToString();
        }

        public static void getComparisonMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IEvaluatable evaluatable)
                return;

            try
            {
                var numberVariables = evaluatable.GetObjectVariables();
                var functions = evaluatable.GetObjectFunctions();

                variables[modifier.GetValue(0)] = (RTMath.Parse(modifier.GetValue(1, variables), numberVariables, functions) == RTMath.Parse(modifier.GetValue(2, variables), numberVariables, functions)).ToString();
            }
            catch { }
        }

        public static void getModifiedColor(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var color = RTColors.HexToColor(modifier.GetValue(1, variables));

            variables[modifier.GetValue(0)] = RTColors.ColorToHexOptional(RTColors.FadeColor(RTColors.ChangeColorHSV(color,
                    modifier.GetFloat(3, 0f, variables),
                    modifier.GetFloat(4, 0f, variables),
                    modifier.GetFloat(5, 0f, variables)), modifier.GetFloat(2, 1f, variables)));
        }

        public static void getMixedColors(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var colors = new List<Color>();
            for (int i = 1; i < modifier.commands.Count; i++)
                colors.Add(RTColors.HexToColor(modifier.GetValue(1, variables)));

            variables[modifier.GetValue(0)] = RTColors.MixColors(colors).ToString();
        }

        public static void getVisualColor(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is BeatmapObject beatmapObject && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject is SolidObject solidObject)
            {
                var colors = solidObject.GetColors();
                variables[modifier.GetValue(0)] = RTColors.ColorToHexOptional(colors.startColor);
                variables[modifier.GetValue(1)] = RTColors.ColorToHexOptional(colors.endColor);
            }
        }

        public static void getFloatAnimationKF(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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

        public static void getSignaledVariables(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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

        public static void signalLocalVariables(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));

            if (list.IsEmpty())
                return;

            var sendVariables = new Dictionary<string, string>(variables);

            foreach (var beatmapObject in list)
            {
                beatmapObject.modifiers.FindAll(x => x.Name == nameof(getSignaledVariables)).ForLoop(modifier =>
                {
                    if (modifier.TryGetResult(out Dictionary<string, string> otherVariables))
                    {
                        otherVariables.InsertRange(variables);
                        return;
                    }

                    modifier.Result = sendVariables;
                });
            }
        }

        public static void clearLocalVariables(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => variables.Clear();

        public static void storeLocalVariables(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.TryGetResult(out Dictionary<string, string> storedVariables))
            {
                variables.InsertRange(storedVariables);
                return;
            }

            var storeVariables = new Dictionary<string, string>(variables);
            modifier.Result = storeVariables;
        }

        // object variable
        public static void addVariable(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.commands.Count == 2)
            {
                if (reference is not IPrefabable prefabable)
                    return;

                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
                if (list.IsEmpty())
                    return;

                int num = modifier.GetInt(0, 0, variables);

                foreach (var beatmapObject in list)
                    beatmapObject.integerVariable += num;
            }
            else
                reference.IntVariable += modifier.GetInt(0, 0, variables);
        }
        
        public static void addVariableOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
            if (list.IsEmpty())
                return;

            int num = modifier.GetInt(0, 0, variables);

            foreach (var beatmapObject in list)
                beatmapObject.integerVariable += num;
        }
        
        public static void subVariable(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.commands.Count == 2)
            {
                if (reference is not IPrefabable prefabable)
                    return;

                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
                if (list.IsEmpty())
                    return;

                int num = modifier.GetInt(0, 0, variables);

                foreach (var beatmapObject in list)
                    beatmapObject.integerVariable -= num;
            }
            else
                reference.IntVariable -= modifier.GetInt(0, 0, variables);
        }

        public static void subVariableOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
            if (list.IsEmpty())
                return;

            int num = modifier.GetInt(0, 0, variables);

            foreach (var beatmapObject in list)
                beatmapObject.integerVariable -= num;
        }

        public static void setVariable(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.commands.Count == 2)
            {
                if (reference is not IPrefabable prefabable)
                    return;

                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
                if (list.IsEmpty())
                    return;

                int num = modifier.GetInt(0, 0, variables);

                foreach (var beatmapObject in list)
                    beatmapObject.integerVariable = num;
            }
            else
                reference.IntVariable = modifier.GetInt(0, 0, variables);
        }
        
        public static void setVariableOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
            if (list.IsEmpty())
                return;

            int num = modifier.GetInt(0, 0, variables);

            foreach (var beatmapObject in list)
                beatmapObject.integerVariable = num;
        }
        
        public static void setVariableRandom(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.commands.Count == 3)
            {
                if (reference is not IPrefabable prefabable)
                    return;

                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));
                if (list.IsEmpty())
                    return;

                int min = modifier.GetInt(1, 0, variables);
                int max = modifier.GetInt(2, 0, variables);

                foreach (var beatmapObject in list)
                    beatmapObject.integerVariable = UnityEngine.Random.Range(min, max < 0 ? max - 1 : max + 1);
            }
            else
            {
                var min = modifier.GetInt(0, 0, variables);
                var max = modifier.GetInt(1, 0, variables);
                reference.IntVariable = UnityEngine.Random.Range(min, max < 0 ? max - 1 : max + 1);
            }
        }
        
        public static void setVariableRandomOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));
            if (list.IsEmpty())
                return;

            int min = modifier.GetInt(1, 0, variables);
            int max = modifier.GetInt(2, 0, variables);

            foreach (var beatmapObject in list)
                beatmapObject.integerVariable = UnityEngine.Random.Range(min, max < 0 ? max - 1 : max + 1);
        }
        
        public static void animateVariableOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var fromType = modifier.GetInt(1, 0, variables);
            var fromAxis = modifier.GetInt(2, 0, variables);
            var delay = modifier.GetFloat(3, 0, variables);
            var multiply = modifier.GetFloat(4, 0, variables);
            var offset = modifier.GetFloat(5, 0, variables);
            var min = modifier.GetFloat(6, -9999f, variables);
            var max = modifier.GetFloat(7, 9999f, variables);
            var loop = modifier.GetFloat(8, 9999f, variables);

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));
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
        
        public static void clampVariable(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is IModifyable modifyable)
                modifyable.IntVariable = Mathf.Clamp(modifyable.IntVariable, modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables));
        }
        
        public static void clampVariableOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));

            var min = modifier.GetInt(1, 0, variables);
            var max = modifier.GetInt(2, 0, variables);

            if (!list.IsEmpty())
                foreach (var bm in list)
                    bm.integerVariable = Mathf.Clamp(bm.integerVariable, min, max);
        }

        #endregion

        #region Enable

        public static void enableObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var value = modifier.GetValue(0, variables);
            if (value == "0")
                value = "True";

            if (reference is ICustomActivatable activatable)
            {
                activatable.SetCustomActive(Parser.TryParse(value, true));
                return;
            }

            if (reference is not IPrefabable prefabable)
                return;

            ModifiersHelper.SetObjectActive(prefabable, Parser.TryParse(value, true));
        }
        
        public static void enableObjectTree(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var value = modifier.GetValue(0, variables);
            if (value == "0")
                value = "False";

            var enabled = modifier.GetBool(2, true, variables);

            var list = modifier.GetResultOrDefault(() =>
            {
                if (reference is not BeatmapObject beatmapObject)
                    return new List<BeatmapObject>();

                var root = Parser.TryParse(value, true) ? beatmapObject : beatmapObject.GetParentChain().Last();
                return root.GetChildTree();
            });

            for (int i = 0; i < list.Count; i++)
                list[i].runtimeObject?.SetCustomActive(enabled);
        }
        
        public static void enableObjectOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(2, true, variables);

            var prefabables = modifier.GetResultOrDefault(() =>
            {
                var prefabable = reference.AsPrefabable();
                if (prefabable == null)
                    return new List<IPrefabable>();

                return GameData.Current.FindPrefabablesWithTag(modifier, prefabable, modifier.GetValue(0, variables));
            });

            if (prefabables.IsEmpty())
                return;

            foreach (var other in prefabables)
                ModifiersHelper.SetObjectActive(other, enabled);
        }
        
        public static void enableObjectTreeOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(3, true, variables);

            var list = modifier.GetResultOrDefault(() =>
            {
                var resultList = new List<BeatmapObject>();

                var prefabable = reference.AsPrefabable();
                if (prefabable == null)
                    return resultList;

                var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
                var useSelf = modifier.GetBool(0, true, variables);

                foreach (var bm in beatmapObjects)
                {
                    var beatmapObject = useSelf ? bm : bm.GetParentChain().Last();
                    resultList.AddRange(beatmapObject.GetChildTree());
                }
                return resultList;
            });

            for (int i = 0; i < list.Count; i++)
                list[i].runtimeObject?.SetCustomActive(enabled);
        }

        // if this ever needs to be updated, add a "version" int number to modifiers that increment each time a major change was done to the modifier.
        public static void enableObjectGroup(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var enabled = modifier.GetBool(0, true, variables);
            var state = modifier.GetInt(1, 0, variables);

            var enableObjectGroupCache = modifier.GetResultOrDefault(() =>
            {
                var cache = new EnableObjectGroupCache();
                var prefabable = reference.AsPrefabable();
                if (prefabable == null)
                    return cache;

                var groups = new List<List<IPrefabable>>();
                int count = 0;
                for (int i = 2; i < modifier.commands.Count; i++)
                {
                    var tag = modifier.commands[i];
                    if (string.IsNullOrEmpty(tag))
                        continue;

                    var list = GameData.Current.FindPrefabablesWithTag(modifier, prefabable, tag);
                    groups.Add(list);
                    cache.allObjects.AddRange(list);

                    count++;
                }
                cache.Init(groups.ToArray(), enabled);
                return cache;
            });
            enableObjectGroupCache?.SetGroupActive(enabled, state);
        }

        public static void disableObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is BeatmapObject beatmapObject)
                beatmapObject.runtimeObject?.SetCustomActive(false);
        }

        public static void disableObjectTree(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var value = modifier.GetValue(0, variables);
            if (value == "0")
                value = "False";

            var list = modifier.GetResultOrDefault(() =>
            {
                var root = Parser.TryParse(value, true) ? beatmapObject : beatmapObject.GetParentChain().Last();
                return root.GetChildTree();
            });

            for (int i = 0; i < list.Count; i++)
                list[i].runtimeObject?.SetCustomActive(false);
        }

        public static void disableObjectOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var list = modifier.GetResultOrDefault(() =>
            {
                var prefabable = reference.AsPrefabable();
                if (prefabable == null)
                    return new List<BeatmapObject>();

                return GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));
            });

            if (!list.IsEmpty())
                foreach (var beatmapObject in list)
                    beatmapObject.runtimeObject?.SetCustomActive(false);
        }

        public static void disableObjectTreeOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var list = modifier.GetResultOrDefault(() =>
            {
                var resultList = new List<BeatmapObject>();

                var prefabable = reference.AsPrefabable();
                if (prefabable == null)
                    return resultList;

                var beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
                var useSelf = modifier.GetBool(0, true, variables);

                foreach (var bm in beatmapObjects)
                {
                    var beatmapObject = useSelf ? bm : bm.GetParentChain().Last();
                    resultList.AddRange(beatmapObject.GetChildTree());
                }
                return resultList;
            });

            for (int i = 0; i < list.Count; i++)
                list[i].runtimeObject?.SetCustomActive(false);
        }

        public static void setActive(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is BackgroundObject backgroundObject)
                backgroundObject.Enabled = modifier.GetBool(0, false, variables);
        }
        
        public static void setActiveOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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

        public static void saveFloat(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            ModifiersHelper.SaveProgress(modifier.GetValue(1, variables), modifier.GetValue(2, variables), modifier.GetValue(3, variables), modifier.GetFloat(0, 0f, variables));
        }
        
        public static void saveString(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            ModifiersHelper.SaveProgress(modifier.GetValue(1, variables), modifier.GetValue(2, variables), modifier.GetValue(3, variables), modifier.GetValue(0, variables));
        }
        
        public static void saveText(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is BeatmapObject beatmapObject && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject is TextObject textObject)
                ModifiersHelper.SaveProgress(modifier.GetValue(1, variables), modifier.GetValue(2, variables), modifier.GetValue(3, variables), textObject.textMeshPro.text);
        }
        
        public static void saveVariable(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            ModifiersHelper.SaveProgress(modifier.GetValue(1, variables), modifier.GetValue(2, variables), modifier.GetValue(3, variables), reference.IntVariable);
        }
        
        public static void loadVariable(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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
                reference.IntVariable = (int)eq;
        }
        
        public static void loadVariableOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile", modifier.GetValue(1, variables) + FileFormat.SES.Dot());
            if (!RTFile.FileExists(path))
                return;

            string json = RTFile.ReadFromFile(path);

            if (string.IsNullOrEmpty(json))
                return;

            var jn = JSON.Parse(json);
            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));
            var fjn = jn[modifier.GetValue(2, variables)][modifier.GetValue(3, variables)]["float"];

            if (list.Count > 0 && !string.IsNullOrEmpty(fjn) && float.TryParse(fjn, out float eq))
                foreach (var bm in list)
                    bm.integerVariable = (int)eq;
        }

        #endregion

        #region Reactive

        public static void reactivePos(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var val = modifier.GetFloat(0, 0f, variables);
            var sampleX = modifier.GetInt(1, 0, variables);
            var sampleY = modifier.GetInt(2, 0, variables);
            var intensityX = modifier.GetFloat(3, 0f, variables);
            var intensityY = modifier.GetFloat(4, 0f, variables);

            beatmapObject.runtimeObject?.visualObject?.SetOrigin(new Vector3(
                beatmapObject.origin.x + RTLevel.Current.GetSample(sampleX, intensityX * val),
                beatmapObject.origin.y + RTLevel.Current.GetSample(sampleY, intensityY * val),
                beatmapObject.Depth * 0.1f));
        }
        
        public static void reactiveSca(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var val = modifier.GetFloat(0, 0f, variables);
            var sampleX = modifier.GetInt(1, 0, variables);
            var sampleY = modifier.GetInt(2, 0, variables);
            var intensityX = modifier.GetFloat(3, 0f, variables);
            var intensityY = modifier.GetFloat(4, 0f, variables);

            beatmapObject.runtimeObject?.visualObject?.SetScaleOffset(new Vector2(
                1f + RTLevel.Current.GetSample(sampleX, intensityX * val),
                1f + RTLevel.Current.GetSample(sampleY, intensityY * val)));
        }
        
        public static void reactiveRot(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is BeatmapObject beatmapObject)
                beatmapObject.runtimeObject?.visualObject?.SetRotationOffset(RTLevel.Current.GetSample(modifier.GetInt(1, 0, variables), modifier.GetFloat(0, 0f, variables)));
        }
        
        public static void reactiveCol(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.renderer)
                runtimeObject.visualObject.SetColor(runtimeObject.visualObject.GetPrimaryColor() + ThemeManager.inst.Current.GetObjColor(modifier.GetInt(2, 0, variables)) * RTLevel.Current.GetSample(modifier.GetInt(1, 0, variables), modifier.GetFloat(0, 0f, variables)));
        }
        
        public static void reactiveColLerp(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.renderer)
                runtimeObject.visualObject.SetColor(RTMath.Lerp(runtimeObject.visualObject.GetPrimaryColor(), ThemeManager.inst.Current.GetObjColor(modifier.GetInt(2, 0, variables)), RTLevel.Current.GetSample(modifier.GetInt(1, 0, variables), modifier.GetFloat(0, 0f, variables))));
        }
        
        public static void reactivePosChain(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IReactive reactive)
                return;

            var val = modifier.GetFloat(0, 0f, variables);
            var sampleX = modifier.GetInt(1, 0, variables);
            var sampleY = modifier.GetInt(2, 0, variables);
            var intensityX = modifier.GetFloat(3, 0f, variables);
            var intensityY = modifier.GetFloat(4, 0f, variables);

            float reactivePositionX = RTLevel.Current.GetSample(sampleX, intensityX * val);
            float reactivePositionY = RTLevel.Current.GetSample(sampleY, intensityY * val);

            reactive.ReactivePositionOffset = new Vector3(reactivePositionX, reactivePositionY);
        }
        
        public static void reactiveScaChain(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IReactive reactive)
                return;

            var val = modifier.GetFloat(0, 0f, variables);
            var sampleX = modifier.GetInt(1, 0, variables);
            var sampleY = modifier.GetInt(2, 0, variables);
            var intensityX = modifier.GetFloat(3, 0f, variables);
            var intensityY = modifier.GetFloat(4, 0f, variables);

            float reactiveScaleX = RTLevel.Current.GetSample(sampleX, intensityX * val);
            float reactiveScaleY = RTLevel.Current.GetSample(sampleY, intensityY * val);

            reactive.ReactiveScaleOffset = new Vector3(reactiveScaleX, reactiveScaleY, 1f);
        }
        
        public static void reactiveRotChain(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is IReactive reactive)
                reactive.ReactiveRotationOffset = RTLevel.Current.GetSample(modifier.GetInt(1, 0, variables), modifier.GetFloat(0, 0f, variables));
        }

        #endregion

        #region Events

        public static void eventOffset(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTLevel.Current.eventEngine && RTLevel.Current.eventEngine.offsets != null)
                RTLevel.Current.eventEngine.SetOffset(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables), modifier.GetFloat(0, 1f, variables));
        }
        
        public static void eventOffsetVariable(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTLevel.Current.eventEngine && RTLevel.Current.eventEngine.offsets != null && reference is IModifyable modifyable)
                RTLevel.Current.eventEngine.SetOffset(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables), modifyable.IntVariable * modifier.GetFloat(0, 1f, variables));
        }
        
        public static void eventOffsetMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (RTLevel.Current.eventEngine && RTLevel.Current.eventEngine.offsets != null && reference is IEvaluatable evaluatable)
            {
                var numberVariables = evaluatable.GetObjectVariables();
                ModifiersHelper.SetVariables(variables, numberVariables);
                RTLevel.Current.eventEngine.SetOffset(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables), RTMath.Parse(modifier.GetValue(0, variables), numberVariables, evaluatable.GetObjectFunctions()));
            }
        }
        
        public static void eventOffsetAnimate(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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
        
        public static void eventOffsetCopyAxis(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!RTLevel.Current.eventEngine || RTLevel.Current.eventEngine.offsets == null || reference is not BeatmapObject beatmapObject)
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

            fromType = Mathf.Clamp(fromType, 0, beatmapObject.events.Count - 1);
            fromAxis = Mathf.Clamp(fromAxis, 0, beatmapObject.events[fromType][0].values.Length - 1);
            toType = Mathf.Clamp(toType, 0, RTLevel.Current.eventEngine.offsets.Count - 1);
            toAxis = Mathf.Clamp(toAxis, 0, RTLevel.Current.eventEngine.offsets[toType].Count - 1);

            if (!useVisual && beatmapObject.cachedSequences)
                RTLevel.Current.eventEngine.SetOffset(toType, toAxis, fromType switch
                {
                    0 => Mathf.Clamp((beatmapObject.cachedSequences.PositionSequence.Interpolate(time - beatmapObject.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                    1 => Mathf.Clamp((beatmapObject.cachedSequences.ScaleSequence.Interpolate(time - beatmapObject.StartTime - delay).At(fromAxis) - offset) * multiply % loop, min, max),
                    2 => Mathf.Clamp((beatmapObject.cachedSequences.RotationSequence.Interpolate(time - beatmapObject.StartTime - delay) - offset) * multiply % loop, min, max),
                    _ => 0f,
                });
            else if (beatmapObject.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.gameObject)
                RTLevel.Current.eventEngine.SetOffset(toType, toAxis, Mathf.Clamp((runtimeObject.visualObject.gameObject.transform.GetVector(fromType).At(fromAxis) - offset) * multiply % loop, min, max));
        }
        
        public static void vignetteTracksPlayer(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!RTLevel.Current.eventEngine)
                return;

            var players = PlayerManager.Players;
            if (players.IsEmpty())
                return;

            var player = players[0].RuntimePlayer;

            if (!player || !player.rb)
                return;

            var cameraToViewportPoint = Camera.main.WorldToViewportPoint(player.rb.position);
            RTLevel.Current.eventEngine.SetOffset(7, 4, cameraToViewportPoint.x);
            RTLevel.Current.eventEngine.SetOffset(7, 5, cameraToViewportPoint.y);
        }
        
        public static void lensTracksPlayer(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!RTLevel.Current.eventEngine)
                return;

            var players = PlayerManager.Players;
            if (players.IsEmpty())
                return;

            var player = players[0].RuntimePlayer;

            if (!player || !player.rb)
                return;

            var cameraToViewportPoint = Camera.main.WorldToViewportPoint(player.rb.position);
            RTLevel.Current.eventEngine.SetOffset(8, 1, cameraToViewportPoint.x - 0.5f);
            RTLevel.Current.eventEngine.SetOffset(8, 2, cameraToViewportPoint.y - 0.5f);
        }

        #endregion

        #region Color

        public static void addColor(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || !beatmapObject.runtimeObject || !beatmapObject.runtimeObject.visualObject)
                return;

            var multiply = modifier.GetFloat(0, 1f, variables);
            var index = modifier.GetInt(1, 0, variables);
            var hue = modifier.GetFloat(2, 0f, variables);
            var sat = modifier.GetFloat(3, 0f, variables);
            var val = modifier.GetFloat(4, 0f, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                beatmapObject.runtimeObject.visualObject.SetColor(beatmapObject.runtimeObject.visualObject.GetPrimaryColor() + RTColors.ChangeColorHSV(ThemeManager.inst.Current.GetObjColor(index), hue, sat, val) * multiply);
            });
        }
        
        public static void addColorOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var list = modifier.GetResultOrDefault(() =>
            {
                var prefabable = reference.AsPrefabable();
                if (prefabable == null)
                    return new List<BeatmapObject>();

                return GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1));
            });

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
        
        public static void lerpColor(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || !beatmapObject.runtimeObject || !beatmapObject.runtimeObject.visualObject)
                return;

            var multiply = modifier.GetFloat(0, 0f, variables);
            var index = modifier.GetInt(1, 0, variables);
            var hue = modifier.GetFloat(2, 0f, variables);
            var sat = modifier.GetFloat(3, 0f, variables);
            var val = modifier.GetFloat(4, 0f, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                beatmapObject.runtimeObject.visualObject.SetColor(RTMath.Lerp(beatmapObject.runtimeObject.visualObject.GetPrimaryColor(), RTColors.ChangeColorHSV(ThemeManager.inst.Current.GetObjColor(index), hue, sat, val), multiply));
            });
        }
        
        public static void lerpColorOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var list = modifier.GetResultOrDefault(() =>
            {
                var prefabable = reference.AsPrefabable();
                if (prefabable == null)
                    return new List<BeatmapObject>();

                return GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1));
            });

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
        
        public static void addColorPlayerDistance(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject.gameObject)
                return;

            var offset = modifier.GetFloat(0, 0f, variables);
            var index = modifier.GetInt(1, 0, variables);
            var multiply = modifier.GetFloat(2, 0, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var player = PlayerManager.GetClosestPlayer(runtimeObject.visualObject.gameObject.transform.position);

                if (!player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                var distance = Vector2.Distance(player.RuntimePlayer.rb.transform.position, runtimeObject.visualObject.gameObject.transform.position);

                runtimeObject.visualObject.SetColor(runtimeObject.visualObject.GetPrimaryColor() + ThemeManager.inst.Current.GetObjColor(index) * -(distance * multiply - offset));
            });
        }
        
        public static void lerpColorPlayerDistance(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject.gameObject)
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

                if (!player.RuntimePlayer || !player.RuntimePlayer.rb)
                    return;

                var distance = Vector2.Distance(player.RuntimePlayer.rb.transform.position, runtimeObject.visualObject.gameObject.transform.position);

                runtimeObject.visualObject.SetColor(Color.Lerp(runtimeObject.visualObject.GetPrimaryColor(),
                                RTColors.FadeColor(RTColors.ChangeColorHSV(ThemeManager.inst.Current.GetObjColor(index), hue, sat, val), opacity),
                                -(distance * multiply - offset)));
            });
        }
        
        public static void setOpacity(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject.gameObject)
                return;

            var opacity = modifier.GetFloat(0, 1f, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                runtimeObject.visualObject.SetColor(RTColors.FadeColor(runtimeObject.visualObject.GetPrimaryColor(), opacity));
            });
        }
        
        public static void setOpacityOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var opacity = modifier.GetFloat(0, 1f, variables);

            var list = modifier.GetResultOrDefault(() => GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables)));

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
        
        public static void copyColor(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || !beatmapObject.runtimeObject)
                return;

            var applyColor1 = modifier.GetBool(1, true, variables);
            var applyColor2 = modifier.GetBool(2, true, variables);

            var other = modifier.GetResultOrDefault(() => GameData.Current.FindObjectWithTag(modifier, beatmapObject, modifier.GetValue(0, variables)));

            if (!other || !other.runtimeObject)
                return;

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() => ModifiersHelper.CopyColor(beatmapObject.runtimeObject, other.runtimeObject, applyColor1, applyColor2));
        }
        
        public static void copyColorOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var list = modifier.GetResultOrDefault(() => GameData.Current.FindObjectsWithTag(modifier, beatmapObject, modifier.GetValue(0, variables)));

            if (list.IsEmpty())
                return;

            var applyColor1 = modifier.GetBool(1, true, variables);
            var applyColor2 = modifier.GetBool(2, true, variables);

            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject)
                return;

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                foreach (var bm in list)
                {
                    var otherRuntimeObject = bm.runtimeObject;
                    if (!otherRuntimeObject)
                        continue;

                    ModifiersHelper.CopyColor(otherRuntimeObject, runtimeObject, applyColor1, applyColor2);
                }
            });
        }
        
        public static void applyColorGroup(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var list = modifier.GetResultOrDefault(() => GameData.Current.FindObjectsWithTag(modifier, beatmapObject, modifier.GetValue(0, variables)));

            var cachedSequences = beatmapObject.cachedSequences;
            if (list.IsEmpty() || !cachedSequences)
                return;

            var type = modifier.GetInt(1, 0, variables);
            var axis = modifier.GetInt(2, 0, variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var time = reference.GetParentRuntime().CurrentTime - beatmapObject.StartTime;
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

                var isEmpty = beatmapObject.objectType == BeatmapObject.ObjectType.Empty;

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
        
        public static void setColorHex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
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
        
        public static void setColorHexOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            List<BeatmapObject> list = modifier.GetResultOrDefault(() => GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables)));

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

        public static void setColorRGBA(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
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

        public static void setColorRGBAOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(8, variables));

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

        public static void animateColorKF(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not ILifetime<AutoKillType> lifetime)
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

            var beatmapObject = reference as BeatmapObject;
            var backgroundObject = reference as BackgroundObject;

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

        public static void animateColorKFHex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not ILifetime<AutoKillType> lifetime)
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

            var beatmapObject = reference as BeatmapObject;
            var backgroundObject = reference as BackgroundObject;

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

        public static void translateShape(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
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

        public static void setShape(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IShapeable shapeable)
                return;

            shapeable.SetCustomShape(modifier.GetInt(0, 0, variables), modifier.GetInt(1, 0, variables));
            if (shapeable is BeatmapObject beatmapObject)
                reference.GetParentRuntime()?.UpdateObject(beatmapObject, ObjectContext.SHAPE);
            else if (shapeable is BackgroundObject backgroundObject)
                backgroundObject.runtimeObject?.UpdateShape(backgroundObject.Shape, backgroundObject.ShapeOption);
        }

        public static void setPolygonShape(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not PolygonObject polygonObject)
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

        public static void setPolygonShapeOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

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

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));

            for (int i = 0; i < list.Count; i++)
            {
                var beatmapObject = list[i];
                if (beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject is PolygonObject polygonObject)
                    polygonObject.UpdatePolygon(radius, sides, roundness, thickness, slices, thicknessOffset, thicknessScale, rotation);
            }

            modifier.Result = meshParams;
        }

        public static void actorFrameTexture(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || beatmapObject.ShapeType != ShapeType.Image || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not ImageObject imageObject)
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

        public static void setImage(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || reference is not BeatmapObject beatmapObject || beatmapObject.ShapeType != ShapeType.Image || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not ImageObject imageObject)
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
        
        public static void setImageOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant)
                return;

            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));

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

        public static void setText(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || beatmapObject.ShapeType != ShapeType.Text || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not TextObject textObject)
                return;

            if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                textObject.SetText(modifier.GetValue(0, variables));
            else
                textObject.text = modifier.GetValue(0, variables);
        }

        public static void setTextOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));

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

        public static void addText(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || beatmapObject.ShapeType != ShapeType.Text || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not TextObject textObject)
                return;

            if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                textObject.SetText(textObject.textMeshPro.text + modifier.GetValue(0, variables));
            else
                textObject.text += modifier.GetValue(0, variables);
        }

        public static void addTextOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));

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

        public static void removeText(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || beatmapObject.ShapeType != ShapeType.Text || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not TextObject textObject)
                return;

            string text = string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty :
                textObject.textMeshPro.text.Substring(0, textObject.textMeshPro.text.Length - Mathf.Clamp(modifier.GetInt(0, 1, variables), 0, textObject.textMeshPro.text.Length));

            if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                textObject.SetText(text);
            else
                textObject.text = text;
        }

        public static void removeTextOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));

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

        public static void removeTextAt(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || beatmapObject.ShapeType != ShapeType.Text || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not TextObject textObject)
                return;

            var remove = modifier.GetInt(0, 1, variables);
            string text = string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty : textObject.textMeshPro.text.Length > remove ?
                textObject.textMeshPro.text.Remove(remove, 1) : string.Empty;

            if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                textObject.SetText(text);
            else
                textObject.text = text;
        }

        public static void removeTextOtherAt(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));

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

        public static void formatText(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!CoreConfig.Instance.AllowCustomTextFormatting.Value && reference is BeatmapObject beatmapObject && beatmapObject.ShapeType == ShapeType.Text &&
                beatmapObject.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject is TextObject textObject)
                textObject.SetText(RTString.FormatText(beatmapObject, textObject.text, variables));
        }

        public static void textSequence(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || beatmapObject.ShapeType != ShapeType.Text || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not TextObject textObject)
                return;

            var value = modifier.GetValue(9, variables);
            var text = !string.IsNullOrEmpty(value) ? value : beatmapObject.text;

            if (!modifier.setTimer)
            {
                modifier.setTimer = true;
                modifier.ResultTimer = AudioManager.inst.CurrentAudioSource.time;
            }

            var offsetTime = modifier.ResultTimer;
            if (!modifier.GetBool(11, false, variables))
                offsetTime = beatmapObject.StartTime;

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
                    SoundManager.inst.PlaySound(soundAsset.audio, volume, pitch, panStereo: modifier.GetFloat(12, 0f, variables));
                else if (SoundManager.inst.TryGetSound(soundName, out AudioClip audioClip))
                    SoundManager.inst.PlaySound(audioClip, volume, pitch, panStereo: modifier.GetFloat(12, 0f, variables));
                else
                    ModifiersHelper.GetSoundPath(beatmapObject.id, soundName, modifier.GetBool(5, false, variables), pitch, volume, false, modifier.GetFloat(12, 0f, variables));
            }
        }

        // modify shape
        public static void backgroundShape(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (modifier.HasResult() || beatmapObject.IsSpecialShape || !runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            if (ShapeManager.inst.Shapes3D.TryGetAt(beatmapObject.Shape, out ShapeGroup shapeGroup) && shapeGroup.TryGetShape(beatmapObject.ShapeOption, out Shape shape))
            {
                runtimeObject.visualObject.gameObject.GetComponent<MeshFilter>().mesh = shape.mesh;
                modifier.Result = "frick";
                runtimeObject.visualObject.gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
            }
        }

        public static void sphereShape(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var runtimeObject = beatmapObject.runtimeObject;
            if (modifier.HasResult() || beatmapObject.IsSpecialShape || !runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                return;

            runtimeObject.visualObject.gameObject.GetComponent<MeshFilter>().mesh = GameManager.inst.PlayerPrefabs[1].GetComponentInChildren<MeshFilter>().mesh;
            modifier.Result = "frick";
            runtimeObject.visualObject.gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
        }

        #endregion

        #region Animation

        public static void animateObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var transformable = reference.AsTransformable();
            if (transformable == null)
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
        
        public static void animateObjectOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var transformables = GameData.Current.FindTransformables(modifier, prefabable, modifier.GetValue(7));

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

            foreach (var transformable in transformables)
            {
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
                    var animation = new RTAnimation("Animate Other Object Offset");

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
                    continue;
                }

                transformable.SetTransform(type, setVector);
            }
        }

        // tests modifier keyframing
        // todo: see if i can get homing to work via adding a keyframe depending on audio time
        public static void animateObjectKF(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not ITransformable transformable || reference is not ILifetime<AutoKillType> lifetime)
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

        public static void animateSignal(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable || reference is not ITransformable transformable)
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
                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, signalGroup);

                foreach (var bm in list)
                {
                    if (!bm.modifiers.IsEmpty() && !bm.modifiers.FindAll(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger).IsEmpty() &&
                        bm.modifiers.TryFind(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger, out Modifier m))
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

                    var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, signalGroup);

                    foreach (var bm in list)
                        CoroutineHelper.StartCoroutine(ModifiersHelper.ActivateModifier(bm, delay));
                };
                AnimationManager.inst.Play(animation);
                return;
            }

            transformable.SetTransform(type, setVector);
        }

        public static void animateSignalOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var transformables = GameData.Current.FindTransformables(modifier, prefabable, modifier.GetValue(7, variables));

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
                var list2 = GameData.Current.FindObjectsWithTag(modifier, prefabable, signalGroup);

                foreach (var bm in list2)
                {
                    if (!bm.modifiers.IsEmpty() && !bm.modifiers.FindAll(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger).IsEmpty() &&
                        bm.modifiers.TryFind(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger, out Modifier m))
                    {
                        m.Result = null;
                    }
                }
            }

            string easing = modifier.GetValue(6, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var applyDeltaTime = modifier.GetBool(11, true, variables);

            foreach (var transformable in transformables)
            {
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
                    var animation = new RTAnimation("Animate Other Object Offset");

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

                        var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, signalGroup);

                        foreach (var bm in list)
                            CoroutineHelper.StartCoroutine(ModifiersHelper.ActivateModifier(bm, delay));
                    };
                    AnimationManager.inst.Play(animation);
                    break;
                }

                transformable.SetTransform(type, setVector);
            }
        }
        
        public static void animateObjectMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var transformable = reference.AsTransformable();
            if (transformable == null)
                return;

            if (reference is not IEvaluatable evaluatable)
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
        
        public static void animateObjectMathOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable || reference is not IEvaluatable evaluatable)
                return;

            var transformables = modifier.GetResultOrDefault(() => GameData.Current.FindTransformables(modifier, prefabable, modifier.GetValue(7, variables)));

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

            foreach (var transformable in transformables)
            {
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
                    var animation = new RTAnimation("Animate Other Object Offset");

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
                    continue;
                }

                transformable.SetTransform(type, setVector);
            }
        }
        
        public static void animateSignalMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IEvaluatable evaluatable || reference is not IPrefabable prefabable || reference is not ITransformable transformable)
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
                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, signalGroup);

                foreach (var bm in list)
                {
                    if (!bm.modifiers.IsEmpty() && !bm.modifiers.FindAll(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger).IsEmpty() &&
                        bm.modifiers.TryFind(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger, out Modifier m))
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

                    var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, signalGroup);

                    foreach (var bm in list)
                        CoroutineHelper.StartCoroutine(ModifiersHelper.ActivateModifier(bm, signalTime));
                };
                AnimationManager.inst.Play(animation);
                return;
            }

            transformable.SetTransform(type, setVector);
        }
        
        public static void animateSignalMathOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable || reference is not IEvaluatable evaluatable)
                return;

            var transformables = GameData.Current.FindTransformables(modifier, prefabable, modifier.GetValue(7, variables));

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
                var list2 = GameData.Current.FindObjectsWithTag(modifier, prefabable, signalGroup);

                foreach (var bm in list2)
                {
                    if (!bm.modifiers.IsEmpty() && bm.modifiers.FindAll(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger).Count > 0 &&
                        bm.modifiers.TryFind(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger, out Modifier m))
                        m.Result = null;
                }
            }

            string easing = modifier.GetValue(6, variables);
            if (int.TryParse(easing, out int e) && e >= 0 && e < DataManager.inst.AnimationList.Count)
                easing = DataManager.inst.AnimationList[e].Name;

            var applyDeltaTime = modifier.GetBool(11, true, variables);

            foreach (var transformable in transformables)
            {
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
                    var animation = new RTAnimation("Animate Other Object Offset");

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

                        var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, signalGroup);

                        foreach (var bm in list)
                            CoroutineHelper.StartCoroutine(ModifiersHelper.ActivateModifier(bm, signalTime));
                    };
                    AnimationManager.inst.Play(animation);
                    continue;
                }

                transformable.SetTransform(type, setVector);
            }
        }

        public static void applyAnimation(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var cache = modifier.GetResultOrDefault(() =>
            {
                var applyAnimationCache = new ApplyAnimationCache();
                applyAnimationCache.from = GameData.Current.FindObjectWithTag(modifier, beatmapObject, modifier.GetValue(0, variables));
                applyAnimationCache.to = GameData.Current.FindObjectsWithTag(modifier, beatmapObject, modifier.GetValue(10, variables));
                applyAnimationCache.startTime = reference.GetParentRuntime()?.CurrentTime ?? 0f;
                return applyAnimationCache;
            });

            if (!cache.from)
                return;

            var from = cache.from;
            var list = cache.to;
            var time = cache.startTime;

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
                AnimationManager.inst.RemoveName("Apply Object Animation " + beatmapObject.id);

            for (int i = 0; i < list.Count; i++)
            {
                var bm = list[i];

                if (!modifier.constant)
                {
                    var animation = new RTAnimation("Apply Object Animation " + beatmapObject.id);
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

                ModifiersHelper.ApplyAnimationTo(bm, from, useVisual, time, reference.GetParentRuntime().CurrentTime, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
            }
        }

        public static void applyAnimationFrom(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var cache = modifier.GetResultOrDefault(() =>
            {
                var applyAnimationCache = new ApplyAnimationCache();
                applyAnimationCache.from = GameData.Current.FindObjectWithTag(modifier, beatmapObject, modifier.GetValue(0, variables));
                applyAnimationCache.startTime = reference.GetParentRuntime()?.CurrentTime ?? 0f;
                return applyAnimationCache;
            });

            if (!cache.from)
                return;

            var from = cache.from;
            var time = cache.startTime;

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
                AnimationManager.inst.RemoveName("Apply Object Animation " + beatmapObject.id);

                var animation = new RTAnimation("Apply Object Animation " + beatmapObject.id);
                animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, 0f, Ease.Linear),
                            new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                        }, x => ModifiersHelper.ApplyAnimationTo(beatmapObject, from, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
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

            ModifiersHelper.ApplyAnimationTo(beatmapObject, from, useVisual, time, reference.GetParentRuntime().CurrentTime, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
        }

        public static void applyAnimationTo(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var cache = modifier.GetResultOrDefault(() =>
            {
                var applyAnimationCache = new ApplyAnimationCache();
                applyAnimationCache.to = GameData.Current.FindObjectsWithTag(modifier, beatmapObject, modifier.GetValue(0, variables));
                applyAnimationCache.startTime = reference.GetParentRuntime()?.CurrentTime ?? 0f;
                return applyAnimationCache;
            });

            var list = cache.to;
            var time = cache.startTime;

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
                AnimationManager.inst.RemoveName("Apply Object Animation " + beatmapObject.id);

            for (int i = 0; i < list.Count; i++)
            {
                var bm = list[i];

                if (!modifier.constant)
                {
                    var animation = new RTAnimation("Apply Object Animation " + beatmapObject.id);
                    animation.animationHandlers = new List<AnimationHandlerBase>
                        {
                            new AnimationHandler<float>(new List<IKeyframe<float>>
                            {
                                new FloatKeyframe(0f, 0f, Ease.Linear),
                                new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                            }, x => ModifiersHelper.ApplyAnimationTo(bm, beatmapObject, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
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

                ModifiersHelper.ApplyAnimationTo(bm, beatmapObject, useVisual, time, reference.GetParentRuntime().CurrentTime, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
            }
        }

        public static void applyAnimationMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var cache = modifier.GetResultOrDefault(() =>
            {
                var applyAnimationCache = new ApplyAnimationCache();
                applyAnimationCache.from = GameData.Current.FindObjectWithTag(modifier, beatmapObject, modifier.GetValue(0, variables));
                applyAnimationCache.to = GameData.Current.FindObjectsWithTag(modifier, beatmapObject, modifier.GetValue(10, variables));
                applyAnimationCache.startTime = reference.GetParentRuntime()?.CurrentTime ?? 0f;
                return applyAnimationCache;
            });

            if (!cache.from)
                return;

            var from = cache.from;
            var list = cache.to;
            var time = cache.startTime;

            var numberVariables = beatmapObject.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);
            var functions = beatmapObject.GetObjectFunctions();

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
                AnimationManager.inst.RemoveName("Apply Object Animation " + beatmapObject.id);

            for (int i = 0; i < list.Count; i++)
            {
                var bm = list[i];

                if (!modifier.constant)
                {
                    var animation = new RTAnimation("Apply Object Animation " + beatmapObject.id);
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

        public static void applyAnimationFromMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var cache = modifier.GetResultOrDefault(() =>
            {
                var applyAnimationCache = new ApplyAnimationCache();
                applyAnimationCache.from = GameData.Current.FindObjectWithTag(modifier, beatmapObject, modifier.GetValue(0, variables));
                applyAnimationCache.startTime = reference.GetParentRuntime()?.CurrentTime ?? 0f;
                return applyAnimationCache;
            });

            if (!cache.from)
                return;

            var from = cache.from;
            var time = cache.startTime;

            var numberVariables = beatmapObject.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);
            var functions = beatmapObject.GetObjectFunctions();

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
                AnimationManager.inst.RemoveName("Apply Object Animation " + beatmapObject.id);

                var animation = new RTAnimation("Apply Object Animation " + beatmapObject.id);
                animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, 0f, Ease.Linear),
                            new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                        }, x => ModifiersHelper.ApplyAnimationTo(beatmapObject, from, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
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

            ModifiersHelper.ApplyAnimationTo(beatmapObject, from, useVisual, time, timeOffset, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
        }

        public static void applyAnimationToMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject)
                return;

            var cache = modifier.GetResultOrDefault(() =>
            {
                var applyAnimationCache = new ApplyAnimationCache();
                applyAnimationCache.to = GameData.Current.FindObjectsWithTag(modifier, beatmapObject, modifier.GetValue(0, variables));
                applyAnimationCache.startTime = reference.GetParentRuntime()?.CurrentTime ?? 0f;
                return applyAnimationCache;
            });

            var list = cache.to;
            var time = cache.startTime;

            var numberVariables = beatmapObject.GetObjectVariables();
            ModifiersHelper.SetVariables(variables, numberVariables);
            var functions = beatmapObject.GetObjectFunctions();

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
                AnimationManager.inst.RemoveName("Apply Object Animation " + beatmapObject.id);

            for (int i = 0; i < list.Count; i++)
            {
                var bm = list[i];

                if (!modifier.constant)
                {
                    var animation = new RTAnimation("Apply Object Animation " + beatmapObject.id);
                    animation.animationHandlers = new List<AnimationHandlerBase>
                        {
                            new AnimationHandler<float>(new List<IKeyframe<float>>
                            {
                                new FloatKeyframe(0f, 0f, Ease.Linear),
                                new FloatKeyframe(Mathf.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                            }, x => ModifiersHelper.ApplyAnimationTo(bm, beatmapObject, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
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

                ModifiersHelper.ApplyAnimationTo(bm, beatmapObject, useVisual, time, timeOffset, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
            }
        }

        public static void copyAxis(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var transformable = reference.AsTransformable();
            if (transformable == null)
                return;
            var prefabable = reference.AsPrefabable();
            if (prefabable == null)
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

            var bm = modifier.GetResultOrDefault(() => GameData.Current.FindObjectWithTag(modifier, prefabable, modifier.GetValue(0)));
            if (!bm)
                return;

            var time = ModifiersHelper.GetTime(bm);

            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
            if (!useVisual)
                fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);

            if (toType < 0 || toType > 3)
                return;

            if (!useVisual && bm.cachedSequences)
            {
                var t = time - bm.StartTime - delay;
                if (fromType == 3)
                {
                    if (toType == 3 && toAxis == 0 && bm.cachedSequences.ColorSequence != null && reference is BeatmapObject beatmapObject && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject)
                        RTLevel.Current.postTick.Enqueue(() =>
                        {
                            var sequence = bm.cachedSequences.ColorSequence.GetValue(t);
                            var visualObject = beatmapObject.runtimeObject.visualObject;
                            visualObject.SetColor(RTMath.Lerp(visualObject.GetPrimaryColor(), sequence, multiply));
                        });
                    return;
                }
                transformable.SetTransform(toType, toAxis, fromType switch
                {
                    0 => Mathf.Clamp((bm.cachedSequences.PositionSequence.GetValue(t).At(fromAxis) - offset) * multiply % loop, min, max),
                    1 => Mathf.Clamp((bm.cachedSequences.ScaleSequence.GetValue(t).At(fromAxis) - offset) * multiply % loop, min, max),
                    2 => Mathf.Clamp((bm.cachedSequences.RotationSequence.GetValue(t) - offset) * multiply % loop, min, max),
                    _ => 0f,
                });
            }
            else if (useVisual && bm.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.gameObject)
                transformable.SetTransform(toType, toAxis, Mathf.Clamp((runtimeObject.visualObject.gameObject.transform.GetVector(fromType).At(fromAxis) - offset) * multiply % loop, min, max));
            else if (useVisual)
                transformable.SetTransform(toType, toAxis, Mathf.Clamp(fromType switch
                {
                    0 => bm.InterpolateChainPosition().At(fromAxis),
                    1 => bm.InterpolateChainScale().At(fromAxis),
                    2 => bm.InterpolateChainRotation(),
                    _ => 0f,
                }, min, max));
        }
        
        public static void copyAxisMath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var transformable = reference.AsTransformable();
            if (transformable == null)
                return;
            var prefabable = reference.AsPrefabable();
            if (prefabable == null)
                return;

            if (reference is not IEvaluatable evaluatable)
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

                if (!GameData.Current.TryFindObjectWithTag(modifier, prefabable, modifier.GetValue(0, variables), out BeatmapObject bm))
                    return;

                var time = ModifiersHelper.GetTime(bm);

                fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                if (!useVisual)
                    fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].values.Length);

                if (toType < 0 || toType > 3)
                    return;

                if (!useVisual && bm.cachedSequences)
                {
                    if (fromType == 3)
                    {
                        if (toType == 3 && toAxis == 0 && bm.cachedSequences.ColorSequence != null &&
                            reference is BeatmapObject beatmapObject && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject &&
                            beatmapObject.runtimeObject.visualObject.renderer)
                        {
                            // queue post tick so the color overrides the sequence color
                            RTLevel.Current.postTick.Enqueue(() =>
                            {
                                var sequence = bm.cachedSequences.ColorSequence.GetValue(time - bm.StartTime - delay);

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
                                0 => bm.cachedSequences.PositionSequence.GetValue(time - bm.StartTime - delay).At(fromAxis),
                                1 => bm.cachedSequences.ScaleSequence.GetValue(time - bm.StartTime - delay).At(fromAxis),
                                2 => bm.cachedSequences.RotationSequence.GetValue(time - bm.StartTime - delay),
                                _ => 0f,
                            };
                        bm.SetOtherObjectVariables(numberVariables);

                        float value = RTMath.Parse(evaluation, numberVariables);

                        transformable.SetTransform(toType, toAxis, Mathf.Clamp(value, min, max));
                    }
                }
                else if (useVisual && bm.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.gameObject)
                {
                    var axis = runtimeObject.visualObject.gameObject.transform.GetVector(fromType).At(fromAxis);

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
        
        public static void copyAxisGroup(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var transformable = reference.AsTransformable();
            if (transformable == null)
                return;
            var prefabable = reference.AsPrefabable();
            if (prefabable == null)
                return;

            if (reference is not IEvaluatable evaluatable)
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

                var time = reference.GetParentRuntime().CurrentTime;
                var numberVariables = evaluatable.GetObjectVariables();
                ModifiersHelper.SetVariables(variables, numberVariables);

                var list = modifier.GetResultOrDefault(() =>
                {
                    var result = new List<BeatmapObject>();

                    for (int i = 3; i < modifier.commands.Count; i += 8)
                    {
                        var group = modifier.GetValue(i + 1);

                        if (GameData.Current.TryFindObjectWithTag(modifier, prefabable, group, out BeatmapObject beatmapObject))
                            result.Add(beatmapObject);
                    }

                    return result;
                });

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
                            0 => Mathf.Clamp(beatmapObject.cachedSequences.PositionSequence.GetValue(time - beatmapObject.StartTime - delay).At(fromAxis), min, max),
                            1 => Mathf.Clamp(beatmapObject.cachedSequences.ScaleSequence.GetValue(time - beatmapObject.StartTime - delay).At(fromAxis), min, max),
                            2 => Mathf.Clamp(beatmapObject.cachedSequences.RotationSequence.GetValue(time - beatmapObject.StartTime - delay), min, max),
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
        
        public static void copyPlayerAxis(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var transformable = reference.AsTransformable();
            if (transformable == null)
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

            if (players.TryFind(x => x.RuntimePlayer && x.RuntimePlayer.rb, out PAPlayer player))
                transformable.SetTransform(toType, toAxis, Mathf.Clamp((player.RuntimePlayer.rb.transform.GetLocalVector(fromType).At(fromAxis) - offset) * multiply, min, max));
        }
        
        public static void legacyTail(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not BeatmapObject beatmapObject || modifier.commands.IsEmpty() || !GameData.Current)
                return;

            var totalTime = modifier.GetFloat(0, 200f, variables);

            var list = modifier.Result is List<LegacyTracker> ? (List<LegacyTracker>)modifier.Result : new List<LegacyTracker>();

            if (!modifier.HasResult())
            {
                list.Add(new LegacyTracker(beatmapObject, Vector3.zero, Vector3.zero, Quaternion.identity, 0f, 0f));

                for (int i = 1; i < modifier.commands.Count; i += 3)
                {
                    var group = GameData.Current.FindObjectsWithTag(modifier, beatmapObject, modifier.GetValue(i, variables));

                    if (modifier.commands.Count <= i + 2 || group.Count < 1)
                        break;

                    var distance = modifier.GetFloat(i + 1, 2f, variables);
                    var time = modifier.GetFloat(i + 2, 12f, variables);

                    for (int j = 0; j < group.Count; j++)
                    {
                        var tail = group[j];
                        list.Add(new LegacyTracker(tail, tail.positionOffset, tail.positionOffset, Quaternion.Euler(tail.rotationOffset), distance, time));
                    }
                }

                modifier.Result = list;
            }

            var animationResult = beatmapObject.InterpolateChain();
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

        public static void gravity(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not ITransformable transformable)
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

            var vector = modifier.GetResult<Vector2>();
            transformable.PositionOffset = RTMath.Rotate(vector, -transformable.GetFullRotation(false).z);
        }

        public static void gravityOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var transformables = GameData.Current.FindTransformables(modifier, prefabable, modifier.GetValue(0, variables));

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

            var vector = modifier.GetResult<Vector2>();
            foreach (var transformable in transformables)
                transformable.PositionOffset = RTMath.Rotate(vector, -transformable.GetFullRotation(false).z);
        }

        #endregion

        #region Prefab

        public static void spawnPrefab(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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
            RTLevel.Current.postTick.Enqueue(() =>
            {
                RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                runtimeLevel?.UpdatePrefab(prefabObject);
            });
        }

        public static void spawnPrefabOffset(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || modifier.HasResult() || reference is not ITransformable transformable)
                return;

            var prefab = ModifiersHelper.GetPrefab(modifier.GetInt(12, 0, variables), modifier.GetValue(0, variables));

            if (!prefab)
                return;

            var animationResult = transformable.GetObjectTransform();

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
            RTLevel.Current.postTick.Enqueue(() =>
            {
                RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                runtimeLevel?.UpdatePrefab(prefabObject);
            });
        }
        
        public static void spawnPrefabOffsetOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || modifier.HasResult() || reference is not IPrefabable prefabable)
                return;

            var prefab = ModifiersHelper.GetPrefab(modifier.GetInt(13, 0, variables), modifier.GetValue(0, variables));

            if (!prefab)
                return;

            if (!GameData.Current.TryFindTransformableWithTag(modifier, prefabable, modifier.GetValue(10, variables), out ITransformable target))
                return;

            var animationResult = target.GetObjectTransform();

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
            RTLevel.Current.postTick.Enqueue(() =>
            {
                RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                runtimeLevel?.UpdatePrefab(prefabObject);
            });
        }

        public static void spawnPrefabCopy(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || modifier.HasResult() || reference is not IPrefabable prefabable)
                return;

            var prefab = ModifiersHelper.GetPrefab(modifier.GetInt(4, 0, variables), modifier.GetValue(0, variables));

            if (!prefab)
                return;

            if (!GameData.Current.TryFindPrefabObjectWithTag(modifier, prefabable, modifier.GetValue(1), out PrefabObject orig))
                return;

            var prefabObject = new PrefabObject();
            prefabObject.id = LSText.randomString(16);
            prefabObject.prefabID = prefab.id;

            prefabObject.StartTime = modifier.GetBool(3, true, variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(2, 0f, variables) : modifier.GetFloat(2, 0f, variables);

            prefabObject.PasteInstanceData(orig);

            prefabObject.fromModifier = true;

            modifier.Result = prefabObject;
            GameData.Current.prefabObjects.Add(prefabObject);
            RTLevel.Current.postTick.Enqueue(() =>
            {
                RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                runtimeLevel?.UpdatePrefab(prefabObject);
            });
        }

        public static void spawnMultiPrefab(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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
            RTLevel.Current.postTick.Enqueue(() =>
            {
                RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                runtimeLevel?.UpdatePrefab(prefabObject);
            });
        }
        
        public static void spawnMultiPrefabOffset(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || reference is not ITransformable transformable)
                return;

            var prefab = ModifiersHelper.GetPrefab(modifier.GetInt(11, 0, variables), modifier.GetValue(0, variables));

            if (!prefab)
                return;

            var animationResult = transformable.GetObjectTransform();

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
            RTLevel.Current.postTick.Enqueue(() =>
            {
                RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                runtimeLevel?.UpdatePrefab(prefabObject);
            });
        }
        
        public static void spawnMultiPrefabOffsetOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || reference is not IPrefabable prefabable)
                return;

            var prefab = ModifiersHelper.GetPrefab(modifier.GetInt(12, 0, variables), modifier.GetValue(0, variables));

            if (!prefab)
                return;

            if (!GameData.Current.TryFindTransformableWithTag(modifier, prefabable, modifier.GetValue(9, variables), out ITransformable target))
                return;

            var animationResult = target.GetObjectTransform();

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
            RTLevel.Current.postTick.Enqueue(() =>
            {
                RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                runtimeLevel?.UpdatePrefab(prefabObject);
            });
        }

        public static void spawnMultiPrefabCopy(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || modifier.HasResult() || reference is not IPrefabable prefabable)
                return;

            var prefab = ModifiersHelper.GetPrefab(modifier.GetInt(4, 0, variables), modifier.GetValue(0, variables));

            if (!prefab)
                return;

            if (!GameData.Current.TryFindPrefabObjectWithTag(modifier, prefabable, modifier.GetValue(1), out PrefabObject orig))
                return;

            if (!modifier.HasResult())
                modifier.Result = new List<PrefabObject>();

            var list = modifier.GetResult<List<PrefabObject>>();
            var prefabObject = new PrefabObject();
            prefabObject.id = LSText.randomString(16);
            prefabObject.prefabID = prefab.id;

            prefabObject.StartTime = modifier.GetBool(3, true, variables) ? AudioManager.inst.CurrentAudioSource.time + modifier.GetFloat(2, 0f, variables) : modifier.GetFloat(2, 0f, variables);

            prefabObject.PasteInstanceData(orig);

            prefabObject.fromModifier = true;

            list.Add(prefabObject);
            modifier.Result = list;

            GameData.Current.prefabObjects.Add(prefabObject);
            RTLevel.Current.postTick.Enqueue(() =>
            {
                RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();
                runtimeLevel?.UpdatePrefab(prefabObject);
            });
        }

        public static void clearSpawnedPrefabs(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var modifyables = GameData.Current.FindModifyables(modifier, prefabable, modifier.GetValue(0, variables)).ToList();

            RTLevel.Current.postTick.Enqueue(() =>
            {
                RTLevelBase runtimeLevel = reference is PrefabObject p && p.runtimeObject ? p.runtimeObject : reference.GetParentRuntime();

                foreach (var modifyable in modifyables)
                {
                    for (int j = 0; j < modifyable.Modifiers.Count; j++)
                    {
                        var otherModifier = modifyable.Modifiers[j];

                        if (otherModifier.TryGetResult(out PrefabObject prefabObjectResult))
                        {
                            runtimeLevel?.UpdatePrefab(prefabObjectResult, false);

                            GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObjectResult.id);

                            otherModifier.Result = null;
                            continue;
                        }

                        if (!otherModifier.TryGetResult(out List<PrefabObject> result))
                            continue;

                        for (int k = 0; k < result.Count; k++)
                        {
                            var prefabObject = result[k];

                            runtimeLevel?.UpdatePrefab(prefabObject, false);
                            GameData.Current.prefabObjects.RemoveAll(x => x.fromModifier && x.id == prefabObject.id);
                        }

                        result.Clear();
                        otherModifier.Result = null;
                    }
                }
            });
        }

        public static void setPrefabTime(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is PrefabObject prefabObject && prefabObject.runtimeObject)
            {
                prefabObject.runtimeObject.CustomTime = modifier.GetFloat(0, 0f, variables);
                prefabObject.runtimeObject.UseCustomTime = modifier.GetBool(1, false, variables);
            }
        }

        #endregion

        #region Ranking

        public static void unlockAchievement(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!LevelManager.CurrentLevel)
                return;

            if (!LevelManager.CurrentLevel.saveData)
                LevelManager.AssignSaveData(LevelManager.CurrentLevel);
            LevelManager.CurrentLevel.saveData.UnlockAchievement(modifier.GetValue(0, variables));
        }
        
        public static void lockAchievement(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!LevelManager.CurrentLevel)
                return;

            if (!LevelManager.CurrentLevel.saveData)
                LevelManager.AssignSaveData(LevelManager.CurrentLevel);
            LevelManager.CurrentLevel.saveData.LockAchievement(modifier.GetValue(0, variables));
        }

        public static void getAchievementUnlocked(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!LevelManager.CurrentLevel)
                return;

            if (!LevelManager.CurrentLevel.saveData)
                LevelManager.AssignSaveData(LevelManager.CurrentLevel);
            variables[modifier.GetValue(0)] = LevelManager.CurrentLevel.saveData.AchievementUnlocked(modifier.GetValue(1, variables)).ToString();
        }

        public static void saveLevelRank(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor || modifier.constant || !LevelManager.CurrentLevel)
                return;

            LevelManager.UpdateCurrentLevelProgress();
        }
        
        public static void clearHits(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            RTBeatmap.Current.hits.Clear();
        }
        
        public static void addHit(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var vector = Vector3.zero;
            if (reference is BeatmapObject beatmapObject)
            {
                if (modifier.GetBool(0, true, variables))
                    vector = beatmapObject.GetFullPosition();
                else
                {
                    var player = PlayerManager.GetClosestPlayer(beatmapObject.GetFullPosition());
                    if (player && player.RuntimePlayer)
                        vector = player.RuntimePlayer.rb.position;
                }
            }

            var timeValue = modifier.GetValue(1, variables);
            float time = AudioManager.inst.CurrentAudioSource.time;
            if (!string.IsNullOrEmpty(timeValue) && reference is IEvaluatable evaluatable)
            {
                var numberVariables = evaluatable.GetObjectVariables();
                ModifiersHelper.SetVariables(variables, numberVariables);

                time = RTMath.Parse(timeValue, numberVariables, evaluatable.GetObjectFunctions());
            }

            RTBeatmap.Current.hits.Add(new PlayerDataPoint(vector, time));
        }
        
        public static void subHit(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!RTBeatmap.Current.hits.IsEmpty())
                RTBeatmap.Current.hits.RemoveAt(RTBeatmap.Current.hits.Count - 1);
        }
        
        public static void clearDeaths(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            RTBeatmap.Current.deaths.Clear();
        }
        
        public static void addDeath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var vector = Vector3.zero;
            if (reference is BeatmapObject beatmapObject)
            {
                if (modifier.GetBool(0, true, variables))
                    vector = beatmapObject.GetFullPosition();
                else
                {
                    var player = PlayerManager.GetClosestPlayer(beatmapObject.GetFullPosition());
                    if (player && player.RuntimePlayer)
                        vector = player.RuntimePlayer.rb.position;
                }
            }

            var timeValue = modifier.GetValue(1, variables);
            float time = AudioManager.inst.CurrentAudioSource.time;
            if (!string.IsNullOrEmpty(timeValue) && reference is IEvaluatable evaluatable)
            {
                var numberVariables = evaluatable.GetObjectVariables();
                ModifiersHelper.SetVariables(variables, numberVariables);

                time = RTMath.Parse(timeValue, numberVariables, evaluatable.GetObjectFunctions());
            }

            RTBeatmap.Current.deaths.Add(new PlayerDataPoint(vector, time));
        }
        
        public static void subDeath(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!RTBeatmap.Current.deaths.IsEmpty())
                RTBeatmap.Current.deaths.RemoveAt(RTBeatmap.Current.deaths.Count - 1);
        }

        public static void getHitCount(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = RTBeatmap.Current.hits.Count.ToString();
        }
        
        public static void getDeathCount(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = RTBeatmap.Current.deaths.Count.ToString();
        }

        #endregion

        #region Updates

        public static void updateObjects(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!modifier.constant)
                CoroutineHelper.StartCoroutine(RTLevel.IReinit());
        }
        
        public static void updateObject(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || reference is not IPrefabable prefabable)
                return;

            var prefabables = GameData.Current.FindPrefabablesWithTag(modifier, prefabable, modifier.GetValue(0));

            if (prefabables.IsEmpty())
                return;

            foreach (var other in prefabables)
            {
                if (other is BeatmapObject beatmapObject)
                    beatmapObject.GetParentRuntime()?.UpdateObject(beatmapObject, recalculate: false);
                if (other is BackgroundObject backgroundObject)
                    backgroundObject.GetParentRuntime()?.UpdateBackgroundObject(backgroundObject, recalculate: false);
                if (other is PrefabObject prefabObject)
                    prefabObject.GetParentRuntime()?.UpdatePrefab(prefabObject, recalculate: false);
            }
            RTLevel.Current?.RecalculateObjectStates();
        }
        
        public static void setParent(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || reference is not IPrefabable prefabable || reference is not IParentable child)
                return;

            var group = modifier.GetValue(0, variables);
            if (group == string.Empty)
                ModifiersHelper.SetParent(child, string.Empty);
            else if (GameData.Current.TryFindObjectWithTag(modifier, prefabable, group, out BeatmapObject target) && child.CanParent(target))
                ModifiersHelper.SetParent(child, target);
            else
                CoreHelper.LogError($"CANNOT PARENT OBJECT!\nID: {child.ID}");
        }
        
        public static void setParentOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || reference is not IPrefabable prefabable || reference is not BeatmapObject parent)
                return;

            var group = modifier.GetValue(2, variables);

            BeatmapObject target = null;
            if (!string.IsNullOrEmpty(group) && GameData.Current.TryFindObjectWithTag(modifier, prefabable, group, out BeatmapObject targetAAA))
                target = targetAAA;
            if (target == null)
                target = parent;

            var parentables = GameData.Current.FindParentables(modifier, prefabable, modifier.GetValue(0, variables));

            var isEmpty = modifier.GetBool(1, false, variables);

            bool failed = false;
            foreach (var parentable in parentables)
            {
                if (isEmpty)
                    ModifiersHelper.SetParent(parentable, string.Empty);
                else if (parentable.CanParent(target))
                    ModifiersHelper.SetParent(parentable, target);
                else
                    failed = true;
            }

            if (failed)
                CoreHelper.LogError($"CANNOT PARENT OBJECT!\nID: {parent.ID}");
        }
        
        public static void detachParent(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || reference is not IParentable parentable)
                return;

            parentable.ParentDetatched = modifier.GetBool(0, true, variables);

            if (reference is not PrefabObject prefabObject || !prefabObject.runtimeObject)
                return;

            foreach (var beatmapObject in prefabObject.runtimeObject.Spawner.BeatmapObjects)
                beatmapObject.detatched = prefabObject.detatched;
        }
        
        public static void detachParentOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (modifier.constant || reference is not IPrefabable prefabable)
                return;

            var parentables = GameData.Current.FindParentables(modifier, prefabable, modifier.GetValue(1));
            var detach = modifier.GetBool(0, true);

            foreach (var other in parentables)
            {
                other.ParentDetatched = detach;

                if (other is not PrefabObject prefabObject || !prefabObject.runtimeObject)
                    continue;

                foreach (var beatmapObject in prefabObject.runtimeObject.Spawner.BeatmapObjects)
                    beatmapObject.detatched = prefabObject.detatched;
            }
        }

        public static void setSeed(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (!modifier.constant)
                RTLevel.Current?.InitSeed(modifier.GetValue(0, variables));
        }

        #endregion

        #region Physics

        public static void setCollision(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is BeatmapObject beatmapObject && beatmapObject.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.collider)
                runtimeObject.visualObject.colliderEnabled = modifier.GetBool(0, false, variables);
        }

        public static void setCollisionOther(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var colliderEnabled = modifier.GetBool(0, false, variables);
            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));

            foreach (var beatmapObject in list)
            {
                if (beatmapObject.runtimeObject is RTBeatmapObject runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.collider)
                    runtimeObject.visualObject.colliderEnabled = colliderEnabled;
            }
        }

        #endregion

        #region Checkpoints

        public static void createCheckpoint(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            // if active checpoints matches the stored checkpoint, do not create a new checkpoint.
            if (modifier.TryGetResult(out Checkpoint prevCheckpoint) && prevCheckpoint.id == RTBeatmap.Current.ActiveCheckpoint.id)
                return;

            var checkpoint = new Checkpoint();
            checkpoint.time = modifier.GetBool(1, true, variables) ? reference.GetParentRuntime().FixedTime + modifier.GetFloat(0, 0f, variables) : modifier.GetFloat(0, 0f, variables);
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

        public static void resetCheckpoint(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            RTBeatmap.Current.ResetCheckpoint(modifier.GetBool(0, false, variables));
        }

        #endregion

        #region Interfaces

        public static void loadInterface(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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

            Dictionary<string, JSONNode> customVariables = null;
            if (modifier.GetBool(2, false, variables))
            {
                customVariables = new Dictionary<string, JSONNode>();
                foreach (var variable in variables)
                    customVariables[variable.Key] = variable.Value;
            }

            InterfaceManager.inst.ParseInterface(path, customVariables: customVariables);

            InterfaceManager.inst.MainDirectory = RTFile.BasePath;

            if (modifier.GetBool(1, true, variables))
                RTBeatmap.Current.Pause();
            ArcadeHelper.endedLevel = false;
        }

        public static void exitInterface(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            InterfaceManager.inst.CloseMenus();
            if (CoreHelper.Paused)
                RTBeatmap.Current.Resume();
        }

        public static void pauseLevel(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor)
            {
                EditorManager.inst.DisplayNotification("Cannot pause in the editor. This modifier only works in the Arcade.", 3f, EditorManager.NotificationType.Warning);
                return;
            }

            PauseMenu.Pause();
        }

        public static void quitToMenu(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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

        public static void quitToArcade(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
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

        public static void setBGActive(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var active = modifier.GetBool(0, false, variables);
            var tag = modifier.GetValue(1, variables);
            var list = GameData.Current.backgroundObjects.FindAll(x => x.tags.Contains(tag));
            if (!list.IsEmpty())
                for (int i = 0; i < list.Count; i++)
                    list[i].Enabled = active;
        }

        public static void signalModifier(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, variables));
            var delay = modifier.GetFloat(0, 0f, variables);

            foreach (var bm in list)
                CoroutineHelper.StartCoroutine(ModifiersHelper.ActivateModifier(bm, delay));
        }
        
        public static void activateModifier(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not IPrefabable prefabable)
                return;

            var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(0, variables));

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
                    var modifiers = list[i].modifiers.FindAll(x => x.type == Modifier.Type.Action && modifierNames.Contains(x.Name));

                    for (int j = 0; j < modifiers.Count; j++)
                    {
                        var otherModifier = modifiers[i];
                        otherModifier.Action?.Invoke(otherModifier, list[i], variables);
                    }
                    continue;
                }

                if (index >= 0 && index < list[i].modifiers.Count)
                {
                    var otherModifier = list[i].modifiers[index];
                    otherModifier.Action?.Invoke(otherModifier, list[i], variables);
                }
            }
        }
        
        public static void editorNotify(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InEditor)
                EditorManager.inst.DisplayNotification(
                    /*text: */ modifier.GetValue(0, variables),
                    /*time: */ modifier.GetFloat(1, 0.5f, variables),
                    /*type: */ (EditorManager.NotificationType)modifier.GetInt(2, 0, variables));
        }
        
        public static void setWindowTitle(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables) => WindowController.SetTitle(modifier.GetValue(0, variables));

        public static void setDiscordStatus(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var discordSubIcons = CoreHelper.discordSubIcons;
            var discordIcons = CoreHelper.discordIcons;

            if (int.TryParse(modifier.commands[2], out int discordSubIcon) && int.TryParse(modifier.commands[3], out int discordIcon))
                CoreHelper.UpdateDiscordStatus(
                    string.Format(modifier.value, MetaData.Current.song.title, $"{(!CoreHelper.InEditor ? "Game" : "Editor")}", $"{(!CoreHelper.InEditor ? "Level" : "Editing")}", $"{(!CoreHelper.InEditor ? "Arcade" : "Editor")}"),
                    string.Format(modifier.commands[1], MetaData.Current.song.title, $"{(!CoreHelper.InEditor ? "Game" : "Editor")}", $"{(!CoreHelper.InEditor ? "Level" : "Editing")}", $"{(!CoreHelper.InEditor ? "Arcade" : "Editor")}"),
                    discordSubIcons[Mathf.Clamp(discordSubIcon, 0, discordSubIcons.Length - 1)], discordIcons[Mathf.Clamp(discordIcon, 0, discordIcons.Length - 1)]);
        }

        public static void callModifierBlock(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var name = modifier.GetValue(0, variables);
            if (GameData.Current.modifierBlocks.TryFind(x => x.Name == name, out ModifierBlock<IModifierReference> modifierBlock))
                modifierBlock.Run(reference, variables);
        }

        #endregion

        #region Player Only

        public static void setCustomObjectActive(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(1, variables);
            var player = reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;

            if (player && player.RuntimePlayer && player.RuntimePlayer.customObjects.TryFind(x => x.id == id, out RTPlayer.RTCustomPlayerObject customObject))
                customObject.active = modifier.GetBool(0, false, variables);
        }

        public static void setIdleAnimation(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(0, variables);
            var referenceID = modifier.GetValue(1, variables);
            var player = reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;

            if (player && player.RuntimePlayer && player.RuntimePlayer.customObjects.TryFind(x => x.id == id, out RTPlayer.RTCustomPlayerObject customObject) && customObject.reference && customObject.reference.animations.TryFind(x => x.ReferenceID == referenceID, out PAAnimation animation))
                customObject.currentIdleAnimation = animation.ReferenceID;
        }

        public static void playAnimation(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var id = modifier.GetValue(0, variables);
            var referenceID = modifier.GetValue(1, variables);
            var player = reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;

            if (player && player.RuntimePlayer && player.RuntimePlayer.customObjects.TryFind(x => x.id == id, out RTPlayer.RTCustomPlayerObject customObject) && customObject.reference && customObject.reference.animations.TryFind(x => x.ReferenceID == referenceID, out PAAnimation animation))
            {
                var runtimeAnimation = new RTAnimation("Custom Animation");
                player.RuntimePlayer.ApplyAnimation(runtimeAnimation, animation, customObject);
                player.RuntimePlayer.animationController.Play(runtimeAnimation);
            }
        }

        public static void kill(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is PAPlayer player)
                player.Health = 0;
        }

        public static void hit(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is not PAPlayer player)
                return;

            var damage = modifier.GetInt(0, 0, variables);
            if (damage <= 1)
                player.RuntimePlayer?.Hit();
            else
                player.RuntimePlayer?.Hit(damage);
        }

        public static void boost(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is PAPlayer player)
                player.RuntimePlayer?.Boost();
        }

        public static void shoot(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is PAPlayer player)
                player.RuntimePlayer?.Shoot();
        }

        public static void pulse(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is PAPlayer player)
                player.RuntimePlayer?.Pulse();
        }

        public static void jump(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (reference is PAPlayer player)
                player.RuntimePlayer?.Jump();
        }

        public static void getHealth(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var player = reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;
            if (!player)
                return;

            variables[modifier.GetValue(0)] = player.Health.ToString();
        }

        public static void getLives(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var player = reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;
            if (!player)
                return;

            variables[modifier.GetValue(0)] = player.lives.ToString();
        }

        public static void getMaxHealth(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var player = reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;
            if (!player)
                return;

            variables[modifier.GetValue(0)] = player.GetMaxHealth().ToString();
        }

        public static void getMaxLives(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var player = reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;
            if (!player)
                return;

            variables[modifier.GetValue(0)] = player.GetMaxLives().ToString();
        }

        public static void getIndex(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var player = reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;
            if (!player)
                return;

            variables[modifier.GetValue(0)] = player.index.ToString();
        }

        public static void getMove(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var player = reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;
            if (!player)
                return;

            var move = player.RuntimePlayer.Actions.Move.Vector;
            if (move.magnitude > 1f && modifier.GetBool(2, true, variables))
                move = move.normalized;
            variables[modifier.GetValue(0)] = move.x.ToString();
            variables[modifier.GetValue(1)] = move.y.ToString();
        }
        
        public static void getMoveX(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var player = reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;
            if (!player)
                return;

            var move = player.RuntimePlayer.Actions.Move.Vector;
            if (move.magnitude > 1f && modifier.GetBool(1, true, variables))
                move = move.normalized;
            variables[modifier.GetValue(0)] = move.x.ToString();
        }
        
        public static void getMoveY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            var player = reference is RTPlayer.RTCustomPlayerObject customPlayerObject ? customPlayerObject.Player.Core : reference as PAPlayer;
            if (!player)
                return;

            var move = player.RuntimePlayer.Actions.Move.Vector;
            if (move.magnitude > 1f && modifier.GetBool(1, true, variables))
                move = move.normalized;
            variables[modifier.GetValue(0)] = move.y.ToString();
        }

        #endregion

        #region DEVONLY

        public static void loadSceneDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InStory)
                SceneManager.inst.LoadScene(modifier.GetValue(0, variables), modifier.commands.Count > 1 && modifier.GetBool(1, true, variables));
        }
        
        public static void loadStoryLevelDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InStory)
                Story.StoryManager.inst.Play(modifier.GetInt(1, 0, variables), modifier.GetInt(2, 0, variables), modifier.GetInt(4, 0, variables), modifier.GetBool(0, false, variables), modifier.GetBool(3, false, variables));
        }
        
        public static void storySaveBoolDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InStory)
                Story.StoryManager.inst.CurrentSave.SaveBool(modifier.GetValue(0, variables), modifier.GetBool(1, false, variables));
        }

        public static void storySaveIntDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InStory)
                Story.StoryManager.inst.CurrentSave.SaveInt(modifier.GetValue(0, variables), modifier.GetInt(1, 0, variables));
        }

        public static void storySaveFloatDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InStory)
                Story.StoryManager.inst.CurrentSave.SaveFloat(modifier.GetValue(0, variables), modifier.GetFloat(1, 0f, variables));
        }

        public static void storySaveStringDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InStory)
                Story.StoryManager.inst.CurrentSave.SaveString(modifier.GetValue(0, variables), modifier.GetValue(1, variables));
        }

        public static void storySaveIntVariableDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (CoreHelper.InStory && reference is IModifyable modifyable)
                Story.StoryManager.inst.CurrentSave.SaveInt(modifier.GetValue(0, variables), modifyable.IntVariable);
        }

        public static void getStorySaveBoolDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = !CoreHelper.InStory ? modifier.GetBool(2, false, variables).ToString() : Story.StoryManager.inst.CurrentSave.LoadBool(modifier.GetValue(1, variables), modifier.GetBool(2, false, variables)).ToString();
        }
        
        public static void getStorySaveIntDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = !CoreHelper.InStory ? modifier.GetInt(2, 0, variables).ToString() : Story.StoryManager.inst.CurrentSave.LoadInt(modifier.GetValue(1, variables), modifier.GetInt(2, 0, variables)).ToString();
        }
        
        public static void getStorySaveFloatDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = !CoreHelper.InStory ? modifier.GetFloat(2, 0f, variables).ToString() : Story.StoryManager.inst.CurrentSave.LoadFloat(modifier.GetValue(1, variables), modifier.GetFloat(2, 0f, variables)).ToString();
        }
        
        public static void getStorySaveStringDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            variables[modifier.GetValue(0)] = !CoreHelper.InStory ? modifier.GetValue(2, variables) : Story.StoryManager.inst.CurrentSave.LoadString(modifier.GetValue(1, variables), modifier.GetValue(2, variables)).ToString();
        }

        public static void exampleEnableDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (Companion.Entity.Example.Current && Companion.Entity.Example.Current.model)
                Companion.Entity.Example.Current.model.SetActive(modifier.GetBool(0, false, variables));
        }
        
        public static void exampleSayDEVONLY(Modifier modifier, IModifierReference reference, Dictionary<string, string> variables)
        {
            if (Companion.Entity.Example.Current && Companion.Entity.Example.Current.chatBubble)
                Companion.Entity.Example.Current.chatBubble.Say(modifier.GetValue(0, variables));
        }

        #endregion
    }
}

#pragma warning restore IDE1006 // Naming Styles