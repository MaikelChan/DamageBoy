namespace GBEmu.Core.MemoryBankControllers
{
    interface IMemoryBankController
    {
        byte this[int index] { get; set; }
    }
}