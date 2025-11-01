using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Video;

using BetterLegacy.Configs;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Core.Managers
{
    /// <summary>
    /// Manages the video that can play in a level.
    /// <br></br>Wraps <see cref="VideoManager"/>.
    /// </summary>
    public class RTVideoManager : BaseManager<RTVideoManager, ManagerSettings>
    {
        #region Values

        /// <summary>
        /// Way to render the video.
        /// </summary>
        public enum RenderType
        {
            /// <summary>
            /// Always renders at the camera's resolution and position.
            /// </summary>
            Camera,
            /// <summary>
            /// Renders onto an object.
            /// </summary>
            Background,
        }

        /// <summary>
        /// The way the video player renders.
        /// </summary>
        public RenderType renderType = RenderType.Background;

        /// <summary>
        /// Video player component.
        /// </summary>
        public VideoPlayer videoPlayer;

        /// <summary>
        /// Video texture.
        /// </summary>
        public GameObject videoTexture;

        /// <summary>
        /// Event to run on audio update.
        /// </summary>
        public event Action<bool, float, float> UpdatedAudioPos;

        /// <summary>
        /// List of video player components.
        /// </summary>
        public List<RTVideoPlayer> videoPlayers = new List<RTVideoPlayer>();

        /// <summary>
        /// If the video is currently being seeked.
        /// </summary>
        public bool Seeking { get; private set; }

        /// <summary>
        /// The current URL / path of the video.
        /// </summary>
        public string currentURL;

        /// <summary>
        /// The current opacity of the video.
        /// </summary>
        public float currentAlpha;

        /// <summary>
        /// If the video didn't play.
        /// </summary>
        public bool didntPlay;

        #endregion

        #region Functions

        public override void OnInit()
        {
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

        public override void OnTick()
        {
            if (videoPlayer && videoPlayer.enabled && videoPlayer.isPlaying != AudioManager.inst.CurrentAudioSource.isPlaying)
                UpdateVideo();
        }

        /// <summary>
        /// Sets the render type of the video player.
        /// </summary>
        /// <param name="renderType">Render type to set.</param>
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

        /// <summary>
        /// Updates the video player.
        /// </summary>
        public void UpdateVideo()
        {
            UpdatedAudioPos?.Invoke(AudioManager.inst.CurrentAudioSource.isPlaying, AudioManager.inst.CurrentAudioSource.time, AudioManager.inst.CurrentAudioSource.pitch);
            for (int i = 0; i < videoPlayers.Count; i++)
                videoPlayers[i].UpdateVideo();
        }

        /// <summary>
        /// Sets up the video player with a set path.
        /// </summary>
        /// <param name="fullPath">Path to the folder that contains the video.</param>
        public IEnumerator Setup(string folder)
        {
            if (!CoreConfig.Instance.EnableVideoBackground.Value || !RTFile.FileExists(RTFile.CombinePaths(folder, "bg.mp4")) && !RTFile.FileExists(RTFile.CombinePaths(folder, "bg.mov")))
            {
                Stop();
                yield break;
            }

            if (RTFile.FileExists(RTFile.CombinePaths(folder, "bg.mp4")))
            {
                Play(RTFile.CombinePaths(folder, "bg.mp4"), 1f);
                while (!videoPlayer.isPrepared)
                    yield return null;
            }
            else if (RTFile.FileExists(RTFile.CombinePaths(folder, "bg.mov")))
            {
                Play(RTFile.CombinePaths(folder, "bg.mov"), 1f);
                while (!videoPlayer.isPrepared)
                    yield return null;
            }
        }

        /// <summary>
        /// Plays a stored video clip.
        /// </summary>
        /// <param name="videoClip">Video clip to play.</param>
        public void Play(VideoClip videoClip)
        {
            if (!videoPlayer)
            {
                LogError($"VideoPlayer does not exist so the set video cannot play.");
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

            Log($"Playing Video from VideoClip");
            videoTexture?.SetActive(renderType == RenderType.Background);
            videoPlayer.enabled = true;
            videoPlayer.targetCameraAlpha = 1f;
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = videoClip;
            videoPlayer.Prepare();
            didntPlay = false;
        }

        /// <summary>
        /// Plays a video at a set file path or URL.
        /// </summary>
        /// <param name="url">URL to play.</param>
        /// <param name="alpha">Opacity of the video.</param>
        public void Play(string url, float alpha)
        {
            if (!videoPlayer)
            {
                LogError($"VideoPlayer does not exist so the set video cannot play.");
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

            Log($"Playing Video from {url}");
            videoTexture?.SetActive(renderType == RenderType.Background);
            videoPlayer.enabled = true;
            videoPlayer.targetCameraAlpha = alpha;
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = url;
            videoPlayer.Prepare();
            didntPlay = false;
        }

        /// <summary>
        /// Stops the video from playing.
        /// </summary>
        public void Stop()
        {
            Log($"Stopping Video.");
            if (videoPlayer)
                videoPlayer.enabled = false;
            else
                LogError($"VideoPlayer does not exist so it wasn't disabled. Continuing...");

            videoTexture?.SetActive(false);
        }

        #region Internal

        void UpdateTime(bool isPlaying, float time, float pitch)
        {
            if (!videoPlayer || !videoPlayer.enabled)
                return;

            if (videoPlayer.isPlaying != isPlaying)
                (isPlaying ? (Action)videoPlayer.Play : videoPlayer.Pause).Invoke();

            if (videoPlayer.playbackSpeed != pitch)
                videoPlayer.playbackSpeed = pitch;
            if (RTMath.Distance(videoPlayer.time, time) > 0.1 && !Seeking)
            {
                Seeking = true;
                videoPlayer.time = time;
            }
        }

        void SeekCompleted(VideoPlayer source) => Seeking = false;

        #endregion

        #endregion
    }
}