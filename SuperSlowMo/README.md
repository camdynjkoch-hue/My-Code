# Super Slow Mo (BONELAB)

Replaces BONELAB's slow-mo button. Press it and **everything in the world drops into super slow motion (10x slower by default) while you keep moving at full speed**. Press it again and time picks back up.

## How it works

The game's own slow-mo lowers `Time.timeScale`, which slows you down too. This mod **never changes the time scale**. It slows every object that isn't you, one by one, so your body, hands and movement are never touched.

For an object running at speed `s`:

| What | Change |
| --- | --- |
| Velocity | multiplied by `s` when slow-mo starts, and divided by `s` when it ends |
| Gravity | countered so only `s² × g` is left (falling objects float down) |
| Drag | multiplied by `s` |
| Animators (NPCs, doors, machines) | `Animator.speed × s` |
| NavMesh agents | speed and turn speed × `s`, acceleration × `s²` |
| Particle effects | simulation speed × `s` |

Your own hits need no special handling. In the slowed world you're `1/s` times faster than everything else, so a punch, swing or throw already lands with the right force. The speed you put into objects is stored up and released when time resumes. That's how you get the "punch something ten times, then let time go" effect. **Max Exit Speed** caps how fast things can fly off when time resumes.

**Whatever you're holding runs at full speed.** That includes the whole item: a gun's slide, magazine and so on. Once you let go of it, it joins the slowed world.

Every value is put back exactly as it was when slow-mo ends. If the game changes a value while it's slowed (an NPC switching from walk to run, for example), the new value is treated as the game's and slowed as well.

## Controls

| Action | Input |
| --- | --- |
| Toggle world slow-mo | **The normal slow-mo button** |
| Toggle world slow-mo | BoneMenu → *Super Slow Mo* → **Toggle World Slow-Mo** |

## Settings

BoneMenu → *Super Slow Mo* (also saved in `UserData/MelonPreferences.cfg` under `[SuperSlowMo]`):

| Setting | Default | Description |
| --- | --- | --- |
| Replace Slow-Mo Button | on | The slow-mo button toggles this mod instead of the game's slow-mo. Turn it off to get the normal slow-mo back. The BoneMenu toggle still works. |
| World Speed | 0.1 | How fast the world runs while active (0.01 to 1). Lower is slower. Can be changed while active. |
| Max Exit Speed | 50 m/s | Speed cap for objects when time resumes |
| Slow Animators | on | Slow down animations |
| Slow NavMesh Agents | on | Slow down NPC pathing |
| Slow Particles | on | Slow down particle effects |

The three *Slow …* toggles take effect the next time slow-mo is turned on.

## Installing

1. Install [MelonLoader](https://melonwiki.xyz) 0.6.x or 0.7.x and [BoneLib](https://bonelab.thunderstore.io/package/gnonme/BoneLib/) 3.x.
2. Put `SuperSlowMo.dll` in `BONELAB/Mods`. A prebuilt copy is in [`dist/SuperSlowMo.dll`](dist/SuperSlowMo.dll).

The prebuilt DLL was compiled without the game installed. It was built against the real MelonLoader, Harmony, Il2CppInterop and BoneLib 3.2.2 DLLs, plus stand-ins for BONELAB's generated assemblies that match their exact signatures. If it logs a `MissingMethodException` on load, build it yourself (below) against your own install.

## Building

You need the [.NET 6+ SDK](https://dotnet.microsoft.com/download) and a BONELAB install that has been launched once with MelonLoader, so that `MelonLoader/Il2CppAssemblies` exists.

```bash
cd SuperSlowMo
dotnet build -c Release /p:BonelabPath="C:\Program Files (x86)\Steam\steamapps\common\BONELAB"
```

Add `/p:CopyToGame=true` to copy the DLL straight into `BONELAB/Mods`. The output is `SuperSlowMo/bin/Release/net6.0/SuperSlowMo.dll`.

## Project layout

```
src/
  SuperSlowMoMod.cs    MelonMod entry point
  SlowMoPatches.cs     Takes over the slow-mo button (OpenControllerRig.TimeInput)
  WorldSlowMo.cs       Finds and slows everything that isn't the player
  SlowedObjects.cs     Per-object slowing and restoring (rigidbodies, animators, agents, particles)
  SlowMoSettings.cs    Preferences + BoneMenu
```

## Known limitations

- **Bullets and other script-moved things aren't slowed.** BONELAB bullets are moved by script, not by physics, so enemy bullets still fly at full speed. The same goes for anything moved by a script every frame (some moving platforms or elevators) instead of by physics or an animator.
- **Springs and motors run at normal strength.** Spring-loaded doors and NPC muscles still pull at full strength. NPCs look slow because their animations are slowed, but a ragdoll's muscles can still snap.
- **Sound isn't pitched down.** World audio plays at normal pitch.
- **Newly spawned objects can take up to 0.2 s to slow down.** New objects are found 5 times a second.
- **Very large levels:** every awake object gets a small gravity correction each physics step. If your frame rate drops, that's why.

## Using it with Quicksilver Sandevistan

Both mods can be installed together. With *Replace Slow-Mo Button* on, the game's slow-mo never runs, so Quicksilver has nothing to do unless you turn the Sandevistan on with the thumbsticks.
