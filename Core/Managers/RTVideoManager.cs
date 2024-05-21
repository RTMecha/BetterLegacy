using BetterLegacy.Configs;
using System;
using System.Collections;
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

        bool prevPlaying;
        float prevTime;
        float prevPitch;

        bool canUpdate = true;

        public static void Init()
        {
            var gameObject = new GameObject("VideoManager");
            gameObject.transform.SetParent(SystemManager.inst.transform);
            gameObject.AddComponent<RTVideoManager>();
        }

        void Awake()
        {
            inst = this;

            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.renderMode = renderType == RenderType.Camera ? VideoRenderMode.CameraFarPlane : VideoRenderMode.MaterialOverride;
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.timeSource = VideoTimeSource.GameTimeSource;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.isLooping = false;
            videoPlayer.waitForFirstFrame = false;

            UpdatedAudioPos += UpdateTime;
        }

        void Update()
        {
            if (canUpdate && (prevTime != AudioManager.inst.CurrentAudioSource.time || prevPlaying != AudioManager.inst.CurrentAudioSource.isPlaying))
            {
                if (videoPlayer != null && videoPlayer.enabled && videoPlayer.isPrepared)
                {
                    UpdatedAudioPos?.Invoke(AudioManager.inst.CurrentAudioSource.isPlaying, AudioManager.inst.CurrentAudioSource.time, AudioManager.inst.CurrentAudioSource.pitch);
                }
            }
            prevPlaying = AudioManager.inst.CurrentAudioSource.isPlaying;
            prevTime = AudioManager.inst.CurrentAudioSource.time;
            prevPitch = AudioManager.inst.CurrentAudioSource.pitch;
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
        public float currentAlpha;
        public bool didntPlay = false;

        public IEnumerator Setup(string fullPath)
        {
            if (fullPath[fullPath.Length - 1] != '/' || fullPath[fullPath.Length - 1] != '\\')
                fullPath += "/";

            if (!CoreConfig.Instance.EnableVideoBackground.Value || !RTFile.FileExists(fullPath + "bg.mp4") && !RTFile.FileExists(fullPath + "bg.mov"))
            {
                Stop();
                yield break;
            }

            if (RTFile.FileExists(fullPath + "bg.mp4"))
            {
                Play(fullPath + "bg.mp4", 1f);
                while (!videoPlayer.isPrepared)
                    yield return null;
            }
            else if (RTFile.FileExists(fullPath + "bg.mov"))
            {
                Play(fullPath + "bg.mov", 1f);
                while (!videoPlayer.isPrepared)
                    yield return null;
            }

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

            if (!videoTexture && GameObject.Find("ExtraBG") && GameObject.Find("ExtraBG").transform.childCount > 0)
            {
                videoTexture = GameObject.Find("ExtraBG").transform.GetChild(0).gameObject;
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