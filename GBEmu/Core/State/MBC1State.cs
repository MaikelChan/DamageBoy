using static GBEmu.Core.MemoryBankControllers.MBC1;

namespace GBEmu.Core.State
{
    class MBC1State : MemoryBankControllerState
    {
        public byte RomBank { get; set; }
        public byte upperRomOrRamBank { get; set; }
        public BankingModes bankingMode { get; set; }
    }
}