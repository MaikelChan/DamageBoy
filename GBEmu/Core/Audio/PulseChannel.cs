using System;

namespace GBEmu.Core.Audio
{
    public enum PulsePatterns : byte
    {
        Percent12_5,
        Percent25,
        Percent50,
        Percent75
    }

    class PulseChannel : SoundChannel, ISweep, IVolumeEnvelope
    {
        // Sweep

        public byte SweepShift { get; set; }
        public SweepTypes SweepType { get; set; }
        public byte SweepTime { get; set; }

        // Pulse

        public PulsePatterns PulsePattern { get; set; }

        // Volume Envelope

        public byte LengthEnvelopeSteps { get; set; }
        public EnvelopeDirections EnvelopeDirection { get; set; }
        public byte InitialVolume { get; set; }

        // Frequency

        public byte FrequencyLo { get; set; }
        public byte FrequencyHi { get; set; }

        // Helper properties

        protected override int MaxLength => 64;
        protected override bool IsDACEnabled => InitialVolume != 0 || EnvelopeDirection == EnvelopeDirections.Increase;

        // Current state

        int currentEnvelopeTimer;
        int currentVolume;
        int currentSweepTimer;
        int currentFrequency;
        float currentWaveCycle;

        public PulseChannel(APU apu) : base(apu)
        {

        }

        protected override byte InternalProcess(bool updateSample, bool updateVolume, bool updateSweep)
        {
            if (updateVolume)
            {
                if (LengthEnvelopeSteps > 0 && currentEnvelopeTimer > 0)
                {
                    currentEnvelopeTimer--;
                    if (currentEnvelopeTimer == 0)
                    {
                        currentEnvelopeTimer = LengthEnvelopeSteps;

                        if (EnvelopeDirection == EnvelopeDirections.Decrease)
                        {
                            currentVolume--;
                            if (currentVolume < 0) currentVolume = 0;
                        }
                        else
                        {
                            currentVolume++;
                            if (currentVolume > 0xF) currentVolume = 0xF;
                        }
                    }
                }
            }

            if (updateSweep)
            {
                if (SweepTime > 0 && currentSweepTimer > 0)
                {
                    currentSweepTimer--;
                    if (currentSweepTimer == 0)
                    {
                        currentSweepTimer = SweepTime;

                        int frequencyDifference = (int)(currentFrequency / MathF.Pow(2, SweepShift));

                        if (SweepType == SweepTypes.Increase)
                        {
                            currentFrequency += frequencyDifference;
                            if (currentFrequency > 0x7FF)
                            {
                                Stop();
                                return WAVE_SILENCE;
                            }
                        }
                        else
                        {
                            if (frequencyDifference >= 0 && SweepShift > 0)
                            {
                                currentFrequency -= frequencyDifference;
                            }
                        }
                    }
                }
            }

            if (updateSample)
            {
                float percentage;
                switch (PulsePattern)
                {
                    default:
                    case PulsePatterns.Percent12_5: percentage = 0.75f; break;
                    case PulsePatterns.Percent25: percentage = 0.5f; break;
                    case PulsePatterns.Percent50: percentage = 0.0f; break;
                    case PulsePatterns.Percent75: percentage = -0.5f; break;
                }

                float frequency = 131072f / (2048 - currentFrequency);

                currentWaveCycle += (frequency * MathF.PI * 2) / APU.SAMPLE_RATE;
                currentWaveCycle %= APU.SAMPLE_RATE;

                float wave = MathF.Sin(currentWaveCycle);
                wave = wave > percentage ? 1f : -0.999f;
                wave *= currentVolume / (float)0xF;
                return FloatWaveToByte(wave);
            }

            return WAVE_SILENCE;
        }

        public override void Initialize(bool reset)
        {
            if (reset)
            {
                currentEnvelopeTimer = LengthEnvelopeSteps;
                currentSweepTimer = SweepTime;
                currentWaveCycle = 0;

                Enabled = true;
            }

            if (currentLength == 0) currentLength = 64;
            currentVolume = InitialVolume;
            currentFrequency = (FrequencyHi << 8) | FrequencyLo;
        }

        public override void Reset()
        {
            SweepShift = 0;
            SweepType = SweepTypes.Increase;
            SweepTime = 0;
            Length = 0;
            PulsePattern = PulsePatterns.Percent12_5;
            LengthEnvelopeSteps = 0;
            EnvelopeDirection = EnvelopeDirections.Decrease;
            InitialVolume = 0;
            FrequencyLo = 0;
            FrequencyHi = 0;
            LengthType = LengthTypes.Consecutive;

            currentLength = 0;
            currentEnvelopeTimer = 0;
            currentVolume = 0;
            currentSweepTimer = 0;
            currentFrequency = 0;
            currentWaveCycle = 0f;
        }
    }
}