using EFT;
using System;
using UnityEngine;
using System.Linq;
using Comfort.Common;
using System.Reflection;
using EFT.InventoryLogic;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using static Val;
using HarmonyLib;
using Aki.Reflection.Utils;
using UnityEngine.Rendering.PostProcessing;
using static EFT.Interactive.BetterPropagationGroups;
using BepInEx.Logging;

namespace RealismMod
{

    public static class DeafeningController
    {
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

        public static float EarProtectionFactor;
        public static float AmmoDeafFactor;
        public static float WeaponDeafFactor;
        public static float BotDeafFactor;
        public static float GrenadeDeafFactor;

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


        public static void DoDeafening()
        {
            float enviroMulti = PlayerStats.EnviroType == EnvironmentType.Indoor ? 1.3f : 1.05f;
            float deafFactor = AmmoDeafFactor * WeaponDeafFactor * EarProtectionFactor;
            float botDeafFactor = BotDeafFactor * EarProtectionFactor;
            float grenadeDeafFactor = GrenadeDeafFactor * EarProtectionFactor;

            if (RecoilController.IsFiringDeafen)
            {
                ChangeDeafValues(deafFactor, ref VignetteDarkness, Plugin.VigRate.Value, VignetteDarknessLimit, ref Volume, Plugin.DeafRate.Value, VolumeLimit, enviroMulti);
            }
            else if (!valuesAreReset)
            {
                ResetDeafValues(deafFactor, ref VignetteDarkness, Plugin.VigReset.Value, VignetteDarknessLimit, ref Volume, Plugin.DeafReset.Value, VolumeLimit, enviroMulti, true);
            }

            if (IsBotFiring)
            {
                ChangeDeafValues(botDeafFactor, ref BotVignetteDarkness, Plugin.VigRate.Value, VignetteDarknessLimit, ref BotVolume, Plugin.DeafRate.Value, VolumeLimit, enviroMulti);
            }
            else if (!valuesAreReset)
            {
                ResetDeafValues(botDeafFactor, ref BotVignetteDarkness, Plugin.VigReset.Value, VignetteDarknessLimit, ref BotVolume, Plugin.DeafReset.Value, VolumeLimit, enviroMulti, false);
            }

            if (GrenadeExploded)
            {
                ChangeDeafValues(grenadeDeafFactor, ref GrenadeVignetteDarkness, GrenadeVignetteDarknessIncreaseRate, GrenadeVignetteDarknessLimit, ref GrenadeVolume, GrenadeVolumeDecreaseRate, GrenadeVolumeLimit, enviroMulti);
            }
            else if (!valuesAreReset)
            {
                ResetDeafValues(grenadeDeafFactor, ref GrenadeVignetteDarkness, GrenadeVignetteDarknessResetRate, GrenadeVignetteDarknessLimit, ref GrenadeVolume, GrenadeVolumeResetRate, GrenadeVolumeLimit, enviroMulti, false);
            }

            float totalVolume = Mathf.Clamp(Volume + BotVolume + GrenadeVolume, -40.0f, 0.0f);
            float totalVignette = Mathf.Clamp(VignetteDarkness + BotVignetteDarkness + GrenadeVignetteDarkness, 0.0f, 65.0f);

            float headsetAmbientVol = Plugin.RealTimeGain.Value == 0f ? -10f : DeafeningController.AmbientVolume * (1f + (Plugin.RealTimeGain.Value / 15f)) * -Plugin.HeadsetAmbientMulti.Value;

            //for some reason this prevents the values from being fully reset to 0
            if (totalVolume != 0.0f || totalVignette != 0.0f)
            {
                DeafeningController.PrismEffects.vignetteStrength = totalVignette;

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
            else
            {
                DeafeningController.PrismEffects.useVignette = false;
                valuesAreReset = true;
            }

            if (DeafeningController.HasHeadSet && RecoilController.IsFiringDeafen)
            {
                Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorMakeup", Plugin.RealTimeGain.Value * Plugin.GainCutoff.Value);
                Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientVolume", headsetAmbientVol * (1f + (1f - Plugin.GainCutoff.Value)));
            }
            else 
            {
                Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorMakeup", Plugin.RealTimeGain.Value);
                Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientVolume", headsetAmbientVol);
            }
        }

        private static void ChangeDeafValues(float deafFactor, ref float vigValue, float vigIncRate, float vigLimit, ref float volValue, float volDecRate, float volLimit, float enviroMulti)
        {
            DeafeningController.PrismEffects.useVignette = true;
            float totalVigLimit = Mathf.Min(vigLimit * deafFactor * enviroMulti, 1.5f);
            vigValue = Mathf.Clamp(vigValue + (vigIncRate * deafFactor * enviroMulti), 0.0f, totalVigLimit);
            volValue = Mathf.Clamp(volValue - (volDecRate * deafFactor * enviroMulti), volLimit, 0.0f);
        }

        private static void ResetDeafValues(float deafFactor, ref float vigValue, float vigResetRate, float vigLimit, ref float volValue, float volResetRate, float volLimit, float enviroMulti, bool wasGunshot)
        {
            float resetFactor = wasGunshot ? 1f - (deafFactor * 0.1f) : 1f;
            float totalVigLimit = Mathf.Min(vigLimit * deafFactor * enviroMulti, 1.5f);
            vigValue = Mathf.Clamp(vigValue - (vigResetRate * resetFactor), 0.0f, totalVigLimit);
            volValue = Mathf.Clamp(volValue + (volResetRate * resetFactor), volLimit, 0.0f);
        }
    }
}
