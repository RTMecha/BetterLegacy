using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

using FFMpegCore;
using FFMpegCore.Pipes;
using StbVorbisSharp;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Threading;

namespace BetterLegacy.Core.FFmpeg
{
    // based on https://github.com/Reimnop/ParallelAnimationSystem and https://github.com/keijiro/FFmpegOut
    /// <summary>
    /// Class for exporting a level to a video file.
    /// </summary>
    public class LevelExporter : Exists
    {
        /* TODO:
         * - Look into why FFmpeg isn't doing anything.
         * - Implement.
         * - Custom formats?
         * - GIFs?
         * - Support audio keyframes (pitch, volume, pan stereo, etc)
         */

        public LevelExporter() { }

        #region Values

        /// <summary>
        /// The current level exporter.
        /// </summary>
        public static LevelExporter Current { get; set; }

        static FFOptions options;
        /// <summary>
        /// Options for the configuration.
        /// </summary>
        public static FFOptions Options
        {
            get
            {
                if (options == null)
                {
                    var directory = RTFile.CombinePaths(RTFile.ApplicationDirectory, RTFile.BepInExPluginsPath, "Dependencies");
                    options = new FFOptions { BinaryFolder = directory, WorkingDirectory = directory };
                }
                return options;
            }
        }

        /// <summary>
        /// Path to write the video to.
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// Path of the audio file.
        /// </summary>
        public string AudioPath { get; set; }

        /// <summary>
        /// Resolution width.
        /// </summary>
        public int Width { get; set; } = 1920;

        /// <summary>
        /// Resolution height.
        /// </summary>
        public int Height { get; set; } = 1080;

        /// <summary>
        /// Frames per second.
        /// </summary>
        public int Framerate { get; } = 60;

        /// <summary>
        /// Speed of the rendered level.
        /// </summary>
        public float Speed { get; set; } = 1f;

        /// <summary>
        /// Video codec.
        /// </summary>
        public string VideoCodec { get; set; } = "libx264";

        /// <summary>
        /// Audio codec.
        /// </summary>
        public string AudioCodec { get; set; } = "aac";

        State state;
        /// <summary>
        /// Current state of the exporter.
        /// </summary>
        public State CurrentState => state;

        Queue<Action> renderQueue = new Queue<Action>();
        List<IVideoFrame> videoFrames = new List<IVideoFrame>();
        bool processedVideo;
        Thread thread;
        bool running;
        bool hadError;
        Exception exception;
        FFMpegArgumentProcessor argumentProcessor;
        System.Diagnostics.Stopwatch sw;

        #endregion

        #region Functions

        /// <summary>
        /// Downloads the binaries used for creating videos.
        /// </summary>
        public static void Setup() => FFMpegCore.Extensions.Downloader.FFMpegDownloader.DownloadBinaries(options: Options);

        /// <summary>
        /// Starts the level export.
        /// </summary>
        public async Task Export()
        {
            if (Current)
                return;

            Current = this;

            running = true;
            StartThread();

            sw = CoreHelper.StartNewStopwatch();
            processedVideo = false;
            SetState(State.Rendering);

            videoFrames = new List<IVideoFrame>();

            var step = 1.0f / Framerate * Speed;
            var duration = SoundManager.inst.MusicLength;

            using var vorbis = Vorbis.FromMemory(File.ReadAllBytes(AudioPath));

            await ProcessVideoFrames(step, duration);
            while (!processedVideo && !hadError && running)
                await Task.Delay(1);

            if (hadError)
            {
                CoreHelper.Log($"Had an error with processing the video.\nException: {exception}");
                return;
            }

            if (!running)
            {
                CoreHelper.Log($"Stopped!");
                return;
            }

            var videoFramesSource = new RawVideoPipeSource(videoFrames)
            {
                FrameRate = Framerate,
            };
            CoreHelper.Log($"Init raw video source\nElapsed: {sw.Elapsed}");
            var audioFramesSource = new RawAudioPipeSource(ProcessAudioFrames(vorbis))
            {
                Channels = (uint)vorbis.Channels,
                SampleRate = (uint)(vorbis.SampleRate * Speed),
                Format = "s161e",
            };
            CoreHelper.Log($"Init raw audio source\nElapsed: {sw.Elapsed}");

            SetState(State.Compiling);
            GlobalFFOptions.Configure(Options);
            CoreHelper.Log($"Configured FFmpeg\nElapsed: {sw.Elapsed}");
            argumentProcessor = FFMpegArguments
                .FromPipeInput(videoFramesSource)
                .AddPipeInput(audioFramesSource)
                .OutputToFile(OutputPath, true, options => options
                    .WithVideoCodec(VideoCodec)
                    .WithAudioCodec(AudioCodec))
                .NotifyOnError(onError => LegacyPlugin.MainTick += () => CoreHelper.LogError($"HAD ERROR: {onError}"))
                .NotifyOnProgress(p => LegacyPlugin.MainTick += () => CoreHelper.Log($"PROGRESS: {p}"), TimeSpan.FromSeconds(duration)) // this never runs...
                .WithLogLevel(FFMpegCore.Enums.FFMpegLogLevel.Verbose)
                .NotifyOnOutput(o => LegacyPlugin.MainTick += () => CoreHelper.Log($"OUTPUT: {o}")); // it never outputs????
            await argumentProcessor.ProcessAsynchronously(); // why does it never end????????
            videoFrames = null;
            running = false;
            SetState(State.Idle);
            sw.Stop();
            CoreHelper.Log($"Finished exporting!\nElapsed: {sw.Elapsed}");
            sw = null;
        }

        /// <summary>
        /// Stops the exporter.
        /// </summary>
        public void Stop()
        {
            running = false;
            SetState(State.Idle);
            thread?.Join();
            thread = null;
        }

        void StartThread()
        {
            if (thread != null)
                return;

            thread = new Thread(ThreadTick);
            thread.Start();
        }

        void ThreadTick()
        {
            while (running)
            {
                while (!renderQueue.IsEmpty())
                {
                    try
                    {
                        renderQueue.Dequeue()?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        hadError = true;
                        exception = ex;
                        break;
                    }
                }
            }
        }

        void SetState(State state)
        {
            this.state = state;
            CoreHelper.Log($"Exporter state: {state}");
        }

        // we do this as a coroutine instead of a collection so that we can render each frame without freezing the game.
        IEnumerator ProcessVideoFrames(float step, float duration)
        {
            var i = 0;
            for (var t = 0.0f; t <= duration; t += step)
            {
                var r = RenderVideoFrame(t);

                while (!r.done)
                {
                    if (r.hasError)
                    {
                        hadError = true;
                        yield break;
                    }
                    yield return null;
                }

                if (r.hasError)
                {
                    CoreHelper.LogWarning($"GPU readback error has occured.");
                    continue;
                }

                try
                {
                    var data = r.GetData<byte>();
                    renderQueue.Enqueue(() => QueueFrame(data));
                }
                catch (Exception ex)
                {
                    hadError = true;
                    exception = ex;
                    break;
                }

                yield return new WaitForEndOfFrame();
                if (!running)
                    yield break;
                i++;

                if (i % (Framerate * 2) == 0)
                    CoreHelper.Log($"Processing video frames: [{i}] at {t / duration * 100f}% ({t} / {duration})\nElapsed: {sw.Elapsed}");
            }
            while (!renderQueue.IsEmpty() && !hadError)
                yield return null;
            processedVideo = true;
        }

        void QueueFrame(NativeArray<byte> data)
        {
            var frame = new FrameData(Width, Height);
            frame.Data = data.ToArray();
            videoFrames.Add(new FFmpegFrameData(frame));
        }

        AsyncGPUReadbackRequest RenderVideoFrame(float t)
        {
            var renderTexture = RenderTexture.GetTemporary(Width, Height, 24);
            var currentTime = AudioManager.inst.CurrentAudioSource.time;
            AudioManager.inst.CurrentAudioSource.time = t;
            RTLevel.Current?.Tick();

            foreach (var camera in RTLevel.Cameras.GetCameras())
            {
                var currentActiveRT = RenderTexture.active;
                RenderTexture.active = renderTexture;

                // Assign render texture to camera and render the camera
                camera.targetTexture = renderTexture;
                camera.Render();
                renderTexture.Create();

                // Reset to defaults
                camera.targetTexture = null;
                RenderTexture.active = currentActiveRT;
            }

            // disable and re-enable the glitch camera to ensure the glitch camera is ordered last.
            RTLevel.Cameras.PostProcess.enabled = false;
            RTLevel.Cameras.PostProcess.enabled = true;

            // disable and re-enable the UI camera to ensure the UI camera is ordered last.
            RTLevel.Cameras.UI.enabled = false;
            RTLevel.Cameras.UI.enabled = true;

            var r = AsyncGPUReadback.Request(renderTexture);
            AudioManager.inst.CurrentAudioSource.time = currentTime;
            RenderTexture.ReleaseTemporary(renderTexture);
            return r;
        }

        // currently we just use the audio file for the exported video, however this creates an issue.
        // levels have audio keyframes which will be ignored because of this. is there a way we can use the keyframe?
        unsafe List<IAudioSample> ProcessAudioFrames(Vorbis vorbis)
        {
            var frames = new List<IAudioSample>();

            while (true)
            {
                vorbis.SubmitBuffer();

                if (vorbis.Decoded == 0)
                    break;

                var sBuffer = vorbis.SongBuffer;
                var bBuffer = new byte[sBuffer.Length * 2];
                for (var i = 0; i < sBuffer.Length; i++)
                {
                    bBuffer[i * 2 + 0] = (byte)(sBuffer[i] & 0xff00 >> 8);
                    bBuffer[i * 2 + 1] = (byte)(sBuffer[i] & 0x00ff);
                }
                frames.Add(new FFmpegAudioFrame(bBuffer));
            }

            return frames;
        }

        FFmpegAudioFrame RenderAudioFrame(float t)
        {
            var currentTime = AudioManager.inst.CurrentAudioSource.time;
            AudioManager.inst.CurrentAudioSource.time = t;
            AudioManager.inst.CurrentAudioSource.UnPause();
            var clip = AudioManager.inst.CurrentAudioSource.clip;
            var samples = new float[clip.samples * clip.channels];
            AudioManager.inst.CurrentAudioSource.clip.GetData(samples, 0);

            var audioFrame = new FFmpegAudioFrame(ToByteArray(samples));
            AudioManager.inst.CurrentAudioSource.Pause();
            AudioManager.inst.CurrentAudioSource.time = currentTime;
            return audioFrame;
        }

        byte[] ToByteArray(float[] array)
        {
            var byteArray = new byte[array.Length * 4];
            Buffer.BlockCopy(array, 0, byteArray, 0, byteArray.Length);
            return byteArray;
        }

        float[] ToFloatArray(byte[] array)
        {
            var floatArray = new float[array.Length / 4];
            Buffer.BlockCopy(array, 0, floatArray, 0, array.Length);
            return floatArray;
        }

        #endregion

        #region Sub

        public enum State
        {
            Idle,
            Rendering,
            Compiling,
        }

        public class FFmpegFrameData : IVideoFrame
        {
            public FFmpegFrameData(FrameData frameData)
            {
                this.frameData = frameData;
            }

            FrameData frameData;

            public int Width => frameData.Width;
            public int Height => frameData.Height;
            public string Format => "rgba";

            public void Serialize(Stream pipe) => pipe.Write(frameData.Data, 0, frameData.Data.Length);

            public Task SerializeAsync(Stream pipe, CancellationToken token) => pipe.WriteAsync(frameData.Data, 0, frameData.Data.Length, token);
        }

        public class FrameData
        {
            public FrameData(int width, int height)
            {
                this.width = width;
                this.height = height;
                Data = new byte[width * height * 4];
            }

            int width;
            int height;

            public int Width => width;
            public int Height => height;
            public byte[] Data { get; set; }
        }

        public class FFmpegAudioFrame : IAudioSample
        {
            public FFmpegAudioFrame(byte[] data)
            {
                this.data = data;
            }

            byte[] data;

            public void Serialize(Stream stream) => stream.Write(data, 0, data.Length);

            public Task SerializeAsync(Stream stream, CancellationToken token) => stream.WriteAsync(data, 0, data.Length, token);
        }

        #endregion
    }
}
