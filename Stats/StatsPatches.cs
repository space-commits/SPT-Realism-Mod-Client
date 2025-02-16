using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using RealismMod.Weapons;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using WeaponSkills = EFT.SkillManager.GClass1981;
using EFT.WeaponMounting;

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
            if (!Utils.PlayerIsReady) return true;
            if (__instance?.Owner != null && __instance?.Owner?.ID != null && __instance.Owner.ID == Singleton<GameWorld>.Instance.MainPlayer.ProfileId)
            {
                AmmoTemplate currentAmmoTemplate = __instance.CurrentAmmoTemplate;
                float duraFactor =__instance.Repairable.Durability / __instance.Repairable.TemplateDurability;
                duraFactor = Mathf.Clamp(Mathf.Pow(duraFactor, 0.1f), 0.95f, 1f);
                __result = (currentAmmoTemplate != null) ? (int)(WeaponStats.SemiFireRate * currentAmmoTemplate.casingMass * duraFactor) : WeaponStats.SemiFireRate;
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
            if (!Utils.PlayerIsReady) return true;
            if (__instance?.Owner != null && __instance?.Owner?.ID != null && __instance.Owner.ID == Singleton<GameWorld>.Instance.MainPlayer.ProfileId)
            {
                AmmoTemplate currentAmmoTemplate = __instance.CurrentAmmoTemplate;
                float duraFactor = __instance.Repairable.Durability / __instance.Repairable.TemplateDurability;
                duraFactor = Mathf.Clamp(Mathf.Pow(duraFactor, 0.1f), 0.9f, 1f);
                __result = (currentAmmoTemplate != null) ? (int)(WeaponStats.AutoFireRate * currentAmmoTemplate.casingMass * duraFactor) : WeaponStats.AutoFireRate;
                return false;
            }
            return true;
        }
    }


    //this method sets player weapon ergo value. For some reason I've removed the injury penalty? Probably because I already apply injury multi myself 
    public class PlayerErgoPatch : ModulePatch
    {
        private static FieldInfo _playerField;
        private static FieldInfo _skillField;

        protected override MethodBase GetTargetMethod()
        {
            _playerField = AccessTools.Field(typeof(EFT.Player.FirearmController), "_player");
            _skillField = AccessTools.Field(typeof(EFT.Player.FirearmController), "gclass1981_0");

            return typeof(Player.FirearmController).GetMethod("method_12", BindingFlags.Instance | BindingFlags.Public);

        }
        [PatchPrefix]
        private static bool Prefix(Player.FirearmController __instance, ref float __result)
        {
            //to find this method again, look for this._player.MovementContext.PhysicalConditionContainsAny(EPhysicalCondition.LeftArmDamaged | EPhysicalCondition.RightArmDamaged)
            //return Mathf.Max(0f, this.Item.ErgonomicsTotal * (1f + this.gclass1560_0.DeltaErgonomics + this._player.ErgonomicsPenalty));

            Player player = (Player)_playerField.GetValue(__instance);
            if (player.IsYourPlayer == true)
            {
                WeaponSkills skillsClass = (WeaponSkills)_skillField.GetValue(__instance);
                float deltaErgo = skillsClass.DeltaErgonomics;
                if (!PluginConfig.OverrideMounting.Value && Plugin.ServerConfig.enable_stances && player.MovementContext.IsInMountedState)
                {
                    deltaErgo += ((player.MovementContext.PlayerMountingPointData.MountPointData.MountSideDirection != EMountSideDirection.Forward || !__instance.BipodState) ? skillsClass.MountingBonusErgo : skillsClass.BipodBonusErgo);
                }
                bool isBracingTop = StanceController.IsBracing && StanceController.BracingDirection == EBracingDirection.Top;
                if (Plugin.ServerConfig.enable_stances && PluginConfig.OverrideMounting.Value && WeaponStats.BipodIsDeployed && !StanceController.IsMounting && !isBracingTop) 
                {
                    deltaErgo -= 0.15f;
                }
                __result = Mathf.Max(0f, __instance.Item.ErgonomicsTotal * (1f + deltaErgo + player.ErgonomicsPenalty));
                return false;
            }
            return true;
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
            if (!Utils.PlayerIsReady) return true;
            if (__instance != null && __instance?.Owner != null && __instance?.Owner?.ID != null && __instance?.Owner?.ID == Singleton<GameWorld>.Instance?.MainPlayer?.ProfileId)
            {
                var weapStats = TemplateStats.GetDataObj<Gun>(TemplateStats.GunStats, __instance.TemplateId);
                if (PlayerValues.IsInReloadOpertation)
                {
                    __result = FinalStatCalc(__instance, weapStats);
                }
                else
                {
                    InitialStaCalc(__instance, weapStats);
                    __result = FinalStatCalc(__instance, weapStats);
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        public static float FinalStatCalc(Weapon __instance, Gun weapStats)
        {
            WeaponStats._WeapClass = __instance.WeapClass;
            bool isManual = weapStats.IsManuallyOperated;
            WeaponStats._IsManuallyOperated = isManual;

            float totalWeapWeight = __instance.TotalWeight;
            string weapType = weapStats.WeapType;
            string weapOpType = weapStats.OperationType;

            Mod magazine = __instance.GetCurrentMagazine();
            float magErgo = 0;
            float magWeight = 0;
            float currentTorque = 0;
            bool hasMag = magazine != null;
            WeaponStats.HasLongMag = false;
            if (hasMag == true)
            {
                var magStats = TemplateStats.GetDataObj<WeaponMod>(TemplateStats.WeaponModStats, magazine.TemplateId);
                float magWeightFactored = StatCalc.FactoredWeight(magWeight);
                string position = StatCalc.GetModPosition(magazine, weapType, weapOpType, "");
                magWeight = magazine.TotalWeight;
                magErgo = magazine.Ergonomics;
                currentTorque = StatCalc.GetTorque(position, magWeightFactored);
                WeaponStats.HasLongMag = magStats.ModType == "long_mag";
            }
            float weapWeightLessMag = totalWeapWeight - magWeight;

            float totalReloadSpeedMod = WeaponStats.SDReloadSpeedModifier;

            float totalChamberSpeedMod = WeaponStats.SDChamberSpeedModifier;

            float recoilDamping = weapStats.RecoilDamping;
            float recoilHandDamping = weapStats.RecoilHandDamping;

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
            float totalErgoLessMag = 0F;
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

            StatCalc.WeaponStatCalc(
                __instance, weapStats, currentTorque, ref totalTorque, currentErgo, currentVRecoil, currentHRecoil, currentDispersion, currentCamRecoil, currentRecoilAngle, 
                baseErgo, baseVRecoil, baseHRecoil, ref totalErgo, ref totalVRecoil, ref totalHRecoil, ref totalDispersion, ref totalCamRecoil, ref totalRecoilAngle, 
                ref totalRecoilDamping, ref totalRecoilHandDamping, ref totalErgoDelta, ref totalVRecoilDelta, ref totalHRecoilDelta, ref recoilDamping, 
                ref recoilHandDamping, WeaponStats.InitTotalCOI, WeaponStats.HasShoulderContact, ref totalCOI, ref totalCOIDelta, __instance.CenterOfImpactBase, 
                currentPureErgo, ref totalPureErgoDelta, ref totalErgoLessMag, WeaponStats.InitTotalErgo, false);

            float ergonomicWeight = StatCalc.ErgoWeightCalc(totalWeapWeight, totalPureErgoDelta, totalTorque, __instance.WeapClass);
            float ergonomicWeightLessMag = StatCalc.ErgoWeightCalc(weapWeightLessMag, totalPureErgoDelta, totalTorque, __instance.WeapClass);

            float ergoFactor = Mathf.Max(1, 80f - totalErgo); //as an experiment, use total ergo as ergonomicWeight
            float ergoFactorLessMag = Mathf.Max(1, 80f - WeaponStats.InitTotalErgo);  //should be totalErgoLessMag but that would require rebalancing everything :'(

            float totalAimMoveSpeedFactor = 0;
            float totalReloadSpeedLessMag = 0;
            float totalChamberSpeed = 0;
            float totalFiringChamberSpeed = 0;
            float totalChamberCheckSpeed = 0;
            float totalFixSpeed = 0;

            StatCalc.SpeedStatCalc(
                __instance, weapStats, ergoFactor, ergoFactorLessMag, totalChamberSpeedMod, 
                totalReloadSpeedMod, ref totalReloadSpeedLessMag, ref totalChamberSpeed, 
                ref totalAimMoveSpeedFactor, ref totalFiringChamberSpeed, ref totalChamberCheckSpeed,
                ref totalFixSpeed);
          
            WeaponStats.TotalFixSpeed = totalFixSpeed;
            WeaponStats.TotalChamberCheckSpeed = totalChamberCheckSpeed;
            WeaponStats.TotalReloadSpeedLessMag = totalReloadSpeedLessMag;
            WeaponStats.TotalChamberSpeed = totalChamberSpeed;
            WeaponStats.TotalFiringChamberSpeed = totalFiringChamberSpeed;
            WeaponStats.AimMoveSpeedWeapModifier = totalAimMoveSpeedFactor;

            if (hasMag == true)
            {
                ReloadController.MagReloadSpeedModifier(__instance, (MagazineItemClass)magazine, false, false);
            }

            if (PluginConfig.EnableGeneralLogging.Value == true)
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
            WeaponStats.TotalRecoilAngle = PluginConfig.EnableAngle.Value ? Mathf.Max(totalRecoilAngle, 65f) : 90f;
            WeaponStats.TotalVRecoil = totalVRecoil;
            WeaponStats.TotalHRecoil = totalHRecoil;
            WeaponStats.Balance = totalTorque;
            WeaponStats.TotalErgo = Mathf.Clamp(totalErgo, 1f, 80f);
            WeaponStats.ErgoDelta = Mathf.Clamp(totalErgoDelta, -0.99f, 2);
            WeaponStats.VRecoilDelta = totalVRecoilDelta;
            WeaponStats.HRecoilDelta = totalHRecoilDelta;
            WeaponStats.ErgoFactor = Mathf.Clamp(80f - totalErgo, 1f, 80f);  //as an experiment, use total ergo as ergonomicWeight
            WeaponStats.ErgonomicWeight = ergonomicWeight;
            WeaponStats.TotalRecoilDamping = totalRecoilDamping;
            WeaponStats.TotalRecoilHandDamping = totalRecoilHandDamping;
            WeaponStats.COIDelta = totalCOIDelta;
            WeaponStats.PureErgoDelta = totalPureErgoDelta;
            WeaponStats.CurrentVisualRecoilMulti = weapStats.VisualMulti;
            return totalErgoDelta;
        }

        public static void InitialStaCalc(Weapon __instance, Gun weapStats)
        {
            WeaponStats._WeapClass = __instance.WeapClass;
            bool isManual = weapStats.IsManuallyOperated;
            WeaponStats._IsManuallyOperated = isManual;
            bool isChonker = __instance.IsBeltMachineGun || __instance.TotalWeight >= 10f;

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

            float baseCamReturnSpeed = weapStats.VisualMulti;
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

            float currentAimStability = 0f;
            float currentHandling = 0f;

            float modBurnRatio = 1;

            float baseMalfChance = __instance.BaseMalfunctionChance;
            float currentMalfChance = baseMalfChance;

            string weapOpType = weapStats.OperationType;
            string weapType = weapStats.WeapType;

            string caliber = __instance.AmmoCaliber;
            float currentLoudness = 0;

            bool weaponAllowsFSADS = weapStats.WeaponAllowADS;
            bool stockAllowsFSADS = false;

            bool canCycleSubs = false;

            float currentFixSpeedMod = 0f;

            float currentFlashSuppression = 0f;
            float currentGas = 0f;

            bool folded = __instance.Folded;
            bool hasShoulderContact = false;
            if (weapStats.HasShoulderContact && !folded)
            {
                hasShoulderContact = true;
            }
            WeaponStats.BaseMeleeDamage = 0f; //reset the melee dmg
            WeaponStats.BaseMeleePen = 0f;

            WeaponStats.HasBayonet = false;
            WeaponStats.HasBooster = false;
            WeaponStats.HasMuzzleDevice = false;
            WeaponStats.HasSuppressor = false;
       
            foreach (Mod mod in __instance.Mods)
            {
                if (!Utils.IsMagazine(mod))
                {
                    var weaponModStats = TemplateStats.GetDataObj<WeaponMod>(TemplateStats.WeaponModStats, mod.TemplateId);
                    string modType = weaponModStats.ModType;
                    float modWeight = mod.Weight;
                    float modWeightFactored = StatCalc.FactoredWeight(modWeight);
                    float modErgo = mod.Ergonomics;
                    float modVRecoil = weaponModStats.VerticalRecoil;
                    float modConv = weaponModStats.Convergence;
                    modVRecoil += modConv > 0f ? modConv * StatCalc.ConvVRecoilConversion : 0f;
                    float modHRecoil = weaponModStats.HorizontalRecoil;
                    float modAutoROF = weaponModStats.AutoROF;
                    float modSemiROF = weaponModStats.SemiROF;
                    float modCamRecoil = weaponModStats.CameraRecoil;
                    float modDispersion = weaponModStats.Dispersion;
                    float modAngle = weaponModStats.RecoilAngle;
                    float modAccuracy = mod.Accuracy;
                    float modReload = weaponModStats.ReloadSpeed;
                    float modChamber = weaponModStats.ChamberSpeed;
                    float modAim = weaponModStats.AimSpeed;
                    float modShotDisp = weaponModStats.ModShotDispersion;
                    string position = StatCalc.GetModPosition(mod, weapType, weapOpType, modType);
                    float modLoudness = mod.Loudness;
                    float modMalfChance = weaponModStats.ModMalfunctionChance;
                    float modDuraBurn = mod.DurabilityBurnModificator;
                    float modFix = weaponModStats.FixSpeed;
                    float modFlashSuppression = weaponModStats.Flash;
                    float modHandling = weaponModStats.Handling;
                    float modStability = weaponModStats.AimStability;

                    if (Utils.IsMuzzleDevice(mod))
                    {
                        if (modType == "bayonet") WeaponStats.HasBayonet = true;
                        if (modType == "booster") WeaponStats.HasBooster = true;
                        if (Utils.IsSilencer(mod)) WeaponStats.HasSuppressor = true;

                        WeaponStats.BaseMeleeDamage = weaponModStats.MeleeDamage;
                        WeaponStats.BaseMeleePen = weaponModStats.MeleePen;
                        WeaponStats.HasMuzzleDevice = true;
                    }

                    if (weaponModStats.CanCycleSubs) canCycleSubs = true;

                    StatCalc.ModConditionalStatCalc(
                        __instance, weapStats, mod, weaponModStats, folded, weapType, weapOpType, ref hasShoulderContact, ref modAutoROF, 
                        ref modSemiROF, ref stockAllowsFSADS, ref modVRecoil, ref modHRecoil, 
                        ref modCamRecoil, ref modAngle, ref modDispersion, ref modErgo, 
                        ref modAccuracy, ref modType, ref position, ref modChamber, ref modLoudness, 
                        ref modMalfChance, ref modDuraBurn, ref modConv, ref modFlashSuppression, ref modStability, 
                        ref modHandling, ref modAim);

                    StatCalc.ModStatCalc(
                        mod, false, isChonker, modWeight, ref currentTorque, position, modWeightFactored, modAutoROF, 
                        ref currentAutoROF, modSemiROF, ref currentSemiROF, modCamRecoil, ref currentCamRecoil, 
                        modDispersion, ref currentDispersion, modAngle, ref currentRecoilAngle, modAccuracy, 
                        ref currentCOI, modErgo, ref currentErgo, modVRecoil, ref currentVRecoil,  modHRecoil, 
                        ref currentHRecoil, ref pureErgo, modShotDisp, ref currentShotDisp, ref currentMalfChance, 
                        modMalfChance, ref pureRecoil, ref currentConv, modConv, ref currentCamReturnSpeed);

       
                    if (!Utils.IsMuzzleCombo(mod) && !Utils.IsFlashHider(mod) && !Utils.IsBarrel(mod)) currentGas += modFlashSuppression;
                    else currentFlashSuppression += modFlashSuppression;
                    if (!Utils.IsSight(mod)) currentAimSpeedMod += modAim;
                    currentChamberSpeedMod += modChamber;
                    currentAimStability += modStability;
                    currentReloadSpeedMod += modReload;
                    currentLoudness += modLoudness;
                    currentHandling += modHandling;
                    currentFixSpeedMod += modFix;
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

            WeaponStats.IsPistol = __instance.WeapClass == "pistol";
            WeaponStats.IsMachinePistol = weapType == "smg_pistol" && !hasShoulderContact;
            WeaponStats.IsStocklessPistol = !hasShoulderContact && WeaponStats.IsPistol ? true : false;
            WeaponStats.IsStockedPistol = hasShoulderContact && WeaponStats.IsPistol ? true : false;

            float totalLoudness = ((currentLoudness / 80) + 1f) * StatCalc.CaliberLoudnessFactor(caliber);

            if (weapType == "bullpup" || weapOpType == "p90")
            {
                totalLoudness *= 1.18f;
                WeaponStats.IsBullpup = true;
            }
            else WeaponStats.IsBullpup = false;
  
            float pureRecoilDelta = ((baseVRecoil + baseHRecoil) - pureRecoil) / ((baseVRecoil + baseHRecoil) * -1f);
            WeaponStats.TotalModDuraBurn = modBurnRatio;
            WeaponStats.TotalMalfChance = Mathf.Max(currentMalfChance, baseMalfChance * 0.35f);
            WeaponStats.MalfChanceDelta = (baseMalfChance - WeaponStats.TotalMalfChance) / baseMalfChance;
            DeafenController.GunDeafFactor = totalLoudness;
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
            WeaponStats.SDFixSpeedModifier = currentFixSpeedMod; //unused, replcaed by chamber speed
            WeaponStats.ModAimSpeedModifier = currentAimSpeedMod / 100f;
            WeaponStats.AutoFireRate = Mathf.Max(400, (int)currentAutoROF);
            WeaponStats.SemiFireRate = Mathf.Max(300, (int)currentSemiROF);
            WeaponStats.FireRateDelta = ((float)WeaponStats.AutoFireRate / (float)__instance.Template.bFirerate) * ((float)WeaponStats.SemiFireRate / (float)__instance.Template.SingleFireRate);
            WeaponStats.AutoFireRateDelta = (float)WeaponStats.AutoFireRate / (float)__instance.Template.bFirerate;
            WeaponStats.SemiFireRateDelta = (float)WeaponStats.SemiFireRate / (float)__instance.Template.SingleFireRate;
            WeaponStats.InitTotalCOI = currentCOI;
            WeaponStats.InitPureErgo = pureErgo;
            WeaponStats.PureRecoilDelta = pureRecoilDelta;
            WeaponStats.ShotDispDelta = (baseShotDisp - currentShotDisp) / (baseShotDisp * -1f);
            WeaponStats.TotalCameraReturnSpeed = currentCamReturnSpeed;
            WeaponStats.TotalModdedConv = currentConv * (!hasShoulderContact ? WeaponStats.FoldedConvergenceFactor : 1f);
            WeaponStats.ConvergenceDelta = currentConv / __instance.Template.RecoilReturnSpeedHandRotation;
            WeaponStats.VelocityDelta = __instance.VelocityDelta;
            WeaponStats.MuzzleLoudness = currentLoudness;
            WeaponStats.Caliber = caliber;
            WeaponStats.TotalMuzzleFlash = currentFlashSuppression;
            WeaponStats.TotalGas = currentGas;
            WeaponStats.IsDirectImpingement = weapType == "DI" ? true : false;
            WeaponStats.TotalAimStabilityModi = Mathf.Clamp(1f - (currentAimStability / 100f), 0.25f, 2f);
            WeaponStats.TotalWeaponHandlingModi = Mathf.Clamp(1f - (currentHandling / 100f), 0.25f, 2f);
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
            if (!Utils.PlayerIsReady) return true;
            if (__instance?.Owner != null && __instance?.Owner?.ID != null && __instance.Owner.ID == Singleton<GameWorld>.Instance.MainPlayer.ProfileId)
            {
                __result = WeaponStats.COIDelta;
                return false;
            }
            else return true;
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

            if (Utils.PlayerIsReady && __instance?.Owner != null && __instance?.Owner?.ID != null && __instance.Owner.ID == Singleton<GameWorld>.Instance.MainPlayer.ProfileId)
            {
                bool isBracingTop = StanceController.BracingDirection == EBracingDirection.Top;
                bool isMountedTop = StanceController.IsMounting && isBracingTop;
                float mountingFactor = isMountedTop && WeaponStats.BipodIsDeployed ? 0.75f : isMountedTop ? 0.85f : StanceController.IsMounting && !isBracingTop ? 0.9f : StanceController.IsBracing && isBracingTop ? 0.95f : StanceController.IsBracing && !isBracingTop ? 0.975f : 1f;
                float stockFactor = !WeaponStats.HasShoulderContact ? 2f : 1f;
                float baseCOI = __instance.CenterOfImpactBase * (1f + __instance.CenterOfImpactDelta);
                float totalCOI = baseCOI * (1f - WeaponStats.ScopeAccuracyFactor) * mountingFactor * stockFactor * (PluginConfig.IncreaseCOI.Value ? 1.5f : 1f);

                if (!includeAmmo)
                {
                    __result = totalCOI;
                    return false;
                }

                AmmoTemplate currentAmmoTemplate = __instance.CurrentAmmoTemplate;
                __result = totalCOI * ((currentAmmoTemplate != null) ? currentAmmoTemplate.AmmoFactor : 1f);

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
            if (!Utils.PlayerIsReady) return true;
            if (__instance?.Owner != null && __instance?.Owner?.ID != null && __instance.Owner.ID == Singleton<GameWorld>.Instance.MainPlayer.ProfileId)
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
            if (player.IsYourPlayer)
            {
                __result = WeaponStats.ErgoFactor * (1f - (PlayerValues.StrengthSkillAimBuff * 1.5f)) * (1f + (1f - PlayerValues.GearErgoPenalty));

                if (PluginConfig.EnablePWALogging.Value == true)
                {
                    Logger.LogWarning("===ErgonomicWeight===");
                    Logger.LogWarning("total ergo weight = " + __result);
                    Logger.LogWarning("base ergo weight = " + WeaponStats.ErgoFactor);
                    Logger.LogWarning("ergoweight ergo weight = " + WeaponStats.ErgonomicWeight);
                }
                return false;
            }
            return true;
        }
    }    
}
