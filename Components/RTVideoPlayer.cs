using BetterLegacy.Configs;
using BetterLegacy.Core;
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

        public static YieldType yieldType = YieldType.FixedUpdate;

        public bool stopped = false;

        IEnumerator IUpdateVideo()
        {
            float delay = 0f;
            while (true)
            {
                if (stopped)
                {
                    stopped = false;
                    yield break;
                }

                UpdatedAudioPos?.Invoke(AudioManager.inst.CurrentAudioSource.isPlaying, AudioManager.inst.CurrentAudioSource.time - timeOffset, AudioManager.inst.CurrentAudioSource.pitch);

                if (yieldType != YieldType.None)
                    yield return CoreHelper.GetYieldInstruction(yieldType, ref delay);
                else
                    yield return null; // not having it yield return will freeze the game indefinitely.
            }
        }

        void UpdateTime(bool isPlaying, float time, float pitch)
        {
            if (!videoPlayer || !videoPlayer.enabled)
                return;

            if (isPlaying)
                videoPlayer.Play();
            else
                videoPlayer.Pause();

            if (videoPlayer.playbackSpeed != pitch)
                videoPlayer.playbackSpeed = pitch;
            time = Mathf.Clamp(time, 0f, (float)videoPlayer.length);
            if (RTMath.Distance(videoPlayer.time, time) > 0.2)
                videoPlayer.time = time;
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

            CoreHelper.StartCoroutine(IUpdateVideo());
        }

        public void Stop()
        {
            CoreHelper.Log($"Stopping Video.");
            if (videoPlayer)
                videoPlayer.enabled = false;

            if (videoPlayer == null)
                CoreHelper.LogError($"VideoPlayer does not exist so it wasn't disabled. Continuing...");

            videoTexture?.SetActive(false);
            stopped = true;
        }
    }
}
