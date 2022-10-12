using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static RealismMod.Helper;
using static EFT.Player;

namespace RealismMod
{

    public class ReloadMagPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("ReloadMag", BindingFlags.Instance | BindingFlags.Public);

        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance)
        {
            if (__instance.Item.Owner.ID.StartsWith("pmc"))
            {
                Helper.isReloading = true;
            }
        }
    }

    public class QuickReloadMagPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("QuickReloadMag", BindingFlags.Instance | BindingFlags.Public);

        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance)
        {
            if (__instance.Item.Owner.ID.StartsWith("pmc"))
            {
                Helper.isReloading = true;
            }
        }
    }

    public class OnMagInsertedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("method_43", BindingFlags.Instance | BindingFlags.NonPublic);

        }

        [PatchPostfix]
        private static void PatchPostfix(ref Player.FirearmController __instance)
        {
            if (__instance.Item.Owner.ID.StartsWith("pmc"))
            {
                Helper.isReloading = false;
            }
        }
    }

    public class SingleFireRatePatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("get_SingleFireRate", BindingFlags.Instance | BindingFlags.Public);

        }
        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref int __result)
        {
            if (__instance?.Owner?.ID != null && __instance.Owner.ID.StartsWith("pmc"))
            {
                __result = WeaponProperties.SemiFireRate;
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public class AutoFireRatePatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("get_FireRate", BindingFlags.Instance | BindingFlags.Public);

        }
        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref int __result)
        {
            if (__instance?.Owner?.ID != null && __instance.Owner.ID.StartsWith("pmc"))
            {
                __result = WeaponProperties.AutoFireRate;
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public class ErgoDeltaPatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("get_ErgonomicsDelta", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref float __result)
        {
            if (__instance?.Owner?.ID != null && __instance.Owner.ID.StartsWith("pmc"))
            {
                ErgoDeltaPatch p = new ErgoDeltaPatch();
                string newInstanceID = __instance.Id + __instance.GetSingleItemTotalWeight().ToString();
                if (__instance.Folded != WeaponProperties.Folded)
                {
                    p.StatDelta(ref __instance);
                    __result = p.MagDelta(ref __instance);
                }
                else if (WeaponProperties.SavedInstanceID != WeaponProperties.InstanceID(__instance) || newInstanceID != WeaponProperties.InstanceID(__instance) || WeaponProperties.InstanceID(__instance) == "")
                {
                    if (Helper.isReloading)
                    {
                        __result = p.MagDelta(ref __instance);
                    }
                    else
                    {
                        p.StatDelta(ref __instance);
                        __result = p.MagDelta(ref __instance);
                    }
                }
                else
                {
                    __result = WeaponProperties.ErgoDelta;
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        public float MagDelta(ref Weapon __instance)
        {
 
            float totalWeight = __instance.GetSingleItemTotalWeight();
            string weapType = WeaponProperties.WeaponType(__instance);
            string weapOpType = WeaponProperties.OperationType(__instance);

            string instanceID = __instance.Id + totalWeight.ToString();
            __instance.ConflictingItems[3] = instanceID;
            WeaponProperties.SavedInstanceID = instanceID;

            Mod magazine = __instance.GetCurrentMagazine();
            float magErgo = 0;
            float magWeight = 0;
            float magReloadSpeed = 0;
            float currentTorque = 0;

            if (magazine != null)
            {
                magReloadSpeed = AttatchmentProperties.ReloadSpeed(magazine);
                magWeight = magazine.GetSingleItemTotalWeight();
                float magWeightFactored = Helper.factoredWeight(magWeight);
                string position = Helper.getModPosition(magazine, weapType, weapOpType);
                magErgo = magazine.Ergonomics;
                currentTorque = Helper.getTorque(position, magWeightFactored, WeaponProperties.Balance);
            }
            float weapWeightLessMagFactor = ((totalWeight - magWeight) / 100f) * -1f;
            float weapTorqueLessMagFactor = WeaponProperties.SDBalance / 100f;

            float currentReloadSpeed = WeaponProperties.SDReloadSpeedModifier + magReloadSpeed;

            float currentFixSpeed = WeaponProperties.SDFixSpeedModifier;

            float recoilDamping = WeaponProperties.RecoilDamping(__instance);
            float recoilHandDamping = WeaponProperties.RecoilHandDamping(__instance);

            float baseErgo = __instance.Template.Ergonomics;
            float ergoWeightFactor = Helper.weightStatCalc(StatCalc.ErgoWeightMult, magWeight) / 100;
            float currentErgo = WeaponProperties.SDTotalErgo + (WeaponProperties.SDTotalErgo * ((magErgo / 100f) + ergoWeightFactor));

            float baseVRecoil = __instance.Template.RecoilForceUp;
            float vRecoilWeightFactor = Helper.weightStatCalc(StatCalc.VRecoilWeightMult, magWeight) / 100;
            float currentVRecoil = WeaponProperties.SDTotalVRecoil + (WeaponProperties.SDTotalVRecoil * vRecoilWeightFactor);

            float baseHRecoil = __instance.Template.RecoilForceBack;
            float hRecoilWeightFactor = Helper.weightStatCalc(StatCalc.HRecoilWeightMult, magWeight) / 100;
            float currentHRecoil = WeaponProperties.SDTotalHRecoil + (WeaponProperties.SDTotalHRecoil * hRecoilWeightFactor);

            float dispersionWeightFactor = Helper.weightStatCalc(StatCalc.DispersionWeightMult, magWeight) / 100;
            float currentDispersion = WeaponProperties.SDDispersion + (WeaponProperties.SDDispersion * dispersionWeightFactor);

            float currentCamRecoil = WeaponProperties.SDCamRecoil;
            float currentRecoilAngle = WeaponProperties.SDRecoilAngle;

            float ergoStatFactor = WeaponProperties.ErgoStatFactor;
            float recoilStatFactor = WeaponProperties.RecoilStatFactor;
            currentTorque = (WeaponProperties.SDBalance + currentTorque);
            float wepBaseWeightFactorErgo = Helper.weightStatCalc(ergoStatFactor, totalWeight) / 100;
            float wepBaseWeightFactorRecoil = Helper.weightStatCalc(recoilStatFactor, totalWeight) / 100;

            float totalTorque = 0;
            float totalErgo = 0;
            float totalVRecoil = 0;
            float totalHRecoil = 0;
            float totalDispersion = 0;
            float totalCamRecoil = 0;
            float totalRecoilAngle = 0;
            float totalRecoilDamping = 0;
            float totalRecoilHandDamping = 0;

            float totalErgoDelta = 0;
            float totalVRecoilDelta = 0;
            float totalHRecoilDelta = 0;

            StatCalc.weaponStatCalc(__instance, currentTorque, ref totalTorque, currentErgo, currentVRecoil, currentHRecoil, currentDispersion, currentCamRecoil, currentRecoilAngle, baseErgo, baseVRecoil, baseHRecoil, ref totalErgo, ref totalVRecoil, ref totalHRecoil, ref totalDispersion, ref totalCamRecoil, ref totalRecoilAngle, ref totalRecoilDamping, ref totalRecoilHandDamping, ref totalErgoDelta, ref totalVRecoilDelta, ref totalHRecoilDelta, ref recoilDamping, ref recoilHandDamping, true);

            float weaponTorqueFactor = totalTorque / 100f;
            float weaponTorqueFactorInverse = totalTorque / 100f * -1f;

            float totalReloadSpeed = (currentReloadSpeed / 100f) + ((weapWeightLessMagFactor + weapTorqueLessMagFactor) * StatCalc.ReloadSpeedMult);
            float totalFixSpeed = (currentFixSpeed / 100f) + ((weapWeightLessMagFactor + weapTorqueLessMagFactor) * StatCalc.FixSpeedMult);
            float totalAimMoveSpeedModifier = (wepBaseWeightFactorErgo + weaponTorqueFactor) * StatCalc.AimMoveSpeedMult;

            float factoredWeight = totalWeight * (1 - totalErgoDelta);
            float ergonomicWeight = Mathf.Clamp((float)(Math.Pow(factoredWeight * 1.1, 4.9) + 1) / 500, 1f, 50f);

            WeaponProperties.AimMoveSpeedModifier = totalAimMoveSpeedModifier;
            WeaponProperties.ReloadSpeedModifier = totalReloadSpeed;
            WeaponProperties.FixSpeedModifier = totalFixSpeed;

            WeaponProperties.Dispersion = totalDispersion;
            WeaponProperties.CamRecoil = totalCamRecoil;
            WeaponProperties.RecoilAngle = totalRecoilAngle;
            WeaponProperties.TotalVRecoil = totalVRecoil;
            WeaponProperties.TotalHRecoil = totalHRecoil;
            WeaponProperties.Balance = totalTorque;
            WeaponProperties.TotalErgo = totalErgo;
            WeaponProperties.ErgoDelta = totalErgoDelta;
            WeaponProperties.VRecoilDelta = totalVRecoilDelta;
            WeaponProperties.HRecoilDelta = totalHRecoilDelta;
            WeaponProperties.ErgnomicWeight = ergonomicWeight;
            WeaponProperties.TotalRecoilDamping = totalRecoilDamping;
            WeaponProperties.TotalRecoilHandDamping = totalRecoilHandDamping;


            return totalErgoDelta;
        }

        public void StatDelta(ref Weapon __instance)
        {
            float baseCOI = __instance.CenterOfImpactBase;
            float currentCOI = baseCOI;

            float baseAutoROF = __instance.Template.bFirerate;
            float currentAutoROF = baseAutoROF;

            float baseSemiROF = Mathf.Max(__instance.Template.SingleFireRate, 240);
            float currentSemiROF = baseSemiROF;

            float baseCamRecoil = __instance.Template.CameraRecoil;
            float currentCamRecoil = baseCamRecoil;

            float baseDispersion = __instance.Template.RecolDispersion;
            float currentDispersion = baseDispersion;

            float baseAngle = __instance.Template.RecoilAngle;
            float currentRecoilAngle = baseAngle;

            float baseVRecoil = __instance.Template.RecoilForceUp;
            float currentVRecoil = baseVRecoil;
            float baseHRecoil = __instance.Template.RecoilForceBack;
            float currentHRecoil = baseHRecoil;

            float baseErgo = __instance.Template.Ergonomics;
            float currentErgo = baseErgo;

            float currentTorque = 0f;

            float currentReloadSpeed = 0f;

            float currentAimSpeed = 0f;

            float currentFixSpeed = 0f;

            string weapOpType = WeaponProperties.OperationType(__instance);
            string weapType = WeaponProperties.WeaponType(__instance);

            bool weaponAllowsFSADS = WeaponProperties.WeaponAllowsADS(__instance);
            bool stockAllowsFSADS = false;

            bool folded = __instance.Folded;
            WeaponProperties.Folded = folded;

            bool hasShoulderContact = false;

            if (WeaponProperties.WepHasShoulderContact(__instance) == true && !folded)
            {
                hasShoulderContact = true;
            }

            for (int i = 0; i < __instance.Mods.Length; i++)
            {
                Mod mod = __instance.Mods[i];
                if (Helper.isMagazine(mod) == false)
                {
                    float modWeight = __instance.Mods[i].Weight;
                    float modWeightFactored = Helper.factoredWeight(modWeight);
                    float modErgo = __instance.Mods[i].Ergonomics;
                    float modVRecoil = AttatchmentProperties.VerticalRecoil(__instance.Mods[i]);
                    float modHRecoil = AttatchmentProperties.HorizontalRecoil(__instance.Mods[i]);
                    float modAutoROF = AttatchmentProperties.AutoROF(__instance.Mods[i]);
                    float modSemiROF = AttatchmentProperties.SemiROF(__instance.Mods[i]);
                    float modCamRecoil = AttatchmentProperties.CameraRecoil(__instance.Mods[i]);
                    float modDispersion = AttatchmentProperties.Dispersion(__instance.Mods[i]);
                    float modAngle = AttatchmentProperties.RecoilAngle(__instance.Mods[i]);
                    float modAccuracy = __instance.Mods[i].Accuracy;
                    float modReload = AttatchmentProperties.ReloadSpeed(__instance.Mods[i]);
                    float modAim = AttatchmentProperties.AimSpeed(__instance.Mods[i]);
                    float modFix = AttatchmentProperties.FixSpeed(__instance.Mods[i]);
                    string modType = AttatchmentProperties.ModType(__instance.Mods[i]);
                    string position = Helper.getModPosition(__instance.Mods[i], weapType, weapOpType);

                    StatCalc.modTypeStatCalc(__instance, mod, folded, weapType, weapOpType, ref hasShoulderContact, ref modAutoROF, ref modSemiROF, ref stockAllowsFSADS, ref modVRecoil, ref modHRecoil, ref modCamRecoil, ref modAngle, ref modDispersion, ref modErgo, ref modAccuracy, ref modType, ref position);
                    StatCalc.modStatCalc(modWeight, ref currentTorque, position, modWeightFactored, modAutoROF, ref currentAutoROF, modSemiROF, ref currentSemiROF, modCamRecoil, ref currentCamRecoil, modDispersion, ref currentDispersion, modAngle, ref currentRecoilAngle, modAccuracy, ref currentCOI, modAim, ref currentAimSpeed, modReload, ref currentReloadSpeed, modFix, ref currentFixSpeed, modErgo, ref currentErgo, modVRecoil, ref currentVRecoil, modHRecoil, ref currentHRecoil);
                }
            }

            StatCalc.stockContactStatCalc(hasShoulderContact, __instance, ref currentErgo, ref currentVRecoil, ref currentHRecoil, ref currentCOI, ref currentCamRecoil, ref currentDispersion, ref currentRecoilAngle);

            if (weaponAllowsFSADS == true || stockAllowsFSADS == true)
            {
                WeaponProperties.WeaponCanFSADS = true;
            }
            else
            {
                WeaponProperties.WeaponCanFSADS = !hasShoulderContact;
            }

            float totalCOI = 0;
            float totalCOIDelta = 0;

            StatCalc.accuracyStatAssignment(__instance, currentCOI, baseCOI, ref totalCOI, ref totalCOIDelta);

            WeaponProperties.HasShoulderContact = hasShoulderContact;
            WeaponProperties.SDTotalErgo = currentErgo;
            WeaponProperties.SDTotalVRecoil = currentVRecoil;
            WeaponProperties.SDTotalHRecoil = currentHRecoil;
            WeaponProperties.SDBalance = currentTorque;
            WeaponProperties.SDCamRecoil = currentCamRecoil;
            WeaponProperties.SDDispersion = currentDispersion;
            WeaponProperties.SDRecoilAngle = currentRecoilAngle;
            WeaponProperties.SDReloadSpeedModifier = currentReloadSpeed;
            WeaponProperties.SDFixSpeedModifier = currentFixSpeed;
            WeaponProperties.AimSpeedModifier = currentAimSpeed / 100f;
            WeaponProperties.AutoFireRate = Mathf.Max(300, (int)currentAutoROF);
            WeaponProperties.SemiFireRate = Mathf.Max(200, (int)currentSemiROF);
            WeaponProperties.COIDelta = totalCOIDelta * -1f;
        }
    }

    public class COIDeltaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("get_CenterOfImpactDelta", BindingFlags.Instance | BindingFlags.Public);

        }

        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref float __result)
        {

            if (__instance?.Owner?.ID != null && __instance.Owner.ID.StartsWith("pmc"))
            {
                __result = WeaponProperties.COIDelta;
                return false;
            }
            else
            {
                return true;
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
        private static bool Prefix(ref Weapon __instance, ref float __result, float ammoBurnRatio, float overheatFactor, float skillWeaponTreatmentFactor, out float modsBurnRatio)
        {

            if (__instance?.Owner?.ID != null && __instance.Owner.ID.StartsWith("pmc"))
            {
                modsBurnRatio = 1f;
                foreach (Mod mod in __instance.Mods)
                {
                    if (AttatchmentProperties.ModType(mod) == "buffer")
                    {
                        modsBurnRatio = 0;
                    }
                    else
                    {
                        modsBurnRatio *= mod.DurabilityBurnModificator;
                    }
                }
                __result = (float)__instance.Repairable.TemplateDurability / __instance.Template.OperatingResource * __instance.DurabilityBurnRatio * (modsBurnRatio * ammoBurnRatio) * overheatFactor * (1f - skillWeaponTreatmentFactor); ;
                return false;
            }
            else
            {
                modsBurnRatio = 1f;
                return true;
            }
        }
    }
}
