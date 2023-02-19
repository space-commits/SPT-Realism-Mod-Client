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

            if (player.IsYourPlayer == true)
            {
                durabilityMalfChance = 0.0;
                magMalfChance = 0f;
                ammoMalfChance = 0f;
                overheatMalfChance = 0f;
                weaponDurability = 0f;

/*                float ammoHotnessFactor = (1f - ((ammoToFire.ammoRec / 200f) + 1f)) + 1f;*/

                if (WeaponProperties.CanCycleSubs == false && ammoToFire.ammoHear == 1)
                {

                    if (ammoToFire.Caliber == "762x39")
                    {
                        __result = 0.2f;
                    }
                    else
                    {
                        __result = 0.4f;
                    }

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
                float ammoMalfChanceMult = malfunction.AmmoMalfChanceMult;
                float magazineMalfChanceMult = malfunction.MagazineMalfChanceMult;
                MagazineClass currentMagazine = __instance.Item.GetCurrentMagazine();
                magMalfChance = ((currentMagazine == null) ? 0f : (currentMagazine.MalfunctionChance * magazineMalfChanceMult));
                ammoMalfChance = ((ammoToFire != null) ? ((ammoToFire.MalfMisfireChance + ammoToFire.MalfFeedChance) * ammoMalfChanceMult) : 0f);
                float durability = __instance.Item.Repairable.Durability / (float)__instance.Item.Repairable.TemplateDurability * 100f;
                weaponDurability = Mathf.Floor(durability);
                float weaponMalfChance = WeaponProperties.TotalMalfChance;
                if (overheat >= overheat2.OverheatProblemsStart)
                {
                    overheatMalfChance = Mathf.Lerp(overheat2.MinMalfChance, overheat2.MaxMalfChance, (overheat - overheat2.OverheatProblemsStart) / (overheat2.MaxOverheat - overheat2.OverheatProblemsStart));
                }
                overheatMalfChance *= (float)__instance.Item.Buff.MalfunctionProtections;

                if (weaponDurability >= Plugin.DuraMalfThreshold.Value)
                {

                    magMalfChance *= 0.5f;
                    weaponMalfChance *= 0.5f;
                }
                if (weaponDurability >= 70)
                {

                    durabilityMalfChance = ((Math.Pow((double)((weaponMalfChance + 1f)), 3.0 + (double)(100f - weaponDurability) / (20.0 - 10.0 / Math.Pow((double)__instance.Item.FireRate / 10.0, 0.322))) - 1.0) / 1000.0);
                }
                else
                {

                    durabilityMalfChance = (Math.Pow((double)((weaponMalfChance + 1f)), Math.Log10(Math.Pow((double)(101f - weaponDurability), (50.0 - Math.Pow((double)weaponDurability, 1.286) / 4.8) / (Math.Pow((double)__instance.Item.FireRate, 0.17) / 2.9815 + 2.1)))) - 1.0) / 1000.0;
                }
                durabilityMalfChance *= (double)(float)__instance.Item.Buff.MalfunctionProtections;
                durabilityMalfChance = (double)Mathf.Clamp01((float)durabilityMalfChance);
                float totalMalfChance = Mathf.Clamp01((float)Math.Round(durabilityMalfChance + (double)((ammoMalfChance + magMalfChance + overheatMalfChance) / 1000f), 5));

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
            return typeof(Weapon.MalfunctionState).GetMethod("IsKnownMalfType", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPostfix]
        private static void PatchPostfix(ref bool __result)
        {
            __result = true;
        }
    }
}
