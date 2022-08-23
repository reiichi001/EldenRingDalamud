﻿using System;
using System.Collections.Generic;
using System.Linq;
using NAudio;
using NAudio.Extras;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace EldenRing.Audio
{
    public enum AudioTrigger
    {
        Death
    }



    class CachedSoundSampleProvider : ISampleProvider
    {
        private readonly CachedSound cachedSound;
        private long position;

        public CachedSoundSampleProvider(CachedSound cachedSound)
        {
            this.cachedSound = cachedSound;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var availableSamples = cachedSound.AudioData.Length - position;
            var samplesToCopy = Math.Min(availableSamples, count);
            Array.Copy(cachedSound.AudioData, position, buffer, offset, samplesToCopy);
            position += samplesToCopy;
            return (int)samplesToCopy;
        }

        public WaveFormat WaveFormat { get { return cachedSound.WaveFormat; } }
    }

    public class AudioHandler
    {
        private readonly IWavePlayer outputDevice;
        private readonly Dictionary<AudioTrigger, CachedSound> sounds;
        private readonly VolumeSampleProvider sampleProvider;
        private readonly MixingSampleProvider mixer;

        public float Volume
        {
            get => sampleProvider.Volume;
            set
            {
                sampleProvider.Volume = value;
                
            }
        }

        public AudioHandler(string deathPath)
        {
            // outputDevice = new WaveOutEvent();
            outputDevice = new DirectSoundOut();
            sounds = new Dictionary<AudioTrigger, CachedSound>();
            sounds.Add(AudioTrigger.Death, new(deathPath));
            mixer = new(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));
            mixer.ReadFully = true;
            sampleProvider = new(mixer);
            outputDevice.Init(sampleProvider);
            outputDevice.Play();
        }

        private ISampleProvider ConvertToRightChannelCount(ISampleProvider input)
        {
            if (input.WaveFormat.Channels == mixer.WaveFormat.Channels)
            {
                return input;
            }
            if (input.WaveFormat.Channels == 1 && mixer.WaveFormat.Channels == 2)
            {
                return new MonoToStereoSampleProvider(input);
            }
            throw new NotImplementedException("Not yet implemented this channel count conversion");
        }


        private void AddMixerInput(ISampleProvider input)
        {
            mixer.AddMixerInput(ConvertToRightChannelCount(input));
        }

        public void PlaySound(AudioTrigger trigger)
        {
            AddMixerInput(new CachedSoundSampleProvider(sounds[trigger]));
        }
        
    }
}
