using DamageBoy.Core.Audio;
using DamageBoy.Core.State;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace DamageBoy.Core;

class APU : IState
{
    readonly Action<byte, byte> addToAudioBufferCallback;

    readonly SoundChannel[] soundChannels;

    public PulseChannel Channel1 => soundChannels[0] as PulseChannel;
    public PulseChannel Channel2 => soundChannels[1] as PulseChannel;
    public WaveChannel Channel3 => soundChannels[2] as WaveChannel;
    public NoiseChannel Channel4 => soundChannels[3] as NoiseChannel;

    public bool Channel1Enabled { get; set; }
    public bool Channel2Enabled { get; set; }
    public bool Channel3Enabled { get; set; }
    public bool Channel4Enabled { get; set; }

    int sampleClocksToWait;
    int lengthControlClocksToWait;
    int volumeEnvelopeClocksToWait;
    int sweepClocksToWait;

    const int LENGTH_CONTROL_INTERVAL_HZ = 256;
    const int VOLUME_ENVELOPE_INTERVAL_HZ = 64;
    const int SWEEP_INTERVAL_HZ = 128;

    public APU(Action<byte, byte> addToAudioBufferCallback)
    {
        this.addToAudioBufferCallback = addToAudioBufferCallback;

        soundChannels = new SoundChannel[Constants.SOUND_CHANNEL_COUNT];
        soundChannels[0] = new PulseChannel(this);
        soundChannels[1] = new PulseChannel(this);
        soundChannels[2] = new WaveChannel(this);
        soundChannels[3] = new NoiseChannel(this);
    }

    public void Update()
    {
        bool updateSample = false;
        bool updateLength = false;
        bool updateVolume = false;
        bool updateSweep = false;

        sampleClocksToWait -= 4;
        if (sampleClocksToWait <= 0)
        {
            sampleClocksToWait = CPU.CPU_CLOCKS / Constants.SAMPLE_RATE;
            updateSample = true;
        }

        lengthControlClocksToWait -= 4;
        if (lengthControlClocksToWait <= 0)
        {
            lengthControlClocksToWait = CPU.CPU_CLOCKS / LENGTH_CONTROL_INTERVAL_HZ;
            updateLength = true;
        }

        volumeEnvelopeClocksToWait -= 4;
        if (volumeEnvelopeClocksToWait <= 0)
        {
            volumeEnvelopeClocksToWait = CPU.CPU_CLOCKS / VOLUME_ENVELOPE_INTERVAL_HZ;
            updateVolume = true;
        }

        sweepClocksToWait -= 4;
        if (sweepClocksToWait <= 0)
        {
            sweepClocksToWait = CPU.CPU_CLOCKS / SWEEP_INTERVAL_HZ;
            updateSweep = true;
        }

        float value1 = soundChannels[0].Process(updateSample, updateLength, updateVolume, updateSweep);
        float value2 = soundChannels[1].Process(updateSample, updateLength, updateVolume, false);
        float value3 = soundChannels[2].Process(updateSample, updateLength, false, false);
        float value4 = soundChannels[3].Process(false, updateLength, updateVolume, false);

        if (updateSample)
        {
            (float leftValue1, float rightValue1) = Channel1Enabled ? ProcessStereo(soundChannels[0], value1) : (SoundChannel.WAVE_SILENCE, SoundChannel.WAVE_SILENCE);
            (float leftValue2, float rightValue2) = Channel2Enabled ? ProcessStereo(soundChannels[1], value2) : (SoundChannel.WAVE_SILENCE, SoundChannel.WAVE_SILENCE);
            (float leftValue3, float rightValue3) = Channel3Enabled ? ProcessStereo(soundChannels[2], value3) : (SoundChannel.WAVE_SILENCE, SoundChannel.WAVE_SILENCE);
            (float leftValue4, float rightValue4) = Channel4Enabled ? ProcessStereo(soundChannels[3], value4) : (SoundChannel.WAVE_SILENCE, SoundChannel.WAVE_SILENCE);

            float leftValue = (leftValue1 + leftValue2 + leftValue3 + leftValue4) / 4f;
            float rightValue = (rightValue1 + rightValue2 + rightValue3 + rightValue4) / 4f;

            addToAudioBufferCallback((byte)(leftValue * 128 + 127), (byte)(rightValue * 128 + 127));
        }
    }

    #region Sound Control Registers

    // FF24 - NR50 - Channel control / ON-OFF / Volume (R/W)

    public byte Output1Level;
    public bool VinOutput1;
    public byte Output2Level;
    public bool VinOutput2;

    // FF26 - NR52 - Sound on/off

    bool allSoundEnabled;
    public bool AllSoundEnabled
    {
        get => allSoundEnabled;
        set
        {
            if (allSoundEnabled && !value)
            {
                for (int sc = 0; sc < Constants.SOUND_CHANNEL_COUNT; sc++)
                {
                    soundChannels[sc].Reset();
                }

                ResetControlRegisters();
            }

            allSoundEnabled = value;
        }
    }

    void ResetControlRegisters()
    {
        Output1Level = 0;
        VinOutput1 = false;
        Output2Level = 0;
        VinOutput2 = false;

        for (int sc = 0; sc < Constants.SOUND_CHANNEL_COUNT; sc++)
        {
            soundChannels[sc].Output1 = false;
            soundChannels[sc].Output2 = false;
            soundChannels[sc].Enabled = false;
        }
    }

    #endregion

    public void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save)
    {
        sampleClocksToWait = SaveState.SaveLoadValue(bw, br, save, sampleClocksToWait);
        lengthControlClocksToWait = SaveState.SaveLoadValue(bw, br, save, lengthControlClocksToWait);
        volumeEnvelopeClocksToWait = SaveState.SaveLoadValue(bw, br, save, volumeEnvelopeClocksToWait);
        sweepClocksToWait = SaveState.SaveLoadValue(bw, br, save, sweepClocksToWait);

        Output1Level = SaveState.SaveLoadValue(bw, br, save, Output1Level);
        VinOutput1 = SaveState.SaveLoadValue(bw, br, save, VinOutput1);
        Output2Level = SaveState.SaveLoadValue(bw, br, save, Output2Level);
        VinOutput2 = SaveState.SaveLoadValue(bw, br, save, VinOutput2);

        AllSoundEnabled = SaveState.SaveLoadValue(bw, br, save, AllSoundEnabled);

        for (int sc = 0; sc < Constants.SOUND_CHANNEL_COUNT; sc++)
        {
            soundChannels[sc].SaveOrLoadState(stream, bw, br, save);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    (float, float) ProcessStereo(SoundChannel soundChannel, float value)
    {
        float leftValue = soundChannel.Output2 ? value * (Output2Level / 7f) : 0f;
        float rightValue = soundChannel.Output1 ? value * (Output1Level / 7f) : 0f;

        return (leftValue, rightValue);
    }
}