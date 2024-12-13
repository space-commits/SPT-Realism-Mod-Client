using Audio.AmbientSubsystem;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;
using CompressorTemplateClass = GClass2918; //SetCompressor
using HeadsetClass = HeadphonesItemClass; //Updatephonesreally()
using HeadsetTemplate = HeadphonesTemplateClass; //SetCompressor
using LootItemClass = GClass2981;

namespace RealismMod
{
    public class FireratePitchPatch : ModulePatch
    {
        private static FieldInfo _playerField;

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            return typeof(Player.FirearmController).GetMethod("method_57", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(Player.FirearmController __instance, ref float __result)
        {
            Player player = (Player)_playerField.GetValue(__instance);
            if (!player.IsYourPlayer) return true;
            if (__instance.Weapon == null)
            {
                __result = 1f;
            }

            float result;
            if (__instance.Weapon.FireMode.FireMode == Weapon.EFireMode.fullauto || __instance.Weapon.FireMode.FireMode == Weapon.EFireMode.burst)
            {
                float firerateMulti = Mathf.Pow(WeaponStats.AutoFireRateDelta, 3f);
                float overHeatMulti = 1f + Mathf.Pow(__instance.Weapon.MalfState.LastShotOverheat / 100f, 2f);
                result = (firerateMulti * overHeatMulti) + UnityEngine.Random.Range(-0.015f, 0.015f);
                result = Mathf.Clamp(result, 0.75f, 1.25f);
            }
            else
            {
                result = 1f + UnityEngine.Random.Range(-0.03f, 0.03f);
            }
            __result = result;

            return false;
        }
    }

    public class PlayPhrasePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PhraseSpeakerClass).GetMethod("Play", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(PhraseSpeakerClass __instance, EPhraseTrigger trigger, ETagStatus tags)
        {
            Player player = Utils.GetYourPlayer();
            if (player == null) return true;
            PhraseSpeakerClass speaker = player.Speaker;
            if (speaker == null) return true;
            if (speaker == __instance && GearController.HasGasMask && (trigger == EPhraseTrigger.OnBreath || ((tags & ETagStatus.Dying) == ETagStatus.Dying)))
            {
                return false;
            }
            return true;
        }
    }

    public class ADSAudioPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("method_50", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(Player __instance, ref float volume)
        {
            if (!StanceController.IsIdle() && StanceController.IsAiming)
            {
                return false;
            }
            volume *= PluginConfig.ADSVolume.Value;
            return true;
        }
    }

    public class CovertEquipmentVolumePatch : ModulePatch
    {
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(MovementContext), "_player");
            return typeof(MovementContext).GetMethod("get_CovertEquipmentNoise", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(MovementContext __instance, ref float __result)
        {
            Player player = (Player)playerField.GetValue(__instance);
            float configFactor = player.IsYourPlayer ? PluginConfig.PlayerMovementVolume.Value : PluginConfig.NPCMovementVolume.Value;
            __result = (1f + __instance.Overweight - player.Skills.CovertMovementEquipment) * configFactor;

            return false;
        }
    }

    public class CovertMovementVolumeBySpeedPatch : ModulePatch
    {
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(MovementContext), "_player");
            return typeof(MovementContext).GetMethod("get_CovertMovementVolumeBySpeed", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(MovementContext __instance, ref float __result)
        {
            Player player = (Player)playerField.GetValue(__instance);
            float configFactor = player.IsYourPlayer ? PluginConfig.PlayerMovementVolume.Value : PluginConfig.NPCMovementVolume.Value;
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
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(MovementContext), "_player");
            return typeof(MovementContext).GetMethod("get_CovertMovementVolume", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(MovementContext __instance, ref float __result)
        {
            Player player = (Player)playerField.GetValue(__instance);
            float configFactor = player.IsYourPlayer ? PluginConfig.PlayerMovementVolume.Value : PluginConfig.NPCMovementVolume.Value;
            if (!__instance.SoftSurface)
            {
                __result = (1f - player.Skills.CovertMovementLoud) * configFactor;
            }
            __result = (1f - player.Skills.CovertMovementSoundVolume) * configFactor;

            return false;
        }
    }

    public class PrismEffectsEnablePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PrismEffects).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostFix(PrismEffects __instance)
        {
            if (__instance.gameObject.name == "FPS Camera") 
            {
                if (DeafeningController.PrismEffects == __instance)
                {
                    return;
                }
                DeafeningController.PrismEffects = __instance;
            }
        }
    }

    public class PrismEffectsDisablePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PrismEffects).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostFix(PrismEffects __instance)
        {
            if (__instance.gameObject.name == "FPS Camera" && DeafeningController.PrismEffects == __instance)
            {
                DeafeningController.PrismEffects = null;
            }
        }
    }


    public class UpdatePhonesPatch : ModulePatch
    {
        private static float HelmDeafFactor(InventoryEquipment equipment)
        {
            string deafStrength = "None";
            LootItemClass headwearItem = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as LootItemClass;

            if (headwearItem != null)
            {
                ArmorItemClass helmet = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as ArmorItemClass; // Not confident on this
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

        private static float HeadsetDeafFactor(InventoryEquipment equipment)
        {
            float protectionFactor;

            LootItemClass headwear = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as LootItemClass;
            HeadsetClass headset = (equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem as HeadsetClass) ?? ((headwear != null) ? headwear.GetAllItemsFromCollection().OfType<HeadsetClass>().FirstOrDefault<HeadsetClass>() : null);

            if (headset != null)
            {
                DeafeningController.HasHeadSet = true;
                HeadsetTemplate headphone = headset.Template;
                protectionFactor = Mathf.Clamp(((headphone.DryVolume / 100f) + 1f) * 1.5f, 0.4f, 1f);
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
            return typeof(Player).GetMethod("UpdatePhones", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            if (__instance.IsYourPlayer)
            {
                DeafeningController.EarProtectionFactor = HeadsetDeafFactor(__instance.Equipment) * HelmDeafFactor(__instance.Equipment);
            }

        }
    }

    public class SetCompressorPatch : ModulePatch
    {
        private static FieldInfo volumeField;

        protected override MethodBase GetTargetMethod()
        {
            volumeField = AccessTools.Field(typeof(BetterAudio), "float_0");
            return typeof(BetterAudio).GetMethod("SetCompressor", BindingFlags.Instance | BindingFlags.Public);

        }
        [PatchPrefix]
        private static bool Prefix(HeadsetTemplate template, BetterAudio __instance)
        { 

            bool hasHeadset = template != null && template?._id != null;
            float gunT;
            float mainT;

            GlobalEventHandlerClass.CreateEvent<CompressorTemplateClass>().Invoke(template);
            DeafeningController.DryVolume = hasHeadset ? template.DryVolume * PluginConfig.DryVolumeMulti.Value : 0f;
            DeafeningController.CompressorVolume = hasHeadset ? template.CompressorGain : -80f; // Not sure about this, CompressorVolume is gone
            DeafeningController.AmbientVolume = hasHeadset ? template.AmbientVolume : 0f;
            DeafeningController.AmbientOccluded = hasHeadset ? (template.AmbientVolume - 15f) : -5f;
            DeafeningController.GunsVolume = hasHeadset ? (template.DryVolume * PluginConfig.DryVolumeMulti.Value) + PluginConfig.GunshotVolume.Value : PluginConfig.GunshotVolume.Value;
            DeafeningController.CompressorLowpass = hasHeadset ? template.LowpassFreq : 2200;
            DeafeningController.CompressorDistortion = hasHeadset ? template.Distortion : 0.277f;
            DeafeningController.CompressorResonance = hasHeadset ? template.HighpassResonance : 2.47f;

            __instance.Master.GetFloat(__instance.AudioMixerData.GunsMixerTinnitusSendLevel, out gunT);
            __instance.Master.GetFloat(__instance.AudioMixerData.MainMixerTinnitusSendLevel, out mainT);
            __instance.Master.SetFloat(__instance.AudioMixerData.GunsMixerTinnitusSendLevel, gunT);
            __instance.Master.SetFloat(__instance.AudioMixerData.MainMixerTinnitusSendLevel, mainT);
            //__instance.Master.SetFloat(__instance.AudioMixerData.CompressorMixerVolume, hasHeadset ? template.CompressorGain : -80f);
            //__instance.Master.SetFloat(__instance.AudioMixerData.OcclusionMixerVolume, hasHeadset ? template.DryVolume : 0f); // TODO Most of this no longer exists
            //__instance.Master.SetFloat(__instance.AudioMixerData.EnvironmentMixerVolume, hasHeadset ? template.DryVolume : 0f);
            //__instance.Master.SetFloat(__instance.AudioMixerData.AmbientMixerVolume, hasHeadset ? template.AmbientVolume : 0f);
            //__instance.Master.SetFloat(__instance.AudioMixerData.AmbientMixerOcclusionSendLevel, hasHeadset ? (template.AmbientVolume - 15f) : -5f);
            //__instance.Master.SetFloat(__instance.AudioMixerData.ReverbMixerVolume, hasHeadset ? template.ReverbVolume : -20f);

            volumeField.SetValue(__instance, (hasHeadset ? DeafeningController.DryVolume : 0f));
            float dryVolume = (float)volumeField.GetValue(__instance);
            __instance.Master.SetFloat(__instance.AudioMixerData.GunsMixerVolume, dryVolume);

            if (!hasHeadset)
            {
                return false;
            }

            //__instance.Master.SetFloat(__instance.AudioMixerData.CompressorAttack, PluginConfig.HeadsetAttack.Value);
            //__instance.Master.SetFloat(__instance.AudioMixerData.CompressorGain, template.CompressorGain);
            //__instance.Master.SetFloat(__instance.AudioMixerData.CompressorRelease, template.CompressorRelease);
            //__instance.Master.SetFloat(__instance.AudioMixerData.CompressorThreshold, template.CompressorTreshold + PluginConfig.HeadsetThreshold.Value);
            //__instance.Master.SetFloat(__instance.AudioMixerData.CompressorDistortion, template.Distortion);
            //__instance.Master.SetFloat(__instance.AudioMixerData.CompressorResonance, template.HighpassResonance);
            //__instance.Master.SetFloat(__instance.AudioMixerData.CompressorCutoff, (float)template.CutoffFreq);
            //__instance.Master.SetFloat(__instance.AudioMixerData.CompressorLowpass, (float)template.LowpassFreq);
            //__instance.Master.SetFloat(__instance.AudioMixerData.CompressorHighFrequenciesGain, template.HighFrequenciesGain);

            return false;
        }
    }

    //this can't be good for performance...
    public class RegisterShotPatch : ModulePatch
    {
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            return typeof(Player.FirearmController).GetMethod("RegisterShot", BindingFlags.Instance | BindingFlags.Public);
        }

        /*        private static float GetMuzzleLoudness(IEnumerable<Mod> mods)
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

                }*/

        private static float CalcAmmoFactor(float ammoRec)
        {
            return Math.Min((ammoRec / 100f) + 1f, 2f);
        }

        private static float CalcVelocityFactor(float speedFactor)
        {
            return ((speedFactor - 1f) * -2f) + 1f;
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance, EftBulletClass shot)
        {
            Player player = (Player)playerField.GetValue(__instance);

            IWeapon iWeap = __instance.Weapon as IWeapon;
            if (!iWeap.IsUnderbarrelWeapon)
            {
                Weapon weap = __instance.Weapon;
                if (player.IsYourPlayer == true)
                {
                    float velocityFactor = CalcVelocityFactor(weap.SpeedFactor);
                    float ammoFactor = CalcAmmoFactor((shot.Ammo as AmmoItemClass).ammoRec);
                    float deafenFactor = velocityFactor * ammoFactor;

             /*       if (shot.InitialSpeed * weap.SpeedFactor <= 335f)
                    {
                        deafenFactor *= 0.6f;
                    }*/

                    DeafeningController.AmmoDeafFactor = deafenFactor == 0f ? 1f : deafenFactor;
                }
                else
                {
                    Vector3 playerPos = Singleton<GameWorld>.Instance.MainPlayer.Transform.position;
                    Vector3 shooterPos = player.Transform.position;
                    float distanceFromPlayer = Vector3.Distance(shooterPos, playerPos);
                    if (distanceFromPlayer <= 15f)
                    {
                        DeafeningController.IsBotFiring = true;;
                        float velocityFactor = CalcVelocityFactor(weap.SpeedFactor);
                        float muzzleFactor = __instance.IsSilenced ? 0.4f : 1f;
                        float calFactor = StatCalc.CaliberLoudnessFactor(weap.AmmoCaliber);
                        if (shot.InitialSpeed * weap.SpeedFactor <= 335f)
                        {
                            velocityFactor *= 0.6f;
                        }
                        float totalBotDeafFactor = 1f * calFactor * velocityFactor;
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
                DeafeningController.GrenadeExploded = true;
                DeafeningController.GrenadeDeafFactor = grenadeItem.Contusion.z * ((-distanceFromPlayer / 100f) + 1f);
            }
        }
    }

    public class GrenadeClassContusionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass2848).GetMethod("get_Contusion"); // Formally grenade class
        }

        [PatchPrefix]
        static bool PreFix(GClass2848 __instance, ref Vector3 __result)
        {
            ThrowWeapItemClass grenade = __instance.Template as ThrowWeapItemClass;
            Vector3 contusionVect = grenade.Contusion;
            float intensity = contusionVect.z * (1f - ((1f - DeafeningController.EarProtectionFactor) * 1.3f));
            float distance = contusionVect.y * 2f * DeafeningController.EarProtectionFactor;
            intensity = PlayerState.EnviroType == EnvironmentType.Indoor ? intensity * 1.7f : intensity;
            distance = PlayerState.EnviroType == EnvironmentType.Indoor ? distance * 1.7f : distance;
            __result = new Vector3(contusionVect.x, distance, intensity);
            return false;
        }
    }



}
