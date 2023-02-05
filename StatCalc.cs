using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using UnityEngine;
using static RealismMod.Helper;
namespace RealismMod
{
    public static class StatCalc
    {

        public static float ErgoWeightMult = 11.8f;
        public static float ErgoTorqueMult = 0.9f;
        public static float PistolErgoWeightMult = 12f;
        public static float PistolErgoTorqueMult = 1.0f;

        public static float VRecoilWeightMult = 2.21f;
        public static float VRecoilTorqueMult = 0.73f;
        public static float PistolVRecoilWeightMult = 2.4f;
        public static float PistolVRecoilTorqueMult = 0.77f;

        public static float HRecoilWeightMult = 3.35f;
        public static float HRecoilTorqueMult = 0.7f;
        public static float PistolHRecoilWeightMult = 3.4f;
        public static float PistolHRecoilTorqueMult = 0.8f;

        public static float DispersionWeightMult = 1.5f;
        public static float DispersionTorqueMult = 1.32f;
        public static float PistolDispersionWeightMult = 1.6f;
        public static float PistolDispersionTorqueMult = 1.4f;

        public static float CamWeightMult = 3.25f;
        public static float CamTorqueMult = 0.25f;

        public static float AngleTorqueMult = 1.0f;//needs tweaking
        public static float PistolAngleTorqueMult = 0.3f;

        public static float DampingWeightMult = 0.07f;
        public static float DampingTorqueMult = 0.1f;
        public static float DampingMin = 0.65f;
        public static float DampingMax = 0.77f;
        public static float DampingPistolMin = 0.5f;
        public static float DampingPistolMax = 0.7f;

        public static float HandDampingWeightMult = 0.07f;//
        public static float HandDampinTorqueMult = 0.1f;// needs tweaking
        public static float HandDampingMin = 0.65f;
        public static float HandDampingMax = 0.77f;
        public static float HandDampingPistolMin = 0.5f;
        public static float HandDampingPistolMax = 0.7f;

        public static float ReloadSpeedWeightMult = 2f;//
        public static float ReloadSpeedTorqueMult = 2f;// needs tweaking
        public static float ReloadSpeedMult = 0.5f;//

        public static float ChamberSpeedWeightMult = 2f;//
        public static float ChamberSpeedTorqueMult = 2f;// needs tweaking
        public static float ChamberSpeedMult = 0.5f;//

        public static float AimMoveSpeedWeightMult = 0.95f;//
        public static float AimMoveSpeedTorqueMult = 1.15f;// needs tweaking
        public static float AimMoveSpeedMult = 0.2f;//

        public static float magWeightMult = 11f;


        public static void SetMagReloadSpeeds(Player.FirearmController __instance, MagazineClass magazine, bool isQuickReload = false)
        {
            Helper.IsMagReloading = true;
            if (Helper.NoMagazineReload == true)
            {
                Player player = (Player)AccessTools.Field(typeof(Player.FirearmController), "_player").GetValue(__instance);
                StatCalc.MagReloadSpeedModifier(magazine, false, true);
                player.HandsAnimator.SetAnimationSpeed(WeaponProperties.CurrentMagReloadSpeed * WeaponProperties.ReloadSpeedModifier * PlayerProperties.ReloadInjuryMulti * PlayerProperties.ReloadSkillMulti);
            }
            else
            {
                StatCalc.MagReloadSpeedModifier(magazine, true, false, isQuickReload);
            }
        }

        public static void MagReloadSpeedModifier(MagazineClass magazine, bool isNewMag, bool reloadFromNoMag, bool isQuickReload = false)
        {
            float magWeight = magazine.GetSingleItemTotalWeight() * magWeightMult;
            float magWeightFactor = ((magWeight / 100) * -1f) + 1;
            float magSpeed = AttachmentProperties.ReloadSpeed(magazine);
            float reloadSpeedModiLessMag = WeaponProperties.ReloadSpeedModifier;

            float magSpeedMulti = (magSpeed / 100) + 1;
            float totalReloadSpeed = Mathf.Max(magSpeedMulti * magWeightFactor * reloadSpeedModiLessMag, 0.65f);

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
                WeaponProperties.NewMagReloadSpeed *= 1.25f;
                WeaponProperties.CurrentMagReloadSpeed *= 1.25f;
            }
        }


        public static float ErgoWeightCalc(float totalWeight, float pureErgoDelta, float totalTorque)
        {
            float totalTorqueFactorInverse = totalTorque / 100f * -1f;
            float ergoFactoredWeight = (totalWeight * 0.95f) * (1f - (pureErgoDelta * 0.3f));
            float balancedErgoFactoredWeight = ergoFactoredWeight + (ergoFactoredWeight * (totalTorqueFactorInverse + 0.25f));
            return Mathf.Clamp((float)(Math.Pow(balancedErgoFactoredWeight * 1.78f, 3.5f) + 1f) / 280, 1f, 115f);
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

        public static void SpeedStatCalc(float ergonomicWeightLessMag, float currentReloadSpeed, float currentFixSpeed, float totalTorque, float weapTorqueLessMag, ref float totalReloadSpeed, ref float totalFixSpeed, ref float totalAimMoveSpeedModifier, float ergoWeight, ref float totalChamberSpeed, float currentChamberSpeed)
        {

            float reloadSpeedWeightFactor = StatCalc.WeightStatCalc(StatCalc.ReloadSpeedWeightMult, ergonomicWeightLessMag * PlayerProperties.StrengthSkillAimBuff) / 100f;
            float fixSpeedWeightFactor = StatCalc.WeightStatCalc(StatCalc.ChamberSpeedWeightMult, ergoWeight * PlayerProperties.StrengthSkillAimBuff) / 100f;
            float aimMoveSpeedWeightFactor = StatCalc.WeightStatCalc(StatCalc.AimMoveSpeedWeightMult, ergoWeight * PlayerProperties.StrengthSkillAimBuff) / 100f;

            float torqueFactor = totalTorque / 100f;
            float weapTorqueLessMagFactor = weapTorqueLessMag / 100f;
            /*   float torqueFactorInverse = totalTorque / 100f * -1f;*/

            totalReloadSpeed = (currentReloadSpeed / 100f) + ((reloadSpeedWeightFactor + (weapTorqueLessMagFactor * StatCalc.ReloadSpeedTorqueMult)) * StatCalc.ReloadSpeedMult);
            totalFixSpeed = (currentFixSpeed / 100f) + ((fixSpeedWeightFactor + (torqueFactor * StatCalc.ChamberSpeedTorqueMult)) * StatCalc.ChamberSpeedMult);
            totalChamberSpeed = (currentChamberSpeed / 100f) + ((fixSpeedWeightFactor + (torqueFactor * StatCalc.ChamberSpeedTorqueMult)) * StatCalc.ChamberSpeedMult);

            totalAimMoveSpeedModifier = (aimMoveSpeedWeightFactor + (torqueFactor * StatCalc.AimMoveSpeedTorqueMult)) * StatCalc.AimMoveSpeedMult;
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

            totalTorque = (weaponBaseTorque + currentTorque);

            float totalTorqueFactor = totalTorque / 100f;
            float totalTorqueFactorInverse = totalTorque / 100f * -1f;

            totalErgo = currentErgo + (currentErgo * (ergoWeapBaseWeightFactor + (totalTorqueFactor * ergoTorqueMult)));
            totalVRecoil = currentVRecoil + (currentVRecoil * (vRecoilWeapBaseWeightFactor + (totalTorqueFactor * vRecoilTorqueMult)));
            totalHRecoil = currentHRecoil + (currentHRecoil * (hRecoilWeapBaseWeightFactor + (totalTorqueFactorInverse * hRecoilTorqueMult)));
            totalCamRecoil = currentCamRecoil + (currentCamRecoil * (camRecoilWeapBaseWeightFactor + (totalTorqueFactorInverse * StatCalc.CamTorqueMult)));
            totalDispersion = currentDispersion + (currentDispersion * (dispersionWeapBaseWeightFactor + (totalTorqueFactor * dispersionTorqueMult)));

            totalRecoilAngle = currentRecoilAngle + (currentRecoilAngle * (totalTorqueFactor * angleTorqueMulti));
            totalCOI = currentCOI + (currentCOI * (WeaponProperties.WeaponAccuracy(weap) / 100));


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
                totalRecoilHandDamping = Mathf.Clamp(recoilHandDamping + (recoilHandDamping * (handDampingTotalWeightFactor + (totalTorqueFactorInverse * StatCalc.HandDampinTorqueMult))), StatCalc.HandDampingPistolMin, StatCalc.HandDampingPistolMax);
            }
            else
            {
                totalRecoilDamping = Mathf.Clamp(recoilDamping + (recoilDamping * (dampingTotalWeightFactor + (totalTorqueFactor * StatCalc.DampingTorqueMult))), StatCalc.DampingMin, StatCalc.DampingMax);
                totalRecoilHandDamping = Mathf.Clamp(recoilHandDamping + (recoilHandDamping * (handDampingTotalWeightFactor + (totalTorqueFactorInverse * StatCalc.HandDampinTorqueMult))), StatCalc.HandDampingMin, StatCalc.HandDampingMax);
            }
        }


        public static void ModStatCalc(Mod mod, float modWeight, ref float currentTorque, string position, float modWeightFactored, float modAutoROF, ref float currentAutoROF, float modSemiROF, ref float currentSemiROF, float modCamRecoil, ref float currentCamRecoil, float modDispersion, ref float currentDispersion, float modAngle, ref float currentRecoilAngle, float modAccuracy, ref float currentCOI, float modAim, ref float currentAimSpeed, float modReload, ref float currentReloadSpeed, float modFix, ref float currentFixSpeed, float modErgo, ref float currentErgo, float modVRecoil, ref float currentVRecoil, float modHRecoil, ref float currentHRecoil, ref float currentChamberSpeed, float modChamber, bool isDisplayDelta, string weapClass, ref float pureErgo, float modShotDisp, ref float currentShotDisp, float modloudness, ref float currentLoudness, ref float currentMalfChance, float modMalfChance)
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

            if (Helper.IsSight(mod) == false)
            {
                currentAimSpeed = currentAimSpeed + modAim;
            }

            currentReloadSpeed = currentReloadSpeed + modReload;

            currentChamberSpeed = currentChamberSpeed + modChamber;

            currentFixSpeed = currentFixSpeed + modFix;

            currentLoudness = currentLoudness + modloudness;
        }


        public static void ModConditionalStatCalc(Weapon weap, Mod mod, bool folded, string weapType, string weapOpType, ref bool hasShoulderContact, ref float modAutoROF, ref float modSemiROF, ref bool stockAllowsFSADS, ref float modVRecoil, ref float modHRecoil, ref float modCamRecoil, ref float modAngle, ref float modDispersion, ref float modErgo, ref float modAccuracy, ref string modType, ref string position, ref float modChamber, ref float modLoudness, ref float modMalfChance, ref float modDuraBurn)
        {
            if (Helper.IsStock(mod) == true)
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

                    if (Helper.ProgramKEnabled == true)
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
                        }
                        if (mod.Slots[0].ContainedItem != null)
                        {
                            Mod containedMod = mod.Slots[0].ContainedItem as Mod;
                            if (AttachmentProperties.ModType(containedMod) != "buffer")
                            {
                                return;
                            }
                            if (Helper.ProgramKEnabled == false)
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
                modDuraBurn *= 0.25f;
                return;
            }


            if (modType == "foregrip_adapter" && mod.Slots[0].ContainedItem != null)
            {
                modErgo = 0f;
                return;
            }

            if (Helper.IsSilencer(mod) == true || Helper.IsFlashHider(mod) == true || Helper.IsMuzzleCombo(mod) == true)
            {
                if (weap.BoltAction == true || WeaponProperties.OperationType(weap) == "manual")
                {
                    modMalfChance = 0f;
                }
                if (WeaponProperties.WeaponType(weap) == "DI")
                {
                    modDuraBurn *= 1.25f;
                }
            }

            if (modType == "muzzle_supp_adapter" && mod.Slots[0].ContainedItem != null)
            {
                Mod containedMod = mod.Slots[0].ContainedItem as Mod;
                if (Helper.IsSilencer(containedMod))
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
                if (Helper.IsForegrip(containedMod) || (AttachmentProperties.ModType(containedMod) == "foregrip_adapter" && containedMod.Slots[0].ContainedItem != null))
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
                    modHRecoil = 20f;
                    modCamRecoil = 20f;
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
                    modMalfChance *= 2f;
                    modHRecoil *= 1.5f;
                    modCamRecoil *= 1.5f;
                    modSemiROF *= 1.5f;
                    modAutoROF *= 1.5f;
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
                return "rear";
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
                    return 2.5f;
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
                    return 2.65f;
                case "Caliber545x39":
                    return 2.7f;
                case "Caliber556x45NATO":
                    return 2.75f;
                case "Caliber762x51":
                    return 3.1f;
                case "Caliber762x54R":
                    return 3.1f;
                case "Caliber127x55":
                    return 4.5f;
                case "Caliber86x70":
                    return 8f;
                case "Caliber127x108":
                    return 8f;
                case "Caliber23x75":
                    return 4.5f;
                case "Caliber12g":
                    return 3.8f;
                case "Caliber20g":
                    return 3.5f;
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
                default:
                    torque = 0;
                    break;
            }
            return torque;
        }
    }
}
