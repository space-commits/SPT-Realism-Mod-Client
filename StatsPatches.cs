using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using static EFT.Player;

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

    public class method_5Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player.FirearmController).GetMethod("method_5", BindingFlags.Instance | BindingFlags.NonPublic);

        }
        [PatchPrefix]
        private static bool Prefix(ref Player.FirearmController __instance, ref float __result)
        {
            Player player = (Player)AccessTools.Field(typeof(EFT.Player.FirearmController), "_player").GetValue(__instance);
            if (!player.IsAI)
            {
                SkillsClass.GClass1560 skillsClass = (SkillsClass.GClass1560)AccessTools.Field(typeof(EFT.Player.FirearmController), "gclass1560_0").GetValue(__instance);
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
                string position = StatCalc.GetModPosition(magazine, weapType, weapOpType, "");
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
            float ergonomicWeightLessMag = StatCalc.ErgoWeightCalc(weapWeightLessMag, pureErgoDelta, totalErgoDelta);


            float weapTorqueLessMag = totalTorque - magazineTorque;

            float totalReloadSpeedMod = 0;
            float totalFixSpeedMod = 0;
            float totalAimMoveSpeed = 0;
            float totalChamberSpeed = 0;

            StatCalc.SpeedStatCalc(ergonomicWeightLessMag, currentReloadSpeed, currentFixSpeed, totalTorque, weapTorqueLessMag, ref totalReloadSpeedMod, ref totalFixSpeedMod, ref totalAimMoveSpeed, ergonomicWeight, ref totalChamberSpeed, currentChamberSpeed);

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
            WeaponProperties.AimMoveSpeedModifier = totalAimMoveSpeed;


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
                    string position = StatCalc.GetModPosition(__instance.Mods[i], weapType, weapOpType, modType);

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

            if (__instance?.Owner?.ID != null && (__instance.Owner.ID.StartsWith("pmc") || __instance.Owner.ID.StartsWith("scav")))
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
            if (!player.IsAI)
            {
                SkillsClass.GClass1560 skillsClass = (SkillsClass.GClass1560)AccessTools.Field(typeof(EFT.Player.FirearmController), "gclass1560_0").GetValue(__instance);
                PlayerProperties.StrengthSkillAimBuff = 1 - player.Skills.StrengthBuffAimFatigue.Value;
                PlayerProperties.ReloadSkillMulti = skillsClass.ReloadSpeed;
                PlayerProperties.FixSkillMulti = skillsClass.FixSpeed;

                /*                FirearmsAnimator firearmAnimator = (FirearmsAnimator)AccessTools.Field(typeof(EFT.Player.FirearmController), "firearmsAnimator_0").GetValue(__instance);
 */
            }
        }
    }


}
