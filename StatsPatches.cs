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
                AmmoTemplate currentAmmoTemplate = __instance.CurrentAmmoTemplate;
                __result = (currentAmmoTemplate != null) ? (int)(WeaponProperties.SemiFireRate * currentAmmoTemplate.casingMass) : WeaponProperties.SemiFireRate;
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
                AmmoTemplate currentAmmoTemplate = __instance.CurrentAmmoTemplate;
                __result = (currentAmmoTemplate != null) ? (int)(WeaponProperties.AutoFireRate * currentAmmoTemplate.casingMass) : WeaponProperties.AutoFireRate;
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
                if (Helper.IsInReloadOpertation)
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

            Mod magazine = __instance.GetCurrentMagazine();
            float magErgo = 0;
            float magWeight = 0;
            float currentTorque = 0;
            bool hasMag = magazine != null;

            if (hasMag == true)
            {
                magWeight = magazine.GetSingleItemTotalWeight();
                float magWeightFactored = StatCalc.FactoredWeight(magWeight);
                string position = StatCalc.GetModPosition(magazine, weapType, weapOpType);
                magErgo = magazine.Ergonomics;
                currentTorque = StatCalc.GetTorque(position, magWeightFactored, __instance.WeapClass);
            }

            float weapWeightLessMag = totalWeight - magWeight;

            float currentReloadSpeed = WeaponProperties.SDReloadSpeedModifier;

            float currentChamberSpeed = WeaponProperties.SDChamberSpeedModifier;

            float currentFixSpeed = WeaponProperties.SDFixSpeedModifier;

            float recoilDamping = WeaponProperties.RecoilDamping(__instance);
            float recoilHandDamping = WeaponProperties.RecoilHandDamping(__instance);

            float baseErgo = __instance.Template.Ergonomics;
            float ergoWeightFactor = StatCalc.WeightStatCalc(StatCalc.ErgoWeightMult, magWeight) / 100;
            float currentErgo = WeaponProperties.SDTotalErgo + (WeaponProperties.SDTotalErgo * ((magErgo / 100f) + ergoWeightFactor));
            float totalPureErgo = WeaponProperties.SDPureErgo + (WeaponProperties.SDPureErgo * (magErgo / 100f));
            float pureErgoDelta = (baseErgo - totalPureErgo) / (baseErgo * -1f);

            float baseVRecoil = __instance.Template.RecoilForceUp;
            float vRecoilWeightFactor = StatCalc.WeightStatCalc(StatCalc.VRecoilWeightMult, magWeight) / 100;
            float currentVRecoil = WeaponProperties.SDTotalVRecoil + (WeaponProperties.SDTotalVRecoil * vRecoilWeightFactor);

            float baseHRecoil = __instance.Template.RecoilForceBack;
            float hRecoilWeightFactor = StatCalc.WeightStatCalc(StatCalc.HRecoilWeightMult, magWeight) / 100;
            float currentHRecoil = WeaponProperties.SDTotalHRecoil + (WeaponProperties.SDTotalHRecoil * hRecoilWeightFactor);

            float dispersionWeightFactor = StatCalc.WeightStatCalc(StatCalc.DispersionWeightMult, magWeight) / 100;
            float currentDispersion = WeaponProperties.SDDispersion + (WeaponProperties.SDDispersion * dispersionWeightFactor);

            float currentCamRecoil = WeaponProperties.SDCamRecoil;
            float currentRecoilAngle = WeaponProperties.SDRecoilAngle;

            float magazineTorque = currentTorque;
            currentTorque = WeaponProperties.SDBalance + currentTorque;

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


            StatCalc.WeaponStatCalc(__instance, currentTorque, ref totalTorque, currentErgo, currentVRecoil, currentHRecoil, currentDispersion, currentCamRecoil, currentRecoilAngle, baseErgo, baseVRecoil, baseHRecoil, ref totalErgo, ref totalVRecoil, ref totalHRecoil, ref totalDispersion, ref totalCamRecoil, ref totalRecoilAngle, ref totalRecoilDamping, ref totalRecoilHandDamping, ref totalErgoDelta, ref totalVRecoilDelta, ref totalHRecoilDelta, ref recoilDamping, ref recoilHandDamping, WeaponProperties.SDTotalCOI, WeaponProperties.HasShoulderContact, ref totalCOI, ref totalCOIDelta, __instance.CenterOfImpactBase, false);

            float ergonomicWeight = StatCalc.ErgoWeightCalc(totalWeight, pureErgoDelta, totalErgoDelta);
            float ergonomicWeightLessMag = StatCalc.ErgoWeightCalc(totalWeight, pureErgoDelta, totalErgoDelta);

            Logger.LogInfo("=====================Ergo weight===============");
            Logger.LogInfo("Ergo weight = " + ergonomicWeight);
            Logger.LogInfo("Pure Ergo = " + pureErgoDelta);
            Logger.LogInfo("=====================================");

            float weapTorqueLessMag = totalTorque - magazineTorque;

            float totalReloadSpeedMod = 0;
            float totalFixSpeedMod = 0;
            float totalAimMoveSpeedMod = 0;
            float totalChamberSpeed = 0;

            StatCalc.SpeedStatCalc(ergonomicWeightLessMag, currentReloadSpeed, currentFixSpeed, totalTorque, weapTorqueLessMag, ref totalReloadSpeedMod, ref totalFixSpeedMod, ref totalAimMoveSpeedMod, ergonomicWeight, ref totalChamberSpeed, currentChamberSpeed);

            if (totalReloadSpeedMod < 1)
            {
                totalReloadSpeedMod = totalReloadSpeedMod + 1;
            }
            if (totalFixSpeedMod < 1)
            {
                totalFixSpeedMod = totalFixSpeedMod + 1;
            }
            if (totalChamberSpeed < 1)
            {
                totalChamberSpeed = totalChamberSpeed + 1;
            }

            WeaponProperties.ReloadSpeedModifier = totalReloadSpeedMod;
            WeaponProperties.FixSpeedModifier = Mathf.Max(totalFixSpeedMod, 0.5f);
            WeaponProperties.ChamberSpeed = Mathf.Max(totalChamberSpeed, 0.5f);
            WeaponProperties.AimMoveSpeedModifier = totalAimMoveSpeedMod;


            if (hasMag == true)
            {
                StatCalc.MagReloadSpeedModifier((MagazineClass)magazine, false, false);
            }

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
            WeaponProperties.COIDelta = totalCOIDelta * -1f;
            WeaponProperties.PureErgoDelta = pureErgoDelta;

            return totalErgoDelta;
        }

        public void StatDelta(ref Weapon __instance)
        {
            WeaponProperties._WeapClass = __instance.WeapClass;

            WeaponProperties.ShouldGetSemiIncrease = false;
            if (WeaponProperties._WeapClass != "pistol" || WeaponProperties._WeapClass != "shotgun" || WeaponProperties._WeapClass != "sniperRifle" || WeaponProperties._WeapClass != "smg")
            {
                WeaponProperties.ShouldGetSemiIncrease = true;
            }

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
            float pureErgo = baseErgo;

            float currentTorque = 0f;

            float currentReloadSpeed = 0f;

            float currentAimSpeed = 0f;

            float currentFixSpeed = 0f;

            float currentChamberSpeed = 0f;

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
                if (Helper.IsMagazine(mod) == false)
                {
                    float modWeight = __instance.Mods[i].Weight;
                    float modWeightFactored = StatCalc.FactoredWeight(modWeight);
                    float modErgo = __instance.Mods[i].Ergonomics;
                    float modVRecoil = AttachmentProperties.VerticalRecoil(__instance.Mods[i]);
                    float modHRecoil = AttachmentProperties.HorizontalRecoil(__instance.Mods[i]);
                    float modAutoROF = AttachmentProperties.AutoROF(__instance.Mods[i]);
                    float modSemiROF = AttachmentProperties.SemiROF(__instance.Mods[i]);
                    float modCamRecoil = AttachmentProperties.CameraRecoil(__instance.Mods[i]);
                    float modDispersion = AttachmentProperties.Dispersion(__instance.Mods[i]);
                    float modAngle = AttachmentProperties.RecoilAngle(__instance.Mods[i]);
                    float modAccuracy = __instance.Mods[i].Accuracy;
                    float modReload = AttachmentProperties.ReloadSpeed(__instance.Mods[i]);
                    float modChamber = AttachmentProperties.ChamberSpeed(__instance.Mods[i]);
                    float modAim = AttachmentProperties.AimSpeed(__instance.Mods[i]);
                    float modFix = AttachmentProperties.FixSpeed(__instance.Mods[i]);
                    string modType = AttachmentProperties.ModType(__instance.Mods[i]);
                    string position = StatCalc.GetModPosition(__instance.Mods[i], weapType, weapOpType);

                    StatCalc.ModConditionalStatCalc(__instance, mod, folded, weapType, weapOpType, ref hasShoulderContact, ref modAutoROF, ref modSemiROF, ref stockAllowsFSADS, ref modVRecoil, ref modHRecoil, ref modCamRecoil, ref modAngle, ref modDispersion, ref modErgo, ref modAccuracy, ref modType, ref position, ref modChamber);
                    StatCalc.ModStatCalc(mod, modWeight, ref currentTorque, position, modWeightFactored, modAutoROF, ref currentAutoROF, modSemiROF, ref currentSemiROF, modCamRecoil, ref currentCamRecoil, modDispersion, ref currentDispersion, modAngle, ref currentRecoilAngle, modAccuracy, ref currentCOI, modAim, ref currentAimSpeed, modReload, ref currentReloadSpeed, modFix, ref currentFixSpeed, modErgo, ref currentErgo, modVRecoil, ref currentVRecoil, modHRecoil, ref currentHRecoil, ref currentChamberSpeed, modChamber, false, __instance.WeapClass, ref pureErgo);
                }
            }
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
            WeaponProperties.SDChamberSpeedModifier = currentChamberSpeed;
            WeaponProperties.AimSpeedModifier = currentAimSpeed / 100f;
            WeaponProperties.AutoFireRate = Mathf.Max(300, (int)currentAutoROF);
            WeaponProperties.SemiFireRate = Mathf.Max(200, (int)currentSemiROF);
            WeaponProperties.SDTotalCOI = currentCOI;
            WeaponProperties.SDPureErgo = pureErgo;

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

    public class TotalShotgunDispersionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("get_TotalShotgunDispersion", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref float __result)
        {
            if (__instance?.Owner?.ID != null && __instance.Owner.ID.StartsWith("pmc"))
            {
                float shotDispLessAmmo = __instance.ShotgunDispersionBase * (1f + __instance.CenterOfImpactDelta);
                AmmoTemplate currentAmmoTemplate = __instance.CurrentAmmoTemplate;
                float totalShotDisp = shotDispLessAmmo * ((currentAmmoTemplate != null) ? currentAmmoTemplate.AmmoFactor : 1f);

                __result = totalShotDisp;
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
                    if (Helper.IsStock(mod) == true)
                    {
                        string modType = AttachmentProperties.ModType(mod);
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
                    Logger.LogInfo("=================UDPATE=======================");

                    float _aimsSpeed = (float)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_7").GetValue(__instance);
                    __instance.HandsContainer.Recoil.ReturnSpeed = Plugin.startingConvergence * __instance.Aiming.RecoilConvergenceMult;
                    __instance.HandsContainer.Recoil.Damping = WeaponProperties.TotalRecoilDamping;
                    __instance.HandsContainer.HandsPosition.Damping = WeaponProperties.TotalRecoilHandDamping;
                    float aimSpeed = _aimsSpeed * (1f + WeaponProperties.AimSpeedModifier);
                    WeaponProperties.AimSpeed = aimSpeed;
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
                SkillsClass.GClass1552 skillsClass = (SkillsClass.GClass1552)AccessTools.Field(typeof(EFT.Player.FirearmController), "gclass1552_0").GetValue(__instance);
/*                FirearmsAnimator firearmAnimator = (FirearmsAnimator)AccessTools.Field(typeof(EFT.Player.FirearmController), "firearmsAnimator_0").GetValue(__instance);
*/
                PlayerProperties.ReloadSkillMulti = skillsClass.ReloadSpeed;
                PlayerProperties.FixSkillMulti = skillsClass.FixSpeed;


            }
        }
    }

    public class SetAimingSlowdownPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass1477).GetMethod("SetAimingSlowdown", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(ref GClass1477 __instance, bool isAiming, float slow)
        {

            Player player = (Player)AccessTools.Field(typeof(GClass1477), "player_0").GetValue(__instance);
            if (player.HandsController.Item.Owner.ID.StartsWith("pmc"))
            {
                if (isAiming)
                {
                    //slow is hard set to 0.33 when called, 0.4-0.43 feels best.
                    slow += 0.10f;
                    __instance.AddStateSpeedLimit(Math.Max((slow) + WeaponProperties.AimMoveSpeedModifier, 0.15f), Player.ESpeedLimit.Aiming);
                    return false;
                }
                __instance.RemoveStateSpeedLimit(Player.ESpeedLimit.Aiming);
                return false;
            }
            return true;
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
                    Player player = (Player)AccessTools.Field(typeof(EFT.Player.ItemHandsController), "_player").GetValue(firearmController);
                    float originalAimSpeed = WeaponProperties.AimSpeed;
                    Mod currentAimingMod = (player.ProceduralWeaponAnimation.CurrentAimingMod != null) ? player.ProceduralWeaponAnimation.CurrentAimingMod.Item as Mod : null;
                    float sightSpeedModi = (currentAimingMod != null) ? AttachmentProperties.AimSpeed(currentAimingMod) : 1;
                    float newAimSpeed = originalAimSpeed * (1 + (sightSpeedModi / 100f));
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_7").SetValue(__instance, newAimSpeed);

                    Logger.LogInfo("_aimsSpeed = " + originalAimSpeed);
                    Logger.LogInfo("currentAimingMod = " + (currentAimingMod != null ? currentAimingMod.LocalizedName() : ""));
                    Logger.LogInfo("sightSpeedModi = " + sightSpeedModi);
                    Logger.LogInfo("aimSpeed = " + newAimSpeed);

                    float ergoWeightFactor = (WeaponProperties.ErgonomicWeight / 250) + 1f;
                    float breathIntensity = Mathf.Min(0.64f * ergoWeightFactor, 1.15f);
                    float handsIntensity = Mathf.Min(0.59f * ergoWeightFactor, 1.15f);

                    if (WeaponProperties.HasShoulderContact == false)
                    {
                        breathIntensity = Mathf.Min(0.9f * ergoWeightFactor, 1.25f);
                        handsIntensity = Mathf.Min(0.9f * ergoWeightFactor, 1.25f);
                    }
                    if (firearmController.Item.WeapClass == "pistol" && WeaponProperties.HasShoulderContact != true)
                    {
                        breathIntensity = Mathf.Min(0.78f * ergoWeightFactor, 1.25f);
                        handsIntensity = Mathf.Min(0.73f * ergoWeightFactor, 1.25f);
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

        [PatchPrefix]
        private static bool Prefix(ref EFT.Animations.ProceduralWeaponAnimation __instance)
        {
            Player.FirearmController firearmController = (Player.FirearmController)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "firearmController_0").GetValue(__instance);
            if (firearmController != null)
            {
                if (firearmController.Item.Owner.ID.StartsWith("pmc"))
                {
                    float ergoWeight = WeaponProperties.ErgonomicWeight;
                    float weightFactor = StatCalc.ProceduralIntensityFactorCalc(ergoWeight, 20f);
                    float displacementModifier = 0.45f;//lower = less drag
                    float aimIntensity = __instance.IntensityByAiming * 0.42f;

                    if (WeaponProperties.HasShoulderContact == false && firearmController.Item.WeapClass != "pistol")
                    {
                        aimIntensity = __instance.IntensityByAiming * 1f;
                    } 
       /*             if (firearmController.Item.WeapClass == "pistol")
                    {
                        aimIntensity = __instance.IntensityByAiming * 0.45f;
                    }*/

                    Logger.LogInfo("===================Sway==================");
                    Logger.LogInfo("ergoWeight = " + ergoWeight);
                    Logger.LogInfo("weightFactor = " + weightFactor);

                    float swayStrength = EFTHardSettings.Instance.SWAY_STRENGTH_PER_KG.Evaluate(ergoWeight * (1f + __instance.Overweight));
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_18").SetValue(__instance, swayStrength);

                    float weapDisplacement = EFTHardSettings.Instance.DISPLACEMENT_STRENGTH_PER_KG.Evaluate(ergoWeight * (1f + __instance.Overweight));//delay from moving mouse to the weapon moving to center of screen.
                    AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "float_19").SetValue(__instance, weapDisplacement * weightFactor * displacementModifier);

                    Logger.LogInfo("weapDisplacement = " + weapDisplacement * weightFactor * displacementModifier);
                    Logger.LogInfo("SwayFactors = " + Mathf.Clamp(aimIntensity * weightFactor, aimIntensity, 1.1f));
                    Logger.LogInfo("=======================================");
                    __instance.MotionReact.SwayFactors = new Vector3(swayStrength, __instance.IsAiming ? (swayStrength * 0.3f) : swayStrength, swayStrength) * Mathf.Clamp(aimIntensity * weightFactor, aimIntensity, 1.1f); // the diving/tiling animation as you move weapon side to side.
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }
    }
}
