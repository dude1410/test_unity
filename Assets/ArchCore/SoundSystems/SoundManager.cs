using System;
using System.Collections.Generic;
using System.Linq;
using ArchCore.Utils;
using UnityEngine;
using ArchCore.Utils.Executions;
using UnityEditor;
using UnityEngine.Audio;

namespace ArchCore.SoundSystems
{
	public enum SoundPriorityType
	{
		Low = 0,
		Medium = 1,
		High = 2
	}
	
	public abstract class SoundManager<T> : MonoBehaviour where T : Enum
	{
		[SerializeField] private AudioClip[] clips;

		[Header("audio mixers")]
		[SerializeField] private AudioMixerGroup defaultMixer = null;
		[SerializeField] private AudioMixerGroup musicMixer = null;

		const int START_CHANNEL_COUNT = 5;
		const int MAX_CHANNEL_COUNT = 10;
		const float VOLUME = 1f;

		private List<AudioChannel> channels;
		private AudioChannel musicChannel;

		protected bool isLogsAllowed = false;

		protected abstract int GetIndex(T value);
		
		protected bool sfxOn;

		public virtual bool IsSfxOn
		{
			get => sfxOn;
			protected set
			{
				sfxOn = value;
				if (isLogsAllowed)
					Debug.Log($"{this} sfxOn={sfxOn}");

				UpdateChannelsMute();
			}
		}

		protected bool musicOn;

		public virtual bool IsMusicOn
		{
			get => musicOn;
			protected set
			{
				musicOn = value;
				if (isLogsAllowed)
					Debug.Log($"{this} musicOn={sfxOn}");

				UpdateChannelsMute();
			}
		}

		private AudioChannel CreateChannel(AudioMixerGroup audioMixerGroup)
		{
			AudioSource newSource = gameObject.AddComponent<AudioSource>();
			newSource.playOnAwake = false;
			newSource.loop = false;
			newSource.outputAudioMixerGroup = audioMixerGroup;
			AudioChannel newChannel = new AudioChannel(newSource);
			newChannel.Mute = !sfxOn;

			if (isLogsAllowed)
				Debug.Log($"{this} created channel={newChannel} source={newSource} mixer={audioMixerGroup}");

			return newChannel;
		}
		
		public AudioToken Play(T audioClip, SoundPriorityType priority = SoundPriorityType.Medium,
			bool loop = false)
		{

			AudioChannel free = channels.FirstOrDefault(source => !source.IsBusy);

			if (free == null)
			{
				if (channels.Count < MAX_CHANNEL_COUNT)
				{
					free = CreateChannel(defaultMixer);
					channels.Add(free);
				}
				else
				{
					for (int i = 0; i < (int) priority; i++)
					{
						foreach (var source in channels)
						{
							if (source.Priority == (SoundPriorityType) i)
							{
								free = source;
								break;
							}
						}
						if (free != null) break;
					}

					if (free == null)
					{
						Debug.LogWarning("Too much audio effects at a time, ignoring " + audioClip);
						return AudioToken.ExpiredToken;
					}
				}
			}

			free.Volume = VOLUME;
			free.Mute = !sfxOn;

			if (isLogsAllowed)
				Debug.Log($"{this} play={audioClip} priority={priority} loop={loop} free={free} volume={free.Volume}");

			return free.Play(clips[GetIndex(audioClip)], priority, loop);
		}

		public AudioToken MusicToken => musicChannel.Token ?? AudioToken.ExpiredToken;
		public AudioToken PlayMusic(T audioClip, bool loop = true)
		{
			if (musicChannel.IsBusy && musicChannel.Clip == clips[GetIndex(audioClip)])
			{
				if (isLogsAllowed)
					Debug.Log($"{this} play music cancel. already played={audioClip}");

				return null;
			}

			musicChannel.Volume = VOLUME;
			musicChannel.Mute = !musicOn;

			if (isLogsAllowed)
				Debug.Log($"{this} play music={audioClip} loop={loop} channel={musicChannel} volume={musicChannel.Volume}");

			return musicChannel.Play(clips[GetIndex(audioClip)], SoundPriorityType.High, loop);
		}

		public ActionExe PlayExe(T audioClip, SoundPriorityType priority = SoundPriorityType.Medium, bool loop = false)
		{
			if (isLogsAllowed)
				Debug.Log($"{this} play exe={audioClip} priority={priority} loop={loop}");

			return new ActionExe(() => Play(audioClip, priority, loop));
		}

		public AudioToken GetPlayingSound(T audioClip)
		{
			AudioClip cl = clips[GetIndex(audioClip)];
			var ch = channels.FirstOrDefault(c => c.Clip == cl);

			return ch?.Token;
		}

#if UNITY_EDITOR
		// private void OnValidate()
		// {
		// 	EnumGenerator.GenerateEnum("AudioClips", clips.Select(clip=>clip.name).ToArray());
		// }
		[Header("editor only")]
		[SerializeField] private MonoScript audioClips;

		protected void GenerateEnum()
		{
			EnumGenerator.GenerateEnum(audioClips.name,clips.Select(clip => clip.name).ToArray(), AssetDatabase.GetAssetPath(audioClips));
		}
#else
		 //avoid build error: Error Unity A scripted object (probably TBR.SoundSystems.SoundManager?) has a different serialization layout when loading. (Read 432 bytes but expected 444 bytes)
		[SerializeField] private object audioClips;
#endif

		protected void Init()
		{
			channels = new List<AudioChannel>();
			for (int i = 0; i < START_CHANNEL_COUNT; i++)
			{
				channels.Add(CreateChannel(defaultMixer));
			}

			musicChannel = CreateChannel(musicMixer);

			if (isLogsAllowed)
				Debug.Log($"{this} init. channels=[{channels.Count}] music={musicChannel} details:\n{string.Join("\n", channels)}");

			UpdateChannelsMute();
		}

		protected virtual void UpdateChannelsMute()
		{
			if(channels == null) return;
			
			foreach (var channel in channels)
			{
				channel.Mute = !sfxOn;
			}

			musicChannel.Mute = !musicOn;

			if (isLogsAllowed)
				Debug.Log($"{this} update channels mute. sfxOn={sfxOn} musicOn={musicOn}");
		}
	}
}