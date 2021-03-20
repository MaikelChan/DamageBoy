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
        public enum NoiseCounterStepWidths : byte { Bits15, Bits7 }

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
            ProcessChannel3();
            ProcessChannel4();

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

        bool allSoundEnabled;
        public bool AllSoundEnabled
        {
            get => allSoundEnabled;
            set
            {
                if (allSoundEnabled && !value)
                {
                    // Reset everything when disabling audio

                    Channel1SweepShift = 0;
                    Channel1SweepType = SweepTypes.Increase;
                    Channel1SweepTime = 0;
                    Channel1Length = 0;
                    Channel1WavePatternDuty = WavePatternDuties.Percent12_5;
                    Channel1LengthEnvelopeSteps = 0;
                    Channel1EnvelopeDirection = EnvelopeDirections.Decrease;
                    Channel1InitialVolume = 0;
                    Channel1FrequencyLo = 0;
                    Channel1FrequencyHi = 0;
                    Channel1LengthType = LengthTypes.Consecutive;
                    Channel1Initialize = false;
                    channel1LengthTimer = 0;
                    channel1EnvelopeTimer = 0;
                    channel1CurrentVolume = 0;

                    Channel2Length = 0;
                    Channel2WavePatternDuty = WavePatternDuties.Percent12_5;
                    Channel2LengthEnvelopeSteps = 0;
                    Channel2EnvelopeDirection = EnvelopeDirections.Decrease;
                    Channel2InitialVolume = 0;
                    Channel2FrequencyLo = 0;
                    Channel2FrequencyHi = 0;
                    Channel2LengthType = LengthTypes.Consecutive;
                    Channel2Initialize = false;
                    channel2LengthTimer = 0;
                    channel2EnvelopeTimer = 0;
                    channel2CurrentVolume = 0;

                    Channel3On = false;
                    Channel3Length = 0;
                    Channel3Volume = 0;
                    Channel3FrequencyLo = 0;
                    Channel3FrequencyHi = 0;
                    Channel3LengthType = LengthTypes.Consecutive;
                    Channel3Initialize = false;
                    //WavePattern = new byte[0x10];
                    channel3LengthTimer = 0;
                    channel3EnvelopeTimer = 0;
                    channel3CurrentVolume = 0;

                    Channel4Length = 0;
                    Channel4LengthEnvelopeSteps = 0;
                    Channel4EnvelopeDirection = EnvelopeDirections.Decrease;
                    Channel4InitialVolume = 0;
                    Channel4DividingRatioFrequencies = 0;
                    Channel4CounterStepWidth = 0;
                    Channel4ShiftClockFrequency = 0;
                    Channel4LengthType = LengthTypes.Consecutive;
                    Channel4Initialize = false;
                    channel4LengthTimer = 0;
                    channel4EnvelopeTimer = 0;
                    channel4CurrentVolume = 0;

                    Output1Level = 0;
                    VinOutput1 = false;
                    Output2Level = 0;
                    VinOutput2 = false;
                    Channel1Output1 = false;
                    Channel2Output1 = false;
                    Channel3Output1 = false;
                    Channel4Output1 = false;
                    Channel1Output2 = false;
                    Channel2Output2 = false;
                    Channel3Output2 = false;
                    Channel4Output2 = false;
                    Channel1Enabled = false;
                    Channel2Enabled = false;
                    Channel3Enabled = false;
                    Channel4Enabled = false;
                }

                allSoundEnabled = value;
            }
        }

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

        #region Sound Channel 3 - Wave Output

        // FF1A - NR30 - Channel 3 Sound on/off (R/W)

        public bool Channel3On;

        // FF1B - NR31 - Channel 3 Sound Length

        public byte Channel3Length;

        // FF1C - NR32 - Channel 3 Select output level (R/W)

        public byte Channel3Volume;

        // FF1D - NR33 - Channel 3 Frequency's lower data (W)

        public byte Channel3FrequencyLo;

        // FF1E - NR34 - Channel 3 Frequency's higher data (R/W)

        public byte Channel3FrequencyHi;
        public LengthTypes Channel3LengthType;
        public bool Channel3Initialize
        {
            set
            {
                if (value) InitializeChannel3();
                else StopChannel3();
            }
        }

        // FF30-FF3F - Wave Pattern RAM

        public byte[] WavePattern = new byte[0x10];

        // Current state

        double channel3LengthTimer;
        double channel3EnvelopeTimer;
        byte channel3CurrentVolume;

        void ProcessChannel3()
        {
            if (!AllSoundEnabled)
            {
                StopChannel3();
                return;
            }

            //channel2LengthTimer -= TIME_INTERVAL;
            //if (channel2LengthTimer <= 0d)
            //{
            //    StopChannel3();
            //    return;
            //}

            //if (Channel2LengthEnvelopeSteps > 0)
            //{
            //    channel2EnvelopeTimer -= TIME_INTERVAL;
            //    if (channel2EnvelopeTimer <= 0)
            //    {
            //        channel2EnvelopeTimer = Channel2LengthEnvelopeSteps / 64d;

            //        if (Channel2EnvelopeDirection == EnvelopeDirections.Decrease)
            //        {
            //            channel2CurrentVolume--;
            //            if (channel2CurrentVolume < 0) channel2CurrentVolume = 0;
            //        }
            //        else
            //        {
            //            channel2CurrentVolume++;
            //            if (channel2CurrentVolume > 0xF) channel2CurrentVolume = 0xF;
            //        }
            //    }
            //}

            //state.Channel2Volume = channel2CurrentVolume / (float)0xF;

            //state.Channel2Frequency = 131072f / (2048 - ((Channel2FrequencyHi << 8) | Channel2FrequencyLo));

            //state.Channel2WavePattern = Channel2WavePatternDuty;
        }

        void InitializeChannel3()
        {
            //channel2LengthTimer = (64d - Channel2Length) / 256d;
            //channel2EnvelopeTimer = Channel2LengthEnvelopeSteps / 64d;
            //channel2CurrentVolume = Channel2InitialVolume;

            Channel3Enabled = true;
            state.Channel3Enabled = true;
        }

        void StopChannel3()
        {
            Channel3Enabled = false;
            state.Channel3Enabled = false;
        }

        #endregion

        #region Sound Channel 4 - Noise

        // FF20 - NR41 - Channel 4 Sound Length (R/W)

        public byte Channel4Length;

        // FF21 - NR42 - Channel 4 Volume Envelope (R/W)

        public byte Channel4LengthEnvelopeSteps;
        public EnvelopeDirections Channel4EnvelopeDirection;
        public byte Channel4InitialVolume;

        // FF22 - NR43 - Channel 4 Polynomial Counter (R/W)

        public byte Channel4DividingRatioFrequencies;
        public NoiseCounterStepWidths Channel4CounterStepWidth;
        public byte Channel4ShiftClockFrequency;

        // FF23 - NR44 - Channel 4 Counter/consecutive; Inital (R/W)

        public LengthTypes Channel4LengthType;
        public bool Channel4Initialize
        {
            set
            {
                if (value) InitializeChannel4();
                else StopChannel4();
            }
        }

        // Current state

        double channel4LengthTimer;
        double channel4EnvelopeTimer;
        byte channel4CurrentVolume;

        void ProcessChannel4()
        {
            if (!AllSoundEnabled)
            {
                StopChannel4();
                return;
            }

        }

        void InitializeChannel4()
        {

            Channel4Enabled = true;
            state.Channel4Enabled = true;
        }

        void StopChannel4()
        {
            Channel4Enabled = false;
            state.Channel4Enabled = false;
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

        public bool Channel3Enabled;
        public float Channel3Volume;
        public float Channel3Frequency;

        public bool Channel4Enabled;
        public float Channel4Volume;
        public float Channel4Frequency;
    }
}