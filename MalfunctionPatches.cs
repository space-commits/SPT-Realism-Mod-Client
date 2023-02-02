using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace RealismMod
{

/*    public class RepairAmountPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.UI.RepairerParametersPanel).GetMethod("get_RepairAmount", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostfix(ref float __result)
        {
            Logger.LogWarning("RepairAmount = " + __result);
        }
    }

    public class Single_1Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.UI.RepairerParametersPanel).GetMethod("get_Single_1", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        [PatchPostfix]
        private static void PatchPostfix(ref float __result)
        {
            Logger.LogWarning("RealCurrentDurability = " + __result);
        }
    }*/

    public class GetTotalMalfunctionChancePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("GetTotalMalfunctionChance", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPrefix]
        private static bool Prefix(ref float __result, BulletClass ammoToFire, Player.FirearmController __instance, float overheat, out double durabilityMalfChance, out float magMalfChance, out float ammoMalfChance, out float overheatMalfChance, out float weaponDurability)
        {

            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);

            float ammoHotnessFactor = (1f - ammoToFire.casingMass) + 1f;

            durabilityMalfChance = 0.0;
            magMalfChance = 0f;
            ammoMalfChance = 0f;
            overheatMalfChance = 0f;
            weaponDurability = 0f;

            Logger.LogWarning("Ammo hot factors = " + ammoHotnessFactor);

            Logger.LogWarning("Factored malf = " + WeaponProperties.TotalMalfChance * ammoHotnessFactor);

            if (!player.IsAI)
            {
                if (WeaponProperties.CanCycleSubs == false && ammoToFire.ammoHear == 1)
                {
                    __result = 0.95f;
                    return false;
                }

                if (!__instance.Item.AllowMalfunction)
                {
                    __result = 0f;
                    return false;
                }
                BackendConfigSettingsClass instance = Singleton<BackendConfigSettingsClass>.Instance;
                BackendConfigSettingsClass.GClass1321 malfunction = instance.Malfunction;
                BackendConfigSettingsClass.GClass1322 overheat2 = instance.Overheat;
                BackendConfigSettingsClass.GClass1295 troubleShooting = instance.SkillsSettings.TroubleShooting;
                float ammoMalfChanceMult = malfunction.AmmoMalfChanceMult;
                float magazineMalfChanceMult = malfunction.MagazineMalfChanceMult;
                MagazineClass currentMagazine = __instance.Item.GetCurrentMagazine();
                magMalfChance = ((currentMagazine == null) ? 0f : (currentMagazine.MalfunctionChance * magazineMalfChanceMult));
                ammoMalfChance = ((ammoToFire != null) ? ((ammoToFire.MalfMisfireChance + ammoToFire.MalfFeedChance) * ammoMalfChanceMult) : 0f);
                float num = __instance.Item.Repairable.Durability / (float)__instance.Item.Repairable.TemplateDurability * 100f;
                weaponDurability = Mathf.Floor(num);
                if (overheat >= overheat2.OverheatProblemsStart)
                {
                    overheatMalfChance = Mathf.Lerp(overheat2.MinMalfChance, overheat2.MaxMalfChance, (overheat - overheat2.OverheatProblemsStart) / (overheat2.MaxOverheat - overheat2.OverheatProblemsStart));
                }
                overheatMalfChance *= (float)__instance.Item.Buff.MalfunctionProtections;
                if (weaponDurability > 59f)
                {
                    durabilityMalfChance = (Math.Pow((double)((WeaponProperties.TotalMalfChance + 1f) * ammoHotnessFactor), 3.0 + (double)(100f - weaponDurability) / (20.0 - 10.0 / Math.Pow((double)__instance.Item.FireRate / 10.0, 0.322))) - 1.0) / 1000.0;
                }
                else
                {
                    durabilityMalfChance = (Math.Pow((double)((WeaponProperties.TotalMalfChance + 1f) * ammoHotnessFactor), Math.Log10(Math.Pow((double)(101f - weaponDurability), (50.0 - Math.Pow((double)weaponDurability, 1.286) / 4.8) / (Math.Pow((double)__instance.Item.FireRate, 0.17) / 2.9815 + 2.1)))) - 1.0) / 1000.0;
                }
                durabilityMalfChance *= (double)((float)__instance.Item.Buff.MalfunctionProtections);
                if (__instance.Item.MalfState.HasMalfReduceChance(player.ProfileId, Weapon.EMalfunctionSource.Durability))
                {
                    durabilityMalfChance *= (double)troubleShooting.EliteDurabilityChanceReduceMult;
                }
                if (__instance.Item.MalfState.HasMalfReduceChance(player.ProfileId, Weapon.EMalfunctionSource.Magazine))
                {
                    magMalfChance *= troubleShooting.EliteMagChanceReduceMult;
                }
                if (__instance.Item.MalfState.HasMalfReduceChance(player.ProfileId, Weapon.EMalfunctionSource.Ammo))
                {
                    ammoMalfChance *= troubleShooting.EliteAmmoChanceReduceMult;
                }
                if (num >= malfunction.DurRangeToIgnoreMalfs.x && num <= malfunction.DurRangeToIgnoreMalfs.y)
                {
                    magMalfChance = 0f;
                }
                durabilityMalfChance = (double)Mathf.Clamp01((float)durabilityMalfChance);
                __result = Mathf.Clamp01((float)Math.Round(durabilityMalfChance + (double)((ammoMalfChance + magMalfChance + overheatMalfChance) / 1000f), 5));
                Logger.LogWarning("ammoMalfChance = " + ammoMalfChance);
                Logger.LogWarning("Total Malf Chance = " + __result);
                return false;
            }
            else
            {
                return true;
            }

        }
    }

    public class IsKnownMalfTypePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon.MalfunctionState).GetMethod("IsKnownMalfType", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostfix(ref bool __result)
        {
            __result = true;
        }
    }
}
