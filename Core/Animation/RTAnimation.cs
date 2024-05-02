using BetterLegacy.Core.Animation.Keyframe;
using LSFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Core.Animation
{
	public class RTAnimation
	{
		public RTAnimation(string name)
		{
			this.name = name;
			id = LSText.randomNumString(16);
			timeOffset = UnityEngine.Time.time;
		}

		public void ResetTime()
		{
			time = 0f;
			timeOffset = useRealTime ? UnityEngine.Time.time : AudioManager.inst.CurrentAudioSource.time;
			for (int i = 0; i < animationHandlers.Count; i++)
				animationHandlers[i].completed = false;
		}

		public void Stop() => playing = false;

		public void Play() => playing = true;

		public void Update()
		{
			Time = useRealTime ? UnityEngine.Time.time - timeOffset : AudioManager.inst.CurrentAudioSource.time - timeOffset;

			if (animationHandlers == null || animationHandlers.Count < 1)
				return;

			for (int i = 0; i < animationHandlers.Count; i++)
			{
				var anim = animationHandlers[i];

				if (anim.Length >= time)
				{
					anim.completed = false;
					anim.Interpolate(time);
				}
				else if (!anim.completed)
				{
					anim.completed = true;
					anim.Completed();
				}
			}

			if (Completed && playing)
			{
				playing = false;
				onComplete?.Invoke();

				if (!loop)
					return;

				ResetTime();
				Play();
			}
		}

		public string id;
		public string name;

		public bool useRealTime = true;

		public bool loop;

		float time;
		public float Time
		{
			get => time;
			private set => time = value;
		}

		float timeOffset;

		public Action onComplete;

		public bool playing = false;

		public bool Completed => animationHandlers.All(x => x.completed);

		public List<AnimationHandlerBase> animationHandlers = new List<AnimationHandlerBase>();
	}

	public class AnimationHandler<T> : AnimationHandlerBase
	{
		public AnimationHandler(List<IKeyframe<T>> keyframes, Action<T> interpolation, Action onComplete = null) : base(onComplete)
		{
			this.keyframes = keyframes;
			sequence = new Sequence<T>(this.keyframes);
			this.interpolation = interpolation;
		}


		public List<IKeyframe<T>> keyframes;

		public Sequence<T> sequence;

		public Action<T> interpolation;

		public override void Interpolate(float t) => interpolation?.Invoke(sequence.Interpolate(t));

		public override float Length => keyframes.Count > 0 ? keyframes.OrderBy(x => x.Time).Last().Time : 0f;
	}

	public abstract class AnimationHandlerBase
	{
		public AnimationHandlerBase(Action onComplete = null)
		{
			this.onComplete = onComplete;
		}

		public float currentTime;

		public Action onComplete;

		public bool completed = false;

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
