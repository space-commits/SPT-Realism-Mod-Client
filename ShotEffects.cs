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
    public class SpeedFactorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("get_SpeedFactor", BindingFlags.Instance | BindingFlags.Public);

        }
        [PatchPostfix]
        private static void PatchPostfix(ref Weapon __instance, ref float __result)
        {

            if (__instance?.Owner?.ID != null && (__instance.Owner.ID.StartsWith("pmc") || __instance.Owner.ID.StartsWith("scav")))
            {
                AmmoTemplate currentAmmoTemplate = __instance.CurrentAmmoTemplate;

                float ammoDeafFactor = Math.Min(((currentAmmoTemplate.ammoRec / 100f) * 2) + 1, 2f) * ((1f - (((__result - 1f) * 3f) + 1f)) + 1f);

                if (currentAmmoTemplate.InitialSpeed <= 340)
                {
                    ammoDeafFactor *= 0.8f;
                    Logger.LogWarning("is subsonic");
                }

                Logger.LogWarning("speedfactor = " + __result);
                Logger.LogWarning("speedfactor factored = " + ((1f - (((__result - 1f) * 3f) + 1f)) + 1f));
                Logger.LogWarning("Ammo factor = " + __result);
                Logger.LogWarning("Ammo factor factored = " + Math.Min(((currentAmmoTemplate.ammoRec / 100f) * 2) + 1, 2f));

                Plugin.AmmoDeafFactor = ammoDeafFactor == 0 ? 1 : ammoDeafFactor;

            }
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
                    return 0.85f;
                case "High":
                    return 0.7f;
                default:
                    return 1f;
            }
        }

        private static float HeadsetDeafFactor(EquipmentClass equipment)
        {
            float protectionFactor;

            LootItemClass headwear = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as LootItemClass;
            GClass2108 headset = (equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem as GClass2108) ?? ((headwear != null) ? headwear.GetAllItemsFromCollection().OfType<GClass2108>().FirstOrDefault<GClass2108>() : null);

            if (headset != null)
            {
                Plugin.HasHeadSet = true;
                GClass2015 headphone = headset.Template;
                protectionFactor = ((headphone.DryVolume / 100f) + 1f) * 0.5f;
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
            }

        }
    }

    public static class Deafening
    {
        //original

        /*        public static float Volume = 0f;
                public static float Distortion = 0f;
                public static float VignetteDarkness = 0f;

                public static float VolumeLimit = -25f;
                public static float DistortionLimit = 150f;
                public static float VignetteDarknessLimit = 70f;

                public static float VolumeDecreaseRate = 0.07f;
                public static float DistortionIncreaseRate = 0.14f;
                public static float VignetteDarknessIncreaseRate = 0.09f;

                public static float VolumeResetRate = 0.04f;
                public static float DistortionResetRate = 0.6f;
                public static float VignetteDarknessResetRate = 0.5f;*/

        //alt
        public static float Volume = 0f;
        public static float Distortion = 0f;
        public static float VignetteDarkness = 0f;

        public static float VolumeLimit = -25f;
        public static float DistortionLimit = 150f;
        public static float VignetteDarknessLimit = 10f;

        public static float VolumeDecreaseRate = 0.03f;
        public static float DistortionIncreaseRate = 0.15f;
        public static float VignetteDarknessIncreaseRate = 0.5f;

        public static float VolumeResetRate = 0.02f;
        public static float DistortionResetRate = 0.8f;
        public static float VignetteDarknessResetRate = 0.8f;

        public static void DoDeafeningAlt()
        {
            float deafFactor = Plugin.EarProtectionFactor * Plugin.AmmoDeafFactor * Plugin.WeaponDeafFactor;

            if (Plugin.IsFiring)
            {
                Plugin.Vignette.enabled = true;
                VignetteDarkness = Mathf.Clamp(VignetteDarkness + (VignetteDarknessIncreaseRate * deafFactor), 0.0f, VignetteDarknessLimit * deafFactor);
                Volume = Mathf.Clamp(Volume - (VolumeDecreaseRate * deafFactor), VolumeLimit, 0.0f);
                Distortion = Mathf.Clamp(Distortion + (DistortionIncreaseRate * deafFactor), 0.0f, DistortionLimit);
            }
            else
            {

                VignetteDarkness = Mathf.Clamp(VignetteDarkness - VignetteDarknessResetRate, 0.0f, VignetteDarknessLimit * deafFactor);
                Volume = Mathf.Clamp(Volume + VolumeResetRate, VolumeLimit, 0.0f);
                Distortion = Mathf.Clamp(Distortion - DistortionResetRate, 0.0f, DistortionLimit);
            }

            if (Volume != 0 || Distortion != 0)
            {
                Plugin.Vignette.darkness = VignetteDarkness;
                Singleton<BetterAudio>.Instance.Master.SetFloat("GunsVolume", Volume + Plugin.GunsVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("OcclusionVolume", Volume + Plugin.MainVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("EnvironmentVolume", Volume + Plugin.MainVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientVolume", Volume + Plugin.AmbientVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientOccluded", Volume + Plugin.AmbientOccluded);
                Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorResonance", Distortion + Plugin.CompressorResonance);

                if (Plugin.HasHeadSet == false)
                {
                    Singleton<BetterAudio>.Instance.Master.SetFloat("Compressor", Distortion + Plugin.Compressor);
                    Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorDistortion", Distortion + Plugin.CompressorDistortion);
                    Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorLowpass", Distortion + Plugin.CompressorDistortion);
                }
            }
        }

        

        public static void DoDeafening()
        {

            if (Plugin.IsFiring == true)
            {
                float deafFactor = Plugin.EarProtectionFactor * Plugin.AmmoDeafFactor * Plugin.WeaponDeafFactor;

                Plugin.Vignette.enabled = true;
                VignetteDarkness = Mathf.Clamp(VignetteDarkness + (VignetteDarknessIncreaseRate * deafFactor), 0.0f, VignetteDarknessLimit);
                Volume = Mathf.Clamp(Volume - (VolumeDecreaseRate * deafFactor), VolumeLimit, 0.0f);
                Distortion = Mathf.Clamp(Distortion + (DistortionIncreaseRate * deafFactor), 0.0f, DistortionLimit);
            }
            else
            {

                VignetteDarkness = Mathf.Clamp(VignetteDarkness - VignetteDarknessResetRate, 0.0f, VignetteDarknessLimit);
                Volume = Mathf.Clamp(Volume + VolumeResetRate, VolumeLimit, 0.0f);
                Distortion = Mathf.Clamp(Distortion - DistortionResetRate, 0.0f, DistortionLimit);
            }

            if (Volume != 0 || Distortion != 0)
            {
                Plugin.Vignette.darkness = VignetteDarkness;
                Singleton<BetterAudio>.Instance.Master.SetFloat("GunsVolume", Volume + Plugin.GunsVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("OcclusionVolume", Volume + Plugin.MainVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("EnvironmentVolume", Volume + Plugin.MainVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientVolume", Volume + Plugin.AmbientVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientOccluded", Volume + Plugin.AmbientOccluded);
                Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorResonance", Distortion + Plugin.CompressorResonance);

                if (Plugin.HasHeadSet == false)
                {
                    Singleton<BetterAudio>.Instance.Master.SetFloat("Compressor", Distortion + Plugin.Compressor);
                    Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorDistortion", Distortion + Plugin.CompressorDistortion);
                    Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorLowpass", Distortion + Plugin.CompressorDistortion);
                }
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

            bool flag = template != null;

            Plugin.MainVolume = flag ? template.DryVolume : 0f;
            Plugin.Compressor = flag ? template.CompressorVolume : -80f;
            Plugin.AmbientVolume = flag ? template.AmbientVolume : 0f;
            Plugin.AmbientOccluded = flag ? (template.AmbientVolume - 15f) : -5f;
            Plugin.GunsVolume = flag ? (template.DryVolume - 40f) : 0f;

            Plugin.CompressorDistortion = flag ? template.Distortion : 0.277f; //0.277
            Plugin.CompressorResonance = flag ? template.Resonance : 2.47f; //2.47
            Plugin.CompressorLowpass = flag ? template.LowpassFreq : 22000f;

            if (flag == false)
            {
                Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorCutoff", 245f);
                Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorTreshold", -20f);
                Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorMakeup", 10f);
                Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorAttack", 35f);
            }

        }
    }


}
