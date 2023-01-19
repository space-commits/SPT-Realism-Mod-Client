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
using notGreg.UniformAim;

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

                float ammoDeafFactor = Math.Min(((currentAmmoTemplate.ammoRec / 100f) * 2f) + 1f, 2f) * ((1f - (((__result - 1f) * 2f) + 1f)) + 1f);

                if (currentAmmoTemplate.InitialSpeed <= 335f)
                {
                    ammoDeafFactor *= 0.7f;
                }

                Plugin.AmmoDeafFactor = ammoDeafFactor == 0f ? 1f : ammoDeafFactor;

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
                    return 0.95f;
                case "High":
                    return 0.9f;
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
                protectionFactor = ((headphone.DryVolume / 100f) + 1f) * 1.3f;
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
                Logger.LogWarning("UpdatePhones");
                EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(__instance);
                Plugin.EarProtectionFactor = HeadsetDeafFactor(equipment) * HelmDeafFactor(equipment);
                Logger.LogWarning("Plugin.EarProtectionFactor = " + Plugin.EarProtectionFactor);
                Logger.LogWarning("Plugin.HasHeadSet = " + Plugin.HasHeadSet);
            }

        }
    }

    public static class Deafening
    {

        //alt
        public static float Volume = 0f;
        public static float Distortion = 0f;
        public static float VignetteDarkness = 0f;

        public static float VolumeLimit = -45f;
        public static float DistortionLimit = 150f;
        public static float VignetteDarknessLimit = 12f;

        public static float VolumeDecreaseRate = 0.025f;
        public static float DistortionIncreaseRate = 0.17f;
        public static float VignetteDarknessIncreaseRate = 0.45f;

        public static float VolumeResetRate = 0.02f;
        public static float DistortionResetRate = 0.7f;
        public static float VignetteDarknessResetRate = 0.5f;

        public static void DoDeafening()
        {
            float deafFactor = Plugin.AmmoDeafFactor * Plugin.WeaponDeafFactor * Plugin.EarProtectionFactor;

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
    }

    public class SetCompressorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BetterAudio).GetMethod("SetCompressor", BindingFlags.Instance | BindingFlags.Public);

        }
        [PatchPrefix]
        private static bool Prefix(GClass2015 template, BetterAudio __instance)
        {
            Logger.LogWarning("SetCompressor");

            bool hasHeadsetTemplate = template != null;
            bool isNotHeadset = template?._id == null; //using both bools is redundant now.

            Logger.LogWarning("isNotHeadset = " + isNotHeadset);
            Logger.LogWarning("hasHeadsetTemplate = " + hasHeadsetTemplate);

            Plugin.MainVolume = hasHeadsetTemplate && !isNotHeadset ? template.DryVolume : 0f;
            Plugin.Compressor = hasHeadsetTemplate && !isNotHeadset ? template.CompressorVolume : -80f;
            Plugin.AmbientVolume = hasHeadsetTemplate && !isNotHeadset ? template.AmbientVolume : 0f;
            Plugin.AmbientOccluded = hasHeadsetTemplate && !isNotHeadset ? (template.AmbientVolume - 15f) : -5f;
            Plugin.GunsVolume = hasHeadsetTemplate && !isNotHeadset ? (template.DryVolume - 30f) : 0f;

            Plugin.CompressorDistortion = hasHeadsetTemplate && !isNotHeadset ? template.Distortion : 0.277f;
            Plugin.CompressorResonance = hasHeadsetTemplate && !isNotHeadset ? template.Resonance : 2.47f;
            Plugin.CompressorLowpass = hasHeadsetTemplate && !isNotHeadset ? template.LowpassFreq : 22000f;

            __instance.Master.SetFloat("Compressor", Plugin.Compressor);
            __instance.Master.SetFloat("OcclusionVolume", Plugin.MainVolume);
            __instance.Master.SetFloat("EnvironmentVolume", Plugin.MainVolume);
            __instance.Master.SetFloat("AmbientVolume", Plugin.AmbientVolume);
            __instance.Master.SetFloat("AmbientOccluded", Plugin.AmbientOccluded);
            __instance.Master.SetFloat("GunsVolume", Plugin.GunsVolume);

            __instance.Master.SetFloat("CompressorAttack", hasHeadsetTemplate && !isNotHeadset ? template.CompressorAttack : 35f);
            __instance.Master.SetFloat("CompressorMakeup", hasHeadsetTemplate && !isNotHeadset ? template.CompressorGain : 10f);
            __instance.Master.SetFloat("CompressorRelease", hasHeadsetTemplate && !isNotHeadset ? template.CompressorRelease : 215f);
            __instance.Master.SetFloat("CompressorTreshold", hasHeadsetTemplate && !isNotHeadset ? template.CompressorTreshold : -20f);
            __instance.Master.SetFloat("CompressorDistortion", Plugin.CompressorDistortion);
            __instance.Master.SetFloat("CompressorResonance", Plugin.CompressorResonance);
            __instance.Master.SetFloat("CompressorCutoff", hasHeadsetTemplate && !isNotHeadset ? template.CutoffFreq : 245f);
            __instance.Master.SetFloat("CompressorLowpass", Plugin.CompressorLowpass);


            return false;

        }
    }

    public class RegisterShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("RegisterShot", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public static float externalDeafenFactor = 1f;

        private static float getMuzzleLoudness(Mod[] mods)
        {
            float loudness = 0f;
            for (int i = 0; i < mods.Length; i++)
            {
                if (mods[i].Slots.Length > 0 && mods[i].Slots[0].ContainedItem != null && Helper.IsSilencer((Mod)mods[i].Slots[0].ContainedItem))
                {
                    return 0;
                }
                else
                {
                    loudness += mods[i].Template.Loudness;
                }
            }
            return (loudness / 100) + 1f;
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance, Weapon weapon)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsAI)
            {
                float distanceFromPlayer = Vector3.Distance(__instance.gameObject.transform.position, Singleton<GameWorld>.Instance.AllPlayers[0].Transform.position);
                if (distanceFromPlayer <= 40f)
                {
                    AmmoTemplate currentAmmoTemplate = weapon.CurrentAmmoTemplate;
                    float muzzleLoudness = getMuzzleLoudness(weapon.Mods) * StatCalc.CalibreLoudnessFactor(weapon.AmmoCaliber) * ((1f - (((weapon.SpeedFactor - 1f) * 2f) + 1f)) + 1f) * Math.Min(((currentAmmoTemplate.ammoRec / 100f) * 2f) + 1f, 2f);
                    externalDeafenFactor = muzzleLoudness;
                    Logger.LogWarning("=========================");
                    Logger.LogWarning("Muzzle Loudness = " + muzzleLoudness);
                    Logger.LogWarning("=========================");
                }

                //get as much info about shot as possible (calibre, suppressed or not/muzzle device loudness)
                //get distance to player, if over a certain distance then ignore, if within that limit then increase deafen factor closer to player
                //apply the effect in void update somehow.


            }
        }

        public void DoExternalDeafening()
        {

        }
    }



}
