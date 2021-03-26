using GBEmu.Core.Audio;
using System;

namespace GBEmu.Core
{
    class APU
    {
        readonly Action<ushort[]> soundUpdateCallback;

        readonly SoundChannel[] soundChannels;

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

        public const int SAMPLE_RATE = CPU.CPU_CLOCKS >> 7; // 32768Hz
        public const int SOUND_CHANNEL_COUNT = 4;

        public APU(Action<ushort[]> soundUpdateCallback)
        {
            this.soundUpdateCallback = soundUpdateCallback;

            soundChannels = new SoundChannel[SOUND_CHANNEL_COUNT];
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
                sampleClocksToWait = CPU.CPU_CLOCKS / SAMPLE_RATE;
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

            ushort[] data = new ushort[SOUND_CHANNEL_COUNT];
            data[0] = soundChannels[0].Process(updateSample, updateLength, updateVolume, updateSweep);
            data[1] = soundChannels[1].Process(updateSample, updateLength, updateVolume, false);
            data[2] = soundChannels[2].Process(updateSample, updateLength, false, false);
            data[3] = soundChannels[3].Process(false, updateLength, updateVolume, false);

            if (updateSample)
            {
                soundUpdateCallback?.Invoke(data);
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
                    for (int sc = 0; sc < SOUND_CHANNEL_COUNT; sc++)
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

            for (int sc = 0; sc < SOUND_CHANNEL_COUNT; sc++)
            {
                soundChannels[sc].Output1 = false;
                soundChannels[sc].Output2 = false;
                soundChannels[sc].Enabled = false;
            }
        }

        #endregion
    }
}