using System;
using System.Runtime.CompilerServices;

namespace GBEmu.Core
{
    class PPU
    {
        readonly IO io;
        readonly VRAM vram;
        readonly Action<byte[]> screenUpdateCallback;

        readonly byte[] lcdPixels;
        readonly byte[] spriteIndicesInCurrentLine;
        byte spritesAmountInCurrentLine;

        public const int RES_X = 160;
        public const int RES_Y = 144;
        const int BG_TILES_X = 32;
        const int BG_TILES_Y = 32;
        const int LCD_TILES_X = RES_X >> 3;
        const int LCD_TILES_Y = RES_Y >> 3;
        const int TILE_BYTES_SIZE = 16;

        const int MAX_SPRITES = 40;
        const int MAX_SPRITES_PER_LINE = 10;
        const int OAM_ENTRY_SIZE = 4;
        const int SPRITE_WIDTH = 8;
        const int SPRITE_HEIGHT = 8;
        const int SPRITE_MAX_HEIGHT = 16;

        const int OAM_SEARCH_CLOCKS = 80;
        const int PIXEL_TRANSFER_CLOCKS = 172;
        const int HORIZONTAL_BLANK_CLOCKS = 204;
        const int VERTICAL_BLANK_CLOCKS = OAM_SEARCH_CLOCKS + PIXEL_TRANSFER_CLOCKS + HORIZONTAL_BLANK_CLOCKS;
        const int VERTICAL_BLANK_LINES = 10;

        const byte COLOR_BLACK = 0;
        const byte COLOR_DARK_GRAY = 85;
        const byte COLOR_LIGHT_GRAY = 170;
        const byte COLOR_WHITE = 255;

        public enum Modes : byte { HorizontalBlank, VerticalBlank, OamSearch, PixelTransfer }
        public enum CoincidenceFlagModes : byte { Different, Equals }

        int clocksToWait;

        public PPU(IO io, VRAM vram, Action<byte[]> screenUpdateCallback)
        {
            this.io = io;
            this.vram = vram;
            this.screenUpdateCallback = screenUpdateCallback;

            lcdPixels = new byte[RES_X * RES_Y];
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
                        Utils.Log(LogType.Warning, $"Tried to read from VRAM while in {io.LCDStatusMode} mode.");
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
                        Utils.Log(LogType.Warning, $"Tried to read from OAM while in {io.LCDStatusMode} mode.");
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
                        Utils.Log(LogType.Warning, $"Tried to write to VRAM while in {io.LCDStatusMode} mode.");
                }
                else if (index >= VRAM.OAM_START_ADDRESS && index < VRAM.OAM_END_ADDRESS)
                {
                    if (CanCPUAccessOAM())
                        vram.Oam[index - VRAM.OAM_START_ADDRESS] = value;
                    else
                        Utils.Log(LogType.Warning, $"Tried to write to OAM while in {io.LCDStatusMode} mode.");
                }
                else
                {
                    throw new IndexOutOfRangeException("Tried to write out of range PPU memory.");
                }
            }
        }

        public void Update()
        {
            if (!io.LCDDisplayEnable) return;

            clocksToWait -= 4;
            if (clocksToWait > 0) return;

            switch (io.LCDStatusMode)
            {
                case Modes.OamSearch:

                    DoPixelTransfer();

                    break;

                case Modes.PixelTransfer:

                    DoHorizontalBlank();

                    break;

                case Modes.HorizontalBlank:

                    io.LY++;
                    CheckLYC();

                    if (io.LY >= RES_Y)
                    {
                        DoVerticalBlank();
                    }
                    else
                    {
                        DoOAMSearch();
                    }

                    break;

                case Modes.VerticalBlank:

                    io.LY++;

                    if (io.LY >= RES_Y + VERTICAL_BLANK_LINES)
                    {
                        screenUpdateCallback?.Invoke(lcdPixels);
                        io.LY = 0;
                        CheckLYC();
                        DoOAMSearch();
                    }
                    else
                    {
                        CheckLYC();
                        io.LCDStatusMode = Modes.VerticalBlank;
                        clocksToWait = VERTICAL_BLANK_CLOCKS;
                    }

                    break;
            }
        }

        void DoOAMSearch()
        {
            io.LCDStatusMode = Modes.OamSearch;
            clocksToWait = OAM_SEARCH_CLOCKS;

            if (io.LCDStatusOAMSearchInterrupt)
            {
                io.InterruptRequestLCDCSTAT = true;
            }

            spritesAmountInCurrentLine = 0;

            byte spriteHeight = io.OBJSize ? SPRITE_MAX_HEIGHT : SPRITE_HEIGHT;

            for (byte s = 0; s < MAX_SPRITES; s++)
            {
                int spriteEntryAddress = s * OAM_ENTRY_SIZE;

                int spriteY = vram.Oam[spriteEntryAddress + 0] - SPRITE_MAX_HEIGHT;
                if (io.LY >= spriteY + spriteHeight) continue;
                if (io.LY < spriteY) continue;

                //byte spriteX = vram.Oam[spriteEntryAddress + 1];
                //if (spriteX == 0) continue;

                // TODO: Check more conditions?

                //if (io.LY >= 0x88)
                //{
                //    int a = 0;
                //}

                spriteIndicesInCurrentLine[spritesAmountInCurrentLine] = s;
                spritesAmountInCurrentLine++;
                if (spritesAmountInCurrentLine >= MAX_SPRITES_PER_LINE) break;
            }
        }

        readonly byte[] currentLineColorIndices = new byte[RES_X];

        void DoPixelTransfer()
        {
            io.LCDStatusMode = Modes.PixelTransfer;
            clocksToWait = PIXEL_TRANSFER_CLOCKS;

            if (io.BGDisplayEnable)
            {
                ushort tileMapAddress = (ushort)((io.BGTileMapDisplaySelect ? 0x9C00 : 0x9800) - VRAM.VRAM_START_ADDRESS);
                ushort tileDataAddress = (ushort)((io.BGAndWindowTileDataSelect ? 0x8000 : 0x8800) - VRAM.VRAM_START_ADDRESS);

                int sY = (io.LY + io.SCY) & 0xFF;

                for (int x = 0; x < RES_X; x++)
                {
                    int sX = (x + io.SCX) & 0xFF;

                    ushort currentTileMapAddress = tileMapAddress;
                    currentTileMapAddress += (ushort)((sY >> 3) * BG_TILES_X + (sX >> 3));

                    byte tile = vram.VRam[currentTileMapAddress];
                    if (!io.BGAndWindowTileDataSelect) tile = (byte)((tile + 0x80) & 0xFF);

                    ushort currentTileDataAddress = tileDataAddress;
                    currentTileDataAddress += (ushort)(tile * TILE_BYTES_SIZE + ((sY & 0x7) << 1));

                    int currentLCDPixel = io.LY * RES_X + (x);

                    byte bit = (byte)(7 - (sX & 0x7));
                    currentLineColorIndices[x] = GetColorIndex(currentTileDataAddress, bit);
                    lcdPixels[currentLCDPixel] = GetBGColor(currentLineColorIndices[x]);
                }
            }
            else
            {
                for (int p = 0; p < RES_X * RES_Y; p++)
                {
                    lcdPixels[p] = 0;
                }
            }

            if (io.WindowDisplayEnable)
            {
                ushort tileMapAddress = (ushort)((io.WindowTileMapDisplaySelect ? 0x9C00 : 0x9800) - VRAM.VRAM_START_ADDRESS);
                ushort tileDataAddress = (ushort)((io.BGAndWindowTileDataSelect ? 0x8000 : 0x8800) - VRAM.VRAM_START_ADDRESS);

                int sY = (io.LY - io.WY) & 0xFF;

                for (int x = 0; x < RES_X; x++)
                {
                    if (io.LY >= io.WY + RES_Y) break;
                    if (io.LY < io.WY) break;

                    if (x >= io.WX - 7 + RES_X) continue;
                    if (x < io.WX - 7) continue;

                    int sX = (x - io.WX + 7) & 0xFF;

                    ushort currentTileMapAddress = tileMapAddress;
                    currentTileMapAddress += (ushort)((sY >> 3) * BG_TILES_X + (sX >> 3));

                    byte tile = vram.VRam[currentTileMapAddress];
                    if (!io.BGAndWindowTileDataSelect) tile = (byte)((tile + 0x80) & 0xFF);

                    ushort currentTileDataAddress = tileDataAddress;
                    currentTileDataAddress += (ushort)(tile * TILE_BYTES_SIZE + ((sY & 0x7) << 1));

                    int currentLCDPixel = io.LY * RES_X + x;

                    byte bit = (byte)(7 - (sX & 0x7));
                    currentLineColorIndices[x] = GetColorIndex(currentTileDataAddress, bit);
                    lcdPixels[currentLCDPixel] = GetBGColor(currentLineColorIndices[x]);
                }
            }

            if (io.OBJDisplayEnable)
            {
                byte spriteHeight = io.OBJSize ? SPRITE_MAX_HEIGHT : SPRITE_HEIGHT;

                for (byte x = 0; x < RES_X; x++)
                {
                    for (int s = spritesAmountInCurrentLine - 1; s >= 0; s--)
                    {
                        byte spriteIndex = spriteIndicesInCurrentLine[s];
                        int spriteEntryAddress = spriteIndex * OAM_ENTRY_SIZE;

                        int spriteY = vram.Oam[spriteEntryAddress + 0] - SPRITE_MAX_HEIGHT;

                        int spriteX = vram.Oam[spriteEntryAddress + 1] - SPRITE_WIDTH;
                        if (x >= spriteX + 8) continue;
                        if (x < spriteX) continue;

                        //if (x >= RES_X)
                        //{
                        //    int a = 0;
                        //}

                        bool spritePalette = Helpers.GetBit(vram.Oam[spriteEntryAddress + 3], 4);
                        bool spriteInvX = Helpers.GetBit(vram.Oam[spriteEntryAddress + 3], 5);
                        bool spriteInvY = Helpers.GetBit(vram.Oam[spriteEntryAddress + 3], 6);
                        bool spritePriority = Helpers.GetBit(vram.Oam[spriteEntryAddress + 3], 7);

                        byte spriteTile = vram.Oam[spriteEntryAddress + 2];
                        int spriteRow = (spriteInvY ? ((spriteHeight - 1) - (io.LY - spriteY)) : (io.LY - spriteY)) << 1;
                        ushort tileDataAddress = (ushort)(spriteTile * TILE_BYTES_SIZE + spriteRow);

                        int currentLCDPixel = io.LY * RES_X + (x);
                        byte bit = (byte)(spriteInvX ? x - spriteX : (SPRITE_WIDTH - 1) - (x - spriteX));
                        byte colorIndex = GetColorIndex(tileDataAddress, bit);
                        if (colorIndex != 0)
                        {
                            if (spritePriority)
                            {
                                if (currentLineColorIndices[x] == 0) lcdPixels[currentLCDPixel] = GetSpriteColor(colorIndex, spritePalette);
                            }
                            else
                            {
                                lcdPixels[currentLCDPixel] = GetSpriteColor(colorIndex, spritePalette);
                            }
                        }
                    }
                }
            }
        }

        void DoHorizontalBlank()
        {
            io.LCDStatusMode = Modes.HorizontalBlank;
            clocksToWait = HORIZONTAL_BLANK_CLOCKS;

            if (io.LCDStatusHorizontalBlankInterrupt)
            {
                io.InterruptRequestLCDCSTAT = true;
            }
        }

        void DoVerticalBlank()
        {
            io.LCDStatusMode = Modes.VerticalBlank;
            io.InterruptRequestVerticalBlanking = true;
            clocksToWait = VERTICAL_BLANK_CLOCKS;

            if (io.LCDStatusVerticalBlankInterrupt)
            {
                io.InterruptRequestLCDCSTAT = true;
            }
        }

        void CheckLYC()
        {
            if (io.LY == io.LYC)
            {
                io.LCDStatusCoincidenceFlag = CoincidenceFlagModes.Equals;

                if (io.LCDStatusCoincidenceInterrupt)
                {
                    io.InterruptRequestLCDCSTAT = true;
                }
            }
            else
            {
                io.LCDStatusCoincidenceFlag = CoincidenceFlagModes.Different;
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
            switch (io.GetBGPaletteColor(colorIndex))
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
            byte color = palette ? io.GetObjPalette1Color(colorIndex) : io.GetObjPalette0Color(colorIndex);

            switch (color)
            {
                case 0: return COLOR_WHITE;
                case 1: return COLOR_LIGHT_GRAY;
                case 2: return COLOR_DARK_GRAY;
                case 3: return COLOR_BLACK;
                default: throw new ArgumentException($"Not valid Obj palette {palette} color index: {colorIndex}");
            }
        }

        bool CanCPUAccessVRAM()
        {
            if (!io.LCDDisplayEnable) return true;
            if (io.LCDStatusMode != Modes.PixelTransfer) return true;
            return false;
        }

        bool CanCPUAccessOAM()
        {
            if (!io.LCDDisplayEnable) return true;
            if (io.LCDStatusMode == Modes.HorizontalBlank) return true;
            if (io.LCDStatusMode == Modes.VerticalBlank) return true;
            return false;
        }
    }
}