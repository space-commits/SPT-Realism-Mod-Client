using EFT;
using EFT.Animations;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using static EFT.Player;
using WeaponSkillsClass = EFT.SkillManager.GClass1783;

namespace RealismMod
{
    public static class StatCalc
    {
        public const float convVRecoilConversion = -0.82f;

        public const float ErgoWeightMult = 13.5f;
        public const float ErgoTorqueMult = 0.8f;
        public const float PistolErgoWeightMult = 12.6f;
        public const float PistolErgoTorqueMult = 1.0f;

        public const float VRecoilWeightMult = 2f;
        public const float VRecoilTorqueMult = 0.87f;
        public const float PistolVRecoilWeightMult = 1.5f;
        public const float PistolVRecoilTorqueMult = 1.5f;

        public const float HRecoilWeightMult = 3.55f;
        public const float HRecoilTorqueMult = 0.4f;
        public const float PistolHRecoilWeightMult = 3.5f;
        public const float PistolHRecoilTorqueMult = 0.8f;

        public const float DispersionWeightMult = 1.5f;
        public const float DispersionTorqueMult = 1.2f;
        public const float PistolDispersionWeightMult = 0.8f;
        public const float PistolDispersionTorqueMult = 1.5f;

        public const float CamWeightMult = 5f;
        public const float CamTorqueMult = 0.2f;

        public const float AngleTorqueMult = 0.39f;
        public const float PistolAngleTorqueMult = 0.3f;

        public const float DampingWeightMult = 0.04f;
        public const float DampingTorqueMult = 0.055f;
        public const float DampingMin = 0.2f;
        public const float DampingMax = 0.9f;
        public const float DampingPistolMin = 0.2f;
        public const float DampingPistolMax = 0.9f;

        public const float HandDampingWeightMult = 0.07f;
        public const float HandDampingTorqueMult = 0.04f;
        public const float HandDampingMin = 0.2f;
        public const float HandDampingMax = 0.9f;
        public const float HandDampingPistolMin = 0.2f;
        public const float HandDampingPistolMax = 0.9f;

        public const float MagWeightMult = 16.5f;

        public static void CalcPlayerWeightStats(Player player)
        {
            player.Inventory.UpdateTotalWeight();
            float playerWeight = player.Inventory.TotalWeight;
            float weaponWeight = player?.HandsController != null && player?.HandsController?.Item != null ? player.HandsController.Item.GetSingleItemTotalWeight() : 1f;
            PlayerState.TotalModifiedWeightMinusWeapon = playerWeight - weaponWeight;
            PlayerState.TotalMousePenalty = (-playerWeight / 10f);
            PlayerState.TotalModifiedWeight = playerWeight;
            if (Plugin.EnableMouseSensPenalty.Value)
            {
                player.RemoveMouseSensitivityModifier(Player.EMouseSensitivityModifier.Armor);
                if (PlayerState.TotalMousePenalty < 0f)
                {
                    player.AddMouseSensitivityModifier(Player.EMouseSensitivityModifier.Armor, PlayerState.TotalMousePenalty / 100f);
                }
            }
        }

        public static void CalcSightAccuracy(Mod currentAimingMod)
        {
            float currentSightFactor = 0f;
            int iterations = 0;
  
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

            WeaponStats.ScopeAccuracyFactor = currentSightFactor;
        }

        public static void UpdateAimParameters(FirearmController firearmController, ProceduralWeaponAnimation pwa) 
        {
            Weapon weapon = firearmController.Weapon;
            WeaponSkillsClass skillsClass = (WeaponSkillsClass)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_buffInfo").GetValue(pwa);
            Player.ValueBlender valueBlender = (Player.ValueBlender)AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimSwayBlender").GetValue(pwa);

       
            float swayStrength = 0f;
            if (weapon.WeapClass != "pistol")
            {
                float headGearFactor = GearController.FSIsActive || GearController.NVGIsActive || GearController.HasGasMask ? 1.45f : 1f;
                float gunWeightFactor = ProceduralIntensityFactorCalc(weapon.GetSingleItemTotalWeight(), 4f);
                float ergoWeightFactor = WeaponStats.ErgoFactor * gunWeightFactor * (1f + (-WeaponStats.Balance / 100f)) * (1f - WeaponStats.PureErgoDelta) * headGearFactor * (1f - (PlayerState.StrengthSkillAimBuff * 1.5f)) * (1f + ((1f - PlayerState.GearErgoPenalty) * 1.5f)); //
                swayStrength = Mathf.InverseLerp(1f, 180f, ergoWeightFactor);
            }
            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimSwayStrength").SetValue(pwa, swayStrength);

            float baseAimspeed = Mathf.InverseLerp(1f, 80f, WeaponStats.TotalErgo * PlayerState.GearErgoPenalty) * 1.15f;
            float aimSpeed = Mathf.Clamp(baseAimspeed * (1f + (skillsClass.AimSpeed * 0.5f)) * (1f + WeaponStats.ModAimSpeedModifier), 0.5f, 1.45f);
            valueBlender.Speed = pwa.SwayFalloff * aimSpeed * 4.35f;

            pwa.UpdateSwayFactors();

            aimSpeed = weapon.WeapClass == "pistol" ? aimSpeed * 1.35f : aimSpeed;
            WeaponStats.SightlessAimSpeed = aimSpeed;
            WeaponStats.ErgoStanceSpeed = baseAimspeed * (1f + (skillsClass.AimSpeed * 0.5f)) * (weapon.WeapClass == "pistol" ? 1.5f : 1f);

            AccessTools.Field(typeof(EFT.Animations.ProceduralWeaponAnimation), "_aimingSpeed").SetValue(pwa, aimSpeed);

            if (Plugin.EnableLogging.Value == true)
            {
                Utils.Logger.LogWarning("========UpdateWeaponVariables=======");
                Utils.Logger.LogWarning("total ergo = " + WeaponStats.TotalErgo);
                Utils.Logger.LogWarning("aimSpeed = " + aimSpeed);
                Utils.Logger.LogWarning("base aimSpeed = " + baseAimspeed);
                Utils.Logger.LogWarning("total ergofactor = " + WeaponStats.ErgoFactor * (1f - (PlayerState.StrengthSkillAimBuff * 1.5f)));
                Utils.Logger.LogWarning("gear ergo factor = " + PlayerState.GearErgoPenalty);

            }
        }

        public static float ErgoWeightCalc(float totalWeight, float ergoDelta, float totalTorque, string weapClass)
        {
            if (weapClass == "pistol")
            {
                totalTorque = totalTorque < 0 ? totalTorque * 2f : totalTorque;
                float totalTorqueFactorInverse = 1f + (totalTorque > 14 ? totalTorque / 100f : totalTorque / -100f);
                float ergoFactoredWeight = Math.Max(1f, totalWeight * (1f - ergoDelta));
                float balancedErgoFactoredWeight = Math.Max(1f, ergoFactoredWeight * totalTorqueFactorInverse);
                return Mathf.Clamp((float)(Math.Pow(balancedErgoFactoredWeight * 3.2f, 3.2f) + 1f) / 10f, 2.5f, 80f);
            }
            else
            {
                totalTorque = totalTorque < 0f ? totalTorque * 3f : totalTorque > 10f ? totalTorque / 2f : totalTorque <= 10f && totalTorque >= 0f ? totalTorque * 1.5f : totalTorque;
                float totalTorqueFactorInverse = 1f + (totalTorque / -100f); ;
                float ergoFactoredWeight = Math.Max(1f, totalWeight * (1f - ergoDelta));
                float balancedErgoFactoredWeight = Math.Max(1f, ergoFactoredWeight * totalTorqueFactorInverse);
                return Mathf.Clamp((float)(Math.Pow(balancedErgoFactoredWeight * 2.1f, 3.7f) + 1f) / 750f, 2.5f, 80f);
            }
        }

        public static float ProceduralIntensityFactorCalc(float weapWeight, float idealWeapWeight)
        {
            //get percentage differenecne between weapon weight and a chosen minimum/threshold weight. Apply that % difference as a multiplier 
            return ((weapWeight - idealWeapWeight) / idealWeapWeight) + 1f; 
        }

        public static void SpeedStatCalc(Weapon weap, float ergoWeight, float ergonomicWeightLessMag, float chamberSpeedMod, float reloadSpeedMod, ref float totalReloadSpeedLessMag, ref float totalChamberSpeed, ref float totalAimMoveSpeedFactor, ref float totalFiringChamberSpeed, ref float totalChamberCheckSpeed, ref float totalFixSpeed)
        {
            chamberSpeedMod = 1f + (chamberSpeedMod / 100f);
            reloadSpeedMod = 1f + (reloadSpeedMod / 100f);
            float baseFixSpeed = WeaponStats.BaseFixSpeed(weap);
            float baseChamberCheckSpeed = WeaponStats.BaseChamberCheckSpeed(weap);
            float baseChamberSpeed = WeaponStats.BaseChamberSpeed(weap);
            float baseReloadSpeed = WeaponStats.BaseReloadSpeed(weap);
            float recoilMulti = 1f + (-1f * WeaponStats.PureRecoilDelta);
            float ergoWeightMulti = 1f - (ergoWeight / 100f);
            float ergoWeightLessMagMulti = 1f - (ergonomicWeightLessMag / 100f);

            totalFixSpeed = Mathf.Clamp(baseFixSpeed * ergoWeightMulti * chamberSpeedMod, WeaponStats.MinChamberSpeed(weap), WeaponStats.MaxChamberSpeed(weap));
            totalFiringChamberSpeed = Mathf.Clamp(baseChamberSpeed * ergoWeightMulti * chamberSpeedMod * recoilMulti, WeaponStats.MinChamberSpeed(weap), WeaponStats.MaxChamberSpeed(weap));
            totalChamberSpeed = Mathf.Clamp(baseChamberSpeed * ergoWeightMulti * chamberSpeedMod, WeaponStats.MinChamberSpeed(weap), WeaponStats.MaxChamberSpeed(weap));
            totalChamberCheckSpeed = Mathf.Clamp(baseChamberCheckSpeed * ergoWeightMulti * chamberSpeedMod, WeaponStats.MinChamberSpeed(weap), WeaponStats.MaxChamberSpeed(weap));
            totalReloadSpeedLessMag = Mathf.Clamp(baseReloadSpeed * ergoWeightLessMagMulti * reloadSpeedMod, WeaponStats.MinReloadSpeed(weap), WeaponStats.MaxReloadSpeed(weap));
            totalAimMoveSpeedFactor = Mathf.Max(1f - (ergoWeight / 150f), 0.5f);
        }

        public static void WeaponStatCalc(Weapon weap, float currentTorque, ref float totalTorque, float currentErgo, float currentVRecoil, float currentHRecoil, float currentDispersion, float currentCamRecoil, float currentRecoilAngle, float baseErgo, float baseVRecoil, float baseHRecoil, ref float totalErgo, ref float totalVRecoil, ref float totalHRecoil, ref float totalDispersion, ref float totalCamRecoil, ref float totalRecoilAngle, ref float totalRecoilDamping, ref float totalRecoilHandDamping, ref float totalErgoDelta, ref float totalVRecoilDelta, ref float totalHRecoilDelta, ref float recoilDamping, ref float recoilHandDamping, float currentCOI, bool hasShoulderContact, ref float totalCOI, ref float totalCOIDelta, float baseCOI, float totalPureErgo, ref float totalPureErgoDelta, ref float totalErgoLessMag, float currentErgoLessMag, bool isDisplayDelta)
        {
            float angleTorqueMulti;

            float ergoTorqueMult;
            float ergoWeightMult;

            float vRecoilTorqueMult;
            float vRecoilWeightMult;

            float hRecoilTorqueMult;
            float hRecoilWeightMult;

            float dispersionWeightMult;
            float dispersionTorqueMult;

            float currentPistolErgoTorque = currentTorque > 3 ? currentTorque * -1f : currentTorque;

            if (weap.WeapClass == "pistol")
            {
                angleTorqueMulti = StatCalc.PistolAngleTorqueMult;
                ergoTorqueMult = StatCalc.PistolErgoTorqueMult;
                ergoWeightMult = StatCalc.PistolErgoWeightMult;
                vRecoilTorqueMult = StatCalc.PistolVRecoilTorqueMult;
                vRecoilWeightMult = StatCalc.PistolVRecoilWeightMult;
                hRecoilTorqueMult = StatCalc.PistolHRecoilTorqueMult;
                hRecoilWeightMult = StatCalc.PistolHRecoilWeightMult;
                dispersionTorqueMult = StatCalc.PistolDispersionTorqueMult;
                dispersionWeightMult = StatCalc.PistolDispersionWeightMult;
            }
            else
            {
                angleTorqueMulti = StatCalc.AngleTorqueMult;
                ergoTorqueMult = StatCalc.ErgoTorqueMult;
                ergoWeightMult = StatCalc.ErgoWeightMult;
                vRecoilTorqueMult = StatCalc.VRecoilTorqueMult;
                vRecoilWeightMult = StatCalc.VRecoilWeightMult;
                hRecoilTorqueMult = StatCalc.HRecoilTorqueMult;
                hRecoilWeightMult = StatCalc.HRecoilWeightMult;
                dispersionTorqueMult = StatCalc.DispersionTorqueMult;
                dispersionWeightMult = StatCalc.DispersionWeightMult;
            }

            float weaponBaseWeight = weap.Weight;
            float weaponBaseWeightFactored = FactoredWeight(weaponBaseWeight);
            float weaponBaseTorque = TorqueCalc(WeaponStats.BaseTorqueDistance(weap), weaponBaseWeightFactored, weap.WeapClass);

            float ergoWeapBaseWeightFactor = WeightStatCalc(ergoWeightMult, weap.IsBeltMachineGun || weaponBaseWeight >= 10f ? weaponBaseWeight * 0.5f : weaponBaseWeight) / 100f;
            float vRecoilWeapBaseWeightFactor = WeightStatCalc(vRecoilWeightMult, weaponBaseWeight) / 100f;
            float hRecoilWeapBaseWeightFactor = WeightStatCalc(hRecoilWeightMult, weaponBaseWeight) / 100f;
            float dispersionWeapBaseWeightFactor = WeightStatCalc(dispersionWeightMult, weaponBaseWeight) / 100f;
            float camRecoilWeapBaseWeightFactor = WeightStatCalc(StatCalc.CamWeightMult, weaponBaseWeight) / 100f;

            float totalWeapWeight = weap.GetSingleItemTotalWeight();
            float dampingTotalWeightFactor = WeightStatCalc(StatCalc.DampingWeightMult, totalWeapWeight) / 100f;
            float handDampingTotalWeightFactor = WeightStatCalc(StatCalc.HandDampingWeightMult, totalWeapWeight) / 100f;

            totalTorque = weaponBaseTorque + currentTorque;
            float totalPistolErgoTorque = weaponBaseTorque + currentPistolErgoTorque;

            float totalTorqueFactorErgo = weap.WeapClass == "pistol" ? totalPistolErgoTorque / 100f : totalTorque / 100f;
            float totalTorqueFactor = totalTorque / 100f;
            float totalTorqueFactorInverse = totalTorque / -100f;

            totalErgoLessMag = currentErgoLessMag + (currentErgoLessMag * (ergoWeapBaseWeightFactor + (totalTorqueFactorErgo * ergoTorqueMult)));
            totalErgo = currentErgo + (currentErgo * (ergoWeapBaseWeightFactor + (totalTorqueFactorErgo * ergoTorqueMult)));
            totalVRecoil = currentVRecoil + (currentVRecoil * (vRecoilWeapBaseWeightFactor + (totalTorqueFactor * vRecoilTorqueMult)));
            totalHRecoil = currentHRecoil + (currentHRecoil * (hRecoilWeapBaseWeightFactor + (totalTorqueFactorInverse * hRecoilTorqueMult)));
            totalCamRecoil = currentCamRecoil + (currentCamRecoil * (camRecoilWeapBaseWeightFactor + (totalTorqueFactorInverse * StatCalc.CamTorqueMult)));
            totalDispersion = currentDispersion + (currentDispersion * (dispersionWeapBaseWeightFactor + (totalTorqueFactor * dispersionTorqueMult)));

            totalRecoilAngle = currentRecoilAngle + (currentRecoilAngle * (totalTorqueFactor * angleTorqueMulti));
            totalCOI = currentCOI + (currentCOI * (-WeaponStats.WeaponAccuracy(weap) / 100));

            if (!hasShoulderContact && weap.WeapClass != "pistol")
            {
                totalPureErgo *= WeaponStats.FoldedErgoFactor;
                totalErgo *= WeaponStats.FoldedErgoFactor;
                totalErgoLessMag *= WeaponStats.FoldedErgoFactor;
                totalVRecoil *= WeaponStats.FoldedVRecoilFactor;
                totalHRecoil *= WeaponStats.FoldedHRecoilFactor;
                totalCamRecoil *= WeaponStats.FoldedCamRecoilFactor;
                totalDispersion *= WeaponStats.FoldedDispersionFactor;
                totalRecoilAngle *= WeaponStats.FoldedRecoilAngleFactor;
            }

            totalCOIDelta = (totalCOI - baseCOI) / baseCOI;
            totalErgoDelta = (totalErgo - 80f) / 80f; //arbitrary base value to differentiate weapons better
            totalPureErgoDelta = (totalPureErgo - 80f) / 80f; //arbitrary base value to differentiate weapons better
            totalVRecoilDelta = (totalVRecoil - baseVRecoil) / baseVRecoil;
            totalHRecoilDelta = (totalHRecoil - baseHRecoil) / baseHRecoil;

            if (isDisplayDelta == true)
            {
                return;
            }

            if (weap.WeapClass == "pistol")
            {
                totalRecoilDamping = Mathf.Clamp(recoilDamping + (recoilDamping * (dampingTotalWeightFactor + (totalTorqueFactor * StatCalc.DampingTorqueMult))), StatCalc.DampingPistolMin, StatCalc.DampingPistolMax);
                totalRecoilHandDamping = Mathf.Clamp(recoilHandDamping + (recoilHandDamping * (handDampingTotalWeightFactor + (totalTorqueFactorInverse * StatCalc.HandDampingTorqueMult))), StatCalc.HandDampingPistolMin, StatCalc.HandDampingPistolMax);
            }
            else
            {
                totalRecoilDamping = Mathf.Clamp(recoilDamping + (recoilDamping * (dampingTotalWeightFactor + (totalTorqueFactor * StatCalc.DampingTorqueMult))), StatCalc.DampingMin, StatCalc.DampingMax);
                totalRecoilHandDamping = Mathf.Clamp(recoilHandDamping + (recoilHandDamping * (handDampingTotalWeightFactor + (totalTorqueFactorInverse * StatCalc.HandDampingTorqueMult))), StatCalc.HandDampingMin, StatCalc.HandDampingMax);
            }
        }


        public static void ModStatCalc(Mod mod, float modWeight, ref float currentTorque, string position, float modWeightFactored, float modAutoROF, ref float currentAutoROF, 
            float modSemiROF, ref float currentSemiROF, float modCamRecoil, ref float currentCamRecoil, float modDispersion, ref float currentDispersion, float modAngle, 
            ref float currentRecoilAngle, float modAccuracy, ref float currentCOI, float modAim, ref float currentAimSpeedMod, float modReload, 
            ref float currentReloadSpeedMod, float modFix, ref float currentFixSpeedMod, float modErgo, ref float currentErgo, float modVRecoil,
            ref float currentVRecoil, float modHRecoil, ref float currentHRecoil, ref float currentChamberSpeedMod, float modChamber, bool isDisplayDelta,
            string weapClass, ref float pureErgo, float modShotDisp, ref float currentShotDisp, float modloudness, ref float currentLoudness, 
            ref float currentMalfChance, float modMalfChance, ref float pureRecoil, ref float currentConv, float modConv, ref float currentCamReturnSpeed, bool isChonker,
            ref float currentFlashSuppression, float modFlashSuppression, ref float currentGas)
        {
            float ergoWeightFactor = WeightStatCalc(StatCalc.ErgoWeightMult, isChonker ? modWeight * 0.5f : modWeight) / 100f;
            float vRecoilWeightFactor = WeightStatCalc(StatCalc.VRecoilWeightMult, modWeight) / 100f;
            float hRecoilWeightFactor = WeightStatCalc(StatCalc.HRecoilWeightMult, modWeight) / 100f;
            float dispersionWeightFactor = WeightStatCalc(StatCalc.DispersionWeightMult, modWeight) / 100f;
            float camRecoilWeightFactor = WeightStatCalc(StatCalc.CamWeightMult, modWeight) / 100f;

            currentTorque += GetTorque(position, modWeightFactored, weapClass);
            currentErgo = currentErgo + (currentErgo * ((modErgo / 100f) + ergoWeightFactor));
            currentVRecoil = currentVRecoil + (currentVRecoil * ((modVRecoil / 100f) + vRecoilWeightFactor));
            currentHRecoil = currentHRecoil + (currentHRecoil * ((modHRecoil / 100f) + hRecoilWeightFactor));
            currentCamRecoil = currentCamRecoil + (currentCamRecoil * ((modCamRecoil / 100f) + camRecoilWeightFactor));
            currentDispersion = currentDispersion + (currentDispersion * ((modDispersion / 100f) + dispersionWeightFactor));
            currentRecoilAngle = currentRecoilAngle + (currentRecoilAngle * (modAngle / 100f));
            currentCOI = currentCOI + (currentCOI * (-modAccuracy / 100f));
            currentAutoROF = currentAutoROF + (currentAutoROF * (modAutoROF / 100f));
            currentSemiROF = currentSemiROF + (currentSemiROF * (modSemiROF / 100f));
            pureErgo = pureErgo + (pureErgo * (modErgo / 100f));
            currentCamReturnSpeed = Mathf.Clamp(currentCamReturnSpeed + (currentCamReturnSpeed * (modCamRecoil / 100f)), 0.1f, 0.5f);
            currentConv = currentConv + (currentConv * ((modConv / 100f)));

            if (isDisplayDelta)
            {
                return;
            }

            currentShotDisp = currentShotDisp + (currentShotDisp * ((-1f * modShotDisp) / 100f));
            currentMalfChance = currentMalfChance + (currentMalfChance * (modMalfChance / 100f));
            currentReloadSpeedMod = currentReloadSpeedMod + modReload;
            currentChamberSpeedMod = currentChamberSpeedMod + modChamber;
            currentFixSpeedMod = currentFixSpeedMod + modFix;
            currentLoudness = currentLoudness + modloudness;
            if (!Utils.IsMuzzleCombo(mod) && !Utils.IsFlashHider(mod) && !Utils.IsBarrel(mod)) currentGas = currentGas + modFlashSuppression;
            else currentFlashSuppression = currentFlashSuppression + modFlashSuppression;


            if (Utils.IsSilencer(mod))
            {
                pureRecoil = pureRecoil + (pureRecoil * ((modHRecoil * 0.5f) / 100f) + ((modCamRecoil * 0.5f) / 100f));
            }
            else
            {
                pureRecoil = pureRecoil + (pureRecoil * ((modVRecoil / 100f) + (modHRecoil / 100f) + (modCamRecoil / 100f) + (-modConv / 100f) + (modDispersion / 100f)));
            }

            if (!Utils.IsSight(mod))
            {
                currentAimSpeedMod = currentAimSpeedMod + modAim;
            }

        }


        public static void ModConditionalStatCalc(Weapon weap, Mod mod, bool folded, string weapType, string weapOpType, ref bool hasShoulderContact, ref float modAutoROF, ref float modSemiROF, ref bool stockAllowsFSADS, ref float modVRecoil, ref float modHRecoil, ref float modCamRecoil, ref float modAngle, ref float modDispersion, ref float modErgo, ref float modAccuracy, ref string modType, ref string position, ref float modChamber, ref float modLoudness, ref float modMalfChance, ref float modDuraBurn, ref float modConv, ref float modFlash)
        {
            Mod parent = null;
            if (mod?.Parent?.Container?.ParentItem != null)
            {
                parent = mod.Parent.Container.ParentItem as Mod;
            }

            if (Utils.IsStock(mod) == true)
            {
                if (folded)
                {
                    modDuraBurn = 1;
                    modMalfChance = 0;
                    modAutoROF = 0;
                    modSemiROF = 0;
                    modConv = 0;
                    modVRecoil = 0;
                    modHRecoil = 0;
                    modCamRecoil = 0;
                    modAngle = 0;
                    modDispersion = 0;
                    modErgo = 0;
                    modAccuracy = 0;
                    modType = "folded_stock";
                    position = "neutral";
                    hasShoulderContact = false;
                    return;
                }

                if (!folded)
                {
                    if (modType.StartsWith("Stock") || modType == "buffer_stock")
                    {
                        hasShoulderContact = mod.Template.HasShoulderContact;
                        stockAllowsFSADS = AttachmentProperties.StockAllowADS(mod);
                    }

                    if (weapOpType != "buffer")
                    {
                        if (modType == "buffer")
                        {
                            modConv = 0;
                            modVRecoil = 0;
                            modHRecoil = 0;
                            modDispersion = 0;
                            modCamRecoil = 0;
                            modAutoROF = 0;
                            modSemiROF = 0;
                            modDuraBurn = 1;
                            modMalfChance = 0;
                            return;
                        }
                        if (modType == "buffer_stock")
                        {
                            modConv = 0;
                            modAutoROF = 0;
                            modSemiROF = 0;
                            modDuraBurn = 1;
                            modMalfChance = 0;
                            return;
                        }

                    }

                    StatCalc.StockPositionChecker(mod, ref modVRecoil, ref modHRecoil, ref modDispersion, ref modCamRecoil, ref modErgo);

                    if (modType == "buffer_adapter" || modType == "stock_adapter")
                    {
                        if (mod.Slots.Length > 1 && mod.Slots[1].ContainedItem != null)
                        {
                            modVRecoil += WeaponStats.AdapterPistolGripBonusVRecoil;
                            modHRecoil += WeaponStats.AdapterPistolGripBonusHRecoil;
                            modDispersion += WeaponStats.AdapterPistolGripBonusDispersion;
                            modErgo += WeaponStats.AdapterPistolGripBonusErgo;
                            modChamber += WeaponStats.AdapterPistolGripBonusChamber;
                        }
                        if (mod.Slots[0].ContainedItem != null)
                        {
                            Mod containedMod = mod.Slots[0].ContainedItem as Mod;
                            if (AttachmentProperties.ModType(containedMod) != "buffer")
                            {
                                return;
                            }
                            for (int i = 0; i < containedMod.Slots.Length; i++)
                            {
                                if (containedMod.Slots[i].ContainedItem != null)
                                {
                                    return;
                                }
                            }
                        }

                        modVRecoil = 0;
                        modHRecoil = 0;
                        modDispersion = 0;
                        modCamRecoil = 0;
                        modErgo = 0;
                        return;
                    }

                    if (modType == "hydraulic_buffer")
                    {
                        if (WeaponStats.IsManuallyOperated(weap))
                        {
                            modMalfChance = 0;
                        }
                        if (weap.WeapClass != "shotgun" || weap.WeapClass != "sniperRifle" || weap.WeapClass != "assaultCarbine" || weapOpType == "buffer")
                        {
                            modConv = 0;
                            modVRecoil = 0;
                            modHRecoil = 0;
                            modDispersion = 0;
                            modCamRecoil = 0;
                            return;
                        }
                        return;
                    }
                }
            }

            if (modType == "mount" || modType == "sight")
            {
                modAccuracy = 0;
            }

            if ((modType == "booster" || Utils.IsSilencer(mod)) && (weapType == "short_AK" || (parent != null && AttachmentProperties.ModType(parent) == "short_barrel")))
            {
                if (Utils.IsSilencer(mod))
                {
                    modAutoROF *= 2.5f;
                    modSemiROF *= 2.5f;
                    modMalfChance *= 3f;
                    modDuraBurn = ((modDuraBurn - 1f) * 1.15f) + 1f;
                }
                else
                {
                    modAutoROF *= 3f;
                    modSemiROF *= 3f;
                    modMalfChance *= 10f;
                    modDuraBurn = ((modDuraBurn - 1f) * 3f) + 1f;
                }
                return;
            }

            if (modType == "foregrip_adapter" && mod.Slots[0].ContainedItem != null)
            {
                modErgo = 0f;
                return;
            }

            if (Utils.IsSilencer(mod) || Utils.IsFlashHider(mod) || Utils.IsMuzzleCombo(mod))
            {
                if (WeaponStats.IsManuallyOperated(weap))
                {
                    modMalfChance = 0f;
                    modDuraBurn = ((modDuraBurn - 1f) * 0.25f) + 1f;
                }
                if (WeaponStats.WeaponType(weap) == "DI")
                {
                    modDuraBurn *= 1.25f;
                }
            }

            if (modType == "muzzle_supp_adapter" && mod.Slots[0].ContainedItem != null)
            {
                Mod containedMod = mod.Slots[0].ContainedItem as Mod;
                if (Utils.IsSilencer(containedMod))
                {
                    modConv *= 0.1f;
                    modVRecoil *= 0.1f;
                    modHRecoil *= 0.1f;
                    modCamRecoil *= 0.1f;
                    modDispersion *= 0.1f;
                    modAngle *= 0.1f;
                    modLoudness = 0f;
                    modFlash = 0f;
                }
                return;
            }

            if (modType == "shot_pump_grip_adapt" && mod.Slots[0].ContainedItem != null)
            {
                Mod containedMod = mod.Slots[0].ContainedItem as Mod;
                if (Utils.IsForegrip(containedMod) || (AttachmentProperties.ModType(containedMod) == "foregrip_adapter" && containedMod.Slots[0].ContainedItem != null))
                {
                    modChamber += WeaponStats.PumpGripReloadBonus;
                }
                return;
            }

            if (modType == "grip_stock_adapter")
            {
                if (mod.Slots[0].ContainedItem != null)
                {
                    Mod containedMod = mod.Slots[0].ContainedItem as Mod;
                    if (AttachmentProperties.ModType(containedMod).StartsWith("Stock"))
                    {
                        return;
                    }
                    else if (containedMod.Slots.Length > 0f && containedMod.Slots[0].ContainedItem != null)
                    {
                        return;
                    }
                }
                modVRecoil = 0f;
                modHRecoil = 0f;
                modDispersion = 0f;
                modCamRecoil = 0f;
                return;
            }

            if (modType == "sig_taper_brake")
            {
                if (parent != null && mod.Parent.Container != null)
                {
                    if (parent.Slots[1].ContainedItem != null)
                    {
                        modConv *= 0.1f;
                        modVRecoil *= 0.1f;
                        modHRecoil *= 0.1f;
                        modCamRecoil *= 0.1f;
                        modDispersion *= 0.1f;
                        modAngle *= 0.1f;
                        modLoudness = 0f;
                        modFlash = 0f;
                    }
                }
                return;
            }

            if (modType == "barrel_2slot")
            {
                if (parent != null && mod.Parent.Container != null)
                {
                    if (parent.Slots[1].ContainedItem != null)
                    {
                        modConv *= 0.1f;
                        modVRecoil *= 0.1f;
                        modHRecoil *= 0.1f;
                        modCamRecoil *= 0.1f;
                        modDispersion *= 0.1f;
                        modAngle *= 0.1f;
                        modLoudness = 0f;
                        modFlash = 0f;
                    }
                }
                return;
            }


/*            if (modType == "short_barrel")
            {

                if (mod.Slots.Length > 0f && mod.Slots[1].ContainedItem != null)
                {
                    Mod containedMod = mod.Slots[1].ContainedItem as Mod;
                    if (AttachmentProperties.ModType(containedMod) == "gasblock_upgassed")
                    {
                        modMalfChance = 0f;
                    }
                }

                return;
            }*/
            /*            if (modType == "gasblock_upgassed")
            {
                if (parent != null && AttachmentProperties.ModType(parent) == "short_barrel")
                {
                    modDuraBurn = 1.3f;
                    modHRecoil = 15f;
                    modCamRecoil = 15f;
                    modSemiROF = 10f;
                    modAutoROF = 6f;
                }
                return;
            }*/
            /*            if (modType == "gasblock_downgassed")
                        {
                            if (AttachmentProperties.ModType(parent) == "short_barrel")
                            {
                                modMalfChance *= 1.25f;
                                modHRecoil *= 1.25f;
                                modCamRecoil *= 1.25f;
                                modSemiROF *= 1.25f;
                                modAutoROF *= 1.25f;
                            }
                            return;
                        }*/
        }

        public static string GetModPosition(Mod mod, string weapType, string opType, string modType)
        {
            if (modType == "StockN")
            {
                return "neutral";
            }
            if (modType == "StockF")
            {
                return "front";
            }
            if (modType == "StockR")
            {
                return "rearHalf";
            }
            if (modType == "counterWeight")
            {
                return "frontFar";
            }
            if (modType == "shotTube" || Utils.IsTacticalCombo(mod) || Utils.IsFlashlight(mod))
            {
                return "frontHalf";
            }
            if (weapType == "pistol" || weapType == "bullpup")
            {
                if (weapType == "pistol" && Utils.IsMount(mod))
                {
                    return "front";
                }
                if (Utils.IsStock(mod) || Utils.IsMagazine(mod))
                {
                    return "rear";
                }
                if (Utils.IsUBGL(mod) || Utils.IsHandguard(mod) || Utils.IsGasblock(mod) || Utils.IsFlashHider(mod) || Utils.IsForegrip(mod) || Utils.IsMuzzleCombo(mod) || Utils.IsSilencer(mod) || Utils.IsBipod(mod) || Utils.IsBarrel(mod))
                {
                    return "front";
                }
                else
                {
                    return "neutral";
                }
            }
            else if (opType == "p90" || opType.Contains("tubefed") || opType == "magForward")
            {
                if (Utils.IsStock(mod))
                {
                    return "rear";
                }
                if (Utils.IsUBGL(mod) || Utils.IsMagazine(mod) || Utils.IsHandguard(mod) || Utils.IsGasblock(mod) || Utils.IsFlashHider(mod) || Utils.IsForegrip(mod) || Utils.IsMuzzleCombo(mod) || Utils.IsSilencer(mod) || Utils.IsTacticalCombo(mod) || Utils.IsFlashlight(mod) || Utils.IsBipod(mod) || Utils.IsBarrel(mod))
                {
                    return "front";
                }
                else
                {
                    return "neutral";
                }
            }
            else
            {
                if (Utils.IsStock(mod) || Utils.IsPistolGrip(mod))
                {
                    return "rear";
                }
                if (Utils.IsUBGL(mod) || Utils.IsTacticalCombo(mod) || Utils.IsFlashlight(mod) || Utils.IsBipod(mod) || Utils.IsHandguard(mod) || Utils.IsGasblock(mod) || Utils.IsForegrip(mod) || Utils.IsMuzzleCombo(mod) || Utils.IsFlashHider(mod) || Utils.IsSilencer(mod) || Utils.IsBarrel(mod))
                {
                    return "front";
                }
                else
                {
                    return "neutral";
                }
            }
        }


        private static void StockPositionChecker(Mod mod, ref float modVRecoil, ref float modHRecoil, ref float modDispersion, ref float modCamRecoil, ref float modErgo)
        {

            if (mod.Parent.Container != null)
            {
                string parentType = AttachmentProperties.ModType(mod.Parent.Container.ParentItem);
                if (parentType == "buffer" || parentType == "buffer_adapter")
                {
                    Mod parentMod = mod.Parent.Container.ParentItem as Mod;
                    for (int i = 0; i < parentMod.Slots.Length; i++)
                    {
                        if (parentMod.Slots[i].ContainedItem != null)
                        {
                            StatCalc.BufferSlotModifier(i, ref modVRecoil, ref modHRecoil, ref modDispersion, ref modCamRecoil, ref modErgo);
                            return;
                        }
                    }
                }
            }
        }

        public static float WeightStatCalc(float statFactor, float itemWeight)
        {
            return itemWeight * -statFactor;
        }

        public static float FactoredWeight(float modWeight)
        {
            return Mathf.Clamp((float)Math.Pow(modWeight * 1.5f, 1.1f) / 1.1f, 0.001f, 5f);
        }

        private static float TorqueCalc(float distance, float weight, string weapClass)
        {
            if (weapClass == "pistol")
            {
                distance *= 2.5f;
            }
            return (distance - 0f) * weight;
        }

        public static float GetTorque(string position, float weight, string weapClass)
        {
            float torque = 0f;
            switch (position)
            {
                case "front":
                    torque = TorqueCalc(-10f, weight, weapClass);
                    break;
                case "rear":
                    torque = TorqueCalc(10f, weight, weapClass);
                    break;
                case "rearHalf":
                    torque = TorqueCalc(2.5f, weight, weapClass);
                    break;
                case "frontFar":
                    torque = TorqueCalc(-15f, weight, weapClass);
                    break;
                case "frontHalf":
                    torque = TorqueCalc(-5f, weight, weapClass);
                    break;
                default:
                    torque = 0f;
                    break;
            }
            return torque;
        }

        private static void BufferSlotModifier(int position, ref float modVRecoil, ref float modHRecoil, ref float modDispersion, ref float modCamRecoil, ref float modErgo)
        {
            switch (position)
            {
                case 0:
                    modVRecoil *= 0.5f;
                    modHRecoil *= 0.5f;
                    modDispersion *= 0.5f;
                    modCamRecoil *= 0.5f;
                    modErgo *= 1.5f;
                    break;
                case 1:
                    modVRecoil *= 0.75f;
                    modHRecoil *= 0.75f;
                    modDispersion *= 0.75f;
                    modCamRecoil *= 0.75f;
                    modErgo *= 1.25f;
                    break;
                case 2:
                    modVRecoil *= 1f;
                    modHRecoil *= 1f;
                    modDispersion *= 1f;
                    modCamRecoil *= 1f;
                    modErgo *= 1f;
                    break;
                case 3:
                    modVRecoil *= 1.25f;
                    modHRecoil *= 1.25f;
                    modDispersion *= 1.25f;
                    modCamRecoil *= 1.25f;
                    modErgo *= 0.75f;
                    break;
                default:
                    modVRecoil *= 1f;
                    modHRecoil *= 1f;
                    modDispersion *= 1f;
                    modCamRecoil *= 1f;
                    modErgo *= 1f;
                    break;
            }
        }

        public static float CaliberSmoke(string caliber)
        {
            switch (caliber)
            {
                case "9x18PM":
                    return 0.4f;
                case "57x28":
                    return 0.4f;
                case "46x30":
                    return 0.35f;
                case "9x21":
                    return 0.45f;
                case "762x25TT":
                    return 0.5f;
                case "1143x23ACP":
                    return 0.45f;
                case "9x19PARA":
                    return 0.425f;
                case "9x33R":
                    return 1f;

                case "762x35":
                    return 0.5f;
                case "9x39":
                    return 0.45f;

                case "762x39":
                    return 0.65f;
                case "545x39":
                    return 0.5f;
                case "556x45NATO":
                    return 0.5f;
                case "366TKM":
                    return 0.9f;

                case "762x51":
                    return 0.8f;
                case "762x54R":
                    return 0.9f;
                case "68x51":
                    return 0.85f;

                case "127x55":
                    return 1.2f;
                case "86x70":
                    return 1.35f;
                case "127x108":
                    return 1.5f;

                case "23x75":
                    return 1.2f;
                case "12g":
                    return 1f;
                case "20g":
                    return 0.8f;

                case "30x29":
                    return 1f;
                case "40x46":
                    return 1f;
                case "40x53":
                    return 1f;
                default:
                    return 0.5f;
            }
        }

        public static float CaliberFlame(string caliber)
        {
            switch (caliber)
            {
                case "9x18PM":
                    return 0.05f;
                case "57x28":
                    return 0.065f;
                case "46x30":
                    return 0.055f;
                case "9x21":
                    return 0.055f;
                case "762x25TT":
                    return 0.065f;
                case "1143x23ACP":
                    return 0.05f;
                case "9x19PARA":
                    return 0.06f;
                case "9x33R":
                    return 0.2f;

                case "762x35":
                    return 0.07f;
                case "9x39":
                    return 0.065f;

                case "762x39":
                    return 0.1f;
                case "545x39":
                    return 0.09f;
                case "556x45NATO":
                    return 0.09f;
                case "366TKM":
                    return 0.1f;

                case "762x51":
                    return 0.15f;
                case "762x54R":
                    return 0.14f;
                case "68x51":
                    return 0.12f;


                case "127x55":
                    return 0.13f;
                case "86x70":
                    return 0.19f;
                case "127x108":
                    return 0.22f;

                case "23x75":
                    return 0.14f;
                case "12g":
                    return 0.12f;
                case "20g":
                    return 0.1f;

                case "30x29":
                    return 0.06f;
                case "40x46":
                    return 0.06f;
                case "40x53":
                    return 0.06f;
                default:
                    return 0.05f;
            }
        }

        public static int CaliberSparks(string caliber)
        {
            switch (caliber)
            {
                case "9x18PM":
                    return 0;
                case "57x28":
                    return 1;
                case "46x30":
                    return 0;
                case "9x21":
                    return 2;
                case "762x25TT":
                    return 3;
                case "1143x23ACP":
                    return 1;
                case "9x19PARA":
                    return 2;
                case "9x33R":
                    return 10;

                case "762x35":
                    return 0;
                case "9x39":
                    return 0;

                case "762x39":
                    return 4;
                case "545x39":
                    return 3;
                case "556x45NATO":
                    return 3;
                case "366TKM":
                    return 2;

                case "762x51":
                    return 5;
                case "762x54R":
                    return 5;
                case "68x51":
                    return 4;

                case "127x55":
                    return 4;
                case "86x70":
                    return 6;
                case "127x108":
                    return 7;

                case "23x75":
                    return 3;
                case "12g":
                    return 2;
                case "20g":
                    return 1;

                case "30x29":
                    return 0;
                case "40x46":
                    return 0;
                case "40x53":
                    return 0;
                default:
                    return 0;
            }
        }


        public static int CaliberMuzzleFlash(string caliber)
        {
            switch (caliber)
            {
                case "9x18PM":
                    return 10;
                case "57x28":
                    return 12;
                case "46x30":
                    return 11;
                case "9x21":
                    return 13;
                case "762x25TT":
                    return 14;
                case "1143x23ACP":
                    return 12;
                case "9x19PARA":
                    return 13;
                case "9x33R":
                    return 30;

                case "762x35":
                    return 12;
                case "9x39":
                    return 11;

                case "762x39":
                    return 16;
                case "545x39":
                    return 15;
                case "556x45NATO":
                    return 15;
                case "366TKM":
                    return 17;

                case "762x51":
                    return 23;
                case "762x54R":
                    return 23;
                case "68x51":
                    return 22;


                case "127x55":
                    return 25;
                case "86x70":
                    return 30;
                case "127x108":
                    return 35;

                case "23x75":
                    return 19;
                case "12g":
                    return 14;
                case "20g":
                    return 12;

                case "30x29":
                    return 8;
                case "40x46":
                    return 8;
                case "40x53":
                    return 8;
                default:
                    return 12;
            }
        }

        public static float CaliberLoudnessFactor(string caliber)
        {
            switch (caliber)
            {
                case "9x18PM":
                    return 2.2f;
                case "57x28":
                    return 2.4f;
                case "46x30":
                    return 2.3f;
                case "9x21":
                    return 2.35f;
                case "762x25TT":
                    return 2.55f;
                case "1143x23ACP":
                    return 2.3f;
                case "9x19PARA":
                    return 2.4f;
                case "9x33R":
                    return 3.3f;

                case "762x35":
                    return 2f;
                case "9x39":
                    return 1.9f;

                case "762x39":
                    return 2.6f;
                case "545x39":
                    return 2.63f;
                case "556x45NATO":
                    return 2.65f;
                case "366TKM":
                    return 2.68f;

                case "762x51":
                    return 2.8f;
                case "762x54R":
                    return 2.82f;
                case "68x51":
                    return 3f;

                case "127x55":
                    return 3.8f;
                case "86x70":
                    return 4f;
                case "127x108":
                    return 4f;
                    
                case "23x75":
                    return 3.35f;
                case "12g":
                    return 3f;
                case "20g":
                    return 2.9f;

                case "30x29":
                    return 2.4f;
                case "40x46":
                    return 2.5f;
                case "40x53":
                    return 2.5f;
                default:
                    return 1f;
            }
        }
    }
}
