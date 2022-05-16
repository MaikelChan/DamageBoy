using DamageBoy.Core.State;
using System;
using System.IO;

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
        public const int UPDATE_FREQUENCY = 65536;

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

                float frequency = (float)UPDATE_FREQUENCY / (2048 - ((FrequencyHi << 8) | FrequencyLo));

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
            base.Initialize(reset);

            if (reset)
            {
                currentWaveCycle = 0;

                Enabled = true;
            }
        }

        public override void Reset()
        {
            base.Reset();

            SoundOn = false;

            Volume = 0;

            FrequencyLo = 0;
            FrequencyHi = 0;
        }

        public override void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save)
        {
            Enabled = SaveState.SaveLoadValue(bw, br, save, Enabled);
            LengthType = (LengthTypes)SaveState.SaveLoadValue(bw, br, save, (byte)LengthType);
            Output2 = SaveState.SaveLoadValue(bw, br, save, Output2);
            Output1 = SaveState.SaveLoadValue(bw, br, save, Output1);
            currentLength = SaveState.SaveLoadValue(bw, br, save, currentLength);

            SoundOn = SaveState.SaveLoadValue(bw, br, save, SoundOn);

            Volume = SaveState.SaveLoadValue(bw, br, save, Volume);

            FrequencyLo = SaveState.SaveLoadValue(bw, br, save, FrequencyLo);
            FrequencyHi = SaveState.SaveLoadValue(bw, br, save, FrequencyHi);

            SaveState.SaveLoadArray(stream, save, WavePattern, WAVE_PATTERN_SIZE);

            currentWaveCycle = SaveState.SaveLoadValue(bw, br, save, currentWaveCycle);
        }
    }
}