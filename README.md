# Quicksilver Sandevistan (BONELAB)

A Cyberpunk: Edgerunners–style **Sandevistan** for BONELAB, built on top of Lakatrazz's **Quicksilver** mod (v1.7.0). The two are merged into one DLL: Quicksilver keeps you moving at full speed while time slows down, and the Sandevistan adds the time-stop, the look, and the afterimages.

> This repo also contains **[Super Slow Mo](SuperSlowMo/README.md)**, a separate mod that replaces the slow-mo button with a world-only slow motion.

## Features

### Sandevistan (new)
- **Slow-motion on demand.** Time drops to 0.25x by default. Quicksilver keeps your body, hands, guns and sounds at normal speed.
- **Green color correction.** A global URP post-processing volume tints the whole world green.
- **You keep your normal colors.** The tint is a per-channel multiply, so your avatar's material colors get multiplied by the exact inverse. That cancels the green out on you, and only you. Shared materials are never modified; this is done with `MaterialPropertyBlock`s and reverted when the Sandevistan turns off.
- **Afterimages that never fade.** Frozen copies of your avatar are left behind as you move. Each one slowly cycles through the rainbow, and none of them fade or time out.
- **Full cleanup.** When the Sandevistan turns off, every afterimage, along with the meshes and materials made for it, is destroyed. Time scale, physics step, color correction, camera settings and your avatar's colors all go back to how they were.

### Quicksilver (original, by Lakatrazz)
- Walk, run, crouch, jump and turn at normal speed while in slow-motion
- Hands and fingers move at normal speed, with extra strength to match
- Guns fire at normal speed, and force grab works at normal speed
- Your own sound effects play at normal pitch

## Controls

| Action | Input |
| --- | --- |
| Toggle Sandevistan | **Click both thumbsticks together** (can be turned off in settings) |
| Toggle Sandevistan | BoneMenu → *Quicksilver Sandevistan* → **Toggle Sandevistan** |
| Toggle Quicksilver movement | BoneMenu → *Quicksilver Sandevistan* → **Quicksilver Movement** |

## Settings

BoneMenu → *Quicksilver Sandevistan* → *Sandevistan Settings* (also saved in `UserData/MelonPreferences.cfg` under `[Sandevistan]`):

| Setting | Default | Description |
| --- | --- | --- |
| Thumbstick Activation | on | Click both sticks to toggle |
| Time Scale | 0.25 | World speed while active (0.05 to 0.95) |
| Green Intensity | 0.7 | Strength of the green tint (0 to 1) |
| Afterimage Interval | 0.08 s | Minimum real time between afterimages |
| Afterimage Spacing | 0.15 m | How far you have to move before another afterimage is left behind |
| Afterimage Opacity | 0.45 | Afterimage transparency |
| Hue Cycle Speed | 0.1 | Color cycles per second (lower is slower) |
| Max Afterimages | 0 (unlimited) | Optional cap. If set, the oldest afterimage is removed once the cap is reached. |

> **Performance note:** afterimages never fade, so a long Sandevistan run keeps adding geometry. If your frame rate drops, set **Max Afterimages** (for example 150–300) or raise **Afterimage Spacing**.

## Installing

1. Install [MelonLoader 0.6.x](https://melonwiki.xyz) and [BoneLib 3.1.x](https://bonelab.thunderstore.io/package/gnonme/BoneLib/).
2. **Remove the original `Quicksilver.dll`** from `BONELAB/Mods`. This mod already contains Quicksilver, and running both would patch everything twice.
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
  QuicksilverMod.cs              Main MelonMod: original Quicksilver logic + Sandevistan hooks
  RigidbodyState.cs
  Patching/                      Original Quicksilver Harmony patches
  Sandevistan/
    SandevistanController.cs     Activation, input, time scale
    SandevistanColorCorrection.cs  Green URP volume
    PlayerColorKeeper.cs         Keeps the player un-tinted
    AfterimageSystem.cs          Never-fading, color-cycling afterimages
    SandevistanSettings.cs       Preferences + BoneMenu
```

## Known limitations

- The player's diffuse and emissive colors are compensated exactly. Specular highlights and reflections on the player can still pick up a slight green tint.
- The green tint needs URP post-processing. The mod turns post-processing on for the camera while the Sandevistan is active and restores the previous setting afterward.
- Afterimages use the first shader it finds out of `Sprites/Default`, `Universal Render Pipeline/Unlit`, `UI/Default` and `Hidden/Internal-Colored`. Which one was used is written to the MelonLoader log.

## Credits

- **Quicksilver** by [Lakatrazz](https://github.com/Lakatrazz). The Quicksilver code in this project was rebuilt from the v1.7.0 release DLL. Please ask Lakatrazz for permission before publishing this mod anywhere.
- Sandevistan add-on inspired by *Cyberpunk: Edgerunners*.
