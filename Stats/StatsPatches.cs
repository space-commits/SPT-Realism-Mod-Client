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
            if (__instance?.Owner?.ID != null && (__instance.Owner.ID.StartsWith("pmc") || __instance.Owner.ID.StartsWith("scav")))
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
            if (__instance?.Owner?.ID != null && (__instance.Owner.ID.StartsWith("pmc") || __instance.Owner.ID.StartsWith("scav")))
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


    //this method sets player weapon ergo value. For some reason I've removed the injury penalty? Probably because I already apply injury mulit myself 
    public class method_9Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("method_9", BindingFlags.Instance | BindingFlags.NonPublic);

        }
        [PatchPrefix]
        private static bool Prefix(ref Player.FirearmController __instance, ref float __result)
        {   
            //to find this method again, look for this._player.MovementContext.PhysicalConditionContainsAny(EPhysicalCondition.LeftArmDamaged | EPhysicalCondition.RightArmDamaged)
            //return Mathf.Max(0f, this.Item.ErgonomicsTotal * (1f + this.gclass1560_0.DeltaErgonomics + this._player.ErgonomicsPenalty));

            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                SkillsClass.GClass1681 skillsClass = (SkillsClass.GClass1681)AccessTools.Field(typeof(EFT.Player.FirearmController), "gclass1681_0").GetValue(__instance);
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
        private static bool Prefix(ref Weapon __instance, ref float __result)
        {
            if (__instance?.Owner?.ID != null && (__instance.Owner.ID.StartsWith("pmc") || __instance.Owner.ID.StartsWith("scav")))
            {
                ErgoDeltaPatch p = new ErgoDeltaPatch();
                if (PlayerProperties.IsInReloadOpertation)
                {
                    __result = p.FinalStatCalc(ref __instance);
                }
                else
                {
                    p.InitialStaCalc(ref __instance);
                    __result = p.FinalStatCalc(ref __instance);
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        public float FinalStatCalc(ref Weapon __instance)
        {
            WeaponProperties._WeapClass = __instance.WeapClass;
            bool isManual = WeaponProperties.IsManuallyOperated(__instance);
            WeaponProperties._IsManuallyOperated = isManual;

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
                string position = StatCalc.GetModPosition(magazine, weapType, weapOpType, "");
                magErgo = magazine.Ergonomics;
                currentTorque = StatCalc.GetTorque(position, magWeightFactored, __instance.WeapClass);
            }

            float weapWeightLessMag = totalWeight - magWeight;

            float totalReloadSpeedMod = WeaponProperties.SDReloadSpeedModifier;

            float totalChamberSpeedMod = WeaponProperties.SDChamberSpeedModifier;

            float recoilDamping = WeaponProperties.RecoilDamping(__instance);
            float recoilHandDamping = WeaponProperties.RecoilHandDamping(__instance);

            float baseErgo = __instance.Template.Ergonomics;
            float ergoWeightFactor = StatCalc.WeightStatCalc(StatCalc.ErgoWeightMult, magWeight) / 100;
            float currentErgo = WeaponProperties.InitTotalErgo + (WeaponProperties.InitTotalErgo * ((magErgo / 100f) + ergoWeightFactor));
            float currentPureErgo = WeaponProperties.InitPureErgo + (WeaponProperties.InitPureErgo * (magErgo / 100f));

            float baseVRecoil = __instance.Template.RecoilForceUp;
            float vRecoilWeightFactor = StatCalc.WeightStatCalc(StatCalc.VRecoilWeightMult, magWeight) / 100;
            float currentVRecoil = WeaponProperties.InitTotalVRecoil + (WeaponProperties.InitTotalVRecoil * vRecoilWeightFactor);

            float baseHRecoil = __instance.Template.RecoilForceBack;
            float hRecoilWeightFactor = StatCalc.WeightStatCalc(StatCalc.HRecoilWeightMult, magWeight) / 100;
            float currentHRecoil = WeaponProperties.InitTotalHRecoil + (WeaponProperties.InitTotalHRecoil * hRecoilWeightFactor);

            float dispersionWeightFactor = StatCalc.WeightStatCalc(StatCalc.DispersionWeightMult, magWeight) / 100;
            float currentDispersion = WeaponProperties.InitDispersion + (WeaponProperties.InitDispersion * dispersionWeightFactor);

            float currentCamRecoil = WeaponProperties.InitCamRecoil;
            float currentRecoilAngle = WeaponProperties.InitRecoilAngle;

            float magazineTorque = currentTorque;
            currentTorque = WeaponProperties.InitBalance + currentTorque;

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


            StatCalc.WeaponStatCalc(__instance, currentTorque, ref totalTorque, currentErgo, currentVRecoil, currentHRecoil, currentDispersion, currentCamRecoil, currentRecoilAngle, baseErgo, baseVRecoil, baseHRecoil, ref totalErgo, ref totalVRecoil, ref totalHRecoil, ref totalDispersion, ref totalCamRecoil, ref totalRecoilAngle, ref totalRecoilDamping, ref totalRecoilHandDamping, ref totalErgoDelta, ref totalVRecoilDelta, ref totalHRecoilDelta, ref recoilDamping, ref recoilHandDamping, WeaponProperties.InitTotalCOI, WeaponProperties.HasShoulderContact, ref totalCOI, ref totalCOIDelta, __instance.CenterOfImpactBase, currentPureErgo, ref totalPureErgoDelta,  false);

            float ergonomicWeight = StatCalc.ErgoWeightCalc(totalWeight, totalPureErgoDelta, totalTorque, __instance.WeapClass);
            float ergonomicWeightLessMag = StatCalc.ErgoWeightCalc(weapWeightLessMag, totalPureErgoDelta, totalTorque, __instance.WeapClass);
            Utils.HasRunErgoWeightCalc = true;

            float totalAimMoveSpeedFactor = 0;
            float totalReloadSpeedLessMag = 0;
            float totalChamberSpeed = 0;
            float totalFiringChamberSpeed = 0;
            float totalChamberCheckSpeed = 0;
            float totalFixSpeed = 0;

            StatCalc.SpeedStatCalc(__instance, ergonomicWeight, ergonomicWeightLessMag, totalChamberSpeedMod, totalReloadSpeedMod, ref totalReloadSpeedLessMag, ref totalChamberSpeed, ref totalAimMoveSpeedFactor, ref totalFiringChamberSpeed, ref totalChamberCheckSpeed, ref totalFixSpeed);

            WeaponProperties.TotalFixSpeed = totalFixSpeed;
            WeaponProperties.TotalChamberCheckSpeed = totalChamberCheckSpeed;
            WeaponProperties.TotalReloadSpeedLessMag = totalReloadSpeedLessMag;
            WeaponProperties.TotalChamberSpeed = totalChamberSpeed;
            WeaponProperties.TotalFiringChamberSpeed = totalFiringChamberSpeed;
            WeaponProperties.AimMoveSpeedModifier = totalAimMoveSpeedFactor;

            if (hasMag == true)
            {
                StatCalc.MagReloadSpeedModifier(__instance, (MagazineClass)magazine, false, false);
            }

            if (Plugin.EnableLogging.Value == true)
            {
                Logger.LogWarning("Total Ergo = " + totalErgo);
                Logger.LogWarning("Total Ergo D = " + totalErgoDelta);
                Logger.LogWarning("Ergo weight = " + ergonomicWeight);
                Logger.LogWarning("Pure Ergo = " + currentPureErgo);
                Logger.LogWarning("Pure Ergo D = " + totalPureErgoDelta);
                Logger.LogWarning("Torque = " + totalTorque);
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
            WeaponProperties.ErgonomicWeight = Mathf.Max(1, 80f - totalErgo);  //as an experiment, use total ergo as ergonomicWeight
            WeaponProperties.TotalRecoilDamping = totalRecoilDamping;
            WeaponProperties.TotalRecoilHandDamping = totalRecoilHandDamping;
            WeaponProperties.COIDelta = totalCOIDelta;
            WeaponProperties.PureErgoDelta = totalPureErgoDelta;

            return totalErgoDelta;
        }

        public void InitialStaCalc(ref Weapon __instance)
        {
            WeaponProperties._WeapClass = __instance.WeapClass;
            bool isManual = WeaponProperties.IsManuallyOperated(__instance);
            WeaponProperties._IsManuallyOperated = isManual;

            WeaponProperties.ShouldGetSemiIncrease = false;
            if (__instance.WeapClass != "pistol" || __instance.WeapClass != "shotgun" || __instance.WeapClass != "sniperRifle" || __instance.WeapClass != "smg")
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

            float baseConv = __instance.Template.Convergence;
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

            string weapOpType = WeaponProperties.OperationType(__instance);
            string weapType = WeaponProperties.WeaponType(__instance);

            string calibre = __instance.Template.ammoCaliber;
            float currentLoudness = 0;

            bool weaponAllowsFSADS = WeaponProperties.WeaponAllowsADS(__instance);
            bool stockAllowsFSADS = false;

            bool folded = __instance.Folded;
            WeaponProperties.Folded = folded;

            bool hasShoulderContact = false;

            bool canCycleSubs = false;

            float currentFixSpeedMod = 0f;


            if (WeaponProperties.WepHasShoulderContact(__instance) == true && !folded)
            {
                hasShoulderContact = true;
            }

            for (int i = 0; i < __instance.Mods.Length; i++)
            {
                Mod mod = __instance.Mods[i];
                if (!Utils.IsMagazine(mod))
                {
                    float modWeight = __instance.Mods[i].Weight;
                    float modWeightFactored = StatCalc.FactoredWeight(modWeight);
                    float modErgo = __instance.Mods[i].Ergonomics;
                    float modVRecoil = AttachmentProperties.VerticalRecoil(__instance.Mods[i]);
                    float modHRecoil = AttachmentProperties.HorizontalRecoil(__instance.Mods[i]);
                    float modAutoROF = AttachmentProperties.AutoROF(__instance.Mods[i]);
                    float modSemiROF = AttachmentProperties.SemiROF(__instance.Mods[i]);
                    float modCamRecoil = AttachmentProperties.CameraRecoil(__instance.Mods[i]);
                    float modConv = AttachmentProperties.ModConvergence(__instance.Mods[i]);
                    float modDispersion = AttachmentProperties.Dispersion(__instance.Mods[i]);
                    float modAngle = AttachmentProperties.RecoilAngle(__instance.Mods[i]);
                    float modAccuracy = __instance.Mods[i].Accuracy;
                    float modReload = AttachmentProperties.ReloadSpeed(__instance.Mods[i]);
                    float modChamber = AttachmentProperties.ChamberSpeed(__instance.Mods[i]);
                    float modAim = AttachmentProperties.AimSpeed(__instance.Mods[i]);
                    float modShotDisp = AttachmentProperties.ModShotDispersion(__instance.Mods[i]);
                    string modType = AttachmentProperties.ModType(__instance.Mods[i]);
                    string position = StatCalc.GetModPosition(__instance.Mods[i], weapType, weapOpType, modType);
                    float modLoudness = __instance.Mods[i].Loudness;
                    float modMalfChance = AttachmentProperties.ModMalfunctionChance(__instance.Mods[i]);
                    float modDuraBurn = __instance.Mods[i].DurabilityBurnModificator;
                    float modFix = AttachmentProperties.FixSpeed(__instance.Mods[i]);

                    StatCalc.ModConditionalStatCalc(__instance, mod, folded, weapType, weapOpType, ref hasShoulderContact, ref modAutoROF, ref modSemiROF, ref stockAllowsFSADS, ref modVRecoil, ref modHRecoil, ref modCamRecoil, ref modAngle, ref modDispersion, ref modErgo, ref modAccuracy, ref modType, ref position, ref modChamber, ref modLoudness, ref modMalfChance, ref modDuraBurn, ref modConv);
                    StatCalc.ModStatCalc(mod, modWeight, ref currentTorque, position, modWeightFactored, modAutoROF, ref currentAutoROF, modSemiROF, ref currentSemiROF, modCamRecoil, ref currentCamRecoil, modDispersion, ref currentDispersion, modAngle, ref currentRecoilAngle, modAccuracy, ref currentCOI, modAim, ref currentAimSpeedMod, modReload, ref currentReloadSpeedMod, modFix, ref currentFixSpeedMod, modErgo, ref currentErgo, modVRecoil, ref currentVRecoil, modHRecoil, ref currentHRecoil, ref currentChamberSpeedMod, modChamber, false, __instance.WeapClass, ref pureErgo, modShotDisp, ref currentShotDisp, modLoudness, ref currentLoudness, ref currentMalfChance, modMalfChance, ref pureRecoil, ref currentConv, modConv);
                    if (AttachmentProperties.CanCylceSubs(__instance.Mods[i]) == true)
                    {
                        canCycleSubs = true;
                    }
                    modBurnRatio *= modDuraBurn;
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

            float totalLoudness = ((currentLoudness / 100) + 1f) * StatCalc.CalibreLoudnessFactor(calibre);
            if (weapType == "bullpup")
            {
                totalLoudness *= 1.1f;
            }

            float pureRecoilDelta = ((baseVRecoil + baseHRecoil) - pureRecoil) / ((baseVRecoil + baseHRecoil) * -1f);
            WeaponProperties.TotalModDuraBurn = modBurnRatio;
            WeaponProperties.TotalMalfChance = currentMalfChance;
            Deafening.WeaponDeafFactor = totalLoudness;
            WeaponProperties.CanCycleSubs = canCycleSubs;
            WeaponProperties.HasShoulderContact = hasShoulderContact;
            WeaponProperties.InitTotalErgo = currentErgo;
            WeaponProperties.InitTotalVRecoil = currentVRecoil;
            WeaponProperties.InitTotalHRecoil = currentHRecoil;
            WeaponProperties.InitBalance = currentTorque;
            WeaponProperties.InitCamRecoil = currentCamRecoil;
            WeaponProperties.ModdedConv = currentConv;
            WeaponProperties.InitDispersion = currentDispersion;
            WeaponProperties.InitRecoilAngle = currentRecoilAngle;
            WeaponProperties.SDReloadSpeedModifier = currentReloadSpeedMod;
            WeaponProperties.SDChamberSpeedModifier = currentChamberSpeedMod;
            WeaponProperties.SDFixSpeedModifier = currentFixSpeedMod;
            WeaponProperties.ModAimSpeedModifier = currentAimSpeedMod / 100f;
            WeaponProperties.AutoFireRate = Mathf.Max(300, (int)currentAutoROF);
            WeaponProperties.SemiFireRate = Mathf.Max(200, (int)currentSemiROF);
            WeaponProperties.InitTotalCOI = currentCOI;
            WeaponProperties.InitPureErgo = pureErgo;
            WeaponProperties.PureRecoilDelta = pureRecoilDelta;
            WeaponProperties.ShotDispDelta = (baseShotDisp - currentShotDisp) / (baseShotDisp * -1f);
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

            if (__instance?.Owner?.ID != null && (__instance.Owner.ID.StartsWith("pmc") || __instance.Owner.ID.StartsWith("scav")))
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

    public class GetTotalCenterOfImpactPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("GetTotalCenterOfImpact", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref float __result, bool includeAmmo)
        {

            if (__instance?.Owner?.ID != null && (__instance.Owner.ID.StartsWith("pmc") || __instance.Owner.ID.StartsWith("scav")))
            {
                float mountFactor = 1f;
                if (Utils.IsReady)
                {
                    Player player = Utils.GetPlayer();
                    Mod currentAimingMod = (player.ProceduralWeaponAnimation.CurrentAimingMod != null) ? player.ProceduralWeaponAnimation.CurrentAimingMod.Item as Mod : null;
                    if (currentAimingMod != null)
                    {
                        if (AttachmentProperties.ModType(currentAimingMod) == "sight")
                        {
                            mountFactor += (currentAimingMod.Accuracy / 100f);
                        }
                        IEnumerable<Item> parents = currentAimingMod.GetAllParentItems();
                        foreach (Item item in parents)
                        {
                            if (item is Mod && AttachmentProperties.ModType(item) == "mount")
                            {
                                Mod mod = item as Mod;
                                mountFactor += (mod.Accuracy / 100f);
                            }
                        }
                    }
                }

                float totalCoi = 2 * (__instance.CenterOfImpactBase * (1f + __instance.CenterOfImpactDelta)) * mountFactor;
                if (!includeAmmo)
                {
                    __result = totalCoi;
                    return false;
                }
                AmmoTemplate currentAmmoTemplate = __instance.CurrentAmmoTemplate;
                __result = totalCoi * ((currentAmmoTemplate != null) ? currentAmmoTemplate.AmmoFactor : 1f);
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
            if (__instance?.Owner?.ID != null && (__instance.Owner.ID.StartsWith("pmc") || __instance.Owner.ID.StartsWith("scav")))
            {
                float shotDispLessAmmo = __instance.ShotgunDispersionBase * (1f + WeaponProperties.ShotDispDelta);
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

            if (__instance?.Owner?.ID != null && (__instance.Owner.ID.StartsWith("pmc") || __instance.Owner.ID.StartsWith("scav")))
            {
                modsBurnRatio = WeaponProperties.TotalModDuraBurn;
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

    public class SyncWithCharacterSkillsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EFT.Player.FirearmController).GetMethod("SyncWithCharacterSkills", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref EFT.Player.FirearmController __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                SkillsClass.GClass1681 skillsClass = (SkillsClass.GClass1681)AccessTools.Field(typeof(EFT.Player.FirearmController), "gclass1681_0").GetValue(__instance);
                PlayerProperties.StrengthSkillAimBuff = player.Skills.StrengthBuffAimFatigue.Value;
                PlayerProperties.ReloadSkillMulti = Mathf.Max(1, ((skillsClass.ReloadSpeed - 1f) * 0.5f) + 1f);
                PlayerProperties.FixSkillMulti = skillsClass.FixSpeed;
                PlayerProperties.WeaponSkillErgo = skillsClass.DeltaErgonomics;
                PlayerProperties.AimSkillADSBuff = skillsClass.AimSpeed;
            }
        }
    }

    public class ErgoWeightPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("get_ErgonomicWeight", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(ref Player.FirearmController __instance, ref float __result)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                __result = WeaponProperties.ErgonomicWeight * PlayerProperties.ErgoDeltaInjuryMulti * (1f - PlayerProperties.StrengthSkillAimBuff * 1.5f);

                if (!Utils.HasRunErgoWeightCalc) 
                {
                    __result = 0;
                    return false;
                }

                if (Plugin.EnableLogging.Value == true)
                {
                    Logger.LogWarning("===ErgonomicWeight===");
                    Logger.LogWarning("total ergo weight = " + __result);
                    Logger.LogWarning("base ergo weight = " + WeaponProperties.ErgonomicWeight);
                }

                return false;
            }
            else
            {
                return true;
            }
        }
    }


}
