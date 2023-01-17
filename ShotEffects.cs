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

namespace RealismMod
{
    public class PlayerState
    {
        public static GameWorld Gameworld
        {
            get
            {
                return Singleton<GameWorld>.Instance;
            }
        }

        public static Player.FirearmController FC
        {
            get
            {
                return Player.HandsController as Player.FirearmController;
            }
        }

        public static Player Player
        {
            get
            {
                return Gameworld.AllPlayers[0];
            }
        }

        public static bool CheckIsReady()
        {
            var sessionResultPanel = Singleton<SessionResultPanel>.Instance;

            if (Gameworld == null || Gameworld.AllPlayers == null || Gameworld.AllPlayers.Count <= 0 || sessionResultPanel != null)
            {
                return false;
            }
            return true;
        }

    }

    public class VignettePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EffectsController).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(EffectsController __instance)
        {
            CC_FastVignette vig = (CC_FastVignette)AccessTools.Field(typeof(EffectsController), "cc_FastVignette_0").GetValue(__instance);
            Plugin.Vignette = vig;
        }
    }

    public class UpdatePhonesPatch : ModulePatch
    {

        private static float HelmDeafFactor(EquipmentClass equipment)
        {
            string deafStrength = "None";
            LootItemClass headwearItem = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as LootItemClass;

            if (headwearItem != null)
            {
                var helmet = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as ArmorClass;
                if (helmet?.Armor?.Deaf != null)
                {
                    deafStrength = helmet.Armor.Deaf.ToString();
                }

            }
            return HelmDeafFactorHelper(deafStrength);
        }

        private static float HelmDeafFactorHelper(string deafStr)
        {
            switch (deafStr)
            {
                case "Low":
                    return 0.95f;
                case "High":
                    return 0.9f;
                default:
                    return 1f;
            }
        }

        private static float HeadsetDeafFactor(EquipmentClass equipment)
        {
            float protectionFactor = 1f;

            LootItemClass headwear = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as LootItemClass;
            GClass2108 headset = (equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem as GClass2108) ?? ((headwear != null) ? headwear.GetAllItemsFromCollection().OfType<GClass2108>().FirstOrDefault<GClass2108>() : null);

            if (headset != null)
            {
                Plugin.HasHeadSet = true;
                GClass2015 headphone = headset.Template;
                protectionFactor = (headphone.DryVolume / 100f) + 1;
            }
            else
            {
                Plugin.HasHeadSet = false;
                protectionFactor = 1f;
            }

            return protectionFactor;
        }


        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("UpdatePhones", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            if (__instance.IsYourPlayer)
            {
                EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(__instance);
                Plugin.EarProtectionFactor = HeadsetDeafFactor(equipment) * HelmDeafFactor(equipment);
                Logger.LogWarning("Earprotection = " + Plugin.EarProtectionFactor);
            }

        }
    }

    public static class Deafening
    {
/*        public static float CompressorVolume = 0f;*/
        public static float Volume = 0f;
        public static float Distortion = 0f;
        public static float VignetteDarkness = 0f;

/*        public static float CompressorVolumeLimit = 0f;*/
        public static float VolumeLimit = -25f;
        public static float DistortionLimit = 200f;
        public static float VignetteDarknessLimit = 80f;

/*        public static float CompressorVolumeIncreaseRate = 0f;*/
        public static float VolumeIncreaseRate = 0.07f;
        public static float DistortionIncreaseRate = 0.15f;
        public static float VignetteDarknessIncreaseRate = 0.1f;

/*        public static float CompressorVolumeResetRate = 0f;*/
        public static float VolumeResetRate = 0.05f;
        public static float DistortionResetRate = 0.3f;
        public static float VignetteDarknessResetRate = 0.5f;

        public static void DoDeafening()
        {
            if (Plugin.HasHeadSet == false)
            {
                if (Plugin.IsFiring == true)
                {
                    Plugin.Vignette.enabled = true;
                    VignetteDarkness = Mathf.Clamp(VignetteDarkness += VignetteDarknessIncreaseRate * Plugin.EarProtectionFactor, 0.0f, VignetteDarknessLimit);
                    Volume = Mathf.Clamp(Volume -= VolumeIncreaseRate * Plugin.EarProtectionFactor, VolumeLimit, 0.0f);
                    Distortion = Mathf.Clamp(Distortion += DistortionIncreaseRate * Plugin.EarProtectionFactor, 0.0f, DistortionLimit);
                }
                else
                {
                    VignetteDarkness = Mathf.Clamp(VignetteDarkness -= VignetteDarknessResetRate, 0.0f, VignetteDarknessLimit);
                    Volume = Mathf.Clamp(Volume += VolumeResetRate, VolumeLimit, 0.0f);
                    Distortion = Mathf.Clamp(Distortion -= DistortionResetRate, 0.0f, DistortionLimit);
                }

                Plugin.Vignette.darkness = VignetteDarkness;
                Singleton<BetterAudio>.Instance.Master.SetFloat("GunsVolume", Volume + Plugin.GunsVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("OcclusionVolume", Volume + Plugin.MainVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("EnvironmentVolume", Volume + Plugin.MainVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientVolume", Volume + Plugin.AmbientVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientOccluded", Volume + Plugin.AmbientOccluded);
                Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorDistortion", Distortion + Plugin.CompressorDistortion);
                Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorResonance", Distortion + Plugin.CompressorResonance);
                Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorLowpass", Distortion + Plugin.CompressorLowpass);
                Singleton<BetterAudio>.Instance.Master.SetFloat("Compressor", Distortion + Plugin.Compressor);
            }
            else
            {
                if (Plugin.IsFiring == true)
                {

                    Plugin.Vignette.enabled = true;
                    VignetteDarkness = Mathf.Clamp(VignetteDarkness += VignetteDarknessIncreaseRate * Plugin.EarProtectionFactor, 0.0f, VignetteDarknessLimit);
                    Volume = Mathf.Clamp(Volume -= VolumeIncreaseRate * Plugin.EarProtectionFactor, VolumeLimit, 0.0f);
                    Distortion = Mathf.Clamp(Distortion += DistortionIncreaseRate * Plugin.EarProtectionFactor, 0.0f, DistortionLimit);
                }
                else
                {
                    VignetteDarkness = Mathf.Clamp(VignetteDarkness -= VignetteDarknessResetRate, 0.0f, 100.0f);
                    Volume = Mathf.Clamp(Volume += VolumeResetRate, VolumeLimit, 0.0f);
                    Distortion = Mathf.Clamp(Distortion -= DistortionResetRate, 0.0f, DistortionLimit);
                }

                Plugin.Vignette.darkness = VignetteDarkness;
                Singleton<BetterAudio>.Instance.Master.SetFloat("GunsVolume", Volume + Plugin.GunsVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("OcclusionVolume", Volume + Plugin.MainVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("EnvironmentVolume", Volume + Plugin.MainVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientVolume", Volume + Plugin.AmbientVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientOccluded", Volume + Plugin.AmbientOccluded);
                Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorResonance", Distortion + Plugin.CompressorResonance);
                Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorLowpass", Distortion + Plugin.CompressorLowpass);
                Singleton<BetterAudio>.Instance.Master.SetFloat("Compressor", Volume + Plugin.Compressor);
            }
        }
    }

    public class SetCompressorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BetterAudio).GetMethod("SetCompressor", BindingFlags.Instance | BindingFlags.Public);

        }
        [PatchPostfix]
        private static void PatchPostfix(GClass2015 template)
        {
            Logger.LogWarning("SetCompressor");
            bool flag = template != null;

            Plugin.MainVolume = flag ? template.DryVolume : 0f;
            Plugin.Compressor = flag ? template.CompressorVolume : -80f;
            Plugin.AmbientVolume = flag ? template.AmbientVolume : 0f;
            Plugin.AmbientOccluded = flag ? (template.AmbientVolume - 15f) : -5f;
            Plugin.GunsVolume = flag ? (template.DryVolume - 100f) : 0f;

            Plugin.CompressorDistortion = flag ? template.Distortion : 0f;
            Plugin.CompressorResonance = flag ? template.Resonance : 0f;
            Plugin.CompressorLowpass = flag ? template.LowpassFreq : 0f;

            Logger.LogWarning("MainVolume " + Plugin.MainVolume);
            Logger.LogWarning("Compressor " + Plugin.Compressor);
            Logger.LogWarning("AmbientVolume " + Plugin.AmbientVolume);
            Logger.LogWarning("AmbientOccluded " + Plugin.AmbientOccluded);
            Logger.LogWarning("GunsVolume " + Plugin.GunsVolume);
            Logger.LogWarning("CompressorDistortion " + Plugin.CompressorDistortion);
            Logger.LogWarning("CompressorResonance " + Plugin.CompressorResonance);
            Logger.LogWarning("CompressorLowpass " + Plugin.CompressorLowpass);

        }
    }


}
