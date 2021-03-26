using GBEmu.Core.State;
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

        public bool Output2 { get; set; } // Left Speaker
        public bool Output1 { get; set; } // Right Speaker

        // Helper properties

        protected abstract int MaxLength { get; }
        protected abstract bool IsDACEnabled { get; }

        // Current State

        protected int currentLength;

        // Constants

        protected const ushort WAVE_SILENCE = 0x7f7f;

        public SoundChannel(APU apu)
        {
            this.apu = apu;
        }

        public ushort Process(bool updateSample, bool updateLength, bool updateVolumeEnvelope, bool updateSweep)
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

        public virtual void Reset()
        {
            LengthType = LengthTypes.Consecutive;

            currentLength = 0;
        }

        protected abstract ushort InternalProcess(bool updateSample, bool updateVolumeEnvelope, bool updateSweep);

        protected void Stop()
        {
            Enabled = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected ushort FloatWaveToUInt16(float value)
        {
            float leftValue = Output2 ? value * (apu.Output2Level / 7f) : 0f;
            float rightValue = Output1 ? value * (apu.Output1Level / 7f) : 0f;

            byte left = (byte)(leftValue * 128 + 127);
            byte right = (byte)(rightValue * 128 + 127);

            return (ushort)((left << 8) | right);
        }

        public abstract SoundChannelState GetState();

        public abstract void SetState(SoundChannelState state);
    }
}