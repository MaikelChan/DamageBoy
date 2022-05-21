using DamageBoy.Core;
using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DamageBoy.Audio
{
    class Sound : IDisposable
    {
        readonly ALDevice device;
        readonly ALContext context;

        readonly SoundChannel soundChannel;

        bool audioLoopRunning;
        bool isInitialized;

        public enum BufferStates { Uninitialized, Ok, Underrun, Overrun }

        public Sound(Func<byte[], bool> fillAudioBufferCallback)
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

                AL.Listener(ALListenerf.Gain, 1.0f);

                soundChannel = new SoundChannel(fillAudioBufferCallback);

                audioLoopRunning = false;
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

            soundChannel.Dispose();

            ALC.MakeContextCurrent(ALContext.Null);
            ALC.DestroyContext(context);
            ALC.CloseDevice(device);
        }

        public void Start()
        {
            if (!isInitialized) return;

            Thread thread = new Thread(AudioLoop);
            thread.Name = "Audio Loop";
            thread.Start();
        }

        public void Stop()
        {
            if (!isInitialized) return;

            audioLoopRunning = false;
        }

        void AudioLoop()
        {
            audioLoopRunning = true;

            while (audioLoopRunning)
            {
                Update();

                Thread.Sleep(1);
            }

            soundChannel.DeleteSource();
        }

        BufferStates Update()
        {
            if (!isInitialized) return BufferStates.Uninitialized;

            soundChannel.ProcessChannel();
            return soundChannel.BufferState;
        }

        static void CheckALError(string str)
        {
            ALError error = AL.GetError();
            if (error != ALError.NoError)
            {
                Utils.Log(LogType.Error, $"ALError at '{str}': {AL.GetErrorString(error)}");
            }
        }
    }
}