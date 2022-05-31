using DamageBoy.Core.State;
using System;
using System.IO;

namespace DamageBoy.Core.Audio;

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

    const int UPDATE_FREQUENCY = 524288;

    public NoiseChannel(APU apu) : base(apu)
    {
        random = new Random();
    }

    protected override float InternalProcess(bool updateSample, bool updateVolume, bool updateSweep)
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
            float frequency = UPDATE_FREQUENCY / r / MathF.Pow(2, ShiftClockFrequency + 1);

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

        // HACK: When ShiftClockFrequency is below 4, those high frequencies
        // are perceived as having lower volume. Sound processing needs a rewrite to
        // be able to process noise at higher intervals, so let's force a lower volume for now.

        if (ShiftClockFrequency == 3) wave *= 0.825f;
        else if (ShiftClockFrequency == 2) wave *= 0.65f;
        else if (ShiftClockFrequency == 1) wave *= 0.475f;
        else if (ShiftClockFrequency == 0) wave *= 0.3f;

        wave *= currentVolume / (float)0xF;
        return wave;
    }

    public override void Initialize(bool reset)
    {
        base.Initialize(reset);

        if (reset)
        {
            currentVolume = InitialVolume;
            currentEnvelopeTimer = LengthEnvelopeSteps;
            currentNoiseSequence = (ushort)random.Next(ushort.MinValue, ushort.MaxValue + 1);

            Enabled = true;
        }
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

    public override void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save)
    {
        Enabled = SaveState.SaveLoadValue(bw, br, save, Enabled);
        LengthType = (LengthTypes)SaveState.SaveLoadValue(bw, br, save, (byte)LengthType);
        Output2 = SaveState.SaveLoadValue(bw, br, save, Output2);
        Output1 = SaveState.SaveLoadValue(bw, br, save, Output1);
        currentLength = SaveState.SaveLoadValue(bw, br, save, currentLength);

        LengthEnvelopeSteps = SaveState.SaveLoadValue(bw, br, save, LengthEnvelopeSteps);
        EnvelopeDirection = (EnvelopeDirections)SaveState.SaveLoadValue(bw, br, save, (byte)EnvelopeDirection);
        InitialVolume = SaveState.SaveLoadValue(bw, br, save, InitialVolume);

        DividingRatioFrequencies = SaveState.SaveLoadValue(bw, br, save, DividingRatioFrequencies);
        CounterStepWidth = (NoiseCounterStepWidths)SaveState.SaveLoadValue(bw, br, save, (byte)CounterStepWidth);
        ShiftClockFrequency = SaveState.SaveLoadValue(bw, br, save, ShiftClockFrequency);

        currentEnvelopeTimer = SaveState.SaveLoadValue(bw, br, save, currentEnvelopeTimer);
        currentVolume = SaveState.SaveLoadValue(bw, br, save, currentVolume);
        currentNoiseClocksToWait = SaveState.SaveLoadValue(bw, br, save, currentNoiseClocksToWait);
        currentNoiseSequence = SaveState.SaveLoadValue(bw, br, save, currentNoiseSequence);
    }
}