
namespace DamageBoy.Core.Audio;

public enum EnvelopeDirections : byte
{
    Decrease,
    Increase
}

interface IVolumeEnvelope
{
    byte LengthEnvelopeSteps { get; set; }
    EnvelopeDirections EnvelopeDirection { get; set; }
    byte InitialVolume { get; set; }
}