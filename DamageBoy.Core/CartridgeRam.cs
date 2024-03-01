namespace DamageBoy.Core;

class CartridgeRam
{
    readonly byte[] bytes;
    public byte[] Bytes => bytes;

    public bool HasBeenModified { get; set; }

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
            HasBeenModified = true;
        }
    }

    public CartridgeRam(int ramSize, byte[] saveData)
    {
        HasBeenModified = false;

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
}