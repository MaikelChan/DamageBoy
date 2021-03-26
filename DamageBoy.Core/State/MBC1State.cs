using static DamageBoy.Core.MemoryBankControllers.MBC1;

namespace DamageBoy.Core.State
{
    class MBC1State : MemoryBankControllerState
    {
        public byte RomBank { get; set; }
        public byte upperRomOrRamBank { get; set; }
        public BankingModes bankingMode { get; set; }
    }
}