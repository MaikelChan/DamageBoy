using DamageBoy.Core.State;

namespace DamageBoy.Core.MemoryBankControllers
{
    interface IMemoryBankController
    {
        byte this[int index] { get; set; }

        MemoryBankControllerState GetState();

        void SetState(MemoryBankControllerState state);
    }
}