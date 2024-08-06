using BetterLegacy.Configs;
using BetterLegacy.Core.Helpers;
using System;
using System.Collections;

using UnityEngine;
using UnityEngine.Video;

namespace BetterLegacy.Components
{
    public class RTVideoPlayer : MonoBehaviour
    {
        public VideoPlayer videoPlayer;

        public GameObject videoTexture;

        public event Action<bool, float, float> UpdatedAudioPos;

        bool prevPlaying;
        float prevTime;
        float prevPitch;

        bool seekDone;

        bool canUpdate = true;

        bool oldStyle = true;

        public float timeOffset;

        void Awake()
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.targetCamera = Camera.main;
            videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.targetCameraAlpha = 1f;
            videoPlayer.timeSource = VideoTimeSource.GameTimeSource;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.isLooping = false;
            videoPlayer.waitForFirstFrame = false;

            UpdatedAudioPos += UpdateTime;
        }

        void Update()
        {
            if (canUpdate && (prevTime != AudioManager.inst.CurrentAudioSource.time - timeOffset || prevPlaying != AudioManager.inst.CurrentAudioSource.isPlaying))
            {
                if (videoPlayer != null && videoPlayer.enabled && videoPlayer.isPrepared)
                {
                    UpdatedAudioPos?.Invoke(AudioManager.inst.CurrentAudioSource.isPlaying, AudioManager.inst.CurrentAudioSource.time - timeOffset, AudioManager.inst.CurrentAudioSource.pitch);
                }
            }
            prevPlaying = AudioManager.inst.CurrentAudioSource.isPlaying;
            prevTime = AudioManager.inst.CurrentAudioSource.time - timeOffset;
            prevPitch = AudioManager.inst.CurrentAudioSource.pitch;
        }

        void UpdateTime(bool isPlaying, float time, float pitch)
        {
            if (isPlaying)
            {
                if (!videoPlayer.isPlaying)
                    videoPlayer.Play();
                videoPlayer.Pause();

                videoPlayer.time = time;
            }
            else
            {
                videoPlayer.Pause();
            }
        }

        public string currentURL;
        public bool didntPlay = false;

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
