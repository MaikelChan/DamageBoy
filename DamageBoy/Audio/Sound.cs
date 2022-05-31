using DamageBoy.Core;
using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DamageBoy.Audio;

class Sound : IDisposable
{
    readonly Settings settings;

    readonly ALDevice device;
    readonly ALContext context;

    readonly SoundChannel soundChannel;

    bool audioLoopRunning;
    bool isInitialized;

    float currentVolume;

    readonly bool ALC_EXT_disconnect;
    const int ALC_CONNECTED = 0x313;

    public enum BufferStates { Uninitialized, Ok, Underrun, Overrun }

    public Sound(Settings settings, Action<BufferStates> bufferStateChangeCallback)
    {
        this.settings = settings;

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
            string alExt = AL.Get(ALGetString.Extensions);

            string alcExt = ALC.GetString(device, AlcGetString.Extensions);
            ALContextAttributes attrs = ALC.GetContextAttributes(device);
            ALC.GetInteger(device, AlcGetInteger.MajorVersion, 1, out int alcMajorVersion);
            ALC.GetInteger(device, AlcGetInteger.MinorVersion, 1, out int alcMinorVersion);

            Utils.Log(LogType.Info, $"Vendor: {vend}");
            Utils.Log(LogType.Info, $"Version: {vers}");
            Utils.Log(LogType.Info, $"Renderer: {rend}");
            Utils.Log(LogType.Info, $"Extensions: {alExt}");

            Utils.Log(LogType.Info, $"ALC Extensions: {alcExt}");
            Utils.Log(LogType.Info, $"ALC Attributes: {attrs}");
            Utils.Log(LogType.Info, $"ALC Version: {alcMajorVersion}.{alcMinorVersion}");

            ALC_EXT_disconnect = ALC.IsExtensionPresent(device, "ALC_EXT_disconnect");

            currentVolume = settings.Data.AudioVolume;
            AL.Listener(ALListenerf.Gain, currentVolume);

            soundChannel = new SoundChannel(bufferStateChangeCallback);

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

        Stop();
        soundChannel.Dispose();

        ALC.MakeContextCurrent(ALContext.Null);
        ALC.DestroyContext(context);
        ALC.CloseDevice(device);

        isInitialized = false;
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

    public void AddToAudioBuffer(byte leftValue, byte rightValue)
    {
        if (!isInitialized) return;

        soundChannel.AddToAudioBuffer(leftValue, rightValue);
    }

    void AudioLoop()
    {
        soundChannel.ClearBuffer();

        audioLoopRunning = true;

        while (isInitialized && audioLoopRunning)
        {
            if (ALC_EXT_disconnect)
            {
                bool deviceConnected = ALC.GetInteger(device, (AlcGetInteger)ALC_CONNECTED) > 0;
                if (!deviceConnected)
                {
                    Utils.Log(LogType.Error, "Lost connection to OpenAL device.");
                    audioLoopRunning = false;
                    Dispose();
                    return;
                }
            }

            Update();

            Thread.Sleep(1);
        }

        soundChannel.DeleteSource();
    }

    void Update()
    {
        if (!isInitialized) return;

        if (currentVolume != settings.Data.AudioVolume)
        {
            currentVolume = settings.Data.AudioVolume;
            AL.Listener(ALListenerf.Gain, currentVolume);
        }

        soundChannel.Update();
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