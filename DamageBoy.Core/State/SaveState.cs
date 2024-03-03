using System.IO;
using System.IO.Compression;
using System.Text;

namespace DamageBoy.Core.State;

internal class SaveState
{
    readonly IState[] componentsStates;
    readonly Cartridge cartridge;

    const uint SAVE_SATATE_FORMAT_VERSION = 4;

    public SaveState(IState[] componentsStates, Cartridge cartridge)
    {
        this.componentsStates = componentsStates;
        this.cartridge = cartridge;
    }

    public bool Save(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            Utils.Log(LogType.Error, "Save state file name cannot be null or empty.");
            return false;
        }

        using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
        using (BinaryWriter fsBw = new BinaryWriter(fs))
        {
            fsBw.Write(SAVE_SATATE_FORMAT_VERSION);
#if COMPRESS_SAVE_STATES
            fsBw.Write(true);
#else
            fsBw.Write(false);
#endif

            fs.Position = 0x10;
            fs.Write(cartridge.RawTitle, 0, Cartridge.RAW_TITLE_LENGTH);

#if COMPRESS_SAVE_STATES
            using (BrotliStream cfs = new BrotliStream(fs, CompressionMode.Compress, true))
            using (BinaryWriter cfsBw = new BinaryWriter(cfs))
            {
                for (int cs = 0; cs < componentsStates.Length; cs++)
                {
                    if (componentsStates[cs] == null) continue;
                    componentsStates[cs].SaveOrLoadState(cfs, cfsBw, null, true);
                }
            }
#else
            for (int cs = 0; cs < componentsStates.Length; cs++)
            {
                if (componentsStates[cs] == null) continue;
                componentsStates[cs].SaveOrLoadState(fs, fsBw, null, true);
            }
#endif
        }

        return true;
    }

    public bool Load(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            Utils.Log(LogType.Error, "Save state file name cannot be null or empty.");
            return false;
        }

        if (!File.Exists(fileName))
        {
            Utils.Log(LogType.Info, $"Save state \"{fileName}\" doesn't exist.");
            return false;
        }

        using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
        using (BinaryReader fsBr = new BinaryReader(fs))
        {
            uint version = fsBr.ReadUInt32();

            if (version != SAVE_SATATE_FORMAT_VERSION)
            {
                Utils.Log(LogType.Error, $"Save state \"{fileName}\" is version {version} but version {SAVE_SATATE_FORMAT_VERSION} is expected.");
                return false;
            }

            bool compressedSaveState = fsBr.ReadBoolean();

            fs.Position = 0x10;

            byte[] rawTitle = new byte[Cartridge.RAW_TITLE_LENGTH];
            fs.Read(rawTitle, 0, Cartridge.RAW_TITLE_LENGTH);

            for (int t = 0; t < Cartridge.RAW_TITLE_LENGTH; t++)
            {
                if (rawTitle[t] != cartridge.RawTitle[t])
                {
                    Utils.Log(LogType.Error, $"Save state \"{fileName}\" is from a different game or a different version of the game ({Encoding.ASCII.GetString(rawTitle)}).");
                    return false;
                }
            }

            if (compressedSaveState)
            {
                using (BrotliStream cfs = new BrotliStream(fs, CompressionMode.Decompress, true))
                using (BinaryReader cfsBr = new BinaryReader(cfs))
                {
                    for (int cs = 0; cs < componentsStates.Length; cs++)
                    {
                        if (componentsStates[cs] == null) continue;
                        componentsStates[cs].SaveOrLoadState(cfs, null, cfsBr, false);
                    }
                }
            }
            else
            {
                for (int cs = 0; cs < componentsStates.Length; cs++)
                {
                    if (componentsStates[cs] == null) continue;
                    componentsStates[cs].SaveOrLoadState(fs, null, fsBr, false);
                }
            }
        }

        return true;
    }

    #region Save / Load Helpers

    public static bool SaveLoadValue(BinaryWriter bw, BinaryReader br, bool save, bool value)
    {
        if (save)
        {
            bw.Write(value);
            return value;
        }
        else
        {
            return br.ReadBoolean();
        }
    }

    public static byte SaveLoadValue(BinaryWriter bw, BinaryReader br, bool save, byte value)
    {
        if (save)
        {
            bw.Write(value);
            return value;
        }
        else
        {
            return br.ReadByte();
        }
    }

    public static ushort SaveLoadValue(BinaryWriter bw, BinaryReader br, bool save, ushort value)
    {
        if (save)
        {
            bw.Write(value);
            return value;
        }
        else
        {
            return br.ReadUInt16();
        }
    }

    public static int SaveLoadValue(BinaryWriter bw, BinaryReader br, bool save, int value)
    {
        if (save)
        {
            bw.Write(value);
            return value;
        }
        else
        {
            return br.ReadInt32();
        }
    }

    public static uint SaveLoadValue(BinaryWriter bw, BinaryReader br, bool save, uint value)
    {
        if (save)
        {
            bw.Write(value);
            return value;
        }
        else
        {
            return br.ReadUInt32();
        }
    }

    public static float SaveLoadValue(BinaryWriter bw, BinaryReader br, bool save, float value)
    {
        if (save)
        {
            bw.Write(value);
            return value;
        }
        else
        {
            return br.ReadSingle();
        }
    }

    public static void SaveLoadArray(Stream stream, bool save, byte[] array, int count)
    {
        if (save) stream.Write(array, 0, count);
        else stream.Read(array, 0, count);
    }

    #endregion
}