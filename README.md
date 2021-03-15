# GBEmu
`GBEmu` is a really unoriginal name for `Yet Another™` Experimental GameBoy emulator created as a learning exercise. It is written in C#, it uses OpenGL for rendering, OpenAL for audio and ImGui for UI. It requires a boot ROM (BIOS) file named `gb_boot_rom` and located in the same path as the GBEmu executable.

## Features
- It can run games with no MBC and with MBC1.
- Basic graphics features are supported: background, window, sprites, scrolling and mid-scanline effects.
- It supports saving and loading. Saves are stored in the `Saves` subfolder in the emulator path.
- No audio support for now.
- It's still not too accurate, so there are some timer / interrupt issues. `Super Mario Land` can randomly crash. Though I've been able to play other games like `Super Mario Land 2` and `Link's Awakening` for a while without issues.

## Requirements

It requires [.NET 5.0 Runtime](https://dotnet.microsoft.com/download), [OpenAL 1.1](https://openal.org/downloads/oalinst.zip) and OpenGL 3.3.

## Screenshots

![Screenshot 00](/00.png)
![Screenshot 01](/01.png)
![Screenshot 02](/02.png)
![Screenshot 03](/03.png)