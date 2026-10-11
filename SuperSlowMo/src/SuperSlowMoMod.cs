using BoneLib;

using MelonLoader;

namespace SuperSlowMo
{
    public class SuperSlowMoMod : MelonMod
    {
        public const string Version = "1.0.0";

        public override void OnInitializeMelon()
        {
            SlowMoSettings.Setup();
            SlowMoSettings.SetupBoneMenu();

            // Everything we were tracking belongs to the old level, so just forget it
            Hooking.OnLevelUnloaded += WorldSlowMo.Reset;
            Hooking.OnLevelLoaded += _ => WorldSlowMo.Reset();
        }

        public override void OnUpdate()
        {
            WorldSlowMo.OnUpdate();
        }

        public override void OnFixedUpdate()
        {
            WorldSlowMo.OnFixedUpdate();
        }
    }
}
