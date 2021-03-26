using OpenTK.Mathematics;

namespace DamageBoy.UI
{
    public class ImGuiInputData
    {
        public bool LeftMouseButtonDown { get; set; }
        public bool RightMouseButtonDown { get; set; }
        public bool MiddleMouseButtonDown { get; set; }
        public Vector2i MousePosition { get; set; }

        public bool KeyTab { get; set; }
        public bool KeyLeftArrow { get; set; }
        public bool KeyRightArrow { get; set; }
        public bool KeyUpArrow { get; set; }
        public bool KeyDownArrow { get; set; }
        public bool KeyPageUp { get; set; }
        public bool KeyPageDown { get; set; }
        public bool KeyHome { get; set; }
        public bool KeyEnd { get; set; }
        public bool KeyInsert { get; set; }
        public bool KeyDelete { get; set; }
        public bool KeyBackspace { get; set; }
        public bool KeySpace { get; set; }
        public bool KeyEnter { get; set; }
        public bool KeyEscape { get; set; }
        public bool KeyKeyPadEnter { get; set; }
        public bool KeyA { get; set; }
        public bool KeyC { get; set; }
        public bool KeyV { get; set; }
        public bool KeyX { get; set; }
        public bool KeyY { get; set; }
        public bool KeyZ { get; set; }

        public bool KeyCtrl { get; set; }
        public bool KeyAlt { get; set; }
        public bool KeyShift { get; set; }
        public bool KeySuper { get; set; }
    }
}