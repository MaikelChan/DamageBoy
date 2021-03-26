using DamageBoy.Core.Audio;

namespace DamageBoy.Core.State
{
    class NoiseChannelState : SoundChannelState, IVolumeEnvelope, INoise
    {
        public byte LengthEnvelopeSteps { get; set; }
        public EnvelopeDirections EnvelopeDirection { get; set; }
        public byte InitialVolume { get; set; }

        public byte DividingRatioFrequencies { get; set; }
        public NoiseCounterStepWidths CounterStepWidth { get; set; }
        public byte ShiftClockFrequency { get; set; }

        public int CurrentEnvelopeTimer { get; set; }
        public int CurrentVolume { get; set; }
        public int CurrentNoiseClocksToWait { get; set; }
        public ushort CurrentNoiseSequence { get; set; }
    }
}