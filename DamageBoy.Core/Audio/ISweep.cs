
namespace DamageBoy.Core.Audio
{
    public enum SweepTypes : byte
    {
        Increase,
        Decrease
    }

    interface ISweep
    {
        byte SweepShift { get; set; }
        SweepTypes SweepType { get; set; }
        byte SweepTime { get; set; }
    }
}