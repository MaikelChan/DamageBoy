using DamageBoy.Core.State;
using System.IO;

namespace DamageBoy.Core.Audio;

public enum LengthTypes : byte
{
    Consecutive,
    Counter
}

abstract class SoundChannel : IState
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

    public const float WAVE_SILENCE = 0f;

    public SoundChannel(APU apu)
    {
        this.apu = apu;
    }

    public float Process(bool updateSample, bool updateLength, bool updateVolumeEnvelope, bool updateSweep)
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

    public virtual void Initialize(bool reset)
    {
        if (currentLength == 0) currentLength = MaxLength;
    }

    public virtual void Reset()
    {
        LengthType = LengthTypes.Consecutive;

        currentLength = 0;
    }

    protected abstract float InternalProcess(bool updateSample, bool updateVolumeEnvelope, bool updateSweep);

    protected void Stop()
    {
        Enabled = false;
    }

    public abstract void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save);
}