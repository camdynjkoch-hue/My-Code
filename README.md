# Quicksilver Sandevistan (BONELAB)

A Cyberpunk: Edgerunners–style **Sandevistan** for BONELAB that **replaces the built-in slow-mo ability**. Press the slow-mo button and everything in the world slows down, but **you don't**. You keep walking, running, jumping, swinging and shooting at full speed. On top of that you get the **SandevistanFX** look: a green world grade, an activation flash, and rainbow afterimages.

It combines three things into one DLL:

- **Lakatrazz's Quicksilver** (v1.7.0) keeps the player at normal speed while time is slowed.
- **SandevistanFX** (v1.4.0) adds the visual effects. It was rebuilt from the release DLL.
- **A slow-mo replacement**: BONELAB's slow-mo button toggles the Sandevistan instead of the game's stepped slow-motion.

## Features

### Slow-mo replacement
- **The slow-mo button toggles the Sandevistan.** Press it once to slow the world to 0.25x (you can change this). Press it again to return to normal speed.
- The game's own slow-mo steps are skipped completely, so there's one slow-motion level that you choose.
- Turn **Replace Slow-Mo Button** off to get BONELAB's normal slow-mo back. Quicksilver and the effects still apply to it.

### Quicksilver: the player isn't slowed
- Walking, running, crouching, jumping and turning all stay at normal speed
- Hands and fingers move at normal speed, with extra strength to match
- Guns fire at normal speed, and force grab works at normal speed
- Your own sound effects play at normal pitch

### SandevistanFX effects
- **Green world grade** with an optional neon glow. It fades in and out in real time.
- **Activation flash** when the Sandevistan kicks in.
- **You keep your normal colors.** Your body, held weapons and holstered gear are drawn over the grade, so they aren't tinted. Shared materials are never modified, and everything is restored afterward.
- **Rainbow afterimages** of your avatar and held or holstered items, left behind as you move. Each one keeps one color from the rainbow cycle, stays for its lifetime (3 s by default), then disappears instantly.
- Afterimage heads hide while your eyes are inside them, so they never block your view.
- All effect timing uses real time, so the effects run at full speed while the world is slowed.

## Controls

| Action | Input |
| --- | --- |
| Toggle Sandevistan | **BONELAB's slow-mo button** |
| Toggle Sandevistan | BoneMenu → *Quicksilver Sandevistan* → **Toggle Sandevistan** |
| Preview the effects without slowing time | BoneMenu → *Quicksilver Sandevistan* → *Sandevistan FX* → **Preview (force on)** |
| Toggle Quicksilver movement | BoneMenu → *Quicksilver Sandevistan* → **Quicksilver Movement** |

## Settings

Everything is in BoneMenu → *Quicksilver Sandevistan*, and saved in `UserData/MelonPreferences.cfg`.

**`[Sandevistan]`: slow-motion**

| Setting | Default | Description |
| --- | --- | --- |
| Replace Slow-Mo | on | The slow-mo button toggles the Sandevistan. Off = BONELAB's normal slow-mo. |
| Time Scale | 0.25 | World speed while active (0.05 to 0.95). You stay at full speed. |

**`[SandevistanFX]`: effects.** These are the same keys SandevistanFX used, so an existing SandevistanFX config carries over.

| Setting | Default | Description |
| --- | --- | --- |
| Enabled | on | Master switch for the effects |
| TriggerOnAnyTimeSlowdown | off | Also show the effects for slow-motion from other mods or Fusion |
| UseFallbackShader | off | Troubleshooting: turn on if the grade or afterimages never show up |
| GradeColor | `#3DFF73` | Hex color of the world grade |
| GradeStrength | 0.7 | How strongly the world is pushed toward the grade color |
| GlowStrength | 0.1 | Extra neon light on top of the grade |
| ActivationFlash | 0.5 | Brightness of the flash on activation |
| FadeInTime / FadeOutTime | 0.12 / 0.15 s | Grade fade times |
| KeepPlayerColors | on | Keep you, your held items and your holstered gear in their normal colors |
| AfterimagesEnabled | on | Rainbow afterimages |
| AfterimageInterval | 0.06 s | Time between afterimages |
| AfterimageLifetime | 3 s | How long each afterimage stays |
| AfterimageOpacity | 0.5 | Afterimage opacity |
| AfterimageHueSpeed | 0.2 | Rainbow cycles per second |
| AfterimageSaturation | 0.85 | Afterimage color saturation |
| AfterimageMinMove | 0.02 m | How far your head or hands must move before a new afterimage spawns |
| AfterimageMaxCount | 50 | Most afterimages alive at once (max 100). The oldest is removed first. |
| AfterimageIncludeItems | on | Include held and holstered items |
| AfterimageAdditive | off | Glowing additive afterimages instead of solid see-through ones |
| AfterimageHeadHideRadius | 0.45 m | Hide afterimage heads within this distance of your eyes |
| AfterimageRevealDelay / AfterimageRevealDistance | 0 | Delay showing new afterimages (by time or by distance moved) |

## Installing

1. Install [MelonLoader 0.6.x](https://melonwiki.xyz) and [BoneLib 3.1.x](https://bonelab.thunderstore.io/package/gnonme/BoneLib/).
2. **Remove `Quicksilver.dll` and `SandevistanFX.dll`** from `BONELAB/Mods` if you have them. This mod already contains both, and running them together would patch everything twice and draw two sets of effects.
3. Put `QuicksilverSandevistan.dll` in `BONELAB/Mods`.

## Building

You need the [.NET 6+ SDK](https://dotnet.microsoft.com/download) and a BONELAB install that has been launched once with MelonLoader, so that `MelonLoader/Il2CppAssemblies` exists.

```bash
dotnet build -c Release /p:BonelabPath="C:\Program Files (x86)\Steam\steamapps\common\BONELAB"
```

Add `/p:CopyToGame=true` to copy the DLL straight into `BONELAB/Mods`. The output is `bin/Release/net6.0/QuicksilverSandevistan.dll`.

## Project layout

```
src/
  QuicksilverMod.cs                Main MelonMod: Quicksilver logic, wires up the Sandevistan
  RigidbodyState.cs
  Patching/
    SlowMoPatches.cs               Replaces BONELAB's slow-mo button with the Sandevistan
    TimePatches.cs, ...            Original Quicksilver Harmony patches
  Sandevistan/
    SandevistanTime.cs             Slow-motion on/off, keeps the time scale in place
    SandevistanSettings.cs         Preferences
    SandevistanMenu.cs             BoneMenu
    Effects/                       SandevistanFX effects
      EffectsController.cs         Turns the effects on and off with slow-motion
      ScreenGrade.cs               Green grade + glow + flash
      PlayerColorShield.cs         Keeps the player out of the grade
      PlayerScanner.cs             Finds the player's renderers and items
      AfterimageSystem.cs          Pooled rainbow afterimages
      MaterialFactory.cs, ...      Helpers
```

## Known limitations

- The slow-mo button is detected through BONELAB's `TimeManager.DECREASE_TIMESCALE` / `TOGGLE_TIMESCALE`. A button press that triggers more than one of them within 0.15 s counts as one press.
- With Fusion, other players' slow-mo button presses that Fusion relays to you also toggle your Sandevistan.
- The grade needs the URP Unlit shader for the full look. If it can't be found, the mod falls back to a simpler green wash. Which shader was used is written to the MelonLoader log.

## Credits

- **Quicksilver** by [Lakatrazz](https://github.com/Lakatrazz). The Quicksilver code in this project was rebuilt from the v1.7.0 release DLL. Please ask Lakatrazz for permission before publishing this mod anywhere.
- **SandevistanFX** v1.4.0 effects, rebuilt from the release DLL.
- Inspired by *Cyberpunk: Edgerunners*.
