# Hero Stance (BONELAB)

When you stand still, your avatar's legs spread into a hero stance: feet planted wider than your shoulders, toes turned out, knees slightly bent. When you start moving again, the legs blend back to normal.

## How it works

- **Only when idle.** You count as idle when your body is barely moving (under 0.2 m/s by default) and you aren't pushing either thumbstick. You need to stay idle for 0.4 s before the stance starts, so short pauses while walking don't set it off.
- **Smooth blend.** The pose fades in and out over 0.35 s, with no snapping.
- **Works on any humanoid avatar.** It uses the avatar's humanoid bone map (hips, upper legs, lower legs, feet). Directions come from where the hips actually are, so it doesn't depend on how a particular avatar's bones are rotated.
- **Feet stay on the floor.** Spreading your legs would normally lift your feet a few centimeters, so the hips drop by the same amount. The soles stay flat instead of rolling onto their edges.
- **Knees track over the toes.** The knees bend in the direction the feet point, so they don't cave in.
- **Visual only.** Only your avatar's visible bones are changed. Your physics body, collision, and movement are exactly the same as before.

## Installing

1. Install [MelonLoader](https://melonwiki.xyz) (0.6.0 or newer) and [BoneLib 3.x](https://bonelab.thunderstore.io/package/gnonme/BoneLib/).
2. Copy **`HeroStance.dll`** from this folder into `BONELAB/Mods`.
3. Start BONELAB. The MelonLoader console should show `Hero Stance ready. Stand still to strike a pose.`

## Building the DLL (only if you change the code)

You need the [.NET SDK](https://dotnet.microsoft.com/download) (6 or newer). You don't need BONELAB installed to build. The project compiles against public NuGet packages, which `dotnet` downloads automatically.

Double-click **`build.bat`**, or run:

```bash
dotnet build -c Release
```

The output is `bin/Release/net6.0/HeroStance.dll`. `build.bat` also copies it next to the project and into `BONELAB/Mods`, if that folder exists. Use `build.bat "D:\path\to\BONELAB"` for a different install.

### How it builds without the game

- The Unity calls (`Transform`, `Animator`, `Time`) compile against Il2CppInterop-generated Unity 2021.3 assemblies from a public reference package. These are the same `UnityEngine.*` API that BONELAB exposes through MelonLoader. Those assemblies are only compiled against and are not shipped.
- BoneLib (for finding your avatar and reading the thumbsticks) and BoneMenu are looked up by name at runtime. If anything is missing, the mod logs a warning instead of crashing.
- Vector and rotation math is done in the mod's own code, so it doesn't depend on Unity helper methods the game may have stripped.

## Settings

BoneMenu → *Hero Stance*. Settings are also saved in `UserData/MelonPreferences.cfg` under `[HeroStance]`.

| Setting | Default | Description |
| --- | --- | --- |
| Enabled | on | Turns the stance on or off |
| Leg Spread | 12° | How far each leg swings out from the hip |
| Knee Bend | 6° | How much the knees bend |
| Foot Turn Out | 12° | How far each foot's toes point outward |
| Keep Feet Planted | on | Drops the hips so the spread feet stay on the floor |
| Idle Speed | 0.2 m/s | You count as standing still below this speed |
| Idle Delay | 0.4 s | How long you have to stand still before the stance starts |
| Blend Time | 0.35 s | How long the stance takes to blend in and out |

For a wider, more dramatic stance, try a Leg Spread of 18–22° and a Knee Bend of 10°.

## Troubleshooting

- **Nothing happens.** Check the MelonLoader console for `Hero Stance` messages. You should see `posing avatar '...'` after you spawn. If it says the avatar has no humanoid leg bones, that avatar can't be posed. If it says BoneLib isn't loaded, install BoneLib. Use a mirror to see your legs.
- **The legs flicker or don't move.** The mod applies the pose in `LateUpdate`, right after the game poses your avatar. If an update to the game moves its leg posing to later in the frame, it can overwrite the stance. Please report it if this happens.
- **The legs look too wide or too narrow.** Adjust Leg Spread in BoneMenu. Changes apply immediately.

## Project layout

```
src/
  HeroStanceMod.cs       Main MelonMod: avatar tracking, idle detection, blending
  LegPoser.cs            Poses the hips, legs and feet
  MathUtil.cs            Vector/quaternion math
  BoneLibBridge.cs       Finds the player's avatar and thumbsticks through BoneLib
  BoneMenuBridge.cs      Builds the BoneMenu page
  HeroStanceSettings.cs  Preferences
```
