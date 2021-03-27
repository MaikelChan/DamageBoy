using DamageBoy.Core.State;
using System;
using System.Runtime.CompilerServices;

namespace DamageBoy.Core
{
    class PPU : IState
    {
        readonly InterruptHandler interruptHandler;
        readonly VRAM vram;
        readonly Action<byte[]> screenUpdateCallback;

        readonly byte[][] lcdPixelBuffers;
        readonly byte[] spriteIndicesInCurrentLine;
        byte spritesAmountInCurrentLine;

        // LCD Control

        bool lcdDisplayEnable;
        public bool LCDDisplayEnable
        {
            get { return lcdDisplayEnable; }
            set
            {
                if (lcdDisplayEnable && !value) ClearScreen();
                lcdDisplayEnable = value;
            }
        }
        public bool WindowTileMapDisplaySelect { get; set; }
        public bool WindowDisplayEnable { get; set; }
        public bool BGAndWindowTileDataSelect { get; set; }
        public bool BGTileMapDisplaySelect { get; set; }
        public bool OBJSize { get; set; }
        public bool OBJDisplayEnable { get; set; }
        public bool BGDisplayEnable { get; set; }

        // LCD Status

        public Modes LCDStatusMode { get; set; }
        public CoincidenceFlagModes LCDStatusCoincidenceFlag { get; set; }
        public bool LCDStatusHorizontalBlankInterrupt { get; set; }
        public bool LCDStatusVerticalBlankInterrupt { get; set; }
        public bool LCDStatusOAMSearchInterrupt { get; set; }
        public bool LCDStatusCoincidenceInterrupt { get; set; }

        // LCD Position and Scrolling

        public byte ScrollY { get; set; }
        public byte ScrollX { get; set; }
        public byte LY { get; set; }
        public byte LYC { get; set; }
        public byte WindowY { get; set; }
        public byte WindowX { get; set; }

        // LCD Monochrome Palettes

        public byte BackgroundPalette { get; set; }
        public byte ObjectPalette0 { get; set; }
        public byte ObjectPalette1 { get; set; }

        // Constants

        const byte BG_TILES_X = 32;
        //const byte BG_TILES_Y = 32;
        //const byte LCD_TILES_X = RES_X >> 3;
        //const byte LCD_TILES_Y = RES_Y >> 3;
        const byte TILE_BYTES_SIZE = 16;

        const byte MAX_SPRITES = 40;
        const byte MAX_SPRITES_PER_LINE = 10;
        const byte OAM_ENTRY_SIZE = 4;
        const byte SPRITE_WIDTH = 8;
        const byte SPRITE_HEIGHT = 8;
        const byte SPRITE_MAX_HEIGHT = 16;

        const int OAM_SEARCH_CLOCKS = 80;
        const int PIXEL_TRANSFER_CLOCKS = 172;
        const int HORIZONTAL_BLANK_CLOCKS = 204;
        const int VERTICAL_BLANK_CLOCKS = OAM_SEARCH_CLOCKS + PIXEL_TRANSFER_CLOCKS + HORIZONTAL_BLANK_CLOCKS;
        const int VERTICAL_BLANK_LINES = 10;
        public const int SCREEN_CLOCKS = VERTICAL_BLANK_CLOCKS * (Constants.RES_Y + VERTICAL_BLANK_LINES);

        const byte COLOR_BLACK = 0;
        const byte COLOR_DARK_GRAY = 85;
        const byte COLOR_LIGHT_GRAY = 170;
        const byte COLOR_WHITE = 255;

        public enum Modes : byte { HorizontalBlank, VerticalBlank, OamSearch, PixelTransfer }
        public enum CoincidenceFlagModes : byte { Different, Equals }

        int clocksToWait;
        int currentBuffer;

        readonly byte[] currentLineColorIndices = new byte[Constants.RES_X];

        public PPU(InterruptHandler interruptHandler, VRAM vram, Action<byte[]> screenUpdateCallback)
        {
            this.interruptHandler = interruptHandler;
            this.vram = vram;
            this.screenUpdateCallback = screenUpdateCallback;

            // Initialize double buffer
            lcdPixelBuffers = new byte[2][];
            lcdPixelBuffers[0] = new byte[Constants.RES_X * Constants.RES_Y];
            lcdPixelBuffers[1] = new byte[Constants.RES_X * Constants.RES_Y];
            currentBuffer = 0;

            spriteIndicesInCurrentLine = new byte[MAX_SPRITES_PER_LINE];

            DoOAMSearch();
        }

        public byte this[int index]
        {
            get
            {
                if (index >= VRAM.VRAM_START_ADDRESS && index < VRAM.VRAM_END_ADDRESS)
                {
                    if (CanCPUAccessVRAM())
                    {
                        return vram.VRam[index - VRAM.VRAM_START_ADDRESS];
                    }
                    else
                    {
                        Utils.Log(LogType.Warning, $"Tried to read from VRAM while in {LCDStatusMode} mode.");
                        return 0xFF;
                    }
                }
                else if (index >= VRAM.OAM_START_ADDRESS && index < VRAM.OAM_END_ADDRESS)
                {
                    if (CanCPUAccessOAM())
                    {
                        return vram.Oam[index - VRAM.OAM_START_ADDRESS];
                    }
                    else
                    {
                        Utils.Log(LogType.Warning, $"Tried to read from OAM while in {LCDStatusMode} mode.");
                        return 0xFF;
                    }
                }
                else
                {
                    throw new IndexOutOfRangeException("Tried to read out of range PPU memory.");
                }
            }

            set
            {
                if (index >= VRAM.VRAM_START_ADDRESS && index < VRAM.VRAM_END_ADDRESS)
                {
                    if (CanCPUAccessVRAM())
                        vram.VRam[index - VRAM.VRAM_START_ADDRESS] = value;
                    else
                        Utils.Log(LogType.Warning, $"Tried to write to VRAM while in {LCDStatusMode} mode.");
                }
                else if (index >= VRAM.OAM_START_ADDRESS && index < VRAM.OAM_END_ADDRESS)
                {
                    if (CanCPUAccessOAM())
                        vram.Oam[index - VRAM.OAM_START_ADDRESS] = value;
                    else
                        Utils.Log(LogType.Warning, $"Tried to write to OAM while in {LCDStatusMode} mode.");
                }
                else
                {
                    throw new IndexOutOfRangeException("Tried to write out of range PPU memory.");
                }
            }
        }

        public void Update()
        {
            if (!LCDDisplayEnable)
            {
                LCDStatusMode = Modes.HorizontalBlank;
                LY = 0;
                return;
            }

            clocksToWait -= 4;
            if (clocksToWait > 0) return;

            switch (LCDStatusMode)
            {
                case Modes.OamSearch:

                    DoPixelTransfer();

                    break;

                case Modes.PixelTransfer:

                    DoHorizontalBlank();

                    break;

                case Modes.HorizontalBlank:

                    LY++;
                    CheckLYC();

                    if (LY >= Constants.RES_Y)
                    {
                        DoVerticalBlank();
                    }
                    else
                    {
                        DoOAMSearch();
                    }

                    break;

                case Modes.VerticalBlank:

                    LY++;

                    if (LY >= Constants.RES_Y + VERTICAL_BLANK_LINES)
                    {
                        screenUpdateCallback?.Invoke(lcdPixelBuffers[currentBuffer]);
                        currentBuffer ^= 1;

                        LY = 0;
                        CheckLYC();
                        DoOAMSearch();
                    }
                    else
                    {
                        CheckLYC();
                        clocksToWait = VERTICAL_BLANK_CLOCKS;
                    }

                    break;
            }
        }

        void DoOAMSearch()
        {
            LCDStatusMode = Modes.OamSearch;
            clocksToWait = OAM_SEARCH_CLOCKS;

            if (LCDStatusOAMSearchInterrupt)
            {
                interruptHandler.RequestLCDCSTAT = true;
            }

            spritesAmountInCurrentLine = 0;

            byte spriteHeight = OBJSize ? SPRITE_MAX_HEIGHT : SPRITE_HEIGHT;

            for (byte s = 0; s < MAX_SPRITES; s++)
            {
                int spriteEntryAddress = s * OAM_ENTRY_SIZE;

                int spriteY = vram.Oam[spriteEntryAddress + 0] - SPRITE_MAX_HEIGHT;
                if (LY >= spriteY + spriteHeight) continue;
                if (LY < spriteY) continue;

                //byte spriteX = vram.Oam[spriteEntryAddress + 1];
                //if (spriteX == 0) continue;

                // TODO: Check more conditions?

                spriteIndicesInCurrentLine[spritesAmountInCurrentLine] = s;
                spritesAmountInCurrentLine++;
                if (spritesAmountInCurrentLine >= MAX_SPRITES_PER_LINE) break;
            }
        }

        void DoPixelTransfer()
        {
            LCDStatusMode = Modes.PixelTransfer;
            clocksToWait = PIXEL_TRANSFER_CLOCKS;

            if (BGDisplayEnable)
            {
                ushort tileMapAddress = (ushort)((BGTileMapDisplaySelect ? 0x9C00 : 0x9800) - VRAM.VRAM_START_ADDRESS);
                ushort tileDataAddress = (ushort)((BGAndWindowTileDataSelect ? 0x8000 : 0x8800) - VRAM.VRAM_START_ADDRESS);

                int sY = (LY + ScrollY) & 0xFF;

                for (int x = 0; x < Constants.RES_X; x++)
                {
                    int sX = (x + ScrollX) & 0xFF;

                    ushort currentTileMapAddress = tileMapAddress;
                    currentTileMapAddress += (ushort)((sY >> 3) * BG_TILES_X + (sX >> 3));

                    byte tile = vram.VRam[currentTileMapAddress];
                    if (!BGAndWindowTileDataSelect) tile = (byte)((tile + 0x80) & 0xFF);

                    ushort currentTileDataAddress = tileDataAddress;
                    currentTileDataAddress += (ushort)(tile * TILE_BYTES_SIZE + ((sY & 0x7) << 1));

                    int currentLCDPixel = LY * Constants.RES_X + (x);

                    byte bit = (byte)(7 - (sX & 0x7));
                    currentLineColorIndices[x] = GetColorIndex(currentTileDataAddress, bit);
                    lcdPixelBuffers[currentBuffer][currentLCDPixel] = GetBGColor(currentLineColorIndices[x]);
                }
            }
            else
            {
                for (int p = 0; p < Constants.RES_X * Constants.RES_Y; p++)
                {
                    lcdPixelBuffers[currentBuffer][p] = 0;
                }
            }

            if (WindowDisplayEnable)
            {
                ushort tileMapAddress = (ushort)((WindowTileMapDisplaySelect ? 0x9C00 : 0x9800) - VRAM.VRAM_START_ADDRESS);
                ushort tileDataAddress = (ushort)((BGAndWindowTileDataSelect ? 0x8000 : 0x8800) - VRAM.VRAM_START_ADDRESS);

                int sY = (LY - WindowY) & 0xFF;

                for (int x = 0; x < Constants.RES_X; x++)
                {
                    if (LY >= WindowY + Constants.RES_Y) break;
                    if (LY < WindowY) break;

                    if (x >= WindowX - 7 + Constants.RES_X) continue;
                    if (x < WindowX - 7) continue;

                    int sX = (x - WindowX + 7) & 0xFF;

                    ushort currentTileMapAddress = tileMapAddress;
                    currentTileMapAddress += (ushort)((sY >> 3) * BG_TILES_X + (sX >> 3));

                    byte tile = vram.VRam[currentTileMapAddress];
                    if (!BGAndWindowTileDataSelect) tile = (byte)((tile + 0x80) & 0xFF);

                    ushort currentTileDataAddress = tileDataAddress;
                    currentTileDataAddress += (ushort)(tile * TILE_BYTES_SIZE + ((sY & 0x7) << 1));

                    int currentLCDPixel = LY * Constants.RES_X + x;

                    byte bit = (byte)(7 - (sX & 0x7));
                    currentLineColorIndices[x] = GetColorIndex(currentTileDataAddress, bit);
                    lcdPixelBuffers[currentBuffer][currentLCDPixel] = GetBGColor(currentLineColorIndices[x]);
                }
            }

            if (OBJDisplayEnable)
            {
                byte spriteHeight = OBJSize ? SPRITE_MAX_HEIGHT : SPRITE_HEIGHT;

                for (byte x = 0; x < Constants.RES_X; x++)
                {
                    for (int s = spritesAmountInCurrentLine - 1; s >= 0; s--)
                    {
                        byte spriteIndex = spriteIndicesInCurrentLine[s];
                        int spriteEntryAddress = spriteIndex * OAM_ENTRY_SIZE;

                        int spriteY = vram.Oam[spriteEntryAddress + 0] - SPRITE_MAX_HEIGHT;

                        int spriteX = vram.Oam[spriteEntryAddress + 1] - SPRITE_WIDTH;
                        if (x >= spriteX + 8) continue;
                        if (x < spriteX) continue;

                        bool spritePalette = Helpers.GetBit(vram.Oam[spriteEntryAddress + 3], 4);
                        bool spriteInvX = Helpers.GetBit(vram.Oam[spriteEntryAddress + 3], 5);
                        bool spriteInvY = Helpers.GetBit(vram.Oam[spriteEntryAddress + 3], 6);
                        bool spritePriority = Helpers.GetBit(vram.Oam[spriteEntryAddress + 3], 7);

                        byte spriteTile = vram.Oam[spriteEntryAddress + 2];
                        int spriteRow = (spriteInvY ? ((spriteHeight - 1) - (LY - spriteY)) : (LY - spriteY)) << 1;
                        ushort tileDataAddress = (ushort)(spriteTile * TILE_BYTES_SIZE + spriteRow);

                        int currentLCDPixel = LY * Constants.RES_X + (x);
                        byte bit = (byte)(spriteInvX ? x - spriteX : (SPRITE_WIDTH - 1) - (x - spriteX));
                        byte colorIndex = GetColorIndex(tileDataAddress, bit);
                        if (colorIndex != 0)
                        {
                            if (spritePriority)
                            {
                                if (currentLineColorIndices[x] == 0) lcdPixelBuffers[currentBuffer][currentLCDPixel] = GetSpriteColor(colorIndex, spritePalette);
                            }
                            else
                            {
                                lcdPixelBuffers[currentBuffer][currentLCDPixel] = GetSpriteColor(colorIndex, spritePalette);
                            }
                        }
                    }
                }
            }
        }

        void DoHorizontalBlank()
        {
            LCDStatusMode = Modes.HorizontalBlank;
            clocksToWait = HORIZONTAL_BLANK_CLOCKS;

            if (LCDStatusHorizontalBlankInterrupt)
            {
                interruptHandler.RequestLCDCSTAT = true;
            }
        }

        void DoVerticalBlank()
        {
            LCDStatusMode = Modes.VerticalBlank;
            interruptHandler.RequestVerticalBlanking = true;
            clocksToWait = VERTICAL_BLANK_CLOCKS;

            if (LCDStatusVerticalBlankInterrupt)
            {
                interruptHandler.RequestLCDCSTAT = true;
            }
        }

        void CheckLYC()
        {
            if (LY == LYC)
            {
                LCDStatusCoincidenceFlag = CoincidenceFlagModes.Equals;

                if (LCDStatusCoincidenceInterrupt)
                {
                    interruptHandler.RequestLCDCSTAT = true;
                }
            }
            else
            {
                LCDStatusCoincidenceFlag = CoincidenceFlagModes.Different;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte GetColorIndex(ushort pixelAddress, byte bit)
        {
            int v1 = (vram.VRam[pixelAddress + 0] & (1 << bit)) != 0 ? 1 : 0;
            int v2 = (vram.VRam[pixelAddress + 1] & (1 << bit)) != 0 ? 1 : 0;
            return (byte)((v2 << 1) | v1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte GetBGColor(byte colorIndex)
        {
            switch (GetBGPaletteColor(colorIndex))
            {
                case 0: return COLOR_WHITE;
                case 1: return COLOR_LIGHT_GRAY;
                case 2: return COLOR_DARK_GRAY;
                case 3: return COLOR_BLACK;
                default: throw new ArgumentException("Not valid BG palette color index: " + colorIndex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte GetSpriteColor(byte colorIndex, bool palette)
        {
            byte color = palette ? GetObjPalette1Color(colorIndex) : GetObjPalette0Color(colorIndex);

            switch (color)
            {
                case 0: return COLOR_WHITE;
                case 1: return COLOR_LIGHT_GRAY;
                case 2: return COLOR_DARK_GRAY;
                case 3: return COLOR_BLACK;
                default: throw new ArgumentException($"Not valid Obj palette {palette} color index: {colorIndex}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte GetBGPaletteColor(byte colorIndex)
        {
            return (byte)((BackgroundPalette >> (colorIndex << 1)) & 0x3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte GetObjPalette0Color(byte colorIndex)
        {
            return (byte)((ObjectPalette0 >> (colorIndex << 1)) & 0x3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte GetObjPalette1Color(byte colorIndex)
        {
            return (byte)((ObjectPalette1 >> (colorIndex << 1)) & 0x3);
        }

        bool CanCPUAccessVRAM()
        {
            if (!LCDDisplayEnable) return true;
            if (LCDStatusMode != Modes.PixelTransfer) return true;
            return false;
        }

        bool CanCPUAccessOAM()
        {
            if (!LCDDisplayEnable) return true;
            if (LCDStatusMode == Modes.HorizontalBlank) return true;
            if (LCDStatusMode == Modes.VerticalBlank) return true;
            return false;
        }

        void ClearScreen()
        {
            for (int p = 0; p < Constants.RES_X * Constants.RES_Y; p++) lcdPixelBuffers[currentBuffer][p] = 255;
            screenUpdateCallback?.Invoke(lcdPixelBuffers[currentBuffer]);
            currentBuffer ^= 1;
        }

        public void GetState(SaveState state)
        {
            state.LCDDisplayEnable = LCDDisplayEnable;
            state.WindowTileMapDisplaySelect = WindowTileMapDisplaySelect;
            state.WindowDisplayEnable = WindowDisplayEnable;
            state.BGAndWindowTileDataSelect = BGAndWindowTileDataSelect;
            state.BGTileMapDisplaySelect = BGTileMapDisplaySelect;
            state.OBJSize = OBJSize;
            state.OBJDisplayEnable = OBJDisplayEnable;
            state.BGDisplayEnable = BGDisplayEnable;

            state.LCDStatusMode = LCDStatusMode;
            state.LCDStatusCoincidenceFlag = LCDStatusCoincidenceFlag;
            state.LCDStatusHorizontalBlankInterrupt = LCDStatusHorizontalBlankInterrupt;
            state.LCDStatusVerticalBlankInterrupt = LCDStatusVerticalBlankInterrupt;
            state.LCDStatusOAMSearchInterrupt = LCDStatusOAMSearchInterrupt;
            state.LCDStatusCoincidenceInterrupt = LCDStatusCoincidenceInterrupt;

            state.ScrollY = ScrollY;
            state.ScrollX = ScrollX;
            state.LY = LY;
            state.LYC = LYC;
            state.WindowY = WindowY;
            state.WindowX = WindowX;

            state.BackgroundPalette = BackgroundPalette;
            state.ObjectPalette0 = ObjectPalette0;
            state.ObjectPalette1 = ObjectPalette1;

            state.PpuClocksToWait = clocksToWait;
        }

        public void SetState(SaveState state)
        {
            LCDDisplayEnable = state.LCDDisplayEnable;
            WindowTileMapDisplaySelect = state.WindowTileMapDisplaySelect;
            WindowDisplayEnable = state.WindowDisplayEnable;
            BGAndWindowTileDataSelect = state.BGAndWindowTileDataSelect;
            BGTileMapDisplaySelect = state.BGTileMapDisplaySelect;
            OBJSize = state.OBJSize;
            OBJDisplayEnable = state.OBJDisplayEnable;
            BGDisplayEnable = state.BGDisplayEnable;

            LCDStatusMode = state.LCDStatusMode;
            LCDStatusCoincidenceFlag = state.LCDStatusCoincidenceFlag;
            LCDStatusHorizontalBlankInterrupt = state.LCDStatusHorizontalBlankInterrupt;
            LCDStatusVerticalBlankInterrupt = state.LCDStatusVerticalBlankInterrupt;
            LCDStatusOAMSearchInterrupt = state.LCDStatusOAMSearchInterrupt;
            LCDStatusCoincidenceInterrupt = state.LCDStatusCoincidenceInterrupt;

            ScrollY = state.ScrollY;
            ScrollX = state.ScrollX;
            LY = state.LY;
            LYC = state.LYC;
            WindowY = state.WindowY;
            WindowX = state.WindowX;

            BackgroundPalette = state.BackgroundPalette;
            ObjectPalette0 = state.ObjectPalette0;
            ObjectPalette1 = state.ObjectPalette1;

            clocksToWait = state.PpuClocksToWait;
        }
    }
}