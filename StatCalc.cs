using BepInEx.Logging;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using HarmonyLib.Tools;
using System;
using System.Collections.Generic;
using UnityEngine;
using static RealismMod.Utils;
namespace RealismMod
{
    public static class StatCalc
    {

        public static float ErgoWeightMult = 11.8f;
        public static float ErgoTorqueMult = 0.9f;
        public static float PistolErgoWeightMult = 12f;
        public static float PistolErgoTorqueMult = 1.0f;

        public static float VRecoilWeightMult = 1.95f;
        public static float VRecoilTorqueMult = 0.9f;
        public static float PistolVRecoilWeightMult = 1.5f;
        public static float PistolVRecoilTorqueMult = 2f;

        public static float HRecoilWeightMult = 3.55f;
        public static float HRecoilTorqueMult = 0.4f;
        public static float PistolHRecoilWeightMult = 3.5f;
        public static float PistolHRecoilTorqueMult = 1f;

        public static float DispersionWeightMult = 1.5f;
        public static float DispersionTorqueMult = 1.2f;
        public static float PistolDispersionWeightMult = 0.8f;
        public static float PistolDispersionTorqueMult = 1f;

        public static float CamWeightMult = 3.25f;
        public static float CamTorqueMult = 0.25f;

        public static float AngleTorqueMult = 0.9f;//needs tweaking
        public static float PistolAngleTorqueMult = 0.3f;

        public static float DampingWeightMult = 0.05f;
        public static float DampingTorqueMult = 0.07f;
        public static float DampingMin = 0.67f;
        public static float DampingMax = 0.77f;
        public static float DampingPistolMin = 0.52f;
        public static float DampingPistolMax = 0.7f;

        public static float HandDampingWeightMult = 0.08f;//
        public static float HandDampingTorqueMult = 0.05f;// needs tweaking
        public static float HandDampingMin = 0.67f;
        public static float HandDampingMax = 0.77f;
        public static float HandDampingPistolMin = 0.52f;
        public static float HandDampingPistolMax = 0.7f;

        public static float MagWeightMult = 11f;

        private static List<ArmorComponent> preAllocatedArmorComponents = new List<ArmorComponent>(10);

        public static void SetGearParamaters(Player player)
        {
            float reloadMulti = 1f;
            bool allowADS = true;
            InventoryClass inventory = (InventoryClass)AccessTools.Property(typeof(Player), "Inventory").GetValue(player);
            preAllocatedArmorComponents.Clear();
            inventory.GetPutOnArmorsNonAlloc(preAllocatedArmorComponents);

            reloadMulti *= StatCalc.GetRigReloadSpeed(player);

            foreach (ArmorComponent armorComponent in preAllocatedArmorComponents)
            {
                if (armorComponent.Item.Template._parent == "5448e5284bdc2dcb718b4567") 
                {
                    return;
                }
                reloadMulti *= ArmorProperties.ReloadSpeedMulti(armorComponent.Item);
                GClass2197 armorTemplate = armorComponent.Template as GClass2197;

                if (!ArmorProperties.AllowsADS(armorComponent.Item) && !armorTemplate.HasHinge)
                {
                    allowADS = false;
                }
            }


            PlayerProperties.GearReloadMulti = reloadMulti;
            PlayerProperties.GearAllowsADS = allowADS;
        }

        public static float GetRigReloadSpeed(Player player)
        {
            EquipmentClass equipment = (EquipmentClass)AccessTools.Property(typeof(Player), "Equipment").GetValue(player);
            LootItemClass tacVest = equipment.GetSlot(EquipmentSlot.TacticalVest).ContainedItem as LootItemClass;

            if (tacVest != null)
            {
                return ArmorProperties.ReloadSpeedMulti(tacVest);
            }
            else
            {
                return 1;
            }
        }

        public static void SetMagReloadSpeeds(Player.FirearmController __instance, MagazineClass magazine, bool isQuickReload = false)
        {
            PlayerProperties.IsMagReloading = true;
            StanceController.CancelLowReady = true;

            if (PlayerProperties.NoCurrentMagazineReload == true)
            {
                Player player = (Player)AccessTools.Field(typeof(Player.FirearmController), "_player").GetValue(__instance);
                StatCalc.MagReloadSpeedModifier(magazine, false, true);
                player.HandsAnimator.SetAnimationSpeed(Mathf.Clamp(WeaponProperties.CurrentMagReloadSpeed * PlayerProperties.ReloadInjuryMulti * PlayerProperties.ReloadSkillMulti * PlayerProperties.GearReloadMulti * StanceController.HighReadyManipBuff * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.7f)), 0.6f, 1.25f));
            }
            else
            {
                StatCalc.MagReloadSpeedModifier(magazine, true, false, isQuickReload);
            }
        }

        public static void MagReloadSpeedModifier(MagazineClass magazine, bool isNewMag, bool reloadFromNoMag, bool isQuickReload = false)
        {
            float magWeight = magazine.GetSingleItemTotalWeight() * StatCalc.MagWeightMult;
            float magWeightFactor = (magWeight / -100) + 1;
            float magSpeed = AttachmentProperties.ReloadSpeed(magazine);
            float reloadSpeedModiLessMag = WeaponProperties.TotalReloadSpeedLessMag;

            float magSpeedMulti = (magSpeed / 100) + 1;
            float totalReloadSpeed = magSpeedMulti * magWeightFactor * reloadSpeedModiLessMag;

            if (reloadFromNoMag == true)
            {
                WeaponProperties.NewMagReloadSpeed = totalReloadSpeed;
                WeaponProperties.CurrentMagReloadSpeed = totalReloadSpeed;
            }
            else
            {
                if (isNewMag == true)
                {
                    WeaponProperties.NewMagReloadSpeed = totalReloadSpeed;
                }
                else
                {
                    WeaponProperties.CurrentMagReloadSpeed = totalReloadSpeed;
                }
            }

            if (isQuickReload == true)
            {
                WeaponProperties.NewMagReloadSpeed *= Plugin.QuickReloadSpeedMulti.Value;
                WeaponProperties.CurrentMagReloadSpeed *= Plugin.QuickReloadSpeedMulti.Value;
            }

            WeaponProperties.NewMagReloadSpeed *= Plugin.GlobalReloadSpeedMulti.Value;
            WeaponProperties.CurrentMagReloadSpeed *= Plugin.GlobalReloadSpeedMulti.Value;
        }


        public static float ErgoWeightCalc(float totalWeight, float pureErgoDelta, float totalTorque)
        {
            if (WeaponProperties._WeapClass == "pistol")
            {
                if (totalTorque > 3)
                {
                    totalTorque *= -1f;
                }

                totalWeight *= 3f;
            }

            float totalTorqueFactorInverse = totalTorque / -100f; // put totaltorque / 100 in brackets
            float ergoFactoredWeight = (totalWeight) * (1f - (pureErgoDelta * 0.5f));
            float balancedErgoFactoredWeight = ergoFactoredWeight + (ergoFactoredWeight * (totalTorqueFactorInverse + 0.45f)); //firstly shold have MULTIPLIED not added 0.5f, secondly I should be mutlying totalTorque, not totalTorqueFactorInverse!

            float ergoWeight = Mathf.Clamp((float)(Math.Pow(balancedErgoFactoredWeight * 2.1f, 3.7f) + 1f) / 750f, 1f, 80f);

            return ergoWeight;

            /*          return Mathf.Clamp((float)(Math.Pow(balancedErgoFactoredWeight * 1.78f, 3.5f) + 1f) / 280, 1f, 90f);*/ //old standard
        }

        public static float ProceduralIntensityFactorCalc(float weapWeight, float idealWeapWeight)
        {
            float weightFactor = 1f;

            //get percentage differenecne between weapon weight and a chosen minimum/threshold weight. Apply that % difference as a multiplier 

            if (weapWeight >= idealWeapWeight)
            {
                weightFactor = Mathf.Max((((weapWeight - idealWeapWeight) / idealWeapWeight) * 0.1f) + 1f, 1f);
            }

            return weightFactor;
        }

        private static float PistolErgoWeightSpeedCalc(float weight, float totalTorque, float pureErgoDelta, float totalWeight)
        {
            float totalTorqueFactorInverse = totalTorque > 0 ? totalTorque / 100f : totalTorque / -100f;
            float ergoFactoredWeight = (totalWeight * 1f) * (1f - (pureErgoDelta * 0.4f));
            float balancedErgoFactoredWeight = ergoFactoredWeight + (ergoFactoredWeight * (totalTorqueFactorInverse));
            return Mathf.Clamp((float)(Math.Pow(balancedErgoFactoredWeight * 3.2f, 3.2f) + 1f) / 10f, 1f, 80f);
        }

        public static void SpeedStatCalc(Weapon weap, float ergoWeight, float ergonomicWeightLessMag, float chamberSpeedMod, float reloadSpeedMod, ref float totalReloadSpeed, ref float totalChamberSpeed, ref float totalAimMoveSpeedFactor, ref float totalFiringChamberSpeed, ref float totalChamberCheckSpeed, ref float totalFixSpeed, float pureErgoDelta, float totalWeight, float totalTorque)
        {
            if (weap.WeapClass == "pistol")
            {
                ergoWeight = PistolErgoWeightSpeedCalc(ergoWeight, totalTorque, pureErgoDelta, totalWeight);
                ergonomicWeightLessMag = PistolErgoWeightSpeedCalc(ergonomicWeightLessMag, totalTorque, pureErgoDelta, totalWeight);
            }

            chamberSpeedMod = 1f + (chamberSpeedMod / 100f);
            reloadSpeedMod = 1f + (reloadSpeedMod / 100f);
            float baseFixSpeed = WeaponProperties.BaseFixSpeed(weap);
            float baseChamberCheckSpeed = WeaponProperties.BaseChamberCheckSpeed(weap);
            float baseChamberSpeed = WeaponProperties.BaseChamberSpeed(weap);
            float baseReloadSpeed = WeaponProperties.BaseReloadSpeed(weap);
            float recoilMulti = (1f + (-1f * WeaponProperties.PureRecoilDelta));
            float ergoWeightMulti = (1f - ( ergoWeight / 100f));

            totalFixSpeed = Mathf.Clamp(baseFixSpeed * ergoWeightMulti * chamberSpeedMod, WeaponProperties.MinChamberSpeed(weap), WeaponProperties.MaxChamberSpeed(weap));
            totalFiringChamberSpeed = Mathf.Clamp(baseChamberSpeed * ergoWeightMulti * chamberSpeedMod * recoilMulti, WeaponProperties.MinChamberSpeed(weap), WeaponProperties.MaxChamberSpeed(weap));
            totalChamberSpeed = Mathf.Clamp(baseChamberSpeed * ergoWeightMulti * chamberSpeedMod, WeaponProperties.MinChamberSpeed(weap), WeaponProperties.MaxChamberSpeed(weap));
            totalChamberCheckSpeed = Mathf.Clamp(baseChamberCheckSpeed * ergoWeightMulti * chamberSpeedMod, WeaponProperties.MinChamberSpeed(weap), WeaponProperties.MaxChamberSpeed(weap));
            totalReloadSpeed = Mathf.Clamp(baseReloadSpeed * (1f - (ergonomicWeightLessMag / 100f)) * reloadSpeedMod, WeaponProperties.MinReloadSpeed(weap), WeaponProperties.MaxReloadSpeed(weap));
            totalAimMoveSpeedFactor = 1f - (ergoWeight / 100f);
        }

        public static void WeaponStatCalc(Weapon weap, float currentTorque, ref float totalTorque, float currentErgo, float currentVRecoil, float currentHRecoil, float currentDispersion, float currentCamRecoil, float currentRecoilAngle, float baseErgo, float baseVRecoil, float baseHRecoil, ref float totalErgo, ref float totalVRecoil, ref float totalHRecoil, ref float totalDispersion, ref float totalCamRecoil, ref float totalRecoilAngle, ref float totalRecoilDamping, ref float totalRecoilHandDamping, ref float totalErgoDelta, ref float totalVRecoilDelta, ref float totalHRecoilDelta, ref float recoilDamping, ref float recoilHandDamping, float currentCOI, bool hasShoulderContact, ref float totalCOI, ref float totalCOIDelta, float baseCOI, bool isDisplayDelta)
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
            float weaponBaseTorque = TorqueCalc(WeaponProperties.BaseTorqueDistance(weap), weaponBaseWeightFactored, weap.WeapClass);

            float ergoWeapBaseWeightFactor = WeightStatCalc(ergoWeightMult, weaponBaseWeight) / 100;
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
            float totalTorqueFactorInverse = totalTorque / 100f * -1f;

            totalErgo = currentErgo + (currentErgo * (ergoWeapBaseWeightFactor + (totalTorqueFactorErgo * ergoTorqueMult)));
            totalVRecoil = currentVRecoil + (currentVRecoil * (vRecoilWeapBaseWeightFactor + (totalTorqueFactor * vRecoilTorqueMult)));
            totalHRecoil = currentHRecoil + (currentHRecoil * (hRecoilWeapBaseWeightFactor + (totalTorqueFactorInverse * hRecoilTorqueMult)));
            totalCamRecoil = currentCamRecoil + (currentCamRecoil * (camRecoilWeapBaseWeightFactor + (totalTorqueFactorInverse * StatCalc.CamTorqueMult)));
            totalDispersion = currentDispersion + (currentDispersion * (dispersionWeapBaseWeightFactor + (totalTorqueFactor * dispersionTorqueMult)));

            totalRecoilAngle = currentRecoilAngle + (currentRecoilAngle * (totalTorqueFactor * angleTorqueMulti));
            totalCOI = currentCOI + (currentCOI * ((-1f * WeaponProperties.WeaponAccuracy(weap)) / 100));


            if (!hasShoulderContact && weap.WeapClass != "pistol")
            {
                totalErgo *= WeaponProperties.FoldedErgoFactor;
                totalVRecoil *= WeaponProperties.FoldedVRecoilFactor;
                totalHRecoil *= WeaponProperties.FoldedHRecoilFactor;
                totalCamRecoil *= WeaponProperties.FoldedCamRecoilFactor;
                totalDispersion *= WeaponProperties.FoldedDispersionFactor;
                totalRecoilAngle *= WeaponProperties.FoldedRecoilAngleFactor;
                totalCOI *= WeaponProperties.FoldedCOIFactor;

                //don't think his is neccessary anymore as shot dispersion no longer uses COI delta or COI.
                /*             if (weap.WeapClass != "shotgun")
                             {
                                 totalCOI *= WeaponProperties.FoldedCOIFactor;
                             }*/
            }

            totalCOIDelta = (baseCOI - totalCOI) / (baseCOI * -1f);
            totalErgoDelta = (baseErgo - totalErgo) / (baseErgo * -1f);
            totalVRecoilDelta = (baseVRecoil - totalVRecoil) / (baseVRecoil * -1f);
            totalHRecoilDelta = (baseHRecoil - totalHRecoil) / (baseHRecoil * -1f);

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


        public static void ModStatCalc(Mod mod, float modWeight, ref float currentTorque, string position, float modWeightFactored, float modAutoROF, ref float currentAutoROF, float modSemiROF, ref float currentSemiROF, float modCamRecoil, ref float currentCamRecoil, float modDispersion, ref float currentDispersion, float modAngle, ref float currentRecoilAngle, float modAccuracy, ref float currentCOI, float modAim, ref float currentAimSpeedMod, float modReload, ref float currentReloadSpeedMod, float modFix, ref float currentFixSpeedMod, float modErgo, ref float currentErgo, float modVRecoil, ref float currentVRecoil, float modHRecoil, ref float currentHRecoil, ref float currentChamberSpeedMod, float modChamber, bool isDisplayDelta, string weapClass, ref float pureErgo, float modShotDisp, ref float currentShotDisp, float modloudness, ref float currentLoudness, ref float currentMalfChance, float modMalfChance, ref float pureRecoil)
        {

            float ergoWeightFactor = WeightStatCalc(StatCalc.ErgoWeightMult, modWeight) / 100f;
            float vRecoilWeightFactor = WeightStatCalc(StatCalc.VRecoilWeightMult, modWeight) / 100f;
            float hRecoilWeightFactor = WeightStatCalc(StatCalc.HRecoilWeightMult, modWeight) / 100f;
            float dispersionWeightFactor = WeightStatCalc(StatCalc.DispersionWeightMult, modWeight) / 100f;
            float camRecoilWeightFactor = WeightStatCalc(StatCalc.CamWeightMult, modWeight) / 100f;

            currentErgo = currentErgo + (currentErgo * ((modErgo / 100f) + ergoWeightFactor));

            currentVRecoil = currentVRecoil + (currentVRecoil * ((modVRecoil / 100f) + vRecoilWeightFactor));

            currentHRecoil = currentHRecoil + (currentHRecoil * ((modHRecoil / 100f) + hRecoilWeightFactor));

            currentTorque += GetTorque(position, modWeightFactored, weapClass);

            currentCamRecoil = currentCamRecoil + (currentCamRecoil * ((modCamRecoil / 100f) + camRecoilWeightFactor));

            currentDispersion = currentDispersion + (currentDispersion * ((modDispersion / 100f) + dispersionWeightFactor));

            currentRecoilAngle = currentRecoilAngle + (currentRecoilAngle * (modAngle / 100f));

            currentCOI = currentCOI + (currentCOI * ((-1f * modAccuracy) / 100f));

            currentShotDisp = currentShotDisp + (currentShotDisp * ((-1f * modShotDisp) / 100f));

            currentAutoROF = currentAutoROF + (currentAutoROF * (modAutoROF / 100f));

            currentSemiROF = currentSemiROF + (currentSemiROF * (modSemiROF / 100f));

            currentMalfChance = currentMalfChance + (currentMalfChance * (modMalfChance / 100f));

            if (isDisplayDelta == true)
            {
                return;
            }

            pureErgo = pureErgo + (pureErgo * (modErgo / 100f));

            if (Utils.IsSilencer(mod) == true)
            {
                pureRecoil = pureRecoil + (pureRecoil * ((modHRecoil * 0.5f) / 100f) + ((modCamRecoil * 0.5f) / 100f));
            }
            else
            {
                pureRecoil = pureRecoil + (pureRecoil * ((modVRecoil / 100f) + (modHRecoil / 100f) + (modCamRecoil / 100f)));
            }


            if (!Utils.IsSight(mod))
            {
                currentAimSpeedMod = currentAimSpeedMod + modAim;
            }

            currentReloadSpeedMod = currentReloadSpeedMod + modReload;

            currentChamberSpeedMod = currentChamberSpeedMod + modChamber;

            currentFixSpeedMod = currentFixSpeedMod + modFix;

            currentLoudness = currentLoudness + modloudness;
        }


        public static void ModConditionalStatCalc(Weapon weap, Mod mod, bool folded, string weapType, string weapOpType, ref bool hasShoulderContact, ref float modAutoROF, ref float modSemiROF, ref bool stockAllowsFSADS, ref float modVRecoil, ref float modHRecoil, ref float modCamRecoil, ref float modAngle, ref float modDispersion, ref float modErgo, ref float modAccuracy, ref string modType, ref string position, ref float modChamber, ref float modLoudness, ref float modMalfChance, ref float modDuraBurn)
        {
            if (Utils.IsStock(mod) == true)
            {
                if (folded)
                {
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

                            modVRecoil = 0;
                            modHRecoil = 0;
                            modDispersion = 0;
                            modCamRecoil = 0;
                            modAutoROF = 0;
                            modSemiROF = 0;
                            modDuraBurn = 0;
                            modMalfChance = 0;
                            return;
                        }
                        if (modType == "buffer_stock")
                        {
                            modAutoROF = 0;
                            modSemiROF = 0;
                            modDuraBurn = 0;
                            modMalfChance = 0;
                            return;
                        }

                    }

                    if (Utils.ProgramKEnabled == true)
                    {
                        StatCalc.StockPositionChecker(mod, ref modVRecoil, ref modHRecoil, ref modDispersion, ref modCamRecoil, ref modErgo);
                    }

                    if (modType == "buffer_adapter" || modType == "stock_adapter")
                    {
                        if (mod.Slots.Length > 1 && mod.Slots[1].ContainedItem != null)
                        {
                            modVRecoil += WeaponProperties.AdapterPistolGripBonusVRecoil;
                            modHRecoil += WeaponProperties.AdapterPistolGripBonusHRecoil;
                            modDispersion += WeaponProperties.AdapterPistolGripBonusDispersion;
                            modErgo += WeaponProperties.AdapterPistolGripBonusErgo;
                            modChamber += WeaponProperties.AdapterPistolGripBonusChamber;
                        }
                        if (mod.Slots[0].ContainedItem != null)
                        {
                            Mod containedMod = mod.Slots[0].ContainedItem as Mod;
                            if (AttachmentProperties.ModType(containedMod) != "buffer")
                            {
                                return;
                            }
                            if (!Utils.ProgramKEnabled)
                            {
                                if (containedMod.Slots.Length > 0 && (containedMod.Slots[0].ContainedItem != null))
                                {
                                    return;
                                }
                            }
                            else
                            {
                                for (int i = 0; i < containedMod.Slots.Length; i++)
                                {
                                    if (containedMod.Slots[i].ContainedItem != null)
                                    {
                                        return;
                                    }
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

                    if (modType == "hydraulic_buffer" && (weap.WeapClass != "shotgun" || weap.WeapClass != "sniperRifle" || weap.WeapClass != "assaultCarbine" || weapOpType == "buffer"))
                    {
                        modVRecoil = 0;
                        modHRecoil = 0;
                        modDispersion = 0;
                        modCamRecoil = 0;
                        return;
                    }
                }
            }

            if (modType == "booster" && weapType != "short_AK")
            {
                modAutoROF *= 0.25f;
                modSemiROF *= 0.25f;
                modMalfChance *= 0.05f;
                modDuraBurn = ((modDuraBurn - 1f) * 0.25f) + 1f;
                return;
            }


            if (modType == "foregrip_adapter" && mod.Slots[0].ContainedItem != null)
            {
                modErgo = 0f;
                return;
            }

            if (Utils.IsSilencer(mod) == true || Utils.IsFlashHider(mod) == true || Utils.IsMuzzleCombo(mod) == true)
            {
                if (WeaponProperties._IsManuallyOperated == true)
                {
                    modMalfChance = 0f;
                    modDuraBurn = ((modDuraBurn - 1f) * 0.25f) + 1f;
                }
                if (WeaponProperties.WeaponType(weap) == "DI")
                {
                    modDuraBurn = ((modDuraBurn - 1f) * 1.3f) + 1f;
                }
            }

            if (modType == "muzzle_supp_adapter" && mod.Slots[0].ContainedItem != null)
            {
                Mod containedMod = mod.Slots[0].ContainedItem as Mod;
                if (Utils.IsSilencer(containedMod))
                {
                    modVRecoil = 0;
                    modHRecoil = 0;
                    modCamRecoil = 0;
                    modDispersion = 0;
                    modAngle = 0;
                    modLoudness = 0;
                }
                return;
            }

            if (modType == "shot_pump_grip_adapt" && mod.Slots[0].ContainedItem != null)
            {
                Mod containedMod = mod.Slots[0].ContainedItem as Mod;
                if (Utils.IsForegrip(containedMod) || (AttachmentProperties.ModType(containedMod) == "foregrip_adapter" && containedMod.Slots[0].ContainedItem != null))
                {
                    modChamber += WeaponProperties.PumpGripReloadBonus;
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
                    else if (containedMod.Slots.Length > 0 && containedMod.Slots[0].ContainedItem != null)
                    {
                        return;
                    }
                }
                modVRecoil = 0;
                modHRecoil = 0;
                modDispersion = 0;
                modCamRecoil = 0;
                return;
            }

            if (modType == "gasblock_upgassed")
            {
                Mod parent = mod.Parent.Container.ParentItem as Mod;
                if (AttachmentProperties.ModType(parent) == "short_barrel")
                {
                    modDuraBurn = 1.3f;
                    modHRecoil = 15f;
                    modCamRecoil = 15f;
                    modSemiROF = 10f;
                    modAutoROF = 6f;
                }
                return;
            }

            if (modType == "short_barrel")
            {

                if (mod.Slots.Length > 0 && mod.Slots[1].ContainedItem != null)
                {
                    Mod containedMod = mod.Slots[1].ContainedItem as Mod;
                    if (AttachmentProperties.ModType(containedMod) == "gasblock_upgassed")
                    {
                        modMalfChance = 0;
                    }
                }

                return;
            }


            if (modType == "gasblock_downgassed")
            {
                Mod parent = mod.Parent.Container.ParentItem as Mod;
                if (AttachmentProperties.ModType(parent) == "short_barrel")
                {
                    modMalfChance *= 1.25f;
                    modHRecoil *= 1.25f;
                    modCamRecoil *= 1.25f;
                    modSemiROF *= 1.25f;
                    modAutoROF *= 1.25f;
                }
                return;
            }

            if (modType == "sig_taper_brake")
            {
                if (mod.Parent.Container != null)
                {
                    Mod parent = mod.Parent.Container.ParentItem as Mod;
                    if (parent.Slots[1].ContainedItem != null)
                    {
                        modVRecoil = 0;
                        modHRecoil = 0;
                        modCamRecoil = 0;
                        modDispersion = 0;
                        modAngle = 0;
                        modLoudness = 0;
                    }
                }
                return;
            }

            if (modType == "barrel_2slot")
            {
                if (mod.Parent.Container != null)
                {
                    Mod parent = mod.Parent.Container.ParentItem as Mod;
                    if (parent.Slots[1].ContainedItem != null)
                    {
                        modVRecoil = 0;
                        modHRecoil = 0;
                        modCamRecoil = 0;
                        modDispersion = 0;
                        modAngle = 0;
                        modLoudness = 0;
                    }
                }
                return;
            }
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

            if (weapType == "pistol" || weapType == "bullpup")
            {
                if (IsStock(mod) || IsMagazine(mod))
                {
                    return "rear";
                }
                if (IsUBGL(mod) || IsHandguard(mod) || IsGasblock(mod) || IsFlashHider(mod) || IsForegrip(mod) || IsMuzzleCombo(mod) || IsSilencer(mod) || IsTacticalCombo(mod) || IsFlashlight(mod) || IsBipod(mod) || IsBarrel(mod))
                {
                    return "front";
                }
                else
                {
                    return "neutral";
                }
            }
            else if (opType == "p90" || opType == "tubefed" || opType == "magForward")
            {
                if (IsStock(mod))
                {
                    return "rear";
                }
                if (IsUBGL(mod) || IsMagazine(mod) || IsHandguard(mod) || IsGasblock(mod) || IsFlashHider(mod) || IsForegrip(mod) || IsMuzzleCombo(mod) || IsSilencer(mod) || IsTacticalCombo(mod) || IsFlashlight(mod) || IsBipod(mod) || IsBarrel(mod))
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
                if (IsStock(mod) || IsPistolGrip(mod))
                {
                    return "rear";
                }
                if (IsUBGL(mod) || IsTacticalCombo(mod) || IsFlashlight(mod) || IsBipod(mod) || IsHandguard(mod) || IsGasblock(mod) || IsForegrip(mod) || IsMuzzleCombo(mod) || IsFlashHider(mod) || IsSilencer(mod) || IsBarrel(mod))
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
                            StatCalc.bufferSlotModifier(i, ref modVRecoil, ref modHRecoil, ref modDispersion, ref modCamRecoil, ref modErgo);
                            return;
                        }
                    }
                }
            }
        }

        private static void bufferSlotModifier(int position, ref float modVRecoil, ref float modHRecoil, ref float modDispersion, ref float modCamRecoil, ref float modErgo)
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
                    modVRecoil *= 1;
                    modHRecoil *= 1;
                    modDispersion *= 1;
                    modCamRecoil *= 1;
                    modErgo *= 1;
                    break;
            }
        }

        public static float CalibreLoudnessFactor(string calibre)
        {
            switch (calibre)
            {
                case "Caliber9x18PM":
                    return 2.1f;
                case "Caliber57x28":
                    return 2.4f;
                case "Caliber46x30":
                    return 2.3f;
                case "Caliber9x21":
                    return 2.4f;
                case "Caliber762x25TT":
                    return 2.55f;
                case "Caliber1143x23ACP":
                    return 2.2f;
                case "Caliber9x19PARA":
                    return 2.3f;
                case "Caliber9x33R":
                    return 3.3f;
                case "Caliber762x35":
                    return 1.9f;
                case "Caliber9x39":
                    return 1.8f;
                case "Caliber762x39":
                    return 2.68f;
                case "Caliber545x39":
                    return 2.7f;
                case "Caliber556x45NATO":
                    return 2.72f;
                case "Caliber366TKM":
                    return 2.75f;
                case "Caliber762x51":
                    return 3.3f;
                case "Caliber762x54R":
                    return 3.3f;
                case "Caliber127x55":
                    return 4.5f;
                case "Caliber86x70":
                    return 8f;
                case "Caliber127x108":
                    return 8f;
                case "Caliber23x75":
                    return 4.5f;
                case "Caliber12g":
                    return 4.5f;
                case "Caliber20g":
                    return 4f;
                case "Caliber30x29":
                    return 2f;
                case "Caliber40x46":
                    return 1.5f;
                case "Caliber40x53":
                    return 1.5f;
                default:
                    return 1f;
            }
        }

        public static float WeightStatCalc(float statFactor, float itemWeight)
        {
            return itemWeight * statFactor * -1;
        }

        public static float FactoredWeight(float modWeight)
        {
            return Mathf.Clamp((float)Math.Pow(modWeight * 1.5, 1.1) / 1.1f, 0.001f, 5f);
        }

        private static float TorqueCalc(float distance, float weight, string weapClass)
        {
            if (weapClass == "pistol")
            {
                distance *= 2.1f;
            }
            return (distance - 0) * weight;
        }

        public static float GetTorque(string position, float weight, string weapClass)
        {
            float torque = 0;
            switch (position)
            {
                case "front":
                    torque = TorqueCalc(-10, weight, weapClass);
                    break;
                case "rear":
                    torque = TorqueCalc(10, weight, weapClass);
                    break;
                case "rearHalf":
                    torque = TorqueCalc(5, weight, weapClass);
                    break;
                default:
                    torque = 0;
                    break;
            }
            return torque;
        }
    }
}
