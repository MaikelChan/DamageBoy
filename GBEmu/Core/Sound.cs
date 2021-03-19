using System;

namespace GBEmu.Core
{
    class Sound
    {
        readonly Action<SoundState> soundUpdateCallback;

        SoundState state;

        public enum SweepTypes : byte { Increase, Decrease }
        public enum EnvelopeDirections : byte { Decrease, Increase }
        public enum WavePatternDuties : byte { Percent12_5, Percent25, Percent50, Percent75 }
        public enum LengthTypes : byte { Consecutive, Counter }

        const double TIME_INTERVAL = 1d / (4d * 1024d * 1024d);

        int clocksToWait;

        public Sound(Action<SoundState> soundUpdateCallback)
        {
            this.soundUpdateCallback = soundUpdateCallback;

            state = new SoundState();
        }

        public void Update()
        {
            clocksToWait -= 4;
            if (clocksToWait > 0) return;

            clocksToWait = 4;

            ProcessChannel1();
            ProcessChannel2();

            soundUpdateCallback?.Invoke(state);
        }

        #region Sound Control Registers

        // FF24 - NR50 - Channel control / ON-OFF / Volume (R/W)

        public byte Output1Level;
        public bool VinOutput1;
        public byte Output2Level;
        public bool VinOutput2;

        // FF25 - NR51 - Selection of Sound output terminal (R/W)

        public bool Channel1Output1;
        public bool Channel2Output1;
        public bool Channel3Output1;
        public bool Channel4Output1;
        public bool Channel1Output2;
        public bool Channel2Output2;
        public bool Channel3Output2;
        public bool Channel4Output2;

        // FF26 - NR52 - Sound on/off

        public bool Channel1Enabled;
        public bool Channel2Enabled;
        public bool Channel3Enabled;
        public bool Channel4Enabled;
        public bool AllSoundEnabled;

        #endregion

        #region Sound Channel 1 - Tone & Sweep

        // FF10 - NR10 - Channel 1 Sweep register (R/W)

        public byte Channel1SweepShift;
        public SweepTypes Channel1SweepType;
        public byte Channel1SweepTime;

        // FF11 - NR11 - Channel 1 Sound length/Wave pattern duty (R/W)

        public byte Channel1Length;
        public WavePatternDuties Channel1WavePatternDuty;

        // FF12 - NR12 - Channel 1 Volume Envelope (R/W)

        public byte Channel1LengthEnvelopeSteps;
        public EnvelopeDirections Channel1EnvelopeDirection;
        public byte Channel1InitialVolume;

        // FF13 - NR13 - Channel 1 Frequency lo (Write Only)

        public byte Channel1FrequencyLo;

        // FF14 - NR14 - Channel 1 Frequency hi (R/W)

        public byte Channel1FrequencyHi;
        public LengthTypes Channel1LengthType;
        public bool Channel1Initialize
        {
            set
            {
                if (value) InitializeChannel1();
                else StopChannel1();
            }
        }

        // Current state

        double channel1LengthTimer;
        double channel1EnvelopeTimer;
        byte channel1CurrentVolume;

        void ProcessChannel1()
        {
            if (!AllSoundEnabled)
            {
                StopChannel1();
                return;
            }

            channel1LengthTimer -= TIME_INTERVAL;
            if (channel1LengthTimer <= 0d)
            {
                StopChannel1();
                return;
            }

            if (Channel1LengthEnvelopeSteps > 0)
            {
                channel1EnvelopeTimer -= TIME_INTERVAL;
                if (channel1EnvelopeTimer <= 0)
                {
                    channel1EnvelopeTimer = Channel1LengthEnvelopeSteps / 64d;

                    if (Channel1EnvelopeDirection == EnvelopeDirections.Decrease)
                    {
                        channel1CurrentVolume--;
                        if (channel1CurrentVolume < 0) channel1CurrentVolume = 0;
                    }
                    else
                    {
                        channel1CurrentVolume++;
                        if (channel1CurrentVolume > 0xF) channel1CurrentVolume = 0xF;
                    }
                }
            }

            state.Channel1Volume = channel1CurrentVolume / (float)0xF;

            state.Channel1Frequency = 131072f / (2048 - ((Channel1FrequencyHi << 8) | Channel1FrequencyLo));

            state.Channel1WavePattern = Channel1WavePatternDuty;
        }

        void InitializeChannel1()
        {
            channel1LengthTimer = (64d - Channel1Length) / 256d;
            channel1EnvelopeTimer = Channel1LengthEnvelopeSteps / 64d;
            channel1CurrentVolume = Channel1InitialVolume;

            Channel1Enabled = true;
            state.Channel1Enabled = true;
        }

        void StopChannel1()
        {
            Channel1Enabled = false;
            state.Channel1Enabled = false;
        }

        #endregion

        #region Sound Channel 2 - Tone

        // FF16 - NR21 - Channel 2 Sound Length/Wave Pattern Duty (R/W)

        public byte Channel2Length;
        public WavePatternDuties Channel2WavePatternDuty;

        // FF17 - NR22 - Channel 2 Volume Envelope (R/W)

        public byte Channel2LengthEnvelopeSteps;
        public EnvelopeDirections Channel2EnvelopeDirection;
        public byte Channel2InitialVolume;

        // FF18 - NR23 - Channel 2 Frequency lo data (W)

        public byte Channel2FrequencyLo;

        // FF18 - NR23 - Channel 2 Frequency lo data (W)

        public byte Channel2FrequencyHi;
        public LengthTypes Channel2LengthType;
        public bool Channel2Initialize
        {
            set
            {
                if (value) InitializeChannel2();
                else StopChannel2();
            }
        }

        // Current state

        double channel2LengthTimer;
        double channel2EnvelopeTimer;
        byte channel2CurrentVolume;

        void ProcessChannel2()
        {
            if (!AllSoundEnabled)
            {
                StopChannel2();
                return;
            }

            channel2LengthTimer -= TIME_INTERVAL;
            if (channel2LengthTimer <= 0d)
            {
                StopChannel2();
                return;
            }

            if (Channel2LengthEnvelopeSteps > 0)
            {
                channel2EnvelopeTimer -= TIME_INTERVAL;
                if (channel2EnvelopeTimer <= 0)
                {
                    channel2EnvelopeTimer = Channel2LengthEnvelopeSteps / 64d;

                    if (Channel2EnvelopeDirection == EnvelopeDirections.Decrease)
                    {
                        channel2CurrentVolume--;
                        if (channel2CurrentVolume < 0) channel2CurrentVolume = 0;
                    }
                    else
                    {
                        channel2CurrentVolume++;
                        if (channel2CurrentVolume > 0xF) channel2CurrentVolume = 0xF;
                    }
                }
            }

            state.Channel2Volume = channel2CurrentVolume / (float)0xF;

            state.Channel2Frequency = 131072f / (2048 - ((Channel2FrequencyHi << 8) | Channel2FrequencyLo));

            state.Channel2WavePattern = Channel2WavePatternDuty;
        }

        void InitializeChannel2()
        {
            channel2LengthTimer = (64d - Channel2Length) / 256d;
            channel2EnvelopeTimer = Channel2LengthEnvelopeSteps / 64d;
            channel2CurrentVolume = Channel2InitialVolume;

            Channel2Enabled = true;
            state.Channel2Enabled = true;
        }

        void StopChannel2()
        {
            Channel2Enabled = false;
            state.Channel2Enabled = false;
        }

        #endregion
    }

    struct SoundState
    {
        public bool Channel1Enabled;
        public float Channel1Volume;
        public float Channel1Frequency;
        public Sound.WavePatternDuties Channel1WavePattern;

        public bool Channel2Enabled;
        public float Channel2Volume;
        public float Channel2Frequency;
        public Sound.WavePatternDuties Channel2WavePattern;
    }
}