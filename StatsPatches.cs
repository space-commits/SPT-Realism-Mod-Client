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
                if (Helper.isReloading)
                {
                    __result = p.MagDelta(ref __instance);
                }
                else
                {
                    p.StatDelta(ref __instance);
                    __result = p.MagDelta(ref __instance);
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
            __instance.ConflictingItems[4] = instanceID;
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
                float magWeightFactored = StatCalc.factoredWeight(magWeight);
                string position = StatCalc.getModPosition(magazine, weapType, weapOpType);
                magErgo = magazine.Ergonomics;
                currentTorque = StatCalc.getTorque(position, magWeightFactored, WeaponProperties.Balance);
            }
            float weapWeightLessMagFactor = ((totalWeight - magWeight) / 100f) * -1f;
            float weapTorqueLessMagFactor = WeaponProperties.SDBalance / 100f;

            float currentReloadSpeed = WeaponProperties.SDReloadSpeedModifier + magReloadSpeed;

            float currentFixSpeed = WeaponProperties.SDFixSpeedModifier;

            float recoilDamping = WeaponProperties.RecoilDamping(__instance);
            float recoilHandDamping = WeaponProperties.RecoilHandDamping(__instance);

            float baseErgo = __instance.Template.Ergonomics;
            float ergoWeightFactor = StatCalc.weightStatCalc(StatCalc.ErgoWeightMult, magWeight) / 100;
            float currentErgo = WeaponProperties.SDTotalErgo + (WeaponProperties.SDTotalErgo * ((magErgo / 100f) + ergoWeightFactor));

            float baseVRecoil = __instance.Template.RecoilForceUp;
            float vRecoilWeightFactor = StatCalc.weightStatCalc(StatCalc.VRecoilWeightMult, magWeight) / 100;
            float currentVRecoil = WeaponProperties.SDTotalVRecoil + (WeaponProperties.SDTotalVRecoil * vRecoilWeightFactor);

            float baseHRecoil = __instance.Template.RecoilForceBack;
            float hRecoilWeightFactor = StatCalc.weightStatCalc(StatCalc.HRecoilWeightMult, magWeight) / 100;
            float currentHRecoil = WeaponProperties.SDTotalHRecoil + (WeaponProperties.SDTotalHRecoil * hRecoilWeightFactor);

            float dispersionWeightFactor = StatCalc.weightStatCalc(StatCalc.DispersionWeightMult, magWeight) / 100;
            float currentDispersion = WeaponProperties.SDDispersion + (WeaponProperties.SDDispersion * dispersionWeightFactor);

            float currentCamRecoil = WeaponProperties.SDCamRecoil;
            float currentRecoilAngle = WeaponProperties.SDRecoilAngle;

            currentTorque = (WeaponProperties.SDBalance + currentTorque);


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

            float totalCOI = 0;
            float totalCOIDelta = 0;


            StatCalc.weaponStatCalc(__instance, currentTorque, ref totalTorque, currentErgo, currentVRecoil, currentHRecoil, currentDispersion, currentCamRecoil, currentRecoilAngle, baseErgo, baseVRecoil, baseHRecoil, ref totalErgo, ref totalVRecoil, ref totalHRecoil, ref totalDispersion, ref totalCamRecoil, ref totalRecoilAngle, ref totalRecoilDamping, ref totalRecoilHandDamping, ref totalErgoDelta, ref totalVRecoilDelta, ref totalHRecoilDelta, ref totalRecoilDamping, ref totalRecoilHandDamping, WeaponProperties.SDTotalCOI, WeaponProperties.HasShoulderContact, ref totalCOI, ref totalCOIDelta, __instance.CenterOfImpactBase);

            float totalReloadSpeedMod = 0;
            float totalFixSpeedMod = 0;
            float totalAimMoveSpeedMod = 0;

            StatCalc.speedStatCalc(totalWeight, currentReloadSpeed, currentFixSpeed, totalTorque, weapWeightLessMagFactor, weapTorqueLessMagFactor, ref totalReloadSpeedMod, ref totalFixSpeedMod, ref totalAimMoveSpeedMod);

            float ergonomicWeight = 0;
            StatCalc.ergoWeightCalc(totalWeight, totalErgoDelta, ref ergonomicWeight);

            WeaponProperties.ReloadSpeedModifier = totalReloadSpeedMod;
            WeaponProperties.FixSpeedModifier = totalFixSpeedMod;
            WeaponProperties.AimMoveSpeedModifier = totalAimMoveSpeedMod;

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
            WeaponProperties.ErgonomicWeight = ergonomicWeight;
            WeaponProperties.TotalRecoilDamping = totalRecoilDamping;
            WeaponProperties.TotalRecoilHandDamping = totalRecoilHandDamping;
            DisplayWeaponProperties.COIDelta = totalCOIDelta * -1f;

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
                    float modWeightFactored = StatCalc.factoredWeight(modWeight);
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
                    string position = StatCalc.getModPosition(__instance.Mods[i], weapType, weapOpType);
                    string templID = mod.TemplateId;

                    StatCalc.modTypeStatCalc(__instance, mod, folded, weapType, weapOpType, ref hasShoulderContact, ref modAutoROF, ref modSemiROF, ref stockAllowsFSADS, ref modVRecoil, ref modHRecoil, ref modCamRecoil, ref modAngle, ref modDispersion, ref modErgo, ref modAccuracy, ref modType, ref position);
                    StatCalc.modStatCalc(modWeight, ref currentTorque, position, modWeightFactored, modAutoROF, ref currentAutoROF, modSemiROF, ref currentSemiROF, modCamRecoil, ref currentCamRecoil, modDispersion, ref currentDispersion, modAngle, ref currentRecoilAngle, modAccuracy, ref currentCOI, modAim, ref currentAimSpeed, modReload, ref currentReloadSpeed, modFix, ref currentFixSpeed, modErgo, ref currentErgo, modVRecoil, ref currentVRecoil, modHRecoil, ref currentHRecoil);
                }
            }

/*            StatCalc.stockContactStatCalc(hasShoulderContact, __instance, ref currentErgo, ref currentVRecoil, ref currentHRecoil, ref currentCOI, ref currentCamRecoil, ref currentDispersion, ref currentRecoilAngle);
*/
            if (weaponAllowsFSADS == true || stockAllowsFSADS == true)
            {
                WeaponProperties.WeaponCanFSADS = true;
            }
            else
            {
                WeaponProperties.WeaponCanFSADS = !hasShoulderContact;
            }

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
            WeaponProperties.SDTotalCOI = currentCOI;

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
                string weapOpType = WeaponProperties.OperationType(__instance);
                foreach (Mod mod in __instance.Mods)
                {
                    if (Helper.isStock(mod) == true)
                    {
                        string modType = AttatchmentProperties.ModType(mod);
                        if (weapOpType != "buffer" && (modType == "buffer" || modType == "buffer_stock"))
                        {
                            modsBurnRatio *= 1;
                        }
                        else
                        {
                            modsBurnRatio *= mod.DurabilityBurnModificator;
                        }

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

    public class UpdateWeaponVariablesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("UpdateWeaponVariables", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref EFT.Animations.ProceduralWeaponAnimation __instance, ref float ___float_7, ref Player.ValueBlender ___valueBlender_0)
        {
            Player.FirearmController firearmController = (Player.FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);

            if (firearmController != null)
            {
                if (firearmController.Item.Owner.ID.StartsWith("pmc"))
                {
                    float _aimsSpeed = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_7").GetValue(__instance);
                    __instance.HandsContainer.Recoil.ReturnSpeed = Plugin.startingConvergence * __instance.Aiming.RecoilConvergenceMult;
                    __instance.HandsContainer.Recoil.Damping = WeaponProperties.TotalRecoilDamping;
                    __instance.HandsContainer.HandsPosition.Damping = WeaponProperties.TotalRecoilHandDamping;
                    float aimSpeed = _aimsSpeed * (1f + WeaponProperties.AimSpeedModifier);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_7").SetValue(__instance, aimSpeed);
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_17").SetValue(__instance, WeaponProperties.ErgonomicWeight);
                }
            }
        }
    }

    public class SyncWithCharacterSkillsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player.FirearmController).GetMethod("SyncWithCharacterSkills", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref EFT.Player.FirearmController __instance)
        {
            if (__instance.Item.Owner.ID.StartsWith("pmc"))
            {
                SkillsClass.GClass1546 skillsClass = (SkillsClass.GClass1546)AccessTools.Field(typeof(EFT.Player.FirearmController), "gclass1546_0").GetValue(__instance);
                Player player = (Player)AccessTools.Field(typeof(EFT.Player.ItemHandsController), "_player").GetValue(__instance);
                SkillsClass.GClass1546 weaponInfo = player.Skills.GetWeaponInfo(__instance.Item);

                skillsClass.ReloadSpeed = weaponInfo.ReloadSpeed * (1 + WeaponProperties.ReloadSpeedModifier);
                skillsClass.FixSpeed = weaponInfo.FixSpeed * (1 + WeaponProperties.FixSpeedModifier);
                skillsClass.AimMovementSpeed = weaponInfo.AimMovementSpeed + WeaponProperties.AimMoveSpeedModifier;

            }
        }
    }

    public class method_17Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("method_17", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            Player.FirearmController firearmController = (Player.FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);
            if (firearmController != null)
            {
                if (firearmController.Item.Owner.ID.StartsWith("pmc"))
                {
                    float ergoWeightFactor = (WeaponProperties.ErgonomicWeight / 250) + 1f;
                    float breathIntensity = Mathf.Min(0.65f * ergoWeightFactor, 1.15f);
                    float handsIntensity = Mathf.Min(0.6f * ergoWeightFactor, 1.15f);

                    if (WeaponProperties.HasShoulderContact == false)
                    {
                        breathIntensity = Mathf.Min(1f * ergoWeightFactor, 1.25f);
                        handsIntensity = Mathf.Min(1f * ergoWeightFactor, 1.25f);
                    }

                    __instance.Breath.Intensity = breathIntensity; //both aim sway and up and down breathing
                    __instance.HandsContainer.HandsRotation.InputIntensity = (__instance.HandsContainer.HandsPosition.InputIntensity = handsIntensity * handsIntensity); //also breathing and sway but different, the hands doing sway motion but camera bobbing up and down.
                }
            }
        }
    }

    public class UpdateSwayFactorsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Animations.ProceduralWeaponAnimation).GetMethod("UpdateSwayFactors", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            Player.FirearmController firearmController = (Player.FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);
            if (firearmController != null)
            {
                if (firearmController.Item.Owner.ID.StartsWith("pmc"))
                {
                    /*                    float ergoWeight = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_17").GetValue(__instance); //ergoweight
                    */
                    float ergoWeight = WeaponProperties.ErgonomicWeight;
                    float weightFactor = StatCalc.proceduralIntensityFactorCalc(ergoWeight, 5f);
                    float aimIntensity = __instance.IntensityByAiming * 0.67f;

                    if (WeaponProperties.HasShoulderContact == false)
                    {
                        aimIntensity = __instance.IntensityByAiming;
                    }

                    float swayStrength = EFTHardSettings.Instance.SWAY_STRENGTH_PER_KG.Evaluate(ergoWeight * (1f + __instance.Overweight));
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_18").SetValue(__instance, swayStrength);

                    float weapDisplacement = EFTHardSettings.Instance.DISPLACEMENT_STRENGTH_PER_KG.Evaluate((ergoWeight * (weightFactor * 0.35f)) * (1f + __instance.Overweight));//delay from moving mouse to the weapon moving to center of screen.
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_19").SetValue(__instance, weapDisplacement);

                    __instance.MotionReact.SwayFactors = new Vector3(swayStrength, __instance.IsAiming ? (swayStrength * 0.3f) : swayStrength, swayStrength) * (Mathf.Clamp(aimIntensity * (weightFactor * 0.1f), aimIntensity, 1.18f)); // the diving/tiling animation as you move weapon side to side.
                }
            }
        }
    }
}
