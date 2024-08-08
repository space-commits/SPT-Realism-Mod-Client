using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Reflection;
using Systems.Effects;
using UnityEngine;
using static EFT.InventoryLogic.Weapon;
using DamageTypeClass = GClass2470;
using MalfGlobals = BackendConfigSettingsClass.GClass1378;
using OverheatGlobals = BackendConfigSettingsClass.GClass1379;

namespace RealismMod
{
    public class GetMalfunctionStatePatch : ModulePatch
    {
        private static FieldInfo playerField;
        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            return typeof(Player.FirearmController).GetMethod("GetMalfunctionState", BindingFlags.Instance | BindingFlags.Public);
        }

        private static void ExplodeWeapon(Player.FirearmController fc, Player player) 
        {
            Singleton<Effects>.Instance.EmitGrenade("Grenade_new2", fc.CurrentFireport.Original.position, Vector3.up, 1f);
            fc.Weapon.Repairable.Durability = 0f;
            fc.Weapon.Repairable.MaxDurability = 0f;

            if (player.ActiveHealthController != null)
            {
                if (player.IsYourPlayer)
                {
                    Plugin.RealHealthController.AddBasesEFTEffect(player, "Contusion", EBodyPart.Head, 0f, 10f, 5f, 1f);
                    Plugin.RealHealthController.AddBasesEFTEffect(player, "TunnelVision", EBodyPart.Head, 0f, 10f, 5f, 1f);
                    Plugin.RealHealthController.AddBasesEFTEffect(player, "Tremor", EBodyPart.Head, 0f, 10f, 5f, 1f);
                    Plugin.RealHealthController.AddBasesEFTEffect(player, "LightBleeding", EBodyPart.Head, null, null, null, null);
                    Plugin.RealHealthController.AddBasesEFTEffect(player, "LightBleeding", EBodyPart.RightArm, null, null, null, null);
                }
                player.ActiveHealthController.ApplyDamage(EBodyPart.Head, UnityEngine.Random.Range(5, 21), DamageTypeClass.Existence);
                player.ActiveHealthController.ApplyDamage(EBodyPart.RightArm, UnityEngine.Random.Range(20, 61), DamageTypeClass.Existence);

                InventoryControllerClass inventoryController = (InventoryControllerClass)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(player);
                if (fc.Item != null && inventoryController.CanThrow(fc.Item))
                {
                    inventoryController.TryThrowItem(fc.Item, null, false);
                }

            }
        }

        [PatchPostfix]
        private static void Postfix(Player.FirearmController __instance, ref Weapon.EMalfunctionState __result, BulletClass ammoToFire)
        {
         
            Player player = (Player)playerField.GetValue(__instance);

            bool do9x18Explodey = false;
            bool isPMMAmmo = ammoToFire.Template._id == "57371aab2459775a77142f22";
            float weaponDurability = __instance.Weapon.Repairable.MaxDurability;
            if (__instance.Weapon.AmmoCaliber == "9x18PM" && isPMMAmmo) 
            {
                if (isPMMAmmo)
                {
                    __instance.Weapon.Repairable.Durability = Mathf.Max(__instance.Weapon.Repairable.Durability - (__instance.Weapon.DurabilityBurnRatio * ammoToFire.DurabilityBurnModificator), 0f);
                }
                int rnd = UnityEngine.Random.Range(1, 11);
                float dura = 2f - (__instance.Weapon.Repairable.Durability / __instance.Weapon.Repairable.MaxDurability);
                do9x18Explodey = __instance.Weapon.Repairable.Durability <= 75f && rnd <= 4 * dura && isPMMAmmo;
            }

            if (__instance.Weapon.AmmoCaliber != ammoToFire.Caliber || __instance.Weapon.Repairable.MaxDurability <= 0)
            {
             
                bool explosiveMismatch = do9x18Explodey || (__instance.Weapon.AmmoCaliber == "366TKM" && ammoToFire.Caliber == "762x39") || (__instance.Weapon.AmmoCaliber == "556x45NATO" && ammoToFire.Caliber == "762x35") || (__instance.Weapon.AmmoCaliber == "762x51" && ammoToFire.Caliber == "68x51");
                bool malfMismatch = (__instance.Weapon.AmmoCaliber == "762x39" && ammoToFire.Caliber == "366TKM") || (__instance.Weapon.AmmoCaliber == "762x35" && ammoToFire.Caliber == "556x45NATO") || (__instance.Weapon.AmmoCaliber == "68x51" && ammoToFire.Caliber == "762x51");

                if (player.IsYourPlayer)
                {
                    if (weaponDurability <= 0f || malfMismatch || (explosiveMismatch && !Plugin.ServerConfig.malf_changes))
                    {
                        if (weaponDurability <= 0f) NotificationManagerClass.DisplayWarningNotification("Weapon Is Broken Beyond Repair", EFT.Communications.ENotificationDurationType.Long);
                        else NotificationManagerClass.DisplayWarningNotification("Wrong Ammo/Weapon Caliber Combination Or Weapon Is Broken", EFT.Communications.ENotificationDurationType.Long);
                        __result = Weapon.EMalfunctionState.Misfire;
                        return;
                    }

                    if (explosiveMismatch)
                    {
                        NotificationManagerClass.DisplayWarningNotification("Catastrophic Failure. Wrong Ammo/Weapon Caliber Combination", EFT.Communications.ENotificationDurationType.Long);
                        ExplodeWeapon(__instance, player);
                    }
                }
                else
                {
                    if (__instance.Weapon.Repairable.MaxDurability <= 0f || malfMismatch || (explosiveMismatch && !Plugin.ServerConfig.malf_changes))
                    {
                        __result = Weapon.EMalfunctionState.Misfire;
                        return;
                    }
                    if (explosiveMismatch)
                    {
                        ExplodeWeapon(__instance, player);
                    }
    
                }
            }
        }
    }


    public class GetDurabilityLossOnShotPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("GetDurabilityLossOnShot", BindingFlags.Instance | BindingFlags.Public);

        }

        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref float __result, float ammoBurnRatio, float overheatFactor, float skillWeaponTreatmentFactor, out float modsBurnRatio)
        {
            if (__instance?.Owner?.ID != null && __instance.Owner.ID == Singleton<GameWorld>.Instance.MainPlayer.ProfileId)
            {
                modsBurnRatio = WeaponStats.TotalModDuraBurn;
                __result = (float)__instance.Repairable.TemplateDurability / __instance.Template.OperatingResource * __instance.DurabilityBurnRatio * (modsBurnRatio * ammoBurnRatio) * overheatFactor * (1f - skillWeaponTreatmentFactor);
                return false;
            }
            else
            {
                modsBurnRatio = 1f;
                return true;
            }
        }
    }

    public class GetTotalMalfunctionChancePatch : ModulePatch
    {
        private static FieldInfo playerField;
        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            return typeof(Player.FirearmController).GetMethod("GetTotalMalfunctionChance", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPrefix]
        private static bool Prefix(ref float __result, BulletClass ammoToFire, Player.FirearmController __instance, float overheat, out double durabilityMalfChance, out float magMalfChance, out float ammoMalfChance, out float overheatMalfChance, out float weaponDurability)
        {
            Player player = (Player)playerField.GetValue(__instance);

            if (player.IsYourPlayer == true)
            {
                durabilityMalfChance = 0.0;
                magMalfChance = 0f;
                ammoMalfChance = 0f;
                overheatMalfChance = 0f;
                weaponDurability = 0f;

                if (!__instance.Item.AllowMalfunction)
                {
                    __result = 0f;
                    return false;
                }

                if (__instance.Weapon.AmmoCaliber == "762x35" && ammoToFire.Caliber == "556x45NATO")
                {
                    __result = 10000f;
                    return false;
                }

                /*                float ammoHotnessFactor = (1f - ((ammoToFire.ammoRec / 200f) + 1f)) + 1f;*/

                float malfDelta = Mathf.Min(WeaponStats.MalfChanceDelta * 3, 0.99f);
                float subFactor = 1f;
     
                BackendConfigSettingsClass instance = Singleton<BackendConfigSettingsClass>.Instance;
                MalfGlobals malfunctionGlobals = instance.Malfunction;
                OverheatGlobals overheatGlobals = instance.Overheat;
                float ammoMalfChanceMult = malfunctionGlobals.AmmoMalfChanceMult;
                float magazineMalfChanceMult = malfunctionGlobals.MagazineMalfChanceMult;
                MagazineClass currentMagazine = __instance.Item.GetCurrentMagazine();
                magMalfChance = ((currentMagazine == null) ? 0f : (currentMagazine.MalfunctionChance * magazineMalfChanceMult));
                ammoMalfChance = ((ammoToFire != null) ? ((ammoToFire.MalfMisfireChance + ammoToFire.MalfFeedChance) * ammoMalfChanceMult) : 0f);
                float weaponMalfChance = WeaponStats.TotalMalfChance;
                float durability = __instance.Item.Repairable.Durability / (float)__instance.Item.Repairable.TemplateDurability * 100f;
                weaponDurability = Mathf.Floor(durability);

                if (weaponDurability >= PluginConfig.DuraMalfThreshold.Value)
                {
                    magMalfChance *= 0.25f;
                    weaponMalfChance *= 0.25f;
                    ammoMalfChance *= 0.25f;
                }
                if (overheat >= overheatGlobals.OverheatProblemsStart)
                {
                    overheatMalfChance = Mathf.Lerp(overheatGlobals.MinMalfChance, overheatGlobals.MaxMalfChance, (overheat - overheatGlobals.OverheatProblemsStart) / (overheatGlobals.MaxOverheat - overheatGlobals.OverheatProblemsStart));
                }
                overheatMalfChance *= (float)__instance.Item.Buff.MalfunctionProtections;

                if (weaponDurability >= 50)
                {
                    durabilityMalfChance = ((Math.Pow((double)(weaponMalfChance + 1f), 3.0 + (double)(100f - weaponDurability) / (20.0 - 10.0 / Math.Pow((double)__instance.Item.FireRate / 10.0, 0.322))) - 1.0) / 1000.0);
                }
                else
                {
                    durabilityMalfChance = (Math.Pow((double)(weaponMalfChance + 1f), Math.Log10(Math.Pow((double)(101f - weaponDurability), (50.0 - Math.Pow((double)weaponDurability, 1.286) / 4.8) / (Math.Pow((double)__instance.Item.FireRate, 0.17) / 2.9815 + 2.1)))) - 1.0) / 1000.0;
                }
                if (weaponDurability >= PluginConfig.DuraMalfThreshold.Value)
                {
                    durabilityMalfChance *= 0.25f;
                }

                if (!WeaponStats.CanCycleSubs && ammoToFire.ammoHear == 1)
                {
                    float suppFactor = __instance.IsSilenced || WeaponStats.HasBooster ? 0.25f : 1f;
                    if (ammoToFire.Caliber == "762x39")
                    {
                        subFactor = 3000f * (1f - malfDelta) * suppFactor;
                    }
                    else
                    {
                        subFactor = 3500f * (1f - malfDelta) * suppFactor;
                    }
                }

                durabilityMalfChance *= subFactor * __instance.Item.Buff.MalfunctionProtections; //* WeaponStats.FireRateDelta
                durabilityMalfChance = Mathf.Clamp01((float)durabilityMalfChance);
                float totalMalfChance = Mathf.Clamp01((float)Math.Round(durabilityMalfChance + ((ammoMalfChance + magMalfChance + overheatMalfChance) / 500f), 5));

                __result = totalMalfChance;
                return false;
            }
            else
            {
                durabilityMalfChance = 0.0;
                magMalfChance = 0f;
                ammoMalfChance = 0f;
                overheatMalfChance = 0f;
                weaponDurability = 0f;
                return true;
            }
        }
    }

    public class IsKnownMalfTypePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(WeaponMalfunctionStateClass).GetMethod("IsKnownMalfType", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref bool __result)
        {
            __result = true;
        }
    }
}
