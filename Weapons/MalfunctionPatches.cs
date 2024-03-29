﻿using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using MalfGlobals = BackendConfigSettingsClass.GClass1265;
using OverheatGlobals = BackendConfigSettingsClass.GClass1266;
using KnowMalfClass = EFT.InventoryLogic.Weapon.GClass2553;
using MalfStateClass = GClass646<EFT.InventoryLogic.Weapon.EMalfunctionState>;
using MalfStateStruct = GClass646<EFT.InventoryLogic.Weapon.EMalfunctionState>.GStruct42<float, EFT.InventoryLogic.Weapon.EMalfunctionState>;
namespace RealismMod
{

    public class GetMalfVariantsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("GetSpecificMalfunctionVariants", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPrefix]
        private static bool Prefix(Player.FirearmController __instance, List<MalfStateStruct> result, BulletClass ammo, Weapon.EMalfunctionSource malfunctionSource, float weaponDurability, bool hasAmmoInMag, bool isMagazineInserted, bool shouldCheckJam)
        {
            result.Clear();
            MalfGlobals malfunction = Singleton<BackendConfigSettingsClass>.Instance.Malfunction;
            switch (malfunctionSource)
            {
                case Weapon.EMalfunctionSource.Durability:
                    if (weaponDurability >= 80f && hasAmmoInMag && isMagazineInserted && __instance.Item.AllowFeed)
                    {
                        result.Add(new MalfStateStruct(malfunction.DurFeedWt, Weapon.EMalfunctionState.Feed));
                        return false;
                    }
                    if (weaponDurability >= 50f && hasAmmoInMag && __instance.Item.AllowSlide)
                    {
                        result.Add(new MalfStateStruct(malfunction.DurSoftSlideWt, Weapon.EMalfunctionState.SoftSlide));
                        return false;
                    }
                    if (weaponDurability < 50f && hasAmmoInMag && __instance.Item.AllowSlide)
                    {
                        float hardSlideWeight = Mathf.Lerp(malfunction.DurHardSlideMinWt, malfunction.DurHardSlideMaxWt, 1f - weaponDurability / 5f);
                        result.Add(new MalfStateStruct(hardSlideWeight, Weapon.EMalfunctionState.HardSlide));
                        return false;
                    }
                    if (shouldCheckJam && __instance.Item.AllowJam)
                    {
                        result.Add(new MalfStateStruct(malfunction.DurJamWt, Weapon.EMalfunctionState.Jam));
                        return false;
                    }
                    if (__instance.Item.AllowMisfire)
                    {
                        result.Add(new MalfStateStruct(malfunction.DurMisfireWt, Weapon.EMalfunctionState.Misfire));
                        return false;
                    }
                    break;
                case Weapon.EMalfunctionSource.Ammo:
                    if (__instance.Item.AllowMisfire)
                    {
                        float ammoWeight = malfunction.AmmoMisfireWt / (ammo.MalfMisfireChance + ammo.MalfFeedChance);
                        result.Add(new MalfStateStruct(ammoWeight, Weapon.EMalfunctionState.Misfire));
                        return false;
                    }
                    if (shouldCheckJam && __instance.Item.AllowJam)
                    {
                        result.Add(new MalfStateStruct(malfunction.AmmoJamWt, Weapon.EMalfunctionState.Jam));
                        return false;
                    }
                    if (hasAmmoInMag && isMagazineInserted && __instance.Item.AllowFeed)
                    {
                        result.Add(new MalfStateStruct(malfunction.AmmoFeedWt, Weapon.EMalfunctionState.Feed));
                        return false;
                    }
                    break;
                case Weapon.EMalfunctionSource.Magazine:
                    if (hasAmmoInMag && isMagazineInserted && __instance.Item.AllowFeed)
                    {
                        result.Add(new MalfStateStruct(1f, Weapon.EMalfunctionState.Feed));
                        return false;
                    }
                    if (hasAmmoInMag && isMagazineInserted && __instance.Item.AllowJam)
                    {
                        result.Add(new MalfStateStruct(1f, Weapon.EMalfunctionState.Jam));
                        return false;
                    }
                    break;
                case Weapon.EMalfunctionSource.Ammo | Weapon.EMalfunctionSource.Magazine:
                    break;
                case Weapon.EMalfunctionSource.Overheat:
                    if (hasAmmoInMag && __instance.Item.AllowSlide)
                    {
                        result.Add(new MalfStateStruct(malfunction.OverheatSoftSlideWt, Weapon.EMalfunctionState.SoftSlide));
                    }
                    if (weaponDurability <= 50f && hasAmmoInMag && __instance.Item.AllowSlide)
                    {
                        float heatMalfWeight = Mathf.Lerp(malfunction.OverheatHardSlideMinWt, malfunction.OverheatHardSlideMaxWt, 1f - weaponDurability / 5f);
                        result.Add(new MalfStateStruct(heatMalfWeight, Weapon.EMalfunctionState.HardSlide));
                        return false;
                    }
                    break;
                default:
                    return false;
            }
            return false;
        }
    }


/*    public class GetMalfunctionStatePatch : ModulePatch
        {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("GetMalfunctionState", BindingFlags.Instance | BindingFlags.Public);
        }
        [PatchPrefix]
        private static bool Prefix(Player.FirearmController __instance, ref Weapon.EMalfunctionState __result, BulletClass ammoToFire, bool hasAmmoInMag, bool doesWeaponHaveBoltCatch, bool isMagazineInserted, float overheat, float fixSlideOverheat, out Weapon.EMalfunctionSource malfunctionSource)
        {
            malfunctionSource = Weapon.EMalfunctionSource.Durability;
            if (!__instance.Item.AllowMalfunction)
            {
                __result = Weapon.EMalfunctionState.None;
                return false;
            }
            if (__instance.Item.MalfState.SlideOnOverheatReached && overheat > fixSlideOverheat && __instance.Item.AllowSlide && hasAmmoInMag)
            {
                malfunctionSource = Weapon.EMalfunctionSource.Overheat;
                __result = Weapon.EMalfunctionState.SoftSlide;
                return false;
            }
            double durabilityMalfChance;
            float magMalfChance;
            float ammoMalfChance;
            float overheatMalfChance;
            float weaponDurability;
            float totalMalfunctionChance = __instance.GetTotalMalfunctionChance(ammoToFire, overheat, out durabilityMalfChance, out magMalfChance, out ammoMalfChance, out overheatMalfChance, out weaponDurability);
            float randomFloat = __instance.gclass2859_0.GetRandomFloat();

            //instead of getting a total malf chance, roll chance to malf per malf type that's above 0
            //if nothing gets rolled then return 0
            //make sure each malf source has a unique malf type
            

            if (randomFloat > totalMalfunctionChance) // get rid of this
            {
                __result = Weapon.EMalfunctionState.None;
                return false;
            }
            List<GClass757<Weapon.EMalfunctionSource>.GStruct43<float, Weapon.EMalfunctionSource>> list = __instance.list_0;// get rid of this
            __instance.GetMalfunctionSources(list, durabilityMalfChance, magMalfChance, ammoMalfChance, overheatMalfChance, hasAmmoInMag, isMagazineInserted);// get rid of this
            malfunctionSource = GClass757<Weapon.EMalfunctionSource>.GenerateDrop(list, randomFloat); // use my own logic to get this
            bool shouldCheckJam = hasAmmoInMag || !doesWeaponHaveBoltCatch || !isMagazineInserted;
            List<GClass757<Weapon.EMalfunctionState>.GStruct43<float, Weapon.EMalfunctionState>> list2 = __instance.list_1; // get rid of this
            __instance.GetSpecificMalfunctionVariants(list2, ammoToFire, malfunctionSource, weaponDurability, hasAmmoInMag, isMagazineInserted, shouldCheckJam); // get rid of this
            if (list2.Count == 0)
            {
                __result = Weapon.EMalfunctionState.None;
                return false;
            }
            __result = GClass757<Weapon.EMalfunctionState>.GenerateDrop(list2); // use my own logic to get this
            return false;

            /////////////////////////////
            malfunctionSource = Weapon.EMalfunctionSource.Durability;
            switch (Plugin.test4.Value) 
            {
                case 1:
                    __result = Weapon.EMalfunctionState.Misfire;
                    return false;
                case 2:
                    __result = Weapon.EMalfunctionState.Jam; //failure to eject
                    return false;
                case 3:
                    __result = Weapon.EMalfunctionState.HardSlide; //stuck slide
                    return false;
                case 4:
                    __result = Weapon.EMalfunctionState.SoftSlide; //stuck slide but quicker
                    return false;
                case 5:
                    __result = Weapon.EMalfunctionState.Feed; //double feed
                    return false;
                default:
                    __result = Weapon.EMalfunctionState.None;
                    return false;
            }

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

                /*                float ammoHotnessFactor = (1f - ((ammoToFire.ammoRec / 200f) + 1f)) + 1f;*/

                float malfDelta = Mathf.Min(WeaponProperties.MalfChanceDelta * 3, 0.99f);
                if (!WeaponProperties.CanCycleSubs && ammoToFire.ammoHear == 1)
                {

                    if (ammoToFire.Caliber == "762x39")
                    {
                        __result = 0.2f * (1f - malfDelta);
                    }
                    else
                    {
                        __result = 0.35f * (1f - malfDelta);
                    }

                    return false;
                }

                BackendConfigSettingsClass instance = Singleton<BackendConfigSettingsClass>.Instance;
                MalfGlobals malfunctionGlobals = instance.Malfunction;
                OverheatGlobals overheatGlobals = instance.Overheat;
                float ammoMalfChanceMult = malfunctionGlobals.AmmoMalfChanceMult;
                float magazineMalfChanceMult = malfunctionGlobals.MagazineMalfChanceMult;
                MagazineClass currentMagazine = __instance.Item.GetCurrentMagazine();
                magMalfChance = ((currentMagazine == null) ? 0f : (currentMagazine.MalfunctionChance * magazineMalfChanceMult));
                ammoMalfChance = ((ammoToFire != null) ? ((ammoToFire.MalfMisfireChance + ammoToFire.MalfFeedChance) * ammoMalfChanceMult) : 0f);
                float weaponMalfChance = WeaponProperties.TotalMalfChance;
                float durability = __instance.Item.Repairable.Durability / (float)__instance.Item.Repairable.TemplateDurability * 100f;
                weaponDurability = Mathf.Floor(durability);
      
                if (weaponDurability >= Plugin.DuraMalfThreshold.Value)
                {
                    magMalfChance *= 0.1f;
                    weaponMalfChance *= 0.1f;
                    ammoMalfChance *= 0.2f;
                }
                if (overheat >= overheatGlobals.OverheatProblemsStart)
                {
                    overheatMalfChance = Mathf.Lerp(overheatGlobals.MinMalfChance, overheatGlobals.MaxMalfChance, (overheat - overheatGlobals.OverheatProblemsStart) / (overheatGlobals.MaxOverheat - overheatGlobals.OverheatProblemsStart));
                }
                overheatMalfChance *= (float)__instance.Item.Buff.MalfunctionProtections;

                if (weaponDurability >= 50)
                {
                    durabilityMalfChance = ((Math.Pow((double)((weaponMalfChance + 1f)), 3.0 + (double)(100f - weaponDurability) / (20.0 - 10.0 / Math.Pow((double)__instance.Item.FireRate / 10.0, 0.322))) - 1.0) / 1000.0);
                }
                else
                {
                    durabilityMalfChance = (Math.Pow((double)((weaponMalfChance + 1f)), Math.Log10(Math.Pow((double)(101f - weaponDurability), (50.0 - Math.Pow((double)weaponDurability, 1.286) / 4.8) / (Math.Pow((double)__instance.Item.FireRate, 0.17) / 2.9815 + 2.1)))) - 1.0) / 1000.0;
                }
                if (weaponDurability >= Plugin.DuraMalfThreshold.Value)
                {
                    durabilityMalfChance *= 0.25f;
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
            return typeof(KnowMalfClass).GetMethod("IsKnownMalfType", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref bool __result)
        {
            __result = true;
        }
    }
}
