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
/*using CompressorTemplateClass = GClass2918; //SetCompressor
using HeadsetClass = GClass2654; //Updatephonesreally()
using HeadsetTemplate = GClass2556; //SetCompressor*/

namespace RealismMod
{
    //gunshot volume patch
    public class GunshotVolumePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass859).GetMethod("Enqueue");
        }

        [PatchPrefix]
        static void PatchPrefix(GClass859 __instance, ref float volume)
        {
            volume *= PluginConfig.GunshotVolume.Value;
        }
    }

    //change firerate sfx pitch based on different factors
    public class FireratePitchPatch : ModulePatch
    {
        private static FieldInfo _playerField;

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            return typeof(Player.FirearmController).GetMethod("method_60", BindingFlags.Instance | BindingFlags.Public);
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

    //muffle player audio when wearing a gas mask
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

    //adjust the volum of ADS, don't play if ADS from stance to prevent doubling up of ADS audio
    public class ADSAudioPatch : ModulePatch
    {
        private static FieldInfo _weaponSoundPlayerField;

        protected override MethodBase GetTargetMethod()
        {
            _weaponSoundPlayerField = AccessTools.Field(typeof(Player.FirearmController), "weaponSoundPlayer_0");
            return typeof(Player.FirearmController).GetMethod("method_59", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(Player.FirearmController __instance)
        {
            if (!StanceController.IsIdle() && StanceController.IsAiming) return false;
            float volume = __instance.CalculateAimingSoundVolume() * PluginConfig.ADSVolume.Value;
            var soundPlayer = (WeaponSoundPlayer)_weaponSoundPlayerField.GetValue(__instance);
            soundPlayer.PlayAimingSound(volume);
            return false;
        }
    }

    //adjust the volume of player + NPC movement
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

    //adjust the volume of player + NPC movement
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

    //adjust the volume of player + NPC movement
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

    //grab reference for EFT's player camera visual effects class for things like tunnel vision
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
                if (DeafenController.PrismEffects == __instance)
                {
                    return;
                }
                DeafenController.PrismEffects = __instance;
            }
        }
    }

    //properly depose of PrismEffects reference
    public class PrismEffectsDisablePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PrismEffects).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostFix(PrismEffects __instance)
        {
            if (__instance.gameObject.name == "FPS Camera" && DeafenController.PrismEffects == __instance)
            {
                DeafenController.PrismEffects = null;
            }
        }
    }


    public class UpdatePhonesPatch : ModulePatch
    {
        private static float GetHelmetProtection(InventoryEquipment equipment)
        {
            EDeafStrength deafStrength = EDeafStrength.None;
            CompoundItem headwearItem = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as CompoundItem;

            if (headwearItem != null)
            {
                ArmorItemClass helmet = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as ArmorItemClass;
                if (helmet?.Armor?.Deaf != null)
                {
                    deafStrength = helmet.Armor.Deaf;
                }

            }
            return HelmeProtectionFactor(deafStrength);
        }

        private static float HelmeProtectionFactor(EDeafStrength deafStr)
        {
            switch (deafStr)
            {
                case EDeafStrength.Low:
                    return DeafenController.LightHelmetDeafReduction;
                case EDeafStrength.High:
                    return DeafenController.HeavyHelmetDeafreduction;
                default:
                    return 1f;
            }
        }

        private static float GetHeadsetProtection(InventoryEquipment equipment)
        {
            CompoundItem headwear = equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as CompoundItem;
            HeadphonesItemClass headset = (equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem as HeadphonesItemClass) ?? ((headwear != null) ? headwear.GetAllItemsFromCollection().OfType<HeadphonesItemClass>().FirstOrDefault<HeadphonesItemClass>() : null);
            if (headset != null)
            {
                DeafenController.HasHeadSet = true;
                HeadphonesTemplateClass headphone = headset.Template;
                float rating = Stats.GearStats[headset.TemplateId].dB;
                return Utils.CalcultateModifierFromRange(rating, 19f, 26f, DeafenController.MaxHeadsetProtection, DeafenController.MinHeadsetProtection);
            }
            else
            {
                DeafenController.HasHeadSet = false;
                return 1f;
            }
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
                DeafenController.EarProtectionFactor = GetHeadsetProtection(__instance.Equipment) * GetHelmetProtection(__instance.Equipment);
            }

        }
    }

    //this can't be good for performance...
    //but anyway, adjust, calculate "loudness" of a bot's shot when near the player for deafening
    public class RegisterShotPatch : ModulePatch
    {
        private const float SuppressorWithSubsFactor = 0.25f;
        private const float SubsonicFactor = 0.85f;
        private const float SpeedOfSound = 335f;

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
            return Mathf.Clamp((ammoRec / 100f) + 1f, 0.8f, 2f);
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

                    if (shot.InitialSpeed * weap.SpeedFactor <= SpeedOfSound)
                    {
                        deafenFactor *= __instance.IsSilenced ? SuppressorWithSubsFactor : SubsonicFactor;
                    }

                    DeafenController.AmmoDeafFactor = Mathf.Clamp(deafenFactor, 0.5f, 2f);
                }
                else
                {
                    Vector3 playerPos = Singleton<GameWorld>.Instance.MainPlayer.Transform.position;
                    Vector3 shooterPos = player.Transform.position;
                    float distanceFromPlayer = Vector3.Distance(shooterPos, playerPos);
                    if (distanceFromPlayer <= 15f)
                    {
                  
                        float velocityFactor = CalcVelocityFactor(weap.SpeedFactor);
                        float calFactor = StatCalc.CaliberLoudnessFactor(weap.AmmoCaliber);
                        if (shot.InitialSpeed * weap.SpeedFactor <= SpeedOfSound)
                        {
                            velocityFactor *= __instance.IsSilenced ? SuppressorWithSubsFactor : SubsonicFactor;
                        }
                        float baseBotDeafFactor = calFactor * velocityFactor;
                        float totalBotDeafFactor = baseBotDeafFactor * ((-distanceFromPlayer / 100f) + 1f) * 1.25f;
                        DeafenController.BotFiringDeafFactor = Mathf.Clamp(totalBotDeafFactor, 1f, 5f);
                        DeafenController.IsBotFiring = true;
                        DeafenController.BotTimer = 0f;
                        DeafenController.IncreaseDeafeningShotAt();
                    }
                }
            }
        }
    }

    //get explosions near player for deafening
    public class ExplosionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Grenade).GetMethod("Explosion", BindingFlags.Static | BindingFlags.Public);
        }

        [PatchPrefix]
        static void PreFix(IExplosiveItem grenadeItem, Vector3 grenadePosition)
        {
            Player player = Singleton<GameWorld>.Instance.MainPlayer;
            float distanceFromPlayer = Vector3.Distance(grenadePosition, player.Transform.position);

            if (distanceFromPlayer <= 15f)
            {
                DeafenController.GrenadeExploded = true;
                DeafenController.GrenadeTimer = 0f;
                DeafenController.ExplosionDeafFactor = grenadeItem.Contusion.z * ((-distanceFromPlayer / 100f) + 1f);
                Logger.LogWarning($"grenadeItem.Contusion.z {grenadeItem.Contusion.z}, distance from player {distanceFromPlayer}");
                DeafenController.IncreaseDeafeningExplosion();
            }
        }
    }

    //adjust the concussive effect of grenades
    public class GrenadeClassContusionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ThrowWeapItemClass).GetMethod("get_Contusion");
        }

        [PatchPrefix]
        static bool PreFix(ThrowWeapItemClass __instance, ref Vector3 __result)
        {
            Vector3 contusionVect = __instance.GetTemplate<ThrowWeapTemplateClass>().Contusion;
            float intensity = contusionVect.z * (1f - ((1f - DeafenController.EarProtectionFactor) * 1.3f));
            float distance = contusionVect.y * 2f * DeafenController.EarProtectionFactor;
            intensity = PlayerValues.EnviroType == EnvironmentType.Indoor ? intensity * 1.7f : intensity;
            distance = PlayerValues.EnviroType == EnvironmentType.Indoor ? distance * 1.7f : distance;
            __result = new Vector3(contusionVect.x, distance, intensity);
            return false;
        }
    }
}
