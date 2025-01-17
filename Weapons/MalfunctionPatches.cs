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
using DamageTypeClass = GClass2788;
using MalfGlobals = BackendConfigSettingsClass.GClass1521;
using OverheatGlobals = BackendConfigSettingsClass.GClass1522;
using EFT.HealthSystem;

namespace RealismMod
{
    //bosses can induce malfunctions, unrealistic
    public class RemoveSillyBossForcedMalf : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ActiveHealthController).GetMethod("AddMisfireEffect", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(ActiveHealthController __instance)
        {
            return false;
        }
    }

    //handle caliber incompatibilities and related mechanics
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

                InventoryController inventoryController = (InventoryController)AccessTools.Field(typeof(Player), "_inventoryController").GetValue(player);
                if (fc.Item != null && inventoryController.CanThrow(fc.Item))
                {
                    inventoryController.TryThrowItem(fc.Item, null, false);
                }

            }
        }

        [PatchPostfix]
        private static void Postfix(Player.FirearmController __instance, ref Weapon.EMalfunctionState __result, AmmoItemClass ammoToFire)
        {
         
            Player player = (Player)playerField.GetValue(__instance);

            bool do9x18Explodey = false;
            bool isPMMAmmo = ammoToFire.Template._id == "57371aab2459775a77142f22";
            float weaponMaxDurability = __instance.Weapon.Repairable.MaxDurability;
            float weaponCurrentDurability = __instance.Weapon.Repairable.Durability;
            if (__instance.Weapon.AmmoCaliber == "9x18PM" && isPMMAmmo) 
            {
                if (isPMMAmmo)
                {
                    __instance.Weapon.Repairable.Durability = Mathf.Max(__instance.Weapon.Repairable.Durability - (__instance.Weapon.DurabilityBurnRatio * ammoToFire.DurabilityBurnModificator), 0f);
                }
                int rnd = UnityEngine.Random.Range(1, 11);
                float dura = 2f - (__instance.Weapon.Repairable.Durability / __instance.Weapon.Repairable.MaxDurability);
                do9x18Explodey = __instance.Weapon.Repairable.Durability <= 80f && rnd <= 4 * dura && isPMMAmmo;
            }

            if (__instance.Weapon.AmmoCaliber != ammoToFire.Caliber || __instance.Weapon.Repairable.MaxDurability <= 0)
            {
             
                bool explosiveMismatch = do9x18Explodey || (__instance.Weapon.AmmoCaliber == "366TKM" && ammoToFire.Caliber == "762x39") || (__instance.Weapon.AmmoCaliber == "556x45NATO" && ammoToFire.Caliber == "762x35") || (__instance.Weapon.AmmoCaliber == "762x51" && ammoToFire.Caliber == "68x51");
                bool malfMismatch = (__instance.Weapon.AmmoCaliber == "762x39" && ammoToFire.Caliber == "366TKM") || (__instance.Weapon.AmmoCaliber == "762x35" && ammoToFire.Caliber == "556x45NATO") || (__instance.Weapon.AmmoCaliber == "68x51" && ammoToFire.Caliber == "762x51");

                if (player.IsYourPlayer)
                {
                    if (weaponCurrentDurability <= 0f || malfMismatch || (explosiveMismatch && !Plugin.ServerConfig.malf_changes))
                    {
                        if (weaponCurrentDurability <= 0f) NotificationManagerClass.DisplayWarningNotification("Weapon Is Broken Beyond Repair", EFT.Communications.ENotificationDurationType.Long);
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
                    if (weaponCurrentDurability <= 0f || malfMismatch || (explosiveMismatch && !Plugin.ServerConfig.malf_changes))
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

    //allows additional factors to modify weapon durability burn
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

    //replaced BSG's malfunction chance calc with my own for better control
    public class GetTotalMalfunctionChancePatch : ModulePatch
    {
        private static FieldInfo playerField;
        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            return typeof(Player.FirearmController).GetMethod("GetTotalMalfunctionChance", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPrefix]
        private static bool Prefix(ref float __result, AmmoItemClass ammoToFire, Player.FirearmController __instance, float overheat, out double durabilityMalfChance, out float magMalfChance, out float ammoMalfChance, out float overheatMalfChance, out float weaponDurability)
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

                BackendConfigSettingsClass globalSettings = Singleton<BackendConfigSettingsClass>.Instance;
                MalfGlobals malfunctionSettings = globalSettings.Malfunction;
                OverheatGlobals overheatSettings = globalSettings.Overheat;
                float baseWeaponMalfChance = WeaponStats.TotalMalfChance;
                float malfDelta = Mathf.Min(WeaponStats.MalfChanceDelta * 3, 0.99f); //for sub malf chance

                //ammo malf chance
                float globalAmmoMalfMulti = malfunctionSettings.AmmoMalfChanceMult;
                ammoMalfChance = ammoToFire != null ? 1f + ((ammoToFire.MalfMisfireChance + ammoToFire.MalfFeedChance) * globalAmmoMalfMulti) : 1f;

                //mag malf chance
                MagazineItemClass currentMagazine = __instance.Item.GetCurrentMagazine();
                magMalfChance = currentMagazine == null ? 1f : 1f + (currentMagazine.MalfunctionChance * malfunctionSettings.MagazineMalfChanceMult);

                //durability factor
                durabilityMalfChance = 2f - (__instance.Item.Repairable.Durability / (float)__instance.Item.Repairable.TemplateDurability);
                weaponDurability = (__instance.Item.Repairable.Durability / (float)__instance.Item.Repairable.TemplateDurability) * 100f;
         
                //overheat
                overheatMalfChance = 1f + Mathf.Clamp01(overheat / 100f);
                overheatMalfChance = Mathf.Pow(overheatMalfChance, 3f);

                float shotFactor = 1f + (ShootController.ShotCount / 200f);
                float fireRateFactor = ShootController.ShotCount > 2 ? Mathf.Max(WeaponStats.AutoFireRateDelta, 1f) : 1f;

                bool isSubsonic = !WeaponStats.CanCycleSubs && ammoToFire.ammoHear == 1;
                bool hasBooster = __instance.IsSilenced || WeaponStats.HasBooster;

                bool canDoMalfChance = weaponDurability < PluginConfig.DuraMalfThreshold.Value || overheatMalfChance > 1.7f || ShootController.ShotCount > 7f || magMalfChance > 2f || ammoMalfChance > 1.5f || WeaponStats.MalfChanceDelta < -0.5 || baseWeaponMalfChance > 0.004f || isSubsonic;

                if (weaponDurability >= PluginConfig.DuraMalfReductionThreshold.Value)
                {
                    baseWeaponMalfChance *= 0.2f;
                }

                if (weaponDurability >= PluginConfig.DuraMalfReductionThreshold.Value)
                {
                    durabilityMalfChance = Math.Pow(durabilityMalfChance, 1.5);
                }
                else if (weaponDurability >= 60f)
                {
                    durabilityMalfChance = Math.Pow(durabilityMalfChance, 3);
                }
                else
                {
                    durabilityMalfChance = Math.Pow(durabilityMalfChance, 5);
                }

                float subFactor = 0f;
                if (isSubsonic)
                {
                    float suppFactor = hasBooster ? 0.25f : 1f;
                    if (ammoToFire.Caliber == "762x39")
                    {
                        subFactor = 0.04f * suppFactor; // * (1f - malfDelta)
                    }
                    else
                    {
                        subFactor = 0.055f * suppFactor; // * (1f - malfDelta)
                    }
                }

                float totalFactors = (float)durabilityMalfChance * overheatMalfChance * ammoMalfChance * magMalfChance * shotFactor * fireRateFactor * (float)__instance.Item.Buff.MalfunctionProtections;
                float totalMalfChance = (baseWeaponMalfChance + subFactor) * totalFactors;
                totalMalfChance = canDoMalfChance ? totalMalfChance : totalMalfChance * 0.01f;
                __result = totalMalfChance * PluginConfig.MalfMulti.Value;

                if (PluginConfig.EnableBallisticsLogging.Value)
                {
                    Logger.LogWarning("=============");
                    Logger.LogWarning("canDoMalfChance " + canDoMalfChance);
                    Logger.LogWarning("weapon base chance " + baseWeaponMalfChance);
                    Logger.LogWarning("weapon malf delta " + WeaponStats.MalfChanceDelta);
                    Logger.LogWarning("ammo malf chance " + ammoMalfChance);
                    Logger.LogWarning("heat malf chance " + overheatMalfChance);
                    Logger.LogWarning("durability " + weaponDurability);
                    Logger.LogWarning("durability malf chance " + durabilityMalfChance);
                    Logger.LogWarning($"mag malf chance {magMalfChance}");
                    Logger.LogWarning("WeaponStats.FireRateDelta " + WeaponStats.AutoFireRateDelta);
                    Logger.LogWarning("subFactor " + subFactor);
                    Logger.LogWarning("total malf chance " + __result);
                }

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

    public class RemoveForcedMalf : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.AddMisfireEffect));
        }

        [PatchPrefix]
        static bool Prefix(ActiveHealthController __instance)
        {
            return false;
        }
    }
}
