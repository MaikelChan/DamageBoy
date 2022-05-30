# DamageBoy Compatibility

Here you can check game compatibility. Games not listed here have not been tested. Please note that the ones I've tested, have not been thoroughly played from beginning to end. So working games could still have unknown breaking issues.

- :heavy_check_mark: Works fine
- :large_blue_diamond: Works, but has some minor issues
- :warning: Game boots, but has major issues
- :x: Doesn't work

## Licensed Games

| ROM Name | Status |
| -- | -- |
| 3 Choume no Tama - Tama and Friends - 3 Choume Obake Panic!! (J) | :heavy_check_mark: |
| 3-pun Yosou - Umaban Club (J) | :heavy_check_mark: |
| 4-in-1 Fun Pak (JUE) | :heavy_check_mark: |
| 4-in-1 Fun Pak Volume II (UE) | :heavy_check_mark: |
| A-mazing Tater (U) | :heavy_check_mark: |
| Aa Harimanada (J) | :heavy_check_mark: |
| Addams Family, The (JUE) | :heavy_check_mark: |
| Addams Family, The - Pugsley's Scavenger Hunt (UE) | :heavy_check_mark: |
| Adventure Island (UE) | :heavy_check_mark: |
| Adventure Island II - Aliens in Paradise (UE) | :heavy_check_mark: |
| Adventures of Lolo (E) | :large_blue_diamond: (Black screen during transitions) |
| Adventures of Rocky and Bullwinkle and Friends, The (U) | :heavy_check_mark: |
| Adventures of Star Saver, The (UE) | :heavy_check_mark: |
| Aerostar (JUE) | :heavy_check_mark: |
| After Burst (J) | :heavy_check_mark: |
| Agro Soar (Australia) | :heavy_check_mark: |
| Akazukin Chacha (J) | :heavy_check_mark: |
| Akumajou Dracula - Shikkoku Taru Zensoukyoku - Dark Night Prelude (J) | :heavy_check_mark: |
| Akumajou Special - Boku Dracula-kun (J) | :heavy_check_mark: |
| Aladdin (UE) | :heavy_check_mark: |
| Alfred Chicken (JUE) | :heavy_check_mark: |
| Alien 3 (JUE) | :heavy_check_mark: |
| Alien Olympics (E) | :heavy_check_mark: |
| Alien vs Predator - The Last of His Clan (JU) | :heavy_check_mark: |
| Alleyway (World) | :heavy_check_mark: |
| All-Star Baseball 99 (U) | :heavy_check_mark: |
| Donkey Kong Land (U) [S][!] | :heavy_check_mark: |
| Donkey Kong Land 2 | :x: (Crashes after title screen) |
| Donkey Kong Land 3 | :x: (Crashes after title screen) |
| Dragon's Lair - The Legend | :heavy_check_mark: |
| F-1 Race (W) (V1.1) [!] | :heavy_check_mark: |
| Ferrari - Grand Prix Challenge (U) [!] | :heavy_check_mark: |
| Ganso!! Yancha Maru | :heavy_check_mark: |
| Gojira-kun | :heavy_check_mark: |
| Indien dans la Ville, Un (F) | :large_blue_diamond: (Music glitches) |
| Initial D Gaiden (J) | :warning: (The game freezes and has visual glitches when selecting the first game option. It shouldn't be selectable?)
| Kirby's Dream Land (UE) [!] | :heavy_check_mark: |
| Kirby's Dream Land 2 (U) [S][!] | :heavy_check_mark: |
| Lamborghini American Challenge (U) [!] | :large_blue_diamond: (Some music sounds bad) |
| Legend of Zelda, The - Link's Awakening (U) (V1.2) [!] | :heavy_check_mark: |
| Little Mermaid, The (E) | :heavy_check_mark: |
| Mario & Yoshi (E) [!] | :heavy_check_mark: |
| Metroid II - Return of Samus (W) [!] | :heavy_check_mark: |
| Pokemon - Red Version (UE) [S][!] | :heavy_check_mark: |
| Pokemon - Yellow Version (UE) [C][!] | :heavy_check_mark: |
| Prehistorik Man (UE) | :warning: (Audio and visual glitches) |
| Road Rash (UE) | :x: (Game breaks because it depends on DMG hardware bug. It doesn't even run on a real GBC.) |
| Spy vs Spy - Operation Boobytrap | :heavy_check_mark: |
| Sunsoft Grand Prix | :heavy_check_mark: |
| Super Chinese Land | :heavy_check_mark: |
| Super Chinese Land 1,2,3 | :warning: (Doesn't load the correct game or goes back to title screen) |
| Super James Pond | :heavy_check_mark: |
| Super Mario Land (W) (V1.1) [!] | :heavy_check_mark: |
| Super Mario Land 2 - 6 Golden Coins (UE) (V1.0) [!] | :heavy_check_mark: |
| Super Street Fighter II (JUE) | :warning: (Visual glitches inside stages) |
| Tetris (W) (V1.1) [!] | :heavy_check_mark: |
| V-Rally Championship Edition | :heavy_check_mark: |
| Vattle Giuce | :heavy_check_mark: |
| Wario Land - Super Mario Land 3 (W) [!] | :heavy_check_mark: |
| Wario Land II (UE) [S][!] | :heavy_check_mark: |
| Zerd no Densetsu (J) | :x: (Game breaks because it depends on DMG hardware bug. It doesn't even run on a real GBC.) |

## Unlicensed Games

| ROM Name | Status |
| -- | -- |
| Super Mario 4 (Unl) [p1][h1C] | :heavy_check_mark: |

## Test ROMs

Here's some test ROMs made for testing features and issues of the hardware. Useful for emulator developers. The table shows which tests pass or fail.

- :heavy_check_mark: Passes the test
- :x: Fails the test

### Blargg's tests

| Test Name | Status |
| -- | -- |
| cpu_instrs/cpu_instrs.gb | :heavy_check_mark: |
| dmg_sound/rom_singles/01-registers.gb | :heavy_check_mark: |
| dmg_sound/rom_singles/02-len_ctr.gb | :heavy_check_mark: |
| dmg_sound/rom_singles/03-trigger.gb | :x: |
| dmg_sound/rom_singles/04-sweep.gb | :x: |
| dmg_sound/rom_singles/05-sweep_details.gb | :x: |
| dmg_sound/rom_singles/06-overflow_on_trigger.gb | :x: |
| dmg_sound/rom_singles/07-len_sweep_period_sync.gb | :x: |
| dmg_sound/rom_singles/08-len_ctr_during_power.gb | :x: |
| dmg_sound/rom_singles/09-wave_read_while_on.gb | :x: |
| dmg_sound/rom_singles/10-wave_trigger_while_on.gb | :x: |
| dmg_sound/rom_singles/11-regs_after_power.gb | :x: |
| dmg_sound/rom_singles/12-wave_write_while_on.gb | :x: |
| instr_timing/instr_timing.gb | :heavy_check_mark: |
| interrupt_time/interrupt_time.gb | :x: |
| halt_bug.gb | :x: |
| mem_timing/mem_timing.gb | :x: |
| mem_timing-2/mem_timing.gb | :x: |
| oam_bug/rom_singles/1-lcd_sync.gb | :x: |
| oam_bug/rom_singles/2-causes.gb | :x: |
| oam_bug/rom_singles/3-non_causes.gb | :heavy_check_mark: |
| oam_bug/rom_singles/4-scanline_timing.gb | :x: |
| oam_bug/rom_singles/5-timing_bug.gb | :x: |
| oam_bug/rom_singles/6-timing_no_bug.gb | :heavy_check_mark: |
| oam_bug/rom_singles/7-timing_effect.gb | :x: |
| oam_bug/rom_singles/8-instr_effect.gb | :x: |

### Mooneye's Hardware Tests

| Test Name | Status |
| -- | -- |
| acceptance/add_sp_e_timing.gb | :x: |
| acceptance/boot_div2-S.gb | :x: |
| acceptance/boot_div-dmg0.gb | :x: |
| acceptance/boot_div-dmgABCmgb.gb | :x: |
| acceptance/boot_div-S.gb | :x: |
| acceptance/boot_hwio-dmg0.gb | :x: |
| acceptance/boot_hwio-dmgABCmgb.gb | :x: |
| acceptance/boot_hwio-S.gb | :x: |
| acceptance/boot_regs-dmg0.gb | :x: |
| acceptance/boot_regs-dmgABC.gb | :heavy_check_mark: |
| acceptance/boot_regs-mgb.gb | :x: |
| acceptance/boot_regs-sgb.gb | :x: |
| acceptance/boot_regs-sgb2.gb | :x: |
| acceptance/call_cc_timing.gb | :x: (Doesn't even finish) |
| acceptance/call_cc_timing2.gb | :x: |
| acceptance/call_timing.gb | :x: (Doesn't even finish) |
| acceptance/call_timing2.gb | :x: |
| acceptance/di_timing-GS.gb | :heavy_check_mark: |
| acceptance/div_timing.gb | :x: |
| acceptance/ei_sequence.gb | :x: |
| acceptance/ei_timing.gb | :heavy_check_mark: |
| acceptance/halt_ime0_ei.gb | :heavy_check_mark: |
| acceptance/halt_ime0_nointr_timing.gb | :x: |
| acceptance/halt_ime1_timing.gb | :heavy_check_mark: |
| acceptance/halt_ime1_timing2-GS.gb | :x: |
| acceptance/if_ie_registers.gb | :heavy_check_mark: |
| acceptance/intr_timing.gb | :x: |
| acceptance/jp_cc_timing.gb | :x: (Doesn't even finish) |
| acceptance/jp_timing.gb | :x: (Doesn't even finish) |
| acceptance/ld_hl_sp_e_timing.gb | :x: (Crashes) |
| acceptance/oam_dma_restart.gb | :heavy_check_mark: |
| acceptance/oam_dma_start.gb | :x: |
| acceptance/oam_dma_timing.gb | :heavy_check_mark: |
| acceptance/pop_timing.gb | :x: |
| acceptance/push_timing.gb | :x: |
| acceptance/rapid_di_ei.gb | :x: |
| acceptance/ret_cc_timing.gb | :x: (Crashes) |
| acceptance/ret_timing.gb | :x: (Crashes) |
| acceptance/reti_intr_timing.gb | :x: |
| acceptance/reti_timing.gb | :x: (Crashes) |
| acceptance/rst_timing.gb | :x: |
| acceptance/bits/mem_oam.gb | :heavy_check_mark: |
| acceptance/bits/reg_f.gb | :heavy_check_mark: |
| acceptance/bits/unused_hwio-GS.gb | :heavy_check_mark: |
| acceptance/instr/daa.gb | :heavy_check_mark: |
| acceptance/interrupts/ie_push.gb | :x: |
| acceptance/oam_dma/basic.gb | :heavy_check_mark: |
| acceptance/oam_dma/reg_read.gb | :heavy_check_mark: |
| acceptance/oam_dma/sources-GS.gb | :heavy_check_mark: |
| acceptance/ppu/hblank_ly_scx_timing-GS.gb | :x: |
| acceptance/ppu/intr_1_2_timing-GS-GS.gb | :x: |
| acceptance/ppu/intr_2_0_timing.gb | :x: |
| acceptance/ppu/intr_2_mode0_timing.gb | :heavy_check_mark: |
| acceptance/ppu/intr_2_mode0_timing_sprites.gb | :x: |
| acceptance/ppu/intr_2_mode3_timing.gb | :heavy_check_mark: |
| acceptance/ppu/intr_2_oam_ok_timing.gb | :heavy_check_mark: |
| acceptance/ppu/lcdon_timing-GS.gb | :x: |
| acceptance/ppu/lcdon_write_timing-GS.gb | :x: |
| acceptance/ppu/stat_irq_blocking.gb | :x: |
| acceptance/ppu/stat_lyc_onoff.gb | :x: |
| acceptance/ppu/vblank_stat_intr-GS.gb | :x: |
| acceptance/serial/boot_sclk_align-dmgABCmgb.gb | :x: |
| acceptance/timer/div_write.gb | :x: |
| acceptance/timer/rapid_toggle.gb | :x: |
| acceptance/timer/tim00.gb | :x: |
| acceptance/timer/tim00_div_trigger.gb | :heavy_check_mark: |
| acceptance/timer/tim01.gb | :x: |
| acceptance/timer/tim01_div_trigger.gb | :x: |
| acceptance/timer/tim10.gb | :x: |
| acceptance/timer/tim10_div_trigger.gb | :x: |
| acceptance/timer/tim11.gb | :x: |
| acceptance/timer/tim11_div_trigger.gb | :heavy_check_mark: |
| acceptance/timer/tima_reload.gb | :x: |
| acceptance/timer/tima_write_reloading.gb | :x: |
| acceptance/timer/tma_write_reloading.gb | :x: |
| emulator-only/mbc1/bits_bank1.gb | :heavy_check_mark: |
| emulator-only/mbc1/bits_bank2.gb | :heavy_check_mark: |
| emulator-only/mbc1/bits_mode.gb | :heavy_check_mark: |
| emulator-only/mbc1/bits_ramg.gb | :heavy_check_mark: |
| emulator-only/mbc1/multicart_rom_8Mb.gb | :x: |
| emulator-only/mbc1/ram_64kb.gb | :heavy_check_mark: |
| emulator-only/mbc1/ram_256kb.gb | :heavy_check_mark: |
| emulator-only/mbc1/rom_1Mb.gb | :heavy_check_mark: |
| emulator-only/mbc1/rom_2Mb.gb | :heavy_check_mark: |
| emulator-only/mbc1/rom_4Mb.gb | :heavy_check_mark: |
| emulator-only/mbc1/rom_8Mb.gb | :heavy_check_mark: |
| emulator-only/mbc1/rom_16Mb.gb | :heavy_check_mark: |
| emulator-only/mbc1/rom_512kb.gb | :heavy_check_mark: |
| emulator-only/mbc2/bits_ramg.gb | :heavy_check_mark: |
| emulator-only/mbc2/bits_romb.gb | :heavy_check_mark: |
| emulator-only/mbc2/bits_unused.gb | :heavy_check_mark: |
| emulator-only/mbc2/ram.gb | :heavy_check_mark: |
| emulator-only/mbc2/rom_1Mb.gb | :heavy_check_mark: |
| emulator-only/mbc2/rom_2Mb.gb | :heavy_check_mark: |
| emulator-only/mbc2/rom_512kb.gb | :heavy_check_mark: |
| emulator-only/mbc5/rom_1Mb.gb | :heavy_check_mark: |
| emulator-only/mbc5/rom_2Mb.gb | :heavy_check_mark: |
| emulator-only/mbc5/rom_4Mb.gb | :heavy_check_mark: |
| emulator-only/mbc5/rom_8Mb.gb | :heavy_check_mark: |
| emulator-only/mbc5/rom_16Mb.gb | :heavy_check_mark: |
| emulator-only/mbc5/rom_32Mb.gb | :heavy_check_mark: |
| emulator-only/mbc5/rom_64Mb.gb | :heavy_check_mark: |
| emulator-only/mbc5/rom_512kb.gb | :heavy_check_mark: |
| manual-only/sprite_priority.gb | :x: |
| misc/boot_div-A.gb | :x: |
| misc/boot_div-cgb0.gb | :x: |
| misc/boot_div-cgbABCDE.gb | :x: |
| misc/boot_hwio-C.gb | :x: |
| misc/boot_regs-A.gb | :x: |
| misc/boot_regs-cgb.gb | :x: |
| misc/bits/unused_hwio-C.gb | :x: |
| misc/ppu/vblank_stat_intr-C.gb | :x: |