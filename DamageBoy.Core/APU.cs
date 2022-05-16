using DamageBoy.Core.Audio;
using DamageBoy.Core.State;
using System;
using System.IO;

namespace DamageBoy.Core
{
    class APU : IState
    {
        readonly Action<ushort[]> soundUpdateCallback;

        readonly SoundChannel[] soundChannels;
        readonly ushort[] channelData;

        public PulseChannel Channel1 => soundChannels[0] as PulseChannel;
        public PulseChannel Channel2 => soundChannels[1] as PulseChannel;
        public WaveChannel Channel3 => soundChannels[2] as WaveChannel;
        public NoiseChannel Channel4 => soundChannels[3] as NoiseChannel;

        int sampleClocksToWait;
        int lengthControlClocksToWait;
        int volumeEnvelopeClocksToWait;
        int sweepClocksToWait;

        const int LENGTH_CONTROL_INTERVAL_HZ = 256;
        const int VOLUME_ENVELOPE_INTERVAL_HZ = 64;
        const int SWEEP_INTERVAL_HZ = 128;

        public APU(Action<ushort[]> soundUpdateCallback)
        {
            this.soundUpdateCallback = soundUpdateCallback;

            soundChannels = new SoundChannel[Constants.SOUND_CHANNEL_COUNT];
            soundChannels[0] = new PulseChannel(this);
            soundChannels[1] = new PulseChannel(this);
            soundChannels[2] = new WaveChannel(this);
            soundChannels[3] = new NoiseChannel(this);

            channelData = new ushort[Constants.SOUND_CHANNEL_COUNT];
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

            channelData[0] = soundChannels[0].Process(updateSample, updateLength, updateVolume, updateSweep);
            channelData[1] = soundChannels[1].Process(updateSample, updateLength, updateVolume, false);
            channelData[2] = soundChannels[2].Process(updateSample, updateLength, false, false);
            channelData[3] = soundChannels[3].Process(false, updateLength, updateVolume, false);

            if (updateSample)
            {
                soundUpdateCallback?.Invoke(channelData);
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
    }
}