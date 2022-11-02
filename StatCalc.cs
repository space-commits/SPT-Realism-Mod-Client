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
namespace RealismMod
{
    public static class StatCalc
    {

        public static float ErgoWeightMult = 11.5f;
        public static float ErgoTorqueMult = 0.9f;

        public static float VRecoilWeightMult = 2.1f;
        public static float VRecoilTorqueMult = 0.6f;

        public static float HRecoilWeightMult = 3.15f;
        public static float HRecoilTorqueMult = 0.75f;

        public static float DispersionWeightMult = 1.5f;
        public static float DispersionTorqueMult = 1.25f;

        public static float CamWeightMult = 3.15f;
        public static float CamTorqueMult = 0.25f;

        public static float AngleTorqueMult = 0.25f;

        public static float DampingWeightMult = 0.055f;//
        public static float DampingTorqueMult = 0.095f;// needs tweaking
        public static float DampingMin = 0.65f;
        public static float DampingMax = 0.77f;
        public static float DampingPistolMin = 0.5f;
        public static float DampingPistolMax = 0.7f;

        public static float HandDampingWeightMult = 0.055f;//
        public static float HandDampinTorqueMult = 0.095f;// needs tweaking
        public static float HandDampingMin = 0.65f;
        public static float HandDampingMax = 0.77f;
        public static float HandDampingPistolMin = 0.5f;
        public static float HandDampingPistolMax = 0.7f;

        public static float ReloadSpeedWeightMult = 0.9f;//
        public static float ReloadSpeedTorqueMult = 1.1f;// needs tweaking
        public static float ReloadSpeedMult = 0.3f;//

        public static float ChamberSpeedWeightMult = 0.9f;//
        public static float ChamberSpeedTorqueMult = 1.1f;// needs tweaking
        public static float ChamberSpeedMult = 0.3f;//

        public static float AimMoveSpeedWeightMult = 0.9f;//
        public static float AimMoveSpeedTorqueMult = 1.1f;// needs tweaking
        public static float AimMoveSpeedMult = 0.2f;//

        public static float magWeightMult = 11f;


        public static void setMagReloadSpeeds(Player.FirearmController __instance, MagazineClass magazine)
        {
            Helper.IsMagReloading = true;
            if (Helper.noMagazineReload == true)
            {
                Player player = (Player)AccessTools.Field(typeof(Player.FirearmController), "_player").GetValue(__instance);
                StatCalc.magReloadSpeedModifier(magazine, false, true);
                player.HandsAnimator.SetAnimationSpeed(WeaponProperties.currentMagReloadSpeed);
            }
            else
            {
                StatCalc.magReloadSpeedModifier(magazine, true, false);
            }
        }

        public static void magReloadSpeedModifier(MagazineClass magazine, bool isNewMag, bool reloadFromNoMag)
        {
            float magWeight = magazine.GetSingleItemTotalWeight() * magWeightMult;
            float magWeightFactor = ((magWeight / 100) * -1f) + 1;
            float magSpeed = AttachmentProperties.ReloadSpeed(magazine);
            float reloadSpeedModiLessMag = WeaponProperties.ReloadSpeedModifier;

            float magSpeedMulti = (magSpeed / 100) + 1;
            float totalReloadSpeed = Mathf.Max(magSpeedMulti * magWeightFactor * reloadSpeedModiLessMag, 0.65f);

            if (reloadFromNoMag == true)
            {
                WeaponProperties.newMagReloadSpeed = totalReloadSpeed;
                WeaponProperties.currentMagReloadSpeed = totalReloadSpeed;
            }
            else
            {
                if (isNewMag == true)
                {
                    WeaponProperties.newMagReloadSpeed = totalReloadSpeed;
                }
                else
                {
                    WeaponProperties.currentMagReloadSpeed = totalReloadSpeed;
                }
            }
        }


        public static float ergoWeightCalc(float totalWeight, float totalErgoDelta)
        {
            float factoredWeight = totalWeight * (1 - (totalErgoDelta * 0.2f));
            return Mathf.Clamp((float)(Math.Pow(factoredWeight * 1.78, 3.5) + 1) / 200, 1f, 115f);
        }

        public static float altErgoWeightCalc(float totalWeight, float pureErgoDelta, float totalTorque)
        {
            float totalTorqueFactorInverse = totalTorque / 100f * -1f;
            float ergoFactoredWeight = totalWeight * (1 - (pureErgoDelta * 0.15f));
            float balancedErgoFactoredWeight = ergoFactoredWeight + (ergoFactoredWeight * (totalTorqueFactorInverse + 0.3f));
            return Mathf.Clamp((float)(Math.Pow(balancedErgoFactoredWeight * 1.78, 3.5) + 1) / 200, 1f, 115f);
        }

        public static float proceduralIntensityFactorCalc(float weapWeight, float idealWeapWeight)
        {
            float weightFactor = 1f;

            //get percentage differenecne between weapon weight and a chosen minimum/threshold weight. Apply that % difference as a multiplier 

            if (weapWeight >= idealWeapWeight)
            {
                weightFactor = ((weapWeight - idealWeapWeight) / Math.Abs(idealWeapWeight)) + 1f;
            }

            return weightFactor;
        }

        public static void speedStatCalc(float ergonomicWeightLessMag, float currentReloadSpeed, float currentFixSpeed, float totalTorque, float weapTorqueLessMag, ref float totalReloadSpeed, ref float totalFixSpeed, ref float totalAimMoveSpeedModifier, float ergoWeight, ref float totalChamberSpeed, float currentChamberSpeed)
        {

            float reloadSpeedWeightFactor = StatCalc.weightStatCalc(StatCalc.ReloadSpeedWeightMult, ergonomicWeightLessMag) / 100;
            float fixSpeedWeightFactor = StatCalc.weightStatCalc(StatCalc.ChamberSpeedWeightMult, ergoWeight) / 100;
            float aimMoveSpeedWeightFactor = StatCalc.weightStatCalc(StatCalc.AimMoveSpeedWeightMult, ergoWeight) / 100;

            float torqueFactor = totalTorque / 100f;
            float weapTorqueLessMagFactor = weapTorqueLessMag / 100f;
            /*   float torqueFactorInverse = totalTorque / 100f * -1f;*/

            totalReloadSpeed = (currentReloadSpeed / 100f) + ((reloadSpeedWeightFactor + (weapTorqueLessMagFactor * StatCalc.ReloadSpeedTorqueMult)) * StatCalc.ReloadSpeedMult);
            totalFixSpeed = (currentFixSpeed / 100f) + ((fixSpeedWeightFactor + (torqueFactor * StatCalc.ChamberSpeedTorqueMult)) * StatCalc.ChamberSpeedMult);
            totalChamberSpeed = (currentChamberSpeed / 100f) + ((fixSpeedWeightFactor + (torqueFactor * StatCalc.ChamberSpeedTorqueMult)) * StatCalc.ChamberSpeedMult);

            totalAimMoveSpeedModifier = (aimMoveSpeedWeightFactor + (torqueFactor * StatCalc.AimMoveSpeedTorqueMult)) * StatCalc.AimMoveSpeedMult;
        }

        public static void weaponStatCalc(Weapon weap, float currentTorque, ref float totalTorque, float currentErgo, float currentVRecoil, float currentHRecoil, float currentDispersion, float currentCamRecoil, float currentRecoilAngle, float baseErgo, float baseVRecoil, float baseHRecoil, ref float totalErgo, ref float totalVRecoil, ref float totalHRecoil, ref float totalDispersion, ref float totalCamRecoil, ref float totalRecoilAngle, ref float totalRecoilDamping, ref float totalRecoilHandDamping, ref float totalErgoDelta, ref float totalVRecoilDelta, ref float totalHRecoilDelta, ref float recoilDamping, ref float recoilHandDamping, float currentCOI, bool hasShoulderContact, ref float totalCOI, ref float totalCOIDelta, float baseCOI, bool isDisplayDelta)
        {
            float weaponBaseWeight = weap.Weight;
            float weaponBaseWeightFactored = factoredWeight(weaponBaseWeight);
            float weaponBaseTorque = torqueCalc(WeaponProperties.BaseTorqueDistance(weap), weaponBaseWeightFactored, weap.WeapClass);

            float ergoWeapBaseWeightFactor = weightStatCalc(StatCalc.ErgoWeightMult, weaponBaseWeight) / 100;
            float vRecoilWeapBaseWeightFactor = weightStatCalc(StatCalc.VRecoilWeightMult, weaponBaseWeight) / 100f;
            float hRecoilWeapBaseWeightFactor = weightStatCalc(StatCalc.HRecoilWeightMult, weaponBaseWeight) / 100f;
            float dispersionWeapBaseWeightFactor = weightStatCalc(StatCalc.DispersionWeightMult, weaponBaseWeight) / 100f;
            float camRecoilWeapBaseWeightFactor = weightStatCalc(StatCalc.CamWeightMult, weaponBaseWeight) / 100f;

            float totalWeapWeight = weap.GetSingleItemTotalWeight();
            float dampingTotalWeightFactor = weightStatCalc(StatCalc.DampingWeightMult, totalWeapWeight) / 100f;
            float handDampingTotalWeightFactor = weightStatCalc(StatCalc.HandDampingWeightMult, totalWeapWeight) / 100f;

            totalTorque = (weaponBaseTorque + currentTorque);

            float totalTorqueFactor = totalTorque / 100f;
            float totalTorqueFactorInverse = totalTorque / 100f * -1f;

            totalErgo = currentErgo + (currentErgo * (ergoWeapBaseWeightFactor + (totalTorqueFactor * StatCalc.ErgoTorqueMult)));
            totalVRecoil = currentVRecoil + (currentVRecoil * (vRecoilWeapBaseWeightFactor + (totalTorqueFactor * StatCalc.VRecoilTorqueMult)));
            totalHRecoil = currentHRecoil + (currentHRecoil * (hRecoilWeapBaseWeightFactor + (totalTorqueFactorInverse * StatCalc.HRecoilTorqueMult)));
            totalCamRecoil = currentCamRecoil + (currentCamRecoil * (camRecoilWeapBaseWeightFactor + (totalTorqueFactorInverse * StatCalc.CamTorqueMult)));
            totalDispersion = currentDispersion + (currentDispersion * (dispersionWeapBaseWeightFactor + (totalTorqueFactor * StatCalc.DispersionTorqueMult)));

            totalRecoilAngle = currentRecoilAngle + (currentRecoilAngle * (totalTorqueFactor * StatCalc.AngleTorqueMult));
            totalCOI = currentCOI + (currentCOI * (WeaponProperties.WeaponAccuracy(weap) / 100));


            if (!hasShoulderContact && weap.WeapClass != "pistol")
            {
                totalErgo *= WeaponProperties.FoldedErgoFactor;
                totalVRecoil *= WeaponProperties.FoldedVRecoilFactor;
                totalHRecoil *= WeaponProperties.FoldedHRecoilFactor;
                totalCamRecoil *= WeaponProperties.FoldedCamRecoilFactor;
                totalDispersion *= WeaponProperties.FoldedDispersionFactor;
                totalRecoilAngle *= WeaponProperties.FoldedRecoilAngleFactor;

                if (weap.WeapClass != "shotgun")
                {
                    totalCOI *= WeaponProperties.FoldedCOIFactor;
                }
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
                totalRecoilHandDamping = Mathf.Clamp(recoilHandDamping + (recoilHandDamping * (handDampingTotalWeightFactor + (totalTorqueFactorInverse * StatCalc.HandDampinTorqueMult))), StatCalc.HandDampingPistolMin, StatCalc.HandDampingPistolMax);
            }
            else
            {
                totalRecoilDamping = Mathf.Clamp(recoilDamping + (recoilDamping * (dampingTotalWeightFactor + (totalTorqueFactor * StatCalc.DampingTorqueMult))), StatCalc.DampingMin, StatCalc.DampingMax);
                totalRecoilHandDamping = Mathf.Clamp(recoilHandDamping + (recoilHandDamping * (handDampingTotalWeightFactor + (totalTorqueFactorInverse * StatCalc.HandDampinTorqueMult))), StatCalc.HandDampingMin, StatCalc.HandDampingMax);
            }
        }


        public static void modStatCalc(Mod mod, float modWeight, ref float currentTorque, string position, float modWeightFactored, float modAutoROF, ref float currentAutoROF, float modSemiROF, ref float currentSemiROF, float modCamRecoil, ref float currentCamRecoil, float modDispersion, ref float currentDispersion, float modAngle, ref float currentRecoilAngle, float modAccuracy, ref float currentCOI, float modAim, ref float currentAimSpeed, float modReload, ref float currentReloadSpeed, float modFix, ref float currentFixSpeed, float modErgo, ref float currentErgo, float modVRecoil, ref float currentVRecoil, float modHRecoil, ref float currentHRecoil, ref float currentChamberSpeed, float modChamber, bool isDisplayDelta, string weapClass, ref float pureErgo)
        {

            float ergoWeightFactor = weightStatCalc(StatCalc.ErgoWeightMult, modWeight) / 100f;
            float vRecoilWeightFactor = weightStatCalc(StatCalc.VRecoilWeightMult, modWeight) / 100f;
            float hRecoilWeightFactor = weightStatCalc(StatCalc.HRecoilWeightMult, modWeight) / 100f;
            float dispersionWeightFactor = weightStatCalc(StatCalc.DispersionWeightMult, modWeight) / 100f;
            float camRecoilWeightFactor = weightStatCalc(StatCalc.CamWeightMult, modWeight) / 100f;

            currentErgo = currentErgo + (currentErgo * ((modErgo / 100f) + ergoWeightFactor));

            currentVRecoil = currentVRecoil + (currentVRecoil * ((modVRecoil / 100f) + vRecoilWeightFactor));

            currentHRecoil = currentHRecoil + (currentHRecoil * ((modHRecoil / 100f) + hRecoilWeightFactor));

            currentTorque += getTorque(position, modWeightFactored, weapClass);

            currentCamRecoil = currentCamRecoil + (currentCamRecoil * ((modCamRecoil / 100f) + camRecoilWeightFactor));

            currentDispersion = currentDispersion + (currentDispersion * ((modDispersion / 100f) + dispersionWeightFactor));

            currentRecoilAngle = currentRecoilAngle + (currentRecoilAngle * (modAngle / 100f));

            currentCOI = currentCOI + (currentCOI * (modAccuracy / 100f));

            currentAutoROF = currentAutoROF + (currentAutoROF * (modAutoROF / 100f));

            currentSemiROF = currentSemiROF + (currentSemiROF * (modSemiROF / 100f));



            if (isDisplayDelta == true)
            {
                return;
            }

            pureErgo = pureErgo + (pureErgo * (modErgo / 100f));

            if (Helper.isSight(mod) == false)
            {
                currentAimSpeed = currentAimSpeed + modAim;
            }

            currentReloadSpeed = currentReloadSpeed + modReload;

            currentChamberSpeed = currentChamberSpeed + modChamber;

            currentFixSpeed = currentFixSpeed + modFix;
        }


        public static void modTypeStatCalc(Weapon weap, Mod mod, bool folded, string weapType, string weapOpType, ref bool hasShoulderContact, ref float modAutoROF, ref float modSemiROF, ref bool stockAllowsFSADS, ref float modVRecoil, ref float modHRecoil, ref float modCamRecoil, ref float modAngle, ref float modDispersion, ref float modErgo, ref float modAccuracy, ref string modType, ref string position, ref float modChamber)
        {
            if (Helper.isStock(mod) == true)
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
                    if (modType == "stock" || modType == "buffer_stock")
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
                            //duraburn = 0 
                            //malfchance = 0
                            return;
                        }
                        if (modType == "buffer_stock")
                        {
                            modAutoROF = 0;
                            modSemiROF = 0;
                            //duraburn = 0 
                            //malfchance = 0
                            return;
                        }

                    }

                    if (modType == "buffer_adapter" || modType == "stock_adapter")
                    {
                        bool adapterContainsStock = false;
                        if (mod.Slots[0].ContainedItem != null)
                        {
                            Mod containedMod = mod.Slots[0].ContainedItem as Mod;
                            if (AttachmentProperties.ModType(containedMod) != "buffer")
                            {
                                adapterContainsStock = true;
                            }
                            if (containedMod.Slots.Length > 0 && containedMod.Slots[0].ContainedItem != null)
                            {
                                adapterContainsStock = true;
                            }
                        }
                        if (adapterContainsStock == false)
                        {
                            modVRecoil = 0;
                            modHRecoil = 0;
                            modDispersion = 0;
                            modCamRecoil = 0;
                            modErgo = 0;
                        }
                        if (mod.Slots.Length > 1 && mod.Slots[1].ContainedItem != null)
                        {
                            modVRecoil += WeaponProperties.AdapterPistolGripBonusVRecoil;
                            modHRecoil += WeaponProperties.AdapterPistolGripBonusHRecoil;
                            modDispersion += WeaponProperties.AdapterPistolGripBonusDispersion;
                            modErgo += WeaponProperties.AdapterPistolGripBonusErgo;
                        }
                        return;
                    }

                    if (modType == "hydraulic_buffer" && (weap.WeapClass != "shotgun" || weap.WeapClass != "sniperRifle" || weap.WeapClass != "assaultCarbine") || weapOpType == "buffer")
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
                modAutoROF *= 0.35f;
                modSemiROF *= 0.35f;
                return;
            }

            if (modType == "foregrip_adapter" && mod.Slots[0].ContainedItem != null)
            {
                modErgo = 0f;
                return;
            }

            if (modType == "muzzle_supp_adapter" && mod.Slots[0].ContainedItem != null)
            {
                Mod containedMod = mod.Slots[0].ContainedItem as Mod;
                if (Helper.isSilencer(containedMod))
                {
                    modVRecoil = 0;
                    modHRecoil = 0;
                    modCamRecoil = 0;
                    modDispersion = 0;
                    modAngle = 0;
                }
                return;
            }


            if (modType == "shot_pump_grip_adapt" && mod.Slots[0].ContainedItem != null)
            {
                Mod containedMod = mod.Slots[0].ContainedItem as Mod;
                if (Helper.isForegrip(containedMod))
                {
                    modChamber += WeaponProperties.PumpGripReloadBonus;
                }
                if (AttachmentProperties.ModType(containedMod) == "foregrip_adapter" && containedMod.Slots[0].ContainedItem != null)
                {
                    modChamber += WeaponProperties.PumpGripReloadBonus;
                }
            }

            if (modType == "grip_stock_adapter")
            {
                bool adapterContainsStock = false;
                if (mod.Slots[0].ContainedItem != null)
                {
                    Mod containedMod = mod.Slots[0].ContainedItem as Mod;
                    if (AttachmentProperties.ModType(containedMod) == "stock")
                    {
                        adapterContainsStock = true;
                    }
                    if (containedMod.Slots.Length > 0 && containedMod.Slots[0].ContainedItem != null)
                    {
                        adapterContainsStock = true;
                    }
                }
                if (adapterContainsStock == false)
                {
                    modVRecoil = 0;
                    modHRecoil = 0;
                    modDispersion = 0;
                    modCamRecoil = 0;
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
                    }
                }
                return;
            }

        }

        public static string getModPosition(Mod mod, string weapType, string opType)
        {
            if (weapType == "pistol" || weapType == "bullpup")
            {
                if (isStock(mod) || isMagazine(mod))
                {
                    return "rear";
                }
                if (isHandguard(mod) || isGasblock(mod) || isFlashHider(mod) || isForegrip(mod) || isMuzzleCombo(mod) || isSilencer(mod) || isTacticalCombo(mod) || isFlashlight(mod) || isBipod(mod) || isBarrel(mod))
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
                if (isStock(mod))
                {
                    return "rear";
                }
                if (isMagazine(mod) || isHandguard(mod) || isGasblock(mod) || isFlashHider(mod) || isForegrip(mod) || isMuzzleCombo(mod) || isSilencer(mod) || isTacticalCombo(mod) || isFlashlight(mod) || isBipod(mod) || isBarrel(mod))
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
                if (isStock(mod) || isPistolGrip(mod))
                {
                    return "rear";
                }
                if (isTacticalCombo(mod) || isFlashlight(mod) || isBipod(mod) || isHandguard(mod) || isGasblock(mod) || isForegrip(mod) || isMuzzleCombo(mod) || isFlashHider(mod) || isSilencer(mod) || isBarrel(mod))
                {
                    return "front";
                }
                else
                {
                    return "neutral";
                }
            }
        }

        public static float weightStatCalc(float statFactor, float itemWeight)
        {
            return itemWeight * statFactor * -1;
        }

        public static float factoredWeight(float modWeight)
        {
            return Mathf.Clamp((float)Math.Pow(modWeight * 1.5, 1.1) / 1.1f, 0.001f, 5f);
        }

        public static float torqueCalc(float distance, float weight, string weapClass)
        {
            if (weapClass == "pistol")
            {
                distance *= 3.5f;
            }
            return (distance - 0) * weight;
        }

        public static float getTorque(string position, float weight, string weapClass)
        {
            float torque = 0;
            switch (position)
            {
                case "front":
                    torque = torqueCalc(-10, weight, weapClass);
                    break;
                case "rear":
                    torque = torqueCalc(10, weight, weapClass);
                    break;
                default:
                    torque = 0;
                    break;
            }
            return torque;
        }
    }
}
