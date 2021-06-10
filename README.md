# DamageBoy
`DamageBoy` is the name of `Yet Another™` Experimental GameBoy emulator created for learning purposes. It comes from `DMG`, which is the original codename for the GameBoy, which stands for `Dot Matrix Game`. The emulator is written in C#, it uses OpenGL for rendering, OpenAL for audio and ImGui for UI. Despite being optional, it's recommended to have a boot ROM (BIOS) file named `dmg_boot_rom` and put it in the same path as the DamageBoy executable.

## Features
- It can run games with no MBC, MBC1, MBC2, MBC3 and MBC5.
- Most graphics features are supported: background, window, sprites, scrolling and mid-scanline effects.
- Audio mostly implemented, but there could be small audio errors here and there.
- It supports saving and loading. Saves are stored in the `Saves` subfolder in the emulator path.
- It also has save state functionality. They are stored in the `SaveStates`subfolder.
- Most games I've tried work perfect or almost perfect. But there are still games that don't work or have severe issues.

## Requirements

It requires [.NET 5.0 Runtime](https://dotnet.microsoft.com/download), [OpenAL 1.1](https://openal.org/downloads/oalinst.zip) and OpenGL 3.3.

## Compatibility

[Click here to check game compatibility.](/COMPATIBILITY.md)

## Screenshots

![Tetris - Screenshot](/00.png)
![Super Mario Land 2 - Screenshot](/01.png)
![Wario Land - Screenshot](/02.png)
![Zelda Link's Awakening - Screenshot](/03.png)
![Donkey Kong Land - Screenshot](/04.png)
![Pokemon Red - Screenshot](/05.png)
![Kirby's Dreamland - Screenshot](/06.png)
![Metroid II Samus Returns - Screenshot](/06.png)