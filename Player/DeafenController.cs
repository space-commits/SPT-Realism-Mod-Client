using Comfort.Common;
using UnityEngine;

namespace RealismMod
{

    public static class DeafeningController
    {

        public const float AmbientOutVolume = 0f; //-0.91515f
        public const float AmbientInVolume = 0f; //-80f
        public const float WeaponMuffle = -80f;
        public const float GameVolume = 0f;

        private static bool valuesAreReset = false;

        public static bool HasHeadSet = false;
        public static PrismEffects PrismEffects;

        public static bool IsBotFiring = false;
        public static bool GrenadeExploded = false;
        public static float BotTimer = 0.0f;
        public static float GrenadeTimer = 0.0f;

        public static float DryVolume = 0f;
        public static float GunsVolume = 0f;
        public static float AmbientVolume = 0f;
        public static float AmbientOccluded = 0f;
        public static float CompressorDistortion = 0f;
        public static float CompressorResonance = 0f;
        public static float CompressorLowpass = 0f;
        public static float CompressorVolume = 0f;
        public static float CompressorGain = 0f;

        public static float EarProtectionFactor = 1f;
        public static float AmmoDeafFactor = 1f;
        public static float WeaponDeafFactor = 1f;
        public static float BotDeafFactor = 1f;
        public static float GrenadeDeafFactor = 1f;

        //player
        public static float Volume = 0f;
        public static float VignetteDarkness = 0f;
        public static float VolumeLimit = -30f;
        public static float VignetteDarknessLimit = 0.34f;

        //bot
        public static float BotVolume = 0f;
        public static float BotVignetteDarkness = 0f;

        //grenade
        public static float GrenadeVolume = 0f;
        public static float GrenadeVignetteDarkness = 0f;
        public static float GrenadeVolumeLimit = -40f;
        public static float GrenadeVignetteDarknessLimit = 0.3f;
        public static float GrenadeVolumeDecreaseRate = 0.02f;
        public static float GrenadeVignetteDarknessIncreaseRate = 0.6f;
        public static float GrenadeVolumeResetRate = 0.02f;
        public static float GrenadeVignetteDarknessResetRate = 0.02f;

        public static void GetVolumeLevels() 
        {

        }

        public static void DoDeafening()
        {
            //do not use
            /*      if (PluginConfig.test3.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("ReverbVolume", PluginConfig.test3.Value); //unused
                  if (PluginConfig.test6.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("WorldVolume", PluginConfig.test6.Value); //overall volume while wearing headset, doesn't seem to do anything without a headset..tried again and it did seem to affect all volume with or without headset...
                  if (PluginConfig.test7.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientOccluded", PluginConfig.test7.Value); //also affects ambient in some way
                  if (PluginConfig.test1.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("MainVolume", PluginConfig.test1.Value); //outdoor ambient, main volume, not gunshots or green flare. Doesn't affect headset
                  if (PluginConfig.test1.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("Tinnitus2", PluginConfig.test1.Value); //not sure, does affect ambient volume
                  if (PluginConfig.test2.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("WorldReverbLevel", PluginConfig.test2.Value); //no noticable effect
                  if (PluginConfig.test4.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("GunCompressorAttack", PluginConfig.test4.Value); //no noticable ffect
                  if (PluginConfig.test5.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("GunCompressorRelease", PluginConfig.test5.Value);//no noticable ffect
                  if (PluginConfig.test6.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("GunCompressorThreshold", PluginConfig.test6.Value);//no noticable ffect
                  if (PluginConfig.test7.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("GunCompressorDistortion", PluginConfig.test7.Value); //all gun noise distortion with headset, don't mess with it
                  if (PluginConfig.test9.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("GunsLowPassFilter", PluginConfig.test9.Value); //nothing
                  if (PluginConfig.test1.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("EffectsReturnsGroupVolume", PluginConfig.test1.Value); //not sure, does affect ambient volume
                  if (PluginConfig.test2.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("HeadphonesMixerVolume", PluginConfig.test2.Value); //no noticable effect
               Singleton<BetterAudio>.Instance.Master.GetFloat("GunsCompressorSendLevel", out float sendlevel); //affected all gun volume with headset
                    //might have to use too, not sure
            if (PluginConfig.test2.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("GunsVolume", PluginConfig.test2.Value); //just gun volume...also affects all gun itneraction volume because BSG is fucking retarded. Doesn't affect it properly when wearing headest
            if (PluginConfig.test3.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("GunCompressorGain", PluginConfig.test3.Value);  //all gun volume using headset
             */
            //maybe use
            //if (PluginConfig.test8.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("OutEnvironmentVolume", PluginConfig.test8.Value); //not sure




            //ambient
            if (PluginConfig.test1.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientOutVolume", PluginConfig.test1.Value); //outdoor ambient, affects headset
            if (PluginConfig.test2.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientInVolume", PluginConfig.test2.Value);//presumably indoor ambient

            //main
            if (PluginConfig.test3.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("InGame", PluginConfig.test3.Value); //seemingly all volume with and without headset

            //distortion
            if (PluginConfig.test4.Value != 1f) Singleton<BetterAudio>.Instance.Master.SetFloat("Tinnitus1", PluginConfig.test4.Value); //higher = more muffled (reverby with headset) gunshots and all weapon sounds





            /*     float enviroMulti = PlayerValues.EnviroType == EnvironmentType.Indoor ? 1.3f : 1f;
                 float deafFactor = AmmoDeafFactor * WeaponDeafFactor * EarProtectionFactor;
                 float botDeafFactor = BotDeafFactor * EarProtectionFactor;
                 float grenadeDeafFactor = GrenadeDeafFactor * EarProtectionFactor;
                 float totalVigLimit = Mathf.Min(0.3f * deafFactor * enviroMulti, 1.5f);
                 float grenadeVigLimit = Mathf.Min(GrenadeVignetteDarknessLimit * deafFactor * enviroMulti, 1.5f);

                 if (IsBotFiring)
                 {
                     BotTimer += Time.deltaTime;
                     if (BotTimer >= 0.5f)
                     {
                         IsBotFiring = false;
                         BotTimer = 0f;
                     }
                 }

                 if (GrenadeExploded)
                 {
                     GrenadeTimer += Time.deltaTime;
                     if (GrenadeTimer >= 0.7f)
                     {
                         GrenadeExploded = false;
                         GrenadeTimer = 0f;
                     }
                 }

                 if (RecoilController.IsFiringDeafen)
                 {
                     ChangeDeafValues(deafFactor, ref VignetteDarkness, PluginConfig.VigRate.Value, totalVigLimit, ref Volume, PluginConfig.DeafRate.Value, VolumeLimit, enviroMulti);
                 }
                 else if (!valuesAreReset)
                 {
                     ResetDeafValues(ref VignetteDarkness, PluginConfig.VigReset.Value, totalVigLimit, ref Volume, PluginConfig.DeafReset.Value, VolumeLimit);
                 }

                 if (IsBotFiring)
                 {
                     ChangeDeafValues(botDeafFactor, ref BotVignetteDarkness, PluginConfig.VigRate.Value, totalVigLimit, ref BotVolume, PluginConfig.DeafRate.Value, VolumeLimit, enviroMulti);
                 }
                 else if (!valuesAreReset)
                 {
                     ResetDeafValues(ref BotVignetteDarkness, PluginConfig.VigReset.Value, totalVigLimit, ref BotVolume, PluginConfig.DeafReset.Value, VolumeLimit);
                 }

                 if (GrenadeExploded)
                 {
                     ChangeDeafValues(grenadeDeafFactor, ref GrenadeVignetteDarkness, GrenadeVignetteDarknessIncreaseRate, grenadeVigLimit, ref GrenadeVolume, GrenadeVolumeDecreaseRate, GrenadeVolumeLimit, enviroMulti);
                 }
                 else if (!valuesAreReset)
                 {
                     ResetDeafValues(ref GrenadeVignetteDarkness, GrenadeVignetteDarknessResetRate, grenadeVigLimit, ref GrenadeVolume, GrenadeVolumeResetRate, GrenadeVolumeLimit);
                 }

                 float totalVolume = Mathf.Clamp(Volume + BotVolume + GrenadeVolume, -40.0f, 0.0f);
                 float totalVignette = Mathf.Clamp(VignetteDarkness + BotVignetteDarkness + GrenadeVignetteDarkness, 0.0f, 65.0f);

                 float ambientGainMulti = 2f * (1f - Mathf.InverseLerp(0f, 30f, PluginConfig.RealTimeGain.Value));
                 float headsetAmbientVol = (DeafeningController.AmbientVolume * ambientGainMulti) + PluginConfig.HeadsetAmbientMulti.Value;

                 if (totalVolume != 0.0f || totalVignette != 0.0f)
                 {
                     DeafeningController.PrismEffects.SetVignetteStrength(totalVignette);
                     DeafeningController.PrismEffects.vignetteStart = 1.5f;
                     DeafeningController.PrismEffects.vignetteEnd = 0.1f;
                     if (!DeafeningController.HasHeadSet)
                     {
                         Singleton<BetterAudio>.Instance.Master.SetFloat("GunsVolume", totalVolume + DeafeningController.GunsVolume);
                         Singleton<BetterAudio>.Instance.Master.SetFloat("OcclusionVolume", totalVolume + DeafeningController.DryVolume);
                         Singleton<BetterAudio>.Instance.Master.SetFloat("EnvironmentVolume", totalVolume + DeafeningController.DryVolume);
                         Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientOccluded", totalVolume + DeafeningController.AmbientOccluded);
                         Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientVolume", totalVolume + DeafeningController.AmbientVolume);
                     }

                     valuesAreReset = false;
                 }
                 else if (!valuesAreReset)
                 {
                     DeafeningController.PrismEffects.useVignette = false;
                     valuesAreReset = true;
                     if (!DeafeningController.HasHeadSet)
                     {
                         Singleton<BetterAudio>.Instance.Master.SetFloat("GunsVolume", DeafeningController.GunsVolume);
                         Singleton<BetterAudio>.Instance.Master.SetFloat("OcclusionVolume", DeafeningController.DryVolume);
                         Singleton<BetterAudio>.Instance.Master.SetFloat("EnvironmentVolume", DeafeningController.DryVolume);
                         Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientOccluded", DeafeningController.AmbientOccluded);
                         Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientVolume", DeafeningController.AmbientVolume);
                     }
                 }

                 if (DeafeningController.HasHeadSet && (RecoilController.IsFiringDeafen || GrenadeVolume > 0f || BotVolume > 0f))
                 {
                     Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorMakeup", PluginConfig.RealTimeGain.Value * PluginConfig.GainCutoff.Value);
                     Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientVolume", (headsetAmbientVol * (1f + (1f - PluginConfig.GainCutoff.Value))));
                 }
                 else if (DeafeningController.HasHeadSet)
                 {
                     Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorMakeup", PluginConfig.RealTimeGain.Value);
                     Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientVolume", headsetAmbientVol);
                 }*/
        }

        private static void ChangeDeafValues(float deafFactor, ref float vigValue, float vigIncRate, float vigLimit, ref float volValue, float volDecRate, float volLimit, float enviroMulti)
        {
            DeafeningController.PrismEffects.useVignette = true;
            vigValue = Mathf.Clamp(vigValue + (vigIncRate * deafFactor * enviroMulti), 0.0f, vigLimit);
            volValue = Mathf.Clamp(volValue - (volDecRate * deafFactor * enviroMulti), volLimit, 0.0f);
        }

        private static void ResetDeafValues(ref float vigValue, float vigResetRate, float vigLimit, ref float volValue, float volResetRate, float volLimit)
        {
            vigValue = Mathf.Clamp(vigValue - vigResetRate, 0.0f, vigLimit);
            volValue = Mathf.Clamp(volValue + volResetRate, volLimit, 0.0f);
        }
    }
}
