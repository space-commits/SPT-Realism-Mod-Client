/*using EFT;
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
using System.Collections;
using EFT.NextObservedPlayer;

namespace RealismMod
{
    public class CovertEquipmentVolumePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MovementContext).GetMethod("get_CovertEquipmentNoise", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(MovementContext __instance, ref float __result)
        {
            Player player = (Player)AccessTools.Field(typeof(MovementContext), "_player").GetValue(__instance);

            float configFactor = player.IsYourPlayer ? Plugin.PlayerMovementVolume.Value : Plugin.NPCMovementVolume.Value;

            __result = (1f + __instance.Overweight - player.Skills.CovertMovementEquipment) * configFactor;

            return false;
        }
    }

    public class CovertMovementVolumeBySpeedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MovementContext).GetMethod("get_CovertMovementVolumeBySpeed", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(MovementContext __instance, ref float __result)
        {
            Player player = (Player)AccessTools.Field(typeof(MovementContext), "_player").GetValue(__instance);

            float configFactor = player.IsYourPlayer ? Plugin.PlayerMovementVolume.Value : Plugin.NPCMovementVolume.Value;

            if (!__instance.SoftSurface)
            {
                __result = (1f - player.Skills.CovertMovementLoud * __instance.CovertEfficiency) * configFactor;
            }
            __result = (1f - player.Skills.CovertMovementSoundVolume * __instance.CovertEfficiency) * configFactor;

            return false;
        }
    }

    public class CovertMovementVolumePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MovementContext).GetMethod("get_CovertMovementVolume", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(MovementContext __instance, ref float __result)
        {
            Player player = (Player)AccessTools.Field(typeof(MovementContext), "_player").GetValue(__instance);

            float configFactor = player.IsYourPlayer ? Plugin.PlayerMovementVolume.Value : Plugin.NPCMovementVolume.Value;

            if (!__instance.SoftSurface)
            {
                __result = (1f - player.Skills.CovertMovementLoud) * configFactor;
            }
            __result = (1f - player.Skills.CovertMovementSoundVolume) * configFactor;

            return false;
        }
    }

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
                    return 0.95f;
                case "High":
                    return 0.75f;
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
            bool hasHeadset = template != null && template?._id != null;
            float gunT;
            float mainT;

            CompressorClass.CreateEvent<CompressorTemplateClass>().Invoke(template);

            DeafeningController.DryVolume = hasHeadset ? template.DryVolume : 0f;
            DeafeningController.Compressor = hasHeadset ? template.CompressorVolume : -80f;
            DeafeningController.AmbientVolume = hasHeadset ? template.AmbientVolume : 0f;
            DeafeningController.AmbientOccluded = hasHeadset ? (template.AmbientVolume - 15f) : -5f;
            DeafeningController.GunsVolume = hasHeadset ? template.DryVolume + Plugin.GunshotVolume.Value : Plugin.GunshotVolume.Value;

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


            *//* bool hasHeadsetTemplate = template != null;
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
             __instance.Master.SetFloat(__instance.AudioMixerData.MainMixerTinnitusSendLevel, tin2);*//*


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
                      *//*  float muzzleFactor = GetMuzzleLoudness(weap.Mods);*//*
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
*/