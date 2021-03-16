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

        readonly int alBuffer;
        readonly int alSource;

        bool isInitialized;
        bool isPlaying;

        const float SAMPLE_RATE = 44100;
        const float FREQUENCY = 440;

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

                AL.GenBuffer(out alBuffer);
                short[] wave = GenerateSquareWave(FREQUENCY, SAMPLE_RATE);
                AL.BufferData(alBuffer, ALFormat.Mono16, ref wave[0], wave.Length * sizeof(short), (int)SAMPLE_RATE);

                CheckALError("After generating buffer");

                AL.Listener(ALListenerf.Gain, 0.1f);

                AL.GenSource(out alSource);
                AL.Source(alSource, ALSourcef.Gain, 1f);
                AL.Source(alSource, ALSourcei.Buffer, alBuffer);
                AL.Source(alSource, ALSourceb.Looping, true);
                //AL.SourcePlay(alSource);

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

            AL.SourceStop(alSource);

            AL.DeleteSource(alSource);
            AL.DeleteBuffer(alBuffer);

            ALC.MakeContextCurrent(ALContext.Null);
            ALC.DestroyContext(context);
            ALC.CloseDevice(device);
        }

        public void Update(SoundState soundState)
        {

        }

        public void Play()
        {
            if (!isInitialized) return;

            if (isPlaying) return;
            isPlaying = true;

            AL.SourcePlay(alSource);
            //AL.Source(alSource, ALSourcef.Gain, 1f);
        }

        public void Stop()
        {
            if (!isInitialized) return;

            if (!isPlaying) return;
            isPlaying = false;

            AL.SourceStop(alSource);
            //AL.Source(alSource, ALSourcef.Gain, 0f);
        }

        static void CheckALError(string str)
        {
            ALError error = AL.GetError();
            if (error != ALError.NoError)
            {
                Utils.Log(LogType.Error, $"ALError at '{str}': {AL.GetErrorString(error)}");
            }
        }

        static short[] GenerateSquareWave(float frequency, float sampleRate)
        {
            float waveLength = 1 / frequency;
            short[] buffer = new short[(int)(waveLength * sampleRate)];

            for (int b = 0; b < buffer.Length; b++)
            {
                float sin = MathF.Sin((b * frequency * MathF.PI * 2) / sampleRate);
                //buffer[b] = (short)(sin * short.MaxValue);
                buffer[b] = sin >= 0 ? short.MaxValue : short.MinValue;
            }

            return buffer;
        }
    }
}