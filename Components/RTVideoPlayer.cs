using BetterLegacy.Configs;
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
            videoPlayer.seekCompleted += SeekCompleted;
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
            //videoPlayer.playbackSpeed = pitch < 0f ? -pitch : pitch;
            if (isPlaying)
            {
                if (!videoPlayer.isPlaying)
                    videoPlayer.Play();
                if (!oldStyle)
                {
                    UpdateVideoPlayerToFrame(time);
                }
                else
                {
                    videoPlayer.Pause();

                    videoPlayer.time = time;

                    //videoPlayer.Play();
                }
            }
            else
            {
                videoPlayer.Pause();
                //videoPlayer.time = time;
            }
        }

        public string currentURL;
        public float currentAlpha;
        public bool didntPlay = false;

        public void Play(string url, float alpha)
        {
            currentURL = url;
            currentAlpha = alpha;

            if (!CoreConfig.Instance.EnableVideoBackground.Value)
            {
                videoPlayer.enabled = false;
                didntPlay = true;
                return;
            }

            if (!videoTexture)
            {
                videoTexture = gameObject;
                videoPlayer.targetMaterialRenderer = videoTexture.GetComponent<MeshRenderer>();
            }

            videoPlayer.enabled = true;
            videoPlayer.targetCameraAlpha = alpha;
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = url;
            videoPlayer.Prepare();
            didntPlay = false;
        }

        public void Stop()
        {
            videoPlayer.enabled = false;
        }

        void SeekCompleted(VideoPlayer par)
        {
            if (!oldStyle)
                StartCoroutine(WaitToUpdateRenderTextureBeforeEndingSeek());
        }

        public void UpdateVideoPlayerToFrame(float time)
        {
            //If you are currently seeking there is no point to seek again.
            if (!seekDone)
                return;

            // You should pause while you seek for better stability
            videoPlayer.Pause();

            videoPlayer.time = time;
            seekDone = false;
        }

        IEnumerator WaitToUpdateRenderTextureBeforeEndingSeek()
        {
            yield return new WaitForEndOfFrame();
            seekDone = true;
        }
    }
}
