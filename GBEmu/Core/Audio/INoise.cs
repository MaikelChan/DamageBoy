
namespace GBEmu.Core.Audio
{
    public enum NoiseCounterStepWidths : byte
    {
        Bits15,
        Bits7
    }

    interface INoise
    {
        byte DividingRatioFrequencies { get; set; }
        NoiseCounterStepWidths CounterStepWidth { get; set; }
        byte ShiftClockFrequency { get; set; }
    }
}