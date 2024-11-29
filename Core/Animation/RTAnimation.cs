using BetterLegacy.Components;
using BetterLegacy.Core.Animation.Keyframe;
using LSFunctions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BetterLegacy.Core.Animation
{
    /// <summary>
    /// Animation wrapper for handling multiple movements.
    /// </summary>
    public class RTAnimation
    {
        public RTAnimation(string name)
        {
            this.name = name;
            id = LSText.randomNumString(16);
            timeOffset = UnityEngine.Time.time;
        }

        #region Interpolation

        /// <summary>
        /// Updates the sequences this animation contains.
        /// </summary>
        public void Update()
        {
            Time += (GlobalTime - startTime - timeOffset) * speed;

            timeOffset = GlobalTime - startTime;

            if (animationHandlers == null || animationHandlers.Count < 1)
                return;

            for (int i = 0; i < animationHandlers.Count; i++)
            {
                var anim = animationHandlers[i];
                float localTime = time - anim.offsetTime;

                if (anim.Length >= localTime)
                {
                    anim.completed = false;
                    anim.Interpolate(localTime);
                }
                else if (!anim.completed)
                {
                    if (anim.interpolateOnComplete)
                        anim.Interpolate(localTime);

                    anim.completed = true;
                    anim.Completed();
                }
            }

            if (playing && Completed)
            {
                Finish();

                if (!loop)
                    return;

                ResetTime();
                Play();
            }
        }

        /// <summary>
        /// Interpolates at a custom time.
        /// </summary>
        /// <param name="t">Time scale.</param>
        /// <param name="runOnComplete">True if on complete functions should run.</param>
        public void Interpolate(float t, bool runOnComplete = false)
        {
            if (animationHandlers == null || animationHandlers.Count < 1)
                return;

            for (int i = 0; i < animationHandlers.Count; i++)
            {
                var anim = animationHandlers[i];
                float localTime = t - anim.offsetTime;

                if (anim.Length >= localTime)
                {
                    if (runOnComplete)
                        anim.completed = false;
                    anim.Interpolate(localTime);
                }
                else if (runOnComplete && !anim.completed)
                {
                    anim.completed = true;
                    anim.Completed();
                }
            }

        }

        #endregion

        #region Base

        /// <summary>
        /// Unique identification of the animation.
        /// </summary>
        public string id;
        /// <summary>
        /// Name of the animation.
        /// </summary>
        public string name;

        /// <summary>
        /// A list of sequences within the animation.
        /// </summary>
        public List<AnimationHandlerBase> animationHandlers = new List<AnimationHandlerBase>();

        #endregion

        #region Timing

        public void ResetTime()
        {
            time = 0f;
            timeOffset = GlobalTime;
            for (int i = 0; i < animationHandlers.Count; i++)
                animationHandlers[i].completed = false;
        }

        /// <summary>
        /// If true <see cref="GlobalTime"/> will use <see cref="UnityEngine.Time.time"/>, otherwise will use <see cref="AudioManager.CurrentAudioSource"/>'s time.
        /// </summary>
        public bool useRealTime = true;

        /// <summary>
        /// If true, the animation should loop.
        /// </summary>
        public bool loop;

        float time;
        /// <summary>
        /// Time scale of the animation.
        /// </summary>
        public float Time
        {
            get => time;
            private set => time = value;
        }

        /// <summary>
        /// The global time the animation should offset from.
        /// </summary>
        public float GlobalTime => useRealTime ? UnityEngine.Time.time : AudioManager.inst.CurrentAudioSource.time;

        /// <summary>
        /// The Start Time offset when the animation should start interpolation.
        /// </summary>
        public float startTime;

        /// <summary>
        /// The current speed of the animation. Applied per frame the animation is run on, so if you want to animate it you will need to create a separate sequence for it.
        /// </summary>
        public float speed = 1f;

        /// <summary>
        /// Total length of all sequences in the animation.
        /// </summary>
        public float Length => animationHandlers.Max(x => x.Length);

        float timeOffset;

        #endregion

        #region Play State

        /// <summary>
        /// Starts the animation from the beginning.
        /// </summary>
        public void Start()
        {
            ResetTime();
            Play();
        }

        /// <summary>
        /// Adds the animation to the main animation manager and starts playing it.
        /// </summary>
        public void Run() => AnimationManager.inst.Play(this);

        /// <summary>
        /// Adds the animation to an <see cref="AnimationController"/> and starts playing it.
        /// </summary>
        public void Run(AnimationController animationController) => animationController.Play(this);

        /// <summary>
        /// Stops the animation and runs the completion functions.
        /// </summary>
        public void Stop()
        {
            Pause();

            if (animationHandlers == null || animationHandlers.Count < 1)
            {
                Finish();
                return;
            }

            for (int i = 0; i < animationHandlers.Count; i++)
            {
                var anim = animationHandlers[i];
                float localTime = time - anim.offsetTime;

                if (!anim.completed)
                {
                    if (anim.interpolateOnComplete)
                        anim.Interpolate(localTime);

                    anim.completed = true;
                    anim.Completed();
                }
            }

            if (playing && Completed)
                Finish();
        }

        /// <summary>
        /// Pauses the animation and runs the main completion function.
        /// </summary>
        public void Finish()
        {
            playing = false;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Pauses the animation.
        /// </summary>
        public void Pause() => playing = false;

        /// <summary>
        /// Resumes the animation.
        /// </summary>
        public void Play() => playing = true;

        /// <summary>
        /// Sets the default completion function, which removes the animation from the main animation manager.
        /// </summary>
        public void SetDefaultOnComplete(bool finishInterpolation = true) => onComplete = () =>
        {
            AnimationManager.inst.Remove(id);
            if (finishInterpolation)
            {
                for (int i = 0; i < animationHandlers.Count; i++)
                {
                    var anim = animationHandlers[i];
                    anim.Interpolate(time - anim.offsetTime);
                }
            }
        };
        
        /// <summary>
        /// Sets the default completion function, which removes the animation from a <see cref="AnimationController"/>.
        /// </summary>
        public void SetDefaultOnComplete(AnimationController animationController, bool finishInterpolation = true) => onComplete = () =>
        {
            animationController.Remove(id);
            if (finishInterpolation)
            {
                for (int i = 0; i < animationHandlers.Count; i++)
                {
                    var anim = animationHandlers[i];
                    anim.Interpolate(time - anim.offsetTime);
                }
            }
        };

        /// <summary>
        /// Function to run when the animation is done.
        /// </summary>
        public Action onComplete;

        /// <summary>
        /// If the animation is playing or not.
        /// </summary>
        public bool playing = false;

        /// <summary>
        /// If all sequences have completed.
        /// </summary>
        public bool Completed => animationHandlers.All(x => x.completed);

        #endregion

        public override string ToString() => $"{id} - {name}";
    }

    /// <summary>
    /// Animation handler. Used for storing a <see cref="Sequence{T}"/> and a delegate to apply the sequence to.
    /// </summary>
    public class AnimationHandler<T> : AnimationHandlerBase
    {
        public AnimationHandler(Sequence<T> sequence, Action<T> interpolation, Action onComplete = null) : base(onComplete)
        {
            this.sequence = sequence;
            this.interpolation = interpolation;
        }

        public AnimationHandler(List<IKeyframe<T>> keyframes, Action<T> interpolation, Action onComplete = null) : base(onComplete)
        {
            sequence = new Sequence<T>(keyframes);
            this.interpolation = interpolation;
        }

        /// <summary>
        /// Inner sequence of the animation handler.
        /// </summary>
        public Sequence<T> sequence;

        /// <summary>
        /// Custom interpolation function.
        /// </summary>
        public Action<T> interpolation;

        /// <summary>
        /// Interpolates the animation sequence.
        /// </summary>
        /// <param name="t">Time scale.</param>
        public override void Interpolate(float t) => interpolation?.Invoke(sequence.Interpolate(t));

        /// <summary>
        /// Total length of the animation handler.
        /// </summary>
        public override float Length => sequence.keyframes.Max(x => x.Time);
    }

    public abstract class AnimationHandlerBase
    {
        public AnimationHandlerBase(Action onComplete = null)
        {
            this.onComplete = onComplete;
        }

        /// <summary>
        /// Function to run when this specific animation sequence is finished animating.
        /// </summary>
        public Action onComplete;

        /// <summary>
        /// If the animation sequence has finished animating.
        /// </summary>
        public bool completed;

        /// <summary>
        /// If the animation sequence should interpolate when it ends.
        /// </summary>
        public bool interpolateOnComplete;

        /// <summary>
        /// The start offset of the animation sequence.
        /// </summary>
        public float offsetTime;

        /// <summary>
        /// Sets <see cref="completed"/> to true and invokes <see cref="onComplete"/>.
        /// </summary>
        public void Completed()
        {
            if (completed)
                return;

            completed = true;
            onComplete?.Invoke();
        }

        public abstract void Interpolate(float t);
        public abstract float Length { get; }
    }
}
