using DamageBoy.Core.State;
using System.IO;

namespace DamageBoy.Core;

class InterruptHandler : IState
{
    public bool RequestVerticalBlanking { get; set; }
    public bool RequestLCDCSTAT { get; set; }
    public bool RequestTimerOverflow { get; set; }
    public bool RequestSerialTransferCompletion { get; set; }
    public bool RequestJoypad { get; set; }

    public bool EnableVerticalBlanking { get; set; }
    public bool EnableLCDCSTAT { get; set; }
    public bool EnableTimerOverflow { get; set; }
    public bool EnableSerialTransferCompletion { get; set; }
    public bool EnableJoypad { get; set; }

    public byte EnableUnusedBits { get; set; } // In this case, unused bits are actually writable and readable

    public void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save)
    {
        RequestVerticalBlanking = SaveState.SaveLoadValue(bw, br, save, RequestVerticalBlanking);
        RequestLCDCSTAT = SaveState.SaveLoadValue(bw, br, save, RequestLCDCSTAT);
        RequestTimerOverflow = SaveState.SaveLoadValue(bw, br, save, RequestTimerOverflow);
        RequestSerialTransferCompletion = SaveState.SaveLoadValue(bw, br, save, RequestSerialTransferCompletion);
        RequestJoypad = SaveState.SaveLoadValue(bw, br, save, RequestJoypad);

        EnableVerticalBlanking = SaveState.SaveLoadValue(bw, br, save, EnableVerticalBlanking);
        EnableLCDCSTAT = SaveState.SaveLoadValue(bw, br, save, EnableLCDCSTAT);
        EnableTimerOverflow = SaveState.SaveLoadValue(bw, br, save, EnableTimerOverflow);
        EnableSerialTransferCompletion = SaveState.SaveLoadValue(bw, br, save, EnableSerialTransferCompletion);
        EnableJoypad = SaveState.SaveLoadValue(bw, br, save, EnableJoypad);

        EnableUnusedBits = SaveState.SaveLoadValue(bw, br, save, EnableUnusedBits);
    }
}