# DamageBoy

<p align="center">
  <img title="DamageBoy Icon" src="/Icon.png">
</p>

`DamageBoy` is the name of `Yet Another™` Experimental GameBoy emulator created for learning purposes. The name comes from `DMG`, which is the original codename for the GameBoy, which stands for `Dot Matrix Game`. The emulator is written in C#, it uses OpenGL for rendering, OpenAL for audio and ImGui for UI. Despite being optional, it's recommended to have a boot ROM (BIOS) file named `dmg_boot_rom` and put it in the same path as the DamageBoy executable.

## Features
- It can run games with the most common Memory Bank Controllers: MBC1, MBC2, MBC3, MBC5, HuC1 and HuC-3.
- Most graphics features are supported: background, window, sprites, scrolling and mid-scanline effects.
- Audio mostly implemented, but there could be small audio errors here and there.
- It can be played with keyboard or with an XInput compatible controller.
- It supports saving and loading. Saves are stored in the `Saves` subfolder in the emulator path.
- It also has save state functionality. They are stored in the `SaveStates` subfolder.
- Most games I've tried work perfect or almost perfect. But there are still games that don't work or have severe issues.

## Requirements

It requires having a GPU compatible with OpenGL 3.3.

## Compatibility

[Click here to check game compatibility.](/COMPATIBILITY.md)

## Screenshots

<p align="center">
  <img title="Tetris - Screenshot" src="/00.png">
  <img title="Super Mario Land 2 - Screenshot" src="/01.png">
  <img title="Wario Land - Screenshot" src="/02.png">
  <img title="Zelda Link's Awakening - Screenshot" src="/03.png">
  <img title="Donkey Kong Land - Screenshot" src="/04.png">
  <img title="Kirby's Dreamland - Screenshot" src="/05.png">
  <img title="Metroid II Samus Returns - Screenshot" src="/06.png">
  <img title="Pokemon Red - Screenshot" src="/07.png">
</p>