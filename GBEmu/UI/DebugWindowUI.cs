using ImGuiNET;
using GBEmu.Core;

namespace GBEmu.UI
{
    class DebugWindowUI : BaseUI
    {
        readonly GameBoy chip8;

        //readonly CPUState cpuState;

        public DebugWindowUI(GameBoy chip8)
        {
            this.chip8 = chip8;

            //cpuState = new CPUState();
        }

        protected override void InternalRender()
        {
            //chip8.GetCPUState(cpuState);

            //ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 450));

            //if (!ImGui.Begin("Debug", ref isVisible, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse))
            //{
            //    ImGui.End();
            //    return;
            //}

            //ImGui.Columns(3, "Columns", true);

            //ImGui.Text("CPU State");

            //ImGui.InputInt("PC", ref cpuState.PC, 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("SP", ref cpuState.SP, 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("I", ref cpuState.I, 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("DT", ref cpuState.delayTimer, 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("ST", ref cpuState.soundTimer, 0, 0, ImGuiInputTextFlags.ReadOnly);

            //ImGui.NextColumn();
            //ImGui.Text("Main registers");

            //ImGui.InputInt("V0", ref cpuState.V[0X0], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("V1", ref cpuState.V[0X1], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("V2", ref cpuState.V[0X2], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("V3", ref cpuState.V[0X3], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("V4", ref cpuState.V[0X4], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("V5", ref cpuState.V[0X5], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("V6", ref cpuState.V[0X6], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("V7", ref cpuState.V[0X7], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("V8", ref cpuState.V[0X8], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("V9", ref cpuState.V[0X9], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("VA", ref cpuState.V[0XA], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("VB", ref cpuState.V[0XB], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("VC", ref cpuState.V[0XC], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("VD", ref cpuState.V[0XD], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("VE", ref cpuState.V[0XE], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("VF", ref cpuState.V[0XF], 0, 0, ImGuiInputTextFlags.ReadOnly);

            //ImGui.NextColumn();
            //ImGui.Text("Stack");
            ////ImGui.Separator();

            //ImGui.InputInt("0", ref cpuState.stack[0X0], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("1", ref cpuState.stack[0X1], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("2", ref cpuState.stack[0X2], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("3", ref cpuState.stack[0X3], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("4", ref cpuState.stack[0X4], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("5", ref cpuState.stack[0X5], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("6", ref cpuState.stack[0X6], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("7", ref cpuState.stack[0X7], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("8", ref cpuState.stack[0X8], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("9", ref cpuState.stack[0X9], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("A", ref cpuState.stack[0XA], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("B", ref cpuState.stack[0XB], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("C", ref cpuState.stack[0XC], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("D", ref cpuState.stack[0XD], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("E", ref cpuState.stack[0XE], 0, 0, ImGuiInputTextFlags.ReadOnly);
            //ImGui.InputInt("F", ref cpuState.stack[0XF], 0, 0, ImGuiInputTextFlags.ReadOnly);

            //ImGui.Columns(1);

            //ImGui.End();
        }
    }
}
