using DamageBoy.Core.State;
using System.IO;

namespace DamageBoy.Core;

class Timer : IState
{
    readonly GameBoy gameBoy;
    readonly InterruptHandler interruptHandler;

    public enum TimerClockSpeeds : byte { Hz4096, Hz262144, Hz65536, Hz16384 }

    public byte Divider => (byte)(currentSystemCounter >> 8);
    public bool TimerEnable { get; set; }
    public TimerClockSpeeds TimerClockSpeed { get; set; }

    byte timerCounter;
    public byte TimerCounter
    {
        get => timerCounter;
        set
        {
            if (timerOverflowWaitCycles == 4)
                return;
            if (timerOverflowWaitCycles == 8)
            {
                interruptHandler.RequestTimerOverflow = false;
                timerOverflowWaitCycles = 0;
            }
            timerCounter = value;
        }
    }

    byte timerModulo;
    public byte TimerModulo
    {
        get => timerModulo;
        set
        {
            if (timerOverflowWaitCycles == 4)
                timerCounter = value;
            timerModulo = value;
        }
    }

    bool TimerHasOverflown => timerOverflowWaitCycles > 0;

    byte timerOverflowWaitCycles;
    ushort currentSystemCounter;
    bool previousSelectedBit;

    public Timer(GameBoy gameBoy, InterruptHandler interruptHandler)
    {
        this.gameBoy = gameBoy;
        this.interruptHandler = interruptHandler;
    }

    public void Update()
    {
        SetSystemCounter((ushort)(currentSystemCounter + 4));
        if (!TimerHasOverflown) return;

        timerOverflowWaitCycles -= 4;

        if (timerOverflowWaitCycles == 8)
        {
            interruptHandler.RequestTimerOverflow = true;
        }
        else if (timerOverflowWaitCycles == 4)
        {
            timerCounter = timerModulo;
        }
    }

    public void ResetDIV()
    {
        SetSystemCounter(0);
    }

    void SetSystemCounter(ushort value)
    {
        currentSystemCounter = value;

        byte bitPosition = GetSelectedBitPosition();
        bool currentSelectedBit = (currentSystemCounter & (1 << bitPosition)) != 0;
        if (gameBoy.HardwareType == HardwareTypes.DMG) currentSelectedBit &= TimerEnable;

        if (!currentSelectedBit && previousSelectedBit)
        {
            timerCounter++;
            if (timerCounter == 0) timerOverflowWaitCycles = 12;
        }

        previousSelectedBit = currentSelectedBit;
    }

    byte GetSelectedBitPosition()
    {
        switch (TimerClockSpeed)
        {
            default:
            case TimerClockSpeeds.Hz4096: return 9;
            case TimerClockSpeeds.Hz262144: return 3;
            case TimerClockSpeeds.Hz65536: return 5;
            case TimerClockSpeeds.Hz16384: return 7;
        }
    }

    public void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save)
    {
        TimerEnable = SaveState.SaveLoadValue(bw, br, save, TimerEnable);
        TimerClockSpeed = (TimerClockSpeeds)SaveState.SaveLoadValue(bw, br, save, (byte)TimerClockSpeed);
        timerCounter = SaveState.SaveLoadValue(bw, br, save, timerCounter);
        timerModulo = SaveState.SaveLoadValue(bw, br, save, timerModulo);

        timerOverflowWaitCycles = SaveState.SaveLoadValue(bw, br, save, timerOverflowWaitCycles);
        currentSystemCounter = SaveState.SaveLoadValue(bw, br, save, currentSystemCounter);
        previousSelectedBit = SaveState.SaveLoadValue(bw, br, save, previousSelectedBit);
    }
}