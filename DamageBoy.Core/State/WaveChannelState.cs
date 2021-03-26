using DamageBoy.Core.Audio;

namespace DamageBoy.Core.State
{
    class WaveChannelState : SoundChannelState
    {
        public bool SoundOn { get; set; }

        public byte Volume { get; set; }

        public byte FrequencyLo { get; set; }
        public byte FrequencyHi { get; set; }

        public byte[] WavePattern { get; }

        public float CurrentWaveCycle { get; set; }

        public WaveChannelState()
        {
            WavePattern = new byte[WaveChannel.WAVE_PATTERN_SIZE];
        }
    }
}