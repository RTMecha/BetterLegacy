using BetterLegacy.Configs;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace BetterLegacy.Core.Managers
{
    public class RTVideoManager : MonoBehaviour
    {
        public static RTVideoManager inst;

        public static string className = "[<color=#e65100>RTVideoManager</color>] \n";

        public enum RenderType
        {
            Camera, // Always renders at the camera's resolution and position.
            Background // Renders at a set spot.
        }

        public RenderType renderType = RenderType.Background;

        public VideoPlayer videoPlayer;

        public GameObject videoTexture;

        public event Action<bool, float, float> UpdatedAudioPos;

        public List<RTVideoPlayer> videoPlayers = new List<RTVideoPlayer>();

        public static void Init() => Creator.NewGameObject(nameof(VideoManager), SystemManager.inst.transform).AddComponent<RTVideoManager>();

        void Awake()
        {
            if (inst)
            {
                Debug.LogWarning($"{className}Init was already called!");
                return;
            }    

            inst = this;

            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.renderMode = renderType == RenderType.Camera ? VideoRenderMode.CameraFarPlane : VideoRenderMode.MaterialOverride;
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.timeSource = VideoTimeSource.GameTimeSource;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.isLooping = false;
            videoPlayer.playOnAwake = true;
            videoPlayer.waitForFirstFrame = false;
            videoPlayer.seekCompleted += SeekCompleted;

            UpdatedAudioPos += UpdateTime;
        }

        bool seeking = false;

        void SeekCompleted(VideoPlayer source) => seeking = false;

        void Update()
        {
            if (videoPlayer && videoPlayer.enabled && videoPlayer.isPlaying != AudioManager.inst.CurrentAudioSource.isPlaying)
                UpdateVideo();
        }

        public void SetType(RenderType renderType)
        {
            this.renderType = renderType;
            videoPlayer.renderMode = this.renderType == RenderType.Camera ? VideoRenderMode.CameraFarPlane : VideoRenderMode.MaterialOverride;
            if (!videoTexture && GameObject.Find("ExtraBG") && GameObject.Find("ExtraBG").transform.childCount > 0)
            {
                videoTexture = GameObject.Find("ExtraBG").transform.GetChild(0).gameObject;
                videoPlayer.targetMaterialRenderer = videoTexture.GetComponent<MeshRenderer>();
            }

            Play(currentURL, currentAlpha);
        }

        public void UpdateVideo()
        {
            UpdatedAudioPos?.Invoke(AudioManager.inst.CurrentAudioSource.isPlaying, AudioManager.inst.CurrentAudioSource.time, AudioManager.inst.CurrentAudioSource.pitch);
            for (int i = 0; i < videoPlayers.Count; i++)
                videoPlayers[i].UpdateVideo();
        }

        void UpdateTime(bool isPlaying, float time, float pitch)
        {
            if (!videoPlayer || !videoPlayer.enabled)
                return;

            if (videoPlayer.isPlaying != isPlaying)
                (isPlaying ? (Action)videoPlayer.Play : videoPlayer.Pause).Invoke();

            if (videoPlayer.playbackSpeed != pitch)
                videoPlayer.playbackSpeed = pitch;
            if (RTMath.Distance(videoPlayer.time, time) > 0.1 && !seeking)
            {
                seeking = true;
                videoPlayer.time = time;
            }
        }

        public string currentURL;
        public float currentAlpha;
        public bool didntPlay = false;

        public IEnumerator Setup(string fullPath)
        {
            if (!CoreConfig.Instance.EnableVideoBackground.Value || !RTFile.FileExists(RTFile.CombinePaths(fullPath, "bg.mp4")) && !RTFile.FileExists(RTFile.CombinePaths(fullPath, "bg.mov")))
            {
                Stop();
                yield break;
            }

            if (RTFile.FileExists(RTFile.CombinePaths(fullPath, "bg.mp4")))
            {
                Play(RTFile.CombinePaths(fullPath, "bg.mp4"), 1f);
                while (!videoPlayer.isPrepared)
                    yield return null;
            }
            else if (RTFile.FileExists(RTFile.CombinePaths(fullPath, "bg.mov")))
            {
                Play(RTFile.CombinePaths(fullPath, "bg.mov"), 1f);
                while (!videoPlayer.isPrepared)
                    yield return null;
            }

        }

        public void Play(VideoClip videoClip)
        {
            if (videoPlayer == null)
            {
                Debug.LogError($"{className}VideoPlayer does not exist so the set video cannot play.");
                return;
            }

            if (!CoreConfig.Instance.EnableVideoBackground.Value)
            {
                videoPlayer.enabled = false;
                videoTexture?.SetActive(false);
                didntPlay = true;
                return;
            }

            if (!videoTexture && CoreHelper.TryFind("ExtraBG", out GameObject extraBG) && extraBG.transform.childCount > 0)
            {
                videoTexture = extraBG.transform.GetChild(0).gameObject;
                videoPlayer.targetMaterialRenderer = videoTexture.GetComponent<MeshRenderer>();
            }

            Debug.Log($"{className}Playing Video from VideoClip");
            videoTexture?.SetActive(renderType == RenderType.Background);
            videoPlayer.enabled = true;
            videoPlayer.targetCameraAlpha = 1f;
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = videoClip;
            videoPlayer.Prepare();
            didntPlay = false;
        }

        public void Play(string url, float alpha)
        {
            if (videoPlayer == null)
            {
                Debug.LogError($"{className}VideoPlayer does not exist so the set video cannot play.");
                return;
            }

            currentURL = url;
            currentAlpha = alpha;

            if (!CoreConfig.Instance.EnableVideoBackground.Value)
            {
                videoPlayer.enabled = false;
                videoTexture?.SetActive(false);
                didntPlay = true;
                return;
            }

            if (!videoTexture && CoreHelper.TryFind("ExtraBG", out GameObject extraBG) && extraBG.transform.childCount > 0)
            {
                videoTexture = extraBG.transform.GetChild(0).gameObject;
                videoPlayer.targetMaterialRenderer = videoTexture.GetComponent<MeshRenderer>();
            }

            Debug.Log($"{className}Playing Video from {url}");
            videoTexture?.SetActive(renderType == RenderType.Background);
            videoPlayer.enabled = true;
            videoPlayer.targetCameraAlpha = alpha;
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = url;
            videoPlayer.Prepare();
            didntPlay = false;
        }

        public void Stop()
        {
            Debug.Log($"{className}Stopping Video.");
            if (videoPlayer)
                videoPlayer.enabled = false;

            if (videoPlayer == null)
                Debug.LogError($"{className}VideoPlayer does not exist so it wasn't disabled. Continuing...");

            videoTexture?.SetActive(false);
        }
    }
}