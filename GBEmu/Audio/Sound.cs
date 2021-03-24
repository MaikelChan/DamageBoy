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

        readonly SoundChannel[] soundChannels;

        bool isInitialized;

        public enum BufferStates { Uninitialized, Ok, Underrun, Overrun }

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

                AL.Listener(ALListenerf.Gain, 0.15f);

                soundChannels = new SoundChannel[APU.SOUND_CHANNEL_COUNT];
                for (int sc = 0; sc < APU.SOUND_CHANNEL_COUNT; sc++)
                {
                    soundChannels[sc] = new SoundChannel();
                }

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

            for (int sc = 0; sc < APU.SOUND_CHANNEL_COUNT; sc++)
            {
                soundChannels[sc].Dispose();
            }

            ALC.MakeContextCurrent(ALContext.Null);
            ALC.DestroyContext(context);
            ALC.CloseDevice(device);
        }

        public BufferStates Update(byte[] data)
        {
            if (!isInitialized) return BufferStates.Uninitialized;

            // Sometimes, not all channels are in the same state,
            // so wait a bit until they are and then exit the loop.
            // 16 iteration should be more than enough to guarantee
            // the channels being in the same state.
            // (Normally it's just 1 iteration)

            for (int i = 0; i < 16; i++)
            {
                for (int sc = 0; sc < APU.SOUND_CHANNEL_COUNT; sc++)
                {
                    if (i == 0)
                        soundChannels[sc].ProcessChannel(data == null ? null : data[sc]);
                    else
                        soundChannels[sc].ProcessChannel(null);
                }

                if (soundChannels[0].BufferState == soundChannels[1].BufferState &&
                    soundChannels[0].BufferState == soundChannels[2].BufferState &&
                    soundChannels[0].BufferState == soundChannels[3].BufferState)
                {
                    break;
                }
            }

            return soundChannels[0].BufferState;
        }

        public void Stop()
        {
            for (int sc = 0; sc < APU.SOUND_CHANNEL_COUNT; sc++)
            {
                soundChannels[sc].DeleteSource();
            }
        }

        static void CheckALError(string str)
        {
            ALError error = AL.GetError();
            if (error != ALError.NoError)
            {
                Utils.Log(LogType.Error, $"ALError at '{str}': {AL.GetErrorString(error)}");
            }
        }

        //static byte[] GenerateSquareWave(float frequency, Core.APU.WavePatternDuties wavePattern, float sampleRate)
        //{
        //    float waveLength = 1 / frequency;
        //    int bufferLength = Math.Max(1, (int)(waveLength * sampleRate));
        //    byte[] buffer = new byte[bufferLength];

        //    float percentage;
        //    switch (wavePattern)
        //    {
        //        default:
        //        case Core.APU.WavePatternDuties.Percent12_5: percentage = 0.875f; break;
        //        case Core.APU.WavePatternDuties.Percent25: percentage = 0.75f; break;
        //        case Core.APU.WavePatternDuties.Percent50: percentage = 0.50f; break;
        //        case Core.APU.WavePatternDuties.Percent75: percentage = 0.25f; break;
        //    }

        //    percentage = percentage * 2 - 1;

        //    for (int b = 0; b < buffer.Length; b++)
        //    {
        //        float sin = MathF.Sin((b * frequency * MathF.PI * 2) / sampleRate);
        //        //buffer[b] = (short)(sin * byte.MaxValue);
        //        buffer[b] = sin >= percentage ? byte.MaxValue : byte.MinValue;
        //    }

        //    return buffer;
        //}
    }
}