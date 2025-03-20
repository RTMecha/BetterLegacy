using System;

using UnityEngine;
using UnityEngine.Video;

using BetterLegacy.Configs;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

namespace BetterLegacy.Core.Components
{
    public class RTVideoPlayer : MonoBehaviour
    {
        public VideoPlayer videoPlayer;

        public GameObject videoTexture;

        //public event Action<bool, float, float> UpdatedAudioPos;

        public int index;

        public float timeOffset;

        void Awake()
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.targetCamera = Camera.main;
            videoPlayer.aspectRatio = VideoAspectRatio.Stretch;
            videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.targetCameraAlpha = 1f;
            videoPlayer.timeSource = VideoTimeSource.GameTimeSource;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.isLooping = false;
            videoPlayer.waitForFirstFrame = false;
            videoPlayer.seekCompleted += SeekCompleted;

            //UpdatedAudioPos += UpdateTime;

            index = RTVideoManager.inst.videoPlayers.Count;
            RTVideoManager.inst.videoPlayers.Add(this);
        }

        void OnDestroy() => RTVideoManager.inst.videoPlayers.RemoveAt(index);

        bool seeking = false;

        void SeekCompleted(VideoPlayer source) => seeking = false;

        void Update()
        {
            if (videoPlayer && videoPlayer.enabled && videoPlayer.isPlaying != AudioManager.inst.CurrentAudioSource.isPlaying)
                UpdateVideo();
        }

        public void UpdateVideo() => UpdateTime(AudioManager.inst.CurrentAudioSource.isPlaying, AudioManager.inst.CurrentAudioSource.time - timeOffset, AudioManager.inst.CurrentAudioSource.pitch);

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
        public bool didntPlay = true;

        public void Play(GameObject gameObject, string url, VideoAudioOutputMode videoAudioOutputMode)
        {
            if (videoPlayer == null)
            {
                CoreHelper.LogError($"VideoPlayer does not exist so the set video cannot play.");
                return;
            }

            currentURL = url;

            if (!CoreConfig.Instance.EnableVideoBackground.Value)
            {
                videoPlayer.enabled = false;
                videoTexture?.SetActive(false);
                didntPlay = true;
                return;
            }

            if (!videoTexture)
            {
                videoTexture = gameObject;
                videoPlayer.targetMaterialRenderer = videoTexture.GetComponent<MeshRenderer>();
            }

            videoTexture.SetActive(true);
            videoPlayer.enabled = true;
            videoPlayer.audioOutputMode = videoAudioOutputMode;
            videoPlayer.targetCameraAlpha = 1f;
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = url;
            videoPlayer.Prepare();
            didntPlay = false;
        }

        public void Stop()
        {
            CoreHelper.Log($"Stopping Video.");
            if (videoPlayer)
                videoPlayer.enabled = false;

            if (videoPlayer == null)
                CoreHelper.LogError($"VideoPlayer does not exist so it wasn't disabled. Continuing...");

            videoTexture?.SetActive(false);
        }
    }
}
