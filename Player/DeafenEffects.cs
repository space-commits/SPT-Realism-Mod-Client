﻿using EFT;
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
using HeadsetClass = GClass2451;
using HeadsetTemplate = GClass2357;
using IWeapon = GInterface273;
using CompressorClass = GClass2718;
using CompressorTemplateClass = GClass2705;
using ShotClass = GClass2784;

namespace RealismMod
{

    public class PrismEffectsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PrismEffects).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostFix(PrismEffects __instance)
        {
            if (__instance.gameObject.name == "FPS Camera") DeafeningController.PrismEffects = __instance;
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
            DeafeningController.Vignette = vig;
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
                ArmorClass helmet = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as ArmorClass;
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
                    return 0.92f;
                case "High":
                    return 0.85f;
                default:
                    return 1f;
            }
        }

        private static float HeadsetDeafFactor(EquipmentClass equipment)
        {
            float protectionFactor;

            LootItemClass headwear = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as LootItemClass;
            HeadsetClass headset = (equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem as HeadsetClass) ?? ((headwear != null) ? headwear.GetAllItemsFromCollection().OfType<HeadsetClass>().FirstOrDefault<HeadsetClass>() : null);

            if (headset != null)
            {
                DeafeningController.HasHeadSet = true;
                HeadsetTemplate headphone = headset.Template;
                protectionFactor = Mathf.Clamp(((headphone.DryVolume / 100f) + 1f) * 1.6f, 0.5f, 1f);
            }
            else
            {
                DeafeningController.HasHeadSet = false;
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
                DeafeningController.EarProtectionFactor = HeadsetDeafFactor(equipment) * HelmDeafFactor(equipment);
            }

        }
    }

    public static class DeafeningController
    {
        public static bool HasHeadSet = false;
        public static CC_FastVignette Vignette;
        public static PrismEffects PrismEffects;

        public static float DryVolume = 0f;
        public static float GunsVolume = 0f;
        public static float AmbientVolume = 0f;
        public static float AmbientOccluded = 0f;
        public static float CompressorDistortion = 0f;
        public static float CompressorResonance = 0f;
        public static float CompressorLowpass = 0f;
        public static float Compressor = 0f;
        public static float CompressorGain = 0f;

        public static float EarProtectionFactor;
        public static float AmmoDeafFactor;
        public static float WeaponDeafFactor;
        public static float BotDeafFactor;
        public static float GrenadeDeafFactor;

        //player
        public static float Volume = 0f;
        public static float Distortion = 0f;
        public static float VignetteDarkness = 0f;

        public static float VolumeLimit = -30f;
        public static float DistortionLimit = 70f;
        public static float VignetteDarknessLimit = 0.34f;

        //bot
        public static float BotVolume = 0f;
        public static float BotDistortion = 0f;
        public static float BotVignetteDarkness = 0f;

        //grenade
        public static float GrenadeVolume = 0f;
        public static float GrenadeDistortion = 0f;
        public static float GrenadeVignetteDarkness = 0f;

        public static float GrenadeVolumeLimit = -40f;
        public static float GrenadeDistortionLimit = 50f;
        public static float GrenadeVignetteDarknessLimit = 0.3f;

        public static float GrenadeVolumeDecreaseRate = 0.02f;
        public static float GrenadeDistortionIncreaseRate = 0.5f;
        public static float GrenadeVignetteDarknessIncreaseRate = 0.6f;

        public static float GrenadeVolumeResetRate = 0.02f;
        public static float GrenadeDistortionResetRate = 0.1f;
        public static float GrenadeVignetteDarknessResetRate = 0.02f;

        public static bool valuesAreReset = false;

        public static void DoDeafening() 
        {
            float enviroMulti = PlayerProperties.EnviroType == EnvironmentType.Indoor ? 1.3f : 1.05f;
            float deafFactor = AmmoDeafFactor * WeaponDeafFactor * EarProtectionFactor;
            float botDeafFactor = BotDeafFactor * EarProtectionFactor;
            float grenadeDeafFactor = GrenadeDeafFactor * EarProtectionFactor;

            if (RecoilController.IsFiring)
            {
                ChangeDeafValues(deafFactor, ref VignetteDarkness, Plugin.VigRate.Value, VignetteDarknessLimit, ref Volume, Plugin.DeafRate.Value, VolumeLimit, ref Distortion, Plugin.DistRate.Value, DistortionLimit, enviroMulti);
            }
            else if (!valuesAreReset)
            {
                ResetDeafValues(deafFactor, ref VignetteDarkness, Plugin.VigReset.Value, VignetteDarknessLimit, ref Volume, Plugin.DeafReset.Value, VolumeLimit, ref Distortion, Plugin.DistReset.Value, DistortionLimit, enviroMulti, true);
            }

            if (Plugin.IsBotFiring)
            {
                ChangeDeafValues(botDeafFactor, ref BotVignetteDarkness, Plugin.VigRate.Value, VignetteDarknessLimit, ref BotVolume, Plugin.DeafRate.Value, VolumeLimit, ref BotDistortion, Plugin.DistRate.Value, DistortionLimit, enviroMulti);
            }
            else if (!valuesAreReset)
            {
                ResetDeafValues(botDeafFactor, ref BotVignetteDarkness, Plugin.VigReset.Value, VignetteDarknessLimit, ref BotVolume, Plugin.DeafReset.Value, VolumeLimit, ref BotDistortion, Plugin.DistReset.Value, DistortionLimit, enviroMulti, false);
            }

            if (Plugin.GrenadeExploded)
            {
                ChangeDeafValues(grenadeDeafFactor, ref GrenadeVignetteDarkness, GrenadeVignetteDarknessIncreaseRate, GrenadeVignetteDarknessLimit, ref GrenadeVolume, GrenadeVolumeDecreaseRate, GrenadeVolumeLimit, ref GrenadeDistortion, GrenadeDistortionIncreaseRate, GrenadeDistortionLimit, enviroMulti);
            }
            else if (!valuesAreReset)
            {
                ResetDeafValues(grenadeDeafFactor, ref GrenadeVignetteDarkness, GrenadeVignetteDarknessResetRate, GrenadeVignetteDarknessLimit, ref GrenadeVolume, GrenadeVolumeResetRate, GrenadeVolumeLimit, ref GrenadeDistortion, GrenadeDistortionResetRate, GrenadeDistortionLimit, enviroMulti, false);
            }


            float totalVolume = Mathf.Clamp(Volume + BotVolume + GrenadeVolume, -40.0f, 0.0f);
            float totalDistortion = Mathf.Clamp(Distortion + BotDistortion + GrenadeDistortion, 0.0f, 70.0f);
            float totalVignette = Mathf.Clamp(VignetteDarkness + BotVignetteDarkness + GrenadeVignetteDarkness, 0.0f, 65.0f);

            float headsetAmbientVol = DeafeningController.AmbientVolume * (1f + ((20f - Plugin.RealTimeGain.Value) / Plugin.HeadsetAmbientMulti.Value));
             
            //for some reason this prevents the values from being fully reset to 0
            if (totalVolume != 0.0f || totalDistortion != 0.0f || totalVignette != 0.0f)
            {
                DeafeningController.PrismEffects.vignetteStrength = totalVignette;   
/*                Plugin.Vignette.darkness = totalVignette;
*/              Singleton<BetterAudio>.Instance.Master.SetFloat("GunsVolume", totalVolume + DeafeningController.GunsVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("OcclusionVolume", totalVolume + DeafeningController.DryVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("EnvironmentVolume", totalVolume + DeafeningController.DryVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientVolume", totalVolume + DeafeningController.AmbientVolume);
                Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientOccluded", totalVolume + DeafeningController.AmbientOccluded);


                if (!DeafeningController.HasHeadSet)
                {
                    Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorResonance", totalDistortion + DeafeningController.CompressorResonance);
                    Singleton<BetterAudio>.Instance.Master.SetFloat("Compressor", totalDistortion + DeafeningController.Compressor);
                    Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorDistortion", totalDistortion + DeafeningController.CompressorDistortion);
                    Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorLowpass", totalDistortion + DeafeningController.CompressorDistortion);
                }
                else
                {
                    Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorMakeup", Plugin.RealTimeGain.Value * Plugin.GainCutoff.Value);
                    Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientVolume", headsetAmbientVol * (1f + (1f - Plugin.GainCutoff.Value)));
                }
                valuesAreReset = false;
            }
            else
            {
                if (DeafeningController.HasHeadSet)
                {
                    Singleton<BetterAudio>.Instance.Master.SetFloat("CompressorMakeup", Plugin.RealTimeGain.Value);
                    Singleton<BetterAudio>.Instance.Master.SetFloat("AmbientVolume", headsetAmbientVol);

                }
                DeafeningController.PrismEffects.useVignette = false;
                valuesAreReset = true;
            }
        }

        private static void ChangeDeafValues(float deafFactor, ref float vigValue, float vigIncRate, float vigLimit, ref float volValue, float volDecRate, float volLimit, ref float distValue, float distIncRate, float distLimit, float enviroMulti)
        {
            DeafeningController.PrismEffects.useVignette = true;
            float totalVigLimit = Mathf.Min(vigLimit * deafFactor * enviroMulti, 1.5f);
            vigValue = Mathf.Clamp(vigValue + (vigIncRate * deafFactor * enviroMulti), 0.0f, totalVigLimit);
            volValue = Mathf.Clamp(volValue - (volDecRate * deafFactor * enviroMulti), volLimit, 0.0f);
            distValue = Mathf.Clamp(distValue + (distIncRate * deafFactor), 0.0f, distLimit);
        }

        private static void ResetDeafValues(float deafFactor, ref float vigValue, float vigResetRate, float vigLimit, ref float volValue, float volResetRate, float volLimit, ref float distValue, float distResetRate, float distLimit, float enviroMulti, bool wasGunshot)
        {
            float resetFactor = wasGunshot ? 1f - (deafFactor * 0.1f) : 1f;
            float totalVigLimit = Mathf.Min(vigLimit * deafFactor * enviroMulti, 1.5f);
            vigValue = Mathf.Clamp(vigValue - (vigResetRate * resetFactor), 0.0f, totalVigLimit);
            volValue = Mathf.Clamp(volValue + (volResetRate * resetFactor), volLimit, 0.0f);
            distValue = Mathf.Clamp(distValue - distResetRate, 0.0f, distLimit);
        }
    }

    public class SetCompressorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BetterAudio).GetMethod("SetCompressor", BindingFlags.Instance | BindingFlags.Public);

        }
        [PatchPrefix]
        private static bool Prefix(HeadsetTemplate template, BetterAudio __instance)
        {
            FieldInfo float_0Field = AccessTools.Field(typeof(BetterAudio), "float_0");
            bool hasHeadset = template != null;
            float gunT;
            float mainT;

            CompressorClass.CreateEvent<CompressorTemplateClass>().Invoke(template);

            DeafeningController.DryVolume = hasHeadset ? template.DryVolume : 0f;
            DeafeningController.Compressor = hasHeadset ? template.CompressorVolume : -80f;
            DeafeningController.AmbientVolume = hasHeadset ? template.AmbientVolume : 0f;
            DeafeningController.AmbientOccluded = hasHeadset ? (template.AmbientVolume - 15f) : -5f;
            DeafeningController.GunsVolume = hasHeadset ? (template.DryVolume) : -5f;

            DeafeningController.CompressorDistortion = hasHeadset ? template.Distortion : 0.277f;
            DeafeningController.CompressorResonance = hasHeadset ? template.Resonance : 2.47f;
            DeafeningController.CompressorLowpass = hasHeadset ? template.LowpassFreq : 22000f;
            DeafeningController.CompressorGain = hasHeadset ? Plugin.RealTimeGain.Value : 10f;

            __instance.Master.GetFloat(__instance.AudioMixerData.GunsMixerTinnitusSendLevel, out gunT);
            __instance.Master.GetFloat(__instance.AudioMixerData.MainMixerTinnitusSendLevel, out mainT);
            __instance.Master.SetFloat(__instance.AudioMixerData.GunsMixerTinnitusSendLevel, gunT);
            __instance.Master.SetFloat(__instance.AudioMixerData.MainMixerTinnitusSendLevel, mainT);
            __instance.Master.SetFloat(__instance.AudioMixerData.CompressorMixerVolume, hasHeadset ? template.CompressorVolume : -80f);
            __instance.Master.SetFloat(__instance.AudioMixerData.OcclusionMixerVolume, hasHeadset ? template.DryVolume : 0f);
            __instance.Master.SetFloat(__instance.AudioMixerData.EnvironmentMixerVolume, hasHeadset ? template.DryVolume : 0f);
            __instance.Master.SetFloat(__instance.AudioMixerData.AmbientMixerVolume, hasHeadset ? template.AmbientVolume : 0f);
            __instance.Master.SetFloat(__instance.AudioMixerData.AmbientMixerOcclusionSendLevel, hasHeadset ? (template.AmbientVolume - 15f) : -5f);
            __instance.Master.SetFloat(__instance.AudioMixerData.ReverbMixerVolume, hasHeadset ? template.ReverbVolume : -20f);

            float_0Field.SetValue(__instance, (hasHeadset ? DeafeningController.DryVolume : 0f));
            float dryVolume = (float)float_0Field.GetValue(__instance);
            __instance.Master.SetFloat(__instance.AudioMixerData.GunsMixerVolume, dryVolume);
            if (!hasHeadset)
            {
                return false;
            }
            __instance.Master.SetFloat(__instance.AudioMixerData.CompressorAttack, template.CompressorAttack);
            __instance.Master.SetFloat(__instance.AudioMixerData.CompressorGain, template.CompressorGain);
            __instance.Master.SetFloat(__instance.AudioMixerData.CompressorRelease, template.CompressorRelease);
            __instance.Master.SetFloat(__instance.AudioMixerData.CompressorThreshold, template.CompressorTreshold);
            __instance.Master.SetFloat(__instance.AudioMixerData.CompressorDistortion, template.Distortion);
            __instance.Master.SetFloat(__instance.AudioMixerData.CompressorResonance, template.Resonance);
            __instance.Master.SetFloat(__instance.AudioMixerData.CompressorCutoff, (float)template.CutoffFreq);
            __instance.Master.SetFloat(__instance.AudioMixerData.CompressorLowpass, (float)template.LowpassFreq);
            __instance.Master.SetFloat(__instance.AudioMixerData.CompressorHighFrequenciesGain, template.HighFrequenciesGain);

            return false;


            /* bool hasHeadsetTemplate = template != null;
             bool isNotHeadset = template?._id == null; //using both bools is redundant now.

             Plugin.DryVolume = hasHeadsetTemplate && !isNotHeadset ? template.DryVolume : 0f;
             Plugin.Compressor = hasHeadsetTemplate && !isNotHeadset ? template.CompressorVolume : -80f;
             Plugin.AmbientVolume = hasHeadsetTemplate && !isNotHeadset ? template.AmbientVolume : 0f;
             Plugin.AmbientOccluded = hasHeadsetTemplate && !isNotHeadset ? (template.AmbientVolume - 15f) : -5f;
             Plugin.GunsVolume = hasHeadsetTemplate && !isNotHeadset ? (template.DryVolume) : -5f;

             Plugin.CompressorDistortion = hasHeadsetTemplate && !isNotHeadset ? template.Distortion : 0.277f;
             Plugin.CompressorResonance = hasHeadsetTemplate && !isNotHeadset ? template.Resonance : 2.47f;
             Plugin.CompressorLowpass = hasHeadsetTemplate && !isNotHeadset ? template.LowpassFreq : 22000f;
             Plugin.CompressorGain = hasHeadsetTemplate && !isNotHeadset ? Plugin.RealTimeGain.Value : 10f;

             __instance.Master.SetFloat(__instance.AudioMixerData.CompressorMixerVolume, Plugin.Compressor);
             __instance.Master.SetFloat(__instance.AudioMixerData.OcclusionMixerVolume, Plugin.DryVolume);
             __instance.Master.SetFloat(__instance.AudioMixerData.EnvironmentMixerVolume, Plugin.DryVolume);
             __instance.Master.SetFloat(__instance.AudioMixerData.AmbientMixerVolume, Plugin.AmbientVolume);
             __instance.Master.SetFloat(__instance.AudioMixerData.AmbientMixerOcclusionSendLevel, hasHeadsetTemplate && !isNotHeadset ? (template.AmbientVolume - 15f) : -5f);
             __instance.Master.SetFloat(__instance.AudioMixerData.ReverbMixerVolume, hasHeadsetTemplate && !isNotHeadset ? template.ReverbVolume : -20f);
             __instance.Master.SetFloat(__instance.AudioMixerData.GunsMixerVolume, Plugin.GunsVolume);

             __instance.Master.SetFloat(__instance.AudioMixerData.CompressorAttack, hasHeadsetTemplate && !isNotHeadset ? template.CompressorAttack : 35f);
             __instance.Master.SetFloat(__instance.AudioMixerData.CompressorGain, Plugin.CompressorGain);
             __instance.Master.SetFloat(__instance.AudioMixerData.CompressorRelease, hasHeadsetTemplate && !isNotHeadset ? template.CompressorRelease : 215f);
             __instance.Master.SetFloat(__instance.AudioMixerData.CompressorThreshold, hasHeadsetTemplate && !isNotHeadset ? template.CompressorTreshold : -20f);
             __instance.Master.SetFloat(__instance.AudioMixerData.CompressorDistortion, Plugin.CompressorDistortion);
             __instance.Master.SetFloat(__instance.AudioMixerData.CompressorResonance, Plugin.CompressorResonance);
             __instance.Master.SetFloat(__instance.AudioMixerData.CompressorCutoff, hasHeadsetTemplate && !isNotHeadset ? template.CutoffFreq : 245f);
             __instance.Master.SetFloat(__instance.AudioMixerData.CompressorLowpass, Plugin.CompressorLowpass);
             __instance.Master.SetFloat(__instance.AudioMixerData.CompressorHighFrequenciesGain, hasHeadsetTemplate && !isNotHeadset ? template.HighFrequenciesGain : 1f);

             //cursed BSG bull shit, best just replicate it
             __instance.Master.GetFloat(__instance.AudioMixerData.GunsMixerTinnitusSendLevel, out float tin1);
             __instance.Master.GetFloat(__instance.AudioMixerData.MainMixerTinnitusSendLevel, out float tin2);
             __instance.Master.SetFloat(__instance.AudioMixerData.GunsMixerTinnitusSendLevel, tin1);
             __instance.Master.SetFloat(__instance.AudioMixerData.MainMixerTinnitusSendLevel, tin2);*/


        }
    }

    public class RegisterShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("RegisterShot", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static float GetMuzzleLoudness(Mod[] mods)
        {
            float loudness = 0f;

            foreach (Mod mod in mods.OfType<Mod>())
            {
                if (mod.Slots.Length > 0 && mod.Slots[0].ContainedItem != null && Utils.IsSilencer((Mod)mod.Slots[0].ContainedItem))
                {
                    continue;
                }
                else
                {
                    loudness += mod.Template.Loudness;
                }
            }
            return (loudness / 100) + 1f;

        }

        private static float CalcAmmoFactor(AmmoTemplate ammTemp)
        {
            return Math.Min((ammTemp.ammoRec / 100f) + 1f, 2f);
        }

        private static float CalcVelocityFactor(Weapon weap)
        {
           return ((weap.SpeedFactor - 1f) * -2f) + 1f;
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance, Item weapon, ShotClass shot)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);

            IWeapon iWeap = weapon as IWeapon;
            if (!iWeap.IsUnderbarrelWeapon)
            {
                Weapon weap = weapon as Weapon;

                if (player.IsYourPlayer == true)
                {
                    BulletClass bullet = shot.Ammo as BulletClass;
                    AmmoTemplate currentAmmoTemplate = bullet.Template as AmmoTemplate;

                    float ammoFactor = CalcAmmoFactor(currentAmmoTemplate);
                    float velocityFactor = CalcVelocityFactor(weap);
                    float ammoDeafFactor = ammoFactor * velocityFactor;

                    if (bullet.InitialSpeed * weap.SpeedFactor <= 335f)
                    {
                        ammoDeafFactor *= 0.6f;
                    }

                    DeafeningController.AmmoDeafFactor = ammoDeafFactor == 0f ? 1f : ammoDeafFactor;
                }
                else
                {
                    Vector3 playerPos = Singleton<GameWorld>.Instance.AllAlivePlayersList[0].Transform.position;
                    Vector3 shootePos = player.Transform.position;
                    float distanceFromPlayer = Vector3.Distance(shootePos, playerPos);
                    if (distanceFromPlayer <= 15f)
                    {
                        Plugin.IsBotFiring = true;
                        BulletClass bullet = shot.Ammo as BulletClass;
                        AmmoTemplate currentAmmoTemplate = bullet.Template as AmmoTemplate;
                        float velocityFactor = CalcVelocityFactor(weap);
                        float ammoFactor = CalcAmmoFactor(currentAmmoTemplate);
                      /*  float muzzleFactor = GetMuzzleLoudness(weap.Mods);*/
                        float calFactor = StatCalc.CalibreLoudnessFactor(weap.AmmoCaliber);
                        float ammoDeafFactor = ammoFactor * velocityFactor;
                        if (bullet.InitialSpeed * weap.SpeedFactor <= 335f)
                        {
                            ammoDeafFactor *= 0.6f;
                        }
                        float totalBotDeafFactor = 1 * calFactor * ammoDeafFactor;
                        DeafeningController.BotDeafFactor = totalBotDeafFactor * ((-distanceFromPlayer / 100f) + 1f) * 1.25f;

                    }
                }
            }
        }
    }

    public class ExplosionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Grenade).GetMethod("Explosion", BindingFlags.Static | BindingFlags.Public);
        }

        [PatchPrefix]
        static void PreFix(IExplosiveItem grenadeItem, Vector3 grenadePosition)
        {
            Player player = Singleton<GameWorld>.Instance.AllAlivePlayersList[0];
            float distanceFromPlayer = Vector3.Distance(grenadePosition, player.Transform.position);

            if (distanceFromPlayer <= 15f)
            {
                Plugin.GrenadeExploded = true;

                DeafeningController.GrenadeDeafFactor = grenadeItem.Contusion.z * ((-distanceFromPlayer / 100f) + 1f);
            }
        }
    }

    public class GrenadeClassContusionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GrenadeClass).GetMethod("get_Contusion");
        }

        [PatchPrefix]
        static bool PreFix(GrenadeClass __instance, ref Vector3 __result)
        {
            ThrowableWeaponClass grenade = __instance.Template as ThrowableWeaponClass;
            Vector3 contusionVect = grenade.Contusion;
            float intensity = contusionVect.z * (1f - ((1f - DeafeningController.EarProtectionFactor) * 1.3f));
            float distance = contusionVect.y * 2f * DeafeningController.EarProtectionFactor;
            intensity = PlayerProperties.EnviroType == EnvironmentType.Indoor ? intensity * 1.7f : intensity;
            distance = PlayerProperties.EnviroType == EnvironmentType.Indoor ? distance * 1.7f : distance;
            __result = new Vector3(contusionVect.x, distance, intensity);
            return false;
        }
    }



}
