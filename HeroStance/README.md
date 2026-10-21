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

1. Install [MelonLoader 0.6.x](https://melonwiki.xyz) and [BoneLib 3.1.x](https://bonelab.thunderstore.io/package/gnonme/BoneLib/).
2. Put `HeroStance.dll` in `BONELAB/Mods`.

## Building the DLL

The DLL has to be compiled against the game files that MelonLoader generates from your own copy of BONELAB, so you build it on your own PC. It takes about a minute.

1. Install the [.NET SDK](https://dotnet.microsoft.com/download) (version 6 or newer).
2. Launch BONELAB once with MelonLoader installed, so that `BONELAB/MelonLoader/Il2CppAssemblies` exists. BoneLib must be in `BONELAB/Mods`.
3. Double-click **`build.bat`** in this folder. It builds `HeroStance.dll` and copies it into `BONELAB/Mods` for you.
   - If BONELAB isn't installed in the default Steam folder, run it from a terminal with your path: `build.bat "D:\SteamLibrary\steamapps\common\BONELAB"`

Or build it by hand:

```bash
dotnet build -c Release /p:BonelabPath="C:\Program Files (x86)\Steam\steamapps\common\BONELAB" /p:CopyToGame=true
```

The output is `bin/Release/net6.0/HeroStance.dll`.

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

- **Nothing happens.** Check the MelonLoader console for `Hero Stance` messages. If it says the avatar has no humanoid leg bones, that avatar can't be posed. Also make sure your mirror or third-person view is showing your avatar's legs.
- **The legs flicker or don't move.** The mod applies the pose in `LateUpdate`, right after the game poses your avatar. If an update to the game moves its leg posing to later in the frame, it can overwrite the stance. Please report it if this happens.
- **The legs look too wide or too narrow.** Adjust Leg Spread in BoneMenu. Changes apply immediately.

## Project layout

```
src/
  HeroStanceMod.cs       Main MelonMod: avatar tracking, idle detection, blending
  LegPoser.cs            Bone math that poses the hips, legs and feet
  HeroStanceSettings.cs  Preferences + BoneMenu
```
