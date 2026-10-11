using BoneLib.BoneMenu;

using UnityEngine;

namespace Quicksilver.Sandevistan
{
    public static class SandevistanMenu
    {
        public static readonly Color MenuColor = new Color(0.24f, 1f, 0.45f, 1f);

        private static readonly Color White = Color.white;
        private static readonly Color Yellow = new Color(1f, 0.92f, 0.2f, 1f);

        public static void Create(Page parent, EffectsController effects)
        {
            parent.CreateFunction("Toggle Sandevistan", MenuColor, SandevistanTime.Toggle);
            parent.CreateBool("Replace Slow-Mo Button", MenuColor, SandevistanSettings.ReplaceSlowMo.Value, v => SandevistanSettings.Set(SandevistanSettings.ReplaceSlowMo, v));
            parent.CreateFloat("Time Scale", MenuColor, SandevistanSettings.TimeScaleEntry.Value, 0.05f, 0.05f, 0.95f, v => SandevistanSettings.Set(SandevistanSettings.TimeScaleEntry, v));

            var fx = parent.CreatePage("Sandevistan FX", MenuColor, 0, true);
            fx.CreateBool("Enabled", White, SandevistanSettings.Enabled.Value, v => SandevistanSettings.Set(SandevistanSettings.Enabled, v));
            fx.CreateBool("Preview (force on)", Yellow, false, v => effects.Preview = v);

            var grade = fx.CreatePage("Color Grade", MenuColor, 0, true);
            grade.CreateFloat("Strength", White, SandevistanSettings.GradeStrength.Value, 0.05f, 0f, 1f, v => SandevistanSettings.Set(SandevistanSettings.GradeStrength, v));
            grade.CreateFloat("Glow", White, SandevistanSettings.GlowStrength.Value, 0.02f, 0f, 1f, v => SandevistanSettings.Set(SandevistanSettings.GlowStrength, v));
            grade.CreateFloat("Activation Flash", White, SandevistanSettings.ActivationFlash.Value, 0.1f, 0f, 1f, v => SandevistanSettings.Set(SandevistanSettings.ActivationFlash, v));
            grade.CreateFloat("Fade In (s)", White, SandevistanSettings.FadeInTime.Value, 0.05f, 0f, 2f, v => SandevistanSettings.Set(SandevistanSettings.FadeInTime, v));
            grade.CreateFloat("Fade Out (s)", White, SandevistanSettings.FadeOutTime.Value, 0.05f, 0f, 2f, v => SandevistanSettings.Set(SandevistanSettings.FadeOutTime, v));
            grade.CreateBool("Keep Player Colors", White, SandevistanSettings.KeepPlayerColors.Value, v => SandevistanSettings.Set(SandevistanSettings.KeepPlayerColors, v));
            grade.CreateBool("Use Fallback Shader", White, SandevistanSettings.UseFallbackShader.Value, v =>
            {
                SandevistanSettings.Set(SandevistanSettings.UseFallbackShader, v);
                effects.OnMaterialSettingsChanged();
            });

            var afterimages = fx.CreatePage("Afterimages", MenuColor, 0, true);
            afterimages.CreateBool("Enabled", White, SandevistanSettings.AfterimagesEnabled.Value, v => SandevistanSettings.Set(SandevistanSettings.AfterimagesEnabled, v));
            afterimages.CreateFloat("Spawn Interval (s)", White, SandevistanSettings.AfterimageInterval.Value, 0.01f, 0.02f, 0.5f, v => SandevistanSettings.Set(SandevistanSettings.AfterimageInterval, v));
            afterimages.CreateFloat("Lifetime (s)", White, SandevistanSettings.AfterimageLifetime.Value, 0.25f, 0.25f, 10f, v => SandevistanSettings.Set(SandevistanSettings.AfterimageLifetime, v));
            afterimages.CreateFloat("Opacity", White, SandevistanSettings.AfterimageOpacity.Value, 0.05f, 0.05f, 1f, v => SandevistanSettings.Set(SandevistanSettings.AfterimageOpacity, v));
            afterimages.CreateFloat("Rainbow Speed", White, SandevistanSettings.AfterimageHueSpeed.Value, 0.05f, 0f, 2f, v => SandevistanSettings.Set(SandevistanSettings.AfterimageHueSpeed, v));
            afterimages.CreateFloat("Saturation", White, SandevistanSettings.AfterimageSaturation.Value, 0.05f, 0f, 1f, v => SandevistanSettings.Set(SandevistanSettings.AfterimageSaturation, v));
            afterimages.CreateInt("Max Count", White, SandevistanSettings.AfterimageMaxCount.Value, 5, 5, SandevistanSettings.MaxCountLimit, v => SandevistanSettings.Set(SandevistanSettings.AfterimageMaxCount, v));
            afterimages.CreateBool("Include Items", White, SandevistanSettings.AfterimageIncludeItems.Value, v => SandevistanSettings.Set(SandevistanSettings.AfterimageIncludeItems, v));
            afterimages.CreateBool("Glowing (Additive)", White, SandevistanSettings.AfterimageAdditive.Value, v =>
            {
                SandevistanSettings.Set(SandevistanSettings.AfterimageAdditive, v);
                effects.OnMaterialSettingsChanged();
            });
        }
    }
}
