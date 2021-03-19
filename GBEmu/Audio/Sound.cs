using GBEmu.Core;
using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;

namespace GBEmu.Audio
{
    class Sound : IDisposable
    {
        readonly ALDevice device;
        readonly ALContext context;

        readonly int[] alBuffers;
        readonly int[] alSources;
        //readonly bool[] sourcesPlaying;

        bool isInitialized;
        SoundState previousState;

        const int SOUND_CHANNELS = 4;

        const float SAMPLE_RATE = 44100;

        public Sound()
        {
            try
            {
                IEnumerable<string> devices = ALC.GetStringList(GetEnumerationStringList.DeviceSpecifier);
                Utils.Log(LogType.Info, $"Audio Devices: {string.Join(", ", devices)}");

                // Get the default device, then go though all devices and select the AL soft device if it exists.
                string deviceName = ALC.GetString(ALDevice.Null, AlcGetString.DefaultDeviceSpecifier);
                foreach (var d in devices)
                {
                    if (d.Contains("OpenAL Soft"))
                    {
                        deviceName = d;
                        break;
                    }
                }

                device = ALC.OpenDevice(deviceName);
                context = ALC.CreateContext(device, (int[])null);
                ALC.MakeContextCurrent(context);

                CheckALError("Start");

                string vend = AL.Get(ALGetString.Vendor);
                string vers = AL.Get(ALGetString.Version);
                string rend = AL.Get(ALGetString.Renderer);
                ALContextAttributes attrs = ALC.GetContextAttributes(device);
                ALC.GetInteger(device, AlcGetInteger.MajorVersion, 1, out int alcMajorVersion);
                ALC.GetInteger(device, AlcGetInteger.MinorVersion, 1, out int alcMinorVersion);

                Utils.Log(LogType.Info, $"Vendor: {vend}");
                Utils.Log(LogType.Info, $"Version: {vers}");
                Utils.Log(LogType.Info, $"Renderer: {rend}");
                Utils.Log(LogType.Info, $"Attributes: {attrs}");
                Utils.Log(LogType.Info, $"ALC Version: {alcMajorVersion}.{alcMinorVersion}");

                CheckALError("Before generating buffer");

                alBuffers = new int[SOUND_CHANNELS];
                AL.GenBuffers(SOUND_CHANNELS, alBuffers);

                CheckALError("After generating buffer");

                AL.Listener(ALListenerf.Gain, 0.1f);

                alSources = new int[SOUND_CHANNELS];
                //sourcesPlaying = new bool[SOUND_CHANNELS];

                AL.GenSources(SOUND_CHANNELS, alSources);
                AL.Source(alSources[0], ALSourcef.Gain, 1f);
                AL.Source(alSources[0], ALSourceb.Looping, true);
                AL.Source(alSources[1], ALSourcef.Gain, 1f);
                AL.Source(alSources[1], ALSourceb.Looping, true);
                AL.Source(alSources[2], ALSourcef.Gain, 1f);
                AL.Source(alSources[2], ALSourceb.Looping, true);
                AL.Source(alSources[3], ALSourcef.Gain, 1f);
                AL.Source(alSources[3], ALSourceb.Looping, true);

                isInitialized = true;
            }
            catch (DllNotFoundException ex)
            {
                Utils.Log(LogType.Error, ex.Message);
                Utils.Log(LogType.Error, "OpenAL required. No audio will be played.");
            }
        }

        public void Dispose()
        {
            if (!isInitialized) return;
            isInitialized = false;

            Stop();

            AL.DeleteSources(alSources);
            AL.DeleteBuffers(alBuffers);

            ALC.MakeContextCurrent(ALContext.Null);
            ALC.DestroyContext(context);
            ALC.CloseDevice(device);
        }

        public void Update(SoundState state)
        {
            if (!isInitialized) return;

            // Channel 1

            if (previousState.Channel1Frequency != state.Channel1Frequency)
            {
                AL.SourceStop(alSources[0]);
                //sourcesPlaying[0] = false;

                AL.Source(alSources[0], ALSourcei.Buffer, 0);

                byte[] wave = GenerateSquareWave(state.Channel1Frequency, state.Channel1WavePattern, SAMPLE_RATE);
                AL.BufferData(alBuffers[0], ALFormat.Mono8, ref wave[0], wave.Length * sizeof(byte), (int)SAMPLE_RATE);

                AL.Source(alSources[0], ALSourcei.Buffer, alBuffers[0]);

                if (state.Channel1Enabled) AL.SourcePlay(alSources[0]);
            }

            if (previousState.Channel1Enabled != state.Channel1Enabled)
            {
                if (state.Channel1Enabled) AL.SourcePlay(alSources[0]);
                else AL.SourceStop(alSources[0]);
            }

            AL.Source(alSources[0], ALSourcef.Gain, state.Channel1Volume);

            // Channel 2

            if (previousState.Channel2Frequency != state.Channel2Frequency)
            {
                AL.SourceStop(alSources[1]);
                //sourcesPlaying[1] = false;

                AL.Source(alSources[1], ALSourcei.Buffer, 0);

                byte[] wave = GenerateSquareWave(state.Channel2Frequency, state.Channel2WavePattern, SAMPLE_RATE);
                AL.BufferData(alBuffers[1], ALFormat.Mono8, ref wave[1], wave.Length * sizeof(byte), (int)SAMPLE_RATE);

                AL.Source(alSources[1], ALSourcei.Buffer, alBuffers[1]);

                if (state.Channel2Enabled) AL.SourcePlay(alSources[1]);
            }

            if (previousState.Channel2Enabled != state.Channel2Enabled)
            {
                if (state.Channel2Enabled) AL.SourcePlay(alSources[1]);
                else AL.SourceStop(alSources[1]);
            }

            AL.Source(alSources[1], ALSourcef.Gain, state.Channel2Volume);

            // Copy current state

            previousState = state;
        }

        public void Stop()
        {
            previousState = default;

            AL.SourceStop(alSources[0]);
            AL.SourceStop(alSources[1]);
            AL.SourceStop(alSources[2]);
            AL.SourceStop(alSources[3]);
        }

        static void CheckALError(string str)
        {
            ALError error = AL.GetError();
            if (error != ALError.NoError)
            {
                Utils.Log(LogType.Error, $"ALError at '{str}': {AL.GetErrorString(error)}");
            }
        }

        static byte[] GenerateSquareWave(float frequency, Core.Sound.WavePatternDuties wavePattern, float sampleRate)
        {
            float waveLength = 1 / frequency;
            byte[] buffer = new byte[(int)(waveLength * sampleRate)];

            float percentage;
            switch (wavePattern)
            {
                default:
                case Core.Sound.WavePatternDuties.Percent12_5: percentage = 0.875f; break;
                case Core.Sound.WavePatternDuties.Percent25: percentage = 0.75f; break;
                case Core.Sound.WavePatternDuties.Percent50: percentage = 0.50f; break;
                case Core.Sound.WavePatternDuties.Percent75: percentage = 0.25f; break;
            }

            for (int b = 0; b < buffer.Length; b++)
            {
                float sin = MathF.Sin((b * frequency * MathF.PI * 2) / sampleRate);
                //buffer[b] = (short)(sin * byte.MaxValue);
                buffer[b] = sin >= percentage ? byte.MaxValue : byte.MinValue;
            }

            return buffer;
        }
    }
}