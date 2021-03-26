using DamageBoy.Core.State;
using System;

namespace DamageBoy.Core.Audio
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
            return FloatWaveToUInt16(wave);
        }

        public override void Initialize(bool reset)
        {
            if (reset)
            {
                currentVolume = InitialVolume;
                currentEnvelopeTimer = LengthEnvelopeSteps;
                currentNoiseSequence = (ushort)random.Next(ushort.MinValue, ushort.MaxValue + 1);

                Enabled = true;
            }

            if (currentLength == 0) currentLength = 64;
        }

        public override void Reset()
        {
            base.Reset();

            LengthEnvelopeSteps = 0;
            EnvelopeDirection = EnvelopeDirections.Decrease;
            InitialVolume = 0;

            DividingRatioFrequencies = 0;
            CounterStepWidth = 0;
            ShiftClockFrequency = 0;

            currentEnvelopeTimer = 0;
            currentVolume = 0;
            currentNoiseSequence = 0;
        }

        public override SoundChannelState GetState()
        {
            NoiseChannelState noiseState = new NoiseChannelState();

            noiseState.Enabled = Enabled;
            noiseState.LengthType = LengthType;
            noiseState.Output2 = Output2;
            noiseState.Output1 = Output1;
            noiseState.CurrentLength = currentLength;

            noiseState.LengthEnvelopeSteps = LengthEnvelopeSteps;
            noiseState.EnvelopeDirection = EnvelopeDirection;
            noiseState.InitialVolume = InitialVolume;

            noiseState.DividingRatioFrequencies = DividingRatioFrequencies;
            noiseState.CounterStepWidth = CounterStepWidth;
            noiseState.ShiftClockFrequency = ShiftClockFrequency;

            noiseState.CurrentEnvelopeTimer = currentEnvelopeTimer;
            noiseState.CurrentVolume = currentVolume;
            noiseState.CurrentNoiseClocksToWait = currentNoiseClocksToWait;
            noiseState.CurrentNoiseSequence = currentNoiseSequence;

            return noiseState;
        }

        public override void SetState(SoundChannelState state)
        {
            NoiseChannelState noiseState = (NoiseChannelState)state;

            Enabled = noiseState.Enabled;
            LengthType = noiseState.LengthType;
            Output2 = noiseState.Output2;
            Output1 = noiseState.Output1;
            currentLength = noiseState.CurrentLength;

            LengthEnvelopeSteps = noiseState.LengthEnvelopeSteps;
            EnvelopeDirection = noiseState.EnvelopeDirection;
            InitialVolume = noiseState.InitialVolume;

            DividingRatioFrequencies = noiseState.DividingRatioFrequencies;
            CounterStepWidth = noiseState.CounterStepWidth;
            ShiftClockFrequency = noiseState.ShiftClockFrequency;

            currentEnvelopeTimer = noiseState.CurrentEnvelopeTimer;
            currentVolume = noiseState.CurrentVolume;
            currentNoiseClocksToWait = noiseState.CurrentNoiseClocksToWait;
            currentNoiseSequence = noiseState.CurrentNoiseSequence;
        }
    }
}