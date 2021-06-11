using DamageBoy.Core.State;
using System;
using System.IO;

namespace DamageBoy.Core.Audio
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

        protected override ushort InternalProcess(bool updateSample, bool updateVolume, bool updateSweep)
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

                currentWaveCycle += (frequency * MathF.PI * 2) / Constants.SAMPLE_RATE;
                currentWaveCycle %= Constants.SAMPLE_RATE;

                float wave = MathF.Sin(currentWaveCycle);
                wave = wave > percentage ? 1f : -0.999f;
                wave *= currentVolume / (float)0xF;
                return FloatWaveToUInt16(wave);
            }

            return WAVE_SILENCE;
        }

        public override void Initialize(bool reset)
        {
            base.Initialize(reset);

            if (reset)
            {
                currentVolume = InitialVolume;
                currentEnvelopeTimer = LengthEnvelopeSteps;
                currentSweepTimer = SweepTime;
                currentWaveCycle = 0;

                Enabled = true;
            }

            currentFrequency = (FrequencyHi << 8) | FrequencyLo;
        }

        public override void Reset()
        {
            base.Reset();

            SweepShift = 0;
            SweepType = SweepTypes.Increase;
            SweepTime = 0;

            PulsePattern = PulsePatterns.Percent12_5;

            LengthEnvelopeSteps = 0;
            EnvelopeDirection = EnvelopeDirections.Decrease;
            InitialVolume = 0;

            FrequencyLo = 0;
            FrequencyHi = 0;

            currentEnvelopeTimer = 0;
            currentVolume = 0;
            currentSweepTimer = 0;
            currentFrequency = 0;
            currentWaveCycle = 0f;
        }

        public override void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save)
        {
            Enabled = SaveState.SaveLoadValue(bw, br, save, Enabled);
            LengthType = (LengthTypes)SaveState.SaveLoadValue(bw, br, save, (byte)LengthType);
            Output2 = SaveState.SaveLoadValue(bw, br, save, Output2);
            Output1 = SaveState.SaveLoadValue(bw, br, save, Output1);
            currentLength = SaveState.SaveLoadValue(bw, br, save, currentLength);

            SweepShift = SaveState.SaveLoadValue(bw, br, save, SweepShift);
            SweepType = (SweepTypes)SaveState.SaveLoadValue(bw, br, save, (byte)SweepType);
            SweepTime = SaveState.SaveLoadValue(bw, br, save, SweepTime);

            PulsePattern = (PulsePatterns)SaveState.SaveLoadValue(bw, br, save, (byte)PulsePattern);

            LengthEnvelopeSteps = SaveState.SaveLoadValue(bw, br, save, LengthEnvelopeSteps);
            EnvelopeDirection = (EnvelopeDirections)SaveState.SaveLoadValue(bw, br, save, (byte)EnvelopeDirection);
            InitialVolume = SaveState.SaveLoadValue(bw, br, save, InitialVolume);

            FrequencyLo = SaveState.SaveLoadValue(bw, br, save, FrequencyLo);
            FrequencyHi = SaveState.SaveLoadValue(bw, br, save, FrequencyHi);

            currentEnvelopeTimer = SaveState.SaveLoadValue(bw, br, save, currentEnvelopeTimer);
            currentVolume = SaveState.SaveLoadValue(bw, br, save, currentVolume);
            currentSweepTimer = SaveState.SaveLoadValue(bw, br, save, currentSweepTimer);
            currentFrequency = SaveState.SaveLoadValue(bw, br, save, currentFrequency);
            currentWaveCycle = SaveState.SaveLoadValue(bw, br, save, currentWaveCycle);
        }
    }
}