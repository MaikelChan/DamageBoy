using System.Runtime.CompilerServices;

namespace GBEmu.Core.Audio
{
    public enum LengthTypes : byte
    {
        Consecutive,
        Counter
    }

    abstract class SoundChannel
    {
        readonly APU apu;

        // Sound Length

        public bool Enabled { get; set; }
        public LengthTypes LengthType { get; set; }
        public byte Length { set => currentLength = MaxLength - value; }

        // Outputs

        public bool Output1 { get; set; }
        public bool Output2 { get; set; }

        // Helper properties

        protected abstract int MaxLength { get; }
        protected abstract bool IsDACEnabled { get; }

        // Current State

        protected int currentLength;

        // Constants

        protected const byte WAVE_SILENCE = 127;

        public SoundChannel(APU apu)
        {
            this.apu = apu;
        }

        public byte Process(bool updateSample, bool updateLength, bool updateVolumeEnvelope, bool updateSweep)
        {
            if (!IsDACEnabled)
            {
                Stop();
                return WAVE_SILENCE;
            }

            if (updateLength && LengthType == LengthTypes.Counter)
            {
                currentLength--;
                if (currentLength == 0)
                {
                    Stop();
                    return WAVE_SILENCE;
                }
            }

            if (!apu.AllSoundEnabled || !Enabled)
            {
                Stop();
                return WAVE_SILENCE;
            }

            return InternalProcess(updateSample, updateVolumeEnvelope, updateSweep);
        }

        public abstract void Initialize(bool reset);

        public abstract void Reset();

        protected abstract byte InternalProcess(bool updateSample, bool updateVolumeEnvelope, bool updateSweep);

        protected void Stop()
        {
            Enabled = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static byte FloatWaveToByte(float value)
        {
            return (byte)(value * 128 + 127);
        }
    }
}