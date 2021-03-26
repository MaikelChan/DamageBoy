using System;

namespace GBEmu.Core.Audio
{
    class NoiseChannel : SoundChannel, IVolumeEnvelope, INoise
    {
        readonly Random random;

        // Volume Envelope

        public byte LengthEnvelopeSteps { get; set; }
        public EnvelopeDirections EnvelopeDirection { get; set; }
        public byte InitialVolume { get; set; }

        // Noise

        public byte DividingRatioFrequencies { get; set; }
        public NoiseCounterStepWidths CounterStepWidth { get; set; }
        public byte ShiftClockFrequency { get; set; }

        // Helper properties

        protected override int MaxLength => 64;
        protected override bool IsDACEnabled => InitialVolume != 0 || EnvelopeDirection == EnvelopeDirections.Increase;

        // Current state

        int currentEnvelopeTimer;
        int currentVolume;
        int currentNoiseClocksToWait;
        ushort currentNoiseSequence;

        public NoiseChannel(APU apu) : base(apu)
        {
            random = new Random();
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

            currentNoiseClocksToWait -= 4;
            if (currentNoiseClocksToWait <= 0)
            {
                float r = DividingRatioFrequencies == 0 ? 0.5f : DividingRatioFrequencies;
                float frequency = 524288f / r / MathF.Pow(2, ShiftClockFrequency + 1);

                currentNoiseClocksToWait = (int)(CPU.CPU_CLOCKS / frequency);

                int xor = (currentNoiseSequence & 0x1) ^ ((currentNoiseSequence >> 1) & 0x1);
                currentNoiseSequence >>= 1;
                currentNoiseSequence |= (ushort)(xor << 14);
                if (CounterStepWidth == NoiseCounterStepWidths.Bits7)
                {
                    currentNoiseSequence = (ushort)(currentNoiseSequence & 0b1111_1111_1011_1111);
                    currentNoiseSequence |= (ushort)(xor << 6);
                }
            }

            int bit = (currentNoiseSequence & 0x1) ^ 1;
            float wave = bit != 0 ? 1.0f : -0.999f;
            wave *= currentVolume / (float)0xF;
            return FloatWaveToByte(wave);
        }

        public override void Initialize(bool reset)
        {
            if (reset)
            {
                currentEnvelopeTimer = LengthEnvelopeSteps;
                currentNoiseSequence = (ushort)random.Next(ushort.MinValue, ushort.MaxValue + 1);

                Enabled = true;
            }

            if (currentLength == 0) currentLength = 64;
            currentVolume = InitialVolume;
        }

        public override void Reset()
        {
            Length = 0;
            LengthEnvelopeSteps = 0;
            EnvelopeDirection = EnvelopeDirections.Decrease;
            InitialVolume = 0;
            DividingRatioFrequencies = 0;
            CounterStepWidth = 0;
            ShiftClockFrequency = 0;
            LengthType = LengthTypes.Consecutive;

            currentLength = 0;
            currentEnvelopeTimer = 0;
            currentVolume = 0;
            currentNoiseSequence = 0;
        }
    }
}