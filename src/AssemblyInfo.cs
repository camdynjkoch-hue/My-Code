using System.Reflection;

using MelonLoader;

using Quicksilver;

[assembly: AssemblyTitle("QuicksilverSandevistan")]
[assembly: AssemblyVersion(QuicksilverMod.Version)]
[assembly: AssemblyFileVersion(QuicksilverMod.Version)]
[assembly: MelonInfo(typeof(QuicksilverMod), "Quicksilver Sandevistan", QuicksilverMod.Version, "Lakatrazz (Quicksilver), Sandevistan add-on (SandevistanFX effects)")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]
[assembly: MelonAdditionalDependencies("BoneLib")]
[assembly: HarmonyDontPatchAll]
