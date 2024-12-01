using BetterLegacy.Components.Player;
using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Networking;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

using BasePrefab = DataManager.GameData.Prefab;

namespace BetterLegacy.Core.Managers
{
    public class ModifiersManager : MonoBehaviour
    {
        static System.Diagnostics.Stopwatch sw;

        /// <summary>
        /// Updates modifiers on level tick.
        /// </summary>
        public static void OnLevelTick()
        {
            if (!GameData.IsValid || !CoreHelper.Playing)
                return;

            if (Input.GetKeyDown(KeyCode.Alpha1))
                sw = CoreHelper.StartNewStopwatch();

            var ldm = CoreConfig.Instance.LDM.Value;
            var beatmapObjects = GameData.Current.beatmapObjects;

            for (int i = 0; i < beatmapObjects.Count; i++)
            {
                var beatmapObject = beatmapObjects[i];

                if (beatmapObject.modifiers.Count <= 0 || ldm && beatmapObject.LDM)
                    continue;

                if (beatmapObject.orderModifiers)
                {
                    ModifiersHelper.RunModifiersLoop(beatmapObject.modifiers, beatmapObject.ignoreLifespan || beatmapObject.Alive);
                    continue;
                }

                ModifiersHelper.RunModifiersAll(beatmapObject.modifiers, beatmapObject.ignoreLifespan || beatmapObject.Alive);
            }

            foreach (var audioSource in audioSources)
            {
                try
                {
                    if (GameData.Current.beatmapObjects.Find(x => x.id == audioSource.Key) == null || !GameData.Current.beatmapObjects.Find(x => x.id == audioSource.Key).Alive)
                        queuedAudioToDelete.Add(audioSource);
                }
                catch
                {

                }
            }

            if (queuedAudioToDelete.Count > 0)
            {
                foreach (var audio in queuedAudioToDelete)
                    DeleteKey(audio.Key, audio.Value);
                queuedAudioToDelete.Clear();
            }

            if (sw != null)
            {
                CoreHelper.StopAndLogStopwatch(sw, "ModifiersManager");
                sw = null;
            }
        }

        public static List<KeyValuePair<string, AudioSource>> queuedAudioToDelete = new List<KeyValuePair<string, AudioSource>>();

        /// <summary>
        /// Inits ModifiersManager.
        /// </summary>
        public static void Init() => Creator.NewGameObject(nameof(ModifiersManager), SystemManager.inst.transform).AddComponent<ModifiersManager>();

        public void ToggleDevelopment()
        {
            ModifiersHelper.development = !ModifiersHelper.development;

            if (!ModifiersHelper.development)
                defaultBeatmapObjectModifiers.RemoveAll(x => x.Name.Contains("DEVONLY"));
            else
                AddDevelopmentModifiers();
        }

        void Awake()
        {
            defaultBeatmapObjectModifiers.Clear();

            var path = RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "default_modifiers.lsb";

            if (!RTFile.FileExists(path))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(path));

            for (int i = 0; i < jn["modifiers"].Count; i++)
                defaultBeatmapObjectModifiers.Add(Modifier<BeatmapObject>.Parse(jn["modifiers"][i]));

            if (ModifiersHelper.development)
                AddDevelopmentModifiers();
        }

        void AddDevelopmentModifiers()
        {
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string> { "loadSceneDEVONLY", "False" },
                value = "Interface"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "loadStoryLevelDEVONLY",
                    "0", // chaoter
                    "0", // level
                    "False", // skip cutscenes
                },
                value = "False" // bonus chapter
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "storySaveIntVariableDEVONLY",
                },
                value = "IntVariable"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "storySaveIntDEVONLY",
                    "0",
                },
                value = "IntVariable"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "storySaveBoolDEVONLY",
                    "True",
                },
                value = "BoolVariable"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "storySaveBoolDEVONLY",
                    "True",
                },
                value = "BoolVariable"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Trigger,
                constant = false,
                commands = new List<string>
                {
                    "storyLoadIntEqualsDEVONLY",
                    "0", // Default
                    "0", // Equals
                },
                value = "IntVariable"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Trigger,
                constant = false,
                commands = new List<string>
                {
                    "storyLoadIntLesserEqualsDEVONLY",
                    "0", // Default
                    "0", // Equals
                },
                value = "IntVariable"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Trigger,
                constant = false,
                commands = new List<string>
                {
                    "storyLoadIntGreaterEqualsDEVONLY",
                    "0", // Default
                    "0", // Equals
                },
                value = "IntVariable"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Trigger,
                constant = false,
                commands = new List<string>
                {
                    "storyLoadIntLesserDEVONLY",
                    "0", // Default
                    "0", // Equals
                },
                value = "IntVariable"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Trigger,
                constant = false,
                commands = new List<string>
                {
                    "storyLoadIntGreaterDEVONLY",
                    "0", // Default
                    "0", // Equals
                },
                value = "IntVariable"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Trigger,
                constant = false,
                commands = new List<string>
                {
                    "storyLoadBoolDEVONLY",
                    "False", // Default
                },
                value = "BoolVariable"
            });
            defaultBeatmapObjectModifiers.Add(new Modifier<BeatmapObject>
            {
                type = ModifierBase.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "enableExampleDEVONLY"
                },
                value = "False"
            });
        }

        public static void DeleteKey(string id, AudioSource audioSource)
        {
            Destroy(audioSource);
            audioSources.Remove(id);
        }

        #region Modifier Functions

        public static void GetSoundPath(string id, string path, bool fromSoundLibrary = false, float pitch = 1f, float volume = 1f, bool loop = false)
        {
            string text = RTFile.ApplicationDirectory + "beatmaps/soundlibrary/" + path;

            if (!fromSoundLibrary)
                text = RTFile.BasePath + path;

            if (!path.Contains(".ogg") && RTFile.FileExists(text + ".ogg"))
                text += ".ogg";

            if (!path.Contains(".wav") && RTFile.FileExists(text + ".wav"))
                text += ".wav";

            if (!path.Contains(".mp3") && RTFile.FileExists(text + ".mp3"))
                text += ".mp3";

            if (RTFile.FileExists(text))
            {
                if (!text.Contains(".mp3"))
                    CoreHelper.StartCoroutine(LoadMusicFileRaw(text, delegate (AudioClip _newSound)
                    {
                        _newSound.name = path;
                        PlaySound(id, _newSound, pitch, volume, loop);
                    }));
                else
                    PlaySound(id, LSAudio.CreateAudioClipUsingMP3File(text), pitch, volume, loop);
            }
        }

        public static void DownloadSoundAndPlay(string id, string path, float pitch = 1f, float volume = 1f, bool loop = false)
        {
            try
            {
                var audioType = RTFile.GetAudioType(path);

                if (audioType != AudioType.UNKNOWN)
                    CoreHelper.StartCoroutine(AlephNetworkManager.DownloadAudioClip(path, audioType, delegate (AudioClip audioClip)
                    {
                        PlaySound(id, audioClip, pitch, volume, loop);
                    }, delegate (string onError)
                    {
                        CoreHelper.Log($"Error! Could not download audioclip.\n{onError}");
                    }));
            }
            catch
            {

            }
        }

        public static void PlaySound(string id, AudioClip clip, float pitch, float volume, bool loop)
        {
            var audioSource = Camera.main.gameObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.playOnAwake = true;
            audioSource.loop = loop;
            audioSource.pitch = pitch * AudioManager.inst.CurrentAudioSource.pitch;
            audioSource.volume = volume * AudioManager.inst.sfxVol;
            audioSource.Play();

            float x = pitch * AudioManager.inst.CurrentAudioSource.pitch;
            if (x == 0f)
                x = 1f;
            if (x < 0f)
                x = -x;

            if (!loop)
                CoreHelper.StartCoroutine(AudioManager.inst.DestroyWithDelay(audioSource, clip.length / x));
            else if (!audioSources.ContainsKey(id))
                audioSources.Add(id, audioSource);
        }

        public static IEnumerator LoadMusicFileRaw(string path, Action<AudioClip> callback)
        {
            if (!RTFile.FileExists(path))
            {
                CoreHelper.Log($"Could not load Music file [{path}]");
            }
            else
            {
                var www = new WWW("file://" + path);
                while (!www.isDone)
                    yield return null;

                var beatmapAudio = www.GetAudioClip(false, false);
                while (beatmapAudio.loadState != AudioDataLoadState.Loaded)
                    yield return null;
                callback?.Invoke(beatmapAudio);
                beatmapAudio = null;
                www = null;
            }
            yield break;
        }

        public static Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();

        public static PrefabObject AddPrefabObjectToLevel(BasePrefab prefab, float startTime, Vector2 pos, Vector2 sca, float rot, int repeatCount, float repeatOffsetTime, float speed)
        {
            var prefabObject = new PrefabObject();
            prefabObject.ID = LSText.randomString(16);
            prefabObject.prefabID = prefab.ID;

            prefabObject.StartTime = startTime;

            prefabObject.events[0].eventValues[0] = pos.x;
            prefabObject.events[0].eventValues[1] = pos.y;
            prefabObject.events[1].eventValues[0] = sca.x;
            prefabObject.events[1].eventValues[1] = sca.y;
            prefabObject.events[2].eventValues[0] = rot;

            prefabObject.RepeatCount = repeatCount;
            prefabObject.RepeatOffsetTime = repeatOffsetTime;
            prefabObject.speed = speed;

            prefabObject.fromModifier = true;

            return prefabObject;
        }

        public static void SaveProgress(string path, string chapter, string level, float data)
        {
            if (path.Contains("\\") || path.Contains("/") || path.Contains(".."))
                return;

            if (!RTFile.DirectoryExists($"{RTFile.ApplicationDirectory}profile"))
                Directory.CreateDirectory($"{RTFile.ApplicationDirectory}profile");

            var jn = JSON.Parse(RTFile.FileExists($"{RTFile.ApplicationDirectory}profile/{path}.ses") ? RTFile.ReadFromFile($"{RTFile.ApplicationDirectory}profile/{path}.ses") : "{}");

            jn[chapter][level]["float"] = data.ToString();

            RTFile.WriteToFile($"{RTFile.ApplicationDirectory}profile/{path}.ses", jn.ToString(3));
        }

        public static void SaveProgress(string path, string chapter, string level, string data)
        {
            if (path.Contains("\\") || path.Contains("/") || path.Contains(".."))
                return;

            if (!RTFile.DirectoryExists($"{RTFile.ApplicationDirectory}profile"))
                Directory.CreateDirectory($"{RTFile.ApplicationDirectory}profile");

            var jn = JSON.Parse(RTFile.FileExists($"{RTFile.ApplicationDirectory}profile/{path}.ses") ? RTFile.ReadFromFile($"{RTFile.ApplicationDirectory}profile/{path}.ses") : "{}");

            jn[chapter][level]["string"] = data.ToString();

            RTFile.WriteToFile($"{RTFile.ApplicationDirectory}profile/{path}.ses", jn.ToString(3));
        }

        public static IEnumerator ActivateModifier(BeatmapObject beatmapObject, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (beatmapObject.modifiers.TryFind(x => x.commands[0] == "requireSignal" && x.type == ModifierBase.Type.Trigger, out Modifier<BeatmapObject> modifier))
            {
                modifier.Result = "death hd";
            }
        }

        #endregion

        public static List<Modifier<BeatmapObject>> defaultBeatmapObjectModifiers = new List<Modifier<BeatmapObject>>();

        public static List<Modifier<BackgroundObject>> defaultBackgroundObjectModifiers = new List<Modifier<BackgroundObject>>
        {
            new Modifier<BackgroundObject>
            {
                type = ModifierBase.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "setActive"
                },
                value = "False"
            }, //setActive
            new Modifier<BackgroundObject>
            {
                type = ModifierBase.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "setActiveOther",
                    "BG Group",
                },
                value = "False"
            }, //setOtherActive
            new Modifier<BackgroundObject>
            {
                type = ModifierBase.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "animateObject",
                    "0", // Pos / Sca / Rot
                    "0", // X
                    "0", // Y
                    "0", // Z
                    "True", // Relative
                    "0", // Easing
                },
                value = "1"
            }, //animateObject
            new Modifier<BackgroundObject>
            {
                type = ModifierBase.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "animateObjectOther",
                    "0", // Pos / Sca / Rot
                    "0", // X
                    "0", // Y
                    "0", // Z
                    "True", // Relative
                    "0", // Easing
                    "BG Group", // BG Group
                },
                value = "1"
            }, //animateObjectOther
            new Modifier<BackgroundObject>
            {
                type = ModifierBase.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "copyAxis",
                    "0", // From Type
                    "0", // From Axis
                    "0", // To Type
                    "0", // To Axis
                    "0", // Delay
                    "1", // Multiply
                    "0", // Offset
                    "-99999", // Min
                    "99999", // Max
                    "99999", // Loop
                },
                value = "Object Group"
            }, //copyAxis
            new Modifier<BackgroundObject>
            {
                type = ModifierBase.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "timeLesserEquals"
                },
                value = "0"
            }, //timeLesserEquals
            new Modifier<BackgroundObject>
            {
                type = ModifierBase.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "timeGreaterEquals"
                },
                value = "0"
            }, //timeGreaterEquals
            new Modifier<BackgroundObject>
            {
                type = ModifierBase.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "timeLesser"
                },
                value = "0"
            }, //timeLesser
            new Modifier<BackgroundObject>
            {
                type = ModifierBase.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "timeGreater"
                },
                value = "0"
            }, //timeGreater
        };

        public static List<Modifier<CustomPlayer>> defaultPlayerModifiers = new List<Modifier<CustomPlayer>>
        {
            new Modifier<CustomPlayer>
            {
                type = ModifierBase.Type.Action,
                constant = true,
                commands = new List<string> { "setCustomActive", "0", "True" },
                value = "False"
            },
            new Modifier<CustomPlayer>
            {
                type = ModifierBase.Type.Action,
                constant = true,
                commands = new List<string> { "kill" },
                value = "0"
            },
            new Modifier<CustomPlayer>
            {
                type = ModifierBase.Type.Action,
                constant = true,
                commands = new List<string> { "hit" },
                value = "0"
            },
            new Modifier<CustomPlayer>
            {
                type = ModifierBase.Type.Action,
                constant = true,
                commands = new List<string> { "signalModifier", "Object Group" },
                value = "0"
            },
            new Modifier<CustomPlayer>
            {
                type = ModifierBase.Type.Trigger,
                constant = true,
                commands = new List<string> { "actionPress" },
                value = "0"
            },
            new Modifier<CustomPlayer>
            {
                type = ModifierBase.Type.Trigger,
                constant = true,
                commands = new List<string> { "actionDown" },
                value = "0"
            },
            new Modifier<CustomPlayer>
            {
                type = ModifierBase.Type.Trigger,
                constant = true,
                commands = new List<string> { "healthEquals" },
                value = "0"
            },
            new Modifier<CustomPlayer>
            {
                type = ModifierBase.Type.Trigger,
                constant = true,
                commands = new List<string> { "healthGreaterEquals" },
                value = "0"
            },
            new Modifier<CustomPlayer>
            {
                type = ModifierBase.Type.Trigger,
                constant = true,
                commands = new List<string> { "healthLesserEquals" },
                value = "0"
            },
            new Modifier<CustomPlayer>
            {
                type = ModifierBase.Type.Trigger,
                constant = true,
                commands = new List<string> { "healthGreater" },
                value = "0"
            },
            new Modifier<CustomPlayer>
            {
                type = ModifierBase.Type.Trigger,
                constant = true,
                commands = new List<string> { "healthLesser" },
                value = "0"
            },
            new Modifier<CustomPlayer>
            {
                type = ModifierBase.Type.Trigger,
                constant = true,
                commands = new List<string> { "healthPerEquals" },
                value = "0"
            },
            new Modifier<CustomPlayer>
            {
                type = ModifierBase.Type.Trigger,
                constant = true,
                commands = new List<string> { "healthPerGreaterEquals" },
                value = "0"
            },
            new Modifier<CustomPlayer>
            {
                type = ModifierBase.Type.Trigger,
                constant = true,
                commands = new List<string> { "healthPerLesserEquals" },
                value = "0"
            },
            new Modifier<CustomPlayer>
            {
                type = ModifierBase.Type.Trigger,
                constant = true,
                commands = new List<string> { "healthPerGreater" },
                value = "0"
            },
            new Modifier<CustomPlayer>
            {
                type = ModifierBase.Type.Trigger,
                constant = true,
                commands = new List<string> { "healthPerLesser" },
                value = "0"
            },
            new Modifier<CustomPlayer>
            {
                type = ModifierBase.Type.Trigger,
                constant = true,
                commands = new List<string> { "isDead" },
                value = "0"
            },
            new Modifier<CustomPlayer>
            {
                type = ModifierBase.Type.Trigger,
                constant = true,
                commands = new List<string> { "isBoosting" },
                value = "0"
            },
        };
    }
}
