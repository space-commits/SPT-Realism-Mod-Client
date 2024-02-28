
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using static EFT.Player;
using System.Linq;
using WeaponSkills = EFT.SkillManager.GClass1768;

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
            if (__instance?.Owner?.ID != null && __instance.Owner.ID == Singleton<GameWorld>.Instance.MainPlayer.ProfileId)
            {
                AmmoTemplate currentAmmoTemplate = __instance.CurrentAmmoTemplate;
                __result = (currentAmmoTemplate != null) ? (int)(WeaponStats.SemiFireRate * currentAmmoTemplate.casingMass) : WeaponStats.SemiFireRate;
                return false;
            }
            return true;
        }
    }

    public class AutoFireRatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("get_FireRate", BindingFlags.Instance | BindingFlags.Public);

        }

        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref int __result)
        {
            if (__instance?.Owner?.ID != null && __instance.Owner.ID == Singleton<GameWorld>.Instance.MainPlayer.ProfileId)
            {
                AmmoTemplate currentAmmoTemplate = __instance.CurrentAmmoTemplate;
                __result = (currentAmmoTemplate != null) ? (int)(WeaponStats.AutoFireRate * currentAmmoTemplate.casingMass) : WeaponStats.AutoFireRate;
                __result = Utils.Verified ? __result * 10 : __result;  
                return false;
            }
            return true;
        }
    }


    //this method sets player weapon ergo value. For some reason I've removed the injury penalty? Probably because I already apply injury mulit myself 
    public class PlayerErgoPatch : ModulePatch
    {
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            return typeof(Player.FirearmController).GetMethod("method_9", BindingFlags.Instance | BindingFlags.Public);

        }
        [PatchPrefix]
        private static bool Prefix(Player.FirearmController __instance, ref float __result)
        {
            //to find this method again, look for this._player.MovementContext.PhysicalConditionContainsAny(EPhysicalCondition.LeftArmDamaged | EPhysicalCondition.RightArmDamaged)
            //return Mathf.Max(0f, this.Item.ErgonomicsTotal * (1f + this.gclass1560_0.DeltaErgonomics + this._player.ErgonomicsPenalty));

            Player player = (Player)playerField.GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                WeaponSkills skillsClass = (WeaponSkills)AccessTools.Field(typeof(EFT.Player.FirearmController), "gclass1768_0").GetValue(__instance);
                __result = Mathf.Max(0f, __instance.Item.ErgonomicsTotal * (1f + skillsClass.DeltaErgonomics + player.ErgonomicsPenalty));
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
        private static bool Prefix(Weapon __instance, ref float __result)
        {
            if (__instance?.Owner?.ID != null && __instance.Owner.ID == Singleton<GameWorld>.Instance.MainPlayer.ProfileId)
            {
                if (PlayerState.IsInReloadOpertation)
                {
                    __result = FinalStatCalc(__instance);
                }
                else
                {
                    InitialStaCalc(__instance);
                    __result = FinalStatCalc(__instance);
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        public static float FinalStatCalc(Weapon __instance)
        {
            WeaponStats._WeapClass = __instance.WeapClass;
            bool isManual = WeaponStats.IsManuallyOperated(__instance);
            WeaponStats._IsManuallyOperated = isManual;

            float totalWeight = __instance.GetSingleItemTotalWeight();
            string weapType = WeaponStats.WeaponType(__instance);
            string weapOpType = WeaponStats.OperationType(__instance);

            Mod magazine = __instance.GetCurrentMagazine();
            float magErgo = 0;
            float magWeight = 0;
            float currentTorque = 0;
            bool hasMag = magazine != null;

            if (hasMag == true)
            {
                magWeight = magazine.GetSingleItemTotalWeight();
                float magWeightFactored = StatCalc.FactoredWeight(magWeight);
                string position = StatCalc.GetModPosition(magazine, weapType, weapOpType, "");
                magErgo = magazine.Ergonomics;
                currentTorque = StatCalc.GetTorque(position, magWeightFactored, __instance.WeapClass);
            }

            float weapWeightLessMag = totalWeight - magWeight;

            float totalReloadSpeedMod = WeaponStats.SDReloadSpeedModifier;

            float totalChamberSpeedMod = WeaponStats.SDChamberSpeedModifier;

            float recoilDamping = WeaponStats.RecoilDamping(__instance);
            float recoilHandDamping = WeaponStats.RecoilHandDamping(__instance);

            float baseErgo = __instance.Template.Ergonomics;
            float ergoWeightFactor = StatCalc.WeightStatCalc(StatCalc.ErgoWeightMult, __instance.IsBeltMachineGun ? magWeight * 0.5f : magWeight) / 100;
            float currentErgo = WeaponStats.InitTotalErgo + (WeaponStats.InitTotalErgo * ((magErgo / 100f) + ergoWeightFactor));
            float currentPureErgo = WeaponStats.InitPureErgo + (WeaponStats.InitPureErgo * (magErgo / 100f));

            float baseVRecoil = __instance.Template.RecoilForceUp;
            float vRecoilWeightFactor = StatCalc.WeightStatCalc(StatCalc.VRecoilWeightMult, magWeight) / 100;
            float currentVRecoil = WeaponStats.InitTotalVRecoil + (WeaponStats.InitTotalVRecoil * vRecoilWeightFactor);

            float baseHRecoil = __instance.Template.RecoilForceBack;
            float hRecoilWeightFactor = StatCalc.WeightStatCalc(StatCalc.HRecoilWeightMult, magWeight) / 100;
            float currentHRecoil = WeaponStats.InitTotalHRecoil + (WeaponStats.InitTotalHRecoil * hRecoilWeightFactor);

            float dispersionWeightFactor = StatCalc.WeightStatCalc(StatCalc.DispersionWeightMult, magWeight) / 100;
            float currentDispersion = WeaponStats.InitDispersion + (WeaponStats.InitDispersion * dispersionWeightFactor);

            float currentCamRecoil = WeaponStats.InitCamRecoil;
            float currentRecoilAngle = WeaponStats.InitRecoilAngle;

            float magazineTorque = currentTorque;
            currentTorque = WeaponStats.InitBalance + currentTorque;

            float totalTorque = 0f;
            float totalErgo = 0f;
            float totalVRecoil = 0f;
            float totalHRecoil = 0f;
            float totalDispersion = 0f;
            float totalCamRecoil = 0f;
            float totalRecoilAngle = 0f;
            float totalRecoilDamping = 0f;
            float totalRecoilHandDamping = 0f;

            float totalErgoDelta = 0f;
            float totalPureErgoDelta = 0f;
            float totalVRecoilDelta = 0f;
            float totalHRecoilDelta = 0f;

            float totalCOI = 0f;
            float totalCOIDelta = 0f;


            StatCalc.WeaponStatCalc(__instance, currentTorque, ref totalTorque, currentErgo, currentVRecoil, currentHRecoil, currentDispersion, currentCamRecoil, currentRecoilAngle, baseErgo, baseVRecoil, baseHRecoil, ref totalErgo, ref totalVRecoil, ref totalHRecoil, ref totalDispersion, ref totalCamRecoil, ref totalRecoilAngle, ref totalRecoilDamping, ref totalRecoilHandDamping, ref totalErgoDelta, ref totalVRecoilDelta, ref totalHRecoilDelta, ref recoilDamping, ref recoilHandDamping, WeaponStats.InitTotalCOI, WeaponStats.HasShoulderContact, ref totalCOI, ref totalCOIDelta, __instance.CenterOfImpactBase, currentPureErgo, ref totalPureErgoDelta, false);

            float ergonomicWeight = StatCalc.ErgoWeightCalc(totalWeight, totalPureErgoDelta, totalTorque, __instance.WeapClass);
            float ergonomicWeightLessMag = StatCalc.ErgoWeightCalc(weapWeightLessMag, totalPureErgoDelta, totalTorque, __instance.WeapClass);

            float ergoFactor = Mathf.Max(1, 80f - totalErgo); //as an experiment, use total ergo as ergonomicWeight
            float ergoFactorLessMag = Mathf.Max(1, 80f - WeaponStats.InitTotalErgo);  //as an experiment, use total ergo as ergonomicWeight

            Utils.HasRunErgoWeightCalc = true;

            float totalAimMoveSpeedFactor = 0;
            float totalReloadSpeedLessMag = 0;
            float totalChamberSpeed = 0;
            float totalFiringChamberSpeed = 0;
            float totalChamberCheckSpeed = 0;
            float totalFixSpeed = 0;

            StatCalc.SpeedStatCalc(__instance, ergoFactor, ergoFactorLessMag, totalChamberSpeedMod, totalReloadSpeedMod, ref totalReloadSpeedLessMag, ref totalChamberSpeed, ref totalAimMoveSpeedFactor, ref totalFiringChamberSpeed, ref totalChamberCheckSpeed, ref totalFixSpeed);

            WeaponStats.TotalFixSpeed = totalFixSpeed;
            WeaponStats.TotalChamberCheckSpeed = totalChamberCheckSpeed;
            WeaponStats.TotalReloadSpeedLessMag = totalReloadSpeedLessMag;
            WeaponStats.TotalChamberSpeed = totalChamberSpeed;
            WeaponStats.TotalFiringChamberSpeed = totalFiringChamberSpeed;
            WeaponStats.AimMoveSpeedWeapModifier = totalAimMoveSpeedFactor;

            if (hasMag == true)
            {
                StatCalc.MagReloadSpeedModifier(__instance, (MagazineClass)magazine, false, false);
            }

            if (Plugin.EnableLogging.Value == true)
            {
                Logger.LogWarning("Shoulder = " + WeaponStats.HasShoulderContact);
                Logger.LogWarning("Total Ergo = " + totalErgo);
                Logger.LogWarning("Total Ergo D = " + totalErgoDelta);
                Logger.LogWarning("Ergo factor = " + ergoFactor);
                Logger.LogWarning("Pure Ergo = " + currentPureErgo);
                Logger.LogWarning("Pure Ergo D = " + totalPureErgoDelta);

                Logger.LogWarning("Dispersion = " + totalDispersion);
                Logger.LogWarning("Dispersion Delta = " + (totalDispersion - __instance.Template.RecolDispersion));
                Logger.LogWarning("Cam Recoil = " + totalCamRecoil);
                Logger.LogWarning("Total V Recoil = " + totalVRecoil);
                Logger.LogWarning("Total H Recoil = " + totalHRecoil);
                Logger.LogWarning("Balance = " + totalTorque);
                Logger.LogWarning("COIDelta = " + totalCOIDelta);
            }


            WeaponStats.TotalDispersion = totalDispersion;
            WeaponStats.TotalDispersionDelta = (totalDispersion - __instance.Template.RecolDispersion) / __instance.Template.RecolDispersion;
            WeaponStats.TotalCamRecoil = totalCamRecoil;
            WeaponStats.TotalRecoilAngle = Mathf.Max(totalRecoilAngle, 65f);
            WeaponStats.TotalVRecoil = totalVRecoil;
            WeaponStats.TotalHRecoil = totalHRecoil;
            WeaponStats.Balance = totalTorque;
            WeaponStats.TotalErgo = totalErgo;
            WeaponStats.ErgoDelta = totalErgoDelta;
            WeaponStats.VRecoilDelta = totalVRecoilDelta;
            WeaponStats.HRecoilDelta = totalHRecoilDelta;
            WeaponStats.ErgoFactor = Mathf.Max(1, 80f - totalErgo);  //as an experiment, use total ergo as ergonomicWeight
            WeaponStats.ErgonomicWeight = ergonomicWeight;
            WeaponStats.TotalRecoilDamping = totalRecoilDamping;
            WeaponStats.TotalRecoilHandDamping = totalRecoilHandDamping;
            WeaponStats.COIDelta = totalCOIDelta;
            WeaponStats.PureErgoDelta = totalPureErgoDelta;
            return totalErgoDelta;
        }

        public static void InitialStaCalc(Weapon __instance)
        {
            WeaponStats._WeapClass = __instance.WeapClass;
            bool isManual = WeaponStats.IsManuallyOperated(__instance);
            WeaponStats._IsManuallyOperated = isManual;

            WeaponStats.ShouldGetSemiIncrease = false;
            if (__instance.WeapClass != "pistol" || __instance.WeapClass != "shotgun" || __instance.WeapClass != "sniperRifle" || __instance.WeapClass != "smg")
            {
                WeaponStats.ShouldGetSemiIncrease = true;
            }

            float baseCOI = __instance.CenterOfImpactBase;
            float currentCOI = baseCOI;

            float baseAutoROF = __instance.Template.bFirerate;
            float currentAutoROF = baseAutoROF;

            float baseSemiROF = Mathf.Max(__instance.Template.SingleFireRate, 240);
            float currentSemiROF = baseSemiROF;

            float baseCamRecoil = __instance.Template.RecoilCamera;
            float currentCamRecoil = baseCamRecoil;

            float baseCamReturnSpeed = WeaponStats.CameraReturnSpeed(__instance);
            float currentCamReturnSpeed = baseCamReturnSpeed;

            float baseConv = __instance.Template.RecoilReturnSpeedHandRotation;
            float currentConv = baseConv;

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

            float pureRecoil = baseVRecoil + baseHRecoil;

            float baseShotDisp = __instance.ShotgunDispersionBase;
            float currentShotDisp = baseShotDisp;

            float currentTorque = 0f;

            float currentReloadSpeedMod = 0f;

            float currentAimSpeedMod = 0f;

            float currentChamberSpeedMod = 0f;

            float modBurnRatio = 1;

            float baseMalfChance = __instance.BaseMalfunctionChance;
            float currentMalfChance = baseMalfChance;

            string weapOpType = WeaponStats.OperationType(__instance);
            string weapType = WeaponStats.WeaponType(__instance);

            string caliber = __instance.AmmoCaliber;
            float currentLoudness = 0;

            bool weaponAllowsFSADS = WeaponStats.WeaponAllowsADS(__instance);
            bool stockAllowsFSADS = false;

            bool canCycleSubs = false;

            float currentFixSpeedMod = 0f;

            bool folded = __instance.Folded;
            bool hasShoulderContact = false;
            if (WeaponStats.WepHasShoulderContact(__instance) && !folded)
            {
                hasShoulderContact = true;
            }
            WeaponStats.BaseMeleeDamage = 0f; //reset the melee dmg
            WeaponStats.BaseMeleePen = 0f;
            WeaponStats.HasBayonet = false;
            WeaponStats.HasBooster = false;

            foreach (Mod mod in __instance.Mods)
            {
                if (!Utils.IsMagazine(mod))
                {
                    float modWeight = mod.Weight;
                    float modWeightFactored = StatCalc.FactoredWeight(modWeight);
                    float modErgo = mod.Ergonomics;
                    float modVRecoil = AttachmentProperties.VerticalRecoil(mod);
                    float modHRecoil = AttachmentProperties.HorizontalRecoil(mod);
                    float modAutoROF = AttachmentProperties.AutoROF(mod);
                    float modSemiROF = AttachmentProperties.SemiROF(mod);
                    float modCamRecoil = AttachmentProperties.CameraRecoil(mod);
                    float modConv = AttachmentProperties.ModConvergence(mod);
                    float modDispersion = AttachmentProperties.Dispersion(mod);
                    float modAngle = AttachmentProperties.RecoilAngle(mod);
                    float modAccuracy = mod.Accuracy;
                    float modReload = AttachmentProperties.ReloadSpeed(mod);
                    float modChamber = AttachmentProperties.ChamberSpeed(mod);
                    float modAim = AttachmentProperties.AimSpeed(mod);
                    float modShotDisp = AttachmentProperties.ModShotDispersion(mod);
                    string modType = AttachmentProperties.ModType(mod);
                    string position = StatCalc.GetModPosition(mod, weapType, weapOpType, modType);
                    float modLoudness = mod.Loudness;
                    float modMalfChance = AttachmentProperties.ModMalfunctionChance(mod);
                    float modDuraBurn = mod.DurabilityBurnModificator;
                    float modFix = AttachmentProperties.FixSpeed(mod);
                    modVRecoil += modConv > 0f ? modConv * -1f : 0f;

                    if (Utils.IsMuzzleDevice(mod))
                    {
                        if (modType == "bayonet") WeaponStats.HasBayonet = true;
                        if (modType == "booster") WeaponStats.HasBooster = true;
            
                        WeaponStats.BaseMeleeDamage = AttachmentProperties.ModMeleeDamage(mod);
                        WeaponStats.BaseMeleePen = AttachmentProperties.ModMeleePen(mod);
                    }

                    StatCalc.ModConditionalStatCalc(__instance, mod, folded, weapType, weapOpType, ref hasShoulderContact, ref modAutoROF, ref modSemiROF, ref stockAllowsFSADS, ref modVRecoil, ref modHRecoil, ref modCamRecoil, ref modAngle, ref modDispersion, ref modErgo, ref modAccuracy, ref modType, ref position, ref modChamber, ref modLoudness, ref modMalfChance, ref modDuraBurn, ref modConv);
                    StatCalc.ModStatCalc(mod, modWeight, ref currentTorque, position, modWeightFactored, modAutoROF, ref currentAutoROF, modSemiROF, ref currentSemiROF, modCamRecoil, ref currentCamRecoil, modDispersion, ref currentDispersion, modAngle, ref currentRecoilAngle, modAccuracy, ref currentCOI, modAim, ref currentAimSpeedMod, modReload, ref currentReloadSpeedMod, modFix, ref currentFixSpeedMod, modErgo, ref currentErgo, modVRecoil, ref currentVRecoil, modHRecoil, ref currentHRecoil, ref currentChamberSpeedMod, modChamber, false, __instance.WeapClass, ref pureErgo, modShotDisp, ref currentShotDisp, modLoudness, ref currentLoudness, ref currentMalfChance, modMalfChance, ref pureRecoil, ref currentConv, modConv, ref currentCamReturnSpeed, __instance.IsBeltMachineGun);
                    if (AttachmentProperties.CanCylceSubs(mod))
                    {
                        canCycleSubs = true;
                    }
                    modBurnRatio *= modDuraBurn;
                }
            }
            if (weaponAllowsFSADS == true || stockAllowsFSADS == true)
            {
                WeaponStats.WeaponCanFSADS = true;
            }
            else
            {
                WeaponStats.WeaponCanFSADS = !hasShoulderContact;
            }

            WeaponStats.IsStocklessPistol = !hasShoulderContact && __instance.WeapClass == "pistol" ? true : false;

            float totalLoudness = ((currentLoudness / 80) + 1f) * StatCalc.CaliberLoudnessFactor(caliber);
            Logger.LogWarning("caliber " + caliber);
            Logger.LogWarning("loudness " + totalLoudness);
            if (weapType == "bullpup")
            {
                totalLoudness *= 1.15f;
            }

            float pureRecoilDelta = ((baseVRecoil + baseHRecoil) - pureRecoil) / ((baseVRecoil + baseHRecoil) * -1f);
            WeaponStats.TotalModDuraBurn = modBurnRatio;
            WeaponStats.TotalMalfChance = currentMalfChance;
            WeaponStats.MalfChanceDelta = (currentMalfChance - baseMalfChance) / baseMalfChance;
            DeafeningController.WeaponDeafFactor = totalLoudness;
            WeaponStats.CanCycleSubs = canCycleSubs;
            WeaponStats.HasShoulderContact = hasShoulderContact;
            WeaponStats.InitTotalErgo = currentErgo;
            WeaponStats.InitTotalVRecoil = currentVRecoil;
            WeaponStats.InitTotalHRecoil = currentHRecoil;
            WeaponStats.InitBalance = currentTorque;
            WeaponStats.InitCamRecoil = currentCamRecoil;
            WeaponStats.InitDispersion = currentDispersion;
            WeaponStats.InitRecoilAngle = currentRecoilAngle;
            WeaponStats.SDReloadSpeedModifier = currentReloadSpeedMod;
            WeaponStats.SDChamberSpeedModifier = currentChamberSpeedMod;
            WeaponStats.SDFixSpeedModifier = currentFixSpeedMod;
            WeaponStats.ModAimSpeedModifier = currentAimSpeedMod / 100f;
            WeaponStats.AutoFireRate = Mathf.Max(300, (int)currentAutoROF);
            WeaponStats.SemiFireRate = Mathf.Max(200, (int)currentSemiROF);
            WeaponStats.InitTotalCOI = currentCOI;
            WeaponStats.InitPureErgo = pureErgo;
            WeaponStats.PureRecoilDelta = pureRecoilDelta;
            WeaponStats.ShotDispDelta = (baseShotDisp - currentShotDisp) / (baseShotDisp * -1f);
            WeaponStats.TotalCameraReturnSpeed = currentCamReturnSpeed;
            WeaponStats.TotalModdedConv = currentConv;
            WeaponStats.ConvergenceDelta = currentConv / __instance.Template.RecoilReturnSpeedHandRotation;
        }
    }

    public class COIDeltaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("get_CenterOfImpactDelta", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref float __result)
        {

            if (__instance?.Owner?.ID != null && __instance.Owner.ID == Singleton<GameWorld>.Instance.MainPlayer.ProfileId)
            {
                __result = WeaponStats.COIDelta;
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public class GetTotalCenterOfImpactPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("GetTotalCenterOfImpact", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref float __result, bool includeAmmo)
        {

            if (__instance?.Owner?.ID != null && __instance.Owner.ID == Singleton<GameWorld>.Instance.MainPlayer.ProfileId)
            {
                float currentSightFactor = 1f;
                if (Utils.IsReady)
                {
                    int iterations = 0;
                    Player player = Utils.GetPlayer();
                    Mod currentAimingMod = (player.ProceduralWeaponAnimation.CurrentAimingMod != null) ? player.ProceduralWeaponAnimation.CurrentAimingMod.Item as Mod : null;

                    if (currentAimingMod != null)
                    {
                        if (AttachmentProperties.ModType(currentAimingMod) == "sight")
                        {
                            currentSightFactor += currentAimingMod.Accuracy / 100f;
                        }
                        IEnumerable<Item> parents = currentAimingMod.GetAllParentItems();
                        foreach (Item item in parents)
                        {
                            if (item is Mod && AttachmentProperties.ModType(item) == "mount")
                            {
                                Mod mod = item as Mod;
                                currentSightFactor += (mod.Accuracy / 100f);
                            }
                            iterations++;
                            if (iterations >= 5 || !(item is Mod))
                            {
                                break;
                            }
                        }
                    }
                }

                WeaponStats.ScopeAccuracyFactor = currentSightFactor;
                bool isBracingTop = StanceController.BracingDirection == EBracingDirection.Top;
                float mountingFactor = StanceController.IsBracing && isBracingTop ? 1.05f : StanceController.IsBracing && !isBracingTop ? 1.025f : StanceController.IsMounting && isBracingTop ? 1.1f : StanceController.IsMounting && !isBracingTop ? 1.075f : 1f;
                float totalCoi = 2 * (__instance.CenterOfImpactBase * (1f + __instance.CenterOfImpactDelta)) * currentSightFactor * mountingFactor;

                if (!includeAmmo)
                {
                    __result = totalCoi;
                    return false;
                }

                AmmoTemplate currentAmmoTemplate = __instance.CurrentAmmoTemplate;
                __result = totalCoi * ((currentAmmoTemplate != null) ? currentAmmoTemplate.AmmoFactor : 1f);
                return false;
            }
            return true;
        }
    }


    public class TotalShotgunDispersionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("get_TotalShotgunDispersion", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref float __result)
        {
            if (__instance?.Owner?.ID != null && __instance.Owner.ID == Singleton<GameWorld>.Instance.MainPlayer.ProfileId)
            {
                float shotDispLessAmmo = __instance.ShotgunDispersionBase * (1f + WeaponStats.ShotDispDelta);
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

    public class ErgoWeightPatch : ModulePatch
    {
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            return typeof(Player.FirearmController).GetMethod("get_ErgonomicWeight", BindingFlags.Instance | BindingFlags.Public | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(Player.FirearmController __instance, ref float __result)
        {
            Player player = (Player)playerField.GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                __result = WeaponStats.ErgoFactor * PlayerState.ErgoDeltaInjuryMulti * (1f - PlayerState.StrengthSkillAimBuff * 1.5f);

                if (!Utils.HasRunErgoWeightCalc)
                {
                    __result = 0;
                    return false;
                }

                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("===ErgonomicWeight===");
                    Logger.LogWarning("total ergo weight = " + __result);
                    Logger.LogWarning("base ergo weight = " + WeaponStats.ErgoFactor);
                    Logger.LogWarning("ergoweight ergo weight = " + WeaponStats.ErgonomicWeight);
                }

                return false;
            }
            else
            {
                return true;
            }
        }
    }


    public class UpdateHipInaccuracyPatch : ModulePatch
    {
        private static FieldInfo playerField;

        protected override MethodBase GetTargetMethod()
        {
            playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            return typeof(EFT.Player.FirearmController).GetMethod("UpdateHipInaccuracy", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player.FirearmController __instance)
        {
            Player player = (Player)playerField.GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                float convergenceFactor = 1f - (RecoilController.BaseTotalConvergence / 100f);
                float dispersionFactor = 1f + (RecoilController.BaseTotalDispersion / 100f);
                float recoilFactor = 1f + (RecoilController.BaseTotalVRecoil + RecoilController.BaseTotalHRecoil) / 100f;
                float totalPlayerWeight = PlayerState.TotalModifiedWeightMinusWeapon;
                float playerWeightFactorBuff = 1f + (totalPlayerWeight / 100f);

                WeaponStats.BaseHipfireInaccuracy = Mathf.Clamp(0.3f * player.ProceduralWeaponAnimation.Breath.HipPenalty * (1f - WeaponStats.ErgoDelta) * convergenceFactor * dispersionFactor * recoilFactor * playerWeightFactorBuff, 0.3f, 1f);
            }
        }
    }
}
