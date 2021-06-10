using System.IO;
using System.IO.Compression;

namespace DamageBoy.Core.State
{
    internal class SaveState
    {
        readonly IState[] componentsStates;
        readonly Cartridge cartridge;

        const uint SAVE_SATATE_FORMAT_VERSION = 1;

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

                fs.Position = 0x10;
                fs.Write(cartridge.RawTitle, 0, Cartridge.RAW_TITLE_LENGTH);

                // Write state of all components with GZip compression

                using (GZipStream gzip = new GZipStream(fs, CompressionMode.Compress, true))
                using (BinaryWriter gzipBw = new BinaryWriter(gzip))
                {
                    for (int cs = 0; cs < componentsStates.Length; cs++)
                    {
                        componentsStates[cs].LoadSaveState(gzip, gzipBw, null, true);
                    }
                }
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
            using (BinaryReader br = new BinaryReader(fs))
            {
                uint version = br.ReadUInt32();

                if (version != SAVE_SATATE_FORMAT_VERSION)
                {
                    Utils.Log(LogType.Error, $"Save state \"{fileName}\" is version {version} but version {SAVE_SATATE_FORMAT_VERSION} is expected.");
                    return false;
                }

                fs.Position = 0x10;

                byte[] rawTitle = new byte[Cartridge.RAW_TITLE_LENGTH];
                fs.Read(rawTitle, 0, Cartridge.RAW_TITLE_LENGTH);

                for (int t = 0; t < Cartridge.RAW_TITLE_LENGTH; t++)
                {
                    if (rawTitle[t] != cartridge.RawTitle[t])
                    {
                        Utils.Log(LogType.Error, $"Save state \"{fileName}\" is from a different game or a different version of the game.");
                        return false;
                    }
                }

                // Read state of all components

                using (GZipStream gzip = new GZipStream(fs, CompressionMode.Decompress, true))
                using (BinaryReader gzipBr = new BinaryReader(gzip))
                {
                    for (int cs = 0; cs < componentsStates.Length; cs++)
                    {
                        componentsStates[cs].LoadSaveState(gzip, null, gzipBr, false);
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
}