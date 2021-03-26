
namespace GBEmu.Core.Audio
{
    class WaveChannel : SoundChannel
    {
        // Sound on/off

        public bool SoundOn { get; set; }

        // Volume

        public byte Volume { get; set; }

        // Frequency

        public byte FrequencyLo { get; set; }
        public byte FrequencyHi { get; set; }

        // Wave Pattern

        public byte[] WavePattern { get; }

        // Helper properties

        protected override int MaxLength => 256;
        protected override bool IsDACEnabled => SoundOn;

        // Current state

        float currentWaveCycle;

        public WaveChannel(APU apu) : base(apu)
        {
            WavePattern = new byte[0x20];
        }

        protected override byte InternalProcess(bool updateSample, bool updateVolume, bool updateSweep)
        {
            if (updateSample)
            {
                float volume;

                switch (Volume)
                {
                    default:
                    case 0: volume = 0f; break;
                    case 1: volume = 1f; break;
                    case 2: volume = 0.5f; break;
                    case 3: volume = 0.25f; break;
                }

                float frequency = 65536f / (2048 - ((FrequencyHi << 8) | FrequencyLo));

                currentWaveCycle += frequency / (APU.SAMPLE_RATE >> 5);
                currentWaveCycle %= 32f;

                float wave = (WavePattern[(int)currentWaveCycle] / 7.5f) - 0.999f;
                wave *= volume;
                return FloatWaveToByte(wave);
            }

            return WAVE_SILENCE;
        }

        public override void Initialize(bool reset)
        {
            if (reset)
            {
                currentWaveCycle = 0;

                Enabled = true;
            }

            if (currentLength == 0) currentLength = 256;
        }

        public override void Reset()
        {
            SoundOn = false;
            Length = 0;
            Volume = 0;
            FrequencyLo = 0;
            FrequencyHi = 0;
            LengthType = LengthTypes.Consecutive;

            currentLength = 0;
        }
    }
}