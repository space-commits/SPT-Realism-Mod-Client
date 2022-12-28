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

        public static float VRecoilWeightMult = 2.2f;
        public static float VRecoilTorqueMult = 0.68f;

        public static float HRecoilWeightMult = 3.25f;
        public static float HRecoilTorqueMult = 0.75f;

        public static float DispersionWeightMult = 1.45f;
        public static float DispersionTorqueMult = 1.25f;

        public static float CamWeightMult = 3.2f;
        public static float CamTorqueMult = 0.25f;

        public static float AngleTorqueMult = 1f;//needs tweaking
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

        public static float ReloadSpeedWeightMult = 0.9f;//
        public static float ReloadSpeedTorqueMult = 1.1f;// needs tweaking
        public static float ReloadSpeedMult = 0.35f;//

        public static float ChamberSpeedWeightMult = 0.9f;//
        public static float ChamberSpeedTorqueMult = 1.1f;// needs tweaking
        public static float ChamberSpeedMult = 0.35f;//

        public static float AimMoveSpeedWeightMult = 0.9f;//
        public static float AimMoveSpeedTorqueMult = 1.1f;// needs tweaking
        public static float AimMoveSpeedMult = 0.2f;//

        public static float magWeightMult = 10f;


        public static void SetMagReloadSpeeds(Player.FirearmController __instance, MagazineClass magazine)
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
                StatCalc.MagReloadSpeedModifier(magazine, true, false);
            }
        }

        public static void MagReloadSpeedModifier(MagazineClass magazine, bool isNewMag, bool reloadFromNoMag)
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
        }


        /*        public static float ergoWeightCalc(float totalWeight, float totalErgoDelta)
                {
                    float factoredWeight = totalWeight * (1 - (totalErgoDelta * 0.2f));
                    return Mathf.Clamp((float)(Math.Pow(factoredWeight * 1.78, 3.5) + 1) / 200, 1f, 115f);
                }*/

        public static float ErgoWeightCalc(float totalWeight, float pureErgoDelta, float totalTorque)
        {
            float totalTorqueFactorInverse = totalTorque / 100f * -1f;
            float ergoFactoredWeight = (totalWeight * 1f) * (1f - (pureErgoDelta * 0.3f));
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
            if (weap.WeapClass == "pistol")
            {
                angleTorqueMulti = StatCalc.PistolAngleTorqueMult;
            }
            else 
            {
                angleTorqueMulti = StatCalc.AngleTorqueMult;
            }


            float weaponBaseWeight = weap.Weight;
            float weaponBaseWeightFactored = FactoredWeight(weaponBaseWeight);
            float weaponBaseTorque = TorqueCalc(WeaponProperties.BaseTorqueDistance(weap), weaponBaseWeightFactored, weap.WeapClass);

            float ergoWeapBaseWeightFactor = WeightStatCalc(StatCalc.ErgoWeightMult, weaponBaseWeight) / 100;
            float vRecoilWeapBaseWeightFactor = WeightStatCalc(StatCalc.VRecoilWeightMult, weaponBaseWeight) / 100f;
            float hRecoilWeapBaseWeightFactor = WeightStatCalc(StatCalc.HRecoilWeightMult, weaponBaseWeight) / 100f;
            float dispersionWeapBaseWeightFactor = WeightStatCalc(StatCalc.DispersionWeightMult, weaponBaseWeight) / 100f;
            float camRecoilWeapBaseWeightFactor = WeightStatCalc(StatCalc.CamWeightMult, weaponBaseWeight) / 100f;

            float totalWeapWeight = weap.GetSingleItemTotalWeight();
            float dampingTotalWeightFactor = WeightStatCalc(StatCalc.DampingWeightMult, totalWeapWeight) / 100f;
            float handDampingTotalWeightFactor = WeightStatCalc(StatCalc.HandDampingWeightMult, totalWeapWeight) / 100f;

            totalTorque = (weaponBaseTorque + currentTorque);

            float totalTorqueFactor = totalTorque / 100f;
            float totalTorqueFactorInverse = totalTorque / 100f * -1f;

            totalErgo = currentErgo + (currentErgo * (ergoWeapBaseWeightFactor + (totalTorqueFactor * StatCalc.ErgoTorqueMult)));
            totalVRecoil = currentVRecoil + (currentVRecoil * (vRecoilWeapBaseWeightFactor + (totalTorqueFactor * StatCalc.VRecoilTorqueMult)));
            totalHRecoil = currentHRecoil + (currentHRecoil * (hRecoilWeapBaseWeightFactor + (totalTorqueFactorInverse * StatCalc.HRecoilTorqueMult)));
            totalCamRecoil = currentCamRecoil + (currentCamRecoil * (camRecoilWeapBaseWeightFactor + (totalTorqueFactorInverse * StatCalc.CamTorqueMult)));
            totalDispersion = currentDispersion + (currentDispersion * (dispersionWeapBaseWeightFactor + (totalTorqueFactor * StatCalc.DispersionTorqueMult)));

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


        public static void ModStatCalc(Mod mod, float modWeight, ref float currentTorque, string position, float modWeightFactored, float modAutoROF, ref float currentAutoROF, float modSemiROF, ref float currentSemiROF, float modCamRecoil, ref float currentCamRecoil, float modDispersion, ref float currentDispersion, float modAngle, ref float currentRecoilAngle, float modAccuracy, ref float currentCOI, float modAim, ref float currentAimSpeed, float modReload, ref float currentReloadSpeed, float modFix, ref float currentFixSpeed, float modErgo, ref float currentErgo, float modVRecoil, ref float currentVRecoil, float modHRecoil, ref float currentHRecoil, ref float currentChamberSpeed, float modChamber, bool isDisplayDelta, string weapClass, ref float pureErgo)
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

            currentCOI = currentCOI + (currentCOI * (modAccuracy / 100f));

            currentAutoROF = currentAutoROF + (currentAutoROF * (modAutoROF / 100f));

            currentSemiROF = currentSemiROF + (currentSemiROF * (modSemiROF / 100f));



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
        }


        public static void ModConditionalStatCalc(Weapon weap, Mod mod, bool folded, string weapType, string weapOpType, ref bool hasShoulderContact, ref float modAutoROF, ref float modSemiROF, ref bool stockAllowsFSADS, ref float modVRecoil, ref float modHRecoil, ref float modCamRecoil, ref float modAngle, ref float modDispersion, ref float modErgo, ref float modAccuracy, ref string modType, ref string position, ref float modChamber)
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
                if (Helper.IsSilencer(containedMod))
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
                if (Helper.IsForegrip(containedMod))
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
                if (IsHandguard(mod) || IsGasblock(mod) || IsFlashHider(mod) || IsForegrip(mod) || IsMuzzleCombo(mod) || IsSilencer(mod) || IsTacticalCombo(mod) || IsFlashlight(mod) || IsBipod(mod) || IsBarrel(mod))
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
                if (IsMagazine(mod) || IsHandguard(mod) || IsGasblock(mod) || IsFlashHider(mod) || IsForegrip(mod) || IsMuzzleCombo(mod) || IsSilencer(mod) || IsTacticalCombo(mod) || IsFlashlight(mod) || IsBipod(mod) || IsBarrel(mod))
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
                if (IsTacticalCombo(mod) || IsFlashlight(mod) || IsBipod(mod) || IsHandguard(mod) || IsGasblock(mod) || IsForegrip(mod) || IsMuzzleCombo(mod) || IsFlashHider(mod) || IsSilencer(mod) || IsBarrel(mod))
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
                distance *= 2f;
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
