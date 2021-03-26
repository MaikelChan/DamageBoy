using GBEmu.Core.State;

namespace GBEmu.Core.MemoryBankControllers
{
    interface IMemoryBankController
    {
        byte this[int index] { get; set; }

        MemoryBankControllerState GetState();

        void SetState(MemoryBankControllerState state);
    }
}