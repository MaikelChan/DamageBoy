using GBEmu.Core.Audio;

namespace GBEmu.Core.State
{
    internal class PulseChannelState : SoundChannelState, ISweep, IVolumeEnvelope
    {
        public byte SweepShift { get; set; }
        public SweepTypes SweepType { get; set; }
        public byte SweepTime { get; set; }

        public PulsePatterns PulsePattern { get; set; }

        public byte LengthEnvelopeSteps { get; set; }
        public EnvelopeDirections EnvelopeDirection { get; set; }
        public byte InitialVolume { get; set; }

        public byte FrequencyLo { get; set; }
        public byte FrequencyHi { get; set; }

        public int CurrentEnvelopeTimer { get; set; }
        public int CurrentVolume { get; set; }
        public int CurrentSweepTimer { get; set; }
        public int CurrentFrequency { get; set; }
        public float CurrentWaveCycle { get; set; }
    }
}