using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using static RealismMod.Attributes;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Collections;

namespace RealismMod
{
    public class ModVRecoilStatDisplayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Mod).GetMethod("method_15", BindingFlags.Instance | BindingFlags.NonPublic);
        }


        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref float __result)
        {
            ModVRecoilStatDisplayPatch p = new ModVRecoilStatDisplayPatch();
            __result = p.method_15P(ref __instance);
            return false;
        }

        public float method_15P(ref Weapon __instance)
        {
            return 0;
        }
    }

    public class ModVRecoilStatDisplayPatch2 : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Mod).GetMethod("method_16", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref string __result)
        {
            __result = "";
            return false;
        }
    }

    public class ModConstructorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Mod).GetConstructor(new Type[] { typeof(string), typeof(ModTemplate) });
        }


        [PatchPostfix]
        private static void PatchPostfix(Mod __instance, string id, ModTemplate template)
        {
            float vRecoil = AttachmentProperties.VerticalRecoil(__instance);
            float hRecoil = AttachmentProperties.HorizontalRecoil(__instance);
            float disperion = AttachmentProperties.Dispersion(__instance);
            float cameraRecoil = AttachmentProperties.CameraRecoil(__instance);
            float autoROF = AttachmentProperties.AutoROF(__instance);
            float semiROF = AttachmentProperties.SemiROF(__instance);
            float malfChance = AttachmentProperties.ModMalfunctionChance(__instance);
            float angle = AttachmentProperties.RecoilAngle(__instance);
            float reloadSpeed = AttachmentProperties.ReloadSpeed(__instance);
            float fixSpeed = AttachmentProperties.FixSpeed(__instance);
            float aimSpeed = AttachmentProperties.AimSpeed(__instance);

            GClass2203 hRecoilAtt = new GClass2203(Attributes.ENewItemAttributeId.HorizontalRecoil);
            hRecoilAtt.Name = ENewItemAttributeId.HorizontalRecoil.GetName();
            hRecoilAtt.Base = () => hRecoil;
            hRecoilAtt.StringValue = () => $"{hRecoil}%";
            hRecoilAtt.LessIsGood = true;
            hRecoilAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            hRecoilAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Helper.SafelyAddAttributeToList(hRecoilAtt, __instance);

            GClass2203 vRecoilAtt = new GClass2203(ENewItemAttributeId.VerticalRecoil);
            vRecoilAtt.Name = ENewItemAttributeId.VerticalRecoil.GetName();
            vRecoilAtt.Base = () => vRecoil;
            vRecoilAtt.StringValue = () => $"{vRecoil}%";
            vRecoilAtt.LessIsGood = true;
            vRecoilAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            vRecoilAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Helper.SafelyAddAttributeToList(vRecoilAtt, __instance);

            GClass2203 dispersionAtt = new GClass2203(ENewItemAttributeId.Dispersion);
            dispersionAtt.Name = ENewItemAttributeId.Dispersion.GetName();
            dispersionAtt.Base = () => disperion;
            dispersionAtt.StringValue = () => $"{disperion}%";
            dispersionAtt.LessIsGood = true;
            dispersionAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            dispersionAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Helper.SafelyAddAttributeToList(dispersionAtt, __instance);

            GClass2203 cameraRecAtt = new GClass2203(ENewItemAttributeId.CameraRecoil);
            cameraRecAtt.Name = ENewItemAttributeId.CameraRecoil.GetName();
            cameraRecAtt.Base = () => cameraRecoil;
            cameraRecAtt.StringValue = () => $"{cameraRecoil}%";
            cameraRecAtt.LessIsGood = true;
            cameraRecAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            cameraRecAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Helper.SafelyAddAttributeToList(cameraRecAtt, __instance);

            GClass2203 autoROFAtt = new GClass2203(ENewItemAttributeId.AutoROF);
            autoROFAtt.Name = ENewItemAttributeId.AutoROF.GetName();
            autoROFAtt.Base = () => autoROF;
            autoROFAtt.StringValue = () => $"{autoROF}%";
            autoROFAtt.LessIsGood = false;
            autoROFAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            autoROFAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Helper.SafelyAddAttributeToList(autoROFAtt, __instance);

            GClass2203 semiROFAtt = new GClass2203(ENewItemAttributeId.SemiROF);
            semiROFAtt.Name = ENewItemAttributeId.SemiROF.GetName();
            semiROFAtt.Base = () => semiROF;
            semiROFAtt.StringValue = () => $"{semiROF}%";
            semiROFAtt.LessIsGood = false;
            semiROFAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            semiROFAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Helper.SafelyAddAttributeToList(semiROFAtt, __instance);

            GClass2203 angleAtt = new GClass2203(ENewItemAttributeId.RecoilAngle);
            angleAtt.Name = ENewItemAttributeId.RecoilAngle.GetName();
            angleAtt.Base = () => angle;
            angleAtt.StringValue = () => $"{angle}%";
            angleAtt.LessIsGood = false;
            angleAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            angleAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Helper.SafelyAddAttributeToList(angleAtt, __instance);

            GClass2203 reloadSpeedAtt = new GClass2203(ENewItemAttributeId.ReloadSpeed);
            reloadSpeedAtt.Name = ENewItemAttributeId.ReloadSpeed.GetName();
            reloadSpeedAtt.Base = () => reloadSpeed;
            reloadSpeedAtt.StringValue = () => $"{reloadSpeed}%";
            reloadSpeedAtt.LessIsGood = false;
            reloadSpeedAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            reloadSpeedAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Helper.SafelyAddAttributeToList(reloadSpeedAtt, __instance);

            GClass2203 fixSpeedAtt = new GClass2203(ENewItemAttributeId.FixSpeed);
            fixSpeedAtt.Name = ENewItemAttributeId.FixSpeed.GetName();
            fixSpeedAtt.Base = () => fixSpeed;
            fixSpeedAtt.StringValue = () => $"{fixSpeed}%";
            fixSpeedAtt.LessIsGood = false;
            fixSpeedAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            fixSpeedAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Helper.SafelyAddAttributeToList(fixSpeedAtt, __instance);

            GClass2203 aimSpeedAtt = new GClass2203(ENewItemAttributeId.AimSpeed);
            aimSpeedAtt.Name = ENewItemAttributeId.AimSpeed.GetName();
            aimSpeedAtt.Base = () => aimSpeed;
            aimSpeedAtt.StringValue = () => $"{aimSpeed}%";
            aimSpeedAtt.LessIsGood = false;
            aimSpeedAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            aimSpeedAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Helper.SafelyAddAttributeToList(aimSpeedAtt, __instance);
        }
    }

    public class WeaponConstructorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetConstructor(new Type[] { typeof(string), typeof(WeaponTemplate) });
        }


        [PatchPostfix]
        private static void PatchPostfix(Weapon __instance, string id, WeaponTemplate template)
        {

            if (Plugin.showBalance.Value == true)
            {

                List<GClass2203> balanceAttList = __instance.Attributes;
                GClass2205 balanceAtt = new GClass2205((EItemAttributeId)ENewItemAttributeId.Balance);
                balanceAtt.Name = ENewItemAttributeId.Balance.GetName();
                balanceAtt.Range = new Vector2(100f, 200f);
                balanceAtt.LessIsGood = false;
                balanceAtt.Base = () => 150;
                balanceAtt.Delta = () => BalanceDelta();
                balanceAtt.StringValue = () => DisplayWeaponProperties.Balance.ToString();
                balanceAtt.DisplayType = () => EItemAttributeDisplayType.FullBar;
                balanceAttList.Add(balanceAtt);

            }

            if (Plugin.showDispersion.Value == true)
            {
                List<GClass2203> dispersionAttList = __instance.Attributes;
                GClass2205 dispersionAtt = new GClass2205((EItemAttributeId)ENewItemAttributeId.Dispersion);
                dispersionAtt.Name = ENewItemAttributeId.Dispersion.GetName();
                dispersionAtt.Range = new Vector2(0f, 50f);
                dispersionAtt.LessIsGood = true;
                dispersionAtt.Base = () => __instance.Template.RecolDispersion;
                dispersionAtt.Delta = () => DispersionDelta(__instance);
                dispersionAtt.StringValue = () => DisplayWeaponProperties.Dispersion.ToString();
                dispersionAtt.DisplayType = () => EItemAttributeDisplayType.FullBar;
                dispersionAttList.Add(dispersionAtt);
            }

            if (Plugin.showCamRecoil.Value == true)
            {
                List<GClass2203> camRecoilAttList = __instance.Attributes;
                GClass2205 camRecoilAtt = new GClass2205((EItemAttributeId)ENewItemAttributeId.CameraRecoil);
                camRecoilAtt.Name = ENewItemAttributeId.CameraRecoil.GetName();
                camRecoilAtt.Range = new Vector2(0f, 0.25f);
                camRecoilAtt.LessIsGood = true;
                camRecoilAtt.Base = () => __instance.Template.CameraRecoil;
                camRecoilAtt.Delta = () => CamRecoilDelta(__instance);
                camRecoilAtt.StringValue = () => DisplayWeaponProperties.CamRecoil.ToString();
                camRecoilAtt.DisplayType = () => EItemAttributeDisplayType.FullBar;
                camRecoilAttList.Add(camRecoilAtt);
            }

            if (Plugin.showRecoilAngle.Value == true)
            {
                List<GClass2203> recoilAngleAttList = __instance.Attributes;
                GClass2203 recoilAngleAtt = new GClass2203(ENewItemAttributeId.RecoilAngle);
                recoilAngleAtt.Name = ENewItemAttributeId.RecoilAngle.GetName();
                recoilAngleAtt.Base = () => DisplayWeaponProperties.RecoilAngle;
                recoilAngleAtt.StringValue = () => DisplayWeaponProperties.RecoilAngle.ToString();
                recoilAngleAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                recoilAngleAttList.Add(recoilAngleAtt);
            }

            if (Plugin.showSemiROF.Value == true)
            {
                List<GClass2203> semiROFAttList = __instance.Attributes;
                GClass2203 semiROFAtt = new GClass2203(ENewItemAttributeId.SemiROF);
                semiROFAtt.Name = ENewItemAttributeId.SemiROF.GetName();
                semiROFAtt.Base = () => DisplayWeaponProperties.SemiFireRate;
                semiROFAtt.StringValue = () => DisplayWeaponProperties.SemiFireRate.ToString() + " " + "RPM".Localized(null);
                semiROFAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                semiROFAttList.Add(semiROFAtt);
            }
        }

        public static float BalanceDelta()
        {
            float currentBalance = 150f - (DisplayWeaponProperties.Balance * -1f);
            return (150f - currentBalance) / (150f * -1f);
        }

        public static float DispersionDelta(Weapon __instance)
        {
            return (__instance.Template.RecolDispersion - DisplayWeaponProperties.Dispersion) / (__instance.Template.RecolDispersion * -1f);
        }

        public static float CamRecoilDelta(Weapon __instance)
        {
            return (__instance.Template.CameraRecoil - DisplayWeaponProperties.CamRecoil) / (__instance.Template.CameraRecoil * -1f);
        }

        public static float RecoilAngleDelta(Weapon __instance)
        {
            return (__instance.Template.RecoilAngle - DisplayWeaponProperties.RecoilAngle) / (__instance.Template.RecoilAngle * -1f);
        }

    }

    public class ErgoDisplayDeltaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_13", BindingFlags.Instance | BindingFlags.NonPublic);
        }


        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref float __result)
        {

            __result = DisplayWeaponProperties.ErgoDelta;
            return false;
        }
    }

    public class ErgoDisplayValuePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_14", BindingFlags.Instance | BindingFlags.NonPublic);
        }


        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref string __result)
        {
            StatDeltaDisplay.DisplayDelta(__instance);

            float ergoTotal = __instance.Template.Ergonomics * (1f + DisplayWeaponProperties.ErgoDelta);
            string result = Mathf.Clamp(ergoTotal, 0f, 100f).ToString("0.##");
            __result = result;
            return false;
        }
    }

    public class HRecoilDisplayDeltaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_24", BindingFlags.Instance | BindingFlags.NonPublic);
        }


        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref float __result)
        {
            __result = DisplayWeaponProperties.HRecoilDelta;
            return false;
        }
    }

    public class HRecoilDisplayValuePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_25", BindingFlags.Instance | BindingFlags.NonPublic);
        }


        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref string __result)
        {
            __result = Math.Round(__instance.Template.RecoilForceBack + __instance.Template.RecoilForceBack * DisplayWeaponProperties.HRecoilDelta, 2).ToString();
            return false;
        }

    }


    public class VRecoilDisplayDeltaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_21", BindingFlags.Instance | BindingFlags.NonPublic);
        }


        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref float __result)
        {
            __result = DisplayWeaponProperties.VRecoilDelta;
            return false;
        }
    }

    public class VRecoilDisplayValuePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_22", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref string __result)
        {
            __result = Math.Round(__instance.Template.RecoilForceUp + __instance.Template.RecoilForceUp * DisplayWeaponProperties.VRecoilDelta, 2).ToString();
            return false;
        }

    }

    public class COIDisplayDeltaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_16", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref float __result)
        {
            float Single_0 = (float)AccessTools.Property(typeof(Weapon), "Single_0").GetValue(__instance);
            var method_9 = AccessTools.Method(typeof(Weapon), "method_9");

            float num = (float)method_9.Invoke(__instance, new object[] { __instance.Repairable.TemplateDurability });
            float num2 = (__instance.GetBarrelDeviation() - num) / (Single_0 - num);
            __result = DisplayWeaponProperties.COIDelta + num2;
            return false;
        }
    }

    public class COIDisplayValuePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_17", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref string __result)
        {
            __result = ((GetTotalCOI(ref __instance, true) * __instance.GetBarrelDeviation() * 100f / 2.9089f) * 2f).ToString("0.0#") + " " + "moa".Localized(null);
            return false;
        }

        private static float GetTotalCOI(ref Weapon __instance, bool includeAmmo)
        {
            float num = __instance.CenterOfImpactBase * (1f + DisplayWeaponProperties.COIDelta);
            if (!includeAmmo)
            {
                return num;
            }
            float num2 = num;
            AmmoTemplate currentAmmoTemplate = __instance.CurrentAmmoTemplate;
            return num2 * ((currentAmmoTemplate != null) ? currentAmmoTemplate.AmmoFactor : 1f);
        }
    }

    public class FireRateDisplayStringPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_34", BindingFlags.Instance | BindingFlags.NonPublic);
        }


        [PatchPrefix]
        private static bool Prefix(ref Weapon __instance, ref string __result)
        {
            __result = DisplayWeaponProperties.AutoFireRate.ToString() + " " + "RPM".Localized(null);
            return false;
        }
    }

    public static class StatDeltaDisplay
    {
        public static void DisplayDelta(Weapon __instance)
        {
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

            float currentTorque = 0f;

            float currentReloadSpeed = 0f;

            float currentAimSpeed = 0f;

            float currentFixSpeed = 0f;

            string weapOpType = WeaponProperties.OperationType(__instance);
            string weapType = WeaponProperties.WeaponType(__instance);

            bool folded = __instance.Folded;

            bool hasShoulderContact = false;

            bool stockAllowsFSADS = false;

            if (WeaponProperties.WepHasShoulderContact(__instance) == true && !folded)
            {
                hasShoulderContact = true;
            }

            for (int i = 0; i < __instance.Mods.Length; i++)
            {
                Mod mod = __instance.Mods[i];
                float modWeight = __instance.Mods[i].Weight;
                if (Helper.isMagazine(__instance.Mods[i]) == true)
                {
                    modWeight = __instance.Mods[i].GetSingleItemTotalWeight(); 
                }
                float modWeightFactored = StatCalc.factoredWeight(modWeight);
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
                float modAim = AttachmentProperties.AimSpeed(__instance.Mods[i]);
                float modFix = AttachmentProperties.FixSpeed(__instance.Mods[i]);
                string modType = AttachmentProperties.ModType(__instance.Mods[i]);
                string position = StatCalc.getModPosition(__instance.Mods[i], weapType, weapOpType);

                StatCalc.modTypeStatCalc(__instance, mod, folded, weapType, weapOpType, ref hasShoulderContact, ref modAutoROF, ref modSemiROF, ref stockAllowsFSADS, ref modVRecoil, ref modHRecoil, ref modCamRecoil, ref modAngle, ref modDispersion, ref modErgo, ref modAccuracy, ref modType, ref position);
                StatCalc.modStatCalc(modWeight, ref currentTorque, position, modWeightFactored, modAutoROF, ref currentAutoROF, modSemiROF, ref currentSemiROF, modCamRecoil, ref currentCamRecoil, modDispersion, ref currentDispersion, modAngle, ref currentRecoilAngle, modAccuracy, ref currentCOI, modAim, ref currentAimSpeed, modReload, ref currentReloadSpeed, modFix, ref currentFixSpeed, modErgo, ref currentErgo, modVRecoil, ref currentVRecoil, modHRecoil, ref currentHRecoil);
            }

/*            StatCalc.stockContactStatCalc(hasShoulderContact, __instance, ref currentErgo, ref currentVRecoil, ref currentHRecoil, ref currentCOI, ref currentCamRecoil, ref currentDispersion, ref currentRecoilAngle);
*/
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

            StatCalc.weaponStatCalc(__instance, currentTorque, ref totalTorque, currentErgo, currentVRecoil, currentHRecoil, currentDispersion, currentCamRecoil, currentRecoilAngle, baseErgo, baseVRecoil, baseHRecoil, ref totalErgo, ref totalVRecoil, ref totalHRecoil, ref totalDispersion, ref totalCamRecoil, ref totalRecoilAngle, ref totalRecoilDamping, ref totalRecoilHandDamping, ref totalErgoDelta, ref totalVRecoilDelta, ref totalHRecoilDelta, ref totalRecoilDamping, ref totalRecoilHandDamping, currentCOI, hasShoulderContact, ref totalCOI, ref totalCOIDelta, baseCOI);

            DisplayWeaponProperties.HasShoulderContact = hasShoulderContact;
            DisplayWeaponProperties.Dispersion = totalDispersion;
            DisplayWeaponProperties.CamRecoil = totalCamRecoil;
            DisplayWeaponProperties.RecoilAngle = totalRecoilAngle;
            DisplayWeaponProperties.TotalVRecoil = totalVRecoil;
            DisplayWeaponProperties.TotalHRecoil = totalHRecoil;
            DisplayWeaponProperties.Balance = totalTorque;
            DisplayWeaponProperties.TotalErgo = totalErgo;
            DisplayWeaponProperties.ErgoDelta = totalErgoDelta;
            DisplayWeaponProperties.VRecoilDelta = totalVRecoilDelta;
            DisplayWeaponProperties.HRecoilDelta = totalHRecoilDelta;
            DisplayWeaponProperties.AutoFireRate = Mathf.Max(300, (int)currentAutoROF);
            DisplayWeaponProperties.SemiFireRate = Mathf.Max(200, (int)currentSemiROF);
            DisplayWeaponProperties.COIDelta = totalCOIDelta * -1f;
        }
    }
}
