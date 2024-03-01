using DamageBoy.Core.State;
using System;
using System.IO;
using static DamageBoy.Core.GameBoy;

namespace DamageBoy.Core;

class CartridgeRam : IState, IDisposable
{
    readonly SaveUpdateDelegate saveUpdateCallback;

    readonly byte[] bytes;
    public byte[] Bytes => bytes;

    AccessModes accessMode;
    public AccessModes AccessMode
    {
        get => accessMode;
        set
        {
            if (accessMode == AccessModes.ReadWrite && value != AccessModes.ReadWrite && hasBeenModifiedSinceLastSave)
            {
                saveUpdateCallback?.Invoke(bytes);
                hasBeenModifiedSinceLastSave = false;
            }

            accessMode = value;
        }
    }

    public enum AccessModes : byte { None, Read, ReadWrite }

    bool hasBeenModifiedSinceLastSave;

    public byte this[int index]
    {
        get
        {
            return bytes[index];
        }

        set
        {
            if (bytes[index] == value) return;

            bytes[index] = value;
            hasBeenModifiedSinceLastSave = true;
        }
    }

    public CartridgeRam(int ramSize, byte[] saveData, SaveUpdateDelegate saveUpdateCallback)
    {
        this.saveUpdateCallback = saveUpdateCallback;

        hasBeenModifiedSinceLastSave = false;
        accessMode = AccessModes.None;

        if (saveData == null)
        {
            bytes = new byte[ramSize];
        }
        else
        {
            if (saveData.Length != ramSize)
            {
                Utils.Log(LogType.Error, $"Save data is {saveData.Length} bytes but the game expects {ramSize} bytes.");
                bytes = new byte[ramSize];
            }
            else
            {
                bytes = saveData;
            }
        }
    }

    public void Dispose()
    {
        saveUpdateCallback?.Invoke(bytes);
        hasBeenModifiedSinceLastSave = false;
    }

    public void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save)
    {
        accessMode = (AccessModes)SaveState.SaveLoadValue(bw, br, save, (byte)accessMode);
        SaveState.SaveLoadArray(stream, save, bytes, bytes.Length);
        if (!save) hasBeenModifiedSinceLastSave = true;
    }
}