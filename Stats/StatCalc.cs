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

        public static float ErgoWeightMult = 13f;
        public static float ErgoTorqueMult = 0.8f;
        public static float PistolErgoWeightMult = 12f;
        public static float PistolErgoTorqueMult = 1.0f;

        public static float VRecoilWeightMult = 2f;
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

        public static float CamWeightMult = 5f;
        public static float CamTorqueMult = 0.25f;

        public static float AngleTorqueMult = 0.4f;//needs tweaking
        public static float PistolAngleTorqueMult = 0.3f;

        public static float DampingWeightMult = 0.04f;
        public static float DampingTorqueMult = 0.055f;
        public static float DampingMin = 0.67f;
        public static float DampingMax = 0.77f;
        public static float DampingPistolMin = 0.52f;
        public static float DampingPistolMax = 0.7f;

        public static float HandDampingWeightMult = 0.07f;//
        public static float HandDampingTorqueMult = 0.04f;// needs tweaking
        public static float HandDampingMin = 0.67f;
        public static float HandDampingMax = 0.77f;
        public static float HandDampingPistolMin = 0.52f;
        public static float HandDampingPistolMax = 0.7f;

        public static float MagWeightMult = 16f;

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
                    break;
                }
                reloadMulti *= GearProperties.ReloadSpeedMulti(armorComponent.Item);
                GClass2442 armorTemplate = armorComponent.Template as GClass2442;

                if (!GearProperties.AllowsADS(armorComponent.Item) && !armorTemplate.HasHinge)
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
                return GearProperties.ReloadSpeedMulti(tacVest);
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
            Weapon weapon = __instance.Item;

            if (PlayerProperties.NoCurrentMagazineReload)
            {
                Player player = (Player)AccessTools.Field(typeof(Player.FirearmController), "_player").GetValue(__instance);
                StatCalc.MagReloadSpeedModifier(weapon, magazine, false, true);
                player.HandsAnimator.SetAnimationSpeed(Mathf.Clamp(WeaponProperties.CurrentMagReloadSpeed * PlayerProperties.ReloadInjuryMulti * PlayerProperties.ReloadSkillMulti * PlayerProperties.GearReloadMulti * StanceController.HighReadyManipBuff * StanceController.ActiveAimManipBuff * (Mathf.Max(PlayerProperties.RemainingArmStamPercentage, 0.7f)), 0.45f, 1.3f));
            }
            else
            {
                StatCalc.MagReloadSpeedModifier(weapon, magazine, true, false, isQuickReload);
            }
        }

        public static void MagReloadSpeedModifier(Weapon weapon, MagazineClass magazine, bool isNewMag, bool reloadFromNoMag, bool isQuickReload = false)
        {
            float magWeight = magazine.GetSingleItemTotalWeight() * StatCalc.MagWeightMult;
            float magWeightFactor = (magWeight / - 100f) + 1f;
            float magSpeed = AttachmentProperties.ReloadSpeed(magazine);
            float reloadSpeedModiLessMag = WeaponProperties.TotalReloadSpeedLessMag;
            float stockModifier = weapon.WeapClass != "pistol" && !WeaponProperties.HasShoulderContact ? 0.8f : 1f;

            float magSpeedMulti = (magSpeed / 100f) + 1f;
            float totalReloadSpeed = magSpeedMulti * magWeightFactor * reloadSpeedModiLessMag * stockModifier;

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
            WeaponProperties.CurrentMagReloadSpeed *= Plugin.GlobalReloadSpeedMulti.Value;;
        }


        public static float ErgoWeightCalc(float totalWeight, float ergoDelta, float totalTorque, string weapClass)
        {
            if (weapClass == "pistol")
            {
                totalTorque = totalTorque < 0 ? totalTorque * 2f : totalTorque;
                float totalTorqueFactorInverse = 1f + (totalTorque > 14 ? totalTorque / 100f : totalTorque / -100f);
                float ergoFactoredWeight = Math.Max(1f, totalWeight * (1f - ergoDelta));
                float balancedErgoFactoredWeight = Math.Max(1f, ergoFactoredWeight * totalTorqueFactorInverse);
                return Mathf.Clamp((float)(Math.Pow(balancedErgoFactoredWeight * 3.2f, 3.2f) + 1f) / 10f, 1f, 80f);
            }
            else 
            {
                totalTorque = totalTorque < 0f ? totalTorque * 3f : totalTorque > 10f ? totalTorque / 2f : totalTorque <= 10f && totalTorque >= 0f ? totalTorque * 1.5f : totalTorque;
                float totalTorqueFactorInverse = 1f + (totalTorque / -100f);;
                float ergoFactoredWeight = Math.Max(1f, totalWeight * (1f - ergoDelta));
                float balancedErgoFactoredWeight = Math.Max(1f, ergoFactoredWeight * totalTorqueFactorInverse);
                return Mathf.Clamp((float)(Math.Pow(balancedErgoFactoredWeight * 2.1f, 3.7f) + 1f) / 750f, 1f, 80f);
            }
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

        public static void SpeedStatCalc(Weapon weap, float ergoWeight, float ergonomicWeightLessMag, float chamberSpeedMod, float reloadSpeedMod, ref float totalReloadSpeedLessMag, ref float totalChamberSpeed, ref float totalAimMoveSpeedFactor, ref float totalFiringChamberSpeed, ref float totalChamberCheckSpeed, ref float totalFixSpeed)
        {
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
            totalReloadSpeedLessMag = Mathf.Clamp(baseReloadSpeed * (1f - (ergonomicWeightLessMag / 100f)) * reloadSpeedMod, WeaponProperties.MinReloadSpeed(weap), WeaponProperties.MaxReloadSpeed(weap));
            totalAimMoveSpeedFactor = Mathf.Max(1f - (ergoWeight / 150f), 0.5f);
        }

        public static void WeaponStatCalc(Weapon weap, float currentTorque, ref float totalTorque, float currentErgo, float currentVRecoil, float currentHRecoil, float currentDispersion, float currentCamRecoil, float currentRecoilAngle, float baseErgo, float baseVRecoil, float baseHRecoil, ref float totalErgo, ref float totalVRecoil, ref float totalHRecoil, ref float totalDispersion, ref float totalCamRecoil, ref float totalRecoilAngle, ref float totalRecoilDamping, ref float totalRecoilHandDamping, ref float totalErgoDelta, ref float totalVRecoilDelta, ref float totalHRecoilDelta, ref float recoilDamping, ref float recoilHandDamping, float currentCOI, bool hasShoulderContact, ref float totalCOI, ref float totalCOIDelta, float baseCOI, float totalPureErgo, ref float totalPureErgoDelta, bool isDisplayDelta)
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

            float ergoWeapBaseWeightFactor = WeightStatCalc(ergoWeightMult, weaponBaseWeight) / 100f;
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

            totalErgo = currentErgo + (currentErgo * (ergoWeapBaseWeightFactor + (totalTorqueFactorErgo * ergoTorqueMult)));
            totalVRecoil = currentVRecoil + (currentVRecoil * (vRecoilWeapBaseWeightFactor + (totalTorqueFactor * vRecoilTorqueMult)));
            totalHRecoil = currentHRecoil + (currentHRecoil * (hRecoilWeapBaseWeightFactor + (totalTorqueFactorInverse * hRecoilTorqueMult)));
            totalCamRecoil = currentCamRecoil + (currentCamRecoil * (camRecoilWeapBaseWeightFactor + (totalTorqueFactorInverse * StatCalc.CamTorqueMult)));
            totalDispersion = currentDispersion + (currentDispersion * (dispersionWeapBaseWeightFactor + (totalTorqueFactor * dispersionTorqueMult)));

            totalRecoilAngle = currentRecoilAngle + (currentRecoilAngle * (totalTorqueFactor * angleTorqueMulti));
            totalCOI = currentCOI + (currentCOI * ((-1f * WeaponProperties.WeaponAccuracy(weap)) / 100));


            if (!hasShoulderContact && weap.WeapClass != "pistol")
            {
                totalPureErgo *= WeaponProperties.FoldedErgoFactor;
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


        public static void ModStatCalc(Mod mod, float modWeight, ref float currentTorque, string position, float modWeightFactored, float modAutoROF, ref float currentAutoROF, float modSemiROF, ref float currentSemiROF, float modCamRecoil, ref float currentCamRecoil, float modDispersion, ref float currentDispersion, float modAngle, ref float currentRecoilAngle, float modAccuracy, ref float currentCOI, float modAim, ref float currentAimSpeedMod, float modReload, ref float currentReloadSpeedMod, float modFix, ref float currentFixSpeedMod, float modErgo, ref float currentErgo, float modVRecoil, ref float currentVRecoil, float modHRecoil, ref float currentHRecoil, ref float currentChamberSpeedMod, float modChamber, bool isDisplayDelta, string weapClass, ref float pureErgo, float modShotDisp, ref float currentShotDisp, float modloudness, ref float currentLoudness, ref float currentMalfChance, float modMalfChance, ref float pureRecoil, ref float currentConv, float modConv)
        {

            float ergoWeightFactor = WeightStatCalc(StatCalc.ErgoWeightMult, modWeight) / 100f;
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
            currentCOI = currentCOI + (currentCOI * ((-1f * modAccuracy) / 100f));
            currentAutoROF = currentAutoROF + (currentAutoROF * (modAutoROF / 100f));
            currentSemiROF = currentSemiROF + (currentSemiROF * (modSemiROF / 100f));
            pureErgo = pureErgo + (pureErgo * (modErgo / 100f));

            if (isDisplayDelta)
            {
                return;
            }

            currentConv = currentConv + (currentConv * ((modConv / 100f)));
            currentShotDisp = currentShotDisp + (currentShotDisp * ((-1f * modShotDisp) / 100f));
            currentMalfChance = currentMalfChance + (currentMalfChance * (modMalfChance / 100f));

            currentReloadSpeedMod = currentReloadSpeedMod + modReload;
            currentChamberSpeedMod = currentChamberSpeedMod + modChamber;
            currentFixSpeedMod = currentFixSpeedMod + modFix;
            currentLoudness = currentLoudness + modloudness;

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


        public static void ModConditionalStatCalc(Weapon weap, Mod mod, bool folded, string weapType, string weapOpType, ref bool hasShoulderContact, ref float modAutoROF, ref float modSemiROF, ref bool stockAllowsFSADS, ref float modVRecoil, ref float modHRecoil, ref float modCamRecoil, ref float modAngle, ref float modDispersion, ref float modErgo, ref float modAccuracy, ref string modType, ref string position, ref float modChamber, ref float modLoudness, ref float modMalfChance, ref float modDuraBurn, ref float modConv)
        {
            if (Utils.IsStock(mod) == true)
            {
                if (folded)
                {
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

                    if (Plugin.EnableStockSlots.Value)
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
                            if (!Plugin.EnableStockSlots.Value)
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

                    if (modType == "hydraulic_buffer")
                    {
                        if (WeaponProperties.IsManuallyOperated(weap)) 
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

            if (Utils.IsSilencer(mod) || Utils.IsFlashHider(mod) || Utils.IsMuzzleCombo(mod))
            {
                if (WeaponProperties.IsManuallyOperated(weap))
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
                    modConv = 0f;
                    modVRecoil = 0f;
                    modHRecoil = 0f;
                    modCamRecoil = 0f;
                    modDispersion = 0f;
                    modAngle = 0f;
                    modLoudness = 0f;
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

                if (mod.Slots.Length > 0f && mod.Slots[1].ContainedItem != null)
                {
                    Mod containedMod = mod.Slots[1].ContainedItem as Mod;
                    if (AttachmentProperties.ModType(containedMod) == "gasblock_upgassed")
                    {
                        modMalfChance = 0f;
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
                        modConv = 0f;
                        modVRecoil = 0f;
                        modHRecoil = 0f;
                        modCamRecoil = 0f;
                        modDispersion = 0f;
                        modAngle = 0f;
                        modLoudness = 0f;
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
                        modConv = 0f;
                        modVRecoil = 0f;
                        modHRecoil = 0f;
                        modCamRecoil = 0f;
                        modDispersion = 0f;
                        modAngle = 0f;
                        modLoudness = 0f;
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
            if (modType == "counterWeight") 
            {
                return "frontFar";
            }
            if (modType == "shotTube")
            {
                return "frontHalf";
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
                    modVRecoil *= 1f;
                    modHRecoil *= 1f;
                    modDispersion *= 1f;
                    modCamRecoil *= 1f;
                    modErgo *= 1f;
                    break;
            }
        }

        public static float CalibreLoudnessFactor(string calibre)
        {
            switch (calibre)
            {
                case "Caliber9x18PM":
                    return 2.2f;
                case "Caliber57x28":
                    return 2.4f;
                case "Caliber46x30":
                    return 2.3f;
                case "Caliber9x21":
                    return 2.35f;
                case "Caliber762x25TT":
                    return 2.55f;
                case "Caliber1143x23ACP":
                    return 2.3f;
                case "Caliber9x19PARA":
                    return 2.4f;
                case "Caliber9x33R":
                    return 3.3f;

                case "Caliber762x35":
                    return 2.3f;
                case "Caliber9x39":
                    return 2.2f;

                case "Caliber762x39":
                    return 2.6f;
                case "Caliber545x39":
                    return 2.63f;
                case "Caliber556x45NATO":
                    return 2.65f;
                case "Caliber366TKM":
                    return 2.68f;

                case "Caliber762x51":
                    return 2.8f;
                case "Caliber762x54R":
                    return 2.82f;
                case "Caliber127x55":
                    return 3.8f;

                case "Caliber86x70":
                    return 6f;
                case "Caliber127x108":
                    return 6f;

                case "Caliber23x75":
                    return 3.35f;
                case "Caliber12g":
                    return 3.5f;
                case "Caliber20g":
                    return 3f;

                case "Caliber30x29":
                    return 2.4f;
                case "Caliber40x46":
                    return 2.5f;
                case "Caliber40x53":
                    return 2.5f;
                default:
                    return 1f;
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
    }
}
