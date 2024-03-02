using DamageBoy.Core.State;
using System;
using System.IO;
using System.Text;

namespace DamageBoy.Core;

class CPU : IDisposable, IState
{
    readonly GameBoyModes gameBoyMode;
    readonly MMU mmu;

    int clocksToWait;
    bool isHalted;

    public const int CPU_CLOCKS = 4 * 1024 * 1024; // 4MHz

    // Registers

    byte a, b, c, d, e, f, h, l;

    byte A { get => a; set => a = value; }
    byte B { get => b; set => b = value; }
    byte C { get => c; set => c = value; }
    byte D { get => d; set => d = value; }
    byte E { get => e; set => e = value; }
    byte F { get => f; set => f = (byte)(value & 0b1111_0000); }
    byte H { get => h; set => h = value; }
    byte L { get => l; set => l = value; }

    ushort SP, PC;

    ushort AF
    {
        get { return (ushort)((A << 8) | F); }
        set { A = (byte)(value >> 8); F = (byte)(value & 0xFF); }
    }
    ushort BC
    {
        get { return (ushort)((B << 8) | C); }
        set { B = (byte)(value >> 8); C = (byte)(value & 0xFF); }
    }
    ushort DE
    {
        get { return (ushort)((D << 8) | E); }
        set { D = (byte)(value >> 8); E = (byte)(value & 0xFF); }
    }
    ushort HL
    {
        get { return (ushort)((H << 8) | L); }
        set { H = (byte)(value >> 8); L = (byte)(value & 0xFF); }
    }

    // Flags

    bool ZeroFlag
    {
        get => Helpers.GetBit(F, 7);
        set => F = Helpers.SetBit(F, 7, value);
    }
    bool NegationFlag
    {
        get => Helpers.GetBit(F, 6);
        set => F = Helpers.SetBit(F, 6, value);
    }
    bool HalfCarryFlag
    {
        get => Helpers.GetBit(F, 5);
        set => F = Helpers.SetBit(F, 5, value);
    }
    bool CarryFlag
    {
        get => Helpers.GetBit(F, 4);
        set => F = Helpers.SetBit(F, 4, value);
    }

    public CPU(GameBoyModes gameBoyMode, MMU mmu, bool isThereBootRom)
    {
        this.gameBoyMode = gameBoyMode;
        this.mmu = mmu;

        // If there's no boot ROM, the emulator will initialize some stuff.
        // If there is, the boot ROM itself will do the initialization.
        // Initial register values depend on each hardware (GB, GBC, Super GB, and some others)
        // These are for the normal GB. Most games check A and B to detect hardware.

        if (!isThereBootRom)
        {
#if GBC // TODO: https://gbdev.io/pandocs/Power_Up_Sequence.html#cpu-registers
            if (gameBoyMode == GameBoyModes.CGB)
            {
                AF = 0x1180;
                BC = 0x0000;
                DE = 0xFF56;
                HL = 0x000D;
            }
            else
            {
                AF = 0x1180;
                BC = 0x0000;
                DE = 0x0008;
                HL = 0x007C;
            }
#else
            AF = 0x01B0;
            BC = 0x0013;
            DE = 0x00D8;
            HL = 0x014D;
#endif
            SP = 0xFFFE;
            PC = 0x0100;

            // Serial

            mmu[0xFF01] = 0x00;
            mmu[0xFF02] = 0x7E;

            // Timers

            mmu[0xFF04] = 0xCE;
            mmu[0xFF05] = 0x00;
            mmu[0xFF06] = 0x00;
            mmu[0xFF07] = 0xF8;

            // Sound

            mmu[0xFF10] = 0x80;
            mmu[0xFF11] = 0xBF;
            mmu[0xFF12] = 0xF3;
            mmu[0xFF13] = 0xFF;
            mmu[0xFF14] = 0xBF;

            mmu[0xFF16] = 0x3F;
            mmu[0xFF17] = 0x00;
            mmu[0xFF18] = 0xFF;
            mmu[0xFF19] = 0xBF;

            mmu[0xFF1A] = 0x7F;
            mmu[0xFF1B] = 0xFF;
            mmu[0xFF1C] = 0x9F;
            mmu[0xFF1D] = 0xFF;
            mmu[0xFF1E] = 0xBF;

            mmu[0xFF20] = 0xFF;
            mmu[0xFF21] = 0x00;
            mmu[0xFF22] = 0x00;
            mmu[0xFF23] = 0xBF;
            mmu[0xFF24] = 0x77;
            mmu[0xFF25] = 0xF3;
            mmu[0xFF26] = 0xF1;

            mmu[0xFF30] = 0x00;
            mmu[0xFF31] = 0x00;
            mmu[0xFF32] = 0x00;
            mmu[0xFF33] = 0x00;
            mmu[0xFF34] = 0x00;
            mmu[0xFF35] = 0x00;
            mmu[0xFF36] = 0x00;
            mmu[0xFF37] = 0x00;
            mmu[0xFF38] = 0x00;
            mmu[0xFF39] = 0x00;
            mmu[0xFF3A] = 0x00;
            mmu[0xFF3B] = 0x00;
            mmu[0xFF3C] = 0x00;
            mmu[0xFF3D] = 0x00;
            mmu[0xFF3E] = 0x00;
            mmu[0xFF3F] = 0x00;

            // LCD Registers

            mmu[0xFF40] = 0x91;
            mmu[0xFF41] = 0x81;
            mmu[0xFF42] = 0x00;
            mmu[0xFF43] = 0x00;
            //mmu[0xFF44] = 0x99; // Setting this manually breaks the timings 
            mmu[0xFF45] = 0x00;
            //mmu[0xFF46] = 0x00; // Setting this manually breaks the timings 
            mmu[0xFF47] = 0xFC;
            mmu[0xFF48] = 0x00;
            mmu[0xFF49] = 0x00;
            mmu[0xFF4A] = 0x00;
            mmu[0xFF4B] = 0x00;

            // Interrupts

            mmu[0xFF0F] = 0xE1;
            mmu[0xFFFF] = 0x00;

            // Disable the Boot ROM in the corresponding IO register
            mmu[0xFF50] = 0x01;
        }

#if DEBUG
        bootromFinishedExecuting = false;
        totalCycles = 0;
#endif
    }

    public void Dispose()
    {
#if DEBUG
        DisableTraceLog();
#endif
    }

    public void Update()
    {
#if DEBUG
        totalCycles += 4;
#endif

        clocksToWait -= 4;
        if (clocksToWait > 0) return;

        if (isHalted)
        {
            clocksToWait = 4;
        }
        else
        {
#if DEBUG
            ProcessTraceLog();
#endif
            ProcessOpcodes();
        }

        ProcessInterrupts();
    }

    void ProcessOpcodes()
    {
        //if (PC == 0xdef8 && mmu[PC] == 0xE8 && AF == 0x1200 && mmu[SP + 1] == 0xc3 && mmu[SP] == 0x00)
        //{
        //    int a = 0;
        //}

        switch (mmu[PC])
        {
            case 0x00: NOP(); break;
            case 0x01: BC = LD_Reg_D16(); break;
            case 0x02: LD_AddressReg1_Reg2(BC, A); break;
            case 0x03: BC = INC(BC); break;
            case 0x04: B = INC_Reg(B); break;
            case 0x05: B = DEC_Reg(B); break;
            case 0x06: B = LD_Reg_D8(); break;
            case 0x07: RLCA(); break;
            case 0x08: LD_A16_SP(); break;
            case 0x09: ADD_HL_Reg(BC); break;
            case 0x0A: A = LD_Reg1_AddressReg2(BC); break;
            case 0x0B: BC = DEC(BC); break;
            case 0x0C: C = INC_Reg(C); break;
            case 0x0D: C = DEC_Reg(C); break;
            case 0x0E: C = LD_Reg_D8(); break;
            case 0x0F: RRCA(); break;
            case 0x11: DE = LD_Reg_D16(); break;
            case 0x12: LD_AddressReg1_Reg2(DE, A); break;
            case 0x13: DE = INC(DE); break;
            case 0x14: D = INC_Reg(D); break;
            case 0x15: D = DEC_Reg(D); break;
            case 0x16: D = LD_Reg_D8(); break;
            case 0x17: RLA(); break;
            case 0x18: JR_R8(); break;
            case 0x19: ADD_HL_Reg(DE); break;
            case 0x1A: A = LD_Reg1_AddressReg2(DE); break;
            case 0x1B: DE = DEC(DE); break;
            case 0x1C: E = INC_Reg(E); break;
            case 0x1D: E = DEC_Reg(E); break;
            case 0x1E: E = LD_Reg_D8(); break;
            case 0x1F: RRA(); break;
            case 0x20: JR_NZ_R8(); break;
            case 0x21: HL = LD_Reg_D16(); break;
            case 0x22: LDI_AddressHL_A(); break;
            case 0x23: HL = INC(HL); break;
            case 0x24: H = INC_Reg(H); break;
            case 0x25: H = DEC_Reg(H); break;
            case 0x26: H = LD_Reg_D8(); break;
            case 0x27: DAA(); break;
            case 0x28: JR_Z_R8(); break;
            case 0x29: ADD_HL_Reg(HL); break;
            case 0x2A: LDI_A_AddressHL(); break;
            case 0x2B: HL = DEC(HL); break;
            case 0x2C: L = INC_Reg(L); break;
            case 0x2D: L = DEC_Reg(L); break;
            case 0x2E: L = LD_Reg_D8(); break;
            case 0x2F: CPL(); break;
            case 0x30: JR_NC_R8(); break;
            case 0x31: LD_SP_D16(); break;
            case 0x32: LDD_AddressHL_A(); break;
            case 0x33: SP = INC(SP); break;
            case 0x34: INC_AddressReg(HL); break;
            case 0x35: DEC_AddressReg(HL); break;
            case 0x36: LD_Reg_D8(HL); break;
            case 0x37: SCF(); break;
            case 0x38: JR_C_R8(); break;
            case 0x39: ADD_HL_Reg(SP); break;
            case 0x3A: LDD_A_AddressHL(); break;
            case 0x3B: SP = DEC(SP); break;
            case 0x3C: A = INC_Reg(A); break;
            case 0x3D: A = DEC_Reg(A); break;
            case 0x3E: A = LD_Reg_D8(); break;
            case 0x3F: CCF(); break;
            case 0x40: B = LD_Reg1_Reg2(B); break;
            case 0x41: B = LD_Reg1_Reg2(C); break;
            case 0x42: B = LD_Reg1_Reg2(D); break;
            case 0x43: B = LD_Reg1_Reg2(E); break;
            case 0x44: B = LD_Reg1_Reg2(H); break;
            case 0x45: B = LD_Reg1_Reg2(L); break;
            case 0x46: B = LD_Reg1_AddressReg2(HL); break;
            case 0x47: B = LD_Reg1_Reg2(A); break;
            case 0x48: C = LD_Reg1_Reg2(B); break;
            case 0x49: C = LD_Reg1_Reg2(C); break;
            case 0x4A: C = LD_Reg1_Reg2(D); break;
            case 0x4B: C = LD_Reg1_Reg2(E); break;
            case 0x4C: C = LD_Reg1_Reg2(H); break;
            case 0x4D: C = LD_Reg1_Reg2(L); break;
            case 0x4E: C = LD_Reg1_AddressReg2(HL); break;
            case 0x4F: C = LD_Reg1_Reg2(A); break;
            case 0x50: D = LD_Reg1_Reg2(B); break;
            case 0x51: D = LD_Reg1_Reg2(C); break;
            case 0x52: D = LD_Reg1_Reg2(D); break;
            case 0x53: D = LD_Reg1_Reg2(E); break;
            case 0x54: D = LD_Reg1_Reg2(H); break;
            case 0x55: D = LD_Reg1_Reg2(L); break;
            case 0x56: D = LD_Reg1_AddressReg2(HL); break;
            case 0x57: D = LD_Reg1_Reg2(A); break;
            case 0x58: E = LD_Reg1_Reg2(B); break;
            case 0x59: E = LD_Reg1_Reg2(C); break;
            case 0x5A: E = LD_Reg1_Reg2(D); break;
            case 0x5B: E = LD_Reg1_Reg2(E); break;
            case 0x5C: E = LD_Reg1_Reg2(H); break;
            case 0x5D: E = LD_Reg1_Reg2(L); break;
            case 0x5E: E = LD_Reg1_AddressReg2(HL); break;
            case 0x5F: E = LD_Reg1_Reg2(A); break;
            case 0x60: H = LD_Reg1_Reg2(B); break;
            case 0x61: H = LD_Reg1_Reg2(C); break;
            case 0x62: H = LD_Reg1_Reg2(D); break;
            case 0x63: H = LD_Reg1_Reg2(E); break;
            case 0x64: H = LD_Reg1_Reg2(H); break;
            case 0x65: H = LD_Reg1_Reg2(L); break;
            case 0x66: H = LD_Reg1_AddressReg2(HL); break;
            case 0x67: H = LD_Reg1_Reg2(A); break;
            case 0x68: L = LD_Reg1_Reg2(B); break;
            case 0x69: L = LD_Reg1_Reg2(C); break;
            case 0x6A: L = LD_Reg1_Reg2(D); break;
            case 0x6B: L = LD_Reg1_Reg2(E); break;
            case 0x6C: L = LD_Reg1_Reg2(H); break;
            case 0x6D: L = LD_Reg1_Reg2(L); break;
            case 0x6E: L = LD_Reg1_AddressReg2(HL); break;
            case 0x6F: L = LD_Reg1_Reg2(A); break;
            case 0x70: LD_AddressReg1_Reg2(HL, B); break;
            case 0x71: LD_AddressReg1_Reg2(HL, C); break;
            case 0x72: LD_AddressReg1_Reg2(HL, D); break;
            case 0x73: LD_AddressReg1_Reg2(HL, E); break;
            case 0x74: LD_AddressReg1_Reg2(HL, H); break;
            case 0x75: LD_AddressReg1_Reg2(HL, L); break;
            case 0x76: HALT(); break;
            case 0x77: LD_AddressReg1_Reg2(HL, A); break;
            case 0x78: A = LD_Reg1_Reg2(B); break;
            case 0x79: A = LD_Reg1_Reg2(C); break;
            case 0x7A: A = LD_Reg1_Reg2(D); break;
            case 0x7B: A = LD_Reg1_Reg2(E); break;
            case 0x7C: A = LD_Reg1_Reg2(H); break;
            case 0x7D: A = LD_Reg1_Reg2(L); break;
            case 0x7E: A = LD_Reg1_AddressReg2(HL); break;
            case 0x7F: A = LD_Reg1_Reg2(A); break;
            case 0x80: ADD_Reg(B); break;
            case 0x81: ADD_Reg(C); break;
            case 0x82: ADD_Reg(D); break;
            case 0x83: ADD_Reg(E); break;
            case 0x84: ADD_Reg(H); break;
            case 0x85: ADD_Reg(L); break;
            case 0x86: ADD_AddressReg(HL); break;
            case 0x87: ADD_Reg(A); break;
            case 0x88: ADC_A_Reg(B); break;
            case 0x89: ADC_A_Reg(C); break;
            case 0x8A: ADC_A_Reg(D); break;
            case 0x8B: ADC_A_Reg(E); break;
            case 0x8C: ADC_A_Reg(H); break;
            case 0x8D: ADC_A_Reg(L); break;
            case 0x8E: ADC_A_AddressReg(HL); break;
            case 0x8F: ADC_A_Reg(A); break;
            case 0x90: SUB_Reg(B); break;
            case 0x91: SUB_Reg(C); break;
            case 0x92: SUB_Reg(D); break;
            case 0x93: SUB_Reg(E); break;
            case 0x94: SUB_Reg(H); break;
            case 0x95: SUB_Reg(L); break;
            case 0x96: SUB_AddressReg(HL); break;
            case 0x97: SUB_Reg(A); break;
            case 0x98: SBC_A_Reg(B); break;
            case 0x99: SBC_A_Reg(C); break;
            case 0x9A: SBC_A_Reg(D); break;
            case 0x9B: SBC_A_Reg(E); break;
            case 0x9C: SBC_A_Reg(H); break;
            case 0x9D: SBC_A_Reg(L); break;
            case 0x9E: SBC_A_AddressReg(HL); break;
            case 0x9F: SBC_A_Reg(A); break;
            case 0xA0: AND_Reg(B); break;
            case 0xA1: AND_Reg(C); break;
            case 0xA2: AND_Reg(D); break;
            case 0xA3: AND_Reg(E); break;
            case 0xA4: AND_Reg(H); break;
            case 0xA5: AND_Reg(L); break;
            case 0xA6: AND_AddressReg(HL); break;
            case 0xA7: AND_Reg(A); break;
            case 0xA8: XOR_Reg(B); break;
            case 0xA9: XOR_Reg(C); break;
            case 0xAA: XOR_Reg(D); break;
            case 0xAB: XOR_Reg(E); break;
            case 0xAC: XOR_Reg(H); break;
            case 0xAD: XOR_Reg(L); break;
            case 0xAE: XOR_AddressReg(HL); break;
            case 0xAF: XOR_Reg(A); break;
            case 0xB0: OR_Reg(B); break;
            case 0xB1: OR_Reg(C); break;
            case 0xB2: OR_Reg(D); break;
            case 0xB3: OR_Reg(E); break;
            case 0xB4: OR_Reg(H); break;
            case 0xB5: OR_Reg(L); break;
            case 0xB6: OR_AddressReg(HL); break;
            case 0xB7: OR_Reg(A); break;
            case 0xB8: CP_Reg(B); break;
            case 0xB9: CP_Reg(C); break;
            case 0xBA: CP_Reg(D); break;
            case 0xBB: CP_Reg(E); break;
            case 0xBC: CP_Reg(H); break;
            case 0xBD: CP_Reg(L); break;
            case 0xBE: CP_AddressReg(HL); break;
            case 0xBF: CP_Reg(A); break;
            case 0xC0: RET_NZ(); break;
            case 0xC1: BC = POP(); break;
            case 0xC2: JP_NZ_A16(); break;
            case 0xC3: JP_A16(); break;
            case 0xC4: CALL_NZ_A16(); break;
            case 0xC5: PUSH(BC); break;
            case 0xC6: ADD_A_D8(); break;
            case 0xC7: RST(0x00); break;
            case 0xC8: RET_Z(); break;
            case 0xC9: RET(); break;
            case 0xCA: JP_Z_A16(); break;
            case 0xCC: CALL_Z_A16(); break;
            case 0xCD: CALL_A16(); break;
            case 0xCE: ADC_A_D8(); break;
            case 0xCF: RST(0x08); break;
            case 0xD0: RET_NC(); break;
            case 0xD1: DE = POP(); break;
            case 0xD2: JP_NC_A16(); break;
            case 0xD4: CALL_NC_A16(); break;
            case 0xD5: PUSH(DE); break;
            case 0xD6: SUB_A_D8(); break;
            case 0xD7: RST(0x10); break;
            case 0xD8: RET_C(); break;
            case 0xD9: RETI(); break;
            case 0xDA: JP_C_A16(); break;
            case 0xDC: CALL_C_A16(); break;
            case 0xDE: SBC_A_D8(); break;
            case 0xDF: RST(0x18); break;
            case 0xE0: LDH_A8_A(); break;
            case 0xE1: HL = POP(); break;
            case 0xE2: LD_AddressC_A(); break;
            case 0xE5: PUSH(HL); break;
            case 0xE6: AND_D8(); break;
            case 0xE7: RST(0x20); break;
            case 0xE8: ADD_SP_R8(); break;
            case 0xE9: JP_HL(); break;
            case 0xEA: LDH_A16_A(); break;
            case 0xEE: XOR_A_D8(); break;
            case 0xEF: RST(0x28); break;
            case 0xF0: LDH_A_A8(); break;
            case 0xF1: AF = POP(); break;
            case 0xF2: LD_A_AddressC(); break;
            case 0xF3: DI(); break;
            case 0xF5: PUSH(AF); break;
            case 0xF6: OR_D8(); break;
            case 0xF7: RST(0x30); break;
            case 0xF8: LD_HL_SP(); break;
            case 0xF9: LD_SP_HL(); break;
            case 0xFA: LDH_A_A16(); break;
            case 0xFB: EI(); break;
            case 0xFE: CP_D8(); break;
            case 0xFF: RST(0x38); break;

            case 0xCB:

                switch (mmu[PC + 1])
                {
                    case 0x00: B = RLC_Reg(B); break;
                    case 0x01: C = RLC_Reg(C); break;
                    case 0x02: D = RLC_Reg(D); break;
                    case 0x03: E = RLC_Reg(E); break;
                    case 0x04: H = RLC_Reg(H); break;
                    case 0x05: L = RLC_Reg(L); break;
                    case 0x06: RLC_AddressReg(HL); break;
                    case 0x07: A = RLC_Reg(A); break;
                    case 0x08: B = RRC_Reg(B); break;
                    case 0x09: C = RRC_Reg(C); break;
                    case 0x0A: D = RRC_Reg(D); break;
                    case 0x0B: E = RRC_Reg(E); break;
                    case 0x0C: H = RRC_Reg(H); break;
                    case 0x0D: L = RRC_Reg(L); break;
                    case 0x0E: RRC_AddressReg(HL); break;
                    case 0x0F: A = RRC_Reg(A); break;
                    case 0x10: B = RL_Reg(B); break;
                    case 0x11: C = RL_Reg(C); break;
                    case 0x12: D = RL_Reg(D); break;
                    case 0x13: E = RL_Reg(E); break;
                    case 0x14: H = RL_Reg(H); break;
                    case 0x15: L = RL_Reg(L); break;
                    case 0x16: RL_AddressReg(HL); break;
                    case 0x17: A = RL_Reg(A); break;
                    case 0x18: B = RR_Reg(B); break;
                    case 0x19: C = RR_Reg(C); break;
                    case 0x1A: D = RR_Reg(D); break;
                    case 0x1B: E = RR_Reg(E); break;
                    case 0x1C: H = RR_Reg(H); break;
                    case 0x1D: L = RR_Reg(L); break;
                    case 0x1E: RR_AddressReg(HL); break;
                    case 0x1F: A = RR_Reg(A); break;
                    case 0x20: B = SLA_Reg(B); break;
                    case 0x21: C = SLA_Reg(C); break;
                    case 0x22: D = SLA_Reg(D); break;
                    case 0x23: E = SLA_Reg(E); break;
                    case 0x24: H = SLA_Reg(H); break;
                    case 0x25: L = SLA_Reg(L); break;
                    case 0x26: SLA_AddressReg(HL); break;
                    case 0x27: A = SLA_Reg(A); break;
                    case 0x28: B = SRA_Reg(B); break;
                    case 0x29: C = SRA_Reg(C); break;
                    case 0x2A: D = SRA_Reg(D); break;
                    case 0x2B: E = SRA_Reg(E); break;
                    case 0x2C: H = SRA_Reg(H); break;
                    case 0x2D: L = SRA_Reg(L); break;
                    case 0x2E: SRA_AddressReg(HL); break;
                    case 0x2F: A = SRA_Reg(A); break;
                    case 0x30: B = SWAP_Reg(B); break;
                    case 0x31: C = SWAP_Reg(C); break;
                    case 0x32: D = SWAP_Reg(D); break;
                    case 0x33: E = SWAP_Reg(E); break;
                    case 0x34: H = SWAP_Reg(H); break;
                    case 0x35: L = SWAP_Reg(L); break;
                    case 0x36: SWAP_AddressReg(HL); break;
                    case 0x37: A = SWAP_Reg(A); break;
                    case 0x38: B = SRL_Reg(B); break;
                    case 0x39: C = SRL_Reg(C); break;
                    case 0x3A: D = SRL_Reg(D); break;
                    case 0x3B: E = SRL_Reg(E); break;
                    case 0x3C: H = SRL_Reg(H); break;
                    case 0x3D: L = SRL_Reg(L); break;
                    case 0x3E: SRL_AddressReg(HL); break;
                    case 0x3F: A = SRL_Reg(A); break;
                    case 0x40: BIT(0, B); break;
                    case 0x41: BIT(0, C); break;
                    case 0x42: BIT(0, D); break;
                    case 0x43: BIT(0, E); break;
                    case 0x44: BIT(0, H); break;
                    case 0x45: BIT(0, L); break;
                    case 0x46: BIT(0, HL); break;
                    case 0x47: BIT(0, A); break;
                    case 0x48: BIT(1, B); break;
                    case 0x49: BIT(1, C); break;
                    case 0x4A: BIT(1, D); break;
                    case 0x4B: BIT(1, E); break;
                    case 0x4C: BIT(1, H); break;
                    case 0x4D: BIT(1, L); break;
                    case 0x4E: BIT(1, HL); break;
                    case 0x4F: BIT(1, A); break;
                    case 0x50: BIT(2, B); break;
                    case 0x51: BIT(2, C); break;
                    case 0x52: BIT(2, D); break;
                    case 0x53: BIT(2, E); break;
                    case 0x54: BIT(2, H); break;
                    case 0x55: BIT(2, L); break;
                    case 0x56: BIT(2, HL); break;
                    case 0x57: BIT(2, A); break;
                    case 0x58: BIT(3, B); break;
                    case 0x59: BIT(3, C); break;
                    case 0x5A: BIT(3, D); break;
                    case 0x5B: BIT(3, E); break;
                    case 0x5C: BIT(3, H); break;
                    case 0x5D: BIT(3, L); break;
                    case 0x5E: BIT(3, HL); break;
                    case 0x5F: BIT(3, A); break;
                    case 0x60: BIT(4, B); break;
                    case 0x61: BIT(4, C); break;
                    case 0x62: BIT(4, D); break;
                    case 0x63: BIT(4, E); break;
                    case 0x64: BIT(4, H); break;
                    case 0x65: BIT(4, L); break;
                    case 0x66: BIT(4, HL); break;
                    case 0x67: BIT(4, A); break;
                    case 0x68: BIT(5, B); break;
                    case 0x69: BIT(5, C); break;
                    case 0x6A: BIT(5, D); break;
                    case 0x6B: BIT(5, E); break;
                    case 0x6C: BIT(5, H); break;
                    case 0x6D: BIT(5, L); break;
                    case 0x6E: BIT(5, HL); break;
                    case 0x6F: BIT(5, A); break;
                    case 0x70: BIT(6, B); break;
                    case 0x71: BIT(6, C); break;
                    case 0x72: BIT(6, D); break;
                    case 0x73: BIT(6, E); break;
                    case 0x74: BIT(6, H); break;
                    case 0x75: BIT(6, L); break;
                    case 0x76: BIT(6, HL); break;
                    case 0x77: BIT(6, A); break;
                    case 0x78: BIT(7, B); break;
                    case 0x79: BIT(7, C); break;
                    case 0x7A: BIT(7, D); break;
                    case 0x7B: BIT(7, E); break;
                    case 0x7C: BIT(7, H); break;
                    case 0x7D: BIT(7, L); break;
                    case 0x7E: BIT(7, HL); break;
                    case 0x7F: BIT(7, A); break;
                    case 0x80: B = RES(0, B); break;
                    case 0x81: C = RES(0, C); break;
                    case 0x82: D = RES(0, D); break;
                    case 0x83: E = RES(0, E); break;
                    case 0x84: H = RES(0, H); break;
                    case 0x85: L = RES(0, L); break;
                    case 0x86: RES(0, HL); break;
                    case 0x87: A = RES(0, A); break;
                    case 0x88: B = RES(1, B); break;
                    case 0x89: C = RES(1, C); break;
                    case 0x8A: D = RES(1, D); break;
                    case 0x8B: E = RES(1, E); break;
                    case 0x8C: H = RES(1, H); break;
                    case 0x8D: L = RES(1, L); break;
                    case 0x8E: RES(1, HL); break;
                    case 0x8F: A = RES(1, A); break;
                    case 0x90: B = RES(2, B); break;
                    case 0x91: C = RES(2, C); break;
                    case 0x92: D = RES(2, D); break;
                    case 0x93: E = RES(2, E); break;
                    case 0x94: H = RES(2, H); break;
                    case 0x95: L = RES(2, L); break;
                    case 0x96: RES(2, HL); break;
                    case 0x97: A = RES(2, A); break;
                    case 0x98: B = RES(3, B); break;
                    case 0x99: C = RES(3, C); break;
                    case 0x9A: D = RES(3, D); break;
                    case 0x9B: E = RES(3, E); break;
                    case 0x9C: H = RES(3, H); break;
                    case 0x9D: L = RES(3, L); break;
                    case 0x9E: RES(3, HL); break;
                    case 0x9F: A = RES(3, A); break;
                    case 0xA0: B = RES(4, B); break;
                    case 0xA1: C = RES(4, C); break;
                    case 0xA2: D = RES(4, D); break;
                    case 0xA3: E = RES(4, E); break;
                    case 0xA4: H = RES(4, H); break;
                    case 0xA5: L = RES(4, L); break;
                    case 0xA6: RES(4, HL); break;
                    case 0xA7: A = RES(4, A); break;
                    case 0xA8: B = RES(5, B); break;
                    case 0xA9: C = RES(5, C); break;
                    case 0xAA: D = RES(5, D); break;
                    case 0xAB: E = RES(5, E); break;
                    case 0xAC: H = RES(5, H); break;
                    case 0xAD: L = RES(5, L); break;
                    case 0xAE: RES(5, HL); break;
                    case 0xAF: A = RES(5, A); break;
                    case 0xB0: B = RES(6, B); break;
                    case 0xB1: C = RES(6, C); break;
                    case 0xB2: D = RES(6, D); break;
                    case 0xB3: E = RES(6, E); break;
                    case 0xB4: H = RES(6, H); break;
                    case 0xB5: L = RES(6, L); break;
                    case 0xB6: RES(6, HL); break;
                    case 0xB7: A = RES(6, A); break;
                    case 0xB8: B = RES(7, B); break;
                    case 0xB9: C = RES(7, C); break;
                    case 0xBA: D = RES(7, D); break;
                    case 0xBB: E = RES(7, E); break;
                    case 0xBC: H = RES(7, H); break;
                    case 0xBD: L = RES(7, L); break;
                    case 0xBE: RES(7, HL); break;
                    case 0xBF: A = RES(7, A); break;
                    case 0xC0: B = SET(0, B); break;
                    case 0xC1: C = SET(0, C); break;
                    case 0xC2: D = SET(0, D); break;
                    case 0xC3: E = SET(0, E); break;
                    case 0xC4: H = SET(0, H); break;
                    case 0xC5: L = SET(0, L); break;
                    case 0xC6: SET(0, HL); break;
                    case 0xC7: A = SET(0, A); break;
                    case 0xC8: B = SET(1, B); break;
                    case 0xC9: C = SET(1, C); break;
                    case 0xCA: D = SET(1, D); break;
                    case 0xCB: E = SET(1, E); break;
                    case 0xCC: H = SET(1, H); break;
                    case 0xCD: L = SET(1, L); break;
                    case 0xCE: SET(1, HL); break;
                    case 0xCF: A = SET(1, A); break;
                    case 0xD0: B = SET(2, B); break;
                    case 0xD1: C = SET(2, C); break;
                    case 0xD2: D = SET(2, D); break;
                    case 0xD3: E = SET(2, E); break;
                    case 0xD4: H = SET(2, H); break;
                    case 0xD5: L = SET(2, L); break;
                    case 0xD6: SET(2, HL); break;
                    case 0xD7: A = SET(2, A); break;
                    case 0xD8: B = SET(3, B); break;
                    case 0xD9: C = SET(3, C); break;
                    case 0xDA: D = SET(3, D); break;
                    case 0xDB: E = SET(3, E); break;
                    case 0xDC: H = SET(3, H); break;
                    case 0xDD: L = SET(3, L); break;
                    case 0xDE: SET(3, HL); break;
                    case 0xDF: A = SET(3, A); break;
                    case 0xE0: B = SET(4, B); break;
                    case 0xE1: C = SET(4, C); break;
                    case 0xE2: D = SET(4, D); break;
                    case 0xE3: E = SET(4, E); break;
                    case 0xE4: H = SET(4, H); break;
                    case 0xE5: L = SET(4, L); break;
                    case 0xE6: SET(4, HL); break;
                    case 0xE7: A = SET(4, A); break;
                    case 0xE8: B = SET(5, B); break;
                    case 0xE9: C = SET(5, C); break;
                    case 0xEA: D = SET(5, D); break;
                    case 0xEB: E = SET(5, E); break;
                    case 0xEC: H = SET(5, H); break;
                    case 0xED: L = SET(5, L); break;
                    case 0xEE: SET(5, HL); break;
                    case 0xEF: A = SET(5, A); break;
                    case 0xF0: B = SET(6, B); break;
                    case 0xF1: C = SET(6, C); break;
                    case 0xF2: D = SET(6, D); break;
                    case 0xF3: E = SET(6, E); break;
                    case 0xF4: H = SET(6, H); break;
                    case 0xF5: L = SET(6, L); break;
                    case 0xF6: SET(6, HL); break;
                    case 0xF7: A = SET(6, A); break;
                    case 0xF8: B = SET(7, B); break;
                    case 0xF9: C = SET(7, C); break;
                    case 0xFA: D = SET(7, D); break;
                    case 0xFB: E = SET(7, E); break;
                    case 0xFC: H = SET(7, H); break;
                    case 0xFD: L = SET(7, L); break;
                    case 0xFE: SET(7, HL); break;
                    case 0xFF: A = SET(7, A); break;
                }

                break;

            default:
                UnknownOpcode(mmu[PC]);
                break;
        }
    }

    #region Misc / Control instructions

    /// <summary>
    /// NOP | 1  4 | - - - -
    /// </summary>
    void NOP()
    {
        PC += 1;
        clocksToWait = 4;
    }

    /// <summary>
    /// EI | 1  4 | - - - -
    /// </summary>
    void EI() // TODO?
    {
        // It doesn't enable the interrupts right away, but it waits a cpu cycle.
        interruptMasterEnablePendingCycles = 2;

        PC += 1;
        clocksToWait = 4;
    }

    /// <summary>
    /// DI | 1  4 | - - - -
    /// </summary>
    void DI() // TODO?
    {
        interruptMasterEnableFlag = false;

        PC += 1;
        clocksToWait = 4;
    }

    #endregion

    #region Jumps / Calls instructions

    /// <summary>
    /// JP a16 | 3  16 | - - - -
    /// </summary>
    void JP_A16()
    {
        PC = (ushort)((mmu[PC + 2] << 8) | mmu[PC + 1]);
        clocksToWait = 16;
    }

    /// <summary>
    /// JP Z, a16 | 3  16/12 | - - - -
    /// </summary>
    void JP_Z_A16()
    {
        if (ZeroFlag)
        {
            JP_A16();
        }
        else
        {
            PC += 3;
            clocksToWait = 12;
        }
    }

    /// <summary>
    /// JP C, a16 | 3  16/12 | - - - -
    /// </summary>
    void JP_C_A16()
    {
        if (CarryFlag)
        {
            JP_A16();
        }
        else
        {
            PC += 3;
            clocksToWait = 12;
        }
    }

    /// <summary>
    /// JP NZ, a16 | 3  16/12 | - - - -
    /// </summary>
    void JP_NZ_A16()
    {
        if (!ZeroFlag)
        {
            JP_A16();
        }
        else
        {
            PC += 3;
            clocksToWait = 12;
        }
    }

    /// <summary>
    /// JP NC, a16 | 3  16/12 | - - - -
    /// </summary>
    void JP_NC_A16()
    {
        if (!CarryFlag)
        {
            JP_A16();
        }
        else
        {
            PC += 3;
            clocksToWait = 12;
        }
    }

    /// <summary>
    /// JP HL | 1  4 | - - - -
    /// </summary>
    void JP_HL()
    {
        PC = HL;
        clocksToWait = 4;
    }

    /// <summary>
    /// JR r8 | 2  12 | - - - -
    /// </summary>
    void JR_R8()
    {
        sbyte n = (sbyte)mmu[PC + 1];
        PC = (ushort)(PC + n);

        PC += 2;
        clocksToWait = 12;
    }

    /// <summary>
    /// If following condition is true then add n to current address and jump to it.
    /// Jump if Z flag is set.
    /// JR Z, r8 | 2  12/8 | - - - -
    /// </summary>
    void JR_Z_R8()
    {
        if (ZeroFlag)
        {
            JR_R8();
        }
        else
        {
            PC += 2;
            clocksToWait = 8;
        }
    }

    /// <summary>
    /// If following condition is true then add n to current address and jump to it.
    /// Jump if C flag is set.
    /// JR C, r8 | 2  12/8 | - - - -
    /// </summary>
    void JR_C_R8()
    {
        if (CarryFlag)
        {
            JR_R8();
        }
        else
        {
            PC += 2;
            clocksToWait = 8;
        }
    }

    /// <summary>
    /// If following condition is true then add n to current address and jump to it.
    /// Jump if Z flag is reset.
    /// JR NZ, r8 | 2  12/8 | - - - -
    /// </summary>
    void JR_NZ_R8()
    {
        if (!ZeroFlag)
        {
            JR_R8();
        }
        else
        {
            PC += 2;
            clocksToWait = 8;
        }
    }

    /// <summary>
    /// If following condition is true then add n to current address and jump to it.
    /// Jump if C flag is reset.
    /// JR NC, r8 | 2  12/8 | - - - -
    /// </summary>
    void JR_NC_R8()
    {
        if (!CarryFlag)
        {
            JR_R8();
        }
        else
        {
            PC += 2;
            clocksToWait = 8;
        }
    }

    /// <summary>
    /// CALL a16 | 3  24 | - - - -
    /// </summary>
    void CALL_A16()
    {
        SP -= 2;
        mmu[SP + 1] = (byte)((PC + 3) >> 8);
        mmu[SP + 0] = (byte)((PC + 3) & 0xFF);

        PC = (ushort)((mmu[PC + 2] << 8) | mmu[PC + 1]);
        clocksToWait = 24;
    }

    /// <summary>
    /// CALL Z, a16 | 3  24/12 | - - - -
    /// </summary>
    void CALL_Z_A16()
    {
        if (ZeroFlag)
        {
            CALL_A16();
        }
        else
        {
            PC += 3;
            clocksToWait = 12;
        }
    }

    /// <summary>
    /// CALL NZ, a16 | 3  24/12 | - - - -
    /// </summary>
    void CALL_NZ_A16()
    {
        if (!ZeroFlag)
        {
            CALL_A16();
        }
        else
        {
            PC += 3;
            clocksToWait = 12;
        }
    }

    /// <summary>
    /// CALL C, a16 | 3  24/12 | - - - -
    /// </summary>
    void CALL_C_A16()
    {
        if (CarryFlag)
        {
            CALL_A16();
        }
        else
        {
            PC += 3;
            clocksToWait = 12;
        }
    }

    /// <summary>
    /// CALL NC, a16 | 3  24/12 | - - - -
    /// </summary>
    void CALL_NC_A16()
    {
        if (!CarryFlag)
        {
            CALL_A16();
        }
        else
        {
            PC += 3;
            clocksToWait = 12;
        }
    }

    /// <summary>
    /// RET | 1  16 | - - - -
    /// </summary>
    void RET()
    {
        SP += 2;

        PC = (ushort)((mmu[SP - 1] << 8) | mmu[SP - 2]);
        clocksToWait = 16;
    }

    /// <summary>
    /// RET Z | 1  20/8 | - - - -
    /// </summary>
    void RET_Z()
    {
        if (ZeroFlag)
        {
            RET();

            clocksToWait = 20;
        }
        else
        {
            PC += 1;
            clocksToWait = 8;
        }
    }

    /// <summary>
    /// RET NZ | 1  20/8 | - - - -
    /// </summary>
    void RET_NZ()
    {
        if (!ZeroFlag)
        {
            RET();

            clocksToWait = 20;
        }
        else
        {
            PC += 1;
            clocksToWait = 8;
        }
    }

    /// <summary>
    /// RET C | 1  20/8 | - - - -
    /// </summary>
    void RET_C()
    {
        if (CarryFlag)
        {
            RET();

            clocksToWait = 20;
        }
        else
        {
            PC += 1;
            clocksToWait = 8;
        }
    }

    /// <summary>
    /// RET NC | 1  20/8 | - - - -
    /// </summary>
    void RET_NC()
    {
        if (!CarryFlag)
        {
            RET();

            clocksToWait = 20;
        }
        else
        {
            PC += 1;
            clocksToWait = 8;
        }
    }

    /// <summary>
    /// RETI | 1  16 | - - - -
    /// </summary>
    void RETI()
    {
        RET();
        interruptMasterEnablePendingCycles = 2;
    }

    /// <summary>
    /// RST <paramref name="offset"/> | 1  16 | - - - -
    /// </summary>
    void RST(byte offset)
    {
        SP -= 2;
        mmu[SP + 1] = (byte)((PC + 1) >> 8);
        mmu[SP + 0] = (byte)((PC + 1) & 0xFF);

        PC = offset;
        clocksToWait = 16;
    }

    /// <summary>
    /// HALT | 1  4 | - - - -
    /// </summary>
    void HALT()
    {
        isHalted = true;

        PC += 1;
        clocksToWait = 4;
    }

    #endregion

    #region 8bit load instructions

    /// <summary>
    /// LD <paramref name="register1"/>, <paramref name="register2"/> | 1  4 | - - - -
    /// </summary>
    byte LD_Reg1_Reg2(byte register2)
    {
        byte result = register2;

        PC += 1;
        clocksToWait = 4;

        return result;
    }

    /// <summary>
    /// LD <paramref name="register1"/>, (<paramref name="register2"/>) | 1  8 | - - - -
    /// </summary>
    byte LD_Reg1_AddressReg2(ushort register2)
    {
        byte result = mmu[register2];

        PC += 1;
        clocksToWait = 8;

        return result;
    }

    /// <summary>
    /// LD (<paramref name="register1"/>), <paramref name="register2"/> | 1  8 | - - - -
    /// </summary>
    void LD_AddressReg1_Reg2(ushort register1, byte register2)
    {
        mmu[register1] = register2;

        PC += 1;
        clocksToWait = 8;
    }

    /// <summary>
    /// LD <paramref name="register"/>, d8 | 2  8 | - - - -
    /// </summary>
    byte LD_Reg_D8()
    {
        byte result = mmu[PC + 1];

        PC += 2;
        clocksToWait = 8;

        return result;
    }

    /// <summary>
    /// LD (<paramref name="register"/>), d8 | 2  12 | - - - -
    /// </summary>
    void LD_Reg_D8(ushort register)
    {
        mmu[register] = mmu[PC + 1];

        PC += 2;
        clocksToWait = 12;
    }

    /// <summary>
    /// Put A into memory address HL. Decrement HL.
    /// LD (HL-), A | 1  8 | - - - -
    /// </summary>
    void LDD_AddressHL_A()
    {
        mmu[HL] = A;
        HL--;

        PC += 1;
        clocksToWait = 8;
    }

    /// <summary>
    /// Put A into memory address HL. Increment HL.
    /// LD (HL+), A | 1  8 | - - - -
    /// </summary>
    void LDI_AddressHL_A()
    {
        mmu[HL] = A;
        HL++;

        PC += 1;
        clocksToWait = 8;
    }

    /// <summary>
    /// Put value at address HL into A. Decrement HL.
    /// LD A, (HL-) | 1  8 | - - - -
    /// </summary>
    void LDD_A_AddressHL()
    {
        // OAM Bug
        // mmu.CorruptOAM(HL);

        A = mmu[HL];
        HL--;

        PC += 1;
        clocksToWait = 8;
    }

    /// <summary>
    /// Put value at address HL into A. Increment HL.
    /// LD A, (HL+) | 1  8 | - - - -
    /// </summary>
    void LDI_A_AddressHL()
    {
        // OAM Bug
        // mmu.CorruptOAM(HL);

        A = mmu[HL];
        HL++;

        PC += 1;
        clocksToWait = 8;
    }

    /// <summary>
    /// Put A into address $FF00 + register C.
    /// LD (C), A | 1  8 | - - - -
    /// </summary>
    void LD_AddressC_A()
    {
        mmu[0xff00 + C] = A;

        PC += 1;
        clocksToWait = 8;
    }

    /// <summary>
    /// Put value at address $FF00 + register C into A.
    /// LD A, (C) | 1  8 | - - - -
    /// </summary>
    void LD_A_AddressC()
    {
        A = mmu[0xff00 | C];

        PC += 1;
        clocksToWait = 8;
    }

    /// <summary>
    /// LDH (a8), A | 2  12 | - - - -
    /// Put A into memory address $FF00+n.
    /// n = one byte immediate value.
    /// </summary>
    void LDH_A8_A()
    {
        mmu[0xFF00 + mmu[PC + 1]] = A;

        PC += 2;
        clocksToWait = 12;
    }

    /// <summary>
    /// LDH A, (a8) | 2  12 | - - - -
    /// Put memory address $FF00+n into A.
    /// n = one byte immediate value.
    /// </summary>
    void LDH_A_A8()
    {
        A = mmu[0xFF00 + mmu[PC + 1]];

        PC += 2;
        clocksToWait = 12;
    }

    #endregion

    #region 16bit load instructions

    /// <summary>
    /// LD <paramref name="register"/>, d16 | 3  12 | - - - -
    /// </summary>
    ushort LD_Reg_D16()
    {
        ushort result = (ushort)((mmu[PC + 2] << 8) | (mmu[PC + 1]));

        PC += 3;
        clocksToWait = 12;

        return result;
    }

    /// <summary>
    /// LD SP, d16 | 3  12 | - - - -
    /// </summary>
    void LD_SP_D16()
    {
        SP = (ushort)((mmu[PC + 2] << 8) | mmu[PC + 1]);

        PC += 3;
        clocksToWait = 12;
    }

    /// <summary>
    /// LD (a16), SP | 3  20 | - - - -
    /// </summary>
    void LD_A16_SP()
    {
        ushort address = (ushort)((mmu[PC + 2] << 8) | mmu[PC + 1]);
        mmu[address + 0] = (byte)(SP & 0xff);
        mmu[address + 1] = (byte)(SP >> 8);

        PC += 3;
        clocksToWait = 20;
    }

    /// <summary>
    /// LD SP, HL | 1  8 | - - - -
    /// </summary>
    void LD_SP_HL()
    {
        SP = HL;

        PC += 1;
        clocksToWait = 8;
    }

    /// <summary>
    /// LD HL, SP + r8 | 2  12 | 0 0 H C
    /// </summary>
    void LD_HL_SP()
    {
        HL = Helpers.AddSigned(SP, mmu[PC + 1], out bool halfCarry, out bool carry);

        ZeroFlag = false;
        NegationFlag = false;
        HalfCarryFlag = halfCarry;
        CarryFlag = carry;

        PC += 2;
        clocksToWait = 12;
    }

    /// <summary>
    /// LD (a16), A | 3  16 | - - - -
    /// </summary>
    void LDH_A16_A()
    {
        mmu[(mmu[PC + 2] << 8) | mmu[PC + 1]] = A;

        PC += 3;
        clocksToWait = 16;
    }

    /// <summary>
    /// LD A, (a16) | 3  16 | - - - -
    /// </summary>
    void LDH_A_A16()
    {
        A = mmu[(mmu[PC + 2] << 8) | mmu[PC + 1]];

        PC += 3;
        clocksToWait = 16;
    }

    /// <summary>
    /// PUSH <paramref name="register"/> | 1  16 | - - - -
    /// </summary>
    void PUSH(ushort register)
    {
        // OAM Bug
        // mmu.CorruptOAM(register);

        SP -= 2;
        mmu[SP + 1] = (byte)(register >> 8);
        mmu[SP + 0] = (byte)(register & 0xFF);

        PC += 1;
        clocksToWait = 16;
    }

    /// <summary>
    /// POP <paramref name="register"/> | 1  12 | - - - -
    /// </summary>
    ushort POP()
    {
        SP += 2;
        ushort register = (ushort)((mmu[SP - 1] << 8) | mmu[SP - 2]);

        // OAM Bug
        // mmu.CorruptOAM(register);

        PC += 1;
        clocksToWait = 12;

        return register;
    }

    #endregion

    #region 8bit arithmetic

    /// <summary>
    /// ADD A d8 | 2  8 | Z 0 H C
    /// </summary>
    void ADD_A_D8()
    {
        A = Helpers.Add(A, mmu[PC + 1], out bool halfCarry, out bool carry);

        ZeroFlag = A == 0;
        NegationFlag = false;
        HalfCarryFlag = halfCarry;
        CarryFlag = carry;

        PC += 2;
        clocksToWait = 8;
    }

    /// <summary>
    /// ADD <paramref name="register"/> | 1  4 | Z 0 H C
    /// </summary>
    void ADD_Reg(byte register)
    {
        A = Helpers.Add(A, register, out bool halfCarry, out bool carry);

        ZeroFlag = A == 0;
        NegationFlag = false;
        HalfCarryFlag = halfCarry;
        CarryFlag = carry;

        PC += 1;
        clocksToWait = 4;
    }

    /// <summary>
    /// ADD (<paramref name="register"/>) | 1  8 | Z 0 H C
    /// </summary>
    void ADD_AddressReg(ushort register)
    {
        A = Helpers.Add(A, mmu[register], out bool halfCarry, out bool carry);

        ZeroFlag = A == 0;
        NegationFlag = false;
        HalfCarryFlag = halfCarry;
        CarryFlag = carry;

        PC += 1;
        clocksToWait = 8;
    }

    /// <summary>
    /// ADC A d8 | 2  8 | Z 0 H C
    /// </summary>
    void ADC_A_D8()
    {
        A = Helpers.AddWithCarry(A, mmu[PC + 1], CarryFlag, out bool halfCarry, out bool carry);

        ZeroFlag = A == 0;
        NegationFlag = false;
        HalfCarryFlag = halfCarry;
        CarryFlag = carry;

        PC += 2;
        clocksToWait = 8;
    }

    /// <summary>
    /// ADC A <paramref name="register"/> | 1  4 | Z 0 H C
    /// </summary>
    void ADC_A_Reg(byte register)
    {
        A = Helpers.AddWithCarry(A, register, CarryFlag, out bool halfCarry, out bool carry);

        ZeroFlag = A == 0;
        NegationFlag = false;
        HalfCarryFlag = halfCarry;
        CarryFlag = carry;

        PC += 1;
        clocksToWait = 4;
    }

    /// <summary>
    /// ADC A (<paramref name="register"/>) | 1  8 | Z 0 H C
    /// </summary>
    void ADC_A_AddressReg(ushort register)
    {
        A = Helpers.AddWithCarry(A, mmu[register], CarryFlag, out bool halfCarry, out bool carry);

        ZeroFlag = A == 0;
        NegationFlag = false;
        HalfCarryFlag = halfCarry;
        CarryFlag = carry;

        PC += 1;
        clocksToWait = 8;
    }

    /// <summary>
    /// SUB d8 | 2  8 | Z 1 H C
    /// </summary>
    void SUB_A_D8()
    {
        A = Helpers.Subtract(A, mmu[PC + 1], out bool halfCarry, out bool carry);

        ZeroFlag = A == 0;
        NegationFlag = true;
        HalfCarryFlag = halfCarry;
        CarryFlag = carry;

        PC += 2;
        clocksToWait = 8;
    }

    /// <summary>
    /// SUB <paramref name="register"/> | 1  4 | Z 1 H C
    /// </summary>
    void SUB_Reg(byte register)
    {
        A = Helpers.Subtract(A, register, out bool halfCarry, out bool carry);

        ZeroFlag = A == 0;
        NegationFlag = true;
        HalfCarryFlag = halfCarry;
        CarryFlag = carry;

        PC += 1;
        clocksToWait = 4;
    }

    /// <summary>
    /// SUB (<paramref name="register"/>) | 1  8 | Z 1 H C
    /// </summary>
    void SUB_AddressReg(ushort register)
    {
        A = Helpers.Subtract(A, mmu[register], out bool halfCarry, out bool carry);

        ZeroFlag = A == 0;
        NegationFlag = true;
        HalfCarryFlag = halfCarry;
        CarryFlag = carry;

        PC += 1;
        clocksToWait = 8;
    }

    /// <summary>
    /// SBC A <paramref name="register"/> | 1  4 | Z 1 H C
    /// </summary>
    void SBC_A_Reg(byte register)
    {
        A = Helpers.SubtractWithCarry(A, register, CarryFlag, out bool halfCarry, out bool carry);

        ZeroFlag = A == 0;
        NegationFlag = true;
        HalfCarryFlag = halfCarry;
        CarryFlag = carry;

        PC += 1;
        clocksToWait = 4;
    }

    /// <summary>
    /// SBC A <paramref name="register"/> | 1  8 | Z 1 H C
    /// </summary>
    void SBC_A_AddressReg(ushort register)
    {
        A = Helpers.SubtractWithCarry(A, mmu[register], CarryFlag, out bool halfCarry, out bool carry);

        ZeroFlag = A == 0;
        NegationFlag = true;
        HalfCarryFlag = halfCarry;
        CarryFlag = carry;

        PC += 1;
        clocksToWait = 8;
    }

    /// <summary>
    /// SBC A d8 | 2  8 | Z 1 H C
    /// </summary>
    void SBC_A_D8()
    {
        A = Helpers.SubtractWithCarry(A, mmu[PC + 1], CarryFlag, out bool halfCarry, out bool carry);

        ZeroFlag = A == 0;
        NegationFlag = true;
        HalfCarryFlag = halfCarry;
        CarryFlag = carry;

        PC += 2;
        clocksToWait = 8;
    }

    /// <summary>
    /// AND <paramref name="register"/> | 1  4 | Z 0 1 0
    /// </summary>
    void AND_Reg(byte register)
    {
        A &= register;

        ZeroFlag = A == 0;
        NegationFlag = false;
        HalfCarryFlag = true;
        CarryFlag = false;

        PC += 1;
        clocksToWait = 4;
    }

    /// <summary>
    /// AND (<paramref name="register"/>) | 1  8 | Z 0 1 0
    /// </summary>
    void AND_AddressReg(ushort register)
    {
        A &= mmu[register];

        ZeroFlag = A == 0;
        NegationFlag = false;
        HalfCarryFlag = true;
        CarryFlag = false;

        PC += 1;
        clocksToWait = 8;
    }

    /// <summary>
    /// AND d8 | 2  8 | Z 0 1 0
    /// </summary>
    void AND_D8()
    {
        A &= mmu[PC + 1];

        ZeroFlag = A == 0;
        NegationFlag = false;
        HalfCarryFlag = true;
        CarryFlag = false;

        PC += 2;
        clocksToWait = 8;
    }

    /// <summary>
    /// OR <paramref name="register"/> | 1  4 | Z 0 0 0
    /// </summary>
    void OR_Reg(byte register)
    {
        A |= register;

        ZeroFlag = A == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = false;

        PC += 1;
        clocksToWait = 4;
    }

    /// <summary>
    /// OR (<paramref name="register"/>) | 1  8 | Z 0 0 0
    /// </summary>
    void OR_AddressReg(ushort register)
    {
        A |= mmu[register];

        ZeroFlag = A == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = false;

        PC += 1;
        clocksToWait = 8;
    }

    /// <summary>
    /// OR d8 | 2  8 | Z 0 0 0
    /// </summary>
    void OR_D8()
    {
        A |= mmu[PC + 1];

        ZeroFlag = A == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = false;

        PC += 2;
        clocksToWait = 8;
    }

    /// <summary>
    /// XOR d8 | 2  8 | Z 0 0 0
    /// </summary>
    void XOR_A_D8()
    {
        A ^= mmu[PC + 1];

        ZeroFlag = A == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = false;

        PC += 2;
        clocksToWait = 8;
    }

    /// <summary>
    /// XOR <paramref name="register"/> | 1  4 | Z 0 0 0
    /// </summary>
    void XOR_Reg(byte register)
    {
        A ^= register;

        ZeroFlag = A == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = false;

        PC += 1;
        clocksToWait = 4;
    }

    /// <summary>
    /// XOR (<paramref name="register"/>) | 1  8 | Z 0 0 0
    /// </summary>
    void XOR_AddressReg(ushort register)
    {
        A ^= mmu[register];

        ZeroFlag = A == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = false;

        PC += 1;
        clocksToWait = 8;
    }

    /// <summary>
    /// INC <paramref name="register"/> | 1  4 | Z 0 H -
    /// </summary>
    byte INC_Reg(byte register)
    {
        byte result = Helpers.Add(register, 1, out bool halfCarry, out _);

        ZeroFlag = result == 0;
        NegationFlag = false;
        HalfCarryFlag = halfCarry;

        PC += 1;
        clocksToWait = 4;

        return result;
    }

    /// <summary>
    /// INC (<paramref name="register"/>) | 1  12 | Z 0 H -
    /// </summary>
    void INC_AddressReg(ushort register)
    {
        mmu[register] = Helpers.Add(mmu[register], 1, out bool halfCarry, out _);

        ZeroFlag = mmu[register] == 0;
        NegationFlag = false;
        HalfCarryFlag = halfCarry;

        PC += 1;
        clocksToWait = 12;
    }

    /// <summary>
    /// DEC <paramref name="register"/> | 1  4 | Z 1 H -
    /// </summary>
    byte DEC_Reg(byte register)
    {
        byte result = Helpers.Subtract(register, 1, out bool halfCarry, out _);

        ZeroFlag = result == 0;
        NegationFlag = true;
        HalfCarryFlag = halfCarry;

        PC += 1;
        clocksToWait = 4;

        return result;
    }

    /// <summary>
    /// DEC (<paramref name="register"/>) | 1  12 | Z 1 H -
    /// </summary>
    void DEC_AddressReg(ushort register)
    {
        mmu[register] = Helpers.Subtract(mmu[register], 1, out bool halfCarry, out _);

        ZeroFlag = mmu[register] == 0;
        NegationFlag = true;
        HalfCarryFlag = halfCarry;

        PC += 1;
        clocksToWait = 12;
    }

    /// <summary>
    /// CP <paramref name="register"/> | 1  4 | Z 1 H C
    /// </summary>
    void CP_Reg(byte register)
    {
        byte result = Helpers.Subtract(A, register, out bool halfCarry, out bool carry);

        ZeroFlag = result == 0;
        NegationFlag = true;
        HalfCarryFlag = halfCarry;
        CarryFlag = carry;

        PC += 1;
        clocksToWait = 4;
    }

    /// <summary>
    /// CP (<paramref name="register"/>) | 1  8 | Z 1 H C
    /// </summary>
    void CP_AddressReg(ushort register)
    {
        byte result = Helpers.Subtract(A, mmu[register], out bool halfCarry, out bool carry);

        ZeroFlag = result == 0;
        NegationFlag = true;
        HalfCarryFlag = halfCarry;
        CarryFlag = carry;

        PC += 1;
        clocksToWait = 8;
    }

    /// <summary>
    /// CP d8 | 2  8 | Z 1 H C
    /// </summary>
    void CP_D8()
    {
        byte result = Helpers.Subtract(A, mmu[PC + 1], out bool halfCarry, out bool carry);

        ZeroFlag = result == 0;
        NegationFlag = true;
        HalfCarryFlag = halfCarry;
        CarryFlag = carry;

        PC += 2;
        clocksToWait = 8;
    }

    /// <summary>
    /// CPL | 1  4 | - 1 1 -
    /// </summary>
    void CPL()
    {
        A = (byte)(~A);

        NegationFlag = true;
        HalfCarryFlag = true;

        PC += 1;
        clocksToWait = 4;
    }

    /// <summary>
    /// CCF | 1  4 | - 0 0 C
    /// </summary>
    void CCF()
    {
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = !CarryFlag;

        PC += 1;
        clocksToWait = 4;
    }

    /// <summary>
    /// SCF | 1  4 | - 0 0 C
    /// </summary>
    void SCF()
    {
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = true;

        PC += 1;
        clocksToWait = 4;
    }

    /// <summary>
    /// DAA | 1  4 | Z - 0 C
    /// </summary>
    void DAA()
    {
        if (!NegationFlag)
        {
            // after an addition, adjust if (half-)carry occurred or if result is out of bounds
            if (CarryFlag || A > 0x99)
            {
                A += 0x60;
                CarryFlag = true;
            }

            if (HalfCarryFlag || (A & 0x0f) > 0x09)
            {
                A += 0x6;
            }
        }
        else
        {
            // after a subtraction, only adjust if (half-)carry occurred
            if (CarryFlag)
            {
                A -= 0x60;
            }

            if (HalfCarryFlag)
            {
                A -= 0x6;
            }
        }

        ZeroFlag = A == 0;
        HalfCarryFlag = false;

        PC += 1;
        clocksToWait = 4;
    }

    #endregion

    #region 16bit arithmetic

    /// <summary>
    /// ADD HL <paramref name="register"/> | 1  8 | - 0 H C
    /// </summary>
    void ADD_HL_Reg(ushort register)
    {
        HL = Helpers.Add(HL, register, out bool halfCarry, out bool carry);

        NegationFlag = false;
        HalfCarryFlag = halfCarry;
        CarryFlag = carry;

        PC += 1;
        clocksToWait = 8;
    }

    /// <summary>
    /// ADD SP, r8 | 2  16 | 0 0 H C
    /// </summary>
    void ADD_SP_R8()
    {
        SP = Helpers.AddSigned(SP, mmu[PC + 1], out bool halfCarry, out bool carry);

        ZeroFlag = false;
        NegationFlag = false;
        HalfCarryFlag = halfCarry;
        CarryFlag = carry;

        PC += 2;
        clocksToWait = 16;
    }

    /// <summary>
    /// INC <paramref name="register"/> | 1  8 | - - - -
    /// </summary>
    ushort INC(ushort register)
    {
        // OAM Bug
        // mmu.CorruptOAM(register);

        ushort result = (ushort)(register + 1);

        PC += 1;
        clocksToWait = 8;

        return result;
    }

    /// <summary>
    /// DEC <paramref name="register"/> | 1  8 | - - - -
    /// </summary>
    ushort DEC(ushort register)
    {
        // OAM Bug
        // mmu.CorruptOAM(register);

        ushort result = (ushort)(register - 1);

        PC += 1;
        clocksToWait = 8;

        return result;
    }

    #endregion

    #region 8-bit shift, rotate and bit instructions

    /// <summary>
    /// BIT <paramref name="bit"/>, <paramref name="register"/> | 2  8 | Z 0 1 -
    /// </summary>
    void BIT(byte bit, byte register)
    {
        ZeroFlag = !Helpers.GetBit(register, bit);
        NegationFlag = false;
        HalfCarryFlag = true;

        PC += 2;
        clocksToWait = 8;
    }

    /// <summary>
    /// BIT <paramref name="bit"/>, (<paramref name="register"/>) | 2  12 | Z 0 1 -
    /// </summary>
    void BIT(byte bit, ushort register)
    {
        ZeroFlag = !Helpers.GetBit(mmu[register], bit);
        NegationFlag = false;
        HalfCarryFlag = true;

        PC += 2;
        clocksToWait = 12;
    }

    /// <summary>
    /// SET <paramref name="bit"/>, <paramref name="register"/> | 2  8 | - - - -
    /// </summary>
    byte SET(byte bit, byte register)
    {
        byte result = Helpers.SetBit(register, bit, true);

        PC += 2;
        clocksToWait = 8;

        return result;
    }

    /// <summary>
    /// SET <paramref name="bit"/>, (<paramref name="register"/>) | 2  16 | - - - -
    /// </summary>
    void SET(byte bit, ushort register)
    {
        mmu[register] = Helpers.SetBit(mmu[register], bit, true);

        PC += 2;
        clocksToWait = 16;
    }

    /// <summary>
    /// RES <paramref name="bit"/>, <paramref name="register"/> | 2  8 | - - - -
    /// </summary>
    byte RES(byte bit, byte register)
    {
        byte result = Helpers.SetBit(register, bit, false);

        PC += 2;
        clocksToWait = 8;

        return result;
    }

    /// <summary>
    /// RES <paramref name="bit"/>, (<paramref name="register"/>) | 2  16 | - - - -
    /// </summary>
    void RES(byte bit, ushort register)
    {
        mmu[register] = Helpers.SetBit(mmu[register], bit, false);

        PC += 2;
        clocksToWait = 16;
    }

    /// <summary>
    /// RLCA | 1  4 | 0 0 0 C
    /// </summary>
    void RLCA()
    {
        byte bit7 = (byte)(A >> 7);
        A <<= 1;
        A |= bit7;

        ZeroFlag = false;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = bit7 == 1;

        PC += 1;
        clocksToWait = 4;
    }

    /// <summary>
    /// RRCA | 1  4 | 0 0 0 C
    /// </summary>
    void RRCA()
    {
        byte bit0 = (byte)(A & 0x1);
        A >>= 1;
        A |= (byte)(bit0 << 7);

        ZeroFlag = false;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = bit0 == 1;

        PC += 1;
        clocksToWait = 4;
    }

    /// <summary>
    /// RLA | 1  4 | 0 0 0 C
    /// </summary>
    void RLA()
    {
        byte bit7 = (byte)(A >> 7);
        A <<= 1;
        A |= (byte)(CarryFlag ? 1 : 0);

        ZeroFlag = false;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = bit7 == 1;

        PC += 1;
        clocksToWait = 4;
    }

    /// <summary>
    /// RRA | 1  4 | 0 0 0 C
    /// </summary>
    void RRA()
    {
        byte bit0 = (byte)(A & 0x1);
        A >>= 1;
        A |= (byte)(CarryFlag ? 0b1000_0000 : 0);

        ZeroFlag = false;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = bit0 == 1;

        PC += 1;
        clocksToWait = 4;
    }

    /// <summary>
    /// RL <paramref name="register"/> | 2  8 | Z 0 0 C
    /// </summary>
    byte RL_Reg(byte register)
    {
        byte bit7 = (byte)(register >> 7);
        byte result = (byte)(register << 1);
        result |= (byte)(CarryFlag ? 1 : 0);

        ZeroFlag = result == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = bit7 == 1;

        PC += 2;
        clocksToWait = 8;

        return result;
    }

    /// <summary>
    /// RL (<paramref name="register"/>) | 2  16 | Z 0 0 C
    /// </summary>
    void RL_AddressReg(ushort register)
    {
        byte bit7 = (byte)(mmu[register] >> 7);
        mmu[register] <<= 1;
        mmu[register] |= (byte)(CarryFlag ? 1 : 0);

        ZeroFlag = mmu[register] == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = bit7 == 1;

        PC += 2;
        clocksToWait = 16;
    }

    /// <summary>
    /// RR <paramref name="register"/> | 2  8 | Z 0 0 C
    /// </summary>
    byte RR_Reg(byte register)
    {
        byte bit0 = (byte)(register & 0x1);
        byte result = (byte)(register >> 1);
        result |= (byte)(CarryFlag ? 0b1000_0000 : 0);

        ZeroFlag = result == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = bit0 == 1;

        PC += 2;
        clocksToWait = 8;

        return result;
    }

    /// <summary>
    /// RR (<paramref name="register"/>) | 2  16 | Z 0 0 C
    /// </summary>
    void RR_AddressReg(ushort register)
    {
        byte bit0 = (byte)(mmu[register] & 0x1);
        mmu[register] >>= 1;
        mmu[register] |= (byte)(CarryFlag ? 0b1000_0000 : 0);

        ZeroFlag = mmu[register] == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = bit0 == 1;

        PC += 2;
        clocksToWait = 16;
    }

    /// <summary>
    /// RLC <paramref name="register"/> | 2  8 | Z 0 0 C
    /// </summary>
    byte RLC_Reg(byte register)
    {
        byte bit7 = (byte)(register >> 7);
        byte result = (byte)(register << 1);
        result |= bit7;

        ZeroFlag = result == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = bit7 == 1;

        PC += 2;
        clocksToWait = 8;

        return result;
    }

    /// <summary>
    /// RRC <paramref name="register"/> | 2  8 | Z 0 0 C
    /// </summary>
    byte RRC_Reg(byte register)
    {
        byte bit0 = (byte)(register & 0x1);
        byte result = (byte)(register >> 1);
        result |= (byte)(bit0 << 7);

        ZeroFlag = result == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = bit0 == 1;

        PC += 2;
        clocksToWait = 8;

        return result;
    }

    /// <summary>
    /// RLC (<paramref name="register"/>) | 2  16 | Z 0 0 C
    /// </summary>
    void RLC_AddressReg(ushort register)
    {
        byte bit7 = (byte)(mmu[register] >> 7);
        mmu[register] <<= 1;
        mmu[register] |= bit7;

        ZeroFlag = mmu[register] == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = bit7 == 1;

        PC += 2;
        clocksToWait = 16;
    }

    /// <summary>
    /// RRC (<paramref name="register"/>) | 2  16 | Z 0 0 C
    /// </summary>
    void RRC_AddressReg(ushort register)
    {
        byte bit0 = (byte)(mmu[register] & 0x1);
        mmu[register] >>= 1;
        mmu[register] |= (byte)(bit0 << 7);

        ZeroFlag = mmu[register] == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = bit0 == 1;

        PC += 2;
        clocksToWait = 16;
    }

    /// <summary>
    /// SLA <paramref name="register"/> | 2  8 | Z 0 0 C
    /// </summary>
    byte SLA_Reg(byte register)
    {
        byte bit7 = (byte)(register >> 7);
        byte result = (byte)(register << 1);

        ZeroFlag = result == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = bit7 == 1;

        PC += 2;
        clocksToWait = 8;

        return result;
    }

    /// <summary>
    /// SLA (<paramref name="register"/>) | 2  16 | Z 0 0 C
    /// </summary>
    void SLA_AddressReg(ushort register)
    {
        byte bit7 = (byte)(mmu[register] >> 7);
        mmu[register] <<= 1;

        ZeroFlag = mmu[register] == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = bit7 == 1;

        PC += 2;
        clocksToWait = 16;
    }

    /// <summary>
    /// SRA <paramref name="register"/> | 2  8 | Z 0 0 C
    /// </summary>
    byte SRA_Reg(byte register)
    {
        byte bit0 = (byte)(register & 0b0000_0001);
        byte bit7 = (byte)(register & 0b1000_0000);
        byte result = (byte)(register >> 1);
        result |= bit7;

        ZeroFlag = result == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = bit0 == 1;

        PC += 2;
        clocksToWait = 8;

        return result;
    }

    /// <summary>
    /// SRA (<paramref name="register"/>) | 2  16 | Z 0 0 C
    /// </summary>
    void SRA_AddressReg(ushort register)
    {
        byte bit0 = (byte)(mmu[register] & 0b0000_0001);
        byte bit7 = (byte)(mmu[register] & 0b1000_0000);
        mmu[register] >>= 1;
        mmu[register] |= bit7;

        ZeroFlag = mmu[register] == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = bit0 == 1;

        PC += 2;
        clocksToWait = 16;
    }

    /// <summary>
    /// SRL <paramref name="register"/> | 2  8 | Z 0 0 C
    /// </summary>
    byte SRL_Reg(byte register)
    {
        byte bit0 = (byte)(register & 0b0000_0001);
        byte result = (byte)(register >> 1);

        ZeroFlag = result == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = bit0 == 1;

        PC += 2;
        clocksToWait = 8;

        return result;
    }

    /// <summary>
    /// SRL (<paramref name="register"/>) | 2  16 | Z 0 0 C
    /// </summary>
    void SRL_AddressReg(ushort register)
    {
        byte bit0 = (byte)(mmu[register] & 0b0000_0001);
        mmu[register] >>= 1;

        ZeroFlag = mmu[register] == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = bit0 == 1;

        PC += 2;
        clocksToWait = 16;
    }

    /// <summary>
    /// SWAP <paramref name="register"/> | 2  8 | Z 0 0 0
    /// </summary>
    byte SWAP_Reg(byte register)
    {
        byte result = (byte)(((register & 0x0F) << 4) | ((register & 0xF0) >> 4));

        ZeroFlag = result == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = false;

        PC += 2;
        clocksToWait = 8;

        return result;
    }

    /// <summary>
    /// SWAP (<paramref name="register"/>) | 2  16 | Z 0 0 0
    /// </summary>
    void SWAP_AddressReg(ushort register)
    {
        mmu[register] = (byte)(((mmu[register] & 0x0F) << 4) | ((mmu[register] & 0xF0) >> 4));

        ZeroFlag = mmu[register] == 0;
        NegationFlag = false;
        HalfCarryFlag = false;
        CarryFlag = false;

        PC += 2;
        clocksToWait = 16;
    }


    #endregion

    #region Unknown Opcodes

    void UnknownOpcode(byte currentOpcode)
    {
        string message = $"Unknown Opcode: 0x{currentOpcode:X2} at PC: 0x{PC:X4}.";
        Utils.Log(LogType.Error, message);
        throw new NotImplementedException(message);
    }

    #endregion

    #region Interrupt Processing

    bool interruptMasterEnableFlag;
    int interruptMasterEnablePendingCycles;

    void ProcessInterrupts()
    {
        if (interruptMasterEnablePendingCycles > 0)
        {
            interruptMasterEnablePendingCycles--;
            if (interruptMasterEnablePendingCycles == 0) interruptMasterEnableFlag = true;
        }

        byte IF = mmu[0xFF0F];
        byte IE = mmu[0xFFFF];

        if (Helpers.GetBit(IF, 0) && Helpers.GetBit(IE, 0))
        {
            isHalted = false;

            if (interruptMasterEnableFlag)
            {
                // Perform VBlank interrupt
                mmu[0xFF0F] = Helpers.SetBit(IF, 0, false);
                interruptMasterEnableFlag = false;
                JumpVector(0x40);

                return;
            }
        }

        if (Helpers.GetBit(IF, 1) && Helpers.GetBit(IE, 1))
        {
            isHalted = false;

            if (interruptMasterEnableFlag)
            {
                // Perform LCD Stat interrupt
                mmu[0xFF0F] = Helpers.SetBit(IF, 1, false);
                interruptMasterEnableFlag = false;
                JumpVector(0x48);

                return;
            }
        }

        if (Helpers.GetBit(IF, 2) && Helpers.GetBit(IE, 2))
        {
            isHalted = false;

            if (interruptMasterEnableFlag)
            {
                // Perform Timer Overflow interrupt
                mmu[0xFF0F] = Helpers.SetBit(IF, 2, false);
                interruptMasterEnableFlag = false;
                JumpVector(0x50);

                return;
            }
        }

        if (Helpers.GetBit(IF, 3) && Helpers.GetBit(IE, 3))
        {
            isHalted = false;

            if (interruptMasterEnableFlag)
            {
                // Perform Serial interrupt
                mmu[0xFF0F] = Helpers.SetBit(IF, 3, false);
                interruptMasterEnableFlag = false;
                JumpVector(0x58);

                return;
            }
        }

        if (Helpers.GetBit(IF, 4) && Helpers.GetBit(IE, 4))
        {
            isHalted = false;

            if (interruptMasterEnableFlag)
            {
                // Perform Joypad interrupt
                mmu[0xFF0F] = Helpers.SetBit(IF, 4, false);
                interruptMasterEnableFlag = false;
                JumpVector(0x60);

                return;
            }
        }
    }

    void JumpVector(byte offset)
    {
        SP -= 2;
        mmu[SP + 1] = (byte)((PC) >> 8);
        mmu[SP + 0] = (byte)((PC) & 0xFF);

        PC = offset;
        clocksToWait = 24; // TODO: Don't know how many clocks. Is it the same as CALL?
    }

    #endregion

    #region State

    public void SaveOrLoadState(Stream stream, BinaryWriter bw, BinaryReader br, bool save)
    {
        A = SaveState.SaveLoadValue(bw, br, save, A);
        B = SaveState.SaveLoadValue(bw, br, save, B);
        C = SaveState.SaveLoadValue(bw, br, save, C);
        D = SaveState.SaveLoadValue(bw, br, save, D);
        E = SaveState.SaveLoadValue(bw, br, save, E);
        F = SaveState.SaveLoadValue(bw, br, save, F);
        H = SaveState.SaveLoadValue(bw, br, save, H);
        L = SaveState.SaveLoadValue(bw, br, save, L);

        SP = SaveState.SaveLoadValue(bw, br, save, SP);
        PC = SaveState.SaveLoadValue(bw, br, save, PC);

        clocksToWait = SaveState.SaveLoadValue(bw, br, save, clocksToWait);
        isHalted = SaveState.SaveLoadValue(bw, br, save, isHalted);

        interruptMasterEnableFlag = SaveState.SaveLoadValue(bw, br, save, interruptMasterEnableFlag);
        interruptMasterEnablePendingCycles = SaveState.SaveLoadValue(bw, br, save, interruptMasterEnablePendingCycles);
    }

    #endregion

    #region TraceLog

#if DEBUG

    public bool IsTraceLogEnabled => traceLogStreamWriter != null;

    StreamWriter traceLogStreamWriter;

    bool bootromFinishedExecuting = false;
    ulong totalCycles = 0;

    public void EnableTraceLog(string fileName)
    {
        if (IsTraceLogEnabled) return;

        traceLogStreamWriter = new StreamWriter(fileName, false, Encoding.UTF8);
    }

    public void DisableTraceLog()
    {
        if (!IsTraceLogEnabled) return;

        traceLogStreamWriter.Flush();
        traceLogStreamWriter.Dispose();
        traceLogStreamWriter = null;
    }

    void ProcessTraceLog()
    {
        if (traceLogStreamWriter == null) return;

        if (!bootromFinishedExecuting)
        {
            if (PC == GameBoy.BOOT_ROM_END_ADDRESS)
            {
                bootromFinishedExecuting = true;
                totalCycles = 0;
            }
            else
            {
                return;
            }
        }

        //bool displayOn = (mmu[0xff40] & 0x80) != 0;
        //int ppuMode = mmu[0xff41] & 0x3;
        //traceLogStreamWriter.WriteLine($"PC:{PC:X4} AF:{AF:X4} BC:{BC:X4} DE:{DE:X4} HL:{HL:X4} SP:{SP:X4} (cy: {totalCycles}) ppu:{(displayOn ? "+" : "-")}{ppuMode} LY:{mmu[0xff44]:X2}");

        traceLogStreamWriter.WriteLine($"PC:{PC:X4} AF:{AF:X4} BC:{BC:X4} DE:{DE:X4} HL:{HL:X4} SP:{SP:X4}");

        //try
        //{
        //    traceLogStreamWriter?.WriteLine($"PC = {PC:X4} ({mmu[PC]:X2}), AF = {AF:X4}, BC = {BC:X4}, DE = {DE:X4}, HL = {HL:X4}, SP = {SP:X4} ({(ushort)((mmu[SP + 1] << 8) | mmu[SP]):X4})");
        //}
        //catch (Exception)
        //{
        //    traceLogStreamWriter?.WriteLine($"PC = {PC:X4} ({mmu[PC]:X2}), AF = {AF:X4}, BC = {BC:X4}, DE = {DE:X4}, HL = {HL:X4}, SP = {SP:X4}");
        //}
    }

#endif

    #endregion
}