using Aki.Reflection.Patching;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using static RealismMod.Attributes;
using UnityEngine;
using UnityEngine.UI;
using EFT;
using BepInEx.Logging;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using StatAttributeClass = GClass2752;
using BarrelTemplateClass = GClass2579;
using ArmorPlateUIClass = GClass2633;
using FormatArmorClass = GClass2520;
namespace RealismMod
{

    public class ArmorLevelDisplayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(FormatArmorClass).GetMethod("FormatArmorClassIcon", BindingFlags.Static | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool PatchPrefix(FormatArmorClass __instance, ref string __result, int armorClass)
        {
            __result = "Lvl " + armorClass.ToString();
            return false;
        }
    }

    public class ArmorLevelUIPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ItemViewStats).GetMethod("method_1", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPrefix(ItemViewStats __instance, ArmorPlateUIClass armorPlate)
        {
            Image armorClassImage = (Image)AccessTools.Field(typeof(ItemViewStats), "_armorClassIcon").GetValue(__instance);
            if (armorPlate.Armor.Template.ArmorClass > 6)
            {
                string armorClass = string.Format("{0}.png", armorPlate.Armor.Template.ArmorClass);
                armorClassImage.sprite = Plugin.LoadedSprites[armorClass];
            }
        }
    }

    public class GetCachedReadonlyQualitiesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(AmmoTemplate).GetMethod("GetCachedReadonlyQualities", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPostfix]
        private static void PatchPostFix(AmmoTemplate __instance, ref List<ItemAttributeClass> __result)
        {

            if (!__result.Any((ItemAttributeClass i) => (ENewItemAttributeId)i.Id == ENewItemAttributeId.Damage) && !__result.Any((ItemAttributeClass i) => (ENewItemAttributeId)i.Id == ENewItemAttributeId.ProjectileCount))
            {
                AddCustomAttributes(__instance, ref __result);
            }

        }
        public static void AddCustomAttributes(AmmoTemplate ammoTemplate, ref List<ItemAttributeClass> ammoAttributes)
        {
            if (Plugin.EnableAmmoStats.Value == true)
            {
                float fireRate = (float)Math.Round((ammoTemplate.casingMass - 1) * 100, 2);
                if (fireRate != 0)
                {
                    ItemAttributeClass fireRateAtt = new ItemAttributeClass(ENewItemAttributeId.Firerate);
                    fireRateAtt.Name = ENewItemAttributeId.Firerate.GetName();
                    fireRateAtt.Base = () => fireRate;
                    fireRateAtt.StringValue = () => $"{fireRate} %";
                    fireRateAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                    fireRateAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
                    fireRateAtt.LessIsGood = true;
                    ammoAttributes.Add(fireRateAtt);
                }

                float pelletCount = ammoTemplate.ProjectileCount;
                if (pelletCount > 1)
                {
                    ItemAttributeClass pelletAtt = new ItemAttributeClass(ENewItemAttributeId.ProjectileCount);
                    pelletAtt.Name = ENewItemAttributeId.ProjectileCount.GetName();
                    pelletAtt.Base = () => pelletCount;
                    pelletAtt.StringValue = () => pelletCount.ToString();
                    pelletAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                    pelletAtt.LabelVariations = EItemAttributeLabelVariations.None;
                    ammoAttributes.Add(pelletAtt);
                }

                float fragChance = ammoTemplate.FragmentationChance * 100;
                if (fragChance > 0)
                {
                    ItemAttributeClass fragAtt = new ItemAttributeClass(ENewItemAttributeId.FragmentationChance);
                    fragAtt.Name = ENewItemAttributeId.FragmentationChance.GetName();
                    fragAtt.Base = () => fragChance;
                    fragAtt.StringValue = () => $"{fragChance} %";
                    fragAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                    ammoAttributes.Add(fragAtt);
                }

                float totalDamage = ammoTemplate.Damage * ammoTemplate.ProjectileCount;
                if (totalDamage > 0)
                {
                    ItemAttributeClass damageAtt = new ItemAttributeClass(ENewItemAttributeId.Damage);
                    damageAtt.Name = ENewItemAttributeId.Damage.GetName();
                    damageAtt.Base = () => totalDamage;
                    damageAtt.StringValue = () => $"{totalDamage}";
                    damageAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                    ammoAttributes.Add(damageAtt);
                }

                if (ammoTemplate.PenetrationPower > 0f)
                {
                    ItemAttributeClass penAtt = new ItemAttributeClass(ENewItemAttributeId.Penetration);
                    penAtt.Name = ENewItemAttributeId.Penetration.GetName();
                    penAtt.Base = () => ammoTemplate.PenetrationPower;
                    penAtt.StringValue = () => $"{ammoTemplate.PenetrationPower}";
                    penAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                    ammoAttributes.Add(penAtt);
                }

                if (ammoTemplate.BallisticCoeficient > 0f)
                {
                    ItemAttributeClass bcAtt = new ItemAttributeClass(ENewItemAttributeId.BallisticCoefficient);
                    bcAtt.Name = ENewItemAttributeId.BallisticCoefficient.GetName();
                    bcAtt.Base = () => ammoTemplate.BallisticCoeficient;
                    bcAtt.StringValue = () => $"{ammoTemplate.BallisticCoeficient}";
                    bcAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                    ammoAttributes.Add(bcAtt);
                }

                if (ammoTemplate.ArmorDamage > 0f)
                {
                    ItemAttributeClass armorDamAtt = new ItemAttributeClass(ENewItemAttributeId.ArmorDamage);
                    armorDamAtt.Name = ENewItemAttributeId.ArmorDamage.GetName();
                    armorDamAtt.Base = () => ammoTemplate.ArmorDamage;
                    armorDamAtt.StringValue = () => $"{ammoTemplate.ArmorDamage}";
                    armorDamAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                    ammoAttributes.Add(armorDamAtt);
                }
            }
        }
    }

    public class AmmoMalfChanceDisplayPatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            return typeof(AmmoTemplate).GetMethod("method_12", BindingFlags.Instance | BindingFlags.Public);
        }

        private static string[] malfChancesKeys = new string[]
        {
            "Malfunction/NoneChance",
            "Malfunction/VeryLowChance",
            "Malfunction/LowChance",
            "Malfunction/MediumChance",
            "Malfunction/HighChance",
            "Malfunction/VeryHighChance"
        };

        [PatchPrefix]
        private static bool Prefix(AmmoTemplate __instance, ref string __result)
        {
            float malfChance = __instance.MalfMisfireChance;
            string text = "";
            switch (malfChance)
            {
                case <= 0f:
                    text = malfChancesKeys[0];
                    break;
                case <= 0.15f:
                    text = malfChancesKeys[1];
                    break;
                case <= 0.3f:
                    text = malfChancesKeys[2];
                    break;
                case <= 0.6f:
                    text = malfChancesKeys[3];
                    break;
                case <= 1.2f:
                    text = malfChancesKeys[4];
                    break;
                case > 1.2f:
                    text = malfChancesKeys[5];
                    break;
            }
            __result = text.Localized(null);
            return false;
        }
    }

    public class MagazineMalfChanceDisplayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MagazineClass).GetMethod("method_39", BindingFlags.Instance | BindingFlags.Public);
        }

        private static string[] malfChancesKeys = new string[]
        {
        "Malfunction/NoneChance",
        "Malfunction/VeryLowChance",
        "Malfunction/LowChance",
        "Malfunction/MediumChance",
        "Malfunction/HighChance",
        "Malfunction/VeryHighChance"
        };

        [PatchPrefix]
        private static bool Prefix(MagazineClass __instance, ref string __result)
        {
            float malfChance = __instance.MalfunctionChance;
            string text = "";
            switch (malfChance)
            {
                case <= 0f:
                    text = malfChancesKeys[0];
                    break;
                case <= 0.05f:
                    text = malfChancesKeys[1];
                    break;
                case <= 0.2f:
                    text = malfChancesKeys[2];
                    break;
                case <= 0.6f:
                    text = malfChancesKeys[3];
                    break;
                case <= 1.2f:
                    text = malfChancesKeys[4];
                    break;
                case > 1.2f:
                    text = malfChancesKeys[5];
                    break;
            }
            __result = text.Localized(null);
            return false;
        }
    }

    public class AmmoDuraBurnDisplayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(AmmoTemplate).GetMethod("method_16", BindingFlags.Instance | BindingFlags.Public);
        }


        [PatchPrefix]
        private static bool Prefix(AmmoTemplate __instance, ref string __result)
        {
            float duraBurn = __instance.DurabilityBurnModificator - 1f;

            switch (duraBurn)
            {
                //560 = 5.6
                case <= 2f:
                    __result = duraBurn.ToString("P1");
                    break;
                case <= 4f:
                    __result = "Significant Increase";
                    break;
                case <= 8f:
                    __result = "Substantial Increase";
                    break;
                case <= 10f:
                    __result = "Large Increase";
                    break;
                case <= 15f:
                    __result = "Very Large Increase";
                    break;
                case <= 100f:
                    __result = "Huge Increase";
                    break;
            }
            return false;
        }
    }

    public class ModVRecoilStatDisplayPatchFloat : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Mod).GetMethod("method_16", BindingFlags.Instance | BindingFlags.Public);
        }


        [PatchPrefix]
        private static bool Prefix(ref float __result)
        {
            __result = 0;
            return false;
        }
    }

    public class ModVRecoilStatDisplayPatchString : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Mod).GetMethod("method_17", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(ref string __result)
        {
            __result = "";
            return false;
        }
    }

    public class ModErgoStatDisplayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Mod).GetMethod("method_19", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(Mod __instance, ref string __result)
        {
            __result = __instance.Ergonomics + "%";
            return false;
        }
    }

    public class AmmoCaliberPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("get_AmmoCaliber", BindingFlags.Instance | BindingFlags.Public);
        }

        private static bool IsMulti556(Mod mod) 
        {
            return Utils.IsBarrel(mod) && AttachmentProperties.ModType(mod) == "556";  
        }
        private static bool IsMulti308(Mod mod)
        {
            return Utils.IsBarrel(mod) && AttachmentProperties.ModType(mod) == "308";
        }
        private static bool IsMulti300(Mod mod)
        {
            return Utils.IsBarrel(mod) && AttachmentProperties.ModType(mod) == "300";
        }
        private static bool IsMulti762x39(Mod mod)
        {
            return Utils.IsBarrel(mod) && AttachmentProperties.ModType(mod) == "762x39";
        }
        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref string __result)
        {
            if (__instance.Mods.Any(IsMulti556)) 
            {
                __result = "556x45NATO";
                return false;
            }
            if (__instance.Mods.Any(IsMulti300))
            {
                __result = "762x35";
                return false;
            }
            if (__instance.Mods.Any(IsMulti308))
            {
                __result = "762x51";
                return false;
            }
            if (__instance.Mods.Any(IsMulti762x39))
            {
                __result = "762x39";
                return false;
            }
            return true;
        }
    }

    public class BarrelModClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BarrelModClass).GetConstructor(new Type[] { typeof(string), typeof(BarrelTemplateClass) });
        }

        [PatchPostfix]
        private static void PatchPostfix(BarrelModClass __instance, BarrelTemplateClass template)
        {
            float shotDisp = (template.ShotgunDispersion - 1f) * 100f;

            ItemAttributeClass shotDispAtt = new ItemAttributeClass(ENewItemAttributeId.ShotDispersion);
            shotDispAtt.Name = ENewItemAttributeId.ShotDispersion.GetName();
            shotDispAtt.Base = () => shotDisp;
            shotDispAtt.StringValue = () => $"{shotDisp}%";
            shotDispAtt.LessIsGood = false;
            shotDispAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            shotDispAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(shotDispAtt, __instance);
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
            float chamberSpeed = AttachmentProperties.ChamberSpeed(__instance);
            float aimSpeed = AttachmentProperties.AimSpeed(__instance);
            float shotDisp = AttachmentProperties.ModShotDispersion(__instance);
            float conv = AttachmentProperties.ModConvergence(__instance);
            float meleeDmg = AttachmentProperties.ModMeleeDamage(__instance);
            float meleePen = AttachmentProperties.ModMeleePen(__instance);

            if (Plugin.ServerConfig.malf_changes == true)
            {
                ItemAttributeClass malfAtt = new ItemAttributeClass(Attributes.ENewItemAttributeId.MalfunctionChance);
                malfAtt.Name = ENewItemAttributeId.MalfunctionChance.GetName();
                malfAtt.Base = () => malfChance;
                malfAtt.StringValue = () => $"{getMalfOdds(malfChance)}";
                malfAtt.LessIsGood = true;
                malfAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                malfAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
                Utils.SafelyAddAttributeToList(malfAtt, __instance);
            }


            if (AttachmentProperties.StockAllowADS(__instance))
            {
                ItemAttributeClass canADSAttAttClass = new ItemAttributeClass(Attributes.ENewItemAttributeId.CanADS);
                canADSAttAttClass.Name = ENewItemAttributeId.CanADS.GetName();
                canADSAttAttClass.Base = () => 1;
                canADSAttAttClass.StringValue = () => "";
                canADSAttAttClass.DisplayType = () => EItemAttributeDisplayType.Compact;
                Utils.SafelyAddAttributeToList(canADSAttAttClass, __instance);
            }

            ItemAttributeClass hRecoilAtt = new ItemAttributeClass(Attributes.ENewItemAttributeId.HorizontalRecoil);
            hRecoilAtt.Name = ENewItemAttributeId.HorizontalRecoil.GetName();
            hRecoilAtt.Base = () => hRecoil;
            hRecoilAtt.StringValue = () => $"{hRecoil}%";
            hRecoilAtt.LessIsGood = true;
            hRecoilAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            hRecoilAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(hRecoilAtt, __instance);

            ItemAttributeClass vRecoilAtt = new ItemAttributeClass(ENewItemAttributeId.VerticalRecoil);
            vRecoilAtt.Name = ENewItemAttributeId.VerticalRecoil.GetName();
            vRecoilAtt.Base = () => vRecoil;
            vRecoilAtt.StringValue = () => $"{vRecoil}%";
            vRecoilAtt.LessIsGood = true;
            vRecoilAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            vRecoilAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(vRecoilAtt, __instance);

            ItemAttributeClass dispersionAtt = new ItemAttributeClass(ENewItemAttributeId.Dispersion);
            dispersionAtt.Name = ENewItemAttributeId.Dispersion.GetName();
            dispersionAtt.Base = () => disperion;
            dispersionAtt.StringValue = () => $"{disperion}%";
            dispersionAtt.LessIsGood = true;
            dispersionAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            dispersionAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(dispersionAtt, __instance);

            ItemAttributeClass cameraRecAtt = new ItemAttributeClass(ENewItemAttributeId.CameraRecoil);
            cameraRecAtt.Name = ENewItemAttributeId.CameraRecoil.GetName();
            cameraRecAtt.Base = () => cameraRecoil;
            cameraRecAtt.StringValue = () => $"{cameraRecoil}%";
            cameraRecAtt.LessIsGood = true;
            cameraRecAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            cameraRecAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(cameraRecAtt, __instance);

            ItemAttributeClass autoROFAtt = new ItemAttributeClass(ENewItemAttributeId.AutoROF);
            autoROFAtt.Name = ENewItemAttributeId.AutoROF.GetName();
            autoROFAtt.Base = () => autoROF;
            autoROFAtt.StringValue = () => $"{autoROF}%";
            autoROFAtt.LessIsGood = false;
            autoROFAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            autoROFAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(autoROFAtt, __instance);

            ItemAttributeClass semiROFAtt = new ItemAttributeClass(ENewItemAttributeId.SemiROF);
            semiROFAtt.Name = ENewItemAttributeId.SemiROF.GetName();
            semiROFAtt.Base = () => semiROF;
            semiROFAtt.StringValue = () => $"{semiROF}%";
            semiROFAtt.LessIsGood = false;
            semiROFAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            semiROFAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(semiROFAtt, __instance);

            ItemAttributeClass angleAtt = new ItemAttributeClass(ENewItemAttributeId.RecoilAngle);
            angleAtt.Name = ENewItemAttributeId.RecoilAngle.GetName();
            angleAtt.Base = () => angle;
            angleAtt.StringValue = () => $"{angle}%";
            angleAtt.LessIsGood = false;
            angleAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            angleAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(angleAtt, __instance);

            ItemAttributeClass reloadSpeedAtt = new ItemAttributeClass(ENewItemAttributeId.ReloadSpeed);
            reloadSpeedAtt.Name = ENewItemAttributeId.ReloadSpeed.GetName();
            reloadSpeedAtt.Base = () => reloadSpeed;
            reloadSpeedAtt.StringValue = () => $"{reloadSpeed}%";
            reloadSpeedAtt.LessIsGood = false;
            reloadSpeedAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            reloadSpeedAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(reloadSpeedAtt, __instance);

            ItemAttributeClass chamberSpeedAtt = new ItemAttributeClass(ENewItemAttributeId.ChamberSpeed);
            chamberSpeedAtt.Name = ENewItemAttributeId.ChamberSpeed.GetName();
            chamberSpeedAtt.Base = () => chamberSpeed;
            chamberSpeedAtt.StringValue = () => $"{chamberSpeed}%";
            chamberSpeedAtt.LessIsGood = false;
            chamberSpeedAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            chamberSpeedAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(chamberSpeedAtt, __instance);

            ItemAttributeClass aimSpeedAtt = new ItemAttributeClass(ENewItemAttributeId.AimSpeed);
            aimSpeedAtt.Name = ENewItemAttributeId.AimSpeed.GetName();
            aimSpeedAtt.Base = () => aimSpeed;
            aimSpeedAtt.StringValue = () => $"{aimSpeed}%";
            aimSpeedAtt.LessIsGood = false;
            aimSpeedAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            aimSpeedAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(aimSpeedAtt, __instance);

            ItemAttributeClass shotDispAtt = new ItemAttributeClass(ENewItemAttributeId.ShotDispersion);
            shotDispAtt.Name = ENewItemAttributeId.ShotDispersion.GetName();
            shotDispAtt.Base = () => shotDisp;
            shotDispAtt.StringValue = () => $"{shotDisp}%";
            shotDispAtt.LessIsGood = false;
            shotDispAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            shotDispAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(shotDispAtt, __instance);

            ItemAttributeClass convAtt = new ItemAttributeClass(ENewItemAttributeId.Convergence);
            convAtt.Name = ENewItemAttributeId.Convergence.GetName();
            convAtt.Base = () => conv;
            convAtt.StringValue = () => $"{conv}%";
            convAtt.LessIsGood = false;
            convAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            convAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(convAtt, __instance);

            ItemAttributeClass meleeDmgAtt = new ItemAttributeClass(ENewItemAttributeId.MeleeDamage);
            meleeDmgAtt.Name = ENewItemAttributeId.MeleeDamage.GetName();
            meleeDmgAtt.Base = () => meleeDmg;
            meleeDmgAtt.StringValue = () => $"{meleeDmg}";
            meleeDmgAtt.LessIsGood = false;
            meleeDmgAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            meleeDmgAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(meleeDmgAtt, __instance);

            ItemAttributeClass meleePenAtt = new ItemAttributeClass(ENewItemAttributeId.MeleePen);
            meleePenAtt.Name = ENewItemAttributeId.MeleePen.GetName();
            meleePenAtt.Base = () => meleePen;
            meleePenAtt.StringValue = () => $"{meleePen}";
            meleePenAtt.LessIsGood = false;
            meleePenAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
            meleePenAtt.LabelVariations = EItemAttributeLabelVariations.Colored;
            Utils.SafelyAddAttributeToList(meleePenAtt, __instance);
        }

        public static string getMalfOdds(float malfChance)
        {
            switch (malfChance)
            {
                case < 0:
                    return $"{malfChance}%";
                case 0:
                    return "No Change";
                case <= 50:
                    return $"{malfChance}%";
                case <= 99:
                    return "Small Increase";
                case <= 500:
                    return "Significant Increase";
                case <= 1000:
                    return "Large Increase";
                case <= 5000:
                    return "Critical Increase";
                default:
                    return "";
            }
        }
    }



    public class CenterOfImpactMOAPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BarrelModClass).GetMethod("get_CenterOfImpactMOA", BindingFlags.Instance | BindingFlags.Public);
        }


        [PatchPrefix]
        private static bool Prefix(BarrelModClass __instance, ref float __result)
        {
            BarrelComponent itemComponent = __instance.GetItemComponent<BarrelComponent>();
            if (itemComponent == null)
            {
                __result = 0f;
            }
            else
            {
                __result = (float)Math.Round((double)(100f * itemComponent.Template.CenterOfImpact / 2.9089f) * 2, 2);
            }

            return false;
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

            if (Plugin.ShowBalance.Value == true)
            {
                List<ItemAttributeClass> balanceAttList = __instance.Attributes;
                StatAttributeClass balanceAtt = new StatAttributeClass((EItemAttributeId)ENewItemAttributeId.Balance);
                balanceAtt.Name = ENewItemAttributeId.Balance.GetName();
                balanceAtt.Range = new Vector2(100f, 200f);
                balanceAtt.LessIsGood = false;
                balanceAtt.Base = () => 150;
                balanceAtt.Delta = () => BalanceDelta();
                balanceAtt.StringValue = () => Math.Round(UIWeaponStats.Balance, 1).ToString();
                balanceAtt.DisplayType = () => EItemAttributeDisplayType.FullBar;
                balanceAttList.Add(balanceAtt);

            }

            if (Plugin.ShowDispersion.Value == true)
            {
                List<ItemAttributeClass> dispersionAttList = __instance.Attributes;
                StatAttributeClass dispersionAtt = new StatAttributeClass((EItemAttributeId)ENewItemAttributeId.Dispersion);
                dispersionAtt.Name = ENewItemAttributeId.Dispersion.GetName();
                dispersionAtt.Range = new Vector2(0f, 50f);
                dispersionAtt.LessIsGood = true;
                dispersionAtt.Base = () => __instance.Template.RecolDispersion;
                dispersionAtt.Delta = () => DispersionDelta(__instance);
                dispersionAtt.StringValue = () => Math.Round(UIWeaponStats.Dispersion, 1).ToString();
                dispersionAtt.DisplayType = () => EItemAttributeDisplayType.FullBar;
                dispersionAttList.Add(dispersionAtt);
            }

            if (Plugin.ShowCamRecoil.Value == true)
            {
                List<ItemAttributeClass> camRecoilAttList = __instance.Attributes;
                StatAttributeClass camRecoilAtt = new StatAttributeClass((EItemAttributeId)ENewItemAttributeId.CameraRecoil);
                camRecoilAtt.Name = ENewItemAttributeId.CameraRecoil.GetName();
                camRecoilAtt.Range = new Vector2(0f, 50f);
                camRecoilAtt.LessIsGood = true;
                camRecoilAtt.Base = () => __instance.Template.RecoilCamera * 100f;
                camRecoilAtt.Delta = () => CamRecoilDelta(__instance);
                camRecoilAtt.StringValue = () => Math.Round(UIWeaponStats.CamRecoil * 100f, 2).ToString();
                camRecoilAtt.DisplayType = () => EItemAttributeDisplayType.FullBar;
                camRecoilAttList.Add(camRecoilAtt);
            }

            if (Plugin.ShowRecoilAngle.Value == true)
            {
                List<ItemAttributeClass> recoilAngleAttList = __instance.Attributes;
                ItemAttributeClass recoilAngleAtt = new ItemAttributeClass(ENewItemAttributeId.RecoilAngle);
                recoilAngleAtt.Name = ENewItemAttributeId.RecoilAngle.GetName();
                recoilAngleAtt.Base = () => UIWeaponStats.RecoilAngle;
                recoilAngleAtt.StringValue = () => Math.Round(UIWeaponStats.RecoilAngle).ToString();
                recoilAngleAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                recoilAngleAttList.Add(recoilAngleAtt);
            }

            if (Plugin.ShowSemiROF.Value == true)
            {
                List<ItemAttributeClass> semiROFAttList = __instance.Attributes;
                ItemAttributeClass semiROFAtt = new ItemAttributeClass(ENewItemAttributeId.SemiROF);
                semiROFAtt.Name = ENewItemAttributeId.SemiROF.GetName();
                semiROFAtt.Base = () => UIWeaponStats.SemiFireRate;
                semiROFAtt.StringValue = () => UIWeaponStats.SemiFireRate.ToString() + " " + "RPM".Localized(null);
                semiROFAtt.DisplayType = () => EItemAttributeDisplayType.Compact;
                semiROFAttList.Add(semiROFAtt);
            }

        }

        private static float BalanceDelta()
        {
            float currentBalance = 150f - (UIWeaponStats.Balance * -1f);
            return (150f - currentBalance) / (150f * -1f);
        }

        private static float DispersionDelta(Weapon __instance)
        {
            return (__instance.Template.RecolDispersion - UIWeaponStats.Dispersion) / (__instance.Template.RecolDispersion * -1f);
        }

        private static float CamRecoilDelta(Weapon __instance)
        {
            float tempalteCam = __instance.Template.RecoilCamera * 100f;
            return (tempalteCam - (UIWeaponStats.CamRecoil * 100f)) / (tempalteCam * -1f);
        }

    }

    public class HRecoilDisplayDeltaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_26", BindingFlags.Instance | BindingFlags.Public);
        }


        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref float __result)
        {
            StatDeltaDisplay.DisplayDelta(__instance);
            __result = UIWeaponStats.HRecoilDelta;
            return false;
        }
    }

    public class HRecoilDisplayStringValuePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_27", BindingFlags.Instance | BindingFlags.Public);
        }


        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref string __result)
        {
            __result = Math.Round(__instance.Template.RecoilForceBack + __instance.Template.RecoilForceBack * UIWeaponStats.HRecoilDelta, 1).ToString();
            return false;
        }
    }

    public class VRecoilDisplayDeltaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_23", BindingFlags.Instance | BindingFlags.Public);
        }


        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref float __result)
        {
            StatDeltaDisplay.DisplayDelta(__instance);
            __result = UIWeaponStats.VRecoilDelta;
            return false;
        }
    }

    public class VRecoilDisplayStringValuePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_24", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref string __result)
        {
            __result = Math.Round(__instance.Template.RecoilForceUp + (__instance.Template.RecoilForceUp * (UIWeaponStats.VRecoilDelta)), 1).ToString();
            return false;
        }

    }

    public class COIDisplayDeltaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_18", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref float __result)
        {
            float num = __instance.method_10((float)__instance.Repairable.TemplateDurability);
            float num2 = (__instance.GetBarrelDeviation() - num) / (__instance.Single_0 - num);
            __result = UIWeaponStats.COIDelta + num2;
            return false;
        }
    }

    public class COIDisplayStringValuePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_19", BindingFlags.Instance | BindingFlags.Public);
        }

        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref string __result)
        {
            __result = ((GetTotalCOI(__instance, true) * __instance.GetBarrelDeviation() * 100f / 2.9089f) * 2f).ToString("0.0#") + " " + "moa".Localized(null);
            return false;
        }

        private static float GetTotalCOI(Weapon __instance, bool includeAmmo)
        {
            float num = __instance.CenterOfImpactBase * (1f + UIWeaponStats.COIDelta);
            if (!includeAmmo)
            {
                return num;
            }
            AmmoTemplate currentAmmoTemplate = __instance.CurrentAmmoTemplate;
            return num * ((currentAmmoTemplate != null) ? currentAmmoTemplate.AmmoFactor : 1f);
        }
    }

    public class FireRateDisplayStringValuePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_36", BindingFlags.Instance | BindingFlags.Public);
        }


        [PatchPrefix]
        private static bool Prefix(ref string __result)
        {
            __result = UIWeaponStats.AutoFireRate.ToString() + " " + "RPM".Localized(null);
            return false;
        }
    }

    public class ErgoDisplayDeltaPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_15", BindingFlags.Instance | BindingFlags.Public);
        }


        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref float __result)
        {
            StatDeltaDisplay.DisplayDelta(__instance);
            __result = UIWeaponStats.ErgoDelta;
            return false;
        }
    }

    public class ErgoDisplayStringValuePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Weapon).GetMethod("method_16", BindingFlags.Instance | BindingFlags.Public);
        }


        [PatchPrefix]
        private static bool Prefix(Weapon __instance, ref string __result)
        {
            StatDeltaDisplay.DisplayDelta(__instance);
            float ergoTotal = __instance.Template.Ergonomics * (1f + UIWeaponStats.ErgoDelta);
            __result = Mathf.Clamp(ergoTotal, 0f, 100f).ToString("0.##");
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

            float baseCamReturnSpeed = WeaponStats.VisualRecoilMulti(__instance);
            float currentCamReturnSpeed = baseCamReturnSpeed;

            float baseCamRecoil = __instance.Template.RecoilCamera;
            float currentCamRecoil = baseCamRecoil;

            float baseConv = __instance.Template.RecoilCategoryMultiplierHandRotation;
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
            float pureRecoil = 0;

            float currentTorque = 0f;

            float currentReloadSpeed = 0f;

            float currentAimSpeed = 0f;

            float currentChamberSpeed = 0f;

            float currentShotDisp = 0f;

            float currentMalfChance = 0f;

            float currentFixSpeed = 0f;

            string weapOpType = WeaponStats.OperationType(__instance);
            string weapType = WeaponStats.WeaponType(__instance);

            float currentLoudness = 0;

            bool stockAllowsFSADS = false;

            bool folded = __instance.Folded;
            bool hasShoulderContact = false;
            if (WeaponStats.WepHasShoulderContact(__instance) && !folded)
            {
                hasShoulderContact = true;
            }

            foreach (Mod mod in __instance.Mods)
            {
                float modWeight = mod.Weight;
                if (Utils.IsMagazine(mod))
                {
                    modWeight = mod.GetSingleItemTotalWeight();
                }
                float modWeightFactored = StatCalc.FactoredWeight(modWeight);
                float modErgo = mod.Ergonomics;
                float modVRecoil = AttachmentProperties.VerticalRecoil(mod);
                float modHRecoil = AttachmentProperties.HorizontalRecoil(mod);
                float modAutoROF = AttachmentProperties.AutoROF(mod);
                float modSemiROF = AttachmentProperties.SemiROF(mod);
                float modCamRecoil = AttachmentProperties.CameraRecoil(mod);
                float modConv = AttachmentProperties.ModConvergence(mod);
                modVRecoil += modConv > 0f ? modConv * StatCalc.convVRecoilConversion : 0f;
                float modDispersion = AttachmentProperties.Dispersion(mod);
                float modAngle = AttachmentProperties.RecoilAngle(mod);
                float modAccuracy = mod.Accuracy;
                float modReload = 0f;
                float modChamber = 0f;
                float modAim = 0f;
                float modLoudness = 0f;
                float modMalfChance = 0f;
                float modDuraBurn = 0f;
                float modFix = 0f;
                string modType = AttachmentProperties.ModType(mod);
                string position = StatCalc.GetModPosition(mod, weapType, weapOpType, modType);
                StatCalc.ModConditionalStatCalc(__instance, mod, folded, weapType, weapOpType, ref hasShoulderContact, ref modAutoROF, ref modSemiROF, ref stockAllowsFSADS, ref modVRecoil, ref modHRecoil, ref modCamRecoil, ref modAngle, ref modDispersion, ref modErgo, ref modAccuracy, ref modType, ref position, ref modChamber, ref modLoudness, ref modMalfChance, ref modDuraBurn, ref modConv);
                StatCalc.ModStatCalc(mod, modWeight, ref currentTorque, position, modWeightFactored, modAutoROF, ref currentAutoROF, modSemiROF, ref currentSemiROF, modCamRecoil, ref currentCamRecoil, modDispersion, ref currentDispersion, modAngle, ref currentRecoilAngle, modAccuracy, ref currentCOI, modAim, ref currentAimSpeed, modReload, ref currentReloadSpeed, modFix, ref currentFixSpeed, modErgo, ref currentErgo, modVRecoil, ref currentVRecoil, modHRecoil, ref currentHRecoil, ref currentChamberSpeed, modChamber, true, __instance.WeapClass, ref pureErgo, 0, ref currentShotDisp, modLoudness, ref currentLoudness, ref currentMalfChance, modMalfChance, ref pureRecoil, ref currentConv, modConv, ref currentCamReturnSpeed, __instance.IsBeltMachineGun);
            }


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
            float pureErgoDelta = 0f;

            StatCalc.WeaponStatCalc(__instance, currentTorque, ref totalTorque, currentErgo, currentVRecoil, currentHRecoil, currentDispersion, currentCamRecoil, currentRecoilAngle, baseErgo, baseVRecoil, baseHRecoil, ref totalErgo, ref totalVRecoil, ref totalHRecoil, ref totalDispersion, ref totalCamRecoil, ref totalRecoilAngle, ref totalRecoilDamping, ref totalRecoilHandDamping, ref totalErgoDelta, ref totalVRecoilDelta, ref totalHRecoilDelta, ref totalRecoilDamping, ref totalRecoilHandDamping, currentCOI, hasShoulderContact, ref totalCOI, ref totalCOIDelta, baseCOI, pureErgo, ref pureErgoDelta, true);

            UIWeaponStats.TotalConvergence = currentConv;
            UIWeaponStats.ConvergenceDelta = (currentConv - baseConv) / baseConv;
            UIWeaponStats.HasShoulderContact = hasShoulderContact;
            UIWeaponStats.Dispersion = totalDispersion;
            UIWeaponStats.CamRecoil = totalCamRecoil;
            UIWeaponStats.RecoilAngle = totalRecoilAngle;
            UIWeaponStats.TotalVRecoil = totalVRecoil;
            UIWeaponStats.TotalHRecoil = totalHRecoil;
            UIWeaponStats.Balance = totalTorque;
            UIWeaponStats.TotalErgo = totalErgo;
            UIWeaponStats.ErgoDelta = totalErgoDelta;
            UIWeaponStats.VRecoilDelta = totalVRecoilDelta;
            UIWeaponStats.HRecoilDelta = totalHRecoilDelta;
            UIWeaponStats.AutoFireRate = Mathf.Max(300, (int)currentAutoROF);
            UIWeaponStats.SemiFireRate = Mathf.Max(200, (int)currentSemiROF);
            UIWeaponStats.COIDelta = totalCOIDelta;
            UIWeaponStats.CamReturnSpeed = currentCamReturnSpeed;
        }
    }
}
