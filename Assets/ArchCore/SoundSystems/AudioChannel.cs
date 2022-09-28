using UnityEngine;
using ArchCore.Utils.Executions;

namespace ArchCore.SoundSystems
{
	public interface IAudioController
	{
		AudioToken Play(AudioClip clip, SoundPriorityType priority = SoundPriorityType.Medium, bool loop = false);
		void Stop();
		void Pause();
		void Resume();
		float Volume { get; set; }
		bool Mute { get; set; }

	}

	public class AudioToken : IAudioController
	{
		static AudioToken()
		{
			expiredToken = new AudioToken(null);
			expiredToken.Expire();
		}

		private static AudioToken expiredToken;

		public static AudioToken ExpiredToken => expiredToken;

		private bool expired = false;
		private readonly AudioChannel channel;

		public bool IsValid => !expired;
		public SoundPriorityType Priority => channel.Priority;

		public AudioToken(AudioChannel channel)
		{
			this.channel = channel;
		}

		public float Volume
		{
			get => channel.Volume;
			set
			{
				if (channel != null)
					channel.Volume = value;
			}
		}

		public bool Mute
		{
			get => channel.Mute;
			set
			{
				if (channel != null)
					channel.Mute = value;
			}
		}

		public AudioToken Play(AudioClip clip, SoundPriorityType priority = SoundPriorityType.Medium, bool loop = false)
		{
			if (expired)
			{
				Debug.LogWarning("Expired Audio Token");
				return null;
			}

			channel.Play(clip, priority, loop);

			return this;
		}

		public void Stop()
		{
			if (expired)
			{
				Debug.LogWarning("Expired Audio Token");
				return;
			}

			channel.Stop();

		}

		public void Pause()
		{
			if (expired)
			{
				Debug.LogWarning("Expired Audio Token");
				return;
			}

			channel.Pause();
		}

		public void Resume()
		{
			if (expired)
			{
				Debug.LogWarning("Expired Audio Token");
				return;
			}

			channel.Resume();
		}

		public void Expire()
		{
			expired = true;
		}


	}

	public class AudioChannel : IAudioController
	{
		private readonly AudioSource source;
		private AudioToken token;

		private bool paused;

		public bool IsPaused => paused;
		public SoundPriorityType Priority => priority;
		private SoundPriorityType priority;

		public AudioClip Clip
		{
			get
			{
				CheckTokenForExpire();
				return source.clip;
			}
		}

		public bool IsBusy
		{
			get
			{
				CheckTokenForExpire();
				return token != null && token.IsValid;
			}
		}

		private void CheckTokenForExpire()
		{
			if (token == null || !token.IsValid) return;
			if (!source.isPlaying && !paused)
			{
				token.Expire();
				source.clip = null;
				token = null;
			}
		}

		public AudioToken Token => token;

		public AudioChannel(AudioSource source)
		{
			this.source = source;
		}

		public float Volume
		{
			get => source.volume;
			set => source.volume = Mathf.Clamp01(value);
		}

		public bool Mute
		{
			get => source.mute;
			set => source.mute = value;
		}

		public AudioToken Play(AudioClip clip, SoundPriorityType priority = SoundPriorityType.Medium, bool loop = false)
		{
			CheckTokenForExpire();
			if (token != null)
			{
				Debug.LogWarning("Playing on channel with unexpired token");
				token.Expire();
			}

			source.Stop();
			source.clip = clip;
			source.loop = loop;
			source.Play();

			paused = false;
			this.priority = priority;
			token = new AudioToken(this);
			return token;
		}

		public void Stop()
		{
			source.Stop();
			token.Expire();
			token = null;
		}

		public void Pause()
		{
			paused = true;
			source.Pause();
		}

		public void Resume()
		{
			paused = false;
			source.Play();
		}


	}
}