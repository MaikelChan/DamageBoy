using DamageBoy.Core.State;
using System;

namespace DamageBoy.Core.Audio
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

        public const int WAVE_PATTERN_SIZE = 0x20;

        public WaveChannel(APU apu) : base(apu)
        {
            WavePattern = new byte[WAVE_PATTERN_SIZE];
        }

        protected override ushort InternalProcess(bool updateSample, bool updateVolume, bool updateSweep)
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

                currentWaveCycle += frequency / (Constants.SAMPLE_RATE >> 5);
                currentWaveCycle %= WAVE_PATTERN_SIZE;

                float wave = (WavePattern[(int)currentWaveCycle] / 7.5f) - 0.999f;
                wave *= volume;
                return FloatWaveToUInt16(wave);
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
            base.Reset();

            SoundOn = false;

            Volume = 0;

            FrequencyLo = 0;
            FrequencyHi = 0;
        }

        public override SoundChannelState GetState()
        {
            WaveChannelState waveState = new WaveChannelState();

            waveState.Enabled = Enabled;
            waveState.LengthType = LengthType;
            waveState.Output2 = Output2;
            waveState.Output1 = Output1;
            waveState.CurrentLength = currentLength;

            waveState.SoundOn = SoundOn;

            waveState.Volume = Volume;

            waveState.FrequencyLo = FrequencyLo;
            waveState.FrequencyHi = FrequencyHi;

            Array.Copy(WavePattern, waveState.WavePattern, WAVE_PATTERN_SIZE);

            waveState.CurrentWaveCycle = currentWaveCycle;

            return waveState;
        }

        public override void SetState(SoundChannelState state)
        {
            WaveChannelState waveState = (WaveChannelState)state;

            Enabled = waveState.Enabled;
            LengthType = waveState.LengthType;
            Output2 = waveState.Output2;
            Output1 = waveState.Output1;
            currentLength = waveState.CurrentLength;

            SoundOn = waveState.SoundOn;

            Volume = waveState.Volume;

            FrequencyLo = waveState.FrequencyLo;
            FrequencyHi = waveState.FrequencyHi;

            Array.Copy(waveState.WavePattern, WavePattern, WAVE_PATTERN_SIZE);

            currentWaveCycle = waveState.CurrentWaveCycle;
        }
    }
}